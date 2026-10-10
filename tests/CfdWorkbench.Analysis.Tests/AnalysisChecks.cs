using System.Diagnostics;
using System.Globalization;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// The Analysis console harness (design area3-analysis.md §18.2 PRE). Each check prints <c>PASS name</c> or
/// <c>FAIL name …</c>, then <c>COST name ms</c> (its wall time, invariant culture, three decimals) for the C-5 cost
/// check. Repository files are never read: a file a check needs ships beside the binary as Content (DESIGN.md).
/// </summary>
internal static class AnalysisChecks
{
    private static int failures;
    // The Core harness's subset selector: comma-separated check-name prefixes. A prefix that selects no check fails the
    // run, so a subset can never pass empty (HARNESS-SILENT-EXIT).
    private static readonly string[]? only = Environment.GetEnvironmentVariable("CFD_TEST_ONLY")?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static readonly HashSet<string> matched = [];

    private static int Main(string[] args)
    {
        // Readiness tier: never spawned by tools/run-tests.sh, which calls this harness with no arguments.
        if (args.Contains("--readiness"))
        {
            LatticeFixtureTests.RunReadiness();
            RunStoreTests.RunReadiness();
            FreshnessTests.RunReadiness();
            PanelCpTests.RunReadiness();
            StripFixtureTests.RunReadiness();
            SectionSeamTests.RunReadiness();
            TipCavitationTests.RunReadiness();
            DxSectionTests.RunReadiness();
            SectionForceTests.RunReadiness();
            ProjectionTests.RunReadiness();
            PolarSeamTests.RunReadiness();
            PolarNumericsTests.RunReadiness();
            return Finish();
        }
        (int Index, int Count)? part;
        try { part = ParsePart(args); }
        catch (ArgumentException error) { Console.WriteLine("FAIL PARTITION " + error.Message); return 1; }
        // B4 (ANALYSIS-HARNESS-GROWTH): the fast ring runs this harness as parts, `--part=k/n`. A group is one test class and
        // runs whole, because checks inside a class share fixtures (F-6's trio feeds F-15, F-15b). Every group lands in exactly
        // one part: groups are placed longest first onto the least-loaded part by CostHintMs (ties to the lower part), the same
        // arithmetic in every part, so the parts together run each check once. Hints are the median of three GROUP wall times of the whole harness in one process (2026-10-08, Release, load ~10), except Service and DxSection, which are ring-measured; a group costs more cold than after the groups that warm it, so do not feed a part's own GROUP lines back in (docs/proof/abl/measure.md). Re-measure when a track adds checks. A stale hint costs balance, never coverage.
        var groups = new (string Name, int CostHintMs, Action Run)[]
        {
            ("Architecture", 7, ArchitectureTests.Run), ("RunStore", 719, RunStoreTests.Run), ("Lattice", 559, LatticeFixtureTests.Run),
            ("Strip", 274, StripFixtureTests.Run), ("Service", 1205, ServiceTests.Run), ("Freshness", 604, FreshnessTests.Run),
            ("Projection", 29, ProjectionTests.Run), ("Labels", 38, LabelsTests.Run), ("LoadsView", 8, LoadsViewTests.Run),
            ("PanelCp", 37, PanelCpTests.Run), ("SectionEstimator", 77, SectionEstimatorTests.Run),
            ("Cavitation", 10, CavitationTests.Run), ("NeuralFoil", 100, NeuralFoilTests.Run),
            ("PolarSeam", 13, PolarSeamTests.Run),
            ("SectionSeam", 1528, SectionSeamTests.Run),
            ("ProvenanceSeam", 33, ProvenanceSeamTests.Run),
            ("PolarNumerics", 19, PolarNumericsTests.Run),
            ("OperatingSearch", 20, OperatingSearchTests.Run),
            ("TipPolar", 1, TipPolarTests.Run),
            ("DxSection", 1177, DxSectionTests.Run),
            ("SectionForce", 297, SectionForceTests.Run),
            ("NotResolved", 842, NotResolvedTests.Run),
            ("TeFloor", 202, TeFloorLabelTests.Run),
        };
        int[] owner = Assign(groups.Select(group => group.CostHintMs).ToArray(), part?.Count ?? 1);
        for (int i = 0; i < groups.Length; i++)
        {
            if (part is { } p && owner[i] != p.Index - 1) continue;
            long begun = Stopwatch.GetTimestamp();
            groups[i].Run();
            Console.WriteLine("GROUP " + groups[i].Name + " " + Stopwatch.GetElapsedTime(begun).TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture));
        }
        if (part is { } q) Console.WriteLine($"PARTITION {q.Index}/{q.Count} of {groups.Length} groups");
        return Finish();
    }

    // Longest first onto the least-loaded part; ties keep the lower part and the earlier group, so the result is a pure function.
    internal static int[] Assign(int[] costs, int parts)
    {
        var load = new long[parts];
        var owner = new int[costs.Length];
        foreach (int g in Enumerable.Range(0, costs.Length).OrderByDescending(g => costs[g]).ThenBy(g => g))
        {
            int best = 0;
            for (int k = 1; k < parts; k++) if (load[k] < load[best]) best = k;
            owner[g] = best;
            load[best] += costs[g];
        }
        return owner;
    }

    private static (int Index, int Count)? ParsePart(string[] args)
    {
        string? value = args.LastOrDefault(arg => arg.StartsWith("--part=", StringComparison.Ordinal))?["--part=".Length..];
        if (value is null) return null;
        string[] fields = value.Split('/');
        if (fields.Length == 2 && int.TryParse(fields[0], out int k) && int.TryParse(fields[1], out int n) && k >= 1 && k <= n) return (k, n);
        throw new ArgumentException($"--part={value} is not k/n with 1 <= k <= n");
    }

    private static int Finish()
    {
        bool selectionMatched = true;
        if (only is not null)
        {
            string[] unmatched = only.Length == 0 ? ["(empty selector)"] : only.Where(prefix => !matched.Contains(prefix)).ToArray();
            foreach (string prefix in unmatched) Console.WriteLine("FAIL SELECTOR " + prefix + " matched no check");
            selectionMatched = unmatched.Length == 0;
        }
        Console.WriteLine($"RESULT failures={failures}");
        return failures == 0 && selectionMatched ? 0 : 1;
    }

    internal static void Check(string name, Action assertion)
    {
        if (only is not null)
        {
            string[] hits = only.Where(prefix => name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            if (hits.Length == 0) return;
            matched.UnionWith(hits);
        }
        long started = Stopwatch.GetTimestamp();
        // Test-runner boundary: report unexpected exceptions as failures and continue.
        try { assertion(); Console.WriteLine("PASS " + name); }
        catch (Exception failure) { failures++; Console.WriteLine("FAIL " + name + " " + failure.GetType().Name + ": " + failure.Message); }
        double ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Console.WriteLine("COST " + name + " " + ms.ToString("F3", CultureInfo.InvariantCulture));
    }

    internal static void Equal<T>(T expected, T actual, string what = "")
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{what} expected {expected}; actual {actual}".TrimStart());
    }
}
