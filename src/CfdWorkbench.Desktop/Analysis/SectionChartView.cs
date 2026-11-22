using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// Draws one <see cref="ChartModel"/>: its plots side by side, each with axes, compact series legend, series told apart
/// by dash and marker as well as colour, an optional band, and clamped markers. A series with no points draws nothing,
/// never a line through a gap. The number of plots, series and points it drew is kept for the checks (<see cref="Drawn"/>).
/// </summary>
public sealed class SectionChartView : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private ChartModel? model;

    public SectionChartView()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public ChartModel? Model
    {
        get => model;
        set { model = value; AutomationProperties.SetName(this, value is null ? "Section chart" : value.Title + " chart; table twin available"); InvalidateVisual(); }
    }

    /// <summary>(plots, series lines, points) drawn by the last render.</summary>
    public (int Plots, int Series, int Points) Drawn { get; private set; }

    /// <summary>Series legend labels drawn during the last render.</summary>
    public IReadOnlyList<string> DrawnLegendItems { get; private set; } = [];

    /// <summary>Measured bounding rectangles of X-title and marker labels drawn during the last render.</summary>
    public IReadOnlyList<Rect> DrawnLabelBounds { get; private set; } = [];

    private IBrush Resource(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        int plots = 0, series = 0, points = 0;
        var legendItems = new List<string>();
        var labelBounds = new List<Rect>();
        if (model is { Unavailable: null } chart && chart.Plots.Count > 0 && Bounds.Width > 40 && Bounds.Height > 30)
        {
            IBrush ink = Resource("InkBrush", Brushes.Black), mute = Resource("MutedBrush", Brushes.Gray), accent = Resource("PrimaryBrush", Brushes.Teal);
            IBrush[] colours = [accent, Resource("WarningBrush", Brushes.DarkGoldenrod), Resource("DangerBrush", Brushes.Firebrick), mute];
            double width = Bounds.Width / chart.Plots.Count;
            for (int i = 0; i < chart.Plots.Count; i++)
            {
                bool hasLegend = chart.Plots[i].Series.Any(s => s.Points.Count > 0);
                double topPad = hasLegend ? 28 : 16;
                double bottomPad = 24;
                double plotHeight = Math.Max(20, Bounds.Height - topPad - bottomPad);
                (int s, int p) = Plot(context, chart.Plots[i], new Rect(i * width + 34, topPad, Math.Max(20, width - 44), plotHeight),
                    ink, mute, colours, legendItems, labelBounds);
                plots++; series += s; points += p;
            }
        }
        Drawn = (plots, series, points);
        DrawnLegendItems = legendItems;
        DrawnLabelBounds = labelBounds;
    }

    private (int Series, int Points) Plot(DrawingContext context, ChartPlot plot, Rect area, IBrush ink, IBrush mute, IBrush[] colours,
        List<string> legendItems, List<Rect> labelBounds)
    {
        var all = plot.Series.SelectMany(s => s.Points).Concat(plot.Markers.Where(m => m.Y is not null).Select(m => new ChartPoint(m.X, m.Y!.Value))).ToList();
        var titleText = Text(plot.Title + " · " + plot.YTitle, mute);
        context.DrawText(titleText, new Point(area.Left, 0));

        // F-28: compact per-plot series legend inside [area.Left .. area.Right], never encroaching on the left Y-tick margin.
        var activeSeries = plot.Series.Where(s => s.Points.Count > 0).ToArray();
        if (activeSeries.Length > 0)
        {
            double lx = area.Left;
            double ly = Math.Max(titleText.Height, 13);
            foreach (ChartSeries s in activeSeries)
            {
                IBrush brush = colours[s.Colour % colours.Length];
                var itemText = Text(s.Name, ink);
                double swatchW = 16;
                double itemW = swatchW + 4 + itemText.Width + 10;
                if (lx > area.Left && lx + itemW > area.Right + 8) break;
                double cy = ly + itemText.Height / 2;
                var swatchPen = new Pen(brush, 1.5, s.Dashed ? new DashStyle([4, 3], 0) : null);
                context.DrawLine(swatchPen, new Point(lx, cy), new Point(lx + swatchW, cy));
                if (s.Marker != "none") Marker(context, brush, new Point(lx + swatchW / 2, cy), s.Marker);
                context.DrawText(itemText, new Point(lx + swatchW + 4, ly));
                legendItems.Add(s.Name);
                lx += itemW;
            }
        }

        context.DrawRectangle(null, new Pen(mute, 1), area);
        if (all.Count == 0)
        {
            var emptyX = Text(plot.XTitle, mute);
            Point pos = ClampPoint(area.Left, area.Bottom + 2, emptyX.Width, emptyX.Height);
            context.DrawText(emptyX, pos);
            labelBounds.Add(new Rect(pos, new Size(emptyX.Width, emptyX.Height)));
            return (0, 0);
        }

        double x0 = all.Min(p => p.X), x1 = all.Max(p => p.X), y0 = all.Min(p => p.Y), y1 = all.Max(p => p.Y);
        foreach (ChartMarker m in plot.Markers.Where(m => m.Y is null)) { x0 = Math.Min(x0, m.X); x1 = Math.Max(x1, m.X); }
        if (x1 - x0 < 1e-12) x1 = x0 + 1;
        if (y1 - y0 < 1e-12) { y0 -= 0.5; y1 += 0.5; }
        if (plot.ExtentStep is { } step) { y0 = ChartPlot.RoundOut(y0, step, low: true); y1 = ChartPlot.RoundOut(y1, step, low: false); } // Ruling 144: no Cp_min printed
        else { double pad = 0.06 * (y1 - y0); y0 -= pad; y1 += pad; }

        Point At(ChartPoint p) => new(area.Left + (p.X - x0) / (x1 - x0) * area.Width,
            plot.InvertY ? area.Top + (p.Y - y0) / (y1 - y0) * area.Height : area.Bottom - (p.Y - y0) / (y1 - y0) * area.Height);

        var xMinText = Text(x0.ToString("0.##", Inv), mute);
        Point xMinPos = ClampPoint(area.Left, area.Bottom + 2, xMinText.Width, xMinText.Height);
        context.DrawText(xMinText, xMinPos);

        // F-41: measure and clamp X-axis title so it never clips outside the plot/view right edge.
        var xTitleText = Text(plot.XTitle + " " + x1.ToString("0.##", Inv), mute);
        double xTitleX = Math.Max(xMinPos.X + xMinText.Width + 6, area.Right - xTitleText.Width);
        Point xTitlePos = ClampPoint(xTitleX, area.Bottom + 2, xTitleText.Width, xTitleText.Height);
        context.DrawText(xTitleText, xTitlePos);
        labelBounds.Add(new Rect(xTitlePos, new Size(xTitleText.Width, xTitleText.Height)));

        var yLow = Text((plot.InvertY ? y1 : y0).ToString("0.##", Inv), mute);
        var yHigh = Text((plot.InvertY ? y0 : y1).ToString("0.##", Inv), mute);
        context.DrawText(yLow, new Point(Math.Max(area.Left - 34, area.Left - 4 - yLow.Width), Math.Max(area.Top, area.Bottom - yLow.Height)));
        context.DrawText(yHigh, new Point(Math.Max(area.Left - 34, area.Left - 4 - yHigh.Width), area.Top));

        if (plot.Band is { } band && plot.Series.FirstOrDefault(s => s.Name == band.Low) is { } low && plot.Series.FirstOrDefault(s => s.Name == band.High) is { } high &&
            low.Points.Count > 1 && low.Points.Count == high.Points.Count)
        {
            var fill = new StreamGeometry();
            using (StreamGeometryContext g = fill.Open())
            {
                g.BeginFigure(At(low.Points[0]), true);
                foreach (ChartPoint p in low.Points.Skip(1)) g.LineTo(At(p));
                foreach (ChartPoint p in high.Points.AsEnumerable().Reverse()) g.LineTo(At(p));
                g.EndFigure(true);
            }
            bool highContrast = ActualThemeVariant == NativeReviewThemes.HighContrast;
            IBrush? bandFill = !highContrast && colours[0] is ISolidColorBrush solidBand
                ? new SolidColorBrush(solidBand.Color, 0.18)
                : null;
            IPen? bandStroke = highContrast ? new Pen(colours[0], 1, new DashStyle([3, 3], 0)) : null;
            context.DrawGeometry(bandFill, bandStroke, fill);
        }

        int lines = 0, drawn = 0;
        foreach (ChartSeries s in plot.Series)
        {
            if (s.Points.Count == 0) continue;
            IBrush brush = colours[s.Colour % colours.Length];
            var pen = new Pen(brush, 1.5, s.Dashed ? new DashStyle([4, 3], 0) : null);
            Point? last = null;
            foreach (ChartPoint p in s.Points)
            {
                Point at = At(p);
                if (last is { } from) context.DrawLine(pen, from, at);
                last = at; drawn++;
                if (s.Marker != "none") Marker(context, brush, at, s.Marker);
            }
            lines++;
        }

        foreach (ChartMarker m in plot.Markers)
        {
            if (m.Y is null) context.DrawLine(new Pen(ink, 1, new DashStyle([2, 2], 0)), At(new ChartPoint(m.X, y0)), At(new ChartPoint(m.X, y1)));
            else context.DrawEllipse(ink, null, At(new ChartPoint(m.X, m.Y.Value)), 3.5, 3.5);
            var markerText = Text(m.Name, ink);
            Point anchor = At(new ChartPoint(m.X, m.Y ?? y1));
            Point markerPos = ClampPoint(anchor.X + 4, anchor.Y - markerText.Height, markerText.Width, markerText.Height);
            context.DrawText(markerText, markerPos);
            labelBounds.Add(new Rect(markerPos, new Size(markerText.Width, markerText.Height)));
        }
        return (lines, drawn);
    }

    private Point ClampPoint(double x, double y, double width, double height)
    {
        double clampedX = Math.Clamp(x, 0, Math.Max(0, Bounds.Width - width));
        double clampedY = Math.Clamp(y, 0, Math.Max(0, Bounds.Height - height));
        return new Point(clampedX, clampedY);
    }

    private static void Marker(DrawingContext context, IBrush brush, Point at, string kind)
    {
        const double r = 3;
        switch (kind)
        {
            case "square": context.DrawRectangle(brush, null, new Rect(at.X - r, at.Y - r, 2 * r, 2 * r)); break;
            case "triangle": context.DrawGeometry(brush, null, Polygon([new(at.X, at.Y - r - 1), new(at.X + r + 1, at.Y + r), new(at.X - r - 1, at.Y + r)])); break;
            case "diamond": context.DrawGeometry(brush, null, Polygon([new(at.X, at.Y - r - 1), new(at.X + r + 1, at.Y), new(at.X, at.Y + r + 1), new(at.X - r - 1, at.Y)])); break;
            default: context.DrawEllipse(brush, null, at, r, r); break;
        }
    }

    private static StreamGeometry Polygon(Point[] corners)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext g = geometry.Open();
        g.BeginFigure(corners[0], true);
        foreach (Point corner in corners.Skip(1)) g.LineTo(corner);
        g.EndFigure(true);
        return geometry;
    }

    private FormattedText Text(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, CanvasTypography.ResolveTypeface(this), CanvasTypography.ResolveFontSize(this), brush);
}
