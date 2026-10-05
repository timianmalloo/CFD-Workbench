using CfdWorkbench.Core;
using Avalonia.Controls;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>ANA-22: the area switch is a state change over the same viewport and accepted document.</summary>
public static class AnalysisToggleTests
{
    public static void Run()
    {
        DesktopChecks.Check("Toggle_RoundTrip_CameraSelectionStationViewportEqual", () =>
        {
            using var controller = Open();
            var camera = new PlanCamera(1733, 41, -23);
            controller.PlanCamera = camera;
            controller.Select(new Selection.Station(1, .5));
            var selection = controller.Selection;
            var layout = controller.Layout;
            Toggle(controller);
            Equal("Analysis", AreaMode(controller), "enter Analysis");
            Equal(camera, controller.PlanCamera, "enter camera");
            Equal(selection, controller.Selection, "enter selection");
            Equal(layout, controller.Layout, "enter layout");
            Toggle(controller);
            Equal("Workspace", AreaMode(controller), "return to CAD");
            Equal(camera, controller.PlanCamera, "return camera");
            Equal(selection, controller.Selection, "return selection");
            Equal(layout, controller.Layout, "return layout");
        });
        DesktopChecks.Check("Toggle_NeverEvaluates", () =>
        {
            using var controller = Open();
            Toggle(controller);
            Toggle(controller);
            if (controller.LocalEvents.Any(item => item.Operation == "analysis.run"))
                throw new Exception("The area switch evaluated a run without Evaluate.");
        });
        DesktopChecks.Check("Toggle_PreviewOpen_HiddenThenRestoredUntouched", () =>
        {
            using var controller = Open();
            var point = OpenPointDraft(controller);
            var draft = controller.Draft ?? throw new Exception("Point draft did not open.");
            double preview = controller.Planform!.Trailing.Points.Single(item => item.Id == point.Id).Ordinate;
            Toggle(controller);
            Equal("Analysis", AreaMode(controller), "preview hidden in Analysis");
            Equal(point.Ordinate, controller.Planform!.Trailing.Points.Single(item => item.Id == point.Id).Ordinate,
                "Analysis reads accepted geometry");
            Toggle(controller);
            Equal(draft, controller.Draft, "draft identity after return");
            Equal(preview, controller.Planform!.Trailing.Points.Single(item => item.Id == point.Id).Ordinate,
                "draft view after return");
        });
        DesktopChecks.Check("Toggle_PreviewOpen_LayersOverAcceptedRevision", () =>
        {
            using var controller = Open();
            var point = OpenPointDraft(controller);
            Toggle(controller);
            Equal(point.Ordinate, controller.CurveFor("trailing")!.Points.Single(item => item.Id == point.Id).Ordinate,
                "the layer base remains accepted while a point draft is hidden");
            if (controller.Draft is null) throw new Exception("The layer base discarded the draft.");
        });
        DesktopChecks.Check("Toggle_SectionDraftOpen_EditorRestoredOnReturn", () =>
        {
            using var controller = Open();
            Task.Run(() => controller.EnterSectionAsync(1, EntryOrigin.Side)).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            var draft = controller.Section?.Draft ?? throw new Exception("Section editor did not open.");
            var area = new ModelArea();
            area.PlanCanvas.Controller = controller;
            area.ShowFoilOpen(true);
            Equal(ModelAreaMode.Section, area.Mode, "CAD section editor before toggle");
            Toggle(controller);
            area.ShowFoilOpen(true);
            Equal(ModelAreaMode.Views, area.Mode, "Analysis views hide the editor");
            Toggle(controller);
            area.ShowFoilOpen(true);
            Equal(ModelAreaMode.Section, area.Mode, "section editor after return");
            Equal(draft, controller.Section?.Draft, "section draft after return");
        });
        DesktopChecks.Check("Analysis_EditVerb_RefusedWithInertMessage", () =>
        {
            using var controller = Open();
            var point = controller.Planform!.Trailing.Points[2];
            var before = controller.AcceptedSource;
            Toggle(controller);
            controller.Select(new Selection.Points([new PointRef("trailing", point.Id)]));
            if (controller.BeginGesture(new PointRef("trailing", point.Id), GestureInput.Typed))
                throw new Exception("A Points-pane gesture entered in Analysis.");
            var outcome = controller.ApplySpanAsync("1400").GetAwaiter().GetResult();
            if (outcome is CommitOutcome.Committed || controller.AcceptedSource != before)
                throw new Exception("A direct dimension edit changed the accepted revision in Analysis.");
            if (!controller.Status.Contains("Points are edited in CAD", StringComparison.Ordinal))
                throw new Exception("The inert point refusal was not reported.");
        });
    }

    private static PointView OpenPointDraft(WorkbenchController controller)
    {
        var point = controller.Planform!.Trailing.Points[2];
        if (!controller.BeginGesture(new PointRef("trailing", point.Id), GestureInput.Pointer))
            throw new Exception("Could not start the point draft.");
        controller.UpdateGesture(point.SpanMeters, point.Ordinate + .005);
        controller.FlushGestureFrame();
        if (controller.Draft is null) throw new Exception("The gesture never opened a draft.");
        return point;
    }

    private static WorkbenchController Open()
    {
        var controller = new WorkbenchController();
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static void Toggle(WorkbenchController controller) =>
        (typeof(WorkbenchController).GetMethod("ToggleAnalysis") ?? throw new Exception("The Analysis toggle is absent."))
        .Invoke(controller, null);

    private static string AreaMode(WorkbenchController controller) =>
        (typeof(WorkbenchController).GetProperty("AreaMode") ?? throw new Exception("The area mode is absent."))
        .GetValue(controller)?.ToString() ?? "";

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }
}
