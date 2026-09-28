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
    public event Action<IReadOnlyList<RecentEntry>>? RecentLoaded;
    private CancellationTokenSource? opening;

    public ShellHost(WorkbenchController controller, PreferenceStore? preferences = null)
    {
        Controller = controller;
        Preferences = preferences;

        RowDefinitions = new RowDefinitions("Auto,*");

        // Top App Bar / Toggle Bar
        var appBar = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4)
        };
        AttachedToVisualTree += (_, _) =>
        {
            appBar.Background = this.FindResource("SurfaceSoftBrush") as IBrush;
            appBar.BorderBrush = this.FindResource("LineBrush") as IBrush;
            RefreshPanes();
        };
        var appPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        LeftSidebarToggle = new Button
        {
            Content = "Sidebar",
            [AutomationProperties.NameProperty] = "Toggle left sidebar"
        };
        LeftSidebarToggle.Click += (_, _) => ToggleLeftSidebar();
        appPanel.Children.Add(LeftSidebarToggle);
        appBar.Child = appPanel;
        SetRow(appBar, 0);
        Children.Add(appBar);

        // Initialize Dock
        LayoutFactory = new ShellLayoutFactory();
        LayoutRoot = LayoutFactory.CreateLayout();

        Properties = new PropertiesPane();
        Browser = new BrowserPane();
        RailEditor = new RailEditorPane();
        ModelView = new ModelArea();

        // Assign views to layout tools / documents
        LayoutFactory.PropertiesTool.Context = Properties;
        LayoutFactory.BrowserTool.Context = Browser;
        LayoutFactory.RailControlsTool.Context = RailEditor;

        // A view has one logical parent. Move these bodies into Dock's document tabs.
        var sectionSample = ModelView.SectionSampleTab.Content;
        var foilSource = ModelView.FoilSourceTab.Content;
        var sectionEditor = ModelView.SectionTab.Content;
        ModelView.SectionSampleTab.Content = null;
        ModelView.FoilSourceTab.Content = null;
        ModelView.SectionTab.Content = null;
        LayoutFactory.ModelDocument.Context = ModelView;
        LayoutFactory.SectionSampleDocument.Context = sectionSample;
        LayoutFactory.FoilSourceDocument.Context = foilSource;
        LayoutFactory.SectionDocument.Context = sectionEditor;

        DockHost = new DockControl
        {
            Factory = LayoutFactory,
            Layout = LayoutRoot,
            InitializeFactory = true
        };
        SetRow(DockHost, 1);
        Children.Add(DockHost);

        // Wire Controller updates
        Controller.Changed += OnControllerChanged;
        Controller.SelectionChanged += OnSelectionChanged;

        // Wire StartView actions in ModelView
        // The controller's blank-foil outcome contract is a pending D2 seam.
        ModelView.StartCardView.StartNewButton.IsEnabled = false;
        ModelView.StartCardView.StartExampleButton.Click += async (_, _) => await OpenExampleAsync();
        ModelView.StartCardView.StartOpenButton.Click += async (_, _) => await OpenFileInteractiveAsync();
        ModelView.StartCardView.ClearRecentButton.Click += async (_, _) => await ClearRecentAsync();
        ModelView.StartCardView.RecentRequested += path => _ = OpenFileAsync(path);
        ModelView.StartCardView.OpenCancelButton.Click += (_, _) => opening?.Cancel();

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
            HandleOpenOutcome(new OpenOutcome.Failed(new OpenFailure.Missing("FILE-NOT-FOUND", "example.foil")), "example.foil");
        }
    }

    public async Task OpenFileAsync(string path)
    {
        if (opening is not null) return;
        using var cancellation = new CancellationTokenSource();
        opening = cancellation;
        ModelView.StartCardView.ShowOpening(System.IO.Path.GetFileName(path));
        try
        {
            var outcome = await Controller.OpenAsync(path, cancellation.Token);
            HandleOpenOutcome(outcome, path);
        }
        finally { opening = null; }
    }

    public async Task OpenFileInteractiveAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open FoilDSL or CFD Workbench project",
            AllowMultiple = false
        });
        if (files.Count == 0) return;
        using var file = files[0];
        await OpenFileAsync(file.Path.LocalPath);
    }

    public void HandleOpenOutcome(OpenOutcome outcome, string path)
    {
        string fileName = System.IO.Path.GetFileName(path);
        switch (outcome)
        {
            case OpenOutcome.Opened:
                ModelView.StartCardView.HideOpening();
                ModelView.ShowFoilOpen(true);
                RefreshPanes();
                ModelView.FoilViewport.Focus();
                _ = RecordRecentAsync(path);
                break;

            case OpenOutcome.Failed failed:
                ModelView.StartCardView.HideOpening();
                if (Controller.Inspection is not null)
                {
                    // Foil already open: show in alert band
                    ModelView.ShowAlertBand($"Failed to open {fileName}: {failed.Failure}");
                }
                else
                {
                    ModelView.StartCardView.ShowAlert(fileName, failed.Failure);
                }
                break;

            case OpenOutcome.Cancelled:
                ModelView.StartCardView.CancelOpening();
                break;

            case OpenOutcome.NeedsIds:
                ModelView.StartCardView.HideOpening();
                ModelView.ShowFoilOpen(true);
                ModelView.ShowAlertBand("Explicit candidate IDs available for insertion.", showAcceptIds: true);
                break;

            case OpenOutcome.Refused refused:
                ModelView.StartCardView.HideOpening();
                ModelView.ShowFoilOpen(true);
                ModelView.ShowAlertBand($"Opening refused ({refused.Code}): foil opened as read-only.");
                break;
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
        if (Preferences != null)
        {
            await Preferences.UpdateRecentAsync(new RecentOp.Clear(), CancellationToken.None);
            await LoadRecentAsync();
        }
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

    private void OnControllerChanged() => RefreshPanes();
    private void OnSelectionChanged() => RefreshPanes();

    public void RefreshPanes()
    {
        Properties.Bind(Controller);
        Browser.Bind(Controller);
        RailEditor.Bind(Controller);

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
                    (parent.ActiveDockable as Control)?.Focus();
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
            var parent = ShellLayoutFactory.FindParentDock(LayoutRoot, dockable);
            if (parent != null)
            {
                if (parent.VisibleDockables?.Contains(dockable) != true)
                {
                    parent.VisibleDockables?.Add(dockable);
                }
                parent.ActiveDockable = dockable;
                (dockable as Control)?.Focus();
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
            (dockable as Control)?.Focus();
        }
    }

    public void SetPaneSize(string id, string sizeName)
    {
        var dockable = LayoutFactory.FindDockable(id);
        if (dockable is IToolDock td)
        {
            td.Proportion = sizeName switch
            {
                "Narrow" => 0.15,
                "Wide" => 0.35,
                _ => 0.25
            };
            (dockable as Control)?.Focus();
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
                bool docCanUndoRedo = Controller.Inspection is not null && Controller.Draft is null;
                switch (verb.ToLowerInvariant())
                {
                    case "undo":
                        if (docCanUndoRedo) { Controller.Undo(); return true; }
                        break;
                    case "redo":
                        if (docCanUndoRedo) { Controller.Redo(); return true; }
                        break;
                }
                return false;

            default:
                return false;
        }
    }
}
