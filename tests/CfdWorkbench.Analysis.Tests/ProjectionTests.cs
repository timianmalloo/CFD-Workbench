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
        Check("Projection_CdiZeroOrNegative_TotalDragMissing", () => {
            foreach (double downwash in new[] { 0d, 0.1 })
            { var v = View(s => s with { DownwashTrefftz = downwash }); Equal(Loads.TotalDragReason, Cell(v, "Wing result", "CL/CD").Value); }
        });
        Check("Projection_TotalDragMissing_ClCdUnavailable", () => {
            var v = View();
            Equal(Loads.TotalDragReason, Cell(v, "Wing result", "CL/CD").Value);
        });
        Check("Projection_TrefftzLiftUsedForE", () => {
            var (run, _) = Data(s => s with { Fz = 100, DownwashTrefftz = -0.032 });
            var v = View(run);
            double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
            double dy = run.Reference.BRef / run.Strips.Count;
            double clTrefftz = run.Water.Rho * run.Op.Speed * run.Strips.Sum(s => s.Gamma * dy) / (q * run.Reference.SRef);
            double cdi = 0.5 * run.Water.Rho * run.Strips.Sum(s => s.Gamma * -s.DownwashTrefftz * dy) / (q * run.Reference.SRef);
            double expected = Trefftz.Oswald(clTrefftz, run.Reference.BRef * run.Reference.BRef / run.Reference.SRef, cdi);
            Equal(expected.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), Cell(v, "Wing result", "e (computed)").Value);
        });
        Check("Projection_ProvisionalTip_NotJudgedOutside", () => {
            var (run, _) = Data();
            run = Rehash(run with { Strips = run.Strips.Select(s => s.J == 3 ? s with { Provisional = true,
                ProvisionalReason = StripLoad.TipProvisionalReason } : s).ToArray() });
            var verdicts = Enumerable.Range(0, 4).Select(j => MethodRecord.JudgeStrip(j == 3 ? 12 : 2, 0, 0.4, 0)).ToArray();
            var v = View(run, new ProjectionContext(Verdicts: verdicts));
            string sentence = Cell(v, "Wing result", "Envelope").Value;
            Equal(false, sentence.Contains("Outside", StringComparison.Ordinal));
            Equal(true, sentence.Contains("3 strips", StringComparison.Ordinal));
            Equal(true, sentence.Contains(Labels.TipNotJudged, StringComparison.Ordinal));
            Equal(false, sentence.Contains("provisional", StringComparison.OrdinalIgnoreCase));
            Equal(false, v.Layers.Single(l => l.Id == "plan-gamma").Samples[3].Outside);
        });
        Check("Projection_NoVerdicts_NonTipStripNeverReadsTipNotJudged", () => {
            var (run, _) = Data();
            run = Rehash(run with { Strips = run.Strips.Select(s => s.J == 3 ? s with { Provisional = true,
                ProvisionalReason = StripLoad.TipProvisionalReason } : s).ToArray() });
            var v = View(run);
            var notes = v.Groups.Single(g => g.Title == "Strips").Rows.Select(r => r.Note).ToArray();
            Equal(true, notes[3] == Labels.TipNotJudged, "the tip strip");
            for (int j = 0; j < 3; j++) Equal(false, notes[j] == Labels.TipNotJudged, "strip " + j);
            var samples = v.Layers.Single(l => l.Id == "plan-gamma").Samples;
            for (int j = 0; j < 3; j++) Equal(false, samples[j].Verdict == Labels.TipNotJudged, "layer strip " + j);
            Equal(false, Cell(v, "Wing result", "Envelope").Value == Labels.TipNotJudged, "run sentence");
        });
        Check("Projection_InsideAndOutsideStrips_NeverTipNotJudged", () => {
            var (run, _) = Data();
            run = Rehash(run with { Strips = run.Strips.Select(s => s.J == 3 ? s with { Provisional = true,
                ProvisionalReason = StripLoad.TipProvisionalReason } : s).ToArray() });
            var verdicts = Enumerable.Range(0, 4).Select(j => j == 3
                ? MethodRecord.JudgeStrip(2, 0, 0.4, 0, provisional: true)
                : MethodRecord.JudgeStrip(j == 1 ? 12 : 2, 0, 0.4, 0)).ToArray();
            var v = View(run, new ProjectionContext(Verdicts: verdicts));
            var samples = v.Layers.Single(l => l.Id == "plan-gamma").Samples;
            Equal(true, samples[0].Verdict!.StartsWith("Inside", StringComparison.Ordinal), "inside strip");
            Equal(true, samples[1].Verdict!.StartsWith("Outside", StringComparison.Ordinal), "outside strip");
            Equal(1, samples.Count(x => x.Verdict == Labels.TipNotJudged), "exactly the tip");
        });
        Check("DeriveVerdicts_TaperedPlanform_SweepIsTheLatticeSweepAndAlphaL0IsTheStripSection", () => {
            using var session = Fixture.OpenedWithTip("40");
            var settings = Settings.WithStations(Settings.Default with { NSpanPerHalf = 6, NChord = 1, SectionEtas = null, SectionXs = null });
            var method = new ProductWingMethod(settings);
            var run = Fixture.Evaluate(new AnalysisService(session, method), Fixture.Op(3));
            byte[] source = session.Snapshot().Source;
            var verdicts = MethodRecord.DeriveVerdicts(run, source)!;
            Equal(run.Strips.Count, verdicts.Count, "one verdict per strip");
            var sections = Placement.Sections(source, settings.SectionEtas!, settings.SectionXs!, CancellationToken.None);
            var solution = method.Solve(sections, run.Op, run.Water, CancellationToken.None);
            var edges = run.Strips.Select(x => (x.YLow!.Value, x.YHigh!.Value)).ToArray();
            double[] sweeps = VortexLattice.StripSweeps(ProductWingMethod.Mirror(sections), edges);
            double maxSweep = 0;
            for (int i = 0; i < sweeps.Length; i++)
            {
                Equal(true, Math.Abs(sweeps[i] - solution.Strips[i].SweepDeg) < 1e-9, "sweep of strip " + i);
                maxSweep = Math.Max(maxSweep, Math.Abs(sweeps[i]));
                StripLoad strip = run.Strips[i];
                double l0 = SectionEstimator.Estimate(source, Math.Abs(strip.Eta), 0, 1e6, 200).AlphaL0Deg;
                var expected = MethodRecord.JudgeStrip(strip.AlphaEff, l0, strip.ClLocal, sweeps[i],
                    strip.Provisional && strip.ProvisionalReason == StripLoad.TipProvisionalReason);
                Equal(expected.Text, verdicts[i].Text, "verdict text of strip " + i);
            }
            Equal(true, maxSweep > 0.5, "the fixture is swept: max |sweep| " + maxSweep);
        });
        Check("DeriveVerdicts_AlphaBound_9p9InsideAnd10p1Outside", () => {
            using var session = Fixture.Opened();
            var settings = Settings.WithStations(Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null });
            var run = Fixture.Evaluate(new AnalysisService(session, new ProductWingMethod(settings)), Fixture.Op(3));
            byte[] source = session.Snapshot().Source;
            StripLoad probe = run.Strips[1];
            double l0 = SectionEstimator.Estimate(source, Math.Abs(probe.Eta), 0, 1e6, 200).AlphaL0Deg;
            foreach (var (delta, inside) in new[] { (9.9, true), (10.1, false), (-9.9, true), (-10.1, false) })
            {
                var moved = run with { Strips = run.Strips.Select(x => x.J == 1 ? x with { AlphaEff = l0 + delta, ClLocal = 0.4 } : x).ToArray() };
                var verdict = MethodRecord.DeriveVerdicts(moved, source)![1];
                Equal(inside, verdict.Inside, "alpha offset " + delta);
                Equal(inside, verdict.Exceeded.Count == 0, "exceeded parts at " + delta);
            }
        });
        Check("JudgeStrip_SweepBound_29p9InsideAnd30p1Outside", () => {
            Equal(true, MethodRecord.JudgeStrip(0, 0, 0.4, 29.9).Inside, "29.9");
            var over = MethodRecord.JudgeStrip(0, 0, 0.4, 30.1);
            Equal(false, over.Inside, "30.1");
            Equal("sweep", over.Exceeded.Single(), "sweep named");
            Equal(false, MethodRecord.JudgeStrip(0, 0, 0.4, -30.1).Inside, "-30.1");
        });
        Check("DeriveVerdicts_OnlyTheTipReasonGetsTheTipRule", () => {
            using var session = Fixture.Opened();
            var settings = Settings.WithStations(Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null });
            var run = Fixture.Evaluate(new AnalysisService(session, new ProductWingMethod(settings)), Fixture.Op(3));
            byte[] source = session.Snapshot().Source;
            var tips = run.Strips.Where(x => x.Provisional).Select(x => x.J).ToArray();
            Equal(2, tips.Length, "two tip strips");
            Equal(true, MethodRecord.DeriveVerdicts(run, source)!.Where((_, i) => tips.Contains(i)).All(v => v.Provisional), "tip reason is provisional");
            var other = run with { Strips = run.Strips.Select(x => x.Provisional ? x with { ProvisionalReason = "OTHER" } : x).ToArray() };
            Equal(true, MethodRecord.DeriveVerdicts(other, source)!.All(v => !v.Provisional), "another reason is judged normally");
            var view = View(Rehash(other), new ProjectionContext(Verdicts: MethodRecord.DeriveVerdicts(other, source)));
            Equal(false, view.Groups.Single(g => g.Title == "Strips").Rows.Any(r => r.Note == Labels.TipNotJudged), "no strip reads tip-not-judged");
        });
        Check("Projection_ProvisionalVerdict_EmptyExceededNeverOutside", () => {
            var (run, _) = Data();
            var verdicts = Enumerable.Range(0, 4).Select(j => j == 3
                ? MethodRecord.JudgeStrip(12, 0, 0.4, 0, provisional: true)
                : MethodRecord.JudgeStrip(2, 0, 0.4, 0)).ToArray();
            var view = View(run, new ProjectionContext(Verdicts: verdicts));
            string sentence = Cell(view, "Wing result", "Envelope").Value;
            Equal(false, sentence.Contains("Outside", StringComparison.Ordinal));
            Equal(false, sentence.Contains("exceeded:", StringComparison.Ordinal));
            Equal(false, view.Layers.Single(l => l.Id == "plan-gamma").Samples[3].Outside);
        });
        Check("Projection_LegacyStripEdges_OmittedAndHashIntact", () => {
            var (run, _) = Data();
            string json = System.Text.Json.JsonSerializer.Serialize(run,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            Equal(false, json.Contains("\"ya\":", StringComparison.Ordinal));
            Equal(false, json.Contains("\"yb\":", StringComparison.Ordinal));
            Equal(false, json.Contains("\"yLow\":", StringComparison.Ordinal));
            Equal(false, json.Contains("\"yHigh\":", StringComparison.Ordinal));
            Equal(run.ContentHash, RunRecord.ContentHash(run));
            Equal(false, Cell(View(run), "Wing result", "CDi (Trefftz)").Value.StartsWith("Unavailable", StringComparison.Ordinal));
            var edged = Rehash(run with
            {
                Strips = run.Strips.Select((s, i) => s with { YLow = -0.4 + 0.2 * i, YHigh = -0.2 + 0.2 * i }).ToArray()
            });
            string edgedJson = System.Text.Json.JsonSerializer.Serialize(edged,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            Equal(true, edgedJson.Contains("\"yLow\":", StringComparison.Ordinal), "yLow");
            Equal(true, edgedJson.Contains("\"yHigh\":", StringComparison.Ordinal), "yHigh");
            Equal(false, edgedJson.Contains("\"ya\":", StringComparison.Ordinal), "ya retired");
            Equal(false, edgedJson.Contains("\"yb\":", StringComparison.Ordinal), "yb retired");
        });
        Check("Projection_OneMissingSpanEdge_WidthUnavailable", () => {
            var view = View(s => s.J == 0 ? s with { YLow = -0.4 } : s);
            Equal("Unavailable — a strip width is not recorded.", Cell(view, "Wing result", "CDi (Trefftz)").Value);
            Equal("Unavailable — e is undefined when CL or induced drag is zero.", Cell(view, "Wing result", "e (computed)").Value);
        });
        Check("Projection_ExcludedClosingTip_UsesKeptStripEdges", () => {
            var (run, _) = Data();
            double Edge(int j) => -Math.Cos(Math.PI * j / 4) * run.Reference.BRef / 2;
            var kept = run.Strips.Skip(1).Select(s => s with
            {
                J = s.J - 1, YLow = Edge(s.J), YHigh = Edge(s.J + 1)
            }).ToArray();
            run = Rehash(run with { Strips = kept });
            var view = View(run);
            double drag = 0.5 * run.Water.Rho * kept.Sum(s => s.Gamma * -s.DownwashTrefftz * (s.YHigh!.Value - s.YLow!.Value));
            double moment = run.Water.Rho * run.Op.Speed * kept.Where(s => s.Y >= 0)
                .Sum(s => s.Gamma * s.Y * (s.YHigh!.Value - s.YLow!.Value));
            Equal(drag.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Cell(view, "Wing result", "Induced drag").Value);
            double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
            Equal((drag / (q * run.Reference.SRef)).ToString("0.00000", System.Globalization.CultureInfo.InvariantCulture),
                Cell(view, "Wing result", "CDi (Trefftz)").Value);
            double trefftzLift = run.Water.Rho * run.Op.Speed * kept.Sum(s => s.Gamma * (s.YHigh!.Value - s.YLow!.Value));
            double cl = trefftzLift / (q * run.Reference.SRef);
            double ar = run.Reference.BRef * run.Reference.BRef / run.Reference.SRef;
            Equal(Trefftz.Oswald(cl, ar, drag / (q * run.Reference.SRef))
                .ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                Cell(view, "Wing result", "e (computed)").Value);
            Equal(moment.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Cell(view, "Loads", "Root bending moment").Value);
            var samples = view.Layers.Single(l => l.Id == "strip-lift").Samples;
            for (int i = 0; i < kept.Length; i++)
                Equal(kept[i].Fz / (kept[i].YHigh!.Value - kept[i].YLow!.Value), samples[i].Value!.Value);
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
        Check("Layers_MomentArc_RightHandPositiveXMatchesRootMoment", () => {
            var view = View();
            double value = double.Parse(Cell(view, "Loads", "Root bending moment").Value,
                System.Globalization.CultureInfo.InvariantCulture);
            var arc = view.Layers.Single(l => l.Id == "root-moment").Samples.Single();
            Equal(true, value > 0); // +z lift at +y gives +x bending in the chosen body convention.
            Equal(true, arc.Vector!.Value.X > 0);
            Equal(arc.Value, arc.Vector.Value.X);
        });
        Check("Layers_Vectors_BodyFrameSignConventionLabelled", () => {
            // The legend names the body axes at either sign of alpha, and the lift vector follows the sign of the force along +z
            // (a mutant taking the direction from |L| flips the negative-alpha vector and is red here).
            foreach (double alpha in new[] { 3.0, -3.0 })
            {
                double sign = Math.Sign(alpha);
                var run = Data(s => s with { Fz = sign * 10 }, alpha).Run;
                var lift = View(run).Layers.Single(l => l.Id == "strip-lift");
                Equal(true, lift.Legend.Contains("+x aft, +y starboard, +z up"));
                Equal(true, lift.Samples.All(s => s.Vector is { } v && Math.Sign(v.Z) == sign && Math.Sign(s.Value!.Value) == sign));
                Equal(true, lift.Samples.All(s => s.Value == s.Vector!.Value.Z));   // the signed value is the vector's z, never |L|
            }
        });
    }

    internal static (AnalysisRun Run, CurrentInputs Current) Data(Func<StripLoad, StripLoad>? change = null, double alpha = 3)
    {
        var settings = new RunSettings(2, 4, "cosine", "cosine", 20, "+x", 1e-8, "vlm-envelope/1", null, [2, 4], "clean", 0.3);
        var inputs = new RunInputs("accepted", new string('a', 64), [new string('b', 64)], "cfdw-cv/2", "placement/1");
        var water = new WaterRecord(15, 35.16504, 1000, 1e-6, 1700, "ITTC", new string('c', 64));
        var op = OperatingPoints.Custom(5, alpha, 0.5);
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
