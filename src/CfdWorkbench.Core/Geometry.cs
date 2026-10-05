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
    /// <summary>Deterministic proof work spent, in bit-work units (see <c>ProofBudget</c>); identical on every machine.</summary>
    public long ProofWork { get; internal set; }
}

public static class Geometry
{
    // The minimum node budget and per-span bisection depth. Every parsed degree-5 profile has at most 32 points,
    // hence at most 27 nonzero spans. N(s) = max(256, 48s) covers every such span; SectionEdits reads this limit.
    internal const int BlendNodes = 256, BlendDepth = 48;
    internal static int BlendSpanLimit() => FoilSource.ProfilePointLimit - FoilSource.ProfileDegree;
    // The all-query operation bound's own refusal code (Not assessed), distinct from the arithmetic bit bound's
    // GEOMETRY-QUERY-RESOURCE, so a caller can tell the capacity limit from other resource refusals.
    internal const string OperationBoundCode = "GEOMETRY-QUERY-OPERATIONS";
    // Pinned literal (design §5.1), not computed on first use: a static initializer's exact-rational search would be
    // charged to whichever proof first touched it, so the same proof's work would depend on order (DET-CLOCK).
    // Geometry_TwistDomain_LargestAssessableDegreesPinned requires it to equal LargestAdmissibleTwist() bit for bit.
    public const double TwistDomainDegrees = 57.295779513082323;
    // Certificate hull stays the open interval (0, 1). This quantum grid is what the gesture clamp reads.
    // Geometry_ThicknessImmediatelyBelowOne_Admitted pins a value above the quantum upper end.
    public static (double Lower, double Upper) ThicknessDomain => (1e-7, 1 - 1e-7);

    /// <summary>Test oracle for <see cref="TwistDomainDegrees"/>: the largest binary64 whose ± twist the Taylor proof admits.</summary>
    internal static double LargestAdmissibleTwist()
    {
        long low = BitConverter.DoubleToInt64Bits(0);
        long high = BitConverter.DoubleToInt64Bits(60);
        long best = low;
        while (low <= high)
        {
            long mid = low + ((high - low) >> 1);
            double value = BitConverter.Int64BitsToDouble(mid);
            if (TwistAdmissible(value) && TwistAdmissible(-value)) { best = mid; low = mid + 1; }
            else high = mid - 1;
        }
        return BitConverter.Int64BitsToDouble(best);
    }

    private static bool TwistAdmissible(double degrees)
    {
        var product = Rational.From(degrees) * Rational.From(PlacementRule.RadiansPerDegree);
        var rounded = Rational.From(product.Nearest());
        return rounded >= -1 && rounded <= 1;
    }

    private static void RequireTwistDomain(Dictionary<string, PolynomialSpan[]> spans)
    {
        var twists = spans["twist"].SelectMany(span => span.Y).ToArray();
        var limit = Rational.From(TwistDomainDegrees);
        Require(twists.Min() >= 0 - limit && twists.Max() <= limit,
            "Whole-domain angle hull is outside the certified Taylor domain.");
    }

    public static PlacedPointEnclosure PointAt(GeometryCertificate certificate, double eta, double x, bool upper, bool port = false,
        CancellationToken cancellationToken = default) =>
        PointAt(certificate, eta, x, upper, port, new ProofBudget(cancellationToken: cancellationToken));

    internal static PlacedPointEnclosure PointAt(GeometryCertificate certificate, double eta, double x, bool upper, bool port, ProofBudget watch)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        Domain(eta, x);
        try
        {
            var section = SectionExact(certificate, eta, x, watch);
            var z = upper ? section.Upper : section.Lower;
            var station = Rational.From(eta);
            var leading = Bernstein.EncloseAt(certificate.Spans["leading"], station, watch);
            var trailing = Bernstein.EncloseAt(certificate.Spans["trailing"], station, watch);
            var elevation = Bernstein.EncloseAt(certificate.Spans["dihedral"], station, watch);
            var degrees = Bernstein.EncloseAt(certificate.Spans["twist"], station, watch);
            var (sin, cos) = RationalInterval.SinCos(RationalInterval.Radians(degrees));
            var (placedX, placedZ) = PlacementRule.Place(leading, trailing, elevation, (sin, cos), RationalInterval.Point(x), z);
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
        CancellationToken cancellationToken = default) =>
        SectionAt(certificate, eta, x, new ProofBudget(cancellationToken: cancellationToken));

    internal static SectionEnclosure SectionAt(GeometryCertificate certificate, double eta, double x, ProofBudget watch)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        Domain(eta, x);
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
        double[] etas = new double[certificate.Stations.Length];
        int[] profiles = new int[etas.Length];
        for (int index = 0; index < etas.Length; index++)
        {
            etas[index] = certificate.Stations[index].Eta.Nearest();
            profiles[index] = certificate.Stations[index].Profile;
        }
        var (left, right) = PlacementRule.Select(etas, profiles, eta, (a, b) => SameGeometry(certificate, a, b));
        if (right < 0) return ProfileSection(certificate, left, eta, x, watch);
        var a = ProfileComponents(certificate, left, x, watch);
        var b = ProfileComponents(certificate, right, x, watch);
        int bracket = PlacementRule.Bracket(etas, eta);
        var weight = RationalInterval.BlendWeight(eta, etas[bracket], etas[bracket + 1]);
        // Scales share one uncertain maximum per profile, so the unit-shape
        // family is the segment between the reciprocal endpoints, not a box.
        var maximum = EncloseMaximumT0(certificate, left, right, weight.Lower, watch);
        Geometry.Require(maximum.Lower > (Rational)1 / 4 && maximum.Upper >= maximum.Lower, "Blend maximum enclosure is not certified.");
        var thickness = Bernstein.EncloseAt(certificate.Spans["thickness"], Rational.From(eta), watch);
        return PlacementRule.Blend((a.Camber, a.Unit), (b.Camber, b.Unit), weight, maximum.Reciprocal(), thickness);
    }

    private static (RationalInterval Upper, RationalInterval Lower) ProfileSection(GeometryCertificate certificate, int index, double eta, double x, ProofBudget watch)
    {
        var profile = certificate.Profiles[index];
        var profileTolerance = ProfileTolerance(profile.Maximum.Lower);
        var upper = Bernstein.EncloseAt(certificate.Spans[profile.UpperPath], Rational.From(x), watch, profileTolerance);
        var lower = Bernstein.EncloseAt(certificate.Spans[profile.LowerPath], Rational.From(x), watch, profileTolerance);
        var thickness = Bernstein.EncloseAt(certificate.Spans["thickness"], Rational.From(eta), watch);
        // Every exact maximum in the authority-produced enclosure is propagated.
        return PlacementRule.Section(upper, lower, profile.Maximum.Reciprocal(), thickness);
    }

    private static (RationalInterval Camber, RationalInterval Unit) ProfileComponents(GeometryCertificate certificate, int index, double x, ProofBudget watch)
    {
        var profile = certificate.Profiles[index];
        var profileTolerance = ProfileTolerance(profile.Maximum.Lower);
        var upper = Bernstein.EncloseAt(certificate.Spans[profile.UpperPath], Rational.From(x), watch, profileTolerance);
        var lower = Bernstein.EncloseAt(certificate.Spans[profile.LowerPath], Rational.From(x), watch, profileTolerance);
        return PlacementRule.Components(upper, lower, profile.Maximum.Reciprocal());
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

    // The certificate's "differ" for two profiles outside a certificate (the section-step budget clause asks it before Assess),
    // as SharedAbscissa is: equal Bernstein spans on both surfaces.
    internal static bool SameGeometry(ProfileDefinition left, ProfileDefinition right, ProofBudget watch) =>
        ReferenceEquals(left, right) || SameSpans(Bernstein.Spans(left.Upper, watch), Bernstein.Spans(right.Upper, watch)) &&
        SameSpans(Bernstein.Spans(left.Lower, watch), Bernstein.Spans(right.Lower, watch));

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

    // The blend rule asked before a section step lands (Ruling 71). A certified profile's difference spans take the upper
    // spans' X, Start and End, and their Y length is the shared degree + 1 (both sides share degree and knots), so the
    // upper spans give the same answer as the differences the certificate compares.
    internal static bool SharedAbscissa(ProfileDefinition left, ProfileDefinition right, ProofBudget watch) =>
        SharedAbscissa(Bernstein.Spans(left.Upper, watch), Bernstein.Spans(right.Upper, watch));

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

    internal static (RationalInterval Sin, RationalInterval Cos) Trigonometry(RationalInterval angle)
    {
        Require(angle.Lower >= -1 && angle.Upper <= 1, "Angles beyond the current certified Taylor domain are not assessed.");
        Rational center = (angle.Lower + angle.Upper) / 2;
        Rational radius = (angle.Upper - angle.Lower) / 2;
        Rational sine = center, cosine = 1, sinTerm = center, cosTerm = 1;
        Rational square = center * center;
        for (int i = 1; i <= PlacementRule.TaylorTerms; i++)
        {
            cosTerm = cosTerm * (0 - square) / ((2 * i - 1) * 2 * i);
            sinTerm = sinTerm * (0 - square) / (2 * i * (2 * i + 1));
            cosine += cosTerm; sine += sinTerm;
        }
        Rational factorial = 1;
        for (int i = 2; i <= 2 * PlacementRule.TaylorTerms; i++) factorial *= i;
        // On [-1,1], 1/32! bounds either omitted Taylor tail. Both derivatives
        // have magnitude <=1, so the input interval adds at most its radius.
        Rational error = (Rational)1 / factorial + radius;
        return (new(sine - error, sine + error), new(cosine - error, cosine + error));
    }

    public static GeometryAssessment Assess(SourceParse source) =>
        Assess(source, new ProofBudget());

    internal static GeometryAssessment Assess(SourceParse source, ProofBudget watch)
    {
        var assessment = AssessWithin(source, watch);
        assessment.ProofWork = watch.Spent;
        return assessment;
    }

    private static GeometryAssessment AssessWithin(SourceParse source, ProofBudget watch)
    {
        var definition = source.Definition;
        if (definition is null) return new(null, "Static parsing did not succeed.");
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
            CheckTangentRows(definition);
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
                    Require(coefficients.All(x => x >= 0) && coefficients.Any(x => x > 0), "Profile separation is not certified.", code: "DSL-PROFILE-CROSS");
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
                string leftName = definition.Profiles[stations[index].Profile].Name;
                string rightName = definition.Profiles[stations[index + 1].Profile].Name;
                Require(SharedAbscissa(left.Difference, right.Difference),
                    "Profile '" + rightName + "' abscissae differ from neighbouring profile '" + leftName + "'.", GeometryStatus.Unsupported);
                spanCount = Math.Max(spanCount, Math.Max(left.Difference.Length, right.Difference.Length));
                degree = left.Difference[0].Y.Length - 1;
            }
            int blendNodes = Math.Max(BlendNodes, BlendDepth * spanCount);
            if (distinct)
            {
                Rational range = 0;
                foreach (var profile in certified)
                {
                    var coefficients = profile.Difference.SelectMany(span => span.Y).ToArray();
                    Rational unitRange = (coefficients.Max() - coefficients.Min()) / profile.Maximum.Lower;
                    if (unitRange > range) range = unitRange;
                }
                const int depth = BlendDepth;
                // The old spanCount * BlendDepth <= blendNodes admission check is true by construction of N(s).
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
                distinct, distinct ? blendNodes : 0, distinct ? certified.Select(item => item.Maximum.Lower).ToArray() : null, spanCount, degree);
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

    private static void CheckTangentRows(Definition definition)
    {
        foreach (var curve in definition.Curves.Values)
        {
            foreach (var row in curve.Tangents)
            {
                int index = Array.IndexOf(curve.Ids, row.Id);
                Require(index > 0 && index < curve.Points.Length - 1, "A tangent row names an interior anchor.", GeometryStatus.Invalid, "DSL-LOCK");
                double[] anchor = curve.Points[index], left = curve.Points[index - 1], right = curve.Points[index + 1];
                if (curve.Path.StartsWith("profile:", StringComparison.Ordinal))
                    CheckProfileRow(row, anchor, left, right);
                else if (curve.Path is "dihedral" or "twist" or "thickness")
                    CheckChannelRow(curve.Path, row.Kind, anchor, left, right);
                else
                    CheckRailRow(definition.HalfSpan, row.Kind, anchor, left, right);
            }
        }
    }

    private static void CheckProfileRow(TangentRow row, double[] anchor, double[] left, double[] right)
    {
        const double tau = 1e-9;
        string because = "Tangent row '" + row.Id + "' does not satisfy " + row.Kind + ".";
        if (row.Kind == "smooth")
            Require(ProfileSmooth(anchor, left, right, tau), because, GeometryStatus.Invalid, "DSL-LOCK");
        else if (row.Kind == "symmetric")
            Require(ProfileSmooth(anchor, left, right, tau) && ProfileMidpoint(anchor, left, right, tau), because, GeometryStatus.Invalid, "DSL-LOCK");
        else if (row.Kind == "horizontal")
            Require(left[1] == anchor[1] && right[1] == anchor[1] && left[0] < anchor[0] && anchor[0] < right[0], because, GeometryStatus.Invalid, "DSL-LOCK");
        else if (row.Kind == "vertical")
            Require(left[0] == anchor[0] && right[0] == anchor[0] && (left[1] - anchor[1]) * (right[1] - anchor[1]) < 0, because, GeometryStatus.Invalid, "DSL-LOCK");
        else if (row.Kind == "angle")
        {
            double radians = (row.Angle ?? double.NaN) * PlacementRule.RadiansPerDegree;
            Require(NearRay(anchor, right, radians, tau) && NearRay(anchor, left, radians + Math.PI, tau), because, GeometryStatus.Invalid, "DSL-LOCK");
        }
        else
            Require(false, because, GeometryStatus.Invalid, "DSL-LOCK");
    }

    private static bool ProfileSmooth(double[] anchor, double[] left, double[] right, double tau)
    {
        double along = (anchor[0] - left[0]) * (right[0] - anchor[0]) + (anchor[1] - left[1]) * (right[1] - anchor[1]);
        double spanX = right[0] - left[0], spanY = right[1] - left[1];
        double length = Math.Sqrt(spanX * spanX + spanY * spanY);
        if (length == 0 || along <= 0) return false;
        double cross = Math.Abs(spanX * (anchor[1] - left[1]) - spanY * (anchor[0] - left[0]));
        return cross / length <= tau;
    }

    private static bool ProfileMidpoint(double[] anchor, double[] left, double[] right, double tau) =>
        Math.Sqrt(Math.Pow(anchor[0] - (left[0] + right[0]) / 2, 2) + Math.Pow(anchor[1] - (left[1] + right[1]) / 2, 2)) <= tau;

    private static bool NearRay(double[] origin, double[] handle, double radians, double tau)
    {
        double vx = handle[0] - origin[0], vy = handle[1] - origin[1];
        return Math.Abs(vx * Math.Sin(radians) - vy * Math.Cos(radians)) <= tau && vx * Math.Cos(radians) + vy * Math.Sin(radians) > 0;
    }

    private static void CheckChannelRow(string path, string kind, double[] anchor, double[] left, double[] right)
    {
        double tau = path switch { "dihedral" => 1e-6, "twist" => 1e-6, "thickness" => 1e-8, _ => 0 };
        Require(right[0] > left[0], "A smooth row has a zero-length handle.", GeometryStatus.Invalid, "DSL-LOCK");
        if (kind == "smooth")
        {
            double t = (anchor[0] - left[0]) / (right[0] - left[0]);
            double line = left[1] + t * (right[1] - left[1]);
            Require(Math.Abs(anchor[1] - line) <= tau, "Smooth row is off the handle line.", GeometryStatus.Invalid, "DSL-LOCK");
        }
        else if (kind == "symmetric")
        {
            double midEta = (left[0] + right[0]) / 2, midValue = (left[1] + right[1]) / 2;
            Require(Math.Abs(anchor[0] - midEta) <= 1e-9 && Math.Abs(anchor[1] - midValue) <= tau,
                "Symmetric row is not the handle midpoint.", GeometryStatus.Invalid, "DSL-LOCK");
        }
    }

    private static void CheckRailRow(double halfSpan, string kind, double[] anchor, double[] left, double[] right)
    {
        double leftSpan = (anchor[0] - left[0]) * halfSpan, leftAft = anchor[1] - left[1];
        double rightSpan = (right[0] - anchor[0]) * halfSpan, rightAft = right[1] - anchor[1];
        double leftLength = Math.Sqrt(leftSpan * leftSpan + leftAft * leftAft);
        double rightLength = Math.Sqrt(rightSpan * rightSpan + rightAft * rightAft);
        if (kind == "smooth")
        {
            Require(leftLength > 0 && rightLength > 0, "A smooth row has a zero-length handle.", GeometryStatus.Invalid, "DSL-LOCK");
            double dot = Math.Clamp((leftSpan * rightSpan + leftAft * rightAft) / (leftLength * rightLength), -1, 1);
            double degrees = Math.Acos(dot) * (180 / Math.PI);
            Require(degrees <= 0.1, "Smooth row is off by more than 0.1 degrees.", GeometryStatus.Invalid, "DSL-LOCK");
        }
        else if (kind == "symmetric")
        {
            double midSpan = (left[0] + right[0]) / 2, midAft = (left[1] + right[1]) / 2;
            double distance = Math.Sqrt(Math.Pow((anchor[0] - midSpan) * halfSpan, 2) + Math.Pow(anchor[1] - midAft, 2));
            double handle = Math.Sqrt(Math.Pow((right[0] - left[0]) * halfSpan, 2) + Math.Pow(right[1] - left[1], 2)) / 2;
            Require(handle > 0 && distance <= 1e-6 * handle, "Symmetric row is not the handle midpoint.", GeometryStatus.Invalid, "DSL-LOCK");
        }
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
        var factor = Rational.From(PlacementRule.RadiansPerDegree);
        RequireTwistDomain(spans);
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
        for (int i = 2; i <= 2 * PlacementRule.TaylorTerms; i++) factorial *= i;
        Rational trigWidth = delta * factor + new Rational(1, BigInteger.One << 51) + new Rational(2, BigInteger.One << PlacementRule.AngleGridBits) + (Rational)2 / factorial;
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
        var factor = Rational.From(PlacementRule.RadiansPerDegree);
        RequireTwistDomain(spans);
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
        for (int i = 2; i <= 2 * PlacementRule.TaylorTerms; i++) factorial *= i;
        Rational trigWidth = delta * factor + new Rational(1, BigInteger.One << 51) + new Rational(2, BigInteger.One << PlacementRule.AngleGridBits) + (Rational)2 / factorial;
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
    /// <summary>Worst-case rational comparisons in one heap-backed Bernstein.Maximum call. L = floor(log2(s + N)).</summary>
    internal static long MaximumComparisons(int nodes, int spans, int degree)
    {
        long height = BitOperations.Log2((uint)(spans + nodes));
        return 2L * spans + spans * (degree + height) + nodes * (2L * degree + 4L * height + 3L) + 2L;
    }

    internal static QueryFeasibilityWitness Prove(Dictionary<string, PolynomialSpan[]> spans, ProfileDefinition profile,
        Rational maximumLower, Rational maximumUpper, double halfSpan, ProofBudget watch, bool blend = false, int blendNodes = 0, Rational[]? profileMaxima = null,
        int blendSpans = 0, int blendDegree = 0)
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
        // Two Maximum calls per query: 32 split/tolerance operations per node, plus bounded heap comparisons.
        if (blend) operations += 64L * blendNodes + 2L * MaximumComparisons(blendNodes, blendSpans, blendDegree);
        Geometry.Require(operations <= 1000000, "All-query operation bound exceeds one million.", GeometryStatus.NotAssessed, Geometry.OperationBoundCode);
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
        Geometry.Require(2 * PlacementRule.TaylorTerms + 1 == 33 && PlacementRule.AngleGridBits == 64,
            "Taylor and grid constants drifted from the certified bound models.");
        var angular = proof.Multiply(bounds["twist"], proof.Actual(Rational.From(PlacementRule.RadiansPerDegree), "degree-factor"), "angular-product");
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
        return new("common-denominator-dyadic-128/taylor-grid-64/v1", 32768, proof.maximum, operations, 128, PlacementRule.AngleGridBits,
            Array.AsReadOnly(witnesses.ToArray()), proof.maximumPath);
    }
}

internal sealed class ProofRefusal(string reason, GeometryStatus status = GeometryStatus.NotAssessed, string code = "DSL-GEOMETRY") : Exception(reason)
{
    internal GeometryStatus Status { get; } = status;
    internal string Code { get; } = code;
}

/// <summary>
/// Cooperative, deterministic proof limit. It counts work, never time (operator ruling 2026-10-02; defect class
/// DET-CLOCK): the unit is bit-work, the summed operand bit lengths of every exact <see cref="Rational"/> the proof
/// constructs on its thread (<see cref="Rational.Work"/>). The same proof spends the same work on every machine at
/// any load, so a busy machine refuses exactly what a quiet one refuses. Cancellation stays a user action.
/// </summary>
internal sealed class ProofBudget
{
    // Calibrated 2026-10-03 over every budget the Core, Desktop, Cli and Core readiness suites construct (11,780):
    // the largest accepted proof spent 225,137,163 units (display samples of a ten-vertex degree-5 rebuild,
    // AuthoringSession.Sample); the largest Assess spent 14,097,010. The limit is 4.4x the worst accepted proof.
    // A unit's cost grows with operand width (GCD and multiply are superlinear), so the limit bounds work, not a
    // fixed time. Measured (Release, Apple silicon, quiet): ~2.1 ns per unit at the accepted proofs' widths (median;
    // widest accepted operand 3,971 bits), so the worst accepted proof is ~0.5 s and the limit ~2 s at those widths;
    // ~10 ns per unit at the 32768-bit cap, so a refusal near the cap can take ~10 s. Readiness_ProofWork_
    // WorstFixtureWithinQuarterOfLimit reports both and fails when the worst case passes a quarter of the limit.
    internal const long DefaultWorkLimit = 1_000_000_000;
    internal static int Entries;
    private readonly long start = Rational.Work;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly CancellationToken cancellation;
    internal ProofBudget(long? workLimit = null, CancellationToken cancellationToken = default)
    {
        Entries++;
        cancellation = cancellationToken;
        Limit = workLimit ?? DefaultWorkLimit;
        Guard.Require(Limit >= 0 && Limit <= DefaultWorkLimit, "DSL-RANGE");
    }
    internal long Limit { get; }
    internal long Spent
    {
        get
        {
            // Rational.Work is per thread; a proof that hopped threads would count another proof's work.
            if (Environment.CurrentManagedThreadId != thread) throw new InvalidOperationException("A proof budget is bound to the thread that created it.");
            return Rational.Work - start;
        }
    }
    /// <summary>The public time parameter's mapping, kept for source compatibility: zero is no work; any admissible
    /// positive time is the default work limit; beyond one second is refused as before.</summary>
    internal static long? LimitFor(TimeSpan? time)
    {
        if (time is not { } requested) return null;
        Guard.Require(requested >= TimeSpan.Zero && requested <= TimeSpan.FromSeconds(1), "DSL-RANGE");
        return requested == TimeSpan.Zero ? 0 : null;
    }
    internal void Check()
    {
        Geometry.Require(!cancellation.IsCancellationRequested, "Query cancelled.", GeometryStatus.NotAssessed, "GEOMETRY-CANCELLED");
        if (Spent >= Limit)
            throw new ProofRefusal("Proof work limit of " + Limit.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " bit-work units reached.", GeometryStatus.NotAssessed, "GEOMETRY-BUDGET");
    }
}

/// <summary>Bounded exact arithmetic for binary64-defined polynomial coefficients.</summary>
internal readonly struct Rational : IComparable<Rational>
{
    [ThreadStatic] private static long work;
    private readonly BigInteger numerator;
    private readonly BigInteger denominator;
    internal BigInteger Numerator => numerator;
    internal BigInteger Denominator => denominator;
    /// <summary>Bit-work on this thread: the summed operand bit lengths of every rational constructed, before
    /// normalization. ProofBudget's unit; it tracked measured proof time with r = 0.99 (2026-10-03).</summary>
    internal static long Work => work;
    [ThreadStatic] private static long widest;
    /// <summary>The widest constructor operand on this thread, in bits, since <see cref="ResetWidest"/>. A unit's cost
    /// grows with operand width, so the calibration reports it beside the work.</summary>
    internal static long WidestOperandBits => widest;
    internal static void ResetWidest() => widest = 0;
    internal Rational(BigInteger n, BigInteger d)
    {
        Geometry.Require(d != 0, "Exact arithmetic denominator is zero.");
        long numeratorBits = n.GetBitLength(), denominatorBits = d.GetBitLength();
        Geometry.Require(numeratorBits <= 32768 && denominatorBits <= 32768, "Exact arithmetic size budget exhausted.");
        work += numeratorBits + denominatorBits;
        widest = Math.Max(widest, Math.Max(numeratorBits, denominatorBits));
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

internal readonly record struct RationalInterval(Rational Lower, Rational Upper) : IPlacementScalar<RationalInterval>
{
    public static RationalInterval Half => Point((Rational)1 / 2);
    internal static RationalInterval Point(Rational value) => new(value, value);
    public static RationalInterval Point(double value) => Point(Rational.From(value));
    public static RationalInterval BlendWeight(double eta, double left, double right)
    {
        Rational weight = (Rational.From(eta) - Rational.From(left)) / (Rational.From(right) - Rational.From(left));
        return Point(weight);
    }
    public static RationalInterval Radians(RationalInterval degrees)
    {
        var product = degrees * Point(Rational.From(PlacementRule.RadiansPerDegree));
        return new(Rational.From(product.Lower.Nearest()).DyadicDown(PlacementRule.AngleGridBits),
            Rational.From(product.Upper.Nearest()).DyadicUp(PlacementRule.AngleGridBits));
    }
    public static (RationalInterval Sin, RationalInterval Cos) SinCos(RationalInterval radians) => Geometry.Trigonometry(radians);
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
        LastMaximumComparisons = 0;
        // The heap's total order preserves the list rescan's first-inserted choice on equal maxima.
        var pending = new MaximumHeap();
        Rational lower = spans[0][0];
        foreach (var coefficients in spans)
        {
            if (Compare(coefficients[0], lower) > 0) lower = coefficients[0];
            if (Compare(coefficients[^1], lower) > 0) lower = coefficients[^1];
        }
        var tolerance = Rational.From(1e-12);
        foreach (var coefficients in spans) pending.Push(coefficients, 0);
        for (int nodes = 0; nodes < nodeBudget; nodes++)
        {
            Budget(watch);
            var chosen = pending.Top;
            var upper = chosen.Maximum;
            if (Compare(lower, 0) > 0 && Compare(upper - lower, tolerance * lower) <= 0) return (lower, upper, nodes);
            Geometry.Require(chosen.Depth < 64, "Maximum enclosure depth budget exhausted.");
            pending.Pop();
            var (left, right) = Split(chosen.Coefficients);
            if (Compare(left[^1], lower) > 0) lower = left[^1];
            pending.Push(left, chosen.Depth + 1); pending.Push(right, chosen.Depth + 1);
        }
        throw new ProofRefusal("Maximum enclosure node budget exhausted.");
    }

    [ThreadStatic] internal static long LastMaximumComparisons;
    private static int Compare(Rational left, Rational right)
    {
        LastMaximumComparisons++;
        return left.CompareTo(right);
    }

    private readonly record struct MaximumNode(Rational[] Coefficients, int Depth, Rational Maximum, long Sequence);

    // Kept here rather than using PriorityQueue: the all-query bound counts this heap's exact comparison sites.
    private sealed class MaximumHeap
    {
        private readonly List<MaximumNode> nodes = new();
        private long sequence;

        internal MaximumNode Top => nodes[0];

        // p comparisons for the coefficient maximum, then at most floor(log2 size) sift-up comparisons.
        internal void Push(Rational[] coefficients, int depth)
        {
            Rational maximum = coefficients[0];
            for (int index = 1; index < coefficients.Length; index++)
                if (Compare(coefficients[index], maximum) > 0) maximum = coefficients[index];
            nodes.Add(new(coefficients, depth, maximum, sequence++));
            for (int child = nodes.Count - 1; child > 0;)
            {
                int parent = (child - 1) / 2;
                if (!Before(nodes[child], nodes[parent])) break;
                (nodes[child], nodes[parent]) = (nodes[parent], nodes[child]);
                child = parent;
            }
        }

        // At most two comparisons at each of floor(log2 size) sift-down levels.
        internal void Pop()
        {
            nodes[0] = nodes[^1];
            nodes.RemoveAt(nodes.Count - 1);
            for (int parent = 0; ;)
            {
                int child = 2 * parent + 1;
                if (child >= nodes.Count) break;
                if (child + 1 < nodes.Count && Before(nodes[child + 1], nodes[child])) child++;
                if (!Before(nodes[child], nodes[parent])) break;
                (nodes[child], nodes[parent]) = (nodes[parent], nodes[child]);
                parent = child;
            }
        }

        private static bool Before(MaximumNode left, MaximumNode right)
        {
            int order = Compare(left.Maximum, right.Maximum);
            return order != 0 ? order > 0 : left.Sequence < right.Sequence;
        }
    }
}
