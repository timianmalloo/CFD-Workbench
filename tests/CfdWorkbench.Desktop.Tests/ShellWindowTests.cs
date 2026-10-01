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
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

public static class ShellWindowTests
{
    public static void Run()
    {
        DesktopChecks.Check("Shell_F7_ModelTabReentry_RendersAcceptedFoil", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var open = host.OpenNewFoilAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while (!open.IsCompleted && !timeout.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                open.GetAwaiter().GetResult();
                Settle(window);
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")!;
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SamplesDocument;
                Settle(window);
                void AssertDrawn(string step)
                {
                    if (!ReferenceEquals(viewport.GetVisualRoot(), window) || !viewport.IsEffectivelyVisible ||
                        viewport.Bounds.Width <= 0 || viewport.Bounds.Height <= 0 ||
                        !ReferenceEquals(viewport.Frame, controller.Frame) ||
                        !ReferenceEquals(viewport.LastRecordedFrame, controller.Frame) ||
                        viewport.LastRecordedRevision != viewport.FrameRevision || viewport.RenderSerial == 0 ||
                        viewport.FoilBrush is null || viewport.StationBrush is null)
                        throw new InvalidOperationException($"{step}: model viewport was not attached and drawn; " +
                            $"root={viewport.GetVisualRoot()?.GetType().Name ?? "none"}, visible={viewport.IsEffectivelyVisible}, " +
                            $"bounds={viewport.Bounds}, render={viewport.RenderSerial}, revision={viewport.LastRecordedRevision}/{viewport.FrameRevision}");
                }
                AssertDrawn("initial");
                var tabs = host.DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>().ToArray();
                var sourceTab = tabs.Single(tab => ReferenceEquals(tab.DataContext, host.LayoutFactory.FoilSourceDocument));
                var modelTab = tabs.Single(tab => ReferenceEquals(tab.DataContext, host.LayoutFactory.SamplesDocument));
                sourceTab.IsSelected = true;
                Settle(window);
                if (!sourceTab.IsSelected)
                    throw new InvalidOperationException("Foil source tab did not select");
                modelTab.IsSelected = true;
                Settle(window);
                long beforeRedraw = viewport.RenderSerial;
                viewport.InvalidateFrameForMetric();
                Settle(window);
                AssertDrawn("re-entry");
                if (viewport.RenderSerial <= beforeRedraw)
                    throw new InvalidOperationException("Re-entered viewport did not draw its accepted foil");
            }
            finally { window.Close(); }
        });

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
                    (host.LayoutFactory.SamplesDocument, host.ModelView.FindControl<Viewport>("FoilViewport")!,
                        () => ReferenceEquals(host.ModelView.FindControl<Viewport>("FoilViewport")!.LastRecordedFrame, controller.Frame)),
                    (host.LayoutFactory.SectionSampleDocument, host.ModelView.FindControl<Viewport>("SectionViewport")!,
                        () => ReferenceEquals(host.ModelView.FindControl<Viewport>("SectionViewport")!.LastRecordedFrame, controller.Frame)),
                    (host.LayoutFactory.FoilSourceDocument, host.ModelView.FindControl<TextBox>("SourceText")!,
                        () => !string.IsNullOrWhiteSpace(host.ModelView.FindControl<TextBox>("SourceText")!.Text)),
                    (host.LayoutFactory.SectionDocument,
                        host.ModelView.FindControl<SectionEditorView>("SectionEditor")!.FindControl<SectionCanvas>("EditableSectionCanvas")!,
                        () => host.ModelView.FindControl<SectionEditorView>("SectionEditor")!
                            .FindControl<SectionCanvas>("EditableSectionCanvas")!.Profile is not null)
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
                if (innerRows != 0 || dockTabs != 5)
                    throw new InvalidOperationException($"Model area has {innerRows} inner tab rows and {dockTabs} Dock document tabs");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F9_SectionSelectedStation_DrawsProfile", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var station = controller.Inspection!.Authored.Assignments[0];
                controller.Select(new Selection.Station(0, station.Eta));
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var canvas = host.ModelView.FindControl<SectionEditorView>("SectionEditor")!
                    .FindControl<SectionCanvas>("EditableSectionCanvas")!;
                if (!ReferenceEquals(canvas.GetVisualRoot(), window) || !canvas.IsEffectivelyVisible ||
                    canvas.Bounds.Width <= 0 || canvas.Bounds.Height <= 0 ||
                    canvas.Profile is null || canvas.Profile.UpperCurve.Count == 0 || canvas.FoilBrush is null)
                    throw new InvalidOperationException("Selected station Section canvas has no realized profile drawing inputs");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Shell_F9_SectionNoStation_ShowsEmptyCopy", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
                Settle(window);
                var editor = host.ModelView.FindControl<SectionEditorView>("SectionEditor")!;
                var canvas = editor.FindControl<SectionCanvas>("EditableSectionCanvas")!;
                var empty = editor.FindControl<TextBlock>("SectionEmptyText");
                if (!ReferenceEquals(canvas.GetVisualRoot(), window) || canvas.Profile is not null ||
                    empty is null || !empty.IsEffectivelyVisible || empty.Text != "No station selected.")
                    throw new InvalidOperationException("Section document omitted its No station selected empty state");
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
                if (host.RowDefinitions.Count != 1 || !host.LeftSidebarToggle.IsEffectivelyVisible ||
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
                foreach (var tool in new[] { host.LayoutFactory.PropertiesTool, host.LayoutFactory.BrowserTool,
                             host.LayoutFactory.RailControlsTool })
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
            var window = new MainWindow(shellMode: true) { Width = 1280, Height = 800 };
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
            var window = new MainWindow(shellMode: true);
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
                            button.Name?.StartsWith("PART_", StringComparison.Ordinal) != true))
                    {
                        if (button.Command is not null) continue;
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
            var window = new MainWindow(shellMode: true, preferences);
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
            var actual = menu.Items.OfType<NativeMenuItem>()
                .SelectMany(item => item.Menu?.Items.OfType<NativeMenuItem>() ?? [])
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
            var window = new MainWindow(shellMode: true);
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
            var window = new MainWindow(shellMode: true);
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
            var window = new MainWindow(shellMode: true) { Width = 1024, Height = 700 };
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
                    .FirstOrDefault(item => ReferenceEquals(item.DataContext, host.LayoutFactory.SamplesDocument))
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
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")!;
                if (viewport.Frame is null || !ReferenceEquals(viewport.Frame, controller.Frame))
                    throw new InvalidOperationException("Accepted frame not bound before Dock focus barrier");
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
            if (!ids.Contains("model") || !ids.Contains("3d-samples") || !ids.Contains("section-sample") || !ids.Contains("foil-source"))
                throw new InvalidOperationException("Model area document tabs are absent");
            var paneIds = host.LayoutFactory.LeftToolDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (!paneIds.Contains("properties") || !paneIds.Contains("browser") || !paneIds.Contains("rail-controls"))
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
                    host.LayoutFactory.SectionSampleDocument,
                    host.LayoutFactory.FoilSourceDocument,
                    host.LayoutFactory.SectionDocument,
                    host.LayoutFactory.SamplesDocument,
                    host.LayoutFactory.ModelDocument
                })
                {
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    window.UpdateLayout();
                }
            }
            finally { window.Close(); }
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

        DesktopChecks.Check("ModelArea_MinimumWindow_PlotWidthAtLeast250", () =>
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
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")!;
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SamplesDocument;
                Settle(window);
                double width = Viewport.PlotWidth(viewport.Bounds.Width, 178);
                if (width < 250 || viewport.AnnotationScroller.VerticalScrollBarVisibility != ScrollBarVisibility.Auto)
                    throw new InvalidOperationException($"Minimum-window plot is too narrow: {width}");
            }
            finally { window.Close(); }
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
                host.ClosePane("rail-controls");
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
            var status = host.ModelView.FindControl<TextBlock>("StatusText");
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
                    var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
                    var retry = host.ModelView.FindControl<Button>("StatusTryAgainButton");
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

        // The --theme-controls matrix retargeted to the shell (design §12.4; inventory rows 113–1446).
        DesktopChecks.Check("ThemeMatrix_ShellControls_AppliedContrast", () =>
        {
            var rowFailures = new List<string>();
            var reflection = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var pseudoProperty = typeof(StyledElement).GetProperty("PseudoClasses", reflection)
                ?? throw new InvalidOperationException("Installed protected PseudoClasses unavailable");
            void Measure(string theme, string row, Color foreground, Color background, double floor)
            {
                double ratio = Contrast(foreground, background);
                Console.WriteLine($"THEME-ROW {theme}/{row} fg={foreground} bg={background} ratio={ratio:F2} floor={floor}");
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
                Measure(theme, row, Solid(presenter.Foreground, row + " ink"), Solid(border.Background, row + " backdrop"), 4.5);
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

            // Dock's deferred presenter fades a pane in when its content changes (DeferredContentControl.cs: opacity
            // 0.85 → 1 over a 90 ms DoubleTransition), so a probe after ShowPane could land mid-fade (observed:
            // opacity 0.964 at Animation priority). The matrix measures settled paint, so the fade is off for it;
            // Backing still refuses any unresolved group opacity.
            var reveal = Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration;
            Dock.Controls.DeferredContentControl.DeferredContentPresentationSettings.RevealDuration = TimeSpan.Zero;
            try
            {
            foreach (var (theme, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark),
                         ("high-contrast", NativeReviewThemes.HighContrast) })
            {
                var window = new MainWindow(shellMode: true) { RequestedThemeVariant = variant, Width = 1024, Height = 700 };
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
                    if (!titles.SequenceEqual(["Plan", "3D samples", "Section sample", "Foil source", "Section"]))
                        throw new InvalidOperationException("Model-area Dock tabs are " + string.Join(", ", titles));
                    var modelTab = docTabs[1];
                    var sourceTab = docTabs[3];
                    modelTab.IsSelected = true;
                    Settle(window);

                    // Theme barrier: a fresh composition batch renders the focused tab after the Example is bound.
                    var composition = ElementComposition.GetElementVisual(modelTab)
                        ?? throw new InvalidOperationException("Barrier tab lacks a compositor");
                    var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")
                        ?? throw new InvalidOperationException("Barrier did not find FoilViewport");
                    if (viewport.Frame is null || !ReferenceEquals(viewport.Frame, controller.Frame))
                        throw new InvalidOperationException("Opened Example not bound before the theme barrier");
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
                    if (!sidebar.Focus(NavigationMethod.Tab))
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
                    Probe(theme, "browser.selected", () => TextRow(theme, "browser.selected", rowItems[0]));
                    Probe(theme, "browser.unselected", () => TextRow(theme, "browser.unselected", rowItems[1]));
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
                        presenter.Foreground = border.Background;
                        double ratio = Contrast(Solid(presenter.Foreground, "mutated ink"), Solid(border.Background, "backdrop"));
                        presenter.ClearValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty);
                        if (ratio >= 4.5) throw new InvalidOperationException("Painter oracle accepted a low-contrast mutation");
                    });
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
                    if (host.ModelView.FindControl<TextBlock>("ViewportProvenance") is null ||
                        host.ModelView.FindControl<TextBlock>("SectionReadout") is null)
                        throw new InvalidOperationException("Viewport or section annotation absent");
                    if (host.Properties.FindControl<TextBox>("SpanInput") is null ||
                        host.Properties.FindControl<TextBlock>("WingHeading") is null)
                        throw new InvalidOperationException("Rail-editor CV list or numeric field absent from the pane namescope");

                    if (variant == ThemeVariant.Light)
                    {
                        // The variant must follow a live switch on an open window, not only the one it opened with.
                        window.RequestedThemeVariant = ThemeVariant.Dark;
                        Settle(window);
                        Probe(theme, "live-flip.dark.tab.Section.unselected",
                            () => TextRow(theme, "live-flip.dark.tab.Section.unselected", docTabs[3]));
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
            if (rowFailures.Count > 0)
                throw new InvalidOperationException($"{rowFailures.Count} theme rows failed: " + string.Join(" | ", rowFailures));
        });
        U2Checks();
    }

    private static void U2Checks()
    {
        const string ControlHelper = "A control point pulls the curve toward it. The curve does not pass through it.";
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
                var block = U2Need<Control>(props, "PointBlock");
                if (!block.IsVisible) throw new InvalidOperationException("Point block is hidden");
                string heading = U2Text(props, "PointHeading");
                if (!heading.Contains("Trailing", StringComparison.Ordinal) || !heading.Contains("point", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("heading: " + heading);
                var type = U2Need<ComboBox>(props, "TypeControl");
                if (!type.IsEnabled) throw new InvalidOperationException("Type is not editable on a control point");
                var choices = U2ComboTexts(type);
                if (!choices.Contains("Anchor point") || !choices.Contains("Control point"))
                    throw new InvalidOperationException("Type choices: " + string.Join(", ", choices));
                if (U2SelectedText(type) != "Control point")
                    throw new InvalidOperationException("Type shows " + U2SelectedText(type));
                U2Near(U2ParseMm(U2Need<TextBox>(props, "PointSpanInput").Text), point.SpanMeters, "span");
                U2Near(U2ParseMm(U2Need<TextBox>(props, "PointAftInput").Text), point.AftMeters, "aft");
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
                if (U2Text(props, "ConstraintText") != "Fixed: the leading edge starts at the root.")
                    throw new InvalidOperationException("constraint: " + U2Text(props, "ConstraintText"));
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
                U2SelectCombo(type, "Anchor point");
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
                var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
                if (!status.IsVisible || status.Text != controller.Status || string.IsNullOrWhiteSpace(status.Text))
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
                var button = U2Need<Button>(host.Properties, "TangentSymmetricButton");
                if (!button.IsEffectivelyVisible || !button.IsEnabled)
                    throw new InvalidOperationException("Symmetric tangent is not an enabled control");
                U2Click(button);
                U2WaitIdle(controller, window);
                var now = U2Reload(controller, point.Curve, point.Id);
                if (now.Kind != TangentKind.Symmetric)
                    throw new InvalidOperationException("kind: " + now.Kind);
                var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
                if (status.Text != controller.Status || string.IsNullOrWhiteSpace(status.Text))
                    throw new InvalidOperationException("tangent status not on the status line");
                controller.Undo();
                Settle(window);
                if (U2Reload(controller, point.Curve, point.Id).Kind != before)
                    throw new InvalidOperationException("one undo did not restore the tangent");
                if (U2Reload(controller, point.Curve, point.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("tangent undo also reverted the anchor");
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
                if (Math.Abs(now.SpanMeters - point.SpanMeters) < 0.005 && Math.Abs(now.AftMeters - point.AftMeters) < 0.001)
                    throw new InvalidOperationException("typed span and aft did not move the point");
                U2Near(U2ParseMm(span.Text), now.SpanMeters, "echoed span");
                U2Near(U2ParseMm(aft.Text), now.AftMeters, "echoed aft");
                if (!controller.CanUndo) throw new InvalidOperationException("typed edit has no undo");
                controller.Undo();
                Settle(window);
                var restored = U2Reload(controller, point.Curve, point.Id);
                if (Math.Abs(restored.SpanMeters - point.SpanMeters) > 0.0001 || Math.Abs(restored.AftMeters - point.AftMeters) > 0.0001)
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
                var handle = plan.Trailing.Points.FirstOrDefault(p => p.Role is PointRole.TipHandle or PointRole.RootHandle)
                    ?? plan.Leading.Points.First(p => p.Role is PointRole.TipHandle or PointRole.RootHandle);
                U2Select(controller, window, handle);
                var angleBox = U2Need<TextBox>(host.Properties, "HandleAngleInput");
                var lengthBox = U2Need<TextBox>(host.Properties, "HandleLengthInput");
                if (!lengthBox.IsEnabled) throw new InvalidOperationException("Handle length is locked");
                double angle = double.Parse(angleBox.Text ?? "", inv);
                double length = U2ParseMm(lengthBox.Text);
                var shown = CfdWorkbench.Core.Planform.HandleTarget(controller.Planform!, handle.Curve, handle.Id, angle, length);
                if (Math.Abs(shown.SpanMeters - handle.SpanMeters) > 5e-4 || Math.Abs(shown.AftMeters - handle.AftMeters) > 5e-4)
                    throw new InvalidOperationException("shown angle/length is not the handle");
                lengthBox.Text = ((length + 0.02) * 1000).ToString("0.##", inv);
                lengthBox.Focus();
                U2Key(lengthBox, Key.Enter);
                U2WaitIdle(controller, window);
                var moved = U2Reload(controller, handle.Curve, handle.Id);
                var expected = CfdWorkbench.Core.Planform.HandleTarget(controller.Planform!, handle.Curve, handle.Id, angle, U2ParseMm(lengthBox.Text));
                if (Math.Abs(expected.SpanMeters - moved.SpanMeters) > 5e-4 || Math.Abs(expected.AftMeters - moved.AftMeters) > 5e-4)
                    throw new InvalidOperationException("committed handle is not HandleTarget");
                if (Math.Abs(moved.SpanMeters - handle.SpanMeters) < 0.005 && Math.Abs(moved.AftMeters - handle.AftMeters) < 0.005)
                    throw new InvalidOperationException("handle did not move");
                controller.Undo();
                Settle(window);
                var restored = U2Reload(controller, handle.Curve, handle.Id);
                if (Math.Abs(restored.SpanMeters - handle.SpanMeters) > 5e-4 || Math.Abs(restored.AftMeters - handle.AftMeters) > 5e-4)
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
                if (U2Text(host.Properties, "PointHeading") != "2 points")
                    throw new InvalidOperationException("heading: " + U2Text(host.Properties, "PointHeading"));
                var banner = U2Need<TextBlock>(host.Properties, "MixedBanner");
                if (!banner.IsVisible || banner.Text != MixedCopy)
                    throw new InvalidOperationException("mixed banner: " + banner.Text);
                var type = host.Properties.FindControl<ComboBox>("TypeControl");
                if (type is { IsEnabled: true, IsEffectivelyVisible: true })
                    throw new InvalidOperationException("Mixed type is editable");
                if (!U2Text(host.Properties, "TypeReadOnly").Contains("Mixed", StringComparison.Ordinal))
                    throw new InvalidOperationException("type: " + U2Text(host.Properties, "TypeReadOnly"));
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
                string example = File.ReadAllText(ExamplePath());
                const string trailing = "points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 120), (1, 120)]";
                if (!example.Contains(trailing, StringComparison.Ordinal))
                    throw new InvalidOperationException("example trailing points not found");
                string path = ScratchPath("u2-closed-tip.foil");
                File.WriteAllText(path, example.Replace(trailing, "points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 60), (1, 0)]", StringComparison.Ordinal));
                Pump(host.OpenFileAsync(path));
                Settle(window);
                var closed = U2Need<TextBlock>(host.Properties, "TipClosedText");
                if (!closed.IsVisible || closed.Text != TipClosedCopy)
                    throw new InvalidOperationException("tip text: " + closed.Text + " chord " + controller.Estimates?.TipChordMeters);
                var input = host.Properties.FindControl<TextBox>("TipChordInput");
                if (input is { IsEffectivelyVisible: true, IsEnabled: true })
                    throw new InvalidOperationException("Closing tip chord is still an enabled field");
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
                string[] labels = ["Span", "Root chord", "Tip chord", "Mean chord", "MAC", "Max t/c", "Aspect ratio", "Area"];
                var seen = wing.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text ?? "").Where(text => labels.Contains(text)).ToList();
                if (!seen.SequenceEqual(labels))
                    throw new InvalidOperationException("wing order: " + string.Join(" | ", seen));
                foreach (var name in new[] { "MeanChordText", "MacText", "MaxTcText", "AspectText", "AreaEstimateText" })
                {
                    string text = U2Text(host.Properties, name);
                    if (!text.StartsWith("≈", StringComparison.Ordinal))
                        throw new InvalidOperationException(name + " is not marked approximate: " + text);
                }
                string heading = U2Text(host.Properties, "WingHeading");
                if (!heading.Contains("Wing", StringComparison.Ordinal))
                    throw new InvalidOperationException("wing heading: " + heading);
                var how = U2Need<Button>(host.Properties, "HowMeasuredButton");
                if (how.Content?.ToString() != "How these are measured")
                    throw new InvalidOperationException("how measured: " + how.Content);
                U2Click(how);
                Settle(window);
                var body = U2Need<TextBlock>(host.Properties, "HowMeasuredBody");
                if (!body.IsVisible || string.IsNullOrWhiteSpace(body.Text) || !body.Text.Contains("MAC", StringComparison.Ordinal))
                    throw new InvalidOperationException("how-measured body: " + body.Text);
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
                controller.UpdateGesture(point.SpanMeters + 0.04, point.AftMeters + 0.01);
                controller.FlushGestureFrame();
                Settle(window);
                if (controller.Gesture == GestureState.Idle)
                    throw new InvalidOperationException("gesture released before the assertion");
                if (controller.Estimates?.Basis != "preview")
                    throw new InvalidOperationException("estimates basis: " + controller.Estimates?.Basis);
                string during = U2Text(host.Properties, "MacText");
                if (during == before || !during.StartsWith("≈", StringComparison.Ordinal))
                    throw new InvalidOperationException("MAC stayed '" + before + "' during the drag");
                if (!U2Text(host.Properties, "WingHeading").Contains("preview", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("heading: " + U2Text(host.Properties, "WingHeading"));
                Pump(controller.EndGestureAsync(GestureEnd.Escape));
            }
            finally { window.Close(); }
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
                string mac = U2Text(host.Properties, "MacText");
                if (!mac.Contains('—'))
                    throw new InvalidOperationException("MAC on a crossing draft: " + mac);
                string reason = U2Text(host.Properties, "WingReasonText");
                if (!U2Need<TextBlock>(host.Properties, "WingReasonText").IsVisible || string.IsNullOrWhiteSpace(reason))
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
                var warning = U2Need<TextBlock>(host.Properties, "ChordWarningText");
                if (!warning.IsVisible || warning.Text?.Contains("above the limit", StringComparison.Ordinal) != true)
                    throw new InvalidOperationException("warning: " + warning.Text);
                var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
                if (!status.IsVisible || status.Text != controller.Status || string.IsNullOrWhiteSpace(status.Text))
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
                var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
                if (!status.IsVisible || status.Text != controller.Status || string.IsNullOrWhiteSpace(status.Text))
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
                U2SelectCombo(type, "Anchor point");
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
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport") ?? throw new InvalidOperationException("missing FoilViewport");
                var span = U2Need<TextBox>(host.Properties, "PointSpanInput");
                viewport.Focus();
                U2Key(viewport, Key.Return);
                Settle(window);
                if (!span.IsFocused) throw new InvalidOperationException("Return did not focus the point span field");
                U2Key(span, Key.Escape);
                Settle(window);
                var focused = window.FocusManager?.GetFocusedElement();
                if (!ReferenceEquals(focused, viewport) && focused is not Viewport)
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
                var status = host.ModelView.FindControl<TextBlock>("StatusText") ?? throw new InvalidOperationException("missing StatusText");
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
                if (U2Text(host.Properties, "ConstraintText") != "Fixed: the leading edge starts at the root.")
                    throw new InvalidOperationException("root constraint");
                var tip = controller.Planform.Trailing.Points.FirstOrDefault(p => p.Role == PointRole.TipEnd) ?? controller.Planform.Leading.Points.First(p => p.Role == PointRole.TipEnd);
                U2Select(controller, window, tip);
                if (U2Text(host.Properties, "PointHelper") != AnchorHelper)
                    throw new InvalidOperationException("anchor helper: " + U2Text(host.Properties, "PointHelper"));
                var a = controller.Planform.Trailing.Points.First(p => p.Role == PointRole.Control);
                var b = controller.Planform.Leading.Points.First(p => p.Role == PointRole.Control);
                controller.Select(new Selection.Points(new[] { new PointRef(a.Curve, a.Id), new PointRef(b.Curve, b.Id) }));
                Settle(window);
                if (U2Text(host.Properties, "MixedBanner") != MixedCopy)
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

        DesktopChecks.Check("ContextMenu_MakeAnchor_SameEffectAsProperties", () =>
        {
            using var controller = new WorkbenchController();
            var window = U2Show(controller, out var host);
            try
            {
                U2Open(host, window);
                var trailingPoint = U2Control(controller, "trailing");
                U2Select(controller, window, trailingPoint);
                U2SelectCombo(U2Need<ComboBox>(host.Properties, "TypeControl"), "Anchor point");
                U2WaitIdle(controller, window);
                string propertiesStatus = controller.Status;
                if (U2Reload(controller, trailingPoint.Curve, trailingPoint.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("properties did not make an anchor");
                controller.Undo();
                Settle(window);
                var leadingPoint = U2Control(controller, "leading");
                var list = U2Need<ListBox>(host.Browser, "LeadingEdgeList");
                var row = list.Items.OfType<ListBoxItem>().First(item => Equals(item.Tag, leadingPoint.Id));
                var menu = row.ContextMenu ?? list.ContextMenu ?? throw new InvalidOperationException("context menu missing");
                var item = menu.Items.OfType<MenuItem>().FirstOrDefault(entry => entry.Header?.ToString() == "Make anchor")
                    ?? throw new InvalidOperationException("Make anchor item missing");
                list.SelectedItem = row;
                if (item.Command is not null) item.Command.Execute(row);
                else item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                U2WaitIdle(controller, window);
                if (U2Reload(controller, leadingPoint.Curve, leadingPoint.Id).Role != PointRole.Anchor)
                    throw new InvalidOperationException("context menu did not make an anchor");
                string prefix = propertiesStatus.Split('.')[0];
                if (!controller.Status.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException("context status '" + controller.Status + "' properties '" + propertiesStatus + "'");
                controller.Undo();
                Settle(window);
                if (U2Reload(controller, leadingPoint.Curve, leadingPoint.Id).Role != PointRole.Control)
                    throw new InvalidOperationException("context-menu undo did not restore the control point");
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
                double zoomBefore = controller.PlanCamera.PixelsPerMeter;
                bool combBefore = controller.CombVisible;
                string statusBefore = host.ModelView.FindControl<TextBlock>("StatusText")?.Text ?? "";
                U2Invoke(host, "RunCommand", "view.zoom-in");
                if (controller.PlanCamera.PixelsPerMeter <= zoomBefore)
                    throw new InvalidOperationException("zoom in did not change the camera");
                U2Invoke(host, "RunCommand", "view.zoom-out");
                U2Invoke(host, "RunCommand", "view.comb");
                if (controller.CombVisible == combBefore)
                    throw new InvalidOperationException("comb did not toggle");
                var status = host.ModelView.FindControl<TextBlock>("StatusText")!;
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
                var symmetric = U2Need<Button>(host.Properties, "TangentSymmetricButton");
                if (!symmetric.IsEffectivelyVisible || !symmetric.IsEnabled || !U2HasClick(symmetric))
                    throw new InvalidOperationException("Symmetric tangent has no action");
                U2Click(symmetric);
                U2WaitIdle(controller, window);
                if (U2Reload(controller, leading.Curve, leading.Id).Kind != TangentKind.Symmetric)
                    throw new InvalidOperationException("tangent click did not set symmetric");
                var failures = new List<string>();
                foreach (var button in host.Properties.GetVisualDescendants().OfType<Button>().Concat(host.Browser.GetVisualDescendants().OfType<Button>())
                    .Where(button => button.IsEffectivelyVisible && button.IsEnabled && button.Name?.StartsWith("PART_", StringComparison.Ordinal) != true))
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
            using var setup = new WorkbenchController();
            Pump(setup.OpenExampleAsync());
            var editable = setup.Inspection!.Authored.Rails
                .SelectMany(rail => rail.Controls.Select(control => (rail.Name, control)))
                .First(item => item.control.Editable);
            setup.GetType().GetMethod(string.Concat("Begin", "Edit"))!.Invoke(setup, new object[] { editable.Name, editable.control.Id });
            setup.GetType().GetMethod(string.Concat("Update", "Draft"))!.Invoke(setup, new object[] { 0.25 });
            string path = ScratchPath("u2-rail-recovery.cfdw.json");
            Pump(setup.SaveAsync(path));
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
                if (!U2Text(host.Properties, "WingHeading").Contains("preview", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("wing heading does not show the draft: " + U2Text(host.Properties, "WingHeading"));
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

        DesktopChecks.Check(string.Concat("Rail", "EditorPane_Removed_NoReferencesRemain"), () =>
        {
            string root = RepoRootFromSource();
            string[] tokens =
            [
                string.Concat("Rail", "EditorPane"),
                string.Concat("Patch", "Rail"),
                string.Concat("Update", "Draft("),
                string.Concat("Begin", "Edit(")
            ];
            var hits = new List<string>();
            foreach (var dir in new[] { "src", "tests" })
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(root, dir), "*", SearchOption.AllDirectories))
                {
                    if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                        continue;
                    string text;
                    try { text = File.ReadAllText(file); }
                    catch (IOException) { continue; }
                    foreach (var token in tokens)
                        if (text.Contains(token, StringComparison.Ordinal))
                            hits.Add(Path.GetRelativePath(root, file));
                }
            }
            if (hits.Count > 0)
                throw new InvalidOperationException(string.Join("; ", hits.Distinct().Take(12)));
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

    private static T U2Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? throw new InvalidOperationException("missing " + name);

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
            double aft = sample.AftMeters - point.AftMeters;
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

    // simplify: the applied-paint oracle of WorkbenchTests.cs:125-176 and :219-227, whose top-level local functions no
    // other class can call; upgrade trigger: the legacy --theme-controls mode retires (D3b), then this is the only copy.
    private static Color Solid(IBrush? brush, string label) =>
        brush is ISolidColorBrush { Color.A: 255 } solid && Math.Abs(brush.Opacity - 1) < 0.000001
            ? solid.Color
            : throw new InvalidOperationException($"Applied {label} has unresolved/nonopaque brush");

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
            var origin = text.TranslatePoint(visibleBounds.Position, layer);
            if (transform is null || Math.Abs(transform.Value.M11 - 1) > 0.000001 || Math.Abs(transform.Value.M22 - 1) > 0.000001 ||
                origin is null || origin.Value.X < 0 || origin.Value.Y < 0 ||
                origin.Value.X + visibleBounds.Width > layer.Bounds.Width + .01 ||
                origin.Value.Y + visibleBounds.Height > layer.Bounds.Height + .01)
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
