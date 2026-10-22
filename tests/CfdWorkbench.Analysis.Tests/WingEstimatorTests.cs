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
        Check("WingEstimator_Derive_SweptCamberedTwisted_HandValues", DeriveHandValues);
        Check("WingEstimator_Derive_ExampleFoilWashout_LowersCl", ExampleFoilWashout);
        Check("Service_EvaluateEstimator_UncertifiedGeometry_RefusedNothingRecorded", UncertifiedRefused);
        Check("Service_EvaluateEstimator_FailedPath_RecordsFailedRowNoDiagnostics", FailedPath);
        Check("Service_EvaluateEstimator_DoesNotCancelVlmInFlight", DoesNotCancelVlm);
    }

    // The swept, cambered, twisted fixture, built so every planform number is exact by hand. The rails and the twist are
    // cubic B-splines whose control abscissae are the Greville points of their knots, so each curve is exactly linear in eta:
    //   leading edge 200 eta mm, trailing edge (120 + 300 eta) mm, half span 450 mm, twist -4 eta degrees (nose-up positive).
    //   chord c(eta) = 120 + 100 eta mm. Mid-chord x(eta) = 60 + 250 eta mm, so tan(mid-chord sweep) = 250 / 450 = 5/9
    //   (the leading-edge line would give 200 / 450 = 4/9). Planform area = 0.9 m x (0.120 + 0.050) m = 0.153 m^2, so
    //   A = 0.81 / 0.153 = 5.2941176471. Stations eta 0, 0.5, 1 have chords 120, 170, 220 mm and twists 0, -2, -4 degrees:
    //   chord-weighted mean twist = (0 - 340 - 880) / 510 = -2.3921568627 degrees (unweighted: -2). One cambered profile
    //   at every station, so the zero-lift angle is the same at each (asserted), taken from the section estimator.
    //   CL = CLalpha (alpha - (alphaL0 - twistMean)) with CLalpha = 2 pi A / (2 + sqrt(A^2 (1 + 25/81) + 4)).
    private static byte[] SweptFoil()
    {
        double[] greville = [0, 1.0 / 12, 0.25, 0.5, 0.75, 11.0 / 12, 1];
        string Pts(Func<double, double> y) => string.Join(", ", greville.Select(g =>
            "(" + g.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ", " + y(g).ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ")"));
        const string knots = "[0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1]";
        string text = "foildsl \"4.0\"\nfoil \"Swept\" {\n  units mm\n  half_span 450 mm\n  evaluator \"cfdw-cv\" \"2\"\n  symmetry mirror_y\n  planform {\n" +
            $"    leading cv {{ degree 3 knots {knots} points [{Pts(g => 200 * g)}] }}\n" +
            $"    trailing cv {{ degree 3 knots {knots} points [{Pts(g => 120 + 300 * g)}] }}\n  }}\n" +
            $"  dihedral cv {{ degree 3 knots {knots} points [{Pts(_ => 0)}] }}\n" +
            $"  twist cv {{ degree 3 knots {knots} points [{Pts(g => -4 * g)}] }}\n" +
            $"  thickness cv {{ degree 3 knots {knots} points [{Pts(_ => 0.12)}] }}\n" +
            "  profiles {\n    profile \"section-a\" {\n" +
            "      upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.035), (0.15, 0.065), (0.35, 0.075), (0.55, 0.06), (0.75, 0.04), (0.9, 0.02), (1, 0)] }\n" +
            "      lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.015), (0.15, -0.045), (0.35, -0.055), (0.55, -0.04), (0.75, -0.02), (0.9, 0), (1, 0)] }\n" +
            "    }\n  }\n  sections { at root profile \"section-a\" at tip profile \"section-a\" }\n}\n";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    private static AnalysisRun DeriveRow(double alphaDeg, double[] etas)
    {
        var settings = new RunSettings(0, 0, "none", "none", 0, "none", 0, WingEstimator.Envelope, null, [], "smooth", 0.3,
            SectionEtas: etas, SectionXs: [0, 1]);
        string hash = new('a', 64);
        return new AnalysisRun(Fixture.Id(), hash, hash, new RunOutcome.Completed(), WingEstimator.TierId, WingEstimator.Method, settings,
            hash, new RunInputs(Fixture.Id(), hash, [hash], "cfdw-cv/2", "rule-a/1"), Fixture.Salt, Fixture.Op(alphaDeg),
            new RunReference(0.153, 0.9, 0.17, "frame-origin", "body"), 0.01, [], 1, new RunPlatform("osx", "arm64", "10.0"));
    }

    private static void DeriveHandValues()
    {
        byte[] source = SweptFoil();
        double[] etas = [0, 0.5, 1];
        double[] alphaL0 = etas.Select(eta => SectionEstimator.Estimate(source, eta, 0, 1e6, PanelMethod.DefaultPanelCount).AlphaL0Deg).ToArray();
        Near(alphaL0[0], alphaL0[1], "one profile, one zero-lift angle (root, mid)");
        Near(alphaL0[0], alphaL0[2], "one profile, one zero-lift angle (root, tip)");
        Equal(true, alphaL0[0] < -0.5, "a positively cambered section lifts at zero angle, so alphaL0 < 0;");
        double aspect = 0.81 / 0.153;
        double slope = 2 * Math.PI * aspect / (2 + Math.Sqrt(aspect * aspect * (1 + 25.0 / 81) + 4));
        double twistMean = -1220.0 / 510;
        const double alpha = 4.0;
        double expected = slope * (alpha - (alphaL0[0] - twistMean)) * Math.PI / 180;
        var outputs = WingEstimator.Derive(DeriveRow(alpha, etas), source);
        Near(expected, outputs.Cl, "CL from the hand-computed sweep, chord-weighted twist and zero-lift angle");
        Near(expected * expected / (Math.PI * aspect), outputs.Cdi, "CDi");
        Equal(1.0, outputs.E, "e;");
        Near(slope, WingEstimator.LiftSlopePerRad(aspect, Math.Atan(5.0 / 9) * 180 / Math.PI), "the hand slope is the slope at tan 5/9");
    }

    // The example foil washes out to -2 degrees at the tip and has a constant 120 mm chord: the estimator CL falls below the
    // same wing without twist by CLalpha times the (equal-weight) mean twist of the stations.
    private static void ExampleFoilWashout()
    {
        string text = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(
            Path.Combine(StripFixtureTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil")));
        byte[] twisted = System.Text.Encoding.UTF8.GetBytes(text);
        int at = text.IndexOf("  twist cv", StringComparison.Ordinal);
        int end = text.IndexOf('\n', at);
        byte[] flat = System.Text.Encoding.UTF8.GetBytes(text[..at] +
            "  twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }" + text[end..]);
        double[] etas = [0, 0.5, 1];
        double mean = Placement.Sections(twisted, etas, [0, 1], CancellationToken.None).Average(section => section.Frame.TwistDegrees);
        Equal(true, mean < -0.5, "the example foil washes out;");
        double lowered = WingEstimator.Derive(DeriveRow(4, etas), twisted).Cl;
        double untwisted = WingEstimator.Derive(DeriveRow(4, etas), flat).Cl;
        double slope = WingEstimator.LiftSlopePerRad(WingEstimates.From(twisted, "accepted", 0).AspectRatio, 0);
        Equal(true, lowered < untwisted, "washout lowers CL;");
        Near(slope * mean * Math.PI / 180, lowered - untwisted, "the CL change is CLalpha times the mean twist");
    }

    private static void UncertifiedRefused()
    {
        using var session = Fixture.OpenedNotAssessed();
        var service = new AnalysisService(session, new FakeWing());
        var error = Fixture.Throws<ContractError>(service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.Estimator, new Scope.Wing(), CancellationToken.None));
        Equal("DSL-NOT-ASSESSED", error.Code, "refusal");
        Equal(0, session.ReadRuns().Runs.Count, "rows");
    }

    private static void FailedPath()
    {
        // No certified wing makes the derivation fail, so the derivation is the service's one internal seam: it throws an
        // ANA- code, and the service records a Failed row (no diagnostics, no strips) that reads back intact. A non-ANA
        // error is not a Failed row: it propagates.
        using var session = Fixture.Opened();
        var service = new AnalysisService(session, new FakeWing())
        {
            EstimatorDerivation = (_, _, _) => throw new ContractError("ANA-INPUT-ESTIMATOR", "forced")
        };
        var run = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.Estimator, new Scope.Wing(), CancellationToken.None).GetAwaiter().GetResult();
        Equal(true, run.Outcome is RunOutcome.Failed { Code: "ANA-INPUT-ESTIMATOR" }, "the row failed with the code;");
        Equal(true, run.Diagnostics is null, "no diagnostics;");
        Equal(0, run.Strips.Count, "strips");
        Equal(RunIntegrity.Intact, session.ReadRuns().Runs.Single().Integrity, "intact");
        Equal(1, Fixture.RunEvents(session).Count(item => item.Outcome == "ANA-INPUT-ESTIMATOR"), "an analysis.run event with the code;");
    }

    private static void DoesNotCancelVlm()
    {
        using var session = Fixture.Opened();
        var barrier = new HoldFirst();
        var wing = new FakeWing();
        var service = new AnalysisService(session, wing, barrier);
        var vlm = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.VlmStrip, new Scope.Wing(), CancellationToken.None);
        barrier.AwaitHeld();
        var estimator = service.EvaluateAsync(Fixture.Op(2.0), Fixture.Salt, Tier.Estimator, new Scope.Wing(), CancellationToken.None).GetAwaiter().GetResult();
        Equal(false, wing.Tokens.First().IsCancellationRequested, "the VLM compute token was cancelled by the estimator;");
        barrier.Release();
        var recorded = vlm.GetAwaiter().GetResult();
        var rows = session.ReadRuns().Runs;
        Equal(2, rows.Count, "rows");
        Equal(true, rows.Any(row => row.Run.RunId == recorded.RunId && row.Run.Tier == "vlm-strip"), "the VLM run recorded;");
        Equal(true, rows.Any(row => row.Run.RunId == estimator.RunId), "the estimator run recorded;");
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
