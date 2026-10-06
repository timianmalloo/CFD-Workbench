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

    /// <summary>Ruling 94 (COPY-242): the editor refusal text, with the minimum as <c>&lt;min&gt;</c>.</summary>
    public const string RefusalTemplate = "Tip chord can't go below <min> (the larger of 5 mm and 2 % of the root chord).";

    // 1 nm for the curve evaluation; only Meets uses it, so repeated edits cannot ratchet below the minimum.
    private const double Slack = 1e-9;

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

    public static string RefusalReason(double rootChordMeters) => RefusalTemplate.Replace("<min>", Format(rootChordMeters));

    /// <summary>Ruling 96 (COPY-243): a typed tip chord below the minimum, with the way out.</summary>
    public static string TypedRefusalReason(double rootChordMeters) => RefusalReason(rootChordMeters) + " Enter " + Format(rootChordMeters) + " or more.";

    /// <summary>Ruling 96 (COPY-246): a typed root chord above what the tip allows. The root leads; the tip is the cause.</summary>
    public const string RootRefusalTemplate = "Root chord can't go above <max> while the tip chord is <tip> (the tip must stay at least 2 % of the root). Widen the tip first.";

    /// <summary>Ruling 96 (COPY-244): the drag hold at the tip minimum.</summary>
    public const string HoldTipTemplate = "Tip chord is at its minimum, <min>.";

    /// <summary>Ruling 96 (COPY-245): the drag hold at the root maximum.</summary>
    public const string HoldRootTemplate = "Root chord is at its maximum, <max>, for a <tip> tip. Widen the tip first.";

    /// <summary>Ruling 96 (COPY-247): a legacy tip already under the minimum.</summary>
    public const string AlreadyUnderText = "This tip is already under the minimum. It can't go lower.";

    /// <summary>
    /// The largest root chord <see cref="Admits"/> allows for a tip that stays at <paramref name="tipChordMeters"/>, from a wing
    /// whose root was <paramref name="oldRootChordMeters"/>: the tip must stay at least <see cref="RootRatio"/> of it, or no
    /// further under the minimum than it was. It only bites above a 250 mm root, because below that the 5 mm floor governs.
    /// </summary>
    public static double MaximumRootMeters(double tipChordMeters, double oldRootChordMeters) =>
        Math.Max(tipChordMeters, MinimumMeters(oldRootChordMeters)) / RootRatio;

    /// <summary>A length as the app writes lengths, for example "5 mm" or "12.5 mm".</summary>
    public static string FormatMm(double meters) => (meters * 1e3).ToString("0.##", CultureInfo.InvariantCulture) + " mm";

    public static string RootRefusalReason(double tipChordMeters, double oldRootChordMeters) =>
        RootRefusalTemplate.Replace("<max>", FormatMm(MaximumRootMeters(tipChordMeters, oldRootChordMeters))).Replace("<tip>", FormatMm(tipChordMeters));

    /// <summary>The status line of a held drag frame, root first when the root is held (Ruling 96).</summary>
    public static string HoldText(GestureLimit limit) => limit.Kind switch
    {
        GestureLimitKind.TipMinimum => HoldTipTemplate.Replace("<min>", FormatMm(limit.LimitMeters)),
        GestureLimitKind.RootMaximum => HoldRootTemplate.Replace("<max>", FormatMm(limit.LimitMeters)).Replace("<tip>", FormatMm(limit.OtherChordMeters)),
        _ => AlreadyUnderText
    };

    /// <summary>
    /// Whether an edit from (<paramref name="oldTip"/>, <paramref name="oldRoot"/>) to (<paramref name="newTip"/>,
    /// <paramref name="newRoot"/>) is admitted: the new wing meets the minimum, or it is no further below it than the old
    /// one was. A file already under the minimum therefore opens, and edits that raise the tip (or leave it alone) go through.
    /// </summary>
    public static bool Admits(double oldTip, double oldRoot, double newTip, double newRoot)
    {
        if (Meets(newTip, newRoot)) return true;
        // A non-finite old tip or root is no prior constraint, so a repairing edit is admitted.
        double before = double.IsFinite(oldTip) && double.IsFinite(oldRoot)
            ? Math.Max(0, MinimumMeters(oldRoot) - oldTip) : double.PositiveInfinity;
        double after = MinimumMeters(newRoot) - newTip;
        return after <= before;
    }
}
