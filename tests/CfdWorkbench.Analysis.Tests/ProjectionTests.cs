using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

internal static class ProjectionTests
{
    internal static void Run()
    {
        Check("Projection_SectionVsWingUnits", () => {
            var v = View(); Equal("N", Cell(v, "Wing result", "Lift L").Unit); Equal("Unavailable — no polar method installed", Cell(v, "Section (2D)", "Cl, Cd, Cm, x_tr").Value);
        });
        Check("Projection_CdZeroOrNegative_ClCdUndefined", () => {
            foreach (double downwash in new[] { 0d, 0.1 })
            { var v = View(s => s with { DownwashTrefftz = downwash }); Equal(Labels.ClCdUndefined, Cell(v, "Wing result", "CL/CD").Value); }
        });
        Check("Projection_NoRun_NoAnalysisYetNoLayers", () => {
            var (_, current) = Data(); var v = AnalysisProjection.Build(null, current, Units.Metric); Equal(RunState.NoResult, v.State);
            Equal(0, v.Layers.Count); Equal(Labels.NoResult, Cell(v, "Wing result", "Result").Value);
        });
        Check("Projection_FailedLatest_PreviousRunHistoricalWithErrorCard", () => {
            var (run, current) = Data(); var failed = Rehash(run with { Outcome = new RunOutcome.Failed("ANA-SOLVE-RESIDUAL", "residual exceeded"), Strips = [] });
            var v = AnalysisProjection.Build(failed, current, Units.Metric, new ProjectionContext(PreviousCompleted: run));
            Equal(RunState.Failed, v.State); Equal(true, v.ErrorCard!.Contains("ANA-SOLVE-RESIDUAL")); Equal(true, v.Groups.Any(g => g.Title == "Wing result"));
        });
        Check("Layers_DepthUnset_NoFreeSurfaceOrTipDepth", () => {
            var (run, _) = Data(); run = Rehash(run with { Op = run.Op with { HRef = null } });
            var v = AnalysisProjection.Build(run, Current(run), Units.Metric); Equal(false, v.Layers.Any(l => l.Id == "depth-band"));
            Equal(false, v.Groups.SelectMany(g => g.Rows).Any(r => r.Label == "Tip depth"));
        });
        Check("Provenance_RunContentHash_Shown", () => {
            var (run, _) = Data(); Equal(run.ContentHash, Cell(View(run), "Provenance", "Content hash").Value);
            Equal(false, run.ContentHash == run.RunKey);
        });
        Check("Layers_ArrowLengthProportional_NormalDirection", () => {
            var v = View(); var strip = v.Layers.Single(l => l.Id == "strip-lift");
            Equal(true, strip.Samples.All(s => s.Vector is not null && s.Value is > 0));
            Equal(true, strip.Samples[0].Vector!.Value.Z > 0);
        });
        Check("Layers_EveryVisualHasTwinAndChip", () => {
            var v = View(); Equal(true, v.Layers.All(l => l.TwinId.Length > 0)); Equal(Labels.VlmChip, Cell(v, "Wing result", "Tier").Value);
        });
        Check("Layers_UnavailableVectorNotDrawn_AbsenceStated", () => {
            var (run, _) = Data(); run = Rehash(run with { Strips = [run.Strips[0] with { J = 999 }] });
            var v = View(run); Equal(null, v.Layers.Single(l => l.Id == "strip-lift").Samples[0].Vector);
        });
        Check("Layers_MomentArc_SenseFollowsSignAboutNamedDatum", () => {
            var positive = View(); var moment = positive.Layers.Single(l => l.Id == "root-moment");
            Equal(true, moment.Legend.Contains("root plane")); Equal(true, moment.Samples[0].Vector!.Value.X > 0);
            var negative = View(s => s with { Gamma = -s.Gamma });
            Equal(true, negative.Layers.Single(l => l.Id == "root-moment").Samples[0].Vector!.Value.X < 0);
        });
        Check("Layers_Vectors_BodyFrameSignConventionLabelled", () => {
            var v = View(); Equal(true, v.Layers.Single(l => l.Id == "strip-lift").Legend.Contains("+x aft, +y starboard, +z up"));
        });
    }

    internal static (AnalysisRun Run, CurrentInputs Current) Data(Func<StripLoad, StripLoad>? change = null)
    {
        var settings = new RunSettings(2, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3);
        var inputs = new RunInputs("accepted", new string('a', 64), [new string('b', 64)], "cfdw-cv/2", "placement/1");
        var water = new WaterRecord(15, 35.16504, 1000, 1e-6, 1700, "ITTC", new string('c', 64));
        var op = OperatingPoints.Custom(5, 3, 0.5);
        var method = MethodRecord.VlmStrip.Method;
        string settingsHash = RunRecord.SettingsHash(settings);
        string key = RunRecord.Key(inputs, water, op, method, settingsHash);
        StripLoad[] strips = Enumerable.Range(0, 4).Select(j => new StripLoad(j, (j - 1.5) * 0.2, Math.Abs(j - 1.5) / 2,
            0.1, 0.01, 1, 2, 500000, 0.4, new StripValue(null, Labels.NoPolar), new StripValue(null, Labels.NoPolar),
            0, 0, 10, 0, 0, 0, -0.1)).Select(s => change?.Invoke(s) ?? s).ToArray();
        var row = new AnalysisRun(Guid.NewGuid().ToString("D"), key, "", new RunOutcome.Completed(), "vlm-strip", method,
            settings, settingsHash, inputs, water, op, new RunReference(0.08, 0.8, 0.1, "frame origin", "body; wind"),
            0.01, strips, 12, new RunPlatform("test", "test", "10"));
        row = Rehash(row);
        return (row, Current(row));
    }

    internal static AnalysisRun Rehash(AnalysisRun row) => row with { ContentHash = RunRecord.ContentHash(row) };
    internal static CurrentInputs Current(AnalysisRun row) => new(row.Inputs, row.Water, row.Op, row.Method, row.SettingsHash);
    internal static AnalysisViewModel View(AnalysisRun? run = null, ProjectionContext? context = null)
    {
        run ??= Data().Run;
        return AnalysisProjection.Build(run, Current(run), Units.Metric, context);
    }
    internal static AnalysisViewModel View(Func<StripLoad, StripLoad> change) => View(Data(change).Run);
    internal static ResultRow Cell(AnalysisViewModel view, string group, string label) =>
        view.Groups.Single(g => g.Title == group).Rows.Single(r => r.Label == label);
}
