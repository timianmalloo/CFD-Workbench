using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// Validation and the derived quantities of an operating point (q, Re_ref, h/c, Fr_h, σ) — derived on read, never stored
/// (design §3.5). The stored record is Core's <see cref="OperatingPoint"/>. SVC owns the bodies.
/// </summary>
public static class OperatingPoints
{
    /// <summary>Refuses an operating point the method cannot evaluate (ANA-INPUT-*), with its stable code.</summary>
    public static void Validate(OperatingPoint op) => throw new NotImplementedException("SVC: OperatingPoints.Validate");
}
