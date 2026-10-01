using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Automation;
using Avalonia.Controls;
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
                if (point is not IInvokeProvider invoke)
                    throw new Exception("Point peer cannot invoke the value request");
                invoke.Invoke();
                if (canvas.LastValueRequest?.Contains("trailing", StringComparison.Ordinal) != true)
                    throw new Exception("Point peer Invoke had no effect");
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
                fixture.Canvas.TooltipText?.Contains("span", StringComparison.Ordinal) != true ||
                Contrast(fixture.RgbNear(point, 10, 0), fixture.BackgroundPixel()) < 3)
                throw new Exception("Hover tooltip or rendered 10 px ring missing");
        });

        DesktopChecks.Check("PlanCanvas_HoverRail_TracingProbeReadout", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point) + new Vector(0, 30));
            if (fixture.Canvas.ProbeText?.Contains("span", StringComparison.Ordinal) != true ||
                fixture.Canvas.ProbeText?.Contains("chord", StringComparison.Ordinal) != true)
                throw new Exception("Tracing probe has no span and chord readout");
        });

        DesktopChecks.Check("PlanCanvas_ShiftClickAndCommandClick_ExtendAndToggle", () =>
        {
            using var fixture = new PlanFixture();
            var points = fixture.Controller.Planform!.Trailing.Points;
            fixture.Canvas.SelectPoint(new PointRef(points[3].Curve, points[3].Id), extend: false, toggle: false);
            fixture.Canvas.SelectPoint(new PointRef(points[4].Curve, points[4].Id), extend: true, toggle: false);
            if (fixture.Controller.Selection is not Selection.Points selected || selected.Items.Count != 2)
                throw new Exception("Shift selection did not extend");
            fixture.Canvas.SelectPoint(new PointRef(points[3].Curve, points[3].Id), extend: false, toggle: true);
            if (fixture.Controller.Selection is not Selection.Points toggled || toggled.Items.Count != 1 ||
                toggled.Items[0].VertexId != points[4].Id)
                throw new Exception("Command selection did not toggle");
        });

        DesktopChecks.Check("PlanCanvas_SpaceAndShiftSpace_SelectAndToggle", () =>
        {
            using var fixture = new PlanFixture();
            var point = fixture.Controller.Planform!.Leading.Points[3];
            fixture.Canvas.FocusPoint(new PointRef(point.Curve, point.Id));
            fixture.Canvas.SelectFocused(toggle: false);
            if (fixture.Controller.Selection is not Selection.Points selected || selected.Items[0].VertexId != point.Id)
                throw new Exception("Space did not select focused point");
            fixture.Canvas.SelectFocused(toggle: true);
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
    }

    public static void RunReadiness() { }

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
        }

        public void Settle()
        {
            PlanCanvasTests.Settle(Window);
            frameBytes = null;
        }

        public (byte R, byte G, byte B) RgbAtPoint(PointView point) => RgbNear(point, 0, 0);

        public (byte R, byte G, byte B) BackgroundPixel() => RgbAtCanvas(Canvas.Bounds.Width / 2,
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
