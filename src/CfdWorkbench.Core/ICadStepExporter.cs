namespace CfdWorkbench.Core;

/// <summary>STEP AP203 topological export variant (ADR-0013 §Decision 2, Ruling 270 DR-KRN-3).</summary>
public enum StepVariant
{
    /// <summary>Watertight closed solid (<c>MANIFOLD_SOLID_BREP</c> / <c>CLOSED_SHELL</c>), Fusion &amp; CAM ready.</summary>
    ClosedShell,

    /// <summary>Untrimmed surface set (<c>SHELL_BASED_SURFACE_MODEL</c> / <c>OPEN_SHELL</c>), no tip or root caps.</summary>
    Untrimmed,
}

/// <summary>Optional header overrides used exclusively for red-first zero-PII negative controls.</summary>
public sealed record StepHeaderOverrides(string? Timestamp = null, string? Author = null, string? Organization = null);

/// <summary>
/// Result of one STEP AP203 export: UTF-8 Part 21 bytes, skin fidelity against <see cref="Placement.Surface"/>,
/// always-on trailing-edge thickness metrics (Ruling 195), bounding box in millimetres, and the independent
/// <see cref="StepVerifier"/> report read back from the emitted bytes.
/// </summary>
public sealed record StepExportResult(
    byte[] Bytes,
    StlScope Scope,
    StepVariant Variant,
    int SectionCount,
    int ChordControlPoints,
    int DegreeU,
    int DegreeV,
    int RefinementRounds,
    double DeviationMm,
    double TipPlanarityErrorMm,
    double RootPlanarityErrorMm,
    double TrailingEdgeMm,
    double TrailingEdgeYMm,
    bool TrailingEdgeAtTip,
    bool TrailingEdgeAlongSpan,
    bool IsOpenTrailingEdge,
    double SizeXMm,
    double SizeYMm,
    double SizeZMm,
    StepVerificationReport Verification);

/// <summary>
/// Stable seam for STEP AP203 export (ADR-0013 §Decision 3). Option B (pure-C# <see cref="StepAp203Export"/>)
/// implements this seam with zero LGPL or native dependencies; Option A (process-isolated OCCT worker) remains
/// a drop-in fallback behind this exact interface if <c>SPIKE-CAD-02</c> ever requires it.
/// </summary>
public interface ICadStepExporter
{
    string EngineId { get; }

    StepExportResult Export(
        byte[] source,
        string foilName,
        int revision,
        StlScope scope,
        StepVariant variant,
        CancellationToken cancellation = default,
        StepHeaderOverrides? headerOverrides = null);
}
