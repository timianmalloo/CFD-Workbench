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
        return failures;
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
