using CfdWorkbench.Cli;
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
