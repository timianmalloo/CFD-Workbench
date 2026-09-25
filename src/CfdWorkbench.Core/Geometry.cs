using System.Diagnostics;
using System.Numerics;

namespace CfdWorkbench.Core;

public enum GeometryStatus { Invalid, Unsupported, NotAssessed, Certified }

public sealed record ExactRatio(string Numerator, string Denominator);
public sealed record GeometryWitness(string Curve, ExactRatio DomainStart, ExactRatio DomainEnd,
    ExactRatio AbscissaDifferenceLower, ExactRatio AbscissaDifferenceUpper, ExactRatio OrdinateLower, ExactRatio OrdinateUpper);
public sealed record EnclosedOrdinate(double Lower, double Upper);
public sealed record SectionEnclosure(EnclosedOrdinate Upper, EnclosedOrdinate Lower);
public sealed record PlacedPointEnclosure(EnclosedOrdinate X, EnclosedOrdinate Y, EnclosedOrdinate Z);
public sealed record QuerySpanBound(string Curve, int Span, int Degree, int CommonDenominatorBits, int RefinedNumeratorBits, int RefinedDenominatorBits);
public sealed record QueryFeasibilityWitness(string Algorithm, int RationalBitLimit, int MaximumIntermediateBits, long RationalOperationsUpper,
    int InverseDepth, int AngleGridBits, IReadOnlyList<QuerySpanBound> Spans, string MaximumBitPath);

/// <summary>Authority-produced enclosure; source parsing alone cannot construct it.</summary>
public sealed class GeometryCertificate
{
    internal GeometryCertificate(string sourceHash, string surfaceHash, Rational lower, Rational upper,
        IEnumerable<GeometryWitness> witnesses, int nodes, Dictionary<string, PolynomialSpan[]> spans, double halfSpan,
        Rational placementWidth, QueryFeasibilityWitness feasibility, CertifiedProfile[] profiles, BlendStation[] stations, int blendNodeBudget)
    {
        SourceHash = sourceHash; SurfaceHash = surfaceHash;
        ThicknessMaximumLower = lower.Down(); ThicknessMaximumUpper = upper.Up();
        ExactThicknessMaximumLower = lower.Exact; ExactThicknessMaximumUpper = upper.Exact;
        NormalizationRelativeErrorUpper = ((upper - lower) / lower).Up();
        Witnesses = Array.AsReadOnly(witnesses.ToArray()); SubdivisionNodes = nodes;
        Spans = spans;
        HalfSpan = Rational.From(halfSpan);
        ExactPlacementWidthUpper = placementWidth.Exact; PlacementWidthUpper = placementWidth.Up();
        QueryFeasibility = feasibility;
        Profiles = profiles; Stations = stations; BlendNodeBudget = blendNodeBudget;
    }
    public string AlgorithmVersion => "cfdw-rational-bernstein-subset-1";
    public string ProofScope => "Continuous source-shape proof with pointwise interval evaluation; no tessellation/export certificate";
    public string SourceHash { get; }
    public string SurfaceHash { get; }
    public double ThicknessMaximumLower { get; }
    public double ThicknessMaximumUpper { get; }
    public ExactRatio ExactThicknessMaximumLower { get; }
    public ExactRatio ExactThicknessMaximumUpper { get; }
    public double NormalizationRelativeErrorUpper { get; }
    public IReadOnlyList<GeometryWitness> Witnesses { get; }
    public int SubdivisionNodes { get; }
    public ExactRatio ExactPlacementWidthUpper { get; }
    public double PlacementWidthUpper { get; }
    public string PlacementDomain => "eta and normalized x in [0,1], either side, port or starboard";
    public QueryFeasibilityWitness QueryFeasibility { get; }
    internal Dictionary<string, PolynomialSpan[]> Spans { get; }
    internal Rational HalfSpan { get; }
    internal CertifiedProfile[] Profiles { get; }
    internal BlendStation[] Stations { get; }
    internal int BlendNodeBudget { get; }
}

internal sealed class CertifiedProfile
{
    internal CertifiedProfile(string upperPath, string lowerPath, RationalInterval maximum, PolynomialSpan[] difference, int nodes)
    {
        UpperPath = upperPath; LowerPath = lowerPath; Maximum = maximum; Difference = difference; Nodes = nodes;
    }
    internal string UpperPath { get; }
    internal string LowerPath { get; }
    internal RationalInterval Maximum { get; }
    internal PolynomialSpan[] Difference { get; }
    internal int Nodes { get; }
}

internal readonly record struct BlendStation(Rational Eta, int Profile);

public sealed class GeometryAssessment
{
    internal GeometryAssessment(GeometryCertificate? certificate, string reason, GeometryStatus status = GeometryStatus.NotAssessed, string code = "DSL-GEOMETRY")
    { Certificate = certificate; Reason = reason; Status = certificate is null ? status : GeometryStatus.Certified; Code = certificate is null ? code : "GEOMETRY-CERTIFIED"; }
    public GeometryStatus Status { get; }
    public GeometryCertificate? Certificate { get; }
    public string Code { get; }
    public string Reason { get; }
}

public static class Geometry
{
    public static PlacedPointEnclosure PointAt(GeometryCertificate certificate, double eta, double x, bool upper, bool port = false,
        TimeSpan? timeBudget = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        Domain(eta, x);
        var watch = new ProofBudget(timeBudget, cancellationToken);
        try
        {
            var section = SectionExact(certificate, eta, x, watch);
            var z = upper ? section.Upper : section.Lower;
            var station = Rational.From(eta);
            var leading = Bernstein.EncloseAt(certificate.Spans["leading"], station, watch);
            var trailing = Bernstein.EncloseAt(certificate.Spans["trailing"], station, watch);
            var elevation = Bernstein.EncloseAt(certificate.Spans["dihedral"], station, watch);
            var degrees = Bernstein.EncloseAt(certificate.Spans["twist"], station, watch);
            var angular = degrees * RationalInterval.Point(Rational.From(0.017453292519943295));
            // Monotone nearest/ties-even conversion encloses the specified once-rounded angle.
            angular = new(Rational.From(angular.Lower.Nearest()), Rational.From(angular.Upper.Nearest()));
            // Enclose, never replace, the once-rounded evaluator angle. This fixed
            // outward grid bounds Taylor arithmetic even for subnormal angles.
            angular = new(angular.Lower.DyadicDown(64), angular.Upper.DyadicUp(64));
            var (sin, cos) = Trigonometry(angular);
            var chord = trailing - leading;
            var abscissa = RationalInterval.Point(Rational.From(x));
            var placedX = leading + chord * (abscissa * cos + z * sin);
            var placedZ = elevation + chord * (z * cos - abscissa * sin);
            var y = certificate.HalfSpan * station * (port ? -1 : 1);
            var result = new PlacedPointEnclosure(placedX.Outward(), RationalInterval.Point(y).Outward(), placedZ.Outward());
            Require(new[] { result.X, result.Y, result.Z }.All(value => double.IsFinite(value.Lower) && double.IsFinite(value.Upper) &&
                value.Upper - value.Lower <= 1e-8), "Placed point error exceeds 10 nm.");
            watch.Check();
            return result;
        }
        catch (ProofRefusal failure) { throw new ContractError(failure.Code is "GEOMETRY-BUDGET" or "GEOMETRY-CANCELLED" ? failure.Code : "GEOMETRY-CERTIFICATE-DEFECT"); }
    }

    public static SectionEnclosure SectionAt(GeometryCertificate certificate, double eta, double x,
        TimeSpan? timeBudget = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        Domain(eta, x);
        var watch = new ProofBudget(timeBudget, cancellationToken);
        try
        {
            var section = SectionExact(certificate, eta, x, watch);
            var result = new SectionEnclosure(section.Upper.Outward(), section.Lower.Outward());
            watch.Check();
            return result;
        }
        catch (ProofRefusal failure) { throw new ContractError(failure.Code is "GEOMETRY-BUDGET" or "GEOMETRY-CANCELLED" ? failure.Code : "GEOMETRY-CERTIFICATE-DEFECT"); }
    }

    private static void Domain(double eta, double x) => Guard.Require(double.IsFinite(eta) && double.IsFinite(x) &&
        eta >= 0 && eta <= 1 && x >= 0 && x <= 1, "DSL-RANGE");

    private static (RationalInterval Upper, RationalInterval Lower) SectionExact(GeometryCertificate certificate, double eta, double x, ProofBudget watch)
    {
        var (left, right, weight) = SelectBlend(certificate, eta);
        if (right < 0) return ProfileSection(certificate, left, eta, x, watch);
        var a = ProfileComponents(certificate, left, x, watch);
        var b = ProfileComponents(certificate, right, x, watch);
        var share = RationalInterval.Point(weight);
        var complement = RationalInterval.Point(1 - weight);
        var camber = complement * a.Camber + share * b.Camber;
        var unnormalized = complement * a.UnitThickness + share * b.UnitThickness;
        // Scales share one uncertain maximum per profile, so the unit-shape
        // family is the segment between the reciprocal endpoints, not a box.
        var maximum = EncloseMaximumT0(certificate, left, right, weight, watch);
        Geometry.Require(maximum.Lower > (Rational)1 / 4 && maximum.Upper >= maximum.Lower, "Blend maximum enclosure is not certified.");
        var thickness = Bernstein.EncloseAt(certificate.Spans["thickness"], Rational.From(eta), watch);
        var half = RationalInterval.Point((Rational)1 / 2);
        var normalizedHalfThickness = unnormalized * maximum.Reciprocal() * thickness * half;
        return (camber + normalizedHalfThickness, camber - normalizedHalfThickness);
    }

    private static (int Left, int Right, Rational Weight) SelectBlend(GeometryCertificate certificate, double eta)
    {
        var station = Rational.From(eta);
        var stations = certificate.Stations;
        int index = 0;
        while (index + 1 < stations.Length && stations[index + 1].Eta.CompareTo(station) < 0) index++;
        var left = stations[index];
        var right = stations[index + 1];
        if (station.CompareTo(left.Eta) == 0 || SameGeometry(certificate, left.Profile, right.Profile)) return (left.Profile, -1, 0);
        if (station.CompareTo(right.Eta) == 0) return (right.Profile, -1, 0);
        return (left.Profile, right.Profile, (station - left.Eta) / (right.Eta - left.Eta));
    }

    private static (RationalInterval Upper, RationalInterval Lower) ProfileSection(GeometryCertificate certificate, int index, double eta, double x, ProofBudget watch)
    {
        var profile = certificate.Profiles[index];
        var profileTolerance = ProfileTolerance(profile.Maximum.Lower);
        var upper = Bernstein.EncloseAt(certificate.Spans[profile.UpperPath], Rational.From(x), watch, profileTolerance);
        var lower = Bernstein.EncloseAt(certificate.Spans[profile.LowerPath], Rational.From(x), watch, profileTolerance);
        var thickness = Bernstein.EncloseAt(certificate.Spans["thickness"], Rational.From(eta), watch);
        var half = RationalInterval.Point((Rational)1 / 2);
        var camber = (upper + lower) * half;
        // Every exact maximum in the authority-produced enclosure is propagated.
        var normalizedHalfThickness = (upper - lower) * profile.Maximum.Reciprocal() * thickness * half;
        return (camber + normalizedHalfThickness, camber - normalizedHalfThickness);
    }

    private static (RationalInterval Camber, RationalInterval UnitThickness) ProfileComponents(GeometryCertificate certificate, int index, double x, ProofBudget watch)
    {
        var profile = certificate.Profiles[index];
        var profileTolerance = ProfileTolerance(profile.Maximum.Lower);
        var upper = Bernstein.EncloseAt(certificate.Spans[profile.UpperPath], Rational.From(x), watch, profileTolerance);
        var lower = Bernstein.EncloseAt(certificate.Spans[profile.LowerPath], Rational.From(x), watch, profileTolerance);
        var half = RationalInterval.Point((Rational)1 / 2);
        return ((upper + lower) * half, (upper - lower) * profile.Maximum.Reciprocal());
    }

    private static RationalInterval EncloseMaximumT0(GeometryCertificate certificate, int left, int right, Rational weight, ProofBudget watch)
    {
        var a = certificate.Profiles[left];
        var b = certificate.Profiles[right];
        var scaleA = RationalInterval.Point(1 - weight) * a.Maximum.Reciprocal();
        var scaleB = RationalInterval.Point(weight) * b.Maximum.Reciprocal();
        var low = Bernstein.Maximum(ScaleSum(a.Difference, b.Difference, scaleA.Lower, scaleB.Lower), watch, certificate.BlendNodeBudget);
        var high = Bernstein.Maximum(ScaleSum(a.Difference, b.Difference, scaleA.Upper, scaleB.Upper), watch, certificate.BlendNodeBudget);
        return new(low.Lower, high.Upper);
    }

    private static Rational[][] ScaleSum(PolynomialSpan[] left, PolynomialSpan[] right, Rational leftScale, Rational rightScale)
    {
        var result = new Rational[left.Length][];
        for (int span = 0; span < left.Length; span++)
        {
            var values = new Rational[left[span].Y.Length];
            for (int index = 0; index < values.Length; index++)
                values[index] = leftScale * left[span].Y[index] + rightScale * right[span].Y[index];
            result[span] = values;
        }
        return result;
    }

    private static bool SameGeometry(GeometryCertificate certificate, int left, int right) =>
        SameGeometry(certificate.Spans, certificate.Profiles[left], certificate.Profiles[right]);

    private static bool SameGeometry(Dictionary<string, PolynomialSpan[]> spans, CertifiedProfile left, CertifiedProfile right)
    {
        if (ReferenceEquals(left, right)) return true;
        return SameSpans(spans[left.UpperPath], spans[right.UpperPath]) && SameSpans(spans[left.LowerPath], spans[right.LowerPath]);
    }

    private static bool SameSpans(PolynomialSpan[] left, PolynomialSpan[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
        {
            if (!SameCoefficients(left[index].X, right[index].X) || !SameCoefficients(left[index].Y, right[index].Y)) return false;
            if (left[index].Start.CompareTo(right[index].Start) != 0 || left[index].End.CompareTo(right[index].End) != 0) return false;
        }
        return true;
    }

    private static bool SharedAbscissa(PolynomialSpan[] left, PolynomialSpan[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index].Y.Length != right[index].Y.Length || !SameCoefficients(left[index].X, right[index].X)) return false;
            if (left[index].Start.CompareTo(right[index].Start) != 0 || left[index].End.CompareTo(right[index].End) != 0) return false;
        }
        return true;
    }

    private static bool SameCoefficients(Rational[] left, Rational[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++) if (left[index].CompareTo(right[index]) != 0) return false;
        return true;
    }

    private static (RationalInterval Sin, RationalInterval Cos) Trigonometry(RationalInterval angle)
    {
        Require(angle.Lower >= -1 && angle.Upper <= 1, "Angles beyond the current certified Taylor domain are not assessed.");
        Rational center = (angle.Lower + angle.Upper) / 2;
        Rational radius = (angle.Upper - angle.Lower) / 2;
        Rational sine = center, cosine = 1, sinTerm = center, cosTerm = 1;
        Rational square = center * center;
        for (int i = 1; i <= 16; i++)
        {
            cosTerm = cosTerm * (0 - square) / ((2 * i - 1) * 2 * i);
            sinTerm = sinTerm * (0 - square) / (2 * i * (2 * i + 1));
            cosine += cosTerm; sine += sinTerm;
        }
        Rational factorial = 1;
        for (int i = 2; i <= 32; i++) factorial *= i;
        // On [-1,1], 1/32! bounds either omitted Taylor tail. Both derivatives
        // have magnitude <=1, so the input interval adds at most its radius.
        Rational error = (Rational)1 / factorial + radius;
        return (new(sine - error, sine + error), new(cosine - error, cosine + error));
    }

    public static GeometryAssessment Assess(SourceParse source, TimeSpan? timeBudget = null)
    {
        var definition = source.Definition;
        if (definition is null) return new(null, "Static parsing did not succeed.");
        var watch = new ProofBudget(timeBudget);
        try
        {
            watch.Check();
            Require(definition.Kind == "foil" && definition.Tip == "open" && definition.Profiles.Length >= 1 &&
                definition.Assertions.Length == 0,
                "Only an open tip and no assertions are currently certified.", GeometryStatus.Unsupported);
            Require(definition.Curves.Values.All(curve => !curve.MissingIds), "Accept an explicit ID candidate first.", GeometryStatus.Unsupported);
            Require(definition.Locks.All(item => item.Kind == "root_mirror"), "Other lock types are not assessed.", GeometryStatus.Unsupported, "DSL-LOCK");
            foreach (var item in definition.Locks)
            {
                var curve = definition.Curves[item.Channel.Text];
                Require(curve.Points[0][1] == curve.Points[1][1], "Root tangent lock is not satisfied.", GeometryStatus.Invalid, "DSL-LOCK");
            }
            Require(definition.Curves["leading"].Points[0][1] == 0 && definition.Curves["dihedral"].Points[0][1] == 0,
                "Root leading edge and elevation must be zero.", GeometryStatus.Invalid);
            foreach (var profile in definition.Profiles)
            {
                Require(profile.Upper.Degree == profile.Lower.Degree && profile.Upper.Knots.SequenceEqual(profile.Lower.Knots) &&
                    profile.Upper.Points.Select(p => p[0]).SequenceEqual(profile.Lower.Points.Select(p => p[0])),
                    "Independent profile x mappings are not assessed.", GeometryStatus.Unsupported);
                Require(profile.Upper.Points[0][1] == 0 && profile.Lower.Points[0][1] == 0,
                    "Profile leading endpoints must meet at zero.", GeometryStatus.Invalid);
                Require(profile.Closure != "closed" || profile.Upper.Points[^1][1] == 0 && profile.Lower.Points[^1][1] == 0,
                    "Closed trailing endpoints must meet at zero.", GeometryStatus.Invalid);
            }
            var spans = definition.Curves.ToDictionary(pair => pair.Key, pair => Bernstein.Spans(pair.Value, watch));
            foreach (var curve in spans.Values)
                foreach (var span in curve)
                {
                    var derivative = span.X.Zip(span.X.Skip(1), (a, b) => b - a).ToArray();
                    Require(derivative.All(x => x >= 0) && derivative.Any(x => x > 0), "Abscissa monotonicity is not certified.");
                }
            var leadingUpper = spans["leading"].SelectMany(s => s.Y).Max();
            var trailingLower = spans["trailing"].SelectMany(s => s.Y).Min();
            Require(spans["trailing"].SelectMany(s => s.Y).Max() > spans["leading"].SelectMany(s => s.Y).Min(),
                "Continuous chord is nonpositive.", GeometryStatus.Invalid);
            Require(trailingLower > leadingUpper, "Rail hulls do not certify strictly positive chord.");
            Require(spans["thickness"].SelectMany(s => s.Y).All(x => x > 0 && x < 1), "Thickness hull is not inside (0,1).");
            var certified = new CertifiedProfile[definition.Profiles.Length];
            for (int profileIndex = 0; profileIndex < definition.Profiles.Length; profileIndex++)
            {
                var profile = definition.Profiles[profileIndex];
                var upper = spans[profile.Upper.Path];
                var lower = spans[profile.Lower.Path];
                Require(upper.Length == lower.Length, "Independent profile x mappings are not assessed.", GeometryStatus.Unsupported);
                var differences = upper.Zip(lower, (u, l) => new PolynomialSpan(u.Start, u.End, u.X, u.Y.Zip(l.Y, (a, b) => a - b).ToArray())).ToArray();
                for (int index = 0; index < differences.Length; index++)
                {
                    var coefficients = differences[index].Y;
                    Require(coefficients.All(x => x >= 0) && coefficients.Any(x => x > 0), "Profile separation is not certified.");
                    Require(index == 0 || coefficients[0] > 0, "Profile sides touch at an interior knot.");
                }
                Require(profile.Closure == "closed" || differences[^1].Y[^1] > 0, "Open trailing endpoints are not separated.");
                var maximum = Bernstein.Maximum(differences.Select(span => span.Y).ToArray(), watch);
                certified[profileIndex] = new(profile.Upper.Path, profile.Lower.Path, new(maximum.Lower, maximum.Upper), differences, maximum.Nodes);
            }
            var stations = definition.Assignments.Select(item => new BlendStation(Rational.From(item.Eta), item.Profile)).ToArray();
            bool distinct = false;
            int spanCount = 1, degree = 5;
            for (int index = 0; index + 1 < stations.Length; index++)
            {
                var left = certified[stations[index].Profile];
                var right = certified[stations[index + 1].Profile];
                if (SameGeometry(spans, left, right)) continue;
                distinct = true;
                Require(SharedAbscissa(left.Difference, right.Difference),
                    "Rule A max_x enclosure is not certified for independent profile bases.", GeometryStatus.Unsupported);
                spanCount = Math.Max(spanCount, Math.Max(left.Difference.Length, right.Difference.Length));
                degree = left.Difference[0].Y.Length - 1;
            }
            const int blendNodes = 256;
            if (distinct)
            {
                Rational range = 0;
                foreach (var profile in certified)
                {
                    var coefficients = profile.Difference.SelectMany(span => span.Y).ToArray();
                    Rational unitRange = (coefficients.Max() - coefficients.Min()) / profile.Maximum.Lower;
                    if (unitRange > range) range = unitRange;
                }
                const int depth = 48;
                Require(spanCount * depth <= blendNodes, "Blend maximum enclosure node budget is insufficient.");
                // Degree times the unit-shape range bounds the derivative. After `depth`
                // bisections every subspan hull is inside the maximum tolerance.
                Require(new Rational(degree, 1) * range / new Rational(BigInteger.One << depth, 1) <= Rational.From(1e-12) / 4,
                    "Blend maximum enclosure depth bound is insufficient.");
            }
            int rootIndex = definition.Assignments[0].Profile;
            var root = certified[rootIndex];
            var rootProfile = definition.Profiles[rootIndex];
            var placementWidth = distinct
                ? BlendPlacementWidth(spans, certified, definition.HalfSpan, watch)
                : PlacementWidth(spans, rootProfile, root.Maximum.Lower, root.Maximum.Upper, definition.HalfSpan, watch);
            var feasibility = QueryFeasibility.Prove(spans, rootProfile, root.Maximum.Lower, root.Maximum.Upper, definition.HalfSpan, watch,
                distinct, distinct ? blendNodes : 0, distinct ? certified.Select(item => item.Maximum.Lower).ToArray() : null);
            var witnesses = spans.SelectMany(pair => pair.Value.Select(span => Witness(pair.Key, span))).ToList();
            foreach (var profile in certified)
                witnesses.AddRange(profile.Difference.Select(span => Witness("profile-separation", span)));
            var result = new GeometryAssessment(new(source.SourceHash, source.SurfaceHash!, root.Maximum.Lower, root.Maximum.Upper, witnesses,
                certified.Sum(item => item.Nodes), spans, definition.HalfSpan, placementWidth, feasibility,
                certified, stations, distinct ? blendNodes : 0), "Continuous conservative subset certified.");
            watch.Check();
            return result;
        }
        catch (ProofRefusal refusal) { return new(null, refusal.Message, refusal.Status, refusal.Code); }
    }

    internal static void Require(bool condition, string reason, GeometryStatus status = GeometryStatus.NotAssessed, string code = "DSL-GEOMETRY")
    { if (!condition) throw new ProofRefusal(reason, status, code); }

    private static GeometryWitness Witness(string path, PolynomialSpan span)
    {
        var differences = span.X.Zip(span.X.Skip(1), (a, b) => b - a).ToArray();
        return new(path, span.Start.Exact, span.End.Exact, differences.Min().Exact, differences.Max().Exact, span.Y.Min().Exact, span.Y.Max().Exact);
    }

    private static Rational ProfileTolerance(Rational maximumLower)
    {
        Rational delta = Rational.From(1e-14);
        return maximumLower < 1 ? delta * maximumLower : delta;
    }

    private static Rational PlacementWidth(Dictionary<string, PolynomialSpan[]> spans, ProfileDefinition profile,
        Rational maximumLower, Rational maximumUpper, double halfSpan, ProofBudget watch)
    {
        Rational delta = Rational.From(1e-14), profileDelta = ProfileTolerance(maximumLower);
        var factor = Rational.From(0.017453292519943295);
        var twists = spans["twist"].SelectMany(span => span.Y).ToArray();
        Require(Rational.From((twists.Min() * factor).Nearest()) >= -1 && Rational.From((twists.Max() * factor).Nearest()) <= 1,
            "Whole-domain angle hull is outside the certified Taylor domain.");
        foreach (var pair in spans)
        {
            Rational tolerance = pair.Key == profile.Upper.Path || pair.Key == profile.Lower.Path ? profileDelta : delta;
            foreach (var span in pair.Value)
            {
                watch.Check();
                // Degree*ordinate range bounds |dy/dt| on the unit span. After
                // 127 bisections its entire ordinate hull fits this tolerance.
                Require((span.Y.Length - 1) * (span.Y.Max() - span.Y.Min()) / new Rational(BigInteger.One << 127, 1) <= tolerance,
                    "Whole-domain inverse depth bound is insufficient.");
            }
        }
        // Interval product width <= width(A)*maxAbs(B) + width(B)*maxAbs(A).
        // The exact normalized thickness is <=1. Include both inverse ordinate
        // errors and uncertainty in the single common maximum denominator.
        Rational normalizedWidth = 2 * profileDelta / maximumLower +
            (maximumUpper + 2 * profileDelta) * (maximumUpper - maximumLower) / (maximumLower * maximumUpper);
        Rational zWidth = profileDelta + (normalizedWidth * (1 + delta) + delta * (1 + normalizedWidth)) / 2;
        var profileValues = spans[profile.Upper.Path].Concat(spans[profile.Lower.Path]).SelectMany(span => span.Y);
        Rational zMagnitude = profileValues.Select(Abs).Max() + profileDelta + (1 + normalizedWidth) * (1 + delta) / 2;
        Rational factorial = 1;
        for (int i = 2; i <= 32; i++) factorial *= i;
        Rational trigWidth = delta * factor + new Rational(1, BigInteger.One << 51) + new Rational(2, BigInteger.One << 64) + (Rational)2 / factorial;
        Rational componentWidth = trigWidth + zWidth * (1 + trigWidth) + zMagnitude * trigWidth;
        Rational componentMagnitude = (1 + zMagnitude) * (1 + trigWidth);
        Rational chordMagnitude = spans["trailing"].SelectMany(span => span.Y).Max() - spans["leading"].SelectMany(span => span.Y).Min();
        Rational width = delta + 2 * delta * componentMagnitude + chordMagnitude * componentWidth;
        Rational coordinateMagnitude = spans["leading"].Concat(spans["dihedral"]).SelectMany(span => span.Y).Select(Abs).Max() +
            chordMagnitude * componentMagnitude + Rational.From(halfSpan);
        // Two outward binary64 conversions, including the subnormal regime.
        width += (coordinateMagnitude + 1) / new Rational(BigInteger.One << 51, 1);
        // simplify: 10 nm is a conservative implementation policy, not a language
        // tolerance. Revisit only with an independently reviewed error policy.
        Require(width <= Rational.From(1e-8), "Whole-domain placed error exceeds the implementation budget.");
        watch.Check();
        return width;
    }

    private static Rational BlendPlacementWidth(Dictionary<string, PolynomialSpan[]> spans, CertifiedProfile[] profiles, double halfSpan, ProofBudget watch)
    {
        Rational delta = Rational.From(1e-14);
        var paths = new HashSet<string>(profiles.SelectMany(profile => new[] { profile.UpperPath, profile.LowerPath }), StringComparer.Ordinal);
        Rational worstProfileDelta = 0, worstNormalized = 0, relativeGap = 0;
        foreach (var profile in profiles)
        {
            Rational profileDelta = ProfileTolerance(profile.Maximum.Lower);
            if (profileDelta > worstProfileDelta) worstProfileDelta = profileDelta;
            Rational normalizedWidth = 2 * profileDelta / profile.Maximum.Lower +
                (profile.Maximum.Upper + 2 * profileDelta) * (profile.Maximum.Upper - profile.Maximum.Lower) / (profile.Maximum.Lower * profile.Maximum.Upper);
            if (normalizedWidth > worstNormalized) worstNormalized = normalizedWidth;
            Rational gap = (profile.Maximum.Upper - profile.Maximum.Lower) / profile.Maximum.Lower;
            if (gap > relativeGap) relativeGap = gap;
        }
        // A convex combination of unit shapes peaks at least at 1/2. The
        // enclosure floor stays at 1/4 after the scalar and subdivision gaps.
        Rational subdivision = Rational.From(1e-12);
        Rational maxWidth = (relativeGap + 2 * subdivision) * (1 + worstNormalized);
        Rational maxLower = (Rational)1 / 4;
        Require(maxWidth < maxLower, "Blend maximum enclosure consumes the certified floor.");
        Rational normalizedWidthBound = worstNormalized / maxLower + (1 + worstNormalized) * maxWidth / (maxLower * maxLower);
        var factor = Rational.From(0.017453292519943295);
        var twists = spans["twist"].SelectMany(span => span.Y).ToArray();
        Require(Rational.From((twists.Min() * factor).Nearest()) >= -1 && Rational.From((twists.Max() * factor).Nearest()) <= 1,
            "Whole-domain angle hull is outside the certified Taylor domain.");
        foreach (var pair in spans)
        {
            Rational tolerance = paths.Contains(pair.Key) ? worstProfileDelta : delta;
            foreach (var span in pair.Value)
            {
                watch.Check();
                Require((span.Y.Length - 1) * (span.Y.Max() - span.Y.Min()) / new Rational(BigInteger.One << 127, 1) <= tolerance,
                    "Whole-domain inverse depth bound is insufficient.");
            }
        }
        Rational zWidth = worstProfileDelta + (normalizedWidthBound * (1 + delta) + delta * (1 + normalizedWidthBound)) / 2;
        var profileValues = profiles.SelectMany(profile => spans[profile.UpperPath].Concat(spans[profile.LowerPath]).SelectMany(span => span.Y));
        Rational zMagnitude = profileValues.Select(Abs).Max() + worstProfileDelta + (1 + normalizedWidthBound) * (1 + delta) / 2;
        Rational factorial = 1;
        for (int i = 2; i <= 32; i++) factorial *= i;
        Rational trigWidth = delta * factor + new Rational(1, BigInteger.One << 51) + new Rational(2, BigInteger.One << 64) + (Rational)2 / factorial;
        Rational componentWidth = trigWidth + zWidth * (1 + trigWidth) + zMagnitude * trigWidth;
        Rational componentMagnitude = (1 + zMagnitude) * (1 + trigWidth);
        Rational chordMagnitude = spans["trailing"].SelectMany(span => span.Y).Max() - spans["leading"].SelectMany(span => span.Y).Min();
        Rational width = delta + 2 * delta * componentMagnitude + chordMagnitude * componentWidth;
        Rational coordinateMagnitude = spans["leading"].Concat(spans["dihedral"]).SelectMany(span => span.Y).Select(Abs).Max() +
            chordMagnitude * componentMagnitude + Rational.From(halfSpan);
        width += (coordinateMagnitude + 1) / new Rational(BigInteger.One << 51, 1);
        Require(width <= Rational.From(1e-8), "Whole-domain placed error exceeds the implementation budget.");
        watch.Check();
        return width;
    }
    private static Rational Abs(Rational value) => value < 0 ? 0 - value : value;
}

/// <summary>Admission-time abstract interpretation of every finite binary64 query path.</summary>
internal sealed class QueryFeasibility
{
    private readonly record struct Size(int N, int D);
    private int maximum;
    private string maximumPath = "binary64 input";
    private void Observe(int bits, string path)
    {
        if (bits > maximum) { maximum = bits; maximumPath = path; }
        Geometry.Require(bits <= 32768, "All-query arithmetic bound exceeds 32768 bits at " + path,
            GeometryStatus.NotAssessed, "GEOMETRY-QUERY-RESOURCE");
    }
    private Size Track(Size size, string path)
    {
        Observe(Math.Max(size.N, size.D), path);
        // Interval extrema compare every product with another product. Record
        // those uncancelled cross-products as well as constructor operands.
        Observe(size.N + size.D, path + "/comparison");
        return size;
    }
    private Size Add(Size a, Size b, string path) => Track(new(Math.Max(a.N + b.D, b.N + a.D) + 1, a.D + b.D), path);
    private Size Multiply(Size a, Size b, string path) => Track(new(a.N + b.N, a.D + b.D), path);
    private Size Divide(Size a, Size b, string path) => Track(new(a.N + b.D, a.D + b.N), path);
    private Size Actual(Rational value, string path) => Track(new(Bits(value.Numerator), Bits(value.Denominator)), path);
    private static int Bits(BigInteger value) => checked((int)BigInteger.Abs(value).GetBitLength() + 1);
    private static Size Union(Size a, Size b) => new(Math.Max(a.N, b.N), Math.Max(a.D, b.D));
    private void Conversion(Size value, string path)
    {
        // DecimalSi.Round aligns the ratio for exponent selection, then scales
        // by 52-e (normal) or 1074 (subnormal), divides and doubles remainder.
        // Positive alignment is <= D+1; negative alignment is <= N+1.
        Observe(Math.Max(value.N + 1075, value.N + value.D + 3), path + "/round-shift-divrem");
        // Outward conversion compares against a finite binary64 rational.
        Observe(Math.Max(value.N + 1075, value.D + 1025), path + "/outward-comparison");
    }
    internal static QueryFeasibilityWitness Prove(Dictionary<string, PolynomialSpan[]> spans, ProfileDefinition profile,
        Rational maximumLower, Rational maximumUpper, double halfSpan, ProofBudget watch, bool blend = false, int blendNodes = 0, Rational[]? profileMaxima = null)
    {
        var proof = new QueryFeasibility();
        var bounds = new Dictionary<string, Size>();
        var bases = new Dictionary<string, Size>();
        var witnesses = new List<QuerySpanBound>();
        long operations = 10000; // fixed interval placement, 16 Taylor steps, conversions and comparisons
        Size query = proof.Track(new(54, 1076), "finite-binary64-domain-input");
        var tolerance = Rational.From(1e-14);
        var profileTolerance = maximumLower < 1 ? tolerance * maximumLower : tolerance;
        foreach (var pair in spans)
        {
            Size combined = new(1, 1);
            for (int index = 0; index < pair.Value.Length; index++)
            {
                watch.Check();
                var span = pair.Value[index];
                BigInteger common = BigInteger.One;
                foreach (var coefficient in span.X.Concat(span.Y))
                {
                    proof.Observe(Bits(common) + Bits(coefficient.Denominator), pair.Key + "/lcm-product");
                    common = common / BigInteger.GreatestCommonDivisor(common, coefficient.Denominator) * coefficient.Denominator;
                }
                BigInteger magnitude = BigInteger.Zero;
                foreach (var coefficient in span.X.Concat(span.Y))
                {
                    proof.Observe(Bits(coefficient.Numerator) + Bits(common), pair.Key + "/common-numerator-product");
                    magnitude = BigInteger.Max(magnitude, BigInteger.Abs(coefficient.Numerator) * (common / coefficient.Denominator));
                }
                int degree = span.Y.Length - 1;
                var basis = new Size(Bits(magnitude), Bits(common));
                bases[pair.Key] = bases.TryGetValue(pair.Key, out var previous) ? Union(previous, basis) : basis;
                // Convex hull bounds numerator magnitude. Every split averages
                // at most degree times, so denominators divide D*2^(degree*d).
                var refined = proof.Track(new(Bits(magnitude) + degree * 128, Bits(common) + degree * 128), pair.Key + "/refined-hull");
                proof.Divide(proof.Add(refined, refined, pair.Key + "/split-add"), new(2, 1), pair.Key + "/split-half");
                proof.Observe(Math.Max(refined.N + query.D, query.N + refined.D), pair.Key + "/query-comparison");
                var difference = proof.Add(refined, refined, pair.Key + "/hull-width");
                Rational accuracyValue = tolerance;
                if (pair.Key == profile.Upper.Path || pair.Key == profile.Lower.Path) accuracyValue = profileTolerance;
                else if (blend && pair.Key.StartsWith("profile:", StringComparison.Ordinal) && profileMaxima is not null)
                {
                    Rational smallest = profileMaxima.Min();
                    accuracyValue = smallest < 1 ? tolerance * smallest : tolerance;
                }
                var accuracy = proof.Actual(accuracyValue, pair.Key + "/accuracy");
                proof.Observe(Math.Max(difference.N + accuracy.D, accuracy.N + difference.D), pair.Key + "/accuracy-comparison");
                combined = Union(combined, refined);
                witnesses.Add(new(pair.Key, index, degree, Bits(common), refined.N, refined.D));
            }
            bounds.Add(pair.Key, combined);
            int p = pair.Value[0].Y.Length - 1;
            // Two-coordinate triangular splits + extrema/endpoint/width tests.
            // Counts primitive rational operations and comparison products.
            operations += 8L * pair.Value.Length + 128L * (8L * p * (p + 1) + 32L * (p + 1) + 64);
        }
        if (blend) operations += 2L * blendNodes * 32L + 6L * blendNodes * (blendNodes + 1L);
        Geometry.Require(operations <= 1000000, "All-query operation bound exceeds one million.", GeometryStatus.NotAssessed, "GEOMETRY-QUERY-RESOURCE");
        Size half = new(1, 2);
        var up = bounds[profile.Upper.Path]; var lo = bounds[profile.Lower.Path];
        if (blend)
            foreach (var pair in bounds)
            {
                if (!pair.Key.StartsWith("profile:", StringComparison.Ordinal)) continue;
                if (pair.Key.EndsWith(":upper", StringComparison.Ordinal)) up = Union(up, pair.Value);
                else if (pair.Key.EndsWith(":lower", StringComparison.Ordinal)) lo = Union(lo, pair.Value);
            }
        var maximum = Union(proof.Actual(maximumLower, "maximum-lower"), proof.Actual(maximumUpper, "maximum-upper"));
        var reciprocal = proof.Divide(new(1, 1), maximum, "maximum-reciprocal");
        var camber = proof.Multiply(proof.Add(up, lo, "camber-sum"), half, "camber");
        var normalized = proof.Multiply(proof.Multiply(proof.Multiply(proof.Add(up, lo, "profile-difference"), reciprocal,
            "normalization"), bounds["thickness"], "thickness"), half, "half-thickness");
        var section = proof.Add(camber, normalized, "section");
        proof.Conversion(section, "section");
        if (blend)
        {
            // max T0 subdivides the original Bernstein coefficients, not the
            // 128-deep pointwise hull. Depth 48 is the admission bound.
            Size basis = new(1, 1);
            foreach (var pair in bases)
                if (pair.Key.StartsWith("profile:", StringComparison.Ordinal)) basis = Union(basis, pair.Value);
            int depthBits = (spans[profile.Upper.Path][0].Y.Length - 1) * 48;
            var weight = proof.Track(new(2200, 4300), "blend-weight");
            var scale = proof.Divide(weight, maximum, "blend-unit-scale");
            var coefficient = proof.Multiply(basis, scale, "blend-coefficient");
            var subdivided = proof.Track(new(coefficient.N + depthBits, coefficient.D + depthBits), "blend-maximum-subdivision");
            proof.Multiply(section, proof.Divide(new(1, 1), subdivided, "blend-maximum-reciprocal"), "normalized-thickness-shape");
        }
        IEnumerable<Rational> ordinates = spans[profile.Upper.Path].Concat(spans[profile.Lower.Path]).SelectMany(span => span.Y);
        if (blend) ordinates = spans.Where(pair => pair.Key.StartsWith("profile:", StringComparison.Ordinal)).SelectMany(pair => pair.Value).SelectMany(span => span.Y);
        var largestProfile = ordinates.Select(value => value < 0 ? 0 - value : value).Max();
        Geometry.Require(largestProfile + 2 < Rational.From(double.MaxValue), "Normalized section outward range is not finite.",
            GeometryStatus.NotAssessed, "GEOMETRY-QUERY-RESOURCE");
        var angular = proof.Multiply(bounds["twist"], proof.Actual(Rational.From(0.017453292519943295), "degree-factor"), "angular-product");
        proof.Conversion(angular, "once-rounded-angle");
        proof.Observe(1141, "angle-grid-shift-divrem");
        // Grid endpoints have denominator 2^64 and |angle|<=1; center/radius
        // denominator divides 2^65. Taylor term k denominator divides
        // 2^(65*k)*k!, k<=33. All sums and error (1/32! + radius)
        // divide 2^(65*33)*33!, <2^2270. Pre-reduction recurrence/sums
        // and comparisons fit 5000 bits. 2400 includes signed numerators.
        proof.Observe(5000, "Taylor-33-common-denominator-intermediates");
        Size trig = proof.Track(new(2400, 2400), "Taylor-result");
        var chord = proof.Add(bounds["trailing"], bounds["leading"], "chord");
        var rotated = proof.Add(proof.Multiply(query, trig, "x-trig"), proof.Multiply(section, trig, "z-trig"), "rotated-section");
        var scaled = proof.Multiply(chord, rotated, "placed-scale");
        proof.Conversion(proof.Add(bounds["leading"], scaled, "placed-X"), "placed-X");
        proof.Conversion(proof.Add(bounds["dihedral"], scaled, "placed-Z"), "placed-Z");
        proof.Conversion(proof.Multiply(proof.Multiply(proof.Actual(Rational.From(halfSpan), "half-span"), query, "placed-Y"), new(2, 1), "port-sign"), "placed-Y");
        watch.Check();
        return new("common-denominator-dyadic-128/taylor-grid-64/v1", 32768, proof.maximum, operations, 128, 64,
            Array.AsReadOnly(witnesses.ToArray()), proof.maximumPath);
    }
}

internal sealed class ProofRefusal(string reason, GeometryStatus status = GeometryStatus.NotAssessed, string code = "DSL-GEOMETRY") : Exception(reason)
{
    internal GeometryStatus Status { get; } = status;
    internal string Code { get; } = code;
}

internal sealed class ProofBudget
{
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private readonly TimeSpan limit;
    private readonly CancellationToken cancellation;
    internal ProofBudget(TimeSpan? requested = null, CancellationToken cancellationToken = default)
    {
        cancellation = cancellationToken;
        limit = requested ?? TimeSpan.FromSeconds(1);
        Guard.Require(limit >= TimeSpan.Zero && limit <= TimeSpan.FromSeconds(1), "DSL-RANGE");
    }
    internal void Check()
    {
        Geometry.Require(!cancellation.IsCancellationRequested, "Query cancelled.", GeometryStatus.NotAssessed, "GEOMETRY-CANCELLED");
        Geometry.Require(watch.Elapsed < limit, "Cooperative proof time budget exhausted.", GeometryStatus.NotAssessed, "GEOMETRY-BUDGET");
    }
}

/// <summary>Bounded exact arithmetic for binary64-defined polynomial coefficients.</summary>
internal readonly struct Rational : IComparable<Rational>
{
    private readonly BigInteger numerator;
    private readonly BigInteger denominator;
    internal BigInteger Numerator => numerator;
    internal BigInteger Denominator => denominator;
    internal Rational(BigInteger n, BigInteger d)
    {
        Geometry.Require(d != 0, "Exact arithmetic denominator is zero.");
        Geometry.Require(n.GetBitLength() <= 32768 && d.GetBitLength() <= 32768, "Exact arithmetic size budget exhausted.");
        if (d.Sign < 0) { n = -n; d = -d; }
        var divisor = BigInteger.GreatestCommonDivisor(n, d);
        numerator = n / divisor; denominator = d / divisor;
    }
    internal static Rational From(double value)
    {
        Geometry.Require(double.IsFinite(value), "Nonfinite coefficient.");
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        int exponent = (int)((bits >> 52) & 2047);
        BigInteger n = bits & 0xfffffffffffffUL;
        if (exponent != 0) n += BigInteger.One << 52;
        if ((bits >> 63) != 0) n = -n;
        int shift = exponent == 0 ? -1074 : exponent - 1075;
        return shift >= 0 ? new(n << shift, 1) : new(n, BigInteger.One << -shift);
    }
    public static implicit operator Rational(int value) => new(value, 1);
    public static Rational operator +(Rational a, Rational b) => new(a.numerator * b.denominator + b.numerator * a.denominator, a.denominator * b.denominator);
    public static Rational operator -(Rational a, Rational b) => new(a.numerator * b.denominator - b.numerator * a.denominator, a.denominator * b.denominator);
    public static Rational operator *(Rational a, Rational b) => new(a.numerator * b.numerator, a.denominator * b.denominator);
    public static Rational operator /(Rational a, Rational b) => new(a.numerator * b.denominator, a.denominator * b.numerator);
    public int CompareTo(Rational other) => (numerator * other.denominator).CompareTo(other.numerator * denominator);
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
    internal double Nearest() => numerator == 0 ? 0 : numerator.Sign * DecimalSi.Round(BigInteger.Abs(numerator), denominator);
    internal ExactRatio Exact => new(numerator.ToString(System.Globalization.CultureInfo.InvariantCulture), denominator.ToString(System.Globalization.CultureInfo.InvariantCulture));
    internal Rational DyadicDown(int bits)
    {
        var scaled = numerator << bits;
        var quotient = BigInteger.DivRem(scaled, denominator, out var remainder);
        if (remainder.Sign < 0) quotient--;
        return new(quotient, BigInteger.One << bits);
    }
    internal Rational DyadicUp(int bits)
    {
        var scaled = numerator << bits;
        var quotient = BigInteger.DivRem(scaled, denominator, out var remainder);
        if (remainder.Sign > 0) quotient++;
        return new(quotient, BigInteger.One << bits);
    }
    internal double Down() { double value = Nearest(); return From(value) > this ? Math.BitDecrement(value) : value; }
    internal double Up() { double value = Nearest(); return From(value) < this ? Math.BitIncrement(value) : value; }
}

internal sealed record PolynomialSpan(Rational Start, Rational End, Rational[] X, Rational[] Y);

internal readonly record struct RationalInterval(Rational Lower, Rational Upper)
{
    internal static RationalInterval Point(Rational value) => new(value, value);
    public static RationalInterval operator +(RationalInterval a, RationalInterval b) => new(a.Lower + b.Lower, a.Upper + b.Upper);
    public static RationalInterval operator -(RationalInterval a, RationalInterval b) => new(a.Lower - b.Upper, a.Upper - b.Lower);
    public static RationalInterval operator *(RationalInterval a, RationalInterval b)
    {
        var products = new[] { a.Lower * b.Lower, a.Lower * b.Upper, a.Upper * b.Lower, a.Upper * b.Upper };
        return new(products.Min(), products.Max());
    }
    internal RationalInterval Reciprocal()
    { Geometry.Require(Lower > 0, "Normalization denominator is not certified positive."); return new((Rational)1 / Upper, (Rational)1 / Lower); }
    internal EnclosedOrdinate Outward() => new(Lower.Down(), Upper.Up());
}

internal static class Bernstein
{
    private static void Budget(ProofBudget watch) => watch.Check();

    internal static PolynomialSpan[] Spans(Curve curve, ProofBudget watch)
    {
        var knots = curve.Knots.Select(Rational.From).ToArray();
        var points = curve.Points.Select(p => p.Select(Rational.From).ToArray()).ToArray();
        int degree = curve.Degree;
        var result = new List<PolynomialSpan>();
        for (int span = degree; span < points.Length; span++)
        {
            Budget(watch);
            if (knots[span] >= knots[span + 1]) continue;
            int count = degree + 1;
            var matrix = new Rational[count, count + 2];
            for (int row = 0; row < count; row++)
            {
                Budget(watch);
                Rational fraction = (Rational)row / degree;
                var value = Evaluate(points, knots, degree, knots[span] + fraction * (knots[span + 1] - knots[span]));
                for (int column = 0; column < count; column++)
                    matrix[row, column] = Choose(degree, column) * Power(fraction, column) * Power(1 - fraction, degree - column);
                matrix[row, count] = value[0]; matrix[row, count + 1] = value[1];
            }
            // Interpolation at p+1 distinct rational points uniquely determines the
            // degree-p span polynomial. Elimination recovers its Bernstein basis.
            for (int pivot = 0; pivot < count; pivot++)
            {
                Budget(watch);
                var divisor = matrix[pivot, pivot];
                for (int column = pivot; column < count + 2; column++) matrix[pivot, column] /= divisor;
                for (int row = 0; row < count; row++)
                {
                    Budget(watch);
                    if (row == pivot) continue;
                    var factor = matrix[row, pivot];
                    for (int column = pivot; column < count + 2; column++) matrix[row, column] -= factor * matrix[pivot, column];
                }
            }
            result.Add(new(knots[span], knots[span + 1], Enumerable.Range(0, count).Select(i => matrix[i, count]).ToArray(),
                Enumerable.Range(0, count).Select(i => matrix[i, count + 1]).ToArray()));
        }
        return result.ToArray();
    }

    private static Rational[] Evaluate(Rational[][] points, Rational[] knots, int degree, Rational t)
    {
        if (t >= 1) return points[^1];
        int span = degree;
        while (span + 1 < points.Length && knots[span + 1] <= t) span++;
        var values = Enumerable.Range(0, degree + 1).Select(j => points[span - degree + j].ToArray()).ToArray();
        for (int level = 1; level <= degree; level++)
            for (int j = degree; j >= level; j--)
            {
                int index = span - degree + j;
                Rational width = knots[index + degree - level + 1] - knots[index];
                Rational alpha = width > 0 ? (t - knots[index]) / width : 0;
                for (int coordinate = 0; coordinate < 2; coordinate++)
                    values[j][coordinate] = (1 - alpha) * values[j - 1][coordinate] + alpha * values[j][coordinate];
            }
        return values[degree];
    }
    private static int Choose(int n, int k) { int result = 1; for (int i = 1; i <= k; i++) result = result * (n - i + 1) / i; return result; }
    private static Rational Power(Rational value, int count) { Rational result = 1; for (int i = 0; i < count; i++) result *= value; return result; }

    internal static RationalInterval EncloseAt(PolynomialSpan[] spans, Rational x, ProofBudget watch, Rational? accuracy = null)
    {
        var span = spans.FirstOrDefault(item => item.X[0] <= x && item.X[^1] >= x);
        Geometry.Require(span is not null, "Requested abscissa is outside the certified domain.");
        var abscissae = span!.X; var ordinates = span.Y;
        for (int depth = 0; depth < 128; depth++)
        {
            Budget(watch);
            if (x.CompareTo(abscissae[0]) == 0) return new(ordinates[0], ordinates[0]);
            if (x.CompareTo(abscissae[^1]) == 0) return new(ordinates[^1], ordinates[^1]);
            Rational low = ordinates.Min(), high = ordinates.Max();
            if (high - low <= (accuracy ?? Rational.From(1e-14))) return new(low, high);
            var (leftX, rightX) = Split(abscissae);
            var (leftY, rightY) = Split(ordinates);
            if (x <= leftX[^1]) { abscissae = leftX; ordinates = leftY; }
            else { abscissae = rightX; ordinates = rightY; }
        }
        throw new ProofRefusal("Inverse abscissa enclosure depth budget exhausted.");
    }

    private static (Rational[] Left, Rational[] Right) Split(Rational[] coefficients)
    {
        var working = coefficients.ToArray();
        var left = new Rational[working.Length]; var right = new Rational[working.Length];
        left[0] = working[0]; right[^1] = working[^1];
        for (int level = 1; level < working.Length; level++)
        {
            for (int j = 0; j < working.Length - level; j++) working[j] = (working[j] + working[j + 1]) / 2;
            left[level] = working[0]; right[^(level + 1)] = working[working.Length - level - 1];
        }
        return (left, right);
    }

    internal static (Rational Lower, Rational Upper, int Nodes) Maximum(Rational[][] spans, ProofBudget watch, int nodeBudget = 4096)
    {
        var pending = spans.Select(coefficients => (Coefficients: coefficients, Depth: 0)).ToList();
        Rational lower = pending.SelectMany(item => new[] { item.Coefficients[0], item.Coefficients[^1] }).Max();
        var tolerance = Rational.From(1e-12);
        for (int nodes = 0; nodes < nodeBudget; nodes++)
        {
            Budget(watch);
            var upper = pending.SelectMany(item => item.Coefficients).Max();
            if (lower > 0 && upper - lower <= tolerance * lower) return (lower, upper, nodes);
            int index = pending.FindIndex(item => item.Coefficients.Max().CompareTo(upper) == 0);
            var chosen = pending[index];
            Geometry.Require(chosen.Depth < 64, "Maximum enclosure depth budget exhausted.");
            pending.RemoveAt(index);
            var (left, right) = Split(chosen.Coefficients);
            if (left[^1] > lower) lower = left[^1];
            pending.Add((left, chosen.Depth + 1)); pending.Add((right, chosen.Depth + 1));
        }
        throw new ProofRefusal("Maximum enclosure node budget exhausted.");
    }
}
