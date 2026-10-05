using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class SectionEstimatorTests
{
    internal static void Run()
    {
        AnalysisChecks.Check("Section_ParabolicCamber_ZeroLiftMinus4p584Deg", Camber);
        AnalysisChecks.Check("Section_ThinSymmetric_PanelClApproaches2PiSlope", Lift);
        AnalysisChecks.Check("Section_Ittc1957_TurbulentBound", Drag);
    }

    private static void Camber()
    {
        SectionSample section = Section(0.04, 0.10, 200);
        SectionEstimate estimate = SectionEstimator.Estimate(section, 0, 1e6);
        Near(-4.583662361, estimate.AlphaL0Deg, 0.05, "α_L0");
        Near(-Math.PI * 0.04, estimate.CmQuarter, 0.003, "Cm_c/4");
        AnalysisChecks.Equal(estimate.Panel.Cl, estimate.Cl, "one Cl definition");
    }

    private static void Lift()
    {
        SectionEstimate zero = SectionEstimator.Estimate(Section(0, 0.002, 200), 0, 1e6);
        SectionEstimate atThree = SectionEstimator.Estimate(Section(0, 0.002, 200), 3, 1e6);
        double slope = (atThree.Cl - zero.Cl) / (3 * Math.PI / 180);
        Console.WriteLine($"OBSERVED thin symmetric panel Cl slope: {slope:G9}");
        Near(2 * Math.PI, slope, 0.01 * 2 * Math.PI, "panel Cl small-angle slope");
        Near(0, zero.AlphaL0Deg, 0.01, "symmetric α_L0");
    }

    private static void Drag()
    {
        SectionEstimate estimate = SectionEstimator.Estimate(Section(0, 0.12, 100), 3, 1e6);
        double cf = 0.075 / Math.Pow(Math.Log10(1e6) - 2, 2);
        double expected = 2 * cf * (1 + 2 * 0.12 + 60 * Math.Pow(0.12, 4));
        Near(expected, estimate.CdTurbulentBound, 1e-12, "ITTC-1957 bound");
        Near(expected, SectionEstimator.Estimate(Section(0, 0.12, 100), -3, 1e6).CdTurbulentBound, 1e-12,
            "no lift-dependent profile drag");
    }

    internal static SectionSample Section(double camber, double thickness, int panels)
    {
        int half = panels / 2;
        var xs = Enumerable.Range(0, half + 1).Select(i => (1 - Math.Cos(Math.PI * i / half)) / 2).ToArray();
        double[] zc = xs.Select(x => 4 * camber * x * (1 - x)).ToArray();
        double[] t = xs.Select(x => thickness * Math.Sqrt(Math.Max(0, 1 - Math.Pow(2 * x - 1, 2)))).ToArray();
        double[] slope = xs.Select(x => 4 * camber * (1 - 2 * x)).ToArray();
        return new SectionSample(new StationFrame(0, 0.45, 0, 0.12, 0, 0, thickness), xs, zc, t, slope,
            xs.Select(x => new Point3(0.12 * x, 0, 0)).ToArray());
    }

    private static void Near(double expected, double actual, double tolerance, string what)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{what}: expected {expected}, actual {actual}");
    }
}
