using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

// The stored Analysis run row (ADR-0011; design area3-analysis.md §3.3, §3.6, §5.6). Core holds it because NativeProject
// is in Core and Core cannot reference CfdWorkbench.Analysis (G-T8, P-3): one definition of the row, its canonical form,
// its run key and its content hash. PRE lands the shape; STO owns the bodies (seam S-A3). VLM, STP, SVC and PRJ read
// these records and never add members: a missing field is a seam request to STO. Exception, granted to STP-3:
// StripLoad.Provisional and ProvisionalReason (Ruling 77(5)), omitted when unset so older documents keep their hash.

/// <summary>The optional top-level <c>analysis</c> member of a <c>cfdw-project-2</c> document. Append-only facts.</summary>
public sealed record AnalysisRecords(IReadOnlyList<AnalysisRun> Runs, IReadOnlyList<PolarSample> PolarSamples,
    IReadOnlyList<PrunedRun> Pruned);

/// <summary>
/// One evaluation attempt of one Surface revision (with its Profile revisions) at one operating point by one method
/// id + version under one settings hash, recorded when the method returns (AM-1.7-19). Immutable once recorded.
/// <paramref name="Tier"/> is the tier id (<c>vlm-strip</c>). <paramref name="RunKey"/> and <paramref name="ContentHash"/>
/// are stored but never trusted on read: both are recomputed (ADR-0011 §4). <paramref name="Diagnostics"/> is the
/// solver's measurement: required on a Completed row and absent on a Failed one, never a zero standing in for a solve
/// that did not finish (IO8). It is last so the reader can take its absence (an optional constructor parameter).
/// </summary>
public sealed record AnalysisRun(string RunId, string RunKey, string ContentHash, RunOutcome Outcome, string Tier,
    RunMethod Method, RunSettings Settings, string SettingsHash, RunInputs Inputs, WaterRecord Water, OperatingPoint Op,
    RunReference Reference, double ReconciliationTolerance, IReadOnlyList<StripLoad> Strips, double WallMs, RunPlatform Platform,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RunDiagnostics? Diagnostics = null);

/// <summary>Completed, or Failed with a stable code and a reason. Cancelled is telemetry, never a row.</summary>
[JsonConverter(typeof(RunOutcomeConverter))]
public abstract record RunOutcome
{
    private RunOutcome() { }
    public sealed record Completed : RunOutcome;
    public sealed record Failed(string Code, string Reason) : RunOutcome;
}

/// <summary>The method id, its version and its observed convergence order (VLM + strip: <c>cfdw.vlm-strip</c>, p = 1).</summary>
public sealed record RunMethod(string Id, string Version, int Order);

/// <summary>
/// Every numeric choice of a run; all of it is in the run key through <see cref="AnalysisRun.SettingsHash"/>.
/// <paramref name="SectionEtas"/> and <paramref name="SectionXs"/> are the η stations and chord abscissae the method
/// samples through <c>Placement.Sections</c>: they reach the compute, so they are settings (design §3.4). They are
/// optional and omitted when null, so a settings record without them keeps its hash (expand only); the service refuses
/// to evaluate without them (<c>ANA-INPUT-STATIONS</c>).
/// </summary>
public sealed record RunSettings(int NSpanPerHalf, int NChord, string SpanSpacing, string ChordSpacing, int WakeSpans,
    string WakeDirection, double SingularityCutoff, string Envelope, RunPolar? Polar, IReadOnlyList<int> Ncrit,
    string SurfaceState, double TeFloorMm,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<double>? SectionEtas = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<double>? SectionXs = null);

/// <summary>The polar method a run used, or none (A3a: none — DR-ANA-1 (a)).</summary>
public sealed record RunPolar(string Id, string Version, string Model);

/// <summary>What the run read: the accepted revision, never a draft (design §4 item 1).</summary>
public sealed record RunInputs(string AcceptedId, string SurfaceHash, IReadOnlyList<string> ProfileHashes, string Evaluator,
    string PlacementRule);

/// <summary>A water record from the bundled ITTC table, with the table's hash.</summary>
public sealed record WaterRecord(double TemperatureC, double SalinityGPerKg, double Rho, double Nu, double Pv,
    string Source, string TableHash);

/// <summary>
/// The operating point as stored. <paramref name="HRef"/> null is "depth not set" (DR-ANA-13).
/// <paramref name="Load"/> is reserved by the design manifest (§5.6) and always null in A3a (DR-ANA-9: Custom operating
/// points only). assume: it is the goal-state design load in newtons; confirmed when Area 1 defines it; if not, it is
/// renamed before any run stores a value (every A3a row stores null, so the rename changes no stored key).
/// </summary>
public sealed record OperatingPoint(double Speed, double PAtm, double? HRef, string Datum, double AlphaDeg, double? Load);

/// <summary>The reference quantities used, derived at run time and recorded so a later change is a visible difference.</summary>
public sealed record RunReference(double SRef, double BRef, double CRef, string MomentDatum, string Axes);

/// <summary>Solver diagnostics: ‖AΓ − b‖∞ and the 1-norm condition estimate κ₁ (non-additive).</summary>
public sealed record RunDiagnostics(double ResidualInf, double Kappa1);

public sealed record RunPlatform(string Os, string Arch, string Dotnet);

/// <summary>
/// Spanwise lattice row <paramref name="J"/> of one Completed run (contiguous 0…n−1). Forces and moments are near-field,
/// about the frame origin, in body axes, and additive across the strips of one run only.
/// <paramref name="Provisional"/> marks the outermost strip of each half until the η* law exists (Ruling 77(5)).
/// Optional span edges carry the original lattice geometry when excluded strips renumber J.
/// <paramref name="YLow"/> is the lower-y edge and <paramref name="YHigh"/> the higher-y edge
/// (<c>YLow</c> &lt; <c>YHigh</c>). On a port strip the lower-y edge is the outboard edge.
/// Absent edges preserve older content hashes; a reader may reconstruct widths only for a complete, unexcluded lattice.
/// </summary>
public sealed record StripLoad(int J, double Y, double Eta, double Chord, double Gamma, double AlphaI, double AlphaEff,
    double ReLocal, double ClLocal, StripValue CdNcrit2, StripValue CdNcrit4,
    double Fx, double Fy, double Fz, double Mx, double My, double Mz, double DownwashTrefftz,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Provisional = false,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProvisionalReason = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? YLow = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? YHigh = null)
{
    /// <summary>Reason on a provisional tip strip. Any other reason is a schema error; absent reads false.</summary>
    public const string TipProvisionalReason = "ANA-TIP-PROVISIONAL";
}

/// <summary>A value, or Unavailable with its reason — never a zero standing in for a missing number.</summary>
public sealed record StripValue(double? Value, string? UnavailableReason);

/// <summary>One polar evaluation at its grain key (empty in A3a: no polar method is installed).</summary>
public sealed record PolarSample(string ProfileHash, string MethodId, string MethodVersion, double Reynolds, double Ncrit,
    string SurfaceState, double AlphaDeg, string WaterHash, double? Cl, double? Cd, double? Cm, double? XtrUpper,
    double? XtrLower, double? CpMin, int? CpMinStations, double? Confidence, bool Converged);

/// <summary>The tombstone a pruned run leaves (ADR-0011 §7), so an Undo that reaches it reads "pruned", not "missing".</summary>
public sealed record PrunedRun(string RunId, string RunKey, DateTimeOffset PrunedAt);

/// <summary>How a stored run reads back (ADR-0011 §4). <see cref="PayloadFailedCheck"/> renders "Unavailable — run payload
/// failed its check": never Current, never deleted.</summary>
public enum RunIntegrity { Intact, PayloadFailedCheck }

/// <summary>One stored run row and its integrity, recomputed on every read (the stored hashes are never trusted).</summary>
public sealed record StoredRun(AnalysisRun Run, RunIntegrity Integrity);

/// <summary>The session's run facts in document order, with the tombstones retention left (ADR-0011 §7).</summary>
public sealed record RunLedger(IReadOnlyList<StoredRun> Runs, IReadOnlyList<PrunedRun> Pruned)
{
    /// <summary>True when the key was pruned and no intact run holds it now: the view reads "pruned", not "missing".</summary>
    public bool IsPruned(string runKey) =>
        Pruned.Any(tombstone => tombstone.RunKey == runKey) &&
        !Runs.Any(stored => stored.Integrity == RunIntegrity.Intact && stored.Run.RunKey == runKey);
}

/// <summary>The accepted revision's ordinal in accepted-row order (r1 is the open) and the rail of the edit that made it
/// (null for the open). Feeds "r4 → r5" and "a twist point moved" (design §18.5 G-T1).</summary>
public sealed record RevisionLabel(int Ordinal, string? Rail);

/// <summary>
/// The Analysis fields of a session event (design §11): <c>analysis.run</c>, <c>analysis.toggle</c>,
/// <c>analysis.project</c> and the save-time <c>analysis.prune</c> report. A field not reached stays null and reads
/// "not recorded" (IO8), never zero. No file names or user text.
/// </summary>
public sealed record AnalysisEvent
{
    public string? Tier { get; init; }
    public string? MethodId { get; init; }
    public string? MethodVersion { get; init; }
    /// <summary>The first 12 hex digits of the run key.</summary>
    public string? RunKey12 { get; init; }
    public string? Scope { get; init; }
    public int? Unknowns { get; init; }
    public int? Strips { get; init; }
    public double? Residual { get; init; }
    public double? Kappa1 { get; init; }
    public int? StripsOutsideEnvelope { get; init; }
    public bool? IdempotentHit { get; init; }
    public double? SnapshotMs { get; init; }
    public double? SectionsMs { get; init; }
    public double? AssembleMs { get; init; }
    public double? SolveMs { get; init; }
    public double? StripMs { get; init; }
    public double? RecordMs { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public int? LayersDrawn { get; init; }
    public string? Freshness { get; init; }
    public string? WhatChanged { get; init; }
    public int? Pruned { get; init; }
}

/// <summary>The one definition of the run key, the content hash and the format string (design §3.4, ADR-0011).</summary>
public static class RunRecord
{
    /// <summary>The settings-validation cap on strips per run (design §3.6; DR-ANA-7's 2,048 unknowns).</summary>
    public const int MaxStrips = 2048;

    // The serialized row is the canonical row: the content hash and the settings hash are taken over JCS of what the
    // writer stores, so a member added to the record is covered without a second list of fields.
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    /// <summary>BLAKE3 over JCS of {surface, profiles, water, op, method {id, version}, settings: settingsHash}.</summary>
    /// <remarks>The Surface revision is taken with its evaluator and placement-rule versions (design §3.4); the accepted
    /// id is not in the key, so an Undo back to equal inputs is Current again.</remarks>
    public static string Key(RunInputs inputs, WaterRecord water, OperatingPoint op, RunMethod method, string settingsHash)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["surface"] = new Dictionary<string, object?>
            {
                ["hash"] = inputs.SurfaceHash, ["evaluator"] = inputs.Evaluator, ["placementRule"] = inputs.PlacementRule
            },
            ["profiles"] = inputs.ProfileHashes.ToList(),
            ["water"] = new Dictionary<string, object?>
            {
                ["temperatureC"] = water.TemperatureC, ["salinityGPerKg"] = water.SalinityGPerKg, ["rho"] = water.Rho,
                ["nu"] = water.Nu, ["pv"] = water.Pv, ["source"] = water.Source, ["tableHash"] = water.TableHash
            },
            ["op"] = new Dictionary<string, object?>
            {
                ["speed"] = op.Speed, ["pAtm"] = op.PAtm, ["hRef"] = op.HRef, ["datum"] = op.Datum,
                ["alphaDeg"] = op.AlphaDeg, ["load"] = op.Load
            },
            ["method"] = new Dictionary<string, object?> { ["id"] = method.Id, ["version"] = method.Version },
            ["settings"] = settingsHash
        };
        return Blake3Jcs(manifest);
    }

    /// <summary>BLAKE3 over JCS of the settings.</summary>
    public static string SettingsHash(RunSettings settings) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(settings, Options), drop: null));

    /// <summary>BLAKE3 over JCS of the row without its <c>contentHash</c>.</summary>
    public static string ContentHash(AnalysisRun run) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(run, Options), drop: "contentHash"));

    /// <summary><c>cfdw-project-1</c> with no run, <c>cfdw-project-2</c> with one or more (ADR-0011 §1).</summary>
    public static string Format(int runCount)
    {
        Guard.Require(runCount >= 0, "DOC-SCHEMA");
        return runCount == 0 ? "cfdw-project-1" : "cfdw-project-2";
    }

    /// <summary>The run key recomputed from the stored manifest (never the stored <c>runKey</c>).</summary>
    public static string RecomputedKey(AnalysisRun run) =>
        Key(run.Inputs, run.Water, run.Op, run.Method, SettingsHash(run.Settings));

    /// <summary>
    /// The per-run integrity check on read (ADR-0011 §4): the content hash matches the row, and the stored key and
    /// settings hash equal the ones recomputed from the manifest. One function, so a tampered value and a forged key
    /// fail the same way.
    /// </summary>
    public static bool Verify(AnalysisRun run)
    {
        // A row the canonical form cannot express (a non-finite number) cannot be verified, so it fails its check.
        try
        {
            return run.ContentHash == ContentHash(run) && run.SettingsHash == SettingsHash(run.Settings) &&
                   run.RunKey == RecomputedKey(run);
        }
        catch (ContractError) { return false; }
    }

    /// <summary>
    /// The structural store invariants of ADR-0011 §3, shared by the reader (<c>NativeProject.Check</c>) and
    /// <c>AuthoringSession.RecordRun</c> so both refuse the same rows with the same codes. Per-run integrity is not
    /// structural: a run that fails <see cref="Verify"/> stays in the document, Unavailable.
    /// </summary>
    internal static void CheckStore(AnalysisRecords records, Func<string, bool> acceptedExists)
    {
        Guard.Require(Present(records.Runs, records.PolarSamples, records.Pruned), "DOC-SCHEMA");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var completedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var run in records.Runs)
        {
            CheckRow(run, acceptedExists);
            Guard.Require(ids.Add(run.RunId), "DOC-RUN-ID");
            // At most one Completed row per key, judged on intact rows by their recomputed key; Failed rows may repeat.
            if (run.Outcome is RunOutcome.Completed && Verify(run))
                Guard.Require(completedKeys.Add(run.RunKey), "DOC-RUN-KEY");
        }
        var grains = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in records.PolarSamples)
        {
            Guard.Require(Present(sample) && Present(sample.MethodId, sample.MethodVersion, sample.SurfaceState), "DOC-SCHEMA");
            RequireHash(sample.ProfileHash); RequireHash(sample.WaterHash);
            RequireFinite(sample.Reynolds, sample.Ncrit, sample.AlphaDeg);
            RequireFinite(sample.Cl, sample.Cd, sample.Cm, sample.XtrUpper, sample.XtrLower, sample.CpMin, sample.Confidence);
            string grain = Jcs.Write(new object?[]
            {
                sample.ProfileHash, sample.MethodId, sample.MethodVersion, sample.Reynolds, sample.Ncrit, sample.SurfaceState,
                sample.AlphaDeg, sample.WaterHash
            });
            Guard.Require(grains.Add(grain), "DOC-POLAR-KEY");
        }
        foreach (var tombstone in records.Pruned)
        {
            Guard.Require(Present(tombstone), "DOC-SCHEMA");
            NativeProject.Uuid(tombstone.RunId); RequireHash(tombstone.RunKey);
            Guard.Require(!ids.Contains(tombstone.RunId), "DOC-RUN-ID");
        }
    }

    private static void CheckRow(AnalysisRun run, Func<string, bool> acceptedExists)
    {
        Guard.Require(Present(run) && Present(run.Outcome, run.Tier, run.Method, run.Settings, run.Inputs, run.Water, run.Op,
            run.Reference, run.Strips, run.Platform), "DOC-SCHEMA");
        // A Completed row carries the solver's diagnostics; a Failed row has none to carry (IO8: absent, never zero).
        Guard.Require((run.Outcome is RunOutcome.Completed) == (run.Diagnostics is not null), "DOC-SCHEMA");
        NativeProject.Uuid(run.RunId);
        RequireHash(run.RunKey); RequireHash(run.ContentHash); RequireHash(run.SettingsHash);
        RequireHash(run.Inputs.SurfaceHash); RequireHash(run.Water.TableHash);
        Guard.Require(Present(run.Inputs.ProfileHashes, run.Settings.Ncrit), "DOC-SCHEMA");
        foreach (string profile in run.Inputs.ProfileHashes) RequireHash(profile);
        NativeProject.Uuid(run.Inputs.AcceptedId);
        Guard.Require(acceptedExists(run.Inputs.AcceptedId), "DOC-REFERENCE");
        RequireFinite(run.Diagnostics?.ResidualInf, run.Diagnostics?.Kappa1);
        RequireFinite((run.Settings.SectionEtas ?? []).Concat(run.Settings.SectionXs ?? []).ToArray());
        RequireFinite(run.ReconciliationTolerance, run.WallMs, run.Water.TemperatureC, run.Water.SalinityGPerKg, run.Water.Rho, run.Water.Nu, run.Water.Pv,
            run.Op.Speed, run.Op.PAtm, run.Op.AlphaDeg, run.Reference.SRef, run.Reference.BRef, run.Reference.CRef,
            run.Settings.SingularityCutoff, run.Settings.TeFloorMm);
        RequireFinite(run.Op.HRef, run.Op.Load);
        // Strips are spanwise lattice rows 0…n−1 in array order, n within the cap, and only a Completed run has them.
        Guard.Require(run.Strips.Count <= MaxStrips && (run.Outcome is RunOutcome.Completed || run.Strips.Count == 0), "DOC-RUN-STRIPS");
        for (int j = 0; j < run.Strips.Count; j++)
        {
            var strip = run.Strips[j];
            Guard.Require(Present(strip) && Present(strip.CdNcrit2, strip.CdNcrit4), "DOC-SCHEMA");
            Guard.Require(strip.J == j, "DOC-RUN-STRIPS");
            RequireFinite(strip.Y, strip.Eta, strip.Chord, strip.Gamma, strip.AlphaI, strip.AlphaEff, strip.ReLocal, strip.ClLocal,
                strip.Fx, strip.Fy, strip.Fz, strip.Mx, strip.My, strip.Mz, strip.DownwashTrefftz);
            RequireFinite(strip.CdNcrit2.Value, strip.CdNcrit4.Value);
            // A value, or Unavailable with its reason: never both, never neither.
            Guard.Require((strip.CdNcrit2.Value is null) != (strip.CdNcrit2.UnavailableReason is null) &&
                          (strip.CdNcrit4.Value is null) != (strip.CdNcrit4.UnavailableReason is null), "DOC-SCHEMA");
            // Expand only: absent provisional is false and carries no reason. The one accepted reason is the tip code.
            bool tip = strip.ProvisionalReason == StripLoad.TipProvisionalReason;
            Guard.Require(strip.Provisional == tip && (strip.Provisional || strip.ProvisionalReason is null), "DOC-SCHEMA");
        }
    }

    // A deserialized row can hold null where the record says non-null; checked as objects so the nullable flow state of
    // the members is not widened.
    private static bool Present(params object?[] values) => values.All(value => value is not null);
    private static void RequireHash(string? hash) => Guard.Require(hash is not null && Hex64.IsMatch(hash), "DOC-INTEGRITY");
    private static void RequireFinite(params double[] values) => Guard.Require(values.All(double.IsFinite), "DOC-SCHEMA");
    private static void RequireFinite(params double?[] values) => Guard.Require(values.All(value => value is null || double.IsFinite(value.Value)), "DOC-SCHEMA");

    private static string Blake3Jcs(object? canonical) => Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(canonical)));

    // A serialized JSON value as the object tree Jcs.Write accepts; `drop` removes one top-level member.
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

/// <summary>
/// <c>{"status":"completed"}</c> or <c>{"status":"failed","code":…,"reason":…}</c>. Any other shape is refused, so the
/// reader's schema stays closed (the native reader maps the exception to <c>DOC-SCHEMA</c>).
/// </summary>
internal sealed class RunOutcomeConverter : JsonConverter<RunOutcome>
{
    public override RunOutcome Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("outcome is not an object");
        string[] names = root.EnumerateObject().Select(member => member.Name).ToArray();
        string? status = root.TryGetProperty("status", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (status == "completed" && names.SequenceEqual(["status"])) return new RunOutcome.Completed();
        if (status == "failed" && names.Order(StringComparer.Ordinal).SequenceEqual(["code", "reason", "status"]) &&
            root.GetProperty("code").ValueKind == JsonValueKind.String && root.GetProperty("reason").ValueKind == JsonValueKind.String)
            return new RunOutcome.Failed(root.GetProperty("code").GetString()!, root.GetProperty("reason").GetString()!);
        throw new JsonException("outcome has an unknown shape");
    }

    public override void Write(Utf8JsonWriter writer, RunOutcome value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case RunOutcome.Completed:
                writer.WriteString("status", "completed");
                break;
            case RunOutcome.Failed failed:
                writer.WriteString("status", "failed");
                writer.WriteString("code", failed.Code);
                writer.WriteString("reason", failed.Reason);
                break;
            default:
                throw new JsonException("outcome has an unknown type");
        }
        writer.WriteEndObject();
    }
}
