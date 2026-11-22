using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Node SET-2 red-first tests (T11 in docs/design/setup-seeding.md §7, Ruling 272):
/// Exact GOAL-02 7-point + shallow-depth (h_ref = 0.3 m) oracle verification in CL (3 dec) and sigma (2 dec),
/// honest Unavailable in polar-dependent feasibility cells, and WaterTable alignment with Core DefaultSaltWater15C.
/// </summary>
internal static class SetupFeasibilityTests
{
    internal static void Run()
    {
        Check("GoalFeasibility_Goal02_SevenPointsAndShallowDepth_ExactOracle", T11_Goal02SevenPointsOracle);
        Check("GoalFeasibility_WaterAlignment_CoreMatchesAnalysisWaterTable", T11b_WaterAlignment);
    }

    // T11 (GOAL-02, DR-SET-4): Pinned race light (10, 14, 18 kn; S = 1000 cm2, b = 1.10 m) and race strong
    // (15, 22, 28, 32 kn; S = 750 cm2, b = 0.95 m) at h_ref = 0.5 m plus 10 kn at h_ref = 0.3 m match exact
    // CL (3 dec) and sigma (2 dec) oracle; shallow h_ref = 0.3 m flags h/c < 5; polar-dependent cells read Unavailable.
    // Mutant: using fresh water rho = 999.10 or dropping rho*g*h hydrostatic term turns this RED.
    private static void T11_Goal02SevenPointsOracle()
    {
        var water = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        var res = DisciplinePresets.ResolvePurpose("wingfoil-race");

        // Race light wing: S = 0.100 m2 (1000 cm2), b = 1.10 m, c_bar = 0.090909... m, W = 1050 N.
        var lightWing = MakeWing(areaM2: 0.100, spanM: 1.10, tc: 0.10);
        GoalOperatingPoint[] lightPts =
        [
            Pt("op-1", "P1 take-off", 10.0, 0.5, 1050.0, "takeoff-cl"),
            Pt("op-1s", "P1 shallow", 10.0, 0.3, 1050.0, "takeoff-cl"),
            Pt("op-2", "P2 upwind", 14.0, 0.5, 1050.0, "cruise-ld"),
            Pt("op-3", "P3 downwind", 18.0, 0.5, 1050.0, "cruise-ld")
        ];
        var lightGoal = DisciplinePresets.BuildGoalState(
            Guid.NewGuid().ToString("D"), null, null, res,
            DisciplinePresets.DefaultSoftTargets(res.Preset),
            water: water) with { OperatingPoints = lightPts };

        var lightReport = GoalFeasibility.Evaluate(lightWing, lightGoal);
        AssertPoint(lightReport.Points[0], expectedCl3: 0.773, expectedSigma2: 7.71, expectShallow: false);
        AssertPoint(lightReport.Points[1], expectedCl3: 0.773, expectedSigma2: 7.56, expectShallow: true);
        AssertPoint(lightReport.Points[2], expectedCl3: 0.395, expectedSigma2: 3.93, expectShallow: false);
        AssertPoint(lightReport.Points[3], expectedCl3: 0.239, expectedSigma2: 2.38, expectShallow: false);

        // Race strong wing: S = 0.075 m2 (750 cm2), b = 0.95 m, c_bar = 0.078947... m, W = 1050 N.
        var strongWing = MakeWing(areaM2: 0.075, spanM: 0.95, tc: 0.10);
        GoalOperatingPoint[] strongPts =
        [
            Pt("op-4", "P4 post-takeoff", 15.0, 0.5, 1050.0, "cruise-ld"),
            Pt("op-5", "P5 upwind", 22.0, 0.5, 1050.0, "cruise-ld"),
            Pt("op-6", "P6 downwind", 28.0, 0.5, 1050.0, "cavitation-margin"),
            Pt("op-7", "P7 top end", 32.0, 0.5, 1050.0, "cavitation-margin")
        ];
        var strongGoal = lightGoal with { OperatingPoints = strongPts };
        var strongReport = GoalFeasibility.Evaluate(strongWing, strongGoal);
        AssertPoint(strongReport.Points[0], expectedCl3: 0.458, expectedSigma2: 3.43, expectShallow: false);
        AssertPoint(strongReport.Points[1], expectedCl3: 0.213, expectedSigma2: 1.59, expectShallow: false);
        AssertPoint(strongReport.Points[2], expectedCl3: 0.132, expectedSigma2: 0.98, expectShallow: false);
        AssertPoint(strongReport.Points[3], expectedCl3: 0.101, expectedSigma2: 0.75, expectShallow: false);

        // Polar-dependent cells MUST honestly report Unavailable (never 0, never guessed, DR-SET-4 (a)).
        foreach (var ptEval in lightReport.Points.Concat(strongReport.Points))
        {
            var cavCells = ptEval.Cells.Where(c => c.ConstraintKind == "cavitation-margin").ToArray();
            Equal(2, cavCells.Length); // Ncrit = 2 and 4
            foreach (var cell in cavCells)
            {
                Equal(FeasibilityCellStatus.Unavailable, cell.Status);
                Equal(Labels.SetupPolarUnavailable, cell.DisplayText);
            }
            var loadCells = ptEval.Cells.Where(c => c.ConstraintKind == "required-load").ToArray();
            Equal(2, loadCells.Length);
            foreach (var cell in loadCells)
            {
                Equal(FeasibilityCellStatus.Unavailable, cell.Status);
                Equal(Labels.SetupPolarUnavailable, cell.DisplayText);
            }
        }
    }

    private static void T11b_WaterAlignment()
    {
        var analysisSalt = WaterTable.At(15.0, OperatingPoints.SaltSalinityGPerKg);
        Equal(analysisSalt, DisciplinePresets.DefaultSaltWater15C);
        var analysisFresh = WaterTable.At(15.0, 0.0);
        Equal(analysisFresh, DisciplinePresets.DefaultFreshWater15C);
    }

    private static void AssertPoint(GoalOperatingPointEvaluation eval, double expectedCl3, double expectedSigma2, bool expectShallow)
    {
        Equal(true, eval.ClRequired.HasValue);
        Equal(true, eval.Derived.Sigma.Value.HasValue);
        double actualCl3 = Math.Round(eval.ClRequired!.Value, 3, MidpointRounding.AwayFromZero);
        double actualSigma2 = Math.Round(eval.Derived.Sigma.Value!.Value, 2, MidpointRounding.AwayFromZero);
        Equal(expectedCl3, actualCl3);
        Equal(expectedSigma2, actualSigma2);
        Equal(expectShallow, eval.ShallowWaterNotice);
        var hcCell = eval.Cells.Single(c => c.ConstraintKind == "deep-water-floor");
        Equal(expectShallow ? FeasibilityCellStatus.NotMet : FeasibilityCellStatus.Met, hcCell.Status);
    }

    private static GoalOperatingPoint Pt(string id, string label, double knots, double hRef, double loadN, string metric) =>
        new(id, label, knots * DisciplinePresets.KnotsToMps, hRef, OperatingPoints.RootLeadingEdge,
            OperatingPoints.StandardAtmosphere, loadN, "Front-wing load", metric, [2, 4], "Flagged");

    private static WingEstimates MakeWing(double areaM2, double spanM, double tc)
    {
        double mean = areaM2 / spanM;
        return new WingEstimates(spanM, mean / 0.81, mean * 0.24 / 0.81, mean, mean * 1.05, tc,
            spanM * spanM / areaM2, areaM2, "accepted", 0, true, new("test", 0, 1, 1, "ok"));
    }
}
