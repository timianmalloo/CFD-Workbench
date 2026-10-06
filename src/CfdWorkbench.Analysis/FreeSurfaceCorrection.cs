using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>A5.2 is a separate estimate beside the deep-water result; no original load is overwritten.</summary>
public sealed record FreeSurfaceResult(double? Lift, double? DragLow, double? DragHigh, double? Moment,
    double? HOverC, double? FroudeDepth, string Id, string SourceId, string? ReasonCode)
{
    public IReadOnlyList<string> ReasonCodes { get; init; } = [];
    public IReadOnlyList<string> ModelNotes { get; init; } =
        ["ANA-FREE-SURFACE-DEPTH-ONLY", "ANA-FREE-SURFACE-WAVE-DRAG-OMITTED"];
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
    public const double MinRe = 73000;
    public const double MaxRe = 290000;
    public const double MinAlphaDeg = -5;
    public const double MaxAlphaDeg = 10;
    public const double MaxFroudeDepth = 5;

    /// <summary>JMSA-2026 [S6] depth-only factors within its model-scale Re, Fr, α and h/c envelope.</summary>
    public static FreeSurfaceResult Evaluate(double deepLift, double? deepDrag2, double? deepDrag4,
        double? deepMoment, double depth, double chord, double speed, double reynolds, double alphaDeg)
    {
        if (!double.IsFinite(deepLift) || (deepDrag2 is { } d2 && !double.IsFinite(d2)) ||
            (deepDrag4 is { } d4 && !double.IsFinite(d4)) ||
            (deepMoment is { } m && !double.IsFinite(m)) || !double.IsFinite(depth) ||
            !double.IsFinite(chord) || chord <= 0 || !double.IsFinite(speed) || speed <= 0 ||
            !double.IsFinite(reynolds) || reynolds <= 0 || !double.IsFinite(alphaDeg))
            throw new ContractError("ANA-FREE-SURFACE-INPUT", "Finite loads, positive chord, speed and Re, and finite alpha are required.");
        if (depth <= 0) return new(null, null, null, null, null, null, Id, SourceId,
            "ANA-FREE-SURFACE-SURFACE-PIERCING")
            { ReasonCodes = ["ANA-FREE-SURFACE-SURFACE-PIERCING"] };
        double hc = depth / chord;
        double froude = speed / Math.Sqrt(OperatingPoints.Gravity * depth);
        var outside = new List<string>();
        // Each axis is stated in docs/knowledge/hydrofoil-workbench/data-and-constants.md [S6].
        if (hc < MinHOverC || hc > MaxHOverC) outside.Add("ANA-FREE-SURFACE-DEPTH-OUTSIDE");
        if (reynolds < MinRe || reynolds > MaxRe) outside.Add("ANA-FREE-SURFACE-RE-OUTSIDE");
        if (froude > MaxFroudeDepth) outside.Add("ANA-FREE-SURFACE-FROUDE-OUTSIDE");
        if (alphaDeg < MinAlphaDeg || alphaDeg > MaxAlphaDeg) outside.Add("ANA-FREE-SURFACE-ALPHA-OUTSIDE");
        if (outside.Count > 0)
            return new(null, null, null, null, hc, froude, Id, SourceId, outside[0]) { ReasonCodes = outside };
        double liftFactor = 1 - 0.45 * Math.Exp(-Math.Pow(hc, 0.70));
        double dragFactor = 1 - 0.50 * Math.Exp(-Math.Pow(hc, 0.40));
        double momentFactor = 1 - 0.45 * Math.Exp(-Math.Pow(hc, 0.90));
        double? drag2 = deepDrag2 * dragFactor, drag4 = deepDrag4 * dragFactor;
        return new(deepLift * liftFactor,
            drag2.HasValue && drag4.HasValue ? Math.Min(drag2.Value, drag4.Value) : null,
            drag2.HasValue && drag4.HasValue ? Math.Max(drag2.Value, drag4.Value) : null,
            deepMoment * momentFactor, hc, froude, Id, SourceId, null);
    }
}
