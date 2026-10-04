using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using System.Text.Json;

var output = new StringWriter();
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
string large = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".foil");
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
    Console.WriteLine("FAIL Cli_InspectJson_PointsRolesAndKinds");
    throw new InvalidOperationException(error.Message);
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
    Console.WriteLine("FAIL Cli_InspectJson_ChannelPointsRolesAndKinds");
    throw new InvalidOperationException(error.Message);
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
    Console.WriteLine("FAIL Cli_Inspect_ListsSectionPointTypesAndKinds");
    throw new InvalidOperationException(error.Message);
}
// CLI-01 (design area3-analysis.md §13.3, track SVC): `analyse` and the GUI path on one operating point give one run key,
// and `inspect --runs` lists the GUI's stored run under the key recomputed from its manifest. In process, never a binary.
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
    var gui = await new AnalysisService(session, host.Method).EvaluateAsync(OperatingPoints.Custom(5.14444, 3, 0.5),
        host.Water(OperatingPoints.DefaultTemperatureC, OperatingPoints.SaltSalinityGPerKg), Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
    if (cliKey != gui.RunKey) throw new Exception($"CLI key {cliKey} differs from the GUI key {gui.RunKey}");
    string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cfdw.json");
    try
    {
        await File.WriteAllBytesAsync(file, session.SaveImage());
        output.GetStringBuilder().Clear();
        exit = await Cli.RunAsync(["inspect", file, "--runs"], output);
        if (exit != 0) throw new Exception($"inspect --runs returned {exit}: {output}");
        using var runsJson = JsonDocument.Parse(output.ToString());
        var runs = runsJson.RootElement.GetProperty("runs");
        if (runs.GetArrayLength() != 1 || runs[0].GetProperty("runKey").GetString() != gui.RunKey ||
            runs[0].GetProperty("integrity").GetString() != "Intact")
            throw new Exception($"inspect --runs did not list the GUI run: {output}");
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
    Console.WriteLine("PASS Cli_AnalyseRunKey_EqualsGui");
}
catch (Exception error)
{
    Console.WriteLine("FAIL Cli_AnalyseRunKey_EqualsGui");
    throw new InvalidOperationException(error.Message);
}
finally
{
    Console.WriteLine("COST Cli_AnalyseRunKey_EqualsGui " +
        System.Diagnostics.Stopwatch.GetElapsedTime(analyseStarted).TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>A fixed wing method for the CLI-01 check: the key depends on inputs and settings, never on these numbers.</summary>
internal sealed class CliFakeWing : IWingMethod
{
    public RunMethod Method { get; } = new("cfdw.vlm-strip", "1.0.0", 1);
    public RunSettings Settings { get; } = new(64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3);
    public double ReconciliationTolerance => 0.01;
    public IReadOnlyList<double> Etas { get; } = [0, 1];
    public IReadOnlyList<double> Xs { get; } = [0, 0.5, 1];

    public static WaterRecord Water(double temperatureC, double salinityGPerKg) =>
        new(temperatureC, salinityGPerKg, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));

    public RunReference Reference(byte[] source) => new(0.108, 0.9, 0.12, "frame origin", "body; wind for lift/drag");

    public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
        new([0.3], [-0.01], new RunDiagnostics(1e-13, 42.5));

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
        WaterRecord water, CancellationToken cancellation) =>
        [new StripLoad(0, 0.1, 0.5, 0.12, 0.3, 0.01, 3.0, 4.2e5, 0.4, new StripValue(null, "no polar method installed"),
            new StripValue(null, "no polar method installed"), 0.1, 0.2, 40.5, 1.5, -0.25, 0.75, -0.01)];
}
