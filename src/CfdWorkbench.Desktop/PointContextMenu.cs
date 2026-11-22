using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop;

/// <summary>
/// Shared point context menu (F-42) used by PlanCanvas, ElevationView, and BrowserPane.
/// Every action is gated via <see cref="ShellHost.CanRun"/> and executed asynchronously with a pending guard (F-21).
/// </summary>
internal static class PointContextMenu
{
    public static IReadOnlyList<Control> BuildItems(ShellHost host, PointRef reference, Func<Func<Task>, Task>? runGuarded = null,
        bool sentenceCase = false, Action? beforeRun = null)
    {
        bool busy = false;

        async Task RunAsync(string id)
        {
            if (busy) return;
            busy = true;
            try
            {
                beforeRun?.Invoke();
                if (runGuarded is not null) await runGuarded(() => host.RunCommand(id));
                else await host.RunCommand(id);
            }
            finally { busy = false; }
        }

        MenuItem Row(string header, string id)
        {
            var item = new MenuItem { Header = header, IsEnabled = host.CanRun(id) };
            item.Click += (_, _) => _ = RunAsync(id);
            return item;
        }

        MenuItem[] tangents =
        [
            Row("Smooth", "point.tangent-smooth"),
            Row("Symmetric", "point.tangent-symmetric"),
            Row("Corner", "point.tangent-corner")
        ];
        var tangent = new MenuItem
        {
            Header = "Tangent",
            ItemsSource = tangents,
            IsEnabled = tangents.Any(item => item.IsEnabled)
        };
        string menuName = PropertiesView.Curves.TryGetValue(reference.Curve, out var curveRows)
            ? curveRows.MenuName
            : reference.Curve;
        var items = new List<Control>
        {
            Row(sentenceCase ? "Make anchor" : "Make Anchor Point", "point.make-anchor"),
            Row(sentenceCase ? "Make control" : "Make Control Point", "point.make-control"),
            tangent,
            new Separator(),
            Row(sentenceCase ? "Remove point" : "Remove Point", "point.remove")
        };
        if (!host.CanRun("point.remove") && host.PointCommandReason("point.remove") is { } reason)
            items.Add(new TextBlock { Text = reason, TextWrapping = TextWrapping.Wrap, MaxWidth = 220 });
        items.Add(Row($"Rebuild {menuName}…", "point.rebuild"));
        items.Add(new Separator());
        items.Add(Row("Fit", "view.fit"));
        return items;
    }

    public static ContextMenu Build(ShellHost host, PointRef reference, Func<Func<Task>, Task>? runGuarded = null) =>
        new() { ItemsSource = BuildItems(host, reference, runGuarded) };

    public static void Open(Control anchor, ShellHost host, PointRef reference, Func<Func<Task>, Task>? runGuarded = null)
    {
        var menu = Build(host, reference, runGuarded);
        menu.Closed += (_, _) => { if (ReferenceEquals(anchor.ContextMenu, menu)) anchor.ContextMenu = null; };
        anchor.ContextMenu = menu;
        menu.Open(anchor);
    }
}

/// <summary>
/// Shared CAD canvas typography lookup (F-37): resolves PropFontFamily and PropFontSize from theme resources,
/// guaranteeing every canvas draw stays >= 11 px and never hardcodes "Inter" or FontFamily.Default.
/// </summary>
internal static class CanvasTypography
{
    public const double MinimumFontSize = 11.0;

    public static FontFamily ResolveFontFamily(Control control)
    {
        if (control.TryFindResource("PropFontFamily", control.ActualThemeVariant, out var local) && local is FontFamily lf)
            return lf;
        if (Application.Current is not null &&
            Application.Current.TryFindResource("PropFontFamily", control.ActualThemeVariant, out var app) && app is FontFamily af)
            return af;
        return FontFamily.Default;
    }

    public static double ResolveFontSize(Control control, double scaleRatio = 1.0)
    {
        double baseSize = MinimumFontSize;
        if (control.TryFindResource("PropFontSize", control.ActualThemeVariant, out var local) && local is double ld && ld >= MinimumFontSize)
            baseSize = ld;
        else if (Application.Current is not null &&
                 Application.Current.TryFindResource("PropFontSize", control.ActualThemeVariant, out var app) && app is double ad && ad >= MinimumFontSize)
            baseSize = ad;
        return Math.Max(MinimumFontSize, Math.Round(baseSize * scaleRatio, 2));
    }

    public static Typeface ResolveTypeface(Control control, FontWeight weight = FontWeight.Normal, FontStyle style = FontStyle.Normal) =>
        new(ResolveFontFamily(control), style, weight);
}
