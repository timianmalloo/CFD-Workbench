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
        var expected = Placement.Sections(accepted.Source, wing.Etas, wing.Xs, CancellationToken.None);
        Equal(true, Fixture.SamePlacement(expected, wing.Seen!), "the lattice read the accepted sections;");
    }

    // α 2° is held at the barrier; α 3° starts on the same scope and records; the older ends ANA-CANCELLED with no row.
    private static void Supersede()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var service = new AnalysisService(session, new FakeWing(), barrier);
        var older = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        var newer = Fixture.Evaluate(service, Fixture.Op(3.0));
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
    private static void CloseMidCompute()
    {
        var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var evaluation = new AnalysisService(session, new FakeWing(), barrier)
            .EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        session.Dispose();
        barrier.Release();
        var error = Fixture.Throws<ContractError>(evaluation);
        Equal("DOC-CLOSED", error.Code, "refusal");
        Equal(true, evaluation.IsFaulted, "no run returned;");
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
}

/// <summary>
/// The fake wing method: fixed Γ, w_T and two strips, the default lattice settings, and the sections it was given.
/// <see cref="FailCode"/> makes its solve fail with that code.
/// </summary>
internal sealed class FakeWing : IWingMethod
{
    public RunMethod Method { get; init; } = new("cfdw.vlm-strip", "1.0.0", 1);
    public RunSettings Settings { get; init; } = new(64, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3);
    public double ReconciliationTolerance => 0.01;
    public IReadOnlyList<double> Etas { get; } = [0, 0.5, 1];
    public IReadOnlyList<double> Xs { get; } = [0, 0.25, 0.5, 0.75, 1];
    public string? FailCode { get; init; }
    public IReadOnlyList<SectionSample>? Seen { get; private set; }

    public RunReference Reference(byte[] source) => new(0.108, 0.9, 0.12, "frame origin", "body; wind for lift/drag");

    public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
    {
        Seen = sections;
        if (FailCode is not null) throw new ContractError(FailCode, "the fake lattice failed");
        return new LatticeSolution([0.31, 0.29], [-0.012, -0.013], new RunDiagnostics(1e-13, 42.5));
    }

    public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
        WaterRecord water, CancellationToken cancellation) =>
        Enumerable.Range(0, solution.Gamma.Count).Select(j => new StripLoad(j, 0.1 * j, 0.5 * j, 0.12, solution.Gamma[j], 0.01,
            0.03 + op.AlphaDeg, 4.2e5, 0.4, new StripValue(null, "no polar method installed"), new StripValue(null, "no polar method installed"),
            0.1, 0.2, 40.5, 1.5, -0.25, 0.75, solution.DownwashTrefftz[j])).ToArray();
}

/// <summary>Holds the first evaluation that reaches it until released; every later one passes.</summary>
internal sealed class HoldFirst : IEvaluationBarrier
{
    private readonly TaskCompletionSource held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int calls;

    public ValueTask ComputedAsync(OperatingPoint op)
    {
        if (Interlocked.Increment(ref calls) != 1) return ValueTask.CompletedTask;
        held.SetResult();
        return new ValueTask(released.Task);
    }

    public void AwaitHeld()
    {
        if (!held.Task.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("no evaluation reached the barrier");
    }

    public void Release() => released.SetResult();
}
