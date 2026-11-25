using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis.Results;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Results;

namespace CfdWorkbench.Desktop.Results;

/// <summary>
/// Surface Cp/Cf scalar field + isoline canvas for Area 6 ResultsWorkspaceView (docs/design/results-and-candidates.md §7.3).
/// Honors ADR-0010 §Decision (4): receives placed starboard nodes from <c>Placement.Surface</c> and applies only a
/// projection camera and <see cref="Point3.Port"/> symmetry (x, y, z) -&gt; (x, -y, z).
/// Resolves every brush exclusively from existing theme/root resource keys (ZERO Color.Parse calls) and renders all text
/// using <c>PropFontFamily</c> / <c>PropFontSize</c> (&gt;= 11 px; UI-CS-TOKEN 0 findings).
/// </summary>
public sealed class ResultsFieldCanvas : Control
{
    private static readonly string[] VikBrushKeys =
    [
        "VikNeg2Brush",
        "VikNeg1Brush",
        "VikZeroBrush",
        "VikPos1Brush",
        "VikPos2Brush"
    ];

    private static readonly string[] BatlowBrushKeys =
    [
        "Batlow0Brush",
        "Batlow1Brush",
        "Batlow2Brush",
        "Batlow3Brush",
        "Batlow4Brush"
    ];

    private SurfaceFieldProjection? projection;
    private DifferenceFloodResult? differenceOverlay;
    private bool showSeparationOverlay;
    private bool showVortexCoreOverlay;
    private string? clearedFailureReason;
    private double? lockedMin;
    private double? lockedMax;

    public ResultsFieldCanvas()
    {
        ClipToBounds = true;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public int RenderedNodeCount { get; private set; }
    public int RenderedIsolineCount { get; private set; }
    public bool IsClearedForFailedSample => clearedFailureReason is not null;
    public string ActiveVariable => differenceOverlay is { Available: true } ? differenceOverlay.Variable : projection?.Variable ?? "";
    public double LastRenderedFontSize { get; private set; } = CanvasTypography.MinimumFontSize;
    public string? LastLegendMaxLabel { get; private set; }
    public string? LastLegendMidLabel { get; private set; }
    public string? LastLegendMinLabel { get; private set; }
    public bool LastRenderedZeroTick { get; private set; }

    /// <summary>
    /// Maps a scalar value to normalized colormap position [0, 1] and discrete palette bin.
    /// Diverging <c>vik</c> fields normalize symmetrically around physical 0.0 so 0.0 always lands on <c>VikZeroBrush</c> (t = 0.5).
    /// </summary>
    public static (double NormalizedT, int BinIndex) MapScalarToColormap(
        double value,
        double valMin,
        double valMax,
        bool isDiverging,
        int paletteLength = 5)
    {
        int count = Math.Max(1, paletteLength);
        double t;
        if (!double.IsFinite(value))
        {
            t = 0.5;
        }
        else if (isDiverging)
        {
            double maxAbs = Math.Max(Math.Abs(valMin), Math.Abs(valMax));
            t = double.IsFinite(maxAbs) && maxAbs > 0.0
                ? Math.Clamp(0.5 + 0.5 * (value / maxAbs), 0.0, 1.0)
                : 0.5;
        }
        else
        {
            double span = valMax - valMin;
            t = double.IsFinite(span) && span > 1e-12
                ? Math.Clamp((value - valMin) / span, 0.0, 1.0)
                : 0.5;
        }
        int bin = Math.Clamp((int)(t * count), 0, count - 1);
        return (t, bin);
    }

    /// <summary>
    /// Adaptive legend label formatter (R2-30): switches to scientific notation (<c>0.00e+0</c>) when
    /// <paramref name="scaleMagnitude"/> &lt; 0.05 so small <c>Cf</c> values (e.g., 0.003) never collapse to "0.00".
    /// </summary>
    public static string FormatLegendValue(double value, double scaleMagnitude)
    {
        if (!double.IsFinite(value)) return "—";
        if (Math.Abs(value) < 1e-12) return "0";
        double mag = double.IsFinite(scaleMagnitude) ? Math.Abs(scaleMagnitude) : Math.Abs(value);
        if (mag < 0.05) return value.ToString("0.00e+0", CultureInfo.InvariantCulture);
        if (mag < 1.0) return value.ToString("0.###", CultureInfo.InvariantCulture);
        return value.ToString("G3", CultureInfo.InvariantCulture);
    }

    public void UpdateField(
        SurfaceFieldProjection? surfaceProjection,
        bool showSeparation,
        bool showVortexCore,
        double? rangeMin = null,
        double? rangeMax = null)
    {
        projection = surfaceProjection;
        differenceOverlay = null;
        showSeparationOverlay = showSeparation;
        showVortexCoreOverlay = showVortexCore;
        clearedFailureReason = null;
        lockedMin = rangeMin;
        lockedMax = rangeMax;
        RecomputeCounts();
        InvalidateVisual();
    }

    public void ShowDifference(SurfaceFieldProjection? baseProjection, DifferenceFloodResult diff)
    {
        projection = baseProjection;
        differenceOverlay = diff;
        clearedFailureReason = diff.Available ? null : diff.UnavailableReason;
        lockedMin = diff.ScaleMin;
        lockedMax = diff.ScaleMax;
        RecomputeCounts();
        InvalidateVisual();
    }

    /// <summary>Failed-sample pause &amp; clear invariant: clears displayed fields immediately before showing the failure banner.</summary>
    public void ClearForFailedSample(string failureReason)
    {
        projection = null;
        differenceOverlay = null;
        showSeparationOverlay = false;
        showVortexCoreOverlay = false;
        clearedFailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Sample failed" : failureReason;
        RenderedNodeCount = 0;
        RenderedIsolineCount = 0;
        InvalidateVisual();
    }

    private void RecomputeCounts()
    {
        if (clearedFailureReason is not null || projection is null || projection.Nodes.Count == 0)
        {
            RenderedNodeCount = 0;
            RenderedIsolineCount = 0;
            return;
        }
        // Starboard + symmetric port nodes via Point3.Port().
        RenderedNodeCount = projection.Nodes.Count * 2;
        RenderedIsolineCount = 5;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 4 || h <= 4) return;

        var bg = ResolveBrush("ViewportBrush", Brushes.Black);
        var gridBrush = ResolveBrush("ViewportGridBrush", Brushes.Gray);
        var ink = ResolveBrush("ViewportInkBrush", Brushes.White);
        var danger = ResolveBrush("PlanDangerBrush", ink);
        var warning = ResolveBrush("PlanWarningBrush", ink);
        var line = ResolveBrush("LineBrush", gridBrush);

        double fontSize = Math.Max(CanvasTypography.MinimumFontSize, CanvasTypography.ResolveFontSize(this));
        LastRenderedFontSize = fontSize;
        var typeface = CanvasTypography.ResolveTypeface(this);
        var boldTypeface = CanvasTypography.ResolveTypeface(this, FontWeight.SemiBold);

        context.FillRectangle(bg, new Rect(0, 0, w, h));

        if (clearedFailureReason is not null)
        {
            DrawBanner(context, clearedFailureReason, warning, boldTypeface, fontSize, w, h);
            return;
        }

        if (projection is null || projection.Nodes.Count == 0)
        {
            DrawBanner(context, "No surface field projected for this sample.", ink, typeface, fontSize, w, h);
            return;
        }

        // Compute world bounds across starboard and Point3.Port() mirror (ADR-0010 §Decision (4)).
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
        double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
        foreach (var node in projection.Nodes)
        {
            var stbd = new Point3(node.X, node.Y, node.Z);
            var port = stbd.Port();
            minX = Math.Min(minX, Math.Min(stbd.X, port.X));
            maxX = Math.Max(maxX, Math.Max(stbd.X, port.X));
            minY = Math.Min(minY, Math.Min(stbd.Y, port.Y));
            maxY = Math.Max(maxY, Math.Max(stbd.Y, port.Y));
        }
        if (!double.IsFinite(minX) || !double.IsFinite(maxX) || maxX - minX < 1e-9)
        {
            minX = 0; maxX = 1;
        }
        if (!double.IsFinite(minY) || !double.IsFinite(maxY) || maxY - minY < 1e-9)
        {
            minY = -0.5; maxY = 0.5;
        }

        double padLeft = 18;
        double padRight = 92;
        double padTop = 30;
        double padBottom = 26;
        double plotW = Math.Max(20, w - padLeft - padRight);
        double plotH = Math.Max(20, h - padTop - padBottom);
        double spanY = maxY - minY;
        double spanX = maxX - minX;
        double scale = Math.Min(plotW / spanY, plotH / spanX);
        double cx = padLeft + plotW * 0.5;
        double cy = padTop + plotH * 0.5;
        double midY = 0.5 * (minY + maxY);
        double midX = 0.5 * (minX + maxX);

        Point Project(Point3 p) => new(cx + (p.Y - midY) * scale, cy + (p.X - midX) * scale);

        // Subtle centreline & reference grid.
        var gridPen = new Pen(gridBrush, 1);
        context.DrawLine(gridPen, new Point(cx, padTop), new Point(cx, padTop + plotH));
        context.DrawRectangle(null, new Pen(line, 1), new Rect(padLeft, padTop, plotW, plotH));

        bool isDiverging = differenceOverlay is { Available: true } || string.Equals(projection.Variable, "Cp", StringComparison.Ordinal);
        string[] paletteKeys = isDiverging ? VikBrushKeys : BatlowBrushKeys;
        IBrush[] palette = paletteKeys.Select(k => ResolveBrush(k, ink)).ToArray();

        double valMin = lockedMin ?? projection.MinValue ?? -1.0;
        double valMax = lockedMax ?? projection.MaxValue ?? 1.0;
        if (Math.Abs(valMax - valMin) < 1e-9) { valMin -= 0.5; valMax += 0.5; }
        double maxAbs = Math.Max(Math.Abs(valMin), Math.Abs(valMax));
        if (!double.IsFinite(maxAbs) || maxAbs < 1e-12) maxAbs = 1.0;

        // Render surface scalar field over starboard & port halves + isoline markers.
        var isoPen = new Pen(ink, 1);
        var sepPen = new Pen(danger, 2);
        var vortexPen = new Pen(warning, 1.5);
        double dotRadius = Math.Clamp(scale * 0.012, 2.0, 5.0);

        for (int i = 0; i < projection.Nodes.Count; i++)
        {
            var node = projection.Nodes[i];
            double? value = differenceOverlay is { Available: true } && i < differenceOverlay.DeltaValues.Count
                ? differenceOverlay.DeltaValues[i]
                : node.Value;

            var (t, bin) = value is { } v
                ? MapScalarToColormap(v, valMin, valMax, isDiverging, palette.Length)
                : (0.5, palette.Length / 2);
            var fill = palette[bin];

            var stbdPt = Project(new Point3(node.X, node.Y, node.Z));
            var portPt = Project(new Point3(node.X, node.Y, node.Z).Port());

            context.DrawEllipse(fill, null, stbdPt, dotRadius, dotRadius);
            context.DrawEllipse(fill, null, portPt, dotRadius, dotRadius);

            // Isoline contour ticks near quantized levels (0.2, 0.4, 0.6, 0.8).
            if (Math.Abs((t * 5.0) - Math.Round(t * 5.0)) < 0.08)
            {
                context.DrawEllipse(null, isoPen, stbdPt, dotRadius + 1.0, dotRadius + 1.0);
                context.DrawEllipse(null, isoPen, portPt, dotRadius + 1.0, dotRadius + 1.0);
            }

            if (showSeparationOverlay && t < 0.2)
            {
                context.DrawLine(sepPen, new Point(stbdPt.X - 3, stbdPt.Y), new Point(stbdPt.X + 3, stbdPt.Y));
                context.DrawLine(sepPen, new Point(portPt.X - 3, portPt.Y), new Point(portPt.X + 3, portPt.Y));
            }

            if (showVortexCoreOverlay && node.Eta >= 0.85)
            {
                context.DrawEllipse(null, vortexPen, stbdPt, dotRadius + 2.5, dotRadius + 2.5);
                context.DrawEllipse(null, vortexPen, portPt, dotRadius + 2.5, dotRadius + 2.5);
            }
        }

        // Header title & colormap legend bar on right edge.
        string header = string.Create(CultureInfo.InvariantCulture,
            $"{ActiveVariable} ({projection.Unit}) · {(isDiverging ? ProvenanceStrip.VikCrameri : ProvenanceStrip.BatlowCrameri)}");
        DrawText(context, header, boldTypeface, fontSize, ink, new Point(padLeft, 6));

        double barX = w - padRight + 12;
        double barW = 14;
        double segH = plotH / palette.Length;
        for (int i = 0; i < palette.Length; i++)
        {
            int idx = palette.Length - 1 - i;
            context.FillRectangle(palette[idx], new Rect(barX, padTop + i * segH, barW, segH + 0.5));
        }
        context.DrawRectangle(null, new Pen(line, 1), new Rect(barX, padTop, barW, plotH));

        double legendMax = isDiverging ? maxAbs : valMax;
        double legendMin = isDiverging ? -maxAbs : valMin;
        string maxLabel = FormatLegendValue(legendMax, maxAbs);
        string minLabel = FormatLegendValue(legendMin, maxAbs);
        LastLegendMaxLabel = maxLabel;
        LastLegendMinLabel = minLabel;

        DrawText(context, maxLabel, typeface, fontSize, ink, new Point(barX + barW + 4, padTop));
        if (isDiverging)
        {
            double zeroY = padTop + plotH * 0.5;
            context.DrawLine(new Pen(ink, 1.5), new Point(barX - 3, zeroY), new Point(barX + barW + 3, zeroY));
            DrawText(context, "0", boldTypeface, fontSize, ink, new Point(barX + barW + 5, zeroY - fontSize * 0.6));
            LastLegendMidLabel = "0";
            LastRenderedZeroTick = true;
        }
        else
        {
            LastLegendMidLabel = null;
            LastRenderedZeroTick = false;
        }
        DrawText(context, minLabel, typeface, fontSize, ink, new Point(barX + barW + 4, Math.Max(padTop + 14, padTop + plotH - fontSize - 2)));
        DrawText(context, "Port (-y) · Starboard (+y)", typeface, fontSize, ink, new Point(padLeft, h - fontSize - 6));
    }

    private static void DrawBanner(DrawingContext ctx, string text, IBrush brush, Typeface typeface, double fontSize, double w, double h)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush)
        {
            MaxTextWidth = Math.Max(40, w - 32)
        };
        ctx.DrawText(ft, new Point(Math.Max(16, (w - ft.Width) * 0.5), Math.Max(16, (h - ft.Height) * 0.5)));
    }

    private static void DrawText(DrawingContext ctx, string text, Typeface typeface, double fontSize, IBrush brush, Point origin)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
        ctx.DrawText(ft, origin);
    }

    private IBrush ResolveBrush(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var val) && val is IBrush b ? b : fallback;
}
