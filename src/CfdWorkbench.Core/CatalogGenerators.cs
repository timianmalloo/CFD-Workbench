using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core;

/// <summary>
/// Deterministic public-domain NACA/DTMB and project-owned CFDW CST section generators.
/// All coordinate-emitting paths use pure managed IEEE-754 arithmetic (+ - * / sqrt, PureMath, and pinned double.CosPi)
/// with zero C-runtime trig/log calls (Ruling 156).
/// </summary>
public static class CatalogGenerators
{
    public const string Naca4Id = CatalogGenerator.Id; // "naca4-closed/2"
    public const string Naca4ModId = "naca4m-closed/1";
    public const string Naca5Id = "naca5-closed/1";
    public const string Naca6Id = "naca6-tm4741/1";
    public const string Naca66ModId = "naca66mod-dtmb1780/1";
    public const string CfdwCstId = "cfdw-cst/1";

    private const double Pi = 3.14159265358979323846;
    private const double TwoPi = 2.0 * Pi;
    private static readonly double[] Binomial7 = [1, 7, 21, 35, 35, 21, 7, 1];

    internal sealed record CfdwCstSpec(string Id, string Designation, double[] Upper, double[] Lower);

    internal static readonly IReadOnlyDictionary<string, CfdwCstSpec> CfdwSpecs =
        new CfdwCstSpec[]
        {
            new("cfdw-surf-118", "CFDW Surf 118",
                [0.1953, 0.2239, 0.2446, 0.2286, 0.2120, 0.1853, 0.1580, 0.1220],
                [-0.1253, -0.1139, -0.0846, -0.0486, -0.0220, -0.0053, 0.0020, 0.0180]),
            new("cfdw-surf-105", "CFDW Surf 105",
                [0.1719, 0.2008, 0.2213, 0.2042, 0.1870, 0.1593, 0.1315, 0.1043],
                [-0.1119, -0.1008, -0.0713, -0.0442, -0.0170, -0.0093, -0.0015, 0.0157]),
            new("cfdw-dw-105", "CFDW Downwind 105",
                [0.1536, 0.1820, 0.2020, 0.1942, 0.1864, 0.1591, 0.1313, 0.0945],
                [-0.1136, -0.1120, -0.0920, -0.0642, -0.0364, -0.0191, -0.0113, 0.0055]),
            new("cfdw-dw-092", "CFDW Downwind 092",
                [0.1329, 0.1610, 0.1717, 0.1635, 0.1561, 0.1386, 0.1111, 0.0843],
                [-0.1029, -0.1010, -0.0817, -0.0635, -0.0361, -0.0186, -0.0111, 0.0057]),
            new("cfdw-race-085", "CFDW Race 085",
                [0.1106, 0.1382, 0.1574, 0.1582, 0.1515, 0.1347, 0.1087, 0.0827],
                [-0.0906, -0.0882, -0.0774, -0.0682, -0.0415, -0.0247, -0.0087, 0.0073]),
            new("cfdw-race-075", "CFDW Race 075",
                [0.0921, 0.1196, 0.1387, 0.1396, 0.1321, 0.1155, 0.0989, 0.0732],
                [-0.0821, -0.0796, -0.0687, -0.0596, -0.0421, -0.0255, -0.0089, 0.0068]),
            new("cfdw-stab-085", "CFDW Stab 085",
                [0.1154, 0.1342, 0.1348, 0.1260, 0.1172, 0.0995, 0.0812, 0.0636],
                [-0.1054, -0.1042, -0.0948, -0.0860, -0.0772, -0.0595, -0.0512, -0.0336]),
            new("cfdw-mast-125", "CFDW Mast 125",
                [0.1303, 0.1533, 0.1687, 0.1764, 0.1610, 0.1303, 0.0997, 0.0690],
                [-0.1303, -0.1533, -0.1687, -0.1764, -0.1610, -0.1303, -0.0997, -0.0690]),
            new("cfdw-mast-150", "CFDW Mast 150",
                [0.1593, 0.1821, 0.2049, 0.2125, 0.1897, 0.1518, 0.1138, 0.0759],
                [-0.1593, -0.1821, -0.2049, -0.2125, -0.1897, -0.1518, -0.1138, -0.0759]),
        }.ToDictionary(s => s.Id, StringComparer.Ordinal);

    public static (byte[] Bytes, double LeShift, double RotationDegrees, double Scale) Generate(string generatorId, string entryId)
    {
        if (!TryGenerate(generatorId, entryId, out var shape))
            throw new ArgumentException($"Unknown generator '{generatorId}' or entry id '{entryId}'.");
        return (shape.Bytes, shape.LeShift, shape.RotationDegrees, shape.Scale);
    }

    internal static bool TryGenerate(string generatorId, string entryId, out CatalogGenerator.NacaShape shape)
    {
        shape = default;
        switch (generatorId)
        {
            case Naca4Id:
                if (!entryId.StartsWith("naca-", StringComparison.Ordinal)) return false;
                string d4 = entryId["naca-".Length..];
                if (d4.Length != 4 || !d4.All(char.IsAsciiDigit)) return false;
                shape = CatalogGenerator.Shape(d4);
                return true;

            case Naca4ModId:
                return TryGenerateNaca4ModOr16(entryId, out shape);

            case Naca5Id:
                if (!entryId.StartsWith("naca-", StringComparison.Ordinal)) return false;
                string d5 = entryId["naca-".Length..];
                if (d5.Length != 5 || !d5.All(char.IsAsciiDigit)) return false;
                shape = Naca5(d5);
                return true;

            case Naca6Id:
                return TryGenerateNaca6(entryId, out shape);

            case Naca66ModId:
                return TryGenerateNaca66Mod(entryId, out shape);

            case CfdwCstId:
                if (!CfdwSpecs.TryGetValue(entryId, out var spec)) return false;
                shape = CfdwCst(spec.Designation, spec.Upper, spec.Lower);
                return true;

            default:
                return false;
        }
    }

    private static bool TryGenerateNaca4ModOr16(string entryId, out CatalogGenerator.NacaShape shape)
    {
        shape = default;
        if (!entryId.StartsWith("naca-", StringComparison.Ordinal)) return false;
        string body = entryId["naca-".Length..];
        // 16-series: "16-012", "16-309", etc.
        if (body.StartsWith("16-", StringComparison.Ordinal) && body.Length == 6 && body[3..].All(char.IsAsciiDigit))
        {
            double cl = (body[3] - '0') / 10.0;
            double toc = int.Parse(body[4..], CultureInfo.InvariantCulture) / 100.0;
            shape = Naca16(cl, toc, "NACA " + body);
            return true;
        }
        // 4-digit-modified: "0010-34"
        if (body.Length == 7 && body[4] == '-' && body[..4].All(char.IsAsciiDigit) && body[5..].All(char.IsAsciiDigit))
        {
            double m = (body[0] - '0') / 100.0;
            double p = (body[1] - '0') / 10.0;
            double toc = int.Parse(body[2..4], CultureInfo.InvariantCulture) / 100.0;
            double leIndex = body[5] - '0';
            double xmaxt = (body[6] - '0') / 10.0;
            shape = Naca4M(m, p, toc, leIndex, xmaxt, "NACA " + body);
            return true;
        }
        return false;
    }

    private static bool TryGenerateNaca6(string entryId, out CatalogGenerator.NacaShape shape)
    {
        shape = default;
        if (!entryId.StartsWith("naca-6", StringComparison.Ordinal)) return false;
        string body = entryId["naca-".Length..];
        // 6A-series: e.g. "64a410"
        if (body.Length == 6 && (body[2] == 'a' || body[2] == 'A') && body[3..].All(char.IsAsciiDigit))
        {
            int fam = body[1] switch { '3' => 5, '4' => 6, '5' => 7, _ => -1 };
            if (fam < 0) return false;
            double cl = (body[3] - '0') / 10.0;
            double toc = int.Parse(body[4..], CultureInfo.InvariantCulture) / 100.0;
            shape = Naca6(fam, cl, toc, is6A: true, "NACA " + body.ToUpperInvariant());
            return true;
        }
        // 6-series: e.g. "63-209", "66-018"
        if (body.Length == 6 && body[2] == '-' && body[3..].All(char.IsAsciiDigit))
        {
            int fam = body[1] switch { '3' => 0, '4' => 1, '5' => 2, '6' => 3, '7' => 4, _ => -1 };
            if (fam < 0) return false;
            double cl = (body[3] - '0') / 10.0;
            double toc = int.Parse(body[4..], CultureInfo.InvariantCulture) / 100.0;
            shape = Naca6(fam, cl, toc, is6A: false, "NACA " + body);
            return true;
        }
        return false;
    }

    private static bool TryGenerateNaca66Mod(string entryId, out CatalogGenerator.NacaShape shape)
    {
        shape = default;
        const string prefix = "naca-66mod-";
        if (!entryId.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string digits = entryId[prefix.Length..];
        if (digits.Length != 3 || !digits.All(char.IsAsciiDigit)) return false;
        double cl = (digits[0] - '0') / 10.0;
        double toc = int.Parse(digits[1..], CultureInfo.InvariantCulture) / 100.0;
        shape = Naca66Mod(cl, toc, "NACA 66(MOD)-" + digits);
        return true;
    }

    // ---- Shared chord-frame normalizer (exact parity with CatalogGenerator.Shape) ----
    private static CatalogGenerator.NacaShape FrameAndFormat(string header, Func<double, int, (double X, double Y)> surface, bool isSymmetric)
    {
        double xs = 0.0;
        if (!isSymmetric)
        {
            double left = 0.0, right = 0.05;
            for (int step = 0; step < 200; step++)
            {
                double c1 = right - (right - left) / 1.618033988749895;
                double c2 = left + (right - left) / 1.618033988749895;
                if (surface(c1, 1).X < surface(c2, 1).X) right = c2;
                else left = c1;
            }
            xs = (left + right) / 2.0;
            if (surface(xs, 1).X > 0.0) xs = 0.0;
        }
        (double X, double Y) le = surface(xs, 1);
        (double X, double Y) teUpper = surface(1.0, 1);
        (double X, double Y) teLower = surface(1.0, -1);
        double mx = (teUpper.X + teLower.X) / 2.0 - le.X;
        double my = (teUpper.Y + teLower.Y) / 2.0 - le.Y;
        double length = Math.Sqrt(mx * mx + my * my);
        double frameCos = mx / length, frameSin = -my / length;
        double angle = Math.Atan2(my, mx); // crt-allowed: Ruling 156 — recorded rotation column only; compared within 1e-12 relative, no byte reads it
        (double X, double Y) Frame((double X, double Y) point)
        {
            double dx = point.X - le.X, dy = point.Y - le.Y;
            return ((dx * frameCos - dy * frameSin) / length,
                (dx * frameSin + dy * frameCos) / length);
        }

        var upper = new (double X, double Y)[81];
        var lower = new (double X, double Y)[81];
        for (int index = 0; index <= 80; index++)
        {
            double s = CatalogGenerator.Spacing(index);
            upper[index] = Frame(surface(xs + (1.0 - xs) * s, 1));
            double distance = s * (xs + 1.0);
            lower[index] = Frame(distance <= xs ? surface(xs - distance, 1) : surface(distance - xs, -1));
        }
        upper[0] = (0.0, 0.0);
        lower[0] = (0.0, 0.0);

        var text = new StringBuilder(header).Append('\n');
        for (int index = 80; index >= 0; index--) Append(text, upper[index]);
        for (int index = 1; index <= 80; index++) Append(text, lower[index]);
        return new(Encoding.UTF8.GetBytes(text.ToString()), le.X, angle / PlacementRule.RadiansPerDegree, length);
    }

    private static void Append(StringBuilder text, (double X, double Y) point) =>
        text.Append(point.X.ToString("G17", CultureInfo.InvariantCulture)).Append(' ')
            .Append(point.Y.ToString("G17", CultureInfo.InvariantCulture)).Append('\n');

    private static (double X, double Y) Offset(double x, double yt, double yc, double slope, int sign)
    {
        double root = Math.Sqrt(1.0 + slope * slope);
        return (x - sign * yt * (slope / root), yc + sign * yt * (1.0 / root));
    }

    private static double Thickness4Closed(double t, double x)
    {
        if (x <= 0.0) return 0.0;
        double sq = x * x;
        return 5.0 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * sq + 0.2843 * sq * x - 0.1036 * sq * sq);
    }

    private static (double Yc, double Slope) MeanLine2(double m, double p, double x)
    {
        if (m == 0.0) return (0.0, 0.0);
        if (x < p) return (m / (p * p) * (2.0 * p * x - x * x), 2.0 * m / (p * p) * (p - x));
        double q = 1.0 - p;
        return (m / (q * q) * (1.0 - 2.0 * p + 2.0 * p * x - x * x), 2.0 * m / (q * q) * (p - x));
    }

    // ---- 4-digit modified & 16-series (NASA TM X-3284 / TM 4741, closed TE d0=0) ----
    private static double CalculateD1(double m)
    {
        double[] c = [3.48e-5, 2.3076628, -10.127712, 19.961478, -10.420597];
        double f = c[4];
        for (int i = 3; i >= 0; i--) f = f * m + c[i];
        return f;
    }

    private static double Thickness4MClosed(double toc, double leIndex, double xmaxt, double x)
    {
        if (x <= 0.0) return 0.0;
        double d1 = CalculateD1(xmaxt);
        double rle = (1.1019 / 36.0) * (toc * leIndex) * (toc * leIndex);
        double a0 = (0.2 / toc) * Math.Sqrt(rle + rle);
        double omx = 1.0 - xmaxt;
        const double span = 0.1; // closed TE (d0 = 0)
        double a3 = 0.1 / (xmaxt * xmaxt * xmaxt) + (d1 * omx - 3.0 * span) / (xmaxt * omx * omx)
            - 0.375 * a0 / (xmaxt * xmaxt * Math.Sqrt(xmaxt));
        double a2 = -0.1 / (xmaxt * xmaxt) + 0.5 * a0 / (xmaxt * Math.Sqrt(xmaxt)) - 2.0 * a3 * xmaxt;
        double a1 = -0.5 * a0 / Math.Sqrt(xmaxt) - 2.0 * a2 * xmaxt - 3.0 * a3 * xmaxt * xmaxt;
        double d3 = (3.0 * d1 - 6.0 * span / omx) / (3.0 * omx * omx);
        double d2 = -1.5 * omx * d3 - 0.5 * d1 / omx;
        double y;
        if (x < xmaxt)
        {
            double srx = Math.Sqrt(x);
            y = a0 * srx + x * (a1 + x * (a2 + x * a3));
        }
        else
        {
            double u = 1.0 - x;
            y = u * (d1 + u * (d2 + u * d3));
        }
        return 5.0 * toc * y;
    }

    internal static CatalogGenerator.NacaShape Naca16(double cl, double toc, string header) =>
        FrameAndFormat(header, (x, sign) =>
        {
            double yt = Thickness4MClosed(toc, 4.0, 0.5, x);
            (double yc, double slope) = cl == 0.0 ? (0.0, 0.0) : MeanLine6(1.0, cl, x);
            return Offset(x, yt, yc, slope, sign);
        }, cl == 0.0);

    internal static CatalogGenerator.NacaShape Naca4M(double m, double p, double toc, double leIndex, double xmaxt, string header) =>
        FrameAndFormat(header, (x, sign) =>
        {
            double yt = Thickness4MClosed(toc, leIndex, xmaxt, x);
            (double yc, double slope) = MeanLine2(m, p, x);
            return Offset(x, yt, yc, slope, sign);
        }, m == 0.0);

    // ---- 5-digit (NACA Report 537 / NASA TM 4741, closed TE) ----
    private static (double Yc, double Slope) MeanLine3(double cl, double xmaxc, bool reflex, double x)
    {
        if (cl == 0.0) return (0.0, 0.0);
        if (!reflex)
        {
            double[] ms = [0.05, 0.10, 0.15, 0.20, 0.25];
            double[] rs = [0.0580, 0.1260, 0.2025, 0.2900, 0.3910];
            double[] ks = [361.4, 51.64, 15.957, 6.643, 3.230];
            double r = LinearLookup(ms, rs, xmaxc);
            double k1 = LinearLookup(ms, ks, xmaxc);
            double scale = k1 * cl / 1.8;
            if (x < r)
            {
                double ym = x * (x * (x - 3.0 * r) + r * r * (3.0 - r));
                double yp = 3.0 * x * (x - 2.0 * r) + r * r * (3.0 - r);
                return (scale * ym, scale * yp);
            }
            double r3 = r * r * r;
            return (scale * r3 * (1.0 - x), -scale * r3);
        }
        else
        {
            double[] ms = [0.10, 0.15, 0.20, 0.25];
            double[] rs = [0.130, 0.217, 0.318, 0.441];
            double[] ks = [51.99, 15.793, 6.520, 3.191];
            double r = LinearLookup(ms, rs, xmaxc);
            double k1 = LinearLookup(ms, ks, xmaxc);
            double mr = 1.0 - r;
            double r3 = r * r * r;
            double mr3 = mr * mr * mr;
            double k21 = (3.0 * (r - xmaxc) * (r - xmaxc) - r3) / mr3;
            double scale = k1 * cl / 1.8;
            double dx = x - r;
            if (x < r)
            {
                double ym = dx * dx * dx - k21 * mr3 * x - x * r3 + r3;
                double yp = 3.0 * dx * dx - k21 * mr3 - r3;
                return (scale * ym, scale * yp);
            }
            else
            {
                double ym = k21 * dx * dx * dx - k21 * mr3 * x - x * r3 + r3;
                double yp = 3.0 * k21 * dx * dx - k21 * mr3 - r3;
                return (scale * ym, scale * yp);
            }
        }
    }

    internal static CatalogGenerator.NacaShape Naca5(string digits)
    {
        double cl = (digits[0] - '0') * 0.15;
        double xmaxc = (digits[1] - '0') * 0.05;
        bool reflex = digits[2] == '1';
        double toc = int.Parse(digits[3..], CultureInfo.InvariantCulture) / 100.0;
        return FrameAndFormat("NACA " + digits, (x, sign) =>
        {
            double yt = Thickness4Closed(toc, x);
            (double yc, double slope) = MeanLine3(cl, xmaxc, reflex, x);
            return Offset(x, yt, yc, slope, sign);
        }, cl == 0.0);
    }

    // ---- 6-series & 6A-series mean lines (NASA TM 4741 / NACA Report 903, PureMath.Ln) ----
    internal static (double Yc, double Slope) MeanLine6(double a, double cl, double x)
    {
        if (cl == 0.0) return (0.0, 0.0);
        const double eps = 1e-7;
        double oma = 1.0 - a;
        if (Math.Abs(oma) < eps)
        {
            double omx = 1.0 - x;
            if (x < eps || omx < eps) return (0.0, 0.0);
            double lnx = PureMath.Ln(x);
            double lnomx = PureMath.Ln(omx);
            double factor = cl * (0.25 / Pi);
            return (-(omx * lnomx + x * lnx) * factor, (lnomx - lnx) * factor);
        }
        else
        {
            double omx = 1.0 - x;
            if (x < eps || Math.Abs(omx) < eps) return (0.0, 0.0);
            double g = Math.Abs(a) < eps ? -0.25 : -(a * a * (0.5 * PureMath.Ln(a) - 0.25) + 0.25) / oma;
            double h = Math.Abs(a) < eps ? -0.5 : g + (0.5 * oma * oma * PureMath.Ln(oma) - 0.25 * oma * oma) / oma;
            double amx = a - x;
            double term1 = 0.0, term1p = 0.0;
            if (Math.Abs(amx) >= eps)
            {
                double lnamx = PureMath.Ln(Math.Abs(amx));
                term1 = amx * amx * (2.0 * lnamx - 1.0);
                term1p = -amx * lnamx;
            }
            double lnomx = PureMath.Ln(omx);
            double term2 = omx * omx * (1.0 - 2.0 * lnomx);
            double term2p = omx * lnomx;
            double lnx = PureMath.Ln(x);
            double ym = 0.25 * (term1 + term2) / oma - x * lnx + g - h * x;
            double yp = (term1p + term2p) / oma - 1.0 - lnx - h;
            double scale = cl / (TwoPi * (a + 1.0));
            return (scale * ym, scale * yp);
        }
    }

    internal static (double Yc, double Slope) MeanLine6M(double cl, double x)
    {
        if (cl == 0.0) return (0.0, 0.0);
        const double eps = 1e-7;
        double teSlope = -0.24521 * cl;
        if (x < eps) return (0.0, 0.0);
        if (x >= 0.90) return (teSlope * (x - 1.0), teSlope);
        if (x > 0.85)
        {
            var (y0, m0) = MeanLine6MCore(cl, 0.85);
            double y1 = teSlope * (0.90 - 1.0);
            double m1 = teSlope;
            const double h = 0.05;
            double t = (x - 0.85) / h;
            double t2 = t * t, t3 = t2 * t;
            double yc = (2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * h * m0 + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * h * m1;
            double yp = ((6 * t2 - 6 * t) * y0 + (3 * t2 - 4 * t + 1) * h * m0 + (-6 * t2 + 6 * t) * y1 + (3 * t2 - 2 * t) * h * m1) / h;
            return (yc, yp);
        }
        return MeanLine6MCore(cl, x);
    }

    private static (double Yc, double Slope) MeanLine6MCore(double cl, double x)
    {
        const double a = 0.8;
        const double oma = 0.2;
        const double eps = 1e-7;
        double omx = 1.0 - x;
        double g = -(a * a * (0.5 * PureMath.Ln(a) - 0.25) + 0.25) / oma;
        double h = g + (0.5 * oma * oma * PureMath.Ln(oma) - 0.25 * oma * oma) / oma;
        double amx = a - x;
        double term1 = 0.0, term1p = 0.0;
        if (Math.Abs(amx) >= eps)
        {
            double lnamx = PureMath.Ln(Math.Abs(amx));
            term1 = amx * amx * (2.0 * lnamx - 1.0);
            term1p = -amx * lnamx;
        }
        double lnomx = PureMath.Ln(omx);
        double term2 = omx * omx * (1.0 - 2.0 * lnomx);
        double term2p = omx * lnomx;
        double lnx = PureMath.Ln(x);
        double ym = 0.25 * (term1 + term2) / oma - x * lnx + g - h * x;
        double yp = (term1p + term2p) / oma - 1.0 - lnx - h;
        double scale = cl * 0.97948 / (TwoPi * (a + 1.0));
        return (scale * ym, scale * yp);
    }

    // ---- 6-series & 6A-series thickness (NASA TM 4741 / PDAS Theodorsen conformal map + FMM spline) ----
    internal sealed class Naca6Thickness
    {
        private readonly double[] sLower, xLower, yLower, xpLower, ypLower;

        internal Naca6Thickness(int familyIndex, double toc)
        {
            const int np = 201;
            double[] epsTable = Naca6Tables.EPS[familyIndex];
            double[] psiTable = Naca6Tables.PSI[familyIndex];
            double[] sc = Naca6Tables.ScaleCoeff[familyIndex];
            double sf = sc[4];
            for (int i = 3; i >= 0; i--) sf = sf * toc + sc[i];

            var xt = new double[np];
            var yt = new double[np];
            var zetaRe = new double[np];
            var zetaIm = new double[np];
            for (int i = 0; i < np; i++)
            {
                double phi = (3.14159265 / (np - 1)) * i;
                double eps = sf * epsTable[i];
                double psi = sf * psiTable[i];
                double ep = PureMath.Exp(psi);
                double em = 1.0 / ep;
                var (sinA, cosA) = PureMath.SinCos(phi - eps);
                zetaRe[i] = (ep + em) * cosA;
                zetaIm[i] = (ep - em) * sinA;
            }
            double z0Re = zetaRe[0], z0Im = zetaIm[0];
            double dRe = zetaRe[np - 1] - z0Re, dIm = zetaIm[np - 1] - z0Im;
            double chord = Math.Sqrt(dRe * dRe + dIm * dIm);
            for (int i = 0; i < np; i++)
            {
                xt[i] = (z0Re - zetaRe[i]) / chord;
                yt[i] = -(z0Im - zetaIm[i]) / chord;
            }

            int nn = 2 * np - 1;
            var xLocal = new double[nn];
            var yLocal = new double[nn];
            var sLocal = new double[nn];
            for (int i = 0; i < np; i++)
            {
                xLocal[i] = xt[np - 1 - i];
                yLocal[i] = yt[np - 1 - i];
            }
            for (int i = 1; i < np; i++)
            {
                xLocal[np - 1 + i] = xt[i];
                yLocal[np - 1 + i] = -yt[i];
            }
            sLocal[0] = 0.0;
            for (int i = 1; i < nn; i++)
            {
                double dx = xLocal[i] - xLocal[i - 1];
                double dy = yLocal[i] - yLocal[i - 1];
                sLocal[i] = sLocal[i - 1] + Math.Sqrt(dx * dx + dy * dy);
            }
            double[] xpLocal = FmmSpline(sLocal, xLocal);
            double[] ypLocal = FmmSpline(sLocal, yLocal);

            sLower = new double[np];
            xLower = new double[np];
            yLower = new double[np];
            xpLower = new double[np];
            ypLower = new double[np];
            for (int i = 0; i < np; i++)
            {
                sLower[i] = sLocal[np - 1 + i];
                xLower[i] = xLocal[np - 1 + i];
                yLower[i] = -yLocal[np - 1 + i];
                xpLower[i] = xpLocal[np - 1 + i];
                ypLower[i] = -ypLocal[np - 1 + i];
            }
        }

        internal double Evaluate(double x)
        {
            if (x <= 0.0 || x >= 1.0) return 0.0;
            double sx = SplineZero(sLower, xLower, xpLower, x, 1e-12);
            return PcLookup(sLower, yLower, ypLower, sx);
        }
    }

    internal static CatalogGenerator.NacaShape Naca6(int familyIndex, double cl, double toc, bool is6A, string header)
    {
        var thickness = new Naca6Thickness(familyIndex, toc);
        double y95 = is6A ? thickness.Evaluate(0.95) : 0.0;
        return FrameAndFormat(header, (x, sign) =>
        {
            double yt = is6A && x > 0.95 ? y95 * ((1.0 - x) / 0.05) : thickness.Evaluate(x);
            (double yc, double slope) = cl == 0.0
                ? (0.0, 0.0)
                : is6A ? MeanLine6M(cl, x) : MeanLine6(1.0, cl, x);
            return Offset(x, yt, yc, slope, sign);
        }, cl == 0.0);
    }

    // ---- DTMB Report 1780 NACA 66(MOD) with a = 0.8 camber (Brockett 1966, closed TE) ----
    internal static CatalogGenerator.NacaShape Naca66Mod(double cl, double toc, string header)
    {
        var naca66 = new Naca6Thickness(3, toc);
        double yAt45 = naca66.Evaluate(0.45);
        double foreScale = (0.5 * toc) / yAt45;
        return FrameAndFormat(header, (x, sign) =>
        {
            double yt;
            if (x <= 0.45) yt = naca66.Evaluate(x) * foreScale;
            else
            {
                double u = (x - 0.45) / 0.55;
                yt = 0.5 * toc * (1.0 - u * u);
            }
            (double yc, double slope) = cl == 0.0 ? (0.0, 0.0) : MeanLine6(0.8, cl, x);
            return Offset(x, yt, yc, slope, sign);
        }, cl == 0.0);
    }

    // ---- Project-owned degree-7 Bernstein CST (cfdw-cst/1) ----
    internal static CatalogGenerator.NacaShape CfdwCst(string header, double[] upperW, double[] lowerW)
    {
        var upper = new (double X, double Y)[81];
        var lower = new (double X, double Y)[81];
        for (int index = 0; index <= 80; index++)
        {
            double x = CatalogGenerator.Spacing(index);
            upper[index] = (x, EvalCst(upperW, x));
            lower[index] = (x, EvalCst(lowerW, x));
        }
        upper[0] = (0.0, 0.0);
        lower[0] = (0.0, 0.0);
        upper[80] = (1.0, 0.0);
        lower[80] = (1.0, 0.0);
        var text = new StringBuilder(header).Append('\n');
        for (int index = 80; index >= 0; index--) Append(text, upper[index]);
        for (int index = 1; index <= 80; index++) Append(text, lower[index]);
        return new(Encoding.UTF8.GetBytes(text.ToString()), 0.0, 0.0, 1.0);
    }

    private static double EvalCst(double[] weights, double x)
    {
        if (x <= 0.0 || x >= 1.0) return 0.0;
        double omx = 1.0 - x;
        double shape = 0.0;
        for (int i = 0; i < 8; i++)
            shape += weights[i] * Binomial7[i] * PureMath.PowInt(x, i) * PureMath.PowInt(omx, 7 - i);
        return Math.Sqrt(x) * omx * shape;
    }

    private static double LinearLookup(double[] xs, double[] ys, double u)
    {
        if (u <= xs[0]) return ys[0];
        if (u >= xs[^1]) return ys[^1];
        for (int i = 0; i < xs.Length - 1; i++)
            if (u <= xs[i + 1])
                return ys[i] + (ys[i + 1] - ys[i]) * (u - xs[i]) / (xs[i + 1] - xs[i]);
        return ys[^1];
    }

    private static double[] FmmSpline(double[] x, double[] y)
    {
        int n = x.Length;
        var yp = new double[n];
        var dx = new double[n - 1];
        var delta = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            dx[i] = x[i + 1] - x[i];
            delta[i] = (y[i + 1] - y[i]) / dx[i];
        }
        var dd = new double[n - 2];
        for (int i = 0; i < n - 2; i++) dd[i] = delta[i + 1] - delta[i];

        var alpha = new double[n];
        var beta = new double[n];
        var sigma = new double[n];
        alpha[0] = -dx[0];
        for (int i = 1; i < n - 1; i++) alpha[i] = 2.0 * (dx[i - 1] + dx[i]);
        for (int i = 1; i < n - 1; i++) alpha[i] -= dx[i - 1] * dx[i - 1] / alpha[i - 1];
        alpha[n - 1] = -dx[n - 2] - dx[n - 2] * dx[n - 2] / alpha[n - 2];

        beta[0] = (dd[1] / (x[3] - x[1]) - dd[0] / (x[2] - x[0])) * dx[0] * dx[0] / (x[3] - x[0]);
        for (int i = 1; i < n - 1; i++) beta[i] = dd[i - 1];
        beta[n - 1] = -(dd[n - 3] / (x[n - 1] - x[n - 3]) - dd[n - 4] / (x[n - 2] - x[n - 4])) * dx[n - 2] * dx[n - 2] / (x[n - 1] - x[n - 4]);

        for (int i = 1; i < n; i++) beta[i] -= dx[i - 1] * beta[i - 1] / alpha[i - 1];
        sigma[n - 1] = beta[n - 1] / alpha[n - 1];
        for (int i = n - 2; i >= 0; i--) sigma[i] = (beta[i] - dx[i] * sigma[i + 1]) / alpha[i];

        for (int i = 0; i < n - 1; i++) yp[i] = delta[i] - dx[i] * (2.0 * sigma[i] + sigma[i + 1]);
        yp[n - 1] = yp[n - 2] + dx[n - 2] * 3.0 * (sigma[n - 1] + sigma[n - 2]);
        return yp;
    }

    private static double EvalCubic(double a, double fa, double fpa, double b, double fb, double fpb, double u)
    {
        double h = b - a;
        double d = (fb - fa) / h;
        double t = (u - a) / h;
        double p = 1.0 - t;
        return p * fa + t * fb - p * t * h * (p * (d - fpa) - t * (d - fpb));
    }

    private static double PcLookup(double[] x, double[] y, double[] yp, double u)
    {
        int n = x.Length;
        int lo = 0, hi = n - 1;
        if (u <= x[0]) lo = 0;
        else if (u >= x[n - 1]) lo = n - 2;
        else
        {
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (u < x[mid]) hi = mid; else lo = mid;
            }
        }
        return EvalCubic(x[lo], y[lo], yp[lo], x[lo + 1], y[lo + 1], yp[lo + 1], u);
    }

    private static double SplineZero(double[] x, double[] f, double[] fp, double target, double tol)
    {
        int n = x.Length;
        for (int k = 0; k < n; k++)
            if (Math.Abs(f[k] - target) < tol) return x[k];
        int idx = -1;
        for (int k = 1; k < n; k++)
            if ((f[k - 1] - target) * (f[k] - target) <= 0.0) { idx = k; break; }
        if (idx < 0) return target <= f[0] ? x[0] : x[n - 1];
        double xa = x[idx - 1], xb = x[idx];
        double fa0 = f[idx - 1] - target, fb0 = f[idx] - target;
        double fpa = fp[idx - 1], fpb = fp[idx];
        double lo = xa, hi = xb, flo = fa0;
        for (int iter = 0; iter < 80; iter++)
        {
            double mid = 0.5 * (lo + hi);
            double fmid = EvalCubic(xa, fa0, fpa, xb, fb0, fpb, mid);
            if (Math.Abs(fmid) < 1e-15 || (hi - lo) <= 1e-15 * (1.0 + Math.Abs(mid))) return mid;
            if ((flo <= 0.0 && fmid <= 0.0) || (flo >= 0.0 && fmid >= 0.0)) { lo = mid; flo = fmid; }
            else hi = mid;
        }
        return 0.5 * (lo + hi);
    }
}
