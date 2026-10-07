using CfdWorkbench.Cli;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop;

public sealed record DisplayPoint(double Eta, double NormalizedX, bool Upper, PlacedPointEnclosure Enclosure)
{
    public double X => (Enclosure.X.Lower + Enclosure.X.Upper) / 2;
    public double Y => (Enclosure.Y.Lower + Enclosure.Y.Upper) / 2;
    public double Z => (Enclosure.Z.Lower + Enclosure.Z.Upper) / 2;
}

public sealed record DisplayFrame(IReadOnlyList<DisplayPoint> Points, SectionEnclosure CenterSection,
    double InteriorEta, double ElapsedMilliseconds, string SourceHash, string Provenance);

public sealed record SectionReportLine(string Label, string Value);

/// <summary>One catalog row or one My-sections entry. DLG passes it to preview and Replace; this track draws no dialog.</summary>
public abstract record CatalogChoice
{
    public sealed record Catalog(CatalogEntry Entry) : CatalogChoice;
    public sealed record Mine(LibraryEntry Entry) : CatalogChoice;
    public sealed record Damaged(string File, string Reason) : CatalogChoice;
}

/// <summary>What <see cref="WorkbenchController.OpenCatalog"/> last read. Choosable rows have coordinates and no disabled reason.</summary>
public sealed record CatalogSnapshot(
    IReadOnlyList<CatalogEntry> Entries,
    IReadOnlyList<LibraryEntry> Mine,
    int Choosable,
    int Disabled,
    int Problems,
    string Outcome)
{
    public string? FailureCause { get; init; }
    public IReadOnlyList<(string File, string Reason)> ProblemRows { get; init; } = [];
}

public enum GestureState { Idle, Pressed, Dragging, Nudging, Busy }
public enum GestureInput { Pointer, Keyboard, Typed }
public enum GestureEnd { Release, KeyUp, Escape, CaptureLost, FocusLost, Deactivated, Save, Close, Open, New }
public enum NudgeModifier { Command, Plain, Shift }

/// <summary>The member that stops a group move: <see cref="Kind"/> is a <see cref="GestureLimitKind"/> name, "Neighbour" or "Domain".</summary>
public sealed record GroupBinding(PointRef Point, string Kind);
public enum EntryOrigin { Properties, Plan, Side, Browser, Palette, Recovery }

/// <summary>One section visit. The draft's cursor bytes are the only preview source.</summary>
public sealed record SectionMode(SectionDraftView Draft, byte[] BaseBytes, EntryOrigin Origin,
    SessionAssessment? Assessment = null, string? FinishReason = null)
{
    public bool IsDirty => !Draft.Bytes.AsSpan().SequenceEqual(BaseBytes);
    public bool CanFinish => IsDirty && Assessment?.Status == GeometryStatus.Certified;
    public SectionStepReport? LastReport => Draft.Last;
}

/// <summary>Plan coordinates are metres; screen distance is used only for gesture activation.</summary>
public sealed record PlanCamera(double PixelsPerMeter = 1000, double PanSpanPixels = 0, double PanAftPixels = 0);

public abstract record GestureOutcome
{
    public sealed record Committed(string AcceptedId, string Report) : GestureOutcome;
    public sealed record Refused(string Code, string Copy) : GestureOutcome;
    public sealed record Cancelled(string Copy) : GestureOutcome;
    public sealed record NoChange : GestureOutcome;
}

public abstract record CommitOutcome
{
    public sealed record Committed(string AcceptedId, string Report) : CommitOutcome
    {
        public PointRef? SelectPoint { get; init; }
        public bool ClearPointSelection { get; init; }
    }
    public sealed record Refused(string Code, string Copy) : CommitOutcome;
}

/// <summary>Label/value view of the open draft's construction, import, and thickness reports.</summary>
public sealed record SectionReport(string Kind, IReadOnlyList<SectionReportLine> Lines, bool Certified)
{
    public static SectionReport? Create(SessionAssessment assessment)
    {
        var lines = new List<SectionReportLine>();
        var kinds = new List<string>();
        if (assessment.Construction is { } construction)
        {
            kinds.Add("construction");
            lines.Add(new("Max deviation", FormatNumber(construction.MaxDeviation)));
            lines.Add(new("Tolerance", FormatTolerance(construction.Tolerance)));
            lines.Add(new("Vertex count", construction.VertexCount.ToString(CultureInfo.InvariantCulture)));
        }
        if (assessment.ImportReport is { } import)
        {
            kinds.Add("import");
            lines.Add(new("Max residual", FormatNumber(import.MaxResidual)));
            lines.Add(new("Vertex count", import.VertexCount.ToString(CultureInfo.InvariantCulture)));
            lines.Add(new("Accepted", import.Accepted ? "yes" : "no"));
            lines.Add(new("Provenance", import.Provenance));
        }
        if (assessment.Thickness is { } thickness)
        {
            kinds.Add("thickness");
            lines.Add(new("Target η", JoinNumbers(thickness.TargetEta)));
            lines.Add(new("Target t/c", JoinNumbers(thickness.TargetThickness)));
            lines.Add(new("Residuals", JoinNumbers(thickness.Residuals)));
            lines.Add(new("Affected η",
                thickness.AffectedEtaStart.ToString("G6", CultureInfo.InvariantCulture) + "–" +
                thickness.AffectedEtaEnd.ToString("G6", CultureInfo.InvariantCulture)));
        }
        if (assessment.Status != GeometryStatus.Certified)
        {
            if (kinds.Count == 0) kinds.Add("refused");
            string reason = assessment.Diagnostics.Count == 0
                ? assessment.Code
                : string.Join(" ", assessment.Diagnostics.Select(item =>
                    string.IsNullOrWhiteSpace(item.Reason) ? item.Code : item.Reason));
            lines.Add(new("Refusal", reason));
        }
        return lines.Count == 0 ? null : new(string.Join('+', kinds), lines, assessment.Status == GeometryStatus.Certified);
    }

    private static string FormatNumber(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string FormatTolerance(double? tolerance) =>
        tolerance is not double value ? "not set" :
        value == 1e-4 ? "1e-4" :
        value.ToString("G17", CultureInfo.InvariantCulture);

    private static string JoinNumbers(IReadOnlyList<double> values) =>
        values.Count == 0 ? "none" : string.Join(", ", values.Select(value => value.ToString("G6", CultureInfo.InvariantCulture)));
}

/// <summary>One adapter over the core session, certificate and store. The viewport owns no source model.</summary>
public sealed class WorkbenchController : IDisposable
{
    private AuthoringSession session = new();
    private IProjectStore store;
    private readonly Func<AuthoringSession, IProjectStore> storeFactory;
    private readonly Func<long, Task>? sectionAssessmentGate;
    // Test seam (CTL): runs on the thread a section step applies on, before Core's patch; it may hold the step, never replace it.
    private readonly Action<long>? sectionStepGate;
    private readonly SectionLibrary? sections;
    private readonly Action<int>? previewGate;
    private readonly Action? libraryGate;
    private int previewTicket;
    private int previewBusy;
    private CatalogChoice? previewWaiting;
    private ReplaceScope previewWaitingScope;
    private int previewWaitingTicket;
    private string? previewDraftId;
    private long previewGeneration;
    private readonly Dictionary<string, string> replaceNames = new(StringComparer.Ordinal);
    private IReadOnlyList<CatalogEntry>? catalogRows;
    private SessionDraft? draft;
    private AuthoredProjection? draftProjection;
    private string? projectedDraftId;
    private long projectedDraftGeneration;
    private SessionAssessment? currentAssessment;
    private CancellationTokenSource? activeSampling;
    private long stateVersion;
    private double interiorEta = .5;
    private string? expectedDiskSha;
    private byte[]? uncertainImage;
    private string? uncertainPath;
    private string? uncertainDraftId, uncertainAcceptedId;
    private long uncertainDraftGeneration;
    private string? savedDraftId, savedAcceptedId;
    private long savedDraftGeneration;
    private int saving;
    private DisplayFrame? acceptedFrame;
    private bool disposed;
    private bool draftInputValid = true;
    private readonly Dictionary<int, (string Key, ProfileView View)> sectionViews = new();
    private CancellationTokenSource? sectionAssessmentCancellation;
    private long sectionAssessmentTicket;
    // §7 Concurrency (release-freeze, 2026-10-04): a step's Core patch took 173-219 ms on the UI thread at load 34, and
    // Readiness_SectionStepApply_Under5Ms missed (55.7 ms), so steps, undo and redo apply on the thread pool. They queue:
    // each runs after the one before it has landed, so none is dropped and each names the generation current when it runs.
    private Task sectionStepTail = Task.CompletedTask;
    private Task sectionAssessmentCall = Task.CompletedTask;
    private int sectionStepsPending;
    private const string SectionChecking = "Checking…";
    // Certificate Finish showed before a step published "Checking…". A refusal leaves the bytes
    // unchanged, so Finish keeps it until the re-check returns. A later queued step must not
    // overwrite this with the placeholder. Cleared when a step lands and when the editor exits.
    private SectionMode? sectionBeforeChecking;

    private long openRequestGeneration;
    private bool isNotifying;
    private Selection? queuedSelection;
    private PointRef? gesturePoint;
    private PointView? gestureOrigin;
    private GestureInput? gestureInput;
    private (double Span, double Aft)? pendingGestureTarget;
    private bool gestureFrameScheduled;
    private readonly List<double> gestureUpdateTimes = new();
    private readonly List<double> gestureEstimateTimes = new();
    private long gestureStarted;
    private int gestureFrames;
    private int gestureClamped;
    private string? gestureClampReason;
    private string? announcedLimit;
    // Design group-move §3: the members of a group gesture (null for a point), what stops it at its threshold, the members
    // the last frame moved, and the selection a click without a drag collapsed (a double-click restores it, Ruling 111).
    private IReadOnlyList<PointRef>? gestureGroup;
    private string? groupRefusal;
    private IReadOnlyList<string> gestureMoved = [];
    private (IReadOnlyList<PointRef> Group, long At)? collapsedGroup;
    private (string Text, ReportKind Kind, long Version)? stripBeforeHold;
    private Task<GestureOutcome>? pendingCommit;
    private Task<CommitOutcome>? pendingDirectCommand;
    private string? gestureOperationId;

    // The surface channel (M1.2b2 §7): latest-wins, single-flight, keyed by a monotonic ticket. It has its own
    // cancellation source and counter and never touches stateVersion, activeSampling or the commit path.
    private readonly SurfaceCompute surfaceCompute;
    private readonly TimeProvider time;
    private long surfaceTicket;
    private long surfaceSettledTicket;
    private long surfaceRequestedAt;
    private string? surfaceKey;
    private SurfaceRequest? surfaceWaiting;
    private CancellationTokenSource? surfaceRunning;
    private bool surfaceBusy;
    private ITimer? surfaceBehindTimer;
    private TaskCompletionSource surfaceSettled = SettledSource();
    private bool surfaceWanted;
    private ViewLayout layout = ViewLayout.Plan3d;
    private ViewArrangement arrangementBeforeOne = ViewArrangement.Plan3d;
    private ViewCamera? camera3d;
    private readonly Dictionary<SingleView, ViewCamera> elevationCameras = new();
    private readonly Dictionary<SingleView, DisplayMode> displayModes = new();
    private readonly Dictionary<string, CurveView> channelViews = new(StringComparer.Ordinal);
    private string? channelViewsKey;
    private ShellMode areaMode = ShellMode.Workspace;
    private ShellMode returnToCadMode = ShellMode.Workspace;
    private readonly IWingMethod analysisMethod;
    private readonly IEvaluationBarrier? analysisBarrier;
    private AnalysisService analysisService;
    private CancellationTokenSource? analysisCancellation;
    private OperatingPoint analysisOp = OperatingPoints.Custom(5.14, 2, null);
    private WaterRecord analysisWater = WaterTable.At(OperatingPoints.DefaultTemperatureC, OperatingPoints.SaltSalinityGPerKg);
    private AnalysisViewModel? analysisView;
    private string? analysisProjectionKey;
    private string? projectionFeedKey;
    private RunFeed projectionFeed = NoFeed;
    private readonly HashSet<string> hiddenLayers = new(StringComparer.Ordinal);

    /// <summary>The model area's CAD or Analysis state; a section draft remains open while hidden.</summary>
    public ShellMode AreaMode => areaMode == ShellMode.Analysis ? areaMode : Section is null ? ShellMode.Workspace : ShellMode.SectionEditor;
    public bool IsAnalysis => AreaMode == ShellMode.Analysis;
    /// <summary>The selected run's visible layers, empty before the first result.</summary>
    public IReadOnlyList<LayerData> LayerSet { get; private set; } = [];
    /// <summary>Layers pane visibility (§18.5 row 30). The flag is controller state; the projection reads it, so it survives a re-projection.</summary>
    public bool IsLayerVisible(string layerId) => !hiddenLayers.Contains(layerId);

    /// <summary>A layer's visibility changed.</summary>
    public event Action? LayersChanged;

    public void SetLayerVisible(string layerId, bool visible)
    {
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        if (visible ? !hiddenLayers.Remove(layerId) : !hiddenLayers.Add(layerId)) return;
        analysisProjectionKey = null;
        LayersChanged?.Invoke();
    }

    private Units analysisUnits = Units.Metric;

    /// <summary>The display units of the Analysis results and the conditions band (Ruling 101 3d): N and m/s, or lbf and kn.</summary>
    public Units AnalysisUnits
    {
        get => analysisUnits;
        set
        {
            if (analysisUnits == value) return;
            analysisUnits = value;
            analysisProjectionKey = null;
            LayersChanged?.Invoke();
            UnitsChanged?.Invoke();
        }
    }

    /// <summary>The display units changed (Ruling 115): the View ▸ Units items and the status-bar item follow it.</summary>
    public event Action? UnitsChanged;

    public OperatingPoint AnalysisOperatingPoint => analysisOp;
    public WaterRecord AnalysisWater => analysisWater;
    public bool AnalysisRunning => analysisCancellation is not null;
    public RunState AnalysisState => AnalysisRunning ? RunState.Running : AnalysisView.State;

    /// <summary>The selected run is projected against the current accepted source and pending conditions on read.</summary>
    public AnalysisViewModel AnalysisView
    {
        get
        {
            if (Inspection is null)
                return new AnalysisViewModel(RunState.NoResult, "Analysis: no result", null, null, [], [], null);
            long started = time.GetTimestamp();
            var snapshot = session.Snapshot();
            var current = Freshness.Current(snapshot, analysisWater, analysisOp, analysisMethod.Method, analysisMethod.Settings);
            var selected = session.ReadRuns().Runs.LastOrDefault();
            string key = Freshness.CurrentKey(current) + ":" + selected?.Run.RunId + ":" + selected?.Integrity + ":" + AnalysisRunning + ":" + analysisUnits;
            if (analysisProjectionKey == key && analysisView is not null) return analysisView;
            var previous = selected?.Run.Outcome is RunOutcome.Failed
                ? session.ReadRuns().Runs.Reverse().Skip(1).FirstOrDefault(row =>
                    row.Integrity == RunIntegrity.Intact && row.Run.Outcome is RunOutcome.Completed)?.Run : null;
            // The feed is a function of the run and its own revision only, so a layer toggle or a new operating point does not
            // re-derive it (measured 0.9 s on the default lattice). A Failed latest attempt shows the previous Completed run
            // (AnalysisProjection.Build, "Historical — previous result"), so that run is the one fed.
            var feedRun = selected is { Integrity: RunIntegrity.Intact } ? selected.Run.Outcome is RunOutcome.Failed ? previous : selected.Run : null;
            string feedKey = feedRun?.RunId ?? "";
            if (feedKey != projectionFeedKey)
            {
                projectionFeed = feedRun is null ? NoFeed : DeriveFeed(feedRun, snapshot, session.AcceptedSourceOf);
                projectionFeedKey = feedKey;
            }
            var view = AnalysisProjection.Build(selected?.Run, current, analysisUnits,
                new ProjectionContext(projectionFeed.Verdicts, projectionFeed.Stations, projectionFeed.RootThicknessRatio, Integrity: selected?.Integrity ?? RunIntegrity.Intact,
                    PreviousCompleted: previous, HiddenLayers: hiddenLayers.ToHashSet(StringComparer.Ordinal),
                    StripNormals: projectionFeed.StripNormals, FeedUnavailable: projectionFeed.Unavailable,
                    SectionTier: projectionFeed.SectionTier, SectionFailureCode: projectionFeed.SectionFailureCode,
                    Revision: feedRun is null ? null : session.RevisionOf(feedRun.Inputs.AcceptedId),
                    HistoricalText: selected is { Integrity: RunIntegrity.Intact } &&
                        selected.Run.Outcome is RunOutcome.Completed && RunRecord.RecomputedKey(selected.Run) != Freshness.CurrentKey(current)
                        ? HistoricalBanner(selected.Run, current) : null));
            if (AnalysisRunning)
                view = view with { State = RunState.Running, StatusText = "Analysis: Running", ErrorCard = null };   // a failed attempt's card is not shown while the next one runs
            LayerSet = view.Layers;
            analysisProjectionKey = key;
            analysisView = view;
            session.RecordAnalysisEvent("analysis.project", "OK", time.GetElapsedTime(started).TotalMilliseconds,
                new AnalysisEvent { Tier = "vlm-strip", RunKey12 = view.RunKey is { Length: >= 12 } runKey ? runKey[..12] : view.RunKey,
                    Freshness = view.State.ToString(), WhatChanged = selected is null ? null :
                        string.Join(",", Freshness.WhatChanged(selected.Run, current)), LayersDrawn = LayerSet.Count });
            return view;
        }
    }

    /// <summary>
    /// What the projection reads that the durable run does not hold, derived on read (design §3: stored facts only; DM7).
    /// <paramref name="Unavailable"/> carries the reason when the run's revision is not held (never another revision's data).
    /// </summary>
    public sealed record RunFeed(IReadOnlyList<StripVerdict>? Verdicts, IReadOnlyList<StationFrame>? Stations, double? RootThicknessRatio,
        IReadOnlyList<Loads.Vec>? StripNormals, string? Unavailable, SectionTierResult? SectionTier, string? SectionFailureCode = null);

    private static readonly RunFeed NoFeed = new(null, null, null, null, null, null);

    /// <summary>
    /// The feed of <paramref name="run"/> from the accepted source of its own revision (<c>run.Inputs.AcceptedId</c>): the current
    /// source when that is the run's revision, else the revision <paramref name="sourceOf"/> holds. A revision not held yields
    /// the Unavailable reason and nothing else. A missing input stays missing; the projection prints Unavailable, never a guess.
    /// </summary>
    public static RunFeed DeriveFeed(AnalysisRun run, SessionView current, Func<string, byte[]?> sourceOf)
    {
        if (run.Outcome is not RunOutcome.Completed) return NoFeed;
        byte[]? source = run.Inputs.AcceptedId == current.AcceptedId ? current.Source : sourceOf(run.Inputs.AcceptedId);
        if (source is null) return NoFeed with { Unavailable = Labels.FeedRevisionNotHeld };
        try
        {
            var verdicts = MethodRecord.DeriveVerdicts(run, source);
            var normals = MethodRecord.DeriveNormals(run, source);
            SectionTierResult sectionTier = SectionTier.Derive(run, source);
            if (run.Settings.SectionEtas is not { Count: > 0 } etas) return new(verdicts, null, null, normals, null, sectionTier);
            double[] all = etas.Append(0).Distinct().Order().ToArray();
            var frames = Placement.Sections(source, all, [0d, 1d], CancellationToken.None).Select(section => section.Frame).ToArray();
            return new(verdicts, frames.Where((_, i) => etas.Contains(all[i])).ToArray(), frames[0].ThicknessRatio, normals, null, sectionTier);
        }
        catch (ContractError error) { return NoFeed with { SectionFailureCode = error.Code }; }   // Row 10: the projection tells a failed solve (COPY-358) from no profile (COPY-357)   // Row 10: the projection tells a failed solve (COPY-358) from no profile (COPY-357)
    }

    private string HistoricalBanner(AnalysisRun run, CurrentInputs current)
    {
        var changed = Freshness.WhatChanged(run, current);
        if (changed.Contains("surface"))
        {
            var was = session.RevisionOf(run.Inputs.AcceptedId);
            var now = session.RevisionOf(current.Inputs.AcceptedId);
            return $"Historical — geometry changed (r{was.Ordinal} → r{now.Ordinal})";
        }
        if (changed.Contains("op.alphaDeg"))
            return string.Create(CultureInfo.InvariantCulture,
                $"Historical — operating point changed (α {run.Op.AlphaDeg:0.00}° → {analysisOp.AlphaDeg:0.00}°)");
        return "Historical — " + string.Join(", ", changed);
    }

    /// <summary>Changes the pending Custom point. A prior run remains Historical until Evaluate is pressed.</summary>
    public void SetAnalysisConditions(OperatingPoint op, WaterRecord water)
    {
        OperatingPoints.Validate(op);
        OperatingPoints.Validate(water);
        analysisOp = op;
        analysisWater = water;
        analysisProjectionKey = null;
        Notify();
    }

    /// <summary>
    /// Find operating α (ANA-05, DR-DXM-6): re-solves the lattice for the accepted source at the pending conditions, off the UI thread.
    /// It records no run and changes no condition; Apply is <see cref="ApplyFoundAlpha"/> and Evaluate stays explicit.
    /// </summary>
    public Task<FindAlphaOutcome> FindAlphaAsync(double targetCl, double lowerDeg, double upperDeg, CancellationToken cancellation = default)
    {
        var op = analysisOp;
        var water = analysisWater;
        byte[] source = session.Snapshot().Source;
        double? hOverC = op.HRef is { } depth && Estimates?.MeanChordMeters is > 0 ? depth / Estimates.MeanChordMeters : null;
        return Task.Run(() => FindAlpha.Run(FindAlpha.ClAt(analysisMethod, source, op, water, cancellation), targetCl, lowerDeg, upperDeg,
            hOverC, cancellation: cancellation), cancellation);
    }

    /// <summary>Apply of the Find α dialog: writes α into the pending conditions only. A prior run stays Historical until Evaluate.</summary>
    public void ApplyFoundAlpha(double alphaDeg) => SetAnalysisConditions(analysisOp with { AlphaDeg = alphaDeg }, analysisWater);

    /// <summary>The only compute entry in the desktop: Cancel records no run, and a failure keeps prior evidence.</summary>
    public async Task<AnalysisRun?> EvaluateAnalysisAsync(OperatingPoint op, WaterRecord water, CancellationToken cancellation = default)
    {
        if (analysisCancellation is not null) return null;
        SetAnalysisConditions(op, water);
        var running = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        analysisCancellation = running;
        analysisProjectionKey = null;
        SetStatus($"Evaluating — VLM + strip · {2 * analysisMethod.Settings.NSpanPerHalf * analysisMethod.Settings.NChord} panels…", ReportKind.Info);
        Notify();
        try
        {
            var run = await analysisService.EvaluateAsync(op, water, Tier.VlmStrip, new Scope.Wing(), running.Token);
            if (run.Outcome is RunOutcome.Failed failed)
                SetStatus($"Analysis failed — {failed.Reason} ({failed.Code}). The previous result is kept as Historical.", ReportKind.Error);
            else
                SetStatus(string.Create(CultureInfo.InvariantCulture, $"Analysis complete — VLM + strip · {run.WallMs / 1000:0.###} s"), ReportKind.Info);
            return run;
        }
        catch (OperationCanceledException) { SetStatus("Analysis cancelled.", ReportKind.Info); return null; }
        catch (ContractError error) { SetStatus(error.Code + ": " + (error.Reason ?? error.Message), ReportKind.Warning); return null; }
        finally
        {
            if (ReferenceEquals(analysisCancellation, running)) analysisCancellation = null;
            running.Dispose();
            analysisProjectionKey = null;
            if (!disposed && Inspection is not null) _ = AnalysisView;
            Notify();
        }
    }

    public void CancelAnalysis() => analysisCancellation?.Cancel();
    public const string AnalysisPointRefusal = "Points are edited in CAD. Switch with the CAD | Analysis toggle.";

    /// <summary>A shared refusal at every controller edit boundary, including the Points pane's typed gesture.</summary>
    private bool RefuseAnalysisEdit()
    {
        if (!IsAnalysis) return false;
        SetStatus(AnalysisPointRefusal, ReportKind.Info);
        Notify();
        return true;
    }

    /// <summary>Switches the area over the same selection, cameras, layout and document. Evaluation is explicit.</summary>
    public void ToggleAnalysis()
    {
        long started = time.GetTimestamp();
        ShellMode from = AreaMode;
        if (IsAnalysis)
            areaMode = returnToCadMode == ShellMode.SectionEditor && Section is null ? ShellMode.Workspace : returnToCadMode;
        else
        {
            returnToCadMode = from;
            areaMode = ShellMode.Analysis;
        }
        if (Inspection is not null) _ = AnalysisView;
        session.RecordAnalysisEvent("analysis.toggle", "OK", time.GetElapsedTime(started).TotalMilliseconds,
            new AnalysisEvent { From = from.ToString(), To = AreaMode.ToString(), LayersDrawn = LayerSet.Count });
        Notify();
    }

    /// <summary>Computes one display mesh off the UI thread; the default is <see cref="Placement.Surface"/>.</summary>
    public delegate Task<SurfaceView> SurfaceCompute(byte[] source, string basis, long generation, CancellationToken cancellation);

    private sealed record SurfaceRequest(long Ticket, byte[] Bytes, string Basis, long Generation);

    /// <summary>A mesh request older than this shows "· Updating…" (TQ reactive recompute).</summary>
    public static readonly TimeSpan SurfaceBehindAfter = TimeSpan.FromMilliseconds(250);

    public const string SurfaceKeptNote = "Showing the last shape that could be drawn.";

    public WorkbenchController(Func<AuthoringSession, IProjectStore>? storeFactory = null,
        SurfaceCompute? surfaceCompute = null, TimeProvider? time = null, Func<long, Task>? sectionAssessmentGate = null,
        Action<long>? sectionStepGate = null, SectionLibrary? sections = null, Action<int>? previewGate = null,
        Action? libraryGate = null, IWingMethod? analysisMethod = null, IEvaluationBarrier? analysisBarrier = null)
    {
        this.storeFactory = storeFactory ?? (active => new ProjectStore(active));
        store = this.storeFactory(session);
        this.surfaceCompute = surfaceCompute ?? ((source, basis, generation, cancellation) =>
            Task.Run(() => Placement.Surface(source, basis, generation, cancellation), cancellation));
        this.time = time ?? TimeProvider.System;
        this.analysisMethod = analysisMethod ?? new ProductWingMethod();
        this.analysisBarrier = analysisBarrier;
        analysisService = new AnalysisService(session, this.analysisMethod, analysisBarrier, this.time);
        this.sectionAssessmentGate = sectionAssessmentGate;
        this.sectionStepGate = sectionStepGate;
        this.sections = sections ?? App.Sections;
        this.previewGate = previewGate;
        this.libraryGate = libraryGate;
    }
    /// <summary>Document, selection, status, estimate and layout changes; the shell rebuilds its panes on each (≈ 25 ms).</summary>
    public event Action? Changed;

    /// <summary>
    /// One view's camera alone moved (<see cref="PlanCamera"/>, <see cref="Camera3d"/>, an elevation camera); the argument
    /// names that view. The model-area views subscribe, each acting on its own camera only, so a wheel step, key step or
    /// pinch redraws that view alone and never rebuilds the shell's panes.
    /// </summary>
    public event Action<SingleView>? CameraChanged;

    public Selection Selection { get; private set; } = new Selection.None();
    public event Action? SelectionChanged;
    public event Action? SectionChanged;
    public SectionMode? Section { get; private set; }
    public (SurfaceSide Side, double ChordX, double DeviationMeters, double LimitMeters)? SectionRefitRefusal { get; private set; }
    public WingEstimates? Estimates { get; private set; }
    public PlanformView? Planform => Inspection is null ? null : CfdWorkbench.Core.Planform.View(
        !IsAnalysis ? draft?.Bytes ?? session.Snapshot().Source : session.Snapshot().Source,
        draft is null || IsAnalysis ? "accepted" : "preview", IsAnalysis ? 0 : draft?.Generation ?? 0);
    public GestureState Gesture
    {
        get;
        private set
        {
            field = value;
            // The preview belongs to a live drag: release, Escape, a refusal or a new gesture clear it.
            if (value != GestureState.Dragging) GestureCrossing = null;
            if (value is not (GestureState.Dragging or GestureState.Nudging))
            {
                GestureLimit = null; announcedLimit = null; stripBeforeHold = null;
                GestureBinding = null; GestureApplied = null; GestureRequested = null; GroupHold = null;
            }
        }
    }

    /// <summary>The members of the group gesture in progress, or null for a point gesture or none.</summary>
    public IReadOnlyList<PointRef>? GestureGroup => Gesture == GestureState.Idle ? null : gestureGroup;

    /// <summary>True when a plain press on <paramref name="point"/> would drag the whole selection (design §3.2, DR-GM-4 A).</summary>
    public bool IsGroupMember(PointRef point) =>
        Selection is Selection.Points { Items.Count: > 1 } picked && picked.Items.Contains(point);

    /// <summary>Design §3.3: the member that stops the group (the end vertex at its chord limit, or the unselected neighbour), or null.</summary>
    public GroupBinding? GestureBinding { get; private set; }

    /// <summary>Design §3.3: where the grabbed point is now (the applied position, not the pointer's), or null.</summary>
    public (double SpanMeters, double Ordinate)? GestureApplied { get; private set; }

    /// <summary>Design §3.3: where the pointer asked the grabbed point to go; only the tether shows it.</summary>
    public (double SpanMeters, double Ordinate)? GestureRequested { get; private set; }

    /// <summary>Where the grabbed point was at the press (the gesture's origin, which the live curve no longer holds), or null.</summary>
    public (double SpanMeters, double Ordinate)? GestureOrigin => Gesture != GestureState.Idle && gestureOrigin is { } origin ? (origin.SpanMeters, origin.Ordinate) : null;

    /// <summary>The applied move and binding point of a group drag, for the inspector; null when none.</summary>
    public string? GroupHold { get; private set; }

    /// <summary>
    /// Ruling 111 (10): a double-click is two presses and a collapsing click between them. When the second press lands on a
    /// member of the group the first click collapsed, the group is selected again. Returns true when it was restored.
    /// </summary>
    public bool RestoreCollapsedGroup(PointRef point)
    {
        if (collapsedGroup is not { } collapsed || !collapsed.Group.Contains(point) ||
            Stopwatch.GetElapsedTime(collapsed.At).TotalMilliseconds > 700 ||
            Selection is not Selection.Points { Items: [var only] } || only != point) return false;
        collapsedGroup = null;
        Select(new Selection.Points(collapsed.Group));
        return true;
    }

    /// <summary>
    /// Ruling 96: the planform limit holding the drag or nudge run in progress (the tip at its minimum, the root at its maximum,
    /// a legacy tip that can't go lower), or null. Core decides it from <see cref="TipChord"/>; the Desktop only shows it.
    /// </summary>
    public GestureLimit? GestureLimit { get; private set; }

    /// <summary>The one sentence for the held limit, shared by the status strip, the marker and the point's accessible name; null when none holds.</summary>
    public string? GestureLimitText => GestureLimit is { } limit ? TipChord.HoldText(limit) : null;

    /// <summary>The point the held limit belongs to, or null.</summary>
    public PointRef? GestureLimitPoint => GestureLimit is null ? null : GestureBinding?.Point ?? gesturePoint;

    /// <summary>
    /// The advisory edge-crossing preview for the drag in progress (§0.1 step 6): where the release would be refused
    /// because the rails' Bernstein hulls overlap, or null. Binary64 mirror of the certificate's
    /// <c>trailingLower &gt; leadingUpper</c> rule in <c>Geometry.Assess</c>; advisory only — the certificate decides.
    /// </summary>
    public (double SpanMeters, double Ordinate)? GestureCrossing { get; private set; }

    public PlanCamera PlanCamera
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyCamera(SingleView.Plan);
        }
    } = new();
    public bool CombVisible { get; set; }
    public int LastGestureFrames { get; private set; }

    /// <summary>The last ended gesture had a frame Core clamped (a twist or t/c past the domain, MC-19); read by Properties' echo.</summary>
    public bool LastGestureClamped { get; private set; }

    /// <summary>The newest completed display mesh: the accepted revision's, or the draft's during a gesture.</summary>
    public SurfaceView? Surface { get; private set; }

    /// <summary>"Showing the last shape that could be drawn." after a mesh failed and an earlier one is kept; else null.</summary>
    public string? SurfaceNote { get; private set; }

    /// <summary>Derived from tickets: the newest issued request has not settled yet.</summary>
    public bool SurfaceUpdating => Interlocked.Read(ref surfaceTicket) != Interlocked.Read(ref surfaceSettledTicket);

    /// <summary>The views show "· Updating…": a request has been outstanding for <see cref="SurfaceBehindAfter"/> or more.</summary>
    public bool SurfaceBehind => SurfaceUpdating && time.GetElapsedTime(surfaceRequestedAt) >= SurfaceBehindAfter;

    /// <summary>Completes when the newest issued mesh request has settled (shown, failed or replaced by none).</summary>
    public Task WhenSurfaceSettledAsync() => surfaceSettled.Task;

    /// <summary>
    /// Set by the model area while a view that draws the mesh (3D, Side, Front) is on screen. Meshes are computed only
    /// then, so a controller with no such view (the Plan alone, a headless test) never spends the 13–24 ms per change.
    /// </summary>
    public bool SurfaceWanted
    {
        get => surfaceWanted;
        set
        {
            if (surfaceWanted == value) return;
            surfaceWanted = value;
            if (!value) surfaceKey = null;
            Notify();
        }
    }

    /// <summary>Session value (Type-1, not persisted; M1.2e persists layouts).</summary>
    public ViewLayout Layout
    {
        get => layout;
        set
        {
            if (layout == value) return;
            if (value.Arrangement == ViewArrangement.One && layout.Arrangement != ViewArrangement.One)
                arrangementBeforeOne = layout.Arrangement;
            layout = value;
            Notify();
        }
    }

    /// <summary>The arrangement One view returns to; the One-view picker names it ("↩ Back to …", DR-VIEW-11).</summary>
    public ViewArrangement ArrangementBeforeOne => arrangementBeforeOne;

    /// <summary>Double-click or Return on a view label: that view alone, and again back to the layout before it.</summary>
    public void ToggleOneView(SingleView view) =>
        Layout = layout.Arrangement == ViewArrangement.One && layout.Single == view
            ? new ViewLayout(arrangementBeforeOne, SingleView.Plan)
            : ViewLayout.One(view);

    /// <summary>The view that Display ▾, zoom and fit commands act on (its label was clicked last).</summary>
    public SingleView TargetView
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Notify();
        }
    } = SingleView.Plan;

    /// <summary>The 3D view's one camera; null until the view first fits the Iso camera to a mesh.</summary>
    public ViewCamera? Camera3d
    {
        get => camera3d;
        set
        {
            if (camera3d == value) return;
            camera3d = value;
            NotifyCamera(SingleView.ThreeD);
        }
    }

    /// <summary>Front and Side keep their own pan and zoom; their direction is fixed.</summary>
    public ViewCamera? CameraFor(SingleView elevation) =>
        elevationCameras.TryGetValue(elevation, out var camera) ? camera : null;

    public void SetCameraFor(SingleView elevation, ViewCamera camera)
    {
        if (elevation is not (SingleView.Front or SingleView.Side))
            throw new ArgumentOutOfRangeException(nameof(elevation), elevation, "Only Front and Side keep an elevation camera.");
        if (elevationCameras.TryGetValue(elevation, out var current) && current == camera) return;
        elevationCameras[elevation] = camera;
        NotifyCamera(elevation);
    }

    public DisplayMode DisplayFor(SingleView view) => displayModes.GetValueOrDefault(view, DisplayMode.Shaded);

    public void SetDisplay(SingleView view, DisplayMode mode)
    {
        if (DisplayFor(view) == mode) return;
        displayModes[view] = mode;
        Notify();
    }

    /// <summary>The 3D view is on screen (the <c>gesture.end</c> field that reads the drag budget with 3D open).</summary>
    public bool ThreeDVisible => surfaceWanted && layout.Shows(SingleView.ThreeD);

    /// <summary>Try again after a failed mesh: a new request for the current source.</summary>
    public void RefreshSurface()
    {
        surfaceKey = null;
        RequestSurfaceIfChanged();
    }

    /// <summary>The selected station's starboard section, or the whole surface with its port half; null with no mesh.</summary>
    public (Point3 Minimum, Point3 Maximum)? FitBounds()
    {
        if (Surface is not { } surface) return null;
        if (Selection is Selection.Station station &&
            surface.Sections.FirstOrDefault(section => section.Eta == station.Eta) is { } placed)
            return SectionBounds(placed);
        return (new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ),
            new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ));
    }

    public static (Point3 Minimum, Point3 Maximum) SectionBounds(PlacedSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;
        foreach (var point in section.Upper.Concat(section.Lower))
        {
            minX = Math.Min(minX, point.X); minY = Math.Min(minY, point.Y); minZ = Math.Min(minZ, point.Z);
            maxX = Math.Max(maxX, point.X); maxY = Math.Max(maxY, point.Y); maxZ = Math.Max(maxZ, point.Z);
        }
        return (new Point3(minX, minY, minZ), new Point3(maxX, maxY, maxZ));
    }

    /// <summary>The points of any of the five channels in the shown revision or draft generation (rails via the Planform).</summary>
    public CurveView? CurveFor(string curve)
    {
        if (curve == "leading") return Planform?.Leading;
        if (curve == "trailing") return Planform?.Trailing;
        if (Inspection is null || !PointModel.EditableCurves.Contains(curve)) return null;
        string key = SourceKey();
        if (channelViewsKey != key)
        {
            channelViews.Clear();
            channelViewsKey = key;
        }
        if (channelViews.TryGetValue(curve, out var cached)) return cached;
        CurveView view;
        try
        {
            view = Channels.View(!IsAnalysis ? draft?.Bytes ?? session.Snapshot().Source : session.Snapshot().Source,
                curve, draft is null || IsAnalysis ? "accepted" : "preview", IsAnalysis ? 0 : draft?.Generation ?? 0);
        }
        catch (ContractError) { return null; }
        channelViews[curve] = view;
        return view;
    }

    private string SourceKey() => !IsAnalysis && draft is { } active
        ? "d:" + active.Id + ":" + active.Generation.ToString(CultureInfo.InvariantCulture)
        : "a:" + Inspection?.Authored.Binding.SourceHash;

    private void RequestSurfaceIfChanged()
    {
        if (disposed || !surfaceWanted || Inspection is null) return;
        string key = SourceKey();
        if (key == surfaceKey) return;
        surfaceKey = key;
        if (draft is { } active) IssueSurface(active.Bytes, "preview", active.Generation);
        else IssueSurface(session.Snapshot().Source, "accepted", 0);
    }

    private void IssueSurface(byte[] bytes, string basis, long generation)
    {
        long ticket = Interlocked.Increment(ref surfaceTicket);
        surfaceRequestedAt = time.GetTimestamp();
        if (surfaceSettled.Task.IsCompleted) surfaceSettled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // A newer request replaces the waiting one (it never runs) and cancels the running one.
        if (surfaceWaiting is { } replaced) RecordSurface(replaced.Basis, "stale-dropped", 0, null, null);
        surfaceWaiting = new SurfaceRequest(ticket, bytes, basis, generation);
        surfaceRunning?.Cancel();
        surfaceBehindTimer?.Dispose();
        surfaceBehindTimer = time.CreateTimer(_ => OnUiThread(Notify), null, SurfaceBehindAfter, Timeout.InfiniteTimeSpan);
        if (!surfaceBusy) StartSurface();
    }

    private void StartSurface()
    {
        if (surfaceWaiting is not { } next) return;
        surfaceWaiting = null;
        surfaceBusy = true;
        var cancellation = new CancellationTokenSource();
        surfaceRunning = cancellation;
        _ = RunSurfaceAsync(next, cancellation);
    }

    private async Task RunSurfaceAsync(SurfaceRequest request, CancellationTokenSource cancellation)
    {
        long started = Stopwatch.GetTimestamp();
        SurfaceView? view = null;
        string outcome;
        string? code = null;
        try
        {
            view = await surfaceCompute(request.Bytes, request.Basis, request.Generation, cancellation.Token);
            outcome = "ok";
        }
        catch (OperationCanceledException) { outcome = "stale-dropped"; }
        catch (ContractError error) { outcome = "error"; code = error.Code; }
        // Background boundary: a bug in the projection must not vanish unobserved; it is reported like a refusal.
        catch (Exception error) when (error is not OutOfMemoryException) { outcome = "error"; code = error.GetType().Name; }
        double milliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        OnUiThread(() => CompleteSurface(request, cancellation, view, outcome, code, milliseconds));
    }

    private void CompleteSurface(SurfaceRequest request, CancellationTokenSource cancellation, SurfaceView? view,
        string outcome, string? code, double milliseconds)
    {
        if (ReferenceEquals(surfaceRunning, cancellation)) surfaceRunning = null;
        cancellation.Dispose();
        surfaceBusy = false;
        if (disposed) return;
        bool newest = request.Ticket == Interlocked.Read(ref surfaceTicket);
        if (!newest) outcome = "stale-dropped";
        else if (outcome == "ok")
        {
            Surface = view;
            SurfaceNote = null;
        }
        else if (outcome == "error") SurfaceNote = Surface is null ? null : SurfaceKeptNote;
        if (newest) Interlocked.Exchange(ref surfaceSettledTicket, request.Ticket);
        RecordSurface(request.Basis, outcome, milliseconds, code, outcome == "ok" ? view : null);
        StartSurface();
        if (newest)
        {
            surfaceBehindTimer?.Dispose();
            surfaceBehindTimer = null;
            surfaceSettled.TrySetResult();
        }
        Notify();
    }

    private static void RecordSurface(string basis, string outcome, double milliseconds, string? code, SurfaceView? view) =>
        CfdWorkbench.Desktop.Shell.ShellEvents.Record("view.surface", outcome, milliseconds, Guid.NewGuid().ToString("N"),
            code: code, basis: basis, stations: view?.Sections.Count, chordSamples: view?.Sections[0].Upper.Count);

    private static void OnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }

    private static TaskCompletionSource SettledSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    public AuthoredProjection? CurrentProjection => DraftProjection ?? Inspection?.Authored ?? PendingProjection;

    // Changed is raised after each accepted edit and cursor move; menu commands requery these values.
    public bool CanUndo => !IsAnalysis && HistoryAvailability().Undo;
    public bool CanRedo => !IsAnalysis && HistoryAvailability().Redo;

    private (bool Undo, bool Redo) HistoryAvailability()
    {
        if (Section is { } mode) return (mode.Draft.Cursor > 0, mode.Draft.Cursor < mode.Draft.StepCount);
        if (Inspection is null || draft is not null || Gesture != GestureState.Idle) return (false, false);
        var history = session.Envelope();
        var (current, redo) = NativeProject.Replay(history);
        return (history.Accepted.Single(item => item.Id == current).Parent is not null, redo.Length != 0);
    }

    /// <summary>The point whose Remove was just refused (§11 state table: a dashed warning ring while it stays the
    /// selection). Cleared by the next point command and by any other selection.</summary>
    public PointRef? RefusedPoint { get; private set; }

    public void Select(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection is not Selection.Points { Items: [var only] } || only != RefusedPoint) RefusedPoint = null;
        if (isNotifying)
        {
            queuedSelection = selection;
            return;
        }
        queuedSelection = selection;
        Notify();
    }

    public static Selection Reconcile(Selection current, AuthoredProjection? projection) =>
        Reconcile(current, projection, null);

    private bool ChannelPointExists(PointRef item) =>
        CurveFor(item.Curve)?.Points.Any(point => point.Id == item.VertexId) == true;

    /// <summary>As <see cref="Reconcile(Selection, AuthoredProjection?)"/>; a point on a channel (dihedral, twist,
    /// thickness) is kept while <paramref name="channelPointExists"/> finds it.</summary>
    public static Selection Reconcile(Selection current, AuthoredProjection? projection, Func<PointRef, bool>? channelPointExists)
    {
        if (projection is null) return new Selection.None();
        if (current is Selection.None) return new Selection.Foil();
        if (current is Selection.Foil) return current;

        if (current is Selection.Station station)
        {
            if (station.Index >= 0 && station.Index < projection.Assignments.Count &&
                projection.Assignments[station.Index].Eta == station.Eta)
            {
                return station;
            }

            for (int i = 0; i < projection.Assignments.Count; i++)
            {
                if (projection.Assignments[i].Eta == station.Eta)
                    return new Selection.Station(i, station.Eta);
            }

            return new Selection.Foil();
        }

        if (current is Selection.Points points)
        {
            var kept = new List<PointRef>();
            foreach (var item in points.Items)
            {
                if (PointExists(item, projection) || item.Curve is not ("leading" or "trailing") &&
                    channelPointExists?.Invoke(item) == true)
                    kept.Add(item);
            }
            if (kept.Count > 0)
                return new Selection.Points(kept);
            return new Selection.Foil();
        }

        return new Selection.Foil();
    }

    private static bool PointExists(PointRef item, AuthoredProjection projection)
    {
        var rail = projection.Rails.FirstOrDefault(r => r.Name == item.Curve);
        if (rail is not null && rail.Controls.Any(c => c.Id == item.VertexId))
            return true;

        return false;
    }

    /// <summary>The section's display curve, derived from the current draft bytes.</summary>
    public CurveView? SectionCurve(SurfaceSide side) => Section is { } mode
        ? Sections.View(mode.Draft.Bytes, mode.Draft.Assignment, side, "preview", mode.Draft.Generation)
        : null;

    public Task EnterSectionAsync(int assignment, EntryOrigin origin)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (disposed) throw new ContractError("DOC-CLOSED");
        ClearReplacePreview();
        if (Section is { } open)
        {
            // A step still applying counts as an edit: switching now would drop it.
            if (open.IsDirty || SectionStepPending) throw new ContractError("DSL-DRAFT-OWNED", "Finish or cancel this section before editing another.");
            CancelSectionAssessment();
            session.Cancel(open.Draft.DraftId);
            Section = null;
            sectionBeforeChecking = null;
            draft = null;
        }
        if (draft is not null || Gesture != GestureState.Idle)
            throw new ContractError("DSL-DRAFT-OWNED");
        if (Inspection?.Geometry.Status != GeometryStatus.Certified)
            throw new ContractError("DSL-NOT-ASSESSED");
        var baseBytes = session.Snapshot().Source;
        var view = session.BeginSectionDraft(Guid.NewGuid().ToString("D"), assignment);
        draft = session.Snapshot().Draft;
        Section = new SectionMode(view, baseBytes, origin);
        SectionRefitRefusal = null;
        interiorEta = Inspection.Authored.Assignments[assignment].Eta;
        var first = SectionCurve(SurfaceSide.Upper)!.Points[0];
        Select(new Selection.Points([new PointRef("upper", first.Id, view.Profile)]));
        CfdWorkbench.Desktop.Shell.ShellEvents.Record("section.mode.enter", "OK", 0,
            Guid.NewGuid().ToString("N"), trigger: origin.ToString().ToLowerInvariant(), editKind: "section");
        SectionChanged?.Invoke();
        return Task.CompletedTask;
    }

    /// <summary>True while a section step, undo or redo is applying or queued. Finish waits for it; a strip switch is refused.</summary>
    public bool SectionStepPending => sectionStepsPending > 0;

    /// <summary>
    /// Appends one structural step, then checks its bytes with the real Core certificate. The step applies on the thread
    /// pool, after any step still applying (§7 Concurrency); the task completes when its certificate answers or a newer
    /// step supersedes it. A refused step throws its <see cref="ContractError"/>; a step whose section was cancelled,
    /// finished or disposed before it landed completes without effect (UI-LIFETIME).
    /// </summary>
    public async Task ApplySectionStepAsync(SectionStep step, CancellationToken cancellation = default)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        Task assessed = Task.CompletedTask;
        await QueueSectionStep(async mode =>
        {
            SectionDraftView next;
            SessionDraft? landed;
            try
            {
                (next, landed) = await ApplyOffUiThread(mode, () => session.ApplySectionStep(mode.Draft.DraftId, mode.Draft.Generation, step));
            }
            catch (ContractError error) when (!SectionStale(mode))
            {
                SectionRefitRefusal = error.Data["RefitMaximumChordX"] is double x &&
                    error.Data["RefitAffectedSide"] is SurfaceSide side &&
                    error.Data["RefitDeviationMeters"] is double deviation &&
                    error.Data["RefitLimitMeters"] is double limit ? (side, x, deviation, limit) : null;
                Status = error.Message;
                // The refused step left the draft unchanged. Keep the certificate this step cleared:
                // "Checking…" is for bytes that moved, and a refused step must not show it while the re-check runs.
                // DraftId is one id per draft. EnterSectionAsync mints it with Guid.NewGuid, and
                // BeginSectionDraftCore rejects an id this session already retired. A resumed draft keeps
                // that id. Generation distinguishes its steps, so the pair is the identity this guard needs.
                if (Section is { } current && sectionBeforeChecking is { } prior &&
                    current.Draft.DraftId == prior.Draft.DraftId &&
                    current.Draft.Generation == prior.Draft.Generation)
                    Section = current with { Assessment = prior.Assessment, FinishReason = prior.FinishReason };
                assessed = AssessCurrentSectionAsync();
                NotifySection();
                throw;
            }
            catch (ContractError) { return; }
            if (SectionStale(mode)) return;
            draft = landed;
            SectionRefitRefusal = null;
            if (step is SectionStep.Replace replace)
                replaceNames[next.Profile] = replace.Source.DisplayName;
            sectionBeforeChecking = null;
            Section = Section! with { Draft = next, Assessment = null, FinishReason = SectionChecking };
            if (step is SectionStep.Replace) ClearReplacePreview();
            // One shell refresh per step: the assessment's "Checking…" write notifies, after the strip has the step report.
            RaiseSectionChanged();
            assessed = AssessCurrentSectionAsync(cancellation);
        });
        await assessed;
    }

    /// <summary>Moves the section cursor back one step, after any step still applying. Never document undo.</summary>
    public Task UndoSectionStepAsync() => RefuseAnalysisEdit() ? Task.CompletedTask : MoveSectionCursorAsync(-1);

    /// <summary>Moves the section cursor forward one step, after any step still applying.</summary>
    public Task RedoSectionStepAsync() => RefuseAnalysisEdit() ? Task.CompletedTask : MoveSectionCursorAsync(1);

    private Task MoveSectionCursorAsync(int delta) => QueueSectionStep(async mode =>
    {
        SectionDraftView next;
        SessionDraft? landed;
        try
        {
            (next, landed) = await ApplyOffUiThread(mode, () => delta < 0
                ? session.UndoSectionStep(mode.Draft.DraftId)
                : session.RedoSectionStep(mode.Draft.DraftId));
        }
        catch (ContractError) when (SectionStale(mode)) { return; }
        if (SectionStale(mode)) return;
        draft = landed;
        SectionRefitRefusal = null;
        bool moved = next.Cursor != mode.Draft.Cursor;
        if (moved) sectionBeforeChecking = null;
        Section = Section! with
        {
            Draft = next, Assessment = null,
            FinishReason = moved ? SectionChecking : delta < 0 ? "No earlier step." : "No later step."
        };
        // A moved cursor refreshes the shell once, through the assessment's "Checking…" write; a no-op says so at once.
        if (!moved) { NotifySection(); return; }
        RaiseSectionChanged();
        _ = AssessCurrentSectionAsync();
    });

    /// <summary>
    /// Queues one step, undo or redo behind the one applying. Finish goes off now, until the certificate of the landed
    /// bytes answers. <paramref name="land"/> runs on the UI thread with the section as it is once the queue reaches it.
    /// </summary>
    private Task QueueSectionStep(Func<SectionMode, Task> land)
    {
        if (Section is not { } mode) return Task.FromException(new ContractError("DSL-DRAFT-OWNED"));
        Interlocked.Increment(ref previewTicket);
        CancelSectionAssessment();
        // Drop the painted preview. Keep the choice so a later Replace still refuses DSL-STALE.
        ClearReplacePreview(keepApplyTarget: true);
        if (mode.Assessment is not null || mode.FinishReason != SectionChecking)
        {
            if (mode.FinishReason != SectionChecking)
                sectionBeforeChecking = mode;
            Section = mode with { Assessment = null, FinishReason = SectionChecking };
            // The mode bar's Finish and reason follow at once; the panes' inputs are unchanged, so no shell refresh.
            RaiseSectionChanged();
        }
        sectionStepsPending++;
        var run = RunQueuedAsync(sectionStepTail, mode.Draft.DraftId, land);
        sectionStepTail = run;
        return run;
    }

    private async Task RunQueuedAsync(Task previous, string draftId, Func<SectionMode, Task> land)
    {
        try
        {
            // The step before reports its own outcome to its caller; this one runs whatever it was.
            await previous.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
            // UI-LIFETIME: Cancel, Finish, a rebind or Dispose before the queue reached this step ends it unapplied.
            if (disposed || Section is not { } mode || mode.Draft.DraftId != draftId) return;
            CancelSectionAssessment();
            await land(mode);
        }
        finally { sectionStepsPending--; }
    }

    /// <summary>
    /// Runs one Core cursor or patch call on the thread pool and returns its view with the session's draft. It also fills
    /// the facts memo for the new bytes at every station: the landing's strip report, station strip and Properties read
    /// them, and computed there they cost ~33 ms each on the UI thread under load.
    /// </summary>
    private Task<(SectionDraftView View, SessionDraft? Draft)> ApplyOffUiThread(SectionMode mode, Func<SectionDraftView> apply)
    {
        int stations = Inspection?.Authored.Assignments.Count ?? 0;
        return Task.Run(() =>
        {
            sectionStepGate?.Invoke(mode.Draft.Generation);
            var view = apply();
            try
            {
                for (int station = 0; station < stations; station++) _ = Sections.Facts(view.Bytes, station);
            }
            catch (ContractError) { }   // the UI-thread reader meets the same refusal and reports it as it always has
            return (view, session.Snapshot().Draft);
        });
    }

    // A result belongs to the section it was asked of: Cancel, Finish, a rebind or Dispose since then discards it.
    private bool SectionStale(SectionMode asked) =>
        disposed || Section is not { } now || now.Draft.DraftId != asked.Draft.DraftId;

    /// <summary>
    /// A section pointer-drag frame (§3.7): records where the release will move the point, and nothing else. The canvas
    /// draws the frame from its own display state; a shell refresh here cost 0.55–0.9 s per move (edit-lag, 2026-10-04).
    /// </summary>
    public void UpdateSectionGesture(double x, double y, double pixelsFromPress)
    {
        if (IsAnalysis) return;
        if (Section is null || gestureInput != GestureInput.Pointer || gestureOrigin is null ||
            Gesture is not (GestureState.Pressed or GestureState.Dragging) || !double.IsFinite(x) || !double.IsFinite(y))
            return;
        if (Gesture == GestureState.Pressed)
        {
            if (pixelsFromPress < 3) return;   // the drag threshold UpdateGestureTarget applies
            Gesture = GestureState.Dragging;
        }
        pendingGestureTarget = (x, y);
    }

    private async Task AssessCurrentSectionAsync(CancellationToken cancellation = default)
    {
        if (Section is not { } mode) return;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        sectionAssessmentCancellation = linked;
        long ticket = ++sectionAssessmentTicket;
        long placeholder = statusSlot.Write("Checking section geometry…");
        Notify();
        try
        {
            // Test seam: a gate may delay the call, but the assessment itself always runs in Core.
            if (sectionAssessmentGate is not null) await sectionAssessmentGate(mode.Draft.Generation);
            // Core runs one validation at a time (DSL-VALIDATION-BUSY, swallowed below), so the calls form a chain: each starts
            // after the one before has returned, and a superseded one returns at once on its cancelled token. Raced, a quick
            // run of queued steps could leave the last one unassessed and Finish off for good.
            var call = AfterPrevious(sectionAssessmentCall, () => session.AssessSection(mode.Draft.DraftId, mode.Draft.Generation, linked.Token));
            sectionAssessmentCall = call;
            var result = await call;
            if (disposed || ticket != sectionAssessmentTicket || Section?.Draft.Generation != mode.Draft.Generation) return;
            string? reason = result.Status switch
            {
                GeometryStatus.Certified when !mode.IsDirty => "No section changes to Finish.",
                GeometryStatus.Certified => null,
                _ when result.Code == "DSL-PROFILE-CROSS" => "Upper and lower surfaces cross. Move the point back to finish.",
                GeometryStatus.NotAssessed => "This section could not be checked. Finish is unavailable.",
                _ => result.Diagnostics.FirstOrDefault()?.Reason ?? result.Code
            };
            Section = mode with { Assessment = result, FinishReason = reason };
            statusSlot.TryReplace(placeholder, reason ?? "Surfaces no longer cross. Finish is available.",
                reason is null ? ReportKind.Info : ReportKind.Warning);
            NotifySection();
        }
        catch (ContractError error) when (error.Code is "DSL-CONFLICT" or "DSL-VALIDATION-BUSY") { }
        finally
        {
            if (ReferenceEquals(sectionAssessmentCancellation, linked)) sectionAssessmentCancellation = null;
            linked.Dispose();
        }
    }

    private static async Task<SessionAssessment> AfterPrevious(Task previous, Func<SessionAssessment> assess)
    {
        await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        return await Task.Run(assess).ConfigureAwait(false);
    }

    private void CancelSectionAssessment()
    {
        ++sectionAssessmentTicket;
        sectionAssessmentCancellation?.Cancel();
        sectionAssessmentCancellation = null;
    }

    public int PreviewLandings { get; private set; }

    public int PreviewDrops { get; private set; }

    public string? PreviewSourceName { get; private set; }

    public ReplacePreview? CurrentPreview { get; private set; }

    /// <summary>A candidate shown for diagnosis only; RefusalCode is set and it is never an Apply target.</summary>
    public ReplacePreview? RefusedPreview { get; private set; }

    public bool PreviewPending => Volatile.Read(ref previewBusy) != 0;

    public CatalogChoice? PreviewChoice { get; private set; }

    public string? LastReplaceName =>
        Section?.Draft.Profile is { } profile && replaceNames.TryGetValue(profile, out string? name) ? name : null;

    /// <summary>The contract error of the latest preview when it refused; null when a preview is showing or none was asked.</summary>
    public ContractError? PreviewFault { get; private set; }

    public CatalogSnapshot? OpenedCatalog { get; private set; }

    /// <summary>DLG repaints the dashed preview. The shell subscribes and does not rebuild panes.</summary>
    public event Action? PreviewChanged;

    public string? SourceChip
    {
        get
        {
            if (Section is not { } mode) return null;
            var parsed = ProvenanceFor(mode.Draft.Bytes, mode.Draft.Profile);
            return parsed.ChipText(ChipSourceName(parsed));
        }
    }

    /// <summary>The station card's Source value, read from the profile provenance in the current foil bytes.</summary>
    public string StationSource(int station)
    {
        var projection = CurrentProjection ?? throw new ContractError("DSL-PROFILE-TARGET");
        if ((uint)station >= (uint)projection.Assignments.Count) throw new ContractError("DSL-PROFILE-TARGET");
        string profile = projection.Assignments[station].ProfileName;
        byte[] foil = Section?.Draft.Bytes ?? session.Snapshot().Source;
        var source = ProvenanceFor(foil, profile);
        string display = source.ChipText(ChipSourceName(source));
        return source.Rights == RightsClass.NotRecorded ? display : display + " (" + source.Rights.ToString().ToUpperInvariant() + ")";
    }

    public CatalogSnapshot OpenCatalog()
    {
        var clock = Stopwatch.StartNew();
        IReadOnlyList<CatalogEntry> entries;
        string outcome;
        string? failureCause = null;
        try
        {
            entries = CfdWorkbench.Core.Catalog.Load();
            outcome = "ok";
        }
        catch (ContractError error) when (error.Code == "CAT-UNAVAILABLE")
        {
            entries = [];
            outcome = error.Code;
            failureCause = error.Reason;
        }
        if (outcome == "ok") catalogRows = entries;
        int choosable = entries.Count(entry => entry.Coordinates is not null && entry.DisabledReason is null);
        int disabled = entries.Count - choosable;
        int naca = entries.Count(entry => entry.Family == CatalogFamily.Naca);
        int eppler = entries.Count(entry => entry.Family == CatalogFamily.Eppler);
        int speer = entries.Count(entry => entry.Family == CatalogFamily.Speer);
        var scanClock = Stopwatch.StartNew();
        LibraryScan scan = sections is null ? new([], []) : sections.Scan();
        scanClock.Stop();
        session.RecordCatalog(new("library.scan", scan.Problems.Count == 0 ? "ok" : "problems",
            scanClock.Elapsed.TotalMilliseconds, null, null, null, scan.Entries.Count, null, scan.Problems.Count, scan.Entries.Count));
        clock.Stop();
        session.RecordCatalog(new("catalog.open", outcome, clock.Elapsed.TotalMilliseconds,
            naca, eppler, speer, scan.Entries.Count, disabled, scan.Problems.Count, entries.Count));
        return OpenedCatalog = new CatalogSnapshot(entries, scan.Entries, choosable, disabled, scan.Problems.Count, outcome)
        { FailureCause = failureCause, ProblemRows = scan.Problems };
    }

    public void ClearCatalogPreview() => ClearReplacePreview();

    /// <summary>
    /// One preview at a time, off the UI thread. A newer choice replaces the one waiting; the in-flight result is dropped
    /// when it is no longer the latest. The compute is the 140–240 ms catalog fit, so it never runs on the dispatcher.
    /// </summary>
    public void PreviewReplace(CatalogChoice choice, ReplaceScope scope = ReplaceScope.Draft)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (Section is null) throw new ContractError("DSL-DRAFT-OWNED");
        int ticket = Interlocked.Increment(ref previewTicket);
        CurrentPreview = null;
        RefusedPreview = null;
        PreviewChoice = null;
        PreviewSourceName = null;
        PreviewFault = null;
        previewDraftId = null;
        previewGeneration = 0;
        PreviewChanged?.Invoke();
        if (Interlocked.CompareExchange(ref previewBusy, 1, 0) != 0)
        {
            previewWaiting = choice;
            previewWaitingScope = scope;
            previewWaitingTicket = ticket;
            return;
        }
        StartPreview(ticket, choice, scope);
    }

    public Task ApplyReplaceAsync(ReplaceScope scope)
    {
        if (RefuseAnalysisEdit()) return Task.FromException(new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal));
        if (PreviewChoice is not { } choice || previewDraftId is null)
            return Task.FromException(new ContractError("CAT-NOT-ADMITTED",
                "This section has no coordinates in this build. Nothing changed."));
        if (Section is not { } mode || mode.Draft.DraftId != previewDraftId || mode.Draft.Generation != previewGeneration)
            return Task.FromException(new ContractError("DSL-STALE",
                "The section changed after this preview. Nothing changed."));
        ReplaceSource source;
        try { source = SourceOf(choice); }
        catch (ContractError error) { return Task.FromException(error); }
        return ApplySectionStepAsync(new SectionStep.Replace(source, scope));
    }

    public Task SaveToMySectionsAsync(string name, CancellationToken cancellation = default)
    {
        if (RefuseAnalysisEdit()) return Task.FromException(new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal));
        if (Section is not { } mode)
            return Task.FromException(new ContractError("DSL-DRAFT-OWNED"));
        if (sections is null)
            return Task.FromException(new ContractError("LIB-IO", "Couldn't save to My sections: no library root. Nothing was saved."));
        if (cancellation.IsCancellationRequested)
            return Task.FromCanceled(cancellation);
        byte[] bytes = mode.Draft.Bytes.ToArray();
        string profile = mode.Draft.Profile;
        var library = sections;
        var saved = Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            libraryGate?.Invoke();
            cancellation.ThrowIfCancellationRequested();
            var clock = Stopwatch.StartNew();
            try
            {
                byte[] block = ProfileBlock(bytes, profile);
                var provenance = ProvenanceFor(bytes, profile);
                // Save takes no token and returns only after the claim publish, so a cancel that
                // arrives once Save has started still reports success: the write completed.
                var entry = library.Save(name, block, provenance);
                clock.Stop();
                session.RecordCatalog(new CatalogTelemetry("library.save", "saved", clock.Elapsed.TotalMilliseconds,
                    null, null, null, null, null, null, null));
                return entry.Name;
            }
            catch (ContractError error)
            {
                clock.Stop();
                session.RecordCatalog(new CatalogTelemetry("library.save", error.Code, clock.Elapsed.TotalMilliseconds,
                    null, null, null, null, null, null, null));
                throw;
            }
        }, cancellation);
        return saved.ContinueWith(task =>
        {
            if (task.IsCanceled) return Task.FromCanceled(cancellation);
            if (task.IsFaulted) return Task.FromException(task.Exception!.GetBaseException());
            string savedName = task.Result;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            OnUiThread(() =>
            {
                try
                {
                    Status = "Saved " + savedName + " to My sections.";
                    Notify();
                    done.TrySetResult();
                }
                catch (Exception error) { done.TrySetException(error); }
            });
            return done.Task;
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
    }

    private void StartPreview(int ticket, CatalogChoice choice, ReplaceScope scope)
    {
        var mode = Section;
        if (mode is null)
        {
            FinishPreview(ticket, null, null, choice, null, 0, new ContractError("DSL-DRAFT-OWNED"));
            return;
        }
        string draftId = mode.Draft.DraftId;
        long generation = mode.Draft.Generation;
        ReplaceSource source;
        try { source = SourceOf(choice); }
        catch (ContractError error)
        {
            FinishPreview(ticket, null, null, choice, draftId, generation, error);
            return;
        }
        _ = Task.Run(() =>
        {
            if (ticket != Volatile.Read(ref previewTicket)) return ((ReplacePreview?)null, (Exception?)null);
            try
            {
                ReplacePreview computed = session.PreviewReplace(draftId, generation, source, scope);
                previewGate?.Invoke(ticket);
                return (computed, (Exception?)null);
            }
            catch (Exception error) { return ((ReplacePreview?)null, error); }
        }).ContinueWith(task =>
        {
            ReplacePreview? preview = null;
            Exception? fault = null;
            if (task.IsFaulted) fault = task.Exception!.GetBaseException();
            else
            {
                preview = task.Result.Item1;
                fault = task.Result.Item2;
            }
            if (fault is not null && fault is not ContractError)
                session.RecordPreviewFault("INTERNAL-ERROR");
            OnUiThread(() => FinishPreview(ticket, preview, source.DisplayName, choice, draftId, generation, fault));
        }, TaskScheduler.Default);
    }

    private void FinishPreview(int ticket, ReplacePreview? preview, string? name, CatalogChoice choice,
        string? draftId, long generation, Exception? fault)
    {
        CatalogChoice? next = null;
        ReplaceScope nextScope = default;
        int nextTicket = 0;
        if (previewWaiting is { } waiting && previewWaitingTicket == Volatile.Read(ref previewTicket))
        {
            next = waiting;
            nextScope = previewWaitingScope;
            nextTicket = previewWaitingTicket;
        }
        previewWaiting = null;
        try
        {
            bool ticketLatest = ticket == Volatile.Read(ref previewTicket) && !disposed && Section is not null;
            bool sameDraft = ticketLatest && draftId is not null
                && Section!.Draft.DraftId == draftId && Section.Draft.Generation == generation;
            bool refused = fault is not null || preview?.RefusalCode is not null;
            if (sameDraft && preview is not null && !refused)
            {
                CurrentPreview = preview;
                RefusedPreview = null;
                PreviewSourceName = name;
                PreviewChoice = choice;
                PreviewFault = null;
                previewDraftId = draftId;
                previewGeneration = generation;
                PreviewLandings++;
                PreviewChanged?.Invoke();
            }
            else if (ticketLatest)
            {
                CurrentPreview = null;
                RefusedPreview = sameDraft && preview?.RefusalCode is not null && preview.RefusedBytes is not null ? preview : null;
                PreviewChoice = null;
                PreviewSourceName = null;
                PreviewFault = fault as ContractError
                    ?? (preview?.RefusalCode is { } code ? new ContractError(code, preview.RefusalReason ?? code) : null);
                previewDraftId = null;
                previewGeneration = 0;
                PreviewDrops++;
                PreviewChanged?.Invoke();
            }
            else PreviewDrops++;
        }
        finally
        {
            if (next is not null) StartPreview(nextTicket, next, nextScope);
            else Interlocked.Exchange(ref previewBusy, 0);
        }
    }

    private static ReplaceSource SourceOf(CatalogChoice choice) => choice switch
    {
        CatalogChoice.Catalog row when row.Entry.Coordinates is { } bytes =>
            new ReplaceSource.Coordinates(row.Entry.Designation, new Provenance("gen:" + row.Entry.Id, false), bytes),
        CatalogChoice.Mine row => new ReplaceSource.Record(row.Entry.Name, row.Entry.Provenance, row.Entry.Bytes),
        _ => throw new ContractError("CAT-NOT-ADMITTED", "This section has no coordinates in this build. Nothing changed.")
    };

    private string ChipSourceName(Provenance parsed)
    {
        if (parsed.Origin is not { } origin) return LastReplaceName ?? "";
        int colon = origin.IndexOf(':');
        if (colon > 0 && (origin.StartsWith("gen:", StringComparison.Ordinal) || origin.StartsWith("vend:", StringComparison.Ordinal)))
        {
            string id = origin[(colon + 1)..];
            var entry = CatalogRows().FirstOrDefault(item => item.Id == id);
            if (entry is not null) return entry.Designation;
        }
        return LastReplaceName is { Length: > 0 } name ? name : origin;
    }

    private IReadOnlyList<CatalogEntry> CatalogRows()
    {
        if (catalogRows is { } rows) return rows;
        try
        {
            catalogRows = Catalog.Load();
            return catalogRows;
        }
        catch (ContractError) { return []; }
    }

    private static byte[] ProfileBlock(byte[] foil, string profile) =>
        FoilSource.ProfileBlock(ProfileOf(foil, profile));

    private static Provenance ProvenanceFor(byte[] foil, string profile)
    {
        string? raw = ProfileOf(foil, profile).Provenance;
        var parsed = CfdWorkbench.Core.Provenance.Parse(raw);
        if (parsed.Origin is null && !string.IsNullOrEmpty(raw))
            return new CfdWorkbench.Core.Provenance(raw, false);
        return parsed;
    }

    private static ProfileDefinition ProfileOf(byte[] foil, string profile) =>
        FoilSource.Parse(foil).Profile(profile) ?? throw new ContractError("LIB-SECTION-INVALID");

    private void ClearReplacePreview(bool keepApplyTarget = false)
    {
        Interlocked.Increment(ref previewTicket);
        previewWaiting = null;
        bool had = CurrentPreview is not null || RefusedPreview is not null || PreviewSourceName is not null || PreviewFault is not null
            || (!keepApplyTarget && PreviewChoice is not null);
        CurrentPreview = null;
        RefusedPreview = null;
        PreviewSourceName = null;
        PreviewFault = null;
        if (!keepApplyTarget)
        {
            PreviewChoice = null;
            previewDraftId = null;
            previewGeneration = 0;
        }
        if (had && !disposed) PreviewChanged?.Invoke();
    }

    private void NotifySection()
    {
        RaiseSectionChanged();
        Notify();
    }

    private void RaiseSectionChanged()
    {
        if (!disposed) SectionChanged?.Invoke();
    }

    public async Task FinishSectionAsync()
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (Section is null) throw new ContractError("DSL-DRAFT-OWNED");
        // Finish follows the certificate of the landed bytes: a step still applying lands (or is refused) first.
        await sectionStepTail.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
        var mode = Section ?? throw new ContractError("DSL-CONFLICT");
        if (mode.Assessment is null) await AssessCurrentSectionAsync();
        mode = Section ?? throw new ContractError("DSL-CONFLICT");
        if (!mode.CanFinish || mode.Assessment is null)
        {
            Status = mode.FinishReason ?? "This section cannot Finish yet.";
            NotifySection();
            throw new ContractError("DSL-NOT-ASSESSED");
        }
        CancelSectionAssessment();
        ClearReplacePreview();
        session.FinishSection(Guid.NewGuid().ToString("D"), mode.Assessment);
        Section = null;
        sectionBeforeChecking = null;
        SectionRefitRefusal = null;
        draft = null;
        Inspection = session.InspectAccepted();
        sectionViews.Clear();
        UpdateEstimates();
        Status = "Section finished as one accepted source revision. Save to persist it.";
        Select(new Selection.Station(mode.Draft.Assignment, Inspection.Authored.Assignments[mode.Draft.Assignment].Eta));
        NotifySection();
    }

    public void CancelSection()
    {
        if (RefuseAnalysisEdit()) return;
        if (Section is not { } mode) return;
        CancelSectionAssessment();
        ClearReplacePreview();
        session.Cancel(mode.Draft.DraftId);
        Section = null;
        sectionBeforeChecking = null;
        SectionRefitRefusal = null;
        draft = null;
        Status = "Section cancelled. Accepted source and history are unchanged.";
        Select(new Selection.Station(mode.Draft.Assignment, Inspection!.Authored.Assignments[mode.Draft.Assignment].Eta));
        NotifySection();
    }

    private void UpdateEstimates()
    {
        if (Inspection is null)
        {
            Estimates = null;
            return;
        }
        try
        {
            byte[] source = session.Snapshot().Source;
            Estimates = WingEstimates.From(source, "accepted", 0);
        }
        catch
        {
            Estimates = null;
        }
    }

    public void ApplySpan(string text)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (Section is not null)
        {
            Status = PropertyCopy.SetInWorkspace;
            Notify();
            throw new ContractError("DSL-DRAFT-OWNED");
        }
        // The existing pane still calls this entry point until its owner ports that call to ApplySpanAsync.
        RequireCertifiedFoil();
        if (draft is not null) throw new ContractError("DSL-DRAFT-OWNED");
        session.ApplyDimension(Guid.NewGuid().ToString("D"), new("span", text));
        Inspection = session.InspectAccepted();
        UpdateEstimates();
        Status = "Span applied as one accepted source revision. Save to persist it.";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public Task<CommitOutcome> ApplySpanAsync(string text) => RunDirectCommandAsync(() =>
    {
        string id = session.ApplyDimension(Guid.NewGuid().ToString("D"), new("span", text));
        return new CommitOutcome.Committed(id, "Span applied as one accepted source revision. Save to persist it.");
    });

    public bool BeginGesture(PointRef point, GestureInput input)
    {
        if (RefuseAnalysisEdit()) return false;
        if (Section is { } mode)
        {
            if (Gesture != GestureState.Idle || point.Profile != mode.Draft.Profile || point.Curve is not ("upper" or "lower")) return false;
            var side = point.Curve == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower;
            var sectionPoint = SectionCurve(side)?.Points.FirstOrDefault(item => item.Id == point.VertexId);
            if (sectionPoint is null || sectionPoint.Freedom == PointFreedom.Fixed) return false;
            gesturePoint = point;
            gestureOrigin = sectionPoint;
            gestureInput = input;
            pendingGestureTarget = null;
            gestureStarted = Stopwatch.GetTimestamp();
            Gesture = input == GestureInput.Pointer ? GestureState.Pressed : GestureState.Nudging;
            Select(new Selection.Points([point]));
            return true;
        }
        var view = CurveFor(point.Curve)?.Points.FirstOrDefault(candidate => candidate.Id == point.VertexId);
        if (view is null) return false;
        if (Gesture == GestureState.Busy)
        {
            if (input == GestureInput.Pointer) Select(new Selection.Points([point]));
            Status = "Checking the last change…";
            Notify();
            return false;
        }
        if (Gesture == GestureState.Nudging && input == GestureInput.Pointer)
        {
            _ = EndGestureAsync(GestureEnd.KeyUp);
            Select(new Selection.Points([point]));
            return false;
        }
        if (Gesture != GestureState.Idle || draft is not null || Inspection?.Geometry.Status != GeometryStatus.Certified)
            return false;
        gestureGroup = null;
        groupRefusal = null;
        gestureMoved = [];
        // Design §3.2: a press on a member of several selected points keeps the selection and drags all of it. What stops a
        // group is said when the drag starts (a click without a drag collapses the selection on release instead).
        if (input != GestureInput.Typed && IsGroupMember(point))
        {
            gestureGroup = ((Selection.Points)Selection).Items;
            groupRefusal = GroupRefusal(gestureGroup, point);
            if (input == GestureInput.Keyboard && groupRefusal is not null)
            {
                SetStatus(groupRefusal, ReportKind.Error);
                Notify();
                return false;
            }
        }
        else Select(new Selection.Points([point]));
        if (gestureGroup is null && view.Freedom == PointFreedom.Fixed)
        {
            SetStatus(view is { Curve: "dihedral", Role: PointRole.RootEnd } ? DihedralRootLocked
                : $"This {view.Role} point is fixed by the foil definition.", ReportKind.Error);
            Notify();
            return false;
        }
        gesturePoint = point;
        gestureOrigin = view;
        gestureInput = input;
        pendingGestureTarget = null;
        gestureFrames = gestureClamped = 0;
        gestureClampReason = null;
        gestureUpdateTimes.Clear();
        gestureEstimateTimes.Clear();
        gestureStarted = Stopwatch.GetTimestamp();
        Gesture = input == GestureInput.Pointer ? GestureState.Pressed : GestureState.Nudging;
        Notify();
        return true;
    }

    /// <summary>§11.4: pressing the dihedral root end (it stays on the centre line, FoilDSL :298).</summary>
    public const string DihedralRootLocked = "The dihedral root is at the centre line. It can't be moved.";

    public void UpdateGesture(double spanMeters, double aftMeters) => UpdateGesture(spanMeters, aftMeters, null);

    /// <summary>
    /// A pointer drag frame. <paramref name="pixelsFromPress"/> is the pointer's screen distance from the press, measured by
    /// the view that knows its own axis mapping (an elevation lane's ordinate is degrees or a chord fraction, not metres);
    /// null measures it on the Plan's metre scale.
    /// </summary>
    public void UpdateGesture(double spanMeters, double aftMeters, double? pixelsFromPress)
    {
        if (IsAnalysis) return;
        if (Gesture == GestureState.Nudging && gestureInput == GestureInput.Keyboard) return;
        UpdateGestureTarget(spanMeters, aftMeters, pixelsFromPress);
    }

    private void UpdateGestureTarget(double spanMeters, double aftMeters, double? pixelsFromPress = null)
    {
        if (Gesture is not (GestureState.Pressed or GestureState.Dragging or GestureState.Nudging) || gestureOrigin is null)
            return;
        if (!double.IsFinite(spanMeters) || !double.IsFinite(aftMeters)) return;
        if (Gesture == GestureState.Pressed)
        {
            double px = pixelsFromPress ?? Math.Sqrt(Math.Pow(spanMeters - gestureOrigin.SpanMeters, 2) +
                Math.Pow(aftMeters - gestureOrigin.Ordinate, 2)) * PlanCamera.PixelsPerMeter;
            if (px < 3) return;
            if (gestureGroup is not null && groupRefusal is { } refusal)
            {
                // Design §4: a refused group never opens a draft; the selection stays and the strip says why.
                Gesture = GestureState.Idle;
                SetStatus(refusal, ReportKind.Error);
                Notify();
                return;
            }
            Gesture = GestureState.Dragging;
        }
        if (Section is not null)
        {
            pendingGestureTarget = (spanMeters, aftMeters);
            Notify();
            return;
        }
        bool opening = draft is null;
        EnsurePointDraft();
        if (opening && gestureGroup is not null) AnnounceGroupStart();
        pendingGestureTarget = (spanMeters, aftMeters);
        ScheduleGestureFrame();
    }

    public void Nudge(int spanDirection, int aftDirection, NudgeModifier modifier)
    {
        if (RefuseAnalysisEdit()) return;
        if (Gesture != GestureState.Nudging || gestureOrigin is null || gesturePoint is null) return;
        if (Section is not null)
        {
            // D-7: the run accumulates from its own pending target and lands as one step on key-up. A key records the target
            // and nothing else, as a drag frame does (UpdateSectionGesture): a shell refresh per key cost ~209 ms under load.
            double sectionStep = modifier switch { NudgeModifier.Command => .0001, NudgeModifier.Shift => .01, _ => .001 };
            var sectionAt = pendingGestureTarget ?? (gestureOrigin.SpanMeters, gestureOrigin.Ordinate);
            (double Span, double Aft) next = (sectionAt.Item1 + spanDirection * sectionStep, sectionAt.Item2 + aftDirection * sectionStep);
            if (double.IsFinite(next.Span) && double.IsFinite(next.Aft)) pendingGestureTarget = next;
            return;
        }
        // The channel's own ladder (§3.6): 0.01 · 0.1 · 1 mm on lengths, ° on twist, % on t/c. The span step stays in metres.
        var unit = Channels.Unit(gesturePoint.Curve);
        double spanStep = modifier switch { NudgeModifier.Command => 0.00001, NudgeModifier.Shift => 0.001, _ => 0.0001 };
        double step = modifier switch { NudgeModifier.Command => unit.NudgeFine, NudgeModifier.Shift => unit.NudgeCoarse, _ => unit.NudgePlain };
        // D-7: a run steps from where the draft has the point now (FlushGestureFrame clears the pending target), so N
        // presses or repeats add N quantized steps (§3.7) and a clamp holds without banking steps past it.
        var at = draft is null ? gestureOrigin
            : CurveFor(gesturePoint.Curve)?.Points.FirstOrDefault(item => item.Id == gesturePoint.VertexId) ?? gestureOrigin;
        var current = pendingGestureTarget ?? (at.SpanMeters, at.Ordinate);
        UpdateGestureTarget(current.Item1 + spanDirection * spanStep, current.Item2 + aftDirection * step);
        FlushGestureFrame();
    }

    private void EnsurePointDraft()
    {
        if (draft is not null) return;
        if (gesturePoint is null) throw new ContractError("DSL-TARGET");
        draft = gestureGroup is { } group
            ? session.BeginGroupGesture(Guid.NewGuid().ToString("D"), gesturePoint.Curve, group.Select(member => member.VertexId).ToArray())
            : session.BeginPointGesture(Guid.NewGuid().ToString("D"), gesturePoint.Curve, gesturePoint.VertexId);
        gestureOperationId = draft.Id;
        draftProjection = null;
    }

    /// <summary>
    /// Why a group can't move, in the order of design §3.1 and §4: two curves (COPY-G7), a handle without its anchor (G5), a
    /// fully locked member (G3). Null when it can. Core refuses the same cases again at Begin.
    /// </summary>
    private string? GroupRefusal(IReadOnlyList<PointRef> members, PointRef grabbed)
    {
        if (members.Select(member => member.Curve).Distinct().Count() > 1 || CurveFor(grabbed.Curve) is not { } curve)
            return GroupCopy.Text("G7");
        var views = members.Select(member => curve.Points.FirstOrDefault(point => point.Id == member.VertexId)).ToList();
        if (views.Any(view => view is null)) return GroupCopy.Text("G7");
        if (views.Any(view => view!.Role == PointRole.AnchorHandle && members.All(member => member.VertexId != view.AnchorId)))
            return GroupCopy.Text("G5");
        return Seeded(curve, views!).FirstOrDefault(view => view.Freedom == PointFreedom.Fixed) is { } locked
            ? GroupCopy.Text("G3", ("point", $"Point {locked.Index + 1}")) : null;
    }

    /// <summary>Ruling 111 (5): point 1 without the root brings the root into the group (the root-mirror coupling), as Core seeds it.</summary>
    private static List<PointView> Seeded(CurveView curve, IReadOnlyList<PointView> members)
    {
        var all = members.ToList();
        if (all.Any(point => point.Index == 1 && point.Locks.Contains("root_mirror")) && all.All(point => point.Index != 0))
            all.Insert(0, curve.Points[0]);
        return all;
    }

    /// <summary>The strip line when a group draft opens: the note of a held axis (COPY-G4), else "Moving n points." (COPY-G1).</summary>
    private void AnnounceGroupStart()
    {
        if (gestureGroup is not { } group || CurveFor(group[0].Curve) is not { } curve) return;
        string noun = GroupCopy.Curve(group[0].Curve);
        var held = Seeded(curve, group.Select(member => curve.Points.First(point => point.Id == member.VertexId)).ToList())
            .FirstOrDefault(view => view.Freedom is PointFreedom.ValueOnly or PointFreedom.SpanOnly &&
                !(view.Index == 1 && view.Locks.Contains("root_mirror")));
        string text = held is null ? GroupCopy.Text("G1", ("n", group.Count.ToString(CultureInfo.InvariantCulture)), ("curve", noun))
            : held.Freedom == PointFreedom.ValueOnly
                ? GroupCopy.Text("G4", ("point", PointNoun(held, curve)), ("axis", GroupCopy.Axis(held.Curve)))
                : GroupCopy.Text("G4.none", ("point", PointNoun(held, curve)));
        SetStatus(text, ReportKind.Info);
    }

    private static string PointNoun(PointView point, CurveView curve) =>
        point.Role == PointRole.RootEnd ? "root end" : point.Role == PointRole.TipEnd ? "tip end" : $"point {point.Index + 1}";

    private void ScheduleGestureFrame()
    {
        if (gestureFrameScheduled || Application.Current is null) return;
        gestureFrameScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            gestureFrameScheduled = false;
            FlushGestureFrame();
        }, DispatcherPriority.Render);
    }

    public void FlushGestureFrame()
    {
        if (IsAnalysis) return;
        if (draft is null || pendingGestureTarget is not { } target || Gesture is GestureState.Idle or GestureState.Busy)
            return;
        pendingGestureTarget = null;
        var timer = Stopwatch.StartNew();
        var frame = gestureGroup is not null
            ? session.UpdateGroupGesture(draft.Id, draft.Generation, gesturePoint!.VertexId, target.Span, target.Aft)
            : session.UpdatePointGesture(draft.Id, draft.Generation, target.Span, target.Aft);
        gestureUpdateTimes.Add(timer.Elapsed.TotalMilliseconds);
        draft = frame.Draft;
        draftProjection = null;
        gestureFrames++;
        if (frame.Clamped) gestureClamped++;
        if (frame.Limit is { } held) gestureClampReason = held.Kind.ToString();
        GestureLimit = frame.Limit;
        if (gestureGroup is null) AnnounceLimit(frame.Limit);
        else RecordGroupFrame(frame, target);
        timer.Restart();
        try { Estimates = WingEstimates.From(draft.Bytes, "preview", draft.Generation); }
        catch { Estimates = null; }
        gestureEstimateTimes.Add(timer.Elapsed.TotalMilliseconds);
        GestureCrossing = Gesture == GestureState.Dragging && gesturePoint is { Curve: "leading" or "trailing" } dragged &&
            Planform is { } plan ? EdgeHullCrossing(plan, dragged.Curve) : null;
        Notify();
    }

    // One announcement per hold: the strip is a live region, so a frame that stays held writes nothing. A new limit value, or
    // a frame that frees the hold and a later one that re-holds, announces again.
    private void AnnounceLimit(GestureLimit? limit) =>
        AnnounceHold(limit is null ? null : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{limit.Kind}:{limit.LimitMeters:F6}"),
            limit is null ? null : TipChord.HoldText(limit));

    /// <summary>
    /// A group frame (design §3.3, §3.4): where the grabbed point is (applied), where the pointer asked (requested), the member
    /// that binds, and the one hold sentence: Core's tip or root limit, or "held by point n" (COPY-G6). A domain hold (twist, t/c)
    /// says its channel's clamp reason on the strip, with the hold icon.
    /// </summary>
    private void RecordGroupFrame(GestureFrame frame, (double Span, double Aft) target)
    {
        GestureRequested = target;
        GestureApplied = (frame.SpanMeters, frame.Ordinate);
        gestureMoved = frame.MovedIds;
        GestureBinding = BindingOf(frame, target);
        if (GestureBinding is { } binding && frame.Clamped)
            gestureClampReason = $"{binding.Kind}:{GroupCopy.Curve(binding.Point.Curve)} {PointNumber(binding.Point)}";
        string? text = frame.Limit is { } limit ? TipChord.HoldText(limit)
            : GestureBinding is { Kind: "Neighbour" } neighbour
                ? GroupCopy.Text("G6", ("n", PointNumber(neighbour.Point).ToString(CultureInfo.InvariantCulture)))
            // A domain hold (twist, t/c) is a hold like the rest: the strip carries the channel's existing reason with the hold icon.
            : GestureBinding is { Kind: "Domain" } ? (gesturePoint?.Curve == "twist" ? ElevationView.TwistClampReason : ElevationView.ThicknessClampReason) : null;
        AnnounceHold(text is null ? null : text + "|" + GestureBinding?.Point.VertexId, text);
        if (gestureOrigin is { } origin)
        {
            var rows = PropertiesView.Curves[origin.Curve];
            double dValue = (frame.Ordinate - origin.Ordinate) * PropertiesView.FieldScale[rows.ValueFamily];
            double dSpan = (frame.SpanMeters - origin.SpanMeters) * 1000;
            string value = Quantity.WithUnit((dValue >= 0 ? "+" : "") + Quantity.Typed(dValue), rows.ValueUnit);
            string applied = Math.Abs(dSpan) < 0.005 ? value
                : $"{Quantity.WithUnit((dSpan >= 0 ? "+" : "") + Quantity.Typed(dSpan), "mm")} from root · {value} {GroupCopy.Axis(origin.Curve)}";
            GroupHold = $"Applied {applied}" +
                (GestureBinding is { } held ? $" · held by point {PointNumber(held.Point)}" : "");
        }
    }

    private int PointNumber(PointRef point) => (CurveFor(point.Curve)?.Points.FirstOrDefault(item => item.Id == point.VertexId)?.Index ?? -1) + 1;

    private GroupBinding? BindingOf(GestureFrame frame, (double Span, double Aft) target)
    {
        if (gesturePoint is null || gestureOrigin is null || CurveFor(gesturePoint.Curve) is not { } curve) return null;
        PointRef At(int index) => new(gesturePoint.Curve, curve.Points[index].Id);
        if (frame.Limit is { } limit) return new(At(limit.Kind == GestureLimitKind.RootMaximum ? 0 : curve.Points.Count - 1), limit.Kind.ToString());
        var moved = frame.MovedIds.Select(id => curve.Points.ToList().FindIndex(point => point.Id == id)).Where(index => index >= 0).ToList();
        if (!frame.Clamped || moved.Count == 0) return null;
        double wanted = target.Span - gestureOrigin.SpanMeters, got = frame.SpanMeters - gestureOrigin.SpanMeters;
        if (Math.Abs(wanted) - Math.Abs(got) > 1e-6)
        {
            int next = wanted > 0 ? moved.Max() + 1 : moved.Min() - 1;
            if (next >= 0 && next < curve.Points.Count && !moved.Contains(next)) return new(At(next), "Neighbour");
        }
        // A domain hold (twist, t/c): the moved member that sits on its channel's lower or upper bound binds the group.
        var unit = Channels.Unit(gesturePoint.Curve);
        if (unit.DomainLower is double lower && unit.DomainUpper is double upper)
            foreach (int index in moved)
                if (Math.Abs(curve.Points[index].Ordinate - lower) < 1e-9 || Math.Abs(curve.Points[index].Ordinate - upper) < 1e-9)
                    return new(At(index), "Domain");
        return null;
    }

    private void AnnounceHold(string? key, string? text)
    {
        if (key == announcedLimit) return;
        bool wasHeld = announcedLimit is not null;
        announcedLimit = key;
        if (text is not null)
        {
            if (!wasHeld) stripBeforeHold = statusSlot.Snapshot();
            SetStatus(text, ReportKind.Warning);
        }
        else if (stripBeforeHold is { } before)
        {
            // The hold freed mid-drag: put back the line it replaced, so the strip never keeps a hold that no longer holds.
            stripBeforeHold = null;
            SetStatus(before.Text, before.Kind);
        }
    }

    /// <summary>
    /// Null when every trailing-rail Bernstein ordinate is strictly aft of every leading-rail one (the certificate's
    /// positive-chord rule); otherwise the dragged rail's offending Bernstein coefficient, in plan metres.
    /// </summary>
    public static (double SpanMeters, double Ordinate)? EdgeHullCrossing(PlanformView plan, string draggedCurve)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var leading = BernsteinCoefficients(plan.Leading);
        var trailing = BernsteinCoefficients(plan.Trailing);
        if (trailing.Min(item => item.Aft) > leading.Max(item => item.Aft)) return null;
        var at = draggedCurve == "leading" ? leading.MaxBy(item => item.Aft) : trailing.MinBy(item => item.Aft);
        return (at.Eta * plan.HalfSpanMeters, at.Aft);
    }

    // Boehm knot insertion to multiplicity p at every interior knot: the control points become the Bernstein
    // coefficients of every span (the same hull Geometry.Assess builds by exact interpolation).
    private static List<(double Eta, double Aft)> BernsteinCoefficients(CurveView curve)
    {
        var knots = curve.Knots.ToList();
        var points = curve.Points.Select(point => (point.Eta, Aft: point.Ordinate)).ToList();
        int degree = knots.Count - points.Count - 1;
        foreach (double knot in curve.Knots.Where(value => value > knots[0] && value < knots[^1]).Distinct().ToArray())
        {
            while (knots.Count(value => value == knot) < degree)
            {
                int span = knots.FindLastIndex(value => value <= knot);
                var refined = new List<(double Eta, double Aft)>(points.Count + 1);
                for (int index = 0; index <= points.Count; index++)
                {
                    if (index <= span - degree) refined.Add(points[index]);
                    else if (index > span) refined.Add(points[index - 1]);
                    else
                    {
                        double alpha = (knot - knots[index]) / (knots[index + degree] - knots[index]);
                        refined.Add(((1 - alpha) * points[index - 1].Eta + alpha * points[index].Eta,
                            (1 - alpha) * points[index - 1].Aft + alpha * points[index].Aft));
                    }
                }
                points = refined;
                knots.Insert(span + 1, knot);
            }
        }
        return points;
    }

    public Task<GestureOutcome> EndGestureAsync(GestureEnd reason, CancellationToken cancellation = default)
    {
        if (RefuseAnalysisEdit()) return Task.FromResult<GestureOutcome>(new GestureOutcome.Refused("ANA-EDIT-INERT", AnalysisPointRefusal));
        if (Section is not null) return EndSectionGestureAsync(reason, cancellation);
        if (Gesture == GestureState.Busy) return Task.FromResult<GestureOutcome>(new GestureOutcome.NoChange());
        if (Gesture == GestureState.Idle)
        {
            if (reason == GestureEnd.Escape && Selection is Selection.Points selected)
            {
                var chosen = selected.Items.FirstOrDefault();
                var handle = chosen is null ? null : CurveFor(chosen.Curve)?.Points.FirstOrDefault(item => item.Id == chosen.VertexId);
                Select(handle?.AnchorId is { } anchorId
                    ? new Selection.Points([new PointRef(chosen!.Curve, anchorId)])
                    : new Selection.Foil());
            }
            return Task.FromResult<GestureOutcome>(new GestureOutcome.NoChange());
        }
        if (reason == GestureEnd.KeyUp && Gesture != GestureState.Nudging ||
            Gesture == GestureState.Nudging && gestureInput == GestureInput.Keyboard &&
            reason is (GestureEnd.Release or GestureEnd.CaptureLost))
            return Task.FromResult<GestureOutcome>(new GestureOutcome.NoChange());
        if (Gesture == GestureState.Pressed && gestureGroup is { } pressedGroup && gesturePoint is { } clicked &&
            reason == GestureEnd.Release)
        {
            // Design §3.2: a click on a member that never became a drag collapses the selection to that point, on release.
            collapsedGroup = (pressedGroup, Stopwatch.GetTimestamp());
            Select(new Selection.Points([clicked]));
        }
        if (reason is GestureEnd.Escape or GestureEnd.CaptureLost ||
            Gesture == GestureState.Pressed || Gesture == GestureState.Dragging && reason is GestureEnd.FocusLost or GestureEnd.Deactivated)
            return Task.FromResult(CancelPointGesture(reason, Gesture == GestureState.Pressed));

        FlushGestureFrame();
        if (draft is null || gestureOrigin is null)
            return Task.FromResult(CancelPointGesture(reason, true));
        var point = CurveFor(gesturePoint!.Curve)!.Points.First(candidate => candidate.Id == gesturePoint.VertexId);
        // Half the channel's quantum of Δ (1 µm on lengths, 10⁻⁵ ° on twist, 10⁻⁷ on t/c) is "no change".
        if (Math.Abs(point.SpanMeters - gestureOrigin.SpanMeters) < 0.00000005 &&
            Math.Abs(point.Ordinate - gestureOrigin.Ordinate) < Channels.Unit(gesturePoint.Curve).Quantum / 2)
            return Task.FromResult(CancelPointGesture(reason, true));

        Gesture = GestureState.Busy;
        Notify();
        var capture = draft;
        long version = stateVersion;
        pendingCommit = CommitPointGestureAsync(capture, reason, version, cancellation);
        return pendingCommit;
    }

    private async Task<GestureOutcome> EndSectionGestureAsync(GestureEnd reason, CancellationToken cancellation)
    {
        if (Gesture == GestureState.Idle) return new GestureOutcome.NoChange();
        if (reason == GestureEnd.KeyUp && Gesture != GestureState.Nudging) return new GestureOutcome.NoChange();
        var target = pendingGestureTarget;
        var origin = gestureOrigin;
        var point = gesturePoint;
        pendingGestureTarget = null;
        gestureOrigin = null;
        gesturePoint = null;
        gestureInput = null;
        Gesture = GestureState.Idle;
        if (reason is GestureEnd.Escape or GestureEnd.CaptureLost || target is null || origin is null || point is null)
            return new GestureOutcome.NoChange();
        if (Math.Abs(target.Value.Span - origin.SpanMeters) < 1e-12 && Math.Abs(target.Value.Aft - origin.Ordinate) < 1e-12)
            return new GestureOutcome.NoChange();
        var side = point.Curve == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower;
        await ApplySectionStepAsync(new SectionStep.Move(side, point.VertexId, target.Value.Span, target.Value.Aft), cancellation);
        // UI-LIFETIME: a section cancelled or finished while the step applied leaves nothing committed.
        return Section is { } landed ? new GestureOutcome.Committed(landed.Draft.DraftId, "Section step added.") : new GestureOutcome.NoChange();
    }

    private async Task<GestureOutcome> CommitPointGestureAsync(SessionDraft capture, GestureEnd reason, long version,
        CancellationToken cancellation)
    {
        GestureOutcome outcome;
        try
        {
            SessionAssessment assessment = await Task.Run(() => session.Validate(capture.Id, capture.Generation, cancellation));
            if (version != stateVersion || draft?.Id != capture.Id || draft.Generation != capture.Generation)
                return new GestureOutcome.Cancelled("The change was superseded.");
            if (assessment.Status != GeometryStatus.Certified)
            {
                string copy = assessment.Status == GeometryStatus.NotAssessed
                    ? "This change couldn't be checked, so it wasn't applied. Nothing changed."
                    : $"{assessment.Code}: This change wasn't applied. Nothing changed.";
                outcome = new GestureOutcome.Refused(assessment.Code, copy);
            }
            else
            {
                string id = await Task.Run(() => session.Apply(Guid.NewGuid().ToString("D"), assessment));
                Inspection = session.InspectAccepted();
                outcome = new GestureOutcome.Committed(id, "Point change applied as one accepted source revision.");
            }
        }
        catch (OperationCanceledException)
        {
            outcome = new GestureOutcome.Refused("DSL-NOT-ASSESSED", "This change couldn't be checked, so it wasn't applied. Nothing changed.");
        }
        catch (ContractError error)
        {
            outcome = new GestureOutcome.Refused(error.Code, $"{error.Code}: This change wasn't applied. Nothing changed.");
        }
        finally
        {
            if (draft?.Id == capture.Id)
            {
                try { session.Cancel(capture.Id); } catch (ContractError) { }
            }
            draft = null;
            draftProjection = null;
            pendingGestureTarget = null;
            pendingCommit = null;
            gestureInput = null;
            LastGestureFrames = gestureFrames;
            LastGestureClamped = gestureClamped > 0;
            Gesture = GestureState.Idle;
            UpdateEstimates();
            Notify();
        }
        // §11.4 "Committed move": one report for the Plan, the elevations and Properties, from the accepted point.
        if (outcome is GestureOutcome.Committed groupAccepted && gestureGroup is { } movedGroup)
            outcome = groupAccepted with { Report = GroupMovedReport(movedGroup) };
        else if (outcome is GestureOutcome.Committed accepted && gestureOrigin is { } origin && CurveFor(origin.Curve) is { } curve &&
            curve.Points.FirstOrDefault(point => point.Id == origin.Id) is { } moved)
            outcome = accepted with
            {
                Report = PropertyCopy.CommittedMove(origin, moved, curve, Estimates?.MacMeters, Estimates?.MaxThicknessRatio)
            };
        SetStatus(outcome switch
        {
            GestureOutcome.Committed committed => committed.Report,
            GestureOutcome.Refused refused => refused.Copy,
            _ => "Point change cancelled."
        }, outcome is GestureOutcome.Refused ? ReportKind.Error : ReportKind.Info);
        EmitGestureEnd(outcome, reason);
        gestureOperationId = null;
        Notify();
        return outcome;
    }

    /// <summary>COPY-G2 with the end-chord clauses of Ruling 116: "Moved 3 trailing edge points. Tip chord 5.00 mm." Rails only, and only for an end vertex that moved.</summary>
    private string GroupMovedReport(IReadOnlyList<PointRef> members)
    {
        string curveName = members[0].Curve;
        string text = GroupCopy.Text("G2", ("n", members.Count.ToString(CultureInfo.InvariantCulture)), ("curve", GroupCopy.Curve(curveName)));
        if (curveName is not ("leading" or "trailing") || CurveFor(curveName) is not { } curve || Estimates is not { } wing) return text;
        if (gestureMoved.Contains(curve.Points[^1].Id))
            text += GroupCopy.Text("G2.end", ("end-chord", "Tip chord"), ("value", Quantity.TypedLength(wing.TipChordMeters)));
        if (gestureMoved.Contains(curve.Points[0].Id))
            text += GroupCopy.Text("G2.end", ("end-chord", "Root chord"), ("value", Quantity.TypedLength(wing.RootChordMeters)));
        return text;
    }

    private GestureOutcome CancelPointGesture(GestureEnd reason, bool noChange)
    {
        if (draft is not null) session.Cancel(draft.Id);
        draft = null;
        draftProjection = null;
        pendingGestureTarget = null;
        gestureInput = null;
        LastGestureFrames = gestureFrames;
        LastGestureClamped = gestureClamped > 0;
        Gesture = GestureState.Idle;
        UpdateEstimates();
        GestureOutcome outcome = noChange ? new GestureOutcome.NoChange() :
            new GestureOutcome.Cancelled("Drag cancelled. The point is back where it was.");
        // DR-STATUS-4: a gesture that changed nothing reports nothing; the strip stays as it was.
        if (outcome is GestureOutcome.Cancelled cancelled) Status = cancelled.Copy;
        EmitGestureEnd(outcome, reason);
        gestureOperationId = null;
        Notify();
        return outcome;
    }

    private void EmitGestureEnd(GestureOutcome outcome, GestureEnd reason)
    {
        string result = outcome switch
        {
            GestureOutcome.Committed => "committed",
            GestureOutcome.Refused => "refused",
            GestureOutcome.Cancelled => "cancelled",
            _ => "no-change"
        };
        CfdWorkbench.Desktop.Shell.ShellEvents.Record("gesture.end", result,
            Stopwatch.GetElapsedTime(gestureStarted).TotalMilliseconds, Guid.NewGuid().ToString("N"),
            code: (outcome as GestureOutcome.Refused)?.Code, clampedCount: gestureClamped, clampReason: gestureClampReason,
            trigger: reason.ToString().ToLowerInvariant(), frames: gestureFrames,
            updateP95Ms: Percentile95(gestureUpdateTimes), estimatesP95Ms: Percentile95(gestureEstimateTimes),
            editKind: "gesture", operationId: gestureOperationId,
            curveFamily: gesturePoint is null ? null : Channels.Family(gesturePoint.Curve), threeDVisible: ThreeDVisible);
    }

    private static double? Percentile95(List<double> values)
    {
        if (values.Count == 0) return null;
        var sorted = values.OrderBy(value => value).ToArray();
        return sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
    }

    /// <summary>
    /// Design group-move §3.6, Ruling 111 (9): a typed Set to or Move by for the selected points of one curve, as one undo step.
    /// <paramref name="amount"/> is in Core's unit (metres on the rails and dihedral, the channel's own otherwise). Core refuses
    /// atomically and names the cause (COPY-A, G8, G9, G10); the selection is kept.
    /// </summary>
    public Task<CommitOutcome> ApplyGroupValueAsync(GroupValueMode mode, GroupValueAxis axis, double amount)
    {
        if (Selection is not Selection.Points { Items.Count: > 1 } picked || picked.Items.Select(item => item.Curve).Distinct().Count() != 1)
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("DSL-TARGET", GroupCopy.Text("G7")));
        string curve = picked.Items[0].Curve;
        string[] ids = picked.Items.Select(item => item.VertexId).ToArray();
        var rows = PropertiesView.Curves[curve];
        return RunDirectCommandAsync(() =>
        {
            GroupValueOutcome result;
            try { result = session.ApplyGroupValue(Guid.NewGuid().ToString("D"), new GroupValueCommand(curve, ids, mode, axis, amount)); }
            catch (ContractError range) when (range.Code == "DSL-GROUP-RANGE")
            {
                // Core reports the point and the range as data; the words are GroupCopy G13 (Rulings 119, 120), one data change away.
                var fields = range.Message.Split(';').Select(part => part.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
                string Shown(string key) => Quantity.WithUnit(Quantity.Typed(double.Parse(fields[key], CultureInfo.InvariantCulture)), fields["unit"]);
                throw new ContractError(range.Code, GroupCopy.Text("G13", ("n", fields["point"]), ("min", Shown("min")), ("max", Shown("max"))));
            }
            double scale = axis == GroupValueAxis.Span ? 1000 : PropertiesView.FieldScale[rows.ValueFamily];
            string unit = axis == GroupValueAxis.Span ? "mm" : rows.ValueUnit;
            string n = ids.Length.ToString(CultureInfo.InvariantCulture);
            string word = axis == GroupValueAxis.Span ? "span" : GroupCopy.Axis(curve);
            string value = Quantity.Typed(amount * scale);
            string report = mode == GroupValueMode.SetTo
                ? GroupCopy.Text("G11", ("axis", word), ("n", n), ("value", Quantity.WithUnit(value, unit)))
                : GroupCopy.Text("G12", ("axis", word), ("n", n), ("value", Quantity.WithUnit((amount >= 0 ? "+" : "") + value, unit)));
            return new CommitOutcome.Committed(result.AcceptedId, report);
        }, preserveStatusAfterCommit: true);
    }

    public Task<CommitOutcome> ApplyPointCommandAsync(PointCommand command)
    {
        int removedNumber = command is PointCommand.RemovePoint remove
            ? CurveFor(command.Curve)?.Points.FirstOrDefault(point => point.Id == remove.VertexId)?.Index + 1 ?? 0 : 0;
        double halfSpan = Planform?.HalfSpanMeters ?? 0;
        RefusedPoint = null;
        return RunDirectCommandAsync(() =>
        {
            PointOutcome result;
            try { result = session.ApplyPointCommand(Guid.NewGuid().ToString("D"), command); }
            catch (ContractError) when (command is PointCommand.RemovePoint refused)
            {
                RefusedPoint = new PointRef(refused.Curve, refused.VertexId);   // read after the refusal's Notify
                throw;
            }
            var rows = PropertiesView.Curves[command.Curve];
            string change = Quantity.Typed(result.MaxDeviationMeters * PropertiesView.FieldScale[rows.ValueFamily]) + " " + rows.ValueUnit;
            string where = Quantity.TypedLength(result.AtEta * halfSpan) + " mm from root";
            string name = rows.Name.ToLowerInvariant();   // running text: "trailing edge", as COPY-190/191/200 write it
            string report;
            if (command is PointCommand.AddPoint)
            {
                var curve = Channels.View(session.Snapshot().Source, command.Curve, "accepted", 0);
                int number = curve.Points.First(point => point.Id == result.SelectId).Index + 1;
                report = $"Added {name} point {number} of {result.PointsAfter}. Shape unchanged: largest change {change}. " +
                    $"Points {number - 1} and {number + 1} moved to keep it.";
            }
            else if (command is PointCommand.RemovePoint)
                report = $"Removed {name} point {removedNumber}. Now {result.PointsAfter} points. Largest change {change} at {where}.";
            else if (command is PointCommand.RebuildCurve)
                report = $"Rebuilt the {name} with {result.PointsAfter} points. Largest change {change} at {where}. ⌘Z undoes it.";
            else
                report = $"Point change applied. Max deviation {change}.";
            if (!string.IsNullOrWhiteSpace(result.Notice)) report += " " + result.Notice;
            return new CommitOutcome.Committed(result.AcceptedId, report)
            {
                SelectPoint = result.SelectId is { Length: > 0 } ? new PointRef(command.Curve, result.SelectId) : null,
                ClearPointSelection = command is PointCommand.RebuildCurve
            };
        }, warningOnRefusal: command is PointCommand.AddPoint or PointCommand.RemovePoint or PointCommand.RebuildCurve,
            preserveStatusAfterCommit: command is PointCommand.AddPoint or PointCommand.RemovePoint or PointCommand.RebuildCurve);
    }

    /// <summary>Read-only seven-count preview from the accepted source; one Core event measures the open.</summary>
    public IReadOnlyList<RebuildPreview> PreviewRebuilds(string curve) => session.PreviewRebuilds(curve);

    public Task<CommitOutcome> ApplyChordAsync(string dimension, string text) => RunDirectCommandAsync(() =>
    {
        var result = session.ApplyChord(Guid.NewGuid().ToString("D"), new(dimension, text));
        var report = result.Report;
        string warning = report.FitAboveLimit
            ? $" — above the limit ({report.FitResidualMeters * 1e6:F2} µm; limit {report.ToleranceMeters * 1e6:F0} µm)."
            : ".";
        return new CommitOutcome.Committed(result.AcceptedId,
            $"{dimension} {report.TypedMeters * 1e3:F2} mm. Fit {report.FitResidualMeters * 1e6:F2} µm{warning} " +
            $"{report.DeviationFromLinearMeters * 1e3:F2} mm from a straight taper. " +
            $"Planform moved {report.PlanformShiftMeters * 1e3:F2} mm.");
    });

    public void ReportPointWarning(string copy)
    {
        SetStatus(copy, ReportKind.Warning);
        Notify();
    }

    public void ReportPointInfo(string copy)
    {
        SetStatus(copy, ReportKind.Info);
        Notify();
    }

    private Task<CommitOutcome> RunDirectCommandAsync(Func<CommitOutcome> action, bool warningOnRefusal = false,
        bool preserveStatusAfterCommit = false)
    {
        if (RefuseAnalysisEdit())
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("ANA-EDIT-INERT", AnalysisPointRefusal));
        if (Gesture != GestureState.Idle || draft is not null)
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("DSL-DRAFT-OWNED", "Finish the current change first."));
        if (Inspection?.Geometry.Status != GeometryStatus.Certified)
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("DSL-NOT-ASSESSED", "This foil couldn't be checked. Nothing changed."));
        Gesture = GestureState.Busy;
        Notify();
        var completion = CompleteDirectCommandAsync(action, session, stateVersion, warningOnRefusal, preserveStatusAfterCommit);
        pendingDirectCommand = completion;
        return completion;
    }

    private async Task<CommitOutcome> CompleteDirectCommandAsync(Func<CommitOutcome> action, AuthoringSession captured, long version,
        bool warningOnRefusal, bool preserveStatusAfterCommit)
    {
        try
        {
            var outcome = await Task.Run(action);
            if (!ReferenceEquals(session, captured) || stateVersion != version) return outcome;
            Inspection = session.InspectAccepted();
            UpdateEstimates();
            var committed = (CommitOutcome.Committed)outcome;
            Status = committed.Report;
            if (committed.ClearPointSelection) queuedSelection = new Selection.Foil();
            else if (committed.SelectPoint is { } point) queuedSelection = new Selection.Points([point]);
            Notify();
            _ = RefreshAcceptedAsync(preserveStatus: preserveStatusAfterCommit);
            return outcome;
        }
        catch (ContractError error)
        {
            string reason = error.Reason ?? $"{error.Code}: This change wasn't applied. Nothing changed.";
            SetStatus(reason, warningOnRefusal ? ReportKind.Warning : ReportKind.Error);
            Notify();
            return new CommitOutcome.Refused(error.Code, reason);
        }
        finally
        {
            Gesture = GestureState.Idle;
            pendingDirectCommand = null;
            Notify();
        }
    }

    private async Task CompleteGestureBeforeDocumentActionAsync(GestureEnd reason)
    {
        if (Gesture == GestureState.Busy)
        {
            if (pendingCommit is not null) await pendingCommit;
            else if (pendingDirectCommand is not null) await pendingDirectCommand;
            return;
        }
        if (Gesture != GestureState.Idle) await EndGestureAsync(reason);
    }

    public AcceptedInspection? Inspection { get; private set; }
    public AuthoredProjection? PendingProjection { get; private set; }
    public byte[]? PendingOriginal { get; private set; }
    public byte[]? PendingCandidate { get; private set; }
    public string? NativePath { get; private set; }
    public string? OpenedPath { get; private set; }
    // STATUS-CLOBBER: every write counts, so a background report replaces only the placeholder it wrote, never a newer message.
    public string Status { get => statusSlot.Text; private set => statusSlot.Write(value); }
    private readonly StatusSlot statusSlot = new("Open Example or a .foil / .cfdw.json file.");

    private void SetStatus(string text, ReportKind kind) => statusSlot.Write(text, kind);

    /// <summary>How the status strip draws <see cref="Status"/>: Info unless the write that set it named Warning or Error.</summary>
    public ReportKind StatusKind => statusSlot.Kind;

    /// <summary>The status write counter: the shell reports <see cref="Status"/> only when this moved (docs/reviews/ui-status-bar.md §2.3).</summary>
    public long StatusVersion => statusSlot.Version;

    /// <summary>Status, kind and version read together (STATUS-CLOBBER across threads).</summary>
    public (string Text, ReportKind Kind, long Version) StatusSnapshot() => statusSlot.Snapshot();

    /// <summary>
    /// STATUS-CLOBBER at the strip: a report shown from outside the controller (a Properties report, a shell message) is
    /// newer than every status written so far, so it counts as a write and a background completion no longer replaces it.
    /// Returns the version it took, so the caller records exactly that one.
    /// </summary>
    public long SupersedeStatus() => statusSlot.Supersede();
    public string Provenance { get; private set; } = "empty";
    public DisplayFrame? Frame { get; private set; }
    public IReadOnlyList<DisplayPoint> Points => Frame?.Points ?? [];
    public SessionDraft? Draft => draft;
    public AuthoredProjection? DraftProjection
    {
        get
        {
            if (draft is null) return null;
            if (draftProjection is null || projectedDraftId != draft.Id || projectedDraftGeneration != draft.Generation)
            {
                try { draftProjection = session.InspectDraft(); }
                catch (ContractError) { draftProjection = null; }
                projectedDraftId = draft.Id;
                projectedDraftGeneration = draft.Generation;
            }
            return draftProjection;
        }
    }
    public bool DraftInputValid => draftInputValid;
    public SectionReport? SectionReport { get; private set; }
    public bool HasRecovery => Inspection is not null && session.Snapshot().Recovery is not null;
    public bool IsDirty
    {
        get
        {
            if (Inspection is null) return false;
            var view = session.Snapshot();
            if (uncertainImage is not null || !draftInputValid) return true;
            return view.Draft is { } active
                ? savedDraftId != active.Id || savedDraftGeneration != active.Generation || savedAcceptedId != view.AcceptedId
                : view.Dirty;
        }
    }
    public bool SaveUncertain => uncertainImage is not null;
    public string? UncertainPath => uncertainPath;
    public string AcceptedSource => Inspection is null ? "" : Encoding.UTF8.GetString(session.Snapshot().Source);
    public string RecoverySource
    {
        get
        {
            var recovery = session.Snapshot().Recovery;
            return recovery is null ? "" : Encoding.UTF8.GetString(recovery.Utf8Base64Chunks
                .SelectMany(Convert.FromBase64String).ToArray());
        }
    }
    public IReadOnlyList<SessionEvent> LocalEvents => session.ReadLocalEvents();

    public async Task OpenExampleAsync() => await OpenFoilAsync(CfdWorkbench.Cli.Cli.ExampleBytes(), "Embedded Example");

    public async Task<OpenOutcome> NewFoilAsync(CancellationToken cancellation = default)
    {
        await CompleteGestureBeforeDocumentActionAsync(GestureEnd.New);
        long requestGen = Interlocked.Increment(ref openRequestGeneration);
        if (cancellation.IsCancellationRequested)
            return new OpenOutcome.Cancelled();

        byte[] bytes;
        try
        {
            bytes = await Task.Run(FoilSource.NewDefault, cancellation);
        }
        catch (OperationCanceledException)
        {
            return new OpenOutcome.Cancelled();
        }
        catch (Exception ex)
        {
            return new OpenOutcome.Failed(OpenFailure.Classify(ex, ""));
        }

        return CommitPreparedFoil(requestGen, bytes, null, cancellation);
    }

    public async Task<OpenOutcome> OpenAsync(string path, CancellationToken cancellation = default)
    {
        await CompleteGestureBeforeDocumentActionAsync(GestureEnd.Open);
        long requestGen = Interlocked.Increment(ref openRequestGeneration);

        if (cancellation.IsCancellationRequested)
            return new OpenOutcome.Cancelled();

        bool isNative = path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase);
        bool isFoil = path.EndsWith(".foil", StringComparison.OrdinalIgnoreCase);

        if (!isNative && !isFoil)
            return new OpenOutcome.Failed(new OpenFailure.NotRecognised("DOC-TYPE", path));

        byte[] bytes;
        ReadResult? nativeRead = null;
        try
        {
            if (isFoil)
            {
                bytes = await Task.Run(() => CfdWorkbench.Cli.Cli.ReadFoilBoundedAsync(path, cancellation), cancellation);
            }
            else
            {
                nativeRead = await Task.Run(() => store.ReadAsync(path, cancellation), cancellation);
                bytes = nativeRead.Image;
            }
        }
        catch (OperationCanceledException)
        {
            return new OpenOutcome.Cancelled();
        }
        catch (Exception) when (disposed)
        {
            // The store was closed under the read (DOC-CLOSED): Dispose superseded this open; it did not fail.
            return new OpenOutcome.Superseded();
        }
        catch (Exception ex)
        {
            return new OpenOutcome.Failed(OpenFailure.Classify(ex, path));
        }

        if (cancellation.IsCancellationRequested)
            return new OpenOutcome.Cancelled();

        if (requestGen != Volatile.Read(ref openRequestGeneration))
            return new OpenOutcome.Superseded();

        if (isFoil)
            return CommitPreparedFoil(requestGen, bytes, path, cancellation);

        var preparedSession = new AuthoringSession();
        try
        {
            preparedSession.Reopen(bytes);
        }
        catch (OperationCanceledException)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Cancelled();
        }
        catch (Exception ex)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Failed(OpenFailure.Classify(ex, path));
        }

        if (cancellation.IsCancellationRequested)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Cancelled();
        }

        if (requestGen != Volatile.Read(ref openRequestGeneration))
        {
            preparedSession.Dispose();
            return new OpenOutcome.Superseded();
        }

        if (!Adopt(preparedSession))
            return new OpenOutcome.Superseded();
        expectedDiskSha = nativeRead?.DiskSha256;
        NativePath = path;
        OpenedPath = path;
        UpdateEstimates();

        _ = RefreshAcceptedAsync(CancellationToken.None);

        Status = HasRecovery
            ? "Accepted project reopened. A separate recovery draft is available."
            : "Accepted project reopened.";
        Notify();

        return new OpenOutcome.Opened(path);
    }

    private OpenOutcome CommitPreparedFoil(long requestGen, byte[] bytes, string? openedPath, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return new OpenOutcome.Cancelled();

        if (requestGen != Volatile.Read(ref openRequestGeneration))
            return new OpenOutcome.Superseded();

        var preparedSession = new AuthoringSession();
        try
        {
            var parsed = FoilSource.Parse(bytes);
            if (!parsed.IsParsed)
            {
                preparedSession.Dispose();
                string code = parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-SYNTAX";
                ClearPendingImport();
                PendingOriginal = bytes.ToArray();
                PendingProjection = parsed.Authored();
                SetStatus($"{code}: Refused. Original source retained read-only.", ReportKind.Error);
                Provenance = Inspection is null ? "unavailable geometry" : "accepted — import refused";
                Notify();
                return new OpenOutcome.Refused(code, bytes);
            }

            var candidate = FoilSource.MaterializeIds(parsed);
            if (!candidate.AsSpan().SequenceEqual(bytes))
            {
                preparedSession.Dispose();
                ClearPendingImport();
                PendingOriginal = bytes.ToArray();
                PendingCandidate = candidate;
                PendingProjection = parsed.Authored();
                Status = "Source has no explicit control IDs. Compare the retained original with the candidate, then accept IDs.";
                Provenance = Inspection is null ? "ID candidate — not accepted" : "accepted — ID candidate pending";
                Notify();
                return new OpenOutcome.NeedsIds(candidate, bytes);
            }

            var assessment = Geometry.Assess(parsed);
            if (assessment.Status != GeometryStatus.Certified)
            {
                preparedSession.Dispose();
                ClearPendingImport();
                PendingOriginal = bytes.ToArray();
                PendingProjection = parsed.Authored();
                SetStatus($"{assessment.Code}: Refused. Original source retained read-only.", ReportKind.Error);
                Provenance = Inspection is null ? "unavailable geometry" : "accepted — import refused";
                Notify();
                return new OpenOutcome.Refused(assessment.Code, bytes);
            }

            preparedSession.Open(bytes, Guid.NewGuid().ToString("D"), false);
        }
        catch (OperationCanceledException)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Cancelled();
        }
        catch (Exception ex)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Failed(OpenFailure.Classify(ex, openedPath ?? ""));
        }

        if (cancellation.IsCancellationRequested)
        {
            preparedSession.Dispose();
            return new OpenOutcome.Cancelled();
        }

        if (requestGen != Volatile.Read(ref openRequestGeneration))
        {
            preparedSession.Dispose();
            return new OpenOutcome.Superseded();
        }

        if (!Adopt(preparedSession))
            return new OpenOutcome.Superseded();
        if (openedPath is not null)
            OpenedPath = openedPath;
        UpdateEstimates();
        _ = RefreshAcceptedAsync(CancellationToken.None);
        Status = openedPath is null ? "New foil." : "Opened.";
        Notify();
        return new OpenOutcome.Opened(openedPath ?? "");
    }

    public async Task OpenPathAsync(string path, CancellationToken cancellation = default)
    {
        var outcome = await OpenAsync(path, cancellation);
        if (outcome is OpenOutcome.Failed failed)
            throw new ContractError(failed.Failure.Code);
    }

    public async Task OpenFoilAsync(byte[] bytes, string label, CancellationToken cancellation = default)
    {
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed)
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingProjection = parsed.Authored();
            Status = parsed.Diagnostics.FirstOrDefault() is { } diagnostic
                ? $"{diagnostic.Code}: {diagnostic.Reason} {diagnostic.Recovery}"
                : "Source rejected. Accepted geometry is unavailable.";
            Provenance = Inspection is null ? "invalid source" : "accepted — import refused";
            Notify();
            return;
        }
        var candidate = FoilSource.MaterializeIds(parsed);
        if (!candidate.AsSpan().SequenceEqual(bytes))
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingCandidate = candidate;
            PendingProjection = parsed.Authored();
            Status = "Source has no explicit control IDs. Compare the retained original with the candidate, then accept IDs.";
            Provenance = Inspection is null ? "ID candidate — not accepted" : "accepted — ID candidate pending";
            Notify();
            return;
        }
        var assessment = Geometry.Assess(parsed);
        if (assessment.Status != GeometryStatus.Certified)
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingProjection = parsed.Authored();
            Status = $"{assessment.Status}: {assessment.Reason} Original source retained read-only.";
            Provenance = Inspection is null ? "unavailable geometry" : "accepted — import refused";
            Notify();
            return;
        }
        var next = new AuthoringSession();
        try { next.Open(bytes, Guid.NewGuid().ToString("D"), false); }
        catch { next.Dispose(); throw; }
        if (!Adopt(next)) return;
        OpenedPath = label;
        UpdateEstimates();
        await RefreshAcceptedAsync(cancellation);
    }

    public async Task AcceptCandidateAsync(CancellationToken cancellation = default)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (PendingOriginal is null || PendingCandidate is null) throw new ContractError("DSL-IDS-REQUIRED");
        var next = new AuthoringSession();
        try { next.Open(PendingOriginal, Guid.NewGuid().ToString("D"), true); }
        catch { next.Dispose(); throw; }
        if (!Adopt(next)) return;
        await RefreshAcceptedAsync(cancellation);
    }

    public ScopeImpact DescribeScope(int assignmentIndex, SectionScope scope)
    {
        if (Inspection is null) throw new ContractError("DOC-EMPTY");
        if ((uint)assignmentIndex >= (uint)Inspection.Authored.Assignments.Count) throw new ContractError("DSL-PROFILE-TARGET");
        string profile = Inspection.Authored.Assignments[assignmentIndex].ProfileName;
        return session.DescribeScope(profile, assignmentIndex, scope);
    }

    public ProfileView SectionView(int assignmentIndex)
    {
        string key = SectionViewKey(assignmentIndex);
        if (sectionViews.TryGetValue(assignmentIndex, out var cached) && cached.Key == key) return cached.View;
        var view = session.ProfileAt(assignmentIndex);
        sectionViews[assignmentIndex] = (key, view);
        return view;
    }

    public void InvalidateDraftInput(string? reason = null)
    {
        if (RefuseAnalysisEdit()) return;
        if (draft is null) return;
        CancelSampling();
        draftInputValid = false;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "draft — invalid numeric input";
        Status = reason ?? "Enter a finite numeric aft position. Preview and Save are blocked until corrected.";
        Notify();
    }

    public async Task PreviewAsync(CancellationToken cancellation = default)
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        CancelSampling();
        long version = stateVersion;
        var capture = draft;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeSampling = linked;
        SectionReport = null;
        Status = $"{SectionDraftPrefix()}Assessing draft geometry…";
        Notify();
        try
        {
            var assessment = await Task.Run(() => session.Validate(capture.Id, capture.Generation, linked.Token), linked.Token);
            if (version != stateVersion || draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
            Remember(assessment);
            if (assessment.Status != GeometryStatus.Certified || assessment.Certificate is null)
            {
                Frame = acceptedFrame;
                Provenance = "draft — unavailable geometry";
                Status = $"{SectionDraftPrefix()}{assessment.Code}: {assessment.Status}. {string.Join(" ", assessment.Diagnostics.Select(d => d.Reason))}";
                Notify();
                return;
            }
            double eta = interiorEta;
            var frame = await Task.Run(() => Sample(assessment.Certificate, assessment.Key!.SourceHash, "preview", eta, linked.Token), linked.Token);
            if (version != stateVersion || draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
            Frame = frame;
            Provenance = "preview";
            Status = $"{SectionDraftPrefix()}Preview of {capture.Rail} {capture.VertexId}; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
            Notify();
        }
        catch (OperationCanceledException) { }
        catch (ContractError error) when (error.Code == "GEOMETRY-CANCELLED") { }
        finally { if (ReferenceEquals(activeSampling, linked)) activeSampling = null; }
    }

    public void Apply()
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        // A resumed rail recovery has a draft but no latched preview: ResumeRecovery samples the
        // accepted frame, and that refresh can finish the version check inside PreviewAsync before
        // Remember runs. Certify the open draft here, then commit it.
        if (draft is not null && draftInputValid &&
            (currentAssessment is null || currentAssessment.Status != GeometryStatus.Certified || Frame?.Provenance != "preview"))
            AssessDraftNow();
        if (draft is null || !draftInputValid || currentAssessment is null || currentAssessment.Status != GeometryStatus.Certified || Frame?.Provenance != "preview")
            throw new ContractError("DSL-NOT-ASSESSED");
        CancelSampling();
        session.Apply(Guid.NewGuid().ToString("D"), currentAssessment);
        draft = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Inspection = session.InspectAccepted();
        acceptedFrame = Frame with { Provenance = "accepted", SourceHash = Inspection.Authored.Binding.SourceHash };
        Frame = acceptedFrame;
        Provenance = "accepted";
        Status = "Draft applied as one accepted source revision. Save to persist it.";
        UpdateEstimates();
        Notify();
    }

    public void Cancel()
    {
        if (RefuseAnalysisEdit()) return;
        if (draft is null) return;
        CancelSampling();
        session.Cancel(draft.Id);
        draft = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "accepted";
        Status = "Draft cancelled. Accepted source and history are unchanged.";
        Notify();
        if (Frame is null) _ = RefreshAcceptedAsync();
    }

    public void Undo()
    {
        if (RefuseAnalysisEdit()) return;
        if (Section is not null) { _ = UndoSectionStepAsync(); return; }
        if (Gesture != GestureState.Idle || draft is not null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        session.Undo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        UpdateEstimates();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Undo selected the preceding accepted source revision.";
        Notify();
        _ = RefreshAcceptedAsync(preserveStatus: true);
    }

    public void Redo()
    {
        if (RefuseAnalysisEdit()) return;
        if (Section is not null) { _ = RedoSectionStepAsync(); return; }
        if (Gesture != GestureState.Idle || draft is not null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        session.Redo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        UpdateEstimates();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Redo selected the next accepted source revision.";
        Notify();
        _ = RefreshAcceptedAsync(preserveStatus: true);
    }

    public async Task<SaveResult> SaveAsync(string path, CancellationToken cancellation = default)
    {
        await CompleteGestureBeforeDocumentActionAsync(GestureEnd.Save);
        if (Section is not null)
        {
            Status = "Finish or Cancel the section before saving.";
            Notify();
            throw new ContractError("DSL-DRAFT-OWNED");
        }
        if (!path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase)) throw new ContractError("DOC-TYPE");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        if (uncertainImage is not null) throw new ContractError("DOC-SAVE-UNCERTAIN");
        if (Interlocked.CompareExchange(ref saving, 1, 0) != 0) throw new ContractError("DOC-SAVE-PENDING");
        try
        {
            var capturedSession = session;
            var capturedStore = store;
            if (capturedSession.Snapshot().Draft is not null) capturedSession.CaptureRecovery();
            var capturedView = capturedSession.Snapshot();
            byte[] image = capturedSession.SaveImage();
            var result = await capturedStore.SaveAsync(path, new SaveRequest(image, path == NativePath ? expectedDiskSha : null,
                Guid.NewGuid().ToString("D")), cancellation);
            if (!ReferenceEquals(session, capturedSession)) return result;
            string hash = Identity.Sha256(image);
            if (result.Code == "OK" && result.PublicationKnown && result.DurabilityConfirmed && result.PublishedSha256 == hash)
            {
                capturedSession.AcknowledgeSaved(image);
                NativePath = path;
                expectedDiskSha = hash;
                MarkSavedDraft(capturedView);
                Status = "Saved and durability confirmed.";
            }
            else
            {
                if (result.Code == "DOC-SAVE-UNCERTAIN" || result.PublicationKnown && !result.DurabilityConfirmed)
                {
                    uncertainImage = image;
                    uncertainPath = path;
                    uncertainDraftId = capturedView.Draft?.Id;
                    uncertainDraftGeneration = capturedView.Draft?.Generation ?? 0;
                    uncertainAcceptedId = capturedView.AcceptedId;
                }
                SetStatus(uncertainImage is null ? Labels.SaveRefusal(result.Code)
                    : $"{result.Code}: Save was not acknowledged. The attempted path and image are retained for a durable retry.",
                    result.Code == "DOC-UNSUPPORTED-PERSISTENCE" ? ReportKind.Warning : ReportKind.Error);
            }
            Notify();
            return result;
        }
        finally { Volatile.Write(ref saving, 0); }
    }

    public async Task<bool> ResolveUncertainSaveAsync(CancellationToken cancellation = default)
    {
        if (uncertainImage is null || uncertainPath is null) throw new ContractError("DOC-SAVE-UNCERTAIN");
        if (Interlocked.CompareExchange(ref saving, 1, 0) != 0) throw new ContractError("DOC-SAVE-PENDING");
        try
        {
        var capturedSession = session;
        var capturedStore = store;
        var capturedImage = uncertainImage;
        var capturedPath = uncertainPath;
        ReadResult read;
        try { read = await capturedStore.ReadAsync(capturedPath, cancellation); }
        catch (ContractError error)
        {
            if (!ReferenceEquals(session, capturedSession)) return false;
            Status = $"{error.Code}: Uncertain save remains dirty; disk image could not be compared.";
            Notify();
            return false;
        }
        if (!ReferenceEquals(session, capturedSession)) return false;
        if (read.DiskSha256 != Identity.Sha256(capturedImage))
        {
            Status = "Disk differs from the captured save image. Keep this draft dirty; reopen or save to a new path after review.";
            Notify();
            return false;
        }
        var retry = await capturedStore.SaveAsync(capturedPath,
            new SaveRequest(capturedImage, read.DiskSha256, Guid.NewGuid().ToString("D")), cancellation);
        if (!ReferenceEquals(session, capturedSession)) return false;
        if (retry.Code != "OK" || !retry.PublicationKnown || !retry.DurabilityConfirmed ||
            retry.PublishedSha256 != read.DiskSha256)
        {
            Status = $"{retry.Code}: Matching readback did not confirm durability; save remains dirty.";
            Notify();
            return false;
        }
        capturedSession.AcknowledgeSaved(capturedImage);
        NativePath = capturedPath;
        expectedDiskSha = retry.PublishedSha256;
        savedDraftId = uncertainDraftId;
        savedDraftGeneration = uncertainDraftGeneration;
        savedAcceptedId = uncertainAcceptedId;
        uncertainImage = null;
        uncertainPath = null;
        uncertainDraftId = uncertainAcceptedId = null;
        Status = "Matching disk image was published again and durability confirmed; save acknowledged.";
        Notify();
        return true;
        }
        finally { Volatile.Write(ref saving, 0); }
    }

    public void ResumeRecovery()
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        session.ResumeRecovery();
        draft = session.Snapshot().Draft;
        draftInputValid = true;
        MarkSavedDraft(session.Snapshot());
        if (session.CurrentSectionDraft() is { } sectionView)
        {
            sectionBeforeChecking = null;
            Section = new SectionMode(sectionView, session.Snapshot().Source, EntryOrigin.Recovery);
            interiorEta = Inspection!.Authored.Assignments[sectionView.Assignment].Eta;
            Frame = acceptedFrame;
            Provenance = "recovery section — accepted geometry shown";
            Status = "Section recovery resumed. Inner undo starts here.";
            var first = SectionCurve(SurfaceSide.Upper)!.Points[0];
            Select(new Selection.Points([new PointRef("upper", first.Id, sectionView.Profile)]));
            NotifySection();
            return;
        }
        interiorEta = Inspection?.Authored.Rails.Single(r => r.Name == draft!.Rail).Controls
            .Single(c => c.Id == draft!.VertexId).Eta ?? .5;
        Frame = acceptedFrame = null;
        Status = $"Recovery draft resumed separately at η {interiorEta:G3}; Preview to assess it.";
        Provenance = "recovery draft — accepted sampling";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void DiscardRecovery()
    {
        if (RefuseAnalysisEdit()) throw new ContractError("ANA-EDIT-INERT", AnalysisPointRefusal);
        session.DiscardRecovery();
        Status = "Recovery draft discarded. Accepted project retained.";
        Notify();
    }

    private async Task RefreshAcceptedAsync(CancellationToken cancellation = default, bool preserveStatus = false)
    {
        CancelSampling();
        var inspected = session.InspectAccepted();
        Inspection = inspected;
        if (Frame?.SourceHash != inspected.Authored.Binding.SourceHash || Frame?.InteriorEta != interiorEta)
            Frame = acceptedFrame = null;
        double eta = interiorEta;
        long version = stateVersion;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeSampling = linked;
        Provenance = draft is null ? "accepted — sampling" : "draft — accepted sampling";
        long placeholder = preserveStatus ? statusSlot.Version :
            statusSlot.Write($"{SectionDraftPrefix()}Sampling accepted geometry at η {eta:G3}…");
        Notify();
        try
        {
            if (inspected.Geometry.Certificate is null)
            {
                Frame = null;
                Status = $"{SectionDraftPrefix()}{inspected.Geometry.Status}: {inspected.Geometry.Reason}";
                Notify();
                return;
            }
            var frame = await Task.Run(() => Sample(inspected.Geometry.Certificate, inspected.Authored.Binding.SourceHash,
                "accepted", eta, linked.Token), linked.Token);
            if (version != stateVersion || Inspection?.Authored.Binding.SourceHash != frame.SourceHash || interiorEta != frame.InteriorEta) return;
            Frame = acceptedFrame = frame;
            Provenance = draft is null ? "accepted" : "draft — accepted geometry shown";
            // A message written since the placeholder (a lock refusal, an open's recovery notice, a report the strip shows)
            // is newer than this report; the compare and the write are one step on any thread (StatusSlot).
            if (!preserveStatus)
                statusSlot.TryReplace(placeholder,
                    $"{SectionDraftPrefix()}Accepted η {eta:G3} slice; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.");
            Notify();
        }
        catch (OperationCanceledException) { }
        catch (ContractError error) when (error.Code == "GEOMETRY-CANCELLED") { }
        catch (ContractError error) { Status = $"{SectionDraftPrefix()}{error.Code}: Geometry display unavailable; accepted source retained."; Notify(); }
        finally { if (ReferenceEquals(activeSampling, linked)) activeSampling = null; }
    }

    private static DisplayFrame Sample(GeometryCertificate certificate, string sourceHash, string provenance, double interiorEta, CancellationToken cancellation)
    {
        var watch = Stopwatch.StartNew();
        var points = new List<DisplayPoint>(15);
        foreach (double eta in new[] { 0d, interiorEta, 1d })
            foreach (var sample in new (double X, bool Upper)[] { (0, true), (.5, true), (1, true), (.5, false), (1, false) })
                points.Add(new(eta, sample.X, sample.Upper,
                    Geometry.PointAt(certificate, eta, sample.X, sample.Upper, cancellationToken: cancellation)));
        var section = Geometry.SectionAt(certificate, interiorEta, .5, cancellationToken: cancellation);
        return new(points.AsReadOnly(), section, interiorEta, watch.Elapsed.TotalMilliseconds, sourceHash, provenance);
    }

    private void CancelSampling()
    {
        Interlocked.Increment(ref stateVersion);
        activeSampling?.Cancel();
        activeSampling = null;
    }

    private void ClearPendingImport()
    {
        PendingProjection = null;
        PendingOriginal = null;
        PendingCandidate = null;
    }

    private void MarkSavedDraft(SessionView view)
    {
        savedDraftId = view.Draft?.Id;
        savedDraftGeneration = view.Draft?.Generation ?? 0;
        savedAcceptedId = view.AcceptedId;
    }

    /// <summary>
    /// Makes <paramref name="next"/> the document. False on a disposed controller: an open that completes after Dispose
    /// is superseded, so its session is disposed here, no store is made and no view hears of it (UI-LIFETIME).
    /// </summary>
    private bool Adopt(AuthoringSession next)
    {
        if (disposed)
        {
            next.Dispose();
            return false;
        }
        CancelSampling();
        CancelAnalysis();
        analysisCancellation = null;
        store.Dispose();
        session.Dispose();
        session = next;
        analysisService = new AnalysisService(session, analysisMethod, analysisBarrier, time);
        analysisProjectionKey = null;
        analysisView = null;
        LayerSet = [];
        areaMode = ShellMode.Workspace;
        returnToCadMode = ShellMode.Workspace;
        store = storeFactory(session);
        Inspection = session.InspectAccepted();
        ClearPendingImport();
        NativePath = null;
        OpenedPath = null;
        expectedDiskSha = null;
        uncertainImage = null;
        uncertainPath = null;
        uncertainDraftId = uncertainAcceptedId = null;
        savedDraftId = savedAcceptedId = null;
        interiorEta = .5;
        acceptedFrame = null;
        Frame = null;
        // A new document draws "Drawing…" until its first mesh; the cameras refit to it. Layout and display modes stay.
        Surface = null;
        SurfaceNote = null;
        surfaceKey = null;
        camera3d = null;
        elevationCameras.Clear();
        draft = null;
        Gesture = GestureState.Idle;
        gesturePoint = null;
        gestureOrigin = null;
        pendingGestureTarget = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        replaceNames.Clear();
        UpdateEstimates();
        var prevSelection = Selection;
        Selection = new Selection.Foil();
        Provenance = "empty";
        Status = "Opening…";
        if (!Equals(prevSelection, Selection))
        {
            SelectionChanged?.Invoke();
        }
        Notify();
        return true;
    }

    private void RequireCertifiedFoil()
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
    }

    private void AssessDraftNow()
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        CancelSampling();
        var capture = draft;
        SectionReport = null;
        Status = $"{SectionDraftPrefix()}Assessing draft geometry…";
        Notify();
        var assessment = session.Validate(capture.Id, capture.Generation);
        if (draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
        Remember(assessment);
        sectionViews.Clear();
        if (assessment.Status != GeometryStatus.Certified || assessment.Certificate is null || assessment.Key is null)
        {
            Frame = acceptedFrame;
            Provenance = "draft — unavailable geometry";
            Status = $"{SectionDraftPrefix()}{assessment.Code}: {assessment.Status}. {string.Join(" ", assessment.Diagnostics.Select(item => item.Reason))}";
            Notify();
            return;
        }
        var frame = Sample(assessment.Certificate, assessment.Key.SourceHash, "preview", interiorEta, CancellationToken.None);
        if (draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
        Frame = frame;
        Provenance = "preview";
        Status = $"{SectionDraftPrefix()}Preview of {capture.Rail} {capture.VertexId}; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
        Notify();
    }

    private void Remember(SessionAssessment assessment)
    {
        currentAssessment = assessment;
        SectionReport = CfdWorkbench.Desktop.SectionReport.Create(assessment);
    }

    private string SectionDraftPrefix() =>
        draft is { Profile: not null, Assignment: >= 0 } section ? $"Draft {section.Id} owns station {section.Assignment}. " : "";

    private string SectionViewKey(int assignmentIndex)
    {
        var binding = Inspection?.Authored.Binding;
        string accepted = binding?.AcceptedId ?? "";
        string hash = binding?.SourceHash ?? "";
        if (draft is { Profile: not null } active && active.Assignment == assignmentIndex)
            return accepted + ":" + hash + ":d:" + active.Id + ":" + active.Generation;
        return accepted + ":" + hash + ":a";
    }

    private void Notify()
    {
        // UI-LIFETIME: a callback queued before Dispose (the 250 ms surface timer, a command's finally) still runs after
        // it; a disposed controller notifies no view, because every view read would hit the closed session (DOC-CLOSED).
        if (disposed || isNotifying) return;
        isNotifying = true;
        try
        {
            while (true)
            {
                if (queuedSelection is not null)
                {
                    var next = queuedSelection;
                    queuedSelection = null;
                    var reconciled = ReconcileCurrent(next);
                    if (Equals(Selection, reconciled))
                    {
                        if (queuedSelection is null)
                            break;
                        continue;
                    }
                    Selection = reconciled;
                    SelectionChanged?.Invoke();
                }
                else
                {
                    var reconciled = ReconcileCurrent(Selection);
                    if (!Equals(Selection, reconciled))
                    {
                        Selection = reconciled;
                        SelectionChanged?.Invoke();
                    }
                }

                RequestSurfaceIfChanged();
                Changed?.Invoke();

                if (queuedSelection is null)
                    break;
            }
        }
        finally
        {
            isNotifying = false;
        }
    }

    private Selection ReconcileCurrent(Selection current)
    {
        if (Section is { } mode && current is Selection.Points points)
        {
            var kept = points.Items.Where(item => item.Profile == mode.Draft.Profile &&
                item.Curve is "upper" or "lower" &&
                SectionCurve(item.Curve == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower)!.Points.Any(p => p.Id == item.VertexId)).ToArray();
            // All kept is the same selection: a rebuilt list would read as a change and raise SelectionChanged, a second
            // full shell refresh on every notify in the section mode (release-freeze).
            if (kept.Length == points.Items.Count) return current;
            if (kept.Length > 0) return new Selection.Points(kept);
        }
        return Reconcile(current, CurrentProjection, ChannelPointExists);
    }

    private void NotifyCamera(SingleView view)
    {
        // UI-LIFETIME, as Notify: a disposed controller notifies no view.
        if (disposed) return;
        CameraChanged?.Invoke(view);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        sectionBeforeChecking = null;
        Gesture = GestureState.Idle;
        CancelSampling();
        CancelAnalysis();
        surfaceRunning?.Cancel();
        surfaceBehindTimer?.Dispose();
        surfaceSettled.TrySetResult();
        store.Dispose();
        session.Dispose();
        Selection = new Selection.None();
        Estimates = null;
    }
}
