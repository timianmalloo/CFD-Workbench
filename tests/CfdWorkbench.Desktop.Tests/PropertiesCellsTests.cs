using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The property sheet in structure B (docs/reviews/ui-property-grid-cells.md §5.3): the B tests, the density tests it keeps
/// and the Text size tests. Each name says what it protects; every check runs on the shell at 1280 × 800.
/// </summary>
public static class PropertiesCellsTests
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly (ThemeVariant Variant, string Name)[] Themes =
        [(ThemeVariant.Light, "light"), (ThemeVariant.Dark, "dark"), (NativeReviewThemes.HighContrast, "high-contrast")];

    public static void Run()
    {
        Pane("PropertiesPane_B_EditableValueHasDottedUnderline", (controller, host, window) =>
        {
            // CL-3, SC 1.4.1: an editable value carries a dotted underline under its text, measured on pixels at 1× and 2×
            // in three themes: dashes in the accent at ≥ 3:1 against the surface, with gaps between them.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (var (variant, name) in Themes)
            {
                window.RequestedThemeVariant = variant;
                Settle(window);
                foreach (int scale in new[] { 1, 2 })
                {
                    var measured = MeasureCue(window, Need<TextBox>(host.Properties, "PointAftInput"), scale);
                    Console.WriteLine(FormattableString.Invariant(
                        $"MEASURE underline {name} {scale}x: dashes {measured.On}/{measured.Length} px at {measured.Contrast:0.00}:1, gaps {measured.Gaps}"));
                    if (measured.Contrast < 3 || measured.On < measured.Length / 3 || measured.Gaps < measured.Length / 3)
                        failures.Add(FormattableString.Invariant($"{name} {scale}x: {measured.On} dashes, {measured.Gaps} gaps of {measured.Length} px, {measured.Contrast:0.00}:1"));
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Capture();
    }

    // ---------------- measuring ----------------

    internal readonly record struct CueMeasure(int Length, int On, int Gaps, double Contrast);

    /// <summary>Renders the window at <paramref name="scale"/> and reads the underline's pixel row against the surface beside it.</summary>
    internal static CueMeasure MeasureCue(Window window, TextBox box, int scale)
    {
        var cue = box.GetVisualDescendants().OfType<PropertiesPane.EditCue>().SingleOrDefault()
            ?? throw new InvalidOperationException("no underline part in " + box.Name);
        if (!cue.IsEffectivelyVisible || cue.Line() is not { } line) return default;
        var start = cue.TranslatePoint(line.Start, window)!.Value;
        var end = cue.TranslatePoint(line.End, window)!.Value;
        using var pixels = Render(window, scale);
        int row = (int)Math.Floor((start.Y - 0.5) * scale);
        int x0 = (int)Math.Round(start.X * scale), x1 = (int)Math.Round(end.X * scale);
        var surface = pixels.At(x0 - 3 * scale, row);
        int on = 0, gaps = 0;
        double best = 1;
        for (int x = x0; x < x1; x++)
        {
            double contrast = Contrast(pixels.At(x, row), surface);
            best = Math.Max(best, contrast);
            if (contrast >= 3) on++;
            else if (contrast < 1.2) gaps++;
        }
        return new CueMeasure(x1 - x0, on, gaps, best);
    }

    internal static Pixels Render(Visual visual, int scale)
    {
        var size = new PixelSize((int)(visual.Bounds.Width * scale), (int)(visual.Bounds.Height * scale));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        bitmap.Render(visual);
        var writeable = new WriteableBitmap(size, new Vector(96 * scale, 96 * scale), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
        using (var frame = writeable.Lock()) bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
        return new Pixels(writeable);
    }

    internal sealed class Pixels(WriteableBitmap bitmap) : IDisposable
    {
        public WriteableBitmap Bitmap { get; } = bitmap;

        public Color At(int x, int y)
        {
            using var frame = Bitmap.Lock();
            int offset = y * frame.RowBytes + x * 4;
            return Color.FromRgb(Marshal.ReadByte(frame.Address, offset + 2), Marshal.ReadByte(frame.Address, offset + 1),
                Marshal.ReadByte(frame.Address, offset));
        }

        public void Dispose() => Bitmap.Dispose();
    }

    /// <summary>WCAG 2.2 contrast ratio of two opaque colours.</summary>
    internal static double Contrast(Color a, Color b)
    {
        static double Channel(byte value)
        {
            double c = value / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    // ---------------- captures (B2-2): only when CFDW_PROPERTIES_CAPTURE names a directory ----------------

    private static void Capture()
    {
        if (Environment.GetEnvironmentVariable("CFDW_PROPERTIES_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        Pane("Capture_PropertiesPane_B", (controller, host, window) =>
        {
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id && point.Index > anchor.Index);
            var states = new (string Name, Action Enter)[]
            {
                ("anchor", () => { Select(controller, window, anchor); Need<TextBox>(host.Properties, "PointAftInput").Focus(); }),
                ("handle", () => Select(controller, window, handle)),
                ("field-error", () =>
                {
                    Select(controller, window, anchor);
                    var aft = Need<TextBox>(host.Properties, "PointAftInput");
                    aft.Focus();
                    aft.Text = "abc";
                    Key(aft, Avalonia.Input.Key.Enter);
                }),
                ("unavailable", () => { Select(controller, window, anchor); host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN }); })
            };
            foreach (var (variant, theme) in Themes)
            {
                window.RequestedThemeVariant = variant;
                foreach (var (name, enter) in states)
                {
                    enter();
                    Settle(window);
                    foreach (int scale in new[] { 1, 2 })
                    {
                        using var pixels = Render(host.Properties, scale);
                        pixels.Bitmap.Save(System.IO.Path.Combine(directory, $"b-{name}-{theme}-{scale}x.png"));
                    }
                }
            }
        });
    }
}
