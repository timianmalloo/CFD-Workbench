using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

// Track S1 (design add-station.md 7): the window checks D1, D4, D5, D6 (focus) and D7, beside the other --plan-canvas checks.
public static partial class PlanCanvasTests
{
    private static void StationWait(PlanFixture fixture, Func<bool> condition, string what, double seconds = 60)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new Exception("Timed out waiting for " + what);
            fixture.Settle();
            Thread.Sleep(10);
        }
        fixture.Settle();
    }

    private static PlanFixture StationFixture(byte[] source, double width = 1280, double height = 800)
    {
        var fixture = new PlanFixture(source: source, width: width, height: height);
        StationDesktopTests.UseCoarseGrid(fixture.Controller);
        return fixture;
    }

    private static int StationRun((byte R, byte G, byte B) probe, (byte R, byte G, byte B) target, Func<int, (byte R, byte G, byte B)> read)
    {
        int run = 0;
        for (int d = -4; d <= 4; d++)
        {
            var pixel = read(d);
            if (Math.Abs(pixel.R - target.R) + Math.Abs(pixel.G - target.G) + Math.Abs(pixel.B - target.B) <= 60) run++;
        }
        _ = probe;
        return run;
    }

    public static void RunStations()
    {
        // Design 5.2, Place (Plan): a press in the chip strip places the slice, within 4 px of a station's line it selects that station,
        // and the arrow keys nudge 0.01 / 0.1 / 1 mm (Command, plain, Shift) in a percent document and in a metre document; Escape clears.
        DesktopChecks.Check("PlanCanvas_PlaceInChipStrip_SnapToStation_NudgeSteps_EscapeClears", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var canvas = fixture.Canvas;
            double y = canvas.VisibleStationChips[0].Bounds.Center.Y, below = canvas.VisibleStationChips[0].Bounds.Bottom + 1;   // below the chip, inside the strip, so a chip hit cannot answer for the snap
            double x0 = canvas.StationLineX(0), x1 = canvas.StationLineX(1), x2 = canvas.StationLineX(2);
            fixture.ClickAt(new Point(x0 + 0.35 * (x2 - x0), y));
            if (controller.Selection is not Selection.Slice { Eta: var placed } || Math.Abs(placed - 0.35) > 0.01) throw new Exception("A press in the chip strip did not place the slice near 35 %: " + controller.Selection);
            fixture.ClickAt(new Point(x1 + 3, below));
            if (controller.Selection is not Selection.Station { Index: 1 }) throw new Exception("A press 3 px from a station's line did not select it: " + controller.Selection);
            fixture.ClickAt(new Point(x1 + 9, below));
            if (controller.Selection is not Selection.Slice) throw new Exception("A press 9 px from a station's line selected it: " + controller.Selection);

            foreach (var (label, sections) in new[]
            {
                ("percent", "at root profile \"a\" at 60 % profile \"a\" at tip profile \"a\""),
                ("metre", "at root profile \"a\" at 0.27 m profile \"a\" at tip profile \"a\"")
            })
            {
                using var doc = StationFixture(StationDesktopTests.Doc(sections, 120, ("a", new[] { 0, 0.025, 0.055, 0.065, 0.05, 0.03, 0.01, 0 })));
                double half = doc.Controller.Inspection!.Authored.HalfSpanMeters ?? throw new Exception("no half span");
                doc.Controller.PlaceSliceTyped("157.5 mm");
                doc.Settle();
                double Metres() => ((Selection.Slice)doc.Controller.Selection).Eta * half;
                foreach (var (modifier, step) in new[] { (KeyModifiers.Control, 0.00001), (KeyModifiers.None, 0.0001), (KeyModifiers.Shift, 0.001) })
                {
                    double before = Metres();
                    doc.KeyDown(Key.Right, modifier);
                    if (Math.Abs(Metres() - before - step) > 1e-8) throw new Exception(label + " Right " + modifier + " moved " + (Metres() - before) * 1000 + " mm, expected " + step * 1000);
                    doc.KeyDown(Key.Left, modifier);
                    if (Math.Abs(Metres() - before) > 1e-8) throw new Exception(label + " Left " + modifier + " did not return to " + before * 1000 + " mm: " + Metres() * 1000);
                }
                doc.KeyDown(Key.Escape);
                if (doc.Controller.Selection is not Selection.Foil) throw new Exception(label + " Escape on the canvas left " + doc.Controller.Selection);
            }
        });

        DesktopChecks.Check("AddStationEntry_AccessibleNameContainsVisibleLabel", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var input = fixture.Host.ModelView.AddStationInput;
            var name = Avalonia.Automation.AutomationProperties.GetName(input) ?? "";
            var heading = fixture.Host.ModelView.FindControl<Border>("AddStationPanel")!.GetVisualDescendants().OfType<TextBlock>().First().Text;
            if (heading != WorkbenchController.AddStationHeading) throw new Exception("The heading is '" + heading + "', not the Labels constant");
            if (!name.Contains(heading!, StringComparison.Ordinal)) throw new Exception("The entry's accessible name '" + name + "' does not contain its visible label '" + heading + "' (WCAG 2.5.3)");
        });

        DesktopChecks.Check("PlanCanvas_SliceChip_PromotePeerDisabledWithReason", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            controller.PlaceSliceTyped("35");
            fixture.Settle();
            Avalonia.Automation.Peers.AutomationPeer Promote() => peer.GetChildren().First(child => child.GetName().StartsWith("Inspection slice", StringComparison.Ordinal))
                .GetChildren().First(child => child.GetName() == "Promote");
            if (!Promote().IsEnabled() || Promote().GetHelpText() != "") throw new Exception("Promote reads disabled with no reason to be: " + Promote().IsEnabled() + " '" + Promote().GetHelpText() + "'");
            controller.EnterSectionAsync(1, EntryOrigin.Properties).GetAwaiter().GetResult();
            controller.PlaceSliceTyped("35");
            fixture.Settle();
            var reason = controller.AddStationReason ?? throw new Exception("No draft reason to observe");
            if (controller.Selection is not Selection.Slice) throw new Exception("The slice did not survive the draft: " + controller.Selection);
            if (Promote().IsEnabled() || Promote().GetHelpText() != reason) throw new Exception("Promote reads enabled or lacks its reason during a draft: " + Promote().IsEnabled() + " '" + Promote().GetHelpText() + "'");
        });

        // SF1 item 2: while the promotion preview runs, the Promote peer is disabled and says Checking…; afterwards it no longer does.
        DesktopChecks.Check("PlanCanvas_SliceChip_PromotePeerDisabledCheckingWhilePreviewRuns", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.NearPair());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            controller.PlaceSliceTyped("50");
            fixture.Settle();
            Avalonia.Automation.Peers.AutomationPeer Promote() => peer.GetChildren().First(child => child.GetName().StartsWith("Inspection slice", StringComparison.Ordinal))
                .GetChildren().First(child => child.GetName() == "Promote");
            _ = controller.PromoteSliceAsync();   // not awaited: the continuation needs the UI thread, so the preview stays running until the next Settle
            if (!controller.SliceChecking) throw new Exception("The preview is not running right after the press");
            if (Promote().IsEnabled() || Promote().GetHelpText() != WorkbenchController.CheckingCopy)
                throw new Exception("While the preview runs the Promote peer reads enabled or lacks the Checking copy: " + Promote().IsEnabled() + " '" + Promote().GetHelpText() + "'");
            StationWait(fixture, () => controller.Flyout is { Checking: false }, "the approximate-promotion preview");
            if (controller.SliceChecking) throw new Exception("The preview finished but SliceChecking is still set");
            if (Promote().GetHelpText() == WorkbenchController.CheckingCopy) throw new Exception("The Promote peer still says Checking after the preview finished");
        });

        // SF1 item 3 (finding F-CHIP): each Plan station chip is an automation child named by its visible text, selectable, with Edit section….
        DesktopChecks.Check("PlanCanvas_StationChips_AutomationPeerNamedSelectableWithEditSection", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.Doc("at root profile \"a\" at 60 % profile \"f\" at tip profile \"n\"", 120,
                ("a", [0, 0.025, 0.055, 0.065, 0.05, 0.03, 0.01, 0]), ("f", [0, 0.02, 0.04, 0.055, 0.065, 0.05, 0.02, 0]), ("n", [0, 0.0225, 0.0475, 0.060, 0.0575, 0.04, 0.015, 0])));
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            var plan = controller.Planform!;
            string[] expected = fixture.Canvas.VisibleStationChips.Select(chip => plan.Stations[chip.Index].ProfileName).ToArray();
            if (expected.Length != 3 || expected.Distinct().Count() != 3) throw new Exception("The fixture does not show three distinct chips: " + string.Join(", ", expected));
            var names = peer.GetChildren().Select(child => child.GetName()).ToArray();
            foreach (string name in expected)
                if (!names.Contains(name)) throw new Exception($"The canvas peer has no child named '{name}' (its chip's visible text): " + string.Join(" | ", names));
            Avalonia.Automation.Peers.AutomationPeer Chip(string name) => peer.GetChildren().First(child => child.GetName() == name);
            if (Chip("f") is not Avalonia.Automation.Provider.ISelectionItemProvider middle) throw new Exception("The station chip peer is not selectable");
            if (middle.IsSelected) throw new Exception("The chip reads selected before any selection");
            middle.Select();
            if (controller.Selection is not Selection.Station { Index: 1 } || !((Avalonia.Automation.Provider.ISelectionItemProvider)Chip("f")).IsSelected || ((Avalonia.Automation.Provider.ISelectionItemProvider)Chip("a")).IsSelected)
                throw new Exception("Selecting the chip peer did not select the station: " + controller.Selection);
            var edit = Chip("f").GetChildren().FirstOrDefault(child => child.GetName() == PropertiesView.EditSection) ?? throw new Exception("The station chip peer has no 'Edit section…' action");
            if (edit is not Avalonia.Automation.Provider.IInvokeProvider invoke) throw new Exception("'Edit section…' is not invokable");
            invoke.Invoke();
            StationWait(fixture, () => controller.Section is not null, "the section editor from the chip peer");
            if (controller.Section!.Draft is null) throw new Exception("Edit section… opened no draft");
            if (controller.Section.Origin != EntryOrigin.Plan) throw new Exception("Edit section… from the Plan chip recorded the origin " + controller.Section.Origin);
        });

        // SF1 conditions 5 and 1: a refused entry (a point draft is open) is reported on the status line, not thrown out of Invoke.
        DesktopChecks.Check("PlanCanvas_StationChips_EditSectionRefusalReportedNotThrown", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            var point = controller.Planform!.Trailing.Points[3];
            if (!controller.BeginGesture(new PointRef("trailing", point.Id), GestureInput.Pointer)) throw new Exception("The point gesture did not begin");
            string expected;
            try { controller.EnterSectionAsync(0, EntryOrigin.Plan); throw new Exception("EnterSectionAsync did not refuse with a point draft open"); }
            catch (ContractError error) { expected = error.Reason ?? error.Code; }
            var chip = peer.GetChildren().First(child => child is Avalonia.Automation.Provider.ISelectionItemProvider);
            var edit = chip.GetChildren().First(child => child.GetName() == PropertiesView.EditSection);
            try { ((Avalonia.Automation.Provider.IInvokeProvider)edit).Invoke(); }
            catch (ContractError error) { throw new Exception("Invoke threw " + error.Code + " instead of reporting it"); }
            if (controller.Status != expected || controller.StatusKind != ReportKind.Warning)
                throw new Exception($"The refusal was not reported as a warning: '{controller.Status}' ({controller.StatusKind}), expected '{expected}'");
        });

        // SF1 condition 4: in Analysis the chip's Edit section… is disabled and says why.
        DesktopChecks.Check("PlanCanvas_StationChips_EditSectionDisabledInAnalysisWithReason", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            controller.ToggleAnalysis();
            if (!controller.IsAnalysis) throw new Exception("The Analysis toggle did not enter Analysis");
            var chip = peer.GetChildren().First(child => child is Avalonia.Automation.Provider.ISelectionItemProvider);
            var edit = chip.GetChildren().First(child => child.GetName() == PropertiesView.EditSection);
            if (edit.IsEnabled()) throw new Exception("Edit section… is enabled in Analysis");
            if (edit.GetHelpText() != WorkbenchController.AnalysisPointRefusal) throw new Exception("Edit section… help text in Analysis is '" + edit.GetHelpText() + "'");
        });

        // SF1 condition 2: Plan chips can share a profile name (SC 1.3.1), so the peer's description carries the station.
        DesktopChecks.Check("PlanCanvas_StationChips_ThreeSameNameSameDescriptionsDistinct", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            var chips = peer.GetChildren().Where(child => child is Avalonia.Automation.Provider.ISelectionItemProvider).ToArray();
            var plan = controller.Planform!;
            if (chips.Length != 3) throw new Exception("Expected three station chip peers, found " + chips.Length);
            if (chips.Select(chip => chip.GetName()).Distinct().Count() != 1) throw new Exception("The fixture's chips no longer share a profile name: " + string.Join(", ", chips.Select(chip => chip.GetName())));
            string[] described = chips.Select(chip => chip.GetHelpText()).ToArray();
            string[] expected = fixture.Canvas.VisibleStationChips.Select(chip => ElevationView.StationName(chip.Index, plan.Stations[chip.Index].Eta)).ToArray();
            if (described.Distinct().Count() != 3 || !described.OrderBy(text => text).SequenceEqual(expected.OrderBy(text => text)))
                throw new Exception($"Descriptions [{string.Join(" | ", described)}] do not name the stations [{string.Join(" | ", expected)}]");
        });

        // SF1 condition 6: a renamed profile changes the peer's name; a removed station's peer goes.
        DesktopChecks.Check("PlanCanvas_StationChips_PeerFollowsRemoval", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            int before = peer.GetChildren().Count(child => child is Avalonia.Automation.Provider.ISelectionItemProvider);
            if (before != 3) throw new Exception("Expected three station chip peers before the removal, found " + before);
            _ = controller.BeginRemoveStationAsync(1);   // not awaited: the test thread is the UI thread, which the continuation needs
            StationWait(fixture, () => controller.Flyout is { Checking: false }, "the Remove preview");
            _ = controller.ApplyFlyoutAsync();
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 2 && !controller.SurfaceUpdating, "the removal");
            int after = peer.GetChildren().Count(child => child is Avalonia.Automation.Provider.ISelectionItemProvider);
            if (after != 2) throw new Exception("The removed station's chip peer did not go: " + after + " station chip peers remain");
        });

        DesktopChecks.Check("PlanCanvas_StationChips_PeerFollowsPromotion", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            if (peer.GetChildren().Any(child => child.GetName() == "Station 2")) throw new Exception("A Station 2 chip peer exists before the promotion");
            controller.PlaceSliceTyped("35");
            _ = controller.PromoteSliceAsync();   // not awaited: the test thread is the UI thread, which the continuation needs
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 4 && !controller.SurfaceUpdating, "the promotion");
            if (!peer.GetChildren().Any(child => child.GetName() == "Station 2")) throw new Exception("The canvas peer was not refreshed with the new station chip: " + string.Join(" | ", peer.GetChildren().Select(child => child.GetName())));
        });

        // D2 extension (S1-b review veto): the menu and palette read ShellHost.CanRun, so the station rows' enablement is checked there.
        DesktopChecks.Check("ShellHost_StationCommands_CanRunFollowsReasons", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var host = fixture.Host;
            controller.Select(new Selection.Station(0, 0));
            if (host.CanRun("station.remove")) throw new Exception("CanRun(station.remove) is true on Root");
            controller.Select(new Selection.Station(2, 1));
            if (host.CanRun("station.remove")) throw new Exception("CanRun(station.remove) is true on Tip");
            controller.Select(new Selection.Station(1, 0.6));
            if (!host.CanRun("station.remove") || !host.CanRun("station.add")) throw new Exception("CanRun refuses an interior station with no draft");
            if (!host.CanRun("file.open")) throw new Exception("An unrelated command's CanRun changed");
            controller.EnterSectionAsync(1, EntryOrigin.Properties).GetAwaiter().GetResult();
            fixture.Settle();
            controller.Select(new Selection.Station(1, 0.6));
            if (host.CanRun("station.add") || host.CanRun("station.remove")) throw new Exception("CanRun is true for a station command during an open draft");
        });

        DesktopChecks.Check("PlanCanvas_SliceChip_AutomationPeerNamesChipAndInvokesPromoteAndClear", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            var canvas = fixture.Canvas;
            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(canvas)!;
            if (peer.GetChildren().Any(child => child.GetName().StartsWith("Inspection slice", StringComparison.Ordinal))) throw new Exception("A slice chip peer exists with no slice");
            controller.PlaceSliceTyped("35");
            fixture.Settle();
            var chip = peer.GetChildren().FirstOrDefault(child => child.GetName() == "Inspection slice, η 35.0 percent, 157.5 millimetres")
                ?? throw new Exception("The canvas peer has no slice chip child: " + string.Join(" | ", peer.GetChildren().Select(child => child.GetName())));
            var actions = chip.GetChildren();
            var promote = actions.FirstOrDefault(child => child.GetName() == "Promote") ?? throw new Exception("The chip peer has no Promote action");
            var clear = actions.FirstOrDefault(child => child.GetName() == "Clear slice") ?? throw new Exception("The chip peer has no × action");
            if (promote is not Avalonia.Automation.Provider.IInvokeProvider promoteInvoke || clear is not Avalonia.Automation.Provider.IInvokeProvider clearInvoke || chip is not Avalonia.Automation.Provider.IInvokeProvider)
                throw new Exception("The chip, Promote or × is not invokable");
            if (promote.GetBoundingRectangle().Width < 24 || clear.GetBoundingRectangle().Width < 24) throw new Exception("An action peer is under 24 px (WCAG 2.5.8)");
            clearInvoke.Invoke();
            fixture.Settle();
            if (controller.Selection is not Selection.Foil) throw new Exception("The × peer did not clear the slice: " + controller.Selection);
            controller.PlaceSliceTyped("35");
            fixture.Settle();
            var again = peer.GetChildren().First(child => child.GetName().StartsWith("Inspection slice", StringComparison.Ordinal)).GetChildren().First(child => child.GetName() == "Promote");
            ((Avalonia.Automation.Provider.IInvokeProvider)again).Invoke();
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 4 && !controller.SurfaceUpdating, "the promotion from the peer");
            if (peer.GetChildren().Any(child => child.GetName().StartsWith("Inspection slice", StringComparison.Ordinal))) throw new Exception("The slice chip peer outlives the slice");
        });

        DesktopChecks.Check("Station_KeyboardPlacement_EntryTypesPromotesAndEscapes", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            dynamic area = fixture.Host.ModelView;
            fixture.Host.RunCommand("station.add").GetAwaiter().GetResult();
            fixture.Settle();
            TextBox input = area.AddStationInput;
            if (!input.IsEffectivelyVisible || !input.IsFocused) throw new Exception($"station.add did not focus the η entry (visible {input.IsEffectivelyVisible}, focused {input.IsFocused})");
            input.Text = "35";
            fixture.Settle();
            if (controller.Selection is not Selection.Slice { Eta: var typed } || Math.Abs(typed - .35) > 1e-6) throw new Exception("Typing 35 did not move the slice: " + controller.Selection);
            input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Return, Source = input });
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 4 && !controller.SurfaceUpdating, "the promotion from the entry");
            if (controller.Selection is not Selection.Station { Index: 1 }) throw new Exception("Return did not promote: " + controller.Selection);
            if (input.IsEffectivelyVisible) throw new Exception("The entry stays open after Return");

            fixture.Host.RunCommand("station.add").GetAwaiter().GetResult();
            fixture.Settle();
            input = area.AddStationInput;
            input.Text = "80";
            fixture.Settle();
            if (controller.Selection is not Selection.Slice) throw new Exception("The second slice was not placed");
            input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape, Source = input });
            fixture.Settle();
            if (controller.Selection is Selection.Slice || input.IsEffectivelyVisible) throw new Exception("Escape did not clear the slice and close the entry: " + controller.Selection);
            if (!fixture.Canvas.IsFocused) throw new Exception("Escape did not return focus to the Plan");
            if (StationDesktopTests.StationCount(controller) != 4) throw new Exception("Escape changed the stations");
        });

        DesktopChecks.Check("PlanCanvas_SliceChip_LabelPromoteInStrip", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            fixture.Controller.PlaceSliceTyped("35");
            fixture.Settle();
            var canvas = fixture.Canvas;
            if (canvas.SliceChipBounds is not { } chip || canvas.SlicePromoteBounds is not { } promote || canvas.SliceClearBounds is not { } clear)
                throw new Exception("The slice drew no chip with Promote and ×");
            if (canvas.SliceChipLabel != "Slice · η 35.0 % · 157.5 mm") throw new Exception("Chip label: " + canvas.SliceChipLabel);
            if (chip.Top < canvas.PlanformBounds.Bottom) throw new Exception($"The chip {chip} is not below the planform {canvas.PlanformBounds}");
            if (!chip.Contains(promote.TopLeft) || !chip.Contains(promote.BottomRight) || !chip.Contains(clear.TopLeft) || !chip.Contains(clear.BottomRight))
                throw new Exception("Promote and × are not inside the chip");
            if (promote.Width < 24 || promote.Height < 24 || clear.Width < 24 || clear.Height < 24) throw new Exception("A chip button is under 24 x 24 px (WCAG 2.5.8)");
            if (canvas.SliceChipName != "Inspection slice, η 35.0 percent, 157.5 millimetres") throw new Exception("Accessible name: " + canvas.SliceChipName);
            var inside = fixture.RgbAtCanvas(chip.Left + 4, chip.Top + 6);
            var outside = fixture.RgbAtCanvas(chip.Left - 6, chip.Top + 6);
            if (inside != outside) throw new Exception($"The chip is filled, not hollow: inside {inside}, outside {outside}");
            int border = 0;
            for (double x = chip.Left; x < chip.Right; x++) if (fixture.RgbAtCanvas(x, chip.Top) != outside) border++;
            if (border < 20) throw new Exception($"The chip's dashed border is not drawn ({border} px)");
            fixture.ClickAt(clear.Center);
            if (fixture.Controller.Selection is not Selection.Foil) throw new Exception("× did not clear the slice: " + fixture.Controller.Selection);
            fixture.Controller.PlaceSliceTyped("35");
            fixture.Settle();
            fixture.ClickAt(canvas.SlicePromoteBounds!.Value.Center);
            StationWait(fixture, () => StationDesktopTests.StationCount(fixture.Controller) == 4 || fixture.Controller.Selection is Selection.Station, "the Promote press");
            if (fixture.Controller.Selection is not Selection.Station { Index: 1, Eta: 0.35 }) throw new Exception("Promote did not select the new station: " + fixture.Controller.Selection);
        });

        DesktopChecks.Check("PlanCanvas_SliceVisibleAtReflow", () =>
        {
            foreach (var (width, height) in new[] { (1280d, 800d), (640d, 400d) })
            {
                using var fixture = StationFixture(StationDesktopTests.ThreeSame(), width, height);
                fixture.Controller.PlaceSliceTyped("35");
                fixture.Settle();
                var canvas = fixture.Canvas;
                if (canvas.SlicePromoteBounds is not { } promote) throw new Exception($"No Promote at {width} x {height}");
                var bounds = new Rect(canvas.Bounds.Size);
                if (!bounds.Contains(promote.TopLeft) || !bounds.Contains(promote.BottomRight))
                    throw new Exception($"Promote {promote} is outside the canvas {bounds} at {width} x {height}");
                if (promote.Intersects(canvas.PlanformBounds)) throw new Exception($"Promote {promote} covers the planform at {width} x {height}");
            }
        });

        DesktopChecks.Check("Controller_Promote_SelectedEverywhereAndRenumbered", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            controller.Layout = ViewLayout.Four;   // Plan, 3D, Side and Front all shown
            fixture.Settle();
            var side = fixture.Host.ModelView.FindControl<ElevationView>("SideElevation")!;
            var front = fixture.Host.ModelView.FindControl<ElevationView>("FrontElevation")!;
            var threeD = fixture.Host.ModelView.FindControl<View3d>("ThreeDView")!;
            controller.PlaceSliceTyped("35");
            _ = controller.PromoteSliceAsync();   // not awaited: the test thread is the UI thread, which the continuation needs
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 4 && !controller.SurfaceUpdating, "the promotion and its surface");
            if (controller.Selection is not Selection.Station { Index: 1, Eta: 0.35 } selected) throw new Exception("Selection: " + controller.Selection);
            var projection = controller.Inspection!.Authored;
            string[] names = projection.Assignments.Select((item, index) => StationNames.Of(index, item.Eta)).ToArray();
            if (!names.SequenceEqual(["Root", "Station 2", "Station 3", "Tip"])) throw new Exception("Projection: " + string.Join(", ", names));
            if (PropertiesView.Build(selected, projection, null, ShellMode.Workspace).Identity?.Title != "Station 2") throw new Exception("Properties heading");
            if (PropertiesView.Build(new Selection.Station(2, 0.6), projection, null, ShellMode.Workspace).Identity?.Title != "Station 3") throw new Exception("Renumbered Properties heading");
            // Plan: the strip chip of the new station is drawn, labelled by its profile "Station 2", and is the selected one.
            var plan = controller.Planform!;
            if (plan.Stations[1].ProfileName != "Station 2" || fixture.Canvas.VisibleStationChips.All(chip => chip.Index != 1)) throw new Exception("Plan chip of Station 2: profile '" + plan.Stations[1].ProfileName + "', chips " + string.Join(",", fixture.Canvas.VisibleStationChips.Select(chip => chip.Index)) + " of " + plan.Stations.Count);
            // Browser: the row is selected and shows the new station.
            var list = fixture.Host.Browser.FindControl<ListBox>("StationList")!;
            if (list.ItemCount != 4 || list.SelectedIndex != 1 || (list.Items.OfType<ListBoxItem>().ElementAt(1).Content as string)?.Contains("Station 2", StringComparison.Ordinal) != true)
                throw new Exception($"Browser row: {list.SelectedIndex} of {list.ItemCount}");
            // Side: the chips read Station 2 and Station 3.
            var chips = side.Chips().Select(chip => chip.Name).ToArray();
            if (!chips.Contains("Station 2") || !chips.Contains("Station 3")) throw new Exception("Side chips: " + string.Join(", ", chips));
            fixture.FrameSnapshot();   // renders the window, so the 3D chip is drawn
            fixture.Settle();
            fixture.FrameSnapshot();
            if (threeD.ChipText != "Station 2") throw new Exception("3D chip: " + threeD.ChipText);
            // Front: the selected station is a 3 px tick of the station colour at its span.
            var placed = controller.Surface!.Sections.FirstOrDefault(section => section.Eta == 0.35) ?? throw new Exception("The surface has no section at 0.35");
            var camera = front.Camera!.Value;
            var (minimum, maximum) = WorkbenchController.SectionBounds(placed);
            var top = camera.Project(new Point3(camera.Target.X, placed.Upper[0].Y, maximum.Z), front.BandRect.Size);
            var bottom = camera.Project(new Point3(camera.Target.X, placed.Upper[0].Y, minimum.Z), front.BandRect.Size);
            var middle = front.TranslatePoint(new Point(top.X, (top.Y + bottom.Y) / 2), fixture.Window)!.Value;
            var station = fixture.Window.TryFindResource("PlanSelectionBrush", fixture.Window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
                ? (brush.Color.R, brush.Color.G, brush.Color.B) : throw new Exception("PlanSelectionBrush is missing");
            int run = StationRun(station, station, d => fixture.RgbAtWindow(Math.Round(middle.X) + d, Math.Round(middle.Y)));
            if (run < 3) throw new Exception($"Front shows {run} px of station colour at the new station, not 3");
            // One Undo takes all of it back.
            controller.Undo();
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 3, "the undo");
            if (list.ItemCount != 3 || side.Chips().Any(chip => chip.Name == "Station 3")) throw new Exception("Undo left a view on four stations");
        });

        DesktopChecks.Check("Controller_PromotedStation_ThumbnailAndEditSection", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.ThreeSame());
            var controller = fixture.Controller;
            controller.PlaceSliceTyped("35");
            _ = controller.PromoteSliceAsync();   // not awaited: the test thread is the UI thread, which the continuation needs
            StationWait(fixture, () => StationDesktopTests.StationCount(controller) == 4, "the promotion");
            var model = PropertiesView.Build(controller.Selection, controller.Inspection!.Authored, null, ShellMode.Workspace);
            if (!model.Groups.SelectMany(group => group.Rows).Any(row => row.Kind == RowKind.Action && row.Value == PropertiesView.EditSection))
                throw new Exception("The new station's panel has no 'Edit section…' action");
            controller.EnterSectionAsync(1, EntryOrigin.Properties).GetAwaiter().GetResult();
            fixture.Settle();
            var thumbnails = fixture.Window.GetVisualDescendants().OfType<Button>()
                .Where(button => Avalonia.Automation.AutomationProperties.GetName(button) == "Station 2 section thumbnail").ToArray();
            if (thumbnails.Length != 1 || !thumbnails[0].IsVisible) throw new Exception("The section editor's strip has no 'Station 2 section thumbnail'");
        });

        DesktopChecks.Check("Controller_PromoteFlyout_EscapeReturnsFocusToPromote", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.NearPair());
            var controller = fixture.Controller;
            controller.PlaceSliceTyped("50");
            fixture.Host.RefreshPanes();
            fixture.Settle();
            HyperlinkButton Link(string text) => fixture.Window.GetVisualDescendants().OfType<HyperlinkButton>().FirstOrDefault(link => link.Content as string == text && link.IsVisible)
                ?? throw new Exception("No visible link '" + text + "'");
            var promote = Link("Promote");
            promote.Focus();
            promote.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            StationWait(fixture, () => controller.Flyout is { Checking: false }, "the approximate-promotion preview");
            fixture.Host.RefreshPanes();
            fixture.Settle();
            var cancel = Link("Cancel");
            cancel.Focus();
            cancel.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape, Source = cancel });
            fixture.Settle();
            if (controller.Flyout is not null) throw new Exception("Escape left the flyout open");
            if (fixture.Window.FocusManager?.GetFocusedElement() is not HyperlinkButton { Content: "Promote" })
                throw new Exception("Escape did not return focus to Promote: " + fixture.Window.FocusManager?.GetFocusedElement());
        });

        DesktopChecks.Check("Controller_RemoveFlyout_FocusEscapeAndReturn", () =>
        {
            using var fixture = StationFixture(StationDesktopTests.Three());
            var controller = fixture.Controller;
            controller.Select(new Selection.Station(1, 0.6));
            fixture.Host.RefreshPanes();
            fixture.Settle();
            HyperlinkButton Link(string text) => fixture.Window.GetVisualDescendants().OfType<HyperlinkButton>().FirstOrDefault(link => link.Content as string == text && link.IsVisible)
                ?? throw new Exception("No visible link '" + text + "'");
            var remove = Link("Remove station");
            remove.Focus();
            remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            StationWait(fixture, () => controller.Flyout is { Checking: false }, "the Remove preview");
            fixture.Host.RefreshPanes();
            fixture.Settle();
            var apply = Link("Apply");
            if (apply.IsEnabled) throw new Exception("Apply is enabled above the 10 µm limit");
            if (Avalonia.Automation.AutomationProperties.GetHelpText(apply) != "Apply is disabled: the change is above the 10 µm limit.") throw new Exception("Apply description: " + Avalonia.Automation.AutomationProperties.GetHelpText(apply));
            var cancel = Link("Cancel");
            cancel.Focus();
            cancel.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape, Source = cancel });
            fixture.Settle();
            if (controller.Flyout is not null) throw new Exception("Escape left the flyout open");
            if (fixture.Window.FocusManager?.GetFocusedElement() is not HyperlinkButton { Content: "Remove station" }) throw new Exception("Escape did not return focus to the Remove button: " + fixture.Window.FocusManager?.GetFocusedElement());
            using var same = StationFixture(StationDesktopTests.ThreeSame());
            same.Controller.Select(new Selection.Station(1, 0.6));
            same.Host.RefreshPanes();
            same.Settle();
            HyperlinkButton SameLink(string text) => same.Window.GetVisualDescendants().OfType<HyperlinkButton>().First(link => link.Content as string == text && link.IsVisible);
            SameLink("Remove station").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            StationWait(same, () => same.Controller.Flyout is { Checking: false }, "the Remove preview");
            same.Host.RefreshPanes();
            same.Settle();
            var go = SameLink("Apply");
            go.Focus();
            go.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Return, Source = go });
            StationWait(same, () => StationDesktopTests.StationCount(same.Controller) == 2, "Return on Apply");
        });
    }
}
