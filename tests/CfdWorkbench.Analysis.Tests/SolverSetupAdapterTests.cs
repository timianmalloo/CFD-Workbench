using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core.SolverSetup;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// T2/T5 Setup Adapter tests:
/// Real read-only Windows ARM64 survey (< 5 s, UTF-16 wsl.exe parsing, zero distro mutation),
/// loopback HttpListener download + SHA-256 verification (good, truncated, hash-mismatch deletes .part),
/// ROUTE-PARENT-MISSING (Ruling 133) negative-path tests, IRunOnceStore fake (DR-SETUP-6),
/// and macOS fixture adapters (sw_vers/sysctl, zero xattr/Homebrew).
/// </summary>
internal static class SolverSetupAdapterTests
{
    internal static void Run()
    {
        Check("SolverSetup_WindowsLiveSurvey_ReadOnlyUnderFiveSeconds", LiveWindowsSurveyReadOnly);
        Check("SolverSetup_WslParser_HandlesUtf16LeAndUtf8", WslParserUtf16AndUtf8);
        Check("SolverSetup_DownloadAndVerify_LoopbackHttpListener", LoopbackDownloadAndVerify);
        Check("SolverSetup_RouteParentMissing_CreatesOrRefusesBeforeCommand", RouteParentMissingRuling133);
        Check("SolverSetup_RunOnceStore_InjectedAbstractionNeverTouchesHkcu", RunOnceInjectedAbstraction);
        Check("SolverSetup_MacAdapters_FixtureSurveyHashAndQuarantineRefusal", MacFixtureAdapters);
    }

    private static void LiveWindowsSurveyReadOnly()
    {
        if (!OperatingSystem.IsWindows())
        {
            // On macOS/Linux verify the macOS fixture probe instead.
            var macProbe = new MacHostSurveyProbe(
                runSwVers: () => "ProductName:\t\tmacOS\nProductVersion:\t\t15.2\nBuildVersion:\t\t24C101\n",
                runSysctlArm64: () => "hw.optional.arm64: 1\n",
                freeDiskProvider: () => 500_000_000_000L,
                appSearchRoots: []);
            var macSurvey = macProbe.SurveyAsync(CancellationToken.None).GetAwaiter().GetResult();
            Equal(SetupOs.MacOs, macSurvey.Os);
            Equal(SetupArch.Arm64, macSurvey.Arch);
            Equal(15, macSurvey.OsBuild);
            return;
        }

        // Real read-only survey on this Windows 11 ARM64 host.
        var probe = new WindowsHostSurveyProbe();
        var first = probe.SurveyAsync(CancellationToken.None).GetAwaiter().GetResult();
        Equal(SetupOs.Windows, first.Os);
        Equal(true, first.OsBuild >= 19041, "expected Windows build >= 19041, got " + first.OsBuild);
        Equal(true, first.DurationMs < 5000, "survey exceeded 5 s budget: " + first.DurationMs + " ms");
        Equal(true, first.FreeDiskBytes is > 0, "expected positive free disk bytes");

        // Verify read-only invariant: a second survey observes identical WSL distro states (never started/mutated).
        var second = probe.SurveyAsync(CancellationToken.None).GetAwaiter().GetResult();
        Equal(first.WslDistros.Count, second.WslDistros.Count);
        for (int i = 0; i < first.WslDistros.Count; i++)
        {
            Equal(first.WslDistros[i].Name, second.WslDistros[i].Name);
            Equal(first.WslDistros[i].State, second.WslDistros[i].State);
            Equal(first.WslDistros[i].Version, second.WslDistros[i].Version);
        }
    }

    private static void WslParserUtf16AndUtf8()
    {
        const string sample =
            "  NAME                        STATE           VERSION\r\n" +
            "* Ubuntu-24.04                Stopped         2\r\n" +
            "  cfdw-openfoam2512-arm64     Stopped         2\r\n";

        byte[] utf16LeNoBom = Encoding.Unicode.GetBytes(sample);
        byte[] utf16LeBom = [0xFF, 0xFE, .. utf16LeNoBom];
        byte[] utf8 = Encoding.UTF8.GetBytes(sample);

        foreach (byte[] raw in new[] { utf16LeNoBom, utf16LeBom, utf8 })
        {
            string decoded = HostSurveyParsers.DecodeWslOutput(raw);
            var distros = HostSurveyParsers.ParseWslListVerbose(decoded);
            Equal(2, distros.Count);
            Equal("Ubuntu-24.04", distros[0].Name);
            Equal("Stopped", distros[0].State);
            Equal(2, distros[0].Version);
            Equal(true, distros[0].IsDefault);
            Equal("cfdw-openfoam2512-arm64", distros[1].Name);
            Equal("Stopped", distros[1].State);
            Equal(2, distros[1].Version);
            Equal(false, distros[1].IsDefault);
        }

        string statusText = "Default Distribution: Ubuntu-24.04\r\nDefault Version: 2\r\n";
        var (available, version) = HostSurveyParsers.ParseWslStatus(0, statusText);
        Equal(true, available);
        Equal(2, version);
    }

    private static void LoopbackDownloadAndVerify()
    {
        byte[] payload = Encoding.UTF8.GetBytes("CFD-Workbench-Pinned-Solver-Artifact-Payload-0123456789");
        string goodSha = Convert.ToHexStringLower(SHA256.HashData(payload));

        int port = FindFreePort();
        string prefix = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        using var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch { break; }
                string path = ctx.Request.Url?.AbsolutePath ?? "/";
                if (path == "/good")
                {
                    ctx.Response.ContentLength64 = payload.Length;
                    await ctx.Response.OutputStream.WriteAsync(payload);
                    ctx.Response.OutputStream.Close();
                }
                else if (path == "/truncated")
                {
                    // Advertise full length but send only half and abort.
                    ctx.Response.ContentLength64 = payload.Length;
                    await ctx.Response.OutputStream.WriteAsync(payload.AsMemory(0, payload.Length / 2));
                    ctx.Response.Abort();
                }
                else if (path == "/wrong")
                {
                    // Exact same length as payload so ONLY SHA-256 hashMismatch catches it (not sizeMismatch).
                    byte[] wrong = (byte[])payload.Clone();
                    wrong[0] ^= 0xFF;
                    ctx.Response.ContentLength64 = wrong.Length;
                    await ctx.Response.OutputStream.WriteAsync(wrong);
                    ctx.Response.OutputStream.Close();
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
            }
        });

        string dir = Path.Combine(Path.GetTempPath(), "cfdw-dl-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1. Good download matches SHA-256 and atomically renames .part -> target.
            string goodDest = Path.Combine(dir, "sub", "artifact.bin");
            var goodRes = WindowsSetupAdapters.DownloadAndVerifyAsync(
                new Uri(prefix + "good"), goodDest, goodSha, payload.Length, null, CancellationToken.None)
                .GetAwaiter().GetResult();
            Equal(true, goodRes.Succeeded);
            Equal(true, File.Exists(goodDest));
            Equal(false, File.Exists(goodDest + ".part"));
            Equal(payload.Length, goodRes.BytesDownloaded);

            // 2. Wrong hash deletes .part and returns download.hash-mismatch!
            string wrongDest = Path.Combine(dir, "wrong.bin");
            var wrongRes = WindowsSetupAdapters.DownloadAndVerifyAsync(
                new Uri(prefix + "wrong"), wrongDest, goodSha, payload.Length, null, CancellationToken.None)
                .GetAwaiter().GetResult();
            Equal(false, wrongRes.Succeeded);
            Equal("download.hash-mismatch", wrongRes.CauseCode);
            Equal(false, File.Exists(wrongDest));
            Equal(false, File.Exists(wrongDest + ".part"));

            // 3. Truncated download deletes .part and fails cleanly (never leaves partial target).
            string truncDest = Path.Combine(dir, "trunc.bin");
            var truncRes = WindowsSetupAdapters.DownloadAndVerifyAsync(
                new Uri(prefix + "truncated"), truncDest, goodSha, payload.Length, null, CancellationToken.None)
                .GetAwaiter().GetResult();
            Equal(false, truncRes.Succeeded);
            Equal(false, File.Exists(truncDest));
            Equal(false, File.Exists(truncDest + ".part"));
        }
        finally
        {
            cts.Cancel();
            listener.Stop();
            try { serverTask.Wait(1000); } catch { }
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    private static void RouteParentMissingRuling133()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-r133-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1. Parent absent -> adapter creates parent, verifies writable, THEN calls wsl.exe --import.
            string missingParentDest = Path.Combine(root, "new-parent", "wsl", "cfdw-openfoam2512");
            string imageFile = Path.Combine(root, "ubuntu.wsl");
            Directory.CreateDirectory(root);
            File.WriteAllText(imageFile, "stub-image");

            var fakeRunner = new SolverLauncherSecurityTests.FakeProcessRunner(0, "Import complete", "");
            var ok = WindowsSetupAdapters.RunWslImportStepAsync(
                "cfdw-openfoam2512", missingParentDest, imageFile, fakeRunner, CancellationToken.None)
                .GetAwaiter().GetResult();
            Equal(true, ok.Succeeded);
            Equal(true, Directory.Exists(Path.GetDirectoryName(missingParentDest)!));
            Equal(1, fakeRunner.CallCount);
            Equal("wsl.exe", fakeRunner.LastRequest!.Executable);
            Equal(true, fakeRunner.LastRequest!.Environment.TryGetValue("SystemRoot", out string? sysRoot) &&
                !string.IsNullOrWhiteSpace(sysRoot));

            // 2. Parent cannot be created (a regular file blocks the parent directory path) -> refuses cleanly
            // with route.parent-missing and NEVER invokes wsl.exe (CallCount == 0)!
            string blockingFile = Path.Combine(root, "not-a-directory");
            File.WriteAllText(blockingFile, "blocker");
            string impossibleDest = Path.Combine(blockingFile, "wsl", "cfdw-openfoam2512");

            var neverCalledRunner = new SolverLauncherSecurityTests.FakeProcessRunner(0, "", "");
            var refused = WindowsSetupAdapters.RunWslImportStepAsync(
                "cfdw-openfoam2512", impossibleDest, imageFile, neverCalledRunner, CancellationToken.None)
                .GetAwaiter().GetResult();
            Equal(false, refused.Succeeded);
            Equal("route.parent-missing", refused.CauseCode);
            Equal(0, neverCalledRunner.CallCount);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void RunOnceInjectedAbstraction()
    {
        IRunOnceStore store = new InMemoryRunOnceStore();
        Equal((string?)null, store.TryRead("CFDWorkbenchSetupResume"));
        store.RegisterResume("CFDWorkbenchSetupResume", "\"C:\\Program Files\\CFD Workbench\\CfdWorkbench.exe\" --resume-solver-setup");
        Equal(true, store.TryRead("CFDWorkbenchSetupResume")!.Contains("--resume-solver-setup", StringComparison.Ordinal));
        store.Remove("CFDWorkbenchSetupResume");
        Equal((string?)null, store.TryRead("CFDWorkbenchSetupResume"));
    }

    private static void MacFixtureAdapters()
    {
        // Fixture parsing: arm64 macOS 15.2, Intel macOS 15.2, old macOS 13.6.
        Equal(15, HostSurveyParsers.ParseMacMajorVersion("ProductName:\t\tmacOS\nProductVersion:\t\t15.2\n"));
        Equal(13, HostSurveyParsers.ParseMacMajorVersion("ProductName:\t\tmacOS\nProductVersion:\t\t13.6.1\n"));
        Equal(SetupArch.Arm64, HostSurveyParsers.ParseMacArch("hw.optional.arm64: 1\n"));
        Equal(SetupArch.X64, HostSurveyParsers.ParseMacArch("hw.optional.arm64: 0\n"));

        // Existing OpenFOAM.app hash verification (zero xattr, zero Homebrew).
        string appRoot = Path.Combine(Path.GetTempPath(), "cfdw-mac-" + Guid.NewGuid().ToString("N"), "OpenFOAM-v2512.app");
        try
        {
            string res = Path.Combine(appRoot, "Contents", "Resources");
            Directory.CreateDirectory(Path.Combine(res, "etc"));
            File.WriteAllText(Path.Combine(res, "OpenFOAM-v2512.dmg"), "fake-dmg-bytes");
            File.WriteAllText(Path.Combine(res, "etc", "openfoam"), "fake-etc-openfoam");
            File.WriteAllText(Path.Combine(res, "volume"), "fake-volume");

            var info = MacSetupAdapters.InspectExistingBundle(appRoot);
            Equal(true, info is not null);
            Equal(false, info!.MatchesPin); // fake bytes do not match pinned real hashes
            Equal(64, info.InstallIdentitySha256.Length);

            // Quarantine-stripping step is explicitly refused (M8 / DR-SETUP-3).
            var refused = MacSetupAdapters.RefuseQuarantineRemovalStep("xattr.strip");
            Equal(false, refused.Succeeded);
            Equal("mac.quarantine-removal-refused", refused.CauseCode);
        }
        finally
        {
            string parent = Path.GetDirectoryName(appRoot)!;
            if (Directory.Exists(parent)) Directory.Delete(parent, recursive: true);
        }
    }

    private static int FindFreePort()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        int port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }
}
