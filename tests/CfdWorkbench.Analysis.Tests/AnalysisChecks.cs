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
            return Finish();
        }
        ArchitectureTests.Run();
        RunStoreTests.Run();
        LatticeFixtureTests.Run();
        StripFixtureTests.Run();
        ServiceTests.Run();
        FreshnessTests.Run();
        ProjectionTests.Run();
        LabelsTests.Run();
        LoadsViewTests.Run();
        PanelCpTests.Run();
        SectionEstimatorTests.Run();
        CavitationTests.Run();
        return Finish();
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
