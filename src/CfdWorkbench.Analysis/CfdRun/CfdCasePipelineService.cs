using System.Diagnostics;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.CfdRun;

public sealed record CfdPipelineOutcome(
    CfdRunRecord Run,
    IReadOnlyList<FieldEvidenceRecord> HarvestedEvidence,
    string? ConvergedLabel,
    SubstrateCancelResult? CancelResult = null);

public sealed record InterruptedRunRecovery(
    string RunId,
    string RunKey,
    OrphanScanResult OrphanScan,
    bool CheckpointAvailable,
    string RecommendedAction);

/// <summary>
/// Orchestrates Area 5 CFD runs through all 11 states: Readiness -> Meshing -> MeshGate -> Solving -> Harvesting ->
/// append-only RecordCfdRun/RecordFieldEvidence, with substrate Cancel escalation, orphan scan, completion oracle,
/// and harvester-validated checkpoint Resume (docs/design/cfd-run-pipeline.md §6-§8).
/// </summary>
public sealed class CfdCasePipelineService
{
    private readonly AuthoringSession session;
    private readonly ICfdRunSubstrate substrate;
    private readonly TimeProvider clock;

    public CfdCasePipelineService(AuthoringSession session, ICfdRunSubstrate substrate, TimeProvider? clock = null)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.substrate = substrate ?? throw new ArgumentNullException(nameof(substrate));
        this.clock = clock ?? TimeProvider.System;
    }

    public async Task<CfdPipelineOutcome> ExecuteAsync(
        TypedCaseModel model,
        BackendCapability capability,
        string? experimentId = null,
        int caseIndex = 0,
        string? priorRunId = null,
        MeshGateOptions? meshGateOptions = null,
        TimeSpan? cancelBoundedWait = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(capability);
        model.ValidateInputs();

        string runId = Guid.NewGuid().ToString("D");
        string caseModelHash = model.ComputeCaseModelHash();
        string runKey = model.ComputeCaseKey();
        string meshHash = model.ComputeMeshHash();
        TimeSpan waitCap = cancelBoundedWait ?? TimeSpan.FromSeconds(5);

        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, clock.GetUtcNow())];

        // 1. Capability pre-check before leaving Pending (SU2 + free surface/cavitation -> Unsupported).
        var cap = model.CheckCapability(capability);
        if (!cap.Supported)
        {
            events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Unsupported, clock.GetUtcNow(), "CFD-UNSUPPORTED-PHYSICS", cap.Reason);
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                null, null, new CfdRunResources(0, 0, 0, 0, 0), null, events, [], null);
        }

        // 2. Readiness: emit deterministic files, run pure M4 pre-flight lint, stage into runs/<runKey>/.
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, clock.GetUtcNow());
        var files = capability.Backend == CfdBackendKind.OpenFoamEsi
            ? OpenFoamCaseEmitter.EmitCaseFiles(model)
            : Su2CaseEmitter.EmitConfigFiles(model);
        if (capability.Backend == CfdBackendKind.OpenFoamEsi)
            OpenFoamCaseEmitter.ValidateNoUnsafeTokens(files);

        var stage = await substrate.StageCaseAsync(model, files, ct);
        if (!stage.Succeeded)
        {
            events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), stage.ErrorCode ?? "CFD-STAGE-FAILED", stage.ErrorReason ?? "Case staging failed.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                null, null, new CfdRunResources(0, 0, 0, 0, 0), null, events, [], null);
        }

        // 3. Meshing
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, clock.GetUtcNow());
        var meshWatch = Stopwatch.StartNew();
        var meshStep = await substrate.ExecuteStepAsync(
            new SubstrateStepRequest(runKey, stage.CaseDirectory, stage.HomeDirectory, capability.Backend, SubstrateStepKind.Mesh),
            ct);
        meshWatch.Stop();

        if (meshStep.Cancelled || ct.IsCancellationRequested)
        {
            var cancelRes = await ExecuteCancelEscalationAsync(runKey, waitCap);
            events = CfdRunStateMachine.TransitionToCancelled(events, clock.GetUtcNow(), "Cancelled during meshing.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                null, null, new CfdRunResources(0, meshStep.Ranks, meshWatch.Elapsed.TotalMilliseconds, meshStep.CpuSeconds, meshStep.PeakRssBytes),
                null, events, [], cancelRes);
        }
        if (meshStep.ExitCode != 0)
        {
            events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), "CFD-MESH-FAILED", $"Meshing exited with code {meshStep.ExitCode}.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                null, null, new CfdRunResources(0, meshStep.Ranks, meshWatch.Elapsed.TotalMilliseconds, meshStep.CpuSeconds, meshStep.PeakRssBytes),
                null, events, [], null);
        }

        // 4. MeshGate (DR-F3-1 A / DR-F2-3 / DR-F2-6) — strictly fail-closed before Solving!
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, clock.GetUtcNow());
        if (meshStep.ObservedMeshMetrics is not { } observedMetrics)
        {
            session.RecordCfdEvent("mesh.generate", "failed", meshWatch.Elapsed.TotalMilliseconds, new CfdTelemetry
            {
                RunKey12 = runKey[..12],
                Backend = model.BackendPin.Backend,
                DurationMs = meshWatch.Elapsed.TotalMilliseconds,
                GateOutcome = "failed"
            });
            events = CfdRunStateMachine.TransitionToFailed(
                events,
                clock.GetUtcNow(),
                "MESH-GATE-FAIL",
                "Mesh quality metrics were not observed; cannot proceed to Solving.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                null, null, new CfdRunResources(0, meshStep.Ranks, meshWatch.Elapsed.TotalMilliseconds, meshStep.CpuSeconds, meshStep.PeakRssBytes),
                null, events, [], null);
        }

        var preSolveOpts = (meshGateOptions ?? new MeshGateOptions(model.WallTreatment)) with { FieldIsStationary = false };
        var meshGate = MeshGateEvaluator.EvaluateMeshGate(observedMetrics, preSolveOpts);

        session.RecordCfdEvent("mesh.generate", meshGate.Passed ? "passed" : "failed", meshWatch.Elapsed.TotalMilliseconds, new CfdTelemetry
        {
            RunKey12 = runKey[..12],
            Backend = model.BackendPin.Backend,
            DurationMs = meshWatch.Elapsed.TotalMilliseconds,
            GateOutcome = meshGate.Passed ? (meshGate.IsScreenOnly ? "screen-only" : "passed") : "failed",
            MaxNonOrthoDeg = meshGate.MaxNonOrthogonalityDeg,
            LayerCoverage20Pct = meshGate.LayerCoverage20PlusFraction * 100.0
        });

        if (!meshGate.Passed)
        {
            string summaryMsg = string.Join("; ", meshGate.Failures.Select(f => f.Message));
            events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), "MESH-GATE-FAIL", summaryMsg);
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                meshGate, null, new CfdRunResources(0, meshStep.Ranks, meshWatch.Elapsed.TotalMilliseconds, meshStep.CpuSeconds, meshStep.PeakRssBytes),
                null, events, [], null);
        }

        // 5. Solving -> Harvesting -> Terminal
        return await ExecuteSolveAndHarvestAsync(
            model, capability, runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash,
            stage.CaseDirectory, stage.HomeDirectory, stage.ManifestHash, meshGate, meshStep.ObservedMeshMetrics,
            meshGateOptions, events, startFromLatestTime: false, waitCap, ct);
    }

    /// <summary>
    /// Resumes from a harvester-validated checkpoint (RUN-04): skips Meshing/MeshGate and goes Readiness -> Solving.
    /// Fails closed with CFD-CHECKPOINT-INVALID on any missing, tampered, or corrupt checkpoint.
    /// </summary>
    public async Task<CfdPipelineOutcome> ResumeFromCheckpointAsync(
        TypedCaseModel model,
        CfdRunRecord priorRun,
        string caseDirectory,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(priorRun);

        var backend = string.Equals(model.BackendPin.Backend, "su2", StringComparison.Ordinal)
            ? CfdBackendKind.Su2
            : CfdBackendKind.OpenFoamEsi;
        var capability = backend == CfdBackendKind.Su2 ? BackendCapability.Su2V850 : BackendCapability.OpenFoamEsiV2512;

        string expectedManifestHash = priorRun.Checkpoint?.ManifestHash ?? "";
        var validation = CfdOutputHarvester.ValidateCheckpoint(caseDirectory, expectedManifestHash, backend);
        string runKey = model.ComputeCaseKey();

        if (!validation.Valid)
        {
            session.RecordCfdEvent("cfd.resume", "CFD-CHECKPOINT-INVALID", 0, new CfdTelemetry
            {
                RunKey12 = runKey[..12],
                CheckpointValidated = false,
                Outcome = "CFD-CHECKPOINT-INVALID"
            });
            throw new ContractError("CFD-CHECKPOINT-INVALID", validation.FailureReason ?? Labels.CfdCheckpointInvalid);
        }

        session.RecordCfdEvent("cfd.resume", "resumed", 0, new CfdTelemetry
        {
            RunKey12 = runKey[..12],
            CheckpointValidated = true,
            TimeDirectory = validation.TimeDirectory,
            Outcome = "resumed"
        });

        string runId = Guid.NewGuid().ToString("D");
        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, clock.GetUtcNow())];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, clock.GetUtcNow());

        string homeDir = Path.Combine(caseDirectory, ".home");
        return await ExecuteSolveAndHarvestAsync(
            model, capability, runId, priorRun.RunId, priorRun.ExperimentId, priorRun.CaseIndex,
            runKey, model.ComputeCaseModelHash(), model.ComputeMeshHash(),
            caseDirectory, homeDir, validation.ManifestHash!, priorRun.MeshGate, null, null,
            events, startFromLatestTime: true, TimeSpan.FromSeconds(5), ct);
    }

    /// <summary>App-quit / relaunch recovery scan (F7 node S3): checks any non-terminal stored runs for surviving orphans.</summary>
    public async Task<IReadOnlyList<InterruptedRunRecovery>> RecoverInterruptedRunsAsync(CancellationToken ct = default)
    {
        var ledger = session.ReadCfdLedger();
        var recoveries = new List<InterruptedRunRecovery>();
        foreach (var stored in ledger.Runs)
        {
            if (CfdRunStateMachine.IsTerminal(stored.Run.CurrentState)) continue;
            var orphans = await substrate.ScanOrphansAsync(stored.Run.RunKey, ct);
            bool hasCheckpoint = stored.Run.Checkpoint?.HarvesterValidated == true;
            string action = !orphans.Clean
                ? Labels.CfdOrphansRemaining(orphans.SurvivingPids.Count)
                : hasCheckpoint
                    ? "Interrupted run stopped cleanly — validated checkpoint available to Resume or Retry."
                    : "Interrupted run stopped cleanly — use Retry sample.";
            recoveries.Add(new InterruptedRunRecovery(stored.Run.RunId, stored.Run.RunKey, orphans, hasCheckpoint, action));
        }
        return recoveries;
    }

    private async Task<CfdPipelineOutcome> ExecuteSolveAndHarvestAsync(
        TypedCaseModel model,
        BackendCapability capability,
        string runId,
        string? priorRunId,
        string? experimentId,
        int caseIndex,
        string runKey,
        string caseModelHash,
        string meshHash,
        string caseDirectory,
        string homeDirectory,
        string manifestHash,
        MeshGateResult? preSolveMeshGate,
        MeshMetrics? observedMetrics,
        MeshGateOptions? meshGateOptions,
        IReadOnlyList<CfdRunStateEvent> events,
        bool startFromLatestTime,
        TimeSpan cancelBoundedWait,
        CancellationToken ct)
    {
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, clock.GetUtcNow());
        var solveWatch = Stopwatch.StartNew();
        var solveStep = await substrate.ExecuteStepAsync(
            new SubstrateStepRequest(runKey, caseDirectory, homeDirectory, capability.Backend, SubstrateStepKind.Solve, StartFromLatestTime: startFromLatestTime),
            ct);
        solveWatch.Stop();

        var resources = new CfdRunResources(0, solveStep.Ranks, solveWatch.Elapsed.TotalMilliseconds, solveStep.CpuSeconds, solveStep.PeakRssBytes);

        if (solveStep.Cancelled || ct.IsCancellationRequested)
        {
            var cancelRes = await ExecuteCancelEscalationAsync(runKey, cancelBoundedWait);
            events = CfdRunStateMachine.TransitionToCancelled(events, clock.GetUtcNow(), "Cancelled by user request; partial outputs retained.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                preSolveMeshGate, null, resources, null, events, [], cancelRes);
        }

        if (capability.Backend == CfdBackendKind.OpenFoamEsi && !string.IsNullOrWhiteSpace(solveStep.StandardOutput))
        {
            var banner = SolverLauncherPolicy.ParseMasterBanner(solveStep.StandardOutput, model.BackendPin.BuildId, !model.BackendPin.VerifiedInstall);
            if (!banner.Passed)
            {
                events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), banner.CauseCode ?? "smoke.banner", banner.Reason);
                return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                    preSolveMeshGate, null, resources, null, events, [], null);
            }
        }

        if (solveStep.ExitCode != 0)
        {
            events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), "CFD-SOLVE-FAILED", $"Solver exited with code {solveStep.ExitCode}.");
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                preSolveMeshGate, null, resources, null, events, [], null);
        }

        // Harvesting & completion oracle (COMMIT-05, RUN-03): exit 0 alone NEVER promotes to Completed!
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, clock.GetUtcNow());
        var harvestWatch = Stopwatch.StartNew();
        HarvestResult harvested;
        try
        {
            harvested = capability.Backend == CfdBackendKind.OpenFoamEsi
                ? CfdOutputHarvester.HarvestOpenFoam(caseDirectory, runId, meshHash, model, solveStep.StandardOutput)
                : CfdOutputHarvester.HarvestSu2(caseDirectory, runId, meshHash, model, solveStep.StandardOutput);
        }
        catch (ContractError err)
        {
            harvestWatch.Stop();
            session.RecordCfdEvent("cfd.harvest", err.Code, harvestWatch.Elapsed.TotalMilliseconds, new CfdTelemetry
            {
                RunKey12 = runKey[..12],
                DurationMs = harvestWatch.Elapsed.TotalMilliseconds,
                Outcome = err.Code
            });
            events = CfdRunStateMachine.TransitionToFailed(events, clock.GetUtcNow(), err.Code, err.Reason ?? Labels.CfdNoOutputs);
            return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
                preSolveMeshGate, null, resources, null, events, [], null);
        }
        harvestWatch.Stop();

        MeshGateResult? finalMeshGate = preSolveMeshGate;
        if (observedMetrics is not null && harvested.YPlusP95 is not null && harvested.YPlusMax is not null)
        {
            var postOpts = (meshGateOptions ?? new MeshGateOptions(model.WallTreatment)) with
            {
                FieldIsStationary = harvested.ResidualSummary.CriterionMet
            };
            finalMeshGate = MeshGateEvaluator.EvaluateMeshGate(
                observedMetrics with { YPlusMin = harvested.YPlusMin, YPlusP95 = harvested.YPlusP95, YPlusMax = harvested.YPlusMax },
                postOpts);
        }

        var cpCheck = CfdOutputHarvester.ValidateCheckpoint(caseDirectory, manifestHash, capability.Backend);
        CfdCheckpointRecord? checkpoint = cpCheck.Valid && cpCheck.TimeDirectory is not null
            ? new CfdCheckpointRecord(cpCheck.TimeDirectory, cpCheck.ManifestHash!, HarvesterValidated: true)
            : null;

        var terminalState = harvested.ResidualSummary.CriterionMet
            ? CfdRunState.Completed
            : CfdRunState.CompletedResidualNotMet;
        events = CfdRunStateMachine.AppendEvent(events, terminalState, clock.GetUtcNow());

        session.RecordCfdEvent("solver.process", terminalState == CfdRunState.Completed ? "completed" : "residual-not-met",
            solveWatch.Elapsed.TotalMilliseconds, new CfdTelemetry
            {
                RunKey12 = runKey[..12],
                Backend = model.BackendPin.Backend,
                Ranks = resources.Ranks,
                DurationMs = resources.WallMs,
                CpuSeconds = resources.CpuSeconds,
                PeakRssBytes = resources.PeakRssBytes,
                YPlusP95 = harvested.YPlusP95,
                OracleOutcome = "outputs-complete",
                CriterionMet = harvested.ResidualSummary.CriterionMet
            });

        session.RecordCfdEvent("cfd.harvest", "ok", harvestWatch.Elapsed.TotalMilliseconds, new CfdTelemetry
        {
            RunKey12 = runKey[..12],
            FilesParsed = harvested.FilesParsed,
            EvidenceRows = harvested.FieldEvidence.Count,
            DurationMs = harvestWatch.Elapsed.TotalMilliseconds,
            Outcome = "ok"
        });

        return PersistTerminal(runId, priorRunId, experimentId, caseIndex, runKey, caseModelHash, meshHash, model,
            finalMeshGate, harvested.ResidualSummary, resources, checkpoint, events, harvested.FieldEvidence, null);
    }

    private async Task<SubstrateCancelResult> ExecuteCancelEscalationAsync(string runKey, TimeSpan boundedWait)
    {
        var cancelRes = await substrate.CancelRunAsync(runKey, boundedWait, CancellationToken.None);
        session.RecordCfdEvent("cfd.cancel", cancelRes.OrphanScan.Clean ? "cancelled" : "orphans-remaining",
            cancelRes.WaitDurationMs, new CfdTelemetry
            {
                RunKey12 = runKey[..12],
                DurationMs = cancelRes.WaitDurationMs,
                OrphansFound = cancelRes.OrphanScan.SurvivingPids.Count,
                Outcome = cancelRes.OrphanScan.Clean ? "cancelled" : "orphans-remaining"
            });
        return cancelRes;
    }

    private CfdPipelineOutcome PersistTerminal(
        string runId,
        string? priorRunId,
        string? experimentId,
        int caseIndex,
        string runKey,
        string caseModelHash,
        string meshHash,
        TypedCaseModel model,
        MeshGateResult? meshGate,
        ResidualSummary? residualSummary,
        CfdRunResources resources,
        CfdCheckpointRecord? checkpoint,
        IReadOnlyList<CfdRunStateEvent> events,
        IReadOnlyList<FieldEvidenceRecord> evidence,
        SubstrateCancelResult? cancelResult)
    {
        var record = CfdRunRecords.Seal(new CfdRunRecord(
            runId, priorRunId, experimentId, caseIndex, runKey, "", caseModelHash, meshHash,
            model.BackendPin, model.Inputs, model.Water, model.Op, model.Reference,
            meshGate, residualSummary, resources, checkpoint, events));

        session.RecordCfdRun(record);
        foreach (var ev in evidence)
            session.RecordFieldEvidence(ev);

        bool outputsComplete = record.CurrentState is CfdRunState.Completed or CfdRunState.CompletedResidualNotMet;
        string? convergedLabel = CfdRunStateMachine.FormatConvergedLabel(residualSummary, outputsComplete);
        return new CfdPipelineOutcome(record, evidence, convergedLabel, cancelResult);
    }
}
