using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The solved lattice: Γ per strip (strip totals), the Trefftz downwash w_T per strip, and the solver diagnostics.
/// Derived; the lattice itself is rebuilt on read, never stored (design §3.1).
/// </summary>
public sealed record LatticeSolution(IReadOnlyList<double> Gamma, IReadOnlyList<double> DownwashTrefftz, RunDiagnostics Diagnostics);

/// <summary>
/// Horseshoe vortices on the placed camber surface of both halves (design §5.2). Panel corners come from
/// <see cref="SectionSample.PlacedCamber"/> only, so the layers cannot disagree with the drawn foil. VLM owns the body.
/// </summary>
public static class VortexLattice
{
    public static LatticeSolution Solve(IReadOnlyList<SectionSample> sections, RunSettings settings, OperatingPoint op,
        CancellationToken cancellation) => throw new NotImplementedException("VLM: VortexLattice.Solve");
}
