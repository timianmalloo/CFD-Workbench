namespace CfdWorkbench.Analysis;

/// <summary>
/// The analysis tiers (design §5). A3a builds <see cref="VlmStrip"/> only; the section polar is Unavailable (DR-ANA-1 a)
/// and the estimator wing tier waits for A3d (P-8). The stored row carries the tier id (<c>vlm-strip</c>).
/// </summary>
public enum Tier
{
    Estimator,
    Polar,
    VlmStrip
}
