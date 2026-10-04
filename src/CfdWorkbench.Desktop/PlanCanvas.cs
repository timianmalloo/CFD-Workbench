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
using CfdWorkbench.Persistence;
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
            if (attached && controller is not null) Unsubscribe(controller);
            controller = value;
            if (attached && controller is not null) Subscribe(controller);
            UpdatePlan();
        }
    }

    // A camera write redraws only this canvas; Changed (document, selection, status) redraws it with the panes.
    private void Subscribe(WorkbenchController source)
    {
        source.Changed += UpdatePlan;
        source.CameraChanged += OnCameraChanged;
    }

    private void Unsubscribe(WorkbenchController source)
    {
        source.Changed -= UpdatePlan;
        source.CameraChanged -= OnCameraChanged;
    }

    private void OnCameraChanged(SingleView view)
    {
        if (view == SingleView.Plan) UpdatePlan();
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
            var map = Layer(plan);
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
            return plan is null ? [] : PlanKeyboardTargets(plan);
        }
    }

    public PlanCanvas()
    {
        Focusable = true;
        AttachedToVisualTree += (_, _) =>
        {
            attached = true;
            if (controller is not null) Subscribe(controller);
            UpdatePlan();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            attached = false;
            if (controller is not null) Unsubscribe(controller);
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

    private RebuildPreview? rebuildPreview;
    public RebuildPreview? RebuildPreview
    {
        get => rebuildPreview;
        set { rebuildPreview = value; InvalidateVisual(); }
    }

    public Point ScreenPoint(PointView point)
    {
        var plan = Controller?.Planform;
        if (plan is null) return default;
        var map = Layer(plan);
        return map.ToScreen(point.SpanMeters, point.Ordinate);
    }

    public Point OutlineScreenPoint(PlanSample sample, bool port = false) =>
        Controller?.Planform is { } plan ? Layer(plan).ToScreen(sample.SpanMeters * (port ? -1 : 1), sample.Ordinate) : default;

    public (string Curve, double Eta, bool Port)? HitTestOutline(Point position)
    {
        if (Controller?.Planform is not { } plan) return null;
        var layer = Layer(plan);
        (string Curve, double Eta, bool Port, double Distance)? nearest = null;
        foreach (var curve in new[] { plan.Leading, plan.Trailing })
            foreach (bool port in new[] { false, true })
            {
                if (layer.HitTestCurve(curve.Samples, position, port ? -1 : 1) is not { } hit) continue;
                if (nearest is { Distance: var old } && hit.Distance >= old) continue;
                nearest = (curve.Curve, hit.Span / plan.HalfSpanMeters, port, hit.Distance);
            }
        return nearest is { } found ? (found.Curve, found.Eta, found.Port) : null;
    }

    public PointView? HitTestPoint(Point position)
    {
        var plan = Controller?.Planform;
        return plan is null ? null : Layer(plan)
            .HitTest(targets, position, Controller!.Selection);
    }

    public void HoverAt(Point position)
    {
        var plan = Controller?.Planform;
        if (plan is null) return;
        hoveredPoint = HitTestPoint(position);
        TooltipText = hoveredPoint is { } point
            ? $"{(point.Curve == "leading" ? "Leading" : "Trailing")} edge, point {point.Index + 1} of " +
              $"{(point.Curve == "leading" ? plan.Leading : plan.Trailing).Points.Count}, " +
              $"{point.Role.ToString().ToLowerInvariant()} point, from root {point.SpanMeters * 1000:F2} mm, aft {point.Ordinate * 1000:F2} mm"
            : null;
        var map = Layer(plan);
        double eta = Math.Clamp(map.FromScreen(position).Span / plan.HalfSpanMeters, 0, 1);
        var probe = CfdWorkbench.Core.Planform.Probe(plan, eta);
        ProbeText = $"η {probe.Eta:F3} · from root {probe.SpanMeters * 1000:F2} mm · {probe.Eta * 100:F1} % half-span · " +
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
        // NS-1: Tab moves on from the point that has focus, however it got it (a click, Return from a field, Escape).
        if (Controller?.Planform is { } plan)
        {
            int index = plan.Leading.Points.Concat(plan.Trailing.Points).ToList()
                .FindIndex(item => item.Curve == point.Curve && item.Id == point.VertexId);
            if (index >= 0) keyboardIndex = index;
        }
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
            PanAftPixels = pivot.Y - FitTop - (pivot.Y - FitTop - camera.PanAftPixels) * actual
        };
    }

    public void PanBy(double spanPixels, double aftPixels)
    {
        if (Controller is null) return;
        Controller.PlanCamera = Controller.PlanCamera with
        {
            PanSpanPixels = Controller.PlanCamera.PanSpanPixels + spanPixels,
            PanAftPixels = Controller.PlanCamera.PanAftPixels + aftPixels
        };
    }

    public void Fit()
    {
        if (Controller is null) return;
        Controller.PlanCamera = new PlanCamera();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Controller?.Planform is null) return;
        var position = e.GetPosition(this);
        var hit = HitTestPoint(position);
        if (hit is null)
        {
            var outline = HitTestOutline(position);
            var emptyButtons = e.GetCurrentPoint(this).Properties;
            if (outline is { } target && e.ClickCount >= 2 && emptyButtons.IsLeftButtonPressed)
            {
                if (target.Port) Controller.ReportPointWarning("Add points on the starboard half, where the points are.");
                else _ = AddAtAsync(target.Curve, target.Eta);
                e.Handled = true;
                return;
            }
            if (outline is { Port: false } context && emptyButtons.IsRightButtonPressed)
            {
                OpenOutlineMenu(context.Curve, context.Eta);
                e.Handled = true;
                return;
            }
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

    private async Task AddAtAsync(string curve, double eta)
    {
        if (Controller is null) return;
        var outcome = await Controller.ApplyPointCommandAsync(new PointCommand.AddPoint(curve, eta));
        if (attached && outcome is CommitOutcome.Committed && Controller.Selection is Selection.Points { Items: [var point] })
            FocusPoint(point);
    }

    public void OpenOutlineMenu(string curve, double eta)
    {
        if (this.FindAncestorOfType<Shell.ShellHost>() is not { } host) return;
        var add = new MenuItem { Header = "Add Point Here", IsEnabled = Controller?.CurveFor(curve)?.Points.Count < Controller?.CurveFor(curve)?.Ceiling };
        add.Click += (_, _) => _ = AddAtAsync(curve, eta);
        var rebuild = new MenuItem { Header = $"Rebuild {PropertiesView.Curves[curve].Name}…", IsEnabled = Controller?.Inspection is not null };
        rebuild.Click += (_, _) => this.FindAncestorOfType<ModelArea>()?.BeginRebuild(curve, this);
        var fit = new MenuItem { Header = "Fit" };
        fit.Click += (_, _) => _ = host.RunCommand("view.fit");
        var menu = new ContextMenu { ItemsSource = new Control[] { add, rebuild, new Separator(), fit } };
        menu.Closed += (_, _) => { if (ReferenceEquals(ContextMenu, menu)) ContextMenu = null; };
        ContextMenu = menu;
        menu.Open(this);
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
        var map = Layer(plan);
        var target = map.FromScreen(position);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && focusedPoint is { } reference)
        {
            var origin = targets.FirstOrDefault(item => item.Curve == reference.Curve && item.Id == reference.VertexId);
            if (origin is not null)
            {
                if (Math.Abs(target.Span - origin.SpanMeters) > Math.Abs(target.Ordinate - origin.Ordinate))
                    target.Ordinate = origin.Ordinate;
                else target.Span = origin.SpanMeters;
            }
        }
        Controller.UpdateGesture(target.Span, target.Ordinate);
        if (focusedPoint is { } selected)
        {
            var origin = targets.FirstOrDefault(item => item.Curve == selected.Curve && item.Id == selected.VertexId);
            if (origin is not null)
                ProbeText += $" · Δ from root {(target.Span - origin.SpanMeters) * 1000:+0.00;-0.00;0.00} mm" +
                    $" · Δ aft {(target.Ordinate - origin.Ordinate) * 1000:+0.00;-0.00;0.00} mm";
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
                new Separator(), Row("Remove Point", "point.remove"),
                Row($"Rebuild {PropertiesView.Curves[reference.Curve].Name}…", "point.rebuild"),
                new Separator(), Row("Fit", "view.fit")
            }
        };
        if (!host.CanRun("point.remove") && host.PointCommandReason("point.remove") is { } reason)
            menu.ItemsSource = ((IEnumerable<Control>)menu.ItemsSource!).Take(5)
                .Concat([new TextBlock { Text = reason, TextWrapping = TextWrapping.Wrap, MaxWidth = 220 }])
                .Concat(((IEnumerable<Control>)menu.ItemsSource!).Skip(5)).ToArray();
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
        if (panOrigin is { } origin && cancel && Controller is not null) Controller.PlanCamera = origin;
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

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        // DR-NAV-1: Tab into the Plan focuses the selected point, or the first target when none is selected; ] and [ walk on from there.
        if (e.NavigationMethod != NavigationMethod.Tab || Controller?.Planform is not { } plan) return;
        PointRef? target = Controller.Selection is Selection.Points { Items: [var selected, ..] }
            ? selected
            : plan.Leading.Points.Concat(plan.Trailing.Points).Select(point => new PointRef(point.Curve, point.Id)).Cast<PointRef?>().FirstOrDefault();
        if (target is { } start) FocusPoint(start);
        else { focusedPoint = null; keyboardIndex = -1; InvalidateVisual(); }
    }

    /// <summary>DR-NAV-1: where Tab from a selected point goes; the shell points it at the Properties pane's first value.</summary>
    public Func<bool>? TabOut { get; set; }

    private bool TabToProperties() => Controller?.Selection is Selection.Points && TabOut?.Invoke() == true;

    /// <summary>DR-NAV-1: Shift+Tab from the Properties pane's first value returns here, to the selected point.</summary>
    public bool FocusSelectedPoint()
    {
        if (Controller?.Selection is not Selection.Points { Items: [var selected, ..] }) return false;
        FocusPoint(selected);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Controller?.Planform is null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool option = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (e.Key == Key.Tab) { e.Handled = !shift && TabToProperties(); return; }
        if (e.Key is Key.OemCloseBrackets or Key.OemOpenBrackets && !command && !option)
        {
            e.Handled = FocusNext(reverse: e.Key == Key.OemOpenBrackets);
            return;
        }
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
        if (e.Key is Key.Back or Key.Delete && !command && !option)
        {
            if (Controller.Selection is Selection.Points { Items: var items } && items.Count > 1)
                Controller.ReportPointWarning("Remove points one at a time, so each change is measured.");
            else if (Controller.Selection is Selection.Points { Items: [var selected] })
                _ = Controller.ApplyPointCommandAsync(new PointCommand.RemovePoint(selected.Curve, selected.VertexId));
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
        var screen = CurvePointLayer.ScreenDirection(e.Key);
        if (screen == default) return;
        if (option)
        {
            PanBy(screen.X * Bounds.Width * .1, screen.Y * Bounds.Height * .1);
            e.Handled = true;
            return;
        }
        if (focusedPoint is not { } focus) return;
        var (span, aft) = Layer(Controller.Planform).KeyboardDirection(e.Key);
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
        var map = Layer(plan);
        targets.Clear();
        foreach (var curve in new[] { plan.Leading, plan.Trailing })
        foreach (var point in curve.Points)
        {
            targets.Add(point);
        }
        // D-2: the marker mirrors the controller's preview of the release check, at the offending hull point.
        advisoryCrossing = Controller!.GestureCrossing is not null;
        if (Controller.GestureCrossing is { } crossing) advisoryPoint = map.ToScreen(crossing.SpanMeters, crossing.Ordinate);
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
        var map = Layer(plan);
        bool certified = Controller!.Inspection?.Geometry.Status == GeometryStatus.Certified;
        RenderBanner = certified ? null : "Foil not certified. Point editing unavailable.";
        if (certified)
            DrawPlan(context, map, plan, Controller.Selection);
        else
        {
            using (context.PushOpacity(.35))
                DrawPlan(context, map, plan, Controller.Selection);
            DrawLabel(context, RenderBanner!, new Point(12, 12));
        }
        if (rebuildPreview is { } preview)
        {
            var station = SelectionBrush ?? Brushes.White;
            var dash = new Pen(station, 2, new DashStyle([7, 4], 0));
            map.DrawCurve(context, preview.Curve.Samples, dash);
            using (context.PushOpacity(.7))
            {
                var polygon = new Pen(station, 1, new DashStyle([4, 3], 0));
                for (int i = 1; i < preview.Curve.Points.Count; i++)
                    context.DrawLine(polygon, map.ToScreen(preview.Curve.Points[i - 1]), map.ToScreen(preview.Curve.Points[i]));
            }
            var glyphs = new PointGlyphBrushes(station, station, BackgroundBrush ?? Brushes.Transparent, MuteBrush ?? Brushes.White);
            foreach (var point in preview.Curve.Points)
                CurvePointLayer.DrawGlyph(context, point, map.ToScreen(point), glyphs, false);
            if (Controller.CombVisible)
                foreach (var tooth in CfdWorkbench.Core.Planform.Comb(preview.Curve))
                {
                    var start = map.ToScreen(tooth.SpanMeters, tooth.Ordinate);
                    context.DrawLine(new Pen(station, 1), start,
                        start + new Vector(tooth.NormalSpan, tooth.NormalAft) * Math.Clamp(Math.Abs(tooth.Curvature) * 100, 6, 24));
                }
            var current = preview.Curve.Curve == "leading" ? plan.Leading : plan.Trailing;
            var at = current.Samples.OrderBy(item => Math.Abs(item.SpanMeters - preview.AtEta * plan.HalfSpanMeters)).First();
            var changed = preview.Curve.Samples.OrderBy(item => Math.Abs(item.SpanMeters - preview.AtEta * plan.HalfSpanMeters)).First();
            var first = map.ToScreen(at.SpanMeters, at.Ordinate);
            var second = map.ToScreen(changed.SpanMeters, changed.Ordinate);
            context.DrawLine(new Pen(WarningBrush ?? Brushes.White, 2), first, second);
            DrawLabel(context, $"{preview.MaxChange * 1000:0.00} mm", second + new Vector(8, -18));
        }
        if (Controller.CombVisible && Controller.Selection is Selection.Points points && points.Items.Count > 0)
        {
            var rail = points.Items[0].Curve == "leading" ? plan.Leading : plan.Trailing;
            var combPen = new Pen(WarningBrush ?? Brushes.White, .9);
            foreach (var tooth in CfdWorkbench.Core.Planform.Comb(rail))
            {
                var start = map.ToScreen(tooth.SpanMeters, tooth.Ordinate);
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
            map.DrawHoverRing(context, hovered, MuteBrush ?? Brushes.White);
        if (focusedPoint is { } focus)
        {
            var target = targets.FirstOrDefault(point => point.Curve == focus.Curve && point.Id == focus.VertexId);
            if (target is not null)
                map.DrawFocusRing(context, target, FocusBrush ?? Brushes.White);
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
            owner.Controller?.Planform is not null
                ? owner.targets.Select(point => CurvePointLayer.Peer(owner, () => owner.PointName(point),
                    () => owner.ScreenPoint(point), () => owner.RequestValue(point))).ToList()
                : [];
    }

    private string PointName(PointView point)
    {
        var plan = Controller?.Planform;
        var curve = point.Curve == "leading" ? plan?.Leading : plan?.Trailing;
        string role = point.Role.ToString().ToLowerInvariant();
        if (point.AnchorId is { } anchorId && curve?.Points.FirstOrDefault(item => item.Id == anchorId) is { } anchor)
        {
            double span = point.SpanMeters - anchor.SpanMeters;
            double aft = point.Ordinate - anchor.Ordinate;
            double angle = Math.Atan2(aft, span) * 180 / Math.PI;
            double length = Math.Sqrt(span * span + aft * aft) * 1000;
            role = $"{(point.Index < anchor.Index ? "in" : "out")} handle, angle {angle:F2}°, length {length:F2} mm";
        }
        return $"{(point.Curve == "leading" ? "Leading" : "Trailing")} edge, point {point.Index + 1} of {curve?.Points.Count ?? 0}, " +
            $"{role}, from root {point.SpanMeters * 1000:F2} mm, aft {point.Ordinate * 1000:F2} mm";
    }

    // F-3: the fitted view keeps the planform, both halves, every point glyph and every station chip clear of the
    // Tracing probe box (top band, 8 + 48 px) and the scale bar (bottom band, 50 px), with a margin.
    private const double GlyphMargin = 8;
    private const double ChipHalfWidth = 48;
    private const double ChipDrop = 34;
    private const double ScaleBarBand = 50;
    private const double FitTop = 8 + 48 + 12 + GlyphMargin;

    /// <summary>The Plan's axis mapping (span right, aft down) for the shared point layer (SR-2).</summary>
    private CurvePointLayer Layer(PlanformView plan)
    {
        var size = Bounds.Size;
        var camera = Controller!.PlanCamera;
        var points = plan.Leading.Points.Concat(plan.Trailing.Points).ToArray();
        double halfWidth = Math.Max(plan.HalfSpanMeters, points.Max(item => Math.Abs(item.SpanMeters)));
        double minAft = Math.Min(plan.Leading.Samples.Min(item => item.Ordinate), points.Min(item => item.Ordinate));
        double maxAft = Math.Max(plan.Trailing.Samples.Max(item => item.Ordinate), points.Max(item => item.Ordinate));
        double usableWidth = size.Width - 2 * (ChipHalfWidth + GlyphMargin);
        double usableHeight = size.Height - FitTop - ChipDrop - ScaleBarBand - GlyphMargin;
        double scale = Math.Max(1, Math.Min(usableWidth / (2 * halfWidth), usableHeight / Math.Max(0.01, maxAft - minAft)))
            * camera.PixelsPerMeter / 1000;
        return new CurvePointLayer(
            (span, aft) => new Point(size.Width / 2 + span * scale + camera.PanSpanPixels,
                FitTop + (aft - minAft) * scale + camera.PanAftPixels),
            position => ((position.X - size.Width / 2 - camera.PanSpanPixels) / scale,
                (position.Y - FitTop - camera.PanAftPixels) / scale + minAft));
    }

    private static IReadOnlyList<string> PlanKeyboardTargets(PlanformView view) =>
        view.Leading.Points.Select(point => point.Id)
            .Concat(view.Trailing.Points.Select(point => point.Id))
            .Concat(Enumerable.Range(0, view.Stations.Count).Select(index => $"station:{index}"))
            .ToArray();

    private void DrawPlan(DrawingContext context, CurvePointLayer map, PlanformView plan, Selection selection)
    {
        var foil = FoilBrush ?? Brushes.White;
        var mute = MuteBrush ?? Brushes.White;
        var brushes = new PointGlyphBrushes(foil, SelectionBrush ?? Brushes.White, BackgroundBrush ?? Brushes.Transparent, mute);
        var railPen = new Pen(foil, 2);
        double centreX = map.ToScreen(0, plan.Leading.Samples[0].Ordinate).X;
        context.DrawLine(new Pen(mute, 1), new Point(centreX, 0), new Point(centreX, Bounds.Height));
        foreach (double side in new[] { -1d, 1d })
        {
            var fill = new StreamGeometry();
            using (var path = fill.Open())
            {
                var first = plan.Leading.Samples[0];
                path.BeginFigure(map.ToScreen(first.SpanMeters * side, first.Ordinate), true);
                foreach (var sample in plan.Leading.Samples.Skip(1))
                    path.LineTo(map.ToScreen(sample.SpanMeters * side, sample.Ordinate));
                foreach (var sample in plan.Trailing.Samples.Reverse())
                    path.LineTo(map.ToScreen(sample.SpanMeters * side, sample.Ordinate));
                path.EndFigure(true);
            }
            using (context.PushOpacity(.25)) context.DrawGeometry(foil, null, fill);
        }
        foreach (var rail in new[] { plan.Leading, plan.Trailing })
        {
            foreach (double side in new[] { -1d, 1d })
                map.DrawCurve(context, rail.Samples, railPen, side);
            map.DrawPoints(context, rail, brushes, selection);
        }
    }
}
