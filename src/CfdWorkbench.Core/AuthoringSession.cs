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
public sealed record Envelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted, CursorRow[] Cursors, RecoveryRow? Recovery)
{
    /// <summary>The Analysis run facts (ADR-0011). Written only when a run exists, so a document with no run is
    /// <c>cfdw-project-1</c> byte for byte; non-positional, so every existing construction compiles unchanged.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AnalysisRecords? Analysis { get; init; }
}
public sealed record SessionDraft(string Id, string Base, long Generation, string Rail, string VertexId, byte[] Bytes, string? Profile = null, int Assignment = -1, ThicknessIntent Intent = ThicknessIntent.KeepCurrent, string? Rule = null, string? Curve = null);
public sealed record SessionBinding(string SourceHash, string Base, string DraftId, long Generation, string Evaluator, string SurfaceHash, string Rail, string VertexId);

public sealed record SessionView(string AcceptedId, string SourceHash, string SurfaceHash, byte[] Source, SessionDraft? Draft, RecoveryRow? Recovery, bool Dirty);
public sealed record SessionEvent(long Sequence, string Operation, string Outcome, double? DurationMilliseconds, int? InputBytes,
    int? OutputBytes, string? TraceId, long? Generation, string? Evaluator, int RetainedSources, int AcceptedFacts, string Action,
    bool? PublicationKnown = null, bool? DurabilityConfirmed = null, string? EditKind = null,
    double? FitMicrometres = null, double? DeviationMicrometres = null, double? ShiftMicrometres = null, bool? FitAboveLimit = null,
    int? Frames = null, string? CurveFamily = null, string? StepKind = null, int? Steps = null, bool? Independent = null,
    double? DeviationInCurveUnit = null, int? PointsBefore = null, int? PointsAfter = null)
{
    /// <summary>The Analysis fields of <c>analysis.*</c> events (design §11); null on every other event.</summary>
    public AnalysisEvent? Analysis { get; init; }
    /// <summary>The Replace fields of <c>catalog.preview</c> and of a <c>section.step</c> whose kind is replace (m12d §10); null otherwise.</summary>
    public ReplaceEvent? Replace { get; init; }
}
/// <summary>Scope is <c>draft</c> or <c>chain</c>; Spacing is <c>current</c>, <c>own-&lt;n&gt;</c> or <c>exact</c>; the residual is in chord fractions.</summary>
public sealed record ReplaceEvent(string Scope, int Stations, double ResidualChord, string Spacing);
public sealed record DimensionCommand(string Name, string Text);
public sealed record GestureFrame(SessionDraft Draft, double SpanMeters, double Ordinate, IReadOnlyList<string> MovedIds, bool Clamped);
public abstract record PointCommand(string Curve, string VertexId)
{
    public sealed record MakeAnchor(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record MakeControl(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record SetTangent(string Curve, string VertexId, TangentKind Kind, string? KeepHandleId) : PointCommand(Curve, VertexId);
    public sealed record AddPoint(string Curve, double Eta) : PointCommand(Curve, "");
    public sealed record RemovePoint(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record RebuildCurve(string Curve, int Count) : PointCommand(Curve, Curve);
}
public sealed record PointOutcome(string AcceptedId, double MaxDeviationMeters, int PointsBefore, int PointsAfter)
{
    public double AtEta { get; init; }
    public string? SelectId { get; init; }
    public string? Notice { get; init; }
}
public sealed record RebuildPreview(int Count, CurveView Curve, double MaxChange, double AtEta,
    int BreaksBefore, int BreaksAfter, double TipTurnDegrees, string? Refusal)
{
    public double AreaBeforeSquareMeters { get; init; }
    public double AreaAfterSquareMeters { get; init; }
}
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
            closed = true; events.Clear(); capturedSaveHashes.Clear(); retiredDraftIds.Clear();
            sources.Clear(); designs.Clear(); accepted.Clear(); cursors.Clear(); redo.Clear(); operations.Clear();
            draft = null; recovery = null; current = null; activeImportReport = null;
            section = null;
        }
    }
    private T Run<T>(string operation, Func<T> action, int? inputBytes = null, long? generation = null, string? editKind = null, string? stepKind = null)
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
                _ => operation.StartsWith("geometry.", StringComparison.Ordinal) || operation.StartsWith("section.", StringComparison.Ordinal)
                    ? operation : "document." + operation
            };
            bool superseded = operation == "section.assess" && outcome == "DSL-CANCELLED";
            Record(name, superseded ? "superseded" : outcome, superseded ? null : timer.Elapsed.TotalMilliseconds,
                inputBytes, null, generation, null, operation, editKind,
                stepKind: stepKind, independent: editKind == "section" ? SectionIndependent() : null);
            trace.Value = priorTrace;
        }
    }
    private string? pendingCurveFamily;
    private void Record(string operation, string outcome, double? elapsed, int? inputBytes, int? outputBytes, long? generation, string? evaluator, string? action = null, string? editKind = null, int? frames = null, string? curveFamily = null,
        string? stepKind = null, int? steps = null, bool? independent = null)
    {
        lock (sync)
        {
            double? fit = pendingFitUm;
            double? deviation = pendingDeviationUm;
            double? shift = pendingShiftUm;
            bool? above = pendingFitAboveLimit;
            string? family = curveFamily ?? pendingCurveFamily;
            if (curveFamily is null) pendingCurveFamily = null;
            pendingFitUm = pendingDeviationUm = pendingShiftUm = null;
            pendingFitAboveLimit = null;
            if (closed) return;
            if (events.Count == 256) events.Dequeue();
            double? deviationInUnit = pendingDeviationInUnit;
            int? pointsBefore = pendingPointsBefore;
            int? pointsAfter = pendingPointsAfter;
            ReplaceEvent? replace = pendingReplace;
            pendingReplace = null;
            pendingDeviationInUnit = null;
            pendingPointsBefore = pendingPointsAfter = null;
            events.Enqueue(new(eventSequence++, operation, outcome, elapsed, inputBytes, outputBytes, trace.Value, generation, evaluator, sources.Count, accepted.Count, action ?? operation, null, null, editKind, fit, deviation, shift, above, frames, family,
                stepKind, steps, independent, deviationInUnit, pointsBefore, pointsAfter) { Replace = replace });
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
        var timer = System.Diagnostics.Stopwatch.StartNew(); var result = Geometry.Assess(parsed, new ProofBudget(proofWorkLimit));
        Record("geometry.validate", result.Code, timer.Elapsed.TotalMilliseconds, parsed.Source.Length, null, generation, "cfdw-cv/2");
        return result;
    }
    public byte[] Open(byte[] source, string operationId, bool acceptIdInsertion) => Run("open", () => OpenCore(source, operationId, acceptIdInsertion), source.Length);
    public SessionDraft BeginPointGesture(string draftId, string curve, string vertexId) =>
        Run("begin", () => BeginPointGestureCore(draftId, curve, vertexId));
    public GestureFrame UpdatePointGesture(string draftId, long generation, double spanMeters, double ordinate) =>
        Run("update", () => UpdatePointGestureCore(draftId, generation, spanMeters, ordinate), 2 * sizeof(double), generation);
    public PointOutcome ApplyPointCommand(string operationId, PointCommand command) =>
        Run("apply", () => ApplyPointCommandCore(operationId, command), editKind: PointEditKind(command));
    /// <summary>Seven rebuild previews, counts 4 through 10. Does not certify and does not add a row.</summary>
    public IReadOnlyList<RebuildPreview> PreviewRebuilds(string curve)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        byte[] bytes;
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(PointModel.EditableCurves.Contains(curve), "DSL-TARGET");
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            bytes = CurrentBytes;
        }
        var parsed = FoilSource.Parse(bytes);
        Guard.Require(parsed.IsParsed && parsed.Definition is not null, "DSL-PATCH");
        var definition = parsed.Definition!;
        var original = definition.Curves[curve];
        bool mirror = definition.Locks.Any(item => item.Kind == "root_mirror" && item.Channel.Text == curve);
        int beforeBreaks = ChannelEdits.CurvatureBreaks(original);
        double beforeTip = ChannelEdits.TipAngleDegrees(original, definition.HalfSpan);
        double beforeArea = WingEstimates.From(bytes, "accepted", 0).AreaSquareMeters;
        var list = new List<RebuildPreview>();
        for (int count = ChannelEdits.Floor; count <= 10; count++)
        {
            try
            {
                var rebuilt = ChannelEdits.Rebuild(original, count, mirror);
                if (rebuilt.Identity)
                {
                    list.Add(new(count, Channels.View(bytes, curve, "Accepted", 0), 0, 0, beforeBreaks, beforeBreaks, 0, null)
                    {
                        AreaBeforeSquareMeters = beforeArea, AreaAfterSquareMeters = beforeArea
                    });
                    continue;
                }
                string? refusal = RailCrossing(definition, curve, rebuilt.Curve);
                var change = ChannelEdits.MaxChange(original, rebuilt.Curve);
                int afterBreaks = ChannelEdits.CurvatureBreaks(rebuilt.Curve);
                double turn = ChannelEdits.TipAngleDegrees(rebuilt.Curve, definition.HalfSpan) - beforeTip;
                byte[] candidate = PrintCurve(definition, curve, rebuilt.Curve);
                var view = Channels.View(candidate, curve, "Accepted", 0);
                double afterArea = WingEstimates.From(candidate, "preview", 0).AreaSquareMeters;
                list.Add(new(count, view, change.Max, change.AtEta, beforeBreaks, afterBreaks, turn, refusal)
                {
                    AreaBeforeSquareMeters = beforeArea, AreaAfterSquareMeters = afterArea
                });
            }
            catch (ContractError error)
            {
                list.Add(new(count, Channels.View(bytes, curve, "Accepted", 0), 0, 0, beforeBreaks, beforeBreaks, 0, error.Reason ?? error.Code)
                {
                    AreaBeforeSquareMeters = beforeArea, AreaAfterSquareMeters = beforeArea
                });
            }
        }
        Record("rebuild.preview", "OK", watch.Elapsed.TotalMilliseconds, bytes.Length, null, null, "cfdw-cv/2", "rebuild.preview", curveFamily: Channels.Family(curve));
        return list;
    }
    private static string PointEditKind(PointCommand? command) => command switch
    {
        PointCommand.SetTangent => "tangent-kind",
        PointCommand.AddPoint => "point-add",
        PointCommand.RemovePoint => "point-remove",
        PointCommand.RebuildCurve => "curve-rebuild",
        _ => "point-type"
    };
    public ProfileView ProfileAt(int assignmentIndex) => Run("profile", () => ProfileAtCore(assignmentIndex));
    public ScopeImpact DescribeScope(string profile, int assignmentIndex, SectionScope scope) => Run("scope", () => DescribeScopeCore(profile, assignmentIndex, scope));
    public SectionDraftView BeginSectionDraft(string draftId, int assignmentIndex) =>
        Run("section.begin", () => BeginSectionDraftCore(draftId, assignmentIndex), editKind: "section");
    /// <summary>Reads the open section draft, including recovered cursor-zero bytes.</summary>
    public SectionDraftView? CurrentSectionDraft() => Run("section.view", () =>
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            if (draft is null || draft.Rail != "section") return null;
            RequireSection(draft.Id);
            return SectionView();
        }
    }, editKind: "section");
    /// <summary>Appends one step at the cursor (dropping any redo tail). Refuses a step that does not parse or pass structure; the draft is then unchanged.</summary>
    public SectionDraftView ApplySectionStep(string draftId, long generation, SectionStep step) =>
        Run("section.step", () => ApplySectionStepCore(draftId, generation, step), generation: generation, editKind: "section", stepKind: SectionStepKind(step));
    /// <summary>
    /// Previews a Replace over the draft's bytes at this generation (m12d §5.3); nothing changes. Emits <c>catalog.preview</c>
    /// with the spacing, points, fit and outcome (§10). Applying it is <see cref="ApplySectionStep"/> with the same generation,
    /// so a stale preview is refused.
    /// </summary>
    public ReplacePreview PreviewReplace(string draftId, long generation, ReplaceSource source, ReplaceScope scope)
    {
        byte[] bytes;
        int assignment;
        SectionScope draftScope;
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            RequireSection(draftId);
            Guard.Require(draft!.Generation == generation, "DSL-CONFLICT");
            var view = SectionView();
            (bytes, assignment, draftScope) = (view.Bytes, view.Assignment, view.Scope);
        }
        var timer = System.Diagnostics.Stopwatch.StartNew();
        ReplacePreview preview;
        try { preview = SectionReplace.Preview(bytes, assignment, draftScope, source, scope); }
        catch (ContractError error)
        {
            Record("catalog.preview", error.Code, timer.Elapsed.TotalMilliseconds, bytes.Length, null, generation, "cfdw-cv/2", editKind: "section");
            throw;
        }
        lock (sync)
        {
            pendingFitUm = preview.FitResidual * preview.AcceptanceChord * 1e6;
            pendingFitAboveLimit = preview.RefusalCode is not null;
            pendingPointsAfter = preview.PointsPerSurface;
            pendingReplace = new(scope == ReplaceScope.BlendChain ? "chain" : "draft", preview.Stations.Count, preview.FitResidual, preview.Spacing);
        }
        Record("catalog.preview", preview.RefusalCode?.ToLowerInvariant() ?? "ok", timer.Elapsed.TotalMilliseconds, bytes.Length, null, generation,
            "cfdw-cv/2", editKind: "section", stepKind: preview.Spacing);
        return preview;
    }
    /// <summary>Moves the cursor back one step; a no-op at cursor 0. Never document undo.</summary>
    public SectionDraftView UndoSectionStep(string draftId) => Run("section.undo", () => MoveSectionCursor(draftId, -1), editKind: "section");
    /// <summary>Moves the cursor forward one step; a no-op at the end.</summary>
    public SectionDraftView RedoSectionStep(string draftId) => Run("section.redo", () => MoveSectionCursor(draftId, 1), editKind: "section");
    /// <summary>The full certificate of the bytes at the cursor. Finish availability comes only from this result.</summary>
    public SessionAssessment AssessSection(string draftId, long generation, CancellationToken cancellation) =>
        Run("section.assess", () => AssessSectionCore(draftId, generation, cancellation), generation: generation, editKind: "section");
    /// <summary>One accepted row for the whole draft. Null when the cursor bytes equal the base bytes, which acts as Cancel.</summary>
    public string? FinishSection(string operationId, SessionAssessment assessment) =>
        Run("apply", () => FinishSectionCore(operationId, assessment), generation: assessment.Key?.Generation, editKind: "section");
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
    private readonly long? proofWorkLimit;
    public AuthoringSession(int envelopeCap = NativeProject.MaxBytes)
    {
        Guard.Require(envelopeCap > 0 && envelopeCap <= NativeProject.MaxBytes, "DOC-SIZE");
        this.envelopeCap = envelopeCap;
    }
    private AuthoringSession(long proofWorkLimit) : this() => this.proofWorkLimit = proofWorkLimit;
    // Test seam only: every geometry assessment in this session runs under the given work limit
    // (bit-work units, at most ProofBudget.DefaultWorkLimit), so a budget refusal (GEOMETRY-BUDGET)
    // is reproduced without a pathological source. No public member exposes this.
    internal static AuthoringSession WithProofWorkLimit(long workLimit)
    {
        Guard.Require(workLimit >= 0 && workLimit <= ProofBudget.DefaultWorkLimit, "DSL-RANGE");
        return new AuthoringSession(workLimit);
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
    double? pendingFitUm, pendingDeviationUm, pendingShiftUm, pendingDeviationInUnit;
    int? pendingPointsBefore, pendingPointsAfter;
    ReplaceEvent? pendingReplace;
    bool? pendingFitAboveLimit;
    RecoveryRow? recovery;
    ImportReport? activeImportReport;
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
    // cooperative work limit must not refuse the whole session — the revision is simply
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
        var prospective = new Envelope(RunRecord.Format(runs.Count), projectId, nextSources.ToArray(), nextDesigns.ToArray(), [.. accepted, row], [.. cursors, cursor], null) { Analysis = AnalysisCore() };
        NativeProject.Preflight(prospective, envelopeCap);
        designs.Clear(); designs.AddRange(nextDesigns); sources.Clear(); sources.AddRange(nextSources);
        accepted.Add(row); current = id; cursors.Add(cursor); redo.Clear(); return id;
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
            var point = Channels.View(CurrentBytes, curve, "Accepted", 0).Points[index];
            Guard.Require(point.Freedom != PointFreedom.Fixed, "DSL-LOCK");
            RequireAdmission(parsed, new(parsed.SourceHash, current!, draftId, 0, "cfdw-cv/2", parsed.SurfaceHash!, curve, vertexId), toleratesBudget: false);
            retiredDraftIds.Add(draftId);
            draft = new(draftId, current!, 0, curve, vertexId, CurrentBytes);
            gestureDraftId = draftId; gestureFrames = 0; gestureStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            return Copy(draft);
        }
    }

    private GestureFrame UpdatePointGestureCore(string draftId, long generation, double spanMeters, double ordinate)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft?.Id == draftId && draft.Generation == generation && gestureDraftId == draftId, "DSL-CONFLICT");
            var rail = Channels.View(BaseBytes(draft!.Base), draft.Rail, "Accepted", 0);
            int grabbed = rail.Points.ToList().FindIndex(point => point.Id == draft.VertexId);
            Guard.Require(grabbed >= 0, "DSL-TARGET");
            var selected = rail.Points[grabbed];
            GestureFrame LastFrame()
            {
                var currentPoint = Channels.View(draft.Bytes, draft.Rail, "Draft", draft.Generation).Points[grabbed];
                return new(Copy(draft), currentPoint.SpanMeters, currentPoint.Ordinate, [], true);
            }
            if (!double.IsFinite(spanMeters) || !double.IsFinite(ordinate))
                return LastFrame();
            if (selected.Freedom == PointFreedom.Fixed) { spanMeters = selected.SpanMeters; ordinate = selected.Ordinate; }
            else if (selected.Freedom == PointFreedom.ValueOnly) spanMeters = selected.SpanMeters;
            else if (selected.Freedom == PointFreedom.SpanOnly) ordinate = selected.Ordinate;
            double halfSpan = rail.Points[^1].SpanMeters / rail.Points[^1].Eta;
            double rawEta = (spanMeters - selected.SpanMeters) / halfSpan;
            double rawOrdinate = ordinate - selected.Ordinate;
            double deltaEta = Math.Round(rawEta, 7, MidpointRounding.ToEven);
            double deltaOrdinate = QuantizedOrdinate(draft.Rail, rawOrdinate);
            // Scale overflow (double.MaxValue × the quantum) is the same refusal as a non-finite request.
            if (!double.IsFinite(deltaEta) || !double.IsFinite(deltaOrdinate)) return LastFrame();
            var moved = new Dictionary<int, (double Eta, double Aft)>();
            void Add(int index, double eta, double aft) => moved[index] = (eta, aft);
            Add(grabbed, selected.Eta + deltaEta, selected.Ordinate + deltaOrdinate);
            if (selected.Role == PointRole.Anchor)
            {
                foreach (int index in new[] { grabbed - 1, grabbed + 1 })
                    Add(index, rail.Points[index].Eta + deltaEta, rail.Points[index].Ordinate + deltaOrdinate);
            }
            else if (selected.Role == PointRole.RootEnd && selected.Locks.Contains("root_mirror"))
                Add(1, rail.Points[1].Eta, rail.Points[1].Ordinate + deltaOrdinate);
            else if (selected.Role == PointRole.AnchorHandle)
            {
                int anchor = rail.Points.ToList().FindIndex(point => point.Id == selected.AnchorId);
                int opposite = 2 * anchor - grabbed;
                var a = rail.Points[anchor]; var h = moved[grabbed]; var old = rail.Points[opposite];
                if (a.Kind == TangentKind.Symmetric)
                    Add(opposite, 2 * a.Eta - h.Eta, 2 * a.Ordinate - h.Aft);
                else if (a.Kind == TangentKind.Smooth)
                {
                    double ratio = (old.Eta - a.Eta) / (h.Eta - a.Eta);
                    Add(opposite, old.Eta, a.Ordinate + ratio * (h.Aft - a.Ordinate));
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
                    return new(Copy(draft), selected.SpanMeters, selected.Ordinate, [], true);
                double eta = Math.Clamp(moved[grabbed].Eta, bounds.Min, bounds.Max);
                clamped = eta != moved[grabbed].Eta;
                moved[grabbed] = (eta, moved[grabbed].Aft);
                if (a.Kind == TangentKind.Symmetric)
                    moved[opposite] = (2 * a.Eta - eta, 2 * a.Ordinate - moved[grabbed].Aft);
                else if (a.Kind == TangentKind.Smooth)
                {
                    double slope = (moved[grabbed].Aft - a.Ordinate) / (eta - a.Eta);
                    moved[opposite] = (rail.Points[opposite].Eta,
                        a.Ordinate + slope * (rail.Points[opposite].Eta - a.Eta));
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
                if (shiftMin > shiftMax) return new(Copy(draft), selected.SpanMeters, selected.Ordinate, [], true);
                double shift = Math.Clamp(0, shiftMin, shiftMax);
                if (shift != 0) clamped = true;
                foreach (int index in moved.Keys.ToArray()) moved[index] = (moved[index].Eta + shift, moved[index].Aft);
            }
            var unit = Channels.Unit(draft.Rail);
            if (unit.DomainLower is double lower && unit.DomainUpper is double upper)
            {
                foreach (int index in moved.Keys.ToArray())
                {
                    double next = ClampGrowing(rail.Points[index].Ordinate, moved[index].Aft, lower, upper);
                    if (next != moved[index].Aft) clamped = true;
                    moved[index] = (moved[index].Eta, next);
                }
            }
            var baseParsed = ParseOwned(BaseBytes(draft.Base));
            byte[] patched = PatchGesture(baseParsed, draft.Rail, moved);
            draft = draft with { Generation = generation + 1, Bytes = patched };
            gestureFrames++;
            var resolved = moved[grabbed];
            return new(Copy(draft), resolved.Eta * halfSpan, resolved.Aft, moved.Keys.Order().Select(index => rail.Points[index].Id).ToArray(), clamped);
        }
    }

    private static double QuantizedOrdinate(string curve, double delta)
    {
        double scale = curve == "twist" ? 1e5 : curve == "thickness" ? 1e7 : 1e6;
        return Math.Round(delta * scale, 0, MidpointRounding.ToEven) / scale;
    }

    private static double ClampGrowing(double original, double proposed, double lower, double upper)
    {
        if (proposed >= lower && proposed <= upper) return proposed;
        if (original >= lower && original <= upper) return proposed > upper ? upper : lower;
        if (original > upper) return proposed > original ? original : proposed;
        if (original < lower) return proposed < original ? original : proposed;
        return proposed;
    }

    internal static byte[] PatchGesture(SourceParse parsed, string curveName, IReadOnlyDictionary<int, (double Eta, double Aft)> moved)
    {
        var definition = parsed.Definition!; var curve = definition.Curves[curveName];
        string text = FoilSource.Utf8.GetString(parsed.Source);
        var edits = new List<(int Start, int End, string Value)>();
        foreach (var (index, value) in moved)
        {
            edits.Add((curve.Abscissae[index].Start, curve.Abscissae[index].End, FoilSource.ExactDecimal(value.Eta)));
            int scale = curveName is "twist" or "thickness" ? 0 : definition.UnitScale;
            edits.Add((curve.Ordinates[index].Start, curve.Ordinates[index].End, FoilSource.ExactDecimal(value.Aft, scale)));
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
    // Display samples, cosine-spaced at the nose. Validity stays with Geometry.Assess.
    // The optional budget is not charged: this path is not the certificate.
    internal static ProfilePoint[] Sample(Curve curve, ProofBudget? watch = null)
    {
        _ = watch;
        return ProfileEvaluator.Samples(curve, 101);
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
    private static Diagnostic ThicknessDiagnostic(SessionDraft capture, string fault) => new(fault, "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, "thickness",
        fault == "DSL-LOCK" ? "A thickness lock contradicts the source-thickness target." : "The thickness fit is singular or its residual exceeds 1e-9.",
        "Keep the current thickness or relax the lock.");
    private SessionAssessment ValidateCore(string draftId, long generation, CancellationToken cancellation = default)
    {
        SessionDraft capture;
        byte[] baseline;
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == generation, "DSL-CONFLICT");
            Guard.Require(Interlocked.CompareExchange(ref validating, 1, 0) == 0, "DSL-VALIDATION-BUSY");
            capture = Copy(draft!);
            baseline = CurrentBytes;
        }
        try
        {
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null, DraftBinding(capture), null, importReport: activeImportReport);
            var timer = System.Diagnostics.Stopwatch.StartNew(); var parsed = FoilSource.Parse(capture.Bytes);
            Record("language.parse", parsed.IsParsed ? "OK" : parsed.Diagnostics[0].Code, timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, parsed.IsParsed ? "cfdw-cv/2" : null);
            if (!parsed.IsParsed) return new(authorityId, parsed.Diagnostics[0].Code == "DSL-LIMIT" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid,
                parsed.Diagnostics[0].Code, null, null, DraftBinding(capture, parsed), parsed.Diagnostics, importReport: activeImportReport);
            timer.Restart(); var key = Key(parsed, capture);
            Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, "cfdw-cv/2");
            ThicknessFit.View? thickness = capture.Intent == ThicknessIntent.UseSource && capture.Profile is not null
                ? ThicknessFit.Describe(capture.Bytes, capture.Profile, baseline) : null;
            if (thickness?.Fault is string fault)
                return new(authorityId, fault == "DSL-LOCK" ? GeometryStatus.Invalid : GeometryStatus.NotAssessed, fault, key, null,
                    DraftBinding(capture, parsed), [ThicknessDiagnostic(capture, fault)], thickness: thickness.Value.Proposal);
            var result = AssessOwned(parsed, generation);
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", key, null, DraftBinding(capture, parsed), importReport: activeImportReport);
            GeometryStatus status = result.Status; string code = result.Code; string reason = result.Reason;
            var diagnostics = new List<Diagnostic>();
            if (status != GeometryStatus.Certified)
                diagnostics.Add(new(code, "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, capture.Rail, reason, "Revise the authored curves or retain the last accepted revision."));
            return new(authorityId, status, code, key, status == GeometryStatus.Certified ? result.Certificate : null, DraftBinding(capture, parsed), diagnostics, thickness: thickness?.Proposal, importReport: activeImportReport);
        }
        catch (ContractError error)
        { return new(authorityId, error.Code is "DSL-LIMIT" or "DSL-UNSUPPORTED" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid, error.Code, null, null, DraftBinding(capture), importReport: activeImportReport); }
        finally { Interlocked.Exchange(ref validating, 0); }
    }
    private byte[] BaseBytes(string acceptedId)
    {
        var row = accepted.Single(item => item.Id == acceptedId);
        return Decode(sources.Single(item => item.Id == row.SourceId).Utf8Base64Chunks);
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
            pendingCurveFamily = Channels.Family(command!.Curve);
            string kind = PointEditKind(command);
            bool replay = operations.ContainsKey(operationId);
            Guard.Require(replay || current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            var priorOperation = replay ? accepted.SingleOrDefault(row => row.OperationId == operationId) : null;
            if (replay && priorOperation?.Edit?.Rail != kind)
                throw new ContractError("DOC-OPERATION-CONFLICT");
            string? parent = replay ? priorOperation!.Parent : current;
            byte[] basis = replay ? BaseBytes(parent!) : CurrentBytes;
            var prior = ParseOwned(basis);
            var oldCurve = prior.Definition!.Curves[command.Curve];
            int index = command is PointCommand.AddPoint or PointCommand.RebuildCurve ? -1 : Array.IndexOf(oldCurve.Ids, command.VertexId);
            if (command is PointCommand.RemovePoint)
            {
                Guard.Require(index >= 0, "DSL-TARGET");
                var refusal = ChannelEdits.RemoveRefusal(Channels.View(basis, command.Curve, "Accepted", 0), command.VertexId);
                if (refusal is not null) throw new ContractError(refusal.Value.Code, refusal.Value.Reason);
            }
            else if (command is not (PointCommand.AddPoint or PointCommand.RebuildCurve))
                Guard.Require(index >= 0, "DSL-TARGET");
            PointPatch patch;
            try { patch = EvaluatePointCommand(prior, command, index, basis); }
            catch (ContractError) when (replay) { throw new ContractError("DOC-OPERATION-CONFLICT"); }
            byte[] bytes = patch.Bytes;
            bool identity = patch.Identity || bytes.AsSpan().SequenceEqual(basis);
            var next = ParseOwned(bytes);
            string vertex = command switch
            {
                PointCommand.AddPoint => patch.SelectId ?? "",
                PointCommand.RebuildCurve => command.Curve,
                _ => command.VertexId
            };
            var receipt = new EditReceipt(operationId, 0, kind, vertex, Curve: command.Curve);
            string payload = Fingerprint(receipt, parent, next.SourceHash, command.Curve);
            int before = oldCurve.Points.Length;
            int after = identity ? before : next.Definition!.Curves[command.Curve].Points.Length;
            double deviation = identity ? 0 : MaxPointDelta(basis, bytes, command.Curve);
            PointOutcome Done(string id) => FinishPoint(kind, id, deviation, before, after, patch);
            if (Retry(operationId, payload, out string existing))
                return Done(existing);
            if (identity)
            {
                operations.Add(operationId, (payload, current!));
                return Done(current!);
            }
            Guard.Require(!retiredDraftIds.Contains(operationId), "DSL-DRAFT-REUSED");
            retiredDraftIds.Add(operationId);
            draft = new(operationId, current!, 0, kind, vertex, bytes, Curve: command.Curve);
            try
            {
                RequireAdmission(next, Key(next, draft));
                string id = Commit(next, operationId, "apply");
                operations.Add(operationId, (payload, id));
                draft = null; recovery = null;
                return Done(id);
            }
            catch
            {
                draft = null; retiredDraftIds.Remove(operationId); throw;
            }
        }
    }

    private readonly record struct PointPatch(byte[] Bytes, string? SelectId, string? Notice, double AtEta, bool Identity);

    private PointOutcome FinishPoint(string kind, string id, double deviation, int before, int after, PointPatch patch)
    {
        if (kind is "point-add" or "point-remove" or "curve-rebuild")
        {
            pendingDeviationInUnit = deviation;
            pendingPointsBefore = before;
            pendingPointsAfter = after;
        }
        return new PointOutcome(id, deviation, before, after) { AtEta = patch.AtEta, SelectId = patch.SelectId, Notice = patch.Notice };
    }

    private static double MaxPointDelta(byte[] before, byte[] after, string curve)
    {
        var first = FoilSource.Parse(before).Definition!.Curves[curve];
        var second = FoilSource.Parse(after).Definition!.Curves[curve];
        return ChannelEdits.MaxChange(first, second).Max;
    }

    private static PointPatch EvaluatePointCommand(SourceParse parsed, PointCommand command, int index, byte[] basis)
    {
        var definition = parsed.Definition!;
        var original = definition.Curves[command.Curve];
        bool mirror = definition.Locks.Any(item => item.Kind == "root_mirror" && item.Channel.Text == command.Curve);
        if (command is PointCommand.AddPoint add)
        {
            var added = ChannelEdits.Add(original, add.Eta);
            byte[] printed = PrintCurve(definition, add.Curve, added.Curve);
            return new(printed, added.Curve.Ids[added.Index], added.Notice, add.Eta, false);
        }
        if (command is PointCommand.RemovePoint)
        {
            var removed = ChannelEdits.Remove(original, index, mirror);
            byte[] printed = PrintCurve(definition, command.Curve, removed.Curve);
            return new(printed, removed.Curve.Ids[removed.SelectIndex], removed.Notice, original.Points[index][0], false);
        }
        if (command is PointCommand.RebuildCurve rebuild)
        {
            var result = ChannelEdits.Rebuild(original, rebuild.Count, mirror);
            if (result.Identity) return new(basis, null, null, 0, true);
            string? crossing = RailCrossing(definition, rebuild.Curve, result.Curve);
            if (crossing is not null) throw new ContractError("DSL-GEOMETRY", crossing);
            byte[] printed = PrintCurve(definition, rebuild.Curve, result.Curve);
            double at = ChannelEdits.MaxChange(original, result.Curve).AtEta;
            return new(printed, null, result.Notice, at, false);
        }
        var point = Channels.View(parsed.Source, command.Curve, "Accepted", 0).Points[index];
        Curve changed = command switch
        {
            PointCommand.MakeAnchor => MakeAnchor(original, index, point),
            PointCommand.MakeControl => MakeControl(original, index, point),
            PointCommand.SetTangent tangent => SetTangent(original, index, point, tangent),
            _ => throw new ContractError("DSL-TARGET")
        };
        byte[] document = PrintCurve(definition, command.Curve, changed);
        return new(document, null, null, 0, false);
    }

    private static byte[] PrintCurve(Definition definition, string curve, Curve changed)
    {
        var curves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal) { [curve] = changed };
        byte[] printed = FoilSource.Print(definition with { Curves = curves });
        if (changed.Tangents.Length > 0 || changed.Points.Length > 10 || changed.Points.Length < 6)
            printed = FoilSource.EnsureHeader41(printed);
        var check = FoilSource.Parse(printed);
        Guard.Require(check.IsParsed, check.Diagnostics.Count == 0 ? "DSL-PATCH" : check.Diagnostics[0].Code);
        return printed;
    }

    private static string? RailCrossing(Definition definition, string curve, Curve changed)
    {
        if (curve is not ("leading" or "trailing")) return null;
        var leading = curve == "leading" ? changed : definition.Curves["leading"];
        var trailing = curve == "trailing" ? changed : definition.Curves["trailing"];
        if (!ChannelEdits.RailsCross(leading, trailing, out double at)) return null;
        return ChannelEdits.CrossingCopy(changed.Points.Length, at * definition.HalfSpan * 1000);
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
            var (nextKnots, nextPoints, inserted) = FoilSource.InsertOnce(knots, points, curve.Degree, knot);
            var nextIds = ids.ToList();
            nextIds.Insert(inserted, "cv-" + suffix++);
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
        double shift = point.Ordinate - aft;
        for (int i = anchor - 1; i <= anchor + 1; i++) points[i][1] += shift;
        for (int i = 1; i < points.Length; i++)
            Guard.Require((points[i][0] - points[i - 1][0]) * 1 >= 1e-7 - 1e-12, "DSL-CURVE");
        return curve with { Knots = knots, Points = points, Ids = ids,
            Tangents = [.. curve.Tangents, new TangentRow(point.Id, "smooth", null)] };
    }

    private static Curve MakeControl(Curve curve, int index, PointView point)
    {
        Guard.Require(point.Role == PointRole.Anchor, "DSL-LOCK");
        Guard.Require(curve.Points.Length >= 7, "DSL-CURVE");
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
            string? family = Channels.Family(draft.Curve ?? draft.Rail);
            pendingCurveFamily = family;
            string id = Commit(p, operationId, "apply"); operations.Add(operationId, (payload, id)); draft = null; recovery = null; activeImportReport = null; section = null;
            if (gesture) { Record("gesture.end", "OK", System.Diagnostics.Stopwatch.GetElapsedTime(gestureStarted).TotalMilliseconds, null, null, assessment.Key!.Generation, "cfdw-cv/2", frames: gestureFrames, curveFamily: family); gestureDraftId = null; gestureFrames = 0; }
            return id;
        }
    }
    private void CancelCore(string draftId)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft?.Id == draftId, "DSL-CONFLICT");
            string? family = gestureDraftId == draftId ? Channels.Family(draft!.Rail) : null;
            if (section is not null && draft!.Rail == "section") EndSection("section.cancel", "OK", null);
            draft = null; recovery = null; activeImportReport = null;
            if (gestureDraftId == draftId) { Record("gesture.end", "NoChange", System.Diagnostics.Stopwatch.GetElapsedTime(gestureStarted).TotalMilliseconds, null, null, null, "cfdw-cv/2", frames: gestureFrames, curveFamily: family); gestureDraftId = null; gestureFrames = 0; }
        }
    }
    // Section draft (ADR-0007 with Amendment 1; m12c-section-editor.md §3.2–§3.3, §5.1, §10). Command + Memento-by-bytes
    // with a cursor: Steps[0] holds the entry bytes and Steps[i] the bytes after step i. The session draft always carries
    // the cursor's bytes, so Assess, Finish, Snapshot and recovery read one place. simplify: a full byte copy per step;
    // ceiling and upgrade trigger: a draft measured over 64 MB.
    private sealed record SectionMemento(byte[] Bytes, string Profile, ThicknessIntent Intent, SectionStepReport? Report);
    private sealed class SectionState(string entryProfile, long began)
    {
        /// <summary>The base profile at the assignment at entry; a section recovery names it (§3.3).</summary>
        internal string EntryProfile { get; } = entryProfile;
        internal long Began { get; } = began;
        internal List<SectionMemento> Steps { get; } = [];
        internal int Cursor { get; set; }
    }
    private SectionState? section;
    private const string ProfileChangeOracle = "FoilSource.MaxOrdinateDeviation";

    private static string SectionStepKind(SectionStep? step) => step switch
    {
        SectionStep.Move => "move",
        SectionStep.SetType => "set-type",
        SectionStep.InsertAnchor => "insert-anchor",
        SectionStep.SetTangent => "set-tangent",
        SectionStep.Insert => "insert",
        SectionStep.Delete => "delete",
        SectionStep.Fair => "fair",
        SectionStep.Rebuild => "rebuild",
        SectionStep.Import or SectionStep.Replace => "replace",
        SectionStep.MakeUnique => "make-unique",
        SectionStep.Thickness => "thickness",
        _ => "unknown"
    };

    private bool? SectionIndependent() { lock (sync) return section is null || draft is null ? null : draft.Profile != section.EntryProfile; }

    private void StartSection(string entryProfile, byte[] bytes, string profile, ThicknessIntent intent)
    {
        section = new SectionState(entryProfile, System.Diagnostics.Stopwatch.GetTimestamp());
        section.Steps.Add(new(bytes.ToArray(), profile, intent, null));
        activeImportReport = null;
    }

    // Closes the open section draft with its event: the steps at the cursor and the milliseconds since it began.
    private void EndSection(string operation, string outcome, long? generation)
    {
        var state = section!;
        Record(operation, outcome, System.Diagnostics.Stopwatch.GetElapsedTime(state.Began).TotalMilliseconds, null, null, generation, "cfdw-cv/2",
            operation, "section", steps: state.Cursor, independent: draft?.Profile != state.EntryProfile);
        section = null;
    }

    private static string SectionProfileName(byte[] bytes, int assignment)
    {
        var definition = SessionSource.Parse(bytes).Definition!;
        Guard.Require((uint)assignment < (uint)definition.Assignments.Length, "DSL-PROFILE-TARGET");
        return definition.Profiles[definition.Assignments[assignment].Profile].Name;
    }

    private SectionDraftView BeginSectionDraftCore(string draftId, int assignmentIndex)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            NativeProject.Uuid(draftId);
            Guard.Require(current is not null && draft is null && recovery is null, "DSL-DRAFT-OWNED");
            Guard.Require(!retiredDraftIds.Contains(draftId), "DSL-DRAFT-REUSED");
            byte[] bytes = CurrentBytes;
            string profile = SectionProfileName(bytes, assignmentIndex);
            retiredDraftIds.Add(draftId);
            draft = new(draftId, current!, 0, "section", profile, bytes, profile, assignmentIndex);
            StartSection(profile, bytes, profile, ThicknessIntent.KeepCurrent);
            return SectionView();
        }
    }

    // The open section draft with this id. Legacy profile recoveries are converted on ResumeRecovery.
    private SectionState RequireSection(string draftId)
    {
        Guard.Require(draft is not null && draft.Id == draftId, "DSL-CONFLICT");
        Guard.Require(section is not null && draft!.Rail == "section", "DSL-CONFLICT");
        return section!;
    }

    private SectionDraftView SectionView()
    {
        var state = section!;
        var at = state.Steps[state.Cursor];
        return new(draft!.Id, draft.Base, draft.Assignment, at.Profile, at.Profile == state.EntryProfile ? SectionScope.Shared : SectionScope.Independent,
            at.Intent, draft.Generation, state.Cursor, state.Steps.Count - 1, draft.Bytes.ToArray(), at.Report);
    }

    // Points the draft at the cursor's memento under a new generation, so an assessment of other bytes no longer matches.
    private void SyncSectionDraft(SectionState state, long generation)
    {
        var at = state.Steps[state.Cursor];
        draft = draft! with { Generation = generation, Bytes = at.Bytes.ToArray(), VertexId = at.Profile, Profile = at.Profile, Intent = at.Intent };
        var imported = state.Steps.Take(state.Cursor + 1).LastOrDefault(item => item.Report?.Import is not null);
        activeImportReport = imported?.Report!.Import;
    }

    private SectionDraftView ApplySectionStepCore(string draftId, long generation, SectionStep step)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            var state = RequireSection(draftId);
            Guard.Require(draft!.Generation == generation && generation < 9007199254740991, "DSL-CONFLICT");
            Guard.Require(step is not null, "DSL-TARGET");
            var next = PatchSectionStep(draft.Bytes, state.Steps[state.Cursor].Intent, draft.Assignment, step!, BaseBytes(draft.Base));
            _ = ParseOwned(next.Bytes);
            state.Steps.RemoveRange(state.Cursor + 1, state.Steps.Count - state.Cursor - 1);
            state.Steps.Add(next);
            state.Cursor++;
            if (next.Report?.Import is { } replaced)
                pendingReplace = new(step is SectionStep.Replace { Scope: ReplaceScope.BlendChain } ? "chain" : "draft", replaced.Stations?.Count ?? 0,
                    replaced.MaxResidual, replaced.Basis == "own" ? "own-" + replaced.VertexCount.ToString(CultureInfo.InvariantCulture) : replaced.Basis ?? "");
            SyncSectionDraft(state, generation + 1);
            return SectionView();
        }
    }

    private SectionDraftView MoveSectionCursor(string draftId, int delta)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            var state = RequireSection(draftId);
            int target = state.Cursor + delta;
            if (target < 0 || target >= state.Steps.Count) return SectionView();
            state.Cursor = target;
            SyncSectionDraft(state, draft!.Generation + 1);
            return SectionView();
        }
    }

    private SessionAssessment AssessSectionCore(string draftId, long generation, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null);
        try
        {
            lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); RequireSection(draftId); }
            return ValidateCore(draftId, generation, cancellation);
        }
        catch (ContractError) when (cancellation.IsCancellationRequested)
        {
            return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null);
        }
    }

    private string? FinishSectionCore(string operationId, SessionAssessment assessment)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(assessment is not null, "DSL-NOT-ASSESSED");
            if (Retry(operationId, "apply:" + JsonSerializer.Serialize(assessment!.Key), out string prior)) return prior;
            Guard.Require(draft is not null, "DSL-CONFLICT");
            var state = RequireSection(draft!.Id);
            // Grain (§3.3): one accepted row per Finish whose cursor bytes differ from the base bytes; equal bytes act as Cancel.
            if (draft.Bytes.AsSpan().SequenceEqual(BaseBytes(draft.Base)))
            {
                EndSection("section.finish", "NoChange", draft.Generation);
                draft = null; recovery = null; activeImportReport = null;
                return null;
            }
            int steps = state.Cursor;
            bool independent = draft.Profile != state.EntryProfile;
            string id = ApplyCore(operationId, assessment);
            Record("section.finish", "OK", System.Diagnostics.Stopwatch.GetElapsedTime(state.Began).TotalMilliseconds, null, null, assessment.Key!.Generation,
                "cfdw-cv/2", "section.finish", "section", steps: steps, independent: independent);
            return id;
        }
    }

    // One step's pure byte patch over the cursor bytes, then the thickness refit its intent asks for. Shared-basis kinds use
    // the as-built FoilSource patches (fixtures are shared-basis until GCRT); the type kinds and per-surface Fair/Rebuild
    // dispatch to SectionEdits, whose body SPT owns (seam S-8).
    private static SectionMemento PatchSectionStep(byte[] bytes, ThicknessIntent intent, int assignment, SectionStep step, byte[] baseBytes)
    {
        var definition = SessionSource.Parse(bytes).Definition!;
        Guard.Require((uint)assignment < (uint)definition.Assignments.Length, "DSL-PROFILE-TARGET");
        var prior = definition.Profiles[definition.Assignments[assignment].Profile];
        byte[] next;
        double? achieved = null;
        ImportReport? import = null;
        SectionStepReport? delegated = null;
        switch (step)
        {
            case SectionStep.Move move:
                next = MoveSectionPoint(bytes, prior, move);
                break;
            case SectionStep.Insert insert:
                Guard.Require(double.IsFinite(insert.X) && insert.X > 0 && insert.X < 1, "DSL-PROFILE-TARGET");
                if (Surface(prior, insert.Side).Points.Length >= 32) throw new ContractError("DSL-CURVE", "A surface holds at most 32 points.");
                next = FoilSource.InsertProfileKnot(bytes, prior.Name, insert.X).Source;
                break;
            case SectionStep.Delete delete:
            {
                var curve = Surface(prior, delete.Side);
                int index = Array.IndexOf(curve.Ids, delete.VertexId);
                Guard.Require(index >= 0, "DSL-PROFILE-TARGET");
                Guard.Require(index != 0 && index != curve.Points.Length - 1, "DSL-LOCK");
                if (curve.Points.Length <= 7) throw new ContractError("DSL-CURVE", "Delete would leave fewer than p + 2 = 7 vertices");
                next = FoilSource.DeleteProfileVertex(bytes, prior.Name, index).Source;
                break;
            }
            case SectionStep.Fair { Side: null } fair:
            {
                var (patched, result) = FoilSource.FairProfile(bytes, prior.Name, fair.Tolerance, fair.Ends);
                RequireFair(result);
                (next, achieved) = (patched, result.MaxDeviation);
                break;
            }
            case SectionStep.Rebuild { Side: null } rebuild:
            {
                var (patched, result) = FoilSource.RebuildProfile(bytes, prior.Name, rebuild.VertexCount, rebuild.Tolerance, rebuild.Ends);
                RequireFair(result);
                (next, achieved) = (patched, result.MaxDeviation);
                break;
            }
            case SectionStep.Import importStep:
                (next, import) = SectionReplace.Patch(bytes, assignment, ScopeOf(definition, assignment), SectionReplace.FromDat(importStep.Dat), step);
                break;
            case SectionStep.Replace replace:
                (next, import) = SectionReplace.Patch(bytes, assignment, ScopeOf(definition, assignment), replace, step);
                break;
            case SectionStep.MakeUnique:
                Guard.Require(definition.Assignments.Count(item => item.Profile == definition.Assignments[assignment].Profile) > 1, "DSL-PROFILE-TARGET");
                next = FoilSource.MakeIndependent(bytes, prior.Name, assignment).Source;
                break;
            case SectionStep.Thickness thickness:
                Guard.Require(thickness.Intent is ThicknessIntent.KeepCurrent or ThicknessIntent.UseSource, "DSL-TARGET");
                intent = thickness.Intent;
                next = intent == ThicknessIntent.KeepCurrent ? KeepThickness(bytes, baseBytes) : bytes;
                break;
            default:
                (next, delegated) = SectionEdits.Apply(bytes, assignment, step);
                break;
        }
        string name = SectionProfileName(next, assignment);
        if (intent == ThicknessIntent.UseSource) next = ThicknessFit.Fit(next, name);
        var after = SessionSource.Parse(next).Definition!;
        // Ruling 71 at the one step choke point (operator 2026-10-04). Replace (and Import, which is a Replace) is judged once,
        // inside SectionReplace, before its preview is offered.
        if (step is not (SectionStep.Replace or SectionStep.Import)) SectionEdits.RequireNeighbourAbscissa(definition, after, assignment, step);
        var profile = after.Profiles.Single(item => item.Name == name);
        ThicknessProposal? proposal = intent == ThicknessIntent.UseSource ? ThicknessFit.Describe(next, name, baseBytes).Proposal : null;
        var report = delegated is not null
            ? delegated with { Thickness = proposal ?? delegated.Thickness }
            : new SectionStepReport(SectionStepKind(step), achieved ?? FoilSource.MaxOrdinateDeviation(prior, profile),
                achieved is null ? ProfileChangeOracle : "ProfileFair.MaxDeviation", profile.Upper.Points.Length, profile.Lower.Points.Length,
                import, proposal, RowsRemoved(prior, profile));
        return new(next, name, intent, report);
    }

    // The block's scope at this station: shared when another station uses it too.
    private static SectionScope ScopeOf(Definition definition, int assignment) =>
        definition.Assignments.Count(item => item.Profile == definition.Assignments[assignment].Profile) > 1 ? SectionScope.Shared : SectionScope.Independent;

    private static byte[] MoveSectionPoint(byte[] bytes, ProfileDefinition profile, SectionStep.Move move)
    {
        var curve = Surface(profile, move.Side);
        int index = Array.IndexOf(curve.Ids, move.VertexId);
        Guard.Require(index >= 0, "DSL-PROFILE-TARGET");
        Guard.Require(index != 0 && (profile.Closure != "closed" || index != curve.Points.Length - 1), "DSL-LOCK");
        Guard.Require(double.IsFinite(move.X) && double.IsFinite(move.Y) && OrderedAt(curve, index, move.X), "DSL-PROFILE-ORDER");
        bool upper = move.Side == SurfaceSide.Upper;
        if (move.X != curve.Points[index][0])
        {
            // Shared basis until SPT: the paired vertex of the other surface takes the same abscissa (as built).
            var other = upper ? profile.Lower : profile.Upper;
            Guard.Require((uint)index < (uint)other.Points.Length, "DSL-PROFILE-TARGET");
            Guard.Require(OrderedAt(other, index, move.X), "DSL-PROFILE-ORDER");
            bytes = FoilSource.PatchProfilePoint(bytes, profile.Name, upper ? "lower" : "upper", other.Ids[index], move.X, other.Points[index][1]);
        }
        return FoilSource.PatchProfilePoint(bytes, profile.Name, upper ? "upper" : "lower", move.VertexId, move.X, move.Y);
    }

    private static bool OrderedAt(Curve curve, int index, double x) =>
        x >= (index == 0 ? double.NegativeInfinity : curve.Points[index - 1][0]) &&
        x <= (index + 1 == curve.Points.Length ? double.PositiveInfinity : curve.Points[index + 1][0]);

    private static Curve Surface(ProfileDefinition profile, SurfaceSide side) => side switch
    {
        SurfaceSide.Upper => profile.Upper,
        SurfaceSide.Lower => profile.Lower,
        _ => throw new ContractError("DSL-PROFILE-TARGET")
    };

    // As built (Validate on a fair or rebuild draft): a result outside its tolerance, or one that adds monotone pieces, is refused.
    private static void RequireFair(FairResult result)
    {
        if (!result.WithinTolerance) throw new ContractError("DSL-GEOMETRY", "Fair result exceeds tolerance");
        if (result.MonotonePiecesAfter > result.MonotonePiecesBefore) throw new ContractError("DSL-GEOMETRY", "Fair increased monotone pieces");
    }

    // Keep current thickness: the thickness channel returns to the base's ordinates.
    private static byte[] KeepThickness(byte[] bytes, byte[] baseBytes) =>
        SessionSource.Parse(baseBytes).Definition!.Curves.TryGetValue("thickness", out var channel)
            ? FoilSource.PatchChannelOrdinates(bytes, channel.Points.Select(point => point[1]).ToArray())
            : bytes;

    private static string[] RowsRemoved(ProfileDefinition before, ProfileDefinition after) =>
        before.Upper.Tangents.Select(row => row.Id).Except(after.Upper.Tangents.Select(row => row.Id))
            .Concat(before.Lower.Tangents.Select(row => row.Id).Except(after.Lower.Tangents.Select(row => row.Id))).ToArray();

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
            // A section recovery names the base profile at the assignment at entry, which the base always holds, even
            // after a Make unique step (§3.3, F-6); its scope is derived from the recovered bytes.
            var next = section is not null && draft!.Rail == "section"
                ? new RecoveryRow(draft.Id, draft.Base, draft.Generation, "section", section.EntryProfile, Chunks(draft.Bytes), section.EntryProfile, draft.Assignment, draft.Intent)
                : new RecoveryRow(draft!.Id, draft.Base, draft.Generation, draft.Rail, draft.VertexId, Chunks(draft.Bytes), draft.Profile, draft.Assignment, draft.Intent);
            NativeProject.Preflight(EnvelopeCore() with { Recovery = next }, envelopeCap);
            recovery = next; return CopyRecovery(recovery)!;
        }
    }
    private void ResumeRecoveryCore()
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(recovery is not null && draft is null && recovery.BaseAcceptedId == current, "DOC-RECOVERY-BASE");
            byte[] bytes = Decode(recovery!.Utf8Base64Chunks);
            if (recovery.Rail == "section")
            {
                // Cursor 0 on the recovered bytes with an empty inner undo (§3.3).
                string profile = SectionProfileName(bytes, recovery.Assignment);
                draft = new(recovery.DraftId, recovery.BaseAcceptedId, recovery.Generation, "section", profile, bytes, profile, recovery.Assignment, recovery.Intent);
                StartSection(recovery.Profile!, bytes, profile, recovery.Intent);
                return;
            }
            if (recovery.Profile is not null)
            {
                string entryProfile = SectionProfileName(BaseBytes(recovery.BaseAcceptedId), recovery.Assignment);
                string profile = SectionProfileName(bytes, recovery.Assignment);
                draft = new(recovery.DraftId, recovery.BaseAcceptedId, recovery.Generation, "section", profile, bytes,
                    profile, recovery.Assignment, recovery.Intent);
                StartSection(entryProfile, bytes, profile, recovery.Intent);
                return;
            }
            draft = new(recovery.DraftId, recovery.BaseAcceptedId, recovery.Generation, recovery.Rail, recovery.VertexId, bytes, recovery.Profile, recovery.Assignment, recovery.Intent);
        }
    }
    private void DiscardRecoveryCore() { lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft is null, "DSL-DRAFT-OWNED"); recovery = null; } }
    static RecoveryRow? CopyRecovery(RecoveryRow? r) => r is null ? null : r with { Utf8Base64Chunks = r.Utf8Base64Chunks.ToArray() };
    private Envelope EnvelopeCore()
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            return new(RunRecord.Format(runs.Count), projectId, sources.Select(s => s with { Utf8Base64Chunks = s.Utf8Base64Chunks.ToArray() }).ToArray(), designs.ToArray(), accepted.ToArray(), cursors.ToArray(), CopyRecovery(recovery))
            { Analysis = AnalysisCore() };
        }
    }
    private byte[] SaveImageCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            PruneRunsAtSave();
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
            LoadRuns(env.Analysis);
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

    #region Analysis runs
    // ADR-0011: runs are append-only facts beside the accepted revisions. Recording one is not an edit: it never touches
    // the cursor or the redo stack, and it makes the document dirty only because the saved image now differs.
    readonly List<AnalysisRun> runs = [];
    readonly List<PolarSample> polarSamples = [];
    readonly List<PrunedRun> prunedRuns = [];
    // simplify: the system clock stamps tombstones only (prunedAt is never compared or keyed). Upgrade trigger: a test
    // or a reader that asserts a prune time needs a TimeProvider seam here.
    readonly TimeProvider pruneClock = TimeProvider.System;
    /// <summary>Retention keeps this many runs per tier beyond the reachable ones (ADR-0011 §7).</summary>
    public const int RetainedOthersPerTier = 20;

    /// <summary>
    /// Appends one run row (ADR-0011 §3, §8). The checks and the append are one step under the session lock: the row's
    /// hashes must match what it holds, it must name an accepted revision and that revision's surface, its id must be
    /// new, a key may hold one Completed row, and the document must stay within its size. Refused after close.
    /// </summary>
    public void RecordRun(AnalysisRun run) => Run("record-run", () => { RecordRunCore(run); return true; });

    /// <summary>Every stored run in document order, each with its integrity recomputed now, and the tombstones.</summary>
    public RunLedger ReadRuns() => Run("read-runs", ReadRunsCore);

    /// <summary>The ordinal and edit rail of an accepted revision (design §18.5 G-T1).</summary>
    public RevisionLabel RevisionOf(string acceptedId) => Run("revision-of", () =>
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            int index = accepted.FindIndex(row => row.Id == acceptedId);
            Guard.Require(index >= 0, "DOC-REFERENCE");
            return new RevisionLabel(index + 1, accepted[index].Edit?.Rail);
        }
    });

    /// <summary>
    /// The Analysis entry to the session's event queue (design §11, G-T10): <c>analysis.run</c>, <c>.toggle</c> and
    /// <c>.project</c> arrive here because <c>Record</c> is private. A closed session drops the event (no measurement).
    /// </summary>
    public void RecordAnalysisEvent(string operation, string outcome, double? durationMilliseconds, AnalysisEvent fields)
    {
        Guard.Require(operation.StartsWith("analysis.", StringComparison.Ordinal), "DSL-RANGE");
        lock (sync)
        {
            if (closed) return;
            if (events.Count == 256) events.Dequeue();
            events.Enqueue(new SessionEvent(eventSequence++, operation, outcome, durationMilliseconds, null, null, trace.Value,
                null, null, sources.Count, accepted.Count, operation) { Analysis = fields });
        }
    }

    private void RecordRunCore(AnalysisRun run)
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(current is not null, "DOC-EMPTY");
            var prospective = new AnalysisRecords([.. runs, run], polarSamples.ToArray(), prunedRuns.ToArray());
            RunRecord.CheckStore(prospective, AcceptedExists);
            Guard.Require(RunRecord.Verify(run) && SurfaceMatches(run), "DOC-INTEGRITY");
            NativeProject.Preflight(EnvelopeCore() with { Format = RunRecord.Format(runs.Count + 1), Analysis = prospective }, envelopeCap);
            runs.Add(run);
        }
    }

    private RunLedger ReadRunsCore()
    {
        lock (sync)
        {
            Guard.Require(!closed, "DOC-CLOSED");
            var stored = runs.Select(run => new StoredRun(run,
                RunRecord.Verify(run) && SurfaceMatches(run) ? RunIntegrity.Intact : RunIntegrity.PayloadFailedCheck)).ToArray();
            return new RunLedger(stored, prunedRuns.ToArray());
        }
    }

    private bool AcceptedExists(string acceptedId) => accepted.Any(row => row.Id == acceptedId);

    // The run claims the surface of the revision it names; a claim the revision does not hold fails the run's check.
    private bool SurfaceMatches(AnalysisRun run)
    {
        var row = accepted.SingleOrDefault(item => item.Id == run.Inputs.AcceptedId);
        return row is not null && designs.Single(design => design.Id == row.DesignId).SurfaceHash == run.Inputs.SurfaceHash;
    }

    private AnalysisRecords? AnalysisCore() =>
        runs.Count == 0 ? null : new AnalysisRecords(runs.ToArray(), polarSamples.ToArray(), prunedRuns.ToArray());

    private void LoadRuns(AnalysisRecords? records)
    {
        if (records is null) return;
        runs.AddRange(records.Runs); polarSamples.AddRange(records.PolarSamples); prunedRuns.AddRange(records.Pruned);
    }

    /// <summary>
    /// Retention at save (ADR-0011 §7). Kept: every run on a surface Undo or Redo can reach (the current revision, its
    /// ancestors and the redo stack); the latest <see cref="RetainedOthersPerTier"/> other runs per tier in document
    /// order; the latest Failed row per key. The rest leave a tombstone, so a key that comes back reads "pruned".
    /// Reachability is ruled (Data &amp; Persistence Architect, STO clearance 2026-10-04; ADR-0011 §7): the surfaces of
    /// the current revision, its parent chain and every redo-stack id (<see cref="ReachableSurfaces"/>), because those
    /// are the only revisions a session can return to: Undo walks parents (<c>Move</c>), reopen rebuilds the redo stack
    /// from the cursors (<c>ReopenCore</c>, <see cref="NativeProject.Replay"/>), and a recovery resumes only on the
    /// current revision (<c>ResumeRecoveryCore</c>). Condition C1: a later feature that opens a revision outside
    /// Undo/Redo (a history jump, A3d Compare) widens <see cref="ReachableSurfaces"/> in the same change. No Discrepancy
    /// record exists before A3d.
    /// </summary>
    private void PruneRunsAtSave()
    {
        if (runs.Count == 0) return;
        var reachable = ReachableSurfaces();
        var keys = runs.Select(RunRecord.RecomputedKey).ToArray();
        var keep = new bool[runs.Count];
        for (int i = 0; i < runs.Count; i++) keep[i] = reachable.Contains(runs[i].Inputs.SurfaceHash);
        foreach (var tier in runs.Select((run, index) => (run, index)).Where(item => !keep[item.index]).GroupBy(item => item.run.Tier))
            foreach (var (_, index) in tier.TakeLast(RetainedOthersPerTier)) keep[index] = true;
        var latestFailed = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < runs.Count; i++) if (runs[i].Outcome is RunOutcome.Failed) latestFailed[keys[i]] = i;
        foreach (int index in latestFailed.Values) keep[index] = true;
        if (keep.All(kept => kept)) return;
        var now = pruneClock.GetUtcNow();
        var kept = new List<AnalysisRun>();
        int pruned = 0;
        for (int i = 0; i < runs.Count; i++)
        {
            if (keep[i]) { kept.Add(runs[i]); continue; }
            prunedRuns.Add(new PrunedRun(runs[i].RunId, keys[i], now)); pruned++;
        }
        runs.Clear(); runs.AddRange(kept);
        // "planned": the prune is part of the captured image; whether that image was saved is the store.save event's
        // outcome, so this event never claims a completed save.
        RecordAnalysisEvent("analysis.prune", "planned", null, new AnalysisEvent { Pruned = pruned });
    }

    private HashSet<string> ReachableSurfaces()
    {
        var revisions = new HashSet<string>(redo, StringComparer.Ordinal);
        for (string? id = current; id is not null; id = accepted.Single(row => row.Id == id).Parent) revisions.Add(id);
        return accepted.Where(row => revisions.Contains(row.Id))
            .Select(row => designs.Single(design => design.Id == row.DesignId).SurfaceHash).ToHashSet(StringComparer.Ordinal);
    }
    #endregion
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
    // RespectRequiredConstructorParameters: a run row missing a member is refused, never read as a zero (ADR-0011 §3).
    // Every pre-A3a member it now requires was already required by the Exact schema checks in Read.
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true, Converters = { new RecoveryRowConverter() } };
    /// <summary>The <c>format</c> string of a native image, or null when the image is not a readable JSON object with
    /// one. Lets the store decide on the <c>.v1.bak</c> without parsing the rest (ADR-0011 §6).</summary>
    public static string? FormatOf(byte[] image)
    {
        // The writer puts `format` first, so this normally reads one property; other members are skipped, not parsed.
        var reader = new Utf8JsonReader(image, new JsonReaderOptions { MaxDepth = 32 });
        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return null;
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                bool isFormat = reader.ValueTextEquals("format"u8);
                if (!reader.Read()) return null;
                if (isFormat) return reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                reader.Skip();
            }
            return null;
        }
        catch (JsonException) { return null; }
    }
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
            Exact(root, ["format", "projectId", "sources", "designs", "accepted", "cursors", "recovery"], ["analysis"]);
            // Expand (ADR-0011 §5): -1 and -2 are read. The format is derived from the run count, so it must agree with
            // the analysis member: -1 never carries one, -2 always carries at least one run.
            string? format = root.GetProperty("format").GetString();
            Guard.Require(format is "cfdw-project-1" or "cfdw-project-2", "DOC-VERSION");
            bool hasAnalysis = root.TryGetProperty("analysis", out var analysisElement);
            if (hasAnalysis)
            {
                Exact(analysisElement, "runs", "polarSamples", "pruned");
                Guard.Require(analysisElement.GetProperty("runs").ValueKind == JsonValueKind.Array, "DOC-SCHEMA");
                Guard.Require(format == RunRecord.Format(analysisElement.GetProperty("runs").GetArrayLength()), "DOC-SCHEMA");
            }
            else Guard.Require(format == RunRecord.Format(0), "DOC-SCHEMA");
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
        "leading" or "trailing" or "dihedral" or "twist" or "thickness" => definition.Curves[channel].Ids.Contains(vertexId),
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
        "leading" or "trailing" or "dihedral" or "twist" or "thickness" or "upper" or "lower" => edit.Curve is null && edit.Rule is null && EditTarget(child, edit.Rail, edit.VertexId) && EditTarget(parent, edit.Rail, edit.VertexId),
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
        "point-add" => edit.Rule is null && edit.Curve is not null &&
            PointModel.EditableCurves.Contains(edit.Curve) &&
            EditTarget(child, edit.Curve, edit.VertexId) && !EditTarget(parent, edit.Curve, edit.VertexId),
        "point-remove" => edit.Rule is null && edit.Curve is not null &&
            PointModel.EditableCurves.Contains(edit.Curve) &&
            EditTarget(parent, edit.Curve, edit.VertexId) && !EditTarget(child, edit.Curve, edit.VertexId),
        "curve-rebuild" => edit.Rule is null && edit.Curve is not null &&
            PointModel.EditableCurves.Contains(edit.Curve) && edit.VertexId == edit.Curve,
        // A section receipt names the profile at Finish (§3.3): the parent has it, or the child newly assigns it (Make unique, Import).
        "section" => edit.Curve is null && edit.Rule is null && ProfileNamed(child, edit.VertexId) &&
            (ProfileNamed(parent, edit.VertexId) || child.Assignments.Any(item => child.Profiles[item.Profile].Name == edit.VertexId)),
        _ => false
    };
    static bool ProfileNamed(Definition definition, string name) => definition.Profiles.Any(profile => profile.Name == name);
    // Same construction rails, against the recovered draft's base: insert/rebuild name a
    // vertex the base does not have yet (or has renumbered), so there is nothing to check there.
    static bool RecoveryReference(Definition definition, string rail, string vertexId) => rail switch
    {
        "leading" or "trailing" or "upper" or "lower" => EditTarget(definition, rail, vertexId),
        "delete" or "fair" => ProfileHas(definition, vertexId),
        "insert" or "rebuild" => true,
        "section" => ProfileNamed(definition, vertexId),
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
        if (e.Analysis is not null) RunRecord.CheckStore(e.Analysis, accepted.ContainsKey);
        if (e.Recovery is not null)
        {
            var r = e.Recovery; Uuid(r.DraftId);
            Guard.Require(accepted.ContainsKey(r.BaseAcceptedId) && r.Generation is >= 0 and <= 9007199254740991
                && r.Rail is "leading" or "trailing" or "upper" or "lower" or "insert" or "delete" or "fair" or "rebuild" or "section", "DOC-REFERENCE");
            Guard.Require(r.Rail is not ("insert" or "delete" or "fair" or "rebuild" or "section") || r.Profile is not null, "DOC-REFERENCE");
            // A section recovery's vertex id is the base profile name at entry, the same name as its Profile (§3.3).
            Guard.Require(r.Rail != "section" || r.VertexId == r.Profile, "DOC-REFERENCE");
            var definition = parsed[accepted[r.BaseAcceptedId].SourceId].Definition!;
            Guard.Require(r.VertexId.Length > 0 && r.VertexId.EnumerateRunes().Count() <= 4096 && RecoveryReference(definition, r.Rail, r.VertexId), "DOC-REFERENCE"); _ = Decode(r.Utf8Base64Chunks, allowEmpty: true);
            if (r.Profile is not null)
            {
                // A profile-edit or construction recovery names its target explicitly; a rail
                // recovery (Profile null) keeps the RecoveryReference check above unchanged.
                Guard.Require(r.Rail is "upper" or "lower" or "insert" or "delete" or "fair" or "rebuild" or "section" && (uint)r.Assignment < (uint)definition.Assignments.Length, "DOC-REFERENCE");
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
