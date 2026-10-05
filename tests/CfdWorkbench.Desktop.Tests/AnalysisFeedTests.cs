using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// CTX: the controller feeds the projection with verdicts, stations and root t/c derived on read from the run and the
/// accepted source (design §3, §5.4, §18.5 rows 14, 16, 26), and owns the per-layer visible flag (row 30). Ring 0, ~3 s.
/// </summary>
public static class AnalysisFeedTests
{
    public static void Run()
    {
        // One controller and one evaluation serve the checks in order (each Open + Evaluate costs the Desktop job ~1 s of its 43 s budget).
        var shared = new Lazy<WorkbenchController>(() =>
        {
            var controller = Open();
            _ = Evaluate(controller, 5.14, 2, 0.6);
            return controller;
        });
        try
        {
            DesktopChecks.Check("Feed_InsideStrips_OnlyTheTwoTipsReadNotJudged", () =>
            {
                var controller = shared.Value;
                var notes = StripNotes(controller);
                Equal(2, notes.Count(note => note == Labels.TipNotJudged), "tip strips not judged (Ruling 78)");
                foreach (string note in notes.Where(note => note != Labels.TipNotJudged))
                    Equal(true, note.StartsWith("Inside the method envelope", StringComparison.Ordinal), note);
                Equal(true, Envelope(controller).StartsWith("Inside the method envelope", StringComparison.Ordinal), Envelope(controller));
            });
            DesktopChecks.Check("Feed_Stations_DepthBandAndTipDepthRecorded", () =>
            {
                var view = shared.Value.AnalysisView;
                var band = view.Layers.SingleOrDefault(item => item.Id == "depth-band") ?? throw new Exception("no depth-band layer (row 16)");
                Equal(true, band.Samples.Count >= 2, "station samples");
                string tip = Row(view, "Labels", "Tip depth").Value;
                Equal(false, tip == Labels.TipDepthMissing, tip);
                double shallowest = band.Samples.Min(sample => sample.Value!.Value);
                if (shallowest > 0)
                    Equal(true, Math.Abs(double.Parse(tip, System.Globalization.CultureInfo.InvariantCulture) - shallowest) < 5e-4, tip + " vs " + shallowest);
            });
            DesktopChecks.Check("Feed_RootThickness_ReadsTheRootStationRatio", () =>
            {
                var row = Row(shared.Value.AnalysisView, "Loads", "t/c (root)");
                Equal(false, row.Value == Labels.ThicknessMissing, row.Value);
                double percent = double.Parse(row.Value, System.Globalization.CultureInfo.InvariantCulture);
                Equal(true, percent > 1 && percent < 30, "root t/c percent " + percent);
            });
            DesktopChecks.Check("Layers_Visibility_ControllerFlagFeedsProjection", () =>
            {
                var controller = shared.Value;
                Equal(true, controller.AnalysisView.Layers.All(layer => layer.Visible), "all visible at first");
                controller.SetLayerVisible("plan-gamma", false);
                var layers = controller.AnalysisView.Layers;
                Equal(false, layers.Single(layer => layer.Id == "plan-gamma").Visible, "plan-gamma hidden");
                Equal(true, layers.Where(layer => layer.Id != "plan-gamma").All(layer => layer.Visible), "others unchanged");
                Equal(false, controller.LayerSet.Single(layer => layer.Id == "plan-gamma").Visible, "LayerSet follows");
                Equal(false, controller.IsLayerVisible("plan-gamma"), "flag");
                controller.SetLayerVisible("plan-gamma", true);
                Equal(true, controller.AnalysisView.Layers.All(layer => layer.Visible), "shown again");
            });
            DesktopChecks.Check("Feed_HistoricalOperatingPoint_KeepsVerdicts", () =>
            {
                var controller = shared.Value;
                controller.SetAnalysisConditions(OperatingPoints.Custom(5.14, 3, 0.6), controller.AnalysisWater);
                Equal(RunState.Historical, controller.AnalysisView.State, "state");
                var notes = StripNotes(controller);
                Equal(2, notes.Count(note => note == Labels.TipNotJudged), "tip strips not judged");
                Equal(true, notes.Where(note => note != Labels.TipNotJudged).All(note => note.StartsWith("Inside", StringComparison.Ordinal)), "the rest are judged");
            });
            DesktopChecks.Check("Feed_OutsideStrips_LabelledOutsideAndOutlined", () =>
            {
                var controller = shared.Value;
                _ = Evaluate(controller, 5.14, 18, null);
                var notes = StripNotes(controller);
                Equal(2, notes.Count(note => note == Labels.TipNotJudged), "tip strips not judged (Ruling 78)");
                Equal(true, notes.Any(note => note.StartsWith("Outside the method envelope at this strip", StringComparison.Ordinal)), "an outside strip");
                var layer = controller.AnalysisView.Layers.Single(item => item.Id == "plan-gamma");
                Equal(true, layer.Samples.Count(sample => sample.Outside) > 0, "dashed-outline count (row 14)");
                Equal(true, layer.Samples.Where(sample => sample.Provisional).All(sample => !sample.Outside), "tip never outlined");
                Equal(true, Envelope(controller).StartsWith("Outside the method envelope", StringComparison.Ordinal), Envelope(controller));
            });
        }
        finally { if (shared.IsValueCreated) shared.Value.Dispose(); }
    }

    public static void RunReadiness()
    {
        DesktopChecks.Check("Feed_DefaultLattice_JudgesAllNonTipStrips", () =>
        {
            using var controller = new WorkbenchController(analysisMethod: new ProductWingMethod());
            Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            _ = Evaluate(controller, 5.14, 2, 0.6);
            controller.SetLayerVisible("strip-lift", false);
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            var notes = StripNotes(controller);
            double ms = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Console.WriteLine("COST Feed_DefaultLattice_JudgesAllNonTipStrips " + ms.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            controller.SetLayerVisible("plan-gamma", false);
            started = System.Diagnostics.Stopwatch.GetTimestamp();
            _ = controller.AnalysisView;
            double toggleMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Console.WriteLine("COST Feed_DefaultLattice_LayerToggleReprojection " + toggleMs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            Equal(true, toggleMs < 250, "a layer toggle re-projects inside the 250 ms preview budget (the feed is cached by run)");
            Equal(2, notes.Count(note => note == Labels.TipNotJudged), "tip strips not judged");
            Equal(126, notes.Count(note => note.StartsWith("Inside", StringComparison.Ordinal) || note.StartsWith("Outside", StringComparison.Ordinal)), "judged strips");
        });
    }

    private static WorkbenchController Open()
    {
        var settings = Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };
        var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(settings));
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double speed, double alpha, double? depth) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(speed, alpha, depth), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()
        ?? throw new Exception("Fixture Evaluate returned no run.");

    private static string[] StripNotes(WorkbenchController controller) =>
        controller.AnalysisView.Groups.Single(group => group.Title == "Strips").Rows.Select(row => row.Note ?? "").ToArray();

    private static string Envelope(WorkbenchController controller) => Row(controller.AnalysisView, "Wing result", "Envelope").Value;

    private static ResultRow Row(AnalysisViewModel view, string group, string label) =>
        view.Groups.Single(item => item.Title == group).Rows.Single(row => row.Label == label);

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }
}
