using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>Side and Front free-surface elevation and per-station depth margin from the selected run.</summary>
public static class ElevationDepthLayer
{
    public sealed record DepthMark(double Y, double Margin, double? Elevation);

    public static IReadOnlyList<DepthMark> Build(LayerData? layer)
    {
        if (layer is not { Id: "depth-band", Visible: true }) return [];
        return layer.Samples.Where(s => s.Value is { } value && double.IsFinite(value) && double.IsFinite(s.Y))
            .Select(s => new DepthMark(s.Y, s.Value!.Value, s.Elevation)).ToArray();
    }

    public static void Draw(DrawingContext context, LayerData? layer, ViewCamera camera, SurfaceView surface,
        Rect band, bool front, IBrush ink, IBrush mute, IBrush soft)
    {
        var marks = Build(layer);
        if (marks.Count == 0 || marks[0].Elevation is not { } elevation) return;
        double z = elevation + marks[0].Margin;
        Point ToScreen(double x, double y, double height) => camera.Project(new Point3(x, y, height), band.Size);
        using (context.PushClip(band))
        {
            var from = front ? ToScreen(camera.Target.X, -surface.MaximumY, z) : ToScreen(surface.MinimumX, 0, z);
            var to = front ? ToScreen(camera.Target.X, surface.MaximumY, z) : ToScreen(surface.MaximumX, 0, z);
            context.DrawLine(new Pen(mute, 1, new DashStyle([5, 4], 0)), from, to);
            var tip = marks.MaxBy(m => m.Y);
            if (tip is not null)
            {
                double x = front ? camera.Target.X : surface.MaximumX;
                double y = front ? tip.Y : 0;
                var bottom = ToScreen(x, y, tip.Elevation ?? 0);
                var top = ToScreen(x, y, z);
                context.DrawLine(new Pen(ink, 1, new DashStyle([2, 3], 0)), bottom, top);
                Text(context, "tip depth " + tip.Margin.ToString("0.###", CultureInfo.InvariantCulture) + " m",
                    top + new Vector(6, 3), ink, soft);
            }
        }
    }

    private static void Text(DrawingContext context, string value, Point position, IBrush ink, IBrush soft)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 11, ink);
        context.DrawRectangle(soft, null, new Rect(position.X - 4, position.Y - 2, text.Width + 8, text.Height + 4));
        context.DrawText(text, position);
    }
}
