using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track STO (design area3-analysis.md §18.8; ADR-0011): the run key, the cfdw-project-2 document, the store invariants
/// checked in RecordRun, per-run integrity on read, retention tombstones and revision labels. In memory only: the file
/// half (the .v1.bak and the size checks) sits in the Core store checks.
/// </summary>
internal static class RunStoreTests
{
    internal static void Run()
    {
        Check("RunKey_PinnedVector_HexEqual", PinnedVector);
        Check("Project2_RoundTrip_ByteEqual", RoundTrip);
        Check("Project1_NoRun_ByteIdenticalToToday", NoRunByteIdentical);
        Check("Project2_TodaysReader_FailsClosedCopy130", ForwardGuard);
        Check("RecordRun_SameRunIdTwice_Refused", SameRunIdTwice);
        Check("RecordRun_SameKeyTwice_OneCompletedRow", SameKeyTwice);
        Check("RecordRun_StripGap_Refused", StripGap);
        Check("Tamper_EditedStripValue_RunUnavailable", TamperStripValue);
        Check("Tamper_StoredKeySetToCurrent_RunUnavailable", TamperStoredKey);
        Check("Retention_PruneThenUndo_TombstoneReadsPruned", PruneThenUndo);
        Check("RevisionLabel_TwistEdit_OrdinalsAndRail", RevisionLabels);
    }

    // One committed manifest → one committed key. Any change to the JCS form of the key (a member renamed, a value moved
    // to another member, a field dropped or added) turns every saved run Historical after an upgrade; this goes red first.
    private static void PinnedVector()
    {
        var inputs = new RunInputs("00000000-0000-4000-8000-000000000001", new string('1', 64), [new string('2', 64), new string('3', 64)],
            "cfdw-cv/2", "rule-a/1");
        string settingsHash = RunRecord.SettingsHash(Settings());
        Equal("cda4f4ecec9026851db10dde69cbd75bd3bd699d6139980cf96448949c9d31c8", settingsHash, "settings hash");
        Equal("cfad1f85fa9866afd3e7dd4fc5fc7f6a79c5a1ef4be4c699936f13382f6e5808",
            RunRecord.Key(inputs, Water(), Op(2.0), Method(), settingsHash), "run key");
    }

    private static void RoundTrip()
    {
        using var session = Opened();
        session.RecordRun(Completed(session, 2.0, strips: 4));
        session.RecordRun(Failed(session, 3.0));
        byte[] first = session.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(first), "format");
        using var reopened = new AuthoringSession();
        reopened.Reopen(first);
        Equal(true, reopened.ReadRuns().Runs.All(stored => stored.Integrity == RunIntegrity.Intact), "intact after reopen;");
        Equal(2, reopened.ReadRuns().Runs.Count, "runs after reopen");
        byte[] second = reopened.SaveImage();
        Equal(true, first.AsSpan().SequenceEqual(second), "written, read, written bytes equal;");
    }

    // Today's writer, reconstructed: the seven pre-A3a members with the native options. A document with no run must be
    // exactly what it wrote, so every -1 reader, tool and recount sees no change.
    private sealed record TodayEnvelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted,
        CursorRow[] Cursors, RecoveryRow? Recovery);

    private static void NoRunByteIdentical()
    {
        using var session = Opened();
        TwistEdit(session, 1.0);
        byte[] image = session.SaveImage();
        var envelope = session.Envelope();
        var today = new TodayEnvelope("cfdw-project-1", envelope.ProjectId, envelope.Sources, envelope.Designs, envelope.Accepted,
            envelope.Cursors, null);
        byte[] expected = JsonSerializer.SerializeToUtf8Bytes(today, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        Equal("cfdw-project-1", NativeProject.FormatOf(image), "format");
        Equal(false, Encoding.UTF8.GetString(image).Contains("\"analysis\"", StringComparison.Ordinal), "analysis member written;");
        Equal(true, image.AsSpan().SequenceEqual(expected), "bytes equal today's writer;");
    }

    // P-9: the build before A3a is proven by docs/proof/a3a-old-build/ (DOC-UNSUPPORTED-FIELD → COPY-130, file unchanged).
    // This is the forward guard: today's reader takes -1 and -2 only, and a newer document fails closed the same way.
    private static void ForwardGuard()
    {
        using var session = Opened();
        session.RecordRun(Completed(session, 2.0));
        string two = Encoding.UTF8.GetString(session.SaveImage());
        string three = two.Replace("\"format\": \"cfdw-project-2\"", "\"format\": \"cfdw-project-3\"", StringComparison.Ordinal);
        Equal(false, three == two, "format rewritten;");
        RefusedEmpty("DOC-VERSION", three);
        RefusedEmpty("DOC-UNSUPPORTED-FIELD", three.Insert(three.IndexOf('\n') + 1, "  \"future\": 1,\n"));
        // The format is derived from the run count: -1 never carries runs, -2 never comes without them.
        RefusedEmpty("DOC-SCHEMA", two.Replace("\"format\": \"cfdw-project-2\"", "\"format\": \"cfdw-project-1\"", StringComparison.Ordinal));
        using var empty = Opened();
        string one = Encoding.UTF8.GetString(empty.SaveImage());
        RefusedEmpty("DOC-SCHEMA", one.Replace("\"format\": \"cfdw-project-1\"", "\"format\": \"cfdw-project-2\"", StringComparison.Ordinal));
    }

    private static void RefusedEmpty(string code, string image)
    {
        using var reader = new AuthoringSession();
        Refuses(code, () => reader.Reopen(Encoding.UTF8.GetBytes(image)));
        Refuses("DOC-EMPTY", () => reader.Snapshot());
    }

    private static void SameRunIdTwice()
    {
        using var session = Opened();
        var first = Completed(session, 2.0);
        session.RecordRun(first);
        var reused = Completed(session, 3.0, runId: first.RunId);
        Refuses("DOC-RUN-ID", () => session.RecordRun(reused));
        Equal(1, session.ReadRuns().Runs.Count, "rows");
    }

    private static void SameKeyTwice()
    {
        using var session = Opened();
        session.RecordRun(Completed(session, 2.0));
        var again = Completed(session, 2.0);
        Refuses("DOC-RUN-KEY", () => session.RecordRun(again));
        // Failed rows may repeat for one key; they never block a retry.
        session.RecordRun(Failed(session, 2.0));
        session.RecordRun(Failed(session, 2.0));
        var ledger = session.ReadRuns();
        Equal(1, ledger.Runs.Count(stored => stored.Run.Outcome is RunOutcome.Completed && stored.Run.RunKey == again.RunKey), "Completed rows for the key");
        Equal(3, ledger.Runs.Count, "rows");
    }

    private static void StripGap()
    {
        using var session = Opened();
        var run = Completed(session, 2.0, strips: 4);
        var gap = Seal(run with { Strips = [run.Strips[0], run.Strips[1], run.Strips[3]] });
        Refuses("DOC-RUN-STRIPS", () => session.RecordRun(gap));
        var failedWithStrips = Seal(Failed(session, 2.0) with { Strips = run.Strips });
        Refuses("DOC-RUN-STRIPS", () => session.RecordRun(failedWithStrips));
        Equal(0, session.ReadRuns().Runs.Count, "rows");
    }

    private static void TamperStripValue()
    {
        using var session = Opened();
        session.RecordRun(Completed(session, 2.0, strips: 3));
        session.RecordRun(Completed(session, 3.0, strips: 3));
        string image = Encoding.UTF8.GetString(session.SaveImage());
        int at = image.IndexOf("\"fz\": ", StringComparison.Ordinal);
        int end = image.IndexOfAny([',', '\n'], at + 6);
        string tampered = image[..(at + 6)] + "123.25" + image[end..];
        Equal(false, tampered == image, "strip edited;");
        using var reopened = new AuthoringSession();
        reopened.Reopen(Encoding.UTF8.GetBytes(tampered));
        var ledger = reopened.ReadRuns();
        Equal(RunIntegrity.PayloadFailedCheck, ledger.Runs[0].Integrity, "edited run");
        Equal(RunIntegrity.Intact, ledger.Runs[1].Integrity, "untouched run");
        // Never deleted: the tampered row is written back as it was read.
        Equal(true, Encoding.UTF8.GetBytes(tampered).AsSpan().SequenceEqual(reopened.SaveImage()), "tampered row kept;");
    }

    // A forger who sets the stored key to the current inputs' key and recomputes the content hash: only the recomputed
    // key catches it, so the run can never read Current for inputs it was not computed from.
    private static void TamperStoredKey()
    {
        using var session = Opened();
        var run = Completed(session, 2.0);
        session.RecordRun(run);
        string currentKey = Completed(session, 3.0).RunKey;
        string forgedHash = RunRecord.ContentHash(run with { RunKey = currentKey });
        string image = Encoding.UTF8.GetString(session.SaveImage())
            .Replace(run.RunKey, currentKey, StringComparison.Ordinal).Replace(run.ContentHash, forgedHash, StringComparison.Ordinal);
        using var reopened = new AuthoringSession();
        reopened.Reopen(Encoding.UTF8.GetBytes(image));
        var stored = reopened.ReadRuns().Runs.Single();
        Equal(currentKey, stored.Run.RunKey, "stored key");
        Equal(forgedHash, RunRecord.ContentHash(stored.Run), "content hash consistent");
        Equal(RunIntegrity.PayloadFailedCheck, stored.Integrity, "forged key");
    }

    // FM-20: A → B, 21 runs on B, Undo, a different edit abandons B; the save prunes B's oldest run with a tombstone.
    // Undo, then the B edit again: the current inputs' key is the pruned run's key, and it reads "pruned", not missing.
    private static void PruneThenUndo()
    {
        using var session = Opened();
        TwistEdit(session, 1.0);
        var onB = Enumerable.Range(0, AuthoringSession.RetainedOthersPerTier + 1).Select(step => Completed(session, step)).ToArray();
        foreach (var run in onB) session.RecordRun(run);
        session.Undo(Id());
        TwistEdit(session, 2.0);
        _ = session.SaveImage();
        var ledger = session.ReadRuns();
        Equal(AuthoringSession.RetainedOthersPerTier, ledger.Runs.Count, "runs kept");
        Equal(onB[0].RunId, ledger.Pruned.Single().RunId, "tombstone");
        Equal(false, ledger.Runs.Any(stored => stored.Run.RunId == onB[0].RunId), "oldest run pruned;");
        session.Undo(Id());
        TwistEdit(session, 1.0);
        string currentKey = Completed(session, 0).RunKey;
        Equal(onB[0].RunKey, currentKey, "the B edit again reaches the pruned key");
        Equal(true, session.ReadRuns().IsPruned(currentKey), "reads pruned;");
        Equal(false, session.ReadRuns().IsPruned(onB[1].RunKey), "a kept run reads pruned;");
        // A save keeps the tombstone through reopen.
        using var reopened = new AuthoringSession();
        reopened.Reopen(session.SaveImage());
        Equal(true, reopened.ReadRuns().IsPruned(currentKey), "pruned after reopen;");
    }

    private static void RevisionLabels()
    {
        using var session = Opened();
        string opened = session.Snapshot().AcceptedId;
        PointEdit(session, "leading", 1, 0.001);
        string leading = session.Snapshot().AcceptedId;
        TwistEdit(session, 1.0);
        string twist = session.Snapshot().AcceptedId;
        Equal(new RevisionLabel(1, null), session.RevisionOf(opened), "open");
        Equal(new RevisionLabel(2, "leading"), session.RevisionOf(leading), "leading edit");
        Equal(new RevisionLabel(3, "twist"), session.RevisionOf(twist), "twist edit");
        Refuses("DOC-REFERENCE", () => session.RevisionOf(Id()));
    }

    // ---- fixtures ----

    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Id(), true);
        return session;
    }

    private static void TwistEdit(AuthoringSession session, double degrees) => PointEdit(session, "twist", 0, degrees);

    private static void PointEdit(AuthoringSession session, string curve, int index, double delta)
    {
        var snapshot = session.Snapshot();
        var point = Channels.View(snapshot.Source, curve, "Accepted", 0).Points[index];
        var draft = session.BeginPointGesture(Id(), curve, point.Id);
        var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + delta);
        session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));
    }

    private static RunSettings Settings() =>
        new(64, 4, "cosine", "uniform", 20, "freestream", 1e-9, "cfdw.vlm-strip/1", null, [2, 4], "smooth", 0.3);

    private static WaterRecord Water() =>
        new(15, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));

    private static OperatingPoint Op(double alphaDeg) => new(10, 101325, null, "frame-origin", alphaDeg, null);

    private static RunMethod Method() => new("cfdw.vlm-strip", "1.0.0", 1);

    private static AnalysisRun Completed(AuthoringSession session, double alphaDeg, int strips = 2, string? runId = null) =>
        Row(session, alphaDeg, new RunOutcome.Completed(), strips, runId);

    private static AnalysisRun Failed(AuthoringSession session, double alphaDeg) =>
        Row(session, alphaDeg, new RunOutcome.Failed("ANA-SOLVE-RESIDUAL", "residual above 1e-8"), 0, null);

    private static AnalysisRun Row(AuthoringSession session, double alphaDeg, RunOutcome outcome, int strips, string? runId)
    {
        var snapshot = session.Snapshot();
        // Parse only: the profile identities need no geometry assessment (InspectAccepted would certify again per row).
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(assignment => assignment.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var settings = Settings();
        string settingsHash = RunRecord.SettingsHash(settings);
        var water = Water(); var op = Op(alphaDeg); var method = Method();
        var rows = Enumerable.Range(0, strips).Select(j => new StripLoad(j, 0.1 * j, j / (double)Math.Max(1, strips), 0.12 - 0.01 * j,
            0.31 + j, 0.01, 0.03 + alphaDeg, 4.2e5, 0.4, new StripValue(null, "no polar method installed"),
            new StripValue(null, "no polar method installed"), 0.1, 0.2, 40.5 + j, 1.5, -0.25, 0.75, -0.0125)).ToArray();
        var run = new AnalysisRun(runId ?? Id(), RunRecord.Key(inputs, water, op, method, settingsHash), "", outcome, "vlm-strip",
            method, settings, settingsHash, inputs, water, op, new RunReference(0.12, 1.0, 0.12, "frame-origin", "body"), 0.01,
            new RunDiagnostics(1e-13, 42.5), rows, 12.5, new RunPlatform("osx", "arm64", "10.0"));
        return Seal(run);
    }

    private static AnalysisRun Seal(AnalysisRun run) => run with { ContentHash = RunRecord.ContentHash(run) };

    private static void Refuses(string code, Action action)
    {
        try { action(); }
        catch (ContractError error) { Equal(code, error.Code, "refusal code"); return; }
        throw new InvalidOperationException("expected refusal " + code + "; none");
    }
}
