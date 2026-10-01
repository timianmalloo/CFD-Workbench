using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop.Shell;

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
}
