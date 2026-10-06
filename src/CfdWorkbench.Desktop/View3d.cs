using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

/// <summary>One face of the view cube that has area from the current camera, in view coordinates.</summary>
public sealed record CubeFace(NamedCamera Camera, string Letter, IReadOnlyList<Point> Corners, Point Centre, double InscribedRadius)
{
    /// <summary>A face is a target only while a 24 px circle fits inside it (M1.2b2 §11.5; WCAG 2.5.8).</summary>
    public bool IsTarget => InscribedRadius >= View3d.FaceTargetDiameter / 2;
}

/// <summary>
/// The 3D view's input and overlay layer (M1.2b2 §5.2, §11.1, §11.3): it lies over the <see cref="SurfaceRenderer"/>
/// in the same cell and turns pointer, trackpad, keyboard and cube input into the one <see cref="ViewCamera"/> verb each
/// names, written to the controller's <see cref="WorkbenchController.Camera3d"/>. It draws the view cube (top-right),
/// the axis triad (bottom-left) and the selected station's chip; the cube's faces and chevrons are real Buttons, so
/// they take focus in Tab order and carry Button peers. Geometry comes only from the controller's mesh; this control
/// projects it through the camera and never evaluates a curve.
/// </summary>
public sealed partial class View3d : Panel
{
    public static readonly StyledProperty<IBrush?> SoftBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(SoftBrush));
    public static readonly StyledProperty<IBrush?> InkBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(InkBrush));
    public static readonly StyledProperty<IBrush?> MuteBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(MuteBrush));
    public static readonly StyledProperty<IBrush?> StationBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(StationBrush));
    public static readonly StyledProperty<IBrush?> FocusBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(FocusBrush));
    public static readonly StyledProperty<IBrush?> ViewportBrushProperty = AvaloniaProperty.Register<View3d, IBrush?>(nameof(ViewportBrush));

    /// <summary>Below this view width the cube hides first; View ▸ Camera reaches every preset (§11.5 overflow).</summary>
    public const double MinimumCubeWidth = 240;
    public const double FaceTargetDiameter = 24;
    public const double ChevronSize = 24;
    /// <summary>The focus ring about a face's centre: 3 px with a 1 px viewport gap each side, inside the 24 px circle.</summary>
    public const double FocusRingRadius = 9;
    public const double OrbitDegreesPerPixel = 0.5;
    public const double OrbitStep = 15, OrbitStepLarge = 90, TiltStepLarge = 45, BracketStep = 5, ChevronStep = 90;
    public const double ZoomStep = 1.2;
    public const double WheelZoomBase = 1.1;
    /// <summary>Two-finger trackpad scroll: view pixels per unit of wheel delta.</summary>
    public const double TrackpadPanPixels = 20;
    /// <summary>⇧+arrows pan by this share of the view (CAD-06; the Plan's ⌥+arrows use the same 10 %).</summary>
    public const double PanFraction = 0.1;
    /// <summary>A click within this many pixels of an authored section's outline selects its station.</summary>
    public const double PickTolerance = 6;
    private const double ClickSlop = 3;

    // The approved mockup (docs/mockups/m12b2-views.html, cube() and triad()): the cube's centre 42 px in from the top
    // and right with a 17 px half edge; the triad on a 62 × 56 plate 10 px in and 92 px up, its axes 20 px long.
    private const double CubeInset = 8, CubeBox = 68, CubeHalfEdge = 17;
    // DR-VIEW-9: Home sits left of the cube box, centred on the cube, with the 4 px gap the arrows keep below the box.
    private const double HomeGap = 4;

    /// <summary>The width the cube row takes from the view's top-right (inset, Home, gap and cube box); the 3D title keeps clear of it.</summary>
    public const double CubeRowReserve = CubeInset + CubeBox + HomeGap + ChevronSize;

    /// <summary>DR-VIEW-9: the Home button's name and tooltip.</summary>
    public const string HomeName = "Home (Iso)";
    private const double TriadLeft = 10, TriadRise = 92, TriadWidth = 62, TriadHeight = 56, TriadAxis = 20;
    private const double OverlayFontSize = 11;

    /// <summary>The 3D view's help text (§11.4): the instructions are its help, never its name.</summary>
    public const string HelpText = "Option arrows orbit. Shift arrows pan here; in the elevations they move a point 1 mm. " +
        "Command equals zooms. F fits the selection. Home returns to Iso.";

    /// <summary>The caption plate starts right of the triad plate, as the mockup draws it.</summary>
    public static readonly Thickness CaptionMargin = new(TriadLeft + TriadWidth + 6, 0, 8, 6);

    private static readonly (NamedCamera Camera, string Letter, Point3 Normal, string Name)[] FaceTable =
    [
        (NamedCamera.Top, "T", new Point3(0, 0, 1), "Top view"), (NamedCamera.Bottom, "B", new Point3(0, 0, -1), "Bottom view"),
        (NamedCamera.Front, "F", new Point3(-1, 0, 0), "Front view"), (NamedCamera.Back, "K", new Point3(1, 0, 0), "Back view"),
        (NamedCamera.Side, "S", new Point3(0, 1, 0), "Side view"), (NamedCamera.Port, "P", new Point3(0, -1, 0), "Port view")
    ];

    // Visual order, left to right, which is also Tab order.
    private static readonly (string Name, double Azimuth, double Elevation, string Glyph)[] ChevronTable =
    [
        ("Orbit left 90°", -ChevronStep, 0, "◀"), ("Orbit up 90°", 0, ChevronStep, "▲"),
        ("Orbit down 90°", 0, -ChevronStep, "▼"), ("Orbit right 90°", ChevronStep, 0, "▶")
    ];

    private readonly Dictionary<NamedCamera, Button> faceButtons = new();
    private readonly Button[] chevronButtons;
    private readonly Button homeButton;
    private readonly Dictionary<Button, Rect> placements = new();
    private IReadOnlyList<CubeFace> faces = [];
    private WorkbenchController? controller;
    private SurfaceRenderer? renderer;
    private Gesture? gesture;
    private bool pointerOverCube;
    private CubeFace? hoveredFace;
    private readonly Overlay overlay;

    public View3d()
    {
        overlay = new Overlay(this) { IsHitTestVisible = false };
        Children.Add(overlay);
        Background = Brushes.Transparent;
        Focusable = true;
        ClipToBounds = true;
        IsTabStop = true;
        AutomationProperties.SetHelpText(this, HelpText);
        AutomationProperties.SetName(this, "3D view");
        foreach (var (camera, _, _, name) in FaceTable)
        {
            var button = CubeButton(name);
            button.Click += (_, _) => ApplyPreset(camera);
            faceButtons[camera] = button;
        }
        chevronButtons = ChevronTable.Select(row =>
        {
            var button = CubeButton(row.Name);
            button.Click += (_, _) => Navigate(Camera?.Orbit(row.Azimuth, row.Elevation), announce: false);
            return button;
        }).ToArray();
        // DR-VIEW-9 (NS-5): Home is always shown beside the cube, so an axis view (one face) always has a way back to Iso.
        homeButton = CubeButton(HomeName);
        ToolTip.SetTip(homeButton, HomeName);
        homeButton.Click += (_, _) => ApplyPreset(NamedCamera.Iso);
        // Faces first (in table order: only target faces are shown), then Home, then the chevrons: Tab order follows the children.
        foreach (var button in CubeButtons)
        {
            button.IsVisible = false;
            button.GotFocus += (_, _) => overlay.InvalidateVisual();
            button.LostFocus += (_, _) => overlay.InvalidateVisual();
            Children.Add(button);
        }
        AddHandler(Gestures.PointerTouchPadGestureMagnifyEvent, OnMagnify);
    }

    public IBrush? SoftBrush { get => GetValue(SoftBrushProperty); set => SetValue(SoftBrushProperty, value); }
    public IBrush? InkBrush { get => GetValue(InkBrushProperty); set => SetValue(InkBrushProperty, value); }
    public IBrush? MuteBrush { get => GetValue(MuteBrushProperty); set => SetValue(MuteBrushProperty, value); }
    public IBrush? StationBrush { get => GetValue(StationBrushProperty); set => SetValue(StationBrushProperty, value); }
    public IBrush? FocusBrush { get => GetValue(FocusBrushProperty); set => SetValue(FocusBrushProperty, value); }
    public IBrush? ViewportBrush { get => GetValue(ViewportBrushProperty); set => SetValue(ViewportBrushProperty, value); }

    /// <summary>The controller whose 3D camera this view navigates; the model area sets it.</summary>
    public WorkbenchController? Controller
    {
        get => controller;
        set
        {
            if (attached && controller is not null) { controller.CameraChanged -= OnCameraChanged; controller.LayersChanged -= OnLayersChanged; }
            controller = value;
            layerView = null;
            seenLayers = null;
            if (attached && controller is not null) { controller.CameraChanged += OnCameraChanged; controller.LayersChanged += OnLayersChanged; }
            Refresh();
        }
    }

    private bool attached;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attached = true;
        if (controller is not null) { controller.CameraChanged += OnCameraChanged; controller.LayersChanged += OnLayersChanged; }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        attached = false;
        if (controller is not null) { controller.CameraChanged -= OnCameraChanged; controller.LayersChanged -= OnLayersChanged; }
    }

    // A camera write (wheel, key, pinch, preset, release) redraws this view alone; the shell's panes do not rebuild.
    private void OnCameraChanged(SingleView view)
    {
        if (view != SingleView.ThreeD) return;
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => OnCameraChanged(view));
            return;
        }
        Refresh();
        LiveCameraChanged?.Invoke();
    }

    private void OnLayersChanged() { layerView = controller?.AnalysisView; seenLayers = controller?.LayerSet; UpdateName(); overlay.InvalidateVisual(); }

    /// <summary>The renderer under this view; its frames feed <c>view.navigate.end</c>.</summary>
    public SurfaceRenderer? Renderer
    {
        get => renderer;
        set
        {
            if (renderer is not null) renderer.FrameRendered -= OnFrameRendered;
            renderer = value;
            if (renderer is not null) renderer.FrameRendered += OnFrameRendered;
        }
    }

    private ViewCamera? Camera => CurrentCamera;

    /// <summary>
    /// The camera the view shows: during a pointer orbit or pan, the live camera of the drag (committed to the
    /// controller once, at release); otherwise the controller's <see cref="WorkbenchController.Camera3d"/>. A drag frame
    /// therefore redraws only the 3D view (<c>Readiness_OrbitFrameP95Under33Ms</c>); a camera write raises
    /// <see cref="WorkbenchController.CameraChanged"/>, never the pane-refreshing <c>Changed</c>.
    /// </summary>
    public ViewCamera? CurrentCamera => gesture?.Live ?? controller?.Camera3d;

    /// <summary>
    /// Raised whenever <see cref="CurrentCamera"/> changes: each drag frame and each controller camera write. The model
    /// area redraws and retitles the view.
    /// </summary>
    public event Action? LiveCameraChanged;
    private SurfaceView? Surface => controller?.Surface;

    public bool CubeVisible => Camera is not null && Bounds.Width >= MinimumCubeWidth;
    public IReadOnlyList<CubeFace> Faces => faces;
    public IReadOnlyList<Button> Chevrons => chevronButtons;

    /// <summary>
    /// The chevrons are drawn while the pointer is over the cube or a cube button has keyboard focus (the approved look
    /// at rest is the bare cube), and always on a straight axis view, where the cube shows one face (DR-VIEW-9); hidden
    /// or shown, they stay Buttons in Tab order.
    /// </summary>
    public bool ChevronsShown => CubeVisible &&
        (pointerOverCube || OnAxisView || CubeButtons.Any(button => button.IsFocused));

    private bool OnAxisView => Camera?.Name is { } named && named != NamedCamera.Iso;

    // Visual and Tab order: the faces, Home, the chevrons.
    private IEnumerable<Button> CubeButtons => faceButtons.Values.Append(homeButton).Concat(chevronButtons);

    /// <summary>The face's Button while the face is a target; null while it has no area, is under 24 px or the cube hides.</summary>
    public Button? FaceButton(NamedCamera face) =>
        faceButtons.TryGetValue(face, out var button) && button.IsVisible ? button : null;

    public Rect TriadBounds => new(TriadLeft, Bounds.Height - TriadRise, TriadWidth, TriadHeight);
    public string? ChipText { get; private set; }
    public Rect? ChipBounds { get; private set; }

    /// <summary>Both halves: the port half is the starboard mesh mirrored in y (FoilDSL §5.2).</summary>
    public static (Point3 Minimum, Point3 Maximum) FullBounds(SurfaceView surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        return (new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ), new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ));
    }

    /// <summary>A named camera fitted to the whole foil, with no transition (HardCut); the preset is announced politely.</summary>
    public void ApplyPreset(NamedCamera name)
    {
        if (Surface is not { } surface || controller is null) return;
        var (minimum, maximum) = FullBounds(surface);
        Navigate(ViewCamera.Named(name, minimum, maximum, Bounds.Size), announce: true);
    }

    /// <summary>The controller changed: re-place the cube, keep focus on a reachable target and name the camera.</summary>
    public void Refresh()
    {
        UpdateCube();
        if (gesture is null) UpdateName();
        overlay.InvalidateVisual();
    }

    private void Navigate(ViewCamera? camera, bool announce)
    {
        if (camera is not { } next || controller is null) return;
        controller.Camera3d = next;   // CameraChanged refreshes this view
        if (announce) this.FindAncestorOfType<ShellHost>()?.Report(new StatusReport("3D view: " + next.Title + "."));
    }

    // ---------------- the cube ----------------

    private static Button CubeButton(string name)
    {
        var button = new Button
        {
            Width = ChevronSize,
            Height = ChevronSize,
            Padding = default,
            FocusAdorner = null,
            Cursor = new Cursor(StandardCursorType.Hand),
            // Drawn by the view (faces, chevrons and the focus ring with its gap); the Button is the target and the peer.
            Template = new FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent })
        };
        AutomationProperties.SetName(button, name);
        return button;
    }

    // On a pixel centre, so the focus ring's 1 px gaps fall on whole pixels.
    private Point CubeCentre => new(Math.Floor(Bounds.Width - CubeInset - CubeBox / 2) + 0.5, CubeInset + CubeBox / 2 + 0.5);
    private Rect CubeRegion => new(Bounds.Width - CubeInset - CubeBox, CubeInset, CubeBox, CubeBox + 4 + ChevronSize);

    private void UpdateCube()
    {
        var focusedBefore = CubeButtons.FirstOrDefault(button => button.IsFocused);
        placements.Clear();
        faces = CubeVisible && Camera is { } camera ? ProjectFaces(camera) : [];
        foreach (var (name, button) in faceButtons)
        {
            var face = faces.FirstOrDefault(item => item.Camera == name);
            button.IsVisible = face is { IsTarget: true };
            if (face is { IsTarget: true })
                placements[button] = new Rect(face.Centre.X - FaceTargetDiameter / 2, face.Centre.Y - FaceTargetDiameter / 2, FaceTargetDiameter, FaceTargetDiameter);
        }
        homeButton.IsVisible = CubeVisible;
        // Whole pixels, centred on the cube: below the 3D title's row, so the title never covers it.
        placements[homeButton] = new Rect(Math.Floor(Bounds.Width - CubeRowReserve), Math.Ceiling(CubeCentre.Y - ChevronSize / 2), ChevronSize, ChevronSize);
        double right = Bounds.Width - CubeInset, top = CubeInset + CubeBox + 4;
        for (int index = 0; index < chevronButtons.Length; index++)
        {
            var button = chevronButtons[index];
            button.IsVisible = CubeVisible;
            double left = right - (chevronButtons.Length - index) * ChevronSize - (chevronButtons.Length - 1 - index) * 2;
            placements[button] = new Rect(left, top, ChevronSize, ChevronSize);
        }
        InvalidateArrange();
        // A focused face that lost its area, or a cube that hid, moves focus to the current face, else to the view.
        if (focusedBefore is not null && !focusedBefore.IsVisible)
        {
            if (Camera?.Name is { } current && FaceButton(current) is { } face) face.Focus(NavigationMethod.Tab);
            else Focus(NavigationMethod.Tab);
        }
    }

    /// <summary>The faces facing the viewer, orthographic, from the camera's own projection of the unit axes.</summary>
    private IReadOnlyList<CubeFace> ProjectFaces(ViewCamera camera)
    {
        var (right, up, toward) = Axes(camera);
        var centre = CubeCentre;
        Point Map(Point3 p) => new(centre.X + ViewCamera.Dot(p, right) * CubeHalfEdge, centre.Y - ViewCamera.Dot(p, up) * CubeHalfEdge);
        var result = new List<CubeFace>();
        foreach (var (name, letter, normal, _) in FaceTable)
        {
            if (ViewCamera.Dot(normal, toward) <= 1e-9) continue;   // edge-on or facing away: no area
            int axis = normal.X != 0 ? 0 : normal.Y != 0 ? 1 : 2;
            int u = axis == 0 ? 1 : 0, v = axis == 2 ? 1 : 2;
            var corners = new List<Point>(4);
            foreach (var (p, q) in new[] { (-1d, -1d), (1d, -1d), (1d, 1d), (-1d, 1d) })
            {
                var c = new double[3];
                c[axis] = axis == 0 ? normal.X : axis == 1 ? normal.Y : normal.Z;
                c[u] = p;
                c[v] = q;
                corners.Add(Map(new Point3(c[0], c[1], c[2])));
            }
            var middle = new Point(corners.Average(point => point.X), corners.Average(point => point.Y));
            result.Add(new CubeFace(name, letter, corners, middle, Inscribed(corners, middle)));
        }
        return result;
    }

    private static double Inscribed(IReadOnlyList<Point> corners, Point centre)
    {
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < corners.Count; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Count];
            double length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            if (length == 0) return 0;
            nearest = Math.Min(nearest, Math.Abs((b.X - a.X) * (centre.Y - a.Y) - (b.Y - a.Y) * (centre.X - a.X)) / length);
        }
        return nearest;
    }

    /// <summary>
    /// The camera's screen right, screen up and toward-viewer unit vectors, read from the camera's own orthographic
    /// projection of the unit axes (a projection of an orthonormal basis onto a plane sums to 2 in squared length), so
    /// the cube and the triad can never disagree with the drawing.
    /// </summary>
    private static (Point3 Right, Point3 Up, Point3 Toward) Axes(ViewCamera camera)
    {
        var flat = camera with { Projection = Projection.Orthographic, Target = default, Distance = 1 };
        var size = new Size(2, 2);
        var origin = flat.Project(default, size);
        var x = flat.Project(new Point3(1, 0, 0), size) - origin;
        var y = flat.Project(new Point3(0, 1, 0), size) - origin;
        var z = flat.Project(new Point3(0, 0, 1), size) - origin;
        double scale = Math.Sqrt((x.X * x.X + x.Y * x.Y + y.X * y.X + y.Y * y.Y + z.X * z.X + z.Y * z.Y) / 2);
        return (new Point3(x.X / scale, y.X / scale, z.X / scale), new Point3(-x.Y / scale, -y.Y / scale, -z.Y / scale),
            flat.TowardViewer(default));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        overlay.Arrange(new Rect(finalSize));
        foreach (var child in Children.OfType<Button>())
            child.Arrange(placements.TryGetValue(child, out var rect) ? rect : default);
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) UpdateCube();
    }

    // ---------------- drawing ----------------

    /// <summary>The overlays, drawn under the cube buttons (which draw nothing themselves).</summary>
    private sealed class Overlay(View3d owner) : Control
    {
        public override void Render(DrawingContext context) => owner.Draw(context);
    }

    private void Draw(DrawingContext context)
    {
        ChipText = null;
        ChipBounds = null;
        if (Camera is not { } camera) return;
        if (controller?.IsAnalysis == true && Surface is { } surface)
            View3dLoadLayer.Draw(context, camera, surface, Bounds.Size, LayerView(),
                InkBrush ?? Brushes.White, MuteBrush ?? Brushes.White, StationBrush ?? Brushes.White, SoftBrush ?? Brushes.Black);
        DrawChip(context, camera);
        DrawTriad(context, camera);
        if (CubeVisible) DrawCube(context, camera);
    }

    private void DrawChip(DrawingContext context, ViewCamera camera)
    {
        if (controller?.Selection is not Selection.Station station || Surface is not { } surface) return;
        var section = surface.Sections.FirstOrDefault(item => item.Eta == station.Eta);
        if (section is null) return;
        string name = station.Eta == 0 ? "Root" : station.Eta == 1 ? "Tip" : "Station " + (station.Index + 1).ToString(CultureInfo.InvariantCulture);
        var text = Text(name, InkBrush, FontWeight.SemiBold);
        // Beside the starboard trailing edge, as the mockup places it, but below the section's drawing so it never
        // covers the 3 px station stroke it names (from the front the section runs right from its trailing edge).
        var trailing = camera.Project(section.Upper[^1], Bounds.Size);
        double bottom = section.Upper.Concat(section.Lower).Max(point => camera.Project(point, Bounds.Size).Y);
        double width = text.Width + 12, height = 18;
        double left = Math.Clamp(trailing.X + 8, CubeInset, Math.Max(CubeInset, Bounds.Width - CubeInset - width));
        double top = Math.Clamp(Math.Max(trailing.Y + 4 - height / 2, bottom + 4), CubeInset, Math.Max(CubeInset, Bounds.Height - CubeInset - height));
        var chip = new Rect(Math.Round(left), Math.Round(top), Math.Ceiling(width), height);   // whole pixels: a crisp 1 px border
        context.FillRectangle(SoftBrush ?? Brushes.Black, chip);
        context.DrawRectangle(new Pen(StationBrush ?? Brushes.White, 1), chip.Deflate(0.5));
        context.DrawText(text, new Point(chip.X + 6, chip.Y + (height - text.Height) / 2));
        ChipText = name;
        ChipBounds = chip;
    }

    private void DrawTriad(DrawingContext context, ViewCamera camera)
    {
        var plate = TriadBounds;
        context.FillRectangle(SoftBrush ?? Brushes.Black, plate);
        var (right, up, _) = Axes(camera);
        var origin = new Point(plate.X + 26, plate.Y + 32);
        var pen = new Pen(InkBrush ?? Brushes.White, 1.5);
        foreach (var (axis, letter) in new[] { (new Point3(1, 0, 0), "x"), (new Point3(0, 1, 0), "y"), (new Point3(0, 0, 1), "z") })
        {
            var tip = new Vector(ViewCamera.Dot(axis, right) * TriadAxis, -ViewCamera.Dot(axis, up) * TriadAxis);
            context.DrawLine(pen, origin, origin + tip);
            var text = Text(letter, InkBrush, FontWeight.Normal);
            context.DrawText(text, origin + tip * 1.25 + new Vector(-3, 4 - text.Baseline));
        }
    }

    private void DrawCube(DrawingContext context, ViewCamera camera)
    {
        var edge = new Pen(MuteBrush ?? Brushes.Gray, 1);
        foreach (var face in faces)
        {
            bool current = camera.Name == face.Camera;
            var geometry = new PolylineGeometry(face.Corners, isFilled: true);
            context.DrawGeometry(current ? StationBrush : SoftBrush, edge, geometry);
            if (ReferenceEquals(face, hoveredFace) && face.IsTarget && !current)
                context.DrawGeometry(null, new Pen(InkBrush ?? Brushes.White, 1.5), geometry);
            var letter = Text(face.Letter, current ? ViewportBrush : InkBrush, FontWeight.SemiBold);
            context.DrawText(letter, face.Centre - new Vector(letter.Width / 2, letter.Height / 2));
        }
        if (ChevronsShown)
            for (int index = 0; index < chevronButtons.Length; index++)
            {
                var rect = placements[chevronButtons[index]];
                context.FillRectangle(SoftBrush ?? Brushes.Black, rect, 3);
                var glyph = Text(ChevronTable[index].Glyph, InkBrush, FontWeight.Normal);
                context.DrawText(glyph, rect.Center - new Vector(glyph.Width / 2, glyph.Height / 2));
            }
        DrawHome(context);
        foreach (var button in CubeButtons)
        {
            if (!button.IsVisible || !button.IsFocused || !placements.TryGetValue(button, out var rect)) continue;
            // DESIGN.md View cube: 3 px focus-ring-viewport with a 1 px viewport gap (it is station-coloured on a station face).
            double radius = ReferenceEquals(button, homeButton) || chevronButtons.Contains(button) ? ChevronSize / 2 + 1 : FocusRingRadius;
            context.DrawEllipse(null, new Pen(ViewportBrush ?? Brushes.Black, 5), rect.Center, radius, radius);
            context.DrawEllipse(null, new Pen(FocusBrush ?? Brushes.White, 3), rect.Center, radius, radius);
        }
    }

    /// <summary>Home on the arrows' plate; its ⌂ is a drawn outline, so it never depends on a font that carries U+2302.</summary>
    private void DrawHome(DrawingContext context)
    {
        var rect = placements[homeButton];
        context.FillRectangle(SoftBrush ?? Brushes.Black, rect, 3);
        var c = rect.Center;
        Point[] house =
        [
            new(c.X - 5, c.Y + 5), new(c.X - 5, c.Y - 1), new(c.X, c.Y - 6), new(c.X + 5, c.Y - 1), new(c.X + 5, c.Y + 5), new(c.X - 5, c.Y + 5)
        ];
        context.DrawGeometry(null, new Pen(InkBrush ?? Brushes.White, 1.5), new PolylineGeometry(house, isFilled: false));
    }

    private static FormattedText Text(string text, IBrush? brush, FontWeight weight) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, weight), OverlayFontSize, brush ?? Brushes.White);

    // ---------------- pointer ----------------

    private enum GestureKind { Click, Orbit, Pan }

    private sealed class Gesture(GestureKind kind, Point start, IPointer pointer)
    {
        public GestureKind Kind { get; set; } = kind;
        public Point Start { get; } = start;
        public Point Last { get; set; } = start;
        public IPointer Pointer { get; } = pointer;
        public bool Moved { get; set; }
        public ViewCamera? Live { get; set; }
        public long Started { get; } = System.Diagnostics.Stopwatch.GetTimestamp();
        public List<double> Frames { get; } = [];
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || Camera is null) return;
        var point = e.GetCurrentPoint(this);
        bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt), shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        GestureKind? kind = point.Properties.IsMiddleButtonPressed ? GestureKind.Pan
            : point.Properties.IsLeftButtonPressed ? alt ? GestureKind.Orbit : shift ? GestureKind.Pan : GestureKind.Click
            : null;
        if (kind is not { } chosen) return;
        gesture = new Gesture(chosen, point.Position, e.Pointer);
        e.Pointer.Capture(this);
        Focus(NavigationMethod.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var at = e.GetPosition(this);
        UpdateHover(at);
        if (gesture is null || !ReferenceEquals(e.Pointer, gesture.Pointer) || Camera is not { } camera) return;
        var delta = at - gesture.Last;
        gesture.Last = at;
        if (Math.Abs(at.X - gesture.Start.X) > ClickSlop || Math.Abs(at.Y - gesture.Start.Y) > ClickSlop) gesture.Moved = true;
        var next = gesture.Kind switch
        {
            GestureKind.Orbit => camera.Orbit(delta.X * OrbitDegreesPerPixel, delta.Y * OrbitDegreesPerPixel),
            GestureKind.Pan => camera.Pan(delta.X, delta.Y, Bounds.Size),
            _ => camera
        };
        if (next != camera)
        {
            gesture.Live = next;
            UpdateCube();
            overlay.InvalidateVisual();
            LiveCameraChanged?.Invoke();
        }
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (gesture is not { } ended || !ReferenceEquals(e.Pointer, ended.Pointer)) return;
        gesture = null;
        e.Pointer.Capture(null);
        e.Handled = true;
        if (ended.Live is { } live && controller is not null) controller.Camera3d = live;
        if (ended.Kind == GestureKind.Click)
        {
            if (!ended.Moved) Pick(e.GetPosition(this));
        }
        else if (ended.Moved) RecordNavigation(ended);
        Refresh();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (gesture is not { } lost || !ReferenceEquals(e.Pointer, lost.Pointer)) return;
        gesture = null;
        if (lost.Live is { } live && controller is not null) controller.Camera3d = live;
        Refresh();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        UpdateHover(null);
    }

    private void UpdateHover(Point? at)
    {
        bool over = at is { } point && CubeVisible && CubeRegion.Contains(point);
        var face = at is { } p && over ? faces.FirstOrDefault(item => item.IsTarget && Contains(item.Corners, p)) : null;
        if (over == pointerOverCube && ReferenceEquals(face, hoveredFace)) return;
        pointerOverCube = over;
        hoveredFace = face;
        overlay.InvalidateVisual();
    }

    /// <summary>
    /// The mouse wheel zooms about the pointer; a two-finger trackpad scroll pans (DR-13, Fusion 360). <c>assume:</c>
    /// Avalonia 11.3.14 reports a trackpad scroll with a horizontal or fractional delta and a wheel notch as a whole
    /// vertical step; confirm: native row N-3D-2 on the operator's trackpad and mouse; if false, a smooth-scrolling
    /// wheel pans instead of zooming (the Plan zooms on every wheel event, design §5.3).
    /// </summary>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Camera is not { } camera) return;
        bool trackpad = e.Delta.X != 0 || e.Delta.Y != Math.Round(e.Delta.Y);
        Navigate(trackpad
            ? camera.Pan(e.Delta.X * TrackpadPanPixels, e.Delta.Y * TrackpadPanPixels, Bounds.Size)
            : camera.ZoomAbout(e.GetPosition(this), Math.Pow(WheelZoomBase, e.Delta.Y), Bounds.Size), announce: false);
        e.Handled = true;
    }

    /// <summary>Trackpad pinch (macOS magnify) zooms about the pointer.</summary>
    private void OnMagnify(object? sender, PointerDeltaEventArgs e)
    {
        if (Camera is not { } camera) return;
        Navigate(camera.ZoomAbout(e.GetPosition(this), 1 + e.Delta.X, Bounds.Size), announce: false);
        e.Handled = true;
    }

    /// <summary>A click on a target face applies its preset; a click on an authored section selects its station.</summary>
    private void Pick(Point at)
    {
        if (faces.FirstOrDefault(face => face.IsTarget && Contains(face.Corners, at)) is { } face)
        {
            ApplyPreset(face.Camera);
            return;
        }
        if (Surface is not { } surface || Camera is not { } camera || controller is null) return;
        PlacedSection? nearest = null;
        double best = PickTolerance;
        foreach (var section in surface.Sections)
        {
            if (section.Assignment is null) continue;
            foreach (bool port in new[] { false, true })
            {
                double distance = Math.Min(Distance(section.Upper, port, camera, at), Distance(section.Lower, port, camera, at));
                if (distance > best) continue;
                best = distance;
                nearest = section;
            }
        }
        // The selection names a station by its index in the authored station list and its exact η (ADR-0009 §4).
        if (nearest is null || controller.Planform?.Stations is not { } stations) return;
        for (int index = 0; index < stations.Count; index++)
            if (stations[index].Eta == nearest.Eta)
            {
                controller.Select(new Selection.Station(index, nearest.Eta));
                return;
            }
    }

    private double Distance(IReadOnlyList<Point3> outline, bool port, ViewCamera camera, Point at)
    {
        double best = double.PositiveInfinity;
        Point? previous = null;
        foreach (var point in outline)
        {
            var screen = camera.Project(port ? point.Port() : point, Bounds.Size);
            if (previous is { } a) best = Math.Min(best, SegmentDistance(a, screen, at));
            previous = screen;
        }
        return best;
    }

    private static double SegmentDistance(Point a, Point b, Point p)
    {
        var ab = b - a;
        double lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
        double t = lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / lengthSquared, 0, 1);
        var nearest = a + ab * t;
        return Math.Sqrt((p.X - nearest.X) * (p.X - nearest.X) + (p.Y - nearest.Y) * (p.Y - nearest.Y));
    }

    private static bool Contains(IReadOnlyList<Point> polygon, Point p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                inside = !inside;
        return inside;
    }

    // ---------------- keyboard ----------------

    /// <summary>
    /// The 3D keymap (§11.3): bound on this control, so the single-character keys act only while the view (or one of
    /// its cube buttons) has focus — never in a text field, the Browser or another view (WCAG 2.1.4). Plain arrows do
    /// nothing here: there is no point to nudge.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || Camera is not { } camera || Surface is not { } surface) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool option = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var size = Bounds.Size;
        var centre = new Point(size.Width / 2, size.Height / 2);
        var (minimum, maximum) = FullBounds(surface);
        ViewCamera? next = (e.Key, command, option) switch
        {
            (Key.Left, false, true) => camera.Orbit(-(shift ? OrbitStepLarge : OrbitStep), 0),
            (Key.Right, false, true) => camera.Orbit(shift ? OrbitStepLarge : OrbitStep, 0),
            (Key.Up, false, true) => camera.Orbit(0, shift ? TiltStepLarge : OrbitStep),
            (Key.Down, false, true) => camera.Orbit(0, -(shift ? TiltStepLarge : OrbitStep)),
            (Key.Left, false, false) when shift => camera.Pan(-PanFraction * size.Width, 0, size),
            (Key.Right, false, false) when shift => camera.Pan(PanFraction * size.Width, 0, size),
            (Key.Up, false, false) when shift => camera.Pan(0, -PanFraction * size.Height, size),
            (Key.Down, false, false) when shift => camera.Pan(0, PanFraction * size.Height, size),
            (Key.OemOpenBrackets, false, false) => camera.Orbit(-BracketStep, 0),
            (Key.OemCloseBrackets, false, false) => camera.Orbit(BracketStep, 0),
            (Key.OemPlus or Key.Add, true, false) => camera.ZoomAbout(centre, ZoomStep, size),
            (Key.OemMinus or Key.Subtract, true, false) => camera.ZoomAbout(centre, 1 / ZoomStep, size),
            (Key.Z, false, false) => camera.ZoomAbout(centre, shift ? ZoomStep : 1 / ZoomStep, size),
            (Key.D0, true, false) => camera.Fit(minimum, maximum, size),
            (Key.F, false, false) when !shift => controller?.FitBounds() is var (low, high) ? camera.Fit(low, high, size) : null,
            (Key.Home, false, false) => ViewCamera.Named(NamedCamera.Iso, minimum, maximum, size),
            _ => null
        };
        if (next is null) return;
        Navigate(next, announce: false);
        e.Handled = true;
    }

    // ---------------- semantics and telemetry ----------------

    /// <summary>The view's name holds only the camera; it changes per key step or at the end of a pointer gesture.</summary>
    private void UpdateName()
    {
        string name = Camera switch
        {
            { Name: { } named } => "3D view, camera " + named,
            { } free => $"3D view, camera Free, azimuth {Degrees(free.AzimuthDegrees)}°, elevation {Degrees(free.ElevationDegrees)}°",
            _ => "3D view"
        };
        name += LayerNameSuffix();
        if (AutomationProperties.GetName(this) != name) AutomationProperties.SetName(this, name);
    }

    private static string Degrees(double value)
    {
        long rounded = (long)Math.Round(value, MidpointRounding.AwayFromZero);
        return rounded < 0 ? "−" + (-rounded).ToString(CultureInfo.InvariantCulture) : rounded.ToString(CultureInfo.InvariantCulture);
    }

    private void OnFrameRendered(double milliseconds) => gesture?.Frames.Add(milliseconds);

    /// <summary><c>view.navigate.end</c>: one per pointer orbit or pan, with its frame count and render p95 (no positions).</summary>
    private static void RecordNavigation(Gesture ended)
    {
        var frames = ended.Frames.Order().ToArray();
        double? p95 = frames.Length == 0 ? null : frames[(int)Math.Ceiling(0.95 * frames.Length) - 1];
        ShellEvents.Record("view.navigate.end", "ok", System.Diagnostics.Stopwatch.GetElapsedTime(ended.Started).TotalMilliseconds,
            System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
            trigger: ended.Kind == GestureKind.Orbit ? "orbit" : "pan", pane: "3d", frames: frames.Length, renderP95Ms: p95);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new View3dPeer(this);

    private sealed class View3dPeer(View3d owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
