using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Setup;

/// <summary>
/// Live Graphite planform preview control for Area 1 Setup (docs/design/setup-seeding.md §5.1).
/// Draws the candidate wing (starboard + port mirror), dashed 4-CV cubic Bézier control polygon,
/// and ghosted prior revision outline when re-seeding. Uses theme-aware resource brushes only.
/// </summary>
public sealed class PlanformPreviewControl : Control
{
    private RecipeRecord? candidate;
    private WingEstimates? prior;

    public void Update(RecipeRecord? nextCandidate, WingEstimates? nextPrior)
    {
        candidate = nextCandidate;
        prior = nextPrior;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size);
        if (rect.Width <= 4 || rect.Height <= 4) return;

        IBrush bg = ResolveBrush("ViewportBrush", Brushes.Black);
        IBrush gridBrush = ResolveBrush("ViewportGridBrush", Brushes.Gray);
        IBrush foilBrush = ResolveBrush("FoilBrush", Brushes.Teal);
        IBrush muteBrush = ResolveBrush("PlanMuteBrush", Brushes.LightGray);
        IBrush stationBrush = ResolveBrush("StationBrush", Brushes.Cyan);

        context.FillRectangle(bg, rect);

        // Subtle viewport grid lines.
        var gridPen = new Pen(gridBrush, 1.0);
        double cx = rect.Width * 0.5;
        double cy = rect.Height * 0.5;
        context.DrawLine(gridPen, new Point(cx, 0), new Point(cx, rect.Height));
        context.DrawLine(gridPen, new Point(0, cy), new Point(rect.Width, cy));

        if (candidate is null) return;

        double maxSpan = Math.Max(candidate.SpanMeters, prior?.SpanMeters ?? 0.0);
        double maxChord = Math.Max(candidate.RootChordMeters, prior?.RootChordMeters ?? 0.0);
        if (maxSpan <= 0 || maxChord <= 0) return;

        double padX = 24.0;
        double padY = 20.0;
        double scale = Math.Min((rect.Width - 2 * padX) / maxSpan, (rect.Height - 2 * padY) / Math.Max(maxChord * 1.4, 0.08));

        Point Map(double ySpan, double xChord) =>
            new(cx + ySpan * scale, cy + (xChord - maxChord * 0.45) * scale);

        // Ghosted prior revision outline when re-seeding.
        if (prior is not null && prior.SpanMeters > 0 && prior.RootChordMeters > 0)
        {
            double priorLam = Math.Clamp(prior.TipChordMeters / prior.RootChordMeters, 0.02, 1.0);
            var ghostGeo = BuildPlanformGeometry(prior.SpanMeters, prior.RootChordMeters, priorLam, Map);
            var ghostPen = new Pen(muteBrush, 1.25, new DashStyle([4, 4], 0));
            context.DrawGeometry(null, ghostPen, ghostGeo);
        }

        // Candidate seeded wing (solid evaluated 4-CV cubic Bézier rails, starboard + port mirror).
        var wingGeo = BuildPlanformGeometry(candidate.SpanMeters, candidate.RootChordMeters, candidate.Taper, Map);
        var fillBrush = foilBrush is ISolidColorBrush solid
            ? new SolidColorBrush(solid.Color, 0.25)
            : foilBrush;
        context.DrawGeometry(fillBrush, new Pen(foilBrush, 2.0), wingGeo);

        // Dashed 4-CV control polygon on starboard half.
        double half = candidate.SpanMeters * 0.5;
        double cRoot = candidate.RootChordMeters;
        double lam = candidate.Taper;
        double[] g = [1.0, 1.0, 1.0, lam];
        double[] etas = [0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0];
        var cvPen = new Pen(stationBrush, 1.0, new DashStyle([3, 3], 0));
        var lePts = new Point[4];
        var tePts = new Point[4];
        for (int i = 0; i < 4; i++)
        {
            double c = cRoot * g[i];
            double le = cRoot * (1.0 - g[i]) * 0.25;
            lePts[i] = Map(half * etas[i], le);
            tePts[i] = Map(half * etas[i], le + c);
        }
        for (int i = 0; i < 3; i++)
        {
            context.DrawLine(cvPen, lePts[i], lePts[i + 1]);
            context.DrawLine(cvPen, tePts[i], tePts[i + 1]);
        }
    }

    private static StreamGeometry BuildPlanformGeometry(double span, double cRoot, double lam, Func<double, double, Point> map)
    {
        double half = span * 0.5;
        const int n = 24;
        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        // Leading edge from port tip (-half) -> root (0) -> starboard tip (+half)
        Point Pt(double eta, bool port, bool te)
        {
            double g = 1.0 - (1.0 - lam) * eta * eta * eta;
            double c = cRoot * g;
            double le = cRoot * (1.0 - g) * 0.25;
            return map((port ? -half : half) * eta, te ? le + c : le);
        }

        ctx.BeginFigure(Pt(1.0, port: true, te: false), isFilled: true);
        for (int i = n - 1; i >= 0; i--) ctx.LineTo(Pt(i / (double)n, port: true, te: false));
        for (int i = 1; i <= n; i++) ctx.LineTo(Pt(i / (double)n, port: false, te: false));
        for (int i = n; i >= 0; i--) ctx.LineTo(Pt(i / (double)n, port: false, te: true));
        for (int i = 1; i <= n; i++) ctx.LineTo(Pt(i / (double)n, port: true, te: true));
        ctx.EndFigure(isClosed: true);
        return geo;
    }

    private IBrush ResolveBrush(string key, IBrush fallback)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out object? res) && res is IBrush brush)
            return brush;
        return fallback;
    }
}
