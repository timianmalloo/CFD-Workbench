using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Ruling 142: a tip-provisional strip never decides the wing cavitation verdict, and a tip station's screen reads Not judged.
/// Ring: every push (Analysis harness, group TipCavitation). Cost: about 1.5 s, one shared lattice run (SectionForceTests.CamberedRun).
/// </summary>
internal static class TipCavitationTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("TipCavitation_PlantedTipStation_NeverGovernsTheWing", PlantedTipNeverGoverns);
        AnalysisChecks.Check("TipCavitation_EveryStationTip_WingLineIsNotJudged", AllTip);
        AnalysisChecks.Check("TipCavitation_Exclusion_WingLineStatesCountAndJudgedStations", WingLineSuffix);
        AnalysisChecks.Check("TipCavitation_TipStationDisplay_NoSigmaAndNoCpMinNumber", TipStationDisplay);
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

    private static void TipStationDisplay()
    {
        (AnalysisRun run, byte[] source, SectionTierResult tier) = Cambered();
        SectionView tip = SectionDisplay.Build(run, tier, source, 1.0, "r1", null, null, default, Units.Metric);
        ResultGroup estimator = tip.Groups.Single(g => g.Title == "Estimator");
        AnalysisChecks.Equal(Labels.TipNotJudged, estimator.Rows.Single(r => r.Label == Labels.CpMinLabel).Value, "estimator -Cp_min row");
        AnalysisChecks.Equal(Labels.TipNotJudged, tip.Profile!.Cavitation, "profile cavitation line");
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
