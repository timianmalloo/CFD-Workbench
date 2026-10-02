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

        RowDefinitions = new RowDefinitions("*,Auto");
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
        ModelView = new ModelArea();
        StatusStrip = new StatusStrip { Name = "StatusStrip" };
        // The strip starts empty (DR-STATUS-4): the controller's opening prompt is the Start view's to show.
        shownStatusVersion = controller.StatusVersion;
        // PG-26 / DR-STATUS-1: the pane's reports (type, kind, nudge value, echoes, "Selected …", availability) go to the
        // strip; a field error stays on the field's own assertive message and never reaches it.
        Properties.Reported += Report;
        ModelView.PlanCanvas.Controller = controller;
        // DR-NAV-1: Tab from a selected Plan point lands on the Properties pane's first value.
        ModelView.PlanCanvas.TabOut = Properties.FocusFirstValue;
        AddHandler(InputElement.KeyDownEvent, OnShellKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // Assign views to layout tools / documents
        LayoutFactory.PropertiesTool.Context = Properties;
        LayoutFactory.BrowserTool.Context = Browser;
        LayoutFactory.RailControlsTool.Context = null;

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
        SetRow(StatusStrip, 1);
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

        // Wire StartView actions in ModelView.
        ModelView.StartCardView.StartNewButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.AlertNewFoilButton.Click += async (_, _) => await OpenNewFoilAsync();
        ModelView.StartCardView.StartExampleButton.Click += async (_, _) => await OpenExampleAsync();
        ModelView.StartCardView.StartOpenButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.StartCardView.ClearRecentButton.Click += async (_, _) => await ClearRecentAsync();
        StatusStrip.StatusTryAgainButton.Click += async (_, _) => await ClearRecentAsync();
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
    public void Report(StatusReport report) => Report(report, offerTryAgain: false);

    private void Report(StatusReport report, bool offerTryAgain)
    {
        Controller.SupersedeStatus();
        shownStatusVersion = Controller.StatusVersion;
        StatusStrip.Show(report, offerTryAgain);
        if (report.Toast) ModelView.ShowToast(report.Text);
    }

    // The controller's own reports reach the strip only when its status was written since the strip last reflected it, so a
    // refresh never re-shows an older controller status over a newer report.
    private void ReportControllerStatus()
    {
        if (Controller.StatusVersion == shownStatusVersion) return;
        shownStatusVersion = Controller.StatusVersion;
        if (!string.IsNullOrWhiteSpace(Controller.Status))
            StatusStrip.Show(new StatusReport(Controller.Status, Controller.StatusKind));
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
        ModelView.SectionEditor.Bind(Controller);
        ReportControllerStatus();
        // The toast closes when the next commit starts (DESIGN.md §4 Toast).
        if (lastGesture == GestureState.Idle && Controller.Gesture != GestureState.Idle) ModelView.CloseToast();
        lastGesture = Controller.Gesture;

        bool foilOpen = Controller.Inspection is not null;
        StatusStrip.ShowItems(SelectionItemText(), foilOpen, Controller.Estimates is not null, Properties.TextScale);
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
        StatusStrip.ShowItems(SelectionItemText(), Controller.Inspection is not null, Controller.Estimates is not null, step);
    }

    /// <summary>The strip's selection item ("TE · pt 7 of 14"); absent with no point selected.</summary>
    private string? SelectionItemText()
    {
        if (Controller.Planform is not { } plan || Controller.Selection is not Selection.Points { Items.Count: > 0 } points)
            return null;
        if (points.Items.Count > 1) return $"{points.Items.Count} points";
        var item = points.Items[0];
        var rail = item.Curve == "leading" ? plan.Leading : plan.Trailing;
        var point = rail.Points.FirstOrDefault(candidate => candidate.Id == item.VertexId);
        return point is null ? null : $"{(item.Curve == "leading" ? "LE" : "TE")} · pt {point.Index + 1} of {rail.Points.Count}";
    }

    /// <summary>Bigger and Smaller step along the ladder and stop at 100 % and 200 %.</summary>
    private void StepTextSize(int direction)
    {
        var sizes = CommandTable.TextSizes;
        int index = Math.Max(0, sizes.ToList().FindIndex(size => Math.Abs(size - Properties.TextScale) < 1e-9));
        SetTextScale(sizes[Math.Clamp(index + direction, 0, sizes.Count - 1)]);
    }

    /// <summary>DR-DEN-4: a model view (Plan, 3D, Section) has keyboard focus, so ⌘= / ⌘− zoom it.</summary>
    private bool ModelViewFocused() =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is PlanCanvas or Viewport or SectionCanvas;

    public bool CanRun(string id)
    {
        if (id is "view.zoom-in" or "view.zoom-out")
            return Controller.Inspection is not null || !ModelViewFocused();
        if (id is "view.comb" or "view.fit")
            return Controller.Inspection is not null;
        if (!id.StartsWith("point.", StringComparison.Ordinal)) return true;
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

    public async Task RunCommand(string id)
    {
        if (id.StartsWith("point.", StringComparison.Ordinal) && !CanRun(id)) return;
        if (CommandTable.TextSizeOf(id) is { } size)
        {
            SetTextScale(size);
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
            case "view.zoom-in" or "view.zoom-out" when Controller.Inspection is null:
                return;
            case "view.zoom-in":
                Controller.PlanCamera = Controller.PlanCamera with { PixelsPerMeter = Controller.PlanCamera.PixelsPerMeter * 1.25 };
                Report(new StatusReport("Zoomed in."));
                return;
            case "view.zoom-out":
                Controller.PlanCamera = Controller.PlanCamera with { PixelsPerMeter = Math.Max(50, Controller.PlanCamera.PixelsPerMeter / 1.25) };
                Report(new StatusReport("Zoomed out."));
                return;
            case "view.comb":
                Controller.CombVisible = !Controller.CombVisible;
                Report(new StatusReport(Controller.CombVisible ? "Curvature comb on." : "Curvature comb off."));
                return;
            case "view.fit":
                Controller.PlanCamera = Controller.PlanCamera with { PixelsPerMeter = 1000, PanSpanPixels = 0, PanAftPixels = 0 };
                Report(new StatusReport("Fit."));
                return;
            case "point.make-anchor":
                await RunPoint(point => new PointCommand.MakeAnchor(point.Curve, point.Id));
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
    }

    private async Task RunPoint(Func<PointView, PointCommand> command)
    {
        if (SelectedPoint() is not { } point) return;
        await Controller.ApplyPointCommandAsync(command(point));
        RefreshPanes();
    }

    private PointView? SelectedPoint()
    {
        if (Controller.Selection is not Selection.Points { Items.Count: 1 } points || Controller.Planform is not { } plan)
            return null;
        var item = points.Items[0];
        var rail = item.Curve == "leading" ? plan.Leading : plan.Trailing;
        return rail.Points.FirstOrDefault(point => point.Id == item.VertexId);
    }

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
