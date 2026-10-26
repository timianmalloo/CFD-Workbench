using System.IO;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using CfdWorkbench.Analysis;
using CfdWorkbench.Desktop.Panes;

namespace CfdWorkbench.Desktop.Tests;

public static class StoreCodeSurfaceTests
{
    public static void Run()
    {
        DesktopChecks.Check("StoreCodes_NonNtfsRemedy_NamesDrive", () =>
        {
            const string path = @"Q:\foils\wing.cfdw.json";
            string open = StartView.FailureMessage(OpenFailure.ClassifyCode("DOC-UNSUPPORTED-PERSISTENCE", path), "wing.cfdw.json", true);
            using var controller = new WorkbenchController(_ => new RefusingStore("DOC-UNSUPPORTED-PERSISTENCE"));
            controller.NewFoilAsync().GetAwaiter().GetResult();
            controller.SaveAsync(path).GetAwaiter().GetResult();
            if (!open.Contains("Q:", StringComparison.Ordinal) || !open.Contains("NTFS", StringComparison.Ordinal) ||
                !controller.Status.Contains("Q:", StringComparison.Ordinal) || !controller.Status.Contains("NTFS", StringComparison.Ordinal))
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
}
