using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>
/// Lightweight orthographic Front (anhedral/dihedral) and Starboard (section side silhouette) elevation canvas
/// for Tab 3 (<c>Plan &amp; Elevations</c>) of <see cref="NotebookWorkspaceView"/>.
/// Uses closed <c>Styles.axaml</c> graphite viewport tokens only (zero new theme brushes, zero <c>Color.Parse</c>,
/// typography always <c>&gt;= 11 px</c> via <see cref="CanvasTypography"/>).
/// </summary>
public sealed class NotebookElevationCanvas : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private CandidateIntentDelta? activeDelta;
    private CandidateIntentDelta? baselineDelta;
    private double? hOverCRoot;
    private double? frH;
    private bool depthUnset;

    public NotebookElevationCanvas()
    {
        ClipToBounds = true;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public int DrawnElevations { get; private set; }
    public bool ShallowFreeSurfaceNoticeDrawn { get; private set; }

    public void Update(
        CandidateIntentDelta? delta,
        CandidateIntentDelta? compareBaseline,
        double? hOverC,
        double? froudeH,
        bool isDepthUnset)
    {
        activeDelta = delta;
        baselineDelta = compareBaseline;
        hOverCRoot = hOverC;
        frH = froudeH;
        depthUnset = isDepthUnset;

        string name = delta is null
            ? "Front and starboard elevations (empty)"
            : string.Create(Inv, $"Front elevation (anhedral {delta.AnhedralDeg:0.0} deg, span {delta.SpanMeters * 1000:0.0} mm) and starboard elevation (root chord {delta.RootChordMeters * 1000:0.0} mm, washout {delta.WashoutDeg:0.0} deg)");
        AutomationProperties.SetName(this, name);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        DrawnElevations = 0;
        ShallowFreeSurfaceNoticeDrawn = false;

        var rect = new Rect(Bounds.Size);
        if (rect.Width < 80 || rect.Height < 60) return;

        IBrush bg = ResolveBrush("ViewportBrush", Brushes.Black);
        IBrush soft = ResolveBrush("PlanSoftBrush", Brushes.DarkSlateGray);
        IBrush grid = ResolveBrush("ViewportGridBrush", Brushes.DimGray);
        IBrush ink = ResolveBrush("ViewportInkBrush", Brushes.White);
        IBrush mute = ResolveBrush("PlanMuteBrush", Brushes.LightGray);
        IBrush foil = ResolveBrush("FoilBrush", Brushes.CadetBlue);
        IBrush edge = ResolveBrush("FoilEdgeBrush", Brushes.LightCyan);
        IBrush warning = ResolveBrush("PlanWarningBrush", Brushes.Goldenrod);

        context.FillRectangle(bg, rect);
        context.DrawRectangle(null, new Pen(grid, 1), new Rect(0.5, 0.5, rect.Width - 1, rect.Height - 1));

        if (activeDelta is null)
        {
            var emptyText = Label("Orthographic Front & Starboard elevations appear once a candidate is synthesized.", mute);
            context.DrawText(emptyText, new Point(Math.Max(12, (rect.Width - emptyText.Width) * 0.5), Math.Max(12, (rect.Height - emptyText.Height) * 0.5)));
            return;
        }

        double splitX = rect.Width * 0.58;
        context.DrawLine(new Pen(grid, 1), new Point(splitX, 0), new Point(splitX, rect.Height));

        var frontRect = new Rect(8, 8, Math.Max(40, splitX - 16), Math.Max(40, rect.Height - 36));
        var sideRect = new Rect(splitX + 8, 8, Math.Max(40, rect.Width - splitX - 16), Math.Max(40, rect.Height - 36));

        DrawFrontElevation(context, frontRect, activeDelta, baselineDelta, grid, ink, mute, foil, edge);
        DrawStarboardElevation(context, sideRect, activeDelta, baselineDelta, grid, ink, mute, foil, edge);
        DrawnElevations = 2;

        // Bottom honesty band inside viewport for A5.1 / ANA-19.
        string footer;
        IBrush footerBrush;
        if (depthUnset)
        {
            footer = "A5.1 / ANA-19: Free-surface h/c and Fr_h Unavailable — depth not set.";
            footerBrush = mute;
        }
        else if (hOverCRoot is { } hc && hc < 5.0)
        {
            footer = frH is { } fr
                ? string.Create(Inv, $"A5.1 notice: deep-water result; free surface not modelled (h/c = {hc:0.00} < 5, Fr_h = {fr:0.00}).")
                : string.Create(Inv, $"A5.1 notice: deep-water result; free surface not modelled (h/c = {hc:0.00} < 5).");
            footerBrush = warning;
            ShallowFreeSurfaceNoticeDrawn = true;
        }
        else if (hOverCRoot is { } hcDeep && frH is { } frDeep)
        {
            footer = string.Create(Inv, $"Submergence h/c = {hcDeep:0.00} (>= 5), Fr_h = {frDeep:0.00}.");
            footerBrush = mute;
        }
        else
        {
            footer = "Orthographic elevations (Front looking aft · Starboard looking inboard).";
            footerBrush = mute;
        }

        context.FillRectangle(soft, new Rect(1, rect.Height - 24, rect.Width - 2, 23));
        context.DrawText(Label(footer, footerBrush), new Point(10, rect.Height - 20));
    }

    private void DrawFrontElevation(
        DrawingContext context,
        Rect area,
        CandidateIntentDelta delta,
        CandidateIntentDelta? baseline,
        IBrush grid,
        IBrush ink,
        IBrush mute,
        IBrush foil,
        IBrush edge)
    {
        context.DrawText(Label(string.Create(Inv, $"Front (looking aft) · anhedral {delta.AnhedralDeg:0.0} deg"), ink), new Point(area.Left + 4, area.Top + 2));

        double cx = area.Center.X;
        double cy = area.Top + area.Height * 0.52;
        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(area.Left + 8, cy), new Point(area.Right - 8, cy));
        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(cx, area.Top + 20), new Point(cx, area.Bottom - 6));

        double maxSpan = Math.Max(delta.SpanMeters, baseline?.SpanMeters ?? 0.0);
        double scale = Math.Max(20.0, (area.Width - 28) / Math.Max(0.2, maxSpan));

        if (baseline is not null)
        {
            var ghost = BuildFrontGeometry(baseline, cx, cy, scale);
            context.DrawGeometry(null, new Pen(mute, 1.25, new DashStyle([4, 4], 0)), ghost);
        }

        var geo = BuildFrontGeometry(delta, cx, cy, scale);
        IBrush fill = foil is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, 0.30) : foil;
        context.DrawGeometry(fill, new Pen(edge, 1.8), geo);
    }

    private static StreamGeometry BuildFrontGeometry(CandidateIntentDelta delta, double cx, double cy, double scale)
    {
        double half = delta.SpanMeters * 0.5;
        double anhedralSlope = double.TanPi(delta.AnhedralDeg / 180.0);
        double rootT = delta.RootChordMeters * delta.RootThicknessRatio;
        double tipT = delta.TipChordMeters * Math.Max(0.06, delta.RootThicknessRatio * 0.85);
        const int n = 20;

        Point Upper(double sgn, double eta)
        {
            double y = sgn * half * eta;
            double zMid = half * anhedralSlope * eta * eta;
            double halfT = 0.5 * (rootT + (tipT - rootT) * eta);
            return new Point(cx + y * scale, cy - (zMid + halfT) * scale);
        }

        Point Lower(double sgn, double eta)
        {
            double y = sgn * half * eta;
            double zMid = half * anhedralSlope * eta * eta;
            double halfT = 0.5 * (rootT + (tipT - rootT) * eta);
            return new Point(cx + y * scale, cy - (zMid - halfT) * scale);
        }

        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        ctx.BeginFigure(Upper(-1, 1.0), true);
        for (int i = n - 1; i >= 0; i--) ctx.LineTo(Upper(-1, i / (double)n));
        for (int i = 1; i <= n; i++) ctx.LineTo(Upper(1, i / (double)n));
        for (int i = n; i >= 0; i--) ctx.LineTo(Lower(1, i / (double)n));
        for (int i = 1; i <= n; i++) ctx.LineTo(Lower(-1, i / (double)n));
        ctx.EndFigure(true);
        return geo;
    }

    private void DrawStarboardElevation(
        DrawingContext context,
        Rect area,
        CandidateIntentDelta delta,
        CandidateIntentDelta? baseline,
        IBrush grid,
        IBrush ink,
        IBrush mute,
        IBrush foil,
        IBrush edge)
    {
        context.DrawText(Label(string.Create(Inv, $"Starboard · root {delta.RootChordMeters * 1000:0.0} mm · washout {delta.WashoutDeg:0.0} deg"), ink),
            new Point(area.Left + 4, area.Top + 2));

        double cy = area.Top + area.Height * 0.55;
        double left = area.Left + 16;
        double chordPx = Math.Max(40, area.Width - 32);
        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(left, cy), new Point(left + chordPx, cy));

        if (baseline is not null)
        {
            var ghost = BuildSectionSilhouette(baseline.RootThicknessRatio, 0.025, left, cy, chordPx);
            context.DrawGeometry(null, new Pen(mute, 1.25, new DashStyle([4, 4], 0)), ghost);
        }

        // Root silhouette + smaller tip silhouette to illustrate thickness & washout taper.
        var rootGeo = BuildSectionSilhouette(delta.RootThicknessRatio, 0.026, left, cy, chordPx);
        IBrush fill = foil is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, 0.30) : foil;
        context.DrawGeometry(fill, new Pen(edge, 1.8), rootGeo);

        double tipScale = Math.Clamp(delta.Taper, 0.2, 0.9);
        double tipLeft = left + chordPx * 0.25 * (1.0 - tipScale);
        var tipGeo = BuildSectionSilhouette(Math.Max(0.06, delta.RootThicknessRatio * 0.85), 0.018, tipLeft, cy, chordPx * tipScale, delta.WashoutDeg);
        context.DrawGeometry(null, new Pen(ResolveBrush("StationBrush", Brushes.Cyan), 1.4, new DashStyle([3, 2], 0)), tipGeo);
    }

    private static StreamGeometry BuildSectionSilhouette(double tc, double camber, double left, double cy, double chordPx, double twistDeg = 0.0)
    {
        const int n = 36;
        (double sin, double cos) = double.SinCosPi(twistDeg / 180.0);
        double px = left + chordPx * 0.25;

        Point Map(double xc, double zc)
        {
            double dx = (xc - 0.25) * chordPx;
            double dz = zc * chordPx * 1.6;
            double rx = dx * cos + dz * sin;
            double rz = -dx * sin + dz * cos;
            return new Point(px + rx, cy - rz);
        }

        double HalfT(double x) => 5.0 * tc * (0.2969 * Math.Sqrt(Math.Max(0, x)) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x - 0.1015 * x * x * x * x);
        double Camber(double x) => 4.0 * camber * x * (1.0 - x);

        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        ctx.BeginFigure(Map(1.0, 0.0), true);
        for (int i = n - 1; i >= 0; i--)
        {
            double x = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            ctx.LineTo(Map(x, Camber(x) + HalfT(x)));
        }
        for (int i = 1; i <= n; i++)
        {
            double x = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            ctx.LineTo(Map(x, Camber(x) - HalfT(x)));
        }
        ctx.EndFigure(true);
        return geo;
    }

    private IBrush ResolveBrush(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    private FormattedText Label(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, CanvasTypography.ResolveTypeface(this), CanvasTypography.ResolveFontSize(this), brush);
}
