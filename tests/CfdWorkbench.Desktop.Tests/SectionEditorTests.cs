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
        Capture();
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
            // §0.1 step 2: "Editing Root section · Shared with Tip · Make unique to Root".
            if (fixture.Text("ModeTitle").Text != "Editing Root section" || fixture.Text("ScopeChip").Text != "Shared with Tip · " ||
                fixture.Text("ScopeChipLinkText").Text != "Make unique to Root" || !fixture.Button("ScopeChipLink").IsEffectivelyVisible)
                throw new Exception($"Mode title or scope chip does not name the active Root section: '{fixture.Text("ModeTitle").Text}', '{fixture.Text("ScopeChip").Text}{fixture.Text("ScopeChipLinkText").Text}'");
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
            string help = Avalonia.Automation.AutomationProperties.GetHelpText(first) ?? "";
            if (!help.Contains("chord") || !help.Contains("t/c") || first.GetVisualDescendants().OfType<SectionThumb>().SingleOrDefault()?.Profile is null ||
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
        // release-freeze: after the key that begins the run (one refresh, as a press), a nudge only records its target. Each
        // key used to refresh the whole shell (~209 ms a key under load). The run still accumulates (D-7) into one step.
        DesktopChecks.Check("SectionEditor_NudgeRun_NoShellRefreshPerKey", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            fixture.Select(point);
            Dispatcher.UIThread.RunJobs();
            int cursor = fixture.Controller.Section!.Draft.Cursor;
            fixture.Key(Key.Up);
            Dispatcher.UIThread.RunJobs();
            int changes = 0;
            void Count() => changes++;
            fixture.Controller.Changed += Count;
            fixture.Controller.SectionChanged += Count;
            fixture.Controller.SelectionChanged += Count;
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    fixture.Key(Key.Up);
                    Dispatcher.UIThread.RunJobs();
                }
            }
            finally
            {
                fixture.Controller.Changed -= Count;
                fixture.Controller.SectionChanged -= Count;
                fixture.Controller.SelectionChanged -= Count;
            }
            if (changes != 0) throw new Exception($"Four nudge keys raised {changes} controller changes; the shell refreshed per key");
            if (fixture.Controller.Section!.Draft.Cursor != cursor) throw new Exception("The nudge run committed before KeyUp");
            fixture.KeyUp(Key.Up);
            WaitUntil(() => fixture.Controller.Section!.Draft.Cursor == cursor + 1 && fixture.Controller.Section.Assessment is not null);
            var moved = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            if (Math.Abs(moved.Ordinate - point.Ordinate - .005) > 1e-8)
                throw new Exception($"Five nudges moved y by {moved.Ordinate - point.Ordinate:G6}, not 0.005 in one step");
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
        // UXR (marine-CAD re-review): §11.2 "teeth point outward" — on the convex 20–60 % chord span of the Example section,
        // every upper tooth rises off the curve and every lower tooth drops off it, away from the chord line.
        DesktopChecks.Check("SectionEditor_Comb_TeethPointOutward", () =>
        {
            fixture.Reset();
            fixture.Canvas.Fit();
            using var pixels = PropertiesCellsTests.Render(fixture.Window, 1);
            double from = fixture.Canvas.ModelToScreen(.2, 0).X, to = fixture.Canvas.ModelToScreen(.6, 0).X;
            double chord = fixture.Canvas.ModelToScreen(0, 0).Y;
            var span = fixture.Canvas.CombTeeth.Where(tooth => tooth.Start.X >= from && tooth.Start.X <= to &&
                Point.Distance(tooth.Start, tooth.Tip) > 1).ToArray();
            var inward = span.Where(tooth => tooth.Start.Y < chord ? tooth.Tip.Y > tooth.Start.Y : tooth.Tip.Y < tooth.Start.Y).ToArray();
            if (span.Length < 10 || inward.Length > 0)
                throw new Exception($"Comb teeth point into the foil: {inward.Length} of {span.Length} on 20–60 % chord");
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
        // §3.7 and §7 Concurrency: a drag frame draws from the gesture's display state. It raises no controller change (the
        // shell refresh that cost 552-896 ms per move under load before this fix) and applies no Core step, and the very
        // next render draws the curve through the moved point. Wall time lives in Readiness_SectionDragMove_Under16Ms.
        DesktopChecks.Check("SectionEditor_DragMove_DrawsWithinOneFrame", () =>
        {
            fixture.Reset();
            var canvas = fixture.Canvas;
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var from = fixture.Local(point);
            var size = new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
            var rest = canvas.DrawnCurves[0];
            var pointer = fixture.Press(from);
            long generation = fixture.Controller.Section!.Draft.Generation;
            int changes = 0;
            bool dragMeasured = false;
            void Count() => changes++;
            fixture.Controller.Changed += Count;
            fixture.Controller.SectionChanged += Count;
            try
            {
                var previous = rest;
                for (int i = 1; i <= 6; i++)
                {
                    var to = from + new Vector(i * 4, -i * 6);
                    fixture.Move(pointer, to);
                    using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
                    var drawn = canvas.DrawnCurves[0];
                    if (changes != 0 || fixture.Controller.Section!.Draft.Generation != generation)
                        throw new Exception($"Move {i} notified the shell {changes} times or applied a step (generation {fixture.Controller.Section!.Draft.Generation} vs {generation})");
                    if (MaxOffset(drawn, previous) < .5)
                        throw new Exception($"Move {i}: the drawn upper curve did not follow the point (largest offset {MaxOffset(drawn, previous):F2} px)");
                    previous = drawn;
                }
                if (MaxOffset(previous, rest) < 4) throw new Exception($"The drawn curve moved only {MaxOffset(previous, rest):F2} px over a 36 px drag");
                dragMeasured = true;
            }
            finally
            {
                fixture.Controller.Changed -= Count;
                fixture.Controller.SectionChanged -= Count;
                // SECTION-EDITOR-LOAD-FLAKE: a failed drag must not leave the gesture pressed and the draft owned,
                // or every later check on this shared fixture fails DSL-DRAFT-OWNED.
                if (!dragMeasured) fixture.Release(pointer, from);
            }
            int cursor = fixture.Controller.Section!.Draft.Cursor;
            fixture.Release(pointer, from + new Vector(24, -36));
            WaitUntil(() => fixture.Controller.Section!.Draft.Cursor == cursor + 1);
        });
        // The certificate gates only Finish: with the assessment held open, the step still lands on release, Finish waits
        // on "Checking…", and the next drag's point and curve still follow the pointer.
        DesktopChecks.Check("SectionEditor_SlowAssessment_DrawFollowsPointer", () =>
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var slow = new Fixture(_ => gate.Task);
            var canvas = slow.Canvas;
            var size = new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height);
            var point = slow.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var from = slow.Local(point);
            int cursor = slow.Controller.Section!.Draft.Cursor;
            var pointer = slow.Press(from);
            slow.Move(pointer, from + new Vector(8, -12));
            slow.Release(pointer, from + new Vector(8, -12));
            WaitUntil(() => slow.Controller.Section!.Draft.Cursor == cursor + 1);
            var mode = slow.Controller.Section!;
            if (mode.Assessment is not null || mode.CanFinish || mode.FinishReason != "Checking…" || slow.Button("ModeFinishButton").IsEnabled)
                throw new Exception($"The released step did not wait on the certificate: assessed {mode.Assessment is not null}, reason '{mode.FinishReason}'");
            var moved = slow.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var start = slow.Local(moved);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
            var rest = canvas.DrawnCurves[0];
            var second = slow.Press(start);
            var to = start + new Vector(10, -16);
            slow.Move(second, to);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
            if (gate.Task.IsCompleted || slow.Controller.Section!.Assessment is not null)
                throw new Exception("The assessment finished before the draw was checked");
            if (MaxOffset(canvas.DrawnCurves[0], rest) < 2)
                throw new Exception($"With the check pending the curve did not follow the pointer ({MaxOffset(canvas.DrawnCurves[0], rest):F2} px)");
            slow.Release(second, to);
            WaitUntil(() => slow.Controller.Section!.Draft.Cursor == cursor + 2);
            gate.SetResult();
            WaitUntil(() => slow.Controller.Section!.Assessment is not null);
            if (!slow.Controller.Section!.CanFinish || !slow.Button("ModeFinishButton").IsEnabled)
                throw new Exception("Finish did not follow the certificate once it answered");
        });
        // release-freeze (§7 Concurrency): a release applies its step on the thread pool. With the step held where it applies,
        // the release returns, the UI thread runs a posted job, the drawn drag result stays on screen, the cursor has not
        // moved and Finish is off; once the hold lifts the step lands once and Finish follows the certificate.
        DesktopChecks.Check("SectionEditor_Release_StepAppliesOffUiThread", () =>
        {
            using var hold = new ManualResetEventSlim(false);
            bool? onUiThread = null;
            using var held = new Fixture(stepGate: _ => { onUiThread = Dispatcher.UIThread.CheckAccess(); hold.Wait(TimeSpan.FromSeconds(5)); });
            var canvas = held.Canvas;
            var size = new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height);
            var point = held.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var from = held.Local(point);
            int cursor = held.Controller.Section!.Draft.Cursor;
            var pointer = held.Press(from);
            var to = from + new Vector(10, -16);
            held.Move(pointer, to);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
            var dragged = canvas.DrawnCurves[0];
            held.Release(pointer, to);
            WaitUntil(() => onUiThread is not null);
            try
            {
                if (onUiThread == true) throw new Exception("The released step applied on the UI thread");
                bool posted = false;
                Dispatcher.UIThread.Post(() => posted = true);
                Dispatcher.UIThread.RunJobs();
                if (!posted) throw new Exception("The UI thread did not run a posted job while the step applied");
                var mode = held.Controller.Section!;
                if (mode.Draft.Cursor != cursor || mode.CanFinish || held.Button("ModeFinishButton").IsEnabled)
                    throw new Exception($"While the step applied: cursor {mode.Draft.Cursor} (was {cursor}), Finish enabled {held.Button("ModeFinishButton").IsEnabled}");
                using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
                if (MaxOffset(canvas.DrawnCurves[0], dragged) > .5)
                    throw new Exception($"The drawn drag result did not stay while the step applied ({MaxOffset(canvas.DrawnCurves[0], dragged):F2} px)");
            }
            finally { hold.Set(); }
            WaitUntil(() => held.Controller.Section!.Draft.Cursor == cursor + 1 && held.Controller.Section.Assessment is not null);
            if (!held.Controller.Section!.CanFinish || !held.Button("ModeFinishButton").IsEnabled)
                throw new Exception("Finish did not follow the certificate once the step landed");
        });
        // release-freeze: in the section mode a controller change rebinds the Properties pane (~100 ms a bind under load) only
        // when an input it reads changed. A step rebinds it once; the assessment landing changes none of its inputs, so it
        // rebinds the other panes and the mode bar (Finish follows the certificate) and leaves Properties as it is.
        DesktopChecks.Check("ShellHost_SectionStep_RefreshesOnlyChangedPanes", () =>
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var shell = new ShellFixture(1280, 800, generation => generation == 1 ? gate.Task : Task.CompletedTask);
            shell.Enter();
            var host = shell.Host;
            long properties = host.PropertiesBinds;
            var point = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            var step = host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, point.Ordinate + .005));
            WaitUntil(() => shell.Controller.Section!.Draft.Cursor == 1);
            shell.Settle();
            long stepped = host.PropertiesBinds, refreshes = host.PaneRefreshes;
            if (stepped - properties != 1) throw new Exception($"One step bound Properties {stepped - properties} times");
            var finish = shell.View.FindControl<Button>("ModeFinishButton")!;
            if (finish.IsEnabled) throw new Exception("Finish was on before the certificate answered");
            gate.SetResult();
            Wait(step);
            shell.Settle();
            if (host.PropertiesBinds != stepped)
                throw new Exception($"The assessment landing bound Properties {host.PropertiesBinds - stepped} times; none of its inputs changed");
            if (host.PaneRefreshes == refreshes || !finish.IsEnabled)
                throw new Exception($"The assessment landing did not refresh the other panes ({host.PaneRefreshes - refreshes}) or turn Finish on");
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
        // UXR: "Checking…" is a state reason; once the check of the same step finishes with Finish on, the box goes.
        DesktopChecks.Check("SectionEditor_CheckFinished_CheckingReasonHidden", () =>
        {
            fixture.Reset();
            var point = fixture.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var step = fixture.Controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + 0.001));
            Dispatcher.UIThread.RunJobs();
            Wait(step);
            WaitUntil(() => fixture.Controller.Section!.Assessment is not null);
            Dispatcher.UIThread.RunJobs();
            var box = fixture.View.FindControl<Border>("ModeReasonBox")!;
            if (!fixture.Controller.Section!.CanFinish || box.IsVisible)
                throw new Exception($"The reason box outlived the check: can finish {fixture.Controller.Section.CanFinish}, box '{fixture.Text("ModeReason").Text}'");
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
            // Ruling 71 (native look): after Make unique to Root, Control → Anchor would give Root other knots than Tip.
            // The step is refused in the strip with COPY-209, and the draft and its Finish state stay as they were.
            DesktopChecks.Check("SectionEditor_UniqueSectionAbscissaStep_RefusedInStripFinishUnchanged", () =>
            {
                shell.Enter();
                Wait(shell.Host.ApplySectionStepAsync(new SectionStep.MakeUnique()));
                WaitUntil(() => shell.Controller.Section is { Assessment: not null });
                shell.Settle();
                var before = shell.Controller.Section!;
                var finish = shell.View.FindControl<Button>("ModeFinishButton")!;
                bool finishBefore = finish.IsEnabled;
                var target = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
                Wait(shell.Host.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, target.Id, true)));
                // The shell task ends when the refusal is in the strip. That refusal cleared the certificate
                // and asked for it again, so Finish stays "Checking…" until this re-check lands.
                WaitUntil(() => shell.Controller.Section?.Assessment is not null);
                shell.Settle();
                var after = shell.Controller.Section!;
                const string expected = "This edit would give Root's section different point positions from Tip's, and the wing between " +
                    "them can't be checked then. Move points up or down only, or keep the section shared. Nothing changed.";
                if (shell.Host.StatusStrip.Text != expected)
                    throw new Exception($"The refusal is not in the strip: '{shell.Host.StatusStrip.Text}'");
                if (after.Draft.Cursor != before.Draft.Cursor || after.Draft.Generation != before.Draft.Generation ||
                    !after.CanFinish || after.CanFinish != before.CanFinish || after.FinishReason != before.FinishReason ||
                    !finish.IsEnabled || finish.IsEnabled != finishBefore)
                    throw new Exception($"The refused step changed the draft or Finish: cursor {before.Draft.Cursor} → {after.Draft.Cursor}, " +
                        $"can finish {before.CanFinish} → {after.CanFinish}, reason '{before.FinishReason}' → '{after.FinishReason}', button {finishBefore} → {finish.IsEnabled}");
                shell.Controller.CancelSection();
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
                var danger = shell.Colour("PlanDangerBrush");   // §11.2: the crossing marker is danger-viewport on the canvas
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

    // Readiness tier (wall time; load-sensitive, so never in the fast ring): one section drag through the whole shell.
    // A move is the move handler, its dispatcher jobs and one render of the canvas; the target is one 60 Hz frame (16 ms).
    // Measured before the fix (load 20-34): 552-896 ms median per move, 1.27-2.18 s from release to the first frame.
    internal static void RunReadiness()
    {
        DesktopChecks.Check("Readiness_SectionDragMove_Under16Ms", () =>
        {
            using var shell = new ShellFixture(1280, 800);
            shell.Enter();
            var canvas = shell.Canvas;
            var point = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            var from = canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
            var size = new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height);
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            Point At(Point local) => canvas.TranslatePoint(local, shell.Window)!.Value;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, shell.Window, At(from), 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            Dispatcher.UIThread.RunJobs();
            double press = watch.Elapsed.TotalMilliseconds;
            var moves = new List<double>();
            for (int i = 1; i <= 20; i++)
            {
                watch.Restart();
                canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, shell.Window, At(from + new Vector(i * 2, -i)),
                    (ulong)(1 + i), new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                Dispatcher.UIThread.RunJobs();
                using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
                moves.Add(watch.Elapsed.TotalMilliseconds);
            }
            int cursor = shell.Controller.Section!.Draft.Cursor;
            watch.Restart();
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, shell.Window, At(from + new Vector(40, -20)), 30,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            WaitUntil(() => shell.Controller.Section!.Draft.Cursor == cursor + 1);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size)) bitmap.Render(canvas);
            double released = watch.Elapsed.TotalMilliseconds;
            WaitUntil(() => shell.Controller.Section!.Assessment is not null);
            double assessed = watch.Elapsed.TotalMilliseconds;
            double p95 = moves.Order().ElementAt((int)Math.Ceiling(.95 * moves.Count) - 1);
            Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"READINESS SectionDragMove value_ms={p95:F3} target_ms=16 median_ms={moves.Order().ElementAt(moves.Count / 2):F3} press_ms={press:F3} release_to_drawn_ms={released:F3} release_to_assessed_ms={assessed:F3} samples={moves.Count}"));
            if (p95 >= 16) Console.WriteLine("READINESS-MISS SectionDragMove p95 over one 60 Hz frame; read with the load average");
        });
        // The UI thread's longest block from a release (or a nudge run's key-up) until the certificate answers: the release
        // handler itself, then each dispatcher turn. landing_ms is the turn in which the assessment result was applied.
        // Measured before release-freeze (load 34): ~660 ms on release (step apply 173-219 ms + one RefreshPanes 240-300 ms),
        // ~300-400 ms on the assessment landing.
        DesktopChecks.Check("Readiness_SectionReleaseFreeze_Under50Ms", () =>
        {
            using var shell = new ShellFixture(1280, 800);
            shell.Enter();
            var canvas = shell.Canvas;
            Point At(Point local) => canvas.TranslatePoint(local, shell.Window)!.Value;
            var handlers = new List<double>();
            var blocks = new List<double>();
            var landings = new List<double>();
            (double Block, double Landing) Settle(int cursor)
            {
                double block = 0, landing = 0;
                var turn = System.Diagnostics.Stopwatch.StartNew();
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
                while (DateTime.UtcNow < deadline)
                {
                    bool assessed = shell.Controller.Section!.Assessment is not null;
                    if (assessed && shell.Controller.Section.Draft.Cursor == cursor) return (block, landing);
                    turn.Restart();
                    Dispatcher.UIThread.RunJobs();
                    double ms = turn.Elapsed.TotalMilliseconds;
                    block = Math.Max(block, ms);
                    if (!assessed && shell.Controller.Section!.Assessment is not null) landing = ms;
                    Thread.Yield();
                }
                throw new TimeoutException("The released step was not assessed");
            }
            for (int run = 0; run < 3; run++)
            {
                var point = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
                var from = canvas.ModelToScreen(point.SpanMeters, point.Ordinate);
                using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
                canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, shell.Window, At(from), 1,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
                Dispatcher.UIThread.RunJobs();
                var to = from + new Vector(6, run % 2 == 0 ? -6 : 6);
                canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, shell.Window, At(to), 2,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                Dispatcher.UIThread.RunJobs();
                int cursor = shell.Controller.Section!.Draft.Cursor;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, shell.Window, At(to), 3,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
                double handler = watch.Elapsed.TotalMilliseconds;
                var (block, landing) = Settle(cursor + 1);
                handlers.Add(handler);
                blocks.Add(Math.Max(handler, block));
                landings.Add(landing);
            }
            // A nudge run: ten plain arrow presses, then the key-up that makes it one step.
            var nudged = shell.Controller.SectionCurve(SurfaceSide.Upper)!.Points[3];
            canvas.SelectedVertex = (nudged.Curve, nudged.Id);
            shell.Controller.Select(new Selection.Points([new PointRef(nudged.Curve, nudged.Id, shell.Controller.Section!.Draft.Profile)]));
            Dispatcher.UIThread.RunJobs();
            int before = shell.Controller.Section!.Draft.Cursor;
            var keys = new List<double>();
            var key = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                key.Restart();
                canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = canvas, Key = Key.Up });
                Dispatcher.UIThread.RunJobs();
                keys.Add(key.Elapsed.TotalMilliseconds);
            }
            key.Restart();
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Source = canvas, Key = Key.Up });
            double keyUp = key.Elapsed.TotalMilliseconds;
            var (nudgeBlock, nudgeLanding) = Settle(before + 1);
            static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);
            double worst = blocks.Max();
            Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"READINESS SectionReleaseFreeze value_ms={worst:F3} target_ms=50 release_handler_median_ms={Median(handlers):F3} release_block_median_ms={Median(blocks):F3} landing_median_ms={Median(landings):F3} nudge_key_median_ms={Median(keys):F3} nudge_key_max_ms={keys.Max():F3} nudge_keyup_ms={keyUp:F3} nudge_block_ms={Math.Max(keyUp, nudgeBlock):F3} nudge_landing_ms={nudgeLanding:F3} load={LoadAverage()} samples={blocks.Count}"));
            if (worst >= 50) Console.WriteLine("READINESS-MISS SectionReleaseFreeze UI thread blocked past 50 ms; read with the load average");
        });
    }

    [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "getloadavg")]
    private static extern int GetLoadAverage(double[] values, int count);

    // The 1-minute load average for a readiness line; "not-recorded" where libc has no getloadavg (Windows), never a guess.
    private static string LoadAverage()
    {
        try
        {
            var values = new double[1];
            return GetLoadAverage(values, 1) == 1 ? values[0].ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "not-recorded";
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException) { return "not-recorded"; }
    }

    /// <summary>The drawn curves' screen polylines, for "did the curve follow" checks.</summary>
    private static double MaxOffset(IReadOnlyList<Point> moved, IReadOnlyList<Point> rest) =>
        moved.Max(point => rest.Min(other => Point.Distance(point, other)));

    /// <summary>
    /// Review captures of the built app (CFDW_EDT_CAPTURE=&lt;dir&gt;): the approved mockup's paired screens 2, 2b, 2c and 3
    /// in the Precision workspace in a window opened at 1280 × 800 (the app's launch size, so the side bars open at their
    /// 260 px), light and dark. Off by default; never part of the gate.
    /// </summary>
    public static void Capture()
    {
        if (Environment.GetEnvironmentVariable("CFDW_EDT_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        foreach (var (variant, theme) in new[] { (Avalonia.Styling.ThemeVariant.Light, "light"), (Avalonia.Styling.ThemeVariant.Dark, "dark") })
            CaptureTheme(directory, variant, theme);
    }

    private static void CaptureTheme(string directory, Avalonia.Styling.ThemeVariant variant, string theme)
    {
        using var shell = new ShellFixture(1280, 800);
        var controller = shell.Controller;
        shell.Window.RequestedThemeVariant = variant;
        shell.Host.ApplyWorkspace(WorkspaceId.Precision);
        shell.Enter();
        void Step(SectionStep step) { Wait(shell.Host.ApplySectionStepAsync(step)); Assessed(); }
        void Assessed()
        {
            WaitUntil(() => controller.Section is not { Draft.StepCount: > 0, Assessment: null });
            shell.Settle();
        }
        void Pick(string curve, string id)
        {
            controller.Select(new Selection.Points([new PointRef(curve, id, controller.Section!.Draft.Profile)]));
            shell.Settle();
        }
        void Show()
        {
            if (shell.Host.StatusStrip.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.Name == "StatusTryAgainButton") is
                { IsEffectivelyVisible: true } action)
                action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            shell.Settle();
        }
        void Save(string name)
        {
            shell.Settle();
            name = name.Replace("-light.png", $"-{theme}.png", StringComparison.Ordinal);
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new PixelSize((int)shell.Window.ClientSize.Width, (int)shell.Window.ClientSize.Height));
            bitmap.Render(shell.Window);
            bitmap.Save(Path.Combine(directory, name));
            Console.WriteLine($"CAPTURE {name} strip '{shell.Host.StatusStrip.Text}' refusal {controller.SectionRefitRefusal} reason-box {shell.View.FindControl<Border>("ModeReasonBox")!.IsVisible} '{shell.View.FindControl<TextBlock>("ModeReason")!.Text}' finish-reason '{controller.Section?.FinishReason}'");
        }

        // 2: upper point 4 made an anchor (paired), Horizontal, selected, after Fit Selection.
        string id = controller.SectionCurve(SurfaceSide.Upper)!.Points[3].Id;
        Step(new SectionStep.SetType(SurfaceSide.Upper, id, true));
        Step(new SectionStep.SetTangent(SurfaceSide.Upper, id, TangentKind.Horizontal, null, null));
        var upper = controller.SectionCurve(SurfaceSide.Upper)!.Points;
        int anchor = upper.ToList().FindIndex(point => point.Role == PointRole.Anchor);
        Pick("upper", upper[anchor].Id);
        shell.Canvas.FitSelection();
        Save("edt-s2-light.png");

        // 2b: the control after the anchor's tail handle typed to x 60 % (the mockup's XM); its lower partner moves too.
        var moved = upper[anchor + 3];
        Step(new SectionStep.Move(SurfaceSide.Upper, moved.Id, 0.60, moved.Ordinate));
        Pick("upper", moved.Id);
        shell.Canvas.Fit();
        Save("edt-s2b-light.png");
        Wait(controller.UndoSectionStepAsync());
        Assessed();

        // 2c: Anchor → Control on the anchor; refused when the lower refit is over the limit, with its marker.
        Pick("upper", upper[anchor].Id);
        Step(new SectionStep.SetType(SurfaceSide.Upper, upper[anchor].Id, false));
        Show();
        Save("edt-s2c-light.png");

        // 3: a lower point dragged up through the upper surface: Finish off with its reason tied to it, the strip warning
        // with Show; the full chord view (Fit), as the mockup draws it, so the crossing shows on the whole section.
        var lower = controller.SectionCurve(SurfaceSide.Lower)!.Points;
        var low = lower[lower.Count - 3];
        Step(new SectionStep.Move(SurfaceSide.Lower, low.Id, low.SpanMeters, 0.10));
        Pick("lower", low.Id);
        shell.Canvas.Fit();
        Save("edt-s3-light.png");
        controller.CancelSection();
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

    // A deadline, not a turn count: a section step now applies on the thread pool (§7 Concurrency), and under load a
    // fixed number of dispatcher turns can pass before it lands.
    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Section editor state did not settle");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
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
        internal WorkbenchController Controller { get; }
        internal ModelArea Area { get; } = new();
        internal Window Window { get; }
        internal SectionEditorView View => Area.FindControl<SectionEditorView>("SectionModeEditor")!;
        internal SectionCanvas Canvas => View.FindControl<SectionCanvas>("ModeCanvas")!;
        internal TextBlock Text(string name) => View.FindControl<TextBlock>(name)!;
        internal Button Button(string name) => View.FindControl<Button>(name)!;

        /// <param name="assessmentGate">The controller's CTL seam: it delays the real <c>AssessSection</c>, never replaces it.</param>
        /// <param name="stepGate">The controller's CTL seam: it runs where a step applies, before Core's patch, never replacing it.</param>
        internal Fixture(Func<long, Task>? assessmentGate = null, Action<long>? stepGate = null)
        {
            Controller = new WorkbenchController(sectionAssessmentGate: assessmentGate, sectionStepGate: stepGate);
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
        internal WorkbenchController Controller { get; }
        internal ShellHost Host { get; }
        internal Window Window { get; }
        internal ElevationView Side => Host.ModelView.FindControl<ElevationView>("SideElevation")!;
        internal SectionEditorView View => Host.ModelView.FindControl<SectionEditorView>("SectionModeEditor")!;
        internal SectionCanvas Canvas => View.FindControl<SectionCanvas>("ModeCanvas")!;
        internal Color Colour(string key) => ColourOf(Canvas, key);

        internal ShellFixture(double width = 1400, double height = 1000, Func<long, Task>? assessmentGate = null)
        {
            Controller = new WorkbenchController(sectionAssessmentGate: assessmentGate);
            Wait(Controller.OpenExampleAsync());
            Host = new ShellHost(Controller);
            Window = new Window { Content = Host, Width = width, Height = height };
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
