using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core;

// assume: CatalogFamily is the picker grouping from design §3.1 (Naca, Eppler, Speer, MySections).
// The §5.1 snippet repeats the admission classes, which cannot group the dialog (§14.1).
// Confirm: a DLG build groups rows by Family. Breaks if a caller expected Family to be Gen/Vend/Link.
public enum CatalogFamily
{
    Naca,
    Eppler,
    Speer,
    MySections
}

public enum AdmissionClass
{
    Gen,
    Vend,
    Link
}

public sealed record CatalogEntry(
    string Id,
    CatalogFamily Family,
    string Designation,
    AdmissionClass Class,
    string? DisabledReason,
    byte[]? Coordinates)
{
    public double FrameLeShift { get; init; }
    public double FrameRotationDegrees { get; init; }
    public double FrameScale { get; init; }
    internal string? CoordinateHash { get; init; }
}

public static class Catalog
{
    private const string ResourcePrefix = "CfdWorkbench.Core.CatalogData.";

    public static IReadOnlyList<CatalogEntry> Load()
    {
        byte[]? table = Resource(ResourcePrefix + "catalog.tsv");
        if (table is null) throw new ContractError("CAT-UNAVAILABLE");
        return Read(Encoding.UTF8.GetString(table), id => Resource(ResourcePrefix + id + ".dat"));
    }

    internal static IReadOnlyList<CatalogEntry> Read(string table, Func<string, byte[]?> coordinates)
    {
        var entries = new List<CatalogEntry>();
        foreach (string raw in table.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;
            string[] field = line.Split('\t');
            if (field.Length != 10) throw new ContractError("CAT-UNAVAILABLE");
            string id = field[0];
            string? reason = field[4].Length == 0 ? null : field[4];
            string hash = field[6];
            double le = double.Parse(field[7], CultureInfo.InvariantCulture);
            double rotation = double.Parse(field[8], CultureInfo.InvariantCulture);
            double scale = double.Parse(field[9], CultureInfo.InvariantCulture);
            byte[]? bytes = null;
            if (reason is null)
            {
                bytes = coordinates(id);
                if (bytes is null || Identity.Sha256(bytes) != hash) throw new ContractError("CAT-UNAVAILABLE");
                if (field[5] != CatalogGenerator.Id || !id.StartsWith("naca-", StringComparison.Ordinal))
                    throw new ContractError("CAT-UNAVAILABLE");
                CatalogGenerator.NacaShape shape = CatalogGenerator.Shape(id["naca-".Length..]);
                if (!bytes.AsSpan().SequenceEqual(shape.Bytes) || shape.LeShift != le || shape.RotationDegrees != rotation || shape.Scale != scale)
                    throw new ContractError("CAT-UNAVAILABLE");
            }
            entries.Add(new CatalogEntry(
                id,
                Enum.Parse<CatalogFamily>(field[1]),
                field[2],
                Enum.Parse<AdmissionClass>(field[3]),
                reason,
                bytes)
            {
                FrameLeShift = le,
                FrameRotationDegrees = rotation,
                FrameScale = scale,
                CoordinateHash = reason is null ? hash : null
            });
        }
        return entries;
    }

    private static byte[]? Resource(string name)
    {
        using Stream? stream = typeof(Catalog).Assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

public static class CatalogGenerator
{
    public const string Id = "naca4-closed/1";

    internal readonly record struct NacaShape(byte[] Bytes, double LeShift, double RotationDegrees, double Scale);

    public static byte[] Naca4(string digits) => Shape(digits).Bytes;

    internal static NacaShape Shape(string digits)
    {
        double m = (digits[0] - '0') / 100.0;
        double p = (digits[1] - '0') / 10.0;
        double t = int.Parse(digits[2..], CultureInfo.InvariantCulture) / 100.0;
        const double a4 = -0.1036;
        (double X, double Y) Surface(double x, int sign)
        {
            double square = x * x;
            double yt = 5 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * square + 0.2843 * square * x + a4 * square * square);
            double yc = 0, slope = 0;
            if (m != 0)
            {
                if (x < p)
                {
                    yc = m / (p * p) * (2 * p * x - square);
                    slope = 2 * m / (p * p) * (p - x);
                }
                else
                {
                    double q = 1 - p;
                    yc = m / (q * q) * (1 - 2 * p + 2 * p * x - square);
                    slope = 2 * m / (q * q) * (p - x);
                }
            }
            double theta = Math.Atan(slope);
            return (x - sign * yt * Math.Sin(theta), yc + sign * yt * Math.Cos(theta));
        }

        double left = 0, right = 0.05;
        for (int step = 0; step < 200; step++)
        {
            double c1 = right - (right - left) / 1.618033988749895;
            double c2 = left + (right - left) / 1.618033988749895;
            if (Surface(c1, 1).X < Surface(c2, 1).X) right = c2;
            else left = c1;
        }
        double xs = (left + right) / 2;
        if (Surface(xs, 1).X > 0) xs = 0;
        (double X, double Y) le = Surface(xs, 1);
        (double X, double Y) teUpper = Surface(1, 1);
        (double X, double Y) teLower = Surface(1, -1);
        double mx = (teUpper.X + teLower.X) / 2 - le.X;
        double my = (teUpper.Y + teLower.Y) / 2 - le.Y;
        double length = Math.Sqrt(mx * mx + my * my);
        double angle = Math.Atan2(my, mx);
        (double X, double Y) Frame((double X, double Y) point)
        {
            double dx = point.X - le.X, dy = point.Y - le.Y;
            return ((dx * Math.Cos(-angle) - dy * Math.Sin(-angle)) / length,
                (dx * Math.Sin(-angle) + dy * Math.Cos(-angle)) / length);
        }

        var upper = new (double X, double Y)[81];
        var lower = new (double X, double Y)[81];
        for (int index = 0; index <= 80; index++)
        {
            double s = 0.5 * (1 - Math.Cos(Math.PI * index / 80.0));
            upper[index] = Frame(Surface(xs + (1 - xs) * s, 1));
            double distance = s * (xs + 1);
            lower[index] = Frame(distance <= xs ? Surface(xs - distance, 1) : Surface(distance - xs, -1));
        }
        upper[0] = (0, 0);
        lower[0] = (0, 0);

        var text = new StringBuilder("NACA ").Append(digits).Append('\n');
        for (int index = 80; index >= 0; index--) Append(text, upper[index]);
        for (int index = 1; index <= 80; index++) Append(text, lower[index]);
        return new(Encoding.UTF8.GetBytes(text.ToString()), le.X, angle * 180 / Math.PI, length);
    }

    private static void Append(StringBuilder text, (double X, double Y) point) =>
        text.Append(point.X.ToString("G17", CultureInfo.InvariantCulture)).Append(' ')
            .Append(point.Y.ToString("G17", CultureInfo.InvariantCulture)).Append('\n');
}
