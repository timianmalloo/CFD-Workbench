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
    public static WingEstimates From(byte[] source, string basis, long generation) =>
        throw new NotImplementedException();

    public static double ChordMeters(byte[] source, double eta) =>
        throw new NotImplementedException();
}
