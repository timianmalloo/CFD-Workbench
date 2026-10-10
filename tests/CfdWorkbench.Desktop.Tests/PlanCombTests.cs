using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

// Rail comb, Desktop layer (docs/design/rail-comb.md section 9, C1 to C7, C10, C11). Ring: fast (headless Avalonia, one shell
// window per check); the screenshot check writes a file only when CFD_PROOF_PNG names one.
public static partial class PlanCanvasTests
{
    // The example foil has straight rails; the comb needs curvature. A swept leading edge and a tapering trailing edge, with an
    // optional wobble in the trailing edge, as the mockup's example, wobble and straight planforms.
    private static byte[] CombFoil(bool wobble = false, double scale = 1)
    {
        string source = File.ReadAllText("docs/examples/foildsl/foil-basic.foil");
        string[] lines = source.Split('\n');
        double[] eta = [0, 0.1, 0.3, 0.5, 0.7, 0.9, 1];
        double[] sweep = [0, 0, 4, 14, 30, 50, 60];
        double[] taper = wobble ? [0, 0, 2, 22, 14, 30, 40] : [0, 0, 2, 10, 20, 30, 40];
        string Points(double[] deviation, double baseline) => string.Join(", ", eta.Select((x, i) =>
            "(" + x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ", " +
            (baseline + scale * deviation[i]).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ")"));
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("leading cv {", StringComparison.Ordinal))
                lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"points \[(.*?)\] \}", $"points [{Points(sweep, 0)}] }}");
            else if (trimmed.StartsWith("trailing cv {", StringComparison.Ordinal))
                lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"points \[(.*?)\] \}", $"points [{Points(taper, 120)}] }}");
        }
        return FoilSource.MaterializeIds(FoilSource.Parse(System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines))));
    }

    private static PlanFixture CombFixture(bool wobble = false, double width = 1280, bool comb = true, Avalonia.Styling.ThemeVariant? theme = null)
    {
        var fixture = new PlanFixture(source: CombFoil(wobble), width: width, theme: theme);
        fixture.Controller.CombVisible = comb;
        fixture.Settle();
        return fixture;
    }

    private static CombPlate PlateOf(PlanFixture fixture) => fixture.Host.ModelView.CombPlate;
    private static T Part<T>(PlanFixture fixture, string name) where T : Control => PlateOf(fixture).FindControl<T>(name)!;
    private static Control? Focused(PlanFixture fixture) => TopLevel.GetTopLevel(fixture.Window)?.FocusManager?.GetFocusedElement() as Control;
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void SelectRailPoint(PlanFixture fixture, string curve, int index)
    {
        var point = curve == "leading" ? fixture.Controller.Planform!.Leading.Points[index] : fixture.Controller.Planform!.Trailing.Points[index];
        fixture.Canvas.SelectPoint(new PointRef(point.Curve, point.Id), false, false);
        fixture.Settle();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    private static double ContrastRatio(Color a, Color b)
    {
        double la = RelativeLuminance(a), lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static Color ColorOf(object? brush) => (brush as ISolidColorBrush)?.Color ?? throw new Exception("Not a solid brush: " + brush);

    private static void RunComb()
    {
        DesktopChecks.Check("PlanComb_BothRails_NothingSelected", () =>
        {
            using var fixture = CombFixture(comb: false);
            Require(fixture.Controller.Selection is Selection.Foil or Selection.None, "A selection is open");
            var tipPixels = new List<(string Rail, Point Mid)>();
            var frame = (RailCombFrame?)null;
            // Pixels at the middle of each rail's longest tooth, comb off then on.
            fixture.Controller.CombVisible = true;
            fixture.Settle();
            frame = fixture.Controller.Comb ?? throw new Exception("No comb frame while Curvature is on");
            Require(frame.Leading.Count >= 32 && frame.Trailing.Count >= 32, "A rail has fewer than 32 teeth");
            double perMetre = fixture.Controller.CombPerMetre;
            foreach (var (name, teeth) in new[] { ("leading", frame.Leading), ("trailing", frame.Trailing) })
            {
                var tooth = teeth.OrderByDescending(item => Math.Abs(item.Curvature)).First();
                double length = Math.Min(Math.Abs(tooth.Curvature) * 30 / perMetre, 60);
                Require(length >= 10, $"{name}: longest tooth is only {length:F1} px");
                var foot = fixture.Canvas.OutlineScreenPoint(new PlanSample(tooth.SpanMeters, tooth.Ordinate));
                tipPixels.Add((name, foot + new Vector(tooth.DirSpan, tooth.DirAft) * (length / 2)));
            }
            var on = tipPixels.Select(item => Neighbourhood(fixture, item.Mid)).ToArray();
            fixture.Controller.CombVisible = false;
            fixture.Settle();
            var off = tipPixels.Select(item => Neighbourhood(fixture, item.Mid)).ToArray();
            for (int i = 0; i < on.Length; i++)
                Require(!on[i].SequenceEqual(off[i]), $"{tipPixels[i].Rail}: no tooth pixels with nothing selected");
        });

        DesktopChecks.Check("PlanComb_AutoHeldDuringDrag_RefitOnRelease", () =>
        {
            using var fixture = CombFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[4];
            double before = fixture.Controller.CombAuto;
            Require(before > 0 && fixture.Controller.CombGain is null, "Auto is not fitted before the drag");
            fixture.BeginDrag(point);
            fixture.MoveDrag(point, 0, 70);
            Require(fixture.Controller.Gesture is GestureState.Pressed or GestureState.Dragging, "No drag in progress");
            Require(fixture.Controller.CombAuto == before, "Auto refit while the drag was still going");
            fixture.ReleaseDrag(point, 0, 70);
            fixture.WaitGesture();
            fixture.Settle();
            Require(fixture.Controller.CombAuto != before, "Auto did not refit on release");
        });

        DesktopChecks.Check("PlanComb_Density_ChangesToothCountOnly_FoilBytesUnchanged", () =>
        {
            using var fixture = CombFixture();
            string source = fixture.Controller.AcceptedSource;
            string hash = fixture.Controller.Planform!.SourceHash;
            Require(fixture.Controller.Comb!.Leading.Count is >= 32 and < 40, $"Density 32 drew {fixture.Controller.Comb!.Leading.Count} teeth");
            Click(Part<Button>(fixture, "DenserButton"));
            fixture.Settle();
            Require(fixture.Controller.CombDensity == 64 && fixture.Controller.Comb!.Leading.Count is >= 64 and < 72, "Denser did not give 64 per rail");
            Click(Part<Button>(fixture, "SparserButton"));
            Click(Part<Button>(fixture, "SparserButton"));
            fixture.Settle();
            Require(fixture.Controller.CombDensity == 16 && fixture.Controller.Comb!.Leading.Count is >= 16 and < 24, "Sparser did not give 16 per rail");
            Require(fixture.Controller.AcceptedSource == source && fixture.Controller.Planform!.SourceHash == hash, "Density changed the foil");
            Require(!fixture.Controller.CanUndo, "Density entered the undo stack");
            // Instrumentation: the comb's recompute against the 100 ms edit budget, measured, never asserted (a wall clock is not a gate).
            var plan = fixture.Controller.Planform!;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var frame = RailComb.Build(plan, 128);
            watch.Stop();
            Console.WriteLine($"MEASURE comb_build_ms={watch.Elapsed.TotalMilliseconds:F1} density=128 teeth={frame.Leading.Count + frame.Trailing.Count} pieces={frame.LeadingPieces.Count}+{frame.TrailingPieces.Count}");
        });

        DesktopChecks.Check("PlanComb_Analysis_NotDrawnToggleDisabled", () =>
        {
            using var fixture = CombFixture();
            Require(PlateOf(fixture).IsVisible && fixture.Controller.Comb is not null, "No plate or comb in CAD");
            fixture.Controller.ToggleAnalysis();
            fixture.Settle();
            Require(fixture.Controller.IsAnalysis, "Analysis did not open");
            Require(fixture.Controller.Comb is null && !PlateOf(fixture).IsVisible, "The comb or plate shows in Analysis");
            Require(fixture.Host.CommandReason("view.comb") == "Curvature is shown in CAD.", "Analysis gives no reason for the toggle");
            fixture.Controller.ToggleAnalysis();
            fixture.Settle();
            Require(fixture.Controller.Comb is not null && PlateOf(fixture).IsVisible, "Scale and density were not remembered");
        });

        DesktopChecks.Check("PlanComb_CurvatureToggle_InAnalysis_StaysFocusable", () =>
        {
            using var fixture = CombFixture();
            fixture.Controller.ToggleAnalysis();
            fixture.Settle();
            Require(fixture.Host.CanRun("view.comb"), "The toggle is disabled in Analysis (focus would be lost)");
            fixture.Host.RunCommand("view.comb").GetAwaiter().GetResult();
            Require(fixture.Controller.CombVisible, "The toggle changed the comb in Analysis");
            Require(fixture.Host.StatusStrip.Text.Contains("Curvature is shown in CAD.", StringComparison.Ordinal), "No reason was reported: " + fixture.Host.StatusStrip.Text);
        });

        DesktopChecks.Check("PlanComb_StepperAtLimit_KeepsFocus", () =>
        {
            using var fixture = CombFixture();
            var larger = Part<Button>(fixture, "LargerButton");
            while (fixture.Controller.CanStepScale(larger: true)) Click(larger);
            fixture.Settle();
            double gain = fixture.Controller.CombPerMetre;
            Require(gain == RailComb.Gains[0], $"Larger teeth ended at {gain}");
            Require(larger.Focus(), "Larger teeth takes no focus");
            Click(larger);
            fixture.Settle();
            Require(ReferenceEquals(Focused(fixture), larger), "Focus left the stepper at its limit: " + Focused(fixture));
            Require(larger.IsEnabled && larger.Classes.Contains("limit"), "The limit stepper is natively disabled, or not marked");
            Require(PlateOf(fixture).LiveAnnouncement == "Largest teeth reached." && fixture.Controller.CombPerMetre == gain, "No limit announcement, or the gain moved");
            Require((AutomationProperties.GetHelpText(larger) ?? "").Contains("Largest teeth.", StringComparison.Ordinal), "No reason on the stepper");
            var denser = Part<Button>(fixture, "DenserButton");
            while (fixture.Controller.CanStepDensity(denser: true)) Click(denser);
            Require(denser.Focus(), "Denser takes no focus");
            Click(denser);
            fixture.Settle();
            Require(ReferenceEquals(Focused(fixture), denser) && PlateOf(fixture).LiveAnnouncement == "Densest density reached.", "Density limit lost focus or its announcement");
        });

        DesktopChecks.Check("PlanProbe_RadiusInProjectUnit", () =>
        {
            Require(RailComb.Radius(0.312) == "312 mm" && RailComb.Radius(0.0312) == "31.2 mm" && RailComb.Radius(9.999) == "9999 mm"
                && RailComb.Radius(10) == "10.00 m", "Radius does not follow the millimetre rule");
            Require(RailComb.Kappa(3.2) == "κ +3.20 per m" && RailComb.Kappa(-0.52) == "κ −0.52 per m" && RailComb.Kappa(0) == "κ 0", "Curvature text");
            using var fixture = CombFixture();
            var point = fixture.Controller.Planform!.Trailing.Points[3];
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(point) + new Vector(0, 40));
            fixture.Settle();
            string text = fixture.Host.ModelView.TracingStrip.Text ?? "";
            Require(text.StartsWith("At pointer:", StringComparison.Ordinal) && text.Contains(" · LE R ", StringComparison.Ordinal)
                && text.Contains(" · TE ", StringComparison.Ordinal) && text.Contains(" per m ", StringComparison.Ordinal) && text.Contains(" mm · κ ", StringComparison.Ordinal),
                "The strip lacks the radius of both rails in mm and curvature per metre: " + text);
        });

        DesktopChecks.Check("PlanProbe_StripStableOnLeave", () =>
        {
            using var fixture = CombFixture();
            SelectRailPoint(fixture, "trailing", 4);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[4].Id));
            fixture.Settle();
            var strip = fixture.Host.ModelView.TracingStrip;
            string selected = strip.Text ?? "";
            Require(selected.StartsWith("Point 5: TE ", StringComparison.Ordinal) && selected.Contains("at point 5's station on the curve", StringComparison.Ordinal),
                "No selected-point reading: " + selected);
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(fixture.Controller.Planform!.Leading.Points[3]) + new Vector(0, 30));
            fixture.Settle();
            Require((strip.Text ?? "").StartsWith("At pointer:", StringComparison.Ordinal), "Hover did not take the strip");
            fixture.Canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, fixture.Canvas, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
                fixture.Window, new Point(-100, -100), 1, default, KeyModifiers.None));
            fixture.Settle();
            Require(strip.Text == selected, "The strip did not return to the selected reading: " + strip.Text);
        });

        DesktopChecks.Check("PlanProbe_Strip_IsOneElement", () =>
        {
            using var fixture = CombFixture();
            var strip = fixture.Host.ModelView.TracingStrip;
            var before = strip;
            SelectRailPoint(fixture, "leading", 3);
            fixture.Canvas.FocusPoint(new PointRef("leading", fixture.Controller.Planform!.Leading.Points[3].Id));
            fixture.KeyDown(Key.OemCloseBrackets);
            fixture.Canvas.HoverAt(new Point(300, 300));
            fixture.Settle();
            Require(ReferenceEquals(before, fixture.Host.ModelView.TracingStrip) && strip.IsEffectivelyVisible, "The strip was rebuilt");
            Require(strip.GetValue(AutomationProperties.NameProperty) == "Tracing", "The strip has no name");
        });

        DesktopChecks.Check("PlanProbe_Strip_QuietOnPointerMoveAndNudge", () =>
        {
            using var fixture = CombFixture();
            var strip = fixture.Host.ModelView.TracingStrip;
            SelectRailPoint(fixture, "leading", 3);
            fixture.Canvas.FocusPoint(new PointRef("leading", fixture.Controller.Planform!.Leading.Points[3].Id));
            fixture.KeyDown(Key.OemCloseBrackets);
            Require(AutomationProperties.GetLiveSetting(strip) == AutomationLiveSetting.Polite, "Walking with ] is not polite");
            fixture.Canvas.HoverAt(new Point(300, 300));
            fixture.Settle();
            Require(AutomationProperties.GetLiveSetting(strip) == AutomationLiveSetting.Off, "A pointer move is announced");
            fixture.KeyDown(Key.OemCloseBrackets);
            Require(AutomationProperties.GetLiveSetting(strip) == AutomationLiveSetting.Polite, "The second walk step is not polite");
            fixture.KeyDown(Key.Down);
            fixture.KeyUp(Key.Down);
            fixture.WaitGesture();
            Require(AutomationProperties.GetLiveSetting(strip) == AutomationLiveSetting.Off, "A nudge is announced");
        });

        DesktopChecks.Check("PlanProbe_SelectedReading_ReturnsOnFocusOut", () =>
        {
            using var fixture = CombFixture();
            SelectRailPoint(fixture, "trailing", 4);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[4].Id));
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(fixture.Controller.Planform!.Leading.Points[2]) + new Vector(0, 30));
            fixture.Settle();
            var strip = fixture.Host.ModelView.TracingStrip;
            Require((strip.Text ?? "").StartsWith("At pointer:", StringComparison.Ordinal), "No hover reading to leave");
            Part<Button>(fixture, "SmallerButton").Focus();
            fixture.Settle();
            Require((strip.Text ?? "").StartsWith("Point 5:", StringComparison.Ordinal), "Focus left the canvas but the strip stayed on: " + strip.Text);
        });

        DesktopChecks.Check("PlanComb_TeethPerpendicularToTangent_AtThreeZooms", () =>
        {
            using var fixture = CombFixture();
            var plan = fixture.Controller.Planform!;
            foreach (double factor in new[] { 1d, 2d, 0.5d })
            {
                fixture.Controller.PlanCamera = new PlanCamera { PixelsPerMeter = 1000 * factor };
                fixture.Settle();
                // Equal scale: a metre along the span and a metre along aft map to the same number of pixels (true normals).
                var origin = fixture.Canvas.OutlineScreenPoint(new PlanSample(0.2, 0.05));
                var along = fixture.Canvas.OutlineScreenPoint(new PlanSample(0.3, 0.05)) - origin;
                var across = fixture.Canvas.OutlineScreenPoint(new PlanSample(0.2, 0.15)) - origin;
                Require(Math.Abs(along.X - across.Y) < 1e-9 && Math.Abs(along.Y) < 1e-9 && Math.Abs(across.X) < 1e-9, $"Unequal scale at x{factor}");
                foreach (var (rail, teeth) in new[] { (plan.Leading, fixture.Controller.Comb!.Leading), (plan.Trailing, fixture.Controller.Comb!.Trailing) })
                    foreach (var tooth in teeth.Where(item => Math.Abs(item.Curvature) > 0))
                    {
                        double h = 1e-5;
                        var before = Planform.CurvatureAt(rail, Math.Max(0, tooth.T - h), CurveSide.Before);
                        var after = Planform.CurvatureAt(rail, Math.Min(1, tooth.T + h), CurveSide.After);
                        double tx = after.SpanMeters - before.SpanMeters, ty = after.Aft - before.Aft, length = Math.Sqrt(tx * tx + ty * ty);
                        double angle = Math.Acos(Math.Clamp((tx * tooth.DirSpan + ty * tooth.DirAft) / length, -1, 1)) * 180 / Math.PI;
                        Require(Math.Abs(angle - 90) < 0.5, $"Tooth at t {tooth.T:F3} is {angle:F2} degrees from the tangent at x{factor}");
                    }
            }
        });

        DesktopChecks.Check("PlanComb_EnvelopePerPiece_StepAtJump", () =>
        {
            // Two cubic Beziers joined at (0.5, 0.1) with a curvature jump: the one-sided pair is kept, and the second tooth starts a new piece.
            var points = new (double X, double Y)[] { (0, 0), (0.1, 0.05), (0.3, 0.1), (0.5, 0.1), (0.7, 0.1), (0.75, 0.28), (1, 0.28) };
            var rail = new CurveView("leading", 10, new double[] { 0, 0, 0, 0, 0.5, 0.5, 0.5, 1, 1, 1, 1 },
                points.Select((p, i) => new PointView("leading", "p" + i, i, p.X, p.X, p.Y, PointRole.Control, null, null, PointFreedom.Free, [])).ToArray(), []);
            var teeth = Planform.Teeth(rail, 16);
            var marks = RailComb.Marks(teeth, 5, tooth => (tooth.SpanMeters * 1000, tooth.Ordinate * 1000));
            Require(marks.Count(mark => mark.Tooth.StartsPiece) == 2, "The jump did not start a second piece");
            int second = marks.ToList().FindIndex(mark => mark.Tooth.StartsPiece && mark.Tooth.T == 0.5);
            Require(second > 0 && marks[second - 1].Tooth.T == 0.5, "The one-sided pair at the jump was thinned");
            var a = marks[second - 1];
            var b = marks[second];
            double tipA = a.Length * Math.Sign(a.Tooth.Curvature), tipB = b.Length * Math.Sign(b.Tooth.Curvature);
            Require(Math.Abs(tipA - tipB) > 1, "The two one-sided tips are level: there is no step");
        });

        DesktopChecks.Check("PlanCanvas_FocusRing_UsesViewportToken", () =>
        {
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                using var fixture = CombFixture(theme: theme);
                var ring = ColorOf(fixture.Canvas.FocusBrush);
                Require(ring == Color.Parse("#66ddc8"), $"{theme}: the plan focus ring is {ring}, not focus-ring-viewport");
                var viewport = ColorOf(fixture.Canvas.BackgroundBrush);
                Require(ContrastRatio(ring, viewport) >= 4.5, $"{theme}: the plan ring is under 4.5:1 on the viewport");
                var accent = ColorOf(Application.Current!.FindResource(theme, "PrimaryBrush"));
                Require(ring != accent, $"{theme}: the plan ring is the accent");
            }
        });

        DesktopChecks.Check("PlanComb_PlateFocusRing_UsesSurfaceToken", () =>
        {
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                using var fixture = CombFixture(theme: theme);
                var ring = ColorOf(Application.Current!.FindResource(theme, "FocusRingBrush"));
                var surface = ColorOf(Application.Current!.FindResource(theme, "SurfaceBrush"));
                var card = PlateOf(fixture).FindControl<Border>("Card")!;
                Require(ColorOf(card.Background) == surface, $"{theme}: the plate card is not on the surface token");
                Require(ContrastRatio(ring, surface) >= 4.5, $"{theme}: the plate focus ring is under 4.5:1 on the surface");
                Require(ring != Color.Parse("#66ddc8"), $"{theme}: the plate uses the viewport ring");
            }
        });

        DesktopChecks.Check("PlanCanvas_Tab_VisitsPlateBeforeProperties", () =>
        {
            using var fixture = CombFixture();
            SelectRailPoint(fixture, "trailing", 3);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[3].Id));
            Require(fixture.KeyDown(Key.Tab), "Tab in the plan was not handled");
            Require(ReferenceEquals(Focused(fixture), Part<Button>(fixture, "SmallerButton")), "Tab did not land on the plate: " + Focused(fixture));
            var denser = Part<Button>(fixture, "DenserButton");
            denser.Focus();
            denser.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = denser, Key = Key.Tab });
            fixture.Settle();
            Require(fixture.Host.Properties.IsKeyboardFocusWithin && !PlateOf(fixture).IsKeyboardFocusWithin, "The plate's last control did not hand on to Properties");
        });

        DesktopChecks.Check("PlanCanvas_ShiftTab_ReturnsToCanvas", () =>
        {
            using var fixture = CombFixture();
            SelectRailPoint(fixture, "trailing", 3);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[3].Id));
            fixture.KeyDown(Key.Tab);
            var smaller = Part<Button>(fixture, "SmallerButton");
            Require(ReferenceEquals(Focused(fixture), smaller), "Tab did not reach the plate");
            smaller.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = smaller, Key = Key.Tab, KeyModifiers = KeyModifiers.Shift });
            fixture.Settle();
            Require(fixture.Canvas.IsFocused, "Shift+Tab from the plate did not return to the plan: " + Focused(fixture));
        });

        DesktopChecks.Check("PlanCanvas_PlateHidden_TabGoesToProperties", () =>
        {
            using var fixture = CombFixture(comb: false);
            Require(!PlateOf(fixture).IsVisible, "The plate shows with Curvature off");
            SelectRailPoint(fixture, "trailing", 3);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[3].Id));
            Require(fixture.KeyDown(Key.Tab), "Tab in the plan was not handled");
            Require(fixture.Host.Properties.IsKeyboardFocusWithin, "Tab with the plate hidden did not reach Properties");
        });

        DesktopChecks.Check("PlanComb_PaletteVerbs_AnnounceNewValue", () =>
        {
            using var fixture = CombFixture();
            var live = Part<TextBlock>(fixture, "LiveText");
            Require(AutomationProperties.GetLiveSetting(live) == AutomationLiveSetting.Polite, "The plate's live region is not polite");
            double before = fixture.Controller.CombPerMetre;
            double expected = RailComb.Gains.Where(gain => gain < before - 1e-9).Max();
            fixture.Host.RunCommand("view.comb-larger").GetAwaiter().GetResult();
            fixture.Settle();
            Require(live.Text == $"Comb scale 30 px = {RailComb.Gain(expected)} per m.", "Larger teeth said: " + live.Text);
            fixture.Host.RunCommand("view.comb-denser").GetAwaiter().GetResult();
            fixture.Settle();
            Require(live.Text == "Comb density 64 per rail.", "Denser said: " + live.Text);
            fixture.Host.RunCommand("view.comb-auto").GetAwaiter().GetResult();
            Require(fixture.Controller.CombGain is null, "Auto scale left a fixed step");
            fixture.Controller.CombVisible = false;
            fixture.Settle();
            fixture.Host.RunCommand("view.comb-sparser").GetAwaiter().GetResult();
            Require(fixture.Host.StatusStrip.Text == "Comb density 32 per rail.", "With the comb off the status strip did not carry it: " + fixture.Host.StatusStrip.Text);
            string[] titles = ["Comb: larger teeth", "Comb: smaller teeth", "Comb: auto scale", "Comb: denser", "Comb: sparser"];
            Require(titles.All(title => CommandTable.Rows.Any(row => row.Title == title && row.Id.StartsWith("view.comb-", StringComparison.Ordinal))), "A verb is missing from the command table");
        });

        DesktopChecks.Check("PlanComb_RefitFlash_OnceAndStaticUnderReducedMotion", () =>
        {
            using var fixture = CombFixture();
            var controller = fixture.Controller;
            void Open(double scale)
            {
                Task.Run(() => controller.OpenFoilAsync(CombFoil(scale: scale), "refit " + scale)).GetAwaiter().GetResult();
                fixture.Settle();
            }
            int flashes = controller.CombRefitFlashes;
            Open(0.25);   // the same planform at a quarter of the bend: the Auto gain moves by more than 2x
            Require(controller.CombRefitFlashes == flashes + 1 && PlateOf(fixture).LegendEmphasised && PlateOf(fixture).FlashTimers == 1, "A refit past 2x did not flash once");
            Require(controller.CombAnnouncement!.StartsWith("Comb scale 30 px = ", StringComparison.Ordinal), "A refit past 2x was not announced");
            Open(0.22);   // a refit of about 12 %
            Require(controller.CombRefitFlashes == flashes + 1, "A refit under 2x flashed");
            controller.EndCombEmphasis();
            Require(!PlateOf(fixture).LegendEmphasised, "The one flash did not end");
            controller.ReducedMotion = true;
            int timers = PlateOf(fixture).FlashTimers;
            Open(1);
            Require(controller.CombRefitFlashes == flashes + 2 && PlateOf(fixture).FlashTimers == timers, "Reduced motion started a flash timer");
            Require(PlateOf(fixture).LegendEmphasised, "Reduced motion has no static emphasis");
        });

        DesktopChecks.Check("PlanComb_StaleFlashTimer_DoesNotClearNewerEmphasis", () =>
        {
            using var fixture = CombFixture();
            var controller = fixture.Controller;
            void Open(double scale)
            {
                Task.Run(() => controller.OpenFoilAsync(CombFoil(scale: scale), "refit " + scale)).GetAwaiter().GetResult();
                fixture.Settle();
            }
            Open(0.25);   // the first flash: its end timer is now pending
            controller.EndCombEmphasis();   // the emphasis ends by another route; the timer has not fired
            controller.ReducedMotion = true;
            Open(1);   // a newer, static emphasis
            Require(PlateOf(fixture).LegendEmphasised, "Reduced motion has no static emphasis");
            PlateOf(fixture).RunPendingFlashEnd();   // the first timer fires now, held exactly after the newer emphasis
            Require(controller.CombRefitEmphasis && PlateOf(fixture).LegendEmphasised, "A stale flash timer cleared the newer emphasis");
        });

        DesktopChecks.Check("PlanComb_PlateDoesNotCoverRail_AtTipFit", () =>
        {
            foreach (var (name, source) in new (string, byte[])[] { ("example", CombFoil()), ("straight", CombFoil(scale: 0)), ("wobble", CombFoil(wobble: true)) })
            {
                using var fixture = new PlanFixture(source: source);
                fixture.Controller.CombVisible = true;
                fixture.Settle();
                var plate = PlateOf(fixture);
                var origin = plate.TranslatePoint(new Point(0, 0), fixture.Canvas) ?? throw new Exception("No plate position");
                var rect = new Rect(origin, plate.Bounds.Size).Inflate(4);
                var plan = fixture.Controller.Planform!;
                foreach (var rail in new[] { plan.Leading, plan.Trailing })
                    foreach (bool port in new[] { false, true })
                    {
                        var drawn = rail.Samples.Select(sample => fixture.Canvas.OutlineScreenPoint(sample, port))
                            .Concat(rail.Points.Select(point => fixture.Canvas.OutlineScreenPoint(new PlanSample(point.SpanMeters, point.Ordinate), port))).ToArray();
                        Require(!drawn.Any(rect.Contains), $"{name}: the plate {rect} covers the {rail.Curve} edge");
                    }
                var frame = fixture.Controller.Comb!;
                double perMetre = fixture.Controller.CombPerMetre;
                foreach (var tooth in frame.Leading.Concat(frame.Trailing))
                {
                    var foot = fixture.Canvas.OutlineScreenPoint(new PlanSample(tooth.SpanMeters, tooth.Ordinate));
                    double length = Math.Min(Math.Abs(tooth.Curvature) * 30 / Math.Max(perMetre, 1e-9), 60);
                    Require(!rect.Contains(foot + new Vector(tooth.DirSpan, tooth.DirAft) * length) && !rect.Contains(foot), $"{name}: the plate covers a tooth");
                }
            }
        });

        DesktopChecks.Check("PlanComb_NarrowPopover_EscapeClosesAndRestoresFocus", () =>
        {
            using var fixture = CombFixture(width: 1050);
            var plate = PlateOf(fixture);
            Require(plate.IsNarrow, $"The plate is not narrow at {fixture.Host.ModelView.PlanSlot.Bounds.Width} px");
            var summary = Part<Button>(fixture, "SummaryButton");
            Require(summary.IsVisible && summary.Content?.ToString() == "Comb · Auto · 32 per rail ▾", "The narrow summary is " + summary.Content);
            Click(summary);
            fixture.Settle();
            Require(plate.IsOpen && fixture.Host.ModelView.FindControl<Border>("CombDisclosure")!.IsVisible, "The summary did not open the disclosure");
            var denser = Part<Button>(fixture, "DenserButton");
            denser.Focus();
            denser.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = denser, Key = Key.Escape });
            fixture.Settle();
            Require(!plate.IsOpen && ReferenceEquals(Focused(fixture), summary), "Escape did not close and return to the summary: " + Focused(fixture));
        });

        DesktopChecks.Check("PlanComb_NarrowOpenPlate_DoesNotCoverTipOrStrip", () =>
        {
            using var fixture = CombFixture(width: 1050);
            var plate = PlateOf(fixture);
            Click(Part<Button>(fixture, "SummaryButton"));
            fixture.Settle();
            var area = fixture.Host.ModelView;
            var disclosure = area.FindControl<Border>("CombDisclosure")!;
            var strip = (Control)area.TracingStrip.Parent!;
            Point At(Control control) => control.TranslatePoint(new Point(0, 0), fixture.Window)!.Value;
            double canvasBottom = At(fixture.Canvas).Y + fixture.Canvas.Bounds.Height, disclosureTop = At(disclosure).Y, disclosureBottom = disclosureTop + disclosure.Bounds.Height;
            Require(disclosureTop >= canvasBottom - 0.5 && At(strip).Y >= disclosureBottom - 0.5, $"Viewport {canvasBottom}, plate {disclosureTop}-{disclosureBottom}, strip {At(strip).Y} are not in order");
            var rect = new Rect(plate.TranslatePoint(new Point(0, 0), fixture.Canvas)!.Value, plate.Bounds.Size).Inflate(4);
            var plan = fixture.Controller.Planform!;
            foreach (var rail in new[] { plan.Leading, plan.Trailing })
                Require(!rail.Samples.Any(sample => rect.Contains(fixture.Canvas.OutlineScreenPoint(sample))), "The open narrow plate's summary covers a rail");
        });

        DesktopChecks.Check("PlanComb_Screenshot_WhenAsked", () =>
        {
            if (Environment.GetEnvironmentVariable("CFD_PROOF_PNG") is not { Length: > 0 } png) return;
            using var fixture = CombFixture(width: 1500);
            SelectRailPoint(fixture, "trailing", 4);
            fixture.Canvas.FocusPoint(new PointRef("trailing", fixture.Controller.Planform!.Trailing.Points[4].Id));
            fixture.Canvas.HoverAt(fixture.Canvas.ScreenPoint(fixture.Controller.Planform!.Leading.Points[4]) + new Vector(0, 60));
            fixture.Settle();
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)fixture.Window.Bounds.Width, (int)fixture.Window.Bounds.Height), new Vector(96, 96));
            bitmap.Render(fixture.Window);
            bitmap.Save(png);
        });
    }

    private static (byte, byte, byte)[] Neighbourhood(PlanFixture fixture, Point centre)
    {
        var pixels = new List<(byte, byte, byte)>();
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                pixels.Add(fixture.RgbAtCanvas(centre.X + dx, centre.Y + dy));
        return pixels.ToArray();
    }
}
