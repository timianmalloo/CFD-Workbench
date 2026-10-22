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
