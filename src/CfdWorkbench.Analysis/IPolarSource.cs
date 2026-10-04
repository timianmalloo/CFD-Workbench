using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// A section polar method (DR-ANA-1). A3a installs none: STP's stub reports "Unavailable — no polar method installed",
/// so cd_profile and Total drag are Unavailable, never zero.
/// </summary>
public interface IPolarSource
{
    /// <summary>Why no polar can be read (null when a method is installed).</summary>
    string? UnavailableReason { get; }

    /// <summary>The polar sample at its grain key, or null when it is Unavailable (outside the Re or α envelope).</summary>
    PolarSample? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation);
}
