using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Spanwise loading chart (Cl·c/c̄ vs η, with the elliptic loading of the same CL dashed) and its table twin (§12.4: a
/// chart is never the only carrier of a number). The twin toggle is built once; <see cref="Update"/> refills only the
/// body, so a re-render while the toggle has focus never moves it (the planted mutant rebuilds the toggle and loses focus).
/// Gaps in the data are never bridged: a point with no value ends the polyline.
/// </summary>
public sealed class LoadingChart : UserControl
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly LoadingPlot plot = new();
    private readonly StackPanel table = new() { Name = "loading-table" };
    private readonly ContentControl body = new();
    private IReadOnlyList<LoadingPoint> points = [];

    public LoadingChart()
    {
        TwinToggle = new ToggleButton { Name = "LoadingTwinToggle", Content = "Show table" };
        AutomationProperties.SetName(TwinToggle, "Spanwise loading table twin");
        TwinToggle.IsCheckedChanged += (_, _) => ShowBody();
        var caption = new TextBlock { Text = Labels.ChartBasis, TextWrapping = TextWrapping.Wrap, Classes = { "caption" } };
        // Grids, not a dock panel: the Dock library is confined to Shell/ (Architecture_DockConfinedToShell).
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Grid.SetColumn(TwinToggle, 1);
        header.Children.Add(caption);
        header.Children.Add(TwinToggle);
        AutomationProperties.SetName(table, "Spanwise loading table");
        AutomationProperties.SetName(plot, Labels.ChartBasis + ", starboard half; table twin available");
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        Grid.SetRow(body, 1);
        root.Children.Add(header);
        root.Children.Add(body);
        Content = root;
        ShowBody();
    }

    /// <summary>The persistent twin toggle: the focus owner the re-render must never replace.</summary>
    public ToggleButton TwinToggle { get; }

    public bool ShowingTable => TwinToggle.IsChecked == true;

    public IReadOnlyList<LoadingPoint> Points => points;

    /// <summary>The table twin's rows, one per point, in the order drawn (read by the tests).</summary>
    public int TwinRowCount => table.Children.Count - 1;

    /// <summary>Redraws the chart and refills the twin from the same points; the toggle and its focus are untouched.</summary>
    public void Update(IReadOnlyList<LoadingPoint> loading)
    {
        points = loading ?? throw new ArgumentNullException(nameof(loading));
        plot.Points = points;
        table.Children.Clear();
        table.Children.Add(Row(true, "η", "Cl·c/c̄", "Elliptic", "α_eff °", "L/span N/m"));
        foreach (var point in points)
            table.Children.Add(Row(false, Num(point.Eta), Num(point.ClChordOverMeanChord), Num(point.EllipticReference),
                point.AlphaEffDeg.ToString("0.##", Inv), Num(point.LiftPerSpan)));
    }

    private void ShowBody()
    {
        TwinToggle.Content = ShowingTable ? "Show chart notes" : "Show table";
        body.Content = ShowingTable ? table : plot;
    }

    private static string Num(double? value) => value.HasValue && double.IsFinite(value.Value) ? value.Value.ToString("0.###", Inv) : "Unavailable";

    private static Grid Row(bool head, params string[] cells)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*") };
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = new TextBlock
            {
                Text = cells[i], TextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Right,
                FontWeight = head ? FontWeight.SemiBold : FontWeight.Normal, Margin = new Thickness(4, 1)
            };
            Grid.SetColumn(cell, i);
            row.Children.Add(cell);
        }
        return row;
    }

    private sealed class LoadingPlot : Control
    {
        private IReadOnlyList<LoadingPoint> data = [];
        private const double Pad = 28;

        public IReadOnlyList<LoadingPoint> Points
        {
            get => data;
            set { data = value; InvalidateVisual(); }
        }

        public LoadingPlot()
        {
            MinHeight = 120;
            Focusable = false;
        }

        public override void Render(DrawingContext context)
        {
            var area = new Rect(Pad, 6, Math.Max(0, Bounds.Width - Pad - 8), Math.Max(0, Bounds.Height - Pad - 6));
            var ink = Brush("InkBrush");
            var mute = Brush("MutedBrush");
            var line = Brush("FoilBrush");
            var axis = new Pen(mute, 1);
            context.DrawLine(axis, area.BottomLeft, area.BottomRight);
            context.DrawLine(axis, area.BottomLeft, area.TopLeft);
            if (data.Count == 0 || area.Width <= 0 || area.Height <= 0) return;
            double max = data.Max(point => Math.Max(Finite(point.ClChordOverMeanChord), Finite(point.EllipticReference)));
            if (max <= 0) max = 1;
            Point At(double eta, double value) => new(area.Left + eta * area.Width, area.Bottom - value / max * area.Height);
            DrawText(context, "0", new Point(area.Left - 4, area.Bottom + 2), mute);
            DrawText(context, "1", new Point(area.Right - 4, area.Bottom + 2), mute);
            DrawText(context, "η", new Point(area.Center.X, area.Bottom + 12), ink);
            DrawText(context, max.ToString("0.##", Inv), new Point(2, area.Top), mute);
            Polyline(context, new Pen(mute, 1.5, new DashStyle([4, 3], 0)), point => point.EllipticReference, At);
            Polyline(context, new Pen(line, 2), point => point.ClChordOverMeanChord, At);
            foreach (var point in data)
                if (point.ClChordOverMeanChord is { } value && double.IsFinite(value))
                    context.DrawEllipse(line, null, At(point.Eta, value), 2.5, 2.5);
        }

        private void Polyline(DrawingContext context, Pen pen, Func<LoadingPoint, double?> value, Func<double, double, Point> at)
        {
            Point? previous = null;
            foreach (var point in data.OrderBy(item => item.Eta))
            {
                if (value(point) is not { } v || !double.IsFinite(v)) { previous = null; continue; }
                var next = at(point.Eta, v);
                if (previous is { } from) context.DrawLine(pen, from, next);
                previous = next;
            }
        }

        private static double Finite(double? value) => value.HasValue && double.IsFinite(value.Value) ? value.Value : 0;

        private IBrush Brush(string key) => this.TryFindResource(key, ActualThemeVariant, out var found) && found is IBrush brush ? brush : Brushes.Gray;

        private static void DrawText(DrawingContext context, string text, Point at, IBrush brush) =>
            context.DrawText(new FormattedText(text, Inv, FlowDirection.LeftToRight, Typeface.Default, 11, brush), at);
    }
}
