using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public sealed class PlanCanvas : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(BackgroundBrush));
    public static readonly StyledProperty<IBrush?> FoilBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(FoilBrush));
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(SelectionBrush));
    public static readonly StyledProperty<IBrush?> FocusBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(FocusBrush));
    public static readonly StyledProperty<IBrush?> MuteBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(MuteBrush));
    public static readonly StyledProperty<IBrush?> DangerBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(DangerBrush));
    public static readonly StyledProperty<IBrush?> WarningBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(WarningBrush));
    public static readonly StyledProperty<IBrush?> SoftBrushProperty =
        AvaloniaProperty.Register<PlanCanvas, IBrush?>(nameof(SoftBrush));

    public IBrush? BackgroundBrush { get => GetValue(BackgroundBrushProperty); set => SetValue(BackgroundBrushProperty, value); }
    public IBrush? FoilBrush { get => GetValue(FoilBrushProperty); set => SetValue(FoilBrushProperty, value); }
    public IBrush? SelectionBrush { get => GetValue(SelectionBrushProperty); set => SetValue(SelectionBrushProperty, value); }
    public IBrush? FocusBrush { get => GetValue(FocusBrushProperty); set => SetValue(FocusBrushProperty, value); }
    public IBrush? MuteBrush { get => GetValue(MuteBrushProperty); set => SetValue(MuteBrushProperty, value); }
    public IBrush? DangerBrush { get => GetValue(DangerBrushProperty); set => SetValue(DangerBrushProperty, value); }
    public IBrush? WarningBrush { get => GetValue(WarningBrushProperty); set => SetValue(WarningBrushProperty, value); }
    public IBrush? SoftBrush { get => GetValue(SoftBrushProperty); set => SetValue(SoftBrushProperty, value); }

    private WorkbenchController? controller;
    private readonly List<PointView> targets = [];
    private bool attached;
    private PointRef? focusedPoint;
    private PointView? hoveredPoint;
    private int keyboardIndex = -1;
    private bool advisoryCrossing;
    private Point advisoryPoint;
    private bool renderFailureNotified;
    // A press on empty canvas (§11.3 Pan row, F-2): a drag pans the camera, a click clears the selection.
    private PlanCamera? panOrigin;
    private Point panPress;
    private Point panLast;
    private bool panning;
    private bool panClicks;

    public WorkbenchController? Controller
    {
        get => controller;
        set
        {
            if (attached && controller is not null) controller.Changed -= UpdatePlan;
            controller = value;
            if (attached && controller is not null) controller.Changed += UpdatePlan;
            UpdatePlan();
        }
    }

    public string? LastValueRequest { get; private set; }
    public string? TooltipText { get; private set; }
    public string? ProbeText { get; private set; }
    public string? RenderBanner { get; private set; }
    public Action? RenderGuard { get; set; }
    public event Action<Exception>? RenderFailed;
    public event Action? RenderRecovered;
    public PointRef? FocusedTarget => focusedPoint;
    public sealed record StationChip(int Index, Rect Bounds);
    public IReadOnlyList<StationChip> VisibleStationChips
    {
        get
        {
            var plan = Controller?.Planform;
            if (plan is null || Bounds.Width <= 0 || Bounds.Height <= 0) return [];
            var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
            var result = new List<StationChip>();
            foreach (var (station, index) in plan.Stations.Select((station, index) => (station, index)))
            {
                var probe = CfdWorkbench.Core.Planform.Probe(plan, station.Eta);
                var at = map.ToScreen(station.SpanMeters, probe.TrailingAftMeters);
                var rect = new Rect(at.X - 48, Math.Min(Bounds.Height - 28, at.Y + 12), 96, 22);
                if (result.Any(chip => chip.Bounds.Intersects(rect))) continue;
                result.Add(new StationChip(index, rect));
            }
            return result;
        }
    }
    public IReadOnlyList<string> KeyboardTargets
    {
        get
        {
            var plan = Controller?.Planform;
            return plan is null ? [] : AxisPointLayer.KeyboardTargets(plan);
        }
    }

    public PlanCanvas()
    {
        Focusable = true;
        AttachedToVisualTree += (_, _) =>
        {
            attached = true;
            if (controller is not null) controller.Changed += UpdatePlan;
            UpdatePlan();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            attached = false;
            if (controller is not null) controller.Changed -= UpdatePlan;
        };
        SizeChanged += (_, _) => UpdatePlan();
        PropertyChanged += (_, args) =>
        {
            if (args.Property == BackgroundBrushProperty || args.Property == FoilBrushProperty ||
                args.Property == SelectionBrushProperty || args.Property == FocusBrushProperty ||
                args.Property == MuteBrushProperty || args.Property == DangerBrushProperty ||
                args.Property == WarningBrushProperty || args.Property == SoftBrushProperty)
                InvalidateVisual();
        };
    }

    public Point ScreenPoint(PointView point)
    {
        var plan = Controller?.Planform;
        if (plan is null) return default;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        return map.ToScreen(point.SpanMeters, point.AftMeters);
    }

    public PointView? HitTestPoint(Point position)
    {
        var plan = Controller?.Planform;
        return plan is null ? null : new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera)
            .HitTest(targets, position, Controller.Selection);
    }

    public void HoverAt(Point position)
    {
        var plan = Controller?.Planform;
        if (plan is null) return;
        hoveredPoint = HitTestPoint(position);
        TooltipText = hoveredPoint is { } point
            ? $"{(point.Curve == "leading" ? "Leading" : "Trailing")} edge, point {point.Index + 1} of " +
              $"{(point.Curve == "leading" ? plan.Leading : plan.Trailing).Points.Count}, " +
              $"{point.Role.ToString().ToLowerInvariant()} point, span {point.SpanMeters * 1000:F2} mm, aft {point.AftMeters * 1000:F2} mm"
            : null;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        double eta = Math.Clamp(map.FromScreen(position).Span / plan.HalfSpanMeters, 0, 1);
        var probe = CfdWorkbench.Core.Planform.Probe(plan, eta);
        ProbeText = $"η {probe.Eta:F3} · span {probe.SpanMeters * 1000:F2} mm · {probe.Eta * 100:F1} % half-span · " +
            $"LE {probe.LeadingAftMeters * 1000:F2} mm · TE {probe.TrailingAftMeters * 1000:F2} mm · chord {probe.ChordMeters * 1000:F2} mm";
        InvalidateVisual();
    }

    public void SelectPoint(PointRef point, bool extend, bool toggle)
    {
        if (Controller is null) return;
        var existing = Controller.Selection is Selection.Points points ? points.Items.ToList() : [];
        int index = existing.FindIndex(item => item.Curve == point.Curve && item.VertexId == point.VertexId);
        if (toggle)
        {
            if (index >= 0) existing.RemoveAt(index);
            else existing.Add(point);
        }
        else if (extend)
        {
            if (index < 0) existing.Add(point);
        }
        else existing = [point];
        Controller.Select(existing.Count == 0 ? new Selection.Foil() : new Selection.Points(existing));
        InvalidateVisual();
    }

    public void FocusPoint(PointRef point)
    {
        focusedPoint = point;
        var view = targets.FirstOrDefault(item => item.Curve == point.Curve && item.Id == point.VertexId);
        if (view is not null)
        {
            var position = ScreenPoint(view);
            double desiredX = Math.Clamp(position.X, 24, Math.Max(24, Bounds.Width - 24));
            double desiredY = Math.Clamp(position.Y, 70, Math.Max(70, Bounds.Height - 48));
            if (desiredX != position.X || desiredY != position.Y)
                PanBy(desiredX - position.X, desiredY - position.Y);
        }
        Focus();
        InvalidateVisual();
    }

    public void SelectFocused(bool toggle)
    {
        if (focusedPoint is { } point) SelectPoint(point, extend: false, toggle);
    }

    public bool FocusNext(bool reverse = false)
    {
        var plan = Controller?.Planform;
        if (plan is null) return false;
        var targets = plan.Leading.Points.Concat(plan.Trailing.Points).ToArray();
        int count = targets.Length + plan.Stations.Count;
        int next = keyboardIndex + (reverse ? -1 : 1);
        if (next < 0 || next >= count) return false;
        keyboardIndex = next;
        if (next < targets.Length) FocusPoint(new PointRef(targets[next].Curve, targets[next].Id));
        else { focusedPoint = null; InvalidateVisual(); }
        return true;
    }

    public void ToggleComb()
    {
        if (Controller is null) return;
        Controller.CombVisible = !Controller.CombVisible;
        InvalidateVisual();
    }

    public void ZoomAt(double factor, Point pivot)
    {
        if (Controller is null || factor <= 0 || !double.IsFinite(factor)) return;
        var camera = Controller.PlanCamera;
        double nextScale = Math.Clamp(camera.PixelsPerMeter * factor, 100, 10000);
        double actual = nextScale / camera.PixelsPerMeter;
        Controller.PlanCamera = camera with
        {
            PixelsPerMeter = nextScale,
            PanSpanPixels = pivot.X - Bounds.Width / 2 - (pivot.X - Bounds.Width / 2 - camera.PanSpanPixels) * actual,
            PanAftPixels = pivot.Y - AxisPointLayer.FitTop - (pivot.Y - AxisPointLayer.FitTop - camera.PanAftPixels) * actual
        };
        UpdatePlan();
    }

    public void PanBy(double spanPixels, double aftPixels)
    {
        if (Controller is null) return;
        Controller.PlanCamera = Controller.PlanCamera with
        {
            PanSpanPixels = Controller.PlanCamera.PanSpanPixels + spanPixels,
            PanAftPixels = Controller.PlanCamera.PanAftPixels + aftPixels
        };
        UpdatePlan();
    }

    public void Fit()
    {
        if (Controller is null) return;
        Controller.PlanCamera = new PlanCamera();
        UpdatePlan();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Controller?.Planform is null) return;
        var position = e.GetPosition(this);
        var hit = HitTestPoint(position);
        if (hit is null)
        {
            var chip = VisibleStationChips.FirstOrDefault(item => item.Bounds.Contains(position));
            if (chip is not null)
            {
                Controller.Select(new Selection.Station(chip.Index, Controller.Planform.Stations[chip.Index].Eta));
                e.Handled = true;
                return;
            }
            var pressed = e.GetCurrentPoint(this).Properties;
            if (pressed.IsLeftButtonPressed || pressed.IsMiddleButtonPressed)
            {
                panOrigin = Controller.PlanCamera;
                panPress = panLast = position;
                panning = false;
                panClicks = pressed.IsLeftButtonPressed;
                e.Pointer.Capture(this);
                e.Handled = true;
            }
            return;
        }
        var reference = new PointRef(hit.Curve, hit.Id);
        var buttons = e.GetCurrentPoint(this).Properties;
        // §11.3 / spec B7: on macOS Control-click is a secondary click and ⌘-click toggles; on Windows Ctrl-click toggles.
        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (buttons.IsRightButtonPressed || OperatingSystem.IsMacOS() && control && buttons.IsLeftButtonPressed)
        {
            OpenPointMenu(reference);
            e.Handled = true;
            return;
        }
        bool extend = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool toggle = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || !OperatingSystem.IsMacOS() && control;
        SelectPoint(reference, extend, toggle);
        FocusPoint(reference);
        if (e.ClickCount >= 2) RequestValue(hit);
        else if (!extend && !toggle && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Controller.BeginGesture(reference, GestureInput.Pointer);
            e.Pointer.Capture(this);
        }
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        if (panOrigin is not null)
        {
            if (!panning && Point.Distance(position, panPress) <= 3) return;
            panning = true;
            PanBy(position.X - panLast.X, position.Y - panLast.Y);
            panLast = position;
            e.Handled = true;
            return;
        }
        HoverAt(position);
        if (Controller?.Gesture is not (GestureState.Pressed or GestureState.Dragging)) return;
        var plan = Controller.Planform;
        if (plan is null) return;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller.PlanCamera);
        var target = map.FromScreen(position);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && focusedPoint is { } reference)
        {
            var origin = targets.FirstOrDefault(item => item.Curve == reference.Curve && item.Id == reference.VertexId);
            if (origin is not null)
            {
                if (Math.Abs(target.Span - origin.SpanMeters) > Math.Abs(target.Ordinate - origin.AftMeters))
                    target.Ordinate = origin.AftMeters;
                else target.Span = origin.SpanMeters;
            }
        }
        Controller.UpdateGesture(target.Span, target.Ordinate);
        if (focusedPoint is { } selected)
        {
            var origin = targets.FirstOrDefault(item => item.Curve == selected.Curve && item.Id == selected.VertexId);
            if (origin is not null)
                ProbeText += $" · Δ span {(target.Span - origin.SpanMeters) * 1000:+0.00;-0.00;0.00} mm" +
                    $" · Δ aft {(target.Ordinate - origin.AftMeters) * 1000:+0.00;-0.00;0.00} mm";
        }
        InvalidateVisual();
        e.Handled = true;
    }

    /// <summary>
    /// Selects <paramref name="reference"/> and opens the point menu (D-3). Each row runs the shell command of the same
    /// name, with the shell's enablement (<see cref="Shell.ShellHost.CanRun"/>).
    /// </summary>
    public void OpenPointMenu(PointRef reference)
    {
        if (this.FindAncestorOfType<Shell.ShellHost>() is not { } host) return;
        SelectPoint(reference, extend: false, toggle: false);
        FocusPoint(reference);
        MenuItem Row(string header, string id)
        {
            var item = new MenuItem { Header = header, IsEnabled = host.CanRun(id) };
            item.Click += (_, _) => _ = host.RunCommand(id);
            return item;
        }
        MenuItem[] tangents = [Row("Smooth", "point.tangent-smooth"), Row("Symmetric", "point.tangent-symmetric"), Row("Corner", "point.tangent-corner")];
        var tangent = new MenuItem { Header = "Tangent", ItemsSource = tangents, IsEnabled = tangents.Any(item => item.IsEnabled) };
        var menu = new ContextMenu
        {
            ItemsSource = new Control[]
            {
                Row("Make Anchor Point", "point.make-anchor"), Row("Make Control Point", "point.make-control"), tangent,
                new Separator(), Row("Fit", "view.fit")
            }
        };
        // Attached only while shown, so a right-click on empty canvas never opens a stale point menu.
        menu.Closed += (_, _) => { if (ReferenceEquals(ContextMenu, menu)) ContextMenu = null; };
        ContextMenu = menu;
        menu.Open(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // The point menu is opened on press; keep Avalonia's right-release context request from opening it twice.
        if (e.InitialPressMouseButton == MouseButton.Right) e.Handled = true;
        base.OnPointerReleased(e);
        if (panOrigin is not null)
        {
            if (!panning && panClicks && Controller is not null)
            {
                Controller.Select(new Selection.Foil());
                TooltipText = null;
            }
            EndPan(cancel: false);
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }
        e.Pointer.Capture(null);
        if (Controller?.Gesture is GestureState.Pressed or GestureState.Dragging)
        {
            _ = Controller.EndGestureAsync(GestureEnd.Release);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndPan(cancel: false);
    }

    // A pan is a view change, never an undo step. Escape restores the camera the press started from.
    private void EndPan(bool cancel)
    {
        if (panOrigin is { } origin && cancel && Controller is not null)
        {
            Controller.PlanCamera = origin;
            UpdatePlan();
        }
        panOrigin = null;
        panning = false;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomAt(Math.Pow(1.1, e.Delta.Y), e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Controller?.Planform is null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool option = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (e.Key == Key.Tab) { e.Handled = FocusNext(shift); return; }
        if (e.Key == Key.Space) { SelectFocused(shift); e.Handled = true; return; }
        if ((e.Key == Key.Apps || e.Key == Key.F10 && shift) && focusedPoint is { } menuTarget)
        {
            OpenPointMenu(menuTarget);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape && panOrigin is not null)
        {
            EndPan(cancel: true);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape)
        {
            if (TooltipText is not null) TooltipText = null;
            else if (Controller.Gesture != GestureState.Idle) _ = Controller.EndGestureAsync(GestureEnd.Escape);
            else if (focusedPoint is { } handleRef &&
                     targets.FirstOrDefault(item => item.Curve == handleRef.Curve && item.Id == handleRef.VertexId)
                         is { AnchorId: { } anchorId })
                FocusPoint(new PointRef(handleRef.Curve, anchorId));
            else Controller.Select(new Selection.Foil());
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Return && focusedPoint is { } reference)
        {
            var point = targets.FirstOrDefault(item => item.Curve == reference.Curve && item.Id == reference.VertexId);
            if (point is not null) RequestValue(point);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.C && !command && !option) { ToggleComb(); e.Handled = true; return; }
        if (command && e.Key == Key.D0) { Fit(); e.Handled = true; return; }
        if (command && e.Key is Key.OemPlus or Key.Add or Key.OemMinus or Key.Subtract)
        {
            ZoomAt(e.Key is Key.OemPlus or Key.Add ? 1.2 : 1 / 1.2, new Point(Bounds.Width / 2, Bounds.Height / 2));
            e.Handled = true;
            return;
        }
        var (span, aft) = AxisPointLayer.KeyboardDirection(e.Key);
        if (span == 0 && aft == 0) return;
        if (option)
        {
            PanBy(span * Bounds.Width * .1, aft * Bounds.Height * .1);
            e.Handled = true;
            return;
        }
        if (focusedPoint is not { } focus) return;
        if (Controller.Gesture == GestureState.Idle && !Controller.BeginGesture(focus, GestureInput.Keyboard))
        {
            AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
            e.Handled = true;
            return;
        }
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Off);
        Controller.Nudge(span, aft, command ? NudgeModifier.Command : shift ? NudgeModifier.Shift : NudgeModifier.Plain);
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (Controller?.Gesture == GestureState.Nudging && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ = Controller.EndGestureAsync(GestureEnd.KeyUp);
            e.Handled = true;
        }
    }

    private void UpdatePlan()
    {
        if (!attached) return;
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdatePlan);
            return;
        }
        var plan = Controller?.Planform;
        if (plan is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            targets.Clear();
            advisoryCrossing = false;
            InvalidateVisual();
            return;
        }
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        targets.Clear();
        foreach (var curve in new[] { plan.Leading, plan.Trailing })
        foreach (var point in curve.Points)
        {
            targets.Add(point);
        }
        // D-2: the marker mirrors the controller's preview of the release check, at the offending hull point.
        advisoryCrossing = Controller.GestureCrossing is not null;
        if (Controller.GestureCrossing is { } crossing) advisoryPoint = map.ToScreen(crossing.SpanMeters, crossing.AftMeters);
        InvalidateVisual();
    }

    private void RequestValue(PointView point) => LastValueRequest = $"{point.Curve}:{point.Id}";

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush ?? Brushes.Transparent, null, new Rect(Bounds.Size));
        try
        {
            RenderGuard?.Invoke();
            RenderScene(context);
        }
        catch (Exception error)
        {
            RenderBanner = "Plan couldn't render. Try again.";
            CfdWorkbench.Desktop.Shell.ShellEvents.Record("shell.pane.render", "error", 0,
                System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
                code: "shell.pane.render", pane: "plan", exceptionType: error.GetType().Name);
            if (!renderFailureNotified)
            {
                renderFailureNotified = true;
                RenderFailed?.Invoke(error);
            }
            DrawLabel(context, RenderBanner, new Point(12, 12));
        }
    }

    public void RetryRender()
    {
        RenderBanner = null;
        renderFailureNotified = false;
        RenderRecovered?.Invoke();
        InvalidateVisual();
    }

    private void RenderScene(DrawingContext context)
    {
        var plan = Controller?.Planform;
        if (plan is null) return;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        bool certified = Controller.Inspection?.Geometry.Status == GeometryStatus.Certified;
        RenderBanner = certified ? null : "Foil not certified. Point editing unavailable.";
        if (certified)
            map.Draw(context, FoilBrush ?? Brushes.White, SelectionBrush ?? Brushes.White,
                BackgroundBrush ?? Brushes.Transparent, MuteBrush ?? Brushes.White, Controller.Selection);
        else
        {
            using (context.PushOpacity(.35))
                map.Draw(context, FoilBrush ?? Brushes.White, SelectionBrush ?? Brushes.White,
                    BackgroundBrush ?? Brushes.Transparent, MuteBrush ?? Brushes.White, Controller.Selection);
            DrawLabel(context, RenderBanner!, new Point(12, 12));
        }
        if (Controller.CombVisible && Controller.Selection is Selection.Points points && points.Items.Count > 0)
        {
            var rail = points.Items[0].Curve == "leading" ? plan.Leading : plan.Trailing;
            var combPen = new Pen(WarningBrush ?? Brushes.White, .9);
            foreach (var tooth in CfdWorkbench.Core.Planform.Comb(rail))
            {
                var start = map.ToScreen(tooth.SpanMeters, tooth.AftMeters);
                context.DrawLine(combPen, start,
                    start + new Vector(tooth.NormalSpan, tooth.NormalAft) *
                    Math.Clamp(Math.Abs(tooth.Curvature) * 100, 6, 24));
            }
        }
        if (advisoryCrossing)
        {
            var marker = new Pen(DangerBrush ?? Brushes.White, 4, new DashStyle([6, 3], 0));
            context.DrawLine(marker, advisoryPoint + new Vector(-12, -12), advisoryPoint + new Vector(12, 12));
            DrawLabel(context, "Edges would cross", advisoryPoint + new Vector(16, 8));
        }
        var stationPen = new Pen(SelectionBrush ?? Brushes.White, 1,
            new DashStyle([3, 3], 0));
        foreach (var chip in VisibleStationChips)
        {
            var station = plan.Stations[chip.Index];
            var stationReading = CfdWorkbench.Core.Planform.Probe(plan, station.Eta);
            context.DrawLine(stationPen, map.ToScreen(station.SpanMeters, stationReading.LeadingAftMeters),
                map.ToScreen(station.SpanMeters, stationReading.TrailingAftMeters));
            context.DrawRectangle(SoftBrush ?? BackgroundBrush, new Pen(MuteBrush ?? Brushes.White, 1), chip.Bounds);
            DrawLabel(context, station.ProfileName, chip.Bounds.Position + new Vector(5, 3));
        }
        double barLength = map.ToScreen(.05, 0).X - map.ToScreen(0, 0).X;
        if (barLength > 0 && barLength < Bounds.Width - 32)
        {
            double y = Bounds.Height - 28;
            var barPen = new Pen(FoilBrush ?? Brushes.White, 1);
            context.DrawLine(barPen, new Point(16, y), new Point(16 + barLength, y));
            context.DrawLine(barPen, new Point(16, y - 4), new Point(16, y + 4));
            context.DrawLine(barPen, new Point(16 + barLength, y - 4), new Point(16 + barLength, y + 4));
            DrawLabel(context, "50 mm · Plan · top", new Point(16, y - 20));
        }
        if (hoveredPoint is { } hovered)
            context.DrawEllipse(null, new Pen(MuteBrush ?? Brushes.White, 1.5), ScreenPoint(hovered), 10, 10);
        if (focusedPoint is { } focus)
        {
            var target = targets.FirstOrDefault(point => point.Curve == focus.Curve && point.Id == focus.VertexId);
            if (target is not null)
                context.DrawEllipse(null, new Pen(FocusBrush ?? Brushes.White, 3), ScreenPoint(target), 13, 13);
        }
        if (ProbeText is { } probe)
        {
            double left = Math.Max(8, Bounds.Width - 428);
            context.DrawRectangle(SoftBrush ?? BackgroundBrush, null, new Rect(left, 8, 420, 48));
            var pieces = probe.Split(" · ");
            DrawLabel(context, string.Join(" · ", pieces.Take(3)), new Point(left + 8, 12));
            DrawLabel(context, string.Join(" · ", pieces.Skip(3)), new Point(left + 8, 30));
        }
        if (TooltipText is { } tooltip && hoveredPoint is { } hoveredTarget)
        {
            var centre = ScreenPoint(hoveredTarget);
            double left = Math.Clamp(centre.X + 14, 4, Math.Max(4, Bounds.Width - 320));
            double top = Math.Clamp(centre.Y + 12, 60, Math.Max(60, Bounds.Height - 30));
            context.DrawRectangle(SoftBrush ?? BackgroundBrush, null, new Rect(left, top, 312, 24));
            DrawLabel(context, tooltip, new Point(left + 5, top + 3));
        }
    }

    private void DrawLabel(DrawingContext context, string copy, Point position)
    {
        var formatted = new FormattedText(copy, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter"), 11, MuteBrush ?? FoilBrush ?? Brushes.White)
        { MaxTextWidth = 408, MaxTextHeight = 20, Trimming = TextTrimming.CharacterEllipsis };
        context.DrawText(formatted, position);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PlanCanvasPeer(this);

    private sealed class PlanCanvasPeer(PlanCanvas owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override List<AutomationPeer>? GetChildrenCore() =>
            owner.Controller?.Planform is { } plan
                ? new AxisPointLayer(plan, owner.Bounds.Size, owner.Controller.PlanCamera).AutomationPeers(owner, owner.targets)
                : [];
    }

    // Drawing, picking, automation and key directions share one axis mapping.
    // The elevation view can supply a different projection for these same points.
    private sealed record AxisMapping(
        Func<double, double, Point> Project,
        Func<Point, (double Span, double Ordinate)> Unproject);

    private sealed class AxisPointLayer
    {
        private readonly PlanformView plan;
        private readonly AxisMapping axes;
        private readonly Size size;

        public AxisPointLayer(PlanformView plan, Size size, PlanCamera camera)
            : this(plan, PlanAxes(plan, size, camera), size) { }

        public AxisPointLayer(PlanformView plan, AxisMapping axes, Size size)
        {
            this.plan = plan;
            this.axes = axes;
            this.size = size;
        }

        // F-3: the fitted view keeps the planform, both halves, every point glyph and every station chip clear of the
        // Tracing probe box (top band, 8 + 48 px) and the scale bar (bottom band, 50 px), with a margin.
        private const double GlyphMargin = 8;
        private const double ChipHalfWidth = 48;
        private const double ChipDrop = 34;
        private const double ScaleBarBand = 50;
        public const double FitTop = 8 + 48 + 12 + GlyphMargin;

        private static AxisMapping PlanAxes(PlanformView plan, Size size, PlanCamera camera)
        {
            var points = plan.Leading.Points.Concat(plan.Trailing.Points).ToArray();
            double halfWidth = Math.Max(plan.HalfSpanMeters, points.Max(item => Math.Abs(item.SpanMeters)));
            double minAft = Math.Min(plan.Leading.Samples.Min(item => item.AftMeters), points.Min(item => item.AftMeters));
            double maxAft = Math.Max(plan.Trailing.Samples.Max(item => item.AftMeters), points.Max(item => item.AftMeters));
            double usableWidth = size.Width - 2 * (ChipHalfWidth + GlyphMargin);
            double usableHeight = size.Height - FitTop - ChipDrop - ScaleBarBand - GlyphMargin;
            double scale = Math.Max(1, Math.Min(usableWidth / (2 * halfWidth), usableHeight / Math.Max(0.01, maxAft - minAft)))
                * camera.PixelsPerMeter / 1000;
            return new AxisMapping(
                (span, aft) => new Point(size.Width / 2 + span * scale + camera.PanSpanPixels,
                    FitTop + (aft - minAft) * scale + camera.PanAftPixels),
                position => ((position.X - size.Width / 2 - camera.PanSpanPixels) / scale,
                    (position.Y - FitTop - camera.PanAftPixels) / scale + minAft));
        }

        public Point ToScreen(double span, double aft) => axes.Project(span, aft);

        public (double Span, double Ordinate) FromScreen(Point position) => axes.Unproject(position);

        public static IReadOnlyList<string> KeyboardTargets(PlanformView view) =>
            view.Leading.Points.Select(point => point.Id)
                .Concat(view.Trailing.Points.Select(point => point.Id))
                .Concat(Enumerable.Range(0, view.Stations.Count).Select(index => $"station:{index}"))
                .ToArray();

        public static (int Span, int Ordinate) KeyboardDirection(Key key) => key switch
        {
            Key.Right => (1, 0), Key.Left => (-1, 0),
            Key.Down => (0, 1), Key.Up => (0, -1), _ => (0, 0)
        };

        public PointView? HitTest(IEnumerable<PointView> targets, Point position, Selection selection)
        {
            PointView? nearest = null;
            double distance = 14 * 14;
            foreach (var candidate in targets)
            {
                var centre = ToScreen(candidate.SpanMeters, candidate.AftMeters);
                double squared = Math.Pow(position.X - centre.X, 2) + Math.Pow(position.Y - centre.Y, 2);
                if (squared > distance) continue;
                if (squared == distance && selection is Selection.Points selected &&
                    !selected.Items.Any(item => item.Curve == candidate.Curve && item.VertexId == candidate.Id)) continue;
                nearest = candidate;
                distance = squared;
            }
            return nearest;
        }

        public List<AutomationPeer> AutomationPeers(PlanCanvas owner, IEnumerable<PointView> targets) =>
            targets.Select(point => (AutomationPeer)new PlanPointPeer(owner, point)).ToList();

        private sealed class PlanPointPeer(PlanCanvas canvas, PointView point)
            : ControlAutomationPeer(canvas), IInvokeProvider
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
            protected override string GetNameCore()
            {
                var plan = canvas.Controller?.Planform;
                var curve = point.Curve == "leading" ? plan?.Leading : plan?.Trailing;
                string role = point.Role.ToString().ToLowerInvariant();
                if (point.AnchorId is { } anchorId && curve?.Points.FirstOrDefault(item => item.Id == anchorId) is { } anchor)
                {
                    double span = point.SpanMeters - anchor.SpanMeters;
                    double aft = point.AftMeters - anchor.AftMeters;
                    double angle = Math.Atan2(aft, span) * 180 / Math.PI;
                    double length = Math.Sqrt(span * span + aft * aft) * 1000;
                    role = $"{(point.Index < anchor.Index ? "in" : "out")} handle, angle {angle:F2}°, length {length:F2} mm";
                }
                return $"{(point.Curve == "leading" ? "Leading" : "Trailing")} edge, point {point.Index + 1} of {curve?.Points.Count ?? 0}, " +
                    $"{role}, span {point.SpanMeters * 1000:F2} mm, aft {point.AftMeters * 1000:F2} mm";
            }
            protected override Rect GetBoundingRectangleCore()
            {
                if (canvas.GetVisualRoot() is not Visual root) return default;
                var screen = canvas.ScreenPoint(point);
                var topLeft = canvas.TranslatePoint(new Point(screen.X - 14, screen.Y - 14), root);
                return topLeft is { } translated ? new Rect(translated, new Size(28, 28)) : default;
            }
            public void Invoke() => canvas.RequestValue(point);
        }

        public void Draw(DrawingContext context, IBrush foil, IBrush station, IBrush background, IBrush mute,
            Selection selection)
        {
            var railPen = new Pen(foil, 2);
            double centreX = ToScreen(0, plan.Leading.Samples[0].AftMeters).X;
            context.DrawLine(new Pen(mute, 1), new Point(centreX, 0), new Point(centreX, size.Height));
            foreach (double side in new[] { -1d, 1d })
            {
                var fill = new StreamGeometry();
                using (var path = fill.Open())
                {
                    var first = plan.Leading.Samples[0];
                    path.BeginFigure(ToScreen(first.SpanMeters * side, first.AftMeters), true);
                    foreach (var sample in plan.Leading.Samples.Skip(1))
                        path.LineTo(ToScreen(sample.SpanMeters * side, sample.AftMeters));
                    foreach (var sample in plan.Trailing.Samples.Reverse())
                        path.LineTo(ToScreen(sample.SpanMeters * side, sample.AftMeters));
                    path.EndFigure(true);
                }
                using (context.PushOpacity(.25)) context.DrawGeometry(foil, null, fill);
            }
            foreach (var rail in new[] { plan.Leading, plan.Trailing })
            {
                foreach (double side in new[] { -1d, 1d })
                {
                    for (int index = 1; index < rail.Samples.Count; index++)
                    {
                        var before = rail.Samples[index - 1];
                        var after = rail.Samples[index];
                        context.DrawLine(railPen, ToScreen(before.SpanMeters * side, before.AftMeters),
                            ToScreen(after.SpanMeters * side, after.AftMeters));
                    }
                }
                var polygon = new Pen(mute, 1, new DashStyle([3, 3], 0));
                for (int index = 1; index < rail.Points.Count; index++)
                {
                    var before = rail.Points[index - 1];
                    var after = rail.Points[index];
                    context.DrawLine(polygon, ToScreen(before.SpanMeters, before.AftMeters),
                        ToScreen(after.SpanMeters, after.AftMeters));
                }
                foreach (var point in rail.Points)
                {
                    var centre = ToScreen(point.SpanMeters, point.AftMeters);
                    bool selected = selection is Selection.Points picked &&
                        picked.Items.Any(item => item.Curve == point.Curve && item.VertexId == point.Id);
                    if (point.Role == PointRole.Control)
                    {
                        context.DrawEllipse(selected ? background : foil, selected ? new Pen(station, 2) : null,
                            centre, 5.5, 5.5);
                        if (selected) context.DrawEllipse(station, null, centre, 2, 2);
                    }
                    else if (point.Role is PointRole.RootEnd or PointRole.TipEnd)
                    {
                        var diamond = new StreamGeometry();
                        using (var path = diamond.Open())
                        {
                            path.BeginFigure(new Point(centre.X, centre.Y - 7), selected);
                            path.LineTo(new Point(centre.X + 7, centre.Y));
                            path.LineTo(new Point(centre.X, centre.Y + 7));
                            path.LineTo(new Point(centre.X - 7, centre.Y));
                            path.EndFigure(true);
                        }
                        context.DrawGeometry(selected ? station : null, new Pen(selected ? station : foil, 1.5), diamond);
                    }
                    else if (point.Role == PointRole.Anchor)
                        context.DrawRectangle(selected ? station : background, new Pen(selected ? station : foil, 1.5),
                            new Rect(centre.X - 6, centre.Y - 6, 12, 12));
                    else
                        context.DrawEllipse(selected ? station : background, new Pen(selected ? station : foil, 1),
                            centre, 4.5, 4.5);
                }
            }
        }
    }
}
