using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Section document's profile on the dark viewport (approved mockup, DX state 5): the closed outline, each panel coloured
/// by its own Cp on the diverging vik ramp pinned at Cp 0, the Cp_min marker, and the caption, estimator and legend plates.
/// The panel colours and counts of the last render are kept for the checks (<see cref="Segments"/>).
/// </summary>
public sealed class SectionProfileView : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly string[] StopKeys = ["VikNeg2Color", "VikNeg1Color", "VikZeroColor", "VikPos1Color", "VikPos2Color"];
    private static readonly Color[] StopFallback = [Color.Parse("#001261"), Color.Parse("#2d7ba5"), Color.Parse("#ebe6e2"), Color.Parse("#c37243"), Color.Parse("#590008")];
    private SectionProfile? model;
    private Color[] stops = StopFallback;

    public SectionProfile? Model
    {
        get => model;
        set
        {
            model = value;
            AutomationProperties.SetName(this, value is null ? "Section profile" :
                value.Caption + ", pressure coefficient on the profile; " + (value.TipNotJudged ? "Cp_min " + Labels.TipNotJudged :
                "Cp_min " + Num(value.CpMinPanel.Cp) + " at x/c " + value.CpMinPanel.X.ToString("0.000", Inv) + " on the " + value.Side + " surface") +
                (value.Forces is { } f ? ". " + Labels.LiftLabel(f.LiftPerSpan, f.Units) + ", " + (f.Anchor == ForceAnchor.CentreOfPressure ? Labels.AnchorCp(f.XcpOverC!.Value) : Labels.AnchorQuarter) +
                    ", " + Labels.FreeStream(f.AlphaGeoDeg) : ""));
            // the plate and the key under it need the room the mockup gives them; the Cp-only view keeps its height
            Height = value?.Forces is null ? 240 : ViewportHeightWithVectors + KeyHeight;
            InvalidateVisual();
        }
    }

    /// <summary>One entry per drawn outline segment: its Cp and the colour it was drawn in.</summary>
    public IReadOnlyList<(double Cp, Color Colour)> Segments { get; private set; } = [];

    /// <summary>The ramp's half-extent of the last render: max(|Cp low|, Cp high); Cp 0 sits at the ramp's centre.</summary>
    public double Range { get; private set; }

    /// <summary>Where a Cp falls on the ramp, 0 to 1, 0.5 at Cp 0.</summary>
    public static double RampPosition(double cp, double range) => Math.Clamp(0.5 + cp / (2 * range), 0, 1);

    public Color Vik(double t)
    {
        double p = Math.Clamp(t, 0, 1) * (stops.Length - 1);
        int i = Math.Min(stops.Length - 2, (int)Math.Floor(p));
        double w = p - i;
        Color a = stops[i], b = stops[i + 1];
        byte Mix(byte x, byte y) => (byte)Math.Round(x + (y - x) * w);
        return Color.FromRgb(Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
    }

    private IBrush Resource(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    private static string Num(double value) => value.ToString("0.00", Inv).Replace('-', '−');

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        Segments = [];
        Vectors = [];
        Plates = [];
        placed.Clear();
        CoupleDrawn = false;
        if (model is not { } profile || Bounds.Width < 120 || Bounds.Height < 100) return;
        stops = StopKeys.Select((key, i) => this.TryFindResource(key, ActualThemeVariant, out object? v) && v is Color c ? c : StopFallback[i]).ToArray();
        IBrush viewport = Resource("ViewportBrush", Brushes.Black), grid = Resource("ViewportGridBrush", Brushes.DimGray),
            ink = Resource("ViewportInkBrush", Brushes.White), mute = Resource("PlanMuteBrush", Brushes.LightGray),
            soft = Resource("PlanSoftBrush", Brushes.DarkSlateGray), foil = Resource("FoilBrush", Brushes.CadetBlue);
        double w = Bounds.Width, h = Bounds.Height - (profile.Forces is null ? 0 : KeyHeight);
        context.DrawRectangle(viewport, null, new Rect(0, 0, w, h));
        if (profile.Forces is not null) DrawKey(context, h + 4, w);

        double range = Math.Max(Math.Max(-profile.CpLow, profile.CpHigh), 1e-9);
        // Ruling 144: on a tip station the bar's ends are rounded outward to a fixed step, so they never print Cp_min
        if (profile.TipNotJudged) range = ChartPlot.RoundOut(range, ChartPlot.TipExtentStep, low: false);
        Range = range;
        // With the force vectors the chord stands to the right of the free-stream labels (mockup: 150 of 974 px left, chord 700 px).
        double s = profile.Forces is null ? Math.Max(40, w - 110) : Math.Max(40, 0.72 * w), ox = profile.Forces is null ? (w - s) / 2 : 0.154 * w, oy = h / 2 - 6;
        Point P(double x, double z) => new(ox + x * s, oy - z * s);
        context.DrawLine(new Pen(grid, 1, new DashStyle([4, 4], 0)), P(-0.02, 0), P(1.02, 0));

        var fill = new StreamGeometry();
        using (StreamGeometryContext g = fill.Open())
        {
            g.BeginFigure(P(profile.Outline[0].X, profile.Outline[0].Z), true);
            foreach (PanelCp p in profile.Outline.Skip(1)) g.LineTo(P(p.X, p.Z));
            g.EndFigure(true);
        }
        context.DrawGeometry(new SolidColorBrush(((ISolidColorBrush)foil).Color, 0.35), null, fill);

        var segments = new List<(double Cp, Color Colour)>();
        for (int i = 0; i < profile.Outline.Count; i++)
        {
            PanelCp a = profile.Outline[i], b = profile.Outline[(i + 1) % profile.Outline.Count];
            double cp = (a.Cp + b.Cp) / 2;
            Color colour = Vik(RampPosition(cp, range));
            segments.Add((cp, colour));
            context.DrawLine(new Pen(new SolidColorBrush(colour), 5, lineCap: PenLineCap.Round), P(a.X, a.Z), P(b.X, b.Z));
        }
        Segments = segments;

        // The plates that stand in fixed places are laid out first and the force labels then find free room among them
        // (Ruling 131 repair: no two plates, and no plate and the Cp_min ring, may overlap). They are drawn last, over the arrows,
        // as before: only the layout moves forward.
        deferred = [];
        Point marker = P(profile.CpMinPanel.X, profile.CpMinPanel.Z);
        if (!profile.TipNotJudged) // Ruling 142 (3): a tip station shows no Cp_min ring and no Cp_min plate
        {
            placed.Add(("Cp_min ring", new Rect(marker.X - 8, marker.Y - 8, 16, 16)));
            string markerText = Labels.CpMinMarker(profile.CpMinPanel.Cp, profile.CpMinPanel.X, profile.Side);
            double markerWidth = Text(markerText, ink).Width + 8;
            Plate(context, markerText, new Point(Math.Clamp(marker.X + 12, 4, Math.Max(4, w - markerWidth - 4)), marker.Y + (profile.Side == "lower" ? 30 : -18)), ink, soft);
        }
        // with vectors the axis plate stands bottom left above the cavitation line (mockup), clear of the labels under the chord
        Plate(context, Labels.AxisPlate, profile.Forces is not null ? new Point(8, h - 56) : new Point(ox, oy + 0.1 * s + 20 > h - 40 ? h - 40 : oy + 0.1 * s + 20), mute, soft);

        Rect caption = Plate(context, profile.Caption, new Point(8, 22), ink, soft);
        double tierWidth = Text(profile.Tier, ink).Width + 8;
        bool beside = w - 8 - tierWidth > caption.Right + 8;
        Plate(context, profile.Tier, new Point(beside ? w - 8 - tierWidth + 4 : 8, beside ? 22 : 46), ink, soft);

        // Ruling 131: a tip strip has no force vectors and no CP anchor; the plate says why
        if (profile.ForcesNotJudged is { } notJudged) Plate(context, notJudged, new Point(8, beside ? 46 : 70), ink, soft);

        // Legend plate, bottom right: title, the ramp bar (extent ±range, 0 at the centre), its ends.
        // Ruling 142 (3): the low end of the Cp range is the station's Cp_min, so a tip station's legend carries no range
        string title = profile.TipNotJudged ? Labels.CpLegend : "Cp · vik pinned at 0 · " + Num(profile.CpLow) + " to +" + Num(profile.CpHigh);
        double barWidth = Math.Max(150, Text(title, ink).Width), left = w - 8 - barWidth - 8;
        placed.Add(("legend", new Rect(left - 4, h - 56, barWidth + 16, 50)));
        deferred.Add(() => DrawLegend(context, title, barWidth, left, h, range, ink, mute, soft));

        if (profile.Cavitation is { } line)
        {
            double lineWidth = Text(line, ink).Width + 8;
            Plate(context, line, new Point(8, left - 4 < 8 + lineWidth ? h - 78 : h - 30), ink, soft);
        }
        List<Action> fixedPlates = deferred;
        deferred = null;
        if (profile.Forces is { } forces) DrawForces(context, forces, P, s, ox, oy, w, h, viewport, ink, mute, soft, profile.Side);
        if (!profile.TipNotJudged) context.DrawEllipse(null, new Pen(ink, 2), marker, 7, 7);
        foreach (Action draw in fixedPlates) draw();
        Plates = placed.ToArray();
    }

    private List<Action>? deferred;

    private void DrawLegend(DrawingContext context, string title, double barWidth, double left, double h, double range, IBrush ink, IBrush mute, IBrush soft)
    {
        context.DrawRectangle(soft, null, new Rect(left - 4, h - 56, barWidth + 16, 50), 3);
        context.DrawText(Text(title, ink), new Point(left + 4, h - 54));
        var bar = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
        for (int i = 0; i < stops.Length; i++) bar.GradientStops.Add(new GradientStop(stops[i], i / (double)(stops.Length - 1)));
        context.DrawRectangle(bar, null, new Rect(left + 4, h - 36, barWidth, 8));
        FormattedText lowEnd = Text(Num(-range), mute), zero = Text("0", mute), highEnd = Text(Num(range), mute);
        context.DrawText(lowEnd, new Point(left + 4, h - 24));
        context.DrawText(zero, new Point(left + 4 + (barWidth - zero.Width) / 2, h - 24));
        context.DrawText(highEnd, new Point(left + 4 + barWidth - highEnd.Width, h - 24));
    }

    private static readonly double[] Shifts = [0, 8, -8, 16, -16, 24, -24, 32, -32, 48, -48, 64, -64, 80, -80, 96, -96, 120, -120];

    // The preferred top-left of a plate of this size, else the nearest position above or below it that meets no plate already placed.
    private Point Free(Point at, double width, double height)
    {
        foreach (double dy in Shifts)
        {
            var rect = new Rect(at.X, at.Y + dy - 12, width, height);
            if (!placed.Any(p => p.Bounds.Intersects(rect))) return new Point(at.X, at.Y + dy);
        }
        return at;
    }

    /// <summary>One vector drawn on the last render: its name (V∞, inflow, lift, drag-profile, drag-induced) and its start and end in view pixels.</summary>
    public sealed record DrawnVector(string Name, Point From, Point To);

    /// <summary>The vectors of the last render, for the checks (angles asserted on the drawn geometry, not on the model).</summary>
    public IReadOnlyList<DrawnVector> Vectors { get; private set; } = [];

    /// <summary>Every plate (and the Cp_min ring and the legend) of the last render with its rectangle in view pixels, for the no-overlap check.</summary>
    public IReadOnlyList<(string Name, Rect Bounds)> Plates { get; private set; } = [];
    private readonly List<(string Name, Rect Bounds)> placed = [];

    /// <summary>True when the last render drew the pitching-moment couple (the c/4 case).</summary>
    public bool CoupleDrawn { get; private set; }

    /// <summary>Where the arrows start in the last render, in view pixels.</summary>
    public Point? ForceStart { get; private set; }

    /// <summary>Height of the page-surface strip under the viewport that carries the vector key (COPY-SF16), two rows.</summary>
    public const double KeyHeight = 44, ViewportHeightWithVectors = 420;

    private static Color Mix(Color a, Color b, double t) =>
        Color.FromRgb((byte)Math.Round(a.R + (b.R - a.R) * t), (byte)Math.Round(a.G + (b.G - a.G) * t), (byte)Math.Round(a.B + (b.B - a.B) * t));

    // Lift and Drag on free-stream axes (Rulings 127, 128, 130; approved mockup states A to E). Chord frame: x aft, z up; the screen has y down.
    // V∞ rises at alpha_geo to the chord (the flow meets a nose-up section from below), so the drag arrows climb to the right and the lift
    // arrow leans forward of the chord normal. The mockup draws both mirrored (V∞ falling); the build follows the physics (docs/proof/sfv/captures.md).
    private void DrawForces(DrawingContext context, SectionForces f, Func<double, double, Point> P, double s, double ox, double oy, double w, double h,
        IBrush viewport, IBrush ink, IBrush mute, IBrush soft, string cpSide)
    {
        IBrush warn = Resource("PlanWarningBrush", Brushes.Goldenrod);
        IBrush induced = new SolidColorBrush(Mix(((ISolidColorBrush)warn).Color, ((ISolidColorBrush)ink).Color, 0.45));
        (double dx, double dz) = f.FreeStream;
        (double lx, double lz) = f.LiftDirection;
        var drag = new Vector(dx, -dz);
        var lift = new Vector(lx, -lz);
        Point anchor = P(f.AnchorX, 0);
        ForceStart = anchor;
        var drawn = new List<DrawnVector>();
        double Clamp(double x, double width) => Math.Clamp(x, 4, Math.Max(4, w - width - 4));
        Rect Place(string text, double x, double y, bool right = false)
        {
            double width = Text(text, ink).Width + 8;
            return Plate(context, text, Free(new Point(Clamp(right ? x - width : x, width), y), width, 16), ink, soft);
        }
        Rect Place2(string first, string second, double x, double y, bool right = false)
        {
            double width = Math.Max(Text(first, ink).Width, Text(second, mute).Width) + 8;
            return Plate2(context, first, second, Free(new Point(Clamp(right ? x - width : x, width), y), width, 32), ink, mute, soft);
        }

        // V∞ at α_geo (solid) and the local inflow at α_eff (faint, dashed): they differ by α_i, drawn without exaggeration.
        const double inflowLength = 120;
        double ae = f.AlphaEffDeg / 360 * Math.Tau;
        Point vEnd = new(ox - 36, oy - 100), iEnd = new(ox - 24, oy);
        Point vStart = vEnd - drag * inflowLength, iStart = iEnd - new Vector(Math.Cos(ae), -Math.Sin(ae)) * inflowLength;
        Arrow(context, iStart, iEnd, mute, 1.5, [5, 4], 10);
        Arrow(context, vStart, vEnd, ink, 2, null, 10);
        drawn.Add(new DrawnVector("inflow", iStart, iEnd));
        drawn.Add(new DrawnVector("V∞", vStart, vEnd));

        double lpx = f.LiftChords * s, ppx = f.ProfileChords * s, ipx = f.InducedChords * s;
        Point liftEnd = anchor + lift * lpx, profileEnd = anchor + drag * ppx, inducedEnd = profileEnd + drag * ipx;
        double pmx = anchor.X + drag.X * ppx * 0.5, imx = profileEnd.X + drag.X * ipx * 0.5;
        const double yI = 66, yP = 94, yT = 122;
        var leader = new Pen(mute, 1);
        context.DrawLine(leader, new Point(imx, anchor.Y + 5), new Point(imx, oy + yI));
        if (f.ProfileLow is not null) context.DrawLine(leader, new Point(pmx, anchor.Y + 8), new Point(pmx, oy + yP));
        if (lpx < 40) context.DrawLine(leader, new Point(anchor.X, anchor.Y - 8), new Point(anchor.X, anchor.Y - 62));

        // halos first, so every arrow reads over the Cp colours
        Arrow(context, anchor, liftEnd, viewport, 7, null, 13);
        if (f.ProfileLow is not null) Arrow(context, anchor, profileEnd, viewport, 8, null, 0);
        Arrow(context, profileEnd, inducedEnd, viewport, 8, null, 13);
        if (f.Anchor == ForceAnchor.QuarterChord)
        {
            Couple(context, anchor, f.CouplePerSpan, viewport, 7, 0);
            Couple(context, anchor, f.CouplePerSpan, ink, 3, 11);
            CoupleDrawn = true;
        }
        Arrow(context, anchor, liftEnd, ink, 3, null, 10);
        drawn.Add(new DrawnVector("lift", anchor, liftEnd));
        if (f.ProfileLow is not null)
        {
            Arrow(context, anchor, profileEnd, warn, 4, null, 0);
            drawn.Add(new DrawnVector("drag-profile", anchor, profileEnd));
            // the Ncrit 2-4 band is a cap on the profile segment
            double a0 = f.ProfileLowChords!.Value * s, a1 = f.ProfileHighChords!.Value * s, bw = Math.Max(5, Math.Abs(a1 - a0));
            Point mid = anchor + drag * ((a0 + a1) / 2);
            using (context.PushTransform(Matrix.CreateRotation(Math.Atan2(drag.Y, drag.X)) * Matrix.CreateTranslation(mid.X, mid.Y)))
            {
                context.DrawRectangle(null, new Pen(viewport, 5), new Rect(-bw / 2, -9, bw, 18));
                context.DrawRectangle(null, new Pen(warn, 2), new Rect(-bw / 2, -9, bw, 18));
            }
            var tick = new Vector(-drag.Y, drag.X) * 7;
            context.DrawLine(new Pen(viewport, 2), profileEnd - tick, profileEnd + tick);
        }
        Arrow(context, profileEnd, inducedEnd, induced, 4, [4, 2], 10);
        drawn.Add(new DrawnVector("drag-induced", profileEnd, inducedEnd));
        bool cp = f.Anchor == ForceAnchor.CentreOfPressure;
        context.DrawEllipse(cp ? ink : viewport, new Pen(cp ? viewport : ink, 2), anchor, cp ? 5 : 5.5, cp ? 5 : 5.5);
        Vectors = drawn;

        Units u = f.Units;
        Place(Labels.FreeStream(f.AlphaGeoDeg), ox - 134, vEnd.Y - 38);
        Place2(Labels.LocalInflow(f.AlphaEffDeg), Labels.LocalInflowWhy, 8, oy + 16);
        string liftText = Labels.LiftLabel(f.LiftPerSpan, u), scaleText = Labels.LiftScale(Labels.ForcePerSpan(f.LiftScale, u), u);
        bool shortLift = lpx < 40;
        Place2(liftText, scaleText, shortLift ? anchor.X - 6 : liftEnd.X + 14, shortLift ? anchor.Y - 100 : liftEnd.Y - 14);
        // Ruling 131: the CP and the couple labels carry the approved bias wording (COPY-SF17) on a second line
        if (cp) Place2(Labels.AnchorCp(f.XcpOverC!.Value), Labels.LatticeBias(f.NChord), anchor.X - 10, oy + yI, right: true);
        else Place(Labels.AnchorQuarter, anchor.X - 10, oy + yI + 20, right: true);
        // the couple label goes on the side of the chord away from the Cp_min plate
        if (!cp) Place2(Labels.CoupleLabel(f.CouplePerSpan, f.CoupleScale, u), Labels.LatticeBias(f.NChord), anchor.X + 44, cpSide == "lower" ? anchor.Y - 62 : anchor.Y + 38);
        Place(Labels.InducedDragLabel(f.InducedPerSpan, u, f.DragMultiple), imx - 14, oy + yI);
        if (f.ProfileLow is { } low && f.ProfileHigh is { } high)
        {
            Place(Labels.ProfileDragLabel(low, high, u, f.DragMultiple, f.LowConfidence), pmx - 26, oy + yP);
            Place(Labels.TotalDragLabel(f.TotalPerSpan, u, f.DragMultiple), pmx - 26, oy + yT);
        }
        else Place(Labels.ProfileDragRow + " " + Labels.UnavailableBecause(f.ProfileUnavailable ?? Labels.NoPolar), pmx - 26, oy + yP);
    }

    // A line with an optional triangular head of the given size (0 for none); the pen is in view pixels.
    private static void Arrow(DrawingContext context, Point from, Point to, IBrush brush, double width, double[]? dash, double head)
    {
        Vector v = to - from;
        double length = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        if (length < 0.5) return;
        var u = new Vector(v.X / length, v.Y / length);
        Point shaftEnd = head > 0 ? to - u * Math.Min(head, length) : to;
        context.DrawLine(new Pen(brush, width, dash is null ? null : new DashStyle(dash, 0)), from, shaftEnd);
        if (head <= 0) return;
        double size = Math.Min(head, length);
        var n = new Vector(-u.Y, u.X) * (size / 2);
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(to, true);
            c.LineTo(to - u * size + n);
            c.LineTo(to - u * size - n);
            c.EndFigure(true);
        }
        context.DrawGeometry(brush, new Pen(brush, 1), g);
    }

    // The pitching-moment couple: a 200-degree arc round the anchor. A nose-down (negative) M' turns counter-clockwise on screen,
    // as the mockup draws it; a nose-up one is its mirror image.
    private static void Couple(DrawingContext context, Point centre, double moment, IBrush brush, double width, double head, double radius = 34)
    {
        const double span = 200.0 / 360 * Math.Tau;
        double sign = moment < 0 ? -1 : 1, start = moment < 0 ? -60.0 / 360 * Math.Tau : 240.0 / 360 * Math.Tau;
        Point At(double t) => new(centre.X + radius * Math.Cos(t), centre.Y + radius * Math.Sin(t));
        var arc = new StreamGeometry();
        using (StreamGeometryContext c = arc.Open())
        {
            c.BeginFigure(At(start), false);
            for (int i = 1; i <= 24; i++) c.LineTo(At(start + sign * span * i / 24));
            c.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(brush, width), arc);
        if (head <= 0) return;
        double end = start + sign * span;
        var tangent = new Vector(-Math.Sin(end) * sign, Math.Cos(end) * sign);
        Point tip = At(end) + tangent * 2;
        var n = new Vector(-tangent.Y, tangent.X) * (head / 2);
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(tip, true);
            c.LineTo(tip - tangent * head + n);
            c.LineTo(tip - tangent * head - n);
            c.EndFigure(true);
        }
        context.DrawGeometry(brush, new Pen(brush, 1), g);
    }

    // A two-line plate: the label in ink, its second line in the muted ink.
    private Rect Plate2(DrawingContext context, string first, string second, Point at, IBrush ink, IBrush mute, IBrush soft)
    {
        FormattedText a = Text(first, ink), b = Text(second, mute);
        var rect = new Rect(at.X, at.Y - 12, Math.Max(a.Width, b.Width) + 8, 32);
        placed.Add((first, rect));
        context.DrawRectangle(soft, null, rect, 2);
        context.DrawText(a, new Point(at.X + 4, at.Y - 11));
        context.DrawText(b, new Point(at.X + 4, at.Y + 4));
        return rect;
    }

    // The key: one swatch and one label per vector, on the page surface below the viewport, wrapping to a second row.
    private void DrawKey(DrawingContext context, double top, double width)
    {
        IBrush ink = Resource("InkBrush", Brushes.Black), mute = Resource("MutedBrush", Brushes.Gray), warn = Resource("WarningBrush", Brushes.Goldenrod);
        const double row = 20;
        double x = 0, y = top + row / 2 + 2;
        for (int i = 0; i < Labels.VectorKey.Length; i++)
        {
            FormattedText label = Text(Labels.VectorKey[i], mute);
            double swatch = i == 5 ? 18 : 30, item = swatch + 6 + label.Width + 16;
            if (x + item > width && x > 0) { x = 0; y += row; }
            Point a = new(x + 1, y), b = new(x + swatch - 1, y);
            switch (i)
            {
                case 0: context.DrawLine(new Pen(ink, 2), a, b); break;
                case 1: context.DrawLine(new Pen(mute, 1.5, new DashStyle([5, 4], 0)), a, b); break;
                case 2: context.DrawLine(new Pen(ink, 3), a, b); break;
                case 3: context.DrawLine(new Pen(warn, 4), a, b); break;
                case 4: context.DrawLine(new Pen(warn, 4, new DashStyle([4, 2], 0)), a, b); break;
                default: Couple(context, new Point(x + 9, y + 1), 1, ink, 2, 0, 6); break;
            }
            context.DrawText(label, new Point(x + swatch + 6, y - 7));
            x += item;
        }
    }

    // A text plate with its top-left at (x, y - 12), the mockup's .plate; returns its rectangle.
    private Rect Plate(DrawingContext context, string text, Point at, IBrush ink, IBrush soft)
    {
        FormattedText t = Text(text, ink);
        var rect = new Rect(at.X, at.Y - 12, t.Width + 8, 16);
        placed.Add((text, rect));
        void Draw()
        {
            context.DrawRectangle(soft, null, rect, 2);
            context.DrawText(t, new Point(at.X + 4, at.Y - 11));
        }
        if (deferred is { } later) later.Add(Draw); else Draw();
        return rect;
    }

    private static FormattedText Text(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, Typeface.Default, 11, brush);
}
