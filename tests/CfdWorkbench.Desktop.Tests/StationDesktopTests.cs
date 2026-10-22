using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track S1 (Add station, design add-station.md 7): the controller half of D2, D3, D6, D9 and the two promotion-cost checks. Controller
/// checks need no window (ring --controller-shell). The window checks (D1, D4, D5, D7, D6's focus) are in PlanCanvasStationTests.cs and D8 in
/// ShellWindowTests.cs. Documents are the committed 8-point basic foil with profiles written here, so every station shares one spacing.
/// </summary>
public static class StationDesktopTests
{
    // foil-basic's degree-5, 8-point upper side; a second shape peaking later; a third peaking much later (a large change).
    private static readonly double[] A = [0, 0.025, 0.055, 0.065, 0.05, 0.03, 0.01, 0];
    private static readonly double[] Near = [0, 0.0225, 0.0475, 0.060, 0.0575, 0.04, 0.015, 0];
    private static readonly double[] Far = [0, 0.02, 0.04, 0.055, 0.065, 0.05, 0.02, 0];

    /// <summary>A two-or-three-station foil of the basic planform (450 mm half span, chord <paramref name="chord"/> mm).</summary>
    public static byte[] Doc(string sections, double chord, params (string Name, double[] Upper)[] profiles)
    {
        const string knots = "[0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1]";
        double[] x = [0, 0, 0.15, 0.35, 0.55, 0.75, 0.9, 1];
        string Points(double[] y, double sign) =>
            string.Join(", ", x.Select((value, i) => FormattableString.Invariant($"({value}, {(i is 0 or 7 ? 0 : sign * y[i])})")));
        var text = new StringBuilder();
        text.Append("foildsl \"4.0\"\nfoil \"Station fixture\" {\n  units mm\n  half_span 450 mm\n  evaluator \"cfdw-cv\" \"2\"\n  symmetry mirror_y\n  planform {\n");
        string rail = "degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [";
        string at = "(0, {0}), (0.1, {0}), (0.3, {0}), (0.5, {0}), (0.7, {0}), (0.9, {0}), (1, {0})";
        text.Append("    leading cv { ").Append(rail).Append(string.Format(CultureInfo.InvariantCulture, at, 0)).Append("] }\n");
        text.Append("    trailing cv { ").Append(rail).Append(string.Format(CultureInfo.InvariantCulture, at, chord)).Append("] }\n  }\n");
        text.Append("  dihedral cv { ").Append(rail).Append(string.Format(CultureInfo.InvariantCulture, at, 0)).Append("] }\n");
        text.Append("  twist cv { ").Append(rail).Append(string.Format(CultureInfo.InvariantCulture, at, 0)).Append("] }\n");
        text.Append("  thickness cv { ").Append(rail).Append(string.Format(CultureInfo.InvariantCulture, at, 0.12)).Append("] }\n  profiles {\n");
        foreach (var (name, upper) in profiles)
        {
            text.Append("    profile \"").Append(name).Append("\" {\n");
            text.Append("      upper cv { degree 5 knots ").Append(knots).Append(" points [").Append(Points(upper, 1)).Append("] }\n");
            text.Append("      lower cv { degree 5 knots ").Append(knots).Append(" points [").Append(Points(upper, -1)).Append("] }\n    }\n");
        }
        text.Append("  }\n  sections { ").Append(sections).Append(" }\n}\n");
        return FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(text.ToString())));
    }

    /// <summary>F-SAME: both ends name one profile (regime R0).</summary>
    public static byte[] Same() => Doc("at root profile \"a\" at tip profile \"a\"", 120, ("a", A));
    /// <summary>F-NEAR: the tip's shape differs slightly (regime R2, a few micrometres).</summary>
    public static byte[] NearPair(double chord = 120) => Doc("at root profile \"a\" at tip profile \"n\"", chord, ("a", A), ("n", Near));
    /// <summary>F-SAME with a station at 60 percent on the same profile: removing it changes nothing.</summary>
    public static byte[] ThreeSame() => Doc("at root profile \"a\" at 60 % profile \"a\" at tip profile \"a\"", 120, ("a", A));
    /// <summary>F-THREE: a station at 60 percent on a far shape; removing it moves the loft far above 10 micrometres.</summary>
    public static byte[] Three() => Doc("at root profile \"a\" at 60 % profile \"f\" at tip profile \"a\"", 120, ("a", A), ("f", Far));

    /// <summary>A controller on <paramref name="source"/>; the measurement grid is the coarse 17 x 201 unless <paramref name="production"/> (the
    /// R1/R2 split and the refusals do not read a sampled maximum; the cost checks do and run on the production grid).</summary>
    public static WorkbenchController Open(byte[] source, bool production = false)
    {
        var controller = new WorkbenchController();
        controller.OpenFoilAsync(source, "Station fixture").GetAwaiter().GetResult();
        if (!production) UseCoarseGrid(controller);
        return controller;
    }

    /// <summary>The 17 eta x 201 x grid, set on the controller's session through its internal setter (Desktop.Tests is not a Core friend).</summary>
    public static void UseCoarseGrid(WorkbenchController controller)
    {
        var session = typeof(WorkbenchController).GetField("session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(controller)!;
        var grid = typeof(AuthoringSession).GetProperty("StationGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        grid.SetValue(session, Activator.CreateInstance(grid.PropertyType, 17, 201));
    }

    public static int StationCount(WorkbenchController controller) => controller.Inspection!.Authored.Assignments.Count;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        DesktopChecks.Check("Controller_StationVerbs_TableMenuAndReasons", () =>
        {
            var edit = CommandTable.MenuFor("Edit");
            var add = edit.FirstOrDefault(row => row.Id == "station.add");
            var remove = edit.FirstOrDefault(row => row.Id == "station.remove");
            Require(add is not null && add.Title == "Add station…", "Edit has no 'Add station…' row (station.add)");
            Require(remove is not null && remove.Title == "Remove station", "Edit has no 'Remove station' row (station.remove)");
            using var controller = Open(ThreeSame());
            controller.Select(new Selection.Station(0, 0));
            Require(controller.RemoveStationReason(0) == "Root and tip stations cannot be removed.", "Root remove reason: " + controller.RemoveStationReason(0));
            Require(controller.RemoveStationReason(2) == "Root and tip stations cannot be removed.", "Tip remove reason: " + controller.RemoveStationReason(2));
            Require(controller.RemoveStationReason(1) is null, "Remove of an interior station is refused: " + controller.RemoveStationReason(1));
            Require(controller.AddStationReason is null, "Add is refused with no draft: " + controller.AddStationReason);
            controller.EnterSectionAsync(1, EntryOrigin.Properties).GetAwaiter().GetResult();
            Require(controller.AddStationReason == "Apply or cancel the open draft on Station 2 first.", "Draft reason for Add: " + controller.AddStationReason);
            Require(controller.RemoveStationReason(1) == "Apply or cancel the open draft on Station 2 first.", "Draft reason for Remove: " + controller.RemoveStationReason(1));
        });

        // Design 5.2 (review MC-2): the pointer snap is 0.1 mm of distance and the nudge steps are 0.01 / 0.1 / 1 mm in every length unit.
        DesktopChecks.Check("Controller_SliceSnapAndNudge_AreMillimetreSteps_InEveryUnit", () =>
        {
            foreach (var (label, sections, tolerance) in new[]
            {
                ("percent", "at root profile \"a\" at 60 % profile \"a\" at tip profile \"a\"", 1e-7),
                ("mm", "at root profile \"a\" at 270 mm profile \"a\" at tip profile \"a\"", 1e-7),
                ("cm", "at root profile \"a\" at 27 cm profile \"a\" at tip profile \"a\"", 1e-7),
                ("m", "at root profile \"a\" at 0.27 m profile \"a\" at tip profile \"a\"", 1e-7)
            })
            {
                using var controller = Open(Doc(sections, 120, ("a", A)));
                double half = controller.Inspection!.Authored.HalfSpanMeters ?? throw new Exception("no half span");
                double Metres() => ((Selection.Slice)controller.Selection).Eta * half;
                controller.PlaceSlice(0.35123);
                double snapped = Metres();
                double expectedMillimetres = label == "percent" ? 0.351 * half * 1000 : 158.1;   // 0.1 % in a percent document, 0.1 mm of distance in a length document
                Require(Math.Abs(snapped * 1000 - expectedMillimetres) < tolerance * 1000, label + " snap: the slice is at " + snapped * 1000 + " mm, expected " + expectedMillimetres);
                foreach (var (modifier, step) in new[] { (NudgeModifier.Command, 0.00001), (NudgeModifier.Plain, 0.0001), (NudgeModifier.Shift, 0.001) })
                {
                    double before = Metres();
                    controller.NudgeSlice(1, modifier);
                    Require(Math.Abs(Metres() - before - step) < 1e-8, label + " nudge " + modifier + ": moved " + (Metres() - before) * 1000 + " mm, expected " + step * 1000);
                    controller.NudgeSlice(-1, modifier);
                    Require(Math.Abs(Metres() - before) < 1e-8, label + " nudge back " + modifier + ": at " + Metres() * 1000 + " mm, expected " + before * 1000);
                }
            }
        });

        // Review UA-4: a flyout belongs to the revision it measured; opening another document or an Undo closes it.
        DesktopChecks.Check("Controller_Flyout_ClosesOnOpenAndOnUndo", () =>
        {
            using var controller = Open(Three());
            controller.BeginRemoveStationAsync(1).GetAwaiter().GetResult();
            Require(controller.Flyout is not null, "Remove opened no flyout");
            controller.OpenFoilAsync(ThreeSame(), "Another fixture").GetAwaiter().GetResult();
            Require(controller.Flyout is null, "The Remove flyout survived opening another document");

            using var edited = Open(ThreeSame());
            edited.PlaceSliceTyped("35");
            edited.PromoteSliceAsync().GetAwaiter().GetResult();
            Require(StationCount(edited) == 4, "The promotion did not add a station");
            edited.BeginRemoveStationAsync(1).GetAwaiter().GetResult();
            Require(edited.Flyout is not null, "Remove opened no flyout after the promotion");
            edited.Undo();
            Require(edited.Flyout is null, "The Remove flyout survived Undo");
        });

        DesktopChecks.Check("Controller_TypedCoincidentEta_SelectsExisting", () =>
        {
            using var controller = Open(ThreeSame());
            controller.PlaceSliceTyped("60");
            Require(controller.Selection is Selection.Station { Index: 1, Eta: 0.6 }, "A typed 60 % did not select the station there: " + controller.Selection);
            Require(controller.Status == "Station 2 is already at 270.0 mm (within the 10 µm join tolerance). Selected it.", "Status: " + controller.Status);
            controller.PlaceSliceTyped("35");
            Require(controller.Selection is Selection.Slice { Eta: 0.35 }, "A typed 35 % did not place a slice at 0.35: " + controller.Selection);
            Require(controller.Status == "Inspection slice at η 35.0 % · 157.5 mm. Not saved. Promote makes it a station.", "Status: " + controller.Status);
            Require(StationCount(controller) == 3, "Placing a slice changed the document");
            controller.ClearSlice();
            Require(controller.Selection is Selection.Foil, "Clearing the slice left " + controller.Selection);
        });

        DesktopChecks.Check("Controller_ApproximatePromotion_FlyoutAndApply", () =>
        {
            using var near = Open(NearPair());
            near.PlaceSliceTyped("50");
            near.PromoteSliceAsync().GetAwaiter().GetResult();
            var flyout = near.Flyout ?? throw new Exception("An R2 promotion opened no approximate-promotion flyout; status: " + near.Status);
            Require(flyout.Kind == WorkbenchController.StationFlyoutKind.Promote && flyout.Title == "Approximate promotion at η 50.0 % · 225.0 mm", "Title: " + flyout.Title);
            Require(flyout.Line.StartsWith("A station here changes the loft. Largest change ", StringComparison.Ordinal) &&
                    flyout.Line.Contains("sampled on 17 η × 201 x, not a bound. Limit 10 µm at the local chord.", StringComparison.Ordinal), "Line: " + flyout.Line);
            Console.WriteLine("INFO near pair at 120 mm: " + flyout.Line);
            Require(flyout.ApplyEnabled && flyout.ApplyReason is null, "Apply is disabled at a few micrometres: " + flyout.ApplyReason);
            Require(StationCount(near) == 2, "The preview added a row");
            near.ApplyFlyoutAsync().GetAwaiter().GetResult();
            Require(StationCount(near) == 3 && near.Flyout is null, "Apply did not add one station");
            Require(near.Selection is Selection.Station { Index: 1, Eta: 0.5 }, "The new station is not selected: " + near.Selection);
            Require(near.Status.StartsWith("Added Station 2 at η 50.0 % as an approximate promotion. Largest change ", StringComparison.Ordinal) &&
                    near.Status.EndsWith(". Thickness: keep current.", StringComparison.Ordinal), "Receipt: " + near.Status);

            using var big = Open(NearPair(2400));
            big.PlaceSliceTyped("50");
            big.PromoteSliceAsync().GetAwaiter().GetResult();
            var over = big.Flyout ?? throw new Exception("No flyout above 10 um");
            Console.WriteLine("INFO near pair at 2400 mm: " + over.Line);
            Require(!over.ApplyEnabled && over.ApplyReason == "Apply is disabled: the change is above the 10 µm limit.", "Apply above the limit: " + over.ApplyEnabled + " " + over.ApplyReason);
            big.ApplyFlyoutAsync().GetAwaiter().GetResult();
            Require(StationCount(big) == 2, "Apply above the limit added a station");
            big.CancelFlyout();
            Require(big.Flyout is null && big.Selection is Selection.Slice, "Cancel did not keep the slice and close the flyout");
        });

        DesktopChecks.Check("Controller_RemoveFlyout_ApplyDisabledWithReason", () =>
        {
            using var controller = Open(Three());
            controller.BeginRemoveStationAsync(1).GetAwaiter().GetResult();
            var flyout = controller.Flyout ?? throw new Exception("Remove opened no flyout; status: " + controller.Status);
            Require(flyout.Kind == WorkbenchController.StationFlyoutKind.Remove && flyout.Title == "Remove Station 2", "Title: " + flyout.Title);
            Require(flyout.Line.StartsWith("The shape between Root and Tip is re-blended. Largest change ", StringComparison.Ordinal) &&
                    flyout.Line.EndsWith(", sampled. Limit 10 µm.", StringComparison.Ordinal), "Line: " + flyout.Line);
            Require(!flyout.ApplyEnabled && flyout.ApplyReason == "Apply is disabled: the change is above the 10 µm limit.", "Apply: " + flyout.ApplyEnabled + " " + flyout.ApplyReason);
            controller.ApplyFlyoutAsync().GetAwaiter().GetResult();
            Require(StationCount(controller) == 3, "Apply above the limit removed the station");
            controller.CancelFlyout();

            using var same = Open(ThreeSame());
            same.BeginRemoveStationAsync(1).GetAwaiter().GetResult();
            Require(same.Flyout is { ApplyEnabled: true }, "Apply is disabled for a removal that changes nothing");
            same.ApplyFlyoutAsync().GetAwaiter().GetResult();
            Require(StationCount(same) == 2 && same.Flyout is null, "Apply did not remove the station");
            Require(same.Selection is Selection.Station { Index: 0, Eta: 0 }, "The left neighbour is not selected: " + same.Selection);
            Require(same.Status.StartsWith("Removed Station 2. Largest change ", StringComparison.Ordinal) && same.Status.EndsWith(". Thickness: keep current.", StringComparison.Ordinal), "Receipt: " + same.Status);
        });

        // Promotion cost (S1-a: about 10 s a plan on the production grid). Red first: a preview run on the caller's thread blocks it.
        DesktopChecks.Check("Controller_PromoteSlice_PreviewOffUiThread_WindowStaysResponsive", () =>
        {
            using var controller = Open(NearPair(), production: true);
            controller.PlaceSliceTyped("50");
            var clock = Stopwatch.StartNew();
            var task = controller.PromoteSliceAsync();
            long returned = clock.ElapsedMilliseconds;
            Require(returned < 1500, $"PromoteSliceAsync held the calling thread for {returned} ms: the preview ran on it");
            Require(!task.IsCompleted && controller.SliceChecking, "The press shows no Checking state while the preview runs");
            long ticks = 0, worstGap = 0, last = clock.ElapsedMilliseconds;
            while (!task.IsCompleted)
            {
                Dispatcher.UIThread.Post(() => ticks++);
                Dispatcher.UIThread.RunJobs();
                long now = clock.ElapsedMilliseconds;
                worstGap = Math.Max(worstGap, now - last);
                last = now;
                Thread.Sleep(10);
            }
            task.GetAwaiter().GetResult();
            Console.WriteLine($"INFO preview-off-thread returned in {returned} ms, {ticks} dispatcher ticks during the plan, worst gap {worstGap} ms, total {clock.ElapsedMilliseconds} ms");
            Require(ticks >= 20 && worstGap < 400, $"The dispatcher was starved during the preview: {ticks} ticks, worst gap {worstGap} ms");
            Require(!controller.SliceChecking && controller.Flyout is not null, "The press left Checking without a flyout");
        });

        DesktopChecks.Check("Controller_PromoteApply_ReusesStoredPlan_NoSecondMeasurement", () =>
        {
            using var controller = Open(NearPair(), production: true);
            controller.PlaceSliceTyped("50");
            controller.PromoteSliceAsync().GetAwaiter().GetResult();
            controller.ApplyFlyoutAsync().GetAwaiter().GetResult();
            Require(StationCount(controller) == 3, "Apply did not add the station");
            var events = controller.LocalEvents;
            double preview = events.Last(item => item.Operation == "document.preview-promote").DurationMilliseconds ?? 0;
            double add = events.Last(item => item.Operation == "document.add-station").DurationMilliseconds ?? double.MaxValue;
            Console.WriteLine($"INFO plan-reuse preview-promote {preview:F0} ms, add-station {add:F0} ms");
            Require(preview > 1000, $"The preview measured nothing ({preview:F0} ms)");
            Require(add < preview / 4, $"Apply measured again: add-station {add:F0} ms against the preview's {preview:F0} ms");
        });
    }
}
