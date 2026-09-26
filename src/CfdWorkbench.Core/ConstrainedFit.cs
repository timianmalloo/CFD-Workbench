using System.Globalization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CfdWorkbench.Core.Tests")]

namespace CfdWorkbench.Core;

/// <summary>Constrained weighted least squares on a fixed basis. The unknown is the ordinate vector.</summary>
public static class ConstrainedFit
{
    /// <summary>Solves min (NP − q)ᵀW(NP − q) + λ PᵀFP subject to A·P = b.</summary>
    public static double[] Solve(double[,] n, double[] q, double[] w, double[,] f, double lambda, double[,] a, double[] b)
    {
        int samples = n.GetLength(0);
        int unknowns = n.GetLength(1);
        int constraints = a.GetLength(0);
        if (q.Length != samples || w.Length != samples || f.GetLength(0) != unknowns || f.GetLength(1) != unknowns
            || a.GetLength(1) != unknowns || b.Length != constraints)
            throw new ArgumentException("Fit system dimensions do not match.");
        if (!double.IsFinite(lambda)) throw new ContractError("GEOMETRY-FIT-SINGULAR");
        int dimension = unknowns + constraints;
        var kkt = new double[dimension, dimension];
        var rhs = new double[dimension];
        for (int row = 0; row < unknowns; row++)
        {
            for (int column = 0; column < unknowns; column++)
            {
                double sum = lambda * f[row, column];
                for (int sample = 0; sample < samples; sample++) sum += n[sample, row] * w[sample] * n[sample, column];
                kkt[row, column] = sum;
            }
            double load = 0;
            for (int sample = 0; sample < samples; sample++) load += n[sample, row] * w[sample] * q[sample];
            rhs[row] = load;
        }
        for (int constraint = 0; constraint < constraints; constraint++)
        {
            for (int column = 0; column < unknowns; column++)
            {
                kkt[unknowns + constraint, column] = a[constraint, column];
                kkt[column, unknowns + constraint] = a[constraint, column];
            }
            rhs[unknowns + constraint] = b[constraint];
        }
        double[] solution = LinearSolver.Solve(kkt, rhs);
        var ordinates = new double[unknowns];
        Array.Copy(solution, ordinates, unknowns);
        return ordinates;
    }

    private static class LinearSolver
    {
        internal static double[] Solve(double[,] matrix, double[] rhs)
        {
            var factor = Factor(matrix);
            double[] solution = factor.Apply(rhs);
            double[] residual = Product(matrix, solution);
            for (int row = 0; row < rhs.Length; row++) residual[row] = rhs[row] - residual[row];
            double[] correction = factor.Apply(residual);
            for (int row = 0; row < solution.Length; row++) solution[row] += correction[row];
            double[] check = Product(matrix, solution);
            double residualNorm = 0, loadNorm = 0;
            for (int row = 0; row < rhs.Length; row++)
            {
                residualNorm = Math.Max(residualNorm, Math.Abs(check[row] - rhs[row]));
                loadNorm = Math.Max(loadNorm, Math.Abs(rhs[row]));
            }
            if (residualNorm > 1e-8 * Math.Max(1, loadNorm)) throw new ContractError("GEOMETRY-FIT-SINGULAR");
            return solution;
        }

        private static Factorization Factor(double[,] source)
        {
            int size = source.GetLength(0);
            var matrix = new double[size, size];
            double scale = 0;
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    double value = source[row, column];
                    if (!double.IsFinite(value)) throw new ContractError("GEOMETRY-FIT-SINGULAR");
                    matrix[row, column] = value;
                    scale = Math.Max(scale, Math.Abs(value));
                }
            if (scale == 0) throw new ContractError("GEOMETRY-FIT-SINGULAR");
            double tolerance = 1e-14 * scale;
            var pivot = new int[size];
            for (int column = 0; column < size; column++)
            {
                int best = column;
                double largest = Math.Abs(matrix[column, column]);
                for (int row = column + 1; row < size; row++)
                {
                    double value = Math.Abs(matrix[row, column]);
                    if (value > largest) { largest = value; best = row; }
                }
                pivot[column] = best;
                if (largest <= tolerance) throw new ContractError("GEOMETRY-FIT-SINGULAR");
                if (best != column)
                    for (int index = 0; index < size; index++)
                        (matrix[column, index], matrix[best, index]) = (matrix[best, index], matrix[column, index]);
                double diagonal = matrix[column, column];
                for (int row = column + 1; row < size; row++)
                {
                    matrix[row, column] /= diagonal;
                    double factor = matrix[row, column];
                    for (int index = column + 1; index < size; index++) matrix[row, index] -= factor * matrix[column, index];
                }
            }
            return new Factorization(matrix, pivot);
        }

        private static double[] Product(double[,] matrix, double[] vector)
        {
            int size = vector.Length;
            var product = new double[size];
            for (int row = 0; row < size; row++)
            {
                double sum = 0;
                for (int column = 0; column < size; column++) sum += matrix[row, column] * vector[column];
                product[row] = sum;
            }
            return product;
        }

        private sealed class Factorization(double[,] matrix, int[] pivot)
        {
            internal double[] Apply(double[] rhs)
            {
                int size = rhs.Length;
                var solution = new double[size];
                Array.Copy(rhs, solution, size);
                for (int column = 0; column < size; column++)
                {
                    int row = pivot[column];
                    if (row != column) (solution[column], solution[row]) = (solution[row], solution[column]);
                }
                for (int row = 0; row < size; row++)
                    for (int column = 0; column < row; column++)
                        solution[row] -= matrix[row, column] * solution[column];
                for (int row = size - 1; row >= 0; row--)
                {
                    for (int column = row + 1; column < size; column++) solution[row] -= matrix[row, column] * solution[column];
                    solution[row] /= matrix[row, row];
                }
                return solution;
            }
        }
    }
}

/// <summary>End condition held while fairing. Stronger values include the weaker pins.</summary>
public enum PreserveEnds
{
    Position = 0,
    Tangency = 1,
    Curvature = 2
}

/// <summary>One fair or rebuild of both profile sides. Curves change only when the result is accepted.</summary>
internal sealed record FairResult(Curve Upper, Curve Lower, double MaxDeviation, bool WithinTolerance, int MonotonePiecesBefore, int MonotonePiecesAfter);

internal static class ProfileFair
{
    private const int FitSamples = 201;
    private const int MeasureSamples = 2001;
    private const int LambdaSteps = 40;

    internal static FairResult Fair(ProfileDefinition profile, double tolerance, PreserveEnds ends)
    {
        var upper = FitSide(profile.Upper, null, null, tolerance, ends);
        var lower = FitSide(profile.Lower, null, null, tolerance, ends);
        return Combine(profile, upper, lower);
    }

    internal static FairResult Rebuild(ProfileDefinition profile, int vertexCount, double tolerance, PreserveEnds ends)
    {
        if (vertexCount < 6 || vertexCount > 32 || profile.Upper.Degree != 5) throw new ContractError("DSL-CURVE");
        double[] knots = ClampedUniformKnots(5, vertexCount);
        // assume: the sides already share x(t), so the rebuilt abscissa is the upper side sampled at the new Greville parameters. A mismatched lower x(t) would be moved onto that abscissa.
        double[] abscissae = MappedAbscissae(profile.Upper, knots);
        var upper = FitSide(profile.Upper, knots, abscissae, tolerance, ends);
        var lower = FitSide(profile.Lower, knots, abscissae, tolerance, ends);
        return Combine(profile, upper, lower);
    }

    internal static double Fairness(Curve curve)
    {
        double[,] smoothing = FairnessMatrix(curve.Knots, curve.Degree, Abscissae(curve));
        return Quadratic(smoothing, Ordinates(curve));
    }

    private readonly record struct SideFit(Curve Curve, double Deviation, int PiecesBefore, int PiecesAfter, bool Accepted);

    private static FairResult Combine(ProfileDefinition profile, SideFit upper, SideFit lower)
    {
        bool accepted = upper.Accepted && lower.Accepted;
        return new FairResult(
            accepted ? upper.Curve : profile.Upper,
            accepted ? lower.Curve : profile.Lower,
            Math.Max(upper.Deviation, lower.Deviation),
            accepted,
            upper.PiecesBefore + lower.PiecesBefore,
            accepted ? upper.PiecesAfter + lower.PiecesAfter : upper.PiecesBefore + lower.PiecesBefore);
    }

    private static SideFit FitSide(Curve curve, double[]? knots, double[]? abscissae, double tolerance, PreserveEnds ends)
    {
        bool rebuild = knots is not null && abscissae is not null;
        double[] origin = Ordinates(curve);
        double[] useKnots = knots ?? curve.Knots;
        double[] useX = abscissae ?? Abscissae(curve);
        int count = useX.Length;
        int before = SignChanges(curve.Knots, curve.Degree, origin);
        var parameters = GrevilleParameters(curve.Degree, FitSamples);
        double[,] basis;
        double[] samples;
        if (rebuild) RebuildDesign(curve, useKnots, useX, parameters, out basis, out samples);
        else SameDesign(curve.Knots, curve.Degree, origin, parameters, out basis, out samples);
        double[,] smoothing = FairnessMatrix(useKnots, rebuild ? 5 : curve.Degree, useX);
        (double[,] constraints, double[] bound) = Pins(origin, count, ends);
        double[] weights = Ones(samples.Length);
        int steps = 0;
        double[]? best = null;
        double bestDeviation = double.PositiveInfinity;
        int bestPieces = before;
        double reported = double.PositiveInfinity;

        bool Consider(double lambda, bool originExact)
        {
            if (steps >= LambdaSteps) return false;
            steps++;
            double[] ordinates;
            if (originExact)
                ordinates = (double[])origin.Clone();
            else
            {
                try { ordinates = ConstrainedFit.Solve(basis, samples, weights, smoothing, lambda, constraints, bound); }
                catch (ContractError error) when (error.Code == "GEOMETRY-FIT-SINGULAR") { return false; }
                if (ordinates.Any(value => !double.IsFinite(value))) return false;
                Snap(ordinates, constraints, bound);
            }
            double deviation = rebuild ? RebuiltGap(curve, useKnots, useX, ordinates) : SameBasisGap(useKnots, curve.Degree, origin, ordinates);
            int pieces = SignChanges(useKnots, rebuild ? 5 : curve.Degree, ordinates);
            if (deviation < reported) reported = deviation;
            if (double.IsFinite(tolerance) && deviation <= tolerance && pieces <= before)
            {
                best = ordinates;
                bestDeviation = deviation;
                bestPieces = pieces;
                return true;
            }
            return false;
        }

        if (!rebuild) Consider(0, true);
        else Consider(0, false);
        double probe = 1e-12;
        double upperLambda = 0;
        bool bracket = false;
        while (steps < LambdaSteps)
        {
            if (Consider(probe, false))
            {
                probe *= 10;
                if (probe > 1e12) break;
            }
            else { upperLambda = probe; bracket = true; break; }
        }
        if (bracket && bestDeviation < double.PositiveInfinity && steps < LambdaSteps)
        {
            double lowerLambda = bestDeviation == 0 && !rebuild ? upperLambda * 1e-12 : Math.Max(probe / 10, upperLambda * 1e-12);
            if (lowerLambda <= 0) lowerLambda = upperLambda * 1e-12;
            while (steps < LambdaSteps && upperLambda > lowerLambda * 1.000001)
            {
                double mid = Math.Exp((Math.Log(lowerLambda) + Math.Log(upperLambda)) / 2);
                if (Consider(mid, false)) lowerLambda = mid;
                else upperLambda = mid;
            }
        }
        if (best is null)
            return new SideFit(curve, double.IsFinite(reported) ? reported : 0, before, before, false);
        // A gap at or below the 1e-12 reproduction floor is the original curve. Reporting it as a shape change is a false measurement.
        if (!rebuild && bestDeviation <= 1e-12)
            return new SideFit(curve, 0, before, before, double.IsFinite(tolerance) && tolerance >= 0);
        bool changed = best.Length != origin.Length;
        for (int index = 0; index < origin.Length && index < best.Length; index++)
            if (best[index] != origin[index]) changed = true;
        Curve result = !changed ? curve : rebuild ? Build(curve, useKnots, useX, best) : ReplaceOrdinates(curve, best);
        return new SideFit(result, bestDeviation, before, bestPieces, true);
    }

    private static void SameDesign(double[] knots, int degree, double[] ordinates, double[] parameters, out double[,] basis, out double[] samples)
    {
        basis = new double[parameters.Length, ordinates.Length];
        samples = new double[parameters.Length];
        for (int row = 0; row < parameters.Length; row++)
        {
            double[] values = SplineBasis.Values(knots, degree, parameters[row]);
            double ordinate = 0;
            for (int column = 0; column < ordinates.Length; column++)
            {
                basis[row, column] = values[column];
                ordinate += values[column] * ordinates[column];
            }
            samples[row] = ordinate;
        }
    }

    private static void RebuildDesign(Curve origin, double[] knots, double[] abscissae, double[] parameters, out double[,] basis, out double[] samples)
    {
        basis = new double[parameters.Length, abscissae.Length];
        samples = new double[parameters.Length];
        double[] originX = Abscissae(origin);
        double[] originY = Ordinates(origin);
        for (int row = 0; row < parameters.Length; row++)
        {
            double[] old = SplineBasis.Values(origin.Knots, origin.Degree, parameters[row]);
            double abscissa = Dot(old, originX);
            samples[row] = Dot(old, originY);
            double parameter = ParameterAt(knots, abscissae, abscissa);
            double[] values = SplineBasis.Values(knots, 5, parameter);
            for (int column = 0; column < abscissae.Length; column++) basis[row, column] = values[column];
        }
    }

    private static (double[,] A, double[] B) Pins(double[] origin, int count, PreserveEnds ends)
    {
        var indexes = new List<int> { 0 };
        if (ends >= PreserveEnds.Position) indexes.Add(count - 1);
        if (ends >= PreserveEnds.Tangency) { indexes.Add(1); indexes.Add(count - 2); }
        if (ends >= PreserveEnds.Curvature) { indexes.Add(2); indexes.Add(count - 3); }
        var constraints = new double[indexes.Count, count];
        var bound = new double[indexes.Count];
        for (int row = 0; row < indexes.Count; row++)
        {
            int index = indexes[row];
            constraints[row, index] = 1;
            bound[row] = index == 0 ? 0 : EndOrdinate(origin, index, count);
        }
        return (constraints, bound);
    }

    private static double EndOrdinate(double[] origin, int index, int count)
    {
        int fromEnd = count - 1 - index;
        if (fromEnd < index) return origin[origin.Length - 1 - fromEnd];
        return origin[Math.Min(index, origin.Length - 1)];
    }

    private static void Snap(double[] ordinates, double[,] constraints, double[] bound)
    {
        for (int row = 0; row < bound.Length; row++)
            for (int column = 0; column < ordinates.Length; column++)
                if (constraints[row, column] == 1) ordinates[column] = bound[row];
    }

    private static double[,] FairnessMatrix(double[] knots, int degree, double[] abscissae)
    {
        if (degree + 1 != GaussLegendre.Order) throw new ContractError("DSL-CURVE");
        int count = abscissae.Length;
        var smoothing = new double[count, count];
        var coefficient = new double[count];
        for (int span = degree; span < count; span++)
        {
            double start = knots[span], end = knots[span + 1];
            if (end <= start) continue;
            double half = 0.5 * (end - start);
            double mid = 0.5 * (start + end);
            for (int node = 0; node < GaussLegendre.Order; node++)
            {
                double parameter = mid + half * GaussLegendre.Nodes[node];
                double weight = GaussLegendre.Weights[node] * half;
                SplineBasis.Jet jet = SplineBasis.Evaluate(knots, degree, parameter);
                double dx = Dot(jet.D1, abscissae);
                double ddx = Dot(jet.D2, abscissae);
                if (dx <= 1e-12) continue;
                double scale = dx * dx * dx;
                for (int index = 0; index < count; index++)
                    coefficient[index] = (dx * jet.D2[index] - ddx * jet.D1[index]) / scale;
                for (int row = 0; row < count; row++)
                {
                    double rowScale = weight * coefficient[row] * dx;
                    for (int column = 0; column < count; column++) smoothing[row, column] += rowScale * coefficient[column];
                }
            }
        }
        return smoothing;
    }

    private static double Quadratic(double[,] smoothing, double[] ordinates)
    {
        double sum = 0;
        for (int row = 0; row < ordinates.Length; row++)
        {
            double product = 0;
            for (int column = 0; column < ordinates.Length; column++) product += smoothing[row, column] * ordinates[column];
            sum += ordinates[row] * product;
        }
        return sum;
    }

    private static double SameBasisGap(double[] knots, int degree, double[] origin, double[] updated)
    {
        double max = 0;
        for (int sample = 0; sample < MeasureSamples; sample++)
        {
            double[] basis = SplineBasis.Values(knots, degree, sample / (double)(MeasureSamples - 1));
            double delta = 0;
            for (int index = 0; index < origin.Length; index++) delta += basis[index] * (updated[index] - origin[index]);
            max = Math.Max(max, Math.Abs(delta));
        }
        return max;
    }

    private static double RebuiltGap(Curve origin, double[] knots, double[] abscissae, double[] ordinates)
    {
        double[] originX = Abscissae(origin);
        double[] originY = Ordinates(origin);
        double max = 0;
        for (int sample = 0; sample < MeasureSamples; sample++)
        {
            double[] old = SplineBasis.Values(origin.Knots, origin.Degree, sample / (double)(MeasureSamples - 1));
            double abscissa = Dot(old, originX);
            double ordinate = Dot(old, originY);
            double parameter = ParameterAt(knots, abscissae, abscissa);
            double fitted = Dot(SplineBasis.Values(knots, 5, parameter), ordinates);
            max = Math.Max(max, Math.Abs(fitted - ordinate));
        }
        return max;
    }

    private static int SignChanges(double[] knots, int degree, double[] ordinates)
    {
        var slope = new double[MeasureSamples];
        double max = 0;
        for (int sample = 0; sample < MeasureSamples; sample++)
        {
            SplineBasis.Jet jet = SplineBasis.Evaluate(knots, degree, sample / (double)(MeasureSamples - 1));
            slope[sample] = Dot(jet.D1, ordinates);
            max = Math.Max(max, Math.Abs(slope[sample]));
        }
        double band = max * 1e-9;
        int changes = 0, sign = 0;
        for (int sample = 0; sample < MeasureSamples; sample++)
        {
            int next = slope[sample] > band ? 1 : slope[sample] < -band ? -1 : 0;
            if (next == 0) continue;
            if (sign != 0 && next != sign) changes++;
            sign = next;
        }
        return changes;
    }

    private static double ParameterAt(double[] knots, double[] abscissae, double target)
    {
        double At(double parameter) => Dot(SplineBasis.Values(knots, 5, parameter), abscissae);
        if (target <= At(0)) return 0;
        if (target >= At(1)) return 1;
        double low = 0, high = 1;
        for (int step = 0; step < 50; step++)
        {
            double mid = 0.5 * (low + high);
            if (At(mid) < target) low = mid;
            else high = mid;
        }
        return 0.5 * (low + high);
    }

    private static double[] MappedAbscissae(Curve guide, double[] knots)
    {
        int count = knots.Length - guide.Degree - 1;
        var abscissae = new double[count];
        double[] guideX = Abscissae(guide);
        for (int index = 0; index < count; index++)
        {
            double sum = 0;
            for (int knot = 1; knot <= 5; knot++) sum += knots[index + knot];
            abscissae[index] = Dot(SplineBasis.Values(guide.Knots, guide.Degree, sum / 5), guideX);
        }
        return abscissae;
    }

    private static double[] GrevilleParameters(int degree, int count)
    {
        double[] knots = ClampedUniformKnots(degree, count);
        var parameters = new double[count];
        for (int index = 0; index < count; index++)
        {
            double sum = 0;
            for (int knot = 1; knot <= degree; knot++) sum += knots[index + knot];
            parameters[index] = sum / degree;
        }
        return parameters;
    }

    private static double[] ClampedUniformKnots(int degree, int count)
    {
        var knots = new double[count + degree + 1];
        int interior = count - degree - 1;
        for (int index = 0; index <= degree; index++) knots[index] = 0;
        for (int index = 1; index <= interior; index++) knots[degree + index] = index / (double)(interior + 1);
        for (int index = knots.Length - degree - 1; index < knots.Length; index++) knots[index] = 1;
        return knots;
    }

    private static Curve ReplaceOrdinates(Curve curve, double[] ordinates)
    {
        var points = new double[ordinates.Length][];
        for (int index = 0; index < ordinates.Length; index++) points[index] = [curve.Points[index][0], ordinates[index]];
        return curve with { Points = points };
    }

    private static Curve Build(Curve template, double[] knots, double[] abscissae, double[] ordinates)
    {
        int count = ordinates.Length;
        var points = new double[count][];
        var ids = new string[count];
        var ordinateTokens = new SourceToken[count];
        var abscissaTokens = new SourceToken[count];
        for (int index = 0; index < count; index++)
        {
            points[index] = [abscissae[index], ordinates[index]];
            ids[index] = "cv-" + index.ToString(CultureInfo.InvariantCulture);
            ordinateTokens[index] = new(ordinates[index].ToString("R", CultureInfo.InvariantCulture), 0, 0);
            abscissaTokens[index] = new(abscissae[index].ToString("R", CultureInfo.InvariantCulture), 0, 0);
        }
        return new Curve(template.Path, 5, knots, points, ids, ordinateTokens, template.InsertAt, true, abscissaTokens, null,
            template.KnotStart, template.KnotEnd, template.PointsStart, template.PointsEnd);
    }

    private static double[] Abscissae(Curve curve)
    {
        var values = new double[curve.Points.Length];
        for (int index = 0; index < values.Length; index++) values[index] = curve.Points[index][0];
        return values;
    }

    private static double[] Ordinates(Curve curve)
    {
        var values = new double[curve.Points.Length];
        for (int index = 0; index < values.Length; index++) values[index] = curve.Points[index][1];
        return values;
    }

    private static double[] Ones(int count)
    {
        var weights = new double[count];
        Array.Fill(weights, 1d);
        return weights;
    }

    private static double Dot(double[] basis, double[] values)
    {
        double sum = 0;
        for (int index = 0; index < values.Length; index++) sum += basis[index] * values[index];
        return sum;
    }

    private static class GaussLegendre
    {
        internal const int Order = 6;
        internal static readonly double[] Nodes =
        [
            -0.9324695142031520278123016, -0.6612093864662645136613996, -0.2386191860831969086305017,
            0.2386191860831969086305017, 0.6612093864662645136613996, 0.9324695142031520278123016
        ];
        internal static readonly double[] Weights =
        [
            0.1713244923791703450402961, 0.3607615730481386075698335, 0.4679139345726910473898703,
            0.4679139345726910473898703, 0.3607615730481386075698335, 0.1713244923791703450402961
        ];
    }
}

internal static class SplineBasis
{
    internal readonly record struct Jet(double[] N, double[] D1, double[] D2);

    internal static double[] Values(double[] knots, int degree, double t) => Evaluate(knots, degree, t).N;

    internal static Jet Evaluate(double[] knots, int degree, double t)
    {
        int count = knots.Length - degree - 1;
        int span = FindSpan(knots, degree, count, t);
        var ndu = new double[degree + 1, degree + 1];
        var left = new double[degree + 1];
        var right = new double[degree + 1];
        ndu[0, 0] = 1;
        for (int level = 1; level <= degree; level++)
        {
            left[level] = t - knots[span + 1 - level];
            right[level] = knots[span + level] - t;
            double saved = 0;
            for (int row = 0; row < level; row++)
            {
                ndu[level, row] = right[row + 1] + left[level - row];
                double temp = ndu[row, level - 1] / ndu[level, row];
                ndu[row, level] = saved + right[row + 1] * temp;
                saved = left[level - row] * temp;
            }
            ndu[level, level] = saved;
        }
        var derivatives = new double[3, degree + 1];
        for (int column = 0; column <= degree; column++) derivatives[0, column] = ndu[column, degree];
        var alternates = new double[2, degree + 1];
        for (int function = 0; function <= degree; function++)
        {
            int first = 0, second = 1;
            alternates[0, 0] = 1;
            for (int derivative = 1; derivative <= 2; derivative++)
            {
                double value = 0;
                int rk = function - derivative;
                int pk = degree - derivative;
                if (function >= derivative)
                {
                    alternates[second, 0] = alternates[first, 0] / ndu[pk + 1, rk];
                    value = alternates[second, 0] * ndu[rk, pk];
                }
                int start = rk >= -1 ? 1 : -rk;
                int finish = function - 1 <= pk ? derivative - 1 : degree - function;
                for (int column = start; column <= finish; column++)
                {
                    alternates[second, column] = (alternates[first, column] - alternates[first, column - 1]) / ndu[pk + 1, rk + column];
                    value += alternates[second, column] * ndu[rk + column, pk];
                }
                if (function <= pk)
                {
                    alternates[second, derivative] = -alternates[first, derivative - 1] / ndu[pk + 1, function];
                    value += alternates[second, derivative] * ndu[function, pk];
                }
                derivatives[derivative, function] = value;
                (first, second) = (second, first);
            }
        }
        int scale = degree;
        for (int derivative = 1; derivative <= 2; derivative++)
        {
            for (int column = 0; column <= degree; column++) derivatives[derivative, column] *= scale;
            scale *= degree - derivative;
        }
        var n = new double[count];
        var d1 = new double[count];
        var d2 = new double[count];
        for (int column = 0; column <= degree; column++)
        {
            int index = span - degree + column;
            n[index] = derivatives[0, column];
            d1[index] = derivatives[1, column];
            d2[index] = derivatives[2, column];
        }
        return new Jet(n, d1, d2);
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
