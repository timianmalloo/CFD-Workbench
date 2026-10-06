using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>A bounded root with a named basis. Null value and a code mean no supported solution.</summary>
public sealed record SearchResult(double? Value, double Lower, double Upper, int Iterations,
    string TerminationCode, string BasisCode, double? RelativeError);

/// <summary>ANA-05 bracketed searches. The callback reevaluates the full method at each α or speed.</summary>
public static class OperatingSearch
{
    public static SearchResult FindAlpha(Func<double, StripValue> clAtAlpha, double targetCl,
        double lowerDeg, double upperDeg, string basisCode, CancellationToken cancellation = default) =>
        Root(clAtAlpha, targetCl, lowerDeg, upperDeg, basisCode, cancellation);

    public static SearchResult FindTakeoff(Func<double, StripValue> liftAtSpeed, double requiredLift,
        double lowerSpeed, double upperSpeed, string basisCode, CancellationToken cancellation = default) =>
        requiredLift > 0 ? Root(liftAtSpeed, requiredLift, lowerSpeed, upperSpeed, basisCode, cancellation) :
            throw new ContractError("ANA-FIND-INPUT", "Take-off requires a positive target lift.");

    private static SearchResult Root(Func<double, StripValue> evaluate, double target, double lower, double upper,
        string basisCode, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        if (!double.IsFinite(target) || !double.IsFinite(lower) || !double.IsFinite(upper) ||
            lower >= upper || string.IsNullOrWhiteSpace(basisCode))
            throw new ContractError("ANA-FIND-INPUT", "A finite target, ordered finite bracket and basis code are required.");
        double tolerance = target == 0 ? 1e-6 : 0.01 * Math.Abs(target);
        double scale = Math.Max(Math.Abs(target), 1e-6);
        StripValue left = evaluate(lower), right = evaluate(upper);
        if (left.Value is null || right.Value is null)
            return new(null, lower, upper, 0, left.UnavailableReason ?? right.UnavailableReason ?? "ANA-FIND-SOURCE-UNAVAILABLE", basisCode, null);
        double fLeft = left.Value.Value - target, fRight = right.Value.Value - target;
        if (Math.Abs(fLeft) <= tolerance)
            return new(lower, lower, upper, 0, "ANA-FIND-ROOT", basisCode, Math.Abs(fLeft) / scale);
        if (Math.Abs(fRight) <= tolerance)
            return new(upper, lower, upper, 0, "ANA-FIND-ROOT", basisCode, Math.Abs(fRight) / scale);
        if (Math.Sign(fLeft) == Math.Sign(fRight))
            return new(null, lower, upper, 0, "ANA-FIND-NO-SIGN-CHANGE", basisCode, null);
        for (int iteration = 1; iteration <= 32; iteration++)
        {
            cancellation.ThrowIfCancellationRequested();
            double middle = 0.5 * (lower + upper);
            StripValue sample = evaluate(middle);
            if (sample.Value is not { } value)
                return new(null, lower, upper, iteration, sample.UnavailableReason ?? "ANA-FIND-SOURCE-UNAVAILABLE", basisCode, null);
            double fMiddle = value - target;
            double relative = Math.Abs(fMiddle) / scale;
            if (Math.Abs(fMiddle) <= tolerance) return new(middle, lower, upper, iteration, "ANA-FIND-ROOT", basisCode, relative);
            if (Math.Sign(fMiddle) == Math.Sign(fLeft)) { lower = middle; fLeft = fMiddle; }
            else { upper = middle; fRight = fMiddle; }
        }
        return new(null, lower, upper, 32, "ANA-FIND-MAX-ITERATIONS", basisCode, null);
    }
}
