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
        Check("Depth_ShallowAtAnyStation_FreeSurfaceNotModelled", () => {
            // R101 3a: root h/c is 5.0 (not below 5); the tip station sits 0.3 m up, so its h/c is 2.0.
            var run = ProjectionTests.Data().Run;
            StationFrame[] shallowTip = [new(0, 0, 0, 0.1, 0.0, 0, 0.12), new(1, 0.4, 0, 0.1, 0.3, 0, 0.12)];
            var v = ProjectionTests.View(run, new ProjectionContext(Stations: shallowTip));
            Equal("attached flow; no stall; no ventilation; free surface not modelled", ProjectionTests.Cell(v, "Labels", "Basis").Value, "COPY-224");
            Equal(true, ProjectionTests.Cell(v, "Labels", "Not modelled").Value.StartsWith("Not modelled: free surface,", StringComparison.Ordinal), "COPY-226");
            Equal(true, ProjectionTests.Cell(v, "Labels", "Depth basis").Value.Contains("h/c = 2.00"), "Depth basis at the shallowest station");
            StationFrame[] deepTip = [new(0, 0, 0, 0.1, 0.0, 0, 0.12), new(1, 0.4, 0, 0.1, 0.0, 0, 0.12)];
            var deep = ProjectionTests.View(run, new ProjectionContext(Stations: deepTip));
            Equal("attached flow; no stall; no ventilation; deep water", ProjectionTests.Cell(deep, "Labels", "Basis").Value, "COPY-223 when every station is at least h/c 5");
            Equal(false, deep.Groups.Single(g => g.Title == "Labels").Rows.Any(r => r.Label == "Depth basis"), "no Depth basis row");
        });
        Check("Projection_NoBareUnavailable_EveryStateCarriesItsReason", () => {
            var (run, _) = ProjectionTests.Data();
            var failed = ProjectionTests.Rehash(run with { Outcome = new RunOutcome.Failed("ANA-SOLVE-RESIDUAL", "residual exceeded"), Strips = [] });
            var states = new List<(string Name, AnalysisViewModel View)>
            {
                ("default", ProjectionTests.View()),
                ("no verdicts, feed reason", ProjectionTests.View(run, new ProjectionContext(FeedUnavailable: Labels.FeedRevisionNotHeld))),
                ("one verdict short", ProjectionTests.View(run, new ProjectionContext(Verdicts: [MethodRecord.JudgeStrip(2, 0, 0.4, 0)]))),
                ("missing span edge", ProjectionTests.View(s => s.J == 0 ? s with { YLow = -0.4 } : s)),
                ("no reference area", ProjectionTests.View(ProjectionTests.Rehash(run with { Reference = run.Reference with { SRef = 0 } }))),
                ("failed", ProjectionTests.View(failed)),
                ("tampered", ProjectionTests.View(run, new ProjectionContext(Integrity: RunIntegrity.PayloadFailedCheck)))
            };
            foreach (var (name, view) in states)
            {
                var cells = view.Groups.SelectMany(g => g.Rows).Concat(view.StripDetails.SelectMany(d => d.Rows))
                    .SelectMany(r => new[] { r.Value, r.Note ?? "" });
                foreach (string cell in cells)
                    Equal(false, cell == "Unavailable" || cell.EndsWith(": Unavailable", StringComparison.Ordinal) ||
                        cell.Contains("; Unavailable", StringComparison.Ordinal), name + ": " + cell);
            }
            string envelope = ProjectionTests.Cell(states[1].View, "Wing result", "Envelope").Value;
            Equal(true, envelope.Contains(Labels.FeedRevisionNotHeld), "the feed's reason reaches the run sentence");
            Equal(true, ProjectionTests.Cell(states[0].View, "Wing result", "Envelope").Value.StartsWith("Unavailable — ", StringComparison.Ordinal), "no feed reason");
        });
        Check("Projection_TotalDrag_OneReasonString", () => {
            var v = ProjectionTests.View();
            Equal("Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray", Labels.UnavailableBecause("missing: profile (no polar method installed), junction, mast, wave, spray"), "COPY-353 in the COPY-70 form");
            Equal("Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray", Loads.TotalDragReason, "COPY-353");
            Equal(Loads.TotalDragReason, ProjectionTests.Cell(v, "Wing result", "CL/CD").Value, "craft CL/CD");
            var values = v.Groups.SelectMany(g => g.Rows).SelectMany(r => new[] { r.Value, r.Note ?? "" }).ToArray();
            Equal(false, values.Any(x => x.Contains("total drag missing", StringComparison.OrdinalIgnoreCase)), "the retired string");
            Equal(false, values.Any(x => x.StartsWith("ANA-TOTAL-DRAG-MISSING", StringComparison.Ordinal)), "no reason code as a value");
        });
        Check("Projection_DragWingOnly_OneRow_ImperialLbf", () => {
            var run = ProjectionTests.Data(s => s with { CdNcrit2 = new StripValue(0.02, null), CdNcrit4 = new StripValue(0.03, null) }).Run;
            var metric = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric);
            var imperial = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Imperial);
            foreach (string group in new[] { "Wing result", "Loads" })
            {
                var rows = metric.Groups.Single(g => g.Title == group).Rows;
                Equal(1, rows.Count(r => r.Label == "Drag (Wing only)"), group + " one row");
                Equal(false, rows.Any(r => r.Label is "Wing-only drag" or "Total drag"), group + " old labels gone");
            }
            ResultRow n = ProjectionTests.Cell(metric, "Loads", "Drag (Wing only)"), lbf = ProjectionTests.Cell(imperial, "Loads", "Drag (Wing only)");
            Equal("N", n.Unit); Equal("lbf", lbf.Unit);
            double[] newtons = n.Value.Split('–').Select(double.Parse).ToArray(), pounds = lbf.Value.Split('–').Select(double.Parse).ToArray();
            Equal(true, Math.Abs(newtons[0] / 4.4482216152605 - pounds[0]) < 0.001 && Math.Abs(newtons[1] / 4.4482216152605 - pounds[1]) < 0.001, "lbf = N / 4.4482");
            Equal(true, n.Note!.StartsWith("Wing only: induced (VLM + strip) plus profile (polar). Not a total.", StringComparison.Ordinal), "note");
            Equal(true, n.Note.Contains("XFOIL-class surrogate"), "surrogate label");
            Equal(true, n.Note.EndsWith("\nNot included: junction, mast, wave, spray", StringComparison.Ordinal), "reason line");
            Equal(Loads.TotalDragReason, ProjectionTests.Cell(metric, "Wing result", "CL/CD").Value, "craft CL/CD stays Unavailable");
            Equal(false, ProjectionTests.Cell(metric, "Wing result", "Wing-only CL/CD").Value.StartsWith("Unavailable", StringComparison.Ordinal), "Wing-only CL/CD shows a number");
        });
        Check("Labels_Ruling109And101_ConstantsMatchTheirRows", () => {
            string design = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"));
            Equal(true, design.Contains("| COPY-354 | " + Labels.WingDragLabel + " — approved — Ruling 109"), "COPY-354");
            Equal(true, design.Contains("| COPY-356 | " + Labels.WingDragNotIncluded + " — approved — Ruling 109"), "COPY-356");
            Equal(true, design.Contains("| COPY-330 | " + Labels.WingDragNote + " — approved — Ruling 108"), "COPY-330");
            Equal(true, design.Contains("| COPY-353 | " + Loads.TotalDragReason + " — approved — Ruling 101"), "COPY-353");
            Equal(true, design.Contains("| COPY-250 | Unavailable — <reason> — approved — Ruling 101"), "COPY-250 form");
            Equal("Unavailable — x", Labels.UnavailableBecause("x"), "COPY-250 builder");
            Equal(true, design.Contains("| COPY-253 | " + Labels.FeedRevisionNotHeld + " — approved — Ruling 101"), "COPY-253");
        });
        Check("Projection_TamperedRun_NoteIsCopy274UnderCopy211", () => {
            var (run, _) = ProjectionTests.Data();
            var row = ProjectionTests.Cell(ProjectionTests.View(run, new ProjectionContext(Integrity: RunIntegrity.PayloadFailedCheck)), "Wing result", "Result");
            Equal("Unavailable — run payload failed its check", row.Value, "COPY-211");
            Equal("The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run.", row.Note, "COPY-274");
        });
        Check("Projection_HistoricalResult_TierChipReadsHistoricalVlmStrip", () => {
            var (run, _) = ProjectionTests.Data();
            var current = ProjectionTests.View(run);
            Equal("VLM + strip · local calculation", ProjectionTests.Cell(current, "Wing result", "Tier").Value, "Current chip is COPY-213");
            var moved = AnalysisProjection.Build(run, ProjectionTests.Current(run) with { Op = run.Op with { AlphaDeg = run.Op.AlphaDeg + 1 } }, Units.Metric);
            Equal(RunState.Historical, moved.State);
            Equal("Historical · VLM + strip", ProjectionTests.Cell(moved, "Wing result", "Tier").Value, "COPY-279");
            var failed = ProjectionTests.Rehash(run with { Outcome = new RunOutcome.Failed("ANA-SOLVE-RESIDUAL", "residual exceeded"), Strips = [] });
            var kept = AnalysisProjection.Build(failed, ProjectionTests.Current(run), Units.Metric, new ProjectionContext(PreviousCompleted: run));
            Equal(RunState.Failed, kept.State);
            Equal("Historical · VLM + strip", ProjectionTests.Cell(kept, "Wing result", "Tier").Value, "COPY-279 while a failed attempt shows the previous result");
        });
        Check("Labels_ConditionsSummary_Copy280Form_FollowsUnits", () => {
            var water = WaterTable.At(15, 35.16504);
            Equal("9.99 kn · salt 15 °C · as the band", Labels.ConditionsSummary(5.14, water, Units.Imperial), "Imperial");
            Equal("5.14 m/s · salt 15 °C · as the band", Labels.ConditionsSummary(5.14, water, Units.Metric), "Metric");
            Equal("5.14 m/s · fresh 15 °C · as the band", Labels.ConditionsSummary(5.14, WaterTable.At(15, 0), Units.Metric), "fresh");
        });
        Check("Labels_ChartAndChipConstants_MatchTheirRows", () => {
            string design = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"));
            Equal(true, design.Contains("| COPY-274 | " + Labels.TamperedNote + " — approved — Ruling 101"), "COPY-274");
            Equal(true, design.Contains("| COPY-275 | " + Labels.ChartXTitle + " — approved — Ruling 101"), "COPY-275");
            Equal(true, design.Contains("| COPY-276 | " + Labels.ChartYTitle + " — approved — Ruling 101"), "COPY-276");
            Equal(true, design.Contains("| COPY-277 | " + Labels.ChartLegend + " — approved — Ruling 101"), "COPY-277");
            Equal(true, design.Contains("| COPY-278 | " + Labels.ChartSeries + " — approved — Ruling 101"), "COPY-278");
            Equal(true, design.Contains("| COPY-279 | " + Labels.HistoricalChip + " — approved — Ruling 101"), "COPY-279");
            Equal(true, design.Contains("| COPY-280 | 10 kn · salt 15 °C · as the band — approved — Ruling 101"), "COPY-280");
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
        // Ruling 96 (COPY-243 to COPY-249): the drag-hold and typed-refusal copy is Core's, pinned to its DESIGN rows.
        Equal(true, design.Contains("| COPY-243 | " + TipChord.RefusalTemplate + " Enter <min> or more. — approved — Ruling 96"), "COPY-243 row");
        Equal(true, design.Contains("| COPY-244 | " + TipChord.HoldTipTemplate + " — approved — Ruling 96"), "COPY-244 row");
        Equal(true, design.Contains("| COPY-245 | " + TipChord.HoldRootTemplate + " — approved — Ruling 96"), "COPY-245 row");
        Equal(true, design.Contains("| COPY-246 | " + TipChord.RootRefusalTemplate + " — approved — Ruling 96"), "COPY-246 row");
        Equal(true, design.Contains("| COPY-247 | " + TipChord.AlreadyUnderText + " — approved — Ruling 96"), "COPY-247 row");
        Equal(true, design.Contains("| COPY-248 | <value> mm · minimum — approved — Ruling 96"), "COPY-248 row");
        Equal("Tip chord can't go below 5 mm (the larger of 5 mm and 2 % of the root chord). Enter 5 mm or more.", TipChord.TypedRefusalReason(0.12), "COPY-243 formatted");
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
