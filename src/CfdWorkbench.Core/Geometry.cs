using System.Diagnostics;
using System.Numerics;

namespace CfdWorkbench.Core;

public enum GeometryStatus { Invalid, Unsupported, NotAssessed, Certified }

public sealed record ExactRatio(string Numerator, string Denominator);
public sealed record GeometryWitness(string Curve, ExactRatio DomainStart, ExactRatio DomainEnd,
    ExactRatio AbscissaDifferenceLower, ExactRatio AbscissaDifferenceUpper, ExactRatio OrdinateLower, ExactRatio OrdinateUpper);
public sealed record EnclosedOrdinate(double Lower, double Upper);
public sealed record SectionEnclosure(EnclosedOrdinate Upper, EnclosedOrdinate Lower);

/// <summary>Authority-produced enclosure; source parsing alone cannot construct it.</summary>
public sealed class GeometryCertificate
{
    internal GeometryCertificate(string sourceHash, string surfaceHash, Rational lower, Rational upper,
        IEnumerable<GeometryWitness> witnesses, int nodes, Dictionary<string, PolynomialSpan[]> spans, string upperPath, string lowerPath)
    {
        SourceHash = sourceHash; SurfaceHash = surfaceHash;
        ThicknessMaximumLower = lower.Down(); ThicknessMaximumUpper = upper.Up();
        ExactThicknessMaximumLower = lower.Exact; ExactThicknessMaximumUpper = upper.Exact;
        NormalizationRelativeErrorUpper = ((upper - lower) / lower).Up();
        Witnesses = Array.AsReadOnly(witnesses.ToArray()); SubdivisionNodes = nodes;
        Spans = spans; UpperPath = upperPath; LowerPath = lowerPath; Maximum = new(lower, upper);
    }
    public string AlgorithmVersion => "cfdw-rational-bernstein-subset-1";
    public string ProofScope => "Continuous source-shape proof; placed preview not yet supplied";
    public string SourceHash { get; }
    public string SurfaceHash { get; }
    public double ThicknessMaximumLower { get; }
    public double ThicknessMaximumUpper { get; }
    public ExactRatio ExactThicknessMaximumLower { get; }
    public ExactRatio ExactThicknessMaximumUpper { get; }
    public double NormalizationRelativeErrorUpper { get; }
    public IReadOnlyList<GeometryWitness> Witnesses { get; }
    public int SubdivisionNodes { get; }
    internal Dictionary<string, PolynomialSpan[]> Spans { get; }
    internal string UpperPath { get; }
    internal string LowerPath { get; }
    internal RationalInterval Maximum { get; }
}

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
    public static SectionEnclosure SectionAt(GeometryCertificate certificate, double eta, double x)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        Guard.Require(double.IsFinite(eta) && double.IsFinite(x) && eta >= 0 && eta <= 1 && x >= 0 && x <= 1, "DSL-RANGE");
        var watch = Stopwatch.StartNew();
        try
        {
            var upper = Bernstein.EncloseAt(certificate.Spans[certificate.UpperPath], Rational.From(x), watch);
            var lower = Bernstein.EncloseAt(certificate.Spans[certificate.LowerPath], Rational.From(x), watch);
            var thickness = Bernstein.EncloseAt(certificate.Spans["thickness"], Rational.From(eta), watch);
            var half = new RationalInterval((Rational)1 / 2, (Rational)1 / 2);
            var camber = (upper + lower) * half;
            // All possible exact maxima in the proof enclosure are propagated.
            // No midpoint is substituted as an exact normalization constant.
            var normalizedHalfThickness = (upper - lower) * certificate.Maximum.Reciprocal() * thickness * half;
            return new((camber + normalizedHalfThickness).Outward(), (camber - normalizedHalfThickness).Outward());
        }
        catch (ProofRefusal) { throw new ContractError("GEOMETRY-NOT-ASSESSED"); }
    }

    public static GeometryAssessment Assess(SourceParse source)
    {
        var definition = source.Definition;
        if (definition is null) return new(null, "Static parsing did not succeed.");
        var watch = Stopwatch.StartNew();
        try
        {
            Require(definition.Kind == "foil" && definition.Tip == "open" && definition.Profiles.Length == 1 &&
                definition.Assignments.All(x => x.Profile == 0) && definition.Assertions.Length == 0,
                "Only one inline profile, open tip and no assertions are currently certified.", GeometryStatus.Unsupported);
            Require(definition.Curves.Values.All(curve => !curve.MissingIds), "Accept an explicit ID candidate first.", GeometryStatus.Unsupported);
            Require(definition.Locks.All(item => item.Kind == "root_mirror"), "Other lock types are not assessed.", GeometryStatus.Unsupported, "DSL-LOCK");
            foreach (var item in definition.Locks)
            {
                var curve = definition.Curves[item.Channel.Text];
                Require(curve.Points[0][1] == curve.Points[1][1], "Root tangent lock is not satisfied.", GeometryStatus.Invalid, "DSL-LOCK");
            }
            Require(definition.Curves["leading"].Points[0][1] == 0 && definition.Curves["dihedral"].Points[0][1] == 0,
                "Root leading edge and elevation must be zero.", GeometryStatus.Invalid);
            var profile = definition.Profiles[0];
            Require(profile.Upper.Degree == profile.Lower.Degree && profile.Upper.Knots.SequenceEqual(profile.Lower.Knots) &&
                profile.Upper.Points.Select(p => p[0]).SequenceEqual(profile.Lower.Points.Select(p => p[0])),
                "Independent profile x mappings are not assessed.", GeometryStatus.Unsupported);
            Require(profile.Upper.Points[0][1] == 0 && profile.Lower.Points[0][1] == 0,
                "Profile leading endpoints must meet at zero.", GeometryStatus.Invalid);
            Require(profile.Closure != "closed" || profile.Upper.Points[^1][1] == 0 && profile.Lower.Points[^1][1] == 0,
                "Closed trailing endpoints must meet at zero.", GeometryStatus.Invalid);
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
            var upper = spans[profile.Upper.Path];
            var lower = spans[profile.Lower.Path];
            var differences = upper.Zip(lower, (u, l) => u.Y.Zip(l.Y, (a, b) => a - b).ToArray()).ToArray();
            for (int index = 0; index < differences.Length; index++)
            {
                var coefficients = differences[index];
                Require(coefficients.All(x => x >= 0) && coefficients.Any(x => x > 0), "Profile separation is not certified.");
                Require(index == 0 || coefficients[0] > 0, "Profile sides touch at an interior knot.");
            }
            Require(profile.Closure == "closed" || differences[^1][^1] > 0, "Open trailing endpoints are not separated.");
            var maximum = Bernstein.Maximum(differences, watch);
            var witnesses = spans.SelectMany(pair => pair.Value.Select(span => Witness(pair.Key, span))).ToList();
            witnesses.AddRange(differences.Select((values, index) => Witness("profile-separation", new(upper[index].Start, upper[index].End, upper[index].X, values))));
            return new(new(source.SourceHash, source.SurfaceHash!, maximum.Lower, maximum.Upper, witnesses, maximum.Nodes,
                spans, profile.Upper.Path, profile.Lower.Path), "Continuous conservative subset certified.");
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
}

internal sealed class ProofRefusal(string reason, GeometryStatus status = GeometryStatus.NotAssessed, string code = "DSL-GEOMETRY") : Exception(reason)
{
    internal GeometryStatus Status { get; } = status;
    internal string Code { get; } = code;
}

/// <summary>Bounded exact arithmetic for binary64-defined polynomial coefficients.</summary>
internal readonly struct Rational : IComparable<Rational>
{
    private readonly BigInteger numerator;
    private readonly BigInteger denominator;
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
    internal double Down() { double value = Nearest(); return From(value) > this ? Math.BitDecrement(value) : value; }
    internal double Up() { double value = Nearest(); return From(value) < this ? Math.BitIncrement(value) : value; }
}

internal sealed record PolynomialSpan(Rational Start, Rational End, Rational[] X, Rational[] Y);

internal readonly record struct RationalInterval(Rational Lower, Rational Upper)
{
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
    private static void Budget(Stopwatch watch) => Geometry.Require(watch.Elapsed < TimeSpan.FromSeconds(1), "Continuous proof time budget exhausted.");

    internal static PolynomialSpan[] Spans(Curve curve, Stopwatch watch)
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

    internal static RationalInterval EncloseAt(PolynomialSpan[] spans, Rational x, Stopwatch watch)
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
            if (high - low <= Rational.From(1e-14)) return new(low, high);
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

    internal static (Rational Lower, Rational Upper, int Nodes) Maximum(Rational[][] spans, Stopwatch watch)
    {
        var pending = spans.Select(coefficients => (Coefficients: coefficients, Depth: 0)).ToList();
        Rational lower = pending.SelectMany(item => new[] { item.Coefficients[0], item.Coefficients[^1] }).Max();
        var tolerance = Rational.From(1e-12);
        for (int nodes = 0; nodes < 4096; nodes++)
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
