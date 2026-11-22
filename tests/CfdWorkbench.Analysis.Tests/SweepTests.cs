using System.Diagnostics;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Node SWP-2 red-first tests (T1–T3, T10–T16 in docs/design/local-sweeps.md §8, Ruling 271):
/// schedule validation &amp; conditions, depth-unset basis, dedicated local runner + idempotency + Cancel &lt;= 250 ms,
/// failed case gap &amp; retry attempt chaining, DOC-SIZE safe stop, Cm / Ncrit 2-4 / non-bridged gap projection,
/// Estimator &amp; 1-panel honesty, Historical freshness by run key, and Goal-state FindAlpha + op.Load = requiredLoadN.
/// </summary>
internal static class SweepTests
{
    internal static void Run()
    {
        Check("SweepSchedule_Cartesian3x4_DerivesTwelveCasesAndConditions", T1_Cartesian3x4Conditions);
        Check("SweepSchedule_InvalidInputs_BlockWithInputNamed", T2_InvalidInputsBlocked);
        Check("SweepSchedule_DepthUnset_UnavailableAndNoDeepWaterLabel", T3_DepthUnsetNoDeepWater);
        Check("SweepService_LocalRun12Cases_ReusesRunKeyAndPreservesCancel", T10_LocalRunReuseAndCancel);
        Check("SweepService_OneFailedCase_StatusCompletedAndRetryAppendsAttempt", T11_OneFailedAndRetryAppends);
        Check("SweepService_DocSizeCap_StopsSafelyWithoutCorrupting", T12_DocSizeStopsSafely);
        Check("SweepProjection_CurvesAndGaps_NoSegmentAcrossFailedCase", T13_CurvesGapsCmAndNcritBand);
        Check("SweepProjection_EstimatorAndOnePanel_CmAndDragHonestUnavailable", T14_EstimatorAndOnePanelHonest);
        Check("SweepProjection_GeometryEditAfterSweep_HistoricalByRunKey", T15_HistoricalByRunKey);
        Check("SweepService_GoalStatePoints_SolvesAlphaPerPoint", T16_GoalStateSolvesAlphaAndLoad);
    }

    // T1 (XS-01): 3 speeds [10, 14, 18] kn x 4 alphas [0, 2, 4, 6] deg, salt 15 C, h_ref 0.5 m -> 12 sorted cases
    // with exact q, Re_ref, h/c, Fr_h, sigma within 1e-12 relative.
    private static void T1_Cartesian3x4Conditions()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var speeds = SweepSchedule.ParseSpeeds("18, 10, 14", SweepSpeedUnit.Knots);
        var angles = SweepSchedule.ParseAngles("6, 0, 4, 2");
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, speeds, angles, water, 0.5);

        Equal(12, preview.Cases.Count, "case count");
        Equal(12, preview.Rows.Count, "row count");
        double cRef = ReferenceQuantities.From(session.Snapshot().Source).CRef;
        double[] expectedSpeeds = [10.0 / Labels.KnotsPerMeterSecond, 14.0 / Labels.KnotsPerMeterSecond, 18.0 / Labels.KnotsPerMeterSecond];
        double[] expectedAngles = [0.0, 2.0, 4.0, 6.0];
        int idx = 0;
        foreach (double v in expectedSpeeds)
        {
            foreach (double a in expectedAngles)
            {
                var row = preview.Rows[idx];
                Equal(idx, row.CaseIndex, "case index");
                NearRel(v, row.SpeedMetersPerSecond, 1e-12, "speed m/s");
                NearRel(a, row.AlphaDeg!.Value, 1e-12, "alpha deg");
                double q = 0.5 * water.Rho * v * v;
                double reRef = v * cRef / water.Nu;
                double hc = 0.5 / cRef;
                double frH = v / Math.Sqrt(OperatingPoints.Gravity * 0.5);
                double sigma = (OperatingPoints.StandardAtmosphere + water.Rho * OperatingPoints.Gravity * 0.5 - water.Pv) / q;
                NearRel(q, row.Conditions.Q.Value!.Value, 1e-12, "q");
                NearRel(reRef, row.Conditions.ReRef.Value!.Value, 1e-12, "Re_ref");
                NearRel(hc, row.Conditions.DepthOverChord.Value!.Value, 1e-12, "h/c");
                NearRel(frH, row.Conditions.FroudeDepth.Value!.Value, 1e-12, "Fr_h");
                NearRel(sigma, row.Conditions.Sigma.Value!.Value, 1e-12, "sigma");
                idx++;
            }
        }
        Equal<double?>(null, preview.EstimatedWallMs, "wall estimate null until measured (IO8)");
    }

    // T2 (XS-01, ANA-15): Empty list, duplicate speed, NaN, speed <= 0, depth <= 0, water 55 C, 121 cases each refuse with stable code naming input.
    private static void T2_InvalidInputsBlocked()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);

        RefusesContaining("ANA-SWEEP-EMPTY", "Speeds", () => SweepSchedule.ParseSpeeds("   ", SweepSpeedUnit.Knots));
        RefusesContaining("ANA-SWEEP-EMPTY", "Angles", () => SweepSchedule.ParseAngles(""));
        RefusesContaining("ANA-SWEEP-DUPLICATE", "14", () => SweepSchedule.ParseSpeeds("10, 14, 14", SweepSpeedUnit.Knots));
        RefusesContaining("ANA-SWEEP-UNIT", "abc", () => SweepSchedule.ParseSpeeds("10, abc", SweepSpeedUnit.Knots));
        RefusesContaining("ANA-INPUT-SPEED", "-5", () => SweepSchedule.ParseSpeeds("-5, 10", SweepSpeedUnit.Knots));
        RefusesContaining("ANA-SWEEP-NONFINITE", "Speeds", () => SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [double.NaN], [2.0], water, 0.5));
        RefusesContaining("ANA-INPUT-SPEED", "0", () => SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [0.0], [2.0], water, 0.5));
        RefusesContaining("ANA-INPUT-DEPTH", "depth", () => SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [7.0], [2.0], water, -0.1));
        RefusesContaining("ANA-INPUT-WATER", "0–50", () => WaterTable.At(55.0, OperatingPoints.SaltSalinityGPerKg));
        var badWater = water with { TemperatureC = 55.0 };
        Refuses("ANA-INPUT-WATER", () => SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [7.0], [2.0], badWater, 0.5));

        double[] speeds11 = Enumerable.Range(1, 11).Select(i => 4.0 + i * 0.5).ToArray();
        double[] angles11 = Enumerable.Range(0, 11).Select(i => (double)i).ToArray();
        RefusesContaining("ANA-SWEEP-CASE-CAP", "121", () => SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, speeds11, angles11, water, 0.5));
    }

    // T3 (ANA-19, A5.6): h_ref = null -> h/c, Fr_h, sigma Unavailable (DepthNotSet); fixed label reads "free surface not modelled" and never "deep water".
    private static void T3_DepthUnsetNoDeepWater()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [7.0], [2.0], water, hRefMeters: null);

        Equal(false, preview.DepthSet, "depth unset");
        var cond = preview.Rows.Single().Conditions;
        Equal(DerivedReason.DepthNotSet, cond.DepthOverChord.Reason, "h/c reason");
        Equal(DerivedReason.DepthNotSet, cond.FroudeDepth.Reason, "Fr_h reason");
        Equal(DerivedReason.DepthNotSet, cond.Sigma.Reason, "sigma reason");
        Equal(true, preview.BasisLabel.Contains("free surface not modelled", StringComparison.Ordinal), "free surface label");
        Equal(false, preview.BasisLabel.Contains("deep water", StringComparison.OrdinalIgnoreCase), "must never say deep water");
        Equal(Labels.NotModelledUnset, preview.NotModelledLabel, "not modelled unset");
    }

    // T10 (RUN-05, RUN-06, A8.1): 12-case sequential local run; pre-computed case 0 hits idempotency (1 run row);
    // cancelling via IEvaluationBarrier on case index 6 finishes <= 250 ms, keeps cases 0..5 Completed, case 6 Cancelled (runId == null), 7..11 Pending.
    private static void T10_LocalRunReuseAndCancel()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        double[] speeds = [6.0, 8.0, 10.0];
        double[] angles = [0.0, 2.0, 4.0, 6.0];

        // Pre-compute case 0 via an independent interactive AnalysisService to prove idempotent reuse.
        var interactive = new AnalysisService(session, method);
        var preRun = interactive.EvaluateAsync(OperatingPoints.Custom(6.0, 0.0, 0.5), water, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None).GetAwaiter().GetResult();
        Equal(true, preRun.Outcome is RunOutcome.Completed, "pre-run completed");

        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, speeds, angles, water, 0.5);
        Equal(1, preview.CacheHits, "case 0 detected as cache hit");
        Equal(true, preview.Rows[0].CacheHit, "row 0 cache hit");

        using var cts = new CancellationTokenSource();
        long cancelRequestedAt = 0;
        int computeCount = 0;
        var barrier = new DelegateBarrier((op, token) =>
        {
            computeCount++;
            // Case 0 is an idempotent hit (doesn't hit barrier), so barrier #6 is case index 6 (the 7th case: speed 8.0, alpha 4.0).
            if (computeCount == 6)
            {
                cancelRequestedAt = Stopwatch.GetTimestamp();
                cts.Cancel();
                token.ThrowIfCancellationRequested();
            }
            return ValueTask.CompletedTask;
        });

        var svc = new SweepService(session, method, barrier);
        var queued = svc.Queue(preview, "Cancelable 3x4");
        var outcome = svc.RunAsync(queued.Id, cts.Token).GetAwaiter().GetResult();
        double cancelMs = Stopwatch.GetElapsedTime(cancelRequestedAt).TotalMilliseconds;

        Equal(true, outcome.Cancelled, "cancelled");
        Equal(SweepExperimentStatus.Cancelled, outcome.Status, "status cancelled");
        Equal(6, outcome.CompletedCases, "cases 0..5 completed");
        Equal(1, outcome.CacheHits, "case 0 reused");
        Equal(true, cancelMs <= 250.0, $"cancel latency {cancelMs:F1} ms <= 250 ms");

        var exp = session.ReadExperiments().Single().Experiment;
        for (int i = 0; i < 6; i++)
        {
            var att = exp.SelectedAttempt(i)!;
            Equal(SweepAttemptState.Completed, att.State, $"case {i} completed");
            Equal(true, att.RunId is not null, $"case {i} has runId");
        }
        Equal(preRun.RunId, exp.SelectedAttempt(0)!.RunId, "case 0 reused exact pre-computed runId");
        var att6 = exp.SelectedAttempt(6)!;
        Equal(SweepAttemptState.Cancelled, att6.State, "case 6 cancelled");
        Equal<string?>(null, att6.RunId, "cancelled attempt runId == null");
        for (int i = 7; i < 12; i++)
            Equal<SweepAttempt?>(null, exp.SelectedAttempt(i), $"case {i} left pending");
        // Case 0 only has 1 run row in AnalysisRuns (not duplicated).
        Equal(1, session.ReadRuns().Runs.Count(r => r.Run.RunKey == preRun.RunKey), "case 0 single run row");
    }

    // T11 (RUN-05, RUN-06, ANA-08): 1 failed case (ANA-SOLVE-RESIDUAL on case 11) -> status Completed (11 of 12), runId == null;
    // RetrySampleAsync(11) appends attemptIndex = 2 with priorAttemptId = attempt1.Id; all-failed yields Failed (0 of 12).
    private static void T11_OneFailedAndRetryAppends()
    {
        using var session = OpenDefault();
        bool failCase11 = true;
        var method = new FaultingWingMethod(FastVlmMethod(), op => failCase11 && Math.Abs(op.Speed - 10.0) < 1e-9 && Math.Abs(op.AlphaDeg - 6.0) < 1e-9);
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [6.0, 8.0, 10.0], [0.0, 2.0, 4.0, 6.0], water, 0.5);

        var svc = new SweepService(session, method);
        var queued = svc.Queue(preview, "One-fail sweep");
        var outcome = svc.RunAsync(queued.Id).GetAwaiter().GetResult();

        Equal(SweepExperimentStatus.Completed, outcome.Status, "11/12 is Completed");
        Equal(11, outcome.CompletedCases, "11 completed");
        Equal(1, outcome.FailedCases, "1 failed");
        var exp1 = session.ReadExperiments().Single().Experiment;
        var att1 = exp1.SelectedAttempt(11)!;
        Equal(SweepAttemptState.Failed, att1.State, "case 11 failed");
        Equal<string?>(null, att1.RunId, "failed runId must be null");
        Equal("ANA-SOLVE-RESIDUAL", att1.Events[^1].Code, "failure code");

        // Retry sample 11 after clearing fault -> appends attempt 2 preserving attempt 1!
        failCase11 = false;
        var att2 = svc.RetrySampleAsync(queued.Id, 11).GetAwaiter().GetResult();
        Equal(2, att2.AttemptIndex, "attemptIndex == 2");
        Equal(att1.AttemptId, att2.PriorAttemptId, "priorAttemptId chains to attempt 1");
        Equal(SweepAttemptState.Completed, att2.State, "retry completed");
        Equal(true, att2.RunId is not null, "retry completed has runId");
        var exp2 = session.ReadExperiments().Single().Experiment;
        Equal(2, exp2.Attempts.Count(a => a.CaseIndex == 11), "attempt 1 preserved alongside attempt 2");

        // All-failed sweep yields status Failed (0 of m).
        using var sessionAllFail = OpenDefault();
        var allFailMethod = new FaultingWingMethod(FastVlmMethod(), _ => true);
        var pFail = SweepSchedule.BuildCartesian(sessionAllFail, allFailMethod, SweepRecords.TierVlmStrip, [6.0, 8.0], [0.0, 2.0], water, 0.5);
        var svcFail = new SweepService(sessionAllFail, allFailMethod);
        var qFail = svcFail.Queue(pFail, "All fail");
        var rFail = svcFail.RunAsync(qFail.Id).GetAwaiter().GetResult();
        Equal(SweepExperimentStatus.Failed, rFail.Status, "0/4 is Failed");
        Equal(0, rFail.CompletedCases, "0 completed");
        Equal(4, rFail.FailedCases, "4 failed");
    }

    // T12 (RUN-04, Ruling 263): Under lowered envelopeCap, hitting DOC-SIZE on case k marks case k Failed (DOC-SIZE, runId == null),
    // halts remaining cases as Pending, and preserves 0..k-1 runs intact.
    private static void T12_DocSizeStopsSafely()
    {
        // Measure size of a 2-run session first, then choose cap with 4 kB slack: fits the Failed (DOC-SIZE) attempt
        // while refusing the ~15 kB 3rd VLM run.
        int twoRunBytes;
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        using (var probe = OpenDefault())
        {
            var p = SweepSchedule.BuildCartesian(probe, method, SweepRecords.TierVlmStrip, [6.0], [0.0, 2.0], water, 0.5);
            var s = new SweepService(probe, method);
            var q = s.Queue(p, "Probe");
            _ = s.RunAsync(q.Id).GetAwaiter().GetResult();
            twoRunBytes = probe.DocumentBytes();
        }
        int cap = twoRunBytes + 4000;
        using var session = new AuthoringSession(envelopeCap: cap);
        session.Open(FoilSource.NewDefault(), Guid.NewGuid().ToString("D"), true);
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [6.0], [0.0, 2.0, 4.0, 6.0], water, 0.5);
        var svc = new SweepService(session, method);
        var queued = svc.Queue(preview, "Cap test");
        var outcome = svc.RunAsync(queued.Id).GetAwaiter().GetResult();

        Equal(true, outcome.StoppedAtDocSize, "stopped at DOC-SIZE");
        Equal(true, outcome.CompletedCases >= 1 && outcome.CompletedCases < 4, $"partial completed ({outcome.CompletedCases}) before cap");
        var exp = session.ReadExperiments().Single().Experiment;
        int failedIdx = outcome.CompletedCases;
        var failedAtt = exp.SelectedAttempt(failedIdx)!;
        Equal(SweepAttemptState.Failed, failedAtt.State, "cap-hitting case failed");
        Equal("DOC-SIZE", failedAtt.Events[^1].Code, "DOC-SIZE code");
        Equal<string?>(null, failedAtt.RunId, "DOC-SIZE attempt has null runId");
        for (int k = failedIdx + 1; k < 4; k++)
            Equal<SweepAttempt?>(null, exp.SelectedAttempt(k), $"case {k} left pending after DOC-SIZE");

        // Document stays intact and reopens cleanly.
        byte[] saved = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(saved);
        Equal(outcome.CompletedCases, reopened.ReadRuns().Runs.Count, "prior runs intact");
    }

    // T13 (ANA-16, RES-04, ANA-08): 11 Completed + 1 Failed interior case (14 kn, alpha 4 deg, index 6):
    // verifies Cm < 0 (positive lift behind LE -> nose-down My < 0), both Ncrit 2 and Ncrit 4 present (never averaged),
    // and interior failed case splits BOTH the 14 kn vs-alpha polyline AND the alpha 4 deg vs-speed polyline into disjoint segments with ZERO edge bridging the gap.
    private static void T13_CurvesGapsCmAndNcritBand()
    {
        using var session = OpenDefault();
        double v10 = 10.0 / Labels.KnotsPerMeterSecond;
        double v14 = 14.0 / Labels.KnotsPerMeterSecond;
        double v18 = 18.0 / Labels.KnotsPerMeterSecond;
        var baseMethod = FastVlmMethod();
        var method = new FaultingWingMethod(baseMethod, op => Math.Abs(op.Speed - v14) < 1e-6 && Math.Abs(op.AlphaDeg - 4.0) < 1e-9);
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [v10, v14, v18], [0.0, 2.0, 4.0, 6.0], water, 0.5);

        var svc = new SweepService(session, method);
        var queued = svc.Queue(preview, "Curves & gap 3x4");
        _ = svc.RunAsync(queued.Id).GetAwaiter().GetResult();

        var stored = session.ReadExperiments().Single();
        var proj = SweepProjection.Project(session, stored, method);
        Equal(SweepExperimentStatus.Completed, proj.Status.Status, "status completed");
        Equal(11, proj.Status.Completed, "11 completed");
        Equal(1, proj.Status.Failed, "1 failed");

        // Positive lift case (alpha = 6 deg, case 3) has CL > 0 and Cm < 0 about root LE (My = -x_cp * L < 0).
        var case3 = proj.Cases[3];
        Equal(true, case3.Cl is > 0.1, "CL > 0 at alpha 6");
        Equal(true, case3.Cm is < 0, $"Cm about root LE must be negative for positive lift, got {case3.Cm}");

        // Ncrit 2 and Ncrit 4 are distinct and both present (never averaged).
        Equal(true, case3.CdWingNcrit2.HasValue && case3.CdWingNcrit4.HasValue, "both Ncrit 2 and 4 CD present");
        Equal(true, case3.CdWingNcrit2!.Value > case3.CdWingNcrit4!.Value, "Ncrit 2 CD > Ncrit 4 CD");
        Equal(true, case3.LdWingNcrit2.HasValue && case3.LdWingNcrit4.HasValue, "both Ncrit 2 and 4 L/D present");
        Equal(true, case3.LdWingNcrit4!.Value > case3.LdWingNcrit2!.Value, "Ncrit 4 L/D > Ncrit 2 L/D");

        // vs Alpha: 14 kn series (index 1) has cases [4 (0°), 5 (2°), 6 (4° FAILED), 7 (6°)] -> MUST split into 2 segments: [0°, 2°] and [6°]!
        var clVsAlpha = proj.Chart(SweepCurveMetric.Cl, SweepXAxis.AlphaDeg, Units.Imperial);
        Equal(3, clVsAlpha.Series.Count, "3 speed series");
        var midSpeedSeries = clVsAlpha.Series[1];
        Equal(2, midSpeedSeries.PrimarySegments.Count, "14 kn series split into 2 non-bridged segments");
        Equal(2, midSpeedSeries.PrimarySegments[0].Points.Count, "left segment has 0° and 2°");
        Equal(1, midSpeedSeries.PrimarySegments[1].Points.Count, "right segment has 6°");
        Equal(1, midSpeedSeries.Gaps.Count, "1 explicit gap marker");
        Equal(6, midSpeedSeries.Gaps[0].CaseIndex, "gap at case 6");

        // vs Speed: alpha 4° series (index 2) has cases [2 (10 kn), 6 (14 kn FAILED), 10 (18 kn)] -> MUST split into 2 segments: [10 kn] and [18 kn]!
        var cdVsSpeed = proj.Chart(SweepCurveMetric.CdWing, SweepXAxis.Speed, Units.Imperial);
        Equal(4, cdVsSpeed.Series.Count, "4 alpha series");
        var alpha4Series = cdVsSpeed.Series[2];
        Equal(2, alpha4Series.PrimarySegments.Count, "alpha 4° series split into 2 non-bridged segments");
        Equal(1, alpha4Series.PrimarySegments[0].Points.Count, "left segment has 10 kn");
        Equal(1, alpha4Series.PrimarySegments[1].Points.Count, "right segment has 18 kn");
        Equal(2, alpha4Series.BandUpperSegments.Count, "Ncrit 4 band also split at gap");
        Equal(2, alpha4Series.SecondarySegments.Count, "CDi Trefftz also split at gap");

        // Cm chart has exact subtitle.
        var cmChart = proj.Chart(SweepCurveMetric.Cm, SweepXAxis.AlphaDeg);
        Equal(Labels.SweepCmSubtitle, cmChart.Subtitle, "Cm subtitle");
    }

    // T14 (A5.6, Ruling 161): Estimator sweep returns CL and CDi (e = 1) and Unavailable for Cm & Wing-only L/D;
    // 1-chordwise-panel VLM sweep returns "Not resolved · 1 chordwise panel" for Cm.
    private static void T14_EstimatorAndOnePanelHonest()
    {
        using var session = OpenDefault();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var defaultMethod = FastVlmMethod();

        // 1. Estimator sweep
        var estPreview = SweepSchedule.BuildCartesian(session, defaultMethod, SweepRecords.TierEstimator, [7.0], [2.0, 4.0], water, 0.5);
        var estSvc = new SweepService(session, defaultMethod);
        var estQ = estSvc.Queue(estPreview, "Estimator sweep");
        _ = estSvc.RunAsync(estQ.Id).GetAwaiter().GetResult();
        var estProj = SweepProjection.Project(session, session.ReadExperiments().Single(e => e.Experiment.Id == estQ.Id), defaultMethod);
        var estRow = estProj.Cases[0];
        Equal(true, estRow.Cl.HasValue && estRow.Cdi.HasValue, "estimator CL and CDi present");
        Equal<double?>(null, estRow.Cm, "estimator Cm null");
        Equal(Labels.SweepCmUnavailableEstimator, estRow.CmUnavailableReason, "estimator Cm reason");
        Equal<double?>(null, estRow.LdWingNcrit2, "estimator L/D null");
        Equal(Labels.SweepDragUnavailableEstimator, estRow.DragUnavailableReason, "estimator drag reason");
        Equal(Labels.SweepCmUnavailableEstimator, estProj.Chart(SweepCurveMetric.Cm, SweepXAxis.AlphaDeg).UnavailableReason, "estimator Cm chart unavailable");
        Equal(Labels.SweepDragUnavailableEstimator, estProj.Chart(SweepCurveMetric.LdWing, SweepXAxis.AlphaDeg).UnavailableReason, "estimator L/D chart unavailable");

        // 2. 1-chordwise-panel VLM sweep
        var onePanelMethod = FastVlmMethod(nChord: 1);
        var onePreview = SweepSchedule.BuildCartesian(session, onePanelMethod, SweepRecords.TierVlmStrip, [7.0], [2.0, 4.0], water, 0.5);
        var oneSvc = new SweepService(session, onePanelMethod);
        var oneQ = oneSvc.Queue(onePreview, "One-panel VLM");
        _ = oneSvc.RunAsync(oneQ.Id).GetAwaiter().GetResult();
        var oneProj = SweepProjection.Project(session, session.ReadExperiments().Single(e => e.Experiment.Id == oneQ.Id), onePanelMethod);
        Equal<double?>(null, oneProj.Cases[0].Cm, "1-panel Cm null");
        Equal(Labels.MomentNotResolved, oneProj.Cases[0].CmUnavailableReason, "1-panel Cm Not resolved");
        Equal(Labels.MomentNotResolved, oneProj.Chart(SweepCurveMetric.Cm, SweepXAxis.AlphaDeg).UnavailableReason, "1-panel Cm chart Not resolved");
    }

    // T15 (ANA-07, UI-06): Accepting a twist edit r1 -> r2 makes the r1 sweep read Historical (r1 -> r2, a twist point moved)
    // without mutating any stored sweep field; Undo back to r1 makes it Current again.
    private static void T15_HistoricalByRunKey()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var preview = SweepSchedule.BuildCartesian(session, method, SweepRecords.TierVlmStrip, [7.0], [2.0, 4.0], water, 0.5);
        var svc = new SweepService(session, method);
        var queued = svc.Queue(preview, "Freshness test");
        _ = svc.RunAsync(queued.Id).GetAwaiter().GetResult();
        var beforeExp = session.ReadExperiments().Single().Experiment;

        var projCurrent = SweepProjection.Project(session, session.ReadExperiments().Single(), method);
        Equal(false, projCurrent.IsHistorical, "initially Current");
        Equal(SweepCaseFreshness.Current, projCurrent.Cases[0].Freshness, "case 0 Current");

        // Apply twist edit -> r1 -> r2.
        TwistEdit(session, 1.0);
        var projHist = SweepProjection.Project(session, session.ReadExperiments().Single(), method);
        Equal(true, projHist.IsHistorical, "Historical after twist edit");
        Equal(SweepCaseFreshness.Historical, projHist.Cases[0].Freshness, "case 0 Historical");
        Equal(true, projHist.FreshnessBanner.Contains("r1 → r2", StringComparison.Ordinal), $"banner names r1 -> r2: {projHist.FreshnessBanner}");
        Equal(true, projHist.FreshnessBanner.Contains("twist", StringComparison.OrdinalIgnoreCase), $"banner names twist: {projHist.FreshnessBanner}");
        // Stored experiment record is completely untouched.
        Equal(beforeExp, session.ReadExperiments().Single().Experiment, "stored experiment unchanged");

        // Undo back to r1 -> Current again!
        session.Undo(Guid.NewGuid().ToString("D"));
        var projBack = SweepProjection.Project(session, session.ReadExperiments().Single(), method);
        Equal(false, projBack.IsHistorical, "Current again after Undo");
        Equal(SweepCaseFreshness.Current, projBack.Cases[0].Freshness, "case 0 Current after Undo");
    }

    // T16 (XS-01, ANA-05, Ruling 271 DR-SWP-4 (b)): 3-point GoalStateSweepSnapshot -> solves alpha within 1 % of RequiredLoadN,
    // records SolvedAlphaRecord and op.Load == RequiredLoadN, and marks unsolvable point Failed (ANA-FIND-NO-SIGN-CHANGE) without extrapolating.
    private static void T16_GoalStateSolvesAlphaAndLoad()
    {
        using var session = OpenDefault();
        var method = FastVlmMethod();
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var goal = new GoalStateSweepSnapshot(
            Guid.NewGuid().ToString("D"),
            new string('c', 64),
            water,
            [
                new GoalOperatingPointSnapshot("p1", "P1 Take-off", 6.0, 0.5, OperatingPoints.RootLeadingEdge, OperatingPoints.StandardAtmosphere, 600.0, "rider+gear", "takeoff-cl", [2, 4], "11.7 kn"),
                new GoalOperatingPointSnapshot("p2", "P2 Cruise", 9.0, 0.5, OperatingPoints.RootLeadingEdge, OperatingPoints.StandardAtmosphere, 600.0, "rider+gear", "cruise-ld", [2, 4], "17.5 kn"),
                new GoalOperatingPointSnapshot("p3", "P3 Impossible", 2.0, 0.5, OperatingPoints.RootLeadingEdge, OperatingPoints.StandardAtmosphere, 50_000.0, "rider+gear", "takeoff-cl", [2, 4], "3.9 kn")
            ],
            new GoalDepthBand(0.25, 0.5, "user"),
            null);

        var preview = SweepSchedule.BuildFromGoalState(session, method, SweepRecords.TierVlmStrip, goal);
        Equal(3, preview.Cases.Count, "3 goal cases");
        var svc = new SweepService(session, method);
        var queued = svc.Queue(preview, "Goal-state 3pt");
        var outcome = svc.RunAsync(queued.Id).GetAwaiter().GetResult();

        Equal(SweepExperimentStatus.Completed, outcome.Status, "2/3 completed");
        Equal(2, outcome.CompletedCases, "2 solvable points completed");
        Equal(1, outcome.FailedCases, "1 unsolvable point failed");

        var exp = session.ReadExperiments().Single().Experiment;
        var runs = session.ReadRuns().Runs.ToDictionary(r => r.Run.RunId, r => r.Run);
        for (int i = 0; i < 2; i++)
        {
            var att = exp.SelectedAttempt(i)!;
            Equal(SweepAttemptState.Completed, att.State, $"point {i} completed");
            Equal(true, att.SolvedAlpha is not null, $"point {i} has SolvedAlphaRecord");
            var run = runs[att.RunId!];
            // Ruling 271 DR-SWP-4 (b): op.Load == requiredLoadN!
            Equal(600.0, run.Op.Load, $"point {i} op.Load == requiredLoadN");
            double q = 0.5 * water.Rho * run.Op.Speed * run.Op.Speed;
            double achievedLift = WingCoefficients.Of(run).Cl!.Value * q * run.Reference.SRef;
            double relErr = Math.Abs(achievedLift - 600.0) / 600.0;
            Equal(true, relErr <= 0.015, $"point {i} achieved lift {achievedLift:F1} N within 1% of 600 N (err={relErr:P2})");
        }
        var attFail = exp.SelectedAttempt(2)!;
        Equal(SweepAttemptState.Failed, attFail.State, "impossible load failed");
        Equal<string?>(null, attFail.RunId, "failed goal attempt has null runId");
        Equal("ANA-FIND-NO-SIGN-CHANGE", attFail.Events[^1].Code, "no sign change without extrapolation");
    }

    // ---- Test fixtures & helpers ----

    private static AuthoringSession OpenDefault()
    {
        var session = new AuthoringSession();
        session.Open(FoilSource.NewDefault(), Guid.NewGuid().ToString("D"), true);
        return session;
    }

    private static IWingMethod FastVlmMethod(int nChord = 2) =>
        new TestWingMethod(Settings.WithStations(Settings.Default with { NSpanPerHalf = 16, NChord = nChord }), new FixedPolarSource(0.012, 0.009));

    private static void TwistEdit(AuthoringSession session, double degrees)
    {
        var snapshot = session.Snapshot();
        var point = Channels.View(snapshot.Source, "twist", "Accepted", 0).Points[0];
        var draft = session.BeginPointGesture(Guid.NewGuid().ToString("D"), "twist", point.Id);
        var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + degrees);
        session.Apply(Guid.NewGuid().ToString("D"), session.Validate(frame.Draft.Id, frame.Draft.Generation));
    }

    private static void NearRel(double expected, double actual, double relTol, string label)
    {
        double scale = Math.Max(1.0, Math.Abs(expected));
        if (Math.Abs(expected - actual) > relTol * scale)
            throw new InvalidOperationException($"{label}: expected {expected:R}, actual {actual:R}");
    }

    private static void Refuses(string code, Action action)
    {
        try { action(); }
        catch (ContractError error) { Equal(code, error.Code, "refusal code"); return; }
        throw new InvalidOperationException("expected refusal " + code + "; none");
    }

    private static void RefusesContaining(string code, string needle, Action action)
    {
        try { action(); }
        catch (ContractError error)
        {
            Equal(code, error.Code, "refusal code");
            string msg = (error.Reason ?? "") + " " + error.Message;
            Equal(true, msg.Contains(needle, StringComparison.OrdinalIgnoreCase), $"refusal message '{msg}' should mention '{needle}'");
            return;
        }
        throw new InvalidOperationException("expected refusal " + code + "; none");
    }

    private sealed class DelegateBarrier(Func<OperatingPoint, CancellationToken, ValueTask> onComputed) : IEvaluationBarrier
    {
        public ValueTask ComputedAsync(OperatingPoint op, CancellationToken cancellation) => onComputed(op, cancellation);
    }

    private sealed class FixedPolarSource(double cd2, double cd4) : IPolarSource
    {
        public string? UnavailableReason => null;
        public PolarResult? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation) =>
            new(new PolarSample(profileHash, "test-polar", "1.0.0", reynolds, ncrit, "smooth", alphaDeg, water.TableHash,
                0.3, Math.Abs(ncrit - 2.0) < 0.5 ? cd2 : cd4, -0.05, 0.5, 0.5, -0.5, 200, 0.99, true), [], false, 0.0, 0.0);
    }

    private sealed class TestWingMethod(RunSettings settings, IPolarSource polar) : IWingMethod
    {
        private readonly ProductWingMethod inner = new(settings, LatticePlant.None, polar);
        private static readonly string FakeProfileHash = new('1', 64);
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Solve(sections, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            var loads = StripCoupler.Couple(sections, solution, op, water, polar, _ => FakeProfileHash, cancellation);
            var withEdges = loads.Select((load, i) => load with { YLow = solution.Strips[i].YInboard, YHigh = solution.Strips[i].YOutboard }).ToArray();
            return ProductWingMethod.MarkOutermostProvisional(withEdges);
        }
        public IReadOnlyList<StripLoad> Couple(byte[] source, IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            Couple(sections, solution, op, water, cancellation);
    }

    private sealed class FaultingWingMethod(IWingMethod inner, Func<OperatingPoint, bool> shouldFail) : IWingMethod
    {
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            if (shouldFail(op)) throw new ContractError("ANA-SOLVE-RESIDUAL", "lattice residual exceeded limit");
            return inner.Solve(sections, op, water, cancellation);
        }
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Couple(sections, solution, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(byte[] source, IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Couple(source, sections, solution, op, water, cancellation);
    }
}
