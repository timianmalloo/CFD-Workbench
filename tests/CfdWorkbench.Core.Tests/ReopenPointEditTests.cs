using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class ReopenPointEditTests
{
    private static string Golden => M12bFixtures.Path("m12a-rail-recovery.cfdw");
    private static AuthoringSession Reopen(AuthoringSession source)
    {
        var next = new AuthoringSession(); next.Reopen(source.SaveImage()); return next;
    }
    internal static void Run()
    {
        Check("Reopen_SameOperationDifferentKind_DocOperationConflict", () =>
        {
            using var s = Open(Row()); string op = Id(); string point = Point(s, "leading", 3).Id;
            s.ApplyPointCommand(op, new PointCommand.SetTangent("leading", point, TangentKind.Corner, null));
            using var reopened = Reopen(s);
            Refuses("DOC-OPERATION-CONFLICT", () => reopened.ApplyPointCommand(op,
                new PointCommand.SetTangent("leading", point, TangentKind.Symmetric, null)));
        });
        Check("History_ReapplyOperationDifferentPayload_Refused", () =>
        {
            using var s = Open(); string op = Id(); string point = Point(s, "leading", 3).Id;
            s.ApplyPointCommand(op, new PointCommand.MakeAnchor("leading", point));
            Refuses("DOC-OPERATION-CONFLICT", () => s.ApplyPointCommand(op, new PointCommand.MakeControl("leading", point)));
            using var dimensions = Open(); string crossKind = Id();
            dimensions.ApplyDimension(crossKind, new("span", "1000"));
            Refuses("DOC-OPERATION-CONFLICT", () => dimensions.ApplyPointCommand(crossKind,
                new PointCommand.MakeAnchor("leading", Point(dimensions, "leading", 3).Id)));
            string cursorOperation = Id(); dimensions.Undo(cursorOperation);
            Refuses("DOC-OPERATION-CONFLICT", () => dimensions.ApplyPointCommand(cursorOperation,
                new PointCommand.MakeAnchor("leading", Point(dimensions, "leading", 3).Id)));
        });
        Check("Reopen_RetrySamePointOperationId_ReturnsPriorId", () =>
        {
            using var s = Open(); string op = Id(); var command = new PointCommand.MakeAnchor("leading", Point(s, "leading", 3).Id);
            var first = s.ApplyPointCommand(op, command);
            using var reopened = Reopen(s); int count = reopened.Envelope().Accepted.Length;
            Equal(first.AcceptedId, reopened.ApplyPointCommand(op, command).AcceptedId);
            Equal(count, reopened.Envelope().Accepted.Length);
        });
        Check("Reopen_PointTypeAndTangentRows_UndoRedoRoundTrip", () =>
        {
            using var s = Open(); string vertex = Point(s, "leading", 3).Id;
            var outcome = s.ApplyPointCommand(Id(), new PointCommand.MakeAnchor("leading", vertex));
            using var reopened = Reopen(s);
            Equal(PointRole.Anchor, Planform.View(reopened.Snapshot().Source, "Accepted", 0).Leading.Points.Single(p => p.Id == vertex).Role);
            reopened.Undo(Id()); reopened.Redo(Id());
            Equal(outcome.AcceptedId, reopened.Snapshot().AcceptedId);
        });
        Check("Reopen_TwoDGestureRow_UndoRedoRoundTrip", () =>
        {
            using var s = Open(); var point = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", point.Id);
            var frame = s.UpdatePointGesture(d.Id, d.Generation, point.SpanMeters + 0.002, point.Ordinate + 0.003);
            s.Apply(Id(), s.Validate(frame.Draft.Id, frame.Draft.Generation));
            using var reopened = Reopen(s);
            Near(frame.SpanMeters, Point(reopened, "trailing", 2).SpanMeters);
            reopened.Undo(Id()); reopened.Redo(Id());
            Near(frame.Ordinate, Point(reopened, "trailing", 2).Ordinate);
        });
        Check("Reopen_CurveOnGestureReceipt_DocReference", () =>
        {
            using var s = Open(); var point = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", point.Id);
            var frame = s.UpdatePointGesture(d.Id, d.Generation, point.SpanMeters, point.Ordinate + 0.003);
            s.Apply(Id(), s.Validate(frame.Draft.Id, frame.Draft.Generation));
            var env = s.Envelope(); var rows = env.Accepted.ToArray();
            rows[1] = rows[1] with { Edit = rows[1].Edit! with { Curve = "leading" } };
            using var next = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => next.Reopen(NativeProject.Encode(env with { Accepted = rows })));
        });
        Check("Save_DragAndSpanOnlyHistory_NoNewReceiptKeys", () =>
        {
            using var s = Open(); var point = Point(s, "trailing", 2); var d = s.BeginPointGesture(Id(), "trailing", point.Id);
            var frame = s.UpdatePointGesture(d.Id, d.Generation, point.SpanMeters, point.Ordinate + 0.003);
            s.Apply(Id(), s.Validate(frame.Draft.Id, frame.Draft.Generation));
            s.ApplyDimension(Id(), new("span", "1000"));
            string image = Encoding.UTF8.GetString(s.SaveImage());
            True(!image.Contains("\"curve\"", StringComparison.Ordinal), "no curve key");
            True(!image.Contains("\"rule\"", StringComparison.Ordinal), "no rule key");
        });
        Check("Recovery_PointTypeRail_RefusedByRecoveryReference", () =>
        {
            using var s = Open(); var image = s.Envelope(); string point = Point(s, "leading", 3).Id;
            var forged = image with { Recovery = new(Id(), s.Snapshot().AcceptedId, 0, "point-type", point, AuthoringSession.Chunks(s.Snapshot().Source)) };
            using var next = new AuthoringSession();
            Refuses("DOC-REFERENCE", () => next.Reopen(NativeProject.Encode(forged)));
        });
        Check("Recovery_M12aGoldenRailDraft_ApplyOneRowNoCurve", () =>
        {
            using var s = new AuthoringSession(); s.Reopen(File.ReadAllBytes(Golden));
            var recovery = s.Snapshot().Recovery!; int rows = s.Envelope().Accepted.Length;
            s.ResumeRecovery(); var draft = s.Snapshot().Draft!;
            s.Apply(Id(), s.Validate(draft.Id, draft.Generation));
            Equal(rows + 1, s.Envelope().Accepted.Length);
            Equal(null, s.Envelope().Accepted[^1].Edit!.Curve);
        });
        Check("Recovery_M12aGoldenRailDraft_DiscardClears", () =>
        {
            using var s = new AuthoringSession(); s.Reopen(File.ReadAllBytes(Golden));
            int rows = s.Envelope().Accepted.Length;
            True(s.Snapshot().Recovery is not null, "golden recovery present");
            s.DiscardRecovery(); Equal(null, s.Snapshot().Recovery);
            Equal(rows, s.Envelope().Accepted.Length);
        });
    }
}
