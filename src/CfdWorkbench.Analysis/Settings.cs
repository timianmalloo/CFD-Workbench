using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The lattice settings (DR-ANA-7): 64 spanwise per half by 4 chordwise, both cosine, wake 20 spans along +x,
/// singularity cutoff 10⁻⁸, and the 1 % near-field/Trefftz reconciliation tolerance. The unknown cap is 2,048.
/// </summary>
public static class Settings
{
    /// <summary>Panel count (both halves) at which <see cref="VortexLattice.Solve"/> refuses the system.</summary>
    public const int UnknownCap = 2048;

    /// <summary>DR-ANA-7 reconciliation tolerance between near-field induced drag and the Trefftz value.</summary>
    public const double ReconciliationTolerance = 0.01;

    /// <summary>A control-point chord below this (10 µm) drops its strip, and the drop is listed.</summary>
    public const double MinimumControlChordMeters = 1e-5;

    /// <summary>
    /// Largest accepted normwise backward error of one lattice solve, ‖AΓ − b‖∞ / (‖A‖∞ ‖Γ‖∞ + ‖b‖∞). Partial-pivoting LU
    /// reaches about n·u (4.5 × 10⁻¹³ at the 2,048 cap); 10⁻¹⁰ leaves a 200× growth margin. Above it the run fails closed.
    /// </summary>
    public const double SolveBackwardErrorTolerance = 1e-10;

    public static RunSettings Default { get; } = new(
        64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, new[] { 2, 4 }, "clean", 0.3);
}
