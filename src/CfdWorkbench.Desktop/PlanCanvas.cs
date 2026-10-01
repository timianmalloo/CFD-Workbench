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
    public IReadOnlyList<string> KeyboardTargets
    {
        get
        {
            var plan = Controller?.Planform;
            return plan is null ? [] : plan.Leading.Points.Select(point => point.Id)
                .Concat(plan.Trailing.Points.Select(point => point.Id))
                .Concat(Enumerable.Range(0, plan.Stations.Count).Select(index => $"station:{index}"))
                .ToArray();
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
        PointView? nearest = null;
        double distance = 14 * 14;
        foreach (var candidate in targets)
        {
            var centre = ScreenPoint(candidate);
            double squared = Math.Pow(position.X - centre.X, 2) + Math.Pow(position.Y - centre.Y, 2);
            if (squared > distance) continue;
            if (squared == distance && Controller?.Selection is Selection.Points selected &&
                !selected.Items.Any(item => item.Curve == candidate.Curve && item.VertexId == candidate.Id)) continue;
            nearest = candidate;
            distance = squared;
        }
        return nearest;
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
            PanAftPixels = pivot.Y - 40 - (pivot.Y - 40 - camera.PanAftPixels) * actual
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
        InvalidateVisual();
    }

    private void RequestValue(PointView point) => LastValueRequest = $"{point.Curve}:{point.Id}";

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush ?? Brushes.Transparent, null, new Rect(Bounds.Size));
        var plan = Controller?.Planform;
        if (plan is null) return;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        map.Draw(context, FoilBrush ?? Brushes.White, SelectionBrush ?? Brushes.White,
            BackgroundBrush ?? Brushes.Transparent, Controller.Selection);
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
        if (hoveredPoint is { } hovered)
            context.DrawEllipse(null, new Pen(MuteBrush ?? Brushes.White, 1.5), ScreenPoint(hovered), 10, 10);
        if (focusedPoint is { } focus)
        {
            var target = targets.FirstOrDefault(point => point.Curve == focus.Curve && point.Id == focus.VertexId);
            if (target is not null)
                context.DrawEllipse(null, new Pen(FocusBrush ?? Brushes.White, 3), ScreenPoint(target), 13, 13);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PlanCanvasPeer(this);

    private sealed class PlanCanvasPeer(PlanCanvas owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override List<AutomationPeer>? GetChildrenCore() =>
            owner.targets.Select(point => (AutomationPeer)new PlanPointPeer(owner, point)).ToList();
    }

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

    // One axis map owns drawing and placement; M1.2b2 can reuse it for elevations.
    private sealed class AxisPointLayer(PlanformView plan, Size size, PlanCamera camera)
    {
        private readonly double scale = Math.Min((size.Width - 80) / (2 * plan.HalfSpanMeters),
            (size.Height - 80) / Math.Max(0.01, plan.Trailing.Samples.Max(item => item.AftMeters) -
                plan.Leading.Samples.Min(item => item.AftMeters))) * camera.PixelsPerMeter / 1000;
        private readonly double minAft = plan.Leading.Samples.Min(item => item.AftMeters);

        public Point ToScreen(double span, double aft) => new(
            size.Width / 2 + span * scale + camera.PanSpanPixels,
            40 + (aft - minAft) * scale + camera.PanAftPixels);

        public (double Span, double Ordinate) FromScreen(Point position) =>
            ((position.X - size.Width / 2 - camera.PanSpanPixels) / scale,
                (position.Y - 40 - camera.PanAftPixels) / scale + minAft);

        public void Draw(DrawingContext context, IBrush foil, IBrush station, IBrush background,
            Selection selection)
        {
            var railPen = new Pen(foil, 2);
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
