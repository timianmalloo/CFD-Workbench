using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using System.Text;

/// <summary>
/// The <c>export</c> verb (Export design 6.1, Ruling 194 (4)): the .dat equals the committed fixture, the STL passes the closure check,
/// the half names its file, the hardened write refuses a link, 3mf is refused by name, and an invalid option prints usage.
/// Ring: every join (tools/run-tests.sh); cost: under 2 s together (Cost lines per check).
/// </summary>
internal static class ExportCliTests
{
    private const string Fixture = "tests/CfdWorkbench.Core.Tests/Fixtures/export/basic-foil-root-r1.dat";

    public static async Task<int> RunAsync()
    {
        int failures = 0;
        foreach (var (name, check) in new (string, Func<Task>)[]
        {
            ("Cli_Export_DatEqualsFixtureAndPrintsSummary", DatEqualsFixture),
            ("Cli_Export_StlPassesClosureCheck", StlPasses),
            ("Cli_Export_HalfNamesFileHalf", HalfNamesFile),
            ("Cli_Export_SymlinkTargetRefused", SymlinkRefused),
            ("Cli_Export_3mfPassesPackageCheckAndHalfNamesFile", ThreeMfWrites),
            ("Cli_Export_StationTakesTheAppsNames", StationNamesAgree),
            ("Cli_Export_InvalidOptionPrintsUsage", InvalidOptions),
            ("Cli_Export_TrailingEdgeBelowFloorAdvisesAndWrites", BelowFloorWrites),
            ("Cli_Export_GeometryNotAcceptedRefused", GeometryRefused),
            ("Cli_Export_ForcedExtensionNeverReplaces", ForcedExtension),
            ("Cli_Export_UnwritablePathExitsIo", UnwritablePath),
            ("Cli_Export_EmitsOneTelemetryEventPerOutcome_NoPathNoName", Telemetry),
        })
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                await check();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception error)
            {
                Console.WriteLine("FAIL " + name + " " + error.GetType().Name + ": " + error.Message);
                failures++;
            }
            Console.WriteLine("COST " + name + " " + System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds
                .ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
        }
        return failures;
    }

    private static async Task<(int Exit, string Output)> Run(params string[] args)
    {
        var output = new StringWriter();
        int exit = await Cli.RunAsync(args, output);
        return (exit, output.ToString());
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Exits(int expected, (int Exit, string Output) run) =>
        Require(run.Exit == expected, $"exit {run.Exit}, expected {expected}: {run.Output}");

    private static async Task WithFolder(Func<string, Task> body)
    {
        string folder = TestTemp.NewDirectory("export-cli-");
        try { await body(folder); }
        finally { Directory.Delete(folder, recursive: true); }
    }

    private static async Task DatEqualsFixture() => await WithFolder(async folder =>
    {
        string file = Path.Combine(folder, "root.dat");
        var run = await Run("export", "example", "--format", "dat", "--out", file);
        Exits(0, run);
        Require(File.ReadAllBytes(file).AsSpan().SequenceEqual(File.ReadAllBytes(Fixture)), "the .dat differs from the committed fixture");
        // Ruling 204 (3): the sentence once, not "Revision: Revision r1, accepted."
        Require(!run.Output.Contains("Revision: Revision", StringComparison.Ordinal), "the Revision row repeats its label:\n" + run.Output);
        // The dialog's rows, as plain text.
        foreach (string line in new[] { "Revision r1, accepted.", "Fidelity: ", "Trailing edge: Least thickness ", "(" + Settings.TrailingEdgeFloorLabel + ")",
            "  " + CfdWorkbench.Analysis.Export.ExportCopy.ManufacturingNotAssessed, "Limit: ", "Safety: ", "Exported root.dat", "Path: " + file })
            Require(run.Output.Contains(line, StringComparison.Ordinal), "the output lacks: " + line + "\n" + run.Output);
        Require(Directory.GetFiles(folder).Length == 1, "a temp file was left behind");
    });

    private static async Task StlPasses() => await WithFolder(async folder =>
    {
        string file = Path.Combine(folder, "wing.stl");
        var run = await Run("export", "example", "--format", "stl", "--out", file);
        Exits(0, run);
        var check = StlExport.Check(File.ReadAllBytes(file));
        Require(check.Closed && check.Triangles > 0 && check.UnpairedEdges == 0 && check.ZeroAreaTriangles == 0, "the written STL is not a closed mesh");
        Require(run.Output.Contains("Mesh: ", StringComparison.Ordinal) && run.Output.Contains("Size of the part: ", StringComparison.Ordinal),
            "the STL summary rows are missing:\n" + run.Output);
    });

    private static async Task HalfNamesFile() => await WithFolder(async folder =>
    {
        // An existing folder as --out takes the dialog's suggested name (design 6.4), which carries -half.
        var run = await Run("export", "example", "--format", "stl", "--scope", "half", "--tolerance", "draft", "--out", folder);
        Exits(0, run);
        string[] files = Directory.GetFiles(folder).Select(Path.GetFileName).Cast<string>().ToArray();
        Require(files.Length == 1 && files[0] == "basic-foil-r1-half-mm.stl", "the half file is named " + string.Join(",", files));
        Require(StlExport.Check(File.ReadAllBytes(Path.Combine(folder, files[0]))).Closed, "the half is not closed");
        var whole = await Run("export", "example", "--format", "stl", "--tolerance", "draft", "--out", folder);
        Exits(0, whole);
        Require(File.Exists(Path.Combine(folder, "basic-foil-r1-mm.stl")), "the whole wing file name is wrong");
    });

    private static async Task SymlinkRefused() => await WithFolder(async folder =>
    {
        string real = Path.Combine(folder, "real.dat"), link = Path.Combine(folder, "link.dat");
        await File.WriteAllTextAsync(real, "keep");
        File.CreateSymbolicLink(link, real);
        var run = await Run("export", "example", "--format", "dat", "--out", link);
        Exits(6, run);
        Require(run.Output.Contains("EXPORT-TARGET-LINK", StringComparison.Ordinal), run.Output);
        Require(File.ReadAllText(real) == "keep", "the link target was overwritten");
        Require((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0, "the link was replaced");
        Require(Directory.GetFiles(folder).Length == 2, "a temp file was left behind");
    });

    private static async Task ThreeMfWrites() => await WithFolder(async folder =>
    {
        string file = Path.Combine(folder, "wing.3mf");
        var run = await Run("export", "example", "--format", "3mf", "--out", file);
        Exits(0, run);
        var check = ThreeMfExport.Check(File.ReadAllBytes(file));
        Require(check.Closed && check.Triangles > 0 && check.UnpairedEdges == 0, "the written 3MF package does not pass the check");
        Require(run.Output.Contains("Mesh: ", StringComparison.Ordinal), "the 3MF summary rows are missing:\n" + run.Output);
        var half = await Run("export", "example", "--format", "3mf", "--scope", "half", "--tolerance", "draft", "--out", folder);
        Exits(0, half);
        Require(File.Exists(Path.Combine(folder, ThreeMfExport.FileName("Basic foil", 1, StlScope.Half))) &&
                ThreeMfExport.FileName("Basic foil", 1, StlScope.Half).Contains("-half", StringComparison.Ordinal) &&
                Directory.GetFiles(folder, "*-half*.3mf").Length == 1, "the half 3MF is not named -half.3mf");
    });

    // Ruling 203 (1): --station takes the app's own names, root, tip or the n of "Station n"; the default is the root.
    private static async Task StationNamesAgree() => await WithFolder(async folder =>
    {
        using var authoring = new AuthoringSession();
        authoring.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
        var assignments = authoring.InspectAccepted().Authored.Assignments;
        string[] choices = [.. StationNames.Choices([.. assignments.Select(a => a.Eta)])];
        Require(choices.Length == assignments.Count && choices[0] == "root" && choices[^1] == "tip", "choices: " + string.Join(",", choices));
        for (int index = 0; index < assignments.Count; index++)
        {
            var run = await Run("export", "example", "--format", "dat", "--station", choices[index], "--out", folder);
            Exits(0, run);
            string name = StationNames.Of(index, assignments[index].Eta);
            Require(File.Exists(Path.Combine(folder, $"basic-foil-{DatImport.Slug(name)}-r1.dat")), $"station {choices[index]} did not write {name}");
            Require(run.Output.Contains($"Chord at {name}:", StringComparison.Ordinal), $"the summary does not name {name}:\n" + run.Output);
        }
        // The example has only a root and a tip; the numbered label is checked on three stations.
        double[] three = [0, 0.5, 1];
        Require(StationNames.Resolve("2", three) == 1 && StationNames.Resolve("Station 2", three) == 1 && StationNames.Resolve("1", three) is null &&
                StationNames.Resolve("3", three) is null && StationNames.Of(1, 0.5) == "Station 2" &&
                string.Join(",", StationNames.Choices(three)) == "root,2,tip", "the numbered station label is not resolved");
        var upper = await Run("export", "example", "--format", "dat", "--station", "TIP", "--out", folder);
        Exits(0, upper);
        var defaulted = await Run("export", "example", "--format", "dat", "--out", folder);
        Require(defaulted.Output.Contains("Chord at Root:", StringComparison.Ordinal), "the default is not the root:\n" + defaulted.Output);
        foreach (string bad in new[] { "0", "1", "99", "-1", "middle" })
        {
            var refused = await Run("export", "example", "--format", "dat", "--station", bad, "--out", folder);
            Exits(2, refused);
            Require(refused.Output.Contains("root", StringComparison.Ordinal) && refused.Output.Contains("tip", StringComparison.Ordinal) &&
                    refused.Output.Contains("EXPORT-USAGE", StringComparison.Ordinal), $"--station {bad} does not list the names:\n" + refused.Output);
        }
    });

    private static async Task InvalidOptions() => await WithFolder(async folder =>
    {
        string file = Path.Combine(folder, "x.dat");
        var cases = new (string Why, string[] Args)[]
        {
            ("unknown option", ["export", "example", "--format", "dat", "--out", file, "--bogus", "1"]),
            ("missing format", ["export", "example", "--out", file]),
            ("missing out", ["export", "example", "--format", "dat"]),
            ("missing input", ["export", "--format", "dat", "--out", file]),
            ("unknown format", ["export", "example", "--format", "step", "--out", file]),
            ("stl option on dat", ["export", "example", "--format", "dat", "--scope", "half", "--out", file]),
            ("dat option on stl", ["export", "example", "--format", "stl", "--order", "selig", "--out", file]),
            ("free numeric tolerance", ["export", "example", "--format", "stl", "--tolerance", "0.02", "--out", file]),
            ("points", ["export", "example", "--format", "dat", "--points", "50", "--out", file]),
            ("unknown station", ["export", "example", "--format", "dat", "--station", "99", "--out", file]),
            ("option without value", ["export", "example", "--format", "dat", "--out"]),
            ("repeated option", ["export", "example", "--format", "dat", "--format", "dat", "--out", file]),
        };
        foreach (var (why, args) in cases)
        {
            var run = await Run(args);
            Require(run.Exit == 2, $"{why}: exit {run.Exit}: {run.Output}");
            Require(run.Output.Contains("EXPORT-USAGE", StringComparison.Ordinal), $"{why}: no usage error: {run.Output}");
            Require(run.Output.Contains("Usage: cfd-workbench export", StringComparison.Ordinal), $"{why}: no usage text: {run.Output}");
        }
        Require(!File.Exists(file), "an invalid run wrote a file");
        // The old verbs' usage still names every verb.
        var other = await Run("nonsense");
        Require(other.Exit == 2 && other.Output.Contains("export <", StringComparison.Ordinal), "the general usage does not name export: " + other.Output);
    });

    private static async Task BelowFloorWrites() => await WithFolder(async folder =>
    {
        string text = Encoding.UTF8.GetString(Cli.ExampleBytes())
            .Replace("(0.9, 0.01), (1, 0)] ids", "(0.9, 0.01), (1, 0.001)] ids", StringComparison.Ordinal)
            .Replace("(0.9, -0.01), (1, 0)] ids", "(0.9, -0.01), (1, -0.001)] ids", StringComparison.Ordinal)
            .Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal);
        string foil = Path.Combine(folder, "thin.foil"), file = Path.Combine(folder, "thin.dat");
        await File.WriteAllTextAsync(foil, text);
        var run = await Run("export", foil, "--format", "dat", "--out", file);
        Exits(0, run);
        Require(run.Output.Contains("Advisory: Trailing edge 0.26 mm, below the floor of 0.30 mm (" + Settings.TrailingEdgeFloorLabel + ")", StringComparison.Ordinal),
            "no below-the-floor advisory:\n" + run.Output);
        Require(File.Exists(file), "the file was not written despite the advisory");
    });

    private static async Task GeometryRefused() => await WithFolder(async folder =>
    {
        string file = Path.Combine(folder, "bad.dat");
        var run = await Run("export", "docs/examples/foildsl/invalid-geometry.foil", "--format", "dat", "--out", file);
        Require(run.Exit == 4 && run.Output.StartsWith("Error DSL-NOT-ASSESSED", StringComparison.Ordinal), $"exit {run.Exit}: {run.Output}");
        Require(!File.Exists(file), "a refused geometry wrote a file");
    });

    private static async Task ForcedExtension() => await WithFolder(async folder =>
    {
        string forced = Path.Combine(folder, "x.dat"), given = Path.Combine(folder, "x.txt");
        await File.WriteAllTextAsync(forced, "keep");
        var run = await Run("export", "example", "--format", "dat", "--out", given);
        Exits(8, run);
        Require(File.ReadAllText(forced) == "keep" && !File.Exists(given), "the forced-extension name was replaced");
        // A name that does not exist is written under the forced extension.
        var fresh = await Run("export", "example", "--format", "dat", "--out", Path.Combine(folder, "y.txt"));
        Exits(0, fresh);
        Require(File.Exists(Path.Combine(folder, "y.dat")), "the forced extension was not applied");
    });

    // Instrumentation: the CLI records to the session it opened (its ring ends with the process; there is no other sink), one event per outcome.
    private static async Task Telemetry() => await WithFolder(async folder =>
    {
        async Task<(int Exit, List<ExportTelemetry> Events)> Observe(params string[] args)
        {
            List<ExportTelemetry> seen = [];
            int exit = await ExportVerb.RunAsync(args, new StringWriter(), CancellationToken.None, events => seen.AddRange(events.Where(e => e.Export is not null).Select(e => e.Export!)));
            return (exit, seen);
        }
        string file = Path.Combine(folder, "private-wing.stl");
        var written = await Observe("export", "example", "--format", "stl", "--scope", "half", "--tolerance", "draft", "--out", file);
        Require(written.Exit == 0 && written.Events.Count == 1, $"a written export gives one event, got {written.Events.Count}");
        var w = written.Events[0];
        Require(w.Operation == "export.write" && w.Outcome == "written" && w.Format == "stl" && w.Scope == "half" && w.Preset == "draft"
            && w.Bytes == new FileInfo(file).Length && w.Triangles > 0 && w.DeviationMm is not null && w.Milliseconds >= 0, "the written event lacks a field: " + w);
        var failed = await Observe("export", "example", "--format", "dat", "--out", Path.Combine(folder, "missing", "x.dat"));
        Require(failed.Exit == 5 && failed.Events.Count == 1 && failed.Events[0] is { Operation: "export.write", Outcome: "EXPORT-FOLDER-GONE", Scope: null, Preset: null }, "a failed write gives one coded event");
        string real = Path.Combine(folder, "real.dat"), link = Path.Combine(folder, "link.dat");
        await File.WriteAllTextAsync(real, "keep");
        File.CreateSymbolicLink(link, real);
        var refused = await Observe("export", "example", "--format", "dat", "--out", link);
        Require(refused.Exit == 6 && refused.Events.Count == 1 && refused.Events[0] is { Operation: "export.validate", Outcome: "EXPORT-TARGET-LINK", Bytes: null }, "a refusal gives one validate event");
        foreach (var e in written.Events.Concat(failed.Events).Concat(refused.Events))
        {
            string text = e.ToString();
            Require(!text.Contains("private-wing", StringComparison.Ordinal) && !text.Contains(folder, StringComparison.Ordinal)
                && !text.Contains(Environment.UserName, StringComparison.Ordinal) && !text.Contains(Environment.MachineName, StringComparison.Ordinal)
                && !text.Contains("basic", StringComparison.OrdinalIgnoreCase), "an event carries an identifier: " + text);
        }
    });

    private static async Task UnwritablePath() => await WithFolder(async folder =>
    {
        var run = await Run("export", "example", "--format", "dat", "--out", Path.Combine(folder, "missing", "x.dat"));
        Exits(5, run);
        Require(run.Output.Contains("Can't write the file", StringComparison.Ordinal), run.Output);
        Require(!run.Output.Contains("earlier file", StringComparison.Ordinal), "a missing folder has no earlier file:\n" + run.Output);
    });
}
