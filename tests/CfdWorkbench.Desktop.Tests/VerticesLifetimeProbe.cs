using System.Text.RegularExpressions;
using SkiaSharp;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// CR2 probe: the GC-pressure test of the SkiaSharp vertices lifetime. Registered and spawned only under CFD_VERTICES_GC_PROBE=1
/// (otherwise the parent mode prints one SKIP line and the arm mode prints nothing). A raster surface draws 3,000 per-vertex-coloured
/// vertices in a loop for 60 s while a second thread runs a blocking collection plus the finalizers continuously. Arm <c>array</c> calls
/// the array helper the way <c>MeshFrame.Draw</c> did before the fix, whose <c>SKVertices</c> nothing keeps reachable during the native
/// call; it is the positive control and is expected to crash. Arm <c>held</c> draws through the shipped <c>MeshFrame.Draw</c> (Desktop
/// grants this assembly <c>InternalsVisibleTo</c>). The parent's exit is the verdict (<see cref="Verdict"/>): 0 only when held survived
/// and the control crashed. Defect class NATIVE-TEMPORARY-FINALIZED-MID-CALL.
/// </summary>
internal static partial class VerticesLifetimeProbe
{
    public const string Mode = "--vertices-lifetime-probe";
    public const string Variable = "CFD_VERTICES_GC_PROBE";
    private const int VertexCount = 3000, Seconds = 60;

    public static bool Enabled => Environment.GetEnvironmentVariable(Variable) == "1";

    /// <summary>False in a Debug build of this assembly, where the JIT keeps every local live and the held arm can never go red.</summary>
    public static bool Optimized => !(typeof(VerticesLifetimeProbe).Assembly
        .GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false)
        .OfType<System.Diagnostics.DebuggableAttribute>().FirstOrDefault()?.IsJITOptimizerDisabled ?? false);

    /// <summary>The pure verdict: the array arm is the positive control, so the probe passes only when held survived and the control crashed.</summary>
    public static (int Exit, string Line) Verdict(int held, int array, long draws, long collections) =>
        held != 0 ? (held, $"FAIL VerticesLifetime held crashed ({Code(held)})")
        : array == 0 ? (1, "FAIL VerticesLifetime the array control did not crash: the probe is not sensitive")
        : (0, $"PASS VerticesLifetime: held survived {draws} draws under {collections} collections; the array control crashed ({Code(array)})");

    private static string Code(int exit) => "0x" + exit.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The parent: the pure verdict checks, then one child per arm, one after the other, so the arms never share the machine.</summary>
    public static int Spawn()
    {
        int failed = VerdictChecks();
        if (!Enabled) { Console.WriteLine($"SKIP {Mode}: set {Variable}=1"); return failed == 0 ? 0 : 1; }
        Console.WriteLine($"VERTICES-PROBE optimizer={(Optimized ? "enabled (Release)" : "disabled (Debug)")}");
        if (!Optimized) { Console.WriteLine("SKIP VerticesLifetime held verdict: Debug build, the JIT keeps every local live (run Release)"); return failed == 0 ? 0 : 1; }
        int array = DesktopChecks.Spawn($"{Mode}=array");
        var captured = new StringWriter();
        int held = DesktopChecks.SpawnWith(captured, Console.Error, DesktopChecks.RunBuffered, [$"{Mode}=held"]);
        Console.Write(captured);
        var stats = HeldStats().Match(captured.ToString());
        long draws = stats.Success ? long.Parse(stats.Groups[1].Value) : -1, collections = stats.Success ? long.Parse(stats.Groups[2].Value) : -1;
        var (exit, line) = Verdict(held, array, draws, collections);
        Console.WriteLine(line);
        return failed != 0 && exit == 0 ? 1 : exit;
    }

    [GeneratedRegex(@"PASS VerticesLifetime_held: (\d+) draws under (\d+) collections")]
    private static partial Regex HeldStats();

    private static int VerdictChecks()
    {
        int failures = 0;
        void Expect(string name, (int Exit, string Line) got, int exit, string line)
        {
            bool ok = got.Exit == exit && got.Line == line;
            if (!ok) failures++;
            Console.WriteLine(ok ? $"PASS {name}" : $"FAIL {name}: got exit {got.Exit} '{got.Line}', want exit {exit} '{line}'");
        }
        Expect("VerticesLifetimeVerdict_HeldSurvivedControlCrashed_Passes", Verdict(0, unchecked((int)0xC0000005), 500, 600000), 0,
            "PASS VerticesLifetime: held survived 500 draws under 600000 collections; the array control crashed (0xC0000005)");
        Expect("VerticesLifetimeVerdict_HeldCrashed_FailsWithItsCode", Verdict(unchecked((int)0xC0000005), unchecked((int)0xC0000005), 0, 0), unchecked((int)0xC0000005),
            "FAIL VerticesLifetime held crashed (0xC0000005)");
        Expect("VerticesLifetimeVerdict_ControlDidNotCrash_FailsInsensitive", Verdict(0, 0, 500, 600000), 1,
            "FAIL VerticesLifetime the array control did not crash: the probe is not sensitive");
        return failures;
    }

    /// <summary>One arm in this process; exits 0 after the bounded time, or the process dies of the native fault.</summary>
    public static int RunArm(string arm)
    {
        if (!Enabled) return 0;
        if (arm is not ("array" or "held")) { Console.WriteLine($"FAIL VerticesLifetime unknown arm '{arm}' (array or held)"); return 2; }
        var rng = new Random(20261022);
        var points = new SKPoint[VertexCount];
        var colors = new SKColor[VertexCount];
        for (int i = 0; i < VertexCount; i++)
        {
            points[i] = new SKPoint((float)(rng.NextDouble() * 1200), (float)(rng.NextDouble() * 800));
            colors[i] = new SKColor(0xFF, (byte)(0x30 + i % 40), 0x57, 0x5B);
        }
        using var surface = SKSurface.Create(new SKImageInfo(1280, 800, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var fill = new SKPaint { IsAntialias = false, Color = SKColors.White };
        var canvas = surface.Canvas;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(Seconds));
        long collections = 0, draws = 0;
        var collector = new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                Interlocked.Increment(ref collections);
            }
        }) { IsBackground = true };
        collector.Start();
        Console.WriteLine($"VERTICES-PROBE arm={arm} vertices={VertexCount} seconds={Seconds} optimizer={(Optimized ? "enabled" : "disabled")}");
        Console.Out.Flush();
        long began = System.Diagnostics.Stopwatch.GetTimestamp();
        while (!stop.IsCancellationRequested)
        {
            if (arm == "array") canvas.DrawVertices(SKVertexMode.Triangles, points, null, colors, fill);
            else DrawHeld(canvas, points, colors);
            draws++;
        }
        collector.Join();
        Console.WriteLine($"PASS VerticesLifetime_{arm}: {draws} draws under {Interlocked.Read(ref collections)} collections in {System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalSeconds:F0} s");
        return 0;
    }

    /// <summary>The shipped code: <c>MeshFrame.Draw</c> itself, in its own non-inlined method so the JIT tiers it up as in the app.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void DrawHeld(SKCanvas canvas, SKPoint[] points, SKColor[] colors) =>
        new MeshFrame { Batches = [new MeshBatch(points, colors, default, 0)] }.Draw(canvas);
}
