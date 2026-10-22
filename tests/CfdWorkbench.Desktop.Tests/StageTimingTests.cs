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
            int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, mode => { lock (started) started.Add(mode); return ([], 0, 0.0); }, modes, _ => null);
            string[] starts = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd())
                .Where(l => l.StartsWith("SPAWN-START ", StringComparison.Ordinal)).ToArray();
            if (starts.Length != modes.Length) throw new Exception("expected one SPAWN-START per mode, got: " + string.Join(" | ", starts));
            for (int i = 0; i < modes.Length; i++)
                if (!Regex.IsMatch(starts[i], "^SPAWN-START " + Regex.Escape(modes[i]) + @" elapsed_ms=\d+$"))
                    throw new Exception($"SPAWN-START {i} is not '<mode> elapsed_ms=<n>' for {modes[i]}: {starts[i]}");
            if (exit != 0 || started.Count != 3) throw new Exception($"exit {exit}, ran {started.Count} children");
        });
        // Track DTJ: the child-slot count is per host. Ring: fast (in-process, pure). Cost: under 5 ms.
        DesktopChecks.Check("DesktopSlots_EnvBaselineDefault_AndRefusals", () =>
        {
            (int, string) Resolve(string? slots, string? baseline, int modes = 10, int cpus = 16) =>
                DesktopChecks.ResolveSlots(k => k == "CFD_DESKTOP_SLOTS" ? slots : k == "CFD_DESKTOP_SLOTS_BASELINE" ? baseline : null, cpus, modes);
            if (Resolve(null, null) != (10, "default")) throw new Exception("default on 16 cpus / 10 modes is not 10 (5/8 of 16)");
            if (Resolve("1", null) != (1, "env")) throw new Exception("env 1 not read");
            if (Resolve("10", "4") != (10, "env")) throw new Exception("env does not beat the baseline");
            if (Resolve(null, "4") != (4, "baseline")) throw new Exception("baseline 4 not read");
            foreach (string bad in new[] { "11", "0", "-1", "two", "", "3.5" })
            {
                try { Resolve(bad, null); throw new Exception($"CFD_DESKTOP_SLOTS='{bad}' was accepted"); }
                catch (ArgumentException exception) when (exception.Message.Contains("CFD_DESKTOP_SLOTS", StringComparison.Ordinal)) { }
            }
            var output = new StringWriter();
            int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, _ => ([], 0, 0.0), ["--a", "--b"], k => k == "CFD_DESKTOP_SLOTS" ? "3" : null);
            if (exit != 2 || !output.ToString().Contains("FAIL DESKTOP-SLOTS", StringComparison.Ordinal)) throw new Exception("modes+1 was not refused: " + output);
            var good = new StringWriter();
            DesktopChecks.SpawnWith(good, TextWriter.Null, _ => ([], 0, 0.0), ["--a", "--b"], k => k == "CFD_DESKTOP_SLOTS" ? "1" : null);
            if (Regex.Matches(good.ToString(), @"^DESKTOP-SLOTS 1 \(source: env\)\r?$", RegexOptions.Multiline).Count != 1)
                throw new Exception("no single DESKTOP-SLOTS line: " + good);
        });
        // Ruling 182: a window mode's output must carry its SCALE_CONTEXT line. Ring: fast (in-process). Cost: under 5 ms.
        DesktopChecks.Check("Spawn_WindowModeWithoutScaleContext_Fails", () =>
        {
            const string line = "SCALE_CONTEXT mode=views RenderScaling=1 PrimaryScaling=1 WorkingArea=1x1 UseLayoutRounding=True";
            int Run(string[] lines, out string text)
            {
                var output = new StringWriter();
                int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, _ => (lines.Select(l => (false, l)).ToList(), 0, 0.0), ["--views"], _ => null);
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
        // PVH (WAIT-UNBOUNDED-A): the TIMEOUT line ends with the in-flight check, or (unknown) when the child left no note.
        // Ring: fast (in-process, pure over a temp file). Cost: under 5 ms.
        DesktopChecks.Check("TimeoutLine_NamesInFlightCheck_OrUnknownWithoutNote", () =>
        {
            string note = TestTemp.Combine("cfdwb-inflight-test-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllText(note, "Some_Check_Name");
                string named = DesktopChecks.TimeoutLine("--views --part=1/2", 7, note);
                string unknown = DesktopChecks.TimeoutLine("--views --part=1/2", 7, note + ".missing");
                if (!named.StartsWith("FAIL TIMEOUT --views --part=1/2 after 7 s", StringComparison.Ordinal) ||
                    !named.EndsWith("; in-flight check: Some_Check_Name", StringComparison.Ordinal) ||
                    !unknown.EndsWith("; in-flight check: (unknown)", StringComparison.Ordinal))
                    throw new Exception($"named: {named} | unknown: {unknown}");
            }
            finally { File.Delete(note); }
        });
        // HARNESS-CRASH-AS-VERDICT: a native termination reads as one, not as a check verdict (2026-10-22: 0xC0000374).
        // Ring: fast (pure, no child process). Cost: under 1 ms.
        DesktopChecks.Check("ExitLine_NativeTermination_NamedNotAVerdict", () =>
        {
            const string tail = ": native crash, not a check verdict";
            (int Exit, bool Windows, string Expected)[] cases =
            [
                (-1073740940, true, "FAIL m exited -1073740940 (0xC0000374)" + tail),
                (-1073741819, true, "FAIL m exited -1073741819 (0xC0000005)" + tail),
                (134, false, "FAIL m exited 134 (signal 6)" + tail),
                (124, false, "FAIL m exited 124"),
                (124, true, "FAIL m exited 124"),
                (1, true, "FAIL m exited 1"),
                (1, false, "FAIL m exited 1"),
            ];
            var wrong = cases.Select(c => (c, Got: DesktopChecks.ExitLine("m", c.Exit, c.Windows))).Where(r => r.Got != r.c.Expected)
                .Select(r => $"{r.c.Exit}/{(r.c.Windows ? "win" : "posix")}: got '{r.Got}'").ToArray();
            if (wrong.Length > 0) throw new Exception(string.Join(" | ", wrong));
        });
        // CR1: ProcDump writes UTF-16 text, so a wrapped child's stdout carries NUL characters, some at the start of a harness line
        // ("\0SCALE_CONTEXT ..."). The parent's SCALE_CONTEXT and PARTITION parsing must read the line as the harness wrote it.
        DesktopChecks.Check("Spawn_NulPrefixedChildLines_ParsedAsTheHarnessWroteThem", () =>
        {
            var output = new StringWriter();
            int exit = DesktopChecks.SpawnWith(output, TextWriter.Null, _ =>
                ([(false, "P\0r\0o\0c\0e\0s\0s\0"), (false, "\0SCALE_CONTEXT mode=properties-view RenderScaling=2"), (false, "\0PARTITION 1/1 of 68 checks")], 0, 0.0),
                ["--properties-view --part=1/1"], _ => null);
            string text = output.ToString();
            if (exit != 0 || text.Contains("FAIL", StringComparison.Ordinal))
                throw new Exception($"exit {exit}: " + string.Join(" | ", text.Split('\n').Where(l => l.Contains("FAIL", StringComparison.Ordinal))));
        });
        // CR1 crash-capture probe: with CFD_FORCE_HEAP_CORRUPTION=1 it corrupts a native heap block so ntdll raises 0xC0000374, which a
        // wrapped child (CFD_TEST_CHILD_WRAPPER) must leave a dump of. Unset, it does nothing and passes. Select it with CFD_TEST_ONLY.
        DesktopChecks.Check("ExitLine_ForcedHeapCorruption_Probe", () =>
        {
            if (Environment.GetEnvironmentVariable("CFD_FORCE_HEAP_CORRUPTION") != "1") return;
            nint block = System.Runtime.InteropServices.Marshal.AllocHGlobal(16);
            for (int offset = 16; offset < 80; offset += 8) System.Runtime.InteropServices.Marshal.WriteInt64(block, offset, -1L);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(block);
        });
    }

    // PVH: a real child that sleeps past a 3 s limit; the parent's TIMEOUT line must name the check it was running.
    // Ring: readiness (a child process start and the 3 s limit). Cost: about 4 s. The sidecar write itself costs under 0.1 ms a check.
    public static void RunReadiness()
    {
        DesktopChecks.Check("Timeout_SleepingChild_NamesItsInFlightCheck", () =>
        {
            var (lines, exit, _) = DesktopChecks.RunBufferedWithin("--inflight-probe", 3);
            string timeout = lines.Select(l => l.Text).FirstOrDefault(t => t.StartsWith("FAIL TIMEOUT --inflight-probe", StringComparison.Ordinal)) ?? "(no TIMEOUT line)";
            if (exit != 124 || !timeout.EndsWith("; in-flight check: InFlightProbe_SleepsPastTheTimeout", StringComparison.Ordinal))
                throw new Exception($"exit {exit}: {timeout}");
        });

        // DRAIN-ON-ARM64-LAPTOP: a real child whose grandchild outlives it holding the inherited standard pipes. The parent's DRAIN
        // line must name that grandchild as the holder (on Windows matched by the pipe's index number through the grandchild's own
        // handle, read-only), and the DRAIN-EOF line must time, per stream, when end of file finally arrived.
        // Ring: readiness (a child, a 5 s grandchild, a 1 s drain wait, then the EOF wait). Cost: about 5 s (measured 5.1-5.2 s).
        DesktopChecks.Check("Drain_OrphanGrandchild_IsNamedAsThePipeHolder", () =>
        {
            var (lines, exit, _) = DesktopChecks.RunBufferedWithin("--drain-probe", 30, drainSeconds: 1);
            string[] texts = lines.Select(l => l.Text).ToArray();
            string probe = texts.FirstOrDefault(t => t.StartsWith("DRAIN-PROBE grandchild pid=", StringComparison.Ordinal)) ?? "(no DRAIN-PROBE line)";
            string drain = texts.FirstOrDefault(t => t.StartsWith("FAIL DRAIN --drain-probe", StringComparison.Ordinal)) ?? "(no DRAIN line)";
            string eof = texts.FirstOrDefault(t => t.StartsWith("DRAIN-EOF --drain-probe", StringComparison.Ordinal)) ?? "(no DRAIN-EOF line)";
            string pid = probe.Split('=')[^1];
            Console.WriteLine($"DRAIN-PROBE rendered: {drain} | {eof}");   // the rendered surface, in every readiness log
            string holder = OperatingSystem.IsWindows() ? $"pid {pid} " : "holder scan: not available on ";
            string proof = OperatingSystem.IsWindows() ? "a descendant of the child) holds stdout, stderr (verified by pipe id)" : "";
            if (exit != 125 || !drain.Contains(holder, StringComparison.Ordinal) || !drain.Contains(proof, StringComparison.Ordinal) ||
                !eof.StartsWith("DRAIN-EOF --drain-probe: stdout end of file arrived ", StringComparison.Ordinal) ||
                !eof.Contains("; stderr end of file arrived ", StringComparison.Ordinal))
                throw new Exception($"exit {exit}: {probe} | {drain} | {eof}");
        });
    }
}
