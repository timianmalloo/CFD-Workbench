using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// "Save is blocked" with Clear unfinished save (docs/design/windows-store-recovery.md section 4 states A1 to A6, section 7 node A; Rulings 146 (2),
/// 160, 259). The dialog checks drive the dialog with a scripted clear; the MainWindow checks run the shipped save and Clear against a real folder.
/// The strings here are the oracle copy: COPY-441, 444, 445, 446, 447, 448, 449, 450, 455 and DS-COPY-5.
/// </summary>
public static class SaveBlockedDialogTests
{
    private const string ConfirmBody = "This removes the files the unfinished save left in this folder (.cfd-writer.claim and any .cfd-….tmp). Your saved file and your current work are not touched. It works only if no other CFD Workbench window is using this project.";
    private const string InUseText = "Can't clear it yet: another CFD Workbench window is still using this project. Close that window, then try again. Your saved file and your current work are unchanged.";

    public static void Run()
    {
        DesktopChecks.Check("SaveBlockedDialog_ClearButton_SwapsToConfirm_FocusAndEnterOnCancel", ClearSwapsToConfirm);
        DesktopChecks.Check("SaveBlockedDialog_Step3AndStaysBlocked_Ruling259Text", Step3AndStaysBlocked);
        DesktopChecks.Check("SaveBlockedDialog_ClearInUse_ShowsCopy449_TryAgain", ClearInUse);
        DesktopChecks.Check("SaveBlockedDialog_ClearFailed_ShowsCopy455WithCode", ClearFailed);
        DesktopChecks.Check("SaveBlockedDialog_Clearing_FocusInsideDialog_ThenCancelOnRefusal", ClearingFocus);
        DesktopChecks.Check("SaveBlockedDialog_EscapeOrCancelFromConfirm_FocusReturnsToClear", CancelReturnsToClear);
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP MainWindow_ClearThenSave_NoRecoveryWording_DocumentSaved windows-only");
            Console.WriteLine("SKIP MainWindow_Cleared_RetriesSaveExactlyOnce_SameDestinationAndHash windows-only");
            return;
        }
        DesktopChecks.Check("MainWindow_ClearThenSave_NoRecoveryWording_DocumentSaved", ClearThenSave);
        DesktopChecks.Check("MainWindow_Cleared_RetriesSaveExactlyOnce_SameDestinationAndHash", ClearedRetriesOnce);
    }

    private static ClearResult Cleared => new(ClearOutcome.Cleared, "OK", 2);

    private static SaveBlockedDialog Shown(Func<Task<ClearResult>> clear)
    {
        var dialog = new SaveBlockedDialog(clear);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        return dialog;
    }

    private static void Pump(Func<bool> until, string what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!until())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException(what);
            Dispatcher.UIThread.RunJobs(); Thread.Sleep(5);
        }
    }

    private static void Await(Task task) { Pump(() => task.IsCompleted, "task did not complete"); task.GetAwaiter().GetResult(); }
    private static StackPanel Panel(Window dialog) => (StackPanel)dialog.Content!;
    private static Button[] Buttons(Window dialog) => Panel(dialog).Children.OfType<StackPanel>().Last().Children.OfType<Button>().ToArray();
    private static string[] Labels(Window dialog) => Buttons(dialog).Select(button => (string)button.Content!).ToArray();
    private static string[] Texts(Window dialog) => Panel(dialog).Children.OfType<TextBlock>().Select(text => text.Text ?? "").ToArray();
    private static Button Find(Window dialog, string label) => Buttons(dialog).Single(button => (string?)button.Content == label);
    private static void Click(Button button) { button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
    private static object? Focused(Window dialog) => dialog.FocusManager?.GetFocusedElement();
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void ClearSwapsToConfirm()
    {
        var dialog = Shown(() => Task.FromResult(Cleared));
        try
        {
            Need(dialog.Title == "Save is blocked", $"title '{dialog.Title}'");
            Need(Labels(dialog).SequenceEqual(["Save As…", "Clear unfinished save…", "Cancel"]), "S3 buttons: " + string.Join(" | ", Labels(dialog)));
            Need(Find(dialog, "Save As…").IsDefault && Focused(dialog) == Find(dialog, "Save As…"), "Save As… is not the default with first focus on S3");
            Click(Find(dialog, "Clear unfinished save…"));
            Need(dialog.Title == "Clear the unfinished save?", $"S4 title '{dialog.Title}'");
            Need(Texts(dialog).Contains(ConfirmBody), "S4 body: " + string.Join(" | ", Texts(dialog)));
            Need(Labels(dialog).SequenceEqual(["Clear unfinished save", "Cancel"]), "S4 buttons: " + string.Join(" | ", Labels(dialog)));
            var confirm = Find(dialog, "Clear unfinished save"); var cancel = Find(dialog, "Cancel");
            Need(cancel.IsDefault && !confirm.IsDefault && cancel.IsCancel, "Enter must act on Cancel in S4");
            Need(Focused(dialog) == cancel, "first focus in S4 is not Cancel");
            Need(!dialog.SaveAs && !dialog.Cleared, "the confirm swap answered the dialog");
        }
        finally { dialog.Close(); }
    }

    private static void Step3AndStaysBlocked()
    {
        var dialog = Shown(() => Task.FromResult(Cleared));
        try
        {
            string[] steps = Panel(dialog).Children.OfType<StackPanel>().First().Children.OfType<TextBlock>().Select(text => text.Text ?? "").ToArray();
            Need(steps.SequenceEqual(["1. Close any other CFD Workbench window using this project.", "2. Check the project shows your last saved work.",
                "3. Use Save As to keep your current work in a different folder, or choose Clear unfinished save, then Save."]), "steps: " + string.Join(" | ", steps));
            Need(Texts(dialog).Contains("This folder stays blocked until the unfinished save is cleared."), "stays-blocked sentence: " + string.Join(" | ", Texts(dialog)));
            Need(Texts(dialog)[0] == "An earlier save of this project didn't finish (the app may have closed unexpectedly). Your saved file is unchanged.", "lead");
        }
        finally { dialog.Close(); }
    }

    private static void ClearInUse()
    {
        int calls = 0;
        var dialog = Shown(() => Task.FromResult(++calls == 1 ? new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0) : Cleared));
        try
        {
            Click(Find(dialog, "Clear unfinished save…")); Click(Find(dialog, "Clear unfinished save"));
            Pump(() => Labels(dialog).Contains("Try again"), "the in-use state did not appear");
            Need(Texts(dialog).Contains(InUseText), "COPY-449: " + string.Join(" | ", Texts(dialog)));
            Need(Labels(dialog).SequenceEqual(["Try again", "Cancel"]), "A4 buttons: " + string.Join(" | ", Labels(dialog)));
            Need(dialog.IsVisible && !dialog.Cleared, "in use was shown as success");
            Click(Find(dialog, "Try again"));
            Pump(() => dialog.Cleared, "Try again did not clear");
            Need(calls == 2 && !dialog.SaveAs, $"Try again ran {calls} clears");
        }
        finally { dialog.Close(); }
    }

    private static void ClearFailed()
    {
        var dialog = Shown(() => Task.FromResult(new ClearResult(ClearOutcome.Failed, "DOC-CONFLICT", 0)));
        try
        {
            Click(Find(dialog, "Clear unfinished save…")); Click(Find(dialog, "Clear unfinished save"));
            Pump(() => Labels(dialog).Contains("Try again"), "the failed state did not appear");
            Need(Texts(dialog).Contains("Couldn't clear the unfinished save (DOC-CONFLICT). Your saved file and your current work are unchanged."), "COPY-455: " + string.Join(" | ", Texts(dialog)));
            Need(Labels(dialog).SequenceEqual(["Try again", "Cancel"]) && !dialog.Cleared, "A5 buttons: " + string.Join(" | ", Labels(dialog)));
        }
        finally { dialog.Close(); }
    }

    // U1: while busy the focus is the dialog's heading, never a disabled button or the window behind; on A4 and A5 it is Cancel.
    private static void ClearingFocus()
    {
        var gate = new TaskCompletionSource<ClearResult>();
        var dialog = Shown(() => gate.Task);
        try
        {
            Click(Find(dialog, "Clear unfinished save…")); Click(Find(dialog, "Clear unfinished save"));
            Need(Buttons(dialog).All(button => !button.IsEnabled), "the buttons stay enabled while clearing");
            Need(Focused(dialog) is TextBlock heading && heading.Text == "Clear the unfinished save?" && ReferenceEquals(heading.GetVisualRoot(), dialog),
                "while clearing, focus is not the dialog heading: " + Focused(dialog));
            gate.SetResult(new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0));
            Pump(() => Labels(dialog).Contains("Try again"), "the in-use state did not appear");
            Need(Focused(dialog) == Find(dialog, "Cancel") && Find(dialog, "Cancel").IsEnabled, "after in use, focus is not Cancel");
        }
        finally { dialog.Close(); }
        var failing = new TaskCompletionSource<ClearResult>();
        dialog = Shown(() => failing.Task);
        try
        {
            Click(Find(dialog, "Clear unfinished save…")); Click(Find(dialog, "Clear unfinished save"));
            failing.SetResult(new ClearResult(ClearOutcome.Failed, "DOC-IO", 0));
            Pump(() => Labels(dialog).Contains("Try again"), "the failed state did not appear");
            Need(Focused(dialog) == Find(dialog, "Cancel"), "after a failure, focus is not Cancel");
        }
        finally { dialog.Close(); }
    }

    // U2: Cancel (Escape is the IsCancel button) from A2, A4 and A5 returns to S3 with focus on the control that opened S4.
    private static void CancelReturnsToClear()
    {
        foreach (string state in new[] { "confirm", "in-use", "failed" })
        {
            var dialog = Shown(() => Task.FromResult(state == "in-use" ? new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0) : new ClearResult(ClearOutcome.Failed, "DOC-IO", 0)));
            try
            {
                Click(Find(dialog, "Clear unfinished save…"));
                if (state != "confirm") { Click(Find(dialog, "Clear unfinished save")); Pump(() => Labels(dialog).Contains("Try again"), state + " did not appear"); }
                Click(Find(dialog, "Cancel"));
                Need(dialog.IsVisible && dialog.Title == "Save is blocked", $"{state}: Cancel did not return to S3 (title '{dialog.Title}')");
                Need(Labels(dialog).SequenceEqual(["Save As…", "Clear unfinished save…", "Cancel"]), $"{state}: S3 buttons " + string.Join(" | ", Labels(dialog)));
                Need(Focused(dialog) == Find(dialog, "Clear unfinished save…"), $"{state}: focus did not return to Clear unfinished save…");
            }
            finally { dialog.Close(); }
        }
    }

    private static SaveBlockedDialog? Blocked(MainWindow window) => window.OwnedWindows.OfType<SaveBlockedDialog>().FirstOrDefault();

    // A claim and a temp the way a crashed save leaves them: created through the store's own private create, then closed.
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void CrashLeft(string folder)
    {
        using var parent = WindowsNative.OpenFixtureDirectory(folder);
        WindowsNative.CreatePrivate(parent, ProjectStore.ClaimName()).Dispose();
        using var temp = WindowsNative.CreatePrivate(parent, ProjectStore.TempName(Guid.NewGuid().ToString("D")));
        RandomAccess.Write(temp, [1, 2, 3], 0);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ClearThenSave()
    {
        string folder = TestTemp.NewDirectory("clear-then-save-"), path = Path.Combine(folder, "project.cfdw.json");
        var window = new MainWindow(new PreferenceStore(TestTemp.NewDirectory("clear-then-save-prefs-"), () => new ProjectStore()));
        try
        {
            window.Show();
            var host = (ShellHost)window.Content!;
            Await(host.OpenExampleAsync()); Await(window.SaveToAsync(path));
            byte[] first = File.ReadAllBytes(path);
            Await(host.Controller.ApplySpanAsync("1400"));
            Need(host.Controller.IsDirty, "the span edit did not dirty the document");
            CrashLeft(folder);
            var pending = window.SaveToAsync(path);
            Pump(() => Blocked(window) is not null, "the Save is blocked dialog did not appear");
            var dialog = Blocked(window)!; var seen = new List<string>(); // the first state carries DS-COPY-5 ("...until the unfinished save is cleared"), a status of the folder, not an announcement
            Click(Find(dialog, "Clear unfinished save…")); seen.AddRange(Texts(dialog));
            Click(Find(dialog, "Clear unfinished save")); seen.AddRange(Texts(dialog));
            Await(pending);
            Need(!host.Controller.IsDirty, "the document is still dirty after Clear and the retried save");
            Need(!File.ReadAllBytes(path).AsSpan().SequenceEqual(first), "the project file was not rewritten by the retried save");
            Need(Directory.GetFileSystemEntries(folder).Length == 1 && !UnfinishedSave.BlocksFolder(path), "leftovers remain: " + string.Join(",", Directory.GetFileSystemEntries(folder).Select(Path.GetFileName)));
            seen.Add(host.StatusStrip.Text);
            foreach (string word in new[] { "recover", "restor", "cleared" })
                Need(!seen.Any(text => text.Contains(word, StringComparison.OrdinalIgnoreCase)), $"'{word}' appears in: " + string.Join(" | ", seen));
        }
        finally { window.Close(); }
    }

    // U4: after Clear the blocked save is retried exactly once, with the same destination and expected-disk hash; a second block shows the
    // ordinary save status and does not reopen S3 by itself.
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ClearedRetriesOnce()
    {
        string folder = TestTemp.NewDirectory("retry-once-"), path = Path.Combine(folder, "project.cfdw.json");
        var saves = new List<(string Path, string? Hash)>();
        var controller = new WorkbenchController(session => new RecordingStore(new ProjectStore(session), (target, request) => saves.Add((target, request.ExpectedDiskSha256)),
            () => CrashLeft(folder))); // the leftover is created again after Clear, so the retry is blocked again
        var window = new MainWindow(new PreferenceStore(TestTemp.NewDirectory("retry-once-prefs-"), () => new ProjectStore()), null, controller);
        try
        {
            window.Show();
            var host = (ShellHost)window.Content!;
            Await(host.OpenExampleAsync()); Await(window.SaveToAsync(path));
            Await(host.Controller.ApplySpanAsync("1400"));
            CrashLeft(folder);
            int before = saves.Count;
            var pending = window.SaveToAsync(path);
            Pump(() => Blocked(window) is not null, "the Save is blocked dialog did not appear");
            var blockedSave = saves[^1];
            var dialog = Blocked(window)!;
            Click(Find(dialog, "Clear unfinished save…")); Click(Find(dialog, "Clear unfinished save"));
            Await(pending);
            Need(saves.Count == before + 2, $"expected the blocked save and one retry, saw {saves.Count - before} store saves");
            Need(saves[^1] == blockedSave && blockedSave.Path == path && blockedSave.Hash is not null, "the retry changed the destination or the expected-disk hash");
            Need(Blocked(window) is null, "Save is blocked reopened by itself after the retry was blocked again");
            Need(host.StatusStrip.Text == CfdWorkbench.Analysis.Labels.SaveRefusal("DOC-CONFLICT"), "the ordinary save status is not shown: " + host.StatusStrip.Text);
            Need(host.Controller.IsDirty, "the blocked retry cleaned the document");
            var again = window.SaveToAsync(path); // a user Save opens S3 again
            Pump(() => Blocked(window) is not null, "a user Save did not open Save is blocked again");
            Click(Find(Blocked(window)!, "Cancel")); Await(again);
        }
        finally { window.Close(); }
    }

    /// <summary>Forwards to the real store; records each Save and runs an action after each Clear.</summary>
    private sealed class RecordingStore(ProjectStore inner, Action<string, SaveRequest> onSave, Action afterClear) : IProjectStore
    {
        public void Dispose() => inner.Dispose();
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => inner.ReadAsync(path, cancellation);
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
        {
            onSave(path, request);
            return inner.SaveAsync(path, request, cancellation);
        }
        public async Task<ClearResult> ClearUnfinishedSaveAsync(string path, CancellationToken cancellation = default)
        {
            var result = await inner.ClearUnfinishedSaveAsync(path, cancellation);
            afterClear();
            return result;
        }
    }
}
