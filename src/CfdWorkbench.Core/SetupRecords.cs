using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

// Area 1 Setup & Goal State durable records, canonical BLAKE3(JCS) content hashes, structural store checks,
// derived RecipeStatus, GoalStateSweepSnapshot conversion (Area 4 seam), and SetupTelemetry (Ruling 272 DR-SET-1..4).

/// <summary>Optional top-level <c>setup</c> member of a <c>cfdw-project-2</c> document. Append-only facts.</summary>
public sealed record SetupRecords(
    IReadOnlyList<SetupBriefVersion> Briefs,
    IReadOnlyList<GoalStateVersion> GoalStates,
    IReadOnlyList<DrcExclusion> DrcExclusions);

/// <summary>One weighted scalar soft target with explicit provenance (SET-01).</summary>
public sealed record SoftTargetScalar(
    double Value,
    double Weight,
    string Provenance,
    string Source);

/// <summary>Unweighted taper ratio input for the 4-CV planform family.</summary>
public sealed record SoftTargetTaper(
    double Value,
    string Provenance,
    string Source);

/// <summary>The soft-target set carried by a SetupBriefVersion and GoalStateVersion.</summary>
public sealed record SoftTargetSet(
    SoftTargetScalar? MaxSpanMeters,
    SoftTargetScalar? MaxChordMeters,
    SoftTargetScalar? TargetAreaSquareMeters,
    SoftTargetScalar? TargetAspectRatio,
    SoftTargetTaper Taper,
    string WeightBasis);

/// <summary>Explicit user acknowledgement of one Flagged default at seed time (SET-04, CAT-03, COPY-39).</summary>
public sealed record FlaggedAcknowledgement(
    string FieldPath,
    string Label,
    string Source,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? AcknowledgedAtUtc = null);

/// <summary>One target vs seeded deviation row recorded on a RecipeRecord (SET-01).</summary>
public sealed record RecipeDeviation(
    string Target,
    double TargetValue,
    double SeededValue,
    double RelativeDelta);

/// <summary>
/// Immutable provenance of the exact 2D solver output emitted one-way into FoilDSL 4.1.
/// Never a second writable geometry AST (GEO-04, ADR-0002).
/// </summary>
public sealed record RecipeRecord(
    string SolverVersion,
    string PlanformFamily,
    double SpanMeters,
    double RootChordMeters,
    double TipChordMeters,
    double Taper,
    double SweepQuarterChordDeg,
    double AnhedralDeg,
    double WashoutDeg,
    double RootThicknessRatio,
    string ProfileId,
    string ProfileOrigin,
    string? PreferredProfileId,
    string? FallbackReason,
    string? ConflictNotice,
    IReadOnlyList<RecipeDeviation> Deviations);

/// <summary>
/// Append-only Type-2 Setup brief version: one submitted brief that seeded one GoalStateVersion and one AcceptedRow.
/// </summary>
public sealed record SetupBriefVersion(
    string BriefId,
    string? Parent,
    string ContentHash,
    string EntryKind,
    string Purpose,
    string DisciplinePresetId,
    string DisciplinePresetVersion,
    string? Text,
    string? LanguageMeta,
    SoftTargetSet SoftTargets,
    IReadOnlyList<FlaggedAcknowledgement> FlaggedAcknowledgements,
    RecipeRecord Recipe,
    string SeededGoalStateId,
    string SeededAcceptedId);

/// <summary>Provenance + confidence + source triple attached to defaulted/inferred/stated Goal fields.</summary>
public sealed record FieldProvenance(string Provenance, string Confidence, string Source);

/// <summary>One additive mass breakdown item inside a GoalStateVersion.</summary>
public sealed record MassItem(
    string Id,
    string Label,
    double MassKg,
    string Provenance,
    string Confidence,
    string Source);

/// <summary>Submergence depth band carried by a GoalStateVersion (metres).</summary>
public sealed record GoalDepthBandRecord(
    double MinMeters,
    double MaxMeters,
    string Provenance,
    string Confidence,
    string Source);

/// <summary>Wind band carried by a GoalStateVersion (knots).</summary>
public sealed record GoalWindBandRecord(
    double MinKnots,
    double MaxKnots,
    string Provenance,
    string Confidence,
    string Source);

/// <summary>Speed band carried by a GoalStateVersion (m/s).</summary>
public sealed record GoalSpeedBandRecord(
    double TakeoffMinMps,
    double TakeoffMaxMps,
    double CruiseMinMps,
    double CruiseMaxMps,
    double TopMaxMps,
    string Provenance,
    string Confidence,
    string SourceLabel,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DisconfirmingNote = null);

/// <summary>Non-designed companion equipment context used for class-rule DRC checks (GWA/iQFOiL).</summary>
public sealed record EquipmentContextRecord(
    double RearAreaCm2,
    double MastLengthCm,
    double FuselageLengthCm,
    double FoilMassKg,
    string Provenance,
    string Confidence,
    string Source);

/// <summary>One target operating condition inside a GoalStateVersion (GOAL-01, GOAL-02, SWP seam).</summary>
public sealed record GoalOperatingPoint(
    string Id,
    string Label,
    double SpeedMetersPerSecond,
    double? HRefMeters,
    string Datum,
    double PAtmPa,
    double RequiredLoadN,
    string LoadSource,
    string CriticalMetric,
    IReadOnlyList<int> Ncrit,
    string SpeedLabel);

/// <summary>One feasibility constraint row inside a GoalStateVersion (GOAL-01).</summary>
public sealed record GoalConstraintRow(
    string Id,
    string Kind,
    string Label,
    string? Operator,
    double? Bound,
    double? BoundMin,
    double? BoundMax,
    string? Unit,
    string? PresetId,
    string? Severity,
    bool Enabled,
    bool EditedByUser,
    string Source);

/// <summary>
/// Append-only Type-2 Goal state version (GOAL-01..03). Immutable once appended; Area 4 sweeps pin
/// <c>(GoalStateId, ContentHash)</c>. Derived quantities (<see cref="SystemMassKg"/>, <see cref="SystemWeightN"/>)
/// are computed on read and never stored (DM7).
/// </summary>
public sealed record GoalStateVersion(
    string GoalStateId,
    string? Parent,
    string? BriefId,
    string ContentHash,
    string DisciplinePresetId,
    string ClassRulePresetId,
    string ClassRuleVersion,
    IReadOnlyList<MassItem> Masses,
    double FrontWingLoadN,
    FieldProvenance FrontWingLoadProvenance,
    WaterRecord Water,
    FieldProvenance WaterProvenance,
    GoalDepthBandRecord? DepthBand,
    GoalWindBandRecord? WindBand,
    GoalSpeedBandRecord SpeedBand,
    EquipmentContextRecord EquipmentContext,
    IReadOnlyList<GoalOperatingPoint> OperatingPoints,
    IReadOnlyList<GoalConstraintRow> Constraints,
    SoftTargetSet SoftTargets)
{
    public const double StandardGravity = 9.80665;

    [JsonIgnore]
    public double SystemMassKg => Masses.Sum(m => m.MassKg);

    [JsonIgnore]
    public double SystemWeightN => SystemMassKg * StandardGravity;

    /// <summary>Projects this immutable Goal state to the Area 4 sweeps read-only seam (Ruling 271 &amp; 272).</summary>
    public GoalStateSweepSnapshot ToSweepSnapshot() => new(
        GoalStateId,
        ContentHash,
        Water,
        OperatingPoints.Select(op => new GoalOperatingPointSnapshot(
            op.Id,
            op.Label,
            op.SpeedMetersPerSecond,
            op.HRefMeters,
            op.Datum,
            op.PAtmPa,
            op.RequiredLoadN,
            op.LoadSource,
            op.CriticalMetric,
            op.Ncrit.ToArray(),
            op.SpeedLabel)).ToArray(),
        DepthBand is null ? null : new GoalDepthBand(DepthBand.MinMeters, DepthBand.MaxMeters, DepthBand.Provenance),
        new GoalSpeedBand(
            SpeedBand.TakeoffMinMps,
            SpeedBand.TakeoffMaxMps,
            SpeedBand.CruiseMinMps,
            SpeedBand.CruiseMaxMps,
            SpeedBand.TopMaxMps,
            SpeedBand.Provenance,
            SpeedBand.SourceLabel,
            SpeedBand.DisconfirmingNote));
}

/// <summary>Append-only user exclusion of one located DRC finding on one DesignRevision (A5.8).</summary>
public sealed record DrcExclusion(
    string ExclusionId,
    string DesignId,
    string RuleId,
    string RuleVersion,
    string LocationKey,
    string Reason,
    string Attribution);

/// <summary>Derived link state between the active revision and the starter Setup recipe (GEO-04, zero shadow flag).</summary>
public enum RecipeLinkState
{
    None,
    Linked,
    DirectParametric
}

/// <summary>Derived starter-recipe status computed on read from accepted history and Setup briefs (DM7, GEO-04).</summary>
public sealed record RecipeProvenanceStatus(
    RecipeLinkState State,
    string? PresetId,
    string? PresetName,
    string? BriefId,
    string? SeededAcceptedId);

/// <summary>Normal-path Area 1 Setup telemetry fields (docs/design/setup-seeding.md §6, IO1–IO12; null = not recorded).</summary>
public sealed record SetupTelemetry
{
    public string? EntryKind { get; init; }
    public string? PresetId { get; init; }
    public string? ClassRuleId { get; init; }
    public string? ClassRuleVersion { get; init; }
    public int? BriefVersion { get; init; }
    public int? GoalVersion { get; init; }
    public int? RevisionOrdinal { get; init; }
    public int? FlaggedAcknowledgedCount { get; init; }
    public bool? FallbackUsed { get; init; }
    public bool? Conflict { get; init; }
    public double? MaxRelativeDeviation { get; init; }
    public bool? TipClampActive { get; init; }
    public int? CandidatesEvaluated { get; init; }
    public double? SolveMs { get; init; }
    public double? AssessMs { get; init; }
    public int? EditedConstraintCount { get; init; }
    public int? OperatingPointCount { get; init; }
    public bool? DepthBandChanged { get; init; }
    public bool? WaterChanged { get; init; }
    public int? ErrorCount { get; init; }
    public int? WarningCount { get; init; }
    public int? AdvisoryCount { get; init; }
    public int? ExcludedCount { get; init; }
    public string? SurfaceHash12 { get; init; }
    public string? GoalHash12 { get; init; }
    public int? OutputBytes { get; init; }
}

/// <summary>Result returned by <see cref="AuthoringSession.Seed"/>.</summary>
public sealed record SetupSeedOutcome(
    string AcceptedId,
    string SurfaceHash,
    SetupBriefVersion Brief,
    GoalStateVersion GoalState,
    bool IsReseed,
    int RevisionOrdinal);

/// <summary>
/// Canonical BLAKE3(JCS) content hashes, structural store checks, and derived RecipeStatus for Area 1 Setup.
/// </summary>
public static class SetupStore
{
    public const double SaltSalinityGPerKg = 35.16504;
    public const double MinWaterTempC = 0.0;
    public const double MaxWaterTempC = 50.0;

    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    /// <summary>BLAKE3 over JCS of the full <see cref="SetupBriefVersion"/> without <c>contentHash</c>.</summary>
    public static string BriefContentHash(SetupBriefVersion brief)
    {
        ArgumentNullException.ThrowIfNull(brief);
        return Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(brief, Options), drop: "contentHash"));
    }

    /// <summary>
    /// BLAKE3 over JCS of the canonical semantic Goal state payload (every physical/intent field, operating points,
    /// constraints, water, masses, bands, equipment context, and soft-target numbers/weights). Excludes surrogate row
    /// UUIDs (<c>goalStateId</c>, <c>parent</c>, <c>briefId</c>), <c>contentHash</c>, and entry-channel provenance
    /// strings so GUI form, CLI, and language-proposal fixtures with identical physical inputs produce the exact same
    /// <c>GoalStateVersion.ContentHash</c> (SET-03, CLI-01, T5, T13) while any tamper to a Goal field triggers
    /// <c>DOC-INTEGRITY</c> (T12).
    /// </summary>
    public static string GoalContentHash(GoalStateVersion goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var manifest = new Dictionary<string, object?>
        {
            ["disciplinePresetId"] = goal.DisciplinePresetId,
            ["classRulePresetId"] = goal.ClassRulePresetId,
            ["classRuleVersion"] = goal.ClassRuleVersion,
            ["masses"] = goal.Masses.Select(m => (object?)new Dictionary<string, object?>
            {
                ["id"] = m.Id,
                ["label"] = m.Label,
                ["massKg"] = m.MassKg
            }).ToList(),
            ["frontWingLoadN"] = goal.FrontWingLoadN,
            ["water"] = new Dictionary<string, object?>
            {
                ["temperatureC"] = goal.Water.TemperatureC,
                ["salinityGPerKg"] = goal.Water.SalinityGPerKg,
                ["rho"] = goal.Water.Rho,
                ["nu"] = goal.Water.Nu,
                ["pv"] = goal.Water.Pv,
                ["source"] = goal.Water.Source,
                ["tableHash"] = goal.Water.TableHash
            },
            ["depthBand"] = goal.DepthBand is null ? null : new Dictionary<string, object?>
            {
                ["minMeters"] = goal.DepthBand.MinMeters,
                ["maxMeters"] = goal.DepthBand.MaxMeters
            },
            ["windBand"] = goal.WindBand is null ? null : new Dictionary<string, object?>
            {
                ["minKnots"] = goal.WindBand.MinKnots,
                ["maxKnots"] = goal.WindBand.MaxKnots
            },
            ["speedBand"] = new Dictionary<string, object?>
            {
                ["takeoffMinMps"] = goal.SpeedBand.TakeoffMinMps,
                ["takeoffMaxMps"] = goal.SpeedBand.TakeoffMaxMps,
                ["cruiseMinMps"] = goal.SpeedBand.CruiseMinMps,
                ["cruiseMaxMps"] = goal.SpeedBand.CruiseMaxMps,
                ["topMaxMps"] = goal.SpeedBand.TopMaxMps
            },
            ["equipmentContext"] = new Dictionary<string, object?>
            {
                ["rearAreaCm2"] = goal.EquipmentContext.RearAreaCm2,
                ["mastLengthCm"] = goal.EquipmentContext.MastLengthCm,
                ["fuselageLengthCm"] = goal.EquipmentContext.FuselageLengthCm,
                ["foilMassKg"] = goal.EquipmentContext.FoilMassKg
            },
            ["operatingPoints"] = goal.OperatingPoints.Select(op => (object?)new Dictionary<string, object?>
            {
                ["id"] = op.Id,
                ["label"] = op.Label,
                ["speedMetersPerSecond"] = op.SpeedMetersPerSecond,
                ["hRefMeters"] = op.HRefMeters,
                ["datum"] = op.Datum,
                ["pAtmPa"] = op.PAtmPa,
                ["requiredLoadN"] = op.RequiredLoadN,
                ["loadSource"] = op.LoadSource,
                ["criticalMetric"] = op.CriticalMetric,
                ["ncrit"] = op.Ncrit.Select(n => (object?)(double)n).ToList(),
                ["speedLabel"] = op.SpeedLabel
            }).ToList(),
            ["constraints"] = goal.Constraints.Select(c => (object?)new Dictionary<string, object?>
            {
                ["id"] = c.Id,
                ["kind"] = c.Kind,
                ["label"] = c.Label,
                ["operator"] = c.Operator,
                ["bound"] = c.Bound,
                ["boundMin"] = c.BoundMin,
                ["boundMax"] = c.BoundMax,
                ["unit"] = c.Unit,
                ["presetId"] = c.PresetId,
                ["severity"] = c.Severity,
                ["enabled"] = c.Enabled,
                ["editedByUser"] = c.EditedByUser,
                ["source"] = c.Source
            }).ToList(),
            ["softTargets"] = new Dictionary<string, object?>
            {
                ["maxSpanMeters"] = CanonicalTarget(goal.SoftTargets.MaxSpanMeters),
                ["maxChordMeters"] = CanonicalTarget(goal.SoftTargets.MaxChordMeters),
                ["targetAreaSquareMeters"] = CanonicalTarget(goal.SoftTargets.TargetAreaSquareMeters),
                ["targetAspectRatio"] = CanonicalTarget(goal.SoftTargets.TargetAspectRatio),
                ["taper"] = goal.SoftTargets.Taper.Value,
                ["weightBasis"] = goal.SoftTargets.WeightBasis
            }
        };
        return Blake3Jcs(manifest);
    }

    private static Dictionary<string, object?>? CanonicalTarget(SoftTargetScalar? target) =>
        target is null ? null : new Dictionary<string, object?>
        {
            ["value"] = target.Value,
            ["weight"] = target.Weight
        };

    /// <summary>Derives starter-recipe link status strictly from accepted history and briefs (zero shadow flag, GEO-04).</summary>
    public static RecipeProvenanceStatus DeriveRecipeStatus(
        string? currentAcceptedId,
        IReadOnlyList<AcceptedRow> acceptedRows,
        IReadOnlyList<SetupBriefVersion> briefs)
    {
        if (currentAcceptedId is null || briefs.Count == 0)
            return new RecipeProvenanceStatus(RecipeLinkState.None, null, null, null, null);

        var byId = acceptedRows.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var briefBySeeded = new Dictionary<string, SetupBriefVersion>(StringComparer.Ordinal);
        foreach (var brief in briefs)
            briefBySeeded[brief.SeededAcceptedId] = brief;

        // Walk the active parent chain from current back to root.
        string? cursor = currentAcceptedId;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (cursor is not null && visited.Add(cursor))
        {
            if (briefBySeeded.TryGetValue(cursor, out var matched))
            {
                string presetName = DisciplinePresets.TryGet(matched.DisciplinePresetId)?.Name ?? matched.DisciplinePresetId;
                var state = string.Equals(cursor, currentAcceptedId, StringComparison.Ordinal)
                    ? RecipeLinkState.Linked
                    : RecipeLinkState.DirectParametric;
                return new RecipeProvenanceStatus(state, matched.DisciplinePresetId, presetName, matched.BriefId, matched.SeededAcceptedId);
            }
            cursor = byId.TryGetValue(cursor, out var row) ? row.Parent : null;
        }
        return new RecipeProvenanceStatus(RecipeLinkState.None, null, null, null, null);
    }

    /// <summary>
    /// Structural and integrity invariants checked both on native read (<see cref="NativeProject"/>) and on write
    /// inside <see cref="AuthoringSession"/>.
    /// </summary>
    internal static void Check(
        SetupRecords setup,
        Func<string, bool> acceptedExists,
        Func<string, bool> designExists)
    {
        Guard.Require(setup is not null && setup.Briefs is not null && setup.GoalStates is not null && setup.DrcExclusions is not null, "DOC-SCHEMA");
        var briefs = setup!.Briefs!;
        var goals = setup.GoalStates!;
        var exclusions = setup.DrcExclusions!;
        Guard.Require(briefs.Count > 0 || goals.Count > 0 || exclusions.Count > 0, "DOC-SCHEMA");

        var briefIds = new HashSet<string>(StringComparer.Ordinal);
        var goalIds = new HashSet<string>(StringComparer.Ordinal);
        var exclusionIds = new HashSet<string>(StringComparer.Ordinal);

        // First register goalStateIds while checking goal states in append order.
        foreach (var goal in goals)
        {
            Guard.Require(goal is not null, "DOC-SCHEMA");
            NativeProject.Uuid(goal!.GoalStateId);
            Guard.Require(goalIds.Add(goal.GoalStateId), "DOC-SCHEMA");
            if (goal.Parent is not null)
            {
                NativeProject.Uuid(goal.Parent);
                Guard.Require(goalIds.Contains(goal.Parent) && goal.Parent != goal.GoalStateId, "DOC-REFERENCE");
            }
            if (goal.BriefId is not null) NativeProject.Uuid(goal.BriefId);
            RequireHash(goal.ContentHash);
            CheckGoalState(goal);
            Guard.Require(string.Equals(goal.ContentHash, GoalContentHash(goal), StringComparison.Ordinal), "DOC-INTEGRITY");
        }

        foreach (var brief in briefs)
        {
            Guard.Require(brief is not null, "DOC-SCHEMA");
            NativeProject.Uuid(brief!.BriefId);
            Guard.Require(briefIds.Add(brief.BriefId), "DOC-SCHEMA");
            if (brief.Parent is not null)
            {
                NativeProject.Uuid(brief.Parent);
                Guard.Require(briefIds.Contains(brief.Parent) && brief.Parent != brief.BriefId, "DOC-REFERENCE");
            }
            RequireHash(brief.ContentHash);
            Guard.Require(brief.EntryKind is "parameters" or "language", "DOC-SCHEMA");
            Guard.Require(!string.IsNullOrWhiteSpace(brief.Purpose) && !string.IsNullOrWhiteSpace(brief.DisciplinePresetId) &&
                          !string.IsNullOrWhiteSpace(brief.DisciplinePresetVersion), "DOC-SCHEMA");
            CheckSoftTargets(brief.SoftTargets);
            Guard.Require(brief.FlaggedAcknowledgements is not null, "DOC-SCHEMA");
            foreach (var ack in brief.FlaggedAcknowledgements!)
            {
                Guard.Require(ack is not null && !string.IsNullOrWhiteSpace(ack.FieldPath) &&
                              !string.IsNullOrWhiteSpace(ack.Label) && !string.IsNullOrWhiteSpace(ack.Source), "DOC-SCHEMA");
            }
            CheckRecipe(brief.Recipe);
            NativeProject.Uuid(brief.SeededAcceptedId);
            NativeProject.Uuid(brief.SeededGoalStateId);
            Guard.Require(acceptedExists(brief.SeededAcceptedId), "DOC-REFERENCE");
            Guard.Require(goalIds.Contains(brief.SeededGoalStateId), "DOC-REFERENCE");
            Guard.Require(string.Equals(brief.ContentHash, BriefContentHash(brief), StringComparison.Ordinal), "DOC-INTEGRITY");
        }

        // Verify goal.BriefId references resolve to existing briefs.
        foreach (var goal in goals)
        {
            if (goal.BriefId is not null)
                Guard.Require(briefIds.Contains(goal.BriefId), "DOC-REFERENCE");
        }

        foreach (var ex in exclusions)
        {
            Guard.Require(ex is not null, "DOC-SCHEMA");
            NativeProject.Uuid(ex!.ExclusionId);
            Guard.Require(exclusionIds.Add(ex.ExclusionId), "DOC-SCHEMA");
            NativeProject.Uuid(ex.DesignId);
            Guard.Require(designExists(ex.DesignId), "DOC-REFERENCE");
            Guard.Require(!string.IsNullOrWhiteSpace(ex.RuleId) && !string.IsNullOrWhiteSpace(ex.RuleVersion) &&
                          !string.IsNullOrWhiteSpace(ex.LocationKey) && !string.IsNullOrWhiteSpace(ex.Reason) &&
                          ex.Attribution is not null, "DOC-SCHEMA");
        }
    }

    internal static void CheckGoalState(GoalStateVersion goal)
    {
        Guard.Require(!string.IsNullOrWhiteSpace(goal.DisciplinePresetId) &&
                      !string.IsNullOrWhiteSpace(goal.ClassRulePresetId) &&
                      !string.IsNullOrWhiteSpace(goal.ClassRuleVersion), "DOC-SCHEMA");
        Guard.Require(goal.Masses is { Count: > 0 }, "DOC-SCHEMA");
        double totalMass = 0;
        var massIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in goal.Masses)
        {
            Guard.Require(m is not null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Label) &&
                          massIds.Add(m.Id) && double.IsFinite(m.MassKg) && m.MassKg >= 0 &&
                          !string.IsNullOrWhiteSpace(m.Provenance) && !string.IsNullOrWhiteSpace(m.Confidence) &&
                          !string.IsNullOrWhiteSpace(m.Source), "DOC-SCHEMA");
            totalMass += m!.MassKg;
        }
        Guard.Require(totalMass > 0, "DOC-SCHEMA");
        Guard.Require(double.IsFinite(goal.FrontWingLoadN) && goal.FrontWingLoadN > 0, "DOC-SCHEMA");
        CheckProvenance(goal.FrontWingLoadProvenance);
        CheckWater(goal.Water);
        CheckProvenance(goal.WaterProvenance);

        if (goal.DepthBand is { } depth)
        {
            Guard.Require(double.IsFinite(depth.MinMeters) && double.IsFinite(depth.MaxMeters) &&
                          depth.MinMeters > 0 && depth.MaxMeters >= depth.MinMeters &&
                          !string.IsNullOrWhiteSpace(depth.Provenance) && !string.IsNullOrWhiteSpace(depth.Confidence) &&
                          !string.IsNullOrWhiteSpace(depth.Source), "DOC-SCHEMA");
        }
        if (goal.WindBand is { } wind)
        {
            Guard.Require(double.IsFinite(wind.MinKnots) && double.IsFinite(wind.MaxKnots) &&
                          wind.MinKnots >= 0 && wind.MaxKnots >= wind.MinKnots &&
                          !string.IsNullOrWhiteSpace(wind.Provenance) && !string.IsNullOrWhiteSpace(wind.Confidence) &&
                          !string.IsNullOrWhiteSpace(wind.Source), "DOC-SCHEMA");
        }
        Guard.Require(goal.SpeedBand is not null, "DOC-SCHEMA");
        var sp = goal.SpeedBand!;
        Guard.Require(double.IsFinite(sp.TakeoffMinMps) && double.IsFinite(sp.TakeoffMaxMps) &&
                      double.IsFinite(sp.CruiseMinMps) && double.IsFinite(sp.CruiseMaxMps) && double.IsFinite(sp.TopMaxMps) &&
                      sp.TakeoffMinMps > 0 && sp.TakeoffMaxMps >= sp.TakeoffMinMps &&
                      sp.CruiseMinMps > 0 && sp.CruiseMaxMps >= sp.CruiseMinMps &&
                      sp.TopMaxMps >= sp.CruiseMaxMps &&
                      !string.IsNullOrWhiteSpace(sp.Provenance) && !string.IsNullOrWhiteSpace(sp.Confidence) &&
                      !string.IsNullOrWhiteSpace(sp.SourceLabel), "DOC-SCHEMA");

        Guard.Require(goal.EquipmentContext is not null, "DOC-SCHEMA");
        var eq = goal.EquipmentContext!;
        Guard.Require(double.IsFinite(eq.RearAreaCm2) && eq.RearAreaCm2 > 0 &&
                      double.IsFinite(eq.MastLengthCm) && eq.MastLengthCm > 0 &&
                      double.IsFinite(eq.FuselageLengthCm) && eq.FuselageLengthCm > 0 &&
                      double.IsFinite(eq.FoilMassKg) && eq.FoilMassKg > 0 &&
                      !string.IsNullOrWhiteSpace(eq.Provenance) && !string.IsNullOrWhiteSpace(eq.Confidence) &&
                      !string.IsNullOrWhiteSpace(eq.Source), "DOC-SCHEMA");

        Guard.Require(goal.OperatingPoints is { Count: > 0 }, "DOC-SCHEMA");
        var opIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var op in goal.OperatingPoints)
        {
            Guard.Require(op is not null && !string.IsNullOrWhiteSpace(op.Id) && opIds.Add(op.Id) &&
                          !string.IsNullOrWhiteSpace(op.Label) && !string.IsNullOrWhiteSpace(op.Datum) &&
                          double.IsFinite(op.SpeedMetersPerSecond) && op.SpeedMetersPerSecond > 0 &&
                          double.IsFinite(op.PAtmPa) && op.PAtmPa > 0 &&
                          double.IsFinite(op.RequiredLoadN) && op.RequiredLoadN > 0 &&
                          (op.HRefMeters is null || (double.IsFinite(op.HRefMeters.Value) && op.HRefMeters.Value > 0)) &&
                          op.Ncrit is { Count: > 0 } && op.Ncrit.All(n => n > 0) &&
                          !string.IsNullOrWhiteSpace(op.LoadSource) && !string.IsNullOrWhiteSpace(op.CriticalMetric) &&
                          !string.IsNullOrWhiteSpace(op.SpeedLabel), "DOC-SCHEMA");
        }

        Guard.Require(goal.Constraints is not null, "DOC-SCHEMA");
        var cIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in goal.Constraints!)
        {
            Guard.Require(c is not null && !string.IsNullOrWhiteSpace(c.Id) && cIds.Add(c.Id) &&
                          !string.IsNullOrWhiteSpace(c.Kind) && !string.IsNullOrWhiteSpace(c.Label) &&
                          !string.IsNullOrWhiteSpace(c.Source) &&
                          (c.Bound is null || double.IsFinite(c.Bound.Value)) &&
                          (c.BoundMin is null || double.IsFinite(c.BoundMin.Value)) &&
                          (c.BoundMax is null || double.IsFinite(c.BoundMax.Value)), "DOC-SCHEMA");
        }

        CheckSoftTargets(goal.SoftTargets);
    }

    internal static void CheckSoftTargets(SoftTargetSet targets)
    {
        Guard.Require(targets is not null && targets.Taper is not null && !string.IsNullOrWhiteSpace(targets.WeightBasis), "DOC-SCHEMA");
        var taper = targets!.Taper!;
        Guard.Require(double.IsFinite(taper.Value) && taper.Value is >= 0.02 and <= 1.0 &&
                      !string.IsNullOrWhiteSpace(taper.Provenance) && !string.IsNullOrWhiteSpace(taper.Source), "DOC-SCHEMA");
        double weightSum = 0;
        foreach (var scalar in new[] { targets.MaxSpanMeters, targets.MaxChordMeters, targets.TargetAreaSquareMeters, targets.TargetAspectRatio })
        {
            if (scalar is null) continue;
            Guard.Require(double.IsFinite(scalar.Value) && scalar.Value > 0 &&
                          double.IsFinite(scalar.Weight) && scalar.Weight >= 0 &&
                          !string.IsNullOrWhiteSpace(scalar.Provenance) && !string.IsNullOrWhiteSpace(scalar.Source), "DOC-SCHEMA");
            weightSum += scalar.Weight;
        }
        Guard.Require(weightSum > 0, "DOC-SCHEMA");
    }

    private static void CheckRecipe(RecipeRecord recipe)
    {
        Guard.Require(recipe is not null && !string.IsNullOrWhiteSpace(recipe.SolverVersion) &&
                      !string.IsNullOrWhiteSpace(recipe.PlanformFamily) &&
                      !string.IsNullOrWhiteSpace(recipe.ProfileId) && !string.IsNullOrWhiteSpace(recipe.ProfileOrigin) &&
                      recipe.Deviations is not null, "DOC-SCHEMA");
        var r = recipe!;
        Guard.Require(double.IsFinite(r.SpanMeters) && r.SpanMeters > 0 &&
                      double.IsFinite(r.RootChordMeters) && r.RootChordMeters > 0 &&
                      double.IsFinite(r.TipChordMeters) && r.TipChordMeters > 0 &&
                      double.IsFinite(r.Taper) && r.Taper is >= 0.02 and <= 1.0 &&
                      double.IsFinite(r.SweepQuarterChordDeg) &&
                      double.IsFinite(r.AnhedralDeg) &&
                      double.IsFinite(r.WashoutDeg) &&
                      double.IsFinite(r.RootThicknessRatio) && r.RootThicknessRatio is > 0 and < 1, "DOC-SCHEMA");
        foreach (var dev in r.Deviations!)
        {
            Guard.Require(dev is not null && !string.IsNullOrWhiteSpace(dev.Target) &&
                          double.IsFinite(dev.TargetValue) && double.IsFinite(dev.SeededValue) && double.IsFinite(dev.RelativeDelta), "DOC-SCHEMA");
        }
    }

    private static void CheckProvenance(FieldProvenance prov) =>
        Guard.Require(prov is not null && !string.IsNullOrWhiteSpace(prov.Provenance) &&
                      !string.IsNullOrWhiteSpace(prov.Confidence) && !string.IsNullOrWhiteSpace(prov.Source), "DOC-SCHEMA");

    private static void CheckWater(WaterRecord water)
    {
        Guard.Require(water is not null && !string.IsNullOrWhiteSpace(water.Source), "DOC-SCHEMA");
        var w = water!;
        RequireHash(w.TableHash);
        Guard.Require(double.IsFinite(w.TemperatureC) && w.TemperatureC is >= MinWaterTempC and <= MaxWaterTempC &&
                      double.IsFinite(w.SalinityGPerKg) && w.SalinityGPerKg is >= 0 and <= SaltSalinityGPerKg &&
                      double.IsFinite(w.Rho) && w.Rho > 0 &&
                      double.IsFinite(w.Nu) && w.Nu > 0 &&
                      double.IsFinite(w.Pv) && w.Pv >= 0, "DOC-SCHEMA");
    }

    private static void RequireHash(string? hash) => Guard.Require(hash is not null && Hex64.IsMatch(hash), "DOC-INTEGRITY");

    private static string Blake3Jcs(object? canonical) => Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(canonical)));

    private static object? Canonical(JsonElement element, string? drop) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Where(member => member.Name != drop)
            .ToDictionary(member => member.Name, member => Canonical(member.Value, null), StringComparer.Ordinal) as IDictionary<string, object?>,
        JsonValueKind.Array => element.EnumerateArray().Select(item => Canonical(item, null)).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
