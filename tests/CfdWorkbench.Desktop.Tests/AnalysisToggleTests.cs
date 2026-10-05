using CfdWorkbench.Core;

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
    }

    private static WorkbenchController Open() => new();

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
