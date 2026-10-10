using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Section .dat export (docs/design/export.md 4.1, build conditions B5, B6, B9). Ring: fast, every push. Cost: each check is
/// a few ms to 0.3 s; about 1.7 s together, the slowest being B9f (0.9 s, a blended eta).
/// </summary>
internal static class DatExportTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    internal static void Run()
    {
        Check("DatExport_B5_Numbers_PositionalNeverExponent_SameBits", Positional);
        Check("DatExport_B6_NameLine_NeverStartsWithTwoNumbers_Reimports", NameLineRule);
        Check("DatExport_B6_NameLine_ControlCharactersAndLengthRemoved", NameLineClean);
        Check("DatExport_B9a_AtStation_EqualsCamberPlusMinusHalfThickness", AtStationIsSections);
        Check("DatExport_B9b_AtStation_EqualsInvertedPlacedSurface", AtStationIsInvertedPlacement);
        Check("DatExport_B9c_AtStation_PeakThicknessIsStationThickness", AtStationPeak);
        Check("DatExport_B9d_Own_PeakIsAuthoredMaximum_IgnoresThicknessChannel", OwnPeak);
        Check("DatExport_B9e_OwnAndAtStation_AgreeWhenThicknessIsTheAuthoredPeak", OwnEqualsAtStation);
        Check("DatExport_B9f_BlendedEta_PeakAndPlacement", BlendedEta);
        Check("DatExport_B9_TrailingEdgeRow_FromWrittenPoints_BothShapes", TrailingEdgeFromWrittenPoints);
        Check("DatExport_B9_XGrid_EqualsChordGrid_AndSurfaceAbscissae", XGrid);
        Check("DatExport_Selig_NoseOnce_SeligOrder_PointCount", SeligLayout);
        Check("DatExport_Lednicer_Layout_Reimports", LednicerLayout);
        Check("DatExport_ClosedAndOpenTrailingEdge_AsBuilt", TrailingEdgeAsBuilt);
        Check("DatExport_Deviation_MatchesProbeAndShrinksWithPoints", Deviation);
        Check("DatExport_ExampleFixture_RoundTripsThroughImportWithinDeviation", FixtureRoundTrip);
        Check("TeFloor_B10_NoPractitionerValueString_InSource", NoPractitionerValue);
    }

    // B10: the floor is "app default, no source" (Ruling 195). A scan of src/ for the retired wording; the one place that names
    // the floor's label is Settings.TrailingEdgeFloorLabel. Cost: about 7 ms (the .cs files of src/).
    private static void NoPractitionerValue()
    {
        var hits = Directory.EnumerateFiles(RepoFile("src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("practitioner value", StringComparison.OrdinalIgnoreCase)).ToArray();
        Equal(0, hits.Length);
    }

    private static string RepoFile(params string[] parts) => Path.Combine([PlacementTests.RepoRoot(), .. parts]);

    private static string ExampleText() => File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Assets", "example.foil"));

    private static byte[] Example() => Encoding.UTF8.GetBytes(ExampleText());

    // The thickness channel is the only place the example spells 0.12.
    private static byte[] WithThickness(string text, string value) => Encoding.UTF8.GetBytes(text.Replace(", 0.12)", ", " + value + ")", StringComparison.Ordinal));

    private static byte[] OpenTrailingEdge(double half) => Encoding.UTF8.GetBytes(ExampleText()
        .Replace("(0.9, 0.01), (1, 0)] ids", $"(0.9, 0.01), (1, {half.ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("(0.9, -0.01), (1, 0)] ids", $"(0.9, -0.01), (1, {(-half).ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal));

    // A second, thinner profile on the tip, so eta 0.5 is a blend of two profiles with different peaks.
    private static byte[] TwoProfiles()
    {
        const string second = """
                profile "section-b" {
                  upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.02), (0.15, 0.04), (0.35, 0.05), (0.55, 0.04), (0.75, 0.02), (0.9, 0.008), (1, 0)] }
                  lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.02), (0.15, -0.04), (0.35, -0.05), (0.55, -0.04), (0.75, -0.02), (0.9, -0.008), (1, 0)] }
                }

            """;
        return Encoding.UTF8.GetBytes(ExampleText()
            .Replace("  profiles {\n    profile \"section-a\" {", "  profiles {\n" + second + "    profile \"section-a\" {", StringComparison.Ordinal)
            .Replace("at tip profile \"section-a\"", "at tip profile \"section-b\"", StringComparison.Ordinal));
    }

    private static void Positional()
    {
        foreach (double value in new[] { 1e-5, 3.585447714271229e-5, 1e-300, 1.5e-7, 2.5e21, -4.2e-9, 0.1, 1, 0.9997532801828658, 123456789.125 })
        {
            string text = DatExport.Number(value);
            Equal(false, text.Contains('E') || text.Contains('e'));
            Equal(BitConverter.DoubleToUInt64Bits(value), BitConverter.DoubleToUInt64Bits(double.Parse(text, NumberStyles.Float, Invariant)));
        }
        Equal("0.00003585447714271229", DatExport.Number(3.585447714271229e-5).Substring(0, 22));
        Equal("0", DatExport.Number(0.0));
        Equal("0", DatExport.Number(-0.0));
        Equal("1", DatExport.Number(1.0));
        Equal("-0.5", DatExport.Number(-0.5));
        Refuses("DSL-RANGE", () => DatExport.Number(double.NaN));
        Refuses("DSL-RANGE", () => DatExport.Number(double.PositiveInfinity));
        // A written file has no exponent anywhere, whatever the section.
        string file = Encoding.UTF8.GetString(DatExport.Build(Example(), "Basic foil", 0, "Root", DatShape.AtStation, DatOrder.Selig, 201, 1).Bytes);
        Equal(false, file.Split('\n').Skip(1).Any(line => line.Contains('E') || line.Contains('e')));
    }

    private static bool StartsWithTwoNumbers(string line)
    {
        string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length >= 2 && double.TryParse(tokens[0], NumberStyles.Float, Invariant, out _) && double.TryParse(tokens[1], NumberStyles.Float, Invariant, out _);
    }

    private static void NameLineRule()
    {
        foreach (string name in new[] { "0.5 0.5 foil", "1 2", "3 4 5", "foil 1 2", "NACA 0012", "-1e3 2", "1,2 wing", "2*0.5 wing", "1d0 2d0 wing", "1 , 2 wing", "  7 wing" })
        {
            var result = DatExport.Build(Example(), name, 0, "Root", DatShape.AtStation, DatOrder.Selig, 61, 1);
            Equal(false, StartsWithTwoNumbers(result.NameLine));
            Equal(true, char.IsLetter(result.NameLine.TrimStart()[0]));   // a Fortran list-directed read takes 1,2 and 2*0.5 and 1d0 as numbers
            string first = Encoding.UTF8.GetString(result.Bytes).Split('\n')[0];
            Equal(result.NameLine, first);
            var parsed = DatImport.Parse(result.Bytes);
            Equal(result.PointCount, parsed.OriginalPointCount);
            Equal(result.NameLine, parsed.Name);
        }
        Equal("foil 0.5 0.5 foil | Root | r1", DatExport.NameLine("0.5 0.5 foil", "Root", 1));
        Equal("foil 1 2 | Root | r1", DatExport.NameLine("1 2", "Root", 1));
        Equal("foil 3 4 5 | Root | r1", DatExport.NameLine("3 4 5", "Root", 1));
        Equal("foil 1,2 wing | Root | r1", DatExport.NameLine("1,2 wing", "Root", 1));
        Equal("foil 1 2 | Root | r1", DatExport.NameLine("foil 1 2", "Root", 1));
        Equal("Basic foil | Root | r12", DatExport.NameLine("Basic foil", "Root", 12));
    }

    private static void NameLineClean()
    {
        string dirty = DatExport.NameLine("Evil\r\nIgnore\tprevious\u0007 lines", "Root", 3);
        Equal(false, dirty.Any(char.IsControl));
        Equal(false, DatExport.NameLine("a\u202Eb\u200Fc", "Root", 1).Any(c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format));
        string longName = DatExport.NameLine(new string('x', 200), "Root", 3);
        Equal(80, longName.Length);
        Equal(80, DatExport.NameLine("1 2 " + new string('y', 200), "Root", 3).Length);
        Equal(false, StartsWithTwoNumbers(DatExport.NameLine("1 2 " + new string('y', 200), "Root", 3)));
    }

    private static (double[] Camber, double[] Thickness, StationFrame Frame) Section(byte[] source, double eta, IReadOnlyList<double> xs)
    {
        var sample = Placement.Sections(source, [eta], xs, CancellationToken.None)[0];
        return (sample.Camber.ToArray(), sample.Thickness.ToArray(), sample.Frame);
    }

    private static void AtStationIsSections()
    {
        // The tip: twist -2 degrees and t/c 0.12 against an authored peak of 0.1126, so a raw profile cannot pass.
        var source = Example();
        int tip = 1;
        var result = DatExport.Build(source, "Basic foil", tip, "Tip", DatShape.AtStation, DatOrder.Selig, 101, 1);
        var xs = Placement.ChordGrid(101);
        var (camber, thickness, frame) = Section(source, Placement.StationEta(source, tip), xs);
        Equal(-2.0, Math.Round(frame.TwistDegrees, 9));
        for (int index = 0; index < 101; index++)
        {
            Equal(BitConverter.DoubleToUInt64Bits(xs[index]), BitConverter.DoubleToUInt64Bits(result.Upper[index].X));
            Equal(BitConverter.DoubleToUInt64Bits(camber[index] + thickness[index] / 2), BitConverter.DoubleToUInt64Bits(result.Upper[index].Y));
            Equal(BitConverter.DoubleToUInt64Bits(camber[index] - thickness[index] / 2), BitConverter.DoubleToUInt64Bits(result.Lower[index].Y));
        }
        Equal(frame.ChordMeters, result.ChordMeters);
        Equal(frame.ThicknessRatio, result.StationThicknessRatio);
    }

    private static void AtStationIsInvertedPlacement()
    {
        var source = Example();
        foreach (int station in new[] { 0, 1 })
            InvertedPlacementAgrees(source, DatExport.Build(source, "Basic foil", station, "S", DatShape.AtStation, DatOrder.Selig, 101, 1),
                Placement.StationEta(source, station));
    }

    private static void InvertedPlacementAgrees(byte[] source, DatExportResult result, double eta)
    {
        var view = Placement.Surface(source, "accepted", 0, CancellationToken.None, 5, result.PointsPerSurface);
        var placed = view.Sections.Single(section => section.Eta == eta);
        var frame = Placement.Frame(source, eta);
        var (sin, cos) = PlacementRule.SinCosDegrees(frame.TwistDegrees);
        double chord = frame.ChordMeters;
        double worst = 0;
        for (int index = 0; index < result.PointsPerSurface; index++)
        {
            foreach (var (point, back) in new[] { (placed.Upper[index], result.Upper[index]), (placed.Lower[index], result.Lower[index]) })
            {
                double u = (point.X - frame.LeadingMeters) / chord, w = (point.Z - frame.ElevationMeters) / chord;
                worst = Math.Max(worst, Math.Max(Math.Abs(u * cos - w * sin - back.X), Math.Abs(u * sin + w * cos - back.Y)));
            }
        }
        Equal(true, worst < 1e-12);
    }

    // Golden-section search for the largest value of a smooth, single-peaked function on [lo, hi]: one new evaluation per step.
    private static double Peak(Func<double, double> value, double lo, double hi)
    {
        double ratio = (Math.Sqrt(5) - 1) / 2;
        double a = hi - ratio * (hi - lo), b = lo + ratio * (hi - lo), fa = value(a), fb = value(b);
        for (int step = 0; step < 45; step++)
        {
            if (fa < fb)
            {
                lo = a; a = b; fa = fb;
                b = lo + ratio * (hi - lo); fb = value(b);
            }
            else
            {
                hi = b; b = a; fb = fa;
                a = hi - ratio * (hi - lo); fa = value(a);
            }
        }
        return Math.Max(fa, fb);
    }

    // The grid maximum (one batch call over 401 stations) brackets the peak; the search then reads the continuous curve.
    private static double ThicknessPeak(Func<double, double> thicknessAt, Func<IReadOnlyList<double>, double[]> batch)
    {
        var xs = Placement.ChordGrid(401);
        double[] grid = batch(xs);
        int at = Array.IndexOf(grid, grid.Max());
        return Peak(thicknessAt, xs[Math.Max(0, at - 1)], xs[Math.Min(xs.Count - 1, at + 1)]);
    }

    private static double StationThickness(byte[] source, double eta, double x)
    {
        var (_, thickness, _) = Section(source, eta, [x]);
        return thickness[0];
    }

    private static double StationPeak(byte[] source, double eta) =>
        ThicknessPeak(x => StationThickness(source, eta, x), xs => Section(source, eta, xs).Thickness);

    private static double OwnPeakValue(byte[] source, int assignment) =>
        ThicknessPeak(x => OwnThickness(source, assignment, x), xs =>
        {
            var own = Placement.OwnProfile(source, assignment, xs);
            return Enumerable.Range(0, xs.Count).Select(index => own.Upper[index] - own.Lower[index]).ToArray();
        });

    private static void AtStationPeak()
    {
        var source = Example();
        foreach (int station in new[] { 0, 1 })
        {
            double eta = Placement.StationEta(source, station);
            double peak = StationPeak(source, eta);
            double tc = Placement.Frame(source, eta).ThicknessRatio;
            Equal(true, Math.Abs(peak - tc) < 1e-12);
            // The written points: their thickest row is the station t/c to the grid's resolution, not the authored 0.1126.
            var written = DatExport.Build(source, "F", station, "S", DatShape.AtStation, DatOrder.Selig, 201, 1);
            double thickest = Enumerable.Range(0, 201).Max(index => written.Upper[index].Y - written.Lower[index].Y);
            Equal(true, Math.Abs(thickest - tc) < 1e-4 && thickest <= tc + 1e-15);
        }
    }

    private static double OwnThickness(byte[] source, int assignment, double x)
    {
        var own = Placement.OwnProfile(source, assignment, [x]);
        return own.Upper[0] - own.Lower[0];
    }

    private static void OwnPeak()
    {
        string text = ExampleText();
        var plain = Encoding.UTF8.GetBytes(text);
        var thin = WithThickness(text, "0.1");
        var parsed = FoilSource.Parse(plain).Definition!;
        double authored = Placement.ProfileDifferenceMaximum(parsed.Profiles[0].Upper, parsed.Profiles[0].Lower);
        double peak = OwnPeakValue(plain, 0);
        Equal(true, Math.Abs(peak - authored) < 1e-12);
        Equal(true, authored > 0.1126 && authored < 0.1127);   // the authored peak is not the station t/c (0.12)
        // The thickness channel does not touch Own; it does set the At-station peak.
        var own = DatExport.Build(plain, "F", 0, "Root", DatShape.Own, DatOrder.Selig, 101, 1);
        var ownThin = DatExport.Build(thin, "F", 0, "Root", DatShape.Own, DatOrder.Selig, 101, 1);
        for (int index = 0; index < 101; index++)
        {
            Equal(BitConverter.DoubleToUInt64Bits(own.Upper[index].Y), BitConverter.DoubleToUInt64Bits(ownThin.Upper[index].Y));
            Equal(BitConverter.DoubleToUInt64Bits(own.Lower[index].Y), BitConverter.DoubleToUInt64Bits(ownThin.Lower[index].Y));
        }
        var atThin = DatExport.Build(thin, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
        Equal(true, Math.Abs(StationPeak(thin, 0) - 0.1) < 1e-12);
        Equal(0.1, atThin.StationThicknessRatio);
        Equal(true, Math.Abs(Enumerable.Range(0, 101).Max(index => atThin.Upper[index].Y - atThin.Lower[index].Y) - 0.1) < 5e-4);
    }

    private static void OwnEqualsAtStation()
    {
        string text = ExampleText();
        var plain = Encoding.UTF8.GetBytes(text);
        var parsed = FoilSource.Parse(plain).Definition!;
        double authored = Placement.ProfileDifferenceMaximum(parsed.Profiles[0].Upper, parsed.Profiles[0].Lower);
        var source = WithThickness(text, authored.ToString("R", Invariant));
        var own = DatExport.Build(source, "F", 0, "Root", DatShape.Own, DatOrder.Selig, 101, 1);
        var at = DatExport.Build(source, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
        for (int index = 0; index < 101; index++)
        {
            Equal(true, Math.Abs(own.Upper[index].Y - at.Upper[index].Y) < 1e-12);
            Equal(true, Math.Abs(own.Lower[index].Y - at.Lower[index].Y) < 1e-12);
        }
        // And they differ when t/c is not the authored peak.
        var ownDefault = DatExport.Build(plain, "F", 0, "Root", DatShape.Own, DatOrder.Selig, 101, 1);
        var atDefault = DatExport.Build(plain, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
        Equal(true, Enumerable.Range(0, 101).Any(index => Math.Abs(ownDefault.Upper[index].Y - atDefault.Upper[index].Y) > 1e-4));
    }

    private static void BlendedEta()
    {
        var source = TwoProfiles();
        double eta = 0.5;
        var result = DatExport.BuildAt(source, eta, 0, "Basic foil", "Blend", DatShape.AtStation, DatOrder.Selig, 101, 1);
        double peak = StationPeak(source, eta);
        Equal(true, Math.Abs(peak - Placement.Frame(source, eta).ThicknessRatio) < 1e-12);
        InvertedPlacementAgrees(source, result, eta);
    }

    private static string[] Rows(DatExportResult result) =>
        Encoding.UTF8.GetString(result.Bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

    private static (double X, double Y) Row(string row)
    {
        string[] parts = row.Split(' ');
        return (double.Parse(parts[0], Invariant), double.Parse(parts[1], Invariant));
    }

    private static void TrailingEdgeFromWrittenPoints()
    {
        foreach (var source in new[] { Example(), OpenTrailingEdge(0.001) })
            foreach (var shape in new[] { DatShape.AtStation, DatShape.Own })
            {
                var result = DatExport.Build(source, "F", 1, "Tip", shape, DatOrder.Selig, 101, 1);
                var rows = Rows(result);
                double first = Row(rows[0]).Y, last = Row(rows[^1]).Y;
                Equal(BitConverter.DoubleToUInt64Bits((first - last) * result.ChordMeters * 1000), BitConverter.DoubleToUInt64Bits(result.TrailingEdgeMm));
            }
        Equal(0.0, DatExport.Build(Example(), "F", 1, "Tip", DatShape.AtStation, DatOrder.Selig, 101, 1).TrailingEdgeMm);
        var open = DatExport.Build(OpenTrailingEdge(0.001), "F", 1, "Tip", DatShape.AtStation, DatOrder.Selig, 101, 1);
        Equal(true, open.TrailingEdgeMm > 0.1 && open.TrailingEdgeMm < 0.3);
    }

    private static void XGrid()
    {
        var source = Example();
        foreach (int count in DatExport.PointChoices)
        {
            var result = DatExport.Build(source, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, count, 1);
            var grid = Placement.ChordGrid(count);
            var rows = Rows(result);
            Equal(2 * count - 1, rows.Length);
            for (int index = 0; index < count; index++)
            {
                // Selig: upper tail to nose is rows 0..count-1, so row k holds grid index count-1-k.
                Equal(BitConverter.DoubleToUInt64Bits(grid[count - 1 - index]), BitConverter.DoubleToUInt64Bits(Row(rows[index]).X));
                if (index > 0) Equal(BitConverter.DoubleToUInt64Bits(grid[index]), BitConverter.DoubleToUInt64Bits(Row(rows[count - 1 + index]).X));
            }
            // The wing surface samples the same abscissae: inverting its placement recovers each grid x.
            InvertedPlacementAgrees(source, result, 0);
        }
    }

    private static void SeligLayout()
    {
        var result = DatExport.Build(Example(), "Basic foil", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 12);
        var rows = Rows(result);
        Equal(201, result.PointCount);
        Equal(201, rows.Length);
        Equal(1.0, Row(rows[0]).X);
        Equal(1.0, Row(rows[^1]).X);
        Equal(1, rows.Count(row => Row(row).X == 0));   // the nose is written once
        Equal(0.0, Row(rows[100]).X);
        for (int index = 1; index <= 100; index++) Equal(true, Row(rows[index]).X < Row(rows[index - 1]).X);        // upper, tail to nose
        for (int index = 101; index < 201; index++) Equal(true, Row(rows[index]).X > Row(rows[index - 1]).X);       // lower, nose to tail
        Equal("Basic foil | Root | r12", Encoding.UTF8.GetString(result.Bytes).Split('\n')[0]);
        Equal(true, Encoding.UTF8.GetString(result.Bytes).EndsWith('\n') && !Encoding.UTF8.GetString(result.Bytes).Contains('\r'));
        Equal(false, result.Bytes.Length >= 3 && result.Bytes[0] == 0xEF);   // no BOM
        var parsed = DatImport.Parse(result.Bytes);
        Equal("selig", parsed.Format);
        Equal(101, parsed.Upper.Count);
        Equal(101, parsed.Lower.Count);
        Equal(201, parsed.OriginalPointCount);
    }

    private static void LednicerLayout()
    {
        var result = DatExport.Build(Example(), "Basic foil", 0, "Root", DatShape.AtStation, DatOrder.Lednicer, 61, 3);
        string[] lines = Encoding.UTF8.GetString(result.Bytes).Split('\n');
        Equal("Basic foil | Root | r3", lines[0]);
        Equal("61 61", lines[1]);
        Equal(0.0, Row(lines[2]).X);            // upper nose first
        Equal(1.0, Row(lines[62]).X);           // upper tail last
        Equal("", lines[63]);                   // one blank line
        Equal(0.0, Row(lines[64]).X);           // lower nose first
        Equal(1.0, Row(lines[124]).X);
        Equal(122, result.PointCount);
        var parsed = DatImport.Parse(result.Bytes);
        Equal("lednicer", parsed.Format);
        Equal(122, parsed.OriginalPointCount);
        Equal(61, parsed.Upper.Count);
        for (int index = 0; index < 61; index++)
        {
            Equal(true, Math.Abs(parsed.Upper[index].Y - result.Upper[index].Y) < 1e-12);
            Equal(true, Math.Abs(parsed.Lower[index].Y - result.Lower[index].Y) < 1e-12);
        }
    }

    private static void TrailingEdgeAsBuilt()
    {
        var closed = Rows(DatExport.Build(Example(), "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1));
        Equal(closed[0], closed[^1]);   // both written, equal
        var open = Rows(DatExport.Build(OpenTrailingEdge(0.001), "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1));
        Equal(true, open[0] != open[^1]);
        Equal(true, Row(open[0]).Y > Row(open[^1]).Y);
        Equal(1.0, Row(open[0]).X);
        Equal(1.0, Row(open[^1]).X);
    }

    private static void Deviation()
    {
        // Receipts: docs/proof/exd/probe2-open-root.txt, the Example foil with an open trailing edge, Root (design 5).
        var source = OpenTrailingEdge(0.001);
        double d61 = DatExport.Build(source, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 61, 1).DeviationMm;
        var r101 = DatExport.Build(source, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
        double d201 = DatExport.Build(source, "F", 0, "Root", DatShape.AtStation, DatOrder.Selig, 201, 1).DeviationMm;
        Equal(true, Math.Abs(d61 - 0.0204) < 0.00006);
        Equal(true, Math.Abs(r101.DeviationMm - 0.0074) < 0.00006);
        Equal(true, Math.Abs(d201 - 0.0019) < 0.00006);
        Equal(true, d61 > r101.DeviationMm && r101.DeviationMm > d201);
        Equal(true, Math.Abs(r101.TrailingEdgeMm - 0.2557) < 0.0002);
        Equal(true, Math.Abs(r101.ChordMeters - 0.12) < 1e-12);
    }

    internal static string FixturePath() => RepoFile("tests", "CfdWorkbench.Core.Tests", "Fixtures", "export", "basic-foil-root-r1.dat");

    private static void FixtureRoundTrip()
    {
        // The committed .dat is the Example foil, Root, At station, Selig, 101 per surface, revision 1.
        var source = Example();
        var result = DatExport.Build(source, "Basic foil", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
        byte[] fixture = File.ReadAllBytes(FixturePath());
        Equal(Convert.ToHexString(fixture), Convert.ToHexString(result.Bytes));
        // Re-import through the existing Import .dat path (parse, then the fit a Replace runs).
        var fit = DatImport.Fit(DatImport.Parse(fixture), "basic-foil-root");
        Equal(true, fit.Accepted);
        double deviationChord = result.DeviationMm / (result.ChordMeters * 1000);
        string foil = ExampleText();
        int block = foil.IndexOf("    profile \"section-a\" {", StringComparison.Ordinal);
        int blockEnd = foil.IndexOf("  sections {", StringComparison.Ordinal);
        var refit = Encoding.UTF8.GetBytes(foil[..block] + fit.ProfileBlock.Replace("basic-foil-root", "section-a") + "\n  }\n"
            + foil[blockEnd..]);
        Equal(true, FoilSource.Parse(refit).IsParsed);
        // The refitted profile against the section the file holds, on a dense grid, vertically in chord units.
        var dense = Enumerable.Range(0, 2001).Select(index => (1 - Math.Cos(Math.PI * index / 2000)) / 2).ToArray();
        var back = Placement.OwnProfile(refit, 0, dense);
        var (camber, thickness, _) = Section(source, 0, dense);
        double worst = 0;
        for (int index = 0; index < dense.Length; index++)
            worst = Math.Max(worst, Math.Max(Math.Abs(back.Upper[index] - (camber[index] + thickness[index] / 2)),
                Math.Abs(back.Lower[index] - (camber[index] - thickness[index] / 2))));
        Console.WriteLine($"MEASURE dat_roundtrip_max_dy_chord={worst.ToString("E3", Invariant)} deviation_chord={deviationChord.ToString("E3", Invariant)} fit_residual_chord={fit.MaxResidual.ToString("E3", Invariant)} vertices={fit.VertexCount}");
        Equal(true, worst <= deviationChord + fit.MaxResidual);
    }
}
