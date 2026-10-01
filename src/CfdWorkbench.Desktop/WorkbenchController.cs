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
    public sealed record Committed(string AcceptedId, string Report) : CommitOutcome;
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

    public WorkbenchController(Func<AuthoringSession, IProjectStore>? storeFactory = null)
    {
        this.storeFactory = storeFactory ?? (active => new ProjectStore(active));
        store = this.storeFactory(session);
    }
    public event Action? Changed;
    public Selection Selection { get; private set; } = new Selection.None();
    public event Action? SelectionChanged;
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
    public (double SpanMeters, double AftMeters)? GestureCrossing { get; private set; }

    public PlanCamera PlanCamera { get; set; } = new();
    public bool CombVisible { get; set; }
    public int LastGestureFrames { get; private set; }

    public AuthoredProjection? CurrentProjection => DraftProjection ?? Inspection?.Authored ?? PendingProjection;

    // Changed is raised after each accepted edit and cursor move; menu commands requery these values.
    public bool CanUndo => HistoryAvailability().Undo;
    public bool CanRedo => HistoryAvailability().Redo;

    private (bool Undo, bool Redo) HistoryAvailability()
    {
        if (Inspection is null || draft is not null || Gesture != GestureState.Idle) return (false, false);
        var history = session.Envelope();
        var (current, redo) = NativeProject.Replay(history);
        return (history.Accepted.Single(item => item.Id == current).Parent is not null, redo.Length != 0);
    }

    public void Select(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (isNotifying)
        {
            queuedSelection = selection;
            return;
        }
        queuedSelection = selection;
        Notify();
    }

    public static Selection Reconcile(Selection current, AuthoredProjection? projection)
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
                if (PointExists(item, projection))
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

        if (item.Profile is not null && projection.Assignments.Any(a => a.ProfileName == item.Profile))
            return true;

        return false;
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
        var plan = Planform;
        var view = (point.Curve == "leading" ? plan?.Leading : point.Curve == "trailing" ? plan?.Trailing : null)?
            .Points.FirstOrDefault(candidate => candidate.Id == point.VertexId);
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
            Status = $"This {view.Role} point is fixed by the foil definition.";
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

    public void UpdateGesture(double spanMeters, double aftMeters)
    {
        if (Gesture == GestureState.Nudging && gestureInput == GestureInput.Keyboard) return;
        UpdateGestureTarget(spanMeters, aftMeters);
    }

    private void UpdateGestureTarget(double spanMeters, double aftMeters)
    {
        if (Gesture is not (GestureState.Pressed or GestureState.Dragging or GestureState.Nudging) || gestureOrigin is null)
            return;
        if (!double.IsFinite(spanMeters) || !double.IsFinite(aftMeters)) return;
        if (Gesture == GestureState.Pressed)
        {
            double px = Math.Sqrt(Math.Pow(spanMeters - gestureOrigin.SpanMeters, 2) +
                Math.Pow(aftMeters - gestureOrigin.AftMeters, 2)) * PlanCamera.PixelsPerMeter;
            if (px < 3) return;
            Gesture = GestureState.Dragging;
        }
        EnsurePointDraft();
        pendingGestureTarget = (spanMeters, aftMeters);
        ScheduleGestureFrame();
    }

    public void Nudge(int spanDirection, int aftDirection, NudgeModifier modifier)
    {
        if (Gesture != GestureState.Nudging || gestureOrigin is null) return;
        double step = modifier switch { NudgeModifier.Command => 0.00001, NudgeModifier.Shift => 0.001, _ => 0.0001 };
        var current = pendingGestureTarget ?? (gestureOrigin.SpanMeters, gestureOrigin.AftMeters);
        UpdateGestureTarget(current.Item1 + spanDirection * step, current.Item2 + aftDirection * step);
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
        GestureCrossing = Gesture == GestureState.Dragging && Planform is { } plan && gesturePoint is { } dragged
            ? EdgeHullCrossing(plan, dragged.Curve) : null;
        Notify();
    }

    /// <summary>
    /// Null when every trailing-rail Bernstein ordinate is strictly aft of every leading-rail one (the certificate's
    /// positive-chord rule); otherwise the dragged rail's offending Bernstein coefficient, in plan metres.
    /// </summary>
    public static (double SpanMeters, double AftMeters)? EdgeHullCrossing(PlanformView plan, string draggedCurve)
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
        var points = curve.Points.Select(point => (point.Eta, Aft: point.AftMeters)).ToList();
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
        if (Gesture == GestureState.Busy) return Task.FromResult<GestureOutcome>(new GestureOutcome.NoChange());
        if (Gesture == GestureState.Idle)
        {
            if (reason == GestureEnd.Escape && Selection is Selection.Points selected)
            {
                var chosen = selected.Items.FirstOrDefault();
                var rail = chosen?.Curve == "leading" ? Planform?.Leading : Planform?.Trailing;
                var handle = rail?.Points.FirstOrDefault(item => item.Id == chosen?.VertexId);
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
        var point = (gesturePoint!.Curve == "leading" ? Planform!.Leading : Planform!.Trailing).Points
            .First(candidate => candidate.Id == gesturePoint.VertexId);
        if (Math.Abs(point.SpanMeters - gestureOrigin.SpanMeters) < 0.00000005 &&
            Math.Abs(point.AftMeters - gestureOrigin.AftMeters) < 0.0000005)
            return Task.FromResult(CancelPointGesture(reason, true));

        Gesture = GestureState.Busy;
        Notify();
        var capture = draft;
        long version = stateVersion;
        pendingCommit = CommitPointGestureAsync(capture, reason, version, cancellation);
        return pendingCommit;
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
            Gesture = GestureState.Idle;
            UpdateEstimates();
            Notify();
        }
        Status = outcome switch
        {
            GestureOutcome.Committed committed => committed.Report,
            GestureOutcome.Refused refused => refused.Copy,
            _ => "Point change cancelled."
        };
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
        Gesture = GestureState.Idle;
        UpdateEstimates();
        GestureOutcome outcome = noChange ? new GestureOutcome.NoChange() :
            new GestureOutcome.Cancelled("Drag cancelled. The point is back where it was.");
        Status = outcome is GestureOutcome.NoChange ? "No point change." : ((GestureOutcome.Cancelled)outcome).Copy;
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
            editKind: "gesture", operationId: gestureOperationId);
    }

    private static double? Percentile95(List<double> values)
    {
        if (values.Count == 0) return null;
        var sorted = values.OrderBy(value => value).ToArray();
        return sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
    }

    public Task<CommitOutcome> ApplyPointCommandAsync(PointCommand command) => RunDirectCommandAsync(() =>
    {
        var result = session.ApplyPointCommand(Guid.NewGuid().ToString("D"), command);
        return new CommitOutcome.Committed(result.AcceptedId,
            $"Point change applied. Max deviation {result.MaxDeviationMeters * 1e3:F2} mm.");
    });

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

    private Task<CommitOutcome> RunDirectCommandAsync(Func<CommitOutcome> action)
    {
        if (Gesture != GestureState.Idle || draft is not null)
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("DSL-DRAFT-OWNED", "Finish the current change first."));
        if (Inspection?.Geometry.Status != GeometryStatus.Certified)
            return Task.FromResult<CommitOutcome>(new CommitOutcome.Refused("DSL-NOT-ASSESSED", "This foil couldn't be checked. Nothing changed."));
        Gesture = GestureState.Busy;
        Notify();
        var completion = CompleteDirectCommandAsync(action, session, stateVersion);
        pendingDirectCommand = completion;
        return completion;
    }

    private async Task<CommitOutcome> CompleteDirectCommandAsync(Func<CommitOutcome> action, AuthoringSession captured, long version)
    {
        try
        {
            var outcome = await Task.Run(action);
            if (!ReferenceEquals(session, captured) || stateVersion != version) return outcome;
            Inspection = session.InspectAccepted();
            UpdateEstimates();
            Status = ((CommitOutcome.Committed)outcome).Report;
            Notify();
            _ = RefreshAcceptedAsync();
            return outcome;
        }
        catch (ContractError error)
        {
            Status = $"{error.Code}: This change wasn't applied. Nothing changed.";
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
    public string Status { get; private set; } = "Open Example or a .foil / .cfdw.json file.";
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

        Adopt(preparedSession);
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
                Status = $"{code}: Refused. Original source retained read-only.";
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
                Status = $"{assessment.Code}: Refused. Original source retained read-only.";
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

        Adopt(preparedSession);
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
        Adopt(next);
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
        Adopt(next);
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

    public void BeginSectionEdit(int assignmentIndex, SectionScope scope, string side, string vertexId,
        ThicknessIntent thickness = ThicknessIntent.KeepCurrent)
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
        var started = session.BeginProfileEdit(Guid.NewGuid().ToString("D"), assignmentIndex, scope, side, vertexId, thickness);
        CancelSampling();
        draft = started;
        draftInputValid = true;
        if ((uint)assignmentIndex < (uint)Inspection.Authored.Assignments.Count)
            interiorEta = Inspection.Authored.Assignments[assignmentIndex].Eta;
        Frame = acceptedFrame = null;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        Status = $"Draft {started.Id} owns station {assignmentIndex} {side} {vertexId}. Sampling accepted geometry at η {interiorEta:G3}.";
        Provenance = "draft — accepted sampling";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void UpdateSectionDraft(double x, double y)
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        draft = session.UpdateProfileDraft(draft.Id, draft.Generation, x, y);
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "draft — accepted geometry shown";
        Status = $"Draft owns station {draft.Assignment} {draft.Rail} {draft.VertexId}. Draft generation {draft.Generation} changed. Preview to assess geometry.";
        Notify();
    }

    public void BeginSectionInsert(int assignmentIndex, SectionScope scope, double x)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileInsert(Guid.NewGuid().ToString("D"), assignmentIndex, scope, x);
        OpenConstructedDraft(started, assignmentIndex, "insert");
    }

    public void BeginSectionDelete(int assignmentIndex, SectionScope scope, int vertexIndex)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileDelete(Guid.NewGuid().ToString("D"), assignmentIndex, scope, vertexIndex);
        OpenConstructedDraft(started, assignmentIndex, "delete");
    }

    public void BeginSectionFair(int assignmentIndex, SectionScope scope, double tolerance, PreserveEnds ends)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileFair(Guid.NewGuid().ToString("D"), assignmentIndex, scope, tolerance, ends);
        OpenConstructedDraft(started, assignmentIndex, "fair");
    }

    public void BeginSectionRebuild(int assignmentIndex, SectionScope scope, int vertexCount, double tolerance, PreserveEnds ends)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileRebuild(Guid.NewGuid().ToString("D"), assignmentIndex, scope, vertexCount, tolerance, ends);
        OpenConstructedDraft(started, assignmentIndex, "rebuild");
    }

    public void BeginSectionImport(int assignmentIndex, byte[] dat)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileImport(Guid.NewGuid().ToString("D"), assignmentIndex, dat);
        OpenConstructedDraft(started, assignmentIndex, "import");
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

    private async Task RefreshAcceptedAsync(CancellationToken cancellation = default)
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
        Status = $"{SectionDraftPrefix()}Sampling accepted geometry at η {eta:G3}…";
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
            Status = $"{SectionDraftPrefix()}Accepted η {eta:G3} slice; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
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
                    Geometry.PointAt(certificate, eta, sample.X, sample.Upper, timeBudget: TimeSpan.FromSeconds(1), cancellationToken: cancellation)));
        var section = Geometry.SectionAt(certificate, interiorEta, .5, TimeSpan.FromSeconds(1), cancellation);
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

    private void Adopt(AuthoringSession next)
    {
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
    }

    private void RequireCertifiedFoil()
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
    }

    private void OpenConstructedDraft(SessionDraft started, int assignmentIndex, string kind)
    {
        CancelSampling();
        draft = started;
        draftInputValid = true;
        if (Inspection is { } inspected && (uint)assignmentIndex < (uint)inspected.Authored.Assignments.Count)
            interiorEta = inspected.Authored.Assignments[assignmentIndex].Eta;
        Frame = acceptedFrame = null;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        Status = $"Draft {started.Id} owns station {assignmentIndex} {kind}.";
        Provenance = "draft — accepted sampling";
        AssessDraftNow();
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
        if (isNotifying) return;
        isNotifying = true;
        try
        {
            while (true)
            {
                if (queuedSelection is not null)
                {
                    var next = queuedSelection;
                    queuedSelection = null;
                    var reconciled = Reconcile(next, CurrentProjection);
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
                    var reconciled = Reconcile(Selection, CurrentProjection);
                    if (!Equals(Selection, reconciled))
                    {
                        Selection = reconciled;
                        SelectionChanged?.Invoke();
                    }
                }

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

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Gesture = GestureState.Idle;
        CancelSampling();
        store.Dispose();
        session.Dispose();
        Selection = new Selection.None();
        Estimates = null;
    }
}
