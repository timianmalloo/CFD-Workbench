using System.Numerics;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Analysis.Tests;

internal static class PanelCpTests
{
    internal static void Run() => AnalysisChecks.Check("PanelCp_KarmanTrefftz_100_200_400", KarmanTrefftz);

    private static void KarmanTrefftz()
    {
        // The Kármán–Trefftz family at exponent 2 is the Joukowski cusp. The circle passes through ζ=1;
        // W'(1)=0 fixes the circulation. This independent conformal-map oracle tests both surfaces.
        double[] errors = new double[3];
        int[] counts = [100, 200, 400];
        for (int k = 0; k < counts.Length; k++)
        {
            int n = counts[k];
            SectionPoint[] points = JoukowskiContour(n);
            PanelResult result = PanelMethod.Solve(points, 4);
            AnalysisChecks.Equal(n, result.StationCount, "station count");
            AnalysisChecks.Equal(n / 2, result.Upper.Count, "upper panel count");
            AnalysisChecks.Equal(n / 2, result.Lower.Count, "lower panel count");
            double sum = 0;
            foreach (PanelCp sample in result.Upper.Concat(result.Lower))
            {
                double expected = JoukowskiCp(sample.X, sample.Z, 4);
                sum += Math.Pow(sample.Cp - expected, 2);
            }
            errors[k] = Math.Sqrt(sum / n);
            if (!double.IsFinite(result.CpMin) || result.CpMin >= -0.1 || result.Cl <= 0)
                throw new InvalidOperationException($"Cp_min {result.CpMin}, Cl {result.Cl}");
        }
        // Tolerance fixed before implementation (OQ-10). The cusp makes the first midpoint the hard case.
        if (!(errors[0] < 0.20 && errors[1] < 0.11 && errors[2] < 0.06 &&
              errors[2] < errors[1] && errors[1] < errors[0]))
            throw new InvalidOperationException($"KT RMS Cp error n100/200/400: {string.Join(", ", errors)}");
        Console.WriteLine($"OBSERVED KT Cp RMS 100/200/400: {string.Join(" / ", errors.Select(e => e.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)))}");
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

    private static double JoukowskiCp(double x, double z, double alphaDeg)
    {
        const double offset = 0.08;
        double leadingZeta = -1 - 2 * offset;
        double chord = 2 - (leadingZeta + 1 / leadingZeta);
        Complex w = new(leadingZeta + chord * x, chord * z);
        Complex root = Complex.Sqrt(w * w - 4);
        Complex a = (w + root) / 2, b = (w - root) / 2;
        Complex zeta = Math.Abs(Complex.Abs(a + offset) - (1 + offset)) <
            Math.Abs(Complex.Abs(b + offset) - (1 + offset)) ? a : b;
        Complex shifted = zeta + offset;
        double alpha = alphaDeg * Math.PI / 180;
        Complex numerator = Complex.Exp(-Complex.ImaginaryOne * alpha) -
            Complex.Exp(Complex.ImaginaryOne * alpha) * Math.Pow(1 + offset, 2) / (shifted * shifted) +
            Complex.ImaginaryOne * 2 * (1 + offset) * Math.Sin(alpha) / shifted;
        Complex velocity = numerator / (1 - 1 / (zeta * zeta));
        return 1 - velocity.Magnitude * velocity.Magnitude;
    }
}
