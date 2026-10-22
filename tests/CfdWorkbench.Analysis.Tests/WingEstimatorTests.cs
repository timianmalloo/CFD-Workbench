using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// A3d-1 (design a3d-compare.md §2.6, §7.0; Ruling 250 Q1): the wing estimator tier. The two theory rows pin the
/// Helmbold-Diederich slope; the row checks pin the tier-keyed diagnostics rule; the service check pins idempotence.
/// </summary>
internal static class WingEstimatorTests
{
    private const double Tolerance = 1e-9;

    internal static void Run()
    {
        Check("WingEstimator_HelmboldDiederich_SlopeAndInducedDrag", SlopeAndInducedDrag);
        Check("EstimatorRun_Completed_NoDiagnostics_Accepted", EstimatorCompletedAccepted);
        Check("VlmRun_Completed_NoDiagnostics_StillRefused", VlmCompletedWithoutDiagnosticsRefused);
        Check("EstimatorRun_Failed_WithDiagnostics_Refused", EstimatorFailedWithDiagnosticsRefused);
        Check("UnknownTier_Refused", UnknownTierRefused);
        Check("Service_EvaluateEstimator_RecordsIdempotent", ServiceIdempotent);
    }

    private static void Near(double expected, double actual, string what)
    {
        if (Math.Abs(expected - actual) > Tolerance)
            throw new InvalidOperationException($"{what} expected {expected:F10}; actual {actual:F10}");
    }

    // Row 1 is the Helmbold slope (no sweep) and the CL and CDi at 5 degrees above the zero-lift angle; row 2 is the
    // hand computation at a 20 degree mid-chord sweep, the row that fails when the sweep term is dropped.
    private static void SlopeAndInducedDrag()
    {
        Near(4.8272350086, WingEstimator.LiftSlopePerRad(7.5, 0), "CL_alpha at A 7.5, sweep 0");
        var outputs = WingEstimator.Outputs(7.5, 0, 5);
        Near(0.4212557233, outputs.Cl, "CL");
        Near(0.0075314829, outputs.Cdi, "CDi = CL^2 / (pi A)");
        Equal(1.0, outputs.E, "e is 1 by construction;");
        Near(4.6072951356, WingEstimator.LiftSlopePerRad(7.5, 20), "CL_alpha at A 7.5, sweep 20");
    }

    private static void EstimatorCompletedAccepted()
    {
        using var session = Fixture.Opened();
        session.RecordRun(Row(session, "estimator", new RunOutcome.Completed(), null));
        Equal(RunIntegrity.Intact, session.ReadRuns().Runs.Single().Integrity, "the estimator row reads back intact");
        using var reopened = new AuthoringSession();
        reopened.Reopen(session.SaveImage());
        Equal(RunIntegrity.Intact, reopened.ReadRuns().Runs.Single().Integrity, "and after reopen");
    }

    private static void VlmCompletedWithoutDiagnosticsRefused()
    {
        using var session = Fixture.Opened();
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "vlm-strip", new RunOutcome.Completed(), null)));
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "vlm-strip", new RunOutcome.Failed("ANA-X", "x"), new RunDiagnostics(0, 0))));
        session.RecordRun(Row(session, "vlm-strip", new RunOutcome.Completed(), new RunDiagnostics(1e-13, 42.5)));
    }

    private static void EstimatorFailedWithDiagnosticsRefused()
    {
        using var session = Fixture.Opened();
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "estimator", new RunOutcome.Failed("ANA-X", "x"), new RunDiagnostics(0, 0))));
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "estimator", new RunOutcome.Completed(), new RunDiagnostics(1e-13, 42.5))));
    }

    private static void UnknownTierRefused()
    {
        using var session = Fixture.Opened();
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "polar-wing", new RunOutcome.Completed(), new RunDiagnostics(1e-13, 42.5))));
        Refuses("DOC-SCHEMA", () => session.RecordRun(Row(session, "polar-wing", new RunOutcome.Completed(), null)));
    }

    private static void ServiceIdempotent()
    {
        using var session = Fixture.Opened();
        var service = new AnalysisService(session, new FakeWing());
        AnalysisRun Evaluate() => service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.Estimator, new Scope.Wing(),
            CancellationToken.None).GetAwaiter().GetResult();
        var first = Evaluate();
        var second = Evaluate();
        Equal(first.RunId, second.RunId, "the second call returns the recorded row;");
        Equal(1, session.ReadRuns().Runs.Count, "rows");
        Equal("estimator", first.Tier, "tier id");
        Equal(WingEstimator.Method, first.Method, "method identity");
        Equal(true, first.Outcome is RunOutcome.Completed, "completed;");
        Equal(true, first.Diagnostics is null, "no diagnostics;");
        Equal(0, first.Strips.Count, "strips");
        Equal("estimator-envelope/1", first.Settings.Envelope, "envelope");
        Equal(RunIntegrity.Intact, session.ReadRuns().Runs.Single().Integrity, "intact");
        Equal(Labels.EstimatorNotValidated, "estimator, not validated", "the A3D-COPY-27 label;");
        Equal(true, WingEstimator.Derive(first, session.Snapshot().Source) is { Cl: > 0, E: 1.0 }, "outputs derive from the row;");
    }

    private static AnalysisRun Row(AuthoringSession session, string tier, RunOutcome outcome, RunDiagnostics? diagnostics)
    {
        var snapshot = session.Snapshot();
        var profiles = FoilSource.Parse(snapshot.Source).Authored().Assignments.Select(assignment => assignment.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, "cfdw-cv/2", "rule-a/1");
        var settings = new RunSettings(0, 0, "none", "none", 0, "none", 0, tier == "estimator" ? "estimator-envelope/1" : "vlm-envelope/1",
            null, [2, 4], "smooth", 0.3);
        string settingsHash = RunRecord.SettingsHash(settings);
        var method = new RunMethod(tier == "estimator" ? "cfdw.estimator.wing" : "cfdw.vlm-strip", "1.0.0", 1);
        var op = Fixture.Op(2.0);
        string key = RunRecord.Key(inputs, Fixture.Salt, op, method, settingsHash);
        var run = new AnalysisRun(Fixture.Id(), key, "", outcome, tier, method, settings, settingsHash, inputs, Fixture.Salt, op,
            new RunReference(0.12, 1.0, 0.12, "frame-origin", "body"), 0.01, [], 1.5, new RunPlatform("osx", "arm64", "10.0"), diagnostics);
        return run with { ContentHash = RunRecord.ContentHash(run) };
    }

    private static void Refuses(string code, Action action)
    {
        try { action(); }
        catch (ContractError error) { Equal(code, error.Code, "refusal code"); return; }
        throw new InvalidOperationException("expected refusal " + code + "; none");
    }
}
