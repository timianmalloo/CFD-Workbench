using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop.Tests;

public static class ControllerSectionTests
{
    private static WorkbenchController Open()
    {
        var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        return controller;
    }

    private static SectionStep.Move Raise(WorkbenchController controller, double delta = .01)
    {
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
        return new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + delta);
    }

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
        DesktopChecks.Check("SectionMode_GestureEnd_AppendsStepNotAcceptedRow", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Side));
            string source = controller.AcceptedSource;
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            if (!controller.BeginGesture(new PointRef("upper", point.Id, controller.Section!.Draft.Profile), GestureInput.Pointer))
                throw new Exception("Section gesture did not begin");
            controller.UpdateGesture(point.SpanMeters, point.Ordinate + .01, 4);
            Wait(controller.EndGestureAsync(GestureEnd.Release));
            if (controller.Section?.Draft.Cursor != 1 || controller.AcceptedSource != source)
                throw new Exception("Gesture did not append one inner step while keeping accepted bytes");
        });

        DesktopChecks.Check("SectionMode_UndoRoutedToInnerStep_DocumentUndoUntouched", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string source = controller.AcceptedSource;
            Wait(controller.ApplySectionStepAsync(Raise(controller)));
            controller.Undo();
            if (controller.Section?.Draft.Cursor != 0 || controller.AcceptedSource != source || controller.CanUndo)
                throw new Exception("Undo did not stop at the section entry");
            controller.Redo();
            if (controller.Section?.Draft.Cursor != 1 || controller.AcceptedSource != source)
                throw new Exception("Redo did not restore the inner step");
        });

        DesktopChecks.Check("SectionMode_UndoAtEntry_DocumentUndoDepthUnchanged", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string source = controller.AcceptedSource;
            controller.Undo();
            if (controller.Section?.Draft.Cursor != 0 || controller.AcceptedSource != source)
                throw new Exception("Undo at entry touched document history");
        });

        DesktopChecks.Check("SectionMode_Finish_OneUndoStepStationSelected", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string source = controller.AcceptedSource;
            Wait(controller.ApplySectionStepAsync(Raise(controller)));
            if (controller.Section?.CanFinish != true) throw new Exception("Certified step cannot Finish");
            Wait(controller.FinishSectionAsync());
            if (controller.Section is not null || controller.Selection is not Selection.Station { Index: 0 } ||
                controller.AcceptedSource == source)
                throw new Exception("Finish did not return to the selected station with new bytes");
            controller.Undo();
            if (controller.AcceptedSource != source) throw new Exception("One document undo did not restore base bytes");
        });

        DesktopChecks.Check("SectionMode_Cancel_ViewsBackStationSelected", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string source = controller.AcceptedSource;
            Wait(controller.ApplySectionStepAsync(Raise(controller)));
            controller.CancelSection();
            if (controller.Section is not null || controller.Selection is not Selection.Station { Index: 0 } ||
                controller.AcceptedSource != source)
                throw new Exception("Cancel changed accepted state or selection");
        });

        DesktopChecks.Check("Selection_SectionPointReconcile_DroppedAfterLeavingMode", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var selected = controller.Selection;
            if (selected is not Selection.Points) throw new Exception("Section point was not selected");
            controller.CancelSection();
            if (WorkbenchController.Reconcile(selected, controller.CurrentProjection) is Selection.Points)
                throw new Exception("Section point survived after leaving the mode");
        });

        DesktopChecks.Check("SectionTelemetry_EnterEvent_CarriesOrigin", () =>
        {
            CfdWorkbench.Desktop.Shell.ShellEvents.Clear();
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var ev = CfdWorkbench.Desktop.Shell.ShellEvents.Read().Last(item => item.Name == "section.mode.enter");
            if (ev.Trigger != "palette" || ev.EditKind != "section")
                throw new Exception("Entry event lost its origin");
        });

        DesktopChecks.Check("SectionMode_StripSwitchNoEdits_DraftRebindsToNewStation", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Side));
            Wait(controller.EnterSectionAsync(1, EntryOrigin.Side));
            if (controller.Section?.Draft.Assignment != 1 || controller.Section.Draft.Cursor != 0)
                throw new Exception("Untouched section visit did not rebind to station 1");
        });

        DesktopChecks.Check("SectionMode_SaveWhileDrafting_AsksFinishOrCancelFirst", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string path = Path.Combine(Path.GetTempPath(), $"section-{Guid.NewGuid():N}.cfdw.json");
            try
            {
                try { Wait(controller.SaveAsync(path)); throw new Exception("Save accepted an open section draft"); }
                catch (ContractError error) when (error.Code == "DSL-DRAFT-OWNED") { }
                if (File.Exists(path) || controller.Section is null ||
                    !controller.Status.Contains("Finish or Cancel", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Save did not ask for Finish or Cancel before disk write");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        });

        DesktopChecks.Check("SectionDraft_ResumeRecovery_CurrentViewReadable", () =>
        {
            using var source = new AuthoringSession();
            source.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
            var draft = source.BeginSectionDraft(Guid.NewGuid().ToString("D"), 0);
            draft = source.ApplySectionStep(draft.DraftId, draft.Generation, new SectionStep.MakeUnique());
            source.CaptureRecovery();
            using var reopened = new AuthoringSession();
            reopened.Reopen(source.SaveImage());
            reopened.ResumeRecovery();
            var reader = reopened.GetType().GetMethod("CurrentSectionDraft")
                ?? throw new Exception("CurrentSectionDraft reader is absent");
            var restored = (SectionDraftView?)reader.Invoke(reopened, null)
                ?? throw new Exception("Recovered section view is absent");
            if (restored.Cursor != 0 || restored.StepCount != 0 || restored.Profile != draft.Profile ||
                !restored.Bytes.SequenceEqual(draft.Bytes))
                throw new Exception("Recovered draft did not expose its cursor-zero view");
        });

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
