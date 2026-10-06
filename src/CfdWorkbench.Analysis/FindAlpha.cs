using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>What the Find α dialog shows: the sentence, the rows, and the root (null when none). It records no run.</summary>
public sealed record FindAlphaOutcome(double? Alpha, double Target, double Lower, double Upper, string Sentence,
    IReadOnlyList<ResultRow> Rows, int Solves);

/// <summary>
/// ANA-05 as the Section tab's dialog needs it (DR-DXM-6). The lattice is re-solved at each α without recording a run;
/// Apply is the caller's, and Evaluate stays explicit.
/// </summary>
public static class FindAlpha
{
    /// <summary>The h/c below which the depth fit of the free-surface data stops (<see cref="FreeSurfaceCorrection.MinHOverC"/>): Find α names it and finds nothing.</summary>
    public const double DepthFloorHOverC = FreeSurfaceCorrection.MinHOverC;

    /// <summary>CL of the wing at α from the lattice and its strip coupling; the reference quantities are the method's own.</summary>
    public static Func<double, StripValue> ClAt(IWingMethod method, byte[] source, OperatingPoint op, WaterRecord water,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        RunSettings settings = method.Settings;
        IReadOnlyList<SectionSample> sections = Placement.Sections(source, settings.SectionEtas!, settings.SectionXs!, cancellation);
        double sRef = method.Reference(source).SRef;
        return alpha =>
        {
            OperatingPoint at = op with { AlphaDeg = alpha };
            LatticeSolution solution = method.Solve(sections, at, water, cancellation);
            IReadOnlyList<StripLoad> strips = method.Couple(sections, solution, at, water, cancellation);
            double radians = VortexLattice.ToRadians(alpha);
            double lift = strips.Sum(s => -s.Fx * Math.Sin(radians) + s.Fz * Math.Cos(radians));
            double q = 0.5 * water.Rho * at.Speed * at.Speed;
            return q > 0 && sRef > 0 && double.IsFinite(lift) ? new StripValue(lift / (q * sRef), null) : new StripValue(null, "ANA-SPEED-NOT-POSITIVE");
        };
    }

    public static FindAlphaOutcome Run(Func<double, StripValue> clAtAlpha, double target, double lower, double upper,
        double? depthOverChord = null, double? polarConfidence = null, CancellationToken cancellation = default)
    {
        int solves = 0;
        Func<double, StripValue> counted = alpha => { solves++; return clAtAlpha(alpha); };
        string basis = depthOverChord is { } hc && hc < Labels.DeepWaterHc
            ? $"deep water, uncorrected; free surface not modelled (h/c = {Labels.Number(hc, "0.00")}; effects measured below h/c 5)"
            : "deep water, uncorrected";
        if (depthOverChord is { } shallow && shallow < DepthFloorHOverC)
            return None(Labels.FindReason("ANA-FIND-DEPTH-BELOW-FLOOR", DepthFloorHOverC), "ANA-FIND-DEPTH-BELOW-FLOOR");
        SearchResult search = OperatingSearch.FindAlpha(counted, target, lower, upper, "deep-water-uncorrected", cancellation);
        if (search.Value is not { } alpha) return None(Labels.FindReason(search.TerminationCode, iterations: search.Iterations), search.TerminationCode);
        var rows = new List<ResultRow>
        {
            new(Labels.FindTarget, Labels.Number(target, "0.###"), null, null),
            new(Labels.FindBracket, Labels.Number(search.Lower, "0.##") + "° to " + Labels.Number(search.Upper, "0.##") + "°", null, null),
            new(Labels.FindIterations, search.Iterations.ToString(System.Globalization.CultureInfo.InvariantCulture) + " · " + solves + " lattice solves", null, null),
            new(Labels.FindStopped, "CL within 1 % of the target", null, null),
            new(Labels.FindBasis, basis, null, null),
            new(Labels.FindPolarLimit, Labels.ClMaxLimit, null, null)
        };
        return new(alpha, target, lower, upper, Labels.FoundAlpha(alpha, target), rows, solves);

        FindAlphaOutcome None(string reason, string _) =>
            new(null, target, lower, upper, Labels.NoAlpha(reason), [new(Labels.FindStopped, reason, null, null)], solves);
    }
}
