using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Sweeps;

/// <summary>
/// Theme-aware native Avalonia performance curve chart for Area 4 local sweeps (ANA-16, RES-04, UI-17).
/// Draws non-bridged segments only (zero line across failed/pruned/pending gaps), distinct stroke dash +
/// marker shape + direct end label per series (WCAG 1.4.1), explicit gap × markers, and pointer/keyboard
/// linked case selection.
/// </summary>
public sealed class PerformanceCurveChart : Control
{
    private static readonly double[][] DashPatterns =
    [
        [],
        [6, 3],
        [2, 2],
        [8, 3, 2, 3]
    ];

    private static readonly string[] BatlowKeys = ["Batlow0Brush", "Batlow1Brush", "Batlow2Brush", "Batlow3Brush", "Batlow4Brush"];

    private readonly List<(Point Pos, int CaseIndex)> hitTargets = [];

    public PerformanceCurveChart()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public event Action<int>? CaseSelected;

    public SweepChartData? ChartData { get; private set; }
    public int SelectedCaseIndex { get; private set; } = -1;

    /// <summary>Observable counts for headless/native tests (non-bridged segment and gap verification).</summary>
    public int RenderedSegmentCount { get; private set; }
    public int RenderedGapCount { get; private set; }

    public void Update(SweepChartData? data, int selectedCaseIndex)
    {
        ChartData = data;
        SelectedCaseIndex = selectedCaseIndex;
        AutomationProperties.SetName(this, data is null ? "Performance curve chart (empty)" : $"{data.Title} vs {data.XAxisLabel}");
        Recount();
        InvalidateVisual();
    }

    private void Recount()
    {
        if (ChartData is null || ChartData.UnavailableReason is not null)
        {
            RenderedSegmentCount = 0;
            RenderedGapCount = ChartData?.Series.Sum(s => s.Gaps.Count) ?? 0;
            return;
        }
        RenderedSegmentCount = ChartData.Series.Sum(s => s.PrimarySegments.Count);
        RenderedGapCount = ChartData.Series.Sum(s => s.Gaps.Count);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var pos = e.GetPosition(this);
        int bestCase = -1;
        double bestDist = 18.0;
        foreach (var (pt, idx) in hitTargets)
        {
            double d = double.Hypot(pt.X - pos.X, pt.Y - pos.Y);
            if (d < bestDist) { bestDist = d; bestCase = idx; }
        }
        if (bestCase >= 0)
        {
            CaseSelected?.Invoke(bestCase);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (ChartData is null || ChartData.TableRows.Count == 0) return;
        int count = ChartData.TableRows.Count;
        if (e.Key is Key.Right or Key.Down)
        {
            int next = SelectedCaseIndex < 0 ? 0 : (SelectedCaseIndex + 1) % count;
            CaseSelected?.Invoke(next);
            e.Handled = true;
        }
        else if (e.Key is Key.Left or Key.Up)
        {
            int prev = SelectedCaseIndex <= 0 ? count - 1 : SelectedCaseIndex - 1;
            CaseSelected?.Invoke(prev);
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        hitTargets.Clear();
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var surface = Brush("SurfaceBrush", Brushes.White);
        var ink = Brush("InkBrush", Brushes.Black);
        var muted = Brush("MutedBrush", Brushes.Gray);
        var line = Brush("LineBrush", Brushes.LightGray);
        var danger = Brush("DangerBrush", Brushes.Red);
        var focus = Brush("FocusRingBrush", Brush("PrimaryBrush", Brushes.Teal));

        context.FillRectangle(surface, new Rect(0, 0, w, h));
        context.DrawRectangle(null, new Pen(line, 1), new Rect(0.5, 0.5, w - 1, h - 1));

        if (ChartData is null)
        {
            DrawCenteredText(context, "No sweep results to plot.", ink, w, h);
            return;
        }

        // Title & subtitle
        DrawText(context, ChartData.Title, 13, FontWeight.SemiBold, ink, new Point(12, 8));
        DrawText(context, ChartData.Subtitle, 11, FontWeight.Normal, muted, new Point(12, 26));

        if (ChartData.UnavailableReason is not null)
        {
            DrawCenteredText(context, ChartData.UnavailableReason, muted, w, h);
            return;
        }

        double left = 58, right = Math.Max(left + 40, w - 88), top = 48, bottom = Math.Max(top + 40, h - 38);
        var plot = new Rect(left, top, right - left, bottom - top);

        // Compute bounds over all valid points & gaps
        var allX = new List<double>();
        var allY = new List<double>();
        foreach (var s in ChartData.Series)
        {
            foreach (var seg in s.PrimarySegments) foreach (var p in seg.Points) { allX.Add(p.X); allY.Add(p.Y); }
            foreach (var seg in s.BandUpperSegments) foreach (var p in seg.Points) { allX.Add(p.X); allY.Add(p.Y); }
            foreach (var seg in s.SecondarySegments) foreach (var p in seg.Points) { allX.Add(p.X); allY.Add(p.Y); }
            foreach (var g in s.Gaps) allX.Add(g.X);
        }

        if (allX.Count == 0 || allY.Count == 0)
        {
            DrawCenteredText(context, "No completed cases available for this curve.", muted, w, h);
            return;
        }

        double xMin = allX.Min(), xMax = allX.Max();
        if (Math.Abs(xMax - xMin) < 1e-9) { xMin -= 1; xMax += 1; }
        double yMin = allY.Min(), yMax = allY.Max();
        double yPad = Math.Max(0.02, (yMax - yMin) * 0.12);
        yMin -= yPad;
        yMax += yPad;

        double MapX(double x) => plot.Left + (x - xMin) / (xMax - xMin) * plot.Width;
        double MapY(double y) => plot.Bottom - (y - yMin) / (yMax - yMin) * plot.Height;

        // Grid and axes
        var gridPen = new Pen(line, 1);
        for (int t = 0; t <= 4; t++)
        {
            double frac = t / 4.0;
            double gy = plot.Bottom - frac * plot.Height;
            context.DrawLine(gridPen, new Point(plot.Left, gy), new Point(plot.Right, gy));
            double yVal = yMin + frac * (yMax - yMin);
            DrawText(context, yVal.ToString("0.###", CultureInfo.InvariantCulture), 10, FontWeight.Normal, muted, new Point(6, gy - 6));

            double gx = plot.Left + frac * plot.Width;
            double xVal = xMin + frac * (xMax - xMin);
            DrawText(context, xVal.ToString("0.##", CultureInfo.InvariantCulture), 10, FontWeight.Normal, muted, new Point(gx - 12, plot.Bottom + 4));
        }

        var axisPen = new Pen(ink, 1);
        context.DrawLine(axisPen, new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom));
        context.DrawLine(axisPen, new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));
        DrawText(context, ChartData.XAxisLabel, 11, FontWeight.SemiBold, ink, new Point(plot.Left + plot.Width / 2 - 20, h - 18));

        // Plot each series: strictly intra-segment lines only (NEVER bridge across gaps!).
        foreach (var s in ChartData.Series)
        {
            var seriesBrush = SeriesBrush(s.StrokePatternIndex, ink);
            double[] dash = DashPatterns[s.StrokePatternIndex % DashPatterns.Length];
            var pen = dash.Length == 0 ? new Pen(seriesBrush, 2) : new Pen(seriesBrush, 2, new DashStyle(dash, 0));
            var bandPen = new Pen(seriesBrush, 1.25, new DashStyle([3, 3], 0));
            var secondaryPen = new Pen(muted, 1.25, new DashStyle([4, 2], 0));

            Point? lastPoint = null;
            foreach (var seg in s.PrimarySegments)
            {
                for (int i = 0; i < seg.Points.Count; i++)
                {
                    var p = seg.Points[i];
                    var pt = new Point(MapX(p.X), MapY(p.Y));
                    if (i > 0)
                    {
                        var prev = seg.Points[i - 1];
                        context.DrawLine(pen, new Point(MapX(prev.X), MapY(prev.Y)), pt);
                    }
                    DrawMarker(context, pt, s.StrokePatternIndex, seriesBrush, p.CaseIndex == SelectedCaseIndex ? focus : null);
                    hitTargets.Add((pt, p.CaseIndex));
                    lastPoint = pt;
                }
            }

            foreach (var seg in s.BandUpperSegments)
            {
                for (int i = 1; i < seg.Points.Count; i++)
                    context.DrawLine(bandPen, new Point(MapX(seg.Points[i - 1].X), MapY(seg.Points[i - 1].Y)),
                        new Point(MapX(seg.Points[i].X), MapY(seg.Points[i].Y)));
            }

            foreach (var seg in s.SecondarySegments)
            {
                for (int i = 1; i < seg.Points.Count; i++)
                    context.DrawLine(secondaryPen, new Point(MapX(seg.Points[i - 1].X), MapY(seg.Points[i - 1].Y)),
                        new Point(MapX(seg.Points[i].X), MapY(seg.Points[i].Y)));
            }

            // Direct end label (never colour alone)
            if (lastPoint is { } lp)
                DrawText(context, s.Label, 10, FontWeight.SemiBold, seriesBrush, new Point(Math.Min(plot.Right + 4, w - 82), lp.Y - 6));

            // Explicit gap × markers on x-axis
            var gapPen = new Pen(danger, 2);
            foreach (var gap in s.Gaps)
            {
                var gp = new Point(MapX(gap.X), plot.Bottom - 8);
                context.DrawLine(gapPen, new Point(gp.X - 4, gp.Y - 4), new Point(gp.X + 4, gp.Y + 4));
                context.DrawLine(gapPen, new Point(gp.X + 4, gp.Y - 4), new Point(gp.X - 4, gp.Y + 4));
                if (gap.CaseIndex == SelectedCaseIndex)
                    context.DrawEllipse(null, new Pen(focus, 2), gp, 8, 8);
                hitTargets.Add((gp, gap.CaseIndex));
            }
        }
    }

    private static void DrawMarker(DrawingContext context, Point center, int patternIndex, IBrush fill, IBrush? selectionRing)
    {
        const double r = 4.0;
        switch (patternIndex % 4)
        {
            case 0:
                context.DrawEllipse(fill, null, center, r, r);
                break;
            case 1:
                context.DrawRectangle(fill, null, new Rect(center.X - r, center.Y - r, 2 * r, 2 * r));
                break;
            case 2:
                var diamond = new StreamGeometry();
                using (var g = diamond.Open())
                {
                    g.BeginFigure(new Point(center.X, center.Y - r), true);
                    g.LineTo(new Point(center.X + r, center.Y));
                    g.LineTo(new Point(center.X, center.Y + r));
                    g.LineTo(new Point(center.X - r, center.Y));
                    g.EndFigure(true);
                }
                context.DrawGeometry(fill, null, diamond);
                break;
            default:
                var tri = new StreamGeometry();
                using (var g = tri.Open())
                {
                    g.BeginFigure(new Point(center.X, center.Y - r), true);
                    g.LineTo(new Point(center.X + r, center.Y + r));
                    g.LineTo(new Point(center.X - r, center.Y + r));
                    g.EndFigure(true);
                }
                context.DrawGeometry(fill, null, tri);
                break;
        }
        if (selectionRing is not null)
            context.DrawEllipse(null, new Pen(selectionRing, 2), center, r + 3.5, r + 3.5);
    }

    private IBrush SeriesBrush(int idx, IBrush fallback)
    {
        if (ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark || ActualThemeVariant == NativeReviewThemes.HighContrast)
            return idx == 0 ? Brush("PrimaryBrush", fallback) : idx == 1 ? Brush("WarningBrush", fallback) : Brush("InkBrush", fallback);
        return Brush(BatlowKeys[idx % BatlowKeys.Length], fallback);
    }

    private IBrush Brush(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var val) && val is IBrush b ? b : fallback;

    private static void DrawCenteredText(DrawingContext ctx, string text, IBrush brush, double w, double h)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, brush);
        ctx.DrawText(ft, new Point(Math.Max(12, (w - ft.Width) / 2), Math.Max(48, (h - ft.Height) / 2)));
    }

    private static void DrawText(DrawingContext ctx, string text, double size, FontWeight weight, IBrush brush, Point at)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, weight), size, brush);
        ctx.DrawText(ft, at);
    }
}
