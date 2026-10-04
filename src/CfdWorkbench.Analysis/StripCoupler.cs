using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// One-way strip coupling (design §5.2): α_i from the trailing-wake downwash, α_eff = α + twist − α_i, Re_local from the
/// local chord c(y), cd_profile at α_eff at both Ncrit when a polar exists. STP owns the body.
/// </summary>
public static class StripCoupler
{
    public static IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution,
        OperatingPoint op, WaterRecord water, IPolarSource polar, CancellationToken cancellation) =>
        throw new NotImplementedException("STP: StripCoupler.Couple");
}
