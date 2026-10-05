using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

internal static class LabelsTests
{
    internal static void Run()
    {
        Check("Depth_Unset_NoDeepWaterLabel", () => {
            var run = ProjectionTests.Data().Run; run = ProjectionTests.Rehash(run with { Op = run.Op with { HRef = null } });
            var v = ProjectionTests.View(run); Equal(Labels.FixedVlmNoDepth, ProjectionTests.Cell(v, "Labels", "Basis").Value);
            Equal(false, string.Join(" ", v.Groups.SelectMany(g => g.Rows).Select(r => r.Value)).Contains("deep water", StringComparison.OrdinalIgnoreCase));
        });
        Check("Depth_HcBelow5_Copy46WithNumbers", () => {
            var run = ProjectionTests.Data().Run; run = ProjectionTests.Rehash(run with { Op = run.Op with { HRef = 0.4 } });
            var v = ProjectionTests.View(run); string depth = ProjectionTests.Cell(v, "Labels", "Depth basis").Value;
            Equal(true, depth.Contains("h/c = 4.00") && depth.Contains("Fr_h ="));
        });
        Check("Depth_StationPiercing_EstimatorUnavailableFlagOnly", () => {
            var (run, _) = ProjectionTests.Data(); run = ProjectionTests.Rehash(run with { Op = run.Op with { HRef = 0.05 } });
            var stations = new[] { new StationFrame(1, 0.4, 0, 0.1, 0.1, 0, 0.12) };
            var v = ProjectionTests.View(run, new ProjectionContext(Stations: stations));
            Equal("Unavailable — surface piercing", ProjectionTests.Cell(v, "Labels", "Tip depth").Value);
        });
        Check("Depth_SetAllSubmerged_TipMarginFrCopy47", () => {
            var run = ProjectionTests.Data().Run; var stations = new[] { new StationFrame(1, 0.4, 0, 0.1, 0.02, 0, 0.12) };
            var v = ProjectionTests.View(run, new ProjectionContext(Stations: stations));
            Equal("0.48", ProjectionTests.Cell(v, "Labels", "Tip depth").Value);
            Equal(true, v.Layers.Any(l => l.Id == "depth-band"));
            Equal(true, v.Groups.Single(g => g.Title == "Labels").Rows.Any(r => r.Label == "Ventilation"));
        });
        Check("Labels_PerTier_FixedPartsAndOmissions", () => {
            Equal(Labels.VlmChip, Labels.Chip(Tier.VlmStrip)); Equal(Labels.PolarChip, Labels.Chip(Tier.Polar));
            Equal(Labels.EstimatorChip, Labels.Chip(Tier.Estimator));
            Equal(true, ProjectionTests.Cell(ProjectionTests.View(), "Labels", "Not modelled").Value.Contains("tip-vortex cavitation"));
        });
        Check("Labels_DepthUnset_FreeSurfaceNotModelled", () => {
            Equal(true, Labels.FixedVlm(false).EndsWith("free surface not modelled"));
            Equal(true, Labels.NotModelled(false).Contains("free surface"));
        });
        Check("Envelope_RunVerdict_BesideCL_FullBound", () => {
            var run = ProjectionTests.Data().Run; var verdicts = Enumerable.Range(0, 4).Select(_ => MethodRecord.JudgeStrip(12, 0, 0.4, 0)).ToArray();
            var v = ProjectionTests.View(run, new ProjectionContext(Verdicts: verdicts));
            var rows = v.Groups.Single(g => g.Title == "Wing result").Rows;
            Equal("Envelope", rows[2].Label); Equal(true, rows[2].Value.Contains("4 of 4 strips"));
            Equal(true, rows[2].Value.Contains("quarter-chord sweep ≤ 30°"));
        });
        Check("Envelope_EOutOfBand_AdvisoryNotBlocking", () => {
            var v = ProjectionTests.View(); Equal(RunState.Current, v.State);
            Equal(true, ProjectionTests.Cell(v, "Wing result", "e (computed)").Note!.Contains("lattice effect"));
        });
        Check("Station_StripReadout_EnvelopeVerdictPerPart", () => {
            var run = ProjectionTests.Data().Run; var verdicts = Enumerable.Range(0, 4).Select(_ => MethodRecord.JudgeStrip(12, 0, 0.4, 0)).ToArray();
            var v = ProjectionTests.View(run, new ProjectionContext(Verdicts: verdicts));
            Equal(true, AnalysisProjection.StripAt(v, 0.25).Rows.Single(r => r.Label == "Envelope (this strip)").Value.Contains("|α_eff − α_L0|"));
        });
        Check("Station_StripReadout_ReAgainstPolarRange", () => {
            var v = ProjectionTests.View(); Equal(Labels.NoPolar, AnalysisProjection.StripAt(v, 0.25).Rows.Single(r => r.Label == "Polar Re range").Value);
        });
        Check("Station_StripReadout_NotModelledList", () => {
            var v = ProjectionTests.View(); Equal(Labels.NotModelled(true), AnalysisProjection.StripAt(v, 0.25).Rows.Single(r => r.Label == "Not modelled").Value);
        });
        Check("Copy_AnalysisStrings_MatchDesignMd", CopyRows);
        Check("Lab01_EveryProjectionString_Lints", () => {
            var v = ProjectionTests.View(); Equal(true, v.Groups.All(g => g.Rows.All(r => !string.IsNullOrWhiteSpace(r.Label) && !string.IsNullOrWhiteSpace(r.Value))));
            Equal(true, v.Layers.All(l => !string.IsNullOrWhiteSpace(l.Legend) && !string.IsNullOrWhiteSpace(l.TwinId)));
        });
    }

    private static void CopyRows()
    {
        string design = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"));
        foreach (int id in Enumerable.Range(206, 34))
            Equal(true, design.Contains("| COPY-" + id + " |") && design.Contains("proposed — awaiting operator"), "COPY-" + id);
        foreach (string copy in new[] { Labels.NoResult, Labels.NoPolar, Labels.PayloadFailed, Labels.SectionCp,
            Labels.VlmChip, Labels.OutsideLattice, Labels.VerifiedLattice, Labels.Provisional, Labels.AtBound,
            Labels.Indeterminate, Labels.FixedVlmNoDepth, Labels.StructuralList, Labels.BodyAxes })
            Equal(true, design.Contains(copy), copy);
    }
}
