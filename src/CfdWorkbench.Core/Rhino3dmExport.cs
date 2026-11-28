using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace CfdWorkbench.Core;

/// <summary>Live in-memory round-trip verification report for a written Rhino <c>.3dm</c> payload.</summary>
public sealed record Rhino3dmVerificationReport(
    bool Passed,
    string UnitSystem,
    IReadOnlyList<string> LayerNames,
    int SurfaceCount,
    int MasterCurveCount,
    int AuthoredStationCurveCount,
    int ObjectCount,
    bool ZeroPiiVerified,
    IReadOnlyList<string> Errors);

/// <summary>Result of one Rhino <c>.3dm</c> export (ADR-0013 §Decision 2, EXP-04).</summary>
public sealed record Rhino3dmExportResult(
    byte[] Bytes,
    StlScope Scope,
    int SectionCount,
    int ChordControlPoints,
    int DegreeU,
    int DegreeV,
    double DeviationMm,
    double TrailingEdgeMm,
    double TrailingEdgeYMm,
    bool TrailingEdgeAtTip,
    bool TrailingEdgeAlongSpan,
    bool IsOpenTrailingEdge,
    double SizeXMm,
    double SizeYMm,
    double SizeZMm,
    Rhino3dmVerificationReport Verification);

/// <summary>
/// Pure in-memory MIT <c>Rhino3dm 8.35.0</c> exporter (<c>STORE-SUBSET</c> compliant: uses
/// <see cref="File3dm.ToByteArray(File3dmWriteOptions)"/> and <see cref="File3dm.FromByteArray(byte[])"/>
/// with zero filesystem I/O). Emits millimetres unscaled with 3 ubiquitous-language layers:
/// <c>Wing Skin</c>, <c>Master Curves</c>, and <c>Authored Stations</c>.
/// </summary>
public static class Rhino3dmExport
{
    public const string WingSkinLayer = "Wing Skin";
    public const string MasterCurvesLayer = "Master Curves";
    public const string AuthoredStationsLayer = "Authored Stations";

    private static readonly Regex WinUserRx = new(@"(?:[A-Za-z]:|/mnt/[a-z])(?:\\|/)+Users(?:\\|/)+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DefaultHostRx = new(@"(?:DESKTOP|LAPTOP)-[A-Z0-9]{7}|WIN-[A-Z0-9]{11}", RegexOptions.Compiled);

    public static string FileName(string foilName, int revision, StlScope scope) =>
        $"{DatImport.Slug(foilName)}-r{revision.ToString(CultureInfo.InvariantCulture)}{(scope == StlScope.Half ? "-half" : "")}.3dm";

    public static Rhino3dmExportResult Build(byte[] source, string foilName, int revision, StlScope scope, CancellationToken cancellation = default)
    {
        var skin = CadExportExtractor.BuildSkin(source, cancellation);
        var masterCurves = CadExportExtractor.ExtractMasterCurvesMm(source, skin, scope, cancellation);
        var stationCurves = CadExportExtractor.ExtractAuthoredStationCurvesMm(source, scope, cancellation);
        byte[] bytes = WriteBytes(skin, masterCurves, stationCurves, foilName, revision, scope, notesOverride: null);
        var verification = Verify(bytes, scope);
        if (!verification.Passed)
            throw new ContractError("EXPORT-3DM-INVALID", string.Join("; ", verification.Errors));

        return new Rhino3dmExportResult(
            bytes,
            scope,
            skin.SectionCount,
            skin.ChordCount,
            skin.UpperStarboard.DegreeU,
            skin.UpperStarboard.DegreeV,
            skin.Deviation.MaxClosestPointMm,
            skin.TrailingEdgeMinMm,
            skin.TrailingEdgeMinYMm,
            skin.TrailingEdgeAtTip,
            skin.TrailingEdgeAlongSpan,
            skin.IsOpenTrailingEdge,
            skin.SizeXMm,
            skin.SizeYMm(scope),
            skin.SizeZMm,
            verification);
    }

    /// <summary>Builds a <c>.3dm</c> payload with explicit notes text for red-first zero-PII negative controls.</summary>
    public static byte[] BuildWithNotesForTest(byte[] source, string foilName, int revision, StlScope scope, string notesOverride)
    {
        var skin = CadExportExtractor.BuildSkin(source);
        var masterCurves = CadExportExtractor.ExtractMasterCurvesMm(source, skin, scope);
        var stationCurves = CadExportExtractor.ExtractAuthoredStationCurvesMm(source, scope);
        return WriteBytes(skin, masterCurves, stationCurves, foilName, revision, scope, notesOverride);
    }

    /// <summary>Reads back <paramref name="bytes"/> in-memory via <see cref="File3dm.FromByteArray(byte[])"/> and verifies units, layers, objects, and zero PII.</summary>
    public static Rhino3dmVerificationReport Verify(byte[] bytes, StlScope scope, IReadOnlyList<string>? extraForbiddenTokens = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var errors = new List<string>();
        if (bytes.Length == 0)
        {
            errors.Add("3DM payload is empty.");
            return new Rhino3dmVerificationReport(false, "", [], 0, 0, 0, 0, false, errors);
        }

        using var readBack = File3dm.FromByteArray(bytes);
        if (readBack is null)
        {
            errors.Add("File3dm.FromByteArray returned null.");
            return new Rhino3dmVerificationReport(false, "", [], 0, 0, 0, 0, false, errors);
        }

        bool zeroPii = CheckZeroPii(bytes, readBack, extraForbiddenTokens, errors);

        string unit = readBack.Settings.ModelUnitSystem.ToString();
        if (readBack.Settings.ModelUnitSystem != UnitSystem.Millimeters)
            errors.Add($"Expected Millimeters unit system, observed {unit}.");

        string[] layers = readBack.AllLayers.Select(l => l.Name).ToArray();
        if (layers.Length != 3 || layers[0] != WingSkinLayer || layers[1] != MasterCurvesLayer || layers[2] != AuthoredStationsLayer)
            errors.Add($"Expected 3 layers [{WingSkinLayer}, {MasterCurvesLayer}, {AuthoredStationsLayer}], observed [{string.Join(", ", layers)}].");

        int surfaceCount = 0;
        int masterCurveCount = 0;
        int stationCurveCount = 0;
        foreach (var obj in readBack.Objects)
        {
            int layerIdx = obj.Attributes.LayerIndex;
            string layerName = layerIdx >= 0 && layerIdx < layers.Length ? layers[layerIdx] : "";
            if (obj.Geometry is NurbsSurface ns)
            {
                if (layerName != WingSkinLayer)
                    errors.Add($"NurbsSurface '{obj.Attributes.Name}' placed on unexpected layer '{layerName}'.");
                if (!ns.IsValid)
                    errors.Add($"NurbsSurface '{obj.Attributes.Name}' failed openNURBS IsValid.");
                surfaceCount++;
            }
            else if (obj.Geometry is NurbsCurve nc)
            {
                if (!nc.IsValid)
                    errors.Add($"NurbsCurve '{obj.Attributes.Name}' failed openNURBS IsValid.");
                if (layerName == MasterCurvesLayer) masterCurveCount++;
                else if (layerName == AuthoredStationsLayer) stationCurveCount++;
                else errors.Add($"NurbsCurve '{obj.Attributes.Name}' placed on unexpected layer '{layerName}'.");
            }
        }

        int minSurfaces = scope == StlScope.Whole ? 4 : 2;
        if (surfaceCount < minSurfaces)
            errors.Add($"Expected at least {minSurfaces} NURBS surfaces for {scope}, observed {surfaceCount}.");
        int minMaster = scope == StlScope.Whole ? 10 : 5;
        if (masterCurveCount < minMaster)
            errors.Add($"Expected at least {minMaster} master curves for {scope}, observed {masterCurveCount}.");
        if (stationCurveCount < 2)
            errors.Add($"Expected at least 2 authored station curves, observed {stationCurveCount}.");

        return new Rhino3dmVerificationReport(
            errors.Count == 0 && zeroPii,
            unit,
            layers,
            surfaceCount,
            masterCurveCount,
            stationCurveCount,
            readBack.Objects.Count,
            zeroPii,
            errors);
    }

    private static byte[] WriteBytes(
        WingCadSkin skin,
        IReadOnlyList<NamedCadCurve> masterCurves,
        IReadOnlyList<NamedCadCurve> stationCurves,
        string foilName,
        int revision,
        StlScope scope,
        string? notesOverride)
    {
        string slug = DatImport.Slug(foilName);
        var version = typeof(Rhino3dmExport).Assembly.GetName().Version;
        string appVersion = $"CFD Workbench {version?.Major ?? 0}.{version?.Minor ?? 0}.{Math.Max(0, version?.Build ?? 0)}";

        using var file = new File3dm();
        file.Settings.ModelUnitSystem = UnitSystem.Millimeters;
        file.ApplicationName = appVersion;
        file.ApplicationUrl = "";
        file.ApplicationDetails = "mm, unscaled";
        file.StartSectionComments = "CFD Workbench 3DM export (mm, unscaled)";
        file.Revision = revision;
        file.Notes = new File3dmNotes
        {
            Notes = notesOverride ?? $"{slug} r{revision.ToString(CultureInfo.InvariantCulture)} ({scope.ToString().ToLowerInvariant()}, mm, unscaled)"
        };

        int skinLayer = file.AllLayers.AddLayer(WingSkinLayer, System.Drawing.Color.SteelBlue);
        int masterLayer = file.AllLayers.AddLayer(MasterCurvesLayer, System.Drawing.Color.DarkOrange);
        int stationLayer = file.AllLayers.AddLayer(AuthoredStationsLayer, System.Drawing.Color.ForestGreen);

        AddSurface(file, skinLayer, "Wing Skin Upper (Starboard)", skin.UpperStarboard);
        AddSurface(file, skinLayer, "Wing Skin Lower (Starboard)", skin.LowerStarboard);
        if (skin.IsOpenTrailingEdge)
        {
            var teS = CadNurbsMath.RuledStrip(skin.UpperStarboard.IsoU(skin.UpperStarboard.CountU - 1), skin.LowerStarboard.IsoU(skin.LowerStarboard.CountU - 1));
            AddSurface(file, skinLayer, "Trailing Edge Strip (Starboard)", teS);
        }
        if (skin.TipMode == "rounded" && skin.StarboardTipCap is { } stbdCap)
        {
            AddSurface(file, skinLayer, "Tip Cap (Starboard)", stbdCap);
            if (skin.IsOpenTrailingEdge && skin.StarboardTipCapTeEnd is { } stbdTeEnd)
                AddSurface(file, skinLayer, "Tip Cap TE End (Starboard)", stbdTeEnd);
        }

        if (scope == StlScope.Whole)
        {
            var upperP = skin.UpperStarboard.MirrorY();
            var lowerP = skin.LowerStarboard.MirrorY();
            AddSurface(file, skinLayer, "Wing Skin Upper (Port)", upperP);
            AddSurface(file, skinLayer, "Wing Skin Lower (Port)", lowerP);
            if (skin.IsOpenTrailingEdge)
            {
                var teP = CadNurbsMath.RuledStrip(upperP.IsoU(upperP.CountU - 1), lowerP.IsoU(lowerP.CountU - 1));
                AddSurface(file, skinLayer, "Trailing Edge Strip (Port)", teP);
            }
            if (skin.TipMode == "rounded" && skin.StarboardTipCap is { } stbdCapPort)
            {
                AddSurface(file, skinLayer, "Tip Cap (Port)", stbdCapPort.MirrorY());
                if (skin.IsOpenTrailingEdge && skin.StarboardTipCapTeEnd is { } stbdTeEndPort)
                    AddSurface(file, skinLayer, "Tip Cap TE End (Port)", stbdTeEndPort.MirrorY());
            }
        }

        foreach (var mc in masterCurves)
            AddCurve(file, masterLayer, mc.Name, mc.Curve);

        foreach (var sc in stationCurves)
            AddCurve(file, stationLayer, sc.Name, sc.Curve);

        var options = new File3dmWriteOptions
        {
            Version = 8,
            SaveUserData = false,
        };
        byte[]? bytes = file.ToByteArray(options);
        if (bytes is null || bytes.Length == 0)
            throw new ContractError("EXPORT-3DM-INVALID", "File3dm.ToByteArray returned an empty buffer.");
        return bytes;
    }

    private static void AddSurface(File3dm file, int layerIndex, string name, CadNurbsSurface src)
    {
        var srf = NurbsSurface.Create(3, false, src.DegreeU + 1, src.DegreeV + 1, src.CountU, src.CountV)
            ?? throw new ContractError("EXPORT-3DM-INVALID", $"NurbsSurface.Create failed for {name}.");
        for (int i = 0; i < src.CountU; i++)
            for (int j = 0; j < src.CountV; j++)
            {
                var p = src.Poles[i, j];
                srf.Points.SetPoint(i, j, new Point3d(p.X, p.Y, p.Z));
            }
        // openNURBS stores n + p - 1 knots (omitting the first and last clamped knot of the Piegl & Tiller vector).
        for (int i = 0; i < srf.KnotsU.Count; i++)
            srf.KnotsU[i] = src.KnotsU[i + 1];
        for (int j = 0; j < srf.KnotsV.Count; j++)
            srf.KnotsV[j] = src.KnotsV[j + 1];

        var attr = new ObjectAttributes { LayerIndex = layerIndex, Name = name };
        file.Objects.AddSurface(srf, attr);
    }

    private static void AddCurve(File3dm file, int layerIndex, string name, CadNurbsCurve src)
    {
        var crv = new NurbsCurve(3, false, src.Degree + 1, src.Poles.Length);
        for (int i = 0; i < src.Poles.Length; i++)
        {
            var p = src.Poles[i];
            crv.Points.SetPoint(i, p.X, p.Y, p.Z);
        }
        for (int i = 0; i < crv.Knots.Count; i++)
            crv.Knots[i] = src.Knots[i + 1];

        var attr = new ObjectAttributes { LayerIndex = layerIndex, Name = name };
        file.Objects.AddCurve(crv, attr);
    }

    private static bool CheckZeroPii(byte[] raw, File3dm readBack, IReadOnlyList<string>? extraForbidden, List<string> errors)
    {
        // Scan raw ASCII/Latin-1, raw UTF-16LE, AND decoded openNURBS metadata/notes/layer/object strings
        // because openNURBS stores notes/strings as UTF-16LE or compressed streams.
        var decoded = new StringBuilder();
        decoded.Append(readBack.StartSectionComments).Append('\n');
        decoded.Append(readBack.ApplicationName).Append('\n');
        decoded.Append(readBack.ApplicationUrl).Append('\n');
        decoded.Append(readBack.ApplicationDetails).Append('\n');
        decoded.Append(readBack.CreatedBy).Append('\n');
        decoded.Append(readBack.LastEditedBy).Append('\n');
        decoded.Append(readBack.Notes?.Notes).Append('\n');
        foreach (var layer in readBack.AllLayers) decoded.Append(layer.Name).Append('\n');
        foreach (var obj in readBack.Objects) decoded.Append(obj.Attributes.Name).Append('\n');

        string[] haystacks =
        [
            Encoding.Latin1.GetString(raw),
            Encoding.Unicode.GetString(raw),
            decoded.ToString(),
        ];

        bool ok = true;
        foreach (string text in haystacks)
        {
            if (WinUserRx.IsMatch(text))
            {
                errors.Add("Zero-PII check failed: Windows user-profile path detected in 3DM bytes/metadata.");
                ok = false;
                break;
            }
        }
        foreach (string text in haystacks)
        {
            if (DefaultHostRx.IsMatch(text))
            {
                errors.Add("Zero-PII check failed: default Windows hostname detected in 3DM bytes/metadata.");
                ok = false;
                break;
            }
        }
        string user = System.Environment.UserName;
        if (!string.IsNullOrEmpty(user) && user.Length >= 3 && haystacks.Any(t => t.Contains(user, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Zero-PII check failed: current OS username detected in 3DM bytes/metadata.");
            ok = false;
        }
        string host = System.Environment.MachineName;
        if (!string.IsNullOrEmpty(host) && host.Length >= 4 && haystacks.Any(t => t.Contains(host, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Zero-PII check failed: current machine hostname detected in 3DM bytes/metadata.");
            ok = false;
        }
        if (extraForbidden is not null)
        {
            foreach (string token in extraForbidden)
            {
                if (!string.IsNullOrEmpty(token) && haystacks.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("Zero-PII check failed: forbidden marker detected in 3DM bytes/metadata.");
                    ok = false;
                }
            }
        }
        return ok;
    }
}
