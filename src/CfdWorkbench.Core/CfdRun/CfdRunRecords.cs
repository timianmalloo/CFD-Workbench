using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.CfdRun;

/// <summary>
/// Additive <c>cfdw-project-2</c> top-level <c>cfd</c> envelope (docs/design/cfd-run-pipeline.md §2).
/// Complete container (<c>runs</c>, <c>fieldEvidence</c>, <c>candidates</c>) landed in W10-RUN so W10-RESULTS
/// builds directly on it without reshaping <see cref="Envelope"/>.
/// Strictly pure zero-I/O (recursive STORE-SUBSET verified).
/// </summary>
public sealed record CfdRecords(
    IReadOnlyList<CfdRunRecord> Runs,
    IReadOnlyList<FieldEvidenceRecord> FieldEvidence,
    IReadOnlyList<CandidateRecord> Candidates);

public sealed record CfdBackendPin(
    string Backend,
    string Version,
    string Substrate,
    string BuildId,
    string PinDigest,
    bool VerifiedInstall);

public sealed record RegionLayerCoverage(
    string Region,
    int MinLayers,
    double FractionAtLeast20,
    double FractionAtLeast10);

public sealed record MeshGateFailure(
    string Measure,
    double Observed,
    double Threshold,
    string Message);

public sealed record MeshGateResult(
    bool Passed,
    string Branch,
    double MaxNonOrthogonalityDeg,
    double MaxInternalSkewness,
    double MaxBoundarySkewness,
    double MinCellVolume,
    double MinCellDeterminantLayer,
    double MinCellDeterminantCore,
    double LayerCoverage20PlusFraction,
    double LayerCoverage10PlusFraction,
    IReadOnlyList<RegionLayerCoverage> RegionCoverage,
    double? YPlusP95,
    double? YPlusMax,
    bool IsScreenOnly,
    IReadOnlyList<MeshGateFailure> Failures);

public sealed record ResidualSummary(
    double MaxInitialResidualRatio,
    double ClWindowMean,
    double CdWindowMean,
    double CmWindowMean,
    double ClHalfBandUi,
    double CdHalfBandUi,
    double ClHalfBandCap,
    double CdHalfBandCap,
    int AveragingWindowIterations,
    int TurbulenceBoundingCountInWindow,
    bool RanToIterationLimit,
    bool CriterionMet);

public sealed record CfdRunResources(
    long Cells,
    int Ranks,
    double WallMs,
    double CpuSeconds,
    long PeakRssBytes);

public sealed record CfdCheckpointRecord(
    string TimeDirectory,
    string ManifestHash,
    bool HarvesterValidated);

public sealed record CfdRunStateEvent(
    int Sequence,
    CfdRunState State,
    DateTimeOffset AtUtc,
    string? Code = null,
    string? Reason = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? ElapsedMs = null);

public sealed record CfdRunRecord(
    string RunId,
    string? PriorRunId,
    string? ExperimentId,
    int CaseIndex,
    string RunKey,
    string ContentHash,
    string CaseModelHash,
    string? MeshHash,
    CfdBackendPin BackendPin,
    RunInputs Inputs,
    WaterRecord Water,
    OperatingPoint Op,
    RunReference Reference,
    MeshGateResult? MeshGate,
    ResidualSummary? ResidualSummary,
    CfdRunResources Resources,
    CfdCheckpointRecord? Checkpoint,
    IReadOnlyList<CfdRunStateEvent> Events)
{
    /// <summary>Derived current state (DM7): always the last event in the append-only transition log.</summary>
    [JsonIgnore]
    public CfdRunState CurrentState => Events[^1].State;
}

public sealed record FieldRangeRecord(
    string Variable,
    string Unit,
    double Min,
    double Max,
    string Basis);

public sealed record FieldEvidenceRecord(
    string EvidenceId,
    string RunId,
    string ContentHash,
    string Kind,
    string RelativePath,
    string ArtifactHash,
    string MeshHash,
    IReadOnlyList<string> Fields,
    string Association,
    IReadOnlyList<FieldRangeRecord> Ranges,
    string ColormapPolicyVersion,
    string ReductionTool,
    string ReductionToolVersion,
    bool HasWallShearStress,
    string? SeparationCriterion,
    bool HasTurbulentKineticEnergy,
    bool IsTransient,
    IReadOnlyList<double> Timesteps);

public sealed record CandidateObjectiveRecord(string Name, string Unit, double Value);

public sealed record CandidateConstraintRecord(string Name, string Unit, double Value, double Bound, bool Satisfied);

public sealed record CandidateParameterDelta(string Channel, string VertexId, double DeltaValue, string Unit);

public sealed record CandidateRecord(
    string CandidateId,
    string? ParentCandidateId,
    string ContentHash,
    string ExperimentId,
    string EvaluationId,
    string BaseAcceptedId,
    string BaseSurfaceHash,
    string? CandidateAcceptedId,
    string Status,
    bool Feasible,
    string Tier,
    IReadOnlyList<CandidateObjectiveRecord> Objectives,
    IReadOnlyList<CandidateConstraintRecord> Constraints,
    IReadOnlyList<string> PromotionRunIds,
    IReadOnlyList<string> DiscrepancyKeys,
    IReadOnlyList<CandidateParameterDelta> ParameterDeltas);

public enum CfdRecordIntegrity { Intact, PayloadFailedCheck }

public sealed record StoredCfdRun(CfdRunRecord Run, CfdRecordIntegrity Integrity);

public sealed record StoredFieldEvidence(FieldEvidenceRecord Evidence, CfdRecordIntegrity Integrity);

public sealed record StoredCandidate(CandidateRecord Candidate, CfdRecordIntegrity Integrity);

public sealed record CfdLedger(
    IReadOnlyList<StoredCfdRun> Runs,
    IReadOnlyList<StoredFieldEvidence> FieldEvidence,
    IReadOnlyList<StoredCandidate> Candidates);

/// <summary>Normal-path Area 5 telemetry fields (docs/design/cfd-run-pipeline.md §8, IO1-IO12). Zero PII.</summary>
public sealed record CfdTelemetry
{
    public string? RunKey12 { get; init; }
    public string? Backend { get; init; }
    public long? Cells { get; init; }
    public int? Ranks { get; init; }
    public double? DurationMs { get; init; }
    public double? CpuSeconds { get; init; }
    public long? PeakRssBytes { get; init; }
    public string? GateOutcome { get; init; }
    public double? MaxNonOrthoDeg { get; init; }
    public double? LayerCoverage20Pct { get; init; }
    public double? YPlusP95 { get; init; }
    public string? OracleOutcome { get; init; }
    public bool? CriterionMet { get; init; }
    public int? FilesParsed { get; init; }
    public int? EvidenceRows { get; init; }
    public int? OrphansFound { get; init; }
    public bool? CheckpointValidated { get; init; }
    public string? TimeDirectory { get; init; }
    public string? Outcome { get; init; }
}

/// <summary>Canonical BLAKE3(JCS) hashing, read-time verification, and store invariants for <see cref="CfdRecords"/>.</summary>
public static class CfdRunRecords
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ValidEvidenceKinds = new(StringComparer.Ordinal)
        { "surface", "slice", "rake", "isosurface", "history" };
    private static readonly HashSet<string> ValidAssociations = new(StringComparer.Ordinal)
        { "face", "point", "cell", "strip" };
    private static readonly HashSet<string> ValidCandidateStatuses = new(StringComparer.Ordinal)
        { "surrogate-candidate", "vlm-checked", "cfd-checked", "experimentally-compared" };
    private static readonly HashSet<string> ValidBackends = new(StringComparer.Ordinal)
        { "openfoam-esi", "su2" };

    /// <summary>Deterministic 64-hex BLAKE3(JCS) run key from inputs, water, operating point, backend pin, and case model hash.</summary>
    public static string ComputeRunKey(RunInputs inputs, WaterRecord water, OperatingPoint op, CfdBackendPin backendPin, string caseModelHash)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["surface"] = new Dictionary<string, object?>
            {
                ["hash"] = inputs.SurfaceHash,
                ["evaluator"] = inputs.Evaluator,
                ["placementRule"] = inputs.PlacementRule
            },
            ["profiles"] = inputs.ProfileHashes.ToList(),
            ["water"] = new Dictionary<string, object?>
            {
                ["temperatureC"] = water.TemperatureC,
                ["salinityGPerKg"] = water.SalinityGPerKg,
                ["rho"] = water.Rho,
                ["nu"] = water.Nu,
                ["pv"] = water.Pv,
                ["source"] = water.Source,
                ["tableHash"] = water.TableHash
            },
            ["op"] = new Dictionary<string, object?>
            {
                ["speed"] = op.Speed,
                ["pAtm"] = op.PAtm,
                ["hRef"] = op.HRef,
                ["datum"] = op.Datum,
                ["alphaDeg"] = op.AlphaDeg,
                ["load"] = op.Load
            },
            ["backendPin"] = new Dictionary<string, object?>
            {
                ["backend"] = backendPin.Backend,
                ["version"] = backendPin.Version,
                ["substrate"] = backendPin.Substrate,
                ["buildId"] = backendPin.BuildId,
                ["pinDigest"] = backendPin.PinDigest,
                ["verifiedInstall"] = backendPin.VerifiedInstall
            },
            ["caseModelHash"] = caseModelHash
        };
        return Blake3Jcs(manifest);
    }

    public static string ContentHash(CfdRunRecord run) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(run, JsonOptions), drop: "contentHash"));

    public static string ContentHash(FieldEvidenceRecord evidence) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(evidence, JsonOptions), drop: "contentHash"));

    public static string ContentHash(CandidateRecord candidate) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(candidate, JsonOptions), drop: "contentHash"));

    public static CfdRunRecord Seal(CfdRunRecord run) => run with { ContentHash = ContentHash(run) };

    public static FieldEvidenceRecord Seal(FieldEvidenceRecord evidence) => evidence with { ContentHash = ContentHash(evidence) };

    public static CandidateRecord Seal(CandidateRecord candidate) => candidate with { ContentHash = ContentHash(candidate) };

    public static bool VerifyRun(CfdRunRecord run)
    {
        try
        {
            string expectedKey = ComputeRunKey(run.Inputs, run.Water, run.Op, run.BackendPin, run.CaseModelHash);
            return string.Equals(run.RunKey, expectedKey, StringComparison.Ordinal) &&
                   string.Equals(run.ContentHash, ContentHash(run), StringComparison.Ordinal);
        }
        catch (ContractError) { return false; }
    }

    public static bool VerifyEvidence(FieldEvidenceRecord evidence)
    {
        try { return string.Equals(evidence.ContentHash, ContentHash(evidence), StringComparison.Ordinal); }
        catch (ContractError) { return false; }
    }

    public static bool VerifyCandidate(CandidateRecord candidate)
    {
        try { return string.Equals(candidate.ContentHash, ContentHash(candidate), StringComparison.Ordinal); }
        catch (ContractError) { return false; }
    }

    /// <summary>
    /// Structural store invariants shared by <c>NativeProject.Check</c> and <see cref="AuthoringSession"/> writers.
    /// Does NOT throw on per-row <c>ContentHash</c> tamper so <see cref="AuthoringSession.ReadCfdLedger"/> surfaces
    /// tampered rows honestly as <see cref="CfdRecordIntegrity.PayloadFailedCheck"/> without deleting them.
    /// </summary>
    public static void CheckStore(CfdRecords records, Func<string, bool> acceptedExists)
    {
        Guard.Require(Present(records, records.Runs, records.FieldEvidence, records.Candidates), "DOC-SCHEMA");

        var runIds = new HashSet<string>(StringComparer.Ordinal);
        var completedRunIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var run in records.Runs)
        {
            CheckRunRow(run, acceptedExists);
            Guard.Require(runIds.Add(run.RunId), "DOC-CFD-ID");
            if (run.CurrentState == CfdRunState.Completed)
                completedRunIds.Add(run.RunId);
        }

        var evidenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ev in records.FieldEvidence)
        {
            CheckEvidenceRow(ev, runIds);
            Guard.Require(evidenceIds.Add(ev.EvidenceId), "DOC-CFD-ID");
        }

        var candidateIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cand in records.Candidates)
        {
            CheckCandidateRow(cand, acceptedExists, completedRunIds);
            Guard.Require(candidateIds.Add(cand.CandidateId), "DOC-CFD-ID");
        }
    }

    private static void CheckRunRow(CfdRunRecord run, Func<string, bool> acceptedExists)
    {
        Guard.Require(Present(run, run.BackendPin, run.Inputs, run.Water, run.Op, run.Reference, run.Resources, run.Events), "DOC-SCHEMA");
        NativeProject.Uuid(run.RunId);
        if (run.PriorRunId is not null) NativeProject.Uuid(run.PriorRunId);
        if (run.ExperimentId is not null) NativeProject.Uuid(run.ExperimentId);
        Guard.Require(run.CaseIndex >= 0, "DOC-SCHEMA");
        RequireHash(run.RunKey);
        RequireHash(run.ContentHash);
        RequireHash(run.CaseModelHash);
        if (run.MeshHash is not null) RequireHash(run.MeshHash);

        Guard.Require(Present(run.BackendPin.Backend, run.BackendPin.Version, run.BackendPin.Substrate, run.BackendPin.BuildId), "DOC-SCHEMA");
        Guard.Require(ValidBackends.Contains(run.BackendPin.Backend), "DOC-SCHEMA");
        RequireHash(run.BackendPin.PinDigest);

        NativeProject.Uuid(run.Inputs.AcceptedId);
        Guard.Require(acceptedExists(run.Inputs.AcceptedId), "DOC-REFERENCE");
        RequireHash(run.Inputs.SurfaceHash);
        Guard.Require(Present(run.Inputs.ProfileHashes), "DOC-SCHEMA");
        foreach (string h in run.Inputs.ProfileHashes) RequireHash(h);

        RequireHash(run.Water.TableHash);
        RequireFinite(run.Water.TemperatureC, run.Water.SalinityGPerKg, run.Water.Rho, run.Water.Nu, run.Water.Pv,
            run.Op.Speed, run.Op.PAtm, run.Op.AlphaDeg, run.Reference.SRef, run.Reference.BRef, run.Reference.CRef,
            run.Resources.WallMs, run.Resources.CpuSeconds);
        RequireFinite(run.Op.HRef, run.Op.Load);
        Guard.Require(run.Resources.Cells >= 0 && run.Resources.Ranks >= 0 && run.Resources.PeakRssBytes >= 0, "DOC-SCHEMA");

        if (run.MeshGate is { } mg)
        {
            Guard.Require(Present(mg.Branch, mg.RegionCoverage, mg.Failures), "DOC-SCHEMA");
            RequireFinite(mg.MaxNonOrthogonalityDeg, mg.MaxInternalSkewness, mg.MaxBoundarySkewness,
                mg.MinCellVolume, mg.MinCellDeterminantLayer, mg.MinCellDeterminantCore,
                mg.LayerCoverage20PlusFraction, mg.LayerCoverage10PlusFraction);
            RequireFinite(mg.YPlusP95, mg.YPlusMax);
            foreach (var r in mg.RegionCoverage)
                RequireFinite(r.FractionAtLeast20, r.FractionAtLeast10);
            foreach (var f in mg.Failures)
                RequireFinite(f.Observed, f.Threshold);
        }

        if (run.ResidualSummary is { } rs)
        {
            RequireFinite(rs.MaxInitialResidualRatio, rs.ClWindowMean, rs.CdWindowMean, rs.CmWindowMean,
                rs.ClHalfBandUi, rs.CdHalfBandUi, rs.ClHalfBandCap, rs.CdHalfBandCap);
            Guard.Require(rs.AveragingWindowIterations >= 0 && rs.TurbulenceBoundingCountInWindow >= 0, "DOC-SCHEMA");
        }

        if (run.Checkpoint is { } cp)
        {
            Guard.Require(!string.IsNullOrWhiteSpace(cp.TimeDirectory), "DOC-SCHEMA");
            RequireHash(cp.ManifestHash);
        }

        Guard.Require(run.Events.Count > 0 && run.Events[0].State == CfdRunState.Pending, "DOC-SCHEMA");
        for (int i = 0; i < run.Events.Count; i++)
        {
            var ev = run.Events[i];
            Guard.Require(ev.Sequence == i, "DOC-SCHEMA");
            RequireFinite(ev.ElapsedMs);
            if (i > 0)
            {
                var tr = CfdRunStateMachine.EvaluateTransition(run.Events[i - 1].State, ev.State, ev.Code, ev.Reason);
                Guard.Require(tr.Legal, "DOC-SCHEMA");
            }
        }
    }

    private static void CheckEvidenceRow(FieldEvidenceRecord ev, HashSet<string> runIds)
    {
        Guard.Require(Present(ev, ev.Kind, ev.RelativePath, ev.Fields, ev.Association, ev.Ranges,
            ev.ColormapPolicyVersion, ev.ReductionTool, ev.ReductionToolVersion, ev.Timesteps), "DOC-SCHEMA");
        NativeProject.Uuid(ev.EvidenceId);
        NativeProject.Uuid(ev.RunId);
        Guard.Require(runIds.Contains(ev.RunId), "DOC-REFERENCE");
        RequireHash(ev.ContentHash);
        RequireHash(ev.ArtifactHash);
        RequireHash(ev.MeshHash);
        Guard.Require(ValidEvidenceKinds.Contains(ev.Kind), "DOC-SCHEMA");
        Guard.Require(ValidAssociations.Contains(ev.Association), "DOC-SCHEMA");
        // VIZ separation persistence invariant: without wall shear stress, separationCriterion MUST be null!
        Guard.Require(ev.HasWallShearStress || ev.SeparationCriterion is null, "DOC-SCHEMA");
        foreach (var range in ev.Ranges)
        {
            Guard.Require(Present(range, range.Variable, range.Unit, range.Basis), "DOC-SCHEMA");
            RequireFinite(range.Min, range.Max);
        }
        foreach (double t in ev.Timesteps) RequireFinite(t);
    }

    private static void CheckCandidateRow(CandidateRecord cand, Func<string, bool> acceptedExists, HashSet<string> completedRunIds)
    {
        Guard.Require(Present(cand, cand.EvaluationId, cand.Status, cand.Tier, cand.Objectives,
            cand.Constraints, cand.PromotionRunIds, cand.DiscrepancyKeys, cand.ParameterDeltas), "DOC-SCHEMA");
        NativeProject.Uuid(cand.CandidateId);
        if (cand.ParentCandidateId is not null) NativeProject.Uuid(cand.ParentCandidateId);
        NativeProject.Uuid(cand.ExperimentId);
        NativeProject.Uuid(cand.BaseAcceptedId);
        Guard.Require(acceptedExists(cand.BaseAcceptedId), "DOC-REFERENCE");
        if (cand.CandidateAcceptedId is not null) NativeProject.Uuid(cand.CandidateAcceptedId);
        RequireHash(cand.ContentHash);
        RequireHash(cand.BaseSurfaceHash);
        Guard.Require(ValidCandidateStatuses.Contains(cand.Status), "DOC-SCHEMA");

        foreach (string runId in cand.PromotionRunIds) NativeProject.Uuid(runId);
        foreach (string dk in cand.DiscrepancyKeys) RequireHash(dk);
        foreach (var obj in cand.Objectives) RequireFinite(obj.Value);
        foreach (var c in cand.Constraints) RequireFinite(c.Value, c.Bound);
        foreach (var d in cand.ParameterDeltas) RequireFinite(d.DeltaValue);

        // CAND-01: cfd-checked requires non-empty PromotionRunIds referencing existing Completed CFD runs.
        if (cand.Status == "cfd-checked")
        {
            Guard.Require(cand.PromotionRunIds.Count > 0 && cand.PromotionRunIds.All(completedRunIds.Contains), "CAND-01");
        }
    }

    internal static string Blake3Jcs(object? canonical) => Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(canonical)));

    private static bool Present(params object?[] values) => values.All(v => v is not null);
    private static void RequireHash(string? hash) => Guard.Require(hash is not null && Hex64.IsMatch(hash), "DOC-INTEGRITY");
    private static void RequireFinite(params double[] values) => Guard.Require(values.All(double.IsFinite), "DOC-SCHEMA");
    private static void RequireFinite(params double?[] values) => Guard.Require(values.All(v => v is null || double.IsFinite(v.Value)), "DOC-SCHEMA");

    private static object? Canonical(JsonElement element, string? drop) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Where(m => m.Name != drop)
            .ToDictionary(m => m.Name, m => Canonical(m.Value, null), StringComparer.Ordinal) as IDictionary<string, object?>,
        JsonValueKind.Array => element.EnumerateArray().Select(item => Canonical(item, null)).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
