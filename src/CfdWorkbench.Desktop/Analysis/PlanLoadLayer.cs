using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using static CfdWorkbench.Desktop.LoadLayerText;

namespace CfdWorkbench.Desktop;

/// <summary>The Plan's selected-run Γ strips. Scene data is shared by drawing and the Desktop ring.</summary>
public static class PlanLoadLayer
{
    public sealed record Strip(double Y, double Low, double High, double Gamma, bool Outside, bool DashedOutline);
    public sealed record Scene(IReadOnlyList<Strip> Strips, int OutsideCount, string? CountText);

    private static IBrush[]? ramp;

    public static Scene Build(LayerData? layer)
    {
        if (layer is not { Id: "plan-gamma", Visible: true }) return new Scene([], 0, null);
        var samples = layer.Samples.Where(s => s.Value is { } value && double.IsFinite(value) && double.IsFinite(s.Y))
            .OrderBy(s => s.Y).ToArray();
        var strips = new List<Strip>(samples.Length);
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            double low = sample.YLow ?? (i == 0 ? sample.Y - (samples.Length > 1 ? samples[1].Y - sample.Y : 0) / 2
                : (samples[i - 1].Y + sample.Y) / 2);
            double high = sample.YHigh ?? (i == samples.Length - 1 ? sample.Y + (samples.Length > 1 ? sample.Y - samples[i - 1].Y : 0) / 2
                : (sample.Y + samples[i + 1].Y) / 2);
            if (!(low < high)) continue;
            strips.Add(new Strip(sample.Y, low, high, sample.Value!.Value, sample.Outside, sample.Outside));
        }
        int outside = strips.Count(s => s.Outside);
        return new Scene(strips, outside, outside == 0 ? null :
            $"dashed outline: {outside} {(outside == 1 ? "strip" : "strips")} outside the method envelope");
    }

    public static void Draw(DrawingContext context, CurvePointLayer map, PlanformView plan, LayerData layer,
        Size viewport, IBrush ink, IBrush soft, IBrush warning, Control resourceScope)
    {
        var scene = Build(layer);
        if (scene.Strips.Count == 0) return;
        var colors = ramp ??= MakeRamp(resourceScope);
        double max = scene.Strips.Max(s => Math.Abs(s.Gamma));
        foreach (var strip in scene.Strips)
        {
            double left = Math.Clamp(strip.Low, -plan.HalfSpanMeters, plan.HalfSpanMeters);
            double right = Math.Clamp(strip.High, -plan.HalfSpanMeters, plan.HalfSpanMeters);
            if (left >= right) continue;
            var points = new[]
            {
                map.ToScreen(left, At(plan.Leading.Samples, Math.Abs(left))),
                map.ToScreen(right, At(plan.Leading.Samples, Math.Abs(right))),
                map.ToScreen(right, At(plan.Trailing.Samples, Math.Abs(right))),
                map.ToScreen(left, At(plan.Trailing.Samples, Math.Abs(left)))
            };
            var shape = new PolylineGeometry(points, isFilled: true);
            double ratio = max > 0 ? Math.Abs(strip.Gamma) / max : 0;
            context.DrawGeometry(colors[(int)Math.Round(Math.Clamp(ratio, 0, 1) * 255)],
                strip.DashedOutline ? new Pen(warning, 1.5, new DashStyle([3, 2], 0)) : null, shape);
        }
        var curve = scene.Strips.Select(s => map.ToScreen(s.Y, At(plan.Leading.Samples, Math.Abs(s.Y)))
            - new Vector(0, 10 + (max > 0 ? 54 * Math.Abs(s.Gamma) / max : 0))).ToArray();
        if (curve.Length > 1) context.DrawGeometry(null, new Pen(ink, 2), new PolylineGeometry(curve, false));
        Text(context, "Γ(y) · loading", new Point(Math.Max(8, viewport.Width / 2 - 48), Math.Max(30, curve.Min(p => p.Y) - 17)), ink, soft);
        var legend = DrawLegend(context, layer.Legend, max, viewport, ink, soft, resourceScope);
        // The outside-strip count sits on the legend, in its column, wrapped to its width, so the two plates never overlap.
        if (scene.CountText is { } count)
            LoadLayerText.PlateAbove(context, count, new Point(legend.Left + 4, legend.Top - 6), warning, soft, legend.Width - 8);
    }

    private const double RampHeight = 8, LegendBottom = 48;

    /// <summary>
    /// The legend plate at the view's bottom right (the mockup's .legend): the plate text, then the batlow ramp from the
    /// Styles brushes with its 0 and maximum ends. The text names the map and range, so colour is never the only carrier.
    /// </summary>
    private static Rect DrawLegend(DrawingContext context, string legend, double max, Size viewport, IBrush ink, IBrush soft, Control scope)
    {
        double width = Math.Min(350, viewport.Width - 16), left = viewport.Width - width - 8;
        var text = LoadLayerText.Format(legend, ink, width);
        var zero = LoadLayerText.Format("0", ink, double.PositiveInfinity);
        var top = LoadLayerText.Format(max.ToString("0.###", CultureInfo.InvariantCulture), ink, double.PositiveInfinity);
        double block = text.Height + 4 + RampHeight + 2 + zero.Height, y = Math.Max(8, viewport.Height - LegendBottom - block);
        var plate = new Rect(left - 4, y - 2, width + 8, block + 4);
        context.DrawRectangle(soft, null, plate);
        context.DrawText(text, new Point(left, y));
        double rampTop = y + text.Height + 4;
        var stops = BatlowStops(scope);
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = [.. stops.Select((color, index) => new GradientStop(color, index / (double)(stops.Count - 1)))]
        };
        context.DrawRectangle(gradient, null, new Rect(left, rampTop, width, RampHeight), 2);
        context.DrawText(zero, new Point(left, rampTop + RampHeight + 2));
        context.DrawText(top, new Point(left + width - top.Width, rampTop + RampHeight + 2));
        return plate;
    }

    /// <summary>The five batlow colours the Γ strips and the legend ramp share (Styles.axaml Batlow0..4).</summary>
    public static IReadOnlyList<Color> BatlowStops(Control scope) => [.. Enumerable.Range(0, 5).Select(index =>
    {
        string key = $"Batlow{index}Brush";
        return scope.TryFindResource(key, scope.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
            ? brush.Color : throw new InvalidOperationException($"Missing {key} in Styles.axaml.");
    })];

    private static double At(IReadOnlyList<PlanSample> points, double y)
    {
        if (points.Count == 0) return 0;
        if (y <= points[0].SpanMeters) return points[0].Ordinate;
        for (int i = 1; i < points.Count; i++)
            if (y <= points[i].SpanMeters)
            {
                double span = points[i].SpanMeters - points[i - 1].SpanMeters;
                return span <= 0 ? points[i].Ordinate : points[i - 1].Ordinate +
                    (points[i].Ordinate - points[i - 1].Ordinate) * (y - points[i - 1].SpanMeters) / span;
            }
        return points[^1].Ordinate;
    }

    private static IBrush[] MakeRamp(Control scope)
    {
        var batlow = BatlowStops(scope);
        return Enumerable.Range(0, 256).Select(index => MakeBrush(index / 255d, batlow)).ToArray();
    }

    private static IBrush MakeBrush(double t, IReadOnlyList<Color> batlow)
    {
        double scaled = Math.Clamp(t, 0, 1) * 4;
        int index = Math.Min(3, (int)scaled);
        double part = scaled - index;
        byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * part);
        var a = batlow[index]; var b = batlow[index + 1];
        return new SolidColorBrush(Color.FromRgb(Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B)));
    }

}
