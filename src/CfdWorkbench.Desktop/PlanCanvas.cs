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
    private WorkbenchController? controller;
    private readonly List<PointView> targets = [];
    private IBrush? background;
    private IBrush? foil;
    private IBrush? station;

    public WorkbenchController? Controller
    {
        get => controller;
        set
        {
            if (controller is not null) controller.Changed -= UpdatePlan;
            controller = value;
            if (controller is not null) controller.Changed += UpdatePlan;
            UpdatePlan();
        }
    }

    public string? LastValueRequest { get; private set; }

    public PlanCanvas()
    {
        Focusable = true;
        AttachedToVisualTree += (_, _) =>
        {
            background = ResolveBrush("ViewportBrush");
            foil = ResolveBrush("FoilBrush");
            station = ResolveBrush("StationBrush");
            UpdatePlan();
        };
        SizeChanged += (_, _) => UpdatePlan();
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

    private IBrush? ResolveBrush(string key)
    {
        if (this.TryFindResource(key, out var resource) && resource is IBrush brush) return brush;
        if (Application.Current is not null && Application.Current.TryFindResource(key, out var appResource) &&
            appResource is IBrush applicationBrush) return applicationBrush;
        return null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(background ?? Brushes.Transparent, null, new Rect(Bounds.Size));
        var plan = Controller?.Planform;
        if (plan is null) return;
        var map = new AxisPointLayer(plan, Bounds.Size, Controller!.PlanCamera);
        map.Draw(context, foil ?? Brushes.White, station ?? Brushes.White);
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

        public void Draw(DrawingContext context, IBrush foil, IBrush station)
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
                    context.DrawEllipse(foil, null, centre, point.Role == PointRole.Control ? 5.5 : 7,
                        point.Role == PointRole.Control ? 5.5 : 7);
                }
            }
        }
    }
}
