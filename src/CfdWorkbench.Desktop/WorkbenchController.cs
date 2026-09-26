using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CfdWorkbench.Desktop;

public sealed record DisplayPoint(double Eta, double NormalizedX, bool Upper, PlacedPointEnclosure Enclosure)
{
    public double X => (Enclosure.X.Lower + Enclosure.X.Upper) / 2;
    public double Y => (Enclosure.Y.Lower + Enclosure.Y.Upper) / 2;
    public double Z => (Enclosure.Z.Lower + Enclosure.Z.Upper) / 2;
}

public sealed record DisplayFrame(IReadOnlyList<DisplayPoint> Points, SectionEnclosure CenterSection,
    double InteriorEta, double ElapsedMilliseconds, string SourceHash, string Provenance);

public sealed record SectionReportLine(string Label, string Value);

/// <summary>Label/value view of the open draft's construction, import, and thickness reports.</summary>
public sealed record SectionReport(string Kind, IReadOnlyList<SectionReportLine> Lines, bool Certified)
{
    public static SectionReport? Create(SessionAssessment assessment)
    {
        var lines = new List<SectionReportLine>();
        var kinds = new List<string>();
        if (assessment.Construction is { } construction)
        {
            kinds.Add("construction");
            lines.Add(new("Max deviation", FormatNumber(construction.MaxDeviation)));
            lines.Add(new("Tolerance", FormatTolerance(construction.Tolerance)));
            lines.Add(new("Vertex count", construction.VertexCount.ToString(CultureInfo.InvariantCulture)));
        }
        if (assessment.ImportReport is { } import)
        {
            kinds.Add("import");
            lines.Add(new("Max residual", FormatNumber(import.MaxResidual)));
            lines.Add(new("Vertex count", import.VertexCount.ToString(CultureInfo.InvariantCulture)));
            lines.Add(new("Accepted", import.Accepted ? "yes" : "no"));
            lines.Add(new("Provenance", import.Provenance));
        }
        if (assessment.Thickness is { } thickness)
        {
            kinds.Add("thickness");
            lines.Add(new("Target η", JoinNumbers(thickness.TargetEta)));
            lines.Add(new("Target t/c", JoinNumbers(thickness.TargetThickness)));
            lines.Add(new("Residuals", JoinNumbers(thickness.Residuals)));
            lines.Add(new("Affected η",
                thickness.AffectedEtaStart.ToString("G6", CultureInfo.InvariantCulture) + "–" +
                thickness.AffectedEtaEnd.ToString("G6", CultureInfo.InvariantCulture)));
        }
        if (assessment.Status != GeometryStatus.Certified)
        {
            if (kinds.Count == 0) kinds.Add("refused");
            string reason = assessment.Diagnostics.Count == 0
                ? assessment.Code
                : string.Join(" ", assessment.Diagnostics.Select(item =>
                    string.IsNullOrWhiteSpace(item.Reason) ? item.Code : item.Reason));
            lines.Add(new("Refusal", reason));
        }
        return lines.Count == 0 ? null : new(string.Join('+', kinds), lines, assessment.Status == GeometryStatus.Certified);
    }

    private static string FormatNumber(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string FormatTolerance(double? tolerance) =>
        tolerance is not double value ? "not set" :
        value == 1e-4 ? "1e-4" :
        value.ToString("G17", CultureInfo.InvariantCulture);

    private static string JoinNumbers(IReadOnlyList<double> values) =>
        values.Count == 0 ? "none" : string.Join(", ", values.Select(value => value.ToString("G6", CultureInfo.InvariantCulture)));
}

/// <summary>One adapter over the core session, certificate and store. The viewport owns no source model.</summary>
public sealed class WorkbenchController : IDisposable
{
    private AuthoringSession session = new();
    private IProjectStore store;
    private readonly Func<AuthoringSession, IProjectStore> storeFactory;
    private SessionDraft? draft;
    private AuthoredProjection? draftProjection;
    private string? projectedDraftId;
    private long projectedDraftGeneration;
    private SessionAssessment? currentAssessment;
    private CancellationTokenSource? activeSampling;
    private long stateVersion;
    private double interiorEta = .5;
    private string? expectedDiskSha;
    private byte[]? uncertainImage;
    private string? uncertainPath;
    private string? uncertainDraftId, uncertainAcceptedId;
    private long uncertainDraftGeneration;
    private string? savedDraftId, savedAcceptedId;
    private long savedDraftGeneration;
    private int saving;
    private DisplayFrame? acceptedFrame;
    private bool disposed;
    private bool draftInputValid = true;
    private readonly Dictionary<int, (string Key, ProfileView View)> sectionViews = new();

    public WorkbenchController(Func<AuthoringSession, IProjectStore>? storeFactory = null)
    {
        this.storeFactory = storeFactory ?? (active => new ProjectStore(active));
        store = this.storeFactory(session);
    }
    public event Action? Changed;
    public AcceptedInspection? Inspection { get; private set; }
    public AuthoredProjection? PendingProjection { get; private set; }
    public byte[]? PendingOriginal { get; private set; }
    public byte[]? PendingCandidate { get; private set; }
    public string? NativePath { get; private set; }
    public string? OpenedPath { get; private set; }
    public string Status { get; private set; } = "Open Example or a .foil / .cfdw.json file.";
    public string Provenance { get; private set; } = "empty";
    public DisplayFrame? Frame { get; private set; }
    public IReadOnlyList<DisplayPoint> Points => Frame?.Points ?? [];
    public SectionEnclosure? CenterSection => Frame?.CenterSection;
    public SessionDraft? Draft => draft;
    public AuthoredProjection? DraftProjection
    {
        get
        {
            if (draft is null) return null;
            if (draftProjection is null || projectedDraftId != draft.Id || projectedDraftGeneration != draft.Generation)
            {
                try { draftProjection = session.InspectDraft(); }
                catch (ContractError) { draftProjection = null; }
                projectedDraftId = draft.Id;
                projectedDraftGeneration = draft.Generation;
            }
            return draftProjection;
        }
    }
    public bool DraftInputValid => draftInputValid;
    public SectionReport? SectionReport { get; private set; }
    public bool HasRecovery => Inspection is not null && session.Snapshot().Recovery is not null;
    public bool IsDirty
    {
        get
        {
            if (Inspection is null) return false;
            var view = session.Snapshot();
            if (uncertainImage is not null || !draftInputValid) return true;
            return view.Draft is { } active
                ? savedDraftId != active.Id || savedDraftGeneration != active.Generation || savedAcceptedId != view.AcceptedId
                : view.Dirty;
        }
    }
    public bool SaveUncertain => uncertainImage is not null;
    public string? UncertainPath => uncertainPath;
    public string AcceptedSource => Inspection is null ? "" : Encoding.UTF8.GetString(session.Snapshot().Source);
    public string CandidateSource => PendingCandidate is null ? "" : Encoding.UTF8.GetString(PendingCandidate);
    public string OriginalSource => PendingOriginal is null ? "" : Encoding.UTF8.GetString(PendingOriginal);
    public string RecoverySource
    {
        get
        {
            var recovery = session.Snapshot().Recovery;
            return recovery is null ? "" : Encoding.UTF8.GetString(recovery.Utf8Base64Chunks
                .SelectMany(Convert.FromBase64String).ToArray());
        }
    }
    public IReadOnlyList<SessionEvent> LocalEvents => session.ReadLocalEvents();

    public async Task OpenExampleAsync() => await OpenFoilAsync(CfdWorkbench.Cli.Cli.ExampleBytes(), "Embedded Example");

    public async Task OpenPathAsync(string path, CancellationToken cancellation = default)
    {
        if (path.EndsWith(".foil", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await CfdWorkbench.Cli.Cli.ReadFoilBoundedAsync(path, cancellation);
            await OpenFoilAsync(bytes, path, cancellation);
        }
        else if (path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            var read = await store.ReadAsync(path, cancellation);
            var next = new AuthoringSession();
            try { next.Reopen(read.Image); }
            catch { next.Dispose(); throw; }
            Adopt(next);
            expectedDiskSha = read.DiskSha256;
            NativePath = path;
            OpenedPath = path;
            await RefreshAcceptedAsync(cancellation);
            Status = HasRecovery ? "Accepted project reopened. A separate recovery draft is available." : "Accepted project reopened.";
            Notify();
        }
        else throw new ContractError("DOC-TYPE");
    }

    public async Task OpenFoilAsync(byte[] bytes, string label, CancellationToken cancellation = default)
    {
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed)
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingProjection = parsed.Authored();
            Status = parsed.Diagnostics.FirstOrDefault() is { } diagnostic
                ? $"{diagnostic.Code}: {diagnostic.Reason} {diagnostic.Recovery}"
                : "Source rejected. Accepted geometry is unavailable.";
            Provenance = Inspection is null ? "invalid source" : "accepted — import refused";
            Notify();
            return;
        }
        var candidate = FoilSource.MaterializeIds(parsed);
        if (!candidate.AsSpan().SequenceEqual(bytes))
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingCandidate = candidate;
            PendingProjection = parsed.Authored();
            Status = "Source has no explicit control IDs. Compare the retained original with the candidate, then accept IDs.";
            Provenance = Inspection is null ? "ID candidate — not accepted" : "accepted — ID candidate pending";
            Notify();
            return;
        }
        var assessment = Geometry.Assess(parsed);
        if (assessment.Status != GeometryStatus.Certified)
        {
            ClearPendingImport();
            PendingOriginal = bytes.ToArray();
            PendingProjection = parsed.Authored();
            Status = $"{assessment.Status}: {assessment.Reason} Original source retained read-only.";
            Provenance = Inspection is null ? "unavailable geometry" : "accepted — import refused";
            Notify();
            return;
        }
        var next = new AuthoringSession();
        try { next.Open(bytes, Guid.NewGuid().ToString("D"), false); }
        catch { next.Dispose(); throw; }
        Adopt(next);
        OpenedPath = label;
        await RefreshAcceptedAsync(cancellation);
    }

    public async Task AcceptCandidateAsync(CancellationToken cancellation = default)
    {
        if (PendingOriginal is null || PendingCandidate is null) throw new ContractError("DSL-IDS-REQUIRED");
        var next = new AuthoringSession();
        try { next.Open(PendingOriginal, Guid.NewGuid().ToString("D"), true); }
        catch { next.Dispose(); throw; }
        Adopt(next);
        await RefreshAcceptedAsync(cancellation);
    }

    public void BeginEdit(string rail, string vertexId)
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
        var control = Inspection.Authored.Rails.Single(r => r.Name == rail).Controls.Single(c => c.Id == vertexId);
        if (!control.Editable) throw new ContractError("DSL-LOCK");
        CancelSampling();
        draft = session.BeginRailEdit(Guid.NewGuid().ToString("D"), rail, vertexId);
        draftInputValid = true;
        interiorEta = control.Eta;
        Frame = acceptedFrame = null;
        currentAssessment = null;
        SectionReport = null;
        Status = $"Draft owns {rail} control {vertexId}. Sampling accepted geometry at η {interiorEta:G3}.";
        Provenance = "draft — accepted sampling";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void UpdateDraft(double ordinateSi)
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        draft = session.UpdateDraft(draft.Id, draft.Generation, ordinateSi);
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "draft — accepted geometry shown";
        Status = $"Draft generation {draft.Generation} changed. Preview to assess geometry.";
        Notify();
    }

    public ScopeImpact DescribeScope(int assignmentIndex, SectionScope scope)
    {
        if (Inspection is null) throw new ContractError("DOC-EMPTY");
        if ((uint)assignmentIndex >= (uint)Inspection.Authored.Assignments.Count) throw new ContractError("DSL-PROFILE-TARGET");
        string profile = Inspection.Authored.Assignments[assignmentIndex].ProfileName;
        return session.DescribeScope(profile, assignmentIndex, scope);
    }

    public ProfileView SectionView(int assignmentIndex)
    {
        string key = SectionViewKey(assignmentIndex);
        if (sectionViews.TryGetValue(assignmentIndex, out var cached) && cached.Key == key) return cached.View;
        var view = session.ProfileAt(assignmentIndex);
        sectionViews[assignmentIndex] = (key, view);
        return view;
    }

    public void BeginSectionEdit(int assignmentIndex, SectionScope scope, string side, string vertexId,
        ThicknessIntent thickness = ThicknessIntent.KeepCurrent)
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
        var started = session.BeginProfileEdit(Guid.NewGuid().ToString("D"), assignmentIndex, scope, side, vertexId, thickness);
        CancelSampling();
        draft = started;
        draftInputValid = true;
        if ((uint)assignmentIndex < (uint)Inspection.Authored.Assignments.Count)
            interiorEta = Inspection.Authored.Assignments[assignmentIndex].Eta;
        Frame = acceptedFrame = null;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        Status = $"Draft {started.Id} owns station {assignmentIndex} {side} {vertexId}. Sampling accepted geometry at η {interiorEta:G3}.";
        Provenance = "draft — accepted sampling";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void UpdateSectionDraft(double x, double y)
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        CancelSampling();
        draft = session.UpdateProfileDraft(draft.Id, draft.Generation, x, y);
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "draft — accepted geometry shown";
        Status = $"Draft owns station {draft.Assignment} {draft.Rail} {draft.VertexId}. Draft generation {draft.Generation} changed. Preview to assess geometry.";
        Notify();
    }

    public void BeginSectionInsert(int assignmentIndex, SectionScope scope, double x)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileInsert(Guid.NewGuid().ToString("D"), assignmentIndex, scope, x);
        OpenConstructedDraft(started, assignmentIndex, "insert");
    }

    public void BeginSectionDelete(int assignmentIndex, SectionScope scope, int vertexIndex)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileDelete(Guid.NewGuid().ToString("D"), assignmentIndex, scope, vertexIndex);
        OpenConstructedDraft(started, assignmentIndex, "delete");
    }

    public void BeginSectionFair(int assignmentIndex, SectionScope scope, double tolerance, PreserveEnds ends)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileFair(Guid.NewGuid().ToString("D"), assignmentIndex, scope, tolerance, ends);
        OpenConstructedDraft(started, assignmentIndex, "fair");
    }

    public void BeginSectionRebuild(int assignmentIndex, SectionScope scope, int vertexCount, double tolerance, PreserveEnds ends)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileRebuild(Guid.NewGuid().ToString("D"), assignmentIndex, scope, vertexCount, tolerance, ends);
        OpenConstructedDraft(started, assignmentIndex, "rebuild");
    }

    public void BeginSectionImport(int assignmentIndex, byte[] dat)
    {
        RequireCertifiedFoil();
        var started = session.BeginProfileImport(Guid.NewGuid().ToString("D"), assignmentIndex, dat);
        OpenConstructedDraft(started, assignmentIndex, "import");
    }

    public void InvalidateDraftInput(string? reason = null)
    {
        if (draft is null) return;
        CancelSampling();
        draftInputValid = false;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "draft — invalid numeric input";
        Status = reason ?? "Enter a finite numeric aft position. Preview and Save are blocked until corrected.";
        Notify();
    }

    public async Task PreviewAsync(CancellationToken cancellation = default)
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        CancelSampling();
        long version = stateVersion;
        var capture = draft;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeSampling = linked;
        SectionReport = null;
        Status = $"{SectionDraftPrefix()}Assessing draft geometry…";
        Notify();
        try
        {
            var assessment = await Task.Run(() => session.Validate(capture.Id, capture.Generation, linked.Token), linked.Token);
            if (version != stateVersion || draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
            Remember(assessment);
            if (assessment.Status != GeometryStatus.Certified || assessment.Certificate is null)
            {
                Frame = acceptedFrame;
                Provenance = "draft — unavailable geometry";
                Status = $"{SectionDraftPrefix()}{assessment.Code}: {assessment.Status}. {string.Join(" ", assessment.Diagnostics.Select(d => d.Reason))}";
                Notify();
                return;
            }
            double eta = interiorEta;
            var frame = await Task.Run(() => Sample(assessment.Certificate, assessment.Key!.SourceHash, "preview", eta, linked.Token), linked.Token);
            if (version != stateVersion || draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
            Frame = frame;
            Provenance = "preview";
            Status = $"{SectionDraftPrefix()}Preview of {capture.Rail} {capture.VertexId}; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
            Notify();
        }
        catch (OperationCanceledException) { }
        catch (ContractError error) when (error.Code == "GEOMETRY-CANCELLED") { }
        finally { if (ReferenceEquals(activeSampling, linked)) activeSampling = null; }
    }

    public void Apply()
    {
        if (draft is null || !draftInputValid || currentAssessment is null || currentAssessment.Status != GeometryStatus.Certified || Frame?.Provenance != "preview")
            throw new ContractError("DSL-NOT-ASSESSED");
        CancelSampling();
        session.Apply(Guid.NewGuid().ToString("D"), currentAssessment);
        draft = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Inspection = session.InspectAccepted();
        acceptedFrame = Frame with { Provenance = "accepted", SourceHash = Inspection.Authored.Binding.SourceHash };
        Frame = acceptedFrame;
        Provenance = "accepted";
        Status = "Draft applied as one accepted source revision. Save to persist it.";
        Notify();
    }

    public void Cancel()
    {
        if (draft is null) return;
        CancelSampling();
        session.Cancel(draft.Id);
        draft = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        Frame = acceptedFrame;
        Provenance = "accepted";
        Status = "Draft cancelled. Accepted source and history are unchanged.";
        Notify();
        if (Frame is null) _ = RefreshAcceptedAsync();
    }

    public void Undo()
    {
        CancelSampling();
        session.Undo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Undo selected the preceding accepted source revision. Sampling…";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void Redo()
    {
        CancelSampling();
        session.Redo(Guid.NewGuid().ToString("D"));
        Inspection = session.InspectAccepted();
        Frame = acceptedFrame = null;
        Provenance = "accepted";
        Status = "Redo selected the next accepted source revision. Sampling…";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public async Task<SaveResult> SaveAsync(string path, CancellationToken cancellation = default)
    {
        if (!path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase)) throw new ContractError("DOC-TYPE");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        if (uncertainImage is not null) throw new ContractError("DOC-SAVE-UNCERTAIN");
        if (Interlocked.CompareExchange(ref saving, 1, 0) != 0) throw new ContractError("DOC-SAVE-PENDING");
        try
        {
            var capturedSession = session;
            var capturedStore = store;
            if (capturedSession.Snapshot().Draft is not null) capturedSession.CaptureRecovery();
            var capturedView = capturedSession.Snapshot();
            byte[] image = capturedSession.SaveImage();
            var result = await capturedStore.SaveAsync(path, new SaveRequest(image, path == NativePath ? expectedDiskSha : null,
                Guid.NewGuid().ToString("D")), cancellation);
            if (!ReferenceEquals(session, capturedSession)) return result;
            string hash = Identity.Sha256(image);
            if (result.Code == "OK" && result.PublicationKnown && result.DurabilityConfirmed && result.PublishedSha256 == hash)
            {
                capturedSession.AcknowledgeSaved(image);
                NativePath = path;
                expectedDiskSha = hash;
                MarkSavedDraft(capturedView);
                Status = "Saved and durability confirmed.";
            }
            else
            {
                if (result.Code == "DOC-SAVE-UNCERTAIN" || result.PublicationKnown && !result.DurabilityConfirmed)
                {
                    uncertainImage = image;
                    uncertainPath = path;
                    uncertainDraftId = capturedView.Draft?.Id;
                    uncertainDraftGeneration = capturedView.Draft?.Generation ?? 0;
                    uncertainAcceptedId = capturedView.AcceptedId;
                }
                Status = $"{result.Code}: Save was not acknowledged. {(uncertainImage is null ? "Resolve the refusal before retry." : "The attempted path and image are retained for a durable retry.")}";
            }
            Notify();
            return result;
        }
        finally { Volatile.Write(ref saving, 0); }
    }

    public async Task<bool> ResolveUncertainSaveAsync(CancellationToken cancellation = default)
    {
        if (uncertainImage is null || uncertainPath is null) throw new ContractError("DOC-SAVE-UNCERTAIN");
        if (Interlocked.CompareExchange(ref saving, 1, 0) != 0) throw new ContractError("DOC-SAVE-PENDING");
        try
        {
        var capturedSession = session;
        var capturedStore = store;
        var capturedImage = uncertainImage;
        var capturedPath = uncertainPath;
        ReadResult read;
        try { read = await capturedStore.ReadAsync(capturedPath, cancellation); }
        catch (ContractError error)
        {
            if (!ReferenceEquals(session, capturedSession)) return false;
            Status = $"{error.Code}: Uncertain save remains dirty; disk image could not be compared.";
            Notify();
            return false;
        }
        if (!ReferenceEquals(session, capturedSession)) return false;
        if (read.DiskSha256 != Identity.Sha256(capturedImage))
        {
            Status = "Disk differs from the captured save image. Keep this draft dirty; reopen or save to a new path after review.";
            Notify();
            return false;
        }
        var retry = await capturedStore.SaveAsync(capturedPath,
            new SaveRequest(capturedImage, read.DiskSha256, Guid.NewGuid().ToString("D")), cancellation);
        if (!ReferenceEquals(session, capturedSession)) return false;
        if (retry.Code != "OK" || !retry.PublicationKnown || !retry.DurabilityConfirmed ||
            retry.PublishedSha256 != read.DiskSha256)
        {
            Status = $"{retry.Code}: Matching readback did not confirm durability; save remains dirty.";
            Notify();
            return false;
        }
        capturedSession.AcknowledgeSaved(capturedImage);
        NativePath = capturedPath;
        expectedDiskSha = retry.PublishedSha256;
        savedDraftId = uncertainDraftId;
        savedDraftGeneration = uncertainDraftGeneration;
        savedAcceptedId = uncertainAcceptedId;
        uncertainImage = null;
        uncertainPath = null;
        uncertainDraftId = uncertainAcceptedId = null;
        Status = "Matching disk image was published again and durability confirmed; save acknowledged.";
        Notify();
        return true;
        }
        finally { Volatile.Write(ref saving, 0); }
    }

    public void ResumeRecovery()
    {
        session.ResumeRecovery();
        draft = session.Snapshot().Draft;
        draftInputValid = true;
        MarkSavedDraft(session.Snapshot());
        interiorEta = Inspection?.Authored.Rails.Single(r => r.Name == draft!.Rail).Controls
            .Single(c => c.Id == draft!.VertexId).Eta ?? .5;
        Frame = acceptedFrame = null;
        Status = $"Recovery draft resumed separately at η {interiorEta:G3}; Preview to assess it.";
        Provenance = "recovery draft — accepted sampling";
        Notify();
        _ = RefreshAcceptedAsync();
    }

    public void DiscardRecovery()
    {
        session.DiscardRecovery();
        Status = "Recovery draft discarded. Accepted project retained.";
        Notify();
    }

    private async Task RefreshAcceptedAsync(CancellationToken cancellation = default)
    {
        CancelSampling();
        var inspected = session.InspectAccepted();
        Inspection = inspected;
        if (Frame?.SourceHash != inspected.Authored.Binding.SourceHash || Frame?.InteriorEta != interiorEta)
            Frame = acceptedFrame = null;
        double eta = interiorEta;
        long version = stateVersion;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeSampling = linked;
        Provenance = draft is null ? "accepted — sampling" : "draft — accepted sampling";
        Status = $"{SectionDraftPrefix()}Sampling accepted geometry at η {eta:G3}…";
        Notify();
        try
        {
            if (inspected.Geometry.Certificate is null)
            {
                Frame = null;
                Status = $"{SectionDraftPrefix()}{inspected.Geometry.Status}: {inspected.Geometry.Reason}";
                Notify();
                return;
            }
            var frame = await Task.Run(() => Sample(inspected.Geometry.Certificate, inspected.Authored.Binding.SourceHash,
                "accepted", eta, linked.Token), linked.Token);
            if (version != stateVersion || Inspection?.Authored.Binding.SourceHash != frame.SourceHash || interiorEta != frame.InteriorEta) return;
            Frame = acceptedFrame = frame;
            Provenance = draft is null ? "accepted" : "draft — accepted geometry shown";
            Status = $"{SectionDraftPrefix()}Accepted η {eta:G3} slice; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
            Notify();
        }
        catch (OperationCanceledException) { }
        catch (ContractError error) when (error.Code == "GEOMETRY-CANCELLED") { }
        catch (ContractError error) { Status = $"{SectionDraftPrefix()}{error.Code}: Geometry display unavailable; accepted source retained."; Notify(); }
        finally { if (ReferenceEquals(activeSampling, linked)) activeSampling = null; }
    }

    private static DisplayFrame Sample(GeometryCertificate certificate, string sourceHash, string provenance, double interiorEta, CancellationToken cancellation)
    {
        var watch = Stopwatch.StartNew();
        var points = new List<DisplayPoint>(15);
        foreach (double eta in new[] { 0d, interiorEta, 1d })
            foreach (var sample in new (double X, bool Upper)[] { (0, true), (.5, true), (1, true), (.5, false), (1, false) })
                points.Add(new(eta, sample.X, sample.Upper,
                    Geometry.PointAt(certificate, eta, sample.X, sample.Upper, timeBudget: TimeSpan.FromSeconds(1), cancellationToken: cancellation)));
        var section = Geometry.SectionAt(certificate, interiorEta, .5, TimeSpan.FromSeconds(1), cancellation);
        return new(points.AsReadOnly(), section, interiorEta, watch.Elapsed.TotalMilliseconds, sourceHash, provenance);
    }

    private void CancelSampling()
    {
        Interlocked.Increment(ref stateVersion);
        activeSampling?.Cancel();
        activeSampling = null;
    }

    private void ClearPendingImport()
    {
        PendingProjection = null;
        PendingOriginal = null;
        PendingCandidate = null;
    }

    private void MarkSavedDraft(SessionView view)
    {
        savedDraftId = view.Draft?.Id;
        savedDraftGeneration = view.Draft?.Generation ?? 0;
        savedAcceptedId = view.AcceptedId;
    }

    private void Adopt(AuthoringSession next)
    {
        CancelSampling();
        store.Dispose();
        session.Dispose();
        session = next;
        store = storeFactory(session);
        Inspection = null;
        ClearPendingImport();
        NativePath = null;
        OpenedPath = null;
        expectedDiskSha = null;
        uncertainImage = null;
        uncertainPath = null;
        uncertainDraftId = uncertainAcceptedId = null;
        savedDraftId = savedAcceptedId = null;
        interiorEta = .5;
        acceptedFrame = null;
        Frame = null;
        draft = null;
        draftInputValid = true;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        Provenance = "empty";
        Status = "Opening…";
        Notify();
    }

    private void RequireCertifiedFoil()
    {
        if (Inspection?.Geometry.Status != GeometryStatus.Certified) throw new ContractError("DSL-NOT-ASSESSED");
    }

    private void OpenConstructedDraft(SessionDraft started, int assignmentIndex, string kind)
    {
        CancelSampling();
        draft = started;
        draftInputValid = true;
        if (Inspection is { } inspected && (uint)assignmentIndex < (uint)inspected.Authored.Assignments.Count)
            interiorEta = inspected.Authored.Assignments[assignmentIndex].Eta;
        Frame = acceptedFrame = null;
        currentAssessment = null;
        SectionReport = null;
        sectionViews.Clear();
        Status = $"Draft {started.Id} owns station {assignmentIndex} {kind}.";
        Provenance = "draft — accepted sampling";
        AssessDraftNow();
    }

    private void AssessDraftNow()
    {
        if (draft is null) throw new ContractError("DSL-DRAFT-OWNED");
        if (!draftInputValid) throw new ContractError("DSL-INVALID-NUMERIC");
        CancelSampling();
        var capture = draft;
        SectionReport = null;
        Status = $"{SectionDraftPrefix()}Assessing draft geometry…";
        Notify();
        var assessment = session.Validate(capture.Id, capture.Generation);
        if (draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
        Remember(assessment);
        sectionViews.Clear();
        if (assessment.Status != GeometryStatus.Certified || assessment.Certificate is null || assessment.Key is null)
        {
            Frame = acceptedFrame;
            Provenance = "draft — unavailable geometry";
            Status = $"{SectionDraftPrefix()}{assessment.Code}: {assessment.Status}. {string.Join(" ", assessment.Diagnostics.Select(item => item.Reason))}";
            Notify();
            return;
        }
        var frame = Sample(assessment.Certificate, assessment.Key.SourceHash, "preview", interiorEta, CancellationToken.None);
        if (draft?.Id != capture.Id || draft.Generation != capture.Generation) return;
        Frame = frame;
        Provenance = "preview";
        Status = $"{SectionDraftPrefix()}Preview of {capture.Rail} {capture.VertexId}; 15 measured display points in {frame.ElapsedMilliseconds:F0} ms. Segment interpolation error is Not assessed.";
        Notify();
    }

    private void Remember(SessionAssessment assessment)
    {
        currentAssessment = assessment;
        SectionReport = CfdWorkbench.Desktop.SectionReport.Create(assessment);
    }

    private string SectionDraftPrefix() =>
        draft is { Profile: not null, Assignment: >= 0 } section ? $"Draft {section.Id} owns station {section.Assignment}. " : "";

    private string SectionViewKey(int assignmentIndex)
    {
        var binding = Inspection?.Authored.Binding;
        string accepted = binding?.AcceptedId ?? "";
        string hash = binding?.SourceHash ?? "";
        if (draft is { Profile: not null } active && active.Assignment == assignmentIndex)
            return accepted + ":" + hash + ":d:" + active.Id + ":" + active.Generation;
        return accepted + ":" + hash + ":a";
    }

    private void Notify() => Changed?.Invoke();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CancelSampling();
        store.Dispose();
        session.Dispose();
    }
}
