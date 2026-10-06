using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class TipPolarTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Tip_LowRe_TipReasonPrecedesReUnavailable", TipPrecedence);
        AnalysisChecks.Check("Tip_ConsistencyEdge_RecomputedWithPolarSlope", MeasuredEdge);
        AnalysisChecks.Check("Tip_ConfidenceNeverClearsNotJudged", Confidence);
    }

    private static void TipPrecedence()
    {
        var (run, _) = ProjectionTests.Data(s => s.J == 0 ? s with
        {
            Provisional = true, ProvisionalReason = StripLoad.TipProvisionalReason,
            ReLocal = 170000, CdNcrit2 = new StripValue(null, "ANA-POLAR-RE-OUTSIDE")
        } : s);
        var view = ProjectionTests.View(run);
        var rows = AnalysisProjection.StripAt(view, run.Strips[0].Eta).Rows.ToList();
        int tip = rows.FindIndex(row => row.Label == "Envelope (this strip)");
        int cd = rows.FindIndex(row => row.Label == "cd (profile)");
        if (tip < 0 || cd < 0 || tip >= cd || rows[tip].Value != Labels.TipNotJudged ||
            rows[cd].Value != "ANA-POLAR-RE-OUTSIDE")
            throw new InvalidOperationException("low-Re tip did not retain its first not-judged reason and Re flag");
    }

    private static void MeasuredEdge()
    {
        PolarConsistencyInput[] strips =
        [
            new(0.1, 5, 0.50, 0.50, 0.10, 0, false, 0.99),
            new(0.4, 5, 0.55, 0.55, 0.11, 0, false, 0.99),
            new(0.75, 5, 0.44, 0.50, 0.10, 0, false, 0.99),
            new(0.95, 5, 0.30, 0.50, 0.10, 0, false, 0.99)
        ];
        PolarConsistencyResult measured = TipPolarConsistency.Evaluate(strips);
        AnalysisChecks.Equal(0.75, measured.EdgeEta, "first measured q < 0.9");
        AnalysisChecks.Equal("ANA-POLAR-CONSISTENCY-EXEMPT", measured.Strips[2].Code, "inside measured edge");
        PolarConsistencyInput[] changedSlope = strips.ToArray();
        changedSlope[2] = changedSlope[2] with { PolarSlopePerDeg = 0.08 };
        PolarConsistencyResult moved = TipPolarConsistency.Evaluate(changedSlope);
        AnalysisChecks.Equal(0.95, moved.EdgeEta, "polar slope moves the measured edge");
    }

    private static void Confidence()
    {
        PolarConsistencyInput[] strips =
        [new(0.1, 5, 0.5, 0.5, 0.1, 0, false, 0.99),
         new(0.9, 5, 0.3, 0.5, 0.1, 0, true, 0.99)];
        PolarConsistencyResult result = TipPolarConsistency.Evaluate(strips);
        AnalysisChecks.Equal("ANA-TIP-PROVISIONAL", result.Strips[1].Code, "high confidence does not clear tip");
    }
}
