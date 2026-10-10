using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Rail comb, Core layer (docs/design/rail-comb.md section 9, G1 and G2). Ring: fast (pure Core, no I/O beyond three
// fixture reads); cost: well under 1 s for the whole class.
internal static class RailCurvatureTests
{
    private const double Tau = Planform.ReversalTau;
    private const string Example = "docs/examples/foildsl/foil-basic.foil";
    private static readonly double[] OnePiece = [0, 0, 0, 0, 1, 1, 1, 1];
    private static readonly double[] TwoPieces = [0, 0, 0, 0, 0.5, 0.5, 0.5, 1, 1, 1, 1];

    private static void Near(double expected, double actual, double tolerance)
    {
        if (!(Math.Abs(expected - actual) <= tolerance))
            throw new InvalidOperationException($"Expected {expected} within {tolerance}; actual {actual}");
    }

    private static CurveView Rail(string name, double[] knots, params (double X, double Y)[] pts)
    {
        double halfSpan = pts[^1].X;
        var points = pts.Select((p, i) => new PointView(name, "p" + i, i, p.X / halfSpan, p.X, p.Y, PointRole.Control, null, null,
            PointFreedom.Free, [])).ToArray();
        return new CurveView(name, 10, knots, points, []);
    }

    private static (double X, double Y)[] Parabola => [(-1, 1), (-1.0 / 3, -1.0 / 3), (1.0 / 3, -1.0 / 3), (1, 1)];

    /// <summary>Raw (x'y'' - y'x'') / speed^3 of a cubic Bezier at local u, from the Bernstein derivatives alone.</summary>
    private static double BezierKappa((double X, double Y)[] p, double u)
    {
        double a = (1 - u) * (1 - u), b = 2 * (1 - u) * u, c = u * u;
        double dx = 3 * (a * (p[1].X - p[0].X) + b * (p[2].X - p[1].X) + c * (p[3].X - p[2].X));
        double dy = 3 * (a * (p[1].Y - p[0].Y) + b * (p[2].Y - p[1].Y) + c * (p[3].Y - p[2].Y));
        double ddx = 6 * ((1 - u) * (p[2].X - 2 * p[1].X + p[0].X) + u * (p[3].X - 2 * p[2].X + p[1].X));
        double ddy = 6 * ((1 - u) * (p[2].Y - 2 * p[1].Y + p[0].Y) + u * (p[3].Y - 2 * p[2].Y + p[1].Y));
        return (dx * ddy - dy * ddx) / Math.Pow(dx * dx + dy * dy, 1.5);
    }

    private static (double X, double Y) At((double X, double Y)[] p, double u)
    {
        double a = (1 - u) * (1 - u) * (1 - u), b = 3 * (1 - u) * (1 - u) * u, c = 3 * (1 - u) * u * u, d = u * u * u;
        return (a * p[0].X + b * p[1].X + c * p[2].X + d * p[3].X, a * p[0].Y + b * p[1].Y + c * p[2].Y + d * p[3].Y);
    }

    /// <summary>Two cubic Beziers sharing P3 (a triple-knot anchor). The second piece's first handle is turned by <paramref name="turnDegrees"/>.</summary>
    private static (CurveView Curve, (double X, double Y)[] First, (double X, double Y)[] Second) Joined(double turnDegrees, double secondBend)
    {
        var first = new (double X, double Y)[] { (0, 0), (0.1, 0.05), (0.3, 0.1), (0.5, 0.1) };
        double turn = turnDegrees * Math.PI / 180;
        var second = new (double X, double Y)[] { (0.5, 0.1), (0.5 + 0.2 * Math.Cos(turn), 0.1 + 0.2 * Math.Sin(turn)),
            (0.75, 0.1 + secondBend), (1, 0.1 + secondBend) };
        var points = new[] { first[0], first[1], first[2], first[3], second[1], second[2], second[3] };
        return (Rail("leading", TwoPieces, points), first, second);
    }

    /// <summary>Layer 1 candidates: the end values and every local extremum of a sampled analytic profile.</summary>
    private static List<double> Extrema(Func<double, double> f)
    {
        const int n = 40000;
        var values = Enumerable.Range(0, n + 1).Select(i => f(i / (double)n)).ToArray();
        var list = new List<double> { values[0] };
        for (int i = 1; i < n; i++)
            if ((values[i] - values[i - 1]) * (values[i + 1] - values[i]) < 0) list.Add(values[i]);
        list.Add(values[n]);
        return list;
    }

    private static int Pieces(IReadOnlyList<double> candidates, double threshold) => Planform.ReversalIndices(candidates, threshold).Count + 1;

    private static double Profile(double s, double amplitude, double width) =>
        2 * Math.Pow(s, 4) + amplitude * Math.Exp(-Math.Pow((s - 0.5) / width, 2));

    private static CurveView Scale(CurveView rail, double k) => rail with
    {
        Points = rail.Points.Select(p => p with { SpanMeters = p.SpanMeters * k, Ordinate = p.Ordinate * k }).ToArray()
    };

    internal static void Run()
    {
        Check("SplineBasis_D3_MatchesBernsteinThirdDerivative_AndFiniteDifferenceOfD2", () =>
        {
            double[] knots = [0, 0, 0, 0, 0.3, 0.3, 0.7, 1, 1, 1, 1];
            foreach (double t in new[] { 0.1, 0.5, 0.9 })
            {
                var jet = SplineBasis.Evaluate(knots, 3, t);
                double h = 1e-6;
                var plus = SplineBasis.Evaluate(knots, 3, t + h);
                var minus = SplineBasis.Evaluate(knots, 3, t - h);
                for (int i = 0; i < jet.D3.Length; i++)
                    Near((plus.D2[i] - minus.D2[i]) / (2 * h), jet.D3[i], 1e-5 * Math.Max(1, Math.Abs(jet.D3[i])));
            }
            var bezier = SplineBasis.Evaluate(OnePiece, 3, 0.4);
            double[] ys = [1, -2, 5, 3];
            double third = 6 * (ys[3] - 3 * ys[2] + 3 * ys[1] - ys[0]);
            Near(third, Enumerable.Range(0, 4).Sum(i => bezier.D3[i] * ys[i]), 1e-9);
        });
        Check("Planform_CurvatureAt_Circle_RadiusWithin1e-9", () =>
        {
            // The parabola y = x^2 is an exact cubic; its vertex osculating circle has radius 1/2.
            var reading = Planform.CurvatureAt(Rail("leading", OnePiece, Parabola), 0.5);
            Near(0.5, reading.RadiusMeters, 1e-9);
            Near(2, reading.Curvature, 1e-9);
            Equal(false, reading.Straight);
        });
        Check("Planform_CurvatureAt_Sign_LeadingPositiveTrailingNegative", () =>
        {
            Near(2, Planform.CurvatureAt(Rail("leading", OnePiece, Parabola), 0.5).Curvature, 1e-9);
            Near(-2, Planform.CurvatureAt(Rail("trailing", OnePiece, Parabola), 0.5).Curvature, 1e-9);
        });
        Check("Planform_CurvatureAt_StraightRun_ReportsStraight", () =>
        {
            var rail = Rail("trailing", OnePiece, (0, 0), (0.1, 0.01), (0.3, 0.03), (0.4, 0.04));
            var reading = Planform.CurvatureAt(rail, 0.37);
            Equal(true, reading.Straight);
            Equal(0.0, reading.Curvature);
            Equal(true, double.IsPositiveInfinity(reading.RadiusMeters));
        });
        Check("Planform_CurvatureAt_StraightTolerance_TenMicrometreSagitta", () =>
        {
            // y = c x^2 on [0, 1]: |k| L^2 / 8 is about c / 4 metres, against 10 micrometres.
            (double X, double Y)[] Arc(double c) => [(0, 0), (1.0 / 3, 0), (2.0 / 3, c / 3), (1, c)];
            var shallow = Planform.CurvatureAt(Rail("leading", OnePiece, Arc(2e-5)), 0.5);
            var deeper = Planform.CurvatureAt(Rail("leading", OnePiece, Arc(8e-5)), 0.5);
            Equal(true, shallow.Straight);
            Equal(false, deeper.Straight);
            Equal(true, deeper.Curvature > 0);
            Near(1 / deeper.Curvature, deeper.RadiusMeters, 1e-9 * deeper.RadiusMeters);
        });
        Check("Planform_CurvatureAt_DegenerateTangent_Throws", () =>
        {
            var rail = Rail("leading", OnePiece, (0, 0), (0, 0), (0.5, 0.2), (1, 0.1));
            bool threw = false;
            try { Planform.CurvatureAt(rail, 0); }
            catch (InvalidOperationException) { threw = true; }
            Equal(true, threw);
            Equal(true, Math.Abs(Planform.CurvatureAt(rail, 0.5).Curvature) > 0);
        });
        Check("Planform_CurvatureAt_AnchorOneSided_BothSides", () =>
        {
            var (curve, first, second) = Joined(0, 0.18);
            var reading = Planform.ReadAt(curve, 0.5);
            Near(BezierKappa(first, 1), reading.Root.Curvature, 1e-9);
            Near(BezierKappa(second, 0), reading.Tip.Curvature, 1e-9);
            Equal(true, Math.Abs(reading.Root.Curvature - reading.Tip.Curvature) > 1);
            Equal(true, reading.SignChanges);
            Equal(StationKind.CurvatureJump, reading.Kind);
            var same = Planform.ReadAt(Joined(0, -0.18).Curve, 0.5);
            Equal(false, same.SignChanges);
            Near(BezierKappa(first, 0.25), Planform.ReadAt(curve, 0.125).Root.Curvature, 1e-9);
            Equal(Planform.ReadAt(curve, 0.125).Root, Planform.ReadAt(curve, 0.125).Tip);
        });
        Check("Planform_CurvatureAt_CornerClassifiedByAngle_0p1Degrees", () =>
        {
            var smooth = Planform.ReadAt(Joined(0.05, 0.18).Curve, 0.5);
            var corner = Planform.ReadAt(Joined(0.2, 0.18).Curve, 0.5);
            Near(0.05, smooth.TangentJumpDegrees, 1e-6);
            Near(0.2, corner.TangentJumpDegrees, 1e-6);
            Equal(StationKind.CurvatureJump, smooth.Kind);
            Equal(StationKind.Corner, corner.Kind);
        });
        Check("Planform_CurvatureAt_GrevilleAtT_EqualsXi", () =>
        {
            var (curve, first, second) = Joined(0, 0.18);
            double[] knots = TwoPieces;
            for (int index = 0; index < 7; index++)
            {
                double xi = (knots[index + 1] + knots[index + 2] + knots[index + 3]) / 3;
                Near(xi, Planform.Greville(curve, index), 1e-15);
                var reading = Planform.ReadAtPoint(curve, index);
                Near(xi, reading.T, 1e-15);
                if (xi == 0.5) continue;
                var place = xi < 0.5 ? At(first, xi / 0.5) : At(second, (xi - 0.5) / 0.5);
                Near(place.X, reading.Root.SpanMeters, 1e-12);
                Near(place.Y, reading.Root.Aft, 1e-12);
            }
        });
        Check("Planform_CurvatureAt_ArcLength_GaussLegendre", () =>
        {
            double exact = Math.Sqrt(5) + Math.Asinh(2) / 2;
            Near(exact, Planform.ArcLength(Rail("leading", OnePiece, Parabola)), 1e-12);
            var (curve, first, second) = Joined(0, 0.18);
            double polyline = 0;
            foreach (var piece in new[] { first, second })
                for (int i = 0; i < 20000; i++)
                {
                    var a = At(piece, i / 20000.0);
                    var b = At(piece, (i + 1) / 20000.0);
                    polyline += Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                }
            Near(polyline, Planform.ArcLength(curve), 1e-8);
        });
        Check("Planform_Teeth_PitchConstantWithin5Percent", () =>
        {
            var teeth = Planform.Teeth(Rail("leading", OnePiece, Parabola), 32);
            Equal(32, teeth.Count);
            var pitch = Enumerable.Range(1, teeth.Count - 1).Select(i =>
                Math.Sqrt(Math.Pow(teeth[i].SpanMeters - teeth[i - 1].SpanMeters, 2) + Math.Pow(teeth[i].Ordinate - teeth[i - 1].Ordinate, 2))).ToArray();
            Equal(true, (pitch.Max() - pitch.Min()) / pitch.Average() < 0.05);
        });
        Check("Planform_Teeth_NoJump_NoExtraTeeth_AtARepeatedKnot", () =>
        {
            // The parabola cut at its vertex into two cubic Beziers: a triple-knot anchor whose tangent and curvature are continuous.
            var halves = Rail("leading", TwoPieces, (-1, 1), (-2.0 / 3, 1.0 / 3), (-1.0 / 3, 0), (0, 0), (1.0 / 3, 0), (2.0 / 3, 1.0 / 3), (1, 1));
            var station = Planform.ReadAt(halves, 0.5);
            Equal(StationKind.Smooth, station.Kind);
            var teeth = Planform.Teeth(halves, 16);
            Equal(16, teeth.Count);
            Equal(1, teeth.Count(t => t.StartsPiece));
        });
        Check("Planform_Teeth_PointAwayFromCentre_AndSplitPiecesAtAJump", () =>
        {
            var vertex = Planform.Teeth(Rail("trailing", OnePiece, Parabola), 33)[16];
            Near(0, vertex.DirSpan, 1e-6);
            Near(-1, vertex.DirAft, 1e-6);
            var (curve, _, _) = Joined(0, 0.18);
            var teeth = Planform.Teeth(curve, 16);
            Equal(18, teeth.Count);
            Equal(2, teeth.Count(t => t.StartsPiece));
            var pair = teeth.Where(t => Math.Abs(t.SpanMeters - 0.5) < 1e-12).OrderBy(t => t.T).ToArray();
            Equal(2, pair.Length);
            Equal(false, pair[0].StartsPiece);
            Equal(true, pair[1].StartsPiece);
            Equal(true, Math.Abs(pair[0].Curvature - pair[1].Curvature) > 1);
        });
        Check("Planform_ReversalIndices_HysteresisZigzag", () =>
        {
            Equal(0, Planform.ReversalIndices([1.0], 0.1).Count);
            Equal(1, Pieces([0, 1, 2, 3], 0.5));
            Equal(2, Pieces([0, 1, 0.2, 0.25], 0.5));
            Equal(1, Pieces([0, 1, 0.8, 1.5], 0.5));
            Equal(3, Pieces([2, 0, 1.5, 0.5], 0.5));
        });
        Check("Planform_MonotonePieces_StraightRun_One", () =>
        {
            var rail = Rail("leading", OnePiece, (0, 0), (0.1, 0.02), (0.3, 0.06), (0.4, 0.08));
            Equal(1, Planform.MonotonePieces(rail).Count);
            Equal(1, Planform.MonotonePieces(Rail("trailing", TwoPieces, (0, 0), (0.1, 0), (0.2, 0), (0.3, 0), (0.4, 0), (0.5, 0), (0.6, 0))).Count);
        });
        Check("Planform_MonotonePieces_MonotoneTip_One", () =>
        {
            var candidates = Extrema(s => 2 * Math.Pow(s, 4));
            Equal(1, Pieces(candidates, 1e-12));
            Equal(1, Pieces(candidates, Tau));
        });
        Check("Planform_MonotonePieces_InteriorPeak_Two", () =>
        {
            var result = Planform.MonotonePieces(Rail("leading", OnePiece, Parabola));
            Equal(2, result.Count);
            Near(0.5, result.Boundaries.Single().T, 1e-6);
            Near(0, result.Boundaries.Single().Eta, 1e-6);
            Near(2.9578857, result.ArcLengthMeters, 1e-6);
            Near(Tau / result.ArcLengthMeters, result.ThresholdPerMeter, 1e-15);
            var analytic = Extrema(s => 1 / Math.Pow(1 + 4 * Math.Pow(2 * s - 1, 2), 1.5));
            Equal(2, Pieces(analytic, 1e-12));
            Equal(2, Pieces(analytic, Tau));
        });
        Check("Planform_MonotonePieces_TipRoundPlusWobble_Three", () =>
        {
            var candidates = Extrema(s => Profile(s, 0.20, 0.06));
            Equal(4, candidates.Count);
            Near(0.3298, candidates[1], 1e-4);
            Near(0.2601, candidates[2], 1e-4);
            Equal(3, Pieces(candidates, 1e-12));
            Equal(3, Pieces(candidates, Tau));
        });
        Check("Planform_MonotonePieces_ThresholdEdges", () =>
        {
            var candidates = Extrema(s => Profile(s, 0.15, 0.06));
            Equal(3, Pieces(candidates, 0.5 * Tau));
            Equal(3, Pieces(candidates, Tau));
            Equal(1, Pieces(candidates, 2 * Tau));
        });
        Check("Planform_MonotonePieces_ScaleInvariant", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Example), "accepted", 1);
            foreach (var rail in new[] { view.Leading, view.Trailing })
            {
                int baseline = Planform.MonotonePieces(rail).Count;
                Equal(baseline, Planform.MonotonePieces(Scale(rail, 0.3)).Count);
                Equal(baseline, Planform.MonotonePieces(Scale(rail, 2)).Count);
            }
            Near(Planform.ArcLength(view.Leading) * 2, Planform.ArcLength(Scale(view.Leading, 2)), 1e-12);
            var (curve, _, _) = Joined(0, 0.18);
            Equal(Planform.MonotonePieces(curve).Count, Planform.MonotonePieces(Scale(curve, 0.3)).Count);
        });
        Check("Planform_MonotonePieces_IndependentOfDensity", () =>
        {
            Func<double, double> narrow = s => Profile(s, 0.20, 0.01);
            var sampledAt16 = Enumerable.Range(0, 16).Select(i => narrow((i + 0.5) / 16)).ToArray();
            Equal(1, Pieces(sampledAt16, Tau));
            Equal(3, Pieces(Extrema(narrow), Tau));
        });
        Check("Planform_MonotonePieces_AnchorJump_AgainstTrend_Splits", () =>
        {
            Equal(2, Pieces([0, 0.5, 0.5 - 3 * Tau, 0.5 - 3 * Tau + 0.005], Tau));
        });
        Check("Planform_MonotonePieces_AnchorJump_WithTrend_DoesNotSplit", () =>
        {
            Equal(1, Pieces([0, 0.5, 0.5 + 3 * Tau, 1.0], Tau));
        });
        Check("Planform_MonotonePieces_ReversalAtSimpleKnot_Counted", () =>
        {
            // Mirror-symmetric points about the simple knot 0.5: the curvature extremum sits on the knot.
            double[] knots = [0, 0, 0, 0, 0.5, 1, 1, 1, 1];
            var rail = Rail("leading", knots, (0, 0), (0.25, 0.1), (0.5, 0.35), (0.75, 0.1), (1, 0));
            var result = Planform.MonotonePieces(rail);
            Equal(2, result.Count);
            Near(0.5, result.Boundaries.Single().T, 1e-6);
        });
        Check("Planform_MonotonePieces_MirrorInvariant", () =>
        {
            var view = Planform.View(File.ReadAllBytes(Example), "accepted", 1);
            foreach (var rail in new[] { view.Leading, view.Trailing })
            {
                string other = rail.Curve == "leading" ? "trailing" : "leading";
                var mirrored = rail with { Curve = other, Points = rail.Points.Select(p => p with { Curve = other, Ordinate = -p.Ordinate }).ToArray() };
                Equal(Planform.MonotonePieces(rail).Count, Planform.MonotonePieces(mirrored).Count);
                Near(Planform.CurvatureAt(rail, 0.4).Curvature, Planform.CurvatureAt(mirrored, 0.4).Curvature, 1e-9);
            }
        });
    }
}
