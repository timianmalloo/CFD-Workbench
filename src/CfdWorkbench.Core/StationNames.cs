using System.Globalization;

namespace CfdWorkbench.Core;

/// <summary>
/// The one rule that names an authored station: Root at eta 0, Tip at eta 1, otherwise "Station n" where n is the 1-based position
/// among the authored stations. The app's views, the section edits and the CLI's <c>--station</c> all read it here.
/// </summary>
public static class StationNames
{
    /// <summary>"Root", "Tip" or "Station n" (n = <paramref name="index"/> + 1) for the station at <paramref name="index"/>.</summary>
    public static string Of(int index, double eta) =>
        eta == 0 ? "Root" : eta == 1 ? "Tip" : "Station " + (index + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>The Properties heading: "Root station", "Tip station" or "Station n".</summary>
    public static string Heading(int index, double eta) => eta == 0 ? "Root station" : eta == 1 ? "Tip station" : Of(index, eta);

    /// <summary>
    /// The station index a typed name selects, or null when it names none. Accepts <c>root</c>, <c>tip</c> and the number n of the label
    /// "Station n" (case-insensitive); a number names a station only when that station's label is "Station n", so the root and the tip
    /// are chosen by name.
    /// </summary>
    public static int? Resolve(string text, IReadOnlyList<double> etas)
    {
        string word = text.Trim();
        for (int index = 0; index < etas.Count; index++)
        {
            string label = Of(index, etas[index]);
            if (string.Equals(label, word, StringComparison.OrdinalIgnoreCase)) return index;
            if (label.StartsWith("Station ", StringComparison.Ordinal) &&
                string.Equals(label["Station ".Length..], word, StringComparison.Ordinal)) return index;
        }
        return null;
    }

    /// <summary>The values <see cref="Resolve"/> accepts, in station order: <c>root</c>, <c>2</c>, <c>3</c>, <c>tip</c>.</summary>
    public static IReadOnlyList<string> Choices(IReadOnlyList<double> etas) =>
        [.. etas.Select((eta, index) => eta == 0 ? "root" : eta == 1 ? "tip" : (index + 1).ToString(CultureInfo.InvariantCulture))];
}
