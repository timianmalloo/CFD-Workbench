using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The four Windows defects of W-1 (docs/reviews/pr-3.md), proved on the Mac by forcing the non-macOS branch:
/// <c>new MainWindow(null, macOS: false)</c> builds the shell a Windows user gets (ADR-0009 S1: the one menu table drawn
/// in the window, Ctrl for the command key). Ring: the Desktop fast ring through the <c>--shell-window</c> mode.
/// Cost: about 8 windows.
/// </summary>
public static class WindowsShellTests
{
    public static void Run()
    {
        DesktopChecks.Check("WindowsShell_Gesture_CommandKeyMapsToControlOffMac", () =>
        {
            var windows = NativeMenuBuilder.ParseGesture("⌘Z", macOS: false);
            var mac = NativeMenuBuilder.ParseGesture("⌘Z", macOS: true);
            if (windows?.Key != Key.Z || windows.KeyModifiers != KeyModifiers.Control)
                throw new InvalidOperationException($"⌘Z off macOS parsed as {windows}");
            if (mac?.Key != Key.Z || mac.KeyModifiers != KeyModifiers.Meta)
                throw new InvalidOperationException($"⌘Z on macOS parsed as {mac}");
        });

        DesktopChecks.Check("WindowsShell_MenuBar_InWindowOffMac_AbsentOnMac", () =>
        {
            var windows = new MainWindow(null, macOS: false);
            var mac = new MainWindow(null, macOS: true);
            try
            {
                windows.Show(); mac.Show();
                Settle(windows); Settle(mac);
                var winHost = (ShellHost)windows.Content!;
                var macHost = (ShellHost)mac.Content!;
                if (winHost.MenuBar is null || !winHost.GetVisualDescendants().Contains(winHost.MenuBar))
                    throw new InvalidOperationException("The Windows shell has no in-window menu bar");
                // The bar sits in the top row, above the dock. Its items are drawn only where the platform does not export the
                // menu, so on this Mac the bar has no height and its rows are the PC's to see (docs/proof/wfx/pc-reverify.md).
                if (Grid.GetRow(winHost.MenuBar) != 0 || Grid.GetRow(winHost.DockHost) < 1)
                    throw new InvalidOperationException($"Menu bar row {Grid.GetRow(winHost.MenuBar)}, dock row {Grid.GetRow(winHost.DockHost)}");
                if (NativeMenu.GetMenu(windows)?.Items.OfType<NativeMenuItem>().Select(item => item.Header?.ToString()).SequenceEqual(
                        ["File", "Edit", "View", CommandTable.SectionMenu, "Window"]) != true)
                    throw new InvalidOperationException("The bar has no table menu to draw");
                if (macHost.MenuBar is not null || macHost.GetVisualDescendants().OfType<NativeMenuBar>().Any())
                    throw new InvalidOperationException("macOS has an in-window menu bar beside the system menu");
                if (mac.KeyBindings.Count != 2 || mac.KeyBindings.Any(binding => binding.Gesture?.Key != Key.F6))
                    throw new InvalidOperationException("macOS window carries key bindings beyond F6: " +
                        string.Join(", ", mac.KeyBindings.Select(binding => binding.Gesture)));
            }
            finally { windows.Close(); mac.Close(); }
        });

        DesktopChecks.Check("WindowsShell_CtrlZ_UndoesExactlyOneStep", () =>
        {
            var window = new MainWindow(null, macOS: false);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
                controller.ApplySpan("900");
                controller.ApplySpan("950");
                host.RefreshPanes();
                var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                canvas.Focus();
                Settle(window);
                Press(window, Key.Z, KeyModifiers.Control);
                Settle(window);
                if (!controller.CanUndo || !controller.CanRedo)
                    throw new InvalidOperationException($"After one Ctrl+Z: can undo {controller.CanUndo}, can redo {controller.CanRedo} (none = no fire, redo only = double fire)");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WindowsShell_EveryTableGesture_FiresItsCommandOnce", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                // The shell wiring MainWindow does, with a probe on each item's command before the keys are bound.
                var menu = NativeMenuBuilder.BuildMenu(window, onAction: _ => { }, macOS: false);
                var fired = new List<string>();
                var items = Flatten(menu).Where(item => item.Gesture is not null).ToList();
                if (items.Count == 0) throw new InvalidOperationException("The table exported no gestures");
                foreach (var item in items)
                {
                    var inner = item.Command;
                    string name = item.Header?.ToString() ?? "?";
                    item.Command = new Probe(inner, () => fired.Add(name));
                }
                NativeMenuBuilder.ShowInWindow(window, host, menu);
                window.Show();
                Settle(window);
                host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!.Focus();
                var problems = new List<string>();
                foreach (var item in items)
                {
                    fired.Clear();
                    // A disabled command (Finish section outside a section) is refused by the binding, as by the menu item.
                    // One key runs one command: the first enabled item that has the gesture.
                    string name = item.Header?.ToString() ?? "?";
                    int expected = items.Any(other => other.Gesture!.Equals(item.Gesture) && other.Command!.CanExecute(null)) ? 1 : 0;
                    Press(window, item.Gesture!.Key, item.Gesture.KeyModifiers);
                    if (fired.Count != expected)
                        problems.Add($"{name} [{item.Gesture}]: fired {fired.Count} ({string.Join("|", fired)}), expected {expected}");
                }
                if (problems.Count > 0) throw new InvalidOperationException(string.Join("; ", problems));
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WindowsShell_MenuReachableFromKeyboard_AltAndF10", () =>
        {
            var window = new MainWindow(null, macOS: false);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                // Avalonia's access-key handler opens the window's main Menu on Alt and F10, and the bar draws one (a Menu). That
                // the Menu is in the window is all a Mac can show: the key press is raw input a test cannot inject, and the bar
                // draws nothing where the platform exports the menu. The PC presses Alt and F10 (docs/proof/wfx/pc-reverify.md).
                var menu = host.MenuBar!.GetVisualDescendants().OfType<Menu>().FirstOrDefault()
                    ?? throw new InvalidOperationException("The bar holds no Menu");
                if (!menu.IsAttachedToVisualTree() || menu.GetVisualRoot() != window)
                    throw new InvalidOperationException("The bar's Menu is not in the window: Alt and F10 would not reach it");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WindowsShell_SaveRefused_ShowsMessageInStatusStrip", () =>
        {
            var window = new MainWindow(null, macOS: false);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                Task.Run(() => host.Controller.OpenExampleAsync()).GetAwaiter().GetResult();
                host.RefreshPanes();
                Settle(window);
                // A save that THROWS (a wrong file type here) was written to stderr only. It is now rethrown for the log and shown.
                var thrown = Save(window, "foil.txt");
                Settle(window);
                string shown = StatusStripTests.Text(host).Text ?? "";
                if (thrown?.Code != "DOC-TYPE" || !shown.StartsWith("DOC-TYPE:", StringComparison.Ordinal) || StatusStripTests.Kind(host) != "error")
                    throw new InvalidOperationException($"A thrown save showed '{shown}' (kind {StatusStripTests.Kind(host)}); threw {thrown?.Code}");
                // A relative path is what Windows hands the POSIX-only store (a drive path does not start with '/'; Supported()
                // also needs macOS): the store returns DOC-UNSUPPORTED-PERSISTENCE and the controller's own status reaches the strip.
                var returned = Save(window, "foil.cfdw.json");
                Settle(window);
                shown = StatusStripTests.Text(host).Text ?? "";
                if (returned is not null || !shown.StartsWith("DOC-UNSUPPORTED-PERSISTENCE:", StringComparison.Ordinal))
                    throw new InvalidOperationException($"An unsupported-persistence save showed '{shown}'; threw {returned?.Code}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("WindowsShell_SaveFailureText_NamesTheCodeAndTheRecovery", () =>
        {
            string unsupported = MainWindow.SaveFailureText(new ContractError("DOC-UNSUPPORTED-PERSISTENCE"));
            string io = MainWindow.SaveFailureText(new IOException("disk"));
            if (!unsupported.StartsWith("DOC-UNSUPPORTED-PERSISTENCE:", StringComparison.Ordinal) || !io.StartsWith("DOC-IO:", StringComparison.Ordinal) ||
                !unsupported.Contains("Resolve the refusal before retry", StringComparison.Ordinal))
                throw new InvalidOperationException($"'{unsupported}' / '{io}'");
        });

        DesktopChecks.Check("PlanCanvas_RetainedPointPeer_NameFollowsTheDrag_AndRaisesNameChanged", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenFoilAsync(DesktopChecks.TenPointFoil(), "New foil 10")).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                var peer = ControlAutomationPeer.CreatePeerForElement(canvas)!;
                var before = controller.Planform!.Trailing.Points[3];
                var retained = peer.GetChildren()!.First(child => child.GetName().Contains("Trailing edge, point 4 of", StringComparison.Ordinal));
                string oldName = retained.GetName();
                int raised = 0;
                retained.PropertyChanged += (_, args) =>
                {
                    if (args.Property == AutomationElementIdentifiers.NameProperty) raised++;
                };
                if (!controller.BeginGesture(new PointRef("trailing", before.Id), GestureInput.Pointer))
                    throw new InvalidOperationException("Could not begin a drag");
                controller.UpdateGesture(before.SpanMeters, before.Ordinate + 0.004);
                controller.FlushGestureFrame();
                Await(controller.EndGestureAsync(GestureEnd.Release));
                Settle(window);
                var after = controller.Planform!.Trailing.Points[3];
                string expectedAft = $"aft {after.Ordinate * 1000:F2} mm";
                if (Math.Abs(after.Ordinate - before.Ordinate) < 0.003)
                    throw new InvalidOperationException("The drag did not move the point");
                string name = retained.GetName();
                if (name == oldName || !name.Contains(expectedAft, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Retained peer reads '{name}', expected '{expectedAft}' (was '{oldName}')");
                if (raised == 0) throw new InvalidOperationException("The retained peer raised no name-changed event");
            }
            finally { window.Close(); }
        });
    }

    /// <summary>Runs a save to completion on the UI thread's dispatcher; returns the refusal it threw, or null.</summary>
    private static ContractError? Save(MainWindow window, string destination)
    {
        var save = window.SaveToAsync(destination);
        Await(save, throwOnFault: false);
        if (save.Exception?.InnerException is ContractError error) return error;
        if (save.Exception is not null) throw save.Exception;
        return null;
    }

    /// <summary>Waits for a task while the UI thread keeps running its jobs: a blocking wait would deadlock a UI continuation.</summary>
    private static void Await(Task task, bool throwOnFault = true)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!task.IsCompleted && DateTime.UtcNow < deadline) Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        if (!task.IsCompleted) throw new InvalidOperationException("The task did not finish in 10 s");
        if (throwOnFault) task.GetAwaiter().GetResult();
    }

    private static IEnumerable<NativeMenuItem> Flatten(NativeMenu? level) =>
        level?.Items.OfType<NativeMenuItem>().SelectMany(item => Flatten(item.Menu).Prepend(item)) ?? [];

    /// <summary>
    /// One key press as the platform delivers it: raw input to the window, which is where Avalonia matches key bindings
    /// (walking from the focused element up to the window). Raising KeyDown by hand skips that step and proves nothing.
    /// </summary>
    private static void Press(Window window, Key key, KeyModifiers modifiers)
    {
        var focused = window.FocusManager?.GetFocusedElement() as Visual ?? window;
        var down = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers, Source = focused };
        // KeyboardDevice.ProcessRawEvent: from the focused element up to the window, the first matching binding handles the key.
        for (Visual? at = focused; at is not null && !down.Handled; at = at.GetVisualParent())
            if (at is InputElement element)
                foreach (var binding in element.KeyBindings)
                {
                    binding.TryHandle(down);
                    if (down.Handled) break;
                }
        if (!down.Handled) (focused as Interactive)?.RaiseEvent(down);
    }

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    /// <summary>Counts a menu item's command without changing what it does.</summary>
    private sealed class Probe(ICommand? inner, Action counted) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { if (inner is not null) inner.CanExecuteChanged += value; } remove { if (inner is not null) inner.CanExecuteChanged -= value; } }
        public bool CanExecute(object? parameter) => inner?.CanExecute(parameter) ?? true;
        public void Execute(object? parameter) { counted(); inner?.Execute(parameter); }
    }
}
