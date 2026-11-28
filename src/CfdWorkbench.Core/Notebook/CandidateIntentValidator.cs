using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Core.Notebook;

/// <summary>
/// Validated typed proposal emitted by the Reasoning model for a notebook candidate
/// (<c>docs/design/ai-guided-design-notebook.md §1.1 &amp; §3.2</c>). Zero raw wing coordinates or forces;
/// embeds chunked <c>&lt;= 1,024 B</c> <c>DatImport</c> coordinate snapshots whenever a <c>My sections</c> profile is referenced
/// so <c>SurfaceHash_Rebuilt == SurfaceHash_Recorded</c> holds deterministically across machines.
/// Includes optional Wave 13A rounded-tip seam fields.
/// </summary>
public sealed record CandidateIntentDelta(
    string DeltaHash,
    double SpanMeters,
    double AreaSquareMeters,
    double Taper,
    double RootThicknessRatio,
    double AnhedralDeg,
    double WashoutDeg,
    double SweepQuarterChordDeg,
    string RootProfileId,
    string TipProfileId,
    IReadOnlyList<AssistantFieldProvenance> Provenance,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? CustomRootProfileSnapshotLines = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? CustomTipProfileSnapshotLines = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? TipLeRadiusMm = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? TipTeRadiusMm = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TipStyle = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? TipCapRatio = null)
{
    public double AspectRatio => AreaSquareMeters > 0.0 ? (SpanMeters * SpanMeters) / AreaSquareMeters : 0.0;
    public double RootChordMeters => SpanMeters > 0.0 ? AreaSquareMeters / (SpanMeters * RecipeSeeder.Kappa(Taper)) : 0.0;
    public double TipChordMeters => RootChordMeters * Taper;
    public double MacMeters => RecipeSeeder.Mac(RootChordMeters, Taper);

    public static string ComputeDeltaHash(CandidateIntentDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);
        var obj = new Dictionary<string, object?>
        {
            ["spanMeters"] = delta.SpanMeters,
            ["areaSquareMeters"] = delta.AreaSquareMeters,
            ["taper"] = delta.Taper,
            ["rootThicknessRatio"] = delta.RootThicknessRatio,
            ["anhedralDeg"] = delta.AnhedralDeg,
            ["washoutDeg"] = delta.WashoutDeg,
            ["sweepQuarterChordDeg"] = delta.SweepQuarterChordDeg,
            ["rootProfileId"] = delta.RootProfileId,
            ["tipProfileId"] = delta.TipProfileId,
            ["customRootSnapshot"] = delta.CustomRootProfileSnapshotLines is null ? null : string.Concat(delta.CustomRootProfileSnapshotLines),
            ["customTipSnapshot"] = delta.CustomTipProfileSnapshotLines is null ? null : string.Concat(delta.CustomTipProfileSnapshotLines),
            ["tipLeRadiusMm"] = delta.TipLeRadiusMm,
            ["tipTeRadiusMm"] = delta.TipTeRadiusMm,
            ["tipStyle"] = delta.TipStyle,
            ["tipCapRatio"] = delta.TipCapRatio
        };
        return Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(obj)));
    }
}

public sealed record CandidateValidationContext(
    IReadOnlySet<string>? AdmittedMySectionIds = null,
    IReadOnlyDictionary<string, string>? MySectionDatSnapshots = null,
    double? HardMaxSpanMeters = null);

public sealed record CandidateIntentValidationResult(
    bool IsValid,
    string Reason,
    string Message,
    CandidateIntentDelta? Delta);

/// <summary>
/// Deterministic Tier-0 validator for <c>candidate-intent-delta</c> JSON proposals
/// (<c>docs/design/ai-guided-design-notebook.md §3.2</c>). Reuses <c>AssistantValidators.TryParseStrictObject</c>
/// and <c>AssistantValidators.ForbiddenGeometryOrPhysicsFields</c>; rejects raw coordinates, forces, disabled VEND/LINK IDs,
/// unknown keys, and out-of-range values without clamping.
/// </summary>
public static class CandidateIntentValidator
{
    public const double MinSpanMeters = 0.35;
    public const double MaxSpanMeters = 2.20;
    public const double MinAreaSquareMeters = 0.030;
    public const double MaxAreaSquareMeters = 0.350;
    public const double MinTaper = 0.15;
    public const double MaxTaper = 0.95;
    public const double MinRootThicknessRatio = 0.06;
    public const double MaxRootThicknessRatio = 0.20;
    public const double MinAnhedralDeg = -15.0;
    public const double MaxAnhedralDeg = 10.0;
    public const double MinWashoutDeg = -6.0;
    public const double MaxWashoutDeg = 4.0;
    public const double MinSweepDeg = -10.0;
    public const double MaxSweepDeg = 30.0;

    private static readonly HashSet<string> AllowedRootFields = new(StringComparer.Ordinal)
    {
        "kind", "spanMeters", "areaSquareMeters", "taper", "rootThicknessRatio",
        "anhedralDeg", "washoutDeg", "sweepQuarterChordDeg",
        "rootProfileId", "tipProfileId", "provenance",
        "tipLeRadiusMm", "tipTeRadiusMm", "tipStyle", "tipCapRatio"
    };

    public static CandidateIntentValidationResult Validate(string rawJson, CandidateValidationContext? context = null)
    {
        context ??= new CandidateValidationContext();
        if (!AssistantValidators.TryParseStrictObject(rawJson, out var doc, out var parseErr))
            return new CandidateIntentValidationResult(false, parseErr!.Reason, parseErr.Message, null);

        using (doc!)
        {
            var root = doc.RootElement;

            // 1. Recursive forbidden-field check (raw coordinates, meshes, forces, cl/cd, scores).
            if (FindForbiddenFieldRecursive(root) is { } forbidden)
                return new CandidateIntentValidationResult(false, "NB-COORD-FORBIDDEN", $"Proposal refused: field '{forbidden}' is forbidden in candidate-intent-delta.", null);

            foreach (var prop in root.EnumerateObject())
            {
                if (!AllowedRootFields.Contains(prop.Name))
                    return new CandidateIntentValidationResult(false, "extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in candidate-intent-delta.", null);
            }

            if (!AssistantValidators.TryGetRequiredString(root, "kind", out string kind) || kind != "candidate-intent-delta")
                return new CandidateIntentValidationResult(false, "invalid-kind", "Proposal refused: kind must be 'candidate-intent-delta'.", null);

            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "spanMeters", MinSpanMeters, MaxSpanMeters, out double spanM, out var e1))
                return new CandidateIntentValidationResult(false, e1.Reason, e1.Msg, null);
            if (context.HardMaxSpanMeters is { } maxSpan && spanM > maxSpan + 1e-6)
                return new CandidateIntentValidationResult(false, "out-of-range:spanMeters",
                    string.Create(CultureInfo.InvariantCulture, $"Proposal refused: spanMeters={spanM:0.####} exceeds hard max_span {maxSpan:0.####} m."), null);

            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "areaSquareMeters", MinAreaSquareMeters, MaxAreaSquareMeters, out double areaM2, out var e2))
                return new CandidateIntentValidationResult(false, e2.Reason, e2.Msg, null);
            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "taper", MinTaper, MaxTaper, out double taper, out var e3))
                return new CandidateIntentValidationResult(false, e3.Reason, e3.Msg, null);
            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "rootThicknessRatio", MinRootThicknessRatio, MaxRootThicknessRatio, out double rootTc, out var e4))
                return new CandidateIntentValidationResult(false, e4.Reason, e4.Msg, null);
            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "anhedralDeg", MinAnhedralDeg, MaxAnhedralDeg, out double anhedral, out var e5))
                return new CandidateIntentValidationResult(false, e5.Reason, e5.Msg, null);
            if (!AssistantValidators.TryReadRequiredBoundedDouble(root, "washoutDeg", MinWashoutDeg, MaxWashoutDeg, out double washout, out var e6))
                return new CandidateIntentValidationResult(false, e6.Reason, e6.Msg, null);
            if (!AssistantValidators.TryReadOptionalBoundedDouble(root, "sweepQuarterChordDeg", MinSweepDeg, MaxSweepDeg, out double? sweepOpt, out var e7))
                return new CandidateIntentValidationResult(false, e7.Reason, e7.Msg, null);
            double sweep = sweepOpt ?? 0.0;

            if (!AssistantValidators.TryGetRequiredString(root, "rootProfileId", out string rootProfileId))
                return new CandidateIntentValidationResult(false, "missing-root-profile", "Proposal refused: rootProfileId is required.", null);
            if (!AssistantValidators.TryGetRequiredString(root, "tipProfileId", out string tipProfileId))
                return new CandidateIntentValidationResult(false, "missing-tip-profile", "Proposal refused: tipProfileId is required.", null);

            if (!TryResolveAdmittedProfile(rootProfileId, context, out var rootSnapLines, out string? rootErr))
                return new CandidateIntentValidationResult(false, "CAT-UNAVAILABLE", rootErr!, null);
            if (!TryResolveAdmittedProfile(tipProfileId, context, out var tipSnapLines, out string? tipErr))
                return new CandidateIntentValidationResult(false, "CAT-UNAVAILABLE", tipErr!, null);

            // Optional Wave 13A rounded-tip seam fields.
            if (!AssistantValidators.TryReadOptionalBoundedDouble(root, "tipLeRadiusMm", 1.0, 200.0, out double? tipLeRadiusMm, out var e8))
                return new CandidateIntentValidationResult(false, e8.Reason, e8.Msg, null);
            if (!AssistantValidators.TryReadOptionalBoundedDouble(root, "tipTeRadiusMm", 1.0, 200.0, out double? tipTeRadiusMm, out var e9))
                return new CandidateIntentValidationResult(false, e9.Reason, e9.Msg, null);
            if (!AssistantValidators.TryReadOptionalBoundedDouble(root, "tipCapRatio", TipCapMath.MinCapRatio, TipCapMath.MaxCapRatio, out double? tipCapRatio, out var e10))
                return new CandidateIntentValidationResult(false, e10.Reason, e10.Msg, null);

            string? tipStyle = null;
            if (root.TryGetProperty("tipStyle", out var tsEl) && tsEl.ValueKind != JsonValueKind.Null)
            {
                if (tsEl.ValueKind != JsonValueKind.String)
                    return new CandidateIntentValidationResult(false, "invalid-field:tipStyle", "Proposal refused: tipStyle must be 'open' or 'rounded'.", null);
                tipStyle = tsEl.GetString()!.Trim().ToLowerInvariant();
                if (tipStyle is not ("open" or "rounded"))
                    return new CandidateIntentValidationResult(false, "out-of-range:tipStyle", $"Proposal refused: tipStyle '{tipStyle}' must be 'open' or 'rounded'.", null);
            }

            if (!AssistantValidators.TryParseProvenance(root, out var provenance, out string provErr))
                return new CandidateIntentValidationResult(false, "invalid-provenance", provErr, null);

            // Check TipChord floor early so impossible taper/chord combinations are flagged cleanly (Ruling 93).
            double rootChordM = areaM2 / (spanM * RecipeSeeder.Kappa(taper));
            double tipChordM = rootChordM * taper;
            if (!TipChord.Meets(tipChordM, rootChordM))
            {
                return new CandidateIntentValidationResult(
                    false,
                    TipChord.RefusalCode,
                    TipChord.TypedRefusalReason(rootChordM),
                    null);
            }

            var draft = new CandidateIntentDelta(
                DeltaHash: "",
                SpanMeters: spanM,
                AreaSquareMeters: areaM2,
                Taper: taper,
                RootThicknessRatio: rootTc,
                AnhedralDeg: anhedral,
                WashoutDeg: washout,
                SweepQuarterChordDeg: sweep,
                RootProfileId: rootProfileId,
                TipProfileId: tipProfileId,
                Provenance: provenance,
                CustomRootProfileSnapshotLines: rootSnapLines,
                CustomTipProfileSnapshotLines: tipSnapLines,
                TipLeRadiusMm: tipLeRadiusMm,
                TipTeRadiusMm: tipTeRadiusMm,
                TipStyle: tipStyle,
                TipCapRatio: tipCapRatio);

            var finalized = draft with { DeltaHash = CandidateIntentDelta.ComputeDeltaHash(draft) };
            return new CandidateIntentValidationResult(true, "ok", "Candidate intent delta validated.", finalized);
        }
    }

    private static bool TryResolveAdmittedProfile(
        string profileId,
        CandidateValidationContext ctx,
        out IReadOnlyList<string>? snapshotLines,
        out string? error)
    {
        snapshotLines = null;
        error = null;

        // First check admitted bundled GEN catalog (rejects disabled VEND/LINK entries!).
        var catalog = Catalog.Load();
        var match = catalog.FirstOrDefault(e => string.Equals(e.Id, profileId, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            if (match.DisabledReason is not null || match.Coordinates is null)
            {
                error = $"Proposal refused (CAT-UNAVAILABLE): profile '{profileId}' is disabled or not admitted ({match.DisabledReason ?? "no coordinates"}).";
                return false;
            }
            return true;
        }

        // Next check admitted My sections + deterministic coordinate snapshot.
        bool inMySections = (ctx.AdmittedMySectionIds is not null && ctx.AdmittedMySectionIds.Contains(profileId)) ||
                            (ctx.MySectionDatSnapshots is not null && ctx.MySectionDatSnapshots.ContainsKey(profileId));
        if (!inMySections)
        {
            error = $"Proposal refused (CAT-UNAVAILABLE): profile '{profileId}' is not an admitted GEN catalog or My sections profile.";
            return false;
        }

        if (ctx.MySectionDatSnapshots is not null && ctx.MySectionDatSnapshots.TryGetValue(profileId, out string? datText) && !string.IsNullOrWhiteSpace(datText))
        {
            // Verify the snapshot parses cleanly as a valid .dat before embedding.
            _ = DatImport.Parse(Encoding.UTF8.GetBytes(datText));
            snapshotLines = NotebookRecords.ChunkText(datText);
        }
        return true;
    }

    private static string? FindForbiddenFieldRecursive(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject())
                {
                    if (AssistantValidators.ForbiddenGeometryOrPhysicsFields.Contains(p.Name))
                        return p.Name;
                    if (FindForbiddenFieldRecursive(p.Value) is { } nested)
                        return nested;
                }
                return null;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                {
                    if (FindForbiddenFieldRecursive(item) is { } nested)
                        return nested;
                }
                return null;
            default:
                return null;
        }
    }
}
