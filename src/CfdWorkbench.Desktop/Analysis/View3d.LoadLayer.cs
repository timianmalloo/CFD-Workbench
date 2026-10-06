using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop;

public sealed partial class View3d
{
    private AnalysisViewModel? layerView;
    private IReadOnlyList<LayerData>? seenLayers;

    private AnalysisViewModel LayerView()
    {
        if (controller is null) throw new InvalidOperationException("No controller for the 3D load layer.");
        if (layerView is null || !ReferenceEquals(seenLayers, controller.LayerSet))
        {
            layerView = controller.AnalysisView;
            seenLayers = controller.LayerSet;
        }
        return layerView;
    }

    private string LayerNameSuffix()
    {
        if (controller?.IsAnalysis != true) return "";
        var visible = controller.LayerSet.Where(layer => layer.Visible).Select(layer => layer.Id).ToHashSet();
        string suffix = "";
        if (visible.Contains("strip-lift")) suffix += "; strip lift arrows, values in the Loads table";
        if (visible.Contains("root-moment")) suffix += "; root moment, values in the Loads table";
        if (visible.Contains("depth-band")) suffix += "; free surface and tip depth, values in the conditions table";
        return suffix;
    }
}
