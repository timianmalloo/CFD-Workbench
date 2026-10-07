using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>Desktop ring: selected-run canvas overlays and their non-colour carriers (§12.2–12.4).</summary>
public static class AnalysisLayerTests
{
    // Readiness only: checks moved out of the fast ring (round-oct06 SPL, Ruling 123); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        DesktopChecks.Check("AnalysisLayers_WindowRendersAndPeersFollowVisibility", () =>
        {
            using var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(
                Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            _ = Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, 0.3), controller.AnalysisWater))
                .WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            controller.ToggleAnalysis();
            controller.Layout = ViewLayout.Four;
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1440, Height = 900 };
            try
            {
                window.Show();
                host.RefreshPanes();
                var deadline = System.Diagnostics.Stopwatch.StartNew();
                while ((controller.Surface is null || controller.SurfaceUpdating || controller.Camera3d is null) && deadline.Elapsed < TimeSpan.FromSeconds(20))
                {
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Thread.Yield();
                }
                if (controller.Surface is null || controller.Camera3d is null) throw new Exception("3D mesh and camera did not become ready.");
                window.UpdateLayout();
                using var bitmap = new RenderTargetBitmap(new PixelSize(1440, 900));
                bitmap.Render(window);
                var plan = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                var threeD = host.ModelView.FindControl<View3d>("ThreeDView")!;
                var sceneField = typeof(View3d).GetField("layerScene",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?? throw new Exception("3D scene cache was not found.");
                if (plan.RenderBanner is not null) throw new Exception("Plan layer render failed: " + plan.RenderBanner);
                if (!AutomationProperties.GetName(plan)!.Contains("Γ loading strips", StringComparison.Ordinal) ||
                    !AutomationProperties.GetName(threeD)!.Contains("strip lift arrows", StringComparison.Ordinal))
                    throw new Exception("Rendered layers have no named table twins in their peers: " +
                        AutomationProperties.GetName(plan) + " / " + AutomationProperties.GetName(threeD) +
                        " / " + string.Join(",", controller.AnalysisView.Layers.Select(l => l.Id + ":" + l.Visible)));
                controller.SetLayerVisible("plan-gamma", false);
                if (AutomationProperties.GetName(plan) != "Plan view" || PlanLoadLayer.Build(controller.AnalysisView.Layers.Single(l => l.Id == "plan-gamma")).Strips.Count != 0)
                    throw new Exception("Hidden plan layer remains on the canvas or in its peer.");
                bitmap.Render(window);
                controller.SetLayerVisible("plan-gamma", true);
                bitmap.Render(window);
                var firstScene = sceneField.GetValue(threeD) ?? throw new Exception("3D scene was not built on render.");
                double? loadBefore = DesktopChecks.LoadAverage1();
                long paneRefreshes = host.PaneRefreshes;
                var camera = controller.Camera3d ?? throw new Exception("No 3D camera");
                for (int i = 0; i < 8; i++)
                {
                    Dispatcher.UIThread.RunJobs();
                    controller.Camera3d = camera.Pan(i % 2 == 0 ? 2 : -2, 0, threeD.Bounds.Size);
                    window.UpdateLayout();
                }
                var steps = new List<double>();
                var events = new List<double>();
                for (int i = 0; i < 32; i++)
                {
                    Dispatcher.UIThread.RunJobs();
                    long started = System.Diagnostics.Stopwatch.GetTimestamp();
                    controller.Camera3d = camera.Pan(i % 2 == 0 ? 2 : -2, 0, threeD.Bounds.Size);
                    events.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                    window.UpdateLayout();
                    steps.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                }
                bitmap.Render(window);
                if (!ReferenceEquals(firstScene, sceneField.GetValue(threeD)))
                    throw new Exception("Camera redraw rebuilt the 3D layer scene without a LayerSet change.");
                double p95 = steps.Order().ElementAt((int)Math.Ceiling(steps.Count * .95) - 1);
                double eventP95 = events.Order().ElementAt((int)Math.Ceiling(events.Count * .95) - 1);
                Console.WriteLine(FormattableString.Invariant($"MEASURE AnalysisLayers_CameraStep_WithLayers full_step_p95_ms={p95:F3} event_p95_ms={eventP95:F3} pane_refreshes={host.PaneRefreshes - paneRefreshes} samples={steps.Count}"));
                if (host.PaneRefreshes != paneRefreshes)
                    throw new Exception($"Layers-on camera step refreshed {host.PaneRefreshes - paneRefreshes} panes.");
                DesktopChecks.RequireFrameBudget("AnalysisLayers_CameraEventNoPaneRefresh_Under3Ms", eventP95, 3, loadBefore);
            }
            finally { window.Close(); }
        });
    }

    public static void Run()
    {
        DesktopChecks.Check("ModelArea_LayersChanged_UnsubscribesOnWindowClose", () =>
        {
            using var controller = new WorkbenchController();
            var area = new ModelArea();
            area.PlanCanvas.Controller = controller;
            area.ShowFoilOpen(true);
            var window = new Window { Content = area, Width = 900, Height = 700 };
            var field = typeof(WorkbenchController).GetField("LayersChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("LayersChanged backing event was not found.");
            int Count() => ((Delegate?)field.GetValue(controller))?.GetInvocationList()
                .Count(handler => ReferenceEquals(handler.Target, area) && handler.Method.Name == "RefreshLayerNames") ?? 0;
            try
            {
                window.Show();
                if (Count() != 1) throw new Exception("ModelArea did not subscribe exactly once while attached.");
            }
            finally { window.Close(); }
            if (Count() != 0) throw new Exception("ModelArea retained LayersChanged after window close.");
        });
        DesktopChecks.Check("PlanLayer_BatlowBrushes_MatchDesignTokens", () =>
        {
            var styles = (Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(
                new Uri("avares://CfdWorkbench.Desktop/Styles.axaml"), null);
            string[] tokens = ["#011959", "#215f61", "#818232", "#f19d6b", "#faccfa"];
            for (int i = 0; i < tokens.Length; i++)
                if (!styles.TryGetResource($"Batlow{i}Brush", Avalonia.Styling.ThemeVariant.Light, out var value) ||
                    value is not ISolidColorBrush brush || brush.Color != Color.Parse(tokens[i]))
                    throw new Exception($"Batlow{i}Brush does not match DESIGN.md batlow-{i}.");
        });
        DesktopChecks.Check("PlanLayer_OutsideStrips_DashedOutlineAndCount", () =>
        {
            var layer = new LayerData("plan-gamma", "Γ per strip", true, "Γ · batlow 1.0 · run 123", "strips-table")
            {
                Samples =
                [
                    new LayerSample(-.5, -.5, .2, null, false, false),
                    new LayerSample(.5, .5, .8, null, true, false)
                ]
            };
            var scene = PlanLoadLayer.Build(layer);
            if (scene.Strips.Count != 2 || scene.OutsideCount != 1 || scene.Strips.Count(strip => strip.Outside && strip.DashedOutline) != 1)
                throw new Exception("Outside strip must have a dashed outline and a text count; colour alone is insufficient.");
            if (scene.CountText is null || !scene.CountText.Contains("1 strip", StringComparison.Ordinal))
                throw new Exception("Outside count was not rendered as text.");
            if (PlanLoadLayer.Build(layer with { Visible = false }).Strips.Count != 0)
                throw new Exception("Hidden layer still draws strips.");
        });
        DesktopChecks.Check("View3dLayer_NormalAndMissingVector", () =>
        {
            var layer = new LayerData("strip-lift", "Lift per strip", true, "N/m", "loads-table")
            {
                Samples =
                [
                    new LayerSample(0, 0, 10, new Loads.Vec(0, 0, 10), false, false) { Normal = new Loads.Vec(0, .6, .8) },
                    new LayerSample(.5, .5, null, null, false, false)
                ]
            };
            var arrows = View3dLoadLayer.Build(layer);
            if (arrows.Count != 1 || Math.Abs(arrows[0].Direction.Y - .6) > 1e-12 || Math.Abs(arrows[0].Direction.Z - .8) > 1e-12)
                throw new Exception("Arrow must use the run's local normal; absent force must stay absent.");
        });
        DesktopChecks.Check("View3dLayer_AnchorInterpolatesBetweenSections", () =>
        {
            var root = new PlacedSection(0, 0, [new Point3(0, 0, 0), new Point3(4, 0, 0)], []);
            var tip = new PlacedSection(1, 1, [new Point3(2, 2, 1), new Point3(4, 2, 1)], []);
            var surface = new SurfaceView("test", "test", 0, 0, 0, 0, 4, 2, 1, [root, tip]);
            var between = View3dLoadLayer.Anchor(surface, -1);
            if (between is not { X: 1.75, Y: -1, Z: 0.5 })
                throw new Exception("Port anchor must interpolate quarter chord and elevation at the strip Y: " + between);
            if (View3dLoadLayer.Anchor(surface, 3) is not { X: 2.5, Y: 3, Z: 1 })
                throw new Exception("Tip anchor must clamp the placed section geometry.");
        });
        DesktopChecks.Check("ElevationLayer_DepthUnsetNoBand", () =>
        {
            var layer = new LayerData("depth-band", "Depth", true, "h_ref", "conditions-table")
            {
                Samples = [new LayerSample(1, .5, .3, null, false, false)]
            };
            if (ElevationDepthLayer.Build(null).Count != 0 || ElevationDepthLayer.Build(layer with { Visible = false }).Count != 0)
                throw new Exception("Depth band must disappear when depth is unset or hidden.");
            if (ElevationDepthLayer.Build(layer).Count != 1)
                throw new Exception("Depth band omitted an available margin.");
        });
    }
}
