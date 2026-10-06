using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

// Design group-move-node-m §9 (Desktop rows), Rulings 107, 111, 116: select several points of one curve and drag, nudge or type
// a value to move them as one rigid, undoable group. Controller checks need no window (ring --controller-shell, tens of ms each);
// the pane, canvas and elevation checks open the Example foil in a window (ring --properties-cells, one window each).
// Wording asserted: COPY-G1..G12 and design §5a as approved by Ruling 116.
public static class GroupDragTests
{
    private const string Tip = "Tip chord is at its minimum, 5 mm.";

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static WorkbenchController Example()
    {
        var controller = new WorkbenchController();
        controller.OpenExampleAsync().GetAwaiter().GetResult();
        return controller;
    }

    private static PointView At(WorkbenchController c, string curve, int index) => c.CurveFor(curve)!.Points[index];

    private static PointRef Ref(WorkbenchController c, string curve, int index) => new(curve, At(c, curve, index).Id);

    private static void Pick(WorkbenchController c, string curve, params int[] indexes) =>
        c.Select(new Selection.Points(indexes.Select(index => Ref(c, curve, index)).ToArray()));

    private static void DragTo(WorkbenchController c, double span, double aft)
    {
        c.UpdateGesture(span, aft, 50);
        c.FlushGestureFrame();
    }

    private static string Report(WorkbenchController c, string curve, params int[] indexes) =>
        string.Join(" ", indexes.Select(index => $"{At(c, curve, index).SpanMeters * 1000:F2},{At(c, curve, index).Ordinate * 1000:F2}"));

    public static void RunController()
    {
        DesktopChecks.Check("GroupDrag_PressOnMember_KeepsSelection", () =>
        {
            using var c = Example();
            Pick(c, "trailing", 2, 3, 4);
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin refused");
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "the press dropped the group: " + c.Selection);
            Require(c.GestureGroup is { Count: 3 } && c.Gesture == GestureState.Pressed, "no group gesture");
            c.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "Escape dropped the group");
        });
        DesktopChecks.Check("GroupDrag_ClickWithoutDrag_CollapsesOnRelease", () =>
        {
            using var c = Example();
            string before = c.AcceptedSource;
            Pick(c, "trailing", 2, 3, 4);
            var grabbed = Ref(c, "trailing", 3);
            Require(c.BeginGesture(grabbed, GestureInput.Pointer), "begin refused");
            c.UpdateGesture(At(c, "trailing", 3).SpanMeters + 0.0001, At(c, "trailing", 3).Ordinate, 1);   // 1 px: under the 3 px threshold
            c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            Require(c.Selection is Selection.Points { Items: [var only] } && only == grabbed, "release did not collapse to the pressed point");
            Require(c.AcceptedSource == before && c.Draft is null, "a click changed the source");
        });
        DesktopChecks.Check("GroupDrag_OneUndoStep_EscapeNone_AnalysisInert", () =>
        {
            using var c = Example();
            string before = c.AcceptedSource;
            Pick(c, "trailing", 2, 3, 4);
            var start = new[] { 2, 3, 4 }.Select(index => At(c, "trailing", index)).ToArray();
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin refused");
            DragTo(c, start[1].SpanMeters + 0.010, start[1].Ordinate + 0.005);
            Require(c.Status == "Moving 3 trailing edge points.", "strip while dragging: " + c.Status);
            for (int member = 0; member < 3; member++)
            {
                var now = At(c, "trailing", member + 2);
                Require(Math.Abs(now.SpanMeters - start[member].SpanMeters - 0.010) < 1.1e-6 && Math.Abs(now.Ordinate - start[member].Ordinate - 0.005) < 1.1e-6,
                    $"member {member} did not move by the one delta: {now.SpanMeters - start[member].SpanMeters} {now.Ordinate - start[member].Ordinate}");
            }
            var outcome = c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            Require(outcome is GestureOutcome.Committed { Report: "Moved 3 trailing edge points." }, "release: " + outcome);
            Require(c.Status == "Moved 3 trailing edge points.", "strip after release: " + c.Status);
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore every point");
            // Escape: nothing changes and the group stays selected.
            Pick(c, "trailing", 2, 3, 4);
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin refused (Escape)");
            DragTo(c, start[1].SpanMeters + 0.010, start[1].Ordinate + 0.005);
            c.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
            Require(c.AcceptedSource == before && c.Draft is null && c.Selection is Selection.Points { Items.Count: 3 }, "Escape left a change or dropped the group");
            // Analysis: selecting works, the drag does not begin.
            (typeof(WorkbenchController).GetMethod("ToggleAnalysis") ?? throw new InvalidOperationException("no Analysis toggle")).Invoke(c, null);
            Require(!c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer) && c.GestureGroup is null, "Analysis began a group gesture");
        });
        DesktopChecks.Check("GroupDrag_HoldsRigid_GrabNotTheBinder_AppliedNotRequested", () =>
        {
            // Mockup frame D: grab point 4 (an interior control point), the tip (point 7) binds. The strip, the inspector and the
            // readout show the move that was applied; only the tether keeps the request.
            using var c = Example();
            Pick(c, "trailing", 3, 6);
            var (grab, tip) = (At(c, "trailing", 3), At(c, "trailing", 6));
            string before = c.AcceptedSource;
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin refused");
            DragTo(c, grab.SpanMeters, grab.Ordinate - 0.200);
            Require(c.GestureBinding is { Kind: "TipMinimum" } binding && binding.Point.VertexId == tip.Id, "binding: " + c.GestureBinding);
            Require(c.GestureLimitPoint == c.GestureBinding!.Point, "the warn outline is not on the binding member");
            Require(c.Status == Tip, "strip: " + c.Status);
            var applied = c.GestureApplied!.Value;
            Require(Math.Abs(applied.Ordinate - (grab.Ordinate - 0.115)) < 1.1e-6, "applied " + applied.Ordinate);
            Require(Math.Abs(c.GestureRequested!.Value.Ordinate - (grab.Ordinate - 0.200)) < 1e-9, "the request was not kept for the tether");
            Require(c.GroupHold == "Applied −115.00 mm · held by point 7", "inspector line: " + c.GroupHold);
            Require(Math.Abs(At(c, "trailing", 3).Ordinate - At(c, "trailing", 6).Ordinate) < 1.1e-6 &&
                Math.Abs(At(c, "trailing", 6).Ordinate - 0.005) < 1.1e-6, "the group changed shape or missed the 5 mm tip");
            var outcome = c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            Require(outcome is GestureOutcome.Committed { Report: "Moved 2 trailing edge points. Tip chord 5.00 mm." }, "release: " + outcome);
            c.Undo();
            Require(c.AcceptedSource == before, "Undo did not restore");
            var end = ShellEvents.Read().Last(item => item.Name == "gesture.end");
            Require(end.ClampReason == "TipMinimum:trailing edge 7", "clamp reason names the binding point: " + end.ClampReason);
        });
        DesktopChecks.Check("GroupDrag_NeighbourHolds_NamesThePoint", () =>
        {
            using var c = Example();
            Pick(c, "trailing", 2, 3);
            var grab = At(c, "trailing", 3);
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin refused");
            double gapBefore = grab.SpanMeters - At(c, "trailing", 2).SpanMeters;
            DragTo(c, grab.SpanMeters + 0.300, grab.Ordinate);
            Require(c.GestureBinding is { Kind: "Neighbour" } held && held.Point.VertexId == At(c, "trailing", 4).Id, "binding: " + c.GestureBinding);
            Require(c.Status == "The selection is held by point 5. Points can't close up on a neighbour.", "strip: " + c.Status);
            Require(Math.Abs(At(c, "trailing", 3).SpanMeters - At(c, "trailing", 2).SpanMeters - gapBefore) < 1.1e-6, "the group deformed at the neighbour");
            c.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
        });
        DesktopChecks.Check("GroupDrag_RootSeeded_HeldAxisSaysSo_LockedMemberRefusesBeforeADraft", () =>
        {
            using var c = Example();
            // {1, 2} on the trailing rail: the root joins; its span lock holds the group's span (COPY-G4, tokenised).
            Pick(c, "trailing", 1, 2);
            var (root, two) = (At(c, "trailing", 0), At(c, "trailing", 2));
            Require(c.BeginGesture(Ref(c, "trailing", 2), GestureInput.Pointer), "begin refused");
            DragTo(c, two.SpanMeters + 0.020, two.Ordinate + 0.004);
            Require(c.Status == "The root end can't move along the span, so the selection moves in aft only.", "strip: " + c.Status);
            Require(Math.Abs(At(c, "trailing", 2).SpanMeters - two.SpanMeters) < 1e-9 && Math.Abs(At(c, "trailing", 2).Ordinate - two.Ordinate - 0.004) < 1.1e-6, "the span was not held");
            Require(Math.Abs(At(c, "trailing", 0).Ordinate - root.Ordinate - 0.004) < 1.1e-6, "the root was not seeded into the group");
            c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            // The leading root is fixed: the group is refused before any draft opens and the strip names it (COPY-G3).
            Pick(c, "leading", 1, 2);
            string before = c.AcceptedSource;
            Require(c.BeginGesture(Ref(c, "leading", 2), GestureInput.Pointer), "begin refused");
            c.UpdateGesture(0.2, 0.01, 50);
            Require(c.Draft is null && c.Gesture == GestureState.Idle && c.Status == "Point 1 is locked. Deselect it to move the others.", "locked: " + c.Status);
            Require(c.Selection is Selection.Points { Items.Count: 2 } && c.AcceptedSource == before, "the refusal changed the selection or the source");
        });
        DesktopChecks.Check("GroupDrag_TwoCurves_HandleWithoutAnchor_RefuseWithTheirWords", () =>
        {
            using var c = Example();
            c.Select(new Selection.Points([Ref(c, "trailing", 2), Ref(c, "leading", 2)]));
            Require(c.BeginGesture(Ref(c, "trailing", 2), GestureInput.Pointer), "begin refused");
            c.UpdateGesture(0.2, 0.1, 50);
            Require(c.Draft is null && c.Status == "Select points on one curve to move them together.", "two curves: " + c.Status);
            Require(c.ApplyGroupValueAsync(GroupValueMode.MoveBy, GroupValueAxis.Value, 0.001).GetAwaiter().GetResult() is CommitOutcome.Refused { Copy: "Select points on one curve to move them together." },
                "typed entry on two curves");
            var made = c.ApplyPointCommandAsync(new PointCommand.MakeAnchor("trailing", At(c, "trailing", 3).Id)).GetAwaiter().GetResult();
            Require(made is CommitOutcome.Committed, "fixture: make anchor refused");
            var handle = c.CurveFor("trailing")!.Points.First(point => point.Role == PointRole.AnchorHandle);
            Pick(c, "trailing", handle.Index, 2);
            Require(c.BeginGesture(new PointRef("trailing", handle.Id), GestureInput.Keyboard) == false &&
                c.Status == "Handles move on their own, or with their anchor. Deselect the handle or select its anchor.", "handle: " + c.Status);
        });
        DesktopChecks.Check("GroupNudge_TenPressesPastLimit_OneAnnouncement_OneUndoRow", () =>
        {
            using var c = Example();
            string before = c.AcceptedSource;
            Pick(c, "trailing", 3, 6);
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Keyboard), "begin refused");
            var held = new List<long>();
            for (int press = 0; press < 130; press++)
            {
                c.Nudge(0, -1, NudgeModifier.Shift);
                if (c.Status == Tip) held.Add(c.StatusVersion);
            }
            Require(c.GestureLimit is { Kind: GestureLimitKind.TipMinimum } && held.Count > 10, "the run never reached the limit");
            Require(held.Distinct().Count() == 1, $"the hold was written {held.Distinct().Count()} times; it is one announcement");
            Require(Math.Abs(At(c, "trailing", 6).Ordinate - 0.005) < 1.1e-6 && Math.Abs(At(c, "trailing", 3).Ordinate - 0.005) < 1.1e-6, "held values");
            c.EndGestureAsync(GestureEnd.KeyUp).GetAwaiter().GetResult();
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore the whole run");
        });
    }

    public static void RunPane()
    {
    }
}
