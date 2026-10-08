using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// Draws one <see cref="ChartModel"/>: its plots side by side, each with axes, series told apart by dash and marker as well as
/// colour, an optional band, and its markers. A series with no points draws nothing, never a line through a gap. The number of
/// plots, series and points it drew is kept for the checks (<see cref="Drawn"/>).
/// </summary>
public sealed class SectionChartView : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private ChartModel? model;

    public ChartModel? Model
    {
        get => model;
        set { model = value; AutomationProperties.SetName(this, value is null ? "Section chart" : value.Title + " chart; table twin available"); InvalidateVisual(); }
    }

    /// <summary>(plots, series lines, points) drawn by the last render.</summary>
    public (int Plots, int Series, int Points) Drawn { get; private set; }

    private IBrush Resource(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        int plots = 0, series = 0, points = 0;
        if (model is { Unavailable: null } chart && chart.Plots.Count > 0 && Bounds.Width > 40 && Bounds.Height > 30)
        {
            IBrush ink = Resource("InkBrush", Brushes.Black), mute = Resource("MutedBrush", Brushes.Gray), accent = Resource("AccentBrush", Brushes.Teal);
            IBrush[] colours = [accent, Resource("WarnBrush", Brushes.DarkGoldenrod), Resource("DangerBrush", Brushes.Firebrick), mute];
            double width = Bounds.Width / chart.Plots.Count;
            for (int i = 0; i < chart.Plots.Count; i++)
            {
                (int s, int p) = Plot(context, chart.Plots[i], new Rect(i * width + 34, 14, width - 44, Bounds.Height - 36), ink, mute, colours);
                plots++; series += s; points += p;
            }
        }
        Drawn = (plots, series, points);
    }

    private (int Series, int Points) Plot(DrawingContext context, ChartPlot plot, Rect area, IBrush ink, IBrush mute, IBrush[] colours)
    {
        var all = plot.Series.SelectMany(s => s.Points).Concat(plot.Markers.Where(m => m.Y is not null).Select(m => new ChartPoint(m.X, m.Y!.Value))).ToList();
        context.DrawText(Text(plot.Title + " · " + plot.YTitle, mute), new Point(area.Left, 0));
        context.DrawRectangle(null, new Pen(mute, 1), area);
        if (all.Count == 0) { context.DrawText(Text(plot.XTitle, mute), new Point(area.Left, area.Bottom + 2)); return (0, 0); }
        double x0 = all.Min(p => p.X), x1 = all.Max(p => p.X), y0 = all.Min(p => p.Y), y1 = all.Max(p => p.Y);
        foreach (ChartMarker m in plot.Markers.Where(m => m.Y is null)) { x0 = Math.Min(x0, m.X); x1 = Math.Max(x1, m.X); }
        if (x1 - x0 < 1e-12) x1 = x0 + 1;
        if (y1 - y0 < 1e-12) { y0 -= 0.5; y1 += 0.5; }
        if (plot.ExtentStep is { } step) { y0 = ChartPlot.RoundOut(y0, step, low: true); y1 = ChartPlot.RoundOut(y1, step, low: false); } // Ruling 144: no Cp_min printed
        else { double pad = 0.06 * (y1 - y0); y0 -= pad; y1 += pad; }
        Point At(ChartPoint p) => new(area.Left + (p.X - x0) / (x1 - x0) * area.Width,
            plot.InvertY ? area.Top + (p.Y - y0) / (y1 - y0) * area.Height : area.Bottom - (p.Y - y0) / (y1 - y0) * area.Height);
        context.DrawText(Text(x0.ToString("0.##", Inv), mute), new Point(area.Left, area.Bottom + 2));
        context.DrawText(Text(plot.XTitle + " " + x1.ToString("0.##", Inv), mute), new Point(area.Right - 70, area.Bottom + 2));
        context.DrawText(Text((plot.InvertY ? y1 : y0).ToString("0.##", Inv), mute), new Point(0, area.Bottom - 12));
        context.DrawText(Text((plot.InvertY ? y0 : y1).ToString("0.##", Inv), mute), new Point(0, area.Top));
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
            context.DrawGeometry(new SolidColorBrush(((ISolidColorBrush)colours[0]).Color, 0.18), null, fill);
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
            context.DrawText(Text(m.Name, ink), At(new ChartPoint(m.X, m.Y ?? y1)) + new Point(4, -12));
        }
        return (lines, drawn);
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

    private static FormattedText Text(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, Typeface.Default, 10, brush);
}
