namespace CfdWorkbench.Core;

public enum CurveSide { Auto, Before, After }
public enum StationKind { Smooth, Straight, Inflection, Corner, CurvatureJump }

/// <summary>Signed display curvature (+ convex on both rails) per metre and its radius at one parameter.
/// A straight station reads curvature 0 and an infinite radius.</summary>
public sealed record CurvatureReading(double T, double SpanMeters, double Aft, double Curvature, double RadiusMeters, bool Straight);

/// <summary>A station on a rail. Root and Tip are the two one-sided readings at a repeated knot; at any other station they are the same reading.</summary>
public sealed record StationReading(double T, StationKind Kind, CurvatureReading Root, CurvatureReading Tip,
    double TangentJumpDegrees, bool SignChanges);

public sealed record MonotoneBoundary(double T, double Eta, double Curvature);
public sealed record MonotoneCount(int Count, double ThresholdPerMeter, double ArcLengthMeters, IReadOnlyList<MonotoneBoundary> Boundaries);

/// <summary>A comb tooth. The unit direction points away from the centre of curvature; StartsPiece marks the first tooth of a
/// continuous piece (the rail's first tooth and the tip-side tooth at every repeated knot); Corner marks a measured tangent jump.</summary>
public sealed record RailTooth(double T, double SpanMeters, double Ordinate, double DirSpan, double DirAft, double Curvature,
    bool StartsPiece, bool Corner);

// Rail comb, Core layer (docs/design/rail-comb.md section 9): curvature at a station with one-sided values by explicit span
// selection, extrema by per-span root finding, monotone pieces with tau / L, Gauss-Legendre length, teeth by whole-rail arc length.
public static partial class Planform
{
    public const double ReversalTau = 0.02;
    public const double StraightSagittaMeters = 1e-5;
    public const double CornerDegrees = 0.1;
    private const double InflectionStep = 0.02;
    private const int RootSamples = 128;
    private const int SubIntervals = 8;

    private static readonly (double Node, double Weight)[] GaussLegendre = GaussLegendreRule(16);

    public static double ArcLength(CurveView curve) => new RailGeometry(curve).Length;

    /// <summary>Signed display curvature at parameter <paramref name="t"/>. Throws on a degenerate tangent: that must never read as straight.</summary>
    public static CurvatureReading CurvatureAt(CurveView curve, double t, CurveSide side = CurveSide.Auto)
    {
        var rail = new RailGeometry(curve);
        return rail.Reading(t, rail.SpanFor(t, side));
    }

    public static StationReading ReadAt(CurveView curve, double t) => new RailGeometry(curve).Station(t);

    /// <summary>The Greville abscissa of control point <paramref name="index"/>: the mean of its three interior knots.</summary>
    public static double Greville(CurveView curve, int index)
    {
        ArgumentNullException.ThrowIfNull(curve);
        var knots = Knots(curve);
        return GrevilleOf(knots, index);
    }

    /// <summary>The reading at the point's Greville station, evaluated at t = xi directly (no eta inversion).</summary>
    public static StationReading ReadAtPoint(CurveView curve, int index) => new RailGeometry(curve).Station(Greville(curve, index));

    public static double ParameterAtEta(CurveView curve, double eta)
    {
        ArgumentNullException.ThrowIfNull(curve);
        var points = new double[curve.Points.Count][];
        for (int i = 0; i < points.Length; i++) points[i] = [curve.Points[i].Eta, curve.Points[i].Ordinate];
        return ChannelEvaluator.Parameter(Knots(curve), 3, points, eta);
    }

    /// <summary>Layer 1 of the monotone count: the indices of the candidates that end a monotone piece, by a zigzag with hysteresis.
    /// A piece ends when the value reverses by more than <paramref name="threshold"/>. The count of pieces is the length plus one.</summary>
    public static IReadOnlyList<int> ReversalIndices(IReadOnlyList<double> candidates, double threshold)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var reversals = new List<int>();
        if (candidates.Count < 2) return reversals;
        int direction = 0, extreme = 0, low = 0, high = 0;
        for (int i = 1; i < candidates.Count; i++)
        {
            double value = candidates[i];
            if (direction == 0)
            {
                if (value < candidates[low]) low = i;
                if (value > candidates[high]) high = i;
                double rise = value - candidates[low], fall = candidates[high] - value;
                if (rise > threshold && rise >= fall) { direction = 1; extreme = i; }
                else if (fall > threshold) { direction = -1; extreme = i; }
            }
            else if (direction > 0)
            {
                if (value >= candidates[extreme]) extreme = i;
                else if (candidates[extreme] - value > threshold) { reversals.Add(extreme); direction = -1; extreme = i; }
            }
            else
            {
                if (value <= candidates[extreme]) extreme = i;
                else if (value - candidates[extreme] > threshold) { reversals.Add(extreme); direction = 1; extreme = i; }
            }
        }
        return reversals;
    }

    /// <summary>The monotone pieces of the signed curvature of one rail. Candidates are the one-sided value at both ends of every
    /// knot span and every root of d(kappa)/dt inside it; the count does not depend on any sampling density.</summary>
    public static MonotoneCount MonotonePieces(CurveView curve)
    {
        var rail = new RailGeometry(curve);
        double threshold = ReversalTau / rail.Length;
        var candidates = new List<(double T, double Kappa)>();
        foreach (int span in rail.Spans)
        {
            double start = rail.Knots[span], end = rail.Knots[span + 1];
            candidates.Add((start, rail.Display(start, span)));
            double previousT = start, previous = rail.Numerator(start, span);
            for (int step = 1; step <= RootSamples; step++)
            {
                double t = start + step / (double)RootSamples * (end - start);
                double value = rail.Numerator(t, span);
                if (step < RootSamples && value == 0) candidates.Add((t, rail.Display(t, span)));
                else if (previous * value < 0)
                {
                    double low = previousT, high = t, lowValue = previous;
                    for (int iteration = 0; iteration < 60 && high - low > 1e-15; iteration++)
                    {
                        double middle = (low + high) / 2, middleValue = rail.Numerator(middle, span);
                        if (lowValue * middleValue <= 0) high = middle; else { low = middle; lowValue = middleValue; }
                    }
                    double root = (low + high) / 2;
                    candidates.Add((root, rail.Display(root, span)));
                }
                previousT = t;
                previous = value;
            }
            candidates.Add((end, rail.Display(end, span)));
        }
        var reversals = ReversalIndices(candidates.Select(c => c.Kappa).ToArray(), threshold);
        var boundaries = reversals.Select(index =>
        {
            var (t, kappa) = candidates[index];
            return new MonotoneBoundary(t, rail.Place(t, rail.SpanFor(t, CurveSide.Auto)).X / rail.HalfSpan, kappa);
        }).ToArray();
        return new(reversals.Count + 1, threshold, rail.Length, boundaries);
    }

    /// <summary>Comb teeth: <paramref name="density"/> teeth at an even arc-length pitch over the whole rail, plus the two one-sided
    /// teeth at every knot of multiplicity two or more where the tangent turns by more than 0.1 degrees or the curvature jumps by more than tau / L.</summary>
    public static IReadOnlyList<RailTooth> Teeth(CurveView curve, int density)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(density, 1);
        var rail = new RailGeometry(curve);
        var entries = new List<(double T, int Order, RailTooth Tooth)>();
        foreach (var (t, span) in rail.EvenArcStations(density))
            entries.Add((t, 1, rail.Tooth(t, span, false, false)));
        foreach (double knot in rail.RepeatedKnots())
        {
            var kind = rail.Station(knot).Kind;
            if (kind is not (StationKind.Corner or StationKind.CurvatureJump)) continue;
            int before = rail.SpanFor(knot, CurveSide.Before), after = rail.SpanFor(knot, CurveSide.After);
            bool corner = kind == StationKind.Corner;
            entries.Add((knot, 0, rail.Tooth(knot, before, false, false)));
            entries.Add((knot, 2, rail.Tooth(knot, after, true, corner)));
        }
        var ordered = entries.OrderBy(e => e.T).ThenBy(e => e.Order).Select(e => e.Tooth).ToArray();
        if (ordered.Length > 0 && !ordered[0].StartsPiece) ordered[0] = ordered[0] with { StartsPiece = true };
        return ordered;
    }

    private static double GrevilleOf(double[] knots, int index)
    {
        double a = knots[index + 1], b = knots[index + 2], c = knots[index + 3];
        return a == b && b == c ? a : (a + b + c) / 3;
    }

    private static (double Node, double Weight)[] GaussLegendreRule(int n)
    {
        var rule = new (double, double)[n];
        for (int i = 0; i < n; i++)
        {
            double x = Math.Cos(Math.PI * (i + 0.75) / (n + 0.5)), derivative = 1;
            for (int iteration = 0; iteration < 100; iteration++)
            {
                double previous = 1, current = x;
                for (int k = 2; k <= n; k++) (previous, current) = (current, ((2 * k - 1) * x * current - (k - 1) * previous) / k);
                derivative = n * (x * current - previous) / (x * x - 1);
                double step = current / derivative;
                x -= step;
                if (Math.Abs(step) < 1e-16) break;
            }
            rule[i] = (x, 2 / ((1 - x * x) * derivative * derivative));
        }
        return rule;
    }

    /// <summary>One rail prepared for evaluation: knots, coordinates in metres, spans, and the Gauss-Legendre length (lazy).</summary>
    private sealed class RailGeometry
    {
        private readonly double[] x, y;
        private readonly double sign;
        private double? length;

        internal RailGeometry(CurveView curve)
        {
            ArgumentNullException.ThrowIfNull(curve);
            Knots = Planform.Knots(curve);
            var last = curve.Points[^1];
            HalfSpan = curve.Points.Count == 0 || curve.Points[0].Eta == 0 && curve.Points[0].SpanMeters == 0
                ? last.SpanMeters : last.SpanMeters / last.Eta;
            x = curve.Points.Select(p => p.Eta * HalfSpan).ToArray();
            y = curve.Points.Select(p => p.Ordinate).ToArray();
            sign = curve.Curve == "trailing" ? -1 : 1;
            Spans = Enumerable.Range(3, Math.Max(0, x.Length - 3)).Where(s => Knots[s] < Knots[s + 1]).ToArray();
        }

        internal double[] Knots { get; }
        internal double HalfSpan { get; }
        internal int[] Spans { get; }

        internal double Length => length ??= Spans.Sum(span => Enumerable.Range(0, SubIntervals).Sum(k =>
            ArcOver(span, Knots[span] + k / (double)SubIntervals * (Knots[span + 1] - Knots[span]),
                Knots[span] + (k + 1) / (double)SubIntervals * (Knots[span + 1] - Knots[span]))));

        internal int SpanFor(double t, CurveSide side)
        {
            if (Spans.Length == 0) throw new InvalidOperationException("The rail has no knot span.");
            if (side == CurveSide.Before)
            {
                foreach (int span in Spans) if (t > Knots[span] && t <= Knots[span + 1]) return span;
                return t <= Knots[Spans[0]] ? Spans[0] : Spans[^1];
            }
            foreach (int span in Spans) if (t >= Knots[span] && t < Knots[span + 1]) return span;
            return t < Knots[Spans[0]] ? Spans[0] : Spans[^1];
        }

        internal (double X, double Y) Place(double t, int span)
        {
            var jet = SplineBasis.Evaluate(Knots, 3, t, span);
            double px = 0, py = 0;
            for (int i = 0; i < x.Length; i++) { px += jet.N[i] * x[i]; py += jet.N[i] * y[i]; }
            return (px, py);
        }

        private (double Dx, double Dy, double Ddx, double Ddy, double Dddx, double Dddy) Derivatives(double t, int span)
        {
            var jet = SplineBasis.Evaluate(Knots, 3, t, span);
            double dx = 0, dy = 0, ddx = 0, ddy = 0, dddx = 0, dddy = 0;
            for (int i = 0; i < x.Length; i++)
            {
                dx += jet.D1[i] * x[i]; dy += jet.D1[i] * y[i];
                ddx += jet.D2[i] * x[i]; ddy += jet.D2[i] * y[i];
                dddx += jet.D3[i] * x[i]; dddy += jet.D3[i] * y[i];
            }
            return (dx, dy, ddx, ddy, dddx, dddy);
        }

        private double Speed(double t, int span)
        {
            var jet = SplineBasis.Evaluate(Knots, 3, t, span);
            double dx = 0, dy = 0;
            for (int i = 0; i < x.Length; i++) { dx += jet.D1[i] * x[i]; dy += jet.D1[i] * y[i]; }
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double ArcOver(int span, double from, double to)
        {
            double half = (to - from) / 2, middle = (to + from) / 2, sum = 0;
            foreach (var (node, weight) in GaussLegendre) sum += weight * Speed(middle + half * node, span);
            return sum * half;
        }

        /// <summary>Raw curvature (x'y'' - y'x'') / speed^3 of the named span's polynomial; a zero tangent throws.</summary>
        private double Raw(double t, int span)
        {
            var d = Derivatives(t, span);
            double speed2 = d.Dx * d.Dx + d.Dy * d.Dy;
            if (speed2 == 0) throw new InvalidOperationException("Curvature is undefined where the tangent is zero (t = " + t.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ").");
            return (d.Dx * d.Ddy - d.Dy * d.Ddx) / (speed2 * Math.Sqrt(speed2));
        }

        internal double Display(double t, int span) => sign * Raw(t, span);

        /// <summary>The numerator of d(kappa)/dt: a polynomial of degree at most 6 on a cubic span.</summary>
        internal double Numerator(double t, int span)
        {
            var d = Derivatives(t, span);
            return (d.Dx * d.Dddy - d.Dy * d.Dddx) * (d.Dx * d.Dx + d.Dy * d.Dy) - 3 * (d.Dx * d.Ddy - d.Dy * d.Ddx) * (d.Dx * d.Ddx + d.Dy * d.Ddy);
        }

        private bool IsStraight(double curvature) => Math.Abs(curvature) * Length * Length / 8 < StraightSagittaMeters;

        internal CurvatureReading Reading(double t, int span)
        {
            double curvature = Display(t, span);
            var place = Place(t, span);
            if (IsStraight(curvature)) return new(t, place.X, place.Y, 0, double.PositiveInfinity, true);
            return new(t, place.X, place.Y, curvature, 1 / Math.Abs(curvature), false);
        }

        internal IEnumerable<double> RepeatedKnots()
        {
            for (int i = 0; i < Knots.Length;)
            {
                double knot = Knots[i];
                int end = i;
                while (end + 1 < Knots.Length && Knots[end + 1] == knot) end++;
                if (end - i + 1 >= 2 && knot > Knots[3] && knot < Knots[^4]) yield return knot;
                i = end + 1;
            }
        }

        internal double TangentJumpDegrees(double knot)
        {
            var left = Derivatives(knot, SpanFor(knot, CurveSide.Before));
            var right = Derivatives(knot, SpanFor(knot, CurveSide.After));
            double cross = left.Dx * right.Dy - left.Dy * right.Dx, dot = left.Dx * right.Dx + left.Dy * right.Dy;
            return Math.Abs(Math.Atan2(cross, dot)) / PlacementRule.RadiansPerDegree;
        }

        internal StationReading Station(double t)
        {
            if (RepeatedKnots().Contains(t))
            {
                var root = Reading(t, SpanFor(t, CurveSide.Before));
                var tip = Reading(t, SpanFor(t, CurveSide.After));
                double angle = TangentJumpDegrees(t);
                bool opposite = !root.Straight && !tip.Straight && root.Curvature * tip.Curvature < 0;
                double jump = Math.Abs(Display(t, SpanFor(t, CurveSide.Before)) - Display(t, SpanFor(t, CurveSide.After)));
                var kind = angle > CornerDegrees ? StationKind.Corner
                    : jump > ReversalTau / Length ? StationKind.CurvatureJump
                    : root.Straight && tip.Straight ? StationKind.Straight : StationKind.Smooth;
                return new(t, kind, root, tip, angle, opposite);
            }
            var reading = Reading(t, SpanFor(t, CurveSide.Auto));
            var single = StationKind.Smooth;
            if (reading.Straight)
            {
                var before = Reading(Math.Max(0, t - InflectionStep), SpanFor(Math.Max(0, t - InflectionStep), CurveSide.Auto));
                var after = Reading(Math.Min(1, t + InflectionStep), SpanFor(Math.Min(1, t + InflectionStep), CurveSide.Auto));
                single = !before.Straight && !after.Straight && before.Curvature * after.Curvature < 0
                    ? StationKind.Inflection : StationKind.Straight;
            }
            return new(t, single, reading, reading, 0, false);
        }

        internal RailTooth Tooth(double t, int span, bool startsPiece, bool corner)
        {
            var d = Derivatives(t, span);
            double speed2 = d.Dx * d.Dx + d.Dy * d.Dy;
            if (speed2 == 0) throw new InvalidOperationException("Curvature is undefined where the tangent is zero.");
            double speed = Math.Sqrt(speed2), raw = (d.Dx * d.Ddy - d.Dy * d.Ddx) / (speed2 * speed);
            var place = Place(t, span);
            double away = raw > 0 ? -1 : 1;
            var reading = Reading(t, span);
            return new(t, place.X, place.Y, away * -d.Dy / speed, away * d.Dx / speed, reading.Curvature, startsPiece, corner);
        }

        /// <summary>The parameters (with their spans) of <paramref name="density"/> stations spaced evenly in arc length over the whole rail.</summary>
        internal IEnumerable<(double T, int Span)> EvenArcStations(int density)
        {
            var edges = new List<(int Span, double From, double To, double Start)>();
            double running = 0;
            foreach (int span in Spans)
                for (int k = 0; k < SubIntervals; k++)
                {
                    double from = Knots[span] + k / (double)SubIntervals * (Knots[span + 1] - Knots[span]);
                    double to = Knots[span] + (k + 1) / (double)SubIntervals * (Knots[span + 1] - Knots[span]);
                    edges.Add((span, from, to, running));
                    running += ArcOver(span, from, to);
                }
            int at = 0;
            for (int tooth = 0; tooth < density; tooth++)
            {
                double target = (tooth + 0.5) / density * running;
                while (at + 1 < edges.Count && edges[at + 1].Start <= target) at++;
                var (span, from, to, start) = edges[at];
                double piece = (at + 1 < edges.Count ? edges[at + 1].Start : running) - start;
                double t = piece == 0 ? from : from + (target - start) / piece * (to - from);
                for (int iteration = 0; iteration < 2; iteration++)
                {
                    double speed = Speed(t, span);
                    if (speed == 0) break;
                    t = Math.Clamp(t - (ArcOver(span, from, t) - (target - start)) / speed, from, to);
                }
                yield return (t, span);
            }
        }
    }
}
