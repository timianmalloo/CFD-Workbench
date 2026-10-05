using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track SVC (design area3-analysis.md §18.8): the service reads the accepted revision, latest-wins cancellation through
/// the evaluation barrier, cancel and close record nothing, the speed rule, and the analysis.run event. The lattice is a
/// fake (<see cref="FakeWing"/>): these checks are about the service, not the numbers (VLM and STP own those).
/// </summary>
internal static class ServiceTests
{
    internal static void Run()
    {
        Check("Analysis_DraftOpen_EvaluatesAcceptedRevision", DraftOpen);
        Check("Evaluate_Supersede_OlderCancelledViaBarrier", Supersede);
        Check("Evaluate_Cancel_NoRowRecorded", CancelRecordsNothing);
        Check("Evaluate_CloseMidCompute_DocClosedNoRow", CloseMidCompute);
        Check("OperatingPoint_SpeedZeroOrNegative_Undefined", SpeedNotPositive);
        Check("Telemetry_AnalysisRun_EmittedWithSubDurations", Telemetry);
        Check("Evaluate_SupersededBeforeCancel_RecordsNothing", SupersededBeforeCancel);
        Check("Evaluate_Supersede_CancelsOlderOutsideTheLock", CancelOutsideTheLock);
        Check("Telemetry_UnexpectedException_OutcomeNotOk", UnexpectedException);
        Check("Evaluate_CancelledBeforeIdempotentHit_Throws", CancelledBeforeHit);
        Check("Evaluate_SameKeyFromTwoServices_ReturnsRecordedRow", SameKeyTwoServices);
        Check("Evaluate_WaterOutsideTable_RefusedNoRow", WaterOutsideTable);
        Check("Evaluate_ComputeFails_FailedRowHasNoDiagnostics", FailedRowWithoutDiagnostics);
        Check("Evaluate_SectionStationsChanged_NewKeyNotAHit", SectionStationsInKey);
        Check("Evaluate_ThrowingCancelCallback_NewerSourceReleased", ThrowingCancelCallbackReleasesNewerSource);
        Check("Evaluate_UncertifiedGeometry_RefusedNotAssessedNoCompute", UncertifiedRefused);
    }

    // Ruling 88 (tip-handling S1): the VLM is reachable only for certified geometry. A closing tip is never certified, and
    // the one way a session holds uncertified geometry is a reopen whose proof ran out of budget (NotAssessed). The
    // service refuses it with the stable code before any compute, and records no row.
    private static void UncertifiedRefused()
    {
        using var session = Fixture.OpenedNotAssessed();
        Equal(GeometryStatus.NotAssessed, session.InspectAccepted().Geometry.Status, "the fixture is uncertified;");
        var wing = new FakeWing();
        var error = Fixture.Throws<ContractError>(new AnalysisService(session, wing)
            .EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None));
        Equal("DSL-NOT-ASSESSED", error.Code, "refusal");
        Equal(true, wing.Seen is null, "the lattice was never given sections;");
        Equal(0, session.ReadRuns().Runs.Count, "rows");
        Equal(true, Fixture.RunEvents(session).Any(item => item.Outcome == "DSL-NOT-ASSESSED"), "an analysis.run event with the code;");
    }

    // A point draft with a moved twist vertex is open; the run reads the accepted bytes, never Draft.Bytes (FM-1, G-1).
    private static void DraftOpen()
    {
        using var session = Fixture.Opened();
        var accepted = session.Snapshot();
        var point = Channels.View(accepted.Source, "twist", "Accepted", 0).Points[0];
        var draft = session.BeginPointGesture(Fixture.Id(), "twist", point.Id);
        session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + 2.0);
        var view = session.Snapshot();
        Equal(true, view.Draft is not null && !view.Draft.Bytes.AsSpan().SequenceEqual(view.Source), "a moved draft is open;");
        var wing = new FakeWing();
        var run = Fixture.Evaluate(new AnalysisService(session, wing), Fixture.Op(2.0));
        Equal(accepted.AcceptedId, run.Inputs.AcceptedId, "accepted id");
        Equal(accepted.SurfaceHash, run.Inputs.SurfaceHash, "surface hash");
        var expected = Placement.Sections(accepted.Source, wing.Settings.SectionEtas!, wing.Settings.SectionXs!, CancellationToken.None);
        Equal(true, Fixture.SamePlacement(expected, wing.Seen!), "the lattice read the accepted sections;");
    }

    // α 2° is held at the barrier; α 3° starts on the same scope and records; the older ends ANA-CANCELLED with no row.
    // The newer cancels the older's token at once (its compute stops per lattice row), not only at the record step.
    private static void Supersede()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var wing = new FakeWing();
        var service = new AnalysisService(session, wing, barrier);
        var older = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        var olderToken = wing.Tokens.First();
        var newer = Fixture.Evaluate(service, Fixture.Op(3.0));
        Equal(true, olderToken.IsCancellationRequested, "the older's compute token cancelled while it is held;");
        Equal(true, barrier.Token.IsCancellationRequested, "the barrier holds the older's cancelled token;");
        barrier.Release();
        Fixture.Throws<OperationCanceledException>(older);
        var runs = session.ReadRuns().Runs;
        Equal(1, runs.Count, "rows");
        Equal(newer.RunId, runs[0].Run.RunId, "the recorded row");
        Equal(3.0, runs[0].Run.Op.AlphaDeg, "recorded α");
        Equal(true, Fixture.RunEvents(session).Any(item => item.Outcome == "ANA-CANCELLED"), "an ANA-CANCELLED analysis.run event;");
    }

    // Cancel while the barrier holds: no row at all, not a Failed row (Cancelled is telemetry, never a row).
    private static void CancelRecordsNothing()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        using var cancel = new CancellationTokenSource();
        var evaluation = new AnalysisService(session, new FakeWing(), barrier)
            .EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), cancel.Token);
        barrier.AwaitHeld();
        cancel.Cancel();
        barrier.Release();
        Fixture.Throws<OperationCanceledException>(evaluation);
        Equal(0, session.ReadRuns().Runs.Count, "rows after cancel");
    }

    // The document closes while the barrier holds: the record is refused DOC-CLOSED and nothing is recorded (FM-16).
    // A closed session has no read, so "no row" is the two halves: none at the hold point (compute wrote nothing), and the
    // one write after it refused.
    private static void CloseMidCompute()
    {
        var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var service = new AnalysisService(session, new FakeWing(), barrier);
        var evaluation = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        Equal(0, session.ReadRuns().Runs.Count, "rows at the hold point, before the close");
        Equal(1, Fixture.InFlight(service).Count, "evaluations in flight at the hold point");
        session.Dispose();
        barrier.Release();
        var error = Fixture.Throws<ContractError>(evaluation);
        Equal("DOC-CLOSED", error.Code, "refusal");
        Equal(0, Fixture.InFlight(service).Count, "evaluations in flight after the refusal (the source is released);");
    }

    // A cancel callback of the older evaluation throws while the newer one supersedes it. The newer evaluation is refused
    // with that exception, and the source it created is neither left in the in-flight map nor left undisposed.
    private static void ThrowingCancelCallbackReleasesNewerSource()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var wing = new FakeWing();
        var service = new AnalysisService(session, wing, barrier);
        var older = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        CancellationTokenSource? newerSource = null;
        using var registration = wing.Tokens.First().Register(() =>
        {
            newerSource = Fixture.InFlight(service)[new Scope.Wing()];
            throw new InvalidOperationException("a cancel callback failed");
        });
        var refused = Fixture.Throws<AggregateException>(service.EvaluateAsync(Fixture.Op(3.0), Fixture.Salt, Tier.VlmStrip,
            new Scope.Wing(), CancellationToken.None));
        Equal("a cancel callback failed", refused.InnerExceptions.Single().Message, "the callback's exception reaches the caller");
        Equal(false, newerSource is null, "the callback saw the newer source in flight;");
        Equal(0, Fixture.InFlight(service).Count, "evaluations in flight after the refusal;");
        Fixture.Throws<ObjectDisposedException>(Task.Run(() => newerSource!.Token.WaitHandle));
        barrier.Release();
        Fixture.Throws<OperationCanceledException>(older);
        Equal(0, session.ReadRuns().Runs.Count, "rows");
    }

    // V = 0 and V = −1 kn: q, Re_ref, Fr_h and σ are Undefined (speed ≤ 0); Evaluate is refused ANA-INPUT-SPEED, no row.
    private static void SpeedNotPositive()
    {
        using var session = Fixture.Opened();
        var service = new AnalysisService(session, new FakeWing());
        foreach (double speed in new[] { 0.0, -1852.0 / 3600.0 })
        {
            var op = OperatingPoints.Custom(speed, 3.0, 0.5);
            var derived = OperatingPoints.Derive(op, Fixture.Salt, 0.12);
            foreach (var (name, value) in new[] { ("q", derived.Q), ("Re_ref", derived.ReRef), ("Fr_h", derived.FroudeDepth), ("σ", derived.Sigma) })
                Equal<DerivedReason?>(DerivedReason.SpeedNotPositive, value.Reason, $"{name} at V = {speed}");
            Equal(0.5 / 0.12, derived.DepthOverChord.Value, "h/c does not depend on speed");
            var error = Fixture.Throws<ContractError>(service.EvaluateAsync(op, Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None));
            Equal("ANA-INPUT-SPEED", error.Code, "refusal");
        }
        Equal(0, session.ReadRuns().Runs.Count, "rows");
        var moving = OperatingPoints.Derive(OperatingPoints.Custom(5.0, 3.0, null), Fixture.Salt, 0.12);
        Equal(0.5 * Fixture.Salt.Rho * 25.0, moving.Q.Value, "q");
        Equal<DerivedReason?>(DerivedReason.DepthNotSet, moving.Sigma.Reason, "σ with depth unset");
    }

    // A completed run emits every sub-duration; a run that fails in its solve emits solveMs and stripMs as not recorded
    // (null), never 0 (IO8); a repeated Evaluate is an idempotent hit returning the stored run.
    private static void Telemetry()
    {
        using var session = Fixture.Opened();
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2.0));
        var ok = Fixture.RunEvents(session).Last();
        Equal("OK", ok.Outcome, "outcome");
        var fields = ok.Analysis!;
        Equal(run.RunKey[..12], fields.RunKey12, "run key (12 hex)");
        Equal("vlm-strip", fields.Tier, "tier");
        Equal(false, fields.IdempotentHit, "idempotent hit");
        Equal(2 * 64 * 4, fields.Unknowns, "unknowns");
        Equal(2, fields.Strips, "strips");
        foreach (var (name, ms) in new[] { ("snapshotMs", fields.SnapshotMs), ("sectionsMs", fields.SectionsMs), ("solveMs", fields.SolveMs),
                     ("stripMs", fields.StripMs), ("recordMs", fields.RecordMs) })
            Equal(true, ms is >= 0, name + " recorded;");
        Equal(true, fields.SnapshotMs + fields.SectionsMs + fields.SolveMs + fields.StripMs + fields.RecordMs <= ok.DurationMilliseconds,
            "sub-durations within the total;");

        var again = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2.0));
        Equal(run.RunId, again.RunId, "idempotent run");
        Equal(true, Fixture.RunEvents(session).Last().Analysis!.IdempotentHit, "idempotent hit emitted;");

        var failed = Fixture.Evaluate(new AnalysisService(session, new FakeWing { FailCode = "ANA-SOLVE-SINGULAR" }), Fixture.Op(4.0));
        Equal(true, failed.Outcome is RunOutcome.Failed { Code: "ANA-SOLVE-SINGULAR" }, "a Failed row;");
        var refused = Fixture.RunEvents(session).Last();
        Equal("ANA-SOLVE-SINGULAR", refused.Outcome, "failed outcome");
        Equal<double?>(null, refused.Analysis!.SolveMs, "solveMs not reached");
        Equal<double?>(null, refused.Analysis.StripMs, "stripMs not reached");
        Equal(true, refused.Analysis.SectionsMs is >= 0, "sectionsMs reached;");
        Equal(2, session.ReadRuns().Runs.Count, "rows: one Completed (the hit adds none) and one Failed");
    }

    // The window between a newer evaluation's swap and its Cancel: the older is no longer the scope's generation but its
    // token is not cancelled yet. Only the identity check stops it recording. No public call stops a thread inside that
    // window, so the check stages the newer's swap directly under the service lock.
    private static void SupersededBeforeCancel()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var service = new AnalysisService(session, new FakeWing(), barrier);
        var older = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        using var newer = new CancellationTokenSource();
        lock (Fixture.ServiceLock(service)) Fixture.InFlight(service)[new Scope.Wing()] = newer;
        barrier.Release();
        Fixture.Throws<OperationCanceledException>(older);
        Equal(0, session.ReadRuns().Runs.Count, "rows");
    }

    // Cancel runs the older's cancellation callbacks synchronously on the newer's thread. They run arbitrary code (the
    // method's, the compute's), so they must not run while the service lock is held.
    private static void CancelOutsideTheLock()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var wing = new FakeWing();
        var service = new AnalysisService(session, wing, barrier);
        var older = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        object gate = Fixture.ServiceLock(service);
        bool? heldAtCancel = null;
        using var registration = wing.Tokens.First().Register(() => heldAtCancel = Monitor.IsEntered(gate));
        Fixture.Evaluate(service, Fixture.Op(3.0));
        barrier.Release();
        Fixture.Throws<OperationCanceledException>(older);
        Equal<bool?>(false, heldAtCancel, "the service lock held while the older's callbacks ran");
    }

    // An exception outside the ANA contract propagates and analysis.run says so; it never reads "OK" (IO8).
    private static void UnexpectedException()
    {
        using var session = Fixture.Opened();
        var service = new AnalysisService(session, new FakeWing { Unexpected = new InvalidOperationException("a fault outside the ANA contract") });
        Fixture.Throws<InvalidOperationException>(service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(),
            CancellationToken.None));
        Equal("ANA-UNEXPECTED", Fixture.RunEvents(session).Last().Outcome, "outcome");
        Equal(0, session.ReadRuns().Runs.Count, "rows");
    }

    // The token is cancelled once the compute has started (the first clock read on the worker); a Completed row with the
    // key is stored. The hit must not hand back a run for an evaluation that was cancelled.
    private static void CancelledBeforeHit()
    {
        using var session = Fixture.Opened();
        Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2.0));
        using var cancel = new CancellationTokenSource();
        var clock = new CancelOnWorker(cancel);
        var service = new AnalysisService(session, new FakeWing(), time: clock);
        Fixture.Throws<OperationCanceledException>(service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), cancel.Token));
        Equal(true, clock.Fired, "cancelled inside the compute;");
        Equal("ANA-CANCELLED", Fixture.RunEvents(session).Last().Outcome, "outcome");
        Equal(1, session.ReadRuns().Runs.Count, "rows");
    }

    // Two services on one session (two views) evaluate one key. The first is held after compute; the second records.
    // The first then finds the Completed row under its lock and returns it instead of being refused DOC-RUN-KEY.
    private static void SameKeyTwoServices()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var first = new AnalysisService(session, new FakeWing(), barrier)
            .EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        var second = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2.0));
        barrier.Release();
        var late = first.GetAwaiter().GetResult();
        Equal(second.RunId, late.RunId, "the held evaluation returns the recorded row");
        Equal(1, session.ReadRuns().Runs.Count, "rows");
        Equal(true, Fixture.RunEvents(session).Last().Analysis!.IdempotentHit, "the late hit emitted as a hit;");
    }

    // Water outside the table (temperature, salinity) or not a table record (a non-finite or non-positive property) is
    // refused before compute, ANA-INPUT-WATER, with nothing recorded (design §8).
    private static void WaterOutsideTable()
    {
        using var session = Fixture.Opened();
        var service = new AnalysisService(session, new FakeWing());
        foreach (var (name, water) in new[]
                 {
                     ("60 °C", Fixture.Salt with { TemperatureC = 60 }), ("−1 °C", Fixture.Salt with { TemperatureC = -1 }),
                     ("salinity 40 g/kg", Fixture.Salt with { SalinityGPerKg = 40 }), ("ρ NaN", Fixture.Salt with { Rho = double.NaN }),
                     ("ν 0", Fixture.Salt with { Nu = 0 }), ("p_v ∞", Fixture.Salt with { Pv = double.PositiveInfinity })
                 })
        {
            var error = Fixture.Throws<ContractError>(service.EvaluateAsync(Fixture.Op(2.0), water, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None));
            Equal("ANA-INPUT-WATER", error.Code, name);
        }
        Equal(0, session.ReadRuns().Runs.Count, "rows");
        Equal(true, Fixture.Evaluate(service, Fixture.Op(2.0), Fixture.Fresh).Outcome is RunOutcome.Completed, "fresh water at 15 °C evaluates;");
    }

    // A Failed row has no diagnostics: not when the solve failed (nothing measured), and not when the coupling failed after
    // the solve measured them. The stored row writes no member, and it reads back intact (IO8: absent, never a zero).
    private static void FailedRowWithoutDiagnostics()
    {
        using var session = Fixture.Opened();
        var solveFailed = Fixture.Evaluate(new AnalysisService(session, new FakeWing { FailCode = "ANA-SOLVE-SINGULAR" }), Fixture.Op(4.0));
        var coupleFailed = Fixture.Evaluate(new AnalysisService(session, new FakeWing { CoupleFailCode = "ANA-NONFINITE" }), Fixture.Op(5.0));
        foreach (var (name, run) in new[] { ("solve failed", solveFailed), ("coupling failed", coupleFailed) })
        {
            Equal(true, run.Outcome is RunOutcome.Failed, name + ": a Failed row;");
            Equal<RunDiagnostics?>(null, run.Diagnostics, name + ": diagnostics");
        }
        Equal(true, Fixture.RunEvents(session).Last().Analysis!.Residual is not null, "the trace keeps the residual the solve measured;");
        string image = System.Text.Encoding.UTF8.GetString(session.SaveImage());
        Equal(false, image.Contains("\"diagnostics\"", StringComparison.Ordinal), "a diagnostics member written for a Failed row;");
        using var reopened = new AuthoringSession();
        reopened.Reopen(System.Text.Encoding.UTF8.GetBytes(image));
        Equal(true, reopened.ReadRuns().Runs.All(stored => stored.Integrity == RunIntegrity.Intact && stored.Run.Diagnostics is null),
            "both rows intact and without diagnostics after reopen;");
    }

    // The stations the method samples are settings, so they are in the key. FakeWing samples 3 η stations while its
    // settings say 64 span panels: a method sampling 5 stations must not be handed the 3-station run as an idempotent hit.
    // Settings with no stations are refused before compute.
    private static void SectionStationsInKey()
    {
        using var session = Fixture.Opened();
        var coarse = new FakeWing();
        var run = Fixture.Evaluate(new AnalysisService(session, coarse), Fixture.Op(2.0));
        var finer = new FakeWing { Settings = coarse.Settings with { SectionEtas = [0, 0.25, 0.5, 0.75, 1] } };
        var again = Fixture.Evaluate(new AnalysisService(session, finer), Fixture.Op(2.0));
        Equal(false, run.RunKey == again.RunKey, "the 5-station run has the 3-station key;");
        Equal(5, finer.Seen?.Count, "sections the finer method was given");
        Equal(2, session.ReadRuns().Runs.Count(stored => stored.Run.Outcome is RunOutcome.Completed), "Completed rows");
        foreach (var settings in new[] { coarse.Settings with { SectionEtas = null }, coarse.Settings with { SectionXs = [] },
                     coarse.Settings with { SectionEtas = [0, 1.5] } })
        {
            var error = Fixture.Throws<ContractError>(new AnalysisService(session, new FakeWing { Settings = settings })
                .EvaluateAsync(Fixture.Op(3.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None));
            Equal("ANA-INPUT-STATIONS", error.Code, "refusal");
        }
        Equal(2, session.ReadRuns().Runs.Count, "rows after the refusals");
    }
}

/// <summary>The shared fixture of the SVC checks: an opened session, its water, operating points and edits.</summary>
internal static class Fixture
{
    internal static readonly WaterRecord Salt =
        new(15, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));
    internal static readonly WaterRecord Fresh =
        new(15, 0, 999.1026, 1.13858e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('a', 64));

    internal static string Id() => Guid.NewGuid().ToString("D");

    internal static OperatingPoint Op(double alphaDeg) => OperatingPoints.Custom(5.14444, alphaDeg, 0.5);

    // Opening certifies the geometry (about 70 ms); every check starts from one opened image instead.
    private static readonly Lazy<byte[]> OpenedImage = new(() =>
    {
        using var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Id(), true);
        return session.SaveImage();
    });

    internal static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Reopen(OpenedImage.Value);
        return session;
    }

    // The same opened image, reopened with a proof work limit of 0: carried in as NotAssessed / GEOMETRY-BUDGET.
    internal static AuthoringSession OpenedNotAssessed()
    {
        var session = AuthoringSession.WithProofWorkLimit(0);
        session.Reopen(OpenedImage.Value);
        return session;
    }

    internal static AnalysisRun Evaluate(AnalysisService service, OperatingPoint op, WaterRecord? water = null) =>
        service.EvaluateAsync(op, water ?? Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None).GetAwaiter().GetResult();

    internal static CurrentInputs Current(AuthoringSession session, OperatingPoint op, WaterRecord? water = null, FakeWing? wing = null)
    {
        wing ??= new FakeWing();
        return Freshness.Current(session.Snapshot(), water ?? Salt, op, wing.Method, wing.Settings);
    }

    internal static RunState StateOf(AuthoringSession session, AnalysisRun run, CurrentInputs current) =>
        Freshness.State(session.ReadRuns().Runs.Single(stored => stored.Run.RunId == run.RunId), current);

    internal static void TwistEdit(AuthoringSession session, double degrees)
    {
        var point = Channels.View(session.Snapshot().Source, "twist", "Accepted", 0).Points[0];
        var draft = session.BeginPointGesture(Id(), "twist", point.Id);
        var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + degrees);
        session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));
    }

    internal static IEnumerable<SessionEvent> RunEvents(AuthoringSession session) =>
        session.ReadLocalEvents().Where(item => item.Operation == "analysis.run");

    internal static T Throws<T>(Task task) where T : Exception
    {
        try { task.GetAwaiter().GetResult(); }
        catch (T expected) { return expected; }
        throw new InvalidOperationException("expected " + typeof(T).Name + "; the evaluation returned");
    }

    internal static bool SamePlacement(IReadOnlyList<SectionSample> expected, IReadOnlyList<SectionSample> actual) =>
        expected.Count == actual.Count && expected.Zip(actual).All(pair => pair.First.PlacedCamber.SequenceEqual(pair.Second.PlacedCamber));

    // The service's lock and in-flight map, read only by the two latest-wins checks that observe or stage a state no public
    // call reaches deterministically. A renamed field fails the check loudly, never passes it.
    internal static object ServiceLock(AnalysisService service) => Field(service, "sync");

    internal static Dictionary<Scope, CancellationTokenSource> InFlight(AnalysisService service) =>
        (Dictionary<Scope, CancellationTokenSource>)Field(service, "inFlight");

    private static object Field(AnalysisService service, string name) =>
        typeof(AnalysisService).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service)
        ?? throw new InvalidOperationException("AnalysisService has no field " + name);
}

/// <summary>A clock that cancels its source on the first read from a worker thread: the compute has started.</summary>
internal sealed class CancelOnWorker(CancellationTokenSource source) : TimeProvider
{
    private int fired;
    public bool Fired => Volatile.Read(ref fired) == 1;

    public override long GetTimestamp()
    {
        if (Thread.CurrentThread.IsThreadPoolThread && Interlocked.Exchange(ref fired, 1) == 0) source.Cancel();
        return base.GetTimestamp();
    }
}

/// <summary>
/// The fake wing method: fixed Γ, w_T and two strips, the default lattice settings, and the sections it was given.
/// <see cref="FailCode"/> makes its solve fail with that code.
/// </summary>
internal sealed class FakeWing : IWingMethod
{
    public RunMethod Method { get; init; } = new("cfdw.vlm-strip", "1.0.0", 1);
    public RunSettings Settings { get; init; } = new(64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3,
        SectionEtas: [0, 0.5, 1], SectionXs: [0, 0.25, 0.5, 0.75, 1]);
    public double ReconciliationTolerance => 0.01;
    public string? FailCode { get; init; }
    /// <summary>Makes the coupling fail with that code, after the solve measured its diagnostics.</summary>
    public string? CoupleFailCode { get; init; }
    /// <summary>An exception outside the ANA contract, thrown by the solve.</summary>
    public Exception? Unexpected { get; init; }
    public IReadOnlyList<SectionSample>? Seen { get; private set; }
    /// <summary>The compute token of each solve, in call order.</summary>
    public System.Collections.Concurrent.ConcurrentQueue<CancellationToken> Tokens { get; } = new();

    public RunReference Reference(byte[] source) => new(0.108, 0.9, 0.12, "frame origin", "body; wind for lift/drag");

    public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
    {
        Seen = sections;
        Tokens.Enqueue(cancellation);
        if (Unexpected is not null) throw Unexpected;
        if (FailCode is not null) throw new ContractError(FailCode, "the fake lattice failed");
        return new LatticeSolution([0.31, 0.29], [-0.012, -0.013], new RunDiagnostics(1e-13, 42.5));
    }

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
        WaterRecord water, CancellationToken cancellation) => CoupleFailCode is not null
        ? throw new ContractError(CoupleFailCode, "the fake coupling failed")
        : Enumerable.Range(0, solution.Gamma.Count).Select(j => new StripLoad(j, 0.1 * j, 0.5 * j, 0.12, solution.Gamma[j], 0.01,
            0.03 + op.AlphaDeg, 4.2e5, 0.4, new StripValue(null, "no polar method installed"), new StripValue(null, "no polar method installed"),
            0.1, 0.2, 40.5, 1.5, -0.25, 0.75, solution.DownwashTrefftz[j])).ToArray();
}

/// <summary>Holds the first evaluation that reaches it until released; every later one passes.</summary>
internal sealed class HoldFirst : IEvaluationBarrier
{
    private readonly TaskCompletionSource held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int calls;

    /// <summary>The token the held evaluation passed in.</summary>
    public CancellationToken Token { get; private set; }

    public ValueTask ComputedAsync(OperatingPoint op, CancellationToken cancellation)
    {
        if (Interlocked.Increment(ref calls) != 1) return ValueTask.CompletedTask;
        Token = cancellation;
        held.SetResult();
        return new ValueTask(released.Task);
    }

    public void AwaitHeld()
    {
        if (!held.Task.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("no evaluation reached the barrier");
    }

    public void Release() => released.SetResult();
}
