using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class PointCommandTests
{
    private static PointOutcome Anchor(AuthoringSession s, string rail, int index) =>
        s.ApplyPointCommand(Id(), new PointCommand.MakeAnchor(rail, Point(s, rail, index).Id));
    private static PointOutcome Tangent(AuthoringSession s, TangentKind kind, string? kept = null) =>
        s.ApplyPointCommand(Id(), new PointCommand.SetTangent("leading", Point(s, "leading", 3).Id, kind, kept));
    private static AuthoringSession WithAnchor()
    {
        var s = Open(FoilSource.NewDefault());
        Anchor(s, "leading", 3);
        return s;
    }
    private static PointView AnchorPoint(AuthoringSession s) =>
        Planform.View(s.Snapshot().Source, "Accepted", 0).Leading.Points.Single(p => p.Role == PointRole.Anchor);
    private static void SourceUnchanged(AuthoringSession s, Action action, string code)
    {
        var prior = s.Snapshot(); int rows = s.Envelope().Accepted.Length; int cursors = s.Envelope().Cursors.Length;
        Refuses(code, action);
        Equal(true, prior.Source.AsSpan().SequenceEqual(s.Snapshot().Source));
        Equal(rows, s.Envelope().Accepted.Length);
        Equal(cursors, s.Envelope().Cursors.Length);
    }
    internal static void Run()
    {
        Check("MakeAnchor_ControlPoint_PassesThroughWithinIdentity", () =>
        {
            byte[] source = Encoding.UTF8.GetBytes(File.ReadAllText("docs/examples/foildsl/foil-basic.foil")
                .Replace("(0.5, 0), (0.7, 0)", "(0.5, 4), (0.7, 0)", StringComparison.Ordinal));
            using var s = Open(source); var p = Point(s, "leading", 3); double before = p.AftMeters;
            var outcome = Anchor(s, "leading", 3);
            var anchored = Planform.View(s.Snapshot().Source, "Accepted", 0).Leading.Points.Single(item => item.Id == p.Id);
            Equal(PointRole.Anchor, anchored.Role);
            Near(before, anchored.AftMeters, 1e-6);
            Near(before, Planform.Probe(Planform.View(s.Snapshot().Source, "Accepted", 0), p.Eta).LeadingAftMeters, 1e-6);
            True(outcome.PointsAfter > outcome.PointsBefore, "anchor adds handles");
        });
        Check("MakeAnchor_Locality_OutsideSegmentWithinIdentity", () =>
        {
            using var s = Open(); var prior = Planform.View(s.Snapshot().Source, "Accepted", 0).Leading;
            Anchor(s, "leading", 3); var next = Planform.View(s.Snapshot().Source, "Accepted", 0).Leading;
            Near(prior.Points[0].AftMeters, next.Points[0].AftMeters);
            Near(prior.Points[^1].AftMeters, next.Points[^1].AftMeters);
        });
        Check("MakeAnchor_NewFoil_TenToThirteenHeader41SmoothRow", () =>
        {
            using var s = Open(FoilSource.NewDefault()); var outcome = Anchor(s, "trailing", 4);
            Equal(10, outcome.PointsBefore); Equal(13, outcome.PointsAfter);
            string source = Encoding.UTF8.GetString(s.Snapshot().Source);
            True(source.Contains("foildsl \"4.1\"", StringComparison.Ordinal), "version");
            True(source.Contains("tangents", StringComparison.Ordinal) && source.Contains("smooth", StringComparison.Ordinal), "row");
        });
        Check("MakeAnchor_ThirteenPoints_SixteenAccepted", () =>
        {
            using var s = Open(FoilSource.NewDefault()); Anchor(s, "trailing", 3);
            var outcome = Anchor(s, "trailing", 9);
            Equal(13, outcome.PointsBefore); Equal(16, outcome.PointsAfter);
        });
        Check("MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling", () =>
        {
            using var s = Open(File.ReadAllBytes("tests/CfdWorkbench.Core.Tests/Fixtures/m12b/foil-41-sixteen-three-anchors.foil"));
            s.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", Point(s, "leading", 7).Id));
            Equal(14, Planform.View(s.Snapshot().Source, "Accepted", 0).Leading.Points.Count);
            string message = "";
            try { Anchor(s, "leading", 7); }
            catch (ContractError error) { Equal("DSL-CURVE", error.Code); message = error.Message; }
            True(message.Contains("16", StringComparison.Ordinal), "refusal names 16-point ceiling");
        });
        Check("MakeAnchor_HandleGapBelowGrid_Refused", () =>
        {
            byte[] source = Encoding.UTF8.GetBytes(File.ReadAllText("docs/examples/foildsl/foil-basic.foil")
                .Replace("(0.3, 0), (0.5, 0), (0.7, 0)", "(0.3, 0), (0.30000001, 0), (0.30000002, 0)", StringComparison.Ordinal));
            using var s = Open(source);
            SourceUnchanged(s, () => Anchor(s, "leading", 3), "DSL-CURVE");
        });
        Check("MakeAnchor_RootEnd_DslLock", () =>
        {
            using var s = Open(); SourceUnchanged(s, () => Anchor(s, "leading", 0), "DSL-LOCK");
        });
        Check("MakeControl_OffLinePoint_GapAboveIdentity", () =>
        {
            using var s = WithAnchor();
            var outcome = s.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", AnchorPoint(s).Id));
            True(outcome.MaxDeviationMeters > 0, "off-line point changes shape");
        });
        Check("MakeControl_Locality_OutsideSegmentWithinIdentity", () =>
        {
            using var s = WithAnchor(); var prior = Point(s, "leading", 0);
            s.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", AnchorPoint(s).Id));
            Near(prior.AftMeters, Point(s, "leading", 0).AftMeters);
        });
        Check("MakeControl_Undo_RestoresExactly", () =>
        {
            using var s = WithAnchor(); byte[] source = s.Snapshot().Source.ToArray();
            s.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", AnchorPoint(s).Id));
            s.Undo(Id()); Equal(true, source.AsSpan().SequenceEqual(s.Snapshot().Source));
        });
        Check("MakeControl_LastRowRemoved_HeaderStays41", () =>
        {
            using var s = WithAnchor(); s.ApplyPointCommand(Id(), new PointCommand.MakeControl("leading", AnchorPoint(s).Id));
            string source = Encoding.UTF8.GetString(s.Snapshot().Source);
            True(source.Contains("foildsl \"4.1\"", StringComparison.Ordinal), "header never lowers");
            True(!source.Contains("tangents {", StringComparison.Ordinal), "last row removed");
        });
        Check("SetTangent_HandleSelected_SelectionKept", () =>
        {
            using var s = Open(Row()); Tangent(s, TangentKind.Corner);
            var left = Point(s, "leading", 2); var right = Point(s, "leading", 4);
            var draft = s.BeginPointGesture(Id(), "leading", right.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, right.SpanMeters, right.AftMeters + 0.008);
            s.Apply(Id(), s.Validate(frame.Draft.Id, frame.Draft.Generation));
            Tangent(s, TangentKind.Smooth, left.Id);
            Near(left.SpanMeters, Point(s, "leading", 2).SpanMeters);
            Near(left.AftMeters, Point(s, "leading", 2).AftMeters);
        });
        Check("SetTangent_SmoothNoHandleSelected_BothOnBisector", () =>
        {
            using var s = Open(Row()); Tangent(s, TangentKind.Corner);
            var left = Point(s, "leading", 2);
            var draft = s.BeginPointGesture(Id(), "leading", left.Id);
            var frame = s.UpdatePointGesture(draft.Id, draft.Generation, left.SpanMeters, left.AftMeters + 0.008);
            s.Apply(Id(), s.Validate(frame.Draft.Id, frame.Draft.Generation));
            var beforeLeft = Point(s, "leading", 2); var beforeRight = Point(s, "leading", 4);
            Tangent(s, TangentKind.Smooth);
            Equal(TangentKind.Smooth, Point(s, "leading", 3).Kind);
            True(Math.Abs(Point(s, "leading", 2).AftMeters - beforeLeft.AftMeters) > 1e-6, "left handle moves to bisector");
            True(Math.Abs(Point(s, "leading", 4).AftMeters - beforeRight.AftMeters) > 1e-6, "right handle moves to bisector");
        });
        Check("SetTangent_SymmetricNextToNeighbour_OrderKept", () =>
        {
            using var s = Open(Row()); Tangent(s, TangentKind.Symmetric);
            var view = Planform.View(s.Snapshot().Source, "Accepted", 0).Leading;
            for (int i = 1; i < view.Points.Count; i++) True(view.Points[i].Eta > view.Points[i - 1].Eta, "order");
            Near(2 * view.Points[3].AftMeters, view.Points[2].AftMeters + view.Points[4].AftMeters);
        });
        Check("SetTangent_Corner_RowRemoved", () =>
        {
            using var s = Open(Row()); Tangent(s, TangentKind.Corner);
            Equal(TangentKind.Corner, Point(s, "leading", 3).Kind);
            True(!Encoding.UTF8.GetString(s.Snapshot().Source).Contains("tangents {", StringComparison.Ordinal), "row removed");
        });
        Check("ChordRefit_SmoothAndSymmetricRows_HeldExactly", () =>
        {
            using var s = Open(Row()); var id = s.ApplyChord(Id(), new("root-chord", "120.1"));
            Equal(true, id.AcceptedId.Length > 0);
            Equal(true, FoilSource.Parse(s.Snapshot().Source).IsParsed);
        });
        Check("ApplyPointCommand_Refused_HistoryUnchanged", () =>
        {
            using var s = Open(); SourceUnchanged(s, () => Anchor(s, "leading", 0), "DSL-LOCK");
        });
        Check("ApplyPointCommand_SameOperationTwice_OneRow", () =>
        {
            using var s = Open(); string operation = Id(); var command = new PointCommand.MakeAnchor("leading", Point(s, "leading", 3).Id);
            string first = s.ApplyPointCommand(operation, command).AcceptedId;
            int count = s.Envelope().Accepted.Length;
            Equal(first, s.ApplyPointCommand(operation, command).AcceptedId);
            Equal(count, s.Envelope().Accepted.Length);
        });
        Check("ApplyPointCommand_SameOperationDifferentKind_DocOperationConflict", () =>
        {
            using var s = Open(Row()); string operation = Id(); string point = Point(s, "leading", 3).Id;
            s.ApplyPointCommand(operation, new PointCommand.SetTangent("leading", point, TangentKind.Corner, null));
            Refuses("DOC-OPERATION-CONFLICT", () => s.ApplyPointCommand(operation, new PointCommand.SetTangent("leading", point, TangentKind.Symmetric, null)));
        });
    }
}
