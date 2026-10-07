using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using System.Globalization;
using System.Text;

namespace CfdWorkbench.Desktop;

/// <summary>
/// The Front and Side elevations (M1.2b2 §11.1). Each is a band (top 60 %) over a lane (bottom 40 %).
/// Front · looking aft: the Front camera over the placed surface (starboard on the viewer's left), the dihedral curve on
/// the leading-edge line with its points on the starboard half, and the t/c lane on the band's span axis (tip ← root).
/// Side · from starboard: every authored section at its true placement (nose right), the selected station at full weight
/// with its chip, and the twist lane root → tip with a zero line. The band is a <see cref="SurfaceRenderer"/> (the
/// content child, so the mesh, mirror and camera have one render path); this control draws what lies under it (the
/// viewport, the centre or ground line) and, on an overlay child, everything over it. Geometry comes only from the mesh
/// (<see cref="WorkbenchController.Surface"/>), the channel points (<see cref="WorkbenchController.CurveFor"/>) and
/// <see cref="Placement.Frame"/> for the probe; this control applies a camera and an axis mapping, nothing else.
/// </summary>
public sealed class ElevationView : Control
{
    public static readonly StyledProperty<SingleView> ViewProperty =
        AvaloniaProperty.Register<ElevationView, SingleView>(nameof(View), SingleView.Front);
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(BackgroundBrush));
    public static readonly StyledProperty<IBrush?> FoilBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(FoilBrush));
    public static readonly StyledProperty<IBrush?> FoilEdgeBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(FoilEdgeBrush));
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(SelectionBrush));
    public static readonly StyledProperty<IBrush?> FocusBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(FocusBrush));
    public static readonly StyledProperty<IBrush?> MuteBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(MuteBrush));
    public static readonly StyledProperty<IBrush?> SoftBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(SoftBrush));
    public static readonly StyledProperty<IBrush?> GridBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(GridBrush));
    public static readonly StyledProperty<IBrush?> InkBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(InkBrush));
    public static readonly StyledProperty<IBrush?> ShadeGrazingBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(ShadeGrazingBrush));
    public static readonly StyledProperty<IBrush?> ShadeLitBrushProperty =
        AvaloniaProperty.Register<ElevationView, IBrush?>(nameof(ShadeLitBrush));

    /// <summary>The band's share of the view (§11.1: band top 60 %, lane bottom 40 %).</summary>
    public const double BandShare = 0.6;
    /// <summary>The Front band's fill: the shading ramp at a fixed 0.7 (the approved mockup), so the silhouette carries the shape.</summary>
    public const double FrontFill = 0.7;
    /// <summary>Below this width the probe wraps to two lines, at most this share of the view wide (§11.1, the mockup's 560 px).</summary>
    public const double ProbeWrapWidth = 560;
    public const double ProbeWrapShare = 0.46;
    private const double CaptionReserve = 150;   // the shell's view-caption chip ("Side · from starboard") over the band's left edge
    private const double PlateFont = 13;
    private const double ChipFont = 11;
    private const double TickFont = 11;
    private const double PlatePadX = 8, PlatePadY = 5, PlateInset = 6;

    public static readonly string TwistClampReason =
        $"Twist is limited to ±{Number(CfdWorkbench.Core.Geometry.TwistDomainDegrees)}° — larger angles can't be checked yet.";
    public const string ThicknessClampReason = "t/c must stay above 0 % and below 100 %.";

    private readonly Overlay overlay;
    private SurfaceRenderer? band;
    private WorkbenchController? controller;
    private bool attached;
    private readonly List<PointView> targetList = [];
    private bool targetsStale = true;
    private readonly Dictionary<string, (double Low, double High, double Step)> laneRanges = new(StringComparer.Ordinal);
    private PointRef? focusedPoint;
    private PointView? hoveredPoint;
    private int keyboardIndex = -1;
    private PointView? gestureOrigin;
    private Point pressPosition;
    private (double Span, double Ordinate)? gestureTarget;
    // A press on empty space (§11.3 Pan row): a drag pans the band camera, a click clears the selection.
    private ViewCamera? panOrigin;
    private Point panPress;
    private Point panLast;
    private bool panning;
    private bool panClicks;
    // §11.3: a second click at the same spot on overlapping Side sections picks the next one.
    private Point? lastSectionClick;

    static ElevationView()
    {
        FocusableProperty.OverrideDefaultValue<ElevationView>(true);
        AffectsRender<ElevationView>(BackgroundBrushProperty, GridBrushProperty, MuteBrushProperty);
    }

    public ElevationView()
    {
        overlay = new Overlay(this) { IsHitTestVisible = false };
        VisualChildren.Add(overlay);
        AttachedToVisualTree += (_, _) =>
        {
            attached = true;
            if (controller is not null) Subscribe(controller);
            Update();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            attached = false;
            if (controller is not null) Unsubscribe(controller);
        };
        SizeChanged += (_, _) => Update();
        PropertyChanged += (_, args) =>
        {
            if (args.Property == ViewProperty || args.Property == FoilBrushProperty || args.Property == FoilEdgeBrushProperty ||
                args.Property == SelectionBrushProperty || args.Property == ShadeGrazingBrushProperty ||
                args.Property == ShadeLitBrushProperty || args.Property == MuteBrushProperty)
                StyleBand();
            Redraw();
        };
    }

    public SingleView View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }
    public IBrush? BackgroundBrush { get => GetValue(BackgroundBrushProperty); set => SetValue(BackgroundBrushProperty, value); }
    public IBrush? FoilBrush { get => GetValue(FoilBrushProperty); set => SetValue(FoilBrushProperty, value); }
    public IBrush? FoilEdgeBrush { get => GetValue(FoilEdgeBrushProperty); set => SetValue(FoilEdgeBrushProperty, value); }
    public IBrush? SelectionBrush { get => GetValue(SelectionBrushProperty); set => SetValue(SelectionBrushProperty, value); }
    public IBrush? FocusBrush { get => GetValue(FocusBrushProperty); set => SetValue(FocusBrushProperty, value); }
    public IBrush? MuteBrush { get => GetValue(MuteBrushProperty); set => SetValue(MuteBrushProperty, value); }
    public IBrush? SoftBrush { get => GetValue(SoftBrushProperty); set => SetValue(SoftBrushProperty, value); }
    public IBrush? GridBrush { get => GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }
    public IBrush? InkBrush { get => GetValue(InkBrushProperty); set => SetValue(InkBrushProperty, value); }
    public IBrush? ShadeGrazingBrush { get => GetValue(ShadeGrazingBrushProperty); set => SetValue(ShadeGrazingBrushProperty, value); }
    public IBrush? ShadeLitBrush { get => GetValue(ShadeLitBrushProperty); set => SetValue(ShadeLitBrushProperty, value); }

    /// <summary>The band: the mesh drawn through this view's camera. The model area sets its surface, camera and display; this view sets its camera on each camera-only change.</summary>
    [Content]
    public SurfaceRenderer? Band
    {
        get => band;
        set
        {
            if (ReferenceEquals(band, value)) return;
            if (band is not null)
            {
                VisualChildren.Remove(band);
                LogicalChildren.Remove(band);
            }
            band = value;
            if (band is not null)
            {
                band.IsHitTestVisible = false;
                VisualChildren.Insert(0, band);
                LogicalChildren.Add(band);
                StyleBand();
            }
            InvalidateArrange();
        }
    }

    public WorkbenchController? Controller
    {
        get => controller;
        set
        {
            if (ReferenceEquals(controller, value)) return;
            if (attached && controller is not null) Unsubscribe(controller);
            controller = value;
            if (attached && controller is not null) Subscribe(controller);
            Update();
        }
    }

    // A camera write redraws only this view: the band takes the new camera here, never through the shell's pane refresh.
    private void Subscribe(WorkbenchController source)
    {
        source.Changed += Update;
        source.CameraChanged += OnCameraChanged;
        source.LayersChanged += OnLayersChanged;
    }

    private void Unsubscribe(WorkbenchController source)
    {
        source.Changed -= Update;
        source.CameraChanged -= OnCameraChanged;
        source.LayersChanged -= OnLayersChanged;
    }

    // Reading AnalysisView refreshes the controller's LayerSet before this elevation redraws it.
    private void OnLayersChanged() { _ = controller?.AnalysisView; Redraw(); }

    private void OnCameraChanged(SingleView view)
    {
        if (view != View) return;
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnCameraChanged(view));
            return;
        }
        if (band is not null && Camera is { } camera) band.Camera = camera;
        // The targets are model points (camera-independent), so a camera step redraws without the channel work.
        Redraw();
    }

    // ---------------------------------------------------------------- read-outs (tests and peers read these)

    public string? ProbeText { get; private set; }
    public string? LastValueRequest { get; private set; }
    public PointRef? FocusedTarget => focusedPoint;
    public bool IsFront => View == SingleView.Front;
    public string LaneCaption => IsFront ? "Thickness t/c (%) · tip ← root" : "Twist (°) · root → tip";
    public string ViewName => IsFront ? "Front" : "Side";

    /// <summary>The curves this view edits, in Tab order: Front dihedral then t/c, Side twist.</summary>
    public IReadOnlyList<string> Curves => IsFront ? ["dihedral", "thickness"] : ["twist"];

    public Rect BandRect => new(0, 0, Bounds.Width, Math.Round(Bounds.Height * BandShare));

    /// <summary>The lane's plotting area (the mockup: 30 px under the band for the caption, 52 px above the bottom).</summary>
    public Rect LaneRect
    {
        get
        {
            double top = BandRect.Bottom + 30, bottom = Math.Max(top + 1, Bounds.Height - 52);
            if (IsFront) return new Rect(0, top, Bounds.Width, bottom - top);
            return new Rect(56, top, Math.Max(1, Bounds.Width - 86), bottom - top);
        }
    }

    public ViewCamera? Camera => controller?.CameraFor(View);

    public IReadOnlyList<string> KeyboardTargets => CurrentTargets.Select(point => point.Curve + ":" + point.Id).ToArray();

    public IReadOnlyList<PointView> Targets => CurrentTargets;

    /// <summary>The probe plate's box, or an empty rect with no probe.</summary>
    public Rect ProbeBounds => ProbeText is { } text ? ProbeLayout(text).Box : default;

    /// <summary>The probe's lines as drawn (one, or two in a narrow view).</summary>
    public IReadOnlyList<string> ProbeLines => ProbeText is { } text ? ProbeLayout(text).Lines : [];

    /// <summary>"100 mm = 87 px": the band's own scale (DR-VIEW-3), a round length of at least 30 px.</summary>
    public string? ScaleText
    {
        get
        {
            if (Camera is not { } camera || BandRect.Width <= 0) return null;
            double perMetre = Math.Abs(camera.Project(new Point3(0, 1, 0), BandRect.Size).X - camera.Project(new Point3(0, 0, 0), BandRect.Size).X);
            if (!IsFront) perMetre = Math.Abs(camera.Project(new Point3(1, 0, 0), BandRect.Size).X - camera.Project(new Point3(0, 0, 0), BandRect.Size).X);
            if (!(perMetre > 0) || !double.IsFinite(perMetre)) return null;
            double millimetres = NiceAtLeast(30 / perMetre * 1000);
            return $"{Number(millimetres, 0)} mm = {Number(Math.Round(millimetres / 1000 * perMetre), 0)} px";
        }
    }

    // ---------------------------------------------------------------- the fit (DR-VIEW-3: each elevation fits itself)

    /// <summary>
    /// The elevation's own fit: its named camera (orthographic), with the approved mockup's margins — Front 30 px a side,
    /// Side 60 px a side so the station chips beside the nose stay inside — and 40 px above and below.
    /// </summary>
    public static ViewCamera Fitted(SingleView view, Point3 minimum, Point3 maximum, Size band)
    {
        var camera = ViewCamera.Named(view == SingleView.Side ? NamedCamera.Side : NamedCamera.Front, minimum, maximum, band);
        double sideMargin = view == SingleView.Side ? 60 : 30, endMargin = 40;
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
        foreach (double x in new[] { minimum.X, maximum.X })
            foreach (double y in new[] { minimum.Y, maximum.Y })
                foreach (double z in new[] { minimum.Z, maximum.Z })
                {
                    var at = camera.Project(new Point3(x, y, z), band);
                    minX = Math.Min(minX, at.X); maxX = Math.Max(maxX, at.X);
                    minY = Math.Min(minY, at.Y); maxY = Math.Max(maxY, at.Y);
                }
        double factor = Math.Min((band.Width - 2 * sideMargin) / Math.Max(1e-9, maxX - minX),
            (band.Height - 2 * endMargin) / Math.Max(1e-9, maxY - minY));
        if (!(factor > 0) || !double.IsFinite(factor)) return camera;
        double distance = camera.Distance / factor;
        return camera with { Distance = distance, FitDistance = distance };
    }

    /// <summary>⌘0: refit the band to the whole surface (both halves).</summary>
    public void Fit()
    {
        if (controller?.Surface is not { } surface || BandRect.Width <= 0 || BandRect.Height <= 0) return;
        controller.SetCameraFor(View, Fitted(View, new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ),
            new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ), BandRect.Size));
    }

    /// <summary>F: fit the selected station's section (or everything when none is selected).</summary>
    public void FitSelection()
    {
        if (controller?.FitBounds() is not { } bounds || BandRect.Width <= 0) return;
        controller.SetCameraFor(View, Fitted(View, bounds.Minimum, bounds.Maximum, BandRect.Size));
    }

    public void ZoomAt(double factor, Point pivot)
    {
        if (controller is null || Camera is not { } camera || !(factor > 0) || !double.IsFinite(factor)) return;
        controller.SetCameraFor(View, camera.ZoomAbout(pivot, factor, BandRect.Size));
    }

    public void PanBy(double dx, double dy)
    {
        if (controller is null || Camera is not { } camera) return;
        controller.SetCameraFor(View, camera.Pan(dx, dy, BandRect.Size));
    }

    // ---------------------------------------------------------------- axis mappings (one per curve; SR-2)

    /// <summary>The axis mapping of one channel: the dihedral on the Front band's LE line, t/c and twist in their lanes.</summary>
    public CurvePointLayer? LayerFor(string curve)
    {
        if (controller?.Planform is not { } plan || Bounds.Width <= 0 || Bounds.Height <= 0) return null;
        double half = plan.HalfSpanMeters;
        var lane = LaneRect;
        if (curve == "twist")
        {
            var range = LaneRange("twist");
            return new CurvePointLayer(
                (span, value) => new Point(lane.Left + span / half * lane.Width, LaneY(value, range, lane)),
                at => ((at.X - lane.Left) / lane.Width * half, LaneValue(at.Y, range, lane)));
        }
        if (Camera is not { } camera) return null;
        var size = BandRect.Size;
        double depth = camera.Target.X;
        var origin = camera.Project(new Point3(depth, 0, 0), size);
        var spanAxis = camera.Project(new Point3(depth, 1, 0), size) - origin;
        if (curve == "dihedral")
        {
            var heightAxis = camera.Project(new Point3(depth, 0, 1), size) - origin;
            double determinant = spanAxis.X * heightAxis.Y - spanAxis.Y * heightAxis.X;
            return new CurvePointLayer(
                (span, height) => camera.Project(new Point3(depth, span, height), size),
                at =>
                {
                    var d = at - origin;
                    return ((d.X * heightAxis.Y - d.Y * heightAxis.X) / determinant, (spanAxis.X * d.Y - spanAxis.Y * d.X) / determinant);
                });
        }
        if (curve == "thickness")
        {
            var range = LaneRange("thickness");
            return new CurvePointLayer(
                (span, value) => new Point(origin.X + span * spanAxis.X, LaneY(value, range, lane)),
                at => ((at.X - origin.X) / spanAxis.X, LaneValue(at.Y, range, lane)));
        }
        return null;
    }

    private static double LaneY(double value, (double Low, double High, double Step) range, Rect lane) =>
        lane.Bottom - (value - range.Low) / (range.High - range.Low) * lane.Height;

    private static double LaneValue(double y, (double Low, double High, double Step) range, Rect lane) =>
        range.Low + (lane.Bottom - y) / lane.Height * (range.High - range.Low);

    /// <summary>
    /// The lane's value range in the curve's own unit: round ticks (1 · 2 · 5 × 10ⁿ, about three), twist padded half a tick
    /// either side around zero, t/c from 0. Held during a gesture, so the axis never moves under a dragged point.
    /// </summary>
    public (double Low, double High, double Step) LaneRange(string curve)
    {
        if (laneRanges.TryGetValue(curve, out var held) && controller?.Gesture is not (null or GestureState.Idle)) return held;
        var view = controller?.CurveFor(curve);
        double scale = curve == "thickness" ? 100 : 1;
        var values = (view?.Points.Select(point => point.Ordinate) ?? []).Concat(view?.Samples.Select(sample => sample.Ordinate) ?? [])
            .Select(value => value * scale).Append(0).ToArray();
        double low = values.Min(), high = values.Max();
        double step = NiceAtLeast(Math.Max(high - low, 1e-6 * scale) / 2);
        (double Low, double High, double Step) range = curve == "thickness"
            ? (0, Math.Max(step, Math.Ceiling(high / step) * step) / scale, step / scale)
            : ((Math.Floor(low / step) * step - step / 2) / scale, (Math.Ceiling(high / step) * step + step / 2) / scale, step / scale);
        laneRanges[curve] = range;
        return range;
    }

    private static double NiceAtLeast(double value)
    {
        if (!(value > 0) || !double.IsFinite(value)) return 1;
        double power = Math.Pow(10, Math.Floor(Math.Log10(value)));
        foreach (double mantissa in new[] { 1d, 2d, 5d, 10d })
            if (mantissa * power >= value * (1 - 1e-12)) return mantissa * power;
        return 10 * power;
    }

    public Point ScreenPoint(PointView point) => LayerFor(point.Curve)?.ToScreen(point) ?? default;

    public PointView? HitTestPoint(Point position)
    {
        PointView? nearest = null;
        double best = double.PositiveInfinity;
        if (controller is null) return null;
        foreach (string curve in Curves)
        {
            var layer = LayerFor(curve);
            if (layer?.HitTest(CurrentTargets.Where(point => point.Curve == curve), position, controller.Selection) is not { } hit) continue;
            double distance = Point.Distance(layer.ToScreen(hit), position);
            if (distance < best) { best = distance; nearest = hit; }
        }
        return nearest;
    }

    // ---------------------------------------------------------------- Side band: sections

    /// <summary>The authored sections in the Side band, with their station index and name, as projected polylines.</summary>
    public IReadOnlyList<(int Index, double Eta, string Name, Point[] Outline)> SideSections()
    {
        if (IsFront || controller?.Surface is not { } surface || controller.Planform is not { } plan || Camera is not { } camera) return [];
        var result = new List<(int, double, string, Point[])>();
        // PlacedSection.Assignment is the authored station index (Placement.PlaceStation), the index into plan.Stations.
        foreach (var section in surface.Sections)
        {
            if (section.Assignment is not int index || index >= plan.Stations.Count) continue;
            var outline = section.Upper.Concat(section.Lower.Reverse()).Select(point => camera.Project(point, BandRect.Size)).ToArray();
            result.Add((index, section.Eta, StationName(index, section.Eta), outline));
        }
        return result;
    }

    public static string StationName(int index, double eta) => eta == 0 ? "Root" : eta == 1 ? "Tip" : $"Station {index + 1}";

    /// <summary>The authored section whose outline passes within 8 px of the pointer (Side band), or null.</summary>
    public (int Index, double Eta, string Name)? SectionAt(Point position) =>
        SectionsAt(position) is [var nearest, ..] ? nearest : null;

    /// <summary>Every authored section whose outline passes within 8 px of the pointer (Side band), nearest first.</summary>
    public IReadOnlyList<(int Index, double Eta, string Name)> SectionsAt(Point position)
    {
        if (!BandRect.Contains(position)) return [];
        var near = new List<(double Distance, (int, double, string) Section)>();
        foreach (var (index, eta, name, outline) in SideSections())
        {
            double best = double.PositiveInfinity;
            for (int k = 1; k < outline.Length; k++) best = Math.Min(best, SegmentDistance(position, outline[k - 1], outline[k]));
            if (best <= 8) near.Add((best, (index, eta, name)));
        }
        // Candidates are per station (a station η that recurs in the mesh lists one candidate).
        return near.OrderBy(item => item.Distance).Select(item => item.Section).DistinctBy(item => item.Item1).ToArray();
    }

    // The section a press picks: the nearest; after a single click at the same spot (within 3 px), the one after the
    // selected; a double-click keeps the one its first click picked, so it opens what the user sees selected.
    private (int Index, double Eta, string Name)? PickSection(Point position, int clicks)
    {
        var candidates = SectionsAt(position);
        if (candidates.Count == 0) return null;
        bool samePlace = lastSectionClick is { } last && Point.Distance(last, position) <= 3;
        lastSectionClick = position;
        int current = controller?.Selection is Selection.Station selected && samePlace
            ? candidates.ToList().FindIndex(item => item.Index == selected.Index) : -1;
        if (current < 0) return candidates[0];
        return clicks >= 2 ? candidates[current] : candidates[(current + 1) % candidates.Count];
    }

    private static double SegmentDistance(Point p, Point a, Point b)
    {
        var ab = b - a;
        double length = ab.X * ab.X + ab.Y * ab.Y;
        double t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / length, 0, 1);
        return Point.Distance(p, a + ab * t);
    }

    /// <summary>The station chips beside each authored section's nose (Side), placed so none overlaps another; the selected one first.</summary>
    public IReadOnlyList<(int Index, string Name, Rect Bounds)> Chips()
    {
        var sections = SideSections();
        int? selected = controller?.Selection is Selection.Station station ? station.Index : null;
        var placed = new List<(int, string, Rect)>();
        foreach (var (index, _, name, outline) in sections.OrderBy(item => item.Index == selected ? 0 : 1))
        {
            var nose = outline[0];
            double width = TextWidth(name, ChipFont, FontWeight.SemiBold) + 12;
            var rect = new Rect(nose.X + 10, nose.Y - 9, width, 18);
            while (placed.Any(chip => chip.Item3.Intersects(rect))) rect = rect.Translate(new Vector(0, 20));
            placed.Add((index, name, rect));
        }
        return placed;
    }

    // ---------------------------------------------------------------- probe

    public void HoverAt(Point position)
    {
        if (controller?.Planform is not { } plan) return;
        hoveredPoint = HitTestPoint(position);
        if (hoveredPoint is { } point) ProbeText = PointProbe(point);
        else if (!IsFront && BandRect.Contains(position)) ProbeText = SectionProbe(SectionAt(position));
        else if (EtaAt(position, plan) is { } eta) ProbeText = FrameProbe(eta);
        Redraw();
    }

    private double? EtaAt(Point position, PlanformView plan)
    {
        var layer = LayerFor(IsFront ? "thickness" : "twist");
        if (layer is null) return null;
        double span = layer.FromScreen(position).Span;
        return double.IsFinite(span) ? Math.Clamp(Math.Abs(span) / plan.HalfSpanMeters, 0, 1) : null;
    }

    private string? SectionProbe((int Index, double Eta, string Name)? section)
    {
        if (section is null && controller?.Selection is Selection.Station station)
            section = (station.Index, station.Eta, StationName(station.Index, station.Eta));
        if (section is not { } shown || Frame(shown.Eta) is not { } frame) return null;
        return $"{shown.Name} · η {frame.Eta.ToString("F3", CultureInfo.InvariantCulture)} · twist {Number(frame.TwistDegrees)}° · " +
            $"chord {Number(frame.ChordMeters * 1000)} mm";
    }

    private string? FrameProbe(double eta) => Frame(eta) is { } frame
        ? $"η {frame.Eta.ToString("F3", CultureInfo.InvariantCulture)} · from root {Number(frame.SpanMeters * 1000)} mm · " +
          $"height {Number(frame.ElevationMeters * 1000)} mm · twist {Number(frame.TwistDegrees)}° · " +
          $"t/c {Number(frame.ThicknessRatio * 100)} % · chord {Number(frame.ChordMeters * 1000)} mm"
        : null;

    private string PointProbe(PointView point)
    {
        int count = controller?.CurveFor(point.Curve)?.Points.Count ?? 0;
        return $"{ChannelName(point.Curve)} · point {point.Index + 1} of {count} · η {point.Eta.ToString("F3", CultureInfo.InvariantCulture)} · " +
            $"from root {Number(point.SpanMeters * 1000)} mm · {ValueName(point.Curve)} {Value(point.Curve, point.Ordinate)}";
    }

    private StationFrame? Frame(double eta)
    {
        if (controller?.Inspection is null) return null;
        try { return Placement.Frame(controller.Draft?.Bytes ?? Encoding.UTF8.GetBytes(controller.AcceptedSource), eta); }
        catch (ContractError) { return null; }
    }

    public static string ChannelName(string curve) => curve switch
    {
        "dihedral" => "Dihedral", "twist" => "Twist", "thickness" => "Thickness", _ => curve
    };

    private static string ValueName(string curve) => curve switch { "dihedral" => "height", "twist" => "twist", _ => "t/c" };

    /// <summary>A channel value in its display unit: height mm, twist °, t/c % (0.01 of each, §11).</summary>
    public static string Value(string curve, double ordinate) => curve switch
    {
        "dihedral" => Number(ordinate * 1000) + " mm",
        "twist" => Number(ordinate) + "°",
        _ => Number(ordinate * 100) + " %"
    };

    private static string Delta(string curve, double delta) => curve switch
    {
        "dihedral" => Signed(delta * 1000) + " mm",
        "twist" => Signed(delta) + "°",
        _ => Signed(delta * 100) + " %"
    };

    /// <summary>Two decimals with a true minus sign — the probe's one number formatter (the clamp reason uses it too).</summary>
    public static string Number(double value, int decimals = 2)
    {
        string text = Math.Abs(value).ToString("F" + decimals, CultureInfo.InvariantCulture);
        return value < 0 && text.Any(digit => digit is >= '1' and <= '9') ? "−" + text : text;
    }

    private static string Signed(double value)
    {
        string text = Number(value);
        return text.StartsWith('−') || text.All(digit => digit is '0' or '.') ? text : "+" + text;
    }

    private (string[] Lines, Rect Box) ProbeLayout(string text)
    {
        bool narrow = Bounds.Width < ProbeWrapWidth;
        // The view's caption chip sits over the left of the band: a long readout (a group drag's applied move, hold and reason)
        // wraps to two lines instead of running under the chip, so its first words (the applied value) stay readable.
        double room = Bounds.Width - 2 * PlateInset - CaptionReserve;
        bool wrap = narrow || controller?.GestureGroup is not null && TextWidth(text, PlateFont) + 2 * PlatePadX > room;
        string[] lines = [text];
        if (wrap)
        {
            var pieces = text.Split(" · ");
            int half = (pieces.Length + 1) / 2;
            lines = pieces.Length > 1 ? [string.Join(" · ", pieces.Take(half)), string.Join(" · ", pieces.Skip(half))] : [text];
            if (!narrow)
            {
                // A group readout fills each line with whole pieces up to the room, so no piece is cut at the edge.
                var filled = new List<string>();
                foreach (string piece in pieces)
                {
                    string joined = filled.Count == 0 ? piece : filled[^1] + " · " + piece;
                    if (filled.Count > 0 && TextWidth(joined, PlateFont) + 2 * PlatePadX <= room) filled[^1] = joined;
                    else filled.Add(piece);
                }
                lines = [.. filled];
            }
        }
        double maxWidth = narrow ? Bounds.Width * ProbeWrapShare : wrap ? room : Bounds.Width - 2 * PlateInset;
        double width = Math.Min(maxWidth, lines.Max(line => TextWidth(line, PlateFont)) + 2 * PlatePadX);
        double height = lines.Length * LineHeight(PlateFont) + 2 * PlatePadY;
        return (lines, new Rect(Bounds.Width - PlateInset - width, PlateInset, width, height));
    }

    // ---------------------------------------------------------------- selection, focus and keyboard

    public void SelectPoint(PointRef point, bool extend, bool toggle)
    {
        if (controller is null) return;
        var existing = controller.Selection is Selection.Points points ? points.Items.ToList() : [];
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
        controller.Select(existing.Count == 0 ? new Selection.Foil() : new Selection.Points(existing));
        Redraw();
    }

    /// <summary>Focuses a point (the probe follows it) and pans it into view clear of the probe plate (2.4.11).</summary>
    public void FocusPoint(PointRef point)
    {
        focusedPoint = point;
        int index = CurrentTargets.FindIndex(item => item.Curve == point.Curve && item.Id == point.VertexId);
        if (index >= 0)
        {
            keyboardIndex = index;
            var view = CurrentTargets[index];
            var position = ScreenPoint(view);
            double desiredX = Math.Clamp(position.X, 24, Math.Max(24, Bounds.Width - 24));
            double desiredY = position.Y;
            if (view.Curve == "dihedral")
            {
                double top = 24;
                if (ProbeText is not null && position.X > ProbeBounds.Left - 14) top = Math.Max(top, ProbeBounds.Bottom + 14);
                desiredY = Math.Clamp(position.Y, top, Math.Max(top, BandRect.Bottom - 24));
            }
            else if (view.Curve == "twist") desiredX = position.X;   // the twist lane does not pan with the band
            if (desiredX != position.X || desiredY != position.Y) PanBy(desiredX - position.X, desiredY - position.Y);
            ProbeText = PointProbe(view);
        }
        Focus();
        Redraw();
    }

    public bool FocusNext(bool reverse = false)
    {
        if (CurrentTargets.Count == 0) return false;
        int next = keyboardIndex + (reverse ? -1 : 1);
        if (next < 0 || next >= CurrentTargets.Count) return false;
        FocusPoint(new PointRef(CurrentTargets[next].Curve, CurrentTargets[next].Id));
        return true;
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        // DR-NAV-1: Tab into an elevation focuses its selected point, or its first point; ] and [ walk on from there.
        if (e.NavigationMethod != NavigationMethod.Tab || CurrentTargets.Count == 0) return;
        var selected = controller?.Selection is Selection.Points { Items: var items }
            ? items.FirstOrDefault(item => CurrentTargets.Any(point => point.Curve == item.Curve && point.Id == item.VertexId)) : null;
        FocusPoint(selected ?? new PointRef(CurrentTargets[0].Curve, CurrentTargets[0].Id));
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        // The focus ring belongs to keyboard focus: once focus leaves the view, Tab back in starts from the selection.
        if (IsKeyboardFocusWithin) return;
        focusedPoint = null;
        keyboardIndex = -1;
        Redraw();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (controller?.Planform is null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool option = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (e.Key == Key.Return && controller.Selection is Selection.Station selectedStation)
        {
            if (controller.IsAnalysis) { controller.ReportPointWarning(WorkbenchController.AnalysisPointRefusal); e.Handled = true; return; }
            _ = controller.EnterSectionAsync(selectedStation.Index, EntryOrigin.Side);
            e.Handled = true;
            return;
        }
        // DR-NAV-1: Tab leaves the view (no trap, 2.1.2); ] and [ move between points.
        if (e.Key == Key.Tab) return;
        if (e.Key is Key.OemCloseBrackets or Key.OemOpenBrackets && !command && !option)
        {
            e.Handled = FocusNext(reverse: e.Key == Key.OemOpenBrackets);
            return;
        }
        if (e.Key == Key.Space && focusedPoint is { } spaceTarget)
        {
            SelectPoint(spaceTarget, extend: false, toggle: shift);
            e.Handled = true;
            return;
        }
        if ((e.Key == Key.Apps || e.Key == Key.F10 && shift) && focusedPoint is { } menuTarget)
        {
            OpenPointMenu(menuTarget);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape)
        {
            if (panOrigin is not null) EndPan(cancel: true);
            else if (controller.Gesture != GestureState.Idle) _ = controller.EndGestureAsync(GestureEnd.Escape);
            else controller.Select(new Selection.Foil());
            Redraw();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Return && Focused() is { } valueTarget)
        {
            RequestValue(new PointRef(valueTarget.Curve, valueTarget.Id));
            e.Handled = true;
            return;
        }
        if (command && e.Key == Key.D0) { Fit(); e.Handled = true; return; }
        bool zoomIn = command && e.Key is Key.OemPlus or Key.Add || !command && !option && shift && e.Key == Key.Z;
        bool zoomOut = command && e.Key is Key.OemMinus or Key.Subtract || !command && !option && !shift && e.Key == Key.Z;
        if (zoomIn || zoomOut)
        {
            var pivot = Focused() is { Curve: "dihedral" } focusedOnBand ? ScreenPoint(focusedOnBand) : BandRect.Center;
            ZoomAt(zoomIn ? 1.2 : 1 / 1.2, pivot);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F && !command && !option) { FitSelection(); e.Handled = true; return; }
        var screen = CurvePointLayer.ScreenDirection(e.Key);
        if (screen == default) return;
        if (option)
        {
            PanBy(screen.X * Bounds.Width * .1, screen.Y * BandRect.Height * .1);
            e.Handled = true;
            return;
        }
        if (Focused() is not { } focus || LayerFor(focus.Curve) is not { } layer) return;
        var reference = new PointRef(focus.Curve, focus.Id);
        if (controller.Gesture == GestureState.Idle && !controller.BeginGesture(reference, GestureInput.Keyboard))
        {
            AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
            e.Handled = true;
            return;
        }
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Off);
        gestureOrigin ??= focus;
        var (span, ordinate) = layer.KeyboardDirection(e.Key);
        controller.Nudge(span, ordinate, command ? NudgeModifier.Command : shift ? NudgeModifier.Shift : NudgeModifier.Plain);
        if (controller.CurveFor(focus.Curve)?.Points.FirstOrDefault(point => point.Id == focus.Id) is { } moved)
            ProbeText = PointProbe(moved) + " · Δ " + ValueName(focus.Curve) + " " + Delta(focus.Curve, moved.Ordinate - gestureOrigin.Ordinate);
        e.Handled = true;
        Redraw();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (controller?.Gesture == GestureState.Nudging && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ = controller.EndGestureAsync(GestureEnd.KeyUp);
            gestureOrigin = null;
            e.Handled = true;
        }
    }

    /// <summary>Ruling 111 (10): on a member of several selected points the request goes to the group's value row; the group stays selected.</summary>
    private void RequestValue(PointRef point)
    {
        if (controller?.IsGroupMember(point) == true)
        {
            LastValueRequest = $"group:{point.Curve}";
            this.FindAncestorOfType<Shell.ShellHost>()?.Properties.FocusFirstValue();
            return;
        }
        LastValueRequest = $"{point.Curve}:{point.VertexId}";
    }

    private PointView? Focused() => focusedPoint is { } focus
        ? CurrentTargets.FirstOrDefault(point => point.Curve == focus.Curve && point.Id == focus.VertexId) : null;

    private async Task AddAtAsync(string curve, double eta)
    {
        if (controller is null) return;
        var outcome = await controller.ApplyPointCommandAsync(new PointCommand.AddPoint(curve, eta));
        if (outcome is CommitOutcome.Committed && controller.Selection is Selection.Points { Items: [var point] })
            FocusPoint(point);
    }

    // ---------------------------------------------------------------- pointer

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (controller?.Planform is null) return;
        var position = e.GetPosition(this);
        var pressed = e.GetCurrentPoint(this).Properties;
        var hit = HitTestPoint(position);
        if (hit is null)
        {
            if (controller.IsAnalysis && pressed.IsLeftButtonPressed && e.ClickCount >= 2)
            { controller.ReportPointWarning(WorkbenchController.AnalysisPointRefusal); e.Handled = true; return; }
            if (pressed.IsLeftButtonPressed && e.ClickCount >= 2 && controller.Gesture == GestureState.Idle)
            {
                foreach (var curveName in Curves)
                {
                    if (LayerFor(curveName) is not { } layer || controller.CurveFor(curveName) is not { } curve) continue;
                    if (layer.HitTestCurve(curve.Samples, position) is not { } onCurve) continue;
                    _ = AddAtAsync(curveName, onCurve.Span / controller.Planform.HalfSpanMeters);
                    e.Handled = true;
                    return;
                }
            }
            if (!IsFront && pressed.IsLeftButtonPressed && !e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
                PickSection(position, e.ClickCount) is { } section)
            {
                controller.Select(new Selection.Station(section.Index, section.Eta));
                if (e.ClickCount >= 2) _ = controller.EnterSectionAsync(section.Index, EntryOrigin.Side);
                e.Handled = true;
                return;
            }
            if (pressed.IsLeftButtonPressed || pressed.IsMiddleButtonPressed)
            {
                panOrigin = Camera;
                panPress = panLast = position;
                panning = false;
                panClicks = pressed.IsLeftButtonPressed;
                e.Pointer.Capture(this);
                Focus();
                e.Handled = true;
            }
            return;
        }
        var reference = new PointRef(hit.Curve, hit.Id);
        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (pressed.IsRightButtonPressed || OperatingSystem.IsMacOS() && control && pressed.IsLeftButtonPressed)
        {
            if (controller.IsAnalysis) { SelectPoint(reference, false, false); controller.ReportPointWarning(WorkbenchController.AnalysisPointRefusal); e.Handled = true; return; }
            OpenPointMenu(reference);
            e.Handled = true;
            return;
        }
        bool extend = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool toggle = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || !OperatingSystem.IsMacOS() && control;
        // Design §3.2 (DR-GM-4 A) in an elevation too (Ruling 111 (6)): a plain press on a member keeps the group.
        bool plain = !extend && !toggle && pressed.IsLeftButtonPressed;
        bool keepGroup = plain && !controller.IsAnalysis && (e.ClickCount >= 2 && controller.RestoreCollapsedGroup(reference) || controller.IsGroupMember(reference));
        if (!keepGroup) SelectPoint(reference, extend, toggle);
        FocusPoint(reference);
        if (controller.IsAnalysis) { controller.ReportPointWarning(WorkbenchController.AnalysisPointRefusal); e.Handled = true; return; }
        if (e.ClickCount >= 2) RequestValue(reference);
        else if (!extend && !toggle && pressed.IsLeftButtonPressed)
        {
            // As on the Plan: Shift held during the drag locks it to one axis (§11.3).
            if (controller.BeginGesture(reference, GestureInput.Pointer))
            {
                gestureOrigin = hit;
                pressPosition = position;
                gestureTarget = null;
                e.Pointer.Capture(this);
                AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Off);
            }
            else AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
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
        if (controller?.Gesture is not (GestureState.Pressed or GestureState.Dragging) || gestureOrigin is not { } origin)
        {
            HoverAt(position);
            return;
        }
        if (LayerFor(origin.Curve) is not { } layer) return;
        var target = layer.FromScreen(position);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            var start = layer.ToScreen(origin);
            if (Math.Abs(position.X - start.X) >= Math.Abs(position.Y - start.Y)) target.Ordinate = origin.Ordinate;
            else target.Span = origin.SpanMeters;
        }
        gestureTarget = target;
        gesturePointer = position;
        controller.UpdateGesture(target.Span, target.Ordinate, Point.Distance(position, pressPosition));
        ApplyGestureProbe();
        Redraw();
        e.Handled = true;
    }

    /// <summary>
    /// The readout of a drag in progress. Design §3.3: a group shows the move Core applied, not the pointer's, applied value first
    /// (the plate clips the tail); the controller's frame lands after the pointer event, so <see cref="Update"/> re-reads it
    /// and the plate never lags the inspector by one frame. A domain hold by another member names that point and the reason.
    /// </summary>
    private void ApplyGestureProbe()
    {
        if (controller is null || gestureOrigin is not { } origin || gestureTarget is not { } target) return;
        bool group = controller.GestureGroup is not null;
        var shown = group && controller.GestureApplied is { } applied ? (applied.SpanMeters, applied.Ordinate) : (target.Span, target.Ordinate);
        string reason = ClampReason(origin.Curve, target.Ordinate);
        if (reason == "" && controller.GestureBinding is { Kind: "Domain" })
            reason = " · " + (origin.Curve == "twist" ? TwistClampReason : ThicknessClampReason);
        string point = PointProbe(origin with { SpanMeters = shown.Item1, Ordinate = shown.Item2 });
        ProbeText = group && controller.GroupHold is { } groupHold
            ? groupHold + reason + " · " + point
            : point + " · Δ " + ValueName(origin.Curve) + " " + Delta(origin.Curve, shown.Item2 - origin.Ordinate) + reason;
    }

    /// <summary>The probe's clamp reason when a twist or t/c target lies past the checkable domain (§7, §11.4).</summary>
    public static string ClampReason(string curve, double ordinate)
    {
        var unit = Channels.Unit(curve);
        if (unit.DomainLower is not { } lower || unit.DomainUpper is not { } upper) return "";
        bool clamped = curve == "thickness" ? ordinate <= lower || ordinate >= upper : ordinate < lower || ordinate > upper;
        return !clamped ? "" : " · " + (curve == "twist" ? TwistClampReason : ThicknessClampReason);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right) e.Handled = true;
        base.OnPointerReleased(e);
        if (panOrigin is not null)
        {
            if (!panning && panClicks && controller is not null) controller.Select(new Selection.Foil());
            EndPan(cancel: false);
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }
        e.Pointer.Capture(null);
        if (controller?.Gesture is GestureState.Pressed or GestureState.Dragging)
        {
            _ = controller.EndGestureAsync(GestureEnd.Release);
            e.Handled = true;
        }
        gestureOrigin = null;
        gestureTarget = null;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndPan(cancel: false);
    }

    private void EndPan(bool cancel)
    {
        if (panOrigin is { } origin && cancel && controller is not null) controller.SetCameraFor(View, origin);
        panOrigin = null;
        panning = false;
        Redraw();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomAt(Math.Pow(1.1, e.Delta.Y), e.GetPosition(this));
        e.Handled = true;
    }

    /// <summary>The point menu (D-3): each row runs the shell command of the same name with the shell's enablement.</summary>
    public void OpenPointMenu(PointRef reference)
    {
        if (this.FindAncestorOfType<Shell.ShellHost>() is not { } host) return;
        if (controller?.IsGroupMember(reference) != true) SelectPoint(reference, extend: false, toggle: false);   // design §3.2 (4)
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
            ItemsSource = new Control[] { Row("Make Anchor Point", "point.make-anchor"), Row("Make Control Point", "point.make-control"), tangent,
                new Separator(), Row("Remove Point", "point.remove"), Row($"Rebuild {PropertiesView.Curves[reference.Curve].MenuName}…", "point.rebuild"),
                new Separator(), Row("Fit", "view.fit") }
        };
        menu.Closed += (_, _) => { if (ReferenceEquals(ContextMenu, menu)) ContextMenu = null; };
        ContextMenu = menu;
        menu.Open(this);
    }

    // ---------------------------------------------------------------- update and layout

    private void Update()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Update);
            return;
        }
        foreach (var peer in peers) peer.RefreshName();
        // A hidden elevation (Plan + 3D) does no channel work per change; it rebuilds when next drawn or read.
        targetsStale = true;
        if (IsEffectivelyVisible) _ = CurrentTargets;
        if (controller?.GestureGroup is not null && controller.Gesture == GestureState.Dragging) ApplyGestureProbe();
        Redraw();
    }

    /// <summary>The channel points of this view's curves in Tab order, rebuilt after a change when first needed.</summary>
    private List<PointView> CurrentTargets
    {
        get
        {
            if (!targetsStale) return targetList;
            targetsStale = false;
            targetList.Clear();
            if (controller?.Inspection is not null)
                foreach (string curve in Curves)
                    if (controller.CurveFor(curve) is { } view) targetList.AddRange(view.Points);
            if (focusedPoint is { } focus && !targetList.Any(point => point.Curve == focus.Curve && point.Id == focus.VertexId))
                focusedPoint = null;
            return targetList;
        }
    }

    private void Redraw()
    {
        InvalidateVisual();
        overlay.InvalidateVisual();
    }

    private void StyleBand()
    {
        if (band is null) return;
        bool front = IsFront;
        band.BackgroundBrush = null;   // this control draws the viewport, and the centre or ground line under the band
        band.FoilBrush = front ? FoilBrush : null;
        band.FoilEdgeBrush = front ? null : FoilEdgeBrush;
        band.StationBrush = SelectionBrush;
        band.MuteBrush = null;         // no intermediate sections in an elevation
        var fill = front ? Mix(ShadeGrazingBrush, ShadeLitBrush, FrontFill) : null;
        band.ShadeGrazingBrush = fill;
        band.ShadeLitBrush = fill;
    }

    private static IBrush? Mix(IBrush? from, IBrush? to, double t)
    {
        if (from is not ISolidColorBrush a || to is not ISolidColorBrush b) return null;
        byte Lerp(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
        return new SolidColorBrush(Color.FromArgb(Lerp(a.Color.A, b.Color.A), Lerp(a.Color.R, b.Color.R),
            Lerp(a.Color.G, b.Color.G), Lerp(a.Color.B, b.Color.B)));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        band?.Measure(availableSize);
        overlay.Measure(availableSize);
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        band?.Arrange(new Rect(0, 0, finalSize.Width, Math.Round(finalSize.Height * BandShare)));
        overlay.Arrange(new Rect(finalSize));
        return finalSize;
    }

    // ---------------------------------------------------------------- drawing

    /// <summary>Under the band: the viewport, and the Front centre line or the Side ground line (z = 0).</summary>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush ?? Brushes.Transparent, null, new Rect(Bounds.Size));
        if (controller?.Surface is not { } surface || Camera is not { } camera) return;
        var size = BandRect.Size;
        using (context.PushClip(BandRect))
        {
            if (IsFront)
            {
                var top = camera.Project(new Point3(camera.Target.X, 0, surface.MaximumZ + 0.012), size);
                var bottom = camera.Project(new Point3(camera.Target.X, 0, surface.MinimumZ - 0.012), size);
                context.DrawLine(new Pen(MuteBrush ?? Brushes.White, 1), top, bottom);
            }
            else
            {
                var fore = camera.Project(new Point3(surface.MinimumX - 0.02, 0, 0), size);
                var aft = camera.Project(new Point3(surface.MaximumX + 0.02, 0, 0), size);
                context.DrawLine(new Pen(GridBrush ?? Brushes.Gray, 1), fore, aft);
            }
        }
    }

    private void RenderOverlay(DrawingContext context)
    {
        if (controller?.Planform is not { } plan || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var foil = FoilBrush ?? Brushes.White;
        var mute = MuteBrush ?? Brushes.White;
        var grid = GridBrush ?? mute;
        var brushes = new PointGlyphBrushes(foil, SelectionBrush ?? Brushes.White, BackgroundBrush ?? Brushes.Transparent, mute);
        var band = BandRect;
        context.DrawLine(new Pen(grid, 1), new Point(0, Math.Floor(band.Bottom) + .5), new Point(Bounds.Width, Math.Floor(band.Bottom) + .5));
        bool certified = controller.Inspection?.Geometry.Status == GeometryStatus.Certified;
        using (context.PushOpacity(certified ? 1 : .35))
        {
            if (IsFront) DrawFrontBand(context, brushes);
            else DrawSideChips(context);
            if (controller.IsAnalysis && controller.Surface is { } surface && Camera is { } camera)
                ElevationDepthLayer.Draw(context, controller.LayerSet.FirstOrDefault(l => l.Id == "depth-band"),
                    camera, surface, band, IsFront, InkBrush ?? foil, mute, SoftBrush ?? BackgroundBrush ?? Brushes.Black);
            DrawLane(context, brushes, plan);
        }
        Plate(context, LaneCaption, new Point(PlateInset, band.Bottom + 6), InkBrush ?? foil, left: true);
        if (ScaleText is { } scale) Plate(context, scale, new Point(Bounds.Width - PlateInset, band.Bottom - 30), mute, left: false);
        if (hoveredPoint is { } hovered && LayerFor(hovered.Curve) is { } hoverLayer) hoverLayer.DrawHoverRing(context, hovered, mute);
        if (Focused() is { } focus && LayerFor(focus.Curve) is { } focusLayer) focusLayer.DrawFocusRing(context, focus, FocusBrush ?? Brushes.White);
        DrawGroupHold(context, mute);
        if (ProbeText is { } probe)
        {
            var (lines, box) = ProbeLayout(probe);
            context.DrawRectangle(SoftBrush ?? BackgroundBrush, null, box, 3, 3);
            for (int line = 0; line < lines.Length; line++)
                DrawText(context, lines[line], new Point(box.Left + PlatePadX, box.Top + PlatePadY + line * LineHeight(PlateFont)),
                    InkBrush ?? foil, PlateFont, maxWidth: box.Width - 2 * PlatePadX);
        }
    }

    private Point? gesturePointer;

    /// <summary>The viewport warning colour, read from the theme at draw time (the Plan's PlanWarningBrush).</summary>
    private IBrush? WarningBrush => this.TryFindResource("PlanWarningBrush", ActualThemeVariant, out var found) && found is IBrush brush ? brush : null;

    /// <summary>
    /// Design §3.3 in an elevation (Ruling 111 (6)): the tether from the grabbed point's applied place to the pointer, and the warn
    /// outline (a square) on the member or neighbour that binds the group.
    /// </summary>
    private void DrawGroupHold(DrawingContext context, IBrush mute)
    {
        if (controller?.GestureGroup is not { } group) return;
        string curve = group[0].Curve;
        if (LayerFor(curve) is not { } layer) return;
        if (controller.GestureApplied is { } applied && gesturePointer is { } pointer)
        {
            var grabbed = layer.ToScreen(applied.SpanMeters, applied.Ordinate);
            if (Point.Distance(pointer, grabbed) > 4)
            {
                context.DrawLine(new Pen(mute, 1.5, new DashStyle([1, 3], 0)), grabbed, pointer);
                context.DrawEllipse(null, new Pen(mute, 1.5), pointer, 7, 7);
            }
        }
        if (controller.GestureBinding is { } binding && binding.Point.Curve == curve &&
            CurrentTargets.FirstOrDefault(point => point.Curve == curve && point.Id == binding.Point.VertexId) is { } bound)
        {
            var centre = layer.ToScreen(bound);
            context.DrawRectangle(null, new Pen(WarningBrush ?? Brushes.White, 2), new Rect(centre.X - 11, centre.Y - 11, 22, 22));
        }
    }

    private void DrawFrontBand(DrawingContext context, PointGlyphBrushes brushes)
    {
        if (LayerFor("dihedral") is not { } layer || controller!.CurveFor("dihedral") is not { } dihedral) return;
        var band = BandRect;
        using (context.PushClip(band))
        {
            var curvePen = new Pen(brushes.Foil, 2);
            layer.DrawCurve(context, dihedral.Samples, curvePen, 1);
            layer.DrawCurve(context, dihedral.Samples, curvePen, -1);
            // DR-VIEW-4: the selected station as a 3 px station tick at its span, on both halves as in 3D.
            if (controller.Selection is Selection.Station station && controller.Surface?.Sections.FirstOrDefault(section => section.Eta == station.Eta) is { } placed &&
                Camera is { } camera)
            {
                var (minimum, maximum) = WorkbenchController.SectionBounds(placed);
                foreach (double side in new[] { 1d, -1d })
                {
                    var top = camera.Project(new Point3(camera.Target.X, side * placed.Upper[0].Y, maximum.Z), band.Size);
                    var bottom = camera.Project(new Point3(camera.Target.X, side * placed.Upper[0].Y, minimum.Z), band.Size);
                    context.DrawLine(new Pen(brushes.Station, 3), top - new Vector(0, 6), bottom + new Vector(0, 6));
                }
            }
            layer.DrawPoints(context, dihedral, brushes, controller.Selection, controller.IsAnalysis);
        }
        DrawText(context, "starboard", new Point(12, band.Bottom - 24), MuteBrush ?? brushes.Foil, TickFont);
        DrawText(context, "port", new Point(Bounds.Width - 40, band.Bottom - 24), MuteBrush ?? brushes.Foil, TickFont);
    }

    private void DrawSideChips(DrawingContext context)
    {
        var station = SelectionBrush ?? Brushes.White;
        using (context.PushClip(BandRect))
            foreach (var (_, name, bounds) in Chips())
            {
                context.DrawRectangle(SoftBrush ?? BackgroundBrush, new Pen(station, 1), bounds.Deflate(.5));
                DrawText(context, name, bounds.TopLeft + new Vector(6, 2), InkBrush ?? FoilBrush ?? Brushes.White, ChipFont, FontWeight.SemiBold);
            }
    }

    private void DrawLane(DrawingContext context, PointGlyphBrushes brushes, PlanformView plan)
    {
        string curve = IsFront ? "thickness" : "twist";
        if (LayerFor(curve) is not { } layer || controller!.CurveFor(curve) is not { } view) return;
        var lane = LaneRect;
        var range = LaneRange(curve);
        var grid = GridBrush ?? brushes.Mute;
        double scale = curve == "thickness" ? 100 : 1;
        double tipX = layer.ToScreen(plan.HalfSpanMeters, 0).X, rootX = layer.ToScreen(0, 0).X;
        for (double value = Math.Ceiling(range.Low / range.Step - 1e-9) * range.Step; value <= range.High + range.Step * 1e-9; value += range.Step)
        {
            double y = Math.Floor(LaneY(value, range, lane)) + .5;   // a crisp 1 px tick
            bool zero = Math.Abs(value) < range.Step * 1e-6;
            string label = Number(value * scale, 0) + (curve == "thickness" ? " %" : "°");
            if (IsFront)
            {
                context.DrawLine(new Pen(grid, 1), new Point(Math.Min(tipX, rootX) - 4, y), new Point(Math.Max(tipX, rootX), y));
                DrawText(context, label, new Point(Math.Max(tipX, rootX) + 6, y - 8), brushes.Mute, TickFont);
            }
            else
            {
                context.DrawLine(new Pen(zero ? brushes.Mute : grid, 1), new Point(lane.Left - 4, y), new Point(lane.Right, y));
                DrawText(context, label, new Point(14, y - 8), brushes.Mute, TickFont);
            }
        }
        using (context.PushClip(new Rect(0, BandRect.Bottom + 1, Bounds.Width, Math.Max(0, Bounds.Height - BandRect.Bottom - 1))))
        {
            layer.DrawCurve(context, view.Samples, new Pen(brushes.Foil, 2));
            layer.DrawPoints(context, view, brushes, controller.Selection, controller.IsAnalysis);
        }
    }

    private void Plate(DrawingContext context, string text, Point anchor, IBrush ink, bool left)
    {
        double width = TextWidth(text, PlateFont) + 2 * PlatePadX, height = LineHeight(PlateFont) + 2 * PlatePadY;
        var box = new Rect(left ? anchor.X : anchor.X - width, anchor.Y, width, height);
        context.DrawRectangle(SoftBrush ?? BackgroundBrush, null, box, 3, 3);
        DrawText(context, text, box.TopLeft + new Vector(PlatePadX, PlatePadY), ink, PlateFont);
    }

    private static FormattedText Text(string text, double size, IBrush brush, FontWeight weight = FontWeight.Normal) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, weight), size, brush);

    private static double TextWidth(string text, double size, FontWeight weight = FontWeight.Normal) =>
        Text(text, size, Brushes.White, weight).WidthIncludingTrailingWhitespace;

    private static double LineHeight(double size) => Math.Ceiling(size * 1.45);

    private static void DrawText(DrawingContext context, string text, Point at, IBrush brush, double size,
        FontWeight weight = FontWeight.Normal, double maxWidth = double.PositiveInfinity)
    {
        var formatted = Text(text, size, brush, weight);
        if (double.IsFinite(maxWidth)) { formatted.MaxTextWidth = Math.Max(1, maxWidth); formatted.Trimming = TextTrimming.CharacterEllipsis; formatted.MaxLineCount = 1; }
        context.DrawText(formatted, at);
    }

    // ---------------------------------------------------------------- accessibility

    protected override AutomationPeer OnCreateAutomationPeer() => new ElevationPeer(this);

    /// <summary>The band group (§11.6): named by its content, holding the channel points as Buttons.</summary>
    public string GroupName
    {
        get
        {
            if (controller?.Planform is not { } plan || Frame(1) is not { } tip) return ViewName + " view";
            return IsFront
                ? $"Front view of the foil, span {Number(plan.HalfSpanMeters * 2000, 0)} mm, tip height {Number(tip.ElevationMeters * 1000, 1)} mm"
                : $"Side view of the foil, {plan.Stations.Count} authored sections, tip twist {Number(tip.TwistDegrees)}°";
        }
    }

    /// <summary>"Twist, point 4 of 7, control point, from root 180.00 mm, twist −0.84°" (§11.4; "from root" as the Plan and the mockup).</summary>
    public string PointName(PointView captured)
    {
        var point = Live(captured);
        var curve = controller?.CurveFor(point.Curve);
        string channel = ChannelName(point.Curve);
        if (point.AnchorId is { } anchorId && curve?.Points.FirstOrDefault(item => item.Id == anchorId) is { } anchor)
        {
            string toward = point.Index > anchor.Index ? "toward the tip" : "toward the root";
            if (point.Curve == "dihedral")
            {
                double span = point.SpanMeters - anchor.SpanMeters, height = point.Ordinate - anchor.Ordinate;
                double sign = Math.Sign(span) == 0 ? 1 : Math.Sign(span);
                double angle = double.Atan2Pi(height * sign, span * sign) * 180;   // the local dihedral angle, root → tip
                return $"{channel}, point {anchor.Index + 1}, handle {toward}, angle {Number(angle)}°, length {Number(Math.Sqrt(span * span + height * height) * 1000)} mm";
            }
            return $"{channel}, point {anchor.Index + 1}, handle {toward}, from root {Number(point.SpanMeters * 1000)} mm, " +
                $"{ValueName(point.Curve)} {Value(point.Curve, point.Ordinate)}";
        }
        string role = point.Role switch
        {
            PointRole.RootEnd => "root end", PointRole.TipEnd => "tip end", PointRole.Anchor => "anchor point", _ => "control point"
        };
        return $"{channel}, point {point.Index + 1} of {curve?.Points.Count ?? 0}, {role}, from root {Number(point.SpanMeters * 1000)} mm, " +
            $"{ValueName(point.Curve)} {Value(point.Curve, point.Ordinate)}";
    }

    private sealed class ElevationPeer(ElevationView view) : ControlAutomationPeer(view)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => view.GroupName;
        protected override List<AutomationPeer>? GetChildrenCore()
        {
            view.peers = view.CurrentTargets.Select(point => CurvePointLayer.Peer(view, () => view.PointName(point),
                () => view.ScreenPoint(view.Live(point)), () => view.LastValueRequest = $"{point.Curve}:{point.Id}")).ToList();
            return view.peers.Cast<AutomationPeer>().ToList();
        }
    }

    /// <summary>Peers handed out by the last child query; each is told when the model moves under its name.</summary>
    private List<CurvePointLayer.PointPeer> peers = [];

    /// <summary>The point as the model holds it now, by curve and id: a retained peer must not report a pre-drag value (W-1 d).</summary>
    private PointView Live(PointView point) =>
        controller?.CurveFor(point.Curve)?.Points.FirstOrDefault(item => item.Id == point.Id) ?? point;

    /// <summary>Draws over the band: lanes, points, chips, plates and rings. Pointer and keys go to the view.</summary>
    private sealed class Overlay(ElevationView owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            try { owner.RenderOverlay(context); }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // A drawing failure is reported like the band's (shell.pane.render); the band keeps its own banner.
                CfdWorkbench.Desktop.Shell.ShellEvents.Record("shell.pane.render", "error", 0,
                    System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
                    code: "shell.pane.render", pane: owner.IsFront ? "front" : "side", exceptionType: error.GetType().Name);
            }
        }
    }
}
