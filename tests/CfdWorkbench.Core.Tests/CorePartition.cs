namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Cost-aware placement of Core checks on the ring's `--part=k/n` processes. Every part reads the same
/// Fixtures/core-costs.tsv and runs the same longest-processing-time-first assignment, so the parts agree without talking.
/// A check missing from the table falls back to the registration-index rule (i % n), so a new check never drops out.
/// </summary>
internal static class CorePartition
{
    private static Dictionary<string, int>? assigned;
    private static int assignedFor;

    internal static string TablePath() =>
        Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "core-costs.tsv");

    /// <summary>Reads `name TAB ms` rows; `#` lines and blank lines are comments. The first row for a name wins.</summary>
    internal static List<(string Name, double Ms)> LoadTable(string path)
    {
        var rows = new List<(string, double)>();
        if (!File.Exists(path)) return rows;
        var seen = new HashSet<string>();
        foreach (string line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] fields = line.Split('\t');
            if (fields.Length == 2 && double.TryParse(fields[1], System.Globalization.CultureInfo.InvariantCulture, out double ms) && seen.Add(fields[0]))
                rows.Add((fields[0], ms));
        }
        return rows;
    }

    /// <summary>Longest first onto the least-loaded part (0-based); ties go to the earlier table row, then the lower part.</summary>
    internal static Dictionary<string, int> Assign(IReadOnlyList<(string Name, double Ms)> table, int parts)
    {
        var load = new double[parts];
        var result = new Dictionary<string, int>();
        foreach (var (name, ms) in table.Select((row, index) => (row.Name, row.Ms, index)).OrderByDescending(row => row.Ms).ThenBy(row => row.index)
                     .Select(row => (row.Name, row.Ms)))
        {
            int lightest = 0;
            for (int k = 1; k < parts; k++) if (load[k] < load[lightest]) lightest = k;
            load[lightest] += ms;
            result[name] = lightest;
        }
        return result;
    }

    /// <summary>True when part <paramref name="index"/> (1-based) of <paramref name="count"/> runs the named check.</summary>
    internal static bool Owns(string name, int registrationIndex, int index, int count)
    {
        if (assigned is null || assignedFor != count)
        {
            assigned = Assign(LoadTable(TablePath()), count);
            assignedFor = count;
        }
        int owner = assigned.TryGetValue(name, out int placed) ? placed : registrationIndex % count;
        return owner == index - 1;
    }

    internal static void Run()
    {
        IdentityTests.Check("CorePartition_CostTable_PlacesEveryCheckOnceWithinFifteenPercent", () =>
        {
            var table = LoadTable(TablePath());
            IdentityTests.Equal(true, table.Count > 100);
            var placed = Assign(table, 3);
            IdentityTests.Equal(table.Count, placed.Count);
            var totals = new double[3];
            foreach (var (name, ms) in table) totals[placed[name]] += ms;
            double largest = totals.Max();
            if (largest - totals.Min() >= 0.15 * largest)
                throw new InvalidOperationException($"Parts {string.Join('/', totals.Select(t => (long)t))} ms differ by 15 % or more of the largest");
        });
        IdentityTests.Check("CorePartition_UnlistedCheck_FallsBackToRegistrationIndex_OnePartOwnsIt", () =>
        {
            for (int index = 0; index < 7; index++)
                IdentityTests.Equal(1, Enumerable.Range(1, 3).Count(part => Owns("No_Such_Check_In_The_Table", index, part, 3)));
            IdentityTests.Equal(true, Owns("No_Such_Check_In_The_Table", 4, 2, 3));
        });
    }
}
