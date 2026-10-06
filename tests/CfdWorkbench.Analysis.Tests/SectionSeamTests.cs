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
        var watch = Stopwatch.StartNew();
        SectionTierResult section = SectionTier.Evaluate(source, Settings.Default.SectionEtas!, [], op, Fixture.Salt);
        watch.Stop();
        Console.WriteLine($"MEASURE warm whole-wing section tier {watch.Elapsed.TotalMilliseconds:F3} ms at {PanelMethod.DefaultPanelCount} panels");
        if (watch.Elapsed.TotalMilliseconds > 1000) throw new InvalidOperationException("warm 200-panel whole-wing section tier exceeded 1 s");
        AnalysisChecks.Equal(Settings.Default.SectionEtas!.Count, section.Stations.Count, "all run stations sampled");
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
        var section = new SectionTierResult([new SectionStationResult(0.5, 2, 5e5, 0.5, estimate, cavitation)],
            cavitation, 0.5, 0.11);
        IReadOnlyList<StripLoad> provisional = SectionTier.MarkGoverning(run.Strips,
            section with { GoverningEta = 0.25, PanelUnderreadFraction = 0.11 });
        AnalysisChecks.Equal(StripLoad.PanelUnderreadReason, provisional[1].ProvisionalReason, "over-10% station reason");
        var view = AnalysisProjection.Build(run, ProjectionTests.Current(run), Units.Metric,
            new ProjectionContext(SectionTier: section));
        var rows = view.Groups.Single(group => group.Title == "Section (2D)").Rows;
        if (!rows.Any(row => row.Label == "Cl" && row.Value != Labels.NoPolar) ||
            !rows.Any(row => row.Label == "Cp_min" && row.Value != Labels.SectionCp))
            throw new InvalidOperationException("section projection still shows the A3a stub");
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
