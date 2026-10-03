using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.SectionDraftTests;

namespace CfdWorkbench.Core.Tests;

// M1.2c section receipts, recovery and reopen (docs/design/m12c-section-editor.md §3.3, §9, §12.4 SDR). Legacy fixtures are
// synthetic envelopes built from FoilSource patches, so they outlive the M1.1 writers that CTL deletes (seam S-3).
internal static class ReopenSectionDraftTests
{
    internal static void Run()
    {
        Check("Receipt_SectionRecovery_RoundTripsExpandOnly", () =>
        {
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            var recovery = session.CaptureRecovery();
            Equal("section", recovery.Rail);
            Equal("section-a", recovery.VertexId);
            Equal("section-a", recovery.Profile);
            Equal(0, recovery.Assignment);
            Equal(view.Generation, recovery.Generation);
            byte[] image = session.SaveImage();
            using var document = JsonDocument.Parse(image);
            string[] fields = document.RootElement.GetProperty("recovery").EnumerateObject().Select(item => item.Name).ToArray();
            string[] asBuilt = ["draftId", "baseAcceptedId", "generation", "rail", "vertexId", "utf8Base64Chunks", "profile", "assignment", "intent"];
            Equal(true, fields.All(asBuilt.Contains));
            using var reopened = new AuthoringSession();
            reopened.Reopen(image);
            var read = reopened.Snapshot().Recovery!;
            Equal(recovery.DraftId, read.DraftId);
            Equal(recovery.Rail, read.Rail);
            Equal(recovery.Profile, read.Profile);
            Equal(string.Join(',', recovery.Utf8Base64Chunks), string.Join(',', read.Utf8Base64Chunks));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(session.Envelope() with
                { Recovery = recovery with { VertexId = "section-a-i1", Profile = "section-a-i1" } })));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(session.Envelope() with
                { Recovery = recovery with { Profile = null, Assignment = -1 } })));
        });
        Check("ReopenSectionDraft_MakeUniqueThenCrash_OpensAndResumes", () =>
        {
            byte[] image, drafted; string id = Id(), baseId;
            using (var session = Opened())
            {
                baseId = session.Snapshot().AcceptedId;
                var view = session.BeginSectionDraft(id, 0);
                view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
                view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
                drafted = view.Bytes;
                session.CaptureRecovery();
                image = session.SaveImage();
            }
            using var reopened = new AuthoringSession();
            reopened.Reopen(image);
            reopened.ResumeRecovery();
            var draft = reopened.Snapshot().Draft!;
            Equal("section", draft.Rail);
            Same(drafted, draft.Bytes);
            var resumed = reopened.UndoSectionStep(id);
            Equal(0, resumed.Cursor);
            Equal(0, resumed.StepCount);
            Equal(baseId, resumed.BaseAcceptedId);
            Equal("section-a-i1", resumed.Profile);
            Equal(SectionScope.Independent, resumed.Scope);
            Same(drafted, resumed.Bytes);
            var view2 = reopened.ApplySectionStep(id, resumed.Generation, Raise(resumed, "cv-4", 0.005));
            reopened.FinishSection(Id(), reopened.AssessSection(id, view2.Generation, CancellationToken.None));
            Equal("section-a-i1", Assigned(reopened.Snapshot().Source, 0));
            Equal("section-a", Assigned(reopened.Snapshot().Source, 1));
            Equal("section-a-i1", reopened.Envelope().Accepted[^1].Edit!.VertexId);
        });
        Check("Reopen_LegacyProfileRecovery_ResumesAsSectionDraft", () =>
        {
            using var session = Opened();
            byte[] source = session.Snapshot().Source;
            var point = FoilSource.Parse(source).Definition!.Profiles[0].Upper.Points[3];
            byte[] recovered = FoilSource.PatchProfilePoint(source, "section-a", "upper", "cv-3", point[0], point[1] + 0.01);
            string id = Id();
            var legacy = new RecoveryRow(id, session.Snapshot().AcceptedId, 1, "upper", "cv-3", AuthoringSession.Chunks(recovered), "section-a", 0);
            using var reopened = new AuthoringSession();
            reopened.Reopen(NativeProject.Encode(session.Envelope() with { Recovery = legacy }));
            reopened.ResumeRecovery();
            var view = reopened.UndoSectionStep(id);
            Equal(0, view.Cursor);
            Equal(0, view.StepCount);
            Equal(1L, view.Generation);
            Equal(session.Snapshot().AcceptedId, view.BaseAcceptedId);
            Equal("section-a", view.Profile);
            Equal(SectionScope.Shared, view.Scope);
            Same(recovered, view.Bytes);
            Equal("section", reopened.Snapshot().Draft!.Rail);
            view = reopened.ApplySectionStep(id, view.Generation, Raise(view, "cv-4", 0.005));
            reopened.FinishSection(Id(), reopened.AssessSection(id, view.Generation, CancellationToken.None));
            var receipt = reopened.Envelope().Accepted[^1].Edit!;
            Equal("section", receipt.Rail);
            Equal("section-a", receipt.VertexId);
            Same(view.Bytes, reopened.Snapshot().Source);
        });
        Check("Reopen_LegacyInsertDeleteFairRebuildReceipts_StillCheck", () =>
        {
            byte[] root = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            var (inserted, newId) = FoilSource.InsertProfileKnot(root, "section-a", 0.5);
            var (deleted, removedId) = FoilSource.DeleteProfileVertex(inserted, "section-a", 2);
            var (faired, fair) = FoilSource.FairProfile(deleted, "section-a", 1e-4, PreserveEnds.Position);
            var (rebuilt, rebuild) = FoilSource.RebuildProfile(faired, "section-a", 10, 1e-2, PreserveEnds.Position);
            Equal(true, fair.WithinTolerance && rebuild.WithinTolerance);
            var envelope = Chain(root, ("insert", newId, inserted), ("delete", removedId, deleted), ("fair", "cv-0", faired), ("rebuild", "cv-0", rebuilt));
            using var reopened = new AuthoringSession();
            reopened.Reopen(NativeProject.Encode(envelope));
            Same(rebuilt, reopened.Snapshot().Source);
            foreach (byte[] expected in new[] { faired, deleted, inserted, root })
            {
                reopened.Undo(Id());
                Same(expected, reopened.Snapshot().Source);
            }
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(Chain(root, ("insert", "cv-99", inserted)))));
        });
        Check("Receipt_SectionRailWithCurveOrRule_DocReference", () =>
        {
            var envelope = FinishedMakeUnique(out _);
            using (var valid = new AuthoringSession()) valid.Reopen(NativeProject.Encode(envelope));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(WithLastEdit(envelope, edit => edit with { Curve = "leading" }))));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(WithLastEdit(envelope, edit => edit with { Rule = "linear" }))));
        });
        Check("Receipt_SectionMakeUnique_ChildOnlyName_Accepted", () =>
        {
            var envelope = FinishedMakeUnique(out byte[] finished);
            var parent = envelope.Accepted[^2];
            byte[] parentSource = Convert.FromBase64String(string.Concat(envelope.Sources.Single(s => s.Id == parent.SourceId).Utf8Base64Chunks));
            Equal(false, FoilSource.Parse(parentSource).Definition!.Profiles.Any(p => p.Name == "section-a-i1"));
            Equal("section-a-i1", envelope.Accepted[^1].Edit!.VertexId);
            using var reopened = new AuthoringSession();
            reopened.Reopen(NativeProject.Encode(envelope));
            Same(finished, reopened.Snapshot().Source);
            Equal("section-a-i1", Assigned(reopened.Snapshot().Source, 0));
        });
        Check("Receipt_ForgedSectionRailUnknownProfile_DocReference", () =>
        {
            var envelope = FinishedMakeUnique(out _);
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(WithLastEdit(envelope, edit => edit with { VertexId = "no-such-profile" }))));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(WithLastEdit(envelope, edit => edit with { VertexId = "cv-3" }))));
            byte[] root = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
            var point = FoilSource.Parse(root).Definition!.Profiles[0].Upper.Points[3];
            byte[] moved = FoilSource.PatchProfilePoint(root, "section-a", "upper", "cv-3", point[0], point[1] + 0.01);
            using (var shared = new AuthoringSession()) shared.Reopen(NativeProject.Encode(Chain(root, ("section", "section-a", moved))));
            Refuses("DOC-REFERENCE", () => new AuthoringSession().Reopen(NativeProject.Encode(Chain(root, ("section", "section-b", moved)))));
        });
        Check("FinishSection_SameOperationIdAfterReopen_Idempotent", () =>
        {
            using var session = Opened();
            string id = Id(), operation = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
            var assessment = session.AssessSection(id, view.Generation, CancellationToken.None);
            string first = session.FinishSection(operation, assessment)!;
            Equal(first, session.FinishSection(operation, assessment));
            int rows = session.Envelope().Accepted.Length;
            byte[] image = session.SaveImage();
            using var reopened = new AuthoringSession();
            reopened.Reopen(image);
            Equal(first, reopened.FinishSection(operation, assessment));
            Equal(rows, reopened.Envelope().Accepted.Length);
            Refuses("DOC-OPERATION-CONFLICT", () => reopened.Undo(operation));
        });
        Check("ReopenSectionDraft_FinishedSection_TypesAndKindsAsSaved", () =>
        {
            // Point types are derived from the knots (ADR-0005) and kinds are the stored tangent rows. Until SPT merges, the
            // shared-basis Insert step is the only one that changes the knots; SetType/SetTangent register at S-8.
            using var session = Opened();
            string id = Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.Insert(SurfaceSide.Upper, 0.5));
            Equal(9, view.Last!.UpperPoints);
            session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
            var saved = FoilSource.Parse(session.Snapshot().Source).Definition!.Profiles.Single(p => p.Name == "section-a");
            using var reopened = new AuthoringSession();
            reopened.Reopen(session.SaveImage());
            var read = FoilSource.Parse(reopened.Snapshot().Source).Definition!.Profiles.Single(p => p.Name == "section-a");
            foreach (var (expected, actual) in new[] { (saved.Upper, read.Upper), (saved.Lower, read.Lower) })
            {
                Equal(string.Join(',', expected.Knots), string.Join(',', actual.Knots));
                Equal(string.Join(',', expected.Ids), string.Join(',', actual.Ids));
                Equal(string.Join(';', expected.Points.Select(p => p[0] + "," + p[1])), string.Join(';', actual.Points.Select(p => p[0] + "," + p[1])));
                Equal(string.Join(';', expected.Tangents.Select(row => row.Id + row.Kind + row.Angle)), string.Join(';', actual.Tangents.Select(row => row.Id + row.Kind + row.Angle)));
            }
            Equal("section", reopened.Envelope().Accepted[^1].Edit!.Rail);
        });
    }

    private static Envelope FinishedMakeUnique(out byte[] finished)
    {
        using var session = Opened();
        string id = Id();
        var view = session.BeginSectionDraft(id, 0);
        view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
        view = session.ApplySectionStep(id, view.Generation, Raise(view, "cv-3", 0.01));
        session.FinishSection(Id(), session.AssessSection(id, view.Generation, CancellationToken.None));
        finished = session.Snapshot().Source;
        return session.Envelope();
    }

    private static Envelope WithLastEdit(Envelope envelope, Func<EditReceipt, EditReceipt> change) =>
        envelope with { Accepted = [.. envelope.Accepted[..^1], envelope.Accepted[^1] with { Edit = change(envelope.Accepted[^1].Edit!) }] };

    /// <summary>A synthetic project: an open row for <paramref name="root"/>, then one accepted row per edit, in order.</summary>
    private static Envelope Chain(byte[] root, params (string Rail, string VertexId, byte[] Bytes)[] edits)
    {
        var sources = new List<SourceRow>(); var designs = new List<DesignRow>(); var accepted = new List<AcceptedRow>(); var cursors = new List<CursorRow>();
        string? parent = null;
        foreach (var (rail, vertexId, bytes) in edits.Prepend(("", "", root)))
        {
            var parsed = FoilSource.Parse(bytes);
            if (!sources.Any(row => row.Id == parsed.SourceHash)) sources.Add(new(parsed.SourceHash, AuthoringSession.Chunks(bytes)));
            if (designs.Count == 0 || designs[^1].SurfaceHash != parsed.SurfaceHash)
                designs.Add(new(Id(), designs.Count == 0 ? null : designs[^1].Id, parsed.SurfaceHash!, "cfdw-cv/2"));
            string row = Id(), operation = Id();
            accepted.Add(new(row, parent, parsed.SourceHash, designs[^1].Id, operation, parent is null ? null : new EditReceipt(Id(), 1, rail, vertexId)));
            cursors.Add(new(cursors.Count, row, parent is null ? "open" : "apply", operation));
            parent = row;
        }
        return new("cfdw-project-1", Id(), sources.ToArray(), designs.ToArray(), accepted.ToArray(), cursors.ToArray(), null);
    }
}
