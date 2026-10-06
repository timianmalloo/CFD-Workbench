using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>A3a has no section polar. Profile drag is Unavailable, never zero (design §5.2, DR-ANA-1).</summary>
public sealed class UnavailablePolar : IPolarSource
{
    public static readonly UnavailablePolar Instance = new();

    public string? UnavailableReason => "Unavailable — no polar method installed";

    public PolarResult? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        return null;
    }
}
