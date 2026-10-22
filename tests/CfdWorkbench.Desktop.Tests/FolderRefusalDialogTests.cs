using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Ruling 160 (1) and (5): the dialog shown after a save-folder pick that is synced by OneDrive or could not be checked. The native
/// Windows picker cannot be driven from the harness, so this covers the dialog itself; the decision it shows is covered in the Core
/// project (WindowsStore_CloudFolder_*). The strings below are the oracle copy: Ruling 146 (1) and COPY-440, 442, 445, 454, 457.
/// </summary>
public static class FolderRefusalDialogTests
{
    public static void Run()
    {
        DesktopChecks.Check("FolderRefusalDialog_Cloud_Ruling146TextAndChooseAnother", () =>
        {
            var dialog = Shown(FolderLocation.Cloud, "DOC-IO");
            try
            {
                Need(dialog.Title == "Can't save to OneDrive", $"title '{dialog.Title}'");
                Need(Body(dialog) == "This folder is synced by OneDrive. CFD Workbench saves only to local folders, so a sync can't damage a save. Choose a folder outside OneDrive.",
                    $"body '{Body(dialog)}'");
                var (choose, cancel) = Buttons(dialog);
                Need((string?)choose.Content == "Choose another folder…" && choose.IsDefault && !choose.IsCancel, "primary button");
                Need((string?)cancel.Content == "Cancel" && cancel.IsCancel && !cancel.IsDefault, "Cancel button");
                Need(!dialog.ChooseAnotherFolder, "the choice starts as Cancel");
                choose.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                Need(dialog.ChooseAnotherFolder && !dialog.IsVisible, "Choose another folder… did not answer and close");
            }
            finally { dialog.Close(); }
        });
        DesktopChecks.Check("FolderRefusalDialog_CheckFailed_Copy454And457AndCancel", () =>
        {
            var dialog = Shown(FolderLocation.CheckFailed, "DOC-IO");
            try
            {
                Need(dialog.Title == "Can't check this folder", $"title '{dialog.Title}'");
                Need(Body(dialog) == "Couldn't check whether this folder is synced by OneDrive (DOC-IO). Nothing was saved. Choose another folder.", $"body '{Body(dialog)}'");
                var (_, cancel) = Buttons(dialog);
                cancel.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                Need(!dialog.ChooseAnotherFolder && !dialog.IsVisible, "Cancel did not close the dialog with no choice");
            }
            finally { dialog.Close(); }
        });
    }

    public static void RunWindowsStore()
    {
        if (!OperatingSystem.IsWindows()) return;
        DesktopChecks.Check("SaveBlocked_CrashLeftClaim_DialogShownInsteadOfInUseLine", SaveBlockedOnCrashLeftClaim);
        DesktopChecks.Check("SaveInPlace_UnderEnvironmentOneDrive_RefusedNothingWritten", InPlaceUnderOneDrive);
    }

    private static void Await(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("task did not complete");
            Dispatcher.UIThread.RunJobs(); Thread.Sleep(5);
        }
        task.GetAwaiter().GetResult();
    }

    // The measured inventory of an unfinished save (Core: WindowsStore_CrashInjection_LeftoverInventoryMeasured): a 0-byte .cfd-writer.claim and
    // a .cfd-<operation>.tmp beside the project. The next save is refused DOC-CONFLICT; the app shows "Save is blocked" (Ruling 253 W2-Q3 c).
    private static void SaveBlockedOnCrashLeftClaim()
    {
        string folder = TestTemp.NewDirectory("blocked-"), path = Path.Combine(folder, "project.cfdw.json");
        var preferences = new PreferenceStore(TestTemp.NewDirectory("blocked-prefs-"), () => new ProjectStore());
        var window = new MainWindow(preferences);
        try
        {
            window.Show();
            Await(((ShellHost)window.Content!).OpenExampleAsync());
            Await(window.SaveToAsync(path));
            Need(File.Exists(path) && Directory.GetFileSystemEntries(folder).Length == 1, "the first save did not leave exactly the project");
            byte[] saved = File.ReadAllBytes(path);
            var controller = ((ShellHost)window.Content!).Controller;
            Await(controller.ApplySpanAsync("1400"));
            Need(controller.IsDirty, "the span edit did not dirty the document");
            string inUseLine = CfdWorkbench.Analysis.Labels.SaveRefusal("DOC-CONFLICT"); // COPY-414
            File.WriteAllBytes(Path.Combine(folder, ".cfd-writer.claim"), []);
            File.WriteAllBytes(Path.Combine(folder, ".cfd-" + Guid.NewGuid().ToString("D") + ".tmp"), [1, 2, 3]);
            var pending = window.SaveToAsync(path);
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            SaveBlockedDialog? dialog = null;
            while ((dialog = window.OwnedWindows.OfType<SaveBlockedDialog>().FirstOrDefault()) is null)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("the Save is blocked dialog did not appear");
                Dispatcher.UIThread.RunJobs(); Thread.Sleep(5);
            }
            Need(dialog!.Title == "Save is blocked", $"title '{dialog.Title}'");
            Need(((ShellHost)window.Content!).StatusStrip.Text != inUseLine, "COPY-414 is shown behind the Save is blocked dialog");
            var panel = (StackPanel)dialog.Content!;
            var texts = panel.Children.OfType<TextBlock>().Select(text => text.Text ?? "").ToArray();
            Need(texts[0] == "An earlier save of this project didn't finish (the app may have closed unexpectedly). Your saved file is unchanged.", $"lead '{texts[0]}'");
            var steps = panel.Children.OfType<StackPanel>().First().Children.OfType<TextBlock>().Select(text => text.Text ?? "").ToArray();
            Need(steps.SequenceEqual(["1. Close any other CFD Workbench window using this project.", "2. Check the project shows your last saved work.",
                "3. Use Save As to keep your current work in a different folder."]), "steps: " + string.Join(" | ", steps));
            Need(texts[1].Contains("stays blocked", StringComparison.Ordinal) && texts[1].Contains(".cfd-writer.claim", StringComparison.Ordinal), $"blocked sentence '{texts[1]}'");
            var buttons = panel.Children.OfType<StackPanel>().Last().Children.OfType<Button>().Select(button => (string?)button.Content).ToArray();
            Need(buttons.SequenceEqual(["Save As…", "Cancel"]), "buttons: " + string.Join(" | ", buttons));
            panel.Children.OfType<StackPanel>().Last().Children.OfType<Button>().Last().RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Await(pending);
            Need(!dialog.SaveAs, "Cancel answered Save As");
            Need(((ShellHost)window.Content!).StatusStrip.Text != inUseLine, "COPY-414 is shown after the Save is blocked dialog closed");
            Need(controller.IsDirty, "Cancel on the Save is blocked dialog cleaned the document");
            Need(File.ReadAllBytes(path).AsSpan().SequenceEqual(saved) && File.Exists(Path.Combine(folder, ".cfd-writer.claim")), "the refused save changed the folder");
            Need(UnfinishedSave.BlocksFolder(path), "BlocksFolder did not see the claim");
        }
        finally { window.Close(); }
    }

    // Security veto (repair cycle 2): a project opened from a OneDrive folder and saved in place is refused before any write.
    private static void InPlaceUnderOneDrive()
    {
        string folder = TestTemp.NewDirectory("inplace-"), path = Path.Combine(folder, "project.cfdw.json");
        using var controller = new WorkbenchController();
        Await(controller.OpenExampleAsync());
        var first = controller.SaveAsync(path); Await(first);
        Need(first.Result.Code == "OK" && controller.NativePath == path, "the first save: " + first.Result.Code);
        byte[] saved = File.ReadAllBytes(path);
        string? previous = Environment.GetEnvironmentVariable("OneDrive");
        try
        {
            Environment.SetEnvironmentVariable("OneDrive", folder);
            var second = controller.SaveAsync(controller.NativePath!); Await(second);
            Need(second.Result.Code == "DOC-UNSUPPORTED-PERSISTENCE", "in-place save under OneDrive: " + second.Result.Code);
        }
        finally { Environment.SetEnvironmentVariable("OneDrive", previous); }
        Need(Directory.GetFileSystemEntries(folder).Length == 1 && File.ReadAllBytes(path).AsSpan().SequenceEqual(saved), "the refused save wrote into the folder");
    }

    private static FolderRefusalDialog Shown(FolderLocation location, string code)
    {
        var dialog = new FolderRefusalDialog(location, code);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        return dialog;
    }

    private static string? Body(FolderRefusalDialog dialog) => Panel(dialog).Children.OfType<TextBlock>().Single().Text;
    private static (Button Choose, Button Cancel) Buttons(FolderRefusalDialog dialog)
    {
        var buttons = Panel(dialog).Children.OfType<StackPanel>().Single().Children.OfType<Button>().ToArray();
        return (buttons[0], buttons[1]);
    }
    private static StackPanel Panel(FolderRefusalDialog dialog) => (StackPanel)dialog.Content!;
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
