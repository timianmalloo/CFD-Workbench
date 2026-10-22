using Avalonia.Controls;
using Avalonia.Threading;
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
