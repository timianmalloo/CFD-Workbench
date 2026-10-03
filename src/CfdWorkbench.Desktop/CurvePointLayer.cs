using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>The brushes of the M1.2b point glyphs (§11.2): outline, selection, the viewport behind a hollow glyph, and the polygon.</summary>
public sealed record PointGlyphBrushes(IBrush Foil, IBrush Station, IBrush Background, IBrush Mute);

/// <summary>
/// The point-editing surface of a curve through one axis mapping (span, ordinate) ↔ screen (M1.2b2 §6.1 Extract Class,
/// seam SR-2): the M1.2b glyphs and dashed control polygon, the 14 px hit test, arrow keys along screen axes, the focus
/// and hover rings, and the point automation peers. The Plan, the elevation lanes and the Front band each supply their
/// own mapping; nothing here knows which view it is in or what the ordinate's unit is.
/// </summary>
public sealed class CurvePointLayer(Func<double, double, Point> project, Func<Point, (double Span, double Ordinate)> unproject)
{
    public const double HitRadius = 14;

    public Point ToScreen(double span, double ordinate) => project(span, ordinate);

    public Point ToScreen(PointView point) => project(point.SpanMeters, point.Ordinate);

    public (double Span, double Ordinate) FromScreen(Point position) => unproject(position);

    /// <summary>The screen step of an arrow key: right, left, down, up; zero for any other key.</summary>
    public static Vector ScreenDirection(Key key) => key switch
    {
        Key.Right => new Vector(1, 0), Key.Left => new Vector(-1, 0),
        Key.Down => new Vector(0, 1), Key.Up => new Vector(0, -1), _ => default
    };

    /// <summary>
    /// The model step (span, ordinate signs) whose screen image goes the arrow's way, so arrows follow screen axes in every
    /// view: on the Plan → is +span and ↓ is aft; on the Front (starboard on the viewer's left) → goes toward the root.
    /// </summary>
    public (int Span, int Ordinate) KeyboardDirection(Key key)
    {
        var screen = ScreenDirection(key);
        if (screen == default) return (0, 0);
        var origin = ToScreen(0, 0);
        double alongSpan = Along(screen, ToScreen(1, 0) - origin), alongOrdinate = Along(screen, ToScreen(0, 1) - origin);
        return Math.Abs(alongSpan) >= Math.Abs(alongOrdinate) ? (Math.Sign(alongSpan), 0) : (0, Math.Sign(alongOrdinate));
    }

    private static double Along(Vector screen, Vector axis)
    {
        double length = axis.Length;
        return length == 0 || !double.IsFinite(length) ? 0 : (screen.X * axis.X + screen.Y * axis.Y) / length;
    }

    /// <summary>The nearest target within <see cref="HitRadius"/>; on a tie a selected point wins.</summary>
    public PointView? HitTest(IEnumerable<PointView> targets, Point position, Selection selection)
    {
        PointView? nearest = null;
        double distance = HitRadius * HitRadius;
        foreach (var candidate in targets)
        {
            var centre = ToScreen(candidate);
            double squared = Math.Pow(position.X - centre.X, 2) + Math.Pow(position.Y - centre.Y, 2);
            if (squared > distance) continue;
            if (squared == distance && selection is Selection.Points selected &&
                !selected.Items.Any(item => item.Curve == candidate.Curve && item.VertexId == candidate.Id)) continue;
            nearest = candidate;
            distance = squared;
        }
        return nearest;
    }

    /// <summary>The evaluated curve as a polyline; <paramref name="spanSign"/> −1 draws the port half.</summary>
    public void DrawCurve(DrawingContext context, IReadOnlyList<PlanSample> samples, IPen pen, double spanSign = 1)
    {
        for (int index = 1; index < samples.Count; index++)
        {
            var before = samples[index - 1];
            var after = samples[index];
            context.DrawLine(pen, ToScreen(before.SpanMeters * spanSign, before.Ordinate), ToScreen(after.SpanMeters * spanSign, after.Ordinate));
        }
    }

    /// <summary>The dashed control polygon, then each point's glyph (M1.2b §11.2).</summary>
    public void DrawPoints(DrawingContext context, CurveView curve, PointGlyphBrushes brushes, Selection selection)
    {
        var polygon = new Pen(brushes.Mute, 1, new DashStyle([3, 3], 0));
        for (int index = 1; index < curve.Points.Count; index++)
            context.DrawLine(polygon, ToScreen(curve.Points[index - 1]), ToScreen(curve.Points[index]));
        foreach (var point in curve.Points)
            DrawGlyph(context, point, ToScreen(point), brushes,
                selection is Selection.Points picked && picked.Items.Any(item => item.Curve == point.Curve && item.VertexId == point.Id));
    }

    /// <summary>
    /// One glyph: a filled 11 px control circle (selected: hollow, 2 px station, centre dot), a 14 px end diamond, a 12 px
    /// anchor square, a 9 px handle circle.
    /// </summary>
    public static void DrawGlyph(DrawingContext context, PointView point, Point centre, PointGlyphBrushes brushes, bool selected)
    {
        var (foil, station, background) = (brushes.Foil, brushes.Station, brushes.Background);
        if (point.Role == PointRole.Control)
        {
            context.DrawEllipse(selected ? background : foil, selected ? new Pen(station, 2) : null, centre, 5.5, 5.5);
            if (selected) context.DrawEllipse(station, null, centre, 2, 2);
        }
        else if (point.Role is PointRole.RootEnd or PointRole.TipEnd)
        {
            var diamond = new StreamGeometry();
            using (var path = diamond.Open())
            {
                path.BeginFigure(new Point(centre.X, centre.Y - 7), selected);
                path.LineTo(new Point(centre.X + 7, centre.Y));
                path.LineTo(new Point(centre.X, centre.Y + 7));
                path.LineTo(new Point(centre.X - 7, centre.Y));
                path.EndFigure(true);
            }
            context.DrawGeometry(selected ? station : null, new Pen(selected ? station : foil, 1.5), diamond);
        }
        else if (point.Role == PointRole.Anchor)
            context.DrawRectangle(selected ? station : background, new Pen(selected ? station : foil, 1.5),
                new Rect(centre.X - 6, centre.Y - 6, 12, 12));
        else
            context.DrawEllipse(selected ? station : background, new Pen(selected ? station : foil, 1), centre, 4.5, 4.5);
    }

    /// <summary>The 10 px hover ring.</summary>
    public void DrawHoverRing(DrawingContext context, PointView point, IBrush brush) =>
        context.DrawEllipse(null, new Pen(brush, 1.5), ToScreen(point), 10, 10);

    /// <summary>The 13 px, 3 px focus ring.</summary>
    public void DrawFocusRing(DrawingContext context, PointView point, IBrush brush) =>
        context.DrawEllipse(null, new Pen(brush, 3), ToScreen(point), 13, 13);

    /// <summary>
    /// A point's automation peer (M1.2b D-1): a Button with a 28 px box around its glyph, its name, and Invoke for its
    /// value request. The name and centre are read when asked, so a peer never reports a stale position.
    /// </summary>
    public static AutomationPeer Peer(Control owner, Func<string> name, Func<Point> centre, Action invoke) =>
        new PointPeer(owner, name, centre, invoke);

    private sealed class PointPeer(Control target, Func<string> name, Func<Point> centre, Action invoke)
        : ControlAutomationPeer(target), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
        protected override string GetNameCore() => name();
        protected override Rect GetBoundingRectangleCore()
        {
            if (Owner.GetVisualRoot() is not Visual root) return default;
            var screen = centre();
            var topLeft = Owner.TranslatePoint(new Point(screen.X - HitRadius, screen.Y - HitRadius), root);
            return topLeft is { } translated ? new Rect(translated, new Size(2 * HitRadius, 2 * HitRadius)) : default;
        }
        public void Invoke() => invoke();
    }
}
