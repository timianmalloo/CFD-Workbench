using System.Globalization;
using System.Reflection;
using System.Text;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using CfdWorkbench.Desktop.Shell;

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

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Section controller state did not settle");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
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

    /// <summary>
    /// FLK-1: a refused step re-checks the unchanged draft. This covers the re-check window after a refusal only.
    /// "Checking…" from the moment the step is queued until the refusal is known is unavoidable, because the refusal
    /// is not known beforehand.
    /// </summary>
    private static void SectionStep_Refused_RestoresCertificate()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int heldCalls = 0;
        bool arm = false;
        using var controller = new WorkbenchController(sectionAssessmentGate: _ =>
        {
            if (!arm) return Task.CompletedTask;
            Interlocked.Increment(ref heldCalls);
            return held.Task;
        });
        var view = new SectionEditorView();
        var window = new Window { Content = view, Width = 900, Height = 600 };
        window.Show();
        string? failure = null;
        try
        {
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            Wait(controller.ApplySectionStepAsync(new SectionStep.MakeUnique()));
            WaitFor(() => controller.Section is { Assessment: not null, CanFinish: true });
            var before = controller.Section!;
            var target = controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            arm = true;
            var refused = controller.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, target.Id, true));
            WaitFor(() => refused.IsCompleted && Volatile.Read(ref heldCalls) > 0);
            Dispatcher.UIThread.RunJobs();
            var during = controller.Section;
            view.Bind(controller);
            Dispatcher.UIThread.RunJobs();
            string? help = AutomationProperties.GetHelpText(view.FindControl<Button>("ModeFinishButton")!);
            string? box = view.FindControl<TextBlock>("ModeReason")!.Text;
            if (during is null)
                failure = "The refused step closed the section";
            else if (during.FinishReason == "Checking…" || help == "Checking…" || box == "Checking…")
                failure = $"A refused step showed Finish \"Checking…\" while it re-checked: reason '{during.FinishReason}', help '{help}', box '{box}'";
            else if (during.Draft.Generation != before.Draft.Generation || during.CanFinish != before.CanFinish || during.FinishReason != before.FinishReason)
                failure = $"The re-check changed Finish before it returned: can finish {before.CanFinish} → {during.CanFinish}, reason '{before.FinishReason}' → '{during.FinishReason}'";
            held.TrySetResult();
            try { Wait(refused); }
            catch (ContractError) { }
            WaitFor(() => controller.Section?.Assessment is not null);
            if (failure is null && controller.Section?.FinishReason == "Checking…")
                failure = "Finish stayed on Checking… after the refused step's re-check";
        }
        finally
        {
            held.TrySetResult();
            try { WaitFor(() => controller.Section?.Assessment is not null || controller.Section is null); }
            catch (TimeoutException) { }
            window.Close();
        }
        if (failure is not null) throw new Exception(failure);
    }

    /// <summary>
    /// A landed step, and leaving the editor, drop the certificate saved for a refusal.
    /// A refusal itself keeps it: that is the re-check window.
    /// </summary>
    private static void SectionStep_LandedOrExit_ClearsPriorCertificate()
    {
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int heldCalls = 0;
        using var controller = new WorkbenchController(sectionAssessmentGate: generation =>
        {
            if (generation < 1) return Task.CompletedTask;
            Interlocked.Increment(ref heldCalls);
            return held.Task;
        });
        try
        {
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var nose = controller.SectionCurve(SurfaceSide.Upper)!.Points[0];
            var refused = controller.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, nose.Id, true));
            try
            {
                Wait(refused);
                throw new Exception("The nose step landed");
            }
            catch (ContractError) { }
            WaitFor(() => controller.Section?.Assessment is not null);
            if (SavedCertificate(controller) is null)
                throw new Exception("A refusal dropped the certificate its re-check still needs");
            controller.CancelSection();
            if (controller.Section is not null || SavedCertificate(controller) is not null)
                throw new Exception("Leaving the section editor kept the saved certificate");
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var landed = controller.ApplySectionStepAsync(Raise(controller));
            WaitFor(() => Volatile.Read(ref heldCalls) > 0);
            if (SavedCertificate(controller) is not null)
                throw new Exception("A landed step kept the certificate from before the bytes changed");
            held.TrySetResult(true);
            Wait(landed);
        }
        finally { held.TrySetResult(true); }
    }

    private static object? SavedCertificate(WorkbenchController controller) =>
        typeof(WorkbenchController).GetField("sectionBeforeChecking", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(controller);

    // Readiness only: checks moved out of the fast ring (round-oct06 SPL, Ruling 123); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        DesktopChecks.Check("SectionStep_Refused_RestoresCertificate", SectionStep_Refused_RestoresCertificate);
    }

    public static void Run()
    {
        DesktopChecks.Check("SectionStep_LandedOrExit_ClearsPriorCertificate", SectionStep_LandedOrExit_ClearsPriorCertificate);
        DesktopChecks.Check("SectionMode_Crossing_FinishDisabledWithReason", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, -.3)));
            if (controller.Section?.Assessment?.Code != "DSL-PROFILE-CROSS" || controller.Section.CanFinish ||
                !controller.Section.FinishReason!.Contains("surfaces cross", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Crossing did not block Finish with COPY-123: {controller.Section?.Assessment?.Status}/{controller.Section?.Assessment?.Code}/{controller.Section?.FinishReason}");
        });

        DesktopChecks.Check("SectionAssess_DisplayCrossingVsCertificate_FinishFollowsCertificate", () =>
        {
            using var certifiedController = Open();
            Wait(certifiedController.EnterSectionAsync(0, EntryOrigin.Properties));
            Wait(certifiedController.ApplySectionStepAsync(Raise(certifiedController)));
            var certified = certifiedController.Section!;
            if (certified.Assessment?.Status != GeometryStatus.Certified ||
                Sections.DisplayCrossing(certified.Draft.Bytes, 0) is not null)
                throw new Exception("Certified fixture is not display clear");

            using var crossingController = Open();
            Wait(crossingController.EnterSectionAsync(0, EntryOrigin.Properties));
            var point = crossingController.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            Wait(crossingController.ApplySectionStepAsync(
                new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, -.3)));
            var crossing = crossingController.Section!;
            if (crossing.Assessment?.Code != "DSL-PROFILE-CROSS" ||
                Sections.DisplayCrossing(crossing.Draft.Bytes, 0) is null)
                throw new Exception("Crossing fixture lacks display and certificate crossing");

            // Inject the two possible disagreements at the mode boundary. The display bytes
            // determine only the marker; the assessment determines Finish availability.
            var displayCrossingCertified = certified with { Draft = certified.Draft with { Bytes = crossing.Draft.Bytes } };
            var displayClearInvalid = crossing with { Draft = crossing.Draft with { Bytes = certified.Draft.Bytes } };
            if (Sections.DisplayCrossing(displayCrossingCertified.Draft.Bytes, 0) is null || !displayCrossingCertified.CanFinish)
                throw new Exception("Display crossing overrode a certified Finish");
            if (Sections.DisplayCrossing(displayClearInvalid.Draft.Bytes, 0) is not null || displayClearInvalid.CanFinish)
                throw new Exception("Clear display overrode a crossing certificate");
        });

        DesktopChecks.Check("SectionMode_NotAssessed_FinishDisabledWithReason", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            Wait(controller.ApplySectionStepAsync(Raise(controller), cancelled.Token));
            if (controller.Section?.Assessment?.Status != GeometryStatus.NotAssessed || controller.Section.CanFinish ||
                string.IsNullOrWhiteSpace(controller.Section.FinishReason))
                throw new Exception("NotAssessed allowed Finish or omitted its reason");
        });

        DesktopChecks.Check("SectionMode_WingFieldsReadOnly_Copy122", () =>
        {
            using var controller = Open();
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string source = controller.AcceptedSource;
            try { controller.ApplySpan("2000 mm"); throw new Exception("Wing edit ran in section mode"); }
            catch (ContractError error) when (error.Code == "DSL-DRAFT-OWNED") { }
            if (controller.AcceptedSource != source || controller.Status != PropertyCopy.SetInWorkspace)
                throw new Exception("Wing edit did not show COPY-122 without changing bytes");
        });

        DesktopChecks.Check("SectionMode_StaleAssessment_Dropped", () =>
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var controller = new WorkbenchController(sectionAssessmentGate: generation => generation == 1 ? gate.Task : Task.CompletedTask);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var first = controller.ApplySectionStepAsync(Raise(controller));
            var second = controller.ApplySectionStepAsync(Raise(controller, .02));
            Wait(second);
            gate.SetResult();
            Wait(first);
            if (controller.Section?.Draft.Generation != 2 || controller.Section.Assessment?.Key?.Generation != 2)
                throw new Exception("Stale assessment replaced the newer step");
            var stale = controller.LocalEvents.LastOrDefault(item => item.Operation == "section.assess" && item.Outcome == "superseded");
            if (stale is null || stale.DurationMilliseconds is not null)
                throw new Exception("Stale assessment was not recorded as superseded without duration: " +
                    string.Join(",", controller.LocalEvents.Where(item => item.Operation == "section.assess")
                        .Select(item => item.Outcome + "/" + item.DurationMilliseconds)));
        });

        DesktopChecks.Check("SectionMode_AssessCompletion_NewerStripMessageSurvives", () =>
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var controller = new WorkbenchController(sectionAssessmentGate: _ => gate.Task);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var pending = controller.ApplySectionStepAsync(Raise(controller));
            // The step applies off the UI thread (§7): the newer report is written once it has landed and its check is held.
            WaitFor(() => controller.Section!.Draft.Cursor == 1);
            long newer = controller.SupersedeStatus();
            gate.SetResult();
            Wait(pending);
            if (controller.StatusVersion != newer)
                throw new Exception("Assessment completion overwrote the newer status slot");
        });

        DesktopChecks.Check("LegacyProfileRecovery_ResumesSectionImmediately", () =>
        {
            using var source = new AuthoringSession();
            source.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
            var started = source.BeginSectionDraft(Guid.NewGuid().ToString("D"), 0);
            var point = Sections.View(started.Bytes, 0, SurfaceSide.Upper, "preview", 0).Points.Single(item => item.Id == "cv-3");
            var changed = source.ApplySectionStep(started.DraftId, started.Generation,
                new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + .01));
            source.CaptureRecovery();
            var envelope = source.Envelope();
            var legacy = envelope with { Recovery = envelope.Recovery! with
            {
                Rail = "upper", VertexId = point.Id, Profile = "section-a", Assignment = 0
            } };
            using var reopened = new AuthoringSession();
            reopened.Reopen(NativeProject.Encode(legacy));
            reopened.ResumeRecovery();
            var view = reopened.CurrentSectionDraft();
            if (view is not { Cursor: 0, StepCount: 0 } || !view.Bytes.SequenceEqual(changed.Bytes))
                throw new Exception("Legacy profile recovery did not become a section draft at cursor zero");
        });

        DesktopChecks.Check("Controller_ResumeSectionRecovery_EntersSectionMode", () =>
        {
            using var source = new AuthoringSession();
            source.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), true);
            var started = source.BeginSectionDraft(Guid.NewGuid().ToString("D"), 0);
            var changed = source.ApplySectionStep(started.DraftId, started.Generation, new SectionStep.MakeUnique());
            source.CaptureRecovery();
            string path = Path.Combine(Directory.GetCurrentDirectory(), ".tmp-tests", $"section-recovery-{Guid.NewGuid():N}.cfdw.json");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, source.SaveImage());
                using var controller = new WorkbenchController();
                Wait(controller.OpenPathAsync(path));
                controller.ResumeRecovery();
                if (controller.Section?.Draft is not { Cursor: 0, StepCount: 0 } view ||
                    view.Assignment != 0 || !view.Bytes.SequenceEqual(changed.Bytes))
                    throw new Exception("Resume did not enter section mode on recovered cursor-zero bytes");
            }
            finally { File.Delete(path); }
        });

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
            // The inner step's undo applies off the UI thread (§7), queued like a step.
            WaitFor(() => !controller.SectionStepPending);
            if (controller.Section?.Draft.Cursor != 0 || controller.AcceptedSource != source || controller.CanUndo)
                throw new Exception("Undo did not stop at the section entry");
            controller.Redo();
            WaitFor(() => !controller.SectionStepPending);
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
            string path = TestTemp.Combine($"section-{Guid.NewGuid():N}.cfdw.json");
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

        // release-freeze (§7 Concurrency, UI-LIFETIME): a step held in its apply is discarded when the section is cancelled
        // or the controller disposed before it lands. Cancel's copy stays, nothing is reported and nothing throws.
        DesktopChecks.Check("SectionMode_StepLandingAfterCancel_Discarded", () =>
        {
            using var hold = new ManualResetEventSlim(false);
            bool? onUiThread = null;
            using (var controller = new WorkbenchController(sectionStepGate: _ => { onUiThread = Dispatcher.UIThread.CheckAccess(); hold.Wait(TimeSpan.FromSeconds(5)); }))
            {
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                var step = controller.ApplySectionStepAsync(Raise(controller));
                WaitFor(() => onUiThread is not null);
                if (onUiThread == true) throw new Exception("The step applied on the UI thread");
                controller.CancelSection();
                string cancelled = controller.Status;
                hold.Set();
                Wait(step);
                Dispatcher.UIThread.RunJobs();
                if (controller.Section is not null || controller.Status != cancelled)
                    throw new Exception($"A step landing after Cancel was applied or reported: section {controller.Section is not null}, status '{controller.Status}'");
            }
            hold.Reset();
            onUiThread = null;
            var disposed = new WorkbenchController(sectionStepGate: _ => { onUiThread = Dispatcher.UIThread.CheckAccess(); hold.Wait(TimeSpan.FromSeconds(5)); });
            Wait(disposed.OpenExampleAsync());
            Wait(disposed.EnterSectionAsync(0, EntryOrigin.Properties));
            int changes = 0;
            var late = disposed.ApplySectionStepAsync(Raise(disposed));
            WaitFor(() => onUiThread is not null);
            disposed.Changed += () => changes++;
            disposed.SectionChanged += () => changes++;
            disposed.Dispose();
            hold.Set();
            Wait(late);
            Dispatcher.UIThread.RunJobs();
            if (changes != 0) throw new Exception($"A step landing after Dispose raised {changes} changes");
        });

        // release-freeze: steps asked for while one applies queue behind it and land in order; none is dropped. Undo queues
        // the same way, and Finish follows the certificate of the last landed bytes.
        DesktopChecks.Check("SectionMode_StepsWhileApplying_QueueInOrder", () =>
        {
            using var hold = new ManualResetEventSlim(false);
            int gated = 0;
            using var controller = new WorkbenchController(sectionStepGate: _ => { if (Interlocked.Increment(ref gated) == 1) hold.Wait(TimeSpan.FromSeconds(5)); });
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            var first = controller.ApplySectionStepAsync(Raise(controller, .01));
            var second = controller.ApplySectionStepAsync(Raise(controller, .02));
            var undo = controller.UndoSectionStepAsync();
            if (controller.Section!.Draft.Cursor != 0 || controller.Section.CanFinish || controller.Section.FinishReason != "Checking…")
                throw new Exception($"While held: cursor {controller.Section.Draft.Cursor}, Finish reason '{controller.Section.FinishReason}'");
            hold.Set();
            Wait(Task.WhenAll(first, second, undo));
            WaitFor(() => controller.Section!.Assessment is not null);
            var mode = controller.Section!;
            var cv3 = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            if (mode.Draft.Cursor != 1 || mode.Draft.StepCount != 2 || !mode.CanFinish)
                throw new Exception($"Queued steps landed as cursor {mode.Draft.Cursor} of {mode.Draft.StepCount}, CanFinish {mode.CanFinish}");
            var start = Sections.View(mode.BaseBytes, 0, SurfaceSide.Upper, "entry", 0).Points.Single(item => item.Id == "cv-3");
            if (Math.Abs(cv3.Ordinate - start.Ordinate - .01) > 1e-9)
                throw new Exception($"The undo did not land on the first step: cv-3 moved {cv3.Ordinate - start.Ordinate:G6}");
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

        DesktopChecks.Check("Controller_PreviewLatestWins_OlderDropped", () =>
        {
            bool? onUi = null;
            int entered = 0;
            using var held = new ManualResetEventSlim(false);
            using var release = new ManualResetEventSlim(false);
            using var controller = new WorkbenchController(previewGate: _ =>
            {
                onUi = Dispatcher.UIThread.CheckAccess();
                if (Interlocked.Increment(ref entered) == 1)
                {
                    held.Set();
                    release.Wait(TimeSpan.FromSeconds(5));
                }
            });
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-4412"));
            if (!held.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("the first preview never reached the gate");
            controller.PreviewReplace(Naca("naca-0012"));
            release.Set();
            WaitFor(() => !controller.PreviewPending && controller.PreviewLandings + controller.PreviewDrops >= 2);
            if (controller.PreviewLandings != 1 || controller.PreviewDrops < 1 || onUi != false
                || controller.PreviewSourceName != "NACA 0012" || controller.CurrentPreview is null)
                throw new InvalidOperationException(
                    $"older preview landed (landings {controller.PreviewLandings}, drops {controller.PreviewDrops}, onUi {onUi}, source {controller.PreviewSourceName})");
            int landed = controller.PreviewLandings;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            controller.PreviewReplace(Naca("naca-4412"));
            WaitFor(() => !controller.PreviewPending && controller.PreviewLandings > landed);
            Console.WriteLine(
                $"MEASURE catalog.preview naca-4412 {clock.Elapsed.TotalMilliseconds:0} ms off-ui (design 140-240 under load, readiness target 50)");
        });

        DesktopChecks.Check("Controller_ApplyReplace_OneStepChipResidualStatus", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null || controller.PreviewLandings > 0);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            var mode = controller.Section ?? throw new InvalidOperationException("section closed");
            if (mode.Draft.Cursor != 1 || mode.Draft.StepCount != 1)
                throw new InvalidOperationException($"replace was not one step (cursor {mode.Draft.Cursor} of {mode.Draft.StepCount})");
            if (controller.SourceChip != "Catalog original · NACA 0012")
                throw new InvalidOperationException("chip: " + (controller.SourceChip ?? "null"));
            var import = mode.LastReport?.Import ?? throw new InvalidOperationException("no replace report");
            if (import.Stations is not { Count: 2 })
                throw new InvalidOperationException("replace stations: " + (import.Stations?.Count.ToString() ?? "none"));
            double chord = import.Stations.Max(index =>
                Placement.Frame(mode.Draft.Bytes, controller.Inspection!.Authored.Assignments[index].Eta).ChordMeters);
            double microns = import.MaxResidual * chord * 1e6;
            if (microns > 10)
                throw new InvalidOperationException($"fit {microns.ToString("0.00", Inv)} µm was over the 10 µm limit");
            string fit = microns.ToString("0.00", Inv);
            string undo = OperatingSystem.IsMacOS() ? "⌘Z" : "Ctrl+Z";
            string text = host.StatusStrip.Text ?? "";
            string expected = $"Replaced Root and Tip with NACA 0012. Fit {fit} µm (limit 10 µm). {undo} puts the old section back.";
            if (text != expected)
                throw new InvalidOperationException("status: " + text);
        });

        DesktopChecks.Check("Controller_SaveMine_DraftDocumentUndoUnchanged", () =>
        {
            string root = BindRoot();
            try
            {
                using var controller = new WorkbenchController();
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                var mode = controller.Section!;
                byte[] bytes = mode.Draft.Bytes.ToArray();
                long generation = mode.Draft.Generation;
                int cursor = mode.Draft.Cursor;
                int steps = mode.Draft.StepCount;
                bool undo = controller.CanUndo;
                Wait(controller.SaveToMySectionsAsync("NACA kept"));
                mode = controller.Section!;
                bool same = mode.Draft.Bytes.AsSpan().SequenceEqual(bytes)
                    && mode.Draft.Generation == generation
                    && mode.Draft.Cursor == cursor
                    && mode.Draft.StepCount == steps
                    && controller.CanUndo == undo;
                bool listed = controller.OpenCatalog().Mine.Any(entry => entry.Name == "NACA kept");
                if (!listed)
                    throw new InvalidOperationException("Save did not publish \"NACA kept\" (draft unchanged " + same + ")");
                if (!same)
                    throw new InvalidOperationException("Save changed the draft or the undo depth");
                controller.CancelSection();
                if (controller.CanUndo)
                    throw new InvalidOperationException("Save added a document undo step");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_SaveMine_SurvivesSectionCancelAndDocumentUndo", () =>
        {
            string root = BindRoot();
            try
            {
                using var controller = new WorkbenchController();
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                Wait(controller.SaveToMySectionsAsync("Kept section"));
                controller.CancelSection();
                if (!controller.OpenCatalog().Mine.Any(entry => entry.Name == "Kept section"))
                    throw new InvalidOperationException("Save did not survive Cancel");
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                Wait(controller.ApplySectionStepAsync(Raise(controller)));
                WaitFor(() => controller.Section!.Assessment?.Status == GeometryStatus.Certified);
                if (!controller.Section!.CanFinish)
                    throw new InvalidOperationException("moved section did not certify: " + controller.Section.FinishReason);
                Wait(controller.FinishSectionAsync());
                if (!controller.CanUndo) throw new InvalidOperationException("Finish did not add a document undo");
                controller.Undo();
                if (!controller.OpenCatalog().Mine.Any(entry => entry.Name == "Kept section"))
                    throw new InvalidOperationException("Document undo removed the saved section");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_SaveMine_ListedAfterOpeningAnotherFoil", () =>
        {
            string root = BindRoot();
            try
            {
                using (var first = new WorkbenchController())
                {
                    Wait(first.OpenExampleAsync());
                    Wait(first.EnterSectionAsync(0, EntryOrigin.Properties));
                    Wait(first.SaveToMySectionsAsync("From the first foil"));
                }
                using var second = new WorkbenchController();
                var opening = second.NewFoilAsync();
                Wait(opening);
                if (opening.Result is not OpenOutcome.Opened)
                    throw new InvalidOperationException("the second foil did not open: " + opening.Result);
                if (!second.OpenCatalog().Mine.Any(entry => entry.Name == "From the first foil"))
                    throw new InvalidOperationException("My sections did not list the saved section after opening another foil");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Commands_SectionMenu_ReplaceSaveImportRowsRun", () =>
        {
            (string Id, string Title)[] rows =
            [
                ("section.replace-catalog", "Replace from catalog…"),
                ("section.save-mine", "Save to My sections…"),
                ("section.import-dat", "Import .dat…")
            ];
            string[] missing = rows.Where(row => !CommandTable.Rows.Any(command =>
                command.Menu == CommandTable.SectionMenu && command.Id == row.Id && command.Title == row.Title))
                .Select(row => row.Id).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("section menu row missing: " + string.Join(", ", missing));

            string root = BindRoot();
            try
            {
                var entry = Naca("naca-0012").Entry;
                string path = Path.Combine(root, "naca0012.dat");
                File.WriteAllBytes(path, entry.Coordinates!);
                using var controller = new WorkbenchController();
                var host = new ShellHost(controller, pickOpenFile: () => Task.FromResult<string?>(path));
                host.AskSaveName = () => Task.FromResult<string?>("Menu save");
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
                Wait(host.RunCommand("section.replace-catalog"));
                if (host.StatusStrip.Text is not { } opened || !opened.StartsWith("Catalog open: ", StringComparison.Ordinal))
                    throw new InvalidOperationException("catalog command: " + host.StatusStrip.Text);
                Wait(host.RunCommand("section.save-mine"));
                if (!controller.OpenCatalog().Mine.Any(item => item.Name == "Menu save"))
                    throw new InvalidOperationException("Save to My sections did not write the named section");
                int cursor = controller.Section!.Draft.Cursor;
                Wait(host.RunCommand("section.import-dat"));
                var mode = controller.Section!;
                if (mode.Draft.Cursor != cursor + 1 || mode.LastReport?.Kind != "replace")
                    throw new InvalidOperationException($"import did not Replace (cursor {mode.Draft.Cursor}, kind {mode.LastReport?.Kind})");
                if (host.StatusStrip.Text is not { } replaced || !replaced.Contains("µm", StringComparison.Ordinal)
                    || replaced.Contains("Imported the .dat", StringComparison.Ordinal))
                    throw new InvalidOperationException("import status: " + host.StatusStrip.Text);
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_RefusedPreview_ClearsAndShowsCopy", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            byte[] before = controller.Section!.Draft.Bytes.ToArray();
            int cursor = controller.Section.Draft.Cursor;
            controller.PreviewReplace(Disabled("naca-16-012"));
            WaitFor(() => !controller.PreviewPending);
            string copy = "This section has no coordinates in this build. Nothing changed.";
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("a disabled row left the canvas preview up");
            if (host.StatusStrip.Text != copy)
                throw new InvalidOperationException("refusal copy: " + host.StatusStrip.Text);
            ExpectRefusal(controller.ApplyReplaceAsync(ReplaceScope.Draft), "CAT-NOT-ADMITTED");
            if (controller.Section!.Draft.Cursor != cursor || !controller.Section.Draft.Bytes.AsSpan().SequenceEqual(before))
                throw new InvalidOperationException("a refused preview applied a section step");
        });

        DesktopChecks.Check("Controller_SpacingRefusal_ClearsPreview", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            Wait(controller.ApplySectionStepAsync(new SectionStep.MakeUnique()));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            byte[] before = controller.Section!.Draft.Bytes.ToArray();
            int cursor = controller.Section.Draft.Cursor;
            controller.PreviewReplace(Naca("naca-4412"));
            WaitFor(() => !controller.PreviewPending && controller.PreviewLandings + controller.PreviewDrops >= 2);
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("a CAT-SPACING preview stayed on the canvas");
            if (host.StatusStrip.Text is not { } text || !text.StartsWith("Root blends point-to-point with Tip", StringComparison.Ordinal))
                throw new InvalidOperationException("spacing copy: " + host.StatusStrip.Text);
            ExpectRefusal(controller.ApplyReplaceAsync(ReplaceScope.Draft), "CAT-NOT-ADMITTED");
            if (controller.Section!.Draft.Cursor != cursor || !controller.Section.Draft.Bytes.AsSpan().SequenceEqual(before))
                throw new InvalidOperationException("a spacing refusal applied a section step");
        });

        DesktopChecks.Check("Controller_PreviewCleared_OnExitAndApply", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.EnterSectionAsync(1, EntryOrigin.Properties));
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("entering another profile left the preview up");

            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            controller.CancelSection();
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("Cancel left the preview up");

            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("Apply left the preview up");
            WaitFor(() => controller.Section!.CanFinish);
            Wait(controller.FinishSectionAsync());
            if (controller.CurrentPreview is not null || controller.PreviewChoice is not null)
                throw new InvalidOperationException("Finish left the preview up");
        });

        DesktopChecks.Check("Controller_StaleApply_RefusesDslStale", () =>
        {
            using var held = new ManualResetEventSlim(false);
            using var release = new ManualResetEventSlim(false);
            int entered = 0;
            using var controller = new WorkbenchController(previewGate: _ =>
            {
                if (Interlocked.Increment(ref entered) == 1)
                {
                    held.Set();
                    release.Wait(TimeSpan.FromSeconds(5));
                }
            });
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            if (!held.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("the preview never reached the gate");
            Wait(controller.ApplySectionStepAsync(Raise(controller)));
            release.Set();
            WaitFor(() => !controller.PreviewPending);
            if (controller.CurrentPreview is not null || controller.PreviewLandings != 0)
                throw new InvalidOperationException(
                    $"a preview landed after a step during the compute (landings {controller.PreviewLandings})");

            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            byte[] before = controller.Section!.Draft.Bytes.ToArray();
            int cursor = controller.Section.Draft.Cursor;
            Wait(controller.ApplySectionStepAsync(Raise(controller, .02)));
            ExpectRefusal(controller.ApplyReplaceAsync(ReplaceScope.Draft), "DSL-STALE");
            if (controller.Section!.Draft.Cursor != cursor + 1 || controller.Section.Draft.Bytes.AsSpan().SequenceEqual(before))
                throw new InvalidOperationException("the stale apply changed the draft, or the step did not");
        });

        DesktopChecks.Check("Controller_ReplaceChain_ReplacesEveryStation", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            Wait(controller.ApplySectionStepAsync(new SectionStep.MakeUnique()));
            controller.PreviewReplace(Naca("naca-4412"), ReplaceScope.BlendChain);
            WaitFor(() => controller.CurrentPreview is not null);
            if (controller.CurrentPreview!.Stations.Count != 2)
                throw new InvalidOperationException("chain preview stations: " + controller.CurrentPreview.Stations.Count);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.BlendChain));
            var names = FoilSource.Parse(controller.Section!.Draft.Bytes).Authored().Assignments
                .Select(item => item.ProfileName).Distinct().ToArray();
            if (names.Length != 1)
                throw new InvalidOperationException("chain replace left profiles " + string.Join(", ", names));
            var replace = SessionOf(controller).ReadLocalEvents().LastOrDefault(item => item.Operation == "section.step" && item.Replace is not null);
            if (replace?.Replace?.Scope != "chain")
                throw new InvalidOperationException("section.step scope: " + replace?.Replace?.Scope);
        });

        DesktopChecks.Check("Controller_PreviewChanged_OnUiThread", () =>
        {
            bool? onUi = null;
            using var controller = new WorkbenchController();
            controller.PreviewChanged += () => onUi = Dispatcher.UIThread.CheckAccess();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => onUi is not null);
            if (onUi != true)
                throw new InvalidOperationException("PreviewChanged ran off the UI thread");
        });

        DesktopChecks.Check("Controller_PreviewFault_RecordsNonContractError", () =>
        {
            using var controller = new WorkbenchController(previewGate: _ => throw new InvalidOperationException("boom"));
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => !controller.PreviewPending);
            bool recorded = SessionOf(controller).ReadLocalEvents()
                .Any(item => item.Operation == "catalog.preview" && item.Outcome == "INTERNAL-ERROR");
            if (!recorded)
                throw new InvalidOperationException("a non-contract preview fault was dropped");
        });

        DesktopChecks.Check("Controller_PreviewChangedThrow_ReleasesBusy", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewChanged += () => throw new InvalidOperationException("preview subscriber");
            try
            {
                controller.PreviewReplace(Naca("naca-0012"));
                WaitFor(() => controller.PreviewLandings + controller.PreviewDrops > 0);
            }
            catch (Exception) { Dispatcher.UIThread.RunJobs(); }
            if (controller.PreviewPending)
                throw new InvalidOperationException("previewBusy stayed set after PreviewChanged threw");
        });

        DesktopChecks.Check("Controller_AsyncRefusals_DoNotThrowSynchronously", () =>
        {
            using var controller = new WorkbenchController();
            Task? apply = null;
            Exception? thrown = null;
            try { apply = controller.ApplyReplaceAsync(ReplaceScope.Draft); }
            catch (Exception error) { thrown = error; }
            if (thrown is not null || apply is null)
                throw new InvalidOperationException("ApplyReplaceAsync threw before returning a task: " + thrown?.Message);
            ExpectRefusal(apply, "CAT-NOT-ADMITTED");

            var save = typeof(WorkbenchController).GetMethod(nameof(WorkbenchController.SaveToMySectionsAsync))
                ?? throw new InvalidOperationException("SaveToMySectionsAsync is absent");
            if (!save.GetParameters().Any(item => item.ParameterType == typeof(CancellationToken)))
                throw new InvalidOperationException("SaveToMySectionsAsync does not take a CancellationToken");
            thrown = null;
            Task? saving = null;
            try { saving = controller.SaveToMySectionsAsync("Name"); }
            catch (Exception error) { thrown = error; }
            if (thrown is not null || saving is null)
                throw new InvalidOperationException("SaveToMySectionsAsync threw before returning a task: " + thrown?.Message);
            ExpectRefusal(saving, "DSL-DRAFT-OWNED");
        });

        DesktopChecks.Check("Controller_QuotedNameAndBraceProvenance_SaveReload", () =>
        {
            string root = BindRoot();
            try
            {
                byte[] foil = QuotedSection();
                using var controller = new WorkbenchController();
                Wait(controller.OpenFoilAsync(foil, "quoted"));
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                if (controller.Section!.Draft.Profile != "sec\"tion")
                    throw new InvalidOperationException("profile name: " + controller.Section.Draft.Profile);
                Wait(controller.SaveToMySectionsAsync("Quoted"));
                var mine = controller.OpenCatalog().Mine.SingleOrDefault(item => item.Name == "Quoted")
                    ?? throw new InvalidOperationException("the saved section was not listed");
                string saved = Encoding.UTF8.GetString(mine.Bytes);
                if (!saved.Contains("brace}inside", StringComparison.Ordinal) || !FoilSource.Parse(mine.Bytes).IsParsed)
                    throw new InvalidOperationException("save did not keep the provenance brace: " + saved);
                using var again = new WorkbenchController();
                var reloaded = again.OpenCatalog().Mine.SingleOrDefault(item => item.Name == "Quoted");
                if (reloaded is null || !Encoding.UTF8.GetString(reloaded.Bytes).Contains("brace}inside", StringComparison.Ordinal))
                    throw new InvalidOperationException("reload lost the provenance brace");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_CatalogTelemetry_NamedSessionRing", () =>
        {
            string root = BindRoot();
            try
            {
                using var controller = new WorkbenchController();
                Wait(controller.OpenExampleAsync());
                ShellEvents.Clear();
                var snapshot = controller.OpenCatalog();
                if (ShellEvents.Read().Any(item => item.Name is "catalog.open" or "library.scan" or "library.save"))
                    throw new InvalidOperationException("catalog events are still on the shell ring");
                var open = SessionOf(controller).ReadLocalEvents().LastOrDefault(item => item.Operation == "catalog.open");
                object? fields = open is null ? null : open.GetType().GetProperty("Catalog")?.GetValue(open);
                if (fields is null)
                    throw new InvalidOperationException("catalog.open is not a named session entry");
                int disabled = (int)fields.GetType().GetProperty("Disabled")!.GetValue(fields)!;
                int problems = (int)fields.GetType().GetProperty("Problems")!.GetValue(fields)!;
                int naca = (int)fields.GetType().GetProperty("Naca")!.GetValue(fields)!;
                if (disabled != snapshot.Disabled || problems != snapshot.Problems || naca <= 0)
                    throw new InvalidOperationException($"named fields disabled {disabled} problems {problems} naca {naca}");
                if (open!.GetType().GetProperty("PointsAfter")?.GetValue(open) is int points && points != 0)
                    throw new InvalidOperationException("catalog.open stored a count in points");

                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                ShellEvents.Clear();
                Wait(controller.SaveToMySectionsAsync("Telemetry"));
                if (ShellEvents.Read().Any(item => item.Name == "library.save"))
                    throw new InvalidOperationException("library.save is still on the shell ring");
                var saved = SessionOf(controller).ReadLocalEvents().LastOrDefault(item => item.Operation == "library.save");
                if (saved is null || saved.Outcome != "saved" || saved.GetType().GetProperty("Catalog")?.GetValue(saved) is null)
                    throw new InvalidOperationException("library.save was not a named session entry");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_LastReplaceName_KeyedByProfile", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            Wait(controller.ApplySectionStepAsync(new SectionStep.MakeUnique()));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            if (controller.LastReplaceName != "NACA 0012")
                throw new InvalidOperationException("root replace name: " + controller.LastReplaceName);
            WaitFor(() => controller.Section!.CanFinish);
            Wait(controller.FinishSectionAsync());
            Wait(controller.EnterSectionAsync(1, EntryOrigin.Properties));
            if (controller.LastReplaceName is not null)
                throw new InvalidOperationException("tip inherited the root replace name: " + controller.LastReplaceName);
        });

        DesktopChecks.Check("Controller_SaveOffUiThread_HonorsCancellation", () =>
        {
            string root = BindRoot();
            try
            {
                int? saveThread = null;
                using var controller = ControllerWithLibraryGate(() => saveThread = Environment.CurrentManagedThreadId);
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                var refused = SaveMine(controller, "Nope", cancelled.Token);
                if (refused is null)
                    throw new InvalidOperationException("SaveToMySectionsAsync returned null");
                try
                {
                    Wait(refused);
                    throw new InvalidOperationException("a cancelled save completed");
                }
                catch (OperationCanceledException) { }
                if (controller.OpenCatalog().Mine.Any(item => item.Name == "Nope"))
                    throw new InvalidOperationException("a cancelled save published");
                Wait(SaveMine(controller, "Off thread"));
                if (saveThread is null || saveThread == Environment.CurrentManagedThreadId)
                    throw new InvalidOperationException("library save ran on the UI thread (" + saveThread + ")");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_ProfileBlock_PublicWithoutDesktopGrant", () =>
        {
            var block = typeof(FoilSource).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(item => item.Name == "ProfileBlock"
                    && item.GetParameters() is [{ } parameter]
                    && parameter.ParameterType.Name == "ProfileDefinition");
            if (block is null)
                throw new InvalidOperationException("FoilSource.ProfileBlock(ProfileDefinition) is not public");
            bool grant = typeof(FoilSource).Assembly
                .GetCustomAttributes(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), false)
                .Cast<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
                .Any(item => item.AssemblyName.Split(',')[0] == "CfdWorkbench.Desktop");
            if (grant)
                throw new InvalidOperationException("Core still grants InternalsVisibleTo Desktop");
        });

        DesktopChecks.Check("Controller_SectionStep_ClearsLandedPreview", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplySectionStepAsync(Raise(controller)));
            if (controller.CurrentPreview is not null || controller.PreviewSourceName is not null)
                throw new InvalidOperationException("a landed preview stayed painted over the stepped draft");
            ExpectRefusal(controller.ApplyReplaceAsync(ReplaceScope.Draft), "DSL-STALE");
        });

        DesktopChecks.Check("Controller_CatalogTelemetry_OmitsInapplicableFields", () =>
        {
            string root = BindRoot();
            try
            {
                using var controller = new WorkbenchController(previewGate: _ => throw new InvalidOperationException("boom"));
                Wait(controller.OpenExampleAsync());
                _ = controller.OpenCatalog();
                var scan = SessionOf(controller).ReadLocalEvents().Last(item => item.Operation == "library.scan");
                if (scan.Catalog is not { } scanFields)
                    throw new InvalidOperationException("library.scan has no catalog fields");
                object? naca = Field(scanFields, "Naca");
                object? eppler = Field(scanFields, "Eppler");
                object? speer = Field(scanFields, "Speer");
                object? disabled = Field(scanFields, "Disabled");
                if (naca is not null || eppler is not null || speer is not null || disabled is not null)
                    throw new InvalidOperationException(
                        $"library.scan wrote family counts naca={naca ?? "null"} eppler={eppler ?? "null"} speer={speer ?? "null"} disabled={disabled ?? "null"}");
                if (Field(scanFields, "Count") is null || Field(scanFields, "Problems") is null || Field(scanFields, "Milliseconds") is null)
                    throw new InvalidOperationException("library.scan dropped count, problems, or milliseconds");

                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                Wait(controller.SaveToMySectionsAsync("Telemetry nulls"));
                var saved = SessionOf(controller).ReadLocalEvents().Last(item => item.Operation == "library.save");
                if (saved.Catalog is not { } saveFields || saved.Outcome != "saved" || Field(saveFields, "Milliseconds") is null)
                    throw new InvalidOperationException("library.save lost its measured outcome");
                foreach (string name in new[] { "Naca", "Eppler", "Speer", "Mine", "Disabled", "Problems", "Count" })
                {
                    if (Field(saveFields, name) is not null)
                        throw new InvalidOperationException("library.save wrote " + name + "=" + Field(saveFields, name));
                }

                controller.PreviewReplace(Naca("naca-0012"));
                WaitFor(() => !controller.PreviewPending);
                var events = SessionOf(controller).ReadLocalEvents().Where(item => item.Operation == "catalog.preview").ToArray();
                var measured = events.LastOrDefault(item => item.Replace is not null);
                var fault = events.LastOrDefault(item => item.Outcome == "INTERNAL-ERROR");
                if (measured is null)
                    throw new InvalidOperationException("a measured catalog.preview has no Replace event");
                if (measured.Catalog is not null)
                    throw new InvalidOperationException("a measured catalog.preview used the catalog-count shape");
                if (fault is null)
                    throw new InvalidOperationException("the preview fault was not recorded as catalog.preview");
                if (fault.Catalog is not null)
                    throw new InvalidOperationException("catalog.preview INTERNAL-ERROR used the catalog-count shape");
                if (fault.DurationMilliseconds is not null)
                    throw new InvalidOperationException("catalog.preview INTERNAL-ERROR recorded " + fault.DurationMilliseconds + " ms");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_SaveCancelDuringWrite_IsCanceledNotFaulted", () =>
        {
            // The gate runs after SaveToMySectionsAsync has started and before library.Save.
            // A cancel there must cancel the task. library.Save itself takes no token and returns
            // only after the claim publish, so a cancel that arrives once Save has started still
            // reports success: the write completed.
            string root = BindRoot();
            try
            {
                using var entered = new ManualResetEventSlim(false);
                using var release = new ManualResetEventSlim(false);
                using var controller = ControllerWithLibraryGate(() =>
                {
                    entered.Set();
                    release.Wait(TimeSpan.FromSeconds(5));
                });
                Wait(controller.OpenExampleAsync());
                Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
                using var cancel = new CancellationTokenSource();
                var saving = SaveMine(controller, "Midway", cancel.Token);
                if (!entered.Wait(TimeSpan.FromSeconds(5)))
                    throw new InvalidOperationException("the save never reached the library gate");
                cancel.Cancel();
                release.Set();
                WaitFor(() => saving.IsCompleted);
                if (saving.IsFaulted)
                    throw new InvalidOperationException("cancel during save faulted the task: " + saving.Exception!.GetBaseException().GetType().Name);
                if (!saving.IsCanceled)
                    throw new InvalidOperationException("cancel during save status: " + saving.Status);
                if (controller.OpenCatalog().Mine.Any(item => item.Name == "Midway"))
                    throw new InvalidOperationException("a cancelled save published");
            }
            finally
            {
                ReleaseRoot(root);
            }
        });

        DesktopChecks.Check("Controller_OpenSecondDocument_ClearsSharedReplaceName", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            string profile = controller.Section!.Draft.Profile;
            controller.PreviewReplace(Naca("naca-0012"));
            WaitFor(() => controller.CurrentPreview is not null);
            Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
            if (controller.LastReplaceName is null)
                throw new InvalidOperationException("the first document did not record a replace name");
            WaitFor(() => controller.Section!.CanFinish);
            Wait(controller.FinishSectionAsync());
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
            if (controller.Section!.Draft.Profile != profile)
                throw new InvalidOperationException("second document profile " + controller.Section.Draft.Profile + " does not share " + profile);
            if (controller.LastReplaceName is not null)
                throw new InvalidOperationException("shared profile " + profile + " kept " + controller.LastReplaceName + " from the first document");
        });
    }

    private static object? Field(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target);

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static CatalogChoice.Catalog Disabled(string id)
    {
        var entry = Catalog.Load().Single(item => item.Id == id && item.Coordinates is null);
        return new CatalogChoice.Catalog(entry);
    }

    private static void ExpectRefusal(Task task, string code)
    {
        try { Wait(task); }
        catch (ContractError error) when (error.Code == code) { return; }
        catch (Exception error)
        {
            throw new InvalidOperationException("expected " + code + " but got " + error.GetType().Name + ": " + error.Message);
        }
        throw new InvalidOperationException("expected " + code + " but the task completed");
    }

    private static AuthoringSession SessionOf(WorkbenchController controller) =>
        (AuthoringSession)typeof(WorkbenchController).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(controller)!;

    private static byte[] QuotedSection()
    {
        string text = File.ReadAllText("src/CfdWorkbench.Desktop/Assets/example.foil");
        text = text.Replace("profile \"section-a\"", "profile \"sec\\\"tion\"", StringComparison.Ordinal);
        int lower = text.LastIndexOf("lower cv {", StringComparison.Ordinal);
        int close = text.IndexOf("\n    }", lower, StringComparison.Ordinal);
        text = text.Insert(close, "\n      provenance \"brace}inside\"");
        return Encoding.UTF8.GetBytes(text);
    }

    private static WorkbenchController ControllerWithLibraryGate(Action gate)
    {
        var ctor = typeof(WorkbenchController).GetConstructors()
            .FirstOrDefault(item => item.GetParameters().Any(parameter => parameter.Name == "libraryGate"));
        if (ctor is null)
            throw new InvalidOperationException("library save ran on the UI thread (no libraryGate)");
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Name == "libraryGate") args[i] = gate;
            else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
        }
        return (WorkbenchController)ctor.Invoke(args)!;
    }

    private static Task SaveMine(WorkbenchController controller, string name, CancellationToken cancellation = default)
    {
        var save = typeof(WorkbenchController).GetMethod(nameof(WorkbenchController.SaveToMySectionsAsync))
            ?? throw new InvalidOperationException("SaveToMySectionsAsync is absent");
        var parameters = save.GetParameters();
        if (!parameters.Any(item => item.ParameterType == typeof(CancellationToken)))
            throw new InvalidOperationException("SaveToMySectionsAsync does not take a CancellationToken");
        var args = new object?[parameters.Length];
        args[0] = name;
        for (int i = 1; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(CancellationToken)) args[i] = cancellation;
            else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
        }
        return (Task)save.Invoke(controller, args)!;
    }

    private static CatalogChoice.Catalog Naca(string id)
    {
        var entry = Catalog.Load().Single(item => item.Id == id && item.Coordinates is not null);
        return new CatalogChoice.Catalog(entry);
    }

    private static string BindRoot()
    {
        string root = TestTemp.NewDirectory("cfdw-ctl-");
        App.BindPreferenceRoot(root);
        return root;
    }

    private static void ReleaseRoot(string root)
    {
        App.ClearSectionLibrary();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
