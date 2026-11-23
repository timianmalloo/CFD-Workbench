using System.Globalization;

namespace CfdWorkbench.Core.Results;

/// <summary>Discrete scalar field values of one admitted sample for surface projection or difference-flood computation.</summary>
public sealed record SampleFieldData(
    string RunId,
    string SampleLabel,
    string? MeshHash,
    string Variable,
    string Unit,
    string Association,
    string MethodTierAndVersion,
    string DepthBasis,
    string EvidenceLabel,
    string AssociationAndTimeBasis,
    IReadOnlyList<double> Values,
    IReadOnlyList<string>? Locations = null);

/// <summary>Result of computing a zero-pinned diverging difference flood between two admitted samples.</summary>
public sealed record DifferenceFloodResult(
    bool Available,
    string? UnavailableReason,
    string Variable,
    string Unit,
    string BaseRunId,
    string TargetRunId,
    double? SymmetricAbsMax,
    double? ScaleMin,
    double? ScaleMax,
    IReadOnlyList<double> DeltaValues,
    ProvenanceStrip? Provenance,
    TableTwin? TableTwin);

/// <summary>
/// Pure zero-I/O difference flood calculator (spec A5.11, RES-04, docs/design/results-and-candidates.md §4.2).
/// Strictly enforces the mesh-hash HARD VETO: differing or absent mesh hashes return
/// <c>"Unavailable — mesh differs"</c> with an empty delta array.
/// </summary>
public static class DifferenceFlood
{
    public const string MeshDiffersReason = "Unavailable — mesh differs";
    public const string LayoutDiffersReason = "Unavailable — field layout differs";

    public static DifferenceFloodResult ComputeDifference(
        SampleFieldData baseSample,
        SampleFieldData targetSample,
        string variable)
    {
        ArgumentNullException.ThrowIfNull(baseSample);
        ArgumentNullException.ThrowIfNull(targetSample);
        string cleanVar = ProvenanceStrip.NormalizeField(variable);
        string cleanUnit = ProvenanceStrip.NormalizeField(baseSample.Unit);

        // 1. Mesh-hash guard (HARD VETO).
        if (string.IsNullOrWhiteSpace(baseSample.MeshHash) ||
            string.IsNullOrWhiteSpace(targetSample.MeshHash) ||
            !string.Equals(baseSample.MeshHash, targetSample.MeshHash, StringComparison.Ordinal))
        {
            return Unavailable(MeshDiffersReason, cleanVar, cleanUnit, baseSample.RunId, targetSample.RunId);
        }

        // 2. Variable, association, and element-count guard.
        if (!string.Equals(baseSample.Variable, variable, StringComparison.Ordinal) ||
            !string.Equals(targetSample.Variable, variable, StringComparison.Ordinal) ||
            !string.Equals(baseSample.Association, targetSample.Association, StringComparison.Ordinal) ||
            baseSample.Values is null || targetSample.Values is null ||
            baseSample.Values.Count == 0 ||
            baseSample.Values.Count != targetSample.Values.Count)
        {
            return Unavailable(LayoutDiffersReason, cleanVar, cleanUnit, baseSample.RunId, targetSample.RunId);
        }

        // 3. Zero-pinned diverging difference: delta[i] = target[i] - base[i], range [-absMax, +absMax] on vik · Crameri 8.0.
        int n = baseSample.Values.Count;
        var deltas = new double[n];
        var tableEntries = new List<(string Location, double? Value, string? UnavailableReason, string? Flag)>(n);
        double absMax = 0.0;

        for (int i = 0; i < n; i++)
        {
            double b = baseSample.Values[i];
            double t = targetSample.Values[i];
            if (!double.IsFinite(b) || !double.IsFinite(t))
                throw new ContractError("RES-DIFF-NONFINITE", "Difference flood encountered a non-finite scalar value.");
            double d = t - b;
            deltas[i] = d;
            double ad = Math.Abs(d);
            if (ad > absMax) absMax = ad;

            string loc = baseSample.Locations is { } locs && i < locs.Count
                ? locs[i]
                : string.Create(CultureInfo.InvariantCulture, $"node {i}");
            tableEntries.Add((loc, d, null, "OK"));
        }

        double scaleMin = -absMax;
        double scaleMax = absMax;
        string baseShort = baseSample.RunId.Length >= 8 ? baseSample.RunId[..8] : baseSample.RunId;
        string targetShort = targetSample.RunId.Length >= 8 ? targetSample.RunId[..8] : targetSample.RunId;

        var tableTwin = ProvenanceStrip.BuildTableTwin($"Δ{cleanVar}", cleanUnit, tableEntries);
        var provenance = ProvenanceStrip.BuildProvenance(
            variable: $"Δ{cleanVar}",
            unit: cleanUnit,
            rangeWithBasis: ProvenanceStrip.FormatRange(scaleMin, scaleMax, "zero-pinned difference"),
            methodTierAndVersion: baseSample.MethodTierAndVersion,
            sourceRunAndSample: $"base {baseShort} ({baseSample.SampleLabel}) → target {targetShort} ({targetSample.SampleLabel})",
            depthBasis: baseSample.DepthBasis,
            evidenceLabel: baseSample.EvidenceLabel,
            colormapNameAndVersion: ProvenanceStrip.VikCrameri,
            associationAndTimeBasis: baseSample.AssociationAndTimeBasis,
            tableTwinRowCount: tableTwin.Rows.Count);

        return new DifferenceFloodResult(
            Available: true,
            UnavailableReason: null,
            Variable: $"Δ{cleanVar}",
            Unit: cleanUnit,
            BaseRunId: baseSample.RunId,
            TargetRunId: targetSample.RunId,
            SymmetricAbsMax: absMax,
            ScaleMin: scaleMin,
            ScaleMax: scaleMax,
            DeltaValues: deltas,
            Provenance: provenance,
            TableTwin: tableTwin);
    }

    private static DifferenceFloodResult Unavailable(string reason, string variable, string unit, string baseRunId, string targetRunId) =>
        new(
            Available: false,
            UnavailableReason: reason,
            Variable: $"Δ{variable}",
            Unit: unit,
            BaseRunId: baseRunId,
            TargetRunId: targetRunId,
            SymmetricAbsMax: null,
            ScaleMin: null,
            ScaleMax: null,
            DeltaValues: [],
            Provenance: null,
            TableTwin: null);
}
