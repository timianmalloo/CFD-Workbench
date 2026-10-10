using System.Runtime.CompilerServices;
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
using CfdWorkbench.Desktop.Analysis;
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
        application.DataTemplates.Add(new FuncDataTemplate<Document>((document, _) => Rehost(document.Context as Control)));
        application.DataTemplates.Add(new FuncDataTemplate<Tool>((tool, _) => Rehost(tool.Context as Control)));
    }

    /// <summary>
    /// A dock removed from the layout and shown again (the left side bar's toggle; a workspace that hides it) gets a new view,
    /// while its pane, one instance per shell, is still the child of the removed view's presenter. A control has one parent,
    /// so the new view stayed blank (the 2026-10-04 blank Properties). A pane is released from a presenter no longer on screen.
    /// </summary>
    private static Control? Rehost(Control? pane)
    {
        if (pane?.GetVisualParent() is Avalonia.Controls.Presenters.ContentPresenter old && old.GetVisualRoot() is null)
            old.Content = null;
        return pane;
    }
    public WorkbenchController Controller { get; }
    public PreferenceStore? Preferences { get; }
    public ShellLayoutFactory LayoutFactory { get; }
    public IRootDock LayoutRoot { get; }
    public DockControl DockHost { get; }

    public PropertiesPane Properties { get; }
    public BrowserPane Browser { get; }
    /// <summary>The Points pane (§11.4); its home is the right side bar (OD-3 B).</summary>
    public PointsPane Points { get; }
    /// <summary>The Layers pane (A3a G-T6): a left-dock tab after Browser.</summary>
    public LayersPane Layers { get; }
    /// <summary>
    /// The Analysis bottom panel (G-T7): a shell slot between the dock and the status strip, shown in Analysis only. It is not a
    /// layout pane (no <see cref="LayoutCodec.Homes"/> row), so no saved layout can place, float or close it.
    /// </summary>
    public AnalysisPanel AnalysisPanel { get; }

    /// <summary>⌘J: the user folded the bottom panel away; it stays folded until ⌘J again, whatever the area mode.</summary>
    private bool bottomFolded;

    /// <summary>The bottom panel is on screen: Analysis, and not folded.</summary>
    public bool BottomPanelShown => AnalysisPanel.IsVisible;

    /// <summary>The workspace last applied (⌘1 / ⌘2 / ⌘3); Planform at start (§11.8, no memory: simplify).</summary>
    public WorkspaceId Workspace { get; private set; } = WorkspaceId.Planform;
    public ModelArea ModelView { get; }

    /// <summary>The status strip under every pane and dock (DR-STATUS-1): the window's one polite status region.</summary>
    public StatusStrip StatusStrip { get; }

    // STATUS-CLOBBER at the strip: the controller status version the strip last reflected. A controller status is shown only
    // when its version moved past this one, and every report from outside the controller moves both (Report).
    private long shownStatusVersion;
    private GestureState lastGesture = GestureState.Idle;

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

    /// <summary>DLG shows the catalog. Null runs the command as a status count and draws no dialog.</summary>
    public Func<Task>? ShowCatalogDialog { get; set; }

    /// <summary>DLG asks for the My-sections name. Null reports the empty-name sentence and writes nothing.</summary>
    public Func<Task<string?>>? AskSaveName { get; set; }

    /// <summary>The save sheet owns its attempt and returns the name only after a successful publish.</summary>
    public Func<Task<string?>>? ShowSaveDialog { get; set; }
    private string? failedPath;

    /// <summary>The in-window menu bar (ADR-0009 S1): null on macOS, where the system menu bar shows the same table.</summary>
    public NativeMenuBar? MenuBar { get; private set; }

    /// <summary>The bar's F10 / Escape handler (WFX2): set with the bar, null on macOS.</summary>
    public MenuBarKeys? MenuKeys { get; internal set; }

    /// <summary>
    /// Adds the menu bar above the dock: a new first row, so the dock, bottom panel and status strip move down one.
    /// The bar renders the window's <see cref="NativeMenu"/>, so there is no second menu definition. Idempotent.
    /// </summary>
    public NativeMenuBar ShowMenuBar()
    {
        if (MenuBar is not null) return MenuBar;
        RowDefinitions.Insert(0, new RowDefinition(GridLength.Auto));
        foreach (Control child in new Control[] { DockHost, AnalysisPanel, StatusStrip }) SetRow(child, GetRow(child) + 1);
        MenuBar = new NativeMenuBar { [AutomationProperties.NameProperty] = "Menu bar" };
        SetRow(MenuBar, 0);
        Children.Add(MenuBar);
        return MenuBar;
    }

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
        if (id is null) return;
        if (IsShellCommand(id) || CommandTable.UnitsOf(id) is not null) _ = RunCommand(id);
        else PaletteCommand?.Invoke(id);
    }

    public ShellHost(WorkbenchController controller, PreferenceStore? preferences = null, Func<Task<string?>>? pickOpenFile = null)
    {
        Controller = controller;
        Controller.PreviewChanged += OnCatalogPreview;
        Preferences = preferences;
        this.pickOpenFile = pickOpenFile;

        RowDefinitions = new RowDefinitions("*,Auto,Auto");
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
        Points = new PointsPane { Name = "PointsPane" };
        Layers = new LayersPane { Name = "LayersPane" };
        AnalysisPanel = new AnalysisPanel { Name = "AnalysisPanel", IsVisible = false };
        ModelView = new ModelArea();
        StatusStrip = new StatusStrip { Name = "StatusStrip" };
        // The strip starts empty (DR-STATUS-4): the controller's opening prompt is the Start view's to show.
        shownStatusVersion = controller.StatusVersion;
        // PG-26 / DR-STATUS-1: the pane's reports (type, kind, nudge value, echoes, "Selected …", availability) go to the
        // strip; a field error stays on the field's own assertive message and never reaches it.
        Properties.Reported += Report;
        Points.Reported += Report;
        Layers.LayerVisibilityChanged += OnLayerVisibilityChanged;
        Properties.EditSectionRequested += () => _ = EnterSectionAsync(EntryOrigin.Properties);
        Properties.RebuildRequested += curve => ModelView.BeginRebuild(curve, ModelView.PlanCanvas);
        Properties.SectionStepRequested += ApplySectionStepAsync;
        Properties.DockDiagnostic = DiagnoseDock;
        ModelView.PlanCanvas.Controller = controller;
        // DR-NAV-1: Tab from a selected Plan point lands on the Properties pane's first value.
        ModelView.PlanCanvas.TabOut = Properties.FocusFirstValue;
        // AM-RC-4: with the comb plate shown, the plan's Tab goes to the plate (ModelArea wires TabToPlate) and its last control to Properties.
        ModelView.CombPlate.TabOut = Properties.FocusFirstValue;
        AddHandler(InputElement.KeyDownEvent, OnShellKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // Assign views to layout tools / documents
        LayoutFactory.PropertiesTool.Context = Properties;
        LayoutFactory.BrowserTool.Context = Browser;
        LayoutFactory.RailControlsTool.Context = null;
        LayoutFactory.PointsTool.Context = Points;
        LayoutFactory.LayersTool.Context = Layers;

        // A view has one logical parent. Dock owns the only document tab strip.
        var sectionDocument = ModelView.SectionDocumentBody;
        var foilSource = ModelView.FoilSourceBody;
        // Ruling 124: the Section document hosts the Analysis panel's Section view at full size; the bottom tab keeps one line
        // and its action opens this document. (The section editor is the model area's Section mode, ModelArea.Mode, EDT.)
        ModelView.SectionDocumentHost.Content = AnalysisPanel.SectionView;
        AnalysisPanel.OpenSectionRequested += OpenSectionDocument;
        LayoutFactory.ActiveDockableChanged += (_, change) =>
        {
            if (change.Dockable is not Dock.Model.Controls.IDocument) return;
            bool section = ReferenceEquals(change.Dockable, LayoutFactory.SectionDocument);
            ModelView.PlaceBand(section);
            if (section) BindAnalysisPanel();
        };
        ModelView.DetachedDocumentBodies.Children.Remove(sectionDocument);
        ModelView.DetachedDocumentBodies.Children.Remove(foilSource);
        LayoutFactory.ModelDocument.Context = ModelView;
        LayoutFactory.SectionDocument.Context = sectionDocument;
        LayoutFactory.FoilSourceDocument.Context = foilSource;

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
        SetRow(AnalysisPanel, 1);
        Children.Add(AnalysisPanel);
        SetRow(StatusStrip, 2);
        Children.Add(StatusStrip);

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
        Controller.SectionChanged += OnSectionChanged;

        // Wire StartView actions in ModelView.
        ModelView.StartCardView.StartNewButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.AlertNewFoilButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.StartExampleButton.Click += async (_, _) => await OpenExampleAsync();
        ModelView.StartCardView.StartOpenButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.StartCardView.ClearRecentButton.Click += async (_, _) => await ClearRecentAsync();
        StatusStrip.TryAgain = () => _ = ClearRecentAsync();
        // Ruling 115: the status-bar item toggles the units; the controller's event refreshes every surface that shows them.
        StatusStrip.UnitsButton.Click += (_, _) =>
            Controller.AnalysisUnits = Controller.AnalysisUnits == CfdWorkbench.Analysis.Units.Metric ? CfdWorkbench.Analysis.Units.Imperial : CfdWorkbench.Analysis.Units.Metric;
        Controller.UnitsChanged += OnUnitsChanged;
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
        if (Preferences is not null) TextSizeLoaded = LoadTextSizeAsync(Preferences);
        if (Preferences is not null) UnitsLoaded = LoadUnitsAsync(Preferences);
    }

    /// <summary>The startup read of the persisted display units (Ruling 121); completes once the value is applied.</summary>
    public Task UnitsLoaded { get; private set; } = Task.CompletedTask;

    /// <summary>The latest units write; completed when there is none.</summary>
    public Task UnitsSaved { get; private set; } = Task.CompletedTask;

    private bool unitsChosen;
    private bool applyingLoadedUnits;

    /// <summary>A units change from any control is kept per user. A save that cannot be kept is silent: no wording is approved for it.</summary>
    private void OnUnitsChanged()
    {
        RefreshPanes();
        if (applyingLoadedUnits) return;
        unitsChosen = true;
        if (Preferences is null) return;
        string value = Controller.AnalysisUnits == CfdWorkbench.Analysis.Units.Imperial ? DisplayPreferences.Imperial : DisplayPreferences.Metric;
        UnitsSaved = SaveUnitsAsync(Preferences, value);
    }

    /// <summary>
    /// Applies the persisted units at startup: no write back. A choice made before the read finishes wins over it. The read of
    /// the shared display file is recorded once, as <c>display.load</c>, by <see cref="LoadTextSizeAsync"/>.
    /// </summary>
    private async Task LoadUnitsAsync(PreferenceStore preferences)
    {
        var load = await preferences.LoadUnitsAsync(CancellationToken.None);
        await OnUiThread(() =>
        {
            if (unitsChosen) return;
            applyingLoadedUnits = true;
            try { Controller.AnalysisUnits = load.Units == DisplayPreferences.Imperial ? CfdWorkbench.Analysis.Units.Imperial : CfdWorkbench.Analysis.Units.Metric; }
            finally { applyingLoadedUnits = false; }
        });
    }

    private async Task SaveUnitsAsync(PreferenceStore preferences, string units)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        var save = await preferences.SaveUnitsAsync(units, CancellationToken.None);
        ShellEvents.Record("display.save", save.Outcome, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            Guid.NewGuid().ToString("N"), code: save.Code, publicationKnown: save.PublicationKnown,
            durabilityConfirmed: save.DurabilityConfirmed, retried: save.Retried);
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
                Report(new StatusReport("Opening cancelled. Nothing changed."));
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
            Report(new StatusReport($"The recent-files list wasn't cleared: {reason}. The list is unchanged.", ReportKind.Error),
                offerTryAgain: true);
        }
        else if (StatusStrip.StatusTryAgainButton.IsVisible)
            StatusStrip.Clear();
        await LoadRecentAsync();
    }

    /// <summary>
    /// The one sink for reports from outside the controller (docs/reviews/ui-status-bar.md §2.3): the strip shows it and,
    /// for a commit warning, the toast opens. STATUS-CLOBBER: the report supersedes every controller status written so far,
    /// so neither a refresh nor a background completion that started earlier replaces it.
    /// </summary>
    public void Report(StatusReport report) => Report(report, (StripAction?)null);

    private void Report(StatusReport report, bool offerTryAgain) =>
        Report(report, offerTryAgain ? new StripAction("Try again", () => _ = ClearRecentAsync()) : null);

    /// <summary>A report with the strip's one action (Try again or Show, design §5.2).</summary>
    public void Report(StatusReport report, StripAction? action)
    {
        shownStatusVersion = Controller.SupersedeStatus();
        StatusStrip.Show(report, action);
        if (report.Toast) ModelView.ShowToast(report.Text);
    }

    // The controller's own reports reach the strip only when its status was written since the strip last reflected it, so a
    // refresh never re-shows an older controller status over a newer report.
    private void ReportControllerStatus()
    {
        // One read of text, kind and version: a write on another thread cannot pair a newer version with older text.
        var (text, kind, version) = Controller.StatusSnapshot();
        if (version <= shownStatusVersion) return;
        shownStatusVersion = version;
        // In the section mode the strip carries the step's own report; the assessment's placeholder and its crossing
        // transitions are drawn from the mode by OnSectionChanged, so COPY-124 is shown once, on a real clearing.
        if (Controller.Section is not null && text is SectionChecking or SectionCrossing or SectionCleared) return;
        if (!string.IsNullOrWhiteSpace(text)) StatusStrip.Show(new StatusReport(text, kind));
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

    private void RefreshOnUiThread([CallerMemberName] string caller = "")
    {
        if (Dispatcher.UIThread.CheckAccess()) RefreshChangedPanes();
        else Dispatcher.UIThread.Post(RefreshChangedPanes, DispatcherPriority.Background);
    }

    /// <summary>How many times the panes were rebuilt (≈ 25 ms each); a camera-only change must leave it unchanged.</summary>
    public long PaneRefreshes { get; private set; }

    /// <summary>How many times a refresh bound the Properties pane, the shell's one costly pane (~100 ms a bind under load).</summary>
    public long PropertiesBinds { get; private set; }

    // release-freeze (2026-10-04), measured per pane at load 85: Properties.Bind ~100 ms, Browser ~1 ms, Points ~0.6 ms, the
    // section editor ~0 ms (its strip has its own input key), the tail ~0.3 ms. In the section mode a controller change
    // rebinds Properties (and re-reads the accepted source, a session read) only when an input Properties reads changed;
    // an assessment landing changes none. Browser, Points and the editor always rebind: measured negligible, and the
    // editor follows the assessment. Outside the mode every refresh stays full, because there Properties also reads the
    // session's recovery state (HasRecovery), which no cheap key sees. An explicit RefreshPanes() is always full.
    // A3a: the key also holds the Analysis view (reference identity), because a section draft hidden by the toggle keeps the
    // mode open, and an Evaluate landing there changes only that view; without it the Properties groups never repaint.
    private SectionPaneInputs? sectionPaneInputs;

    private readonly record struct SectionPaneInputs(Selection Selection, object? Inspection, object? Draft, object SectionDraft,
        object? Estimates, bool Busy, object? Analysis)
    {
        public static SectionPaneInputs? Of(WorkbenchController controller) => controller.Section is { } mode
            ? new(controller.Selection, controller.Inspection, controller.Draft, mode.Draft, controller.Estimates,
                controller.Gesture == GestureState.Busy, controller.IsAnalysis && controller.Inspection is not null ? controller.AnalysisView : null)
            : null;

        // Reference identity for the documents (a new step, estimate or acceptance is a new object); the selection by value,
        // because the controller rebuilds an equal point selection when it reconciles.
        public bool Same(SectionPaneInputs other) =>
            ReferenceEquals(Inspection, other.Inspection) && ReferenceEquals(Draft, other.Draft) &&
            ReferenceEquals(SectionDraft, other.SectionDraft) && ReferenceEquals(Estimates, other.Estimates) && Busy == other.Busy &&
            ReferenceEquals(Analysis, other.Analysis) &&
            (Selection, other.Selection) switch
            {
                (Selection.Points mine, Selection.Points theirs) => mine.Items.SequenceEqual(theirs.Items),
                var (mine, theirs) => Equals(mine, theirs)
            };
    }

    public void RefreshPanes() => RefreshPanes(full: true);

    private void RefreshChangedPanes() => RefreshPanes(full: false);

    /// <summary>
    /// The bottom panel binds the selected run only while it is on screen (Analysis shows it, ⌘J folds it) or while the Section
    /// document, which shows the panel's Section view, is the open document.
    /// </summary>
    private void BindAnalysisPanel()
    {
        bool shown = Controller.IsAnalysis && !bottomFolded;
        if (AnalysisPanel.IsVisible != shown) AnalysisPanel.IsVisible = shown;
        if (shown || ReferenceEquals(LayoutFactory.MainDocumentDock.ActiveDockable, LayoutFactory.SectionDocument))
            AnalysisPanel.Bind(Controller);
    }

    /// <summary>"Open in main area" (Ruling 124): the Section document becomes the open document and takes focus.</summary>
    private void OpenSectionDocument()
    {
        LayoutFactory.MainDocumentDock.ActiveDockable = LayoutFactory.SectionDocument;
        BindAnalysisPanel();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => AnalysisPanel.SectionView.FocusSelector());
    }

    /// <summary>
    /// A layer check changed the controller's flag, which raises no notification: the panel is re-read and every view that may
    /// carry a layer repaints. Properties is not re-bound (its rows do not depend on layer visibility).
    /// </summary>
    private void OnLayerVisibilityChanged()
    {
        BindAnalysisPanel();
        foreach (var view in ModelView.GetVisualDescendants().OfType<Control>().Where(item => item is PlanCanvas or View3d or ElevationView))
            view.InvalidateVisual();
    }

    /// <summary>What ⌘J says outside Analysis (every row runs or names why, UI-DEAD-CONTROL). New copy: flagged to the operator.</summary>
    public const string BottomPanelInAnalysis = "The bottom panel shows the analysis results. Switch with the CAD | Analysis toggle.";

    /// <summary>The bottom panel's fold (⌘J, View ▸ Bottom panel): it appears in Analysis only, and folds there.</summary>
    public void ToggleBottomPanel()
    {
        if (!Controller.IsAnalysis)
        {
            Report(new StatusReport(BottomPanelInAnalysis));
            return;
        }
        bottomFolded = !bottomFolded;
        BindAnalysisPanel();
    }

    private void RefreshPanes(bool full)
    {
        PaneRefreshes++;
        var inputs = SectionPaneInputs.Of(Controller);
        bool propertiesCurrent = !full && inputs is { } now && sectionPaneInputs is { } shown && now.Same(shown);
        sectionPaneInputs = inputs;
        if (!propertiesCurrent)
        {
            PropertiesBinds++;
            Properties.Bind(Controller);
        }
        Browser.Bind(Controller);
        Points.Bind(Controller);
        Layers.Bind(Controller);
        BindAnalysisPanel();
        ModelView.SectionEditor.Bind(Controller);
        ReportControllerStatus();
        // The toast closes when the next commit starts (DESIGN.md §4 Toast).
        if (lastGesture == GestureState.Idle && Controller.Gesture != GestureState.Idle) ModelView.CloseToast();
        lastGesture = Controller.Gesture;

        bool foilOpen = Controller.Inspection is not null;
        StatusStrip.ShowItems(SelectionItemText(), foilOpen, Controller.Estimates is not null, Properties.TextScale, StripUnits());
        ModelView.ShowFoilOpen(foilOpen);
        if (foilOpen)
        {
            // The accepted source changes only with the acceptance (Inspection), one of the inputs above.
            if (!propertiesCurrent) ModelView.SourceText.Text = Controller.AcceptedSource;
        }
    }

    // simplify: the CFDW_DIAG_PANES trace (PaneDiagnostics): the Properties region's active tool, whether the left dock is in
    // the layout, and the control each on-screen presenter of the Properties tool actually hosts (its hash, or empty).
    private string DiagnoseDock()
    {
        var tool = LayoutFactory.PropertiesTool;
        string hosted = string.Join(",", DockHost.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
            .Where(presenter => ReferenceEquals(presenter.Content, tool))
            .Select(presenter => presenter.Child?.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "empty"));
        return $"dock={(tool.Owner as IDock)?.ActiveDockable?.Id ?? "none"} left-in-layout={LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(LayoutFactory.LeftToolDock)} " +
               $"workspace={Workspace} hosted={(hosted.Length == 0 ? "none" : hosted)}";
    }

    public void ToggleLeftSidebar()
    {
        PaneDiagnostics.Write(() => $"shell.left-toggle {DiagnoseDock()}");
        var dock = LayoutFactory.LeftToolDock;
        if (dock != null)
        {
            bool wasVisible = LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(dock) ?? false;
            if (wasVisible)
            {
                if (dock.IsActive || Properties.IsKeyboardFocusWithin || Browser.IsKeyboardFocusWithin)
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
        // While a warning toast is open it is one more stop, after the model area (DESIGN.md §4 Toast).
        var toast = ModelView.ToastOpen ? ModelView.ToastDismissButton : null;
        var targets = new[] { leftTarget, (Control?)documentTab, toast };
        int current = leftTarget.IsKeyboardFocusWithin ? 0 : documentTab?.IsKeyboardFocusWithin == true ? 1
            : ModelView.WarningToast.IsKeyboardFocusWithin ? 2 : -1;
        var available = targets.Select(target => target is { IsVisible: true, IsEnabled: true }).ToArray();
        var origin = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        for (int attempt = 0; attempt < targets.Length; attempt++)
        {
            int next = FocusRing.NextRegionIndex(current, reverse, available);
            if (next < 0) return false;
            if (next == 2 && current != 2) ModelView.ToastReturnFocus = origin;
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
                LayoutFactory.LeftToolDock.ActiveDockable = LayoutFactory.BrowserTool;
                FocusControlWhenReady(() => Browser.FindControl<ListBox>("LeadingEdgeList")?.Items
                    .OfType<ListBoxItem>().FirstOrDefault());
                break;
            default:
                throw new ArgumentException("Unknown review persona", nameof(persona));
        }
    }

    /// <summary>The Text size multiplier the panes are drawn at (DN-5).</summary>
    public double TextScale => Properties.TextScale;

    /// <summary>The startup read of the persisted Text size (DN-5); completes once the value is applied.</summary>
    public Task TextSizeLoaded { get; private set; } = Task.CompletedTask;

    /// <summary>The latest Text size write; completed when there is none.</summary>
    public Task TextSizeSaved { get; private set; } = Task.CompletedTask;

    /// <summary>Raised after the Text size changes, so the View ▸ Text size radio items can follow.</summary>
    public event Action<double>? TextScaleChanged;

    /// <summary>
    /// DN-5: sets the Text size to a step of the ladder, announces it politely ("Text size 150 %.") and persists it per
    /// user (<c>cfdw-display</c> in the preference root). A choice made before the startup read finishes wins over it.
    /// </summary>
    public void SetTextScale(double scale)
    {
        textSizeChosen = true;
        double step = CommandTable.TextSizes.MinBy(size => Math.Abs(size - scale));
        if (Math.Abs(step - Properties.TextScale) < 1e-9)
        {
            // Chosen while the startup read is pending: the read will not apply, so this choice must reach the file.
            if (Preferences is not null && !TextSizeLoaded.IsCompleted) TextSizeSaved = SaveTextSizeAsync(Preferences, step);
            return;
        }
        ApplyTextScale(step);
        Report(new StatusReport(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Text size {step * 100:0} %.")));
        TextScaleChanged?.Invoke(step);
        if (Preferences is not null) TextSizeSaved = SaveTextSizeAsync(Preferences, step);
    }

    private bool textSizeChosen;
    private bool textSizeNoticeShown;

    /// <summary>Applies the persisted Text size at startup: no announcement and no write back. Recorded as <c>display.load</c>.</summary>
    private async Task LoadTextSizeAsync(PreferenceStore preferences)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        var load = await preferences.LoadTextSizeAsync(CancellationToken.None);
        ShellEvents.Record("display.load", load.Outcome, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            Guid.NewGuid().ToString("N"), code: load.Codes.Count == 0 ? null : string.Join(",", load.Codes));
        await OnUiThread(() => ApplyLoadedTextSize(load.Percent));
    }

    /// <summary>
    /// Writes the Text size, records <c>display.save</c>, and tells the user once per session, politely, when the
    /// setting cannot be kept.
    /// </summary>
    private async Task SaveTextSizeAsync(PreferenceStore preferences, double step)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        var save = await preferences.SaveTextSizeAsync((int)Math.Round(step * 100), CancellationToken.None);
        ShellEvents.Record("display.save", save.Outcome, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            Guid.NewGuid().ToString("N"), code: save.Code, publicationKnown: save.PublicationKnown,
            durabilityConfirmed: save.DurabilityConfirmed, retried: save.Retried);
        if (save.Outcome is "saved" or "cancelled") return;
        await OnUiThread(() =>
        {
            if (textSizeNoticeShown) return;
            textSizeNoticeShown = true;
            Report(new StatusReport("Text size will apply this session only: " + NotKeptReason(save.Outcome) + ".", ReportKind.Warning));
        });
    }

    private static string NotKeptReason(string outcome) => outcome switch
    {
        "session-only" => "the preference folder is linked or cannot be saved to",
        "never-write" => "the saved preferences file could not be read",
        "claim-held" => "another copy of CFD Workbench is saving preferences",
        _ => "the preferences could not be saved"
    };

    private static async Task OnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else await Dispatcher.UIThread.InvokeAsync(action);
    }

    private void ApplyLoadedTextSize(int percent)
    {
        if (textSizeChosen) return;
        double step = CommandTable.TextSizes.MinBy(size => Math.Abs(size - percent / 100.0));
        if (Math.Abs(step - Properties.TextScale) < 1e-9) return;
        ApplyTextScale(step);
        TextScaleChanged?.Invoke(step);
    }

    // DN-5: the panes, the strip and the toast draw their 11 px type at the same Text size.
    private void ApplyTextScale(double step)
    {
        Properties.ApplyTextScale(step);
        StatusStrip.ApplyTextScale(step);
        ModelView.ApplyTextScale(step);
        StatusStrip.ShowItems(SelectionItemText(), Controller.Inspection is not null, Controller.Estimates is not null, step, StripUnits());
    }

    /// <summary>The Units item's text (Ruling 115): the display units of every area. CAD lengths stay in mm (A4.7).</summary>
    private string StripUnits() => Controller.AnalysisUnits.ToString();

    /// <summary>The strip's selection item ("TE · pt 7 of 14", "Twist · pt 5 of 7"); absent with no point selected.</summary>
    private string? SelectionItemText()
    {
        if (Controller.Selection is not Selection.Points { Items.Count: > 0 } points) return null;
        if (points.Items.Count > 1) return $"{points.Items.Count} points";
        var item = points.Items[0];
        if (item.Curve is "upper" or "lower")
        {
            // "Upper · pt 7 of 13 ⇄ lower" (Ruling 60's partner cue); the nose is one shared point.
            var surface = Controller.SectionCurve(SectionPoints.Side(item.Curve));
            var vertex = surface?.Points.FirstOrDefault(candidate => candidate.Id == item.VertexId);
            if (surface is null || vertex is null) return null;
            if (vertex.Role == PointRole.Nose) return "Nose";
            return $"{SectionPoints.Surface(item.Curve)} · pt {vertex.Index + 1} of {surface.Points.Count} ⇄ {SectionPoints.Other(item.Curve)}";
        }
        if (Controller.CurveFor(item.Curve) is not { } curve) return null;
        var point = curve.Points.FirstOrDefault(candidate => candidate.Id == item.VertexId);
        string name = item.Curve switch
        {
            "leading" => "LE",
            "trailing" => "TE",
            _ => PropertiesView.Curves.TryGetValue(item.Curve, out var rows) ? rows.Name : item.Curve
        };
        return point is null ? null : $"{name} · pt {point.Index + 1} of {curve.Points.Count}";
    }

    /// <summary>Bigger and Smaller step along the ladder and stop at 100 % and 200 %.</summary>
    private void StepTextSize(int direction)
    {
        var sizes = CommandTable.TextSizes;
        int index = Math.Max(0, sizes.ToList().FindIndex(size => Math.Abs(size - Properties.TextScale) < 1e-9));
        SetTextScale(sizes[Math.Clamp(index + direction, 0, sizes.Count - 1)]);
    }

    /// <summary>
    /// DR-DEN-4: a model view has keyboard focus, so ⌘= / ⌘− zoom it. A model view is the Plan canvas, the section
    /// canvas, or any focusable control inside a model-area view frame — the 3D view and its cube, the elevations and
    /// their points — except the frame's view label: by place in the tree, not by a list of view types.
    /// </summary>
    private bool ModelViewFocused() =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is SectionCanvas || FocusedModelView() is not null;

    /// <summary>The model-area view that holds keyboard focus, or null.</summary>
    private SingleView? FocusedModelView()
    {
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not Visual focused) return null;
        if (focused is PlanCanvas) return SingleView.Plan;
        if (focused is Button label && label.Classes.Contains("viewLabel")) return null;
        foreach (var (frame, view) in ViewFrames())
            if (frame.IsVisualAncestorOf(focused)) return view;
        return null;
    }

    private IEnumerable<(Border Frame, SingleView View)> ViewFrames() =>
    [
        (ModelView.PlanFrame, SingleView.Plan), (ModelView.ThreeDFrame, SingleView.ThreeD),
        (ModelView.SideFrame, SingleView.Side), (ModelView.FrontFrame, SingleView.Front)
    ];

    /// <summary>The drawn size of a model-area view: the control its camera projects into (the size its first fit used).</summary>
    public Size ViewSize(SingleView view) => view switch
    {
        SingleView.Plan => ModelView.PlanCanvas.Bounds.Size,
        SingleView.ThreeD => ModelView.ThreeDView.Bounds.Size,
        SingleView.Side => ModelView.SideRenderer.Bounds.Size,
        _ => ModelView.FrontRenderer.Bounds.Size
    };

    private ViewCommandContext ViewContext() =>
        new(Controller, ViewSize, Report, FocusedModelView(), ModelView.ThreeDView.ApplyPreset);

    public bool CanRun(string id)
    {
        if (IsShellCommand(id)) return ShellCommandReason(id) is null;
        if (id is "view.zoom-in" or "view.zoom-out")
            return Controller.Inspection is not null || !ModelViewFocused();
        // C1: the Curvature toggle stays focusable and runnable in Analysis; it does nothing there and says why (CommandReason).
        if (id.StartsWith("view.comb", StringComparison.Ordinal))
            return Controller.Inspection is not null;
        if (ViewCommands.Handles(id)) return ViewCommands.CanRun(id, Controller);
        if (!id.StartsWith("point.", StringComparison.Ordinal)) return true;
        if (id is "point.add" or "point.remove" or "point.rebuild") return PointCommandReason(id) is null;
        var point = SelectedPoint();
        if (point is null) return false;
        return id switch
        {
            "point.make-anchor" => point.Role == PointRole.Control,
            "point.make-control" => point.Role == PointRole.Anchor,
            "point.tangent-smooth" or "point.tangent-symmetric" or "point.tangent-corner" =>
                point.Role is PointRole.Anchor or PointRole.RootEnd or PointRole.TipEnd,
            _ => false
        };
    }

    /// <summary>Why a runnable command does nothing right now (C1, the aria-disabled equivalent), or null: the Curvature toggle in Analysis.</summary>
    public string? CommandReason(string id) => id == "view.comb" && Controller.IsAnalysis ? RailComb.AnalysisReason : null;

    // C4: a palette verb changes the comb's scale or density and announces the new value. The plate's live region speaks it while the
    // plate is shown; with the comb off (or in Analysis) the status strip, the window's one polite region, carries it.
    private void RunCombVerb(string id)
    {
        switch (id)
        {
            case "view.comb-larger": Controller.StepCombScale(larger: true); break;
            case "view.comb-smaller": Controller.StepCombScale(larger: false); break;
            case "view.comb-auto": Controller.SetCombAuto(); break;
            case "view.comb-denser": Controller.StepCombDensity(denser: true); break;
            default: Controller.StepCombDensity(denser: false); break;
        }
        if ((!Controller.CombVisible || Controller.IsAnalysis) && Controller.CombAnnouncement is { } text)
            Report(new StatusReport(text));
    }

    /// <summary>The visible reason for a point verb's disabled Edit or context-menu row.</summary>
    public string? PointCommandReason(string id)
    {
        if (Controller.Inspection is null || Controller.Section is not null)
            return "Open a certified foil in the views to edit points.";
        if (Controller.Gesture != GestureState.Idle)
            return "Finish the current change first.";
        if (Controller.Selection is not Selection.Points { Items: var items } || items.Count == 0)
            return "Select a point on the curve first.";
        if (id == "point.remove" && items.Count != 1)
            return "Remove points one at a time, so each change is measured.";
        if (items.Count != 1) return "Select one curve point first.";
        var point = SelectedPoint();
        if (point is null || Controller.CurveFor(point.Curve) is not { } curve)
            return "Select a point on the curve first.";
        // Add's only limit is Core's verb ceiling (16, §3.6; CurveView.Ceiling is the file version's, 10 under 4.0):
        // Core refuses past it with COPY-196, which the strip shows (Controller_RefusalCopy_UsesCoreReasonNotCode).
        if (id == "point.add") return null;
        if (id == "point.rebuild") return null;
        if (id != "point.remove") return "Unknown point command.";
        if (curve.Points.Count <= 4) return "A curve needs at least 4 points.";
        return point.Role switch
        {
            PointRole.RootEnd => "The root end can't be removed: the curve starts there.",
            PointRole.TipEnd => "The tip end can't be removed: the curve ends there.",
            PointRole.RootHandle => "This handle sets the curve's direction at the root. Move it, or rebuild the curve with fewer points.",
            PointRole.TipHandle => "This handle sets the curve's direction at the tip. Move it, or rebuild the curve with fewer points.",
            PointRole.Anchor => $"Point {point.Index + 1} is an anchor. Make it a control point first, then remove it.",
            PointRole.AnchorHandle => $"Point {point.Index + 1} is a handle of the anchor at point {curve.Points.First(item => item.Id == point.AnchorId).Index + 1}. Make that anchor a control point first.",
            _ => point.Locks.FirstOrDefault(item => item != "root_mirror") is { } named
                ? $"Point {point.Index + 1} is locked ({named})." : null
        };
    }

    public async Task RunCommand(string id)
    {
        if (id.StartsWith("point.", StringComparison.Ordinal) && !CanRun(id)) return;
        if (CommandTable.TextSizeOf(id) is { } size)
        {
            SetTextScale(size);
            return;
        }
        if (CommandTable.UnitsOf(id) is { } units)
        {
            Controller.AnalysisUnits = units;
            return;
        }
        if (IsShellCommand(id))
        {
            await RunShellCommandAsync(id);
            return;
        }
        switch (id)
        {
            case "view.text-bigger":
                StepTextSize(+1);
                return;
            case "view.text-smaller":
                StepTextSize(-1);
                return;
            case "view.zoom-in" or "view.zoom-out" when !ModelViewFocused():
                // DR-DEN-4: with focus anywhere but a model view, ⌘= / ⌘− change the Text size.
                StepTextSize(id == "view.zoom-in" ? +1 : -1);
                return;
            case "view.comb":
                if (CommandReason(id) is { } reason)
                {
                    Report(new StatusReport(reason));
                    return;
                }
                Controller.CombVisible = !Controller.CombVisible;
                Report(new StatusReport(Controller.CombVisible ? "Curvature comb on." : "Curvature comb off."));
                return;
            case "view.comb-larger" or "view.comb-smaller" or "view.comb-auto" or "view.comb-denser" or "view.comb-sparser":
                RunCombVerb(id);
                return;
            case "point.make-anchor":
                await RunPoint(point => new PointCommand.MakeAnchor(point.Curve, point.Id));
                return;
            case "point.add":
                if (SelectedPoint() is { } addPoint) ModelView.BeginAddPoint(addPoint, ModelView.PlanCanvas);
                return;
            case "point.remove":
                await RunPoint(point => new PointCommand.RemovePoint(point.Curve, point.Id));
                return;
            case "point.rebuild":
                if (SelectedPoint() is { } rebuildPoint)
                    ModelView.BeginRebuild(rebuildPoint.Curve, ModelView.PlanCanvas);
                return;
            case "point.make-control":
                await RunPoint(point => new PointCommand.MakeControl(point.Curve, point.Id));
                return;
            case "point.tangent-smooth":
                await RunPoint(point => new PointCommand.SetTangent(point.Curve, point.Id, TangentKind.Smooth, null));
                return;
            case "point.tangent-symmetric":
                await RunPoint(point => new PointCommand.SetTangent(point.Curve, point.Id, TangentKind.Symmetric, null));
                return;
            case "point.tangent-corner":
                await RunPoint(point => new PointCommand.SetTangent(point.Curve, point.Id, TangentKind.Corner, null));
                return;
        }
        // Zoom, fit, pan, layouts, display and cameras act on the target view (M1.2b2 §5.2) through one runner.
        if (ViewCommands.Handles(id))
        {
            if (!ViewCommands.CanRun(id, Controller))
            {
                if (Controller.Inspection is not null && ViewCommands.DisabledReason(id, Controller) is { } reason)
                    Report(new StatusReport(reason));
                return;
            }
            ViewCommands.Run(id, ViewContext());
        }
    }

    // ---------------- workspaces (§11.8) ----------------

    /// <summary>
    /// Applies one workspace's preset (⌘1 / ⌘2 / ⌘3): the side bars its regions open, from <see cref="WorkspacePresets"/>
    /// (the codec's homes), and its view arrangement. Precision = Planform plus the Points pane in the right side bar.
    /// simplify: no per-workspace memory and no persistence of the switch; the ceiling is that a user's changes to a
    /// workspace are not remembered on return; the upgrade trigger is app-shell D4 (M1.2e).
    /// </summary>
    public void ApplyWorkspace(WorkspaceId workspace)
    {
        PaneDiagnostics.Write(() => $"shell.workspace from={Workspace} to={workspace} {DiagnoseDock()}");
        Workspace = workspace;
        var layout = WorkspacePresets.Preset(workspace).Workspaces.Single(item => item.Id == workspace);
        SetLeftShown(layout.Regions.Any(region => region.Id == RegionId.Left && region.Open && region.Groups.Count > 0));
        SetRightShown(WorkspacePresets.ShownIn(workspace, "points") == RegionId.Right);
        if (Controller.Inspection is not null && Controller.Section is null)
            Controller.Layout = layout.Views.Arrangement == ViewArrangement.Four ? ViewLayout.Four : ViewLayout.Plan3d;
        Report(new StatusReport($"{WorkspaceName(workspace)} workspace."));
    }

    private static string WorkspaceName(WorkspaceId workspace) => workspace switch
    {
        WorkspaceId.Precision => "Precision",
        WorkspaceId.Review => "Review",
        _ => "Planform"
    };

    /// <summary>The right side bar is in the layout.</summary>
    public bool RightSidebarShown => LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(LayoutFactory.RightToolDock) == true;

    private void SetLeftShown(bool shown)
    {
        var docks = LayoutFactory.TopProportionalDock.VisibleDockables;
        if (docks is null || docks.Contains(LayoutFactory.LeftToolDock) == shown) return;
        if (shown) docks.Insert(0, LayoutFactory.LeftToolDock);
        else
        {
            if (Properties.IsKeyboardFocusWithin || Browser.IsKeyboardFocusWithin) LeftSidebarToggle.Focus();
            docks.Remove(LayoutFactory.LeftToolDock);
        }
    }

    private void SetRightShown(bool shown)
    {
        var docks = LayoutFactory.TopProportionalDock.VisibleDockables;
        if (docks is null || RightSidebarShown == shown) return;
        if (shown)
        {
            // The right side bar opens at the preset's 260 DIP (LayoutCodec Chrome), as the left one does.
            // Dock normalizes the siblings' proportions (left + model already sum to 1), so the share p of the width is
            // written as p / (1 − p).
            double width = DockHost.Bounds.Width;
            double share = width > 0 ? WorkspacePresets.ProportionFor(DefaultLeftPaneWidth, width) : 0.2;
            LayoutFactory.RightToolDock.Proportion = share / (1 - share);
            docks.Add(LayoutFactory.RightSplitter);
            docks.Add(LayoutFactory.RightToolDock);
            LayoutFactory.RightToolDock.ActiveDockable = LayoutFactory.PointsTool;
        }
        else
        {
            if (Points.IsKeyboardFocusWithin) LeftSidebarToggle.Focus();
            docks.Remove(LayoutFactory.RightToolDock);
            docks.Remove(LayoutFactory.RightSplitter);
        }
    }

    // ---------------- section commands (§5.2) and the section strip ----------------

    /// <summary>The rows the shell runs itself: the section rows, Thickness ×2, the Points pane and the workspaces.</summary>
    public static bool IsShellCommand(string id) =>
        id.StartsWith("section.", StringComparison.Ordinal) || id is "view.thickness-x2" or "window.points" or "window.layers" ||
        id.StartsWith("window.workspace-", StringComparison.Ordinal);

    /// <summary>The copy a section row names when the mode is not open.</summary>
    public const string NotInSection = "Open a section first: select a station and choose Edit section….";

    /// <summary>
    /// Thickness ×2 (§11.2) is the section editor's drawing state, owned by its mode bar toggle (EDT). Until that toggle is
    /// reachable from the shell the row names where it is, rather than flip a state nothing draws (UI-DEAD-CONTROL).
    /// </summary>
    public const string ThicknessOnModeBar = "Thickness ×2 is on the section editor's mode bar.";

    /// <summary>
    /// Show (design §5.2): selects the point a blocker names and asks the section editor to frame it with the location the
    /// blocker measured (a chord-fraction range, or null when it has none). The editor's Fit Selection does the framing.
    /// </summary>
    public event Action<PointRef?, (double X0, double X1)?>? SectionShowRequested;

    /// <summary>COPY-182's sibling for a blocker with no place on the section.</summary>
    public const string CannotShow = "This can't be shown on the section.";

    private const string SectionChecking = "Checking section geometry…";
    private const string SectionCrossing = "Upper and lower surfaces cross. Move the point back to finish.";   // COPY-123
    private const string SectionCleared = "Surfaces no longer cross. Finish is available.";                    // COPY-124

    private long sectionGeneration = -1;
    private bool sectionCrossing;
    private byte[]? sectionBytes;
    private PointRef? sectionMoved;

    /// <summary>Why a shell row cannot run now, or null when it can (UI-DEAD-CONTROL: every row runs or names why).</summary>
    public string? ShellCommandReason(string id)
    {
        if (id.StartsWith("window.workspace-", StringComparison.Ordinal) || id is "window.points" or "window.layers") return null;
        var mode = Controller.Section;
        if (id == "section.edit")
        {
            if (Controller.Inspection is null) return "Open a foil to edit a section.";
            if (mode is not null) return "The section editor is already open.";
            return Controller.Selection is Selection.Station ? null : "Select a station to edit its section.";
        }
        if (mode is null) return NotInSection;
        switch (id)
        {
            case "section.finish":
                return mode.CanFinish ? null : mode.FinishReason ?? (mode.IsDirty ? "This section is still being checked." : "No section changes to Finish.");
            case "section.insert-point" or "section.insert-anchor":
                if (SelectedSectionPoint() is not { } at) return "Select a point on the section to insert beside it.";
                return at.Point.Role == PointRole.TrailingEnd ? "Select a point before the trailing edge to insert after it." : null;
            case "section.delete-point":
                if (SelectedSectionPoint() is not { } doomed) return "Select a point on the section to delete it.";
                return doomed.Point.Role switch
                {
                    PointRole.Nose => "The nose is always an anchor. It can't be deleted.",
                    PointRole.TrailingEnd => "The trailing-edge point is always an anchor. It can't be deleted.",
                    _ => null
                };
            case "view.thickness-x2":
                return ThicknessOnModeBar;
            case "section.make-unique":
                return mode.Draft.Scope == SectionScope.Independent ? "This station already has its own section." : null;
            case "section.thickness-channel":
                return mode.Draft.Intent == ThicknessIntent.KeepCurrent ? "Station t/c already comes from the Thickness curve." : null;
            case "section.thickness-source":
                return mode.Draft.Intent == ThicknessIntent.UseSource ? "Station t/c already comes from this section." : null;
            default:
                return null;
        }
    }

    private async Task RunShellCommandAsync(string id)
    {
        if (ShellCommandReason(id) is { } reason)
        {
            Report(new StatusReport(reason));
            return;
        }
        switch (id)
        {
            case "window.workspace-planform": ApplyWorkspace(WorkspaceId.Planform); return;
            case "window.workspace-precision": ApplyWorkspace(WorkspaceId.Precision); return;
            case "window.workspace-review": ApplyWorkspace(WorkspaceId.Review); return;
            case "window.layers":
                ShowPane("layers");
                Report(new StatusReport("Layers pane shown in the left side bar."));
                return;
            case "window.points":
                SetRightShown(true);
                Report(new StatusReport("Points pane shown in the right side bar."));
                return;
            case "section.edit":
                await EnterSectionAsync(EntryOrigin.Palette);
                return;
            case "section.finish":
                await FinishSectionAsync();
                return;
            case "section.cancel":
                CancelSection();
                return;
            case "section.import-dat":
                await ImportDatAsync();
                return;
            case "section.replace-catalog":
                await ReplaceFromCatalogAsync();
                return;
            case "section.save-mine":
                await SaveMineAsync();
                return;
        }
        if (SectionStepFor(id) is { } step) await ApplySectionStepAsync(step);
    }

    /// <summary>Edit section (COPY-172): the selected station's section opens in the model area.</summary>
    public async Task EnterSectionAsync(EntryOrigin origin)
    {
        if (Controller.Selection is not Selection.Station station) return;
        try
        {
            await Controller.EnterSectionAsync(station.Index, origin);
        }
        catch (ContractError error)
        {
            Report(new StatusReport(error.Reason ?? error.Code, ReportKind.Warning));
            return;
        }
        if (Controller.CurrentProjection is { } projection)
            Report(new StatusReport($"Editing {SectionPoints.StationName(projection, station.Index)} section."));
    }

    private async Task FinishSectionAsync()
    {
        var mode = Controller.Section!;
        string station = StationNameOf(mode);
        int changes = mode.Draft.Cursor;
        try { await Controller.FinishSectionAsync(); }
        catch (ContractError)
        {
            Report(new StatusReport(Controller.Section?.FinishReason ?? "This section cannot Finish yet.", ReportKind.Warning));
            return;
        }
        // COPY-179
        Report(new StatusReport($"Finished {station} section: {changes} {(changes == 1 ? "change" : "changes")} in one undo step."));
    }

    private void CancelSection()
    {
        string station = StationNameOf(Controller.Section!);
        Controller.CancelSection();
        Report(new StatusReport($"Cancelled. {station} section is as it was."));   // COPY-180
    }

    private string StationNameOf(SectionMode mode) =>
        Controller.CurrentProjection is { } projection ? SectionPoints.StationName(projection, mode.Draft.Assignment) : "This";

    private (PointRef Ref, PointView Point, CurveView Curve)? SelectedSectionPoint()
    {
        if (Controller.Selection is not Selection.Points { Items.Count: 1 } points || points.Items[0] is not { Curve: "upper" or "lower" } item)
            return null;
        var curve = Controller.SectionCurve(SectionPoints.Side(item.Curve));
        var point = curve?.Points.FirstOrDefault(candidate => candidate.Id == item.VertexId);
        return curve is null || point is null ? null : (item, point, curve);
    }

    private SectionStep? SectionStepFor(string id)
    {
        var selected = SelectedSectionPoint();
        double Between()
        {
            var (_, point, curve) = selected!.Value;
            var next = curve.Points[Math.Min(point.Index + 1, curve.Points.Count - 1)];
            return (point.SpanMeters + next.SpanMeters) / 2;
        }
        return id switch
        {
            "section.insert-point" => new SectionStep.Insert(SectionPoints.Side(selected!.Value.Ref.Curve), Between()),
            "section.insert-anchor" => new SectionStep.InsertAnchor(SectionPoints.Side(selected!.Value.Ref.Curve), Between()),
            "section.delete-point" => new SectionStep.Delete(SectionPoints.Side(selected!.Value.Ref.Curve), selected.Value.Ref.VertexId),
            // simplify: Smooth fairs both surfaces at the 10⁻³ chord tolerance the section draft is tested at; the
            // upgrade trigger is a Smooth dialog with its own tolerance (OI).
            "section.smooth" => new SectionStep.Fair(null, 1e-3, PreserveEnds.Position),
            "section.make-unique" => new SectionStep.MakeUnique(),
            "section.thickness-channel" => new SectionStep.Thickness(ThicknessIntent.KeepCurrent),
            "section.thickness-source" => new SectionStep.Thickness(ThicknessIntent.UseSource),
            _ => null
        };
    }

    private async Task ImportDatAsync()
    {
        string? path = null;
        if (pickOpenFile is not null) path = await pickOpenFile();
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Import .dat section", AllowMultiple = false });
            if (files.Count > 0) { using var file = files[0]; path = file.Path.LocalPath; }
        }
        if (path is null)
        {
            Report(new StatusReport("Import cancelled. Nothing changed."));
            return;
        }
        byte[] dat;
        try { dat = await UserFile.ReadAllBytesAsync(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Report(new StatusReport($"The .dat file couldn't be read. Nothing changed.", ReportKind.Error));
            return;
        }
        await ApplySectionStepAsync(new SectionStep.Replace(
            new ReplaceSource.Coordinates("a .dat file", new Provenance("dat:sha256:" + Identity.Sha256(dat), false), dat),
            ReplaceScope.Draft));
    }

    /// <summary>
    /// One step from a shell row or a pane (one step in the section mode, §11.4). A refusal names its reason in the strip;
    /// the paired refit refusal (COPY-187) carries Show.
    /// </summary>
    public async Task ApplySectionStepAsync(SectionStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        try { await Controller.ApplySectionStepAsync(step); }
        catch (ContractError error)
        {
            ReportRefusal(step, error);
        }
    }

    private void ReportRefusal(SectionStep step, ContractError error)
    {
        if (step is SectionStep.SetType { Anchor: false } toControl && RefitRefusal(toControl, error) is { } copy)
        {
            var named = new PointRef(toControl.Side == SurfaceSide.Upper ? "upper" : "lower", toControl.VertexId, Controller.Section?.Draft.Profile);
            Report(new StatusReport(copy, ReportKind.Warning), new StripAction("Show", () => ShowBlocker(named, null)));
            return;
        }
        Report(new StatusReport(RefusalCopy(error), ReportKind.Warning));
    }

    /// <summary>CAT and LIB reasons are already the operator sentence. Other refusals gain "Nothing changed." once.</summary>
    internal static string RefusalCopy(ContractError error)
    {
        bool catalogOrLibrary = error.Code.StartsWith("CAT-", StringComparison.Ordinal)
            || error.Code.StartsWith("LIB-", StringComparison.Ordinal);
        if (catalogOrLibrary)
            return string.IsNullOrWhiteSpace(error.Reason) ? error.Code + " Nothing changed." : error.Reason;
        string reason = error.Reason ?? error.Code;
        return reason.Contains("Nothing changed.", StringComparison.Ordinal) ? reason : reason + " Nothing changed.";
    }

    private void OnCatalogPreview()
    {
        ModelView.SectionEditor.RefreshPreview();
        if (Controller.PreviewFault is { } error)
            Report(new StatusReport(RefusalCopy(error), ReportKind.Warning));
    }

    private async Task ReplaceFromCatalogAsync()
    {
        var snapshot = Controller.OpenCatalog();
        if (ShowCatalogDialog is not null)
        {
            await ShowCatalogDialog();
            return;
        }
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dialog = new CatalogDialog(Controller, snapshot, ModelView.SectionEditor.SectionButton)
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            dialog.Position = new PixelPoint(owner.Position.X + (int)owner.Bounds.Width - (int)dialog.Width, owner.Position.Y);
            await dialog.ShowDialog(owner);
            return;
        }
        string text = snapshot.Outcome == "CAT-UNAVAILABLE"
            ? "Catalog unavailable."
            : $"Catalog open: {snapshot.Choosable} sections.";
        Report(new StatusReport(text, snapshot.Outcome == "CAT-UNAVAILABLE" ? ReportKind.Warning : ReportKind.Info));
    }

    private async Task SaveMineAsync()
    {
        if (ShowSaveDialog is not null || AskSaveName is null && TopLevel.GetTopLevel(this) is Window)
        {
            string? saved;
            if (ShowSaveDialog is not null) saved = await ShowSaveDialog();
            else
            {
                var owner = (Window)TopLevel.GetTopLevel(this)!;
                var dialog = new SaveSectionDialog(Controller, name => Controller.SaveToMySectionsAsync(name),
                    ModelView.SectionEditor.SectionButton);
                saved = await dialog.ShowDialog<string?>(owner);
            }
            if (saved is not null) Report(new StatusReport($"Saved “{saved}” to My sections"));
            return;
        }
        if (AskSaveName is null)
        {
            Report(new StatusReport("Name the section to save it."));
            return;
        }
        string? name = await AskSaveName();
        if (string.IsNullOrWhiteSpace(name))
        {
            Report(new StatusReport("Name the section to save it."));
            return;
        }
        try
        {
            await Controller.SaveToMySectionsAsync(name);
            Report(new StatusReport(Controller.Status));
        }
        catch (ContractError error)
        {
            Report(new StatusReport(RefusalCopy(error), ReportKind.Warning));
        }
    }

    private string? RefitRefusal(SectionStep.SetType step, ContractError error) =>
        Controller.SectionCurve(step.Side)?.Points.FirstOrDefault(point => point.Id == step.VertexId) is { } point
            ? RefitCopy(error.Reason, point.Index + 1, step.Side == SurfaceSide.Upper ? SurfaceSide.Lower : SurfaceSide.Upper)
            : null;

    /// <summary>
    /// COPY-187 from Core's refusal "refit &lt;µm&gt; µm exceeds 10 µm at the largest chord &lt;m&gt; m", for point
    /// &lt;n&gt; whose <paramref name="affected"/> surface would move; null when the reason is not that refusal. The strip
    /// and the section editor's reason box both say it (one composer).
    /// </summary>
    public static string? RefitCopy(string? coreReason, int pointNumber, SurfaceSide affected)
    {
        var match = System.Text.RegularExpressions.Regex.Match(coreReason ?? "", @"refit ([0-9.]+) µm exceeds 10 µm at the largest chord ([0-9.Ee+-]+) m");
        if (!match.Success) return null;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        double microns = double.Parse(match.Groups[1].Value, invariant);
        double chord = double.Parse(match.Groups[2].Value, invariant);
        string other = affected == SurfaceSide.Upper ? "upper" : "lower";
        return string.Create(invariant,
            $"Point {pointNumber} stays an anchor. As a control point the {other} surface would move {microns / 1000:0.0000} mm, over the 0.010 mm limit at {chord * 1000:0.00} mm chord. Nothing changed.");
    }

    /// <summary>Show: select what the blocker names and frame it; a blocker with no place says so.</summary>
    public void ShowBlocker(PointRef? point, (double X0, double X1)? range)
    {
        if (point is null && range is null)
        {
            Report(new StatusReport(CannotShow));
            return;
        }
        if (point is not null) Controller.Select(new Selection.Points([point]));
        SectionShowRequested?.Invoke(point, range);
    }

    private void OnSectionChanged()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(OnSectionChanged);
            return;
        }
        if (Controller.Section is not { } mode)
        {
            sectionGeneration = -1;
            sectionCrossing = false;
            sectionBytes = null;
            sectionMoved = null;
            return;
        }
        // A new step: its report goes to the strip (COPY-174..176, 185, 186 and the insert report).
        if (mode.Draft.Generation != sectionGeneration)
        {
            bool stepped = sectionBytes is not null && mode.Draft.Generation > sectionGeneration && mode.Draft.Cursor == mode.Draft.StepCount &&
                           mode.LastReport is not null;
            if (stepped && SectionStrip.Report(Controller, sectionBytes!, mode, StationNameOf(mode)) is { } report)
            {
                sectionMoved = report.Moved ?? sectionMoved;
                Report(new StatusReport(report.Text));
            }
            sectionGeneration = mode.Draft.Generation;
            sectionBytes = mode.Draft.Bytes;
        }
        if (mode.Assessment is not { } assessment) return;
        bool crossing = assessment.Code == "DSL-PROFILE-CROSS";
        if (crossing && !sectionCrossing)
        {
            var range = Sections.DisplayCrossing(mode.Draft.Bytes, mode.Draft.Assignment);
            var moved = sectionMoved;
            Report(new StatusReport(SectionCrossing, ReportKind.Warning), new StripAction("Show", () => ShowBlocker(moved, range)));
        }
        else if (!crossing && sectionCrossing && assessment.Status == GeometryStatus.Certified)
            Report(new StatusReport(SectionCleared));   // COPY-124, once per clearing
        sectionCrossing = crossing;
    }


    private async Task RunPoint(Func<PointView, PointCommand> command)
    {
        if (SelectedPoint() is not { } point) return;
        await Controller.ApplyPointCommandAsync(command(point));
        RefreshPanes();
    }

    /// <summary>The one selected point, on any of the five curves.</summary>
    private PointView? SelectedPoint() =>
        Controller.Selection is Selection.Points { Items.Count: 1 } points
            ? PropertiesView.Find(Controller.CurveFor, points.Items[0])
            : null;


    private void OnShellKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Handled || e.Key != Avalonia.Input.Key.Return) return;
        // Inside Properties, Return belongs to the focused control: it commits a pending Type or Kind, or toggles a
        // group header (docs/reviews/ui-property-grid.md §10.4).
        if (Properties.IsKeyboardFocusWithin) return;
        // An open Type or Tangent kind list is a popup: focus is outside the pane's visual tree but its items are the pane's
        // logical descendants, and Return there picks the focused item (CB-3).
        if (e.Source is Avalonia.LogicalTree.ILogical source && Avalonia.LogicalTree.LogicalExtensions.IsLogicalAncestorOf(Properties, source)) return;
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox) return;
        if (SelectedPoint() is null) return;
        if (Properties.FindControl<TextBox>("PointSpanInput") is not { IsEnabled: true } span) return;
        span.Focus();
        e.Handled = true;
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
        // Layers' home is the left side bar (G-T6); a hidden left bar opens first, as the Points route opens the right one.
        if (id == "layers") SetLeftShown(true);
        if (id == "points")
        {
            // The Points pane's home is the right side bar (OD-3 B).
            SetRightShown(true);
            FocusDockableTab(LayoutFactory.PointsTool);
            return;
        }
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
            if (ModelView.PlanCanvas.Focus()) return;
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
