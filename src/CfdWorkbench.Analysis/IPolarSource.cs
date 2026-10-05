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

    /// <summary>
    /// The polar sample at its grain key. Current behaviour differs by source: <c>UnavailablePolar</c> returns null;
    /// <c>NeuralFoilPolarSource</c> never returns null and throws <c>ContractError</c> ANA-POLAR-NONCOMPUTABLE (with the
    /// reason) for a point outside the network training range or a section its CST fit cannot represent. Computed points
    /// outside the validated bracket, and low-confidence points, are returned without those flags (only
    /// <c>NeuralFoilPolarSource.Evaluate</c> carries them). A3c-2 must reconcile this contract before a caller wires it.
    /// </summary>
    PolarSample? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation);
}
