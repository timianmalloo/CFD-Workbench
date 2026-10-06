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
                var sectionRows = controller.AnalysisView.Groups.Single(group => group.Title == "Section (2D)").Rows;
                var sectionTable = panel.GetLogicalDescendants().OfType<StackPanel>()
                    .Single(table => table.Name == "section-table");
                var renderedRows = sectionTable.Children.OfType<Grid>().ToArray();
                Equal(sectionRows.Count, renderedRows.Length, "all section rows rendered");
                for (int i = 0; i < sectionRows.Count; i++)
                {
                    if (!sectionRows[i].Value.Any(char.IsDigit)) continue;
                    var cells = renderedRows[i].Children.OfType<TextBlock>().ToArray();
                    Equal(sectionRows[i].Note, cells[2].Text, "numeric tier note rendered for " + sectionRows[i].Label);
                    Equal(true, !string.IsNullOrWhiteSpace(cells[2].Text), "numeric tier note visible for " + sectionRows[i].Label);
                }
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
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        if (shared.IsValueCreated) shared.Value.Dispose();
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
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);

        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            Fail ? throw new ContractError("ANA-SOLVE-RESIDUAL", "the lattice residual is above tolerance") : inner.Solve(sections, op, water, cancellation);

        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
            WaterRecord water, CancellationToken cancellation) => inner.Couple(sections, solution, op, water, cancellation);
    }
}
