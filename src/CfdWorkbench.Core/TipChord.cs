using System.Globalization;

namespace CfdWorkbench.Core;

/// <summary>
/// Ruling 93: the minimum tip chord of a wing, the one definition. Core lengths are metres. The rule is a design rule
/// (the planform edits refuse to cross it) and the analysis backstop (<c>ANA-TIP-BELOW-FLOOR</c>) reads the same number.
/// It never gates Open: geometry certification is unchanged.
/// </summary>
public static class TipChord
{
    /// <summary>The stable refusal code of an edit that would take the tip chord below the minimum.</summary>
    public const string RefusalCode = "DSL-TIP-CHORD-MIN";

    /// <summary>The absolute floor, 5 mm.</summary>
    public const double FloorMeters = 0.005;

    /// <summary>The relative floor, 2 % of the root chord.</summary>
    public const double RootRatio = 0.02;

    /// <summary>max(5 mm, 2 % of the root chord). A non-finite root chord gives the absolute floor.</summary>
    public static double MinimumMeters(double rootChordMeters) =>
        double.IsFinite(rootChordMeters) ? Math.Max(FloorMeters, RootRatio * rootChordMeters) : FloorMeters;

    /// <summary>True when the tip chord meets the minimum (1 nm of slack for the curve evaluation).</summary>
    public static bool Meets(double tipChordMeters, double rootChordMeters) =>
        tipChordMeters >= MinimumMeters(rootChordMeters) - Slack;

    /// <summary>The minimum as the app writes lengths, for example "5 mm" or "12.5 mm".</summary>
    public static string Format(double rootChordMeters) =>
        (MinimumMeters(rootChordMeters) * 1e3).ToString("0.##", CultureInfo.InvariantCulture) + " mm";

    /// <summary>Ruling 94: the editor refusal text.</summary>
    public static string RefusalReason(double rootChordMeters) =>
        $"Tip chord can't go below {Format(rootChordMeters)} (the larger of 5 mm and 2 % of the root chord).";

    /// <summary>
    /// Whether an edit from (<paramref name="oldTip"/>, <paramref name="oldRoot"/>) to (<paramref name="newTip"/>,
    /// <paramref name="newRoot"/>) is admitted: the new wing meets the minimum, or it is no further below it than the old
    /// one was. A file already under the minimum therefore opens, and edits that raise the tip (or leave it alone) go through.
    /// </summary>
    public static bool Admits(double oldTip, double oldRoot, double newTip, double newRoot)
    {
        if (Meets(newTip, newRoot)) return true;
        double before = Math.Max(0, MinimumMeters(oldRoot) - oldTip);
        double after = MinimumMeters(newRoot) - newTip;
        return after <= before + Slack;
    }

    private const double Slack = 1e-9;
}
