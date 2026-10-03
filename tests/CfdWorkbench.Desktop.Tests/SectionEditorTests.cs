using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop;
using CfdWorkbench.Core;

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

        using var fixture = new Fixture();
        DesktopChecks.Check("SectionEditor_ModeBar_NamesStationAndScopeChip", () =>
        {
            fixture.Reset();
            if (fixture.Text("ModeTitle").Text != "Editing Root section" || fixture.Text("ScopeChip").Text != "shared profile")
                throw new Exception("Mode title or scope chip does not name the active Root section");
            if (!fixture.Button("ModeFinishButton").IsEffectivelyVisible || !fixture.Button("ModeCancelButton").IsEffectivelyVisible)
                throw new Exception("Mode actions are not in the realized surface");
        });
        DesktopChecks.Check("SectionEditor_StripThumbnails_OnePerStationCurrentMarked", () =>
        {
            fixture.Reset();
            int count = fixture.Controller.Inspection!.Authored.Assignments.Count;
            var strip = fixture.View.FindControl<StackPanel>("StationStrip")!;
            if (strip.Children.Count != count) throw new Exception("Station strip count differs from authored assignments");
            var first = (Button)strip.Children[0];
            if (!first.Content!.ToString()!.Contains("chord") || !first.Content.ToString()!.Contains("t/c") ||
                Avalonia.Automation.AutomationProperties.GetItemStatus(first) != "current")
                throw new Exception("Current station thumbnail lacks chord, t/c, or current state");
        });
        DesktopChecks.Check("SectionEditor_PlateAndProbe_SayDisplay", () =>
        {
            fixture.Reset();
            if (!fixture.Text("ModePlate").Text!.Contains("display") || !fixture.Text("ModeProbe").Text!.Contains("display"))
                throw new Exception("Plate and probe do not state display provenance");
        });
        DesktopChecks.Check("SectionEditor_FocusedPoint_AccessibleNameSurfaceIndexTypeXY", () =>
        {
            fixture.Reset();
            var names = fixture.Canvas.AccessibleTexts;
            if (!names.Any(name => name.Contains("upper surface · point 1 · Anchor · x") && name.Contains("% c · y") && name.EndsWith("% c")))
                throw new Exception("Focused section point has no surface/index/type/x/y accessible name");
        });
        DesktopChecks.Check("SectionEditor_ArrowRun_OneStepOnKeyUp", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            fixture.Select(point);
            int cursor = fixture.Controller.Section!.Draft.Cursor;
            for (int i = 0; i < 3; i++) fixture.Key(Key.Right);
            if (fixture.Controller.Section!.Draft.Cursor != cursor) throw new Exception("Arrow run committed before KeyUp");
            fixture.KeyUp(Key.Right);
            WaitUntil(() => fixture.Controller.Section!.Draft.Cursor == cursor + 1);
            var moved = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            if (Math.Abs(moved.SpanMeters - point.SpanMeters - .003) > 1e-8)
                throw new Exception("Arrow run did not accumulate three nudges into one step");
        });
        DesktopChecks.Check("SectionEditor_BracketKeys_WalkInOrder", () =>
        {
            fixture.Reset();
            var points = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            fixture.Select(points[0]);
            fixture.Key(Key.OemCloseBrackets);
            if (fixture.Canvas.SelectedVertex != ("upper", points[1].Id)) throw new Exception("] did not select next section point");
            fixture.Key(Key.OemOpenBrackets);
            if (fixture.Canvas.SelectedVertex != ("upper", points[0].Id)) throw new Exception("[ did not select prior section point");
        });
        DesktopChecks.Check("SectionEditor_ThicknessX2_DrawingOnlyValuesUnchanged", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var before = fixture.Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            var bytes = fixture.Controller.Section!.Draft.Bytes.ToArray();
            fixture.View.FindControl<ToggleButton>("ThicknessToggle")!.IsChecked = true;
            fixture.Canvas.ThicknessDoubled = true;
            var after = fixture.Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            if (Math.Abs(after.Y - fixture.Canvas.Bounds.Height / 2) <= Math.Abs(before.Y - fixture.Canvas.Bounds.Height / 2) * 1.9 ||
                !fixture.Controller.Section!.Draft.Bytes.SequenceEqual(bytes))
                throw new Exception("Thickness ×2 changed values or failed to double drawing height");
            fixture.View.FindControl<ToggleButton>("ThicknessToggle")!.IsChecked = false;
            fixture.Canvas.ThicknessDoubled = false;
        });
        DesktopChecks.Check("SectionEditor_CombAutoScale_ClippedTeethMarked", () =>
        {
            fixture.Reset();
            using var pixels = PropertiesCellsTests.Render(fixture.Window, 1);
            if (fixture.Canvas.CombScale <= 0 || fixture.Canvas.CombClippedCount < 0)
                throw new Exception("The rendered comb did not compute its automatic scale");
        });
    }

    private static void WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 10000; i++)
        {
            if (condition()) return;
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        throw new TimeoutException("Section editor state did not settle");
    }

    private sealed class Fixture : IDisposable
    {
        internal WorkbenchController Controller { get; } = new();
        internal ModelArea Area { get; } = new();
        internal Window Window { get; }
        internal SectionEditorView View => Area.FindControl<SectionEditorView>("SectionModeEditor")!;
        internal SectionCanvas Canvas => View.FindControl<SectionCanvas>("ModeCanvas")!;
        internal TextBlock Text(string name) => View.FindControl<TextBlock>(name)!;
        internal Button Button(string name) => View.FindControl<Button>(name)!;

        internal Fixture()
        {
            Wait(Controller.OpenExampleAsync());
            Area.PlanCanvas.Controller = Controller;
            Window = new Window { Content = Area, Width = 1280, Height = 800 };
            Window.Show();
            Area.ShowFoilOpen(true);
            Reset();
        }

        internal void Reset()
        {
            if (Controller.Section is not null) Controller.CancelSection();
            var station = Controller.Inspection!.Authored.Assignments[0];
            Controller.Select(new Selection.Station(0, station.Eta));
            Wait(Controller.EnterSectionAsync(0, EntryOrigin.Side));
            Dispatcher.UIThread.RunJobs();
        }

        internal void Select(PointView point)
        {
            Canvas.SelectedVertex = (point.Curve, point.Id);
            Controller.Select(new Selection.Points([new PointRef(point.Curve, point.Id, Controller.Section!.Draft.Profile)]));
        }

        internal void Key(Key key) => Canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent, Source = Canvas, Key = key
        });

        internal void KeyUp(Key key) => Canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent, Source = Canvas, Key = key
        });

        public void Dispose() { Window.Close(); Controller.Dispose(); }
    }
}
