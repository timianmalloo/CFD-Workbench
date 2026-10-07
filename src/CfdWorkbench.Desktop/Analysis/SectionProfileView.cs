using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Section document's profile on the dark viewport (approved mockup, DX state 5): the closed outline, each panel coloured
/// by its own Cp on the diverging vik ramp pinned at Cp 0, the Cp_min marker, and the caption, estimator and legend plates.
/// The panel colours and counts of the last render are kept for the checks (<see cref="Segments"/>).
/// </summary>
public sealed class SectionProfileView : Control
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly string[] StopKeys = ["VikNeg2Color", "VikNeg1Color", "VikZeroColor", "VikPos1Color", "VikPos2Color"];
    private static readonly Color[] StopFallback = [Color.Parse("#001261"), Color.Parse("#2d7ba5"), Color.Parse("#ebe6e2"), Color.Parse("#c37243"), Color.Parse("#590008")];
    private SectionProfile? model;
    private Color[] stops = StopFallback;

    public SectionProfile? Model
    {
        get => model;
        set
        {
            model = value;
            AutomationProperties.SetName(this, value is null ? "Section profile" :
                value.Caption + ", pressure coefficient on the profile; Cp_min " + Num(value.CpMinPanel.Cp) + " at x/c " +
                value.CpMinPanel.X.ToString("0.000", Inv) + " on the " + value.Side + " surface");
            InvalidateVisual();
        }
    }

    /// <summary>One entry per drawn outline segment: its Cp and the colour it was drawn in.</summary>
    public IReadOnlyList<(double Cp, Color Colour)> Segments { get; private set; } = [];

    /// <summary>The ramp's half-extent of the last render: max(|Cp low|, Cp high); Cp 0 sits at the ramp's centre.</summary>
    public double Range { get; private set; }

    /// <summary>Where a Cp falls on the ramp, 0 to 1, 0.5 at Cp 0.</summary>
    public static double RampPosition(double cp, double range) => Math.Clamp(0.5 + cp / (2 * range), 0, 1);

    public Color Vik(double t)
    {
        double p = Math.Clamp(t, 0, 1) * (stops.Length - 1);
        int i = Math.Min(stops.Length - 2, (int)Math.Floor(p));
        double w = p - i;
        Color a = stops[i], b = stops[i + 1];
        byte Mix(byte x, byte y) => (byte)Math.Round(x + (y - x) * w);
        return Color.FromRgb(Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
    }

    private IBrush Resource(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush brush ? brush : fallback;

    private static string Num(double value) => value.ToString("0.00", Inv).Replace('-', '−');

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        Segments = [];
        if (model is not { } profile || Bounds.Width < 120 || Bounds.Height < 100) return;
        stops = StopKeys.Select((key, i) => this.TryFindResource(key, ActualThemeVariant, out object? v) && v is Color c ? c : StopFallback[i]).ToArray();
        IBrush viewport = Resource("ViewportBrush", Brushes.Black), grid = Resource("ViewportGridBrush", Brushes.DimGray),
            ink = Resource("ViewportInkBrush", Brushes.White), mute = Resource("PlanMuteBrush", Brushes.LightGray),
            soft = Resource("PlanSoftBrush", Brushes.DarkSlateGray), foil = Resource("FoilBrush", Brushes.CadetBlue);
        double w = Bounds.Width, h = Bounds.Height;
        context.DrawRectangle(viewport, null, new Rect(0, 0, w, h));

        double range = Math.Max(Math.Max(-profile.CpLow, profile.CpHigh), 1e-9);
        Range = range;
        double s = Math.Max(40, w - 110), ox = (w - s) / 2, oy = h / 2 - 6;
        Point P(double x, double z) => new(ox + x * s, oy - z * s);
        context.DrawLine(new Pen(grid, 1, new DashStyle([4, 4], 0)), P(-0.02, 0), P(1.02, 0));

        var fill = new StreamGeometry();
        using (StreamGeometryContext g = fill.Open())
        {
            g.BeginFigure(P(profile.Outline[0].X, profile.Outline[0].Z), true);
            foreach (PanelCp p in profile.Outline.Skip(1)) g.LineTo(P(p.X, p.Z));
            g.EndFigure(true);
        }
        context.DrawGeometry(new SolidColorBrush(((ISolidColorBrush)foil).Color, 0.35), null, fill);

        var segments = new List<(double Cp, Color Colour)>();
        for (int i = 0; i < profile.Outline.Count; i++)
        {
            PanelCp a = profile.Outline[i], b = profile.Outline[(i + 1) % profile.Outline.Count];
            double cp = (a.Cp + b.Cp) / 2;
            Color colour = Vik(RampPosition(cp, range));
            segments.Add((cp, colour));
            context.DrawLine(new Pen(new SolidColorBrush(colour), 5, lineCap: PenLineCap.Round), P(a.X, a.Z), P(b.X, b.Z));
        }
        Segments = segments;

        Point marker = P(profile.CpMinPanel.X, profile.CpMinPanel.Z);
        context.DrawEllipse(null, new Pen(ink, 2), marker, 7, 7);
        string markerText = "Cp_min " + Num(profile.CpMinPanel.Cp) + " · x/c " + profile.CpMinPanel.X.ToString("0.000", Inv) + " · " + profile.Side;
        double markerWidth = Text(markerText, ink).Width + 8;
        Plate(context, markerText, new Point(Math.Clamp(marker.X + 12, 4, Math.Max(4, w - markerWidth - 4)), marker.Y + (profile.Side == "lower" ? 30 : -18)), ink, soft);
        Plate(context, "x/c 0 → 1", new Point(ox, oy + 0.1 * s + 20 > h - 40 ? h - 40 : oy + 0.1 * s + 20), mute, soft);

        Rect caption = Plate(context, profile.Caption, new Point(8, 22), ink, soft);
        double tierWidth = Text(profile.Tier, ink).Width + 8;
        bool beside = w - 8 - tierWidth > caption.Right + 8;
        Plate(context, profile.Tier, new Point(beside ? w - 8 - tierWidth + 4 : 8, beside ? 22 : 46), ink, soft);

        // Legend plate, bottom right: title, the ramp bar (extent ±range, 0 at the centre), its ends.
        string title = "Cp · vik pinned at 0 · " + Num(profile.CpLow) + " to +" + Num(profile.CpHigh);
        double barWidth = Math.Max(150, Text(title, ink).Width), left = w - 8 - barWidth - 8;
        context.DrawRectangle(soft, null, new Rect(left - 4, h - 56, barWidth + 16, 50), 3);
        context.DrawText(Text(title, ink), new Point(left + 4, h - 54));
        var bar = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
        for (int i = 0; i < stops.Length; i++) bar.GradientStops.Add(new GradientStop(stops[i], i / (double)(stops.Length - 1)));
        context.DrawRectangle(bar, null, new Rect(left + 4, h - 36, barWidth, 8));
        FormattedText lowEnd = Text(Num(-range), mute), zero = Text("0", mute), highEnd = Text(Num(range), mute);
        context.DrawText(lowEnd, new Point(left + 4, h - 24));
        context.DrawText(zero, new Point(left + 4 + (barWidth - zero.Width) / 2, h - 24));
        context.DrawText(highEnd, new Point(left + 4 + barWidth - highEnd.Width, h - 24));

        if (profile.Cavitation is { } line)
        {
            double lineWidth = Text(line, ink).Width + 8;
            Plate(context, line, new Point(8, left - 4 < 8 + lineWidth ? h - 78 : h - 30), ink, soft);
        }
    }

    // A text plate with its top-left at (x, y - 12), the mockup's .plate; returns its rectangle.
    private static Rect Plate(DrawingContext context, string text, Point at, IBrush ink, IBrush soft)
    {
        FormattedText t = Text(text, ink);
        var rect = new Rect(at.X, at.Y - 12, t.Width + 8, 16);
        context.DrawRectangle(soft, null, rect, 2);
        context.DrawText(t, new Point(at.X + 4, at.Y - 11));
        return rect;
    }

    private static FormattedText Text(string text, IBrush brush) =>
        new(text, Inv, FlowDirection.LeftToRight, Typeface.Default, 11, brush);
}
