using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Sweeps;

public enum SweepSubTab { ExperimentSetup = 0, LocalRun = 1, Results = 2 }

/// <summary>
/// View-model/controller for Area 4 local-tier sweeps inside the Sweeps workspace segment (Ruling 271 DR-SWP-6).
/// Owns a dedicated <see cref="SweepService"/> (never sharing interactive <see cref="AnalysisService"/>), manages
/// Experiment setup preview/validation, local Run execution/Cancel/Retry, and Results synchronized selection.
/// </summary>
public sealed class SweepController
{
    private AuthoringSession session;
    private readonly IWingMethod method;
    private readonly IEvaluationBarrier? barrier;
    private readonly TimeProvider time;
    private SweepService service;
    private CancellationTokenSource? activeCts;
    private readonly List<double> measuredCaseWallMs = [];

    public SweepController(AuthoringSession session, IWingMethod method, IEvaluationBarrier? barrier = null, TimeProvider? time = null)
    {
        this.session = session;
        this.method = method;
        this.barrier = barrier;
        this.time = time ?? TimeProvider.System;
        service = new SweepService(session, method, barrier, this.time);
    }

    public event Action? Changed;

    public SweepSubTab ActiveSubTab { get; private set; } = SweepSubTab.ExperimentSetup;
    public bool HasDraft { get; private set; }
    public string SweepName { get; set; } = "Baseline local sweep";
    public string SourceKind { get; set; } = SweepRecords.SourceCartesian;
    public string Tier { get; set; } = SweepRecords.TierVlmStrip;
    public string SpeedsText { get; set; } = "10, 14, 18";
    public SweepSpeedUnit SpeedUnit { get; set; } = SweepSpeedUnit.Knots;
    public string AnglesText { get; set; } = "0, 2, 4, 6";
    public bool SaltWater { get; set; } = true;
    public string WaterTempText { get; set; } = "15";
    public bool DepthSet { get; set; } = true;
    public string DepthText { get; set; } = "0.5";

    public SweepPreview? Preview { get; private set; }
    public string? ValidationError { get; private set; }
    public string? ValidationCode { get; private set; }
    public string? StalePreviewBanner { get; private set; }
    public string? LastBanner { get; private set; }

    public string? SelectedExperimentId { get; private set; }
    public int SelectedCaseIndex { get; private set; } = -1;
    public SweepCurveMetric ActiveMetric { get; set; } = SweepCurveMetric.Cl;
    public SweepXAxis ActiveXAxis { get; set; } = SweepXAxis.AlphaDeg;
    public bool ShowTableTwin { get; set; }
    public Units DisplayUnits { get; set; } = Units.Metric;

    public bool IsRunning { get; private set; }
    public int? ActiveRunningCaseIndex { get; private set; }
    public SweepProgress? LiveProgress { get; private set; }
    public string? NextSeriesId { get; private set; }
    public int NextVersion { get; private set; } = 1;
    public string? NextParentExperimentId { get; private set; }

    public GoalStateSweepSnapshot? GoalSnapshot
    {
        get
        {
            try { return session.CurrentGoalSweepSnapshot(); }
            catch (ContractError) { return null; }
        }
    }

    public IReadOnlyList<StoredSweepExperiment> Experiments
    {
        get
        {
            try { return session.ReadExperiments(); }
            catch (ContractError) { return []; }
        }
    }

    public StoredSweepExperiment? SelectedExperiment
    {
        get
        {
            var list = Experiments;
            if (list.Count == 0) return null;
            if (SelectedExperimentId is not null)
            {
                var found = list.FirstOrDefault(e => e.Experiment.Id == SelectedExperimentId);
                if (found is not null) return found;
            }
            return list[^1];
        }
    }

    public string StatusStripText
    {
        get
        {
            if (IsRunning && LiveProgress is { } p) return p.Message;
            if (SelectedExperiment is { } stored)
            {
                var status = SweepProjection.StatusOf(stored.Experiment, ActiveRunningCaseIndex);
                return "Sweep: " + status.BadgeText;
            }
            return "Sweep: none";
        }
    }

    public void BindSession(AuthoringSession next)
    {
        CancelRun();
        session = next;
        service = new SweepService(session, method, barrier, time);
        Preview = null;
        ValidationError = null;
        ValidationCode = null;
        StalePreviewBanner = null;
        LastBanner = null;
        HasDraft = false;
        NextSeriesId = null;
        NextVersion = 1;
        NextParentExperimentId = null;
        var list = Experiments;
        SelectedExperimentId = list.Count > 0 ? list[^1].Experiment.Id : null;
        SelectedCaseIndex = list.Count > 0 && list[^1].Experiment.Cases.Count > 0 ? 0 : -1;
        Notify();
    }

    public void SetSubTab(SweepSubTab tab)
    {
        if (ActiveSubTab == tab) return;
        ActiveSubTab = tab;
        CheckPreviewFreshness();
        Notify();
    }

    public void SetDisplayUnits(Units units)
    {
        if (DisplayUnits == units) return;
        DisplayUnits = units;
        Notify();
    }

    public void NewSweep()
    {
        HasDraft = true;
        NextSeriesId = null;
        NextVersion = 1;
        NextParentExperimentId = null;
        SweepName = "Baseline local sweep";
        RefreshPreview();
    }

    public void NewVersionFromSelected()
    {
        if (SelectedExperiment is not { } stored) { NewSweep(); return; }
        var def = stored.Experiment.Definition;
        HasDraft = true;
        NextSeriesId = stored.Experiment.SeriesId;
        NextParentExperimentId = stored.Experiment.Id;
        NextVersion = Experiments.Where(e => e.Experiment.SeriesId == stored.Experiment.SeriesId).Max(e => e.Experiment.Version) + 1;
        SweepName = stored.Experiment.Name;
        SourceKind = def.SourceKind;
        Tier = def.Tier;
        SpeedUnit = SweepSpeedUnit.MetersPerSecond;
        SpeedsText = string.Join(", ", def.SpeedsMetersPerSecond.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));
        AnglesText = string.Join(", ", def.AnglesDeg.Select(a => a.ToString("0.###", CultureInfo.InvariantCulture)));
        SaltWater = def.Water.SalinityGPerKg > 1.0;
        WaterTempText = def.Water.TemperatureC.ToString("0.##", CultureInfo.InvariantCulture);
        DepthSet = def.HRefMeters is not null;
        DepthText = def.HRefMeters is double h ? h.ToString("0.###", CultureInfo.InvariantCulture) : "";
        ActiveSubTab = SweepSubTab.ExperimentSetup;
        RefreshPreview();
    }

    public bool RefreshPreview()
    {
        HasDraft = true;
        ValidationError = null;
        ValidationCode = null;
        StalePreviewBanner = null;
        try
        {
            double? medianMs = MedianWallMs();
            int samples = measuredCaseWallMs.Count;
            if (SourceKind == SweepRecords.SourceGoalState)
            {
                var goal = GoalSnapshot ?? throw new ContractError("ANA-SWEEP-EMPTY", Labels.SweepNoGoalState);
                Preview = SweepSchedule.BuildFromGoalState(session, method, Tier, goal, medianMs, samples);
            }
            else
            {
                var speeds = SweepSchedule.ParseSpeeds(SpeedsText, SpeedUnit);
                var angles = SweepSchedule.ParseAngles(AnglesText);
                if (!double.TryParse(WaterTempText.Replace('−', '-'), NumberStyles.Float, CultureInfo.InvariantCulture, out double tempC) || !double.IsFinite(tempC))
                    throw new ContractError("ANA-INPUT-WATER", Labels.SweepInvalidValue("Water temperature", WaterTempText));
                double salinity = SaltWater ? OperatingPoints.SaltSalinityGPerKg : 0.0;
                var water = WaterTable.At(tempC, salinity);
                double? hRef = null;
                if (DepthSet)
                {
                    if (!double.TryParse(DepthText.Replace('−', '-'), NumberStyles.Float, CultureInfo.InvariantCulture, out double h) || !double.IsFinite(h) || h <= 0)
                        throw new ContractError("ANA-INPUT-DEPTH", Labels.SweepInvalidValue("Reference depth h_ref", DepthText));
                    hRef = h;
                }
                Preview = SweepSchedule.BuildCartesian(session, method, Tier, speeds, angles, water, hRef,
                    medianWallMs: medianMs, measuredRunSamples: samples);
            }
            Notify();
            return true;
        }
        catch (ContractError error)
        {
            Preview = null;
            ValidationCode = error.Code;
            ValidationError = error.Reason ?? error.Message;
            Notify();
            return false;
        }
    }

    public bool CanQueue => HasDraft && Preview is not null && ValidationError is null && StalePreviewBanner is null && !IsRunning;

    public SweepExperiment? Queue()
    {
        CheckPreviewFreshness();
        if (!CanQueue || Preview is null) return null;
        try
        {
            var queued = service.Queue(Preview, SweepName, NextSeriesId, NextVersion, NextParentExperimentId);
            SelectedExperimentId = queued.Id;
            SelectedCaseIndex = queued.Cases.Count > 0 ? 0 : -1;
            NextSeriesId = null;
            NextVersion = 1;
            NextParentExperimentId = null;
            LastBanner = Labels.SweepQueuedBanner(queued.Definition.Tier == SweepRecords.TierEstimator ? Labels.EstimatorChip : Labels.VlmChip, queued.Version);
            Notify();
            return queued;
        }
        catch (ContractError error)
        {
            ValidationCode = error.Code;
            ValidationError = error.Reason ?? error.Message;
            Notify();
            return null;
        }
    }

    public async Task<SweepRunOutcome?> RunSelectedAsync()
    {
        if (IsRunning || SelectedExperiment is not { } stored) return null;
        IsRunning = true;
        ActiveRunningCaseIndex = 0;
        LastBanner = null;
        using var cts = new CancellationTokenSource();
        activeCts = cts;
        var progress = new Progress<SweepProgress>(p =>
        {
            LiveProgress = p;
            ActiveRunningCaseIndex = p.IsRunning && p.ActiveCaseIndex >= 0 ? p.ActiveCaseIndex : null;
            Notify();
        });
        Notify();
        try
        {
            var outcome = await service.RunAsync(stored.Experiment.Id, cts.Token, progress).ConfigureAwait(false);
            RecordMeasuredWalls(outcome.Experiment);
            LastBanner = SweepProjection.StatusOf(outcome.Experiment).BannerText;
            return outcome;
        }
        catch (ContractError error)
        {
            LastBanner = error.Reason ?? error.Message;
            return null;
        }
        finally
        {
            IsRunning = false;
            ActiveRunningCaseIndex = null;
            LiveProgress = null;
            activeCts = null;
            Notify();
        }
    }

    public void CancelRun()
    {
        try { activeCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public async Task<SweepAttempt?> RetrySampleAsync(int caseIndex)
    {
        if (IsRunning || SelectedExperiment is not { } stored) return null;
        IsRunning = true;
        ActiveRunningCaseIndex = caseIndex;
        using var cts = new CancellationTokenSource();
        activeCts = cts;
        Notify();
        try
        {
            var att = await service.RetrySampleAsync(stored.Experiment.Id, caseIndex, cts.Token).ConfigureAwait(false);
            if (att.WallMs is double w && w > 0) measuredCaseWallMs.Add(w);
            return att;
        }
        catch (ContractError error)
        {
            LastBanner = error.Reason ?? error.Message;
            return null;
        }
        finally
        {
            IsRunning = false;
            ActiveRunningCaseIndex = null;
            LiveProgress = null;
            activeCts = null;
            Notify();
        }
    }

    public void ArchiveSelected()
    {
        if (IsRunning || SelectedExperiment is not { } stored || stored.Experiment.IsArchived) return;
        service.Archive(stored.Experiment.Id, "Archived from Sweeps results");
        LastBanner = Labels.SweepArchivedBanner;
        Notify();
    }

    public void SelectExperiment(string experimentId)
    {
        SelectedExperimentId = experimentId;
        var sel = SelectedExperiment;
        SelectedCaseIndex = sel is not null && sel.Experiment.Cases.Count > 0 ? 0 : -1;
        Notify();
    }

    public void SelectCase(int caseIndex)
    {
        if (SelectedCaseIndex == caseIndex) return;
        SelectedCaseIndex = caseIndex;
        Notify();
    }

    public void SetMetric(SweepCurveMetric metric)
    {
        if (ActiveMetric == metric) return;
        ActiveMetric = metric;
        Notify();
    }

    public void SetXAxis(SweepXAxis axis)
    {
        if (ActiveXAxis == axis) return;
        ActiveXAxis = axis;
        Notify();
    }

    public void SetShowTableTwin(bool show)
    {
        if (ShowTableTwin == show) return;
        ShowTableTwin = show;
        Notify();
    }

    public void CheckPreviewFreshness()
    {
        if (Preview is null) { StalePreviewBanner = null; return; }
        try
        {
            StalePreviewBanner = SweepSchedule.IsPreviewStale(Preview, session, method, Tier, out string? changed) && changed is not null
                ? Labels.SweepPreviewOutOfDate(changed)
                : null;
        }
        catch (ContractError) { StalePreviewBanner = null; }
    }

    public SweepProjectionResult? CurrentProjection()
    {
        if (SelectedExperiment is not { } stored) return null;
        try { return SweepProjection.Project(session, stored, method, ActiveRunningCaseIndex); }
        catch (ContractError) { return null; }
    }

    public void NotifyExternalChange()
    {
        CheckPreviewFreshness();
        Notify();
    }

    private void RecordMeasuredWalls(SweepExperiment exp)
    {
        foreach (var att in exp.Attempts)
            if (att.State == SweepAttemptState.Completed && att.WallMs is double w && double.IsFinite(w) && w > 0)
                measuredCaseWallMs.Add(w);
    }

    private double? MedianWallMs()
    {
        if (measuredCaseWallMs.Count == 0) return null;
        var sorted = measuredCaseWallMs.OrderBy(v => v).ToArray();
        return sorted[sorted.Length / 2];
    }

    private void Notify() => Changed?.Invoke();
}
