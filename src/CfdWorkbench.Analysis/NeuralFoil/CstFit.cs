using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.NeuralFoil;

/// <summary>Eight CST weights per side, one shared leading-edge modification, and a trailing-edge gap.</summary>
public sealed record CstParameters(double[] Upper, double[] Lower, double LeadingEdge, double TrailingEdge);

/// <summary>Normalized ordinates of one section, keyed by the profile revision's hash.</summary>
public sealed record NeuralFoilSection(string ProfileHash, string Family, IReadOnlyList<double> X,
    IReadOnlyList<double> Upper, IReadOnlyList<double> Lower);

public sealed record CstFitResult(CstParameters Parameters, double RmsResidual, double MaxResidual);

/// <summary>
/// CST with leading-edge modification, ported from AeroSandbox 4.2.9's least-squares fit used by SPIKE-ANA-1.
/// The ordinate residual is computed on the supplied section grid, never inferred from a family name.
/// </summary>
public static class CstFit
{
    private const int Coefficients = 18;
    private static readonly double[] Binomial7 = [1, 7, 21, 35, 35, 21, 7, 1];

    public static CstFitResult Fit(NeuralFoilSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        int count = section.X.Count;
        if (count < 20 || section.Upper.Count != count || section.Lower.Count != count)
            throw new ContractError("ANA-POLAR-CST-INPUT", "CST fit needs at least 20 matching ordinates per side.");
        var normal = new double[Coefficients, Coefficients];
        var rhs = new double[Coefficients];
        Span<double> basis = stackalloc double[Coefficients];
        double previous = -1;
        for (int i = 0; i < count; i++)
        {
            double x = section.X[i];
            if (!double.IsFinite(x) || x < 0 || x > 1 || x <= previous ||
                !double.IsFinite(section.Upper[i]) || !double.IsFinite(section.Lower[i]))
                throw new ContractError("ANA-POLAR-CST-INPUT", "CST section stations must be finite, increasing and within [0,1].");
            previous = x;
            for (int side = 0; side < 2; side++)
            {
                bool upper = side == 0;
                Basis(x, upper, basis);
                double target = upper ? section.Upper[i] : section.Lower[i];
                for (int row = 0; row < Coefficients; row++)
                {
                    rhs[row] += basis[row] * target;
                    for (int col = 0; col <= row; col++) normal[row, col] += basis[row] * basis[col];
                }
            }
        }
        for (int row = 0; row < Coefficients; row++)
            for (int col = row + 1; col < Coefficients; col++) normal[row, col] = normal[col, row];
        double[] solved = Solve(normal, rhs, Coefficients);
        if (solved[17] < 0) solved = Solve(normal, rhs, 17);
        var parameters = new CstParameters(solved[..8], solved[8..16], solved[16], solved.Length == 18 ? solved[17] : 0);
        double sumSquares = 0, maximum = 0;
        for (int i = 0; i < count; i++)
        {
            double x = section.X[i];
            double upperError = Ordinate(parameters, x, true) - section.Upper[i];
            double lowerError = Ordinate(parameters, x, false) - section.Lower[i];
            sumSquares += upperError * upperError + lowerError * lowerError;
            maximum = Math.Max(maximum, Math.Max(Math.Abs(upperError), Math.Abs(lowerError)));
        }
        return new(parameters, Math.Sqrt(sumSquares / (2 * count)), maximum);
    }

    public static double Ordinate(CstParameters parameters, double x, bool upper)
    {
        ReadOnlySpan<double> weights = upper ? parameters.Upper : parameters.Lower;
        double shape = 0;
        for (int i = 0; i < 8; i++)
            shape += weights[i] * Binomial7[i] * Math.Pow(x, i) * Math.Pow(1 - x, 7 - i);
        return Math.Sqrt(x) * (1 - x) * shape + parameters.LeadingEdge * x * Math.Pow(1 - x, 8.5)
            + (upper ? 1 : -1) * x * parameters.TrailingEdge / 2;
    }

    private static void Basis(double x, bool upper, Span<double> basis)
    {
        basis.Clear();
        double cls = Math.Sqrt(x) * (1 - x);
        int offset = upper ? 0 : 8;
        for (int i = 0; i < 8; i++) basis[offset + i] = cls * Binomial7[i] * Math.Pow(x, i) * Math.Pow(1 - x, 7 - i);
        basis[16] = x * Math.Pow(1 - x, 8.5);
        basis[17] = (upper ? 1 : -1) * x / 2;
    }

    private static double[] Solve(double[,] normal, double[] rhs, int dimension)
    {
        var matrix = new double[dimension, dimension + 1];
        for (int row = 0; row < dimension; row++)
        {
            for (int col = 0; col < dimension; col++) matrix[row, col] = normal[row, col];
            matrix[row, dimension] = rhs[row];
        }
        for (int pivot = 0; pivot < dimension; pivot++)
        {
            int best = pivot;
            for (int row = pivot + 1; row < dimension; row++)
                if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[best, pivot])) best = row;
            if (Math.Abs(matrix[best, pivot]) < 1e-18)
                throw new ContractError("ANA-POLAR-CST-FIT", "CST fit is singular for this section.");
            for (int col = pivot; col <= dimension; col++)
                (matrix[pivot, col], matrix[best, col]) = (matrix[best, col], matrix[pivot, col]);
            double divisor = matrix[pivot, pivot];
            for (int col = pivot; col <= dimension; col++) matrix[pivot, col] /= divisor;
            for (int row = pivot + 1; row < dimension; row++)
            {
                double factor = matrix[row, pivot];
                for (int col = pivot; col <= dimension; col++) matrix[row, col] -= factor * matrix[pivot, col];
            }
        }
        var answer = new double[dimension];
        for (int row = dimension - 1; row >= 0; row--)
        {
            double value = matrix[row, dimension];
            for (int col = row + 1; col < dimension; col++) value -= matrix[row, col] * answer[col];
            answer[row] = value;
        }
        return answer;
    }
}
