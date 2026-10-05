using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// Fresh water and standard seawater at integer degrees from ITTC 7.5-02-01-03 Rev 03, hashed at load (design §10, P-7).
/// Temperature and salinity are interpolated linearly. A query outside 0–50 °C or the two salinity columns is refused
/// and never clamped. The 0 °C row is the one-step extension of the published 0.1 °C and 0.2 °C rows.
/// </summary>
public static class WaterTable
{
    public const string Source = "ITTC 7.5-02-01-03 Rev 03";
    public const string OutsideReason = "Unavailable — outside the ITTC table (0–50 °C)";
    public const string FailedCheckReason = "Unavailable — water table failed its check";

    /// <summary>BLAKE3 of the committed table. A one-bit change of the resource fails the load.</summary>
    internal const string PinnedHash = "fca86591cf575f50c0c17ca6de8dac6372d0c2baaa40f7711d184c2b6e154480";

    private static readonly byte[] Bytes = Load();
    private static readonly Node[] Fresh = new Node[51];
    private static readonly Node[] Salt = new Node[51];

    static WaterTable()
    {
        Parse(Bytes, Fresh, Salt);
    }

    internal static byte[] TableBytes => Bytes;

    public static WaterRecord At(double temperatureC, double salinityGPerKg)
    {
        string hash = Identity.Blake3(Bytes);
        if (!string.Equals(hash, PinnedHash, StringComparison.Ordinal))
            throw new ContractError("ANA-INPUT-WATER", FailedCheckReason);
        return Lookup(temperatureC, salinityGPerKg, Fresh, Salt, hash);
    }

    internal static WaterRecord At(double temperatureC, double salinityGPerKg, byte[] table)
    {
        string hash = Identity.Blake3(table);
        if (!string.Equals(hash, PinnedHash, StringComparison.Ordinal))
            throw new ContractError("ANA-INPUT-WATER", FailedCheckReason);
        var fresh = new Node[51];
        var salt = new Node[51];
        Parse(table, fresh, salt);
        return Lookup(temperatureC, salinityGPerKg, fresh, salt, hash);
    }

    private static WaterRecord Lookup(double temperatureC, double salinityGPerKg, Node[] fresh, Node[] salt, string hash)
    {
        if (!double.IsFinite(temperatureC) || temperatureC < 0 || temperatureC > 50)
            throw new ContractError("ANA-INPUT-WATER", OutsideReason);
        double saltColumn = OperatingPoints.SaltSalinityGPerKg;
        if (!double.IsFinite(salinityGPerKg) || salinityGPerKg < 0 || salinityGPerKg > saltColumn)
            throw new ContractError("ANA-INPUT-WATER", "Unavailable — outside the ITTC table (salinity 0–35.16504 g/kg)");
        Node left = AtTemperature(fresh, temperatureC);
        Node right = AtTemperature(salt, temperatureC);
        double fraction = salinityGPerKg / saltColumn;
        Node mixed = fraction <= 1e-15 ? left : fraction >= 1 - 1e-12 ? right : Blend(left, right, fraction);
        return new WaterRecord(temperatureC, salinityGPerKg, mixed.Rho, mixed.Nu, mixed.Pv, Source, hash);
    }

    private static Node AtTemperature(Node[] column, double temperatureC)
    {
        double index = temperatureC;
        int lower = (int)Math.Floor(index + 1e-12);
        if (lower >= 50) return column[50];
        double fraction = index - lower;
        if (fraction <= 1e-12) return column[lower];
        return Blend(column[lower], column[lower + 1], fraction);
    }

    private static Node Blend(Node left, Node right, double fraction) => new(
        left.Rho + (right.Rho - left.Rho) * fraction,
        left.Nu + (right.Nu - left.Nu) * fraction,
        left.Pv + (right.Pv - left.Pv) * fraction);

    private static void Parse(byte[] table, Node[] fresh, Node[] salt)
    {
        Array.Fill(fresh, new Node(double.NaN, double.NaN, double.NaN));
        Array.Fill(salt, new Node(double.NaN, double.NaN, double.NaN));
        string text = System.Text.Encoding.UTF8.GetString(table);
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            string[] field = line.Split(',');
            if (field.Length != 5) throw new ContractError("ANA-INPUT-WATER", FailedCheckReason);
            int temperature = int.Parse(field[0], CultureInfo.InvariantCulture);
            double salinity = double.Parse(field[1], CultureInfo.InvariantCulture);
            var node = new Node(
                double.Parse(field[2], CultureInfo.InvariantCulture),
                double.Parse(field[3], CultureInfo.InvariantCulture),
                double.Parse(field[4], CultureInfo.InvariantCulture));
            if (salinity == 0) fresh[temperature] = node;
            else salt[temperature] = node;
        }
        for (int i = 0; i <= 50; i++)
        {
            if (!double.IsFinite(fresh[i].Rho) || !double.IsFinite(salt[i].Rho))
                throw new ContractError("ANA-INPUT-WATER", FailedCheckReason);
        }
    }

    private static byte[] Load()
    {
        using Stream? stream = typeof(WaterTable).Assembly.GetManifestResourceStream("CfdWorkbench.Analysis.IttcWater.csv");
        if (stream is null) throw new InvalidOperationException("the ITTC water table is not in the assembly");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private readonly record struct Node(double Rho, double Nu, double Pv);
}
