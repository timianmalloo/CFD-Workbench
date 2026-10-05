using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// HIST: the projection feed follows the selected run's own revision (design §3.4, §18.5 rows 13-16 and 22): a geometry-Historical
/// run keeps its verdicts, stations, root t/c and strip normals; a run whose revision the session does not hold reads
/// Unavailable with the reason; a failed Evaluate keeps the previous Completed run's feed; a layer toggle raises LayersChanged.
/// Ring 0, ~4 s (two small evaluations).
/// </summary>
public static class AnalysisHistoricalFeedTests
{
    public static void Run()
    {
        DesktopChecks.Check("HistFeed_GeometryHistorical_KeepsOwnVerdictsStationsRootAndNormals", () =>
        {
            using var controller = Open(new ProductWingMethod(Small()));
            var run = Evaluate(controller, 2);
            var before = controller.AnalysisView;
            var notes = StripNotes(before);
            var stations = Stations(before);
            string root = Row(before, "Loads", "t/c (root)").Value;
            var normals = Normals(before);
            Equal(true, normals.All(vec => vec is not null), "normals derived while Current");
            Equal(true, notes.Where(note => note != Labels.TipNotJudged).All(note => note.StartsWith("Inside", StringComparison.Ordinal)), "judged while Current");
            var edited = Task.Run(() => controller.ApplySpanAsync("1400")).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (edited is not CommitOutcome.Committed) throw new Exception("Fixture geometry edit was refused.");
            var after = controller.AnalysisView;
            Equal(RunState.Historical, after.State, "state");
            Equal(true, StripNotes(after).SequenceEqual(notes), "verdicts are the run's own, not Unavailable");
            Equal(true, Stations(after).SequenceEqual(stations), "stations are the run's own revision, not the edited span");
            Equal(root, Row(after, "Loads", "t/c (root)").Value, "root t/c");
            Equal(true, Normals(after).SequenceEqual(normals), "strip normals are the run's own revision");
            // The discriminator: the edited revision's own derivation differs, so equality above is not vacuous.
            var currentNormals = MethodRecord.DeriveNormals(run, FoilSource.PatchSpan(CfdWorkbench.Cli.Cli.ExampleBytes(), "1400"));
            Equal(true, currentNormals is not null && !currentNormals.SequenceEqual(normals.Select(vec => vec!.Value)), "the edit changes the normals");
        });
        DesktopChecks.Check("HistFeed_RevisionNotHeld_UnavailableWithReasonNeverTheCurrentRevision", () =>
        {
            using var controller = Open(new ProductWingMethod(Small()));
            var run = Evaluate(controller, 2);
            var current = new SessionView("accepted-other", "", "", [1, 2, 3], null, null, false);
            var feed = WorkbenchController.DeriveFeed(run, current, _ => null);
            Equal(true, feed.Verdicts is null && feed.Stations is null && feed.RootThicknessRatio is null && feed.StripNormals is null, "nothing derived");
            Equal(Labels.FeedRevisionNotHeld, feed.Unavailable, "reason");
            var held = WorkbenchController.DeriveFeed(run, current, id => id == run.Inputs.AcceptedId ? CfdWorkbench.Cli.Cli.ExampleBytes() : null);
            Equal(true, held.Verdicts is { Count: > 0 } && held.Stations is { Count: > 0 } && held.StripNormals is { Count: > 0 } && held.Unavailable is null,
                "control: a held revision derives the feed");
        });
        DesktopChecks.Check("HistFeed_FailedEvaluate_PreviousCompletedKeepsItsVerdicts", () =>
        {
            var method = new FailableMethod(new ProductWingMethod(Small()));
            using var controller = Open(method);
            _ = Evaluate(controller, 2);
            var notes = StripNotes(controller.AnalysisView);
            method.FailNext = true;
            var failed = Evaluate(controller, 3);
            Equal(true, failed.Outcome is RunOutcome.Failed, "fixture failed");
            var view = controller.AnalysisView;
            Equal(RunState.Failed, view.State, "state");
            Equal(true, StripNotes(view).SequenceEqual(notes), "the previous run's verdicts survive the failed attempt");
            Equal(true, Normals(view).All(vec => vec is not null), "and its strip normals");
        });
        DesktopChecks.Check("HistFeed_SetLayerVisible_RaisesLayersChangedOncePerChange", () =>
        {
            using var controller = Open(new ProductWingMethod(Small()));
            int raised = 0;
            controller.LayersChanged += () => raised++;
            controller.SetLayerVisible("plan-gamma", false);
            Equal(1, raised, "hide");
            controller.SetLayerVisible("plan-gamma", false);
            Equal(1, raised, "no change, no event");
            controller.SetLayerVisible("plan-gamma", true);
            Equal(2, raised, "show");
        });
    }

    private static RunSettings Small() => Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };

    private static WorkbenchController Open(IWingMethod method)
    {
        var controller = new WorkbenchController(analysisMethod: method);
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double alpha) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, 0.6), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()
        ?? throw new Exception("Fixture Evaluate returned no run.");

    private static string[] StripNotes(AnalysisViewModel view) =>
        view.Groups.Single(group => group.Title == "Strips").Rows.Select(row => row.Note ?? "").ToArray();

    private static double[] Stations(AnalysisViewModel view) =>
        view.Layers.Single(layer => layer.Id == "depth-band").Samples.Select(sample => sample.Y).ToArray();

    private static Loads.Vec?[] Normals(AnalysisViewModel view) =>
        view.Layers.Single(layer => layer.Id == "strip-lift").Samples.Select(sample => sample.Normal).ToArray();

    private static ResultRow Row(AnalysisViewModel view, string group, string label) =>
        view.Groups.Single(item => item.Title == group).Rows.Single(row => row.Label == label);

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }

    /// <summary>The product method, with one solve that fails on request (a recorded Failed row).</summary>
    private sealed class FailableMethod(IWingMethod inner) : IWingMethod
    {
        public bool FailNext { get; set; }
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            if (!FailNext) return inner.Solve(sections, op, water, cancellation);
            FailNext = false;
            throw new ContractError("ANA-HIST-FIXTURE", "fixture solve failure");
        }
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
            WaterRecord water, CancellationToken cancellation) => inner.Couple(sections, solution, op, water, cancellation);
    }
}
