using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

public static class ShellWindowTests
{
    public static void Run()
    {
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
            string root = Path.Combine(FindRepoRoot(), ".tmp-tests", "cfdw-d3a-recent-" + Guid.NewGuid().ToString("N"));
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
            string root = Path.Combine(FindRepoRoot(), ".tmp-tests", "cfdw-d3a-open-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "opened.foil");
            File.Copy(Path.Combine(FindRepoRoot(), "src", "CfdWorkbench.Desktop", "Assets", "example.foil"), path);
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
            string root = FindRepoRoot();
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
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")!;
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
                if (!host.ModelView.FindControl<Viewport>("FoilViewport")!.IsFocused)
                    throw new InvalidOperationException("Designer persona missed the viewport");
                host.FocusForPersona("screen-reader");
                Settle(window);
                if (!host.Browser.FindControl<ListBox>("StationList")!.Items.OfType<ListBoxItem>().Any(item => item.IsFocused))
                    throw new InvalidOperationException("Screen-reader persona missed Browser rows");
                host.FocusForPersona("dense");
                Settle(window);
                if (!host.RailEditor.FindControl<ListBox>("ControlList")!.Items.OfType<ListBoxItem>().Any(item => item.IsFocused))
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
            if (!ids.Contains("model") || !ids.Contains("section-sample") || !ids.Contains("foil-source"))
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
                if (!host.ModelView.FindControl<Control>("DocumentTabs")!.IsVisible ||
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
            var locked = controller.Inspection!.Authored.Rails
                .SelectMany(rail => rail.Controls.Select(control => (rail.Name, control)))
                .First(pair => !pair.control.Editable);
            var pane = new RailEditorPane();
            pane.Bind(controller);
            var list = pane.FindControl<ListBox>("ControlList")!;
            int index = controller.Inspection.Authored.Rails
                .SelectMany(rail => rail.Controls).TakeWhile(control => control.Id != locked.control.Id).Count();
            list.SelectedIndex = index;
            try
            {
                controller.BeginEdit(locked.Name, locked.control.Id);
                throw new InvalidOperationException("Locked rail control accepted an edit");
            }
            catch (ContractError error) when (error.Code == "DSL-LOCK") { }
            if (controller.Draft is not null || pane.FindControl<TextBox>("NumericInput")!.IsEnabled)
                throw new InvalidOperationException("Locked rail control enabled the numeric draft");
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
                double width = Viewport.PlotWidth(viewport.Bounds.Width, 178);
                if (width < 250 || viewport.AnnotationScroller.VerticalScrollBarVisibility != ScrollBarVisibility.Auto)
                    throw new InvalidOperationException($"Minimum-window plot is too narrow: {width}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("F6_RegionEntry_FocusesSelectedTabOrRow", () =>
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
            var rail = new RailEditorPane();
            rail.FindControl<ListBox>("ControlList")!.ItemsSource = new[] { new ListBoxItem { Content = "stale control" } };
            rail.Bind(null!);
            if (!rail.FindControl<Control>("ErrorPanel")!.IsVisible ||
                rail.FindControl<ListBox>("ControlList")!.ItemCount != 0)
                throw new InvalidOperationException("Rail editor retained stale controls after a bind failure");
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
                if (!host.ModelView.FindControl<Viewport>("FoilViewport")!.IsFocused)
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
                    !host.ModelView.FindControl<Viewport>("FoilViewport")!.IsFocused)
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
                var viewport = host.ModelView.FindControl<Viewport>("FoilViewport")!;
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
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "CFDWorkbench.slnx")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new DirectoryNotFoundException("CFDWorkbench.slnx not found");
    }

    private static T StartControl<T>(StartView start, string name) where T : Control =>
        start.FindControl<T>(name) ?? throw new InvalidOperationException($"Start control {name} missing");

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static bool FocusedToolTab(ShellHost host, string id) =>
        host.DockHost.GetVisualDescendants().OfType<Control>().Any(control =>
            control.GetType().Name == "ToolTabStripItem" && control.IsFocused &&
            string.Equals(control.DataContext?.GetType().GetProperty("Id")?.GetValue(control.DataContext)?.ToString(),
                id, StringComparison.Ordinal));
}
