using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track SMA (Rulings 124, 125): the Section view is a document in the main area, the Section sample is gone, and the bottom
/// panel's Section tab is one summary line with "Open in main area". Ring: readiness (one window, one evaluation shared;
/// the chip strip's own check is in PlanCanvasTests, the pure Section content stays in the Analysis harness, DxSectionTests).
/// </summary>
public static class SectionMainAreaTests
{
    public static void RunReadiness()
    {
        var shared = new Lazy<(WorkbenchController Controller, ShellHost Host, Window Window)>(() =>
        {
            var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(
                Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1500, Height = 870 };
            window.Show();
            controller.ToggleAnalysis();
            controller.Layout = ViewLayout.Four;
            _ = Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 3, 0.6), controller.AnalysisWater))
                .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            host.RefreshPanes();
            Settle(window);
            return (controller, host, window);
        });
        try
        {
            DesktopChecks.Check("SectionMainArea_Tabs_PlanSectionFoilSource_SampleGone", () =>
            {
                var (_, host, window) = shared.Value;
                string[] ids = host.LayoutFactory.MainDocumentDock.VisibleDockables!.Select(d => d.Id).ToArray();
                Equal("model,section,foil-source", string.Join(",", ids), "the main-area documents");
                Equal("Plan,Section,Foil source", string.Join(",", host.LayoutFactory.MainDocumentDock.VisibleDockables!.Select(d => d.Title)), "the tab titles");
                Equal(true, host.LayoutFactory.FindDockable("section-sample") is null, "no Section sample document");
                Equal(true, host.ModelView.FindControl<Control>("SectionViewport") is null && host.ModelView.FindControl<Control>("SectionReadout") is null,
                    "the Section sample controls are gone from the model area");
                Settle(window);
            });
            DesktopChecks.Check("SectionMainArea_Document_FullSize_HeaderNamesSelectedStrip", () =>
            {
                var (controller, host, window) = shared.Value;
                var station = controller.Inspection!.Authored.Assignments[^1];
                controller.Select(new Selection.Station(controller.Inspection.Authored.Assignments.Count - 1, station.Eta));
                host.RefreshPanes();
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var view = host.AnalysisPanel.SectionView;
                Equal(true, view.IsEffectivelyVisible && view.Shown is not null, "the Section document shows the evaluated run");
                Equal(Labels.StationName(view.Shown!.Eta, false), Header(view), "COPY-394 header for a selected strip");
                Equal(false, view.Shown.IsGoverning, "a selected strip is shown, not the governing station");
                Equal(true, view.Bounds.Width >= 900, "the view takes the main area's width, not a 260 px tab: " + view.Bounds.Width);
                Equal(true, view.Chart.Bounds.Height >= 250 && view.ProfileView.Bounds.Height >= 150,
                    $"charts at document size: chart {view.Chart.Bounds.Height}, profile {view.ProfileView.Bounds.Height}");
                Equal(true, Descendants(view).OfType<StackPanel>().Any(p => p.Name == "section-stations-table"), "the Stations table is in the document");
                Equal(true, Descendants(view).OfType<StackPanel>().Any(p => p.Name == "section-cavitation-table"), "the cavitation group is in the document");
            });
            DesktopChecks.Check("SectionMainArea_Document_NoStrip_HeaderNamesGoverningStation", () =>
            {
                var (controller, host, window) = shared.Value;
                controller.Select(new Selection.Foil());
                host.RefreshPanes();
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var view = host.AnalysisPanel.SectionView;
                Equal(true, view.Shown is { IsGoverning: true }, "no strip selected shows the governing station");
                Equal(Labels.StationName(view.Shown!.Eta, true), Header(view), "COPY-395 header with no strip selected");
            });
            DesktopChecks.Check("SectionMainArea_BottomTab_OneLineSummary_OpensDocument", () =>
            {
                var (controller, host, window) = shared.Value;
                controller.Select(new Selection.Foil());
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                host.RefreshPanes();
                var panel = host.AnalysisPanel;
                var tabs = panel.FindControl<TabControl>("PanelTabs")!;
                tabs.SelectedItem = panel.FindControl<TabItem>("SectionTab");
                Settle(window);
                var summary = panel.SectionView.Summary ?? throw new Exception("no summary");
                Equal(summary.Station, panel.FindControl<TextBlock>("SectionSummaryStation")!.Text, "the summary names the shown station");
                Equal(Labels.ClPanel + " " + summary.Cl, panel.FindControl<TextBlock>("SectionSummaryCl")!.Text, "cl (panel)");
                Equal(Labels.CpMinLabel + " " + summary.CpMin, panel.FindControl<TextBlock>("SectionSummaryCp")!.Text, "-Cp_min");
                Equal(true, summary.Cl.Length > 0 && summary.CpMin.Length > 0, "both numbers are present");
                Equal(AnalysisPanel.CompactHeight, panel.Height, "the panel is one summary line high while the Section tab is shown");
                Equal(0, Descendants(panel).OfType<SectionChartView>().Count(), "no chart in the bottom tab");
                var open = panel.FindControl<Button>("OpenInMainAreaButton")!;
                Equal(Labels.OpenInMainArea, (string?)open.Content, "COPY-405 label");
                Equal(true, open.Focusable && open.IsTabStop && open.IsEffectivelyEnabled, "the action is keyboard reachable");
                Equal(true, open.Focus(NavigationMethod.Tab), "the action takes keyboard focus");
                open.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Settle(window);
                Equal(true, ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.SectionDocument), "the action opens the Section document");
                Equal(true, panel.SectionView.IsEffectivelyVisible, "the document is on screen");
                tabs.SelectedItem = panel.FindControl<TabItem>("LoadingTab");
                Settle(window);
                Equal(AnalysisPanel.HeightFor(window.Bounds.Height), panel.Height, "other tabs keep the panel's full height");
            });
            DesktopChecks.Check("SectionMainArea_Document_MinimumWindow_ChartsDrawn", () =>
            {
                var (_, host, window) = shared.Value;
                window.Width = 1024;
                window.Height = 700;
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var view = host.AnalysisPanel.SectionView;
                Equal(true, view.Chart.Bounds.Width >= 250 && view.ProfileView.Bounds.Width >= 250,
                    $"plots stay wide enough at the minimum window: chart {view.Chart.Bounds.Width}, profile {view.ProfileView.Bounds.Width}");
                window.Width = 1500;
                window.Height = 870;
                Settle(window);
            });
            // Ruling 125 (mockup B, C): the one conditions band is above the Section document; Evaluate runs from there.
            DesktopChecks.Check("SectionMainArea_ConditionsBand_OneInstance_EvaluateFromSectionTab", () =>
            {
                var (controller, host, window) = shared.Value;
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var bands = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window).OfType<ConditionsBand>().ToArray();
                Equal(1, bands.Length, "one band instance in the window");
                var band = bands[0];
                Equal(true, band.IsEffectivelyVisible, "the band is visible with the Section document open");
                Equal(true, Avalonia.VisualTree.VisualExtensions.GetVisualAncestors(band).OfType<Control>().Any(a => a.Name == "SectionDocumentBody"),
                    "the band sits in the Section document");
                string? before = controller.AnalysisView.RunKey;
                band.FindControl<TextBox>("AlphaInput")!.Text = "4.00";
                Settle(window);
                band.FindControl<Button>("EvaluateButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while ((controller.AnalysisView.RunKey == before || controller.AnalysisState == RunState.Running) && sw.Elapsed.TotalSeconds < 60)
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(10);
                }
                host.RefreshPanes();
                Settle(window);
                Equal(true, controller.AnalysisView.RunKey != before, "Evaluate from the Section tab recorded a new run");
                Equal(controller.AnalysisView.RunKey, host.AnalysisPanel.BoundRunKey, "the document is bound to the new run");
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                Settle(window);
                Equal(true, band.IsEffectivelyVisible && !ReferenceEquals(band.Parent, host.ModelView.FindControl<ContentControl>("SectionBandHost")), "the band returns above the Plan");
            });
        }
        finally
        {
            if (shared.IsValueCreated) { shared.Value.Window.Close(); shared.Value.Controller.Dispose(); }
        }
    }

    private static string Header(SectionTabView view) =>
        Descendants(view).OfType<TextBlock>().First(t => t.Name == "SectionHeader").Text ?? "";

    private static IEnumerable<Control> Descendants(Control root)
    {
        yield return root;
        foreach (Avalonia.LogicalTree.ILogical child in ((Avalonia.LogicalTree.ILogical)root).LogicalChildren)
            if (child is Control control) foreach (Control inner in Descendants(control)) yield return inner;
    }

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }
}
