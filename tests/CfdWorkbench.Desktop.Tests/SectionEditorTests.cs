using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Desktop;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

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
            if (!fixture.Text("ModePlate").Text!.Contains("display") ||
                !fixture.Text("ModeProbe").Text!.Contains("display", StringComparison.OrdinalIgnoreCase))
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
        DesktopChecks.Check("SectionEditor_ProbeFollowsPointer_PlacedMmAtStation", () =>
        {
            fixture.Reset();
            using var pointer = fixture.NewPointer();
            fixture.Move(pointer, fixture.Canvas.ModelToScreen(.42, 0));
            string text = fixture.Text("ModeProbe").Text ?? "";
            if (!text.StartsWith("Display · pointer x 42.00 %", StringComparison.Ordinal) || !text.Contains(" · at Root ") ||
                !text.Contains(" mm (") || !text.EndsWith("% t/c)", StringComparison.Ordinal))
                throw new Exception("Pointer probe did not report x, placed millimetres at Root, and display provenance: " + text);
        });
        DesktopChecks.Check("SectionCanvas_DragUpperPoint_StepAppliedInController", () =>
        {
            fixture.Reset();
            var upper = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            var point = upper[3];
            var from = fixture.Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            var to = fixture.Canvas.ModelToScreen(point.SpanMeters + .01, point.Ordinate);
            int cursor = fixture.Controller.Section!.Draft.Cursor;
            var pointer = fixture.Press(from);
            fixture.Move(pointer, to);
            fixture.Release(pointer, to);
            WaitUntil(() => fixture.Controller.Section!.Draft.Cursor == cursor + 1);
            var changedUpper = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var changedLower = fixture.Controller.SectionCurve(SurfaceSide.Lower)!.Points[3];
            if (Math.Abs(changedUpper.SpanMeters - point.SpanMeters - .01) > 1e-8 ||
                Math.Abs(changedLower.SpanMeters - changedUpper.SpanMeters) > 1e-10)
                throw new Exception("Drag did not append a paired controller step");
        });
        DesktopChecks.Check("SectionEditor_DragUnderPointer_WithinTwoPixels", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var from = fixture.Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            var to = fixture.Canvas.ModelToScreen(point.SpanMeters + .015, point.Ordinate + .01);
            var pointer = fixture.Press(from);
            fixture.Move(pointer, to);
            using (var pixels = PropertiesCellsTests.Render(fixture.Window, 1))
            {
                var centre = fixture.Canvas.TranslatePoint(to, fixture.Window)!.Value;
                int coloured = 0;
                for (int y = (int)centre.Y - 2; y <= (int)centre.Y + 2; y++)
                    for (int x = (int)centre.X - 2; x <= (int)centre.X + 2; x++)
                    {
                        var colour = pixels.At(x, y);
                        if (colour.R + colour.G + colour.B > 200) coloured++;
                    }
                if (coloured < 2) throw new Exception("Dragged glyph did not render under the pointer");
            }
            fixture.Release(pointer, to);
        });
        DesktopChecks.Check("SectionEditor_StripSwitchWithEdits_RefusedByClick", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            Wait(fixture.Controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters + .001, point.Ordinate)));
            var strip = fixture.View.FindControl<StackPanel>("StationStrip")!;
            ((Button)strip.Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            if (fixture.Controller.Section!.Draft.Assignment != 0 ||
                !fixture.Text("ModeReason").Text!.Contains("Finish or cancel"))
                throw new Exception("Dirty station switch was not refused by its strip button");
        });
        DesktopChecks.Check("SectionEditor_EscapeWithEdits_FocusCancelNothingDiscarded", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            Wait(fixture.Controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters + .001, point.Ordinate)));
            byte[] bytes = fixture.Controller.Section!.Draft.Bytes.ToArray();
            fixture.Canvas.SelectedVertex = null;
            fixture.Key(Key.Escape);
            if (fixture.Controller.Section is null || !fixture.Controller.Section.Draft.Bytes.SequenceEqual(bytes) ||
                !fixture.Button("ModeCancelButton").IsFocused ||
                !fixture.Text("ModeReason").Text!.Contains("unsaved changes"))
                throw new Exception("Escape with edits did not preserve bytes and focus Cancel with COPY-119");
        });
        DesktopChecks.Check("SectionEditor_EscapeCascade_DragPointSelectionThenLeave", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            fixture.Select(point);
            Dispatcher.UIThread.RunJobs();
            int cursor = fixture.Controller.Section!.Draft.Cursor;
            var from = fixture.Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            var pointer = fixture.Press(from);
            fixture.Move(pointer, from + new Vector(30, -20));
            if (fixture.Controller.Gesture == GestureState.Idle) throw new Exception("The drag did not start a gesture");
            fixture.Key(Key.Escape);
            WaitUntil(() => fixture.Controller.Gesture == GestureState.Idle);
            fixture.Release(pointer, from + new Vector(30, -20));
            if (fixture.Controller.Section?.Draft.Cursor != cursor || fixture.Canvas.SelectedVertex != ("upper", point.Id))
                throw new Exception("Escape during a drag did not cancel only the drag");
            fixture.Key(Key.Escape);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Canvas.SelectedVertex is not null || fixture.Controller.Selection is not Selection.Station)
                throw new Exception("Escape on a point did not step back to the station selection");
            fixture.Key(Key.Escape);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Controller.Section is not null || fixture.Area.Mode != ModelAreaMode.Views || !fixture.Area.PlanCanvas.IsEffectivelyVisible)
                throw new Exception("Escape with no edits and nothing selected did not leave to the views");

            // The handle rung: a handle steps back to its anchor (paired anchor, Ruling 60), then to nothing, then (edits) to Cancel.
            fixture.Reset();
            var target = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            Wait(fixture.Controller.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, target.Id, true)));
            Dispatcher.UIThread.RunJobs();
            var handle = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points.First(item => item.Role == PointRole.AnchorHandle);
            fixture.Select(handle);
            Dispatcher.UIThread.RunJobs();
            fixture.Key(Key.Escape);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Canvas.SelectedVertex != ("upper", handle.AnchorId!)) throw new Exception("Escape on a handle did not select its anchor");
            fixture.Key(Key.Escape);
            Dispatcher.UIThread.RunJobs();
            fixture.Key(Key.Escape);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Controller.Section is null || !fixture.Button("ModeCancelButton").IsFocused)
                throw new Exception("Escape with edits left the mode instead of focusing Cancel");
        });
        DesktopChecks.Check("SectionEditor_Rendered_AnchorSquareControlCircleNamedDiamond", () =>
        {
            var (anchor, control, nose) = fixture.ResetWithAnchor();
            using var pixels = PropertiesCellsTests.Render(fixture.Window, 1);
            var (background, foil) = (fixture.Colour("ViewportBrush"), fixture.Colour("FoilBrush"));
            // Anchor: a hollow 12 px square — background inside, outline at the edge midpoint and at the corner.
            var a = fixture.Centre(anchor);
            Expect(pixels, a, 0, 0, background, "anchor centre is hollow");
            Expect(pixels, a, 6, 0, foil, "anchor square edge");
            Expect(pixels, a, 6, 6, foil, "anchor square corner (a circle has none)");
            // Control: an 11 px filled circle — foil at the centre, nothing at the bounding-box corner.
            var c = fixture.Centre(control);
            Expect(pixels, c, 0, 0, foil, "control circle is filled");
            NotNear(pixels, c, 5, 5, foil, "control circle has no corner");
            // Named point: a 14 px diamond — outline on the 45° edge, nothing at the square corner.
            var n = fixture.Centre(nose);
            Expect(pixels, n, 3.5, -3.5, foil, "nose diamond edge");
            NotNear(pixels, n, 6, -6, foil, "nose diamond has no square corner");
        });
        DesktopChecks.Check("SectionEditor_SelectedVsUnselected_ShapeNotColourOnly", () =>
        {
            var (anchor, control, _) = fixture.ResetWithAnchor();
            var (background, foil, station) = (fixture.Colour("ViewportBrush"), fixture.Colour("FoilBrush"), fixture.Colour("StationBrush"));
            using (var unselected = PropertiesCellsTests.Render(fixture.Window, 1))
            {
                Expect(unselected, fixture.Centre(anchor), 0, 0, background, "unselected anchor is hollow");
                Expect(unselected, fixture.Centre(control), 3, 0, foil, "unselected control is a solid disc");
            }
            fixture.Select(anchor);
            Dispatcher.UIThread.RunJobs();
            using (var selected = PropertiesCellsTests.Render(fixture.Window, 1))
                Expect(selected, fixture.Centre(anchor), 0, 0, station, "selected anchor is filled");
            fixture.Select(control);
            Dispatcher.UIThread.RunJobs();
            using (var selected = PropertiesCellsTests.Render(fixture.Window, 1))
            {
                Expect(selected, fixture.Centre(control), 3, 0, background, "selected control is a ring (hollow between dot and rim)");
                Expect(selected, fixture.Centre(control), 0, 0, station, "selected control has its centre dot");
            }
        });
        DesktopChecks.Check("SectionEditor_FitSelectionOnAnchor_HandlesAtLeast24PxApart", () =>
        {
            var (anchor, _, _) = fixture.ResetWithAnchor();
            fixture.Select(anchor);
            Dispatcher.UIThread.RunJobs();
            fixture.Key(Key.F);
            var points = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            var centre = fixture.Centre(anchor);
            foreach (var neighbour in new[] { points[anchor.Index - 1], points[anchor.Index + 1] })
            {
                double gap = Point.Distance(centre, fixture.Centre(neighbour));
                if (gap < 24) throw new Exception($"Fit Selection left {neighbour.Role} {neighbour.Id} {gap:F1} px from the anchor");
            }
            fixture.Canvas.Fit();
        });
        DesktopChecks.Check("SectionEditor_DoubleClickInserts_BackspaceDeletesNotNamed", () =>
        {
            fixture.Reset();
            var mode = fixture.Controller.Section!;
            var before = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            // A spot on the upper curve with no point within the 14 px hit radius.
            var spot = Enumerable.Range(30, 41).Select(step => step / 100.0)
                .Select(x => fixture.Canvas.ModelToScreen(x, Sections.Probe(mode.Draft.Bytes, 0, x).UpperY))
                .First(at => before.All(point => Point.Distance(fixture.Local(point), at) > 16));
            fixture.Release(fixture.Press(spot, 1), spot);
            fixture.Release(fixture.Press(spot, 2), spot);
            TryWaitUntil(() => fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points.Count == before.Count + 1);
            if (fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points.Count != before.Count + 1)
                throw new Exception($"Double-click did not insert: {before.Count} points, reason '{fixture.Text("ModeReason").Text}', steps {fixture.Controller.Section!.Draft.Cursor}, status '{fixture.Controller.Status}', spot {spot}, selection {fixture.Controller.Selection}, sel {fixture.Canvas.SelectedVertex}");
            var after = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            var inserted = after.First(point => before.All(old => old.Id != point.Id));
            fixture.Select(after[0]);
            fixture.Key(Key.Back);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points.Count != after.Count ||
                fixture.Text("ModeReason").Text != SectionCanvas.NoseNotDeleted || !fixture.View.FindControl<Border>("ModeReasonBox")!.IsEffectivelyVisible)
                throw new Exception("⌫ on the nose was not refused with its reason: " + fixture.Text("ModeReason").Text);
            fixture.Select(inserted);
            fixture.Key(Key.Back);
            WaitUntil(() => fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points.Count == before.Count);
        });
        DesktopChecks.Check("SectionEditor_FinishWhileBlocked_FocusesReason", () =>
        {
            fixture.Reset();
            fixture.Cross();
            fixture.Canvas.Focus();
            fixture.Key(Key.Return, KeyModifiers.Meta);
            Dispatcher.UIThread.RunJobs();
            var reason = fixture.Text("ModeReason");
            if (fixture.Controller.Section is null || fixture.Button("ModeFinishButton").IsEnabled ||
                !reason.IsFocused || !reason.IsEffectivelyVisible || !reason.Text!.Contains("cross", StringComparison.Ordinal))
                throw new Exception($"⌘↩ while blocked did not focus the reason: focused {reason.IsFocused}, '{reason.Text}'");
        });

        using (var shell = new ShellFixture())
        {
            DesktopChecks.Check("SectionEditor_ReturnToX_TabToType", () =>
            {
                shell.Enter();
                var point = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
                shell.Controller.Select(new Selection.Points([new PointRef("upper", point.Id, shell.Controller.Section!.Draft.Profile)]));
                shell.Settle();
                shell.Canvas.Focus();
                shell.Key(shell.Canvas, Key.Return);
                var focused = shell.Window.FocusManager?.GetFocusedElement();
                if (focused is not TextBox { Name: "PointSpanInput" }) throw new Exception("Return on a section point did not focus its x: " + focused);
                shell.Canvas.Focus();
                shell.Key(shell.Canvas, Key.Tab);
                focused = shell.Window.FocusManager?.GetFocusedElement();
                if (focused is not ComboBox) throw new Exception("Tab from a section point did not land on its Type: " + focused);
            });
            DesktopChecks.Check("SectionEditor_Crossing_MarkerAndReasonRendered", () =>
            {
                shell.Enter();
                var crossing = shell.Cross();
                var view = shell.View;
                var finish = view.FindControl<Button>("ModeFinishButton")!;
                var reason = view.FindControl<TextBlock>("ModeReason")!;
                if (finish.IsEnabled || !reason.IsEffectivelyVisible || reason.Text != shell.Controller.Section!.FinishReason ||
                    !reason.Text!.Contains("cross", StringComparison.Ordinal) || AutomationProperties.GetHelpText(finish) != reason.Text)
                    throw new Exception($"Crossing: Finish {finish.IsEnabled}, reason '{reason.Text}' visible {reason.IsEffectivelyVisible}");
                // Show (the strip's action): the canvas frames the crossing, which then fills a good part of the width.
                shell.Host.ShowBlocker(null, crossing);
                shell.Settle();
                var canvas = shell.Canvas;
                double width = canvas.ModelToScreen(crossing.X1, 0).X - canvas.ModelToScreen(crossing.X0, 0).X;
                if (width < canvas.Bounds.Width / 4) throw new Exception($"Show did not frame the crossing: {width:F0} px of {canvas.Bounds.Width:F0}");
                double x = (crossing.X0 + crossing.X1) / 2;
                var probe = Sections.Probe(shell.Controller.Section!.Draft.Bytes, 0, x);
                var at = canvas.TranslatePoint(canvas.ModelToScreen(x, probe.UpperY), shell.Window)!.Value;
                var danger = shell.Colour("DangerBrush");
                using var pixels = PropertiesCellsTests.Render(shell.Window, 1);
                int red = 0;
                for (int dy = -14; dy <= 14; dy++)
                    for (int dx = -14; dx <= 14; dx++)
                        if (Distance(pixels.At((int)at.X + dx, (int)at.Y + dy), danger) < 60) red++;
                if (red < 12) throw new Exception($"No crossing marker rendered at the crossing: {red} danger pixels");
                shell.Controller.CancelSection();
            });
            DesktopChecks.Check("SectionEditor_EnterFromSideDoubleClick_ModeShown", () =>
            {
                shell.Leave();
                var root = shell.Side.SideSections().First(item => item.Index == 0);
                var spot = root.Outline.Skip(root.Outline.Length / 4).First(at => shell.Side.SectionsAt(at) is [{ Index: 0 }]);
                shell.Click(shell.Side, spot, 1);
                shell.Click(shell.Side, spot, 2);
                WaitUntil(() => { shell.Settle(); return shell.Controller.Section is not null; });
                var canvas = shell.Canvas;
                var nose = shell.Controller.SectionCurve(SurfaceSide.Upper)?.Points[0];
                if (shell.Controller.Section?.Draft.Assignment != 0 || shell.Host.ModelView.Mode != ModelAreaMode.Section)
                    throw new Exception("Double-click on the Root section in Side did not open the section editor: " + $"status '{shell.Controller.Status}', selection {shell.Controller.Selection}, spot {spot}, at {string.Join(";", shell.Side.SectionsAt(spot))}, gesture {shell.Controller.Gesture}");
                if (!canvas.IsFocused || canvas.SelectedVertex != ("upper", nose!.Id) || shell.Host.ModelView.PlanCanvas.IsEffectivelyVisible)
                    throw new Exception($"Mode shown without focus on the first point (focused {canvas.IsFocused}, {canvas.SelectedVertex}) or with the Plan visible");
                using var pixels = PropertiesCellsTests.Render(shell.Window, 1);
                var foil = shell.Colour("FoilBrush");
                var sample = canvas.Profile!.UpperCurve[canvas.Profile.UpperCurve.Count / 2];
                var at = canvas.TranslatePoint(canvas.ModelToScreen(sample.X, sample.Y), shell.Window)!.Value;
                if (!Near(pixels, at, 1, foil)) throw new Exception("No curve pixels at the projected upper curve");
            });
            // The Side view names a section by its station, not its profile: the Example's Root and Tip share one profile.
            DesktopChecks.Check("SectionEditor_SideDoubleClickOnTip_OpensTip", () =>
            {
                shell.Leave();
                int tip = shell.Controller.Inspection!.Authored.Assignments.Count - 1;
                var listed = shell.Side.SideSections().Select(item => item.Index).ToArray();
                if (!listed.Contains(tip) || listed.Distinct().Count() != listed.Length)
                    throw new Exception("The Side view does not list each station once: " + string.Join(", ", listed));
                var outline = shell.Side.SideSections().First(item => item.Index == tip).Outline;
                // The Example's Root and Tip overlap in Side: a spot where the Tip is the nearest, so the first click picks it.
                var spot = outline.First(at => shell.Side.SectionsAt(at) is [var nearest, ..] && nearest.Index == tip);
                shell.Click(shell.Side, spot, 1);
                shell.Click(shell.Side, spot, 2);
                WaitUntil(() => { shell.Settle(); return shell.Controller.Section is not null; });
                if (shell.Controller.Section!.Draft.Assignment != tip || shell.View.FindControl<TextBlock>("ModeTitle")!.Text != "Editing Tip section")
                    throw new Exception($"Double-click on the Tip opened station {shell.Controller.Section.Draft.Assignment} ('{shell.View.FindControl<TextBlock>("ModeTitle")!.Text}')");
            });
            DesktopChecks.Check("SectionEditor_SideOverlap_ClickCyclesSections", () =>
            {
                shell.Leave();
                var spot = shell.Side.SideSections().SelectMany(item => item.Outline)
                    .FirstOrDefault(at => shell.Side.SectionsAt(at).Count >= 2);
                var candidates = shell.Side.SectionsAt(spot);
                if (candidates.Count < 2) throw new Exception("The Side view has no spot where two sections overlap: " + string.Join(" ", shell.Side.SideSections().Select(item => $"{item.Index}:{item.Outline.Length}:{item.Outline.First()}-{item.Outline.Max(p => p.X):F0}")));
                var picked = new List<int>();
                for (int click = 0; click < 3; click++)
                {
                    shell.Click(shell.Side, spot, 1);
                    picked.Add(shell.Controller.Selection is Selection.Station station ? station.Index : -1);
                }
                if (picked[0] != candidates[0].Index || picked[1] != candidates[1].Index || picked[2] != (candidates.Count == 2 ? picked[0] : candidates[2].Index))
                    throw new Exception("Clicks at one spot did not cycle the overlapping sections: " + string.Join(", ", picked));
            });
        }

        // Last: it accepts a revision into the shared fixture's document.
        DesktopChecks.Check("SectionEditor_FinishThenReenter_ViewsAndCanvasRedrawn", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            double raised = point.Ordinate + .01;
            Wait(fixture.Controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, raised)));
            WaitUntil(() => fixture.Controller.Section!.CanFinish);
            fixture.Canvas.Focus();
            fixture.Key(Key.Return, KeyModifiers.Meta);
            WaitUntil(() => fixture.Controller.Section is null);
            Dispatcher.UIThread.RunJobs();
            if (fixture.Area.Mode != ModelAreaMode.Views || !fixture.Area.PlanCanvas.IsEffectivelyVisible || fixture.View.IsEffectivelyVisible)
                throw new Exception("Finish did not return to the views");
            fixture.Reset();
            var moved = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            if (Math.Abs(moved.Ordinate - raised) > 1e-12) throw new Exception("Re-entry did not read the finished section");
            fixture.Controller.Select(new Selection.Station(0, 0));
            Dispatcher.UIThread.RunJobs();
            using var pixels = PropertiesCellsTests.Render(fixture.Window, 1);
            Expect(pixels, fixture.Centre(moved), 0, 0, fixture.Colour("FoilBrush"), "the finished point's glyph at its new place");
        });
    }

    private static int Distance(Color colour, Color target) =>
        Math.Abs(colour.R - target.R) + Math.Abs(colour.G - target.G) + Math.Abs(colour.B - target.B);

    private static void Expect(PropertiesCellsTests.Pixels pixels, Point centre, double dx, double dy, Color target, string what)
    {
        if (!Near(pixels, centre + new Vector(dx, dy), 0.5, target))
            throw new Exception($"{what}: {pixels.At((int)Math.Round(centre.X + dx), (int)Math.Round(centre.Y + dy))} is not {target}");
    }

    private static void NotNear(PropertiesCellsTests.Pixels pixels, Point centre, double dx, double dy, Color target, string what)
    {
        var colour = pixels.At((int)Math.Round(centre.X + dx), (int)Math.Round(centre.Y + dy));
        if (Distance(colour, target) < 60) throw new Exception($"{what}: {colour} is {target}");
    }

    // A pixel within radius (in whole pixels, rounding the centre) that is close to the target colour.
    private static bool Near(PropertiesCellsTests.Pixels pixels, Point at, double radius, Color target)
    {
        int r = (int)Math.Ceiling(radius);
        for (int y = (int)Math.Round(at.Y) - r; y <= (int)Math.Round(at.Y) + r; y++)
            for (int x = (int)Math.Round(at.X) - r; x <= (int)Math.Round(at.X) + r; x++)
                if (Distance(pixels.At(x, y), target) < 60) return true;
        return false;
    }

    private static Color ColourOf(StyledElement scope, string key) =>
        scope.TryFindResource(key, scope.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
            ? brush.Color : throw new Exception("No theme brush " + key);

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

    private static bool TryWaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 2000; i++)
        {
            if (condition()) return true;
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        return false;
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

        internal void Key(Key key, KeyModifiers modifiers = KeyModifiers.None) => Canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent, Source = Canvas, Key = key, KeyModifiers = modifiers
        });

        internal Point Local(PointView point) => Canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
        internal Point Centre(PointView point) => Canvas.TranslatePoint(Local(point), Window)!.Value;
        internal Color Colour(string key) => ColourOf(Canvas, key);

        /// <summary>Screen 2: upper point 4 made a (paired) anchor; nothing selected. Returns an anchor, a control and the nose.</summary>
        internal (PointView Anchor, PointView Control, PointView Nose) ResetWithAnchor()
        {
            Reset();
            var target = Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            Wait(Controller.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, target.Id, true)));
            Controller.Select(new Selection.Station(0, Controller.Inspection!.Authored.Assignments[0].Eta));
            Dispatcher.UIThread.RunJobs();
            var points = Controller.SectionCurve(SurfaceSide.Upper)!.Points;
            return (points.First(point => point.Role == PointRole.Anchor), points.First(point => point.Role == PointRole.Control), points[0]);
        }

        /// <summary>Screen 3: upper cv-3 pulled through the lower surface; returns the crossing once it is assessed.</summary>
        internal (double X0, double X1) Cross() => CrossSection(Controller);

        internal void KeyUp(Key key) => Canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent, Source = Canvas, Key = key
        });

        internal Pointer NewPointer() => new(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        private Point WindowPoint(Point local) => Canvas.TranslatePoint(local, Window)!.Value;
        internal Pointer Press(Point local, int clicks = 1)
        {
            var pointer = NewPointer();
            Canvas.RaiseEvent(new PointerPressedEventArgs(Canvas, pointer, Window, WindowPoint(local), 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, clicks));
            Dispatcher.UIThread.RunJobs();
            return pointer;
        }

        internal void Move(Pointer pointer, Point local)
        {
            Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, Canvas, pointer, Window, WindowPoint(local), 2,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            Dispatcher.UIThread.RunJobs();
        }

        internal void Release(Pointer pointer, Point local)
        {
            Canvas.RaiseEvent(new PointerReleasedEventArgs(Canvas, pointer, Window, WindowPoint(local), 3,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            pointer.Dispose();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() { Window.Close(); Controller.Dispose(); }
    }

    private static (double X0, double X1) CrossSection(WorkbenchController controller)
    {
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
        Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, -0.3)));
        WaitUntil(() => controller.Section!.Assessment is not null);
        Dispatcher.UIThread.RunJobs();
        var mode = controller.Section!;
        return Sections.DisplayCrossing(mode.Draft.Bytes, mode.Draft.Assignment) ?? throw new Exception("The pulled point does not cross");
    }

    /// <summary>The whole shell (Side view, Properties, Show), for the Return/Tab and Show checks.</summary>
    private sealed class ShellFixture : IDisposable
    {
        internal WorkbenchController Controller { get; } = new();
        internal ShellHost Host { get; }
        internal Window Window { get; }
        internal ElevationView Side => Host.ModelView.FindControl<ElevationView>("SideElevation")!;
        internal SectionEditorView View => Host.ModelView.FindControl<SectionEditorView>("SectionModeEditor")!;
        internal SectionCanvas Canvas => View.FindControl<SectionCanvas>("ModeCanvas")!;
        internal Color Colour(string key) => ColourOf(Canvas, key);

        internal ShellFixture()
        {
            Wait(Controller.OpenExampleAsync());
            Host = new ShellHost(Controller);
            Window = new Window { Content = Host, Width = 1400, Height = 1000 };
            Window.Show();
            Host.RefreshPanes();
            Controller.Layout = ViewLayout.Four;
            WaitUntil(() => { Settle(); return Controller.Surface is not null && !Controller.SurfaceUpdating && Side.Camera is not null; });
        }

        internal void Settle()
        {
            for (int i = 0; i < 4; i++)
            {
                Dispatcher.UIThread.RunJobs();
                Window.UpdateLayout();
            }
        }

        internal void Enter()
        {
            if (Controller.Section is not null) Controller.CancelSection();
            Controller.Select(new Selection.Station(0, Controller.Inspection!.Authored.Assignments[0].Eta));
            Wait(Controller.EnterSectionAsync(0, EntryOrigin.Side));
            Settle();
        }

        internal (double X0, double X1) Cross()
        {
            var crossing = CrossSection(Controller);
            Settle();
            return crossing;
        }

        internal void Leave()
        {
            if (Controller.Section is not null) Controller.CancelSection();
            Controller.Select(new Selection.Foil());
            Settle();
            WaitUntil(() => { Settle(); return Controller.Surface is not null && !Controller.SurfaceUpdating && Side.Camera is not null; });
        }

        // A left click with its click count (the trailing clickCount argument; the timestamp is not the count).
        internal void Click(Control target, Point local, int clicks)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var at = target.TranslatePoint(local, Window)!.Value;
            target.RaiseEvent(new PointerPressedEventArgs(target, pointer, Window, at, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, clicks));
            target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, Window, at, 2,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            Settle();
        }

        internal void Key(Control target, Key key)
        {
            target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = key });
            Settle();
        }

        public void Dispose() { Window.Close(); Controller.Dispose(); }
    }
}
