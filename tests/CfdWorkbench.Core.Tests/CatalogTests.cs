using System.Globalization;
using System.Numerics;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class CatalogTests
{
    private const string Pending = "Pending admission — terms requested from UIUC · GEN sections remain";
    private const string CiteOnly = "Cite only (LINK) — its coordinates may not be copied, so it cannot be edited here";

    internal static void Run()
    {
        Check("Catalog_GenEntries_RegenerateToRecordedHash", GenEntriesRegenerate);
        Check("Catalog_HashMismatch_CatUnavailable", HashMismatch);
        Check("Catalog_Refusal_NamesItsCheck", RefusalNamesItsCheck);
        Check("Catalog_VendAndLink_NoCoordinates", VendAndLink);
        Check("Catalog_VendAndLink_AdmissionGuards", VendAndLinkAdmissionGuards);
        Check("Catalog_InvalidEnumAndGenerator_Refused", InvalidEnumAndGeneratorRefused);
        Check("Catalog_Fairings_NeverListed", FairingsNeverListed);
        Check("CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations", Naca0012ClosedForm);
        Check("CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX", ClosedTe4412Frame);
        Check("CatalogGenerators_PhysicalGeometry_GoldenChecks", PhysicalGeometryGoldenChecks);
        Check("Catalog_GenNeverThroughDatParse", GenNeverThroughDatParse);
        Check("CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy", SpacingGoldenAndAccuracy);
        Check("CatalogGenerator_PureMath_MatchesIeeeReference", PureMathAccuracy);
        Check("DatImport_OwnAdaptiveSqrtBasis_BoundedAndMonotonic", AdaptiveSqrtBasisBounded);
        Check("Catalog_All77ActiveGen_FitWithin10Microns", AllActiveGenFitWithin10Microns);
    }

    private static void GenEntriesRegenerate()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        // Existing NACA 4-digit golden hashes remain 100% byte-identical.
        (string Id, string Hash)[] preserved =
        [
            ("naca-0009", "dd2dcade3892cde15f24d017dd0e1f8ed7e3e25b66de4d848fae3935cccb3b1c"),
            ("naca-0012", "9ef9fe41168f3407928962d21d337317be2e2b40daaae4d645f847bfbb297c9b"),
            ("naca-4412", "bf9e75bfd12cbe534107be3811d65fb447b34d7113929ce2d5ec509ff7cdf16d")
        ];
        foreach (var (id, expectedHash) in preserved)
        {
            CatalogEntry entry = entries.Single(item => item.Id == id);
            Equal(CatalogFamily.Naca, entry.Family);
            Equal(AdmissionClass.Gen, entry.Class);
            Equal(null, entry.DisabledReason);
            byte[] generated = CatalogGenerator.Naca4(id["naca-".Length..]);
            Equal(true, generated.AsSpan().SequenceEqual(entry.Coordinates));
            Equal(expectedHash, entry.CoordinateHash);
            Equal(CatalogGenerator.Id, "naca4-closed/2");
        }
        // Previously-disabled NACA 16/6/6A rows are now active GEN sections.
        foreach (string id in new[] { "naca-16-012", "naca-63-209", "naca-63-412", "naca-64a410", "naca-66-012", "naca-66-209", "naca-66-018" })
        {
            CatalogEntry entry = entries.Single(item => item.Id == id);
            Equal(CatalogFamily.Naca, entry.Family);
            Equal(AdmissionClass.Gen, entry.Class);
            Equal(null, entry.DisabledReason);
            Equal(true, entry.Coordinates is { Length: > 1000 });
        }
        // All 9 project-owned CFDW Hydrofoil CST sections are active GEN entries.
        var hydrofoils = entries.Where(item => item.Family == CatalogFamily.Hydrofoil).ToArray();
        Equal(9, hydrofoils.Length);
        foreach (CatalogEntry entry in hydrofoils)
        {
            Equal(AdmissionClass.Gen, entry.Class);
            Equal(null, entry.DisabledReason);
            var (generated, _, _, _) = CatalogGenerators.Generate(CatalogGenerators.CfdwCstId, entry.Id);
            Equal(true, generated.AsSpan().SequenceEqual(entry.Coordinates));
            Equal(Identity.Sha256(generated), entry.CoordinateHash);
        }
        // Total active GEN count = 77 (68 public-domain NACA/DTMB + 9 CFDW Hydrofoil CST).
        Equal(77, entries.Count(item => item.Class == AdmissionClass.Gen && item.DisabledReason is null && item.Coordinates is not null));
    }

    private static void HashMismatch()
    {
        const string row = "naca-0012\tNaca\tNACA 0012\tGen\t\tnaca4-closed/2\t" +
            "0000000000000000000000000000000000000000000000000000000000000000\t0\t0\t1\n";
        ExpectUnavailable(() => Catalog.Read(row, _ => CatalogGenerator.Naca4("0012")));
        ExpectUnavailable(() => Catalog.Read(row, _ => null));
    }

    // WFX2 item 4: the Windows ring saw "a catalog file failed its check" with no way to tell which check. The thrown error names the
    // check and the first differing entry in Data (the Reason stays the approved COPY-135 text). The equality policy is unchanged.
    private static void RefusalNamesItsCheck()
    {
        byte[] good = CatalogGenerator.Naca4("0012");
        byte[] bent = (byte[])good.Clone();
        bent[bent.Length / 2] ^= 1;
        string Row(string hash, string le = "0") => $"naca-0012\tNaca\tNACA 0012\tGen\t\tnaca4-closed/2\t{hash}\t{le}\t0\t1\n";
        string zero = new('0', 64), goodHash = Identity.Sha256(good), bentHash = Identity.Sha256(bent);

        var hash = Planted(Row(zero), _ => good);
        Equal("coordinate-hash", (string)hash.Data["check"]!);
        Equal(true, ((string)hash.Data["detail"]!).Contains("naca-0012") && ((string)hash.Data["detail"]!).Contains(zero) &&
            ((string)hash.Data["detail"]!).Contains(goodHash));
        Equal("coordinates-missing", (string)Planted(Row(zero), _ => null).Data["check"]!);
        var bytes = Planted(Row(bentHash), _ => bent);
        Equal("generated-bytes", (string)bytes.Data["check"]!);
        Equal(true, ((string)bytes.Data["detail"]!).Contains($"first differing byte {bent.Length / 2}"));
        var frame = Planted(Row(goodHash, le: "0.5"), _ => good);
        Equal("frame-le-shift", (string)frame.Data["check"]!);
        Equal(true, ((string)frame.Data["detail"]!).Contains("0.5"));
        Equal("field-count", (string)Planted("naca-0012\tNaca\n", _ => good).Data["check"]!);
    }

    private static ContractError Planted(string table, Func<string, byte[]?> coordinates)
    {
        try { Catalog.Read(table, coordinates); }
        catch (ContractError error) { Equal("CAT-UNAVAILABLE", error.Code); return error; }
        throw new InvalidOperationException("expected CAT-UNAVAILABLE");
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

    private static void VendAndLinkAdmissionGuards()
    {
        byte[] validDat = CatalogGenerator.Naca4("0012");
        string validHash = Identity.Sha256(validDat);
        string vendOk = $"vend-sample\tEppler\tVend Sample\tVend\t\t\t{validHash}\t0\t0\t1\n";
        var loaded = Catalog.Read(vendOk, id => id == "vend-sample" ? validDat : null);
        Equal(1, loaded.Count);
        Equal(AdmissionClass.Vend, loaded[0].Class);
        Equal(true, loaded[0].Coordinates!.AsSpan().SequenceEqual(validDat));

        // Tampered VEND hash refused
        string vendBadHash = $"vend-sample\tEppler\tVend Sample\tVend\t\t\t{new string('0', 64)}\t0\t0\t1\n";
        Equal("coordinate-hash", (string)Planted(vendBadHash, _ => validDat).Data["check"]!);

        // Unparseable VEND coordinates refused
        byte[] garbage = Encoding.UTF8.GetBytes("not a valid dat");
        string vendGarbage = $"vend-sample\tEppler\tVend Sample\tVend\t\t\t{Identity.Sha256(garbage)}\t0\t0\t1\n";
        Equal("vend-parse", (string)Planted(vendGarbage, _ => garbage).Data["check"]!);

        // Disabled VEND carrying bundled bytes refused
        string vendDisabled = $"e817\tEppler\tEppler E817\tVend\t{Pending}\t\t\t0\t0\t1\n";
        Equal("disabled-has-coordinates", (string)Planted(vendDisabled, _ => validDat).Data["check"]!);

        // Active LINK refused; LINK carrying bytes refused
        string linkActive = $"speer-h105\tSpeer\tSpeer H105\tLink\t\t\t{validHash}\t0\t0\t1\n";
        Equal("link-active", (string)Planted(linkActive, _ => validDat).Data["check"]!);
        string linkWithBytes = $"speer-h105\tSpeer\tSpeer H105\tLink\t{CiteOnly}\t\t\t0\t0\t1\n";
        Equal("link-has-coordinates", (string)Planted(linkWithBytes, _ => validDat).Data["check"]!);
    }

    private static void PureMathAccuracy()
    {
        double maxLn = 0, maxExpRel = 0, maxSin = 0, maxCos = 0;
        for (int i = 1; i <= 1000; i++)
        {
            double x = i / 1000.0;
            maxLn = Math.Max(maxLn, Math.Abs(PureMath.Ln(x) - Math.Log(x)));
            double ex = (i - 500) / 100.0;
            maxExpRel = Math.Max(maxExpRel, Math.Abs(PureMath.Exp(ex) - Math.Exp(ex)) / Math.Exp(ex));
            double th = (i - 500) * 0.01;
            var (s, c) = PureMath.SinCos(th);
            maxSin = Math.Max(maxSin, Math.Abs(s - Math.Sin(th)));
            maxCos = Math.Max(maxCos, Math.Abs(c - Math.Cos(th)));
        }
        Equal(true, maxLn < 5e-15);
        Equal(true, maxExpRel < 5e-15);
        Equal(true, maxSin < 5e-15);
        Equal(true, maxCos < 5e-15);
        Equal(true, double.IsPositiveInfinity(PureMath.Exp(800.0)));
        Equal(0.0, PureMath.Exp(-800.0));
        Equal(true, double.IsNaN(PureMath.Exp(double.NaN)));
    }

    private static void InvalidEnumAndGeneratorRefused()
    {
        byte[] validDat = CatalogGenerator.Naca4("0012");
        string validHash = Identity.Sha256(validDat);
        string badFamily = $"naca-0012\t99\tNACA 0012\tGen\t\tnaca4-closed/2\t{validHash}\t0\t0\t1\n";
        Equal("family", (string)Planted(badFamily, _ => validDat).Data["check"]!);

        string badAdmission = $"naca-0012\tNaca\tNACA 0012\t99\t\tnaca4-closed/2\t{validHash}\t0\t0\t1\n";
        Equal("admission-class", (string)Planted(badAdmission, _ => validDat).Data["check"]!);

        string badGen = $"naca-0012\tNaca\tNACA 0012\tGen\t\tunknown-gen/9\t{validHash}\t0\t0\t1\n";
        Equal("generator-id", (string)Planted(badGen, _ => validDat).Data["check"]!);
    }

    private static void PhysicalGeometryGoldenChecks()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        static double Interp((double X, double Y)[] pts, double x)
        {
            for (int i = 0; i < pts.Length - 1; i++)
                if (x >= pts[i].X && x <= pts[i + 1].X)
                {
                    double t = (x - pts[i].X) / Math.Max(1e-15, pts[i + 1].X - pts[i].X);
                    return pts[i].Y + t * (pts[i + 1].Y - pts[i].Y);
                }
            return pts[^1].Y;
        }
        static (double MaxT, double XMaxT, double MaxC, double SymErr) Measure(CatalogEntry entry)
        {
            var u = Stations(entry.Coordinates!, upper: true);
            var l = Stations(entry.Coordinates!, upper: false);
            double maxT = 0, xMaxT = 0, maxC = 0, symErr = 0;
            for (int i = 1; i <= 199; i++)
            {
                double x = i / 200.0;
                double yu = Interp(u, x), yl = Interp(l, x);
                double t = yu - yl, c = 0.5 * (yu + yl);
                if (t > maxT) { maxT = t; xMaxT = x; }
                if (Math.Abs(c) > Math.Abs(maxC)) maxC = c;
                symErr = Math.Max(symErr, Math.Abs(yu + yl));
            }
            return (maxT, xMaxT, maxC, symErr);
        }

        // NACA 16-012: symmetrical, 12% max thickness at x = 0.50
        var n16 = Measure(entries.Single(e => e.Id == "naca-16-012"));
        Equal(true, n16.SymErr < 1e-10);
        Equal(true, Math.Abs(n16.MaxT - 0.12) < 0.002 && Math.Abs(n16.XMaxT - 0.50) < 0.03);

        // NACA 23012: 12% thickness near x = 0.30, chord-frame max camber ~1.47% (unrotated 1.84% shifted by LE-TE frame)
        var n5 = Measure(entries.Single(e => e.Id == "naca-23012"));
        Equal(true, Math.Abs(n5.MaxT - 0.12) < 0.002 && Math.Abs(n5.XMaxT - 0.30) < 0.03);
        Equal(true, Math.Abs(n5.MaxC - 0.01465) < 0.001);

        // NACA 64A410: matches NASA TM 4741 published half-thickness & a=0.8(mod) camber envelope (~10% thick at x=0.39, ~2.58% camber)
        var n6a = Measure(entries.Single(e => e.Id == "naca-64a410"));
        Equal(true, Math.Abs(n6a.MaxT - 0.10) < 0.002 && Math.Abs(n6a.XMaxT - 0.39) < 0.03);
        Equal(true, Math.Abs(n6a.MaxC - 0.02582) < 0.001);

        // NACA 66(MOD)-412 (DTMB 1780): ~12% thickness near x = 0.46, positive a=0.8 camber ~2.62%
        var n66m = Measure(entries.Single(e => e.Id == "naca-66mod-412"));
        Equal(true, Math.Abs(n66m.MaxT - 0.12) < 0.003 && Math.Abs(n66m.XMaxT - 0.46) < 0.04);
        Equal(true, Math.Abs(n66m.MaxC - 0.02621) < 0.001);

        // CFDW Surf 118 (~11.8% cambered) & CFDW Mast 125 (symmetric 12.5%)
        var surf = Measure(entries.Single(e => e.Id == "cfdw-surf-118"));
        Equal(true, Math.Abs(surf.MaxT - 0.118) < 0.004 && surf.MaxC > 0.025);
        var mast = Measure(entries.Single(e => e.Id == "cfdw-mast-125"));
        Equal(true, mast.SymErr < 1e-10 && Math.Abs(mast.MaxT - 0.125) < 0.004);
    }

    private static void AdaptiveSqrtBasisBounded()
    {
        // Dense baseline clustered around u = 0.5 would have oscillated in an unbounded search.
        CatalogEntry entry = Catalog.Load().Single(e => e.Id == "naca-64a410");
        DatProfile profile = DatImport.Parse(entry.Coordinates!);
        double[] denseMid = [0.496, 0.499, 0.501, 0.504];
        var result = DatImport.OwnAdaptiveSqrtBasis(profile, 16, denseMid);
        Equal(true, result is not null);
        var (knots, controlX) = result!.Value;
        Equal(22, knots.Length);
        Equal(16, controlX.Length);
        for (int i = 1; i < knots.Length; i++)
            Equal(true, knots[i] >= knots[i - 1]);
        Equal(0.0, controlX[0]);
        Equal(1.0, controlX[^1]);
    }

    private static void AllActiveGenFitWithin10Microns()
    {
        byte[] foil = FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(
            Path.Combine(PlacementTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil"))));
        var active = Catalog.Load().Where(item => item.Class == AdmissionClass.Gen && item.DisabledReason is null).ToArray();
        Equal(77, active.Length);
        foreach (var entry in active)
        {
            var preview = SectionReplace.Preview(foil, 0, SectionScope.Shared,
                new ReplaceSource.Coordinates(entry.Designation, new Provenance("gen:" + entry.Id, false), entry.Coordinates!),
                ReplaceScope.Draft);
            if (preview.RefusalCode is not null)
                throw new InvalidOperationException($"Catalog entry {entry.Id} refused SectionReplace: {preview.RefusalCode} ({preview.RefusalReason})");
            double residualMeters = preview.FitResidual * preview.AcceptanceChord;
            if (residualMeters > 10e-6)
                throw new InvalidOperationException($"Catalog entry {entry.Id} residual {residualMeters * 1e6:F2} um > 10 um");
        }
    }

    private static void FairingsNeverListed()
    {
        IReadOnlyList<CatalogEntry> entries = Catalog.Load();
        Equal(86, entries.Count);
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
        // Probe table CF NACA 4412c, G4: -0.0003002; -0.174; 1. Design §3 rounds the same
        // measurement to 3.0e-4 chord, 0.17° and scale 1.0000.
        Equal("-0.0003002", entry.FrameLeShift.ToString("G4", CultureInfo.InvariantCulture));
        Equal("-0.174", entry.FrameRotationDegrees.ToString("G4", CultureInfo.InvariantCulture));
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

    // Ruling 156 controls (b)+(d), one fixture, one check. The fixture pins the bits of double.CosPi(i/80.0) (a runtime pin: a runtime
    // upgrade that moves a bit fails here before it moves a catalog byte) and holds the exact abscissa to 40 digits. The generator's
    // spacing must be 0.5*(1-CosPi) bit for bit, within 2^-52 of exact, exactly 0, 0.5 and 1 at the ends and the middle, and strictly rising.
    private static void SpacingGoldenAndAccuracy()
    {
        string path = Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "catalog", "spacing-cospi-80.tsv");
        string[] rows = File.ReadAllText(path).Split('\n').Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Equal(81, rows.Length);
        BigInteger scale = BigInteger.Pow(10, 40);
        BigInteger tolerance = scale >> 52;
        double previous = double.NegativeInfinity;
        for (int index = 0; index <= 80; index++)
        {
            string[] field = rows[index].Split('\t');
            Equal(index, int.Parse(field[0], CultureInfo.InvariantCulture));
            long goldenBits = long.Parse(field[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Equal(goldenBits, BitConverter.DoubleToInt64Bits(double.CosPi(index / 80.0)));
            double s = CatalogGenerator.Spacing(index);
            Equal(BitConverter.DoubleToInt64Bits(0.5 * (1 - BitConverter.Int64BitsToDouble(goldenBits))), BitConverter.DoubleToInt64Bits(s));
            BigInteger exact = BigInteger.Parse(field[2].Replace(".", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
            Equal(true, BigInteger.Abs(Scaled(s, scale) - exact) <= tolerance);
            Equal(true, s > previous);
            previous = s;
        }
        Equal(0d, CatalogGenerator.Spacing(0));
        Equal(0.5, CatalogGenerator.Spacing(40));
        Equal(1d, CatalogGenerator.Spacing(80));
    }

    // value * scale as an integer, from the double's exact binary expansion (truncation error 1 unit of 1e-40).
    private static BigInteger Scaled(double value, BigInteger scale)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        int exponent = (int)((bits >> 52) & 0x7FF);
        BigInteger mantissa = bits & 0xFFFFFFFFFFFFFL;
        if (exponent == 0) exponent++; else mantissa |= BigInteger.One << 52;
        exponent -= 1075;
        BigInteger scaled = mantissa * scale;
        return exponent >= 0 ? scaled << exponent : scaled >> -exponent;
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
