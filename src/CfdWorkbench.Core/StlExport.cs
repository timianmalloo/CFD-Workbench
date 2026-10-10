namespace CfdWorkbench.Core;

/// <summary>Which part of the wing an STL holds: both halves welded at the root, or the starboard half closed with a root cap at y = 0.</summary>
public enum StlScope { Whole, Half }

/// <summary>
/// One wing STL: its bytes and everything the dialog states about it (Export design 4.2, 5). <paramref name="DeviationMm"/> is the largest
/// distance, sampled at cell midpoints, between the written mesh and the app's own surface at twice the resolution; it is neither a bound
/// nor a certificate. The trailing-edge fields describe the sections of the written mesh. Sizes and the volume are read back from the bytes.
/// </summary>
public sealed record StlExportResult(byte[] Bytes, StlScope Scope, int Stations, int ChordPoints, int Triangles, int Vertices,
    double ToleranceMm, double DeviationMm, bool ToleranceMet, double TrailingEdgeMm, double TrailingEdgeYMm, bool TrailingEdgeAtTip,
    bool TrailingEdgeAlongSpan, double SizeXMm, double SizeYMm, double SizeZMm, double VolumeMm3);

/// <summary>What the edge check finds in the bytes of an STL as written (build condition B2).</summary>
public sealed record StlCheck(bool Closed, int Triangles, int Vertices, int Euler, int ZeroAreaTriangles, int UnpairedEdges,
    double VolumeMm3, double MinXMm, double MinYMm, double MinZMm, double MaxXMm, double MaxYMm, double MaxZMm);

/// <summary>The weld of the surface in binary32 millimetres: <paramref name="Vertices"/> holds x, y, z per vertex; no coordinate is -0.0.</summary>
internal sealed record StlMesh(float[] Vertices, int[] Triangles);

/// <summary>The three kinds of midpoint the deviation samples, in metres (design 4.2; B4 compares the largest with a dense sample).</summary>
internal readonly record struct StlDeviation(double Chord, double Span, double Diagonal)
{
    internal double Largest => Math.Max(Chord, Math.Max(Span, Diagonal));
}

/// <summary>
/// The wing STL writer (Export design 3.3, 4.2; build conditions B2, B3, B4). The skin is the display evaluator's surface at the first rung of a
/// fixed ladder whose measured deviation is within the tolerance; a topology-only closing step then welds, strips an open trailing edge, caps the
/// tips and, for a half, the root. Every write is checked on its own bytes and refused when an edge is not shared by exactly two triangles.
/// </summary>
public static class StlExport
{
    public const double DraftMm = 0.05;
    public const double PrintMm = 0.02;
    public const double FineMm = 0.005;

    /// <summary>The large-mesh line (design H5, B7): above this many triangles the dialog warns.</summary>
    public const int LargeMeshTriangles = 500_000;

    /// <summary>Stations by chord points, coarse to fine. simplify: uniform grids; upgrade to adaptive refinement near the tip when a real wing needs more than the Print rung's 500,000 triangles.</summary>
    public static IReadOnlyList<(int Stations, int ChordPoints)> Ladder { get; } =
        [(11, 26), (21, 51), (31, 76), (41, 101), (81, 201), (161, 401), (321, 801)];

    /// <summary>The wing STL at the first ladder rung whose measured deviation is at or under <paramref name="toleranceMm"/>, or the finest rung.</summary>
    public static StlExportResult Build(byte[] source, string foilName, int revision, StlScope scope, double toleranceMm,
        CancellationToken cancellation = default) =>
        Build(source, foilName, revision, scope, toleranceMm, Ladder, cancellation);

    internal static StlExportResult Build(byte[] source, string foilName, int revision, StlScope scope, double toleranceMm,
        IReadOnlyList<(int Stations, int ChordPoints)> ladder, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        Guard.Require(double.IsFinite(toleranceMm) && toleranceMm > 0 && ladder.Count > 0, "DSL-RANGE");
        SurfaceView surface = null!;
        double deviationMm = 0;
        var (stations, chordPoints) = ladder[0];
        for (int rung = 0; rung < ladder.Count; rung++)
        {
            (stations, chordPoints) = ladder[rung];
            surface = Placement.Surface(source, "accepted", 0, cancellation, stations, chordPoints);
            var fine = Placement.Surface(source, "accepted", 0, cancellation, 2 * stations - 1, 2 * chordPoints - 1);
            deviationMm = Deviation(surface, fine).Largest * 1000;
            if (deviationMm <= toleranceMm) break;
        }
        var mesh = Close(surface, scope);
        byte[] bytes = Write(mesh, Header(foilName, revision));
        var check = Check(bytes);
        Guard.Require(check.Closed, "EXPORT-NOT-CLOSED");
        var (leastMm, leastYMm, greatestMm) = TrailingEdge(surface);
        double tipYMm = surface.Sections[^1].Upper[0].Y * 1000;
        return new(bytes, scope, stations, chordPoints, check.Triangles, check.Vertices, toleranceMm, deviationMm, deviationMm <= toleranceMm,
            leastMm, leastYMm, leastYMm == tipYMm, greatestMm - leastMm <= 1e-6,
            check.MaxXMm - check.MinXMm, check.MaxYMm - check.MinYMm, check.MaxZMm - check.MinZMm, check.VolumeMm3);
    }

    /// <summary><c>CFD Workbench &lt;foil slug&gt; r&lt;n&gt; mm</c>, ASCII, never beginning with <c>solid</c> (many readers take that for ASCII STL).</summary>
    internal static string Header(string foilName, int revision) =>
        $"CFD Workbench {DatImport.Slug(foilName)} r{revision.ToString(System.Globalization.CultureInfo.InvariantCulture)} mm";

    // Least trailing-edge thickness over the sections of the mesh (the 3D distance of the last upper and lower points), where, and the greatest.
    internal static (double LeastMm, double LeastYMm, double GreatestMm) TrailingEdge(SurfaceView surface)
    {
        double least = double.PositiveInfinity, leastY = 0, greatest = 0;
        foreach (var section in surface.Sections)
        {
            double thickness = Distance(section.Upper[^1], section.Lower[^1]) * 1000;
            if (thickness < least) { least = thickness; leastY = section.Upper[^1].Y * 1000; }
            greatest = Math.Max(greatest, thickness);
        }
        return (least, leastY, greatest);
    }

    private static double Distance(Point3 a, Point3 b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));

    private static Point3 Middle(Point3 a, Point3 b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);

    /// <summary>
    /// Largest distance, over the cells of <paramref name="coarse"/>, from the midpoint of a cell edge or diagonal to the matching point of
    /// <paramref name="fine"/> (twice the resolution). A cell whose stations are not two fine steps apart (an authored station between grid
    /// lines) is skipped.
    /// </summary>
    internal static StlDeviation Deviation(SurfaceView coarse, SurfaceView fine)
    {
        var fineIndex = new Dictionary<double, int>();
        for (int index = 0; index < fine.Sections.Count; index++) fineIndex[fine.Sections[index].Eta] = index;
        double chord = 0, span = 0, diagonal = 0;
        var sections = coarse.Sections;
        int points = sections[0].Upper.Count;
        for (int i = 0; i < sections.Count - 1; i++)
        {
            if (!fineIndex.TryGetValue(sections[i].Eta, out int fi) || !fineIndex.TryGetValue(sections[i + 1].Eta, out int fj) || fj != fi + 2) continue;
            var middle = fine.Sections[fi + 1];
            for (int side = 0; side < 2; side++)
            {
                var a = side == 0 ? sections[i].Upper : sections[i].Lower;
                var b = side == 0 ? sections[i + 1].Upper : sections[i + 1].Lower;
                var fineA = side == 0 ? fine.Sections[fi].Upper : fine.Sections[fi].Lower;
                var fineMiddle = side == 0 ? middle.Upper : middle.Lower;
                for (int j = 0; j < points - 1; j++)
                {
                    chord = Math.Max(chord, Distance(fineA[2 * j + 1], Middle(a[j], a[j + 1])));
                    span = Math.Max(span, Distance(fineMiddle[2 * j], Middle(a[j], b[j])));
                    diagonal = Math.Max(diagonal, Distance(fineMiddle[2 * j + 1], Middle(Middle(a[j], b[j + 1]), Middle(a[j + 1], b[j]))));
                }
            }
        }
        return new(chord, span, diagonal);
    }

    /// <summary>
    /// The closed mesh of <paramref name="surface"/>: vertices welded by their binary32 millimetre bits, so the table is the table the bytes carry;
    /// an open trailing edge gets a strip, each tip a cap, and a half a root cap in the plane y = 0 with the opposite winding. Adds no geometry.
    /// </summary>
    internal static StlMesh Close(SurfaceView surface, StlScope scope)
    {
        var index = new Dictionary<(uint, uint, uint), int>();
        var vertices = new List<float>();
        var triangles = new List<int>();
        int V(Point3 p)
        {
            float x = Millimetres(p.X), y = Millimetres(p.Y), z = Millimetres(p.Z);
            var key = (BitConverter.SingleToUInt32Bits(x), BitConverter.SingleToUInt32Bits(y), BitConverter.SingleToUInt32Bits(z));
            if (index.TryGetValue(key, out int found)) return found;
            index[key] = vertices.Count / 3;
            vertices.Add(x); vertices.Add(y); vertices.Add(z);
            return index[key];
        }
        void T(int a, int b, int c)
        {
            if (a == b || b == c || a == c) return;
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
        }
        var sections = surface.Sections;
        int n = sections[0].Upper.Count;
        foreach (bool port in scope == StlScope.Half ? new[] { false } : new[] { false, true })
        {
            Point3 P(Point3 p) => port ? p.Port() : p;
            for (int i = 0; i < sections.Count - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    int a = V(P(sections[i].Upper[j])), b = V(P(sections[i].Upper[j + 1])), c = V(P(sections[i + 1].Upper[j + 1])), d = V(P(sections[i + 1].Upper[j]));
                    if (!port) { T(a, b, c); T(a, c, d); } else { T(a, c, b); T(a, d, c); }
                    a = V(P(sections[i].Lower[j])); b = V(P(sections[i].Lower[j + 1])); c = V(P(sections[i + 1].Lower[j + 1])); d = V(P(sections[i + 1].Lower[j]));
                    if (!port) { T(a, c, b); T(a, d, c); } else { T(a, b, c); T(a, c, d); }
                }
                // The trailing-edge strip: two triangles per cell when the edge is open, none once the welded ends coincide.
                int u0 = V(P(sections[i].Upper[n - 1])), u1 = V(P(sections[i + 1].Upper[n - 1]));
                int l0 = V(P(sections[i].Lower[n - 1])), l1 = V(P(sections[i + 1].Lower[n - 1]));
                if (!port) { T(u0, l0, l1); T(u0, l1, u1); } else { T(u0, l1, l0); T(u0, u1, l1); }
            }
            var tip = sections[^1];
            for (int j = 0; j < n - 1; j++)
            {
                int a = V(P(tip.Upper[j])), b = V(P(tip.Upper[j + 1])), c = V(P(tip.Lower[j + 1])), d = V(P(tip.Lower[j]));
                if (port) { T(a, d, c); T(a, c, b); } else { T(a, c, d); T(a, b, c); }
            }
        }
        if (scope == StlScope.Half)
        {
            var root = sections[0];
            for (int j = 0; j < n - 1; j++)
            {
                int a = V(root.Upper[j]), b = V(root.Upper[j + 1]), c = V(root.Lower[j + 1]), d = V(root.Lower[j]);
                T(a, d, c); T(a, c, b);
            }
        }
        return new([.. vertices], [.. triangles]);
    }

    // Metres to millimetres in binary32; -0.0 becomes +0.0 so two zeros are one vertex and the bytes never carry the sign bit alone.
    private static float Millimetres(double metres) => PositiveZero((float)(metres * 1000));

    private static float PositiveZero(float value) => value == 0 ? 0f : value;

    /// <summary>The binary STL: 80-byte header, a 32-bit count, then 50 bytes per triangle (unit normal, three vertices, two zero bytes).</summary>
    internal static byte[] Write(StlMesh mesh, string header)
    {
        int count = mesh.Triangles.Length / 3;
        var bytes = new byte[84 + 50 * count];
        System.Text.Encoding.ASCII.GetBytes(header.AsSpan(0, Math.Min(header.Length, 80)), bytes.AsSpan(0, 80));
        BitConverter.TryWriteBytes(bytes.AsSpan(80, 4), count);
        var v = mesh.Vertices;
        for (int triangle = 0; triangle < count; triangle++)
        {
            int a = mesh.Triangles[3 * triangle] * 3, b = mesh.Triangles[3 * triangle + 1] * 3, c = mesh.Triangles[3 * triangle + 2] * 3;
            double ux = (double)v[b] - v[a], uy = (double)v[b + 1] - v[a + 1], uz = (double)v[b + 2] - v[a + 2];
            double wx = (double)v[c] - v[a], wy = (double)v[c + 1] - v[a + 1], wz = (double)v[c + 2] - v[a + 2];
            double nx = uy * wz - uz * wy, ny = uz * wx - ux * wz, nz = ux * wy - uy * wx;
            double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length > 0) { nx /= length; ny /= length; nz /= length; }
            int at = 84 + 50 * triangle;
            Put(bytes, at, PositiveZero((float)nx)); Put(bytes, at + 4, PositiveZero((float)ny)); Put(bytes, at + 8, PositiveZero((float)nz));
            for (int corner = 0; corner < 3; corner++)
            {
                int vertex = mesh.Triangles[3 * triangle + corner] * 3;
                Put(bytes, at + 12 + 12 * corner, v[vertex]); Put(bytes, at + 16 + 12 * corner, v[vertex + 1]); Put(bytes, at + 20 + 12 * corner, v[vertex + 2]);
            }
        }
        return bytes;
    }

    private static void Put(byte[] bytes, int at, float value) => BitConverter.TryWriteBytes(bytes.AsSpan(at, 4), value);

    /// <summary>
    /// Reads the bytes back, welds the vertices by their binary32 bit pattern and counts: every directed edge must occur once and have its
    /// opposite once (an edge in exactly two triangles, facing opposite ways), and no triangle may have zero area. Also the Euler number, the signed
    /// volume and the bounds. A length that is not 84 + 50 n is not closed.
    /// </summary>
    public static StlCheck Check(byte[] stl)
    {
        ArgumentNullException.ThrowIfNull(stl);
        int count = stl.Length >= 84 ? BitConverter.ToInt32(stl, 80) : -1;
        if (count < 0 || stl.Length != 84 + 50L * count) return new(false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var welded = new Dictionary<(uint, uint, uint), int>();
        var edges = new Dictionary<long, int>(EdgeKey.Instance);
        int zeroArea = 0;
        double volume = 0;
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;
        Span<int> id = stackalloc int[3];
        Span<double> p = stackalloc double[9];
        for (int triangle = 0; triangle < count; triangle++)
        {
            int at = 84 + 50 * triangle + 12;
            for (int corner = 0; corner < 3; corner++)
            {
                uint bx = BitConverter.ToUInt32(stl, at + 12 * corner), by = BitConverter.ToUInt32(stl, at + 12 * corner + 4), bz = BitConverter.ToUInt32(stl, at + 12 * corner + 8);
                if (!welded.TryGetValue((bx, by, bz), out id[corner])) { id[corner] = welded.Count; welded[(bx, by, bz)] = id[corner]; }
                p[3 * corner] = BitConverter.UInt32BitsToSingle(bx); p[3 * corner + 1] = BitConverter.UInt32BitsToSingle(by); p[3 * corner + 2] = BitConverter.UInt32BitsToSingle(bz);
                minX = Math.Min(minX, p[3 * corner]); maxX = Math.Max(maxX, p[3 * corner]);
                minY = Math.Min(minY, p[3 * corner + 1]); maxY = Math.Max(maxY, p[3 * corner + 1]);
                minZ = Math.Min(minZ, p[3 * corner + 2]); maxZ = Math.Max(maxZ, p[3 * corner + 2]);
            }
            double ux = p[3] - p[0], uy = p[4] - p[1], uz = p[5] - p[2], wx = p[6] - p[0], wy = p[7] - p[1], wz = p[8] - p[2];
            double nx = uy * wz - uz * wy, ny = uz * wx - ux * wz, nz = ux * wy - uy * wx;
            if (id[0] == id[1] || id[1] == id[2] || id[0] == id[2] || nx == 0 && ny == 0 && nz == 0) zeroArea++;
            volume += (p[0] * (p[4] * p[8] - p[5] * p[7]) - p[1] * (p[3] * p[8] - p[5] * p[6]) + p[2] * (p[3] * p[7] - p[4] * p[6])) / 6;
            for (int corner = 0; corner < 3; corner++)
            {
                long key = ((long)id[corner] << 32) | (uint)id[(corner + 1) % 3];
                edges[key] = edges.GetValueOrDefault(key) + 1;
            }
        }
        int unpaired = 0;
        foreach (var (key, uses) in edges)
        {
            long mate = ((key & 0xFFFFFFFFL) << 32) | (long)(uint)(key >> 32);
            if (uses != 1 || !edges.TryGetValue(mate, out int back) || back != 1) unpaired++;
        }
        int euler = welded.Count - edges.Count / 2 + count;
        return new(count > 0 && zeroArea == 0 && unpaired == 0, count, welded.Count, euler, zeroArea, unpaired, volume,
            count == 0 ? 0 : minX, count == 0 ? 0 : minY, count == 0 ? 0 : minZ, count == 0 ? 0 : maxX, count == 0 ? 0 : maxY, count == 0 ? 0 : maxZ);
    }
}

// long.GetHashCode folds (a << 32 | b) to a ^ b, which collides for neighbouring vertex pairs; mix the whole key instead.
internal sealed class EdgeKey : IEqualityComparer<long>
{
    internal static readonly EdgeKey Instance = new();

    public bool Equals(long x, long y) => x == y;

    public int GetHashCode(long key) => (int)(((ulong)key * 0x9E3779B97F4A7C15UL) >> 32);
}
