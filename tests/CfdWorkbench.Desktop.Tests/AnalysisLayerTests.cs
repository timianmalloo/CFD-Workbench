using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>Desktop ring: selected-run canvas overlays and their non-colour carriers (§12.2–12.4).</summary>
public static class AnalysisLayerTests
{
    public static void Run()
    {
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
