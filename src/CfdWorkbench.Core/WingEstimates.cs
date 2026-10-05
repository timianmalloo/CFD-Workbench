using System.Diagnostics;

namespace CfdWorkbench.Core;

/// <summary>One <c>estimates.compute</c> measurement. A missing run is not a zero.</summary>
public sealed record EstimateCompute(string Name, double DurationMilliseconds, int IntervalCount, int Iterations, string Outcome);

/// <summary>
/// Derived wing quantities. Never stored. <see cref="Basis"/> is <c>accepted</c> or <c>preview</c>,
/// and <see cref="Generation"/> is the source generation the value was computed from.
/// </summary>
public sealed record WingEstimates(
    double SpanMeters,
    double RootChordMeters,
    double TipChordMeters,
    double MeanChordMeters,
    double MacMeters,
    double MaxThicknessRatio,
    double AspectRatio,
    double AreaSquareMeters,
    string Basis,
    long Generation,
    bool Converged,
    EstimateCompute Compute)
{
    public static WingEstimates From(byte[] source, string basis, long generation)
    {
        var timer = Stopwatch.StartNew();
        var definition = RequireFoil(source);
        var leading = definition.Curves["leading"];
        var trailing = definition.Curves["trailing"];
        int intervals = SpanCount(leading) + SpanCount(trailing);
        double chordIntegral = IntegrateOrdinate(trailing) - IntegrateOrdinate(leading);
        double span = 2 * definition.HalfSpan;
        double area = span * chordIntegral;
        var squared = IntegrateSquare(leading, trailing);
        // Validity is per quantity: mean chord and aspect ratio need only a finite positive area (a crossing
        // draft has none); only MAC also needs the squared-chord integral to converge.
        bool hasArea = double.IsFinite(area) && area > 0 && double.IsFinite(span) && span > 0;
        double mean = hasArea ? area / span : double.NaN;
        double aspect = hasArea ? span * span / area : double.NaN;
        double mac = hasArea && squared.Converged ? span * squared.Integral / area : double.NaN;
        double tipEta = definition.Assignments.Length == 0 ? 1 : definition.Assignments[^1].Eta;
        timer.Stop();
        string outcome = hasArea && squared.Converged ? "ok" : "not-converged";
        return new(span, Chord(definition, 0), Chord(definition, tipEta), mean, mac, MaxOrdinate(definition.Curves["thickness"]),
            aspect, area, basis, generation, squared.Converged, new("estimates.compute", timer.Elapsed.TotalMilliseconds, intervals, squared.Iterations, outcome));
    }

    public static double ChordMeters(byte[] source, double eta) => Chord(RequireFoil(source), eta);

    private static Definition RequireFoil(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" } definition)
            throw new ContractError(parsed.Diagnostics.Count > 0 ? parsed.Diagnostics[0].Code : "DSL-PATCH");
        return definition;
    }

    internal static double Chord(Definition definition, double eta) =>
        OrdinateAt(definition.Curves["trailing"], eta) - OrdinateAt(definition.Curves["leading"], eta);

    // ∫x dη = ∫x(u)·η′(u) du, degree 2p−1 on each span, so p-point Gauss–Legendre is exact up to round-off.
    private static double IntegrateOrdinate(Curve curve)
    {
        int degree = curve.Degree;
        int count = curve.Points.Length;
        double sum = 0;
        for (int span = degree; span < count; span++)
        {
            double start = curve.Knots[span], end = curve.Knots[span + 1];
            if (!(end > start)) continue;
            sum += Quadrature(degree, start, end, parameter =>
            {
                var jet = SplineBasis.Evaluate(curve.Knots, degree, parameter);
                double ordinate = 0, dAbscissa = 0;
                for (int i = 0; i < count; i++)
                {
                    ordinate += jet.N[i] * curve.Points[i][1];
                    dAbscissa += jet.D1[i] * curve.Points[i][0];
                }
                return ordinate * dAbscissa;
            });
        }
        return sum;
    }

    // ∫leading·trailing dη mixes two parameterisations, so it is adaptive in η over both rails' knot images.
    private static (double Integral, int Iterations, bool Converged) IntegrateSquare(Curve leading, Curve trailing)
    {
        var breaks = new List<double> { 0, 1 };
        void Images(Curve curve)
        {
            for (int span = curve.Degree; span <= curve.Points.Length; span++)
                breaks.Add(Math.Clamp(Abscissa(curve, curve.Knots[span]), 0, 1));
        }
        Images(leading);
        Images(trailing);
        breaks.Sort();
        double total = 0;
        int iterations = 1;
        bool converged = true;
        double previous = breaks[0];
        for (int index = 1; index < breaks.Count; index++)
        {
            double end = breaks[index];
            if (end - previous <= 1e-15) continue;
            var piece = Adaptive(previous, end, eta =>
            {
                double chord = OrdinateAt(trailing, eta) - OrdinateAt(leading, eta);
                return chord * chord;
            });
            total += piece.Value;
            iterations = Math.Max(iterations, piece.Iterations);
            converged &= piece.Converged;
            previous = end;
        }
        return (total, iterations, converged);
    }

    // A short handle beside an anchor makes dη/du ≈ 0 at a piece end, so chord²(η) is near-singular there and the
    // Gauss ladder alone does not agree (D-4). Bisect such a piece; each half gets half the absolute tolerance.
    // Iterations reports the deepest level reached plus the ladder steps at that leaf. The fixed depth cap bounds
    // the worst case at 2^MaxBisectionDepth leaves per piece; the measured per-frame cost is estimatesP95Ms.
    private const int MaxBisectionDepth = 8;

    private static (double Value, int Iterations, bool Converged) Adaptive(double start, double end, Func<double, double> f) =>
        Adaptive(start, end, f, tolerance: double.NaN, depth: 0);

    private static (double Value, int Iterations, bool Converged) Adaptive(double start, double end, Func<double, double> f, double tolerance, int depth)
    {
        int[] orders = [4, 8, 12, 16];
        double previous = Quadrature(orders[0], start, end, f);
        for (int step = 1; step < orders.Length; step++)
        {
            double next = Quadrature(orders[step], start, end, f);
            double limit = double.IsNaN(tolerance) ? 1e-9 * Math.Max(Math.Max(Math.Abs(previous), Math.Abs(next)), 1e-30) : tolerance;
            if (Math.Abs(next - previous) <= limit) return (next, depth + step + 1, true);
            previous = next;
        }
        if (depth == MaxBisectionDepth) return (previous, depth + orders.Length, false);
        double childTolerance = 0.5 * (double.IsNaN(tolerance) ? 1e-9 * Math.Max(Math.Abs(previous), 1e-30) : tolerance);
        double mid = 0.5 * (start + end);
        var left = Adaptive(start, mid, f, childTolerance, depth + 1);
        var right = Adaptive(mid, end, f, childTolerance, depth + 1);
        return (left.Value + right.Value, Math.Max(left.Iterations, right.Iterations), left.Converged && right.Converged);
    }

    private static int SpanCount(Curve curve)
    {
        int count = 0;
        for (int span = curve.Degree; span < curve.Points.Length; span++)
            if (curve.Knots[span + 1] > curve.Knots[span]) count++;
        return count;
    }

    private static double MaxOrdinate(Curve curve)
    {
        double max = Math.Max(Ordinate(curve, 0), Ordinate(curve, 1));
        for (int span = curve.Degree; span < curve.Points.Length; span++)
        {
            double start = curve.Knots[span], end = curve.Knots[span + 1];
            if (!(end > start)) continue;
            const int grid = 16;
            double previousParameter = start, previousSlope = OrdinateSlope(curve, start);
            max = Math.Max(max, Ordinate(curve, start));
            for (int sample = 1; sample <= grid; sample++)
            {
                double parameter = start + (end - start) * sample / grid;
                double slope = OrdinateSlope(curve, parameter);
                max = Math.Max(max, Ordinate(curve, parameter));
                if (previousSlope == 0 || previousSlope > 0 && slope <= 0)
                    max = Math.Max(max, Ordinate(curve, Root(curve, previousParameter, parameter, previousSlope)));
                previousParameter = parameter;
                previousSlope = slope;
            }
        }
        return max;
    }

    private static double Root(Curve curve, double start, double end, double startSlope)
    {
        double low = start, high = end, lowSlope = startSlope;
        for (int step = 0; step < 50; step++)
        {
            double mid = (low + high) / 2;
            double slope = OrdinateSlope(curve, mid);
            if (lowSlope == 0 || (lowSlope > 0) == (slope > 0)) { low = mid; lowSlope = slope; }
            else high = mid;
        }
        return (low + high) / 2;
    }

    private static double OrdinateAt(Curve curve, double eta) =>
        ChannelEvaluator.Value(curve.Knots, curve.Degree, curve.Points, eta);

    private static double Abscissa(Curve curve, double parameter) => Dot(curve, parameter, 0, false);
    private static double Ordinate(Curve curve, double parameter) => Dot(curve, parameter, 1, false);
    private static double OrdinateSlope(Curve curve, double parameter) => Dot(curve, parameter, 1, true);

    private static double Dot(Curve curve, double parameter, int coordinate, bool derivative)
    {
        var jet = SplineBasis.Evaluate(curve.Knots, curve.Degree, parameter);
        double[] basis = derivative ? jet.D1 : jet.N;
        double value = 0;
        for (int i = 0; i < basis.Length; i++) value += basis[i] * curve.Points[i][coordinate];
        return value;
    }

    private static double Quadrature(int order, double start, double end, Func<double, double> f)
    {
        var (nodes, weights) = GaussLegendre(order);
        double mid = 0.5 * (start + end), half = 0.5 * (end - start), sum = 0;
        for (int i = 0; i < order; i++) sum += weights[i] * f(mid + half * nodes[i]);
        return sum * half;
    }

    private static (double[] Nodes, double[] Weights) GaussLegendre(int order)
    {
        var nodes = new double[order];
        var weights = new double[order];
        for (int i = 0; i < (order + 1) / 2; i++)
        {
            double z = Math.Cos(Math.PI * (i + 0.75) / (order + 0.5));
            double derivative = 0;
            for (int iter = 0; iter < 50; iter++)
            {
                double p0 = 1, p1 = z;
                for (int k = 2; k <= order; k++)
                {
                    double next = ((2 * k - 1) * z * p1 - (k - 1) * p0) / k;
                    p0 = p1;
                    p1 = next;
                }
                derivative = order * (z * p1 - p0) / (z * z - 1);
                double step = p1 / derivative;
                z -= step;
                if (Math.Abs(step) < 1e-15) break;
            }
            nodes[i] = -z;
            nodes[order - 1 - i] = z;
            weights[i] = weights[order - 1 - i] = 2 / ((1 - z * z) * derivative * derivative);
        }
        return (nodes, weights);
    }
}
