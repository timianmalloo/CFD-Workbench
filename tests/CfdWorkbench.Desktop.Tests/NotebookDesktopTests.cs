using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Analysis.Notebook;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.Notebook;
using CfdWorkbench.Desktop.Assistant;
using CfdWorkbench.Desktop.Notebook;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 13B Desktop UI &amp; Shell Integration test suite (<c>T-NB-08</c>,
/// <c>docs/design/ai-guided-design-notebook.md §9</c>).
/// Proves all 7 locked mockup states across Light/Dark/HighContrast, <c>COPY-924..934</c> + zero <c>LAB-01</c>,
/// <c>Open in CAD Mode →</c> disabled until <see cref="GeometryStatus.Certified"/>, all 5 arrow-key-navigable
/// artifact tabs reusing existing scientific controls, interactive Grill-Me Q&amp;A + <c>TQ9</c> stale re-open,
/// <see cref="AssistantConsentPreviewDialog"/> <c>EffectiveModelId</c> &amp; <c>ExtractProvenance</c> for both new kinds,
/// <see cref="AssistantSettingsDialog"/> <c>OptionalFastModelId</c> + fast-model eval + dynamic 11-item count,
/// <see cref="CadHandoffDialog"/> appending <c>rN</c> and switching to Area 2 CAD, <see cref="StartView"/> 2×2 grid
/// at 1024 px / 200%, <c>view.notebook</c> from Section mode, and real loopback <see cref="HttpListener"/> end-to-end UI flow.
/// </summary>
public static class NotebookDesktopTests
{
    private static readonly string[] BannedLab01Words =
        ["optimized", "best", "optimal", "ideal", "perfect", "world-class", "fastest", "safest", "guaranteed", "validated", "certified", "cavitation-free", "ventilation-safe", "recommended"];

    public static void Run()
    {
        DesktopChecks.Check("T_NB_08_Copy924To934_VerbatimAndZeroLab01", CheckCopy924To934);
        DesktopChecks.Check("T_NB_08_SevenStates_AcrossLightDarkHighContrast_AndCadHandoffGate", CheckSevenStatesAndThemes);
        DesktopChecks.Check("T_NB_08_FiveArtifactTabs_ArrowKeys_ReuseControls_CompareAndTableTwin", CheckFiveArtifactTabs);
        DesktopChecks.Check("T_NB_08_GrillMe_SerialSingleCard_AnswerAndReopenMarksStale", CheckGrillMeAndReopenStale);
        DesktopChecks.Check("T_NB_08_ConsentPreviewDialog_EffectiveModelId_And_ExtractProvenance", CheckConsentPreviewEffectiveModelAndProvenance);
        DesktopChecks.Check("T_NB_08_SettingsDialog_OptionalFastModelId_SecretScreen_And_DynamicEvalCount", CheckSettingsDialogFastModelAndEval);
        DesktopChecks.Check("T_NB_08_ShellIntegration_Start2x2_SectionModeSwitch_ModelAreaGates_And_CadHandoff", CheckShellIntegrationAndCadHandoff);
        DesktopChecks.Check("T_NB_08_RealLoopbackHttpListener_EndToEndNotebookFlow", CheckRealLoopbackEndToEndFlow);
    }

    private static void CheckCopy924To934()
    {
        Require(NotebookCopy.EntryTitle == "New AI-guided design", "COPY-924 mismatch");
        Require(NotebookCopy.EmptyPromptGuide == "Describe the foil you want to design, how you ride it, and any reference foil concepts to reason over.", "COPY-925 mismatch");
        string c926 = NotebookCopy.UnitSanityAlert("950 cm", "9,500 mm", "950 mm");
        Require(c926 == "Unit check: \"950 cm\" (9,500 mm) is outside hydrofoil wing spans (350–2,200 mm). Did you mean 950 mm?", "COPY-926 mismatch: " + c926);
        Require(NotebookCopy.ReferenceDisclosure == "Reference traits are conceptual only (Inferred / Flagged from public specs) — no proprietary geometry is copied.", "COPY-927 mismatch");
        Require(NotebookCopy.SuggestedDefaultPill == "Suggested default", "COPY-928 mismatch");
        Require(NotebookCopy.ArtifactsProvenanceStrip == "Local calculation (VLM + strip · NeuralFoil 0.3.2 XFOIL-class surrogate · PanelMethod 200p) — computed, not tank-measured. Ventilation not modelled.", "COPY-929 mismatch");
        Require(NotebookCopy.OpenInCadButton == "Open in CAD Mode →", "COPY-930 mismatch");
        Require(NotebookCopy.OpenInCadDisabledReason == "Generate a geometry-checked 3D candidate first to open in CAD Mode.", "COPY-931 mismatch");
        string c932 = NotebookCopy.CadHandoffConfirmBody("v2", 3);
        Require(c932 == "Opens candidate v2 as Design revision r3 in Area 2 CAD. Your notebook transcript and FoilReq stay saved in project history.", "COPY-932 mismatch: " + c932);
        Require(NotebookCopy.NoKeyBanner == "No API key configured — AI-guided design needs a BYOK key. Manual Setup, CAD, and local Analysis work offline without one.", "COPY-933 mismatch");
        Require(NotebookCopy.ResponseWithheldBanner == "Response withheld — cited a numeral not present in local solver output or breached LAB-01 copy policy.", "COPY-934 mismatch");

        string combined = string.Join(" ", [
            NotebookCopy.EntryTitle, NotebookCopy.StartCardSubtitle, NotebookCopy.EmptyPromptGuide, c926,
            NotebookCopy.ReferenceDisclosure, NotebookCopy.SuggestedDefaultPill, NotebookCopy.ArtifactsProvenanceStrip,
            NotebookCopy.OpenInCadButton, NotebookCopy.OpenInCadDisabledReason, c932, NotebookCopy.NoKeyBanner,
            NotebookCopy.ResponseWithheldBanner, NotebookCopy.ComposerHint, NotebookCopy.StaleCandidatesNotice
        ]);

        foreach (string banned in BannedLab01Words)
        {
            Require(!Regex.IsMatch(combined, @"\b" + Regex.Escape(banned) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                $"LAB-01 banned word '{banned}' found in NotebookCopy");
        }
    }

    private static void CheckSevenStatesAndThemes()
    {
        var view = new NotebookWorkspaceView();
        var window = new Window { Content = view, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            ThemeVariant[] themes = [ThemeVariant.Light, ThemeVariant.Dark, NativeReviewThemes.HighContrast];
            NotebookWorkspaceState[] states =
            [
                NotebookWorkspaceState.Empty,
                NotebookWorkspaceState.Grill,
                NotebookWorkspaceState.Simulating,
                NotebookWorkspaceState.Iterated,
                NotebookWorkspaceState.Refused,
                NotebookWorkspaceState.NoKey,
                NotebookWorkspaceState.Overflow
            ];

            foreach (var theme in themes)
            {
                window.RequestedThemeVariant = theme;
                Settle(window);

                foreach (var state in states)
                {
                    view.ApplyPreviewState(state);
                    Settle(window);

                    var openCad = Need<Button>(view, "OpenInCadButton");
                    var disabledBar = Need<Border>(view, "CadDisabledNoteBar");
                    var promptBox = Need<TextBox>(view, "PromptInputBox");
                    var previewBtn = Need<Button>(view, "PreviewPayloadButton");

                    bool expectCadEnabled = state is NotebookWorkspaceState.Iterated or NotebookWorkspaceState.Overflow;
                    Require(openCad.IsEnabled == expectCadEnabled,
                        $"Theme={theme.Key}, State={state}: OpenInCadButton.IsEnabled expected {expectCadEnabled}, got {openCad.IsEnabled}");
                    Require(disabledBar.IsVisible == !expectCadEnabled,
                        $"Theme={theme.Key}, State={state}: CadDisabledNoteBar.IsVisible mismatch");

                    switch (state)
                    {
                        case NotebookWorkspaceState.Empty:
                            Require(Need<Border>(view, "EmptyWelcomeCard").IsVisible, $"{theme.Key}/Empty: EmptyWelcomeCard hidden");
                            Require(Need<Border>(view, "ArtifactEmptyPlaceholder").IsVisible, $"{theme.Key}/Empty: ArtifactEmptyPlaceholder hidden");
                            Require(promptBox.IsEnabled && previewBtn.IsEnabled, $"{theme.Key}/Empty: composer/preview must be enabled");
                            break;

                        case NotebookWorkspaceState.Grill:
                            Require(Need<Border>(view, "ActiveGrillCard").IsVisible, $"{theme.Key}/Grill: ActiveGrillCard hidden");
                            Require(Need<Border>(view, "FoilReqPinCard").IsVisible, $"{theme.Key}/Grill: FoilReqPinCard hidden");
                            break;

                        case NotebookWorkspaceState.Simulating:
                            Require(Need<Border>(view, "SimulatingCard").IsVisible, $"{theme.Key}/Simulating: SimulatingCard hidden");
                            Require(Need<Border>(view, "SimulatingSkeletonPanel").IsVisible, $"{theme.Key}/Simulating: SimulatingSkeletonPanel hidden");
                            Require(!promptBox.IsEnabled && !previewBtn.IsEnabled, $"{theme.Key}/Simulating: composer must be disabled");
                            break;

                        case NotebookWorkspaceState.Iterated:
                            Require(openCad.IsEnabled, $"{theme.Key}/Iterated: OpenInCadButton must be enabled for certified v2");
                            Require(Need<TextBlock>(view, "MetricSpanText").Text != "—", $"{theme.Key}/Iterated: MetricSpanText empty");
                            Require(Need<TextBlock>(view, "ProvenanceStripText").Text == NotebookCopy.ArtifactsProvenanceStrip,
                                $"{theme.Key}/Iterated: ProvenanceStripText mismatch");
                            break;

                        case NotebookWorkspaceState.Refused:
                            Require(!openCad.IsEnabled, $"{theme.Key}/Refused: OpenInCadButton MUST stay disabled on Refused candidate!");
                            Require(Need<Border>(view, "RefusedCard").IsVisible, $"{theme.Key}/Refused: RefusedCard hidden");
                            Require(Need<Border>(view, "RefusedArtifactBanner").IsVisible, $"{theme.Key}/Refused: RefusedArtifactBanner hidden");
                            Require(Need<Border>(view, "WithheldBannerBox").IsVisible, $"{theme.Key}/Refused: WithheldBannerBox hidden");
                            Require(Need<TextBlock>(view, "WithheldBannerText").Text == NotebookCopy.ResponseWithheldBanner,
                                $"{theme.Key}/Refused: COPY-934 withheld text mismatch");
                            break;

                        case NotebookWorkspaceState.NoKey:
                            Require(Need<Border>(view, "NoKeyBannerCard").IsVisible, $"{theme.Key}/NoKey: NoKeyBannerCard hidden");
                            Require(Need<TextBlock>(view, "NoKeyBannerText").Text == NotebookCopy.NoKeyBanner,
                                $"{theme.Key}/NoKey: COPY-933 mismatch");
                            Require(!promptBox.IsEnabled && !previewBtn.IsEnabled,
                                $"{theme.Key}/NoKey: composer and payload preview MUST be disabled in no-key state");
                            break;

                        case NotebookWorkspaceState.Overflow:
                            Require(view.CurrentSession?.Candidates.Count == 12, $"{theme.Key}/Overflow: expected 12 candidates");
                            Require(openCad.IsEnabled, $"{theme.Key}/Overflow: certified v12 must enable CAD handoff");
                            break;
                    }
                }
            }

            // In Refused state, clicking "Inspect checked v1" switches to certified baseline and re-enables Open in CAD Mode.
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.ApplyPreviewState(NotebookWorkspaceState.Refused);
            Settle(window);
            Require(!Need<Button>(view, "OpenInCadButton").IsEnabled, "Refused v2b must keep OpenInCadButton disabled");
            Click(Need<Button>(view, "SwitchToCheckedBaselineButton"));
            Settle(window);
            Require(Need<Button>(view, "OpenInCadButton").IsEnabled, "Switching to certified v1 must re-enable OpenInCadButton");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckFiveArtifactTabs()
    {
        var view = new NotebookWorkspaceView();
        var window = new Window { Content = view, Width = 1280, Height = 820 };
        try
        {
            window.Show();
            view.ApplyPreviewState(NotebookWorkspaceState.Iterated);
            Settle(window);

            // Tab 1: 2D Section & Cp — must come from real PanelMethod (200 panels, not synthetic 65-pt outline)
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabSection, "Default tab must be 2D Section & Cp");
            var secProfile = Need<CfdWorkbench.Desktop.Analysis.SectionProfileView>(view, "NotebookSectionProfileView");
            var cpChart = Need<CfdWorkbench.Desktop.Analysis.SectionChartView>(view, "NotebookCpChartView");
            Require(secProfile.Model is not null && secProfile.Model.Outline.Count == PanelMethod.DefaultPanelCount,
                $"SectionProfileView MUST carry real PanelMethod {PanelMethod.DefaultPanelCount}-panel outline, got {secProfile.Model?.Outline.Count ?? 0}");
            Require(secProfile.Segments.Count >= PanelMethod.DefaultPanelCount - 2,
                $"SectionProfileView must render Cp-coloured segments across real 200 panels, got {secProfile.Segments.Count}");
            Require(cpChart.Model is not null && cpChart.Model.Plots[0].InvertY && cpChart.Drawn.Plots == 1, "SectionChartView must draw inverted -Cp plot");

            // Keyboard arrow-key navigation across all 5 tabs: Right -> Tab 2 (Polars & Curves)
            var tab1 = Need<ToggleButton>(view, "TabSectionButton");
            tab1.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Settle(window);
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabPolars, "Right arrow from Tab 1 must move to Tab 2 Polars");
            var perfChart = Need<CfdWorkbench.Desktop.Sweeps.PerformanceCurveChart>(view, "NotebookPerformanceChart");
            var loadChart = Need<CfdWorkbench.Desktop.Analysis.LoadingChart>(view, "NotebookLoadingChart");
            Require(perfChart.ChartData?.Title == NotebookCopy.WingOnlyLdLabel && perfChart.RenderedSegmentCount >= 1,
                "PerformanceCurveChart must render Wing-only L/D series");
            int expectedVlmStrips = new ProductWingMethod().Settings.NSpanPerHalf;
            Require(loadChart.Points.Count == expectedVlmStrips &&
                    loadChart.Points.All(p => p.ClChordOverMeanChord is { } clc && p.EllipticReference is { } ell && p.LiftPerSpan is { } lift &&
                                              double.IsFinite(clc) && double.IsFinite(ell) && double.IsFinite(p.AlphaEffDeg) && double.IsFinite(lift)),
                $"LoadingChart must carry {expectedVlmStrips} real ProductWingMethod VLM strip points with finite lift/span, got {loadChart.Points.Count}");
            Require(Need<TextBlock>(view, "PolarsNoticeText").Text!.Contains("XFOIL-class surrogate", StringComparison.Ordinal) &&
                    Need<TextBlock>(view, "PolarsNoticeText").Text!.Contains("Attached-flow polar limit", StringComparison.Ordinal),
                "Polars tab must display A5.3 and ANA-05 notices");

            // Toggle Table twin -> live table twin visible on Polars & LoadingChart
            var twinToggle = Need<ToggleButton>(view, "TableTwinToggleButton");
            twinToggle.IsChecked = true;
            Settle(window);
            Require(Need<Border>(view, "PolarsTableTwinPanel").IsVisible && loadChart.ShowingTable,
                "Table twin toggle must show PolarsTableTwinPanel and switch LoadingChart to table");
            twinToggle.IsChecked = false;
            Settle(window);

            // Right arrow -> Tab 3 (Plan & Elevations) — must project real placed SurfaceView (ADR-0010 §Decision 4)
            Need<ToggleButton>(view, "TabPolarsButton").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Settle(window);
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabElevations, "Right arrow from Tab 2 must move to Tab 3 Elevations");
            var elev = Need<NotebookElevationCanvas>(view, "NotebookElevationView");
            using (var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(640, 260), new Vector(96, 96)))
                rtb.Render(elev);
            Require(elev.DrawnElevations == 2 && elev.RenderedFromPlacedSurface && elev.ProjectedSectionCount >= 2,
                $"NotebookElevationCanvas must project real placed SurfaceView into Front & Starboard elevations (drawn={elev.DrawnElevations}, placed={elev.RenderedFromPlacedSurface}, sections={elev.ProjectedSectionCount})");
            Require(Need<Border>(view, "ElevationsA51Banner").IsVisible &&
                    Need<TextBlock>(view, "ElevationsA51BannerText").Text!.Contains("h/c_root", StringComparison.Ordinal) &&
                    Need<TextBlock>(view, "ElevationsA51BannerText").Text!.Contains("Fr_h", StringComparison.Ordinal),
                "Tab 3 must surface A5.1 h/c < 5 and Fr_h notice");

            // Right arrow -> Tab 4 (3D Wing)
            Need<ToggleButton>(view, "TabElevationsButton").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Settle(window);
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabWing3d, "Right arrow from Tab 3 must move to Tab 4 3D Wing");
            var wing3d = Need<NotebookWing3dControl>(view, "Wing3dTabPanel");
            Require(wing3d.CurrentSurface is not null && wing3d.CurrentCamera is not null, "NotebookWing3dControl must host SurfaceRenderer with fitted ViewCamera");
            double azBefore = wing3d.CurrentCamera!.Value.AzimuthDegrees;
            wing3d.OrbitBy(15, 5);
            Settle(window);
            Require(Math.Abs(wing3d.CurrentCamera!.Value.AzimuthDegrees - azBefore) > 1.0, "3D Wing orbit must update ViewCamera azimuth");
            wing3d.SetDisplayMode(DisplayMode.Wireframe);
            Require(wing3d.DisplayMode == DisplayMode.Wireframe && wing3d.Renderer.Display == DisplayMode.Wireframe, "3D Wing toggle must switch to Wireframe");

            // End key -> Tab 5 (Scorecard & FoilReq); Home key -> Tab 1
            Need<ToggleButton>(view, "TabWing3dButton").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End });
            Settle(window);
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabScorecard, "End key must jump to Tab 5 Scorecard");
            var scorecardView = Need<NotebookScorecardView>(view, "ScorecardTabPanel");
            Require(scorecardView.ClauseCount >= 4, $"Scorecard must populate >= 4 clauses, got {scorecardView.ClauseCount}");
            Require(scorecardView.CurrentFoilReqText.Contains("foilreq \"1.0\"", StringComparison.Ordinal), "Scorecard must render FoilReq 1.0 monospace DSL");

            Need<ToggleButton>(view, "TabScorecardButton").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });
            Settle(window);
            Require(view.ActiveTabIndex == NotebookWorkspaceView.TabSection, "Home key must return to Tab 1 Section & Cp");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckGrillMeAndReopenStale()
    {
        var view = new NotebookWorkspaceView();
        var window = new Window { Content = view, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            view.ApplyPreviewState(NotebookWorkspaceState.Grill);
            Settle(window);

            // Strictly 1 active grill question (Q1 unit sanity for 950 cm -> 950 mm).
            Require(view.CurrentGrillState?.ActiveQuestion?.QuestionId == GrillTreeEngine.Q1UnitSanityId,
                $"Expected Q1 unit sanity first, got '{view.CurrentGrillState?.ActiveQuestion?.QuestionId}'");
            Require(Need<Border>(view, "ActiveGrillCard").IsVisible, "ActiveGrillCard must be visible in Grill state");
            Require(Need<StackPanel>(view, "GrillOptionsPanel").Children.Count >= 2, "Q1 must render clickable options");

            // Answer Q1 -> advances to Q2 and creates a clickable summary pill.
            Require(view.AnswerActiveGrill("opt-hard-fix"), "Answering Q1 should succeed");
            Settle(window);
            Require(view.CurrentDocument?.FindConstraint("max_span")?.Quantity?.Unit == "mm", "Answering Q1 must fix 950 cm -> 950 mm");
            Require(Need<WrapPanel>(view, "GrillSummaryPanel").Children.Count == 1, "Answered Q1 must appear as clickable summary pill");

            // Now test TQ9 stale derivation: start in Iterated (with v1/v2), re-open Q2 -> Scorecard shows StaleBanner!
            view.ApplyPreviewState(NotebookWorkspaceState.Iterated);
            view.SelectTab(NotebookWorkspaceView.TabScorecard);
            Settle(window);
            var scorecard = Need<NotebookScorecardView>(view, "ScorecardTabPanel");
            Require(!Need<Border>(scorecard, "StaleBanner").IsVisible, "Fresh iterated candidate must not show Stale banner");

            Require(view.ReopenGrillBranch(GrillTreeEngine.Q2PhysicsTensionId), "Re-opening Q2 must succeed");
            Settle(window);
            Require(Need<Border>(scorecard, "StaleBanner").IsVisible, "Re-opening a grill branch MUST mark downstream candidates Stale (TQ9)");
            Require(Need<Border>(view, "ActiveGrillCard").IsVisible, "Re-opening Q2 must re-display ActiveGrillCard");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckConsentPreviewEffectiveModelAndProvenance()
    {
        string primaryModel = "claude-sonnet-4-5";
        string fastModel = "claude-haiku-fast";

        using var server = new LoopbackLlmServer();
        var creds = new InMemoryCredentialStore();
        var store = new InMemoryAssistantSettingsStore();
        var hub = new AssistantRuntimeHub(creds, store);
        var window = new Window { Width = 900, Height = 700 };
        try
        {
            window.Show();
            creds.WriteKey(AssistantProviderKind.OpenAiCompatible, "test-secret-key-w13b");
            var settings = AssistantSettings.CreateDefault() with
            {
                ProviderKind = AssistantProviderKind.OpenAiCompatible,
                EndpointUrl = server.BaseUrl,
                ModelId = primaryModel,
                OptionalFastModelId = fastModel
            };
            Pump(hub.SaveSettingsAsync(settings));
            Pump(hub.RunEvaluationAsync(liveWire: false, targetModelId: primaryModel));
            Pump(hub.RunEvaluationAsync(liveWire: false, targetModelId: fastModel));

            // 1. Verify EffectiveModelId (fastModel != primaryModel) + RequirementDecomp provenance!
            string decompJson = JsonSerializer.Serialize(new
            {
                kind = "requirement-decomp",
                summary = "Decomposed 90 kg wingfoil brief into FoilReq 1.0.",
                foilReq = BuildCleanFoilReqDsl(),
                provenance = new[]
                {
                    new { fieldName = "rider.mass", tag = "Stated", valueDisplay = "90.0 kg", reason = "Stated in brief" },
                    new { fieldName = "envelope.max_span", tag = "Stated", valueDisplay = "<= 950 mm", reason = "Confirmed span cap" }
                }
            });
            server.EnqueueTextResponse(decompJson);

            var decompPreview = Pump(hub.PreparePreviewAsync(new AssistantPreviewRequest(
                Area: AssistantArea.Notebook,
                ProposalKind: AssistantProposalKind.RequirementDecomp,
                SystemPrompt: "Return strict JSON.",
                UserPrompt: "Decompose the quoted brief.",
                Excerpts: [new UntrustedExcerpt("user-brief", "b1", "90 kg wingfoil under 950 mm")],
                ModelIdOverride: fastModel)));
            Require(decompPreview.Allowed && decompPreview.Prepared is not null, "Decomp preview must be allowed");

            var dlg1 = new AssistantConsentPreviewDialog(
                hub,
                decompPreview.Prepared!,
                new AssistantValidationInput(AssistantArea.Notebook, AssistantProposalKind.RequirementDecomp));
            dlg1.Show(window);
            Settle(dlg1);
            try
            {
                string wireText = Need<TextBlock>(dlg1, "WireIdentityText").Text ?? "";
                Require(wireText.Contains(fastModel, StringComparison.Ordinal),
                    $"Consent preview MUST display EffectiveModelId '{fastModel}', got: '{wireText}'");
                Require(!wireText.Contains(primaryModel, StringComparison.Ordinal),
                    $"Consent preview must NOT display primary model '{primaryModel}' when overridden by '{fastModel}'");
                Require(wireText.Contains("UTF-8 B", StringComparison.Ordinal),
                    "Consent preview must display dynamic UTF-8 byte count");

                var exec1 = Pump(dlg1.SendAsync());
                Settle(dlg1);
                Require(exec1.Succeeded, "RequirementDecomp execution failed: " + exec1.ErrorMessage);
                Require(Need<Border>(dlg1, "ProvenanceCard").IsVisible, "RequirementDecomp must show ProvenanceCard via ExtractProvenance");
                var rows1 = Need<ItemsControl>(dlg1, "ProvenanceRowsList").ItemsSource?.Cast<object>().ToArray() ?? [];
                Require(rows1.Length == 2, $"Expected 2 RequirementDecomp provenance rows, got {rows1.Length}");
            }
            finally { dlg1.Close(); }

            // 2. Verify CandidateIntentDelta provenance!
            string deltaJson = JsonSerializer.Serialize(new
            {
                kind = "candidate-intent-delta",
                spanMeters = 0.92,
                areaSquareMeters = 0.083,
                taper = 0.35,
                rootThicknessRatio = 0.11,
                anhedralDeg = -5.5,
                washoutDeg = -1.2,
                sweepQuarterChordDeg = 6.8,
                rootProfileId = "cfdw-surf-118",
                tipProfileId = "cfdw-surf-105",
                provenance = new[]
                {
                    new { fieldName = "spanMeters", tag = "Stated", valueDisplay = "920 mm", reason = "Within 950 mm cap" },
                    new { fieldName = "anhedralDeg", tag = "Inferred", valueDisplay = "-5.5 deg", reason = "Cutback roll balance" }
                }
            });
            server.EnqueueTextResponse(deltaJson);

            var deltaPreview = Pump(hub.PreparePreviewAsync(new AssistantPreviewRequest(
                Area: AssistantArea.Notebook,
                ProposalKind: AssistantProposalKind.CandidateIntentDelta,
                SystemPrompt: "Return strict JSON.",
                UserPrompt: "Synthesize candidate intent delta.",
                Excerpts: [new UntrustedExcerpt("foilreq", "f1", BuildCleanFoilReqDsl())],
                ModelIdOverride: fastModel)));
            Require(deltaPreview.Allowed && deltaPreview.Prepared is not null, "CandidateIntentDelta preview must be allowed");

            var dlg2 = new AssistantConsentPreviewDialog(
                hub,
                deltaPreview.Prepared!,
                new AssistantValidationInput(AssistantArea.Notebook, AssistantProposalKind.CandidateIntentDelta));
            dlg2.Show(window);
            Settle(dlg2);
            try
            {
                var exec2 = Pump(dlg2.SendAsync());
                Settle(dlg2);
                Require(exec2.Succeeded, "CandidateIntentDelta execution failed: " + exec2.ErrorMessage);
                Require(Need<Border>(dlg2, "ProvenanceCard").IsVisible, "CandidateIntentDelta must show ProvenanceCard via ExtractProvenance");
                var rows2 = Need<ItemsControl>(dlg2, "ProvenanceRowsList").ItemsSource?.Cast<object>().ToArray() ?? [];
                Require(rows2.Length == 2, $"Expected 2 CandidateIntentDelta provenance rows, got {rows2.Length}");
            }
            finally { dlg2.Close(); }
        }
        finally
        {
            window.Close();
            hub.Dispose();
        }
    }

    private static void CheckSettingsDialogFastModelAndEval()
    {
        var creds = new InMemoryCredentialStore();
        var store = new InMemoryAssistantSettingsStore();
        var hub = new AssistantRuntimeHub(creds, store);
        var window = new Window { Width = 900, Height = 700 };
        try
        {
            window.Show();
            creds.WriteKey(AssistantProviderKind.Anthropic, "test-key-settings-w13b");

            var dlg = new AssistantSettingsDialog(hub);
            dlg.Show(window);
            Settle(dlg);
            try
            {
                // Verify dynamic 11 active + 1 reserved = 12 total eval rows from AssistantEvalSuite.GetSuiteDefinition().
                int expectedTotal = AssistantEvalSuite.GetSuiteDefinition().Count;
                int expectedActive = AssistantEvalSuite.GetSuiteDefinition().Count(i => !i.Reserved);
                Require(expectedActive == 11 && expectedTotal == 12, $"Expected 11 active / 12 total eval items, got {expectedActive}/{expectedTotal}");
                var evalRows = Need<ItemsControl>(dlg, "EvalItemsList").ItemsSource?.Cast<object>().ToArray() ?? [];
                Require(evalRows.Length == expectedTotal, $"Expected {expectedTotal} eval rows in Settings dialog, got {evalRows.Length}");
                Require((Need<Button>(dlg, "RunEvalButton").Content as string ?? "").Contains($"{expectedActive}-item", StringComparison.Ordinal),
                    "RunEvalButton must reflect dynamic active eval count");

                // Value-level secret screening rejects pasted sk-ant-... key in OptionalFastModelIdBox!
                var fastBox = Need<TextBox>(dlg, "OptionalFastModelIdBox");
                fastBox.Text = string.Concat("sk-ant-", "api03-secretvalue1234567890abcdef");
                Settle(dlg);
                var rejected = Pump(dlg.SaveSettingsNowAsync());
                Require(!rejected.Succeeded, "Pasted API key in OptionalFastModelIdBox MUST be rejected");

                // Enter valid fast model ID, save, verify RunFastEvalButton is visible, and run fast-model evaluation.
                fastBox.Text = "claude-haiku-4-5";
                Settle(dlg);
                var saved = Pump(dlg.SaveSettingsNowAsync());
                Require(saved.Succeeded, "Saving valid OptionalFastModelId failed: " + saved.Message);
                Require(hub.CurrentSettings.OptionalFastModelId == "claude-haiku-4-5", "OptionalFastModelId did not persist");
                Require(Need<Button>(dlg, "RunFastEvalButton").IsVisible, "RunFastEvalButton must be visible when OptionalFastModelId is set");

                var fastEvalRes = Pump(dlg.RunFastEvaluationNowAsync());
                Settle(dlg);
                Require(fastEvalRes is not null && fastEvalRes.Receipt.ModelId == "claude-haiku-4-5" && fastEvalRes.Receipt.AllActiveItemsMetThreshold,
                    "Fast-model evaluation must produce passing receipt for claude-haiku-4-5");
                Require(hub.FastModelEligibility?.ProposalsEnabled == true,
                    "Hub FastModelEligibility must enable proposals after fast-model evaluation passes");
            }
            finally { dlg.Close(); }
        }
        finally
        {
            window.Close();
            hub.Dispose();
        }
    }

    private static void CheckShellIntegrationAndCadHandoff()
    {
        // CommandTable parity for view.notebook (File menu, Modal: false, ShellHost.IsPaneCommand).
        var nbRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "view.notebook")
            ?? throw new InvalidOperationException("CommandTable missing view.notebook");
        Require(nbRow.Title == NotebookCopy.EntryTitle && nbRow.Menu == "File" && !nbRow.Modal,
            $"view.notebook mismatch: Title='{nbRow.Title}', Menu='{nbRow.Menu}', Modal={nbRow.Modal}");
        Require(ShellHost.IsPaneCommand("view.notebook"), "view.notebook must be routed by ShellHost.IsPaneCommand");

        // Verify StartView 2x2 grid fits at 1024x700 and 200% text scaling with zero clipping/overlap.
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1024, Height = 700 };
        try
        {
            window.Show();
            Settle(window);

            // Check ActivityRail width remains 84 DIP and RailNotebookButton hidden before notebook session exists.
            Require(Math.Abs(host.ActivityRail.Bounds.Width - 84.0) <= 1.0,
                $"ActivityRail width must stay 84 DIP, got {host.ActivityRail.Bounds.Width}");
            Require(!Need<Button>(host.ActivityRail, "RailNotebookButton").IsVisible,
                "RailNotebookButton must stay hidden before a notebook session is active or attached");

            // Check StartView 2x2 grid at 1024px / 200% text scaling.
            Pump(host.RunCommand("view.text-200"));
            Settle(window);
            var startView = host.ModelView.StartCardView;
            var btnNew = Need<Button>(startView, "StartNewButton");
            var btnAi = Need<Button>(startView, "StartAiNotebookButton");
            var btnEx = Need<Button>(startView, "StartExampleButton");
            var btnOpen = Need<Button>(startView, "StartOpenButton");
            foreach (var b in new[] { btnNew, btnAi, btnEx, btnOpen })
            {
                Require(b.IsEffectivelyVisible && b.Bounds.Width > 100 && b.Bounds.Height >= 32,
                    $"StartView card '{b.Name}' did not lay out cleanly at 1024px / 200%: {b.Bounds}");
            }
            Require(Grid.GetRow(btnNew) == 0 && Grid.GetColumn(btnNew) == 0 &&
                    Grid.GetRow(btnAi) == 0 && Grid.GetColumn(btnAi) == 1 &&
                    Grid.GetRow(btnEx) == 1 && Grid.GetColumn(btnEx) == 0 &&
                    Grid.GetRow(btnOpen) == 1 && Grid.GetColumn(btnOpen) == 1,
                "StartView must lay out a 2x2 card grid");
            Pump(host.RunCommand("view.text-100"));
            Settle(window);

            // Clicking StartAiNotebookButton enters Notebook from Start (!foilOpen), hides StartCardView, shows NotebookWorkspace, collapses CAD docks.
            Click(btnAi);
            Settle(window);
            Require(controller.IsNotebook && controller.ActiveArea == WorkbenchArea.Notebook, "StartAiNotebookButton did not enter Notebook");
            Require(!host.ModelView.StartCardView.IsVisible, "ModelArea MUST hide StartCardView when IsNotebook is true");
            Require(host.ModelView.FindControl<Border>("PlanContent")!.IsVisible, "ModelArea PlanContent must be visible in Notebook");
            Require(!host.ModelView.FindControl<Grid>("ViewArrangementGrid")!.IsVisible, "ModelArea MUST hide ViewArrangementGrid when IsNotebook is true");
            Require(host.ModelView.NotebookView.IsVisible, "NotebookWorkspaceView must be visible");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true,
                "SyncDockAutoCollapse must collapse CAD LeftToolDock in Notebook");
            Require(Need<Button>(host.ActivityRail, "RailNotebookButton").IsVisible,
                "RailNotebookButton must become visible when Notebook is active");

            // Test CadHandoffDialog gate: disabled on Refused, enabled on Certified, appends r1 and switches to Area 2 CAD!
            host.ModelView.NotebookView.ApplyPreviewState(NotebookWorkspaceState.Refused);
            Settle(window);
            var refusedCand = host.ModelView.NotebookView.ActiveCandidate!;
            var dlgRefused = new CadHandoffDialog(refusedCand, 1);
            dlgRefused.Show(window);
            Settle(dlgRefused);
            try
            {
                Require(!Need<Button>(dlgRefused, "ConfirmButton").IsEnabled, "CadHandoffDialog ConfirmButton MUST be disabled on Refused candidate");
            }
            finally { dlgRefused.Close(); }

            host.ModelView.NotebookView.ApplyPreviewState(NotebookWorkspaceState.Iterated);
            Settle(window);
            var certifiedCand = host.ModelView.NotebookView.ActiveCandidate!;
            var dlgCert = new CadHandoffDialog(certifiedCand, 1, "v1.4", "wingfoil-surf");
            dlgCert.Show(window);
            Settle(dlgCert);
            try
            {
                Require(Need<Button>(dlgCert, "ConfirmButton").IsEnabled, "CadHandoffDialog ConfirmButton must be enabled on Certified candidate");
                Require(Need<TextBlock>(dlgCert, "ConfirmBodyText").Text == NotebookCopy.CadHandoffConfirmBody(certifiedCand.CandidateId, 1),
                    "CadHandoffDialog body must match COPY-932");
            }
            finally { dlgCert.Close(); }

            // Confirm handoff -> appends r1 and switches cleanly to Area 2 CAD + restores CAD docks!
            host.ModelView.NotebookView.CadHandoffConfirmHandler = (_, _) => Task.FromResult(true);
            var handoff = Pump(host.ModelView.NotebookView.RequestCadHandoffAsync());
            Settle(window);
            Require(handoff is not null && handoff.SeedOutcome.RevisionOrdinal == 1, "CAD handoff from Start must seed revision r1");
            Require(controller.ActiveArea == WorkbenchArea.Cad && !controller.IsNotebook, "CAD handoff must switch cleanly to Area 2 CAD");
            Require(controller.Inspection is not null && host.ModelView.FindControl<Grid>("ViewArrangementGrid")!.IsVisible,
                "Area 2 CAD views must be visible after CAD handoff");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true,
                "SyncDockAutoCollapse must restore CAD LeftToolDock upon returning to Area 2 CAD");
            Require(Need<Button>(host.ActivityRail, "RailNotebookButton").IsVisible,
                "RailNotebookButton must remain visible after CAD handoff so user can return to saved Notebook");

            // Enter Section mode, then invoke view.notebook -> must switch cleanly out of Section mode without modal!
            Pump(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            Settle(window);
            Require(host.ModelView.Mode == ModelAreaMode.Section, "Expected Section mode before view.notebook test");
            Pump(host.RunCommand("view.notebook"));
            Settle(window);
            Require(controller.IsNotebook && host.ModelView.Mode == ModelAreaMode.Views && host.ModelView.NotebookView.IsVisible,
                "view.notebook invoked from Section mode must cleanly switch to Notebook workspace");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckRealLoopbackEndToEndFlow()
    {
        using var server = new LoopbackLlmServer();
        var creds = new InMemoryCredentialStore();
        var store = new InMemoryAssistantSettingsStore();
        using var controller = new WorkbenchController(credentialStore: creds, assistantSettingsStore: store);
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            creds.WriteKey(AssistantProviderKind.OpenAiCompatible, "test-loopback-key-w13b");
            var settings = AssistantSettings.CreateDefault() with
            {
                ProviderKind = AssistantProviderKind.OpenAiCompatible,
                EndpointUrl = server.BaseUrl,
                ModelId = "local-loopback-model"
            };
            Pump(controller.Assistant.SaveSettingsAsync(settings));
            Pump(controller.Assistant.RunEvaluationAsync(liveWire: false));

            Pump(host.RunCommand("view.notebook"));
            Settle(window);
            var nb = host.ModelView.NotebookView;
            nb.ConsentHandler = (prepared, _) => Task.FromResult(new NotebookConsentDecision(true, prepared.Payload.PayloadSha256));
            nb.CadHandoffConfirmHandler = (_, _) => Task.FromResult(true);

            // 1. Enqueue RequirementDecomp response on real loopback HttpListener.
            string decompJson = JsonSerializer.Serialize(new
            {
                kind = "requirement-decomp",
                summary = "Parsed 90 kg wingfoil brief into FoilReq 1.0.",
                foilReq = BuildCleanFoilReqDsl(),
                provenance = new[]
                {
                    new { fieldName = "rider.mass", tag = "Stated", valueDisplay = "90.0 kg", reason = "Rider brief" }
                }
            });
            server.EnqueueTextResponse(decompJson);

            bool decompOk = Pump(nb.SubmitPromptAsync("90 kg advanced wingfoil under 950 mm, 24 kn steep drops at 0.45 m depth"));
            Settle(window);
            Require(decompOk && nb.CurrentDocument is not null, "Real loopback RequirementDecomp failed");
            Require(server.ReceivedBodies.Count == 1, $"Expected 1 loopback HTTP call, got {server.ReceivedBodies.Count}");
            string firstUserContent = ExtractOpenAiUserMessage(server.ReceivedBodies[0]);
            Require(firstUserContent.Contains("<<<CFDW_QUOTED_DATA", StringComparison.Ordinal),
                "Loopback server must receive user brief fenced in <<<CFDW_QUOTED_DATA");

            // 2. Enqueue CandidateIntentDelta + AnalysisExplanation responses on real loopback HttpListener.
            string deltaJson = JsonSerializer.Serialize(new
            {
                kind = "candidate-intent-delta",
                spanMeters = 0.92,
                areaSquareMeters = 0.084,
                taper = 0.35,
                rootThicknessRatio = 0.115,
                anhedralDeg = -5.0,
                washoutDeg = -1.2,
                sweepQuarterChordDeg = 6.5,
                rootProfileId = "cfdw-surf-118",
                tipProfileId = "cfdw-surf-105",
                provenance = new[]
                {
                    new { fieldName = "spanMeters", tag = "Stated", valueDisplay = "920 mm", reason = "Within 950 mm hard cap" }
                }
            });
            string translationJson = JsonSerializer.Serialize(new
            {
                kind = "explanation",
                text = "Candidate v1 spans 920.0 mm with aspect ratio within target and stays below the 5.0 h/c threshold.",
                citations = new[] { "v1" }
            });
            server.EnqueueTextResponse(deltaJson);
            server.EnqueueTextResponse(translationJson);

            bool synthOk = Pump(nb.RunCandidateSynthesisLoopAsync("Generate initial balanced surf-glide foil"));
            Settle(window);
            Require(synthOk && nb.ActiveCandidate is { GeometryStatus: "Certified" },
                $"Real loopback synthesis + local physics failed; status={nb.ActiveCandidate?.GeometryStatus}");
            Require(nb.ActiveCandidate!.OperatingPoints.Count == 3, "Local NotebookCandidateRunner must compute 3 operating points");
            Require(Need<Button>(nb, "OpenInCadButton").IsEnabled, "Certified loopback candidate v1 must enable Open in CAD Mode");
            Require(server.ReceivedBodies.Count == 3, $"Expected 3 real loopback HTTP calls (decomp, delta, translation), got {server.ReceivedBodies.Count}");

            // 3. Hand off v1 to Area 2 CAD -> revision r1 open in CAD!
            var handoff = Pump(nb.RequestCadHandoffAsync());
            Settle(window);
            Require(handoff is not null && handoff.SeedOutcome.RevisionOrdinal == 1 && controller.ActiveArea == WorkbenchArea.Cad,
                "End-to-end loopback CAD handoff did not land r1 in Area 2 CAD");
        }
        finally
        {
            window.Close();
        }
    }

    private static string BuildCleanFoilReqDsl() => """
        foilreq "1.0" {
          brief {
            discipline: wingfoil-surf;
            skill: advanced;
          }
          rider {
            mass: 90.0 kg @stated;
            board_mass: 5.5 kg @defaulted("board");
            foil_mass: 4.5 kg @defaulted("foil");
            wing_kit_mass: 5.0 kg @defaulted("kit");
          }
          water {
            kind: salt @stated;
            temp: 15.0 C @defaulted("ITTC");
            depth_ref: 0.45 m @stated;
          }
          envelope {
            max_span: <= 950 mm hard @stated;
            target_ar: ~ 10.1 soft(0.85) @stated;
          }
          objectives {
            takeoff_speed: <= 10.8 kn high @stated;
            drop_speed: >= 24.0 kn high @stated;
          }
          maneuvers {
            cutback: "controlled steep wave drops" high @stated;
          }
          open_questions {
          }
        }
        """;

    private static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? throw new InvalidOperationException($"Missing control '{name}' in {root.GetType().Name}");

    private static void Click(Button button) =>
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Settle(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string ExtractOpenAiUserMessage(string wireJson)
    {
        using var doc = JsonDocument.Parse(wireJson);
        if (doc.RootElement.TryGetProperty("messages", out var msgs))
        {
            foreach (var msg in msgs.EnumerateArray())
            {
                if (msg.TryGetProperty("role", out var role) && role.GetString() == "user" &&
                    msg.TryGetProperty("content", out var content))
                    return content.GetString() ?? "";
            }
        }
        return wireJson;
    }

    private static void Pump(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Timed out waiting for async desktop task.");
            Thread.Sleep(5);
        }
        task.GetAwaiter().GetResult();
    }

    private static T Pump<T>(Task<T> task)
    {
        Pump((Task)task);
        return task.GetAwaiter().GetResult();
    }

    private sealed class LoopbackLlmServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly CancellationTokenSource cts = new();
        private readonly Queue<string> queuedResponses = new();
        private readonly object gate = new();
        private readonly Task loopTask;

        public LoopbackLlmServer()
        {
            int port;
            using (var tcp = new TcpListener(IPAddress.Loopback, 0))
            {
                tcp.Start();
                port = ((IPEndPoint)tcp.LocalEndpoint).Port;
            }
            BaseUrl = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}");
            listener = new HttpListener();
            listener.Prefixes.Add(BaseUrl + "/");
            listener.Start();
            loopTask = Task.Run(ServeLoopAsync);
        }

        public string BaseUrl { get; }
        public List<string> ReceivedBodies { get; } = [];

        public void EnqueueTextResponse(string assistantContentText)
        {
            lock (gate) queuedResponses.Enqueue(assistantContentText);
        }

        private async Task ServeLoopAsync()
        {
            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
                catch { break; }

                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                string content;
                lock (gate)
                {
                    ReceivedBodies.Add(body);
                    content = queuedResponses.Count > 0 ? queuedResponses.Dequeue() : "{}";
                }

                string wireJson = JsonSerializer.Serialize(new
                {
                    id = "chatcmpl-w13b-test",
                    choices = new[]
                    {
                        new { message = new { role = "assistant", content } }
                    },
                    usage = new { prompt_tokens = 120, completion_tokens = 64 }
                });
                byte[] bytes = Encoding.UTF8.GetBytes(wireJson);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
                ctx.Response.Close();
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            try { listener.Stop(); } catch { }
            try { listener.Close(); } catch { }
            try { loopTask.Wait(TimeSpan.FromSeconds(1)); } catch { }
            cts.Dispose();
        }
    }
}
