using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CfdWorkbench.Core;

// A3d owns this file (Ruling 250 Q4): the Discrepancy record, its line, the compare rule identity, the content hash, the one
// definition of canonical orientation and the comparison key, the store checks and the CompareEvent telemetry fields.
// Compare in CfdWorkbench.Analysis reads it and never adds members. Design docs/design/a3d-compare.md section 1.

/// <summary>The compare rule a record was written under: the quantity set, the delta formula, the floors, the exclusions. A rule change is a version bump.</summary>
public sealed record CompareRule(string Id, string Version)
{
    public const string RuleId = "cfdw.compare";
    public const string V100 = "1.0.0";
    public static CompareRule Current { get; } = new(RuleId, V100);
}

/// <summary>
/// One Discrepancy line: a compared quantity in canonical orientation, exactly one of a delta, an Undefined code or a
/// Not-compared code. <paramref name="Base"/> and <paramref name="Compared"/> are always written; null only on the
/// estimator's side of a Not-compared line.
/// </summary>
public sealed record DiscrepancyLine(string Quantity, double? Base, double? Compared,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Delta = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Undefined = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NotCompared = null);

/// <summary>
/// One comparison of one unordered pair of distinct Completed runs, stored in canonical orientation under one compare-rule
/// version (Ruling 250 Q2). Append-only; references the runs and is never referenced by them. The content hash is stored
/// but never trusted: it is recomputed on read.
/// </summary>
public sealed record DiscrepancyRecord(string Id, string ContentHash, CompareRule Rule, string BaseRunId, string ComparedRunId,
    IReadOnlyList<DiscrepancyLine> Lines);

/// <summary>A recorded comparison, and whether this call created it (false is a read of an earlier record).</summary>
public sealed record DiscrepancyOutcome(DiscrepancyRecord Record, bool Created);

/// <summary>
/// The Compare fields of an <c>analysis.compare</c> event and of the save-time pinned-run report (design 6.1, Ruling 251).
/// A field not measured stays null and reads "not recorded", never zero.
/// </summary>
public sealed record CompareEvent
{
    public string? BaseTier { get; init; }
    public string? ComparedTier { get; init; }
    /// <summary>The record count after the event.</summary>
    public int? Records { get; init; }
    /// <summary>Runs kept only because a record references them.</summary>
    public int? PinnedRuns { get; init; }
    /// <summary>The size in bytes of the save image at the event.</summary>
    public int? DocumentBytes { get; init; }
}

/// <summary>Canonical orientation, the comparison key, the content hash and the store checks of the Discrepancy records.</summary>
public static class DiscrepancyRecords
{
    public static readonly string[] Quantities = ["CL", "CDi", "e"];
    private static readonly string[] UndefinedCodes = ["ANA-CMP-BASE-ZERO", "ANA-CMP-BASE-FLOOR", "ANA-CMP-E-ILL-CONDITIONED"];
    private static readonly string[] NotComparedCodes = ["ANA-CMP-ESTIMATOR-E"];
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Estimator below polar below VLM + strip. Any other tier id is refused by the run row check before it is ranked.</summary>
    public static int TierRank(string tier) => tier switch { "estimator" => 0, "polar" => 1, "vlm-strip" => 2, _ => 3 };

    /// <summary>
    /// The one definition of canonical orientation (Ruling 250 Q2): lower tier first; the same tier, the earlier accepted
    /// revision first (its ordinal in accepted-row order), then the earlier run in document order. Returns true when
    /// <paramref name="a"/> comes first.
    /// </summary>
    public static bool AFirst(string tierA, int ordinalA, int indexA, string tierB, int ordinalB, int indexB)
    {
        int rank = TierRank(tierA).CompareTo(TierRank(tierB));
        if (rank != 0) return rank < 0;
        if (ordinalA != ordinalB) return ordinalA < ordinalB;
        return indexA < indexB;
    }

    /// <summary>The comparison key of an oriented pair: the same whichever way the user opened it.</summary>
    public static string Key(string baseRunId, string comparedRunId, CompareRule rule) =>
        string.Join('|', baseRunId, comparedRunId, rule.Id, rule.Version);

    public static string Key(DiscrepancyRecord record) => Key(record.BaseRunId, record.ComparedRunId, record.Rule);

    /// <summary>BLAKE3 over JCS of the record without its <c>contentHash</c>.</summary>
    public static string ContentHash(DiscrepancyRecord record) =>
        Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(Canonical(JsonSerializer.SerializeToElement(record, Options), "contentHash"))));

    public static DiscrepancyRecord Seal(DiscrepancyRecord record) => record with { ContentHash = ContentHash(record) };

    /// <summary>
    /// The structural store invariants (design 1.8), shared by the reader and the session so both refuse the same rows with
    /// the same codes. Per-record integrity (hash, reproduction) is not structural: a failing record stays, Unavailable.
    /// <paramref name="acceptedOrdinal"/> is the accepted-row ordinal of a revision, null when the id names none.
    /// </summary>
    internal static void Check(AnalysisRecords records, Func<string, int?> acceptedOrdinal)
    {
        if (records.Discrepancies is not { } list) return;
        Guard.Require(list.Count > 0, "DOC-SCHEMA");
        var byId = new Dictionary<string, (AnalysisRun Run, int Index)>(StringComparer.Ordinal);
        for (int i = 0; i < records.Runs.Count; i++) byId[records.Runs[i].RunId] = (records.Runs[i], i);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in list)
        {
            Guard.Require(record is not null && record.Rule is not null && record.Lines is not null && record.Id is not null &&
                          record.BaseRunId is not null && record.ComparedRunId is not null && record.ContentHash is not null, "DOC-SCHEMA");
            string id = record!.Id!, baseId = record.BaseRunId!, comparedId = record.ComparedRunId!;
            Guard.Require(IsUuid(id) && ids.Add(id), "DOC-DISCREPANCY-ID");
            Guard.Require(keys.Add(Key(record)), "DOC-DISCREPANCY-KEY");
            Guard.Require(baseId != comparedId, "DOC-SCHEMA");
            (AnalysisRun Run, int Index) first = default, second = default;
            Guard.Require(byId.TryGetValue(baseId, out first) && byId.TryGetValue(comparedId, out second) &&
                          first.Run!.Outcome is RunOutcome.Completed && second.Run!.Outcome is RunOutcome.Completed, "DOC-REFERENCE");
            int? ordinalFirst = acceptedOrdinal(first.Run.Inputs.AcceptedId), ordinalSecond = acceptedOrdinal(second.Run!.Inputs.AcceptedId);
            Guard.Require(ordinalFirst is not null && ordinalSecond is not null, "DOC-REFERENCE");
            Guard.Require(AFirst(first.Run.Tier, ordinalFirst!.Value, first.Index, second.Run.Tier, ordinalSecond!.Value, second.Index), "DOC-SCHEMA");
            Guard.Require(System.Text.RegularExpressions.Regex.IsMatch(record.ContentHash!, @"\A[0-9a-f]{64}\z"), "DOC-INTEGRITY");
            CheckLines(record.Lines!);
        }
    }

    private static void CheckLines(IReadOnlyList<DiscrepancyLine> lines)
    {
        int last = -1;
        foreach (var line in lines)
        {
            Guard.Require(line is not null && line.Quantity is not null, "DOC-SCHEMA");
            int at = Array.IndexOf(Quantities, line!.Quantity!);
            Guard.Require(at > last, "DOC-SCHEMA");
            last = at;
            Guard.Require(Finite(line.Base) && Finite(line.Compared) && Finite(line.Delta), "DOC-SCHEMA");
            int shapes = (line.Delta is null ? 0 : 1) + (line.Undefined is null ? 0 : 1) + (line.NotCompared is null ? 0 : 1);
            Guard.Require(shapes == 1, "DOC-SCHEMA");
            Guard.Require(line.Undefined is null || UndefinedCodes.Contains(line.Undefined), "DOC-SCHEMA");
            Guard.Require(line.NotCompared is null || NotComparedCodes.Contains(line.NotCompared), "DOC-SCHEMA");
            // A value on both sides, except the estimator's side of a Not-compared line.
            Guard.Require(line.NotCompared is not null || (line.Base is not null && line.Compared is not null), "DOC-SCHEMA");
        }
    }

    private static bool Finite(double? value) => value is null || double.IsFinite(value.Value);
    private static bool IsUuid(string id) => Guid.TryParseExact(id, "D", out _) && id == id.ToLowerInvariant();

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
