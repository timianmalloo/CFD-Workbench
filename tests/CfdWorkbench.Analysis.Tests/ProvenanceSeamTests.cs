using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class ProvenanceSeamTests
{
    internal static void Run() => AnalysisChecks.Check("Provenance_RunInputsWaterAndChangedSince", Rows);

    private static void Rows()
    {
        using var session = Fixture.Opened();
        var wing = new FakeWing();
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, wing), Fixture.Op(2));
        CurrentInputs changed = Freshness.Current(session.Snapshot(), Fixture.Salt, Fixture.Op(3), wing.Method, wing.Settings);
        const string historical = "Historical — operating point changed (α 2.00° → 3.00°)";
        var view = AnalysisProjection.Build(run, changed, Units.Metric,
            new ProjectionContext(Revision: new RevisionLabel(4, "twist"), HistoricalText: historical));
        var rows = view.Groups.Single(group => group.Title == "Provenance").Rows.ToDictionary(row => row.Label, row => row.Value);
        if (!rows["Inputs"].Contains("r4", StringComparison.Ordinal) ||
            !rows["Inputs"].Contains(run.Inputs.SurfaceHash[..12], StringComparison.Ordinal) ||
            !rows["Inputs"].Contains(run.Inputs.ProfileHashes[0][..12], StringComparison.Ordinal) ||
            !rows["Inputs"].Contains(run.Inputs.Evaluator, StringComparison.Ordinal))
            throw new InvalidOperationException("run revision, surface, profiles or evaluator missing from Inputs");
        if (!rows["Water"].Contains(run.Water.Source, StringComparison.Ordinal))
            throw new InvalidOperationException("water source missing from Provenance");
        AnalysisChecks.Equal(historical, rows["Changed since"], "controller historical banner shared with Provenance");
    }
}
