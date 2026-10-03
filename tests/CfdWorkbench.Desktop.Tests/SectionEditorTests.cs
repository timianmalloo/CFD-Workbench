using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop;

namespace CfdWorkbench.Desktop.Tests;

// M1.2c EDT suite (docs/design/m12c-section-editor.md §12.4): registered empty by the PRE track; EDT adds the named checks.
public static class SectionEditorTests
{
    private static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Section editor setup did not complete");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        task.GetAwaiter().GetResult();
    }

    public static void Run()
    {
        DesktopChecks.Check("SectionEditor_RefusedPairedRefit_MarkerAtMaximum", () =>
        {
            var profile = new CfdWorkbench.Core.ProfileView("test", "test",
                [new("upper", "u0", 0, 0, true), new("upper", "u1", 1, 0, true)],
                [new("lower", "l0", 0, 0, true), new("lower", "l1", 1, 0, true)],
                [new(0, 0), new(1, 0)], [new(0, 0), new(1, 0)], "closed");
            var canvas = new SectionCanvas
            {
                Profile = profile, Width = 800, Height = 400,
                BackgroundBrush = Brushes.Black, FoilBrush = Brushes.White,
                StationBrush = Brushes.Gray, DangerBrush = Brushes.Red,
                RefitMarker = new Point(.47, -.07)
            };
            var window = new Window { Content = canvas, Width = 800, Height = 400 };
            window.Show();
            using var pixels = PropertiesCellsTests.Render(window, 1);
            var local = canvas.ModelToScreen(.47, -.07);
            var centre = canvas.TranslatePoint(local, window)!.Value;
            int red = 0;
            for (int y = (int)centre.Y - 7; y <= (int)centre.Y + 7; y++)
                for (int x = (int)centre.X - 7; x <= (int)centre.X + 7; x++)
                {
                    var colour = pixels.At(x, y);
                    if (colour.R > 160 && colour.G < 100 && colour.B < 100) red++;
                }
            window.Close();
            if (red < 6) throw new Exception($"No red refusal marker at the reported position: {red} pixels");
        });
        DesktopChecks.Check("SectionEditor_ReturnOnSelectedStation_ModeShown", () =>
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            var station = controller.Inspection!.Authored.Assignments[0];
            controller.Select(new Selection.Station(0, station.Eta));
            var area = new ModelArea();
            area.PlanCanvas.Controller = controller;
            var window = new Window { Content = area, Width = 1280, Height = 800 };
            window.Show();
            area.ShowFoilOpen(true);
            area.SideElevation.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Source = area.SideElevation,
                Key = Key.Return
            });
            var mode = area.GetType().GetProperty("Mode")?.GetValue(area)?.ToString();
            if (controller.Section is null || mode != "Section")
                throw new Exception($"Selected-station Return did not show section mode: {mode ?? "missing"}");
            Dispatcher.UIThread.RunJobs();
            if (!area.GetVisualDescendants().OfType<SectionCanvas>().Any(canvas => canvas.IsEffectivelyVisible && canvas.Bounds.Width > 0))
                throw new Exception("Section canvas is not visible in the model area");
            window.Close();
        });
    }
}
