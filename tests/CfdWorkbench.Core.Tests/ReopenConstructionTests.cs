using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Envelope reopen must recognize insert/delete/fair/rebuild construction receipts
// and recovery drafts (GEO-14, DSL-10) the same way it already recognizes rail
// and profile-vertex edits, or a saved project with one of these applied (or
// captured mid-construction) refuses to reopen.
internal static class ReopenConstructionTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        return session;
    }

    internal static void Run()
    {
        Check("Reopen_Insert_AcceptedRoundtripsUndoRedo", () =>
        {
            using var session = Opened();
            AppliedRoundtrip(session, (s, draft) => s.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37));
        });
        Check("Reopen_Delete_AcceptedRoundtripsUndoRedo", () =>
        {
            using var session = Opened();
            AppliedRoundtrip(session, (s, draft) => s.BeginProfileDelete(draft, 0, SectionScope.Shared, 3));
        });
        Check("Reopen_Fair_AcceptedRoundtripsUndoRedo", () =>
        {
            using var session = Opened();
            AppliedRoundtrip(session, (s, draft) => s.BeginProfileFair(draft, 0, SectionScope.Shared, 1e-4, PreserveEnds.Position));
        });
        Check("Reopen_Rebuild_AcceptedRoundtripsUndoRedo", () =>
        {
            using var session = Opened();
            AppliedRoundtrip(session, (s, draft) => s.BeginProfileRebuild(draft, 0, SectionScope.Shared, 10, 1e-2, PreserveEnds.Position));
        });
        Check("Reopen_RecoveryMidInsert_ResumesSameDraftBytes", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37);
            RecoveryRoundtrip(session, draft);
        });
        Check("Reopen_RecoveryMidRebuild_ResumesSameDraftBytes", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginProfileRebuild(draft, 0, SectionScope.Shared, 10, 1e-2, PreserveEnds.Position);
            RecoveryRoundtrip(session, draft);
        });
        Check("Reopen_BogusEditRail_StillRefusesDocReference", () =>
        {
            using var session = Opened(); string draft = Id();
            session.BeginRailEdit(draft, "leading", "cv-2"); session.UpdateDraft(draft, 0, .01);
            session.Apply(Id(), session.Validate(draft, 1));
            var envelope = session.Envelope();
            var bogus = envelope.Accepted[1] with { Edit = envelope.Accepted[1].Edit! with { Rail = "bogus" } };
            var bad = envelope with { Accepted = [envelope.Accepted[0], bogus] };
            var reopened = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => reopened.Reopen(NativeProject.Encode(bad)));
        });
        Check("Reopen_InsertThenDelete_NeverThrows", () =>
        {
            using var session = Opened();
            var inserted = session.BeginProfileInsert(Id(), 0, SectionScope.Shared, 0.37);
            var insertAssessment = session.Validate(inserted.Id, inserted.Generation);
            Equal(GeometryStatus.Certified, insertAssessment.Status);
            session.Apply(Id(), insertAssessment);
            int insertedIndex = Array.FindIndex(session.ProfileAt(0).Upper.ToArray(), v => v.Id == inserted.VertexId);
            Equal(true, insertedIndex >= 0);
            var deleted = session.BeginProfileDelete(Id(), 0, SectionScope.Shared, insertedIndex);
            var deleteAssessment = session.Validate(deleted.Id, deleted.Generation);
            session.Apply(Id(), deleteAssessment);
            byte[] saved = session.SaveImage();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using var reopened = new AuthoringSession();
            Exception? thrown = null;
            try { reopened.Reopen(saved); } catch (Exception failure) { thrown = failure; }
            watch.Stop();
            Equal((Exception?)null, thrown);
            var inspection = reopened.InspectAccepted();
            string outcome = inspection.Geometry.Status == GeometryStatus.Certified ? "Certified" : inspection.Geometry.Status.ToString();
            Console.WriteLine($"BUDGET-CASE: {outcome} {watch.Elapsed.TotalMilliseconds:F1}");
            if (inspection.Geometry.Status != GeometryStatus.Certified)
            {
                Equal(GeometryStatus.NotAssessed, inspection.Geometry.Status);
                Equal("GEOMETRY-BUDGET", inspection.Geometry.Code);
            }
            Equal(true, session.Snapshot().Source.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        });
        Check("Reopen_ForcedBudgetExhaustion_OpensNotAssessed", () =>
        {
            using var session = Opened();
            AppliedRoundtrip(session, (s, draft) => s.BeginProfileInsert(draft, 0, SectionScope.Shared, 0.37));
            byte[] saved = session.SaveImage();
            using var reopened = new AuthoringSession(TimeSpan.Zero);
            reopened.Reopen(saved);
            var inspection = reopened.InspectAccepted();
            Equal(GeometryStatus.NotAssessed, inspection.Geometry.Status);
            Equal("GEOMETRY-BUDGET", inspection.Geometry.Code);
            Equal(true, session.Snapshot().Source.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        });
    }

    private static void AppliedRoundtrip(AuthoringSession session, Func<AuthoringSession, string, SessionDraft> begin)
    {
        byte[] original = session.Snapshot().Source.ToArray();
        string draft = Id();
        var begun = begin(session, draft);
        var assessment = session.Validate(draft, begun.Generation);
        Equal(GeometryStatus.Certified, assessment.Status);
        session.Apply(Id(), assessment);
        byte[] applied = session.Snapshot().Source.ToArray();
        byte[] saved = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(saved);
        Equal(true, applied.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        reopened.Undo(Id());
        Equal(true, original.AsSpan().SequenceEqual(reopened.Snapshot().Source));
        reopened.Redo(Id());
        Equal(true, applied.AsSpan().SequenceEqual(reopened.Snapshot().Source));
    }

    private static void RecoveryRoundtrip(AuthoringSession session, string draft)
    {
        byte[] draftBytes = session.Snapshot().Draft!.Bytes.ToArray();
        long generation = session.Snapshot().Draft!.Generation;
        session.CaptureRecovery();
        byte[] saved = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(saved);
        reopened.ResumeRecovery();
        Equal(generation, reopened.Snapshot().Draft!.Generation);
        Equal(true, draftBytes.AsSpan().SequenceEqual(reopened.Snapshot().Draft!.Bytes));
        reopened.Cancel(draft);
    }
}
