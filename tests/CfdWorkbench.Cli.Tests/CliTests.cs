using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using System.Text.Json;

var output = new StringWriter();
int cliFailures = 0;
int exit = await Cli.RunAsync(["inspect", "example", "--json"], output);
if (exit != 0) throw new Exception($"Example inspect returned {exit}: {output}");
using var json = JsonDocument.Parse(output.ToString());
var root = json.RootElement;
foreach (var field in new[] { "schemaVersion", "acceptedSourceSha256", "definitionHash", "evaluator", "acceptedId", "assessment", "diagnostics", "derived" })
    if (!root.TryGetProperty(field, out _)) throw new Exception($"Missing {field}");
if (root.GetProperty("assessment").GetProperty("status").GetString() != "Certified") throw new Exception("Example is not certified");
if (root.GetProperty("derived").GetProperty("points").GetArrayLength() != 15) throw new Exception("Expected bounded fifteen point projection");
if (root.GetProperty("derived").GetProperty("pointUnit").GetString() != "m") throw new Exception("Placed point unit is wrong");
if (root.GetProperty("derived").GetProperty("centerSectionUnit").GetString() != "z/c") throw new Exception("Normalized section was labelled in metres");
if (root.GetProperty("derived").GetProperty("centerSectionEta").GetDouble() != .5 ||
    root.GetProperty("derived").GetProperty("centerSectionNormalizedX").GetDouble() != .5) throw new Exception("Section query position is missing");
output.GetStringBuilder().Clear();
exit = await Cli.RunAsync(["inspect", "bad.txt", "--json"], output);
if (exit != 2) throw new Exception($"Wrong extension returned {exit}");
output.GetStringBuilder().Clear();
exit = await Cli.RunAsync(["inspect", "docs/examples/foildsl/invalid-evaluator.foil", "--json"], output);
if (exit != 3 || !output.ToString().Contains("DSL-VERSION", StringComparison.Ordinal)) throw new Exception($"Legacy evaluator returned {exit}: {output}");
output.GetStringBuilder().Clear();
using (var cancelled = new CancellationTokenSource())
{
    cancelled.Cancel();
    exit = await Cli.RunAsync(["inspect", "example", "--json"], output, cancelled.Token);
    if (exit != 130) throw new Exception($"Cancellation returned {exit}");
}
if (Cli.ExitForCode("GEOMETRY-BUDGET") != 4 || Cli.ExitForCode("GEOMETRY-CANCELLED") != 130 ||
    Cli.ExitForCode("DOC-CONFLICT") != 5 || Cli.ExitForCode("DOC-VERSION") != 3)
    throw new Exception("Stable refusal exit mapping is wrong");
string large = TestTemp.Combine(Guid.NewGuid().ToString("N") + ".foil");
try
{
    await File.WriteAllBytesAsync(large, new byte[1_048_577]);
    output.GetStringBuilder().Clear();
    exit = await Cli.RunAsync(["inspect", large, "--json"], output);
    if (exit != 4 || !output.ToString().Contains("DSL-LIMIT", StringComparison.Ordinal)) throw new Exception($"Oversize source returned {exit}: {output}");
}
finally { File.Delete(large); }
Console.WriteLine("CLI Example identity, assessment, bounded projection and extension refusal passed.");
output.GetStringBuilder().Clear();
exit = await Cli.RunAsync(["inspect", "example", "--json"], output);
if (exit != 0) { Console.WriteLine("FAIL Cli_InspectJson_PointsRolesAndKinds"); throw new Exception($"Example inspect returned {exit}: {output}"); }
using var pointsJson = JsonDocument.Parse(output.ToString());
var pointsRoot = pointsJson.RootElement;
if (pointsRoot.GetProperty("derived").GetProperty("points").GetArrayLength() != 15)
{
    Console.WriteLine("FAIL Cli_InspectJson_PointsRolesAndKinds");
    throw new Exception("Expected bounded fifteen point projection");
}
try
{
    var model = pointsRoot.GetProperty("points");
    var leading = model.GetProperty("leading");
    var trailing = model.GetProperty("trailing");
    if (leading.GetArrayLength() != 7) throw new Exception("Leading point count");
    if (leading[0].GetProperty("role").GetString() != "RootEnd" || leading[0].GetProperty("freedom").GetString() != "Fixed") throw new Exception("Leading root end");
    if (leading[0].GetProperty("kind").ValueKind != JsonValueKind.Null) throw new Exception("Control kind is absent");
    if (!leading[0].GetProperty("locks").EnumerateArray().Any(item => item.GetString() == "root_mirror")) throw new Exception("Root mirror lock");
    if (leading[1].GetProperty("role").GetString() != "RootHandle" || leading[1].GetProperty("freedom").GetString() != "SpanOnly") throw new Exception("Root handle");
    if (leading[2].GetProperty("role").GetString() != "Control" || leading[2].GetProperty("freedom").GetString() != "Free") throw new Exception("Control point");
    if (leading[5].GetProperty("role").GetString() != "TipHandle") throw new Exception("Tip handle");
    if (leading[6].GetProperty("role").GetString() != "TipEnd" || leading[6].GetProperty("freedom").GetString() != "ValueOnly") throw new Exception("Tip end");
    if (leading[6].GetProperty("ordinate").GetDouble() != leading[6].GetProperty("aftMeters").GetDouble()) throw new Exception("Ordinate alias");
    if (trailing[0].GetProperty("role").GetString() != "RootEnd" || trailing[0].GetProperty("freedom").GetString() != "ValueOnly") throw new Exception("Trailing root");
    if (leading[0].GetProperty("id").GetString() != "cv-0") throw new Exception("Point id");
    Console.WriteLine("PASS Cli_InspectJson_PointsRolesAndKinds");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_InspectJson_PointsRolesAndKinds " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
try
{
    var model = pointsRoot.GetProperty("points");
    foreach (string name in new[] { "leading", "trailing", "dihedral", "twist", "thickness" })
    {
        var channel = model.GetProperty(name);
        if (channel.GetArrayLength() < 2) throw new Exception(name);
        if (channel[0].GetProperty("role").GetString() != "RootEnd") throw new Exception(name + " root");
        if (channel[channel.GetArrayLength() - 1].GetProperty("role").GetString() != "TipEnd") throw new Exception(name + " tip");
    }
    if (model.GetProperty("dihedral")[0].GetProperty("freedom").GetString() != "Fixed") throw new Exception("Dihedral root");
    if (model.GetProperty("twist")[0].GetProperty("freedom").GetString() != "ValueOnly") throw new Exception("Twist root");
    if (model.GetProperty("thickness")[0].GetProperty("freedom").GetString() != "ValueOnly") throw new Exception("Thickness root");
    if (model.GetProperty("twist")[1].GetProperty("freedom").GetString() != "SpanOnly") throw new Exception("Twist handle");
    Console.WriteLine("PASS Cli_InspectJson_ChannelPointsRolesAndKinds");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_InspectJson_ChannelPointsRolesAndKinds " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
// Moved from the Core suite (SPT): the Core harness runs from a published copy where the CLI binary is not built, so the
// check now calls the CLI in-process like its neighbours. Same name and assertions.
output.GetStringBuilder().Clear();
try
{
    exit = await Cli.RunAsync(["inspect", "example", "--json"], output);
    if (exit != 0) throw new Exception($"Example inspect returned {exit}: {output}");
    using var sectionJson = JsonDocument.Parse(output.ToString());
    var sectionPoints = sectionJson.RootElement.GetProperty("sectionPoints");
    if (sectionPoints.GetArrayLength() == 0) throw new Exception("No section points");
    bool anchor = false, control = false, kindField = false;
    foreach (var point in sectionPoints.EnumerateArray())
    {
        kindField |= point.TryGetProperty("kind", out _);
        string? type = point.GetProperty("type").GetString();
        anchor |= type == "Anchor";
        control |= type == "Control";
    }
    if (!anchor || !control || !kindField) throw new Exception($"anchor={anchor} control={control} kindField={kindField}");
    Console.WriteLine("PASS Cli_Inspect_ListsSectionPointTypesAndKinds");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_Inspect_ListsSectionPointTypesAndKinds " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
// CLI-01 (design area3-analysis.md §13.3, track SVC): `analyse --op <json>` and the service on OperatingPoints.Custom (the
// one builder the conditions band also calls) give one run key for one operating point and the default water; and
// `inspect --runs` lists that stored run under the key recomputed from its manifest. In process, never a binary. What it
// does not prove: the conditions band's own call (TGL's check).
long analyseStarted = System.Diagnostics.Stopwatch.GetTimestamp();
try
{
    const string op = "{\"speed\":5.14444,\"alphaDeg\":3,\"hRef\":0.5}";
    var host = new AnalysisHost(new CliFakeWing(), CliFakeWing.Water);
    output.GetStringBuilder().Clear();
    exit = await Cli.RunAsync(["analyse", "example", "--op", op], output, CancellationToken.None, host);
    if (exit != 0) throw new Exception($"analyse returned {exit}: {output}");
    string? cliKey;
    using (var runJson = JsonDocument.Parse(output.ToString())) cliKey = runJson.RootElement.GetProperty("run").GetProperty("runKey").GetString();
    using var session = new AuthoringSession();
    session.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
    var service = await new AnalysisService(session, host.Method).EvaluateAsync(OperatingPoints.Custom(5.14444, 3, 0.5),
        host.Water(OperatingPoints.DefaultTemperatureC, OperatingPoints.SaltSalinityGPerKg), Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
    if (cliKey != service.RunKey) throw new Exception($"CLI key {cliKey} differs from the service key on OperatingPoints.Custom {service.RunKey}");
    // Neither side's builder: the conditions band's Custom point written out field by field (1 atm, datum "root LE", no
    // load; design §3.5). A default drifting in OperatingPoints.Custom, which the CLI and the band share, changes this key.
    var band = await new AnalysisService(session, host.Method).EvaluateAsync(new OperatingPoint(5.14444, 101325, 0.5, "root LE", 3, null),
        host.Water(15, 35.16504), Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
    if (cliKey != band.RunKey) throw new Exception($"CLI key {cliKey} differs from the key of the Custom point written out {band.RunKey}");
    string file = TestTemp.Combine(Guid.NewGuid().ToString("N") + ".cfdw.json");
    try
    {
        await File.WriteAllBytesAsync(file, session.SaveImage());
        output.GetStringBuilder().Clear();
        exit = await Cli.RunAsync(["inspect", file, "--runs"], output);
        if (exit != 0) throw new Exception($"inspect --runs returned {exit}: {output}");
        using var runsJson = JsonDocument.Parse(output.ToString());
        var runs = runsJson.RootElement.GetProperty("runs");
        if (runs.GetArrayLength() != 1 || runs[0].GetProperty("runKey").GetString() != service.RunKey ||
            runs[0].GetProperty("integrity").GetString() != "Intact")
            throw new Exception($"inspect --runs did not list the service run: {output}");
    }
    finally { File.Delete(file); }
    foreach (var (refused, code) in new (string[] Args, string Code)[]
             {
                 (["analyse", "example", "--op", op], "ANA-METHOD-UNAVAILABLE"),
                 (["analyse", "example", "--op", "{\"speed\":5,\"alphaDeg\":3,\"units\":\"lbf\"}"], "ANA-INPUT-OP"),
                 (["analyse", "example", "--op", "{\"speed\":0,\"alphaDeg\":3}"], "ANA-INPUT-SPEED")
             })
    {
        output.GetStringBuilder().Clear();
        exit = await Cli.RunAsync(refused, output, CancellationToken.None, code == "ANA-METHOD-UNAVAILABLE" ? null : host);
        if (exit != 2 || !output.ToString().Contains(code, StringComparison.Ordinal)) throw new Exception($"{refused[3]} returned {exit}: {output}");
    }
    Console.WriteLine("PASS Cli_AnalyseRunKey_EqualsServiceOnCustomOp");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_AnalyseRunKey_EqualsServiceOnCustomOp " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
finally
{
    Console.WriteLine("COST Cli_AnalyseRunKey_EqualsServiceOnCustomOp " +
        System.Diagnostics.Stopwatch.GetElapsedTime(analyseStarted).TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
}
// SVC-2 (IO8): `analyse` on a run whose compute fails prints the Failed run with its code and no diagnostics member, never
// a measured-looking "residualInf": 0; the exit is the code's.
long failedStarted = System.Diagnostics.Stopwatch.GetTimestamp();
try
{
    var failing = new AnalysisHost(new CliFakeWing { FailCode = "ANA-SOLVE-SINGULAR" }, CliFakeWing.Water);
    output.GetStringBuilder().Clear();
    exit = await Cli.RunAsync(["analyse", "example", "--op", "{\"speed\":5.14444,\"alphaDeg\":3}"], output, CancellationToken.None, failing);
    using var failedJson = JsonDocument.Parse(output.ToString());
    var printed = failedJson.RootElement.GetProperty("run");
    if (exit != Cli.ExitForCode("ANA-SOLVE-SINGULAR")) throw new Exception($"analyse of a failing run returned {exit}: {output}");
    if (printed.GetProperty("outcome").GetProperty("code").GetString() != "ANA-SOLVE-SINGULAR") throw new Exception($"no failure code: {output}");
    if (printed.TryGetProperty("diagnostics", out _)) throw new Exception($"a Failed run printed diagnostics: {output}");
    Console.WriteLine("PASS Cli_AnalyseFailedRun_PrintsNoDiagnostics");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_AnalyseFailedRun_PrintsNoDiagnostics " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
finally
{
    Console.WriteLine("COST Cli_AnalyseFailedRun_PrintsNoDiagnostics " +
        System.Diagnostics.Stopwatch.GetElapsedTime(failedStarted).TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
}

// Track C item 4: `inspect --runs` prints each run's revision as the session's own label for the revision the run was made
// on: the ordinal in accepted-row order and the rail of the edit that made it (RevisionOf, the label the GUI shows).
long revisionStarted = System.Diagnostics.Stopwatch.GetTimestamp();
try
{
    var revisionHost = new AnalysisHost(new CliFakeWing(), CliFakeWing.Water);
    using var revisionSession = new AuthoringSession();
    revisionSession.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
    var revisionService = new AnalysisService(revisionSession, revisionHost.Method);
    var revisionWater = revisionHost.Water(OperatingPoints.DefaultTemperatureC, OperatingPoints.SaltSalinityGPerKg);
    var expected = new List<(string RunKey, RevisionLabel Label)>();
    async Task EvaluateHere(double alphaDeg)
    {
        var run = await revisionService.EvaluateAsync(OperatingPoints.Custom(5.14444, alphaDeg, 0.5), revisionWater, Tier.VlmStrip,
            new Scope.Wing(), CancellationToken.None);
        expected.Add((run.RunKey, revisionSession.RevisionOf(run.Inputs.AcceptedId)));
    }
    void TwistEdit(double delta)
    {
        var point = Channels.View(revisionSession.Snapshot().Source, "twist", "Accepted", 0).Points[0];
        var draft = revisionSession.BeginPointGesture(Guid.NewGuid().ToString("D"), "twist", point.Id);
        var frame = revisionSession.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + delta);
        revisionSession.Apply(Guid.NewGuid().ToString("D"), revisionSession.Validate(frame.Draft.Id, frame.Draft.Generation));
    }
    await EvaluateHere(3);
    TwistEdit(1.0);
    TwistEdit(2.0);
    await EvaluateHere(3);
    if (expected[0].Label.Ordinal != 1 || expected[1].Label.Ordinal != 3 || expected[1].Label.Rail != "twist")
        throw new Exception($"the fixture's own labels are wrong: {string.Join(", ", expected.Select(item => item.Label))}");
    string revisionFile = TestTemp.Combine(Guid.NewGuid().ToString("N") + ".cfdw.json");
    try
    {
        await File.WriteAllBytesAsync(revisionFile, revisionSession.SaveImage());
        output.GetStringBuilder().Clear();
        exit = await Cli.RunAsync(["inspect", revisionFile, "--runs"], output);
        if (exit != 0) throw new Exception($"inspect --runs returned {exit}: {output}");
        using var listed = JsonDocument.Parse(output.ToString());
        var rows = listed.RootElement.GetProperty("runs").EnumerateArray().ToArray();
        if (rows.Length != expected.Count) throw new Exception($"listed {rows.Length} runs, stored {expected.Count}: {output}");
        foreach (var (row, (key, label)) in rows.Zip(expected))
        {
            if (row.GetProperty("runKey").GetString() != key) throw new Exception($"run order or key differs: {output}");
            var printed = row.GetProperty("revision");
            bool hasRail = printed.ValueKind == JsonValueKind.Object && printed.TryGetProperty("rail", out var rail) && rail.ValueKind == JsonValueKind.String;
            if (printed.ValueKind != JsonValueKind.Object || printed.GetProperty("ordinal").GetInt32() != label.Ordinal ||
                hasRail != (label.Rail is not null) || (hasRail && printed.GetProperty("rail").GetString() != label.Rail))
                throw new Exception($"run {key[..12]} printed revision {printed}, session label {label}: {output}");
        }
    }
    finally { File.Delete(revisionFile); }
    Console.WriteLine("PASS Cli_InspectRuns_RevisionIsSessionLabel");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_InspectRuns_RevisionIsSessionLabel " + error.GetType().Name + ": " + error.Message);
    cliFailures++;
}
finally
{
    Console.WriteLine("COST Cli_InspectRuns_RevisionIsSessionLabel " +
        System.Diagnostics.Stopwatch.GetElapsedTime(revisionStarted).TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
}

// A check that fails reports its own FAIL line and the harness goes on (WRT-HARNESS-ABORT); the exit says whether any failed.
return cliFailures == 0 ? 0 : 1;

/// <summary>A fixed wing method for the CLI checks: the key depends on inputs and settings, never on these numbers.</summary>
internal sealed class CliFakeWing : IWingMethod
{
    public RunMethod Method { get; } = new("cfdw.vlm-strip", "1.0.0", 1);
    public RunSettings Settings { get; } = new(64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3,
        SectionEtas: [0, 1], SectionXs: [0, 0.5, 1]);
    public double ReconciliationTolerance => 0.01;
    /// <summary>Makes the solve fail with that code.</summary>
    public string? FailCode { get; init; }

    public static WaterRecord Water(double temperatureC, double salinityGPerKg) =>
        new(temperatureC, salinityGPerKg, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));

    public RunReference Reference(byte[] source) => new(0.108, 0.9, 0.12, "frame origin", "body; wind for lift/drag");

    public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
        FailCode is not null ? throw new ContractError(FailCode, "the fake lattice failed") : new([0.3], [-0.01], new RunDiagnostics(1e-13, 42.5));

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
        WaterRecord water, CancellationToken cancellation) =>
        [new StripLoad(0, 0.1, 0.5, 0.12, 0.3, 0.01, 3.0, 4.2e5, 0.4, new StripValue(null, "no polar method installed"),
            new StripValue(null, "no polar method installed"), 0.1, 0.2, 40.5, 1.5, -0.25, 0.75, -0.01)];
}
