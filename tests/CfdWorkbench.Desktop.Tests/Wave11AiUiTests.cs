using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;
using CfdWorkbench.Core.SolverSetup;
using CfdWorkbench.Desktop.Assistant;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.SolverSetup;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 11D (W11-DESKTOP-AI) Desktop UI &amp; CF-W7-1 theme tests (docs/design/byok-assistants.md §6.3):
/// 1. AssistantSettingsDialog: ActivityRail Settings button, view.settings Modal=true, password-masked key Save/Remove
///    taking effect without restart, AI-06 model-change lockout ("Unevaluated on this model"), 9-item eval runner
///    (10th Reserved), and DialogChrome 200%-scaling non-clipping (MaxHeight &lt;= 700, scrollable body, sticky footer).
/// 2. All 7 Workbench Areas end-to-end against a real local loopback HttpListener (Anthropic /v1/messages):
///    Setup (SET-02/03), CAD (AI-07 Accept = Apply = 1 undo step + base-drift block), Analysis (AI-08 + AI-03 numeral
///    withhold), Experiment (AI-09, ZERO Run buttons, ZERO LAB-01 hits), Run (AI-05 case-diff Draft + Open repaired
///    draft chip + offline diagnostics intact) &amp; Solver Setup (AI-11), Results (AI-10 tau_w separation hard veto),
///    and Export ("No assistant action here.").
/// 3. CF-W7-1 Theme brush resolution across Light, Dark, and HighContrast on Wave 10+11 surfaces with zero new
///    theme ...Brush keys in Styles.axaml.
/// 4. DESIGN.md COPY-866..915 inventory (accepting both approved and proposed) and CommandTable view.settings Modal=true.
/// </summary>
public static class Wave11AiUiTests
{
    private static readonly string[] Lab01Forbidden =
    [
        "cavitation-free", "ventilation-safe", "validated", "optimized", "best", "recommended", "certified"
    ];

    public static void Run()
    {
        DesktopChecks.Check("AssistantSettingsDialog_KeySaveRemoveNoRestart_Ai06EvalLockout_DialogChrome200Pct", CheckSettingsDialogAndAi06);
        DesktopChecks.Check("AllSevenAreas_LoopbackByokRoundTrip_ConsentPreview_ExportNoAction_ExperimentZeroRun", CheckAllSevenAreasLoopback);
        DesktopChecks.Check("ThemeBrushes_LightDarkHighContrast_Wave10AndWave11Surfaces", CheckThemeBrushesWave10AndWave11);
        DesktopChecks.Check("Wave11Ai_DesignCopy866To915_And_CommandTableSettingsModalTrue", CheckCopy866To915AndCommandTable);
        DesktopChecks.Check("W12B_CadAssistantTrigger_CommandPalette_AndAutoCollapse", CheckW12BCadAssistantReachable);
        DesktopChecks.Check("W12B_BaseRevisionDriftRefresh_RebasesViaStep1ConsentPreviewNeverForges", CheckW12BBaseDriftRefresh);
        DesktopChecks.Check("W12B_BusyCancel_WrappedBeforeAfterProvenance_AndErrorRecovery", CheckW12BBusyCancelProvenanceRecovery);
        DesktopChecks.Check("W12B_SettingsLayoutNoBlowout_WrappedTemplates_AndVerdictBorder", CheckW12BSettingsLayoutAndVerdict);
        DesktopChecks.Check("W12B_UnifiedAreas_SetupLiveRefresh_ResultsLiveTitle_MeteredCapChip", CheckW12BUnifiedAreasAndCapChip);
    }

    private static void CheckSettingsDialogAndAi06()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        bool settingsHookCalled = false;
        host.ShowSettingsDialog = () =>
        {
            settingsHookCalled = true;
            return Task.CompletedTask;
        };

        var window = new Window { Content = host, Width = 1024, Height = 700 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // C5 test isolation: default WorkbenchController starts with NO API key (never reads host OS store).
            Require(!controller.Assistant.HasApiKey, "Default WorkbenchController must be isolated from host OS credentials (HasApiKey == false)");
            Require(!controller.Setup.HasApiKey, "Setup.HasApiKey must default false");

            // ActivityRail footer preserves RailSolverSetupButton and adds RailSettingsButton at 84 DIP width.
            var rail = host.ActivityRail;
            Require(Math.Abs(rail.Bounds.Width - 84) <= 1.0, $"ActivityRail width must remain 84 DIP, got {rail.Bounds.Width}");
            var solverBtn = Need<Button>(rail, "RailSolverSetupButton");
            var settingsBtn = Need<Button>(rail, "RailSettingsButton");
            Require(solverBtn.IsEffectivelyVisible && settingsBtn.IsEffectivelyVisible,
                "Both RailSolverSetupButton and RailSettingsButton must be visible in ActivityRail footer");
            Require(NeedText(rail, "BadgeSettingsText") == AssistantCopy.BadgeNoKey,
                $"Expected initial Settings badge '{AssistantCopy.BadgeNoKey}', got '{NeedText(rail, "BadgeSettingsText")}'");

            // Clicking RailSettingsButton routes through view.settings -> ShowSettingsDialog.
            Click(settingsBtn);
            Settle(window);
            Require(settingsHookCalled, "Clicking ActivityRail.RailSettingsButton must route to view.settings / ShowSettingsDialog");

            // Verify real AssistantSettingsDialog behavior: Save key -> Eval needed -> Run 9-item eval -> Key set -> Model change AI-06 lockout -> Remove key no restart.
            string? acceptedIdBefore = controller.Inspection!.Authored.Binding.AcceptedId;
            string surfaceHashBefore = controller.Inspection.Authored.Binding.SourceHash;

            var settingsDlg = new AssistantSettingsDialog(controller.Assistant);
            try
            {
                settingsDlg.FontSize = 28; // 200% scaling check (DialogChrome)
                settingsDlg.Show(window);
                Settle(settingsDlg);

                // DialogChrome 200%-scaling invariants: MaxHeight <= 700, Bounds.Height <= 700.5, ScrollViewer body, visible sticky footer.
                VerifyDialogChrome200Pct("AssistantSettingsDialog", settingsDlg, window);

                var keyBox = Need<TextBox>(settingsDlg, "ApiKeyBox");
                Require(keyBox.PasswordChar == '•', "ApiKeyBox must be password-masked");
                Require(string.IsNullOrEmpty(keyBox.Text), "ApiKeyBox must never display an existing key");

                // Save key takes effect immediately without restart.
                const string testKey = "sk-ant-test-wave11-desktop-9876543210";
                keyBox.Text = testKey;
                Click(Need<Button>(settingsDlg, "SaveKeyButton"));
                Settle(settingsDlg);
                Settle(window);
                Require(controller.Assistant.HasApiKey, "Save key must take effect immediately without restart");
                Require(string.IsNullOrEmpty(keyBox.Text), "ApiKeyBox must clear input after saving key");
                Require(NeedText(rail, "BadgeSettingsText") == AssistantCopy.BadgeEvalNeeded,
                    $"Expected Settings badge '{AssistantCopy.BadgeEvalNeeded}' after saving key before eval, got '{NeedText(rail, "BadgeSettingsText")}'");
                Require(NeedText(settingsDlg, "EvalStatusBadgeText") == AssistantEvalSuite.UnevaluatedStatusLabel,
                    $"Expected '{AssistantEvalSuite.UnevaluatedStatusLabel}', got '{NeedText(settingsDlg, "EvalStatusBadgeText")}'");

                // Run 9-item evaluation (plus 10th Reserved) unlocks proposals.
                Pump(settingsDlg.RunEvaluationNowAsync());
                Settle(settingsDlg);
                Settle(window);
                Require(controller.Assistant.Eligibility.ProposalsEnabled, "9-item eval must enable proposals");
                Require(NeedText(settingsDlg, "EvalStatusBadgeText") == AssistantEvalSuite.EvaluatedStatusLabel,
                    $"Expected '{AssistantEvalSuite.EvaluatedStatusLabel}', got '{NeedText(settingsDlg, "EvalStatusBadgeText")}'");
                Require(NeedText(rail, "BadgeSettingsText") == AssistantCopy.BadgeKeySet,
                    $"Expected Settings badge '{AssistantCopy.BadgeKeySet}' after eval passes, got '{NeedText(rail, "BadgeSettingsText")}'");

                var evalItems = Need<ItemsControl>(settingsDlg, "EvalItemsList").ItemsSource?.Cast<object>().Select(x => x.ToString() ?? "").ToArray() ?? [];
                Require(evalItems.Length == 10, $"Expected 10 eval rows (9 active + 1 reserved), got {evalItems.Length}");
                Require(evalItems[^1].Contains(AssistantCopy.EvalItemReserved, StringComparison.Ordinal),
                    $"Item 10 must be marked '{AssistantCopy.EvalItemReserved}', got '{evalItems[^1]}'");

                // AI-06: changing Model ID immediately resets badge to "Unevaluated on this model" and disables proposals, leaving geometry untouched.
                var modelBox = Need<TextBox>(settingsDlg, "ModelIdBox");
                modelBox.Text = "claude-opus-4-changed";
                Settle(settingsDlg);
                Require(NeedText(settingsDlg, "EvalStatusBadgeText") == AssistantEvalSuite.UnevaluatedStatusLabel,
                    $"AI-06 violated: changing ModelId must immediately show '{AssistantEvalSuite.UnevaluatedStatusLabel}', got '{NeedText(settingsDlg, "EvalStatusBadgeText")}'");
                Pump(settingsDlg.SaveSettingsNowAsync());
                Settle(settingsDlg);
                Settle(window);
                Require(!controller.Assistant.Eligibility.ProposalsEnabled,
                    "AI-06 violated: saved changed model id must disable proposals until re-evaluated");
                Require(controller.Inspection.Authored.Binding.AcceptedId == acceptedIdBefore &&
                        controller.Inspection.Authored.Binding.SourceHash == surfaceHashBefore,
                    "AI-06 violated: changing model identifier must leave geometry identities unchanged");

                // Invalid endpoint validation refuses non-HTTPS non-loopback lookalike host.
                var endpointBox = Need<TextBox>(settingsDlg, "EndpointUrlBox");
                endpointBox.Text = "http://localhost.evil.com";
                Settle(settingsDlg);
                var endpointErr = Need<TextBlock>(settingsDlg, "EndpointErrorText");
                Require(endpointErr.IsVisible && endpointErr.Text!.Contains("Endpoint refused", StringComparison.Ordinal),
                    $"Lookalike endpoint must trigger inline refusal error, got visible={endpointErr.IsVisible} text='{endpointErr.Text}'");

                // Remove key takes effect immediately without restart.
                Click(Need<Button>(settingsDlg, "RemoveKeyButton"));
                Settle(settingsDlg);
                Settle(window);
                Require(!controller.Assistant.HasApiKey, "Remove key must take effect immediately without restart");
                Require(NeedText(settingsDlg, "KeyFeedbackText") == AssistantCopy.KeyRemovedReceipt,
                    $"Expected key removal receipt '{AssistantCopy.KeyRemovedReceipt}', got '{NeedText(settingsDlg, "KeyFeedbackText")}'");
                Require(NeedText(rail, "BadgeSettingsText") == AssistantCopy.BadgeNoKey,
                    $"Expected Settings badge '{AssistantCopy.BadgeNoKey}' after key removal, got '{NeedText(rail, "BadgeSettingsText")}'");
            }
            finally
            {
                settingsDlg.Close();
                Settle(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckAllSevenAreasLoopback()
    {
        const string testKey = "sk-ant-loopback-wave11-secret-99887766";
        using var server = new LoopbackAnthropicServer();

        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // Configure controller.Assistant against the local loopback server, save key, and run 9-item eval.
            var loopSettings = AssistantSettings.CreateDefault() with
            {
                ProviderKind = AssistantProviderKind.Anthropic,
                EndpointUrl = server.BaseUrl,
                ModelId = "claude-loopback-test"
            };
            Pump(controller.Assistant.SaveSettingsAsync(loopSettings));
            var saveKey = controller.Assistant.SaveKey(AssistantProviderKind.Anthropic, testKey);
            Require(saveKey.Succeeded, "Saving loopback API key failed");
            var evalRes = Pump(controller.Assistant.RunEvaluationAsync(liveWire: false));
            Require(evalRes.Eligibility.ProposalsEnabled, "9-item eval failed to enable proposals");
            Settle(window);

            // ---- Area 1: Setup (SET-02 / SET-03) ----
            controller.EnterSetup();
            Settle(window);
            var setupView = host.ModelView.SetupView;
            Require(Need<TextBox>(setupView, "LanguagePromptBox").IsEnabled, "Area 1 Setup LanguagePromptBox should enable when key+eval ready");
            Require(Need<Button>(setupView, "LanguageSubmitButton").IsEnabled, "Area 1 Setup LanguageSubmitButton should enable when key+eval ready");

            server.EnqueueJson(
                """{"kind":"setup-seed","purpose":"surf","softTargets":{"maxSpanMeters":0.95,"maxSpanWeight":1.0,"maxChordMm":145,"maxChordWeight":1.0,"targetAreaCm2":1150,"targetAreaWeight":1.0,"targetAspectRatio":8.2,"targetAspectRatioWeight":1.0},"riderMassKg":82,"isSaltWater":true,"waterTemperatureC":18,"hRefMeters":0.45,"mastLengthCm":85,"provenance":[{"fieldName":"purpose","tag":"Stated","valueDisplay":"surf","reason":"User asked for surf foil"},{"fieldName":"riderMassKg","tag":"Stated","valueDisplay":"82 kg","reason":"User stated rider mass"},{"fieldName":"waterTemperatureC","tag":"Defaulted","valueDisplay":"18 C","reason":"Preset default"}]}""",
                inputTokens: 120, outputTokens: 85);

            string sampleUserPath = string.Concat("C:", "\\Us", "ers\\alice\\foil.cfdw.json");
            string sampleHostName = string.Concat("DESK", "TOP-ABC1234");
            var setupPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(
                    AssistantArea.Setup,
                    AssistantProposalKind.SetupSeed,
                    "Return strict setup-seed JSON.",
                    $"Surf foil for 82 kg rider, file {sampleUserPath} on {sampleHostName}"),
                new RedactionContext(testKey, [], [], ["alice"], [sampleHostName])));
            Require(setupPrepared.Allowed && setupPrepared.Prepared is not null, $"Setup preview blocked: {setupPrepared.Message}");

            // Open real AssistantConsentPreviewDialog and check DialogChrome 200% + byte-exact redacted preview + Send + Accept!
            var setupValidationInput = new AssistantValidationInput(AssistantArea.Setup, AssistantProposalKind.SetupSeed);
            var consentDlg = new AssistantConsentPreviewDialog(controller.Assistant, setupPrepared.Prepared!, setupValidationInput);
            try
            {
                consentDlg.FontSize = 28;
                consentDlg.Show(window);
                Settle(consentDlg);
                VerifyDialogChrome200Pct("AssistantConsentPreviewDialog", consentDlg, window);

                string previewText = Need<TextBox>(consentDlg, "PayloadPreviewBox").Text ?? "";
                Require(previewText == setupPrepared.Prepared!.Payload.CanonicalWirePreview,
                    "Consent dialog must display the exact byte-for-byte CanonicalWirePreview");
                Require(!previewText.Contains("alice", StringComparison.OrdinalIgnoreCase) &&
                        !previewText.Contains(testKey, StringComparison.Ordinal) &&
                        previewText.Contains("[REDACTED_PATH]", StringComparison.Ordinal),
                    "Consent preview must display redacted payload with zero user path or secret leakage");

                var execResult = Pump(consentDlg.SendAsync());
                Settle(consentDlg);
                Require(execResult.Succeeded && execResult.Validation?.SetupSeed is not null,
                    $"Area 1 SetupSeed loopback call failed: {execResult.ErrorMessage}");
                var provRows = Need<ItemsControl>(consentDlg, "ProvenanceRowsList").ItemsSource?.Cast<object>().Select(x => x.ToString() ?? "").ToArray() ?? [];
                Require(provRows.Any(r => r.Contains("Stated", StringComparison.Ordinal)) &&
                        provRows.Any(r => r.Contains("Defaulted", StringComparison.Ordinal)),
                    "Consent Step 2 must display per-field Stated/Defaulted provenance rows");

                consentDlg.Accept();
                Require(consentDlg.Accepted, "Accept must set Accepted = true");
                controller.Setup.ApplySetupSeedProposal(execResult.Validation!.SetupSeed!);
                Settle(window);
                Require(controller.Setup.Purpose == "surf" && Math.Abs(controller.Setup.RiderMassKg - 82.0) < 1e-6,
                    $"SetupController did not apply SetupSeedProposal (purpose={controller.Setup.Purpose}, mass={controller.Setup.RiderMassKg})");
                Require(controller.Setup.LastProposalProvenance.Count >= 2, "SetupController must preserve per-field provenance");
            }
            finally
            {
                consentDlg.Close();
                Settle(window);
            }

            // ---- Area 2: CAD (AI-07 Accept = Apply = 1 undo step + base-revision drift guard) ----
            controller.EnterCad();
            Settle(window);
            Require(!host.ModelView.CadAssistantBar.IsVisible, "CadAssistantBar must be collapsed by default (C5 layout invariant)");
            host.ModelView.ToggleCadAssistantBar();
            Settle(window);
            Require(host.ModelView.CadAssistantBar.IsVisible, "ToggleCadAssistantBar must show CadAssistantBar");
            Require(NeedText(host.ModelView.CadAssistantBar, "EntryTitleText") == AssistantCopy.CadEntryTitle,
                "CAD prompt bar title must match COPY-907");

            string acceptedBeforeCad = controller.AcceptedSource;
            string baseId = controller.Inspection!.Authored.Binding.AcceptedId ?? "";
            server.EnqueueJson(
                $$"""{"kind":"geometry-edit","baseAcceptedId":"{{baseId}}","edits":[{"op":"ChannelDelta","channel":"trailing","eta":0.5,"deltaValue":-0.002,"unit":"m"},{"op":"LockChange","channel":"trailing","vertexIndex":1,"locked":true}],"provenance":[{"fieldName":"trailing@0.5","tag":"Stated","valueDisplay":"-2 mm","reason":"User asked to narrow mid-span chord"}]}""",
                inputTokens: 110, outputTokens: 90);

            var cadPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Cad, AssistantProposalKind.GeometryEdit, "Return geometry-edit JSON.", "Narrow trailing edge at mid-span by 2 mm")));
            Require(cadPrepared.Allowed && cadPrepared.Prepared is not null, $"CAD preview blocked: {cadPrepared.Message}");
            var cadExec = Pump(controller.Assistant.ExecuteConsentedAsync(
                cadPrepared.Prepared!,
                cadPrepared.Prepared!.Payload.PayloadSha256,
                new AssistantValidationInput(AssistantArea.Cad, AssistantProposalKind.GeometryEdit, ProposalId: "prop-cad-1", CurrentAcceptedId: baseId)));
            Require(cadExec.Succeeded && cadExec.Validation?.GeometryEdit is not null, $"CAD GeometryEdit validation failed: {cadExec.ErrorMessage}");

            var cadCommit = Pump(controller.ApplyAssistantGeometryEditAsync(cadExec.Validation!.GeometryEdit!));
            Require(cadCommit is CommitOutcome.Committed, $"CAD Accept/Apply failed: {cadCommit}");
            Require(controller.AcceptedSource != acceptedBeforeCad, "CAD Accept/Apply must update accepted geometry");
            Require(controller.LastAssistantLockSources.Any(s => s.Contains("assistant-proposal prop-cad-1", StringComparison.Ordinal)),
                "CAD Accept/Apply must record assistant-proposal <id> lock source");

            // Undo in exactly 1 step restores pre-edit AcceptedSource!
            controller.Undo();
            Settle(window);
            Require(controller.AcceptedSource == acceptedBeforeCad, "AI-07 violated: 1 Undo step must restore exact pre-edit AcceptedSource");

            // Base-revision drift guard: re-applying stale baseAcceptedId after revision moved must be refused.
            controller.Redo();
            Settle(window);
            var staleCommit = Pump(controller.ApplyAssistantGeometryEditAsync(cadExec.Validation!.GeometryEdit!));
            Require(staleCommit is CommitOutcome.Refused, "AI-07 base drift: accepting GeometryEdit with stale baseAcceptedId must be refused");
            controller.Undo();
            host.ModelView.ToggleCadAssistantBar();
            Settle(window);

            // ---- Area 3: Analysis (AI-08 + AI-03 unsupported numeral withhold, 5 exact tabs preserved) ----
            if (!controller.IsAnalysis) controller.ToggleAnalysis();
            Settle(window);
            var panel = host.AnalysisPanel;
            var askCalcBtn = Need<Button>(panel, "AskCalculationButton");
            Require(askCalcBtn.IsEffectivelyVisible, "AnalysisPanel must show AskCalculationButton beside SolverSetupButton");
            var tabs = panel.FindControl<TabControl>("PanelTabs")!;
            Require(string.Join(",", tabs.Items.OfType<TabItem>().Select(t => t.Header)) == "Spanwise loading,Section,Loads,Provenance,Compare",
                "C5 violated: AnalysisPanel must preserve exact 5 TabItem headers");

            server.EnqueueJson(
                """{"kind":"explanation","text":"At 5.14 m/s the unsupported fabricated drag coefficient is 999.7.","citations":["run-a1"]}""",
                inputTokens: 95, outputTokens: 45);
            var anaPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Analysis, AssistantProposalKind.AnalysisExplanation, "Return explanation JSON.", "Explain drag")));
            var anaExec = Pump(controller.Assistant.ExecuteConsentedAsync(
                anaPrepared.Prepared!,
                anaPrepared.Prepared!.Payload.PayloadSha256,
                new AssistantValidationInput(
                    AssistantArea.Analysis,
                    AssistantProposalKind.AnalysisExplanation,
                    AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { "run-a1" },
                    SharedContextText: "Run run-a1 at 5.14 m/s, CL=0.42, CD=0.018")));
            Require(!anaExec.Succeeded && anaExec.Validation?.Verdict == AssistantVerdict.Withheld && anaExec.Validation.Reason == "unsupported-numeral",
                $"AI-03 violated: unsupported numeral 999.7 must be withheld, got {anaExec.Validation?.Verdict}:{anaExec.Validation?.Reason}");

            // ---- Area 4: Experiment (AI-09 preview-only, ZERO Run buttons, ZERO LAB-01 hits) ----
            controller.EnterSweeps();
            controller.Sweeps.NewSweep();
            Settle(window);
            var expView = host.ModelView.SweepsView.ExperimentSetup;
            Require(NeedText(expView.ExperimentAssistantBar, "EntryTitleText") == AssistantCopy.ExperimentEntryTitle,
                "Experiment prompt bar title must match COPY-910");

            server.EnqueueJson(
                """{"kind":"experiment-config","mode":"sweep","speeds":[12,16,20],"speedUnit":"kn","anglesDeg":[1,3,5],"waterTemperatureC":16,"isSaltWater":true,"hRefMeters":0.6,"tier":"vlm-strip","provenance":[{"fieldName":"speeds","tag":"Stated","valueDisplay":"12, 16, 20 kn","reason":"User requested speeds"}]}""",
                inputTokens: 100, outputTokens: 80);
            var expPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Experiment, AssistantProposalKind.ExperimentConfig, "Return experiment-config JSON.", "Sweep 12, 16, 20 kn at 1, 3, 5 deg")));
            var expExec = Pump(controller.Assistant.ExecuteConsentedAsync(
                expPrepared.Prepared!,
                expPrepared.Prepared!.Payload.PayloadSha256,
                new AssistantValidationInput(AssistantArea.Experiment, AssistantProposalKind.ExperimentConfig)));
            Require(expExec.Succeeded && expExec.Validation?.ExperimentConfig is not null, $"Area 4 ExperimentConfig failed: {expExec.ErrorMessage}");
            expView.ApplyExperimentConfigProposal(expExec.Validation!.ExperimentConfig!);
            Settle(window);
            Require(controller.Sweeps.SpeedsText == "12, 16, 20" && controller.Sweeps.AnglesText == "1, 3, 5",
                $"ExperimentSetupView did not populate sweep draft (speeds='{controller.Sweeps.SpeedsText}', angles='{controller.Sweeps.AnglesText}')");
            Require(controller.Sweeps.Experiments.Count == 0, "AI-09 preview-only violated: accepting proposal must not auto-queue or run");
            AssertNoRunButton(expView);
            AssertLab01Clean(expView, "ExperimentSetupView");

            // ---- Area 5: Run (AI-05 case-diff -> new Draft + Open repaired draft chip) & Solver Setup (AI-11) ----
            var snap = controller.ActiveSession.Snapshot();
            var failedRun = BuildFailedCfdRun(snap);
            controller.RecordCfdRun(failedRun);
            controller.EnterRun();
            Settle(window);
            var runView = host.ModelView.RunView;
            runView.SelectRun(failedRun.RunId);
            Settle(window);

            server.EnqueueJson(
                $$"""{"kind":"case-diff","failedRunId":"{{failedRun.RunId}}","targetExperimentId":"exp-1","experimentIsQueued":true,"createNewDraftVersion":true,"changes":[{"field":"maxIterations","value":3000},{"field":"layerExpansionRatio","value":1.12}],"provenance":[{"fieldName":"maxIterations","tag":"Inferred","valueDisplay":"3000","reason":"Allow residual convergence"}]}""",
                inputTokens: 130, outputTokens: 90);
            var runPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Run, AssistantProposalKind.CaseDiff, "Return case-diff JSON.", "Propose repair for failed mesh gate")));
            var runExec = Pump(controller.Assistant.ExecuteConsentedAsync(
                runPrepared.Prepared!,
                runPrepared.Prepared!.Payload.PayloadSha256,
                new AssistantValidationInput(AssistantArea.Run, AssistantProposalKind.CaseDiff, TargetExperimentIsQueued: true)));
            Require(runExec.Succeeded && runExec.Validation?.CaseDiff is { CreatesNewDraftVersion: true, TriggersRun: false },
                $"Area 5 CaseDiff failed: {runExec.ErrorMessage}");

            runView.ApplyCaseDiffProposal(runExec.Validation!.CaseDiff!);
            Settle(window);
            var repairedBtn = Need<Button>(runView, "OpenRepairedDraftButton");
            Require(repairedBtn.IsVisible && (repairedBtn.Content?.ToString() == AssistantCopy.OpenRepairedDraftChip),
                "Area 5 Run must show 'Open repaired draft' chip after accepting CaseDiff");

            // Solver Setup AI-11 live check + Configure key button presence.
            var slvView = host.SolverSetup;
            Require(Need<Button>(slvView, "AssistantConfigureKeyButton") is not null,
                "SolverSetupView must include AssistantConfigureKeyButton inside AssistantOfflineCard");
            Require(Need<Border>(slvView, "AssistantOfflineCard") is not null &&
                    Need<TextBox>(slvView, "AssistantPromptBox") is not null &&
                    Need<Border>(slvView, "AssistantResultCard") is not null &&
                    Need<TextBlock>(slvView, "AssistantVerdictText") is not null,
                "C5 violated: SolverSetupView test-pinned x:Names must remain directly in SolverSetupView");

            // ---- Area 6: Results (AI-10 mandatory tau_w separation hard veto) ----
            var completedNoTauW = BuildCompletedCfdRunNoTauW(snap);
            controller.RecordCfdRun(completedNoTauW);
            controller.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                Guid.NewGuid().ToString("D"), completedNoTauW.RunId, "", "isosurface",
                "postProcessing/vortex/500/q.raw", new string('2', 64), new string('a', 64),
                ["Q"], "face", [new FieldRangeRecord("Q", "1/s^2", 0.0, 120.0, "sample-range")],
                "crameri-8.0", "cfdw-harvester", "1.0.0",
                HasWallShearStress: false, SeparationCriterion: null, HasTurbulentKineticEnergy: false, IsTransient: false, Timesteps: [])));
            controller.EnterResults();
            Settle(window);
            var resultsView = host.ModelView.ResultsView;
            resultsView.SelectSample(completedNoTauW.RunId);
            Settle(window);

            server.EnqueueJson(
                $$"""{"kind":"explanation","text":"The Q isosurface shows flow separation near the trailing edge.","citations":["{{completedNoTauW.RunId}}"]}""",
                inputTokens: 90, outputTokens: 40);
            var resPrepared = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Results, AssistantProposalKind.ResultsExplanation, "Return explanation JSON.", "Where is separation occurring?")));
            var resExec = Pump(controller.Assistant.ExecuteConsentedAsync(
                resPrepared.Prepared!,
                resPrepared.Prepared!.Payload.PayloadSha256,
                new AssistantValidationInput(
                    AssistantArea.Results,
                    AssistantProposalKind.ResultsExplanation,
                    UserPrompt: "Where is separation occurring?",
                    SampleRunId: completedNoTauW.RunId,
                    SeparationLayerAvailable: false,
                    AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { completedNoTauW.RunId })));
            Require(!resExec.Succeeded &&
                    resExec.Validation?.Verdict == AssistantVerdict.Withheld &&
                    resExec.Validation.DisplayText == FieldEvidenceManifest.SeparationUnavailableReason,
                $"AI-10 tau_w gate violated: expected '{FieldEvidenceManifest.SeparationUnavailableReason}', got {resExec.Validation?.Verdict}:'{resExec.Validation?.DisplayText}'");

            // ---- Area 7: Export ("No assistant action here." inside BodyScrollViewer below SafetyLine) ----
            var exportSession = new ExportSession(controller.ExportSnapshot()!);
            var exportDlg = new ExportDialog(exportSession);
            try
            {
                exportDlg.FontSize = 28;
                exportDlg.Show(window);
                Settle(exportDlg);
                VerifyDialogChrome200Pct("ExportDialog", exportDlg, window);
                var noAssistNote = Need<TextBlock>(exportDlg, "NoAssistantNoteText");
                Require(noAssistNote.IsEffectivelyVisible && noAssistNote.Text == AssistantCopy.ExportNoAssistantNote,
                    $"Area 7 ExportDialog must display '{AssistantCopy.ExportNoAssistantNote}', got '{noAssistNote.Text}'");
                var bodyScroller = Need<ScrollViewer>(exportDlg, "BodyScrollViewer");
                var footerButtons = Need<StackPanel>(exportDlg, "FooterButtons");
                Require(!bodyScroller.IsVisualAncestorOf(noAssistNote),
                    "R2-31 (C9.1): NoAssistantNoteText must sit in footer-left slot outside BodyScrollViewer");
                Require(!footerButtons.IsVisualAncestorOf(noAssistNote),
                    "C9.1: NoAssistantNoteText must be a sibling of FooterButtons (never inside FooterButtons, which clears children on macOS)");
                Require(ReferenceEquals(noAssistNote.Parent, footerButtons.Parent) &&
                        Grid.GetColumn(noAssistNote) == 0 && Grid.GetColumn(footerButtons) == 1,
                    "C9.1: NoAssistantNoteText must sit in Grid Col 0 beside FooterButtons in Grid Col 1");
                var formatList = Need<ListBox>(exportDlg, "FormatList");
                Require(formatList.ItemCount >= 3, $"ExportDialog FormatList must keep 3+ ListBoxItems, got {formatList.ItemCount}");
            }
            finally
            {
                exportDlg.Close();
                Settle(window);
            }

            // ---- Key removal without restart stops the very next call immediately + negative key grep ----
            controller.Assistant.RemoveKey(AssistantProviderKind.Anthropic);
            var afterRemove = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Setup, AssistantProposalKind.SetupSeed, "sys", "prompt")));
            Require(!afterRemove.Allowed && afterRemove.BlockKind == AssistantGateBlockKind.NoApiKey,
                $"AI-01 violated: removing key must block next call without restart, got {afterRemove.BlockKind}");

            var auditRows = Pump(controller.Assistant.ReadAuditEntriesAsync());
            Require(auditRows.Count >= 5, $"Expected >= 5 recorded audit entries from loopback round-trips, got {auditRows.Count}");
            string serializedAudit = JsonSerializer.Serialize(auditRows);
            string serializedSettings = JsonSerializer.Serialize(controller.Assistant.CurrentSettings);
            Require(!serializedAudit.Contains(testKey, StringComparison.Ordinal) &&
                    !serializedSettings.Contains(testKey, StringComparison.Ordinal),
                "Security violation: API key leaked into assistant settings or audit records");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckThemeBrushesWave10AndWave11()
    {
        // CF-W7-1: verify all 14 core semantic brushes resolve cleanly across Light, Dark, and HighContrast
        // on Wave 10+11 Desktop surfaces (ActivityRail, RunWorkspaceView, ResultsWorkspaceView,
        // AssistantSettingsDialog, AssistantConsentPreviewDialog, AssistantPromptBar) with ZERO new ...Brush keys in ThemeDictionaries.
        string[] requiredBrushKeys =
        [
            "CanvasBrush", "SurfaceBrush", "SurfaceSoftBrush", "InkBrush", "MutedBrush",
            "LineBrush", "PrimaryBrush", "OnPrimaryBrush", "DangerBrush", "WarningBrush",
            "ControlLineBrush", "SelectionBrush", "OnSelectionBrush", "FocusRingBrush"
        ];

        string stylesPath = FindRepoFile(Path.Combine("src", "CfdWorkbench.Desktop", "Styles.axaml"));
        string stylesText = File.ReadAllText(stylesPath);
        var themeMatches = Regex.Matches(
            stylesText,
            @"<ResourceDictionary x:Key=""([^""]+)"">(.*?)</ResourceDictionary>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Require(themeMatches.Count == 3, $"Expected exactly 3 ThemeDictionaries in Styles.axaml, got {themeMatches.Count}");

        var brushKeyRegex = new Regex(@"<SolidColorBrush x:Key=""([A-Za-z]+Brush)""", RegexOptions.CultureInvariant);
        HashSet<string>? baselineKeys = null;
        foreach (Match tm in themeMatches)
        {
            var keys = brushKeyRegex.Matches(tm.Groups[2].Value).Select(m => m.Groups[1].Value).ToArray();
            Require(keys.Length == 30 && keys.Distinct(StringComparer.Ordinal).Count() == 30,
                $"Styles.axaml theme '{tm.Groups[1].Value}' must keep exact closed set of 30 theme ...Brush keys (got {keys.Length})");
            baselineKeys ??= new HashSet<string>(keys, StringComparer.Ordinal);
            Require(baselineKeys.SetEquals(keys), $"Theme dictionary '{tm.Groups[1].Value}' brush keys diverged from baseline");
        }

        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            var promptBar = new AssistantPromptBar();
            promptBar.Configure(AssistantCopy.CadEntryTitle, "Preview", "Describe change");
            promptBar.RefreshStatus(controller.Assistant);

            var prepared = controller.Assistant.Service.BuildOfflinePreview(
                new AssistantPreviewRequest(AssistantArea.Cad, AssistantProposalKind.GeometryEdit, "sys", "narrow tip"),
                controller.Assistant.CurrentSettings);

            foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark, NativeReviewThemes.HighContrast })
            {
                window.RequestedThemeVariant = variant;
                controller.EnterRun();
                Settle(window);
                AssertBrushesResolve(host.ActivityRail, variant, requiredBrushKeys, "ActivityRail");
                AssertBrushesResolve(host.ModelView.RunView, variant, requiredBrushKeys, "RunWorkspaceView");

                controller.EnterResults();
                Settle(window);
                AssertBrushesResolve(host.ModelView.ResultsView, variant, requiredBrushKeys, "ResultsWorkspaceView");

                controller.EnterCad();
                if (!host.ModelView.CadAssistantBar.IsVisible) host.ModelView.ToggleCadAssistantBar();
                Settle(window);
                AssertBrushesResolve(host.ModelView.CadAssistantBar, variant, requiredBrushKeys, "AssistantPromptBar");

                var settingsDlg = new AssistantSettingsDialog(controller.Assistant) { RequestedThemeVariant = variant };
                try
                {
                    settingsDlg.Show(window);
                    Settle(settingsDlg);
                    AssertBrushesResolve(settingsDlg, variant, requiredBrushKeys, "AssistantSettingsDialog");
                }
                finally { settingsDlg.Close(); Settle(window); }

                var consentDlg = new AssistantConsentPreviewDialog(
                    controller.Assistant,
                    prepared,
                    new AssistantValidationInput(AssistantArea.Cad, AssistantProposalKind.GeometryEdit))
                {
                    RequestedThemeVariant = variant
                };
                try
                {
                    consentDlg.Show(window);
                    Settle(consentDlg);
                    AssertBrushesResolve(consentDlg, variant, requiredBrushKeys, "AssistantConsentPreviewDialog");
                }
                finally { consentDlg.Close(); Settle(window); }
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckCopy866To915AndCommandTable()
    {
        var settingsRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "view.settings")
            ?? throw new InvalidOperationException("CommandTable missing view.settings");
        Require(settingsRow.Menu == "View" && settingsRow.Modal,
            "view.settings must be in View menu with Modal: true");
        Require(ShellHost.IsPaneCommand("view.settings"),
            "view.settings must be routed via ShellHost.IsPaneCommand");

        string designPath = FindRepoFile("DESIGN.md");
        string designText = File.ReadAllText(designPath);
        var rowRegex = new Regex(@"^\|\s*COPY-(\d+)\s*\|\s*(.+?)\s*\|\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var hexRegex = new Regex(@"#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b", RegexOptions.CultureInvariant);
        var foundIds = new HashSet<int>();
        var errors = new List<string>();
        foreach (Match m in rowRegex.Matches(designText))
        {
            int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if (id is < 866 or > 915) continue;
            foundIds.Add(id);
            string body = m.Groups[2].Value;
            if (!body.Contains(" — approved —", StringComparison.Ordinal) &&
                !body.Contains(" — proposed —", StringComparison.Ordinal))
                errors.Add($"COPY-{id} must be marked '— approved —' (Ruling 278) or '— proposed —'");
            if (id == 886 && body.Contains("A8.6", StringComparison.OrdinalIgnoreCase))
                errors.Add("R2-15: COPY-886 must not expose internal 'A8.6' spec ID");
            if (hexRegex.IsMatch(body))
                errors.Add($"COPY-{id} contains a raw hex color");
            foreach (string forbidden in Lab01Forbidden)
            {
                if (Regex.IsMatch(body, $@"\b{Regex.Escape(forbidden)}\b", RegexOptions.IgnoreCase))
                    errors.Add($"COPY-{id} contains LAB-01 forbidden word '{forbidden}'");
            }
        }
        foreach (int id in new[] { 584, 585, 630 })
        {
            var m = Regex.Match(designText, $@"^\|\s*COPY-{id}\s*\|\s*(.+?)\s*\|\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
            if (!m.Success) errors.Add($"DESIGN.md missing COPY-{id}");
            else if (m.Groups[1].Value.Split(" — ", 2)[0].Contains("certified", StringComparison.OrdinalIgnoreCase))
                errors.Add($"R2-15 / LAB-01: COPY-{id} still contains 'certified'");
        }
        if (!designText.Contains("Ruling 277", StringComparison.Ordinal) || !designText.Contains("ActivityRail", StringComparison.Ordinal))
            errors.Add("R2-27: DESIGN.md §5 must be reconciled with unified ActivityRail shell (Ruling 277)");
        for (int id = 866; id <= 915; id++)
        {
            if (!foundIds.Contains(id))
                errors.Add($"DESIGN.md missing COPY-{id}");
        }
        if (errors.Count > 0)
            throw new InvalidOperationException("Wave 11D DESIGN.md copy check failed: " + string.Join("; ", errors));
    }

    // ---- Wave 12B (W12-B) Red-First Tests (R2-01, R2-03, R2-08, R2-10, R2-11) ----

    private static void CheckW12BCadAssistantReachable()
    {
        // R2-01 (C3, C6): CAD Assistant trigger button in ModelArea.Navbar + CommandTable assistant.cad (Modal: false).
        var cadRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "assistant.cad")
            ?? throw new InvalidOperationException("R2-01: CommandTable missing 'assistant.cad'");
        Require(cadRow.Menu == "View" && !cadRow.Modal,
            $"R2-01: assistant.cad must be in View menu with Modal: false, got Menu='{cadRow.Menu}' Modal={cadRow.Modal}");
        Require(!cadRow.Title.Contains("Analysis", StringComparison.OrdinalIgnoreCase),
            $"C3 violated: assistant.cad title must not contain 'Analysis', got '{cadRow.Title}'");
        Require(ShellHost.IsPaneCommand("assistant.cad"),
            "C3 violated: assistant.cad must be routed via ShellHost.IsPaneCommand");

        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // Collapsed by default (C6).
            Require(!host.ModelView.CadAssistantBar.IsVisible, "CadAssistantBar must default collapsed (IsVisible == false)");

            // Discoverable toolbar button in ModelArea.Navbar wired to ToggleCadAssistantBar.
            var navBtn = host.ModelView.FindControl<Button>("NavCadAssistantButton")
                ?? throw new InvalidOperationException("R2-01: ModelArea.Navbar missing NavCadAssistantButton");
            Require(navBtn.IsEffectivelyVisible && navBtn.IsEnabled,
                "R2-01: NavCadAssistantButton must be visible and enabled in CAD views");
            Click(navBtn);
            Settle(window);
            Require(host.ModelView.CadAssistantBar.IsVisible,
                "R2-01: clicking NavCadAssistantButton must reveal CadAssistantBar");

            // Auto-collapse when leaving CAD (e.g., entering Setup or Analysis).
            controller.EnterSetup();
            Settle(window);
            Require(!host.ModelView.CadAssistantBar.IsVisible,
                "R2-01: CadAssistantBar must auto-collapse when leaving CAD");

            // CommandTable assistant.cad switches back to CAD and opens CadAssistantBar.
            Pump(host.RunCommand("assistant.cad"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Cad && host.ModelView.CadAssistantBar.IsVisible,
                "R2-01: RunCommand('assistant.cad') must switch to CAD and reveal CadAssistantBar");

            // Toggle off via assistant.cad in CAD.
            Pump(host.RunCommand("assistant.cad"));
            Settle(window);
            Require(!host.ModelView.CadAssistantBar.IsVisible,
                "R2-01: second RunCommand('assistant.cad') in CAD must collapse CadAssistantBar");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckW12BBaseDriftRefresh()
    {
        // R2-03 (C5 — Security HARD VETO): Refresh over current revision re-prepares Step 1 Consent Preview
        // over the new AcceptedId & fresh AI-04 payloadSha256 — NEVER forging BaseAcceptedId.
        const string testKey = "sk-ant-w12b-drift-key-11223344";
        using var server = new LoopbackAnthropicServer();
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            Pump(controller.Assistant.SaveSettingsAsync(AssistantSettings.CreateDefault() with
            {
                ProviderKind = AssistantProviderKind.Anthropic,
                EndpointUrl = server.BaseUrl,
                ModelId = "claude-w12b-drift"
            }));
            Require(controller.Assistant.SaveKey(AssistantProviderKind.Anthropic, testKey).Succeeded, "Save key failed");
            Require(Pump(controller.Assistant.RunEvaluationAsync(liveWire: false)).Eligibility.ProposalsEnabled, "Eval failed");

            string oldAcceptedId = controller.Inspection!.Authored.Binding.AcceptedId
                ?? throw new InvalidOperationException("No initial AcceptedId");

            async Task<(PreparedAssistantCall Prepared, AssistantValidationInput ValidationInput)?> PrepareCurrentAsync()
            {
                string? curId = controller.Inspection?.Authored.Binding.AcceptedId;
                var prep = await controller.Assistant.PreparePreviewAsync(new AssistantPreviewRequest(
                    AssistantArea.Cad,
                    AssistantProposalKind.GeometryEdit,
                    $"Return strict geometry-edit JSON referencing baseAcceptedId '{curId}'. Never emit raw coordinates or meshes.",
                    "Narrow mid-span trailing edge by 2 mm"));
                if (!prep.Allowed || prep.Prepared is null) return null;
                return (prep.Prepared, new AssistantValidationInput(
                    AssistantArea.Cad,
                    AssistantProposalKind.GeometryEdit,
                    ProposalId: prep.Prepared.RequestId,
                    CurrentAcceptedId: curId));
            }

            var initial = Pump(PrepareCurrentAsync()) ?? throw new InvalidOperationException("Initial preview failed");
            string initialSha = initial.Prepared.Payload.PayloadSha256;

            // Drift the accepted revision BEFORE the model proposal is accepted!
            Pump(controller.ApplySpanAsync("1040"));
            Settle(window);
            string newAcceptedId = controller.Inspection!.Authored.Binding.AcceptedId
                ?? throw new InvalidOperationException("No post-edit AcceptedId");
            Require(!string.Equals(oldAcceptedId, newAcceptedId, StringComparison.Ordinal),
                "Expected span edit to advance AcceptedId");

            // Model answers with the stale oldAcceptedId -> BaseDriftBanner must appear and Accept must be disabled.
            server.EnqueueJson(
                $$"""{"kind":"geometry-edit","baseAcceptedId":"{{oldAcceptedId}}","edits":[{"op":"ChannelDelta","channel":"trailing","eta":0.5,"deltaValue":-0.002,"unit":"m"}],"provenance":[{"fieldName":"trailing@0.5","tag":"Stated","valueDisplay":"-2 mm","reason":"User asked to narrow chord"}]}""",
                inputTokens: 100, outputTokens: 60);

            var dlg = new AssistantConsentPreviewDialog(
                controller.Assistant,
                initial.Prepared,
                initial.ValidationInput,
                currentAcceptedIdProvider: () => controller.Inspection?.Authored.Binding.AcceptedId,
                onRefreshOverCurrentRevision: PrepareCurrentAsync);
            try
            {
                dlg.Show(window);
                Settle(dlg);

                var driftedExec = Pump(dlg.SendAsync());
                Settle(dlg);
                Require(driftedExec.Succeeded, $"Expected validated proposal on old revision, got {driftedExec.ErrorMessage}");
                Require(Need<Border>(dlg, "BaseDriftBanner").IsVisible,
                    "R2-03: BaseDriftBanner must be visible when base revision drifted");
                Require(!Need<Button>(dlg, "AcceptButton").IsEnabled,
                    "R2-03: AcceptButton must be disabled on drifted base revision");
                dlg.Accept();
                Require(!dlg.Accepted, "R2-03: Accept() must refuse while base revision drifted");

                // Click "Refresh over current revision" -> must return cleanly to Step 1 Consent Preview over newAcceptedId & fresh SHA-256!
                Click(Need<Button>(dlg, "RefreshBaseButton"));
                // Wait for async refresh callback to settle.
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!Need<StackPanel>(dlg, "ConsentPanel").IsVisible && DateTime.UtcNow < deadline)
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(5);
                }
                Settle(dlg);

                Require(Need<StackPanel>(dlg, "ConsentPanel").IsVisible && !Need<StackPanel>(dlg, "ProposalPanel").IsVisible,
                    "R2-03 (C5): Refresh over current revision must return dialog to Step 1 Consent Preview");
                string refreshedPreview = Need<TextBox>(dlg, "PayloadPreviewBox").Text ?? "";
                Require(refreshedPreview.Contains(newAcceptedId, StringComparison.Ordinal),
                    "R2-03 (C5): Refreshed Step 1 preview must carry the new AcceptedId");
                string refreshedSha = PromptRedactor.ComputeUtf8Sha256Hex(refreshedPreview);
                Require(!string.Equals(initialSha, refreshedSha, StringComparison.Ordinal),
                    "R2-03 (C5): Refreshed Step 1 preview must bind a fresh AI-04 PayloadSha256");
                Require(Need<Button>(dlg, "SendButton").IsVisible && Need<Button>(dlg, "SendButton").IsEnabled,
                    "R2-03 (C5): SendButton must be visible and enabled for re-consent");

                // Re-send over the fresh revision -> AcceptButton enables and commits cleanly!
                server.EnqueueJson(
                    $$"""{"kind":"geometry-edit","baseAcceptedId":"{{newAcceptedId}}","edits":[{"op":"ChannelDelta","channel":"trailing","eta":0.5,"deltaValue":-0.002,"unit":"m"}],"provenance":[{"fieldName":"trailing@0.5","tag":"Stated","valueDisplay":"-2 mm","reason":"User asked to narrow chord"}]}""",
                    inputTokens: 105, outputTokens: 65);
                var freshExec = Pump(dlg.SendAsync());
                Settle(dlg);
                Require(freshExec.Succeeded && freshExec.Validation?.GeometryEdit?.BaseAcceptedId == newAcceptedId,
                    "R2-03: Fresh execution must carry the real newAcceptedId (never forged)");
                Require(!Need<Border>(dlg, "BaseDriftBanner").IsVisible,
                    "R2-03: BaseDriftBanner must clear after re-consenting over current revision");
                Require(Need<Button>(dlg, "AcceptButton").IsEnabled,
                    "R2-03: AcceptButton must re-enable after refreshing over current revision");
                dlg.Accept();
                Require(dlg.Accepted, "R2-03: Accept() must succeed after refreshing over current revision");
                var commit = Pump(controller.ApplyAssistantGeometryEditAsync(freshExec.Validation!.GeometryEdit!));
                Require(commit is CommitOutcome.Committed, $"R2-03: Applying refreshed edit failed: {commit}");
            }
            finally
            {
                dlg.Close();
                Settle(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckW12BBusyCancelProvenanceRecovery()
    {
        // R2-11 (C6): Busy indicator + CancellationTokenSource wired to CancelButton during SendAsync,
        // wrapped Before -> After provenance DataTemplate, and Retry / Edit prompt recovery on errors.
        const string testKey = "sk-ant-w12b-busy-key-55667788";
        using var server = new LoopbackAnthropicServer();
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            Pump(controller.Assistant.SaveSettingsAsync(AssistantSettings.CreateDefault() with
            {
                ProviderKind = AssistantProviderKind.Anthropic,
                EndpointUrl = server.BaseUrl,
                ModelId = "claude-w12b-busy"
            }));
            Require(controller.Assistant.SaveKey(AssistantProviderKind.Anthropic, testKey).Succeeded, "Save key failed");
            Require(Pump(controller.Assistant.RunEvaluationAsync(liveWire: false)).Eligibility.ProposalsEnabled, "Eval failed");

            var prep = Pump(controller.Assistant.PreparePreviewAsync(
                new AssistantPreviewRequest(AssistantArea.Setup, AssistantProposalKind.SetupSeed, "Return setup-seed JSON.", "Surf foil 80 kg")));
            Require(prep.Allowed && prep.Prepared is not null, "Preview failed");

            // 1. Busy state + live cancellation via CancelButton on delayed response.
            server.EnqueueJson("""{"kind":"setup-seed","purpose":"surf"}""", delayMs: 4000);
            var cancelDlg = new AssistantConsentPreviewDialog(
                controller.Assistant,
                prep.Prepared!,
                new AssistantValidationInput(AssistantArea.Setup, AssistantProposalKind.SetupSeed));
            try
            {
                cancelDlg.Show(window);
                Settle(cancelDlg);

                var sendTask = cancelDlg.SendAsync();
                var inFlightDeadline = DateTime.UtcNow.AddSeconds(5);
                while (server.RequestsReceived < 1 && DateTime.UtcNow < inFlightDeadline)
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(5);
                }
                Require(server.RequestsReceived >= 1, "R2-11: Expected loopback server to receive in-flight request before cancellation");
                var busyBanner = cancelDlg.FindControl<Border>("BusyBanner")
                    ?? throw new InvalidOperationException("R2-11: AssistantConsentPreviewDialog missing BusyBanner");
                var busyText = cancelDlg.FindControl<TextBlock>("BusyStatusText")
                    ?? throw new InvalidOperationException("R2-11: AssistantConsentPreviewDialog missing BusyStatusText");
                Require(busyBanner.IsVisible && busyText.Text!.Contains("Sending to model", StringComparison.Ordinal),
                    $"R2-11: BusyBanner must show 'Sending to model…' while SendAsync is in flight, got visible={busyBanner.IsVisible} text='{busyText.Text}'");

                // Click CancelButton to cancel the in-flight request!
                Click(Need<Button>(cancelDlg, "CancelButton"));
                var cancelled = Pump(sendTask);
                Settle(cancelDlg);
                Require(!cancelled.Succeeded && cancelled.ErrorCode == "ASSISTANT-CANCELLED",
                    $"R2-11: Clicking CancelButton during SendAsync must cancel cleanly with ASSISTANT-CANCELLED, got {cancelled.ErrorCode}");
                Require(!busyBanner.IsVisible, "R2-11: BusyBanner must hide once cancelled");

                // Error recovery buttons (Retry & Edit prompt) must appear alongside Discard!
                var retryBtn = cancelDlg.FindControl<Button>("RetryButton")
                    ?? throw new InvalidOperationException("R2-11: AssistantConsentPreviewDialog missing RetryButton");
                var editBtn = cancelDlg.FindControl<Button>("EditPromptButton")
                    ?? throw new InvalidOperationException("R2-11: AssistantConsentPreviewDialog missing EditPromptButton");
                Require(retryBtn.IsVisible && editBtn.IsVisible,
                    "R2-11: RetryButton and EditPromptButton must be visible on cancelled/failed responses");

                // 2. Retry succeeds with a valid proposal -> wrapped Before -> After provenance DataTemplate rendered!
                server.EnqueueJson(
                    """{"kind":"setup-seed","purpose":"surf","softTargets":{"maxSpanMeters":0.95,"maxSpanWeight":1.0,"maxChordMm":145,"maxChordWeight":1.0,"targetAreaCm2":1150,"targetAreaWeight":1.0,"targetAspectRatio":8.2,"targetAspectRatioWeight":1.0},"riderMassKg":80,"isSaltWater":true,"waterTemperatureC":15,"hRefMeters":0.45,"mastLengthCm":85,"provenance":[{"fieldName":"riderMassKg","tag":"Stated","valueDisplay":"80 kg","reason":"User stated rider mass"},{"fieldName":"waterTemperatureC","tag":"Defaulted","valueDisplay":"15 C","reason":"Preset default"}]}""",
                    inputTokens: 80, outputTokens: 50);
                Click(retryBtn);
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (cancelDlg.ExecutionResult?.Succeeded != true && DateTime.UtcNow < deadline)
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(5);
                }
                Settle(cancelDlg);
                Require(cancelDlg.ExecutionResult?.Succeeded == true,
                    $"R2-11: RetryButton must re-run SendAsync cleanly (err={cancelDlg.ExecutionResult?.ErrorCode}:{cancelDlg.ExecutionResult?.ErrorMessage})");
                var provList = Need<ItemsControl>(cancelDlg, "ProvenanceRowsList");
                Require(provList.ItemTemplate is not null, "R2-11: ProvenanceRowsList must use a wrapped structured DataTemplate");
                var provStrings = provList.ItemsSource?.Cast<object>().Select(x => x.ToString() ?? "").ToArray() ?? [];
                Require(provStrings.Any(s => s.Contains("Stated", StringComparison.Ordinal) && s.Contains("→", StringComparison.Ordinal)) &&
                        provStrings.Any(s => s.Contains("Defaulted", StringComparison.Ordinal)),
                    $"R2-11: Provenance rows must include Stated/Defaulted and Before → After arrow, got: {string.Join(" | ", provStrings)}");
            }
            finally
            {
                cancelDlg.Close();
                Settle(window);
            }

            // 3. Edit prompt button sets EditPromptRequested on Withheld/Refused response.
            server.EnqueueJson("""{"kind":"invalid-garbage"}""");
            var recoverDlg = new AssistantConsentPreviewDialog(
                controller.Assistant,
                prep.Prepared!,
                new AssistantValidationInput(AssistantArea.Setup, AssistantProposalKind.SetupSeed));
            try
            {
                recoverDlg.Show(window);
                Settle(recoverDlg);
                var refused = Pump(recoverDlg.SendAsync());
                Settle(recoverDlg);
                Require(!refused.Succeeded, "Expected refused/withheld response");
                var editBtn = Need<Button>(recoverDlg, "EditPromptButton");
                Require(editBtn.IsVisible && Need<Button>(recoverDlg, "RetryButton").IsVisible,
                    "R2-11: Refused/Withheld responses must show RetryButton and EditPromptButton");
                Click(editBtn);
                Settle(window);
                Require(recoverDlg.EditPromptRequested && !recoverDlg.Accepted,
                    "R2-11: Clicking EditPromptButton must set EditPromptRequested = true and leave Accepted = false");
            }
            finally
            {
                recoverDlg.Close();
                Settle(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckW12BSettingsLayoutAndVerdict()
    {
        // R2-10 (C6): Short EndpointUrlLabel ("Endpoint URL") eliminating 520 px horizontal blowout,
        // wrapped DataTemplates on EvalItemsList/AuditEntriesList, verdict-colored EvalStatusBanner border,
        // and eval busy/cancel controls.
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            var dlg = new AssistantSettingsDialog(controller.Assistant);
            try
            {
                dlg.Show(window);
                Settle(dlg);

                var endpointLabel = Need<TextBlock>(dlg, "EndpointUrlLabelText");
                Require(endpointLabel.Text == "Endpoint URL",
                    $"R2-10: EndpointUrlLabelText must be short 'Endpoint URL' (not 92-char blowout), got '{endpointLabel.Text}'");
                Require(endpointLabel.Bounds.Width < 200,
                    $"R2-10: EndpointUrlLabelText column width must not blow out dialog (got {endpointLabel.Bounds.Width:F1} px)");
                var helperText = dlg.FindControl<TextBlock>("EndpointHelperText")
                    ?? throw new InvalidOperationException("R2-10: AssistantSettingsDialog missing wrapped EndpointHelperText caption");
                Require(helperText.TextWrapping == TextWrapping.Wrap && helperText.Text!.Contains("HTTPS", StringComparison.Ordinal),
                    $"R2-10: EndpointHelperText must wrap and carry HTTPS/loopback guidance, got '{helperText.Text}'");

                var evalList = Need<ItemsControl>(dlg, "EvalItemsList");
                var auditList = Need<ItemsControl>(dlg, "AuditEntriesList");
                Require(evalList.ItemTemplate is not null && auditList.ItemTemplate is not null,
                    "R2-10: EvalItemsList and AuditEntriesList must use wrapped structured DataTemplates");

                // EvalStatusBanner verdict border classes: unevaluated (neither pass nor fail) -> pass (.eval-pass) -> fail (.eval-fail).
                var banner = Need<Border>(dlg, "EvalStatusBanner");
                Require(!banner.Classes.Contains("eval-pass") && !banner.Classes.Contains("eval-fail"),
                    "R2-10: Unevaluated EvalStatusBanner must start in warning state (no eval-pass/eval-fail class)");

                Require(dlg.FindControl<Border>("EvalBusyBanner") is not null &&
                        dlg.FindControl<Button>("CancelEvalButton") is not null,
                    "R2-11: AssistantSettingsDialog must provide EvalBusyBanner and CancelEvalButton");

                controller.Assistant.SaveKey(AssistantProviderKind.Anthropic, "sk-ant-w12b-settings-123456");
                Pump(dlg.RunEvaluationNowAsync());
                Settle(dlg);
                Require(banner.Classes.Contains("eval-pass") && !banner.Classes.Contains("eval-fail"),
                    "R2-10: Passing evaluation must apply .eval-pass (PrimaryBrush border) to EvalStatusBanner");

                // Inject a cryptographically valid failing evaluation receipt (all 10 suite items, item 0 failing)
                // to verify .eval-fail (DangerBrush border).
                var failingItems = AssistantEvalSuite.GetSuiteDefinition()
                    .Select((def, idx) => def.Reserved
                        ? new AssistantEvalItemResult(def.ItemId, def.Title, 0, 0, 0.0, def.Threshold, true, false)
                        : idx == 0
                            ? new AssistantEvalItemResult(def.ItemId, def.Title, 0, def.Cases.Count, 0.0, def.Threshold, false, false)
                            : new AssistantEvalItemResult(def.ItemId, def.Title, def.Cases.Count, def.Cases.Count, 1.0, def.Threshold, false, true))
                    .ToArray();
                var failedReceipt = AssistantEvalSuite.BuildReceipt(
                    controller.Assistant.CurrentSettings.ProviderKind,
                    controller.Assistant.CurrentSettings.EndpointUrl,
                    controller.Assistant.CurrentSettings.ModelId,
                    DateTimeOffset.UtcNow.AddSeconds(5),
                    failingItems);
                Pump(controller.Assistant.SaveSettingsAsync(controller.Assistant.CurrentSettings with
                {
                    EvalReceipts = [failedReceipt]
                }));
                Pump(dlg.RefreshFromStoreAsync());
                Settle(dlg);
                Require(banner.Classes.Contains("eval-fail") && !banner.Classes.Contains("eval-pass"),
                    $"R2-10: Failing evaluation must apply .eval-fail (DangerBrush border) to EvalStatusBanner (status='{NeedText(dlg, "EvalStatusBadgeText")}')");

                // R2-11: Pre-cancelled CancellationToken cancels RunEvaluationNowAsync cleanly without throwing.
                using var preCancelled = new CancellationTokenSource();
                preCancelled.Cancel();
                var cancelEvalRes = Pump(dlg.RunEvaluationNowAsync(preCancelled.Token));
                Settle(dlg);
                Require(cancelEvalRes is null && Need<Button>(dlg, "RunEvalButton").IsEnabled && !Need<Border>(dlg, "EvalBusyBanner").IsVisible,
                    "R2-11: Cancelled RunEvaluationNowAsync must return null, hide EvalBusyBanner, and re-enable RunEvalButton");
            }
            finally
            {
                dlg.Close();
                Settle(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckW12BUnifiedAreasAndCapChip()
    {
        // R2-08 (C6):
        // 1. ShellHost.OnAssistantStateChanged refreshes SetupView immediately without leaving Setup.
        // 2. AnalysisPanel AskCalculationButton toggles collapsed-by-default prompt + inline answer card (5 tabs & plot >= 82 px at rest).
        // 3. ResultsWorkspaceView Ask heading dynamically reflects live BYOK vs offline + metered cap chip.
        // 4. AssistantPromptBar & RunWorkspaceView surface metered daily cap chip ("Used X/Y today").
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // 1. Stay in Setup while saving key & passing eval -> LanguagePromptBox must enable IMMEDIATELY without leaving Setup!
            controller.EnterSetup();
            Settle(window);
            var setupView = host.ModelView.SetupView;
            Require(!Need<TextBox>(setupView, "LanguagePromptBox").IsEnabled, "Setup prompt starts disabled without key");
            controller.Assistant.SaveKey(AssistantProviderKind.Anthropic, "sk-ant-w12b-unified-112233");
            Pump(controller.Assistant.RunEvaluationAsync(liveWire: false));
            Settle(window);
            Require(Need<TextBox>(setupView, "LanguagePromptBox").IsEnabled && Need<Button>(setupView, "LanguageSubmitButton").IsEnabled,
                "R2-08: ShellHost.OnAssistantStateChanged must refresh SetupView immediately when key/eval changes without leaving Setup");

            // Metered daily cap chip on AssistantPromptBar (CAD & Experiment) and Run.
            controller.EnterCad();
            host.ModelView.ToggleCadAssistantBar();
            Settle(window);
            var cadUsageChip = host.ModelView.CadAssistantBar.FindControl<TextBlock>("UsageChipText");
            Require(cadUsageChip is not null && cadUsageChip.Text == "Used 0/50 today",
                $"R2-08: AssistantPromptBar must show metered daily cap chip 'Used 0/50 today', got '{cadUsageChip?.Text}'");

            controller.EnterRun();
            Settle(window);
            var runUsageChip = host.ModelView.RunView.FindControl<TextBlock>("RunAssistantUsageChipText");
            Require(runUsageChip is not null && runUsageChip.Text == "Used 0/50 today",
                $"R2-08: RunWorkspaceView must surface metered daily cap chip, got '{runUsageChip?.Text}'");

            // 2. ResultsWorkspaceView Ask card title dynamically reflects live BYOK vs offline!
            controller.EnterResults();
            Settle(window);
            var resultsHeading = host.ModelView.ResultsView.FindControl<TextBlock>("AskResultHeadingText")
                ?? throw new InvalidOperationException("R2-08: ResultsWorkspaceView missing AskResultHeadingText");
            Require(!resultsHeading.Text!.Contains("(offline)", StringComparison.OrdinalIgnoreCase) &&
                    resultsHeading.Text.Contains("BYOK", StringComparison.OrdinalIgnoreCase),
                $"R2-08: Results Ask heading must reflect live BYOK when ready (not stuck saying '(offline)'), got '{resultsHeading.Text}'");
            var resultsUsage = host.ModelView.ResultsView.FindControl<TextBlock>("AskResultUsageChipText");
            Require(resultsUsage is not null && resultsUsage.Text == "Used 0/50 today",
                $"R2-08: ResultsWorkspaceView must surface metered daily cap chip, got '{resultsUsage?.Text}'");

            // Removing key immediately flips Results heading back to "(offline)" and disables Setup prompt!
            controller.Assistant.RemoveKey(AssistantProviderKind.Anthropic);
            Settle(window);
            Require(resultsHeading.Text!.Contains("(offline)", StringComparison.OrdinalIgnoreCase),
                $"R2-08: Results Ask heading must return to '(offline)' after key removal, got '{resultsHeading.Text}'");
            controller.EnterSetup();
            Settle(window);
            Require(!Need<TextBox>(setupView, "LanguagePromptBox").IsEnabled,
                "R2-08: Removing key must immediately disable SetupView LanguagePromptBox");

            // 3. AnalysisPanel: collapsed-by-default drawer, AskCalculationButton toggles prompt + inline answer card,
            // preserving exact 5 tabs!
            controller.EnterCad();
            controller.ToggleAnalysis();
            host.RefreshPanes();
            Settle(window);
            var panel = host.AnalysisPanel;
            var drawer = panel.FindControl<Border>("AnalysisAssistantDrawer")
                ?? throw new InvalidOperationException("R2-08: AnalysisPanel missing collapsed-by-default AnalysisAssistantDrawer");
            Require(!drawer.IsVisible, "C6: AnalysisAssistantDrawer must be collapsed by default");
            var tabs = panel.FindControl<TabControl>("PanelTabs")!;
            Require(string.Join(",", tabs.Items.OfType<TabItem>().Select(t => t.Header)) == "Spanwise loading,Section,Loads,Provenance,Compare",
                "C6: AnalysisPanel must preserve exact 5 TabItem headers");

            Click(Need<Button>(panel, "AskCalculationButton"));
            Settle(window);
            Require(drawer.IsVisible, "R2-08: Clicking AskCalculationButton must open inline AnalysisAssistantDrawer");
            var anaBar = panel.FindControl<AssistantPromptBar>("AnalysisAssistantBar")
                ?? throw new InvalidOperationException("R2-08: AnalysisAssistantDrawer missing AnalysisAssistantBar");
            Require(anaBar.IsEffectivelyVisible, "R2-08: AnalysisAssistantBar must be visible when drawer is open");
            panel.ShowAssistantAnswer("CL is 0.42 at 5.14 m/s.", ["run-a1"]);
            Settle(window);
            var ansCard = Need<Border>(panel, "AnalysisAnswerCard");
            Require(ansCard.IsVisible && NeedText(panel, "AnalysisAnswerText").Contains("CL is 0.42", StringComparison.Ordinal),
                "R2-08: AnalysisPanel must render assistant answers in inline scrollable AnalysisAnswerCard");

            Click(Need<Button>(panel, "AskCalculationButton"));
            Settle(window);
            Require(!drawer.IsVisible, "R2-08: Second AskCalculationButton click must collapse AnalysisAssistantDrawer");
        }
        finally
        {
            window.Close();
        }
    }

    private static void VerifyDialogChrome200Pct(string name, Window dlg, Window owner)
    {
        bool bounded = !double.IsInfinity(dlg.MaxHeight) && dlg.MaxHeight <= 700 && dlg.Bounds.Height <= 700.5;
        var scroller = dlg.GetVisualDescendants().OfType<ScrollViewer>()
            .FirstOrDefault(s => s.VerticalScrollBarVisibility == ScrollBarVisibility.Auto);
        var footerButtons = dlg.GetVisualDescendants().OfType<Button>()
            .Where(b => b.IsEffectivelyVisible && (scroller is null || !scroller.IsVisualAncestorOf(b)))
            .ToArray();
        bool buttonsOk = footerButtons.Length > 0 && footerButtons.All(b =>
        {
            var o = b.TranslatePoint(default, dlg);
            return o is { } p && p.Y >= -0.5 && p.Y + b.Bounds.Height <= dlg.Bounds.Height + 0.5 && b.Bounds.Height >= 24;
        });
        Require(bounded && scroller is not null && buttonsOk,
            $"DialogChrome 200% check failed for {name}: height={dlg.Bounds.Height:F1}, maxHeight={dlg.MaxHeight:F1}, scroller={scroller is not null}, footerButtons={footerButtons.Length}, buttonsOk={buttonsOk}");
    }

    private static void AssertBrushesResolve(Control control, ThemeVariant variant, string[] keys, string surface)
    {
        foreach (string key in keys)
        {
            bool found = control.TryFindResource(key, variant, out var value);
            Require(found && value is ISolidColorBrush brush && brush.Color.A > 0,
                $"CF-W7-1 theme brush failed on {surface} ({variant.Key}): key '{key}' did not resolve to an opaque ISolidColorBrush");
        }
    }

    private static void AssertNoRunButton(Control root)
    {
        foreach (var btn in root.GetLogicalDescendants().OfType<Button>())
        {
            string label = (btn.Content?.ToString() ?? "").Trim();
            string a11y = (AutomationProperties.GetName(btn) ?? "").Trim();
            bool forbidden = label.Equals("Run", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Run locally", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Start", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Execute", StringComparison.OrdinalIgnoreCase)
                || a11y.StartsWith("Run ", StringComparison.OrdinalIgnoreCase);
            Require(!forbidden, $"XS-03 violation: found Run button ('{label}' / '{a11y}') inside ExperimentSetupView");
        }
    }

    private static void AssertLab01Clean(Control root, string surface)
    {
        var parts = new List<string>();
        foreach (var node in root.GetLogicalDescendants())
        {
            if (node is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text)) parts.Add(tb.Text);
            else if (node is ContentControl cc && cc.Content is string s && !string.IsNullOrWhiteSpace(s)) parts.Add(s);
        }
        string text = string.Join(" | ", parts);
        foreach (string word in Lab01Forbidden)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException($"LAB-01 violation on {surface}: found forbidden word '{word}'");
        }
    }

    private static CfdRunRecord BuildFailedCfdRun(SessionView snap)
    {
        string runId = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var op = OperatingPoints.Custom(7.2, 6.0, 0.50);
        var reference = new RunReference(0.10, 1.0, 0.12, "root-le", "x-aft,y-stbd,z-up");
        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, [new string('c', 64)], "cfdw-cv/2", "rule-a/1");
        var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720-20260127", new string('e', 64), true);
        var meshGate = new MeshGateResult(
            false, "wall-resolved", 74.5, 1.8, 3.4, 1.2e-11, 0.042, 0.68, 0.985, 1.0,
            [new RegionLayerCoverage("leading-edge", 20, 0.99, 1.0)], 0.62, 1.38, false,
            [new MeshGateFailure("MaxNonOrthogonalityDeg", 74.5, 70.0, "Max non-orthogonality 74.5 deg exceeds 70 deg threshold.")]);
        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, now)];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now.AddSeconds(1));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now.AddSeconds(2));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now.AddSeconds(3));
        events = CfdRunStateMachine.TransitionToFailed(events, now.AddSeconds(4), "MESH-GATE-FAIL", "Max non-orthogonality 74.5 deg exceeds 70 deg threshold.");
        string caseHash = new string('f', 64);
        string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, caseHash);
        return CfdRunRecords.Seal(new CfdRunRecord(
            runId, null, null, 0, runKey, "", caseHash, new string('a', 64), pin, inputs, water, op, reference,
            meshGate, null, new CfdRunResources(1_840_000, 4, 142_300, 512.4, 2_147_483_648), null, events));
    }

    private static CfdRunRecord BuildCompletedCfdRunNoTauW(SessionView snap)
    {
        string runId = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var op = OperatingPoints.Custom(7.2, 2.0, 0.50);
        var reference = new RunReference(0.10, 1.0, 0.12, "root-le", "x-aft,y-stbd,z-up");
        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, [new string('c', 64)], "cfdw-cv/2", "rule-a/1");
        var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720-20260127", new string('e', 64), true);
        var meshGate = new MeshGateResult(
            true, "wall-resolved", 42.3, 1.8, 3.4, 1.2e-11, 0.042, 0.68, 0.985, 1.0,
            [new RegionLayerCoverage("leading-edge", 20, 0.99, 1.0)], 0.62, 1.38, false, []);
        var residuals = new ResidualSummary(4.2e-4, 0.412, 0.0184, -0.041, 1.8e-4, 4.5e-5, 1.0e-3, 2.0e-4, 200, 0, false, true);
        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, now)];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now.AddSeconds(1));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now.AddSeconds(2));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now.AddSeconds(3));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now.AddSeconds(4));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now.AddSeconds(5));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, now.AddSeconds(6));
        string caseHash = new string('f', 64);
        string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, caseHash);
        return CfdRunRecords.Seal(new CfdRunRecord(
            runId, null, null, 0, runKey, "", caseHash, new string('a', 64), pin, inputs, water, op, reference,
            meshGate, residuals, new CfdRunResources(1_840_000, 4, 142_300, 512.4, 2_147_483_648), null, events));
    }

    private sealed class LoopbackAnthropicServer : IDisposable
    {
        private readonly HttpListener listener = new();
        private readonly Queue<(string Text, int? InTok, int? OutTok, int DelayMs)> responses = new();
        private readonly CancellationTokenSource cts = new();
        private readonly Task loopTask;
        private int requestsReceived;

        public LoopbackAnthropicServer()
        {
            int port = PickFreeTcpPort();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add(BaseUrl + "/");
            listener.Start();
            loopTask = Task.Run(ServeLoopAsync);
        }

        public string BaseUrl { get; }
        public int RequestsReceived => Volatile.Read(ref requestsReceived);

        public void EnqueueJson(string textJson, int? inputTokens = null, int? outputTokens = null, int delayMs = 0)
        {
            lock (responses) responses.Enqueue((textJson, inputTokens, outputTokens, delayMs));
        }

        private async Task ServeLoopAsync()
        {
            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException) { break; }

                Interlocked.Increment(ref requestsReceived);
                (string text, int? inTok, int? outTok, int delayMs) next;
                lock (responses)
                {
                    next = responses.Count > 0 ? responses.Dequeue() : ("{}", null, null, 0);
                }

                _ = Task.Run(() => HandleContextAsync(ctx, next));
            }
        }

        private async Task HandleContextAsync(
            HttpListenerContext ctx,
            (string Text, int? InTok, int? OutTok, int DelayMs) next)
        {
            if (next.DelayMs > 0)
            {
                try { await Task.Delay(next.DelayMs, cts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }

            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                writer.WriteStartArray("content");
                writer.WriteStartObject();
                writer.WriteString("type", "text");
                writer.WriteString("text", next.Text);
                writer.WriteEndObject();
                writer.WriteEndArray();
                if (next.InTok is not null || next.OutTok is not null)
                {
                    writer.WriteStartObject("usage");
                    if (next.InTok is { } i) writer.WriteNumber("input_tokens", i);
                    if (next.OutTok is { } o) writer.WriteNumber("output_tokens", o);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            byte[] payload = ms.ToArray();
            try
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = payload.Length;
                await ctx.Response.OutputStream.WriteAsync(payload, cts.Token).ConfigureAwait(false);
                ctx.Response.Close();
            }
            catch (HttpListenerException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
        }

        private static int PickFreeTcpPort()
        {
            using var socket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            int port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return port;
        }

        public void Dispose()
        {
            cts.Cancel();
            try { listener.Stop(); } catch { }
            try { listener.Close(); } catch { }
            try { loopTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            cts.Dispose();
        }
    }

    private static void Pump(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    private static T Pump<T>(Task<T> task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        Dispatcher.UIThread.RunJobs();
        return task.GetAwaiter().GetResult();
    }

    private static string NeedText(Control root, string name) => Need<TextBlock>(root, name).Text ?? "";

    private static void Click(Button button) =>
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string FindRepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, relative);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new FileNotFoundException($"Could not find {relative} above {AppContext.BaseDirectory}");
    }
}
