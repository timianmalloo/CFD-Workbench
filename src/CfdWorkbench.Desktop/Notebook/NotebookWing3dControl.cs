using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>
/// Interactive 3D wing artifact preview for Tab 4 (<c>3D Wing</c>) of <see cref="NotebookWorkspaceView"/>.
/// Reuses <see cref="SurfaceRenderer"/> and <see cref="ViewCamera"/> directly without coupling to
/// <see cref="WorkbenchController"/> CAD slots (<c>docs/design/ai-guided-design-notebook.md §8</c>,
/// <c>ADR-0010 §Decision 4</c>: displays receive geometry, never compute it).
/// Supports pointer drag orbit, wheel zoom, keyboard orbit (Arrows, Home, W), and Shaded/Wireframe toggle.
/// </summary>
public sealed class NotebookWing3dControl : UserControl
{
    private const double KeyboardOrbitStepDeg = 12.0;
    private const double PointerOrbitDegPerPx = 0.45;

    private readonly SurfaceRenderer renderer;
    private readonly ToggleButton shadedButton;
    private readonly ToggleButton wireframeButton;
    private readonly TextBlock cameraReadout;
    private readonly TextBlock emptyNote;

    private SurfaceView? surface;
    private ViewCamera? camera;
    private DisplayMode displayMode = DisplayMode.Shaded;
    private bool dragging;
    private Point lastPointer;

    public NotebookWing3dControl()
    {
        Focusable = true;
        AutomationProperties.SetName(this, "3D Wing preview — drag or use arrow keys to orbit, Home to reset, W for wireframe");

        renderer = new SurfaceRenderer
        {
            Name = "NotebookWing3dRenderer",
            Pane = "notebook-3d",
            ClipToBounds = true,
            ShowGround = true,
            Display = DisplayMode.Shaded
        };
        BindRendererBrushes(renderer);

        shadedButton = new ToggleButton
        {
            Name = "Wing3dShadedButton",
            Content = "Shaded",
            IsChecked = true
        };
        wireframeButton = new ToggleButton
        {
            Name = "Wing3dWireframeButton",
            Content = "Wireframe",
            IsChecked = false
        };
        var isoButton = new Button { Name = "Wing3dIsoButton", Content = "Iso" };
        var topButton = new Button { Name = "Wing3dTopButton", Content = "Top" };
        var frontButton = new Button { Name = "Wing3dFrontButton", Content = "Front" };
        var sideButton = new Button { Name = "Wing3dSideButton", Content = "Side" };

        shadedButton.Click += (_, _) => SetDisplayMode(DisplayMode.Shaded);
        wireframeButton.Click += (_, _) => SetDisplayMode(DisplayMode.Wireframe);
        isoButton.Click += (_, _) => ApplyPreset(NamedCamera.Iso);
        topButton.Click += (_, _) => ApplyPreset(NamedCamera.Top);
        frontButton.Click += (_, _) => ApplyPreset(NamedCamera.Front);
        sideButton.Click += (_, _) => ApplyPreset(NamedCamera.Side);

        cameraReadout = new TextBlock
        {
            Name = "Wing3dCameraReadout",
            Classes = { "pnl-cell" },
            VerticalAlignment = VerticalAlignment.Center
        };
        cameraReadout.Bind(TextBlock.ForegroundProperty, cameraReadout.GetResourceObservable("PlanMuteBrush"));

        emptyNote = new TextBlock
        {
            Name = "Wing3dEmptyNote",
            Text = "No geometry-checked 3D candidate yet.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = true
        };
        emptyNote.Bind(TextBlock.ForegroundProperty, emptyNote.GetResourceObservable("PlanMuteBrush"));

        var controlsBar = new WrapPanel
        {
            Classes = { "toolbar-wrap" },
            Orientation = Orientation.Horizontal,
            Children = { shadedButton, wireframeButton, isoButton, topButton, frontButton, sideButton }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        Grid.SetColumn(controlsBar, 0);
        Grid.SetColumn(cameraReadout, 1);
        headerGrid.Children.Add(controlsBar);
        headerGrid.Children.Add(cameraReadout);

        var headerBorder = new Border
        {
            Padding = new Thickness(8, 4),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = headerGrid
        };
        headerBorder.Bind(Border.BackgroundProperty, headerBorder.GetResourceObservable("PlanSoftBrush"));
        headerBorder.Bind(Border.BorderBrushProperty, headerBorder.GetResourceObservable("ViewportGridBrush"));

        var viewportPanel = new Panel
        {
            Children = { renderer, emptyNote }
        };

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };
        Grid.SetRow(headerBorder, 0);
        Grid.SetRow(viewportPanel, 1);
        rootGrid.Children.Add(headerBorder);
        rootGrid.Children.Add(viewportPanel);

        var frame = new Border
        {
            BorderThickness = new Thickness(1),
            Child = rootGrid
        };
        frame.Bind(Border.BackgroundProperty, frame.GetResourceObservable("ViewportBrush"));
        frame.Bind(Border.BorderBrushProperty, frame.GetResourceObservable("ViewportGridBrush"));

        Content = frame;

        renderer.SizeChanged += (_, _) =>
        {
            if (surface is not null && camera is null && renderer.Bounds.Width > 10 && renderer.Bounds.Height > 10)
                ApplyPreset(NamedCamera.Iso);
        };
    }

    public SurfaceRenderer Renderer => renderer;
    public SurfaceView? CurrentSurface => surface;
    public ViewCamera? CurrentCamera => camera;
    public DisplayMode DisplayMode => displayMode;

    public void SetSurface(SurfaceView? nextSurface, string? label = null)
    {
        surface = nextSurface;
        renderer.Surface = nextSurface;
        emptyNote.IsVisible = nextSurface is null;
        if (nextSurface is null)
        {
            camera = null;
            renderer.Camera = null;
            cameraReadout.Text = "No 3D mesh";
            return;
        }

        if (camera is null)
        {
            ApplyPreset(NamedCamera.Iso);
        }
        else
        {
            renderer.Camera = camera;
            UpdateReadout(label);
        }
    }

    public void SetDisplayMode(DisplayMode mode)
    {
        displayMode = mode;
        renderer.Display = mode;
        shadedButton.IsChecked = mode == DisplayMode.Shaded;
        wireframeButton.IsChecked = mode == DisplayMode.Wireframe;
        UpdateReadout(null);
    }

    public void ApplyPreset(NamedCamera preset)
    {
        if (surface is null) return;
        var size = EffectiveViewportSize();
        var (min, max) = WholeBounds(surface);
        camera = ViewCamera.Named(preset, min, max, size);
        renderer.Camera = camera;
        UpdateReadout(null);
    }

    public void OrbitBy(double deltaAzimuthDeg, double deltaElevationDeg)
    {
        if (surface is null) return;
        if (camera is null) ApplyPreset(NamedCamera.Iso);
        if (camera is not { } current) return;
        camera = current.Orbit(deltaAzimuthDeg, deltaElevationDeg);
        renderer.Camera = camera;
        UpdateReadout(null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (surface is null) return;
        dragging = true;
        lastPointer = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!dragging || surface is null) return;
        var pos = e.GetPosition(this);
        double dx = pos.X - lastPointer.X;
        double dy = pos.Y - lastPointer.Y;
        lastPointer = pos;
        OrbitBy(-dx * PointerOrbitDegPerPx, dy * PointerOrbitDegPerPx);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!dragging) return;
        dragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (surface is null || camera is not { } current) return;
        double factor = e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15;
        camera = current.ZoomAbout(e.GetPosition(renderer), factor, EffectiveViewportSize());
        renderer.Camera = camera;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (surface is null) return;
        switch (e.Key)
        {
            case Key.Left:
                OrbitBy(-KeyboardOrbitStepDeg, 0);
                e.Handled = true;
                break;
            case Key.Right:
                OrbitBy(KeyboardOrbitStepDeg, 0);
                e.Handled = true;
                break;
            case Key.Up:
                OrbitBy(0, KeyboardOrbitStepDeg);
                e.Handled = true;
                break;
            case Key.Down:
                OrbitBy(0, -KeyboardOrbitStepDeg);
                e.Handled = true;
                break;
            case Key.Home:
                ApplyPreset(NamedCamera.Iso);
                e.Handled = true;
                break;
            case Key.W:
                SetDisplayMode(displayMode == DisplayMode.Shaded ? DisplayMode.Wireframe : DisplayMode.Shaded);
                e.Handled = true;
                break;
        }
    }

    private Size EffectiveViewportSize()
    {
        double w = renderer.Bounds.Width > 10 ? renderer.Bounds.Width : 520;
        double h = renderer.Bounds.Height > 10 ? renderer.Bounds.Height : 300;
        return new Size(w, h);
    }

    private void UpdateReadout(string? prefix)
    {
        if (camera is not { } cam)
        {
            cameraReadout.Text = "No 3D mesh";
            return;
        }
        string mode = displayMode == DisplayMode.Wireframe ? "Wireframe" : "Shaded";
        string baseText = string.Create(CultureInfo.InvariantCulture,
            $"{mode} · Az {cam.AzimuthDegrees:0}° · El {cam.ElevationDegrees:0}°");
        cameraReadout.Text = string.IsNullOrWhiteSpace(prefix) ? baseText : $"{prefix} · {baseText}";
    }

    private static (Point3 Minimum, Point3 Maximum) WholeBounds(SurfaceView s) =>
        (new Point3(s.MinimumX, -s.MaximumY, s.MinimumZ), new Point3(s.MaximumX, s.MaximumY, s.MaximumZ));

    private static void BindRendererBrushes(SurfaceRenderer target)
    {
        target.Bind(SurfaceRenderer.BackgroundBrushProperty, target.GetResourceObservable("ViewportBrush"));
        target.Bind(SurfaceRenderer.GridBrushProperty, target.GetResourceObservable("ViewportGridBrush"));
        target.Bind(SurfaceRenderer.ShadeGrazingBrushProperty, target.GetResourceObservable("PlanSoftBrush"));
        target.Bind(SurfaceRenderer.ShadeLitBrushProperty, target.GetResourceObservable("FoilShadeLitBrush"));
        target.Bind(SurfaceRenderer.FoilBrushProperty, target.GetResourceObservable("PlanFoilBrush"));
        target.Bind(SurfaceRenderer.FoilEdgeBrushProperty, target.GetResourceObservable("FoilEdgeBrush"));
        target.Bind(SurfaceRenderer.StationBrushProperty, target.GetResourceObservable("PlanSelectionBrush"));
        target.Bind(SurfaceRenderer.MuteBrushProperty, target.GetResourceObservable("PlanMuteBrush"));
    }
}
