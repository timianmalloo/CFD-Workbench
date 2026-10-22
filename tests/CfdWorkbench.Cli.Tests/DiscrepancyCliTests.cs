using System.Text.Json;
using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;

/// <summary>
/// A3d-2 (design a3d-compare.md 3, 7.4): <c>inspect --runs</c> lists the Discrepancy records in canonical orientation with
/// lines recomputed from the two runs, and prints nothing new for a document without a record. The listing is built by
/// <c>Cli.DiscrepancyListing</c>, which the checks call in process, so they run where the native store does not (Windows).
/// </summary>
internal static class DiscrepancyCliTests
{
    private static readonly JsonSerializerOptions Camel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static int Run()
    {
        int failures = 0;
        failures += Check("InspectRuns_ListsDiscrepancies_RecomputedLines", ListsRecomputed);
        failures += Check("InspectRuns_NoDiscrepancies_OutputUnchanged", NoneUnchanged);
        // A3 review condition 11: the CLI binary itself, on a project saved through the Windows store (the native store runs there now).
        failures += Check("InspectRuns_ChildHarness_SkipLineText", SkipLineText);
        failures += Check("InspectRuns_ChildHarness_TimeoutIsNamedAndBounded", ChildTimeout);
        failures += Check("InspectRuns_ChildHarness_FailureCarriesStderr", ChildFailureText);
        if (OperatingSystem.IsWindows())
        {
            failures += Check(BinaryNoRecords, () => BinaryRuns(withRecord: false));
            failures += Check(BinaryOneRecord, () => BinaryRuns(withRecord: true));
        }
        else
        {
            // The native store runs on Windows only; elsewhere the two checks print a SKIP line, so a run that skipped them says so.
            Console.WriteLine(SkipLine(BinaryNoRecords));
            Console.WriteLine(SkipLine(BinaryOneRecord));
        }
        return failures;
    }

    private const string BinaryNoRecords = "InspectRuns_WindowsStore_BinaryNoRecords_OutputUnchanged";
    private const string BinaryOneRecord = "InspectRuns_WindowsStore_BinaryOneRecord_ListsDiscrepancies";

    private static void SkipLineText()
    {
        Require(SkipLine(BinaryNoRecords) == "SKIP InspectRuns_WindowsStore_BinaryNoRecords_OutputUnchanged windows-only", "the SKIP line is 'SKIP <name> windows-only'");
        Require(SkipLine(BinaryOneRecord).StartsWith("SKIP " + BinaryOneRecord + " ", StringComparison.Ordinal), "the second check has its own line");
    }

    private static void ChildTimeout()
    {
        Require(ChildTimeoutVariable == "CFD_TEST_CHILD_TIMEOUT_SECONDS", "the variable is the one the Desktop harness reads");
        Require(ChildLimitSeconds(null) == 120 && ChildLimitSeconds("") == 120 && ChildLimitSeconds("abc") == 120 && ChildLimitSeconds("0") == 120 && ChildLimitSeconds("-5") == 120,
            "an unset, empty, non-numeric or non-positive value gives the default 120 s");
        Require(ChildLimitSeconds("7") == 7, "a positive value is the limit");
        string line = ChildFailure("inspect --runs", exit: -1, timedOut: true, limit: 7, stderr: "");
        Require(line == "TIMEOUT inspect --runs after 7 s (CFD_TEST_CHILD_TIMEOUT_SECONDS); its process tree was killed", "the timeout line names the limit and the variable: " + line);
    }

    private static void ChildFailureText()
    {
        string line = ChildFailure("inspect --runs", exit: 3, timedOut: false, limit: 120, stderr: "first\r\nsecond\n");
        Require(line == "inspect --runs exited 3; stderr: first | second", "the failure line carries stderr on one line: " + line);
        Require(ChildFailure("inspect --runs", 3, false, 120, "").EndsWith("exited 3; stderr: (empty)", StringComparison.Ordinal), "an empty stderr is said to be empty");
        Require(ChildFailure("inspect --runs", 3, false, 120, new string('x', 2000)).Length < 600, "a long stderr is cut");
    }

    private static void BinaryRuns(bool withRecord)
    {
        using var session = Session(out var vlm, out var estimator);
        if (withRecord) Require(Compare.Record(session, vlm, estimator).Record is not null, "the record was made");
        string path = TestTemp.Combine(Guid.NewGuid().ToString("N") + ".cfdw.json");
        try
        {
            using (var store = new CfdWorkbench.Persistence.ProjectStore())
            {
                var saved = store.SaveAsync(path, new CfdWorkbench.Persistence.SaveRequest(session.SaveImage(), null, Guid.NewGuid().ToString("D"))).GetAwaiter().GetResult();
                Require(saved.Code == "OK", "saved through the store: " + saved.Code);
            }
            var info = new System.Diagnostics.ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "CfdWorkbench.Cli.exe"))
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("inspect"); info.ArgumentList.Add(path); info.ArgumentList.Add("--runs");
            int limit = ChildLimitSeconds(Environment.GetEnvironmentVariable(ChildTimeoutVariable));
            using var child = System.Diagnostics.Process.Start(info)!;
            // Both pipes are drained while the child runs, so a full stderr pipe cannot stall it; the wait is bounded.
            var stdoutTask = child.StandardOutput.ReadToEndAsync();
            var stderrTask = child.StandardError.ReadToEndAsync();
            bool finished = child.WaitForExit(limit * 1000);
            if (!finished) child.Kill(entireProcessTree: true);
            child.WaitForExit();
            string stdout = stdoutTask.GetAwaiter().GetResult(), stderr = stderrTask.GetAwaiter().GetResult();
            Require(finished && child.ExitCode == 0, ChildFailure("binary inspect --runs", child.ExitCode, !finished, limit, stderr) + "; stdout: " + stdout);
            using var json = JsonDocument.Parse(stdout);
            Require(json.RootElement.GetProperty("runs").GetArrayLength() == 2, "both runs listed");
            Require(json.RootElement.TryGetProperty("discrepancies", out var listed) == withRecord, "discrepancies member present only with a record");
            if (withRecord) Require(listed.GetArrayLength() == 1, "one record listed");
            else
            {
                // Unchanged output: exactly the three members of the pre-A3 listing, in the pre-A3 order.
                Require(string.Join(",", json.RootElement.EnumerateObject().Select(member => member.Name)) == "schemaVersion,runs,pruned", "the unchanged member list");
            }
        }
        finally { File.Delete(path); }
    }

    // ---- the child-process harness lines (wave 2 AF): the Desktop harness's names, kept for the one child this project starts ----

    /// <summary>The environment variable that bounds a child, in seconds; the Desktop harness reads the same one.</summary>
    internal const string ChildTimeoutVariable = "CFD_TEST_CHILD_TIMEOUT_SECONDS";

    /// <summary>The child limit: a positive whole number of seconds from the variable, else 120.</summary>
    internal static int ChildLimitSeconds(string? value) => int.TryParse(value, out int seconds) && seconds > 0 ? seconds : 120;

    /// <summary>The line a Windows-only check prints elsewhere.</summary>
    internal static string SkipLine(string name) => "SKIP " + name + " windows-only";

    /// <summary>The failure text of a child: a timeout names the limit and the variable; an exit carries stderr on one line, cut at 400 characters.</summary>
    internal static string ChildFailure(string what, int exit, bool timedOut, int limit, string stderr)
    {
        if (timedOut) return $"TIMEOUT {what} after {limit} s ({ChildTimeoutVariable}); its process tree was killed";
        string flat = string.Join(" | ", stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (flat.Length > 400) flat = flat[..400] + "…";
        return $"{what} exited {exit}; stderr: {(flat.Length == 0 ? "(empty)" : flat)}";
    }

    private static int Check(string name, Action check)
    {
        try { check(); Console.WriteLine("PASS " + name); return 0; }
        catch (Exception error)
        {
            Console.WriteLine("FAIL " + name + " " + error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }

    private static void ListsRecomputed()
    {
        using var session = Session(out var vlm, out var estimator);
        var record = Compare.Record(session, vlm, estimator).Record!;
        using var listing = JsonDocument.Parse(JsonSerializer.Serialize(Cli.DiscrepancyListing(session), Camel));
        var item = listing.RootElement[0];
        Require(item.GetProperty("baseRunId").GetString() == estimator.RunId, "canonical orientation: the estimator run first");
        Require(item.GetProperty("comparedRunId").GetString() == vlm.RunId, "the VLM run second");
        Require(item.GetProperty("integrity").GetString() == "Intact", "intact");
        var lines = item.GetProperty("lines");
        Require(lines.GetArrayLength() == 3 && lines[0].GetProperty("quantity").GetString() == "CL" && lines[0].TryGetProperty("delta", out _), "CL carries a delta");
        Require(lines[2].GetProperty("notCompared").GetString() == "ANA-CMP-ESTIMATOR-E" && lines[2].GetProperty("base").ValueKind == JsonValueKind.Null &&
                !lines[2].TryGetProperty("delta", out _), "the estimator's e is not compared and its side is null");
        // A record whose stored line is not what the rule gives from the two runs (the hash fixed) must not print Intact.
        using var tampered = Session(out var vlm2, out var estimator2);
        var changed = record.Lines.ToArray();
        changed[0] = changed[0] with { Delta = changed[0].Delta + 1e-6 };
        tampered.RecordDiscrepancy(DiscrepancyRecords.Seal(record with { Id = Guid.NewGuid().ToString("D"), BaseRunId = estimator2.RunId,
            ComparedRunId = vlm2.RunId, Lines = changed }));
        using var after = JsonDocument.Parse(JsonSerializer.Serialize(Cli.DiscrepancyListing(tampered), Camel));
        Require(after.RootElement[0].GetProperty("integrity").GetString() == "NoLongerReproduces", "a tampered line is recomputed, not printed as Intact");
    }

    private static void NoneUnchanged()
    {
        using var session = Session(out _, out _);
        Require(Cli.DiscrepancyListing(session) is null, "no discrepancies member for a document with no record");
    }

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    // The example foil with a VLM run (real strips, so CL, CDi and e are numbers) and the estimator run at the same inputs.
    private static AuthoringSession Session(out AnalysisRun vlm, out AnalysisRun estimator)
    {
        var session = new AuthoringSession();
        session.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(assignment => assignment.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        vlm = Row(inputs, "vlm-strip", 4);
        estimator = Row(inputs, "estimator", 0);
        session.RecordRun(vlm);
        session.RecordRun(estimator);
        return session;
    }

    private static AnalysisRun Row(RunInputs inputs, string tier, int strips)
    {
        bool estimator = tier == "estimator";
        var settings = new RunSettings(estimator ? 0 : 64, estimator ? 0 : 4, "cosine", "cosine", 20, "+x", 1e-8,
            estimator ? "estimator-envelope/1" : "vlm-envelope/1", null, [2, 4], "clean", 0.3, SectionEtas: [0, 0.5, 1], SectionXs: [0, 1]);
        string settingsHash = RunRecord.SettingsHash(settings);
        var method = new RunMethod(estimator ? "cfdw.estimator.wing" : "cfdw.vlm-strip", "1.0.0", estimator ? 0 : 1);
        var water = CliFakeWing.Water(15, 35.16504);
        var op = OperatingPoints.Custom(5.14444, 3, 0.5);
        var rows = Enumerable.Range(0, strips).Select(j => new StripLoad(j, 0.01 * j, j / (double)Math.Max(1, strips), 0.12, 0.31 + 0.001 * j, 0.01, 3.0, 4.2e5, 0.4,
            new StripValue(null, "no polar method installed"), new StripValue(null, "no polar method installed"),
            0.1, 0.2, 40.5 + j, 1.5, -0.25, 0.75, -0.0125, YLow: 0.01 * j, YHigh: 0.01 * (j + 1))).ToArray();
        var run = new AnalysisRun(Guid.NewGuid().ToString("D"), RunRecord.Key(inputs, water, op, method, settingsHash), "", new RunOutcome.Completed(), tier, method,
            settings, settingsHash, inputs, water, op, ReferenceQuantities.From(Cli.ExampleBytes()), 0.01, rows, 1, new RunPlatform("osx", "arm64", "10.0"),
            estimator ? null : new RunDiagnostics(1e-13, 42.5));
        return run with { ContentHash = RunRecord.ContentHash(run) };
    }
}
