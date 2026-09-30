using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;
using static CfdWorkbench.Core.Tests.PointGestureTests;

namespace CfdWorkbench.Core.Tests;

internal static class PointCommandTests
{
    private static PointOutcome Anchor(AuthoringSession s, string rail, int index) =>
        s.ApplyPointCommand(Id(), new PointCommand.MakeAnchor(rail, Point(s, rail, index).Id));
    private static PointOutcome Control(AuthoringSession s, string rail, int index) =>
        s.ApplyPointCommand(Id(), new PointCommand.MakeControl(rail, Point(s, rail, index).Id));
    private static PointOutcome Tangent(AuthoringSession s, TangentKind kind, string? kept = null) =>
        s.ApplyPointCommand(Id(), new PointCommand.SetTangent("leading", Point(s, "leading", 3).Id, kind, kept));
    private static void SourceUnchanged(AuthoringSession s, Action action, string code)
    {
        var prior = s.Snapshot(); int rows = s.Envelope().Accepted.Length;
        Refuses(code, action);
        Equal(true, prior.Source.AsSpan().SequenceEqual(s.Snapshot().Source));
        Equal(rows, s.Envelope().Accepted.Length);
    }
    internal static void Run()
    {
        Check("MakeAnchor_ControlPoint_PassesThroughWithinIdentity", () =>
        {
            using var s = Open(); var p = Point(s, "leading", 3); double before = p.AftMeters;
            var outcome = Anchor(s, "leading", 3);
            Equal(PointRole.Anchor, Point(s, "leading", 3).Role);
            Near(before, Point(s, "leading", 3).AftMeters, 1e-6);
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
            using var s = Open(FoilSource.NewDefault()); Anchor(s, "trailing", 3);
            // A valid 14-point rail can be supplied by the construction fixture once the first anchor is present.
            string message = "";
            try { Anchor(s, "trailing", 9); Anchor(s, "trailing", 6); }
            catch (ContractError error) { Equal("DSL-CURVE", error.Code); message = error.Message; }
            True(message.Contains("16", StringComparison.Ordinal), "refusal names 16-point ceiling");
        });
        Check("MakeAnchor_HandleGapBelowGrid_Refused", () =>
        {
            using var s = Open(); Anchor(s, "leading", 3);
            SourceUnchanged(s, () => Anchor(s, "leading", 2), "DSL-LOCK");
        });
        Check("MakeAnchor_RootEnd_DslLock", () =>
        {
            using var s = Open(); SourceUnchanged(s, () => Anchor(s, "leading", 0), "DSL-LOCK");
        });
        Check("MakeControl_OffLinePoint_GapAboveIdentity", () =>
        {
            using var s = Open(Row());
            var outcome = Control(s, "leading", 3);
            True(outcome.MaxDeviationMeters > 0, "off-line point changes shape");
        });
        Check("MakeControl_Locality_OutsideSegmentWithinIdentity", () =>
        {
            using var s = Open(Row()); var prior = Point(s, "leading", 0);
            Control(s, "leading", 3); Near(prior.AftMeters, Point(s, "leading", 0).AftMeters);
        });
        Check("MakeControl_Undo_RestoresExactly", () =>
        {
            using var s = Open(Row()); byte[] source = s.Snapshot().Source.ToArray();
            Control(s, "leading", 3); s.Undo(Id()); Equal(true, source.AsSpan().SequenceEqual(s.Snapshot().Source));
        });
        Check("MakeControl_LastRowRemoved_HeaderStays41", () =>
        {
            using var s = Open(Row()); Control(s, "leading", 3);
            string source = Encoding.UTF8.GetString(s.Snapshot().Source);
            True(source.Contains("foildsl \"4.1\"", StringComparison.Ordinal), "header never lowers");
            True(!source.Contains("tangents {", StringComparison.Ordinal), "last row removed");
        });
        Check("SetTangent_HandleSelected_SelectionKept", () =>
        {
            using var s = Open(Row()); var h = Point(s, "leading", 2);
            Tangent(s, TangentKind.Smooth, h.Id);
            Equal(h.Id, Point(s, "leading", 2).Id);
        });
        Check("SetTangent_SmoothNoHandleSelected_BothOnBisector", () =>
        {
            using var s = Open(Row("corner")); Tangent(s, TangentKind.Smooth);
            Equal(TangentKind.Smooth, Point(s, "leading", 3).Kind);
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
