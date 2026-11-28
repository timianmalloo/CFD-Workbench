using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>
/// Lightweight orthographic Front (looking aft, <c>(Y, Z)</c>) and Starboard (looking inboard, <c>(X, Z)</c>)
/// elevation canvas for Tab 3 (<c>Plan &amp; Elevations</c>) of <see cref="NotebookWorkspaceView"/>.
/// Strictly obeys <c>ADR-0010 §Decision 4</c> ("Displays receive geometry, never compute it"):
/// projects placed <see cref="SurfaceView"/> <see cref="Point3"/> coordinates directly with zero synthetic airfoil
/// or sweep geometry math. Uses closed <c>Styles.axaml</c> graphite viewport tokens only (zero new theme brushes,
/// zero <c>Color.Parse</c>, typography always <c>&gt;= 11 px</c> via <see cref="CanvasTypography"/>).
/// </summary>
public sealed class NotebookElevationCanvas : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private SurfaceView? activeSurface;
    private SurfaceView? baselineSurface;
    private CandidateIntentDelta? activeDelta;
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
    public bool RenderedFromPlacedSurface { get; private set; }
    public int ProjectedSectionCount { get; private set; }

    public void Update(
        SurfaceView? surface,
        SurfaceView? compareBaselineSurface,
        CandidateIntentDelta? delta,
        double? hOverC,
        double? froudeH,
        bool isDepthUnset)
    {
        activeSurface = surface;
        baselineSurface = compareBaselineSurface;
        activeDelta = delta;
        hOverCRoot = hOverC;
        frH = froudeH;
        depthUnset = isDepthUnset;

        string name = surface is null || surface.Sections.Count == 0
            ? "Front and starboard elevations (empty)"
            : delta is not null
                ? string.Create(Inv, $"Front elevation (anhedral {delta.AnhedralDeg:0.0} deg, span {delta.SpanMeters * 1000:0.0} mm) and starboard elevation (root chord {delta.RootChordMeters * 1000:0.0} mm, washout {delta.WashoutDeg:0.0} deg)")
                : string.Create(Inv, $"Front and starboard orthographic elevations ({surface.Sections.Count} placed sections)");
        AutomationProperties.SetName(this, name);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        DrawnElevations = 0;
        ShallowFreeSurfaceNoticeDrawn = false;
        RenderedFromPlacedSurface = false;
        ProjectedSectionCount = 0;

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

        if (activeSurface is null || activeSurface.Sections.Count == 0)
        {
            var emptyText = Label("Orthographic Front & Starboard elevations appear once a geometry-checked candidate is placed.", mute);
            context.DrawText(emptyText, new Point(Math.Max(12, (rect.Width - emptyText.Width) * 0.5), Math.Max(12, (rect.Height - emptyText.Height) * 0.5)));
            return;
        }

        double splitX = rect.Width * 0.58;
        context.DrawLine(new Pen(grid, 1), new Point(splitX, 0), new Point(splitX, rect.Height));

        var frontRect = new Rect(8, 8, Math.Max(40, splitX - 16), Math.Max(40, rect.Height - 36));
        var sideRect = new Rect(splitX + 8, 8, Math.Max(40, rect.Width - splitX - 16), Math.Max(40, rect.Height - 36));

        DrawFrontElevation(context, frontRect, activeSurface, baselineSurface, activeDelta, grid, ink, mute, foil, edge);
        DrawStarboardElevation(context, sideRect, activeSurface, baselineSurface, activeDelta, grid, ink, mute, foil, edge);
        DrawnElevations = 2;
        RenderedFromPlacedSurface = true;
        ProjectedSectionCount = activeSurface.Sections.Count;

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
        SurfaceView surface,
        SurfaceView? baseline,
        CandidateIntentDelta? delta,
        IBrush grid,
        IBrush ink,
        IBrush mute,
        IBrush foil,
        IBrush edge)
    {
        string heading = delta is not null
            ? string.Create(Inv, $"Front (looking aft) · anhedral {delta.AnhedralDeg:0.0} deg")
            : "Front (looking aft)";
        context.DrawText(Label(heading, ink), new Point(area.Left + 4, area.Top + 2));

        double maxAbsY = Math.Max(Math.Abs(surface.MinimumY), Math.Abs(surface.MaximumY));
        double minZ = surface.MinimumZ;
        double maxZ = surface.MaximumZ;
        if (baseline is not null && baseline.Sections.Count > 0)
        {
            maxAbsY = Math.Max(maxAbsY, Math.Max(Math.Abs(baseline.MinimumY), Math.Abs(baseline.MaximumY)));
            minZ = Math.Min(minZ, baseline.MinimumZ);
            maxZ = Math.Max(maxZ, baseline.MaximumZ);
        }

        double spanY = Math.Max(0.1, 2.0 * maxAbsY);
        double spanZ = Math.Max(0.02, maxZ - minZ);
        double midZ = 0.5 * (minZ + maxZ);
        double usableW = Math.Max(20, area.Width - 24);
        double usableH = Math.Max(20, area.Height - 30);
        double scale = Math.Min(usableW / spanY, usableH / Math.Max(spanZ * 1.4, spanY * 0.18));

        double cx = area.Center.X;
        double cy = area.Top + 22 + usableH * 0.5 + midZ * scale;

        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(area.Left + 8, cy), new Point(area.Right - 8, cy));
        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(cx, area.Top + 20), new Point(cx, area.Bottom - 6));

        if (baseline is not null && baseline.Sections.Count > 0)
        {
            var ghost = ProjectFrontSilhouette(baseline, cx, cy, scale);
            if (ghost is not null)
                context.DrawGeometry(null, new Pen(mute, 1.25, new DashStyle([4, 4], 0)), ghost);
        }

        var geo = ProjectFrontSilhouette(surface, cx, cy, scale);
        if (geo is not null)
        {
            IBrush fill = foil is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, 0.30) : foil;
            context.DrawGeometry(fill, new Pen(edge, 1.8), geo);
        }
    }

    private static StreamGeometry? ProjectFrontSilhouette(SurfaceView surface, double cx, double cy, double scale)
    {
        int count = surface.Sections.Count;
        if (count == 0) return null;

        var stbdUpper = new List<Point>(count);
        var stbdLower = new List<Point>(count);
        var portUpper = new List<Point>(count);
        var portLower = new List<Point>(count);

        for (int i = 0; i < count; i++)
        {
            var sec = surface.Sections[i];
            if (sec.Upper.Count == 0 || sec.Lower.Count == 0) continue;

            Point3 hi = sec.Upper[0];
            for (int k = 1; k < sec.Upper.Count; k++)
                if (sec.Upper[k].Z > hi.Z) hi = sec.Upper[k];

            Point3 lo = sec.Lower[0];
            for (int k = 1; k < sec.Lower.Count; k++)
                if (sec.Lower[k].Z < lo.Z) lo = sec.Lower[k];

            stbdUpper.Add(new Point(cx + hi.Y * scale, cy - hi.Z * scale));
            stbdLower.Add(new Point(cx + lo.Y * scale, cy - lo.Z * scale));

            Point3 hiPort = hi.Port();
            Point3 loPort = lo.Port();
            portUpper.Add(new Point(cx + hiPort.Y * scale, cy - hiPort.Z * scale));
            portLower.Add(new Point(cx + loPort.Y * scale, cy - loPort.Z * scale));
        }

        if (stbdUpper.Count == 0) return null;

        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        // Port tip upper -> root upper -> starboard tip upper -> starboard tip lower -> root lower -> port tip lower.
        ctx.BeginFigure(portUpper[^1], true);
        for (int i = portUpper.Count - 2; i >= 0; i--) ctx.LineTo(portUpper[i]);
        for (int i = 1; i < stbdUpper.Count; i++) ctx.LineTo(stbdUpper[i]);
        for (int i = stbdLower.Count - 1; i >= 0; i--) ctx.LineTo(stbdLower[i]);
        for (int i = 1; i < portLower.Count; i++) ctx.LineTo(portLower[i]);
        ctx.EndFigure(true);
        return geo;
    }

    private void DrawStarboardElevation(
        DrawingContext context,
        Rect area,
        SurfaceView surface,
        SurfaceView? baseline,
        CandidateIntentDelta? delta,
        IBrush grid,
        IBrush ink,
        IBrush mute,
        IBrush foil,
        IBrush edge)
    {
        string heading = delta is not null
            ? string.Create(Inv, $"Starboard · root {delta.RootChordMeters * 1000:0.0} mm · washout {delta.WashoutDeg:0.0} deg")
            : "Starboard (looking inboard)";
        context.DrawText(Label(heading, ink), new Point(area.Left + 4, area.Top + 2));

        double minX = surface.MinimumX;
        double maxX = surface.MaximumX;
        double minZ = surface.MinimumZ;
        double maxZ = surface.MaximumZ;
        if (baseline is not null && baseline.Sections.Count > 0)
        {
            minX = Math.Min(minX, baseline.MinimumX);
            maxX = Math.Max(maxX, baseline.MaximumX);
            minZ = Math.Min(minZ, baseline.MinimumZ);
            maxZ = Math.Max(maxZ, baseline.MaximumZ);
        }

        double spanX = Math.Max(0.02, maxX - minX);
        double spanZ = Math.Max(0.01, maxZ - minZ);
        double midX = 0.5 * (minX + maxX);
        double midZ = 0.5 * (minZ + maxZ);
        double usableW = Math.Max(20, area.Width - 28);
        double usableH = Math.Max(20, area.Height - 30);
        double scale = Math.Min(usableW / spanX, usableH / Math.Max(spanZ * 1.4, spanX * 0.22));

        double cx = area.Center.X;
        double cy = area.Top + 22 + usableH * 0.5;

        double axisY = cy + midZ * scale;
        context.DrawLine(new Pen(grid, 1, new DashStyle([3, 3], 0)), new Point(area.Left + 8, axisY), new Point(area.Right - 8, axisY));

        if (baseline is not null && baseline.Sections.Count > 0)
        {
            var ghost = ProjectSectionXZ(baseline.Sections[0], cx, cy, midX, midZ, scale);
            if (ghost is not null)
                context.DrawGeometry(null, new Pen(mute, 1.25, new DashStyle([4, 4], 0)), ghost);
        }

        // Root placed section (filled) + Tip placed section (dashed station accent) directly from SurfaceView (X, Z).
        var rootGeo = ProjectSectionXZ(surface.Sections[0], cx, cy, midX, midZ, scale);
        if (rootGeo is not null)
        {
            IBrush fill = foil is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, 0.30) : foil;
            context.DrawGeometry(fill, new Pen(edge, 1.8), rootGeo);
        }

        if (surface.Sections.Count > 1)
        {
            var tipGeo = ProjectSectionXZ(surface.Sections[^1], cx, cy, midX, midZ, scale);
            if (tipGeo is not null)
                context.DrawGeometry(null, new Pen(ResolveBrush("StationBrush", Brushes.Cyan), 1.4, new DashStyle([3, 2], 0)), tipGeo);
        }
    }

    private static StreamGeometry? ProjectSectionXZ(PlacedSection section, double cx, double cy, double midX, double midZ, double scale)
    {
        if (section.Upper.Count < 2 || section.Lower.Count < 2) return null;

        Point Map(Point3 p) => new(cx + (p.X - midX) * scale, cy - (p.Z - midZ) * scale);

        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        ctx.BeginFigure(Map(section.Upper[0]), true);
        for (int i = 1; i < section.Upper.Count; i++)
            ctx.LineTo(Map(section.Upper[i]));
        for (int i = section.Lower.Count - 1; i >= 0; i--)
            ctx.LineTo(Map(section.Lower[i]));
        ctx.EndFigure(true);
        return geo;
    }

    private IBrush ResolveBrush(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    private FormattedText Label(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, CanvasTypography.ResolveTypeface(this), CanvasTypography.ResolveFontSize(this), brush);
}
