using CfdWorkbench.Core.Notebook;

namespace CfdWorkbench.Analysis.Notebook;

/// <summary>Structured progress notification emitted via <see cref="IProgress{T}"/> (zero unsubscribed C# events).</summary>
public sealed record NotebookProgressUpdate(
    string Stage,
    string Detail,
    double? FractionComplete = null);

/// <summary>Result of evaluating one unaccepted candidate via local physics solvers (zero <c>Envelope.Analysis</c> mutation).</summary>
public sealed record NotebookCandidateRunResult(
    bool Certified,
    string SurfaceHash,
    string GeometryStatus,
    string? RefusalCode,
    byte[] FoilDslBytes,
    IReadOnlyList<CandidateOperatingPointSummary> OperatingPoints,
    double TotalWallMs);

/// <summary>
/// Substitutable local physics runner seam (<c>docs/design/ai-guided-design-notebook.md §2 &amp; §3.4</c>).
/// Production implementation is <see cref="NotebookCandidateRunner"/> over direct
/// <see cref="ProductWingMethod"/> + <see cref="PanelMethod"/> + <see cref="SectionTier"/> + NeuralFoil.
/// </summary>
public interface INotebookSolverRunner
{
    Task<NotebookCandidateRunResult> EvaluateCandidateAsync(
        FoilReqDocument req,
        CandidateIntentDelta delta,
        IProgress<NotebookProgressUpdate>? progress = null,
        CancellationToken ct = default);
}
