using System.Text.Json;
using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;

/// <summary>
/// Node SWP-3 red-first tests (T17 in docs/design/local-sweeps.md §4 &amp; §8, Ruling 271):
/// CLI <c>sweep preview</c>, <c>sweep run [--out]</c>, and <c>inspect --sweeps</c> share exact
/// SweepSchedule / SweepService / RunRecord logic with the GUI, achieving bitwise runKey parity
/// and 1e-12 scalar parity (CL, CDi, Cm).
/// </summary>
internal static class SweepCliTests
{
    internal static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await CheckAsync("Cli_SweepRunAndInspect_MatchesGuiRunKeysBitwise", T17_ParityAndInspectAsync);
        failures += await CheckAsync("Cli_SweepPreview_InvalidGridRefusedWritesNothing", InvalidGridRefusedAsync);
        return failures;
    }

    private static async Task T17_ParityAndInspectAsync()
    {
        var method = new ProductWingMethod(Settings.Default with { NSpanPerHalf = 8, NChord = 2 });
        var host = new AnalysisHost(method, WaterTable.At);
        string gridJson = """{"speedsKn":[12,16],"anglesDeg":[1,3],"hRef":0.5,"water":{"temperatureC":15,"salinityGPerKg":35.16504},"tier":"vlm-strip"}""";

        // 1. CLI sweep preview
        var previewOut = new StringWriter();
        int previewExit = await Cli.RunAsync(["sweep", "preview", "example", "--grid", gridJson], previewOut, CancellationToken.None, host);
        Require(previewExit == 0, $"sweep preview exit={previewExit}: {previewOut}");
        using var previewDoc = JsonDocument.Parse(previewOut.ToString());
        var pRoot = previewDoc.RootElement;
        Require(pRoot.GetProperty("schemaVersion").GetInt32() == 1, "preview schemaVersion == 1");
        Require(pRoot.GetProperty("caseCount").GetInt32() == 4, "preview caseCount == 4");
        Require(pRoot.GetProperty("cases").GetArrayLength() == 4, "preview 4 cases");
        Require(pRoot.GetProperty("estimatedStorageBytes").GetInt32() > 0, "estimatedStorageBytes > 0");
        Require(pRoot.GetProperty("documentHeadroomBytes").GetInt32() > 0, "documentHeadroomBytes > 0");

        // 2. CLI sweep run --out <file>
        string outPath = TestTemp.Combine(Guid.NewGuid().ToString("N") + ".cfdw.json");
        try
        {
            var runOut = new StringWriter();
            int runExit = await Cli.RunAsync(["sweep", "run", "example", "--grid", gridJson, "--out", outPath], runOut, CancellationToken.None, host);
            Require(runExit == 0, $"sweep run exit={runExit}: {runOut}");
            Require(File.Exists(outPath), "--out file must be written");

            using var runDoc = JsonDocument.Parse(runOut.ToString());
            var rRoot = runDoc.RootElement;
            Require(rRoot.GetProperty("status").GetString() == "Completed", "CLI sweep status == Completed");
            var cliCases = rRoot.GetProperty("cases");
            Require(cliCases.GetArrayLength() == 4, "CLI 4 cases");

            // 3. GUI path on the exact same example, grid, water, and method -> compare bitwise runKey & 1e-12 scalars!
            using var guiSession = new AuthoringSession();
            guiSession.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
            var guiSpeeds = SweepSchedule.ParseSpeeds("12, 16", SweepSpeedUnit.Knots);
            var guiAngles = SweepSchedule.ParseAngles("1, 3");
            var guiWater = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
            var guiPreview = SweepSchedule.BuildCartesian(guiSession, method, SweepRecords.TierVlmStrip, guiSpeeds, guiAngles, guiWater, 0.5);
            var guiSvc = new SweepService(guiSession, method);
            var guiExp = guiSvc.Queue(guiPreview, "GUI parity");
            _ = await guiSvc.RunAsync(guiExp.Id, CancellationToken.None);
            var guiProj = SweepProjection.Project(guiSession, guiSession.ReadExperiments().Single(), method);

            for (int i = 0; i < 4; i++)
            {
                var cCase = cliCases[i];
                var gCase = guiProj.Cases[i];
                string? cliKey = cCase.GetProperty("runKey").GetString();
                Require(cliKey == gCase.RunKey, $"case {i} runKey mismatch: CLI={cliKey} GUI={gCase.RunKey}");
                NearRel(gCase.Cl!.Value, cCase.GetProperty("cl").GetDouble(), 1e-12, $"case {i} CL");
                NearRel(gCase.Cdi!.Value, cCase.GetProperty("cdi").GetDouble(), 1e-12, $"case {i} CDi");
                NearRel(gCase.Cm!.Value, cCase.GetProperty("cm").GetDouble(), 1e-12, $"case {i} Cm");
            }

            // 4. CLI inspect <file.cfdw.json> --sweeps
            var inspectOut = new StringWriter();
            int inspectExit = await Cli.RunAsync(["inspect", outPath, "--sweeps"], inspectOut, CancellationToken.None, host);
            Require(inspectExit == 0, $"inspect --sweeps exit={inspectExit}: {inspectOut}");
            using var inspectDoc = JsonDocument.Parse(inspectOut.ToString());
            var sweeps = inspectDoc.RootElement.GetProperty("sweeps");
            Require(sweeps.GetArrayLength() == 1, "1 stored sweep");
            var s0 = sweeps[0];
            Require(s0.GetProperty("integrity").GetString() == "Intact", "sweep integrity Intact");
            Require(s0.GetProperty("status").GetString() == "Completed", "sweep status Completed");
            Require(s0.GetProperty("archived").GetBoolean() == false, "archived false");
            var inspCases = s0.GetProperty("cases");
            Require(inspCases.GetArrayLength() == 4, "4 inspected cases");
            for (int i = 0; i < 4; i++)
            {
                Require(inspCases[i].GetProperty("runKey").GetString() == guiProj.Cases[i].RunKey, $"inspect case {i} recomputed runKey parity");
                Require(inspCases[i].GetProperty("freshness").GetString() == "Current", $"inspect case {i} Current");
            }
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    private static async Task InvalidGridRefusedAsync()
    {
        var method = new ProductWingMethod(Settings.Default with { NSpanPerHalf = 8, NChord = 2 });
        var host = new AnalysisHost(method, WaterTable.At);

        // Duplicate speed refused with ANA-SWEEP-DUPLICATE, exit 2.
        var dupOut = new StringWriter();
        int dupExit = await Cli.RunAsync(["sweep", "preview", "example", "--grid", """{"speedsKn":[14,14],"anglesDeg":[2]}"""], dupOut, CancellationToken.None, host);
        Require(dupExit == 2, $"duplicate speed exit={dupExit}");
        Require(dupOut.ToString().Contains("ANA-SWEEP-DUPLICATE", StringComparison.Ordinal), $"expected ANA-SWEEP-DUPLICATE: {dupOut}");

        // Out-of-ITTC water refused with ANA-INPUT-WATER, exit 2.
        var waterOut = new StringWriter();
        int waterExit = await Cli.RunAsync(["sweep", "preview", "example", "--grid", """{"speedsMs":[7],"anglesDeg":[2],"water":{"temperatureC":55,"salinityGPerKg":35.16504}}"""], waterOut, CancellationToken.None, host);
        Require(waterExit == 2, $"bad water exit={waterExit}");
        Require(waterOut.ToString().Contains("ANA-INPUT-WATER", StringComparison.Ordinal), $"expected ANA-INPUT-WATER: {waterOut}");

        // Unknown grid field refused with ANA-INPUT-OP, exit 2.
        var unkOut = new StringWriter();
        int unkExit = await Cli.RunAsync(["sweep", "preview", "example", "--grid", """{"speedsMs":[7],"anglesDeg":[2],"bogus":1}"""], unkOut, CancellationToken.None, host);
        Require(unkExit == 2, $"unknown field exit={unkExit}");
        Require(unkOut.ToString().Contains("ANA-INPUT-OP", StringComparison.Ordinal), $"expected ANA-INPUT-OP: {unkOut}");
    }

    private static void NearRel(double expected, double actual, double relTol, string label)
    {
        double scale = Math.Max(1.0, Math.Abs(expected));
        if (Math.Abs(expected - actual) > relTol * scale)
            throw new InvalidOperationException($"{label}: expected {expected:R}, actual {actual:R}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task<int> CheckAsync(string name, Func<Task> body)
    {
        try
        {
            await body();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine($"FAIL {name} {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }
}
