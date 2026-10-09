using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// ELV (M1.2b2 §12.4): the Front and Side elevations and the extracted point layer. Rendered checks read pixels of the
/// realized window at points projected through each view's own mapping (UI-RENDERED-STATE). Windows are shared where a
/// check leaves no trace (every gesture there ends with Escape); a check that commits gets its own window.
/// </summary>
public static class ElevationTests
{
    private static Fixture? example;
    private static Fixture? dihedral;

    private static Fixture Example => example ??= new Fixture();
    private static Fixture Dihedral => dihedral ??= new Fixture(bytes: DihedralBytes());

    // Readiness only: checks moved out of the fast ring (round-oct06 SPL, Ruling 123); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        DesktopChecks.Check("Elevation_NudgeLadders_PerChannelTable", () =>
        {
            var f = Example.Reset();
            foreach (var (view, curve) in new[] { (f.Front, "dihedral"), (f.Front, "thickness"), (f.Side, "twist") })
            {
                var unit = Channels.Unit(curve);
                foreach (var (modifiers, step) in new[] { (KeyModifiers.Meta, unit.NudgeFine), (KeyModifiers.None, unit.NudgePlain), (KeyModifiers.Shift, unit.NudgeCoarse) })
                {
                    var point = f.Curve(curve).Points[3];
                    view.FocusPoint(new PointRef(curve, point.Id));
                    f.Key(view, Key.Up, modifiers);
                    double moved = f.Curve(curve).Points[3].Ordinate - point.Ordinate;
                    f.Cancel(view);
                    if (Math.Abs(moved - step) > step * 1e-6 + 1e-12) throw new Exception($"{curve} {modifiers}: moved {moved}, ladder {step}");
                    if (f.Curve(curve).Points[3].Ordinate != point.Ordinate) throw new Exception("Escape did not restore " + curve);
                }
            }
        });
    }

    public static void Run()
    {
        try
        {
            SamplerChecks();
            FrontChecks();
            SideChecks();
            InteractionChecks();
            AccessibilityChecks();
            // The proof capture (docs/proof/m12b2-elv): Four views of the mockup's 60 mm-dihedral Example, tip station selected.
            if (Environment.GetEnvironmentVariable("CFD_ELV_CAPTURE") is { Length: > 0 } capture)
            {
                var f = Dihedral.Reset();
                f.Controller.Select(new Selection.Station(f.Controller.Planform!.Stations.Count - 1, 1));
                f.Save(capture);
            }
        }
        finally
        {
            example?.Dispose();
            dihedral?.Dispose();
        }
    }

    // ECR, pure (fast ring, no window): a 96-dpi capture splits a 1 DIP line over two pixels at a fractional scale.
    private static void SamplerChecks()
    {
        DesktopChecks.Check("Elevation_ChipBorderSampler_HoldsHalfOfASplitLine", () =>
        {
            (byte R, byte G, byte B) ground = (0x1a, 0x28, 0x2b), line = (0x66, 0xdd, 0xc8);
            (byte R, byte G, byte B) Blend(double t) => ((byte)Math.Round(ground.R + (line.R - ground.R) * t),
                (byte)Math.Round(ground.G + (line.G - ground.G) * t), (byte)Math.Round(ground.B + (line.B - ground.B) * t));
            if (Distance(Blend(2.0 / 3), line) <= 60) throw new Exception("The fixture split is not past the old exact threshold");
            if (!DevicePixel.HoldsHalfOf(Blend(2.0 / 3), line, ground)) throw new Exception("The 2/3 half of a split line is refused");
            if (!DevicePixel.HoldsHalfOf(Blend(.5), line, ground)) throw new Exception("An even split is refused");
            if (DevicePixel.HoldsHalfOf(Blend(1.0 / 3), line, ground)) throw new Exception("The 1/3 half of a split line is accepted");
            if (DevicePixel.HoldsHalfOf(ground, line, ground)) throw new Exception("A capture without the line is accepted");
        });
    }

    private static void FrontChecks()
    {
        DesktopChecks.Check("Elevation_FrontBand_RenderedFromSurfaceView", () =>
        {
            var f = Example.Reset();
            var surface = f.Controller.Surface!;
            if (!ReferenceEquals(f.Front.Band!.Surface, surface)) throw new Exception("The Front band does not draw the controller's mesh");
            f.Shoot();
            var row = surface.Sections[16];
            double top = row.Upper.Max(point => point.Z), bottom = row.Lower.Min(point => point.Z);
            var inside = new Point3(0, row.Upper[0].Y, top * 0.6);
            var fill = f.Fill();
            var pixel = f.RgbAtBand(f.Front, inside);
            if (Distance(pixel, fill) > 12) throw new Exception($"Band interior {pixel} is not the band fill {fill}");
            foreach (double z in new[] { top, bottom })
            {
                var edge = f.NearestAtBand(f.Front, new Point3(0, row.Upper[0].Y, z), f.Brush("PlanFoilBrush"), 2);
                if (Distance(edge, fill) < 90 || Contrast(edge, f.Brush("ViewportBrush")) < 3)
                    throw new Exception($"Band outline at z {z} is {edge}: not a foil stroke over the fill {fill}");
            }
            var outside = f.RgbAtBand(f.Front, new Point3(0, row.Upper[0].Y, top + 0.012));
            if (outside != f.Brush("ViewportBrush")) throw new Exception($"Above the band is {outside}, not the viewport");
        });

        DesktopChecks.Check("Elevation_FrontBand_StarboardPointsOnViewerLeftRendered", () =>
        {
            var f = Example.Reset();
            var layer = f.Front.LayerFor("dihedral")!;
            var tip = f.Curve("dihedral").Points[^1];
            double centre = layer.ToScreen(0, 0).X;
            if (!(layer.ToScreen(tip).X < centre - 100)) throw new Exception("The starboard tip end is not on the viewer's left");
            var control = f.Curve("dihedral").Points[4];
            var starboard = layer.ToScreen(control) + new Vector(0, -4);
            var port = new Point(2 * centre - starboard.X, starboard.Y);
            f.Shoot();
            var foil = f.Brush("PlanFoilBrush");
            if (Distance(f.RgbAt(f.Front, starboard), foil) > 40) throw new Exception("No control glyph at the starboard (left) point: " + f.RgbAt(f.Front, starboard));
            if (Distance(f.RgbAt(f.Front, port), foil) < 90) throw new Exception("A point glyph was drawn on the port half: " + f.RgbAt(f.Front, port));
        });

        DesktopChecks.Check("Elevation_FrontDihedralFrame_PointsOnLeadingEdgeLine", () =>
        {
            var f = Dihedral.Reset();
            var surface = f.Controller.Surface!;
            var view = f.Curve("dihedral");
            var layer = f.Front.LayerFor("dihedral")!;
            var camera = f.Front.Camera!.Value;
            var size = f.Front.BandRect.Size;
            f.Shoot();
            foreach (int row in new[] { 8, 16, 24, 32 })
            {
                var leading = surface.Sections[row].Upper[0];
                double height = Interpolate(view.Samples, leading.Y);
                if (Math.Abs(height - leading.Z) > 2e-5) throw new Exception($"Row {row}: dihedral {height} is off the LE height {leading.Z}");
                var onCurve = layer.ToScreen(leading.Y, height);
                var onMesh = camera.Project(leading, size);
                if (Point.Distance(onCurve, onMesh) > 0.5) throw new Exception($"Row {row}: dihedral {onCurve} vs LE {onMesh}");
                var pixel = f.NearestAt(f.Front, onMesh, f.Brush("PlanFoilBrush"), 1);
                if (Distance(pixel, f.Brush("PlanFoilBrush")) > 40) throw new Exception($"Row {row}: no foil LE line at {onMesh}: {pixel}");
            }
            var tipEnd = view.Points[^1];
            if (Point.Distance(layer.ToScreen(tipEnd), camera.Project(surface.Sections[^1].Upper[0], size)) > 0.5)
                throw new Exception("The dihedral tip end is not on the tip's leading edge");
        });

        DesktopChecks.Check("Elevation_FrontThicknessLane_CaptionedSharedSpanAxis", () =>
        {
            var f = Example.Reset();
            Equal("Thickness t/c (%) · tip ← root", f.Front.LaneCaption, "caption");
            var lane = f.Front.LayerFor("thickness")!;
            var bandLayer = f.Front.LayerFor("dihedral")!;
            foreach (var point in f.Curve("thickness").Points)
                Near(bandLayer.ToScreen(point.SpanMeters, 0).X, lane.ToScreen(point).X, 1e-6, "shared span axis at " + point.Id);
            var points = f.Curve("thickness").Points;
            if (!(lane.ToScreen(points[^1]).X < lane.ToScreen(points[0]).X)) throw new Exception("t/c tip is not left of its root");
            if (!f.Front.LaneRect.Contains(lane.ToScreen(points[3]))) throw new Exception("t/c points are outside the lane");
            f.Shoot();
            var caption = new Point(10, f.Front.BandRect.Bottom + 9);
            if (Distance(f.RgbAt(f.Front, caption), f.Brush("PlanSoftBrush")) > 20) throw new Exception("No caption plate under the band");
            var mid = lane.ToScreen((points[2].SpanMeters + points[3].SpanMeters) / 2, Interpolate(f.Curve("thickness").Samples, (points[2].SpanMeters + points[3].SpanMeters) / 2));
            if (Distance(f.NearestAt(f.Front, mid, f.Brush("PlanFoilBrush"), 1), f.Brush("PlanFoilBrush")) > 40) throw new Exception("No t/c curve in the lane");
        });

        DesktopChecks.Check("Elevation_FrontLane_ArrowsFollowScreenAxes", () =>
        {
            var f = Example.Reset();
            var point = f.Curve("thickness").Points[3];
            var lane = f.Front.LayerFor("thickness")!;
            var before = lane.ToScreen(point);
            f.Front.FocusPoint(new PointRef(point.Curve, point.Id));
            f.Key(f.Front, Key.Right);
            var right = f.Curve("thickness").Points[3];
            var rightAt = f.Front.LayerFor("thickness")!.ToScreen(right);
            f.Cancel(f.Front);
            f.Front.FocusPoint(new PointRef(point.Curve, point.Id));
            f.Key(f.Front, Key.Up);
            var up = f.Curve("thickness").Points[3];
            var upAt = f.Front.LayerFor("thickness")!.ToScreen(up);
            f.Cancel(f.Front);
            if (!(right.SpanMeters < point.SpanMeters) || !(rightAt.X > before.X) || right.Ordinate != point.Ordinate)
                throw new Exception($"→ moved span {point.SpanMeters} → {right.SpanMeters} (screen {before.X} → {rightAt.X}); it must go toward the root, right on screen");
            if (!(up.Ordinate > point.Ordinate) || !(upAt.Y < before.Y) || up.SpanMeters != point.SpanMeters)
                throw new Exception("↑ did not raise t/c up the screen");
        });
    }

    private static void SideChecks()
    {
        DesktopChecks.Check("ElevationView_DoubleClickOnTwistCurve_AddsPoint", () =>
        {
            var f = Example.Reset();
            var curve = f.Curve("twist");
            var sample = curve.Samples.OrderBy(item => Math.Abs(item.SpanMeters / f.Controller.Planform!.HalfSpanMeters - .45)).First();
            var at = f.Side.LayerFor("twist")!.ToScreen(sample.SpanMeters, sample.Ordinate);
            if (f.Side.HitTestPoint(at) is not null) throw new Exception("Twist sample overlaps a point glyph");
            int before = curve.Points.Count;
            var original = f.Controller.Inspection!.Authored.Binding.SourceHash;
            try
            {
                using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
                f.Side.RaiseEvent(new PointerPressedEventArgs(f.Side, pointer, f.Window,
                    f.Side.TranslatePoint(at, f.Window)!.Value, 1,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None, 2));
                Pump(() => f.Curve("twist").Points.Count == before + 1, "the twist double-click add");
                if (f.Controller.Selection is not Selection.Points { Items.Count: 1 })
                    throw new Exception("Twist add did not select the new point");
            }
            finally
            {
                // Leave the shared window as it was: one undo step takes the add back.
                if (f.Controller.Inspection!.Authored.Binding.SourceHash != original) f.Controller.Undo();
                Pump(() => f.Controller.Inspection!.Authored.Binding.SourceHash == original && !f.Controller.SurfaceUpdating, "the undo");
            }
        });
        DesktopChecks.Check("Elevation_SideBodyPlan_AuthoredSectionsOverlaid", () =>
        {
            var f = Dihedral.Reset();
            var surface = f.Controller.Surface!;
            f.Shoot();
            var edge = f.Brush("FoilEdgeBrush");
            var authored = surface.Sections.Where(section => section.Assignment is not null).ToArray();
            Equal(2, authored.Length, "authored sections");
            foreach (var section in authored)
            {
                var pixel = f.NearestAtBand(f.Side, section.Upper[50], edge, 1);
                if (Contrast(pixel, f.Brush("ViewportBrush")) < 3 || Distance(pixel, edge) > 80) throw new Exception($"Section η {section.Eta} is not drawn: {pixel}");
            }
            var intermediate = surface.Sections.First(section => section.Eta == 0.5);
            var off = f.RgbAtBand(f.Side, intermediate.Upper[50]);
            if (off != f.Brush("ViewportBrush")) throw new Exception($"An intermediate section is drawn: {off}");
            Equal(2, f.Side.SideSections().Count, "body-plan sections");
        });

        DesktopChecks.Check("Elevation_SideSignFixture_ExampleTipTrailingEdgeRenderedHigher", () =>
        {
            var f = Example.Reset();
            var tip = f.Controller.Surface!.Sections[^1];
            var camera = f.Side.Camera!.Value;
            var size = f.Side.BandRect.Size;
            var leading = camera.Project(tip.Upper[0], size);
            var trailing = camera.Project(tip.Upper[^1], size);
            if (!(trailing.Y < leading.Y - 10)) throw new Exception($"Tip TE {trailing} is not higher than its LE {leading}");
            if (!(leading.X > trailing.X)) throw new Exception("The nose is not on the right");
            f.Shoot();
            foreach (var at in new[] { tip.Upper[^1], tip.Upper[1] })
                if (Contrast(f.NearestAtBand(f.Side, at, f.Brush("FoilEdgeBrush"), 1), f.Brush("ViewportBrush")) < 3)
                    throw new Exception("The tip section is not drawn at " + at);
        });

        DesktopChecks.Check("Elevation_SideSelectedStation_RenderedFullWeight", () =>
        {
            var f = Example.Reset();
            var plan = f.Controller.Planform!;
            int tipIndex = plan.Stations.Count - 1;
            f.Controller.Select(new Selection.Station(tipIndex, 1));
            f.Shoot();
            var tip = f.Controller.Surface!.Sections[^1];
            var root = f.Controller.Surface!.Sections[0];
            var station = f.Brush("PlanSelectionBrush");
            var at = f.WindowPoint(f.Side, f.Side.Camera!.Value.Project(tip.Upper[50], f.Side.BandRect.Size));
            int run = 0;
            for (int dy = -4; dy <= 4; dy++)
                if (Distance(f.Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y) + dy), station) <= 40) run++;
            if (run < 3) throw new Exception($"The selected tip section is {run} px of station, not 3");
            var rootPixel = f.NearestAtBand(f.Side, root.Upper[50], f.Brush("FoilEdgeBrush"), 1);
            if (Contrast(rootPixel, f.Brush("ViewportBrush")) < 3 || Distance(rootPixel, f.Brush("FoilEdgeBrush")) > 80 || Distance(rootPixel, f.Brush("PlanSelectionBrush")) < Distance(rootPixel, f.Brush("FoilEdgeBrush"))) throw new Exception("The root lost its 1 px foil-edge weight: " + rootPixel);
            var chip = f.Side.Chips().FirstOrDefault(item => item.Name == "Tip");
            if (chip.Name is null) throw new Exception("No Tip chip");
            if (!DevicePixel.HoldsHalfOf(f.NearestAt(f.Side, new Point(chip.Bounds.Left + 0.5, chip.Bounds.Center.Y), station, 1), station, f.Brush("ViewportBrush"))) throw new Exception("The chip has no station border");
        });

        DesktopChecks.Check("Elevation_SideClickSection_SelectsStation", () =>
        {
            var f = Example.Reset();
            var surface = f.Controller.Surface!;
            var camera = f.Side.Camera!.Value;
            var size = f.Side.BandRect.Size;
            f.Click(f.Side, camera.Project(surface.Sections[^1].Upper[50], size));
            if (f.Controller.Selection is not Selection.Station { Eta: 1 } tip) throw new Exception("Clicking the tip section did not select it: " + f.Controller.Selection);
            f.Click(f.Side, camera.Project(surface.Sections[0].Upper[50], size));
            if (f.Controller.Selection is not Selection.Station { Index: 0, Eta: 0 }) throw new Exception("Clicking the root section did not select it");
            _ = tip;
        });

        DesktopChecks.Check("Elevation_SideTwistLane_ZeroLinePositiveUpRootLeft", () =>
        {
            var f = Example.Reset();
            Equal("Twist (°) · root → tip", f.Side.LaneCaption, "caption");
            var layer = f.Side.LayerFor("twist")!;
            var points = f.Curve("twist").Points;
            if (!(layer.ToScreen(points[0]).X < layer.ToScreen(points[^1]).X)) throw new Exception("Twist root is not left of the tip");
            if (!(layer.ToScreen(0, 1).Y < layer.ToScreen(0, 0).Y)) throw new Exception("Positive twist is not up");
            var range = f.Side.LaneRange("twist");
            if (!(range.Low < 0 && range.High > 0)) throw new Exception("The lane has no zero line");
            f.Shoot();
            int background = f.Brush("ViewportBrush").G, mute = f.Brush("PlanMuteBrush").G - background, grid = f.Brush("ViewportGridBrush").G - background;
            double zeroInk = f.Ink(f.Side, new Point(f.Side.LaneRect.Right - 4, layer.ToScreen(0, 0).Y), background);
            double tickInk = f.Ink(f.Side, new Point(f.Side.LaneRect.Right - 4, layer.ToScreen(0, -1).Y), background);
            if (zeroInk < 0.75 * mute) throw new Exception($"No mute zero line: ink {zeroInk} of {mute}");
            if (tickInk < 0.5 * grid || tickInk > 1.5 * grid) throw new Exception($"No grid −1° tick: ink {tickInk} of {grid}");
        });
    }

    private static void InteractionChecks()
    {
        DesktopChecks.Check("Elevation_DragDihedralPoint_BandAnd3dFollowBeforeRelease", () =>
        {
            var f = Example.Reset();
            string original = f.Controller.Inspection!.Authored.Binding.SourceHash;
            try
            {
                var point = f.Curve("dihedral").Points[^1];
                var accepted = f.Controller.Surface!;
                var layer = f.Front.LayerFor("dihedral")!;
                var start = layer.ToScreen(point);
                var pointer = f.Press(f.Front, start);
                f.Move(f.Front, pointer, start + new Vector(0, -30));
                Pump(() => f.Controller.Surface?.Basis == "preview" && !f.Controller.SurfaceUpdating, "the draft mesh");
                f.Settle();
                var surface = f.Controller.Surface!;
                if (!ReferenceEquals(f.Front.Band!.Surface, surface) || !ReferenceEquals(f.Area.ThreeDRenderer.Surface, surface))
                    throw new Exception("The band and 3D do not draw the draft mesh during the drag");
                if (!(surface.Sections[^1].Upper[0].Z > 0.005)) throw new Exception("The draft tip did not rise");
                if (f.Front.ProbeText?.Contains("Δ height +", StringComparison.Ordinal) != true) throw new Exception("Probe: " + f.Front.ProbeText);
                f.Shoot();
                var row = surface.Sections.Count - 3;
                var raised = surface.Sections[row];
                var was = accepted.Sections.First(section => section.Eta == raised.Eta);
                if (!(raised.Upper[0].Z - was.Upper[0].Z > 0.01)) throw new Exception("The draft row near the tip did not rise 10 mm");
                var now = f.RgbAtBand(f.Front, new Point3(0, raised.Upper[0].Y, (raised.Upper.Max(p => p.Z) + raised.Upper[0].Z) / 2));
                var before = f.RgbAtBand(f.Front, new Point3(0, was.Upper[0].Y, (was.Upper.Max(p => p.Z) + was.Upper[0].Z) / 2));
                if (Contrast(now, f.Brush("ViewportBrush")) < 1.5) throw new Exception($"The band does not show the raised tip before release: {now}");
                if (before != f.Brush("ViewportBrush")) throw new Exception($"The band still shows the tip where it was: {before}");
                f.Release(f.Front, pointer, start + new Vector(0, -30));
                Pump(() => f.Controller.Gesture == GestureState.Idle && f.Controller.Draft is null, "the commit");
                if (!(f.Curve("dihedral").Points[^1].Ordinate > point.Ordinate)) throw new Exception("The release did not commit the raised point");
            }
            finally
            {
                // Leave the shared window as it was: one undo step takes the commit back (§0.1 step 7).
                if (f.Controller.Gesture != GestureState.Idle) Await(f.Controller.EndGestureAsync(GestureEnd.Escape));
                if (f.Controller.Inspection!.Authored.Binding.SourceHash != original) f.Controller.Undo();
                Pump(() => f.Controller.Inspection!.Authored.Binding.SourceHash == original && !f.Controller.SurfaceUpdating, "the undo");
            }
        });

        DesktopChecks.Check("Elevation_ShiftDrag_PointLocksAxisEmptySpacePans", () =>
        {
            var f = Example.Reset();
            var point = f.Curve("thickness").Points[3];
            var start = f.Front.LayerFor("thickness")!.ToScreen(point);
            var pointer = f.Press(f.Front, start);
            f.Move(f.Front, pointer, start + new Vector(40, 6), KeyModifiers.Shift);
            Pump(() => f.Controller.Draft is not null && f.Curve("thickness").Points[3].SpanMeters != point.SpanMeters, "the locked drag frame");
            var moved = f.Curve("thickness").Points[3];
            f.Key(f.Front, Key.Escape);
            f.Release(f.Front, pointer, start + new Vector(40, 6));
            Pump(() => f.Controller.Gesture == GestureState.Idle, "Escape");
            if (moved.Ordinate != point.Ordinate) throw new Exception("Shift-drag did not lock the small vertical component");
            if (!(moved.SpanMeters < point.SpanMeters)) throw new Exception("Shift-drag right did not move toward the root");
            var before = f.Front.Camera!.Value;
            var empty = new Point(f.Front.Bounds.Width * 0.75, f.Front.BandRect.Height * 0.2);
            var drag = f.Press(f.Front, empty, KeyModifiers.Shift);
            f.Move(f.Front, drag, empty + new Vector(25, 10), KeyModifiers.Shift);
            f.Release(f.Front, drag, empty + new Vector(25, 10), KeyModifiers.Shift);
            var expected = before.Pan(25, 10, f.Front.BandRect.Size);
            if (f.Front.Camera != expected) throw new Exception($"Shift-drag on empty space did not pan: {f.Front.Camera} vs {expected}");
            if (f.Controller.Draft is not null || f.Controller.Gesture != GestureState.Idle) throw new Exception("Panning started a gesture");
        });

        DesktopChecks.Check("Elevation_TabOrder_DihedralThenThicknessThenLeaves", () =>
        {
            var f = Example.Reset();
            var expected = f.Curve("dihedral").Points.Select(point => "dihedral:" + point.Id)
                .Concat(f.Curve("thickness").Points.Select(point => "thickness:" + point.Id)).ToArray();
            Equal(string.Join(",", expected), string.Join(",", f.Front.KeyboardTargets), "Front target order");
            f.Front.Focus(NavigationMethod.Tab);
            f.Settle();
            var visited = new List<string>();
            do visited.Add(f.Front.FocusedTarget!.Curve + ":" + f.Front.FocusedTarget!.VertexId);
            while (f.Key(f.Front, Key.OemCloseBrackets));
            Equal(string.Join(",", expected), string.Join(",", visited), "] walks dihedral then t/c");
            Equal(string.Join(",", f.Curve("twist").Points.Select(point => "twist:" + point.Id)), string.Join(",", f.Side.KeyboardTargets), "Side order");
        });

        DesktopChecks.Check("Elevation_TabPastLastPoint_LeavesView", () =>
        {
            var f = Example.Reset();
            var last = f.Curve("thickness").Points[^1];
            f.Front.FocusPoint(new PointRef(last.Curve, last.Id));
            f.Key(f.Front, Key.Tab);
            var focused = TopLevel.GetTopLevel(f.Front)?.FocusManager?.GetFocusedElement();
            if (ReferenceEquals(focused, f.Front) || f.Front.FocusedTarget is not null)
                throw new Exception("Tab from the last point stayed in the view (a trap): " + focused);
            f.Front.FocusPoint(new PointRef(last.Curve, last.Id));
            var next = KeyboardNavigationHandler.GetNext(f.Front, NavigationDirection.Next);
            if (next is null || ReferenceEquals(next, f.Front) || next is Visual visual && visual.GetVisualAncestors().Contains(f.Front))
                throw new Exception("Tab from the last point does not leave the view: " + next);
        });

        DesktopChecks.Check("Elevation_TwistDomainClamp_ProbeShowsReason", () =>
        {
            var f = Example.Reset();
            var tip = f.Curve("twist").Points[^1];
            var layer = f.Side.LayerFor("twist")!;
            var start = layer.ToScreen(tip);
            var far = new Point(start.X, layer.ToScreen(tip.SpanMeters, -80).Y);
            var pointer = f.Press(f.Side, start);
            f.Move(f.Side, pointer, far);
            try
            {
                if (f.Side.ProbeText?.EndsWith(ElevationView.TwistClampReason, StringComparison.Ordinal) != true)
                    throw new Exception("Probe has no clamp reason: " + f.Side.ProbeText);
                Equal("Twist is limited to ±57.30° — larger angles can't be checked yet.", ElevationView.TwistClampReason, "copy");
                Pump(() => f.Controller.Draft is not null && f.Curve("twist").Points[^1].Ordinate < tip.Ordinate - 1, "the clamped frame");
                double shown = f.Curve("twist").Points[^1].Ordinate;
                if (shown < -CfdWorkbench.Core.Geometry.TwistDomainDegrees - 1e-9) throw new Exception($"Twist {shown} passed the domain");
            }
            finally
            {
                f.Key(f.Side, Key.Escape);
                f.Release(f.Side, pointer, far);
                Pump(() => f.Controller.Gesture == GestureState.Idle, "Escape");
            }
        });

        DesktopChecks.Check("Elevation_FocusOffscreenPoint_PansIntoView", () =>
        {
            var f = Example.Reset();
            var point = f.Curve("dihedral").Points[3];
            f.Front.PanBy(4000, 0);
            if (f.Front.ScreenPoint(point).X < f.Front.Bounds.Width) throw new Exception("The pan did not take the point off screen");
            f.Front.FocusPoint(new PointRef(point.Curve, point.Id));
            f.Shoot();
            var at = f.Front.ScreenPoint(point);
            if (at.X < 16 || at.X > f.Front.Bounds.Width - 16) throw new Exception("The focused point was not panned into view: " + at);
            var focus = f.Brush("PlanFocusBrush");
            if (Distance(f.NearestAt(f.Front, at + new Vector(13, 0), focus, 1), focus) > 40) throw new Exception("No focus ring at the panned point");
        });

        DesktopChecks.Check("Elevation_ZoomPanFit_KeyboardAndPointerSameCamera", () =>
        {
            var f = Example.Reset();
            var view = f.Front;
            var size = view.BandRect.Size;
            // EZF (Ruling 172): the baseline fit must come from the settled mesh, or a later fit compares a different mesh.
            if (f.Controller.SurfaceUpdating) throw new Exception("The baseline fit was taken while a mesh was still pending");
            var fitted = view.Camera!.Value;
            view.Focus();
            f.Key(view, Key.OemPlus, KeyModifiers.Meta);
            SameCamera(fitted.ZoomAbout(view.BandRect.Center, 1.2, size), view.Camera!.Value, "⌘= zooms about the centre");
            f.Key(view, Key.Z);
            Near(fitted.Distance, view.Camera!.Value.Distance, fitted.Distance * 1e-12, "Z zooms back out");
            f.Key(view, Key.D0, KeyModifiers.Meta);
            SameCamera(fitted, view.Camera!.Value, "⌘0 fits");
            var pivot = new Point(size.Width * 0.3, size.Height * 0.4);
            f.Wheel(view, pivot, 2);
            SameCamera(fitted.ZoomAbout(pivot, Math.Pow(1.1, 2), size), view.Camera!.Value, "the wheel zooms about the pointer");
            view.Fit();
            f.Key(view, Key.Right, KeyModifiers.Alt);
            SameCamera(fitted.Pan(view.Bounds.Width * .1, 0, size), view.Camera!.Value, "⌥→ pans 10 %");
            view.Fit();
            var start = new Point(size.Width * 0.8, size.Height * 0.15);
            var pointer = f.Press(view, start, button: MouseButton.Middle);
            f.Move(view, pointer, start + new Vector(30, -12), button: MouseButton.Middle);
            f.Release(view, pointer, start + new Vector(30, -12), button: MouseButton.Middle);
            SameCamera(fitted.Pan(30, -12, size), view.Camera!.Value, "a middle-drag pans by the pointer");
        });

        DesktopChecks.Check("Elevation_LockedDihedralRoot_AssertiveLockCopy", () =>
        {
            var f = Example.Reset();
            var root = f.Curve("dihedral").Points[0];
            Equal(PointFreedom.Fixed, root.Freedom, "the dihedral root's freedom");
            var pointer = f.Press(f.Front, f.Front.ScreenPoint(root));
            f.Release(f.Front, pointer, f.Front.ScreenPoint(root));
            string line = StatusStripTests.Text(f.Host).Text ?? "";
            if (f.Controller.Gesture != GestureState.Idle || f.Controller.Status != WorkbenchController.DihedralRootLocked ||
                line != WorkbenchController.DihedralRootLocked || AutomationProperties.GetLiveSetting(f.Front) != AutomationLiveSetting.Assertive)
                throw new Exception($"Pressing the root: gesture {f.Controller.Gesture}, status \"{f.Controller.Status}\", line \"{line}\"");
            Equal("The dihedral root is at the centre line. It can't be moved.", WorkbenchController.DihedralRootLocked, "copy");
            f.Controller.Select(new Selection.Foil());
            f.Front.FocusPoint(new PointRef(root.Curve, root.Id));
            f.Key(f.Front, Key.Up);
            if (f.Controller.Gesture != GestureState.Idle || f.Controller.Status != WorkbenchController.DihedralRootLocked ||
                AutomationProperties.GetLiveSetting(f.Front) != AutomationLiveSetting.Assertive)
                throw new Exception("The keyboard path did not announce the lock");
        });
    }

    private static void AccessibilityChecks()
    {
        DesktopChecks.Check("Elevation_AutomationPeers_BandGroupChannelNamesCarryValueAndUnit", () =>
        {
            var f = Example.Reset();
            var peer = ControlAutomationPeer.CreatePeerForElement(f.Front);
            Equal(AutomationControlType.Group, peer.GetAutomationControlType(), "band type");
            if (!peer.GetName().StartsWith("Front view of the foil, span 900 mm, tip height 0.0 mm", StringComparison.Ordinal))
                throw new Exception("Band name: " + peer.GetName());
            var children = peer.GetChildren();
            Equal(f.Front.KeyboardTargets.Count, children.Count, "point children");
            Equal("Dihedral, point 1 of 7, root end, from root 0.00 mm, height 0.00 mm", children[0].GetName(), "first dihedral point");
            if (!children.Any(child => child.GetName() == "Thickness, point 4 of 7, control point, from root 225.00 mm, t/c 12.00 %"))
                throw new Exception("No t/c child carrying its value and unit: " + string.Join(" | ", children.Select(child => child.GetName())));
            if (children.Any(child => child.GetAutomationControlType() != AutomationControlType.Button || child.GetBoundingRectangle().Width != 28))
                throw new Exception("A point child is not a 28 px Button");
            var side = ControlAutomationPeer.CreatePeerForElement(f.Side);
            if (!side.GetName().StartsWith("Side view of the foil, 2 authored sections, tip twist −2.00°", StringComparison.Ordinal))
                throw new Exception("Side band name: " + side.GetName());
            Equal("Twist, point 5 of 7, control point, from root 315.00 mm, twist −1.00°", side.GetChildren()[4].GetName(), "twist child");
        });

        DesktopChecks.Check("Elevation_HoverProbe_AllChannelsAtEta", () =>
        {
            var f = Example.Reset();
            var lane = f.Front.LayerFor("thickness")!;
            var at = lane.ToScreen(0.5 * f.Controller.Planform!.HalfSpanMeters, 0.03);
            // GUI-AMBIENT-INPUT: read the probe before the dispatcher runs, so the OS pointer cannot overwrite it.
            f.Front.HoverAt(at);
            string? probe = f.Front.ProbeText;
            double eta = Math.Abs(lane.FromScreen(at).Span) / f.Controller.Planform!.HalfSpanMeters;
            var frame = Placement.Frame(System.Text.Encoding.UTF8.GetBytes(f.Controller.AcceptedSource), eta);
            string expected = $"η {eta:F3} · from root {ElevationView.Number(frame.SpanMeters * 1000)} mm · height {ElevationView.Number(frame.ElevationMeters * 1000)} mm · " +
                $"twist {ElevationView.Number(frame.TwistDegrees)}° · t/c {ElevationView.Number(frame.ThicknessRatio * 100)} % · chord {ElevationView.Number(frame.ChordMeters * 1000)} mm";
            Equal(expected, probe, "Front lane probe");
            if (probe?.Contains("η 0.500", StringComparison.Ordinal) != true || !probe.Contains("twist −0.5", StringComparison.Ordinal))
                throw new Exception("The probe is not at η 0.500: " + probe);
            f.Side.HoverAt(f.Side.LayerFor("twist")!.ToScreen(0.5 * f.Controller.Planform!.HalfSpanMeters, 0.4));
            if (f.Side.ProbeText?.StartsWith("η 0.500 · from root 225.00 mm · height 0.00 mm · twist −0.", StringComparison.Ordinal) != true)
                throw new Exception("Side lane probe: " + f.Side.ProbeText);
        });

        DesktopChecks.Check("Elevation_SideBandHover_ProbeNamesStation", () =>
        {
            var f = Example.Reset();
            var camera = f.Side.Camera!.Value;
            var tip = f.Controller.Surface!.Sections[^1];
            f.Side.HoverAt(camera.Project(tip.Upper[50], f.Side.BandRect.Size));
            Equal("Tip · η 1.000 · twist −2.00° · chord 120.00 mm", f.Side.ProbeText, "section under the pointer");
            var empty = new Point(8, f.Side.BandRect.Height - 8);
            f.Side.HoverAt(empty);
            if (f.Side.ProbeText is not null) throw new Exception("Empty band with nothing selected still names a section: " + f.Side.ProbeText);
            f.Controller.Select(new Selection.Station(0, 0));
            f.Side.HoverAt(empty);
            Equal("Root · η 0.000 · twist 0.00° · chord 120.00 mm", f.Side.ProbeText, "the selected station");
        });

        DesktopChecks.Check("Elevation_FocusedPoint_ProbeFollows", () =>
        {
            var f = Example.Reset();
            var point = f.Curve("twist").Points[4];
            f.Side.FocusPoint(new PointRef(point.Curve, point.Id));
            Equal("Twist · point 5 of 7 · η 0.700 · from root 315.00 mm · twist −1.00°", f.Side.ProbeText, "probe on focus");
            f.Key(f.Side, Key.OemCloseBrackets);
            if (f.Side.ProbeText?.StartsWith("Twist · point 6 of 7 · η 0.900", StringComparison.Ordinal) != true)
                throw new Exception("The probe did not follow ]: " + f.Side.ProbeText);
        });

        DesktopChecks.Check("Elevation_Probe_NotLiveRegionWrapsInNarrowView", () =>
        {
            var f = Example.Reset();
            f.Front.HoverAt(f.Front.LayerFor("thickness")!.ToScreen(0.2, 0.03));
            if (f.Front.Bounds.Width < ElevationView.ProbeWrapWidth) throw new Exception("The wide fixture is narrow");
            Equal(1, f.Front.ProbeLines.Count, "one line in a wide view");
            if (AutomationProperties.GetLiveSetting(f.Front) != AutomationLiveSetting.Off ||
                f.Front.GetVisualDescendants().OfType<Control>().Any(control => AutomationProperties.GetLiveSetting(control) != AutomationLiveSetting.Off))
                throw new Exception("The probe is a live region");
            var view = f.Front;
            f.Window.Width = 1000;
            f.Window.Height = 800;
            try
            {
                f.Settle();
                if (!(view.Bounds.Width < ElevationView.ProbeWrapWidth)) throw new Exception("The narrow window is wide: " + view.Bounds.Width);
                view.HoverAt(view.LayerFor("thickness")!.ToScreen(0.2, 0.03));
                Equal(2, view.ProbeLines.Count, "two lines in a narrow view");
                var box = view.ProbeBounds;
                if (box.Width > view.Bounds.Width * ElevationView.ProbeWrapShare + 0.5) throw new Exception($"Probe {box.Width} px is wider than 46 % of {view.Bounds.Width}");
                f.Shoot();
                if (Distance(f.RgbAt(view, new Point(box.Right - 3, box.Bottom - 3)), f.Brush("PlanSoftBrush")) > 20) throw new Exception("The wrapped probe plate is not drawn");
            }
            finally
            {
                f.Window.Width = 1400;
                f.Window.Height = 1000;
                f.Settle();
            }
        });

        DesktopChecks.Check("Elevation_SelectedPoint_RenderedFilledAtLeastThreeToOne", () =>
        {
            var f = Example.Reset();
            var point = f.Curve("twist").Points[3];
            f.Shoot();
            var centre = f.Side.ScreenPoint(point);
            var before = f.RgbAt(f.Side, centre);
            f.Controller.Select(new Selection.Points([new PointRef(point.Curve, point.Id)]));
            f.Shoot();
            var after = f.RgbAt(f.Side, centre);
            if (before == after) throw new Exception("The selected glyph is drawn like an unselected one");
            if (Contrast(after, f.Brush("ViewportBrush")) < 3) throw new Exception($"Selected glyph {after} is under 3:1 on the viewport");
        });

        DesktopChecks.Check("CurvePointLayer_PlanAndLane_SameGlyphPixels", () =>
        {
            var f = Example.Reset();
            var plan = f.Controller.Planform!.Trailing.Points[4];
            var lane = f.Curve("twist").Points[3];
            foreach (bool selected in new[] { false, true })
            {
                f.Controller.Select(selected ? new Selection.Points([new PointRef(plan.Curve, plan.Id), new PointRef(lane.Curve, lane.Id)]) : new Selection.Foil());
                f.Settle();
                // One glyph code draws both; put the Plan glyph on the lane glyph's sub-pixel phase so anti-aliasing matches too.
                var a = f.WindowPoint(f.Area.PlanCanvas, f.Area.PlanCanvas.ScreenPoint(plan));
                var b = f.WindowPoint(f.Side, f.Side.ScreenPoint(lane));
                static double Phase(double value) => value - Math.Floor(value);
                f.Area.PlanCanvas.PanBy(Phase(b.X) - Phase(a.X), Phase(b.Y) - Phase(a.Y));
                f.Shoot();
                a = f.WindowPoint(f.Area.PlanCanvas, f.Area.PlanCanvas.ScreenPoint(plan));
                int compared = 0;
                for (int dy = -5; dy <= 5; dy++)
                    for (int dx = -5; dx <= 5; dx++)
                    {
                        if (dx * dx + dy * dy > 20) continue;   // the glyph alone: inside the 5.5 px circle, clear of the curves through it
                        var plain = f.Rgb((int)Math.Floor(a.X) + dx, (int)Math.Floor(a.Y) + dy);
                        var other = f.Rgb((int)Math.Floor(b.X) + dx, (int)Math.Floor(b.Y) + dy);
                        if (Distance(plain, other) > 12) throw new Exception($"Selected {selected}: glyph pixel ({dx},{dy}) Plan {plain} vs lane {other}");
                        compared++;
                    }
                if (compared < 60) throw new Exception("Too few glyph pixels compared");
                f.Area.PlanCanvas.Fit();
            }
        });
    }

    // ---- fixtures and helpers ------------------------------------------------------------------------------------

    /// <summary>The Example with a 60 mm tip dihedral, as the approved mockup draws it (the fixture itself has none).</summary>
    private static byte[] DihedralBytes()
    {
        string source = System.Text.Encoding.UTF8.GetString(CfdWorkbench.Cli.Cli.ExampleBytes());
        const string flat = "dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)]";
        if (!source.Contains(flat, StringComparison.Ordinal)) throw new Exception("The Example's dihedral row changed");
        return System.Text.Encoding.UTF8.GetBytes(source.Replace(flat,
            "dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 2), (0.5, 9), (0.7, 22), (0.9, 42), (1, 60)]", StringComparison.Ordinal));
    }

    private static double Interpolate(IReadOnlyList<PlanSample> samples, double span)
    {
        for (int index = 1; index < samples.Count; index++)
            if (samples[index].SpanMeters >= span)
            {
                var (a, b) = (samples[index - 1], samples[index]);
                double t = b.SpanMeters == a.SpanMeters ? 0 : (span - a.SpanMeters) / (b.SpanMeters - a.SpanMeters);
                return a.Ordinate + t * (b.Ordinate - a.Ordinate);
            }
        return samples[^1].Ordinate;
    }

    private sealed class Fixture : IDisposable
    {
        private byte[]? frameBytes;
        private int stride, frameWidth, frameHeight;
        public WorkbenchController Controller { get; } = new();
        public ShellHost Host { get; }
        public ModelArea Area => Host.ModelView;
        public Window Window { get; }
        public ElevationView Front => Area.FindControl<ElevationView>("FrontElevation")!;
        public ElevationView Side => Area.FindControl<ElevationView>("SideElevation")!;

        public Fixture(int width = 1400, int height = 1000, byte[]? bytes = null)
        {
            if (bytes is null) Await(Controller.OpenExampleAsync());
            else Await(Controller.OpenFoilAsync(bytes, "Dihedral fixture"));
            if (Controller.Inspection?.Geometry.Status != GeometryStatus.Certified) throw new Exception("The fixture foil is not certified");
            Host = new ShellHost(Controller);
            Window = new Window { Content = Host, Width = width, Height = height, RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark };
            Window.Show();
            Host.RefreshPanes();
            Controller.Layout = ViewLayout.Four;
            Settle();
            Pump(() => Controller.Surface is not null && !Controller.SurfaceUpdating && Front.Camera is not null && Side.Camera is not null, "first mesh and cameras");
            Settle();
        }

        /// <summary>A shared window back to its first state: nothing selected or focused, both elevations fitted.</summary>
        public Fixture Reset()
        {
            if (Controller.Gesture != GestureState.Idle) Await(Controller.EndGestureAsync(GestureEnd.Escape));
            Controller.Select(new Selection.Foil());
            // EZF: the previous check's Escape leaves an accepted mesh in flight; a fit before it lands compares two meshes.
            Pump(() => !Controller.SurfaceUpdating, "the last mesh");
            Front.Fit();
            Side.Fit();
            AutomationProperties.SetLiveSetting(Front, AutomationLiveSetting.Off);
            Area.PlanLabel.Focus();
            Settle();
            return this;
        }

        public CurveView Curve(string curve) => Controller.CurveFor(curve) ?? throw new Exception("No curve " + curve);

        public void Settle()
        {
            for (int i = 0; i < 4; i++)
            {
                Dispatcher.UIThread.RunJobs();
                Window.UpdateLayout();
            }
            frameBytes = null;
        }

        public void Shoot()
        {
            Settle();
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Window.Bounds.Width, (int)Window.Bounds.Height));
            bitmap.Render(Window);
            using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
            using var frame = pixels.Lock();
            bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
            stride = frame.RowBytes;
            frameWidth = frame.Size.Width;
            frameHeight = frame.Size.Height;
            frameBytes = new byte[stride * frameHeight];
            Marshal.Copy(frame.Address, frameBytes, 0, frameBytes.Length);
        }

        public void Save(string path)
        {
            Settle();
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Window.Bounds.Width, (int)Window.Bounds.Height));
            bitmap.Render(Window);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            bitmap.Save(path);
        }

        public (byte R, byte G, byte B) Rgb(int x, int y)
        {
            if (frameBytes is null) throw new InvalidOperationException("Shoot() first");
            if (x < 0 || y < 0 || x >= frameWidth || y >= frameHeight) throw new Exception($"Pixel {x},{y} is outside the window");
            int offset = y * stride + x * 4;
            return (frameBytes[offset + 2], frameBytes[offset + 1], frameBytes[offset]);
        }

        public Point WindowPoint(Visual visual, Point local) => visual.TranslatePoint(local, Window) ?? throw new Exception("No window point");

        public (byte R, byte G, byte B) RgbAt(Visual visual, Point local)
        {
            var at = WindowPoint(visual, local);
            return Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y));
        }

        public (byte R, byte G, byte B) RgbAtBand(ElevationView view, Point3 point) =>
            RgbAt(view, view.Camera!.Value.Project(point, view.BandRect.Size));

        public (byte R, byte G, byte B) NearestAt(Visual visual, Point local, (byte R, byte G, byte B) target, int radius)
        {
            var at = WindowPoint(visual, local);
            var best = Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y));
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var candidate = Rgb((int)Math.Round(at.X) + dx, (int)Math.Round(at.Y) + dy);
                    if (Distance(candidate, target) < Distance(best, target)) best = candidate;
                }
            return best;
        }

        /// <summary>The green excess over the viewport summed over three rows: a 1 px line's weight wherever it falls between rows.</summary>
        public double Ink(Visual visual, Point local, int background)
        {
            var at = WindowPoint(visual, local);
            double ink = 0;
            for (int dy = -1; dy <= 1; dy++) ink += Math.Max(0, Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y) + dy).G - background);
            return ink;
        }

        public (byte R, byte G, byte B) NearestAtBand(ElevationView view, Point3 point, (byte R, byte G, byte B) target, int radius) =>
            NearestAt(view, view.Camera!.Value.Project(point, view.BandRect.Size), target, radius);

        public (byte R, byte G, byte B) Brush(string key) =>
            Window.TryFindResource(key, Window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
                ? (brush.Color.R, brush.Color.G, brush.Color.B) : throw new Exception("Theme resource " + key + " is missing");

        /// <summary>The Front band's fill: the token ramp at 0.7.</summary>
        public (byte R, byte G, byte B) Fill()
        {
            var (a, b) = (Brush("PlanSoftBrush"), Brush("FoilShadeLitBrush"));
            byte Mix(byte x, byte y) => (byte)Math.Round(x + (y - x) * ElevationView.FrontFill);
            return (Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
        }

        public bool Key(ElevationView view, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var down = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = key, KeyModifiers = modifiers };
            view.RaiseEvent(down);
            Settle();
            return down.Handled;
        }

        /// <summary>Escape ends a keyboard or pointer gesture without a commit.</summary>
        public void Cancel(ElevationView view)
        {
            if (Controller.Gesture != GestureState.Idle) view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Avalonia.Input.Key.Escape });
            Pump(() => Controller.Gesture == GestureState.Idle && Controller.Draft is null, "the cancelled gesture");
            Settle();
        }

        private static (RawInputModifiers Raw, PointerUpdateKind Pressed, PointerUpdateKind Released) Buttons(MouseButton button) => button switch
        {
            MouseButton.Middle => (RawInputModifiers.MiddleMouseButton, PointerUpdateKind.MiddleButtonPressed, PointerUpdateKind.MiddleButtonReleased),
            _ => (RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased)
        };

        public Pointer Press(ElevationView view, Point local, KeyModifiers modifiers = KeyModifiers.None, MouseButton button = MouseButton.Left)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var (raw, pressed, _) = Buttons(button);
            view.RaiseEvent(new PointerPressedEventArgs(view, pointer, Window, WindowPoint(view, local), 1,
                new PointerPointProperties(raw, pressed), modifiers));
            Settle();
            return pointer;
        }

        public void Move(ElevationView view, Pointer pointer, Point local, KeyModifiers modifiers = KeyModifiers.None, MouseButton button = MouseButton.Left)
        {
            view.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, view, pointer, Window, WindowPoint(view, local), 2,
                new PointerPointProperties(Buttons(button).Raw, PointerUpdateKind.Other), modifiers));
            Settle();
        }

        public void Release(ElevationView view, Pointer pointer, Point local, KeyModifiers modifiers = KeyModifiers.None, MouseButton button = MouseButton.Left)
        {
            view.RaiseEvent(new PointerReleasedEventArgs(view, pointer, Window, WindowPoint(view, local), 3,
                new PointerPointProperties(RawInputModifiers.None, Buttons(button).Released), modifiers, button));
            pointer.Dispose();
            Settle();
        }

        public void Click(ElevationView view, Point local)
        {
            var pointer = Press(view, local);
            Release(view, pointer, local);
        }

        public void Wheel(ElevationView view, Point local, double delta)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            view.RaiseEvent(new PointerWheelEventArgs(view, pointer, Window, WindowPoint(view, local), 4,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, new Vector(0, delta)));
            Settle();
        }

        public void Dispose()
        {
            Window.Close();
            Controller.Dispose();
        }
    }


    private static void Pump(Func<bool> condition, string what, double seconds = 30)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (deadline.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Timed out waiting for " + what);
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
    }

    private static void Await(Task task)
    {
        Pump(() => task.IsCompleted, "a task");
        task.GetAwaiter().GetResult();
    }

    private static int Distance((byte R, byte G, byte B) a, (byte R, byte G, byte B) b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static double Contrast((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
    {
        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        static double Luminance((byte R, byte G, byte B) c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{label}: expected {expected}, actual {actual}");
    }

    private static void SameCamera(ViewCamera expected, ViewCamera actual, string label)
    {
        double scale = Math.Max(1e-3, expected.Distance);
        if (Math.Abs(expected.Distance - actual.Distance) > scale * 1e-12 ||
            Math.Abs(expected.Target.X - actual.Target.X) > scale * 1e-12 || Math.Abs(expected.Target.Y - actual.Target.Y) > scale * 1e-12 ||
            Math.Abs(expected.Target.Z - actual.Target.Z) > scale * 1e-12 || expected.AzimuthDegrees != actual.AzimuthDegrees ||
            expected.ElevationDegrees != actual.ElevationDegrees || expected.Projection != actual.Projection)
            throw new Exception($"{label}: expected {expected}, actual {actual}");
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!(Math.Abs(expected - actual) <= tolerance)) throw new Exception($"{label}: expected {expected}, actual {actual}");
    }
}
