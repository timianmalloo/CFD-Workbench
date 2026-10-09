using Avalonia;
using Avalonia.Controls;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// DPI-A: layout places edges on whole device pixels, so a DIP comparison cannot be tighter than one device pixel at a
/// fractional render scale (1 / 1.5 = 0.667 DIP). The Mac runs at 1 and 2, where the old tolerance holds exactly.
/// </summary>
public static class DevicePixel
{
    /// <summary>One device pixel in DIPs at the visual's window scale: 1 at scale 1.</summary>
    public static double Of(Visual visual) => 1 / Scale(visual);

    /// <summary>
    /// The tolerance to use in place of <paramref name="exact"/>: unchanged at an integer scale, never below one device
    /// pixel at a fractional one.
    /// </summary>
    public static double Tolerance(Visual visual, double exact) =>
        Scale(visual) % 1 == 0 ? exact : Math.Max(exact, Of(visual));

    /// <summary>
    /// <paramref name="dip"/> as layout renders it at the visual's scale: Avalonia rounds a border, padding or margin to
    /// whole device pixels, half to even, so 1 DIP is 1.333 and 3 DIP is 2.667 at 1.5.
    /// </summary>
    public static double Rounded(Visual visual, double dip) => Rounded(dip, Scale(visual));

    /// <summary>The pure form of <see cref="Rounded(Visual, double)"/> for a given scale.</summary>
    public static double Rounded(double dip, double scale) => Math.Round(dip * scale, MidpointRounding.ToEven) / scale;

    /// <summary>
    /// The colour nearest <paramref name="target"/> within <paramref name="radius"/> device pixels of the device pixel
    /// nearest <paramref name="windowDip"/>, read from a shot taken at <paramref name="scale"/> device pixels per DIP.
    /// At device resolution a 1 DIP border is whole pixels, so it is its exact colour rather than a blend across a seam.
    /// </summary>
    public static (byte R, byte G, byte B) NearestAtDevice(Func<int, int, (byte R, byte G, byte B)> rgb, Point windowDip, double scale,
        (byte R, byte G, byte B) target, int radius)
    {
        int cx = (int)Math.Round(windowDip.X * scale), cy = (int)Math.Round(windowDip.Y * scale);
        var best = rgb(cx, cy);
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var candidate = rgb(cx + dx, cy + dy);
                if (Distance(candidate, target) < Distance(best, target)) best = candidate;
            }
        return best;
    }

    /// <summary>
    /// True when <paramref name="pixel"/> holds at least half of a 1 DIP <paramref name="line"/> over <paramref name="ground"/>.
    /// A 96-dpi capture of a line at a fractional device position splits it over two pixels, and the better one holds at
    /// least half; a capture without the line reads the ground. ECR: the overlay is absent from a device-resolution
    /// capture, so the line is sampled at 96 dpi through this test, not as an exact colour.
    /// </summary>
    public static bool HoldsHalfOf((byte R, byte G, byte B) pixel, (byte R, byte G, byte B) line, (byte R, byte G, byte B) ground) =>
        Distance(pixel, line) <= Distance(ground, line) / 2 + 4;

    private static int Distance((byte R, byte G, byte B) a, (byte R, byte G, byte B) b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static double Scale(Visual visual) => TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1;
}
