using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// A section polar method (DR-ANA-1). The unavailable source reports a reason, so missing profile drag is never zero.
/// </summary>
public interface IPolarSource
{
    /// <summary>Why no polar can be read (null when a method is installed).</summary>
    string? UnavailableReason { get; }

    /// <summary>
    /// A computed sample and its derived, non-persisted validity metadata. The unavailable source returns null.
    /// NeuralFoil throws <c>ContractError</c> ANA-POLAR-NONCOMPUTABLE for a point outside the network training range
    /// or a section its CST fit cannot represent. A computed point outside the validated bracket or below the
    /// advisory confidence threshold still returns a result with explicit flags.
    /// </summary>
    PolarResult? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation);
}

/// <summary>Read result. Only <see cref="Sample"/> is stored; all other members are derived from its grain and the run's own section.</summary>
public sealed record PolarResult(PolarSample Sample, IReadOnlyList<string> OutsideBracketReasons, bool LowConfidence,
    double CstResidualRms, double CstResidualMax)
{
    /// <summary>Ruling 117: a section outside the NACA 0012 family is computed and flagged (COPY-364), never refused.</summary>
    public bool SectionUnvalidated =>
        OutsideBracketReasons.Any(reason => reason.StartsWith("section ", StringComparison.Ordinal));

    /// <summary>Stable availability code. Re takes priority when a low-Re tip is outside several axes. The section family is a flag, not a refusal.</summary>
    public string? AvailabilityCode =>
        OutsideBracketReasons.Any(reason => reason.StartsWith("Re ", StringComparison.Ordinal)) ? "ANA-POLAR-RE-OUTSIDE" :
        OutsideBracketReasons.Any(reason => reason.StartsWith("alpha ", StringComparison.Ordinal)) ? "ANA-POLAR-ALPHA-OUTSIDE" :
        OutsideBracketReasons.Any(reason => reason.StartsWith("Ncrit ", StringComparison.Ordinal)) ? "ANA-POLAR-NCRIT-OUTSIDE" :
        !Sample.Converged ? "ANA-POLAR-NOT-CONVERGED" : null;
}
