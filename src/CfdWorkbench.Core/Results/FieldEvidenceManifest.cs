using System.Globalization;
using System.Text.Json.Serialization;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Core.Results;

/// <summary>All 10 Area 6 field/diagnostic visualization layers (spec A5.11, docs/design/results-and-candidates.md §2).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FieldLayerKind>))]
public enum FieldLayerKind
{
    SurfaceCp,
    SurfaceCf,
    SliceVelocity,
    Streamlines3D,
    TurbulentKineticEnergy,
    ForceVectors,
    SpanwiseLoading,
    VortexCoreCandidates,
    Separation,
    Pathlines
}

/// <summary>Derived availability, colormap policy, and honest physical basis label for one visualization layer.</summary>
public sealed record FieldLayerDescriptor(
    FieldLayerKind Kind,
    bool Available,
    string ColormapPolicy,
    string BasisLabel,
    string? UnavailableReason,
    string? ThresholdLabel = null,
    string? SeparationCriterion = null,
    string? MovingDashLabel = null);

/// <summary>Deterministic offline AI-10 answer for separation queries grounded strictly in <see cref="FieldEvidenceManifest"/>.</summary>
public sealed record AskSeparationAnswer(
    string RunId,
    bool SeparationSupported,
    string Answer,
    string? Criterion,
    IReadOnlyList<string> CitedLayers);

/// <summary>
/// Pure zero-I/O manifest deriving every Area 6 visualization layer's availability and VIZ truth label from a sample's
/// harvested <see cref="FieldEvidenceRecord"/> list (or local VLM/strip evidence). Strictly enforces the spec A5.11
/// HARD VETO: Separation is enabled ONLY when wall shear stress (<c>tau_w</c>) exists on a wall surface patch together
/// with a supported named criterion — Q/lambda2 isosurfaces are strictly "vortex-core candidates" and NEVER substitute.
/// </summary>
public sealed record FieldEvidenceManifest(
    string RunId,
    string? MeshHash,
    string SourceBasis,
    IReadOnlyList<FieldLayerDescriptor> Layers)
{
    public const string SeparationUnavailableReason = "No supported criterion in this sample";
    public const string StreamlinesSteadyRansLabel = "instantaneous integral curves of the mean field (steady RANS)";
    public const string StreamlinesMovingDashLabel = "affordance, not fluid motion";
    public const string TurbulentKineticEnergyLabel = "modeled turbulent kinetic energy, m²/s², model, mean-flow basis";
    public const string ForceVectorsLabel = "Body axes: +x aft, +y starboard, +z up; lift and drag in wind axes";
    public const string SpanwiseLoadingLabel = "Spanwise loading Cl·c/c̄ vs η";
    public const string SliceVelocityLabel = "in-plane velocity magnitude, m/s · steady RANS mean field";
    public const string PathlinesTransientLabel = "physical-time pathlines (transient)";
    public const string PathlinesSteadyUnavailable = "Unavailable — steady RANS has no physical-time pathlines";
    public const string SkinFrictionConvergenceCriterion = "skin-friction-line convergence";
    public const string CfxSignChangeCriterion = "C_f,x sign change on this cut";

    private static readonly HashSet<string> SupportedSeparationCriteria = new(StringComparer.Ordinal)
    {
        SkinFrictionConvergenceCriterion,
        CfxSignChangeCriterion
    };

    public static bool IsSupportedSeparationCriterion(string? criterion) =>
        criterion is not null && SupportedSeparationCriteria.Contains(criterion.Trim());

    public static string FormatVortexCoreLabel(double? threshold)
    {
        string t = threshold is { } value && double.IsFinite(value)
            ? value.ToString("G", CultureInfo.InvariantCulture)
            : "t";
        return $"vortex-core candidates at threshold {t}";
    }

    public static string FormatSeparationLabel(string criterion) => $"Separation — criterion: {criterion}";

    public FieldLayerDescriptor ResolveLayer(FieldLayerKind kind)
    {
        for (int i = 0; i < Layers.Count; i++)
        {
            if (Layers[i].Kind == kind) return Layers[i];
        }
        throw new ContractError("RES-LAYER-UNKNOWN", $"Unknown field layer '{kind}'.");
    }

    /// <summary>Builds a complete 10-layer manifest strictly derived from harvested Area 5 <see cref="FieldEvidenceRecord"/> rows.</summary>
    public static FieldEvidenceManifest BuildManifest(
        string runId,
        IReadOnlyList<FieldEvidenceRecord> evidence,
        bool hasForceVectors = true,
        bool hasSpanwiseLoading = false,
        double? vortexThreshold = null)
    {
        Guard.Require(!string.IsNullOrWhiteSpace(runId), "RES-MANIFEST-RUNID");
        var items = evidence ?? [];
        string? meshHash = items.Select(e => e.MeshHash).FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));

        bool hasSurfaceCp = items.Any(e => e.Kind == "surface" && HasField(e, "Cp"));
        bool hasSurfaceCf = items.Any(e => e.Kind == "surface" && (HasField(e, "Cf") || e.HasWallShearStress || HasField(e, "wallShearStress")));
        bool hasSliceVelocity = items.Any(e => e.Kind == "slice" && HasField(e, "U"));
        bool hasStreamlines = items.Any(e => e.Kind == "rake" && HasField(e, "U"));
        bool hasTke = items.Any(e => e.HasTurbulentKineticEnergy || HasField(e, "k"));
        bool hasVortexIso = items.Any(e => e.Kind == "isosurface" && (HasField(e, "Q") || HasField(e, "lambda2")));

        // HARD VETO (spec A5.11): Separation enabled ONLY when wall tau_w exists on wall surface patch + supported named criterion.
        string? sepCriterion = items
            .Where(e => e.Kind == "surface" && e.HasWallShearStress && IsSupportedSeparationCriterion(e.SeparationCriterion))
            .Select(e => e.SeparationCriterion!.Trim())
            .FirstOrDefault();
        bool hasSeparation = sepCriterion is not null;

        bool hasPathlines = items.Any(e => e.IsTransient && e.Timesteps.Count >= 2);
        string vortexLabel = FormatVortexCoreLabel(vortexThreshold);

        var layers = new FieldLayerDescriptor[]
        {
            new(
                FieldLayerKind.SurfaceCp,
                hasSurfaceCp,
                "vik pinned at 0",
                "Cp · dimensionless · face/panel",
                hasSurfaceCp ? null : "Unavailable — Cp field not present in this sample"),
            new(
                FieldLayerKind.SurfaceCf,
                hasSurfaceCf,
                "batlow",
                "C_f · dimensionless · wall patch",
                hasSurfaceCf ? null : "Unavailable — C_f field not present in this sample"),
            new(
                FieldLayerKind.SliceVelocity,
                hasSliceVelocity,
                "batlow",
                SliceVelocityLabel,
                hasSliceVelocity ? null : "Unavailable — velocity slice not computed (requires ParaView 5.12+ or slice harvest)"),
            new(
                FieldLayerKind.Streamlines3D,
                hasStreamlines,
                "batlow",
                StreamlinesSteadyRansLabel,
                hasStreamlines ? null : "Unavailable — streamlines not precomputed for this sample",
                MovingDashLabel: StreamlinesMovingDashLabel),
            new(
                FieldLayerKind.TurbulentKineticEnergy,
                hasTke,
                "batlow",
                TurbulentKineticEnergyLabel,
                hasTke ? null : "Unavailable — k not modelled by this turbulence model"),
            new(
                FieldLayerKind.ForceVectors,
                hasForceVectors,
                "PrimaryBrush",
                ForceVectorsLabel,
                hasForceVectors ? null : "Unavailable — force vectors not recorded"),
            new(
                FieldLayerKind.SpanwiseLoading,
                hasSpanwiseLoading,
                "batlow",
                SpanwiseLoadingLabel,
                hasSpanwiseLoading ? null : "Unavailable — spanwise loading not recorded"),
            new(
                FieldLayerKind.VortexCoreCandidates,
                hasVortexIso,
                "batlow",
                vortexLabel,
                hasVortexIso ? null : "Unavailable — Q/λ2 isosurface not present in this sample",
                ThresholdLabel: vortexLabel),
            new(
                FieldLayerKind.Separation,
                hasSeparation,
                "DangerBrush",
                hasSeparation ? FormatSeparationLabel(sepCriterion!) : SeparationUnavailableReason,
                hasSeparation ? null : SeparationUnavailableReason,
                SeparationCriterion: hasSeparation ? sepCriterion : null),
            new(
                FieldLayerKind.Pathlines,
                hasPathlines,
                "batlow",
                PathlinesTransientLabel,
                hasPathlines ? null : PathlinesSteadyUnavailable)
        };

        return new FieldEvidenceManifest(runId, meshHash, "cfd", layers);
    }

    /// <summary>Builds a manifest for a local VLM + strip calculation; Separation is strictly disabled (no wall tau_w).</summary>
    public static FieldEvidenceManifest BuildLocalVlmManifest(
        string runId,
        bool hasPanelCp,
        bool hasForceVectors,
        bool hasSpanwiseLoading)
    {
        Guard.Require(!string.IsNullOrWhiteSpace(runId), "RES-MANIFEST-RUNID");
        var layers = new FieldLayerDescriptor[]
        {
            new(
                FieldLayerKind.SurfaceCp,
                hasPanelCp,
                "vik pinned at 0",
                "Cp · dimensionless · face/panel",
                hasPanelCp ? null : "Unavailable — Cp field not present in this sample"),
            new(
                FieldLayerKind.SurfaceCf,
                false,
                "batlow",
                "C_f · dimensionless · wall patch",
                "Unavailable — C_f field not present in this sample"),
            new(
                FieldLayerKind.SliceVelocity,
                false,
                "batlow",
                SliceVelocityLabel,
                "Unavailable — velocity slice not computed (requires ParaView 5.12+ or slice harvest)"),
            new(
                FieldLayerKind.Streamlines3D,
                false,
                "batlow",
                StreamlinesSteadyRansLabel,
                "Unavailable — streamlines not precomputed for this sample",
                MovingDashLabel: StreamlinesMovingDashLabel),
            new(
                FieldLayerKind.TurbulentKineticEnergy,
                false,
                "batlow",
                TurbulentKineticEnergyLabel,
                "Unavailable — k not modelled by this turbulence model"),
            new(
                FieldLayerKind.ForceVectors,
                hasForceVectors,
                "PrimaryBrush",
                ForceVectorsLabel,
                hasForceVectors ? null : "Unavailable — force vectors not recorded"),
            new(
                FieldLayerKind.SpanwiseLoading,
                hasSpanwiseLoading,
                "batlow",
                SpanwiseLoadingLabel,
                hasSpanwiseLoading ? null : "Unavailable — spanwise loading not recorded"),
            new(
                FieldLayerKind.VortexCoreCandidates,
                false,
                "batlow",
                FormatVortexCoreLabel(null),
                "Unavailable — Q/λ2 isosurface not present in this sample"),
            new(
                FieldLayerKind.Separation,
                false,
                "DangerBrush",
                SeparationUnavailableReason,
                SeparationUnavailableReason),
            new(
                FieldLayerKind.Pathlines,
                false,
                "batlow",
                PathlinesTransientLabel,
                PathlinesSteadyUnavailable)
        };

        return new FieldEvidenceManifest(runId, null, "local calculation", layers);
    }

    /// <summary>
    /// Offline key-free AI-10 separation validator: when <see cref="FieldLayerKind.Separation"/> is unavailable,
    /// strictly returns <see cref="SeparationUnavailableReason"/> and never infers separation from Q/lambda2.
    /// </summary>
    public static AskSeparationAnswer EvaluateAskAboutSeparation(FieldEvidenceManifest manifest, string question)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        _ = question;
        var sep = manifest.ResolveLayer(FieldLayerKind.Separation);
        var cited = manifest.Layers
            .Select(l => $"{l.Kind}: {(l.Available ? "available" : "unavailable")}")
            .ToArray();

        if (!sep.Available || string.IsNullOrWhiteSpace(sep.SeparationCriterion))
        {
            return new AskSeparationAnswer(
                RunId: manifest.RunId,
                SeparationSupported: false,
                Answer: SeparationUnavailableReason,
                Criterion: null,
                CitedLayers: cited);
        }

        return new AskSeparationAnswer(
            RunId: manifest.RunId,
            SeparationSupported: true,
            Answer: $"{FormatSeparationLabel(sep.SeparationCriterion)} (run {manifest.RunId}).",
            Criterion: sep.SeparationCriterion,
            CitedLayers: cited);
    }

    private static bool HasField(FieldEvidenceRecord record, string fieldName) =>
        record.Fields.Any(f => string.Equals(f, fieldName, StringComparison.Ordinal));
}
