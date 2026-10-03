using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop.Tests;

public static class ControllerSectionTests
{
    private static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Section controller UI wait timed out");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
    }

    public static void Run()
    {
        DesktopChecks.Check("SectionTelemetry_CancelledAssessment_SupersededNoDuration", () =>
        {
            using var session = new AuthoringSession();
            session.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
            var view = session.BeginSectionDraft(Guid.NewGuid().ToString("D"), 0);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var assessment = session.AssessSection(view.DraftId, view.Generation, cancelled.Token);
            if (assessment.Code != "DSL-CANCELLED") throw new Exception("Assessment did not cancel");
            var ev = session.ReadLocalEvents().Last(item => item.Operation == "section.assess");
            if (ev.Outcome != "superseded" || ev.DurationMilliseconds is not null)
                throw new Exception($"Cancelled assessment recorded {ev.Outcome} with {ev.DurationMilliseconds} ms");
        });

        DesktopChecks.Check("SectionMode_Enter_DraftBoundFirstPointSelected", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            var enter = controller.GetType().GetMethod("EnterSectionAsync")
                ?? throw new Exception("EnterSectionAsync is absent");
            var origin = Enum.Parse(enter.GetParameters()[1].ParameterType, "Properties");
            Wait((Task)enter.Invoke(controller, [0, origin])!);
            var mode = controller.GetType().GetProperty("Section")?.GetValue(controller)
                ?? throw new Exception("Section mode is absent");
            var view = (SectionDraftView)(mode.GetType().GetProperty("Draft")?.GetValue(mode)
                ?? throw new Exception("Section draft view is absent"));
            if (view.Assignment != 0 || view.Cursor != 0 || controller.Selection is not Selection.Points { Items.Count: 1 })
                throw new Exception("Entry did not bind station 0 and select its first section point");
        });
    }
}
