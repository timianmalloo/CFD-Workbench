namespace CfdWorkbench.Analysis;

/// <summary>
/// The one definition of the cosine and uniform spacing (Ruling 214, track LSP). The section stations in the run key, the lattice span
/// and chord stations and the strip-width fallback all call it, so they agree bit for bit on every host. Cosine uses
/// <c>double.CosPi</c>, never the platform <c>Math.Cos</c> (class CRT-GOLDEN; gate tools/check-crt-transcendentals.py).
/// </summary>
internal static class Spacing
{
    /// <summary>Edge form on [-1, 1]: edge <paramref name="index"/> of <paramref name="n"/> intervals.</summary>
    internal static double Edge(int index, int n, string spacing) => spacing switch
    {
        "cosine" => -double.CosPi(index / (double)n),
        "uniform" => -1 + 2.0 * index / n,
        _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "VLM: spacing is cosine or uniform.")
    };

    /// <summary>Fraction form on [0, 1]: edge <paramref name="index"/> of <paramref name="n"/> intervals.</summary>
    internal static double Fraction(int index, int n, string spacing) => spacing switch
    {
        "cosine" => 0.5 * (1 - double.CosPi(index / (double)n)),
        "uniform" => (double)index / n,
        _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "VLM: spacing is cosine or uniform.")
    };
}
