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
            var begun = session.BeginSectionFair(draft, 0, SectionScope.Shared, 1e-4, PreserveEnds.Position);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            var report = session.CurrentSectionDraft()!.Last!;
            Equal("ProfileFair.MaxDeviation", report.MaxChangeOracle);
            Equal(true, report.MaxChange <= 1e-4);
            Equal(session.ProfileAt(0).Upper.Count, report.UpperPoints);
            session.Apply(Id(), assessment);
            byte[] applied = session.Snapshot().Source.ToArray();
            Equal(2, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, applied.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        // The section API refuses an unsatisfiable Fair step before it enters the draft.
        Check("Fair_UnsatisfiableTolerance_StepRefused", () =>
        {
            using var session = Opened();
            byte[] original = session.Snapshot().Source.ToArray();
            string draft = Id();
            var begun = session.BeginSectionDraft(draft, 0);
            try
            {
                session.ApplySectionStep(draft, begun.Generation, new SectionStep.Fair(null, -1, PreserveEnds.Position));
                throw new InvalidOperationException("expected refusal");
            }
            catch (ContractError error) { Equal("DSL-GEOMETRY", error.Code); }
            Equal(0, session.CurrentSectionDraft()!.Cursor);
            Equal(true, original.AsSpan().SequenceEqual(session.CurrentSectionDraft()!.Bytes));
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(1, session.Envelope().Accepted.Length);
        });
        Check("Rebuild_TenVertices_CertifiedOneUndoItem", () =>
        {
            using var session = Opened();
            string draft = Id();
            var begun = session.BeginSectionRebuild(draft, 0, SectionScope.Shared, 10, 1e-2, PreserveEnds.Position);
            var assessment = session.Validate(draft, begun.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            var report = session.CurrentSectionDraft()!.Last!;
            Equal("ProfileFair.MaxDeviation", report.MaxChangeOracle);
            Equal(10, report.UpperPoints);
            Equal(10, report.LowerPoints);
            Equal(true, report.MaxChange <= 1e-2);
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
            session.BeginSectionFair(draft, 0, SectionScope.Shared, 1e-4, PreserveEnds.Position);
            session.Cancel(draft);
            Equal(true, original.AsSpan().SequenceEqual(session.Snapshot().Source));
            Equal(null, session.Snapshot().Draft);
            Equal(1, session.Envelope().Accepted.Length);
        });
    }
}
