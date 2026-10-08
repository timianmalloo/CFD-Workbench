using System.Globalization;
using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.NeuralFoil;

/// <summary>
/// The validated section, derived from geometry. SPIKE-ANA-1 validated NACA 0012 against XFOIL; a section keeps that
/// claim only if its CST ordinates match the catalog's NACA 0012 (the generated Selig coordinates in Core). A caller's
/// family string is never trusted, so an edited section cannot claim the validated family.
/// </summary>
public static class Naca0012Reference
{
    // The catalog section is closed at the trailing edge; the spike's NeuralFoil case is open by 0.25 percent chord, a
    // 1.3e-3 c ordinate difference at the trailing edge. 1.5e-3 c accepts both and rejects a 4e-3 c edit.
    public const double MatchTolerance = 1.5e-3;

    private static readonly Lazy<CstParameters> Parameters = new(() =>
        CstFit.Fit(FromSelig(new string('0', 64), "naca0012", ShippedBytes())).Parameters);

    // GEO-A (Ruling 156): the bytes of record are the shipped catalog bytes, not a second evaluation of the generator.
    private static byte[] ShippedBytes() =>
        Catalog.Load().Single(entry => entry.Id == "naca-0012").Coordinates ?? throw new ContractError("CAT-UNAVAILABLE");

    /// <summary>Largest ordinate difference, in chord lengths, between a fitted section and the catalog NACA 0012.</summary>
    public static double MaxOrdinateDifference(CstParameters fitted)
    {
        ArgumentNullException.ThrowIfNull(fitted);
        double maximum = 0;
        for (int i = 0; i < 200; i++)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / 199));
            foreach (bool upper in new[] { true, false })
                maximum = Math.Max(maximum, Math.Abs(CstFit.Ordinate(fitted, x, upper) - CstFit.Ordinate(Parameters.Value, x, upper)));
        }
        return maximum;
    }

    public static bool Matches(CstParameters fitted) => MaxOrdinateDifference(fitted) <= MatchTolerance;

    /// <summary>Parse Selig coordinates (upper trailing edge to leading edge to lower trailing edge, shared x stations).</summary>
    public static NeuralFoilSection FromSelig(string profileHash, string label, byte[] coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        var points = new List<(double X, double Y)>();
        string[] lines = Encoding.UTF8.GetString(coordinates).Split('\n');
        foreach (string line in lines.Skip(1))
        {
            string[] field = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (field.Length == 0) continue;
            if (field.Length != 2 || !double.TryParse(field[0], CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(field[1], CultureInfo.InvariantCulture, out double y))
                throw new ContractError("ANA-POLAR-CST-INPUT", "Selig coordinates must be two numbers per line.");
            points.Add((Math.Max(0, x), y));
        }
        int leading = points.IndexOf(points.MinBy(point => point.X));
        int count = leading + 1;
        if (points.Count != 2 * leading + 1 || leading < 19)
            throw new ContractError("ANA-POLAR-CST-INPUT", "Selig coordinates need matching upper and lower stations.");
        var stations = new double[count];
        var upper = new double[count];
        var lower = new double[count];
        for (int i = 0; i < count; i++)
        {
            (double ux, double uy) = points[leading - i];
            (double lx, double ly) = points[leading + i];
            if (Math.Abs(ux - lx) > 1e-9)
                throw new ContractError("ANA-POLAR-CST-INPUT", "Selig upper and lower stations differ.");
            stations[i] = ux; upper[i] = uy; lower[i] = ly;
        }
        return new NeuralFoilSection(profileHash, label, stations, upper, lower);
    }
}
