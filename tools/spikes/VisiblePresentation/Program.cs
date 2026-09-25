// R41 synthetic target. No product references, renderer override or native automation.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;

namespace VisiblePresentation;

public sealed record Witness(string Operation, string Source, string Draft, int Generation, string State);
public sealed record Region(string Role, Witness Witness, int Value);

public sealed class TargetState
{
    public Region[] Regions { get; private set; } = Create("initial", 0, "accepted", 80);
    public int Generation { get; private set; }
    private static Region[] Create(string operation, int generation, string state, int value)
    {
        string source = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"synthetic:{value}")));
        var witness = new Witness(operation, source, "synthetic-draft", generation, state);
        return [new("inspector", witness, value), new("status", witness, value), new("canvas", witness, value)];
    }
    public int BeginPreview()
    {
        Generation++;
        Regions = Create("preview", Generation, "assessing", 120);
        return Generation;
    }
    public bool CompletePreview(int generation)
    {
        if (generation != Generation || Regions[0].Witness.State != "assessing") return false;
        Regions = Create("preview", Generation, "preview", 120);
        return true;
    }
    public void Cancel()
    {
        Generation++;
        Regions = Create("cancel", Generation, "cancel-acknowledged", 80);
    }
}

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Timebase { public uint Numerator; public uint Denominator; }
    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern ulong mach_absolute_time();
    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern int mach_timebase_info(out Timebase info);

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.SequenceEqual(new[] { "--contracts" }))
        {
            var state = new TargetState();
            int old = state.BeginPreview();
            state.Cancel();
            if (state.CompletePreview(old) || state.Regions.Any(r => r.Witness.State != "cancel-acknowledged")) return 1;
            int next = state.BeginPreview();
            if (!state.CompletePreview(next) || state.Regions.Any(r => r.Value != 120 || r.Witness.Generation != next)) return 1;
            Console.WriteLine(JsonSerializer.Serialize(new { qualification = "synthetic-executed", late_preview_refused = true,
                regions = state.Regions, visible_latency = "Not assessed", backend = "unobserved" }));
            return 0;
        }
        if (args.SequenceEqual(new[] { "--clock" }))
        {
            if (!OperatingSystem.IsMacOS()) { Console.WriteLine("{\"clock\":\"Not assessed: macOS only\"}"); return 3; }
            if (mach_timebase_info(out var tb) != 0 || tb.Denominator == 0) return 3;
            var pairs = new List<object>();
            for (int i = 0; i < 32; i++)
            {
                long before = Stopwatch.GetTimestamp();
                ulong mach = mach_absolute_time();
                long after = Stopwatch.GetTimestamp();
                pairs.Add(new { before, mach, after });
            }
            Console.WriteLine(JsonSerializer.Serialize(new { qualification = "noncapture-clock-executed",
                stopwatch_frequency = Stopwatch.Frequency, mach_numerator = tb.Numerator, mach_denominator = tb.Denominator,
                pairs, drift = "Not assessed: adjacent samples only", visible_latency = "Not assessed" }));
            return 0;
        }
        if (!args.SequenceEqual(new[] { "--target" }))
        {
            Console.WriteLine("Use --contracts, --clock, or --target. Target launch requires root-owned native UI review.");
            return args.Length == 0 || args.SequenceEqual(new[] { "--help" }) ? 0 : 2;
        }
        AppBuilder.Configure<TargetApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
        return 0;
    }
}

public sealed class TargetApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var state = new TargetState();
            var surface = new TargetSurface(state);
            var window = new Window { Title = "CFD synthetic presentation target", Width = 720, Height = 480,
                CanResize = false, Content = surface };
            desktop.MainWindow = window;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            int stage = 0;
            int generation = 0;
            timer.Tick += (_, _) =>
            {
                if (stage == 0) generation = state.BeginPreview();
                else if (stage == 1) state.CompletePreview(generation);
                else if (stage == 2) state.Cancel();
                else if (stage == 3) state.CompletePreview(generation);
                else { timer.Stop(); window.Close(); return; }
                stage++;
                surface.InvalidateVisual();
                Console.WriteLine(JsonSerializer.Serialize(new { kind = "synthetic-state-change-not-presentation",
                    tick = Stopwatch.GetTimestamp(), frequency = Stopwatch.Frequency, regions = state.Regions,
                    backend = "unobserved", visible_latency = "Not assessed" }));
            };
            window.Opened += (_, _) => timer.Start();
            window.Closed += (_, _) => timer.Stop();
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public sealed class TargetSurface(TargetState state) : Control
{
    // simplify: three full-region bit fields are the synthetic content itself, not a product marker.
    // Ceiling: endpoint feasibility only; product integration needs independent semantic pixel oracles.
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.White, Bounds);
        for (int region = 0; region < 3; region++)
        {
            var item = state.Regions[region];
            byte[] content = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(item)));
            double width = Bounds.Width / 3;
            double cellWidth = width / 16;
            double cellHeight = Bounds.Height / 16;
            for (int bit = 0; bit < 256; bit++)
            {
                bool on = (content[bit / 8] & (1 << (bit % 8))) != 0;
                context.FillRectangle(on ? Brushes.Black : Brushes.White,
                    new Rect(region * width + (bit % 16) * cellWidth, (bit / 16) * cellHeight, cellWidth, cellHeight));
            }
        }
    }
}
