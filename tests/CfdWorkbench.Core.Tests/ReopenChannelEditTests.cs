using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class ReopenChannelEditTests
{
    private static AuthoringSession Reopen(AuthoringSession source)
    {
        var next = new AuthoringSession();
        next.Reopen(source.SaveImage());
        return next;
    }

    internal static void Run()
    {
        Check("Reopen_ChannelGestureRows_UndoRedoRoundTrip", () =>
        {
            using var session = Open();
            var point = ChannelEditTests.ChannelPoint(session, "dihedral", 2);
            var draft = session.BeginPointGesture(Id(), "dihedral", point.Id);
            var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + 0.002);
            session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));
            byte[] edited = session.Snapshot().Source.ToArray();
            using var reopened = Reopen(session);
            var edit = reopened.Envelope().Accepted[^1].Edit!;
            Equal("dihedral", edit.Rail);
            Equal(null, edit.Curve);
            True(edited.AsSpan().SequenceEqual(reopened.Snapshot().Source), "reopen keeps the edit");
            reopened.Undo(Id());
            reopened.Redo(Id());
            True(edited.AsSpan().SequenceEqual(reopened.Snapshot().Source), "redo restores the edit");
            Equal("dihedral", reopened.Envelope().Accepted[^1].Edit!.Rail);
        });
        Check("Reopen_ChannelPointTypeAndTangentRows_UndoRedoRoundTrip", () =>
        {
            using var session = Open(File.ReadAllBytes(M12bFixtures.Path("foil-41-sixteen-three-anchors.foil")));
            string id = ChannelEditTests.ChannelPoint(session, "twist", 7).Id;
            session.ApplyPointCommand(Id(), new PointCommand.SetTangent("twist", id, TangentKind.Symmetric, null));
            using var reopened = Reopen(session);
            Equal(TangentKind.Symmetric, ChannelEditTests.Channel(reopened, "twist").Points.Single(point => point.Id == id).Kind);
            reopened.Undo(Id());
            Equal(TangentKind.Corner, ChannelEditTests.Channel(reopened, "twist").Points.Single(point => point.Id == id).Kind);
            reopened.Redo(Id());
            Equal(TangentKind.Symmetric, ChannelEditTests.Channel(reopened, "twist").Points.Single(point => point.Id == id).Kind);
            Equal("twist", reopened.Envelope().Accepted[^1].Edit!.Curve);
        });
        Check("Reopen_UnknownCurveOnReceipt_DocReference", () =>
        {
            using var session = Open();
            var point = ChannelEditTests.ChannelPoint(session, "dihedral", 2);
            var draft = session.BeginPointGesture(Id(), "dihedral", point.Id);
            var frame = session.UpdatePointGesture(draft.Id, draft.Generation, point.SpanMeters, point.Ordinate + 0.002);
            session.Apply(Id(), session.Validate(frame.Draft.Id, frame.Draft.Generation));
            var envelope = session.Envelope();
            var rows = envelope.Accepted.ToArray();
            rows[^1] = rows[^1] with { Edit = rows[^1].Edit! with { Rail = "camber" } };
            using var next = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => next.Reopen(NativeProject.Encode(envelope with { Accepted = rows })));
        });
        Check("Recovery_ChannelGestureRail_RefusedProjectStillOpens", () =>
        {
            using var session = Open();
            var image = session.Envelope();
            var forged = image with
            {
                Recovery = new RecoveryRow(Id(), session.Snapshot().AcceptedId, 0, "twist",
                    ChannelEditTests.ChannelPoint(session, "twist", 3).Id, AuthoringSession.Chunks(session.Snapshot().Source))
            };
            using var refused = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => refused.Reopen(NativeProject.Encode(forged)));
            using var opened = Reopen(session);
            True(opened.Snapshot().Source.AsSpan().SequenceEqual(session.Snapshot().Source), "the honest image still opens");
        });
    }
}
