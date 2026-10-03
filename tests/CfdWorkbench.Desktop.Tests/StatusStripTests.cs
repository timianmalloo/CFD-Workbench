using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The status strip and the warning toast (DR-STATUS-1..4; docs/reviews/ui-status-bar.md §2.5). The checks find the parts by
/// name, so each one ran red on the tree before the strip existed.
/// </summary>
public static class StatusStripTests
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Run()
    {
        Pane("StatusStrip_SitsAtWindowBottom_FullWidth_24px", (controller, host, window) =>
        {
            var strip = Strip(host);
            var origin = strip.TranslatePoint(default, window) ?? throw new InvalidOperationException("strip not in the window");
            double bottom = origin.Y + strip.Bounds.Height;
            var dock = host.DockHost.TranslatePoint(new Point(0, host.DockHost.Bounds.Height), window)!.Value;
            Console.WriteLine(FormattableString.Invariant($"MEASURE strip y {origin.Y:0.#} h {strip.Bounds.Height:0.#} w {strip.Bounds.Width:0.#} in {window.ClientSize.Width:0.#}x{window.ClientSize.Height:0.#}"));
            if (Math.Abs(bottom - window.ClientSize.Height) > 0.5 || Math.Abs(origin.X) > 0.5 ||
                Math.Abs(strip.Bounds.Width - window.ClientSize.Width) > 0.5 || strip.Bounds.Height < 23.5 || origin.Y < dock.Y - 0.5)
                throw new InvalidOperationException($"strip at y {origin.Y}, h {strip.Bounds.Height}, w {strip.Bounds.Width}; dock bottom {dock.Y}");
        });

        Pane("ModelArea_HasNoTopStatusLine", (controller, host, window) =>
        {
            var left = host.ModelView.GetVisualDescendants().OfType<Control>()
                .Where(control => control.Name is "StatusText" or "StatusTryAgainButton").Select(control => control.Name).ToList();
            if (left.Count > 0 || host.ModelView.FindControl<Control>("StatusText") is not null)
                throw new InvalidOperationException("the model area still has a status line: " + string.Join(", ", left));
            Strip(host);
        });

        Pane("StatusStrip_IsTheOnlyPoliteStatusRegion", (controller, host, window) =>
        {
            var strip = Strip(host);
            var lines = window.GetVisualDescendants().OfType<TextBlock>().Where(block => block.Name == "StatusText").ToList();
            if (lines.Count != 1 || !strip.IsVisualAncestorOf(lines[0]) ||
                AutomationProperties.GetLiveSetting(lines[0]) != AutomationLiveSetting.Polite)
                throw new InvalidOperationException($"{lines.Count} StatusText line(s); in strip {lines.Count == 1 && strip.IsVisualAncestorOf(lines[0])}");
            if (Toast(host) is { } toast && AutomationProperties.GetLiveSetting(toast) != AutomationLiveSetting.Off)
                throw new InvalidOperationException("the toast is a live region");
        });

        Pane("StatusStrip_ChordAboveLimit_WarningInStrip_ToastOpens_RowKeepsRailOnly", (controller, host, window) =>
        {
            CommitRootChord(controller, host, window, 10.1);
            var failures = new List<string>();
            string text = Text(host).Text ?? "";
            if (!text.Contains("above the limit", StringComparison.Ordinal) || Kind(host) != "warning")
                failures.Add($"strip '{text}' kind {Kind(host)}");
            var toast = NeedToast(host);
            if (!toast.IsEffectivelyVisible || Need<TextBlock>(host.ModelView, "ToastText").Text != text)
                failures.Add($"toast visible {toast.IsEffectivelyVisible} text '{Need<TextBlock>(host.ModelView, "ToastText").Text}'");
            if (Need<TextBlock>(host.Properties, "ChordWarningText").IsEffectivelyVisible)
                failures.Add("the row still shows the warning text");
            if (!Need<Border>(host.Properties, "Row_w_root").Classes.Contains("warning"))
                failures.Add("the row lost its warning rail");
            var icon = Need<Control>(host.Properties, "StateIcon_w_root");
            if (!icon.IsEffectivelyVisible || ToolTip.GetTip(icon) as string != text)
                failures.Add($"state icon visible {icon.IsEffectivelyVisible} tip '{ToolTip.GetTip(icon)}'");
            if (AutomationProperties.GetHelpText(Need<TextBox>(host.Properties, "RootChordInput"))?.Contains(text, StringComparison.Ordinal) != true)
                failures.Add("the field's help text lacks the report");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("Toast_Hold_ClosesAfterHold_PausedWhileHovered", (controller, host, window) =>
        {
            SetHold(host, TimeSpan.FromMilliseconds(150));
            CommitRootChord(controller, host, window, 10.1);
            var toast = NeedToast(host);
            if (!toast.IsVisible) throw new InvalidOperationException("no toast after the warning");
            Wait(TimeSpan.FromMilliseconds(600));
            if (toast.IsVisible) throw new InvalidOperationException("the toast outlived its hold");
            CommitRootChord(controller, host, window, 9.5);
            if (!toast.IsVisible) throw new InvalidOperationException("no toast after the second warning");
            Hover(toast, entered: true);
            Wait(TimeSpan.FromMilliseconds(600));
            if (!toast.IsVisible) throw new InvalidOperationException("the hold ran while the pointer was over the toast");
            Hover(toast, entered: false);
            Wait(TimeSpan.FromMilliseconds(600));
            if (toast.IsVisible) throw new InvalidOperationException("the hold did not restart when the pointer left");
        });

        Pane("Toast_EscInside_ClosesAndReturnsFocus", (controller, host, window) =>
        {
            CommitRootChord(controller, host, window, 10.1);
            var input = Need<TextBox>(host.Properties, "RootChordInput");
            var toast = NeedToast(host);
            // Shift+F6 from the field: the toast is the last stop of the ring, after the model area.
            host.MoveFocus(reverse: true);
            Settle(window);
            if (!toast.IsKeyboardFocusWithin)
                throw new InvalidOperationException("F6 did not reach the toast: " + (window.FocusManager!.GetFocusedElement() as Control)?.Name);
            var focused = (Control)window.FocusManager!.GetFocusedElement()!;
            Key(focused, Avalonia.Input.Key.Escape);
            Settle(window);
            if (toast.IsVisible || !input.IsFocused)
                throw new InvalidOperationException($"toast visible {toast.IsVisible}; focus on {(window.FocusManager!.GetFocusedElement() as Control)?.Name}");
        });

        Pane("Toast_OpensWithoutTakingFocus", (controller, host, window) =>
        {
            CommitRootChord(controller, host, window, 10.1);
            var input = Need<TextBox>(host.Properties, "RootChordInput");
            if (!NeedToast(host).IsVisible || !input.IsFocused)
                throw new InvalidOperationException($"toast {NeedToast(host).IsVisible}; focus on {(window.FocusManager!.GetFocusedElement() as Control)?.Name}");
        });

        Pane("Toast_NewerWarning_ReplacesText_InfoDoesNotClose", (controller, host, window) =>
        {
            CommitRootChord(controller, host, window, 10.1);
            var toast = NeedToast(host);
            string first = Need<TextBlock>(host.ModelView, "ToastText").Text ?? "";
            CommitRootChord(controller, host, window, 9.5);
            string second = Need<TextBlock>(host.ModelView, "ToastText").Text ?? "";
            int toasts = window.GetVisualDescendants().OfType<Border>().Count(border => border.Name == "WarningToast" && border.IsVisible);
            if (!toast.IsVisible || second == first || second != Text(host).Text || toasts != 1)
                throw new InvalidOperationException($"toasts {toasts}; first '{first}' second '{second}'");
            host.RunCommand("view.comb").GetAwaiter().GetResult();   // an info report
            Settle(window);
            if (!toast.IsVisible || Need<TextBlock>(host.ModelView, "ToastText").Text != second || Kind(host) != "info")
                throw new InvalidOperationException($"after an info report: toast {toast.IsVisible}, strip kind {Kind(host)}");
        });

        Pane("StatusStrip_LongReport_EllipsizesWithFullTextInNameAndTooltip", (controller, host, window) =>
        {
            window.Width = 800;
            Settle(window);
            CommitRootChord(controller, host, window, 10.1);
            var line = Text(host);
            string text = line.Text ?? "";
            var full = new TextBlock { Text = text, FontSize = line.FontSize, FontFamily = line.FontFamily };
            full.Measure(Size.Infinity);
            var strip = Strip(host);
            Console.WriteLine(FormattableString.Invariant($"MEASURE long report {full.DesiredSize.Width:0} px in {line.Bounds.Width:0} px, strip h {strip.Bounds.Height:0.#}"));
            if (full.DesiredSize.Width <= line.Bounds.Width + 0.5)
                throw new InvalidOperationException("the report fits; the check needs a longer one");
            if (line.TextTrimming != TextTrimming.CharacterEllipsis || line.TextWrapping != TextWrapping.NoWrap || strip.Bounds.Height > 24.5)
                throw new InvalidOperationException($"trimming {line.TextTrimming}, wrapping {line.TextWrapping}, strip h {strip.Bounds.Height}");
            if (ToolTip.GetTip(line) as string != text || AutomationProperties.GetName(line) != text)
                throw new InvalidOperationException($"tooltip '{ToolTip.GetTip(line)}' name '{AutomationProperties.GetName(line)}'");
        });

        DesktopChecks.Check("StatusStrip_RecentNotCleared_TryAgainInStrip", () =>
        {
            string root = Scratch("strip-clear-" + Guid.NewGuid().ToString("N"));
            try
            {
                // A recent list written by a newer version (COPY-147) is not cleared, and Try again sits after the message.
                Directory.CreateDirectory(Path.Combine(root, "recent"));
                File.WriteAllBytes(Path.Combine(root, "recent", "recent.json"),
                    System.Text.Encoding.UTF8.GetBytes("{\"format\":\"cfdw-recent\",\"version\":2,\"entries\":[]}"));
                using var controller = new WorkbenchController();
                var host = new ShellHost(controller, new PreferenceStore(root, () => new ProjectStore()));
                var window = new Window { Content = host, Width = 1280, Height = 800 };
                try
                {
                    window.Show();
                    Settle(window);
                    Pump(host.ClearRecentAsync());
                    Settle(window);
                    var strip = Strip(host);
                    var retry = Need<Button>(strip, "StatusTryAgainButton");
                    string text = Text(host).Text ?? "";
                    if (!text.StartsWith("The recent-files list wasn't cleared: ", StringComparison.Ordinal) || Kind(host) != "error" ||
                        !retry.IsEffectivelyVisible || !strip.IsVisualAncestorOf(retry) || retry.Bounds.Height < 23.5)
                        throw new InvalidOperationException($"strip '{text}' kind {Kind(host)} retry {retry.IsEffectivelyVisible} h {retry.Bounds.Height}");
                    // "right after the message": the action follows the text, not the far end of the strip.
                    double textRight = Text(host).TranslatePoint(new Point(Text(host).Bounds.Width, 0), strip)!.Value.X;
                    double retryLeft = retry.TranslatePoint(default, strip)!.Value.X;
                    if (retryLeft - textRight > 12 || retryLeft < textRight - 0.5)
                        throw new InvalidOperationException($"Try again at x {retryLeft:0}, the message ends at {textRight:0}");
                }
                finally { window.Close(); }
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        });

        Capture();
        // Track PNL's window checks (the section strip, Show, the Points pane, Properties' section rows, workspaces).
        PointsPaneTests.Run();
    }

    /// <summary>
    /// Review captures of the whole window (CFDW_STATUS_CAPTURE=&lt;dir&gt;): a type change and a fit warning, light and dark,
    /// to set beside docs/mockups/status-bar.html V2. Off by default; never part of the gate.
    /// </summary>
    public static void Capture()
    {
        if (Environment.GetEnvironmentVariable("CFDW_STATUS_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        foreach (var (variant, theme) in new[] { (Avalonia.Styling.ThemeVariant.Light, "light"), (Avalonia.Styling.ThemeVariant.Dark, "dark") })
            Pane("Capture_StatusStrip_" + theme, (controller, host, window) =>
            {
                window.RequestedThemeVariant = variant;
                Select(controller, window, Control(controller, "trailing"));
                var type = Need<ComboBox>(host.Properties, "TypeControl");
                type.Focus();
                Key(type, Avalonia.Input.Key.Down);
                Settle(window);
                if (type.IsDropDownOpen) Key(type, Avalonia.Input.Key.Escape);
                Key(type, Avalonia.Input.Key.Enter);
                WaitIdle(controller, window);
                Save(window, Path.Combine(directory, $"strip-type-{theme}.png"));
                controller.Undo();
                Settle(window);
                CommitRootChord(controller, host, window, 10.1);
                Save(window, Path.Combine(directory, $"strip-chord-{theme}.png"));
            });
    }

    private static void Save(Window window, string path)
    {
        Settle(window);
        var size = new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height);
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
        bitmap.Render(window);
        bitmap.Save(path);
    }

    // ---------------- helpers (by name, so they compile and fail on a tree without the strip) ----------------

    internal static Control Strip(Control host) =>
        host.GetVisualDescendants().OfType<Control>().FirstOrDefault(control => control.Name == "StatusStrip")
        ?? throw new InvalidOperationException("missing StatusStrip");

    /// <summary>The strip's polite status line.</summary>
    internal static TextBlock Text(Control host) =>
        Strip(host).FindControl<TextBlock>("StatusText")
        ?? Strip(host).GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(block => block.Name == "StatusText")
        ?? throw new InvalidOperationException("the strip has no StatusText");

    /// <summary>The kind the strip draws: "warning", "error" or "info".</summary>
    internal static string Kind(Control host)
    {
        var classes = Text(host).Classes;
        return classes.Contains("error") ? "error" : classes.Contains("warning") ? "warning" : "info";
    }

    private static Border? Toast(ShellHost host) => host.ModelView.FindControl<Border>("WarningToast");

    internal static Border NeedToast(ShellHost host) => Toast(host) ?? throw new InvalidOperationException("missing WarningToast");

    private static void SetHold(ShellHost host, TimeSpan hold) =>
        (typeof(ModelArea).GetProperty("ToastHold") ?? throw new InvalidOperationException("ModelArea has no ToastHold"))
            .SetValue(host.ModelView, hold);

    /// <summary>Types a root chord <paramref name="factor"/> × the current one and commits it: the fit lands above the limit.</summary>
    internal static void CommitRootChord(WorkbenchController controller, ShellHost host, Window window, double factor)
    {
        var input = Need<TextBox>(host.Properties, "RootChordInput");
        string before = controller.AcceptedSource;
        input.Focus();
        input.Text = (controller.Estimates!.RootChordMeters * 1000 * factor).ToString("0.##", Inv);
        Key(input, Avalonia.Input.Key.Enter);
        WaitIdle(controller, window);
        if (controller.AcceptedSource == before) throw new InvalidOperationException("the root chord was not committed: " + controller.Status);
    }

    private static void Hover(Control target, bool entered)
    {
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        var root = (Visual)TopLevel.GetTopLevel(target)!;
        target.RaiseEvent(new PointerEventArgs(entered ? InputElement.PointerEnteredEvent : InputElement.PointerExitedEvent,
            target, pointer, root, default, 0, new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None));
    }

    /// <summary>Runs the dispatcher's own loop for a while, so its timers fire.</summary>
    private static void Wait(TimeSpan time)
    {
        using var timeout = new CancellationTokenSource(time);
        Avalonia.Threading.Dispatcher.UIThread.MainLoop(timeout.Token);
    }

    private static string Scratch(string name)
    {
        string temp = Path.GetTempPath();
        if (temp.StartsWith("/tmp/", StringComparison.Ordinal) || temp.StartsWith("/var/", StringComparison.Ordinal)) temp = "/private" + temp;
        return Path.Combine(temp, name);
    }
}
