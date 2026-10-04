using System.Runtime.InteropServices;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>What one evaluation covers: the whole wing, or one authored station at its η (design §6.1).</summary>
public abstract record Scope
{
    private Scope() { }
    public sealed record Wing : Scope;
    public sealed record Station(int Index, double Eta) : Scope;
}

/// <summary>
/// The wing method the service runs (Strategy, design §7): its stored identity and settings, the Core stations it reads,
/// its reference quantities, the lattice solve and the strip coupling. The product method is VLM + strip
/// (<see cref="VortexLattice"/>, <see cref="StripCoupler"/>); a check passes its own. A compute failure is a
/// <see cref="ContractError"/> with an <c>ANA-*</c> code and a reason, which the service records as a Failed row.
/// </summary>
public interface IWingMethod
{
    RunMethod Method { get; }
    RunSettings Settings { get; }
    /// <summary>The near-field vs Trefftz reconciliation tolerance: recorded in the manifest, outside the key (§3.3).</summary>
    double ReconciliationTolerance { get; }
    /// <summary>The η stations and chord abscissae the method reads through <see cref="Placement.Sections"/>.</summary>
    IReadOnlyList<double> Etas { get; }
    IReadOnlyList<double> Xs { get; }
    RunReference Reference(byte[] source);
    LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation);
    IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
        WaterRecord water, CancellationToken cancellation);
}

/// <summary>
/// The hold point between compute and record (design §13.3): a check holds an evaluation here to supersede, cancel or
/// close under it. The product barrier returns at once.
/// </summary>
public interface IEvaluationBarrier
{
    ValueTask ComputedAsync(OperatingPoint op);
}

/// <summary>
/// Evaluates the accepted revision: snapshot once, compute, record (design §4, §6.1, §8). Idempotent by run key: an
/// existing intact Completed run with the key is returned. Latest wins: one evaluation in flight per scope, and a new one
/// cancels the older, which then records nothing. A cancelled evaluation records nothing; a compute failure records a
/// Failed row. The only write is <see cref="AuthoringSession.RecordRun"/>. Every evaluation emits <c>analysis.run</c>.
/// </summary>
public sealed class AnalysisService(AuthoringSession session, IWingMethod method, IEvaluationBarrier? barrier = null,
    TimeProvider? time = null)
{
    private const string TierId = "vlm-strip";
    // assume: a run that failed before its solve has no residual or κ₁, yet RunDiagnostics holds two finite numbers
    // (RunRecord.CheckStore refuses NaN). Zeros are written and the Failed outcome marks them as not measured. Confirmed by
    // the PRJ rule that a Failed row shows its error, never its diagnostics; if a reader shows them, it shows a false
    // "residual 0". Seam request S-A3 to STO: nullable diagnostics on a Failed row.
    private static readonly RunDiagnostics NotSolved = new(0, 0);
    private static readonly RunPlatform Platform = new(RuntimeInformation.OSDescription,
        RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(), Environment.Version.ToString());

    private readonly AuthoringSession session = session;
    private readonly IWingMethod method = method;
    private readonly IEvaluationBarrier? barrier = barrier;
    private readonly TimeProvider time = time ?? TimeProvider.System;
    private readonly object sync = new();
    private readonly Dictionary<Scope, CancellationTokenSource> inFlight = [];

    /// <summary>
    /// Evaluates <paramref name="op"/> in <paramref name="water"/>. Refuses an invalid operating point, another tier or a
    /// station scope before compute (<c>ANA-INPUT-*</c>, nothing recorded). Throws <see cref="OperationCanceledException"/>
    /// when cancelled or superseded, and the session's <see cref="ContractError"/> when it closed mid-compute.
    /// </summary>
    public async Task<AnalysisRun> EvaluateAsync(OperatingPoint op, WaterRecord water, Tier tier, Scope scope,
        CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(op);
        ArgumentNullException.ThrowIfNull(water);
        ArgumentNullException.ThrowIfNull(scope);
        long started = time.GetTimestamp();
        var trace = new Trace(scope is Scope.Wing ? "wing" : "station");
        string outcome = "OK";
        CancellationTokenSource? generation = null;
        try
        {
            if (tier != Tier.VlmStrip) throw new ContractError("ANA-INPUT-TIER", "A3a evaluates the VLM + strip tier only");
            if (scope is not Scope.Wing) throw new ContractError("ANA-INPUT-SCOPE", "the VLM + strip tier evaluates the wing");
            OperatingPoints.Validate(op);
            generation = Supersede(scope, cancellation);
            var token = generation.Token;
            var run = await Task.Run(() => ComputeAndRecordAsync(op, water, scope, generation, trace, token), token).ConfigureAwait(false);
            if (run.Outcome is RunOutcome.Failed failed) outcome = failed.Code;
            return run;
        }
        catch (OperationCanceledException) { outcome = "ANA-CANCELLED"; throw; }
        catch (ContractError error) { outcome = error.Code; throw; }
        finally
        {
            Release(scope, generation);
            session.RecordAnalysisEvent("analysis.run", outcome, time.GetElapsedTime(started).TotalMilliseconds, trace.Event(method));
        }
    }

    private async Task<AnalysisRun> ComputeAndRecordAsync(OperatingPoint op, WaterRecord water, Scope scope,
        CancellationTokenSource generation, Trace trace, CancellationToken token)
    {
        long computeStarted = time.GetTimestamp();
        long mark = computeStarted;
        double Lap()
        {
            long now = time.GetTimestamp();
            double ms = time.GetElapsedTime(mark, now).TotalMilliseconds;
            mark = now;
            return ms;
        }

        var view = session.Snapshot();
        trace.SnapshotMs = Lap();
        var inputs = Freshness.Inputs(view);
        string settingsHash = RunRecord.SettingsHash(method.Settings);
        string key = RunRecord.Key(inputs, water, op, method.Method, settingsHash);
        trace.RunKey12 = key[..12];
        var existing = session.ReadRuns().Runs.FirstOrDefault(stored => stored.Integrity == RunIntegrity.Intact &&
            stored.Run.Outcome is RunOutcome.Completed && stored.Run.RunKey == key);
        trace.IdempotentHit = existing is not null;
        if (existing is not null) return existing.Run;

        var reference = method.Reference(view.Source);
        RunOutcome outcome = new RunOutcome.Completed();
        var diagnostics = NotSolved;
        IReadOnlyList<StripLoad> strips = [];
        try
        {
            var sections = Placement.Sections(view.Source, method.Etas, method.Xs, token);
            trace.SectionsMs = Lap();
            var solution = method.Solve(sections, op, water, token);
            trace.SolveMs = Lap();
            diagnostics = solution.Diagnostics;
            trace.Diagnostics = diagnostics;
            strips = method.Couple(sections, solution, op, water, token);
            trace.StripMs = Lap();
        }
        catch (ContractError error) when (error.Code.StartsWith("ANA-", StringComparison.Ordinal))
        {
            outcome = new RunOutcome.Failed(error.Code, error.Reason ?? error.Message);
            strips = [];
        }
        trace.Strips = strips.Count;
        double wallMs = time.GetElapsedTime(computeStarted).TotalMilliseconds;

        if (barrier is not null) await barrier.ComputedAsync(op).ConfigureAwait(false);
        mark = time.GetTimestamp();
        var row = new AnalysisRun(Guid.NewGuid().ToString("D"), key, "", outcome, TierId, method.Method, method.Settings,
            settingsHash, inputs, water, op, reference, method.ReconciliationTolerance, diagnostics, strips, wallMs, Platform);
        row = row with { ContentHash = RunRecord.ContentHash(row) };
        // Check and record under the scope lock, so a newer evaluation either supersedes this one first (nothing is
        // recorded) or starts after the row is in: never two rows from one superseded pair.
        lock (sync)
        {
            token.ThrowIfCancellationRequested();
            if (!inFlight.TryGetValue(scope, out var current) || current != generation) throw new OperationCanceledException(token);
            session.RecordRun(row);
        }
        trace.RecordMs = Lap();
        return row;
    }

    private CancellationTokenSource Supersede(Scope scope, CancellationToken cancellation)
    {
        var generation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        lock (sync)
        {
            if (inFlight.TryGetValue(scope, out var older)) older.Cancel();
            inFlight[scope] = generation;
        }
        return generation;
    }

    private void Release(Scope scope, CancellationTokenSource? generation)
    {
        if (generation is null) return;
        lock (sync)
        {
            if (inFlight.TryGetValue(scope, out var current) && current == generation) inFlight.Remove(scope);
        }
        generation.Dispose();
    }

    // The analysis.run fields (design §11). A phase not reached stays null and reads "not recorded" (IO8). assembleMs is
    // never recorded: the method's solve assembles and solves in one call, so solveMs covers both.
    private sealed class Trace(string scope)
    {
        public string Scope { get; } = scope;
        public string? RunKey12 { get; set; }
        public bool? IdempotentHit { get; set; }
        public int? Strips { get; set; }
        public RunDiagnostics? Diagnostics { get; set; }
        public double? SnapshotMs { get; set; }
        public double? SectionsMs { get; set; }
        public double? SolveMs { get; set; }
        public double? StripMs { get; set; }
        public double? RecordMs { get; set; }

        public AnalysisEvent Event(IWingMethod method) => new()
        {
            Tier = TierId, MethodId = method.Method.Id, MethodVersion = method.Method.Version, RunKey12 = RunKey12,
            Scope = Scope, Unknowns = 2 * method.Settings.NSpanPerHalf * method.Settings.NChord, Strips = Strips,
            Residual = Diagnostics?.ResidualInf, Kappa1 = Diagnostics?.Kappa1, IdempotentHit = IdempotentHit,
            SnapshotMs = SnapshotMs, SectionsMs = SectionsMs, SolveMs = SolveMs, StripMs = StripMs, RecordMs = RecordMs
        };
    }
}
