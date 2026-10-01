using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;

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
        protected override string GetNameCore() =>
            $"{(point.Curve == "leading" ? "Leading" : "Trailing")} edge, point {point.Index + 1} of 10, {point.Role}, span {point.SpanMeters * 1000:F2} mm, aft {point.AftMeters * 1000:F2} mm";
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
                plan.Leading.Samples.Min(item => item.AftMeters)));
        private readonly double minAft = plan.Leading.Samples.Min(item => item.AftMeters);

        public Point ToScreen(double span, double aft) => new(
            size.Width / 2 + span * scale + camera.PanSpanPixels,
            40 + (aft - minAft) * scale + camera.PanAftPixels);

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
