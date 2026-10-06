using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The Units switch (Ruling 115; GEO-09, ANA-18): View ▸ Units ▸ Metric / Imperial and the status-bar item. Ring: the controller
/// checks all run at readiness (0.4-1.1 s each): the fast ring has no room, so the one controller-level check sits here too.
/// The checks find the parts by name and id, so each one ran red on the tree before the switch existed.
/// </summary>
public static class UnitsSwitchTests
{
    private const string Metric = "view.units-metric", Imperial = "view.units-imperial";

    private static void RegisterControllerCheck()
    {
        DesktopChecks.Check("Units_Switch_ChangesForceAndSpeed_KeysAndStoredValuesUnchanged", () =>
        {
            using var controller = Evaluated();
            VerifySwitch(controller, units => controller.AnalysisUnits = units);
        });
    }

    private static WorkbenchController Evaluated()
    {
        var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(
            Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null }));
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, null), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        return controller;
    }

    /// <summary>GEO-09 / ANA-18: <paramref name="choose"/> switches the units; force and speed rows convert, nothing else moves.</summary>
    private static void VerifySwitch(WorkbenchController controller, Action<Units> choose)
    {
        {
            var metric = Rows(controller);
            string key = controller.AnalysisView.RunKey ?? throw new Exception("no run key");
            double speed = controller.AnalysisOperatingPoint.Speed;
            if (!metric.Any(row => row.Unit == "N") || !metric.Any(row => row.Unit == "m/s"))
                throw new Exception("the Metric fixture shows no N and m/s rows");
            choose(Units.Imperial);
            var imperial = Rows(controller);
            if (imperial.Count != metric.Count) throw new Exception($"{metric.Count} rows became {imperial.Count}");
            int converted = 0;
            for (int i = 0; i < metric.Count; i++)
            {
                var (m, p) = (metric[i], imperial[i]);
                double factor = m.Unit == "N" ? 4.4482216152605 : m.Unit == "m/s" ? 1.9438444924406 : 0;
                if (factor == 0)
                {
                    if (m != p) throw new Exception($"{m.Label} changed: {m} -> {p}");
                    continue;
                }
                string unit = m.Unit == "N" ? "lbf" : "kn";
                if (p.Unit != unit || m.Label != p.Label) throw new Exception($"{m.Label}: {m.Unit} -> {p.Unit}");
                if (double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double was) &&
                    double.TryParse(p.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double now))
                {
                    bool metricIsNewtons = m.Unit == "N";
                    double expected = metricIsNewtons ? was / factor : was * factor;
                    if (Math.Abs(expected - now) > 0.011 + Math.Abs(expected) * 1e-3) throw new Exception($"{m.Label}: {m.Value} {m.Unit} -> {p.Value} {p.Unit}");
                }
                converted++;
            }
            if (converted == 0) throw new Exception("no row was converted");
            if (controller.AnalysisView.RunKey != key || controller.AnalysisOperatingPoint.Speed != speed)
                throw new Exception($"run key '{key}' or stored speed {speed} changed: '{controller.AnalysisView.RunKey}' {controller.AnalysisOperatingPoint.Speed}");
            choose(Units.Metric);
            if (!Rows(controller).SequenceEqual(metric)) throw new Exception("Metric did not return the Metric rows");
        }
    }

    public static void RunReadiness()
    {
        RegisterControllerCheck();
        DesktopChecks.Check("Units_Persist_ChoiceSurvivesShellRestart_TextSizeKept", () =>
        {
            string root = Root();
            Restart(root, (host, controller) =>
            {
                Pump(host.UnitsLoaded);
                Pump(host.TextSizeLoaded);
                host.SetTextScale(1.5);
                Pump(host.TextSizeSaved);
                Pump(host.RunCommand(Imperial));
                Pump(host.UnitsSaved);
            });
            Restart(root, (host, controller) =>
            {
                Pump(host.UnitsLoaded);
                Pump(host.TextSizeLoaded);
                if (controller.AnalysisUnits != Units.Imperial || (string?)Need<Button>(host.StatusStrip, "UnitsButton").Content != "Imperial")
                    throw new Exception($"after a restart: {controller.AnalysisUnits}");
                if (Math.Abs(host.TextScale - 1.5) > 1e-9) throw new Exception("units save lost the Text size: " + host.TextScale);
                Pump(host.RunCommand(Metric));
                Pump(host.UnitsSaved);
            });
            Restart(root, (host, controller) =>
            {
                Pump(host.UnitsLoaded);
                if (controller.AnalysisUnits != Units.Metric) throw new Exception("Metric was not kept");
            });
        });

        DesktopChecks.Check("Units_OldPreferenceFileWithoutKey_LoadsMetric", () =>
        {
            string root = Root();
            WriteDisplay(root, "{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":125}");
            Restart(root, (host, controller) =>
            {
                Pump(host.UnitsLoaded);
                Pump(host.TextSizeLoaded);
                if (controller.AnalysisUnits != Units.Metric || Math.Abs(host.TextScale - 1.25) > 1e-9)
                    throw new Exception($"{controller.AnalysisUnits}, text {host.TextScale}");
            });
        });

        DesktopChecks.Check("Units_UnknownValue_FallsBackToMetric_FileNeverRewritten", () =>
        {
            string root = Root();
            const string bad = "{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":150,\"units\":\"furlongs\"}";
            WriteDisplay(root, bad);
            Restart(root, (host, controller) =>
            {
                Pump(host.UnitsLoaded);
                if (controller.AnalysisUnits != Units.Metric || (string?)Need<Button>(host.StatusStrip, "UnitsButton").Content != "Metric")
                    throw new Exception("an unknown value did not read Metric");
                Pump(host.RunCommand(Imperial));
                Pump(host.UnitsSaved);
                if (controller.AnalysisUnits != Units.Imperial) throw new Exception("the session choice was refused");
                if (host.StatusStrip.Text.Contains("session only", StringComparison.OrdinalIgnoreCase)) throw new Exception("a units save wrote Text size wording");
            });
            if (File.ReadAllText(Path.Combine(root, "display", "display.json")) != bad) throw new Exception("the unreadable file was rewritten");
        });
        DesktopChecks.Check("Units_MenuAndItemRoutes_ConvertTheAnalysisRows", () =>
        {
            using var controller = Evaluated();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                VerifySwitch(controller, units => Pump(host.RunCommand(units == Units.Imperial ? Imperial : Metric)));
                var button = Need<Button>(host.StatusStrip, "UnitsButton");
                Click(button);
                if (controller.AnalysisUnits != Units.Imperial) throw new Exception("the item did not switch the controller");
            }
            finally { window.Close(); }
        });

        Pane("Units_MenuItems_ExistAreCheckedByState_AndToggle", (controller, host, window) =>
        {
            var menu = Cfd(NativeMenuBuilder.BuildMenu(window));
            var units = menu.Single(item => item.Header == "View").Menu!.Items.OfType<NativeMenuItem>()
                .SingleOrDefault(item => item.Header == "Units") ?? throw new Exception("View has no Units submenu");
            var items = units.Menu!.Items.OfType<NativeMenuItem>().ToList();
            if (!items.Select(item => item.Header).SequenceEqual(["Metric", "Imperial"]))
                throw new Exception("Units items: " + string.Join(", ", items.Select(item => item.Header)));
            RequireChecked(items, true, false, "start");
            items[1].Command!.Execute(null);
            RequireChecked(items, false, true, "after Imperial");
            if (controller.AnalysisUnits != Units.Imperial) throw new Exception("the Imperial item did not set Imperial");
            items[0].Command!.Execute(null);
            RequireChecked(items, true, false, "after Metric");
            if (controller.AnalysisUnits != Units.Metric) throw new Exception("the Metric item did not set Metric");
            controller.AnalysisUnits = Units.Imperial;
            RequireChecked(items, false, true, "a change from elsewhere");
        });

        Pane("Units_StatusItem_TogglesAndShowsState", (controller, host, window) =>
        {
            var button = Need<Button>(host.StatusStrip, "UnitsButton");
            if ((string?)button.Content != "Metric" || !button.IsEffectivelyVisible) throw new Exception($"item reads '{button.Content}'");
            Capture(host.StatusStrip, "status-metric");
            Click(button);
            Capture(host.StatusStrip, "status-imperial");
            if ((string?)button.Content != "Imperial" || controller.AnalysisUnits != Units.Imperial)
                throw new Exception($"after a click: '{button.Content}', {controller.AnalysisUnits}");
            Click(button);
            if ((string?)button.Content != "Metric" || controller.AnalysisUnits != Units.Metric) throw new Exception("a second click did not return to Metric");
            _ = host.RunCommand(Imperial);
            if ((string?)button.Content != "Imperial") throw new Exception("the menu route did not update the item");
        });

        Pane("Units_KeyboardAccess_PaletteRowsAndFocusedItemTakeSpaceAndEnter", (controller, host, window) =>
        {
            var palette = CommandTable.PaletteEntries().Where(entry => entry.Menu == "Units").Select(entry => entry.Id).ToList();
            if (!palette.SequenceEqual([Metric, Imperial])) throw new Exception("palette rows: " + string.Join(", ", palette));
            var button = Need<Button>(host.StatusStrip, "UnitsButton");
            if (!button.Focusable || !button.IsTabStop) throw new Exception("the status item is not a tab stop");
            foreach (var pressed in new[] { Avalonia.Input.Key.Space, Avalonia.Input.Key.Enter })
            {
                var before = controller.AnalysisUnits;
                button.Focus(NavigationMethod.Tab);
                button.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = pressed, Source = button });
                button.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = pressed, Source = button });
                if (controller.AnalysisUnits == before) throw new Exception($"{pressed} on the focused item did not toggle");
            }
        });
    }

    /// <summary>With CFDW_UNITS_CAPTURE_DIR set, saves the control's pixels there as a PNG (the track's evidence; off in the rings).</summary>
    private static void Capture(Control control, string name)
    {
        if (Environment.GetEnvironmentVariable("CFDW_UNITS_CAPTURE_DIR") is not { Length: > 0 } dir) return;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        var size = new Avalonia.PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height));
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
        bitmap.Render(control);
        bitmap.Save(Path.Combine(dir, name + ".png"));
    }

    // The store refuses a symlinked root and macOS /var is a symlink, so the root is spelled through /private there.
    private static string Root()
    {
        string path = Directory.CreateTempSubdirectory("units-").FullName;
        return OperatingSystem.IsMacOS() && path.StartsWith("/var/", StringComparison.Ordinal) ? "/private" + path : path;
    }

    private static void WriteDisplay(string root, string json)
    {
        Directory.CreateDirectory(Path.Combine(root, "display"));
        File.WriteAllText(Path.Combine(root, "display", "display.json"), json);
    }

    /// <summary>One shell run on a fresh controller and a fresh store over <paramref name="root"/>: a restart.</summary>
    private static void Restart(string root, Action<ShellHost, WorkbenchController> body)
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller, new CfdWorkbench.Persistence.PreferenceStore(root, () => new CfdWorkbench.Persistence.ProjectStore()));
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            body(host, controller);
        }
        finally { window.Close(); }
    }

    private static List<ResultRow> Rows(WorkbenchController controller) =>
        controller.AnalysisView.Groups.SelectMany(group => group.Rows).ToList();

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    private static List<NativeMenuItem> Cfd(NativeMenu menu) => menu.Items.OfType<NativeMenuItem>().ToList();

    private static void RequireChecked(List<NativeMenuItem> items, bool metric, bool imperial, string when)
    {
        if (items[0].IsChecked != metric || items[1].IsChecked != imperial)
            throw new Exception($"{when}: Metric {items[0].IsChecked}, Imperial {items[1].IsChecked}");
    }
}
