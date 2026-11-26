using System.Diagnostics;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core;
using CfdWorkbench.Core.SolverSetup;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// M1-M8 Product Launcher security tests (Ruling 68, docs/notes/solver-security-right-size.md):
/// M1 controlDict, M2 live master Disallowing banner, M3 argv-only, M4 real cases/tools/foam-dict-lint.py oracle
/// over hostile foreign-design fixture, M5 exclusive run dir + symlink refusal/safe-unlink, M6 clean child env,
/// M7 Stop-to-kill escalation, and M8 install-identity hash check (DR-SEC-1 A).
/// </summary>
internal static class SolverLauncherSecurityTests
{
    internal static void Run()
    {
        Check("SolverLauncher_M1M5_ExclusiveRunDir_WritesControlDictAndRefusesTraversal", M1M5_ExclusiveRunDir);
        Check("SolverLauncher_M5_SymlinkInsideRunDir_RemovedAsLinkTargetUntouched", M5_SymlinkSafeCleanup);
        Check("SolverLauncher_M4_HostileForeignDesign_PassesRealFoamDictLintOracle", M4_RealFoamDictLintOracle);
        Check("SolverLauncher_M2M3M6M7_BannerEnforced_CleanEnv_StopEscalation", M2M3M6M7_LauncherRuntime);
        Check("SolverLauncher_WindowsWsl_CleanEnvSystemRootAndLinuxHome_M1M6", WindowsWslCleanEnvAndSmoke);
        Check("SolverLauncher_M8_InstallIdentityHashCheck_DrSec1A", M8_InstallIdentityCheck);
    }

    private static void M1M5_ExclusiveRunDir()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-slv-m5-" + Guid.NewGuid().ToString("N"));
        try
        {
            var launcher = new SolverLauncher(root);

            // Traversal / separator in run key refused.
            AssertContractError("SETUP-RUN-KEY", () => launcher.PrepareExclusiveRunDirectory("../escape"));
            AssertContractError("SETUP-RUN-KEY", () => launcher.PrepareExclusiveRunDirectory("a/b"));
            AssertContractError("SETUP-RUN-KEY", () => launcher.PrepareExclusiveRunDirectory("a\\b"));

            var prepared = launcher.PrepareExclusiveRunDirectory("smoke-run-01");
            Equal(true, Directory.Exists(prepared.RunDirectory));
            Equal(true, File.Exists(prepared.ControlDictPath));

            string ctrl = ReadTextShared(prepared.ControlDictPath);
            Equal(true, ctrl.Contains("allowSystemOperations 0;", StringComparison.Ordinal));
            Equal(true, ctrl.Contains("stopAtWriteNowSignal 30;", StringComparison.Ordinal));

            // Pre-existing run directory refused exclusively (M5).
            AssertContractError("SETUP-RUN-EXISTS", () => launcher.PrepareExclusiveRunDirectory("smoke-run-01"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void M5_SymlinkSafeCleanup()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-slv-sym-" + Guid.NewGuid().ToString("N"));
        string outside = Path.Combine(Path.GetTempPath(), "cfdw-slv-outside-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(outside);
            string targetDir = Path.Combine(outside, "precious-folder");
            Directory.CreateDirectory(targetDir);
            string preciousFile = Path.Combine(targetDir, "keep.txt");
            File.WriteAllText(preciousFile, "DO-NOT-DELETE");

            var launcher = new SolverLauncher(root);
            var prepared = launcher.PrepareExclusiveRunDirectory("sym-run-01");

            // Create a directory junction (works without elevation on Windows) or symlink on Unix inside the run dir.
            string linkInside = Path.Combine(prepared.RunDirectory, "linked-outside");
            CreateDirectoryLink(linkInside, targetDir);
            Equal(true, SolverLauncher.IsReparseOrLink(linkInside));

            // SafeDeleteRunDirectory removes the link itself and leaves the external target intact!
            launcher.SafeDeleteRunDirectory("sym-run-01");
            Equal(false, Directory.Exists(prepared.RunDirectory));
            Equal(true, File.Exists(preciousFile));
            Equal("DO-NOT-DELETE", ReadTextShared(preciousFile));

            // Also verify PrepareExclusiveRunDirectory refuses if runs/ itself is a link!
            string linkRoot = Path.Combine(Path.GetTempPath(), "cfdw-slv-linkroot-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(linkRoot);
                CreateDirectoryLink(Path.Combine(linkRoot, "runs"), targetDir);
                var linkLauncher = new SolverLauncher(linkRoot);
                AssertContractError("SETUP-SYMLINK-REFUSED", () => linkLauncher.PrepareExclusiveRunDirectory("run-02"));
            }
            finally
            {
                string linkRuns = Path.Combine(linkRoot, "runs");
                if (Directory.Exists(linkRuns)) Directory.Delete(linkRuns, recursive: false);
                if (Directory.Exists(linkRoot)) Directory.Delete(linkRoot, recursive: true);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(outside)) Directory.Delete(outside, recursive: true);
        }
    }

    private static void M4_RealFoamDictLintOracle()
    {
        string lintScript = Path.Combine(StripFixtureTests.RepoRoot(), "cases", "tools", "foam-dict-lint.py");
        Equal(true, File.Exists(lintScript), "cases/tools/foam-dict-lint.py must exist");

        string root = Path.Combine(Path.GetTempPath(), "cfdw-slv-m4-" + Guid.NewGuid().ToString("N"));
        string hostileRawDir = Path.Combine(Path.GetTempPath(), "cfdw-slv-m4-bad-" + Guid.NewGuid().ToString("N"));
        try
        {
            var launcher = new SolverLauncher(root);
            var prepared = launcher.PrepareExclusiveRunDirectory("m4-hostile-01");

            // Hostile foreign design fixture with #codeStream, #include, quoted regex, and comma decimal attempt.
            const string hostileDesignName = "wing; #codeStream { code #{ system(\"id\"); #}; } #include \"/etc/passwd\" 1,5";
            var emitted = launcher.EmitSmokeCase(prepared.RunDirectory, "m4-hostile-01", hostileDesignName);

            // Assert no emitted file name equals any OpenFoamApp binary name (M4).
            var appNames = Enum.GetValues<OpenFoamApp>()
                .Select(SolverLauncherPolicy.BinaryName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string relPath in emitted)
                Equal(false, appNames.Contains(Path.GetFileName(relPath)), "emitted file equals app name: " + relPath);

            // Non-finite numbers must be refused before emission (M4).
            AssertContractError("SETUP-NONFINITE", () => SolverLauncherPolicy.EmitCavityCaseFiles("m4-nan", endTime: double.NaN));
            AssertContractError("SETUP-NONFINITE", () => SolverLauncherPolicy.EmitCavityCaseFiles("m4-inf", deltaT: double.PositiveInfinity));

            // Run the REAL cases/tools/foam-dict-lint.py oracle on the emitted case directory -> MUST exit 0!
            var (cleanExit, cleanOut) = RunPython(lintScript, prepared.RunDirectory);
            Equal(0, cleanExit, "foam-dict-lint.py on app-emitted case failed: " + cleanOut);
            Equal(true, cleanOut.Contains("lint: clean", StringComparison.Ordinal), "expected lint: clean, got: " + cleanOut);

            // Negative control: raw hostile file with #codeStream MUST exit non-zero (3) and print REFUSED!
            Directory.CreateDirectory(Path.Combine(hostileRawDir, "system"));
            File.WriteAllText(Path.Combine(hostileRawDir, "cfdw-manifest.json"), "[\"system/controlDict\"]");
            File.WriteAllText(Path.Combine(hostileRawDir, "system", "controlDict"),
                "FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\n" +
                "runTimeModifiable false;\n" +
                "#codeStream { code #{ system(\"id\"); #}; };\n");
            var (badExit, badOut) = RunPython(lintScript, hostileRawDir);
            Equal(3, badExit, "foam-dict-lint.py failed to refuse raw hostile #codeStream: " + badOut);
            Equal(true, badOut.Contains("lint: REFUSED", StringComparison.Ordinal), "expected lint: REFUSED: " + badOut);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(hostileRawDir)) Directory.Delete(hostileRawDir, recursive: true);
        }
    }

    private static void M2M3M6M7_LauncherRuntime()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-slv-m2-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1. Live Allowing banner (even if Disallowing also appears in noise) -> stopped/killed and returns smoke.banner failure!
            var allowingRunner = new FakeProcessRunner(
                exitCode: 0,
                stdout: "Build  : _87ed40d256-20251219\nNote: expected Disallowing\nAllowing user-supplied system call operations\n",
                stderr: "");
            var launcherAllowing = new SolverLauncher(root, allowingRunner);
            var reqAllow = new SolverLaunchRequest(
                "m2-allowing", SetupOs.MacOs, "/Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam",
                OpenFoamApp.IcoFoam, "_87ed40d256-20251219", AllowUnverifiedBuild: false);
            var resAllow = launcherAllowing.LaunchAsync(reqAllow, CancellationToken.None).GetAwaiter().GetResult();
            Equal(false, resAllow.Succeeded);
            Equal("smoke.banner", resAllow.CauseCode);

            // 2. Clean environment + argv-only verified on captured ProcessRequest (M3, M6).
            Equal(false, allowingRunner.LastRequest!.Arguments.Contains("-c"));
            Equal(5, allowingRunner.LastRequest!.Environment.Count);
            Equal("C", allowingRunner.LastRequest!.Environment["LANG"]);
            Equal(false, allowingRunner.LastRequest!.Environment.ContainsKey("FOAM_CONTROLDICT"));
            Equal(false, allowingRunner.LastRequest!.Environment.ContainsKey("WM_PROJECT_SITE"));

            // 3. Passing Disallowing banner + pinned build + Courant mean -> Succeeded!
            var okRunner = new FakeProcessRunner(
                exitCode: 0,
                stdout: "Build  : _87ed40d256-20251219\nDisallowing user-supplied system call operations\nCourant Number mean: 0.222158 max: 0.852134\n",
                stderr: "");
            var launcherOk = new SolverLauncher(root, okRunner);
            var resOk = launcherOk.LaunchAsync(reqAllow with { RunKey = "m2-ok" }, CancellationToken.None).GetAwaiter().GetResult();
            Equal(true, resOk.Succeeded);
            Equal("Disallowing", resOk.BannerStatus);
            Equal(0.222158, resOk.CourantMean);

            // 4. M7 Stop-to-kill escalation on cancellation.
            var hangingRunner = new FakeProcessRunner(exitCode: -1, stdout: "", stderr: "", hangUntilCancelled: true);
            var launcherHang = new SolverLauncher(root, hangingRunner);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var resCancel = launcherHang.LaunchAsync(reqAllow with { RunKey = "m7-cancel" }, cts.Token).GetAwaiter().GetResult();
            Equal(false, resCancel.Succeeded);
            Equal(true, hangingRunner.KillTreeCalled);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void WindowsWslCleanEnvAndSmoke()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-slv-wsl-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1. Deterministic RunOpenFoamSmokeAsync contract + ALL 4 failure branches (runs on Windows AND macOS CI!).
            SetupOs hostOs = OperatingSystem.IsWindows() ? SetupOs.Windows : SetupOs.MacOs;
            string activation = OperatingSystem.IsWindows()
                ? "cfdw-openfoam2512-arm64"
                : "/Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam";
            const string buildId = "_bd2b6720-20260127";

            var recorded = new List<ProcessRunRequest>();
            var okRunner = new ScriptedRunner(req =>
            {
                recorded.Add(req);
                string app = req.Arguments[^3]; // ... <app> -case <caseArg>
                if (app == "blockMesh")
                {
                    return new ProcessRunResult(0,
                        $"Build  : {buildId} OPENFOAM=2512 version=2512\n" +
                        "allowSystemOperations : Disallowing user-supplied system call operations\n", "", false, false, 10);
                }
                if (app == "checkMesh")
                {
                    return new ProcessRunResult(0,
                        $"Build  : {buildId} OPENFOAM=2512 version=2512\n" +
                        "allowSystemOperations : Disallowing user-supplied system call operations\nMesh OK.\n", "", false, false, 10);
                }
                Directory.CreateDirectory(Path.Combine(req.WorkingDirectory, "0.5"));
                return new ProcessRunResult(0,
                    $"Build  : {buildId} OPENFOAM=2512 version=2512\n" +
                    "allowSystemOperations : Disallowing user-supplied system call operations\n" +
                    "Time = 0.5\nCourant Number mean: 0.222158 max: 0.852134\nEnd\n", "", false, false, 15);
            });

            var launcher = new SolverLauncher(root, okRunner);
            var smoke = launcher.RunOpenFoamSmokeAsync(
                hostOs, activation, "smoke-ok", buildId, cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
            Equal(true, smoke.Succeeded, smoke.Reason);
            Equal("Disallowing", smoke.BannerStatus);
            Equal(buildId, smoke.BuildId);
            Equal(0.222158, smoke.CourantMean);
            Equal(3, recorded.Count);

            if (OperatingSystem.IsWindows())
            {
                foreach (var req in recorded)
                {
                    Equal("wsl.exe", req.Executable);
                    Equal(false, req.Arguments.Contains("-c"));
                    Equal(true, req.Arguments.Contains("/usr/bin/env"));
                    Equal(true, req.Arguments.Contains("-i"));
                    Equal(true, req.Arguments.Any(a => a.StartsWith("HOME=/mnt/", StringComparison.Ordinal)));
                    Equal(true, req.Arguments.Contains("/usr/bin/openfoam2512"));
                    Equal(true, req.Environment.TryGetValue("SystemRoot", out string? sr) && !string.IsNullOrWhiteSpace(sr));
                    Equal(false, req.Environment.ContainsKey("FOAM_CONTROLDICT"));
                    Equal(false, req.Environment.ContainsKey("WM_PROJECT_SITE"));
                }
            }

            // Negative branch (a): blockMesh exits non-zero -> stops immediately at step 1.
            int bmCalls = 0;
            var bmFailLauncher = new SolverLauncher(root, new ScriptedRunner(_ =>
            {
                bmCalls++;
                return new ProcessRunResult(1,
                    $"Build  : {buildId}\nallowSystemOperations : Disallowing\n", "mesh error", false, false, 5);
            }));
            var bmFail = bmFailLauncher.RunOpenFoamSmokeAsync(hostOs, activation, "smoke-bm-fail", buildId).GetAwaiter().GetResult();
            Equal(false, bmFail.Succeeded);
            Equal(1, bmCalls);

            // Negative branch (b): checkMesh exits 0 without "Mesh OK" -> smoke.no-outputs, never launches icoFoam.
            int cmCalls = 0;
            var cmFailLauncher = new SolverLauncher(root, new ScriptedRunner(req =>
            {
                cmCalls++;
                return new ProcessRunResult(0,
                    $"Build  : {buildId}\nallowSystemOperations : Disallowing\nFailed 4 checks.\n", "", false, false, 5);
            }));
            var cmFail = cmFailLauncher.RunOpenFoamSmokeAsync(hostOs, activation, "smoke-cm-fail", buildId).GetAwaiter().GetResult();
            Equal(false, cmFail.Succeeded);
            Equal("smoke.no-outputs", cmFail.CauseCode);
            Equal(2, cmCalls);

            // Negative branch (c): icoFoam prints Allowing banner -> smoke.banner.
            var allowLauncher = new SolverLauncher(root, new ScriptedRunner(req =>
            {
                string app = req.Arguments[^3];
                if (app == "icoFoam")
                {
                    return new ProcessRunResult(0,
                        $"Build  : {buildId}\nallowSystemOperations : Allowing user-supplied system call operations\n", "", false, false, 5);
                }
                return new ProcessRunResult(0,
                    $"Build  : {buildId}\nallowSystemOperations : Disallowing\nMesh OK.\n", "", false, false, 5);
            }));
            var allowFail = allowLauncher.RunOpenFoamSmokeAsync(hostOs, activation, "smoke-allow-fail", buildId).GetAwaiter().GetResult();
            Equal(false, allowFail.Succeeded);
            Equal("smoke.banner", allowFail.CauseCode);

            // Negative branch (d): icoFoam exits 0 with Disallowing + Courant, but missing 0.5/ output dir -> smoke.no-outputs.
            var missingDirLauncher = new SolverLauncher(root, new ScriptedRunner(req =>
                new ProcessRunResult(0,
                    $"Build  : {buildId}\nallowSystemOperations : Disallowing\nMesh OK.\nCourant Number mean: 0.222158\n", "", false, false, 5)));
            var missingDirFail = missingDirLauncher.RunOpenFoamSmokeAsync(hostOs, activation, "smoke-no-05", buildId).GetAwaiter().GetResult();
            Equal(false, missingDirFail.Succeeded);
            Equal("smoke.no-outputs", missingDirFail.CauseCode);

            // 2. Deterministic LocalCfdRunSubstrate.ExecuteStepAsync check for Windows WSL-CLEAN-ENV-M1 (runs on all OSes!).
            ProcessRunRequest? capturedSubReq = null;
            var subRunner = new ScriptedRunner(req =>
            {
                capturedSubReq = req;
                return new ProcessRunResult(0, "", "", false, false, 5);
            });
            var substrate = new CfdWorkbench.Analysis.CfdRun.LocalCfdRunSubstrate(
                root, subRunner, hostOs: SetupOs.Windows, activationPathOrDistro: "cfdw-openfoam2512-arm64");
            substrate.ExecuteStepAsync(
                new CfdWorkbench.Analysis.CfdRun.SubstrateStepRequest(
                    "run-wsl-sub-01", @"D:\runs\k", @"D:\runs\k\.home",
                    CfdWorkbench.Core.CfdRun.CfdBackendKind.OpenFoamEsi,
                    CfdWorkbench.Analysis.CfdRun.SubstrateStepKind.Solve),
                CancellationToken.None).GetAwaiter().GetResult();
            Equal(true, capturedSubReq is not null);
            Equal("wsl.exe", capturedSubReq!.Executable);
            Equal(false, capturedSubReq.Arguments.Contains("-c"));
            Equal(true, capturedSubReq.Arguments.Contains("/usr/bin/env"));
            Equal(true, capturedSubReq.Arguments.Contains("-i"));
            Equal(true, capturedSubReq.Arguments.Contains("HOME=/mnt/d/runs/k/.home"));
            Equal(true, capturedSubReq.Arguments.Contains("/usr/bin/openfoam2512"));
            Equal(true, capturedSubReq.Arguments.Contains("simpleFoam"));
            Equal(true, capturedSubReq.Arguments.Contains("-case"));
            Equal(true, capturedSubReq.Arguments.Contains("/mnt/d/runs/k"));
            Equal(true, capturedSubReq.Environment.TryGetValue("SystemRoot", out string? subSysRoot) && !string.IsNullOrWhiteSpace(subSysRoot));
            Equal(false, capturedSubReq.Environment.ContainsKey("FOAM_CONTROLDICT"));
            Equal(false, capturedSubReq.Environment.ContainsKey("WM_PROJECT_SITE"));

            // 3. Opt-in real host execution against installed cfdw-openfoam2512-arm64 (never starts WSL in default CI).
            if (OperatingSystem.IsWindows() &&
                string.Equals(Environment.GetEnvironmentVariable("CFDW_LIVE_WSL_SMOKE"), "1", StringComparison.Ordinal))
            {
                var liveLauncher = new SolverLauncher(root);
                var live = liveLauncher.RunOpenFoamSmokeAsync(
                    SetupOs.Windows, "cfdw-openfoam2512-arm64", "wsl-smoke-live", buildId,
                    cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                Equal(true, live.Succeeded, $"Live WSL smoke failed: {live.CauseCode} {live.Reason}\n{live.OutputExcerptRedacted}");
                Equal("Disallowing", live.BannerStatus);
                Equal(buildId, live.BuildId);
                Equal(0.222158, live.CourantMean);
                Console.WriteLine($"LIVE_WSL_SMOKE_PASS build={live.BuildId} banner={live.BannerStatus} courant={live.CourantMean} duration_ms={live.DurationMs}");
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class ScriptedRunner(Func<ProcessRunRequest, ProcessRunResult> handler) : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    private static void M8_InstallIdentityCheck()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        var matchingHashes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["openfoam-app-dmg"] = "5eb2ab10af3e90b7e284276b7046632b9c9bc14885c67d6664bd2628eb393439",
            ["etc-openfoam"] = "fa900c4e2cf4c57b0e7c0df4fa167a607435e83aa1675101cf0bffc87502572a",
            ["volume"] = "a1d4e57b2931a32501a39cff6fc7ab509f4306c12a8c5f6dd09f7f051196da28"
        };
        var verified = SolverLauncher.VerifyInstallIdentity(route, matchingHashes);
        Equal(true, verified.Verified);
        Equal(64, verified.CombinedIdentitySha256.Length);

        var mismatched = new Dictionary<string, string>(matchingHashes)
        {
            ["openfoam-app-dmg"] = "0000000000000000000000000000000000000000000000000000000000000000"
        };
        var unverified = SolverLauncher.VerifyInstallIdentity(route, mismatched);
        Equal(false, unverified.Verified);
        Equal(true, unverified.RequiresUnverifiedDisclosure);
        Equal(false, unverified.CombinedIdentitySha256 == verified.CombinedIdentitySha256);
    }

    private static void AssertContractError(string expectedCode, Action action)
    {
        try { action(); }
        catch (ContractError ex) { Equal(expectedCode, ex.Code); return; }
        throw new InvalidOperationException("Expected ContractError " + expectedCode);
    }

    private static string ReadTextShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }

    private static void CreateDirectoryLink(string linkPath, string targetDir)
    {
        Equal(true, TestLinks.TryDirectoryLink(linkPath, targetDir), "directory link creation failed");
    }

    private static (int ExitCode, string Output) RunPython(string scriptPath, string targetDir)
    {
        string exe = OperatingSystem.IsWindows() ? "py" : "python3";
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (OperatingSystem.IsWindows()) psi.ArgumentList.Add("-3");
        psi.ArgumentList.Add(scriptPath);
        psi.ArgumentList.Add(targetDir);
        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(15_000);
        return (proc.ExitCode, stdout + stderr);
    }

    internal sealed class FakeProcessRunner(int exitCode, string stdout, string stderr, bool hangUntilCancelled = false) : IProcessRunner
    {
        public ProcessRunRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }
        public bool KillTreeCalled { get; private set; }

        public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            if (hangUntilCancelled)
            {
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                catch (OperationCanceledException)
                {
                    KillTreeCalled = true;
                    return new ProcessRunResult(-1, stdout, stderr, Cancelled: true, TimedOut: false, DurationMs: 50);
                }
            }
            return new ProcessRunResult(exitCode, stdout, stderr, Cancelled: false, TimedOut: false, DurationMs: 12);
        }
    }
}

/// <summary>Analysis test link helper (TEST-FOREIGN-OS-BRANCH): junction on Windows, real symlink on macOS/Linux.</summary>
internal static class TestLinks
{
    internal static bool TryDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return true;
        }
        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string arg in new[] { "/c", "mklink", "/J", link, target }) psi.ArgumentList.Add(arg);
        using var proc = Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(5000);
        return proc.ExitCode == 0 && Directory.Exists(link);
    }
}
