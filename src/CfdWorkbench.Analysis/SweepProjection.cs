using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

public enum SweepExperimentStatus { Draft, Queued, Running, Completed, Failed, Cancelled }
public enum SweepCaseFreshness { Current, Historical, Pruned, PayloadFailedCheck, NotComputed }
public enum SweepCurveMetric { Cl, CdWing, LdWing, Cm }
public enum SweepXAxis { AlphaDeg, Speed }

public sealed record SweepStatusSummary(
    SweepExperimentStatus Status,
    int Total,
    int Completed,
    int Failed,
    int Cancelled,
    int Pending,
    string BadgeText,
    string BannerText);

public sealed record SweepCaseRow(
    int CaseIndex,
    string CaseKey,
    string? Label,
    double SpeedMetersPerSecond,
    double? HRefMeters,
    double? AlphaDeg,
    double? TargetLoadN,
    SweepAttemptState State,
    int AttemptCount,
    string? RunId,
    string? RunKey,
    string? FailureCode,
    string? FailureReason,
    bool IsGap,
    SweepCaseFreshness Freshness,
    string FreshnessText,
    DerivedConditions Conditions,
    double? Cl,
    double? Cdi,
    double? CdWingNcrit2,
    double? CdWingNcrit4,
    double? LdWingNcrit2,
    double? LdWingNcrit4,
    double? Cm,
    string? CmUnavailableReason,
    string? DragUnavailableReason,
    double? OswaldE,
    double? LiftN,
    double? InducedDragN,
    double? WingDragNcrit2N,
    double? WingDragNcrit4N,
    string EnvelopeVerdict);

public sealed record SweepPlotPoint(int CaseIndex, double X, double Y, double? YUpper, string Label);
public sealed record SweepSeriesSegment(IReadOnlyList<SweepPlotPoint> Points);
public sealed record SweepGapMarker(int CaseIndex, double X, string Code, string Reason, string AccessibleName);

public sealed record SweepCurveSeries(
    string SeriesId,
    string Label,
    double HeldValue,
    string HeldUnit,
    int StrokePatternIndex,
    IReadOnlyList<SweepSeriesSegment> PrimarySegments,
    IReadOnlyList<SweepSeriesSegment> BandUpperSegments,
    IReadOnlyList<SweepSeriesSegment> SecondarySegments,
    IReadOnlyList<SweepGapMarker> Gaps);

public sealed record SweepChartData(
    SweepCurveMetric Metric,
    SweepXAxis XAxis,
    string Title,
    string Subtitle,
    string XAxisLabel,
    string YAxisLabel,
    string? UnavailableReason,
    IReadOnlyList<SweepCurveSeries> Series,
    IReadOnlyList<SweepCaseRow> TableRows,
    string? GoalBandNote);

public sealed record SweepProjectionResult(
    StoredSweepExperiment Stored,
    SweepStatusSummary Status,
    bool IsHistorical,
    string FreshnessBanner,
    string BasisLabel,
    string NotModelledLabel,
    IReadOnlyList<SweepCaseRow> Cases,
    int? ActiveRunningCaseIndex)
{
    public SweepChartData Chart(SweepCurveMetric metric, SweepXAxis xAxis, Units units = Units.Metric, GoalStateSweepSnapshot? goal = null) =>
        SweepProjection.BuildChart(this, metric, xAxis, units, goal);
}

/// <summary>
/// Pure read-side derivation of sweep experiment status, per-case freshness by run key,
/// hydrodynamic coefficients (CL, CDi, Wing-only CD &amp; L/D Ncrit 2–4 band, frame-origin Cm),
/// and non-bridged gap-preserving chart series (docs/design/local-sweeps.md §2.4, Ruling 271).
/// </summary>
public static class SweepProjection
{
    public static SweepStatusSummary StatusOf(SweepExperiment experiment, int? activeRunningCaseIndex = null)
    {
        ArgumentNullException.ThrowIfNull(experiment);
        int total = experiment.Cases.Count;
        int completed = 0, failed = 0, cancelled = 0, pending = 0, running = 0;

        for (int i = 0; i < total; i++)
        {
            if (activeRunningCaseIndex == i)
            {
                running++;
                continue;
            }
            var selected = experiment.SelectedAttempt(i);
            if (selected is null) { pending++; continue; }
            switch (selected.State)
            {
                case SweepAttemptState.Completed: completed++; break;
                case SweepAttemptState.Failed: failed++; break;
                case SweepAttemptState.Cancelled: cancelled++; break;
                case SweepAttemptState.Running: running++; break;
                default: pending++; break;
            }
        }

        if (!experiment.IsQueued)
            return new(SweepExperimentStatus.Draft, total, completed, failed, cancelled, pending,
                $"Draft v{experiment.Version}", Labels.SweepSetupFooterNote);

        if (running > 0)
            return new(SweepExperimentStatus.Running, total, completed, failed, cancelled, pending,
                $"Running {completed + 1} of {total}", Labels.SweepRunningProgress(completed + 1, total));

        // Check if CancelRequested occurred after any non-running state when incomplete.
        bool hasCancelEvent = experiment.Events.Any(e => e.Kind == SweepEventKind.CancelRequested);
        if (cancelled > 0 || (hasCancelEvent && completed + failed < total))
            return new(SweepExperimentStatus.Cancelled, total, completed, failed, cancelled, pending,
                $"Cancelled ({completed} of {total})", Labels.SweepCancelledBanner(completed, total));

        if (completed == 0 && failed == 0 && pending == total)
            return new(SweepExperimentStatus.Queued, total, completed, failed, cancelled, pending,
                $"Queued v{experiment.Version}", Labels.SweepQueuedBanner(TierDisplayName(experiment.Definition.Tier), experiment.Version));

        if (pending == 0)
        {
            if (completed > 0)
            {
                string banner = failed > 0
                    ? Labels.SweepPartialCompletedBanner(completed, total, failed)
                    : $"Completed ({completed} of {total}) · {TierDisplayName(experiment.Definition.Tier)}";
                return new(SweepExperimentStatus.Completed, total, completed, failed, cancelled, pending,
                    failed > 0 ? $"Completed ({completed} of {total} · {failed} failed)" : $"Completed ({completed} of {total})",
                    banner);
            }
            return new(SweepExperimentStatus.Failed, total, completed, failed, cancelled, pending,
                $"Failed (0 of {total})", Labels.SweepAllFailedBanner(total));
        }

        // Partial progress (e.g. stopped on DOC-SIZE): remaining cases are Pending.
        return new(completed > 0 ? SweepExperimentStatus.Completed : SweepExperimentStatus.Failed,
            total, completed, failed, cancelled, pending,
            $"Stopped ({completed} of {total})",
            completed > 0 ? Labels.SweepPartialCompletedBanner(completed, total, failed + pending) : Labels.SweepAllFailedBanner(total));
    }

    public static SweepProjectionResult Project(
        AuthoringSession session,
        StoredSweepExperiment stored,
        IWingMethod method,
        int? activeRunningCaseIndex = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(method);

        var exp = stored.Experiment;
        var status = StatusOf(exp, activeRunningCaseIndex);
        var currentView = session.Snapshot();
        var currentInputsBase = Freshness.Inputs(currentView);
        var currentRef = ReferenceQuantities.From(currentView.Source);
        var ledger = session.ReadRuns();
        var runsById = ledger.Runs.ToDictionary(r => r.Run.RunId, r => r, StringComparer.Ordinal);
        var prunedById = ledger.Pruned.ToDictionary(p => p.RunId, p => p, StringComparer.Ordinal);

        (RunMethod currentMethod, RunSettings currentSettings) = SweepSchedule.MethodForTier(method, exp.Definition.Tier);
        string currentSettingsHash = RunRecord.SettingsHash(currentSettings);

        bool anyHistorical = false;
        bool surfaceChanged = exp.Definition.DesignRef.SurfaceHash != currentView.SurfaceHash;
        var rows = new List<SweepCaseRow>(exp.Cases.Count);

        // Determine cRef of the pinned revision for derived conditions.
        byte[]? pinnedSource = session.AcceptedSourceOf(exp.Definition.DesignRef.AcceptedId);
        double pinnedCRef = pinnedSource is not null ? ReferenceQuantities.From(pinnedSource).CRef : currentRef.CRef;

        for (int i = 0; i < exp.Cases.Count; i++)
        {
            var c = exp.Cases[i];
            var selected = exp.SelectedAttempt(i);
            int attemptCount = exp.Attempts.Count(a => a.CaseIndex == i);
            SweepAttemptState state = activeRunningCaseIndex == i
                ? SweepAttemptState.Running
                : (selected?.State ?? SweepAttemptState.Pending);

            double? effectiveAlpha = c.AlphaDeg ?? selected?.SolvedAlpha?.AlphaDeg;
            var opForDerive = new OperatingPoint(c.SpeedMetersPerSecond, c.PAtm, c.HRefMeters, c.Datum, effectiveAlpha ?? 0.0, c.TargetLoadN);
            var conditions = OperatingPoints.Derive(opForDerive, exp.Definition.Water, pinnedCRef);

            if (stored.Integrity != SweepIntegrity.Intact)
            {
                rows.Add(GapRow(c, effectiveAlpha, state, attemptCount, selected?.RunId, null,
                    "DOC-INTEGRITY", "Unavailable — sweep payload failed its check",
                    SweepCaseFreshness.PayloadFailedCheck, "Unavailable — sweep payload failed its check", conditions));
                continue;
            }

            if (selected is null || state != SweepAttemptState.Completed || selected.RunId is null)
            {
                string? code = selected?.Events[^1].Code;
                string? reason = selected?.Events[^1].Reason;
                rows.Add(GapRow(c, effectiveAlpha, state, attemptCount, null, null,
                    code, reason, SweepCaseFreshness.NotComputed, state.ToString(), conditions));
                continue;
            }

            if (prunedById.TryGetValue(selected.RunId, out var tombstone))
            {
                rows.Add(GapRow(c, effectiveAlpha, state, attemptCount, selected.RunId, tombstone.RunKey,
                    "DOC-PRUNED", Labels.SweepPrunedCaseCell,
                    SweepCaseFreshness.Pruned, Labels.SweepPrunedCaseCell, conditions));
                continue;
            }

            if (!runsById.TryGetValue(selected.RunId, out var storedRun))
            {
                rows.Add(GapRow(c, effectiveAlpha, state, attemptCount, selected.RunId, null,
                    "DOC-REFERENCE", Labels.PayloadFailed,
                    SweepCaseFreshness.PayloadFailedCheck, Labels.PayloadFailed, conditions));
                continue;
            }

            if (storedRun.Integrity != RunIntegrity.Intact)
            {
                rows.Add(GapRow(c, effectiveAlpha, state, attemptCount, storedRun.Run.RunId, RunRecord.RecomputedKey(storedRun.Run),
                    "DOC-INTEGRITY", Labels.PayloadFailed,
                    SweepCaseFreshness.PayloadFailedCheck, Labels.PayloadFailed, conditions));
                continue;
            }

            var run = storedRun.Run;
            string recomputedKey = RunRecord.RecomputedKey(run);
            var currentForCase = new CurrentInputs(currentInputsBase, exp.Definition.Water, run.Op, currentMethod, currentSettingsHash);
            var runFreshness = Freshness.State(storedRun, currentForCase);
            SweepCaseFreshness caseFreshness = runFreshness == RunState.Current ? SweepCaseFreshness.Current : SweepCaseFreshness.Historical;
            if (caseFreshness == SweepCaseFreshness.Historical) anyHistorical = true;
            string freshnessText = caseFreshness == SweepCaseFreshness.Current
                ? "Current"
                : BuildHistoricalPhrase(session, run, currentView.AcceptedId, currentForCase);

            rows.Add(DeriveCompletedRow(c, effectiveAlpha, selected, attemptCount, run, recomputedKey, caseFreshness, freshnessText, conditions, session));
        }

        bool isHistorical = anyHistorical || (surfaceChanged && exp.Attempts.Any(a => a.State == SweepAttemptState.Completed));
        string freshnessBanner;
        if (stored.Integrity != SweepIntegrity.Intact)
            freshnessBanner = "Unavailable — sweep payload failed its check";
        else if (exp.IsArchived)
            freshnessBanner = Labels.SweepArchivedBanner;
        else if (isHistorical)
        {
            var oldRev = session.RevisionOf(exp.Definition.DesignRef.AcceptedId);
            var newRev = session.RevisionOf(currentView.AcceptedId);
            string railDetail = DescribeRailEdit(newRev.Rail);
            freshnessBanner = surfaceChanged
                ? $"Historical — surface changed (r{oldRev.Ordinal} → r{newRev.Ordinal}{(railDetail.Length > 0 ? ", " + railDetail : "")})"
                : "Historical — method or settings changed";
        }
        else
            freshnessBanner = "Current";

        bool deepWater = exp.Definition.HRefMeters is double h && pinnedCRef > 0 && h / pinnedCRef >= Labels.DeepWaterHc;
        return new SweepProjectionResult(
            stored,
            status,
            isHistorical,
            freshnessBanner,
            Labels.FixedVlm(deepWater),
            Labels.NotModelled(deepWater),
            rows,
            activeRunningCaseIndex);
    }

    public static SweepChartData BuildChart(
        SweepProjectionResult projection,
        SweepCurveMetric metric,
        SweepXAxis xAxis,
        Units units = Units.Metric,
        GoalStateSweepSnapshot? goal = null)
    {
        ArgumentNullException.ThrowIfNull(projection);
        long started = Stopwatch.GetTimestamp();
        var exp = projection.Stored.Experiment;
        bool isEstimator = exp.Definition.Tier == SweepRecords.TierEstimator;

        string? unavailable = null;
        if (metric == SweepCurveMetric.Cm)
        {
            if (isEstimator) unavailable = Labels.SweepCmUnavailableEstimator;
            else if (projection.Cases.Any(c => c.CmUnavailableReason == Labels.MomentNotResolved) && projection.Cases.All(c => c.Cm is null))
                unavailable = Labels.MomentNotResolved;
        }
        else if (metric == SweepCurveMetric.LdWing && isEstimator)
        {
            unavailable = Labels.SweepDragUnavailableEstimator;
        }

        (string title, string subtitle, string yLabel) = metric switch
        {
            SweepCurveMetric.Cl => ("CL (Lift coefficient)", projection.BasisLabel, "CL"),
            SweepCurveMetric.CdWing => isEstimator
                ? ("CDi (Estimator, e = 1)", Labels.SweepDragUnavailableEstimator, "CDi")
                : ("CD (Wing only, Ncrit 2–4) & CDi (Trefftz)", "Wing-only band (Ncrit 2–4, never averaged) + CDi (Trefftz)", "CD"),
            SweepCurveMetric.LdWing => ("Wing-only L/D (Ncrit 2–4)", Labels.ReasonTexts["ANA-WING-ONLY-RATIO"], "CL/CD (wing)"),
            SweepCurveMetric.Cm => ("Cm (Pitching moment)", Labels.SweepCmSubtitle, "Cm"),
            _ => ("CL", projection.BasisLabel, "CL")
        };

        string speedUnitLabel = units == Units.Imperial ? "kn" : "m/s";
        string xLabel = xAxis == SweepXAxis.AlphaDeg ? "α (°)" : $"Speed ({speedUnitLabel})";

        var groups = GroupForAxis(projection, xAxis, units, speedUnitLabel);
        var seriesList = new List<SweepCurveSeries>(groups.Count);
        int totalGaps = 0;

        for (int sIdx = 0; sIdx < groups.Count; sIdx++)
        {
            var g = groups[sIdx];
            var primarySegs = new List<SweepSeriesSegment>();
            var upperSegs = new List<SweepSeriesSegment>();
            var secondarySegs = new List<SweepSeriesSegment>();
            var gaps = new List<SweepGapMarker>();

            var curPrimary = new List<SweepPlotPoint>();
            var curUpper = new List<SweepPlotPoint>();
            var curSecondary = new List<SweepPlotPoint>();

            void FlushSegments()
            {
                if (curPrimary.Count > 0) { primarySegs.Add(new SweepSeriesSegment(curPrimary.ToArray())); curPrimary.Clear(); }
                if (curUpper.Count > 0) { upperSegs.Add(new SweepSeriesSegment(curUpper.ToArray())); curUpper.Clear(); }
                if (curSecondary.Count > 0) { secondarySegs.Add(new SweepSeriesSegment(curSecondary.ToArray())); curSecondary.Clear(); }
            }

            foreach (var row in g.OrderedRows)
            {
                double xVal = xAxis == SweepXAxis.AlphaDeg
                    ? (row.AlphaDeg ?? 0.0)
                    : Labels.Speed(row.SpeedMetersPerSecond, units);

                if (row.IsGap || unavailable is not null)
                {
                    // Mandatory invariant (ANA-08, RES-04): NEVER bridge across a gap!
                    FlushSegments();
                    if (row.IsGap)
                    {
                        string code = row.FailureCode ?? row.State.ToString();
                        string reason = row.FailureReason ?? row.FreshnessText;
                        gaps.Add(new SweepGapMarker(row.CaseIndex, xVal, code, reason,
                            $"Case {row.CaseIndex + 1}: {row.State} — {reason}"));
                        totalGaps++;
                    }
                    continue;
                }

                (double? yPrimary, double? yUpper, double? ySecondary) = ExtractMetric(row, metric, isEstimator);
                if (yPrimary is null && ySecondary is null)
                {
                    FlushSegments();
                    continue;
                }

                string ptLabel = row.Label ?? $"Case {row.CaseIndex + 1}";
                if (yPrimary is { } yp) curPrimary.Add(new SweepPlotPoint(row.CaseIndex, xVal, yp, yUpper, ptLabel));
                else if (curPrimary.Count > 0) { primarySegs.Add(new SweepSeriesSegment(curPrimary.ToArray())); curPrimary.Clear(); }

                if (yUpper is { } yu) curUpper.Add(new SweepPlotPoint(row.CaseIndex, xVal, yu, null, ptLabel));
                else if (curUpper.Count > 0) { upperSegs.Add(new SweepSeriesSegment(curUpper.ToArray())); curUpper.Clear(); }

                if (ySecondary is { } ys) curSecondary.Add(new SweepPlotPoint(row.CaseIndex, xVal, ys, null, ptLabel));
                else if (curSecondary.Count > 0) { secondarySegs.Add(new SweepSeriesSegment(curSecondary.ToArray())); curSecondary.Clear(); }
            }
            FlushSegments();

            seriesList.Add(new SweepCurveSeries(
                g.SeriesId,
                g.Label,
                g.HeldValue,
                g.HeldUnit,
                sIdx,
                primarySegs,
                upperSegs,
                secondarySegs,
                gaps));
        }

        string? goalNote = goal is null
            ? "Goal-state bands: no Goal state in project"
            : exp.Definition.HRefMeters is null
                ? "Cavitation ceiling unavailable — depth not set"
                : $"Goal-state bands active ({goal.OperatingPoints.Count} operating point(s))";

        _ = Stopwatch.GetElapsedTime(started);
        return new SweepChartData(metric, xAxis, title, subtitle, xLabel, yLabel, unavailable, seriesList, projection.Cases, goalNote);
    }

    private static (double? Primary, double? Upper, double? Secondary) ExtractMetric(SweepCaseRow row, SweepCurveMetric metric, bool isEstimator) =>
        metric switch
        {
            SweepCurveMetric.Cl => (row.Cl, null, null),
            SweepCurveMetric.CdWing => isEstimator
                ? (row.Cdi, null, null)
                : (row.CdWingNcrit2, row.CdWingNcrit4, row.Cdi),
            SweepCurveMetric.LdWing => (row.LdWingNcrit2, row.LdWingNcrit4, null),
            SweepCurveMetric.Cm => (row.Cm, null, null),
            _ => (null, null, null)
        };

    private sealed record AxisGroup(string SeriesId, string Label, double HeldValue, string HeldUnit, IReadOnlyList<SweepCaseRow> OrderedRows);

    private static List<AxisGroup> GroupForAxis(SweepProjectionResult projection, SweepXAxis xAxis, Units units, string speedUnitLabel)
    {
        var exp = projection.Stored.Experiment;
        if (exp.Definition.SourceKind == SweepRecords.SourceGoalState)
        {
            var ordered = xAxis == SweepXAxis.Speed
                ? projection.Cases.OrderBy(c => c.SpeedMetersPerSecond).ThenBy(c => c.CaseIndex).ToArray()
                : projection.Cases.OrderBy(c => c.AlphaDeg ?? double.PositiveInfinity).ThenBy(c => c.CaseIndex).ToArray();
            return [new AxisGroup("goal", "Goal-state points", 0, "", ordered)];
        }

        if (xAxis == SweepXAxis.AlphaDeg)
        {
            return projection.Cases
                .GroupBy(c => c.SpeedMetersPerSecond)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    double displayV = Labels.Speed(g.Key, units);
                    string label = $"{Labels.Number(displayV, "0.##")} {speedUnitLabel}";
                    return new AxisGroup($"v-{g.Key:R}", label, displayV, speedUnitLabel,
                        g.OrderBy(c => c.AlphaDeg ?? 0.0).ThenBy(c => c.CaseIndex).ToArray());
                })
                .ToList();
        }
        else
        {
            return projection.Cases
                .GroupBy(c => c.AlphaDeg ?? 0.0)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    string label = $"α {Labels.Number(g.Key, "0.##")}°";
                    return new AxisGroup($"a-{g.Key:R}", label, g.Key, "°",
                        g.OrderBy(c => c.SpeedMetersPerSecond).ThenBy(c => c.CaseIndex).ToArray());
                })
                .ToList();
        }
    }

    private static SweepCaseRow DeriveCompletedRow(
        SweepCase c,
        double? effectiveAlpha,
        SweepAttempt selected,
        int attemptCount,
        AnalysisRun run,
        string recomputedKey,
        SweepCaseFreshness freshness,
        string freshnessText,
        DerivedConditions conditions,
        AuthoringSession session)
    {
        double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        double sRef = run.Reference.SRef;
        bool validRef = q > 0 && sRef > 0;

        double? cl, cdi, oswaldE, liftN, inducedDragN;
        double? cdWing2 = null, cdWing4 = null, ld2 = null, ld4 = null, wingDrag2 = null, wingDrag4 = null;
        string? dragUnavailable = null;
        string envelopeVerdict;

        if (run.Tier == WingEstimator.TierId)
        {
            byte[]? src = session.AcceptedSourceOf(run.Inputs.AcceptedId);
            if (src is not null)
            {
                var est = WingEstimator.Derive(run, src);
                cl = est.Cl;
                cdi = est.Cdi;
                oswaldE = est.E;
                liftN = validRef ? cl * q * sRef : null;
                inducedDragN = validRef ? cdi * q * sRef : null;
            }
            else
            {
                cl = cdi = oswaldE = liftN = inducedDragN = null;
            }
            dragUnavailable = Labels.SweepDragUnavailableEstimator;
            envelopeVerdict = Labels.EstimatorNotValidated;
        }
        else
        {
            var coeffs = WingCoefficients.Of(run);
            cl = coeffs.Cl;
            cdi = coeffs.Cdi;
            oswaldE = coeffs.E;
            double aRad = VortexLattice.ToRadians(run.Op.AlphaDeg);
            liftN = run.Strips.Sum(s => -s.Fx * Math.Sin(aRad) + s.Fz * Math.Cos(aRad));
            inducedDragN = Loads.InducedDrag(run).Value;

            var wd2 = Loads.WingDrag(run, 2);
            var wd4 = Loads.WingDrag(run, 4);
            if (wd2.Value is { } d2 && wd4.Value is { } d4 && validRef)
            {
                wingDrag2 = d2;
                wingDrag4 = d4;
                cdWing2 = d2 / (q * sRef);
                cdWing4 = d4 / (q * sRef);
                ld2 = d2 > 0 ? liftN / d2 : null;
                ld4 = d4 > 0 ? liftN / d4 : null;
            }
            else
            {
                int missing = run.Strips.Count(s => s.CdNcrit2.Value is null || s.CdNcrit4.Value is null);
                dragUnavailable = missing > 0
                    ? $"Unavailable — cd missing at {missing} strips; the estimator bound is not substituted"
                    : Labels.ReasonTexts["ANA-WING-RATIO-UNAVAILABLE"];
            }
            envelopeVerdict = Labels.Inside;
        }

        var moment = WingCoefficients.PitchingMoment(run);

        return new SweepCaseRow(
            c.CaseIndex,
            c.CaseKey,
            c.Label,
            c.SpeedMetersPerSecond,
            c.HRefMeters,
            effectiveAlpha,
            c.TargetLoadN,
            SweepAttemptState.Completed,
            attemptCount,
            run.RunId,
            recomputedKey,
            null,
            null,
            false,
            freshness,
            freshnessText,
            conditions,
            cl,
            cdi,
            cdWing2,
            cdWing4,
            ld2,
            ld4,
            moment.Cm,
            moment.UnavailableReason,
            dragUnavailable,
            oswaldE,
            liftN,
            inducedDragN,
            wingDrag2,
            wingDrag4,
            envelopeVerdict);
    }

    private static SweepCaseRow GapRow(
        SweepCase c,
        double? effectiveAlpha,
        SweepAttemptState state,
        int attemptCount,
        string? runId,
        string? runKey,
        string? code,
        string? reason,
        SweepCaseFreshness freshness,
        string freshnessText,
        DerivedConditions conditions) =>
        new(
            c.CaseIndex, c.CaseKey, c.Label, c.SpeedMetersPerSecond, c.HRefMeters, effectiveAlpha, c.TargetLoadN,
            state, attemptCount, runId, runKey, code, reason, true, freshness, freshnessText, conditions,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Unavailable");

    private static string BuildHistoricalPhrase(AuthoringSession session, AnalysisRun run, string currentAcceptedId, CurrentInputs current)
    {
        var changed = Freshness.WhatChanged(run, current);
        if (changed.Contains("surface"))
        {
            var oldRev = session.RevisionOf(run.Inputs.AcceptedId);
            var newRev = session.RevisionOf(currentAcceptedId);
            string rail = DescribeRailEdit(newRev.Rail);
            return $"Historical — surface (r{oldRev.Ordinal} → r{newRev.Ordinal}{(rail.Length > 0 ? ", " + rail : "")})";
        }
        return "Historical — " + string.Join(", ", changed);
    }

    private static string DescribeRailEdit(string? rail) => rail switch
    {
        "twist" => "a twist point moved",
        "leading" => "a leading-edge point moved",
        "trailing" => "a trailing-edge point moved",
        "dihedral" => "a dihedral point moved",
        "thickness" => "a thickness point moved",
        "upper" or "lower" or "section" => "a section profile changed",
        "dimension" => "a wing dimension changed",
        "station-add" or "station-remove" or "station-match" => "stations changed",
        _ => ""
    };

    private static string TierDisplayName(string tier) =>
        tier == SweepRecords.TierEstimator ? Labels.EstimatorChip : Labels.VlmChip;
}
