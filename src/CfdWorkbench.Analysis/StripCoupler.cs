using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// Per-strip induction and local Reynolds number (design §5.2). α_i is the trailing-wake angle already on the strip,
/// α_eff = α + twist − α_i, and Re_local = V c(y) / ν with c(y) the strip chord and ν the water record's viscosity.
/// </summary>
public static class StripCoupler
{
    public static IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution,
        OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
        Couple(sections, solution, op, water, UnavailablePolar.Instance, cancellation);

    public static IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution,
        OperatingPoint op, WaterRecord water, IPolarSource polar, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (solution.Strips.Count != solution.Forces.Count)
            throw new ContractError("ANA-NONFINITE", "The lattice returned a different number of strips and forces.");
        double etaMin = 0, etaMax = 1;
        if (sections.Count > 0)
        {
            etaMin = sections.Min(section => section.Frame.Eta);
            etaMax = sections.Max(section => section.Frame.Eta);
        }
        string reason = polar.UnavailableReason ?? "Unavailable — no polar method installed";
        var cd = new StripValue(null, reason);
        var loads = new StripLoad[solution.Strips.Count];
        for (int i = 0; i < loads.Length; i++)
        {
            LatticeStrip strip = solution.Strips[i];
            StripForce force = solution.Forces[i];
            double eta = Math.Abs(strip.Eta);
            if (sections.Count > 0 && (eta < etaMin - 1e-9 || eta > etaMax + 1e-9))
                throw new ContractError("ANA-INPUT-STATIONS", "A strip lies outside the placed sections.");
            // c(y) is the chord of this strip. A reference chord used for every strip is the BC-4 mutant.
            double chord = strip.Chord;
            double alphaI = strip.InducedAngleDeg;
            double alphaEff = op.AlphaDeg + strip.TwistDeg - alphaI;
            double reynolds = op.Speed * chord / water.Nu;
            loads[i] = new StripLoad(strip.J, strip.Y, strip.Eta, chord, strip.Gamma, alphaI, alphaEff, reynolds,
                strip.ClLocal, cd, cd, force.Fx, force.Fy, force.Fz, force.Mx, force.My, force.Mz, strip.Downwash);
        }
        return loads;
    }
}
