using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;
using Dock.Avalonia.Controls;
using Dock.Avalonia.Themes.Fluent;
using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;

namespace CfdWorkbench.Desktop.Shell;

public sealed class ShellHost : Grid
{
    public static void InstallTheme(Application application)
    {
        // The shell has no reveal motion. A delayed Dock reveal can leave an invalidated
        // viewport unpainted after returning to its document tab.
        Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration = TimeSpan.Zero;
        application.Styles.Add(new DockFluentTheme());
        application.DataTemplates.Add(new FuncDataTemplate<Document>((document, _) => document.Context as Control));
        application.DataTemplates.Add(new FuncDataTemplate<Tool>((tool, _) => tool.Context as Control));
    }
    public WorkbenchController Controller { get; }
    public PreferenceStore? Preferences { get; }
    public ShellLayoutFactory LayoutFactory { get; }
    public IRootDock LayoutRoot { get; }
    public DockControl DockHost { get; }

    public PropertiesPane Properties { get; }
    public BrowserPane Browser { get; }
    public RailEditorPane RailEditor { get; }
    public ModelArea ModelView { get; }

    public Button LeftSidebarToggle { get; }
    private DocumentTabStrip? sidebarToggleStrip;
    public event Action<IReadOnlyList<RecentEntry>>? RecentLoaded;
    public event Action<string>? PaletteCommand;
    public AutoCompleteBox PaletteSearch { get; }
    public bool PaletteVisible => paletteOverlay.IsVisible;
    public IReadOnlyList<PaletteEntry> PaletteMatches => CommandTable.PaletteEntries()
        .Where(entry => string.IsNullOrWhiteSpace(PaletteSearch.Text) ||
            entry.Title.Contains(PaletteSearch.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
    private readonly Border paletteOverlay;
    private Control? paletteOrigin;
    private CancellationTokenSource? opening;
    private readonly Func<Task<string?>>? pickOpenFile;
    private string? failedPath;

    public static void BindF6(Window window, ShellHost host)
    {
        window.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F6),
            Command = new DelegateCommand(() => host.MoveFocus(false))
        });
        window.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F6, KeyModifiers.Shift),
            Command = new DelegateCommand(() => host.MoveFocus(true))
        });
    }

    public void OpenPalette()
    {
        paletteOrigin = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        PaletteSearch.Text = "";
        paletteOverlay.IsVisible = true;
        PaletteSearch.Focus();
        PaletteSearch.IsDropDownOpen = true;
    }

    private void ClosePalette(bool runSelection)
    {
        string? id = runSelection ? PaletteMatches.FirstOrDefault()?.Id : null;
        paletteOverlay.IsVisible = false;
        paletteOrigin?.Focus();
        if (id is not null) PaletteCommand?.Invoke(id);
    }

    public ShellHost(WorkbenchController controller, PreferenceStore? preferences = null, Func<Task<string?>>? pickOpenFile = null)
    {
        Controller = controller;
        Preferences = preferences;
        this.pickOpenFile = pickOpenFile;

        RowDefinitions = new RowDefinitions("*");
        AttachedToVisualTree += (_, _) =>
        {
            RefreshPanes();
        };
        LeftSidebarToggle = new Button
        {
            Content = "◧",
            [AutomationProperties.NameProperty] = "Toggle left sidebar"
        };
        LeftSidebarToggle.Click += (_, _) => ToggleLeftSidebar();

        // Initialize Dock
        LayoutFactory = new ShellLayoutFactory();
        LayoutRoot = LayoutFactory.CreateLayout();

        Properties = new PropertiesPane();
        Browser = new BrowserPane();
        RailEditor = new RailEditorPane();
        ModelView = new ModelArea();
        ModelView.PlanCanvas.Controller = controller;

        // Assign views to layout tools / documents
        LayoutFactory.PropertiesTool.Context = Properties;
        LayoutFactory.BrowserTool.Context = Browser;
        LayoutFactory.RailControlsTool.Context = RailEditor;

        // A view has one logical parent. Dock owns the only document tab strip.
        var sectionSample = ModelView.SectionSampleBody;
        var foilSource = ModelView.FoilSourceBody;
        var sectionEditor = ModelView.SectionEditor;
        var samples = ModelView.Plan3DContent;
        ModelView.ModelRoot.Children.Remove(samples);
        ModelView.DetachedDocumentBodies.Children.Remove(sectionSample);
        ModelView.DetachedDocumentBodies.Children.Remove(foilSource);
        ModelView.DetachedDocumentBodies.Children.Remove(sectionEditor);
        LayoutFactory.ModelDocument.Context = ModelView;
        LayoutFactory.SamplesDocument.Context = samples;
        LayoutFactory.SectionSampleDocument.Context = sectionSample;
        LayoutFactory.FoilSourceDocument.Context = foilSource;
        LayoutFactory.SectionDocument.Context = sectionEditor;

        DockHost = new DockControl
        {
            Factory = LayoutFactory,
            Layout = LayoutRoot,
            InitializeFactory = true
        };
        DockHost.LayoutUpdated += (_, _) =>
        {
            InstallToolTabMenus();
            LabelToolChrome();
            PlaceToolTabsAtTop();
            PlaceSidebarToggle();
            ApplyDefaultLeftPaneWidth();
        };
        SetRow(DockHost, 0);
        Children.Add(DockHost);

        PaletteSearch = new AutoCompleteBox
        {
            [AutomationProperties.NameProperty] = "Command palette",
            ItemsSource = CommandTable.PaletteEntries().Select(entry => entry.Title).ToArray(),
            MinimumPrefixLength = 0,
            Width = 360
        };
        PaletteSearch.KeyDown += (_, args) =>
        {
            if (args.Key is not (Key.Enter or Key.Escape)) return;
            ClosePalette(args.Key == Key.Enter);
            args.Handled = true;
        };
        paletteOverlay = new Border
        {
            Child = PaletteSearch,
            Padding = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 64, 0, 0),
            IsVisible = false
        };
        paletteOverlay.AttachedToVisualTree += (_, _) =>
        {
            paletteOverlay.Background = this.FindResource("SurfaceBrush") as IBrush;
            paletteOverlay.BorderBrush = this.FindResource("LineBrush") as IBrush;
            paletteOverlay.BorderThickness = new Thickness(1);
        };
        Children.Add(paletteOverlay);

        // Wire Controller updates
        Controller.Changed += OnControllerChanged;
        Controller.SelectionChanged += OnSelectionChanged;

        // Wire StartView actions in ModelView.
        ModelView.StartCardView.StartNewButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.AlertNewFoilButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.StartExampleButton.Click += async (_, _) => await OpenExampleAsync();
        ModelView.StartCardView.StartOpenButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.StartCardView.ClearRecentButton.Click += async (_, _) => await ClearRecentAsync();
        ModelView.StatusTryAgainButton.Click += async (_, _) => await ClearRecentAsync();
        ModelView.StartCardView.RecentRequested += path => _ = OpenFileAsync(path, fromRecent: true,
            origin: ModelView.StartCardView.SelectedRecentControl);
        ModelView.StartCardView.LocateRequested += () => _ = OpenFileInteractiveAsync();
        ModelView.StartCardView.OpenAnotherRequested += () => _ = OpenFileInteractiveAsync();
        ModelView.StartCardView.TryAgainRequested += () =>
        {
            if (failedPath is not null) _ = OpenFileAsync(failedPath);
        };
        ModelView.StartCardView.RemoveRecentRequested += () => _ = RemoveFailedRecentAsync();
        ModelView.StartCardView.AcceptIdsRequested += () => _ = AcceptCandidateAsync();
        ModelView.BandLocateButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.BandOpenAnotherButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.BandTryAgainButton.Click += async (_, _) =>
        {
            if (failedPath is not null) await OpenFileAsync(failedPath);
        };
        ModelView.BandRemoveRecentButton.Click += async (_, _) => await RemoveFailedRecentAsync();
        ModelView.StartCardView.OpenCancelButton.Click += (_, _) => opening?.Cancel();
        ModelView.AcceptIdsButton.Click += async (_, _) => await AcceptCandidateAsync();
        ModelView.ResumeRecoveryButton.Click += (_, _) =>
        {
            Controller.ResumeRecovery();
            ModelView.HideAlertBand();
            RefreshPanes();
        };
        ModelView.DiscardRecoveryButton.Click += (_, _) =>
        {
            Controller.DiscardRecovery();
            ModelView.HideAlertBand();
            RefreshPanes();
        };

        // Initial Bind
        RefreshPanes();
    }

    public async Task OpenExampleAsync()
    {
        ModelView.StartCardView.ShowOpening("example.foil", ModelView.StartCardView.StartExampleButton);
        try
        {
            await Controller.OpenExampleAsync();
            HandleOpenOutcome(new OpenOutcome.Opened("example.foil"), "example.foil");
        }
        catch (Exception)
        {
            ModelView.StartCardView.HideOpening();
            ModelView.StartCardView.ShowAlert("example.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "example.foil"), isMissingFixture: true);
        }
    }

    public async Task OpenNewFoilAsync()
    {
        if (opening is not null) return;
        using var cancellation = new CancellationTokenSource();
        opening = cancellation;
        ModelView.StartCardView.ShowOpening("new foil", ModelView.StartCardView.StartNewButton);
        try
        {
            var outcome = await Controller.NewFoilAsync(cancellation.Token);
            HandleOpenOutcome(outcome, "new foil", recordRecent: false);
        }
        finally { opening = null; }
    }

    public async Task OpenFileAsync(string path, bool fromRecent = false, Control? origin = null)
    {
        if (opening is not null) return;
        using var cancellation = new CancellationTokenSource();
        opening = cancellation;
        ModelView.StartCardView.ShowOpening(System.IO.Path.GetFileName(path), origin);
        try
        {
            var outcome = await Controller.OpenAsync(path, cancellation.Token);
            HandleOpenOutcome(outcome, path, fromRecent: fromRecent);
        }
        finally { opening = null; }
    }

    public async Task OpenFileInteractiveAsync()
    {
        if (pickOpenFile is not null)
        {
            var selected = await pickOpenFile();
            if (selected is not null) await OpenFileAsync(selected, origin: ModelView.StartCardView.StartOpenButton);
            return;
        }
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open FoilDSL or CFD Workbench project",
            AllowMultiple = false
        });
        if (files.Count == 0) return;
        using var file = files[0];
        await OpenFileAsync(file.Path.LocalPath, origin: ModelView.StartCardView.StartOpenButton);
    }

    public void HandleOpenOutcome(OpenOutcome outcome, string path, bool recordRecent = true, bool fromRecent = false)
    {
        string fileName = System.IO.Path.GetFileName(path);
        switch (outcome)
        {
            case OpenOutcome.Opened:
                ModelView.StartCardView.HideOpening();
                ModelView.ShowFoilOpen(true);
                RefreshPanes();
                FocusModelWhenReady();
                if (recordRecent) _ = RecordRecentAsync(path);
                break;

            case OpenOutcome.Failed failed:
                failedPath = path;
                ModelView.StartCardView.HideOpening();
                if (Controller.Inspection is not null)
                {
                    // Foil already open: show in alert band
                    ModelView.ShowOpenFailure(fileName, failed.Failure, fromRecent);
                }
                else
                {
                    ModelView.StartCardView.ShowAlert(fileName, failed.Failure, fromRecent: fromRecent);
                }
                break;

            case OpenOutcome.Cancelled:
                ModelView.StartCardView.CancelOpening();
                ShowStatus("Opening cancelled. Nothing changed.");
                break;

            case OpenOutcome.NeedsIds:
                ModelView.StartCardView.HideOpening();
                if (Controller.Inspection is null)
                {
                    ModelView.ShowFoilOpen(false);
                    ModelView.StartCardView.ShowImportAlert($"“{fileName}” has no control-point IDs.",
                        "CFD Workbench can add them. The file hasn't been changed.", showAcceptIds: true);
                }
                else
                {
                    ModelView.ShowFoilOpen(true);
                    ModelView.ShowAlertBand($"“{fileName}” has no control-point IDs. CFD Workbench can add them. The file hasn't been changed.",
                        showAcceptIds: true);
                }
                break;

            case OpenOutcome.Refused:
                ModelView.StartCardView.HideOpening();
                if (Controller.Inspection is null)
                {
                    ModelView.ShowFoilOpen(false);
                    ModelView.StartCardView.ShowImportAlert($"“{fileName}” couldn't be checked, so it wasn't opened for editing.",
                        "The file hasn't been changed.", showAcceptIds: false);
                }
                else
                {
                    ModelView.ShowFoilOpen(true);
                    ModelView.ShowAlertBand($"“{fileName}” couldn't be checked, so it wasn't opened for editing. The file hasn't been changed.");
                }
                break;
        }
    }

    private async Task AcceptCandidateAsync()
    {
        try
        {
            await Controller.AcceptCandidateAsync();
            ModelView.StartCardView.DismissAlert();
            ModelView.HideAlertBand();
            RefreshPanes();
            FocusModelWhenReady();
        }
        catch (Exception)
        {
            if (Controller.Inspection is null)
                ModelView.StartCardView.ShowImportAlert("The candidate IDs couldn't be accepted.",
                    "The file hasn't been changed.", showAcceptIds: false);
            else
                ModelView.ShowAlertBand("The candidate IDs couldn't be accepted. The file hasn't been changed.");
        }
    }

    private async Task RecordRecentAsync(string path)
    {
        if (Preferences != null)
        {
            await Preferences.UpdateRecentAsync(new RecentOp.Add(path), CancellationToken.None);
            await LoadRecentAsync();
        }
    }

    public async Task ClearRecentAsync()
    {
        if (Preferences is null) return;
        var save = await Preferences.UpdateRecentAsync(new RecentOp.Clear(), CancellationToken.None);
        if (save.Outcome == "Recent list not cleared")
        {
            // simplify: two <reason> values (COPY-147, COPY-148); a held claim and an I/O failure both read as
            // "couldn't be saved". Upgrade trigger: a reason whose recovery differs from Try again.
            string reason = save.Code == "LAYOUT-VERSION"
                ? "it was saved by a newer version of CFD Workbench"
                : "it couldn't be saved";
            ShowStatus($"The recent-files list wasn't cleared: {reason}. The list is unchanged.", offerTryAgain: true);
        }
        else if (ModelView.StatusTryAgainButton.IsVisible)
        {
            ModelView.StatusText.IsVisible = false;
            ModelView.StatusTryAgainButton.IsVisible = false;
        }
        await LoadRecentAsync();
    }

    private void ShowStatus(string message, bool offerTryAgain = false)
    {
        ModelView.ShowStatus(message);
        ModelView.StatusTryAgainButton.IsVisible = offerTryAgain;
    }

    public async Task RemoveFailedRecentAsync()
    {
        if (Preferences is null || failedPath is null) return;
        string path = failedPath;
        // One compare-and-swap write: a failed write leaves the list as it was, and the alert stays.
        var removed = await Preferences.UpdateRecentAsync(new RecentOp.Remove(path), CancellationToken.None);
        await LoadRecentAsync();
        if (removed.Outcome != "saved") return;
        ModelView.StartCardView.DismissAlert();
        ModelView.HideAlertBand();
    }

    public async Task LoadRecentAsync()
    {
        if (Preferences != null)
        {
            var load = await Preferences.LoadRecentAsync(CancellationToken.None);
            var paths = load.Entries.Select(e => e.Path).ToList();
            ModelView.StartCardView.PopulateRecent(paths);
            RecentLoaded?.Invoke(load.Entries);
        }
    }

    private void OnControllerChanged() => RefreshOnUiThread();
    private void OnSelectionChanged() => RefreshOnUiThread();

    private void RefreshOnUiThread()
    {
        if (Dispatcher.UIThread.CheckAccess()) RefreshPanes();
        else Dispatcher.UIThread.Post(RefreshPanes, DispatcherPriority.Background);
    }

    public void RefreshPanes()
    {
        Properties.Bind(Controller);
        Browser.Bind(Controller);
        RailEditor.Bind(Controller);
        ModelView.SectionEditor.Bind(Controller);

        bool foilOpen = Controller.Inspection is not null;
        ModelView.ShowFoilOpen(foilOpen);
        if (foilOpen)
        {
            ModelView.FoilViewport.Frame = Controller.Frame;
            ModelView.ViewportProvenance.Text = Controller.Provenance;
            ModelView.SectionViewport.Frame = Controller.Frame;
            ModelView.SourceText.Text = Controller.AcceptedSource;
        }
    }

    public void ToggleLeftSidebar()
    {
        var dock = LayoutFactory.LeftToolDock;
        if (dock != null)
        {
            bool wasVisible = LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(dock) ?? false;
            if (wasVisible)
            {
                if (dock.IsActive || Properties.IsKeyboardFocusWithin || Browser.IsKeyboardFocusWithin || RailEditor.IsKeyboardFocusWithin)
                {
                    LeftSidebarToggle.Focus();
                }
                LayoutFactory.TopProportionalDock.VisibleDockables?.Remove(dock);
            }
            else
            {
                LayoutFactory.TopProportionalDock.VisibleDockables?.Insert(0, dock);
                (dock.ActiveDockable as Control)?.Focus();
            }
        }
    }

    public bool MoveFocus(bool reverse)
    {
        var browserRow = Browser.FindControl<ListBox>("StationList")?.Items
            .OfType<ListBoxItem>().FirstOrDefault(item => item.IsVisible && item.IsEnabled);
        var documentTab = DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>()
            .FirstOrDefault(item => ReferenceEquals(item.DataContext, LayoutFactory.MainDocumentDock.ActiveDockable)
                && item.IsVisible && item.IsEnabled);
        var leftTarget = (Control?)browserRow ?? LeftSidebarToggle;
        var targets = new[] { leftTarget, (Control?)documentTab };
        int current = leftTarget.IsKeyboardFocusWithin ? 0 : documentTab?.IsKeyboardFocusWithin == true ? 1 : -1;
        var available = targets.Select(target => target is { IsVisible: true, IsEnabled: true }).ToArray();
        for (int attempt = 0; attempt < targets.Length; attempt++)
        {
            int next = FocusRing.NextRegionIndex(current, reverse, available);
            if (next < 0) return false;
            if (targets[next]?.Focus() == true) return true;
            available[next] = false;
            current = next;
        }
        return false;
    }

    public void FocusForPersona(string persona)
    {
        switch (persona)
        {
            case "designer":
                FocusModelWhenReady();
                break;
            case "keyboard":
                if (Controller.Inspection is null) ModelView.StartCardView.StartOpenButton.Focus();
                else FocusControlWhenReady(() => Properties.FindControl<TextBox>("SpanInput"));
                break;
            case "screen-reader":
                LayoutFactory.LeftToolDock.ActiveDockable = LayoutFactory.BrowserTool;
                FocusControlWhenReady(() => Browser.FindControl<ListBox>("StationList")?.Items
                    .OfType<ListBoxItem>().FirstOrDefault());
                break;
            case "dense":
                LayoutFactory.LeftToolDock.ActiveDockable = LayoutFactory.RailControlsTool;
                FocusControlWhenReady(() => RailEditor.FindControl<ListBox>("ControlList")?.Items
                    .OfType<ListBoxItem>().FirstOrDefault());
                break;
            default:
                throw new ArgumentException("Unknown review persona", nameof(persona));
        }
    }

    public void ClosePane(string id)
    {
        var dockable = LayoutFactory.FindDockable(id);
        if (dockable != null)
        {
            var parent = ShellLayoutFactory.FindParentDock(LayoutRoot, dockable);
            if (parent != null)
            {
                parent.VisibleDockables?.Remove(dockable);
                if (parent.VisibleDockables?.Count > 0)
                {
                    parent.ActiveDockable = parent.VisibleDockables[0];
                    FocusDockableTab(parent.ActiveDockable);
                }
                else
                {
                    // Last pane closed in dock
                    LeftSidebarToggle.Focus();
                }
            }
        }
    }

    public void ShowPane(string id)
    {
        var dockable = LayoutFactory.FindDockable(id);
        if (dockable != null)
        {
            var parent = ShellLayoutFactory.FindParentDock(LayoutRoot, dockable) ??
                (dockable is ITool ? LayoutFactory.LeftToolDock : LayoutFactory.MainDocumentDock);
            if (parent != null)
            {
                if (parent.VisibleDockables?.Contains(dockable) != true)
                {
                    parent.VisibleDockables?.Add(dockable);
                }
                parent.ActiveDockable = dockable;
                FocusDockableTab(dockable);
            }
        }
    }

    public void MovePane(string id, string targetDockId)
    {
        var dockable = LayoutFactory.FindDockable(id);
        var targetDock = LayoutFactory.FindDockable(targetDockId) as IDock;
        if (dockable != null && targetDock != null)
        {
            var sourceParent = ShellLayoutFactory.FindParentDock(LayoutRoot, dockable);
            sourceParent?.VisibleDockables?.Remove(dockable);
            targetDock.VisibleDockables?.Add(dockable);
            targetDock.ActiveDockable = dockable;
            FocusDockableTab(dockable);
        }
    }

    private void FocusDockableTab(IDockable dockable, int attempts = 3)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var tab = DockHost.GetVisualDescendants().OfType<Control>()
                .FirstOrDefault(control => control is ToolTabStripItem or DocumentTabStripItem &&
                    ReferenceEquals(control.DataContext, dockable));
            if (tab?.Focus() == true) return;
            if (attempts > 1) FocusDockableTab(dockable, attempts - 1);
            else LeftSidebarToggle.Focus();
        }, DispatcherPriority.Background);
    }

    private void InstallToolTabMenus()
    {
        foreach (var tab in DockHost.GetVisualDescendants().OfType<ToolTabStripItem>())
        {
            if (tab.ContextMenu is not null || tab.DataContext is not IDockable dockable || dockable.Id is not { } id)
                continue;
            var size = new MenuItem { Header = "Size" };
            size.ItemsSource = new[] { "Narrow", "Default", "Wide" }.Select(name =>
            {
                var item = new MenuItem { Header = name };
                item.Click += (_, _) => SetPaneSize(id, name);
                return item;
            }).ToArray();
            var close = new MenuItem { Header = "Close" };
            close.Click += (_, _) => ClosePane(id);
            var menu = new ContextMenu { ItemsSource = new[] { size, close } };
            menu.Closed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                if (tab.IsEffectivelyVisible && ShellLayoutFactory.FindParentDock(LayoutRoot, dockable)?
                    .VisibleDockables?.Contains(dockable) == true)
                    tab.Focus(NavigationMethod.Tab);
            }, DispatcherPriority.Input);
            tab.ContextMenu = menu;
        }
    }

    private void PlaceSidebarToggle()
    {
        var strip = DockHost.GetVisualDescendants().OfType<DocumentTabStrip>().FirstOrDefault();
        if (strip is null || ReferenceEquals(strip, sidebarToggleStrip)) return;
        if (sidebarToggleStrip is not null) sidebarToggleStrip.RightContent = null;
        strip.RightContent = LeftSidebarToggle;
        sidebarToggleStrip = strip;
    }

    private void LabelToolChrome()
    {
        foreach (var dock in DockHost.GetVisualDescendants().OfType<ToolDockControl>())
        {
            foreach (var button in dock.GetVisualDescendants().OfType<Button>())
            {
                switch (button.Name)
                {
                    case "PART_MenuButton" when button.Content is not string:
                        button.Content = "⋯";
                        AutomationProperties.SetName(button, "Pane menu");
                        break;
                    case "PART_CloseButton" when button.Content is not string:
                        button.Content = "×";
                        AutomationProperties.SetName(button, "Close left side bar");
                        break;
                }
            }
        }
    }

    // Dock's ToolControl template docks its tab strip at the bottom (Dock.Avalonia.Themes.Fluent
    // Controls/ToolControl.axaml:52, DockPanel.Dock="Bottom"). The design puts the one tab row at the top, so the
    // strip gets a local value. Only the dock position changes: pane content and its brushes are untouched.
    private void PlaceToolTabsAtTop()
    {
        foreach (var strip in DockHost.GetVisualDescendants().OfType<ToolTabStrip>())
            if (strip.TemplatedParent is ToolControl && DockPanel.GetDock(strip) != Avalonia.Controls.Dock.Top)
                DockPanel.SetDock(strip, Avalonia.Controls.Dock.Top);
    }

    // The left side bar opens at 260 DIP whatever the window width (docs/mockups/workbench-v10.html, SIZES.left.Default).
    public const double DefaultLeftPaneWidth = 260;
    private bool leftPaneWidthApplied;

    // Dock sizes a ProportionalDock child by the ProportionalStackPanel.Proportion attached property on its item
    // presenter. The first layout pass writes that property as a local value (ProportionManager.ApplyProportions →
    // ProportionalStackPanel.SetProportion, Internal/ProportionManager.cs:118 and ProportionalStackPanel.cs:57), which
    // shadows the style binding to the model (ProportionalDockControl.axaml:27-28). A later model Proportion change
    // therefore never reaches the panel. So the default is written where Dock itself writes it — on the realized
    // presenters, once the panel's width is known — and the two-way binding carries it back to the model.
    private void ApplyDefaultLeftPaneWidth()
    {
        if (leftPaneWidthApplied) return;
        var panel = DockHost.GetVisualDescendants().OfType<Dock.Controls.ProportionalStackPanel.ProportionalStackPanel>()
            .FirstOrDefault(candidate => candidate.Children.Any(child => ReferenceEquals(child.DataContext, LayoutFactory.LeftToolDock)));
        if (panel is null || panel.Bounds.Width <= 0) return;
        double splitters = panel.Children.Where(child => child.DataContext is IProportionalDockSplitter).Sum(child => child.Bounds.Width);
        double left = WorkspacePresets.ProportionFor(DefaultLeftPaneWidth, panel.Bounds.Width - splitters);
        foreach (var child in panel.Children)
        {
            if (ReferenceEquals(child.DataContext, LayoutFactory.LeftToolDock))
                Dock.Controls.ProportionalStackPanel.ProportionalStackPanel.SetProportion(child, left);
            else if (ReferenceEquals(child.DataContext, LayoutFactory.MainDocumentDock))
                Dock.Controls.ProportionalStackPanel.ProportionalStackPanel.SetProportion(child, 1 - left);
        }
        leftPaneWidthApplied = true;
    }

    // The production launch path shows the Start card with nothing focused (UX-28). Focus its first card by keyboard
    // navigation, so the focus ring shows. The card sits in the model document's DeferredContentControl, which realizes
    // content in a Background-priority dispatcher batch (DeferredContentControl.cs:740, :959), after any Input-priority
    // retry. So focus waits for the card's own Loaded event rather than counting attempts.
    public void FocusStartWhenReady()
    {
        var first = ModelView.StartCardView.StartNewButton;
        void FocusFirst()
        {
            // Never take focus the user already placed before the card loaded.
            var focused = TopLevel.GetTopLevel(first)?.FocusManager?.GetFocusedElement();
            if (focused is not null && focused is not TopLevel) return;
            if (Controller.Inspection is null && first.IsEffectivelyVisible) first.Focus(NavigationMethod.Tab);
        }
        if (first.IsLoaded)
        {
            Dispatcher.UIThread.Post(FocusFirst, DispatcherPriority.Input);
            return;
        }
        void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
        {
            first.Loaded -= OnLoaded;
            Dispatcher.UIThread.Post(FocusFirst, DispatcherPriority.Input);
        }
        first.Loaded += OnLoaded;
    }

    private void FocusModelWhenReady(int attempts = 3)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ModelView.FoilViewport.Focus()) return;
            if (attempts > 1) FocusModelWhenReady(attempts - 1);
            else LeftSidebarToggle.Focus();
        }, DispatcherPriority.Background);
    }

    private void FocusControlWhenReady(Func<Control?> target, int attempts = 3)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (target()?.Focus() == true) return;
            if (attempts > 1) FocusControlWhenReady(target, attempts - 1);
            else LeftSidebarToggle.Focus();
        }, DispatcherPriority.Background);
    }

    public void SetPaneSize(string id, string sizeName)
    {
        var dockable = LayoutFactory.FindDockable(id);
        var toolDock = ShellLayoutFactory.FindParentDock(LayoutRoot, dockable!) as IToolDock;
        if (dockable is not null && toolDock is not null)
        {
            toolDock.Proportion = sizeName switch
            {
                "Narrow" => 0.15,
                "Wide" => 0.35,
                _ => 0.25
            };
            toolDock.ActiveDockable = dockable;
            FocusDockableTab(dockable);
        }
    }

    public bool RouteEditVerb(string verb, IInputElement? keyWindowFocus)
    {
        var route = EditVerbRouter.Route(verb, keyWindowFocus);
        switch (route)
        {
            case EditVerbTarget.ToText:
                if (keyWindowFocus is TextBox tb)
                {
                    switch (verb.ToLowerInvariant())
                    {
                        case "undo": tb.Undo(); break;
                        case "redo": tb.Redo(); break;
                        case "cut": tb.Cut(); break;
                        case "copy": tb.Copy(); break;
                        case "paste": tb.Paste(); break;
                        case "selectall":
                        case "select-all": tb.SelectAll(); break;
                    }
                    return true;
                }
                return false;

            case EditVerbTarget.ToDocument:
                switch (verb.ToLowerInvariant())
                {
                    case "undo":
                        if (Controller.CanUndo) { Controller.Undo(); return true; }
                        break;
                    case "redo":
                        if (Controller.CanRedo) { Controller.Redo(); return true; }
                        break;
                }
                return false;

            default:
                return false;
        }
    }
}
