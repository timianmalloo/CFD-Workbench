using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class FairSessionTests
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
        Check("Fair_WithinTolerance_ApplyUndoRedo", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            string draft = Id();
            var begun = session.BeginProfileFair(draft, 0, SectionScope.Shared, 1e-4, PreserveEnds.Position);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.Construction is not null);
            Equal(true, assessment.Construction!.MaxDeviation <= 1e-4);
            Equal(1e-4, assessment.Construction.Tolerance);
            Equal(session.ProfileAt(0).Upper.Count, assessment.Construction.VertexCount);
            session.Apply(Id(), assessment);
            byte[] applied = session.Snapshot().Source.ToArray();
            Equal(2, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, applied.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        // ProfileFair.Fair always has a zero-change fallback (leave the curve exactly as
        // it is), so any non-negative tolerance is trivially satisfiable and Fair never
        // refuses on tolerance grounds; verified via a spike (ProfileFair internals are
        // out of scope to change) and reported as a defect worth a docs/comment fix in
        // ProfileFair. A negative tolerance is the only input observed to make
        // WithinTolerance false, and it exercises the session's refusal path faithfully.
        Check("Fair_UnsatisfiableTolerance_ApplyRefused", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            string draft = Id();
            var begun = session.BeginProfileFair(draft, 0, SectionScope.Shared, -1, PreserveEnds.Position);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(false, assessment.Status == GeometryStatus.Certified);
            Equal("DSL-GEOMETRY", assessment.Code);
            Equal(true, assessment.Construction is not null);
            try
            {
                session.Apply(Id(), assessment);
                throw new InvalidOperationException("expected refusal");
            }
            catch (ContractError error) { Equal("DSL-NOT-ASSESSED", error.Code); }
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(1, session.Envelope().Accepted.Length);
        });
        Check("Rebuild_TenVertices_CertifiedOneUndoItem", () =>
        {
            using var session = Opened();
            string draft = Id();
            var begun = session.BeginProfileRebuild(draft, 0, SectionScope.Shared, 10, 1e-2, PreserveEnds.Position);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.Construction is not null);
            Equal(10, assessment.Construction!.VertexCount);
            Equal(1e-2, assessment.Construction.Tolerance);
            session.Apply(Id(), assessment);
            Equal(10, session.ProfileAt(0).Upper.Count);
            Equal(10, session.ProfileAt(0).Lower.Count);
            session.Undo(Id());
            Equal(1, session.Envelope().Cursors.Count(row => row.Reason == "undo"));
        });
        Check("Fair_Cancel_RestoresExactBytes", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            string draft = Id();
            session.BeginProfileFair(draft, 0, SectionScope.Shared, 1e-4, PreserveEnds.Position);
            session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(null, session.Snapshot().Draft);
            Equal(1, session.Envelope().Accepted.Length);
        });
    }
}
