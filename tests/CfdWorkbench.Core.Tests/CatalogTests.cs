using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class CatalogTests
{
    private const string NotGenerated = "Not generated in this build. GEN sections above can be used.";
    private const string Pending = "Pending admission — terms requested from UIUC · GEN sections remain";
    private const string CiteOnly = "Cite only (LINK) — its coordinates may not be copied, so it cannot be edited here";

    internal static void Run()
    {
        Check("Catalog_GenEntries_RegenerateToRecordedHash", GenEntriesRegenerate);
        Check("Catalog_HashMismatch_CatUnavailable", HashMismatch);
        Check("Catalog_VendAndLink_NoCoordinates", VendAndLink);
        Check("Catalog_Fairings_NeverListed", FairingsNeverListed);
        Check("CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations", Naca0012ClosedForm);
        Check("CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX", ClosedTe4412Frame);
        Check("Catalog_GenNeverThroughDatParse", GenNeverThroughDatParse);
    }

    private static void GenEntriesRegenerate()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        string[] choosable = ["naca-0009", "naca-0012", "naca-4412"];
        foreach (string id in choosable)
        {
            CatalogEntry entry = entries.Single(item => item.Id == id);
            Equal(CatalogFamily.Naca, entry.Family);
            Equal(AdmissionClass.Gen, entry.Class);
            Equal(null, entry.DisabledReason);
            byte[] generated = CatalogGenerator.Naca4(id["naca-".Length..]);
            Equal(true, generated.AsSpan().SequenceEqual(entry.Coordinates));
            Equal(Identity.Sha256(generated), entry.CoordinateHash);
            Equal(CatalogGenerator.Id, "naca4-closed/1");
        }
        foreach (string id in new[] { "naca-16-012", "naca-63-209", "naca-63-412", "naca-64a410", "naca-66-012", "naca-66-209", "naca-66-018" })
        {
            CatalogEntry entry = entries.Single(item => item.Id == id);
            Equal(CatalogFamily.Naca, entry.Family);
            Equal(AdmissionClass.Gen, entry.Class);
            Equal(NotGenerated, entry.DisabledReason);
            Equal(null, entry.Coordinates);
        }
    }

    private static void HashMismatch()
    {
        const string row = "naca-0012\tNaca\tNACA 0012\tGen\t\tnaca4-closed/1\t" +
            "0000000000000000000000000000000000000000000000000000000000000000\t0\t0\t1\n";
        ExpectUnavailable(() => Catalog.Read(row, _ => CatalogGenerator.Naca4("0012")));
        ExpectUnavailable(() => Catalog.Read(row, _ => null));
    }

    private static void VendAndLink()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        foreach (string id in new[] { "e817", "e818", "e874", "e904", "e908", "e836", "e837", "e838" })
        {
            CatalogEntry entry = entries.Single(item => item.Id == id);
            Equal(CatalogFamily.Eppler, entry.Family);
            Equal(AdmissionClass.Vend, entry.Class);
            Equal(Pending, entry.DisabledReason);
            Equal(null, entry.Coordinates);
            Equal(true, entry.Designation.Contains(id[1..].ToUpperInvariant(), StringComparison.Ordinal));
        }
        CatalogEntry speer = entries.Single(item => item.Id == "speer-h105");
        Equal(CatalogFamily.Speer, speer.Family);
        Equal(AdmissionClass.Link, speer.Class);
        Equal(CiteOnly, speer.DisabledReason);
        Equal("Speer H105", speer.Designation);
        Equal(null, speer.Coordinates);
    }

    private static void FairingsNeverListed()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        Equal(19, entries.Count);
        foreach (CatalogEntry entry in entries)
        {
            string text = entry.Id + " " + entry.Designation;
            Equal(false, text.Contains("862", StringComparison.OrdinalIgnoreCase));
            Equal(false, text.Contains("863", StringComparison.OrdinalIgnoreCase));
            Equal(false, text.Contains("864", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void Naca0012ClosedForm()
    {
        (double X, double Y)[] upper = Stations(CatalogGenerator.Naca4("0012"), upper: true);
        (double X, double Y)[] lower = Stations(CatalogGenerator.Naca4("0012"), upper: false);
        Equal(81, upper.Length);
        Equal(81, lower.Length);
        for (int index = 0; index <= 80; index++)
        {
            double x = Cosine(index);
            double yt = ClosedThickness(0.12, x);
            Near(x, upper[index].X);
            Near(yt, upper[index].Y);
            Near(x, lower[index].X);
            Near(-yt, lower[index].Y);
        }
        double open = OpenThickness(0.12, Cosine(40));
        Equal(true, Math.Abs(upper[40].Y - open) > 1e-6);
    }

    private static void ClosedTe4412Frame()
    {
        CatalogEntry entry = Catalog.Load().Single(item => item.Id == "naca-4412");
        (double X, double Y)[] upper = Stations(entry.Coordinates!, upper: true);
        (double X, double Y)[] lower = Stations(entry.Coordinates!, upper: false);
        Equal(0d, upper[0].X);
        Equal(0d, upper[0].Y);
        Equal(0d, lower[0].X);
        Equal(0d, lower[0].Y);
        double minX = upper.Concat(lower).Min(point => point.X);
        Equal(true, minX >= -1e-12);
        Near(1, upper[^1].X);
        Near(1, lower[^1].X);
        Equal("0.0003", Math.Abs(entry.FrameLeShift).ToString("G4", CultureInfo.InvariantCulture));
        Equal("0.17", Math.Abs(entry.FrameRotationDegrees).ToString("G4", CultureInfo.InvariantCulture));
        Equal("1", entry.FrameScale.ToString("G4", CultureInfo.InvariantCulture));
    }

    private static void GenNeverThroughDatParse()
    {
        byte[] catalog = CatalogGenerator.Naca4("4412");
        Equal(true, catalog.AsSpan().SequenceEqual(Catalog.Load().Single(item => item.Id == "naca-4412").Coordinates));
        string text = Encoding.UTF8.GetString(catalog);
        Equal(false, text.Contains("foildsl", StringComparison.Ordinal));
        Equal(false, text.Contains("profile ", StringComparison.Ordinal));
        DatProfile parsed = DatImport.Parse(RawClosedSelig("4412"));
        (double X, double Y)[] framed = Stations(catalog, upper: true);
        double gap = 0;
        for (int index = 0; index < parsed.Upper.Count && index < framed.Length; index++)
            gap = Math.Max(gap, Math.Abs(parsed.Upper[index].Y - framed[index].Y));
        Equal(true, gap > 1e-4);
    }

    private static void ExpectUnavailable(Action action)
    {
        try
        {
            action();
        }
        catch (ContractError error)
        {
            Equal("CAT-UNAVAILABLE", error.Code);
            return;
        }
        throw new InvalidOperationException("expected CAT-UNAVAILABLE");
    }

    private static (double X, double Y)[] Stations(byte[] bytes, bool upper)
    {
        string[] lines = Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var points = new (double X, double Y)[81];
        for (int index = 0; index <= 80; index++)
        {
            int line = upper || index == 0 ? 1 + (80 - index) : 1 + 80 + index;
            string[] parts = lines[line].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            points[index] = (
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture));
        }
        return points;
    }

    private static byte[] RawClosedSelig(string digits)
    {
        double m = (digits[0] - '0') / 100.0;
        double p = (digits[1] - '0') / 10.0;
        double t = int.Parse(digits[2..], CultureInfo.InvariantCulture) / 100.0;
        var upper = new (double X, double Y)[81];
        var lower = new (double X, double Y)[81];
        for (int index = 0; index <= 80; index++)
        {
            double x = Cosine(index);
            double yt = ClosedThickness(t, x);
            (double Yc, double Slope) camber = Camber(m, p, x);
            double theta = Math.Atan(camber.Slope);
            upper[index] = (x - yt * Math.Sin(theta), camber.Yc + yt * Math.Cos(theta));
            lower[index] = (x + yt * Math.Sin(theta), camber.Yc - yt * Math.Cos(theta));
        }
        var text = new StringBuilder("NACA ").Append(digits).Append('\n');
        for (int index = 80; index >= 0; index--) text.Append(F(upper[index].X)).Append(' ').Append(F(upper[index].Y)).Append('\n');
        for (int index = 1; index <= 80; index++) text.Append(F(lower[index].X)).Append(' ').Append(F(lower[index].Y)).Append('\n');
        return Encoding.UTF8.GetBytes(text.ToString());
    }

    private static (double Yc, double Slope) Camber(double m, double p, double x)
    {
        if (m == 0) return (0, 0);
        if (x < p) return (m / (p * p) * (2 * p * x - x * x), 2 * m / (p * p) * (p - x));
        double q = 1 - p;
        return (m / (q * q) * (1 - 2 * p + 2 * p * x - x * x), 2 * m / (q * q) * (p - x));
    }

    private static double Cosine(int index) => 0.5 * (1 - Math.Cos(Math.PI * index / 80.0));

    private static double ClosedThickness(double t, double x) => Thickness(t, x, -0.1036);

    private static double OpenThickness(double t, double x) => Thickness(t, x, -0.1015);

    private static double Thickness(double t, double x, double a4)
    {
        double square = x * x;
        return 5 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * square + 0.2843 * square * x + a4 * square * square);
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static void Near(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 1e-12)
            throw new InvalidOperationException("Expected " + expected.ToString("G17", CultureInfo.InvariantCulture) +
                "; actual " + actual.ToString("G17", CultureInfo.InvariantCulture));
    }
}
