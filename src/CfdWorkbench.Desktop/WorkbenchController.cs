using CfdWorkbench.Cli;
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

public enum GestureState { Idle, Pressed, Dragging, Nudging, Busy }
public enum GestureInput { Pointer, Keyboard, Typed }
public enum GestureEnd { Release, KeyUp, Escape, CaptureLost, FocusLost, Deactivated, Save, Close, Open, New }
public enum NudgeModifier { Command, Plain, Shift }
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

    /// <summary>Computes one display mesh off the UI thread; the default is <see cref="Placement.Surface"/>.</summary>
    public delegate Task<SurfaceView> SurfaceCompute(byte[] source, string basis, long generation, CancellationToken cancellation);

    private sealed record SurfaceRequest(long Ticket, byte[] Bytes, string Basis, long Generation);

    /// <summary>A mesh request older than this shows "· Updating…" (TQ reactive recompute).</summary>
    public static readonly TimeSpan SurfaceBehindAfter = TimeSpan.FromMilliseconds(250);

    public const string SurfaceKeptNote = "Showing the last shape that could be drawn.";

    public WorkbenchController(Func<AuthoringSession, IProjectStore>? storeFactory = null,
        SurfaceCompute? surfaceCompute = null, TimeProvider? time = null, Func<long, Task>? sectionAssessmentGate = null)
    {
        this.storeFactory = storeFactory ?? (active => new ProjectStore(active));
        store = this.storeFactory(session);
        this.surfaceCompute = surfaceCompute ?? ((source, basis, generation, cancellation) =>
            Task.Run(() => Placement.Surface(source, basis, generation, cancellation), cancellation));
        this.time = time ?? TimeProvider.System;
        this.sectionAssessmentGate = sectionAssessmentGate;
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
        draft?.Bytes ?? session.Snapshot().Source, draft is null ? "accepted" : "preview", draft?.Generation ?? 0);
    public GestureState Gesture
    {
        get;
        private set
        {
            field = value;
            // The preview belongs to a live drag: release, Escape, a refusal or a new gesture clear it.
            if (value != GestureState.Dragging) GestureCrossing = null;
        }
    }

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
            view = Channels.View(draft?.Bytes ?? session.Snapshot().Source, curve, draft is null ? "accepted" : "preview",
                draft?.Generation ?? 0);
        }
        catch (ContractError) { return null; }
        channelViews[curve] = view;
        return view;
    }

    private string SourceKey() => draft is { } active
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
        if (Application.Current is null || Dispatcher.UIThread.CheckAccess()) action();
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
    public bool CanUndo => HistoryAvailability().Undo;
    public bool CanRedo => HistoryAvailability().Redo;

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
        if (disposed) throw new ContractError("DOC-CLOSED");
        if (Section is { } open)
        {
            if (open.IsDirty) throw new ContractError("DSL-DRAFT-OWNED", "Finish or cancel this section before editing another.");
            CancelSectionAssessment();
            session.Cancel(open.Draft.DraftId);
            Section = null;
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

    /// <summary>Appends one structural step, then checks its bytes with the real Core certificate.</summary>
    public async Task ApplySectionStepAsync(SectionStep step, CancellationToken cancellation = default)
    {
        if (Section is not { } mode) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSectionAssessment();
        SectionDraftView next;
        try
        {
            next = session.ApplySectionStep(mode.Draft.DraftId, mode.Draft.Generation, step);
            SectionRefitRefusal = null;
        }
        catch (ContractError error)
        {
            SectionRefitRefusal = error.Data["RefitMaximumChordX"] is double x &&
                error.Data["RefitAffectedSide"] is SurfaceSide side &&
                error.Data["RefitDeviationMeters"] is double deviation &&
                error.Data["RefitLimitMeters"] is double limit ? (side, x, deviation, limit) : null;
            Status = error.Message;
            NotifySection();
            throw;
        }
        draft = session.Snapshot().Draft;
        Section = mode with { Draft = next, Assessment = null, FinishReason = "Checking…" };
        // One shell refresh per step: the assessment's "Checking…" write notifies, after the strip has the step report.
        RaiseSectionChanged();
        await AssessCurrentSectionAsync(cancellation);
    }

    /// <summary>
    /// A section pointer-drag frame (§3.7): records where the release will move the point, and nothing else. The canvas
    /// draws the frame from its own display state; a shell refresh here cost 0.55–0.9 s per move (edit-lag, 2026-10-04).
    /// </summary>
    public void UpdateSectionGesture(double x, double y, double pixelsFromPress)
    {
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

    public void UndoSectionStep()
    {
        if (Section is not { } mode) throw new ContractError("DSL-DRAFT-OWNED");
        SectionRefitRefusal = null;
        CancelSectionAssessment();
        var next = session.UndoSectionStep(mode.Draft.DraftId);
        draft = session.Snapshot().Draft;
        Section = mode with { Draft = next, Assessment = null, FinishReason = next.Cursor == mode.Draft.Cursor ? "No earlier step." : "Checking…" };
        AfterCursorMove(next.Cursor != mode.Draft.Cursor);
    }

    // A moved cursor refreshes the shell once, through the assessment's "Checking…" write; a no-op says so at once.
    private void AfterCursorMove(bool moved)
    {
        if (!moved) { NotifySection(); return; }
        RaiseSectionChanged();
        _ = AssessCurrentSectionAsync();
    }

    public void RedoSectionStep()
    {
        if (Section is not { } mode) throw new ContractError("DSL-DRAFT-OWNED");
        SectionRefitRefusal = null;
        CancelSectionAssessment();
        var next = session.RedoSectionStep(mode.Draft.DraftId);
        draft = session.Snapshot().Draft;
        Section = mode with { Draft = next, Assessment = null, FinishReason = next.Cursor == mode.Draft.Cursor ? "No later step." : "Checking…" };
        AfterCursorMove(next.Cursor != mode.Draft.Cursor);
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
            var result = await Task.Run(() => session.AssessSection(mode.Draft.DraftId, mode.Draft.Generation, linked.Token));
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

    private void CancelSectionAssessment()
    {
        ++sectionAssessmentTicket;
        sectionAssessmentCancellation?.Cancel();
        sectionAssessmentCancellation = null;
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
        if (Section is not { } mode) throw new ContractError("DSL-DRAFT-OWNED");
        if (mode.Assessment is null) await AssessCurrentSectionAsync();
        mode = Section ?? throw new ContractError("DSL-CONFLICT");
        if (!mode.CanFinish || mode.Assessment is null)
        {
            Status = mode.FinishReason ?? "This section cannot Finish yet.";
            NotifySection();
            throw new ContractError("DSL-NOT-ASSESSED");
        }
        CancelSectionAssessment();
        session.FinishSection(Guid.NewGuid().ToString("D"), mode.Assessment);
        Section = null;
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
        if (Section is not { } mode) return;
        CancelSectionAssessment();
        session.Cancel(mode.Draft.DraftId);
        Section = null;
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
        Select(new Selection.Points([point]));
        if (view.Freedom == PointFreedom.Fixed)
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
            Gesture = GestureState.Dragging;
        }
        if (Section is not null)
        {
            pendingGestureTarget = (spanMeters, aftMeters);
            Notify();
            return;
        }
        EnsurePointDraft();
        pendingGestureTarget = (spanMeters, aftMeters);
        ScheduleGestureFrame();
    }

    public void Nudge(int spanDirection, int aftDirection, NudgeModifier modifier)
    {
        if (Gesture != GestureState.Nudging || gestureOrigin is null || gesturePoint is null) return;
        if (Section is not null)
        {
            double sectionStep = modifier switch { NudgeModifier.Command => .0001, NudgeModifier.Shift => .01, _ => .001 };
            var sectionAt = pendingGestureTarget ?? (gestureOrigin.SpanMeters, gestureOrigin.Ordinate);
            UpdateGestureTarget(sectionAt.Item1 + spanDirection * sectionStep, sectionAt.Item2 + aftDirection * sectionStep);
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
        draft = session.BeginPointGesture(Guid.NewGuid().ToString("D"), gesturePoint.Curve, gesturePoint.VertexId);
        gestureOperationId = draft.Id;
        draftProjection = null;
    }

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
        if (draft is null || pendingGestureTarget is not { } target || Gesture is GestureState.Idle or GestureState.Busy)
            return;
        pendingGestureTarget = null;
        var timer = Stopwatch.StartNew();
        var frame = session.UpdatePointGesture(draft.Id, draft.Generation, target.Span, target.Aft);
        gestureUpdateTimes.Add(timer.Elapsed.TotalMilliseconds);
        draft = frame.Draft;
        draftProjection = null;
        gestureFrames++;
        if (frame.Clamped) gestureClamped++;
        timer.Restart();
        try { Estimates = WingEstimates.From(draft.Bytes, "preview", draft.Generation); }
        catch { Estimates = null; }
        gestureEstimateTimes.Add(timer.Elapsed.TotalMilliseconds);
        GestureCrossing = Gesture == GestureState.Dragging && gesturePoint is { Curve: "leading" or "trailing" } dragged &&
            Planform is { } plan ? EdgeHullCrossing(plan, dragged.Curve) : null;
        Notify();
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
        return new GestureOutcome.Committed(Section!.Draft.DraftId, "Section step added.");
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
        if (outcome is GestureOutcome.Committed accepted && gestureOrigin is { } origin && CurveFor(origin.Curve) is { } curve &&
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
            code: (outcome as GestureOutcome.Refused)?.Code, clampedCount: gestureClamped,
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
            SetStatus(error.Reason ?? $"{error.Code}: This change wasn't applied. Nothing changed.",
                warningOnRefusal ? ReportKind.Warning : ReportKind.Error);
            Notify();
            return new CommitOutcome.Refused(error.Code, Status);
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
        if (Section is not null) { UndoSectionStep(); return; }
        if (Gesture != GestureState.Idle || draft is not null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        session.Undo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        UpdateEstimates();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Undo selected the preceding accepted source revision. Sampling…";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void Redo()
    {
        if (Section is not null) { RedoSectionStep(); return; }
        if (Gesture != GestureState.Idle || draft is not null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        session.Redo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        UpdateEstimates();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Redo selected the next accepted source revision. Sampling…";
        Notify();
        _ = RefreshAcceptedAsync();
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
                Status = $"{result.Code}: Save was not acknowledged. {(uncertainImage is null ? "Resolve the refusal before retry." : "The attempted path and image are retained for a durable retry.")}";
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
        session.ResumeRecovery();
        draft = session.Snapshot().Draft;
        draftInputValid = true;
        MarkSavedDraft(session.Snapshot());
        if (session.CurrentSectionDraft() is { } sectionView)
        {
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
        store.Dispose();
        session.Dispose();
        session = next;
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
        Gesture = GestureState.Idle;
        CancelSampling();
        surfaceRunning?.Cancel();
        surfaceBehindTimer?.Dispose();
        surfaceSettled.TrySetResult();
        store.Dispose();
        session.Dispose();
        Selection = new Selection.None();
        Estimates = null;
    }
}
