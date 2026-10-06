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
        => Couple(sections, solution, op, water, polar, null, cancellation);

    /// <summary>Retrieve the run section's profile drag at each strip's own Re and α_eff for Ncrit 2 and 4.</summary>
    public static IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution,
        OperatingPoint op, WaterRecord water, IPolarSource polar, Func<double, string>? profileHashOf,
        CancellationToken cancellation)
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
        string reason = polar.UnavailableReason ?? "ANA-POLAR-PROFILE-MISSING";
        var unavailable = new StripValue(null, reason);
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
            StripValue cd2 = unavailable, cd4 = unavailable;
            if (polar.UnavailableReason is null && profileHashOf is not null)
            {
                string hash = profileHashOf(eta);
                cd2 = ProfileCd(polar, hash, reynolds, 2, alphaEff, water, cancellation);
                cd4 = ProfileCd(polar, hash, reynolds, 4, alphaEff, water, cancellation);
            }
            loads[i] = new StripLoad(strip.J, strip.Y, strip.Eta, chord, strip.Gamma, alphaI, alphaEff, reynolds,
                strip.ClLocal, cd2, cd4, force.Fx, force.Fy, force.Fz, force.Mx, force.My, force.Mz, strip.Downwash);
        }
        return loads;
    }

    private static StripValue ProfileCd(IPolarSource polar, string profileHash, double reynolds, double ncrit,
        double alphaEff, WaterRecord water, CancellationToken cancellation)
    {
        try
        {
            PolarResult? result = polar.Sample(profileHash, reynolds, ncrit, alphaEff, water, cancellation);
            if (result is null) return new(null, polar.UnavailableReason ?? "ANA-POLAR-UNAVAILABLE");
            // D14: a computed network point outside the validated bracket is not a supported profile drag.
            if (result.AvailabilityCode is { } code) return new(null, code);
            // Ruling 117: a section outside the NACA 0012 family is computed and carries COPY-364, not refused.
            return result.Sample.Cd is { } cd && double.IsFinite(cd) && cd > 0
                ? new(cd, null, StripFlags.Join(result.SectionUnvalidated ? StripFlags.SectionUnvalidated : null,
                    result.LowConfidence ? StripFlags.LowConfidence : null))
                : new(null, "ANA-POLAR-CD-UNAVAILABLE");
        }
        catch (ContractError error) when (error.Code.StartsWith("ANA-POLAR-", StringComparison.Ordinal))
        {
            return new(null, error.Code);
        }
    }
}

/// <summary>A <see cref="StripValue"/> flag is one code or several joined by '|'; a flagged value is computed, never refused.</summary>
public static class StripFlags
{
    public const string LowConfidence = "ANA-POLAR-LOW-CONFIDENCE";
    public const string SectionUnvalidated = "ANA-POLAR-SECTION-UNVALIDATED";

    public static string? Join(params string?[] codes)
    {
        string[] present = codes.Where(code => code is not null).Select(code => code!).Distinct().Order().ToArray();
        return present.Length == 0 ? null : string.Join('|', present);
    }

    public static bool Has(string? flags, string code) => flags is not null && flags.Split('|').Contains(code);

    public static IEnumerable<string> Codes(string? flags) => flags is null ? [] : flags.Split('|');
}
