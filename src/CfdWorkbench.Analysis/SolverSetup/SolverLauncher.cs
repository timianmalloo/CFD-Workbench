using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using CfdWorkbench.Core;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.SolverSetup;

public sealed record ProcessRunRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    TimeSpan Timeout,
    TimeSpan KillGracePeriod);

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool Cancelled,
    bool TimedOut,
    long DurationMs);

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Headless argv-only process runner (M3, M6, M7). Never uses shell execution or new console windows.
/// Escalates cancellation/timeout to process-tree kill after the grace period.
/// </summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(request.Executable)
        {
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string arg in request.Arguments)
            psi.ArgumentList.Add(arg);

        psi.Environment.Clear();
        foreach (var (key, value) in request.Environment)
            psi.Environment[key] = value;

        var sw = Stopwatch.StartNew();
        using var proc = new Process { StartInfo = psi };
        proc.Start();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = proc.StandardError.ReadToEndAsync(CancellationToken.None);

        using var timeoutCts = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        bool cancelled = false;
        bool timedOut = false;
        try
        {
            await proc.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
            cancelled = cancellationToken.IsCancellationRequested;
            TryEscalateKillTree(proc, request.KillGracePeriod);
        }

        string stdout = await SafeReadAsync(stdoutTask);
        string stderr = await SafeReadAsync(stderrTask);
        sw.Stop();

        int exitCode = proc.HasExited ? proc.ExitCode : -1;
        return new ProcessRunResult(exitCode, stdout, stderr, cancelled, timedOut, sw.ElapsedMilliseconds);
    }

    private static void TryEscalateKillTree(Process proc, TimeSpan grace)
    {
        try
        {
            if (proc.HasExited) return;
            if (grace > TimeSpan.Zero && proc.WaitForExit((int)Math.Min(grace.TotalMilliseconds, 2000)))
                return;
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process may have exited concurrently.
        }
    }

    private static async Task<string> SafeReadAsync(Task<string> readTask)
    {
        try { return await readTask.WaitAsync(TimeSpan.FromSeconds(2)); }
        catch { return ""; }
    }
}

public sealed record PreparedRunDirectory(string RunKey, string RunDirectory, string HomeDirectory, string ControlDictPath);

public sealed record SolverLaunchRequest(
    string RunKey,
    SetupOs Os,
    string ActivationPathOrDistro,
    OpenFoamApp App,
    string? ExpectedBuildId,
    bool AllowUnverifiedBuild = false,
    IReadOnlyList<string>? TypedArgs = null,
    TimeSpan? Timeout = null,
    long MinimumFreeDiskBytes = 100_000_000L);

public sealed record SolverLaunchResult(
    bool Succeeded,
    int ExitCode,
    string? BannerStatus,
    string? BuildId,
    double? CourantMean,
    string? CauseCode,
    string Reason,
    string OutputExcerptRedacted,
    long DurationMs);

public sealed record InstallIdentityVerification(
    bool Verified,
    bool RequiresUnverifiedDisclosure,
    string CombinedIdentitySha256,
    IReadOnlyList<string> MismatchedPinIds);

/// <summary>
/// Right-sized Product Launcher implementing M1-M8 (Ruling 68, docs/notes/solver-security-right-size.md §5).
/// </summary>
public sealed class SolverLauncher
{
    private readonly string appDataRoot;
    private readonly IProcessRunner runner;
    private readonly SemaphoreSlim singleJobGate = new(1, 1);

    public SolverLauncher(string appDataRoot, IProcessRunner? runner = null)
    {
        this.appDataRoot = appDataRoot;
        this.runner = runner ?? new SystemProcessRunner();
    }

    public string RunsRoot => Path.Combine(appDataRoot, "runs");

    /// <summary>True if the path is a symlink, junction, or reparse point (M5).</summary>
    public static bool IsReparseOrLink(string path)
    {
        if (File.Exists(path))
        {
            var fi = new FileInfo(path);
            return fi.LinkTarget is not null || (fi.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        if (Directory.Exists(path))
        {
            var di = new DirectoryInfo(path);
            return di.LinkTarget is not null || (di.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        return false;
    }

    /// <summary>
    /// M5 + M1: creates an exclusive run directory under <c>runs/&lt;runKey&gt;/</c>, refuses symlinks or pre-existing
    /// directories, and writes the app-owned HOME <c>.OpenFOAM/2512/controlDict</c> with <c>allowSystemOperations 0</c>.
    /// </summary>
    public PreparedRunDirectory PrepareExclusiveRunDirectory(string runKey)
    {
        SolverLauncherPolicy.ValidateRunKey(runKey);

        if (IsReparseOrLink(appDataRoot))
            throw new ContractError("SETUP-SYMLINK-REFUSED", "App data root must not be a symlink.");

        Directory.CreateDirectory(appDataRoot);

        if (IsReparseOrLink(RunsRoot))
            throw new ContractError("SETUP-SYMLINK-REFUSED", "Runs root must not be a symlink.");

        var parentCheck = WindowsSetupAdapters.EnsureOwnedParentDirectory(Path.Combine(RunsRoot, runKey));
        if (!parentCheck.Succeeded)
            throw new ContractError("ROUTE-PARENT-MISSING", parentCheck.Message);

        string runDir = Path.Combine(RunsRoot, runKey);
        if (IsReparseOrLink(runDir))
            throw new ContractError("SETUP-SYMLINK-REFUSED", "Run directory must not be a symlink.");
        if (Directory.Exists(runDir) || File.Exists(runDir))
            throw new ContractError("SETUP-RUN-EXISTS", $"Run directory '{runKey}' already exists.");

        Directory.CreateDirectory(runDir);
        string homeDir = Path.Combine(runDir, ".home");
        string ofDir = Path.Combine(homeDir, ".OpenFOAM", "2512");
        Directory.CreateDirectory(ofDir);

        string controlDictPath = Path.Combine(ofDir, "controlDict");
        WriteUtf8Exclusive(controlDictPath, SolverLauncherPolicy.EmitUserControlDict());
        return new PreparedRunDirectory(runKey, runDir, homeDir, controlDictPath);
    }

    /// <summary>M4: emits every file of the bundled cavity smoke case into <paramref name="runDirectory"/>.</summary>
    public IReadOnlyList<string> EmitSmokeCase(string runDirectory, string runKey, string? designName = null)
    {
        var files = SolverLauncherPolicy.EmitCavityCaseFiles(runKey, designName);
        var written = new List<string>(files.Count);
        foreach (var (relPath, content) in files)
        {
            string full = Path.Combine(runDirectory, relPath.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            WriteUtf8Exclusive(full, content);
            written.Add(relPath);
        }
        return written;
    }

    /// <summary>
    /// M5 safe cleanup: deletes <c>runs/&lt;runKey&gt;</c>; any symlink/junction inside is unlinked without
    /// recursing into or touching its target.
    /// </summary>
    public void SafeDeleteRunDirectory(string runKey)
    {
        SolverLauncherPolicy.ValidateRunKey(runKey);
        string runDir = Path.Combine(RunsRoot, runKey);
        if (!Directory.Exists(runDir)) return;
        if (IsReparseOrLink(runDir))
        {
            Directory.Delete(runDir, recursive: false);
            return;
        }
        DeleteDirectoryWithoutFollowingLinks(runDir);
    }

    /// <summary>M1-M7: runs one OpenFOAM app at a time with clean env, argv-only, and live Disallowing banner verification.</summary>
    public async Task<SolverLaunchResult> LaunchAsync(SolverLaunchRequest request, CancellationToken cancellationToken)
    {
        await singleJobGate.WaitAsync(cancellationToken);
        try
        {
            if (request.MinimumFreeDiskBytes > 0 && HasInsufficientDisk(appDataRoot, request.MinimumFreeDiskBytes))
            {
                return new SolverLaunchResult(false, -1, null, null, null, "disk.low",
                    "Insufficient free disk space before launch.", "", 0);
            }

            var prepared = PrepareExclusiveRunDirectory(request.RunKey);
            return await ExecuteInPreparedDirectoryAsync(prepared, request, request.App, request.TypedArgs, cancellationToken);
        }
        finally
        {
            singleJobGate.Release();
        }
    }

    /// <summary>
    /// Runs the bundled M1/M2/M4/M5/M6 OpenFOAM cavity smoke sequence (<c>blockMesh</c> → <c>checkMesh</c> → <c>icoFoam</c>)
    /// inside a single exclusive run directory and verifies the master-rank <c>Disallowing</c> banner, build id,
    /// <c>Mesh OK</c>, final <c>Time = 0.5</c>, and finite Courant mean.
    /// </summary>
    public async Task<SolverLaunchResult> RunOpenFoamSmokeAsync(
        SetupOs os,
        string activationPathOrDistro,
        string runKey,
        string? expectedBuildId,
        bool allowUnverifiedBuild = false,
        TimeSpan? perStepTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await singleJobGate.WaitAsync(cancellationToken);
        try
        {
            if (HasInsufficientDisk(appDataRoot, 100_000_000L))
            {
                return new SolverLaunchResult(false, -1, null, null, null, "disk.low",
                    "Insufficient free disk space before smoke run.", "", 0);
            }

            var prepared = PrepareExclusiveRunDirectory(runKey);
            EmitSmokeCase(prepared.RunDirectory, runKey);

            string caseArg = os == SetupOs.Windows
                ? SolverLauncherPolicy.ToWslPath(prepared.RunDirectory)
                : prepared.RunDirectory;
            string[] caseArgs = ["-case", caseArg];
            var baseReq = new SolverLaunchRequest(
                runKey, os, activationPathOrDistro, OpenFoamApp.BlockMesh,
                expectedBuildId, allowUnverifiedBuild, caseArgs, perStepTimeout ?? TimeSpan.FromSeconds(60));

            long totalMs = 0;
            var bm = await ExecuteInPreparedDirectoryAsync(prepared, baseReq, OpenFoamApp.BlockMesh, caseArgs, cancellationToken);
            totalMs += bm.DurationMs;
            if (!bm.Succeeded) return bm with { DurationMs = totalMs };

            var cm = await ExecuteInPreparedDirectoryAsync(prepared, baseReq, OpenFoamApp.CheckMesh, caseArgs, cancellationToken);
            totalMs += cm.DurationMs;
            if (!cm.Succeeded) return cm with { DurationMs = totalMs };
            if (!cm.OutputExcerptRedacted.Contains("Mesh OK", StringComparison.Ordinal))
            {
                return new SolverLaunchResult(false, cm.ExitCode, cm.BannerStatus, cm.BuildId, null,
                    "smoke.no-outputs", "checkMesh did not report Mesh OK.", cm.OutputExcerptRedacted, totalMs);
            }

            var ico = await ExecuteInPreparedDirectoryAsync(prepared, baseReq, OpenFoamApp.IcoFoam, caseArgs, cancellationToken);
            totalMs += ico.DurationMs;
            if (!ico.Succeeded) return ico with { DurationMs = totalMs };

            bool hasFinalTime = Directory.Exists(Path.Combine(prepared.RunDirectory, "0.5"));
            if (!hasFinalTime || ico.CourantMean is null)
            {
                return new SolverLaunchResult(false, ico.ExitCode, ico.BannerStatus, ico.BuildId, ico.CourantMean,
                    "smoke.no-outputs", "icoFoam did not produce expected time directory 0.5 and Courant mean.",
                    ico.OutputExcerptRedacted, totalMs);
            }

            return ico with { DurationMs = totalMs };
        }
        finally
        {
            singleJobGate.Release();
        }
    }

    internal static string ResolveWindowsSystemRoot()
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return string.IsNullOrWhiteSpace(dir) ? @"C:\Windows" : dir;
    }

    private async Task<SolverLaunchResult> ExecuteInPreparedDirectoryAsync(
        PreparedRunDirectory prepared,
        SolverLaunchRequest request,
        OpenFoamApp app,
        IReadOnlyList<string>? typedArgs,
        CancellationToken cancellationToken)
    {
        string? wslHome = request.Os == SetupOs.Windows
            ? SolverLauncherPolicy.ToWslPath(prepared.HomeDirectory)
            : null;
        var command = SolverLauncherPolicy.BuildArgv(request.Os, request.ActivationPathOrDistro, app, typedArgs, wslHome);
        string defaultPath = request.Os == SetupOs.Windows
            ? @"C:\Windows\System32;C:\Windows"
            : "/usr/bin:/bin:/usr/sbin:/sbin";
        string? winSystemRoot = request.Os == SetupOs.Windows ? ResolveWindowsSystemRoot() : null;
        var cleanEnv = SolverLauncherPolicy.BuildCleanEnvironment(prepared.HomeDirectory, defaultPath, windowsSystemRoot: winSystemRoot);

        var runReq = new ProcessRunRequest(
            command.Executable,
            command.Arguments,
            prepared.RunDirectory,
            cleanEnv,
            request.Timeout ?? TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(2));

        var result = await runner.RunAsync(runReq, cancellationToken);
        string combined = string.IsNullOrEmpty(result.StandardError)
            ? result.StandardOutput
            : result.StandardOutput + "\n" + result.StandardError;
        string excerpt = TrimAndRedactExcerpt(combined);

        if (result.TimedOut)
            return new SolverLaunchResult(false, result.ExitCode, null, null, null, "smoke.timeout", "The test run exceeded its 60 s limit.", excerpt, result.DurationMs);
        if (result.Cancelled)
            return new SolverLaunchResult(false, result.ExitCode, null, null, null, "cancelled", "Launch stopped by user request.", excerpt, result.DurationMs);

        var banner = SolverLauncherPolicy.ParseMasterBanner(combined, request.ExpectedBuildId, request.AllowUnverifiedBuild);
        if (!banner.Passed)
        {
            return new SolverLaunchResult(false, result.ExitCode, banner.BannerStatus, banner.BuildId, null,
                banner.CauseCode, banner.Reason, excerpt, result.DurationMs);
        }

        if (result.ExitCode != 0)
        {
            return new SolverLaunchResult(false, result.ExitCode, banner.BannerStatus, banner.BuildId, null,
                "unknown", $"Solver exited with code {result.ExitCode}.", excerpt, result.DurationMs);
        }

        double? courant = SolverLauncherPolicy.ParseFinalCourantMean(combined);
        return new SolverLaunchResult(true, 0, banner.BannerStatus, banner.BuildId, courant, null,
            "Disallowing banner and exit 0 verified.", excerpt, result.DurationMs);
    }

    /// <summary>M8 / DR-SEC-1 A: checks observed install file hashes against the route's pinned hashes.</summary>
    public static InstallIdentityVerification VerifyInstallIdentity(
        RouteDefinition route,
        IReadOnlyDictionary<string, string> observedFileHashes)
    {
        var mismatched = new List<string>();
        var canonical = new StringBuilder();
        foreach (var kv in observedFileHashes.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            canonical.Append(kv.Key).Append('=').Append(kv.Value.ToLowerInvariant()).Append('\n');
            var pin = route.Pins.FirstOrDefault(p => string.Equals(p.Id, kv.Key, StringComparison.Ordinal));
            if (pin is null || !string.Equals(pin.Sha256, kv.Value, StringComparison.OrdinalIgnoreCase))
                mismatched.Add(kv.Key);
        }

        string combinedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        bool verified = observedFileHashes.Count > 0 && mismatched.Count == 0;
        return new InstallIdentityVerification(verified, !verified, combinedHash, mismatched);
    }

    /// <summary>Computes SHA-256 of a file using explicit delete-sharing read (READER-SHARE compliant).</summary>
    public static string ComputeFileSha256(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var sha = SHA256.Create();
        return Convert.ToHexStringLower(sha.ComputeHash(fs));
    }

    private static void DeleteDirectoryWithoutFollowingLinks(string dir)
    {
        foreach (string file in Directory.GetFiles(dir))
        {
            File.Delete(file);
        }
        foreach (string sub in Directory.GetDirectories(dir))
        {
            if (IsReparseOrLink(sub))
                Directory.Delete(sub, recursive: false);
            else
                DeleteDirectoryWithoutFollowingLinks(sub);
        }
        Directory.Delete(dir, recursive: false);
    }

    private static void WriteUtf8Exclusive(string path, string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush(flushToDisk: true);
    }

    private static bool HasInsufficientDisk(string path, long minimumBytes)
    {
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(root)) return false;
            var drive = new DriveInfo(root);
            return drive.IsReady && drive.AvailableFreeSpace < minimumBytes;
        }
        catch
        {
            return false;
        }
    }

    private static string TrimAndRedactExcerpt(string text)
    {
        string redacted = SetupReportBuilder.Redact(text, Environment.UserName, Environment.MachineName);
        const int maxBytes = 4096;
        if (redacted.Length <= maxBytes) return redacted;
        return redacted[^maxBytes..];
    }
}
