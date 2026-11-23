using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core.SolverSetup;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.SolverSetup;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 8C (W8-SLV-UI) Guided Solver Setup Desktop UI tests:
/// - SETUP-04 no-command copy lint over every SolverSetupCopy constant and DESIGN.md COPY-766..805
/// - All 6 hard states (Survey, Step Ready, Existing Unverified, Failed, Needs Restart, Ready)
/// - DR-SETUP-5 Technical details collapsed by default, revealing exact command + pin on expand
/// - DR-SETUP-1 Other options route switcher (win-wsl-of2512 <-> win-su2-850)
/// - Zero-PII Copy a report output (A8.5)
/// - DR-SETUP-4 / AI-01 / AI-11 key-free offline rails + assistant validator slot
/// - Zero first-launch nagging + View > Set up a solver, StatusStrip Solver item, and AnalysisPanel entry Dock tab wiring
/// </summary>
public static class SolverSetupViewTests
{
    public static void Run()
    {
        DesktopChecks.Check("SolverSetup_CopyLint_EveryConstantAndDesignRow_ZeroCommandsToType", CheckCopyLint);
        DesktopChecks.Check("SolverSetup_AllSixHardStates_RenderCleanly", CheckAllSixHardStates);
        DesktopChecks.Check("SolverSetup_OtherOptions_SwitchesRouteBetweenOpenFoamAndSu2", CheckOtherOptionsRouteSwitch);
        DesktopChecks.Check("SolverSetup_CopyReport_EmitsZeroPiiText", CheckCopyReportZeroPii);
        DesktopChecks.Check("SolverSetup_OfflineAssistant_KeyFreeAndAi11Validator", CheckOfflineAssistantAndValidator);
        DesktopChecks.Check("SolverSetup_ShellWiring_ZeroFirstLaunchNag_MenuStatusStripAnalysisPanelOpenDockTab", CheckShellWiring);
    }

    private static void CheckCopyLint()
    {
        // Negative control: verify SetupCopyLint catches shell commands.
        var planted = SetupCopyLint.Lint("Type wsl --install -d Ubuntu in your terminal.");
        if (planted.Passed)
            throw new InvalidOperationException("SETUP-04 negative control escaped: shell command was not flagged.");

        var allCopy = SolverSetupCopy.AllUserFacingCopy();
        if (allCopy.Count < 35)
            throw new InvalidOperationException($"Expected >= 35 SolverSetupCopy constants, found {allCopy.Count}.");

        var failures = new List<string>();
        foreach (var (name, text) in allCopy)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                failures.Add($"{name}: empty copy constant");
                continue;
            }
            var lint = SetupCopyLint.Lint(text);
            if (!lint.Passed)
                failures.Add($"{name}: {string.Join(", ", lint.Violations.Select(v => $"{v.Rule}({v.Snippet})"))}");
        }

        // Verify DESIGN.md COPY-766..805 exist, are proposed, and pass SETUP-04 no-command lint.
        string designPath = FindRepoFile("DESIGN.md");
        string designText = File.ReadAllText(designPath);
        var rowRegex = new Regex(@"^\|\s*COPY-(\d+)\s*\|\s*(.+?)\s*\|\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var foundIds = new HashSet<int>();
        foreach (Match m in rowRegex.Matches(designText))
        {
            int id = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (id is < 766 or > 805) continue;
            foundIds.Add(id);
            string body = m.Groups[2].Value;
            int split = body.IndexOf(" — approved —", StringComparison.Ordinal);
            if (split < 0) split = body.IndexOf(" — proposed —", StringComparison.Ordinal);
            if (split < 0)
                failures.Add($"DESIGN.md COPY-{id} is not marked '— approved —' or '— proposed —'.");
            string copyText = split > 0 ? body[..split] : body;
            var lint = SetupCopyLint.Lint(copyText);
            if (!lint.Passed)
                failures.Add($"DESIGN.md COPY-{id}: {string.Join(", ", lint.Violations.Select(v => $"{v.Rule}({v.Snippet})"))}");
        }

        for (int id = 766; id <= 805; id++)
        {
            if (!foundIds.Contains(id))
                failures.Add($"DESIGN.md missing COPY-{id}.");
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("SETUP-04 copy lint failures:\n  " + string.Join("\n  ", failures));
    }

    private static void CheckAllSixHardStates()
    {
        string dir = NewScratchDir("slv-states");
        try
        {
            var probe = new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: false, wslVersion: null));
            var runOnce = new InMemoryRunOnceStore();
            var service = new SolverSetupService(dir, probe, runOnce);
            var view = new SolverSetupView();
            var window = new Window { Content = view, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);

                // --- Hard State 1: Survey / RouteSelection (no survey yet -> run read-only check -> route selection) ---
                view.BindService(service);
                Settle(window);
                Require(view.ViewModel.Screen == SetupScreenKind.RouteSelection, "initial screen should be RouteSelection");
                Require(NeedText(view, "HeaderStatusText") == SolverSetupCopy.HeaderNotSetUp,
                    $"expected header '{SolverSetupCopy.HeaderNotSetUp}', got '{NeedText(view, "HeaderStatusText")}'");
                var otherExpander = Need<Expander>(view, "OtherOptionsExpander");
                Require(!otherExpander.IsExpanded, "Other options must be collapsed by default");

                // Run read-only survey via service -> now we have a Windows survey + StepReady (step 2: wsl.enable).
                Pump(view.RunSurveyAsync());
                Settle(window);

                // --- Hard State 2: Step Ready + Technical details collapsed by default (DR-SETUP-5) ---
                Require(view.ViewModel.Screen == SetupScreenKind.StepReady, $"expected StepReady, got {view.ViewModel.Screen}");
                Require(view.ViewModel.CurrentCard?.StepId == "wsl.enable", $"expected wsl.enable, got {view.ViewModel.CurrentCard?.StepId}");
                var techExpander = Need<Expander>(view, "TechnicalDetailsExpander");
                Require(techExpander.IsVisible, "Technical details disclosure must be visible on step card");
                Require(!techExpander.IsExpanded, "Technical details must be collapsed by default (DR-SETUP-5)");
                string techCmd = NeedText(view, "TechnicalCommandText");
                Require(techCmd.Contains("wsl.exe --install --no-distribution", StringComparison.Ordinal),
                    $"Technical details missing exact command: '{techCmd}'");
                techExpander.IsExpanded = true;
                Settle(window);
                Require(techExpander.IsExpanded, "Technical details did not expand");
                Require(Need<TextBlock>(view, "TechnicalCommandText").IsEffectivelyVisible,
                    "Exact command must be visibly rendered when Technical details is expanded");

                // Verify primary consent button records a ConsentRecordFact with exact technical command shown.
                var primaryBtn = Need<Button>(view, "PrimaryActionButton");
                Require(primaryBtn.IsEffectivelyVisible && primaryBtn.IsEnabled, "Primary consent button must be enabled");
                Click(primaryBtn);
                Settle(window);
                var consents = service.Store.ReadAll().Where(f => f.Kind == SetupFactEnvelope.KindConsent).ToList();
                Require(consents.Count == 1 && consents[0].Consent!.StepId == "wsl.enable" &&
                        consents[0].Consent!.TechnicalCommandShown.Contains("wsl.exe --install", StringComparison.Ordinal),
                    "Clicking primary step button must record consent with exact technical command");

                // --- Hard State 3: Needs Restart (DR-SETUP-6) ---
                service.RecordStepOutcome(new EnvironmentStepFact(
                    "step-wsl-restart", view.SessionId, RouteCatalogue.WinWslOpenFoamId, "2512.1.0",
                    "wsl.enable", consents[0].Consent!.ConsentId,
                    DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow,
                    StepOutcome.NeedsRestart, null, 0, true, 0, null, 5000));
                view.RefreshProjection();
                Settle(window);
                Require(view.ViewModel.Screen == SetupScreenKind.NeedsRestart, $"expected NeedsRestart, got {view.ViewModel.Screen}");
                Require(Need<Control>(view, "NeedsRestartPanel").IsVisible, "NeedsRestartPanel must be visible");
                Click(Need<Button>(view, "RestartNowButton"));
                Settle(window);
                Require(runOnce.TryRead(SolverSetupService.RunOnceEntryName) is not null,
                    "RestartNowButton must register one-time RunOnce resume entry via injected store");

                // --- Hard State 4: Failed with Cause + Next Step (known cause with firmware click path) ---
                string failDir = NewScratchDir("slv-fail");
                try
                {
                    var failService = new SolverSetupService(failDir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: false, wslVersion: null, virtEnabled: false, maker: "Lenovo")), new InMemoryRunOnceStore());
                    Pump(failService.RunSurveyAsync());
                    failService.RecordStepOutcome(new EnvironmentStepFact(
                        "step-fail-virt", "sess-fail", RouteCatalogue.WinWslOpenFoamId, "2512.1.0",
                        "wsl.enable", null, DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow,
                        StepOutcome.Failed, "win.virtualization-off", 1, false, 0,
                        "1  WSL error 0x80370102: Virtualization is disabled in firmware.", 2000));
                    view.BindService(failService, sessionId: "sess-fail");
                    Settle(window);
                    Require(view.ViewModel.Screen == SetupScreenKind.FailedKnown, $"expected FailedKnown, got {view.ViewModel.Screen}");
                    Require(Need<Control>(view, "FailurePanel").IsVisible, "FailurePanel must be visible");
                    Require(NeedText(view, "CauseSummaryText").Contains("Virtualization is off", StringComparison.Ordinal),
                        $"unexpected cause summary: '{NeedText(view, "CauseSummaryText")}'");
                    Require(NeedText(view, "NextStepText").Length > 0, "NextStepText must be non-empty");
                    Require(Need<StackPanel>(view, "ClickPathList").Children.Count >= 4,
                        "Firmware click path steps must render for win.virtualization-off");
                }
                finally { DeleteDir(failDir); }

                // --- Hard State 5: Existing Unverified Install (DR-SEC-1 A) ---
                string unverifiedDir = NewScratchDir("slv-unverified");
                try
                {
                    var existing = new ExistingInstallInfo(
                        "OpenFOAM", "v2512", "wsl://custom-of", "deadbeef0123456789", MatchesPin: false, IsSupportedVersion: true);
                    var unverifiedSurvey = MakeWindowsSurvey(wslAvailable: true, wslVersion: 2, existingInstalls: [existing]);
                    var unverifiedService = new SolverSetupService(unverifiedDir, new StubSurveyProbe(unverifiedSurvey), new InMemoryRunOnceStore());
                    Pump(unverifiedService.RunSurveyAsync());
                    view.BindService(unverifiedService, sessionId: "sess-unv");
                    Settle(window);
                    Require(view.ViewModel.Screen == SetupScreenKind.ExistingUnverified,
                        $"expected ExistingUnverified, got {view.ViewModel.Screen}");
                    Require(Need<Control>(view, "ExistingUnverifiedPanel").IsVisible, "ExistingUnverifiedPanel must be visible");
                    Require(Need<Button>(view, "InstallTestedButton").IsEffectivelyVisible, "InstallTestedButton must be visible");
                    var useMine = Need<Button>(view, "UseMineAnywayButton");
                    Require(useMine.IsEffectivelyVisible, "UseMineAnywayButton must be visible");
                    Click(useMine);
                    Settle(window);
                    Require(unverifiedService.Store.ReadAll().Any(f => f.Kind == SetupFactEnvelope.KindAcceptance),
                        "Clicking Use mine anyway must append InstallAcceptanceFact");
                    Require(view.ViewModel.Screen != SetupScreenKind.ExistingUnverified,
                        "View must advance past ExistingUnverified after accepting unverified install");
                }
                finally { DeleteDir(unverifiedDir); }

                // --- Hard State 6: Ready (SETUP-08, M2 Disallowing banner) ---
                string readyDir = NewScratchDir("slv-ready");
                try
                {
                    var readyService = new SolverSetupService(readyDir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: true, wslVersion: 2)), new InMemoryRunOnceStore());
                    Pump(readyService.RunSurveyAsync());
                    const string idHash = "verified-pin-hash-2512";
                    readyService.RecordBackendCheck(new BackendCheckFact(
                        "chk-id", DateTimeOffset.UtcNow.AddSeconds(-6), RouteCatalogue.WinWslOpenFoamId,
                        "OpenFOAM", "v2512", "wsl://cfdw-openfoam2512", idHash, true,
                        BackendCheckKind.Identify, true, "_bd2b6720-20260127", "Disallowing",
                        null, null, null, true, 500, null));
                    readyService.RecordBackendCheck(new BackendCheckFact(
                        "chk-smoke", DateTimeOffset.UtcNow, RouteCatalogue.WinWslOpenFoamId,
                        "OpenFOAM", "v2512", "wsl://cfdw-openfoam2512", idHash, true,
                        BackendCheckKind.Smoke, true, "_bd2b6720-20260127", "Disallowing",
                        "courant_mean", 0.222158, 1e-3, true, 4800, null));
                    view.BindService(readyService, sessionId: "sess-ready");
                    Settle(window);
                    Require(view.ViewModel.Screen == SetupScreenKind.Ready, $"expected Ready, got {view.ViewModel.Screen}");
                    Require(Need<Control>(view, "ReadyPanel").IsVisible, "ReadyPanel must be visible");
                    Require(NeedText(view, "HeaderStatusText") == SolverSetupCopy.HeaderReady,
                        $"expected header '{SolverSetupCopy.HeaderReady}', got '{NeedText(view, "HeaderStatusText")}'");
                    Require(NeedText(view, "ReadySummaryText").Contains("OpenFOAM v2512", StringComparison.Ordinal),
                        $"unexpected Ready summary: '{NeedText(view, "ReadySummaryText")}'");
                }
                finally { DeleteDir(readyDir); }
            }
            finally { window.Close(); }
        }
        finally { DeleteDir(dir); }
    }

    private static void CheckOtherOptionsRouteSwitch()
    {
        string dir = NewScratchDir("slv-route");
        try
        {
            var service = new SolverSetupService(dir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: false, wslVersion: null)), new InMemoryRunOnceStore());
            Pump(service.RunSurveyAsync());
            var view = new SolverSetupView();
            var window = new Window { Content = view, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                view.BindService(service);
                Settle(window);

                Require(view.SelectedRouteId == RouteCatalogue.WinWslOpenFoamId,
                    $"expected default Windows route {RouteCatalogue.WinWslOpenFoamId}, got '{view.SelectedRouteId}'");
                Require(view.ViewModel.OtherRoutes.Any(r => r.Id == RouteCatalogue.WinSu2Id),
                    "Other options must list win-su2-850 on Windows");

                var otherExpander = Need<Expander>(view, "OtherOptionsExpander");
                otherExpander.IsExpanded = true;
                Settle(window);

                var switchBtn = Need<StackPanel>(view, "OtherRoutesList")
                    .GetVisualDescendants().OfType<Button>()
                    .FirstOrDefault(b => Equals(b.Tag, RouteCatalogue.WinSu2Id))
                    ?? throw new InvalidOperationException("Missing route switch button for win-su2-850");
                Click(switchBtn);
                Settle(window);

                Require(view.SelectedRouteId == RouteCatalogue.WinSu2Id,
                    $"route did not switch to {RouteCatalogue.WinSu2Id}, actual '{view.SelectedRouteId}'");
                Require(view.ViewModel.RouteId == RouteCatalogue.WinSu2Id && view.ViewModel.Steps.Count == 4,
                    $"expected 4 SU2 steps after switch, got {view.ViewModel.Steps.Count}");
                Require(view.ViewModel.OtherRoutes.Any(r => r.Id == RouteCatalogue.WinWslOpenFoamId),
                    "OpenFOAM WSL route should now appear under Other options");
            }
            finally { window.Close(); }
        }
        finally { DeleteDir(dir); }
    }

    private static void CheckCopyReportZeroPii()
    {
        string dir = NewScratchDir("slv-report");
        try
        {
            string fakeUser = "FakeDesignerName";
            string fakePath = "C:\\" + "Users\\" + fakeUser + "\\AppData\\Local\\temp";
            string fakeSid = "S-1-5-" + "21-111111111-222222222-333333333-1001";
            string fakeKey = "sk-" + "secret1234567890abcdef";
            var service = new SolverSetupService(dir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: true, wslVersion: 2)), new InMemoryRunOnceStore());
            Pump(service.RunSurveyAsync());
            service.RecordStepOutcome(new EnvironmentStepFact(
                "step-pii", "sess-pii", RouteCatalogue.WinWslOpenFoamId, "2512.1.0",
                "wsl.base.import", null, DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow,
                StepOutcome.Failed, "unknown", 1, false, 0,
                $"Failed at {fakePath} SID {fakeSid} key={fakeKey}",
                1000));

            var view = new SolverSetupView();
            var window = new Window { Content = view, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                view.BindService(service, sessionId: "sess-pii");
                Settle(window);

                Click(Need<Button>(view, "CopyReportButton"));
                Settle(window);

                string report = view.LastCopiedReport ?? "";
                Require(report.Contains("CFD Workbench Solver Setup Report", StringComparison.Ordinal), "Report header missing");
                Require(!report.Contains(fakeUser, StringComparison.OrdinalIgnoreCase), "Report leaked Windows user name!");
                Require(!report.Contains("111111111-222222222", StringComparison.Ordinal), "Report leaked machine SID!");
                Require(!report.Contains(fakeKey, StringComparison.Ordinal), "Report leaked API key!");
                Require(Need<TextBlock>(view, "ReportReceiptText").IsVisible, "Inline Copy a report receipt should be visible");
            }
            finally { window.Close(); }
        }
        finally { DeleteDir(dir); }
    }

    private static void CheckOfflineAssistantAndValidator()
    {
        string dir = NewScratchDir("slv-ai");
        try
        {
            var service = new SolverSetupService(dir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: false, wslVersion: null)), new InMemoryRunOnceStore());
            Pump(service.RunSurveyAsync());
            var view = new SolverSetupView();
            var window = new Window { Content = view, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                view.BindService(service, hasApiKey: false);
                Settle(window);

                // AI-01 / DR-SETUP-4: no key shows offline notice and never disables rails.
                Require(Need<Control>(view, "AssistantOfflineCard").IsVisible, "Offline no-key notice must be visible");
                Require(!Need<TextBox>(view, "AssistantPromptBox").IsEnabled, "Assistant prompt should be disabled without a key");
                Require(Need<Button>(view, "PrimaryActionButton").IsEnabled, "Primary rails button must remain enabled without a key");

                // AI-11: valid explanation with resolvable citation is shown.
                var kbIds = new HashSet<string>(StringComparer.Ordinal) { "kb-wsl" };
                var shown = view.ApplyAssistantResponse(
                    """{"kind":"explanation","text":"WSL is turned off on this PC and needs Windows permission once.","citations":["kb-wsl"]}""",
                    knownKnowledgeIds: kbIds);
                Settle(window);
                Require(shown.Verdict == AssistantVerdict.Shown, $"expected Shown, got {shown.Verdict}: {shown.Message}");
                Require(Need<Control>(view, "AssistantResultCard").IsVisible, "AssistantResultCard must be visible after response");

                // AI-11: command-to-type explanation is withheld and shows fixed fallback.
                var withheld = view.ApplyAssistantResponse(
                    """{"kind":"explanation","text":"Run wsl --install -d Ubuntu to fix it.","citations":["kb-wsl"]}""",
                    knownKnowledgeIds: kbIds);
                Settle(window);
                Require(withheld.Verdict == AssistantVerdict.Withheld && withheld.Reason == "command-to-type",
                    $"expected Withheld(command-to-type), got {withheld.Verdict}({withheld.Reason})");
                Require(NeedText(view, "AssistantVerdictText").Contains("Response withheld", StringComparison.Ordinal),
                    $"expected withheld notice, got '{NeedText(view, "AssistantVerdictText")}'");

                // AI-11: disallowed step id is refused.
                var refused = view.ApplyAssistantResponse("""{"kind":"environment-step","stepId":"of.install"}""");
                Settle(window);
                Require(refused.Verdict == AssistantVerdict.Refused && refused.Reason == "step-not-allowed",
                    $"expected Refused(step-not-allowed), got {refused.Verdict}({refused.Reason})");
            }
            finally { window.Close(); }
        }
        finally { DeleteDir(dir); }
    }

    private static void CheckShellWiring()
    {
        string dir = NewScratchDir("slv-shell");
        try
        {
            var service = new SolverSetupService(dir, new StubSurveyProbe(MakeWindowsSurvey(wslAvailable: false, wslVersion: null)), new InMemoryRunOnceStore());
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            host.BindSolverSetup(service);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);

                // F7a: zero first-launch nagging — SolverSetupDocument is NOT open at startup.
                Require(host.LayoutFactory.MainDocumentDock.VisibleDockables?.Contains(host.SolverSetupDocument) != true,
                    "Solver setup tab must not open automatically on first launch");
                Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument),
                    "Model document must stay active at startup");

                // StatusStrip shows derived Solver: not set up item.
                var solverText = Need<TextBlock>(host.StatusStrip, "SolverItemText");
                Require(solverText.Text == SolverSetupCopy.StripNotSetUp,
                    $"expected '{SolverSetupCopy.StripNotSetUp}', got '{solverText.Text}'");

                // CommandTable + NativeMenuBuilder View > Set up a solver (Modal: false).
                var row = CommandTable.Rows.Single(r => r.Id == "solver.setup");
                Require(row.Title == SolverSetupCopy.VerbTitle && row.Menu == "View" && !row.Modal,
                    $"CommandTable row mismatch: title='{row.Title}' menu='{row.Menu}' modal={row.Modal}");
                var menu = NativeMenuBuilder.BuildMenu(window);
                var viewMenu = menu.Items.OfType<NativeMenuItem>().Single(i => Equals(i.Header, "View")).Menu!;
                Require(viewMenu.Items.OfType<NativeMenuItem>().Any(i => Equals(i.Header, SolverSetupCopy.VerbTitle)),
                    "View menu is missing 'Set up a solver'");

                // Entry point 1: View > Set up a solver command opens non-blocking Dock document tab.
                Pump(host.RunCommand("solver.setup"));
                Settle(window);
                Require(host.LayoutFactory.MainDocumentDock.VisibleDockables!.Contains(host.SolverSetupDocument),
                    "RunCommand('solver.setup') did not add SolverSetupDocument to MainDocumentDock");
                Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.SolverSetupDocument),
                    "RunCommand('solver.setup') did not activate SolverSetupDocument");
                Require(ReferenceEquals(host.SolverSetupDocument.Context, host.SolverSetup),
                    "SolverSetupDocument.Context must be host.SolverSetup");

                // Non-blocking: user can switch back to ModelDocument without closing SolverSetup.
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                Settle(window);
                Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument),
                    "Could not switch back from Solver setup tab to Model document");

                // Entry point 2: StatusStrip Solver button opens/focuses SolverSetupDocument.
                host.CloseSolverSetup();
                Settle(window);
                Require(!host.LayoutFactory.MainDocumentDock.VisibleDockables!.Contains(host.SolverSetupDocument),
                    "CloseSolverSetup did not remove SolverSetupDocument");
                Click(Need<Button>(host.StatusStrip, "SolverButton"));
                Settle(window);
                Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.SolverSetupDocument),
                    "Clicking StatusStrip SolverButton did not open SolverSetupDocument");

                // Entry point 3: AnalysisPanel 'Set up a solver' entry opens/focuses SolverSetupDocument.
                host.CloseSolverSetup();
                Pump(controller.OpenExampleAsync());
                controller.ToggleAnalysis();
                host.RefreshPanes();
                Settle(window);
                var analysisSolverBtn = Need<Button>(host.AnalysisPanel, "SolverSetupButton");
                Require(analysisSolverBtn.IsEffectivelyVisible, "AnalysisPanel SolverSetupButton must be visible in Analysis");
                Click(analysisSolverBtn);
                Settle(window);
                Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.SolverSetupDocument),
                    "Clicking AnalysisPanel SolverSetupButton did not open SolverSetupDocument");

                // StatusStrip derived Solver state transitions.
                host.StatusStrip.ShowSolverState(new SolverStatusSummary(
                    SolverReadiness.SettingUp, "Solver: step 3 of 9", "OpenFOAM", "v2512", null, false, 3, 9, 0.3, null, null, null));
                Require(solverText.Text == "Solver: step 3 of 9", $"expected SettingUp label, got '{solverText.Text}'");
                host.StatusStrip.ShowSolverState(new SolverStatusSummary(
                    SolverReadiness.Ready, SolverSetupCopy.StripReady, "OpenFOAM", "v2512", "_bd2b6720", true, null, null, 1.0, DateTimeOffset.UtcNow, 4000, null));
                Require(solverText.Text == SolverSetupCopy.StripReady, $"expected Ready label, got '{solverText.Text}'");
            }
            finally { window.Close(); }
        }
        finally { DeleteDir(dir); }
    }

    private sealed class StubSurveyProbe(HostSurveyFact fact) : IHostSurveyProbe
    {
        public Task<HostSurveyFact> SurveyAsync(CancellationToken cancellationToken = default) => Task.FromResult(fact);
    }

    private static HostSurveyFact MakeWindowsSurvey(
        bool wslAvailable,
        int? wslVersion,
        bool? virtEnabled = true,
        string? maker = "Dell",
        IReadOnlyList<ExistingInstallInfo>? existingInstalls = null) =>
        new(
            FactId: "surv-" + Guid.NewGuid().ToString("N")[..8],
            AtUtc: DateTimeOffset.UtcNow,
            Os: SetupOs.Windows,
            OsName: "Windows 11 Pro",
            OsBuild: 26300,
            Arch: SetupArch.Arm64,
            FreeDiskBytes: 120L * 1_073_741_824L,
            RebootPending: false,
            VirtualizationFirmwareEnabled: virtEnabled,
            HypervisorPresent: virtEnabled,
            WslAvailable: wslAvailable,
            WslDefaultVersion: wslVersion,
            WslDistros: wslAvailable ? [new WslDistroInfo("Ubuntu-24.04", "Stopped", 2, true)] : [],
            ExistingInstalls: existingInstalls ?? [],
            DeviceMaker: maker,
            ManagedDeviceHint: false,
            ProxyDetected: false,
            DurationMs: 42);

    private static string NeedText(Control root, string name) =>
        Need<TextBlock>(root, name).Text ?? "";

    private static void Click(Button button) =>
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string NewScratchDir(string prefix)
    {
        string path = TestTemp.Combine($"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDir(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
    }

    private static string FindRepoFile(string relative)
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            string candidate = Path.Combine(dir, relative);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        if (File.Exists(relative)) return Path.GetFullPath(relative);
        throw new FileNotFoundException($"Could not locate repo file '{relative}'.");
    }
}
