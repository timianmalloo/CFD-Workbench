using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track PNA (design area3-analysis.md §18.2, §18.5 rows 18-31): the Layers pane, the Analysis bottom panel and its chart
/// twin, the Properties Analysis groups, and the shell slots that carry them. Ring D, est. 1.5 s (one evaluation shared).
/// </summary>
public static class AnalysisPanelTests
{
    public static void Run()
    {
        // One controller, one evaluation (small lattice) serve the checks in order; the failing evaluation is last.
        var method = new FlakyMethod(new ProductWingMethod(
            Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
        var shared = new Lazy<WorkbenchController>(() =>
        {
            var controller = new WorkbenchController(analysisMethod: method);
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            _ = Evaluate(controller, 2);
            return controller;
        });
        try
        {
            DesktopChecks.Check("LayersPane_HideLayer_ViewDropsLayerTwinKept", () =>
            {
                var controller = shared.Value;
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                var layers = new LayersPane();
                layers.Bind(controller);
                var panel = new AnalysisPanel();
                panel.Bind(controller);
                var gamma = layers.CheckFor("plan-gamma") ?? throw new Exception("no plan-gamma check");
                Equal(true, gamma.IsChecked, "visible at first");
                int stripRows = TableRows(panel, "strips-table"), twinRows = panel.LoadingView.TwinRowCount;
                Equal(true, stripRows > 0 && twinRows > 0, "twin rows drawn");
                int raised = 0;
                layers.LayerVisibilityChanged += () => raised++;
                gamma.IsChecked = false;
                Equal(1, raised, "the shell was told");
                Equal(false, controller.IsLayerVisible("plan-gamma"), "controller flag");
                Equal(false, controller.LayerSet.Single(layer => layer.Id == "plan-gamma").Visible, "the view drops the layer");
                Equal(true, controller.LayerSet.Where(layer => layer.Id != "plan-gamma").All(layer => layer.Visible), "others stay");
                panel.Bind(controller);
                Equal(stripRows, TableRows(panel, "strips-table"), "the layer's table twin is kept");
                Equal(twinRows, panel.LoadingView.TwinRowCount, "the chart twin is kept");
                layers.Bind(controller);
                Equal(false, layers.CheckFor("plan-gamma")!.IsChecked, "the pane follows the flag after a re-read");
                gamma.IsChecked = true;
                Equal(true, controller.LayerSet.All(layer => layer.Visible), "shown again");
            });

            DesktopChecks.Check("PropertiesView_Analysis_StationShowsStripOfWingRun", () =>
            {
                var controller = shared.Value;
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                var view = controller.AnalysisView;
                var projection = controller.CurrentProjection ?? throw new Exception("no projection");
                var context = new PropertiesContext(controller.Planform, Analysis: view);
                int tip = projection.Assignments.Count - 1;
                foreach (var (index, eta) in new[] { (1, 0.5), (tip, 1.0) })
                {
                    var model = PropertiesView.Build(new Selection.Station(index, eta), projection, controller.Estimates, ShellMode.Analysis, context);
                    var strip = AnalysisProjection.StripAt(view, eta);
                    var group = model.Groups.SingleOrDefault(item => item.Id == "ana-strip") ?? throw new Exception("no strip group at eta " + eta);
                    Equal(strip.Title, group.Title, "the strip header");
                    Equal(true, group.Title.StartsWith(Labels.StripHeader, StringComparison.Ordinal), group.Title);
                    Equal(strip.Rows.Count, group.Rows.Count, "row count");
                    for (int i = 0; i < strip.Rows.Count; i++)
                    {
                        Equal(strip.Rows[i].Value, group.Rows[i].Value, "verbatim value of " + strip.Rows[i].Label);
                        Equal(strip.Rows[i].Note, group.Rows[i].Description, "verbatim note of " + strip.Rows[i].Label);
                    }
                    string verdict = group.Rows.Single(row => row.Label == "Envelope (this strip)").Value;
                    Equal(eta == 1.0, verdict == Labels.TipNotJudged, "only the outermost strip reads Not judged: " + verdict);
                    Equal(true, group.Rows.Single(row => row.Label == "cd (profile)").Value.StartsWith("Unavailable", StringComparison.Ordinal), "cd Unavailable");
                    Equal(true, model.Groups.Any(item => item.Title == "Section (2D)"), "section group");
                    Equal(false, model.Groups.SelectMany(item => item.Rows).Any(row => row.Kind == RowKind.Action), "no Edit section link in Analysis");
                    Equal(true, model.Wing!.Rows.Where(row => row.Key.StartsWith("w:", StringComparison.Ordinal)).All(row => row.State == RowState.Locked), "Wing read-only");
                }
                var foil = PropertiesView.Build(new Selection.None(), projection, controller.Estimates, ShellMode.Analysis, context);
                Equal("Wing result,Conditions,Labels,Section (2D)", string.Join(",", foil.Groups.Select(item => item.Title)), "foil groups");
                var envelope = view.Groups.Single(item => item.Title == "Wing result").Rows.Single(row => row.Label == "Envelope");
                Equal(envelope.Value, foil.Groups[0].Rows.Single(row => row.Label == "Envelope").Value, "run verdict verbatim");
                var running = PropertiesView.Build(new Selection.None(), projection, controller.Estimates, ShellMode.Analysis,
                    context with { Analysis = view with { State = RunState.Running } });
                Equal("Evaluating", running.Groups[0].Title, "skeleton while running");
                var failed = PropertiesView.Build(new Selection.None(), projection, controller.Estimates, ShellMode.Analysis,
                    context with { Analysis = view with { State = RunState.Failed, ErrorCard = "Analysis failed — x (ANA-SOLVE-RESIDUAL). The previous result is kept as Historical." } });
                Equal("Analysis failed", failed.Groups[0].Title, "error card group");
                Equal(true, failed.Groups[0].Notes.Single().Text.StartsWith("Analysis failed", StringComparison.Ordinal), "error card text");
            });

            DesktopChecks.Check("PropertiesPane_Analysis_BuildsWithAnalysisModeAndContext", () =>
            {
                var controller = shared.Value;
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                var host = new ShellHost(controller);
                var window = new Window { Content = host, Width = 1280, Height = 800 };
                try
                {
                    window.Show();
                    host.RefreshPanes();
                    Settle(window);
                    var texts = Texts(host.Properties);
                    Equal(true, texts.Contains("Wing result") && texts.Contains("CL") && texts.Contains("Envelope"), "Analysis groups rendered: " + string.Join("|", texts.Take(12)));
                    Equal(false, texts.Contains(Labels.NoResult), "not the no-result text");
                }
                finally { window.Close(); }
            });

            DesktopChecks.Check("AnalysisPanel_Tabs_BoundToSelectedRun", () =>
            {
                var controller = shared.Value;
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                var panel = new AnalysisPanel();
                panel.Bind(controller);
                var tabs = panel.FindControl<TabControl>("PanelTabs") ?? throw new Exception("no tabs");
                Equal("Spanwise loading,Section,Loads,Provenance",
                    string.Join(",", tabs.Items.OfType<TabItem>().Select(tab => tab.Header)), "the tabs (Checks omitted, OD-4 a)");
                string keyA = panel.BoundRunKey ?? throw new Exception("no run key");
                int twin = panel.LoadingView.TwinRowCount, strips = TableRows(panel, "strips-table");
                string loads = LoadsFacts(panel);
                Equal(true, twin > 0 && strips > 0, "bound to the completed run");
                Equal(false, panel.FindControl<Border>("PanelErrorCard")!.IsVisible, "no error card");
                // A failed latest attempt: the tabs keep the previous Completed run (Historical) beside the error card.
                method.Fail = true;
                var failed = Evaluate(controller, 3);
                Equal(true, failed.Outcome is RunOutcome.Failed, "the attempt failed");
                panel.Bind(controller);
                Equal(keyA, panel.BoundRunKey, "still bound to the selected run, not the latest attempt");
                Equal(twin, panel.LoadingView.TwinRowCount, "loading twin kept");
                Equal(strips, TableRows(panel, "strips-table"), "strips kept");
                Equal(loads, LoadsFacts(panel), "the run's own loads kept");
                Equal(true, panel.FindControl<Border>("PanelErrorCard")!.IsVisible, "error card shown");
                Equal(true, panel.FindControl<TextBlock>("PanelErrorText")!.Text!.StartsWith("Analysis failed —", StringComparison.Ordinal), "error text");
                Equal(true, panel.FindControl<TextBlock>("PanelBanner")!.Text!.StartsWith("Historical", StringComparison.Ordinal), "Historical banner");
                method.Fail = false;
            });
        }
        finally { Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }

        DesktopChecks.Check("LoadingChart_TableTwin_TogglesAndKeepsFocus", () =>
        {
            var chart = new LoadingChart();
            var window = new Window { Content = chart, Width = 600, Height = 300 };
            try
            {
                window.Show();
                var toggle = chart.TwinToggle;
                chart.Update(Points(5));
                Settle(window);
                Equal(false, chart.ShowingTable, "chart first");
                if (!toggle.Focus(Avalonia.Input.NavigationMethod.Tab)) throw new Exception("the twin toggle refused focus");
                Equal(true, toggle.IsFocused, "focused before");
                toggle.IsChecked = true;
                Settle(window);
                Equal(true, chart.ShowingTable, "table twin shown");
                Equal(5, chart.TwinRowCount, "twin rows");
                Equal(true, toggle.IsFocused, "focus kept by showing the twin");
                chart.Update(Points(7));
                Settle(window);
                Equal(true, ReferenceEquals(toggle, chart.TwinToggle), "the toggle is the same control");
                Equal(true, toggle.IsFocused, "focus kept when the twin re-renders");
                Equal(true, chart.ShowingTable, "twin still shown");
                Equal(7, chart.TwinRowCount, "twin refilled");
                toggle.IsChecked = false;
                Equal(false, chart.ShowingTable, "chart again");
                Equal(true, toggle.IsFocused, "focus kept by showing the chart");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_AnalysisSlots_BottomPanelInAnalysisOnly_LayersTabInLeftDock", () =>
        {
            using var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(
                Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                host.RefreshPanes();
                Settle(window);
                Equal(false, host.BottomPanelShown, "no bottom panel in CAD");
                Equal("properties,browser,layers",
                    string.Join(",", host.LayoutFactory.LeftToolDock.VisibleDockables!.Select(item => item.Id)), "left tabs");
                host.ToggleBottomPanel();
                Equal(true, StatusText(host).StartsWith("The bottom panel", StringComparison.Ordinal), "⌘J outside Analysis names why");
                Equal(false, host.BottomPanelShown, "still no panel");
                controller.ToggleAnalysis();
                host.RefreshPanes();
                Settle(window);
                Equal(true, host.BottomPanelShown, "the panel shows in Analysis");
                Equal(true, host.AnalysisPanel.GetVisualParent() == host, "a shell slot");
                Equal(true, Grid.GetRow(host.AnalysisPanel) == 1 && Grid.GetRow(host.StatusStrip) == 2, "between the dock and the strip");
                host.ToggleBottomPanel();
                Equal(false, host.BottomPanelShown, "⌘J folds it");
                host.RefreshPanes();
                Equal(false, host.BottomPanelShown, "a refresh keeps it folded");
                host.ToggleBottomPanel();
                Equal(true, host.BottomPanelShown, "⌘J unfolds it");
                controller.ToggleAnalysis();
                host.RefreshPanes();
                Equal(false, host.BottomPanelShown, "gone in CAD");
                var row = CommandTable.Rows.Single(item => item.Id == "window.layers");
                Equal("Window", row.Menu, "window.layers menu");
                Equal(true, ShellHost.IsShellCommand("window.layers") && host.ShellCommandReason("window.layers") is null, "a shell command that runs");
                host.ShowPane("layers");
                Settle(window);
                Equal("layers", host.LayoutFactory.LeftToolDock.ActiveDockable?.Id, "Layers tab active");
                Equal(true, ReferenceEquals(host.LayoutFactory.LayersTool.Context, host.Layers), "pane registered");
                Equal(true, CommandTable.Rows.Any(item => item.Id == "view.toggle-bottom") && CommandTable.PaletteEntries().Any(item => item.Id == "window.layers"), "palette rows");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_SectionDraftHiddenInAnalysis_EvaluateRepaintsProperties", () =>
        {
            using var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(
                Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            Task.Run(() => controller.EnterSectionAsync(1, EntryOrigin.Side)).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                controller.ToggleAnalysis();
                host.RefreshPanes();
                Settle(window);
                Equal(true, controller.Section is not null && controller.IsAnalysis, "draft hidden, Analysis open");
                Equal(true, Texts(host.Properties).Contains(Labels.NoResult), "no result yet");
                _ = Evaluate(controller, 2);
                Settle(window);
                var texts = Texts(host.Properties);
                Equal(true, texts.Contains("CL") && !texts.Contains(Labels.NoResult), "Properties repainted after Evaluate: " + string.Join("|", texts.Take(10)));
                Equal(true, host.AnalysisPanel.BoundRunKey is not null, "the panel repainted too");
            }
            finally { window.Close(); }
        });
        // POL (A3a polish, Ring D, est. 3 s: three small-lattice evaluations, three windows). Each check names the defect it protects.
        DesktopChecks.Check("Properties_AnalysisLayout_OneVisibleScroll_TierChipAboveGroups", () =>
        {
            using var controller = OpenSmall();
            _ = EvaluateAt(controller, 2, 0.6);
            controller.ToggleAnalysis();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1500, Height = 870 };
            try
            {
                window.Show();
                host.RefreshPanes();
                Settle(window);
                var pane = host.Properties;
                var scroll = pane.FindControl<ScrollViewer>("SelectionScroll")!;
                Equal(true, ReferenceEquals(pane.FindControl<Border>("WingBlock")!.Parent, pane.FindControl<StackPanel>("SelectionPanel")),
                    "the Wing follows the groups inside the one scroll");
                Equal(ScrollBarVisibility.Disabled, pane.FindControl<ScrollViewer>("WingScroll")!.VerticalScrollBarVisibility, "no second vertical scroller");
                Equal(false, scroll.AllowAutoHide, "the one scroll shows its bar, so nothing below the fold is hidden");
                Equal(true, scroll.Bounds.Height > 400, "the groups get the pane, not half of it: " + scroll.Bounds.Height);
                var chip = pane.FindControl<Border>("AnalysisChip")!;
                Equal(true, chip.IsVisible && chip.Classes.Contains("modebar-chip"), "the tier is the pill chip");
                Equal(Labels.VlmChip, pane.FindControl<TextBlock>("AnalysisChipText")!.Text, "the chip carries the approved tier text");
                Equal(false, pane.ShownModel!.Blocks.Any(group => group.Rows.Any(row => row.Label == "Tier")), "the Tier is not also a row");
                foreach (string title in new[] { "Wing result", "Labels" })
                {
                    var header = pane.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(text => text.IsEffectivelyVisible && text.Text == title)
                        ?? throw new Exception(title + " group header is not drawn");
                    Equal(true, InView(header, scroll), title + " header is in the first viewport at 1500 x 870");
                }
                var method = pane.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(text => text.IsEffectivelyVisible && text.Text == "Method")
                    ?? throw new Exception("Labels rows are not drawn");
                Equal(true, InView(method, scroll), "the Labels rows start in view");
            }
            finally { window.Close(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }
        });

        DesktopChecks.Check("Analysis_Running_HidesPreviousFailureCard", () =>
        {
            var hold = new FlakyMethod(new ProductWingMethod(Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            using var controller = OpenSmall(hold);
            _ = EvaluateAt(controller, 2, null);
            hold.Fail = true;
            _ = EvaluateAt(controller, 3, null);
            hold.Fail = false;
            Equal(true, controller.AnalysisView.ErrorCard is not null, "the failed attempt shows its card");
            hold.Gate = new ManualResetEventSlim(false);
            var running = Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 4, null), controller.AnalysisWater));
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while (!controller.AnalysisRunning && deadline.Elapsed < TimeSpan.FromSeconds(10)) Thread.Sleep(5);
            try
            {
                Equal(true, controller.AnalysisRunning, "the next Evaluate is running");
                var view = controller.AnalysisView;
                Equal(RunState.Running, view.State, "state");
                Equal(null, view.ErrorCard, "the stale card is hidden while running");
                var panel = new AnalysisPanel();
                panel.Bind(controller);
                Equal(false, panel.FindControl<Border>("PanelErrorCard")!.IsVisible, "the panel's card is hidden too");
            }
            finally
            {
                hold.Gate.Set();
                running.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            }
        });

        DesktopChecks.Check("Tampered_RunView_UnavailableNoLayersRowKept", () =>
        {
            // A stored run whose payload no longer matches its hash: edited on disk, reopened through the controller (no seam needed).
            string path = Path.Combine(Path.GetTempPath(), "pol-tamper-" + Guid.NewGuid().ToString("N") + ".cfdw.json");
            string resaved = path + ".again.cfdw.json";
            try
            {
                using (var source = OpenSmall())
                {
                    _ = EvaluateAt(source, 2, 0.6);
                    var saved = Task.Run(() => source.SaveAsync(path)).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
                    Equal("OK", saved.Code, "the fixture saved");
                }
                string image = File.ReadAllText(path);
                int at = image.IndexOf("\"fz\": ", StringComparison.Ordinal);
                if (at < 0) throw new Exception("the saved image has no stored strip force to edit");
                int end = image.IndexOfAny([',', '\n'], at + 6);
                File.WriteAllText(path, image[..(at + 6)] + "123.25" + image[end..]);
                using var controller = new WorkbenchController();
                var outcome = Task.Run(() => controller.OpenAsync(path)).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
                Equal(true, outcome is OpenOutcome.Opened, "the edited file still opens: " + outcome);
                controller.ToggleAnalysis();
                var view = controller.AnalysisView;
                Equal(RunState.Unavailable, view.State, "state");
                Equal("Analysis: Unavailable", view.StatusText, "status item");
                Equal(Labels.PayloadFailed, view.Groups.Single().Rows.Single(row => row.Label == "Result").Value, "the verdict row");
                Equal(0, view.Layers.Count, "no layers are drawn from a failed payload");
                Equal(true, view.RunKey is not null, "the run row is kept, named by its key");
                var panel = new AnalysisPanel();
                panel.Bind(controller);
                Equal(0, panel.LoadingView.Points.Count, "no loading curve from a failed payload");
                var host = new ShellHost(controller);
                var window = new Window { Content = host, Width = 1280, Height = 800 };
                try
                {
                    window.Show();
                    host.RefreshPanes();
                    Settle(window);
                    Equal(true, Texts(host.Properties).Contains(Labels.PayloadFailed), "Properties shows the verdict");
                }
                finally { window.Close(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }
                // Never deleted: saving again writes the edited value back as it was read.
                Equal("OK", Task.Run(() => controller.SaveAsync(resaved)).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult().Code, "resaved");
                Equal(true, File.ReadAllText(resaved).Contains("123.25", StringComparison.Ordinal), "the tampered row is kept");
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();   // the save's refresh reaches the closed host while the controller is alive
            }
            finally { File.Delete(path); File.Delete(resaved); }
        });

        DesktopChecks.Check("LoadingChart_Axes_TicksTitlesSeriesLabel_OnlyApprovedCopy", () =>
        {
            Equal(5, LoadingChart.XTicks.Count, "x ticks");
            Equal(true, LoadingChart.XTicks[0] == 0 && LoadingChart.XTicks[^1] == 1, "x runs 0 to 1 (root to tip)");
            Equal("0,0.08,0.16", string.Join(",", LoadingChart.YTicks(0.16).Select(v => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))), "y ticks");
            // COPY-238 names Cl·c/c̄ and η; COPY-213 names the tier. No sentence is added.
            Equal(true, Labels.ChartBasis.Contains(LoadingChart.XTitle) && Labels.ChartBasis.Contains(LoadingChart.YTitle), "axis titles are COPY-238 symbols");
            Equal(true, Labels.VlmChip.StartsWith(LoadingChart.SeriesLabel, StringComparison.Ordinal), "the series label is the COPY-213 tier name");
            var chart = new LoadingChart();
            var window = new Window { Content = chart, Width = 600, Height = 300 };
            try
            {
                window.Show();
                chart.Update(Points(9));
                Settle(window);
                using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(600, 300));
                bitmap.Render(window);
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("PlanLayer_LegendRamp_IsTheBatlowTokens_3dPlatesStayInView", () =>
        {
            var scope = new Border();
            var window = new Window { Content = scope, Width = 200, Height = 100 };
            try
            {
                window.Show();
                var stops = PlanLoadLayer.BatlowStops(scope);
                Equal(5, stops.Count, "five stops");
                Equal(true, stops[0] == Avalonia.Media.Color.Parse("#011959") && stops[^1] == Avalonia.Media.Color.Parse("#faccfa"), "the ramp ends are batlow 0 and 4");
            }
            finally { window.Close(); }
            // 3D plates: a label that starts near the right edge slides left and wraps instead of clipping; the lift legend clears the triad plate.
            var (x, width) = LoadLayerText.Within(1100 - 885 + 24, 620);
            Equal(true, x + width <= 620 - 8 && width >= 160, "root-moment label stays inside the view: " + x + " + " + width);
            var (kept, keptWidth) = LoadLayerText.Within(100, 620);
            Equal(true, kept == 100 && keptWidth == 620 - 100 - 8, "a label with room is not moved");
            Equal(true, View3dLoadLayer.LegendLeft >= View3d.CaptionMargin.Left, "the legend plate starts in the caption's column, right of the triad plate");
        });

        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        if (shared.IsValueCreated) shared.Value.Dispose();
    }

    private static WorkbenchController OpenSmall(IWingMethod? method = null)
    {
        var controller = new WorkbenchController(analysisMethod: method ?? new ProductWingMethod(
            Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static AnalysisRun EvaluateAt(WorkbenchController controller, double alpha, double? depth) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, depth), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()
        ?? throw new Exception("Fixture Evaluate returned no run.");

    /// <summary>True when the control sits wholly inside the scroll viewer's visible rectangle, with no scrolling.</summary>
    private static bool InView(Control control, ScrollViewer viewer)
    {
        var top = control.TranslatePoint(default, viewer);
        var bottom = control.TranslatePoint(new Point(0, control.Bounds.Height), viewer);
        return top is not null && bottom is not null && top.Value.Y >= 0 && bottom.Value.Y <= viewer.Bounds.Height;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double alpha) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, null), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()
        ?? throw new Exception("Fixture Evaluate returned no run.");

    private static IReadOnlyList<LoadingPoint> Points(int count) => [.. Enumerable.Range(0, count).Select(i =>
        new LoadingPoint(i / (double)Math.Max(1, count - 1), i == 2 ? null : 0.4 + 0.01 * i, 0.5, 2.0, 12.5))];

    private static int TableRows(AnalysisPanel panel, string name) =>
        (panel.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(item => item.Name == name)
            ?? throw new Exception("table " + name + " is absent")).Children.Count - 1;

    // The run's own numbers, which no feed supplies. (Verdicts and root t/c come from the controller's feed, which is empty
    // for a Failed latest row: a finding for CTX, reported as a seam request, not asserted here.)
    private static string LoadsFacts(AnalysisPanel panel) => string.Join("|", panel.GetLogicalDescendants().OfType<TextBlock>()
        .Select(text => text.Text ?? "").Where(text => text.EndsWith(" N", StringComparison.Ordinal) || text.EndsWith(" m", StringComparison.Ordinal) ||
            text.EndsWith(" N·m", StringComparison.Ordinal) || text.EndsWith(" kPa", StringComparison.Ordinal)));

    private static HashSet<string> Texts(Control root) => root.GetVisualDescendants().OfType<TextBlock>()
        .Where(text => text.IsEffectivelyVisible && !string.IsNullOrEmpty(text.Text)).Select(text => text.Text!).ToHashSet(StringComparer.Ordinal);

    private static string StatusText(ShellHost host) => host.StatusStrip.FindControl<TextBlock>("StatusText")?.Text ?? "";

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }

    /// <summary>A method that solves like the product method, until told to fail with the residual refusal.</summary>
    private sealed class FlakyMethod(IWingMethod inner) : IWingMethod
    {
        public bool Fail { get; set; }

        /// <summary>When set, a solve waits on it (a held run, so a test can look at the Running state).</summary>
        public ManualResetEventSlim? Gate { get; set; }
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);

        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            if (Fail) throw new ContractError("ANA-SOLVE-RESIDUAL", "the lattice residual is above tolerance");
            Gate?.Wait(cancellation);
            return inner.Solve(sections, op, water, cancellation);
        }

        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
            WaterRecord water, CancellationToken cancellation) => inner.Couple(sections, solution, op, water, cancellation);
    }
}
