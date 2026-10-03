namespace CfdWorkbench.Core;

public sealed record SectionFacts(
    double OwnThickness,
    double OwnThicknessX,
    double UpperLeRadius,
    double LowerLeRadius,
    double TrailingGap,
    double TrailingWedgeDegrees,
    double StationChordMeters,
    double StationThicknessRatio,
    double StationUpperLeRadius,
    double StationLowerLeRadius,
    double StationTrailingGap,
    double StationTrailingWedgeDegrees);

public sealed record SectionProbeReading(
    double X,
    double UpperY,
    double LowerY,
    double Thickness,
    double Camber,
    double PlacedThicknessMeters);

public sealed record KindChoice(TangentKind Kind, bool Enabled, string? Reason);

public sealed record SectionPointRecord(int Assignment, string Side, string Id, int Index, string Role, string Type, string? Kind, double? Angle);

public static class Sections
{
    public const string VerticalInteriorReason =
        "A vertical tangent inside a surface makes a step. The nose already has one.";

    public static CurveView View(byte[] source, int assignment, SurfaceSide side, string basis, long generation)
    {
        ArgumentNullException.ThrowIfNull(source);
        var definition = Require(source);
        var profile = definition.Profiles[definition.Assignments[assignment].Profile];
        var curve = side == SurfaceSide.Upper ? profile.Upper : profile.Lower;
        return Project(curve, side == SurfaceSide.Upper ? "upper" : "lower", profile.Closure);
    }

    public static SectionFacts Facts(byte[] source, int assignment)
    {
        var definition = Require(source);
        var profile = definition.Profiles[definition.Assignments[assignment].Profile];
        var shape = Measure(profile.Upper, profile.Lower);
        var frame = Placement.Frame(source, definition.Assignments[assignment].Eta);
        double k = shape.OwnThickness == 0 ? 0 : frame.ThicknessRatio / shape.OwnThickness;
        bool mirror = Mirror(profile.Upper, profile.Lower);
        double upperStation = mirror ? shape.UpperRadius * k * k : PlacedRadius(profile.Upper, k);
        double lowerStation = mirror ? shape.LowerRadius * k * k : PlacedRadius(profile.Lower, k);
        double toDegrees = 1 / PlacementRule.RadiansPerDegree;
        double wedge = mirror
            ? 2 * Math.Atan(k * Math.Tan(shape.WedgeRadians / 2)) * toDegrees
            : shape.WedgeRadians * toDegrees;
        return new(shape.OwnThickness, shape.OwnThicknessX, shape.UpperRadius, shape.LowerRadius, shape.Gap,
            shape.WedgeRadians * toDegrees, frame.ChordMeters, frame.ThicknessRatio,
            upperStation, lowerStation, shape.Gap * k, wedge);
    }

    public static SectionProbeReading Probe(byte[] source, int assignment, double x)
    {
        var definition = Require(source);
        var profile = definition.Profiles[definition.Assignments[assignment].Profile];
        double upper = ProfileEvaluator.OrdinateAt(profile.Upper, x);
        double lower = ProfileEvaluator.OrdinateAt(profile.Lower, x);
        var facts = Facts(source, assignment);
        double thickness = upper - lower;
        double placed = facts.OwnThickness == 0 ? 0 : thickness / facts.OwnThickness * facts.StationThicknessRatio * facts.StationChordMeters;
        return new(x, upper, lower, thickness, (upper + lower) / 2, placed);
    }

    public static (double X0, double X1)? DisplayCrossing(byte[] source, int assignment)
    {
        var definition = Require(source);
        var profile = definition.Profiles[definition.Assignments[assignment].Profile];
        var upper = ProfileEvaluator.Samples(profile.Upper, 401);
        var lower = ProfileEvaluator.Samples(profile.Lower, 401);
        double? start = null, end = null;
        int count = Math.Min(upper.Length, lower.Length);
        for (int index = 0; index < count; index++)
        {
            if (upper[index].Y >= lower[index].Y) continue;
            start = start is null ? upper[index].X : Math.Min(start.Value, upper[index].X);
            end = end is null ? upper[index].X : Math.Max(end.Value, upper[index].X);
        }
        return start is null || end is null ? null : (start.Value, end.Value);
    }

    public static IReadOnlyList<CombTooth> Comb(CurveView surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        var knots = surface.Knots as double[] ?? surface.Knots.ToArray();
        var teeth = new List<CombTooth>();
        foreach (var point in surface.Points)
        {
            if (point.Role != PointRole.Anchor) continue;
            double knot = knots[point.Index + 1];
            teeth.Add(Tooth(surface, knot, true));
        }
        return teeth;
    }

    public static IReadOnlyList<KindChoice> KindChoices(PointView point)
    {
        ArgumentNullException.ThrowIfNull(point);
        bool interior = point.Role == PointRole.Anchor;
        TangentKind[] kinds = [TangentKind.Corner, TangentKind.Smooth, TangentKind.Symmetric, TangentKind.Horizontal, TangentKind.Vertical, TangentKind.Angle];
        var choices = new KindChoice[kinds.Length];
        for (int index = 0; index < kinds.Length; index++)
        {
            bool blocked = kinds[index] == TangentKind.Vertical && interior;
            choices[index] = new(kinds[index], interior && !blocked, blocked ? VerticalInteriorReason : null);
        }
        return choices;
    }

    public static IReadOnlyList<SectionPointRecord> Points(byte[] source)
    {
        var definition = Require(source);
        var points = new List<SectionPointRecord>();
        for (int assignment = 0; assignment < definition.Assignments.Length; assignment++)
        {
            foreach (var side in new[] { SurfaceSide.Upper, SurfaceSide.Lower })
            {
                string name = side == SurfaceSide.Upper ? "upper" : "lower";
                foreach (var point in View(source, assignment, side, "accepted", 0).Points)
                    points.Add(new(assignment, name, point.Id, point.Index, point.Role.ToString(), PointType(point), point.Kind?.ToString(), point.AngleDegrees));
            }
        }
        return points;
    }

    public static string PointType(PointView point) => point.Role switch
    {
        PointRole.Nose or PointRole.Anchor or PointRole.TrailingEnd => "Anchor",
        PointRole.NoseHandle or PointRole.AnchorHandle or PointRole.TrailingHandle => "Handle",
        _ => "Control"
    };

    private static CurveView Project(Curve curve, string name, string closure)
    {
        int count = curve.Points.Length;
        var anchors = new bool[count];
        for (int index = 0; index < count; index++)
            anchors[index] = FoilSource.IsAnchor(curve.Knots, count, curve.Degree, index);
        var points = new PointView[count];
        for (int index = 0; index < count; index++)
        {
            bool neighbor = (index > 0 && anchors[index - 1]) || (index + 1 < count && anchors[index + 1]);
            var role = index == 0 ? PointRole.Nose
                : index == 1 ? PointRole.NoseHandle
                : index == count - 1 ? PointRole.TrailingEnd
                : index == count - 2 ? PointRole.TrailingHandle
                : anchors[index] ? PointRole.Anchor
                : neighbor ? PointRole.AnchorHandle
                : PointRole.Control;
            int row = curve.Ids is null ? -1 : Array.FindIndex(curve.Tangents, item => item.Id == curve.Ids[index]);
            TangentKind? kind = role == PointRole.Anchor ? row < 0 ? TangentKind.Corner : MapKind(curve.Tangents[row].Kind) : null;
            double? angle = role == PointRole.Anchor && row >= 0 ? curve.Tangents[row].Angle : null;
            string? anchorId = role == PointRole.AnchorHandle
                ? curve.Ids![index > 0 && anchors[index - 1] ? index - 1 : index + 1]
                : null;
            var freedom = role switch
            {
                PointRole.Nose => PointFreedom.Fixed,
                PointRole.NoseHandle => PointFreedom.ValueOnly,
                PointRole.TrailingEnd => closure == "open" ? PointFreedom.ValueOnly : PointFreedom.Fixed,
                _ => PointFreedom.Free
            };
            double x = curve.Points[index][0], y = curve.Points[index][1];
            points[index] = new(name, curve.Ids![index], index, x, x, y, role, anchorId, kind, freedom, Array.Empty<string>(), angle);
        }
        return new(name, 32, curve.Knots, points, Array.Empty<PlanSample>());
    }

    private static TangentKind MapKind(string kind) => kind switch
    {
        "smooth" => TangentKind.Smooth,
        "symmetric" => TangentKind.Symmetric,
        "horizontal" => TangentKind.Horizontal,
        "vertical" => TangentKind.Vertical,
        "angle" => TangentKind.Angle,
        _ => TangentKind.Corner
    };

    private readonly record struct Shape(double OwnThickness, double OwnThicknessX, double UpperRadius, double LowerRadius, double Gap, double WedgeRadians);

    private static Shape Measure(Curve upper, Curve lower)
    {
        var upperSamples = ProfileEvaluator.Samples(upper, 4001);
        var lowerSamples = ProfileEvaluator.Samples(lower, 4001);
        double thickness = 0, at = 0;
        int count = Math.Min(upperSamples.Length, lowerSamples.Length);
        for (int index = 0; index < count; index++)
        {
            double local = upperSamples[index].Y - lowerSamples[index].Y;
            if (local > thickness) { thickness = local; at = upperSamples[index].X; }
        }
        double gap = ProfileEvaluator.OrdinateAt(upper, 1) - ProfileEvaluator.OrdinateAt(lower, 1);
        return new(thickness, at, Radius(upper), Radius(lower), gap, Wedge(upper, lower));
    }

    private static double Radius(Curve curve)
    {
        var jet = ProfileEvaluator.Jet(curve, 0);
        double cross = jet.Xt * jet.Ytt - jet.Yt * jet.Xtt;
        double speed2 = jet.Xt * jet.Xt + jet.Yt * jet.Yt;
        return 1.0 / Math.Abs(cross / (speed2 * Math.Sqrt(speed2)));
    }

    private static double PlacedRadius(Curve curve, double k)
    {
        var jet = ProfileEvaluator.Jet(curve, 0);
        double xt = jet.Xt, yt = k * jet.Yt, xtt = jet.Xtt, ytt = k * jet.Ytt;
        double cross = xt * ytt - yt * xtt;
        double speed2 = xt * xt + yt * yt;
        return 1.0 / Math.Abs(cross / (speed2 * Math.Sqrt(speed2)));
    }

    private static double Wedge(Curve upper, Curve lower)
    {
        var top = ProfileEvaluator.Jet(upper, 1);
        var bottom = ProfileEvaluator.Jet(lower, 1);
        double dot = top.Xt * bottom.Xt + top.Yt * bottom.Yt;
        double topLength = Math.Sqrt(top.Xt * top.Xt + top.Yt * top.Yt);
        double bottomLength = Math.Sqrt(bottom.Xt * bottom.Xt + bottom.Yt * bottom.Yt);
        return Math.Acos(Math.Clamp(dot / (topLength * bottomLength), -1, 1));
    }

    private static bool Mirror(Curve upper, Curve lower)
    {
        var top = ProfileEvaluator.Samples(upper, 401);
        var bottom = ProfileEvaluator.Samples(lower, 401);
        int count = Math.Min(top.Length, bottom.Length);
        for (int index = 0; index < count; index++)
            if (Math.Abs(top[index].Y + bottom[index].Y) > 1e-9) return false;
        return true;
    }

    private static CombTooth Tooth(CurveView surface, double t, bool breakBefore)
    {
        var jet = SplineBasis.Evaluate(surface.Knots.ToArray(), 5, t);
        double x = 0, y = 0, xt = 0, yt = 0, xtt = 0, ytt = 0;
        for (int index = 0; index < surface.Points.Count; index++)
        {
            double px = surface.Points[index].SpanMeters, py = surface.Points[index].Ordinate;
            x += jet.N[index] * px;
            y += jet.N[index] * py;
            xt += jet.D1[index] * px;
            yt += jet.D1[index] * py;
            xtt += jet.D2[index] * px;
            ytt += jet.D2[index] * py;
        }
        double speed = Math.Sqrt(xt * xt + yt * yt);
        double curvature = speed == 0 ? 0 : (xt * ytt - yt * xtt) / (speed * speed * speed);
        return new(x, y, speed == 0 ? 0 : -yt / speed, speed == 0 ? 0 : xt / speed, curvature, breakBefore);
    }

    private static Definition Require(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is null)
            throw new ContractError(parsed.Diagnostics.Count == 0 ? "DSL-INVALID" : parsed.Diagnostics[0].Code,
                parsed.Diagnostics.Count == 0 ? "Section source did not parse." : parsed.Diagnostics[0].Reason);
        return parsed.Definition;
    }
}
