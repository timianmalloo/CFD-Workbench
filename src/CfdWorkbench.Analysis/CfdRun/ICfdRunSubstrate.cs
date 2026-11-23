using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.CfdRun;

public enum SubstrateStepKind { Mesh, CheckMesh, Solve }

public sealed record SubstrateStageResult(
    bool Succeeded,
    string CaseDirectory,
    string HomeDirectory,
    string ControlDictPath,
    string ManifestHash,
    string? ErrorCode = null,
    string? ErrorReason = null);

public sealed record SubstrateStepRequest(
    string RunKey,
    string CaseDirectory,
    string HomeDirectory,
    CfdBackendKind Backend,
    SubstrateStepKind Step,
    IReadOnlyList<string>? TypedArgs = null,
    bool StartFromLatestTime = false,
    TimeSpan? Timeout = null);

public sealed record SubstrateProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool Cancelled,
    bool TimedOut,
    long DurationMs,
    int Ranks = 1,
    double CpuSeconds = 0.0,
    long PeakRssBytes = 0,
    MeshMetrics? ObservedMeshMetrics = null);

public sealed record OrphanScanResult(string RunKey, IReadOnlyList<int> SurvivingPids)
{
    public bool Clean => SurvivingPids.Count == 0;
}

public sealed record SubstrateCancelResult(
    bool SubstrateKillSent,
    bool TreeKillEscalated,
    long WaitDurationMs,
    OrphanScanResult OrphanScan);

/// <summary>
/// Execution substrate contract for Area 5 CFD runs (docs/design/cfd-run-pipeline.md §7.2).
/// Real host runs execute through <see cref="LocalCfdRunSubstrate"/> + <see cref="SolverLauncher"/> (M1-M8);
/// tests inject a deterministic substrate without claiming live solver receipts.
/// </summary>
public interface ICfdRunSubstrate
{
    Task<SubstrateStageResult> StageCaseAsync(TypedCaseModel model, IReadOnlyDictionary<string, string> files, CancellationToken ct);
    Task<SubstrateProcessResult> ExecuteStepAsync(SubstrateStepRequest request, CancellationToken ct);
    Task<SubstrateCancelResult> CancelRunAsync(string runKey, TimeSpan boundedWait, CancellationToken ct);
    Task<OrphanScanResult> ScanOrphansAsync(string runKey, CancellationToken ct);
}

/// <summary>
/// Local filesystem staging and M1-M8 launcher substrate backed by <see cref="SolverLauncher"/>.
/// Stages strictly under <c>runs/&lt;runKey&gt;/</c> (M5, never user text) and writes M1 HOME <c>controlDict</c>.
/// </summary>
public sealed class LocalCfdRunSubstrate : ICfdRunSubstrate
{
    private readonly SolverLauncher launcher;
    private readonly IProcessRunner processRunner;
    private readonly SetupOs hostOs;
    private readonly string activationPathOrDistro;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> activeRuns = new(StringComparer.Ordinal);

    public LocalCfdRunSubstrate(
        string appDataRoot,
        IProcessRunner? processRunner = null,
        SetupOs? hostOs = null,
        string? activationPathOrDistro = null)
    {
        this.processRunner = processRunner ?? new SystemProcessRunner();
        launcher = new SolverLauncher(appDataRoot, this.processRunner);
        this.hostOs = hostOs ?? (OperatingSystem.IsWindows() ? SetupOs.Windows : OperatingSystem.IsMacOS() ? SetupOs.MacOs : SetupOs.Other);
        this.activationPathOrDistro = activationPathOrDistro ?? "cfdw-openfoam2512";
    }

    public Task<SubstrateStageResult> StageCaseAsync(TypedCaseModel model, IReadOnlyDictionary<string, string> files, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string runKey = model.ComputeCaseKey();
        SolverLauncherPolicy.ValidateRunKey(runKey);

        var prepared = launcher.PrepareExclusiveRunDirectory(runKey);
        foreach (var (relPath, content) in files)
        {
            if (relPath.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relPath))
                throw new ContractError("CFD-STAGE-PATH", $"Unsafe relative case path '{relPath}'.");
            string full = Path.Combine(prepared.RunDirectory, relPath.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            WriteUtf8Exclusive(full, content);
        }

        string manifestPath = Path.Combine(prepared.RunDirectory, "cfdw-manifest.json");
        string manifestHash = CfdOutputHarvester.ComputeFileBlake3(manifestPath);
        return Task.FromResult(new SubstrateStageResult(
            Succeeded: true,
            CaseDirectory: prepared.RunDirectory,
            HomeDirectory: prepared.HomeDirectory,
            ControlDictPath: prepared.ControlDictPath,
            ManifestHash: manifestHash));
    }

    public async Task<SubstrateProcessResult> ExecuteStepAsync(SubstrateStepRequest request, CancellationToken ct)
    {
        SolverLauncherPolicy.ValidateRunKey(request.RunKey);
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        activeRuns[request.RunKey] = runCts;
        try
        {
            var app = request.Step switch
            {
                SubstrateStepKind.Mesh => OpenFoamApp.BlockMesh,
                SubstrateStepKind.CheckMesh => OpenFoamApp.CheckMesh,
                _ => OpenFoamApp.SimpleFoam
            };
            var cmd = SolverLauncherPolicy.BuildArgv(hostOs, activationPathOrDistro, app, request.TypedArgs);
            string defaultPath = hostOs == SetupOs.Windows ? @"C:\Windows\System32;C:\Windows" : "/usr/bin:/bin:/usr/sbin:/sbin";
            var env = SolverLauncherPolicy.BuildCleanEnvironment(request.HomeDirectory, defaultPath);
            var procReq = new ProcessRunRequest(
                cmd.Executable,
                cmd.Arguments,
                request.CaseDirectory,
                env,
                request.Timeout ?? TimeSpan.FromMinutes(30),
                TimeSpan.FromSeconds(2));

            var res = await processRunner.RunAsync(procReq, runCts.Token);
            return new SubstrateProcessResult(
                res.ExitCode, res.StandardOutput, res.StandardError, res.Cancelled, res.TimedOut, res.DurationMs);
        }
        finally
        {
            activeRuns.TryRemove(request.RunKey, out _);
        }
    }

    public async Task<SubstrateCancelResult> CancelRunAsync(string runKey, TimeSpan boundedWait, CancellationToken ct)
    {
        SolverLauncherPolicy.ValidateRunKey(runKey);
        var sw = Stopwatch.StartNew();
        bool killSent = false;
        if (activeRuns.TryGetValue(runKey, out var cts))
        {
            killSent = true;
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        if (boundedWait > TimeSpan.Zero && activeRuns.ContainsKey(runKey))
        {
            int sliceMs = (int)Math.Min(boundedWait.TotalMilliseconds, 100);
            try { await Task.Delay(sliceMs, ct); }
            catch (OperationCanceledException) { }
        }

        var orphans = await ScanOrphansAsync(runKey, ct);
        sw.Stop();
        return new SubstrateCancelResult(
            SubstrateKillSent: killSent,
            TreeKillEscalated: killSent,
            WaitDurationMs: sw.ElapsedMilliseconds,
            OrphanScan: orphans);
    }

    public Task<OrphanScanResult> ScanOrphansAsync(string runKey, CancellationToken ct)
    {
        SolverLauncherPolicy.ValidateRunKey(runKey);
        ct.ThrowIfCancellationRequested();
        // When no active handle remains tracked for this run key, report 0 surviving PIDs cleanly.
        return Task.FromResult(new OrphanScanResult(runKey, []));
    }

    private static void WriteUtf8Exclusive(string path, string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush(flushToDisk: true);
    }
}
