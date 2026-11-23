using System.Text;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Red-first pure Core tests for additive cfdw-project-2 cfd envelope (docs/design/cfd-run-pipeline.md §2, §9):
/// round-trip serialization, BLAKE3(JCS) tamper detection without deletion, byte-identical legacy -1 and pre-W10 -2
/// documents, DOC-UNSUPPORTED-FIELD fail-closed guard, tau_w separation persistence invariant, and CAND-01 promotion guard.
/// Strictly zero-I/O (recursive STORE-SUBSET verified).
/// </summary>
internal static class CfdRunEnvelopeTests
{
    private static readonly DateTimeOffset FixedUtc = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    internal static void Run()
    {
        Check("CfdEnvelope_RoundTrip_TamperDetection_And_ByteIdenticalLegacy", RoundTrip_Tamper_And_ByteIdentical);
        Check("CfdEnvelope_PersistenceInvariants_SeparationTauW_And_Cand01", PersistenceInvariants);
        Check("CfdEnvelope_CadCommitPreflight_AccountsForCfdAndRetainsRecords", CadCommitPreflight_AccountsForCfd);
    }

    private static void RoundTrip_Tamper_And_ByteIdentical()
    {
        using var session = OpenDefault();

        // 1. Empty cfd preserves cfdw-project-1 byte-identically (no "cfd" member written).
        byte[] v1 = session.SaveImage();
        Equal("cfdw-project-1", NativeProject.FormatOf(v1));
        string v1Text = Encoding.UTF8.GetString(v1);
        Equal(false, v1Text.Contains("\"cfd\"", StringComparison.Ordinal));

        // Verify RunRecord.Format 4-argument overload & backward-compatible defaults.
        Equal("cfdw-project-1", RunRecord.Format(0));
        Equal("cfdw-project-1", RunRecord.Format(0, 0, false, false));
        Equal("cfdw-project-2", RunRecord.Format(0, 0, false, true));

        // 2. Record complete cfd: { runs, fieldEvidence, candidates } -> format becomes cfdw-project-2.
        var cfdRun = MakeCompletedCfdRun(session);
        session.RecordCfdRun(cfdRun);

        var evidence = CfdRunRecords.Seal(new FieldEvidenceRecord(
            EvidenceId: Id(),
            RunId: cfdRun.RunId,
            ContentHash: "",
            Kind: "surface",
            RelativePath: "postProcessing/wingSurface/500/wing.raw",
            ArtifactHash: new string('f', 64),
            MeshHash: cfdRun.MeshHash!,
            Fields: ["Cp", "Cf", "wallShearStress"],
            Association: "face",
            Ranges: [new FieldRangeRecord("Cp", "dimensionless", -1.84, 1.00, "sample-range")],
            ColormapPolicyVersion: "crameri-8.0",
            ReductionTool: "cfdw-harvester",
            ReductionToolVersion: "1.0.0",
            HasWallShearStress: true,
            SeparationCriterion: "skin-friction-line convergence",
            HasTurbulentKineticEnergy: false,
            IsTransient: false,
            Timesteps: []));
        session.RecordFieldEvidence(evidence);

        var snapshot = session.Snapshot();
        var candidate = CfdRunRecords.Seal(new CandidateRecord(
            CandidateId: Id(),
            ParentCandidateId: null,
            ContentHash: "",
            ExperimentId: Id(),
            EvaluationId: "eval-001",
            BaseAcceptedId: snapshot.AcceptedId,
            BaseSurfaceHash: snapshot.SurfaceHash,
            CandidateAcceptedId: null,
            Status: "cfd-checked",
            Feasible: true,
            Tier: "cfd",
            Objectives: [new CandidateObjectiveRecord("L/D", "dimensionless", 22.4)],
            Constraints: [new CandidateConstraintRecord("cavitation-margin", "ratio", 1.32, 1.15, true)],
            PromotionRunIds: [cfdRun.RunId],
            DiscrepancyKeys: [new string('9', 64)],
            ParameterDeltas: [new CandidateParameterDelta("trailing", "cv-3", -0.005, "m")]));
        session.RecordCandidate(candidate);

        byte[] v2 = session.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(v2));
        string v2Text = Encoding.UTF8.GetString(v2);
        Equal(true, v2Text.Contains("\"cfd\"", StringComparison.Ordinal));
        Equal(true, v2Text.Contains("\"fieldEvidence\"", StringComparison.Ordinal));
        Equal(true, v2Text.Contains("\"candidates\"", StringComparison.Ordinal));

        // 3. Round-trip reopen preserves exact bytes and reports Intact across all three lists.
        using (var reopened = new AuthoringSession())
        {
            reopened.Reopen(v2);
            var ledger = reopened.ReadCfdLedger();
            Equal(1, ledger.Runs.Count);
            Equal(CfdRecordIntegrity.Intact, ledger.Runs[0].Integrity);
            Equal(1, ledger.FieldEvidence.Count);
            Equal(CfdRecordIntegrity.Intact, ledger.FieldEvidence[0].Integrity);
            Equal(1, ledger.Candidates.Count);
            Equal(CfdRecordIntegrity.Intact, ledger.Candidates[0].Integrity);
            Equal(true, reopened.SaveImage().AsSpan().SequenceEqual(v2));
        }

        // 4. Tamper 1 byte in run's clWindowMean -> reads PayloadFailedCheck without crashing or deleting!
        string tamperedRunText = v2Text.Replace("0.412", "0.912", StringComparison.Ordinal);
        Equal(false, tamperedRunText == v2Text);
        using (var tampered = new AuthoringSession())
        {
            tampered.Reopen(Encoding.UTF8.GetBytes(tamperedRunText));
            var ledger = tampered.ReadCfdLedger();
            Equal(1, ledger.Runs.Count);
            Equal(CfdRecordIntegrity.PayloadFailedCheck, ledger.Runs[0].Integrity);
            Equal(CfdRecordIntegrity.Intact, ledger.FieldEvidence[0].Integrity);
            Equal(CfdRecordIntegrity.Intact, ledger.Candidates[0].Integrity);
        }

        // 5. Tamper 1 byte in fieldEvidence colormapPolicyVersion -> PayloadFailedCheck without deleting.
        string tamperedEvText = v2Text.Replace("crameri-8.0", "crameri-9.0", StringComparison.Ordinal);
        using (var tampered = new AuthoringSession())
        {
            tampered.Reopen(Encoding.UTF8.GetBytes(tamperedEvText));
            var ledger = tampered.ReadCfdLedger();
            Equal(CfdRecordIntegrity.Intact, ledger.Runs[0].Integrity);
            Equal(CfdRecordIntegrity.PayloadFailedCheck, ledger.FieldEvidence[0].Integrity);
        }

        // 6. Unknown top-level member still fails closed with DOC-UNSUPPORTED-FIELD; empty "cfd" fails DOC-SCHEMA.
        string unknownTop = v2Text.Insert(v2Text.IndexOf('\n') + 1, "  \"unknownFuture\": 1,\n");
        using (var reader = new AuthoringSession())
            Refuses("DOC-UNSUPPORTED-FIELD", () => reader.Reopen(Encoding.UTF8.GetBytes(unknownTop)));

        string emptyCfd = v1Text.Replace("\"format\": \"cfdw-project-1\"", "\"format\": \"cfdw-project-2\"", StringComparison.Ordinal);
        int lastBrace = emptyCfd.LastIndexOf('}');
        string withEmptyCfd = emptyCfd[..lastBrace].TrimEnd() + ",\n  \"cfd\": { \"runs\": [], \"fieldEvidence\": [], \"candidates\": [] }\n}\n";
        using (var reader = new AuthoringSession())
            Refuses("DOC-SCHEMA", () => reader.Reopen(Encoding.UTF8.GetBytes(withEmptyCfd)));
    }

    private static void PersistenceInvariants()
    {
        using var session = OpenDefault();
        var run = MakeCompletedCfdRun(session);
        session.RecordCfdRun(run);
        var snapshot = session.Snapshot();

        // VIZ separation rule enforced at persistence level: HasWallShearStress == false MUST have SeparationCriterion == null!
        var illegalSeparation = CfdRunRecords.Seal(new FieldEvidenceRecord(
            Id(), run.RunId, "", "isosurface", "postProcessing/q/500/q.raw",
            new string('1', 64), run.MeshHash!, ["Q"], "face",
            [new FieldRangeRecord("Q", "1/s^2", 0, 50, "sample-range")],
            "crameri-8.0", "cfdw-harvester", "1.0.0",
            HasWallShearStress: false,
            SeparationCriterion: "Q-criterion",
            HasTurbulentKineticEnergy: false, IsTransient: false, Timesteps: []));
        Refuses("DOC-SCHEMA", () => session.RecordFieldEvidence(illegalSeparation));

        // CAND-01: cfd-checked candidate requires non-empty PromotionRunIds referencing existing Completed CFD runs.
        var badCandEmpty = CfdRunRecords.Seal(new CandidateRecord(
            Id(), null, "", Id(), "eval-bad", snapshot.AcceptedId, snapshot.SurfaceHash, null,
            "cfd-checked", true, "cfd", [], [], PromotionRunIds: [], DiscrepancyKeys: [], ParameterDeltas: []));
        Refuses("CAND-01", () => session.RecordCandidate(badCandEmpty));

        var badCandMissingRun = CfdRunRecords.Seal(new CandidateRecord(
            Id(), null, "", Id(), "eval-bad2", snapshot.AcceptedId, snapshot.SurfaceHash, null,
            "cfd-checked", true, "cfd", [], [], PromotionRunIds: [Id()], DiscrepancyKeys: [], ParameterDeltas: []));
        Refuses("CAND-01", () => session.RecordCandidate(badCandMissingRun));

        // Duplicate runId refused with DOC-CFD-ID.
        Refuses("DOC-CFD-ID", () => session.RecordCfdRun(run));
    }

    private static void CadCommitPreflight_AccountsForCfd()
    {
        // Measure baseline size with 1 CFD run vs without CFD run, then verify CAD Commit preflight counts Cfd!
        int bytesWithoutCfd;
        int bytesWithCfd;
        using (var probe = OpenDefault())
        {
            bytesWithoutCfd = probe.SaveImage().Length;
            probe.RecordCfdRun(MakeCompletedCfdRun(probe));
            bytesWithCfd = probe.SaveImage().Length;
        }
        Equal(true, bytesWithCfd > bytesWithoutCfd + 1000);

        // Tight cap: admits open + RecordCfdRun, but NOT an additional CAD edit when Cfd is counted in Commit's Preflight.
        // If Commit omitted Cfd from prospective, Preflight would under-count by (bytesWithCfd - bytesWithoutCfd) and wrongly pass!
        int tightCap = bytesWithCfd + 50;
        using (var tight = new AuthoringSession(tightCap))
        {
            tight.Open(FoilSource.NewDefault(), Id(), true);
            tight.RecordCfdRun(MakeCompletedCfdRun(tight));
            var point = Channels.View(tight.Snapshot().Source, "twist", "Accepted", 0).Points[0];
            var draft = tight.BeginPointGesture(Id(), "twist", point.Id);
            var frame = tight.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + 1.0);
            var assessment = tight.Validate(frame.Draft.Id, frame.Draft.Generation);
            Refuses("DOC-SIZE", () => tight.Apply(Id(), assessment));
        }

        // Normal cap: CAD Commit after RecordCfdRun succeeds, retains Cfd records intact, and keeps cfdw-project-2!
        using var normal = OpenDefault();
        normal.RecordCfdRun(MakeCompletedCfdRun(normal));
        var pt = Channels.View(normal.Snapshot().Source, "twist", "Accepted", 0).Points[0];
        var d = normal.BeginPointGesture(Id(), "twist", pt.Id);
        var f = normal.UpdatePointGesture(d.Id, d.Generation, pt.SpanMeters, pt.Ordinate + 1.0);
        normal.Apply(Id(), normal.Validate(f.Draft.Id, f.Draft.Generation));
        byte[] saved = normal.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(saved));
        Equal(1, normal.ReadCfdLedger().Runs.Count);
        Equal(CfdRecordIntegrity.Intact, normal.ReadCfdLedger().Runs[0].Integrity);
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession OpenDefault()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Id(), true);
        return session;
    }

    internal static CfdRunRecord MakeCompletedCfdRun(AuthoringSession session)
    {
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var model = CfdRunModelTests.SampleCase() with { Inputs = inputs };
        string runId = Id();
        string caseModelHash = model.ComputeCaseModelHash();
        string runKey = model.ComputeCaseKey();
        string meshHash = model.ComputeMeshHash();
        var gate = MeshGateEvaluator.EvaluateMeshGate(
            CfdRunModelTests.GoodMetrics(),
            new MeshGateOptions(WallTreatmentKind.WallResolved, MeshKind.UnstructuredOpenFoam, FieldIsStationary: true));
        var residual = new ResidualSummary(4.2e-4, 0.412, 0.0184, -0.041, 1.8e-4, 4.5e-5, 1e-3, 2e-4, 200, 0, false, true);
        var resources = new CfdRunResources(1_840_000, 4, 142_300.0, 512.4, 2_147_483_648L);
        var checkpoint = new CfdCheckpointRecord("500", new string('7', 64), true);
        var events = new[]
        {
            new CfdRunStateEvent(0, CfdRunState.Pending, FixedUtc),
            new CfdRunStateEvent(1, CfdRunState.Readiness, FixedUtc.AddSeconds(1)),
            new CfdRunStateEvent(2, CfdRunState.Meshing, FixedUtc.AddSeconds(2)),
            new CfdRunStateEvent(3, CfdRunState.MeshGate, FixedUtc.AddSeconds(10)),
            new CfdRunStateEvent(4, CfdRunState.Solving, FixedUtc.AddSeconds(12)),
            new CfdRunStateEvent(5, CfdRunState.Harvesting, FixedUtc.AddSeconds(20)),
            new CfdRunStateEvent(6, CfdRunState.Completed, FixedUtc.AddSeconds(22))
        };
        return CfdRunRecords.Seal(new CfdRunRecord(
            runId, null, null, 0, runKey, "", caseModelHash, meshHash,
            model.BackendPin, model.Inputs, model.Water, model.Op, model.Reference,
            gate, residual, resources, checkpoint, events));
    }
}
