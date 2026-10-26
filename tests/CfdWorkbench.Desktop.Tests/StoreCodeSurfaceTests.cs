using System.IO;
using Avalonia;
using Avalonia.Controls;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using CfdWorkbench.Analysis;
using CfdWorkbench.Desktop.Panes;

namespace CfdWorkbench.Desktop.Tests;

public static class StoreCodeSurfaceTests
{
    public static void Run()
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try { RunChecks(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void RunChecks()
    {
        DesktopChecks.Check("StoreCodes_SaveSection_UnadmittedHost_UsesSystemSentence", () =>
        {
            var previous = SynchronizationContext.Current;
            try
            {
                if (Application.Current is null) AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
                using var controller = new WorkbenchController();
                foreach (string code in new[] { "DOC-UNSUPPORTED-PERSISTENCE", "DOC-UNSUPPORTED-PLATFORM" })
                {
                    var dialog = new SaveSectionDialog(controller, _ => Task.FromException(new ContractError(code)), storeAdmitted: () => false);
                    dialog.Show();
                    try
                    {
                        dialog.FindControl<TextBox>("NameBox")!.Text = "Kept";
                        dialog.SaveAsync().GetAwaiter().GetResult();
                        string? actual = dialog.FindControl<TextBlock>("SaveError")!.Text;
                        if (actual != Labels.SectionSaveUnavailable) throw new Exception(code + " unadmitted host: " + actual);
                    }
                    finally { dialog.Close(); }
                }
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        });
        DesktopChecks.Check("StoreCodes_ConflictExistenceProbe_OffDispatcher", () =>
        {
            // Earlier controller checks can bind Avalonia's uninitialized dispatcher. Qualify thread affinity in a fresh process.
            if (Environment.GetEnvironmentVariable("CFD_STORE_DISPATCHER_CHILD") != "1")
            {
                string host = Path.Combine(AppContext.BaseDirectory, "CfdWorkbench.Desktop.Tests" + (OperatingSystem.IsWindows() ? ".exe" : ""));
                var start = new System.Diagnostics.ProcessStartInfo(host) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add("--harness"); start.ArgumentList.Add("--controller-shell");
                start.Environment["CFD_STORE_DISPATCHER_CHILD"] = "1";
                start.Environment["CFD_TEST_ONLY"] = "StoreCodes_ConflictExistenceProbe_OffDispatcher";
                using var child = System.Diagnostics.Process.Start(start) ?? throw new Exception("dispatcher child did not start");
                var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(45_000)) { child.Kill(entireProcessTree: true); throw new Exception("dispatcher child timed out"); }
                if (child.ExitCode != 0 || !output.GetAwaiter().GetResult().Contains("PASS StoreCodes_ConflictExistenceProbe_OffDispatcher", StringComparison.Ordinal))
                    throw new Exception("dispatcher child: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
                return;
            }
            var previous = SynchronizationContext.Current;
            if (Avalonia.Application.Current is null)
                Avalonia.AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
            var dispatcher = Avalonia.Threading.Dispatcher.UIThread;
            if (!dispatcher.CheckAccess()) throw new Exception("test must start on dispatcher thread");
            var context = new PumpContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                foreach (bool exists in new[] { false, true })
                {
                    bool? onDispatcher = null;
                    using var controller = new WorkbenchController(_ => new RefusingStore("DOC-CONFLICT"),
                        fileExistsProbe: _ => { onDispatcher = dispatcher.CheckAccess(); return exists; });
                    var task = controller.OpenAsync(TestTemp.Combine("conflict.cfdw.json"));
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    while (!task.IsCompleted && DateTime.UtcNow < deadline) { context.Pump(); Thread.Sleep(1); }
                    if (!task.IsCompleted) throw new Exception("open did not complete");
                    var outcome = task.GetAwaiter().GetResult();
                    if (onDispatcher != false) throw new Exception("existence probe dispatcher=" + onDispatcher);
                    if (outcome is not OpenOutcome.Failed failure ||
                        failure.Failure.Kind != (exists ? OpenFailureKind.Changed : OpenFailureKind.Missing))
                        throw new Exception("conflict classification lost existence result");
                }
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        });
        DesktopChecks.Check("StoreCodes_NonNtfsRemedy_NamesDrive", () =>
        {
            const string path = @"Q:\foils\wing.cfdw.json";
            string open = StartView.FailureMessage(OpenFailure.ClassifyCode("DOC-UNSUPPORTED-PERSISTENCE", path), "wing.cfdw.json", true);
            using var controller = new WorkbenchController(_ => new RefusingStore("DOC-UNSUPPORTED-PERSISTENCE"));
            controller.NewFoilAsync().GetAwaiter().GetResult();
            controller.SaveAsync(path).GetAwaiter().GetResult();
            if (!open.Contains("Q:", StringComparison.Ordinal) || !open.Contains("NTFS", StringComparison.Ordinal) ||
                OperatingSystem.IsWindows() && (!controller.Status.Contains("Q:", StringComparison.Ordinal) || !controller.Status.Contains("NTFS", StringComparison.Ordinal)))
                throw new Exception("drive/NTFS remedy absent: Open=" + open.Replace('\n', '|') + "; Save=" + controller.Status);
        });
        DesktopChecks.Check("StoreCodes_NewCodes_ReachEverySurface", () =>
        {
            var errors = new List<string>();
            foreach (string code in new[] { "DOC-UNSUPPORTED-PLATFORM", "DOC-NOT-A-FILE" })
            {
                using var controller = new WorkbenchController(_ => new RefusingStore(code));
                controller.NewFoilAsync().GetAwaiter().GetResult();
                controller.SaveAsync(TestTemp.Combine("refused.cfdw.json")).GetAwaiter().GetResult();
                if (controller.StatusKind != ReportKind.Warning) errors.Add(code + " status=" + controller.StatusKind);
                if (MainWindow.SaveFailureKind(code) != ReportKind.Warning) errors.Add(code + " window status is not Warning");
                var open = controller.OpenAsync(TestTemp.Combine("refused.cfdw.json")).GetAwaiter().GetResult();
                if (open is not OpenOutcome.Failed failed) throw new Exception(code + " did not fail Open");
                string message = StartView.FailureMessage(failed.Failure, "refused.cfdw.json");
                string expected = code == "DOC-UNSUPPORTED-PLATFORM"
                    ? "CFD Workbench can't open projects on this system. The file hasn't been changed."
                    : "This path is a folder. Choose a project file inside it. The folder hasn't been changed.";
                if (!message.StartsWith(expected, StringComparison.Ordinal)) errors.Add(code + " StartView=" + message.Replace('\n', '|'));
                string root = TestTemp.NewDirectory("store-codes-");
                try
                {
                    Directory.CreateDirectory(Path.Combine(root, "display"));
                    File.WriteAllText(Path.Combine(root, "display", "display.json"), "{}");
                    Directory.CreateDirectory(Path.Combine(root, "layout"));
                    File.WriteAllText(Path.Combine(root, "layout", "layout.json"), "{}");
                    Directory.CreateDirectory(Path.Combine(root, "recent"));
                    File.WriteAllText(Path.Combine(root, "recent", "recent.json"), "{}");
                    var prefs = new PreferenceStore(root, () => new RefusingStore(code));
                    var loaded = prefs.LoadTextSizeAsync(default).GetAwaiter().GetResult();
                    if (loaded.Outcome != "session-only" || !loaded.SessionOnly || !loaded.Codes.Contains(code))
                        errors.Add(code + " preferences=" + loaded.Outcome);
                    var layout = prefs.LoadLayoutAsync(new HashSet<string>(), default).GetAwaiter().GetResult();
                    var recent = prefs.LoadRecentAsync(default).GetAwaiter().GetResult();
                    if (layout.Outcome != "session-only" || recent.Outcome != "session-only") errors.Add(code + " layout/recent not session-only");
                }
                finally { Directory.Delete(root, true); }
            }
            if (errors.Count > 0) throw new Exception(string.Join("; ", errors));
        });
    }

    private sealed class RefusingStore(string code) : IProjectStore
    {
        public void Dispose() { }
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => Task.FromException<ReadResult>(new ContractError(code));
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default) => Task.FromResult(new SaveResult(code, null, false, false));
    }

    private sealed class PumpContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> jobs = new();
        public override void Post(SendOrPostCallback callback, object? state) => jobs.Enqueue(() => callback(state));
        public void Pump() { while (jobs.TryDequeue(out var job)) job(); }
    }
}
