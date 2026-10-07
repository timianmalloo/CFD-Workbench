using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Dock.Avalonia.Controls;
using Avalonia.Rendering.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

public static class ShellWindowTests
{
    // Readiness only: checks moved out of the fast ring (round-oct06 SPL, Ruling 123); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        DesktopChecks.Check("ContextMenu_MakeAnchor_SameEffectAsProperties", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                // Each step runs until its accepted sampling has settled, observed through Changed (Provenance goes through
                // "… sampling" and back to "accepted"); bounded, no sleep. Every status written on the way is kept.
                List<string> Settled(string step, Action act)
                {
                    var seen = new List<string>();
                    bool sampling = false, settled = false;
                    void OnChanged()
                    {
                        lock (seen)
                        {
                            seen.Add(controller.Status);
                            if (controller.Provenance.Contains("sampling", StringComparison.Ordinal)) sampling = true;
                            else if (sampling && controller.Provenance == "accepted") settled = true;
                        }
                    }
                    controller.Changed += OnChanged;
                    try
                    {
                        act();
                        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(8);
                        while (!Volatile.Read(ref settled) && DateTime.UtcNow < deadline)
                            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    }
                    finally { controller.Changed -= OnChanged; }
                    Settle(window);
                    if (!settled) throw new InvalidOperationException(step + ": sampling did not settle; provenance " + controller.Provenance);
                    lock (seen) return [.. seen];
                }
                // The operation's own report, whichever path ran it. The status after settling differs by design: the
                // Properties path's pane report supersedes the sampling line (STATUS-CLOBBER, DR-STATUS-1).
                static string Report(List<string> seen) =>
                    seen.FirstOrDefault(text => text.StartsWith("Point change applied.", StringComparison.Ordinal)) ?? "";
                var trailingPoint = U2Control(controller, "trailing");
                U2Select(controller, window, trailingPoint);
                var viaProperties = Settled("properties", () => U2CommitType(U2Need<ComboBox>(host.Properties, "TypeControl"), TypeAnchorOption));
                if (U2Reload(controller, trailingPoint.Curve, trailingPoint.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("properties did not make an anchor");
                Settled("undo", controller.Undo);
                var leadingPoint = U2Control(controller, "leading");
                var list = U2Need<ListBox>(host.Browser, "LeadingEdgeList");
                var row = list.Items.OfType<ListBoxItem>().First(item => Equals(item.Tag, leadingPoint.Id));
                var menu = row.ContextMenu ?? list.ContextMenu ?? throw new InvalidOperationException("context menu missing");
                var item = menu.Items.OfType<MenuItem>().FirstOrDefault(entry => entry.Header?.ToString() == "Make anchor")
                    ?? throw new InvalidOperationException("Make anchor item missing");
                list.SelectedItem = row;
                var viaMenu = Settled("context menu", () =>
                {
                    if (item.Command is not null) item.Command.Execute(row);
                    else item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                });
                if (U2Reload(controller, leadingPoint.Curve, leadingPoint.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("context menu did not make an anchor");
                if (Report(viaProperties).Length == 0 || Report(viaMenu).Length == 0)
                    throw new InvalidOperationException("context reports '" + string.Join(" | ", viaMenu) + "' properties '" + string.Join(" | ", viaProperties) + "'");
                Settled("context undo", controller.Undo);
                if (U2Reload(controller, leadingPoint.Curve, leadingPoint.Id).Role != PointRole.Control)
                    throw new InvalidOperationException("context-menu undo did not restore the control point");
            }
            finally { window.Close(); }
        });
    }

    public static void Run()
    {
        DesktopChecks.Check("Shell_AllModelTabs_ReentryRealizesContent", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var station = controller.Inspection!.Authored.Assignments[0];
            controller.Select(new Selection.Station(0, station.Eta));
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var documents = new (Dock.Model.Controls.IDocument Document, Control Surface, Func<bool> Ready)[]
                {
                    (host.LayoutFactory.ModelDocument, host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!,
                        () => controller.Planform is not null),
                    (host.LayoutFactory.SectionDocument, host.AnalysisPanel.SectionView,
                        () => host.AnalysisPanel.SectionView.Bounds.Width > 0),
                    (host.LayoutFactory.FoilSourceDocument, host.ModelView.FindControl<TextBox>("SourceText")!,
                        () => !string.IsNullOrWhiteSpace(host.ModelView.FindControl<TextBox>("SourceText")!.Text))
                    // Ruling 124: the Section document replaced the Section sample; the section editor is the model area's Section mode (EDT).
                };
                foreach (var (document, surface, ready) in documents)
                {
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    Settle(window);
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                    Settle(window);
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    Settle(window);
                    if (!ReferenceEquals(surface.GetVisualRoot(), window) || !surface.IsEffectivelyVisible ||
                        surface.Bounds.Width <= 0 || surface.Bounds.Height <= 0 || !ready())
                        throw new InvalidOperationException($"Re-entered {document.Title} has no realized content");
                }
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F6_ModelArea_OnlyDockDocumentTabs", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                int innerRows = host.ModelView.GetVisualDescendants().OfType<TabControl>()
                    .Count(tab => tab.IsEffectivelyVisible);
                int dockTabs = host.DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>()
                    .Count(tab => tab.IsEffectivelyVisible);
                if (innerRows != 0 || dockTabs != 3)
                    throw new InvalidOperationException($"Model area has {innerRows} inner tab rows and {dockTabs} Dock document tabs");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F1_NoSidebarHeaderBand", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                // Three rows: the dock host, the Analysis slot (collapsed outside Analysis, A3a G-T7) and the status strip (DR-STATUS-1).
                // No row above the dock host is a header band.
                if (host.RowDefinitions.Count != 3 || Grid.GetRow(host.DockHost) != 0 || Grid.GetRow(host.AnalysisPanel) != 1 || host.AnalysisPanel.IsVisible ||
                    Grid.GetRow(host.StatusStrip) != 2 || !host.LeftSidebarToggle.IsEffectivelyVisible ||
                    host.GetVisualDescendants().OfType<TextBlock>().Any(text => text.IsEffectivelyVisible && text.Text == "Sidebar") ||
                    host.LeftSidebarToggle.Content is string { Length: > 2 })
                    throw new InvalidOperationException("Standalone Sidebar header band is visible");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F2_LeftPaneChromeButtons_NamedAndDrawn", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var pane = host.DockHost.GetVisualDescendants().OfType<ToolDockControl>().Single();
                var chrome = pane.GetVisualDescendants().OfType<Button>()
                    .Where(button => button.IsEffectivelyVisible &&
                        button.TranslatePoint(default, host) is { } point && point.Y < 80)
                    .ToArray();
                if (chrome.Length != 2 ||
                    chrome.Single(button => button.Name == "PART_MenuButton").Content as string != "⋯" ||
                    Avalonia.Automation.AutomationProperties.GetName(chrome.Single(button => button.Name == "PART_MenuButton")) != "Pane menu" ||
                    chrome.Single(button => button.Name == "PART_CloseButton").Content as string != "×" ||
                    Avalonia.Automation.AutomationProperties.GetName(chrome.Single(button => button.Name == "PART_CloseButton")) != "Close left side bar")
                    throw new InvalidOperationException("Left pane has blank or unnamed chrome buttons");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F3_Properties_OneTopTabLabel", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                // The class, not the instance: every left-pane tool shows its name once, in the one tab row at the top.
                foreach (var tool in new[] { host.LayoutFactory.PropertiesTool, host.LayoutFactory.BrowserTool, host.LayoutFactory.LayersTool })
                {
                    host.LayoutFactory.LeftToolDock.ActiveDockable = tool;
                    Settle(window);
                    var labels = host.DockHost.GetVisualDescendants().OfType<TextBlock>()
                        .Where(text => text.IsEffectivelyVisible && text.Text == tool.Title).ToArray();
                    var pane = host.DockHost.GetVisualDescendants().OfType<ToolDockControl>().Single();
                    var tab = host.DockHost.GetVisualDescendants().OfType<ToolTabStripItem>()
                        .Single(item => ReferenceEquals(item.DataContext, tool));
                    var tabTop = tab.TranslatePoint(default, pane)?.Y ?? double.PositiveInfinity;
                    if (labels.Length != 1 || tabTop > 55)
                        throw new InvalidOperationException($"{tool.Title} has {labels.Length} visible labels; tab top={tabTop}");
                }
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F4_LeftPane_Default260At1440And1280", () =>
        {
            foreach (double width in new[] { 1440d, 1280d })
            {
                using var controller = new WorkbenchController();
                var host = new ShellHost(controller);
                var window = new Window { Content = host, Width = width, Height = 800 };
                try
                {
                    window.Show();
                    Settle(window);
                    var pane = host.DockHost.GetVisualDescendants().OfType<ToolDockControl>().Single();
                    if (Math.Abs(pane.Bounds.Width - 260) > 1)
                        throw new InvalidOperationException($"At {width} DIP, left pane is {pane.Bounds.Width} DIP rather than 260");
                }
                finally { window.Close(); }
            }
        });

        DesktopChecks.Check("Shell_F5_Start_FirstCardFocusedWithRing", () =>
        {
            var window = new MainWindow() { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var card = host.ModelView.FindControl<StartView>("StartCardView")!
                    .FindControl<Button>("StartNewButton")!;
                var layer = AdornerLayer.GetAdornerLayer(card);
                bool ring = layer?.Children.OfType<Control>().Any(child =>
                    ReferenceEquals(AdornerLayer.GetAdornedElement(child), card) && child.Bounds.Width > 0) == true;
                if (!card.IsFocused || !ring)
                    throw new InvalidOperationException($"First start card focus/ring absent: focused={card.IsFocused}, ring={ring}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("MainWindow_ShellMode_ContainsDockHostAndNativeMenu", () =>
        {
            var window = new MainWindow();
            try
            {
                window.Show();
                window.UpdateLayout();
                if (window.Content is not ShellHost host || !host.DockHost.IsVisible)
                    throw new InvalidOperationException("Main window did not render the shell host");
                if (NativeMenu.GetMenu(window)?.Items.Count == 0)
                    throw new InvalidOperationException("Main window has no native menu");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("App_ReviewMode_UsesDockShellAndExactTitle", () =>
        {
            var property = typeof(NativeReviewOptions).GetProperty("Current")!;
            var previous = property.GetValue(null);
            property.SetValue(null, new NativeReviewOptions("keyboard", 1280, 800, "empty", "system", false, null));
            try
            {
                var window = App.CreateMainWindow();
                try
                {
                    if (window.Content is not ShellHost ||
                        window.Title != "CFD Workbench — Offline Foil · REVIEW keyboard / empty / system / motion default")
                        throw new InvalidOperationException("Review window omitted Dock shell or exact title");
                }
                finally { window.Close(); }
            }
            finally { property.SetValue(null, previous); }
        });

        DesktopChecks.Check("UI_DEAD_CONTROL_ShellButtonsHaveActions", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var start = host.ModelView.FindControl<StartView>("StartCardView")!;
                var failures = new HashSet<string>(StringComparer.Ordinal);
                void CheckState()
                {
                    Settle(window);
                    foreach (var button in host.GetVisualDescendants().OfType<Button>()
                        .Where(button => button.IsEffectivelyVisible && button.IsEnabled &&
                            button.Name?.StartsWith("PART_", StringComparison.Ordinal) != true &&
                            button is not ToggleButton { TemplatedParent: Expander }))   // a group header's action is expand/collapse
                    {
                        if (button.Command is not null) continue;
                        // A menu button's action is its menu (the navbar's Views ▾ and Display ▾); an empty menu is still dead.
                        if (button.Flyout is MenuFlyout { Items.Count: > 0 }) continue;
                        var store = typeof(Avalonia.Interactivity.Interactive).GetField("_eventHandlers",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                            .GetValue(button) as System.Collections.IDictionary;
                        if (store?.Contains(Button.ClickEvent) != true) failures.Add(button.Name ?? button.Content?.ToString() ?? "unnamed");
                    }
                }
                CheckState();
                start.ShowOpening("sample.foil");
                CheckState();
                start.HideOpening();
                foreach (OpenFailure failure in new OpenFailure[]
                {
                    new OpenFailure.Missing("FILE-NOT-FOUND", "sample.foil"),
                    new OpenFailure.Unreadable("DOC-IO", "sample.foil"),
                    new OpenFailure.AccessDenied("EACCES", "sample.foil")
                })
                {
                    start.ShowAlert("sample.foil", failure, fromRecent: true);
                    CheckState();
                }
                start.DismissAlert();
                host.ModelView.ShowAlertBand("Candidate IDs", showAcceptIds: true);
                CheckState();
                host.ModelView.ShowAlertBand("Recovery", showResumeRecovery: true);
                CheckState();
                host.ModelView.ShowAlertBand("Failure");
                CheckState();
                host.ModelView.ShowOpenFailure("sample.foil",
                    new OpenFailure.Missing("FILE-NOT-FOUND", "sample.foil"), fromRecent: true);
                CheckState();
                Pump(host.OpenExampleAsync());
                Settle(window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                CheckState();
                if (failures.Count != 0)
                    throw new InvalidOperationException("Enabled shell buttons without Click or Command: " + string.Join(", ", failures));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("NativeMenu_Application_AboutOnly", () =>
        {
            var menu = NativeMenu.GetMenu(Application.Current!);
            var items = menu?.Items.OfType<NativeMenuItem>().Select(item => item.Header?.ToString()).ToArray() ?? [];
            if (items.Count(item => item == "About CFD Workbench") != 1 ||
                items.Any(item => item is "File" or "Edit" or "Window"))
                throw new InvalidOperationException("Application menu mixes workbench commands with OS items");
        });

        DesktopChecks.Check("Recent_StoredRows_StartAndFileMenu", () =>
        {
            string root = ScratchPath("cfdw-d3a-recent-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string recentPath = Path.Combine(root, "opened.foil");
            var preferences = new PreferenceStore(root, () => new ProjectStore());
            var save = preferences.UpdateRecentAsync(new RecentOp.Add(recentPath), CancellationToken.None)
                .GetAwaiter().GetResult();
            var loaded = preferences.LoadRecentAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (loaded.Entries.Count != 1)
                throw new InvalidOperationException($"Recent fixture was not stored: {save.Outcome}/{save.Code}/{loaded.Outcome} root={root}");
            var window = new MainWindow(preferences);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var rows = host.ModelView.FindControl<StartView>("StartCardView")!
                    .FindControl<ListBox>("RecentListBox")!;
                var file = NativeMenu.GetMenu(window)!.Items.OfType<NativeMenuItem>()
                    .Single(item => Equals(item.Header, "File"));
                var openRecent = file.Menu!.Items.OfType<NativeMenuItem>()
                    .Single(item => Equals(item.Header, "Open Recent"));
                if (rows.Items.OfType<ListBoxItem>().All(item => !Equals(item.Content, recentPath)) ||
                    openRecent.Menu!.Items.OfType<NativeMenuItem>().All(item => !Equals(item.Header, recentPath)))
                    throw new InvalidOperationException("Stored recent file is missing: rows=" +
                        string.Join(",", rows.Items.OfType<ListBoxItem>().Select(item => item.Content)) +
                        " menu=" + string.Join(",", openRecent.Menu!.Items.OfType<NativeMenuItem>().Select(item => item.Header)));
            }
            finally
            {
                window.Close();
                Directory.Delete(root, recursive: true);
            }
        });

        DesktopChecks.Check("Recent_OpenedOutcome_AppendsPath", () =>
        {
            string root = ScratchPath("cfdw-d3a-open-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "opened.foil");
            File.Copy(ExamplePath(), path);
            var preferences = new PreferenceStore(Path.Combine(root, "preferences"), () => new ProjectStore());
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller, preferences);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                bool recentLoaded = false;
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                host.RecentLoaded += entries =>
                {
                    recentLoaded = entries.Any(entry => entry.Path == path);
                    if (recentLoaded) timeout.Cancel();
                };
                var open = host.OpenFileAsync(path);
                Avalonia.Threading.Dispatcher.UIThread.MainLoop(timeout.Token);
                if (!open.IsCompletedSuccessfully || controller.Inspection is null || !recentLoaded)
                    throw new InvalidOperationException("D2 Opened did not add the file to P1 recent rows");
            }
            finally
            {
                window.Close();
                Directory.Delete(root, recursive: true);
            }
        });

        DesktopChecks.Check("Architecture_DockConfinedToShell", () =>
        {
            string root = RepoRootFromSource();
            string desktop = Path.Combine(root, "src", "CfdWorkbench.Desktop");
            var offenders = Directory.EnumerateFiles(desktop, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}Shell{Path.DirectorySeparatorChar}"))
                .Where(path => File.ReadAllText(path).Contains("Dock.", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(root, path)).ToArray();
            if (offenders.Length != 0)
                throw new InvalidOperationException("Dock reference outside Shell/: " + string.Join(", ", offenders));
        });

        DesktopChecks.Check("NativeMenu_MainWindow_BuiltFromTable", () =>
        {
            var window = new Window();
            var menu = NativeMenuBuilder.BuildForWindow(window);
            var expected = CommandTable.Rows.Select(row => row.Title).ToHashSet(StringComparer.Ordinal);
            static IEnumerable<NativeMenuItem> Items(NativeMenu? level) =>
                level?.Items.OfType<NativeMenuItem>().SelectMany(item => Items(item.Menu).Prepend(item)) ?? [];
            var actual = menu.Items.OfType<NativeMenuItem>().SelectMany(item => Items(item.Menu))
                .Select(item => item.Header?.ToString() ?? "").ToHashSet(StringComparer.Ordinal);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException("Native menu lacks: " + string.Join(", ", expected.Except(actual)));
            if (!ReferenceEquals(NativeMenu.GetMenu(window), menu))
                throw new InvalidOperationException("Menu was not installed on the window");
            var file = menu.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "File"));
            var newFoil = file.Menu!.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "New foil"));
            if (!newFoil.IsEnabled)
                throw new InvalidOperationException("New foil is disabled after the controller seam joined");
            var edit = menu.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "Edit"));
            var undo = edit.Menu!.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "Undo"));
            var expectedModifier = OperatingSystem.IsMacOS() ? Avalonia.Input.KeyModifiers.Meta : Avalonia.Input.KeyModifiers.Control;
            if (undo.Gesture?.Key != Avalonia.Input.Key.Z || undo.Gesture.KeyModifiers != expectedModifier)
                throw new InvalidOperationException("Native undo shortcut does not match the platform");
        });

        DesktopChecks.Check("KeyBindings_MenuGesture_NotBound", () =>
        {
            var window = new MainWindow();
            try
            {
                var exported = CommandTable.Rows.Select(row => NativeMenuBuilder.ParseGesture(row.Gesture))
                    .Where(gesture => gesture is not null).ToArray();
                if (exported.Length == 0)
                    throw new InvalidOperationException("Command table exported no menu gestures");
                if (window.KeyBindings.Any(binding => binding.Gesture is { } bound &&
                    exported.Any(menu => menu!.Key == bound.Key && menu.KeyModifiers == bound.KeyModifiers)))
                    throw new InvalidOperationException("A native menu gesture was also bound on the window");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Menu_UndoEnabled_FollowsFocusAndHistory", () =>
        {
            var window = new MainWindow();
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var edit = NativeMenu.GetMenu(window)!.Items.OfType<NativeMenuItem>()
                    .Single(item => Equals(item.Header, "Edit"));
                var undo = edit.Menu!.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "Undo"));
                var redo = edit.Menu.Items.OfType<NativeMenuItem>().Single(item => Equals(item.Header, "Redo"));
                if (undo.IsEnabled || redo.IsEnabled)
                    throw new InvalidOperationException("Empty document enabled Undo or Redo");
                var controller = host.Controller;
                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                controller.ApplySpan("900");
                host.RefreshPanes();
                var viewport = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                viewport.Focus();
                Settle(window);
                if (!controller.CanUndo || !undo.IsEnabled || redo.IsEnabled)
                    throw new InvalidOperationException("Accepted edit did not enable document Undo alone");
                var span = host.Properties.FindControl<TextBox>("SpanInput")!;
                span.Focus();
                Settle(window);
                if (undo.IsEnabled != span.CanUndo || redo.IsEnabled != span.CanRedo)
                    throw new InvalidOperationException("Menu did not follow focused text history");
                viewport.Focus();
                controller.Undo();
                Settle(window);
                if (undo.IsEnabled || !redo.IsEnabled || !controller.CanRedo)
                    throw new InvalidOperationException("Undo did not flip document menu history");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Unhandled_Exception_StderrHasNoMarkerPath", () =>
        {
            string marker = "/tmp/CFDW-PRIVATE-" + Guid.NewGuid().ToString("N");
            var info = SelfLaunch.StartInfo(SelfLaunchTests.FailureProbe + "=" + marker);
            info.RedirectStandardError = true;
            using var child = Process.Start(info)!;
            string stderr = child.StandardError.ReadToEnd();
            if (!child.WaitForExit(TimeSpan.FromSeconds(30)))
            {
                child.Kill(entireProcessTree: true);
                throw new InvalidOperationException("Failure probe did not exit");
            }
            if (child.ExitCode != StartupFailure.ExitCode ||
                !stderr.Contains($"{StartupFailure.Code} {StartupFailure.FailureCode} System.InvalidOperationException", StringComparison.Ordinal) ||
                stderr.Contains(marker, StringComparison.Ordinal))
                throw new InvalidOperationException("Unhandled stderr leaked the marker or lost its exit contract");
        });

        DesktopChecks.Check("Telemetry_MarkerInjection_AbsentEverywhere", () =>
        {
            string root = ScratchPath("cfdw-private-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string marker = "CFDW-PRIVATE-" + Guid.NewGuid().ToString("N");
            var preferences = new PreferenceStore(Path.Combine(root, "preferences"), () => new ProjectStore());
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller, preferences);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            var priorError = Console.Error;
            using var capturedError = new StringWriter();
            try
            {
                Console.SetError(capturedError);
                window.Show();
                Settle(window);
                foreach (string name in new[] { marker + ".foil", "win\\" + marker + ".foil" })
                {
                    string path = Path.Combine(root, name);
                    File.Copy(ExamplePath(), path);
                    var open = host.OpenFileAsync(path);
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    while (!open.IsCompleted && !timeout.IsCancellationRequested)
                        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    open.GetAwaiter().GetResult();
                }
                RecentLoad recent;
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
                {
                    do
                    {
                        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                        recent = preferences.LoadRecentAsync(CancellationToken.None).GetAwaiter().GetResult();
                    }
                    while (recent.Entries.Count == 0 && !timeout.IsCancellationRequested);
                }
                if (controller.Inspection is null || recent.Entries.Count == 0)
                    throw new InvalidOperationException("Marker probe did not open and record the file");
                string emitted = JsonSerializer.Serialize(controller.LocalEvents) +
                    JsonSerializer.Serialize(ShellEvents.Read()) +
                    JsonSerializer.Serialize(new { recent.Outcome, recent.Codes, recent.NeverWrite, recent.SessionOnly }) +
                    capturedError;
                if (emitted.Contains(marker, StringComparison.Ordinal))
                    throw new InvalidOperationException("Marker appeared in telemetry or stderr");
            }
            finally
            {
                Console.SetError(priorError);
                window.Close();
                Directory.Delete(root, recursive: true);
            }
        });

        DesktopChecks.Check("KeyBindings_F6InFloat_Bound", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var floatWindow = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                ShellHost.BindF6(floatWindow, host);
                if (!floatWindow.KeyBindings.Any(binding => binding.Gesture?.Key == Avalonia.Input.Key.F6 &&
                    binding.Gesture.KeyModifiers == Avalonia.Input.KeyModifiers.None) ||
                    !floatWindow.KeyBindings.Any(binding => binding.Gesture?.Key == Avalonia.Input.Key.F6 &&
                    binding.Gesture.KeyModifiers == Avalonia.Input.KeyModifiers.Shift))
                    throw new InvalidOperationException("Float window lacks both F6 region bindings");
            }
            finally { floatWindow.Close(); }
        });

        DesktopChecks.Check("Palette_Keyboard_FiltersAndRuns", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                string? invoked = null;
                host.PaletteCommand += id => invoked = id;
                host.OpenPalette();
                var search = host.PaletteSearch;
                search.Text = "New foil";
                if (!search.IsFocused || host.PaletteMatches.Count != 1 ||
                    host.PaletteMatches[0].Id != "file.new")
                    throw new InvalidOperationException("Palette did not focus and filter to New foil");
                search.RaiseEvent(new Avalonia.Input.KeyEventArgs
                {
                    RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
                    Source = search,
                    Key = Avalonia.Input.Key.Enter
                });
                if (invoked != "file.new" || host.PaletteVisible)
                    throw new InvalidOperationException("Palette Enter did not run and close the command");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("DockTabFocus_FreshBatch_ReadyAndTwoRing", () =>
        {
            var window = new MainWindow() { Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var reflection = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var controller = typeof(MainWindow).GetField("workbench", reflection)?.GetValue(window) as WorkbenchController
                    ?? throw new InvalidOperationException("Shell controller field is unreadable");
                var host = window.Content as ShellHost
                    ?? throw new InvalidOperationException("Shell host did not load");
                var tab = host.DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>()
                    .FirstOrDefault(item => ReferenceEquals(item.DataContext, host.LayoutFactory.SectionDocument))
                    ?? throw new InvalidOperationException("Model Dock tab did not render");
                var visual = ElementComposition.GetElementVisual(tab)
                    ?? throw new InvalidOperationException("Dock tab lacks composition visual");
                var early = visual.Compositor.RequestCompositionBatchCommitAsync();
                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                host.RefreshPanes();
                Settle(window);
                if (!ReferenceEquals(ElementComposition.GetElementVisual(tab)?.Compositor, visual.Compositor))
                    throw new InvalidOperationException("Dock tab lost its compositor after the fixture opened");
                if (!tab.Focus(NavigationMethod.Tab))
                    throw new InvalidOperationException("Dock tab refused keyboard focus");
                Settle(window);
                var layer = AdornerLayer.GetAdornerLayer(tab)
                    ?? throw new InvalidOperationException("Dock tab has no adorner layer");
                var adorner = layer.Children.OfType<Control>().SingleOrDefault(child =>
                    ReferenceEquals(AdornerLayer.GetAdornedElement(child), tab))
                    ?? throw new InvalidOperationException("Dock tab has no focus adorner");
                var rings = adorner.GetVisualDescendants().OfType<Border>()
                    .Prepend(adorner as Border).Where(border => border?.BorderBrush is not null).ToArray();
                if (rings.Length < 2)
                    throw new InvalidOperationException("Dock tab lacks the two focus rings");
                var fresh = visual.Compositor.RequestCompositionBatchCommitAsync();
                if (ReferenceEquals(early, fresh))
                    throw new InvalidOperationException("Stale batch reused for Dock focus barrier");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!fresh.Rendered.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                if (!fresh.Rendered.IsCompletedSuccessfully || !tab.IsFocused || tab.Bounds.Width <= 0 || adorner.Bounds.Width <= 0)
                    throw new InvalidOperationException("Fresh Dock focus batch did not render a visible focused tab");
            }
            finally
            {
                var approval = typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Shell close approval field is unreadable");
                approval.SetValue(window, true);
                window.Close();
            }
        });

        DesktopChecks.Check("Review_Persona_FocusesShellRegion", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.FocusForPersona("keyboard");
                Settle(window);
                if (!host.ModelView.FindControl<StartView>("StartCardView")!
                    .FindControl<Button>("StartOpenButton")!.IsFocused)
                    throw new InvalidOperationException("Keyboard persona missed Start Open");
                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                Settle(window);
                host.FocusForPersona("designer");
                Settle(window);
                if (!host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.IsFocused)
                    throw new InvalidOperationException("Designer persona missed the viewport");
                host.FocusForPersona("screen-reader");
                Settle(window);
                if (!host.Browser.FindControl<ListBox>("StationList")!.Items.OfType<ListBoxItem>().Any(item => item.IsFocused))
                    throw new InvalidOperationException("Screen-reader persona missed Browser rows");
                host.FocusForPersona("dense");
                Settle(window);
                if (!host.Browser.FindControl<ListBox>("LeadingEdgeList")!.Items.OfType<ListBoxItem>().Any(item => item.IsFocused))
                    throw new InvalidOperationException("Dense persona missed rail controls");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("ShellHost_PlanformLayout_ContainsModelAndSidePanes", () =>
        {
            using var controller = new WorkbenchController();
            ShellHost host;
            try { host = new ShellHost(controller); }
            catch (Exception error) { throw new InvalidOperationException(error.ToString(), error); }
            var ids = host.LayoutFactory.MainDocumentDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (!ids.SequenceEqual(["model", "section", "foil-source"]))
                throw new InvalidOperationException("Model area document tabs are absent");
            var paneIds = host.LayoutFactory.LeftToolDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (!paneIds.Contains("properties") || !paneIds.Contains("browser") || !paneIds.Contains("layers"))
                throw new InvalidOperationException("Planform side bar is incomplete");
            if (host.DockHost.Layout is null || host.DockHost.Factory is null)
                throw new InvalidOperationException("Dock host is not connected to its layout");
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                window.UpdateLayout();
                if (!host.DockHost.IsVisible)
                    throw new InvalidOperationException("Dock host did not render in the window");
                foreach (var document in new[]
                {
                    host.LayoutFactory.SectionDocument,
                    host.LayoutFactory.FoilSourceDocument,
                    host.LayoutFactory.ModelDocument
                })
                {
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    window.UpdateLayout();
                }
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("ModelArea_SamplesTabRetired_NoReferencesRemain", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var ids = host.LayoutFactory.MainDocumentDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (ids.Contains("3d-samples") || host.LayoutFactory.FindDockable("3d-samples") is not null)
                throw new InvalidOperationException("The 3D samples document still exists: " + string.Join(", ", ids));
            if (host.ModelView.FindControl<Control>("Plan3DContent") is not null || host.ModelView.FindControl<Control>("FoilViewport") is not null)
                throw new InvalidOperationException("The 3D samples body is still in the model area");
            // The retired names must not reappear in the Desktop source: the document, its body, its viewport, its provenance line,
            // the Viewport 3D mode and the inspection semantics only that mode used. The old Viewport flag is matched by its
            // usage (attribute, bool property, viewport read), because M1.2c reuses the name SectionMode for the editor mode.
            string root = RepoRootFromSource();
            var retired = new System.Text.RegularExpressions.Regex(
                @"SamplesDocument|Plan3DContent|FoilViewport|ViewportProvenance|3d-samples|3D samples|SectionMode=""|bool SectionMode\b|viewport\.SectionMode\b|FromInspection|SectionSampleDocument|SectionSampleBody|SectionViewport|SectionReadout|section-sample");
            var hits = Directory.EnumerateFiles(Path.Combine(root, "src", "CfdWorkbench.Desktop"), "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".axaml", StringComparison.Ordinal))
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .Where(file => retired.IsMatch(File.ReadAllText(file))).Select(Path.GetFileName).ToArray();
            if (hits.Length > 0) throw new InvalidOperationException("Retired 3D samples names remain in: " + string.Join(", ", hits));
        });

        DesktopChecks.Check("ShellHost_AcceptedExample_BindsPanes", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                window.UpdateLayout();
                if (!host.ModelView.FindControl<Control>("PlanContent")!.IsVisible ||
                    host.Browser.FindControl<ListBox>("StationList")!.ItemCount == 0 ||
                    !host.Properties.FindControl<Control>("ContentPanel")!.IsVisible ||
                    !host.Properties.FindControl<Control>("WingBlock")!.IsVisible)
                    throw new InvalidOperationException("Accepted foil did not reach the model and pane surfaces");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Browser_AcceptedIdentity_KeepsOrReplacesRows", () =>
        {
            var original = new ListBoxItem { Content = "root station" };
            var list = new ListBox { ItemsSource = new[] { original } };
            var source = list.ItemsSource;
            BrowserPane.BindStations(list, [new ListBoxItem { Content = "selection refresh" }], acceptedChanged: false);
            if (!ReferenceEquals(list.ItemsSource, source) || !ReferenceEquals(list.Items[0], original))
                throw new InvalidOperationException("Selection refresh replaced a Browser row");
            BrowserPane.BindStations(list, [new ListBoxItem { Content = "new accepted foil" }], acceptedChanged: true);
            if (ReferenceEquals(list.ItemsSource, source) || Equals((list.Items[0] as ListBoxItem)?.Content, "root station"))
                throw new InvalidOperationException("New accepted identity retained stale Browser rows");
        });

        DesktopChecks.Check("Controller_LockedRailControl_RefusesDraft", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            window.Show();
            Settle(window);
            var root = controller.Planform!.Leading.Points.First(point => point.Role == PointRole.RootEnd);
            controller.Select(new Selection.Points(new[] { new PointRef(root.Curve, root.Id) }));
            Settle(window);
            if (controller.BeginGesture(new PointRef(root.Curve, root.Id), GestureInput.Pointer))
                throw new InvalidOperationException("Locked leading root accepted a gesture");
            var span = host.Properties.FindControl<TextBox>("PointSpanInput");
            if (controller.Draft is not null || span is not { IsEnabled: false })
                throw new InvalidOperationException("Locked root enabled a span draft");
            window.Close();
        });

        DesktopChecks.Check("F6_RegionEntry_FocusesSelectedTabOrRow", () =>
        {
            if (FocusRing.NextRegionIndex(0, reverse: false, [true, true, false, true]) != 1)
                throw new InvalidOperationException("F6 ring did not skip an unavailable region");
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }
                host.LayoutFactory.LeftToolDock.ActiveDockable = host.LayoutFactory.BrowserTool;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }
                var row = host.Browser.FindControl<ListBox>("StationList")!.Items.OfType<ListBoxItem>().First();
                if (!row.Focus()) throw new InvalidOperationException("Browser row cannot take focus");
                host.MoveFocus(reverse: false);
                if (!host.DockHost.GetVisualDescendants().OfType<Control>()
                    .Any(control => control.GetType().Name == "DocumentTabStripItem" && control.IsFocused))
                    throw new InvalidOperationException("F6 did not focus the selected Dock document tab: " +
                        string.Join(", ", host.DockHost.GetVisualDescendants().OfType<Control>()
                            .Where(control => control.GetType().Name == "DocumentTabStripItem")
                            .Select(control => $"{control.IsVisible}/{control.IsEnabled}/{control.IsFocused}/{control.Focusable}/{control.GetType().GetProperty("IsActive")?.GetValue(control)}/{control.DataContext?.GetType().Name}")));
                host.MoveFocus(reverse: true);
                if (!row.IsFocused)
                    throw new InvalidOperationException("Shift+F6 did not focus the Browser row");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("PaneBind_Throws_ShowsErrorStateClearsOld", () =>
        {
            var properties = new PropertiesPane();
            properties.FindControl<StackPanel>("BlocksPanel")!.Children.Add(new TextBlock { Text = "stale property" });
            properties.Bind(null!);
            if (!properties.FindControl<Control>("ErrorPanel")!.IsVisible ||
                properties.FindControl<Control>("ContentPanel")!.IsVisible ||
                properties.FindControl<StackPanel>("BlocksPanel")!.Children.Count != 0)
                throw new InvalidOperationException("Properties pane retained stale content after a bind failure");
            var browser = new BrowserPane();
            browser.FindControl<ListBox>("StationList")!.ItemsSource = new[] { new ListBoxItem { Content = "stale row" } };
            browser.Bind(null!);
            if (!browser.FindControl<Control>("ErrorPanel")!.IsVisible ||
                browser.FindControl<ListBox>("StationList")!.ItemCount != 0)
                throw new InvalidOperationException("Browser pane retained stale rows after a bind failure");
        });

        DesktopChecks.Check("EditVerb_UndoInSpanField_EditsText", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }
                var span = host.Properties.FindControl<TextBox>("SpanInput")!;
                string before = span.Text ?? "";
                string source = controller.AcceptedSource;
                if (before.Length == 0 || !span.Focus())
                    throw new InvalidOperationException("Span field was not ready for text input");
                span.SelectAll();
                span.RaiseEvent(new Avalonia.Input.TextInputEventArgs
                {
                    RoutedEvent = Avalonia.Input.InputElement.TextInputEvent,
                    Source = span,
                    Text = "5"
                });
                if (span.Text != "5") throw new InvalidOperationException("Span text input was not applied");
                if (!host.RouteEditVerb("undo", span) || span.Text != before || controller.AcceptedSource != source)
                    throw new InvalidOperationException("Undo in Span edited the foil or missed the text field");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_Opened_FocusModelArea", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                host.HandleOpenOutcome(new OpenOutcome.Opened("example.foil"), "example.foil");
                Settle(window);
                if (!host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.IsFocused)
                    throw new InvalidOperationException("Opened foil did not focus the model area");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_NewFoil_FocusModelArea", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                if (!host.ModelView.FindControl<StartView>("StartCardView")!.FindControl<Button>("StartNewButton")!.IsEnabled)
                    throw new InvalidOperationException("New foil card is disabled");
                var task = host.OpenNewFoilAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!task.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                task.GetAwaiter().GetResult();
                Settle(window);
                if (controller.Inspection is null || controller.OpenedPath is not null ||
                    !host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.IsFocused)
                    throw new InvalidOperationException("New foil did not open an unsaved foil and focus the model area");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_ExampleFixtureMissing_NamesFixtureNewFoilAvailable", () =>
        {
            var start = new StartView();
            start.ShowAlert("example.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "example.foil"), isMissingFixture: true);
            if (StartControl<TextBlock>(start, "AlertTitle").Text?.Contains("example.foil", StringComparison.Ordinal) != true ||
                !StartControl<Button>(start, "AlertNewFoilButton").IsVisible)
                throw new InvalidOperationException("Missing example alert omitted fixture name or New foil action");
        });

        DesktopChecks.Check("Open_IdCandidate_AcceptThroughShell_Opens", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var open = host.OpenFileAsync("docs/examples/foildsl/foil-comment.foil");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                var start = host.ModelView.FindControl<StartView>("StartCardView")!;
                var accept = StartControl<Button>(start, "AlertAcceptIdsButton");
                if (!accept.IsVisible) throw new InvalidOperationException("ID candidate has no Accept action on Start");
                accept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                using var acceptTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (StartControl<Border>(start, "AlertPanel").IsVisible && !acceptTimeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Settle(window);
                if (controller.Inspection is null || !host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.IsVisible ||
                    StartControl<Border>(start, "AlertPanel").IsVisible)
                    throw new InvalidOperationException($"Accept through shell did not open: inspection={controller.Inspection is not null}, plan={host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.IsVisible}, alert={StartControl<Border>(start, "AlertPanel").IsVisible}, pending={controller.PendingCandidate is not null}, status={controller.Status}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Open_Refused_ReturnsToOriginWithAlert", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            var parsed = FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/invalid-geometry.foil"));
            byte[] original = FoilSource.MaterializeIds(parsed);
            string path = ScratchPath($"openfix-refused-{Guid.NewGuid():N}.foil");
            File.WriteAllBytes(path, original);
            try
            {
                window.Show();
                Settle(window);
                var open = host.OpenFileAsync(path);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                var start = host.ModelView.FindControl<StartView>("StartCardView")!;
                string expected = CopyRow(DesignCopyRows(), "COPY-141")
                    .Replace("<file>", Path.GetFileName(path), StringComparison.Ordinal);
                if (!start.IsVisible || !StartControl<Border>(start, "AlertPanel").IsVisible ||
                    StartAlertLine(start) != expected ||
                    !StartControl<Button>(start, "AlertDismissButton").IsFocused ||
                    controller.Inspection is not null || controller.PendingOriginal is null ||
                    !controller.PendingOriginal.AsSpan().SequenceEqual(original) || controller.PendingCandidate is not null)
                    throw new InvalidOperationException("Refused open lost Start, alert focus, or read-only original");

                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                host.RefreshPanes();
                string accepted = controller.AcceptedSource;
                open = host.OpenFileAsync(path);
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                if (start.IsVisible || !host.ModelView.FindControl<Border>("AlertBand")!.IsVisible ||
                    BandLine(host.ModelView) != expected ||
                    !host.ModelView.FindControl<Button>("DismissAlertBandButton")!.IsFocused ||
                    controller.AcceptedSource != accepted || controller.PendingOriginal is null ||
                    !controller.PendingOriginal.AsSpan().SequenceEqual(original))
                    throw new InvalidOperationException("Refused open replaced the workspace or lost its alert and original");
            }
            finally { window.Close(); File.Delete(path); }
        });

        DesktopChecks.Check("Open_NeedsIds_ReturnsToOriginWithAlert", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var open = host.OpenFileAsync("docs/examples/foildsl/foil-comment.foil");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                var start = host.ModelView.FindControl<StartView>("StartCardView")!;
                string expected = CopyRow(DesignCopyRows(), "COPY-140")
                    .Replace("<file>", "foil-comment.foil", StringComparison.Ordinal);
                if (!start.IsVisible || !StartControl<Border>(start, "AlertPanel").IsVisible ||
                    StartAlertLine(start) != expected ||
                    !StartControl<Button>(start, "AlertAcceptIdsButton").IsFocused ||
                    controller.Inspection is not null || controller.PendingCandidate is null)
                    throw new InvalidOperationException("ID candidate lost Start, alert focus, or pending candidate");

                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                host.RefreshPanes();
                string accepted = controller.AcceptedSource;
                open = host.OpenFileAsync("docs/examples/foildsl/foil-comment.foil");
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                if (start.IsVisible || !host.ModelView.FindControl<Border>("AlertBand")!.IsVisible ||
                    BandLine(host.ModelView) != expected ||
                    !host.ModelView.FindControl<Button>("AcceptIdsButton")!.IsFocused ||
                    controller.AcceptedSource != accepted || controller.PendingCandidate is null)
                    throw new InvalidOperationException("ID candidate replaced the workspace or lost its alert");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_ClosePane_NextTab", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.Properties.FindControl<TextBox>("SpanInput")!.Focus();
                host.ClosePane("properties");
                Settle(window);
                if (!FocusedToolTab(host, "browser"))
                    throw new InvalidOperationException("Closing Properties did not focus the next tool tab");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_WindowPanesShow_PaneTab", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.ClosePane("properties");
                Settle(window);
                host.ShowPane("properties");
                Settle(window);
                if (!FocusedToolTab(host, "properties"))
                    throw new InvalidOperationException("Window ▸ Panes did not restore and focus Properties");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_HideDockHoldingFocus_ToToggle", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                if (!host.Properties.FindControl<TextBox>("SpanInput")!.Focus())
                    throw new InvalidOperationException("Properties field cannot take focus");
                host.ToggleLeftSidebar();
                Settle(window);
                if (!host.LeftSidebarToggle.IsFocused)
                    throw new InvalidOperationException("Hiding the focused dock lost its focus target");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_CloseLastPane_DockToggle", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.ClosePane("properties");
                Settle(window);
                host.ClosePane("browser");
                Settle(window);
                host.ClosePane("layers");
                Settle(window);
                if (!host.LeftSidebarToggle.IsFocused)
                    throw new InvalidOperationException("Closing the final tool pane did not focus the dock toggle");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_SizeMenu_ReturnsToTab", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.SetPaneSize("properties", "Wide");
                Settle(window);
                if (Math.Abs(host.LayoutFactory.LeftToolDock.Proportion - 0.35) > 1e-6 ||
                    !FocusedToolTab(host, "properties"))
                    throw new InvalidOperationException("Size menu did not resize the tool dock and return focus");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_MenuTab_ClosesMenuReturns", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var tab = host.DockHost.GetVisualDescendants().OfType<ToolTabStripItem>()
                    .First(item => ReferenceEquals(item.DataContext, host.LayoutFactory.PropertiesTool));
                if (tab.ContextMenu is not { } menu || !tab.Focus(NavigationMethod.Tab))
                    throw new InvalidOperationException("Properties tab menu or keyboard focus is missing");
                menu.Open(tab);
                Settle(window);
                if (!menu.IsOpen)
                    throw new InvalidOperationException("Properties tab menu did not open");
                menu.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Source = menu,
                    Key = Key.Escape
                });
                Settle(window);
                if (menu.IsOpen || !tab.IsFocused)
                    throw new InvalidOperationException("Closing the tab menu did not restore tab focus");
                menu.Open(tab);
                var close = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Close"));
                close.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                menu.Close();
                Settle(window);
                if (!FocusedToolTab(host, "browser"))
                    throw new InvalidOperationException("Menu Close returned focus to the removed tab");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("SidebarToggle_LiveThemeSwitch_RefreshesBrush", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700,
                RequestedThemeVariant = ThemeVariant.Light };
            try
            {
                window.Show();
                Settle(window);
                var light = ((ISolidColorBrush)host.LeftSidebarToggle.Background!).Color;
                window.RequestedThemeVariant = ThemeVariant.Dark;
                Settle(window);
                var dark = ((ISolidColorBrush)host.LeftSidebarToggle.Background!).Color;
                if (light == dark)
                    throw new InvalidOperationException($"Sidebar toggle kept the old theme brush: {light} / {dark}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_MoveTo_StaysOnMovedTab", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.MovePane("browser", "LeftDock");
                Settle(window);
                if (host.LayoutFactory.LeftToolDock.VisibleDockables?.Count(item => item.Id == "browser") != 1 ||
                    !FocusedToolTab(host, "browser"))
                    throw new InvalidOperationException("Move to left lost the Browser tab or its focus");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_DockRerender_NeverWindowRoot", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var span = host.Properties.FindControl<TextBox>("SpanInput")!;
                if (!span.Focus()) throw new InvalidOperationException("Span field cannot take focus");
                host.RefreshPanes();
                Settle(window);
                if (!span.IsFocused || window.IsFocused)
                    throw new InvalidOperationException("Dock rerender moved focus to the window root");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_SpanInvalid_StaysInFieldWithAlert", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var span = host.Properties.FindControl<TextBox>("SpanInput")!;
                span.Focus();
                span.Text = "-";
                if (host.Properties.CommitSpan() || !span.IsFocused ||
                    !host.Properties.FindControl<Control>("SpanErrorPanel")!.IsVisible)
                    throw new InvalidOperationException("Invalid Span did not retain field focus with an error");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_SpanCommitTab_NextField", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var span = host.Properties.FindControl<TextBox>("SpanInput")!;
                span.Focus();
                string before = controller.AcceptedSource;
                span.Text = "1000";
                span.RaiseEvent(new Avalonia.Input.KeyEventArgs
                {
                    RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
                    Source = span,
                    Key = Avalonia.Input.Key.Tab
                });
                Settle(window);
                if (controller.AcceptedSource == before || span.IsFocused || window.FocusManager?.GetFocusedElement() is not Control)
                    throw new InvalidOperationException($"Tab did not commit Span and advance focus: changed={controller.AcceptedSource != before}, spanFocused={span.IsFocused}, focus={window.FocusManager?.GetFocusedElement()?.GetType().Name ?? "none"}, text={span.Text}, status={controller.Status}, error={host.Properties.FindControl<Control>("SpanErrorPanel")!.IsVisible}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_BrowserEnter_StaysOnRowSelectsStation", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                host.LayoutFactory.LeftToolDock.ActiveDockable = host.LayoutFactory.BrowserTool;
                Settle(window);
                var rows = host.Browser.FindControl<ListBox>("StationList")!;
                var row = rows.Items.OfType<ListBoxItem>().Skip(1).First();
                rows.SelectedItem = row;
                if (!row.Focus()) throw new InvalidOperationException("Browser row cannot take focus");
                row.RaiseEvent(new Avalonia.Input.KeyEventArgs
                {
                    RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
                    Source = row,
                    Key = Avalonia.Input.Key.Enter
                });
                if (!row.IsFocused || controller.Selection is not Selection.Station { Index: 1 })
                    throw new InvalidOperationException("Enter did not keep row focus and select the station");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_UndoFromCanvas_StaysOnCanvas", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            string before = controller.AcceptedSource;
            controller.ApplySpan("900");
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var viewport = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                if (!viewport.Focus()) throw new InvalidOperationException("Viewport cannot take focus");
                if (!host.RouteEditVerb("undo", viewport) || !viewport.IsFocused ||
                    controller.AcceptedSource != before)
                    throw new InvalidOperationException("Canvas Undo changed focus or missed document history");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Viewport_FocusVertex_RaisesFocusedTargetChanged", () =>
        {
            var viewport = new Viewport
            {
                Semantics = [new ViewportSemantic("cv-1", "upper control vertex cv-1", "editable")]
            };
            FocusedTargetEventArgs? raised = null;
            viewport.FocusedTargetChanged += (_, args) => raised = args;
            viewport.FocusVertex("cv-1");
            if (raised?.AccessibleName != "upper control vertex cv-1" ||
                raised.Bounds.Width <= 0 || raised.Bounds.Height <= 0)
                throw new InvalidOperationException("Viewport did not emit the focused semantic target");
            raised = null;
            viewport.FocusVertex("missing");
            if (raised is not null)
                throw new InvalidOperationException("Unknown viewport target emitted focus");
        });

        DesktopChecks.Check("SectionCanvas_FocusVertex_RaisesFocusedTargetChanged", () =>
        {
            var vertices = new[] { new ProfileVertex("upper", "u1", .25, .08, false) };
            var points = new[] { new ProfilePoint(.25, .08) };
            var canvas = new SectionCanvas
            {
                Width = 800,
                Height = 400,
                Profile = new ProfileView("section", "hash", vertices, [], points, [], "open")
            };
            FocusedTargetEventArgs? raised = null;
            canvas.FocusedTargetChanged += (_, args) => raised = args;
            canvas.FocusVertex("upper", "u1");
            if (canvas.SelectedVertex != ("upper", "u1") ||
                raised?.AccessibleName != "upper vertex u1" ||
                raised.Bounds.Width <= 0 || raised.Bounds.Height <= 0)
                throw new InvalidOperationException("SectionCanvas did not emit the selected vertex bounds");
            raised = null;
            canvas.FocusVertex("upper", "missing");
            if (raised is not null)
                throw new InvalidOperationException("Unknown section vertex emitted focus");
        });

        DesktopChecks.Check("Start_Opening_FocusOnCancel", () =>
        {
            var start = new StartView();
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                start.ShowOpening("foil.foil", StartControl<Button>(start, "StartOpenButton"));
                if (!StartControl<Control>(start, "OpeningPanel").IsVisible ||
                    !StartControl<Button>(start, "OpenCancelButton").IsFocused)
                    throw new InvalidOperationException("Opening did not focus its Cancel button");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_OpeningCancel_FocusReturnsToCard", () =>
        {
            var start = new StartView();
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                start.ShowOpening("foil.foil", StartControl<Button>(start, "StartOpenButton"));
                start.CancelOpening();
                if (StartControl<Control>(start, "OpeningPanel").IsVisible ||
                    !StartControl<Button>(start, "StartOpenButton").IsFocused)
                    throw new InvalidOperationException("Cancel did not return focus to the originating card action");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_CancelWithoutOrigin_FocusesFirstCard", () =>
        {
            var start = new StartView();
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                start.ShowOpening("sample.foil");
                start.CancelOpening();
                if (!StartControl<Button>(start, "StartNewButton").IsFocused)
                    throw new InvalidOperationException("Cancel without a card origin did not focus first start card");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_CancelFromRecent_FocusesOriginRow", () =>
        {
            var start = new StartView();
            start.PopulateRecent(["/example/folder/sample.foil"]);
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                var row = StartControl<ListBox>(start, "RecentListBox").Items.OfType<ListBoxItem>().Single();
                start.ShowOpening("sample.foil", row);
                start.CancelOpening();
                if (!row.IsFocused) throw new InvalidOperationException("Cancel did not focus the Recent origin row");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("AlertBand_AnnouncesAndFocusesFirstAction", () =>
        {
            var area = new ModelArea();
            var window = new Window { Content = area, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                area.ShowAlertBand("Open failed");
                var band = area.FindControl<Border>("AlertBand")!;
                var dismiss = area.FindControl<Button>("DismissAlertBandButton")!;
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(band) ==
                    Avalonia.Automation.AutomationLiveSetting.Off || !dismiss.IsFocused)
                    throw new InvalidOperationException("Alert band is silent or its first action lacks focus");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Span_Invalid_AnnouncedAndTabCanLeave", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var pane = new PropertiesPane();
            var after = new Button { Content = "After Span" };
            var window = new Window
            {
                Content = new StackPanel { Children = { pane, after } }, Width = 1024, Height = 700
            };
            try
            {
                window.Show();
                pane.Bind(controller);
                var input = pane.FindControl<TextBox>("SpanInput")!;
                input.Text = "invalid";
                if (pane.CommitSpan()) throw new InvalidOperationException("Invalid Span committed");
                var error = pane.FindControl<Border>("SpanErrorPanel")!;
                input.Focus();
                var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab };
                input.RaiseEvent(args);
                Settle(window);
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(error) ==
                    Avalonia.Automation.AutomationLiveSetting.Off || input.IsFocused)
                    throw new InvalidOperationException("Span error live=" +
                        Avalonia.Automation.AutomationProperties.GetLiveSetting(error) + " tabHandled=" + args.Handled +
                        " inputFocused=" + input.IsFocused);
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Recent_AccessibleName_FileAndFolder", () =>
        {
            var start = new StartView();
            start.PopulateRecent(["/example/folder/sample.foil"]);
            var row = StartControl<ListBox>(start, "RecentListBox").Items.OfType<ListBoxItem>().Single();
            if (Avalonia.Automation.AutomationProperties.GetName(row) != "sample.foil, folder" ||
                Avalonia.Automation.AutomationProperties.GetHelpText(row) != "/example/folder/sample.foil")
                throw new InvalidOperationException("Recent row name is not file plus folder with path description");
        });

        DesktopChecks.Check("Copy_OpenFailures_MatchesDesignRows", () =>
        {
            var rows = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"))
                .Where(line => line.StartsWith("| COPY-", StringComparison.Ordinal))
                .Select(line => line.Split('|', 4))
                .Where(parts => parts.Length >= 3)
                .ToDictionary(parts => parts[1].Trim(), parts => parts[2].Trim());
            var cases = new (string Row, OpenFailure Failure, bool FromRecent)[]
            {
                ("COPY-125", new OpenFailure.Missing("FILE-NOT-FOUND", "sample.foil"), true),
                ("COPY-126", new OpenFailure.AccessDenied("EACCES", "sample.foil"), false),
                ("COPY-127", new OpenFailure.Unreadable("DOC-IO", "sample.foil"), false),
                ("COPY-128", new OpenFailure.NotRecognised("DOC-TYPE", "sample.foil"), false),
                ("COPY-129", new OpenFailure.TooLarge("DSL-LIMIT", "sample.foil"), false),
                ("COPY-130", new OpenFailure.UnknownContent("DOC-UNSUPPORTED-FIELD", "sample.foil"), false),
                ("COPY-103", new OpenFailure.Newer("DOC-VERSION", "sample.foil"), false)
            };
            foreach (var item in cases)
            {
                var start = new StartView();
                start.ShowAlert("sample.foil", item.Failure, fromRecent: item.FromRecent);
                var actions = ((Panel)StartControl<Button>(start, "AlertLocateButton").Parent!)
                    .Children.OfType<Button>().Where(button => button.IsVisible && button.Name != "AlertDismissButton")
                    .Select(button => button.Content?.ToString()).ToArray();
                string actual = StartControl<TextBlock>(start, "AlertTitle").Text + " " +
                    StartControl<TextBlock>(start, "AlertMessage").Text +
                    (item.Row == "COPY-103" || actions.Length == 0 ? "" : " · " + string.Join(" · ", actions));
                string expected = rows[item.Row].Replace("<file>", "sample.foil", StringComparison.Ordinal)
                    .Replace("<limit>", "1 MiB", StringComparison.Ordinal);
                if (actual != expected)
                    throw new InvalidOperationException($"{item.Row}: expected '{expected}', built '{actual}'");
            }
        });

        DesktopChecks.Check("Copy_CancelOpening_ShowsStatus", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            host.HandleOpenOutcome(new OpenOutcome.Cancelled(), "sample.foil");
            var status = StatusStripTests.Text(host);
            if (status?.Text != "Opening cancelled. Nothing changed.")
                throw new InvalidOperationException("COPY-105 was not shown after Cancel");
        });

        DesktopChecks.Check("Copy_OpenFailureWithFoil_AlertBandMatchesStart", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                host.HandleOpenOutcome(new OpenOutcome.Failed(
                    new OpenFailure.Missing("FILE-NOT-FOUND", "/missing/sample.foil")), "/missing/sample.foil", fromRecent: true);
                Settle(window);
                var text = host.ModelView.FindControl<TextBlock>("AlertBandText")!.Text;
                var locate = host.ModelView.FindControl<Button>("BandLocateButton");
                if (text != "“sample.foil” didn't open. " + StartView.FailureMessage(
                        new OpenFailure.Missing("FILE-NOT-FOUND", "/missing/sample.foil"), "sample.foil") ||
                    locate?.IsVisible != true || !locate.IsFocused)
                    throw new InvalidOperationException($"Foil-open failure band: text='{text}', locate={locate?.IsVisible}, focused={locate?.IsFocused}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Copy_BrowserEmpty_SingleRendering", () =>
        {
            var browser = new BrowserPane();
            var empty = browser.FindControl<StackPanel>("EmptyPanel")!;
            var lines = empty.Children.OfType<TextBlock>().Select(line => line.Text).ToArray();
            if (lines.Length != 1 || lines[0] != "No foil open")
                throw new InvalidOperationException("Browser empty state has extra uncatalogued caption");
        });

        DesktopChecks.Check("Copy_PaneErrors_WithAndWithoutFoil", () =>
        {
            var properties = new PropertiesPane();
            var browser = new BrowserPane();
            var panes = new (string Name, Action<bool> Show, Func<string?> Text)[]
            {
                ("Properties", properties.ShowRenderFailure, () => properties.FindControl<TextBlock>("ErrorText")!.Text),
                ("Browser", browser.ShowRenderFailure, () => browser.FindControl<TextBlock>("ErrorText")!.Text)
            };
            foreach (var pane in panes)
                foreach (bool foilOpen in new[] { false, true })
                {
                    pane.Show(foilOpen);
                    string expected = pane.Name + " couldn't be shown." +
                        (foilOpen ? " Your foil hasn't changed." : "");
                    if (pane.Text() != expected)
                        throw new InvalidOperationException($"COPY-{(foilOpen ? 138 : 139)} mismatch for {pane.Name}");
                }
        });

        DesktopChecks.Check("Copy_SpanErrors_MatchDesignRows", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var pane = new PropertiesPane();
            pane.Bind(controller);
            var input = pane.FindControl<TextBox>("SpanInput")!;
            var error = pane.FindControl<TextBlock>("SpanErrorText")!;
            input.Text = "n/a";
            _ = pane.CommitSpan();
            if (error.Text != "Enter a number. Span is unchanged.")
                throw new InvalidOperationException("COPY-118 differs from built Span error");
            input.Text = "0";
            _ = pane.CommitSpan();
            if (error.Text != "Enter a length greater than 0 mm. Span is unchanged.")
                throw new InvalidOperationException("COPY-106 differs from built Span error");
        });

        DesktopChecks.Check("Span_NaNOrInfinity_InvalidNotNotAssessed", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            string source = controller.AcceptedSource;
            var pane = new PropertiesPane();
            pane.Bind(controller);
            var input = pane.FindControl<TextBox>("SpanInput")!;
            var error = pane.FindControl<TextBlock>("SpanErrorText")!;
            foreach (string value in new[] { "NaN", "Infinity", "-Infinity", "not-a-number" })
            {
                input.Text = value;
                bool committed = pane.CommitSpan();
                if (committed || error.Text != "Enter a number. Span is unchanged." ||
                    controller.AcceptedSource != source)
                    throw new InvalidOperationException($"{value} showed '{error.Text}' or changed the geometry");
            }
        });

        DesktopChecks.Check("Copy_SpanNotAssessed_MatchesDesignRow", () =>
        {
            // No certified foil: ApplySpan refuses with DSL-NOT-ASSESSED (WorkbenchController.RequireCertifiedFoil).
            using var controller = new WorkbenchController();
            var pane = new PropertiesPane();
            pane.Bind(controller);
            pane.FindControl<TextBox>("SpanInput")!.Text = "900";
            bool committed = pane.CommitSpan();
            string? built = pane.FindControl<TextBlock>("SpanErrorText")!.Text;
            string expected = CopyRow(DesignCopyRows(), "COPY-145");
            if (committed || built != expected || controller.Inspection is not null)
                throw new InvalidOperationException($"COPY-145: expected '{expected}', built '{built}', committed={committed}");
        });

        DesktopChecks.Check("Copy_StartAlert_NoUncataloguedFallback", () =>
        {
            // C-4: every OpenFailure has a COPY row, so ShowAlert takes no null failure and no free-text message.
            var show = typeof(StartView).GetMethod(nameof(StartView.ShowAlert))!;
            var failure = show.GetParameters().Single(parameter => parameter.ParameterType == typeof(OpenFailure));
            var nullability = new System.Reflection.NullabilityInfoContext().Create(failure);
            if (show.GetParameters().Any(parameter => parameter.Name == "customMessage") ||
                nullability.WriteState != System.Reflection.NullabilityState.NotNull)
                throw new InvalidOperationException("StartView.ShowAlert still accepts an uncatalogued fallback message");
        });

        DesktopChecks.Check("Copy_ExampleMissing_MatchesDesignRow", () =>
        {
            var start = new StartView();
            start.ShowAlert("example.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "example.foil"), isMissingFixture: true);
            string expected = CopyRow(DesignCopyRows(), "COPY-143").Replace("<file>", "example.foil", StringComparison.Ordinal);
            string built = StartAlertLine(start);
            if (built != expected)
                throw new InvalidOperationException($"COPY-143: expected '{expected}', built '{built}'");
        });

        DesktopChecks.Check("Copy_Dismiss_MatchesDesignRow", () =>
        {
            string expected = CopyRow(DesignCopyRows(), "COPY-144");
            var start = new StartView();
            var area = new ModelArea();
            string?[] built = [StartControl<Button>(start, "AlertDismissButton").Content?.ToString(),
                area.FindControl<Button>("DismissAlertBandButton")!.Content?.ToString()];
            if (built.Any(label => label != expected))
                throw new InvalidOperationException($"COPY-144: expected '{expected}', built '{string.Join("', '", built)}'");
        });

        DesktopChecks.Check("Copy_AlertBand_IdCandidateRefusedAcceptFailed_MatchDesignRows", () =>
        {
            var rows = DesignCopyRows();
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            host.HandleOpenOutcome(new OpenOutcome.NeedsIds([1], [2]), "/example/sample.foil");
            string candidate = BandLine(host.ModelView);
            host.HandleOpenOutcome(new OpenOutcome.Refused("DSL-SYNTAX", [2]), "/example/sample.foil");
            string refused = BandLine(host.ModelView);
            host.HandleOpenOutcome(new OpenOutcome.NeedsIds([1], [2]), "/example/sample.foil");
            // Nothing is pending in the controller on this path, so Accept candidate IDs fails and says COPY-142.
            host.ModelView.FindControl<Button>("AcceptIdsButton")!
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            string acceptFailed = BandLine(host.ModelView);
            var cases = new (string Row, string Built)[] { ("COPY-140", candidate), ("COPY-141", refused), ("COPY-142", acceptFailed) };
            foreach (var item in cases)
            {
                string expected = CopyRow(rows, item.Row).Replace("<file>", "sample.foil", StringComparison.Ordinal);
                if (item.Built != expected)
                    throw new InvalidOperationException($"{item.Row}: expected '{expected}', built '{item.Built}'");
            }
        });

        DesktopChecks.Check("Copy_RecentNotCleared_StatusAndTryAgain", () =>
        {
            var rows = DesignCopyRows();
            string root = ScratchPath("copyfix-clear-" + Guid.NewGuid().ToString("N"));
            try
            {
                // Newer list (COPY-147) and a failing write (COPY-148): both keep the list and offer Try again.
                string newerRoot = Path.Combine(root, "newer");
                Directory.CreateDirectory(Path.Combine(newerRoot, "recent"));
                string newerPath = Path.Combine(newerRoot, "recent", "recent.json");
                byte[] newer = System.Text.Encoding.UTF8.GetBytes("{\"format\":\"cfdw-recent\",\"version\":2,\"entries\":[]}");
                File.WriteAllBytes(newerPath, newer);
                string failRoot = Path.Combine(root, "fail");
                string kept = Path.Combine(failRoot, "kept.foil");
                var seed = new PreferenceStore(failRoot, () => new ProjectStore());
                seed.UpdateRecentAsync(new RecentOp.Add(kept), CancellationToken.None).GetAwaiter().GetResult();
                var cases = new (string Reason, PreferenceStore Store, Func<bool> Unchanged)[]
                {
                    ("COPY-147", new PreferenceStore(newerRoot, () => new ProjectStore()),
                        () => File.ReadAllBytes(newerPath).AsSpan().SequenceEqual(newer)),
                    ("COPY-148", new PreferenceStore(failRoot, () => new FailingSaveStore()),
                        () => seed.LoadRecentAsync(CancellationToken.None).GetAwaiter().GetResult().Entries.Single().Path == kept)
                };
                foreach (var item in cases)
                {
                    using var controller = new WorkbenchController();
                    var host = new ShellHost(controller, item.Store);
                    Pump(host.ClearRecentAsync());
                    string expected = CopyRow(rows, "COPY-146").Replace("<reason>", CopyRow(rows, item.Reason), StringComparison.Ordinal);
                    var status = StatusStripTests.Text(host);
                    var retry = StatusStripTests.Strip(host).FindControl<Button>("StatusTryAgainButton");
                    string built = status.Text + (retry?.IsVisible == true ? " · " + retry.Content : "");
                    if (built != expected || !status.IsVisible || !item.Unchanged())
                        throw new InvalidOperationException($"{item.Reason}: expected '{expected}', built '{built}', unchanged={item.Unchanged()}");
                }
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        });

        DesktopChecks.Check("OpenFailure_RemoveFromRecent_RemovesOnlyFailedPath", () =>
        {
            string root = ScratchPath("u1fix-remove-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string failed = Path.Combine(root, "missing.foil");
            string kept = Path.Combine(root, "kept.foil");
            var preferences = new PreferenceStore(root, () => new ProjectStore());
            preferences.UpdateRecentAsync(new RecentOp.Add(kept), CancellationToken.None).GetAwaiter().GetResult();
            preferences.UpdateRecentAsync(new RecentOp.Add(failed), CancellationToken.None).GetAwaiter().GetResult();
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller, preferences);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Settle(window);
                var open = host.OpenFileAsync(failed, fromRecent: true);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                var remove = host.ModelView.FindControl<StartView>("StartCardView")!
                    .FindControl<Button>("AlertRemoveRecentButton")!;
                if (!remove.IsVisible) throw new InvalidOperationException("Recent failure hid Remove from Recent");
                bool removed = false;
                host.RecentLoaded += entries =>
                {
                    removed = entries.Count == 1 && entries[0].Path == kept;
                    if (removed) timeout.Cancel();
                };
                remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Avalonia.Threading.Dispatcher.UIThread.MainLoop(timeout.Token);
                var entries = preferences.LoadRecentAsync(CancellationToken.None).GetAwaiter().GetResult().Entries;
                if (!removed || entries.Count != 1 || entries[0].Path != kept)
                    throw new InvalidOperationException("Remove from Recent did not retain only the other path");
            }
            finally
            {
                window.Close();
                Directory.Delete(root, recursive: true);
            }
        });

        DesktopChecks.Check("Start_OpenMissing_AlertLocate", () =>
        {
            var start = new StartView();
            start.ShowAlert("missing.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "missing.foil"));
            if (!StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "AlertLocateButton").IsVisible ||
                StartControl<TextBlock>(start, "AlertTitle").Text?.Contains("missing.foil", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Missing-file alert lacks its file name or Locate action");
        });

        DesktopChecks.Check("Start_OpenNewer_AlertOpenAnother", () =>
        {
            var start = new StartView();
            start.ShowAlert("newer.foil", new OpenFailure.Newer("FUTURE-VER", "newer.foil"));
            if (!StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "AlertOpenAnotherButton").IsVisible)
                throw new InvalidOperationException("Newer-file alert lacks Open another file");
        });

        DesktopChecks.Check("Start_OpenFailedDismissed_StartKept", () =>
        {
            var start = new StartView();
            start.ShowAlert("broken.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "broken.foil"));
            start.DismissAlert();
            if (StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "StartNewButton").IsVisible)
                throw new InvalidOperationException("Dismissing open failure did not keep the Start card");
        });

        foreach (string action in new[] { "AlertLocateButton", "AlertOpenAnotherButton", "AlertTryAgainButton" })
        {
            DesktopChecks.Check($"OpenFailure_{action}_OpensFile", () =>
            {
                string path = ExamplePath();
                int picks = 0;
                using var controller = new WorkbenchController();
                var host = new ShellHost(controller, pickOpenFile: () =>
                {
                    picks++;
                    return Task.FromResult<string?>(path);
                });
                var window = new Window { Content = host, Width = 1024, Height = 700 };
                try
                {
                    window.Show();
                    Settle(window);
                    var start = host.ModelView.FindControl<StartView>("StartCardView")!;
                    host.HandleOpenOutcome(new OpenOutcome.Failed(action == "AlertTryAgainButton"
                        ? new OpenFailure.Unreadable("DOC-IO", path)
                        : new OpenFailure.Missing("FILE-NOT-FOUND", path)), path);
                    StartControl<Button>(start, action).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    while (controller.Inspection is null && !timeout.IsCancellationRequested)
                        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    if (controller.Inspection is null || picks != (action == "AlertTryAgainButton" ? 0 : 1))
                        throw new InvalidOperationException($"{action} did not open its selected or original file");
                }
                finally { window.Close(); }
            });
        }

        // The applied-contrast matrix on the real shell window (design §12.4; inventory rows 113–1446). It is the gate's
        // theme evidence: tools/verify-application-adapters.py freezes its row set and re-derives every ratio.
        DesktopChecks.Check("ThemeMatrix_ShellControls_AppliedContrast", () =>
        {
            var rowFailures = new List<string>();
            var reflection = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var pseudoProperty = typeof(StyledElement).GetProperty("PseudoClasses", reflection)
                ?? throw new InvalidOperationException("Installed protected PseudoClasses unavailable");
            // Each row is one measured contrast on the real shell window; tools/verify-application-adapters.py re-derives
            // every ratio from the emitted ARGB and refuses a missing, duplicate or unknown row.
            var emittedRows = new List<(string Theme, string Row)>();
            if (PressScaleAccepted(Matrix.CreateScale(0.9, 0.9)) || PressScaleAccepted(Matrix.CreateScale(1.02, 1.02)) ||
                PressScaleAccepted(Matrix.CreateScale(0.98, 0.99)) || !PressScaleAccepted(Matrix.CreateScale(0.98, 0.98)) ||
                !PressScaleAccepted(Matrix.Identity))
                rowFailures.Add("pressed-scale bound: 0.9, 1.02 and non-uniform must be refused; 0.98 and 1 accepted");
            void Measure(string theme, string row, Color foreground, Color background, double floor)
            {
                double ratio = Contrast(foreground, background);
                emittedRows.Add((theme, row));
                Console.WriteLine(FormattableString.Invariant(
                    $"THEME-ROW {theme}/{row} fg=#{foreground.ToUInt32():X8} bg=#{background.ToUInt32():X8} ratio={ratio:F4} floor={floor}"));
                if (ratio < floor) rowFailures.Add($"{theme}/{row} {foreground} on {background} = {ratio:F2} < {floor}");
            }
            void Probe(string theme, string row, Action probe)
            {
                try { probe(); }
                catch (Exception error) { rowFailures.Add($"{theme}/{row}: {error.Message}"); }
            }
            void TextRow(string theme, string row, Control target)
            {
                var text = TextVisual(target);
                Measure(theme, row, Solid(PropertyBrush(text, "Foreground"), row + " ink"), Backing(text), 4.5);
            }
            void TextBoxRow(string theme, string row, TextBox box)
            {
                var presenter = box.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().Single();
                var border = box.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_BorderElement");
                Measure(theme, row, Solid(presenter.Foreground, row + " ink"), Backdrop(box, border, row + " backdrop"), 4.5);
            }
            void RingRow(string theme, string row, Control target, Window window)
            {
                Settle(window);
                var outside = target.GetVisualParent() ?? throw new InvalidOperationException("Focus target has no parent");
                if (target is TextBox box)
                {
                    // Fluent TextBox has no focus adorner; its focus indicator is the PART_BorderElement stroke.
                    var stroke = box.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_BorderElement");
                    Measure(theme, row, Solid(stroke.BorderBrush, row + " stroke"), Backing(outside), 3);
                    return;
                }
                var layer = AdornerLayer.GetAdornerLayer(target) ?? throw new InvalidOperationException("No adorner layer");
                var adorner = layer.Children.OfType<Control>().SingleOrDefault(child =>
                    ReferenceEquals(AdornerLayer.GetAdornedElement(child), target))
                    ?? throw new InvalidOperationException("No focus adorner");
                var rings = adorner.GetVisualDescendants().OfType<Border>().Prepend(adorner as Border)
                    .OfType<Border>().Where(border => border.BorderBrush is not null).Distinct().ToArray();
                // Observed Fluent geometry: the outer ring has the target's own size and paints over its edge, so its
                // outer edge meets the parent's surface; the inner ring is inset by the outer thickness and meets the fill.
                if (rings.Length != 2 || rings[0].Bounds.Size != target.Bounds.Size ||
                    rings[1].Bounds != new Rect(rings[0].Bounds.Size).Deflate(rings[0].BorderThickness))
                    throw new InvalidOperationException($"Focus adorner geometry changed ({rings.Length} rings, outer {rings[0].Bounds}, target {target.Bounds})");
                Measure(theme, row, Solid(rings[0].BorderBrush, row + " ring"), Backing(outside), 3);
                // On a filled target the two-tone ring must also stand out from the fill: one of its tones reaches 3:1
                // against the fill (the outer tone on an unselected fill, the inner tone on a Primary selected fill).
                if (PropertyBrush(target, "Background") is ISolidColorBrush { Color.A: 255 } fill)
                {
                    var best = rings.Select(ring => Solid(ring.BorderBrush, row + " ring tone"))
                        .MaxBy(tone => Contrast(tone, fill.Color));
                    Measure(theme, row + ".vs-fill", best, fill.Color, 3);
                }
            }
            void SelectedRow(string theme, string row, Control tab) =>
                // The selected state's cue (SC 1.4.11): the tab's own fill against the strip it sits on.
                Measure(theme, row, Solid(PropertyBrush(tab, "Background"), row + " fill"),
                    Backing(tab.GetVisualParent() ?? throw new InvalidOperationException("Tab has no strip")), 3);
            IPseudoClasses Pseudo(Control control) => pseudoProperty.GetValue(control) as IPseudoClasses
                ?? throw new InvalidOperationException("Installed IPseudoClasses unavailable");
            void Hover(Control target, Window window, bool enter)
            {
                using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
                var origin = target.TranslatePoint(new Point(4, 4), window)
                    ?? throw new InvalidOperationException("Hover target position unresolvable");
                target.RaiseEvent(new PointerEventArgs(enter ? InputElement.PointerEnteredEvent : InputElement.PointerExitedEvent,
                    target, pointer, window, origin, 1, default, KeyModifiers.None));
                Settle(window);
                if (target.IsPointerOver != enter || Pseudo(target).Contains(":pointerover") != enter)
                    throw new InvalidOperationException("PointerEntered/Exited did not set :pointerover");
            }

            (Color Ink, Color Back) TextPaint(Control target)
            {
                var text = TextVisual(target);
                return (Solid(PropertyBrush(text, "Foreground"), "ink"), Backing(text));
            }
            // A Button takes a framework press, released outside it so no click fires. A list row or a Dock tab takes the
            // :pressed style state instead, because a real press would select it (the retired matrix's "styled" route).
            void Press(Control target, Window window, bool press)
            {
                using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
                var origin = target.TranslatePoint(new Point(4, 4), window)
                    ?? throw new InvalidOperationException("Press target position unresolvable");
                if (target is Button)
                {
                    if (press)
                        target.RaiseEvent(new PointerPressedEventArgs(target, pointer, window, origin, 2,
                            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
                    else
                        target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, window, new Point(-100, -100), 3,
                            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
                }
                else if (press) Pseudo(target).Add(":pressed");
                else Pseudo(target).Remove(":pressed");
                Settle(window);
                if (Pseudo(target).Contains(":pressed") != press)
                    throw new InvalidOperationException($":pressed did not {(press ? "set" : "clear")}");
            }
            // Pressed, then returned: hover and press, measure; release outside and leave, measure. Returned must paint as rest.
            void PressRows(string theme, string prefix, Control target, Window window)
            {
                var rest = TextPaint(target);
                Probe(theme, prefix + ".pressed", () =>
                {
                    Hover(target, window, true);
                    Press(target, window, true);
                    TextRow(theme, prefix + ".pressed", target);
                });
                Probe(theme, prefix + ".returned", () =>
                {
                    if (Pseudo(target).Contains(":pressed")) Press(target, window, false);
                    if (target.IsPointerOver) Hover(target, window, false);
                    TextRow(theme, prefix + ".returned", target);
                    if (TextPaint(target) != rest)
                        throw new InvalidOperationException($"returned paint {TextPaint(target)} differs from rest {rest}");
                });
            }
            void HoverRow(string theme, string row, Control target, Window window)
            {
                Probe(theme, row, () =>
                {
                    Hover(target, window, true);
                    TextRow(theme, row, target);
                    Hover(target, window, false);
                });
            }
            // TextBox states (Styles.axaml TextBox rules): hover, focused text and caret, focus-hover, selection, returned.
            void TextBoxStates(string theme, string prefix, TextBox box, Window window, Control focusAway, bool editable)
            {
                var presenter = box.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().Single();
                var border = box.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_BorderElement");
                (Color, Color) Paint() => (Solid(presenter.Foreground, prefix + " ink"), Backdrop(box, border, prefix + " backdrop"));
                var rest = Paint();
                if (editable)
                    Probe(theme, prefix + ".hover", () =>
                    {
                        Hover(box, window, true);
                        TextBoxRow(theme, prefix + ".hover", box);
                        Hover(box, window, false);
                    });
                if (!box.Focus(NavigationMethod.Tab)) throw new InvalidOperationException(prefix + " refused keyboard focus");
                Settle(window);
                Probe(theme, prefix + ".focus.text", () => TextBoxRow(theme, prefix + ".focus.text", box));
                if (editable)
                {
                    Probe(theme, prefix + ".focus.caret", () =>
                        Measure(theme, prefix + ".focus.caret", Solid(presenter.CaretBrush, prefix + " caret"), Backdrop(box, border, prefix + " backdrop"), 3));
                    Probe(theme, prefix + ".focus-hover", () =>
                    {
                        Hover(box, window, true);
                        TextBoxRow(theme, prefix + ".focus-hover", box);
                        Hover(box, window, false);
                    });
                }
                Probe(theme, prefix + ".selection", () =>
                {
                    box.SelectAll();
                    Settle(window);
                    if (box.SelectionStart == box.SelectionEnd) throw new InvalidOperationException(prefix + " selected nothing");
                    Measure(theme, prefix + ".selection", Solid(presenter.SelectionForegroundBrush, prefix + " selected ink"),
                        Solid(presenter.SelectionBrush, prefix + " selection"), 4.5);
                    box.ClearSelection();
                });
                if (editable)
                    Probe(theme, prefix + ".returned", () =>
                    {
                        if (!focusAway.Focus(NavigationMethod.Tab) || box.IsFocused)
                            throw new InvalidOperationException(prefix + " kept focus");
                        Settle(window);
                        TextBoxRow(theme, prefix + ".returned", box);
                        if (Paint() != rest) throw new InvalidOperationException($"{prefix} returned paint {Paint()} differs from rest {rest}");
                    });
            }

            // Dock's deferred presenter fades a pane in when its content changes (DeferredContentControl.cs: opacity
            // 0.85 → 1 over a 90 ms DoubleTransition), so a probe after ShowPane could land mid-fade (observed:
            // opacity 0.964 at Animation priority). The matrix measures settled paint, so the fade is off for it;
            // Backing still refuses any unresolved group opacity.
            var reveal = Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration;
            Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration = TimeSpan.Zero;
            try
            {
            foreach (var (theme, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark),
                         ("high-contrast", NativeReviewThemes.HighContrast), ("default", ThemeVariant.Default) })
            {
                var window = new MainWindow() { RequestedThemeVariant = variant, Width = 1024, Height = 700 };
                var controller = typeof(MainWindow).GetField("workbench", reflection)?.GetValue(window) as WorkbenchController
                    ?? throw new InvalidOperationException("Controller field unreadable");
                try
                {
                    window.Show();
                    Settle(window);
                    var host = window.Content as ShellHost ?? throw new InvalidOperationException("Shell host did not load");
                    var span = host.Properties.FindControl<TextBox>("SpanInput") ?? throw new InvalidOperationException("Span field did not load");
                    if (span.IsEffectivelyVisible && span.IsEnabled)
                        throw new InvalidOperationException("Empty-state Span field is enabled with no foil open");
                    Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                    host.RefreshPanes();
                    Settle(window);

                    var docTabs = host.DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>().ToArray();
                    string[] titles = docTabs.Select(tab => (tab.DataContext as Dock.Model.Core.IDockable)?.Title ?? "").ToArray();
                    if (!titles.SequenceEqual(["Plan", "Section", "Foil source"]))
                        throw new InvalidOperationException("Model-area Dock tabs are " + string.Join(", ", titles));
                    var modelTab = docTabs[1];
                    var sourceTab = docTabs[2];
                    modelTab.IsSelected = true;
                    Settle(window);

                    // Theme barrier: a fresh composition batch renders the focused tab after the Example is bound.
                    var composition = ElementComposition.GetElementVisual(modelTab)
                        ?? throw new InvalidOperationException("Barrier tab lacks a compositor");
                    if (!modelTab.Focus(NavigationMethod.Tab))
                        throw new InvalidOperationException("Barrier tab refused keyboard focus");
                    var fresh = composition.Compositor.RequestCompositionBatchCommitAsync();
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
                        while (!fresh.Rendered.IsCompleted && !timeout.IsCancellationRequested)
                            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    if (!fresh.Rendered.IsCompletedSuccessfully || !modelTab.IsFocused)
                        throw new InvalidOperationException("Theme barrier focus composition was not ready");
                    if (ElementComposition.GetElementVisual(modelTab) is null)
                        throw new InvalidOperationException("Barrier tab lost its compositor");
                    Probe(theme, "focus.tab", () => RingRow(theme, "focus.tab", modelTab, window));

                    foreach (var (tab, index) in docTabs.Select((tab, index) => (tab, index)))
                    {
                        Probe(theme, $"tab.{titles[index]}.{(tab.IsSelected ? "selected" : "unselected")}.rest",
                            () => TextRow(theme, $"tab.{titles[index]}.{(tab.IsSelected ? "selected" : "unselected")}.rest", tab));
                        if (tab.IsSelected) Probe(theme, $"select.tab.{titles[index]}", () => SelectedRow(theme, $"select.tab.{titles[index]}", tab));
                    }
                    Probe(theme, "tab.Foil source.unselected.hover", () =>
                    {
                        Hover(sourceTab, window, true);
                        TextRow(theme, "tab.Foil source.unselected.hover", sourceTab);
                        Hover(sourceTab, window, false);
                    });
                    PressRows(theme, "tab.Foil source.unselected", sourceTab, window);
                    // Focus first, then select: the focus ring must follow the selected state it now surrounds.
                    if (!sourceTab.Focus(NavigationMethod.Tab))
                        throw new InvalidOperationException("Foil source tab refused keyboard focus");
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.FoilSourceDocument;
                    Settle(window);
                    if (sourceTab.IsFocused)
                        Probe(theme, "focus.tab.selected-while-focused",
                            () => RingRow(theme, "focus.tab.selected-while-focused", sourceTab, window));
                    else rowFailures.Add($"{theme}/focus.tab.selected-while-focused: selecting the tab moved focus away");
                    Probe(theme, "tab.Foil source.selected.rest", () => TextRow(theme, "tab.Foil source.selected.rest", sourceTab));
                    Probe(theme, "select.tab.Foil source", () => SelectedRow(theme, "select.tab.Foil source", sourceTab));
                    Probe(theme, "tab.Foil source.selected.hover", () =>
                    {
                        Hover(sourceTab, window, true);
                        TextRow(theme, "tab.Foil source.selected.hover", sourceTab);
                        Hover(sourceTab, window, false);
                    });
                    PressRows(theme, "tab.Foil source.selected", sourceTab, window);

                    var toolTabs = host.DockHost.GetVisualDescendants().OfType<Control>()
                        .Where(control => control.GetType().Name == "ToolTabStripItem" && control.IsEffectivelyVisible).ToArray();
                    if (toolTabs.Length == 0) throw new InvalidOperationException("No Dock tool tabs rendered");
                    foreach (var tool in toolTabs)
                    {
                        string name = (tool.DataContext as Dock.Model.Core.IDockable)?.Title ?? "?";
                        string state = tool is ISelectable { IsSelected: true } ? "selected" : "unselected";
                        Probe(theme, $"tool.{name}.{state}.rest", () => TextRow(theme, $"tool.{name}.{state}.rest", tool));
                        if (state == "selected") Probe(theme, $"select.tool.{name}", () => SelectedRow(theme, $"select.tool.{name}", tool));
                        Probe(theme, $"tool.{name}.{state}.hover", () =>
                        {
                            Hover(tool, window, true);
                            TextRow(theme, $"tool.{name}.{state}.hover", tool);
                            Hover(tool, window, false);
                        });
                    }

                    var sidebar = host.LeftSidebarToggle;
                    Probe(theme, "appbar.sidebar.rest", () => TextRow(theme, "appbar.sidebar.rest", sidebar));
                    HoverRow(theme, "appbar.sidebar.hover", sidebar, window);
                    PressRows(theme, "appbar.sidebar", sidebar, window);
                    // The press probes give the button pointer focus; move focus off it so the Tab focus is a real change.
                    if (!modelTab.Focus(NavigationMethod.Tab) || !sidebar.Focus(NavigationMethod.Tab))
                        throw new InvalidOperationException("App-bar button refused keyboard focus");
                    Probe(theme, "focus.appbar", () => RingRow(theme, "focus.appbar", sidebar, window));

                    host.ShowPane("browser");
                    Settle(window);
                    var stations = host.Browser.FindControl<ListBox>("StationList")
                        ?? throw new InvalidOperationException("Browser rows did not load");
                    var rowItems = stations.Items.OfType<ListBoxItem>().ToArray();
                    if (rowItems.Length < 2) throw new InvalidOperationException("Example Browser rows did not bind");
                    stations.SelectedIndex = 0;
                    Settle(window);
                    if (!rowItems[0].IsSelected) throw new InvalidOperationException("Example Browser row did not select");
                    // DR-STATUS-1: the status strip takes 24 px of the 700 px window, so the second row can sit under the
                    // Browser's fold; scroll it into view before measuring what paints behind its text.
                    rowItems[1].BringIntoView();
                    Settle(window);
                    Probe(theme, "browser.selected", () => TextRow(theme, "browser.selected", rowItems[0]));
                    Probe(theme, "browser.unselected", () => TextRow(theme, "browser.unselected", rowItems[1]));
                    foreach (var (item, state) in new[] { (rowItems[0], "selected"), (rowItems[1], "unselected") })
                    {
                        HoverRow(theme, $"browser.{state}.hover", item, window);
                        PressRows(theme, $"browser.{state}", item, window);
                    }
                    if (!rowItems[1].Focus(NavigationMethod.Tab))
                        throw new InvalidOperationException("Browser row refused keyboard focus");
                    Probe(theme, "focus.browser", () => RingRow(theme, "focus.browser", rowItems[1], window));
                    if (!rowItems[0].Focus(NavigationMethod.Tab))
                        throw new InvalidOperationException("Selected Browser row refused keyboard focus");
                    Probe(theme, "focus.browser.selected", () => RingRow(theme, "focus.browser.selected", rowItems[0], window));

                    host.ShowPane("properties");
                    Settle(window);
                    if (!span.IsEffectivelyVisible || !span.IsEnabled)
                        throw new InvalidOperationException("Span field not editable with the Example open");
                    Probe(theme, "span.text", () => TextBoxRow(theme, "span.text", span));
                    Probe(theme, "span.painter-oracle", () =>
                    {
                        var presenter = span.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().Single();
                        var border = span.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_BorderElement");
                        var backdrop = Backdrop(span, border, "backdrop");
                        presenter.Foreground = new SolidColorBrush(backdrop);
                        double ratio;
                        try { ratio = Contrast(Solid(presenter.Foreground, "mutated ink"), backdrop); }
                        finally { presenter.ClearValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty); }
                        if (ratio >= 4.5) throw new InvalidOperationException("Painter oracle accepted a low-contrast mutation");
                    });
                    TextBoxStates(theme, "span", span, window, sidebar, editable: true);
                    if (!span.Focus(NavigationMethod.Tab)) throw new InvalidOperationException("Span field refused keyboard focus");
                    span.SelectAll();
                    span.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = "1234" });
                    if (span.Text != "1234") throw new InvalidOperationException("Typed replacement was not applied to Span");
                    if (controller.Draft is not null) throw new InvalidOperationException("Span typing opened a draft");
                    Probe(theme, "focus.span", () => RingRow(theme, "focus.span", span, window));
                    span.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

                    var source = host.ModelView.FindControl<TextBox>("SourceText")
                        ?? throw new InvalidOperationException("Foil source field did not load");
                    if (!source.IsReadOnly || source.Text != controller.AcceptedSource)
                        throw new InvalidOperationException("Foil source tab lost its read-only accepted text");
                    Probe(theme, "source.text", () => TextBoxRow(theme, "source.text", source));
                    TextBoxStates(theme, "source", source, window, sidebar, editable: false);
                    // The point fields replace the retired per-control numeric field: a selected free point enables Span.
                    var freePoint = controller.Planform!.Trailing.Points.First(point => point.Role == PointRole.Control && point.Freedom == PointFreedom.Free);   // a handle shows angle + length
                    controller.Select(new Selection.Points(new[] { new PointRef(freePoint.Curve, freePoint.Id) }));
                    host.RefreshPanes();
                    Settle(window);
                    var pointSpan = host.Properties.FindControl<TextBox>("PointSpanInput") ?? throw new InvalidOperationException("Point Span field absent");
                    if (!pointSpan.IsEffectivelyVisible || !pointSpan.IsEnabled)
                        throw new InvalidOperationException("Point Span field not editable with a free point selected");
                    Probe(theme, "point-span.text", () => TextBoxRow(theme, "point-span.text", pointSpan));
                    if (!pointSpan.Focus(NavigationMethod.Tab)) throw new InvalidOperationException("Point Span field refused keyboard focus");
                    Probe(theme, "focus.point-span", () => RingRow(theme, "focus.point-span", pointSpan, window));
                    if (!sidebar.Focus(NavigationMethod.Tab)) throw new InvalidOperationException("Focus could not leave the point Span field");
                    // Annotations and the unsaved-changes modal: rows the retired pre-shell matrix measured on surfaces
                    // the shell still ships.
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                    Settle(window);
                    Probe(theme, "section.annotation", () => TextRow(theme, "section.annotation",
                        host.AnalysisPanel.SectionView.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()
                            ?? throw new InvalidOperationException("Section annotation absent")));
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.FoilSourceDocument;
                    Settle(window);
                    var pending = (Task<string>)(typeof(MainWindow).GetMethod("UnsavedDialogAsync", reflection)?.Invoke(window, null)
                        ?? throw new InvalidOperationException("Unsaved dialog unavailable"));
                    var modal = window.OwnedWindows.SingleOrDefault() ?? throw new InvalidOperationException("Unsaved modal is not owned by the window");
                    Settle(modal);
                    if (modal.ActualThemeVariant != window.ActualThemeVariant)
                        rowFailures.Add($"{theme}/modal: the unsaved modal did not inherit its owner's theme");
                    var modalBody = modal.GetVisualDescendants().OfType<TextBlock>()
                        .FirstOrDefault(text => text.Text?.StartsWith("Save this foil", StringComparison.Ordinal) == true)
                        ?? throw new InvalidOperationException("Unsaved modal body unavailable");
                    Probe(theme, "modal.body", () => TextRow(theme, "modal.body", modalBody));
                    var modalButtons = modal.GetVisualDescendants().OfType<Button>()
                        .Where(button => button.Content is string).ToDictionary(button => (string)button.Content!);
                    foreach (var choice in new[] { "Save", "Discard", "Cancel" })
                    {
                        string row = "modal." + choice.ToLowerInvariant();
                        Probe(theme, row + ".rest", () => TextRow(theme, row + ".rest", modalButtons[choice]));
                        Probe(theme, row + ".hover", () =>
                        {
                            Hover(modalButtons[choice], modal, true);
                            TextRow(theme, row + ".hover", modalButtons[choice]);
                            Hover(modalButtons[choice], modal, false);
                        });
                        PressRows(theme, row, modalButtons[choice], modal);
                    }
                    if (!modalButtons["Cancel"].IsDefault || !modalButtons["Cancel"].IsCancel || !modalButtons["Cancel"].IsFocused)
                        rowFailures.Add($"{theme}/modal: Cancel is not the focused safe default");
                    modal.Close();
                    Settle(window);
                    if (!pending.IsCompletedSuccessfully || pending.Result != "Cancel")
                        rowFailures.Add($"{theme}/modal: closing the modal did not answer Cancel");
                    if (host.Properties.FindControl<TextBox>("SpanInput") is null ||
                        host.Properties.FindControl<TextBlock>("WingHeading") is null)
                        throw new InvalidOperationException("Rail-editor CV list or numeric field absent from the pane namescope");

                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                    Settle(window);
                    controller.ToggleAnalysis();
                    host.RefreshPanes();
                    Settle(window);
                    var band = host.ModelView.FindControl<CfdWorkbench.Desktop.Analysis.ConditionsBand>("AnalysisConditionsBand")
                        ?? throw new InvalidOperationException("Analysis conditions band is absent");
                    Probe(theme, "analysis.nav.selected", () => TextRow(theme, "analysis.nav.selected",
                        host.ModelView.FindControl<Avalonia.Controls.Primitives.ToggleButton>("NavAnalysisButton")!));
                    Probe(theme, "analysis.band.speed", () => TextBoxRow(theme, "analysis.band.speed",
                        band.FindControl<TextBox>("SpeedInput")!));
                    Probe(theme, "analysis.band.evaluate", () => TextRow(theme, "analysis.band.evaluate",
                        band.FindControl<Button>("EvaluateButton")!));
                    Probe(theme, "analysis.band.derived", () => TextRow(theme, "analysis.band.derived",
                        band.FindControl<TextBlock>("DerivedQ")!));
                    Probe(theme, "analysis.status.text", () => TextRow(theme, "analysis.status.text",
                        host.StatusStrip.FindControl<TextBlock>("AnalysisItemText")!));
                    Probe(theme, "analysis.panel.tab.selected", () => TextRow(theme, "analysis.panel.tab.selected",
                        host.AnalysisPanel.FindControl<TabItem>("LoadingTab")!));
                    Probe(theme, "analysis.panel.tab.unselected", () => TextRow(theme, "analysis.panel.tab.unselected",
                        host.AnalysisPanel.FindControl<TabItem>("LoadsTab")!));
                    controller.ToggleAnalysis();
                    host.RefreshPanes();
                    Settle(window);
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.FoilSourceDocument;
                    Settle(window);

                    if (variant == ThemeVariant.Light)
                    {
                        // The variant must follow a live switch on an open window, not only the one it opened with.
                        window.RequestedThemeVariant = ThemeVariant.Dark;
                        Settle(window);
                        // M1.2c: the Section tab is retired; the live flip probes the Plan tab, unselected here.
                        Probe(theme, "live-flip.dark.tab.Plan.unselected",
                            () => TextRow(theme, "live-flip.dark.tab.Plan.unselected", docTabs[0]));
                        Probe(theme, "live-flip.dark.select.tab.Foil source",
                            () => SelectedRow(theme, "live-flip.dark.select.tab.Foil source", sourceTab));
                    }
                }
                finally
                {
                    typeof(MainWindow).GetField("closeApproved", reflection)?.SetValue(window, true);
                    window.Close();
                }
            }
            }
            finally { Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration = reveal; }
            // Every variant measures the same rows (the light-only live flip aside), so a skipped probe cannot hide.
            var perTheme = emittedRows.Where(item => !item.Row.StartsWith("live-flip.", StringComparison.Ordinal))
                .GroupBy(item => item.Theme).ToDictionary(group => group.Key, group => group.Select(item => item.Row).ToHashSet());
            var reference = perTheme.GetValueOrDefault("light") ?? [];
            foreach (var (theme, rows) in perTheme)
                if (!rows.SetEquals(reference))
                    rowFailures.Add($"{theme}: row set differs from light ({string.Join(", ", rows.Except(reference).Concat(reference.Except(rows)))})");
            if (perTheme.Count != 4) rowFailures.Add($"{perTheme.Count} variants measured, not 4");
            Console.WriteLine($"THEME-SHELL-CHECK rows={emittedRows.Count} variants={perTheme.Count} source=shell-MainWindow");
            if (rowFailures.Count > 0)
                throw new InvalidOperationException($"{rowFailures.Count} theme rows failed: " + string.Join(" | ", rowFailures));
        });
        U2Checks();
    }

    private static void U2Checks()
    {
        const string ControlHelper = "A control point pulls the curve toward it. The curve does not pass through it. Choosing Anchor point adds handles (the rail gains up to 3 points).";
        const string AnchorHelper = "An anchor point is on the curve. Its handles set the curve's direction on each side.";
        const string MixedCopy = "Select one point to change it.";
        const string TipClosedCopy = "Tip closes — edit the tip station";
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        DesktopChecks.Check("Properties_ControlPoint_TypeSpanAftRows", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                U2Select(controller, window, point);
                var props = host.Properties;
                var block = U2Need<Control>(props, "Group_pos");
                if (!block.IsVisible) throw new InvalidOperationException("Position group is hidden");
                string heading = U2Text(props, "IdentityTitle");
                if (!heading.Contains("Trailing", StringComparison.Ordinal) || !heading.Contains("point", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("heading: " + heading);
                var type = U2Need<ComboBox>(props, "TypeControl");
                if (!type.IsEnabled) throw new InvalidOperationException("Type is not editable on a control point");
                var choices = U2ComboTexts(type);
                if (!choices.Contains(TypeAnchorOption) || !choices.Contains("Control point"))
                    throw new InvalidOperationException("Type choices: " + string.Join(", ", choices));
                if (U2SelectedText(type) != "Control point")
                    throw new InvalidOperationException("Type shows " + U2SelectedText(type));
                U2Near(U2ParseMm(U2Need<TextBox>(props, "PointSpanInput").Text), point.SpanMeters, "span");
                U2Near(U2ParseMm(U2Need<TextBox>(props, "PointAftInput").Text), point.Ordinate, "aft");
                if (!U2Need<TextBox>(props, "PointSpanInput").IsEnabled || !U2Need<TextBox>(props, "PointAftInput").IsEnabled)
                    throw new InvalidOperationException("Span or aft is locked on a free control point");
                if (U2Text(props, "PointHelper") != ControlHelper)
                    throw new InvalidOperationException("helper: " + U2Text(props, "PointHelper"));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_NamedPoint_TypeReadOnlyWithConstraint", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var root = controller.Planform!.Leading.Points.First(p => p.Role == PointRole.RootEnd);
                U2Select(controller, window, root);
                var props = host.Properties;
                var type = props.FindControl<ComboBox>("TypeControl");
                if (type is { IsEffectivelyVisible: true, IsEnabled: true })
                    throw new InvalidOperationException("Named root end still has an enabled type combo");
                string shown = U2Text(props, "TypeReadOnly");
                if (!shown.Contains("root", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("type text: " + shown);
                if (U2Text(props, "Note_pos_0") != "Fixed: the leading edge starts at the root.")
                    throw new InvalidOperationException("constraint: " + U2Text(props, "Note_pos_0"));
                if (U2Need<TextBox>(props, "PointSpanInput").IsEnabled)
                    throw new InvalidOperationException("Fixed root span is editable");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_TypeToAnchor_OneUndoStepCurvePassesThrough", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                U2Select(controller, window, point);
                var type = U2Need<ComboBox>(host.Properties, "TypeControl");
                type.Focus();
                U2CommitType(type, TypeAnchorOption);
                U2WaitIdle(controller, window);
                var now = U2Reload(controller, point.Curve, point.Id);
                if (now.Role != PointRole.Anchor)
                    throw new InvalidOperationException("role after type change: " + now.Role);
                if (!controller.CanUndo)
                    throw new InvalidOperationException("type change did not add one undo step");
                var plan = controller.Planform ?? throw new InvalidOperationException("planform missing");
                double gap = U2CurveGap(plan, now);
                if (gap > 0.002)
                    throw new InvalidOperationException("curve misses the anchor by " + gap.ToString("G4", inv) + " m");
                var status = StatusStripTests.Text(host);
                // PG-26 / DR-STATUS-1: the type report from the operation is in the strip, and the row shows no message.
                if (status.Text?.Contains("is now an anchor point with 2 handles", StringComparison.Ordinal) != true ||
                    U2Need<TextBlock>(host.Properties, "Message_p_type").IsEffectivelyVisible)
                    throw new InvalidOperationException("status '" + status.Text + "' controller '" + controller.Status + "'");
                controller.Undo();
                Settle(window);
                if (U2Reload(controller, point.Curve, point.Id).Role != PointRole.Control)
                    throw new InvalidOperationException("one undo did not restore the control point");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_TangentSymmetric_OneUndoStep", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)));
                U2Select(controller, window, U2Reload(controller, point.Curve, point.Id));
                var before = U2Reload(controller, point.Curve, point.Id).Kind;
                var kind = U2Need<ComboBox>(host.Properties, "KindControl");
                if (!kind.IsEffectivelyVisible || !kind.IsEnabled)
                    throw new InvalidOperationException("Tangent kind is not an enabled control");
                U2CommitType(kind, "Symmetric");
                U2WaitIdle(controller, window);
                var now = U2Reload(controller, point.Curve, point.Id);
                if (now.Kind != TangentKind.Symmetric)
                    throw new InvalidOperationException("kind: " + now.Kind);
                var status = StatusStripTests.Text(host);
                // PG-33 / DR-STATUS-1: the kind report is in the strip, and the row shows no message.
                if (status.Text?.Contains("is now Symmetric.", StringComparison.Ordinal) != true ||
                    U2Need<TextBlock>(host.Properties, "Message_t_kind").IsEffectivelyVisible)
                    throw new InvalidOperationException("tangent status not in the strip: " + status.Text);
                controller.Undo();
                Settle(window);
                if (U2Reload(controller, point.Curve, point.Id).Kind != before)
                    throw new InvalidOperationException("one undo did not restore the tangent");
                if (U2Reload(controller, point.Curve, point.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("tangent undo also reverted the anchor");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_TangentGroup_LabelledChoiceAndHandleShowsItsAnchor", () =>
        {
            // F-4, O-4, O-6 (docs/reviews/m12b-native.md §3): a labelled tangent choice showing the current kind, also on
            // a handle (where it edits the parent anchor); the handle is named as a handle; chord fields carry "mm".
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var props = host.Properties;
                var point = U2Control(controller, "trailing");
                Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)));
                var anchor = U2Reload(controller, point.Curve, point.Id);
                string Checked() => U2SelectedText(U2Need<ComboBox>(props, "KindControl"));
                U2Select(controller, window, anchor);
                // B: the tangent rows continue the "Point" group, and the choice is labelled "Tangent kind" (DESIGN.md §12.0f).
                if (U2Text(props, "GroupTitle_pos") != "Point" || U2Text(props, "TangentLabel") != "Tangent kind" ||
                    !U2Need<TextBlock>(props, "TangentLabel").IsEffectivelyVisible)
                    throw new InvalidOperationException("the tangent kind has no visible label");
                if (Checked() != anchor.Kind.ToString()) throw new InvalidOperationException($"anchor kind {anchor.Kind} shown as '{Checked()}'");
                var handle = controller.Planform!.Trailing.Points.First(p => p.AnchorId == anchor.Id);
                U2Select(controller, window, handle);
                if (!U2Need<Control>(props, "KindControl").IsEffectivelyVisible || Checked() != anchor.Kind.ToString())
                    throw new InvalidOperationException($"a selected handle does not show its anchor's kind: '{Checked()}'");
                // O-6: one identity per selection — the handle by its own name, its anchor as the crumb link.
                string heading = U2Text(props, "IdentityTitle");
                string crumb = U2Need<HyperlinkButton>(props, "IdentityCrumbLink").Content?.ToString() ?? "";
                if (heading is not ("Handle toward the tip" or "Handle toward the root") ||
                    !crumb.Contains($"Trailing edge · anchor point {anchor.Index + 1}", StringComparison.Ordinal))
                    throw new InvalidOperationException($"handle identity: {heading} / {crumb}");
                if (Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(props).OfType<TextBlock>().Any(block => block.IsEffectivelyVisible && block.Text == AnchorHelper))
                    throw new InvalidOperationException("a handle reuses its anchor's helper");
                U2CommitType(U2Need<ComboBox>(props, "KindControl"), "Symmetric");
                U2WaitIdle(controller, window);
                if (U2Reload(controller, anchor.Curve, anchor.Id).Kind != TangentKind.Symmetric || Checked() != "Symmetric")
                    throw new InvalidOperationException("Symmetric on a handle did not set its anchor: " + U2Reload(controller, anchor.Curve, anchor.Id).Kind);
                foreach (var unit in new[] { "RootChordUnit", "TipChordUnit" })
                    if (U2Text(props, unit) != "mm" || !U2Need<TextBlock>(props, unit).IsEffectivelyVisible)
                        throw new InvalidOperationException(unit + " does not show mm");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_TypedSpanAftExpression_CommitsAsOneGestureEchoed", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                U2Select(controller, window, point);
                var span = U2Need<TextBox>(host.Properties, "PointSpanInput");
                var aft = U2Need<TextBox>(host.Properties, "PointAftInput");
                span.Text = Math.Abs(point.SpanMeters - 0.20) < 0.015 ? "30 cm" : "20 cm";
                aft.Text = "#root_chord * 0.05";
                span.Focus();
                U2Key(aft, Key.Enter);
                U2WaitIdle(controller, window);
                var now = U2Reload(controller, point.Curve, point.Id);
                if (Math.Abs(now.SpanMeters - point.SpanMeters) < 0.005 && Math.Abs(now.Ordinate - point.Ordinate) < 0.001)
                    throw new InvalidOperationException("typed span and aft did not move the point");
                U2Near(U2ParseMm(span.Text), now.SpanMeters, "echoed span");
                U2Near(U2ParseMm(aft.Text), now.Ordinate, "echoed aft");
                if (!controller.CanUndo) throw new InvalidOperationException("typed edit has no undo");
                controller.Undo();
                Settle(window);
                var restored = U2Reload(controller, point.Curve, point.Id);
                if (Math.Abs(restored.SpanMeters - point.SpanMeters) > 0.0001 || Math.Abs(restored.Ordinate - point.Ordinate) > 0.0001)
                    throw new InvalidOperationException("one undo did not restore span and aft");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_HandleAngleLength_TypedCommitsOneStep", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var plan = controller.Planform!;
                // A tip handle: a root-mirror handle's angle is a locked fact, not a field (COPY-156).
                var handle = plan.Trailing.Points.FirstOrDefault(p => p.Role is PointRole.TipHandle)
                    ?? plan.Leading.Points.First(p => p.Role is PointRole.TipHandle);
                U2Select(controller, window, handle);
                var angleBox = U2Need<TextBox>(host.Properties, "HandleAngleInput");
                var lengthBox = U2Need<TextBox>(host.Properties, "HandleLengthInput");
                if (!lengthBox.IsEnabled) throw new InvalidOperationException("Handle length is locked");
                double angle = double.Parse(angleBox.Text ?? "", inv);
                double length = U2ParseMm(lengthBox.Text);
                var shown = CfdWorkbench.Core.Planform.HandleTarget(controller.Planform!, handle.Curve, handle.Id, angle, length);
                if (Math.Abs(shown.SpanMeters - handle.SpanMeters) > 5e-4 || Math.Abs(shown.Ordinate - handle.Ordinate) > 5e-4)
                    throw new InvalidOperationException("shown angle/length is not the handle");
                lengthBox.Text = ((length + 0.02) * 1000).ToString("0.##", inv);
                lengthBox.Focus();
                U2Key(lengthBox, Key.Enter);
                U2WaitIdle(controller, window);
                var moved = U2Reload(controller, handle.Curve, handle.Id);
                var expected = CfdWorkbench.Core.Planform.HandleTarget(controller.Planform!, handle.Curve, handle.Id, angle, U2ParseMm(lengthBox.Text));
                if (Math.Abs(expected.SpanMeters - moved.SpanMeters) > 5e-4 || Math.Abs(expected.Ordinate - moved.Ordinate) > 5e-4)
                    throw new InvalidOperationException("committed handle is not HandleTarget");
                if (Math.Abs(moved.SpanMeters - handle.SpanMeters) < 0.005 && Math.Abs(moved.Ordinate - handle.Ordinate) < 0.005)
                    throw new InvalidOperationException("handle did not move");
                controller.Undo();
                Settle(window);
                var restored = U2Reload(controller, handle.Curve, handle.Id);
                if (Math.Abs(restored.SpanMeters - handle.SpanMeters) > 5e-4 || Math.Abs(restored.Ordinate - handle.Ordinate) > 5e-4)
                    throw new InvalidOperationException("one undo did not restore the handle");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_MultiplePoints_MixedReadOnly", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var plan = controller.Planform!;
                var first = plan.Trailing.Points.First(p => p.Role == PointRole.Control);
                var second = plan.Leading.Points.First(p => p.Role == PointRole.Control);
                controller.Select(new Selection.Points(new[] { new PointRef(first.Curve, first.Id), new PointRef(second.Curve, second.Id) }));
                Settle(window);
                if (U2Text(host.Properties, "IdentityTitle") != "2 points")
                    throw new InvalidOperationException("heading: " + U2Text(host.Properties, "IdentityTitle"));
                var banner = U2Need<TextBlock>(host.Properties, "Note_pos_0");
                if (!banner.IsVisible || banner.Text != MixedCopy)
                    throw new InvalidOperationException("mixed banner: " + banner.Text);
                var type = host.Properties.FindControl<ComboBox>("TypeControl");
                if (type is { IsEnabled: true, IsEffectivelyVisible: true })
                    throw new InvalidOperationException("Mixed type is editable");
                if (U2Text(host.Properties, "TypeReadOnly") != "Control point" || U2Text(host.Properties, "Value_p_from") != "Mixed" || U2Text(host.Properties, "Value_p_aft") != "Mixed")   // OI-3: the common type; Mixed values
                    throw new InvalidOperationException("type/mixed: " + U2Text(host.Properties, "TypeReadOnly") + " / " + U2Text(host.Properties, "Value_p_from"));
                if (U2Need<TextBox>(host.Properties, "PointSpanInput").IsEnabled)
                    throw new InvalidOperationException("Mixed span is editable");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Properties_TipCloses_TipChordIsText", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var accepted = controller.Estimates ?? throw new InvalidOperationException("open foil has no estimates");
                if (accepted.TipChordMeters <= 1e-9)
                    throw new InvalidOperationException("example tip is already closed");
                var closing = accepted with { TipChordMeters = 0 };
                var projected = PropertiesView.Build(new Selection.Foil(), controller.Inspection?.Authored, closing, ShellMode.Workspace);
                string row = projected.Blocks.First(block => block.Title == "Wing").Rows.First(item => item.Label == "Tip chord").Value;
                if (row != TipClosedCopy)
                    throw new InvalidOperationException("projection row: " + row);
                host.Properties.Bind(controller, closing);
                Settle(window);
                var closed = U2Need<TextBlock>(host.Properties, "TipClosedText");
                if (!closed.IsVisible || closed.Text != row)
                    throw new InvalidOperationException("tip text: " + closed.Text);
                var input = host.Properties.FindControl<TextBox>("TipChordInput");
                if (input is { IsEffectivelyVisible: true })
                    throw new InvalidOperationException("Closing tip chord is still a field");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WingBlock_AllCad17Rows_InOrderWithApprox", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var wing = U2Need<Control>(host.Properties, "WingBlock");
                string[] labels = ["Span", "Root chord", "Tip chord", "Mean chord", "MAC", "Max t/c", "AR", "Area"];
                var seen = wing.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text ?? "").Where(text => labels.Contains(text)).ToList();
                if (!seen.SequenceEqual(labels))
                    throw new InvalidOperationException("wing order: " + string.Join(" | ", seen));
                foreach (var name in new[] { "MeanChordText", "MacText", "MaxTcText", "AspectText", "AreaEstimateText" })
                {
                    string text = U2Text(host.Properties, name);
                    if (!text.StartsWith("≈", StringComparison.Ordinal) || text.Contains('—'))
                        throw new InvalidOperationException(name + " is not an approximate value: " + text);
                }
                string heading = U2Text(host.Properties, "WingHeading");
                if (!heading.Contains("Wing", StringComparison.Ordinal))
                    throw new InvalidOperationException("wing heading: " + heading);
                var how = U2Need<Button>(host.Properties, "HowMeasuredButton");
                string howText = string.Join(" ", how.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text));
                if (howText != "Estimates · definitions")
                    throw new InvalidOperationException("definitions link: " + howText);
                U2Click(how);
                Settle(window);
                var body = U2Need<TextBlock>(host.Properties, "HowMeasuredBody");
                if (!body.IsVisible || string.IsNullOrWhiteSpace(body.Text) || !body.Text.Contains("MAC", StringComparison.Ordinal))
                    throw new InvalidOperationException("how-measured body: " + body.Text);
                // PG-31: the disclosure exposes its expanded state.
                if (how is not ToggleButton { IsChecked: true }) throw new InvalidOperationException("Estimates · definitions does not expose expanded");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WingBlock_DuringDrag_RenderedMacTextChangesBeforeRelease", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                string before = U2Text(host.Properties, "MacText");
                var point = U2Control(controller, "trailing");
                if (!controller.BeginGesture(new PointRef(point.Curve, point.Id), GestureInput.Pointer))
                    throw new InvalidOperationException("gesture did not begin");
                controller.UpdateGesture(point.SpanMeters + 0.04, point.Ordinate + 0.01);
                controller.FlushGestureFrame();
                Settle(window);
                if (controller.Gesture == GestureState.Idle)
                    throw new InvalidOperationException("gesture released before the assertion");
                if (controller.Estimates?.Basis != "preview")
                    throw new InvalidOperationException("estimates basis: " + controller.Estimates?.Basis);
                string during = U2Text(host.Properties, "MacText");
                if (during == before || !during.StartsWith("≈", StringComparison.Ordinal))
                    throw new InvalidOperationException("MAC stayed '" + before + "' during the drag");
                if (!U2Text(host.Properties, "WingChipText").Contains("preview", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("wing chip: " + U2Text(host.Properties, "WingChipText"));
                Pump(controller.EndGestureAsync(GestureEnd.Escape));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WingBlock_AfterTangentChange_DerivedRowsNotDashed", () =>
        {
            // D-4 (docs/reviews/m12b-native.md §3.1): after Make Anchor + Symmetric every derived row read "≈ —".
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                Pump(host.OpenNewFoilAsync());
                // New foil ships 4 control vertices per rail (Ruling 64): anchor the interior point one Add point creates, not the tip end.
                if (Run(controller.ApplyPointCommandAsync(new PointCommand.AddPoint("trailing", 0.45))) is not CommitOutcome.Committed)
                    throw new InvalidOperationException("Add point was not committed");
                Settle(window);
                var point = controller.Planform!.Trailing.Points.First(p => p.Role == PointRole.Control);
                if (Run(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id))) is not CommitOutcome.Committed)
                    throw new InvalidOperationException("Make Anchor was not committed");
                var anchor = controller.Planform!.Trailing.Points.Single(p => p.Role == PointRole.Anchor);
                if (Run(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, TangentKind.Symmetric, null))) is not CommitOutcome.Committed)
                    throw new InvalidOperationException("Symmetric was not committed");
                Settle(window);
                var dashed = new[] { "MeanChordText", "MacText", "MaxTcText", "AspectText", "AreaEstimateText" }
                    .Where(name => U2Text(host.Properties, name).Contains('—')).ToList();
                if (dashed.Count > 0)
                    throw new InvalidOperationException("dashed after a tangent change: " + string.Join(", ", dashed));
            }
            finally { window.Close(); }

            static CommitOutcome Run(Task<CommitOutcome> task) { Pump(task); return task.Result; }
        });

        DesktopChecks.Check("WingBlock_CrossingDraft_ShowsDashAndReason", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                if (!controller.BeginGesture(new PointRef(point.Curve, point.Id), GestureInput.Pointer))
                    throw new InvalidOperationException("gesture did not begin");
                controller.UpdateGesture(point.SpanMeters, -1);
                controller.FlushGestureFrame();
                Settle(window);
                // D-4 retired "≈ —": a crossing draft reads Unavailable and the Wing says why (COPY-155).
                string mac = U2Text(host.Properties, "MacText");
                if (mac != "Unavailable")
                    throw new InvalidOperationException("MAC on a crossing draft: " + mac);
                string reason = U2Text(host.Properties, "Note_wing_0");
                if (!U2Need<TextBlock>(host.Properties, "Note_wing_0").IsEffectivelyVisible || !reason.StartsWith("Unavailable — ", StringComparison.Ordinal))
                    throw new InvalidOperationException("crossing reason is blank");
                Pump(controller.EndGestureAsync(GestureEnd.Escape));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WingRootChord_FitAboveLimit_WarningShownAndCommitted", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                string before = controller.AcceptedSource;
                var input = U2Need<TextBox>(host.Properties, "RootChordInput");
                input.Text = ((controller.Estimates!.RootChordMeters * 1000) * 10.1).ToString("0.##", inv);
                input.Focus();
                U2Key(input, Key.Enter);
                U2WaitIdle(controller, window);
                if (controller.AcceptedSource == before) throw new InvalidOperationException("root chord was not committed");
                // DR-STATUS-1: the warning is in the strip and the toast; the row keeps its rail, not the text.
                var status = StatusStripTests.Text(host);
                if (status.Text?.Contains("above the limit", StringComparison.Ordinal) != true || StatusStripTests.Kind(host) != "warning" ||
                    !StatusStripTests.NeedToast(host).IsVisible || U2Need<TextBlock>(host.Properties, "ChordWarningText").IsEffectivelyVisible ||
                    !U2Need<Border>(host.Properties, "Row_w_root").Classes.Contains("warning"))
                    throw new InvalidOperationException("status: " + status.Text);
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WingTipChord_CentimetresAndReference_EchoedMm", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var input = U2Need<TextBox>(host.Properties, "TipChordInput");
                input.Text = "12 cm";
                input.Focus();
                U2Key(input, Key.Enter);
                U2WaitIdle(controller, window);
                U2Near(U2ParseMm(input.Text), controller.Estimates!.TipChordMeters, "echoed tip cm");
                // DR-STATUS-3: the unit echo is in the strip, not under the field.
                var status = StatusStripTests.Text(host);
                if (status.Text?.StartsWith("12 cm = ", StringComparison.Ordinal) != true ||
                    U2Need<TextBlock>(host.Properties, "Message_w_tip").IsEffectivelyVisible)
                    throw new InvalidOperationException("tip status: " + status.Text);
                controller.Undo();
                Settle(window);
                input.Text = "#root_chord * 0.4";
                U2Key(input, Key.Enter);
                U2WaitIdle(controller, window);
                U2Near(U2ParseMm(input.Text), controller.Estimates!.TipChordMeters, "echoed tip reference");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_RootChordCommitTab_NextField", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                string before = controller.AcceptedSource;
                var root = U2Need<TextBox>(host.Properties, "RootChordInput");
                var tip = U2Need<TextBox>(host.Properties, "TipChordInput");
                root.Text = (controller.Estimates!.RootChordMeters * 1000 + 1).ToString("0.##", inv);
                root.Focus();
                U2Key(root, Key.Tab);
                U2WaitIdle(controller, window);
                if (controller.AcceptedSource == before) throw new InvalidOperationException("Tab did not commit the root chord");
                if (!tip.IsFocused) throw new InvalidOperationException("Tab did not move to the tip chord");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_TypeChange_StaysOnTypeControl", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                var type = U2Need<ComboBox>(host.Properties, "TypeControl");
                type.Focus();
                U2CommitType(type, TypeAnchorOption);
                Settle(window);
                if (!type.IsFocused) throw new InvalidOperationException("Type change moved focus");
                U2WaitIdle(controller, window);
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_ReturnOnPoint_SpanFieldEscapeBack", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")
                    ?? throw new InvalidOperationException("missing PlanCanvas");
                var span = U2Need<TextBox>(host.Properties, "PointSpanInput");
                canvas.Focus();
                U2Key(canvas, Key.Return);
                Settle(window);
                if (!span.IsFocused) throw new InvalidOperationException("Return did not focus the point span field");
                U2Key(span, Key.Escape);
                Settle(window);
                var focused = window.FocusManager?.GetFocusedElement();
                if (!ReferenceEquals(focused, canvas))
                    throw new InvalidOperationException("Escape did not return to the canvas");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Focus_TypeValueRequest_SpanFieldFocused", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                U2Invoke(host.Properties, "FocusTypeValue");
                Settle(window);
                if (!U2Need<TextBox>(host.Properties, "PointSpanInput").IsFocused)
                    throw new InvalidOperationException("Type-value request did not focus the span field");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("StatusLine_CommitReport_PoliteLiveRegion", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var point = U2Control(controller, "trailing");
                Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)));
                Settle(window);
                var status = StatusStripTests.Text(host);
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(status) != Avalonia.Automation.AutomationLiveSetting.Polite)
                    throw new InvalidOperationException("status live setting is not polite");
                if (!status.IsVisible || status.Text != controller.Status || string.IsNullOrWhiteSpace(status.Text))
                    throw new InvalidOperationException("status '" + status.Text + "' controller '" + controller.Status + "'");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Copy_M12bOutcomes_ExactStrings", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                if (U2Text(host.Properties, "PointHelper") != ControlHelper)
                    throw new InvalidOperationException("control helper");
                var root = controller.Planform!.Leading.Points.First(p => p.Role == PointRole.RootEnd);
                U2Select(controller, window, root);
                if (U2Text(host.Properties, "Note_pos_0") != "Fixed: the leading edge starts at the root.")
                    throw new InvalidOperationException("root constraint");
                var tip = controller.Planform.Trailing.Points.FirstOrDefault(p => p.Role == PointRole.TipEnd) ?? controller.Planform.Leading.Points.First(p => p.Role == PointRole.TipEnd);
                U2Select(controller, window, tip);
                // A named point states its Type as a locked fact; COPY-149 belongs under an editable anchor's Type.
                if (U2Text(host.Properties, "TypeReadOnly") != "Tip end")
                    throw new InvalidOperationException("tip end type: " + U2Text(host.Properties, "TypeReadOnly"));
                var anchorPoint = controller.Planform.Trailing.Points.First(p => p.Role == PointRole.Control);
                Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(anchorPoint.Curve, anchorPoint.Id)));
                U2Select(controller, window, U2Reload(controller, anchorPoint.Curve, anchorPoint.Id));
                if (U2Text(host.Properties, "PointHelper") != AnchorHelper)
                    throw new InvalidOperationException("anchor helper: " + U2Text(host.Properties, "PointHelper"));
                var a = controller.Planform.Trailing.Points.First(p => p.Role == PointRole.Control);
                var b = controller.Planform.Leading.Points.First(p => p.Role == PointRole.Control);
                controller.Select(new Selection.Points(new[] { new PointRef(a.Curve, a.Id), new PointRef(b.Curve, b.Id) }));
                Settle(window);
                if (U2Text(host.Properties, "Note_pos_0") != MixedCopy)
                    throw new InvalidOperationException("mixed copy");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Browser_RailGroups_SelectPointOnCanvas", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var leading = U2Need<ListBox>(host.Browser, "LeadingEdgeList");
                var trailing = U2Need<ListBox>(host.Browser, "TrailingEdgeList");
                var plan = controller.Planform!;
                if (leading.Items.Count != plan.Leading.Points.Count || trailing.Items.Count != plan.Trailing.Points.Count)
                    throw new InvalidOperationException("browser counts " + leading.Items.Count + "/" + trailing.Items.Count);
                host.LayoutFactory.LeftToolDock.ActiveDockable = host.LayoutFactory.BrowserTool;
                Settle(window);
                var leadingHeader = U2Need<TextBlock>(host.Browser, "LeadingEdgeHeader");
                var trailingHeader = U2Need<TextBlock>(host.Browser, "TrailingEdgeHeader");
                if (!leadingHeader.IsVisible || leadingHeader.Text != "Leading edge" || !trailingHeader.IsVisible || trailingHeader.Text != "Trailing edge")
                    throw new InvalidOperationException("rail group headers missing");
                var point = plan.Trailing.Points.First(p => p.Role == PointRole.Control);
                var row = trailing.Items.OfType<ListBoxItem>().FirstOrDefault(item => Equals(item.Tag, point.Id))
                    ?? throw new InvalidOperationException("browser row missing for " + point.Id);
                if (!row.GetVisualDescendants().Any(visual => visual.Name == "RoleGlyph"))
                    throw new InvalidOperationException("role glyph missing");
                string rowText = string.Join(" ", row.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text));
                if (!rowText.Contains("mm", StringComparison.Ordinal))
                    throw new InvalidOperationException("row text: " + rowText);
                trailing.SelectedItem = row;
                Settle(window);
                if (controller.Selection is not Selection.Points selected || selected.Items.Count != 1 || selected.Items[0].VertexId != point.Id)
                    throw new InvalidOperationException("selection: " + controller.Selection);
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("CommandTable_PointRows_ExecuteOrDisabled", () =>
        {
            string[] ids = ["point.make-anchor", "point.make-control", "point.tangent-smooth", "point.tangent-symmetric", "point.tangent-corner", "view.comb", "view.zoom-in", "view.zoom-out"];
            foreach (var id in ids)
                if (!CommandTable.Rows.Any(row => row.Id == id))
                    throw new InvalidOperationException("command row missing: " + id);
            var titles = new Dictionary<string, string>
            {
                ["point.make-anchor"] = "Make anchor",
                ["point.make-control"] = "Make control",
                ["point.tangent-smooth"] = "Smooth tangent",
                ["point.tangent-symmetric"] = "Symmetric tangent",
                ["point.tangent-corner"] = "Corner tangent",
                ["view.comb"] = "Curvature comb",
                ["view.zoom-in"] = "Zoom in",
                ["view.zoom-out"] = "Zoom out"
            };
            foreach (var pair in titles)
            {
                var row = CommandTable.Rows.Single(item => item.Id == pair.Key);
                if (row.Title != pair.Value) throw new InvalidOperationException(pair.Key + " title " + row.Title);
            }
            if (CommandTable.Rows.Single(row => row.Id == "view.zoom-in").Gesture != "⌘=")
                throw new InvalidOperationException("zoom in gesture");
            if (CommandTable.Rows.Single(row => row.Id == "view.zoom-out").Gesture != "⌘−")
                throw new InvalidOperationException("zoom out gesture");
            if (CommandTable.Rows.Single(row => row.Id == "view.comb").Gesture != "C")
                throw new InvalidOperationException("comb gesture");
            if (NativeMenuBuilder.ParseGesture("C") is not null)
                throw new InvalidOperationException("bare C is a window gesture");
            var zoomIn = NativeMenuBuilder.ParseGesture("⌘=") ?? throw new InvalidOperationException("⌘= did not parse");
            if (zoomIn.Key != Key.OemPlus || zoomIn.KeyModifiers == KeyModifiers.None)
                throw new InvalidOperationException("zoom in key");
            var zoomOut = NativeMenuBuilder.ParseGesture("⌘−") ?? throw new InvalidOperationException("⌘− did not parse");
            if (zoomOut.Key != Key.OemMinus) throw new InvalidOperationException("zoom out key");
            if (NativeMenuBuilder.ParseGesture("⌘-")?.Key != Key.OemMinus)
                throw new InvalidOperationException("hyphen minus did not parse");
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                string untouched = controller.AcceptedSource;
                U2Invoke(host, "RunCommand", "point.make-anchor");
                if (controller.AcceptedSource != untouched)
                    throw new InvalidOperationException("make-anchor ran with no point selected");
                var root = controller.Planform!.Leading.Points.First(p => p.Role == PointRole.RootEnd);
                U2Select(controller, window, root);
                U2Invoke(host, "RunCommand", "point.make-anchor");
                if (U2Reload(controller, root.Curve, root.Id).Role != PointRole.RootEnd)
                    throw new InvalidOperationException("make-anchor changed a fixed root");
                var point = U2Control(controller, "trailing");
                U2Select(controller, window, point);
                host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.Focus();
                double zoomBefore = controller.PlanCamera.PixelsPerMeter;
                bool combBefore = controller.CombVisible;
                string statusBefore = StatusStripTests.Text(host).Text ?? "";
                U2Invoke(host, "RunCommand", "view.zoom-in");
                if (controller.PlanCamera.PixelsPerMeter <= zoomBefore)
                    throw new InvalidOperationException("zoom in did not change the camera");
                U2Invoke(host, "RunCommand", "view.zoom-out");
                U2Invoke(host, "RunCommand", "view.comb");
                if (controller.CombVisible == combBefore)
                    throw new InvalidOperationException("comb did not toggle");
                var status = StatusStripTests.Text(host);
                if (!status.IsVisible || status.Text == statusBefore)
                    throw new InvalidOperationException("zoom/comb did not change the status line");
                U2Invoke(host, "RunCommand", "point.make-anchor");
                U2WaitIdle(controller, window);
                if (U2Reload(controller, point.Curve, point.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("make-anchor command did not change the point");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UI_DEAD_CONTROL_PointAndWingControlsHaveActions", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                U2Select(controller, window, U2Control(controller, "trailing"));
                var how = U2Need<Button>(host.Properties, "HowMeasuredButton");
                if (!how.IsEffectivelyVisible || !how.IsEnabled || !U2HasClick(how))
                    throw new InvalidOperationException("HowMeasuredButton is enabled without an action");
                var body = U2Need<TextBlock>(host.Properties, "HowMeasuredBody");
                bool was = body.IsVisible;
                U2Click(how);
                Settle(window);
                if (body.IsVisible == was) throw new InvalidOperationException("How measured click had no effect");
                var leadingControl = controller.Planform!.Leading.Points.First(p => p.Role == PointRole.Control);
                Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(leadingControl.Curve, leadingControl.Id)));
                var leading = U2Reload(controller, leadingControl.Curve, leadingControl.Id);
                U2Select(controller, window, leading);
                var kind = U2Need<ComboBox>(host.Properties, "KindControl");
                if (!kind.IsEffectivelyVisible || !kind.IsEnabled)
                    throw new InvalidOperationException("Tangent kind has no action");
                U2CommitType(kind, "Symmetric");
                U2WaitIdle(controller, window);
                if (U2Reload(controller, leading.Curve, leading.Id).Kind != TangentKind.Symmetric)
                    throw new InvalidOperationException("tangent click did not set symmetric");
                var failures = new List<string>();
                foreach (var button in host.Properties.GetVisualDescendants().OfType<Button>().Concat(host.Browser.GetVisualDescendants().OfType<Button>())
                    .Where(button => button.IsEffectivelyVisible && button.IsEnabled && button.Name?.StartsWith("PART_", StringComparison.Ordinal) != true &&
                        button is not ToggleButton { TemplatedParent: Expander }))   // a group header expands and collapses
                {
                    if (!U2HasClick(button)) failures.Add(button.Name ?? button.Content?.ToString() ?? "unnamed");
                }
                if (failures.Count > 0)
                    throw new InvalidOperationException("Enabled point or wing buttons without an action: " + string.Join(", ", failures));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Recovery_RailDraftResumed_PlanShowsDraftApplyCommits", () =>
        {
            string golden = Path.Combine(RepoRootFromSource(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b", "m12a-rail-recovery.cfdw");
            string path = ScratchPath("u2-rail-recovery.cfdw.json");
            File.WriteAllBytes(path, File.ReadAllBytes(golden));
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                Pump(host.OpenFileAsync(path));
                Settle(window);
                if (!controller.HasRecovery)
                    throw new InvalidOperationException("opened project has no recovery: " + controller.Status);
                var banner = U2Need<TextBlock>(host.Properties, "RecoveryBanner");
                if (!banner.IsVisible || banner.Text is null || !banner.Text.Contains("A recovered edit", StringComparison.Ordinal) || !banner.Text.Contains("is open.", StringComparison.Ordinal))
                    throw new InvalidOperationException("recovery banner: " + banner.Text);
                if (controller.Draft is null || controller.Planform?.Basis != "preview")
                    throw new InvalidOperationException("plan is not showing the recovery draft");
                if (!U2Text(host.Properties, "WingChipText").Contains("preview", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("wing chip does not show the draft: " + U2Text(host.Properties, "WingChipText"));
                string before = controller.AcceptedSource;
                U2Click(U2Need<Button>(host.Properties, "RecoveryApplyButton"));
                U2WaitIdle(controller, window);
                if (controller.HasRecovery || controller.Draft is not null)
                    throw new InvalidOperationException("apply left the recovery open");
                if (controller.AcceptedSource == before)
                    throw new InvalidOperationException("apply did not commit");
                _ = U2Need<Button>(host.Properties, "RecoveryDiscardButton");
            }
            finally { window.Close(); }
        });

        // CONTROL-GAMED-BY-RENAME: the retirement is checked by capability, not by member names. A rename cannot pass it.
        DesktopChecks.Check(string.Concat("Rail", "EditorPane_Removed_NoReferencesRemain"), () =>
        {
            var found = new List<string>();
            // 1. Every public MainWindow constructor builds the shell. No pre-shell window remains.
            foreach (var constructor in typeof(MainWindow).GetConstructors())
            {
                var window = (Window)constructor.Invoke(constructor.GetParameters().Select(_ => (object?)null).ToArray());
                try
                {
                    if (window.Content is not ShellHost)
                        found.Add($"MainWindow({string.Join(", ", constructor.GetParameters().Select(parameter => parameter.ParameterType.Name))}) builds {window.Content?.GetType().Name ?? "nothing"}, not the shell");
                }
                finally { window.Close(); }
            }
            // 2. No public member opens a draft from (rail, control id) whose single ordinate a scalar can then revise.
            //    Openers and revisers are found by signature, so a renamed member is still found.
            static bool Signature(System.Reflection.MethodInfo method, params Type[] types) =>
                !method.IsSpecialName && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(types);
            static object? Call(System.Reflection.MethodInfo method, object target, params object?[] arguments)
            {
                try
                {
                    object? result = method.Invoke(target, arguments);
                    if (result is Task task) Pump(task);
                    return result;
                }
                catch (Exception error) when (error is System.Reflection.TargetInvocationException or ContractError or InvalidOperationException or ArgumentException) { return null; }
            }
            var publicInstance = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var sessionOpeners = typeof(AuthoringSession).GetMethods(publicInstance)
                .Where(method => Signature(method, typeof(string), typeof(string), typeof(string))).ToArray();
            var sessionRevisers = typeof(AuthoringSession).GetMethods(publicInstance)
                .Where(method => Signature(method, typeof(string), typeof(long), typeof(double))).ToArray();
            var controllerOpeners = typeof(WorkbenchController).GetMethods(publicInstance)
                .Where(method => Signature(method, typeof(string), typeof(string))).ToArray();
            var controllerRevisers = typeof(WorkbenchController).GetMethods(publicInstance)
                .Where(method => Signature(method, typeof(double))).ToArray();
            using (var session = new AuthoringSession())
            {
                session.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
                var leading = session.Snapshot().Source;
                var editable = Planform.View(leading, "Accepted", 0).Leading.Points.First(point => point.Freedom != PointFreedom.Fixed);
                foreach (var opener in sessionOpeners)
                {
                    Call(opener, session, Guid.NewGuid().ToString("D"), "leading", editable.Id);
                    if (session.Snapshot().Draft is not { } opened) continue;
                    foreach (var reviser in sessionRevisers)
                    {
                        var before = session.Snapshot().Draft!;
                        Call(reviser, session, before.Id, before.Generation, 0.01);
                        if (session.Snapshot().Draft is { } after && after.Generation != before.Generation)
                            found.Add($"AuthoringSession.{opener.Name} then {reviser.Name} revises one control ordinate");
                    }
                    session.Cancel(opened.Id);
                }
            }
            string golden = Path.Combine(RepoRootFromSource(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b", "m12a-rail-recovery.cfdw");
            using (var controller = new WorkbenchController())
            {
                Pump(controller.OpenExampleAsync());
                var control = controller.Inspection!.Authored.Rails.Single(rail => rail.Name == "leading").Controls.First(item => item.Editable);
                foreach (var opener in controllerOpeners)
                {
                    Call(opener, controller, "leading", control.Id);
                    if (controller.Draft is null) continue;
                    foreach (var reviser in controllerRevisers)
                    {
                        long before = controller.Draft!.Generation;
                        Call(reviser, controller, control.OrdinateSi + .005);
                        if (controller.Draft is { } after && after.Generation != before)
                            found.Add($"WorkbenchController.{opener.Name} then {reviser.Name} revises one control ordinate");
                    }
                    controller.Cancel();
                }
                // 3. The one kept member: a resumed M1.2a rail recovery draft can be applied or discarded, never revised.
                string path = ScratchPath("capability-rail-recovery.cfdw.json");
                File.WriteAllBytes(path, File.ReadAllBytes(golden));
                Pump(controller.OpenPathAsync(path));
                if (!controller.HasRecovery) found.Add("golden M1.2a rail recovery did not open as a recovery");
                else
                {
                    controller.ResumeRecovery();
                    foreach (var reviser in controllerRevisers)
                    {
                        long before = controller.Draft?.Generation ?? -1;
                        Call(reviser, controller, 0.01);
                        if (controller.Draft is { } after && after.Generation != before)
                            found.Add($"WorkbenchController.{reviser.Name} revises a resumed rail recovery draft");
                    }
                }
            }
            if (found.Count > 0) throw new InvalidOperationException(string.Join("; ", found));
        });
    }

    private static Window U2Show(WorkbenchController controller, out ShellHost host)
    {
        host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 840 };
        window.Show();
        Settle(window);
        return window;
    }

    private static void U2Open(ShellHost host, Window window)
    {
        Pump(host.OpenExampleAsync());
        Settle(window);
    }

    private static PointView U2Control(WorkbenchController controller, string curve)
    {
        var plan = controller.Planform ?? throw new InvalidOperationException("planform missing");
        var rail = curve == "leading" ? plan.Leading : plan.Trailing;
        return rail.Points.FirstOrDefault(point => point.Role == PointRole.Control)
            ?? throw new InvalidOperationException("no control point on " + curve);
    }

    private static PointView U2Reload(WorkbenchController controller, string curve, string id)
    {
        var plan = controller.Planform ?? throw new InvalidOperationException("planform missing");
        var rail = curve == "leading" ? plan.Leading : plan.Trailing;
        return rail.Points.First(point => point.Id == id);
    }

    private static void U2Select(WorkbenchController controller, Window window, PointView point)
    {
        controller.Select(new Selection.Points(new[] { new PointRef(point.Curve, point.Id) }));
        Settle(window);
    }

    // The property grid names the parts it builds from the model (rows, notes, values) without registering them in the
    // pane's name scope, so a part is found by name in the logical tree when the name scope does not hold it.
    private static T U2Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name)
        ?? Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(root).OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? throw new InvalidOperationException("missing " + name);

    private const string TypeAnchorOption = "Anchor point";

    // PG-07: a type changes on a pick from the open list (an arrow on the closed box is only pending).
    private static void U2CommitType(ComboBox type, string text)
    {
        type.IsDropDownOpen = true;
        U2SelectCombo(type, text);
        type.IsDropDownOpen = false;
    }

    private static string U2Text(Control root, string name) => U2Need<TextBlock>(root, name).Text ?? "";

    private static void U2Key(Control control, Key key) =>
        control.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = control, Key = key });

    private static void U2Invoke(object target, string method, params object?[] args)
    {
        var found = target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (found is null) throw new InvalidOperationException(method + " missing");
        var result = found.Invoke(target, args);
        if (result is Task task) Pump(task);
    }

    private static bool U2HasClick(Button button)
    {
        if (button.Command is not null) return true;
        var store = typeof(Avalonia.Interactivity.Interactive).GetField("_eventHandlers",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(button) as System.Collections.IDictionary;
        return store?.Contains(Button.ClickEvent) == true;
    }

    private static void U2Click(Button button)
    {
        if (!U2HasClick(button)) throw new InvalidOperationException((button.Name ?? "button") + " has no action");
        if (button.Command is not null) button.Command.Execute(null);
        else button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    }

    private static IReadOnlyList<string> U2ComboTexts(ComboBox combo) =>
        combo.Items.Cast<object?>().Select(item => item is ComboBoxItem choice ? choice.Content?.ToString() ?? "" : item?.ToString() ?? "").ToList();

    private static string U2SelectedText(ComboBox combo) =>
        combo.SelectedItem is ComboBoxItem choice ? choice.Content?.ToString() ?? "" : combo.SelectedItem?.ToString() ?? "";

    private static void U2SelectCombo(ComboBox combo, string text)
    {
        int index = U2ComboTexts(combo).ToList().FindIndex(item => item == text);
        if (index < 0) throw new InvalidOperationException("combo missing " + text);
        combo.SelectedIndex = index;
    }

    private static double U2ParseMm(string? text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text ?? "", @"-?\d+(?:\.\d+)?");
        if (!match.Success) throw new InvalidOperationException("no millimetre value in '" + text + "'");
        return double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) / 1000d;
    }

    private static void U2Near(double actual, double expected, string label)
    {
        if (Math.Abs(actual - expected) > 5e-5)
            throw new InvalidOperationException(label + " " + actual.ToString("G6") + " vs " + expected.ToString("G6"));
    }

    private static double U2CurveGap(PlanformView plan, PointView point)
    {
        var samples = point.Curve == "leading" ? plan.Leading.Samples : plan.Trailing.Samples;
        double best = double.MaxValue;
        foreach (var sample in samples)
        {
            double span = sample.SpanMeters - point.SpanMeters;
            double aft = sample.Ordinate - point.Ordinate;
            best = Math.Min(best, Math.Sqrt(span * span + aft * aft));
        }
        return best;
    }

    private static void U2WaitIdle(WorkbenchController controller, Window window)
    {
        var start = DateTime.UtcNow;
        while (controller.Gesture != GestureState.Idle && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
        Settle(window);
    }

    /// <summary>A non-symlinked scratch path under the harness's temp root — TMPDIR under
    /// <c>tools/run-tests.sh</c> and under the readiness gate, the OS default otherwise — never
    /// a repo-relative path, so a scratch fixture does not depend on where the binary runs. macOS
    /// aliases /tmp to /private/tmp; the store refuses symlinked paths, so resolve the alias the
    /// same way <c>tools/run-tests.sh</c> and <c>LayoutFileTests.Root()</c> do.</summary>
    private static string ScratchPath(string name)
    {
        string temp = Path.GetTempPath();
        if (temp.StartsWith("/tmp/", StringComparison.Ordinal)) temp = "/private" + temp;
        if (temp.StartsWith("/var/", StringComparison.Ordinal)) temp = "/private" + temp;
        return Path.Combine(temp, name);
    }

    /// <summary>The Desktop example fixture, linked into the test output by the .csproj Content
    /// item — read from AppContext.BaseDirectory so it does not depend on where the binary runs.</summary>
    private static string ExamplePath() => Path.Combine(AppContext.BaseDirectory, "Assets", "example.foil");

    /// <summary>The repository root from this file's build-time source path (mirrors
    /// <c>SelfLaunch.cs</c>'s <c>NoRawProcessPathRelaunch</c>) — for the one check that genuinely
    /// needs the checked-out source tree (a scan of every .cs file under src/CfdWorkbench.Desktop),
    /// not a single linked fixture. Compile-time, so it never depends on where the binary runs.</summary>
    private static string RepoRootFromSource([CallerFilePath] string self = "")
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));
        if (!File.Exists(Path.Combine(root, "CFDWorkbench.slnx")))
            throw new DirectoryNotFoundException($"repository root not found from {self}");
        return root;
    }

    private static T StartControl<T>(StartView start, string name) where T : Control =>
        start.FindControl<T>(name) ?? throw new InvalidOperationException($"Start control {name} missing");

    /// <summary>DESIGN.md §7 COPY rows by id — the copy record every Copy_* check compares the build against.</summary>
    private static Dictionary<string, string> DesignCopyRows() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"))
            .Where(line => line.StartsWith("| COPY-", StringComparison.Ordinal))
            .Select(line => line.Split('|', 4))
            .Where(parts => parts.Length >= 3)
            .ToDictionary(parts => parts[1].Trim(), parts => parts[2].Trim());

    private static string CopyRow(Dictionary<string, string> rows, string id) =>
        rows.TryGetValue(id, out var row) ? row : throw new InvalidOperationException($"{id} is not a DESIGN.md COPY row");

    /// <summary>The start-card alert as one COPY-row line: title, message, then visible actions other than Dismiss.</summary>
    private static string StartAlertLine(StartView start)
    {
        var actions = ((Panel)StartControl<Button>(start, "AlertLocateButton").Parent!).Children.OfType<Button>()
            .Where(button => button.IsVisible && button.Name != "AlertDismissButton").Select(button => button.Content?.ToString());
        return string.Join(" · ", new[] { StartControl<TextBlock>(start, "AlertTitle").Text + " " +
            StartControl<TextBlock>(start, "AlertMessage").Text }.Concat(actions));
    }

    /// <summary>The model-area alert band as one COPY-row line: text, then visible actions other than Dismiss.</summary>
    private static string BandLine(ModelArea area)
    {
        var text = area.FindControl<TextBlock>("AlertBandText")!;
        var actions = ((Panel)text.Parent!).Children.OfType<Button>()
            .Where(button => button.IsVisible && button.Name != "DismissAlertBandButton").Select(button => button.Content?.ToString());
        return string.Join(" · ", new[] { text.Text }.Concat(actions));
    }

    private static void Pump(Task task)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!task.IsCompleted && !timeout.IsCancellationRequested)
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    /// <summary>Reads through the real store; every save fails with <c>DOC-IO</c> and publishes nothing.</summary>
    private sealed class FailingSaveStore : IProjectStore
    {
        private readonly ProjectStore inner = new();
        public void Dispose() => inner.Dispose();
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => inner.ReadAsync(path, cancellation);
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default) =>
            Task.FromResult(new SaveResult("DOC-IO", null, false, false));
    }

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    // The applied-paint oracle. Its pre-shell copy retired with the `--theme-controls` mode, so this is the only one.
    private static Color Solid(IBrush? brush, string label) =>
        brush is ISolidColorBrush { Color.A: 255 } solid && Math.Abs(brush.Opacity - 1) < 0.000001
            ? solid.Color
            : throw new InvalidOperationException($"Applied {label} has unresolved/nonopaque brush");

    /// <summary>B: an editable value has no box at rest (a transparent band), so its backdrop is the surface behind it.</summary>
    private static Color Backdrop(TextBox box, Border border, string label) =>
        border.Background is ISolidColorBrush { Color.A: 0 } ? Backing(box) : Solid(border.Background, label);

    private static IBrush? PropertyBrush(object value, string name) =>
        value.GetType().GetProperty(name)?.GetValue(value) as IBrush;

    private static Visual TextVisual(Control target)
    {
        if (target is TextBlock block && block.Text?.Length > 0 && block.Bounds.Width > 0 && block.Bounds.Height > 0)
            return block;
        return target.GetVisualDescendants().FirstOrDefault(item =>
            item.GetType().Name is "AccessText" or "TextBlock" &&
            PropertyBrush(item, "Foreground") is not null && item.Bounds.Width > 0 && item.Bounds.Height > 0)
            ?? throw new InvalidOperationException($"Rendered text presenter absent for {target.Name ?? target.GetType().Name}");
    }

    // Fluent's pressed Button shrinks to scale(0.98), so text may sit under a uniform scale in [0.97, 1].
    // Any other scale, rotation or skew is refused.
    private static bool PressScaleAccepted(Matrix matrix) =>
        matrix.M12 == 0 && matrix.M21 == 0 && Math.Abs(matrix.M11 - matrix.M22) <= 0.000001 &&
        matrix.M11 >= 0.97 && matrix.M11 <= 1 + 0.000001;

    private static Color Backing(Visual text)
    {
        var visibleBounds = new Rect(text.Bounds.Size);
        Color? backing = null;
        foreach (var layer in text.GetVisualAncestors().Reverse().Append(text))
        {
            if (Math.Abs(layer.Opacity - 1) > 0.000001)
                throw new InvalidOperationException($"Unknown group opacity on {layer.GetType().Name}");
            if (PropertyBrush(layer, "Background") is not { } paint) continue;
            if (paint is not ISolidColorBrush solid || Math.Abs(paint.Opacity - 1) > 0.000001)
                throw new InvalidOperationException($"Unknown background paint on {layer.GetType().Name}");
            if (solid.Color.A == 0) continue;
            if (solid.Color.A != 255)
                throw new InvalidOperationException($"Partial alpha {solid.Color} on {layer.GetType().Name}");
            var transform = text.TransformToVisual(layer);
            if (transform is not { } matrix || !PressScaleAccepted(matrix))
                throw new InvalidOperationException($"Text transform to {layer.GetType().Name} is outside the pressed-scale range");
            var area = visibleBounds.TransformToAABB(matrix);
            if (area.X < -.01 || area.Y < -.01 || area.Right > layer.Bounds.Width + .01 || area.Bottom > layer.Bounds.Height + .01)
                throw new InvalidOperationException($"Painted background does not enclose text on {layer.GetType().Name}");
            backing = solid.Color;
        }
        return backing ?? throw new InvalidOperationException("No proven opaque backing for rendered text");
    }

    private static double Contrast(Color first, Color second)
    {
        static double Linear(byte channel) { double value = channel / 255.0; return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4); }
        static double Luminance(Color color) => .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    private static bool FocusedToolTab(ShellHost host, string id) =>
        host.DockHost.GetVisualDescendants().OfType<Control>().Any(control =>
            control.GetType().Name == "ToolTabStripItem" && control.IsFocused &&
            string.Equals(control.DataContext?.GetType().GetProperty("Id")?.GetValue(control.DataContext)?.ToString(),
                id, StringComparison.Ordinal));
}
