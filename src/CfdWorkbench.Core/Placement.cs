using System.Numerics;

namespace CfdWorkbench.Core;

public readonly record struct Point3(double X, double Y, double Z)
{
    public Point3 Port() => new(X, -Y, Z);
}

/// <summary>One placed section of the display mesh. <paramref name="Assignment"/> is the authored station this section lies on
/// (the index into <see cref="AuthoredProjection.Assignments"/>, as every <c>assignmentIndex</c> in Core), or null between
/// stations; the station's profile is <c>Assignments[Assignment].ProfileName</c>, never this number.</summary>
public sealed record PlacedSection(double Eta, int? Assignment, IReadOnlyList<Point3> Upper, IReadOnlyList<Point3> Lower);

public sealed record SurfaceView(string SourceHash, string Basis, long Generation,
    double MinimumX, double MinimumY, double MinimumZ, double MaximumX, double MaximumY, double MaximumZ,
    IReadOnlyList<PlacedSection> Sections);

/// <summary>
/// The Rule A section at one η, for Analysis (design area3-analysis.md §4 item 3; ADR-0010: one placement authority).
/// <paramref name="X"/> are the chord stations; <paramref name="Camber"/> is (zu + zl)/2 and <paramref name="Thickness"/>
/// zu − zl, both normalised to the local chord; <paramref name="CamberSlope"/> is the analytic dz_c/dx from the profile
/// jet, never a difference; <paramref name="PlacedCamber"/> is the camber placed by the placement rule, in metres.
/// Derived on read, never stored.
/// </summary>
public sealed record SectionSample(StationFrame Frame, IReadOnlyList<double> X, IReadOnlyList<double> Camber,
    IReadOnlyList<double> Thickness, IReadOnlyList<double> CamberSlope, IReadOnlyList<Point3> PlacedCamber);

public sealed record StationFrame(double Eta, double SpanMeters, double LeadingMeters, double TrailingMeters,
    double ElevationMeters, double TwistDegrees, double ThicknessRatio)
{
    public double ChordMeters => TrailingMeters - LeadingMeters;
}

internal interface IPlacementScalar<TSelf> :
    IAdditionOperators<TSelf, TSelf, TSelf>,
    ISubtractionOperators<TSelf, TSelf, TSelf>,
    IMultiplyOperators<TSelf, TSelf, TSelf>
    where TSelf : struct, IPlacementScalar<TSelf>
{
    static abstract TSelf Half { get; }
    static abstract TSelf Point(double value);
    static abstract TSelf BlendWeight(double eta, double left, double right);
    static abstract TSelf Radians(TSelf degrees);
    static abstract (TSelf Sin, TSelf Cos) SinCos(TSelf radians);
}

internal static class PlacementRule
{
    internal const double RadiansPerDegree = 0.017453292519943295;
    internal const int TaylorTerms = 16;
    internal const int AngleGridBits = 64;
    internal const int MaximumGrid = 401;

    internal static (int Left, int Right) Select(ReadOnlySpan<double> stationEtas, ReadOnlySpan<int> profiles, double eta,
        Func<int, int, bool> sameGeometry)
    {
        if (stationEtas.Length < 2) return (profiles[0], -1);
        int index = Bracket(stationEtas, eta);
        int left = profiles[index];
        int right = profiles[index + 1];
        var station = Rational.From(eta);
        if (station.CompareTo(Rational.From(stationEtas[index])) == 0 || sameGeometry(left, right)) return (left, -1);
        if (station.CompareTo(Rational.From(stationEtas[index + 1])) == 0) return (right, -1);
        return (left, right);
    }

    internal static int Bracket(ReadOnlySpan<double> stationEtas, double eta)
    {
        var station = Rational.From(eta);
        int index = 0;
        while (index + 1 < stationEtas.Length && Rational.From(stationEtas[index + 1]).CompareTo(station) < 0) index++;
        return index;
    }

    internal static (T Camber, T Unit) Components<T>(T upper, T lower, T maximumReciprocal) where T : struct, IPlacementScalar<T> =>
        ((upper + lower) * T.Half, (upper - lower) * maximumReciprocal);

    internal static (T Upper, T Lower) Section<T>(T upper, T lower, T maximumReciprocal, T thickness) where T : struct, IPlacementScalar<T>
    {
        var (camber, unit) = Components(upper, lower, maximumReciprocal);
        T halfThickness = unit * thickness * T.Half;
        return (camber + halfThickness, camber - halfThickness);
    }

    internal static (T Upper, T Lower) Blend<T>((T Camber, T Unit) a, (T Camber, T Unit) b, T weight,
        T maximumT0Reciprocal, T thickness) where T : struct, IPlacementScalar<T>
    {
        T complement = T.Point(1) - weight;
        T camber = complement * a.Camber + weight * b.Camber;
        T unnormalized = complement * a.Unit + weight * b.Unit;
        T halfThickness = unnormalized * maximumT0Reciprocal * thickness * T.Half;
        return (camber + halfThickness, camber - halfThickness);
    }

    internal static (T X, T Z) Place<T>(T leading, T trailing, T elevation, (T Sin, T Cos) angle, T abscissa, T z)
        where T : struct, IPlacementScalar<T>
    {
        T chord = trailing - leading;
        T x = leading + chord * (abscissa * angle.Cos + z * angle.Sin);
        T placedZ = elevation + chord * (z * angle.Cos - abscissa * angle.Sin);
        return (x, placedZ);
    }
}

internal readonly record struct Binary64(double Value) : IPlacementScalar<Binary64>
{
    public static Binary64 operator +(Binary64 left, Binary64 right) => new(left.Value + right.Value);
    public static Binary64 operator -(Binary64 left, Binary64 right) => new(left.Value - right.Value);
    public static Binary64 operator *(Binary64 left, Binary64 right) => new(left.Value * right.Value);
    public static Binary64 Half => new(0.5);
    public static Binary64 Point(double value) => new(value);
    public static Binary64 BlendWeight(double eta, double left, double right) => new((eta - left) / (right - left));
    public static Binary64 Radians(Binary64 degrees) => new(degrees.Value * PlacementRule.RadiansPerDegree);
    public static (Binary64 Sin, Binary64 Cos) SinCos(Binary64 radians)
    {
        var (sin, cos) = Math.SinCos(radians.Value);
        return (new(sin), new(cos));
    }
}

// The one binary64 channel inversion. WingEstimates and Planform.View read Value.
internal static class ChannelEvaluator
{
    internal static double At(Curve curve, double eta)
    {
        Interlocked.Increment(ref Placement.ChannelEvaluations);
        return Value(curve.Knots, curve.Degree, curve.Points, eta);
    }

    internal static double Value(double[] knots, int degree, double[][] points, double eta)
    {
        if (eta <= points[0][0]) return points[0][1];
        if (eta >= points[^1][0]) return points[^1][1];
        return Dot(knots, degree, points, Parameter(knots, degree, points, eta), 1);
    }

    internal static double Parameter(double[] knots, int degree, double[][] points, double eta)
    {
        if (eta <= points[0][0]) return 0;
        if (eta >= points[^1][0]) return 1;
        double lo = 0, hi = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (lo + hi) / 2;
            if (Dot(knots, degree, points, mid, 0) < eta) lo = mid;
            else hi = mid;
        }
        return (lo + hi) / 2;
    }

    private static double Dot(double[] knots, int degree, double[][] points, double t, int coordinate)
    {
        double[] basis = SplineBasis.Values(knots, degree, t);
        double value = 0;
        for (int i = 0; i < basis.Length; i++) value += basis[i] * points[i][coordinate];
        return value;
    }
}

// The one binary64 profile evaluator (ADR-0010 Amendment 1). x(t), y(t) and their t-derivatives,
// and y at a chord fraction by abscissa inversion. Placement.Prepare, AuthoringSession.Sample and
// the section projections read this; the certificate keeps Bernstein.
internal readonly record struct ProfileJet(double X, double Y, double Xt, double Yt, double Xtt, double Ytt);

internal static class ProfileEvaluator
{
    internal static ProfileJet Jet(Curve curve, double t)
    {
        Interlocked.Increment(ref Placement.ProfileEvaluations);
        var jet = SplineBasis.Evaluate(curve.Knots, curve.Degree, t);
        double x = 0, y = 0, xt = 0, yt = 0, xtt = 0, ytt = 0;
        for (int i = 0; i < curve.Points.Length; i++)
        {
            x += jet.N[i] * curve.Points[i][0];
            y += jet.N[i] * curve.Points[i][1];
            xt += jet.D1[i] * curve.Points[i][0];
            yt += jet.D1[i] * curve.Points[i][1];
            xtt += jet.D2[i] * curve.Points[i][0];
            ytt += jet.D2[i] * curve.Points[i][1];
        }
        return new(x, y, xt, yt, xtt, ytt);
    }

    internal static double ParameterFor(Curve curve, double x)
    {
        if (x <= curve.Points[0][0]) return 0;
        if (x >= curve.Points[^1][0]) return 1;
        double lo = 0, hi = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = (lo + hi) / 2;
            if (AbscissaAt(curve, mid) < x) lo = mid;
            else hi = mid;
        }
        return (lo + hi) / 2;
    }

    internal static double OrdinateAt(Curve curve, double x) => Jet(curve, ParameterFor(curve, x)).Y;

    // Display spacing: x = (1 - cos θ) / 2, so the nose has more samples than the tail.
    internal static ProfilePoint[] Samples(Curve curve, int count)
    {
        if (count < 2) throw new ContractError("DSL-RANGE");
        var samples = new ProfilePoint[count];
        double step = count - 1;
        for (int index = 0; index < count; index++)
        {
            double x = (1 - Math.Cos(Math.PI * index / step)) / 2;
            if (index == 0) x = 0;
            else if (index == count - 1) x = 1;
            samples[index] = new ProfilePoint(x, OrdinateAt(curve, x));
        }
        return samples;
    }

    private static double AbscissaAt(Curve curve, double t)
    {
        Interlocked.Increment(ref Placement.ProfileEvaluations);
        // Same N as SplineBasis.Evaluate, without the derivative rows the abscissa does not read.
        // Degree and point count above the section grammar fall back to that call.
        if (curve.Degree > 8 || curve.Points.Length > 16)
        {
            var basis = SplineBasis.Values(curve.Knots, curve.Degree, t);
            double slow = 0;
            for (int index = 0; index < curve.Points.Length; index++) slow += basis[index] * curve.Points[index][0];
            return slow;
        }
        int degree = curve.Degree;
        int count = curve.Knots.Length - degree - 1;
        int span = FindSpan(curve.Knots, degree, count, t);
        int width = degree + 1;
        Span<double> ndu = stackalloc double[width * width];
        Span<double> left = stackalloc double[width];
        Span<double> right = stackalloc double[width];
        ndu.Clear();
        left.Clear();
        right.Clear();
        ndu[0] = 1;
        for (int level = 1; level <= degree; level++)
        {
            left[level] = t - curve.Knots[span + 1 - level];
            right[level] = curve.Knots[span + level] - t;
            double saved = 0;
            for (int row = 0; row < level; row++)
            {
                ndu[level * width + row] = right[row + 1] + left[level - row];
                double temp = ndu[row * width + (level - 1)] / ndu[level * width + row];
                ndu[row * width + level] = saved + right[row + 1] * temp;
                saved = left[level - row] * temp;
            }
            ndu[level * width + level] = saved;
        }
        double x = 0;
        for (int column = 0; column <= degree; column++)
            x += ndu[column * width + degree] * curve.Points[span - degree + column][0];
        return x;
    }

    private static int FindSpan(double[] knots, int degree, int count, double t)
    {
        if (t >= knots[count]) return count - 1;
        int low = degree, high = count, mid = (low + high) / 2;
        while (t < knots[mid] || t >= knots[mid + 1])
        {
            if (t < knots[mid]) high = mid;
            else low = mid;
            mid = (low + high) / 2;
        }
        return mid;
    }
}

public static class Placement
{
    /// <summary>ADR-0010 FoilDSL §6 rule version. The run manifest records this as <c>placementRule</c>.</summary>
    public const string PlacementRuleVersion = "foildsl-6/1";

    internal static int ChannelEvaluations;
    internal static int ProfileEvaluations;
    internal static Action? AfterStation;

    internal static void ResetEvaluatorCounts()
    {
        Interlocked.Exchange(ref ChannelEvaluations, 0);
        Interlocked.Exchange(ref ProfileEvaluations, 0);
    }

    public static SurfaceView Surface(byte[] source, string basis, long generation, CancellationToken cancellation, int stations = 41, int chordSamples = 101)
    {
        var (parsed, definition) = RequireFoil(source);
        Guard.Require(stations >= 2 && chordSamples >= 2, "DSL-RANGE");
        double[] xs = ChordSamples(chordSamples);
        var prepared = new PreparedProfile[definition.Profiles.Length];
        for (int profile = 0; profile < prepared.Length; profile++)
            prepared[profile] = Prepare(definition.Profiles[profile], xs);
        double[] stationEtas = definition.Assignments.Select(item => item.Eta).ToArray();
        int[] stationProfiles = definition.Assignments.Select(item => item.Profile).ToArray();
        var etas = MeshEtas(stationEtas, stations);
        var sections = new List<PlacedSection>(etas.Count);
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;
        for (int index = 0; index < etas.Count; index++)
        {
            if (index > 0) cancellation.ThrowIfCancellationRequested();
            var section = PlaceStation(definition, prepared, stationEtas, stationProfiles, etas[index], xs);
            sections.Add(section);
            Bound(section, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
            AfterStation?.Invoke();
        }
        return new(parsed.SourceHash, basis, generation, minX, minY, minZ, maxX, maxY, maxZ, sections);
    }

    /// <summary>
    /// The section at each η, sampled at the chord stations <paramref name="xs"/>; parses the source once.
    /// Camber and thickness are the Section/Blend outputs. Placed camber is PlacementRule.Place of that camber.
    /// Camber slope is the analytic jet dz/dx, never a difference of samples.
    /// </summary>
    public static IReadOnlyList<SectionSample> Sections(byte[] source, IReadOnlyList<double> etas, IReadOnlyList<double> xs,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var (_, definition) = RequireFoil(source);
        Guard.Require(etas.Count > 0 && xs.Count > 0, "DSL-RANGE");
        var abscissa = new double[xs.Count];
        for (int index = 0; index < xs.Count; index++)
        {
            double x = xs[index];
            Guard.Require(double.IsFinite(x) && x >= 0 && x <= 1, "DSL-RANGE");
            abscissa[index] = x;
        }
        foreach (double eta in etas)
            Guard.Require(double.IsFinite(eta) && eta >= 0 && eta <= 1, "DSL-RANGE");

        var prepared = new PreparedProfile[definition.Profiles.Length];
        for (int profile = 0; profile < prepared.Length; profile++)
            prepared[profile] = Prepare(definition.Profiles[profile], abscissa);
        double[] stationEtas = definition.Assignments.Select(item => item.Eta).ToArray();
        int[] stationProfiles = definition.Assignments.Select(item => item.Profile).ToArray();
        var samples = new SectionSample[etas.Count];
        for (int index = 0; index < etas.Count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            samples[index] = SampleSection(definition, prepared, stationEtas, stationProfiles, etas[index], abscissa);
        }
        return samples;
    }

    public static StationFrame Frame(byte[] source, double eta)
    {
        var (_, definition) = RequireFoil(source);
        Guard.Require(double.IsFinite(eta) && eta >= 0 && eta <= 1, "DSL-RANGE");
        return ReadFrame(definition, eta);
    }

    internal static double ProfileDifferenceMaximum(Curve upper, Curve lower) => DifferenceMaximum(upper, lower);

    private static StationFrame ReadFrame(Definition definition, double eta) => new(eta, eta * definition.HalfSpan,
        ChannelEvaluator.At(definition.Curves["leading"], eta),
        ChannelEvaluator.At(definition.Curves["trailing"], eta),
        ChannelEvaluator.At(definition.Curves["dihedral"], eta),
        ChannelEvaluator.At(definition.Curves["twist"], eta),
        ChannelEvaluator.At(definition.Curves["thickness"], eta));

    private readonly struct StationGeometry
    {
        internal StationFrame Frame { get; init; }
        internal (Binary64 Sin, Binary64 Cos) Angle { get; init; }
        internal int Left { get; init; }
        internal int Right { get; init; }
        internal Binary64 Weight { get; init; }
        internal Binary64 Reciprocal { get; init; }
        internal Binary64 Thickness { get; init; }
        internal int? Assignment { get; init; }
    }

    private static StationGeometry ReadStation(Definition definition, PreparedProfile[] prepared, double[] stationEtas, int[] stationProfiles, double eta)
    {
        var frame = ReadFrame(definition, eta);
        var angle = Binary64.SinCos(Binary64.Radians(Binary64.Point(frame.TwistDegrees)));
        var (left, right) = PlacementRule.Select(stationEtas, stationProfiles, eta, (a, b) => SameRecord(definition.Profiles[a], definition.Profiles[b]));
        int? assignment = null;
        for (int index = 0; index < stationEtas.Length; index++)
            if (stationEtas[index] == eta) assignment = index;
        Binary64 thickness = Binary64.Point(frame.ThicknessRatio);
        Binary64 weight = default;
        Binary64 reciprocal;
        if (right < 0)
            reciprocal = Binary64.Point(1 / prepared[left].Maximum);
        else
        {
            int bracket = PlacementRule.Bracket(stationEtas, eta);
            weight = Binary64.BlendWeight(eta, stationEtas[bracket], stationEtas[bracket + 1]);
            reciprocal = Binary64.Point(1 / BlendedMaximum(prepared[left], prepared[right], weight.Value));
        }
        return new()
        {
            Frame = frame, Angle = angle, Left = left, Right = right, Weight = weight, Reciprocal = reciprocal,
            Thickness = thickness, Assignment = right < 0 ? assignment : null,
        };
    }

    private static (Binary64 Zu, Binary64 Zl) OrdinatesAt(StationGeometry station, PreparedProfile[] prepared, int sample)
    {
        if (station.Right < 0)
        {
            var profile = prepared[station.Left];
            return PlacementRule.Section(Binary64.Point(profile.Upper[sample]), Binary64.Point(profile.Lower[sample]), station.Reciprocal, station.Thickness);
        }
        var a = prepared[station.Left];
        var b = prepared[station.Right];
        var componentsA = PlacementRule.Components(Binary64.Point(a.Upper[sample]), Binary64.Point(a.Lower[sample]), Binary64.Point(1 / a.Maximum));
        var componentsB = PlacementRule.Components(Binary64.Point(b.Upper[sample]), Binary64.Point(b.Lower[sample]), Binary64.Point(1 / b.Maximum));
        return PlacementRule.Blend(componentsA, componentsB, station.Weight, station.Reciprocal, station.Thickness);
    }

    private static PlacedSection PlaceStation(Definition definition, PreparedProfile[] prepared, double[] stationEtas, int[] stationProfiles, double eta, double[] xs)
    {
        var station = ReadStation(definition, prepared, stationEtas, stationProfiles, eta);
        var upper = new Point3[xs.Length];
        var lower = new Point3[xs.Length];
        Binary64 leading = Binary64.Point(station.Frame.LeadingMeters);
        Binary64 trailing = Binary64.Point(station.Frame.TrailingMeters);
        Binary64 elevation = Binary64.Point(station.Frame.ElevationMeters);
        double y = station.Frame.SpanMeters;
        for (int sample = 0; sample < xs.Length; sample++)
        {
            var (zu, zl) = OrdinatesAt(station, prepared, sample);
            upper[sample] = Place(leading, trailing, elevation, station.Angle, xs[sample], zu, y);
            lower[sample] = Place(leading, trailing, elevation, station.Angle, xs[sample], zl, y);
        }
        return new(eta, station.Assignment, upper, lower);
    }

    private static SectionSample SampleSection(Definition definition, PreparedProfile[] prepared, double[] stationEtas, int[] stationProfiles, double eta, double[] xs)
    {
        var station = ReadStation(definition, prepared, stationEtas, stationProfiles, eta);
        var camber = new double[xs.Length];
        var thickness = new double[xs.Length];
        var slope = new double[xs.Length];
        var placed = new Point3[xs.Length];
        Binary64 leading = Binary64.Point(station.Frame.LeadingMeters);
        Binary64 trailing = Binary64.Point(station.Frame.TrailingMeters);
        Binary64 elevation = Binary64.Point(station.Frame.ElevationMeters);
        double y = station.Frame.SpanMeters;
        for (int sample = 0; sample < xs.Length; sample++)
        {
            var (zu, zl) = OrdinatesAt(station, prepared, sample);
            Binary64 mean = (zu + zl) * Binary64.Half;
            camber[sample] = mean.Value;
            thickness[sample] = (zu - zl).Value;
            slope[sample] = CamberSlope(station, prepared, xs[sample]);
            // Place of the averaged camber is not the drawn midline's bits (example.foil). The midline is
            // the average of the two Place results, the same calls Surface makes.
            Point3 upper = Place(leading, trailing, elevation, station.Angle, xs[sample], zu, y);
            Point3 lower = Place(leading, trailing, elevation, station.Angle, xs[sample], zl, y);
            placed[sample] = new((upper.X + lower.X) / 2, (upper.Y + lower.Y) / 2, (upper.Z + lower.Z) / 2);
        }
        return new(station.Frame, xs, camber, thickness, slope, placed);
    }

    // dz_c/dx of the Section/Blend camber. Shared abscissa uses one parameter, so the slope is
    // (Yt_upper + Yt_lower) / (Xt_upper + Xt_lower). A zero numerator is a flat camber derivative,
    // including the symmetric vertical nose where both Xt are zero.
    private static double CamberSlope(StationGeometry station, PreparedProfile[] prepared, double x)
    {
        double left = MeanCamberSlope(prepared[station.Left], x);
        if (station.Right < 0) return left;
        double right = MeanCamberSlope(prepared[station.Right], x);
        double weight = station.Weight.Value;
        return (1d - weight) * left + weight * right;
    }

    private static double MeanCamberSlope(PreparedProfile profile, double x)
    {
        if (SameAbscissa(profile.UpperCurve, profile.LowerCurve))
        {
            double t = ProfileEvaluator.ParameterFor(profile.UpperCurve, x);
            var upper = ProfileEvaluator.Jet(profile.UpperCurve, t);
            var lower = ProfileEvaluator.Jet(profile.LowerCurve, t);
            double numerator = upper.Yt + lower.Yt;
            if (numerator == 0) return 0;
            return numerator / (upper.Xt + lower.Xt);
        }
        return (OrdinateSlope(profile.UpperCurve, x) + OrdinateSlope(profile.LowerCurve, x)) * 0.5;
    }

    private static double OrdinateSlope(Curve curve, double x)
    {
        var jet = ProfileEvaluator.Jet(curve, ProfileEvaluator.ParameterFor(curve, x));
        if (jet.Yt == 0) return 0;
        return jet.Yt / jet.Xt;
    }

    private static Point3 Place(Binary64 leading, Binary64 trailing, Binary64 elevation, (Binary64 Sin, Binary64 Cos) angle, double x, Binary64 z, double y)
    {
        var (placedX, placedZ) = PlacementRule.Place(leading, trailing, elevation, angle, Binary64.Point(x), z);
        return new(placedX.Value, y, placedZ.Value);
    }

    private static void Bound(PlacedSection section, ref double minX, ref double minY, ref double minZ, ref double maxX, ref double maxY, ref double maxZ)
    {
        foreach (var point in section.Upper.Concat(section.Lower))
        {
            minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y);
            minZ = Math.Min(minZ, point.Z); maxZ = Math.Max(maxZ, point.Z);
        }
    }

    private sealed class PreparedProfile
    {
        internal required double[] Upper { get; init; }
        internal required double[] Lower { get; init; }
        internal required double Maximum { get; init; }
        internal required Curve UpperCurve { get; init; }
        internal required Curve LowerCurve { get; init; }
    }

    private static PreparedProfile Prepare(ProfileDefinition profile, double[] xs)
    {
        var upper = new double[xs.Length];
        var lower = new double[xs.Length];
        bool shared = SameAbscissa(profile.Upper, profile.Lower);
        for (int index = 0; index < xs.Length; index++)
        {
            if (!shared)
            {
                upper[index] = ProfileEvaluator.OrdinateAt(profile.Upper, xs[index]);
                lower[index] = ProfileEvaluator.OrdinateAt(profile.Lower, xs[index]);
                continue;
            }
            double t = ProfileEvaluator.ParameterFor(profile.Upper, xs[index]);
            upper[index] = ProfileEvaluator.Jet(profile.Upper, t).Y;
            lower[index] = ProfileEvaluator.Jet(profile.Lower, t).Y;
        }
        return new()
        {
            Upper = upper, Lower = lower, Maximum = DifferenceMaximum(profile.Upper, profile.Lower),
            UpperCurve = profile.Upper, LowerCurve = profile.Lower,
        };
    }

    private static double DifferenceMaximum(Curve upper, Curve lower)
    {
        if (SameAbscissa(upper, lower)) return ParameterMaximum(upper, lower);
        var knots = KnotImages(upper).Concat(KnotImages(lower)).ToArray();
        return Maximize(x => ProfileEvaluator.OrdinateAt(upper, x) - ProfileEvaluator.OrdinateAt(lower, x), x => ThicknessDerivative(upper, lower, x), knots);
    }

    // Same abscissa: the thickness maximum is the maximum of y_upper(t) - y_lower(t). A knot
    // parameter is a candidate so a C0 peak that sits on a knot is not missed.
    private static double ParameterMaximum(Curve upper, Curve lower) => Maximize(
        t => ProfileEvaluator.Jet(upper, t).Y - ProfileEvaluator.Jet(lower, t).Y,
        t =>
        {
            var high = ProfileEvaluator.Jet(upper, t);
            var low = ProfileEvaluator.Jet(lower, t);
            return (high.Yt - low.Yt, high.Ytt - low.Ytt);
        },
        upper.Knots);

    private static double BlendedMaximum(PreparedProfile a, PreparedProfile b, double weight)
    {
        double complement = 1 - weight;
        var knots = KnotImages(a.UpperCurve).Concat(KnotImages(a.LowerCurve)).Concat(KnotImages(b.UpperCurve)).Concat(KnotImages(b.LowerCurve)).ToArray();
        return Maximize(
            x => complement * (ProfileEvaluator.OrdinateAt(a.UpperCurve, x) - ProfileEvaluator.OrdinateAt(a.LowerCurve, x)) / a.Maximum
                + weight * (ProfileEvaluator.OrdinateAt(b.UpperCurve, x) - ProfileEvaluator.OrdinateAt(b.LowerCurve, x)) / b.Maximum,
            x =>
            {
                var da = ThicknessDerivative(a.UpperCurve, a.LowerCurve, x);
                var db = ThicknessDerivative(b.UpperCurve, b.LowerCurve, x);
                return (complement * da.D1 / a.Maximum + weight * db.D1 / b.Maximum,
                    complement * da.D2 / a.Maximum + weight * db.D2 / b.Maximum);
            }, knots);
    }

    // Every grid local maximum and every grid interval where the slope turns from positive to negative
    // is polished, and the best result wins. Two humps within one grid step of each other then cannot
    // leave the maximum on the lower one.
    private static double Maximize(Func<double, double> value, Func<double, (double D1, double D2)> derivative, IReadOnlyList<double> knots)
    {
        int count = PlacementRule.MaximumGrid;
        double step = count - 1;
        var samples = new double[count];
        var slopes = new double[count];
        double best = double.NegativeInfinity;
        for (int index = 0; index < count; index++)
        {
            double x = index / step;
            samples[index] = value(x);
            slopes[index] = derivative(x).D1;
            best = Math.Max(best, samples[index]);
        }
        foreach (double knot in knots)
        {
            if (knot < 0 || knot > 1 || double.IsNaN(knot)) continue;
            best = Math.Max(best, value(knot));
        }
        for (int index = 0; index < count; index++)
        {
            bool rises = index == 0 || samples[index] > samples[index - 1];
            bool holds = index == count - 1 || samples[index] >= samples[index + 1];
            if (rises && holds || index == 0 && holds)
                best = Math.Max(best, Polish(value, derivative, Math.Max(0, (index - 1) / step), Math.Min(1, (index + 1) / step), index / step));
            if (index + 1 < count && slopes[index] > 0 && slopes[index + 1] < 0)
                best = Math.Max(best, Polish(value, derivative, index / step, (index + 1) / step, (index + 0.5) / step));
        }
        return best;
    }

    // Safeguarded Newton on the slope inside a bracket: a Newton step that leaves the bracket is replaced by
    // bisection, and the sign of the slope shrinks the bracket on every iteration.
    private static double Polish(Func<double, double> value, Func<double, (double D1, double D2)> derivative, double left, double right, double start)
    {
        double x = start;
        double best = value(x);
        for (int iter = 0; iter < 40 && left < right; iter++)
        {
            var (d1, d2) = derivative(x);
            double newton = d2 == 0 || !double.IsFinite(d1) || !double.IsFinite(d2) ? double.NaN : x - d1 / d2;
            double next = newton > left && newton < right ? newton : (left + right) / 2;
            if (d1 > 0) left = x;
            else if (d1 < 0) right = x;
            if (next == x) break;
            x = next;
        }
        return Math.Max(best, value(Math.Clamp(x, 0, 1)));
    }

    private static (double D1, double D2) ThicknessDerivative(Curve upper, Curve lower, double x)
    {
        var du = Slope(upper, x);
        var dl = Slope(lower, x);
        return (du.D1 - dl.D1, du.D2 - dl.D2);
    }

    private static (double D1, double D2) Slope(Curve curve, double x)
    {
        var jet = ProfileEvaluator.Jet(curve, ProfileEvaluator.ParameterFor(curve, x));
        if (Math.Abs(jet.Xt) < 1e-18) return (0, 0);
        double d1 = jet.Yt / jet.Xt;
        double d2 = (jet.Ytt * jet.Xt - jet.Yt * jet.Xtt) / (jet.Xt * jet.Xt * jet.Xt);
        return (d1, d2);
    }

    private static bool SameAbscissa(Curve left, Curve right)
    {
        if (left.Degree != right.Degree || left.Knots.Length != right.Knots.Length || left.Points.Length != right.Points.Length) return false;
        for (int index = 0; index < left.Knots.Length; index++) if (left.Knots[index] != right.Knots[index]) return false;
        for (int index = 0; index < left.Points.Length; index++) if (left.Points[index][0] != right.Points[index][0]) return false;
        return true;
    }

    private static IEnumerable<double> KnotImages(Curve curve)
    {
        var seen = new HashSet<double>();
        foreach (double knot in curve.Knots)
        {
            if (knot < 0 || knot > 1 || !seen.Add(knot)) continue;
            yield return ProfileEvaluator.Jet(curve, knot).X;
        }
    }

    private static bool SameRecord(ProfileDefinition left, ProfileDefinition right) =>
        ReferenceEquals(left, right) || SameCurve(left.Upper, right.Upper) && SameCurve(left.Lower, right.Lower);

    // Curve records, not certificate spans: identical control data selects one profile.
    private static bool SameCurve(Curve left, Curve right)
    {
        if (left.Degree != right.Degree || left.Knots.Length != right.Knots.Length || left.Points.Length != right.Points.Length) return false;
        for (int index = 0; index < left.Knots.Length; index++) if (left.Knots[index] != right.Knots[index]) return false;
        for (int index = 0; index < left.Points.Length; index++)
            if (left.Points[index][0] != right.Points[index][0] || left.Points[index][1] != right.Points[index][1]) return false;
        return true;
    }

    private static List<double> MeshEtas(double[] authored, int stations)
    {
        var etas = new List<double>(stations + authored.Length);
        double step = stations - 1;
        for (int index = 0; index < stations; index++) etas.Add(index / step);
        etas.AddRange(authored);
        etas.Sort();
        var unique = new List<double>(etas.Count);
        foreach (double eta in etas)
            if (unique.Count == 0 || eta != unique[^1]) unique.Add(eta);
        return unique;
    }

    private static double[] ChordSamples(int count)
    {
        var xs = new double[count];
        double step = count - 1;
        for (int index = 0; index < count; index++)
            xs[index] = (1 - Math.Cos(Math.PI * index / step)) / 2;
        xs[0] = 0;
        xs[^1] = 1;
        return xs;
    }

    private static (SourceParse Parsed, Definition Definition) RequireFoil(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" } definition)
            throw new ContractError(parsed.Diagnostics.Count > 0 ? parsed.Diagnostics[0].Code : "DSL-PATCH");
        return (parsed, definition);
    }
}
