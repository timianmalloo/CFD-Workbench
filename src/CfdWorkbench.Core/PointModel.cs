namespace CfdWorkbench.Core;

public enum PointRole { RootEnd, RootHandle, Control, AnchorHandle, Anchor, TipHandle, TipEnd }
public enum TangentKind { Corner, Smooth, Symmetric }
public enum PointFreedom { Fixed, SpanOnly, AftOnly, Free }

public static class PointModel
{
    // One source of truth for the rails on which point editing is defined.
    public static IReadOnlySet<string> EditableCurves { get; } = new HashSet<string>(StringComparer.Ordinal)
    { "leading", "trailing" };
}

public sealed record PointView(string Curve, string Id, int Index, double Eta, double SpanMeters, double AftMeters,
    PointRole Role, string? AnchorId, TangentKind? Kind, PointFreedom Freedom, IReadOnlyList<string> Locks);
public sealed record PlanSample(double SpanMeters, double AftMeters);
public sealed record CombTooth(double SpanMeters, double AftMeters, double NormalSpan, double NormalAft,
    double Curvature, bool BreakBefore);
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

    public static (double SpanMeters, double AftMeters) HandleTarget(PlanformView view, string curve, string handleId, double angleDegrees, double lengthMeters)
    {
        ArgumentNullException.ThrowIfNull(view);
        var rail = curve == "leading" ? view.Leading : view.Trailing;
        var handle = rail.Points.Single(point => point.Id == handleId);
        var anchor = rail.Points.Single(point => point.Id == handle.AnchorId);
        double radians = angleDegrees * (Math.PI / 180);
        double span = Math.Cos(radians) * lengthMeters;
        double aft = Math.Sin(radians) * lengthMeters;
        if (handle.Index < anchor.Index) { span = -span; aft = -aft; }
        return (anchor.SpanMeters + span, anchor.AftMeters + aft);
    }

    private static CurveView Project(Definition definition, string name, int ceiling)
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
            if (anchor)
            {
                int row = Array.FindIndex(curve.Tangents, item => item.Id == curve.Ids[index]);
                kind = row < 0 ? TangentKind.Corner : curve.Tangents[row].Kind == "symmetric" ? TangentKind.Symmetric : TangentKind.Smooth;
            }
            var freedom = Freedom(name, index, points.Length, mirror);
            string[] locks = definition.Locks.Where(item => item.Channel.Text == name &&
                (item.Kind == "root_mirror" ? index < 2 : item.Id is null || item.Id.String == curve.Ids[index]))
                .Select(item => item.Kind).Distinct().ToArray();
            double eta = curve.Points[index][0];
            points[index] = new(name, curve.Ids[index], index, eta, eta * definition.HalfSpan, curve.Points[index][1],
                role, anchorId, kind, freedom, locks);
        }
        var samples = new List<PlanSample>();
        for (int span = curve.Degree; span < curve.Points.Length; span++)
        {
            if (curve.Knots[span] >= curve.Knots[span + 1]) continue;
            double start = curve.Knots[span], end = curve.Knots[span + 1];
            for (int step = 0; step < 8; step++)
            {
                double t = start + (step + 0.5) / 8.0 * (end - start);
                var place = Evaluate(curve.Knots, curve.Points, curve.Degree, t, definition.HalfSpan);
                samples.Add(new(place.Span, place.Aft));
            }
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
        if (index == 0 && curve == "leading") return PointFreedom.Fixed;
        if (index == 0 && curve == "trailing" && mirror) return PointFreedom.AftOnly;
        if (index == 1 && mirror) return PointFreedom.SpanOnly;
        if (index == count - 1) return PointFreedom.AftOnly;
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
                arc[step] = arc[step - 1] + Math.Sqrt(Math.Pow(places[step].Span - places[step - 1].Span, 2) + Math.Pow(places[step].Aft - places[step - 1].Aft, 2));
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
            double eta = curve.Points[index].Eta, ordinate = curve.Points[index].AftMeters;
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
        Evaluate(Knots(curve), curve.Points.Select(point => new[] { point.Eta, point.AftMeters }).ToArray(), 3, t, halfSpan);

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
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (low + high) / 2;
            if (Evaluate(curve, mid, halfSpan).Span < eta * halfSpan) low = mid;
            else high = mid;
        }
        return Evaluate(curve, (low + high) / 2, halfSpan).Aft;
    }
}
