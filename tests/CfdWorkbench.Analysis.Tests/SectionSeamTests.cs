using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class SectionSeamTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Section_WingRun_PanelValuesAtEveryStation", WingRows);
    }

    private static void WingRows()
    {
        using var session = Fixture.Opened();
        var wing = new FakeWing { Settings = Settings.Default };
        OperatingPoint op = Fixture.Op(3);
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, wing), op);
        byte[] source = session.AcceptedSourceOf(run.Inputs.AcceptedId)!;
        SectionTierResult section = SectionTier.Derive(run, source);
        AnalysisChecks.Equal(Settings.Default.SectionEtas!.Count, section.Stations.Count, "all run stations sampled");
        if (section.Stations.Any(station => station.Estimate.Panel.StationCount != 200 ||
            !double.IsFinite(station.Estimate.Cl) || !double.IsFinite(station.Estimate.CmQuarter) ||
            !double.IsFinite(station.Estimate.AlphaL0Deg) || !double.IsFinite(station.Estimate.CdTurbulentBound)))
            throw new InvalidOperationException("a section station lacks a 200-panel estimate");
        if (section.PanelUnderreadFraction is not >= 0)
            throw new InvalidOperationException("the governing-station two-grid delta was not measured");
        CurrentInputs current = Freshness.Current(session.Snapshot(), Fixture.Salt, op, wing.Method, wing.Settings);
        var view = AnalysisProjection.Build(run, current, Units.Metric, new ProjectionContext(Source: source));
        var rows = view.Groups.Single(group => group.Title == "Section (2D)").Rows;
        if (!rows.Any(row => row.Label == "Cl" && row.Value != Labels.NoPolar) ||
            !rows.Any(row => row.Label == "Cp_min" && row.Value != Labels.SectionCp))
            throw new InvalidOperationException("section projection still shows the A3a stub");
    }
}
