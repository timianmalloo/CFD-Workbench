namespace CfdWorkbench.Core;

public sealed record DimensionReport(string Dimension, double TypedMeters, string Rule, double FitResidualMeters,
    double ToleranceMeters, double DeviationFromLinearMeters, double PlanformShiftMeters, bool FitAboveLimit);

public sealed record DimensionOutcome(string AcceptedId, DimensionReport Report);

public static class ChordDimension
{
    public const string RootFlat = "chord-blend-rootflat/1";
    public const string Linear = "chord-blend-linear/1";

    /// <summary>Model/join tolerance. A fit above this is accepted with a warning (Ruling 56, DR-12).</summary>
    internal const double FitToleranceMeters = 10e-6;

    public static (DimensionReport Report, byte[] Patched) Evaluate(byte[] source, DimensionCommand command) =>
        throw new NotImplementedException();

    internal static double[] FitOrdinates(Curve curve, Func<double, double> target, bool mirror,
        IReadOnlyList<(double[] Coefficients, double Bound)>? extra = null) =>
        throw new NotImplementedException();
}
