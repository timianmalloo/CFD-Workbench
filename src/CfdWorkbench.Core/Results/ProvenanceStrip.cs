using System.Globalization;
using System.Text.Json.Serialization;

namespace CfdWorkbench.Core.Results;

/// <summary>One row of the linked table twin sharing the exact data source as a visual plot or field canvas.</summary>
public sealed record TableTwinRow(
    int Index,
    string Location,
    string Variable,
    string Unit,
    double? Value,
    string DisplayValue,
    string StatusFlag);

/// <summary>Linked tabular twin for a Results view; missing/masked values never substitute 0.0 (spec A5.11, RES-01).</summary>
public sealed record TableTwin(
    string Variable,
    string Unit,
    IReadOnlyList<TableTwinRow> Rows);

/// <summary>
/// 9-field required provenance strip carried by every Area 6 Results view (spec A5.11, docs/design/results-and-candidates.md §3).
/// Every required field is strictly non-empty; unmeasured values format explicitly as <see cref="NotRecorded"/>.
/// </summary>
public sealed record ProvenanceStrip(
    string Variable,
    string Unit,
    string RangeWithBasis,
    string MethodTierAndVersion,
    string SourceRunAndSample,
    string DepthBasis,
    string EvidenceLabel,
    string ColormapNameAndVersion,
    string AssociationAndTimeBasis,
    int TableTwinRowCount = 0)
{
    public const string NotRecorded = "Not recorded";
    public const string VikCrameri = "vik · Crameri 8.0";
    public const string BatlowCrameri = "batlow · Crameri 8.0";

    /// <summary>Alias matching the brief's short name while keeping canonical JSON key <c>colormapNameAndVersion</c>.</summary>
    [JsonIgnore]
    public string MapNameAndVersion => ColormapNameAndVersion;

    public static string NormalizeField(string? value) =>
        string.IsNullOrWhiteSpace(value) ? NotRecorded : value.Trim();

    public static string FormatRange(double? min, double? max, string basis)
    {
        if (min is not { } lo || max is not { } hi || !double.IsFinite(lo) || !double.IsFinite(hi))
            return NotRecorded;
        string basisClean = NormalizeField(basis);
        return string.Create(CultureInfo.InvariantCulture, $"[{lo:0.00}, {hi:0.00}] · {basisClean}");
    }

    public static ProvenanceStrip BuildProvenance(
        string? variable,
        string? unit,
        string? rangeWithBasis,
        string? methodTierAndVersion,
        string? sourceRunAndSample,
        string? depthBasis,
        string? evidenceLabel,
        string? colormapNameAndVersion,
        string? associationAndTimeBasis,
        int tableTwinRowCount = 0)
    {
        Guard.Require(tableTwinRowCount >= 0, "RES-PROVENANCE-ROWS");
        return new ProvenanceStrip(
            Variable: NormalizeField(variable),
            Unit: NormalizeField(unit),
            RangeWithBasis: NormalizeField(rangeWithBasis),
            MethodTierAndVersion: NormalizeField(methodTierAndVersion),
            SourceRunAndSample: NormalizeField(sourceRunAndSample),
            DepthBasis: NormalizeField(depthBasis),
            EvidenceLabel: NormalizeField(evidenceLabel),
            ColormapNameAndVersion: NormalizeField(colormapNameAndVersion),
            AssociationAndTimeBasis: NormalizeField(associationAndTimeBasis),
            TableTwinRowCount: tableTwinRowCount);
    }

    /// <summary>Verifies all 9 spec A5.11 provenance strings are non-null and non-whitespace.</summary>
    public static bool HasAllNineFields(ProvenanceStrip? strip) =>
        strip is not null &&
        !string.IsNullOrWhiteSpace(strip.Variable) &&
        !string.IsNullOrWhiteSpace(strip.Unit) &&
        !string.IsNullOrWhiteSpace(strip.RangeWithBasis) &&
        !string.IsNullOrWhiteSpace(strip.MethodTierAndVersion) &&
        !string.IsNullOrWhiteSpace(strip.SourceRunAndSample) &&
        !string.IsNullOrWhiteSpace(strip.DepthBasis) &&
        !string.IsNullOrWhiteSpace(strip.EvidenceLabel) &&
        !string.IsNullOrWhiteSpace(strip.ColormapNameAndVersion) &&
        !string.IsNullOrWhiteSpace(strip.AssociationAndTimeBasis);

    /// <summary>
    /// Deterministic linked table twin builder. Finite numeric values format in invariant culture;
    /// null or non-finite values print <c>"Unavailable — &lt;reason&gt;"</c> and NEVER substitute <c>0.0</c>.
    /// </summary>
    public static TableTwin BuildTableTwin(
        string variable,
        string unit,
        IReadOnlyList<(string Location, double? Value, string? UnavailableReason, string? Flag)> entries)
    {
        string cleanVar = NormalizeField(variable);
        string cleanUnit = NormalizeField(unit);
        var list = entries ?? [];
        var rows = new List<TableTwinRow>(list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            var (loc, val, reason, flag) = list[i];
            string cleanLoc = NormalizeField(loc);
            if (val is { } finite && double.IsFinite(finite))
            {
                rows.Add(new TableTwinRow(
                    Index: i,
                    Location: cleanLoc,
                    Variable: cleanVar,
                    Unit: cleanUnit,
                    Value: finite,
                    DisplayValue: finite.ToString("G6", CultureInfo.InvariantCulture),
                    StatusFlag: string.IsNullOrWhiteSpace(flag) ? "OK" : flag.Trim()));
            }
            else
            {
                string why = string.IsNullOrWhiteSpace(reason) ? "value not recorded" : reason.Trim();
                string display = why.StartsWith("Unavailable", StringComparison.Ordinal)
                    ? why
                    : $"Unavailable — {why}";
                rows.Add(new TableTwinRow(
                    Index: i,
                    Location: cleanLoc,
                    Variable: cleanVar,
                    Unit: cleanUnit,
                    Value: null,
                    DisplayValue: display,
                    StatusFlag: string.IsNullOrWhiteSpace(flag) ? "Unavailable" : flag.Trim()));
            }
        }

        return new TableTwin(cleanVar, cleanUnit, rows);
    }
}
