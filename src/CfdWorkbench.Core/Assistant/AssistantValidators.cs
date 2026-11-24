using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core.Results;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Core.Assistant;

public sealed record Lab01CopyLintViolation(string Rule, string Snippet);

public sealed record Lab01CopyLintResult(bool Passed, IReadOnlyList<Lab01CopyLintViolation> Violations);

/// <summary>
/// Pure Core LAB-01 forbidden-claim lint (docs/specs/cfd-workbench-v1.md:1229, docs/design/byok-assistants.md §2.3).
/// Rejects unqualified claims ("cavitation-free", "ventilation-safe", "optimized", "best", "recommended", "certified"),
/// unqualified "validated" outside an explicit ITTC U_V statement, and unqualified "Converged" without all three A7 parts.
/// </summary>
public static class Lab01CopyLint
{
    private static readonly Regex ForbiddenWordRegex = new(
        @"(?i)(?<![A-Za-z0-9-])(?:cavitation-free|ventilation-safe|optimized|best|recommended|certified)(?![A-Za-z0-9-])",
        RegexOptions.CultureInvariant);

    private static readonly Regex ValidatedWordRegex = new(
        @"(?i)\bvalidated\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex ConvergedWordRegex = new(
        @"(?i)\bconverged\b",
        RegexOptions.CultureInvariant);

    public static Lab01CopyLintResult CheckText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Lab01CopyLintResult(true, []);

        var violations = new List<Lab01CopyLintViolation>();

        foreach (Match m in ForbiddenWordRegex.Matches(text))
            violations.Add(new("lab01-forbidden-claim", m.Value));

        if (ValidatedWordRegex.IsMatch(text))
        {
            bool hasExplicitPhrase = text.Contains("validated at the U_V level", StringComparison.OrdinalIgnoreCase);
            bool hasIttcAndUv = text.Contains("ITTC", StringComparison.OrdinalIgnoreCase) &&
                                text.Contains("U_V", StringComparison.OrdinalIgnoreCase);
            if (!hasExplicitPhrase && !hasIttcAndUv)
                violations.Add(new("lab01-unqualified-validated", "validated"));
        }

        if (ConvergedWordRegex.IsMatch(text))
        {
            bool hasResiduals = text.Contains("residuals", StringComparison.OrdinalIgnoreCase);
            bool hasOutputsComplete = text.Contains("outputs complete", StringComparison.OrdinalIgnoreCase);
            bool hasGridUncertainty = text.Contains("grid uncertainty", StringComparison.OrdinalIgnoreCase);
            if (!(hasResiduals && hasOutputsComplete && hasGridUncertainty))
                violations.Add(new("lab01-unqualified-converged", "Converged"));
        }

        return new Lab01CopyLintResult(violations.Count == 0, violations);
    }
}

public sealed record SetupSoftTargetsProposal(
    double? MaxSpanMeters,
    double MaxSpanWeight,
    double? MaxChordMm,
    double MaxChordWeight,
    double? TargetAreaCm2,
    double TargetAreaWeight,
    double? TargetAspectRatio,
    double TargetAspectRatioWeight);

public sealed record SetupSeedProposal(
    string Purpose,
    SetupSoftTargetsProposal? SoftTargets,
    double? Taper,
    double? RootThicknessPercent,
    double? AnhedralDeg,
    double? WashoutDeg,
    double? RiderMassKg,
    bool? IsSaltWater,
    double? WaterTemperatureC,
    double? HRefMeters,
    double? MastLengthCm,
    string? ProfileId,
    IReadOnlyList<AssistantFieldProvenance> Provenance);

public enum GeometryEditOpKind
{
    ChannelDelta,
    AddStation,
    RemoveStation,
    AssignProfile,
    LockChange
}

public sealed record GeometryEditOp(
    GeometryEditOpKind Kind,
    string? Channel,
    double? Eta,
    double? DeltaValue,
    string? Unit,
    string? ProfileId,
    int? VertexIndex,
    bool? Locked,
    string? LockSource);

public sealed record GeometryEditDraftSpec(
    string ProposalId,
    string BaseAcceptedId,
    IReadOnlyList<GeometryEditOp> Operations,
    IReadOnlyList<AssistantFieldProvenance> Provenance,
    bool BaseDriftDetected,
    bool AcceptAllowed,
    string? DriftReason);

public sealed record WhatIfOffer(
    double? SpeedMps,
    double? AlphaDeg,
    double? HRefMeters,
    double? WaterTemperatureC,
    bool? IsSaltWater);

public sealed record AnalysisExplanationProposal(
    string Text,
    IReadOnlyList<string> Citations,
    WhatIfOffer? WhatIfOffer);

public sealed record OptimizeObjectiveSpec(
    string Kind,
    string OperatingPointId,
    double Weight);

public sealed record ExperimentConfigProposal(
    string Mode,
    IReadOnlyList<double>? Speeds,
    string? SpeedUnit,
    IReadOnlyList<double>? AnglesDeg,
    double? WaterTemperatureC,
    bool? IsSaltWater,
    double? HRefMeters,
    string? Tier,
    IReadOnlyList<string>? OperatingPointIds,
    IReadOnlyList<OptimizeObjectiveSpec>? Objectives,
    bool IncludeAcavConstraint,
    bool UnfreezeThickness,
    bool HasStructuralProxyRow,
    int? MaxEvaluations,
    int? MaxWallMinutes,
    int? Seed,
    IReadOnlyList<AssistantFieldProvenance> Provenance,
    bool PreviewOnly,
    bool Queued);

public sealed record RunFailureExplanationProposal(
    string Text,
    IReadOnlyList<string> Observations,
    IReadOnlyList<string> Hypotheses,
    IReadOnlyList<string> Citations);

public sealed record CaseDiffChange(
    string Field,
    string ValueDisplay,
    double? NumericValue,
    string? StringValue);

public sealed record CaseDiffProposal(
    string FailedRunId,
    string TargetExperimentId,
    bool CreatesNewDraftVersion,
    IReadOnlyList<CaseDiffChange> Changes,
    IReadOnlyList<AssistantFieldProvenance> Provenance,
    ProposalDisposition Disposition,
    bool TriggersRun);

public sealed record ResultsExplanationProposal(
    string SampleRunId,
    string Text,
    IReadOnlyList<string> Citations,
    bool SeparationLayerAvailable,
    string? SeparationCriterion);

public sealed record AssistantValidationInput(
    AssistantArea Area,
    AssistantProposalKind ProposalKind,
    string? ProposalId = null,
    string? CurrentAcceptedId = null,
    IReadOnlySet<string>? AdmittedMySectionIds = null,
    IReadOnlyList<double>? ExistingStationEtas = null,
    IReadOnlySet<string>? AllowedRunIds = null,
    IReadOnlySet<string>? AllowedQuantityIds = null,
    IReadOnlySet<string>? KnownKnowledgeIds = null,
    IReadOnlyDictionary<int, string>? ExcerptLines = null,
    string? SharedContextText = null,
    string? UserPrompt = null,
    string? SampleRunId = null,
    bool SeparationLayerAvailable = false,
    string? SeparationCriterion = null,
    IReadOnlySet<string>? AvailableLayerIds = null,
    bool TargetExperimentIsQueued = false,
    AssistantValidationContext? SetupContext = null,
    string? FallbackText = null);

public sealed record AssistantResponseValidation(
    AssistantVerdict Verdict,
    ProposalDisposition Disposition,
    string Reason,
    string Message,
    string DisplayText,
    IReadOnlyList<string> Citations,
    SetupSeedProposal? SetupSeed = null,
    GeometryEditDraftSpec? GeometryEdit = null,
    AnalysisExplanationProposal? AnalysisExplanation = null,
    ExperimentConfigProposal? ExperimentConfig = null,
    RunFailureExplanationProposal? RunFailureExplanation = null,
    CaseDiffProposal? CaseDiff = null,
    ResultsExplanationProposal? ResultsExplanation = null,
    string? SuggestedEnvironmentStepId = null,
    bool TriggersRun = false);

/// <summary>
/// Deterministic T0 validation authority across all seven workbench areas (docs/design/byok-assistants.md §2.3).
/// Composes directly with <see cref="SetupAssistantValidator"/> for AI-11 without duplicating it, enforces LAB-01,
/// AI-03 numeral-in-context, AI-10 wall tau_w separation hard veto, strict JSON non-duplicate-key parsing,
/// zero silent clamping, and explicit Area 7 Export refusal.
/// </summary>
public static class AssistantValidators
{
    public const string ExportNoAssistantMessage = "No assistant action here.";
    public const string SinglePointOptimizeRefusal =
        "Single-point optimization fills the design to one condition and degrades the others (Drela 1998; Garg 2017) — add at least a second operating point or accept the multipoint default";
    public const string UnfrozenThicknessRefusal =
        "Thickness is frozen until a structural proxy row or the Beam tier exists — without one, thickness collapses to the floor (Garg 2017)";

    private static readonly JsonDocumentOptions StrictJsonOptions = new()
    {
        MaxDepth = 16,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly HashSet<string> AllowedChannels = new(StringComparer.Ordinal)
    {
        "leading", "trailing", "elevation", "twist", "thickness"
    };

    private static readonly HashSet<string> ForbiddenGeometryOrPhysicsFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "coordinates", "points", "controlVertices", "mesh", "forces", "cl", "cd", "lift", "drag", "score", "scores"
    };

    private static readonly Regex LineCitation = new(@"^line:(\d+)$", RegexOptions.CultureInvariant);
    private static readonly Regex LinesRangeCitation = new(@"^lines:(\d+)-(\d+)$", RegexOptions.CultureInvariant);

    private static readonly Regex NumeralTokenRegex = new(
        @"(?<![A-Za-z0-9_.-])\d+(?:\.\d+)?(?!(?:\.\d|[A-Za-z0-9_-]))",
        RegexOptions.CultureInvariant);

    private static readonly Regex SeparationMentionRegex = new(
        @"(?i)\b(?:separat(?:ion|ed|ing|e)|detach(?:ment|ed|ing)|stall[- ]?line|reattachment|reversed?\s+flow)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex VortexCoreProxyRegex = new(
        @"(?i)(?:\bq[- ]?(?:criterion|isosurface|iso)|lambda\s*2|λ\s*2|vortex[- ]?core)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex CaseDiffMacroRegex = new(
        @"(?i)(?:#codeStream|#calc|#include|#eval|systemCall|foamExec|codedFixedValue|includeEtc|functions\s*\{)",
        RegexOptions.CultureInvariant);

    public static string FormatNoKeyStatus(AssistantArea area) => area == AssistantArea.Export
        ? ExportNoAssistantMessage
        : "No API key configured — every design, analysis, solver setup, run, results, and export tool works without it.";

    public static AssistantResponseValidation ValidateResponse(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Area == AssistantArea.Export || input.ProposalKind == AssistantProposalKind.None)
            return Refused("export-no-assistant", ExportNoAssistantMessage, ExportNoAssistantMessage);

        return input.ProposalKind switch
        {
            AssistantProposalKind.SetupSeed => ValidateSetupSeed(rawJson, input),
            AssistantProposalKind.GeometryEdit => ValidateGeometryEdit(rawJson, input),
            AssistantProposalKind.AnalysisExplanation => ValidateAnalysisExplanation(rawJson, input),
            AssistantProposalKind.ExperimentConfig => ValidateExperimentConfig(rawJson, input),
            AssistantProposalKind.EnvironmentStep => ValidateSolverSetupFromInput(rawJson, input),
            AssistantProposalKind.RunFailureExplanation => ValidateRunFailureExplanation(rawJson, input),
            AssistantProposalKind.CaseDiff => ValidateCaseDiff(rawJson, input),
            AssistantProposalKind.ResultsExplanation => ValidateResultsExplanation(rawJson, input),
            _ => Refused("unsupported-proposal-kind", $"Unsupported proposal kind '{input.ProposalKind}'.", input.FallbackText)
        };
    }

    public static AssistantResponseValidation ValidateSolverSetup(string rawJson, AssistantValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var delegated = SetupAssistantValidator.Validate(rawJson, context);
        if (delegated.Verdict == AssistantVerdict.Shown && delegated.SuggestedStepId is null)
        {
            var lab = Lab01CopyLint.CheckText(delegated.DisplayText);
            if (!lab.Passed)
            {
                return Withheld(
                    "lab01-forbidden-claim",
                    $"Response withheld: forbidden LAB-01 claim '{lab.Violations[0].Snippet}'.",
                    context.FallbackFixedExplanation);
            }
        }

        var disp = delegated.Verdict switch
        {
            AssistantVerdict.Shown => ProposalDisposition.Pending,
            AssistantVerdict.Withheld => ProposalDisposition.Withheld,
            _ => ProposalDisposition.Refused
        };

        return new AssistantResponseValidation(
            Verdict: delegated.Verdict,
            Disposition: disp,
            Reason: delegated.Reason,
            Message: delegated.Message,
            DisplayText: delegated.DisplayText,
            Citations: delegated.Citations,
            SuggestedEnvironmentStepId: delegated.SuggestedStepId,
            TriggersRun: false);
    }

    public static AssistantResponseValidation ValidateSetupSeed(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot =
            [
                "kind", "purpose", "softTargets", "taper", "rootThicknessPercent",
                "anhedralDeg", "washoutDeg", "riderMassKg", "isSaltWater",
                "waterTemperatureC", "hRefMeters", "mastLengthCm", "profileId", "provenance"
            ];

            foreach (var prop in root.EnumerateObject())
            {
                if (ForbiddenGeometryOrPhysicsFields.Contains(prop.Name))
                    return Refused("forbidden-field:" + prop.Name, $"Proposal refused: field '{prop.Name}' is forbidden in setup-seed.", input.FallbackText);
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in setup-seed.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "setup-seed")
                return Refused("invalid-kind", "Proposal refused: kind must be 'setup-seed'.", input.FallbackText);

            if (!TryGetRequiredString(root, "purpose", out string purpose))
                return Refused("missing-purpose", "Proposal refused: purpose is required.", input.FallbackText);

            try { _ = DisciplinePresets.ResolvePurpose(purpose); }
            catch (ContractError)
            {
                return Refused("invalid-purpose", $"Proposal refused: unknown setup purpose '{purpose}'.", input.FallbackText);
            }

            SetupSoftTargetsProposal? softTargets = null;
            if (root.TryGetProperty("softTargets", out var stEl) && stEl.ValueKind != JsonValueKind.Null)
            {
                if (stEl.ValueKind != JsonValueKind.Object)
                    return Refused("invalid-soft-targets", "Proposal refused: softTargets must be an object.", input.FallbackText);
                if (!TryParseSoftTargets(stEl, out softTargets, out string stReason, out string stMsg))
                    return Refused(stReason, stMsg, input.FallbackText);
            }

            if (!TryReadOptionalBoundedDouble(root, "taper", 0.10, 1.00, out double? taper, out var err1)) return Refused(err1.Reason, err1.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "rootThicknessPercent", 5.0, 20.0, out double? rootTcPct, out var err2)) return Refused(err2.Reason, err2.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "anhedralDeg", -20.0, 20.0, out double? anhedralDeg, out var err3)) return Refused(err3.Reason, err3.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "washoutDeg", -10.0, 10.0, out double? washoutDeg, out var err4)) return Refused(err4.Reason, err4.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "riderMassKg", 30.0, 160.0, out double? riderMassKg, out var err5)) return Refused(err5.Reason, err5.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "waterTemperatureC", 0.0, 50.0, out double? waterTempC, out var err6)) return Refused(err6.Reason, err6.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "hRefMeters", 0.05, 5.0, out double? hRefMeters, out var err7)) return Refused(err7.Reason, err7.Msg, input.FallbackText);
            if (!TryReadOptionalBoundedDouble(root, "mastLengthCm", 40.0, 150.0, out double? mastLengthCm, out var err8)) return Refused(err8.Reason, err8.Msg, input.FallbackText);

            bool? isSaltWater = null;
            if (root.TryGetProperty("isSaltWater", out var saltEl) && saltEl.ValueKind != JsonValueKind.Null)
            {
                if (saltEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:isSaltWater", "Proposal refused: isSaltWater must be boolean.", input.FallbackText);
                isSaltWater = saltEl.GetBoolean();
            }

            string? profileId = null;
            if (root.TryGetProperty("profileId", out var profEl) && profEl.ValueKind != JsonValueKind.Null)
            {
                if (profEl.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(profEl.GetString()))
                    return Refused("invalid-field:profileId", "Proposal refused: profileId must be a non-empty string.", input.FallbackText);
                profileId = profEl.GetString()!.Trim();
                if (!IsAdmittedProfileId(profileId, input.AdmittedMySectionIds))
                    return Refused("unadmitted-profile", $"Proposal refused: profileId '{profileId}' is not an admitted GEN or My section.", input.FallbackText);
            }

            if (!TryParseProvenance(root, out var provenance, out string provErr))
                return Refused("invalid-provenance", provErr, input.FallbackText);

            var proposal = new SetupSeedProposal(
                Purpose: purpose.Trim(),
                SoftTargets: softTargets,
                Taper: taper,
                RootThicknessPercent: rootTcPct,
                AnhedralDeg: anhedralDeg,
                WashoutDeg: washoutDeg,
                RiderMassKg: riderMassKg,
                IsSaltWater: isSaltWater,
                WaterTemperatureC: waterTempC,
                HRefMeters: hRefMeters,
                MastLengthCm: mastLengthCm,
                ProfileId: profileId,
                Provenance: provenance);

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Setup seed proposal validated.",
                DisplayText: $"Validated setup seed for '{proposal.Purpose}' ({provenance.Count} field provenance rows).",
                Citations: [],
                SetupSeed: proposal,
                TriggersRun: false);
        }
    }

    public static SetupSeedRequest BuildSetupSeedRequest(
        SetupSeedProposal proposal,
        string? text = null,
        string? languageMeta = null,
        bool acknowledgeAllFlagged = false)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        var resolution = DisciplinePresets.ResolvePurpose(proposal.Purpose);
        var preset = resolution.Preset;

        string provenanceTag = proposal.Provenance.Count > 0 ? proposal.Provenance[0].Tag.ToString() : "Inferred";
        const string source = "Assistant setup-seed proposal";

        SoftTargetSet? softTargets = null;
        var st = proposal.SoftTargets;
        if (st is not null || proposal.Taper is not null)
        {
            double taperVal = proposal.Taper ?? preset.Taper;
            softTargets = new SoftTargetSet(
                MaxSpanMeters: st?.MaxSpanMeters is { } b ? new SoftTargetScalar(b, st.MaxSpanWeight, provenanceTag, source) : new SoftTargetScalar(preset.MaxSpanMeters, 1.0, "Defaulted", source),
                MaxChordMeters: st?.MaxChordMm is { } cMm ? new SoftTargetScalar(cMm / 1000.0, st.MaxChordWeight, provenanceTag, source) : new SoftTargetScalar(preset.MaxChordMeters, 1.0, "Defaulted", source),
                TargetAreaSquareMeters: st?.TargetAreaCm2 is { } aCm2 ? new SoftTargetScalar(aCm2 / 10_000.0, st.TargetAreaWeight, provenanceTag, source) : new SoftTargetScalar(preset.TargetAreaSquareMeters, 1.0, "Defaulted", source),
                TargetAspectRatio: st?.TargetAspectRatio is { } ar ? new SoftTargetScalar(ar, st.TargetAspectRatioWeight, provenanceTag, source) : new SoftTargetScalar(preset.TargetAspectRatio, 1.0, "Defaulted", source),
                Taper: new SoftTargetTaper(taperVal, provenanceTag, source),
                WeightBasis: "equal-default");
        }

        WaterRecord? water = null;
        if (proposal.IsSaltWater is { } salt)
        {
            water = salt ? DisciplinePresets.DefaultSaltWater15C : DisciplinePresets.DefaultFreshWater15C;
            if (proposal.WaterTemperatureC is { } tC && Math.Abs(tC - 15.0) > 1e-9)
                water = water with { TemperatureC = tC };
        }

        return new SetupSeedRequest(
            Purpose: proposal.Purpose,
            SoftTargets: softTargets,
            EntryKind: "language",
            Text: text,
            LanguageMeta: languageMeta,
            RiderMassKg: proposal.RiderMassKg,
            Water: water,
            HRefMeters: proposal.HRefMeters,
            MastLengthCm: proposal.MastLengthCm,
            RootThicknessRatio: proposal.RootThicknessPercent is { } tcPct ? tcPct / 100.0 : null,
            AnhedralDeg: proposal.AnhedralDeg,
            WashoutDeg: proposal.WashoutDeg,
            ProfileId: proposal.ProfileId,
            AcknowledgeAllFlagged: acknowledgeAllFlagged);
    }

    public static AssistantResponseValidation ValidateGeometryEdit(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot = ["kind", "baseAcceptedId", "edits", "provenance"];
            foreach (var prop in root.EnumerateObject())
            {
                if (ForbiddenGeometryOrPhysicsFields.Contains(prop.Name))
                    return Refused("forbidden-field:" + prop.Name, $"Proposal refused: field '{prop.Name}' is forbidden in geometry-edit.", input.FallbackText);
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in geometry-edit.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "geometry-edit")
                return Refused("invalid-kind", "Proposal refused: kind must be 'geometry-edit'.", input.FallbackText);

            if (!TryGetRequiredString(root, "baseAcceptedId", out string baseAcceptedId))
                return Refused("missing-base-id", "Proposal refused: baseAcceptedId is required.", input.FallbackText);

            if (!root.TryGetProperty("edits", out var editsEl) || editsEl.ValueKind != JsonValueKind.Array || editsEl.GetArrayLength() == 0)
                return Refused("missing-edits", "Proposal refused: edits must be a non-empty array.", input.FallbackText);

            if (!TryParseProvenance(root, out var provenance, out string provErr))
                return Refused("invalid-provenance", provErr, input.FallbackText);

            string proposalId = string.IsNullOrWhiteSpace(input.ProposalId) ? Guid.NewGuid().ToString("D") : input.ProposalId.Trim();
            string lockSource = $"assistant-proposal {proposalId}";
            var ops = new List<GeometryEditOp>();

            foreach (var item in editsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return Refused("invalid-edit-op", "Proposal refused: each edit entry must be an object.", input.FallbackText);

                if (!TryParseGeometryEditOp(item, lockSource, input, out var op, out string opReason, out string opMsg))
                    return Refused(opReason, opMsg, input.FallbackText);
                ops.Add(op!);
            }

            var draftSpec = new GeometryEditDraftSpec(
                ProposalId: proposalId,
                BaseAcceptedId: baseAcceptedId.Trim(),
                Operations: ops,
                Provenance: provenance,
                BaseDriftDetected: false,
                AcceptAllowed: true,
                DriftReason: null);

            if (!string.IsNullOrWhiteSpace(input.CurrentAcceptedId))
                draftSpec = BuildGeometryEditDraft(draftSpec, input.CurrentAcceptedId);

            if (draftSpec.BaseDriftDetected)
            {
                return new AssistantResponseValidation(
                    Verdict: AssistantVerdict.Refused,
                    Disposition: ProposalDisposition.Refused,
                    Reason: "base-revision-moved",
                    Message: draftSpec.DriftReason!,
                    DisplayText: draftSpec.DriftReason!,
                    Citations: [],
                    GeometryEdit: draftSpec,
                    TriggersRun: false);
            }

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Geometry edit proposal validated.",
                DisplayText: $"Validated {ops.Count} typed geometry edit(s) on base revision {draftSpec.BaseAcceptedId}.",
                Citations: [],
                GeometryEdit: draftSpec,
                TriggersRun: false);
        }
    }

    public static GeometryEditDraftSpec BuildGeometryEditDraft(GeometryEditDraftSpec spec, string currentAcceptedId)
    {
        ArgumentNullException.ThrowIfNull(spec);
        bool drifted = !string.Equals(spec.BaseAcceptedId, currentAcceptedId, StringComparison.Ordinal);
        return spec with
        {
            BaseDriftDetected = drifted,
            AcceptAllowed = !drifted,
            DriftReason = drifted
                ? $"Accept disabled — base design revision moved from '{spec.BaseAcceptedId}' to '{currentAcceptedId}'. Refresh over the current revision before accepting."
                : null
        };
    }

    public static AssistantResponseValidation ValidateAnalysisExplanation(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot = ["kind", "text", "citations", "whatIfOffer"];
            foreach (var prop in root.EnumerateObject())
            {
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in analysis explanation.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "explanation")
                return Refused("invalid-kind", "Proposal refused: kind must be 'explanation'.", input.FallbackText);

            if (!TryGetRequiredString(root, "text", out string text))
                return Withheld("empty-text", "Response withheld: explanation text is empty.", input.FallbackText);

            if (!TryResolveCitations(root, input, out var citations, out var citedLines, out string citMsg))
                return Withheld("unresolved-citation", citMsg, input.FallbackText);

            if (CheckExplanationTextSafety(text, input, citedLines) is { } safetyFail)
                return safetyFail;

            WhatIfOffer? whatIf = null;
            if (root.TryGetProperty("whatIfOffer", out var wiEl) && wiEl.ValueKind != JsonValueKind.Null)
            {
                if (wiEl.ValueKind != JsonValueKind.Object)
                    return Refused("invalid-what-if", "Proposal refused: whatIfOffer must be an object.", input.FallbackText);
                if (!TryParseWhatIfOffer(wiEl, out whatIf, out string wiReason, out string wiMsg))
                    return Refused(wiReason, wiMsg, input.FallbackText);
            }

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Analysis explanation verified.",
                DisplayText: text,
                Citations: citations,
                AnalysisExplanation: new AnalysisExplanationProposal(text, citations, whatIf),
                TriggersRun: false);
        }
    }

    public static AssistantResponseValidation ValidateExperimentConfig(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "experiment-config")
                return Refused("invalid-kind", "Proposal refused: kind must be 'experiment-config'.", input.FallbackText);

            if (!TryGetRequiredString(root, "mode", out string mode) || mode is not ("sweep" or "optimize"))
                return Refused("invalid-mode", "Proposal refused: mode must be 'sweep' or 'optimize'.", input.FallbackText);

            if (!TryParseProvenance(root, out var provenance, out string provErr))
                return Refused("invalid-provenance", provErr, input.FallbackText);

            if (mode == "sweep")
            {
                HashSet<string> allowedSweep =
                [
                    "kind", "mode", "speeds", "speedUnit", "anglesDeg",
                    "waterTemperatureC", "isSaltWater", "hRefMeters", "tier", "provenance"
                ];
                foreach (var prop in root.EnumerateObject())
                {
                    if (!allowedSweep.Contains(prop.Name))
                        return Refused("extra-field:" + prop.Name, $"Proposal refused: field '{prop.Name}' is not allowed in sweep experiment-config.", input.FallbackText);
                }

                if (!TryGetRequiredString(root, "speedUnit", out string speedUnit) || speedUnit is not ("kn" or "m/s"))
                    return Refused("invalid-speed-unit", "Proposal refused: speedUnit must be 'kn' or 'm/s'.", input.FallbackText);

                double maxSpeed = speedUnit == "kn" ? 100.0 : 50.0;
                if (!TryReadUniqueDoubleArray(root, "speeds", 1e-6, maxSpeed, out var speeds, out string spMsg))
                    return Refused("invalid-speeds", spMsg, input.FallbackText);

                if (!TryReadUniqueDoubleArray(root, "anglesDeg", -15.0, 25.0, out var angles, out string angMsg))
                    return Refused("invalid-angles", angMsg, input.FallbackText);

                if (speeds.Count * angles.Count > 200)
                    return Refused("sweep-grid-too-large", $"Proposal refused: sweep grid has {speeds.Count * angles.Count} cases, exceeding the 200-case limit.", input.FallbackText);

                if (!TryReadRequiredBoundedDouble(root, "waterTemperatureC", 0.0, 50.0, out double tempC, out var tErr))
                    return Refused(tErr.Reason, tErr.Msg, input.FallbackText);

                if (!root.TryGetProperty("isSaltWater", out var saltEl) || saltEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:isSaltWater", "Proposal refused: isSaltWater must be boolean.", input.FallbackText);

                if (!TryReadOptionalBoundedDouble(root, "hRefMeters", 0.05, 5.0, out double? hRef, out var hErr))
                    return Refused(hErr.Reason, hErr.Msg, input.FallbackText);

                if (!TryGetRequiredString(root, "tier", out string tier) || tier is not ("vlm-strip" or "estimator" or "cfd"))
                    return Refused("invalid-tier", "Proposal refused: tier must be 'vlm-strip', 'estimator', or 'cfd'.", input.FallbackText);

                var proposal = new ExperimentConfigProposal(
                    Mode: "sweep",
                    Speeds: speeds,
                    SpeedUnit: speedUnit,
                    AnglesDeg: angles,
                    WaterTemperatureC: tempC,
                    IsSaltWater: saltEl.GetBoolean(),
                    HRefMeters: hRef,
                    Tier: tier,
                    OperatingPointIds: null,
                    Objectives: null,
                    IncludeAcavConstraint: false,
                    UnfreezeThickness: false,
                    HasStructuralProxyRow: false,
                    MaxEvaluations: null,
                    MaxWallMinutes: null,
                    Seed: null,
                    Provenance: provenance,
                    PreviewOnly: true,
                    Queued: false);

                return new AssistantResponseValidation(
                    Verdict: AssistantVerdict.Shown,
                    Disposition: ProposalDisposition.Pending,
                    Reason: "ok",
                    Message: "Sweep experiment preview validated.",
                    DisplayText: $"Sweep preview: {speeds.Count} speeds x {angles.Count} angles ({speeds.Count * angles.Count} cases, {tier}).",
                    Citations: [],
                    ExperimentConfig: proposal,
                    TriggersRun: false);
            }
            else
            {
                HashSet<string> allowedOpt =
                [
                    "kind", "mode", "operatingPointIds", "objectives", "includeAcavConstraint",
                    "unfreezeThickness", "hasStructuralProxyRow", "maxEvaluations", "maxWallMinutes", "seed", "provenance"
                ];
                foreach (var prop in root.EnumerateObject())
                {
                    if (!allowedOpt.Contains(prop.Name))
                        return Refused("extra-field:" + prop.Name, $"Proposal refused: field '{prop.Name}' is not allowed in optimize experiment-config.", input.FallbackText);
                }

                if (!root.TryGetProperty("operatingPointIds", out var opsEl) || opsEl.ValueKind != JsonValueKind.Array)
                    return Refused("missing-operating-points", "Proposal refused: operatingPointIds array is required.", input.FallbackText);

                var opIds = new List<string>();
                var seenOps = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in opsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                        return Refused("invalid-operating-point", "Proposal refused: operatingPointIds must contain non-empty strings.", input.FallbackText);
                    string opId = item.GetString()!.Trim();
                    if (!seenOps.Add(opId))
                        return Refused("duplicate-operating-point", $"Proposal refused: duplicate operatingPointId '{opId}'.", input.FallbackText);
                    opIds.Add(opId);
                }

                if (opIds.Count < 2)
                    return Refused("single-point-optimize-refused", SinglePointOptimizeRefusal, SinglePointOptimizeRefusal);

                if (!root.TryGetProperty("includeAcavConstraint", out var acavEl) || acavEl.ValueKind != JsonValueKind.True)
                    return Refused("acav-constraint-required", "Proposal refused: includeAcavConstraint must be true for multipoint optimization.", input.FallbackText);

                if (!root.TryGetProperty("unfreezeThickness", out var unfEl) || unfEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:unfreezeThickness", "Proposal refused: unfreezeThickness must be boolean.", input.FallbackText);
                if (!root.TryGetProperty("hasStructuralProxyRow", out var proxyEl) || proxyEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:hasStructuralProxyRow", "Proposal refused: hasStructuralProxyRow must be boolean.", input.FallbackText);

                bool unfreeze = unfEl.GetBoolean();
                bool hasProxy = proxyEl.GetBoolean();
                if (unfreeze && !hasProxy)
                    return Refused("thickness-frozen-without-structural-proxy", UnfrozenThicknessRefusal, UnfrozenThicknessRefusal);

                if (!root.TryGetProperty("objectives", out var objEl) || objEl.ValueKind != JsonValueKind.Array || objEl.GetArrayLength() == 0)
                    return Refused("missing-objectives", "Proposal refused: objectives must be a non-empty array.", input.FallbackText);

                var objectives = new List<OptimizeObjectiveSpec>();
                foreach (var objItem in objEl.EnumerateArray())
                {
                    if (objItem.ValueKind != JsonValueKind.Object)
                        return Refused("invalid-objective", "Proposal refused: each objective must be an object.", input.FallbackText);
                    foreach (var p in objItem.EnumerateObject())
                    {
                        if (p.Name is not ("kind" or "operatingPointId" or "weight"))
                            return Refused("extra-field:" + p.Name, $"Proposal refused: unknown objective field '{p.Name}'.", input.FallbackText);
                    }
                    if (!TryGetRequiredString(objItem, "kind", out string oKind) || oKind is not ("D_k/D_k(x0)" or "L/D"))
                        return Refused("invalid-objective-kind", "Proposal refused: objective kind must be dimensionless 'D_k/D_k(x0)' or 'L/D'.", input.FallbackText);
                    if (!TryGetRequiredString(objItem, "operatingPointId", out string oOp) || !seenOps.Contains(oOp))
                        return Refused("invalid-objective-op", $"Proposal refused: objective operatingPointId '{oOp}' is not in operatingPointIds.", input.FallbackText);
                    if (!TryReadRequiredBoundedDouble(objItem, "weight", 1e-6, 1e6, out double weight, out var wErr))
                        return Refused(wErr.Reason, wErr.Msg, input.FallbackText);
                    objectives.Add(new OptimizeObjectiveSpec(oKind, oOp, weight));
                }

                if (!TryReadRequiredBoundedInt(root, "maxEvaluations", 10, 500, out int maxEvals, out var evalErr))
                    return Refused(evalErr.Reason, evalErr.Msg, input.FallbackText);
                if (!TryReadRequiredBoundedInt(root, "maxWallMinutes", 1, 240, out int maxWall, out var wallErr))
                    return Refused(wallErr.Reason, wallErr.Msg, input.FallbackText);
                if (!root.TryGetProperty("seed", out var seedEl) || seedEl.ValueKind != JsonValueKind.Number || !seedEl.TryGetInt32(out int seed))
                    return Refused("invalid-field:seed", "Proposal refused: seed must be a 32-bit integer.", input.FallbackText);

                var proposal = new ExperimentConfigProposal(
                    Mode: "optimize",
                    Speeds: null,
                    SpeedUnit: null,
                    AnglesDeg: null,
                    WaterTemperatureC: null,
                    IsSaltWater: null,
                    HRefMeters: null,
                    Tier: null,
                    OperatingPointIds: opIds,
                    Objectives: objectives,
                    IncludeAcavConstraint: true,
                    UnfreezeThickness: unfreeze,
                    HasStructuralProxyRow: hasProxy,
                    MaxEvaluations: maxEvals,
                    MaxWallMinutes: maxWall,
                    Seed: seed,
                    Provenance: provenance,
                    PreviewOnly: true,
                    Queued: false);

                return new AssistantResponseValidation(
                    Verdict: AssistantVerdict.Shown,
                    Disposition: ProposalDisposition.Pending,
                    Reason: "ok",
                    Message: "Multipoint optimize experiment preview validated.",
                    DisplayText: $"Optimize preview: {opIds.Count} operating points, {maxEvals} max evaluations, {maxWall} min budget (not queued).",
                    Citations: [],
                    ExperimentConfig: proposal,
                    TriggersRun: false);
            }
        }
    }

    public static AssistantResponseValidation ValidateRunFailureExplanation(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot = ["kind", "text", "observations", "hypotheses", "citations"];
            foreach (var prop in root.EnumerateObject())
            {
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in run failure explanation.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "explanation")
                return Refused("invalid-kind", "Proposal refused: kind must be 'explanation'.", input.FallbackText);

            if (!TryGetRequiredString(root, "text", out string text))
                return Withheld("empty-text", "Response withheld: explanation text is empty.", input.FallbackText);

            if (!TryReadNonEmptyStringArray(root, "observations", out var observations))
                return Withheld("missing-observations", "Response withheld: observations array must be non-empty.", input.FallbackText);

            if (!TryReadNonEmptyStringArray(root, "hypotheses", out var hypotheses))
                return Withheld("missing-hypotheses", "Response withheld: hypotheses array must be non-empty.", input.FallbackText);

            if (!TryResolveCitations(root, input, out var citations, out var citedLines, out string citMsg))
                return Withheld("unresolved-citation", citMsg, input.FallbackText);

            string combinedProse = string.Join("\n", new[] { text }.Concat(observations).Concat(hypotheses));
            if (CheckExplanationTextSafety(combinedProse, input, citedLines) is { } safetyFail)
                return safetyFail;

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Run failure explanation verified.",
                DisplayText: text,
                Citations: citations,
                RunFailureExplanation: new RunFailureExplanationProposal(text, observations, hypotheses, citations),
                TriggersRun: false);
        }
    }

    public static AssistantResponseValidation ValidateCaseDiff(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (CaseDiffMacroRegex.IsMatch(rawJson ?? ""))
            return Refused("forbidden-case-macro", "Proposal refused: dictionary macros (#codeStream, #calc, systemCall) are forbidden.", input.FallbackText);

        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot =
            [
                "kind", "failedRunId", "targetExperimentId", "experimentIsQueued",
                "createNewDraftVersion", "changes", "provenance"
            ];
            foreach (var prop in root.EnumerateObject())
            {
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: field '{prop.Name}' is not allowed in case-diff.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "case-diff")
                return Refused("invalid-kind", "Proposal refused: kind must be 'case-diff'.", input.FallbackText);

            if (!TryGetRequiredString(root, "failedRunId", out string failedRunId))
                return Refused("missing-failed-run-id", "Proposal refused: failedRunId is required.", input.FallbackText);

            if (!TryGetRequiredString(root, "targetExperimentId", out string targetExperimentId))
                return Refused("missing-target-experiment-id", "Proposal refused: targetExperimentId is required.", input.FallbackText);

            bool payloadQueued = false;
            if (root.TryGetProperty("experimentIsQueued", out var qEl) && qEl.ValueKind != JsonValueKind.Null)
            {
                if (qEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:experimentIsQueued", "Proposal refused: experimentIsQueued must be boolean.", input.FallbackText);
                payloadQueued = qEl.GetBoolean();
            }

            bool createNewDraft = false;
            if (root.TryGetProperty("createNewDraftVersion", out var draftEl) && draftEl.ValueKind != JsonValueKind.Null)
            {
                if (draftEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return Refused("invalid-field:createNewDraftVersion", "Proposal refused: createNewDraftVersion must be boolean.", input.FallbackText);
                createNewDraft = draftEl.GetBoolean();
            }

            bool targetIsQueued = input.TargetExperimentIsQueued || payloadQueued;
            if (targetIsQueued && !createNewDraft)
            {
                return Refused(
                    "queued-experiment-immutable",
                    $"Proposal refused: Queued experiment '{targetExperimentId}' is immutable and cannot be mutated directly — create a new Draft experiment version referencing failedRunId '{failedRunId}'.",
                    input.FallbackText);
            }

            if (!root.TryGetProperty("changes", out var changesEl) || changesEl.ValueKind != JsonValueKind.Array || changesEl.GetArrayLength() == 0)
                return Refused("missing-changes", "Proposal refused: changes must be a non-empty array.", input.FallbackText);

            if (!TryParseProvenance(root, out var provenance, out string provErr))
                return Refused("invalid-provenance", provErr, input.FallbackText);

            var changes = new List<CaseDiffChange>();
            var seenFields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in changesEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return Refused("invalid-change", "Proposal refused: each change must be an object.", input.FallbackText);
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("field" or "value"))
                        return Refused("extra-field:" + p.Name, $"Proposal refused: unknown change field '{p.Name}'.", input.FallbackText);
                }

                if (!TryGetRequiredString(item, "field", out string fieldName))
                    return Refused("missing-change-field", "Proposal refused: change.field is required.", input.FallbackText);
                if (!seenFields.Add(fieldName))
                    return Refused("duplicate-change-field", $"Proposal refused: duplicate case-diff field '{fieldName}'.", input.FallbackText);
                if (!item.TryGetProperty("value", out var valEl))
                    return Refused("missing-change-value", $"Proposal refused: change.value is required for '{fieldName}'.", input.FallbackText);

                if (!TryValidateCaseDiffChange(fieldName, valEl, out var change, out string cReason, out string cMsg))
                    return Refused(cReason, cMsg, input.FallbackText);
                changes.Add(change!);
            }

            // Always Pending, never triggers a run — even when quoted logs contain "apply this repair and run".
            var proposal = new CaseDiffProposal(
                FailedRunId: failedRunId.Trim(),
                TargetExperimentId: targetExperimentId.Trim(),
                CreatesNewDraftVersion: targetIsQueued || createNewDraft,
                Changes: changes,
                Provenance: provenance,
                Disposition: ProposalDisposition.Pending,
                TriggersRun: false);

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Case diff proposal validated (Draft preview only, pending review).",
                DisplayText: $"Validated {changes.Count} TypedCaseModel change(s) referencing {proposal.FailedRunId}.",
                Citations: [],
                CaseDiff: proposal,
                TriggersRun: false);
        }
    }

    public static AssistantResponseValidation ValidateResultsExplanation(string rawJson, AssistantValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryParseStrictObject(rawJson, out var doc, out var err))
            return err!;

        using (doc!)
        {
            var root = doc.RootElement;
            HashSet<string> allowedRoot = ["kind", "text", "citations", "sampleRunId", "separationCriterion"];
            foreach (var prop in root.EnumerateObject())
            {
                if (!allowedRoot.Contains(prop.Name))
                    return Refused("extra-field:" + prop.Name, $"Proposal refused: unknown field '{prop.Name}' is not allowed in results explanation.", input.FallbackText);
            }

            if (!TryGetRequiredString(root, "kind", out string kind) || kind != "explanation")
                return Refused("invalid-kind", "Proposal refused: kind must be 'explanation'.", input.FallbackText);

            if (!TryGetRequiredString(root, "text", out string text))
                return Withheld("empty-text", "Response withheld: explanation text is empty.", input.FallbackText);

            string sampleRunId = input.SampleRunId ?? "";
            if (root.TryGetProperty("sampleRunId", out var runEl) && runEl.ValueKind != JsonValueKind.Null)
            {
                if (runEl.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(runEl.GetString()))
                    return Refused("invalid-sample-run-id", "Proposal refused: sampleRunId must be a non-empty string.", input.FallbackText);
                string providedRunId = runEl.GetString()!.Trim();
                if (!string.IsNullOrWhiteSpace(sampleRunId) && !string.Equals(sampleRunId, providedRunId, StringComparison.Ordinal))
                    return Refused("sample-run-mismatch", $"Proposal refused: sampleRunId '{providedRunId}' does not match selected sample '{sampleRunId}'.", input.FallbackText);
                sampleRunId = providedRunId;
            }

            string? sepCriterion = null;
            if (root.TryGetProperty("separationCriterion", out var scEl) && scEl.ValueKind != JsonValueKind.Null)
            {
                if (scEl.ValueKind != JsonValueKind.String)
                    return Refused("invalid-separation-criterion", "Proposal refused: separationCriterion must be a string.", input.FallbackText);
                sepCriterion = scEl.GetString()!.Trim();
            }

            // HARD VETO (AI-10 / A5.11): Separation requires wall tau_w on a wall surface patch + supported named criterion.
            bool mentionsSeparation =
                SeparationMentionRegex.IsMatch(input.UserPrompt ?? "") ||
                SeparationMentionRegex.IsMatch(text) ||
                !string.IsNullOrWhiteSpace(sepCriterion);

            if (mentionsSeparation)
            {
                if (!input.SeparationLayerAvailable)
                {
                    var fallbackCitations = new List<string>();
                    if (!string.IsNullOrWhiteSpace(sampleRunId)) fallbackCitations.Add(sampleRunId);
                    if (input.AvailableLayerIds is not null) fallbackCitations.AddRange(input.AvailableLayerIds.OrderBy(x => x, StringComparer.Ordinal));
                    return new AssistantResponseValidation(
                        Verdict: AssistantVerdict.Withheld,
                        Disposition: ProposalDisposition.Withheld,
                        Reason: "separation-no-supported-criterion",
                        Message: FieldEvidenceManifest.SeparationUnavailableReason,
                        DisplayText: FieldEvidenceManifest.SeparationUnavailableReason,
                        Citations: fallbackCitations,
                        TriggersRun: false);
                }

                // Never allow diagnosing separation from Q or lambda2 vortex-core candidates even if tau_w exists.
                if (VortexCoreProxyRegex.IsMatch(text))
                {
                    return Withheld(
                        "separation-q-lambda2-forbidden",
                        "Response withheld: Q/lambda2 isosurfaces are vortex-core candidates only and cannot diagnose separation.",
                        FieldEvidenceManifest.SeparationUnavailableReason);
                }

                if (!string.IsNullOrWhiteSpace(sepCriterion) && !FieldEvidenceManifest.IsSupportedSeparationCriterion(sepCriterion))
                {
                    return Withheld(
                        "separation-unsupported-criterion",
                        $"Response withheld: separationCriterion '{sepCriterion}' is not a supported wall tau_w criterion.",
                        FieldEvidenceManifest.SeparationUnavailableReason);
                }
            }

            if (!TryResolveCitations(root, input, out var citations, out var citedLines, out string citMsg))
                return Withheld("unresolved-citation", citMsg, input.FallbackText);

            if (CheckExplanationTextSafety(text, input, citedLines) is { } safetyFail)
                return safetyFail;

            return new AssistantResponseValidation(
                Verdict: AssistantVerdict.Shown,
                Disposition: ProposalDisposition.Pending,
                Reason: "ok",
                Message: "Results explanation verified.",
                DisplayText: text,
                Citations: citations,
                ResultsExplanation: new ResultsExplanationProposal(
                    sampleRunId,
                    text,
                    citations,
                    input.SeparationLayerAvailable,
                    sepCriterion ?? input.SeparationCriterion),
                TriggersRun: false);
        }
    }

    public static string? FindUnsupportedNumeral(
        string text,
        string sharedContextText,
        IReadOnlyDictionary<int, string> excerptLines,
        IReadOnlySet<int>? citedLines = null)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in NumeralTokenRegex.Matches(sharedContextText ?? ""))
            allowed.Add(m.Value);
        if (excerptLines is not null)
        {
            foreach (var kv in excerptLines)
            {
                allowed.Add(kv.Key.ToString(CultureInfo.InvariantCulture));
                foreach (Match m in NumeralTokenRegex.Matches(kv.Value ?? ""))
                    allowed.Add(m.Value);
            }
        }
        if (citedLines is not null)
        {
            foreach (int line in citedLines)
                allowed.Add(line.ToString(CultureInfo.InvariantCulture));
        }

        foreach (Match m in NumeralTokenRegex.Matches(text ?? ""))
        {
            if (!allowed.Contains(m.Value))
                return m.Value;
        }
        return null;
    }

    private static AssistantResponseValidation ValidateSolverSetupFromInput(string rawJson, AssistantValidationInput input)
    {
        if (input.SetupContext is null)
            return Refused("missing-setup-context", "Step refused: outside the allow-list — setup validation context is required.", input.FallbackText);
        return ValidateSolverSetup(rawJson, input.SetupContext);
    }

    private static AssistantResponseValidation? CheckExplanationTextSafety(
        string text,
        AssistantValidationInput input,
        HashSet<int> citedLines)
    {
        var lab = Lab01CopyLint.CheckText(text);
        if (!lab.Passed)
        {
            return Withheld(
                "lab01-forbidden-claim",
                $"Response withheld: forbidden LAB-01 claim '{lab.Violations[0].Snippet}'.",
                input.FallbackText);
        }

        var cmd = SetupCopyLint.Lint(text);
        if (!cmd.Passed)
        {
            return Withheld(
                "command-to-type",
                "Response withheld: it told you to type a shell command.",
                input.FallbackText);
        }

        string? badNumeral = FindUnsupportedNumeral(
            text,
            input.SharedContextText ?? "",
            input.ExcerptLines ?? new Dictionary<int, string>(),
            citedLines);
        if (badNumeral is not null)
        {
            return Withheld(
                "unsupported-numeral",
                $"Response withheld: numeral '{badNumeral}' is not supported by the shared context.",
                input.FallbackText);
        }

        return null;
    }

    private static bool TryResolveCitations(
        JsonElement root,
        AssistantValidationInput input,
        out List<string> citations,
        out HashSet<int> citedLines,
        out string errorMessage)
    {
        citations = [];
        citedLines = [];
        errorMessage = "";

        if (!root.TryGetProperty("citations", out var citEl) || citEl.ValueKind != JsonValueKind.Array)
        {
            errorMessage = "Response withheld: citations array is required.";
            return false;
        }

        foreach (var item in citEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                errorMessage = "Response withheld: every citation must be a non-empty string.";
                return false;
            }

            string cid = item.GetString()!.Trim();
            if (!TryResolveSingleCitation(cid, input, citedLines))
            {
                errorMessage = $"Response withheld: citation '{cid}' does not resolve.";
                return false;
            }
            citations.Add(cid);
        }

        if (citations.Count == 0)
        {
            errorMessage = "Response withheld: at least one citation is required.";
            return false;
        }
        return true;
    }

    private static bool TryResolveSingleCitation(string citation, AssistantValidationInput input, HashSet<int> citedLines)
    {
        var excerpts = input.ExcerptLines;
        if (excerpts is not null)
        {
            var single = LineCitation.Match(citation);
            if (single.Success && int.TryParse(single.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int lineNum))
            {
                if (!excerpts.ContainsKey(lineNum)) return false;
                citedLines.Add(lineNum);
                return true;
            }

            var range = LinesRangeCitation.Match(citation);
            if (range.Success &&
                int.TryParse(range.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int start) &&
                int.TryParse(range.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int end) &&
                start <= end && end - start <= 100)
            {
                for (int n = start; n <= end; n++)
                {
                    if (!excerpts.ContainsKey(n)) return false;
                    citedLines.Add(n);
                }
                return true;
            }
        }

        if (input.KnownKnowledgeIds is not null && input.KnownKnowledgeIds.Contains(citation)) return true;
        if (input.AllowedRunIds is not null && input.AllowedRunIds.Contains(citation)) return true;
        if (input.AllowedQuantityIds is not null && input.AllowedQuantityIds.Contains(citation)) return true;
        if (!string.IsNullOrWhiteSpace(input.SampleRunId) && string.Equals(citation, input.SampleRunId, StringComparison.Ordinal)) return true;
        if (input.AvailableLayerIds is not null && input.AvailableLayerIds.Contains(citation)) return true;

        return false;
    }

    private static bool TryParseSoftTargets(
        JsonElement stEl,
        out SetupSoftTargetsProposal? result,
        out string reason,
        out string msg)
    {
        result = null;
        reason = "";
        msg = "";
        HashSet<string> allowed =
        [
            "maxSpanMeters", "maxSpanWeight", "maxChordMm", "maxChordWeight",
            "targetAreaCm2", "targetAreaWeight", "targetAspectRatio", "targetAspectRatioWeight"
        ];
        foreach (var prop in stEl.EnumerateObject())
        {
            if (ForbiddenGeometryOrPhysicsFields.Contains(prop.Name) || !allowed.Contains(prop.Name))
            {
                reason = "extra-field:softTargets." + prop.Name;
                msg = $"Proposal refused: unknown softTargets field '{prop.Name}'.";
                return false;
            }
        }

        if (!TryReadOptionalBoundedDouble(stEl, "maxSpanMeters", 0.40, 2.50, out double? maxSpan, out var e1)) { reason = e1.Reason; msg = e1.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "maxSpanWeight", 0.0, 100.0, out double? wb, out var e2)) { reason = e2.Reason; msg = e2.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "maxChordMm", 40.0, 400.0, out double? maxChord, out var e3)) { reason = e3.Reason; msg = e3.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "maxChordWeight", 0.0, 100.0, out double? wc, out var e4)) { reason = e4.Reason; msg = e4.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "targetAreaCm2", 300.0, 3500.0, out double? areaCm2, out var e5)) { reason = e5.Reason; msg = e5.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "targetAreaWeight", 0.0, 100.0, out double? wS, out var e6)) { reason = e6.Reason; msg = e6.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "targetAspectRatio", 4.0, 20.0, out double? ar, out var e7)) { reason = e7.Reason; msg = e7.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(stEl, "targetAspectRatioWeight", 0.0, 100.0, out double? wAr, out var e8)) { reason = e8.Reason; msg = e8.Msg; return false; }

        result = new SetupSoftTargetsProposal(
            MaxSpanMeters: maxSpan,
            MaxSpanWeight: wb ?? 1.0,
            MaxChordMm: maxChord,
            MaxChordWeight: wc ?? 1.0,
            TargetAreaCm2: areaCm2,
            TargetAreaWeight: wS ?? 1.0,
            TargetAspectRatio: ar,
            TargetAspectRatioWeight: wAr ?? 1.0);
        return true;
    }

    private static bool TryParseGeometryEditOp(
        JsonElement item,
        string lockSource,
        AssistantValidationInput input,
        out GeometryEditOp? op,
        out string reason,
        out string msg)
    {
        op = null;
        reason = "";
        msg = "";
        if (!TryGetRequiredString(item, "op", out string opName))
        {
            reason = "missing-edit-op";
            msg = "Proposal refused: each edit requires an 'op' field.";
            return false;
        }

        switch (opName)
        {
            case "ChannelDelta":
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("op" or "channel" or "eta" or "deltaValue" or "unit"))
                    {
                        reason = "extra-field:" + p.Name;
                        msg = $"Proposal refused: unknown ChannelDelta field '{p.Name}'.";
                        return false;
                    }
                }
                if (!TryGetRequiredString(item, "channel", out string ch) || !AllowedChannels.Contains(ch))
                {
                    reason = "invalid-channel";
                    msg = $"Proposal refused: channel '{ch}' must be one of leading, trailing, elevation, twist, thickness.";
                    return false;
                }
                if (!TryReadRequiredBoundedDouble(item, "eta", 0.0, 1.0, out double eta, out var etaErr))
                {
                    reason = etaErr.Reason; msg = etaErr.Msg; return false;
                }
                if (!TryGetRequiredString(item, "unit", out string unit))
                {
                    reason = "missing-unit"; msg = "Proposal refused: ChannelDelta requires unit."; return false;
                }
                (double minDelta, double maxDelta, bool validUnit) = (ch, unit) switch
                {
                    ("leading" or "trailing" or "elevation", "m") => (-0.50, 0.50, true),
                    ("leading" or "trailing" or "elevation", "mm") => (-500.0, 500.0, true),
                    ("twist", "deg") => (-15.0, 15.0, true),
                    ("thickness", "%") => (-10.0, 10.0, true),
                    ("thickness", "ratio") => (-0.10, 0.10, true),
                    _ => (0.0, 0.0, false)
                };
                if (!validUnit)
                {
                    reason = "invalid-unit";
                    msg = $"Proposal refused: unit '{unit}' is invalid for channel '{ch}'.";
                    return false;
                }
                if (!TryReadRequiredBoundedDouble(item, "deltaValue", minDelta, maxDelta, out double delta, out var dErr))
                {
                    reason = dErr.Reason; msg = dErr.Msg; return false;
                }
                op = new GeometryEditOp(GeometryEditOpKind.ChannelDelta, ch, eta, delta, unit, null, null, null, null);
                return true;

            case "AddStation":
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("op" or "eta"))
                    {
                        reason = "extra-field:" + p.Name;
                        msg = $"Proposal refused: unknown AddStation field '{p.Name}'.";
                        return false;
                    }
                }
                if (!TryReadRequiredBoundedDouble(item, "eta", 1e-4, 1.0 - 1e-4, out double addEta, out var aErr))
                {
                    reason = aErr.Reason; msg = aErr.Msg; return false;
                }
                if (input.ExistingStationEtas is not null && input.ExistingStationEtas.Any(e => Math.Abs(e - addEta) < 1e-4))
                {
                    reason = "coincident-station";
                    msg = $"Proposal refused: AddStation eta={addEta.ToString("G", CultureInfo.InvariantCulture)} coincides with an existing station.";
                    return false;
                }
                op = new GeometryEditOp(GeometryEditOpKind.AddStation, null, addEta, null, null, null, null, null, null);
                return true;

            case "RemoveStation":
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("op" or "eta"))
                    {
                        reason = "extra-field:" + p.Name;
                        msg = $"Proposal refused: unknown RemoveStation field '{p.Name}'.";
                        return false;
                    }
                }
                if (!TryReadRequiredBoundedDouble(item, "eta", 1e-4, 1.0 - 1e-4, out double remEta, out var rErr))
                {
                    reason = "cannot-remove-root-or-tip";
                    msg = "Proposal refused: RemoveStation eta must lie strictly inside (0, 1) — root and tip stations cannot be removed.";
                    return false;
                }
                op = new GeometryEditOp(GeometryEditOpKind.RemoveStation, null, remEta, null, null, null, null, null, null);
                return true;

            case "AssignProfile":
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("op" or "eta" or "profileId"))
                    {
                        reason = "extra-field:" + p.Name;
                        msg = $"Proposal refused: unknown AssignProfile field '{p.Name}'.";
                        return false;
                    }
                }
                if (!TryReadRequiredBoundedDouble(item, "eta", 0.0, 1.0, out double profEta, out var peErr))
                {
                    reason = peErr.Reason; msg = peErr.Msg; return false;
                }
                if (!TryGetRequiredString(item, "profileId", out string profileId) || !IsAdmittedProfileId(profileId, input.AdmittedMySectionIds))
                {
                    reason = "unadmitted-profile";
                    msg = $"Proposal refused: profileId '{profileId}' is not an admitted GEN or My section.";
                    return false;
                }
                op = new GeometryEditOp(GeometryEditOpKind.AssignProfile, null, profEta, null, null, profileId.Trim(), null, null, null);
                return true;

            case "LockChange":
                foreach (var p in item.EnumerateObject())
                {
                    if (p.Name is not ("op" or "channel" or "vertexIndex" or "locked"))
                    {
                        reason = "extra-field:" + p.Name;
                        msg = $"Proposal refused: unknown LockChange field '{p.Name}'.";
                        return false;
                    }
                }
                if (!TryGetRequiredString(item, "channel", out string lch) || !AllowedChannels.Contains(lch))
                {
                    reason = "invalid-channel";
                    msg = $"Proposal refused: LockChange channel '{lch}' is invalid.";
                    return false;
                }
                if (!TryReadRequiredBoundedInt(item, "vertexIndex", 0, 32, out int vIdx, out var vErr))
                {
                    reason = vErr.Reason; msg = vErr.Msg; return false;
                }
                if (!item.TryGetProperty("locked", out var lockedEl) || lockedEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    reason = "invalid-field:locked";
                    msg = "Proposal refused: LockChange locked must be boolean.";
                    return false;
                }
                bool locked = lockedEl.GetBoolean();
                op = new GeometryEditOp(GeometryEditOpKind.LockChange, lch, null, null, null, null, vIdx, locked, locked ? lockSource : null);
                return true;

            default:
                reason = "unknown-edit-op:" + opName;
                msg = $"Proposal refused: unsupported geometry-edit operation '{opName}'.";
                return false;
        }
    }

    private static bool TryParseWhatIfOffer(
        JsonElement wiEl,
        out WhatIfOffer? offer,
        out string reason,
        out string msg)
    {
        offer = null;
        reason = "";
        msg = "";
        HashSet<string> allowed = ["speedMps", "alphaDeg", "hRefMeters", "waterTemperatureC", "isSaltWater"];
        foreach (var p in wiEl.EnumerateObject())
        {
            if (!allowed.Contains(p.Name))
            {
                reason = "extra-field:whatIfOffer." + p.Name;
                msg = $"Proposal refused: field '{p.Name}' is not allowed in whatIfOffer (never invent computed metrics).";
                return false;
            }
        }

        if (!TryReadOptionalBoundedDouble(wiEl, "speedMps", 0.5, 40.0, out double? speed, out var e1)) { reason = e1.Reason; msg = e1.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(wiEl, "alphaDeg", -15.0, 25.0, out double? alpha, out var e2)) { reason = e2.Reason; msg = e2.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(wiEl, "hRefMeters", 0.05, 5.0, out double? href, out var e3)) { reason = e3.Reason; msg = e3.Msg; return false; }
        if (!TryReadOptionalBoundedDouble(wiEl, "waterTemperatureC", 0.0, 50.0, out double? temp, out var e4)) { reason = e4.Reason; msg = e4.Msg; return false; }
        bool? salt = null;
        if (wiEl.TryGetProperty("isSaltWater", out var sEl) && sEl.ValueKind != JsonValueKind.Null)
        {
            if (sEl.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                reason = "invalid-field:whatIfOffer.isSaltWater";
                msg = "Proposal refused: whatIfOffer.isSaltWater must be boolean.";
                return false;
            }
            salt = sEl.GetBoolean();
        }

        offer = new WhatIfOffer(speed, alpha, href, temp, salt);
        return true;
    }

    private static bool TryValidateCaseDiffChange(
        string fieldName,
        JsonElement valEl,
        out CaseDiffChange? change,
        out string reason,
        out string msg)
    {
        change = null;
        reason = "";
        msg = "";

        if (fieldName == "turbulenceModel")
        {
            if (valEl.ValueKind != JsonValueKind.String)
            {
                reason = "invalid-field:turbulenceModel";
                msg = "Proposal refused: turbulenceModel must be 'SpalartAllmaras' or 'KOmegaSst'.";
                return false;
            }
            string model = valEl.GetString()!.Trim();
            if (model is not ("SpalartAllmaras" or "KOmegaSst"))
            {
                reason = "out-of-range:turbulenceModel";
                msg = $"Proposal refused: turbulenceModel '{model}' must be 'SpalartAllmaras' or 'KOmegaSst'.";
                return false;
            }
            change = new CaseDiffChange(fieldName, model, null, model);
            return true;
        }

        if (fieldName is "maxIterations" or "averagingWindowIterations")
        {
            (int min, int max) = fieldName == "maxIterations" ? (100, 20_000) : (20, 2_000);
            if (valEl.ValueKind != JsonValueKind.Number || !valEl.TryGetInt32(out int iv) || iv < min || iv > max)
            {
                reason = "out-of-range:" + fieldName;
                msg = $"Proposal refused: {fieldName} must be an integer in [{min}, {max}] (never clamped).";
                return false;
            }
            change = new CaseDiffChange(fieldName, iv.ToString(CultureInfo.InvariantCulture), iv, null);
            return true;
        }

        (double dMin, double dMax, bool known) = fieldName switch
        {
            "upstreamChords" => (10.0, 50.0, true),
            "downstreamChords" => (20.0, 80.0, true),
            "lateralHalfSpans" => (5.0, 30.0, true),
            "verticalChords" => (10.0, 50.0, true),
            "layerExpansionRatio" => (1.05, 1.20, true),
            "clHalfBandCap" => (1e-5, 1e-2, true),
            "cdHalfBandCap" => (1e-5, 5e-3, true),
            _ => (0.0, 0.0, false)
        };

        if (!known)
        {
            reason = "unknown-case-field:" + fieldName;
            msg = $"Proposal refused: case-diff field '{fieldName}' is outside the closed TypedCaseModel subset.";
            return false;
        }

        if (valEl.ValueKind != JsonValueKind.Number || !valEl.TryGetDouble(out double dv) || !double.IsFinite(dv) || dv < dMin || dv > dMax)
        {
            reason = "out-of-range:" + fieldName;
            msg = string.Create(CultureInfo.InvariantCulture, $"Proposal refused: {fieldName} must be finite in [{dMin:G}, {dMax:G}] (never clamped).");
            return false;
        }

        change = new CaseDiffChange(fieldName, dv.ToString("G", CultureInfo.InvariantCulture), dv, null);
        return true;
    }

    private static bool TryParseProvenance(JsonElement root, out List<AssistantFieldProvenance> list, out string error)
    {
        list = [];
        error = "";
        if (!root.TryGetProperty("provenance", out var provEl) || provEl.ValueKind != JsonValueKind.Array || provEl.GetArrayLength() == 0)
        {
            error = "Proposal refused: non-empty provenance array is required.";
            return false;
        }

        foreach (var item in provEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                error = "Proposal refused: each provenance entry must be an object.";
                return false;
            }
            foreach (var p in item.EnumerateObject())
            {
                if (p.Name is not ("fieldName" or "tag" or "valueDisplay" or "reason"))
                {
                    error = $"Proposal refused: unknown provenance field '{p.Name}'.";
                    return false;
                }
            }
            if (!TryGetRequiredString(item, "fieldName", out string fn) ||
                !TryGetRequiredString(item, "tag", out string tagStr) ||
                !TryGetRequiredString(item, "valueDisplay", out string vd) ||
                !TryGetRequiredString(item, "reason", out string r))
            {
                error = "Proposal refused: provenance requires fieldName, tag, valueDisplay, and reason.";
                return false;
            }

            if (!Enum.TryParse<ProvenanceTag>(tagStr, ignoreCase: false, out var tag))
            {
                error = $"Proposal refused: provenance tag '{tagStr}' must be Stated, Inferred, or Defaulted.";
                return false;
            }

            list.Add(new AssistantFieldProvenance(fn.Trim(), tag, vd.Trim(), r.Trim()));
        }

        return true;
    }

    private static bool IsAdmittedProfileId(string profileId, IReadOnlySet<string>? admittedMySectionIds)
    {
        if (admittedMySectionIds is not null && admittedMySectionIds.Contains(profileId))
            return true;

        var catalog = Catalog.Load();
        foreach (var entry in catalog)
        {
            if (string.Equals(entry.Id, profileId, StringComparison.OrdinalIgnoreCase) &&
                entry.DisabledReason is null &&
                entry.Coordinates is not null)
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryParseStrictObject(
        string? rawJson,
        [NotNullWhen(true)] out JsonDocument? doc,
        [NotNullWhen(false)] out AssistantResponseValidation? error)
    {
        doc = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            error = Refused("invalid-json", "Proposal refused: empty JSON response.", null);
            return false;
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(rawJson);
        if (!CheckNoDuplicateJsonKeys(utf8, out string? dupKey, out string? syntaxError))
        {
            error = dupKey is not null
                ? Refused("duplicate-json-key", $"Proposal refused: duplicate JSON key '{dupKey}' is not allowed.", null)
                : Refused("invalid-json", $"Proposal refused: malformed JSON ({syntaxError}).", null);
            return false;
        }

        try
        {
            doc = JsonDocument.Parse(utf8, StrictJsonOptions);
        }
        catch (JsonException ex)
        {
            error = Refused("invalid-json", $"Proposal refused: malformed JSON ({ex.Message}).", null);
            return false;
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            doc.Dispose();
            doc = null;
            error = Refused("invalid-schema", "Proposal refused: JSON root must be an object.", null);
            return false;
        }
        return true;
    }

    private static bool CheckNoDuplicateJsonKeys(byte[] utf8, out string? duplicateKey, out string? syntaxError)
    {
        duplicateKey = null;
        syntaxError = null;
        var readerOptions = new JsonReaderOptions
        {
            MaxDepth = StrictJsonOptions.MaxDepth,
            CommentHandling = StrictJsonOptions.CommentHandling,
            AllowTrailingCommas = StrictJsonOptions.AllowTrailingCommas
        };

        var stack = new Stack<HashSet<string>>();
        try
        {
            var reader = new Utf8JsonReader(utf8, readerOptions);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        stack.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.EndObject:
                        if (stack.Count > 0) stack.Pop();
                        break;
                    case JsonTokenType.PropertyName:
                        string propName = reader.GetString() ?? "";
                        if (stack.Count > 0 && !stack.Peek().Add(propName))
                        {
                            duplicateKey = propName;
                            return false;
                        }
                        break;
                }
            }
            return true;
        }
        catch (JsonException ex)
        {
            syntaxError = ex.Message;
            return false;
        }
    }

    private static bool TryGetRequiredString(JsonElement obj, string name, out string value)
    {
        value = "";
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        string? raw = el.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        value = raw.Trim();
        return true;
    }

    private static bool TryReadNonEmptyStringArray(JsonElement obj, string name, out List<string> values)
    {
        values = [];
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array || el.GetArrayLength() == 0)
            return false;
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                return false;
            values.Add(item.GetString()!.Trim());
        }
        return values.Count > 0;
    }

    private static bool TryReadOptionalBoundedDouble(
        JsonElement obj,
        string name,
        double min,
        double max,
        out double? value,
        out (string Reason, string Msg) error)
    {
        value = null;
        error = default;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
            return true;

        if (el.ValueKind != JsonValueKind.Number || !el.TryGetDouble(out double d) || !double.IsFinite(d))
        {
            error = ("invalid-field:" + name, $"Proposal refused: {name} must be a finite number.");
            return false;
        }
        if (d < min || d > max)
        {
            error = ("out-of-range:" + name, string.Create(CultureInfo.InvariantCulture,
                $"Proposal refused: {name}={d:G} is outside allowed bounds [{min:G}, {max:G}] (never clamped)."));
            return false;
        }
        value = d;
        return true;
    }

    private static bool TryReadRequiredBoundedDouble(
        JsonElement obj,
        string name,
        double min,
        double max,
        out double value,
        out (string Reason, string Msg) error)
    {
        value = 0;
        error = default;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Number || !el.TryGetDouble(out double d) || !double.IsFinite(d))
        {
            error = ("invalid-field:" + name, $"Proposal refused: {name} must be a finite number in [{min.ToString("G", CultureInfo.InvariantCulture)}, {max.ToString("G", CultureInfo.InvariantCulture)}].");
            return false;
        }
        if (d < min || d > max)
        {
            error = ("out-of-range:" + name, string.Create(CultureInfo.InvariantCulture,
                $"Proposal refused: {name}={d:G} is outside allowed bounds [{min:G}, {max:G}] (never clamped)."));
            return false;
        }
        value = d;
        return true;
    }

    private static bool TryReadRequiredBoundedInt(
        JsonElement obj,
        string name,
        int min,
        int max,
        out int value,
        out (string Reason, string Msg) error)
    {
        value = 0;
        error = default;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out int iv))
        {
            error = ("invalid-field:" + name, $"Proposal refused: {name} must be an integer in [{min}, {max}].");
            return false;
        }
        if (iv < min || iv > max)
        {
            error = ("out-of-range:" + name, $"Proposal refused: {name}={iv} is outside allowed bounds [{min}, {max}] (never clamped).");
            return false;
        }
        value = iv;
        return true;
    }

    private static bool TryReadUniqueDoubleArray(
        JsonElement obj,
        string name,
        double min,
        double max,
        out List<double> values,
        out string error)
    {
        values = [];
        error = "";
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array || el.GetArrayLength() == 0)
        {
            error = $"Proposal refused: {name} must be a non-empty numeric array.";
            return false;
        }

        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetDouble(out double d) || !double.IsFinite(d))
            {
                error = $"Proposal refused: {name} contains a non-finite or non-numeric element.";
                return false;
            }
            if (d < min || d > max)
            {
                error = string.Create(CultureInfo.InvariantCulture, $"Proposal refused: {name} value {d:G} is outside [{min:G}, {max:G}].");
                return false;
            }
            if (values.Any(existing => Math.Abs(existing - d) < 1e-9))
            {
                error = string.Create(CultureInfo.InvariantCulture, $"Proposal refused: duplicate value {d:G} in {name}.");
                return false;
            }
            values.Add(d);
        }
        return true;
    }

    private static AssistantResponseValidation Refused(string reason, string message, string? fallbackText) =>
        new(AssistantVerdict.Refused, ProposalDisposition.Refused, reason, message, fallbackText ?? message, [], TriggersRun: false);

    private static AssistantResponseValidation Withheld(string reason, string message, string? fallbackText) =>
        new(AssistantVerdict.Withheld, ProposalDisposition.Withheld, reason, message, fallbackText ?? message, [], TriggersRun: false);
}
