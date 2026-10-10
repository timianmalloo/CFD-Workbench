using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Wing STL export (docs/design/export.md 3.3, 4.2, 5; build conditions B2, B3, B4). Ring: fast, every push. Cost: about 3.8 s together (measured with CFD_CORE_COST=1); the slowest are the finest-rung half (1.2 s), the presets (1.0 s) and the four-wing byte check (0.9 s). The
/// finest rung is 321x801, about a million triangles.
/// </summary>
internal static class StlExportTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    internal static void Run()
    {
        Check("StlExport_B2_WrittenBytes_ReweldByBitPattern_EveryEdgeTwice_NoZeroArea_FourWings", BytesClosed);
        Check("StlExport_B2_TwoDoublesOneFloat_AndNegativeZero_StillClose", FloatRounding);
        Check("StlExport_B2_Check_RefusesBrokenBytes", CheckRefuses);
        Check("StlExport_Format_HeaderNotSolid_Length84Plus50n_NoNegativeZero_UnitOutwardNormals", Format);
        Check("StlExport_B3_Half_PairedEdges_Euler2_PositiveVolume_RootFacePlanarAtY0", HalfWing);
        Check("StlExport_B3_Half_FinestRung_Closed", HalfFinest);
        Check("StlExport_B3_WholeVolume_IsTwiceTheHalf", WholeIsTwiceHalf);
        Check("StlExport_B4_ReportedDeviation_NotBelowDenseMaximumMinusMargin_BothProbeWings", DenseDeviation);
        Check("StlExport_B4_ChordwiseOnlyReporter_WouldUnderReadTheUntitledWing", ChordwiseOnlyUnderReads);
        Check("StlExport_Presets_TriangleCountsAndDeviation_MatchProbeReceipts", Presets);
        Check("StlExport_ToleranceNotReached_SaysSo_AtTheFinestRung", ToleranceNotReached);
        Check("StlExport_TrailingEdge_LeastThicknessAndWhere", TrailingEdge);
        Check("StlExport_Check_RefusesASingleSectionWithNoSkin", Refuses);
        Check("StlExport_EdgeKey_HashSpreadsPackedNeighbours_HASH_FOLD_A", EdgeHash);
    }

    // HASH-FOLD-A: long.GetHashCode folds (a << 32 | b) to a ^ b, so neighbouring vertex pairs share buckets and a million-edge check
    // took minutes. The comparer must give (nearly) as many distinct hashes as there are distinct packed keys. Cost: about 5 ms.
    private static void EdgeHash()
    {
        var keys = new HashSet<long>();
        for (long a = 0; a < 2000; a++)
            for (long b = Math.Max(0, a - 3); b <= a + 3; b++) keys.Add((a << 32) | b);
        int folded = keys.Select(key => key.GetHashCode()).Distinct().Count();
        int mixed = keys.Select(key => EdgeKey.Instance.GetHashCode(key)).Distinct().Count();
        True(folded < keys.Count / 2, $"the default hash folds these keys ({folded} distinct of {keys.Count}); the test would prove nothing otherwise");
        True(mixed > keys.Count * 99 / 100, $"EdgeKey spreads them: {mixed} distinct of {keys.Count}");
    }

    /// <summary>
    /// The fixtures of tools/check-slicer-open.py (build condition B1): the Example foil with an open and a closed trailing edge, whole and
    /// starboard half, at Print, plus the Untitled wing at Fine (a large mesh for the load time), each written by the app's writer, with
    /// the numbers the app reports for it. Each wing is written twice, as <c>.stl</c> and as <c>.3mf</c> (same mesh). The harness entry point
    /// writes them with <c>--write-stl-fixtures &lt;dir&gt;</c>; not a check.
    /// </summary>
    internal static IEnumerable<(string File, StlExportResult Result)> Fixtures()
    {
        foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
        {
            foreach (var (name, source) in new[] { ("example-open", OpenTrailingEdge(0.001)), ("example-closed", Example()) })
            {
                string stem = $"{name}-{(scope == StlScope.Half ? "half" : "whole")}";
                yield return ($"{stem}.stl", StlExport.Build(source, name, 1, scope, StlExport.PrintMm));
                yield return ($"{stem}.3mf", ThreeMfExport.Build(source, name, 1, scope, StlExport.PrintMm));
            }
        }
        yield return ("untitled-fine-whole.stl", StlExport.Build(Untitled(), "untitled-fine", 1, StlScope.Whole, StlExport.FineMm));
        yield return ("untitled-fine-whole.3mf", ThreeMfExport.Build(Untitled(), "untitled-fine", 1, StlScope.Whole, StlExport.FineMm));
    }

    private static string RepoFile(params string[] parts) => Path.Combine([PlacementTests.RepoRoot(), .. parts]);

    private static string ExampleText() => File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Assets", "example.foil"));

    internal static byte[] Example() => Encoding.UTF8.GetBytes(ExampleText());

    private static byte[] Untitled() => FoilSource.NewDefault();

    private static byte[] OpenTrailingEdge(double half) => Encoding.UTF8.GetBytes(ExampleText()
        .Replace("(0.9, 0.01), (1, 0)] ids", $"(0.9, 0.01), (1, {half.ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("(0.9, -0.01), (1, 0)] ids", $"(0.9, -0.01), (1, {(-half).ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal));

    // The four probe wings: Example foil open 0.26 mm, open 0.36 mm, closed (as shipped), and the Untitled NACA 0012 wing.
    internal static IEnumerable<(string Name, byte[] Source)> Wings()
    {
        yield return ("open 0.26", OpenTrailingEdge(0.001));
        yield return ("open 0.36", OpenTrailingEdge(0.0014));
        yield return ("closed", Example());
        yield return ("untitled", Untitled());
    }

    private static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    private static void Near(double expected, double actual, double tolerance, string what)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException($"{what}: expected {expected}; actual {actual}");
    }

    // An independent re-weld of the bytes by float32 bit pattern. It does not call StlExport.Check, so the two can disagree.
    private sealed record Welded(int Triangles, int Vertices, int Edges, int OpenOrReused, int ZeroArea, double Volume, double[][] Corners, float[][] Normals);

    private static Welded Reweld(byte[] bytes)
    {
        int count = BitConverter.ToInt32(bytes, 80);
        True(bytes.Length == 84 + 50 * count, "length is 84 + 50 n");
        var ids = new Dictionary<(uint, uint, uint), int>();
        var uses = new Dictionary<(int, int), int>();
        var corners = new double[count][];
        var normals = new float[count][];
        int zero = 0;
        double volume = 0;
        for (int t = 0; t < count; t++)
        {
            int at = 84 + 50 * t;
            normals[t] = [BitConverter.ToSingle(bytes, at), BitConverter.ToSingle(bytes, at + 4), BitConverter.ToSingle(bytes, at + 8)];
            var p = new double[9];
            var id = new int[3];
            for (int c = 0; c < 3; c++)
            {
                uint x = BitConverter.ToUInt32(bytes, at + 12 + 12 * c), y = BitConverter.ToUInt32(bytes, at + 16 + 12 * c), z = BitConverter.ToUInt32(bytes, at + 20 + 12 * c);
                if (!ids.TryGetValue((x, y, z), out id[c])) { id[c] = ids.Count; ids[(x, y, z)] = id[c]; }
                p[3 * c] = BitConverter.UInt32BitsToSingle(x); p[3 * c + 1] = BitConverter.UInt32BitsToSingle(y); p[3 * c + 2] = BitConverter.UInt32BitsToSingle(z);
            }
            corners[t] = p;
            double ax = p[3] - p[0], ay = p[4] - p[1], az = p[5] - p[2], bx = p[6] - p[0], by = p[7] - p[1], bz = p[8] - p[2];
            if (id[0] == id[1] || id[1] == id[2] || id[0] == id[2] || ay * bz - az * by == 0 && az * bx - ax * bz == 0 && ax * by - ay * bx == 0) zero++;
            volume += (p[0] * (p[4] * p[8] - p[5] * p[7]) - p[1] * (p[3] * p[8] - p[5] * p[6]) + p[2] * (p[3] * p[7] - p[4] * p[6])) / 6;
            for (int c = 0; c < 3; c++) uses[(id[c], id[(c + 1) % 3])] = uses.GetValueOrDefault((id[c], id[(c + 1) % 3])) + 1;
        }
        // Every edge used exactly twice: the directed edge once, its reverse once.
        int bad = uses.Count(pair => pair.Value != 1 || !uses.TryGetValue((pair.Key.Item2, pair.Key.Item1), out int back) || back != 1);
        return new(count, ids.Count, uses.Count / 2, bad, zero, volume, corners, normals);
    }

    private static void Closed(Welded w, string what)
    {
        True(w.OpenOrReused == 0, $"{what}: {w.OpenOrReused} edges are not used exactly twice");
        True(w.ZeroArea == 0, $"{what}: {w.ZeroArea} zero-area triangles");
        True(w.Volume > 0, $"{what}: signed volume {w.Volume} is not positive");
    }

    private static void BytesClosed()
    {
        foreach (var (name, source) in Wings())
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                var result = StlExport.Build(source, name, 1, scope, StlExport.PrintMm);
                var w = Reweld(result.Bytes);
                Closed(w, $"{name} {scope}");
                True(w.Triangles == result.Triangles && w.Vertices == result.Vertices, $"{name} {scope}: the result counts are the bytes' counts");
                True(w.Vertices - w.Edges + w.Triangles == 2, $"{name} {scope}: Euler 2");
            }
    }

    // A hand-built wing whose trailing edge is open by 1e-15 m (1e-12 mm): two distinct doubles that round to one float, and a root
    // on y = 0 whose mirror is -0.0. A writer that welds or checks doubles sees a sliver strip; the bytes carry none.
    private static SurfaceView Sliver()
    {
        static PlacedSection Section(double eta, double y, double chord) => new(eta, null,
            [new(0, y, 0), new(chord / 2, y, 0.01), new(chord, y, 0)],
            [new(0, y, 0), new(chord / 2, y, -0.01), new(chord + 1e-15, y, 0)]);
        return new("hand", "accepted", 0, 0, 0, -0.01, 0.1, 0.2, 0.01, [Section(0, 0, 0.1), Section(1, 0.2, 0.1)]);
    }

    private static void FloatRounding()
    {
        var surface = Sliver();
        var doubleKeys = surface.Sections.SelectMany(s => s.Upper.Concat(s.Lower)).Select(p => (p.X, p.Y, p.Z)).Distinct().Count();
        var mesh = StlExport.Close(surface, StlScope.Whole);
        True(mesh.Vertices.Length / 3 < doubleKeys * 2, "the float weld merges what the doubles keep apart");
        var bytes = StlExport.Write(mesh, "hand");
        var w = Reweld(bytes);
        Closed(w, "hand-built whole wing");
        True(w.Vertices - w.Edges + w.Triangles == 2, "Euler 2");
        for (int offset = 84; offset < bytes.Length; offset += 4)
            if ((offset - 84) % 50 < 48) True(BitConverter.ToUInt32(bytes, offset) != 0x80000000u, "no -0.0 word at " + offset);
        var half = Reweld(StlExport.Write(StlExport.Close(surface, StlScope.Half), "hand"));
        Closed(half, "hand-built half wing");
    }

    private static void CheckRefuses()
    {
        var good = StlExport.Build(Example(), "closed", 1, StlScope.Whole, StlExport.DraftMm).Bytes;
        True(StlExport.Check(good).Closed, "the written bytes pass");
        var removed = new byte[good.Length - 50];
        Array.Copy(good, removed, 84 + 50 * 100);
        Array.Copy(good, 84 + 50 * 101, removed, 84 + 50 * 100, good.Length - 84 - 50 * 101);
        BitConverter.TryWriteBytes(removed.AsSpan(80, 4), BitConverter.ToInt32(good, 80) - 1);
        var oneGone = StlExport.Check(removed);
        True(!oneGone.Closed && oneGone.UnpairedEdges > 0, "one triangle removed leaves edges unpaired");
        True(!StlExport.Check(good[..^7]).Closed, "a truncated file is not closed");
        var flipped = (byte[])good.Clone();
        for (int c = 0; c < 4; c++) (flipped[84 + 12 + c], flipped[84 + 24 + c]) = (flipped[84 + 24 + c], flipped[84 + 12 + c]);
        for (int c = 4; c < 12; c++) (flipped[84 + 12 + c], flipped[84 + 24 + c]) = (flipped[84 + 24 + c], flipped[84 + 12 + c]);
        True(!StlExport.Check(flipped).Closed, "a flipped triangle is not closed");
    }

    private static void Format()
    {
        var result = StlExport.Build(OpenTrailingEdge(0.001), "Solid Foil", 12, StlScope.Whole, StlExport.PrintMm);
        var bytes = result.Bytes;
        True(!Encoding.ASCII.GetString(bytes, 0, 5).Equals("solid", StringComparison.OrdinalIgnoreCase), "header does not begin with solid");
        Equal("CFD Workbench solid-foil r12 mm", Encoding.ASCII.GetString(bytes, 0, 80).TrimEnd('\0'));
        Equal(84 + 50 * result.Triangles, bytes.Length);
        var w = Reweld(bytes);
        for (int offset = 84; offset < bytes.Length; offset += 50)
            for (int word = 0; word < 12; word++)
                True(BitConverter.ToUInt32(bytes, offset + 4 * word) != 0x80000000u, "no -0.0 at triangle " + (offset - 84) / 50);
        for (int t = 0; t < w.Triangles; t += 7)
        {
            var n = w.Normals[t];
            Near(1, Math.Sqrt((double)n[0] * n[0] + (double)n[1] * n[1] + (double)n[2] * n[2]), 1e-6, "unit normal");
            var p = w.Corners[t];
            double ax = p[3] - p[0], ay = p[4] - p[1], az = p[5] - p[2], bx = p[6] - p[0], by = p[7] - p[1], bz = p[8] - p[2];
            double cx = ay * bz - az * by, cy = az * bx - ax * bz, cz = ax * by - ay * bx, length = Math.Sqrt(cx * cx + cy * cy + cz * cz);
            True(cx / length * n[0] + cy / length * n[1] + cz / length * n[2] > 0.999999, "the facet normal follows the winding");
        }
        // Millimetres, unscaled: the Example foil is 120 mm of chord and 450 mm of half span; the whole wing is 900 mm across.
        Near(120.0, result.SizeXMm, 0.01, "chord in mm");
        Near(900.0, result.SizeYMm, 0.01, "span in mm");
        Near(result.VolumeMm3, w.Volume, Math.Abs(w.Volume) * 1e-9, "result volume is the bytes' volume");
    }

    private static void HalfWing()
    {
        foreach (var (name, source) in Wings())
        {
            var surface = Placement.Surface(source, "accepted", 0, CancellationToken.None, 41, 101);
            AssertHalf(StlExport.Write(StlExport.Close(surface, StlScope.Half), "half"), name);
        }
    }

    private static void HalfFinest()
    {
        foreach (var (name, source) in Wings().Where(w => w.Name is "open 0.26" or "untitled"))
        {
            var surface = Placement.Surface(source, "accepted", 0, CancellationToken.None, 321, 801);
            AssertHalf(StlExport.Write(StlExport.Close(surface, StlScope.Half), "half"), name + " finest");
        }
    }

    private static void AssertHalf(byte[] bytes, string what)
    {
        var w = Reweld(bytes);
        Closed(w, what + " half");
        True(w.Vertices - w.Edges + w.Triangles == 2, what + ": Euler 2");
        // The root face: every triangle with all three corners at y = 0 lies in that plane and faces -y; no vertex is on the other side.
        int rootFaces = 0;
        double minY = double.PositiveInfinity;
        for (int t = 0; t < w.Triangles; t++)
        {
            var p = w.Corners[t];
            minY = Math.Min(minY, Math.Min(p[1], Math.Min(p[4], p[7])));
            if (p[1] == 0 && p[4] == 0 && p[7] == 0)
            {
                rootFaces++;
                True(w.Normals[t][1] == -1f && w.Normals[t][0] == 0 && w.Normals[t][2] == 0, what + ": root face normal is -y");
            }
        }
        True(minY == 0, what + ": the root is the plane y = 0 and nothing lies beyond it");
        True(rootFaces > 0, what + ": a root cap exists");
        // The root outline is the cap's boundary: every y = 0 vertex belongs to a root face.
        var onPlane = new HashSet<(double, double, double)>();
        var inFace = new HashSet<(double, double, double)>();
        for (int t = 0; t < w.Triangles; t++)
        {
            var p = w.Corners[t];
            bool face = p[1] == 0 && p[4] == 0 && p[7] == 0;
            for (int c = 0; c < 3; c++)
            {
                if (p[3 * c + 1] != 0) continue;
                onPlane.Add((p[3 * c], 0, p[3 * c + 2]));
                if (face) inFace.Add((p[3 * c], 0, p[3 * c + 2]));
            }
        }
        True(onPlane.SetEquals(inFace), what + ": every vertex at y = 0 is on the root face");
    }

    private static void WholeIsTwiceHalf()
    {
        foreach (var (name, source) in Wings().Where(w => w.Name is "open 0.26" or "untitled"))
        {
            var whole = StlExport.Build(source, name, 1, StlScope.Whole, StlExport.DraftMm);
            var half = StlExport.Build(source, name, 1, StlScope.Half, StlExport.DraftMm);
            Near(2 * half.VolumeMm3, whole.VolumeMm3, 1.0, name + ": whole volume (mm3) is twice the half to within 0.001 cm3");
            True(whole.Stations == half.Stations, name + ": the same rung");
        }
    }

    // B4. The surface the mesh approximates is sampled densely: the grid refined eight times in both directions nests the coarse grid
    // exactly (the same rational etas and cosine angles), so every fine vertex belongs to one coarse cell. Seeded, 100,000 samples.
    private static (double DenseMaxMetres, double ReportedMm, double ChordOnlyMm) DenseAndReported(byte[] source, double toleranceMm)
    {
        var result = StlExport.Build(source, "dense", 1, StlScope.Whole, toleranceMm);
        var coarse = Placement.Surface(source, "accepted", 0, CancellationToken.None, result.Stations, result.ChordPoints);
        var fine = Placement.Surface(source, "accepted", 0, CancellationToken.None, 2 * result.Stations - 1, 2 * result.ChordPoints - 1);
        const int Refine = 8;
        var reference = Placement.Surface(source, "accepted", 0, CancellationToken.None, Refine * (result.Stations - 1) + 1, Refine * (result.ChordPoints - 1) + 1);
        True(reference.Sections.Count == Refine * (coarse.Sections.Count - 1) + 1, "the refined grid nests the coarse grid");
        var random = new Random(20261010);
        int cells = coarse.Sections.Count - 1, across = result.ChordPoints - 1;
        double dense = 0;
        for (int sample = 0; sample < 100_000; sample++)
        {
            int i = random.Next(cells), j = random.Next(across), side = random.Next(2);
            int a = random.Next(Refine + 1), b = random.Next(Refine + 1);
            var section = reference.Sections[Refine * i + a];
            var point = (side == 0 ? section.Upper : section.Lower)[Refine * j + b];
            var s0 = coarse.Sections[i]; var s1 = coarse.Sections[i + 1];
            var corners = side == 0
                ? (s0.Upper[j], s0.Upper[j + 1], s1.Upper[j + 1], s1.Upper[j])
                : (s0.Lower[j], s0.Lower[j + 1], s1.Lower[j + 1], s1.Lower[j]);
            dense = Math.Max(dense, Math.Min(Distance(point, corners.Item1, corners.Item2, corners.Item3), Distance(point, corners.Item1, corners.Item3, corners.Item4)));
        }
        return (dense, result.DeviationMm, StlExport.Deviation(coarse, fine).Chord * 1000);
    }

    private const double MarginMm = 0.0005;

    private static void DenseDeviation()
    {
        foreach (var (name, source) in new[] { ("example", OpenTrailingEdge(0.001)), ("untitled", Untitled()) })
        {
            var (dense, reported, _) = DenseAndReported(source, StlExport.PrintMm);
            True(reported >= dense * 1000 - MarginMm, $"{name}: reported {reported:F5} mm is below the dense maximum {dense * 1000:F5} mm minus {MarginMm} mm");
        }
    }

    private static void ChordwiseOnlyUnderReads()
    {
        var (dense, reported, chordOnly) = DenseAndReported(Untitled(), StlExport.PrintMm);
        True(chordOnly < dense * 1000 - MarginMm, $"a chordwise-only reporter ({chordOnly:F5} mm) must fall short of the dense maximum ({dense * 1000:F5} mm)");
        True(reported > chordOnly, "the three-kind reporter reads more than the chordwise one");
    }

    // Point to triangle distance (Ericson, Real-Time Collision Detection 5.1.5), in the units of the points.
    private static double Distance(Point3 p, Point3 a, Point3 b, Point3 c)
    {
        static (double, double, double) Sub(Point3 u, Point3 v) => (u.X - v.X, u.Y - v.Y, u.Z - v.Z);
        static double Dot((double X, double Y, double Z) u, (double X, double Y, double Z) v) => u.X * v.X + u.Y * v.Y + u.Z * v.Z;
        var ab = Sub(b, a); var ac = Sub(c, a); var ap = Sub(p, a);
        double d1 = Dot(ab, ap), d2 = Dot(ac, ap);
        (double, double, double) at;
        if (d1 <= 0 && d2 <= 0) at = (a.X, a.Y, a.Z);
        else
        {
            var bp = Sub(p, b); double d3 = Dot(ab, bp), d4 = Dot(ac, bp);
            var cp = Sub(p, c); double d5 = Dot(ab, cp), d6 = Dot(ac, cp);
            double vc = d1 * d4 - d3 * d2, vb = d5 * d2 - d1 * d6, va = d3 * d6 - d5 * d4;
            if (d3 >= 0 && d4 <= d3) at = (b.X, b.Y, b.Z);
            else if (vc <= 0 && d1 >= 0 && d3 <= 0) { double v = d1 / (d1 - d3); at = (a.X + v * ab.Item1, a.Y + v * ab.Item2, a.Z + v * ab.Item3); }
            else if (d6 >= 0 && d5 <= d6) at = (c.X, c.Y, c.Z);
            else if (vb <= 0 && d2 >= 0 && d6 <= 0) { double w = d2 / (d2 - d6); at = (a.X + w * ac.Item1, a.Y + w * ac.Item2, a.Z + w * ac.Item3); }
            else if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) { double w = (d4 - d3) / (d4 - d3 + d5 - d6); at = (b.X + w * (c.X - b.X), b.Y + w * (c.Y - b.Y), b.Z + w * (c.Z - b.Z)); }
            else
            {
                double denominator = 1 / (va + vb + vc), v = vb * denominator, w = vc * denominator;
                at = (a.X + ab.Item1 * v + ac.Item1 * w, a.Y + ab.Item2 * v + ac.Item2 * w, a.Z + ab.Item3 * v + ac.Item3 * w);
            }
        }
        double dx = p.X - at.Item1, dy = p.Y - at.Item2, dz = p.Z - at.Item3;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // The design's preset table (section 4.2) and the probe receipts: docs/proof/exd/probe-open.txt and probe-default-wing.txt.
    private static void Presets()
    {
        var cases = new (string Wing, StlScope Scope, double Tolerance, int Triangles, double DeviationMm)[]
        {
            ("example", StlScope.Whole, StlExport.DraftMm, 8_278, 0.0325), ("example", StlScope.Whole, StlExport.PrintMm, 18_418, 0.0145),
            ("example", StlScope.Whole, StlExport.FineMm, 129_118, 0.0020),
            ("example", StlScope.Half, StlExport.DraftMm, 4_238, 0.0325), ("example", StlScope.Half, StlExport.PrintMm, 9_358, 0.0145),
            ("example", StlScope.Half, StlExport.FineMm, 64_958, 0.0020),
            ("untitled", StlScope.Whole, StlExport.DraftMm, 32_396, 0.0450), ("untitled", StlScope.Whole, StlExport.PrintMm, 128_796, 0.0113),
            ("untitled", StlScope.Whole, StlExport.FineMm, 513_596, 0.0028),
            ("untitled", StlScope.Half, StlExport.DraftMm, 16_396, 0.0450), ("untitled", StlScope.Half, StlExport.PrintMm, 64_796, 0.0113),
            ("untitled", StlScope.Half, StlExport.FineMm, 257_596, 0.0028),
        };
        foreach (var (wing, scope, tolerance, triangles, deviation) in cases)
        {
            var result = StlExport.Build(wing == "example" ? OpenTrailingEdge(0.001) : Untitled(), wing, 1, scope, tolerance);
            Equal(triangles, result.Triangles);
            Near(deviation, result.DeviationMm, 0.00005, $"{wing} {scope} {tolerance}: deviation");
            True(result.ToleranceMet && result.DeviationMm <= tolerance, $"{wing} {scope} {tolerance}: tolerance met");
            Equal(84 + 50 * triangles, result.Bytes.Length);
        }
        // The large-mesh line (B7): only the Untitled wing at Fine, whole, is over it.
        True(StlExport.Build(Untitled(), "u", 1, StlScope.Whole, StlExport.FineMm).Triangles > StlExport.LargeMeshTriangles, "Untitled at Fine, whole, is over the line");
        True(StlExport.Build(Untitled(), "u", 1, StlScope.Half, StlExport.FineMm).Triangles <= StlExport.LargeMeshTriangles, "Untitled at Fine, half, is under it");
    }

    private static void ToleranceNotReached()
    {
        var result = StlExport.Build(Untitled(), "u", 1, StlScope.Whole, 0.0001, [(11, 26), (21, 51)]);
        True(!result.ToleranceMet, "the tolerance was not reached");
        Equal(21, result.Stations);
        True(result.DeviationMm > 0.0001, "the reported deviation is what the finest rung reached");
    }

    private static void TrailingEdge()
    {
        var open = StlExport.Build(OpenTrailingEdge(0.001), "o", 1, StlScope.Whole, StlExport.DraftMm);
        Near(0.2557, open.TrailingEdgeMm, 0.00005, "least thickness, open 0.26 mm");
        // The open Example's gap is 0.25572873962800 mm at every station to 1e-15: the "where" of a minimum is rounding noise, so it is the whole span.
        True(open.TrailingEdgeAlongSpan, "an open trailing edge of constant gap holds along the whole span");
        // A located value: a hand-built wing whose gap is least at the middle section (y = 0.1 m) and is not at the tip.
        static PlacedSection Section(double y, double gap) => new(y / 0.2, null,
            [new(0, y, 0), new(0.05, y, 0.01), new(0.1, y, gap / 2000)], [new(0, y, 0), new(0.05, y, -0.01), new(0.1, y, -gap / 2000)]);
        var tapered = new SurfaceView("hand", "accepted", 0, 0, 0, 0, 0, 0, 0, [Section(0, 0.4), Section(0.1, 0.1), Section(0.2, 0.3)]);
        var (least, where, greatest) = StlExport.TrailingEdge(tapered);
        Near(0.1, least, 1e-9, "least gap of the hand-built wing");
        Near(100, where, 1e-9, "at y = 100 mm");
        Near(0.4, greatest, 1e-9, "greatest gap");
        var closed = StlExport.Build(Example(), "c", 1, StlScope.Whole, StlExport.DraftMm);
        Equal(0.0, closed.TrailingEdgeMm);
        True(closed.TrailingEdgeAlongSpan, "closed: 0.00 mm along the whole span");
        var untitled = StlExport.Build(Untitled(), "u", 1, StlScope.Half, StlExport.DraftMm);
        Equal(0.0, untitled.TrailingEdgeMm);
        True(untitled.TrailingEdgeAlongSpan, "Untitled: 0.00 mm along the whole span");
    }

    // A surface that cannot close (a tip section that is not the same outline as its neighbour's cell) must stop the write.
    private static void Refuses()
    {
        var broken = new SurfaceView("hand", "accepted", 0, 0, 0, 0, 0, 0, 0,
            [new(0, null, [new(0, 0, 0), new(0.05, 0, 0.01), new(0.1, 0, 0)], [new(0, 0, 0), new(0.05, 0, -0.01), new(0.1, 0, 0)])]);
        var mesh = StlExport.Close(broken, StlScope.Half);
        True(!StlExport.Check(StlExport.Write(mesh, "broken")).Closed, "a single section has no skin, so the check refuses it");
    }
}
