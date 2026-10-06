using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CfdWorkbench.Core;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

// Ruling 96: planform limits felt during the gesture. Controller checks need no window (ring: --controller-shell, tens of ms
// each); pane checks need the Properties pane (ring: --properties-cells, one window each, the Example foil).
public static class GestureLimitTests
{
    private static string Min(WorkbenchController c) => TipChord.FormatMm(TipChord.MinimumMeters(c.Estimates!.RootChordMeters));

    private static WorkbenchController NewFoil(string? tipMm = null)
    {
        var controller = new WorkbenchController();
        controller.NewFoilAsync().GetAwaiter().GetResult();
        if (tipMm is not null && controller.ApplyChordAsync("tip-chord", tipMm).GetAwaiter().GetResult() is not CommitOutcome.Committed)
            throw new InvalidOperationException("fixture: the tip chord was refused");
        return controller;
    }

    private static (PointRef Ref, PointView Point) Tip(WorkbenchController c, string rail = "trailing")
    {
        var point = (rail == "trailing" ? c.Planform!.Trailing : c.Planform!.Leading).Points[^1];
        return (new PointRef(rail, point.Id), point);
    }

    private static void DragTo(WorkbenchController c, double span, double aft)
    {
        c.UpdateGesture(span, aft);
        c.FlushGestureFrame();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void RunController()
    {
        DesktopChecks.Check("GestureLimit_TipDragPastMinimum_HoldsAndSpeaksOnce_ReleaseCommitsHeldValueAsOneUndoStep", () =>
        {
            using var c = NewFoil();
            var (tip, point) = Tip(c);
            double leadTip = c.Planform!.Leading.Points[^1].Ordinate;
            string before = c.AcceptedSource;
            Require(c.BeginGesture(tip, GestureInput.Pointer), "begin refused");
            DragTo(c, point.SpanMeters, leadTip + 0.001);
            Require(c.GestureLimit is { Kind: GestureLimitKind.TipMinimum }, "no tip limit while the pointer is past the minimum");
            Require(Math.Abs(c.Estimates!.TipChordMeters - 0.005) < 1.1e-6, $"tip chord {c.Estimates.TipChordMeters} is not held at 5 mm");
            string spoken = TipChord.HoldText(c.GestureLimit!);
            Require(c.Status == "Tip chord is at its minimum, 5 mm." && c.GestureLimitText == spoken, "strip: " + c.Status);
            long version = c.StatusVersion;
            DragTo(c, point.SpanMeters, leadTip + 0.0005);
            DragTo(c, point.SpanMeters, leadTip - 0.02);
            Require(c.StatusVersion == version, "a frame that stays held wrote the strip again");
            var outcome = c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            Require(outcome is GestureOutcome.Committed, "release was not committed: " + outcome);
            Require(c.GestureLimit is null, "the limit outlived the gesture");
            Require(Math.Abs(c.Estimates!.TipChordMeters - 0.005) < 1.1e-6, "release did not commit the held value");
            c.Undo();
            Require(c.AcceptedSource == before, "one Undo did not restore the source");
        });
        DesktopChecks.Check("GestureLimit_Escape_AfterHold_ChangesNothing", () =>
        {
            using var c = NewFoil();
            var (tip, point) = Tip(c);
            string before = c.AcceptedSource;
            bool undo = c.CanUndo;
            Require(c.BeginGesture(tip, GestureInput.Pointer), "begin refused");
            DragTo(c, point.SpanMeters, c.Planform!.Leading.Points[^1].Ordinate + 0.001);
            Require(c.GestureLimit is not null, "not held");
            c.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
            Require(c.AcceptedSource == before && c.CanUndo == undo && c.GestureLimit is null, "Escape left a step or a limit");
        });
        DesktopChecks.Check("GestureLimit_RootDragPastMaximum_HoldsAndLeadsWithTheRoot", () =>
        {
            using var c = NewFoil("6");
            var root = c.Planform!.Trailing.Points[0];
            Require(c.BeginGesture(new PointRef("trailing", root.Id), GestureInput.Pointer), "begin refused");
            DragTo(c, root.SpanMeters, root.Ordinate + 0.5);
            Require(c.GestureLimit is { Kind: GestureLimitKind.RootMaximum }, "no root limit");
            Require(c.Status == "Root chord is at its maximum, 300 mm, for a 6 mm tip. Widen the tip first.", "strip: " + c.Status);
            Require(Math.Abs(c.Estimates!.RootChordMeters - 0.3) < 1.1e-6, "root chord not held at 300 mm");
            c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
        });
        DesktopChecks.Check("GestureLimit_KeyboardNudges_HoldWithoutBankingAndAnnounceOnce", () =>
        {
            using var c = NewFoil("8");
            var (tip, _) = Tip(c);
            string before = c.AcceptedSource;
            Require(c.BeginGesture(tip, GestureInput.Keyboard), "begin refused");
            var versions = new List<long>();
            for (int press = 0; press < 10; press++)
            {
                c.Nudge(0, -1, NudgeModifier.Shift);
                versions.Add(c.StatusVersion);
            }
            Require(c.GestureLimit is { Kind: GestureLimitKind.TipMinimum }, "nudges did not reach the limit");
            int writes = versions.Zip(versions.Skip(1), (a, b) => b != a).Count(changed => changed);
            Require(writes == 1, $"the strip was written {writes} times in 10 presses; the hold is one announcement");
            Require(Math.Abs(c.Estimates!.TipChordMeters - 0.005) < 1.1e-6, "held tip chord " + c.Estimates.TipChordMeters);
            c.Nudge(0, 1, NudgeModifier.Shift);
            Require(c.GestureLimit is null && Math.Abs(c.Estimates.TipChordMeters - 0.006) < 1.5e-5,
                "one press back did not free the hold by one step (banked steps?): " + c.Estimates.TipChordMeters);
            c.Nudge(0, -1, NudgeModifier.Shift);
            c.Nudge(0, -1, NudgeModifier.Shift);
            long again = c.StatusVersion;
            Require(c.GestureLimit is not null, "the hold did not return");
            c.EndGestureAsync(GestureEnd.KeyUp).GetAwaiter().GetResult();
            c.Undo();
            Require(c.AcceptedSource == before && again > versions[^1], "one Undo did not restore, or the freed hold was not re-announced");
        });
        DesktopChecks.Check("GestureLimit_AnalysisMode_Inert", () =>
        {
            using var c = NewFoil();
            var (tip, _) = Tip(c);
            (typeof(WorkbenchController).GetMethod("ToggleAnalysis") ?? throw new InvalidOperationException("no Analysis toggle")).Invoke(c, null);
            Require(!c.BeginGesture(tip, GestureInput.Pointer) && c.GestureLimit is null && c.GestureLimitText is null, "Analysis began a gesture or showed a limit");
        });
        DesktopChecks.Check("GestureLimit_GestureEnd_TelemetryNamesTheClampReason", () =>
        {
            using var c = NewFoil();
            var (tip, point) = Tip(c);
            Require(c.BeginGesture(tip, GestureInput.Pointer), "begin refused");
            DragTo(c, point.SpanMeters, c.Planform!.Leading.Points[^1].Ordinate + 0.001);
            c.EndGestureAsync(GestureEnd.Release).GetAwaiter().GetResult();
            var end = CfdWorkbench.Desktop.Shell.ShellEvents.Read().Last(item => item.Name == "gesture.end");
            Require(end.ClampReason == "TipMinimum" && end.ClampedCount > 0 && end.Code is null, $"gesture.end: {end.ClampReason}/{end.ClampedCount}/{end.Code}");
        });
    }

    public static void RunPane()
    {
        Pane("PropertiesPane_TypedTipBelowMinimum_KeepsTextRefusesAndOffersUseOnlyOnClick", (controller, host, window) =>
        {
            var tip = Need<TextBox>(host.Properties, "TipChordInput");
            string before = controller.AcceptedSource;
            tip.Text = "3";
            Key(tip, Avalonia.Input.Key.Enter);
            Settle(window);
            string minimum = Min(controller);
            Require(tip.Text == "3", "the typed text was rewritten: " + tip.Text);
            Require(Text(host.Properties, "Message_w_tip") == TipChord.TypedRefusalReason(controller.Estimates!.RootChordMeters),
                "refusal: " + Text(host.Properties, "Message_w_tip"));
            Require(Text(host.Properties, "Message_w_tip").EndsWith($"Enter {minimum} or more.", StringComparison.Ordinal), "no way out in the message");
            var use = Need<HyperlinkButton>(host.Properties, "UseLimit_w_tip");
            Require(use.IsVisible && use.Content?.ToString() == $"Use {minimum}", "no Use action: " + use.Content);
            Require(controller.AcceptedSource == before, "the refusal changed the source before any click");
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(controller.AcceptedSource != before && Math.Abs(controller.Estimates!.TipChordMeters - 0.005) < 1e-6, "Use did not apply the minimum");
            Require(!Need<HyperlinkButton>(host.Properties, "UseLimit_w_tip").IsVisible, "Use stayed after it was applied");
        });
        Pane("PropertiesPane_TypedRootAboveMaximum_LeadsWithRootAndUseAppliesTheMaximum", (controller, host, window) =>
        {
            var tip = Need<TextBox>(host.Properties, "TipChordInput");
            tip.Text = "6";
            Key(tip, Avalonia.Input.Key.Enter);
            Settle(window);
            var rootBox = Need<TextBox>(host.Properties, "RootChordInput");
            rootBox.Text = "400";
            Key(rootBox, Avalonia.Input.Key.Enter);
            Settle(window);
            Require(rootBox.Text == "400", "the typed root was rewritten");
            Require(Text(host.Properties, "ChordWarningText") == "Root chord can't go above 300 mm while the tip chord is 6 mm (the tip must stay at least 2 % of the root). Widen the tip first.",
                "refusal: " + Text(host.Properties, "ChordWarningText"));
            var use = Need<HyperlinkButton>(host.Properties, "UseLimit_w_root");
            Require(use.IsVisible && use.Content?.ToString() == "Use 300 mm", "no Use 300 mm: " + use.Content);
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(Math.Abs(controller.Estimates!.RootChordMeters - 0.3) < 1e-6, "Use did not apply the root maximum");
        });
        Pane("PropertiesPane_HeldTipDrag_WingRowReadsMinimumInTheStripsWords", (controller, host, window) =>
        {
            var tipView = controller.Planform!.Trailing.Points[^1];
            var tip = new PointRef("trailing", tipView.Id);
            Require(controller.BeginGesture(tip, GestureInput.Pointer), "begin refused");
            DragTo(controller, tipView.SpanMeters, controller.Planform!.Leading.Points[^1].Ordinate + 0.001);
            Settle(window);
            var description = Need<TextBlock>(host.Properties, "Description_w_tip");
            Require(description.IsVisible && description.Text == $"{Quantity.TypedLength(controller.Estimates!.TipChordMeters)} mm · minimum",
                "tip row: " + description.Text);
            Require(controller.Status == "Tip chord is at its minimum, 5 mm.", "strip: " + controller.Status);
            controller.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
            Settle(window);
            Require(!Need<TextBlock>(host.Properties, "Description_w_tip").IsVisible, "the minimum note outlived the gesture");
        });
        Pane("PlanCanvas_HeldLimit_PointNameCarriesTheStripsWordsAndTheMarkerRenders", (controller, host, window) =>
        {
            var tipView = controller.Planform!.Trailing.Points[^1];
            Require(controller.BeginGesture(new PointRef("trailing", tipView.Id), GestureInput.Pointer), "begin refused");
            DragTo(controller, tipView.SpanMeters, controller.Planform!.Leading.Points[^1].Ordinate + 0.001);
            Settle(window);
            var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
            var peer = ControlAutomationPeer.CreatePeerForElement(canvas)!;
            var named = peer.GetChildren()!.Where(child => child.GetName().Contains("Tip chord is at its minimum, 5 mm.", StringComparison.Ordinal)).ToArray();
            Require(named.Length == 1 && named[0].GetName().Contains("Trailing edge, point", StringComparison.Ordinal),
                $"{named.Length} points carry the hold sentence; it belongs on the held point only");
            using var pixels = PropertiesCellsTests.Render(canvas, 1);
            Require(canvas.RenderBanner is null, "the plan could not render with a held limit: " + canvas.RenderBanner);
            controller.EndGestureAsync(GestureEnd.Escape).GetAwaiter().GetResult();
            Settle(window);
            Require(!peer.GetChildren()!.Any(child => child.GetName().Contains("minimum", StringComparison.Ordinal)), "the hold sentence outlived the gesture");
        });
    }
}
