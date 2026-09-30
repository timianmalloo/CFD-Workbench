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
            string root = Path.Combine(FindRepoRoot(), ".tmp-tests", "cfdw-private-" + Guid.NewGuid().ToString("N"));
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
                    File.Copy(Path.Combine(FindRepoRoot(), "src", "CfdWorkbench.Desktop", "Assets", "example.foil"), path);
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
                    .FirstOrDefault(item => ReferenceEquals(item.DataContext, host.LayoutFactory.ModelDocument))
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

        DesktopChecks.Check("AppBar_LiveThemeSwitch_RefreshesBrushes", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700,
                RequestedThemeVariant = ThemeVariant.Light };
            try
            {
                window.Show();
                Settle(window);
                var bar = (Border)host.Children[0];
                var light = ((ISolidColorBrush)bar.Background!).Color;
                var lightBorder = ((ISolidColorBrush)bar.BorderBrush!).Color;
                window.RequestedThemeVariant = ThemeVariant.Dark;
                Settle(window);
                var dark = ((ISolidColorBrush)bar.Background!).Color;
                var darkBorder = ((ISolidColorBrush)bar.BorderBrush!).Color;
                if (light == dark || lightBorder == darkBorder)
                    throw new InvalidOperationException($"App bar kept the old theme brushes: {light} / {dark}");
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
            var rows = File.ReadAllLines(Path.Combine(FindRepoRoot(), "DESIGN.md"))
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
            var rail = new RailEditorPane();
            var panes = new (string Name, Action<bool> Show, Func<string?> Text)[]
            {
                ("Properties", properties.ShowRenderFailure, () => properties.FindControl<TextBlock>("ErrorText")!.Text),
                ("Browser", browser.ShowRenderFailure, () => browser.FindControl<TextBlock>("ErrorText")!.Text),
                ("Rail editor", rail.ShowRenderFailure, () => rail.FindControl<TextBlock>("ErrorText")!.Text)
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

        DesktopChecks.Check("OpenFailure_RemoveFromRecent_RemovesOnlyFailedPath", () =>
        {
            string root = Path.Combine(FindRepoRoot(), ".tmp-tests", "u1fix-remove-" + Guid.NewGuid().ToString("N"));
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
                string path = Path.Combine(FindRepoRoot(), "src", "CfdWorkbench.Desktop", "Assets", "example.foil");
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
                    if (!titles.SequenceEqual(["Plan + 3D", "Section sample", "Foil source", "Section"]))
                        throw new InvalidOperationException("Model-area Dock tabs are " + string.Join(", ", titles));
                    var modelTab = docTabs[0];
                    var sourceTab = docTabs[2];

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
                    if (host.RailEditor.FindControl<ListBox>("ControlList") is null ||
                        host.RailEditor.FindControl<TextBox>("NumericInput") is null)
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
