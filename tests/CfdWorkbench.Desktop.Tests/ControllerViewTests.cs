using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// VW1 (M1.2b2 §12.4): the controller's surface channel, layouts, telemetry, and the rendered 3D, Side and Front views.
/// Async ordering is driven by gates (<see cref="GatedSurfaces"/>) and a manual clock (<see cref="ManualTime"/>), never by
/// timing. Rendered checks read pixels of the realized window at projected model points (UI-RENDERED-STATE).
/// </summary>
public static class ControllerViewTests
{
    public static void Run()
    {
        ControllerChecks();
        TelemetryChecks();
        RenderedChecks();
    }

    private static void ControllerChecks()
    {
        DesktopChecks.Check("Controller_SurfaceAsync_StaleTicketDropped", () =>
        {
            ShellEvents.Clear();
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute);
            Open(controller);
            controller.SurfaceWanted = true;
            Equal(1, gate.Calls.Count, "the first request runs");
            controller.RefreshSurface();
            controller.RefreshSurface();
            Equal(1, gate.Calls.Count, "single flight: one runs and one waits");
            if (!gate.Calls[0].Token.IsCancellationRequested) throw new Exception("A newer request did not cancel the running one");
            gate.Release(0, "first");
            Pump(() => gate.Calls.Count == 2, "the newest waiting request starts");
            if (controller.Surface is not null) throw new Exception("A stale mesh was shown");
            gate.Release(1, "newest");
            Await(controller.WhenSurfaceSettledAsync());
            Equal("newest", controller.Surface?.SourceHash, "the newest ticket's mesh");
            Equal("stale-dropped,stale-dropped,ok", Outcomes(), "replaced waiting request, dropped running result, shown newest");
        });

        DesktopChecks.Check("Controller_SurfaceRequest_NeverStalesCommit", () =>
        {
            using var controller = new WorkbenchController();
            Open(controller);
            controller.SurfaceWanted = true;
            Await(controller.WhenSurfaceSettledAsync());
            var point = controller.Planform!.Trailing.Points[4];
            if (!controller.BeginGesture(new PointRef("trailing", point.Id), GestureInput.Keyboard))
                throw new Exception("Keyboard gesture did not begin");
            controller.Nudge(0, 1, NudgeModifier.Shift);
            var commit = controller.EndGestureAsync(GestureEnd.KeyUp);
            if (commit.IsCompleted) throw new Exception("The commit finished before a mesh request could race it");
            controller.RefreshSurface();
            controller.RefreshSurface();
            Await(commit);
            if (commit.Result is not GestureOutcome.Committed)
                throw new Exception("A mesh request made the commit stale: " + commit.Result);
            Await(controller.WhenSurfaceSettledAsync());
            Equal(controller.Inspection!.Authored.Binding.SourceHash, controller.Surface?.SourceHash, "mesh of the committed revision");
        });

        DesktopChecks.Check("Controller_DuringChannelDrag_SurfaceFromDraftGeneration", () =>
        {
            using var controller = new WorkbenchController();
            Open(controller);
            controller.SurfaceWanted = true;
            Await(controller.WhenSurfaceSettledAsync());
            double acceptedTop = controller.Surface!.MaximumZ;
            var point = controller.CurveFor("dihedral")!.Points.Single(item => item.Id == "cv-3");
            if (!controller.BeginGesture(new PointRef("dihedral", "cv-3"), GestureInput.Pointer))
                throw new Exception("Dihedral gesture did not begin");
            controller.UpdateGesture(point.SpanMeters, point.Ordinate + 0.02);
            controller.FlushGestureFrame();
            if (controller.Gesture != GestureState.Dragging || controller.Draft is null)
                throw new Exception("Dihedral drag did not open a draft");
            Await(controller.WhenSurfaceSettledAsync());
            Equal("preview", controller.Surface!.Basis, "basis during the drag");
            Equal(controller.Draft.Generation, controller.Surface.Generation, "draft generation");
            if (!(controller.Surface.MaximumZ > acceptedTop + 0.005))
                throw new Exception($"The mesh does not show the raised dihedral: {controller.Surface.MaximumZ} vs {acceptedTop}");
            var cancelled = controller.EndGestureAsync(GestureEnd.Escape);
            Await(cancelled);
            Await(controller.WhenSurfaceSettledAsync());
            Equal("accepted", controller.Surface!.Basis, "basis after Escape");
            Near(acceptedTop, controller.Surface.MaximumZ, 0, "accepted mesh after Escape");
        });

        DesktopChecks.Check("Controller_UndoDuringSurfaceCompute_ShowsUndoneShape", () =>
        {
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute);
            Open(controller);
            controller.SurfaceWanted = true;
            gate.Release(0);
            Await(controller.WhenSurfaceSettledAsync());
            string original = controller.Surface!.SourceHash;
            var applied = controller.ApplySpanAsync("1350");
            Await(applied);
            if (applied.Result is not CommitOutcome.Committed) throw new Exception("Span change was refused");
            Pump(() => gate.Calls.Count == 2, "the changed shape's request");
            controller.Undo();
            gate.Release(1);   // the slow pre-Undo mesh finishes after Undo
            Pump(() => gate.Calls.Count == 3, "the post-Undo request runs after the dropped one");
            Equal(original, controller.Surface?.SourceHash, "the pre-Undo mesh never replaces the shown one");
            gate.Release(2);
            Await(controller.WhenSurfaceSettledAsync());
            Equal(original, controller.Surface?.SourceHash, "undone shape");
            Equal(controller.Inspection!.Authored.Binding.SourceHash, controller.Surface?.SourceHash, "accepted after Undo");
        });

        DesktopChecks.Check("Controller_SlowSurface_UpdatingShownAfter250Ms", () =>
        {
            var time = new ManualTime();
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute, time: time);
            Open(controller);
            controller.SurfaceWanted = true;
            if (!controller.SurfaceUpdating || controller.SurfaceBehind) throw new Exception("A fresh request already shows Updating…");
            int changed = 0;
            controller.Changed += () => changed++;
            time.Advance(TimeSpan.FromMilliseconds(249));
            Drain();
            if (controller.SurfaceBehind || changed != 0) throw new Exception("Updating… before 250 ms");
            time.Advance(TimeSpan.FromMilliseconds(1));
            Pump(() => changed > 0, "the 250 ms timer notifies the views");
            if (!controller.SurfaceBehind) throw new Exception("Updating… not shown at 250 ms");
            gate.Release(0);
            Await(controller.WhenSurfaceSettledAsync());
            if (controller.SurfaceBehind || controller.SurfaceUpdating) throw new Exception("Updating… outlived the mesh");
        });

        DesktopChecks.Check("Controller_SurfaceComputeFails_KeepsLastMeshNoteErrorEvent", () =>
        {
            ShellEvents.Clear();
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute);
            Open(controller);
            controller.SurfaceWanted = true;
            gate.Release(0);
            Await(controller.WhenSurfaceSettledAsync());
            var kept = controller.Surface ?? throw new Exception("No first mesh");
            controller.RefreshSurface();
            gate.Fail(1, new ContractError("DSL-PATCH"));
            Await(controller.WhenSurfaceSettledAsync());
            if (!ReferenceEquals(kept, controller.Surface)) throw new Exception("A failed mesh replaced the last good one");
            Equal(WorkbenchController.SurfaceKeptNote, controller.SurfaceNote, "note");
            if (controller.SurfaceUpdating) throw new Exception("A failed request still reads as updating");
            controller.RefreshSurface();
            gate.Fail(2, new InvalidOperationException("projection bug"));
            Await(controller.WhenSurfaceSettledAsync());
            if (!ReferenceEquals(kept, controller.Surface)) throw new Exception("A projection bug replaced the last good mesh");
            var errors = ShellEvents.Read().Where(item => item.Name == "view.surface" && item.Outcome == "error").Select(item => item.Code);
            Equal("DSL-PATCH,InvalidOperationException", string.Join(",", errors), "error codes");
            controller.RefreshSurface();
            gate.Release(3);
            Await(controller.WhenSurfaceSettledAsync());
            if (controller.SurfaceNote is not null) throw new Exception("The note outlived a good mesh");
        });

        DesktopChecks.Check("Controller_ChannelGesture_SameTransitionTableAsRails", () =>
        {
            using var controller = new WorkbenchController();
            Open(controller);
            string? railTrace = null;
            foreach (string curve in Channels.Names)
            {
                var reference = new PointRef(curve, "cv-3");
                var origin = controller.CurveFor(curve)!.Points.Single(item => item.Id == "cv-3");
                var trace = new List<string>
                {
                    "begin-pointer " + controller.BeginGesture(reference, GestureInput.Pointer) + " " + controller.Gesture
                };
                controller.UpdateGesture(origin.SpanMeters, origin.Ordinate + 1e-4);
                controller.FlushGestureFrame();
                trace.Add("below-threshold " + controller.Gesture);
                controller.UpdateGesture(origin.SpanMeters, origin.Ordinate + 0.004);
                controller.FlushGestureFrame();
                trace.Add("drag " + controller.Gesture + " draft=" + (controller.Draft is not null));
                var escape = controller.EndGestureAsync(GestureEnd.Escape);
                Await(escape);
                trace.Add("escape " + escape.Result.GetType().Name + " " + controller.Gesture + " draft=" + (controller.Draft is not null));
                trace.Add("begin-keyboard " + controller.BeginGesture(reference, GestureInput.Keyboard) + " " + controller.Gesture);
                controller.Nudge(0, 1, NudgeModifier.Plain);
                trace.Add("nudge " + controller.Gesture + " draft=" + (controller.Draft is not null));
                var release = controller.EndGestureAsync(GestureEnd.Release);
                trace.Add("release " + release.Result.GetType().Name + " " + controller.Gesture);
                var keyUp = controller.EndGestureAsync(GestureEnd.KeyUp);
                trace.Add("key-up " + controller.Gesture);
                Await(keyUp);
                trace.Add("done " + keyUp.Result.GetType().Name + " " + controller.Gesture);
                var moved = controller.CurveFor(curve)!.Points.Single(item => item.Id == "cv-3");
                Near(origin.Ordinate + Channels.Unit(curve).NudgePlain, moved.Ordinate, Channels.Unit(curve).Quantum / 2,
                    curve + " moved by its own plain nudge");
                controller.Undo();
                Near(origin.Ordinate, controller.CurveFor(curve)!.Points.Single(item => item.Id == "cv-3").Ordinate, 0, curve + " undo");
                string joined = string.Join(" | ", trace);
                if (railTrace is null) railTrace = joined;
                else Equal(railTrace, joined, curve + " transitions");
            }
        });

        DesktopChecks.Check("Controller_GestureEnd_CurveFamilyAndThreeDVisible", () =>
        {
            ShellEvents.Clear();
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute);
            Open(controller);
            controller.SurfaceWanted = true;
            foreach (var (curve, layout, family, threeD) in new (string, ViewLayout, string, bool)[]
            {
                ("twist", ViewLayout.Plan3d, "twist", true), ("leading", ViewLayout.One(SingleView.Plan), "rail", false),
                ("thickness", ViewLayout.Four, "thickness", true), ("dihedral", ViewLayout.One(SingleView.ThreeD), "dihedral", true),
                ("trailing", ViewLayout.One(SingleView.Front), "rail", false)
            })
            {
                controller.Layout = layout;
                if (!controller.BeginGesture(new PointRef(curve, "cv-3"), GestureInput.Pointer)) throw new Exception(curve + " did not begin");
                Await(controller.EndGestureAsync(GestureEnd.Escape));
                var end = ShellEvents.Read().Last(item => item.Name == "gesture.end");
                Equal(family, end.CurveFamily, curve + " family");
                Equal(threeD, end.ThreeDVisible, curve + " 3D visible in " + layout);
            }
            controller.Layout = ViewLayout.Plan3d;
            controller.SurfaceWanted = false;
            controller.BeginGesture(new PointRef("twist", "cv-3"), GestureInput.Pointer);
            Await(controller.EndGestureAsync(GestureEnd.Escape));
            Equal(false, ShellEvents.Read().Last(item => item.Name == "gesture.end").ThreeDVisible, "no model views on screen");
        });

        DesktopChecks.Check("Controller_Layout_SessionValueNotPersisted", () =>
        {
            using var controller = new WorkbenchController();
            Open(controller);
            bool dirty = controller.IsDirty;
            controller.Layout = ViewLayout.Four;
            controller.SetDisplay(SingleView.ThreeD, DisplayMode.Wireframe);
            controller.TargetView = SingleView.Side;
            controller.Camera3d = ViewCamera.Named(NamedCamera.Front, new Point3(0, -1, -1), new Point3(1, 1, 1), new Size(400, 300));
            if (controller.IsDirty != dirty) throw new Exception("A view change dirtied the document");
            using var fresh = new WorkbenchController();
            if (fresh.Layout != ViewLayout.Plan3d || fresh.DisplayFor(SingleView.ThreeD) != DisplayMode.Shaded ||
                fresh.Camera3d is not null || fresh.TargetView != SingleView.Plan)
                throw new Exception("A new session inherited view state");
            // A new document keeps the session's layout and display modes and refits its cameras.
            Open(controller);
            if (controller.Layout != ViewLayout.Four || controller.DisplayFor(SingleView.ThreeD) != DisplayMode.Wireframe ||
                controller.Camera3d is not null)
                throw new Exception("Opening a document reset the layout or kept the old camera");
        });
    }

    private static void TelemetryChecks()
    {
        DesktopChecks.Check("ViewEvents_SurfaceCompute_EmittedWithDuration", () =>
        {
            ShellEvents.Clear();
            using var controller = new WorkbenchController();
            Open(controller);
            controller.SurfaceWanted = true;
            Await(controller.WhenSurfaceSettledAsync());
            var surface = ShellEvents.Read().Single(item => item.Name == "view.surface");
            Equal("ok", surface.Outcome, "outcome");
            if (!(surface.DurationMilliseconds > 0)) throw new Exception("No duration: " + surface.DurationMilliseconds);
            Equal(41, surface.Stations, "stations");
            Equal(101, surface.ChordSamples, "chord samples");
            Equal("accepted", surface.Basis, "basis");
            if (surface.TraceId.Length != 32 || surface.Code is not null) throw new Exception("Trace id or code: " + surface);
            Console.WriteLine(FormattableString.Invariant($"MEASURE view_surface_41x101_fast_ring_ms={surface.DurationMilliseconds:F1}"));
        });

        DesktopChecks.Check("ViewEvents_SurfaceOutcome_StaleDroppedAndError", () =>
        {
            ShellEvents.Clear();
            var gate = new GatedSurfaces();
            using var controller = new WorkbenchController(surfaceCompute: gate.Compute);
            Open(controller);
            controller.SurfaceWanted = true;
            controller.RefreshSurface();
            controller.RefreshSurface();
            gate.Release(0);
            Pump(() => gate.Calls.Count == 2, "the newest request starts");
            gate.Fail(1, new ContractError("DSL-PATCH"));
            Await(controller.WhenSurfaceSettledAsync());
            var events = ShellEvents.Read().Where(item => item.Name == "view.surface").ToArray();
            Equal("stale-dropped,stale-dropped,error", string.Join(",", events.Select(item => item.Outcome)), "outcomes");
            Equal(0d, events[0].DurationMilliseconds, "a replaced request never ran");
            Equal("DSL-PATCH", events[2].Code, "error code");
            if (events.Any(item => item.Basis != "accepted")) throw new Exception("Basis missing on a surface event");
        });
    }

    private static void RenderedChecks()
    {
        DesktopChecks.Check("SurfaceRenderer_Silhouette_RenderedFoilStrokeThreeToOne", () =>
        {
            using var fixture = new AreaFixture();
            fixture.Shoot();
            var renderer = fixture.Area.ThreeDRenderer;
            var surface = fixture.Controller.Surface!;
            var foil = fixture.Brush("PlanFoilBrush");
            foreach (int row in new[] { 10, 20, 30 })
            {
                var edge = fixture.NearestTo(renderer, surface.Sections[row].Upper[0], foil, 1);
                var face = fixture.RgbAt(renderer, UpperFace(surface, row, 30));
                if (Distance(edge, foil) > 30) throw new Exception($"Leading edge at row {row} is {edge}, not foil {foil}");
                if (Contrast(edge, face) < 3) throw new Exception($"Leading edge {edge} on face {face}: {Contrast(edge, face):F2}:1");
            }
        });

        DesktopChecks.Check("SurfaceRenderer_Wireframe_InteriorViewportColourTenThinRows", () =>
        {
            using var fixture = new AreaFixture();
            fixture.Controller.SetDisplay(SingleView.ThreeD, DisplayMode.Wireframe);
            fixture.SetCamera3d(NamedCamera.Top);
            fixture.Shoot();
            var surface = fixture.Controller.Surface!;
            var renderer = fixture.Area.ThreeDRenderer;
            var background = fixture.Background();
            // Between two mesh rows, mid-chord, the wireframe has no fill.
            Equal(background, fixture.RgbAt(renderer, UpperFace(surface, 22, 50)), "interior pixel");
            // Scan the starboard half at mid-chord from root to tip: one thin viewport-mute row per η = i/10 that is not
            // an authored station (Example: root and tip are authored, so nine), plus the authored tip in foil-edge.
            var mute = fixture.Brush("PlanMuteBrush");
            var from = fixture.WindowPoint(renderer, surface.Sections[0].Upper[50]);
            var to = fixture.WindowPoint(renderer, surface.Sections[^1].Upper[50]);
            int rows = 0;
            bool inRow = false;
            for (int x = (int)Math.Ceiling(from.X) + 2; x <= (int)Math.Floor(to.X) - 3; x++)
            {
                bool muted = Distance(fixture.Rgb(x, (int)Math.Round(from.Y)), mute) < 60;
                if (muted && !inRow) rows++;
                inRow = muted;
            }
            Equal(9, rows, "thin intermediate rows between the authored root and tip");
        });

        DesktopChecks.Check("Workspace_NewFoil_WindowPixelsShow3dSurface", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                Await(controller.NewFoilAsync());
                host.RefreshPanes();
                Pump(() => controller.Surface is not null && !controller.SurfaceUpdating, "the new foil's first mesh");
                Settle(window);
                var renderer = host.ModelView.ThreeDRenderer;
                if (!renderer.IsEffectivelyVisible || renderer.Bounds.Width < 200 || renderer.Bounds.Height < 200)
                    throw new Exception("The 3D view is not realized in the window: " + renderer.Bounds);
                using var shot = Shot.Of(window);
                var foil = ResourceRgb(window, "PlanFoilBrush");
                var background = ResourceRgb(window, "ViewportBrush");
                var surface = controller.Surface!;
                foreach (int row in new[] { 0, surface.Sections.Count / 2, surface.Sections.Count - 1 })
                {
                    var at = renderer.TranslatePoint(renderer.Camera!.Value.Project(surface.Sections[row].Upper[0], renderer.Bounds.Size), window)
                        ?? throw new Exception("No window coordinate");
                    var nearest = shot.Nearest(at, foil, 1);
                    if (Distance(nearest, foil) > 30 || Contrast(nearest, background) < 3)
                        throw new Exception($"Row {row} leading edge at {at} shows {nearest}, not the foil outline");
                }
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("ModelArea_Plan3d_PlanTwoThirds3dOneThird", () =>
        {
            using var fixture = new AreaFixture(width: 1200, height: 700);
            var area = fixture.Area;
            if (!area.PlanSlot.IsEffectivelyVisible || !area.ThreeDSlot.IsEffectivelyVisible ||
                area.SideSlot.IsEffectivelyVisible || area.FrontSlot.IsEffectivelyVisible)
                throw new Exception("Plan + 3D shows the wrong views");
            Near(2, area.PlanSlot.Bounds.Width / area.ThreeDSlot.Bounds.Width, 0.01, "Plan : 3D width");
            Near(area.PlanSlot.Bounds.Height, area.ThreeDSlot.Bounds.Height, 0, "same height");
            if (!(area.ThreeDSlot.Bounds.X > area.PlanSlot.Bounds.X)) throw new Exception("3D is not right of the Plan");
        });

        DesktopChecks.Check("ModelArea_FourViews_PlanThreeDSideFront", () =>
        {
            using var fixture = new AreaFixture(width: 1400, height: 1000);
            fixture.Controller.Layout = ViewLayout.Four;
            fixture.Shoot();
            var area = fixture.Area;
            var slots = new[] { area.PlanSlot, area.ThreeDSlot, area.SideSlot, area.FrontSlot };
            if (slots.Any(slot => !slot.IsEffectivelyVisible)) throw new Exception("Four views hides a view");
            var (plan, threeD, side, front) = (area.PlanSlot.Bounds, area.ThreeDSlot.Bounds, area.SideSlot.Bounds, area.FrontSlot.Bounds);
            if (!(plan.X < threeD.X && plan.Y == threeD.Y && side.X == plan.X && side.Y > plan.Y && front.X == threeD.X && front.Y == side.Y))
                throw new Exception($"Quad order: plan {plan}, 3D {threeD}, side {side}, front {front}");
            foreach (var (renderer, title) in new[] { (area.SideRenderer, "Side"), (area.FrontRenderer, "Front") })
            {
                var edge = fixture.NearestTo(renderer, fixture.Controller.Surface!.Sections[^1].Upper[0], fixture.Brush("PlanFoilBrush"), 3);
                if (Contrast(edge, fixture.Background()) < 3) throw new Exception(title + " draws no foil outline");
            }
            Equal("Side · from starboard", area.SideLabel.Content?.ToString(), "Side label");
            Equal("Front · looking aft", area.FrontLabel.Content?.ToString(), "Front label");
        });

        DesktopChecks.Check("ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack", () =>
        {
            using var fixture = new AreaFixture(width: 1400, height: 1000);
            var area = fixture.Area;
            area.ThreeDLabel.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            fixture.Settle();
            if (fixture.Controller.Layout != ViewLayout.One(SingleView.ThreeD) || area.PlanSlot.IsEffectivelyVisible ||
                !area.ThreeDSlot.IsEffectivelyVisible || area.ThreeDSlot.Bounds.Width < area.PlanContent.Bounds.Width - 1)
                throw new Exception("Double-click on the 3D label did not show 3D alone");
            area.ThreeDLabel.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            fixture.Settle();
            if (fixture.Controller.Layout != ViewLayout.Plan3d || !area.PlanSlot.IsEffectivelyVisible)
                throw new Exception("A second double-click did not return to Plan + 3D");
            fixture.Controller.Layout = ViewLayout.Four;
            fixture.Settle();
            area.SideLabel.Focus();
            area.SideLabel.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Return, Source = area.SideLabel });
            fixture.Settle();
            if (fixture.Controller.Layout != ViewLayout.One(SingleView.Side) || !area.SideSlot.IsEffectivelyVisible || area.PlanSlot.IsEffectivelyVisible)
                throw new Exception("Return on the Side label did not show Side alone");
            area.SideLabel.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Return, Source = area.SideLabel });
            fixture.Settle();
            if (fixture.Controller.Layout != ViewLayout.Four) throw new Exception("Return again did not return to Four views");
            area.FrontLabel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Equal(SingleView.Front, fixture.Controller.TargetView, "a click makes the view the command target");
        });

        DesktopChecks.Check("ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView", () =>
        {
            // The app's minimum window (WindowMinWidth × WindowMinHeight, 1024 × 700) with the side panes open.
            using var fixture = new AreaFixture(width: 1024, height: 700);
            fixture.Controller.Layout = ViewLayout.Four;
            fixture.Settle();
            var area = fixture.Area;
            var grid = area.ViewArrangementGrid;
            AssertFourOrOne(area, grid.Bounds.Width >= 640 && grid.Bounds.Height >= 480, "minimum window " + grid.Bounds.Size);
            // The boundary itself: an arrangement of exactly 640 × 480 keeps Four views; one pixel less in either axis shows one.
            foreach (var (width, height, four) in new[] { (640d, 480d, true), (639d, 480d, false), (640d, 479d, false) })
            {
                area.PlanContent.Width = width;
                area.PlanContent.Height = height;
                fixture.Settle();
                Near(width, grid.Bounds.Width, 0, "arrangement width");
                Near(height, grid.Bounds.Height, 0, "arrangement height");
                AssertFourOrOne(area, four, $"{width} × {height}");
            }
            if (fixture.Controller.Layout != ViewLayout.Four) throw new Exception("The fallback changed the chosen layout");

            static void AssertFourOrOne(ModelArea area, bool four, string label)
            {
                var visible = new Control[] { area.PlanSlot, area.ThreeDSlot, area.SideSlot, area.FrontSlot }.Where(slot => slot.IsEffectivelyVisible).ToArray();
                if (four && (visible.Length != 4 || visible.Any(slot => slot.Bounds.Width < 320 || slot.Bounds.Height < 240)))
                    throw new Exception($"{label}: Four views with a view under 320 × 240: {string.Join(", ", visible.Select(slot => slot.Bounds.Size))}");
                if (!four && visible.Length != 1) throw new Exception($"{label}: {visible.Length} views below the Four-view minimum");
            }
        });

        DesktopChecks.Check("ModelArea_LayoutRoundTripAndTabReentry_AllViewsRenderPixels", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1600, Height = 1000 };
            try
            {
                window.Show();
                Settle(window);
                Await(controller.OpenExampleAsync());
                host.RefreshPanes();
                controller.Layout = ViewLayout.Four;
                Pump(() => controller.Surface is not null && !controller.SurfaceUpdating, "first mesh");
                Settle(window);
                AssertViewsDrawn(window, host.ModelView, controller, "Four views");
                foreach (var layout in new[] { ViewLayout.One(SingleView.Plan), ViewLayout.Plan3d, ViewLayout.One(SingleView.Front), ViewLayout.Four })
                {
                    controller.Layout = layout;
                    Settle(window);
                }
                AssertViewsDrawn(window, host.ModelView, controller, "after the layout round trip");
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.FoilSourceDocument;
                Settle(window);
                host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.ModelDocument;
                Settle(window);
                AssertViewsDrawn(window, host.ModelView, controller, "after tab re-entry");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("ModelArea_NotCertifiedFoil_ViewsDimmedWithCaption", () =>
        {
            using var fixture = new AreaFixture();
            fixture.Shoot();
            var renderer = fixture.Area.ThreeDRenderer;
            var surface = fixture.Controller.Surface!;
            var foil = fixture.Brush("PlanFoilBrush");
            var before = fixture.NearestTo(renderer, surface.Sections[20].Upper[0], foil, 1);
            var parse = FoilSource.Parse(System.Text.Encoding.UTF8.GetBytes(fixture.Controller.AcceptedSource));
            var uncertified = CfdWorkbench.Core.Geometry.Assess(parse, TimeSpan.Zero);
            if (uncertified.Status == GeometryStatus.Certified) throw new Exception("Fixture did not force an uncertified assessment");
            typeof(WorkbenchController).GetProperty("Inspection")!.SetValue(fixture.Controller,
                new AcceptedInspection(fixture.Controller.Inspection!.Authored, uncertified));
            fixture.Controller.Select(new Selection.Foil());
            fixture.Controller.Layout = ViewLayout.Four;
            fixture.Window.Width = 1400;
            fixture.Window.Height = 1000;
            fixture.Shoot();
            foreach (var label in new[] { fixture.Area.ThreeDLabel, fixture.Area.SideLabel, fixture.Area.FrontLabel, fixture.Area.PlanLabel })
                if (label.Content?.ToString()?.EndsWith(" · not checked", StringComparison.Ordinal) != true)
                    throw new Exception("Caption without \"· not checked\": " + label.Content);
            var after = fixture.NearestTo(renderer, surface.Sections[20].Upper[0], foil, 1);
            if (!(Contrast(after, fixture.Background()) < Contrast(before, fixture.Background()) - 1) || after == fixture.Background())
                throw new Exception($"Outline not dimmed: before {before}, after {after}");
        });

        DesktopChecks.Check("ModelArea_FirstMesh_DrawingStateThenSurface", () =>
        {
            var gate = new GatedSurfaces();
            using var fixture = new AreaFixture(gate: gate);
            var area = fixture.Area;
            if (!area.ThreeDDrawing.IsEffectivelyVisible || area.ThreeDDrawing.Text != "Drawing…")
                throw new Exception("No Drawing… state before the first mesh");
            var renderer = area.ThreeDRenderer;
            fixture.Shoot();
            var centre = renderer.TranslatePoint(new Point(renderer.Bounds.Width / 2, renderer.Bounds.Height * 0.8), fixture.Window)!.Value;
            Equal(fixture.Background(), fixture.Rgb((int)centre.X, (int)centre.Y), "no surface before the first mesh");
            gate.Release(0);
            Await(fixture.Controller.WhenSurfaceSettledAsync());
            fixture.Shoot();
            if (area.ThreeDDrawing.IsEffectivelyVisible) throw new Exception("Drawing… outlived the first mesh");
            var edge = fixture.NearestTo(renderer, fixture.Controller.Surface!.Sections[20].Upper[0], fixture.Brush("PlanFoilBrush"), 1);
            if (Contrast(edge, fixture.Background()) < 3) throw new Exception("The first mesh did not draw");
        });

        DesktopChecks.Check("View_RenderThrows_CopyTryAgainAndEvent", () =>
        {
            using var fixture = new AreaFixture();
            ShellEvents.Clear();
            var area = fixture.Area;
            area.ThreeDRenderer.RenderGuard = () => throw new InvalidOperationException("render probe");
            area.ThreeDRenderer.InvalidateVisual();
            fixture.Shoot();
            fixture.Settle();
            if (!area.ThreeDRenderErrorBand.IsEffectivelyVisible || area.ThreeDRenderErrorText.Text != "3D view couldn't be drawn. Your foil hasn't changed." ||
                area.ThreeDRenderTryAgainButton.Content?.ToString() != "Try again")
                throw new Exception("Render failure shows no copy or Try again");
            if (!ShellEvents.Read().Any(item => item.Name == "shell.pane.render" && item.Outcome == "error" && item.Pane == "3d" &&
                    item.ExceptionType == nameof(InvalidOperationException)))
                throw new Exception("No shell.pane.render event for the 3D view");
            area.ThreeDRenderer.RenderGuard = null;
            area.ThreeDRenderTryAgainButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            fixture.Shoot();
            if (area.ThreeDRenderErrorBand.IsEffectivelyVisible) throw new Exception("Try again did not restore the 3D view");
        });
    }

    internal static void RunReadiness()
    {
        // Wall-clock measurements (TEST-RING): the mesh on its own and a channel drag with the 3D view on screen.
        DesktopChecks.Check("Readiness_ViewSurface41x101_Measured", () =>
        {
            ShellEvents.Clear();
            using var controller = new WorkbenchController();
            Open(controller);
            controller.SurfaceWanted = true;
            var durations = new List<double>();
            for (int run = 0; run < 12; run++)
            {
                controller.RefreshSurface();
                Await(controller.WhenSurfaceSettledAsync());
            }
            durations.AddRange(ShellEvents.Read().Where(item => item.Name == "view.surface" && item.Outcome == "ok").Skip(2)
                .Select(item => item.DurationMilliseconds));
            durations.Sort();
            Console.WriteLine(FormattableString.Invariant(
                $"MEASURE view_surface_41x101_ms median={durations[durations.Count / 2]:F1} max={durations[^1]:F1} n={durations.Count}"));
        });
        DesktopChecks.Check("Readiness_ThreeDFrame_Measured", () =>
        {
            // One 3D frame at 1440 × 900 (mesh to screen, painter's sort, one Skia draw), as orbit will redraw it.
            using var fixture = new AreaFixture(width: 1440, height: 900);
            var renderer = fixture.Area.ThreeDRenderer;
            var size = new PixelSize((int)renderer.Bounds.Width, (int)renderer.Bounds.Height);
            var times = new List<double>();
            for (int frame = 0; frame < 12; frame++)
            {
                fixture.Controller.Camera3d = fixture.Controller.Camera3d!.Value.Orbit(5, 0);
                fixture.Settle();
                using var bitmap = new RenderTargetBitmap(size);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                bitmap.Render(renderer);
                times.Add(watch.Elapsed.TotalMilliseconds);
            }
            times.Sort();
            Console.WriteLine(FormattableString.Invariant(
                $"MEASURE three_d_frame_ms median={times[times.Count / 2]:F1} max={times[^1]:F1} size={size.Width}x{size.Height}"));
        });
        DesktopChecks.Check("Readiness_ChannelDragWith3dVisible_GestureEndP95", () =>
        {
            ShellEvents.Clear();
            using var controller = new WorkbenchController();
            Open(controller);
            controller.SurfaceWanted = true;
            Await(controller.WhenSurfaceSettledAsync());
            var point = controller.CurveFor("dihedral")!.Points.Single(item => item.Id == "cv-3");
            controller.BeginGesture(new PointRef("dihedral", "cv-3"), GestureInput.Pointer);
            for (int frame = 1; frame <= 60; frame++)
            {
                controller.UpdateGesture(point.SpanMeters, point.Ordinate + frame * 0.0005);
                controller.FlushGestureFrame();
                Drain();
            }
            var end = controller.EndGestureAsync(GestureEnd.Release);
            Await(end);
            var gesture = ShellEvents.Read().Last(item => item.Name == "gesture.end");
            int meshes = ShellEvents.Read().Count(item => item.Name == "view.surface");
            Console.WriteLine(FormattableString.Invariant(
                $"MEASURE gesture_end_3d_visible update_p95_ms={gesture.UpdateP95Ms:F1} estimates_p95_ms={gesture.EstimatesP95Ms:F1} frames={gesture.Frames} three_d_visible={gesture.ThreeDVisible} outcome={gesture.Outcome} surface_events={meshes}"));
            Equal(true, gesture.ThreeDVisible, "3D visible during the drag");
        });
    }

    // ---- fixtures -------------------------------------------------------------------------------------------------

    /// <summary>The shell in a realized window, so the model area is hosted exactly as the app hosts it.</summary>
    private sealed class AreaFixture : IDisposable
    {
        private Shot? shot;
        public WorkbenchController Controller { get; }
        public ShellHost Host { get; }
        public ModelArea Area { get; }
        public Window Window { get; }

        public AreaFixture(int width = 1200, int height = 700, GatedSurfaces? gate = null, Avalonia.Styling.ThemeVariant? theme = null)
        {
            Controller = new WorkbenchController(surfaceCompute: gate is null ? null : gate.Compute);
            Open(Controller);
            Host = new ShellHost(Controller);
            Area = Host.ModelView;
            Window = new Window { Content = Host, Width = width, Height = height };
            if (theme is not null) Window.RequestedThemeVariant = theme;
            Window.Show();
            Host.RefreshPanes();
            Settle();
            if (gate is null) Pump(() => Controller.Surface is not null && !Controller.SurfaceUpdating, "first mesh");
            Settle();
        }

        public void Settle()
        {
            ControllerViewTests.Settle(Window);
            shot?.Dispose();
            shot = null;
        }

        public void SetCamera3d(NamedCamera name)
        {
            var surface = Controller.Surface!;
            Controller.Camera3d = ViewCamera.Named(name, new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ),
                new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ), Area.ThreeDRenderer.Bounds.Size);
            Settle();
        }

        public void Shoot()
        {
            Settle();
            shot = Shot.Of(Window);
        }

        private Shot Current => shot ?? throw new InvalidOperationException("Shoot() first");
        public (byte R, byte G, byte B) Rgb(int x, int y) => Current.Rgb(x, y);
        public Point WindowPoint(SurfaceRenderer renderer, Point3 point) =>
            renderer.TranslatePoint(renderer.Camera!.Value.Project(point, renderer.Bounds.Size), Window) ?? throw new Exception("No window point");
        public (byte R, byte G, byte B) RgbAt(SurfaceRenderer renderer, Point3 point)
        {
            var at = WindowPoint(renderer, point);
            return Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y));
        }
        public (byte R, byte G, byte B) NearestTo(SurfaceRenderer renderer, Point3 point, (byte R, byte G, byte B) target, int radius) =>
            Current.Nearest(WindowPoint(renderer, point), target, radius);
        public Color ResourceColor(string key) =>
            Window.TryFindResource(key, Window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
                ? brush.Color : throw new Exception("Theme resource " + key + " is missing (Styles.axaml)");
        public (byte R, byte G, byte B) Brush(string key) { var color = ResourceColor(key); return (color.R, color.G, color.B); }
        public (byte R, byte G, byte B) Background() => Brush("ViewportBrush");

        public void Dispose()
        {
            shot?.Dispose();
            Window.Close();
            Controller.Dispose();
        }
    }

    /// <summary>One RenderTargetBitmap of the whole realized window, read as unpremultiplied BGRA.</summary>
    private sealed class Shot : IDisposable
    {
        private readonly WriteableBitmap pixels;
        private readonly ILockedFramebuffer frame;

        private Shot(Window window)
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
            bitmap.Render(window);
            pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
            frame = pixels.Lock();
            bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
        }

        public static Shot Of(Window window) => new(window);

        public (byte R, byte G, byte B) Rgb(int x, int y)
        {
            if (x < 0 || y < 0 || x >= frame.Size.Width || y >= frame.Size.Height) throw new Exception($"Pixel {x},{y} is outside the window");
            int offset = y * frame.RowBytes + x * 4;
            return (Marshal.ReadByte(frame.Address, offset + 2), Marshal.ReadByte(frame.Address, offset + 1), Marshal.ReadByte(frame.Address, offset));
        }

        public (byte R, byte G, byte B) Nearest(Point at, (byte R, byte G, byte B) target, int radius)
        {
            var best = Rgb((int)Math.Round(at.X), (int)Math.Round(at.Y));
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var candidate = Rgb((int)Math.Round(at.X) + dx, (int)Math.Round(at.Y) + dy);
                    if (Distance(candidate, target) < Distance(best, target)) best = candidate;
                }
            return best;
        }

        public void Dispose()
        {
            frame.Dispose();
            pixels.Dispose();
        }
    }

    /// <summary>A surface compute the test releases or fails by hand, in request order.</summary>
    private sealed class GatedSurfaces
    {
        public List<(byte[] Bytes, string Basis, long Generation, CancellationToken Token, TaskCompletionSource<SurfaceView> Gate)> Calls { get; } = [];

        public Task<SurfaceView> Compute(byte[] source, string basis, long generation, CancellationToken cancellation)
        {
            var gate = new TaskCompletionSource<SurfaceView>(TaskCreationOptions.RunContinuationsAsynchronously);
            Calls.Add((source, basis, generation, cancellation, gate));
            return gate.Task;
        }

        public void Release(int call, string? marker = null)
        {
            var (bytes, basis, generation, _, gate) = Calls[call];
            var view = Placement.Surface(bytes, basis, generation, CancellationToken.None);
            gate.SetResult(marker is null ? view : view with { SourceHash = marker });
        }

        public void Fail(int call, Exception error) => Calls[call].Gate.SetException(error);
    }

    /// <summary>A clock the test advances; timers fire synchronously inside <see cref="Advance"/>.</summary>
    private sealed class ManualTime : TimeProvider
    {
        private long ticks;
        private readonly List<ManualTimer> timers = [];
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, ticks + dueTime.Ticks);
            timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            ticks += by.Ticks;
            foreach (var timer in timers.Where(timer => !timer.Disposed && timer.Due <= ticks).ToArray())
            {
                timer.Disposed = true;
                timer.Callback(timer.State);
            }
        }

        private sealed class ManualTimer(TimerCallback callback, object? state, long due) : ITimer
        {
            public TimerCallback Callback { get; } = callback;
            public object? State { get; } = state;
            public long Due { get; } = due;
            public bool Disposed { get; set; }
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() => Disposed = true;
            public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        }
    }

    // ---- helpers --------------------------------------------------------------------------------------------------

    private static void AssertViewsDrawn(Window window, ModelArea area, WorkbenchController controller, string label)
    {
        using var shot = Shot.Of(window);
        var foil = ResourceRgb(window, "PlanFoilBrush");
        var background = ResourceRgb(window, "ViewportBrush");
        var surface = controller.Surface!;
        foreach (var renderer in new[] { area.ThreeDRenderer, area.SideRenderer, area.FrontRenderer })
        {
            if (!renderer.IsEffectivelyVisible || renderer.Camera is null) throw new Exception($"{label}: {renderer.Pane} is not shown");
            var at = renderer.TranslatePoint(renderer.Camera.Value.Project(surface.Sections[^1].Upper[0], renderer.Bounds.Size), window)
                ?? throw new Exception("No window point");
            var nearest = shot.Nearest(at, foil, 3);
            if (Contrast(nearest, background) < 3) throw new Exception($"{label}: {renderer.Pane} shows no foil pixels near {at}");
        }
    }

    /// <summary>A point on the starboard upper surface at mesh row and chord sample, nudged off the stroke rows.</summary>
    private static Point3 UpperFace(SurfaceView surface, int row, int sample)
    {
        var a = surface.Sections[row].Upper[sample];
        var b = surface.Sections[row + 1].Upper[sample + 1];
        return new Point3((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
    }

    private static void Open(WorkbenchController controller) => Await(controller.OpenExampleAsync());

    private static string Outcomes() =>
        string.Join(",", ShellEvents.Read().Where(item => item.Name == "view.surface").Select(item => item.Outcome));

    private static (byte R, byte G, byte B) ResourceRgb(Window window, string key) =>
        window.TryFindResource(key, window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
            ? (brush.Color.R, brush.Color.G, brush.Color.B) : throw new Exception("Theme resource " + key + " is missing");

    internal static void Settle(Window window)
    {
        for (int i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Drain() => Dispatcher.UIThread.RunJobs();

    /// <summary>Runs the UI dispatcher until the condition holds; background completions post their results to it.</summary>
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

    private static void Near(double expected, double actual, double tolerance, string label) =>
        ViewCameraTests.Near(expected, actual, tolerance, label);
}

/// <summary>The model area's named parts, found by name as the other Desktop suites find theirs.</summary>
internal static class ModelAreaParts
{
    extension(ModelArea area)
    {
        public PlanCanvas PlanCanvas => area.Part<PlanCanvas>("PlanCanvas");
        public Grid ViewArrangementGrid => area.Part<Grid>("ViewArrangementGrid");
        public Border PlanContent => area.Part<Border>("PlanContent");
        public Grid PlanSlot => area.Part<Grid>("PlanSlot");
        public Grid ThreeDSlot => area.Part<Grid>("ThreeDSlot");
        public Grid SideSlot => area.Part<Grid>("SideSlot");
        public Grid FrontSlot => area.Part<Grid>("FrontSlot");
        public Button PlanLabel => area.Part<Button>("PlanLabel");
        public Button ThreeDLabel => area.Part<Button>("ThreeDLabel");
        public Button SideLabel => area.Part<Button>("SideLabel");
        public Button FrontLabel => area.Part<Button>("FrontLabel");
        public SurfaceRenderer ThreeDRenderer => area.Part<SurfaceRenderer>("ThreeDRenderer");
        public SurfaceRenderer SideRenderer => area.Part<SurfaceRenderer>("SideRenderer");
        public SurfaceRenderer FrontRenderer => area.Part<SurfaceRenderer>("FrontRenderer");
        public TextBlock ThreeDDrawing => area.Part<TextBlock>("ThreeDDrawing");
        public Border ThreeDRenderErrorBand => area.Part<Border>("ThreeDRenderErrorBand");
        public TextBlock ThreeDRenderErrorText => area.Part<TextBlock>("ThreeDRenderErrorText");
        public Button ThreeDRenderTryAgainButton => area.Part<Button>("ThreeDRenderTryAgainButton");
    }

    private static T Part<T>(this ModelArea area, string name) where T : Control =>
        area.FindControl<T>(name) ?? throw new InvalidOperationException("ModelArea has no " + name);
}
