using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

public sealed record SourceRow(string Id, string[] Utf8Base64Chunks);
public sealed record DesignRow(string Id, string? Parent, string SurfaceHash, string Evaluator);
public sealed record EditReceipt(string DraftId, long Generation, string Rail, string VertexId);
public sealed record AcceptedRow(string Id, string? Parent, string SourceId, string DesignId, string OperationId, EditReceipt? Edit);
public sealed record CursorRow(long Sequence, string Target, string Reason, string OperationId);
public sealed record RecoveryRow(string DraftId, string BaseAcceptedId, long Generation, string Rail, string VertexId, string[] Utf8Base64Chunks);
public sealed record Envelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted, CursorRow[] Cursors, RecoveryRow? Recovery);
public sealed record SessionDraft(string Id, string Base, long Generation, string Rail, string VertexId, byte[] Bytes);
public sealed record SessionBinding(string SourceHash, string Base, string DraftId, long Generation, string Evaluator, string SurfaceHash, string Rail, string VertexId);

public sealed record SessionView(string AcceptedId, string SourceHash, string SurfaceHash, byte[] Source, SessionDraft? Draft, RecoveryRow? Recovery, bool Dirty);
public sealed record SessionEvent(long Sequence, string Operation, string Outcome, double DurationMilliseconds, int? InputBytes,
    int? OutputBytes, string? TraceId, long? Generation, string? Evaluator, int RetainedSources, int AcceptedFacts, string Action);
public sealed record SessionPreview(SessionBinding Binding, PlacedPointEnclosure Point, double UniformWidthUpper);

public sealed class SessionAssessment
{
    internal SessionAssessment(Guid owner, GeometryStatus status, string code, SessionBinding? key, GeometryCertificate? certificate)
    { Owner = owner; Status = status; Code = code; Key = key; Certificate = certificate; }
    internal Guid Owner { get; }
    public GeometryStatus Status { get; }
    public string Code { get; }
    public SessionBinding? Key { get; }
    public GeometryCertificate? Certificate { get; }
}

public sealed class AuthoringSession : IDisposable
{
    private readonly Queue<SessionEvent> events = new();
    private readonly AsyncLocal<string?> trace = new();
    private long eventSequence;
    private bool closed;
    public IReadOnlyList<SessionEvent> ReadLocalEvents()
    { lock (sync) return Array.AsReadOnly(events.ToArray()); }
    public void Dispose()
    {
        lock (sync)
        {
            closed = true; events.Clear(); capturedSaveHashes.Clear(); retiredDraftIds.Clear();
            sources.Clear(); designs.Clear(); accepted.Clear(); cursors.Clear(); redo.Clear(); operations.Clear();
            draft = null; recovery = null; current = null;
        }
    }
    private T Run<T>(string operation, Func<T> action, int? inputBytes = null, long? generation = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); string outcome = "OK";
        string? priorTrace = trace.Value; trace.Value = Guid.NewGuid().ToString("N");
        try
        {
            lock (sync) Guard.Require(!closed, "DOC-CLOSED");
            var result = action();
            if (result is SessionAssessment assessment) outcome = assessment.Code;
            return result;
        }
        catch (ContractError error) { outcome = error.Code; throw; }
        catch (Exception) { outcome = "INTERNAL-ERROR"; throw; }
        finally
        {
            string name = operation switch
            {
                "undo" or "redo" => "document.cursor",
                "capture-recovery" or "resume-recovery" or "discard-recovery" => "document.recovery",
                "capture-save" or "acknowledge-save" => "document.save",
                _ => operation.StartsWith("geometry.", StringComparison.Ordinal) ? operation : "document." + operation
            };
            Record(name, outcome, timer.Elapsed.TotalMilliseconds, inputBytes, null, generation, null, operation);
            trace.Value = priorTrace;
        }
    }
    private void Record(string operation, string outcome, double elapsed, int? inputBytes, int? outputBytes, long? generation, string? evaluator, string? action = null)
    {
        lock (sync)
        {
            if (closed) return;
            if (events.Count == 256) events.Dequeue();
            events.Enqueue(new(eventSequence++, operation, outcome, elapsed, inputBytes, outputBytes, trace.Value, generation, evaluator, sources.Count, accepted.Count, action ?? operation));
        }
    }
    private SourceParse ParseOwned(byte[] bytes)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); SourceParse parsed;
        try { parsed = SessionSource.Parse(bytes); }
        catch (ContractError error) { Record("language.parse", error.Code, timer.Elapsed.TotalMilliseconds, bytes.Length, null, null, null); throw; }
        Record("language.parse", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, bytes.Length, null, "cfdw-cv/1");
        timer.Restart(); _ = parsed.SourceHash; _ = parsed.SurfaceHash;
        Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, null, null, "cfdw-cv/1");
        return parsed;
    }
    private GeometryAssessment AssessOwned(SourceParse parsed, long? generation = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); var result = Geometry.Assess(parsed);
        Record("geometry.validate", result.Code, timer.Elapsed.TotalMilliseconds, parsed.Source.Length, null, generation, "cfdw-cv/1");
        return result;
    }
    public byte[] Open(byte[] source, string operationId, bool acceptIdInsertion) => Run("open", () => OpenCore(source, operationId, acceptIdInsertion), source.Length);
    public SessionDraft BeginRailEdit(string draftId, string rail, string vertexId) => Run("begin", () => BeginRailEditCore(draftId, rail, vertexId));
    public SessionDraft UpdateDraft(string draftId, long generation, double si) => Run("update", () => UpdateDraftCore(draftId, generation, si), sizeof(double), generation);
    public SessionAssessment Validate(string draftId, long generation, CancellationToken cancellation = default) => Run("validate", () => ValidateCore(draftId, generation, cancellation), generation: generation);
    public SessionPreview Preview(string draftId, long generation, double eta, double x, bool upper, bool port = false) => Run("geometry.preview", () =>
    {
        var assessment = ValidateCore(draftId, generation, default);
        Guard.Require(assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null && assessment.Key is not null, "DSL-NOT-ASSESSED");
        var point = Geometry.PointAt(assessment.Certificate!, eta, x, upper, port);
        lock (sync) Guard.Require(!closed && draft?.Id == draftId && draft.Generation == generation, "DSL-CONFLICT");
        return new SessionPreview(assessment.Key!, point, assessment.Certificate!.PlacementWidthUpper);
    }, 2 * sizeof(double), generation);
    public string Apply(string operationId, SessionAssessment assessment) => Run("apply", () => ApplyCore(operationId, assessment), generation: assessment.Key?.Generation);
    public void Cancel(string draftId) => Run("cancel", () => { CancelCore(draftId); return true; });
    public string Undo(string operationId) => Run("undo", () => UndoCore(operationId));
    public string Redo(string operationId) => Run("redo", () => RedoCore(operationId));
    public SessionView Snapshot() => Run("snapshot", SnapshotCore);
    public RecoveryRow CaptureRecovery() => Run("capture-recovery", CaptureRecoveryCore);
    public void ResumeRecovery() => Run("resume-recovery", () => { ResumeRecoveryCore(); return true; });
    public void DiscardRecovery() => Run("discard-recovery", () => { DiscardRecoveryCore(); return true; });
    public Envelope Envelope() => Run("envelope", EnvelopeCore);
    public byte[] SaveImage() => Run("capture-save", SaveImageCore);
    public void AcknowledgeSaved(byte[] image) => Run("acknowledge-save", () => { AcknowledgeSavedCore(image); return true; }, image.Length);
    public void Reopen(byte[] image) => Run("reopen", () => { ReopenCore(image); return true; }, image.Length);
    private readonly int envelopeCap;
    public AuthoringSession(int envelopeCap = NativeProject.MaxBytes)
    {
        Guard.Require(envelopeCap > 0 && envelopeCap <= NativeProject.MaxBytes, "DOC-SIZE");
        this.envelopeCap = envelopeCap;
    }
    readonly object sync = new();
    readonly Guid authorityId = Guid.NewGuid();
    int validating;
    readonly List<SourceRow> sources = [];
    readonly List<DesignRow> designs = [];
    readonly List<AcceptedRow> accepted = [];
    readonly List<CursorRow> cursors = [];
    readonly Stack<string> redo = [];
    readonly Dictionary<string, (string Payload, string Result)> operations = [];
    readonly HashSet<string> retiredDraftIds = [];
    readonly HashSet<string> capturedSaveHashes = [];
    SessionDraft? draft;
    RecoveryRow? recovery;
    string? current;
    string projectId = Guid.NewGuid().ToString("D");
    string? savedImageHash;
    public static string[] Chunks(byte[] bytes) => Enumerable.Range(0, (bytes.Length + 2303) / 2304).Select(i => Convert.ToBase64String(bytes.Skip(i * 2304).Take(2304).ToArray())).ToArray();
    static byte[] Decode(string[] chunks) => chunks.SelectMany(Convert.FromBase64String).ToArray();
    AcceptedRow Current => accepted.Single(a => a.Id == current);
    byte[] CurrentBytes => Decode(sources.Single(s => s.Id == Current.SourceId).Utf8Base64Chunks);
    SessionBinding Key(SourceParse p, SessionDraft d) => new(p.SourceHash, d.Base, d.Id, d.Generation, "cfdw-cv/1", p.SurfaceHash!, d.Rail, d.VertexId);
    void RequireAdmission(SourceParse p, SessionBinding key)
    {
        var assessment = AssessOwned(p);
        Guard.Require(assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null &&
            assessment.Certificate.SourceHash == key.SourceHash && assessment.Certificate.SurfaceHash == key.SurfaceHash, "DSL-NOT-ASSESSED");
    }
    bool Retry(string op, string payload, out string result)
    {
        NativeProject.Uuid(op);
        if (operations.TryGetValue(op, out var prior)) { Guard.Require(prior.Payload == payload, "DOC-OPERATION-CONFLICT"); result = prior.Result; return true; }
        result = ""; return false;
    }
    private byte[] OpenCore(byte[] source, string operationId, bool acceptIdInsertion)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            var p = ParseOwned(source);
            byte[] candidate = FoilSource.MaterializeIds(p);
            if (!candidate.SequenceEqual(source) && !acceptIdInsertion) return candidate;
            p = ParseOwned(candidate);
            if (Retry(operationId, "open:" + p.SourceHash, out _)) return candidate;
            Guard.Require(current is null, "DOC-SESSION-NOT-EMPTY");
            var key = new SessionBinding(p.SourceHash, "", "", 0, "cfdw-cv/1", p.SurfaceHash!, "", "");
            RequireAdmission(p, key);
            string id = Commit(p, operationId, "open"); operations.Add(operationId, ("open:" + p.SourceHash, id)); return candidate;
        }
    }
    string Commit(SourceParse p, string op, string reason)
    {
        NativeProject.Uuid(op);
        string? parent = current; string? priorDesign = current is null ? null : Current.DesignId;
        string design = priorDesign is not null && designs.Single(d => d.Id == priorDesign).SurfaceHash! == p.SurfaceHash! ? priorDesign : Guid.NewGuid().ToString("D");
        var nextDesigns = designs.ToList(); var nextSources = sources.ToList();
        if (!nextDesigns.Any(d => d.Id == design)) nextDesigns.Add(new(design, priorDesign, p.SurfaceHash!, "cfdw-cv/1"));
        if (!nextSources.Any(s => s.Id == p.SourceHash)) nextSources.Add(new(p.SourceHash, Chunks(p.Source)));
        string id = Guid.NewGuid().ToString("D");
        var row = new AcceptedRow(id, parent, p.SourceHash, design, op, draft is null ? null : new(draft.Id, draft.Generation, draft.Rail, draft.VertexId));
        var cursor = new CursorRow(cursors.Count, id, reason, op);
        var prospective = new Envelope("cfdw-project-1", projectId, nextSources.ToArray(), nextDesigns.ToArray(), [.. accepted, row], [.. cursors, cursor], null);
        NativeProject.Preflight(prospective, envelopeCap);
        designs.Clear(); designs.AddRange(nextDesigns); sources.Clear(); sources.AddRange(nextSources);
        accepted.Add(row); current = id; cursors.Add(cursor); redo.Clear(); return id;
    }
    private SessionDraft BeginRailEditCore(string draftId, string rail, string vertexId)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Uuid(draftId); Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
            var p = ParseOwned(CurrentBytes); Guard.Require(rail is "leading" or "trailing" && p.Definition!.Curves[rail].Ids.Contains(vertexId), "DSL-TARGET");
            retiredDraftIds.Add(draftId);
            draft = new(draftId, current!, 0, rail, vertexId, CurrentBytes); return Copy(draft);
        }
    }
    static SessionDraft Copy(SessionDraft d) => d with { Bytes = d.Bytes.ToArray() };
    private SessionDraft UpdateDraftCore(string draftId, long expectedGeneration, double si)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == expectedGeneration && expectedGeneration < 9007199254740991, "DSL-CONFLICT");
            draft = draft! with { Generation = expectedGeneration + 1, Bytes = FoilSource.PatchRail(ParseOwned(draft.Bytes), draft.Rail, draft.VertexId, si) }; return Copy(draft);
        }
    }
    private SessionAssessment ValidateCore(string draftId, long generation, CancellationToken cancellation = default)
    {
        SessionDraft capture;
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == generation, "DSL-CONFLICT");
            Guard.Require(Interlocked.CompareExchange(ref validating, 1, 0) == 0, "DSL-VALIDATION-BUSY");
            capture = Copy(draft!);
        }
        try
        {
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null);
            var parsed = ParseOwned(capture.Bytes); var key = Key(parsed, capture);
            var result = AssessOwned(parsed, generation);
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", key, null);
            return new(authorityId, result.Status, result.Code, key, result.Certificate);
        }
        catch (ContractError error)
        { return new(authorityId, error.Code is "DSL-LIMIT" or "DSL-UNSUPPORTED" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid, error.Code, null, null); }
        finally { Interlocked.Exchange(ref validating, 0); }
    }
    private string ApplyCore(string operationId, SessionAssessment assessment)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(assessment.Key is not null && assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null, "DSL-NOT-ASSESSED");
            string payload = "apply:" + JsonSerializer.Serialize(assessment.Key);
            if (Retry(operationId, payload, out string prior)) return prior;
            Guard.Require(draft is not null && current == draft.Base, "DSL-CONFLICT");
            var p = ParseOwned(draft!.Bytes); var key = Key(p, draft);
            Guard.Require(assessment.Owner == authorityId && assessment.Certificate!.SourceHash == key.SourceHash &&
                assessment.Certificate.SurfaceHash == key.SurfaceHash && assessment.Key == key, "DSL-CONFLICT");
            string id = Commit(p, operationId, "apply"); operations.Add(operationId, (payload, id)); draft = null; recovery = null; return id;
        }
    }
    private void CancelCore(string draftId) { lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft?.Id == draftId, "DSL-CONFLICT"); draft = null; recovery = null; } }
    private string UndoCore(string operationId) => Move(operationId, false);
    private string RedoCore(string operationId) => Move(operationId, true);
    string Move(string op, bool forward)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            string payload = forward ? "redo" : "undo";
            if (Retry(op, payload, out string prior)) return prior;
            Guard.Require(draft is null && current is not null, "DSL-DRAFT-OWNED");
            string? target = forward ? redo.Count > 0 ? redo.Peek() : null : Current.Parent;
            if (target is null) { operations.Add(op, (payload, current!)); return current!; }
            var targetRow = accepted.Single(a => a.Id == target);
            var targetParsed = ParseOwned(Decode(sources.Single(s => s.Id == targetRow.SourceId).Utf8Base64Chunks));
            RequireAdmission(targetParsed, new(targetParsed.SourceHash, "", "", 0, "cfdw-cv/1", targetParsed.SurfaceHash!, "", ""));
            var cursor = new CursorRow(cursors.Count, target, payload, op);
            NativeProject.Preflight(EnvelopeCore() with { Cursors = [.. cursors, cursor] }, envelopeCap);
            if (forward) redo.Pop(); else redo.Push(current!);
            current = target; cursors.Add(cursor); operations.Add(op, (payload, target)); return target;
        }
    }
    private SessionView SnapshotCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(current is not null, "DOC-EMPTY");
            return new(current!, Current.SourceId, designs.Single(d => d.Id == Current.DesignId).SurfaceHash!, CurrentBytes, draft is null ? null : Copy(draft), CopyRecovery(recovery), draft is not null || savedImageHash != Identity.Sha256(NativeProject.Encode(EnvelopeCore())));
        }
    }
    private RecoveryRow CaptureRecoveryCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null, "DOC-NO-RECOVERY");
            var next = new RecoveryRow(draft!.Id, draft.Base, draft.Generation, draft.Rail, draft.VertexId, Chunks(draft.Bytes));
            NativeProject.Preflight(EnvelopeCore() with { Recovery = next }, envelopeCap);
            recovery = next; return CopyRecovery(recovery)!;
        }
    }
    private void ResumeRecoveryCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(recovery is not null && draft is null && recovery.BaseAcceptedId == current, "DOC-RECOVERY-BASE"); draft = new(recovery!.DraftId, recovery.BaseAcceptedId, recovery.Generation, recovery.Rail, recovery.VertexId, Decode(recovery.Utf8Base64Chunks)); }
    }
    private void DiscardRecoveryCore() { lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft is null, "DSL-DRAFT-OWNED"); recovery = null; } }
    static RecoveryRow? CopyRecovery(RecoveryRow? r) => r is null ? null : r with { Utf8Base64Chunks = r.Utf8Base64Chunks.ToArray() };
    private Envelope EnvelopeCore()
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            return new("cfdw-project-1", projectId, sources.Select(s => s with { Utf8Base64Chunks = s.Utf8Base64Chunks.ToArray() }).ToArray(), designs.ToArray(), accepted.ToArray(), cursors.ToArray(), CopyRecovery(recovery));
        }
    }
    private byte[] SaveImageCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Preflight(EnvelopeCore(), envelopeCap);
            byte[] image = NativeProject.Encode(EnvelopeCore()); string hash = Identity.Sha256(image);
            Guard.Require(capturedSaveHashes.Contains(hash) || capturedSaveHashes.Count < 256, "DOC-SAVE-PENDING");
            capturedSaveHashes.Add(hash); return image;
        }
    }
    private void AcknowledgeSavedCore(byte[] image)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(image.Length <= NativeProject.MaxBytes, "DOC-SIZE");
            string hash = Identity.Sha256(image.ToArray());
            Guard.Require(capturedSaveHashes.Remove(hash), "DOC-SAVE-CAPTURE");
            savedImageHash = hash;
        }
    }
    private void ReopenCore(byte[] image)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(current is null, "DOC-SESSION-NOT-EMPTY"); var env = NativeProject.Read(image);
            var replay = NativeProject.Replay(env); var active = env.Accepted.Single(a => a.Id == replay.Current);
            var p = ParseOwned(Decode(env.Sources.Single(s => s.Id == active.SourceId).Utf8Base64Chunks));
            var key = new SessionBinding(p.SourceHash, "", "", 0, "cfdw-cv/1", p.SurfaceHash!, "", "");
            RequireAdmission(p, key);
            projectId = env.ProjectId;
            sources.AddRange(env.Sources); designs.AddRange(env.Designs); accepted.AddRange(env.Accepted); cursors.AddRange(env.Cursors); recovery = env.Recovery;
            current = replay.Current;
            foreach (var row in accepted) if (row.Edit is not null) retiredDraftIds.Add(row.Edit.DraftId);
            if (recovery is not null) retiredDraftIds.Add(recovery.DraftId);
            foreach (string id in replay.Redo.Reverse()) redo.Push(id);
            foreach (var c in cursors)
            {
                if (c.Reason is "undo" or "redo") operations[c.OperationId] = (c.Reason, c.Target);
                else
                {
                    var a = env.Accepted.Single(a => a.Id == c.Target);
                    var d = env.Designs.Single(d => d.Id == a.DesignId);
                    string payload = c.Reason == "open" ? "open:" + a.SourceId : "apply:" + JsonSerializer.Serialize(new SessionBinding(a.SourceId, a.Parent!, a.Edit!.DraftId, a.Edit.Generation, d.Evaluator, d.SurfaceHash!, a.Edit.Rail, a.Edit.VertexId));
                    operations[c.OperationId] = (payload, c.Target);
                }
            }
            // Dirty-state identity is independent of original on-disk formatting/conflict token.
            savedImageHash = Identity.Sha256(NativeProject.Encode(EnvelopeCore()));
        }
    }
}

public static class NativeProject
{
    public const int MaxBytes = 8_000_000;
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    public static void Uuid(string id) => Guard.Require(Regex.IsMatch(id, @"\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z") && Guid.TryParseExact(id, "D", out _), "DOC-ID");
    static void Hash(string hash) => Guard.Require(Regex.IsMatch(hash, @"\A[0-9a-f]{64}\z"), "DOC-INTEGRITY");
    public static byte[] Encode(Envelope envelope) => JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
    public static void Preflight(Envelope envelope, int cap)
    {
        byte[] bytes = Encode(envelope);
        Guard.Require(bytes.Length <= cap && FoilSource.Utf8.GetString(bytes).Split('\n').All(l => FoilSource.Utf8.GetByteCount(l.TrimEnd('\r')) <= 4096), "DOC-SIZE");
    }
    static byte[] Decode(string[] chunks, bool allowEmpty = false)
    {
        Guard.Require(allowEmpty || chunks.Length > 0, "DOC-SCHEMA"); var result = new List<byte>();
        foreach (string chunk in chunks)
        {
            Guard.Require(chunk.Length is > 0 and <= 3072 && chunk.Length % 4 == 0, "DOC-SCHEMA");
            byte[] bytes; try { bytes = Convert.FromBase64String(chunk); } catch (FormatException) { throw new ContractError("DOC-SCHEMA"); }
            Guard.Require(Convert.ToBase64String(bytes) == chunk, "DOC-SCHEMA");
            Guard.Require(result.Count + bytes.Length <= 1_048_576, "DOC-SIZE"); result.AddRange(bytes);
        }
        for (int i = 0; i + 1 < chunks.Length; i++) Guard.Require(!chunks[i].Contains('='), "DOC-SCHEMA");
        _ = SessionSource.Text(result.ToArray()); return result.ToArray();
    }
    static void Exact(JsonElement element, params string[] keys)
    {
        Guard.Require(element.ValueKind == JsonValueKind.Object, "DOC-SCHEMA");
        var names = element.EnumerateObject().Select(p => p.Name).ToArray();
        Guard.Require(names.Distinct(StringComparer.Ordinal).Count() == names.Length, "DOC-SCHEMA");
        Guard.Require(keys.All(names.Contains), "DOC-SCHEMA");
        Guard.Require(names.All(keys.Contains), "DOC-UNSUPPORTED-FIELD");
    }
    public static Envelope Read(byte[] bytes)
    {
        Guard.Require(bytes.Length <= MaxBytes, "DOC-SIZE");
        bytes = bytes.ToArray();
        string text; try { text = FoilSource.Utf8.GetString(bytes); } catch (DecoderFallbackException) { throw new ContractError("DOC-SCHEMA"); }
        Guard.Require(!text.StartsWith('\uFEFF') && text.Split('\n').All(l => FoilSource.Utf8.GetByteCount(l.TrimEnd('\r')) <= 4096), "DOC-SCHEMA");
        try
        {
            using var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 }); var root = doc.RootElement;
            Exact(root, "format", "projectId", "sources", "designs", "accepted", "cursors", "recovery");
            Guard.Require(root.GetProperty("format").GetString() == "cfdw-project-1", "DOC-VERSION");
            foreach (var s in root.GetProperty("sources").EnumerateArray()) Exact(s, "id", "utf8Base64Chunks");
            foreach (var d in root.GetProperty("designs").EnumerateArray()) Exact(d, "id", "parent", "surfaceHash", "evaluator");
            foreach (var a in root.GetProperty("accepted").EnumerateArray())
            {
                Exact(a, "id", "parent", "sourceId", "designId", "operationId", "edit");
                if (a.GetProperty("edit").ValueKind != JsonValueKind.Null) Exact(a.GetProperty("edit"), "draftId", "generation", "rail", "vertexId");
            }
            foreach (var c in root.GetProperty("cursors").EnumerateArray()) Exact(c, "sequence", "target", "reason", "operationId");
            if (root.GetProperty("recovery").ValueKind != JsonValueKind.Null) Exact(root.GetProperty("recovery"), "draftId", "baseAcceptedId", "generation", "rail", "vertexId", "utf8Base64Chunks");
            var env = JsonSerializer.Deserialize<Envelope>(bytes, Options)!; Check(env); return env;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or ArgumentException or NullReferenceException or KeyNotFoundException) { throw new ContractError("DOC-SCHEMA"); }
    }
    static void Check(Envelope e)
    {
        Uuid(e.ProjectId);
        Guard.Require(e.Sources.Length > 0 && e.Designs.Length > 0 && e.Accepted.Length > 0 && e.Cursors.Length > 0, "DOC-REFERENCE");
        Guard.Require(e.Sources.Select(x => x.Id).Distinct().Count() == e.Sources.Length && e.Designs.Select(x => x.Id).Distinct().Count() == e.Designs.Length && e.Accepted.Select(x => x.Id).Distinct().Count() == e.Accepted.Length, "DOC-REFERENCE");
        var parsed = new Dictionary<string, SourceParse>();
        foreach (var s in e.Sources) { Hash(s.Id); byte[] bytes = Decode(s.Utf8Base64Chunks); Guard.Require(Identity.Sha256(bytes) == s.Id, "DOC-INTEGRITY"); var p = SessionSource.Parse(bytes); Guard.Require(p.Definition!.Curves.Values.All(c => !c.MissingIds), "DOC-INTEGRITY"); parsed.Add(s.Id, p); }
        var designs = new Dictionary<string, DesignRow>();
        foreach (var d in e.Designs) { Uuid(d.Id); Hash(d.SurfaceHash!); Guard.Require(d.Evaluator == "cfdw-cv/1" && (designs.Count == 0 ? d.Parent is null : d.Parent is not null && designs.ContainsKey(d.Parent)), "DOC-REFERENCE"); designs.Add(d.Id, d); }
        var accepted = new Dictionary<string, AcceptedRow>();
        foreach (var a in e.Accepted)
        {
            Uuid(a.Id); Uuid(a.OperationId); Guard.Require(accepted.Count == 0 ? a.Parent is null : a.Parent is not null && accepted.ContainsKey(a.Parent), "DOC-REFERENCE");
            Guard.Require(parsed.ContainsKey(a.SourceId) && designs.ContainsKey(a.DesignId), "DOC-REFERENCE");
            Guard.Require(parsed[a.SourceId].SurfaceHash! == designs[a.DesignId].SurfaceHash!, "DOC-INTEGRITY");
            if (a.Parent is not null)
            {
                Guard.Require(a.Edit is not null, "DOC-REFERENCE"); Uuid(a.Edit!.DraftId);
                Guard.Require(a.Edit.Generation is >= 0 and <= 9007199254740991 && a.Edit.Rail is "leading" or "trailing" && parsed[a.SourceId].Definition!.Curves[a.Edit.Rail].Ids.Contains(a.Edit.VertexId), "DOC-REFERENCE");
                var parent = accepted[a.Parent]; bool same = parsed[a.SourceId].SurfaceHash! == parsed[parent.SourceId].SurfaceHash!;
                Guard.Require(parsed[parent.SourceId].Definition!.Curves[a.Edit.Rail].Ids.Contains(a.Edit.VertexId), "DOC-REFERENCE");
                Guard.Require(same ? a.DesignId == parent.DesignId : designs[a.DesignId].Parent == parent.DesignId, "DOC-REFERENCE");
            }
            else Guard.Require(a.Edit is null, "DOC-REFERENCE");
            accepted.Add(a.Id, a);
        }
        Guard.Require(e.Sources.All(s => e.Accepted.Any(a => a.SourceId == s.Id)) && e.Designs.All(d => e.Accepted.Any(a => a.DesignId == d.Id)), "DOC-REFERENCE");
        _ = Replay(e);
        if (e.Recovery is not null)
        {
            var r = e.Recovery; Uuid(r.DraftId); Guard.Require(accepted.ContainsKey(r.BaseAcceptedId) && r.Generation is >= 0 and <= 9007199254740991 && r.Rail is "leading" or "trailing", "DOC-REFERENCE");
            Guard.Require(r.VertexId.Length > 0 && r.VertexId.EnumerateRunes().Count() <= 4096 && parsed[accepted[r.BaseAcceptedId].SourceId].Definition!.Curves[r.Rail].Ids.Contains(r.VertexId), "DOC-REFERENCE"); _ = Decode(r.Utf8Base64Chunks, allowEmpty: true);
        }
    }
    public static (string Current, string[] Redo) Replay(Envelope e)
    {
        string? current = null; var redo = new Stack<string>(); var seen = new HashSet<string>(); int nextAccepted = 0;
        for (int i = 0; i < e.Cursors.Length; i++)
        {
            var c = e.Cursors[i]; Uuid(c.OperationId); Guard.Require(c.Sequence == i && seen.Add(c.OperationId), "DOC-REFERENCE");
            var target = e.Accepted.SingleOrDefault(a => a.Id == c.Target); Guard.Require(target is not null, "DOC-REFERENCE");
            switch (c.Reason)
            {
                case "open": Guard.Require(i == 0 && target!.Parent is null, "DOC-REFERENCE"); goto case "apply";
                case "apply":
                    Guard.Require(i > 0 || c.Reason == "open", "DOC-REFERENCE");
                    Guard.Require(nextAccepted < e.Accepted.Length && e.Accepted[nextAccepted] == target && target!.Parent == current && target.OperationId == c.OperationId, "DOC-REFERENCE");
                    nextAccepted++; redo.Clear(); break;
                case "undo":
                    Guard.Require(current is not null && e.Accepted.Single(a => a.Id == current).Parent == c.Target, "DOC-REFERENCE"); redo.Push(current!); break;
                case "redo":
                    Guard.Require(redo.Count > 0 && redo.Peek() == c.Target && target!.Parent == current, "DOC-REFERENCE"); redo.Pop(); break;
                default: throw new ContractError("DOC-REFERENCE");
            }
            current = c.Target;
        }
        Guard.Require(nextAccepted == e.Accepted.Length && current is not null, "DOC-REFERENCE");
        Guard.Require(e.Accepted.Select(a => a.OperationId).Distinct().Count() == e.Accepted.Length, "DOC-REFERENCE");
        return (current!, redo.ToArray());
    }
}

internal static class SessionSource
{
    internal static SourceParse Parse(byte[] bytes)
    {
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed) throw new ContractError(parsed.Diagnostics[0].Code);
        return parsed;
    }
    internal static string Text(byte[] bytes)
    {
        Guard.Require(bytes.Length <= 1_048_576, "DSL-LIMIT");
        try { return FoilSource.Utf8.GetString(bytes); }
        catch (DecoderFallbackException) { throw new ContractError("DSL-LEX"); }
    }
}
