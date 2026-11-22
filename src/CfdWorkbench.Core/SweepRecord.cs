using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

// Area 4 Local-Tier Sweeps & Performance Curves (docs/design/local-sweeps.md §1, Ruling 271).
// Core holds the durable records, GoalStateSweepSnapshot seam, canonical hashes, structural store checks,
// and SweepTelemetry fields because NativeProject lives in Core and Core cannot reference CfdWorkbench.Analysis.

/// <summary>Depth band from Area 1 Goal state (aligned with docs/design/setup-seeding.md §1.6).</summary>
public sealed record GoalDepthBand(double MinMeters, double MaxMeters, string Provenance);

/// <summary>Speed band from Area 1 Goal state (aligned with docs/design/setup-seeding.md §1.6).</summary>
public sealed record GoalSpeedBand(
    double TakeoffMinMps,
    double TakeoffMaxMps,
    double CruiseMinMps,
    double CruiseMaxMps,
    double TopMaxMps,
    string Provenance,
    string SourceLabel,
    string? DisconfirmingNote = null);

/// <summary>One operating point in a read-only Goal-state snapshot for sweeps (Ruling 271, §1.6).</summary>
public sealed record GoalOperatingPointSnapshot(
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

/// <summary>
/// Read-only seam snapshot of an Area 1 Goal state consumed by Area 4 sweeps (Ruling 271).
/// <see cref="AuthoringSession.CurrentGoalSweepSnapshot"/> returns null until Wave C (SET-BUILD) wires live Goal states.
/// </summary>
public sealed record GoalStateSweepSnapshot(
    string GoalStateId,
    string ContentHash,
    WaterRecord Water,
    IReadOnlyList<GoalOperatingPointSnapshot> OperatingPoints,
    GoalDepthBand? DepthBand,
    GoalSpeedBand? SpeedBand);

/// <summary>Pinned accepted geometry reference on a sweep definition.</summary>
public sealed record SweepDesignRef(string AcceptedId, string SurfaceHash);

/// <summary>Pinned Goal-state version reference on a goal-state sweep definition.</summary>
public sealed record SweepGoalStateRef(string GoalStateId, string ContentHash);

/// <summary>Authored inputs of a sweep experiment; immutable once Queued (XS-03).</summary>
public sealed record SweepDefinition(
    string SourceKind,
    string Tier,
    SweepDesignRef DesignRef,
    SweepGoalStateRef? GoalStateRef,
    WaterRecord Water,
    double? HRefMeters,
    string Datum,
    double PAtm,
    IReadOnlyList<double> SpeedsMetersPerSecond,
    IReadOnlyList<double> AnglesDeg,
    RunMethod Method,
    string SettingsHash);

/// <summary>Backend pin copied at Queue (XS-03); local in-process tiers use id "local", substrate "in-process".</summary>
public sealed record BackendPin(
    string Id,
    string Substrate,
    string Tier,
    string MethodId,
    string MethodVersion,
    string SettingsHash);

/// <summary>One resolved unique operating condition inside a sweep's CaseSchedule, frozen at Queue.</summary>
public sealed record SweepCase(
    int CaseIndex,
    string CaseKey,
    string? Label,
    double SpeedMetersPerSecond,
    double? HRefMeters,
    string Datum,
    double PAtm,
    double? AlphaDeg,
    double? TargetLoadN);

/// <summary>Lifecycle transition kind on a <see cref="SweepExperiment"/>.</summary>
[JsonConverter(typeof(SweepEventKindConverter))]
public enum SweepEventKind { Queued, CancelRequested, Archived }

/// <summary>State transition on a <see cref="SweepAttempt"/>.</summary>
[JsonConverter(typeof(SweepAttemptStateConverter))]
public enum SweepAttemptState { Pending, Running, Completed, Failed, Cancelled }

/// <summary>One append-only lifecycle event on a sweep experiment.</summary>
public sealed record ExperimentEvent(int Sequence, SweepEventKind Kind, DateTimeOffset AtUtc, string? Reason = null);

/// <summary>One append-only state transition on a sweep attempt.</summary>
public sealed record AttemptStateEvent(int Sequence, SweepAttemptState State, DateTimeOffset AtUtc, string? Code = null, string? Reason = null);

/// <summary>FindAlpha provenance recorded on a Completed goal-state sweep attempt.</summary>
public sealed record SolvedAlphaRecord(double AlphaDeg, double LowerDeg, double UpperDeg, int Iterations, int Solves, string Basis);

/// <summary>
/// One execution attempt of one <see cref="SweepCase"/>. Retries append a new row with contiguous <see cref="AttemptIndex"/>
/// and <see cref="PriorAttemptId"/>. Completed attempts carry a non-null <see cref="RunId"/>; non-Completed attempts carry null.
/// </summary>
public sealed record SweepAttempt(
    string AttemptId,
    int CaseIndex,
    int AttemptIndex,
    string? PriorAttemptId,
    string? RunId,
    SolvedAlphaRecord? SolvedAlpha,
    double? WallMs,
    IReadOnlyList<AttemptStateEvent> Events)
{
    [JsonIgnore]
    public SweepAttemptState State => Events.Count > 0 ? Events[^1].State : SweepAttemptState.Pending;
}

/// <summary>
/// Aggregate root of a local-tier sweep experiment stored under optional top-level <c>experiments</c> on <c>cfdw-project-2</c>.
/// </summary>
public sealed record SweepExperiment(
    string Id,
    string SeriesId,
    int Version,
    string? ParentExperimentId,
    string Kind,
    string Name,
    string DefinitionHash,
    SweepDefinition Definition,
    BackendPin? BackendPin,
    IReadOnlyList<SweepCase> Cases,
    IReadOnlyList<ExperimentEvent> Events,
    IReadOnlyList<SweepAttempt> Attempts)
{
    [JsonIgnore]
    public bool IsQueued => Events.Any(ev => ev.Kind == SweepEventKind.Queued);

    [JsonIgnore]
    public bool IsArchived => Events.Any(ev => ev.Kind == SweepEventKind.Archived);

    /// <summary>Derived selected attempt for <paramref name="caseIndex"/> (highest <c>attemptIndex</c>, never stored).</summary>
    public SweepAttempt? SelectedAttempt(int caseIndex)
    {
        SweepAttempt? best = null;
        foreach (var attempt in Attempts)
        {
            if (attempt.CaseIndex == caseIndex && (best is null || attempt.AttemptIndex > best.AttemptIndex))
                best = attempt;
        }
        return best;
    }

    /// <summary>Run IDs referenced by the selected (latest) Completed attempt of each case.</summary>
    public IEnumerable<string> SelectedCompletedRunIds()
    {
        for (int i = 0; i < Cases.Count; i++)
        {
            var selected = SelectedAttempt(i);
            if (selected is not null && selected.State == SweepAttemptState.Completed && selected.RunId is not null)
                yield return selected.RunId;
        }
    }
}

/// <summary>Per-experiment integrity recomputed on every read (stored hashes are never trusted).</summary>
public enum SweepIntegrity { Intact, PayloadFailedCheck }

/// <summary>One stored sweep experiment and its recomputed integrity.</summary>
public sealed record StoredSweepExperiment(SweepExperiment Experiment, SweepIntegrity Integrity);

/// <summary>Normal-path sweep telemetry fields (docs/design/local-sweeps.md §6, IO1–IO12; null means not recorded).</summary>
public sealed record SweepTelemetry
{
    public string? ExperimentId { get; init; }
    public int? Version { get; init; }
    public string? SourceKind { get; init; }
    public string? Tier { get; init; }
    public int? CaseCount { get; init; }
    public int? EstimatedBytes { get; init; }
    public int? DocumentBytes { get; init; }
    public int? TotalCases { get; init; }
    public int? CompletedCases { get; init; }
    public int? FailedCases { get; init; }
    public int? CacheHits { get; init; }
    public bool? Cancelled { get; init; }
    public double? WallMs { get; init; }
    public double? CancelLatencyMs { get; init; }
    public int? CaseIndex { get; init; }
    public int? AttemptIndex { get; init; }
    public string? PriorAttemptId { get; init; }
    public string? RunKey12 { get; init; }
    public bool? IdempotentHit { get; init; }
    public int? FindAlphaSolves { get; init; }
    public int? ReleasedRuns { get; init; }
    public int? PinnedRunsAfter { get; init; }
    public int? DocumentBytesBefore { get; init; }
    public int? DocumentBytesAfter { get; init; }
    public string? ActiveChart { get; init; }
    public string? XAxis { get; init; }
    public int? CasesShown { get; init; }
    public int? Gaps { get; init; }
}

/// <summary>Canonical hashes, constants, and structural store invariants for sweep experiments (Ruling 271).</summary>
public static class SweepRecords
{
    public const int MaxCasesPerSweep = 120;
    public const int MaxSpeeds = 30;
    public const int MaxAngles = 30;
    public const int MaxNameLength = 120;
    public const string KindSweep = "sweep";
    public const string SourceCartesian = "cartesian";
    public const string SourceGoalState = "goal-state";
    public const string TierVlmStrip = "vlm-strip";
    public const string TierEstimator = "estimator";
    public const string LocalBackendId = "local";
    public const string LocalSubstrate = "in-process";

    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    /// <summary>BLAKE3 over JCS of the canonical <see cref="SweepDefinition"/>.</summary>
    public static string DefinitionHash(SweepDefinition definition) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(definition, Options)));

    /// <summary>BLAKE3 over JCS of one case's physical operating inputs.</summary>
    public static string CaseKey(double speedMetersPerSecond, double? hRefMeters, string datum, double pAtm, double? alphaDeg, double? targetLoadN)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["speedMetersPerSecond"] = speedMetersPerSecond,
            ["hRefMeters"] = hRefMeters,
            ["datum"] = datum,
            ["pAtm"] = pAtm,
            ["alphaDeg"] = alphaDeg,
            ["targetLoadN"] = targetLoadN
        };
        return Blake3Jcs(manifest);
    }

    public static string CaseKey(SweepCase item) =>
        CaseKey(item.SpeedMetersPerSecond, item.HRefMeters, item.Datum, item.PAtm, item.AlphaDeg, item.TargetLoadN);

    public static SweepExperiment Seal(SweepExperiment experiment) =>
        experiment with { DefinitionHash = DefinitionHash(experiment.Definition) };

    /// <summary>
    /// Recomputes <see cref="SweepExperiment.DefinitionHash"/> and every case's <see cref="SweepCase.CaseKey"/>.
    /// Never trusts stored hashes on read (ADR-0011 §4, §1.4 #5).
    /// </summary>
    public static bool Verify(SweepExperiment experiment)
    {
        try
        {
            if (experiment.DefinitionHash != DefinitionHash(experiment.Definition)) return false;
            foreach (var item in experiment.Cases)
            {
                if (item.CaseKey != CaseKey(item)) return false;
            }
            return true;
        }
        catch (ContractError) { return false; }
    }

    /// <summary>
    /// Structural store invariants (§1.4 #1–#9, Ruling 271), shared by <c>NativeProject.Check</c> and
    /// <c>AuthoringSession</c> mutations. Per-experiment hash integrity is checked on write and surfaced as
    /// <see cref="SweepIntegrity.PayloadFailedCheck"/> on read without deleting data.
    /// </summary>
    internal static void Check(IReadOnlyList<SweepExperiment> experiments, AnalysisRecords? analysis,
        Func<string, bool> acceptedExists, Func<string, string?> surfaceHashOf)
    {
        Guard.Require(experiments is not null && experiments.Count > 0, "DOC-SCHEMA");
        var runById = new Dictionary<string, AnalysisRun>(StringComparer.Ordinal);
        var prunedIds = new HashSet<string>(StringComparer.Ordinal);
        if (analysis is not null)
        {
            foreach (var run in analysis.Runs) runById[run.RunId] = run;
            foreach (var tombstone in analysis.Pruned) prunedIds.Add(tombstone.RunId);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var seriesVersions = new HashSet<(string SeriesId, int Version)>();
        var expById = new Dictionary<string, SweepExperiment>(StringComparer.Ordinal);

        foreach (var exp in experiments!)
        {
            Guard.Require(Present(exp) && Present(exp.Id, exp.SeriesId, exp.Kind, exp.Name, exp.DefinitionHash,
                exp.Definition, exp.Cases, exp.Events, exp.Attempts), "DOC-SCHEMA");
            NativeProject.Uuid(exp.Id);
            NativeProject.Uuid(exp.SeriesId);
            if (exp.ParentExperimentId is not null) NativeProject.Uuid(exp.ParentExperimentId);
            Guard.Require(ids.Add(exp.Id), "DOC-SWEEP-ID");
            Guard.Require(exp.Version >= 1 && seriesVersions.Add((exp.SeriesId, exp.Version)), "DOC-SWEEP-VERSION");
            Guard.Require(exp.Kind == KindSweep, "DOC-SCHEMA");
            Guard.Require(exp.Name.Length > 0 && exp.Name.Length <= MaxNameLength, "DOC-SCHEMA");
            RequireHash(exp.DefinitionHash);

            CheckDefinition(exp.Definition, acceptedExists, surfaceHashOf);
            CheckCases(exp.Definition, exp.Cases);
            CheckEventsAndPin(exp);
            CheckAttempts(exp, runById, prunedIds);

            expById[exp.Id] = exp;
        }

        foreach (var exp in experiments)
        {
            if (exp.ParentExperimentId is not null)
            {
                Guard.Require(expById.TryGetValue(exp.ParentExperimentId, out var parent) &&
                              parent!.SeriesId == exp.SeriesId && parent.Version < exp.Version, "DOC-REFERENCE");
            }
        }
    }

    private static void CheckDefinition(SweepDefinition def, Func<string, bool> acceptedExists, Func<string, string?> surfaceHashOf)
    {
        Guard.Require(Present(def.SourceKind, def.Tier, def.DesignRef, def.Water, def.Datum,
            def.SpeedsMetersPerSecond, def.AnglesDeg, def.Method, def.SettingsHash), "DOC-SCHEMA");
        Guard.Require(def.SourceKind is SourceCartesian or SourceGoalState, "DOC-SCHEMA");
        Guard.Require(def.Tier is TierVlmStrip or TierEstimator, "DOC-SCHEMA");
        Guard.Require(Present(def.DesignRef.AcceptedId, def.DesignRef.SurfaceHash), "DOC-SCHEMA");
        NativeProject.Uuid(def.DesignRef.AcceptedId);
        RequireHash(def.DesignRef.SurfaceHash);
        Guard.Require(acceptedExists(def.DesignRef.AcceptedId), "DOC-REFERENCE");
        Guard.Require(surfaceHashOf(def.DesignRef.AcceptedId) == def.DesignRef.SurfaceHash, "DOC-INTEGRITY");

        if (def.SourceKind == SourceGoalState)
        {
            Guard.Require(def.GoalStateRef is not null && Present(def.GoalStateRef.GoalStateId, def.GoalStateRef.ContentHash), "DOC-SCHEMA");
            NativeProject.Uuid(def.GoalStateRef!.GoalStateId);
            RequireHash(def.GoalStateRef.ContentHash);
        }
        else if (def.GoalStateRef is not null)
        {
            throw new ContractError("DOC-SCHEMA");
        }

        RequireHash(def.Water.TableHash);
        RequireHash(def.SettingsHash);
        Guard.Require(Present(def.Water.Source, def.Method.Id, def.Method.Version), "DOC-SCHEMA");
        RequireFinite(def.Water.TemperatureC, def.Water.SalinityGPerKg, def.Water.Rho, def.Water.Nu, def.Water.Pv, def.PAtm);
        RequireFinite(def.HRefMeters);
        Guard.Require(def.PAtm > 0 && (def.HRefMeters is null || def.HRefMeters.Value > 0), "DOC-SCHEMA");
        Guard.Require(def.SpeedsMetersPerSecond.Count <= MaxSpeeds && def.AnglesDeg.Count <= MaxAngles, "DOC-SCHEMA");
        foreach (double v in def.SpeedsMetersPerSecond) Guard.Require(double.IsFinite(v) && v > 0, "DOC-SCHEMA");
        foreach (double a in def.AnglesDeg) Guard.Require(double.IsFinite(a), "DOC-SCHEMA");
    }

    private static void CheckCases(SweepDefinition def, IReadOnlyList<SweepCase> cases)
    {
        Guard.Require(cases.Count > 0 && cases.Count <= MaxCasesPerSweep, "DOC-SCHEMA");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            Guard.Require(Present(c) && Present(c.CaseKey, c.Datum), "DOC-SCHEMA");
            Guard.Require(c.CaseIndex == i, "DOC-SCHEMA");
            RequireHash(c.CaseKey);
            Guard.Require(keys.Add(c.CaseKey), "DOC-SCHEMA");
            RequireFinite(c.SpeedMetersPerSecond, c.PAtm);
            RequireFinite(c.HRefMeters, c.AlphaDeg, c.TargetLoadN);
            Guard.Require(c.SpeedMetersPerSecond > 0 && c.PAtm > 0, "DOC-SCHEMA");
            Guard.Require(c.HRefMeters is null || c.HRefMeters.Value > 0, "DOC-SCHEMA");
            Guard.Require(c.TargetLoadN is null || c.TargetLoadN.Value > 0, "DOC-SCHEMA");
            if (def.SourceKind == SourceCartesian)
                Guard.Require(c.AlphaDeg is not null && c.TargetLoadN is null, "DOC-SCHEMA");
            else
                Guard.Require(c.AlphaDeg is null && c.TargetLoadN is not null, "DOC-SCHEMA");
        }
    }

    private static void CheckEventsAndPin(SweepExperiment exp)
    {
        bool seenQueued = false;
        bool seenArchived = false;
        for (int i = 0; i < exp.Events.Count; i++)
        {
            var ev = exp.Events[i];
            Guard.Require(Present(ev) && ev.Sequence == i, "DOC-SCHEMA");
            switch (ev.Kind)
            {
                case SweepEventKind.Queued:
                    Guard.Require(!seenQueued && i == 0, "DOC-SCHEMA");
                    seenQueued = true;
                    break;
                case SweepEventKind.CancelRequested:
                    Guard.Require(seenQueued, "DOC-SCHEMA");
                    break;
                case SweepEventKind.Archived:
                    Guard.Require(seenQueued && !seenArchived, "DOC-SCHEMA");
                    seenArchived = true;
                    break;
                default:
                    throw new ContractError("DOC-SCHEMA");
            }
        }

        if (seenQueued)
        {
            Guard.Require(exp.BackendPin is not null && Present(exp.BackendPin.Id, exp.BackendPin.Substrate,
                exp.BackendPin.Tier, exp.BackendPin.MethodId, exp.BackendPin.MethodVersion, exp.BackendPin.SettingsHash), "DOC-SCHEMA");
            var pin = exp.BackendPin!;
            Guard.Require(pin.Tier == exp.Definition.Tier &&
                          pin.MethodId == exp.Definition.Method.Id &&
                          pin.MethodVersion == exp.Definition.Method.Version &&
                          pin.SettingsHash == exp.Definition.SettingsHash, "DOC-SCHEMA");
        }
        else
        {
            // XS-03: no attempts may exist before Queued.
            Guard.Require(exp.Attempts.Count == 0, "DOC-SCHEMA");
        }
    }

    private static void CheckAttempts(SweepExperiment exp, Dictionary<string, AnalysisRun> runById, HashSet<string> prunedIds)
    {
        bool expIntact = Verify(exp);
        var attemptIds = new HashSet<string>(StringComparer.Ordinal);
        var byCase = new Dictionary<int, List<SweepAttempt>>();
        foreach (var att in exp.Attempts)
        {
            Guard.Require(Present(att) && Present(att.AttemptId, att.Events) && att.Events.Count > 0, "DOC-SCHEMA");
            NativeProject.Uuid(att.AttemptId);
            Guard.Require(attemptIds.Add(att.AttemptId), "DOC-ID");
            if (att.PriorAttemptId is not null) NativeProject.Uuid(att.PriorAttemptId);
            Guard.Require(att.CaseIndex >= 0 && att.CaseIndex < exp.Cases.Count, "DOC-SCHEMA");
            Guard.Require(att.AttemptIndex >= 1, "DOC-SCHEMA");
            RequireFinite(att.WallMs);
            Guard.Require(att.WallMs is null || att.WallMs.Value >= 0, "DOC-SCHEMA");
            if (att.SolvedAlpha is { } sa)
            {
                Guard.Require(Present(sa.Basis), "DOC-SCHEMA");
                RequireFinite(sa.AlphaDeg, sa.LowerDeg, sa.UpperDeg);
                Guard.Require(sa.Iterations >= 0 && sa.Solves >= 0, "DOC-SCHEMA");
            }
            for (int i = 0; i < att.Events.Count; i++)
            {
                var ev = att.Events[i];
                Guard.Require(Present(ev) && ev.Sequence == i, "DOC-SCHEMA");
            }

            if (!byCase.TryGetValue(att.CaseIndex, out var list)) byCase[att.CaseIndex] = list = [];
            list.Add(att);

            var terminal = att.State;
            if (terminal == SweepAttemptState.Completed)
            {
                Guard.Require(att.RunId is not null, "DOC-SCHEMA");
                NativeProject.Uuid(att.RunId!);
                if (runById.TryGetValue(att.RunId!, out var run))
                {
                    Guard.Require(run.Outcome is RunOutcome.Completed, "DOC-REFERENCE");
                    if (expIntact && RunRecord.Verify(run))
                    {
                        var c = exp.Cases[att.CaseIndex];
                        double? expectedAlpha = c.AlphaDeg ?? att.SolvedAlpha?.AlphaDeg;
                        Guard.Require(expectedAlpha is not null, "DOC-SCHEMA");
                        Guard.Require(run.Inputs.SurfaceHash == exp.Definition.DesignRef.SurfaceHash &&
                                      run.Method == exp.Definition.Method &&
                                      run.SettingsHash == exp.Definition.SettingsHash &&
                                      run.Water == exp.Definition.Water &&
                                      run.Op.Speed == c.SpeedMetersPerSecond &&
                                      run.Op.HRef == c.HRefMeters &&
                                      run.Op.Datum == c.Datum &&
                                      run.Op.PAtm == c.PAtm &&
                                      run.Op.Load == c.TargetLoadN &&
                                      run.Op.AlphaDeg == expectedAlpha!.Value, "DOC-REFERENCE");
                    }
                }
                else
                {
                    // Pruned tombstone allowed if and only if experiment is Archived OR this attempt is superseded (not selected).
                    bool isSelected = exp.SelectedAttempt(att.CaseIndex)?.AttemptId == att.AttemptId;
                    Guard.Require(prunedIds.Contains(att.RunId!) && (exp.IsArchived || !isSelected), "DOC-REFERENCE");
                }
            }
            else
            {
                // Spec A3.1:255 — a failed/cancelled/pending/running attempt references no AnalysisRun.
                Guard.Require(att.RunId is null, "DOC-SCHEMA");
            }
        }

        foreach (var (_, list) in byCase)
        {
            list.Sort((a, b) => a.AttemptIndex.CompareTo(b.AttemptIndex));
            for (int i = 0; i < list.Count; i++)
            {
                Guard.Require(list[i].AttemptIndex == i + 1, "DOC-SCHEMA");
                if (i == 0) Guard.Require(list[i].PriorAttemptId is null, "DOC-SCHEMA");
                else Guard.Require(list[i].PriorAttemptId == list[i - 1].AttemptId, "DOC-SCHEMA");
            }
        }
    }

    private static bool Present(params object?[] values) => values.All(v => v is not null);
    private static void RequireHash(string? hash) => Guard.Require(hash is not null && Hex64.IsMatch(hash), "DOC-INTEGRITY");
    private static void RequireFinite(params double[] values) => Guard.Require(values.All(double.IsFinite), "DOC-SCHEMA");
    private static void RequireFinite(params double?[] values) => Guard.Require(values.All(v => v is null || double.IsFinite(v.Value)), "DOC-SCHEMA");
    private static string Blake3Jcs(object? canonical) => Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(canonical)));

    private static object? Canonical(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(m => m.Name, m => Canonical(m.Value), StringComparer.Ordinal) as IDictionary<string, object?>,
        JsonValueKind.Array => element.EnumerateArray().Select(Canonical).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}

internal sealed class SweepEventKindConverter : JsonConverter<SweepEventKind>
{
    public override SweepEventKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("sweep event kind must be a string");
        return reader.GetString() switch
        {
            "queued" => SweepEventKind.Queued,
            "cancel-requested" => SweepEventKind.CancelRequested,
            "archived" => SweepEventKind.Archived,
            _ => throw new JsonException("unknown sweep event kind")
        };
    }

    public override void Write(Utf8JsonWriter writer, SweepEventKind value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            SweepEventKind.Queued => "queued",
            SweepEventKind.CancelRequested => "cancel-requested",
            SweepEventKind.Archived => "archived",
            _ => throw new JsonException("unknown sweep event kind")
        });
    }
}

internal sealed class SweepAttemptStateConverter : JsonConverter<SweepAttemptState>
{
    public override SweepAttemptState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("sweep attempt state must be a string");
        return reader.GetString() switch
        {
            "pending" => SweepAttemptState.Pending,
            "running" => SweepAttemptState.Running,
            "completed" => SweepAttemptState.Completed,
            "failed" => SweepAttemptState.Failed,
            "cancelled" => SweepAttemptState.Cancelled,
            _ => throw new JsonException("unknown sweep attempt state")
        };
    }

    public override void Write(Utf8JsonWriter writer, SweepAttemptState value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            SweepAttemptState.Pending => "pending",
            SweepAttemptState.Running => "running",
            SweepAttemptState.Completed => "completed",
            SweepAttemptState.Failed => "failed",
            SweepAttemptState.Cancelled => "cancelled",
            _ => throw new JsonException("unknown sweep attempt state")
        });
    }
}
