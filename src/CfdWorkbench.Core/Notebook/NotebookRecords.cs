using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.Notebook;

/// <summary>Type-2 immutable <c>FoilReq 1.0</c> snapshot stored as <c>&lt;= 1,024 B</c> UTF-8 chunked lines.</summary>
public sealed record RequirementSpecVersion(
    string ReqVersionId,
    string OrdinalLabel,
    string? ParentReqVersionId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<string> DslLines,
    string ContentHash);

/// <summary>Append-only notebook turn fact stored as <c>&lt;= 1,024 B</c> UTF-8 chunked lines.</summary>
public sealed record NotebookTurnRecord(
    string TurnId,
    DateTimeOffset UtcTimestamp,
    string Kind,
    string? ReqVersionId,
    string? CandidateId,
    IReadOnlyList<string> BodyLines,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? WallTimeMs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ErrorCode = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RepairAttempt = null);

/// <summary>
/// Self-contained local solver operating-point summary stored inside <see cref="CandidateIterationRecord"/>
/// — never writes dangling unaccepted runs into <c>Envelope.Analysis</c> (preserves <c>RunRecord.CheckRow</c>).
/// </summary>
public sealed record CandidateOperatingPointSummary(
    string OpId,
    double SpeedKn,
    double AlphaDeg,
    double Cl,
    double Cd,
    double? WingOnlyLd,
    double? Ncrit2Ld,
    double? Ncrit4Ld,
    double ReMac,
    double? CpMin,
    double? Sigma,
    double? CavMargin,
    double? VCritKn,
    double? GoverningEta,
    double? HOverCRoot,
    double? FrH,
    bool DepthUnset,
    string MethodVersion,
    string? UnavailableReason,
    double SolverWallMs);

/// <summary>
/// Type-2 immutable 3D candidate iteration (<c>v1..vn</c>). Unaccepted <c>FoilDSL</c> bytes are NOT stored
/// (derive-don't-store DM7) — deterministically rebuilt from <c>(RecipeSeederVersion, Delta)</c> and verified on reopen.
/// </summary>
public sealed record CandidateIterationRecord(
    string CandidateId,
    int Ordinal,
    string ReqVersionId,
    CandidateIntentDelta Delta,
    string RecipeSeederVersion,
    string SurfaceHash,
    string GeometryStatus,
    string? RefusalCode,
    IReadOnlyList<CandidateOperatingPointSummary> OperatingPoints,
    int RepairAttemptIndex,
    DateTimeOffset CreatedUtc,
    string ContentHash);

/// <summary>Append-only fact recording a user-confirmed transfer of a Certified candidate into Area 2 CAD.</summary>
public sealed record CadHandoffRecord(
    string HandoffId,
    string CandidateId,
    string SurfaceHash,
    string SeededBriefId,
    string SeededGoalStateId,
    string SeededAcceptedId,
    DateTimeOffset HandedOffUtc);

/// <summary>
/// Aggregate root for <c>Envelope.Notebook</c> (0..1 per <c>.cfdw.json</c> project in v1).
/// </summary>
// simplify: 0..1 NotebookSessionRecord per .cfdw.json project in v1; ceiling: 1 session, <= 200 turns, <= 50 candidates; upgrade trigger: when users request parallel multi-brief notebooks inside one project.
public sealed record NotebookSessionRecord(
    string SessionId,
    string Title,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<RequirementSpecVersion> Requirements,
    IReadOnlyList<NotebookTurnRecord> Turns,
    IReadOnlyList<CandidateIterationRecord> Candidates,
    IReadOnlyList<CadHandoffRecord> Handoffs,
    string ContentHash);

/// <summary>Zero-PII structured telemetry fields for <c>notebook.*</c> events on <see cref="AuthoringSession"/>.</summary>
public sealed record NotebookTelemetry(
    string EventKind,
    bool? HasExisting = null,
    bool? HasKey = null,
    bool? EvalPassed = null,
    string? ModelId = null,
    int? PayloadBytes = null,
    bool? Valid = null,
    bool? UnitFlagged = null,
    bool? PhysicsTension = null,
    string? GrillAction = null,
    int? OpenCount = null,
    bool? MarkedStale = null,
    int? CandidateOrdinal = null,
    string? GeometryStatus = null,
    double? SolverWallMs = null,
    int? OpCount = null,
    int? RepairAttempt = null,
    int? RevisionOrdinal = null,
    string? Code = null);

/// <summary>Result of <see cref="AuthoringSession.HandoffNotebookCandidate"/>.</summary>
public sealed record CadHandoffOutcome(
    CadHandoffRecord Handoff,
    SetupSeedOutcome SeedOutcome);

/// <summary>
/// Pure Core helpers for <see cref="NotebookSessionRecord"/>: <c>&lt;= 1,024 B</c> UTF-8 line-array chunking
/// (respecting <c>NativeProject</c>'s <c>4,096 B</c>/line cap even under heavy JSON escaping), BLAKE3(JCS) content hashing,
/// deterministic candidate <c>FoilDSL</c> rebuild (<c>MaterializeCandidateDsl</c>), and <c>NativeProject.Check</c> store validation.
/// </summary>
public static class NotebookRecords
{
    public const int MaxRawChunkUtf8Bytes = 1_024;
    private const int MaxJsonEncodedChunkBytes = 3_400;
    public const int MaxTurns = 200;
    public const int MaxCandidates = 50;

    private static readonly JsonSerializerOptions HashJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    /// <summary>
    /// Splits arbitrary text into rune-safe UTF-8 chunks where every chunk is <c>&lt;= 1,024</c> raw UTF-8 bytes
    /// AND <c>&lt;= 3,400</c> JSON-escaped bytes, guaranteeing every serialized JSON line in <c>.cfdw.json</c> stays
    /// strictly <c>&lt;= 3,800 B</c> (well below <c>NativeProject</c>'s <c>4,096 B</c> per-line cap).
    /// </summary>
    public static string[] ChunkText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var chunks = new List<string>();
        var current = new StringBuilder();
        int rawBytes = 0;
        int escapedBytes = 0;

        foreach (Rune rune in text.EnumerateRunes())
        {
            int rBytes = rune.Utf8SequenceLength;
            int eBytes = EstimateJsonEscapedBytes(rune);

            if (current.Length > 0 && (rawBytes + rBytes > MaxRawChunkUtf8Bytes || escapedBytes + eBytes > MaxJsonEncodedChunkBytes))
            {
                chunks.Add(current.ToString());
                current.Clear();
                rawBytes = 0;
                escapedBytes = 0;
            }

            current.Append(rune);
            rawBytes += rBytes;
            escapedBytes += eBytes;
        }

        if (current.Length > 0)
            chunks.Add(current.ToString());

        return chunks.ToArray();
    }

    public static string JoinChunks(IReadOnlyList<string>? chunks) =>
        chunks is null || chunks.Count == 0 ? "" : string.Concat(chunks);

    public static RequirementSpecVersion CreateRequirementVersion(
        FoilReqDocument doc,
        string ordinalLabel,
        string? parentReqVersionId = null,
        string? reqVersionId = null,
        DateTimeOffset? createdUtc = null)
    {
        ArgumentNullException.ThrowIfNull(doc);
        string id = string.IsNullOrWhiteSpace(reqVersionId) ? Guid.NewGuid().ToString("D") : reqVersionId.Trim();
        string dsl = FoilReqSerializer.Format(doc);
        string hash = FoilReqSerializer.ComputeContentHash(doc);
        return new RequirementSpecVersion(
            ReqVersionId: id,
            OrdinalLabel: ordinalLabel,
            ParentReqVersionId: parentReqVersionId,
            CreatedUtc: (createdUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            DslLines: ChunkText(dsl),
            ContentHash: hash);
    }

    public static CandidateIterationRecord CreateCandidateRecord(
        int ordinal,
        string reqVersionId,
        CandidateIntentDelta delta,
        string surfaceHash,
        string geometryStatus,
        string? refusalCode,
        IReadOnlyList<CandidateOperatingPointSummary> operatingPoints,
        int repairAttemptIndex = 0,
        string? candidateId = null,
        DateTimeOffset? createdUtc = null)
    {
        ArgumentNullException.ThrowIfNull(delta);
        string id = string.IsNullOrWhiteSpace(candidateId) ? $"v{ordinal.ToString(CultureInfo.InvariantCulture)}" : candidateId.Trim();
        var draft = new CandidateIterationRecord(
            CandidateId: id,
            Ordinal: ordinal,
            ReqVersionId: reqVersionId,
            Delta: delta,
            RecipeSeederVersion: RecipeSeeder.SolverVersion,
            SurfaceHash: surfaceHash,
            GeometryStatus: geometryStatus,
            RefusalCode: refusalCode,
            OperatingPoints: operatingPoints.ToArray(),
            RepairAttemptIndex: repairAttemptIndex,
            CreatedUtc: (createdUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            ContentHash: "");
        return draft with { ContentHash = CandidateContentHash(draft) };
    }

    public static NotebookSessionRecord FinalizeSession(NotebookSessionRecord draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return draft with { ContentHash = SessionContentHash(draft) };
    }

    public static string CandidateContentHash(CandidateIterationRecord candidate) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(candidate, HashJsonOptions), drop: "contentHash"));

    public static string SessionContentHash(NotebookSessionRecord session) =>
        Blake3Jcs(Canonical(JsonSerializer.SerializeToElement(session, HashJsonOptions), drop: "contentHash"));

    public static bool VerifyCandidateHash(CandidateIterationRecord candidate)
    {
        try { return candidate.ContentHash == CandidateContentHash(candidate); }
        catch (ContractError) { return false; }
    }

    public static bool VerifySessionHash(NotebookSessionRecord session)
    {
        try { return session.ContentHash == SessionContentHash(session); }
        catch (ContractError) { return false; }
    }

    /// <summary>
    /// Deterministically regenerates candidate <c>FoilDSL</c> UTF-8 bytes from <paramref name="delta"/> using
    /// two-profile <see cref="RecipeSeeder.EmitFoilDsl"/> (bundled GEN catalog OR embedded <c>My sections</c> coordinate snapshot).
    /// Guarantees <c>SurfaceHash_Rebuilt == SurfaceHash_Recorded</c> across machines without duplicating unaccepted DSL in <c>.cfdw.json</c>.
    /// </summary>
    public static byte[] MaterializeCandidateDsl(CandidateIntentDelta delta, string foilName = "Notebook Candidate")
    {
        ArgumentNullException.ThrowIfNull(delta);
        double rootChordM = delta.RootChordMeters;
        double tipChordM = delta.TipChordMeters;

        var recipe = new RecipeRecord(
            SolverVersion: RecipeSeeder.SolverVersion,
            PlanformFamily: RecipeSeeder.PlanformFamily,
            SpanMeters: delta.SpanMeters,
            RootChordMeters: rootChordM,
            TipChordMeters: tipChordM,
            Taper: delta.Taper,
            SweepQuarterChordDeg: delta.SweepQuarterChordDeg,
            AnhedralDeg: delta.AnhedralDeg,
            WashoutDeg: delta.WashoutDeg,
            RootThicknessRatio: delta.RootThicknessRatio,
            ProfileId: delta.RootProfileId,
            ProfileOrigin: delta.CustomRootProfileSnapshotLines is { Count: > 0 } ? "my:" + delta.RootProfileId : "gen:" + delta.RootProfileId,
            PreferredProfileId: delta.RootProfileId,
            FallbackReason: null,
            ConflictNotice: null,
            Deviations: []);

        (string rootDat, string rootProv) = ResolveProfileDat(delta.RootProfileId, delta.CustomRootProfileSnapshotLines);
        (string tipDat, string tipProv) = ResolveProfileDat(delta.TipProfileId, delta.CustomTipProfileSnapshotLines);

        byte[] raw = RecipeSeeder.EmitFoilDsl(
            recipe: recipe,
            rootId: delta.RootProfileId,
            rootDatText: rootDat,
            tipId: delta.TipProfileId,
            tipDatText: tipDat,
            foilName: foilName,
            rootProvenance: rootProv,
            tipProvenance: tipProv,
            tipStyle: delta.TipStyle,
            tipCapRatio: delta.TipCapRatio);

        var parsed = FoilSource.Parse(raw);
        if (!parsed.IsParsed)
            throw new ContractError(parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-SYNTAX");
        return FoilSource.MaterializeIds(parsed);
    }

    /// <summary>
    /// Structural, chunk-size, referential, and rebuild-equality checks shared by <c>NativeProject.Check</c>
    /// and <see cref="AuthoringSession.RecordNotebookSession"/>.
    /// </summary>
    internal static void CheckStore(
        NotebookSessionRecord session,
        Func<string, bool> acceptedExists,
        Func<string, string?> acceptedSurfaceHash,
        Func<string, bool> briefExists,
        Func<string, bool> goalExists)
    {
        if (session is null || session.Requirements is null || session.Turns is null || session.Candidates is null || session.Handoffs is null)
            throw new ContractError("DOC-SCHEMA");
        NativeProject.Uuid(session.SessionId);
        Guard.Require(!string.IsNullOrWhiteSpace(session.Title) && Encoding.UTF8.GetByteCount(session.Title) <= 512, "DOC-SCHEMA");
        Guard.Require(session.Requirements.Count + session.Turns.Count + session.Candidates.Count + session.Handoffs.Count > 0, "DOC-SCHEMA");
        Guard.Require(session.Turns.Count <= MaxTurns && session.Candidates.Count <= MaxCandidates, "DOC-SCHEMA");
        RequireHash(session.ContentHash);
        Guard.Require(VerifySessionHash(session), "DOC-INTEGRITY");

        var reqIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var req in session.Requirements)
        {
            Guard.Require(req is not null, "DOC-SCHEMA");
            NativeProject.Uuid(req!.ReqVersionId);
            Guard.Require(reqIds.Add(req.ReqVersionId), "DOC-REFERENCE");
            if (req.ParentReqVersionId is not null)
                Guard.Require(reqIds.Contains(req.ParentReqVersionId), "DOC-REFERENCE");
            CheckChunks(req.DslLines);
            RequireHash(req.ContentHash);
            string dslText = JoinChunks(req.DslLines);
            var parsed = FoilReqParser.Parse(dslText);
            Guard.Require(parsed.IsValid && parsed.Document is not null, "DOC-SCHEMA");
            Guard.Require(FoilReqSerializer.ComputeContentHash(parsed.Document!) == req.ContentHash, "DOC-INTEGRITY");
        }

        var turnIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var turn in session.Turns)
        {
            Guard.Require(turn is not null, "DOC-SCHEMA");
            NativeProject.Uuid(turn!.TurnId);
            Guard.Require(turnIds.Add(turn.TurnId), "DOC-REFERENCE");
            Guard.Require(!string.IsNullOrWhiteSpace(turn.Kind), "DOC-SCHEMA");
            if (turn.ReqVersionId is not null)
                Guard.Require(reqIds.Contains(turn.ReqVersionId), "DOC-REFERENCE");
            CheckChunks(turn.BodyLines);
        }

        var candById = new Dictionary<string, CandidateIterationRecord>(StringComparer.Ordinal);
        foreach (var cand in session.Candidates)
        {
            if (cand is null || cand.Delta is null || cand.OperatingPoints is null)
                throw new ContractError("DOC-SCHEMA");
            Guard.Require(!string.IsNullOrWhiteSpace(cand.CandidateId) && candById.TryAdd(cand.CandidateId, cand), "DOC-REFERENCE");
            Guard.Require(cand.Ordinal >= 1 && cand.RepairAttemptIndex is >= 0 and <= 2, "DOC-SCHEMA");
            Guard.Require(reqIds.Contains(cand.ReqVersionId), "DOC-REFERENCE");
            RequireHash(cand.Delta.DeltaHash);
            Guard.Require(CandidateIntentDelta.ComputeDeltaHash(cand.Delta) == cand.Delta.DeltaHash, "DOC-INTEGRITY");
            if (cand.Delta.CustomRootProfileSnapshotLines is not null) CheckChunks(cand.Delta.CustomRootProfileSnapshotLines);
            if (cand.Delta.CustomTipProfileSnapshotLines is not null) CheckChunks(cand.Delta.CustomTipProfileSnapshotLines);
            RequireHash(cand.ContentHash);
            Guard.Require(VerifyCandidateHash(cand), "DOC-INTEGRITY");

            if (string.Equals(cand.GeometryStatus, nameof(GeometryStatus.Certified), StringComparison.Ordinal))
            {
                RequireHash(cand.SurfaceHash);
                // Rebuild equality check (DM7): deterministic SurfaceHash_Rebuilt == SurfaceHash_Recorded.
                byte[] rebuilt = MaterializeCandidateDsl(cand.Delta);
                var rebuiltParse = FoilSource.Parse(rebuilt);
                Guard.Require(rebuiltParse.IsParsed && rebuiltParse.SurfaceHash == cand.SurfaceHash, "DOC-INTEGRITY");
            }
        }

        var handoffIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var h in session.Handoffs)
        {
            Guard.Require(h is not null, "DOC-SCHEMA");
            NativeProject.Uuid(h!.HandoffId);
            Guard.Require(handoffIds.Add(h.HandoffId), "DOC-REFERENCE");
            NativeProject.Uuid(h.SeededBriefId);
            NativeProject.Uuid(h.SeededGoalStateId);
            NativeProject.Uuid(h.SeededAcceptedId);
            RequireHash(h.SurfaceHash);

            Guard.Require(candById.TryGetValue(h.CandidateId, out var cand), "DOC-REFERENCE");
            Guard.Require(string.Equals(cand!.GeometryStatus, nameof(GeometryStatus.Certified), StringComparison.Ordinal), "DOC-REFERENCE");
            Guard.Require(cand.SurfaceHash == h.SurfaceHash, "DOC-REFERENCE");
            Guard.Require(acceptedExists(h.SeededAcceptedId), "DOC-REFERENCE");
            Guard.Require(acceptedSurfaceHash(h.SeededAcceptedId) == h.SurfaceHash, "DOC-REFERENCE");
            Guard.Require(briefExists(h.SeededBriefId), "DOC-REFERENCE");
            Guard.Require(goalExists(h.SeededGoalStateId), "DOC-REFERENCE");
        }
    }

    public static string DeterministicSessionTitle(FoilReqDocument doc)
    {
        string disc = doc.Brief.Discipline;
        var riderMass = doc.Rider.Find("mass");
        string massPart = riderMass is not null ? $" · {riderMass.Quantity.Format()}" : "";
        var maxSpan = doc.FindConstraint("max_span");
        string spanPart = maxSpan is { Quantity: { } sq } ? $" · <= {sq.Format()}" : "";
        return $"{disc}{massPart}{spanPart}";
    }

    private static (string DatText, string Provenance) ResolveProfileDat(string profileId, IReadOnlyList<string>? snapshotLines)
    {
        if (snapshotLines is { Count: > 0 })
        {
            string dat = JoinChunks(snapshotLines);
            return (dat, "my:" + profileId);
        }

        var entry = Catalog.Load().FirstOrDefault(e => string.Equals(e.Id, profileId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ContractError("CAT-UNAVAILABLE", $"Profile '{profileId}' is not in the admitted catalog and has no embedded snapshot.");
        if (entry.DisabledReason is not null || entry.Coordinates is null)
            throw new ContractError("CAT-UNAVAILABLE", entry.DisabledReason ?? $"Profile '{profileId}' is unavailable.");
        return (FoilSource.Utf8.GetString(entry.Coordinates), "gen:" + entry.Id);
    }

    private static void CheckChunks(IReadOnlyList<string> chunks)
    {
        Guard.Require(chunks is not null, "DOC-SCHEMA");
        foreach (string chunk in chunks!)
        {
            Guard.Require(chunk is not null && Encoding.UTF8.GetByteCount(chunk) <= MaxRawChunkUtf8Bytes, "DOC-SIZE");
        }
    }

    private static int EstimateJsonEscapedBytes(Rune rune)
    {
        int v = rune.Value;
        if (v is '"' or '\\' or '\b' or '\f' or '\n' or '\r' or '\t') return 2;
        if (v < 0x20 || v is '<' or '>' or '&' or '\'' or '+') return 6; // System.Text.Json default encoder escapes HTML-sensitive chars as \uXXXX
        if (v > 0x7F) return v <= 0xFFFF ? 6 : 12; // non-ASCII may be \uXXXX escaped
        return 1;
    }

    private static void RequireHash(string? hash) =>
        Guard.Require(hash is not null && Hex64.IsMatch(hash), "DOC-INTEGRITY");

    private static string Blake3Jcs(object? canonical) =>
        Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(canonical)));

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
