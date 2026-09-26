using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class FitTests
{
    internal static void Run()
    {
        Check("Solve_ExactPolynomial_ReproducesControlOrdinates", ExactPolynomial);
        Check("Solve_EqualityConstraints_HoldAndSingularRefuses", ConstraintsAndSingular);
        Check("Fair_ExampleWithinTolerance_KeepsShapeContract", FairWithinTolerance);
        Check("Fair_TinyTolerance_DoesNotAcceptAShapeChange", TinyTolerance);
        Check("Rebuild_TenVertices_SharesKnotsAbscissaAndPins", RebuildTen);
        Check("Fair_Tangency_KeepsSecondAndPenultimateOrdinates", TangencyPins);
    }

    private static void ExactPolynomial()
    {
        const int degree = 5, count = 8, samples = 24;
        double[] knots = ClampedUniform(degree, count);
        double[] known = [0, 0.02, 0.06, 0.05, 0.04, 0.025, 0.01, 0];
        var design = new double[count][];
        for (int i = 0; i < count; i++) design[i] = [i / (double)(count - 1), known[i]];
        var n = new double[samples, count];
        var q = new double[samples];
        var w = new double[samples];
        for (int s = 0; s < samples; s++)
        {
            double t = s / (double)(samples - 1);
            double[] basis = BasisValues(knots, degree, t);
            double[] jet = SplineBasis.Values(knots, degree, t);
            w[s] = 1;
            double ordinate = 0;
            for (int i = 0; i < count; i++)
            {
                if (Math.Abs(basis[i] - jet[i]) > 1e-12)
                    throw new InvalidOperationException($"Basis mismatch {basis[i]} vs {jet[i]} at t {t}");
                n[s, i] = basis[i];
                ordinate += basis[i] * known[i];
            }
            q[s] = ordinate;
            double evaluated = DeBoor(design, knots, degree, t)[1];
            if (Math.Abs(evaluated - ordinate) > 1e-12)
                throw new InvalidOperationException($"De Boor mismatch {evaluated} vs {ordinate}");
        }
        var smoothing = new double[count, count];
        for (int i = 0; i < count; i++) smoothing[i, i] = 1;
        double[] solved = ConstrainedFit.Solve(n, q, w, smoothing, 0, new double[0, count], []);
        for (int i = 0; i < count; i++)
            if (Math.Abs(solved[i] - known[i]) > 1e-12)
                throw new InvalidOperationException($"Ordinate {i} is {solved[i]}, expected {known[i]}");
    }

    private static void ConstraintsAndSingular()
    {
        const int degree = 5, count = 8, samples = 24;
        double[] knots = ClampedUniform(degree, count);
        double[] known = [0, 0.03, 0.07, 0.06, 0.05, 0.02, 0.01, 0];
        var n = new double[samples, count];
        var q = new double[samples];
        var w = new double[samples];
        for (int s = 0; s < samples; s++)
        {
            double[] basis = BasisValues(knots, degree, s / (double)(samples - 1));
            w[s] = 1;
            for (int i = 0; i < count; i++)
            {
                n[s, i] = basis[i];
                q[s] += basis[i] * known[i];
            }
        }
        var smoothing = new double[count, count];
        for (int i = 0; i < count; i++) smoothing[i, i] = 1;
        var constraints = new double[2, count];
        constraints[0, 3] = 1;
        constraints[1, 6] = 1;
        double[] bound = [known[3], known[6]];
        double[] solved = ConstrainedFit.Solve(n, q, w, smoothing, 0.25, constraints, bound);
        for (int row = 0; row < bound.Length; row++)
        {
            double value = 0;
            for (int column = 0; column < count; column++) value += constraints[row, column] * solved[column];
            if (Math.Abs(value - bound[row]) > 1e-12)
                throw new InvalidOperationException($"Constraint residual {value - bound[row]}");
        }
        Refuses("GEOMETRY-FIT-SINGULAR", () =>
            ConstrainedFit.Solve(new double[4, 3], new double[4], [1, 1, 1, 1], new double[3, 3], 0, new double[0, 3], []));
    }

    private static void FairWithinTolerance()
    {
        ProfileDefinition profile = Example();
        double upperFairness = ProfileFair.Fairness(profile.Upper);
        double lowerFairness = ProfileFair.Fairness(profile.Lower);
        FairResult result = ProfileFair.Fair(profile, 1e-4, PreserveEnds.Position);
        if (result.MaxDeviation > 1e-4)
            throw new InvalidOperationException($"Max deviation {result.MaxDeviation}");
        Equal(true, result.WithinTolerance);
        Equal(true, result.MonotonePiecesAfter <= result.MonotonePiecesBefore);
        Equal(true, ProfileFair.Fairness(result.Upper) <= upperFairness + 1e-6 * (1 + Math.Abs(upperFairness)));
        Equal(true, ProfileFair.Fairness(result.Lower) <= lowerFairness + 1e-6 * (1 + Math.Abs(lowerFairness)));
        Equal(0d, result.Upper.Points[0][1]);
        Equal(0d, result.Lower.Points[0][1]);
        Equal(profile.Upper.Points[^1][1], result.Upper.Points[^1][1]);
        Equal(profile.Lower.Points[^1][1], result.Lower.Points[^1][1]);
        SameKnotsAndAbscissae(profile.Upper, result.Upper);
        SameKnotsAndAbscissae(profile.Lower, result.Lower);
    }

    private static void TinyTolerance()
    {
        ProfileDefinition profile = Example();
        FairResult result = ProfileFair.Fair(profile, 1e-15, PreserveEnds.Position);
        bool unchanged = SameCurve(profile.Upper, result.Upper) && SameCurve(profile.Lower, result.Lower);
        if (!result.WithinTolerance)
        {
            if (!unchanged) throw new InvalidOperationException("A rejected fair changed the curves");
            return;
        }
        if (result.MaxDeviation != 0 || !unchanged)
            throw new InvalidOperationException($"Expected an unchanged curve, deviation {result.MaxDeviation}");
    }

    private static void RebuildTen()
    {
        ProfileDefinition profile = Example();
        FairResult result = ProfileFair.Rebuild(profile, 10, 1e-2, PreserveEnds.Position);
        Equal(10, result.Upper.Points.Length);
        Equal(10, result.Lower.Points.Length);
        Equal(true, double.IsFinite(result.MaxDeviation) && result.MaxDeviation >= 0);
        SameKnotsAndAbscissae(result.Upper, result.Lower);
        Equal(0d, result.Upper.Points[0][1]);
        Equal(0d, result.Lower.Points[0][1]);
        Equal(profile.Upper.Points[^1][1], result.Upper.Points[^1][1]);
        Equal(profile.Lower.Points[^1][1], result.Lower.Points[^1][1]);
    }

    private static void TangencyPins()
    {
        ProfileDefinition profile = Example();
        FairResult result = ProfileFair.Fair(profile, 1e-2, PreserveEnds.Tangency);
        Equal(profile.Upper.Points[1][1], result.Upper.Points[1][1]);
        Equal(profile.Upper.Points[^2][1], result.Upper.Points[^2][1]);
        Equal(profile.Lower.Points[1][1], result.Lower.Points[1][1]);
        Equal(profile.Lower.Points[^2][1], result.Lower.Points[^2][1]);
    }

    private static ProfileDefinition Example()
    {
        var parsed = FoilSource.Parse(FoilSourceTests.Example);
        return parsed.Definition!.Profiles[0];
    }

    private static void SameKnotsAndAbscissae(Curve left, Curve right)
    {
        Equal(left.Knots.Length, right.Knots.Length);
        Equal(left.Points.Length, right.Points.Length);
        for (int i = 0; i < left.Knots.Length; i++) Equal(left.Knots[i], right.Knots[i]);
        for (int i = 0; i < left.Points.Length; i++) Equal(left.Points[i][0], right.Points[i][0]);
    }

    private static bool SameCurve(Curve left, Curve right)
    {
        if (left.Knots.Length != right.Knots.Length || left.Points.Length != right.Points.Length) return false;
        for (int i = 0; i < left.Knots.Length; i++) if (left.Knots[i] != right.Knots[i]) return false;
        for (int i = 0; i < left.Points.Length; i++)
            if (left.Points[i][0] != right.Points[i][0] || left.Points[i][1] != right.Points[i][1]) return false;
        return true;
    }

    private static double[] ClampedUniform(int degree, int count)
    {
        var knots = new double[count + degree + 1];
        int interior = count - degree - 1;
        for (int i = 0; i <= degree; i++) knots[i] = 0;
        for (int j = 1; j <= interior; j++) knots[degree + j] = j / (double)(interior + 1);
        for (int i = knots.Length - degree - 1; i < knots.Length; i++) knots[i] = 1;
        return knots;
    }

    private static double[] BasisValues(double[] knots, int degree, double t)
    {
        int count = knots.Length - degree - 1;
        var result = new double[count];
        int span = degree;
        if (t >= knots[count]) span = count - 1;
        else
        {
            int low = degree, high = count, mid = (low + high) / 2;
            while (t < knots[mid] || t >= knots[mid + 1])
            {
                if (t < knots[mid]) high = mid;
                else low = mid;
                mid = (low + high) / 2;
            }
            span = mid;
        }
        var basis = new double[degree + 1];
        var left = new double[degree + 1];
        var right = new double[degree + 1];
        basis[0] = 1;
        for (int j = 1; j <= degree; j++)
        {
            left[j] = t - knots[span + 1 - j];
            right[j] = knots[span + j] - t;
            double saved = 0;
            for (int r = 0; r < j; r++)
            {
                double temp = basis[r] / (right[r + 1] + left[j - r]);
                basis[r] = saved + right[r + 1] * temp;
                saved = left[j - r] * temp;
            }
            basis[j] = saved;
        }
        for (int j = 0; j <= degree; j++) result[span - degree + j] = basis[j];
        return result;
    }

    private static double[] DeBoor(double[][] points, double[] knots, int degree, double t)
    {
        if (t >= 1) return points[^1];
        int span = degree;
        while (span + 1 < points.Length && knots[span + 1] <= t) span++;
        var values = Enumerable.Range(0, degree + 1).Select(j => (double[])points[span - degree + j].Clone()).ToArray();
        for (int level = 1; level <= degree; level++)
            for (int j = degree; j >= level; j--)
            {
                int index = span - degree + j;
                double width = knots[index + degree - level + 1] - knots[index];
                double alpha = width > 0 ? (t - knots[index]) / width : 0;
                values[j][0] = (1 - alpha) * values[j - 1][0] + alpha * values[j][0];
                values[j][1] = (1 - alpha) * values[j - 1][1] + alpha * values[j][1];
            }
        return values[degree];
    }
}
