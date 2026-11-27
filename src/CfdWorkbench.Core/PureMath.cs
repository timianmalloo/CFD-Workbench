namespace CfdWorkbench.Core;

/// <summary>
/// Pure managed IEEE-754 transcendental kernels (+ - * / and bit ops only, zero C-runtime calls).
/// Ruling 156: bit-identical across Windows ARM64, Windows x64, macOS ARM64, and Linux.
/// </summary>
internal static class PureMath
{
    private const double Ln2Hi = 0.693147180369123816490e0;
    private const double Ln2Lo = 1.90821492927058770002e-10;
    private const double InvLn2 = 1.44269504088896340736e0;
    private const double HalfPiHi = 1.57079632679489655800e0;
    private const double HalfPiLo = 6.12323399573676603587e-17;
    private const double InvHalfPi = 0.636619772367581343076e0;

    internal static double Ln(double x)
    {
        if (x <= 0.0 || double.IsNaN(x)) return double.NaN;
        if (x == 1.0) return 0.0;
        long bits = BitConverter.DoubleToInt64Bits(x);
        int exp = (int)((bits >> 52) & 0x7FF) - 1023;
        long mantBits = (bits & 0x000F_FFFF_FFFF_FFFFL) | 0x3FF0_0000_0000_0000L;
        double m = BitConverter.Int64BitsToDouble(mantBits);
        if (m > 1.41421356237309504880)
        {
            m *= 0.5;
            exp++;
        }
        double u = (m - 1.0) / (m + 1.0);
        double u2 = u * u;
        double poly = 1.0 / 23.0;
        poly = poly * u2 + 1.0 / 21.0;
        poly = poly * u2 + 1.0 / 19.0;
        poly = poly * u2 + 1.0 / 17.0;
        poly = poly * u2 + 1.0 / 15.0;
        poly = poly * u2 + 1.0 / 13.0;
        poly = poly * u2 + 1.0 / 11.0;
        poly = poly * u2 + 1.0 / 9.0;
        poly = poly * u2 + 1.0 / 7.0;
        poly = poly * u2 + 1.0 / 5.0;
        poly = poly * u2 + 1.0 / 3.0;
        poly = poly * u2 + 1.0;
        double series = 2.0 * u * poly;
        return exp * Ln2Hi + (series + exp * Ln2Lo);
    }

    internal static double Exp(double x)
    {
        if (double.IsNaN(x)) return double.NaN;
        if (x > 709.782712893384) return double.PositiveInfinity;
        if (x < -708.3964185322641) return 0.0;
        if (x == 0.0) return 1.0;
        double scaled = x * InvLn2;
        long k = scaled >= 0.0 ? (long)(scaled + 0.5) : (long)(scaled - 0.5);
        double r = (x - k * Ln2Hi) - k * Ln2Lo;
        double p = 1.0 / 6227020800.0;
        p = p * r + 1.0 / 479001600.0;
        p = p * r + 1.0 / 39916800.0;
        p = p * r + 1.0 / 3628800.0;
        p = p * r + 1.0 / 362880.0;
        p = p * r + 1.0 / 40320.0;
        p = p * r + 1.0 / 5040.0;
        p = p * r + 1.0 / 720.0;
        p = p * r + 1.0 / 120.0;
        p = p * r + 1.0 / 24.0;
        p = p * r + 1.0 / 6.0;
        p = p * r + 0.5;
        p = p * r + 1.0;
        p = p * r + 1.0;
        double pow2 = BitConverter.Int64BitsToDouble((1023L + k) << 52);
        return p * pow2;
    }

    internal static (double Sin, double Cos) SinCos(double x)
    {
        double scaled = x * InvHalfPi;
        long q = scaled >= 0.0 ? (long)(scaled + 0.5) : (long)(scaled - 0.5);
        double r = (x - q * HalfPiHi) - q * HalfPiLo;
        double r2 = r * r;
        double s = -1.0 / 355687428096000.0;
        s = s * r2 + 1.0 / 1307674368000.0;
        s = s * r2 - 1.0 / 6227020800.0;
        s = s * r2 + 1.0 / 39916800.0;
        s = s * r2 - 1.0 / 362880.0;
        s = s * r2 + 1.0 / 5040.0;
        s = s * r2 - 1.0 / 120.0;
        s = s * r2 + 1.0 / 6.0;
        s = r - r * r2 * s;
        double c = -1.0 / 6402373705728000.0;
        c = c * r2 + 1.0 / 20922789888000.0;
        c = c * r2 - 1.0 / 87178291200.0;
        c = c * r2 + 1.0 / 479001600.0;
        c = c * r2 - 1.0 / 3628800.0;
        c = c * r2 + 1.0 / 40320.0;
        c = c * r2 - 1.0 / 720.0;
        c = c * r2 + 1.0 / 24.0;
        c = c * r2 - 0.5;
        c = 1.0 + r2 * c;
        int quad = (int)(q & 3);
        return quad switch
        {
            0 => (s, c),
            1 => (c, -s),
            2 => (-s, -c),
            _ => (-c, s)
        };
    }

    internal static double PowInt(double x, int n)
    {
        double result = 1.0;
        for (int i = 0; i < n; i++) result *= x;
        return result;
    }
}
