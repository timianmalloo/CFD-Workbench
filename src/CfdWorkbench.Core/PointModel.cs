namespace CfdWorkbench.Core;

public enum PointRole { RootEnd, RootHandle, Control, AnchorHandle, Anchor, TipHandle, TipEnd, Nose, NoseHandle, TrailingEnd, TrailingHandle }
public enum TangentKind { Corner, Smooth, Symmetric, Horizontal, Vertical, Angle }
public enum PointFreedom { Fixed, SpanOnly, ValueOnly, Free }

public static class PointModel
{
    // The five editable channels.
    public static IReadOnlySet<string> EditableCurves { get; } = new HashSet<string>(Channels.Names, StringComparer.Ordinal);
}

public sealed record PointView(string Curve, string Id, int Index, double Eta, double SpanMeters, double Ordinate,
    PointRole Role, string? AnchorId, TangentKind? Kind, PointFreedom Freedom, IReadOnlyList<string> Locks, double? AngleDegrees = null);
public sealed record PlanSample(double SpanMeters, double Ordinate);
public sealed record CombTooth(double SpanMeters, double Ordinate, double NormalSpan, double NormalAft,
    double Curvature, bool BreakBefore);

public readonly record struct HandleTargetPoint(double SpanMeters, double Ordinate);
public sealed record CurveView(string Curve, int Ceiling, IReadOnlyList<double> Knots,
    IReadOnlyList<PointView> Points, IReadOnlyList<PlanSample> Samples);
public sealed record PlanformView(string SourceHash, string Basis, long Generation, string Version,
    double HalfSpanMeters, CurveView Leading, CurveView Trailing, IReadOnlyList<AuthoredAssignment> Stations);
public sealed record ProbeReading(double Eta, double SpanMeters, double LeadingAftMeters, double TrailingAftMeters,
    double ChordMeters);

public static class Planform
{
    public static PlanformView View(byte[] source, string basis, long generation)
    {
        ArgumentNullException.ThrowIfNull(source);
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" } definition)
            throw new ContractError(parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-PATCH");
        int ceiling = definition.Version == "4.1" ? 16 : 10;
        return new(parsed.SourceHash, basis, generation, definition.Version, definition.HalfSpan,
            Project(definition, "leading", ceiling), Project(definition, "trailing", ceiling), parsed.Authored().Assignments);
    }

    public static IReadOnlyList<CombTooth> Comb(CurveView curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        double halfSpan = curve.Points.Count == 0 || curve.Points[0].Eta == 0 && curve.Points[0].SpanMeters == 0
            ? curve.Points[^1].SpanMeters : curve.Points[^1].SpanMeters / curve.Points[^1].Eta;
        var teeth = new List<CombTooth>();
        var knots = curve.Knots;
        int count = curve.Points.Count;
        for (int span = 3; span < count; span++)
        {
            if (knots[span] >= knots[span + 1]) continue;
            teeth.AddRange(ArcTeeth(curve, halfSpan, knots[span], knots[span + 1]));
        }
        var marked = new HashSet<double>();
        var knotArray = knots as double[] ?? knots.ToArray();
        for (int index = 3; index <= count - 4; index++)
        {
            if (!FoilSource.IsAnchor(knotArray, count, 3, index)) continue;
            double knot = knots[index + 1];
            bool corner = curve.Points[index].Kind == TangentKind.Corner;
            teeth.Add(Tooth(curve, halfSpan, Math.Max(0, knot - 1e-9), false));
            teeth.Add(Tooth(curve, halfSpan, Math.Min(1, knot + 1e-9), corner));
            marked.Add(knot);
        }
        for (int index = 0; index < knots.Count;)
        {
            double knot = knots[index];
            int end = index;
            while (end + 1 < knots.Count && knots[end + 1] == knot) end++;
            if (end - index + 1 == 2 && knot > 0 && knot < 1 && marked.Add(knot))
                teeth.Add(Tooth(curve, halfSpan, knot, false));
            index = end + 1;
        }
        return teeth;
    }

    public static ProbeReading Probe(PlanformView view, double eta)
    {
        ArgumentNullException.ThrowIfNull(view);
        double leading = AftAt(view.Leading, eta, view.HalfSpanMeters);
        double trailing = AftAt(view.Trailing, eta, view.HalfSpanMeters);
        return new(eta, eta * view.HalfSpanMeters, leading, trailing, trailing - leading);
    }

    public static HandleTargetPoint HandleTarget(PlanformView view, string curve, string handleId, double angleDegrees, double lengthMeters)
    {
        ArgumentNullException.ThrowIfNull(view);
        var rail = curve == "leading" ? view.Leading : view.Trailing;
        var handle = rail.Points.Single(point => point.Id == handleId);
        var anchor = rail.Points.Single(point => point.Id == handle.AnchorId);
        var (sin, cos) = PlacementRule.SinCosDegrees(angleDegrees);
        double span = cos * lengthMeters;
        double aft = sin * lengthMeters;
        if (handle.Index < anchor.Index) { span = -span; aft = -aft; }
        return new(anchor.SpanMeters + span, anchor.Ordinate + aft);
    }

    private static TangentKind MapKind(string kind) => kind switch
    {
        "symmetric" => TangentKind.Symmetric,
        "smooth" => TangentKind.Smooth,
        "horizontal" => TangentKind.Horizontal,
        "vertical" => TangentKind.Vertical,
        "angle" => TangentKind.Angle,
        _ => TangentKind.Corner
    };

    internal static CurveView Project(Definition definition, string name, int ceiling)
    {
        var curve = definition.Curves[name];
        bool mirror = definition.Locks.Any(item => item.Kind == "root_mirror" && item.Channel.Text == name);
        var anchors = new bool[curve.Points.Length];
        for (int index = 0; index < anchors.Length; index++)
            anchors[index] = FoilSource.IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, index);
        var points = new PointView[curve.Points.Length];
        for (int index = 0; index < points.Length; index++)
        {
            bool anchor = anchors[index];
            bool neighbor = (index > 0 && anchors[index - 1]) || (index + 1 < anchors.Length && anchors[index + 1]);
            var role = Role(index, points.Length, anchor, neighbor);
            string? anchorId = role switch
            {
                PointRole.RootHandle => curve.Ids[0],
                PointRole.TipHandle => curve.Ids[^1],
                PointRole.AnchorHandle when index > 0 && anchors[index - 1] => curve.Ids[index - 1],
                PointRole.AnchorHandle => curve.Ids[index + 1],
                _ => null
            };
            TangentKind? kind = null;
            int row = anchor ? Array.FindIndex(curve.Tangents, item => item.Id == curve.Ids[index]) : -1;
            if (anchor) kind = row < 0 ? TangentKind.Corner : MapKind(curve.Tangents[row].Kind);
            var freedom = Freedom(name, index, points.Length, mirror);
            string[] locks = definition.Locks.Where(item => item.Channel.Text == name &&
                (item.Kind == "root_mirror" ? index < 2 : item.Id is null || item.Id.String == curve.Ids[index]))
                .Select(item => item.Kind).Distinct().ToArray();
            double eta = curve.Points[index][0];
            double? angle = anchor && row >= 0 ? curve.Tangents[row].Angle : null;
            points[index] = new(name, curve.Ids[index], index, eta, eta * definition.HalfSpan, curve.Points[index][1],
                role, anchorId, kind, freedom, locks, angle);
        }
        int spans = 0;
        for (int span = curve.Degree; span < curve.Points.Length; span++)
            if (curve.Knots[span] < curve.Knots[span + 1]) spans++;
        int per = 8;
        if (spans > 0)
            while (per + (spans - 1) * (per - 1) < 64) per++;
        var samples = new List<PlanSample>();
        bool firstSpan = true;
        for (int span = curve.Degree; span < curve.Points.Length; span++)
        {
            if (curve.Knots[span] >= curve.Knots[span + 1]) continue;
            double start = curve.Knots[span], end = curve.Knots[span + 1];
            for (int step = firstSpan ? 0 : 1; step < per; step++)
            {
                double t = start + step / (double)(per - 1) * (end - start);
                var place = Evaluate(curve.Knots, curve.Points, curve.Degree, t, definition.HalfSpan);
                samples.Add(new(place.Span, place.Aft));
            }
            firstSpan = false;
        }
        return new(name, ceiling, curve.Knots, points, samples);
    }

    private static PointRole Role(int index, int count, bool anchor, bool neighbor)
    {
        if (index == 0) return PointRole.RootEnd;
        if (index == 1) return PointRole.RootHandle;
        if (index == count - 1) return PointRole.TipEnd;
        if (index == count - 2) return PointRole.TipHandle;
        if (anchor) return PointRole.Anchor;
        if (neighbor) return PointRole.AnchorHandle;
        return PointRole.Control;
    }

    private static PointFreedom Freedom(string curve, int index, int count, bool mirror)
    {
        if (index == 0 && curve is "leading" or "dihedral") return PointFreedom.Fixed;
        if (index == count - 1 || index == 0) return PointFreedom.ValueOnly;
        if (index == 1 && mirror) return PointFreedom.SpanOnly;
        return PointFreedom.Free;
    }

    private static IEnumerable<CombTooth> ArcTeeth(CurveView curve, double halfSpan, double start, double end)
    {
        const int dense = 32;
        var parameter = new double[dense + 1];
        var arc = new double[dense + 1];
        var places = new (double Span, double Aft)[dense + 1];
        for (int step = 0; step <= dense; step++)
        {
            parameter[step] = start + step / (double)dense * (end - start);
            places[step] = Evaluate(curve, parameter[step], halfSpan);
            if (step > 0)
                arc[step] = arc[step - 1] + Math.Sqrt((places[step].Span - places[step - 1].Span) * (places[step].Span - places[step - 1].Span) + (places[step].Aft - places[step - 1].Aft) * (places[step].Aft - places[step - 1].Aft));
        }
        for (int tooth = 0; tooth < 8; tooth++)
        {
            double target = (tooth + 0.5) / 8.0 * arc[dense];
            int step = 1;
            while (step < dense && arc[step] < target) step++;
            double span = arc[step] - arc[step - 1];
            double fraction = span == 0 ? 0 : (target - arc[step - 1]) / span;
            double t = parameter[step - 1] + fraction * (parameter[step] - parameter[step - 1]);
            yield return Tooth(curve, halfSpan, t, false);
        }
    }

    private static CombTooth Tooth(CurveView curve, double halfSpan, double t, bool breakBefore)
    {
        var jet = SplineBasis.Evaluate(Knots(curve), 3, t);
        double span = 0, aft = 0, dSpan = 0, dAft = 0, ddSpan = 0, ddAft = 0;
        for (int index = 0; index < curve.Points.Count; index++)
        {
            double eta = curve.Points[index].Eta, ordinate = curve.Points[index].Ordinate;
            span += jet.N[index] * eta * halfSpan;
            aft += jet.N[index] * ordinate;
            dSpan += jet.D1[index] * eta * halfSpan;
            dAft += jet.D1[index] * ordinate;
            ddSpan += jet.D2[index] * eta * halfSpan;
            ddAft += jet.D2[index] * ordinate;
        }
        double speed2 = dSpan * dSpan + dAft * dAft;
        double speed = Math.Sqrt(speed2);
        double curvature = speed2 == 0 ? 0 : (dSpan * ddAft - dAft * ddSpan) / (speed2 * speed);
        return new(span, aft, speed == 0 ? 0 : -dAft / speed, speed == 0 ? 0 : dSpan / speed, curvature, breakBefore);
    }

    private static (double Span, double Aft) Evaluate(CurveView curve, double t, double halfSpan) =>
        Evaluate(Knots(curve), curve.Points.Select(point => new[] { point.Eta, point.Ordinate }).ToArray(), 3, t, halfSpan);

    private static (double Span, double Aft) Evaluate(double[] knots, double[][] points, int degree, double t, double halfSpan)
    {
        var jet = SplineBasis.Evaluate(knots, degree, t);
        double eta = 0, aft = 0;
        for (int index = 0; index < points.Length; index++)
        {
            eta += jet.N[index] * points[index][0];
            aft += jet.N[index] * points[index][1];
        }
        return (eta * halfSpan, aft);
    }

    private static double[] Knots(CurveView curve) => curve.Knots as double[] ?? curve.Knots.ToArray();

    private static double AftAt(CurveView curve, double eta, double halfSpan)
    {
        // Span scaling is not part of the inversion. The shared evaluator inverts η directly.
        _ = halfSpan;
        var points = new double[curve.Points.Count][];
        for (int index = 0; index < points.Length; index++)
            points[index] = [curve.Points[index].Eta, curve.Points[index].Ordinate];
        return ChannelEvaluator.Value(Knots(curve), 3, points, eta);
    }
}

public sealed record ChannelUnit(string Curve, string SiUnit, string DisplayUnit, double Quantum,
    double NudgeFine, double NudgePlain, double NudgeCoarse, double? RowTolerance,
    double? DomainLower, double? DomainUpper, string HandleTyping);

public static class Channels
{
    public static readonly string[] Names = ["leading", "trailing", "dihedral", "twist", "thickness"];
    public const string AngleAndLength = "AngleAndLength";
    public const string SpanAndValue = "SpanAndValue";

    public static string Family(string curve) => curve is "leading" or "trailing" ? "rail" : curve;

    public static CurveView View(byte[] source, string curve, string basis, long generation)
    {
        _ = basis;
        _ = generation;
        ArgumentNullException.ThrowIfNull(source);
        var (definition, ceiling) = Open(source);
        if (!definition.Curves.ContainsKey(curve)) throw new ContractError("DSL-TARGET");
        return Planform.Project(definition, curve, ceiling);
    }

    public static ChannelUnit Unit(string curve) => curve switch
    {
        "leading" or "trailing" => Length(curve, null, null, null),
        "dihedral" => Length(curve, 1e-6, null, null),
        "twist" => new("twist", "deg", "°", 1e-5, 0.01, 0.1, 1, 1e-6,
            -Geometry.TwistDomainDegrees, Geometry.TwistDomainDegrees, SpanAndValue),
        "thickness" => new("thickness", "1", "%", 1e-7, 1e-4, 1e-3, 1e-2, 1e-8,
            Geometry.ThicknessDomain.Lower, Geometry.ThicknessDomain.Upper, SpanAndValue),
        _ => throw new ContractError("DSL-TARGET")
    };

    internal static (Definition Definition, int Ceiling) Open(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" } definition)
            throw new ContractError(parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-PATCH");
        return (definition, definition.Version == "4.1" ? 16 : 10);
    }

    private static ChannelUnit Length(string curve, double? rowTolerance, double? lower, double? upper) =>
        new(curve, "m", "mm", 1e-6, 1e-5, 1e-4, 1e-3, rowTolerance, lower, upper,
            curve is "twist" or "thickness" ? SpanAndValue : AngleAndLength);
}
