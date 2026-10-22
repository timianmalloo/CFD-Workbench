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
        DesktopChecks.Check("OpenRefusal_UnsupportedLocation_Windows_NamesLocationAndNameCauses", RefusalWindows);
        DesktopChecks.Check("OpenRefusal_UnsupportedLocation_OffWindows_NoDamagedNoWindowsCause", RefusalOffWindows);
        DesktopChecks.Check("OpenRefusal_Conflict_ChangedOrInUse_NotDamaged", RefusalConflict);
        DesktopChecks.Check("OpenRefusal_ConflictOnAbsentFile_IsMissing", RefusalAbsentFile);
        DesktopChecks.Check("OpenRefusal_AccessDenied_FinderOnMacOnly", RefusalAccessDenied);
        DesktopChecks.Check("OpenRefusal_Sentences_AreTheLabelsConstants", RefusalConstants);
        if (!OperatingSystem.IsWindows())
        {
            foreach (string name in new[] { "ControllerShell_OneDriveOpen_ExportStartsAtLocalDefault","ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing", "ControllerShell_OneDriveOpen_SaveAsAtLocalDefault_OriginBecomesLocal",
                "ControllerShell_OneDriveOpen_TitleAndNotice_NoModal", "ControllerShell_OneDriveOpen_NoticeStaysAcrossStatusReports", "ControllerShell_OneDriveOpen_NoticeRow_DirectlyAboveStatusStrip", "ControllerShell_CloudOnlyRefusal_SentenceAndCodeLine" })
                Console.WriteLine("SKIP " + name + " windows-only");
            return;
        }
        DesktopChecks.Check("ControllerShell_OneDriveOpen_ExportStartsAtLocalDefault", ExportStartsAtLocalDefault);
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

    // Pure sentence mappings: they call no operating-system API, so they run on every host and need no host guard (TEST-FOREIGN-OS-BRANCH concerns OS calls).
    private const bool ForWindows = true, ForOtherSystem = false;

    private static string Message(string code, string path, bool windows) =>
        StartView.FailureMessage(OpenFailure.ClassifyCode(code, path), Path.GetFileName(path), windows);

    private static bool Has(string text, string part) => text.Contains(part, StringComparison.OrdinalIgnoreCase);

    // Items 1 and 7: the Windows location sentence names every cause the store reports under one code, none of them "damaged".
    private static void RefusalWindows()
    {
        string message = Message("DOC-UNSUPPORTED-PERSISTENCE", "x.cfdw.json", windows: ForWindows);
        Need(!Has(message, "damaged") && !Has(message, "windows can't use"), "blames damage or Windows: " + message);
        Need(Has(message, "name or path") && Has(message, "cloud-synced") && Has(message, "network"), "a store cause is missing: " + message);
        Need(Has(message, "copy the project to a local folder"), "no remedy: " + message);
        Need(message.EndsWith("\nCode: DOC-UNSUPPORTED-PERSISTENCE", StringComparison.Ordinal), "no code line: " + message);
    }

    // Item 2: off Windows the same code is a truthful sentence about the location and system, with no Windows or OneDrive cause and no "damaged".
    private static void RefusalOffWindows()
    {
        string message = Message("DOC-UNSUPPORTED-PERSISTENCE", "x.cfdw.json", windows: ForOtherSystem);
        Need(!Has(message, "damaged") && !Has(message, "onedrive") && !Has(message, "windows"), "wrong cause: " + message);
        Need(Has(message, "can't open") && Has(message, "local folder"), "no statement and remedy: " + message);
    }

    private static string ExistingFile()
    {
        string path = Path.Combine(TestTemp.NewDirectory("open-cause-"), "wing.cfdw.json");
        File.WriteAllBytes(path, "{}"u8.ToArray());
        return path;
    }

    // Item 3: DOC-CONFLICT on Open is a file that changed or was in use while it was read (Ruling 261), not a damaged one.
    private static void RefusalConflict()
    {
        string message = Message("DOC-CONFLICT", ExistingFile(), OperatingSystem.IsWindows());
        Need(!Has(message, "damaged") && !Has(message, "wasn't downloaded"), "says damaged: " + message);
        Need(Has(message, "changed") && Has(message, "was using it") && Has(message, "try opening it again"), "no cause and remedy: " + message);
    }

    // Item 4: the store reports a missing file as DOC-CONFLICT; the file system tells them apart without a store change.
    private static void RefusalAbsentFile()
    {
        string absent = Path.Combine(TestTemp.NewDirectory("open-cause-"), "gone.cfdw.json");
        var failure = OpenFailure.ClassifyCode("DOC-CONFLICT", absent);
        Need(failure is OpenFailure.Missing, "an absent file is " + failure.GetType().Name);
        string message = StartView.FailureMessage(failure, "gone.cfdw.json", OperatingSystem.IsWindows());
        Need(Has(message, "isn't there any more") && !Has(message, "damaged") && !Has(message, "using it"), message);
        Need(OpenFailure.ClassifyCode("DOC-CONFLICT", "") is not OpenFailure.Missing, "no path is a conflict, not a missing file");
    }

    // Item 5: File Explorer on Windows, Finder on macOS (COPY-126 keeps its row).
    private static void RefusalAccessDenied()
    {
        string windows = Message("DOC-ACCESS", "x.cfdw.json", windows: ForWindows), mac = Message("DOC-ACCESS", "x.cfdw.json", windows: ForOtherSystem);
        Need(!Has(windows, "finder") && Has(windows, "File Explorer") && Has(windows, "Properties") && Has(windows, "Security"), windows);
        Need(Has(mac, "in Finder") && !Has(mac, "File Explorer"), mac);
    }

    // Every refusal sentence is the Labels constant, never a retyped literal (COPY-641 to 645).
    private static void RefusalConstants()
    {
        string path = ExistingFile(), absent = Path.Combine(Path.GetDirectoryName(path)!, "gone.cfdw.json");
        Need(Message("DOC-UNSUPPORTED-PERSISTENCE", path, true) == Labels.OpenUnavailableWindows + "\nCode: DOC-UNSUPPORTED-PERSISTENCE", "COPY-641 is not the Windows sentence");
        Need(Message("DOC-UNSUPPORTED-PERSISTENCE", path, false) == Labels.OpenUnavailableOther + "\nCode: DOC-UNSUPPORTED-PERSISTENCE", "COPY-642 is not the other sentence");
        Need(Message("DOC-CONFLICT", path, true) == Labels.OpenChanged + "\nCode: DOC-CONFLICT", "COPY-643 is not the conflict sentence");
        Need(Message("DOC-CONFLICT", absent, true) == Labels.OpenGone + "\nCode: DOC-CONFLICT", "COPY-644 is not the absent-file sentence");
        Need(Message("DOC-ACCESS", path, true) == Labels.OpenAccessDeniedWindows + "\nCode: DOC-ACCESS", "COPY-645 is not the Windows permission sentence");
        Need(Message("DOC-ACCESS", path, false) == Labels.OpenAccessDeniedMac + "\nCode: DOC-ACCESS", "COPY-126 changed on macOS");
    }

    // Item 8: a read-only OneDrive document must not start the export panel in the folder where its save would be refused.
    private static void ExportStartsAtLocalDefault()
    {
        var (folder, path) = Fixture();
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CFD Workbench");
        OpenReadOnly(path, (window, host, controller) =>
        {
            string? start = controller.ExportSnapshot()!.ProjectFolder;
            Need(start is not null && !string.Equals(start, folder, StringComparison.OrdinalIgnoreCase), "the export panel starts in the OneDrive folder: " + start);
            Need(string.Equals(start, expected, StringComparison.OrdinalIgnoreCase), "the export start is not the Save As default: " + start);
        }, folder);
        string local = Path.Combine(TestTemp.NewDirectory("onedrive-local-"), "copy.cfdw.json");
        using var controller2 = new WorkbenchController();
        Await(controller2.OpenExampleAsync()); Await(controller2.SaveAsync(local));
        Need(string.Equals(controller2.ExportSnapshot()!.ProjectFolder, Path.GetDirectoryName(local), StringComparison.OrdinalIgnoreCase), "a local project lost its own folder");
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
