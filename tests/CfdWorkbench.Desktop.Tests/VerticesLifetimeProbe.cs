using SkiaSharp;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// CR2 probe: the GC-pressure test of the SkiaSharp vertices lifetime. Registered and spawned only under CFD_VERTICES_GC_PROBE=1
/// (otherwise the parent mode prints one SKIP line and the arm mode prints nothing). A raster surface draws 3,000 per-vertex-coloured
/// vertices in a loop for 60 s while a second thread runs a blocking collection plus the finalizers continuously. Arm <c>array</c> calls
/// the array helper the way <c>MeshFrame.Draw</c> did before the fix, whose <c>SKVertices</c> nothing keeps reachable during the native
/// call; arm <c>held</c> builds the vertices explicitly and holds them (the pattern <c>MeshFrame.Draw</c> ships; that class is internal to Desktop, so the probe carries a copy). A native fault ends the child, which the
/// parent's exit line names. Defect class NATIVE-TEMPORARY-FINALIZED-MID-CALL.
/// </summary>
internal static class VerticesLifetimeProbe
{
    public const string Mode = "--vertices-lifetime-probe";
    public const string Variable = "CFD_VERTICES_GC_PROBE";
    private const int VertexCount = 3000, Seconds = 60;

    public static bool Enabled => Environment.GetEnvironmentVariable(Variable) == "1";

    /// <summary>The parent: one child per arm, one after the other, so the arms never share the machine.</summary>
    public static int Spawn()
    {
        if (!Enabled) { Console.WriteLine($"SKIP {Mode}: set {Variable}=1"); return 0; }
        int array = DesktopChecks.Spawn($"{Mode}=array");
        int held = DesktopChecks.Spawn($"{Mode}=held");
        return array != 0 ? array : held;
    }

    /// <summary>One arm in this process; exits 0 after the bounded time, or the process dies of the native fault.</summary>
    public static int RunArm(string arm)
    {
        if (!Enabled || arm is not ("array" or "held")) return 0;
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
        Console.WriteLine($"VERTICES-PROBE arm={arm} vertices={VertexCount} seconds={Seconds}");
        Console.Out.Flush();
        long began = System.Diagnostics.Stopwatch.GetTimestamp();
        while (!stop.IsCancellationRequested)
        {
            if (arm == "array") canvas.DrawVertices(SKVertexMode.Triangles, points, null, colors, fill);
            else DrawHeld(canvas, points, colors, fill);
            draws++;
        }
        collector.Join();
        Console.WriteLine($"PASS VerticesLifetime_{arm}: {draws} draws under {Interlocked.Read(ref collections)} collections in {System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalSeconds:F0} s");
        return 0;
    }

    /// <summary>The shipped pattern, in its own method like <c>MeshFrame.Draw</c>, so the JIT tiers it up and tracks the local's lifetime as it would there.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void DrawHeld(SKCanvas canvas, SKPoint[] points, SKColor[] colors, SKPaint fill)
    {
        using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, points, null, colors);
        canvas.DrawVertices(vertices, SKBlendMode.Modulate, fill);
        GC.KeepAlive(vertices);
    }
}
