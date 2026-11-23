using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Red-first pure zero-I/O Core tests for Area 6 CandidateLadder, COMMIT-01 promotion evidence,
/// Pareto extraction, and BuildGeometryDraft / BuildRebasedDraft Rebase guard
/// (docs/design/results-and-candidates.md §5, §9.1).
/// </summary>
internal static class CandidateLadderTests
{
    private static readonly string SurfaceHash1 = new('a', 64);
    private static readonly string SurfaceHash2 = new('b', 64);
    private static readonly string DiscrepancyKey1 = new('c', 64);
    private static readonly string DiscrepancyKey2 = new('d', 64);
    private static readonly string PinDigest = new('e', 64);
    private static readonly string TableHash = new('f', 64);
    private static readonly string CaseModelHash = new('1', 64);
    private static readonly string MeshHash = new('2', 64);

    internal static void Run()
    {
        Check("CandidateLadder_PromotionEvidence_And_BuildGeometryDraftRebaseGuard", CandidateLadder_PromotionEvidence_And_BuildGeometryDraftRebaseGuard);
        Check("CandidateLadder_ParetoAndZeroFeasibleTerminalReason", CandidateLadder_ParetoAndZeroFeasibleTerminalReason);
    }

    private static StoredCfdRun MakeCompletedCfdRun(string acceptedId, string surfaceHash)
    {
        string runId = Guid.NewGuid().ToString("D");
        var inputs = new RunInputs(acceptedId, surfaceHash, [new string('3', 64)], "cfdw-cv/2", Placement.PlacementRuleVersion);
        var water = new WaterRecord(15.0, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC", TableHash);
        var op = new OperatingPoint(7.0, 101325.0, 0.5, "root LE", 4.0, null);
        var reference = new RunReference(0.108, 0.9, 0.12, "frame origin", "body; wind for lift/drag");
        var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "build-1", PinDigest, true);
        string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, CaseModelHash);
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, now)];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, now);

        var run = CfdRunRecords.Seal(new CfdRunRecord(
            RunId: runId,
            PriorRunId: null,
            ExperimentId: null,
            CaseIndex: 0,
            RunKey: runKey,
            ContentHash: "",
            CaseModelHash: CaseModelHash,
            MeshHash: MeshHash,
            BackendPin: pin,
            Inputs: inputs,
            Water: water,
            Op: op,
            Reference: reference,
            MeshGate: null,
            ResidualSummary: new ResidualSummary(4e-4, 0.45, 0.017, -0.04, 2e-4, 5e-5, 1e-3, 2e-4, 25, 0, false, true),
            Resources: new CfdRunResources(500_000, 4, 1200, 45, 256_000_000),
            Checkpoint: null,
            Events: events));
        return new StoredCfdRun(run, CfdRecordIntegrity.Intact);
    }

    private static CandidateRecord MakeCandidate(
        string baseAcceptedId,
        string baseSurfaceHash,
        string status,
        string tier,
        bool feasible,
        double ld,
        double cd) =>
        CfdRunRecords.Seal(new CandidateRecord(
            CandidateId: Guid.NewGuid().ToString("D"),
            ParentCandidateId: null,
            ContentHash: "",
            ExperimentId: Guid.NewGuid().ToString("D"),
            EvaluationId: "eval-01",
            BaseAcceptedId: baseAcceptedId,
            BaseSurfaceHash: baseSurfaceHash,
            CandidateAcceptedId: null,
            Status: status,
            Feasible: feasible,
            Tier: tier,
            Objectives:
            [
                new CandidateObjectiveRecord("L/D", "dimensionless", ld),
                new CandidateObjectiveRecord("CD", "dimensionless", cd)
            ],
            Constraints:
            [
                new CandidateConstraintRecord("cavitation_margin", "%", 18.5, 15.0, feasible)
            ],
            PromotionRunIds: [],
            DiscrepancyKeys: [DiscrepancyKey1],
            ParameterDeltas:
            [
                new CandidateParameterDelta("twist", "cv-2", -0.75, "deg"),
                new CandidateParameterDelta("trailing", "cv-4", 0.008, "m")
            ]));

    private static void CandidateLadder_PromotionEvidence_And_BuildGeometryDraftRebaseGuard()
    {
        string baseRevId = Guid.NewGuid().ToString("D");
        string movedRevId = Guid.NewGuid().ToString("D");

        var cand = MakeCandidate(baseRevId, SurfaceHash1, "vlm-checked", "vlm-strip", true, ld: 26.4, cd: 0.0170);
        var runOp1 = MakeCompletedCfdRun(baseRevId, SurfaceHash1);
        var runOp2 = MakeCompletedCfdRun(baseRevId, SurfaceHash1);

        // 1. Missing 1 operating point in multipoint set -> Promotion MUST be refused naming the missing point.
        var missingOne = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            Candidate: cand,
            TargetStatus: "cfd-checked",
            RequiredOperatingPointIds: ["takeoff", "cruise"],
            CompletedTargetRunsByPointId: new Dictionary<string, StoredCfdRun> { ["takeoff"] = runOp1 },
            DiscrepancyKeysByPointId: new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1, ["cruise"] = DiscrepancyKey2 },
            ObjectiveGainSurvived: true));

        Equal(false, missingOne.Admitted);
        Equal("CAND-PROMOTION-INCOMPLETE", missingOne.RefusalCode);
        Equal(1, missingOne.MissingOperatingPoints.Count);
        Equal("cruise", missingOne.MissingOperatingPoints[0]);
        if (missingOne.RefusalReason is null || !missingOne.RefusalReason.Contains("cruise", StringComparison.Ordinal))
            throw new InvalidOperationException($"Refusal reason must name missing point 'cruise': {missingOne.RefusalReason}");

        // 2. Missing DiscrepancyRecord key -> Promotion MUST be refused.
        var missingDisc = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            Candidate: cand,
            TargetStatus: "cfd-checked",
            RequiredOperatingPointIds: ["takeoff", "cruise"],
            CompletedTargetRunsByPointId: new Dictionary<string, StoredCfdRun> { ["takeoff"] = runOp1, ["cruise"] = runOp2 },
            DiscrepancyKeysByPointId: new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1 },
            ObjectiveGainSurvived: true));
        Equal(false, missingDisc.Admitted);
        Equal("CAND-PROMOTION-INCOMPLETE", missingDisc.RefusalCode);

        // 3. Objective gain failed to survive at CFD tier -> Promotion refused.
        var noGain = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            Candidate: cand,
            TargetStatus: "cfd-checked",
            RequiredOperatingPointIds: ["takeoff", "cruise"],
            CompletedTargetRunsByPointId: new Dictionary<string, StoredCfdRun> { ["takeoff"] = runOp1, ["cruise"] = runOp2 },
            DiscrepancyKeysByPointId: new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1, ["cruise"] = DiscrepancyKey2 },
            ObjectiveGainSurvived: false));
        Equal(false, noGain.Admitted);
        Equal("CAND-GAIN-NOT-SURVIVED", noGain.RefusalCode);

        // 4. All multipoint Completed Intact runs + DiscrepancyKeys + survived gain -> Admitted to cfd-checked.
        var promoted = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            Candidate: cand,
            TargetStatus: "cfd-checked",
            RequiredOperatingPointIds: ["takeoff", "cruise"],
            CompletedTargetRunsByPointId: new Dictionary<string, StoredCfdRun> { ["takeoff"] = runOp1, ["cruise"] = runOp2 },
            DiscrepancyKeysByPointId: new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1, ["cruise"] = DiscrepancyKey2 },
            ObjectiveGainSurvived: true));
        Equal(true, promoted.Admitted);
        Equal("cfd-checked", promoted.NextStatus);

        // 5. BuildGeometryDraft when base AcceptedId matches -> emits typed draft without mutating geometry.
        var draft = CandidateLadder.BuildGeometryDraft(cand, baseRevId, SurfaceHash1);
        Equal(true, draft.AcceptEnabled);
        Equal((string?)null, draft.RefusalReason);
        Equal(false, draft.IsRebased);
        Equal((double?)null, draft.RebaseDeviationMeters);
        Equal(2, draft.ParameterDeltas.Count);

        // 6. Base-revision moved guard -> direct Accept strictly disabled with exact Rebase guidance.
        var blocked = CandidateLadder.BuildGeometryDraft(cand, movedRevId, SurfaceHash2);
        Equal(false, blocked.AcceptEnabled);
        Equal(0, blocked.ParameterDeltas.Count);
        Equal(
            $"Accept disabled — base revision moved ({SurfaceHash1[..12]}). Use Rebase to preview over the current revision.",
            blocked.RefusalReason);

        // 7. BuildRebasedDraft explicitly reports measured geometric deviation over the moved revision.
        var rebased = CandidateLadder.BuildRebasedDraft(cand, movedRevId, SurfaceHash2, measuredDeviationMeters: 0.00042);
        Equal(true, rebased.AcceptEnabled);
        Equal(true, rebased.IsRebased);
        Equal(movedRevId, rebased.TargetAcceptedId);
        Equal(SurfaceHash2, rebased.TargetSurfaceHash);
        Equal(0.00042, rebased.RebaseDeviationMeters);
        Equal(2, rebased.ParameterDeltas.Count);

        // 8. Negative/non-finite rebase deviation refused; skipping a ladder rung refused; tampered run refused.
        Refuses("RES-REBASE-DEVIATION", () => CandidateLadder.BuildRebasedDraft(cand, movedRevId, SurfaceHash2, -0.001));
        var skipRung = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            cand, "experimentally-compared", ["takeoff"],
            new Dictionary<string, StoredCfdRun> { ["takeoff"] = runOp1 },
            new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1 },
            true));
        Equal(false, skipRung.Admitted);
        Equal("CAND-PROMOTION-ORDER", skipRung.RefusalCode);

        var tamperedRun = runOp1 with { Integrity = CfdRecordIntegrity.PayloadFailedCheck };
        var tamperedEval = CandidateLadder.EvaluatePromotion(new PromotionEvidenceInput(
            cand, "cfd-checked", ["takeoff"],
            new Dictionary<string, StoredCfdRun> { ["takeoff"] = tamperedRun },
            new Dictionary<string, string> { ["takeoff"] = DiscrepancyKey1 },
            true));
        Equal(false, tamperedEval.Admitted);
        Equal("CAND-PROMOTION-INCOMPLETE", tamperedEval.RefusalCode);
    }

    private static void CandidateLadder_ParetoAndZeroFeasibleTerminalReason()
    {
        string revId = Guid.NewGuid().ToString("D");
        var cDominated = MakeCandidate(revId, SurfaceHash1, "vlm-checked", "vlm-strip", true, ld: 20.0, cd: 0.022);
        var cParetoA = MakeCandidate(revId, SurfaceHash1, "vlm-checked", "vlm-strip", true, ld: 28.0, cd: 0.019);
        var cParetoB = MakeCandidate(revId, SurfaceHash1, "cfd-checked", "cfd", true, ld: 25.0, cd: 0.015);
        var cInfeasible = MakeCandidate(revId, SurfaceHash1, "surrogate-candidate", "surrogate", false, ld: 35.0, cd: 0.010);

        var ladder = CandidateLadder.RankCandidates(
            [
                new StoredCandidate(cDominated, CfdRecordIntegrity.Intact),
                new StoredCandidate(cParetoA, CfdRecordIntegrity.Intact),
                new StoredCandidate(cParetoB, CfdRecordIntegrity.Intact),
                new StoredCandidate(cInfeasible, CfdRecordIntegrity.Intact)
            ],
            currentAcceptedId: revId,
            currentSurfaceHash: SurfaceHash1);

        Equal((string?)null, ladder.EmptyReason);
        Equal(2, ladder.ParetoCandidateIds.Count);
        if (!ladder.ParetoCandidateIds.Contains(cParetoA.CandidateId) || !ladder.ParetoCandidateIds.Contains(cParetoB.CandidateId))
            throw new InvalidOperationException("Pareto front must contain exactly the two non-dominated feasible candidates.");
        if (ladder.ParetoCandidateIds.Contains(cDominated.CandidateId) || ladder.ParetoCandidateIds.Contains(cInfeasible.CandidateId))
            throw new InvalidOperationException("Dominated and infeasible candidates must not be on Pareto front.");

        // Zero feasible candidates -> displays only experiment's terminal reason (RES-05).
        var emptyLadder = CandidateLadder.RankCandidates(
            [new StoredCandidate(cInfeasible, CfdRecordIntegrity.Intact)],
            currentAcceptedId: revId,
            currentSurfaceHash: SurfaceHash1,
            experimentTerminalReason: "All 12 evaluated designs violated cavitation margin < 15 %.");
        Equal("All 12 evaluated designs violated cavitation margin < 15 %.", emptyLadder.EmptyReason);
        Equal(0, emptyLadder.ParetoCandidateIds.Count);
    }
}
