using System.Text;
using System.Text.Json;

namespace CfdWorkbench.Analysis.Export;

/// <summary>
/// Release-gate status for Area 7 STEP AP203 and Rhino 3DM export (ADR-0013 §Decision 4, Ruling 273 DR-KRN-2).
/// Production UI keeps STEP and 3DM disabled behind <c>COPY-481</c> until <c>docs/proof/cad-export-measure/cad-open-measure.json</c>
/// records both red-first planted-defect failures and green open-and-measure passes in Autodesk Fusion, FreeCAD CAM (Path),
/// Vectric VCarve/Aspire (for STEP), and Rhino 8 (for 3DM).
/// </summary>
public sealed record CadExportGateStatus(
    bool StepUnlocked,
    bool ThreeDmUnlocked,
    string StepReason,
    string ThreeDmReason,
    IReadOnlyList<string> MissingOrFailedTargets);

/// <summary>
/// Evaluates <c>docs/proof/cad-export-measure/cad-open-measure.json</c> against the ADR-0013 §Decision 4 tolerances:
/// units = mm unscaled, chord/span/thickness error &lt;= 10 um, TE error &lt;= 5 um, single watertight solid (STEP) with
/// 0 repair warnings, volume error &lt;= 0.1 %, zero header PII, and mandatory red-first planted-defect detection.
/// </summary>
public static class CadExportReleaseGate
{
    public const string DefaultReceiptRelativePath = "docs/proof/cad-export-measure/cad-open-measure.json";
    public const double MaxLinearErrorUm = 10.0;
    public const double MaxTrailingEdgeErrorUm = 5.0;
    public const double MaxVolumeErrorPercent = 0.1;

    public static readonly IReadOnlyList<string> RequiredStepTargetIds =
    [
        "autodesk-fusion",
        "freecad-cam",
        "vectric-vcarve",
    ];

    public const string RequiredThreeDmTargetId = "rhino-8";

    public static CadExportGateStatus Gated { get; } = new(
        StepUnlocked: false,
        ThreeDmUnlocked: false,
        StepReason: ExportCopy.StepUnavailable,
        ThreeDmReason: ExportCopy.StepUnavailable,
        MissingOrFailedTargets: ["autodesk-fusion", "freecad-cam", "vectric-vcarve", "rhino-8"]);

    /// <summary>
    /// Loads the default receipt path if present beside the working directory or application directory;
    /// returns <see cref="Gated"/> (COPY-481) whenever the receipt is absent, unreadable, or incomplete.
    /// </summary>
    public static CadExportGateStatus LoadDefault()
    {
        string? candidate = ResolveReceiptPath(Directory.GetCurrentDirectory())
            ?? ResolveReceiptPath(AppContext.BaseDirectory);
        return candidate is null ? Gated : LoadFromFile(candidate);
    }

    /// <summary>Reads a receipt file with explicit <c>FileShare.Read | FileShare.Delete</c> (READER-SHARE compliant).</summary>
    public static CadExportGateStatus LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Gated;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return EvaluateJson(reader.ReadToEnd());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Gated;
        }
    }

    /// <summary>Pure JSON receipt validator enforcing ADR-0013 §Decision 4 and Ruling 273.</summary>
    public static CadExportGateStatus EvaluateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Gated;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Gated;
            if (!root.TryGetProperty("schemaVersion", out var sv) || sv.ValueKind != JsonValueKind.Number || sv.GetInt32() != 1)
                return Gated;
            if (!root.TryGetProperty("targets", out var targetsEl) || targetsEl.ValueKind != JsonValueKind.Array)
                return Gated;

            var byId = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var item in targetsEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("id", out var idEl) &&
                    idEl.ValueKind == JsonValueKind.String &&
                    idEl.GetString() is { Length: > 0 } id)
                {
                    byId[id] = item;
                }
            }

            var failed = new List<string>();
            bool allStepPassed = true;
            foreach (string id in RequiredStepTargetIds)
            {
                if (!byId.TryGetValue(id, out var entry) || !TargetPasses(entry, requireWatertightSolid: true))
                {
                    allStepPassed = false;
                    failed.Add(id);
                }
            }

            bool threeDmPassed = byId.TryGetValue(RequiredThreeDmTargetId, out var rhinoEntry)
                && TargetPasses(rhinoEntry, requireWatertightSolid: false);
            if (!threeDmPassed)
                failed.Add(RequiredThreeDmTargetId);

            return new CadExportGateStatus(
                StepUnlocked: allStepPassed,
                ThreeDmUnlocked: threeDmPassed,
                StepReason: allStepPassed ? ExportCopy.CadGateVerifiedStep : ExportCopy.StepUnavailable,
                ThreeDmReason: threeDmPassed ? ExportCopy.CadGateVerifiedThreeDm : ExportCopy.StepUnavailable,
                MissingOrFailedTargets: failed);
        }
        catch (JsonException)
        {
            return Gated;
        }
    }

    private static bool TargetPasses(JsonElement entry, bool requireWatertightSolid)
    {
        if (!ReadBool(entry, "redFirstPassed")) return false;
        if (!ReadNonEmptyString(entry, "redFirstDefect")) return false;
        if (!ReadBool(entry, "greenPassed")) return false;
        if (!ReadBool(entry, "unitsMmUnscaled")) return false;
        if (!ReadBool(entry, "zeroPiiVerified")) return false;
        if (requireWatertightSolid && !ReadBool(entry, "singleWatertightSolid")) return false;

        if (!TryReadDouble(entry, "chordErrorUm", out double chord) || chord < 0 || chord > MaxLinearErrorUm) return false;
        if (!TryReadDouble(entry, "spanErrorUm", out double span) || span < 0 || span > MaxLinearErrorUm) return false;
        if (!TryReadDouble(entry, "thicknessErrorUm", out double thick) || thick < 0 || thick > MaxLinearErrorUm) return false;
        if (!TryReadDouble(entry, "trailingEdgeErrorUm", out double te) || te < 0 || te > MaxTrailingEdgeErrorUm) return false;
        if (!TryReadDouble(entry, "volumeErrorPercent", out double vol) || vol < 0 || vol > MaxVolumeErrorPercent) return false;
        if (!TryReadInt(entry, "repairWarnings", out int warnings) || warnings != 0) return false;

        return true;
    }

    private static bool ReadBool(JsonElement obj, string prop) =>
        obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.True;

    private static bool ReadNonEmptyString(JsonElement obj, string prop) =>
        obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString());

    private static bool TryReadDouble(JsonElement obj, string prop, out double value)
    {
        value = 0;
        return obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out value) && double.IsFinite(value);
    }

    private static bool TryReadInt(JsonElement obj, string prop, out int value)
    {
        value = 0;
        return obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value);
    }

    private static string? ResolveReceiptPath(string startDir)
    {
        string path = Path.Combine(startDir, DefaultReceiptRelativePath);
        return File.Exists(path) ? path : null;
    }
}
