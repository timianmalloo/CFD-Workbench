using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Node SWP-1 red-first tests (T4–T9 in docs/design/local-sweeps.md §8, Ruling 271):
/// sweep store immutability, cfdw-project-2 runless format + .v1.bak, byte-identity when empty,
/// fail-closed unknown fields + old-build proof fixture, tamper detection, active retention pinning &amp; Archive tombstones,
/// OperatingPoint.Load positive guard, and GoalStateSweepSnapshot seam.
/// </summary>
internal static class SweepStoreTests
{
    private static readonly DateTimeOffset FixedUtc = new(2026, 11, 22, 4, 0, 0, TimeSpan.Zero);

    internal static void Run()
    {
        Check("SweepRecord_ImmutableOnceQueued_EditRefused", T4_ImmutableOnceQueued);
        Check("SweepStore_RunlessExperiment_FormatProject2AndV1Bak", T5_RunlessFormatProject2AndV1Bak);
        Check("SweepStore_OmittedWhenEmpty_ExistingBytesIdentical", T6_OmittedWhenEmptyByteIdentical);
        Check("SweepStore_OldBuildReader_FailsClosedCopy130", T7_OldBuildAndForwardGuard);
        Check("SweepStore_TamperedDefinitionOrRun_ReadsUnavailable", T8_TamperedDefinitionOrRunUnavailable);
        Check("SweepRetention_ActivePinsSelected_ArchiveUnpinsAndTombstones", T9_RetentionPinAndArchiveTombstones);
        Check("SweepStore_OperatingPointLoadAndGoalSeam_Enforced", LoadAndGoalSeam);
    }

    // T4 (XS-03): Appending Queued seals definition/cases/pin; attempting to edit a Queued experiment or append attempts
    // before Queued fails closed (DOC-SCHEMA). Mutant: allowing RecordSweepAttempt on a Draft experiment.
    private static void T4_ImmutableOnceQueued()
    {
        using var session = OpenDefault();
        var draft = MakeCartesianExperiment(session, speeds: [5.0, 7.0], angles: [0.0, 2.0], queued: false);
        session.RecordExperiment(draft);

        // Editing a Draft before Queue is allowed.
        var renamed = draft with { Name = "Renamed draft" };
        session.UpdateDraftExperiment(renamed);
        Equal("Renamed draft", session.ReadExperiments().Single().Experiment.Name);

        // Appending an attempt BEFORE Queued MUST fail closed (XS-03).
        var earlyAttempt = MakeFailedAttempt(0, 1, null, "ANA-TEST", "premature");
        Refuses("DOC-SCHEMA", () => session.RecordSweepAttempt(draft.Id, earlyAttempt));

        // Recording a Draft that already carries attempts MUST fail closed.
        var forgedDraft = MakeCartesianExperiment(session, speeds: [5.0], angles: [0.0], queued: false) with
        {
            Attempts = [earlyAttempt]
        };
        Refuses("DOC-SCHEMA", () => session.RecordExperiment(forgedDraft));

        // Queueing freezes BackendPin and seals the experiment.
        var pin = PinFor(draft.Definition);
        session.AppendSweepEvent(draft.Id, new ExperimentEvent(0, SweepEventKind.Queued, FixedUtc), pin);
        var queued = session.ReadExperiments().Single().Experiment;
        Equal(true, queued.IsQueued);
        Equal(pin, queued.BackendPin);

        // Editing a Queued experiment MUST fail closed (DOC-SCHEMA).
        Refuses("DOC-SCHEMA", () => session.UpdateDraftExperiment(queued with { Name = "Illegal edit" }));
        // Queueing twice MUST fail closed.
        Refuses("DOC-SCHEMA", () => session.AppendSweepEvent(draft.Id, new ExperimentEvent(1, SweepEventKind.Queued, FixedUtc), pin));

        // After Queue, recording a Failed attempt with runId == null succeeds, while Failed with non-null runId is refused.
        Refuses("DOC-SCHEMA", () => session.RecordSweepAttempt(draft.Id, earlyAttempt with { RunId = Id() }));
        session.RecordSweepAttempt(draft.Id, earlyAttempt);
        Equal(1, session.ReadExperiments().Single().Experiment.Attempts.Count);
    }

    // T5 (DR-SWP-1, ADR-0011): Queueing a 12-case sweep (0 runs yet) produces cfdw-project-2 with experiments and
    // analysis == null and round-trips byte-identically; on-disk .v1.bak transition is verified under umask in
    // ProjectStoreTests.Backup_V1Bak_RunlessSweep_FormatProject2 (STORE-SUBSET).
    private static void T5_RunlessFormatProject2AndV1Bak()
    {
        using var session = OpenDefault();
        byte[] v1 = session.SaveImage();
        Equal("cfdw-project-1", NativeProject.FormatOf(v1));

        // Queue a 12-case sweep with 0 runs recorded.
        var exp = MakeCartesianExperiment(session, speeds: [5.144444444444445, 7.202222222222222, 9.26], angles: [0.0, 2.0, 4.0, 6.0], queued: true);
        session.RecordExperiment(exp);
        Equal(0, session.ReadRuns().Runs.Count);

        byte[] v2 = session.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(v2));
        string v2Text = Encoding.UTF8.GetString(v2);
        Equal(false, v2Text.Contains("\"analysis\"", StringComparison.Ordinal));
        Equal(true, v2Text.Contains("\"experiments\"", StringComparison.Ordinal));

        // Round-trip reopen preserves all 12 cases and bytes identically.
        using var reopened = new AuthoringSession();
        reopened.Reopen(v2);
        var stored = reopened.ReadExperiments().Single();
        Equal(SweepIntegrity.Intact, stored.Integrity);
        Equal(12, stored.Experiment.Cases.Count);
        Equal(true, reopened.SaveImage().AsSpan().SequenceEqual(v2));
    }

    // T6 (DR-SWP-1, ADR-0011): Existing -1 and -2 documents without experiments serialize byte-identically (no "experiments" member).
    private static void T6_OmittedWhenEmptyByteIdentical()
    {
        using var session = OpenDefault();
        byte[] v1 = session.SaveImage();
        Equal("cfdw-project-1", NativeProject.FormatOf(v1));
        Equal(false, Encoding.UTF8.GetString(v1).Contains("\"experiments\"", StringComparison.Ordinal));

        var run = SampleRun(session, 5.0, 2.0, strips: 3);
        session.RecordRun(run);
        byte[] v2 = session.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(v2));
        Equal(false, Encoding.UTF8.GetString(v2).Contains("\"experiments\"", StringComparison.Ordinal));
        Equal(true, Encoding.UTF8.GetString(v2).Contains("\"analysis\"", StringComparison.Ordinal));

        // Verify unified RunRecord.Format overload (DR-SWP-1).
        Equal("cfdw-project-1", RunRecord.Format(0));
        Equal("cfdw-project-1", RunRecord.Format(0, 0, false));
        Equal("cfdw-project-2", RunRecord.Format(1, 0, false));
        Equal("cfdw-project-2", RunRecord.Format(0, 1, false));
        Equal("cfdw-project-2", RunRecord.Format(0, 0, true));
    }

    // T7 (ADR-0011 §6, Ruling 271 DR-SWP-1): Staged fail-closed admission ("setup" still refused with DOC-UNSUPPORTED-FIELD
    // until Wave C, unknown top-level members refused, format/content mismatches refused), and committed old-build proof fixture
    // read via PlacementTests.RepoRoot() (zero relative "docs/..." reads).
    private static void T7_OldBuildAndForwardGuard()
    {
        using var session = OpenDefault();
        var exp = MakeCartesianExperiment(session, speeds: [5.0, 7.0], angles: [0.0, 2.0], queued: true);
        session.RecordExperiment(exp);
        string two = Encoding.UTF8.GetString(session.SaveImage());

        // Unknown top-level fields (including Wave C's unparsed "setup" in Wave B) fail closed with DOC-UNSUPPORTED-FIELD.
        string withSetup = two.Insert(two.IndexOf('\n') + 1, "  \"setup\": {},\n");
        RefusedEmpty("DOC-UNSUPPORTED-FIELD", withSetup);
        string withFuture = two.Insert(two.IndexOf('\n') + 1, "  \"future\": 1,\n");
        RefusedEmpty("DOC-UNSUPPORTED-FIELD", withFuture);

        // Format mismatch: cfdw-project-1 carrying experiments is refused with DOC-SCHEMA.
        string badV1 = two.Replace("\"format\": \"cfdw-project-2\"", "\"format\": \"cfdw-project-1\"", StringComparison.Ordinal);
        RefusedEmpty("DOC-SCHEMA", badV1);

        // Empty experiments array is refused with DOC-SCHEMA.
        using var empty = OpenDefault();
        string one = Encoding.UTF8.GetString(empty.SaveImage());
        string emptyArr = one.Replace("\"format\": \"cfdw-project-1\"", "\"format\": \"cfdw-project-2\"", StringComparison.Ordinal);
        int lastBrace = emptyArr.LastIndexOf('}');
        string withEmptyExperiments = emptyArr[..lastBrace].TrimEnd() + ",\n  \"experiments\": []\n}\n";
        RefusedEmpty("DOC-SCHEMA", withEmptyExperiments);

        // Committed old-build proof fixture read strictly through PlacementTests.RepoRoot() (check-test-paths.py).
        string proofPath = Path.Combine(PlacementTests.RepoRoot(), "docs", "proof", "swp-old-build", "one-sweep.cfdw.json");
        byte[] fixtureBytes = File.ReadAllBytes(proofPath);
        string shaBefore = Identity.Sha256(fixtureBytes);
        using var reader = new AuthoringSession();
        reader.Reopen(fixtureBytes);
        Equal("cfdw-project-2", NativeProject.FormatOf(fixtureBytes));
        Equal(1, reader.ReadExperiments().Count);
        Equal(SweepIntegrity.Intact, reader.ReadExperiments()[0].Integrity);
        Equal(shaBefore, Identity.Sha256(File.ReadAllBytes(proofPath)));
    }

    // T8 (ADR-0011 §4): Flipping 1 byte in definition.anglesDeg or a referenced run's strip makes the sweep / run
    // read PayloadFailedCheck (Unavailable) without deleting it.
    private static void T8_TamperedDefinitionOrRunUnavailable()
    {
        using var session = OpenDefault();
        var exp = MakeCartesianExperiment(session, speeds: [5.0], angles: [0.0, 2.0], queued: true);
        session.RecordExperiment(exp);
        var run0 = SampleRun(session, 5.0, 0.0, strips: 3);
        session.RecordRun(run0);
        session.RecordSweepAttempt(exp.Id, MakeCompletedAttempt(0, 1, null, run0.RunId, 12.5));

        string original = Encoding.UTF8.GetString(session.SaveImage());

        // 1. Tamper 1 byte in definition.anglesDeg (2 -> 3) without updating definitionHash.
        int anglesPos = original.IndexOf("\"anglesDeg\"", StringComparison.Ordinal);
        int twoPos = original.IndexOf("2", anglesPos, StringComparison.Ordinal);
        Equal(true, twoPos > anglesPos);
        string tamperedDef = original[..twoPos] + "3" + original[(twoPos + 1)..];
        using (var reopened = new AuthoringSession())
        {
            reopened.Reopen(Encoding.UTF8.GetBytes(tamperedDef));
            var storedExp = reopened.ReadExperiments().Single();
            Equal(SweepIntegrity.PayloadFailedCheck, storedExp.Integrity);
            // Tampered experiment is kept, never deleted.
            Equal(1, reopened.ReadExperiments().Count);
        }

        // 2. Tamper 1 byte in the referenced run's strip fz value.
        int fzPos = original.IndexOf("\"fz\": ", StringComparison.Ordinal);
        int fzEnd = original.IndexOfAny([',', '\n'], fzPos + 6);
        string tamperedRun = original[..(fzPos + 6)] + "999.75" + original[fzEnd..];
        using (var reopened = new AuthoringSession())
        {
            reopened.Reopen(Encoding.UTF8.GetBytes(tamperedRun));
            Equal(SweepIntegrity.Intact, reopened.ReadExperiments().Single().Integrity);
            Equal(RunIntegrity.PayloadFailedCheck, reopened.ReadRuns().Runs.Single().Integrity);
            // Tampered run is preserved on re-save.
            Equal(1, reopened.ReadRuns().Runs.Count);
        }
    }

    // T9 (DR-SWP-2, Ruling 263): 12-case VLM sweep on an unreachable branch survives SaveImage() with all 12 runs intact;
    // superseded retry attempt is not pinned; appending Archived then SaveImage() prunes unreachable runs into PrunedRun tombstones.
    private static void T9_RetentionPinAndArchiveTombstones()
    {
        using var session = OpenDefault();
        TwistEdit(session, 1.0); // Branch B

        double[] speeds = [5.0, 7.0, 9.0];
        double[] angles = [0.0, 2.0, 4.0, 6.0];
        var exp = MakeCartesianExperiment(session, speeds, angles, queued: true);
        session.RecordExperiment(exp);

        // Case 0 gets an initial completed run (attempt 1) that is superseded by a retry (attempt 2, with a different runId via load/different alpha?
        // Actually same operating point has 1 Completed run per runKey; to test superseded attempt unpinning cleanly,
        // use an estimator or a second run? Wait: a completed run has unique runKey per operating point, so a retry of a Completed case
        // can only occur if attempt 1 Failed (no runId) OR on a Goal-state case where attempt 2 solves a slightly refined alpha!
        // Let's test BOTH: 12 completed sweep runs on Branch B + a goal-state-style superseded completed run on a second sweep!).
        var sweepRunIds = new List<string>();
        for (int i = 0; i < exp.Cases.Count; i++)
        {
            var c = exp.Cases[i];
            var run = SampleRun(session, c.SpeedMetersPerSecond, c.AlphaDeg!.Value, strips: 2);
            session.RecordRun(run);
            session.RecordSweepAttempt(exp.Id, MakeCompletedAttempt(i, 1, null, run.RunId, 10.0));
            sweepRunIds.Add(run.RunId);
        }

        // Also record a goal-state sweep on Branch B whose case 0 has attempt 1 (completed at alpha 2.0) superseded by attempt 2 (completed at alpha 2.05).
        var goalSweep = MakeGoalExperiment(session, speed: 6.0, targetLoadN: 700.0);
        session.RecordExperiment(goalSweep);
        var supersededRun = SampleRun(session, 6.0, 2.0, strips: 2, load: 700.0);
        var selectedRetryRun = SampleRun(session, 6.0, 2.05, strips: 2, load: 700.0);
        session.RecordRun(supersededRun);
        session.RecordRun(selectedRetryRun);
        var att1 = MakeCompletedAttempt(0, 1, null, supersededRun.RunId, 11.0, new SolvedAlphaRecord(2.0, -4.0, 14.0, 3, 5, "vlm-strip"));
        var att2 = MakeCompletedAttempt(0, 2, att1.AttemptId, selectedRetryRun.RunId, 11.0, new SolvedAlphaRecord(2.05, -4.0, 14.0, 3, 5, "vlm-strip"));
        session.RecordSweepAttempt(goalSweep.Id, att1);
        session.RecordSweepAttempt(goalSweep.Id, att2);

        // Add 25 extra non-sweep runs on Branch B AFTER the sweep runs so the sweep runs fall outside the RetainedOthersPerTier = 20 window.
        for (int k = 0; k < AuthoringSession.RetainedOthersPerTier + 5; k++)
            session.RecordRun(SampleRun(session, 11.0 + k * 0.25, 1.0, strips: 1));

        // Abandon Branch B (Undo to A, then a different edit to C).
        session.Undo(Id());
        TwistEdit(session, 2.0);

        // SaveImage #1: all 12 selected runs of active exp + selectedRetryRun of goalSweep MUST survive (pinned on top of 20 quota),
        // while supersededRun (outside last-20 window and NOT selected) MUST be pruned!
        byte[] savedActive = session.SaveImage();
        var ledger1 = session.ReadRuns();
        foreach (string runId in sweepRunIds)
            Equal(true, ledger1.Runs.Any(r => r.Run.RunId == runId));
        Equal(true, ledger1.Runs.Any(r => r.Run.RunId == selectedRetryRun.RunId));
        Equal(false, ledger1.Runs.Any(r => r.Run.RunId == supersededRun.RunId));
        Equal(true, ledger1.Pruned.Any(p => p.RunId == supersededRun.RunId));

        // Reopen with active sweeps succeeds cleanly.
        using (var checkActive = new AuthoringSession())
        {
            checkActive.Reopen(savedActive);
            Equal(2, checkActive.ReadExperiments().Count);
        }

        // Archive the 12-case sweep, then SaveImage #2: its 12 unreachable runs MUST now be pruned into PrunedRun tombstones!
        session.AppendSweepEvent(exp.Id, new ExperimentEvent(exp.Events.Count, SweepEventKind.Archived, FixedUtc, "reclaim storage"));
        byte[] savedArchived = session.SaveImage();
        var ledger2 = session.ReadRuns();
        foreach (string runId in sweepRunIds)
        {
            Equal(false, ledger2.Runs.Any(r => r.Run.RunId == runId));
            Equal(true, ledger2.Pruned.Any(p => p.RunId == runId));
        }
        // Archived sweep stays intact and document reopens cleanly with PrunedRun tombstones!
        using var reopenedArchived = new AuthoringSession();
        reopenedArchived.Reopen(savedArchived);
        var archivedExp = reopenedArchived.ReadExperiments().Single(e => e.Experiment.Id == exp.Id);
        Equal(SweepIntegrity.Intact, archivedExp.Integrity);
        Equal(true, archivedExp.Experiment.IsArchived);
        Equal(12, archivedExp.Experiment.Cases.Count);
        Equal(12, archivedExp.Experiment.Attempts.Count);
    }

    // Ruling 271 DR-SWP-4 (b) & GoalStateSweepSnapshot seam.
    private static void LoadAndGoalSeam()
    {
        using var session = OpenDefault();
        Equal<GoalStateSweepSnapshot?>(null, session.CurrentGoalSweepSnapshot());

        // Positive Load accepted by Core RunRecord.CheckStore.
        var positiveLoad = SampleRun(session, 7.0, 2.5, strips: 2, load: 735.75);
        session.RecordRun(positiveLoad);
        Equal(735.75, session.ReadRuns().Runs.Single().Run.Op.Load);

        // Zero or negative Load refused by Core RunRecord.CheckStore (DOC-SCHEMA).
        Refuses("DOC-SCHEMA", () => session.RecordRun(SampleRun(session, 7.0, 3.0, strips: 2, load: 0.0)));
        Refuses("DOC-SCHEMA", () => session.RecordRun(SampleRun(session, 7.0, 3.5, strips: 2, load: -10.0)));
    }

    // ---- Helpers ----

    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession OpenDefault()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Id(), true);
        return session;
    }

    private static void TwistEdit(AuthoringSession session, double deltaDeg)
    {
        var snapshot = session.Snapshot();
        var point = Channels.View(snapshot.Source, "twist", "Accepted", 0).Points[0];
        var draft = session.BeginPointGesture(Id(), "twist", point.Id);
        var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + deltaDeg);
        session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));
    }

    private static void RefusedEmpty(string code, string image)
    {
        using var reader = new AuthoringSession();
        Refuses(code, () => reader.Reopen(Encoding.UTF8.GetBytes(image)));
        Refuses("DOC-EMPTY", () => reader.Snapshot());
    }

    private static RunSettings DefaultSettings() =>
        new(64, 4, "cosine", "uniform", 20, "freestream", 1e-9, "cfdw.vlm-strip/1", null, [2, 4], "smooth", 0.3);

    private static WaterRecord DefaultWater() =>
        new(15, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));

    private static RunMethod DefaultMethod() => new("cfdw.vlm-strip", "1.1.0", 1);

    private static BackendPin PinFor(SweepDefinition def) =>
        new(SweepRecords.LocalBackendId, SweepRecords.LocalSubstrate, def.Tier, def.Method.Id, def.Method.Version, def.SettingsHash);

    internal static SweepExperiment MakeCartesianExperiment(AuthoringSession session, double[] speeds, double[] angles, bool queued)
    {
        var snapshot = session.Snapshot();
        var settings = DefaultSettings();
        string settingsHash = RunRecord.SettingsHash(settings);
        var water = DefaultWater();
        var method = DefaultMethod();
        var def = new SweepDefinition(
            SweepRecords.SourceCartesian, SweepRecords.TierVlmStrip,
            new SweepDesignRef(snapshot.AcceptedId, snapshot.SurfaceHash), null,
            water, 0.5, "root LE", 101325.0, speeds, angles, method, settingsHash);
        var cases = new List<SweepCase>();
        int idx = 0;
        foreach (double s in speeds.OrderBy(v => v))
            foreach (double a in angles.OrderBy(v => v))
                cases.Add(new SweepCase(idx++, SweepRecords.CaseKey(s, 0.5, "root LE", 101325.0, a, null), null, s, 0.5, "root LE", 101325.0, a, null));
        var events = queued ? new[] { new ExperimentEvent(0, SweepEventKind.Queued, FixedUtc) } : Array.Empty<ExperimentEvent>();
        var exp = new SweepExperiment(Id(), Id(), 1, null, SweepRecords.KindSweep, "Baseline sweep",
            "", def, queued ? PinFor(def) : null, cases, events, []);
        return SweepRecords.Seal(exp);
    }

    private static SweepExperiment MakeGoalExperiment(AuthoringSession session, double speed, double targetLoadN)
    {
        var snapshot = session.Snapshot();
        var settings = DefaultSettings();
        string settingsHash = RunRecord.SettingsHash(settings);
        var water = DefaultWater();
        var method = DefaultMethod();
        var def = new SweepDefinition(
            SweepRecords.SourceGoalState, SweepRecords.TierVlmStrip,
            new SweepDesignRef(snapshot.AcceptedId, snapshot.SurfaceHash),
            new SweepGoalStateRef(Id(), new string('b', 64)),
            water, 0.5, "root LE", 101325.0, [speed], [], method, settingsHash);
        var c = new SweepCase(0, SweepRecords.CaseKey(speed, 0.5, "root LE", 101325.0, null, targetLoadN), "P1 Take-off",
            speed, 0.5, "root LE", 101325.0, null, targetLoadN);
        var exp = new SweepExperiment(Id(), Id(), 1, null, SweepRecords.KindSweep, "Goal sweep",
            "", def, PinFor(def), [c], [new ExperimentEvent(0, SweepEventKind.Queued, FixedUtc)], []);
        return SweepRecords.Seal(exp);
    }

    private static SweepAttempt MakeFailedAttempt(int caseIndex, int attemptIndex, string? priorAttemptId, string code, string reason) =>
        new(Id(), caseIndex, attemptIndex, priorAttemptId, null, null, 5.0,
            [
                new AttemptStateEvent(0, SweepAttemptState.Pending, FixedUtc),
                new AttemptStateEvent(1, SweepAttemptState.Running, FixedUtc),
                new AttemptStateEvent(2, SweepAttemptState.Failed, FixedUtc, code, reason)
            ]);

    private static SweepAttempt MakeCompletedAttempt(int caseIndex, int attemptIndex, string? priorAttemptId, string runId, double wallMs, SolvedAlphaRecord? solvedAlpha = null) =>
        new(Id(), caseIndex, attemptIndex, priorAttemptId, runId, solvedAlpha, wallMs,
            [
                new AttemptStateEvent(0, SweepAttemptState.Pending, FixedUtc),
                new AttemptStateEvent(1, SweepAttemptState.Running, FixedUtc),
                new AttemptStateEvent(2, SweepAttemptState.Completed, FixedUtc)
            ]);

    private static AnalysisRun SampleRun(AuthoringSession session, double speed, double alphaDeg, int strips, double? load = null)
    {
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var settings = DefaultSettings();
        string settingsHash = RunRecord.SettingsHash(settings);
        var water = DefaultWater();
        var op = new OperatingPoint(speed, 101325.0, 0.5, "root LE", alphaDeg, load);
        var method = DefaultMethod();
        var unavailable = new StripValue(null, "no polar method installed");
        var rows = Enumerable.Range(0, strips).Select(j => new StripLoad(j, 0.1 * j, j / (double)Math.Max(1, strips), 0.12,
            0.3 + j * 0.01, 0.01, 0.03, 4.2e5, 0.4, unavailable, unavailable,
            0.1, 0.2, 40.5 + j, 1.5, -0.25, 0.75, -0.0125)).ToArray();
        var run = new AnalysisRun(Id(), RunRecord.Key(inputs, water, op, method, settingsHash), "", new RunOutcome.Completed(),
            "vlm-strip", method, settings, settingsHash, inputs, water, op, new RunReference(0.12, 1.0, 0.12, "frame origin", "body"), 0.01,
            rows, 12.5, new RunPlatform("win", "arm64", "10.0"), new RunDiagnostics(1e-13, 42.5));
        return run with { ContentHash = RunRecord.ContentHash(run) };
    }
}
