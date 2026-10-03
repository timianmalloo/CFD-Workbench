using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// V3D (M1.2b2 §12.4): the 3D view — navigation verbs from pointer, trackpad and keyboard, the view cube and its peers,
/// station pick and chip, display modes, the sign fixture and <c>view.navigate.end</c>. Every navigation check compares
/// the controller's camera with the one <see cref="ViewCamera"/> function the verb names; rendered checks read pixels of
/// the realized window at projected model points (UI-RENDERED-STATE).
/// </summary>
public static class View3dTests
{
    public static void Run()
    {
        try
        {
            RenderedChecks();
            NavigationChecks();
            CubeChecks();
            SelectionChecks();
            AccessibilityChecks();
        }
        finally { Fixture.DisposeShared(); }
    }

    private static void RenderedChecks()
    {
        DesktopChecks.Check("View3d_DefaultIso_RenderedSurfaceCubeAndTriad", () =>
        {
            var fixture = Fixture.Shared();
            Equal(NamedCamera.Iso, fixture.Camera.Name, "default camera");
            Equal("3D · Iso", fixture.Area.ThreeDLabel.Content as string, "title");
            fixture.Shoot();
            var surface = fixture.Controller.Surface!;
            var (grazing, lit) = (fixture.Brush("PlanSoftBrush"), fixture.Brush("FoilShadeLitBrush"));
            var face = fixture.RgbAt(UpperFace(surface, 20, 40));
            if (!Between(face, grazing, lit) || face == fixture.Brush("ViewportBrush"))
                throw new Exception($"Shaded face {face} is not on the ramp {grazing} → {lit}");
            // The ground grid at z = min z: the y = 0 line ahead of the root leading edge, clear of the foil.
            var grid = fixture.Brush("ViewportGridBrush");
            var aft = new Point3(surface.MinimumX - 0.5 * (surface.MaximumX - surface.MinimumX), 0, surface.MinimumZ);
            var gridPixel = fixture.NearestTo(aft, grid, 1);
            // A 1 px anti-aliased line: the nearest pixel is much closer to the grid token than to the viewport.
            if (Distance(gridPixel, grid) * 2 > Distance(gridPixel, fixture.Brush("ViewportBrush"))) throw new Exception($"No ground-grid pixel at {fixture.ViewPoint(aft)}: nearest {gridPixel}, grid {grid}");
            // The cube: three faces at Iso, each a viewport-soft plate with an ink letter.
            var view = fixture.View;
            if (!view.CubeVisible || view.Faces.Count != 3) throw new Exception($"Iso cube shows {view.Faces.Count} faces");
            var soft = fixture.Brush("PlanSoftBrush");
            var ink = fixture.Brush("ViewportInkBrush");
            foreach (var cubeFace in view.Faces)
            {
                var fill = fixture.RgbAtView(Lerp(cubeFace.Centre, cubeFace.Corners[0], 0.55));
                if (Distance(fill, soft) > 12) throw new Exception($"Face {cubeFace.Letter} fill {fill}, not viewport-soft {soft}");
                if (Distance(fixture.NearestToView(cubeFace.Centre, ink, 4), ink) > 90) throw new Exception($"Face {cubeFace.Letter} has no letter");
            }
            // The axis triad bottom-left: a viewport-soft plate with three ink axes.
            var plate = view.TriadBounds;
            if (plate.X > 16 || plate.Bottom < view.Bounds.Height - 100) throw new Exception("Triad is not bottom-left: " + plate);
            Equal(soft, fixture.RgbAtView(plate.TopLeft + new Point(2, 2)), "triad plate");
            int inkPixels = 0;
            for (int y = (int)plate.Y; y < (int)plate.Bottom; y++)
                for (int x = (int)plate.X; x < (int)plate.Right; x++)
                    if (Distance(fixture.RgbAtView(new Point(x + 0.5, y + 0.5)), ink) < 60) inkPixels++;
            if (inkPixels < 40) throw new Exception($"Triad shows {inkPixels} ink pixels");
        });

        DesktopChecks.Check("View3d_DefaultIso_FromFrontFillsWidth", () =>
        {
            // DR-VIEW-5: Iso looks from the front, starboard and above (the approved mockup): the leading edge is nearer
            // the eye and the cube shows F, S and T. DR-VIEW-6: the fitted wing spans the view's width less the fit margin.
            var fixture = Fixture.Shared();
            var camera = fixture.Camera;
            var view = fixture.View;
            var surface = fixture.Controller.Surface!;
            Equal(NamedCamera.Iso, camera.Name, "Iso");
            Equal("F,S,T", string.Join(",", view.Faces.Select(face => face.Letter).Order()), "Iso cube faces");
            var root = surface.Sections[0];
            if (!(camera.Depth(root.Upper[0]) < camera.Depth(root.Upper[^1])))
                throw new Exception($"The leading edge is not nearer the eye: LE {camera.Depth(root.Upper[0]):F4} m, TE {camera.Depth(root.Upper[^1]):F4} m");
            double left = double.PositiveInfinity, right = double.NegativeInfinity;
            foreach (var section in surface.Sections)
                foreach (var point in section.Upper.Concat(section.Lower))
                    foreach (var placed in new[] { point, point.Port() })
                    {
                        double x = fixture.ViewPoint(placed).X;
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                    }
            double width = view.Bounds.Width, margin = ViewCamera.FitMarginPixels;
            // The fit holds the bounding box's corners inside the margin; the wing reaches those corners at its tips to
            // within the tips' few-millimetre thickness, so allow 4 px.
            if (right - left < width - 2 * margin - 4)
                throw new Exception($"The fitted wing spans {right - left:F1} px of a {width:F0} px view (margin {margin} px)");
            if (left < margin - 1e-6 || right > width - margin + 1e-6) throw new Exception($"The wing leaves the margin: {left:F1} … {right:F1}");
            fixture.Shoot();
            var foil = fixture.Brush("PlanFoilBrush");
            var starboardTip = surface.Sections[^1].Upper[0];
            if (Distance(fixture.NearestTo(starboardTip, foil, 1), foil) > 40) throw new Exception("No outline at the starboard tip's leading edge");
            if (!(fixture.ViewPoint(starboardTip).X < fixture.ViewPoint(starboardTip.Port()).X)) throw new Exception("Starboard is not on the viewer's left");
        });

        DesktopChecks.Check("View3d_DisplayWireframeShaded_RenderedPerMode", () =>
        {
            var fixture = Fixture.Shared();
            var surface = fixture.Controller.Surface!;
            var (grazing, lit) = (fixture.Brush("PlanSoftBrush"), fixture.Brush("FoilShadeLitBrush"));
            var background = fixture.Brush("ViewportBrush");
            fixture.Controller.SetDisplay(SingleView.ThreeD, DisplayMode.Wireframe);
            fixture.Shoot();
            Equal("3D · Iso · wireframe", fixture.Area.ThreeDLabel.Content as string, "wireframe title");
            // No fill: across the upper surface's interior many pixels are the viewport — the 1 px ground-grid lines and
            // the thin mesh rows cross the rest; a fill leaves none (shaded, every one of these samples is on the ramp).
            int samples = 0, empty = 0;
            for (int row = 16; row <= 28; row += 2)
                for (int sample = 30; sample <= 70; sample += 10)
                {
                    samples++;
                    if (fixture.RgbAt(UpperFace(surface, row, sample)) == background) empty++;
                }
            if (empty < 0.4 * samples) throw new Exception($"Wireframe interior: {empty} of {samples} samples are the viewport colour");
            var edge = fixture.NearestTo(surface.Sections[20].Upper[0], fixture.Brush("PlanFoilBrush"), 1);
            if (Distance(edge, fixture.Brush("PlanFoilBrush")) > 30) throw new Exception("Wireframe lost the outline: " + edge);
            fixture.Controller.SetDisplay(SingleView.ThreeD, DisplayMode.Shaded);
            fixture.Shoot();
            Equal("3D · Iso", fixture.Area.ThreeDLabel.Content as string, "shaded title");
            if (!Between(fixture.RgbAt(UpperFace(surface, 22, 50)), grazing, lit)) throw new Exception("Shaded did not return the fill");
            for (int row = 16; row <= 28; row += 2)
                for (int sample = 30; sample <= 70; sample += 10)
                    if (fixture.RgbAt(UpperFace(surface, row, sample)) == background) throw new Exception($"Shaded leaves row {row} sample {sample} unfilled");
        });

        DesktopChecks.Check("View3d_SignFixture_ExampleTipTrailingEdgeRenderedHigher", () =>
        {
            // FoilDSL §6: the Example's tip twist is −2°, nose-down negative, so its trailing edge rises. From the Side
            // preset (orthographic, from starboard, z up) the tip TE is drawn above the tip LE and the root TE.
            var fixture = Fixture.Shared();
            fixture.View.ApplyPreset(NamedCamera.Side);
            fixture.Shoot();
            var surface = fixture.Controller.Surface!;
            var tip = surface.Sections[^1];
            var foil = fixture.Brush("PlanFoilBrush");
            var trailing = fixture.ViewPoint(tip.Upper[^1]);
            var leading = fixture.ViewPoint(tip.Upper[0]);
            var rootTrailing = fixture.ViewPoint(surface.Sections[0].Upper[^1]);
            if (Distance(fixture.NearestTo(tip.Upper[^1], foil, 1), foil) > 40) throw new Exception("No tip trailing-edge pixels at " + trailing);
            if (Distance(fixture.NearestTo(tip.Upper[0], foil, 1), foil) > 40) throw new Exception("No tip leading-edge pixels at " + leading);
            if (!(trailing.Y < leading.Y - 4)) throw new Exception($"Tip TE at y {trailing.Y:F1} is not above the tip LE at {leading.Y:F1}");
            if (!(trailing.Y < rootTrailing.Y - 4)) throw new Exception($"Tip TE at y {trailing.Y:F1} is not above the root TE at {rootTrailing.Y:F1}");
            // Above the tip TE, past the stroke, the view is empty: the TE is the top of the drawing there.
            Equal(fixture.Brush("ViewportBrush"), fixture.RgbAtView(trailing + new Point(-2, -6)), "above the tip TE");
        });
    }

    private static void NavigationChecks()
    {
        DesktopChecks.Check("View3d_AltDrag_OrbitsTitleShowsAzimuthElevation", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            var centre = fixture.Centre;
            fixture.Drag(centre, new Vector(40, -20), MouseButton.Left, KeyModifiers.Alt);
            var expected = start.Orbit(40 * View3d.OrbitDegreesPerPixel, -20 * View3d.OrbitDegreesPerPixel);
            Equal(expected, fixture.Camera, "orbit by the drag");
            Equal("3D · Free · az 65° · el 20°", fixture.Area.ThreeDLabel.Content as string, "title after the orbit");
            Equal(Projection.Perspective, fixture.Camera.Projection, "orbit is perspective");
        });

        DesktopChecks.Check("View3d_PointerBindings_TrackpadPansPinchAndWheelZoomMiddleAndShiftDragPan", () =>
        {
            var fixture = Fixture.Shared();
            var size = fixture.View.Bounds.Size;
            var at = new Point(size.Width * 0.3, size.Height * 0.6);
            var start = fixture.Camera;
            fixture.Wheel(at, new Vector(0, 2));
            Close(start.ZoomAbout(at, Math.Pow(View3d.WheelZoomBase, 2), size), fixture.Camera, "wheel zooms about the pointer");
            fixture.Reset(start);
            fixture.Wheel(at, new Vector(0.5, -1.25));
            Close(start.Pan(0.5 * View3d.TrackpadPanPixels, -1.25 * View3d.TrackpadPanPixels, size), fixture.Camera, "two-finger scroll pans");
            fixture.Reset(start);
            fixture.Magnify(at, 0.2);
            Close(start.ZoomAbout(at, 1.2, size), fixture.Camera, "pinch zooms about the pointer");
            fixture.Reset(start);
            fixture.Drag(at, new Vector(30, 10), MouseButton.Middle, KeyModifiers.None);
            Equal(start.Pan(30, 10, size), fixture.Camera, "middle-drag pans");
            fixture.Reset(start);
            fixture.Drag(at, new Vector(-25, 15), MouseButton.Left, KeyModifiers.Shift);
            Equal(start.Pan(-25, 15, size), fixture.Camera, "Shift-drag on empty space pans");
            fixture.Reset(start);
            fixture.Drag(at, new Vector(-25, 15), MouseButton.Left, KeyModifiers.None);
            Equal(start, fixture.Camera, "a plain drag does not move the camera");
        });

        DesktopChecks.Check("View3d_KeyboardOrbit_Alt15ShiftAlt90Brackets5", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            foreach (var (key, modifiers, azimuth) in new (Key, KeyModifiers, double)[]
            {
                (Key.Left, KeyModifiers.Alt, -15), (Key.Right, KeyModifiers.Alt, 15),
                (Key.Left, KeyModifiers.Alt | KeyModifiers.Shift, -90), (Key.Right, KeyModifiers.Alt | KeyModifiers.Shift, 90),
                (Key.OemOpenBrackets, KeyModifiers.None, -5), (Key.OemCloseBrackets, KeyModifiers.None, 5)
            })
            {
                fixture.Reset(start);
                if (!fixture.Key(key, modifiers)) throw new Exception($"{modifiers}+{key} was not handled");
                Equal(start.Orbit(azimuth, 0), fixture.Camera, $"{modifiers}+{key}");
            }
        });

        DesktopChecks.Check("View3d_KeyboardTilt_Alt15ShiftAlt45", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            foreach (var (key, modifiers, elevation) in new (Key, KeyModifiers, double)[]
            {
                (Key.Up, KeyModifiers.Alt, 15), (Key.Down, KeyModifiers.Alt, -15),
                (Key.Up, KeyModifiers.Alt | KeyModifiers.Shift, 45), (Key.Down, KeyModifiers.Alt | KeyModifiers.Shift, -45)
            })
            {
                fixture.Reset(start);
                if (!fixture.Key(key, modifiers)) throw new Exception($"{modifiers}+{key} was not handled");
                Equal(start.Orbit(0, elevation), fixture.Camera, $"{modifiers}+{key}");
            }
        });

        DesktopChecks.Check("View3d_ShiftArrows_Pan", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            var size = fixture.View.Bounds.Size;
            double dx = View3d.PanFraction * size.Width, dy = View3d.PanFraction * size.Height;
            foreach (var (key, x, y) in new (Key, double, double)[] { (Key.Left, -dx, 0), (Key.Right, dx, 0), (Key.Up, 0, -dy), (Key.Down, 0, dy) })
            {
                fixture.Reset(start);
                if (!fixture.Key(key, KeyModifiers.Shift)) throw new Exception("Shift+" + key + " was not handled");
                Equal(start.Pan(x, y, size), fixture.Camera, "Shift+" + key);
            }
        });

        DesktopChecks.Check("View3d_PlainArrows_DoNothing", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            foreach (var key in new[] { Key.Left, Key.Right, Key.Up, Key.Down })
            {
                if (fixture.Key(key, KeyModifiers.None)) throw new Exception("Plain " + key + " was handled by the 3D view");
                Equal(start, fixture.Camera, "plain " + key);
            }
        });

        DesktopChecks.Check("View3d_ZoomKeys_CommandPlusMinusAndZ_AboutCentre", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            var size = fixture.View.Bounds.Size;
            var centre = new Point(size.Width / 2, size.Height / 2);
            foreach (var (key, modifiers, factor) in new (Key, KeyModifiers, double)[]
            {
                (Key.OemPlus, KeyModifiers.Meta, View3d.ZoomStep), (Key.OemMinus, KeyModifiers.Meta, 1 / View3d.ZoomStep),
                (Key.Z, KeyModifiers.Shift, View3d.ZoomStep), (Key.Z, KeyModifiers.None, 1 / View3d.ZoomStep)
            })
            {
                fixture.Reset(start);
                if (!fixture.Key(key, modifiers)) throw new Exception($"{modifiers}+{key} was not handled");
                Equal(start.ZoomAbout(centre, factor, size), fixture.Camera, $"{modifiers}+{key}");
            }
        });

        DesktopChecks.Check("View3d_HomeIsoCommandZeroFitFFitSelection", () =>
        {
            var fixture = Fixture.Shared();
            var size = fixture.View.Bounds.Size;
            var surface = fixture.Controller.Surface!;
            var (minimum, maximum) = View3d.FullBounds(surface);
            var moved = fixture.Camera.Orbit(-70, 20).Pan(30, -12, size).ZoomAbout(new Point(40, 50), 3, size);
            fixture.Reset(moved);
            fixture.Key(Key.Home, KeyModifiers.None);
            Equal(ViewCamera.Named(NamedCamera.Iso, minimum, maximum, size), fixture.Camera, "Home = Iso");
            fixture.Reset(moved);
            fixture.Key(Key.D0, KeyModifiers.Meta);
            Equal(moved.Fit(minimum, maximum, size), fixture.Camera, "⌘0 fits everything");
            fixture.Reset(moved);
            fixture.Key(Key.F, KeyModifiers.None);
            Equal(moved.Fit(minimum, maximum, size), fixture.Camera, "F with nothing selected fits everything");
            var tip = surface.Sections[^1];
            fixture.Controller.Select(new Selection.Station(tip.Assignment!.Value, tip.Eta));
            fixture.Settle();
            fixture.Reset(moved);
            fixture.Key(Key.F, KeyModifiers.None);
            var (low, high) = WorkbenchController.SectionBounds(tip);
            Equal(moved.Fit(low, high, size), fixture.Camera, "F fits the selected station");
            fixture.Key(Key.D0, KeyModifiers.Meta);
            Equal(moved.Fit(low, high, size).Fit(minimum, maximum, size), fixture.Camera, "⌘0 still fits everything");
        });

        DesktopChecks.Check("View3d_SingleKeys_InertInTextFieldAndBrowser", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            var keys = new (Key, KeyModifiers)[]
            {
                (Key.OemOpenBrackets, KeyModifiers.None), (Key.OemCloseBrackets, KeyModifiers.None),
                (Key.Z, KeyModifiers.None), (Key.Z, KeyModifiers.Shift), (Key.F, KeyModifiers.None)
            };
            var field = fixture.Host.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(box => box.IsEffectivelyVisible)
                ?? throw new Exception("No visible text field in the shell");
            var browser = fixture.Host.Browser;
            foreach (var target in new Control[] { field, browser })
            {
                target.Focus();
                foreach (var (key, modifiers) in keys)
                {
                    target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = key, KeyModifiers = modifiers });
                    fixture.Settle();
                    Equal(start, fixture.Camera, $"{modifiers}+{key} in {target.GetType().Name}");
                }
            }
            // The same keys act while the 3D view has focus, so the checks above are not vacuous.
            foreach (var (key, modifiers) in keys)
            {
                fixture.Reset(start);
                if (!fixture.Key(key, modifiers)) throw new Exception($"{modifiers}+{key} did nothing in the 3D view");
            }
        });

        DesktopChecks.Check("View3d_NavigateEnd_EmitsFramesAndP95", () =>
        {
            var fixture = Fixture.Shared();
            ShellEvents.Clear();
            fixture.DragWithFrames(fixture.Centre, [new Vector(10, 0), new Vector(20, 4), new Vector(30, 8)], KeyModifiers.Alt);
            var end = ShellEvents.Read().Where(item => item.Name == "view.navigate.end").ToArray();
            Equal(1, end.Length, "one view.navigate.end per gesture");
            var item = end[0];
            Equal("3d", item.Pane, "pane");
            Equal("orbit", item.Trigger, "trigger");
            Equal("ok", item.Outcome, "outcome");
            if (item.Frames is not >= 3) throw new Exception("Frames: " + item.Frames);
            if (item.RenderP95Ms is not > 0) throw new Exception("RenderP95Ms: " + item.RenderP95Ms);
            if (string.IsNullOrEmpty(item.TraceId)) throw new Exception("No trace id");
            ShellEvents.Clear();
            fixture.DragWithFrames(fixture.Centre, [new Vector(6, 6), new Vector(12, 12)], KeyModifiers.Shift);
            Equal("pan", ShellEvents.Read().Single(entry => entry.Name == "view.navigate.end").Trigger, "pan gesture trigger");
        });
    }

    private static void CubeChecks()
    {
        DesktopChecks.Check("View3d_CubeFaceClick_PresetOrthographicAndAnnounced", () =>
        {
            var fixture = Fixture.Shared();
            var size = fixture.View.Bounds.Size;
            var (minimum, maximum) = View3d.FullBounds(fixture.Controller.Surface!);
            var side = fixture.View.Faces.Single(face => face.Camera == NamedCamera.Side);
            fixture.ClickView(Lerp(side.Centre, side.Corners[2], 0.6));   // on the face, off its centre button
            Equal(ViewCamera.Named(NamedCamera.Side, minimum, maximum, size), fixture.Camera, "pointer on the S face");
            Equal(Projection.Orthographic, fixture.Camera.Projection, "axis preset is orthographic");
            Equal("3D view: Side.", fixture.Host.StatusStrip.Text, "announced in the status strip");
            Equal("3D · Side", fixture.Area.ThreeDLabel.Content as string, "title");
            fixture.Reset(ViewCamera.Named(NamedCamera.Iso, minimum, maximum, size));
            var front = fixture.View.FaceButton(NamedCamera.Front) ?? throw new Exception("The F face is not a button at Iso");
            front.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            fixture.Settle();
            Equal(ViewCamera.Named(NamedCamera.Front, minimum, maximum, size), fixture.Camera, "the F face button");
            Equal("3D view: Front.", fixture.Host.StatusStrip.Text, "announced");
        });

        DesktopChecks.Check("View3d_CubeChevron_Orbits90", () =>
        {
            var fixture = Fixture.Shared();
            var start = fixture.Camera;
            var chevrons = fixture.View.Chevrons;
            Equal(4, chevrons.Count, "four chevrons");
            foreach (var (button, name, azimuth, elevation) in chevrons.Zip(
                new (string, double, double)[] { ("Orbit left 90°", -90, 0), ("Orbit up 90°", 0, 90), ("Orbit down 90°", 0, -90), ("Orbit right 90°", 90, 0) },
                (button, row) => (button, row.Item1, row.Item2, row.Item3)))
            {
                Equal(name, AutomationProperties.GetName(button), "chevron name");
                if (button.Bounds.Width < 24 || button.Bounds.Height < 24) throw new Exception(name + " is smaller than 24 px: " + button.Bounds);
                fixture.Reset(start);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                fixture.Settle();
                Equal(start.Orbit(azimuth, elevation), fixture.Camera, name);
            }
        });

        DesktopChecks.Check("View3d_CubeFaces_TargetOnlyWhen24PxCircleFits", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            Equal("F,S,T", string.Join(",", view.Faces.Select(face => face.Letter).Order()), "Iso faces with area (front, starboard, above)");
            foreach (var face in view.Faces)
            {
                double fits = Inscribed(face);
                bool target = fits >= 12;
                Equal(target, face.IsTarget, $"{face.Letter} target with a {2 * fits:F2} px circle");
                var button = view.FaceButton(face.Camera);
                Equal(target, button is { IsEffectivelyVisible: true }, $"{face.Letter} button shown");
                if (button is not null && target)
                {
                    var box = button.Bounds;
                    if (box.Width < 24 || box.Height < 24 || Math.Abs(box.Center.X - face.Centre.X) > 0.5 || Math.Abs(box.Center.Y - face.Centre.Y) > 0.5)
                        throw new Exception($"{face.Letter} button {box} is not a 24 px target on its face");
                }
            }
            if (view.Faces.Single(face => face.Letter == "T").IsTarget) throw new Exception("The Iso top face is under 24 px but is a target");
            view.ApplyPreset(NamedCamera.Front);
            fixture.Settle();
            var front = view.Faces.Single();
            Equal("F", front.Letter, "Front shows one face");
            if (!front.IsTarget || view.FaceButton(NamedCamera.Front) is not { IsEffectivelyVisible: true }) throw new Exception("The Front face is not a target");
        });

        DesktopChecks.Check("View3d_FocusedCubeFaceLosesArea_FocusToCurrentFace", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            var side = view.FaceButton(NamedCamera.Side) ?? throw new Exception("No S button at Iso");
            side.Focus(NavigationMethod.Tab);
            view.ApplyPreset(NamedCamera.Front);   // as View ▸ Camera ▸ Front does: S loses its area
            fixture.Settle();
            var focused = fixture.Focused;
            if (!ReferenceEquals(focused, view.FaceButton(NamedCamera.Front))) throw new Exception("Focus went to " + Describe(focused));
            // From Front, ⇧⌥← orbits 90° to a Free camera with no current face: focus returns to the view.
            fixture.KeyOn((Control)focused!, Key.Left, KeyModifiers.Alt | KeyModifiers.Shift);
            if (fixture.Camera.Name is not null) throw new Exception("Expected a Free camera");
            if (!ReferenceEquals(fixture.Focused, view)) throw new Exception("Focus went to " + Describe(fixture.Focused));
        });

        DesktopChecks.Check("View3d_CubeHiddenBelow240_FocusToViewMenuReachesPresets", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            view.Width = View3d.MinimumCubeWidth;
            fixture.Settle();
            if (!view.CubeVisible || view.Chevrons.Any(button => !button.IsEffectivelyVisible)) throw new Exception("Cube hidden at 240 px");
            view.Chevrons[0].Focus(NavigationMethod.Tab);
            view.Width = View3d.MinimumCubeWidth - 1;
            fixture.Settle();
            if (view.CubeVisible || view.Faces.Count != 0) throw new Exception("Cube shown at 239 px");
            if (view.Chevrons.Any(button => button.IsEffectivelyVisible) || view.Children.OfType<Button>().Any(button => button.IsEffectivelyVisible))
                throw new Exception("A cube button stays reachable while the cube is hidden");
            if (!ReferenceEquals(fixture.Focused, view)) throw new Exception("Focus went to " + Describe(fixture.Focused));
            // Every preset stays reachable without the cube: View ▸ Camera calls ApplyPreset (its menu rows are PNL's).
            foreach (var name in Enum.GetValues<NamedCamera>())
            {
                view.ApplyPreset(name);
                fixture.Settle();
                Equal(name, fixture.Camera.Name, "preset without the cube");
            }
        });

        DesktopChecks.Check("View3d_AxisView_HomeAndArrowsVisible_HomeReturnsToIso", () =>
        {
            // DR-VIEW-9 (NS-5): on an axis view the cube shows one face, so the way back is always on screen: a Home button
            // beside the cube (a Tab stop after the faces) and the four rotate arrows drawn without hover or focus.
            var fixture = Fixture.Shared();
            var view = fixture.View;
            var size = view.Bounds.Size;
            var (minimum, maximum) = View3d.FullBounds(fixture.Controller.Surface!);
            var iso = ViewCamera.Named(NamedCamera.Iso, minimum, maximum, size);
            var home = view.Children.OfType<Button>().SingleOrDefault(button => AutomationProperties.GetName(button) == "Home (Iso)")
                ?? throw new Exception("The 3D view has no Home (Iso) button");
            Equal("Home (Iso)", ToolTip.GetTip(home) as string, "Home tooltip");
            if (!home.IsEffectivelyVisible || home.Bounds.Width < 24 || home.Bounds.Height < 24)
                throw new Exception($"Home is not a visible 24 px target at Iso: {home.Bounds}");
            double cubeLeft = view.Faces.SelectMany(face => face.Corners).Min(point => point.X);
            double cubeTop = view.Faces.SelectMany(face => face.Corners).Min(point => point.Y);
            double cubeBottom = view.Faces.SelectMany(face => face.Corners).Max(point => point.Y);
            if (home.Bounds.Right > cubeLeft || cubeLeft - home.Bounds.Right > 16 || home.Bounds.Center.Y < cubeTop || home.Bounds.Center.Y > cubeBottom)
                throw new Exception($"Home {home.Bounds} is not beside the cube (left {cubeLeft:F1}, rows {cubeTop:F1}–{cubeBottom:F1})");
            if (view.ChevronsShown) throw new Exception("The arrows show at Iso without hover or focus");

            foreach (var axis in new[] { NamedCamera.Top, NamedCamera.Bottom, NamedCamera.Front, NamedCamera.Back, NamedCamera.Side, NamedCamera.Port })
            {
                view.ApplyPreset(axis);
                view.Focus();
                fixture.Settle();
                if (!view.ChevronsShown) throw new Exception($"The arrows are hidden at {axis} without hover or focus");
                if (!home.IsEffectivelyVisible) throw new Exception($"Home is hidden at {axis}");
            }

            // Rendered (UI-RENDERED-STATE): at Bottom each arrow and Home sit on a viewport-soft plate with an ink glyph.
            view.ApplyPreset(NamedCamera.Bottom);
            view.Focus();
            fixture.Shoot();
            var soft = fixture.Brush("PlanSoftBrush");
            var ink = fixture.Brush("ViewportInkBrush");
            foreach (var button in view.Chevrons.Append(home))
            {
                var box = button.Bounds;
                var plate = fixture.RgbAtView(new Point(box.X + 3, box.Center.Y));
                if (Distance(plate, soft) > 6) throw new Exception($"{AutomationProperties.GetName(button)}: no plate at Bottom ({plate})");
                var glyph = fixture.NearestToView(box.Center, ink, 6);
                if (Distance(glyph, ink) > 40) throw new Exception($"{AutomationProperties.GetName(button)}: no ink glyph at Bottom ({glyph})");
            }

            // A click on Home goes to Iso and is announced, as the cube's faces are. The click is the Button's own (a pointer
            // release and AT Invoke both end in Button.OnClick): this harness commits no rendered frame, so the window cannot
            // hit-test a synthetic pointer onto Home; docs/proof/m12b2-cube/bottom-with-home.png and the native run cover it.
            if (ControlAutomationPeer.CreatePeerForElement(home) is not Avalonia.Automation.Provider.IInvokeProvider invoke)
                throw new Exception("Home has no Invoke pattern");
            invoke.Invoke();
            fixture.Settle();
            Equal(iso, fixture.Camera, "a click on Home");
            Equal("3D view: Iso.", fixture.Host.StatusStrip.Text, "Home announced");
            fixture.Reset(fixture.Camera);
            if (view.ChevronsShown) throw new Exception("The arrows stay shown back at Iso");
            fixture.Reset(fixture.Camera.Orbit(15, 0));
            if (view.ChevronsShown) throw new Exception("The arrows show on a Free camera without hover or focus");

            // Keyboard: Home is the Tab stop after the faces and before the arrows; Return activates it.
            view.ApplyPreset(NamedCamera.Bottom);
            fixture.Settle();
            var bottom = view.FaceButton(NamedCamera.Bottom) ?? throw new Exception("No B face button at Bottom");
            if (!ReferenceEquals(KeyboardNavigationHandler.GetNext(bottom, NavigationDirection.Next), home))
                throw new Exception("Tab from the B face does not reach Home");
            if (!ReferenceEquals(KeyboardNavigationHandler.GetNext(home, NavigationDirection.Next), view.Chevrons[0]))
                throw new Exception("Tab from Home does not reach the first arrow");
            if (!home.Focus(NavigationMethod.Tab)) throw new Exception("Home takes no keyboard focus");
            fixture.KeyOn(home, Key.Enter, KeyModifiers.None);
            Equal(iso, fixture.Camera, "Return on Home");

            // Home hides with the cube; View ▸ Camera ▸ Iso stays the path (View3d_CubeHiddenBelow240_FocusToViewMenuReachesPresets).
            view.Width = View3d.MinimumCubeWidth - 1;
            fixture.Settle();
            if (home.IsEffectivelyVisible) throw new Exception("Home stays while the cube is hidden");
        });

        DesktopChecks.Check("View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            view.ApplyPreset(NamedCamera.Front);
            fixture.Settle();
            var button = view.FaceButton(NamedCamera.Front) ?? throw new Exception("No F button");
            button.Focus(NavigationMethod.Tab);
            fixture.Shoot();
            var face = view.Faces.Single();
            var station = fixture.Brush("PlanSelectionBrush");
            var ring = fixture.Brush("PlanFocusBrush");
            var gap = fixture.Brush("ViewportBrush");
            double r = View3d.FocusRingRadius;
            var c = face.Centre;
            // The ring is 3 px on radius r with a 1 px gap each side; along the horizontal through the (pixel-centred)
            // face centre the ring covers r − 1.5 … r + 1.5 and the gaps the pixel either side.
            Equal(station, fixture.RgbAtView(c + new Point(r + 4, 0)), "current face filled station");
            var onRing = fixture.RgbAtView(c + new Point(r, 0));
            var outer = fixture.RgbAtView(c + new Point(r + 2, 0));
            var inner = fixture.RgbAtView(c + new Point(r - 2, 0));
            if (Distance(onRing, ring) > 20) throw new Exception($"Ring pixel {onRing}, not focus-ring-viewport {ring}");
            if (Distance(outer, gap) > 40 || Distance(inner, gap) > 40) throw new Exception($"Gap pixels {inner} / {outer}, not viewport {gap}");
            if (Contrast(onRing, outer) < 3 || Contrast(outer, station) < 3) throw new Exception($"Ring/gap {Contrast(onRing, outer):F2}:1, gap/face {Contrast(outer, station):F2}:1");
            // Unfocused, the ring is gone.
            view.Focus();
            fixture.Shoot();
            Equal(station, fixture.RgbAtView(c + new Point(r, 0)), "no ring without focus");
        });
    }

    private static void SelectionChecks()
    {
        DesktopChecks.Check("View3d_ClickStationSection_SelectsStationEverywhere", () =>
        {
            var fixture = Fixture.Shared();
            var surface = fixture.Controller.Surface!;
            var tip = surface.Sections[^1];
            var plan = fixture.Controller.Planform!;
            int index = Enumerable.Range(0, plan.Stations.Count).Single(i => plan.Stations[i].Eta == tip.Eta);
            fixture.ClickView(fixture.ViewPoint(tip.Upper[50]));
            Equal(new Selection.Station(index, tip.Eta), fixture.Controller.Selection, "the tip station, by its Plan index");
            if (!fixture.Host.Properties.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Tip station"))
                throw new Exception("Properties does not show the tip station");
            // Empty space picks nothing and keeps the selection.
            fixture.ClickView(new Point(fixture.View.Bounds.Width / 2, 100));
            Equal(new Selection.Station(index, tip.Eta), fixture.Controller.Selection, "a click on empty space");
        });

        DesktopChecks.Check("View3d_SelectedStation_RenderedWidthAndChip", () =>
        {
            var fixture = Fixture.Shared();
            var surface = fixture.Controller.Surface!;
            var tip = surface.Sections[^1];
            fixture.Controller.Select(new Selection.Station(tip.Assignment!.Value, tip.Eta));
            fixture.Shoot();
            var station = fixture.Brush("PlanSelectionBrush");
            int selected = Thickness(fixture, tip.Upper[50], station);
            int root = Thickness(fixture, surface.Sections[0].Upper[50], fixture.Brush("FoilEdgeBrush"));
            if (selected < 3) throw new Exception($"The selected station is {selected} px across, not 3");
            if (selected <= root) throw new Exception($"The selected station ({selected} px) is not wider than an authored section ({root} px)");
            Equal("Tip", fixture.View.ChipText, "chip names the station");
            var chip = fixture.View.ChipBounds ?? throw new Exception("No chip");
            if (chip.X < 0 || chip.Right > fixture.View.Bounds.Width || chip.Y < 0 || chip.Bottom > fixture.View.Bounds.Height)
                throw new Exception("Chip outside the view: " + chip);
            Equal(fixture.Brush("PlanSoftBrush"), fixture.RgbAtView(new Point(chip.Right - 2.5, chip.Center.Y)), "chip plate");
            if (Distance(fixture.RgbAtView(new Point(chip.Center.X, chip.Y + 0.5)), station) > 30) throw new Exception("Chip border is not station");
            if (tip.Upper.Concat(tip.Lower).Select(fixture.ViewPoint).Any(point => chip.Contains(point)))
                throw new Exception("The chip covers the station it names");
            fixture.Controller.Select(new Selection.Foil());
            fixture.Shoot();
            if (fixture.View.ChipBounds is not null) throw new Exception("Chip stays without a selected station");
        });
    }

    private static void AccessibilityChecks()
    {
        DesktopChecks.Check("View3d_AutomationName_CameraOnlyUpdatedPerStep", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            Equal("3D view, camera Iso", AutomationProperties.GetName(view), "Iso name");
            var start = fixture.Camera;
            fixture.BeginDrag(fixture.Centre, KeyModifiers.Alt);
            fixture.MoveDrag(fixture.Centre + new Vector(20, 0), KeyModifiers.Alt);
            fixture.MoveDrag(fixture.Centre + new Vector(40, 10), KeyModifiers.Alt);
            if (view.CurrentCamera == start) throw new Exception("The drag did not orbit");
            Equal("3D · Free · az 65° · el 35°", fixture.Area.ThreeDLabel.Content as string, "the title follows the drag");
            Equal(start, fixture.Camera, "the controller takes the camera at release, not per frame");
            Equal("3D view, camera Iso", AutomationProperties.GetName(view), "name during the drag");
            fixture.EndDrag(fixture.Centre + new Vector(40, 10));
            Equal("3D view, camera Free, azimuth 65°, elevation 35°", AutomationProperties.GetName(view), "name at the end of the drag");
            fixture.Key(Key.Right, KeyModifiers.Alt);
            Equal("3D view, camera Free, azimuth 80°, elevation 35°", AutomationProperties.GetName(view), "name after one key step");
            view.ApplyPreset(NamedCamera.Front);
            fixture.Settle();
            Equal("3D view, camera Front", AutomationProperties.GetName(view), "name after a preset");
        });

        DesktopChecks.Check("View3d_AutomationPeers_CubeButtonsAndHelpText", () =>
        {
            var fixture = Fixture.Shared();
            var peer = ControlAutomationPeer.CreatePeerForElement(fixture.View);
            Equal(AutomationControlType.Group, peer.GetAutomationControlType(), "view peer type");
            Equal("3D view, camera Iso", peer.GetName(), "view peer name");
            Equal(View3d.HelpText, peer.GetHelpText(), "help text");
            Equal("Option arrows orbit. Shift arrows pan here; in the elevations they move a point 1 mm. Command equals zooms. F fits the selection. Home returns to Iso.",
                View3d.HelpText, "help copy (§11.4)");
            var children = peer.GetChildren();
            var names = children.Select(child => (child.GetAutomationControlType(), child.GetName())).ToArray();
            foreach (var expected in new[] { "Front view", "Side view", "Orbit left 90°", "Orbit up 90°", "Orbit down 90°", "Orbit right 90°" })
                if (!names.Contains((AutomationControlType.Button, expected)))
                    throw new Exception($"No Button peer '{expected}': {string.Join(", ", names.Select(item => item.Item1 + " " + item.Item2))}");
            if (names.Any(item => item.Item2 == "Top view")) throw new Exception("The Iso top face is under 24 px but has a peer");
            fixture.View.ApplyPreset(NamedCamera.Front);
            fixture.Settle();
            if (!peer.GetChildren().Any(child => child.GetName() == "Front view" && child.GetAutomationControlType() == AutomationControlType.Button))
                throw new Exception("No Front view button peer at Front");
        });

        DesktopChecks.Check("View3d_PresetChange_NoAnimationFrames", () =>
        {
            var fixture = Fixture.Shared();
            var size = fixture.View.Bounds.Size;
            var (minimum, maximum) = View3d.FullBounds(fixture.Controller.Surface!);
            var cameras = new List<ViewCamera?>();
            void Record(SingleView view) => cameras.Add(fixture.Controller.Camera3d);
            fixture.Controller.CameraChanged += Record;
            var target = ViewCamera.Named(NamedCamera.Side, minimum, maximum, size);
            try
            {
                fixture.View.FaceButton(NamedCamera.Side)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Equal(target, fixture.Controller.Camera3d, "the preset camera, before any frame");
                for (int frame = 0; frame < 20; frame++) fixture.Settle();
            }
            finally { fixture.Controller.CameraChanged -= Record; }
            var distinct = cameras.Distinct().ToArray();
            if (distinct.Length != 1 || distinct[0] != target)
                throw new Exception($"The camera passed through {distinct.Length} values: {string.Join(" | ", distinct.Select(item => item?.Title))}");
        });

        DesktopChecks.Check("View3d_TabPastCube_LeavesView", () =>
        {
            var fixture = Fixture.Shared();
            var view = fixture.View;
            var label = fixture.Area.ThreeDLabel;
            var order = new List<IInputElement>();
            IInputElement? current = label;
            for (int step = 0; step < 12 && current is not null; step++)
            {
                current = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);
                if (current is null) break;
                order.Add(current);
                if (current is Visual visual && !ReferenceEquals(visual, view) && !view.IsVisualAncestorOf(visual)) break;
            }
            if (order.Count == 0 || !ReferenceEquals(order[0], view)) throw new Exception("Tab from the label does not reach the view: " + Describe(order.FirstOrDefault()));
            // DR-VIEW-9: Home is the stop after the faces.
            var home = view.Children.OfType<Button>().Single(button => AutomationProperties.GetName(button) == View3d.HomeName);
            var expected = view.Faces.Where(face => face.IsTarget).Select(face => (IInputElement)view.FaceButton(face.Camera)!)
                .Append(home).Concat(view.Chevrons).ToArray();
            var inside = order.Skip(1).Take(expected.Length).ToArray();
            if (!inside.SequenceEqual(expected))
                throw new Exception("Tab order inside the view: " + string.Join(" → ", inside.Select(Describe)));
            var after = order.Last();
            if (after is Visual leaving && (ReferenceEquals(leaving, view) || view.IsVisualAncestorOf(leaving)))
                throw new Exception("Tab past the last chevron stays in the view");
            Equal(expected.Length + 2, order.Count, "label → view → faces → Home → chevrons → out");
            var back = KeyboardNavigationHandler.GetNext(after, NavigationDirection.Previous);
            if (!ReferenceEquals(back, expected[^1])) throw new Exception("Shift+Tab does not come back to the last chevron: " + Describe(back));
        });
    }

    internal static void RunReadiness()
    {
        if (Environment.GetEnvironmentVariable("CFD_PROOF_DIR") is { Length: > 0 } proof) CaptureProof(proof);
        if (Environment.GetEnvironmentVariable("CFD_PROOF_CUBE_DIR") is { Length: > 0 } cube) CaptureCubeProof(cube);
        // Wall-clock (TEST-RING): one ⌥-drag orbit frame at 1440 × 900 in Plan + 3D — the pointer move, the view's live
        // camera, the cube and title, layout, and the 3D view's draw (mesh to screen, painter's sort, one Skia draw).
        DesktopChecks.Check("Readiness_OrbitFrameP95Under33Ms", () =>
        {
            using var fixture = new Fixture(width: 1440, height: 900);
            var renderer = fixture.Area.ThreeDRenderer;
            var size = new PixelSize((int)renderer.Bounds.Width, (int)renderer.Bounds.Height);
            var times = new List<double>();
            fixture.BeginDrag(fixture.Centre, KeyModifiers.Alt);
            for (int frame = 1; frame <= 48; frame++)
            {
                using var bitmap = new RenderTargetBitmap(size);
                Dispatcher.UIThread.RunJobs();   // the window's own pass for the previous frame, outside the timing
                long started = System.Diagnostics.Stopwatch.GetTimestamp();
                fixture.MoveDragNoSettle(fixture.Centre + new Vector(frame * 4, frame), KeyModifiers.Alt);
                fixture.Window.UpdateLayout();
                bitmap.Render(renderer);
                times.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
            fixture.EndDrag(fixture.Centre + new Vector(48 * 4, 48));
            var end = ShellEvents.Read().Last(item => item.Name == "view.navigate.end");
            Console.WriteLine(FormattableString.Invariant($"MEASURE view_navigate_end frames={end.Frames} render_p95_ms={end.RenderP95Ms:F1}"));
            var warm = times.Skip(8).Order().ToArray();
            double p95 = warm[(int)Math.Ceiling(0.95 * warm.Length) - 1];
            Console.WriteLine(FormattableString.Invariant(
                $"READINESS Readiness_OrbitFrameP95Under33Ms value_ms={p95:F3} target_ms=33 samples={warm.Length} median_ms={warm[warm.Length / 2]:F3} size={size.Width}x{size.Height}"));
            if (p95 > 33) throw new Exception(FormattableString.Invariant($"orbit frame p95 {p95:F1} ms is over 33 ms"));
        });
        // Wall-clock (TEST-RING): one wheel step and one arrow pan on the 3D view and on the Side elevation, Four views at
        // 1440 × 900. The gated step is the input event and layout: the UI thread's own work for a camera step (before the
        // split, the shell's synchronous RefreshPanes). The window's composition pass and the view's draw are measured beside
        // it, not gated (the 3D draw alone is ~17 ms). A pure camera change rebuilds no pane (ShellHost.PaneRefreshes stays put).
        DesktopChecks.Check("Readiness_CameraStep_NoPaneRefresh_Under8Ms", () =>
        {
            using var fixture = new Fixture(width: 1440, height: 900);
            fixture.Controller.Layout = ViewLayout.Four;
            fixture.Settle();
            var side = fixture.Area.FindControl<ElevationView>("SideElevation") ?? throw new Exception("No Side elevation");
            if (fixture.Controller.CameraFor(SingleView.Side) is null) throw new Exception("The Side elevation has no camera");
            var failures = new List<string>();
            Measure("3d_wheel", fixture.View, fixture.Area.ThreeDRenderer, step => Wheel(fixture, fixture.View, step));
            Measure("3d_arrow_pan", fixture.View, fixture.Area.ThreeDRenderer,
                step => PressKey(fixture.View, step % 2 == 0 ? Avalonia.Input.Key.Left : Avalonia.Input.Key.Right, KeyModifiers.Shift));
            Measure("side_wheel", side, fixture.Area.SideRenderer, step => Wheel(fixture, side, step));
            Measure("side_arrow_pan", side, fixture.Area.SideRenderer,
                step => PressKey(side, step % 2 == 0 ? Avalonia.Input.Key.Left : Avalonia.Input.Key.Right, KeyModifiers.Alt));
            if (failures.Count > 0) throw new Exception(string.Join("; ", failures));

            void Measure(string name, Control target, SurfaceRenderer renderer, Action<int> step)
            {
                target.Focus();
                fixture.Settle();
                var size = new PixelSize((int)renderer.Bounds.Width, (int)renderer.Bounds.Height);
                var steps = new List<double>();
                var draws = new List<double>();
                var raises = new List<double>();
                var jobs = new List<double>();
                var frames = new List<double>();
                long refreshesBefore = fixture.Host.PaneRefreshes;
                int changedEvents = 0;
                void OnChanged() => changedEvents++;
                fixture.Controller.Changed += OnChanged;
                try
                {
                    for (int index = 0; index < 32; index++)
                    {
                        using var bitmap = new RenderTargetBitmap(size);
                        var before = fixture.Controller.Camera3d;
                        var sideBefore = fixture.Controller.CameraFor(SingleView.Side);
                        Dispatcher.UIThread.RunJobs();   // the window's own pass for the previous step, outside the timing
                        long started = System.Diagnostics.Stopwatch.GetTimestamp();
                        step(index);
                        long raised = System.Diagnostics.Stopwatch.GetTimestamp();
                        fixture.Window.UpdateLayout();
                        steps.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                        raises.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started, raised).TotalMilliseconds);
                        long laidOut = System.Diagnostics.Stopwatch.GetTimestamp();
                        Dispatcher.UIThread.RunJobs();
                        jobs.Add(System.Diagnostics.Stopwatch.GetElapsedTime(laidOut).TotalMilliseconds);
                        frames.Add(steps[^1] + jobs[^1]);   // what the screen waits for: the step and the window pass
                        if (fixture.Controller.Camera3d == before && fixture.Controller.CameraFor(SingleView.Side) == sideBefore)
                            throw new Exception(name + " step " + index + " did not move a camera");
                        long drawn = System.Diagnostics.Stopwatch.GetTimestamp();
                        bitmap.Render(renderer);
                        draws.Add(System.Diagnostics.Stopwatch.GetElapsedTime(drawn).TotalMilliseconds);
                    }
                }
                finally { fixture.Controller.Changed -= OnChanged; }
                long refreshes = fixture.Host.PaneRefreshes - refreshesBefore;
                var warm = steps.Skip(8).Order().ToArray();
                double p95 = P95(steps);
                Console.WriteLine(FormattableString.Invariant(
                    $"READINESS Readiness_CameraStep_NoPaneRefresh_Under8Ms step={name} value_ms={p95:F3} target_ms=8 median_ms={warm[warm.Length / 2]:F3} event_p95_ms={P95(raises):F3} window_pass_p95_ms={P95(jobs):F3} frame_p95_ms={P95(frames):F3} draw_p95_ms={P95(draws):F3} samples={warm.Length} pane_refreshes={refreshes} changed_events={changedEvents} size={size.Width}x{size.Height}"));
                if (refreshes != 0) failures.Add(FormattableString.Invariant($"{name}: {refreshes} pane refreshes for {steps.Count} camera steps"));
                if (p95 > 8) failures.Add(FormattableString.Invariant($"{name}: step p95 {p95:F1} ms is over 8 ms"));
            }
        });
    }

    // The p95 of the warm samples (the first 8 steps warm the JIT and caches).
    private static double P95(IEnumerable<double> samples)
    {
        var warm = samples.Skip(8).Order().ToArray();
        return warm[(int)Math.Ceiling(0.95 * warm.Length) - 1];
    }

    // A wheel notch in, then out, at the target's centre (no settle: the readiness step times the dispatcher itself).
    private static void Wheel(Fixture fixture, Control target, int step)
    {
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        var at = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), fixture.Window) ?? throw new Exception("No window point");
        target.RaiseEvent(new PointerWheelEventArgs(target, pointer, fixture.Window, at, 1,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, new Vector(0, step % 2 == 0 ? 1 : -1)));
    }

    private static void PressKey(Control target, Key key, KeyModifiers modifiers) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = key, KeyModifiers = modifiers });

    /// <summary>
    /// The operator's captures at the mockup's 1280 × 800 (docs/mockups/m12b2-views.html screens 1 and 3a), at the
    /// window's render scaling: Plan + 3D with the tip station selected, Four views (screen 2), and One view 3D in Wireframe.
    /// </summary>
    private static void CaptureProof(string directory)
    {
        Directory.CreateDirectory(directory);
        using var fixture = new Fixture(width: 1280, height: 800);
        var surface = fixture.Controller.Surface!;
        var plan = fixture.Controller.Planform!;
        int tip = Enumerable.Range(0, plan.Stations.Count).Single(i => plan.Stations[i].Eta == 1);
        fixture.Controller.Select(new Selection.Station(tip, 1));
        fixture.Settle();
        Save(fixture, Path.Combine(directory, "plan-3d.png"));
        fixture.Controller.Layout = ViewLayout.Four;
        fixture.Settle();
        Pump(() => fixture.Controller.CameraFor(SingleView.Side) is not null && fixture.Controller.CameraFor(SingleView.Front) is not null,
            "the elevations' first fit");
        foreach (var elevation in fixture.Area.GetVisualDescendants().OfType<ElevationView>()) elevation.InvalidateVisual();
        Save(fixture, Path.Combine(directory, "four-views.png"));
        fixture.Controller.Layout = ViewLayout.One(SingleView.ThreeD);
        fixture.Controller.SetDisplay(SingleView.ThreeD, DisplayMode.Wireframe);
        fixture.Settle();
        var (minimum, maximum) = View3d.FullBounds(surface);
        fixture.Reset(ViewCamera.Named(NamedCamera.Iso, minimum, maximum, fixture.View.Bounds.Size));
        Save(fixture, Path.Combine(directory, "one-view-wireframe.png"));

        static void Save(Fixture fixture, string file)
        {
            fixture.Settle();
            double scale = fixture.Window.RenderScaling;
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Round(fixture.Window.Bounds.Width * scale),
                (int)Math.Round(fixture.Window.Bounds.Height * scale)), new Vector(96 * scale, 96 * scale));
            bitmap.Render(fixture.Window);
            bitmap.Save(file);
            Console.WriteLine($"PROOF {file} {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}");
        }
    }

    /// <summary>
    /// DR-VIEW-9 and DR-VIEW-10 for the operator, at the mockup's 1280 × 800: Plan + 3D on the Bottom camera (Home beside
    /// the cube, the arrows shown), and One view 3D with the plate's picker open. The picker is a popup window, so it is
    /// rendered on its own and drawn at its anchor, the plate's bottom-left (BottomEdgeAlignedLeft).
    /// </summary>
    private static void CaptureCubeProof(string directory)
    {
        Directory.CreateDirectory(directory);
        using var fixture = new Fixture(width: 1280, height: 800);
        double scale = fixture.Window.RenderScaling;
        var view = fixture.View;
        view.ApplyPreset(NamedCamera.Bottom);
        view.Focus();
        fixture.Settle();
        Save(Path.Combine(directory, "bottom-with-home.png"), null, default);

        fixture.Controller.Layout = ViewLayout.One(SingleView.ThreeD);
        fixture.Settle();
        var (minimum, maximum) = View3d.FullBounds(fixture.Controller.Surface!);
        fixture.Reset(ViewCamera.Named(NamedCamera.Iso, minimum, maximum, view.Bounds.Size));
        var label = fixture.Area.FindControl<Button>("ThreeDLabel") ?? throw new Exception("No 3D plate");
        if (ControlAutomationPeer.CreatePeerForElement(label) is not Avalonia.Automation.Provider.IInvokeProvider invoke)
            throw new Exception("The plate has no Invoke pattern");
        invoke.Invoke();
        fixture.Settle();
        var item = (label.Flyout as MenuFlyout)?.Items.OfType<MenuItem>().FirstOrDefault() ?? throw new Exception("No picker");
        var popup = TopLevel.GetTopLevel(item) ?? throw new Exception("The picker is not open");
        popup.UpdateLayout();
        Save(Path.Combine(directory, "one-view-picker-open.png"), ReferenceEquals(popup, fixture.Window) ? null : popup,
            label.TranslatePoint(new Point(0, label.Bounds.Height), fixture.Window) ?? throw new Exception("No anchor"));
        label.Flyout!.Hide();

        void Save(string file, TopLevel? overlay, Point anchor)
        {
            fixture.Settle();
            var pixels = new PixelSize((int)Math.Round(fixture.Window.Bounds.Width * scale), (int)Math.Round(fixture.Window.Bounds.Height * scale));
            var dpi = new Vector(96 * scale, 96 * scale);
            using var window = new RenderTargetBitmap(pixels, dpi);
            window.Render(fixture.Window);
            using var bitmap = new RenderTargetBitmap(pixels, dpi);
            using (var context = bitmap.CreateDrawingContext())
            {
                context.DrawImage(window, new Rect(0, 0, pixels.Width, pixels.Height), new Rect(fixture.Window.Bounds.Size));
                if (overlay is not null)
                {
                    using var menu = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(overlay.Bounds.Width * scale),
                        (int)Math.Ceiling(overlay.Bounds.Height * scale)), dpi);
                    menu.Render(overlay);
                    context.DrawImage(menu, new Rect(0, 0, menu.PixelSize.Width, menu.PixelSize.Height), new Rect(anchor, overlay.Bounds.Size));
                }
            }
            bitmap.Save(file);
            Console.WriteLine($"PROOF {file} {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}" + (overlay is null ? "" : $" picker {overlay.Bounds.Size}"));
        }
    }

    // ---- fixture --------------------------------------------------------------------------------------------------

    /// <summary>The shell in a realized window with the Example open, Plan + 3D, the 3D view focused and its first mesh drawn.</summary>
    private sealed class Fixture : IDisposable
    {
        private Shot? shot;
        public WorkbenchController Controller { get; }
        public ShellHost Host { get; }
        public ModelArea Area { get; }
        public Window Window { get; }
        private static Fixture? shared;

        /// <summary>
        /// The suite's one window (a window costs about half a second to realize and mesh), returned to the state every
        /// check starts from: Iso fitted to the view, Shaded, no station selected, the view at its slot width and focused.
        /// </summary>
        public static Fixture Shared()
        {
            shared ??= new Fixture();
            shared.Restore();
            return shared;
        }

        public static void DisposeShared()
        {
            shared?.Dispose();
            shared = null;
        }

        private void Restore()
        {
            View.Width = double.NaN;
            Controller.SetDisplay(SingleView.ThreeD, DisplayMode.Shaded);
            Controller.Select(new Selection.Foil());
            Settle();
            var (minimum, maximum) = View3d.FullBounds(Controller.Surface!);
            Reset(ViewCamera.Named(NamedCamera.Iso, minimum, maximum, View.Bounds.Size));
        }

        public View3d View => Area.FindControl<View3d>("ThreeDView") ?? throw new InvalidOperationException("ModelArea has no ThreeDView");

        public Fixture(int width = 1200, int height = 700)
        {
            Controller = new WorkbenchController();
            Await(Controller.OpenExampleAsync());
            Host = new ShellHost(Controller);
            Area = Host.ModelView;
            Window = new Window { Content = Host, Width = width, Height = height };
            Window.Show();
            Host.RefreshPanes();
            Settle();
            Pump(() => Controller.Surface is not null && !Controller.SurfaceUpdating && Controller.Camera3d is not null, "first mesh and camera");
            Settle();
            View.Focus();
            Settle();
        }

        public ViewCamera Camera => Controller.Camera3d ?? throw new Exception("No 3D camera");
        public Point Centre => new(View.Bounds.Width / 2, View.Bounds.Height / 2);
        public IInputElement? Focused => Window.FocusManager?.GetFocusedElement();

        public void Reset(ViewCamera camera)
        {
            Controller.Camera3d = camera;
            View.Focus();
            Settle();
        }

        public void Settle()
        {
            for (int i = 0; i < 10; i++)
            {
                Dispatcher.UIThread.RunJobs();
                Window.UpdateLayout();
            }
            shot?.Dispose();
            shot = null;
        }

        public void Shoot()
        {
            Settle();
            shot = new Shot(Window);
        }

        public bool Key(Key key, KeyModifiers modifiers) => KeyOn(View, key, modifiers);

        public bool KeyOn(Control target, Key key, KeyModifiers modifiers)
        {
            var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = key, KeyModifiers = modifiers };
            target.RaiseEvent(args);
            Settle();
            return args.Handled;
        }

        public void MoveDragNoSettle(Point local, KeyModifiers modifiers) =>
            View.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, View, dragPointer!, Window, ToWindow(local), 2,
                new PointerPointProperties(Buttons(lastButton).Raw, PointerUpdateKind.Other), modifiers));

        private Pointer? dragPointer;

        public void BeginDrag(Point local, KeyModifiers modifiers, MouseButton button = MouseButton.Left)
        {
            dragPointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var (raw, kind, _) = Buttons(button);
            View.RaiseEvent(new PointerPressedEventArgs(View, dragPointer, Window, ToWindow(local), 1, new PointerPointProperties(raw, kind), modifiers));
            Settle();
            lastButton = button;
        }

        private MouseButton lastButton;

        public void MoveDrag(Point local, KeyModifiers modifiers)
        {
            var (raw, _, _) = Buttons(lastButton);
            View.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, View, dragPointer!, Window, ToWindow(local), 2,
                new PointerPointProperties(raw, PointerUpdateKind.Other), modifiers));
            Settle();
        }

        public void EndDrag(Point local, KeyModifiers modifiers = KeyModifiers.None)
        {
            var (_, _, released) = Buttons(lastButton);
            View.RaiseEvent(new PointerReleasedEventArgs(View, dragPointer!, Window, ToWindow(local), 3,
                new PointerPointProperties(RawInputModifiers.None, released), modifiers, lastButton));
            dragPointer!.Dispose();
            dragPointer = null;
            Settle();
        }

        public void Drag(Point local, Vector by, MouseButton button, KeyModifiers modifiers)
        {
            BeginDrag(local, modifiers, button);
            MoveDrag(local + by, modifiers);
            EndDrag(local + by, modifiers);
        }

        /// <summary>A drag with one rendered 3D frame after each move, as the screen would draw them.</summary>
        public void DragWithFrames(Point local, IReadOnlyList<Vector> steps, KeyModifiers modifiers)
        {
            BeginDrag(local, modifiers);
            var renderer = Area.ThreeDRenderer;
            foreach (var step in steps)
            {
                MoveDrag(local + step, modifiers);
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)renderer.Bounds.Width, (int)renderer.Bounds.Height));
                bitmap.Render(renderer);
            }
            EndDrag(local + steps[^1], modifiers);
        }

        public void ClickView(Point local)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var at = ToWindow(local);
            // Hit-test as the window would, so a press on a cube button reaches the button, not the view.
            var target = (Window.InputHitTest(at) as Interactive) ?? View;
            target.RaiseEvent(new PointerPressedEventArgs(target, pointer, Window, at, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, Window, at, 2,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            Settle();
        }

        public void Wheel(Point local, Vector delta)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            View.RaiseEvent(new PointerWheelEventArgs(View, pointer, Window, ToWindow(local), 1,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, delta));
            Settle();
        }

        public void Magnify(Point local, double delta)
        {
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
            View.RaiseEvent(new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureMagnifyEvent, View, pointer, Window, ToWindow(local), 1,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, new Vector(delta, 0)));
            Settle();
        }

        private Point ToWindow(Point local) => View.TranslatePoint(local, Window) ?? throw new Exception("No window point");

        private static (RawInputModifiers Raw, PointerUpdateKind Pressed, PointerUpdateKind Released) Buttons(MouseButton button) => button switch
        {
            MouseButton.Middle => (RawInputModifiers.MiddleMouseButton, PointerUpdateKind.MiddleButtonPressed, PointerUpdateKind.MiddleButtonReleased),
            _ => (RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased)
        };

        public Point ViewPoint(Point3 point) => Camera.Project(point, View.Bounds.Size);
        private Shot Current => shot ?? throw new InvalidOperationException("Shoot() first");
        public (byte R, byte G, byte B) RgbAtView(Point local)
        {
            var at = ToWindow(local);
            return Current.Rgb((int)Math.Floor(at.X), (int)Math.Floor(at.Y));
        }
        public (byte R, byte G, byte B) RgbAt(Point3 point) => RgbAtView(ViewPoint(point));
        public (byte R, byte G, byte B) NearestToView(Point local, (byte R, byte G, byte B) target, int radius)
        {
            var best = RgbAtView(local);
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var candidate = RgbAtView(local + new Point(dx, dy));
                    if (Distance(candidate, target) < Distance(best, target)) best = candidate;
                }
            return best;
        }
        public (byte R, byte G, byte B) NearestTo(Point3 point, (byte R, byte G, byte B) target, int radius) => NearestToView(ViewPoint(point), target, radius);

        public (byte R, byte G, byte B) Brush(string key) =>
            Window.TryFindResource(key, Window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
                ? (brush.Color.R, brush.Color.G, brush.Color.B) : throw new Exception("Theme resource " + key + " is missing");

        public void Dispose()
        {
            shot?.Dispose();
            Window.Close();
            Controller.Dispose();
        }
    }

    /// <summary>One RenderTargetBitmap of the whole realized window, read as unpremultiplied BGRA.</summary>
    private sealed class Shot : IDisposable
    {
        private readonly WriteableBitmap pixels;
        private readonly ILockedFramebuffer frame;

        public Shot(Window window)
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Round(window.Bounds.Width), (int)Math.Round(window.Bounds.Height)));
            bitmap.Render(window);
            pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
            frame = pixels.Lock();
            bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
        }

        public (byte R, byte G, byte B) Rgb(int x, int y)
        {
            if (x < 0 || y < 0 || x >= frame.Size.Width || y >= frame.Size.Height) throw new Exception($"Pixel {x},{y} is outside the window");
            int offset = y * frame.RowBytes + x * 4;
            return (Marshal.ReadByte(frame.Address, offset + 2), Marshal.ReadByte(frame.Address, offset + 1), Marshal.ReadByte(frame.Address, offset));
        }

        public void Dispose()
        {
            frame.Dispose();
            pixels.Dispose();
        }
    }

    // ---- helpers --------------------------------------------------------------------------------------------------

    /// <summary>The run of pixels near <paramref name="colour"/> crossing the stroke through a point, scanned vertically.</summary>
    private static int Thickness(Fixture fixture, Point3 point, (byte R, byte G, byte B) colour)
    {
        var at = fixture.ViewPoint(point);
        int best = 0, run = 0;
        for (int dy = -8; dy <= 8; dy++)
        {
            bool on = Distance(fixture.RgbAtView(new Point(at.X, at.Y + dy)), colour) < 25;
            run = on ? run + 1 : 0;
            best = Math.Max(best, run);
        }
        return best;
    }

    /// <summary>The largest circle about the face centre that stays inside the face: its distance to the nearest edge line.</summary>
    private static double Inscribed(CubeFace face)
    {
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < face.Corners.Count; i++)
        {
            var a = face.Corners[i];
            var b = face.Corners[(i + 1) % face.Corners.Count];
            double cross = (b.X - a.X) * (face.Centre.Y - a.Y) - (b.Y - a.Y) * (face.Centre.X - a.X);
            nearest = Math.Min(nearest, Math.Abs(cross) / Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)));
        }
        return nearest;
    }

    private static Point3 UpperFace(SurfaceView surface, int row, int sample)
    {
        var a = surface.Sections[row].Upper[sample];
        var b = surface.Sections[row + 1].Upper[sample + 1];
        return new Point3((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    private static string Describe(object? element) => element switch
    {
        null => "nothing",
        Control control when AutomationProperties.GetName(control) is { Length: > 0 } name => control.GetType().Name + " '" + name + "'",
        _ => element.GetType().Name
    };

    private static void Pump(Func<bool> condition, string what, double seconds = 30)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (deadline.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Timed out waiting for " + what);
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
    }

    private static void Await(Task task)
    {
        Pump(() => task.IsCompleted, "a task");
        task.GetAwaiter().GetResult();
    }

    private static bool Between((byte R, byte G, byte B) pixel, (byte R, byte G, byte B) low, (byte R, byte G, byte B) high)
    {
        static bool Within(byte value, byte a, byte b) => value >= Math.Min(a, b) - 1 && value <= Math.Max(a, b) + 1;
        return Within(pixel.R, low.R, high.R) && Within(pixel.G, low.G, high.G) && Within(pixel.B, low.B, high.B);
    }

    private static int Distance((byte R, byte G, byte B) a, (byte R, byte G, byte B) b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static double Contrast((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
    {
        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        static double Luminance((byte R, byte G, byte B) c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    /// <summary>Cameras from pointer input: the window ↔ view coordinate round trip may move the last bits.</summary>
    private static void Close(ViewCamera expected, ViewCamera actual, string label)
    {
        static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(1, Math.Abs(a));
        if (!(Near(expected.Target.X, actual.Target.X) && Near(expected.Target.Y, actual.Target.Y) && Near(expected.Target.Z, actual.Target.Z) &&
              Near(expected.AzimuthDegrees, actual.AzimuthDegrees) && Near(expected.ElevationDegrees, actual.ElevationDegrees) &&
              Near(expected.Distance, actual.Distance) && expected.Projection == actual.Projection && expected.Name == actual.Name))
            throw new Exception($"{label}: expected {expected}, actual {actual}");
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{label}: expected {expected}, actual {actual}");
    }
}
