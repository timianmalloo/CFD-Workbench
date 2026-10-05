using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Analysis bottom panel: Spanwise loading (chart and table twin), Section, Loads, Provenance. Every cell is the
/// projection's own <see cref="ResultRow"/> text, verbatim. The tabs read <see cref="WorkbenchController.AnalysisView"/>,
/// the selected run's projection (design §3.3), so a failed latest attempt keeps the previous run's tables, Historical,
/// beside the error card. A layer's table twin is always drawn: hiding the layer never hides its table.
/// </summary>
public partial class AnalysisPanel : UserControl
{
    private readonly LoadingChart loading = new();

    public AnalysisPanel()
    {
        InitializeComponent();
        LoadingHost.Content = loading;
    }

    public LoadingChart LoadingView => loading;

    /// <summary>The run key of the view last bound (null before a result), for the tests and the shell.</summary>
    public string? BoundRunKey { get; private set; }

    public void Bind(WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        var view = controller.Inspection is null
            ? new AnalysisViewModel(RunState.NoResult, "Analysis: no result", null, null, [], [], null)
            : controller.AnalysisView;
        BoundRunKey = view.RunKey;
        PanelBanner.IsVisible = view.Banner is not null;
        PanelBanner.Text = view.Banner ?? "";
        PanelErrorCard.IsVisible = view.ErrorCard is not null;
        PanelErrorText.Text = view.ErrorCard ?? "";
        PanelSkeleton.IsVisible = view.State == RunState.Running;
        bool hasResult = view.Groups.Count > 0 && view.RunKey is not null;
        bool unavailable = view.State == RunState.Unavailable;
        LoadingEmpty.IsVisible = !hasResult || view.Loading.Count == 0;
        LoadingEmpty.Text = LoadingEmpty.IsVisible ? EmptyText(view) : "";
        loading.IsVisible = !LoadingEmpty.IsVisible;
        loading.Update(unavailable ? [] : view.Loading);
        Fill(SectionBody, view, ("Section (2D)", "section-table"));
        Fill(LoadsBody, view, ("Loads", "loads-table"), ("Strips", "strips-table"));
        Fill(ProvenanceBody, view, ("Conditions", "conditions-table"), ("Labels", "labels-table"), ("Provenance", "provenance-table"));
    }

    private static string EmptyText(AnalysisViewModel view) =>
        view.Groups.FirstOrDefault()?.Rows.FirstOrDefault()?.Value ?? Labels.NoResult;

    private static void Fill(Panel host, AnalysisViewModel view, params (string Group, string Name)[] tables)
    {
        host.Children.Clear();
        bool any = false;
        foreach (var (title, name) in tables)
        {
            if (view.Groups.FirstOrDefault(group => group.Title == title) is not { } group || view.RunKey is null) continue;
            host.Children.Add(Table(name, group));
            any = true;
        }
        if (!any) host.Children.Add(new TextBlock { Text = EmptyText(view), Classes = { "pnl-note" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
    }

    private static StackPanel Table(string name, ResultGroup group)
    {
        var table = new StackPanel { Name = name, Margin = new Thickness(0, 0, 0, 6) };
        AutomationProperties.SetName(table, group.Title);
        table.Children.Add(new TextBlock { Text = group.Title, Classes = { "pnl-head" } });
        foreach (var row in group.Rows)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("160,160,*") };
            grid.Children.Add(Cell(row.Label, 0));
            grid.Children.Add(Cell(row.Unit is null ? row.Value : row.Value + " " + row.Unit, 1));
            grid.Children.Add(Cell(row.Note ?? "", 2));
            table.Children.Add(grid);
        }
        return table;
    }

    private static TextBlock Cell(string text, int column)
    {
        var cell = new TextBlock { Text = text, Classes = { "pnl-cell" }, Margin = new Thickness(4, 1) };
        Grid.SetColumn(cell, column);
        return cell;
    }
}
