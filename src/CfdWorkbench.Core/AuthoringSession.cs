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
public sealed record EditReceipt(string DraftId, long Generation, string Rail, string VertexId);
public sealed record AcceptedRow(string Id, string? Parent, string SourceId, string DesignId, string OperationId, EditReceipt? Edit);
public sealed record CursorRow(long Sequence, string Target, string Reason, string OperationId);
public sealed record RecoveryRow(string DraftId, string BaseAcceptedId, long Generation, string Rail, string VertexId, string[] Utf8Base64Chunks, string? Profile = null, int Assignment = -1);
public sealed record Envelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted, CursorRow[] Cursors, RecoveryRow? Recovery);
public sealed record SessionDraft(string Id, string Base, long Generation, string Rail, string VertexId, byte[] Bytes, string? Profile = null, int Assignment = -1);
public sealed record SessionBinding(string SourceHash, string Base, string DraftId, long Generation, string Evaluator, string SurfaceHash, string Rail, string VertexId);

public sealed record SessionView(string AcceptedId, string SourceHash, string SurfaceHash, byte[] Source, SessionDraft? Draft, RecoveryRow? Recovery, bool Dirty);
public sealed record SessionEvent(long Sequence, string Operation, string Outcome, double DurationMilliseconds, int? InputBytes,
    int? OutputBytes, string? TraceId, long? Generation, string? Evaluator, int RetainedSources, int AcceptedFacts, string Action,
    bool? PublicationKnown = null, bool? DurabilityConfirmed = null);
public sealed record SessionPreview(SessionBinding Binding, PlacedPointEnclosure Point, double UniformWidthUpper);

public sealed class SessionAssessment
{
    internal SessionAssessment(Guid owner, GeometryStatus status, string code, SessionBinding? key, GeometryCertificate? certificate,
        AuthoredBinding? sourceBinding = null, IEnumerable<Diagnostic>? diagnostics = null, ImportReport? importReport = null)
    {
        Owner = owner; Status = status; Code = code; Key = key; Certificate = certificate;
        SourceBinding = sourceBinding; Diagnostics = Array.AsReadOnly((diagnostics ?? []).ToArray());
        ImportReport = importReport;
    }
    internal Guid Owner { get; }
    public GeometryStatus Status { get; }
    public string Code { get; }
    public SessionBinding? Key { get; }
    public GeometryCertificate? Certificate { get; }
    public AuthoredBinding? SourceBinding { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public ImportReport? ImportReport { get; }
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
        Record("language.parse", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, bytes.Length, null, "cfdw-cv/2");
        timer.Restart(); _ = parsed.SourceHash; _ = parsed.SurfaceHash;
        Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, bytes.Length, null, null, "cfdw-cv/2");
        return parsed;
    }
    private GeometryAssessment AssessOwned(SourceParse parsed, long? generation = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); var result = Geometry.Assess(parsed);
        Record("geometry.validate", result.Code, timer.Elapsed.TotalMilliseconds, parsed.Source.Length, null, generation, "cfdw-cv/2");
        return result;
    }
    public byte[] Open(byte[] source, string operationId, bool acceptIdInsertion) => Run("open", () => OpenCore(source, operationId, acceptIdInsertion), source.Length);
    public SessionDraft BeginRailEdit(string draftId, string rail, string vertexId) => Run("begin", () => BeginRailEditCore(draftId, rail, vertexId));
    public SessionDraft UpdateDraft(string draftId, long generation, double si) => Run("update", () => UpdateDraftCore(draftId, generation, si), sizeof(double), generation);
    public ProfileView ProfileAt(int assignmentIndex) => Run("profile", () => ProfileAtCore(assignmentIndex));
    public ScopeImpact DescribeScope(string profile, int assignmentIndex, SectionScope scope) => Run("scope", () => DescribeScopeCore(profile, assignmentIndex, scope));
    public SessionDraft BeginProfileEdit(string draftId, int assignmentIndex, SectionScope scope, string side, string vertexId) =>
        Run("begin", () => BeginProfileEditCore(draftId, assignmentIndex, scope, side, vertexId));
    public SessionDraft BeginProfileImport(string draftId, int assignmentIndex, byte[] dat) =>
        Run("begin", () => BeginProfileImportCore(draftId, assignmentIndex, dat));
    public SessionDraft UpdateProfileDraft(string draftId, long generation, double x, double y) =>
        Run("update", () => UpdateProfileDraftCore(draftId, generation, x, y), 2 * sizeof(double), generation);
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
    ImportReport? activeImportReport;
    string? current;
    string projectId = Guid.NewGuid().ToString("D");
    string? savedImageHash;
    public static string[] Chunks(byte[] bytes) => Enumerable.Range(0, (bytes.Length + 2303) / 2304).Select(i => Convert.ToBase64String(bytes.Skip(i * 2304).Take(2304).ToArray())).ToArray();
    static byte[] Decode(string[] chunks) => chunks.SelectMany(Convert.FromBase64String).ToArray();
    AcceptedRow Current => accepted.Single(a => a.Id == current);
    byte[] CurrentBytes => Decode(sources.Single(s => s.Id == Current.SourceId).Utf8Base64Chunks);
    SessionBinding Key(SourceParse p, SessionDraft d) => new(p.SourceHash, d.Base, d.Id, d.Generation, "cfdw-cv/2", p.SurfaceHash!, d.Rail, d.VertexId);
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
            activeImportReport = null;
            draft = new(draftId, current!, 0, rail, vertexId, CurrentBytes); return Copy(draft);
        }
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
        var samples = new ProfilePoint[101];
        for (int index = 0; index < samples.Length; index++)
        {
            double x = index / 100d;
            var ordinate = Bernstein.EncloseAt(spans, Rational.From(x), watch);
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
    private SessionDraft BeginProfileEditCore(string draftId, int assignmentIndex, SectionScope scope, string side, string vertexId)
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
            activeImportReport = null;
            draft = new(draftId, current!, 0, side, vertexId, bytes, target, assignmentIndex); return Copy(draft);
        }
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

            var fitted = DatImport.Fit(datProfile, profileName);
            var report = new ImportReport(fitted.MaxResidual, fitted.VertexCount, fitted.Accepted, fitted.Provenance);

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
            draft = draft with { Generation = expectedGeneration + 1, Bytes = bytes };
            return Copy(draft);
        }
    }
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
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", null, null, DraftBinding(capture), null, activeImportReport);
            var timer = System.Diagnostics.Stopwatch.StartNew(); var parsed = FoilSource.Parse(capture.Bytes);
            Record("language.parse", parsed.IsParsed ? "OK" : parsed.Diagnostics[0].Code, timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, parsed.IsParsed ? "cfdw-cv/2" : null);
            if (!parsed.IsParsed) return new(authorityId, parsed.Diagnostics[0].Code == "DSL-LIMIT" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid,
                parsed.Diagnostics[0].Code, null, null, DraftBinding(capture, parsed), parsed.Diagnostics, activeImportReport);
            timer.Restart(); var key = Key(parsed, capture);
            Record("identity.canonicalize", "OK", timer.Elapsed.TotalMilliseconds, capture.Bytes.Length, null, generation, "cfdw-cv/2");
            var result = AssessOwned(parsed, generation);
            if (cancellation.IsCancellationRequested) return new(authorityId, GeometryStatus.NotAssessed, "DSL-CANCELLED", key, null, DraftBinding(capture, parsed), null, activeImportReport);
            Diagnostic[] diagnostics = result.Status == GeometryStatus.Certified ? [] :
                [new(result.Code, "Geometry", "Error", 0, capture.Bytes.Length, 1, 1, capture.Rail, result.Reason, "Revise the authored curves or retain the last accepted revision.")];
            return new(authorityId, result.Status, result.Code, key, result.Certificate, DraftBinding(capture, parsed), diagnostics, activeImportReport);
        }
        catch (ContractError error)
        { return new(authorityId, error.Code is "DSL-LIMIT" or "DSL-UNSUPPORTED" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid, error.Code, null, null, DraftBinding(capture), null, activeImportReport); }
        finally { Interlocked.Exchange(ref validating, 0); }
    }
    private string ApplyCore(string operationId, SessionAssessment assessment)
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED");
            Guard.Require(assessment.ImportReport is null || assessment.ImportReport.Accepted, "DSL-TOLERANCE");
            Guard.Require(assessment.Key is not null && assessment.Status == GeometryStatus.Certified && assessment.Certificate is not null, "DSL-NOT-ASSESSED");
            string payload = "apply:" + JsonSerializer.Serialize(assessment.Key);
            if (Retry(operationId, payload, out string prior)) return prior;
            Guard.Require(draft is not null && current == draft.Base, "DSL-CONFLICT");
            var p = ParseOwned(draft!.Bytes); var key = Key(p, draft);
            Guard.Require(assessment.Owner == authorityId && assessment.Certificate!.SourceHash == key.SourceHash &&
                assessment.Certificate.SurfaceHash == key.SurfaceHash && assessment.Key == key, "DSL-CONFLICT");
            string id = Commit(p, operationId, "apply"); operations.Add(operationId, (payload, id)); draft = null; recovery = null; activeImportReport = null; return id;
        }
    }
    private void CancelCore(string draftId) { lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(draft?.Id == draftId, "DSL-CONFLICT"); draft = null; recovery = null; activeImportReport = null; } }
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
            RequireAdmission(targetParsed, new(targetParsed.SourceHash, "", "", 0, "cfdw-cv/2", targetParsed.SurfaceHash!, "", ""));
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
            var next = new RecoveryRow(draft!.Id, draft.Base, draft.Generation, draft.Rail, draft.VertexId, Chunks(draft.Bytes), draft.Profile, draft.Assignment);
            NativeProject.Preflight(EnvelopeCore() with { Recovery = next }, envelopeCap);
            recovery = next; return CopyRecovery(recovery)!;
        }
    }
    private void ResumeRecoveryCore()
    {
        lock (sync) { Guard.Require(!closed, "DOC-CLOSED"); Guard.Require(recovery is not null && draft is null && recovery.BaseAcceptedId == current, "DOC-RECOVERY-BASE"); draft = new(recovery!.DraftId, recovery.BaseAcceptedId, recovery.Generation, recovery.Rail, recovery.VertexId, Decode(recovery.Utf8Base64Chunks), recovery.Profile, recovery.Assignment); }
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
        return new RecoveryRow(root.GetProperty("draftId").GetString()!, root.GetProperty("baseAcceptedId").GetString()!,
            root.GetProperty("generation").GetInt64(), root.GetProperty("rail").GetString()!, root.GetProperty("vertexId").GetString()!,
            chunks, profile, assignment);
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
                if (a.GetProperty("edit").ValueKind != JsonValueKind.Null) Exact(a.GetProperty("edit"), "draftId", "generation", "rail", "vertexId");
            }
            foreach (var c in root.GetProperty("cursors").EnumerateArray()) Exact(c, "sequence", "target", "reason", "operationId");
            var recoveryElement = root.GetProperty("recovery");
            if (recoveryElement.ValueKind != JsonValueKind.Null)
            {
                Exact(recoveryElement, ["draftId", "baseAcceptedId", "generation", "rail", "vertexId", "utf8Base64Chunks"], ["profile", "assignment"]);
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
                Guard.Require(a.Edit.Generation is >= 0 and <= 9007199254740991 && EditTarget(parsed[a.SourceId].Definition!, a.Edit.Rail, a.Edit.VertexId), "DOC-REFERENCE");
                var parent = accepted[a.Parent]; bool same = parsed[a.SourceId].SurfaceHash! == parsed[parent.SourceId].SurfaceHash!;
                Guard.Require(EditTarget(parsed[parent.SourceId].Definition!, a.Edit.Rail, a.Edit.VertexId), "DOC-REFERENCE");
                Guard.Require(same ? a.DesignId == parent.DesignId : designs[a.DesignId].Parent == parent.DesignId, "DOC-REFERENCE");
            }
            else Guard.Require(a.Edit is null, "DOC-REFERENCE");
            accepted.Add(a.Id, a);
        }
        Guard.Require(e.Sources.All(s => e.Accepted.Any(a => a.SourceId == s.Id)) && e.Designs.All(d => e.Accepted.Any(a => a.DesignId == d.Id)), "DOC-REFERENCE");
        _ = Replay(e);
        if (e.Recovery is not null)
        {
            var r = e.Recovery; Uuid(r.DraftId); Guard.Require(accepted.ContainsKey(r.BaseAcceptedId) && r.Generation is >= 0 and <= 9007199254740991 && r.Rail is "leading" or "trailing" or "upper" or "lower", "DOC-REFERENCE");
            var definition = parsed[accepted[r.BaseAcceptedId].SourceId].Definition!;
            Guard.Require(r.VertexId.Length > 0 && r.VertexId.EnumerateRunes().Count() <= 4096 && EditTarget(definition, r.Rail, r.VertexId), "DOC-REFERENCE"); _ = Decode(r.Utf8Base64Chunks, allowEmpty: true);
            if (r.Profile is not null)
            {
                // A profile-edit recovery names its target explicitly; a rail
                // recovery (Profile null) keeps the EditTarget check above unchanged.
                Guard.Require(r.Rail is "upper" or "lower" && (uint)r.Assignment < (uint)definition.Assignments.Length, "DOC-REFERENCE");
                var profile = definition.Profiles[definition.Assignments[r.Assignment].Profile];
                Guard.Require(profile.Name == r.Profile, "DOC-REFERENCE");
                var curve = r.Rail == "upper" ? profile.Upper : profile.Lower;
                Guard.Require(curve.Ids.Contains(r.VertexId), "DOC-REFERENCE");
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
