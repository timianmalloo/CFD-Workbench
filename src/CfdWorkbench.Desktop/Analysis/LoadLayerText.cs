using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace CfdWorkbench.Desktop;

public static class LoadLayerText
{
    private const double Edge = 8, MinWidth = 160, Pad = 4;

    internal static void Text(DrawingContext context, string value, Point position, IBrush ink, IBrush soft) =>
        Plate(context, value, position, ink, soft, double.PositiveInfinity);

    /// <summary>A label plate that wraps at <paramref name="maxWidth"/> instead of running off the view; returns its rectangle.</summary>
    internal static Rect Plate(DrawingContext context, string value, Point position, IBrush ink, IBrush soft, double maxWidth)
    {
        var text = Format(value, ink, maxWidth);
        var plate = new Rect(position.X - Pad, position.Y - 2, text.Width + 2 * Pad, text.Height + 4);
        context.DrawRectangle(soft, null, plate);
        context.DrawText(text, position);
        return plate;
    }

    /// <summary>A label at <paramref name="position"/> kept inside <paramref name="viewport"/>: it wraps in the width left to its right,
    /// and slides left when that width is under a readable minimum.</summary>
    internal static void TextWithin(DrawingContext context, string value, Point position, Size viewport, IBrush ink, IBrush soft)
    {
        var (x, width) = Within(position.X, viewport.Width);
        Plate(context, value, new Point(x, position.Y), ink, soft, width);
    }

    /// <summary>The left edge and wrap width of a plate that wants to start at <paramref name="x"/> in a view <paramref name="viewportWidth"/> wide.</summary>
    public static (double X, double Width) Within(double x, double viewportWidth)
    {
        if (viewportWidth - x - Edge < MinWidth) x = Math.Max(Edge, viewportWidth - Edge - MinWidth);
        return (x, viewportWidth - x - Edge);
    }

    /// <summary>A label whose bottom-left corner sits at <paramref name="bottomLeft"/>, wrapped at <paramref name="maxWidth"/>.</summary>
    internal static Rect PlateAbove(DrawingContext context, string value, Point bottomLeft, IBrush ink, IBrush soft, double maxWidth)
    {
        var text = Format(value, ink, maxWidth);
        return Plate(context, value, new Point(bottomLeft.X, bottomLeft.Y - text.Height), ink, soft, maxWidth);
    }

    internal static FormattedText Format(string value, IBrush ink, double maxWidth)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 11, ink);
        if (double.IsFinite(maxWidth)) text.MaxTextWidth = Math.Max(40, maxWidth);
        return text;
    }
}
