using CfdWorkbench.Core;
using Avalonia.Controls;
using Avalonia;
using CfdWorkbench.Analysis;
using CfdWorkbench.Persistence;
using CfdWorkbench.Desktop.Shell;
using System.Text.Json;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>ANA-22: the area switch is a state change over the same viewport and accepted document.</summary>
public static class AnalysisToggleTests
{
    public static void RunReadiness()
    {
        DesktopChecks.Check("Toggle_LayersFirstFrame_P95WithinPreviewBudget", () =>
        {
            using var controller = OpenSmallAnalysis();
            _ = Evaluate(controller);
            var samples = new List<double>();
            for (int i = 0; i < 20; i++)
            {
                long started = System.Diagnostics.Stopwatch.GetTimestamp();
                controller.ToggleAnalysis();
                if (controller.LayerSet.Count == 0)
                    throw new Exception("The first Analysis frame has no selected-run layers.");
                samples.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                controller.ToggleAnalysis();
            }
            samples.Sort();
            double p95 = samples[(int)Math.Ceiling(samples.Count * .95) - 1];
            Console.WriteLine("COST Toggle_LayersFirstFrame_P95WithinPreviewBudget " +
                p95.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            if (p95 > 250) throw new Exception($"Toggle p95 {p95:F3} ms exceeds 250 ms.");
        });
    }

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
            var threeD = controller.Camera3d;
            var side = controller.CameraFor(SingleView.Side);
            Toggle(controller);
            Equal("Analysis", AreaMode(controller), "enter Analysis");
            Equal(camera, controller.PlanCamera, "enter camera");
            Equal(selection, controller.Selection, "enter selection");
            Equal(layout, controller.Layout, "enter layout");
            Equal(threeD, controller.Camera3d, "enter 3D camera");
            Equal(side, controller.CameraFor(SingleView.Side), "enter elevation camera");
            Toggle(controller);
            Equal("Workspace", AreaMode(controller), "return to CAD");
            Equal(camera, controller.PlanCamera, "return camera");
            Equal(selection, controller.Selection, "return selection");
            Equal(layout, controller.Layout, "return layout");
            Equal(threeD, controller.Camera3d, "return 3D camera");
            Equal(side, controller.CameraFor(SingleView.Side), "return elevation camera");
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
            var hiddenRelease = Task.Run(() => controller.EndGestureAsync(GestureEnd.Release))
                .WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (hiddenRelease is GestureOutcome.Committed || controller.Draft is null)
                throw new Exception("A held point gesture committed while its draft was hidden in Analysis.");
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
            if (!Need<Control>(area, "SectionModeEditor").IsVisible) throw new Exception("CAD section editor is not rendered.");
            Toggle(controller);
            area.ShowFoilOpen(true);
            Equal(ModelAreaMode.Views, area.Mode, "Analysis views hide the editor");
            if (Need<Control>(area, "SectionModeEditor").IsVisible) throw new Exception("Analysis still renders the editor.");
            Toggle(controller);
            area.ShowFoilOpen(true);
            Equal(ModelAreaMode.Section, area.Mode, "section editor after return");
            if (!Need<Control>(area, "SectionModeEditor").IsVisible) throw new Exception("The editor did not render on return.");
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
            if (typeof(CurvePointLayer).GetMethod("DrawPoints")?.GetParameters().LastOrDefault()?.ParameterType != typeof(bool))
                throw new Exception("CurvePointLayer has no Analysis dimming input from the views.");
        });
        DesktopChecks.Check("ConditionsBand_Running_EvaluateBecomesCancelAnnounced", () =>
        {
            var band = NewBand();
            Call(band, "ShowRunState", RunState.Running);
            var button = Need<Button>(band, "EvaluateButton");
            Equal("Cancel", button.Content?.ToString(), "running action");
            Call(band, "ShowRunState", RunState.Current);
            Equal("Evaluate", button.Content?.ToString(), "completed action");
        });
        DesktopChecks.Check("ConditionsBand_At1024_MoreHoldsDerivedExceptQAndSigma", () =>
        {
            var band = NewBand();
            Call(band, "SetAvailableWidth", 1024d);
            if (!Need<Button>(band, "MoreButton").IsVisible ||
                !Need<TextBlock>(band, "DerivedQ").IsVisible || !Need<TextBlock>(band, "DerivedSigma").IsVisible ||
                Need<TextBlock>(band, "DerivedRe").IsVisible)
                throw new Exception("The 1024 px band did not keep q and σ while moving the other derived cells to More.");
        });
        DesktopChecks.Check("ConditionsBand_CliRunKey_EqualsAnalyse", () =>
        {
            var band = NewBand();
            Need<TextBox>(band, "SpeedInput").Text = "5.14444";
            Need<TextBox>(band, "DepthInput").Text = "0.5";
            Need<TextBox>(band, "AlphaInput").Text = "3";
            var op = (OperatingPoint)(band.GetType().GetMethod("BuildOperatingPoint")?.Invoke(band, null)
                ?? throw new Exception("The band has no operating point builder."));
            var water = (WaterRecord)(band.GetType().GetMethod("BuildWater")?.Invoke(band, null)
                ?? throw new Exception("The band has no water builder."));
            var method = new ProductWingMethod(Settings.Default with
                { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null });
            using var output = new StringWriter();
            int exit = Task.Run(() => CfdWorkbench.Cli.Cli.RunAsync(
                ["analyse", "example", "--op", "{\"speed\":5.14444,\"alphaDeg\":3,\"hRef\":0.5}"],
                output, analysis: new CfdWorkbench.Cli.AnalysisHost(method, WaterTable.At)))
                .WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (exit != 0) throw new Exception("CLI analysis failed: " + output);
            using var json = JsonDocument.Parse(output.ToString());
            string? cliKey = json.RootElement.GetProperty("run").GetProperty("runKey").GetString();
            using var session = new AuthoringSession();
            session.Open(CfdWorkbench.Cli.Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
            var bandKey = Freshness.CurrentKey(Freshness.Current(session.Snapshot(), water, op,
                method.Method, method.Settings));
            Equal(cliKey, bandKey, "CLI and conditions band run keys");
        });
        DesktopChecks.Check("StatusStrip_AnalysisItem_FollowsRunState", () =>
        {
            var strip = new StatusStrip();
            Call(strip, "ShowAnalysisState", RunState.Historical);
            Equal("Analysis: Historical", Need<TextBlock>(strip, "AnalysisItemText").Text, "Historical item");
            Call(strip, "ShowAnalysisState", RunState.Current);
            Equal("Analysis: Current", Need<TextBlock>(strip, "AnalysisItemText").Text, "Current item");
            Call(strip, "ShowAnalysisState", RunState.Unavailable);
            Equal("Analysis: Unavailable", Need<TextBlock>(strip, "AnalysisItemText").Text, "Unavailable item");
        });
        DesktopChecks.Check("Toggle_HistoricalRun_BannerInBothModes", () =>
        {
            using var controller = OpenSmallAnalysis();
            var run = Evaluate(controller);
            if (run.Outcome is not RunOutcome.Completed) throw new Exception("Fixture run did not complete.");
            var edited = Task.Run(() => controller.ApplySpanAsync("1400"))
                .WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (edited is not CommitOutcome.Committed) throw new Exception("Fixture geometry edit was refused.");
            string? cad = controller.AnalysisView.Banner;
            var area = new ModelArea();
            area.PlanCanvas.Controller = controller;
            area.ShowFoilOpen(true);
            if (controller.AnalysisView.State != RunState.Historical || cad is null ||
                !cad.Contains("geometry changed (r1 → r2)", StringComparison.Ordinal))
                throw new Exception("CAD did not show the Historical revision transition: " + cad);
            Equal(cad, Need<TextBlock>(area, "HistoricalBannerText").Text, "CAD banner copy");
            if (!Need<Control>(area, "HistoricalBanner").IsVisible) throw new Exception("CAD banner is not rendered.");
            Toggle(controller);
            area.ShowFoilOpen(true);
            Equal(cad, controller.AnalysisView.Banner, "Historical banner in Analysis");
            Equal(cad, Need<TextBlock>(area, "HistoricalBannerText").Text, "Analysis banner copy");
            if (!Need<Control>(area, "HistoricalBanner").IsVisible) throw new Exception("Analysis banner is not rendered.");
        });
        DesktopChecks.Check("Telemetry_AnalysisProject_FreshnessOnRebuild", () =>
        {
            using var controller = OpenSmallAnalysis();
            _ = Evaluate(controller);
            _ = controller.AnalysisView;
            controller.SetAnalysisConditions(OperatingPoints.Custom(5.14, 3, null), controller.AnalysisWater);
            if (controller.AnalysisView.State != RunState.Historical)
                throw new Exception("The changed operating point did not make the run Historical.");
            var projected = controller.LocalEvents.LastOrDefault(item => item.Operation == "analysis.project");
            if (projected?.Analysis?.Freshness != "Historical" ||
                projected.Analysis.WhatChanged?.Contains("op.alphaDeg", StringComparison.Ordinal) != true)
                throw new Exception("The Historical rebuild omitted freshness and what changed.");
        });
        DesktopChecks.Check("Toggle_NavbarAndMenuReachable", () =>
        {
            var area = new ModelArea();
            if (area.FindControl<Avalonia.Controls.Primitives.ToggleButton>("NavAnalysisButton") is null)
                throw new Exception("The navbar has no Analysis segment.");
            var row = CommandTable.Rows.FirstOrDefault(item => item.Id == "view.analysis")
                ?? throw new Exception("View ▸ Analysis has no command row.");
            if (row.Gesture != "⇧⌘A" || NativeMenuBuilder.ParseGesture(row.Gesture) is null)
                throw new Exception("View ▸ Analysis does not carry Ruling 67's shortcut.");
            var window = new MainWindow { Width = 1024, Height = 700 };
            try
            {
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var host = (ShellHost)window.Content!;
                host.OpenPalette();
                host.PaletteSearch.Text = "Analysis";
                if (host.PaletteMatches.Count != 1 || host.PaletteMatches[0].Id != row.Id)
                    throw new Exception("The palette does not offer View ▸ Analysis.");
                host.PaletteSearch.RaiseEvent(new Avalonia.Input.KeyEventArgs
                {
                    RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
                    Source = host.PaletteSearch,
                    Key = Avalonia.Input.Key.Enter
                });
                if (!host.Controller.IsAnalysis) throw new Exception("Palette Enter did not enter Analysis.");
            }
            finally { window.Close(); }
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

    private static WorkbenchController OpenSmallAnalysis()
    {
        var settings = Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };
        var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(settings));
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, null), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult()
        ?? throw new Exception("Fixture Evaluate returned no run.");

    private static Control NewBand()
    {
        var type = typeof(ModelArea).Assembly.GetType("CfdWorkbench.Desktop.Analysis.ConditionsBand")
            ?? throw new Exception("The conditions band is absent.");
        return (Control)(Activator.CreateInstance(type) ?? throw new Exception("The conditions band could not be created."));
    }

    private static void Call(object target, string method, object argument) =>
        (target.GetType().GetMethod(method) ?? throw new Exception(method + " is absent."))
        .Invoke(target, [argument]);

    private static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? throw new Exception(name + " is absent.");

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
