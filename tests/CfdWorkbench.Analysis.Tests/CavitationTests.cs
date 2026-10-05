using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class CavitationTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Cavitation_ScreenString_NamesStationCount", String);
        AnalysisChecks.Check("Cavitation_Margin15Percent_Applied", Margin);
        AnalysisChecks.Check("Cavitation_NegCpMinNonPositive_Undefined", Undefined);
        AnalysisChecks.Check("Cavitation_PvOrDepthMissing_Unavailable", Unavailable);
        AnalysisChecks.Check("Cavitation_GoverningStation_AtAlphaEffAndLocalDepth", Governing);
    }

    private static void String()
    {
        CavitationResult result = Cavitation.Screen(-2, 200, 0.4, 5, 1000, 101325, 1700, "η 0.25");
        const string copy = "Cavitation screening (sheet, by −Cp_min): inception is possible above V_crit; not a prediction of inception, extent, tip-vortex or cloud cavitation; Cp_min resolution: 200 stations";
        AnalysisChecks.Equal(copy, result.ScreenText, "COPY-48 and N");
    }

    private static void Margin()
    {
        double speed = 5, rho = 1000, pv = 1700, depth = 0.4, negCp = 2;
        double sigma = 1.10 * negCp;
        double pAtm = pv + sigma * 0.5 * rho * speed * speed - rho * OperatingPoints.Gravity * depth;
        CavitationResult result = Cavitation.Screen(-negCp, 100, depth, speed, rho, pAtm, pv, "η 0");
        AnalysisChecks.Equal(CavitationState.InsideMargin, result.State, "15% margin at 1.10 × −Cp_min");
        AnalysisChecks.Equal(0.15, result.MarginFraction, "default margin");
        if (!result.MarginLabel.Contains("practitioner assumption, not sourced", StringComparison.Ordinal))
            throw new InvalidOperationException("margin provenance omitted");
    }

    private static void Undefined()
    {
        foreach (double cp in new[] { 0.0, 0.1 })
        {
            CavitationResult result = Cavitation.Screen(cp, 100, 0.4, 5, 1000, 101325, 1700, "η 0");
            AnalysisChecks.Equal(CavitationState.Undefined, result.State, "−Cp_min ≤ 0");
            if (result.CriticalSpeed is not null || result.Sigma is not null)
                throw new InvalidOperationException("Undefined carried a number");
        }
    }

    private static void Unavailable()
    {
        foreach (CavitationResult result in new[] {
            Cavitation.Screen(-2, 100, 0.4, 5, 1000, 101325, null, "η 0"),
            Cavitation.Screen(-2, 100, null, 5, 1000, 101325, 1700, "η 0") })
        {
            AnalysisChecks.Equal(CavitationState.Unavailable, result.State, "missing input");
            if (result.CriticalSpeed is not null || result.Sigma is not null)
                throw new InvalidOperationException("Unavailable carried a number");
        }
    }

    private static void Governing()
    {
        SectionSample basic = SectionEstimatorTests.Section(0, 0.08, 100);
        StationFrame root = basic.Frame;
        SectionSample outer = basic with { Frame = root with { Eta = 0.75, ElevationMeters = 0.25 } };
        var stations = new[] { new CavitationStation(basic, 0), new CavitationStation(outer, 8) };
        OperatingPoint op = OperatingPoints.Custom(5, 0, 0.5);
        CavitationResult wing = Cavitation.ScreenWing(stations, op, 1000, 1700, root);
        PanelResult rootCp = PanelMethod.Solve(basic, 0);
        PanelResult outerCp = PanelMethod.Solve(outer, 8);
        if (!(outerCp.CpMin < rootCp.CpMin)) throw new InvalidOperationException("fixture has no outer governing peak");
        AnalysisChecks.Equal("η 0.75", wing.GoverningStation, "governing station");
        if (wing.GoverningDepth is null || Math.Abs(wing.GoverningDepth.Value - 0.25) > 1e-8)
            throw new InvalidOperationException("local depth was not 0.25 m");
        if (wing.CpMin is null || Math.Abs(wing.CpMin.Value - outerCp.CpMin) > 1e-10)
            throw new InvalidOperationException("Cp did not use α_eff");
    }
}
