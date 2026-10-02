using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

public static class PlanCanvasTests
{
    public static void Run()
    {
        DesktopChecks.Check("PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                if (!ReferenceEquals(canvas.GetVisualRoot(), window) || !canvas.IsEffectivelyVisible ||
                    canvas.Bounds.Width < 320 || canvas.Bounds.Height < 240)
                    throw new Exception("Plan canvas is not realized at the required size");
                var point = canvas.ScreenPoint(controller.Planform!.Trailing.Points[4]);
                var inWindow = canvas.TranslatePoint(point, window)
                    ?? throw new Exception("Point has no window coordinate");
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
                bitmap.Render(window);
                using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888,
                    AlphaFormat.Unpremul);
                using var frame = pixels.Lock();
                bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
                int x = (int)Math.Round(inWindow.X);
                int y = (int)Math.Round(inWindow.Y);
                int offset = y * frame.RowBytes + x * 4;
                byte blue = Marshal.ReadByte(frame.Address, offset);
                byte green = Marshal.ReadByte(frame.Address, offset + 1);
                byte red = Marshal.ReadByte(frame.Address, offset + 2);
                int backdropOffset = (y + 20) * frame.RowBytes + x * 4;
                int difference = Math.Abs(red - Marshal.ReadByte(frame.Address, backdropOffset + 2)) +
                    Math.Abs(green - Marshal.ReadByte(frame.Address, backdropOffset + 1)) +
                    Math.Abs(blue - Marshal.ReadByte(frame.Address, backdropOffset));
                if (difference < 50)
                    throw new Exception($"Point pixel is background at {x},{y}: {red},{green},{blue}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
                var peer = ControlAutomationPeer.CreatePeerForElement(canvas)
                    ?? throw new Exception("Plan peer missing");
                var point = peer.GetChildren()?.FirstOrDefault(child => child.GetName().Contains("Trailing edge, point 5"))
                    ?? throw new Exception("Point peer missing");
                if (point.GetBoundingRectangle().Width < 24 || point.GetBoundingRectangle().Height < 24)
                    throw new Exception("Point peer has no target bounds");
                // MC-6 (§10.9): a point's spanwise coordinate is "from root"; "span" means only the wing span b.
                if (!point.GetName().Contains(", from root ", StringComparison.Ordinal) || point.GetName().Contains(", span ", StringComparison.Ordinal))
                    throw new Exception("Point peer name: " + point.GetName());
                if (point is not IInvokeProvider invoke)
                    throw new Exception("Point peer cannot invoke the value request");
                invoke.Invoke();
                if (canvas.LastValueRequest?.Contains("trailing", StringComparison.Ordinal) != true)
                    throw new Exception("Point peer Invoke had no effect");
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
                bitmap.Render(window);
                using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888,
                    AlphaFormat.Unpremul);
                using var frame = pixels.Lock();
                bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
                var local = canvas.ScreenPoint(controller.Planform!.Trailing.Points[4]);
                var translated = canvas.TranslatePoint(local, window)!.Value;
                int offset = (int)translated.Y * frame.RowBytes + (int)translated.X * 4;
                if (Marshal.ReadByte(frame.Address, offset + 1) < 90)
                    throw new Exception("Point peer's realized glyph is absent");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var plan = fixture.Controller.Planform!;
            foreach (var rail in new[] { plan.Leading, plan.Trailing })
                foreach (var point in rail.Points)
                    fixture.AssertGlyphPixel(point);
            var leading = fixture.Canvas.ScreenPoint(plan.Leading.Points[4]);
            var trailing = fixture.Canvas.ScreenPoint(plan.Trailing.Points[4]);
            var interior = fixture.RgbAtCanvas((leading.X + trailing.X) / 2, (leading.Y + trailing.Y) / 2);
            if (interior == fixture.BackgroundPixel())
                throw new Exception("The planform fill is absent from the rendered window");
        });

        DesktopChecks.Check("PlanCanvas_Orientation_SpanRightAftDown", () =>
        {
            using var fixture = new PlanFixture();
            var plan = fixture.Controller.Planform!;
            var root = plan.Leading.Points[0];
            var tip = plan.Leading.Points[^1];
            if (fixture.Canvas.ScreenPoint(tip).X <= fixture.Canvas.ScreenPoint(root).X)
                throw new Exception("Positive span did not run right");
            var leading = fixture.Canvas.ScreenPoint(plan.Leading.Points[4]);
            var trailing = fixture.Canvas.ScreenPoint(plan.Trailing.Points[4]);
            if (trailing.Y <= leading.Y) throw new Exception("Positive aft did not run down");
            fixture.AssertGlyphPixel(plan.Leading.Points[4]);
            fixture.AssertGlyphPixel(plan.Trailing.Points[4]);
        });

        DesktopChecks.Check("PlanCanvas_ExampleOpen_RendersBothRailsAndEveryPoint", () =>
        {
            using var fixture = new PlanFixture();
            var plan = fixture.Controller.Planform!;
            if (plan.Leading.Points.Count == 0 || plan.Trailing.Points.Count == 0)
                throw new Exception("Example rails have no points");
            foreach (var rail in new[] { plan.Leading, plan.Trailing })
                foreach (var point in rail.Points)
                    fixture.AssertGlyphPixel(point);
        });

        DesktopChecks.Check("PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[0];
            var before = fixture.RgbAtPoint(point);
            fixture.Controller.Select(new Selection.Points([new PointRef(point.Curve, point.Id)]));
            fixture.Settle();
            var after = fixture.RgbAtPoint(point);
            if (before == after) throw new Exception("Selected glyph interior did not change");
            fixture.AssertGlyphPixel(fixture.Controller.Planform!.Trailing.Points[1]);
        });

        DesktopChecks.Check("PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.RgbAtPoint(point);
            fixture.Controller.Select(new Selection.Points([new PointRef(point.Curve, point.Id)]));
            fixture.Settle();
            var after = fixture.RgbAtPoint(point);
            if (before == after) throw new Exception("Selected control shape has the same rendered interior");
            if (Contrast(after, fixture.BackgroundPixel()) < 3)
                throw new Exception("Selected glyph does not contrast with the viewport by 3:1");
        });

        DesktopChecks.Check("PlanCanvas_HitTest_NearestWithin14Px", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var centre = fixture.Canvas.ScreenPoint(point);
            if (fixture.Canvas.HitTestPoint(new Point(centre.X + 13, centre.Y))?.Id != point.Id ||
                fixture.Canvas.HitTestPoint(new Point(centre.X + 15, centre.Y))?.Id == point.Id)
                throw new Exception("Point hit circle is not 14 px with nearest target");
        });

        DesktopChecks.Check("PlanCanvas_HoverPoint_TooltipCopyAndRing", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point));
            fixture.Settle();
            if (fixture.Canvas.TooltipText?.Contains("Trailing edge, point 5", StringComparison.Ordinal) != true ||
                fixture.Canvas.TooltipText?.Contains(", from root ", StringComparison.Ordinal) != true ||
                fixture.Canvas.TooltipText?.Contains(", span ", StringComparison.Ordinal) == true ||
                Contrast(fixture.RgbNear(point, 10, 0), fixture.BackgroundPixel()) < 3)
                throw new Exception("Hover tooltip or rendered 10 px ring missing");
        });

        DesktopChecks.Check("PlanCanvas_HoverRail_TracingProbeReadout", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point) + new Vector(0, 30));
            // MC-6 (§10.9): "from root 206.00 mm" with η beside it; the probe never calls a position "span".
            if (fixture.Canvas.ProbeText?.Contains(" · from root ", StringComparison.Ordinal) != true ||
                fixture.Canvas.ProbeText?.Contains("· span ", StringComparison.Ordinal) == true ||
                fixture.Canvas.ProbeText?.Contains("chord", StringComparison.Ordinal) != true)
                throw new Exception("Tracing probe has no from-root and chord readout: " + fixture.Canvas.ProbeText);
        });

        DesktopChecks.Check("PlanCanvas_ShiftClickAndCommandClick_ExtendAndToggle", () =>
        {
            using var fixture = new PlanFixture();
            var points = fixture.Controller.Planform!.Trailing.Points;
            fixture.Press(points[3]);
            fixture.Press(points[4], KeyModifiers.Shift);
            if (fixture.Controller.Selection is not Selection.Points selected || selected.Items.Count != 2)
                throw new Exception("Shift selection did not extend");
            fixture.Press(points[3], KeyModifiers.Meta);
            if (fixture.Controller.Selection is not Selection.Points toggled || toggled.Items.Count != 1 ||
                toggled.Items[0].VertexId != points[4].Id)
                throw new Exception("Command selection did not toggle");
        });

        DesktopChecks.Check("PlanCanvas_SecondaryClickPoint_SelectsAndOpensPointMenu", () =>
        {
            // D-3 (docs/reviews/m12b-native.md §3.1, design §11.3 Point type row): Control-click is a secondary click on
            // macOS; it and right-click select the point and open its menu. Shift+F10 and the menu key do the same.
            using var fixture = new PlanFixture();
            var controls = fixture.Controller.Planform!.Trailing.Points.Where(p => p.Role == PointRole.Control).ToArray();
            var point = controls[0];
            var other = controls[^1];
            ContextMenu Open(string how)
            {
                if (fixture.Controller.Selection is not Selection.Points selected || selected.Items.Count != 1 ||
                    selected.Items[0].VertexId != point.Id)
                    throw new Exception(how + " did not select the point alone");
                if (fixture.Canvas.ContextMenu is not { IsOpen: true } menu) throw new Exception(how + " opened no point menu");
                var items = menu.Items.OfType<MenuItem>().ToArray();
                string rows = string.Join(" | ", items.Select(item => $"{item.Header}:{item.IsEnabled}"));
                if (rows != "Make Anchor Point:True | Make Control Point:False | Tangent:False | Fit:True")
                    throw new Exception(how + " menu rows: " + rows);
                string tangents = string.Join(" | ", items[2].Items.OfType<MenuItem>().Select(item => item.Header));
                if (tangents != "Smooth | Symmetric | Corner") throw new Exception(how + " tangent rows: " + tangents);
                return menu;
            }
            void Close(ContextMenu menu) { menu.Close(); fixture.Settle(); }
            fixture.Press(other);
            fixture.Press(point, KeyModifiers.Control);
            if (OperatingSystem.IsMacOS()) Close(Open("Control-click"));
            else if (fixture.Controller.Selection is not Selection.Points { Items.Count: 2 })
                throw new Exception("Ctrl-click no longer toggles on Windows");
            fixture.Press(other);
            fixture.DragAt(fixture.Canvas.ScreenPoint(point), default, MouseButton.Right, KeyModifiers.None);
            Close(Open("Right-click"));
            fixture.Press(other);
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.KeyDown(Key.F10, KeyModifiers.Shift);
            Close(Open("Shift+F10"));
            fixture.KeyDown(Key.Apps);
            var bound = Open("Context-menu key");
            bound.Items.OfType<MenuItem>().First().RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            for (int i = 0; i < 400 && fixture.Controller.Planform!.Trailing.Points.Single(p => p.Id == point.Id).Role != PointRole.Anchor; i++)
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }
            if (fixture.Controller.Planform!.Trailing.Points.Single(p => p.Id == point.Id).Role != PointRole.Anchor)
                throw new Exception("Make Anchor Point did not run point.make-anchor");
        });

        DesktopChecks.Check("PlanCanvas_SpaceAndShiftSpace_SelectAndToggle", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Leading.Points[3];
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.KeyDown(Key.Space);
            if (fixture.Controller.Selection is not Selection.Points selected || selected.Items[0].VertexId != point.Id)
                throw new Exception("Space did not select focused point");
            fixture.KeyDown(Key.Space, KeyModifiers.Shift);
            if (fixture.Controller.Selection is Selection.Points)
                throw new Exception("Shift+Space did not toggle focused point off");
        });

        DesktopChecks.Check("PlanCanvas_TabOrder_LeadingThenTrailingThenChips", () =>
        {
            using var fixture = new PlanFixture();
            var order = fixture.Canvas.KeyboardTargets;
            var plan = fixture.Controller.Planform!;
            var expected = plan.Leading.Points.Select(point => point.Id)
                .Concat(plan.Trailing.Points.Select(point => point.Id)).ToArray();
            if (!order.Take(expected.Length).SequenceEqual(expected) ||
                order.Count != expected.Length + plan.Stations.Count)
                throw new Exception("Plan Tab order differs from LE, TE, station chips");
        });

        DesktopChecks.Check("PlanCanvas_TabPastLastPoint_LeavesCanvas", () =>
        {
            using var fixture = new PlanFixture();
            for (int i = 0; i < fixture.Canvas.KeyboardTargets.Count; i++)
                if (!fixture.Canvas.FocusNext()) throw new Exception("Tab left before the last target");
            if (fixture.Canvas.FocusNext()) throw new Exception("Tab trapped focus after the last target");
        });

        DesktopChecks.Check("PlanCanvas_CombToggle_RenderedTeethOnSelectedRail", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.SelectPoint(new PointRef(point.Curve, point.Id), false, false);
            fixture.Settle();
            var before = fixture.FrameSnapshot();
            fixture.Canvas.ToggleComb();
            fixture.Settle();
            if (!fixture.Controller.CombVisible || before.SequenceEqual(fixture.FrameSnapshot()))
                throw new Exception("Comb toggle did not paint teeth on selected rail");
        });

        DesktopChecks.Check("PlanCanvas_FitAndDefaultView_PlanformClearOfProbeAndScaleBar", () =>
        {
            // F-3, O-1, O-3 (docs/reviews/m12b-native.md §3): the default view and Fit keep both halves, every point glyph and
            // every station chip inside the canvas and clear of the Tracing probe box and the scale bar.
            using var fixture = new PlanFixture();
            void AssertClear(string view)
            {
                var canvas = fixture.Canvas;
                double width = canvas.Bounds.Width, height = canvas.Bounds.Height;
                var inside = new Rect(0, 0, width, height);
                var probe = new Rect(Math.Max(8, width - 428), 8, 420, 48);
                var scaleBar = new Rect(8, height - 50, 160, 34);
                var plan = fixture.Controller.Planform!;
                foreach (var point in plan.Leading.Points.Concat(plan.Trailing.Points))
                foreach (double side in new[] { -1d, 1d })
                {
                    var centre = canvas.ScreenPoint(point with { SpanMeters = point.SpanMeters * side });
                    var glyph = new Rect(centre.X - 8, centre.Y - 8, 16, 16);
                    if (!inside.Contains(glyph) || glyph.Intersects(probe) || glyph.Intersects(scaleBar))
                        throw new Exception($"{view}: {point.Curve} point {point.Index + 1} (side {side}) at {centre} is clipped or obscured");
                }
                var chips = canvas.VisibleStationChips;
                if (chips.Count == 0) throw new Exception(view + ": no station chip shown");
                foreach (var chip in chips)
                    if (!inside.Contains(chip.Bounds) || chip.Bounds.Intersects(probe) || chip.Bounds.Intersects(scaleBar))
                        throw new Exception($"{view}: station chip {chip.Index} at {chip.Bounds} is clipped or obscured");
            }
            AssertClear("default view");
            fixture.Canvas.PanBy(240, -120);
            fixture.Canvas.Fit();
            fixture.Settle();
            AssertClear("Fit");
        });

        DesktopChecks.Check("PlanCanvas_ZoomPanFit_KeyboardAndPointerSameCamera", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.Canvas.ScreenPoint(point);
            fixture.Canvas.ZoomAt(1.2, before);
            fixture.Canvas.PanBy(12, 8);
            if (fixture.Canvas.ScreenPoint(point) == before)
                throw new Exception("Zoom and pan did not move the point");
            fixture.Canvas.Fit();
            if (fixture.Controller.PlanCamera != new PlanCamera())
                throw new Exception("Fit did not restore the camera");
        });

        DesktopChecks.Check("ModelArea_MinimumWindow_PlanAtLeast320x240", () =>
        {
            using var fixture = new PlanFixture(width: 1024, height: 700);
            if (!fixture.Canvas.IsEffectivelyVisible || fixture.Canvas.Bounds.Width < 320 ||
                fixture.Canvas.Bounds.Height < 240)
                throw new Exception($"Plan minimum bounds are {fixture.Canvas.Bounds}");
        });

        DesktopChecks.Check("ModelArea_SamplesTab_IsometricMovedUnchanged", () =>
        {
            using var fixture = new PlanFixture();
            if (!ReferenceEquals(fixture.Host.LayoutFactory.MainDocumentDock.ActiveDockable,
                    fixture.Host.LayoutFactory.ModelDocument))
                throw new Exception("Plan is not the initial document");
            fixture.Host.LayoutFactory.MainDocumentDock.ActiveDockable = fixture.Host.LayoutFactory.SamplesDocument;
            var viewport = fixture.Host.ModelView.FindControl<Viewport>("FoilViewport")!;
            for (int i = 0; i < 5 && (!viewport.IsEffectivelyVisible || viewport.Bounds.Width < 320 ||
                                      !ReferenceEquals(viewport.LastRecordedFrame, fixture.Controller.Frame)); i++)
                fixture.Settle();
            if (!viewport.IsEffectivelyVisible || !ReferenceEquals(viewport.LastRecordedFrame,
                    fixture.Controller.Frame) || viewport.Bounds.Width < 320)
                throw new Exception($"3D samples document lost the isometric plot: visible={viewport.IsEffectivelyVisible}, " +
                    $"bounds={viewport.Bounds}, frame={ReferenceEquals(viewport.LastRecordedFrame, fixture.Controller.Frame)}");
        });

        DesktopChecks.Check("PlanCanvas_FocusRing_RenderedPixelsAtLeastThreeToOne", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.Settle();
            if (Contrast(fixture.RgbNear(point, 13, 0), fixture.BackgroundPixel()) < 3)
                throw new Exception("Focused point has no contrasting 13 px ring");
        });

        DesktopChecks.Check("PlanCanvas_HighContrast_RenderedRingContrastPrimary", () =>
        {
            using var fixture = new PlanFixture(theme: NativeReviewThemes.HighContrast);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.Settle();
            var ring = fixture.RgbNear(point, 13, 0);
            if (ring.R < 200 || ring.G < 200 || Contrast(ring, fixture.BackgroundPixel()) < 7)
                throw new Exception($"High contrast ring is not the primary token: {ring}");
        });

        DesktopChecks.Check("PlanCanvas_AfterDockReattach_RendersSameScene", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.RgbAtPoint(point);
            fixture.Host.LayoutFactory.MainDocumentDock.ActiveDockable = fixture.Host.LayoutFactory.SamplesDocument;
            fixture.Settle();
            fixture.Host.LayoutFactory.MainDocumentDock.ActiveDockable = fixture.Host.LayoutFactory.ModelDocument;
            fixture.Settle();
            if (!fixture.Canvas.IsEffectivelyVisible || before != fixture.RgbAtPoint(point))
                throw new Exception("Plan glyph changed or disappeared after tab re-entry");
        });

        DesktopChecks.Check("PlanCanvas_Brushes_AllFromThemeResources", () =>
        {
            using var fixture = new PlanFixture();
            var canvas = fixture.Canvas;
            foreach (var brush in new[] { canvas.BackgroundBrush, canvas.FoilBrush, canvas.SelectionBrush,
                         canvas.FocusBrush, canvas.MuteBrush, canvas.DangerBrush, canvas.WarningBrush, canvas.SoftBrush })
                if (brush is not ISolidColorBrush { Color.A: 255 })
                    throw new Exception("Plan brush is missing or not opaque from the theme");
            if (canvas.FoilBrush is not ISolidColorBrush foil || foil.Color != Color.Parse("#85c9c4"))
                throw new Exception("Plan foil brush does not match the theme token");
        });

        DesktopChecks.Check("PlanCanvas_ProbeAndDelta_NotLiveRegions", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point));
            if (AutomationProperties.GetLiveSetting(fixture.Canvas) != AutomationLiveSetting.Off ||
                fixture.Canvas.GetVisualDescendants().OfType<Control>()
                    .Any(control => AutomationProperties.GetLiveSetting(control) != AutomationLiveSetting.Off))
                throw new Exception("Frame-by-frame probe is a live region");
        });

        DesktopChecks.Check("PlanCanvas_AutomationPeers_HandleNamesCarryAngleAndLength", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            Task.Run(() => fixture.Controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)))
                .GetAwaiter().GetResult();
            fixture.Settle();
            var peer = ControlAutomationPeer.CreatePeerForElement(fixture.Canvas)!;
            var handles = peer.GetChildren()!.Where(child => child.GetName().Contains("handle", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (handles.Length < 2 || handles.Any(child => !child.GetName().Contains("°", StringComparison.Ordinal) ||
                                                      !child.GetName().Contains("mm", StringComparison.Ordinal)))
                throw new Exception("Handle peers omitted direction, angle or length");
        });

        DesktopChecks.Check("PlanCanvas_DragFrame_RenderedCurveThroughDraftSample", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.RgbAtPoint(point);
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, 24);
            if (fixture.Controller.Gesture != GestureState.Dragging || fixture.Controller.Planform?.Basis != "preview")
                throw new Exception("Drag did not produce a preview generation");
            var draftPoint = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Settle();
            if (before == fixture.RgbAtPoint(draftPoint) ||
                Contrast(fixture.RgbAtPoint(draftPoint), fixture.BackgroundPixel()) < 3)
                throw new Exception("Draft control point did not move in the realized pixels");
            fixture.ReleaseDrag(point, 0, 24);
        });

        DesktopChecks.Check("PlanCanvas_DragDeltaReadout_Live", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, 20);
            if (fixture.Canvas.ProbeText?.Contains("Δ aft", StringComparison.Ordinal) != true)
                throw new Exception("Live drag probe omitted aft delta");
            fixture.ReleaseDrag(point, 0, 20);
        });

        DesktopChecks.Check("PlanCanvas_ShiftDragFromPoint_OrthoLocked", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 32, 3, KeyModifiers.Shift);
            var moved = fixture.Controller.Planform!.Trailing.Points[4];
            if (moved.AftMeters != point.AftMeters || moved.SpanMeters == point.SpanMeters)
                throw new Exception("Shift-drag did not lock the small aft component");
            fixture.ReleaseDrag(point, 32, 3);
        });

        DesktopChecks.Check("PlanCanvas_TabWithMultiSelection_KeepsSelection", () =>
        {
            using var fixture = new PlanFixture();
            var points = fixture.Controller.Planform!.Leading.Points;
            fixture.Canvas.SelectPoint(new PointRef(points[3].Curve, points[3].Id), false, false);
            fixture.Canvas.SelectPoint(new PointRef(points[4].Curve, points[4].Id), true, false);
            fixture.Canvas.FocusNext();
            if (fixture.Controller.Selection is not Selection.Points selected || selected.Items.Count != 2)
                throw new Exception("Tab collapsed multiple selection");
        });

        DesktopChecks.Check("PlanCanvas_Escape_DismissTooltipThenClearSelection", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.SelectPoint(new PointRef(point.Curve, point.Id), false, false);
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point));
            fixture.KeyDown(Key.Escape);
            if (fixture.Canvas.TooltipText is not null || fixture.Controller.Selection is not Selection.Points)
                throw new Exception("First Escape did not dismiss tooltip while keeping selection");
            fixture.KeyDown(Key.Escape);
            if (fixture.Controller.Selection is Selection.Points)
                throw new Exception("Second Escape did not clear selection");
        });

        DesktopChecks.Check("PlanCanvas_CKeyInTipChordField_CombNotToggled", () =>
        {
            using var fixture = new PlanFixture();
            var tipChord = new TextBox { Name = "TipChordInput", Text = "12.0" };
            var window = new Window { Content = tipChord, Width = 400, Height = 200 };
            try
            {
                window.Show();
                Settle(window);
                tipChord.Focus();
                tipChord.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent,
                    Source = tipChord, Key = Key.C });
                if (fixture.Controller.CombVisible) throw new Exception("C in a text field toggled Plan comb");
                fixture.Canvas.Focus();
                fixture.KeyDown(Key.C);
                if (!fixture.Controller.CombVisible) throw new Exception("C on focused Plan did not toggle comb");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("PlanCanvas_DragEmptyCanvas_PansPlainShiftMiddle_ClickStillClears", () =>
        {
            // D-1 + F-2 (docs/reviews/m12b-native.md §3): a drag that starts on empty canvas pans; Escape cancels it.
            using var fixture = new PlanFixture();
            var empty = new Point(24, fixture.Canvas.Bounds.Height / 2);
            if (fixture.Canvas.HitTestPoint(empty) is not null) throw new Exception("Test location is not empty canvas");
            string accepted = fixture.Controller.AcceptedSource;
            bool undo = fixture.Controller.CanUndo;
            foreach (var (button, modifiers) in new[] { (MouseButton.Left, KeyModifiers.None),
                         (MouseButton.Left, KeyModifiers.Shift), (MouseButton.Middle, KeyModifiers.None) })
            {
                var before = fixture.Controller.PlanCamera;
                fixture.DragAt(empty, new Vector(30, 20), button, modifiers);
                var after = fixture.Controller.PlanCamera;
                if (after.PanSpanPixels - before.PanSpanPixels != 30 || after.PanAftPixels - before.PanAftPixels != 20)
                    throw new Exception($"{modifiers} {button} drag on empty canvas did not pan: {before} -> {after}");
            }
            var start = fixture.Controller.PlanCamera;
            fixture.DragAt(empty, new Vector(40, 10), MouseButton.Left, KeyModifiers.None, escapeBeforeRelease: true);
            if (fixture.Controller.PlanCamera != start)
                throw new Exception("Escape did not cancel the pan back to its start");
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.SelectPoint(new PointRef(point.Curve, point.Id), false, false);
            fixture.DragAt(empty, new Vector(1, 1), MouseButton.Left, KeyModifiers.None);
            if (fixture.Controller.Selection is not Selection.Foil)
                throw new Exception("A click on empty canvas did not clear the selection to the Foil");
            if (fixture.Controller.AcceptedSource != accepted || fixture.Controller.CanUndo != undo)
                throw new Exception("Panning changed the source or the undo history");
        });

        DesktopChecks.Check("PlanCanvas_ClickStationChip_SelectsStation", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var chip = fixture.Canvas.VisibleStationChips.First();
            fixture.ClickAt(chip.Bounds.Center);
            if (fixture.Controller.Selection is not Selection.Station station || station.Index != chip.Index)
                throw new Exception("Clicking the rendered station chip did not select its station");
        });

        DesktopChecks.Check("PlanCanvas_CollidingChips_AlternateHiddenStillInBrowser", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            fixture.Canvas.ZoomAt(.1, new Point(fixture.Canvas.Bounds.Width / 2, 100));
            fixture.Settle();
            int all = fixture.Controller.Planform!.Stations.Count;
            int shown = fixture.Canvas.VisibleStationChips.Count;
            int browser = fixture.Host.Browser.FindControl<ListBox>("StationList")!.ItemCount;
            if (all < 2 || shown >= all || browser != all)
                throw new Exception($"Overlapping chips were not alternated while Browser kept rows: {shown}/{all}/{browser}");
        });

        DesktopChecks.Check("PlanCanvas_DoubleClickPoint_RaisesTypeValueRequest", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.DoubleClick(point);
            if (fixture.Canvas.LastValueRequest != $"{point.Curve}:{point.Id}")
                throw new Exception("Double-click did not request the point's typed value");
        });

        DesktopChecks.Check("PlanCanvas_FocusOffscreenPoint_PansIntoView", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.PanBy(2000, 0);
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.Settle();
            var position = fixture.Canvas.ScreenPoint(point);
            if (position.X < 16 || position.X > fixture.Canvas.Bounds.Width - 16)
                throw new Exception("Focused offscreen point was not panned into view");
            fixture.AssertGlyphPixel(point);
        });

        DesktopChecks.Check("PlanCanvas_FocusUnderProbeOrChip_PansIntoView", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.Canvas.ScreenPoint(point);
            fixture.Canvas.PanBy(fixture.Canvas.Bounds.Width - 100 - before.X, 20 - before.Y);
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.Settle();
            var position = fixture.Canvas.ScreenPoint(point);
            if (position.X > fixture.Canvas.Bounds.Width - 428 && position.Y < 60)
                throw new Exception("Focused point remains under the probe");
            fixture.AssertGlyphPixel(point);
        });

        DesktopChecks.Check("PlanCanvas_LockedNudge_AssertiveLockCopy", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Leading.Points[0];
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.KeyDown(Key.Down);
            // The rendered status line too: a polite pane announcement must not land after the lock copy (PG-36).
            string? line = fixture.Host.ModelView.FindControl<TextBlock>("StatusText")?.Text;
            if (fixture.Controller.Gesture != GestureState.Idle ||
                !fixture.Controller.Status.Contains("fixed", StringComparison.OrdinalIgnoreCase) ||
                line?.Contains("fixed", StringComparison.OrdinalIgnoreCase) != true ||
                AutomationProperties.GetLiveSetting(fixture.Canvas) != AutomationLiveSetting.Assertive)
                throw new Exception("Locked point nudge did not announce its lock assertively" +
                    $" (gesture {fixture.Controller.Gesture}, status \"{fixture.Controller.Status}\", line \"{line}\", live {AutomationProperties.GetLiveSetting(fixture.Canvas)})");
        });

        DesktopChecks.Check("PlanCanvas_EscapeOnHandle_FocusBackToPoint", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            Task.Run(() => fixture.Controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)))
                .GetAwaiter().GetResult();
            fixture.Settle();
            var handle = fixture.Controller.Planform!.Trailing.Points.First(item => item.AnchorId == point.Id);
            fixture.Canvas.FocusPoint(new PointRef(handle.Curve, handle.Id));
            fixture.KeyDown(Key.Escape);
            if (fixture.Canvas.FocusedTarget?.VertexId != point.Id)
                throw new Exception("Escape on a handle did not return focus to its anchor");
        });

        DesktopChecks.Check("PlanCanvas_ArrowOnHandle_MovesHandleWithCoMotion", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            Task.Run(() => fixture.Controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)))
                .GetAwaiter().GetResult();
            fixture.Settle();
            var handles = fixture.Controller.Planform!.Trailing.Points.Where(item => item.AnchorId == point.Id).ToArray();
            var handle = handles[0];
            fixture.Canvas.FocusPoint(new PointRef(handle.Curve, handle.Id));
            fixture.KeyDown(Key.Down);
            fixture.KeyUp(Key.Down);
            fixture.WaitGesture();
            var after = fixture.Controller.Planform!.Trailing.Points.Single(item => item.Id == handle.Id);
            if (after.AftMeters == handle.AftMeters)
                throw new Exception("Arrow did not move the focused handle");
            var partner = fixture.Controller.Planform!.Trailing.Points.Single(item => item.Id == handles[1].Id);
            if (partner.AftMeters == handles[1].AftMeters)
                throw new Exception("Smooth opposite handle did not co-move");
        });

        DesktopChecks.Check("PlanCanvas_ReleaseEdgesCross_PointRenderedAtOriginal", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var plan = fixture.Controller.Planform!;
            var point = plan.Trailing.Points[4];
            var leading = CfdWorkbench.Core.Planform.Probe(plan, point.Eta).LeadingAftMeters;
            double delta = fixture.Canvas.ScreenPoint(new PointView(point.Curve, point.Id, point.Index, point.Eta,
                point.SpanMeters, leading - .04, point.Role, point.AnchorId, point.Kind, point.Freedom, point.Locks)).Y -
                fixture.Canvas.ScreenPoint(point).Y;
            string before = fixture.Controller.AcceptedSource;
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, delta);
            fixture.ReleaseDrag(point, 0, delta);
            fixture.WaitGesture();
            fixture.Settle();
            var restored = fixture.Controller.Planform!.Trailing.Points[4];
            if (fixture.Controller.AcceptedSource != before || restored != point)
                throw new Exception("Crossing release changed accepted geometry");
            fixture.AssertGlyphPixel(restored);
        });

        DesktopChecks.Check("PlanCanvas_AdvisoryCrossingClear_CertificateStillDecides", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var plan = fixture.Controller.Planform!;
            var point = plan.Trailing.Points[4];
            var leading = CfdWorkbench.Core.Planform.Probe(plan, point.Eta).LeadingAftMeters;
            double delta = fixture.Canvas.ScreenPoint(new PointView(point.Curve, point.Id, point.Index, point.Eta,
                point.SpanMeters, leading - .04, point.Role, point.AnchorId, point.Kind, point.Freedom, point.Locks)).Y -
                fixture.Canvas.ScreenPoint(point).Y;
            string before = fixture.Controller.AcceptedSource;
            bool undoBefore = fixture.Controller.CanUndo;
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, delta);
            var advisory = typeof(PlanCanvas).GetField("advisoryCrossing",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("Advisory tier field missing");
            advisory.SetValue(fixture.Canvas, false);
            fixture.ReleaseDrag(point, 0, delta);
            fixture.WaitGesture();
            if (fixture.Controller.AcceptedSource != before || fixture.Controller.CanUndo != undoBefore)
                throw new Exception($"Advisory clear overruled the geometry certificate: changed={fixture.Controller.AcceptedSource != before}, status={fixture.Controller.Status}");
            fixture.AssertGlyphPixel(fixture.Controller.Planform!.Trailing.Points[4]);
        });

        DesktopChecks.Check("PlanCanvas_CrossingMarker_OnlyWhileReleaseWouldBeRefused", () =>
        {
            // D-2 (docs/reviews/m12b-native.md §3.1, design §0.1 step 6): the marker shows during the drag, never after.
            using var fixture = new PlanFixture(newFoil: true);
            var plan = fixture.Controller.Planform!;
            var point = plan.Trailing.Points[4];
            var leading = CfdWorkbench.Core.Planform.Probe(plan, point.Eta).LeadingAftMeters;
            double cross = fixture.Canvas.ScreenPoint(point with { AftMeters = leading - .04 }).Y - fixture.Canvas.ScreenPoint(point).Y;
            var field = typeof(PlanCanvas).GetField("advisoryCrossing",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("Advisory marker field missing");
            bool Marker() => (bool)field.GetValue(fixture.Canvas)!;
            string before = fixture.Controller.AcceptedSource;
            // Bisect the drag distance down to the pixel where the release starts to be refused. At every probe, the marker
            // during the drag must equal the release outcome, and no release leaves it behind.
            bool Probe(double dy)
            {
                fixture.BeginDrag(point);
                fixture.MoveDrag(point, 0, dy);
                bool shown = Marker();
                fixture.ReleaseDrag(point, 0, dy);
                fixture.WaitGesture();
                fixture.Settle();
                bool refused = fixture.Controller.AcceptedSource == before;
                if (shown != refused)
                    throw new Exception($"dy={dy}: marker during the drag={shown}, release refused={refused}");
                if (Marker()) throw new Exception($"dy={dy}: the marker stayed after the release");
                if (!refused) { fixture.Controller.Undo(); fixture.Settle(); }
                return refused;
            }
            double valid = -8, refusedAt = Math.Round(cross);
            if (Probe(valid) || !Probe(refusedAt)) throw new Exception("the bracket does not straddle the refusal");
            while (valid - refusedAt > 1)
            {
                double mid = Math.Round((valid + refusedAt) / 2);
                if (Probe(mid)) refusedAt = mid; else valid = mid;
            }
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, cross);
            if (!Marker()) throw new Exception("no marker on a crossing drag");
            fixture.MoveDrag(point, 0, -8);
            if (Marker()) throw new Exception("the marker stayed after the drag moved back to a valid position");
            fixture.MoveDrag(point, 0, cross);
            fixture.KeyDown(Key.Escape);
            fixture.WaitGesture();
            fixture.Settle();
            if (Marker()) throw new Exception("the marker stayed after Escape");
            fixture.ReleaseDrag(point, 0, cross);
        });

        DesktopChecks.Check("PlanCanvas_HoverProbe_ParksPointerFirst", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.ParkAndHover(point);
            if (fixture.Canvas.ProbeText?.Contains("chord", StringComparison.Ordinal) != true ||
                Contrast(fixture.RgbAtCanvas(fixture.Canvas.Bounds.Width - 420, 12), fixture.BackgroundPixel()) < 1.1)
                throw new Exception("Parked pointer did not produce a visible probe");
        });

        DesktopChecks.Check("PlanCanvas_NotCertifiedFoil_PointsDimmedBannerNoDraft", () =>
        {
            using var fixture = new PlanFixture(newFoil: true);
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            var before = fixture.RgbAtPoint(point);
            var parse = FoilSource.Parse(System.Text.Encoding.UTF8.GetBytes(fixture.Controller.AcceptedSource));
            var uncertified = CfdWorkbench.Core.Geometry.Assess(parse, TimeSpan.Zero);
            if (uncertified.Status == GeometryStatus.Certified) throw new Exception("Fixture did not force an uncertified assessment");
            typeof(WorkbenchController).GetProperty("Inspection")!.SetValue(fixture.Controller,
                new AcceptedInspection(fixture.Controller.Inspection!.Authored, uncertified));
            fixture.Canvas.InvalidateVisual();
            fixture.Settle();
            if (fixture.RgbAtPoint(point) == before || fixture.Canvas.RenderBanner?.Contains("not certified", StringComparison.OrdinalIgnoreCase) != true)
                throw new Exception("Uncertified points were not dimmed with a banner");
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, 20);
            if (fixture.Controller.Gesture != GestureState.Idle || fixture.Controller.Draft is not null)
                throw new Exception("Uncertified foil opened a point draft");
        });

        DesktopChecks.Check("PlanCanvas_RenderThrows_CopyTryAgainAndEvent", () =>
        {
            using var fixture = new PlanFixture();
            ShellEvents.Clear();
            fixture.Canvas.RenderGuard = () => throw new InvalidOperationException("render probe");
            fixture.Canvas.InvalidateVisual();
            fixture.Settle();
            fixture.FrameSnapshot();
            fixture.Settle();
            var error = fixture.Host.ModelView.FindControl<Border>("PlanRenderErrorBand")!;
            var retry = fixture.Host.ModelView.FindControl<Button>("PlanRenderTryAgainButton")!;
            if (!error.IsEffectivelyVisible || retry.Content?.ToString() != "Try again" ||
                !ShellEvents.Read().Any(item => item.Name == "shell.pane.render" && item.Code == "shell.pane.render"))
                throw new Exception($"Render error omitted copy, Try again, or telemetry: visible={error.IsEffectivelyVisible}, button={retry.Content}, events={ShellEvents.Read().Count}");
            fixture.Canvas.RenderGuard = null;
            retry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            fixture.Settle();
            if (error.IsEffectivelyVisible) throw new Exception("Try again did not restore Plan rendering");
        });
    }

    public static void RunReadiness()
    {
        AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
        DesktopChecks.Check("Readiness_PlanRender_Under8Ms", () =>
        {
            using var fixture = new PlanFixture(newFoil: true, width: 1440, height: 900);
            using var bitmap = new RenderTargetBitmap(new PixelSize(1440, 900));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bitmap.Render(fixture.Window);
            watch.Stop();
            Console.WriteLine($"READINESS-MEASURE PlanRender {watch.Elapsed.TotalMilliseconds:F2} ms target 8.00 ms " +
                (watch.Elapsed.TotalMilliseconds <= 8 ? "met" : "miss"));
        });
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 10; i++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static double Contrast((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
    {
        static double Linear(byte channel)
        {
            double value = channel / 255d;
            return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
        }
        static double L((byte R, byte G, byte B) color) =>
            .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        double first = L(a), second = L(b);
        return (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
    }

    private sealed class PlanFixture : IDisposable
    {
        public WorkbenchController Controller { get; } = new();
        public ShellHost Host { get; }
        public Window Window { get; }
        public PlanCanvas Canvas => Host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
        private byte[]? frameBytes;
        private int frameStride;
        private int frameWidth;
        private int frameHeight;
        private Pointer? dragPointer;

        public PlanFixture(bool newFoil = false, double width = 1280, double height = 800,
            ThemeVariant? theme = null)
        {
            if (newFoil) Task.Run(() => Controller.NewFoilAsync()).GetAwaiter().GetResult();
            else Task.Run(() => Controller.OpenExampleAsync()).GetAwaiter().GetResult();
            Host = new ShellHost(Controller);
            Window = new Window { Content = Host, Width = width, Height = height,
                RequestedThemeVariant = theme ?? ThemeVariant.Light };
            Window.Show();
            Settle();
            AssertGlyphPixel(Controller.Planform!.Trailing.Points[4]);
        }

        public void Settle()
        {
            PlanCanvasTests.Settle(Window);
            frameBytes = null;
        }

        public void Press(PointView point, KeyModifiers modifiers = KeyModifiers.None)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(point), Window)
                ?? throw new Exception("Point has no window coordinate");
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, pointer, Window, position, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                modifiers));
            Canvas.RaiseEvent(new PointerReleasedEventArgs(Canvas, pointer, Window, position, 2,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                modifiers, MouseButton.Left));
            Settle();
        }

        public void ClickAt(Point local)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var position = Canvas.TranslatePoint(local, Window)!.Value;
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, pointer, Window, position, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));
            Canvas.RaiseEvent(new PointerReleasedEventArgs(Canvas, pointer, Window, position, 2,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None, MouseButton.Left));
            Settle();
        }

        /// <summary>Press with <paramref name="button"/> at a canvas point, move by <paramref name="by"/>, then release (or Escape first).</summary>
        public void DragAt(Point local, Vector by, MouseButton button, KeyModifiers modifiers, bool escapeBeforeRelease = false)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var (raw, kind, released) = button switch
            {
                MouseButton.Middle => (RawInputModifiers.MiddleMouseButton, PointerUpdateKind.MiddleButtonPressed, PointerUpdateKind.MiddleButtonReleased),
                MouseButton.Right => (RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed, PointerUpdateKind.RightButtonReleased),
                _ => (RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased)
            };
            var start = Canvas.TranslatePoint(local, Window)!.Value;
            var end = Canvas.TranslatePoint(local + by, Window)!.Value;
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, pointer, Window, start, 1,
                new PointerPointProperties(raw, kind), modifiers));
            Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, Canvas, pointer, Window, end, 2,
                new PointerPointProperties(raw, PointerUpdateKind.Other), modifiers));
            Settle();
            if (escapeBeforeRelease) KeyDown(Key.Escape);
            Canvas.RaiseEvent(new PointerReleasedEventArgs(Canvas, pointer, Window, end, 3,
                new PointerPointProperties(RawInputModifiers.None, released), modifiers, button));
            Settle();
        }

        public void DoubleClick(PointView point)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(point), Window)!.Value;
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, pointer, Window, position, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None, 2));
            Settle();
        }

        public void KeyDown(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Canvas.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Source = Canvas,
                Key = key,
                KeyModifiers = modifiers
            });
            Settle();
        }

        public void KeyUp(Key key)
        {
            Canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent,
                Source = Canvas, Key = key });
            Settle();
        }

        public void WaitGesture()
        {
            for (int i = 0; i < 400 && Controller.Gesture != GestureState.Idle; i++)
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }
            if (Controller.Gesture != GestureState.Idle) throw new Exception("Gesture did not complete");
        }

        public void ParkAndHover(PointView point)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, Canvas, pointer, Window,
                new Point(-100, -100), 1, default, KeyModifiers.None));
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(point) + new Vector(0, 30), Window)!.Value;
            Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, Canvas, pointer, Window,
                position, 2, default, KeyModifiers.None));
            Settle();
        }

        public void BeginDrag(PointView point)
        {
            dragPointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(point), Window)!.Value;
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, dragPointer, Window, position, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));
        }

        public void MoveDrag(PointView origin, double dx, double dy, KeyModifiers modifiers = KeyModifiers.None)
        {
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(origin) + new Vector(dx, dy), Window)!.Value;
            Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, Canvas, dragPointer!, Window,
                position, 2, default, modifiers));
            Settle();
        }

        public void ReleaseDrag(PointView origin, double dx, double dy)
        {
            var position = Canvas.TranslatePoint(Canvas.ScreenPoint(origin) + new Vector(dx, dy), Window)!.Value;
            Canvas.RaiseEvent(new PointerReleasedEventArgs(Canvas, dragPointer!, Window, position, 3,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None, MouseButton.Left));
            dragPointer!.Dispose();
            dragPointer = null;
            Settle();
        }

        public (byte R, byte G, byte B) RgbAtPoint(PointView point) => RgbNear(point, 0, 0);

        public (byte R, byte G, byte B) BackgroundPixel() => RgbAtCanvas(Canvas.Bounds.Width / 2 + 40,
            Canvas.Bounds.Height - 30);

        public (byte R, byte G, byte B) RgbNear(PointView point, int dx, int dy)
        {
            var position = Canvas.ScreenPoint(point);
            return RgbAtCanvas(position.X + dx, position.Y + dy);
        }

        public (byte R, byte G, byte B) RgbAtCanvas(double canvasX, double canvasY)
        {
            var position = Canvas.TranslatePoint(new Point(canvasX, canvasY), Window)
                ?? throw new Exception("Canvas position is not translated into the realized window");
            if (frameBytes is null) Capture();
            int x = Math.Clamp((int)Math.Round(position.X), 0, frameWidth - 1);
            int y = Math.Clamp((int)Math.Round(position.Y), 0, frameHeight - 1);
            int offset = y * frameStride + x * 4;
            return (frameBytes![offset + 2], frameBytes[offset + 1], frameBytes[offset]);
        }

        private void Capture()
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Window.Bounds.Width, (int)Window.Bounds.Height));
            bitmap.Render(Window);
            using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888,
                AlphaFormat.Unpremul);
            using var frame = pixels.Lock();
            bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
            frameStride = frame.RowBytes;
            frameWidth = bitmap.PixelSize.Width;
            frameHeight = bitmap.PixelSize.Height;
            frameBytes = new byte[frameStride * frameHeight];
            Marshal.Copy(frame.Address, frameBytes, 0, frameBytes.Length);
        }

        public byte[] FrameSnapshot()
        {
            if (frameBytes is null) Capture();
            return (byte[])frameBytes!.Clone();
        }

        public void AssertGlyphPixel(PointView point)
        {
            var background = BackgroundPixel();
            double contrast = 0;
            for (int dx = -7; dx <= 7; dx++)
            for (int dy = -7; dy <= 7; dy++)
                contrast = Math.Max(contrast, Contrast(RgbNear(point, dx, dy), background));
            if (contrast < 3)
                throw new Exception($"{point.Curve} point {point.Index} is not rendered with 3:1 contrast: {contrast:F2}, screen {Canvas.ScreenPoint(point)}");
        }

        public void Dispose()
        {
            Window.Close();
            for (int i = 0; i < 10; i++) Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Controller.Dispose();
        }
    }
}
