using System.Text.RegularExpressions;

namespace CfdWorkbench.Desktop.Tests;

// Track OBS (Ruling 170): a killed ring log must say slow or hung. Ring: fast (in-process, before the spawn). Cost: under 5 ms.
public static class StageTimingTests
{
    public static void Run()
    {
        DesktopChecks.Check("StageLine_CarriesElapsedMs_KeepsPrefixAndName", () =>
        {
            var output = new StringWriter();
            DesktopChecks.Stage("native-review-options", output);
            string line = output.ToString().TrimEnd();
            if (!Regex.IsMatch(line, @"^STAGE native-review-options elapsed_ms=\d+$"))
                throw new Exception("STAGE line has no elapsed_ms after the stage name: " + line);
        });
        DesktopChecks.Check("Spawn_PrintsOneSpawnStartPerMode_InStartOrder", () =>
        {
            var output = new StringWriter();
            var started = new List<string>();
            string[] modes = ["--c", "--b", "--a"];
            int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, mode => { lock (started) started.Add(mode); return ([], 0, 0.0); }, modes);
            string[] starts = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd())
                .Where(l => l.StartsWith("SPAWN-START ", StringComparison.Ordinal)).ToArray();
            if (starts.Length != modes.Length) throw new Exception("expected one SPAWN-START per mode, got: " + string.Join(" | ", starts));
            for (int i = 0; i < modes.Length; i++)
                if (!Regex.IsMatch(starts[i], "^SPAWN-START " + Regex.Escape(modes[i]) + @" elapsed_ms=\d+$"))
                    throw new Exception($"SPAWN-START {i} is not '<mode> elapsed_ms=<n>' for {modes[i]}: {starts[i]}");
            if (exit != 0 || started.Count != 3) throw new Exception($"exit {exit}, ran {started.Count} children");
        });
        // Ruling 182: a window mode's output must carry its SCALE_CONTEXT line. Ring: fast (in-process). Cost: under 5 ms.
        DesktopChecks.Check("Spawn_WindowModeWithoutScaleContext_Fails", () =>
        {
            const string line = "SCALE_CONTEXT mode=views RenderScaling=1 PrimaryScaling=1 WorkingArea=1x1 UseLayoutRounding=True";
            int Run(string[] lines, out string text)
            {
                var output = new StringWriter();
                int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, _ => (lines.Select(l => (false, l)).ToList(), 0, 0.0), ["--views"]);
                text = output.ToString();
                return exit;
            }
            if (Run([line], out _) != 0) throw new Exception("a mode that printed its SCALE_CONTEXT line failed");
            if (Run(["PASS x"], out string missing) == 0 || !missing.Contains("FAIL SCALE_CONTEXT --views", StringComparison.Ordinal))
                throw new Exception("a window mode with no SCALE_CONTEXT line did not fail: " + missing);
        });
        // Ruling 184 (option A): `--scale-diagnostic` prints one SCALE_CONTEXT line, runs no check, exits 0; a setup failure is one FAIL line.
        // Ring: fast (in-process, pure over a fake line source). Cost: under 5 ms.
        DesktopChecks.Check("ScaleDiagnostic_PrintsOneScaleLine_NoCheckLines_ExitsZero", () =>
        {
            var output = new StringWriter();
            int exit = DesktopChecks.ScaleDiagnostic(mode => $"SCALE_CONTEXT mode={mode} RenderScaling=1.5", output);
            string[] lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd()).ToArray();
            if (exit != 0 || lines.Length != 1 || !lines[0].StartsWith("SCALE_CONTEXT mode=scale-diagnostic ", StringComparison.Ordinal))
                throw new Exception($"exit {exit}, lines: {string.Join(" | ", lines)}");
            var failed = new StringWriter();
            int failExit = DesktopChecks.ScaleDiagnostic(_ => throw new InvalidOperationException("no display"), failed);
            if (failExit == 0 || !failed.ToString().StartsWith("FAIL SCALE_DIAGNOSTIC no display", StringComparison.Ordinal))
                throw new Exception($"setup failure exit {failExit}: {failed}");
        });
    }
}
