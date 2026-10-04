using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop;

public enum ModelAreaMode { Views, Section }

public partial class ModelArea : UserControl
{
    /// <summary>Each view of Four views needs at least this much; below it the model area shows One view.</summary>
    public static readonly Size MinimumFourViewSize = new(320, 240);

    // DR-VIEW-1: a view is its slot inside a 1 px frame; the gutter (spacing.view-gutter) separates frames.
    private const double FrameThickness = 1;

    private WorkbenchController? controller;
    private bool foilOpen;

    public ModelAreaMode Mode { get; private set; } = ModelAreaMode.Views;

    public ModelArea()
    {
        InitializeComponent();
        Gutter = ViewArrangementGrid.ColumnSpacing;

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
        (oneViewPicker, backItem, backSeparator) = WireOneViewPicker();
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
                if (controller is null) return;
                // DR-VIEW-10: in One view the plate is the picker; Return and ↓ open it (Space is the Button's own click).
                if (label.Flyout is { } picker)
                {
                    if (args.Key is not (Key.Return or Key.Down)) return;
                    picker.ShowAt(label);
                    args.Handled = true;
                    return;
                }
                if (args.Key != Key.Return) return;
                controller.ToggleOneView(view);
                args.Handled = true;
            }, RoutingStrategies.Tunnel);
        }
        WireNavbar();
        // A view shown by a layout change gets its size only after that layout pass; its first camera fits then.
        ViewArrangementGrid.SizeChanged += (_, _) => Refresh();
        // The caption plate sits right of the axis triad (the approved mockup); View3d owns both positions.
        ThreeDCaptionStack.Margin = View3d.CaptionMargin;
        // The navbar's place follows the model area and its own width (its menus name the layout and display).
        PlanContent.SizeChanged += (_, _) => FitCaption();
        Navbar.SizeChanged += (_, _) => FitCaption();
        ThreeDView.Renderer = ThreeDRenderer;
        // A drag frame redraws only the 3D view (its live camera); the controller takes the camera at release.
        ThreeDView.LiveCameraChanged += () =>
        {
            ThreeDRenderer.Camera = ThreeDView.CurrentCamera;
            if (controller is not null) ThreeDLabel.Content = PlateText(ThreeDTitle(controller));
        };
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
        foilOpen = isOpen;
        Bind();
        Refresh();
    }

    // The gutter is the spacing.view-gutter token the XAML gives the grid; ApplyLayout drops it where a layout has no neighbour.
    private double Gutter { get; }

    /// <summary>The arrangement actually shown: Four views falls back to One view when a view, inside its frame and past the gutter, is under 320 × 240.</summary>
    public ViewLayout EffectiveLayout
    {
        get
        {
            var chosen = controller?.Layout ?? ViewLayout.Plan3d;
            var size = ViewArrangementGrid.Bounds.Size;
            double chrome = 2 * FrameThickness;
            if (chosen.Arrangement == ViewArrangement.Four && size.Width > 0 &&
                ((size.Width - Gutter) / 2 - chrome < MinimumFourViewSize.Width || (size.Height - Gutter) / 2 - chrome < MinimumFourViewSize.Height))
                return ViewLayout.One(controller?.TargetView ?? SingleView.Plan);
            return chosen;
        }
    }

    private IEnumerable<(Button Label, SingleView View)> Labels() =>
    [
        (PlanLabel, SingleView.Plan), (ThreeDLabel, SingleView.ThreeD), (SideLabel, SingleView.Side), (FrontLabel, SingleView.Front)
    ];

    // ---------------- the One-view picker (DR-VIEW-10, NS-7) ----------------
    // In One view the shown view's plate opens Plan / 3D / Side / Front. A choice writes the same controller state the
    // View menu and the label double-click write (Layout, TargetView); there is no second layout implementation.

    // DR-VIEW-11: the first item, set apart by a separator, goes back to the layout chosen before One view.
    private readonly MenuFlyout oneViewPicker;
    private readonly MenuItem backItem;
    private readonly Separator backSeparator;
    private bool oneView;

    private static readonly (SingleView View, string Title)[] PickerRows =
        [(SingleView.Plan, "Plan"), (SingleView.ThreeD, "3D"), (SingleView.Side, "Side"), (SingleView.Front, "Front")];

    private (MenuFlyout Picker, MenuItem Back, Separator Separator) WireOneViewPicker()
    {
        var picker = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, FlyoutPresenterClasses = { "navbar-menu" } };
        var back = new MenuItem { Header = "↩ Back" };
        back.Click += (_, _) => GoBack();
        var separator = new Separator();
        picker.Items.Add(back);
        picker.Items.Add(separator);
        foreach (var (view, title) in PickerRows)
        {
            var item = new MenuItem { Header = title, Tag = view, ToggleType = MenuItemToggleType.Radio, GroupName = "one-view" };
            item.Click += (_, _) => ShowAlone(view);
            picker.Items.Add(item);
        }
        return (picker, back, separator);
    }

    // The plate double-click's toggle: from One view of the shown view back to the arrangement before it.
    private void GoBack()
    {
        oneViewPicker.Hide();
        if (controller is not { Layout.Arrangement: ViewArrangement.One } current) return;
        current.ToggleOneView(current.Layout.Single);
        Labels().Single(item => item.View == ViewCommands.Target(current, null)).Label.Focus();
    }

    // A layout's name is its View ▸ Views row title, so the picker and the menus say the same thing.
    private static string LayoutTitle(ViewArrangement arrangement) =>
        CommandTable.Rows.Single(row => row.Id == (arrangement == ViewArrangement.Four ? "view.layout-four" : "view.layout-plan3d")).Title;

    private void ShowAlone(SingleView view)
    {
        oneViewPicker.Hide();
        if (controller is null) return;
        // The target view is the one a Four-views fallback shows; a chosen One view takes the view as well.
        controller.TargetView = view;
        if (controller.Layout.Arrangement == ViewArrangement.One) controller.Layout = ViewLayout.One(view);
        // Focus follows to the shown plate, so the keyboard can open the picker again (the old plate is hidden).
        Labels().Single(item => item.View == view).Label.Focus();
    }

    /// <summary>A plate's text: in One view it leads with ▾, where trimming never reaches it.</summary>
    private string PlateText(string title) => oneView ? "▾ " + title : title;

    private void Bind()
    {
        var next = PlanCanvas.Controller;
        if (ReferenceEquals(next, controller)) return;
        if (controller is not null)
        {
            controller.Changed -= OnControllerChanged;
            controller.SectionChanged -= OnControllerChanged;
            controller.SurfaceWanted = false;
        }
        controller = next;
        ThreeDView.Controller = controller;
        if (controller is not null)
        {
            controller.Changed += OnControllerChanged;
            controller.SectionChanged += OnControllerChanged;
        }
        SideElevation.Controller = controller;
        FrontElevation.Controller = controller;
    }

    private ShellHost? showHost;

    // §5.2 Show: the strip's Show asks the section editor to frame what a blocker names. The host is found, not passed,
    // as the Properties pane finds it (S-4: ShellHost stays PNL's); the subscription lives exactly as long as the attachment.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        showHost = this.FindAncestorOfType<ShellHost>();
        if (showHost is not null) showHost.SectionShowRequested += FrameSectionBlocker;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (showHost is not null) showHost.SectionShowRequested -= FrameSectionBlocker;
        showHost = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Show: frames a blocker's chord range, or else the point it names, on the section canvas.</summary>
    public void FrameSectionBlocker(PointRef? point, (double X0, double X1)? range)
    {
        if (Mode != ModelAreaMode.Section) return;
        var canvas = SectionModeEditor.ModeCanvas;
        if (point is not null) canvas.SelectedVertex = (point.Curve, point.VertexId);
        if (range is { } at) canvas.FrameRange(at.X0, at.X1);
        else canvas.FitSelection();
        canvas.Focus();
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
        Mode = controller.Section is null ? ModelAreaMode.Views : ModelAreaMode.Section;
        PlanContent.IsVisible = foilOpen && Mode == ModelAreaMode.Views;
        SectionModeEditor.IsVisible = foilOpen && Mode == ModelAreaMode.Section;
        SectionModeEditor.Bind(controller);
        if (Mode == ModelAreaMode.Section)
        {
            controller.SurfaceWanted = false;
            return;
        }
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
        ThreeDRenderer.Camera = ThreeDView.CurrentCamera ?? ThreeDRenderer.Camera;
        ThreeDView.Refresh();
        oneView = layout.Arrangement == ViewArrangement.One;
        if (!oneView) oneViewPicker.Hide();
        foreach (var item in oneViewPicker.Items.OfType<MenuItem>().Where(item => item.Tag is SingleView))
            item.IsChecked = oneView && (SingleView)item.Tag! == layout.Single;
        // A Four-views fallback (no room for four) is not a chosen One view, so it has no layout to go back to.
        backItem.IsVisible = backSeparator.IsVisible = controller.Layout.Arrangement == ViewArrangement.One;
        backItem.Header = "↩ Back to " + LayoutTitle(controller.ArrangementBeforeOne);
        // The plate of the view that Display ▾, zoom and fit act on carries the station underline (the mockup's aria-pressed).
        var target = ViewCommands.Target(controller, null);
        foreach (var (label, view) in Labels())
        {
            string title = view switch
            {
                SingleView.ThreeD => ThreeDTitle(controller),
                SingleView.Side => "Side · from starboard" + suffix,
                SingleView.Front => "Front · looking aft" + suffix,
                _ => "Plan" + (certified ? "" : " · not checked")
            };
            label.Content = PlateText(title);
            label.Flyout = oneView ? oneViewPicker : null;
            label.Classes.Set("target", view == target);
            Avalonia.Automation.AutomationProperties.SetName(label, title + " view label. " +
                (oneView ? "Return or Down Arrow chooses the view to show." : "Double-click or Return shows this view alone."));
        }
        FitLabels();
        RefreshNavbar(controller);
        FitCaption();
    }

    // The 3D caption sits right of the axis triad at the view's bottom; the navbar floats over the views at the bottom
    // centre. The caption stops short of the navbar or, with too little room beside it, rises above it.
    private const double CaptionNavbarGap = 8, MinimumCaptionWidth = 160;

    private void FitCaption()
    {
        var margin = View3d.CaptionMargin;
        double maxWidth = double.PositiveInfinity;
        if (ThreeDSlot.Bounds.Width > 0 && Navbar.IsVisible && Navbar.Bounds.Width > 0 && Navbar.TranslatePoint(default, ThreeDSlot) is { } bar)
        {
            double left = margin.Left, right = ThreeDSlot.Bounds.Width - margin.Right;
            bool besideSlot = bar.Y >= ThreeDSlot.Bounds.Height || bar.Y + Navbar.Bounds.Height <= 0;
            if (!besideSlot && bar.X < right && bar.X + Navbar.Bounds.Width > left)
            {
                if (bar.X - CaptionNavbarGap - left >= MinimumCaptionWidth) maxWidth = bar.X - CaptionNavbarGap - left;
                else margin = new Thickness(margin.Left, margin.Top, margin.Right, ThreeDSlot.Bounds.Height - bar.Y + CaptionNavbarGap);
            }
        }
        ThreeDCaptionStack.MaxWidth = maxWidth;
        ThreeDCaptionStack.Margin = margin;
    }

    // V3D (§11.1): the 3D title and the cube row (Home and the cube, DR-VIEW-9) share the top of the view. The title keeps
    // the room left of the cube row and trims; below View3d.MinimumCubeWidth the cube hides first and the title takes the
    // row. Every other plate stays inside its view.
    private void FitLabels()
    {
        foreach (var (label, view) in Labels())
        {
            if (label.Parent is not Control slot || slot.Bounds.Width <= 0) continue;
            double inset = label.Margin.Left;
            double right = view == SingleView.ThreeD && ThreeDView.CubeVisible ? View3d.CubeRowReserve + inset : inset;
            label.MaxWidth = Math.Max(label.MinWidth, slot.Bounds.Width - inset - right);
        }
    }

    // ---------------- the navbar (§11.7: v10's Fit and Views ▾, plus Display ▾ and Fit Selection) ----------------
    // Each control is a CommandTable row run through the shell's view-command runner, the path the View menu takes
    // (NativeMenuBuilder → ShellHost.RunCommand); its radio state, enablement and reason come from ViewCommands, as the
    // menu's do. No second implementation of any view verb lives here.

    private const string FitId = "view.fit", FitSelectionId = "view.fit-selection";

    private IEnumerable<(Button Button, string Menu)> NavMenus() =>
        [(NavViewsButton, ViewCommands.ViewsMenu), (NavDisplayButton, ViewCommands.DisplayMenu)];

    private static IEnumerable<MenuItem> NavItems(Button button) => ((MenuFlyout)button.Flyout!).Items.OfType<MenuItem>();

    private void WireNavbar()
    {
        foreach (var (button, menu) in NavMenus())
        {
            var items = ((MenuFlyout)button.Flyout!).Items;
            foreach (var row in CommandTable.Rows.Where(row => row.Menu == menu))
            {
                var item = new MenuItem { Header = row.Title, Tag = row.Id, ToggleType = MenuItemToggleType.Radio, GroupName = menu };
                item.Click += (_, _) => RunViewCommand(row.Id);
                items.Add(item);
            }
        }
        NavFitButton.Click += (_, _) => RunViewCommand(FitId);
        NavFitSelectionButton.Click += (_, _) => RunViewCommand(FitSelectionId);
    }

    // The menu bar's call: the shell runs the row, or reports why it cannot in the strip.
    private void RunViewCommand(string id)
    {
        if (this.FindAncestorOfType<ShellHost>() is { } host) _ = host.RunCommand(id);
    }

    private void RefreshNavbar(WorkbenchController controller)
    {
        foreach (var (button, menu) in NavMenus())
        {
            string? chosen = null;
            foreach (var item in NavItems(button))
            {
                var id = (string)item.Tag!;
                item.IsChecked = ViewCommands.IsChecked(id, controller) == true;
                item.IsEnabled = ViewCommands.CanRun(id, controller);
                if (item.IsChecked) chosen = (string?)item.Header;
            }
            button.Content = $"{menu} ▾ {chosen}";
            Avalonia.Automation.AutomationProperties.SetName(button, $"{menu}: {chosen}");
            ShowReason(button, ViewCommands.DisabledReason((string)NavItems(button).First().Tag!, controller));
        }
        ShowReason(NavFitButton, ViewCommands.DisabledReason(FitId, controller));
        ShowReason(NavFitSelectionButton, ViewCommands.DisabledReason(FitSelectionId, controller));

        // A control that cannot run is disabled and says why (UI-DEAD-CONTROL); the style shows the tip while disabled.
        static void ShowReason(Button button, string? reason)
        {
            button.IsEnabled = reason is null;
            ToolTip.SetTip(button, reason);
            Avalonia.Automation.AutomationProperties.SetHelpText(button, reason);
        }
    }

    /// <summary>"3D · Iso", "3D · Free · az 212° · el 24°", with "· wireframe" and the view's state suffixes.</summary>
    private string ThreeDTitle(WorkbenchController controller)
    {
        bool certified = controller.Inspection?.Geometry.Status == GeometryStatus.Certified;
        return "3D · " + (ThreeDView.CurrentCamera?.Title ?? "Iso") +
            (controller.DisplayFor(SingleView.ThreeD) == DisplayMode.Wireframe ? " · wireframe" : "") +
            (certified ? "" : " · not checked") + (controller.SurfaceBehind ? " · Updating…" : "");
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
        // DR-VIEW-3: each elevation fits itself (its band, with room for its chips and plates).
        var fitted = view is SingleView.Side or SingleView.Front
            ? ElevationView.Fitted(view, minimum, maximum, size)
            : ViewCamera.Named(NamedCamera.Iso, minimum, maximum, size);
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
        // One view is a frame with no gutter; a zero-size track would still be charged one.
        grid.ColumnSpacing = one ? 0 : Gutter;
        grid.RowSpacing = four ? Gutter : 0;
        foreach (var (frame, view, row, column) in new (Control, SingleView, int, int)[]
        {
            (PlanFrame, SingleView.Plan, 0, 0), (ThreeDFrame, SingleView.ThreeD, 0, 1),
            (SideFrame, SingleView.Side, 1, 0), (FrontFrame, SingleView.Front, 1, 1)
        })
        {
            frame.IsVisible = layout.Shows(view);
            Grid.SetRow(frame, one ? 0 : row);
            Grid.SetColumn(frame, one ? 0 : column);
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
