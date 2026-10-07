using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track DX, the Section tab and Find alpha in real windows. Ring: readiness (a window each; est. 1.7 s for the four checks,
/// one controller and one evaluation shared). The pure content is checked in the Analysis harness (DxSectionTests).
/// </summary>
public static class DxSectionPanelTests
{
    public static void RunReadiness()
    {
        var shared = new Lazy<(WorkbenchController Controller, ShellHost Host, Window Window, SectionTabView Tab)>(() =>
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
            // Ruling 124: the Section view is the model area's Section document.
            host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
            Settle(window);
            return (controller, host, window, host.AnalysisPanel.SectionView);
        });
        try
        {
            DesktopChecks.Check("Section_TabBody_BuildsChartSelector", () =>
            {
                var (_, _, _, tab) = shared.Value;
                Equal(true, tab.Shown is not null, "the Section tab has content for the evaluated run");
                string[] ids = ["cp", "polar", "transition", "bucket"];
                foreach (string id in ids)
                    Equal(true, FindToggle(tab, "SectionChart-" + id) is not null, "selector button " + id);
                Equal(0, Descendants(tab).OfType<StackPanel>().Count(p => p.Name == "section-table"), "the old text table of the group is gone");
                Equal(true, tab.TwinToggle.Parent is not null, "the table twin toggle is placed");
            });
            DesktopChecks.Check("SectionView_OneView_SelectorDrivesChart", () =>
            {
                var (_, _, window, tab) = shared.Value;
                Equal(true, tab.ProfileView.Model is not null, "one Section view");
                foreach (string id in new[] { "polar", "transition", "bucket", "cp" })
                {
                    tab.Choose(id);
                    Settle(window);
                    Equal(id, tab.Chart.Model?.Id, "the selector drives the chart");
                    Equal(true, ReferenceEquals(tab.Shown!.Profile, tab.ProfileView.Model), "the Section view stays one view");
                }
            });
            // Ring: readiness; cost: one render of one control in the shared window (under 0.05 s).
            DesktopChecks.Check("SectionProfileView_Renders_CpOnVikPinnedAtZero", () =>
            {
                var (_, _, window, tab) = shared.Value;
                SectionProfileView view = tab.ProfileView;
                SectionProfile profile = view.Model ?? throw new Exception("the profile view has no model");
                Render(view);
                Equal(profile.Outline.Count, view.Segments.Count, "one coloured segment per outline panel, a closed outline");
                var low = view.Segments.MinBy(s => s.Cp);
                var high = view.Segments.MaxBy(s => s.Cp);
                Equal(true, low.Cp < 0 && high.Cp > 0, "segments of both Cp signs");
                Equal(true, SectionProfileView.RampPosition(low.Cp, view.Range) < 0.5 && SectionProfileView.RampPosition(high.Cp, view.Range) > 0.5,
                    "opposite Cp signs fall on opposite sides of the ramp's centre");
                Equal(true, low.Colour.B > low.Colour.R && high.Colour.R > high.Colour.B, "suction reads blue, pressure reads red (vik)");
                Equal(Avalonia.Media.Color.Parse("#ebe6e2"), view.Vik(SectionProfileView.RampPosition(0, view.Range)), "Cp 0 is the ramp's centre colour");
                Equal(Math.Max(-profile.CpLow, profile.CpHigh), view.Range, "the ramp's extent is the data's");
                Equal(true, profile.Outline.Contains(profile.CpMinPanel), "the Cp_min marker is on the outline");
                Settle(window);
            });
            DesktopChecks.Check("SectionTab_Desktop_RendersAllChartsWithoutThrowing", () =>
            {
                var (_, _, window, tab) = shared.Value;
                foreach (string id in new[] { "cp", "polar", "transition", "bucket" })
                {
                    tab.Choose(id);
                    Settle(window);
                    Render(tab.Chart);
                    Equal(tab.Chart.Model!.Plots.Count, tab.Chart.Drawn.Plots, "plots drawn for " + id);
                    Equal(true, tab.Chart.Drawn.Points > 3, "points drawn for " + id);
                }
                tab.TwinToggle.Focus();
                tab.TwinToggle.IsChecked = true;
                Settle(window);
                Equal(false, tab.Chart.IsVisible, "the table twin replaces the plot");
                var held = tab.TwinToggle;
                tab.Choose("polar");
                Equal(true, ReferenceEquals(held, tab.TwinToggle) && tab.TwinToggle.IsChecked == true, "the twin toggle survives a re-render");
                tab.TwinToggle.IsChecked = false;
            });
            DesktopChecks.Check("FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6", () =>
            {
                var (controller, host, _, _) = shared.Value;
                var band = host.ModelView.FindControl<ConditionsBand>("AnalysisConditionsBand")!;
                var find = band.FindControl<Button>("FindAlphaButton")!;
                var evaluate = band.FindControl<Button>("EvaluateButton")!;
                var row = (Panel)evaluate.Parent!;
                Equal(true, ReferenceEquals(row, find.Parent) && row.Children.IndexOf(find) == row.Children.IndexOf(evaluate) + 1, "Find α sits beside Evaluate");
                Equal(24d, find.MinHeight, "24 px target, as the More button");
                string? before = controller.AnalysisView.RunKey;
                double alpha0 = controller.AnalysisOperatingPoint.AlphaDeg;
                FindAlphaOutcome found = Task.Run(() => controller.FindAlphaAsync(0.2, -2, 8)).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
                Equal(true, found.Alpha is not null, "a root: " + found.Sentence);
                Equal(alpha0, controller.AnalysisOperatingPoint.AlphaDeg, "Find alone leaves α alone");
                Equal(before, controller.AnalysisView.RunKey, "Find records no run");
                bool cancelled = false;
                var dialog = new FindAlphaDialog(controller, 5.14, _ => cancelled = true);
                dialog.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Equal(false, cancelled, "Cancel applies nothing");
                Equal(alpha0, controller.AnalysisOperatingPoint.AlphaDeg, "Cancel leaves α alone");
                controller.ApplyFoundAlpha(found.Alpha!.Value);
                Equal(found.Alpha.Value, controller.AnalysisOperatingPoint.AlphaDeg, "Apply writes α");
                Equal(before, controller.AnalysisView.RunKey, "Apply records no run");
                Equal(RunState.Historical, controller.AnalysisView.State, "the run is Historical until Evaluate");
                controller.ApplyFoundAlpha(alpha0);
            });
            DesktopChecks.Check("Section_CpUnavailable_PanelSolveFailed_ShowsCopy358InApp", () =>
            {
                // Row 10 in the app. The controller's feed (DeriveFeed, the one the controller projects) must carry the failure code.
                // A completed run whose speed puts the section Re under 100 fails the station solve inside the feed (ANA-SECTION-RE).
                var (controller, _, _, _) = shared.Value;
                var stored = controller.AnalysisView.Run!;
                var doctored = stored with { Op = stored.Op with { Speed = 0.0005 } };
                byte[] source = CfdWorkbench.Cli.Cli.ExampleBytes();
                var feed = WorkbenchController.DeriveFeed(doctored, new SessionView(stored.Inputs.AcceptedId, "", "", source, null, null, false), _ => null);
                Equal("ANA-SECTION-RE", feed.SectionFailureCode, "the feed names the section failure");
                var view = AnalysisProjection.Build(stored, new CurrentInputs(stored.Inputs, stored.Water, stored.Op, stored.Method, stored.SettingsHash),
                    Units.Metric, new ProjectionContext(feed.Verdicts, feed.Stations, feed.RootThicknessRatio, StripNormals: feed.StripNormals,
                        FeedUnavailable: feed.Unavailable, SectionTier: feed.SectionTier, SectionFailureCode: feed.SectionFailureCode));
                Equal("Unavailable — the panel solve failed at this station. Evaluate again.",
                    view.Groups.Single(g => g.Title == "Section (2D)").Rows.Single(r => r.Label == "Cp_min").Value, "COPY-358, not COPY-357");
            });
        }
        finally
        {
            if (shared.IsValueCreated) { shared.Value.Window.Close(); shared.Value.Controller.Dispose(); }
        }
    }

    private static ToggleButton? FindToggle(Control root, string name) => Descendants(root).OfType<ToggleButton>().FirstOrDefault(t => t.Name == name);

    private static IEnumerable<Control> Descendants(Control root)
    {
        yield return root;
        foreach (Avalonia.LogicalTree.ILogical child in ((Avalonia.LogicalTree.ILogical)root).LogicalChildren)
            if (child is Control control) foreach (Control inner in Descendants(control)) yield return inner;
    }

    private static void Render(Control control)
    {
        PixelSize size = new((int)Math.Max(20, control.Bounds.Width), (int)Math.Max(20, control.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
        bitmap.Render(control);
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
