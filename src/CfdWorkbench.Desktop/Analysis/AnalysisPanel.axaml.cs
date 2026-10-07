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
        OpenInMainAreaButton.Content = Labels.OpenInMainArea;
        AutomationProperties.SetName(OpenInMainAreaButton, Labels.OpenInMainArea);
        OpenInMainAreaButton.Click += (_, _) => OpenSectionRequested?.Invoke();
        PanelTabs.SelectionChanged += (_, _) => UpdateHeight();
        // The slot height is the Bottom region's preset size (LayoutCodec Chrome: 190), 150 in a short window (HeightFor);
        // the skeleton bars are static placeholders.
        Height = FullHeight;
        SkeletonLong.Width = 320;
        SkeletonShort.Width = 240;
    }

    /// <summary>The panel's height, and its height in a window shorter than <see cref="ShortWindowClientHeight"/> (Ruling 101 3d, layout C).</summary>
    public const double FullHeight = 190, ShortHeight = 150;

    /// <summary>
    /// A window client under 820 px tall gets the short panel: 1280x800 (client 800) fires it and 1500x870 (client 860) does not,
    /// so four views fit at 1280x800 with the 320x240 floor unchanged (measured margin: docs/proof/lay-1280/options.md, C + D).
    /// </summary>
    public const double ShortWindowClientHeight = 820;

    public static double HeightFor(double windowClientHeight) =>
        windowClientHeight > 0 && windowClientHeight < ShortWindowClientHeight ? ShortHeight : FullHeight;

    /// <summary>The panel's height while the Section tab (one summary line) is the shown tab and nothing sits above the tabs (Ruling 125, state D).</summary>
    public const double CompactHeight = 68;

    /// <summary>The Section view, hosted by the model area's Section document (Ruling 124). The bottom tab shows only its summary.</summary>
    public SectionTabView SectionView { get; } = new();

    /// <summary>Raised by the summary's "Open in main area" action; the shell opens and focuses the Section document.</summary>
    public event Action? OpenSectionRequested;

    private double windowHeight;

    private void UpdateHeight()
    {
        bool compact = PanelTabs.SelectedItem == SectionTab && !PanelBanner.IsVisible && !PanelErrorCard.IsVisible && !PanelSkeleton.IsVisible;
        Height = compact ? CompactHeight : HeightFor(windowHeight);
    }

    private Visual? sizedBy;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        sizedBy = e.Root as Visual;
        if (sizedBy is null) return;
        sizedBy.PropertyChanged += OnRootBounds;
        windowHeight = sizedBy.Bounds.Height;
        UpdateHeight();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (sizedBy is not null) sizedBy.PropertyChanged -= OnRootBounds;
        sizedBy = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnRootBounds(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property != BoundsProperty || sender is not Visual root) return;
        windowHeight = root.Bounds.Height;
        UpdateHeight();
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
        loading.Update(unavailable ? [] : view.Loading, controller.AnalysisUnits);
        SectionView.Bind(view, controller);
        ShowSummary();
        UpdateHeight();
        Fill(LoadsBody, view, ("Loads", "loads-table"), ("Strips", "strips-table"));
        Fill(ProvenanceBody, view, ("Conditions", "conditions-table"), ("Labels", "labels-table"), ("Provenance", "provenance-table"));
    }

    // One line: the shown station, cl (panel) and -Cp_min of the Section document's content, then the action that opens it.
    private void ShowSummary()
    {
        var summary = SectionView.Summary;
        SectionSummaryStation.Text = summary?.Station ?? Labels.NoResult;
        SectionSummaryCl.Text = summary is null ? "" : Labels.ClPanel + " " + summary.Cl;
        SectionSummaryCp.Text = summary is null ? "" : Labels.CpMinLabel + " " + summary.CpMin;
        OpenInMainAreaButton.IsEnabled = summary is not null;
    }

    // The tampered view (COPY-211) names its note (COPY-274) beside the verdict.
    private static string EmptyText(AnalysisViewModel view) =>
        view.Groups.FirstOrDefault()?.Rows.FirstOrDefault() is { } row
            ? view.State == RunState.Unavailable && row.Note is not null ? row.Value + ". " + row.Note : row.Value
            : Labels.NoResult;

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

    internal static StackPanel Table(string name, ResultGroup group)
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
