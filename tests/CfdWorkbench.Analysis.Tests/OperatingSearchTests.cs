using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class OperatingSearchTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("FindAlpha_TargetCL_RootWithinTolerance", TargetAlpha);
        AnalysisChecks.Check("FindAlpha_NoRoot_UnavailableWithReason", NoRoot);
        AnalysisChecks.Check("FindAlpha_ZeroLift_BracketedRoot", ZeroLift);
        AnalysisChecks.Check("FindTakeoff_RetrievesLoadAtEachSpeed", Takeoff);
        AnalysisChecks.Check("FreeSurface_A52_FactorsBesideDeepWater", Correction);
        AnalysisChecks.Check("FreeSurface_A52_EachEnvelopeAxis", CorrectionEnvelope);
    }

    private static void TargetAlpha()
    {
        using var session = Fixture.Opened();
        byte[] source = session.Snapshot().Source;
        RunSettings settings = Settings.WithStations(Settings.Default with
        {
            NSpanPerHalf = 8, NChord = 2, SectionEtas = null, SectionXs = null
        });
        var method = new ProductWingMethod(settings);
        var sections = Placement.Sections(source, settings.SectionEtas!, settings.SectionXs!, CancellationToken.None);
        RunReference reference = method.Reference(source);
        StripValue Cl(double alpha)
        {
            OperatingPoint op = Fixture.Op(alpha);
            LatticeSolution solved = method.Solve(sections, op, Fixture.Salt, CancellationToken.None);
            double radians = VortexLattice.ToRadians(alpha);
            double lift = solved.Forces.Sum(force => -force.Fx * Math.Sin(radians) + force.Fz * Math.Cos(radians));
            double q = 0.5 * Fixture.Salt.Rho * op.Speed * op.Speed;
            return new(lift / (q * reference.SRef), null);
        }
        SearchResult result = OperatingSearch.FindAlpha(Cl, 0.3, 0, 6, "ANA-BASIS-DEEP-WATER");
        if (result.Value is not { } alphaRoot || Math.Abs(Cl(alphaRoot).Value!.Value - 0.3) > 0.003 ||
            result.TerminationCode != "ANA-FIND-ROOT")
            throw new InvalidOperationException("target CL root outside 1%: " + result);
    }

    private static void NoRoot()
    {
        SearchResult result = OperatingSearch.FindAlpha(alpha => new StripValue(alpha / 10, null),
            2, 0, 6, "ANA-BASIS-DEEP-WATER");
        if (result.Value is not null || result.TerminationCode != "ANA-FIND-NO-SIGN-CHANGE")
            throw new InvalidOperationException("no-root search extrapolated or lost its reason");
    }

    private static void ZeroLift()
    {
        SearchResult root = OperatingSearch.FindAlpha(alpha => new StripValue(0.1 * (alpha + 2), null),
            0, -5, 1, "ANA-BASIS-DEEP-WATER");
        if (root.Value is not { } alpha || Math.Abs(alpha + 2) > 0.01)
            throw new InvalidOperationException("zero-lift operating alpha was unavailable");
    }

    private static void Takeoff()
    {
        var visited = new List<double>();
        SearchResult result = OperatingSearch.FindTakeoff(speed =>
        {
            visited.Add(speed);
            return new StripValue(100 * speed * speed * (1 + speed / 100), null);
        }, 1030, 1, 5, "ANA-BASIS-DEEP-WATER");
        if (result.Value is not { } speed || Math.Abs(100 * speed * speed * (1 + speed / 100) - 1030) > 10.3 ||
            visited.Distinct().Count() < 3)
            throw new InvalidOperationException("take-off did not re-evaluate the load over V");
    }

    private static void Correction()
    {
        FreeSurfaceResult result = FreeSurfaceCorrection.Evaluate(100, 10, 20, 5, 0.3, 0.1, 2, 150000, 3);
        double expectedLift = 1 - 0.45 * Math.Exp(-Math.Pow(3, 0.70));
        double expectedDrag = 1 - 0.50 * Math.Exp(-Math.Pow(3, 0.40));
        if (Math.Abs(result.Lift!.Value - 100 * expectedLift) > 1e-9 ||
            Math.Abs(result.DragLow!.Value - 10 * expectedDrag) > 1e-9 ||
            Math.Abs(result.DragHigh!.Value - 20 * expectedDrag) > 1e-9 ||
            Math.Abs(result.HOverC!.Value - 3) > 1e-12 || result.FroudeDepth is not > 0 || result.Id != "JMSA-2026-depth-fit")
            throw new InvalidOperationException("A5.2 depth-only correction or provenance changed");
        if (result.ModelScaleReFrom != 73000 || result.ModelScaleReTo != 290000 ||
            result.FroudeLiftLossAtHc4Fr2To5 != 0.17 || result.FroudeDependenceCode != "ANA-FREE-SURFACE-FROUDE-NOT-MODELLED")
            throw new InvalidOperationException("A5.2 evidence limits were not projected as data");
        if (!result.ModelNotes.Contains("ANA-FREE-SURFACE-DEPTH-ONLY") ||
            !result.ModelNotes.Contains("ANA-FREE-SURFACE-WAVE-DRAG-OMITTED"))
            throw new InvalidOperationException("A5.2 omissions are absent");
        FreeSurfaceResult piercing = FreeSurfaceCorrection.Evaluate(100, 10, 20, 5, 0, 0.1, 2, 150000, 3);
        if (piercing.Lift is not null || piercing.ReasonCode != "ANA-FREE-SURFACE-SURFACE-PIERCING")
            throw new InvalidOperationException("surface-piercing correction returned a plausible value");
    }

    private static void CorrectionEnvelope()
    {
        void Outside(string axis, double depth = 0.3, double speed = 2, double reynolds = 150000,
            double alpha = 3)
        {
            FreeSurfaceResult result = FreeSurfaceCorrection.Evaluate(100, 10, 20, 5, depth, 0.1,
                speed, reynolds, alpha);
            if (result.Lift is not null || result.DragLow is not null || result.DragHigh is not null ||
                !result.ReasonCodes.Contains(axis))
                throw new InvalidOperationException("outside A5.2 " + axis + " produced a supported number");
        }
        Outside("ANA-FREE-SURFACE-RE-OUTSIDE", reynolds: 72999);
        Outside("ANA-FREE-SURFACE-RE-OUTSIDE", reynolds: 290001);
        Outside("ANA-FREE-SURFACE-FROUDE-OUTSIDE", speed: 9);
        Outside("ANA-FREE-SURFACE-ALPHA-OUTSIDE", alpha: -5.01);
        Outside("ANA-FREE-SURFACE-ALPHA-OUTSIDE", alpha: 10.01);
        Outside("ANA-FREE-SURFACE-DEPTH-OUTSIDE", depth: 0.049);
        Outside("ANA-FREE-SURFACE-DEPTH-OUTSIDE", depth: 0.951);
    }
}
