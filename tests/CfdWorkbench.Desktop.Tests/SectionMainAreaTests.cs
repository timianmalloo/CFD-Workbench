using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
                var selector = Descendants(view).OfType<ToggleButton>().Where(t => t.Name?.StartsWith("SectionChart-") == true || t.Name == "SectionTwinToggle").ToArray();
                Equal(5, selector.Length, "four chart buttons and the table toggle");
                Equal(true, selector.All(t => t.Classes.Contains("prop-seg") && t.Bounds.Height <= 26),
                    "the selector is the Set to | Move by segmented style at row height: " + string.Join(",", selector.Select(t => t.Bounds.Height)));
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
            // The band's derived readouts neither overlap nor clip, in the Plan host and in the Section host.
            DesktopChecks.Check("SectionMainArea_ConditionsBand_DerivedReadouts_NoOverlapNoClip_BothHosts", () =>
            {
                var (_, host, window) = shared.Value;
                foreach (var (name, document) in new[] { ("Plan", host.LayoutFactory.ModelDocument), ("Section", host.LayoutFactory.SectionDocument) })
                {
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    Settle(window);
                    var band = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window).OfType<ConditionsBand>().Single();
                    var cells = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(band).OfType<TextBlock>()
                        .Where(t => t.Classes.Contains("cond-derived") && t.IsEffectivelyVisible).ToArray();
                    Equal(true, cells.Length >= 2, name + ": derived readouts are shown");
                    var rects = cells.Select(c => (Cell: c, Rect: new Rect(c.TranslatePoint(default, band) ?? default, c.Bounds.Size))).ToArray();
                    foreach (var (cell, rect) in rects)
                    {
                        // The text at the font size it is drawn with must fit the width layout gave it (a stale measure clips it).
                        double drawn = new Avalonia.Media.FormattedText(cell.Text ?? "", System.Globalization.CultureInfo.InvariantCulture,
                            Avalonia.Media.FlowDirection.LeftToRight, new Avalonia.Media.Typeface(cell.FontFamily), cell.FontSize, Avalonia.Media.Brushes.Black).Width;
                        Equal(true, drawn <= cell.Bounds.Width + 1, $"{name}: '{cell.Text}' is clipped (drawn {drawn:F1} px in {cell.Bounds.Width:F1})");
                        Equal(true, rect.Right <= band.Bounds.Width + 0.5 && rect.Left >= 0, $"{name}: '{cell.Text}' is outside the band {band.Bounds.Width}");
                    }
                    for (int i = 0; i < rects.Length; i++)
                        for (int j = i + 1; j < rects.Length; j++)
                            Equal(false, rects[i].Rect.Intersects(rects[j].Rect), $"{name}: '{rects[i].Cell.Text}' overlaps '{rects[j].Cell.Text}'");
                }
            });
            // Ruling 125 (mockup B, C): the compact Stations table, four columns, the shown station highlighted, COPY-384 where not measured.
            DesktopChecks.Check("SectionMainArea_StationsTable_FourColumns_ShownRowHighlighted", () =>
            {
                var (controller, host, window) = shared.Value;
                controller.Select(new Selection.Foil());
                host.RefreshPanes();
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var view = host.AnalysisPanel.SectionView;
                var rows = view.Shown!.StationTable ?? throw new Exception("no station table in the view");
                var table = Descendants(view).OfType<StackPanel>().Single(p => p.Name == "section-stations-table");
                var headers = ((Grid)table.Children[1]).Children.OfType<TextBlock>().Select(t => t.Text).ToArray();
                Equal("Station|α_eff °|" + Labels.CpMinLabel + "|Cavitation", string.Join("|", headers), "the four headers");
                Equal(rows.Count, table.Children.OfType<Border>().Count(), "one rendered row per solved, governing or shown station");
                Equal(1, rows.Count(r => r.IsShown), "exactly one shown row");
                Equal(true, rows.Single(r => r.IsShown).Eta == view.Shown.Eta, "the shown row is the shown station");
                Equal(1, table.Children.OfType<Border>().Count(b => b.Background is not null), "one highlighted row");
                Equal(true, rows.All(r => r.Cavitation is "Clear" or "Inside the margin" or "Possible" or "Unavailable"), "cavitation words come from COPY-301 to COPY-303");
                Equal(true, rows.Where(r => r.NotMeasured is not null).All(r => r.NotMeasured == Labels.UnderreadNotMeasured), "COPY-384 on a station not measured");
                Equal(0, Descendants(view).OfType<StackPanel>().Count(p => p.Name == "section-station-table"), "the Station name group is gone (the header names it)");
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
