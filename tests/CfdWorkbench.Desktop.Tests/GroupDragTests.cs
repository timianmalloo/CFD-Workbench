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

    // ---------------------------------------------------------------- window checks

    private sealed class Rig : IDisposable
    {
        public WorkbenchController Controller { get; } = new();
        public ShellHost Host { get; }
        public Window Window { get; }
        public PlanCanvas Canvas => Host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
        public ElevationView Side => Host.ModelView.FindControl<ElevationView>("SideElevation")!;

        public Rig()
        {
            PropertiesViewTests.Pump(Controller.OpenExampleAsync());
            Host = new ShellHost(Controller);
            Window = new Window { Content = Host, Width = 1400, Height = 1000 };
            Window.Show();
            Host.RefreshPanes();
            Controller.Layout = ViewLayout.Four;
            Settle();
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while ((Controller.Surface is null || Controller.SurfaceUpdating || Side.Camera is null) && deadline.Elapsed.TotalSeconds < 30)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Yield();
            }
            Settle();
        }

        public void Settle()
        {
            for (int i = 0; i < 6; i++)
            {
                Dispatcher.UIThread.RunJobs();
                Window.UpdateLayout();
            }
        }

        public Point Win(Visual visual, Point local) => visual.TranslatePoint(local, Window) ?? throw new InvalidOperationException("no window point");

        public Pointer Press(InputElement target, Point local, int clicks = 1, KeyModifiers modifiers = KeyModifiers.None, bool right = false)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var (raw, kind) = right ? (RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed)
                : (RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
            target.RaiseEvent(new PointerPressedEventArgs(target, pointer, Window, Win((Visual)target, local), 1,
                new PointerPointProperties(raw, kind), modifiers, clicks));
            Settle();
            return pointer;
        }

        public void Move(InputElement target, Pointer pointer, Point local)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, target, pointer, Window, Win((Visual)target, local), 2,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            Settle();
        }

        public void Release(InputElement target, Pointer pointer, Point local, bool right = false)
        {
            target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, Window, Win((Visual)target, local), 3,
                new PointerPointProperties(RawInputModifiers.None, right ? PointerUpdateKind.RightButtonReleased : PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None, right ? MouseButton.Right : MouseButton.Left));
            pointer.Dispose();
            Pump();
        }

        public void Key(InputElement target, Key key, KeyModifiers modifiers = KeyModifiers.None, bool up = false)
        {
            target.RaiseEvent(new KeyEventArgs { RoutedEvent = up ? InputElement.KeyUpEvent : InputElement.KeyDownEvent, Source = target, Key = key, KeyModifiers = modifiers });
            Settle();
        }

        /// <summary>Lets a release's validate-and-apply finish.</summary>
        public void Pump()
        {
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while (Controller.Gesture != GestureState.Idle && deadline.Elapsed.TotalSeconds < 10)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Yield();
            }
            Settle();
        }

        public void Dispose()
        {
            Window.Close();
            Controller.Dispose();
        }
    }

    private static void Pump(Task task) => PropertiesViewTests.Pump(task);

    private static void TypeInto(Rig rig, TextBox box, string text)
    {
        box.Focus();
        box.Text = text;
        rig.Key(box, Avalonia.Input.Key.Enter);
        rig.Settle();
    }

    private static void Check(string name, Action<Rig> body) => DesktopChecks.Check(name, () =>
    {
        using var rig = new Rig();
        body(rig);
    });

    private static TextBox Aft(Rig rig) => PropertiesViewTests.Need<TextBox>(rig.Host.Properties, "PointAftInput");

    private static string Msg(Rig rig, string key) => PropertiesViewTests.Text(rig.Host.Properties, key);

    public static void RunPane()
    {
        Check("Properties_MultiplePoints_SharedValueShown_MixedWhereDiffer", rig =>
        {
            var c = rig.Controller;
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            var aft = Aft(rig);
            Require(aft.IsEnabled && aft.Text == "120.00", "shared value: " + aft.Text);
            Require(PropertiesViewTests.Need<ToggleButton>(rig.Host.Properties, "GroupModeSetTo").IsChecked == true, "Set to is not the default");
            // One point moved 6 mm aft: the values differ, so the field is empty with Mixed as its placeholder and a range line.
            Pick(c, "trailing", 3);
            Require(c.BeginGesture(Ref(c, "trailing", 3), GestureInput.Pointer), "begin");
            DragTo(c, At(c, "trailing", 3).SpanMeters, 0.126);
            Pump(c.EndGestureAsync(GestureEnd.Release));
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            aft = Aft(rig);
            Require(aft.Text == "" && aft.Watermark == "Mixed", $"mixed: '{aft.Text}' / {aft.Watermark}");
            Require(Msg(rig, "Description_p_aft") == "Range 120.00 to 126.00 mm.", "range: " + Msg(rig, "Description_p_aft"));
            Require(Msg(rig, "Description_p_from") == "Range 135.00 to 315.00 mm.", "span range: " + Msg(rig, "Description_p_from"));
            Require(PropertiesViewTests.Need<TextBox>(rig.Host.Properties, "PointSpanInput").IsEnabled, "From root is not editable");
        });
        Check("Properties_MultiplePoints_TypedSetTo_OneUndoStep", rig =>
        {
            var c = rig.Controller;
            string before = c.AcceptedSource;
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            TypeInto(rig, Aft(rig), "30");
            Require(new[] { 2, 3, 4 }.All(index => Math.Abs(At(c, "trailing", index).Ordinate - 0.030) < 1.1e-6), "values: " + Report(c, "trailing", 2, 3, 4));
            Require(c.Status == "Set aft of 3 points to 30.00 mm.", "echo: " + c.Status);
            Require(Aft(rig).Text == "30.00" && c.Selection is Selection.Points { Items.Count: 3 }, "row or selection after the commit");
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore the source");
        });
        Check("Properties_MultiplePoints_TypedMoveBy_OneUndoStep", rig =>
        {
            var c = rig.Controller;
            string before = c.AcceptedSource;
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            var moveBy = PropertiesViewTests.Need<ToggleButton>(rig.Host.Properties, "GroupModeMoveBy");
            moveBy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            rig.Settle();
            Require(Aft(rig).Text == "0" && Msg(rig, "Description_p_aft") == "Now 120.00 mm.", $"move-by row: {Aft(rig).Text} / {Msg(rig, "Description_p_aft")}");
            TypeInto(rig, Aft(rig), "5");
            Require(new[] { 2, 3, 4 }.All(index => Math.Abs(At(c, "trailing", index).Ordinate - 0.125) < 1.1e-6), "values: " + Report(c, "trailing", 2, 3, 4));
            Require(c.Status == "Moved 3 points by +5.00 mm in aft.", "echo: " + c.Status);
            Require(Aft(rig).Text == "0" && Msg(rig, "Description_p_aft") == "Now 125.00 mm.", $"after: {Aft(rig).Text} / {Msg(rig, "Description_p_aft")}");
            Require(PropertiesViewTests.Need<ToggleButton>(rig.Host.Properties, "GroupModeMoveBy").IsChecked == true, "the mode did not stay while the selection was kept");
            TypeInto(rig, PropertiesViewTests.Need<TextBox>(rig.Host.Properties, "PointSpanInput"), "2");
            Require(Math.Abs(At(c, "trailing", 3).SpanMeters - 0.227) < 1.1e-6 && c.Status == "Moved 3 points by +2.00 mm in span.", "span: " + c.Status);
            c.Undo();
            c.Undo();
            Require(c.AcceptedSource == before, "two entries are two undo steps");
            Pick(c, "trailing", 2, 3);
            rig.Settle();
            Require(PropertiesViewTests.Need<ToggleButton>(rig.Host.Properties, "GroupModeSetTo").IsChecked == true, "the mode did not reset with the selection");
        });
        Check("Properties_MultiplePoints_RefusalKeepsText_UseNeverAutomatic", rig =>
        {
            var c = rig.Controller;
            Pick(c, "trailing", 3, 6);
            rig.Settle();
            string before = c.AcceptedSource;
            TypeInto(rig, Aft(rig), "3");
            Require(Aft(rig).Text == "3", "the typed text was rewritten: " + Aft(rig).Text);
            Require(Msg(rig, "Message_p_aft") == TipChord.TypedRefusalReason(c.Estimates!.RootChordMeters), "refusal: " + Msg(rig, "Message_p_aft"));
            var use = PropertiesViewTests.Need<HyperlinkButton>(rig.Host.Properties, "UseLimit_p_aft");
            Require(use.IsVisible && use.Content?.ToString() == "Use 5 mm" && c.AcceptedSource == before, "no Use offer, or the refusal applied something");
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            rig.Settle();
            Require(c.AcceptedSource != before && Math.Abs(At(c, "trailing", 6).Ordinate - 0.005) < 1e-6, "Use did not apply the minimum");
            // Move by past the tip limit: Core's amount and a Use of the most the group can move.
            c.Undo();
            Pick(c, "trailing", 3, 6);
            rig.Settle();
            PropertiesViewTests.Need<ToggleButton>(rig.Host.Properties, "GroupModeMoveBy").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            rig.Settle();
            TypeInto(rig, Aft(rig), "-200");
            Require(Msg(rig, "Message_p_aft").EndsWith("would take the tip chord below 5 mm. The most they can move that way is 115 mm.", StringComparison.Ordinal),
                "refusal: " + Msg(rig, "Message_p_aft"));
            Require(Aft(rig).Text == "-200" && c.AcceptedSource == before, "text rewritten or applied");
            use = PropertiesViewTests.Need<HyperlinkButton>(rig.Host.Properties, "UseLimit_p_aft");
            Require(use.IsVisible && use.Content?.ToString() == "Use -115.00 mm", "Use: " + use.Content);
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            rig.Settle();
            Require(Math.Abs(At(c, "trailing", 6).Ordinate - 0.005) < 1e-6, "Use did not move the group by the most it can");
        });
        Check("GroupDrag_Canvas_Trackpad_TwelveSmallMoves_GlyphUnderPointer_GroupRigid", rig =>
        {
            var c = rig.Controller;
            string before = c.AcceptedSource;
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            var start = new[] { 2, 3, 4 }.Select(index => At(c, "trailing", index)).ToArray();
            var origin = rig.Canvas.ScreenPoint(start[1]);
            var pointer = rig.Press(rig.Canvas, origin);
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "the press dropped the group");
            double worst = 0;
            for (int step = 1; step <= 12; step++)
            {
                var local = origin + new Vector(2.5 * step, 0.7 * step);   // a trackpad: small steps, never a jump
                rig.Move(rig.Canvas, pointer, local);
                if (step < 2) continue;
                worst = Math.Max(worst, Point.Distance(rig.Canvas.ScreenPoint(At(c, "trailing", 3)), local));
            }
            Require(worst <= 2, $"the grabbed glyph is {worst:F2} px from the pointer");
            var delta = (At(c, "trailing", 3).SpanMeters - start[1].SpanMeters, At(c, "trailing", 3).Ordinate - start[1].Ordinate);
            Require(delta.Item1 > 0 && delta.Item2 > 0, "the grabbed point did not follow the pointer");
            for (int member = 0; member < 3; member++)
            {
                var now = At(c, "trailing", member + 2);
                Require(Math.Abs(now.SpanMeters - start[member].SpanMeters - delta.Item1) < 1.1e-6 && Math.Abs(now.Ordinate - start[member].Ordinate - delta.Item2) < 1.1e-6,
                    $"member {member} is not on the one delta");
            }
            rig.Release(rig.Canvas, pointer, origin + new Vector(30, 8.4));
            Require(c.Status.StartsWith("Moved 3 trailing edge points.", StringComparison.Ordinal), "strip: " + c.Status);
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore");
        });
        Check("GroupDrag_Canvas_ClickCollapses_ContextClickKeepsGroup_DoubleClickRestoresAndFocusesValueRow", rig =>
        {
            var c = rig.Controller;
            Pick(c, "trailing", 2, 3, 4);
            rig.Settle();
            var at = rig.Canvas.ScreenPoint(At(c, "trailing", 3));
            var grabbed = Ref(c, "trailing", 3);
            // A context click on a member keeps the group (the menu is closed again).
            var right = rig.Press(rig.Canvas, at, right: true);
            rig.Release(rig.Canvas, right, at, right: true);
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "a context click dropped the group");
            rig.Canvas.ContextMenu?.Close();
            // A click without a drag collapses on release.
            var click = rig.Press(rig.Canvas, at);
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "the press collapsed the group");
            rig.Release(rig.Canvas, click, at);
            Require(c.Selection is Selection.Points { Items: [var only] } && only == grabbed, "the release did not collapse");
            // The second press of a double-click puts the group back and sends focus to the group's value row.
            var second = rig.Press(rig.Canvas, at, clicks: 2);
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "the double-click lost the group");
            Require(rig.Canvas.LastValueRequest == "group:trailing", "value request: " + rig.Canvas.LastValueRequest);
            Require(Aft(rig).IsKeyboardFocusWithin, "focus is not in the group's value row");
            rig.Release(rig.Canvas, second, at);
            Require(c.Selection is Selection.Points { Items.Count: 3 }, "the double-click's release dropped the group");
        });
        Check("GroupDrag_Keyboard_SelectNudgeTypeToTheGroupRow", rig =>
        {
            // GEO-05 without a pointer: Space and Shift+Space select, arrows nudge as one run, Return goes to the value row.
            var c = rig.Controller;
            string before = c.AcceptedSource;
            var canvas = rig.Canvas;
            canvas.Focus();
            c.Select(new Selection.Foil());
            canvas.FocusPoint(Ref(c, "trailing", 2));
            rig.Key(canvas, Avalonia.Input.Key.Space);
            canvas.FocusPoint(Ref(c, "trailing", 3));
            rig.Key(canvas, Avalonia.Input.Key.Space, KeyModifiers.Shift);
            Require(c.Selection is Selection.Points { Items.Count: 2 }, "Space and Shift+Space did not select two points: " + c.Selection);
            for (int press = 0; press < 3; press++) rig.Key(canvas, Avalonia.Input.Key.Down);
            rig.Key(canvas, Avalonia.Input.Key.Down, up: true);
            rig.Pump();
            Require(Math.Abs(At(c, "trailing", 2).Ordinate - 0.1203) < 1.1e-6 && Math.Abs(At(c, "trailing", 3).Ordinate - 0.1203) < 1.1e-6, "nudged: " + Report(c, "trailing", 2, 3));
            Require(c.Selection is Selection.Points { Items.Count: 2 }, "the nudge dropped the group");
            rig.Key(canvas, Avalonia.Input.Key.Return);
            Require(Aft(rig).IsKeyboardFocusWithin, "Return did not go to the value row");
            TypeInto(rig, Aft(rig), "10");
            Require(Math.Abs(At(c, "trailing", 2).Ordinate - 0.010) < 1.1e-6 && Math.Abs(At(c, "trailing", 3).Ordinate - 0.010) < 1.1e-6, "typed: " + Report(c, "trailing", 2, 3));
            c.Undo();
            c.Undo();
            Require(c.AcceptedSource == before, "nudge run and typed entry are two undo steps");
        });
        Check("GroupDrag_Elevation_TwistPointsMoveAsOneGroup_ReadoutShowsTheAppliedMove", rig =>
        {
            var c = rig.Controller;
            string before = c.AcceptedSource;
            var free = c.CurveFor("twist")!.Points.Where(point => point.Role is PointRole.Control or PointRole.Anchor && point.Freedom == PointFreedom.Free).Take(2).ToArray();
            Require(free.Length == 2, "fixture: no two free twist points");
            c.Select(new Selection.Points(free.Select(point => new PointRef("twist", point.Id)).ToArray()));
            rig.Settle();
            var side = rig.Side;
            var origin = side.ScreenPoint(free[0]);
            var pointer = rig.Press(side, origin);
            Require(c.Selection is Selection.Points { Items.Count: 2 } && c.GestureGroup is { Count: 2 }, "the press dropped the group in the elevation");
            for (int step = 1; step <= 10; step++) rig.Move(side, pointer, origin + new Vector(0, -2.5 * step));
            var moved = free.Select(point => c.CurveFor("twist")!.Points.First(item => item.Id == point.Id)).ToArray();
            double d0 = moved[0].Ordinate - free[0].Ordinate, d1 = moved[1].Ordinate - free[1].Ordinate;
            Require(Math.Abs(d0) > 1e-4 && Math.Abs(d0 - d1) < 1e-4 && moved[0].SpanMeters == free[0].SpanMeters && moved[1].SpanMeters == free[1].SpanMeters,
                $"twist deltas {d0} / {d1}");
            Require(side.ProbeText?.Contains("Δ", StringComparison.Ordinal) == true, "no readout");
            rig.Release(side, pointer, origin + new Vector(0, -25));
            Require(c.Status.StartsWith("Moved 2 twist points.", StringComparison.Ordinal) && !c.Status.Contains("chord", StringComparison.Ordinal), "strip: " + c.Status);
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore");
        });
    }
}
