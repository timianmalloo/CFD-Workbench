namespace CfdWorkbench.Analysis;

/// <summary>
/// The analysis tiers (design §5). A3a builds <see cref="VlmStrip"/> only; the section polar is Unavailable (DR-ANA-1 a)
/// the wing estimator tier is built in A3d-1 (<see cref="WingEstimator"/>, P-8). The stored row carries the tier id (<c>vlm-strip</c> or <c>estimator</c>).
/// </summary>
public enum Tier
{
    Estimator,
    Polar,
    VlmStrip
}
