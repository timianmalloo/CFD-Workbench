using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The lattice settings (DR-ANA-7): 64 spanwise per half by 4 chordwise, both cosine, wake 20 spans along +x,
/// singularity cutoff 10⁻⁸, and the 1 % near-field/Trefftz reconciliation tolerance. The unknown cap is 2,048.
/// Section stations on <see cref="Default"/> are the starboard edges and strip centres, and the chord edges plus
/// the bound (¼) and control (¾) abscissa of each panel. The service refuses a method whose stations are empty.
/// </summary>
public static class Settings
{
    /// <summary>Panel count (both halves) at which <see cref="VortexLattice.Solve"/> refuses the system.</summary>
    public const int UnknownCap = 2048;

    /// <summary>DR-ANA-7 reconciliation tolerance between near-field induced drag and the Trefftz value.</summary>
    public const double ReconciliationTolerance = 0.01;

    /// <summary>A control-point chord below this (10 µm) drops its strip, and the drop is listed.</summary>
    public const double MinimumControlChordMeters = 1e-5;

    /// <summary>
    /// Largest accepted normwise backward error of one lattice solve, ‖AΓ − b‖∞ / (‖A‖∞ ‖Γ‖∞ + ‖b‖∞). Partial-pivoting LU
    /// reaches about n·u (4.5 × 10⁻¹³ at the 2,048 cap); 10⁻¹⁰ leaves a 200× growth margin. Above it the run fails closed.
    /// </summary>
    public const double SolveBackwardErrorTolerance = 1e-10;

    /// <summary>The trailing-edge floor (0.3 mm), a value the app chose. It is in the settings hash through <see cref="RunSettings.TeFloorMm"/>.</summary>
    public const double TrailingEdgeFloorMm = 0.3;

    /// <summary>What every surface calls the floor (Ruling 195): an app default with no source behind it. Never "practitioner value".</summary>
    public const string TrailingEdgeFloorLabel = "app default, no source";

    public static RunSettings Default { get; } = WithStations(new RunSettings(
        64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, new[] { 2, 4 }, "clean", TrailingEdgeFloorMm));

    /// <summary>Fills empty section stations from the spacing law. Stations a caller already set are kept; they are in the run key.</summary>
    public static RunSettings WithStations(RunSettings settings)
    {
        if (settings.SectionEtas is { Count: > 0 } && settings.SectionXs is { Count: > 0 }) return settings;
        return settings with
        {
            SectionEtas = SpanEtas(settings.NSpanPerHalf, settings.SpanSpacing),
            SectionXs = ChordXs(settings.NChord, settings.ChordSpacing)
        };
    }

    internal static double[] SpanEtas(int nPerHalf, string spacing)
    {
        int n = checked(2 * nPerHalf);
        var values = new List<double> { 0 };
        for (int i = n / 2; i < n; i++)
        {
            double outer = Edge(i + 1, n, spacing);
            values.Add(0.5 * (Edge(i, n, spacing) + outer));
            values.Add(outer);
        }
        return Unique(values);
    }

    internal static double[] ChordXs(int nChord, string spacing)
    {
        var values = new List<double>();
        for (int k = 0; k < nChord; k++)
        {
            double inner = Fraction(k, nChord, spacing);
            double outer = Fraction(k + 1, nChord, spacing);
            double width = outer - inner;
            values.Add(inner);
            values.Add(inner + 0.25 * width);
            values.Add(inner + 0.75 * width);
        }
        values.Add(1);
        return Unique(values);
    }

    private static double Edge(int index, int n, string spacing) => spacing switch
    {
        "cosine" => -Math.Cos(Math.PI * index / n),
        "uniform" => -1 + 2.0 * index / n,
        _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "VLM: spacing is cosine or uniform.")
    };

    private static double Fraction(int index, int n, string spacing) => spacing switch
    {
        "cosine" => 0.5 * (1 - Math.Cos(Math.PI * index / n)),
        "uniform" => (double)index / n,
        _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "VLM: spacing is cosine or uniform.")
    };

    private static double[] Unique(List<double> values)
    {
        values.Sort();
        var kept = new List<double>();
        foreach (double value in values)
        {
            double clamped = Math.Clamp(value, 0, 1);
            if (kept.Count == 0 || clamped - kept[^1] > 1e-12) kept.Add(clamped);
        }
        return kept.ToArray();
    }
}
