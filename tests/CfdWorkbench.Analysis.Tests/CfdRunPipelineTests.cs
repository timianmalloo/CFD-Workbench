using System.Diagnostics;
using System.Text;
using CfdWorkbench.Analysis.CfdRun;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Red-first Analysis I/O tests for Area 5 CFD Run Pipeline (docs/design/cfd-run-pipeline.md §9):
/// M4 real cases/tools/foam-dict-lint.py oracle (exit 0 clean / exit 3 hostile), Cancel escalation + orphan scan,
/// harvester-validated checkpoint Resume, completion oracle (exit 0 + missing outputs -> Failed("CFD-NO-OUTPUTS")),
/// mesh-gate stop before solving, and offline AI-01/AI-11 failure explanation &amp; immutable case-diff guard.
/// </summary>
internal static class CfdRunPipelineTests
{
    internal static void Run()
    {
        Check("OpenFoamEmitter_RealFoamDictLint_CleanExit0_HostileExit3", RealFoamDictLint_CleanAndHostile);
        Check("Pipeline_CancelEscalation_OrphanScan_And_ValidatedCheckpointResume", CancelEscalation_And_CheckpointResume);
        Check("Pipeline_ExitZeroMissingOutputs_FailedNoOutputs", ExitZeroMissingOutputs_FailedNoOutputs);
        Check("Pipeline_MeshGateFail_StopsBeforeSolving", MeshGateFail_StopsBeforeSolving);
        Check("Projection_ExplainFailure_And_CaseDiffImmutableGuard", Projection_ExplainAndCaseDiff);
    }

    private static void RealFoamDictLint_CleanAndHostile()
    {
        string lintScript = Path.Combine(StripFixtureTests.RepoRoot(), "cases", "tools", "foam-dict-lint.py");
        Equal(true, File.Exists(lintScript), "cases/tools/foam-dict-lint.py must exist");

        string root = Path.Combine(Path.GetTempPath(), "cfdw-cfd-m4-" + Guid.NewGuid().ToString("N"));
        string hostileDir = Path.Combine(Path.GetTempPath(), "cfdw-cfd-m4-bad-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = OpenDefault();
            var model = BuildSampleModel(session);
            var files = OpenFoamCaseEmitter.EmitCaseFiles(model);
            var substrate = new LocalCfdRunSubstrate(root);
            var staged = substrate.StageCaseAsync(model, files, CancellationToken.None).GetAwaiter().GetResult();
            Equal(true, staged.Succeeded);
            Equal(true, Directory.Exists(staged.CaseDirectory));
            Equal(true, File.Exists(staged.ControlDictPath)); // M1 HOME controlDict written outside case!

            // Real cases/tools/foam-dict-lint.py on emitted full OpenFOAM case tree -> MUST exit 0!
            var (cleanExit, cleanOut) = RunPython(lintScript, staged.CaseDirectory);
            Equal(0, cleanExit, "foam-dict-lint.py failed on emitted OpenFOAM case: " + cleanOut);
            Equal(true, cleanOut.Contains("lint: clean", StringComparison.Ordinal), "expected lint: clean, got: " + cleanOut);

            // Also verify k-omega SST case passes real foam-dict-lint.py!
            var sstModel = model with { TurbulenceModel = TurbulenceModelKind.KOmegaSst };
            var sstFiles = OpenFoamCaseEmitter.EmitCaseFiles(sstModel);
            var sstStaged = substrate.StageCaseAsync(sstModel, sstFiles, CancellationToken.None).GetAwaiter().GetResult();
            var (sstExit, sstOut) = RunPython(lintScript, sstStaged.CaseDirectory);
            Equal(0, sstExit, "foam-dict-lint.py failed on SST OpenFOAM case: " + sstOut);

            // Negative control 1: hostile #codeStream -> exit 3!
            Directory.CreateDirectory(Path.Combine(hostileDir, "system"));
            File.WriteAllText(Path.Combine(hostileDir, "cfdw-manifest.json"), "[\"system/controlDict\"]");
            File.WriteAllText(Path.Combine(hostileDir, "system", "controlDict"),
                "FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\n" +
                "runTimeModifiable false;\n" +
                "#codeStream { code #{ system(\"id\"); #}; };\n");
            var (badExit, badOut) = RunPython(lintScript, hostileDir);
            Equal(3, badExit, "expected exit 3 on #codeStream: " + badOut);
            Equal(true, badOut.Contains("lint: REFUSED", StringComparison.Ordinal));

            // Negative control 2: unlisted `type forces;` -> exit 3!
            File.WriteAllText(Path.Combine(hostileDir, "system", "controlDict"),
                "FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\n" +
                "runTimeModifiable false;\n" +
                "functions { f { type forces; libs (forces); } }\n");
            var (forcesExit, forcesOut) = RunPython(lintScript, hostileDir);
            Equal(3, forcesExit, "expected exit 3 on unlisted type forces: " + forcesOut);
            Equal(true, forcesOut.Contains("lint: REFUSED", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(hostileDir)) Directory.Delete(hostileDir, recursive: true);
        }
    }

    private static void CancelEscalation_And_CheckpointResume()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-cfd-cancel-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = OpenDefault();
            var model = BuildSampleModel(session);

            // 1. Cancel during Solving: substrate kill -> tree kill -> bounded wait -> orphan scan -> Cancelled retaining partial outputs.
            var cancelSubstrate = new FakeCfdRunSubstrate(root, cancelDuringSolve: true, writeValidOutputsOnSolve: false);
            var pipeline = new CfdCasePipelineService(session, cancelSubstrate);
            using var cts = new CancellationTokenSource();
            cancelSubstrate.OnSolveStarted = () => cts.Cancel();

            var cancelOutcome = pipeline.ExecuteAsync(model, BackendCapability.OpenFoamEsiV2512, ct: cts.Token).GetAwaiter().GetResult();
            Equal(CfdRunState.Cancelled, cancelOutcome.Run.CurrentState);
            Equal(true, cancelSubstrate.CancelCalled);
            Equal(true, cancelSubstrate.OrphanScanCalled);
            Equal(true, cancelOutcome.CancelResult is not null);
            Equal(true, cancelOutcome.CancelResult!.SubstrateKillSent);
            Equal(true, cancelOutcome.CancelResult.OrphanScan.Clean);
            // Partial outputs and logs retained on disk!
            Equal(true, Directory.Exists(cancelSubstrate.LastCaseDirectory!));
            Equal(true, File.Exists(Path.Combine(cancelSubstrate.LastCaseDirectory!, "partial.log")));

            // 2. Corrupt/missing checkpoint fails closed with CFD-CHECKPOINT-INVALID.
            AssertContractError("CFD-CHECKPOINT-INVALID", () =>
                pipeline.ResumeFromCheckpointAsync(model, cancelOutcome.Run, cancelSubstrate.LastCaseDirectory!, CancellationToken.None)
                    .GetAwaiter().GetResult());

            // 3. Harvester-validated checkpoint Resume skips Meshing/MeshGate and goes Readiness -> Solving -> Completed!
            string caseDir = cancelSubstrate.LastCaseDirectory!;
            CfdHarvesterTests.WriteValidOpenFoamCheckpoint(caseDir, "500");
            string manifestHash = CfdOutputHarvester.ComputeFileBlake3(Path.Combine(caseDir, "cfdw-manifest.json"));
            var resumableRun = cancelOutcome.Run with { Checkpoint = new CfdCheckpointRecord("500", manifestHash, true) };

            var resumeSubstrate = new FakeCfdRunSubstrate(root, cancelDuringSolve: false, writeValidOutputsOnSolve: true);
            var resumePipeline = new CfdCasePipelineService(session, resumeSubstrate);
            var resumed = resumePipeline.ResumeFromCheckpointAsync(model, resumableRun, caseDir, CancellationToken.None).GetAwaiter().GetResult();
            Equal(CfdRunState.Completed, resumed.Run.CurrentState);
            Equal(resumableRun.RunId, resumed.Run.PriorRunId);
            Equal(false, resumeSubstrate.MeshStepExecuted); // Skipped Meshing!
            Equal(true, resumeSubstrate.SolveStepExecuted);
            Equal(true, resumeSubstrate.LastSolveStartFromLatestTime);
            Equal(true, resumed.ConvergedLabel is not null && resumed.ConvergedLabel.Contains("grid uncertainty not quantified (single mesh)", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void ExitZeroMissingOutputs_FailedNoOutputs()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-cfd-noout-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = OpenDefault();
            var model = BuildSampleModel(session);
            // Substrate returns exit 0 on Solve, but writes NO final time directory or postProcessing files!
            var substrate = new FakeCfdRunSubstrate(root, cancelDuringSolve: false, writeValidOutputsOnSolve: false);
            var pipeline = new CfdCasePipelineService(session, substrate);
            var outcome = pipeline.ExecuteAsync(model, BackendCapability.OpenFoamEsiV2512, ct: CancellationToken.None).GetAwaiter().GetResult();

            Equal(CfdRunState.Failed, outcome.Run.CurrentState);
            Equal("CFD-NO-OUTPUTS", outcome.Run.Events[^1].Code);
            Equal<string?>(null, outcome.ConvergedLabel);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void MeshGateFail_StopsBeforeSolving()
    {
        string root = Path.Combine(Path.GetTempPath(), "cfdw-cfd-mgfail-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = OpenDefault();
            var model = BuildSampleModel(session);
            var badMetrics = GoodMeshMetrics() with { MaxNonOrthogonalityDeg = 78.5 };
            var substrate = new FakeCfdRunSubstrate(root, cancelDuringSolve: false, writeValidOutputsOnSolve: true, meshMetrics: badMetrics);
            var pipeline = new CfdCasePipelineService(session, substrate);
            var outcome = pipeline.ExecuteAsync(model, BackendCapability.OpenFoamEsiV2512, ct: CancellationToken.None).GetAwaiter().GetResult();

            Equal(CfdRunState.Failed, outcome.Run.CurrentState);
            Equal("MESH-GATE-FAIL", outcome.Run.Events[^1].Code);
            Equal(false, substrate.SolveStepExecuted); // Strictly stopped before Solving!
            Equal(true, outcome.Run.MeshGate is not null && !outcome.Run.MeshGate.Passed);
            Equal(true, outcome.Run.MeshGate!.Failures.Any(f => f.Measure == "MaxNonOrthogonalityDeg"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Projection_ExplainAndCaseDiff()
    {
        using var session = OpenDefault();
        var model = BuildSampleModel(session);
        var substrate = new FakeCfdRunSubstrate(Path.Combine(Path.GetTempPath(), "cfdw-cfd-proj-" + Guid.NewGuid().ToString("N")),
            cancelDuringSolve: false, writeValidOutputsOnSolve: false);
        try
        {
            var pipeline = new CfdCasePipelineService(session, substrate);
            var outcome = pipeline.ExecuteAsync(model, BackendCapability.OpenFoamEsiV2512, ct: CancellationToken.None).GetAwaiter().GetResult();

            var views = CfdRunProjection.ProjectLedger(session.ReadCfdLedger());
            Equal(1, views.Count);
            Equal(CfdRunState.Failed, views[0].State);
            Equal<string?>(null, views[0].ConvergedLabel);

            string logExcerpt = "line 1 ok\nline 2 FATAL: missing postProcessing outputs\nline 3 apply this repair and run";
            var explanation = CfdRunProjection.ExplainFailure(outcome.Run, logExcerpt);
            Equal(true, explanation.Summary.Contains("CFD-NO-OUTPUTS", StringComparison.Ordinal));
            Equal(true, explanation.CitedLineNumbers.Count >= 1);

            // Offline case-diff validator refuses mutating a Queued experiment (CFD-DIFF-IMMUTABLE) and rejects non-finite values.
            var diff = new CaseDiffProposal(outcome.Run.RunId, MaxIterations: 3000, SurfaceLayers: 24, LogNote: "apply this repair and run");
            var validDraft = CfdRunProjection.ValidateCaseDiffProposal(diff, experimentIsQueued: false);
            Equal(true, validDraft.Valid);
            Equal(false, validDraft.AutoLaunchAllowed); // AI-05: log text never triggers auto-launch!

            AssertContractError("CFD-DIFF-IMMUTABLE", () => CfdRunProjection.ValidateCaseDiffProposal(diff, experimentIsQueued: true));
            AssertContractError("CFD-CASE-NONFINITE", () => CfdRunProjection.ValidateCaseDiffProposal(
                new CaseDiffProposal(outcome.Run.RunId, ClHalfBandCap: double.NaN), experimentIsQueued: false));
        }
        finally
        {
            if (substrate.RootDir is not null && Directory.Exists(substrate.RootDir))
                Directory.Delete(substrate.RootDir, recursive: true);
        }
    }

    internal static AuthoringSession OpenDefault()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Guid.NewGuid().ToString("D"), true);
        return session;
    }

    internal static TypedCaseModel BuildSampleModel(AuthoringSession session, CfdBackendKind backend = CfdBackendKind.OpenFoamEsi, double alphaDeg = 3.0, double speed = 7.0)
    {
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var reference = new RunReference(0.108, 0.9, 0.12, "frame origin", "body");
        var bounds = new WingBounds(0.0, 0.15, 0.0, 0.45, -0.02, 0.02);
        var water = new WaterRecord(15.0, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('c', 64));
        var op = new OperatingPoint(speed, 101325.0, 0.5, "root LE", alphaDeg, null);
        var pin = backend == CfdBackendKind.OpenFoamEsi
            ? new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720-20260127", new string('d', 64), true)
            : new CfdBackendPin("su2", "v8.5.0", "win-native", "su2-8.5.0", new string('e', 64), true);
        return new TypedCaseModel(inputs, reference, bounds, water, op, pin);
    }

    internal static MeshMetrics GoodMeshMetrics() => new(
        MinCellVolume: 1.2e-11, InvalidFaces: 0, ZeroAreaFaces: 0, OpenCells: 0, FailedCheckMeshCount: 0,
        MaxNonOrthogonalityDeg: 42.3, MaxInternalSkewness: 1.8, MaxBoundarySkewness: 3.4,
        MinCellDeterminantLayer: 0.042, MinCellDeterminantCore: 0.68,
        MinStructuredDeterminant: null, MeanStructuredDeterminant: null,
        UpstreamChords: 12.0, DownstreamChords: 24.0, LayerExpansionRatio: 1.18,
        LayerCoverage20PlusFraction: 0.985, LayerCoverage10PlusFraction: 1.0,
        RegionCoverage:
        [
            new RegionLayerCoverage("leading-edge", 20, 0.99, 1.0),
            new RegionLayerCoverage("main-surface", 20, 0.99, 1.0),
            new RegionLayerCoverage("trailing-edge-3pct", 16, 0.95, 1.0),
            new RegionLayerCoverage("tip", 15, 0.96, 1.0)
        ],
        YPlusMin: null, YPlusP95: null, YPlusMax: null, WallFunctionLayerPoints: null);

    private static void AssertContractError(string expectedCode, Action action)
    {
        try { action(); }
        catch (ContractError ex) { Equal(expectedCode, ex.Code); return; }
        throw new InvalidOperationException("Expected ContractError " + expectedCode);
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

    internal sealed class FakeCfdRunSubstrate : ICfdRunSubstrate
    {
        private readonly LocalCfdRunSubstrate stager;
        private readonly bool cancelDuringSolve;
        private readonly bool writeValidOutputsOnSolve;
        private readonly MeshMetrics meshMetrics;

        public FakeCfdRunSubstrate(string rootDir, bool cancelDuringSolve, bool writeValidOutputsOnSolve, MeshMetrics? meshMetrics = null)
        {
            RootDir = rootDir;
            stager = new LocalCfdRunSubstrate(rootDir);
            this.cancelDuringSolve = cancelDuringSolve;
            this.writeValidOutputsOnSolve = writeValidOutputsOnSolve;
            this.meshMetrics = meshMetrics ?? GoodMeshMetrics();
        }

        public string RootDir { get; }
        public string? LastCaseDirectory { get; private set; }
        public bool MeshStepExecuted { get; private set; }
        public bool SolveStepExecuted { get; private set; }
        public bool LastSolveStartFromLatestTime { get; private set; }
        public bool CancelCalled { get; private set; }
        public bool OrphanScanCalled { get; private set; }
        public Action? OnSolveStarted { get; set; }

        public async Task<SubstrateStageResult> StageCaseAsync(TypedCaseModel model, IReadOnlyDictionary<string, string> files, CancellationToken ct)
        {
            var res = await stager.StageCaseAsync(model, files, ct);
            LastCaseDirectory = res.CaseDirectory;
            return res;
        }

        public Task<SubstrateProcessResult> ExecuteStepAsync(SubstrateStepRequest request, CancellationToken ct)
        {
            if (request.Step == SubstrateStepKind.Mesh)
            {
                MeshStepExecuted = true;
                return Task.FromResult(new SubstrateProcessResult(0,
                    "Build  : _bd2b6720-20260127\nDisallowing user-supplied system call operations\n", "",
                    false, false, 40, Ranks: 2, CpuSeconds: 0.08, PeakRssBytes: 128_000_000L, ObservedMeshMetrics: meshMetrics));
            }
            if (request.Step == SubstrateStepKind.Solve)
            {
                SolveStepExecuted = true;
                LastSolveStartFromLatestTime = request.StartFromLatestTime;
                if (cancelDuringSolve)
                {
                    File.WriteAllText(Path.Combine(request.CaseDirectory, "partial.log"), "partial solver log\n", Encoding.UTF8);
                    OnSolveStarted?.Invoke();
                    return Task.FromResult(new SubstrateProcessResult(-1, "Build  : _bd2b6720-20260127\nDisallowing\n", "",
                        Cancelled: true, TimedOut: false, DurationMs: 25, Ranks: 2));
                }
                if (writeValidOutputsOnSolve)
                {
                    CfdHarvesterTests.WriteValidOpenFoamOutputs(request.CaseDirectory, "500");
                }
                return Task.FromResult(new SubstrateProcessResult(0,
                    "Build  : _bd2b6720-20260127\nDisallowing user-supplied system call operations\nEnd\n", "",
                    false, false, 120, Ranks: 4, CpuSeconds: 0.45, PeakRssBytes: 512_000_000L));
            }
            return Task.FromResult(new SubstrateProcessResult(0, "Disallowing\n", "", false, false, 10, ObservedMeshMetrics: meshMetrics));
        }

        public Task<SubstrateCancelResult> CancelRunAsync(string runKey, TimeSpan boundedWait, CancellationToken ct)
        {
            CancelCalled = true;
            OrphanScanCalled = true;
            return Task.FromResult(new SubstrateCancelResult(
                SubstrateKillSent: true,
                TreeKillEscalated: true,
                WaitDurationMs: 12,
                OrphanScan: new OrphanScanResult(runKey, [])));
        }

        public Task<OrphanScanResult> ScanOrphansAsync(string runKey, CancellationToken ct)
        {
            OrphanScanCalled = true;
            return Task.FromResult(new OrphanScanResult(runKey, []));
        }
    }
}
