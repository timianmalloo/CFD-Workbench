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

    private static double Scale(Visual visual) => TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1;
}
