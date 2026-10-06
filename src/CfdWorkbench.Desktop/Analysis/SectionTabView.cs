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
/// The Section tab (DXM-4): the shown station named, the Section view with Cp on the profile, one chart selector over Cp, Polar,
/// Transition and Bucket with a table twin, then the station's tables. The shown station is the selected strip, else the governing
/// cavitation station (DXM-9). The content is <see cref="SectionDisplay"/>'s; this view only places it.
/// </summary>
public sealed class SectionTabView : UserControl
{
    private static readonly string[] Selector = ["cp", "polar", "transition", "bucket"];
    private readonly StackPanel root = new() { Name = "SectionTab" };
    private readonly SectionChartView profile = new() { Width = 220, Height = 118, Name = "SectionProfileView" };
    private readonly SectionChartView chart = new() { Height = 118, MinWidth = 260, Name = "SectionChart" };
    private readonly StackPanel twin = new() { Name = "section-twin", IsVisible = false };
    private readonly Dictionary<string, ToggleButton> buttons = new();
    private string chosen = "cp";
    private string? key;

    public SectionTabView()
    {
        TwinToggle = new ToggleButton { Name = "SectionTwinToggle", Content = "Show table", MinHeight = 24, Padding = new Thickness(8, 0), VerticalContentAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(TwinToggle, "Section chart table twin");
        TwinToggle.IsCheckedChanged += (_, _) => ShowTwin();
        Content = root;
    }

    public ToggleButton TwinToggle { get; }

    /// <summary>The content last built (null when there is no section tier), for the checks.</summary>
    public SectionView? Shown { get; private set; }

    public string ChosenChart => chosen;

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
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        bar.Children.Add(new TextBlock { Text = Shown.StationName, Classes = { "pnl-head" }, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        foreach (string id in Selector)
        {
            var button = new ToggleButton { Name = "SectionChart-" + id, Content = Shown.Charts.First(c => c.Id == id).Title, MinHeight = 24, Padding = new Thickness(8, 0),
                VerticalContentAlignment = VerticalAlignment.Center, IsChecked = id == chosen };
            AutomationProperties.SetName(button, "Chart: " + button.Content);
            string captured = id;
            button.Click += (_, _) => Choose(captured);
            buttons[id] = button;
            bar.Children.Add(button);
        }
        bar.Children.Add(TwinToggle);
        root.Children.Add(bar);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2) };
        row.Children.Add(profile);
        row.Children.Add(chart);
        row.Children.Add(twin);
        root.Children.Add(row);
        root.Children.Add(new TextBlock { Name = "SectionChartLegend", Classes = { "pnl-note" } });
        Refresh();
        foreach (ResultGroup group in Shown.Groups) root.Children.Add(AnalysisPanel.Table("section-" + group.Title.ToLowerInvariant().Replace(' ', '-').Replace("-", "") + "-table", group));
    }

    private void Refresh()
    {
        if (Shown is null) return;
        profile.Model = Shown.Charts.First(c => c.Id == "profile");
        ChartModel model = Shown.Charts.First(c => c.Id == chosen);
        chart.Model = model;
        if (root.Children.OfType<TextBlock>().FirstOrDefault(t => t.Name == "SectionChartLegend") is { } legend)
            legend.Text = string.Join(" · ", new[] { model.Unavailable, model.Legend, model.Label }.Where(s => !string.IsNullOrEmpty(s)));
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
