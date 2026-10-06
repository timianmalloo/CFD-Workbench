using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace CfdWorkbench.Desktop;

internal static class LoadLayerText
{
    internal static void Text(DrawingContext context, string value, Point position, IBrush ink, IBrush soft)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 11, ink);
        context.DrawRectangle(soft, null, new Rect(position.X - 4, position.Y - 2, text.Width + 8, text.Height + 4));
        context.DrawText(text, position);
    }
}
