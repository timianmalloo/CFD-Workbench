using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Layers pane: a check per layer of the selected run, drawn from <see cref="AnalysisViewModel.Layers"/>. A toggle writes the
/// controller's per-layer flag and re-reads the view (the controller raises no notification for it), then tells the shell
/// through <see cref="LayerVisibilityChanged"/> so the views redraw. Rows are reused by layer id, so a toggle never moves focus.
/// </summary>
public partial class LayersPane : UserControl
{
    private readonly Dictionary<string, (StackPanel Root, CheckBox Check, TextBlock Legend)> rows = new(StringComparer.Ordinal);
    private WorkbenchController? bound;
    private bool binding;

    public LayersPane()
    {
        InitializeComponent();
    }

    /// <summary>Raised after a check changed the controller's flag and the view was re-read.</summary>
    public event Action? LayerVisibilityChanged;

    public IReadOnlyCollection<string> LayerIds => rows.Keys;

    public CheckBox? CheckFor(string layerId) => rows.TryGetValue(layerId, out var row) ? row.Check : null;

    public void Bind(WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        bound = controller;
        var layers = controller.Inspection is null ? [] : controller.AnalysisView.Layers;
        binding = true;
        try
        {
            LayersEmpty.IsVisible = layers.Count == 0;
            LayersEmpty.Text = layers.Count == 0 ? Labels.NoResult : "";
            foreach (string stale in rows.Keys.Where(id => layers.All(layer => layer.Id != id)).ToArray())
            {
                LayerList.Children.Remove(rows[stale].Root);
                rows.Remove(stale);
            }
            for (int index = 0; index < layers.Count; index++)
            {
                var layer = layers[index];
                if (!rows.TryGetValue(layer.Id, out var row)) rows[layer.Id] = row = Create(layer.Id);
                row.Check.Content = layer.Title;
                row.Check.IsChecked = controller.IsLayerVisible(layer.Id);
                AutomationProperties.SetName(row.Check, layer.Title + " layer");
                row.Legend.Text = layer.Note is { Length: > 0 } note ? layer.Legend + " · " + note : layer.Legend;
                if (LayerList.Children.IndexOf(row.Root) != index)
                {
                    LayerList.Children.Remove(row.Root);
                    LayerList.Children.Insert(Math.Min(index, LayerList.Children.Count), row.Root);
                }
            }
        }
        finally { binding = false; }
    }

    private (StackPanel, CheckBox, TextBlock) Create(string id)
    {
        var check = new CheckBox { Classes = { "lay-check" }, Tag = id };
        var legend = new TextBlock { Classes = { "lay-legend" }, Margin = new Avalonia.Thickness(24, 0, 0, 0) };
        var root = new StackPanel { Children = { check, legend } };
        check.IsCheckedChanged += (_, _) =>
        {
            if (binding || bound is null) return;
            bound.SetLayerVisible(id, check.IsChecked == true);
            _ = bound.AnalysisView;
            LayerVisibilityChanged?.Invoke();
        };
        return (root, check, legend);
    }
}
