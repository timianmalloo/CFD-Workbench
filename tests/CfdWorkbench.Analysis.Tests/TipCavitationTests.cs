using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Ruling 142: a tip-provisional strip never decides the wing cavitation verdict, and a tip station's screen reads Not judged.
/// Ring: readiness, about 3.5 s in all (the example wing 2.5 s, the shared lattice run SectionForceTests.CamberedRun about 0.5 s, the tier
/// checks 0.3 s). Not the every-push ring: the Analysis parts there sit at 4.85 to 4.91 s against the 5 s C-2 limit on the baseline,
/// and this group added 0.2 to 0.3 s and tipped both parts over (measured 2026-10-08, trk-tcv).
/// </summary>
internal static class TipCavitationTests
{
    /// <summary>The example-wing check prints the before/after record of docs/proof/tcv.</summary>
    internal static void RunReadiness()
    {
        AnalysisChecks.Check("TipCavitation_PlantedTipStation_NeverGovernsTheWing", PlantedTipNeverGoverns);
        AnalysisChecks.Check("TipCavitation_EveryStationTip_WingLineIsNotJudged", AllTip);
        AnalysisChecks.Check("TipCavitation_Exclusion_WingLineStatesCountAndJudgedStations", WingLineSuffix);
        AnalysisChecks.Check("TipCavitation_ProjectionRowsAndBand_FollowTheRule", ProjectionRowsFollowTheRule);
        AnalysisChecks.Check("TipCavitation_TipStationDisplay_NoSigmaAndNoCpMinNumber", TipStationDisplay);
        AnalysisChecks.Check("TipCavitation_ExampleWing_GoverningStationIsJudged_BeforeAfterObserved", ExampleWing);
    }

    private static void ExampleWing()
    {
        using var session = new AuthoringSession();
        byte[] source = FoilSource.NewDefault();
        session.Open(source, Fixture.Id(), true);
        var service = new AnalysisService(session, new ProductWingMethod(Settings.Default));
        AnalysisRun run = Fixture.Evaluate(service, Fixture.Op(3));
        // Before: the same strips with the tip flag cleared, which is what the tier saw before Ruling 142.
        StripLoad[] unflagged = run.Strips.Select(s => s with { Provisional = false, ProvisionalReason = null }).ToArray();
        SectionTierResult before = SectionTier.Evaluate(source, run.Settings.SectionEtas!, unflagged, run.Op, run.Water);
        SectionTierResult after = SectionTier.Derive(run, source);
        string Describe(SectionTierResult t) => $"governing eta {t.GoverningEta:F4}, state {t.Cavitation.State}, sigma {t.Cavitation.Sigma:F4}, " +
            $"-Cp_min {-t.Cavitation.CpMin:F4}, stations {t.Stations.Count}, tip stations left out {t.TipNotJudgedCount} (eta {string.Join(", ", t.Stations.Where(s => s.TipNotJudged).Select(s => s.Eta.ToString("F4") + " ratio " + (s.Cavitation.Sigma / -s.Estimate.Panel.CpMin)?.ToString("F3")))}; governing ratio " +
            (t.Stations.Single(s => s.Eta == t.GoverningEta).Cavitation.Sigma / -t.Stations.Single(s => s.Eta == t.GoverningEta).Estimate.Panel.CpMin)?.ToString("F3") + $"), 400-panel candidates {t.PanelCandidateCount}";
        Console.WriteLine("OBSERVED example wing BEFORE (tip flag ignored): " + Describe(before));
        Console.WriteLine("OBSERVED example wing AFTER  (Ruling 142):      " + Describe(after));
        SectionStationResult governing = after.Stations.Single(s => s.Eta == after.GoverningEta);
        AnalysisChecks.Equal(false, governing.TipNotJudged, "the governing station is judged");
        AnalysisChecks.Equal(true, after.TipNotJudgedCount >= 1, "the example wing has a tip station left out");
    }

    private static StripLoad Tip(double eta, double alphaEff) =>
        SectionSeamTests.Strip(eta, alphaEff) with { Provisional = true, ProvisionalReason = StripLoad.TipProvisionalReason };

    // The tip station (eta 1) reads the largest alpha, so it holds the smallest sigma / (-Cp_min). Without the flag it governs.
    private static void PlantedTipNeverGoverns()
    {
        byte[] source = SectionSeamTests.ThicknessSource(_ => 0.10);
        SectionTierResult control = SectionTier.Evaluate(source, [0.5, 1.0],
            [SectionSeamTests.Strip(0.5, 2), SectionSeamTests.Strip(1.0, 8)], Fixture.Op(3), Fixture.Salt);
        AnalysisChecks.Equal(1.0, control.GoverningEta, "fixture: with no flag the high-alpha tip station governs");
        SectionTierResult tier = SectionTier.Evaluate(source, [0.5, 1.0], [SectionSeamTests.Strip(0.5, 2), Tip(1.0, 8)],
            Fixture.Op(3), Fixture.Salt);
        AnalysisChecks.Equal(0.5, tier.GoverningEta, "the tip-provisional station is not the governing station");
        AnalysisChecks.Equal("η 0.5", tier.Cavitation.GoverningStation, "the wing screen is the judged station's");
        AnalysisChecks.Equal(1, tier.TipNotJudgedCount, "one station left out");
        AnalysisChecks.Equal(true, tier.Stations[1].TipNotJudged && !tier.Stations[0].TipNotJudged, "the flag is on the tip station only");
        AnalysisChecks.Equal(1, tier.PanelCandidateCount, "the tip station is not a 400-panel candidate either");
    }

    private static void AllTip()
    {
        byte[] source = SectionSeamTests.ThicknessSource(_ => 0.10);
        SectionTierResult tier = SectionTier.Evaluate(source, [0.5, 1.0], [Tip(0.5, 2), Tip(1.0, 8)], Fixture.Op(3), Fixture.Salt);
        AnalysisChecks.Equal(CavitationState.Unavailable, tier.Cavitation.State, "no judged station, no verdict");
        AnalysisChecks.Equal(Labels.TipNotJudged, Labels.UnavailableBecause(tier.Cavitation.Reason!), "the wing line reads Not judged - tip strip");
        AnalysisChecks.Equal(null, tier.Cavitation.Sigma, "no sigma");
        AnalysisChecks.Equal(null, tier.Cavitation.CriticalSpeed, "no V_crit");
    }

    private static (AnalysisRun Run, byte[] Source, SectionTierResult Tier) Cambered()
    {
        (AnalysisRun run, byte[] source) = SectionForceTests.CamberedRun();
        // The outermost strips are tip-provisional; plant the largest alpha on them so they would govern if they were judged.
        StripLoad[] strips = run.Strips.Select(s => s.ProvisionalReason == StripLoad.TipProvisionalReason ? s with { AlphaEff = 10 } : s).ToArray();
        SectionTierResult tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], strips, Fixture.Op(1), Fixture.Salt);
        return (run, source, tier);
    }

    private static void WingLineSuffix()
    {
        (AnalysisRun run, byte[] source, SectionTierResult tier) = Cambered();
        AnalysisChecks.Equal(1, tier.TipNotJudgedCount, "eta 1 reads the tip strip");
        SectionView view = SectionDisplay.Build(run, tier, source, null, "r1", null, null, default, Units.Metric);
        ResultGroup cavitation = view.Groups.Single(g => g.Title == "Cavitation");
        string screen = cavitation.Rows.Single(r => r.Label == "Cavitation screen").Value;
        AnalysisChecks.Equal(true, screen.EndsWith("; 1 " + Labels.TipNotJudged, StringComparison.Ordinal), "the wing line states the count: " + screen);
        string governing = cavitation.Rows.Single(r => r.Label == "Governing station").Value;
        AnalysisChecks.Equal(true, governing.EndsWith("of 3 stations", StringComparison.Ordinal), "the count is judged stations only: " + governing);
        AnalysisChecks.Equal(true, tier.GoverningEta < 1.0, "the tip station is not governing");
    }

    // The Analysis panel's section rows and the conditions band read the same tier: the suffix and the V_crit note follow the rule.
    private static void ProjectionRowsFollowTheRule()
    {
        (AnalysisRun run, _, SectionTierResult tier) = Cambered();
        AnalysisViewModel view = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: tier));
        ResultRow[] rows = view.Groups.SelectMany(g => g.Rows).ToArray();
        string cavitation = rows.First(r => r.Label == "Cavitation").Value;
        AnalysisChecks.Equal(true, cavitation.EndsWith("; 1 " + Labels.TipNotJudged, StringComparison.Ordinal), "Cavitation row: " + cavitation);
        AnalysisChecks.Equal(true, rows.First(r => r.Label == "V_crit").Note!.EndsWith("; 1 " + Labels.TipNotJudged, StringComparison.Ordinal), "V_crit note");
        SectionTierResult allTip = SectionTier.Evaluate(SectionSeamTests.ThicknessSource(_ => 0.10), [0.5, 1.0], [Tip(0.5, 2), Tip(1.0, 8)],
            Fixture.Op(3), Fixture.Salt);
        ResultRow[] none = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric, new ProjectionContext(SectionTier: allTip))
            .Groups.SelectMany(g => g.Rows).ToArray();
        AnalysisChecks.Equal(Labels.TipNotJudged, none.First(r => r.Label == "Cavitation").Value, "Cavitation row, every station tip");
        AnalysisChecks.Equal(Labels.TipNotJudged, none.First(r => r.Label == "V_crit").Value, "V_crit, every station tip");
        AnalysisChecks.Equal(Labels.TipNotJudged, none.First(r => r.Label == "Cp_min").Value, "Cp_min, every station tip");
        // The Section view's wing line with every station a tip strip: exactly the approved text, not wrapped.
        SectionView allTipView = SectionDisplay.Build(run, allTip, null, null);
        string wing = allTipView.Groups.Single(g => g.Title == "Cavitation").Rows.Single(r => r.Label == "Cavitation screen").Value;
        AnalysisChecks.Equal(Labels.TipNotJudged, wing, "Section view wing line, every station tip");
        Console.WriteLine($"OBSERVED all-tip wing line: [{wing}]; Analysis panel Cavitation row: [{none.First(r => r.Label == "Cavitation").Value}]; band V_crit: [{none.First(r => r.Label == "V_crit").Value}]");
    }

    private static void TipStationDisplay()
    {
        (AnalysisRun run, byte[] source, SectionTierResult tier) = Cambered();
        SectionView tip = SectionDisplay.Build(run, tier, source, 1.0, "r1", null, null, default, Units.Metric);
        ResultGroup estimator = tip.Groups.Single(g => g.Title == "Estimator");
        AnalysisChecks.Equal(Labels.TipNotJudged, estimator.Rows.Single(r => r.Label == Labels.CpMinLabel).Value, "estimator -Cp_min row");
        AnalysisChecks.Equal(Labels.TipNotJudged, tip.Profile!.Cavitation, "profile cavitation line");
        AnalysisChecks.Equal(true, tip.Profile.TipNotJudged, "the profile carries the tip flag (the Cp_min ring and plate are not drawn)");
        AnalysisChecks.Equal(0, tip.Charts.SelectMany(c => c.Plots).SelectMany(p => p.Markers).Count(m => m.Name == "Cp_min"), "no Cp_min point on a tip station's charts");
        AnalysisChecks.Equal(Labels.CpLegend, tip.Charts.Single(c => c.Id == "cp").Legend, "the Cp chart legend carries no range: its low end is Cp_min");
        SectionView interior =SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric);
        AnalysisChecks.Equal(false, interior.Profile!.TipNotJudged, "an interior station is not flagged");
        AnalysisChecks.Equal(true, interior.Charts.SelectMany(c => c.Plots).SelectMany(p => p.Markers).Any(m => m.Name == "Cp_min"), "an interior station keeps its Cp_min point");
        StationTableRow row = tip.StationTable!.Single(r => r.Eta == 1.0);
        AnalysisChecks.Equal(Labels.TipNotJudged, row.Cavitation, "station-table cavitation word");
        AnalysisChecks.Equal(Labels.TipNotJudged, row.CpMin, "station-table -Cp_min cell: no number on a tip station");
        AnalysisChecks.Equal(false, tip.Profile.Cavitation!.Contains('σ') || tip.Profile.Cavitation.Any(char.IsDigit), "no sigma, no number");
        ResultGroup stations = tip.Groups.Single(g => g.Title == "Stations");
        AnalysisChecks.Equal(Labels.TipNotJudged, stations.Rows.Single(r => r.Label == "η 1").Value, "stations group -Cp_min cell");
        // the wing-level group stays the wing result, with its sigma
        AnalysisChecks.Equal(true, tip.Groups.Single(g => g.Title == "Cavitation").Rows.Single(r => r.Label == Labels.SigmaLabel).Value.Any(char.IsDigit), "wing sigma stays");
        SectionView inner = SectionDisplay.Build(run, tier, source, 0.5, "r1", null, null, default, Units.Metric);
        AnalysisChecks.Equal(true, inner.Profile!.Cavitation!.StartsWith("σ", StringComparison.Ordinal), "an interior station keeps its sigma line");
        AnalysisChecks.Equal(true, inner.StationTable!.Single(r => r.Eta == 0.5).CpMin != Labels.TipNotJudged, "interior -Cp_min stays a number");
    }
}
