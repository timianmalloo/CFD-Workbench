using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The estimator's one section Cl comes from <see cref="Panel"/>. The Glauert read supplies α_L0 and Cm_c/4.</summary>
public sealed record SectionEstimate(PanelResult Panel, double AlphaL0Deg, double CmQuarter, double CdTurbulentBound)
{
    public double Cl => Panel.Cl;
}

/// <summary>In-process section estimate: inviscid panel lift and Cp, thin-airfoil camber moment, and an ITTC drag bound.</summary>
public static class SectionEstimator
{
    // Eight-point Gauss–Legendre per source chord interval; intervals split at every supplied camber knot.
    private static readonly double[] Nodes = [
        -0.9602898564975363, -0.7966664774136267, -0.5255324099163290, -0.1834346424956498,
         0.1834346424956498, 0.5255324099163290, 0.7966664774136267, 0.9602898564975363];
    private static readonly double[] Weights = [
        0.1012285362903763, 0.2223810344533745, 0.3137066458778873, 0.3626837833783620,
        0.3626837833783620, 0.3137066458778873, 0.2223810344533745, 0.1012285362903763];

    public static SectionEstimate Estimate(SectionSample section, double alphaDeg, double reynolds,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(section);
        int count = section.X.Count;
        if (count < 3 || section.CamberSlope.Count != count || section.Thickness.Count != count ||
            section.Camber.Count != count || Math.Abs(section.X[0]) > 1e-10 || Math.Abs(section.X[^1] - 1) > 1e-10)
            throw new ContractError("ANA-SECTION-GEOMETRY", "Section samples must span x/c = 0 to 1 with matching arrays.");
        if (!double.IsFinite(reynolds) || reynolds <= 100 || !double.IsFinite(section.Frame.ThicknessRatio) ||
            section.Frame.ThicknessRatio < 0)
            throw new ContractError("ANA-SECTION-RE", "Re must exceed 100 and t/c must be nonnegative.");

        double mean = 0, first = 0, second = 0;
        for (int i = 0; i < count - 1; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            double xa = section.X[i], xb = section.X[i + 1];
            if (!double.IsFinite(xa) || !double.IsFinite(xb) || !(xb > xa) ||
                !double.IsFinite(section.CamberSlope[i]) || !double.IsFinite(section.CamberSlope[i + 1]))
                throw new ContractError("ANA-SECTION-GEOMETRY", "Chord stations and camber slopes must be finite and ordered.");
            double thetaA = Math.Acos(1 - 2 * xa), thetaB = Math.Acos(1 - 2 * xb);
            double half = (thetaB - thetaA) / 2, centre = (thetaB + thetaA) / 2;
            for (int q = 0; q < Nodes.Length; q++)
            {
                double theta = centre + half * Nodes[q];
                double x = (1 - Math.Cos(theta)) / 2;
                double f = (x - xa) / (xb - xa);
                double slope = section.CamberSlope[i] + f * (section.CamberSlope[i + 1] - section.CamberSlope[i]);
                double weight = half * Weights[q] * slope;
                mean += weight;
                first += weight * Math.Cos(theta);
                second += weight * Math.Cos(2 * theta);
            }
        }
        double alphaL0 = (mean - first) / Math.PI;
        double cmQuarter = (second - first) / 2; // π/4 (A₂−A₁), Aₙ = (2/π)∫ z′ cos(nθ)dθ.
        double cf = 0.075 / Math.Pow(Math.Log10(reynolds) - 2, 2);
        double tc = section.Frame.ThicknessRatio;
        double cd = 2 * cf * (1 + 2 * tc + 60 * Math.Pow(tc, 4));
        PanelResult panel = PanelMethod.Solve(section, alphaDeg, cancellation);
        return new(panel, alphaL0 * 180 / Math.PI, cmQuarter, cd);
    }
}
