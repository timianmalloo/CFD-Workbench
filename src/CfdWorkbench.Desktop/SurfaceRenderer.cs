using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CfdWorkbench.Core;
using SkiaSharp;

namespace CfdWorkbench.Desktop;

/// <summary>
/// Draws a <see cref="SurfaceView"/> through a <see cref="ViewCamera"/> (M1.2b2 §5.2). Shaded: painter-sorted,
/// back-face-culled triangles on one single-hue ramp from the grazing brush to the lit brush, clamped there, with a
/// headlight at the camera; the port half is the starboard mesh through <see cref="Point3.Port"/> with reversed
/// winding. Wireframe: no fill; rails, authored sections and the ten-per-span intermediate mesh rows. In both, the
/// silhouette (edges between a front- and a back-facing triangle, and boundary edges) and the LE, TE and tip outline in
/// 1.5 px foil. Geometry comes only from the mesh; this control applies the mirror and the camera, nothing else.
/// One render path: a Skia draw operation (<c>DrawVertices</c> per run of faces, strokes interleaved in painter's order).
/// </summary>
public sealed class SurfaceRenderer : Control
{
    public static readonly StyledProperty<SurfaceView?> SurfaceProperty =
        AvaloniaProperty.Register<SurfaceRenderer, SurfaceView?>(nameof(Surface));
    public static readonly StyledProperty<ViewCamera?> CameraProperty =
        AvaloniaProperty.Register<SurfaceRenderer, ViewCamera?>(nameof(Camera));
    public static readonly StyledProperty<DisplayMode> DisplayProperty =
        AvaloniaProperty.Register<SurfaceRenderer, DisplayMode>(nameof(Display));
    public static readonly StyledProperty<double?> SelectedEtaProperty =
        AvaloniaProperty.Register<SurfaceRenderer, double?>(nameof(SelectedEta));
    public static readonly StyledProperty<bool> DimmedProperty =
        AvaloniaProperty.Register<SurfaceRenderer, bool>(nameof(Dimmed));
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(BackgroundBrush));
    public static readonly StyledProperty<IBrush?> ShadeGrazingBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(ShadeGrazingBrush));
    public static readonly StyledProperty<IBrush?> ShadeLitBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(ShadeLitBrush));
    public static readonly StyledProperty<IBrush?> FoilBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(FoilBrush));
    public static readonly StyledProperty<IBrush?> FoilEdgeBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(FoilEdgeBrush));
    public static readonly StyledProperty<IBrush?> StationBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(StationBrush));
    public static readonly StyledProperty<IBrush?> MuteBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(MuteBrush));
    public static readonly StyledProperty<IBrush?> GridBrushProperty =
        AvaloniaProperty.Register<SurfaceRenderer, IBrush?>(nameof(GridBrush));
    public static readonly StyledProperty<bool> ShowGroundProperty =
        AvaloniaProperty.Register<SurfaceRenderer, bool>(nameof(ShowGround));

    /// <summary>The share of the ramp a grazing face keeps (ambient); a face toward the headlight reaches the lit brush.</summary>
    public const double Ambient = 0.15;
    public const double OutlineWidth = 1.5;

    private bool renderFailureNotified;
    private volatile bool leaseMissing;

    static SurfaceRenderer()
    {
        AffectsRender<SurfaceRenderer>(SurfaceProperty, CameraProperty, DisplayProperty, SelectedEtaProperty, DimmedProperty,
            BackgroundBrushProperty, ShadeGrazingBrushProperty, ShadeLitBrushProperty, FoilBrushProperty,
            FoilEdgeBrushProperty, StationBrushProperty, MuteBrushProperty, GridBrushProperty, ShowGroundProperty);
    }

    public SurfaceView? Surface { get => GetValue(SurfaceProperty); set => SetValue(SurfaceProperty, value); }
    public ViewCamera? Camera { get => GetValue(CameraProperty); set => SetValue(CameraProperty, value); }
    public DisplayMode Display { get => GetValue(DisplayProperty); set => SetValue(DisplayProperty, value); }
    public double? SelectedEta { get => GetValue(SelectedEtaProperty); set => SetValue(SelectedEtaProperty, value); }
    public bool Dimmed { get => GetValue(DimmedProperty); set => SetValue(DimmedProperty, value); }
    public IBrush? BackgroundBrush { get => GetValue(BackgroundBrushProperty); set => SetValue(BackgroundBrushProperty, value); }
    public IBrush? ShadeGrazingBrush { get => GetValue(ShadeGrazingBrushProperty); set => SetValue(ShadeGrazingBrushProperty, value); }
    public IBrush? ShadeLitBrush { get => GetValue(ShadeLitBrushProperty); set => SetValue(ShadeLitBrushProperty, value); }
    public IBrush? FoilBrush { get => GetValue(FoilBrushProperty); set => SetValue(FoilBrushProperty, value); }
    public IBrush? FoilEdgeBrush { get => GetValue(FoilEdgeBrushProperty); set => SetValue(FoilEdgeBrushProperty, value); }
    public IBrush? StationBrush { get => GetValue(StationBrushProperty); set => SetValue(StationBrushProperty, value); }
    public IBrush? MuteBrush { get => GetValue(MuteBrushProperty); set => SetValue(MuteBrushProperty, value); }
    public IBrush? GridBrush { get => GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }

    /// <summary>The 3D view's ground grid at z = min z, behind the surface (§11.1); the elevations leave it off.</summary>
    public bool ShowGround { get => GetValue(ShowGroundProperty); set => SetValue(ShowGroundProperty, value); }

    /// <summary>Raised after each frame with the milliseconds the UI thread spent building it (<c>view.navigate.end</c>).</summary>
    public event Action<double>? FrameRendered;

    /// <summary>Test seam, as PlanCanvas.RenderGuard: runs before the scene is built, so a test can make rendering throw.</summary>
    public Action? RenderGuard { get; set; }
    public string? RenderError { get; private set; }
    public long RenderSerial { get; private set; }

    /// <summary>The pane name <c>shell.pane.render</c> records (3d · side · front).</summary>
    public string Pane { get; set; } = "surface";
    public event Action<Exception>? RenderFailed;
    public event Action? RenderRecovered;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var size = Bounds.Size;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (BackgroundBrush is not null) context.FillRectangle(BackgroundBrush, new Rect(size));
        try
        {
            if (leaseMissing) throw new InvalidOperationException("The Skia drawing lease is unavailable.");
            RenderGuard?.Invoke();
            if (Surface is { } surface && Camera is { } camera && size.Width > 0 && size.Height > 0)
                context.Custom(new MeshOperation(new Rect(size), SurfaceMesh.Build(surface, camera, size, Display,
                    SelectedEta, Palette(), DimmedTowards(), ShowGround), this));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            RenderError = error.GetType().Name;
            CfdWorkbench.Desktop.Shell.ShellEvents.Record("shell.pane.render", "error", 0,
                System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
                code: "shell.pane.render", pane: Pane, exceptionType: error.GetType().Name);
            if (!renderFailureNotified)
            {
                renderFailureNotified = true;
                RenderFailed?.Invoke(error);
            }
        }
        RenderSerial++;
        FrameRendered?.Invoke(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    public void RetryRender()
    {
        RenderError = null;
        renderFailureNotified = false;
        leaseMissing = false;
        RenderRecovered?.Invoke();
        InvalidateVisual();
    }

    private SurfacePalette Palette() => new(Color(ShadeGrazingBrush), Color(ShadeLitBrush), Color(FoilBrush),
        Color(FoilEdgeBrush), Color(StationBrush), Color(MuteBrush), Color(GridBrush));

    private SKColor? DimmedTowards() => Dimmed ? Color(BackgroundBrush) ?? SKColors.Transparent : null;

    private static SKColor? Color(IBrush? brush) => brush is ISolidColorBrush solid
        ? new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, (byte)Math.Round(solid.Color.A * Math.Clamp(solid.Opacity, 0, 1)))
        : null;

    private sealed class MeshOperation(Rect bounds, MeshFrame frame, SurfaceRenderer owner) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            // Runs on the render thread: a missing lease is reported to the next UI-thread Render, never thrown here.
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature)
            {
                owner.leaseMissing = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(owner.InvalidateVisual);
                return;
            }
            using var lease = feature.Lease();
            frame.Draw(lease.SkCanvas);
        }
    }
}

/// <summary>The resolved colours of one draw; a null colour leaves its layer out.</summary>
internal sealed record SurfacePalette(SKColor? Grazing, SKColor? Lit, SKColor? Foil, SKColor? FoilEdge, SKColor? Station, SKColor? Mute,
    SKColor? Grid = null);

/// <summary>One run of the painter's order: filled triangles (per-vertex colours) or stroke segments of one paint.</summary>
internal sealed record MeshBatch(SKPoint[] Points, SKColor[]? Colors, SKColor StrokeColor, float StrokeWidth);

/// <summary>Screen-space batches in painter's order (far first), ready for one Skia draw.</summary>
internal sealed class MeshFrame
{
    public required IReadOnlyList<MeshBatch> Batches { get; init; }

    public void Draw(SKCanvas canvas)
    {
        using var fill = new SKPaint { IsAntialias = false, Color = SKColors.White };
        using var stroke = new SKPaint { IsAntialias = true, IsStroke = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        foreach (var batch in Batches)
        {
            if (batch.Colors is { } colors)
            {
                canvas.DrawVertices(SKVertexMode.Triangles, batch.Points, null, colors, fill);
                continue;
            }
            stroke.Color = batch.StrokeColor;
            stroke.StrokeWidth = batch.StrokeWidth;
            canvas.DrawPoints(SKPointMode.Lines, batch.Points, stroke);
        }
    }
}

/// <summary>The pure mesh-to-screen step: topology, culling, the headlight ramp, the painter's sort and the stroke sets.</summary>
internal static class SurfaceMesh
{
    public static MeshFrame Build(SurfaceView surface, ViewCamera camera, Size size, DisplayMode display, double? selectedEta,
        SurfacePalette palette, SKColor? dimTowards, bool ground = false)
    {
        var mesh = Topology.Of(surface);
        var screen = new SKPoint[mesh.Positions.Count];
        var depth = new double[mesh.Positions.Count];
        double near = camera.Projection == Projection.Perspective ? camera.Distance * 1e-3 : double.NegativeInfinity;
        for (int index = 0; index < screen.Length; index++)
        {
            var at = camera.Project(mesh.Positions[index], size);
            screen[index] = new SKPoint((float)at.X, (float)at.Y);
            depth[index] = camera.Depth(mesh.Positions[index]);
        }
        bool Visible(int index) => depth[index] > near;

        // Classify every triangle once: front faces carry the fill and, with the back faces, the silhouette.
        int count = mesh.Triangles.Count;
        var front = new bool[count];
        var shade = new double[count];
        var centre = new double[count];
        for (int triangle = 0; triangle < count; triangle++)
        {
            var (a, b, c) = mesh.Triangles[triangle];
            var p = mesh.Positions;
            var normal = ViewCamera.Cross(ViewCamera.Subtract(p[b], p[a]), ViewCamera.Subtract(p[c], p[a]));
            double length = ViewCamera.Length(normal);
            var centroid = new Point3((p[a].X + p[b].X + p[c].X) / 3, (p[a].Y + p[b].Y + p[c].Y) / 3, (p[a].Z + p[b].Z + p[c].Z) / 3);
            double facing = length == 0 ? 0 : ViewCamera.Dot(normal, camera.TowardViewer(centroid)) / length;
            front[triangle] = facing > 0;
            shade[triangle] = Math.Clamp(Ambient + (1 - Ambient) * facing, 0, 1);
            centre[triangle] = (depth[a] + depth[b] + depth[c]) / 3;
        }
        var edges = Edges(mesh, front, centre);

        // One painter's order for faces and strokes, far first. A stroke sorts just after the nearest front face it
        // borders (or at its own depth when it borders none), so faces nearer the eye cover it: no line shows through.
        bool filled = display == DisplayMode.Shaded && palette.Grazing is not null && palette.Lit is not null;
        var items = new List<(double Depth, int Layer, int Index)>(count);
        if (filled)
            for (int triangle = 0; triangle < count; triangle++)
            {
                var (a, b, c) = mesh.Triangles[triangle];
                if (front[triangle] && Visible(a) && Visible(b) && Visible(c)) items.Add((centre[triangle], -1, triangle));
            }
        var layers = new List<(SKColor Color, float Width)>();
        var segments = new List<(int A, int B)>();
        double bias = camera.Distance * 1e-9;
        void Layer(SKColor? color, float width, IEnumerable<(int A, int B)> layerEdges)
        {
            if (color is not { } resolved) return;
            layers.Add((Dim(resolved, dimTowards), width));
            foreach (var (a, b) in layerEdges)
            {
                if (!Visible(a) || !Visible(b)) continue;
                var key = a < b ? (a, b) : (b, a);
                double at = edges.TryGetValue(key, out var edge) && edge.NearestFront < double.PositiveInfinity
                    ? edge.NearestFront - bias : (depth[a] + depth[b]) / 2;
                items.Add((at, layers.Count - 1, segments.Count));
                segments.Add((a, b));
            }
        }

        var rows = Enumerable.Range(0, mesh.Sections);
        if (display == DisplayMode.Wireframe)
            Layer(palette.Mute, 1, mesh.SectionEdges(rows.Where(row => IsTenth(surface.Sections[row].Eta) && surface.Sections[row].Assignment is null)));
        Layer(palette.FoilEdge, 1, mesh.SectionEdges(rows.Where(row => surface.Sections[row].Assignment is not null)));
        Layer(palette.Foil, (float)SurfaceRenderer.OutlineWidth,
            edges.Where(edge => edge.Value.Front > 0 && edge.Value.Back > 0 || edge.Value.Front + edge.Value.Back == 1)
                .Select(edge => edge.Key).Concat(mesh.Outline()));
        if (selectedEta is double eta)
            Layer(palette.Station, 3, mesh.SectionEdges(rows.Where(row => surface.Sections[row].Eta == eta)));

        items.Sort((left, right) => right.Depth != left.Depth ? right.Depth.CompareTo(left.Depth) : left.Layer.CompareTo(right.Layer));
        var batches = new List<MeshBatch>();
        if (ground && palette.Grid is { } grid) batches.AddRange(Ground(surface, camera, size, near, Dim(grid, dimTowards)));
        var points = new List<SKPoint>();
        var colors = new List<SKColor>();
        int open = int.MinValue;
        void Flush()
        {
            if (points.Count == 0) return;
            batches.Add(open < 0
                ? new MeshBatch(points.ToArray(), colors.ToArray(), default, 0)
                : new MeshBatch(points.ToArray(), null, layers[open].Color, layers[open].Width));
            points.Clear();
            colors.Clear();
        }
        foreach (var (_, layer, index) in items)
        {
            if (layer != open) Flush();
            open = layer;
            if (layer < 0)
            {
                var (a, b, c) = mesh.Triangles[index];
                var color = Dim(Ramp(palette.Grazing!.Value, palette.Lit!.Value, shade[index]), dimTowards);
                points.Add(screen[a]); points.Add(screen[b]); points.Add(screen[c]);
                colors.Add(color); colors.Add(color); colors.Add(color);
            }
            else
            {
                points.Add(screen[segments[index].A]);
                points.Add(screen[segments[index].B]);
            }
        }
        Flush();
        return new MeshFrame { Batches = batches };
    }

    private const double Ambient = SurfaceRenderer.Ambient;

    /// <summary>
    /// The ground grid at z = min z under both halves (the approved mockup's grid): lines a third of the chord extent
    /// apart from one extent ahead of the leading edge to two behind it, and a fifth of the half span apart out to
    /// 1.2 half spans each side. Lines whose ends are not both in front of the eye are left out.
    /// </summary>
    internal static IEnumerable<MeshBatch> Ground(SurfaceView surface, ViewCamera camera, Size size, double near, SKColor color)
    {
        double chord = Math.Max(1e-9, surface.MaximumX - surface.MinimumX), half = Math.Max(1e-9, surface.MaximumY);
        double z = surface.MinimumZ, x0 = surface.MinimumX - chord, x1 = surface.MinimumX + 2 * chord, y = 1.2 * half;
        var points = new List<SKPoint>();
        void Line(Point3 a, Point3 b)
        {
            if (!(camera.Depth(a) > near) || !(camera.Depth(b) > near)) return;
            var p = camera.Project(a, size);
            var q = camera.Project(b, size);
            points.Add(new SKPoint((float)p.X, (float)p.Y));
            points.Add(new SKPoint((float)q.X, (float)q.Y));
        }
        for (int k = -3; k <= 6; k++) Line(new Point3(surface.MinimumX + k * chord / 3, -y, z), new Point3(surface.MinimumX + k * chord / 3, y, z));
        for (int k = -6; k <= 6; k++) Line(new Point3(x0, k * half / 5, z), new Point3(x1, k * half / 5, z));
        if (points.Count > 0) yield return new MeshBatch(points.ToArray(), null, color, 1);
    }

    /// <summary>η = i/10: the wireframe's intermediate rows, a subset of the 41 uniform mesh rows (never interpolated).</summary>
    private static bool IsTenth(double eta) => Math.Abs(eta * 10 - Math.Round(eta * 10)) < 1e-9;

    /// <summary>
    /// Per edge: how many front and back faces border it, and the depth of the nearest front face. The silhouette is the
    /// edges with one of each, plus boundary edges (one face).
    /// </summary>
    private static Dictionary<(int, int), (int Front, int Back, double NearestFront)> Edges(Topology mesh, bool[] front, double[] centre)
    {
        var edges = new Dictionary<(int, int), (int Front, int Back, double NearestFront)>();
        for (int triangle = 0; triangle < mesh.Triangles.Count; triangle++)
        {
            var (a, b, c) = mesh.Triangles[triangle];
            foreach (var (u, v) in new[] { (a, b), (b, c), (c, a) })
            {
                var key = u < v ? (u, v) : (v, u);
                var (f, k, nearest) = edges.TryGetValue(key, out var seen) ? seen : (0, 0, double.PositiveInfinity);
                edges[key] = front[triangle] ? (f + 1, k, Math.Min(nearest, centre[triangle])) : (f, k + 1, nearest);
            }
        }
        return edges;
    }

    /// <summary>Lerp in sRGB from grazing to lit; t ≤ 1, so no face is brighter than the lit token.</summary>
    internal static SKColor Ramp(SKColor grazing, SKColor lit, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte Mix(byte from, byte to) => (byte)Math.Round(from + (to - from) * t);
        return new SKColor(Mix(grazing.Red, lit.Red), Mix(grazing.Green, lit.Green), Mix(grazing.Blue, lit.Blue), Mix(grazing.Alpha, lit.Alpha));
    }

    private static SKColor Dim(SKColor color, SKColor? towards) => towards is { } background
        ? new SKColor((byte)((color.Red + background.Red) / 2), (byte)((color.Green + background.Green) / 2),
            (byte)((color.Blue + background.Blue) / 2), color.Alpha)
        : color;

    /// <summary>
    /// Both halves as one closed-at-the-root vertex set: the lower surface shares the upper's leading-edge vertex (and
    /// trailing-edge vertex where they coincide), and the port root shares the starboard root, so the root and the
    /// leading edge are interior edges. Starboard upper triangles wind with outward normal +z; lower and port reverse.
    /// </summary>
    private sealed class Topology
    {
        public List<Point3> Positions { get; } = [];
        public List<(int A, int B, int C)> Triangles { get; } = [];
        public int Sections { get; private init; }
        private int samples;
        private int[,,,] index = new int[0, 0, 0, 0];   // [half, surface, section, sample]

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SurfaceView, Topology> Cache = new();

        /// <summary>The topology of one mesh, built once per <see cref="SurfaceView"/> instance (orbit redraws reuse it).</summary>
        public static Topology Of(SurfaceView surface) => Cache.GetValue(surface, Build);

        private static Topology Build(SurfaceView surface)
        {
            int sections = surface.Sections.Count, samples = surface.Sections[0].Upper.Count;
            var mesh = new Topology { Sections = sections, samples = samples, index = new int[2, 2, sections, samples] };
            for (int half = 0; half < 2; half++)
                for (int row = 0; row < sections; row++)
                {
                    var section = surface.Sections[row];
                    bool trailingShared = section.Upper[samples - 1] == section.Lower[samples - 1];
                    for (int side = 0; side < 2; side++)
                        for (int sample = 0; sample < samples; sample++)
                        {
                            var starboard = (side == 0 ? section.Upper : section.Lower)[sample];
                            if (side == 1 && (sample == 0 || sample == samples - 1 && trailingShared))
                                mesh.index[half, side, row, sample] = mesh.index[half, 0, row, sample];
                            else if (half == 1 && starboard.Y == 0)
                                mesh.index[half, side, row, sample] = mesh.index[0, side, row, sample];
                            else
                            {
                                mesh.index[half, side, row, sample] = mesh.Positions.Count;
                                mesh.Positions.Add(half == 0 ? starboard : starboard.Port());
                            }
                        }
                }
            for (int half = 0; half < 2; half++)
                for (int side = 0; side < 2; side++)
                {
                    bool reverse = (half == 1) != (side == 1);   // lower and port each reverse; both reverse back
                    for (int row = 0; row + 1 < sections; row++)
                        for (int sample = 0; sample + 1 < samples; sample++)
                        {
                            int a = mesh.index[half, side, row, sample], b = mesh.index[half, side, row, sample + 1];
                            int c = mesh.index[half, side, row + 1, sample], d = mesh.index[half, side, row + 1, sample + 1];
                            mesh.Add(a, b, c, reverse);
                            mesh.Add(b, d, c, reverse);
                        }
                }
            return mesh;
        }

        private void Add(int a, int b, int c, bool reverse)
        {
            if (a == b || b == c || a == c) return;
            Triangles.Add(reverse ? (a, c, b) : (a, b, c));
        }

        /// <summary>The closed outline of each listed section, on both halves.</summary>
        public IEnumerable<(int, int)> SectionEdges(IEnumerable<int> rows)
        {
            foreach (int row in rows)
                for (int half = 0; half < 2; half++)
                    for (int side = 0; side < 2; side++)
                        for (int sample = 0; sample + 1 < samples; sample++)
                            yield return (index[half, side, row, sample], index[half, side, row, sample + 1]);
        }

        /// <summary>The leading edge, both trailing-edge lines and the tip section, on both halves.</summary>
        public IEnumerable<(int, int)> Outline()
        {
            for (int half = 0; half < 2; half++)
            {
                for (int row = 0; row + 1 < Sections; row++)
                {
                    yield return (index[half, 0, row, 0], index[half, 0, row + 1, 0]);
                    yield return (index[half, 0, row, samples - 1], index[half, 0, row + 1, samples - 1]);
                    yield return (index[half, 1, row, samples - 1], index[half, 1, row + 1, samples - 1]);
                }
            }
            foreach (var edge in SectionEdges([Sections - 1])) yield return edge;
        }
    }
}
