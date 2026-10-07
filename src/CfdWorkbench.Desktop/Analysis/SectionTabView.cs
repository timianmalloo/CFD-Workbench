using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using CfdWorkbench.Analysis;
using Avalonia.Media;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Section view (DXM-4), hosted full size by the model area's Section document (Ruling 124): the shown station named, the
/// Section view with Cp on the profile, one chart selector over Cp, Polar, Transition and Bucket with a table twin, then the
/// station's tables. The shown station is the selected strip, else the governing cavitation station (DXM-9). The content is
/// <see cref="SectionDisplay"/>'s; this view only places it. The bottom panel's Section tab shows <see cref="Summary"/> only.
/// </summary>
public sealed class SectionTabView : UserControl
{
    private static readonly string[] Selector = ["cp", "polar", "transition", "bucket"];
    private const double ProfileHeight = 200, ChartHeight = 300;
    private readonly StackPanel root = new() { Name = "SectionTab" };
    private readonly SectionChartView profile = new() { Height = ProfileHeight, Name = "SectionProfileView" };
    private readonly SectionChartView chart = new() { Height = ChartHeight, MinWidth = 260, Name = "SectionChart" };
    private readonly StackPanel twin = new() { Name = "section-twin", IsVisible = false };
    private readonly Dictionary<string, ToggleButton> buttons = new();
    private TextBlock? legend;
    private string chosen = "cp";
    private string? key;

    public SectionTabView()
    {
        TwinToggle = new ToggleButton { Name = "SectionTwinToggle", Content = "Show table", Classes = { "prop-seg" } };
        AutomationProperties.SetName(TwinToggle, "Section chart table twin");
        TwinToggle.IsCheckedChanged += (_, _) => ShowTwin();
        Content = root;
        Rebuild();
    }

    /// <summary>What the bottom panel's Section tab shows: the shown station, cl (panel) and -Cp_min as the document displays them.</summary>
    public sealed record SummaryLine(string Station, string Cl, string CpMin);

    /// <summary>The one-line summary of <see cref="Shown"/>, or null when there is no section tier.</summary>
    public SummaryLine? Summary =>
        Shown is { } shown && shown.Groups.FirstOrDefault(g => g.Title == "Estimator") is { } estimator
            ? new SummaryLine(shown.StationName, Value(estimator, Labels.ClPanel), Value(estimator, Labels.CpMinLabel))
            : null;

    private static string Value(ResultGroup group, string label) =>
        group.Rows.FirstOrDefault(r => r.Label == label) is { } row ? (row.Unit is null ? row.Value : row.Value + " " + row.Unit) : "";

    public ToggleButton TwinToggle { get; }

    /// <summary>The content last built (null when there is no section tier), for the checks.</summary>
    public SectionView? Shown { get; private set; }

    public string ChosenChart => chosen;

    /// <summary>Moves keyboard focus to the chart selector's chosen button (the document's first stop); false before there is content.</summary>
    public bool FocusSelector() => buttons.GetValueOrDefault(chosen)?.Focus() ?? false;

    public SectionChartView Chart => chart;

    public SectionChartView ProfileView => profile;

    public IReadOnlyList<ResultGroup> Tables { get; private set; } = [];

    public void Choose(string id)
    {
        if (!Selector.Contains(id)) return;
        chosen = id;
        foreach ((string name, ToggleButton button) in buttons) button.IsChecked = name == id;
        Refresh();
    }

    public void Bind(AnalysisViewModel view, WorkbenchController controller)
    {
        double? eta = controller.Selection is Selection.Station station ? station.Eta : null;
        string next = view.RunKey + "|" + view.State + "|" + eta + "|" + view.SectionTier?.GoverningEta + "|" + controller.AnalysisUnits;
        if (next == key) return;
        key = next;
        Shown = null;
        if (view.Run is not { } run || view.SectionTier is not { } tier)
        {
            Tables = view.Groups.Where(g => g.Title == "Section (2D)").ToArray();
            Rebuild();
            return;
        }
        byte[]? source = view.State == RunState.Current ? Encoding.UTF8.GetBytes(controller.AcceptedSource) : null;
        string revision = Regex.Match(view.Groups.FirstOrDefault(g => g.Title == "Provenance")?.Rows.FirstOrDefault(r => r.Label == "Inputs")?.Value ?? "", @"revision (r\d+)") is { Success: true } m
            ? m.Groups[1].Value : "r?";
        IReadOnlyList<ResultRow>? strip = eta is { } e ? AnalysisProjection.StripAt(view, e).Rows : null;
        Shown = SectionDisplay.Build(run, tier, source, eta, revision, (bytes, e, a, re) => SectionTier.UnderreadAt(bytes, e, a, re), strip);
        Tables = Shown.Groups;
        Rebuild();
    }

    private void Rebuild()
    {
        root.Children.Clear();
        buttons.Clear();
        foreach (Control kept in new Control[] { TwinToggle, profile, chart, twin }) (kept.Parent as Panel)?.Children.Remove(kept);
        if (Shown is null)
        {
            foreach (ResultGroup group in Tables) root.Children.Add(AnalysisPanel.Table("section-table", group));
            if (Tables.Count == 0) root.Children.Add(new TextBlock { Text = Labels.NoResult, Classes = { "pnl-note" } });
            return;
        }
        // The document: charts left (header, selector, profile, chart), the station's tables right (mockup states B and C).
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,2*"), Margin = new Thickness(12) };
        var left = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 16, 0) };
        var right = new StackPanel { Spacing = 8 };
        Grid.SetColumn(right, 1);
        layout.Children.Add(left);
        layout.Children.Add(right);
        root.Children.Add(layout);
        left.Children.Add(new TextBlock { Name = "SectionHeader", Text = Shown.StationName, Classes = { "heading" } });
        // The chart selector and the table toggle use the group value row's Set to | Move by switch style (Styles.axaml prop-seg).
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var segments = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (string id in Selector)
        {
            var button = new ToggleButton { Name = "SectionChart-" + id, Content = Shown.Charts.First(c => c.Id == id).Title,
                Classes = { "prop-seg" }, IsChecked = id == chosen };
            AutomationProperties.SetName(button, "Chart: " + button.Content);
            string captured = id;
            button.Click += (_, _) => Choose(captured);
            buttons[id] = button;
            segments.Children.Add(button);
        }
        bar.Children.Add(new Border { Classes = { "prop-seg-box" }, Child = segments });
        bar.Children.Add(new Border { Classes = { "prop-seg-box" }, Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { TwinToggle } } });
        left.Children.Add(bar);
        left.Children.Add(profile);
        left.Children.Add(chart);
        left.Children.Add(twin);
        legend = new TextBlock { Name = "SectionChartLegend", Classes = { "pnl-note" } };
        left.Children.Add(legend);
        Refresh();
        // The mockup's compact Stations table replaces the "Station" name row (the header carries it) and the text "Stations" group.
        if (Shown.StationTable is { } stations) right.Children.Add(StationsTable(stations));
        foreach (ResultGroup group in Shown.Groups.Where(g => Shown.StationTable is null || g.Title is not ("Station" or "Stations"))) right.Children.Add(AnalysisPanel.Table("section-" + group.Title.ToLowerInvariant().Replace(' ', '-').Replace("-", "") + "-table", group));
    }

    // Station (η) · α_eff ° · −Cp_min · Cavitation; the shown station is highlighted and named "shown" for assistive technology.
    private static StackPanel StationsTable(IReadOnlyList<StationTableRow> rows)
    {
        var table = new StackPanel { Name = "section-stations-table", Margin = new Thickness(0, 0, 0, 6) };
        AutomationProperties.SetName(table, "Stations");
        table.Children.Add(new TextBlock { Text = "Stations", Classes = { "pnl-head" } });
        Grid Row(params string[] cells)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,2*") };
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = new TextBlock { Text = cells[i], Classes = { "pnl-cell" }, Margin = new Thickness(4, 1) };
                Grid.SetColumn(cell, i);
                grid.Children.Add(cell);
            }
            return grid;
        }
        var head = Row("Station", "α_eff °", Labels.CpMinLabel, "Cavitation");
        foreach (TextBlock cell in head.Children.OfType<TextBlock>()) cell.FontWeight = Avalonia.Media.FontWeight.SemiBold;
        table.Children.Add(head);
        foreach (StationTableRow row in rows)
        {
            var holder = new StackPanel { Name = "section-station-row" };
            holder.Children.Add(Row(row.Station, row.AlphaEff, row.CpMin, row.Cavitation));
            if (row.NotMeasured is { } note) holder.Children.Add(new TextBlock { Text = note, Classes = { "pnl-note" }, Margin = new Thickness(4, 0) });
            var item = new Border { Child = holder };
            if (row.IsShown)
            {
                item.Bind(Border.BackgroundProperty, item.GetResourceObservable("SurfaceSoftBrush"));
                AutomationProperties.SetName(item, row.Station + ", shown");
            }
            table.Children.Add(item);
        }
        return table;
    }

    private void Refresh()
    {
        if (Shown is null) return;
        profile.Model = Shown.Charts.First(c => c.Id == "profile");
        ChartModel model = Shown.Charts.First(c => c.Id == chosen);
        chart.Model = model;
        if (legend is not null)
            legend.Text =string.Join(" · ", new[] { model.Unavailable, model.Legend, model.Label }.Where(s => !string.IsNullOrEmpty(s)));
        twin.Children.Clear();
        foreach (ChartPlot plot in model.Plots)
            foreach (ChartSeries series in plot.Series)
                twin.Children.Add(new TextBlock { Classes = { "pnl-cell" }, FontSize = 10,
                    Text = plot.Title + " · " + series.Name + ": " + string.Join("  ", series.Points.Select(p =>
                        p.X.ToString("0.##", CultureInfo.InvariantCulture) + "→" + p.Y.ToString("0.###", CultureInfo.InvariantCulture))) });
        ShowTwin();
    }

    private void ShowTwin()
    {
        bool table = TwinToggle.IsChecked == true;
        twin.IsVisible = table;
        chart.IsVisible = !table;
        TwinToggle.Content = table ? "Show chart" : "Show table";
    }
}
