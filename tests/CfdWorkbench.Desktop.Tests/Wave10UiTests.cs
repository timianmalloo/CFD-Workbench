using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.CfdRun;
using CfdWorkbench.Analysis.Results;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;
using CfdWorkbench.Desktop.Results;
using CfdWorkbench.Desktop.CfdRun;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 10D (W10-RAIL-UI) Desktop UI tests covering:
/// 1. 7-Area Left Activity Rail docking in ShellHost (Col=0, Row=GetRow(DockHost), RowSpan=1), live derived
///    readiness badges, Export dialog &amp; Solver setup jumps, zero view rewrites (Rulings 271 &amp; 272),
///    4-button ModelArea.Navbar synchronization, and zero Dock.* references in ActivityRail.
/// 2. Area 5 RunWorkspaceView: 11-state run queue &amp; live status, MeshGate breakdown + screen-only badge,
///    residuals/forces table + LAB-01 three-part Converged label, Run/Cancel/Retry/Set up a solver jump,
///    and offline key-free Explain this failure / Preview case diff (AI-01/AI-11).
/// 3. Area 6 ResultsWorkspaceView &amp; ResultsFieldCanvas: FieldEvidenceManifest layer toggles (separation strictly
///    gated on wall tau_w; Q/lambda2 labelled vortex-core candidates), ResultsFieldCanvas scalar field + isolines
///    with PropFontSize >= 11 and theme brushes, 9-field ProvenanceStrip + linked TableTwin, Replay (Play/Step/
///    Scrub/Hold/RangeLock + failed-sample pause &amp; clear), DifferenceFlood ("Unavailable — mesh differs"),
///    CandidateLadder + Accept as draft CAD handoff &amp; Rebase guard, and ParaView honest absence + process-only launch.
/// 4. DESIGN.md COPY-806..865 proposed rows &amp; CommandTable view.run/view.results Modal=false parity.
/// </summary>
public static class Wave10UiTests
{
    public static void Run()
    {
        DesktopChecks.Check("ActivityRail_SevenAreas_LiveBadges_DockingZeroRewrite", CheckActivityRailAndDocking);
        DesktopChecks.Check("Wave12A_ShellChrome_CommandTable_ControlTokens_TextScale_StatusStripFit", CheckWave12AShellChromeAndTokens);
        DesktopChecks.Check("RunWorkspaceView_ElevenStates_MeshGate_And_OfflineFailureExplain", CheckRunWorkspaceView);
        DesktopChecks.Check("ResultsWorkspaceView_TauWSeparation_ReplayPauseClear_DifferenceFlood_CandidateDraft", CheckResultsWorkspaceView);
        DesktopChecks.Check("Wave10Ui_DesignCopy806To865_And_CommandTableRunResultsModalFalse", CheckCopyAndCommandTable);
    }

    private static void CheckActivityRailAndDocking()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        bool exportOpened = false;
        host.ShowExportDialog = _ =>
        {
            exportOpened = true;
            return Task.FromResult<CfdWorkbench.Analysis.Export.ExportOutcome?>(null);
        };

        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            // R2-14 ShellHost layout invariants: Auto,* columns, exact 3 rows, ActivityRail in Col=0 Row=GetRow(DockHost) RowSpan=2.
            Require(host.ColumnDefinitions.Count == 2, $"expected 2 ShellHost columns, got {host.ColumnDefinitions.Count}");
            Require(host.RowDefinitions.Count == 3, $"expected 3 ShellHost rows, got {host.RowDefinitions.Count}");
            Require(Grid.GetColumn(host.ActivityRail) == 0, "ActivityRail must sit in Col=0");
            Require(Grid.GetRow(host.ActivityRail) == Grid.GetRow(host.DockHost), "ActivityRail row must match DockHost row");
            Require(Grid.GetRowSpan(host.ActivityRail) == 2, "ActivityRail RowSpan must be 2 to span DockHost and AnalysisPanel");
            Require(Grid.GetColumn(host.DockHost) == 1, "DockHost must sit in Col=1");
            Require(Grid.GetColumn(host.AnalysisPanel) == 1, "AnalysisPanel must sit in Col=1");
            Require(Grid.GetColumn(host.StatusStrip) == 0 && Grid.GetColumnSpan(host.StatusStrip) == 2,
                "StatusStrip must span both columns");

            // ShowMenuBar shifts ActivityRail alongside DockHost, AnalysisPanel, StatusStrip while keeping RowSpan=2.
            host.ShowMenuBar();
            Settle(window);
            Require(Grid.GetRow(host.MenuBar!) == 0 && Grid.GetColumnSpan(host.MenuBar!) == 2,
                "MenuBar must sit at Row=0 ColumnSpan=2");
            Require(Grid.GetRow(host.ActivityRail) == Grid.GetRow(host.DockHost),
                "ShowMenuBar must shift ActivityRail alongside DockHost");
            Require(Grid.GetRowSpan(host.ActivityRail) == 2,
                "ActivityRail RowSpan must remain 2 after ShowMenuBar");

            var rail = host.ActivityRail;
            Require(rail.IsEffectivelyVisible, "ActivityRail must be visible");

            // R2-04 (Ruling 277): duplicate 4-area Navbar toggles retired; Navbar hidden on Start (!foilOpen).
            Require(host.ModelView.FindControl<ToggleButton>("NavSetupButton") is null &&
                    host.ModelView.FindControl<ToggleButton>("NavCadButton") is null &&
                    host.ModelView.FindControl<ToggleButton>("NavAnalysisButton") is null &&
                    host.ModelView.FindControl<ToggleButton>("NavSweepsButton") is null,
                "Ruling 277 violated: duplicate area toggles must be removed from ModelArea.Navbar");
            Require(!host.ModelView.FindControl<Border>("Navbar")!.IsVisible,
                "ModelArea.Navbar must be hidden on Start when no foil is open");

            // C4 trap guard: initial Start (ActiveArea == Cad, !foilOpen) must NEVER auto-collapse Left Tool Dock.
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true,
                "C4 violated: LeftToolDock must remain visible on initial Cad/Start");

            // R2-14: clicking Analysis or Experiment on Start (!foilOpen) keeps CAD/Start active and reports status.
            Click(Need<Button>(rail, "RailAnalysisButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Cad &&
                    !Need<Button>(rail, "RailAnalysisButton").Classes.Contains("active") &&
                    Need<Button>(rail, "RailCadButton").Classes.Contains("active") &&
                    host.StatusStrip.Text == "Open or seed a foil first.",
                $"Start Analysis click should guard with status, got area={controller.ActiveArea}, status='{host.StatusStrip.Text}'");

            Click(Need<Button>(rail, "RailExperimentButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Cad &&
                    !Need<Button>(rail, "RailExperimentButton").Classes.Contains("active") &&
                    Need<Button>(rail, "RailCadButton").Classes.Contains("active") &&
                    host.StatusStrip.Text == "Open or seed a foil first.",
                $"Start Experiment click should guard with status, got area={controller.ActiveArea}, status='{host.StatusStrip.Text}'");

            // Open a real foil so all 6 document areas + Export are live.
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // Verify live derived readiness badges exist and reflect open foil state.
            Require(NeedText(rail, "BadgeCadText").StartsWith("r", StringComparison.Ordinal),
                $"expected CAD revision badge r<n>, got '{NeedText(rail, "BadgeCadText")}'");
            Require(!string.IsNullOrWhiteSpace(NeedText(rail, "BadgeSetupText")), "Setup badge must be non-empty");
            Require(!string.IsNullOrWhiteSpace(NeedText(rail, "BadgeAnalysisText")), "Analysis badge must be non-empty");
            Require(!string.IsNullOrWhiteSpace(NeedText(rail, "BadgeExperimentText")), "Experiment badge must be non-empty");
            Require(!string.IsNullOrWhiteSpace(NeedText(rail, "BadgeRunText")), "Run badge must be non-empty");
            Require(!string.IsNullOrWhiteSpace(NeedText(rail, "BadgeResultsText")), "Results badge must be non-empty");
            Require(host.ModelView.FindControl<Border>("Navbar")!.IsVisible,
                "ModelArea.Navbar must be visible in CAD when ViewArrangementGrid is visible");

            // Open Right Tool Dock in CAD to verify both Left & Right docks auto-collapse outside CAD/Analysis and restore on return.
            host.ToggleRightSidebar();
            Settle(window);
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true &&
                    host.RightSidebarShown,
                "Both Left and Right docks should be visible in CAD before switching");

            // Slot 1: Setup auto-collapses Left/Right docks, hides Navbar, removes SweepNavbarClearance.
            Click(Need<Button>(rail, "RailSetupButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Setup && controller.IsSetup, "Rail Setup click did not enter Setup");
            Require(host.ModelView.SetupView.IsVisible, "SetupWorkspaceView must be visible on rail slot 1");
            Require(Need<Button>(rail, "RailSetupButton").Classes.Contains("active"), "RailSetupButton must be active");
            Require(!host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must hide in Setup");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true &&
                    !host.RightSidebarShown,
                "R2-05: Left and Right CAD docks must auto-collapse in Setup");
            Require(((Grid)host.ModelView.SetupView.Content!).Margin == new Thickness(0),
                "SetupWorkspaceView must have zero bottom SweepNavbarClearance margin");

            // Slot 4: Experiment keeps docks collapsed, hides Navbar, zero SweepNavbarClearance.
            Click(Need<Button>(rail, "RailExperimentButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Experiment && controller.IsSweeps, "Rail Experiment click did not enter Sweeps");
            Require(host.ModelView.SweepsView.IsVisible, "SweepsWorkspaceView must be visible on rail slot 4");
            Require(Need<Button>(rail, "RailExperimentButton").Classes.Contains("active"), "RailExperimentButton must be active");
            Require(!host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must hide in Experiment");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true &&
                    !host.RightSidebarShown,
                "R2-05: Left and Right CAD docks must remain collapsed in Experiment");
            Require(((Grid)host.ModelView.SweepsView.Content!).Margin == new Thickness(0),
                "SweepsWorkspaceView must have zero bottom SweepNavbarClearance margin");

            // Slot 5: Run keeps docks collapsed, hides Navbar, zero SweepNavbarClearance.
            Click(Need<Button>(rail, "RailRunButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Run && controller.IsRun, "Rail Run click did not enter Run");
            Require(host.ModelView.RunView.IsVisible, "RunWorkspaceView must be visible on rail slot 5");
            Require(Need<Button>(rail, "RailRunButton").Classes.Contains("active"), "RailRunButton must be active");
            Require(!host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must hide in Run");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true &&
                    !host.RightSidebarShown,
                "R2-05: Left and Right CAD docks must remain collapsed in Run");
            Require(((Grid)host.ModelView.RunView.Content!).Margin == new Thickness(0),
                "RunWorkspaceView must have zero bottom SweepNavbarClearance margin");

            // Slot 6: Results keeps docks collapsed, hides Navbar, zero SweepNavbarClearance.
            Click(Need<Button>(rail, "RailResultsButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Results && controller.IsResults, "Rail Results click did not enter Results");
            Require(host.ModelView.ResultsView.IsVisible, "ResultsWorkspaceView must be visible on rail slot 6");
            Require(Need<Button>(rail, "RailResultsButton").Classes.Contains("active"), "RailResultsButton must be active");
            Require(!host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must hide in Results");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true &&
                    !host.RightSidebarShown,
                "R2-05: Left and Right CAD docks must remain collapsed in Results");
            Require(((Grid)host.ModelView.ResultsView.Content!).Margin == new Thickness(0),
                "ResultsWorkspaceView must have zero bottom SweepNavbarClearance margin");

            // Slot 2: CAD restores prior Left & Right dock visibility and shows viewport Navbar.
            Click(Need<Button>(rail, "RailCadButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Cad, "Rail CAD click did not return to CAD");
            Require(Need<Button>(rail, "RailCadButton").Classes.Contains("active"), "RailCadButton must be active");
            Require(host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must show again in CAD");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true &&
                    host.RightSidebarShown,
                "R2-05: returning to CAD must restore prior Left and Right dock visibility");

            // Slot 3: Analysis activates Analysis, shows AnalysisPanel and viewport Navbar, keeps docks restored.
            Click(Need<Button>(rail, "RailAnalysisButton"));
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Analysis && controller.IsAnalysis, "Rail Analysis click did not enter Analysis");
            Require(Need<Button>(rail, "RailAnalysisButton").Classes.Contains("active"), "RailAnalysisButton must be active");
            Require(host.ModelView.FindControl<Border>("Navbar")!.IsVisible, "Navbar must be visible in Analysis");

            // Slot 7: Export opens existing ExportDialog.
            Click(Need<Button>(rail, "RailExportButton"));
            Settle(window);
            Require(exportOpened, "Rail Export button must invoke ExportDialog");

            // Footer: Solver setup jumps to existing non-blocking SolverSetupDocument, highlights RailSolverSetupButton .active,
            // clears area .active flags, and auto-collapses CAD docks.
            Click(Need<Button>(rail, "RailSolverSetupButton"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.SolverSetupDocument),
                "Rail Solver setup footer button must open SolverSetupDocument");
            Require(Need<Button>(rail, "RailSolverSetupButton").Classes.Contains("active"),
                "R2-14: RailSolverSetupButton must carry .active while SolverSetupDocument is active");
            Require(!Need<Button>(rail, "RailAnalysisButton").Classes.Contains("active") &&
                    !Need<Button>(rail, "RailCadButton").Classes.Contains("active"),
                "R2-14: area buttons must clear .active while SolverSetupDocument is active");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true &&
                    !host.RightSidebarShown,
                "R2-05: SolverSetupDocument must auto-collapse CAD Left and Right docks");

            // Closing SolverSetup returns to Analysis and restores prior docks + RailAnalysisButton .active.
            host.CloseSolverSetup();
            Settle(window);
            Require(!Need<Button>(rail, "RailSolverSetupButton").Classes.Contains("active") &&
                    Need<Button>(rail, "RailAnalysisButton").Classes.Contains("active"),
                "Closing SolverSetup must restore active area highlight on ActivityRail");
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true &&
                    host.RightSidebarShown,
                "Closing SolverSetup back to Analysis must restore prior CAD docks");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckWave12AShellChromeAndTokens()
    {
        // 1. C3 / R2-20 CommandTable parity: view.cad added, view.sweeps titled "Experiment", "Analysis" palette search == 1 match.
        var cadRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "view.cad")
            ?? throw new InvalidOperationException("CommandTable missing view.cad");
        Require(cadRow.Title == "CAD" && cadRow.Menu == "View" && !cadRow.Modal,
            $"view.cad must be Title='CAD', Menu='View', Modal=false (got {cadRow})");
        Require(ShellHost.IsPaneCommand("view.cad"), "view.cad must be routed via ShellHost.IsPaneCommand");
        var expRow = CommandTable.Rows.Single(r => r.Id == "view.sweeps");
        Require(expRow.Title == "Experiment", $"view.sweeps title must be 'Experiment', got '{expRow.Title}'");
        Require(!ShellHost.BottomPanelInAnalysis.Contains("CAD | Analysis toggle", StringComparison.Ordinal),
            $"BottomPanelInAnalysis still references retired toggle: '{ShellHost.BottomPanelInAnalysis}'");

        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());
            Settle(window);

            // Palette search for "Analysis" must yield exactly 1 match (C3).
            host.OpenPalette();
            host.PaletteSearch.Text = "Analysis";
            Require(host.PaletteMatches.Count == 1 && host.PaletteMatches[0].Id == "view.analysis",
                $"Expected 1 'Analysis' palette match, got {host.PaletteMatches.Count}");
            host.PaletteSearch.RaiseEvent(new Avalonia.Input.KeyEventArgs
            {
                RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
                Source = host.PaletteSearch,
                Key = Avalonia.Input.Key.Escape
            });
            Settle(window);

            // R2-20: view.<area> commands activate ModelDocument from SolverSetupDocument and enter target area (no accidental toggle-back).
            host.OpenSolverSetup();
            Settle(window);
            Pump(host.RunCommand("view.setup"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument) &&
                    controller.IsSetup, "view.setup from SolverSetup must activate ModelDocument and enter Setup");
            Pump(host.RunCommand("view.setup"));
            Settle(window);
            Require(controller.IsSetup, "Repeating view.setup must stay in Setup (not toggle back to CAD)");

            host.OpenSolverSetup();
            Settle(window);
            Pump(host.RunCommand("view.sweeps"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument) &&
                    controller.IsSweeps, "view.sweeps from SolverSetup must activate ModelDocument and enter Experiment");
            Pump(host.RunCommand("view.sweeps"));
            Settle(window);
            Require(controller.IsSweeps, "Repeating view.sweeps must stay in Experiment");

            host.OpenSolverSetup();
            Settle(window);
            Pump(host.RunCommand("view.run"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument) &&
                    controller.IsRun, "view.run from SolverSetup must activate ModelDocument and enter Run");
            Pump(host.RunCommand("view.run"));
            Settle(window);
            Require(controller.IsRun, "Repeating view.run must stay in Run");

            host.OpenSolverSetup();
            Settle(window);
            Pump(host.RunCommand("view.results"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument) &&
                    controller.IsResults, "view.results from SolverSetup must activate ModelDocument and enter Results");
            Pump(host.RunCommand("view.results"));
            Settle(window);
            Require(controller.IsResults, "Repeating view.results must stay in Results");

            host.OpenSolverSetup();
            Settle(window);
            Pump(host.RunCommand("view.cad"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument) &&
                    controller.ActiveArea == WorkbenchArea.Cad, "view.cad from SolverSetup must activate ModelDocument and enter CAD");

            // R2-05 user-closed dock memory: closing Left dock manually in CAD, entering Setup, and returning keeps Left dock closed.
            host.ToggleLeftSidebar();
            Settle(window);
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true,
                "Manual ToggleLeftSidebar should hide LeftToolDock");
            Pump(host.RunCommand("view.setup"));
            Settle(window);
            Pump(host.RunCommand("view.cad"));
            Settle(window);
            Require(host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) != true,
                "Returning to CAD must preserve user-closed LeftToolDock state");
            host.ToggleLeftSidebar();
            Settle(window);

            // R2-21 DN-5 Text scaling hoisted onto ShellHost.Resources so ActivityRail and all workspace children scale uniformly.
            double baseProp = (double)Application.Current!.FindResource("PropFontSize")!;
            double baseNote = (double)Application.Current!.FindResource("PropNoteSize")!;
            double baseTitle = (double)Application.Current!.FindResource("PropTitleSize")!;
            host.SetTextScale(1.5);
            Settle(window);
            Require(host.Resources.TryGetValue("PropFontSize", out var hostedProp) && hostedProp is double hp && Math.Abs(hp - baseProp * 1.5) < 0.01,
                $"ShellHost.Resources['PropFontSize'] should be {baseProp * 1.5}, got {hostedProp}");
            Require(host.Resources.TryGetValue("PropNoteSize", out var hostedNote) && hostedNote is double hn && Math.Abs(hn - baseNote * 1.5) < 0.01,
                $"ShellHost.Resources['PropNoteSize'] should be {baseNote * 1.5}, got {hostedNote}");
            Require(host.Resources.TryGetValue("PropTitleSize", out var hostedTitle) && hostedTitle is double ht && Math.Abs(ht - baseTitle * 1.5) < 0.01,
                $"ShellHost.Resources['PropTitleSize'] should be {baseTitle * 1.5}, got {hostedTitle}");
            var badgeCad = Need<TextBlock>(host.ActivityRail, "BadgeCadText");
            Require(Math.Abs(badgeCad.FontSize - baseNote * 1.5) < 0.01,
                $"ActivityRail BadgeCadText did not inherit scaled PropNoteSize (expected {baseNote * 1.5}, got {badgeCad.FontSize})");
            host.SetTextScale(1.0);
            Settle(window);

            // R2-09: Global ToggleButton / Slider / ProgressBar / Expander tokens and HC ActivityRail :pressed badge contrast.
            var stylesText = File.ReadAllText(FindRepoFile(Path.Combine("src", "CfdWorkbench.Desktop", "Styles.axaml")));
            Require(!stylesText.Contains("SweepNavbarClearance", StringComparison.Ordinal),
                "Unused SweepNavbarClearance token must be removed from Styles.axaml");
            foreach (var variant in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark, NativeReviewThemes.HighContrast })
            {
                window.RequestedThemeVariant = variant;
                Pump(host.RunCommand("view.setup"));
                Settle(window);
                var subTab = Need<ToggleButton>(host.ModelView.SetupView, "SubTabBriefButton");
                subTab.TryFindResource("PrimaryBrush", variant, out var primaryRes);
                subTab.TryFindResource("OnPrimaryBrush", variant, out var onPrimaryRes);
                var primaryBrush = (Avalonia.Media.ISolidColorBrush)primaryRes!;
                var onPrimaryBrush = (Avalonia.Media.ISolidColorBrush)onPrimaryRes!;
                var presenter = subTab.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                    .First(p => p.Name == "PART_ContentPresenter");
                Require(presenter.Background is Avalonia.Media.ISolidColorBrush bg && bg.Color == primaryBrush.Color,
                    $"Checked ToggleButton in {variant.Key} must use PrimaryBrush ({primaryBrush.Color}), got {presenter.Background}");
                Require(presenter.Foreground is Avalonia.Media.ISolidColorBrush fg && fg.Color == onPrimaryBrush.Color,
                    $"Checked ToggleButton in {variant.Key} must use OnPrimaryBrush ({onPrimaryBrush.Color}), got {presenter.Foreground}");

                // Slider (ResultsView.ReplayScrubSlider) & ProgressBar (SweepsView.LocalRun.RunProgressBar) resolve PrimaryBrush.
                var slider = Need<Slider>(host.ModelView.ResultsView, "ReplayScrubSlider");
                Require(slider.Foreground is Avalonia.Media.ISolidColorBrush sfg && sfg.Color == primaryBrush.Color,
                    $"Slider in {variant.Key} must use PrimaryBrush ({primaryBrush.Color}), got {slider.Foreground}");
                var progress = Need<ProgressBar>(host.ModelView.SweepsView.LocalRun, "RunProgressBar");
                Require(progress.Foreground is Avalonia.Media.ISolidColorBrush pfg && pfg.Color == primaryBrush.Color,
                    $"ProgressBar in {variant.Key} must use PrimaryBrush ({primaryBrush.Color}), got {progress.Foreground}");

                // Expander (SolverSetup) uses theme SurfaceBrush while PropertiesPane Expander.prop-group stays transparent (C4).
                subTab.TryFindResource("SurfaceBrush", variant, out var surfaceRes);
                var surfaceBrush = (Avalonia.Media.ISolidColorBrush)surfaceRes!;
                host.OpenSolverSetup();
                Settle(window);
                var slvExpander = Need<Expander>(host.SolverSetup, "OtherOptionsExpander");
                Require(slvExpander.Background is Avalonia.Media.ISolidColorBrush ebg && ebg.Color == surfaceBrush.Color,
                    $"Expander in {variant.Key} must use SurfaceBrush ({surfaceBrush.Color}), got {slvExpander.Background}");
                host.CloseSolverSetup();
                Settle(window);

                // Check HC rail-item:pressed TextBlock.rail-badge contrast uses OnSelectionBrush (not MutedBrush).
                if (variant == NativeReviewThemes.HighContrast)
                {
                    var cadBtn = Need<Button>(host.ActivityRail, "RailCadButton");
                    var badge = Need<TextBlock>(host.ActivityRail, "BadgeCadText");
                    var pseudoProp = typeof(StyledElement).GetProperty("PseudoClasses",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                    var pseudos = (IPseudoClasses)pseudoProp.GetValue(cadBtn)!;
                    pseudos.Add(":pressed");
                    Settle(window);
                    try
                    {
                        badge.TryFindResource("OnSelectionBrush", variant, out var onSelRes);
                        var onSel = (Avalonia.Media.ISolidColorBrush)onSelRes!;
                        Require(badge.Foreground is Avalonia.Media.ISolidColorBrush bfg && bfg.Color == onSel.Color,
                            $"HC Button.rail-item:pressed TextBlock.rail-badge must use OnSelectionBrush ({onSel.Color}), got {badge.Foreground}");
                    }
                    finally
                    {
                        pseudos.Remove(":pressed");
                        Settle(window);
                    }
                }
            }
            window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            Settle(window);

            // R2-24: StatusStrip.FitItems() drops SetupItem and idle SweepItem before SolverItem at narrow widths,
            // while protecting running SweepItem (canCancel: true).
            window.Width = 520;
            host.StatusStrip.ShowSetupState("Setup: Linked (r1)", visible: true);
            host.StatusStrip.ShowSweepState("Sweep: 1 stored", visible: true, canCancel: false, onCancel: null);
            Settle(window);
            var setupItem = Need<Border>(host.StatusStrip, "SetupItem");
            var sweepItem = Need<Border>(host.StatusStrip, "SweepItem");
            var solverItem = Need<Border>(host.StatusStrip, "SolverItem");
            Require(!setupItem.IsVisible || !sweepItem.IsVisible,
                "R2-24: FitItems() must drop SetupItem / idle SweepItem at narrow width before crowding StatusMessage");
            Require(solverItem.IsVisible, "SolverItem must remain visible ahead of dropped SetupItem / idle SweepItem");

            // Active running sweep with Cancel must be protected even at narrow width (UX-21).
            host.StatusStrip.ShowSweepState("Sweep: 1 / 4", visible: true, canCancel: true, onCancel: () => { });
            Settle(window);
            Require(sweepItem.IsVisible && Need<Button>(host.StatusStrip, "SweepCancelButton").IsVisible,
                "R2-24 / UX-21: FitItems() must NEVER drop running SweepItem with Cancel button");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckRunWorkspaceView()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());

            var snap = controller.ActiveSession.Snapshot();
            string meshHash = new string('a', 64);

            // Seed one Completed converged run + one MeshGate-failed run into the real cfdw-project-2 cfd ledger.
            var convergedRun = BuildCfdRunRecord(
                snap,
                meshHash,
                alphaDeg: 4.0,
                speedMs: 7.2,
                meshGatePassed: true,
                screenOnly: false,
                converged: true,
                failedCode: null,
                failedReason: null);

            var failedMeshRun = BuildCfdRunRecord(
                snap,
                meshHash,
                alphaDeg: 6.0,
                speedMs: 7.2,
                meshGatePassed: false,
                screenOnly: false,
                converged: false,
                failedCode: "MESH-GATE-FAIL",
                failedReason: "Max non-orthogonality 74.5 deg exceeds 70 deg threshold.");

            controller.RecordCfdRun(convergedRun);
            controller.RecordCfdRun(failedMeshRun);

            controller.EnterRun();
            Settle(window);

            var runView = host.ModelView.RunView;
            Require(runView.IsVisible, "RunWorkspaceView must be visible");

            // Verify all 11 state pills exist in the status pipeline display.
            Require(runView.DisplayedStateCount == 11, $"expected 11 state slots in pipeline display, got {runView.DisplayedStateCount}");

            // Select converged run -> verify LAB-01 three-part Converged label and MeshGate pass metrics.
            runView.SelectRun(convergedRun.RunId);
            Settle(window);
            string convergedText = NeedText(runView, "ConvergedLabelText");
            Require(convergedText.Contains("Converged:", StringComparison.Ordinal) &&
                    convergedText.Contains("outputs complete", StringComparison.Ordinal) &&
                    convergedText.Contains("grid uncertainty not quantified (single mesh)", StringComparison.Ordinal),
                $"Converged label missing LAB-01 three parts: '{convergedText}'");
            string nonOrtho = NeedText(runView, "MeshGateNonOrthoText");
            Require(nonOrtho.Contains("42.3", StringComparison.Ordinal) && nonOrtho.Contains("70", StringComparison.Ordinal),
                $"MeshGate non-orthogonality readout missing observed/threshold: '{nonOrtho}'");
            string clText = NeedText(runView, "ForcesClText");
            Require(clText.Contains("0.412", StringComparison.Ordinal), $"Forces CL readout missing value: '{clText}'");

            // Select failed MeshGate run -> verify failure breakdown + offline key-free Explain this failure & Preview case diff.
            runView.SelectRun(failedMeshRun.RunId);
            Settle(window);
            Require(NeedText(runView, "SelectedRunStateText").Contains("Failed", StringComparison.Ordinal),
                "Failed run must display Failed state");
            Require(Need<ListBox>(runView, "MeshGateFailuresList").ItemCount >= 1,
                "Failed MeshGate must display failing checks");

            // Offline AI-01 Explain this failure (zero network calls, cites log line numbers).
            runView.SetLogExcerptForInspection("line 1 info\nline 2 FATAL: non-orthogonality exceeded\nline 3 done");
            Click(Need<Button>(runView, "ExplainFailureButton"));
            Settle(window);
            string explainSummary = NeedText(runView, "FailureExplanationSummaryText");
            string citedLines = NeedText(runView, "FailureCitedLinesText");
            Require(explainSummary.Contains("MESH-GATE-FAIL", StringComparison.Ordinal),
                $"Explain this failure did not cite failure code: '{explainSummary}'");
            Require(citedLines.Contains("2", StringComparison.Ordinal),
                $"Explain this failure did not cite line 2: '{citedLines}'");

            // Offline AI-11 Preview case diff validates without auto-launching and refuses queued mutation.
            Click(Need<Button>(runView, "PreviewCaseDiffButton"));
            Settle(window);
            string diffResult = NeedText(runView, "CaseDiffResultText");
            Require(diffResult.Contains("Draft", StringComparison.Ordinal),
                $"Preview case diff must state new Draft version creation: '{diffResult}'");

            // Set up a solver button jumps cleanly to SolverSetupDocument.
            Click(Need<Button>(runView, "SetUpSolverButton"));
            Settle(window);
            Require(ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.SolverSetupDocument),
                "RunWorkspaceView 'Set up a solver' must jump to SolverSetupDocument");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckResultsWorkspaceView()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(controller.OpenExampleAsync());

            var snap = controller.ActiveSession.Snapshot();
            string meshHashA = new string('a', 64);
            string meshHashB = new string('b', 64);

            // Sample 1: Completed CFD run WITHOUT wall tau_w, WITH Q isosurface (vortex-core candidates only — Separation MUST stay disabled!).
            var runNoTauW = BuildCfdRunRecord(snap, meshHashA, 2.0, 7.2, true, false, true, null, null);
            controller.RecordCfdRun(runNoTauW);
            controller.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                Guid.NewGuid().ToString("D"),
                runNoTauW.RunId,
                "",
                "surface",
                "postProcessing/wingSurface/500/wing.raw",
                new string('1', 64),
                meshHashA,
                ["Cp"],
                "face",
                [new FieldRangeRecord("Cp", "dimensionless", -1.6, 1.0, "sample-range")],
                "crameri-8.0",
                "cfdw-harvester",
                "1.0.0",
                HasWallShearStress: false,
                SeparationCriterion: null,
                HasTurbulentKineticEnergy: false,
                IsTransient: false,
                Timesteps: [])));
            controller.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                Guid.NewGuid().ToString("D"),
                runNoTauW.RunId,
                "",
                "isosurface",
                "postProcessing/vortex/500/q.raw",
                new string('2', 64),
                meshHashA,
                ["Q"],
                "face",
                [new FieldRangeRecord("Q", "1/s^2", 0.0, 120.0, "sample-range")],
                "crameri-8.0",
                "cfdw-harvester",
                "1.0.0",
                HasWallShearStress: false,
                SeparationCriterion: null,
                HasTurbulentKineticEnergy: false,
                IsTransient: false,
                Timesteps: [])));

            // Sample 2: Failed CFD run on same meshHashA (for Replay failed-sample pause & clear).
            var runFailed = BuildCfdRunRecord(snap, meshHashA, 4.0, 7.2, true, false, false, "CFD-NO-OUTPUTS", "Solver exited 0 with no required outputs");
            controller.RecordCfdRun(runFailed);

            // Sample 3: Completed CFD run on DIFFERENT meshHashB WITH wall tau_w (Separation enabled; DifferenceFlood vs Sample 1 -> "Unavailable — mesh differs").
            var runWithTauW = BuildCfdRunRecord(snap, meshHashB, 6.0, 7.2, true, false, true, null, null);
            controller.RecordCfdRun(runWithTauW);
            controller.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                Guid.NewGuid().ToString("D"),
                runWithTauW.RunId,
                "",
                "surface",
                "postProcessing/wingSurface/500/wing.raw",
                new string('3', 64),
                meshHashB,
                ["Cp", "Cf", "wallShearStress"],
                "face",
                [
                    new FieldRangeRecord("Cp", "dimensionless", -1.9, 1.0, "sample-range"),
                    new FieldRangeRecord("Cf", "dimensionless", 0.001, 0.012, "sample-range")
                ],
                "crameri-8.0",
                "cfdw-harvester",
                "1.0.0",
                HasWallShearStress: true,
                SeparationCriterion: FieldEvidenceManifest.SkinFrictionConvergenceCriterion,
                HasTurbulentKineticEnergy: false,
                IsTransient: false,
                Timesteps: [])));

            // Candidate on current accepted revision.
            var cand = CfdRunRecords.Seal(new CandidateRecord(
                Guid.NewGuid().ToString("D"),
                null,
                "",
                Guid.NewGuid().ToString("D"),
                "eval-001",
                snap.AcceptedId,
                snap.SurfaceHash,
                null,
                "cfd-checked",
                true,
                "cfd",
                [new CandidateObjectiveRecord("L/D", "dimensionless", 22.4)],
                [new CandidateConstraintRecord("cavitation-margin", "ratio", 1.35, 1.15, true)],
                [runWithTauW.RunId],
                [new string('d', 64)],
                [new CandidateParameterDelta("trailing", "cv-1", -0.002, "m")]));
            controller.RecordCandidate(cand);

            controller.EnterResults();
            Settle(window);

            var resultsView = host.ModelView.ResultsView;
            Require(resultsView.IsVisible, "ResultsWorkspaceView must be visible");

            // 1. Sample 1 (no wall tau_w, Q present): Separation toggle MUST be disabled with "No supported criterion in this sample";
            //    VortexCore toggle MUST be enabled and labelled "vortex-core candidates".
            resultsView.SelectSample(runNoTauW.RunId);
            Settle(window);
            var sepToggle = Need<ToggleButton>(resultsView, "LayerSeparationToggle");
            var vortexToggle = Need<ToggleButton>(resultsView, "LayerVortexCoreToggle");
            Require(!sepToggle.IsEnabled, "Separation toggle MUST be disabled when wall tau_w is absent even if Q isosurface exists");
            Require(NeedText(resultsView, "LayerSeparationBasisText").Contains(FieldEvidenceManifest.SeparationUnavailableReason, StringComparison.Ordinal),
                $"Separation basis text must say '{FieldEvidenceManifest.SeparationUnavailableReason}'");
            Require(vortexToggle.IsEnabled, "Vortex-core toggle should be enabled when Q is present");
            Require(NeedText(resultsView, "LayerVortexCoreBasisText").Contains("vortex-core candidates", StringComparison.Ordinal),
                "Vortex-core toggle must be labelled 'vortex-core candidates'");

            // 2. ResultsFieldCanvas paints surface Cp scalar field with PropFontSize >= 11.
            var canvas = Need<ResultsFieldCanvas>(resultsView, "FieldCanvas");
            Require(canvas.RenderedNodeCount > 0, "ResultsFieldCanvas must project and render surface nodes");
            Require(canvas.LastRenderedFontSize >= 11.0, $"ResultsFieldCanvas font size must be >= 11 px, got {canvas.LastRenderedFontSize}");

            // 3. All 9 ProvenanceStrip fields non-empty + linked TableTwin populated.
            string[] provNames =
            [
                "ProvVariableText", "ProvUnitText", "ProvRangeText", "ProvMethodText",
                "ProvSourceRunText", "ProvDepthBasisText", "ProvEvidenceLabelText",
                "ProvColormapText", "ProvAssociationText"
            ];
            foreach (string name in provNames)
                Require(!string.IsNullOrWhiteSpace(NeedText(resultsView, name)), $"ProvenanceStrip field {name} must be non-empty");
            Require(Need<ListBox>(resultsView, "TableTwinList").ItemCount > 0, "Linked TableTwin must hold projected rows");

            // 4. Sample 3 (wall tau_w present): Separation toggle becomes enabled.
            resultsView.SelectSample(runWithTauW.RunId);
            Settle(window);
            Require(sepToggle.IsEnabled, "Separation toggle must be enabled when wall tau_w + supported criterion exist");

            // 5. Replay on compatible meshHashA series (Sample 1 admitted -> Sample 2 failed):
            //    Start Play on frame 0, then advance to failed frame 1 -> MUST pause (IsPlaying=false) and CLEAR canvas/metrics!
            resultsView.BuildReplayForSample(runNoTauW.RunId);
            Settle(window);
            Require(NeedText(resultsView, "ReplayBannerText") == ReplaySequence.SeriesBanner,
                "Replay must show exact series banner");
            Click(Need<Button>(resultsView, "ReplayPlayButton"));
            Settle(window);
            Require(resultsView.ActiveReplay?.IsPlaying == true, "Replay Play did not start playback on admitted sample");

            Click(Need<Button>(resultsView, "ReplayStepForwardButton"));
            Settle(window);
            Require(resultsView.ActiveReplay?.IsPlaying == false, "Stepping onto failed sample MUST pause playback");
            Require(resultsView.ActiveReplay?.ActiveFieldData is null && resultsView.ActiveReplay?.ActiveMetrics is null,
                "Stepping onto failed sample MUST clear ActiveFieldData and ActiveMetrics");
            Require(canvas.IsClearedForFailedSample, "ResultsFieldCanvas MUST clear displayed scalar field on failed sample");
            Require(Need<Border>(resultsView, "ReplayFailureBanner").IsVisible,
                "Replay failure banner must be visible on failed sample");

            // Range unlock shows exact label.
            Click(Need<ToggleButton>(resultsView, "ReplayLockRangeToggle"));
            Settle(window);
            Require(NeedText(resultsView, "ReplayRangeLabelText") == ReplaySequence.RangeUnlockedLabel,
                $"Unlocked range must display '{ReplaySequence.RangeUnlockedLabel}'");

            // 6. DifferenceFlood between Sample 1 (meshHashA) and Sample 3 (meshHashB) -> "Unavailable — mesh differs".
            resultsView.ComputeDifferenceBetween(runNoTauW.RunId, runWithTauW.RunId, "Cp");
            Settle(window);
            Require(NeedText(resultsView, "DiffStatusText").Contains(DifferenceFlood.MeshDiffersReason, StringComparison.Ordinal),
                $"Mismatched mesh difference flood must show '{DifferenceFlood.MeshDiffersReason}', got '{NeedText(resultsView, "DiffStatusText")}'");

            // 7. ParaView handoff honest absence string when absent.
            resultsView.ParaViewLocatorOverride = new AbsentParaViewLocator();
            resultsView.RefreshFromController();
            Settle(window);
            Require(!Need<Button>(resultsView, "OpenParaViewButton").IsEnabled,
                "Open in ParaView must be disabled when ParaView 5.12+ is absent");
            Require(NeedText(resultsView, "ParaViewStatusText") == CfdWorkbench.Analysis.Labels.ParaViewNotFound,
                $"ParaView absence copy mismatch: '{NeedText(resultsView, "ParaViewStatusText")}'");

            // 8. CandidateLadder Accept as draft jumps cleanly to CAD draft without mutating accepted bytes.
            string acceptedBefore = controller.AcceptedSource;
            resultsView.SelectCandidate(cand.CandidateId);
            Settle(window);
            var acceptBtn = Need<Button>(resultsView, "AcceptAsDraftButton");
            Require(acceptBtn.IsEnabled, "Accept as draft must be enabled when base AcceptedId matches");
            Click(acceptBtn);
            Settle(window);
            Require(controller.ActiveArea == WorkbenchArea.Cad, "Accept as draft must jump to CAD");
            Require(controller.ActiveCandidateDraft is not null && controller.ActiveCandidateDraft.CandidateId == cand.CandidateId,
                "Accept as draft must open typed CandidateGeometryDraft on controller");
            Require(controller.AcceptedSource == acceptedBefore,
                "COMMIT-01 violated: Accept as draft must NEVER mutate accepted geometry directly");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckCopyAndCommandTable()
    {
        // CommandTable view.run and view.results under "View" with Modal: false.
        var runRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "view.run")
            ?? throw new InvalidOperationException("CommandTable missing view.run");
        var resultsRow = CommandTable.Rows.SingleOrDefault(r => r.Id == "view.results")
            ?? throw new InvalidOperationException("CommandTable missing view.results");
        Require(runRow.Menu == "View" && !runRow.Modal, "view.run must be in View menu with Modal: false");
        Require(resultsRow.Menu == "View" && !resultsRow.Modal, "view.results must be in View menu with Modal: false");
        Require(ShellHost.IsPaneCommand("view.run") && ShellHost.IsPaneCommand("view.results"),
            "view.run and view.results must be routed via ShellHost.IsPaneCommand");

        // DESIGN.md COPY-806..865 proposed rows exist with zero raw hexes.
        string designPath = FindRepoFile("DESIGN.md");
        string designText = File.ReadAllText(designPath);
        var rowRegex = new Regex(@"^\|\s*COPY-(\d+)\s*\|\s*(.+?)\s*\|\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var hexRegex = new Regex(@"#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b", RegexOptions.CultureInvariant);
        var foundIds = new HashSet<int>();
        var errors = new List<string>();
        foreach (Match m in rowRegex.Matches(designText))
        {
            int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if (id is < 806 or > 865) continue;
            foundIds.Add(id);
            string body = m.Groups[2].Value;
            if (!body.Contains(" — approved —", StringComparison.Ordinal) &&
                !body.Contains(" — proposed —", StringComparison.Ordinal))
                errors.Add($"COPY-{id} must be marked '— approved —' or '— proposed —'");
            if (hexRegex.IsMatch(body))
                errors.Add($"COPY-{id} contains a raw hex color");
        }
        for (int id = 806; id <= 865; id++)
        {
            if (!foundIds.Contains(id))
                errors.Add($"DESIGN.md missing COPY-{id}");
        }
        if (errors.Count > 0)
            throw new InvalidOperationException("Wave 10D DESIGN.md copy check failed: " + string.Join("; ", errors));
    }

    private static CfdRunRecord BuildCfdRunRecord(
        SessionView snap,
        string meshHash,
        double alphaDeg,
        double speedMs,
        bool meshGatePassed,
        bool screenOnly,
        bool converged,
        string? failedCode,
        string? failedReason)
    {
        string runId = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var op = OperatingPoints.Custom(speedMs, alphaDeg, 0.50);
        var reference = new RunReference(0.10, 1.0, 0.12, "root-le", "x-aft,y-stbd,z-up");
        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, [new string('c', 64)], "cfdw-cv/2", "rule-a/1");
        var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720-20260127", new string('e', 64), true);

        var failures = meshGatePassed
            ? Array.Empty<MeshGateFailure>()
            : [new MeshGateFailure("MaxNonOrthogonalityDeg", 74.5, 70.0, "Max non-orthogonality 74.5 deg exceeds 70 deg threshold.")];

        var meshGate = new MeshGateResult(
            Passed: meshGatePassed,
            Branch: "wall-resolved",
            MaxNonOrthogonalityDeg: meshGatePassed ? 42.3 : 74.5,
            MaxInternalSkewness: 1.8,
            MaxBoundarySkewness: 3.4,
            MinCellVolume: 1.2e-11,
            MinCellDeterminantLayer: 0.042,
            MinCellDeterminantCore: 0.68,
            LayerCoverage20PlusFraction: 0.985,
            LayerCoverage10PlusFraction: 1.0,
            RegionCoverage: [new RegionLayerCoverage("leading-edge", 20, 0.99, 1.0)],
            YPlusP95: screenOnly ? null : 0.62,
            YPlusMax: screenOnly ? null : 1.38,
            IsScreenOnly: screenOnly,
            Failures: failures);

        ResidualSummary? residuals = failedCode is not null
            ? null
            : new ResidualSummary(
                MaxInitialResidualRatio: converged ? 4.2e-4 : 5.0e-3,
                ClWindowMean: 0.412,
                CdWindowMean: 0.0184,
                CmWindowMean: -0.041,
                ClHalfBandUi: 1.8e-4,
                CdHalfBandUi: 4.5e-5,
                ClHalfBandCap: 1.0e-3,
                CdHalfBandCap: 2.0e-4,
                AveragingWindowIterations: 200,
                TurbulenceBoundingCountInWindow: 0,
                RanToIterationLimit: !converged,
                CriterionMet: converged);

        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, now)];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now.AddSeconds(1));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now.AddSeconds(2));
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now.AddSeconds(3));
        if (failedCode is not null && !meshGatePassed)
        {
            events = CfdRunStateMachine.TransitionToFailed(events, now.AddSeconds(4), failedCode, failedReason ?? "Failed");
        }
        else if (failedCode is not null)
        {
            events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now.AddSeconds(4));
            events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now.AddSeconds(5));
            events = CfdRunStateMachine.TransitionToFailed(events, now.AddSeconds(6), failedCode, failedReason ?? "Failed");
        }
        else
        {
            events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now.AddSeconds(4));
            events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now.AddSeconds(5));
            events = CfdRunStateMachine.AppendEvent(events, converged ? CfdRunState.Completed : CfdRunState.CompletedResidualNotMet, now.AddSeconds(6));
        }

        string caseModelHash = new string('f', 64);
        string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, caseModelHash);
        return CfdRunRecords.Seal(new CfdRunRecord(
            runId,
            null,
            null,
            0,
            runKey,
            "",
            caseModelHash,
            meshHash,
            pin,
            inputs,
            water,
            op,
            reference,
            meshGate,
            residuals,
            new CfdRunResources(1_840_000, 4, 142_300, 512.4, 2_147_483_648),
            null,
            events));
    }

    private sealed class AbsentParaViewLocator : IParaViewLocator
    {
        public string? FindExecutable(string? explicitOverridePath = null) => null;
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
