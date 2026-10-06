using System.Numerics;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class PanelCpTests
{
    // Analytic Joukowski Cp minimised on 200,000 circle-angle points by the independent review harness.
    private const double ExactJoukowskiCpMin = -1.713602661966227;

    internal static void Run()
    {
        AnalysisChecks.Check("PanelCp_KarmanTrefftz_InteriorOrderAndCpMin", KarmanTrefftz);
        AnalysisChecks.Check("PanelCp_DefaultResolution_CpMinWithin4Percent", DefaultResolution);
        AnalysisChecks.Check("RunKey_PanelCountAndTeExclusion_ChangeKey", PanelKey);
    }

    internal static void RunReadiness()
    {
        AnalysisChecks.Check("PanelCp_Cusp800_TeExcludedFromCpMin", Cusp800);
    }

    private static void KarmanTrefftz()
    {
        AnalysisChecks.Equal("inviscid; no boundary layer", PanelMethod.ModelLabel, "method label");
        // The Kármán–Trefftz family at exponent 2 is the Joukowski cusp. The circle passes through ζ=1;
        // W'(1)=0 fixes the circulation. This independent conformal-map oracle tests both surfaces.
        double[] errors = new double[3], minimumErrors = new double[3];
        int[] counts = [100, 200, 400];
        double exactMin = ExactJoukowskiCpMin;
        double[] cpMinCeilings = [0.13, 0.07, 0.04];
        for (int k = 0; k < counts.Length; k++)
        {
            int n = counts[k];
            SectionPoint[] points = JoukowskiContour(n);
            PanelResult result = PanelMethod.Solve(points, 4);
            AnalysisChecks.Equal(n, result.StationCount, "station count");
            AnalysisChecks.Equal(n / 2, result.Upper.Count, "upper panel count");
            AnalysisChecks.Equal(n / 2, result.Lower.Count, "lower panel count");
            double sum = 0;
            PanelCp[] samples = result.Upper.Concat(result.Lower).ToArray();
            for (int i = 3; i < n - 3; i++)
            {
                // The exact point is the circle-angle midpoint; omit three panels at each cusp side.
                double expected = JoukowskiCpAtTheta(2 * Math.PI * (i + 0.5) / n, 4);
                sum += Math.Pow(samples[i].Cp - expected, 2);
            }
            errors[k] = Math.Sqrt(sum / (n - 6));
            minimumErrors[k] = Math.Abs(result.CpMin - exactMin);
            if (minimumErrors[k] > cpMinCeilings[k] || result.Cl <= 0)
                throw new InvalidOperationException($"n={n}: Cp_min {result.CpMin}, exact {exactMin}, Cl {result.Cl}");
        }
        double[] orders = Enumerable.Range(0, 2).Select(i => Math.Log2(errors[i] / errors[i + 1])).ToArray();
        if (orders.Any(p => p < 0.9))
            throw new InvalidOperationException($"KT interior Cp RMS {string.Join(" / ", errors)}; order {string.Join(" / ", orders)}");
        Console.WriteLine($"OBSERVED KT interior Cp RMS 100/200/400: {string.Join(" / ", errors.Select(e => e.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)))}; p {string.Join(" / ", orders.Select(p => p.ToString("G4", System.Globalization.CultureInfo.InvariantCulture)))}; exact Cp_min {exactMin:G7}; |ΔCp_min| {string.Join(" / ", minimumErrors.Select(e => e.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)))}");
    }

    private static void DefaultResolution()
    {
        int panels = PanelMethod.DefaultPanelCount;
        AnalysisChecks.Equal(200, panels, "Ruling 90 panel count at every station");
        AnalysisChecks.Equal(3, PanelMethod.CpMinTrailingEdgePanelsPerSide, "TE exclusion in method identity");
        if (!MethodRecord.VlmStrip.Method.Version.Contains("panel200-gov400-te3", StringComparison.Ordinal))
            throw new InvalidOperationException("method version omits panel count or TE exclusion");
        PanelResult result = PanelMethod.Solve(JoukowskiContour(panels), 4);
        double relativeError = Math.Abs(result.CpMin - ExactJoukowskiCpMin) / -ExactJoukowskiCpMin;
        if (relativeError > 0.04)
            throw new InvalidOperationException($"{panels} panels: Cp_min {result.CpMin:G9}, exact {ExactJoukowskiCpMin:G9}, relative error {relativeError:P3} > 4%");
        Console.WriteLine($"OBSERVED default {panels}-panel KT Cp_min relative error {relativeError:P3}");
    }

    private static void PanelKey()
    {
        var inputs = new RunInputs(Guid.NewGuid().ToString("D"), new string('a', 64), [new string('b', 64)], "cfdw-cv/2", "placement/1");
        var water = new WaterRecord(15, 0, 999, 1e-6, 1700, "fixture", new string('c', 64));
        var op = new OperatingPoint(5, 101325, 0.5, "root", 3, null);
        RunMethod current = MethodRecord.VlmStrip.Method;
        string settings = new string('d', 64);
        string key = RunRecord.Key(inputs, water, op, current, settings);
        string coarse = RunRecord.Key(inputs, water, op, current with { Version = current.Version.Replace("panel200", "panel400", StringComparison.Ordinal) }, settings);
        string te = RunRecord.Key(inputs, water, op, current with { Version = current.Version.Replace("te3", "te2", StringComparison.Ordinal) }, settings);
        if (key == coarse || key == te || coarse == te) throw new InvalidOperationException("panel count or TE exclusion did not change the run key");
    }

    private static void Cusp800()
    {
        PanelResult result = PanelMethod.Solve(JoukowskiContour(800), 4);
        PanelCp[] samples = result.Upper.Concat(result.Lower).ToArray();
        double interiorMin = samples.Skip(3).Take(800 - 6).Min(p => p.Cp);
        double teMin = Math.Min(samples[0].Cp, samples[^1].Cp);
        if (!(teMin < interiorMin && Math.Abs(result.CpMin - interiorMin) < 1e-12 &&
              Math.Abs(result.CpMin - ExactJoukowskiCpMin) < 0.025))
            throw new InvalidOperationException($"800-panel TE Cp {teMin}, interior minimum {interiorMin}, reported Cp_min {result.CpMin}");
        Console.WriteLine($"OBSERVED 800-panel TE Cp {teMin:G7}; reported Cp_min {result.CpMin:G7}");
    }

    private static SectionPoint[] JoukowskiContour(int n)
    {
        const double offset = 0.08;
        double radius = 1 + offset;
        var raw = new Complex[n + 1];
        for (int i = 0; i <= n; i++)
        {
            double theta = 2 * Math.PI * i / n;
            Complex zeta = -offset + radius * Complex.FromPolarCoordinates(1, theta);
            raw[i] = zeta + 1 / zeta;
        }
        double leading = raw[n / 2].Real;
        double chord = 2 - leading;
        return raw.Select(w => new SectionPoint((w.Real - leading) / chord, w.Imaginary / chord)).ToArray();
    }

    private static double JoukowskiCpAtTheta(double theta, double alphaDeg)
    {
        const double offset = 0.08;
        Complex zeta = -offset + (1 + offset) * Complex.FromPolarCoordinates(1, theta);
        Complex shifted = zeta + offset;
        double alpha = alphaDeg * Math.PI / 180;
        Complex numerator = Complex.Exp(-Complex.ImaginaryOne * alpha) -
            Complex.Exp(Complex.ImaginaryOne * alpha) * Math.Pow(1 + offset, 2) / (shifted * shifted) +
            Complex.ImaginaryOne * 2 * (1 + offset) * Math.Sin(alpha) / shifted;
        Complex velocity = numerator / (1 - 1 / (zeta * zeta));
        return 1 - velocity.Magnitude * velocity.Magnitude;
    }
}
