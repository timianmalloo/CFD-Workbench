using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CfdWorkbench.Persistence")]

namespace CfdWorkbench.Core;

public sealed record SourceRow(string Id, string[] Utf8Base64Chunks);
public sealed record DesignRow(string Id, string? Parent, string SurfaceHash, string Evaluator);
public sealed record EditReceipt(string DraftId, long Generation, string Rail, string VertexId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] ThicknessIntent Intent = ThicknessIntent.KeepCurrent,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Rule = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Curve = null);
public sealed record AcceptedRow(string Id, string? Parent, string SourceId, string DesignId, string OperationId, EditReceipt? Edit);
public sealed record CursorRow(long Sequence, string Target, string Reason, string OperationId);
public sealed record RecoveryRow(string DraftId, string BaseAcceptedId, long Generation, string Rail, string VertexId, string[] Utf8Base64Chunks, string? Profile = null, int Assignment = -1, ThicknessIntent Intent = ThicknessIntent.KeepCurrent);
public sealed record Envelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted, CursorRow[] Cursors, RecoveryRow? Recovery);
public sealed record SessionDraft(string Id, string Base, long Generation, string Rail, string VertexId, byte[] Bytes, string? Profile = null, int Assignment = -1, ThicknessIntent Intent = ThicknessIntent.KeepCurrent, string? Rule = null, string? Curve = null);
public sealed record SessionBinding(string SourceHash, string Base, string DraftId, long Generation, string Evaluator, string SurfaceHash, string Rail, string VertexId);

public sealed record SessionView(string AcceptedId, string SourceHash, string SurfaceHash, byte[] Source, SessionDraft? Draft, RecoveryRow? Recovery, bool Dirty);
public sealed record SessionEvent(long Sequence, string Operation, string Outcome, double DurationMilliseconds, int? InputBytes,
    int? OutputBytes, string? TraceId, long? Generation, string? Evaluator, int RetainedSources, int AcceptedFacts, string Action,
    bool? PublicationKnown = null, bool? DurabilityConfirmed = null, string? EditKind = null,
    double? FitMicrometres = null, double? DeviationMicrometres = null, double? ShiftMicrometres = null, bool? FitAboveLimit = null,
    int? Frames = null);
public sealed record DimensionCommand(string Name, string Text);
public sealed record GestureFrame(SessionDraft Draft, double SpanMeters, double AftMeters, IReadOnlyList<string> MovedIds, bool Clamped);
public abstract record PointCommand(string Curve, string VertexId)
{
    public sealed record MakeAnchor(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record MakeControl(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record SetTangent(string Curve, string VertexId, TangentKind Kind, string? KeepHandleId) : PointCommand(Curve, VertexId);
}
public sealed record PointOutcome(string AcceptedId, double MaxDeviationMeters, int PointsBefore, int PointsAfter);
public sealed record SessionPreview(SessionBinding Binding, PlacedPointEnclosure Point, double UniformWidthUpper);

public sealed class SessionAssessment
{
    internal SessionAssessment(Guid owner, GeometryStatus status, string code, SessionBinding? key, GeometryCertificate? certificate,
        AuthoredBinding? sourceBinding = null, IEnumerable<Diagnostic>? diagnostics = null, ConstructionReport? construction = null,
        ThicknessProposal? thickness = null, ImportReport? importReport = null)
    {
        Owner = owner; Status = status; Code = code; Key = key; Certificate = certificate; SourceBinding = sourceBinding;
        Diagnostics = Array.AsReadOnly((diagnostics ?? []).ToArray()); Construction = construction; Thickness = thickness; ImportReport = importReport;
    }
    internal Guid Owner { get; }
    public GeometryStatus Status { get; }
    public string Code { get; }
    public SessionBinding? Key { get; }
    public GeometryCertificate? Certificate { get; }
    public AuthoredBinding? SourceBinding { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public ConstructionReport? Construction { get; }
    public ImportReport? ImportReport { get; }
    public ThicknessProposal? Thickness { get; }
}

public sealed class AuthoringSession : IDisposable
{
    public AcceptedInspection InspectAccepted() => Run("inspect-accepted", () =>
    {
        byte[] bytes; AcceptedRow revision; DesignRow design;
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(current is not null, "DOC-EMPTY");
            bytes = CurrentBytes; revision = Current; design = designs.Single(item => item.Id == revision.DesignId);
        }
        var parsed = ParseOwned(bytes);
        var binding = new AuthoredBinding(parsed.SourceHash, "Accepted", revision.Id, design.Id, parsed.SurfaceHash, design.Evaluator, null, null, null);
        return new AcceptedInspection(parsed.Authored().Rebind(binding), AssessOwned(parsed));
    });
    public AuthoredProjection InspectDraft() => Run("inspect-draft", () =>
    {
        SessionDraft capture;
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft is not null, "DSL-DRAFT-OWNED"); capture = Copy(draft!); }
        var parsed = FoilSource.Parse(capture.Bytes); return parsed.Authored().Rebind(DraftBinding(capture, parsed));
    });
    private static AuthoredBinding DraftBinding(SessionDraft capture, SourceParse? parsed = null) =>
        new(parsed?.SourceHash ?? Identity.Sha256(capture.Bytes), "Draft", null, null, parsed?.SurfaceHash,
            parsed?.IsParsed == true ? "cfdw-cv/2" : null, capture.Base, capture.Id, capture.Generation);
    private readonly Queue<SessionEvent> events = new();
    private readonly AsyncLocal<string?> trace = new();
    private long eventSequence;
    private bool closed;
    internal void RecordPersistence(string action, string outcome, double milliseconds, int? inputBytes, int? outputBytes,
        string traceId, bool? publicationKnown, bool? durabilityConfirmed)
    {
        lock (sync)
        {
            if (closed) return;
            if (events.Count == 256) events.Dequeue();
            events.Enqueue(new(eventSequence++, action == "store.read" ? "document.reopen" : "document.save", outcome,
                milliseconds, inputBytes, outputBytes, traceId, null, null, sources.Count, accepted.Count, action, publicationKnown, durabilityConfirmed));
        }
    }
    public IReadOnlyList<SessionEvent> ReadLocalEvents()
    { lock (sync) return Array.AsReadOnly(events.ToArray()); }
    public void Dispose()
    {
        lock (sync)
        {
            closed = true; events.Clear(); capturedSaveHashes.Clear(); retiredDraftIds.Clear(); pendingFairAssessment.Clear();
            sources.Clear(); designs.Clear(); accepted.Clear(); cursors.Clear(); redo.Clear(); operations.Clear();
            draft = null; recovery = null; current = null; activeImportReport = null; importBasisFallback = null;
        }
    }
    private T Run<T>(string operation, Func<T> action, int? inputBytes = null, long? generation = null, string? editKind = null)
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
            Record(name, outcome, timer.Elapsed.TotalMilliseconds, inputBytes, null, generation, null, operation, editKind);
            trace.Value = priorTrace;
        }
    }
    private void Record(string operation, string outcome, double elapsed, int? inputBytes, int? outputBytes, long? generation, string? evaluator, string? action = null, string? editKind = null, int? frames = null)
    {
        lock (sync)
        {
            double? fit = pendingFitUm;
            double? deviation = pendingDeviationUm;
            double? shift = pendingShiftUm;
            bool? above = pendingFitAboveLimit;
            pendingFitUm = pendingDeviationUm = pendingShiftUm = null;
            pendingFitAboveLimit = null;
            if (closed) return;
            if (events.Count == 256) events.Dequeue();
            events.Enqueue(new(eventSequence++, operation, outcome, elapsed, inputBytes, outputBytes, trace.Value, generation, evaluator, sources.Count, accepted.Count, action ?? operation, null, null, editKind, fit, deviation, shift, above, frames));
        }
    }
    private SourceParse ParseOwned(byte[] bytes)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); SourceParse parsed;
        try { parsed = SessionSource.Parse(bytes); }
        catch (ContractError error) { Record("language.parse", error.Code, timer.Elapsed.TotalMilliseconds, bytes.Length, null, null, null); throw; }
        Record("language.parse", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, bytes.Length, null, "cfdw-cv/2");
        timer.Restart(); _ = parsed.SourceHash; _ = parsed.SurfaceHash;
        Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, null, null, "cfdw-cv/2");
        return parsed;
    }
    private GeometryAssessment AssessOwned(SourceParse parsed, long? generation = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); var result = Geometry.Assess(parsed, proofBudget);
        Record("geometry.validate", result.Code, timer.Elapsed.TotalMilliseconds, parsed.Source.Length, null, generation, "cfdw-cv/2");
        return result;
    }
    public byte[] Open(byte[] source, string operationId, bool acceptIdInsertion) => Run("open", () => OpenCore(source, operationId, acceptIdInsertion), source.Length);
    public SessionDraft BeginRailEdit(string draftId, string rail, string vertexId) => Run("begin", () => BeginRailEditCore(draftId, rail, vertexId));
    public SessionDraft UpdateDraft(string draftId, long generation, double si) => Run("update", () => UpdateDraftCore(draftId, generation, si), sizeof(double), generation);
    public SessionDraft BeginPointGesture(string draftId, string curve, string vertexId) =>
        Run("begin", () => BeginPointGestureCore(draftId, curve, vertexId));
    public GestureFrame UpdatePointGesture(string draftId, long generation, double spanMeters, double aftMeters) =>
        Run("update", () => UpdatePointGestureCore(draftId, generation, spanMeters, aftMeters), 2 * sizeof(double), generation);
    public PointOutcome ApplyPointCommand(string operationId, PointCommand command) =>
        Run("apply", () => ApplyPointCommandCore(operationId, command), editKind: command is PointCommand.SetTangent ? "tangent-kind" : "point-type");
    public ProfileView ProfileAt(int assignmentIndex) => Run("profile", () => ProfileAtCore(assignmentIndex));
    public ScopeImpact DescribeScope(string profile, int assignmentIndex, SectionScope scope) => Run("scope", () => DescribeScopeCore(profile, assignmentIndex, scope));
    public SessionDraft BeginProfileEdit(string draftId, int assignmentIndex, SectionScope scope, string side, string vertexId,
        ThicknessIntent thickness = ThicknessIntent.KeepCurrent) =>
        Run("begin", () => BeginProfileEditCore(draftId, assignmentIndex, scope, side, vertexId, thickness));
    public SessionDraft BeginProfileImport(string draftId, int assignmentIndex, byte[] dat) =>
        Run("begin", () => BeginProfileImportCore(draftId, assignmentIndex, dat));
    public SessionDraft UpdateProfileDraft(string draftId, long generation, double x, double y) =>
        Run("update", () => UpdateProfileDraftCore(draftId, generation, x, y), 2 * sizeof(double), generation);
    public SessionDraft BeginProfileInsert(string draftId, int assignmentIndex, SectionScope scope, double x) =>
        Run("begin", () => BeginProfileInsertCore(draftId, assignmentIndex, scope, x));
    public SessionDraft BeginProfileDelete(string draftId, int assignmentIndex, SectionScope scope, int vertexIndex) =>
        Run("begin", () => BeginProfileDeleteCore(draftId, assignmentIndex, scope, vertexIndex));
    public SessionDraft BeginProfileFair(string draftId, int assignmentIndex, SectionScope scope, double tolerance, PreserveEnds ends) =>
        Run("begin", () => BeginProfileFairCore(draftId, assignmentIndex, scope, tolerance, ends));
    public SessionDraft BeginProfileRebuild(string draftId, int assignmentIndex, SectionScope scope, int vertexCount, double tolerance, PreserveEnds ends) =>
        Run("begin", () => BeginProfileRebuildCore(draftId, assignmentIndex, scope, vertexCount, tolerance, ends));
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
    public string ApplyDimension(string operationId, DimensionCommand command) =>
        Run("apply", () => ApplyDimensionCore(operationId, command), editKind: "dimension");
    public DimensionOutcome ApplyChord(string operationId, DimensionCommand command) =>
        Run("apply", () => ApplyChordCore(operationId, command), editKind: "dimension");
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
    private readonly TimeSpan? proofBudget;
    public AuthoringSession(int envelopeCap = NativeProject.MaxBytes)
    {
        Guard.Require(envelopeCap > 0 && envelopeCap <= NativeProject.MaxBytes, "DOC-SIZE");
        this.envelopeCap = envelopeCap;
    }
    // Test seam only: forces every geometry proof in this session to run under the given
    // budget, so a budget refusal (GEOMETRY-BUDGET) can be reproduced deterministically
    // instead of depending on real elapsed time. No public constructor exposes this.
    internal AuthoringSession(TimeSpan proofBudget, int envelopeCap = NativeProject.MaxBytes) : this(envelopeCap)
    {
        this.proofBudget = proofBudget;
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
    readonly Dictionary<string, (double MaxDeviation, double Tolerance, int VertexCount, bool WithinTolerance, bool PiecesIncreased)> pendingFairAssessment = [];
    SessionDraft? draft;
    double? pendingFitUm, pendingDeviationUm, pendingShiftUm;
    bool? pendingFitAboveLimit;
    RecoveryRow? recovery;
    ImportReport? activeImportReport;
    string? importBasisFallback;
    string? current;
    string projectId = Guid.NewGuid().ToString("D");
    string? savedImageHash;
    public static string[] Chunks(byte[] bytes) => Enumerable.Range(0, (bytes.Length + 2303) / 2304).Select(i => Convert.ToBase64String(bytes.Skip(i * 2304).Take(2304).ToArray())).ToArray();
    static byte[] Decode(string[] chunks) => chunks.SelectMany(Convert.FromBase64String).ToArray();
    AcceptedRow Current => accepted.Single(a => a.Id == current);
    byte[] CurrentBytes => Decode(sources.Single(s => s.Id == Current.SourceId).Utf8Base64Chunks);
    SessionBinding Key(SourceParse p, SessionDraft d) => new(p.SourceHash, d.Base, d.Id, d.Generation, "cfdw-cv/2", p.SurfaceHash!, d.Rail, d.VertexId);
    // Admits a document into the session. `toleratesBudget` is for re-certifying a revision
    // that is already stored (reopen, undo/redo): a proof that merely ran out of its
    // cooperative time budget must not refuse the whole session — the revision is simply
    // carried in as NotAssessed / GEOMETRY-BUDGET (readable via InspectAccepted/Snapshot),
    // never thrown and never labelled Certified. Every other refusal (integrity, reference,
    // format) still refuses exactly as before. First admission of a brand-new source (Open)
    // does not tolerate this, since that revision is not yet a stored one to fall back to.
    void RequireAdmission(SourceParse p, SessionBinding key, bool toleratesBudget = false)
    {
        var assessment = AssessOwned(p);
        if (toleratesBudget && assessment.Status == GeometryStatus.NotAssessed && assessment.Code == "GEOMETRY-BUDGET") return;
        Guard.Require(assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null &&
            assessment.Certificate.SourceHash == key.SourceHash && assessment.Certificate.SurfaceHash == key.SurfaceHash, "DSL-NOT-ASSESSED");
    }
    bool Retry(string op, string payload, out string result)
    {
        NativeProject.Uuid(op);
        if (operations.TryGetValue(op, out var prior)) { Guard.Require(prior.Payload == payload, "DOC-OPERATION-CONFLICT"); result = prior.Result; return true; }
        result = ""; return false;
    }

    // Persisted members only. curve is the B1b slot; chords pass the empty slot.
    internal static string Fingerprint(EditReceipt receipt, string? parentId, string sourceId, string curve = "") =>
        string.Join('\u001f', "dimension", receipt.Rail, curve, receipt.VertexId, receipt.Rule ?? "", parentId ?? "", sourceId);
    private byte[] OpenCore(byte[] source, string operationId, bool acceptIdInsertion)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            var p = ParseOwned(source);
            byte[] candidate = FoilSource.MaterializeIds(p);
            if (!candidate.SequenceEqual(source) && !acceptIdInsertion) return candidate;
            p = ParseOwned(candidate);
            if (Retry(operationId, "open:" + p.SourceHash, out _)) return candidate;
            Guard.Require(current is null, "DOC-SESSION-NOT-EMPTY");
            var key = new SessionBinding(p.SourceHash, "", "", 0, "cfdw-cv/2", p.SurfaceHash!, "", "");
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
        if (!nextDesigns.Any(d => d.Id == design)) nextDesigns.Add(new(design, priorDesign, p.SurfaceHash!, "cfdw-cv/2"));
        if (!nextSources.Any(s => s.Id == p.SourceHash)) nextSources.Add(new(p.SourceHash, Chunks(p.Source)));
        string id = Guid.NewGuid().ToString("D");
        var row = new AcceptedRow(id, parent, p.SourceHash, design, op, draft is null ? null : new(draft.Id, draft.Generation, draft.Rail, draft.VertexId, draft.Intent, draft.Rule, draft.Curve));
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
            activeImportReport = null; importBasisFallback = null;
            draft = new(draftId, current!, 0, rail, vertexId, CurrentBytes); return Copy(draft);
        }
    }
    private string? gestureDraftId;
    private int gestureFrames;
    private long gestureStarted;
    private SessionDraft BeginPointGestureCore(string draftId, string curve, string vertexId)
    {
        lock (sync)
        {
            NativeProject.Uuid(draftId);
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
            Guard.Require(PointModel.EditableCurves.Contains(curve), "DSL-TARGET");
            var parsed = ParseOwned(CurrentBytes);
            var rail = parsed.Definition!.Curves[curve];
            int index = Array.IndexOf(rail.Ids, vertexId);
            Guard.Require(index >= 0, "DSL-TARGET");
            var point = (curve == "leading" ? Planform.View(CurrentBytes, "Accepted", 0).Leading : Planform.View(CurrentBytes, "Accepted", 0).Trailing).Points[index];
            Guard.Require(point.Freedom != PointFreedom.Fixed, "DSL-LOCK");
            RequireAdmission(parsed, new(parsed.SourceHash, current!, draftId, 0, "cfdw-cv/2", parsed.SurfaceHash!, curve, vertexId), toleratesBudget: false);
            retiredDraftIds.Add(draftId);
            draft = new(draftId, current!, 0, curve, vertexId, CurrentBytes);
            gestureDraftId = draftId; gestureFrames = 0; gestureStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            return Copy(draft);
        }
    }

    private GestureFrame UpdatePointGestureCore(string draftId, long generation, double spanMeters, double aftMeters)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft?.Id == draftId && draft.Generation == generation && gestureDraftId == draftId, "DSL-CONFLICT");
            var initial = Planform.View(BaseBytes(draft!.Base), "Accepted", 0);
            var rail = draft.Rail == "leading" ? initial.Leading : initial.Trailing;
            int grabbed = rail.Points.ToList().FindIndex(point => point.Id == draft.VertexId);
            Guard.Require(grabbed >= 0, "DSL-TARGET");
            var selected = rail.Points[grabbed];
            GestureFrame LastFrame()
            {
                var currentView = Planform.View(draft.Bytes, "Draft", draft.Generation);
                var currentPoint = (draft.Rail == "leading" ? currentView.Leading : currentView.Trailing).Points[grabbed];
                return new(Copy(draft), currentPoint.SpanMeters, currentPoint.AftMeters, [], true);
            }
            if (!double.IsFinite(spanMeters) || !double.IsFinite(aftMeters))
                return LastFrame();
            if (selected.Freedom is PointFreedom.Fixed or PointFreedom.AftOnly) spanMeters = selected.SpanMeters;
            if (selected.Freedom == PointFreedom.SpanOnly) aftMeters = selected.AftMeters;
            double halfSpan = initial.HalfSpanMeters;
            double rawEta = (spanMeters - selected.SpanMeters) / halfSpan;
            double rawAft = (aftMeters - selected.AftMeters) * 1e6;
            if (!double.IsFinite(rawEta) || !double.IsFinite(rawAft)) return LastFrame();
            double deltaEta = Math.Round(rawEta, 7, MidpointRounding.ToEven);
            double deltaAft = Math.Round(rawAft, 0, MidpointRounding.ToEven) / 1e6;
            var moved = new Dictionary<int, (double Eta, double Aft)>();
            void Add(int index, double eta, double aft) => moved[index] = (eta, aft);
            Add(grabbed, selected.Eta + deltaEta, selected.AftMeters + deltaAft);
            if (selected.Role == PointRole.Anchor)
            {
                foreach (int index in new[] { grabbed - 1, grabbed + 1 })
                    Add(index, rail.Points[index].Eta + deltaEta, rail.Points[index].AftMeters + deltaAft);
            }
            else if (selected.Role == PointRole.RootEnd && draft.Rail == "trailing" && selected.Locks.Contains("root_mirror"))
                Add(1, rail.Points[1].Eta, rail.Points[1].AftMeters + deltaAft);
            else if (selected.Role == PointRole.AnchorHandle)
            {
                int anchor = rail.Points.ToList().FindIndex(point => point.Id == selected.AnchorId);
                int opposite = 2 * anchor - grabbed;
                var a = rail.Points[anchor]; var h = moved[grabbed]; var old = rail.Points[opposite];
                if (a.Kind == TangentKind.Symmetric)
                    Add(opposite, 2 * a.Eta - h.Eta, 2 * a.AftMeters - h.Aft);
                else if (a.Kind == TangentKind.Smooth)
                {
                    double ratio = (old.Eta - a.Eta) / (h.Eta - a.Eta);
                    Add(opposite, old.Eta, a.AftMeters + ratio * (h.Aft - a.AftMeters));
                }
            }
            bool clamped = false;
            // An anchor and its handles translate together. A handle pair instead rotates
            // around a fixed anchor, so clamp the grabbed handle and derive its mate again.
            if (selected.Role == PointRole.AnchorHandle)
            {
                (double Min, double Max) Bounds(int i)
                {
                    double leftGap = Math.Min(0.001 / halfSpan, rail.Points[i].Eta - rail.Points[i - 1].Eta);
                    double rightGap = Math.Min(0.001 / halfSpan, rail.Points[i + 1].Eta - rail.Points[i].Eta);
                    return (rail.Points[i - 1].Eta + leftGap, rail.Points[i + 1].Eta - rightGap);
                }
                var bounds = Bounds(grabbed);
                int anchor = rail.Points.ToList().FindIndex(point => point.Id == selected.AnchorId);
                int opposite = 2 * anchor - grabbed;
                var a = rail.Points[anchor];
                if (a.Kind == TangentKind.Symmetric)
                {
                    var other = Bounds(opposite);
                    bounds = (Math.Max(bounds.Min, 2 * a.Eta - other.Max),
                        Math.Min(bounds.Max, 2 * a.Eta - other.Min));
                }
                if (bounds.Min > bounds.Max)
                    return new(Copy(draft), selected.SpanMeters, selected.AftMeters, [], true);
                double eta = Math.Clamp(moved[grabbed].Eta, bounds.Min, bounds.Max);
                clamped = eta != moved[grabbed].Eta;
                moved[grabbed] = (eta, moved[grabbed].Aft);
                if (a.Kind == TangentKind.Symmetric)
                    moved[opposite] = (2 * a.Eta - eta, 2 * a.AftMeters - moved[grabbed].Aft);
                else if (a.Kind == TangentKind.Smooth)
                {
                    double slope = (moved[grabbed].Aft - a.AftMeters) / (eta - a.Eta);
                    moved[opposite] = (rail.Points[opposite].Eta,
                        a.AftMeters + slope * (rail.Points[opposite].Eta - a.Eta));
                }
            }
            else
            {
                double shiftMin = double.NegativeInfinity, shiftMax = double.PositiveInfinity;
                foreach (var (index, target) in moved)
                {
                    if (index > 0 && !moved.ContainsKey(index - 1))
                    {
                        double gap = Math.Min(0.001 / halfSpan, rail.Points[index].Eta - rail.Points[index - 1].Eta);
                        shiftMin = Math.Max(shiftMin, rail.Points[index - 1].Eta + gap - target.Eta);
                    }
                    if (index + 1 < rail.Points.Count && !moved.ContainsKey(index + 1))
                    {
                        double gap = Math.Min(0.001 / halfSpan, rail.Points[index + 1].Eta - rail.Points[index].Eta);
                        shiftMax = Math.Min(shiftMax, rail.Points[index + 1].Eta - gap - target.Eta);
                    }
                }
                if (shiftMin > shiftMax) return new(Copy(draft), selected.SpanMeters, selected.AftMeters, [], true);
                double shift = Math.Clamp(0, shiftMin, shiftMax);
                if (shift != 0) clamped = true;
                foreach (int index in moved.Keys.ToArray()) moved[index] = (moved[index].Eta + shift, moved[index].Aft);
            }
            var baseParsed = ParseOwned(BaseBytes(draft.Base));
            byte[] patched = PatchGesture(baseParsed, draft.Rail, moved);
            draft = draft with { Generation = generation + 1, Bytes = patched };
            gestureFrames++;
            var resolved = moved[grabbed];
            return new(Copy(draft), resolved.Eta * halfSpan, resolved.Aft, moved.Keys.Order().Select(index => rail.Points[index].Id).ToArray(), clamped);
        }
    }

    internal static byte[] PatchGesture(SourceParse parsed, string curveName, IReadOnlyDictionary<int, (double Eta, double Aft)> moved)
    {
        var definition = parsed.Definition!; var curve = definition.Curves[curveName];
        string text = FoilSource.Utf8.GetString(parsed.Source);
        var edits = new List<(int Start, int End, string Value)>();
        foreach (var (index, value) in moved)
        {
            edits.Add((curve.Abscissae[index].Start, curve.Abscissae[index].End, FoilSource.ExactDecimal(value.Eta)));
            edits.Add((curve.Ordinates[index].Start, curve.Ordinates[index].End, FoilSource.ExactDecimal(value.Aft, definition.UnitScale)));
        }
        foreach (var edit in edits.OrderByDescending(edit => edit.Start)) text = text[..edit.Start] + edit.Value + text[edit.End..];
        byte[] result = FoilSource.Utf8.GetBytes(text);
        var check = FoilSource.Parse(result);
        Guard.Require(check.IsParsed && check.Definition is not null, "DSL-PATCH");
        foreach (var (index, value) in moved)
        {
            var point = check.Definition!.Curves[curveName].Points[index];
            Guard.Require(BitConverter.DoubleToInt64Bits(point[0]) == BitConverter.DoubleToInt64Bits(value.Eta) &&
                BitConverter.DoubleToInt64Bits(point[1]) == BitConverter.DoubleToInt64Bits(value.Aft), "DSL-PATCH");
        }
        return result;
    }
    static SessionDraft Copy(SessionDraft d) => d with { Bytes = d.Bytes.ToArray() };
    private ProfileView ProfileAtCore(int assignmentIndex)
    {
        byte[] bytes;
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(current is not null, "DOC-EMPTY");
            bytes = draft is not null && draft.Profile is not null && draft.Assignment == assignmentIndex ? draft.Bytes.ToArray() : CurrentBytes;
        }
        var parsed = ParseOwned(bytes);
        var definition = parsed.Definition ?? throw new ContractError("DSL-PROFILE-TARGET");
        Guard.Require((uint)assignmentIndex < (uint)definition.Assignments.Length, "DSL-PROFILE-TARGET");
        var profile = definition.Profiles[definition.Assignments[assignmentIndex].Profile];
        string identity = parsed.Authored().Assignments[assignmentIndex].ProfileIdentity;
        return new(profile.Name, identity, Vertices(profile.Upper, "upper", profile.Closure), Vertices(profile.Lower, "lower", profile.Closure),
            Sample(profile.Upper), Sample(profile.Lower), profile.Closure);
    }
    private ScopeImpact DescribeScopeCore(string profile, int assignmentIndex, SectionScope scope)
    {
        byte[] bytes;
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(current is not null, "DOC-EMPTY"); bytes = CurrentBytes; }
        var parsed = ParseOwned(bytes);
        var authored = parsed.Authored();
        Guard.Require(parsed.Definition!.Profiles.Any(item => item.Name == profile), "DSL-PROFILE-TARGET");
        Guard.Require(scope is SectionScope.Shared or SectionScope.Independent && (uint)assignmentIndex < (uint)authored.Assignments.Count &&
            authored.Assignments[assignmentIndex].ProfileName == profile, "DSL-PROFILE-TARGET");
        int[] affected = scope == SectionScope.Independent ? [assignmentIndex] :
            authored.Assignments.Select((item, index) => (item, index)).Where(pair => pair.item.ProfileName == profile).Select(pair => pair.index).ToArray();
        return new(profile, scope, affected, MergeIntervals(authored.Assignments, affected));
    }
    private static ProfileVertex[] Vertices(Curve curve, string side, string closure) => curve.Points.Select((point, index) =>
        new ProfileVertex(side, curve.Ids[index], point[0], point[1], index == 0 || (closure == "closed" && index == curve.Points.Length - 1))).ToArray();
    private static ProfilePoint[] Sample(Curve curve)
    {
        var watch = new ProofBudget();
        var spans = Bernstein.Spans(curve, watch);
        // Display polyline. The certificate inverse (1e-14) on a degree-5 10-CV rebuild
        // measured ~970ms and ProofBudget.Check (Geometry.cs:682) throws past one second.
        var accuracy = Rational.From(1e-8);
        var samples = new ProfilePoint[101];
        for (int index = 0; index < samples.Length; index++)
        {
            double x = index / 100d;
            var ordinate = Bernstein.EncloseAt(spans, Rational.From(x), watch, accuracy);
            samples[index] = new(x, (ordinate.Lower.Down() + ordinate.Upper.Up()) / 2);
        }
        return samples;
    }
    private static BlendInterval[] MergeIntervals(IReadOnlyList<AuthoredAssignment> stations, IReadOnlyList<int> affected)
    {
        var raw = new List<BlendInterval>();
        foreach (int index in affected)
        {
            if (index > 0) raw.Add(new(stations[index - 1].Eta, stations[index].Eta, stations[index - 1].SpanMeters, stations[index].SpanMeters));
            if (index + 1 < stations.Count) raw.Add(new(stations[index].Eta, stations[index + 1].Eta, stations[index].SpanMeters, stations[index + 1].SpanMeters));
        }
        raw.Sort((left, right) => left.EtaStart != right.EtaStart ? left.EtaStart.CompareTo(right.EtaStart) : left.EtaEnd.CompareTo(right.EtaEnd));
        var merged = new List<BlendInterval>();
        foreach (var interval in raw)
        {
            if (merged.Count == 0) { merged.Add(interval); continue; }
            var last = merged[^1];
            bool duplicate = interval.EtaStart == last.EtaStart && interval.EtaEnd == last.EtaEnd;
            if (duplicate) continue;
            if (interval.EtaStart < last.EtaEnd)
                merged[^1] = new(last.EtaStart, Math.Max(last.EtaEnd, interval.EtaEnd), last.RootDistanceStartMeters,
                    interval.EtaEnd > last.EtaEnd ? interval.RootDistanceEndMeters : last.RootDistanceEndMeters);
            else merged.Add(interval);
        }
        return merged.ToArray();
    }
    private SessionDraft BeginProfileEditCore(string draftId, int assignmentIndex, SectionScope scope, string side, string vertexId, ThicknessIntent thickness)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Uuid(draftId); Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
            var definition = ParseOwned(CurrentBytes).Definition!;
            Guard.Require(scope is SectionScope.Shared or SectionScope.Independent && (uint)assignmentIndex < (uint)definition.Assignments.Length && side is "upper" or "lower", "DSL-PROFILE-TARGET");
            var profile = definition.Profiles[definition.Assignments[assignmentIndex].Profile];
            var curve = side == "upper" ? profile.Upper : profile.Lower;
            int index = Array.IndexOf(curve.Ids, vertexId);
            Guard.Require(index >= 0, "DSL-PROFILE-TARGET");
            Guard.Require(index != 0 && (profile.Closure != "closed" || index != curve.Points.Length - 1), "DSL-LOCK");
            byte[] bytes = CurrentBytes; string target = profile.Name;
            if (scope == SectionScope.Independent)
            {
                var made = FoilSource.MakeIndependent(bytes, profile.Name, assignmentIndex);
                bytes = made.Source; target = made.NewProfile;
            }
            retiredDraftIds.Add(draftId);
            activeImportReport = null; importBasisFallback = null;
            draft = new(draftId, current!, 0, side, vertexId, bytes, target, assignmentIndex, thickness); return Copy(draft);
        }
    }
    private static List<(double[] Knots, double[] X, int Degree)> NeighbourBases(Definition definition, int assignmentIndex)
    {
        var found = new List<(double[] Knots, double[] X, int Degree)>();
        foreach (int index in new[] { assignmentIndex - 1, assignmentIndex + 1 })
        {
            if ((uint)index >= (uint)definition.Assignments.Length) continue;
            var profile = definition.Profiles[definition.Assignments[index].Profile];
            if (profile.Upper.Degree != profile.Lower.Degree || profile.Upper.Points.Length != profile.Lower.Points.Length) continue;
            if (!profile.Upper.Knots.SequenceEqual(profile.Lower.Knots)) continue;
            bool sameX = true;
            for (int point = 0; point < profile.Upper.Points.Length; point++)
                if (profile.Upper.Points[point][0] != profile.Lower.Points[point][0]) { sameX = false; break; }
            if (!sameX) continue;
            int degree = profile.Upper.Degree;
            double[] knots = profile.Upper.Knots.ToArray();
            double[] abscissae = profile.Upper.Points.Select(point => point[0]).ToArray();
            if (knots.Length != abscissae.Length + degree + 1) continue;
            if (found.Any(item => item.Degree == degree && item.Knots.SequenceEqual(knots) && item.X.SequenceEqual(abscissae))) continue;
            found.Add((knots, abscissae, degree));
        }
        return found;
    }
    private SessionDraft BeginProfileImportCore(string draftId, int assignmentIndex, byte[] dat)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Uuid(draftId);
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
            var definition = ParseOwned(CurrentBytes).Definition ?? throw new ContractError("DSL-PROFILE-TARGET");
            Guard.Require((uint)assignmentIndex < (uint)definition.Assignments.Length, "DSL-PROFILE-TARGET");

            var datProfile = DatImport.Parse(dat);
            string baseSlug = DatImport.Slug(datProfile.Name);
            var names = definition.Profiles.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
            string profileName = baseSlug;
            if (names.Contains(profileName))
            {
                for (int k = 1; ; k++)
                {
                    profileName = $"{baseSlug}-i{k.ToString(CultureInfo.InvariantCulture)}";
                    if (!names.Contains(profileName)) break;
                    Guard.Require(k < 100000, "DSL-LIMIT");
                }
            }

            var bases = NeighbourBases(definition, assignmentIndex);
            ImportedProfile? neighbourFit = null;
            foreach (var basis in bases)
            {
                var attempt = DatImport.FitToBasis(datProfile, profileName, basis.Knots, basis.X, basis.Degree);
                if (attempt is null) continue;
                if (neighbourFit is null || attempt.MaxResidual < neighbourFit.MaxResidual)
                    neighbourFit = attempt;
            }
            ImportedProfile fitted;
            string? fallback = null;
            string basisUsed;
            if (neighbourFit is not null && neighbourFit.MaxResidual <= 1e-5)
            {
                fitted = neighbourFit;
                basisUsed = "neighbour";
            }
            else
            {
                fitted = DatImport.Fit(datProfile, profileName);
                basisUsed = "own";
                fallback = DatImport.OwnSpacingReason(neighbourFit?.MaxResidual ?? double.PositiveInfinity);
            }
            var report = new ImportReport(fitted.MaxResidual, fitted.VertexCount, fitted.Accepted, fitted.Provenance, basisUsed);

            var target = definition.Profiles[definition.Assignments[assignmentIndex].Profile];
            string text = FoilSource.Utf8.GetString(CurrentBytes);
            int newline = text.LastIndexOf('\n', target.BlockStart);
            string indent = newline < 0 ? "" : text[(newline + 1)..target.BlockStart];
            string indentedBlock = string.Join("\n" + indent, fitted.ProfileBlock.Split('\n'));
            string insertion = "\n" + indent + indentedBlock;
            var token = definition.AssignmentProfiles[assignmentIndex];
            Guard.Require(token.Start >= target.BlockEnd, "DSL-PROFILE-TARGET");
            string result = text[..target.BlockEnd] + insertion + text[target.BlockEnd..token.Start] + Jcs.Quote(profileName) + text[token.End..];
            byte[] candidate = FoilSource.Utf8.GetBytes(result);
            Guard.Require(FoilSource.Parse(candidate).IsParsed, "DSL-PATCH");

            retiredDraftIds.Add(draftId);
            activeImportReport = report;
            importBasisFallback = fallback;
            draft = new(draftId, current!, 0, "profile", profileName, candidate, profileName, assignmentIndex);
            return Copy(draft);
        }
    }
    private SessionDraft UpdateProfileDraftCore(string draftId, long expectedGeneration, double x, double y)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == expectedGeneration && expectedGeneration < 9007199254740991, "DSL-CONFLICT");
            Guard.Require(draft!.Profile is not null && draft.Rail is "upper" or "lower", "DSL-PROFILE-TARGET");
            string profileName = draft.Profile!, side = draft.Rail;
            Guard.Require(double.IsFinite(x) && double.IsFinite(y), "DSL-PROFILE-ORDER");
            var profile = ParseOwned(draft.Bytes).Definition!.Profiles.First(item => item.Name == profileName);
            var curve = side == "upper" ? profile.Upper : profile.Lower;
            int index = Array.IndexOf(curve.Ids, draft.VertexId);
            Guard.Require(index >= 0, "DSL-PROFILE-TARGET");
            double previous = index == 0 ? double.NegativeInfinity : curve.Points[index - 1][0];
            double next = index + 1 == curve.Points.Length ? double.PositiveInfinity : curve.Points[index + 1][0];
            Guard.Require(x >= previous && x <= next, "DSL-PROFILE-ORDER");
            byte[] bytes = draft.Bytes;
            if (x != curve.Points[index][0])
            {
                var other = side == "upper" ? profile.Lower : profile.Upper;
                Guard.Require((uint)index < (uint)other.Points.Length, "DSL-PROFILE-TARGET");
                double otherPrevious = index == 0 ? double.NegativeInfinity : other.Points[index - 1][0];
                double otherNext = index + 1 == other.Points.Length ? double.PositiveInfinity : other.Points[index + 1][0];
                Guard.Require(x >= otherPrevious && x <= otherNext, "DSL-PROFILE-ORDER");
                bytes = FoilSource.PatchProfilePoint(bytes, profileName, side == "upper" ? "lower" : "upper", other.Ids[index], x, other.Points[index][1]);
            }
            bytes = FoilSource.PatchProfilePoint(bytes, profileName, side, draft.VertexId, x, y);
            if (draft.Intent == ThicknessIntent.UseSource) bytes = ThicknessFit.Fit(bytes, profileName);
            draft = draft with { Generation = expectedGeneration + 1, Bytes = bytes };
            return Copy(draft);
        }
    }
    private SessionDraft BeginProfileInsertCore(string draftId, int assignmentIndex, SectionScope scope, double x)
    {
        lock (sync)
        {
            var (bytes, target, profile) = PrepareConstruction(draftId, assignmentIndex, scope);
            Guard.Require(double.IsFinite(x) && x > 0 && x < 1, "DSL-PROFILE-TARGET");
            if (profile.Upper.Points.Length >= 32) throw new ContractError("DSL-CURVE");
            var patched = FoilSource.InsertProfileKnot(bytes, target, x);
            return OpenConstruction(draftId, "insert", patched.VertexId, patched.Source, target, assignmentIndex);
        }
    }
    private SessionDraft BeginProfileDeleteCore(string draftId, int assignmentIndex, SectionScope scope, int vertexIndex)
    {
        lock (sync)
        {
            var (bytes, target, profile) = PrepareConstruction(draftId, assignmentIndex, scope);
            int count = profile.Upper.Points.Length;
            Guard.Require(count == profile.Lower.Points.Length && (uint)vertexIndex < (uint)count, "DSL-PROFILE-TARGET");
            Guard.Require(vertexIndex != 0 && vertexIndex != count - 1, "DSL-LOCK");
            if (count <= 7) throw new ContractError("DSL-CURVE", "Delete would leave fewer than p + 2 = 7 vertices");
            var patched = FoilSource.DeleteProfileVertex(bytes, target, vertexIndex);
            return OpenConstruction(draftId, "delete", patched.VertexId, patched.Source, target, assignmentIndex);
        }
    }
    private SessionDraft BeginProfileFairCore(string draftId, int assignmentIndex, SectionScope scope, double tolerance, PreserveEnds ends)
    {
        lock (sync)
        {
            var (bytes, target, profile) = PrepareConstruction(draftId, assignmentIndex, scope);
            var (patched, result) = FoilSource.FairProfile(bytes, target, tolerance, ends);
            RecordFairAssessment(draftId, result, tolerance);
            return OpenConstruction(draftId, "fair", profile.Upper.Ids[0], patched, target, assignmentIndex);
        }
    }
    private SessionDraft BeginProfileRebuildCore(string draftId, int assignmentIndex, SectionScope scope, int vertexCount, double tolerance, PreserveEnds ends)
    {
        lock (sync)
        {
            var (bytes, target, profile) = PrepareConstruction(draftId, assignmentIndex, scope);
            var (patched, result) = FoilSource.RebuildProfile(bytes, target, vertexCount, tolerance, ends);
            RecordFairAssessment(draftId, result, tolerance);
            return OpenConstruction(draftId, "rebuild", profile.Upper.Ids[0], patched, target, assignmentIndex);
        }
    }
    // Recorded once at Begin (fair/rebuild drafts have no Update step) and read back by
    // ValidateCore, since the requested tolerance itself is not part of the draft's bytes.
    private void RecordFairAssessment(string draftId, FairResult result, double tolerance) =>
        pendingFairAssessment[draftId] = (result.MaxDeviation, tolerance, result.Upper.Points.Length, result.WithinTolerance, result.MonotonePiecesAfter > result.MonotonePiecesBefore);
    private (byte[] Bytes, string Profile, ProfileDefinition Definition) PrepareConstruction(string draftId, int assignmentIndex, SectionScope scope)
    {
        Guard.Require(!closed, "DOC-CLOSED");
        NativeProject.Uuid(draftId);
        Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
        Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
        var definition = ParseOwned(CurrentBytes).Definition!;
        Guard.Require(scope is SectionScope.Shared or SectionScope.Independent && (uint)assignmentIndex < (uint)definition.Assignments.Length, "DSL-PROFILE-TARGET");
        var profile = definition.Profiles[definition.Assignments[assignmentIndex].Profile];
        byte[] bytes = CurrentBytes;
        string target = profile.Name;
        if (scope == SectionScope.Independent)
        {
            var made = FoilSource.MakeIndependent(bytes, profile.Name, assignmentIndex);
            bytes = made.Source;
            target = made.NewProfile;
            profile = ParseOwned(bytes).Definition!.Profiles.First(item => item.Name == target);
        }
        return (bytes, target, profile);
    }
    private SessionDraft OpenConstruction(string draftId, string kind, string vertexId, byte[] bytes, string profile, int assignmentIndex)
    {
        retiredDraftIds.Add(draftId);
        draft = new(draftId, current!, 0, kind, vertexId, bytes, profile, assignmentIndex);
        return Copy(draft);
    }
    private SessionDraft UpdateDraftCore(string draftId, long expectedGeneration, double si)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == expectedGeneration && expectedGeneration < 9007199254740991, "DSL-CONFLICT");
            draft = draft! with { Generation = expectedGeneration + 1, Bytes = FoilSource.PatchRail(ParseOwned(draft.Bytes), draft.Rail, draft.VertexId, si) }; return Copy(draft);
        }
    }
    private static Diagnostic ThicknessDiagnostic(SessionDraft capture, string fault) => new(fault, "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, "thickness",
        fault == "DSL-LOCK" ? "A thickness lock contradicts the source-thickness target." : "The thickness fit is singular or its residual exceeds 1e-9.",
        "Keep the current thickness or relax the lock.");
    private SessionAssessment ValidateCore(string draftId, long generation, CancellationToken cancellation = default)
    {
        SessionDraft capture;
        byte[]? origin = null;
        byte[] baseline;
        string? basisFallback;
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == generation, "DSL-CONFLICT");
            Guard.Require(Interlocked.CompareExchange(ref validating, 1, 0) == 0, "DSL-VALIDATION-BUSY");
            capture = Copy(draft!);
            if (capture.Rail is "insert" or "delete" && capture.Profile is not null) origin = BaseBytes(capture.Base);
            baseline = CurrentBytes;
            basisFallback = importBasisFallback;
        }
        ConstructionReport? construction = null;
        string? fairIssue = null;
        try
        {
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null, DraftBinding(capture), null, importReport: activeImportReport);
            var timer = System.Diagnostics.Stopwatch.StartNew(); var parsed = FoilSource.Parse(capture.Bytes);
            Record("language.parse", parsed.IsParsed ? "OK" : parsed.Diagnostics[0].Code, timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, parsed.IsParsed ? "cfdw-cv/2" : null);
            if (parsed.IsParsed && origin is not null) construction = MeasureConstruction(capture, parsed, origin);
            if (parsed.IsParsed && capture.Rail is "fair" or "rebuild" && pendingFairAssessment.TryGetValue(capture.Id, out var fair))
            {
                construction = new ConstructionReport(fair.MaxDeviation, fair.Tolerance, fair.VertexCount);
                fairIssue = !fair.WithinTolerance ? "Fair result exceeds tolerance" : fair.PiecesIncreased ? "Fair increased monotone pieces" : null;
            }
            if (!parsed.IsParsed) return new(authorityId, parsed.Diagnostics[0].Code == "DSL-LIMIT" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid,
                parsed.Diagnostics[0].Code, null, null, DraftBinding(capture, parsed), parsed.Diagnostics, construction, importReport: activeImportReport);
            timer.Restart(); var key = Key(parsed, capture);
            Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, "cfdw-cv/2");
            ThicknessFit.View? thickness = capture.Intent == ThicknessIntent.UseSource && capture.Profile is not null
                ? ThicknessFit.Describe(capture.Bytes, capture.Profile, baseline) : null;
            if (thickness?.Fault is string fault)
                return new(authorityId, fault == "DSL-LOCK" ? GeometryStatus.Invalid : GeometryStatus.NotAssessed, fault, key, null,
                    DraftBinding(capture, parsed), [ThicknessDiagnostic(capture, fault)], construction, thickness.Value.Proposal);
            var result = AssessOwned(parsed, generation);
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", key, null, DraftBinding(capture, parsed), null, construction, importReport: activeImportReport);
            GeometryStatus status = result.Status; string code = result.Code; string reason = result.Reason;
            if (fairIssue is not null && status == GeometryStatus.Certified) { status = GeometryStatus.Invalid; code = "DSL-GEOMETRY"; reason = fairIssue; }
            var diagnostics = new List<Diagnostic>();
            if (status != GeometryStatus.Certified)
                diagnostics.Add(new(code, "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, capture.Rail, reason, "Revise the authored curves or retain the last accepted revision."));
            if (basisFallback is not null)
                diagnostics.Add(new("DSL-GEOMETRY", "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, capture.Rail, basisFallback, "Import the profile at every station that shares it, or Rebuild the neighbouring profiles."));
            return new(authorityId, status, code, key, status == GeometryStatus.Certified ? result.Certificate : null, DraftBinding(capture, parsed), diagnostics, construction, thickness?.Proposal, importReport: activeImportReport);
        }
        catch (ContractError error)
        { return new(authorityId, error.Code is "DSL-LIMIT" or "DSL-UNSUPPORTED" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid, error.Code, null, null, DraftBinding(capture), null, construction, importReport: activeImportReport); }
        finally { Interlocked.Exchange(ref validating, 0); }
    }
    private byte[] BaseBytes(string acceptedId)
    {
        var row = accepted.Single(item => item.Id == acceptedId);
        return Decode(sources.Single(item => item.Id == row.SourceId).Utf8Base64Chunks);
    }
    private static ConstructionReport? MeasureConstruction(SessionDraft capture, SourceParse parsed, byte[] origin)
    {
        if (parsed.Definition is null || capture.Profile is null) return null;
        var originParse = FoilSource.Parse(origin);
        if (originParse.Definition is null || (uint)capture.Assignment >= (uint)originParse.Definition.Assignments.Length) return null;
        string assigned = originParse.Definition.Profiles[originParse.Definition.Assignments[capture.Assignment].Profile].Name;
        if (!string.Equals(assigned, capture.Profile, StringComparison.Ordinal))
        {
            var made = FoilSource.MakeIndependent(origin, assigned, capture.Assignment);
            originParse = FoilSource.Parse(made.Source);
            if (originParse.Definition is null) return null;
        }
        var prior = originParse.Definition.Profiles.FirstOrDefault(item => item.Name == capture.Profile);
        var next = parsed.Definition.Profiles.FirstOrDefault(item => item.Name == capture.Profile);
        if (prior is null || next is null) return null;
        return new(FoilSource.MaxOrdinateDeviation(prior, next), null, next.Upper.Points.Length);
    }
    private string ApplyDimensionCore(string operationId, DimensionCommand command)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(command.Name is "span" or "root-chord" or "tip-chord", "DSL-TARGET");
            Guard.Require(command.Name == "span", "DSL-TARGET");
            if (operations.ContainsKey(operationId))
            {
                var replay = PrepareSpan(operationId, command, true);
                if (Retry(operationId, replay.Payload, out string prior)) return prior;
            }
            NativeProject.Uuid(operationId);
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(operationId), "DSL-DRAFT-REUSED");
            var fresh = PrepareSpan(operationId, command, false);
            retiredDraftIds.Add(operationId);
            draft = new(operationId, current!, 0, "dimension", command.Name, fresh.Patched);
            try
            {
                var key = Key(fresh.Parsed, draft);
                RequireAdmission(fresh.Parsed, key);
                string id = Commit(fresh.Parsed, operationId, "apply");
                operations.Add(operationId, (fresh.Payload, id));
                draft = null;
                recovery = null;
                return id;
            }
            catch
            {
                draft = null;
                retiredDraftIds.Remove(operationId);
                throw;
            }
        }
    }

    private DimensionOutcome ApplyChordCore(string operationId, DimensionCommand command)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(command.Name is "root-chord" or "tip-chord", "DSL-TARGET");
            if (operations.ContainsKey(operationId))
            {
                var replay = PrepareChord(operationId, command, true);
                if (Retry(operationId, replay.Payload, out string prior))
                {
                    Remember(replay.Report);
                    return new(prior, replay.Report);
                }
            }
            NativeProject.Uuid(operationId);
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(operationId), "DSL-DRAFT-REUSED");
            var fresh = PrepareChord(operationId, command, false);
            retiredDraftIds.Add(operationId);
            draft = new(operationId, current!, 0, "dimension", command.Name, fresh.Patched, Rule: fresh.Report.Rule);
            try
            {
                var key = Key(fresh.Parsed, draft);
                RequireAdmission(fresh.Parsed, key);
                string id = Commit(fresh.Parsed, operationId, "apply");
                operations.Add(operationId, (fresh.Payload, id));
                draft = null;
                recovery = null;
                Remember(fresh.Report);
                return new(id, fresh.Report);
            }
            catch
            {
                draft = null;
                retiredDraftIds.Remove(operationId);
                throw;
            }
        }
    }

    private (string Payload, byte[] Patched, SourceParse Parsed) PrepareSpan(string operationId, DimensionCommand command, bool replay)
    {
        string? parentId;
        byte[] basis;
        if (replay)
        {
            var row = accepted.Single(item => item.OperationId == operationId && item.Edit?.Rail == "dimension");
            parentId = row.Parent;
            basis = BaseBytes(parentId!);
        }
        else
        {
            parentId = current;
            basis = CurrentBytes;
        }
        double spanSi = DecimalSi.Parse(command.Text, -3);
        Guard.Require(spanSi > 0, "DSL-UNIT");
        Guard.Require(spanSi < 1e6, "DSL-EDGES-CROSS");
        byte[] patched = FoilSource.PatchSpan(basis, command.Text);
        var parsed = ParseOwned(patched);
        return (Fingerprint(new EditReceipt(operationId, 0, "dimension", "span"), parentId, parsed.SourceHash), patched, parsed);
    }

    private (string Payload, byte[] Patched, SourceParse Parsed, DimensionReport Report) PrepareChord(string operationId, DimensionCommand command, bool replay)
    {
        string? parentId;
        byte[] basis;
        if (replay)
        {
            var row = accepted.Single(item => item.OperationId == operationId && item.Edit?.Rail == "dimension");
            parentId = row.Parent;
            basis = BaseBytes(parentId!);
        }
        else
        {
            parentId = current;
            basis = CurrentBytes;
        }
        var (report, patched) = ChordDimension.Evaluate(basis, command);
        var parsed = ParseOwned(patched);
        var receipt = new EditReceipt(operationId, 0, "dimension", command.Name, Rule: report.Rule);
        return (Fingerprint(receipt, parentId, parsed.SourceHash), patched, parsed, report);
    }

    private void Remember(DimensionReport report)
    {
        pendingFitUm = report.FitResidualMeters * 1e6;
        pendingDeviationUm = report.DeviationFromLinearMeters * 1e6;
        pendingShiftUm = report.PlanformShiftMeters * 1e6;
        pendingFitAboveLimit = report.FitAboveLimit;
    }
    private PointOutcome ApplyPointCommandCore(string operationId, PointCommand command)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Uuid(operationId);
            Guard.Require(command is not null && PointModel.EditableCurves.Contains(command.Curve), "DSL-TARGET");
            bool replay = operations.ContainsKey(operationId);
            Guard.Require(replay || current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            var priorOperation = replay ? accepted.SingleOrDefault(row => row.OperationId == operationId) : null;
            if (replay && priorOperation?.Edit?.Rail is not ("point-type" or "tangent-kind"))
                throw new ContractError("DOC-OPERATION-CONFLICT");
            string? parent = replay ? priorOperation!.Parent : current;
            byte[] basis = replay ? BaseBytes(parent!) : CurrentBytes;
            var prior = ParseOwned(basis);
            var oldCurve = prior.Definition!.Curves[command!.Curve];
            int index = Array.IndexOf(oldCurve.Ids, command.VertexId);
            Guard.Require(index >= 0, "DSL-TARGET");
            byte[] bytes;
            try { bytes = EvaluatePointCommand(prior, command, index); }
            catch (ContractError) when (replay) { throw new ContractError("DOC-OPERATION-CONFLICT"); }
            var next = ParseOwned(bytes);
            string kind = command is PointCommand.SetTangent ? "tangent-kind" : "point-type";
            var receipt = new EditReceipt(operationId, 0, kind, command.VertexId, Curve: command.Curve);
            string payload = Fingerprint(receipt, parent, next.SourceHash, command.Curve);
            if (Retry(operationId, payload, out string existing))
                return new(existing, MaxPointDelta(basis, bytes, command.Curve), oldCurve.Points.Length, next.Definition!.Curves[command.Curve].Points.Length);
            Guard.Require(!retiredDraftIds.Contains(operationId), "DSL-DRAFT-REUSED");
            retiredDraftIds.Add(operationId);
            draft = new(operationId, current!, 0, kind, command.VertexId, bytes, Curve: command.Curve);
            try
            {
                RequireAdmission(next, Key(next, draft));
                string id = Commit(next, operationId, "apply");
                operations.Add(operationId, (payload, id));
                draft = null; recovery = null;
                return new(id, MaxPointDelta(basis, bytes, command.Curve), oldCurve.Points.Length, next.Definition!.Curves[command.Curve].Points.Length);
            }
            catch
            {
                draft = null; retiredDraftIds.Remove(operationId); throw;
            }
        }
    }

    private static double MaxPointDelta(byte[] before, byte[] after, string curve)
    {
        var first = Planform.View(before, "Accepted", 0);
        var second = Planform.View(after, "Accepted", 0);
        double max = 0;
        for (int step = 0; step <= 200; step++)
        {
            double eta = step / 200d;
            var a = Planform.Probe(first, eta);
            var b = Planform.Probe(second, eta);
            max = Math.Max(max, Math.Abs(curve == "leading" ? b.LeadingAftMeters - a.LeadingAftMeters : b.TrailingAftMeters - a.TrailingAftMeters));
        }
        return max;
    }

    private static byte[] EvaluatePointCommand(SourceParse parsed, PointCommand command, int index)
    {
        var definition = parsed.Definition!;
        var original = definition.Curves[command.Curve];
        var view = Planform.View(parsed.Source, "Accepted", 0);
        var point = (command.Curve == "leading" ? view.Leading : view.Trailing).Points[index];
        Curve changed = command switch
        {
            PointCommand.MakeAnchor => MakeAnchor(original, index, point),
            PointCommand.MakeControl => MakeControl(original, index, point),
            PointCommand.SetTangent tangent => SetTangent(original, index, point, tangent),
            _ => throw new ContractError("DSL-TARGET")
        };
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal) { [command.Curve] = changed };
        byte[] printed = FoilSource.Print(definition with { Curves = curves });
        if (changed.Tangents.Length > 0 || changed.Points.Length > 10)
            printed = FoilSource.EnsureHeader41(printed);
        var check = FoilSource.Parse(printed);
        Guard.Require(check.IsParsed, "DSL-PATCH");
        return printed;
    }

    private static Curve MakeAnchor(Curve curve, int index, PointView point)
    {
        Guard.Require(point.Role == PointRole.Control, "DSL-LOCK");
        double eta = curve.Points[index][0];
        double low = 0, high = 1;
        for (int step = 0; step < 64; step++)
        {
            double middle = (low + high) / 2;
            var basis = SplineBasis.Evaluate(curve.Knots, curve.Degree, middle);
            double value = 0;
            for (int i = 0; i < curve.Points.Length; i++) value += basis.N[i] * curve.Points[i][0];
            if (value < eta) low = middle; else high = middle;
        }
        double knot = (low + high) / 2;
        foreach (double existing in curve.Knots)
            if (Math.Abs(existing - knot) <= 1e-12 * Math.Max(1, Math.Abs(knot))) { knot = existing; break; }
        int multiplicity = curve.Knots.Count(value => value == knot);
        int additions = 3 - multiplicity;
        Guard.Require(additions > 0, "DSL-LOCK");
        int ceiling = 16;
        if (curve.Points.Length + additions > ceiling)
            throw new ContractError("DSL-CURVE", $"Making this an anchor needs {additions} more points. This rail has {curve.Points.Length} of {ceiling}.");
        var knots = curve.Knots; var points = curve.Points.Select(p => p.ToArray()).ToArray(); var ids = curve.Ids.ToArray();
        int suffix = ids.Select(id => id.StartsWith("cv-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), out int n) ? n : -1).Max() + 1;
        for (int turn = 0; turn < additions; turn++)
        {
            int k = curve.Degree;
            while (k + 1 < points.Length && knots[k + 1] <= knot) k++;
            int s = knots.Count(value => value == knot);
            var nextKnots = new double[knots.Length + 1];
            Array.Copy(knots, nextKnots, k + 1); nextKnots[k + 1] = knot;
            Array.Copy(knots, k + 1, nextKnots, k + 2, knots.Length - k - 1);
            var nextPoints = new double[points.Length + 1][];
            for (int i = 0; i <= k - 3; i++) nextPoints[i] = points[i].ToArray();
            for (int i = k - s; i < points.Length; i++) nextPoints[i + 1] = points[i].ToArray();
            for (int i = k - 2; i <= k - s; i++)
            {
                double alpha = (knot - knots[i]) / (knots[i + 3] - knots[i]);
                nextPoints[i] = [(1 - alpha) * points[i - 1][0] + alpha * points[i][0],
                    (1 - alpha) * points[i - 1][1] + alpha * points[i][1]];
            }
            int inserted = (2 * (k + 1) - (s + 1) - 3) / 2;
            var nextIds = ids.ToList(); nextIds.Insert(inserted, "cv-" + suffix++);
            knots = nextKnots; points = nextPoints; ids = nextIds.ToArray();
        }
        int anchor = Enumerable.Range(3, points.Length - 6).Single(i =>
            knots[i + 1] == knot && FoilSource.IsAnchor(knots, points.Length, 3, i));
        int oldIdAtAnchor = Array.IndexOf(ids, point.Id);
        if (oldIdAtAnchor != anchor)
        {
            string displaced = ids[anchor]; ids[anchor] = point.Id;
            ids[oldIdAtAnchor] = displaced;
        }
        var sample = SplineBasis.Evaluate(knots, 3, knot);
        double aft = 0;
        for (int i = 0; i < points.Length; i++) aft += sample.N[i] * points[i][1];
        double shift = point.AftMeters - aft;
        for (int i = anchor - 1; i <= anchor + 1; i++) points[i][1] += shift;
        for (int i = 1; i < points.Length; i++)
            Guard.Require((points[i][0] - points[i - 1][0]) * 1 >= 1e-7 - 1e-12, "DSL-CURVE");
        return curve with { Knots = knots, Points = points, Ids = ids,
            Tangents = [.. curve.Tangents, new TangentRow(point.Id, "smooth", null)] };
    }

    private static Curve MakeControl(Curve curve, int index, PointView point)
    {
        Guard.Require(point.Role == PointRole.Anchor, "DSL-LOCK");
        Guard.Require(curve.Points.Length >= 8, "DSL-CURVE");
        double knot = curve.Knots[index + 1];
        var points = curve.Points.Where((_, i) => i != index - 1 && i != index + 1).ToArray();
        var ids = curve.Ids.Where((_, i) => i != index - 1 && i != index + 1).ToArray();
        var knots = curve.Knots.ToList();
        for (int i = 0; i < 2; i++) knots.Remove(knot);
        return curve with { Knots = knots.ToArray(), Points = points, Ids = ids,
            Tangents = curve.Tangents.Where(row => row.Id != point.Id).ToArray() };
    }

    private static Curve SetTangent(Curve curve, int index, PointView point, PointCommand.SetTangent command)
    {
        Guard.Require(point.Role == PointRole.Anchor, "DSL-LOCK");
        Guard.Require(command.KeepHandleId is null || command.KeepHandleId == curve.Ids[index - 1] || command.KeepHandleId == curve.Ids[index + 1], "DSL-TARGET");
        var points = curve.Points.Select(p => p.ToArray()).ToArray();
        if (command.Kind != TangentKind.Corner)
        {
            int keep = command.KeepHandleId == curve.Ids[index + 1] ? index + 1 : index - 1;
            int move = keep == index - 1 ? index + 1 : index - 1;
            double slope;
            if (command.KeepHandleId is null)
            {
                double halfSpan = point.SpanMeters / point.Eta;
                double left = (points[index][1] - points[index - 1][1]) / ((points[index][0] - points[index - 1][0]) * halfSpan);
                double right = (points[index + 1][1] - points[index][1]) / ((points[index + 1][0] - points[index][0]) * halfSpan);
                double leftNorm = Math.Sqrt(1 + left * left), rightNorm = Math.Sqrt(1 + right * right);
                slope = halfSpan * (left / leftNorm + right / rightNorm) / (1 / leftNorm + 1 / rightNorm);
                points[index - 1][1] = points[index][1] + slope * (points[index - 1][0] - points[index][0]);
                points[index + 1][1] = points[index][1] + slope * (points[index + 1][0] - points[index][0]);
            }
            else
            {
                slope = (points[keep][1] - points[index][1]) / (points[keep][0] - points[index][0]);
                points[move][1] = points[index][1] + slope * (points[move][0] - points[index][0]);
            }
            if (command.Kind == TangentKind.Symmetric)
            {
                double shortEta = Math.Min(points[index][0] - points[index - 1][0], points[index + 1][0] - points[index][0]);
                points[index - 1][0] = points[index][0] - shortEta;
                points[index + 1][0] = points[index][0] + shortEta;
                points[index - 1][1] = points[index][1] - slope * shortEta;
                points[index + 1][1] = points[index][1] + slope * shortEta;
            }
        }
        var rows = curve.Tangents.Where(row => row.Id != point.Id).ToList();
        if (command.Kind != TangentKind.Corner)
            rows.Add(new(point.Id, command.Kind == TangentKind.Symmetric ? "symmetric" : "smooth", null));
        return curve with { Points = points, Tangents = rows.ToArray() };
    }
    private string ApplyCore(string operationId, SessionAssessment assessment)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(assessment.ImportReport is null || assessment.ImportReport.Accepted, "DSL-TOLERANCE");
            Guard.Require(assessment.Key is not null && assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null, "DSL-NOT-ASSESSED");
            if (assessment.Thickness is { } proposal)
                Guard.Require(proposal.Residuals.All(item => Math.Abs(item) <= 1e-9), "GEOMETRY-FIT-SINGULAR");
            string payload = "apply:" + JsonSerializer.Serialize(assessment.Key);
            if (Retry(operationId, payload, out string prior)) return prior;
            Guard.Require(draft is not null && current == draft.Base, "DSL-CONFLICT");
            var p = ParseOwned(draft!.Bytes); var key = Key(p, draft);
            Guard.Require(assessment.Owner == authorityId && assessment.Certificate!.SourceHash == key.SourceHash &&
                assessment.Certificate.SurfaceHash == key.SurfaceHash && assessment.Key == key, "DSL-CONFLICT");
            bool gesture = gestureDraftId == draft.Id;
            string id = Commit(p, operationId, "apply"); operations.Add(operationId, (payload, id)); draft = null; recovery = null; activeImportReport = null; importBasisFallback = null;
            if (gesture) { Record("gesture.end", "OK", System.Diagnostics.Stopwatch.GetElapsedTime(gestureStarted).TotalMilliseconds, null, null, assessment.Key!.Generation, "cfdw-cv/2", frames: gestureFrames); gestureDraftId = null; gestureFrames = 0; }
            return id;
        }
    }
    private void CancelCore(string draftId) { lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft?.Id == draftId, "DSL-CONFLICT"); draft = null; recovery = null; activeImportReport = null; importBasisFallback = null; if (gestureDraftId == draftId) { Record("gesture.end", "NoChange", System.Diagnostics.Stopwatch.GetElapsedTime(gestureStarted).TotalMilliseconds, null, null, null, "cfdw-cv/2", frames: gestureFrames); gestureDraftId = null; gestureFrames = 0; } } }
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
            RequireAdmission(targetParsed, new(targetParsed.SourceHash, "", "", 0, "cfdw-cv/2", targetParsed.SurfaceHash!, "", ""), toleratesBudget: true);
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
            var next = new RecoveryRow(draft!.Id, draft.Base, draft.Generation, draft.Rail, draft.VertexId, Chunks(draft.Bytes), draft.Profile, draft.Assignment, draft.Intent);
            NativeProject.Preflight(EnvelopeCore() with { Recovery = next }, envelopeCap);
            recovery = next; return CopyRecovery(recovery)!;
        }
    }
    private void ResumeRecoveryCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(recovery is not null && draft is null && recovery.BaseAcceptedId == current, "DOC-RECOVERY-BASE"); draft = new(recovery!.DraftId, recovery.BaseAcceptedId, recovery.Generation, recovery.Rail, recovery.VertexId, Decode(recovery.Utf8Base64Chunks), recovery.Profile, recovery.Assignment, recovery.Intent); }
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
            var key = new SessionBinding(p.SourceHash, "", "", 0, "cfdw-cv/2", p.SurfaceHash!, "", "");
            RequireAdmission(p, key, toleratesBudget: true);
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
                    string payload = c.Reason == "open"
                        ? "open:" + a.SourceId
                        : a.Edit?.Rail == "dimension"
                            ? Fingerprint(a.Edit, a.Parent, a.SourceId)
                            : a.Edit?.Rail is "point-type" or "tangent-kind"
                                ? Fingerprint(a.Edit, a.Parent, a.SourceId, a.Edit.Curve ?? "")
                            : "apply:" + JsonSerializer.Serialize(new SessionBinding(a.SourceId, a.Parent!, a.Edit!.DraftId, a.Edit.Generation, d.Evaluator, d.SurfaceHash!, a.Edit.Rail, a.Edit.VertexId));
                    operations[c.OperationId] = (payload, c.Target);
                }
            }
            // Dirty-state identity is independent of original on-disk formatting/conflict token.
            savedImageHash = Identity.Sha256(NativeProject.Encode(EnvelopeCore()));
        }
    }
}

internal sealed class RecoveryRowConverter : JsonConverter<RecoveryRow>
{
    // A rail recovery (Profile null) serializes exactly as before this field was
    // added, so an envelope holding only rail recoveries is byte-for-byte
    // unchanged and the "no such fields" backward-read path is exercised for real.
    public override RecoveryRow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        string[] chunks = root.GetProperty("utf8Base64Chunks").EnumerateArray().Select(e => e.GetString()!).ToArray();
        string? profile = root.TryGetProperty("profile", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetString() : null;
        int assignment = root.TryGetProperty("assignment", out var a) && a.ValueKind != JsonValueKind.Null ? a.GetInt32() : -1;
        ThicknessIntent intent = ThicknessIntent.KeepCurrent;
        if (root.TryGetProperty("intent", out var intentElement) && intentElement.ValueKind != JsonValueKind.Null)
            intent = intentElement.ValueKind == JsonValueKind.String
                ? Enum.Parse<ThicknessIntent>(intentElement.GetString()!)
                : (ThicknessIntent)intentElement.GetInt32();
        return new RecoveryRow(root.GetProperty("draftId").GetString()!, root.GetProperty("baseAcceptedId").GetString()!,
            root.GetProperty("generation").GetInt64(), root.GetProperty("rail").GetString()!, root.GetProperty("vertexId").GetString()!,
            chunks, profile, assignment, intent);
    }
    public override void Write(Utf8JsonWriter writer, RecoveryRow value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("draftId", value.DraftId);
        writer.WriteString("baseAcceptedId", value.BaseAcceptedId);
        writer.WriteNumber("generation", value.Generation);
        writer.WriteString("rail", value.Rail);
        writer.WriteString("vertexId", value.VertexId);
        writer.WriteStartArray("utf8Base64Chunks");
        foreach (string chunk in value.Utf8Base64Chunks) writer.WriteStringValue(chunk);
        writer.WriteEndArray();
        if (value.Profile is not null) { writer.WriteString("profile", value.Profile); writer.WriteNumber("assignment", value.Assignment); }
        if (value.Intent != ThicknessIntent.KeepCurrent) writer.WriteString("intent", value.Intent.ToString());
        writer.WriteEndObject();
    }
}

public static class NativeProject
{
    public const int MaxBytes = 8_000_000;
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow, Converters = { new RecoveryRowConverter() } };
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
    static void Exact(JsonElement element, string[] required, string[]? optional = null)
    {
        Guard.Require(element.ValueKind == JsonValueKind.Object, "DOC-SCHEMA");
        var names = element.EnumerateObject().Select(p => p.Name).ToArray();
        Guard.Require(names.Distinct(StringComparer.Ordinal).Count() == names.Length, "DOC-SCHEMA");
        Guard.Require(required.All(names.Contains), "DOC-SCHEMA");
        var allowed = optional is null ? required : [.. required, .. optional];
        Guard.Require(names.All(allowed.Contains), "DOC-UNSUPPORTED-FIELD");
    }
    static void Exact(JsonElement element, params string[] keys) => Exact(element, keys, null);
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
                if (a.GetProperty("edit").ValueKind != JsonValueKind.Null)
                    Exact(a.GetProperty("edit"), ["draftId", "generation", "rail", "vertexId"], ["intent", "rule", "curve"]);
            }
            foreach (var c in root.GetProperty("cursors").EnumerateArray()) Exact(c, "sequence", "target", "reason", "operationId");
            var recoveryElement = root.GetProperty("recovery");
            if (recoveryElement.ValueKind != JsonValueKind.Null)
            {
                Exact(recoveryElement, ["draftId", "baseAcceptedId", "generation", "rail", "vertexId", "utf8Base64Chunks"], ["profile", "assignment", "intent"]);
                Guard.Require(recoveryElement.TryGetProperty("profile", out _) == recoveryElement.TryGetProperty("assignment", out _), "DOC-SCHEMA");
            }
            var env = JsonSerializer.Deserialize<Envelope>(bytes, Options)!; Check(env); return env;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or ArgumentException or NullReferenceException or KeyNotFoundException) { throw new ContractError("DOC-SCHEMA"); }
    }
    static bool EditTarget(Definition definition, string channel, string vertexId) => channel switch
    {
        "leading" or "trailing" => definition.Curves[channel].Ids.Contains(vertexId),
        "upper" or "lower" => definition.Profiles.Any(profile => (channel == "upper" ? profile.Upper : profile.Lower).Ids.Contains(vertexId)),
        _ => false
    };
    static bool ProfileHas(Definition definition, string vertexId) =>
        definition.Profiles.Any(profile => profile.Upper.Ids.Contains(vertexId) || profile.Lower.Ids.Contains(vertexId));
    // A construction (insert/delete/fair/rebuild) rail names a profile vertex rather than a
    // channel EditTarget knows. Insert's vertex is freshly minted, so only the child has it.
    // Delete's and rebuild's vertex is read off the profile before the operation runs (and
    // rebuild may then renumber every id, per FoilSource.RebuildProfile.NextVertexIds), so
    // only the parent is guaranteed to hold it. Fair renumbers neither, so both must.
    static bool EditReference(Definition child, Definition parent, EditReceipt edit) => edit.Rail switch
    {
        "leading" or "trailing" or "upper" or "lower" => edit.Curve is null && edit.Rule is null && EditTarget(child, edit.Rail, edit.VertexId) && EditTarget(parent, edit.Rail, edit.VertexId),
        "insert" => edit.Curve is null && edit.Rule is null && ProfileHas(child, edit.VertexId),
        "delete" or "rebuild" => edit.Curve is null && edit.Rule is null && ProfileHas(parent, edit.VertexId),
        "fair" => edit.Curve is null && edit.Rule is null && ProfileHas(child, edit.VertexId) && ProfileHas(parent, edit.VertexId),
        "dimension" => edit.Curve is null && edit.VertexId is "span" or "root-chord" or "tip-chord"
            && (edit.VertexId == "span"
                ? edit.Rule is null
                : edit.Rule is ChordDimension.RootFlat or ChordDimension.Linear),
        "point-type" or "tangent-kind" => edit.Rule is null && edit.Curve is not null &&
            PointModel.EditableCurves.Contains(edit.Curve) &&
            EditTarget(child, edit.Curve, edit.VertexId) && EditTarget(parent, edit.Curve, edit.VertexId),
        _ => false
    };
    // Same construction rails, against the recovered draft's base: insert/rebuild name a
    // vertex the base does not have yet (or has renumbered), so there is nothing to check there.
    static bool RecoveryReference(Definition definition, string rail, string vertexId) => rail switch
    {
        "leading" or "trailing" or "upper" or "lower" => EditTarget(definition, rail, vertexId),
        "delete" or "fair" => ProfileHas(definition, vertexId),
        "insert" or "rebuild" => true,
        _ => false
    };
    static void Check(Envelope e)
    {
        Uuid(e.ProjectId);
        Guard.Require(e.Sources.Length > 0 && e.Designs.Length > 0 && e.Accepted.Length > 0 && e.Cursors.Length > 0, "DOC-REFERENCE");
        Guard.Require(e.Sources.Select(x => x.Id).Distinct().Count() == e.Sources.Length && e.Designs.Select(x => x.Id).Distinct().Count() == e.Designs.Length && e.Accepted.Select(x => x.Id).Distinct().Count() == e.Accepted.Length, "DOC-REFERENCE");
        // Native compatibility is checked before parsing any retained source or
        // considering adoption. An older evaluator is not a broken reference.
        foreach (var design in e.Designs) Guard.Require(design.Evaluator == "cfdw-cv/2", "DOC-VERSION");
        var parsed = new Dictionary<string, SourceParse>();
        foreach (var s in e.Sources) { Hash(s.Id); byte[] bytes = Decode(s.Utf8Base64Chunks); Guard.Require(Identity.Sha256(bytes) == s.Id, "DOC-INTEGRITY"); var p = SessionSource.Parse(bytes); Guard.Require(p.Definition!.Curves.Values.All(c => !c.MissingIds), "DOC-INTEGRITY"); parsed.Add(s.Id, p); }
        var designs = new Dictionary<string, DesignRow>();
        foreach (var d in e.Designs) { Uuid(d.Id); Hash(d.SurfaceHash!); Guard.Require(designs.Count == 0 ? d.Parent is null : d.Parent is not null && designs.ContainsKey(d.Parent), "DOC-REFERENCE"); designs.Add(d.Id, d); }
        var accepted = new Dictionary<string, AcceptedRow>();
        foreach (var a in e.Accepted)
        {
            Uuid(a.Id); Uuid(a.OperationId); Guard.Require(accepted.Count == 0 ? a.Parent is null : a.Parent is not null && accepted.ContainsKey(a.Parent), "DOC-REFERENCE");
            Guard.Require(parsed.ContainsKey(a.SourceId) && designs.ContainsKey(a.DesignId), "DOC-REFERENCE");
            Guard.Require(parsed[a.SourceId].SurfaceHash! == designs[a.DesignId].SurfaceHash!, "DOC-INTEGRITY");
            if (a.Parent is not null)
            {
                Guard.Require(a.Edit is not null, "DOC-REFERENCE"); Uuid(a.Edit!.DraftId);
                Guard.Require(a.Edit.Generation is >= 0 and <= 9007199254740991, "DOC-REFERENCE");
                var parent = accepted[a.Parent]; bool same = parsed[a.SourceId].SurfaceHash! == parsed[parent.SourceId].SurfaceHash!;
                Guard.Require(EditReference(parsed[a.SourceId].Definition!, parsed[parent.SourceId].Definition!, a.Edit), "DOC-REFERENCE");
                Guard.Require(same ? a.DesignId == parent.DesignId : designs[a.DesignId].Parent == parent.DesignId, "DOC-REFERENCE");
            }
            else Guard.Require(a.Edit is null, "DOC-REFERENCE");
            accepted.Add(a.Id, a);
        }
        Guard.Require(e.Sources.All(s => e.Accepted.Any(a => a.SourceId == s.Id)) && e.Designs.All(d => e.Accepted.Any(a => a.DesignId == d.Id)), "DOC-REFERENCE");
        _ = Replay(e);
        if (e.Recovery is not null)
        {
            var r = e.Recovery; Uuid(r.DraftId);
            Guard.Require(accepted.ContainsKey(r.BaseAcceptedId) && r.Generation is >= 0 and <= 9007199254740991
                && r.Rail is "leading" or "trailing" or "upper" or "lower" or "insert" or "delete" or "fair" or "rebuild", "DOC-REFERENCE");
            Guard.Require(r.Rail is not ("insert" or "delete" or "fair" or "rebuild") || r.Profile is not null, "DOC-REFERENCE");
            var definition = parsed[accepted[r.BaseAcceptedId].SourceId].Definition!;
            Guard.Require(r.VertexId.Length > 0 && r.VertexId.EnumerateRunes().Count() <= 4096 && RecoveryReference(definition, r.Rail, r.VertexId), "DOC-REFERENCE"); _ = Decode(r.Utf8Base64Chunks, allowEmpty: true);
            if (r.Profile is not null)
            {
                // A profile-edit or construction recovery names its target explicitly; a rail
                // recovery (Profile null) keeps the RecoveryReference check above unchanged.
                Guard.Require(r.Rail is "upper" or "lower" or "insert" or "delete" or "fair" or "rebuild" && (uint)r.Assignment < (uint)definition.Assignments.Length, "DOC-REFERENCE");
                var profile = definition.Profiles[definition.Assignments[r.Assignment].Profile];
                Guard.Require(profile.Name == r.Profile, "DOC-REFERENCE");
                if (r.Rail is "upper" or "lower")
                {
                    var curve = r.Rail == "upper" ? profile.Upper : profile.Lower;
                    Guard.Require(curve.Ids.Contains(r.VertexId), "DOC-REFERENCE");
                }
            }
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
