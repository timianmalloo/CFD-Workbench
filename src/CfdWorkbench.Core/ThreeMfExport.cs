using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace CfdWorkbench.Core;

/// <summary>
/// The wing 3MF writer (Export design 4.3, 5). It writes the welded, closed mesh <see cref="StlExport"/> builds, unchanged: the same ladder, the same
/// deviation, the same closing step, then a ZIP package of three parts (<c>[Content_Types].xml</c>, <c>_rels/.rels</c>, <c>3D/3dmodel.model</c>)
/// with one object, one mesh and one build item. The unit is the attribute <c>millimeter</c> and the coordinates are the STL's binary32 millimetres, so
/// nothing is scaled. Every write is read back and refused (<c>EXPORT-NOT-CLOSED</c>) when an edge is not shared by exactly two triangles.
/// </summary>
/// <remarks>
/// Verified against the 3MF Core specification, master at version 1.4.0 (docs/proof/tmf/3mf-core-spec-excerpts.md): section 3.4 lists <c>millimeter</c> as a unit value and
/// the default; section 4.1.4 requires counter-clockwise vertex order seen from outside, face normal outward. The STL writer's winding is already
/// outward, so the triangles are written in the same order.
/// </remarks>
public static class ThreeMfExport
{
    private const string CoreNamespace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    private const string ModelPart = "3D/3dmodel.model";
    private const string ModelContentType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml";
    private const string RelationshipsContentType = "application/vnd.openxmlformats-package.relationships+xml";
    private const string ModelRelationshipType = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel";

    // A fixed modification time, so the archive carries no clock and two builds of one wing are one byte sequence.
    private static readonly DateTimeOffset FixedTime = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The wing 3MF at the first ladder rung whose measured deviation is at or under <paramref name="toleranceMm"/>, or the finest rung. <c>Bytes</c> is the package.</summary>
    public static StlExportResult Build(byte[] source, string foilName, int revision, StlScope scope, double toleranceMm,
        CancellationToken cancellation = default)
    {
        var stage = StlExport.Stage(source, toleranceMm, StlExport.Ladder, cancellation);
        byte[] bytes = Write(StlExport.Close(stage.Surface, scope), foilName, revision, toleranceMm, stage.DeviationMm);
        return StlExport.Summarize(bytes, Check(bytes), stage, scope, toleranceMm);
    }

    /// <summary><c>&lt;foil-slug&gt;-r&lt;n&gt;.3mf</c>, a starboard half <c>&lt;foil-slug&gt;-r&lt;n&gt;-half.3mf</c> (the unit is in the attribute, so there is no <c>-mm</c>).</summary>
    public static string FileName(string foilName, int revision, StlScope scope) =>
        $"{DatImport.Slug(foilName)}-r{revision.ToString(CultureInfo.InvariantCulture)}{(scope == StlScope.Half ? "-half" : "")}.3mf";

    private static byte[] Write(StlMesh mesh, string foilName, int revision, double toleranceMm, double deviationMm)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Put(archive, "[Content_Types].xml", ContentTypes());
            Put(archive, "_rels/.rels", Relationships());
            Put(archive, ModelPart, Model(mesh, foilName, revision, toleranceMm, deviationMm));
        }
        return output.ToArray();
    }

    private static void Put(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = FixedTime;
        using var stream = entry.Open();
        stream.Write(content);
    }

    private static XmlWriter Writer(MemoryStream stream) =>
        XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, NewLineHandling = NewLineHandling.None });

    private static byte[] ContentTypes()
    {
        using var stream = new MemoryStream();
        using (var xml = Writer(stream))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
            xml.WriteStartElement("Default"); xml.WriteAttributeString("Extension", "rels"); xml.WriteAttributeString("ContentType", RelationshipsContentType); xml.WriteEndElement();
            xml.WriteStartElement("Default"); xml.WriteAttributeString("Extension", "model"); xml.WriteAttributeString("ContentType", ModelContentType); xml.WriteEndElement();
            xml.WriteEndElement();
        }
        return stream.ToArray();
    }

    private static byte[] Relationships()
    {
        using var stream = new MemoryStream();
        using (var xml = Writer(stream))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
            xml.WriteStartElement("Relationship");
            xml.WriteAttributeString("Target", "/" + ModelPart);
            xml.WriteAttributeString("Id", "rel0");
            xml.WriteAttributeString("Type", ModelRelationshipType);
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        return stream.ToArray();
    }

    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    // Metadata is exactly three keys. The description is numbers the dialog already shows; no name, path or host is ever read here (spec A8.5).
    private static byte[] Model(StlMesh mesh, string foilName, int revision, double toleranceMm, double deviationMm)
    {
        var invariant = CultureInfo.InvariantCulture;
        var version = typeof(ThreeMfExport).Assembly.GetName().Version;
        using var stream = new MemoryStream();
        using (var xml = Writer(stream))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("model", CoreNamespace);
            xml.WriteAttributeString("unit", "millimeter");
            xml.WriteAttributeString("xml", "lang", null, "en-US");
            Metadata(xml, "Title", foilName);
            Metadata(xml, "Description", $"Revision r{revision.ToString(invariant)}, tolerance {toleranceMm.ToString("0.0##", invariant)} mm, measured deviation {deviationMm.ToString("F4", invariant)} mm");
            Metadata(xml, "Application", $"CFD Workbench {version?.Major ?? 0}.{version?.Minor ?? 0}.{Math.Max(0, version?.Build ?? 0)}");
            xml.WriteStartElement("resources");
            xml.WriteStartElement("object"); xml.WriteAttributeString("id", "1"); xml.WriteAttributeString("type", "model");
            xml.WriteStartElement("mesh");
            xml.WriteStartElement("vertices");
            for (int vertex = 0; vertex < mesh.Vertices.Length; vertex += 3)
            {
                xml.WriteStartElement("vertex");
                xml.WriteAttributeString("x", Number(mesh.Vertices[vertex]));
                xml.WriteAttributeString("y", Number(mesh.Vertices[vertex + 1]));
                xml.WriteAttributeString("z", Number(mesh.Vertices[vertex + 2]));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteStartElement("triangles");
            for (int corner = 0; corner < mesh.Triangles.Length; corner += 3)
            {
                xml.WriteStartElement("triangle");
                xml.WriteAttributeString("v1", mesh.Triangles[corner].ToString(invariant));
                xml.WriteAttributeString("v2", mesh.Triangles[corner + 1].ToString(invariant));
                xml.WriteAttributeString("v3", mesh.Triangles[corner + 2].ToString(invariant));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteStartElement("build");
            xml.WriteStartElement("item"); xml.WriteAttributeString("objectid", "1"); xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        return stream.ToArray();
    }

    private static void Metadata(XmlWriter xml, string name, string value)
    {
        xml.WriteStartElement("metadata");
        xml.WriteAttributeString("name", name);
        xml.WriteString(value);
        xml.WriteEndElement();
    }

    /// <summary>
    /// Reads the package back and runs the STL edge check on what it holds: vertices welded by their binary32 bit pattern, every directed edge once with
    /// its opposite once, no zero-area triangle. Also the Euler number, the signed volume and the bounds. A package that is not a ZIP, has no model part,
    /// a unit other than <c>millimeter</c>, or an index out of range is not closed.
    /// </summary>
    public static StlCheck Check(byte[] package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var notClosed = new StlCheck(false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        try
        {
            using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
            var entry = archive.GetEntry(ModelPart);
            if (entry is null) return notClosed;
            using var stream = entry.Open();
            using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var points = new List<uint>();
            var corners = new List<uint>();
            bool millimeter = false;
            while (xml.Read())
            {
                if (xml.NodeType != XmlNodeType.Element) continue;
                switch (xml.LocalName)
                {
                    case "model": millimeter = xml.GetAttribute("unit") == "millimeter"; break;
                    case "vertex":
                        foreach (string axis in new[] { "x", "y", "z" })
                            points.Add(BitConverter.SingleToUInt32Bits(float.Parse(xml.GetAttribute(axis)!, NumberStyles.Float, CultureInfo.InvariantCulture)));
                        break;
                    case "triangle":
                        foreach (string index in new[] { "v1", "v2", "v3" })
                        {
                            int at = int.Parse(xml.GetAttribute(index)!, NumberStyles.None, CultureInfo.InvariantCulture);
                            if (at < 0 || 3 * at + 2 >= points.Count) return notClosed;
                            corners.Add(points[3 * at]); corners.Add(points[3 * at + 1]); corners.Add(points[3 * at + 2]);
                        }
                        break;
                }
            }
            return millimeter ? StlExport.Analyze([.. corners]) : notClosed;
        }
        catch (Exception error) when (error is InvalidDataException or XmlException or FormatException or ArgumentException or OverflowException)
        {
            return notClosed;
        }
    }
}
