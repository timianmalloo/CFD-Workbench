using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

public sealed record SweepProgress(
    string ExperimentId,
    int ActiveCaseIndex,
    int CompletedCases,
    int FailedCases,
    int TotalCases,
    bool IsRunning,
    string Message);

public sealed record SweepRunOutcome(
    SweepExperiment Experiment,
    SweepExperimentStatus Status,
    int CompletedCases,
    int FailedCases,
    int CacheHits,
    bool Cancelled,
    bool StoppedAtDocSize,
    double WallMs,
    double? CancelLatencyMs);

/// <summary>
/// Dedicated sequential local-tier sweep runner (docs/design/local-sweeps.md §2.3, Ruling 271 DR-SWP-3).
/// Owns its own <see cref="AnalysisService"/> instance so interactive single-point Analysis never supersedes
/// an active sweep, executes cases sequentially with idempotent run-key reuse, supports Goal-state FindAlpha
/// with <c>OperatingPoint.Load = requiredLoadN</c> (DR-SWP-4 (b)), and stops cleanly on Cancel or <c>DOC-SIZE</c>.
/// </summary>
public sealed class SweepService
{
    private const double FindAlphaLowerDeg = -4.0;
    private const double FindAlphaUpperDeg = 14.0;

    private readonly AuthoringSession session;
    private readonly IWingMethod method;
    private readonly TimeProvider time;
    private readonly AnalysisService analysis;

    public SweepService(AuthoringSession session, IWingMethod method, IEvaluationBarrier? barrier = null, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(method);
        this.session = session;
        this.method = method;
        this.time = time ?? TimeProvider.System;
        // Dedicated instance (G-SWP-4, Ruling 271 DR-SWP-3): never shares interactive AnalysisService.
        analysis = new AnalysisService(session, method, barrier, this.time);
    }

    /// <summary>
    /// Validates certification &amp; tip-chord floor, freezes <c>BackendPin = local · in-process</c>, and appends an
    /// immutable Queued <see cref="SweepExperiment"/> (XS-03).
    /// </summary>
    public SweepExperiment Queue(SweepPreview preview, string name, string? seriesId = null, int version = 1, string? parentExperimentId = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        long started = time.GetTimestamp();
        string outcome = "OK";
        string expId = Guid.NewGuid().ToString("D");
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ContractError("ANA-SWEEP-EMPTY", "Sweep name cannot be empty.");
            string trimmedName = name.Trim();
            if (trimmedName.Length > SweepRecords.MaxNameLength)
                trimmedName = trimmedName[..SweepRecords.MaxNameLength];

            // XS-03: verify current accepted geometry is certified and meets tip-chord floor before queueing.
            var inspected = session.InspectAccepted();
            if (inspected.Geometry.Status != GeometryStatus.Certified)
                throw new ContractError("DSL-NOT-ASSESSED", "the accepted geometry is not certified");
            if (preview.Definition.Tier == SweepRecords.TierVlmStrip)
                VerifyTipFloor(session.Snapshot().Source, method.Settings);

            var def = preview.Definition;
            var pin = new BackendPin(
                SweepRecords.LocalBackendId,
                SweepRecords.LocalSubstrate,
                def.Tier,
                def.Method.Id,
                def.Method.Version,
                def.SettingsHash);

            var exp = SweepRecords.Seal(new SweepExperiment(
                expId,
                seriesId ?? Guid.NewGuid().ToString("D"),
                version,
                parentExperimentId,
                SweepRecords.KindSweep,
                trimmedName,
                "",
                def,
                pin,
                preview.Cases.ToArray(),
                [new ExperimentEvent(0, SweepEventKind.Queued, time.GetUtcNow())],
                []));

            session.RecordExperiment(exp);
            return exp;
        }
        catch (ContractError error) { outcome = error.Code; throw; }
        finally
        {
            session.RecordSweepEvent("sweep.queue", outcome, time.GetElapsedTime(started).TotalMilliseconds, new SweepTelemetry
            {
                ExperimentId = expId,
                Version = version,
                SourceKind = preview.Definition.SourceKind,
                Tier = preview.Definition.Tier,
                CaseCount = preview.Cases.Count,
                EstimatedBytes = preview.EstimatedStorageBytes,
                DocumentBytes = SafeDocumentBytes()
            });
        }
    }

    public async Task<SweepRunOutcome> RunAsync(string experimentId, CancellationToken cancellation = default, IProgress<SweepProgress>? progress = null)
    {
        var exp = RequireLocalRunnable(experimentId);
        long runStarted = time.GetTimestamp();
        int cacheHits = 0;
        bool cancelled = false;
        bool stoppedDocSize = false;
        double? cancelLatencyMs = null;

        try
        {
            int total = exp.Cases.Count;
            for (int k = 0; k < total; k++)
            {
                exp = FindExperiment(experimentId);
                var existingSelected = exp.SelectedAttempt(k);
                if (existingSelected is not null && existingSelected.State == SweepAttemptState.Completed)
                    continue;

                if (cancellation.IsCancellationRequested)
                {
                    long cancelMark = time.GetTimestamp();
                    cancelled = true;
                    AppendCancelEvent(experimentId);
                    cancelLatencyMs = time.GetElapsedTime(cancelMark).TotalMilliseconds;
                    break;
                }

                var counts = CountStates(exp);
                progress?.Report(new SweepProgress(experimentId, k, counts.Completed, counts.Failed, total, true,
                    Labels.SweepRunningProgress(counts.Completed + 1, total)));

                var (attempt, hit, hitCancel, hitDocSize, cancelMs) = await ExecuteSingleCaseAsync(exp, k, cancellation).ConfigureAwait(false);
                if (hit) cacheHits++;
                if (hitCancel)
                {
                    cancelled = true;
                    cancelLatencyMs = cancelMs;
                    AppendCancelEvent(experimentId);
                    break;
                }
                if (hitDocSize)
                {
                    stoppedDocSize = true;
                    break;
                }
            }
        }
        finally
        {
            exp = FindExperiment(experimentId);
            var counts = CountStates(exp);
            progress?.Report(new SweepProgress(experimentId, -1, counts.Completed, counts.Failed, exp.Cases.Count, false,
                SweepProjection.StatusOf(exp).BannerText));
        }

        exp = FindExperiment(experimentId);
        var summary = SweepProjection.StatusOf(exp);
        double wallMs = time.GetElapsedTime(runStarted).TotalMilliseconds;
        string telemetryOutcome = stoppedDocSize ? "DOC-SIZE" : summary.Status.ToString();
        session.RecordSweepEvent("sweep.run", telemetryOutcome, wallMs, new SweepTelemetry
        {
            ExperimentId = experimentId,
            Version = exp.Version,
            Tier = exp.Definition.Tier,
            TotalCases = exp.Cases.Count,
            CompletedCases = summary.Completed,
            FailedCases = summary.Failed,
            CacheHits = cacheHits,
            Cancelled = cancelled,
            WallMs = wallMs,
            CancelLatencyMs = cancelLatencyMs
        });

        return new SweepRunOutcome(exp, summary.Status, summary.Completed, summary.Failed, cacheHits, cancelled, stoppedDocSize, wallMs, cancelLatencyMs);
    }

    public async Task<SweepAttempt> RetrySampleAsync(string experimentId, int caseIndex, CancellationToken cancellation = default, IProgress<SweepProgress>? progress = null)
    {
        var exp = RequireLocalRunnable(experimentId);
        if (caseIndex < 0 || caseIndex >= exp.Cases.Count)
            throw new ContractError("ANA-SWEEP-CASE", $"Case index {caseIndex} is out of range.");

        long started = time.GetTimestamp();
        var prior = exp.SelectedAttempt(caseIndex);
        var counts = CountStates(exp);
        progress?.Report(new SweepProgress(experimentId, caseIndex, counts.Completed, counts.Failed, exp.Cases.Count, true,
            Labels.SweepRunningProgress(caseIndex + 1, exp.Cases.Count)));

        var (attempt, _, hitCancel, _, _) = await ExecuteSingleCaseAsync(exp, caseIndex, cancellation).ConfigureAwait(false);
        if (hitCancel) AppendCancelEvent(experimentId);

        double wallMs = time.GetElapsedTime(started).TotalMilliseconds;
        session.RecordSweepEvent("sweep.retry", attempt.State == SweepAttemptState.Completed ? "OK" : (attempt.Events[^1].Code ?? attempt.State.ToString()),
            wallMs, new SweepTelemetry
            {
                ExperimentId = experimentId,
                CaseIndex = caseIndex,
                AttemptIndex = attempt.AttemptIndex,
                PriorAttemptId = prior?.AttemptId,
                WallMs = wallMs
            });

        exp = FindExperiment(experimentId);
        counts = CountStates(exp);
        progress?.Report(new SweepProgress(experimentId, -1, counts.Completed, counts.Failed, exp.Cases.Count, false,
            SweepProjection.StatusOf(exp).BannerText));
        return attempt;
    }

    public SweepExperiment Archive(string experimentId, string? reason = null)
    {
        long started = time.GetTimestamp();
        var exp = FindExperiment(experimentId);
        if (exp.IsArchived) return exp;
        int released = exp.SelectedCompletedRunIds().Count();
        int bytesBefore = SafeDocumentBytes() ?? 0;
        session.AppendSweepEvent(experimentId, new ExperimentEvent(exp.Events.Count, SweepEventKind.Archived, time.GetUtcNow(), reason));
        int bytesAfter = SafeDocumentBytes() ?? bytesBefore;
        var updated = FindExperiment(experimentId);
        int pinnedAfter = CountActivePinnedRuns();
        session.RecordSweepEvent("sweep.archive", "OK", time.GetElapsedTime(started).TotalMilliseconds, new SweepTelemetry
        {
            ExperimentId = experimentId,
            ReleasedRuns = released,
            PinnedRunsAfter = pinnedAfter,
            DocumentBytesBefore = bytesBefore,
            DocumentBytesAfter = bytesAfter
        });
        return updated;
    }

    public SweepPreview CreateNextVersionPreview(string sourceExperimentId, SweepPreview refreshedPreview, out string seriesId, out int nextVersion, out string parentId)
    {
        ArgumentNullException.ThrowIfNull(refreshedPreview);
        var prior = FindExperiment(sourceExperimentId);
        seriesId = prior.SeriesId;
        parentId = prior.Id;
        nextVersion = session.ReadExperiments()
            .Where(item => item.Experiment.SeriesId == prior.SeriesId)
            .Max(item => item.Experiment.Version) + 1;
        return refreshedPreview;
    }

    private async Task<(SweepAttempt Attempt, bool IdempotentHit, bool Cancelled, bool HitDocSize, double? CancelLatencyMs)>
        ExecuteSingleCaseAsync(SweepExperiment exp, int caseIndex, CancellationToken cancellation)
    {
        var sweepCase = exp.Cases[caseIndex];
        var prior = exp.SelectedAttempt(caseIndex);
        int nextAttemptIndex = (prior?.AttemptIndex ?? 0) + 1;
        string attemptId = Guid.NewGuid().ToString("D");
        DateTimeOffset pendingAt = time.GetUtcNow();
        DateTimeOffset runningAt = pendingAt;
        long caseStarted = time.GetTimestamp();

        Tier tierEnum = exp.Definition.Tier == SweepRecords.TierEstimator ? Tier.Estimator : Tier.VlmStrip;
        SolvedAlphaRecord? solvedAlpha = null;
        int? findAlphaSolves = null;
        bool idempotentHit = false;
        string? runKey12 = null;

        try
        {
            cancellation.ThrowIfCancellationRequested();
            OperatingPoint op;
            if (sweepCase.AlphaDeg is { } fixedAlpha)
            {
                op = new OperatingPoint(sweepCase.SpeedMetersPerSecond, sweepCase.PAtm, sweepCase.HRefMeters, sweepCase.Datum, fixedAlpha, null);
            }
            else
            {
                // Goal-state operating point: solve alpha via FindAlpha within 1 % of RequiredLoadN and set op.Load = RequiredLoadN (Ruling 271 DR-SWP-4 (b)).
                var solveResult = SolveGoalAlpha(exp, sweepCase, tierEnum, cancellation);
                findAlphaSolves = solveResult.Solves;
                if (solveResult.Solved is null)
                {
                    double failMs = time.GetElapsedTime(caseStarted).TotalMilliseconds;
                    var failAttempt = BuildAttempt(attemptId, caseIndex, nextAttemptIndex, prior?.AttemptId, null, null, failMs,
                        pendingAt, runningAt, SweepAttemptState.Failed, solveResult.FailureCode!, solveResult.FailureReason!);
                    session.RecordSweepAttempt(exp.Id, failAttempt);
                    EmitCaseTelemetry(exp, caseIndex, nextAttemptIndex, null, false, findAlphaSolves, failMs, solveResult.FailureCode!);
                    return (failAttempt, false, false, false, null);
                }
                solvedAlpha = solveResult.Solved;
                op = new OperatingPoint(sweepCase.SpeedMetersPerSecond, sweepCase.PAtm, sweepCase.HRefMeters, sweepCase.Datum,
                    solvedAlpha.AlphaDeg, sweepCase.TargetLoadN);
            }

            // Detect idempotent cache hit before calling EvaluateAtRevisionAsync.
            byte[] pinnedSource = session.AcceptedSourceOf(exp.Definition.DesignRef.AcceptedId)
                ?? throw new ContractError("DOC-REFERENCE", "the pinned accepted revision is not held by this session");
            var pinnedParsed = FoilSource.Parse(pinnedSource);
            var pinnedInputs = Freshness.Inputs(new SessionView(exp.Definition.DesignRef.AcceptedId, pinnedParsed.SourceHash, pinnedParsed.SurfaceHash!, pinnedSource, null, null, false));
            string expectedKey = RunRecord.Key(pinnedInputs, exp.Definition.Water, op, exp.Definition.Method, exp.Definition.SettingsHash);
            runKey12 = expectedKey[..12];
            idempotentHit = session.ReadRuns().Runs.Any(stored =>
                stored.Integrity == RunIntegrity.Intact && stored.Run.Outcome is RunOutcome.Completed && stored.Run.RunKey == expectedKey);

            var run = await analysis.EvaluateAtRevisionAsync(
                exp.Definition.DesignRef.AcceptedId, op, exp.Definition.Water, tierEnum, new Scope.Wing(), cancellation).ConfigureAwait(false);
            double wallMs = time.GetElapsedTime(caseStarted).TotalMilliseconds;

            if (run.Outcome is RunOutcome.Completed)
            {
                var okAttempt = BuildAttempt(attemptId, caseIndex, nextAttemptIndex, prior?.AttemptId, run.RunId, solvedAlpha, wallMs,
                    pendingAt, runningAt, SweepAttemptState.Completed, null, null);
                session.RecordSweepAttempt(exp.Id, okAttempt);
                EmitCaseTelemetry(exp, caseIndex, nextAttemptIndex, runKey12, idempotentHit, findAlphaSolves, wallMs, "OK");
                return (okAttempt, idempotentHit, false, false, null);
            }
            else
            {
                var failed = (RunOutcome.Failed)run.Outcome;
                // Spec A3.1:255: Failed attempt MUST store runId == null.
                var failAttempt = BuildAttempt(attemptId, caseIndex, nextAttemptIndex, prior?.AttemptId, null, solvedAlpha, wallMs,
                    pendingAt, runningAt, SweepAttemptState.Failed, failed.Code, failed.Reason);
                session.RecordSweepAttempt(exp.Id, failAttempt);
                EmitCaseTelemetry(exp, caseIndex, nextAttemptIndex, runKey12, false, findAlphaSolves, wallMs, failed.Code);
                return (failAttempt, false, false, false, null);
            }
        }
        catch (OperationCanceledException)
        {
            long cancelMark = time.GetTimestamp();
            double wallMs = time.GetElapsedTime(caseStarted).TotalMilliseconds;
            var cancelAttempt = BuildAttempt(attemptId, caseIndex, nextAttemptIndex, prior?.AttemptId, null, null, wallMs,
                pendingAt, runningAt, SweepAttemptState.Cancelled, "ANA-CANCELLED", "Cancelled by user");
            TryRecordAttempt(exp.Id, cancelAttempt);
            double cancelLatencyMs = time.GetElapsedTime(cancelMark).TotalMilliseconds;
            EmitCaseTelemetry(exp, caseIndex, nextAttemptIndex, runKey12, false, findAlphaSolves, wallMs, "ANA-CANCELLED");
            return (cancelAttempt, false, true, false, cancelLatencyMs);
        }
        catch (ContractError error)
        {
            double wallMs = time.GetElapsedTime(caseStarted).TotalMilliseconds;
            bool isDocSize = error.Code == "DOC-SIZE";
            string reason = isDocSize ? Labels.SweepDocSizeStopped(caseIndex + 1) : (error.Reason ?? error.Message);
            var failAttempt = BuildAttempt(attemptId, caseIndex, nextAttemptIndex, prior?.AttemptId, null, null, wallMs,
                pendingAt, runningAt, SweepAttemptState.Failed, error.Code, reason);
            TryRecordAttempt(exp.Id, failAttempt);
            EmitCaseTelemetry(exp, caseIndex, nextAttemptIndex, runKey12, false, findAlphaSolves, wallMs, error.Code);
            return (failAttempt, false, false, isDocSize, null);
        }
    }

    private (SolvedAlphaRecord? Solved, int Solves, string? FailureCode, string? FailureReason) SolveGoalAlpha(
        SweepExperiment exp, SweepCase sweepCase, Tier tier, CancellationToken cancellation)
    {
        byte[] source = session.AcceptedSourceOf(exp.Definition.DesignRef.AcceptedId)
            ?? throw new ContractError("DOC-REFERENCE", "the pinned accepted revision is not held by this session");
        var reference = ReferenceQuantities.From(source);
        double q = 0.5 * exp.Definition.Water.Rho * sweepCase.SpeedMetersPerSecond * sweepCase.SpeedMetersPerSecond;
        if (!(q > 0) || !(reference.SRef > 0))
            return (null, 0, "ANA-SPEED-NOT-POSITIVE", Labels.ReasonTexts["ANA-SPEED-NOT-POSITIVE"]);
        double targetCl = sweepCase.TargetLoadN!.Value / (q * reference.SRef);
        double? depthOverChord = sweepCase.HRefMeters is double h && reference.CRef > 0 ? h / reference.CRef : null;

        if (depthOverChord is { } shallow && shallow < FindAlpha.DepthFloorHOverC)
            return (null, 0, "ANA-FIND-DEPTH-BELOW-FLOOR", Labels.FindReason("ANA-FIND-DEPTH-BELOW-FLOOR", FindAlpha.DepthFloorHOverC));

        string basis = depthOverChord is { } hc && hc < Labels.DeepWaterHc
            ? $"deep water, uncorrected; free surface not modelled (h/c = {Labels.Number(hc, "0.00")})"
            : "deep water, uncorrected";

        var baseOp = new OperatingPoint(sweepCase.SpeedMetersPerSecond, sweepCase.PAtm, sweepCase.HRefMeters, sweepCase.Datum, 0.0, sweepCase.TargetLoadN);
        int solves = 0;
        Func<double, StripValue> clAt;
        if (tier == Tier.Estimator)
        {
            var estSettings = WingEstimator.Settings(method.Settings);
            string estSettingsHash = RunRecord.SettingsHash(estSettings);
            var parsed = FoilSource.Parse(source);
            var inputs = Freshness.Inputs(new SessionView(exp.Definition.DesignRef.AcceptedId, parsed.SourceHash, parsed.SurfaceHash!, source, null, null, false));
            clAt = alpha =>
            {
                solves++;
                var opAt = baseOp with { AlphaDeg = alpha };
                var synthetic = new AnalysisRun(Guid.NewGuid().ToString("D"), new string('0', 64), new string('0', 64),
                    new RunOutcome.Completed(), WingEstimator.TierId, WingEstimator.Method, estSettings, estSettingsHash,
                    inputs, exp.Definition.Water, opAt, reference, method.ReconciliationTolerance, [], 0, new RunPlatform("local", "in-process", "10.0"));
                var est = WingEstimator.Derive(synthetic, source, cancellation);
                return new StripValue(est.Cl, null);
            };
        }
        else
        {
            var rawCl = FindAlpha.ClAt(method, source, baseOp, exp.Definition.Water, cancellation);
            clAt = alpha => { solves++; return rawCl(alpha); };
        }

        SearchResult search = OperatingSearch.FindAlpha(clAt, targetCl, FindAlphaLowerDeg, FindAlphaUpperDeg, basis, cancellation);
        if (search.Value is not { } alphaDeg)
            return (null, solves, search.TerminationCode, Labels.FindReason(search.TerminationCode, iterations: search.Iterations));

        return (new SolvedAlphaRecord(alphaDeg, search.Lower, search.Upper, search.Iterations, solves, basis), solves, null, null);
    }

    private SweepAttempt BuildAttempt(string attemptId, int caseIndex, int attemptIndex, string? priorAttemptId,
        string? runId, SolvedAlphaRecord? solvedAlpha, double wallMs, DateTimeOffset pendingAt, DateTimeOffset runningAt,
        SweepAttemptState terminalState, string? code, string? reason)
    {
        DateTimeOffset endedAt = time.GetUtcNow();
        return new SweepAttempt(
            attemptId,
            caseIndex,
            attemptIndex,
            priorAttemptId,
            runId,
            solvedAlpha,
            wallMs,
            [
                new AttemptStateEvent(0, SweepAttemptState.Pending, pendingAt),
                new AttemptStateEvent(1, SweepAttemptState.Running, runningAt),
                new AttemptStateEvent(2, terminalState, endedAt, code, reason)
            ]);
    }

    private void TryRecordAttempt(string experimentId, SweepAttempt attempt)
    {
        try { session.RecordSweepAttempt(experimentId, attempt); }
        catch (ContractError error) when (error.Code == "DOC-SIZE") { }
    }

    private void AppendCancelEvent(string experimentId)
    {
        try
        {
            var exp = FindExperiment(experimentId);
            session.AppendSweepEvent(experimentId, new ExperimentEvent(exp.Events.Count, SweepEventKind.CancelRequested, time.GetUtcNow(), "Cancelled by user"));
        }
        catch (ContractError) { }
    }

    private SweepExperiment RequireLocalRunnable(string experimentId)
    {
        var exp = FindExperiment(experimentId);
        if (!exp.IsQueued)
            throw new ContractError("ANA-SWEEP-NOT-QUEUED", "Sweep must be Queued before execution (XS-03).");
        if (exp.IsArchived)
            throw new ContractError("ANA-SWEEP-ARCHIVED", "Archived sweeps cannot be executed; create a new version instead.");
        if (exp.BackendPin is not { Id: SweepRecords.LocalBackendId, Substrate: SweepRecords.LocalSubstrate })
            throw new ContractError("ANA-SWEEP-BACKEND",
                $"Not ready — no backend row matches the pin {exp.BackendPin?.Id ?? "none"} · {exp.BackendPin?.Substrate ?? "none"}");
        return exp;
    }

    private SweepExperiment FindExperiment(string experimentId) =>
        session.ReadExperiments().FirstOrDefault(item => item.Experiment.Id == experimentId)?.Experiment
        ?? throw new ContractError("DOC-REFERENCE", $"Sweep experiment {experimentId} was not found.");

    private static (int Completed, int Failed) CountStates(SweepExperiment exp)
    {
        int completed = 0, failed = 0;
        for (int i = 0; i < exp.Cases.Count; i++)
        {
            var sel = exp.SelectedAttempt(i);
            if (sel?.State == SweepAttemptState.Completed) completed++;
            else if (sel?.State == SweepAttemptState.Failed) failed++;
        }
        return (completed, failed);
    }

    private int CountActivePinnedRuns()
    {
        var pinned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stored in session.ReadExperiments())
        {
            if (stored.Experiment.IsArchived) continue;
            foreach (string id in stored.Experiment.SelectedCompletedRunIds()) pinned.Add(id);
        }
        return pinned.Count;
    }

    private int? SafeDocumentBytes()
    {
        try { return session.DocumentBytes(); }
        catch (ContractError) { return null; }
    }

    private void EmitCaseTelemetry(SweepExperiment exp, int caseIndex, int attemptIndex, string? runKey12,
        bool idempotentHit, int? findAlphaSolves, double wallMs, string outcome)
    {
        session.RecordSweepEvent("sweep.case", outcome, wallMs, new SweepTelemetry
        {
            ExperimentId = exp.Id,
            CaseIndex = caseIndex,
            AttemptIndex = attemptIndex,
            Tier = exp.Definition.Tier,
            RunKey12 = runKey12,
            IdempotentHit = idempotentHit,
            FindAlphaSolves = findAlphaSolves,
            WallMs = wallMs
        });
    }

    private static void VerifyTipFloor(byte[] source, RunSettings settings)
    {
        if (settings.SectionEtas is not { Count: > 0 } etas || settings.SectionXs is not { Count: > 0 } xs) return;
        var sections = Placement.Sections(source, etas, xs, CancellationToken.None);
        if (sections.Count == 0) return;
        var root = sections.MinBy(s => s.Frame.Eta)!.Frame;
        var tip = sections.MaxBy(s => s.Frame.Eta)!.Frame;
        double rootChord = root.TrailingMeters - root.LeadingMeters;
        double tipChord = tip.TrailingMeters - tip.LeadingMeters;
        if (!TipChord.Meets(tipChord, rootChord))
            throw new ContractError(AnalysisService.TipBelowFloorCode, Labels.TipChordUnderMinimum(rootChord));
    }
}
