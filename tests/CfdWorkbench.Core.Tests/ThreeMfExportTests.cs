using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Wing 3MF export (docs/design/export.md 4.3, 5; build condition B1 is the slicer run, tools/check-slicer-open.py). Ring: fast, every push.
/// Cost: about 2.5 s together, measured with CFD_CORE_COST=1 (the presets, 1.3 s, and the four-wing read-back, 0.8 s, are the slowest). Every check reads the package back
/// with System.IO.Compression and System.Xml.Linq, not with ThreeMfExport.Check, so the writer and the reader can disagree.
/// </summary>
internal static class ThreeMfExportTests
{
    private static readonly XNamespace Core = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    internal static void Run()
    {
        Check("ThreeMfExport_Package_HasTheThreePartsAndTheirContentTypes", Package);
        Check("ThreeMfExport_Model_UnitMillimeter_OneObjectOneMeshOneBuildItem", Model);
        Check("ThreeMfExport_AsWritten_ReweldByBitPattern_EveryEdgeTwice_CounterClockwiseOutward_FourWings", AsWrittenClosed);
        Check("ThreeMfExport_TriangleCount_EqualsTheStl_AtEachPreset_AndTheSameMesh", SameMeshAsStl);
        Check("ThreeMfExport_Metadata_OnlyTitleDescriptionApplication", MetadataKeys);
        Check("ThreeMfExport_Package_HoldsNoUserPathOrHost_RuntimeFixture", NoPersonalData);
        Check("ThreeMfExport_Bytes_AreTheSameOnEveryBuild_NoClock", Deterministic);
        Check("ThreeMfExport_Check_RefusesBrokenPackages", CheckRefuses);
        Check("ThreeMfExport_FileName_SlugRevisionHalf", FileNames);
    }

    private static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    private static Dictionary<string, byte[]> Unzip(byte[] package)
    {
        using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        var parts = new Dictionary<string, byte[]>();
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            parts[entry.FullName] = copy.ToArray();
        }
        return parts;
    }

    private static XDocument ModelOf(byte[] package) => XDocument.Parse(Encoding.UTF8.GetString(Unzip(package)["3D/3dmodel.model"]));

    private static void Package()
    {
        var package = ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 3, StlScope.Whole, StlExport.DraftMm).Bytes;
        var parts = Unzip(package);
        True(parts.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(["3D/3dmodel.model", "[Content_Types].xml", "_rels/.rels"]),
            "the parts are exactly the three: " + string.Join(", ", parts.Keys));
        var types = XDocument.Parse(Encoding.UTF8.GetString(parts["[Content_Types].xml"]));
        XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
        True(types.Descendants(ct + "Default").Any(d => (string?)d.Attribute("Extension") == "rels"
            && (string?)d.Attribute("ContentType") == "application/vnd.openxmlformats-package.relationships+xml"), "rels content type");
        True(types.Descendants(ct + "Default").Any(d => (string?)d.Attribute("Extension") == "model"
            && (string?)d.Attribute("ContentType") == "application/vnd.ms-package.3dmanufacturing-3dmodel+xml"), "model content type");
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relationship = XDocument.Parse(Encoding.UTF8.GetString(parts["_rels/.rels"])).Descendants(rel + "Relationship").Single();
        True((string?)relationship.Attribute("Target") == "/3D/3dmodel.model", "the relationship targets the model part");
        True((string?)relationship.Attribute("Type") == "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel", "the relationship is the 3D model type");
        // [Content_Types].xml is first in the archive, as OPC readers expect.
        using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        True(archive.Entries[0].FullName == "[Content_Types].xml", "[Content_Types].xml is the first entry");
    }

    private static void Model()
    {
        var model = ModelOf(ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 3, StlScope.Whole, StlExport.DraftMm).Bytes).Root!;
        True(model.Name == Core + "model", "the root is the core model element");
        True((string?)model.Attribute("unit") == "millimeter", "unit is millimeter, not the default by silence: " + (string?)model.Attribute("unit"));
        var objects = model.Descendants(Core + "object").ToList();
        True(objects.Count == 1 && (string?)objects[0].Attribute("type") == "model", "one object of type model");
        True(objects[0].Elements(Core + "mesh").Count() == 1, "one mesh");
        var items = model.Descendants(Core + "item").ToList();
        True(items.Count == 1 && (string?)items[0].Attribute("objectid") == (string?)objects[0].Attribute("id"), "one build item that names the object");
    }

    // Independent re-weld of the written package: vertices by float32 bit pattern, directed edges once with their reverse once, no degenerate
    // triangle, and a positive signed volume (the 3MF Core rule, 4.1.4: counter-clockwise from outside, so the face normal points out).
    private static (int Triangles, int Vertices, int Bad, int Zero, double Volume) Reweld(byte[] package)
    {
        var mesh = ModelOf(package).Descendants(Core + "mesh").Single();
        var points = mesh.Element(Core + "vertices")!.Elements(Core + "vertex")
            .Select(v => (X: (double)float.Parse((string)v.Attribute("x")!, Invariant), Y: (double)float.Parse((string)v.Attribute("y")!, Invariant), Z: (double)float.Parse((string)v.Attribute("z")!, Invariant))).ToList();
        var ids = new Dictionary<(uint, uint, uint), int>();
        var weld = points.Select(p =>
        {
            var key = (BitConverter.SingleToUInt32Bits((float)p.X), BitConverter.SingleToUInt32Bits((float)p.Y), BitConverter.SingleToUInt32Bits((float)p.Z));
            if (!ids.TryGetValue(key, out int id)) ids[key] = id = ids.Count;
            return id;
        }).ToList();
        var uses = new Dictionary<(int, int), int>();
        int zero = 0, count = 0;
        double volume = 0;
        foreach (var t in mesh.Element(Core + "triangles")!.Elements(Core + "triangle"))
        {
            int[] v = [(int)t.Attribute("v1")!, (int)t.Attribute("v2")!, (int)t.Attribute("v3")!];
            count++;
            int[] id = [weld[v[0]], weld[v[1]], weld[v[2]]];
            var (a, b, c) = (points[v[0]], points[v[1]], points[v[2]]);
            double ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z, wx = c.X - a.X, wy = c.Y - a.Y, wz = c.Z - a.Z;
            if (id[0] == id[1] || id[1] == id[2] || id[0] == id[2] || uy * wz - uz * wy == 0 && uz * wx - ux * wz == 0 && ux * wy - uy * wx == 0) zero++;
            volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6;
            for (int k = 0; k < 3; k++) uses[(id[k], id[(k + 1) % 3])] = uses.GetValueOrDefault((id[k], id[(k + 1) % 3])) + 1;
        }
        int bad = uses.Count(pair => pair.Value != 1 || !uses.TryGetValue((pair.Key.Item2, pair.Key.Item1), out int back) || back != 1);
        return (count, ids.Count, bad, zero, volume);
    }

    private static void AsWrittenClosed()
    {
        foreach (var (name, source) in StlExportTests.Wings())
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                var result = ThreeMfExport.Build(source, name, 1, scope, StlExport.DraftMm);
                var w = Reweld(result.Bytes);
                True(w.Bad == 0, $"{name} {scope}: {w.Bad} edges are not used exactly twice");
                True(w.Zero == 0, $"{name} {scope}: {w.Zero} zero-area triangles");
                True(w.Volume > 0, $"{name} {scope}: signed volume {w.Volume} is not positive (inside out)");
                True(w.Triangles == result.Triangles && w.Vertices == result.Vertices, $"{name} {scope}: the result counts are the package's counts");
                if (scope == StlScope.Whole) True(w.Vertices - w.Triangles * 3 / 2 + w.Triangles == 2, $"{name}: Euler 2");
            }
    }

    private static void SameMeshAsStl()
    {
        var open = StlExportTests.Wings().First().Source;
        foreach (var (preset, mm) in new[] { ("Draft", StlExport.DraftMm), ("Print", StlExport.PrintMm), ("Fine", StlExport.FineMm) })
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                if (preset == "Fine" && scope == StlScope.Half) continue;
                var stl = StlExport.Build(open, "open", 1, scope, mm);
                var mf = ThreeMfExport.Build(open, "open", 1, scope, mm);
                Equal(stl.Triangles, mf.Triangles);
                Equal(stl.Vertices, mf.Vertices);
                if (preset == "Fine") { Equal(ThreeMfExport.Check(mf.Bytes).Triangles, stl.Triangles); continue; }   // the XDocument read-back is slow at 129,000 triangles; Check is the same rule
                Equal(Reweld(mf.Bytes).Triangles, stl.Triangles);
                Equal(stl.SizeXMm, mf.SizeXMm); Equal(stl.SizeYMm, mf.SizeYMm); Equal(stl.SizeZMm, mf.SizeZMm);
                Equal(stl.VolumeMm3, mf.VolumeMm3);
                Equal(stl.DeviationMm, mf.DeviationMm);
                True(Math.Abs(Reweld(mf.Bytes).Volume - stl.VolumeMm3) <= 1e-9 * Math.Abs(stl.VolumeMm3), $"{preset} {scope}: the volume read back from the package is the STL's");
            }
    }

    private static Dictionary<string, string> Metadata(byte[] package) =>
        ModelOf(package).Root!.Elements(Core + "metadata").ToDictionary(m => (string)m.Attribute("name")!, m => m.Value);

    private static void MetadataKeys()
    {
        var result = ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 12, StlScope.Whole, StlExport.PrintMm);
        var metadata = Metadata(result.Bytes);
        True(metadata.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(["Application", "Description", "Title"]), "keys: " + string.Join(", ", metadata.Keys));
        Equal("Basic Foil", metadata["Title"]);
        Equal($"Revision r12, tolerance 0.02 mm, measured deviation {result.DeviationMm.ToString("F4", Invariant)} mm", metadata["Description"]);
        True(metadata["Application"].StartsWith("CFD Workbench ", StringComparison.Ordinal) && metadata["Application"].Length > "CFD Workbench ".Length, "Application: " + metadata["Application"]);
        // The foil name is data: markup characters are escaped, not parsed.
        var odd = ThreeMfExport.Build(StlExportTests.Example(), "A&B <x> \"q\"", 1, StlScope.Half, StlExport.DraftMm);
        Equal("A&B <x> \"q\"", Metadata(odd.Bytes)["Title"]);
    }

    // The fixture is built at run time from this machine: the account name, the host name, the home folder, the working folder, the temp folder
    // and the repository root. None may appear in any part of the package, in any case.
    private static void NoPersonalData()
    {
        string?[] secrets = [Environment.UserName, Environment.MachineName, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Directory.GetCurrentDirectory(), Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), PlacementTests.RepoRoot()];
        var wanted = secrets.Where(s => !string.IsNullOrWhiteSpace(s) && s!.Length >= 3).Select(s => s!).ToList();
        True(wanted.Count >= 4, "the runtime fixture found its secrets: " + string.Join(" | ", wanted));
        foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
        {
            var package = ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 2, scope, StlExport.DraftMm).Bytes;
            foreach (var (part, bytes) in Unzip(package))
            {
                string text = Encoding.UTF8.GetString(bytes);
                foreach (string secret in wanted)
                    True(!text.Contains(secret, StringComparison.OrdinalIgnoreCase), $"{part} holds the runtime value '{secret}'");
            }
            // The zip headers too: a clock or an account in the archive (extra fields, comment) would show here. Only values of 8+ characters: in deflated bytes a short one matches by chance.
            string raw = Encoding.Latin1.GetString(package);
            foreach (string secret in wanted.Where(s => s.Length >= 8))
                True(!raw.Contains(secret, StringComparison.OrdinalIgnoreCase), $"the archive holds the runtime value '{secret}'");
            using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
            True(archive.Comment.Length == 0, "no archive comment");
            True(archive.Entries.All(e => e.LastWriteTime.Year == 1980), "no modification time: " + string.Join(",", archive.Entries.Select(e => e.LastWriteTime)));
        }
    }

    private static void Deterministic()
    {
        var a = ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 2, StlScope.Whole, StlExport.DraftMm).Bytes;
        var b = ThreeMfExport.Build(StlExportTests.Example(), "Basic Foil", 2, StlScope.Whole, StlExport.DraftMm).Bytes;
        True(a.AsSpan().SequenceEqual(b), "two builds of one wing are one byte sequence");
    }

    private static byte[] Rebuild(byte[] package, Func<string, string> edit)
    {
        var parts = Unzip(package);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, bytes) in parts)
            {
                var entry = archive.CreateEntry(name);
                using var stream = entry.Open();
                var content = name == "3D/3dmodel.model" ? Encoding.UTF8.GetBytes(edit(Encoding.UTF8.GetString(bytes))) : bytes;
                stream.Write(content);
            }
        return output.ToArray();
    }

    private static void CheckRefuses()
    {
        var good = ThreeMfExport.Build(StlExportTests.Example(), "closed", 1, StlScope.Whole, StlExport.DraftMm).Bytes;
        var check = ThreeMfExport.Check(good);
        True(check.Closed && check.UnpairedEdges == 0, "the written package passes");
        var stl = StlExport.Build(StlExportTests.Example(), "closed", 1, StlScope.Whole, StlExport.DraftMm);
        Equal(stl.Triangles, check.Triangles);
        Equal(stl.VolumeMm3, check.VolumeMm3);
        int start = Encoding.UTF8.GetString(Unzip(good)["3D/3dmodel.model"]).IndexOf("<triangle ", StringComparison.Ordinal);
        True(start > 0, "the model has triangles");
        var oneGone = ThreeMfExport.Check(Rebuild(good, text =>
        {
            int first = text.IndexOf("<triangle ", start + 10, StringComparison.Ordinal);
            return text.Remove(first, text.IndexOf("/>", first, StringComparison.Ordinal) + 2 - first);
        }));
        True(!oneGone.Closed && oneGone.UnpairedEdges > 0, "one triangle removed leaves edges unpaired");
        True(!ThreeMfExport.Check(good[..^7]).Closed, "a truncated package is not closed");
        True(!ThreeMfExport.Check([1, 2, 3]).Closed, "bytes that are not a zip are not closed");
        True(!ThreeMfExport.Check(Rebuild(good, text => text.Replace("unit=\"millimeter\"", "unit=\"meter\"", StringComparison.Ordinal))).Closed,
            "a unit other than millimeter is not accepted");
    }

    private static void FileNames()
    {
        Equal("basic-foil-r12.3mf", ThreeMfExport.FileName("Basic Foil", 12, StlScope.Whole));
        Equal("basic-foil-r12-half.3mf", ThreeMfExport.FileName("Basic Foil", 12, StlScope.Half));
    }
}
