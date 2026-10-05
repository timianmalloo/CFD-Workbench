using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Section lift, quarter-chord moment, and zero-lift angle all come from the panel solution.</summary>
public sealed record SectionEstimate(PanelResult Panel, double AlphaL0Deg, double CdTurbulentBound)
{
    public double Cl => Panel.Cl;
    public double CmQuarter => Panel.CmQuarter;
}

/// <summary>In-process section estimate: inviscid panel lift, moment and Cp, plus an ITTC drag bound.</summary>
public static class SectionEstimator
{
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

        for (int i = 0; i < count - 1; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            double xa = section.X[i], xb = section.X[i + 1];
            if (!double.IsFinite(xa) || !double.IsFinite(xb) || !(xb > xa) ||
                !double.IsFinite(section.CamberSlope[i]) || !double.IsFinite(section.CamberSlope[i + 1]))
                throw new ContractError("ANA-SECTION-GEOMETRY", "Chord stations and camber slopes must be finite and ordered.");
        }
        double cf = 0.075 / Math.Pow(Math.Log10(reynolds) - 2, 2);
        double tc = section.Frame.ThicknessRatio;
        double cd = 2 * cf * (1 + 2 * tc + 60 * Math.Pow(tc, 4));
        PanelResult panel = PanelMethod.Solve(section, alphaDeg, cancellation);
        PanelResult atZero = alphaDeg == 0 ? panel : PanelMethod.Solve(section, 0, cancellation);
        PanelResult atOne = alphaDeg == 1 ? panel : PanelMethod.Solve(section, 1, cancellation);
        double a0 = 0, a1 = 1, cl0 = atZero.Cl, cl1 = atOne.Cl;
        for (int iteration = 0; iteration < 8; iteration++)
        {
            if (Math.Abs(cl1) < 1e-8) return new(panel, a1, cd);
            double denominator = cl1 - cl0;
            if (!double.IsFinite(denominator) || Math.Abs(denominator) < 1e-12)
                break;
            double next = a1 - cl1 * (a1 - a0) / denominator;
            if (!double.IsFinite(next)) break;
            PanelResult atNext = PanelMethod.Solve(section, next, cancellation);
            (a0, cl0, a1, cl1) = (a1, cl1, next, atNext.Cl);
        }
        throw new ContractError("ANA-SECTION-ZEROLIFT", "The panel zero-lift angle did not converge.");
    }
}
