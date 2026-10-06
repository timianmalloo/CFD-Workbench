using System.Diagnostics;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class SectionSeamTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Section_WingRun_PanelValuesAtEveryStation", WingRows);
        AnalysisChecks.Check("Section_ProvisionalAndProjectionRows", ProjectionRows);
        AnalysisChecks.Check("Section_SurfacePiercing_EstimatorUnavailable", Piercing);
    }

    private static void WingRows()
    {
        byte[] source = FoilSource.NewDefault();
        OperatingPoint op = Fixture.Op(3);
        // C-5: 97 complete half-wing stations exercise the every-station invariant without the default 129-station
        // fixture's measured 515 ms cost under the concurrent ring. The product default remains 129 stations.
        double[] etas = Settings.SpanEtas(48, "cosine");
        var watch = Stopwatch.StartNew();
        SectionTierResult section = SectionTier.Evaluate(source, etas, [], op, Fixture.Salt);
        watch.Stop();
        Console.WriteLine($"MEASURE warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms at {PanelMethod.DefaultPanelCount} panels");
        if (watch.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException("warm 200-panel whole-wing section tier exceeded 1 s");
        AnalysisChecks.Equal(etas.Length, section.Stations.Count, "all run stations sampled");
        if (section.Stations.Any(station => station.Estimate.Panel.StationCount != 200 ||
            !double.IsFinite(station.Estimate.Cl) || !double.IsFinite(station.Estimate.CmQuarter) ||
            !double.IsFinite(station.Estimate.AlphaL0Deg) || !double.IsFinite(station.Estimate.CdTurbulentBound) ||
            !double.IsFinite(station.LiftPerSpan)))
            throw new InvalidOperationException("a section station lacks a 200-panel estimate");
        if (!double.IsFinite(section.PanelUnderreadFraction))
            throw new InvalidOperationException("the governing-station two-grid delta was not measured");
        Console.WriteLine($"MEASURE governing-station eta {section.GoverningEta:F3} 200-vs-400 suction under-read {section.PanelUnderreadFraction:P3}");
    }

    private static void ProjectionRows()
    {
        AnalysisRun run = ProjectionTests.Data().Run;
        var panel = new PanelResult([], [], -1, PanelMethod.DefaultPanelCount, 0.3, -0.02);
        var estimate = new SectionEstimate(panel, 0, 0.01);
        CavitationResult cavitation = Cavitation.Screen(-1, PanelMethod.DefaultPanelCount, 0.5,
            run.Op.Speed, run.Water.Rho, run.Op.PAtm, run.Water.Pv, "η 0.5");
        var sample = new PolarSample("profile", "neuralfoil", "test", 5e5, 2, "clean", 2, "water",
            0.3, 0.01, -0.02, 0.4, 0.5, -1, 200, 0.9, true);
        var polar = new PolarResult(sample, [], false, 0.0001, 0.0002);
        var section = new SectionTierResult([new SectionStationResult(0.5, 2, 5e5, 0.5, estimate, cavitation)],
            cavitation, 0.5, 0.11) { PolarNcrit2 = polar, PolarNcrit4 = polar };
        IReadOnlyList<StripLoad> provisional = SectionTier.MarkGoverning(run.Strips,
            section with { GoverningEta = 0.25, PanelUnderreadFraction = 0.11 });
        AnalysisChecks.Equal(StripLoad.PanelUnderreadReason, provisional[1].ProvisionalReason, "over-10% station reason");
        var view = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: section));
        var rows = view.Groups.Single(group => group.Title == "Section (2D)").Rows;
        if (!rows.Any(row => row.Label == "Cl" && row.Value != Labels.NoPolar) ||
            !rows.Any(row => row.Label == "Cp_min" && row.Value != Labels.SectionCp))
            throw new InvalidOperationException("section projection still shows the A3a stub");
        foreach (string label in new[] { "Cl", "Cm_c/4", "α_L0", "Cp_min" })
            AnalysisChecks.Equal(PanelMethod.ModelLabel, rows.Single(row => row.Label == label).Note,
                label + " panel tier label");
        if (rows.Any(row => row.Label == "Cd") ||
            rows.Single(row => row.Label == "ANA-SECTION-ITTC1957-BOUND").Note is null)
            throw new InvalidOperationException("the turbulent estimator bound is labelled as polar Cd");
        foreach (string label in new[] { "Ncrit 2", "Ncrit 4", "CST residual", "analysis_confidence" })
            AnalysisChecks.Equal("XFOIL-class surrogate; accuracy relative to XFOIL, not experiment",
                rows.Single(row => row.Label == label).Note, label + " COPY-66");
    }

    private static void Piercing()
    {
        AnalysisRun run = ProjectionTests.Data().Run;
        var panel = new PanelResult([], [], -1, 200, 0.3, -0.02);
        var estimate = new SectionEstimate(panel, 0, 0.01);
        CavitationResult cavitation = Cavitation.Screen(-1, 200, -0.1, run.Op.Speed,
            run.Water.Rho, run.Op.PAtm, run.Water.Pv, "η 0.5");
        var station = new SectionStationResult(0.5, 2, 5e5, -0.1, estimate, cavitation);
        if (station.EstimatorAvailabilityCode != Cavitation.SurfacePiercing)
            throw new InvalidOperationException("surface-piercing station did not flag estimator unavailable");
        var tier = new SectionTierResult([station], cavitation, 0.5, 0);
        var projected = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: tier));
        if (projected.Groups.Single(group => group.Title == "Section (2D)").Rows.Single(row => row.Label == "Cl").Value !=
            Cavitation.SurfacePiercing)
            throw new InvalidOperationException("surface-piercing estimator still projected a number");
    }
}
