using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

public partial class ModelArea : UserControl
{
    /// <summary>Each view of Four views needs at least this much; below it the model area shows One view.</summary>
    public static readonly Size MinimumFourViewSize = new(320, 240);

    private WorkbenchController? controller;
    private bool foilOpen;

    public ModelArea()
    {
        InitializeComponent();

        DismissAlertBandButton.Click += (_, _) => AlertBand.IsVisible = false;
        WireToast();
        PlanCanvas.RenderFailed += _ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PlanRenderErrorText.Text = "Plan couldn't render.";
                PlanRenderErrorBand.IsVisible = true;
            }, DispatcherPriority.Background);
        };
        PlanCanvas.RenderRecovered += () => PlanRenderErrorBand.IsVisible = false;
        PlanRenderTryAgainButton.Click += (_, _) => PlanCanvas.RetryRender();

        foreach (var (renderer, band, retry) in new[]
        {
            (ThreeDRenderer, ThreeDRenderErrorBand, ThreeDRenderTryAgainButton),
            (SideRenderer, SideRenderErrorBand, SideRenderTryAgainButton),
            (FrontRenderer, FrontRenderErrorBand, FrontRenderTryAgainButton)
        })
        {
            renderer.RenderFailed += _ => Dispatcher.UIThread.Post(() => band.IsVisible = true, DispatcherPriority.Background);
            renderer.RenderRecovered += () => band.IsVisible = false;
            retry.Click += (_, _) =>
            {
                renderer.RetryRender();
                controller?.RefreshSurface();
            };
        }
        foreach (var (label, view) in Labels())
        {
            label.Click += (_, _) => { if (controller is not null) controller.TargetView = view; };
            label.DoubleTapped += (_, args) =>
            {
                controller?.ToggleOneView(view);
                args.Handled = true;
            };
            label.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (args.Key != Key.Return || controller is null) return;
                controller.ToggleOneView(view);
                args.Handled = true;
            }, RoutingStrategies.Tunnel);
        }
        // A view shown by a layout change gets its size only after that layout pass; its first camera fits then.
        ViewArrangementGrid.SizeChanged += (_, _) => Refresh();
        foreach (var renderer in new[] { ThreeDRenderer, SideRenderer, FrontRenderer })
            renderer.SizeChanged += (_, _) => Refresh();
        // Re-entering a document tab re-attaches the same controller: refresh so the views ask for a mesh again.
        AttachedToVisualTree += (_, _) =>
        {
            Bind();
            Refresh();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (controller is not null) controller.SurfaceWanted = false;
        };
    }

    /// <summary>The controller this area draws; the shell hands it to the Plan canvas, and the area follows it.</summary>
    public WorkbenchController? Controller => controller;

    public void ShowFoilOpen(bool isOpen)
    {
        StartCardView.IsVisible = !isOpen;
        PlanContent.IsVisible = isOpen;
        Plan3DContent.IsVisible = isOpen;
        foilOpen = isOpen;
        Bind();
        Refresh();
    }

    /// <summary>The arrangement actually shown: Four views falls back to One view when a quarter is under 320 × 240.</summary>
    public ViewLayout EffectiveLayout
    {
        get
        {
            var chosen = controller?.Layout ?? ViewLayout.Plan3d;
            var size = ViewArrangementGrid.Bounds.Size;
            if (chosen.Arrangement == ViewArrangement.Four && size.Width > 0 &&
                (size.Width / 2 < MinimumFourViewSize.Width || size.Height / 2 < MinimumFourViewSize.Height))
                return ViewLayout.One(controller?.TargetView ?? SingleView.Plan);
            return chosen;
        }
    }

    private IEnumerable<(Button Label, SingleView View)> Labels() =>
    [
        (PlanLabel, SingleView.Plan), (ThreeDLabel, SingleView.ThreeD), (SideLabel, SingleView.Side), (FrontLabel, SingleView.Front)
    ];

    private void Bind()
    {
        var next = PlanCanvas.Controller;
        if (ReferenceEquals(next, controller)) return;
        if (controller is not null)
        {
            controller.Changed -= OnControllerChanged;
            controller.SurfaceWanted = false;
        }
        controller = next;
        if (controller is not null) controller.Changed += OnControllerChanged;
    }

    private void OnControllerChanged()
    {
        if (Dispatcher.UIThread.CheckAccess()) Refresh();
        else Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Background);
    }

    private bool refreshing;

    private void Refresh()
    {
        // Fitting a first camera writes it back to the controller, which raises Changed into this method again.
        if (controller is null || refreshing) return;
        refreshing = true;
        try { RefreshViews(controller); }
        finally { refreshing = false; }
    }

    private void RefreshViews(WorkbenchController controller)
    {
        var layout = EffectiveLayout;
        ApplyLayout(layout);
        bool attached = TopLevel.GetTopLevel(this) is not null;
        controller.SurfaceWanted = attached && foilOpen &&
            (layout.Shows(SingleView.ThreeD) || layout.Shows(SingleView.Side) || layout.Shows(SingleView.Front));
        if (!foilOpen) return;

        var surface = controller.Surface;
        bool certified = controller.Inspection?.Geometry.Status == GeometryStatus.Certified;
        double? selectedEta = controller.Selection is Selection.Station station ? station.Eta : null;
        string suffix = (certified ? "" : " · not checked") + (controller.SurfaceBehind ? " · Updating…" : "");

        ThreeDDrawing.IsVisible = surface is null;
        ThreeDNote.Text = controller.SurfaceNote;
        ThreeDNote.IsVisible = controller.SurfaceNote is not null;
        foreach (var (renderer, view) in new[] { (ThreeDRenderer, SingleView.ThreeD), (SideRenderer, SingleView.Side), (FrontRenderer, SingleView.Front) })
        {
            renderer.Surface = surface;
            renderer.Display = controller.DisplayFor(view);
            renderer.SelectedEta = selectedEta;
            renderer.Dimmed = !certified;
            renderer.Camera = CameraFor(view, renderer.Bounds.Size, surface);
        }
        ThreeDLabel.Content = "3D · " + (controller.Camera3d?.Title ?? "Iso") + suffix;
        SideLabel.Content = "Side · from starboard" + suffix;
        FrontLabel.Content = "Front · looking aft" + suffix;
        PlanLabel.Content = "Plan" + (certified ? "" : " · not checked");
        foreach (var (label, view) in Labels())
            Avalonia.Automation.AutomationProperties.SetName(label,
                label.Content + " view label. Double-click or Return shows " +
                (layout.Arrangement == ViewArrangement.One ? "every view again." : "this view alone."));
    }

    // The first mesh fits the named camera to the view's own size; later meshes keep the user's camera.
    private ViewCamera? CameraFor(SingleView view, Size size, SurfaceView? surface)
    {
        if (controller is null) return null;
        var current = view == SingleView.ThreeD ? controller.Camera3d : controller.CameraFor(view);
        if (current is not null || surface is null || size.Width <= 0 || size.Height <= 0) return current;
        // Both halves: the port half is the starboard mesh mirrored in y.
        var minimum = new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ);
        var maximum = new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ);
        var fitted = ViewCamera.Named(view switch
        {
            SingleView.Side => NamedCamera.Side,
            SingleView.Front => NamedCamera.Front,
            _ => NamedCamera.Iso
        }, minimum, maximum, size);
        if (view == SingleView.ThreeD) controller.Camera3d = fitted;
        else controller.SetCameraFor(view, fitted);
        return fitted;
    }

    private void ApplyLayout(ViewLayout layout)
    {
        var grid = ViewArrangementGrid;
        bool four = layout.Arrangement == ViewArrangement.Four;
        bool one = layout.Arrangement == ViewArrangement.One;
        grid.ColumnDefinitions[0].Width = new GridLength(four ? 1 : 2, GridUnitType.Star);
        grid.ColumnDefinitions[1].Width = one ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        grid.RowDefinitions[1].Height = four ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        foreach (var (slot, view, row, column) in new (Control, SingleView, int, int)[]
        {
            (PlanSlot, SingleView.Plan, 0, 0), (ThreeDSlot, SingleView.ThreeD, 0, 1),
            (SideSlot, SingleView.Side, 1, 0), (FrontSlot, SingleView.Front, 1, 1)
        })
        {
            slot.IsVisible = layout.Shows(view);
            Grid.SetRow(slot, one ? 0 : row);
            Grid.SetColumn(slot, one ? 0 : column);
        }
    }

    public void ShowAlertBand(string message, bool showAcceptIds = false, bool showResumeRecovery = false)
    {
        AlertBandText.Text = message;
        BandLocateButton.IsVisible = false;
        BandTryAgainButton.IsVisible = false;
        BandOpenAnotherButton.IsVisible = false;
        BandRemoveRecentButton.IsVisible = false;
        AcceptIdsButton.IsVisible = showAcceptIds;
        ResumeRecoveryButton.IsVisible = showResumeRecovery;
        DiscardRecoveryButton.IsVisible = showResumeRecovery;
        DismissAlertBandButton.IsVisible = !showAcceptIds && !showResumeRecovery;
        AlertBand.IsVisible = true;
        if (showAcceptIds) AcceptIdsButton.Focus();
        else if (showResumeRecovery) ResumeRecoveryButton.Focus();
        else DismissAlertBandButton.Focus();
    }

    public void ShowOpenFailure(string fileName, OpenFailure failure, bool fromRecent)
    {
        ShowAlertBand($"“{fileName}” didn't open. {StartView.FailureMessage(failure, fileName)}");
        DismissAlertBandButton.IsVisible = false;
        BandLocateButton.IsVisible = failure is OpenFailure.Missing;
        BandTryAgainButton.IsVisible = failure is OpenFailure.Unreadable;
        BandOpenAnotherButton.IsVisible = failure is not OpenFailure.Newer;
        BandRemoveRecentButton.IsVisible = fromRecent && failure is OpenFailure.Missing;
        Dispatcher.UIThread.Post(() =>
        {
            if (BandLocateButton.IsVisible) BandLocateButton.Focus();
            else if (BandTryAgainButton.IsVisible) BandTryAgainButton.Focus();
            else if (BandOpenAnotherButton.IsVisible) BandOpenAnotherButton.Focus();
        }, DispatcherPriority.Input);
    }

    // ---------------- warning toast (DR-STATUS-1; DESIGN.md §4 Toast; docs/reviews/ui-status-bar.md §2.4) ----------------
    // simplify: one toast slot owned by the model area; upgrade to a shell-level toast host only if a second document type
    // needs warnings.

    private readonly DispatcherTimer toastHold = new();
    private bool toastHovered;

    /// <summary>How long a toast stays (motion.toast-hold, 8000 ms); a check sets it short.</summary>
    public TimeSpan ToastHold
    {
        get => toastHold.Interval;
        set => toastHold.Interval = value;
    }

    public bool ToastOpen => WarningToast.IsVisible;

    /// <summary>Where Esc inside the toast returns focus: the element that had focus before focus entered the toast.</summary>
    public IInputElement? ToastReturnFocus { get; set; }

    private void WireToast()
    {
        ToastHold = TimeSpan.FromMilliseconds(Token("ToastHoldMilliseconds", 8000));
        ToastIcon.Data = Avalonia.Media.Geometry.Parse(Shell.StatusStrip.IconWarning);
        toastHold.Tick += (_, _) => CloseToast();
        ToastDismissButton.Click += (_, _) => CloseToast();
        WarningToast.PointerEntered += (_, _) => { toastHovered = true; UpdateToastHold(); };
        WarningToast.PointerExited += (_, _) => { toastHovered = false; UpdateToastHold(); };
        WarningToast.PropertyChanged += (_, change) =>
        {
            if (change.Property == IsKeyboardFocusWithinProperty) UpdateToastHold();
        };
        WarningToast.AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape) return;
            args.Handled = true;
            CloseToast();
        });
        ModelRoot.SizeChanged += (_, _) => FitToast();
    }

    /// <summary>
    /// Opens the toast, or replaces its text and restarts the hold when one is open (one at a time). It takes no focus:
    /// focus stays in the field or view that made the change.
    /// </summary>
    public void ShowToast(string text)
    {
        if (!WarningToast.IsVisible)
            ToastReturnFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        ToastText.Text = text;
        FitToast();
        WarningToast.IsVisible = true;
        toastHold.Stop();
        UpdateToastHold();
    }

    /// <summary>Closes the toast; with focus inside it, focus returns where it came from.</summary>
    public void CloseToast()
    {
        if (!WarningToast.IsVisible) return;
        bool focusInside = WarningToast.IsKeyboardFocusWithin;
        toastHold.Stop();
        toastHovered = false;
        WarningToast.IsVisible = false;
        if (!focusInside) return;
        if (ToastReturnFocus is Control { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } origin && origin.Focus()) return;
        PlanCanvas.Focus();
    }

    // The hold runs only while the pointer is off the toast and focus is outside it; leaving restarts it in full.
    private void UpdateToastHold()
    {
        bool paused = toastHovered || WarningToast.IsKeyboardFocusWithin;
        if (!WarningToast.IsVisible || paused) toastHold.Stop();
        else if (!toastHold.IsEnabled) toastHold.Start();
    }

    // Width {spacing.toast-w} or the model area less 2 × {spacing.toast-inset}, whichever is smaller.
    private void FitToast()
    {
        double room = ModelRoot.Bounds.Width > 0
            ? ModelRoot.Bounds.Width - WarningToast.Margin.Left - WarningToast.Margin.Right : double.PositiveInfinity;
        WarningToast.Width = Math.Max(0, Math.Min(Token("ToastWidth", 420), room));
    }

    /// <summary>DN-5: the toast's prop type scales with Text size, as the strip's does.</summary>
    public void ApplyTextScale(double scale)
    {
        foreach (var key in new[] { "PropFontSize", "PropLineHeight" })
            WarningToast.Resources[key] = Token(key, 0) * scale;
    }

    private static double Token(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : fallback;

    public void HideAlertBand()
    {
        AlertBand.IsVisible = false;
    }
}
