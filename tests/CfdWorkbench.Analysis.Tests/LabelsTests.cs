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
            var run = ProjectionTests.Data(s => s with { DownwashTrefftz = -0.032 }).Run;
            var v = ProjectionTests.View(run); Equal(RunState.Current, v.State);
            string note = ProjectionTests.Cell(v, "Wing result", "e (computed)").Note!;
            Equal(true, note.Contains("e below 0.85"));
            Equal(false, note.Contains("lattice effect"));
        });
        Check("Labels_EAboveOne_LatticeAttributionOnlyMeasuredBand", () => {
            const string checkLattice = "e above 1 — check the lattice";
            string? measured = ENote(1.01, 64);
            Equal(true, measured?.Contains("lattice bias") == true, "measured band");
            Equal(false, measured?.Contains(checkLattice) == true, "measured band is not the open check");
            string? coarse = ENote(1.01, 32);
            Equal(false, coarse?.Contains("lattice bias") == true, "non-default");
            Equal(checkLattice, coarse, "e > 1 off the default lattice");
            string? beyond = ENote(1.03, 64);
            Equal(false, beyond?.Contains("lattice bias") == true, "above 1.02");
            Equal(checkLattice, beyond, "e > 1.02");
        });
        Check("Labels_VerifiedLattice_NamesFixtureScope", () => {
            string claim = Labels.VerifiedLattice;
            Equal(true, claim.Contains("rectangular and elliptic", StringComparison.Ordinal), "planforms");
            Equal(true, claim.Contains("±20° dihedral at 32 × 4", StringComparison.Ordinal), "F-8");
            Equal(true, claim.Contains("45° sweep, AR 5, at 4 × 1", StringComparison.Ordinal), "F-16");
            Equal(true, claim.Contains("4% camber at 32/64/128 × 4 cosine/cosine (F-18)", StringComparison.Ordinal), "F-18");
            Equal(true, claim.Contains("1° washin at 32/64/128 × 4 cosine/cosine (F-19)", StringComparison.Ordinal), "F-19");
            Equal(true, claim.Contains("F-6 order at 32/64/128 × 4 cosine span, uniform chord", StringComparison.Ordinal), "F-6");
            Equal(true, claim.Contains("F-21 at 16 × 4 cosine/cosine per half", StringComparison.Ordinal), "F-21");
            Equal(false, claim.Contains("at the 64 × 4", StringComparison.Ordinal), "blanket lattice");
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
        {
            string marker = id is 218 or 219 ? "retired — Ruling 92" : id == 220 ? "approved — Ruling 92" : "approved — Ruling 82";
            string? row = design.Split('\n').FirstOrDefault(l => l.Contains("| COPY-" + id + " |"));
            Equal(true, row != null && row.Contains(marker), "COPY-" + id);
        }
        foreach (string copy in new[] { Labels.NoResult, Labels.NoPolar, Labels.PayloadFailed, Labels.SectionCp,
            Labels.VlmChip, Labels.OutsideLattice, Labels.VerifiedLattice,
            Labels.TipNotJudged, Labels.FixedVlmNoDepth, Labels.StructuralList, Labels.BodyAxes })
            Equal(true, design.Contains(copy), copy);
        Equal("Not judged — tip strip", Labels.TipNotJudged, "COPY-220 text (Ruling 92)");
        Equal(true, design.Contains("| COPY-220 | Not judged — tip strip — approved — Ruling 92 |"), "COPY-220 row");
        Equal(true, design.Contains("| COPY-241 | " + Labels.TipChordUnderMinimumTemplate + " — approved — Ruling 94 |"), "COPY-241 row");
        Equal(true, design.Contains("| COPY-242 | " + TipChord.RefusalTemplate + " — approved — Ruling 94 |"), "COPY-242 row");
        Equal("Unavailable — tip chord under the minimum (5 mm). The tip is not certified for analysis.", Labels.TipChordUnderMinimum(0.12), "COPY-241 formatted");
        Equal(true, design.Contains("| COPY-240 |") && design.Contains("e above 1 — check the lattice — approved — Ruling 82"), "COPY-240");
    }

    private static string? ENote(double target, int spanPerHalf)
    {
        var (baseRun, _) = ProjectionTests.Data();
        var settings = baseRun.Settings with { NSpanPerHalf = spanPerHalf };
        string settingsHash = RunRecord.SettingsHash(settings);
        double q = 0.5 * baseRun.Water.Rho * baseRun.Op.Speed * baseRun.Op.Speed;
        double sumGammaDy = baseRun.Strips.Sum(s => s.Gamma * 0.2);
        double cl = baseRun.Water.Rho * baseRun.Op.Speed * sumGammaDy / (q * baseRun.Reference.SRef);
        double ar = baseRun.Reference.BRef * baseRun.Reference.BRef / baseRun.Reference.SRef;
        double drag = cl * cl / (Math.PI * ar * target) * q * baseRun.Reference.SRef;
        double downwash = -drag / (0.5 * baseRun.Water.Rho * sumGammaDy);
        var strips = baseRun.Strips.Select((s, i) => s with
        { YLow = -0.4 + 0.2 * i, YHigh = -0.2 + 0.2 * i, DownwashTrefftz = downwash }).ToArray();
        var run = baseRun with { Settings = settings, SettingsHash = settingsHash,
            RunKey = RunRecord.Key(baseRun.Inputs, baseRun.Water, baseRun.Op, baseRun.Method, settingsHash), Strips = strips };
        run = ProjectionTests.Rehash(run);
        return ProjectionTests.Cell(ProjectionTests.View(run), "Wing result", "e (computed)").Note;
    }
}
