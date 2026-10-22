using System.Reflection;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Read-only Open from OneDrive, the Desktop side (docs/design/windows-store-recovery.md sections 2.7, 4 B1 to B3, 7 node B; Rulings 259, 260, 261).
/// The OneDrive folder is a fixture folder named by the OneDrive variable; the store's attribute seam gives its root directory the measured
/// cloud root tag (0x9000701A), so the shipped Open path admits it exactly as it would a real one.
/// </summary>
public static class ControllerShellOneDriveTests
{
    private const uint RootTag = 0x9000701A, FullyLocalTag = 0x9000601A, RecallOnDataAccess = 0x400000, Reparse = 0x400;

    public static void Run()
    {
        DesktopChecks.Check("OpenOutcome_ClassifyCode_CloudOnly_And_StartViewSentence", ClassifyCloudOnly);
        if (!OperatingSystem.IsWindows())
        {
            foreach (string name in new[] { "ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing", "ControllerShell_OneDriveOpen_SaveAsAtLocalDefault_OriginBecomesLocal",
                "ControllerShell_OneDriveOpen_TitleAndNotice_NoModal", "ControllerShell_OneDriveOpen_NoticeStaysAcrossStatusReports", "ControllerShell_OneDriveOpen_NoticeRow_DirectlyAboveStatusStrip", "ControllerShell_CloudOnlyRefusal_SentenceAndCodeLine" })
                Console.WriteLine("SKIP " + name + " windows-only");
            return;
        }
        DesktopChecks.Check("ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing", InPlaceSaveWritesNothing);
        DesktopChecks.Check("ControllerShell_OneDriveOpen_SaveAsAtLocalDefault_OriginBecomesLocal", SaveAsBecomesLocal);
        DesktopChecks.Check("ControllerShell_OneDriveOpen_TitleAndNotice_NoModal", TitleAndNotice);
        DesktopChecks.Check("ControllerShell_OneDriveOpen_NoticeStaysAcrossStatusReports", NoticeStays);
        DesktopChecks.Check("ControllerShell_OneDriveOpen_NoticeRow_DirectlyAboveStatusStrip", NoticeRow);
        DesktopChecks.Check("ControllerShell_CloudOnlyRefusal_SentenceAndCodeLine", CloudOnlyRefusal);
    }

    private static void Need(bool condition, string what) { if (!condition) throw new InvalidOperationException(what); }

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
    private static T Await<T>(Task<T> task) { Await((Task)task); return task.GetAwaiter().GetResult(); }

    private static void ClassifyCloudOnly()
    {
        var failure = OpenFailure.ClassifyCode("DOC-CLOUD-ONLY", "x.cfdw.json");
        Need(failure is OpenFailure.CloudOnly && failure.Kind == OpenFailureKind.CloudOnly, "DOC-CLOUD-ONLY is not CloudOnly: " + failure.GetType().Name);
        Need(StartView.FailureMessage(failure, "x.cfdw.json") == Labels.OpenCloudOnly + "\nCode: DOC-CLOUD-ONLY", "the cloud-only message is wrong");
        // U5: a refusal after the data handle exists is DOC-CONFLICT and never shows the "wasn't downloaded" sentence.
        Need(!StartView.FailureMessage(OpenFailure.ClassifyCode("DOC-CONFLICT", "x.cfdw.json"), "x.cfdw.json").Contains("wasn't downloaded"), "DOC-CONFLICT shows DS-COPY-3");
        Need(!StartView.FailureMessage(failure, "x.cfdw.json").Contains("damaged", StringComparison.OrdinalIgnoreCase), "the cloud-only message says damaged");
    }

    private static StoreHooks Hooks(Func<string, (uint, uint)?>? leaf = null, List<string>? stages = null) => new()
    {
        OnStage = stage => { if (stages is not null) lock (stages) stages.Add("stage " + stage); },
        AttributeOverride = (site, name, real) =>
        {
            if (stages is not null) lock (stages) stages.Add("attribute " + site);
            return site == "ancestor"
                ? (name == "OneDrive" ? (real.Attributes | Reparse, RootTag) : real)
                : leaf?.Invoke(site) ?? real;
        }
    };

    /// <summary>A project file in a folder named OneDrive, made before the environment names that folder.</summary>
    private static (string Folder, string Path) Fixture()
    {
        string folder = Path.Combine(TestTemp.NewDirectory("onedrive-open-"), "OneDrive"); Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "wing.cfdw.json");
        using var seed = new WorkbenchController();
        Await(seed.OpenExampleAsync()); Await(seed.SaveAsync(path));
        return (folder, path);
    }

    private static string Snapshot(string folder) => string.Join(";", Directory.GetFileSystemEntries(folder).Order(StringComparer.Ordinal)
        .Select(entry => Path.GetFileName(entry) + ":" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(entry)))));

    private static void WithOneDrive(string folder, Action action)
    {
        string? old = Environment.GetEnvironmentVariable("OneDrive");
        try { Environment.SetEnvironmentVariable("OneDrive", folder); action(); }
        finally { Environment.SetEnvironmentVariable("OneDrive", old); }
    }

    private static (MainWindow Window, ShellHost Host, WorkbenchController Controller) Show(StoreHooks hooks, bool? macOS = null)
    {
        var controller = new WorkbenchController(_ => new ProjectStore(hooks));
        var window = new MainWindow(new PreferenceStore(TestTemp.NewDirectory("onedrive-open-prefs-"), () => new ProjectStore()), macOS, controller);
        window.Show();
        return (window, (ShellHost)window.Content!, controller);
    }

    private static Border Notice(MainWindow window) => window.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "ReadOnlyNotice");

    private static void OpenReadOnly(string path, Action<MainWindow, ShellHost, WorkbenchController> check, string folder, List<string>? stages = null)
    {
        WithOneDrive(folder, () =>
        {
            var (window, host, controller) = Show(Hooks(null, stages));
            try
            {
                Await(host.OpenFileAsync(path));
                Need(controller.Origin == DocumentOrigin.OneDriveReadOnly, "the project did not open read-only: origin " + controller.Origin);
                check(window, host, controller);
            }
            finally { window.Close(); }
        });
    }

    private static void InPlaceSaveWritesNothing()
    {
        var (folder, path) = Fixture(); string before = Snapshot(folder); var stages = new List<string>();
        OpenReadOnly(path, (window, host, controller) =>
        {
            Await(controller.ApplySpanAsync("1400")); Need(controller.IsDirty, "the span edit did not dirty the document");
            var save = (Task)typeof(MainWindow).GetMethod("SaveWithPickerAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null)!;
            Pump(() => window.OwnedWindows.OfType<FolderRefusalDialog>().Any(), "the OneDrive refusal (S2) did not appear");
            var dialog = window.OwnedWindows.OfType<FolderRefusalDialog>().First();
            Need(dialog.Title == FolderRefusalDialog.CloudTitle, "the dialog is not COPY-440: " + dialog.Title);
            dialog.Close(); Await(save);
            Need(Snapshot(folder) == before, "the OneDrive folder changed");
            Need(controller.IsDirty, "the refused save cleaned the document");
            // Defence in depth: the controller never calls the store over the OneDrive path (I-13): no store stage is visited by the direct save.
            lock (stages) stages.Clear();
            var direct = Await(controller.SaveAsync(path));
            Need(direct.Code == "DOC-UNSUPPORTED-PERSISTENCE" && !direct.PublicationKnown && Snapshot(folder) == before, "a direct in-place save reached the store: " + direct.Code);
            lock (stages) Need(stages.Count == 0, "the direct in-place save called the store (stages and attribute reads seen): " + string.Join(",", stages));
        }, folder, stages);
    }

    private static void SaveAsBecomesLocal()
    {
        var (folder, path) = Fixture(); string before = Snapshot(folder);
        string local = Path.Combine(TestTemp.NewDirectory("onedrive-local-"), "copy.cfdw.json");
        OpenReadOnly(path, (window, host, controller) =>
        {
            Await(controller.ApplySpanAsync("1400"));
            Await(window.SaveToAsync(local));
            Need(File.Exists(local) && controller.NativePath == local, "Save As did not write the local copy");
            Need(controller.Origin == DocumentOrigin.Local, "the origin stayed " + controller.Origin);
            Pump(() => !Notice(window).IsVisible && window.Title == "CFD Workbench — Offline Foil", "the notice or the title suffix stayed after Save As: " + window.Title);
            Need(Snapshot(folder) == before, "the OneDrive file changed");
        }, folder);
    }

    private static void TitleAndNotice()
    {
        var (folder, path) = Fixture();
        OpenReadOnly(path, (window, host, controller) =>
        {
            Pump(() => window.Title!.EndsWith(Labels.ReadOnlyTitleSuffix, StringComparison.Ordinal) && Notice(window).IsVisible, "title suffix or notice missing: " + window.Title);
            Need(window.OwnedWindows.Count == 0, "a modal opened on Open");
            Need(((TextBlock)Notice(window).Child!).Text == Labels.ReadOnlyNotice, "the notice text is not DS-COPY-2");
            Need(Avalonia.Automation.AutomationProperties.GetLiveSetting(Notice(window)) == Avalonia.Automation.AutomationLiveSetting.Polite, "the notice is not a polite live region");
        }, folder);
    }

    // U3: the notice is its own element; ordinary reports use the status slot and can neither replace nor hide it.
    private static void NoticeStays()
    {
        var (folder, path) = Fixture();
        OpenReadOnly(path, (window, host, controller) =>
        {
            Pump(() => Notice(window).IsVisible, "the notice did not appear");
            Need(!Notice(window).GetVisualAncestors().Contains(host.StatusStrip), "the notice is inside the status strip");
            foreach (var report in new[] { new StatusReport(Labels.SaveRefusal("DOC-CONFLICT"), ReportKind.Error), new StatusReport("Saving…"), new StatusReport("Analysis: nothing to evaluate yet.") })
            {
                host.Report(report); Dispatcher.UIThread.RunJobs();
                Need(host.StatusStrip.Text == report.Text, "the report did not use the status slot: " + host.StatusStrip.Text);
                Need(Notice(window).IsVisible && ((TextBlock)Notice(window).Child!).Text == Labels.ReadOnlyNotice, "a report replaced or hid the notice: " + report.Text);
            }
        }, folder);
    }

    // U3 / DS-COPY-2: the notice row is directly above the status strip and shares its grid row with no other shell child, also when the
    // menu bar arrives after the notice was placed (a window built without the bar, then ShowMenuBar).
    private static void NoticeRow()
    {
        foreach (bool lateBar in new[] { false, true })
        {
            var (window, host, _) = Show(Hooks(), lateBar ? true : null);
            try
            {
                if (lateBar) host.ShowMenuBar();
                Dispatcher.UIThread.RunJobs();
                var notice = Notice(window); int row = Grid.GetRow(notice);
                string where = lateBar ? " (menu bar added after the notice)" : "";
                Need(row == Grid.GetRow(host.StatusStrip) - 1, "the notice row " + row + " is not directly above the status strip row " + Grid.GetRow(host.StatusStrip) + where);
                Need(host.Children.Count(child => Grid.GetRow(child) == row) == 1, "another shell child shares the notice row" + where);
                var named = new Control?[] { host.MenuBar, host.DockHost, host.AnalysisPanel, notice, host.StatusStrip }.OfType<Control>().Select(Grid.GetRow).ToList();
                Need(named.Distinct().Count() == named.Count && named.Count == host.RowDefinitions.Count, "the shell rows are not distinct or do not match the row definitions" + where);
            }
            finally { window.Close(); }
        }
    }

    private static void CloudOnlyRefusal()
    {
        var (folder, path) = Fixture();
        WithOneDrive(folder, () =>
        {
            var (window, host, controller) = Show(Hooks(site => (0x20u | Reparse | RecallOnDataAccess, FullyLocalTag)));
            try
            {
                var outcome = Await(controller.OpenAsync(path));
                Need(outcome is OpenOutcome.Failed { Failure: OpenFailure.CloudOnly }, "the open outcome is " + outcome);
                var failure = ((OpenOutcome.Failed)outcome).Failure;
                string message = StartView.FailureMessage(failure, "wing.cfdw.json");
                Need(message == Labels.OpenCloudOnly + "\nCode: DOC-CLOUD-ONLY", "the message is: " + message.Replace("\n", " | "));
                Need(!message.Contains("damaged", StringComparison.OrdinalIgnoreCase), "the message says damaged");
                Need(controller.Inspection is null && controller.Origin == DocumentOrigin.Local, "a refused open left a document behind");
            }
            finally { window.Close(); }
        });
    }
}
