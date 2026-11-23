using System.Globalization;
using CfdWorkbench.Core.Results;

namespace CfdWorkbench.Analysis.Results;

/// <summary>One scalar sample placed on the wing surface from <c>Placement.Surface</c> (ADR-0010).</summary>
public sealed record SurfaceScalarNode(
    double Eta,
    double X,
    double Y,
    double Z,
    double? Value);

/// <summary>Surface Cp/Cf scalar projection over <c>Placement.Surface</c> with 9-field provenance and linked table twin.</summary>
public sealed record SurfaceFieldProjection(
    string RunId,
    string Variable,
    string Unit,
    string SurfaceHash,
    double? MinValue,
    double? MaxValue,
    IReadOnlyList<SurfaceScalarNode> Nodes,
    ProvenanceStrip Provenance,
    TableTwin TableTwin);

/// <summary>
/// One unified Area 6 result sample (local VLM/Sweep or Area 5 CFD). Tiers stay strictly separate and are NEVER averaged.
/// </summary>
public sealed record UnifiedResultSample(
    string SampleId,
    string RunId,
    string Tier,
    string SourceKind,
    string MethodTierAndVersion,
    double AlphaDeg,
    double SpeedMs,
    double? Cl,
    double? Cd,
    double? Cm,
    string? MeshHash,
    string SurfaceHash,
    bool Admitted,
    string StatusLabel,
    string EvidenceLabel,
    FieldEvidenceManifest Manifest,
    ProvenanceStrip Provenance,
    TableTwin TableTwin,
    SampleFieldData? SurfaceCpData,
    SampleFieldData? SurfaceCfData);

/// <summary>One sweep small-multiple summary tile for Area 6 ResultsWorkspaceView.</summary>
public sealed record SmallMultipleTile(
    string RunId,
    string Tier,
    double AlphaDeg,
    double SpeedMs,
    string Label,
    bool Admitted,
    string? FailureReason,
    double? Cl,
    double? Cd,
    ProvenanceStrip Provenance);

/// <summary>Complete Area 6 Results workspace projection consumed by Wave 10D ResultsWorkspaceView and CLI.</summary>
public sealed record ResultsWorkspaceModel(
    string CurrentAcceptedId,
    string CurrentSurfaceHash,
    IReadOnlyList<UnifiedResultSample> Samples,
    IReadOnlyList<SmallMultipleTile> SmallMultiples,
    CandidateLadderView CandidateLadder,
    ParaViewAvailability ParaView,
    string NoCrossTierAveragingNote);

/// <summary>
/// Read-only projection helpers for Area 6 ResultsWorkspaceView and CLI (docs/design/results-and-candidates.md §1–§5).
/// Enforces no cross-tier averaging, 9-field ProvenanceStrip completeness, and exact linked TableTwin parity.
/// </summary>
public static class ResultsProjection
{
    public static IReadOnlyList<SmallMultipleTile> BuildSmallMultiples(IReadOnlyList<UnifiedResultSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var tiles = new List<SmallMultipleTile>(samples.Count);

        foreach (var sample in samples.OrderBy(s => s.Tier, StringComparer.Ordinal).ThenBy(s => s.SpeedMs).ThenBy(s => s.AlphaDeg))
        {
            string label = string.Create(
                CultureInfo.InvariantCulture,
                $"{sample.Tier} · α {sample.AlphaDeg:0.#}° · V {sample.SpeedMs:0.##} m/s");
            tiles.Add(new SmallMultipleTile(
                RunId: sample.RunId,
                Tier: sample.Tier,
                AlphaDeg: sample.AlphaDeg,
                SpeedMs: sample.SpeedMs,
                Label: label,
                Admitted: sample.Admitted,
                FailureReason: sample.Admitted ? null : sample.StatusLabel,
                Cl: sample.Admitted ? sample.Cl : null,
                Cd: sample.Admitted ? sample.Cd : null,
                Provenance: sample.Provenance));
        }

        return tiles;
    }

    public static string FormatDepthBasis(double? hRefMeters, string? datum)
    {
        if (hRefMeters is not { } h || !double.IsFinite(h))
            return "depth not set · free surface not modelled";
        string cleanDatum = string.IsNullOrWhiteSpace(datum) ? "root LE" : datum.Trim();
        return string.Create(CultureInfo.InvariantCulture, $"h_ref = {h:0.00} m ({cleanDatum})");
    }

    public static string FormatRunSampleLabel(string runId, double alphaDeg, double speedMs)
    {
        string shortId = runId.Length >= 12 ? runId[..12] : runId;
        return string.Create(CultureInfo.InvariantCulture, $"run {shortId} · α {alphaDeg:0.0}° · V {speedMs:0.00} m/s");
    }
}
