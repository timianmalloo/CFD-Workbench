using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using static CfdWorkbench.Desktop.LoadLayerText;

namespace CfdWorkbench.Desktop;

/// <summary>Selected-run loads projected through the live 3D camera. Missing terms have no glyph.</summary>
public static class View3dLoadLayer
{
    public sealed record Arrow(double Y, double Magnitude, Loads.Vec Direction);
    public sealed record Scene(LayerData? Lift, IReadOnlyList<Arrow> Arrows, LayerData? Moment,
        IReadOnlyList<ElevationDepthLayer.DepthMark> Depth);

    /// <summary>The legend plate's left edge: in the caption's column, right of the triad plate, so the two never overlap.</summary>
    public static double LegendLeft => View3d.CaptionMargin.Left + 4;

    public static Scene BuildScene(AnalysisViewModel view)
    {
        var lift = view.Layers.FirstOrDefault(layer => layer.Id == "strip-lift");
        return new Scene(lift, Build(lift),
            view.Layers.FirstOrDefault(layer => layer.Id == "root-moment" && layer.Visible && layer.Samples.Any(sample => sample.Value.HasValue)),
            ElevationDepthLayer.Build(view.Layers.FirstOrDefault(layer => layer.Id == "depth-band")));
    }

    public static IReadOnlyList<Arrow> Build(LayerData? layer)
    {
        if (layer is not { Id: "strip-lift", Visible: true }) return [];
        return layer.Samples.Where(s => s.Value is { } value && double.IsFinite(value) &&
                s.Normal is { } normal && double.IsFinite(normal.X) && double.IsFinite(normal.Y) && double.IsFinite(normal.Z))
            .Select(s =>
            {
                var n = s.Normal!.Value;
                double length = Math.Sqrt(n.X * n.X + n.Y * n.Y + n.Z * n.Z);
                return length > 0 ? new Arrow(s.Y, s.Value!.Value,
                    new Loads.Vec(n.X / length, n.Y / length, n.Z / length)) : null;
            }).Where(a => a is not null).Select(a => a!).ToArray();
    }

    public static void Draw(DrawingContext context, ViewCamera camera, SurfaceView surface, Size viewport,
        AnalysisViewModel view, Scene scene, IBrush ink, IBrush mute, IBrush station, IBrush soft)
    {
        var lift = scene.Lift;
        var arrows = scene.Arrows;
        if (arrows.Count > 0)
        {
            double peak = arrows.Max(a => Math.Abs(a.Magnitude));
            double worldScale = peak > 0 ? Math.Max(.02, surface.MaximumY * .22) / peak : 0;
            foreach (var arrow in arrows)
            {
                if (Anchor(surface, arrow.Y) is not { } start) continue;
                var end = new Point3(start.X + arrow.Direction.X * arrow.Magnitude * worldScale,
                    start.Y + arrow.Direction.Y * arrow.Magnitude * worldScale,
                    start.Z + arrow.Direction.Z * arrow.Magnitude * worldScale);
                DrawArrow(context, camera.Project(start, viewport), camera.Project(end, viewport), ink, 1.5);
            }
            var total = view.Groups.FirstOrDefault(g => g.Title == "Loads")?.Rows.FirstOrDefault(r => r.Label == "Lift (Wing only)");
            if (total is { Value: not "Unavailable" })
            {
                var root = surface.Sections.MinBy(s => Math.Abs(s.Eta));
                if (root is not null)
                {
                    var le = root.Upper[0]; var te = root.Upper[^1];
                    var from = camera.Project(new Point3(le.X + .25 * (te.X - le.X), 0, le.Z), viewport);
                    var to = camera.Project(new Point3(le.X + .25 * (te.X - le.X), 0,
                        le.Z + Math.Max(.03, surface.MaximumY * .3)), viewport);
                    DrawArrow(context, from, to, station, 3);
                    TextWithin(context, "L " + total.Value + " " + total.Unit, to + new Vector(8, 4), viewport, ink, soft);
                }
            }
            // The legend plate starts right of the triad plate (the caption's column) and sits above the caption, so the two never overlap.
            double left = LegendLeft;
            PlateAbove(context, lift!.Legend, new Point(left, Math.Max(48, viewport.Height - 48)), ink, soft, viewport.Width - left - 8);
        }
        var moment = scene.Moment;
        if (moment is not null && surface.Sections.Count > 0)
        {
            var origin = camera.Project(surface.Sections.MinBy(s => Math.Abs(s.Eta))!.Upper[0], viewport);
            var arc = Enumerable.Range(0, 13).Select(i => origin + new Vector(18 * Math.Cos((.9 + .7 * i / 12) * Math.PI),
                18 * Math.Sin((.9 + .7 * i / 12) * Math.PI))).ToArray();
            context.DrawGeometry(null, new Pen(ink, 1.5), new PolylineGeometry(arc, false));
            TextWithin(context, moment.Legend, origin + new Vector(24, 26), viewport, ink, soft);
        }
        var depth = scene.Depth;
        if (depth.Count > 0 && depth[0].Elevation.HasValue)
        {
            double z = depth[0].Elevation!.Value + depth[0].Margin;
            var line = new[]
            {
                camera.Project(new Point3(surface.MinimumX, -surface.MaximumY, z), viewport),
                camera.Project(new Point3(surface.MaximumX, -surface.MaximumY, z), viewport),
                camera.Project(new Point3(surface.MaximumX, surface.MaximumY, z), viewport),
                camera.Project(new Point3(surface.MinimumX, surface.MaximumY, z), viewport),
                camera.Project(new Point3(surface.MinimumX, -surface.MaximumY, z), viewport)
            };
            context.DrawGeometry(null, new Pen(mute, 1, new DashStyle([5, 4], 0)), new PolylineGeometry(line, false));
            TextWithin(context, "free surface · " + view.Layers.First(l => l.Id == "depth-band").Legend,
                line[0] + new Vector(8, -18), viewport, mute, soft);
            var tip = depth.MaxBy(d => d.Y);
            if (tip is not null)
            {
                var foot = camera.Project(new Point3(surface.MinimumX, tip.Y, tip.Elevation ?? 0), viewport);
                var top = camera.Project(new Point3(surface.MinimumX, tip.Y, z), viewport);
                context.DrawLine(new Pen(mute, 1, new DashStyle([2, 3], 0)), foot, top);
                TextWithin(context, "tip depth " + tip.Margin.ToString("0.###", CultureInfo.InvariantCulture) + " m",
                    new Point(top.X + 6, (foot.Y + top.Y) / 2), viewport, ink, soft);
            }
        }
    }

    /// <summary>Quarter-chord glyph anchor between placed sections, clamped at the root and tip.</summary>
    public static Point3? Anchor(SurfaceView surface, double y)
    {
        if (!double.IsFinite(y)) return null;
        var sections = surface.Sections.Where(section => section.Upper.Count > 0)
            .OrderBy(section => section.Upper[0].Y).ToArray();
        if (sections.Length == 0) return null;
        double span = Math.Abs(y);
        var high = sections.FirstOrDefault(section => section.Upper[0].Y >= span) ?? sections[^1];
        int index = Array.IndexOf(sections, high);
        var low = sections[Math.Max(0, index - 1)];
        static double Quarter(PlacedSection section) => section.Upper[0].X +
            .25 * (section.Upper[^1].X - section.Upper[0].X);
        double delta = high.Upper[0].Y - low.Upper[0].Y;
        double t = delta > 0 ? Math.Clamp((span - low.Upper[0].Y) / delta, 0, 1) : 0;
        return new Point3(Quarter(low) + (Quarter(high) - Quarter(low)) * t, y,
            low.Upper[0].Z + (high.Upper[0].Z - low.Upper[0].Z) * t);
    }

    private static void DrawArrow(DrawingContext context, Point from, Point to, IBrush brush, double width)
    {
        var d = new Vector(to.X - from.X, to.Y - from.Y);
        if (d.Length < 2) return;
        context.DrawLine(new Pen(brush, width), from, to);
        var unit = d / d.Length; var normal = new Vector(-unit.Y, unit.X);
        var head = new PolylineGeometry([to, to - unit * (5 + width * 2) + normal * 3,
            to - unit * (5 + width * 2) - normal * 3], true);
        context.DrawGeometry(brush, null, head);
    }

}
