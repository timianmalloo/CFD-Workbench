using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>A5.2 is a separate estimate beside the deep-water result; no original load is overwritten.</summary>
public sealed record FreeSurfaceResult(double? Lift, double? Drag, double? Moment,
    double? HOverC, double? FroudeDepth, string Id, string SourceId, string? ReasonCode)
{
    public double ModelScaleReFrom => 73000;
    public double ModelScaleReTo => 290000;
    public double FroudeLiftLossAtHc4Fr2To5 => 0.17;
    public string FroudeDependenceCode => "ANA-FREE-SURFACE-FROUDE-NOT-MODELLED";
}

public static class FreeSurfaceCorrection
{
    public const string Id = "JMSA-2026-depth-fit";
    public const string SourceId = "JMSA-2026-S6";
    public const double MinHOverC = 0.5;
    public const double MaxHOverC = 9.5;

    /// <summary>The paper's depth-only factors. Froude is reported because the fit does not model its dependence.</summary>
    public static FreeSurfaceResult Evaluate(double deepLift, double? deepDrag, double? deepMoment,
        double depth, double chord, double speed)
    {
        if (!double.IsFinite(deepLift) || (deepDrag is { } d && !double.IsFinite(d)) ||
            (deepMoment is { } m && !double.IsFinite(m)) || !double.IsFinite(depth) ||
            !double.IsFinite(chord) || chord <= 0 || !double.IsFinite(speed) || speed <= 0)
            throw new ContractError("ANA-FREE-SURFACE-INPUT", "Finite loads, positive chord and speed are required.");
        if (depth <= 0) return new(null, null, null, null, null, Id, SourceId, "ANA-FREE-SURFACE-SURFACE-PIERCING");
        double hc = depth / chord;
        double froude = speed / Math.Sqrt(OperatingPoints.Gravity * depth);
        if (hc < MinHOverC || hc > MaxHOverC)
            return new(null, null, null, hc, froude, Id, SourceId, "ANA-FREE-SURFACE-DEPTH-OUTSIDE");
        double liftFactor = 1 - 0.45 * Math.Exp(-Math.Pow(hc, 0.70));
        double dragFactor = 1 - 0.50 * Math.Exp(-Math.Pow(hc, 0.40));
        double momentFactor = 1 - 0.45 * Math.Exp(-Math.Pow(hc, 0.90));
        return new(deepLift * liftFactor, deepDrag * dragFactor, deepMoment * momentFactor,
            hc, froude, Id, SourceId, null);
    }
}
