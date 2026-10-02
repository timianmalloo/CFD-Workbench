using Avalonia;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>The pure camera functions of M1.2b2 §6.2 (one test per verb), without a window.</summary>
public static class ViewCameraTests
{
    private static readonly Size Viewport = new(800, 600);
    private static readonly Point3 Minimum = new(0, -0.45, -0.01);
    private static readonly Point3 Maximum = new(0.12, 0.45, 0.01);

    public static void Run()
    {
        DesktopChecks.Check("ViewCamera_Presets_TopFrontSideIsoBottomBackPort", () =>
        {
            // The view direction (eye → target) per preset, read back through Depth.
            foreach (var (name, x, y, z) in new (NamedCamera, double, double, double)[]
            {
                (NamedCamera.Top, 0, 0, -1), (NamedCamera.Front, 1, 0, 0), (NamedCamera.Side, 0, -1, 0),
                (NamedCamera.Bottom, 0, 0, 1), (NamedCamera.Back, -1, 0, 0), (NamedCamera.Port, 0, 1, 0)
            })
            {
                var forward = Forward(Camera(name));
                Near(x, forward.X, 1e-12, name + " x");
                Near(y, forward.Y, 1e-12, name + " y");
                Near(z, forward.Z, 1e-12, name + " z");
            }
            // Iso looks from aft, starboard and above (az 135°, el 30°; the M1.2a isometric quadrant) toward the target.
            var iso = Forward(Camera(NamedCamera.Iso));
            if (!(iso.X < 0 && iso.Y < 0 && iso.Z < 0)) throw new Exception($"Iso looks along {iso}");
            foreach (var name in Enum.GetValues<NamedCamera>())
                if (Camera(name).Name != name || Camera(name).Title != name.ToString())
                    throw new Exception(name + " lost its name");
        });

        DesktopChecks.Check("ViewCamera_Front_StarboardOnViewerLeft", () =>
        {
            var front = Camera(NamedCamera.Front);
            var starboard = front.Project(new Point3(0.06, 0.4, 0), Viewport);
            var port = front.Project(new Point3(0.06, -0.4, 0), Viewport);
            var high = front.Project(new Point3(0.06, 0, 0.005), Viewport);
            var low = front.Project(new Point3(0.06, 0, -0.005), Viewport);
            if (!(starboard.X < port.X)) throw new Exception($"Front: starboard {starboard.X} is not left of port {port.X}");
            if (!(high.Y < low.Y)) throw new Exception("Front: +z is not up");
        });

        DesktopChecks.Check("ViewCamera_Side_NoseRightTrailingEdgeLeft", () =>
        {
            var side = Camera(NamedCamera.Side);
            var nose = side.Project(new Point3(0, 0.2, 0), Viewport);
            var trailingEdge = side.Project(new Point3(0.12, 0.2, 0), Viewport);
            var high = side.Project(new Point3(0.06, 0.2, 0.005), Viewport);
            var low = side.Project(new Point3(0.06, 0.2, -0.005), Viewport);
            if (!(nose.X > trailingEdge.X)) throw new Exception($"Side: nose {nose.X} is not right of the trailing edge {trailingEdge.X}");
            if (!(high.Y < low.Y)) throw new Exception("Side: +z is not up");
        });

        DesktopChecks.Check("ViewCamera_Top_MatchesPlanOrientation", () =>
        {
            // PlanCanvas maps span to +screen x (starboard right) and aft to +screen y (aft down).
            var top = Camera(NamedCamera.Top);
            var root = top.Project(new Point3(0.06, 0, 0), Viewport);
            var tip = top.Project(new Point3(0.06, 0.4, 0), Viewport);
            var leading = top.Project(new Point3(0, 0.2, 0), Viewport);
            var trailing = top.Project(new Point3(0.12, 0.2, 0), Viewport);
            Near(root.Y, tip.Y, 1e-9, "span is horizontal");
            if (!(tip.X > root.X)) throw new Exception("Top: starboard is not to the right");
            Near(leading.X, trailing.X, 1e-9, "chord is vertical");
            if (!(trailing.Y > leading.Y)) throw new Exception("Top: aft is not down");
            // The pole keeps the azimuth's up vector: one degree off the pole the layout is the same.
            var nearPole = top with { ElevationDegrees = 89 };
            if (!(nearPole.Project(new Point3(0.12, 0.2, 0), Viewport).Y > nearPole.Project(new Point3(0, 0.2, 0), Viewport).Y))
                throw new Exception("Top flips one degree off the pole");
        });

        DesktopChecks.Check("ViewCamera_AxisPresetsOrthographic_OrbitPerspective", () =>
        {
            foreach (var name in Enum.GetValues<NamedCamera>())
            {
                var expected = name == NamedCamera.Iso ? Projection.Perspective : Projection.Orthographic;
                if (Camera(name).Projection != expected) throw new Exception(name + " is " + Camera(name).Projection);
            }
            var orbited = Camera(NamedCamera.Front).Orbit(15, 0);
            if (orbited.Projection != Projection.Perspective || orbited.Name is not null)
                throw new Exception("An orbited axis camera stays orthographic or named");
            // Pan, zoom and fit keep the preset (its direction is unchanged).
            var front = Camera(NamedCamera.Front).Pan(10, 5, Viewport).ZoomAbout(new Point(100, 100), 2, Viewport)
                .Fit(Minimum, Maximum, Viewport);
            if (front.Projection != Projection.Orthographic || front.Name != NamedCamera.Front)
                throw new Exception("Pan, zoom or fit changed the preset");
        });

        DesktopChecks.Check("ViewCamera_OrbitElevation_ClampedAtPolesNoFlip", () =>
        {
            var start = Camera(NamedCamera.Front);
            Near(90, start.Orbit(0, 91).ElevationDegrees, 0, "upper clamp");
            Near(-90, start.Orbit(0, -91).ElevationDegrees, 0, "lower clamp");
            Near(89.5, (start with { ElevationDegrees = 89 }).Orbit(0, 0.5).ElevationDegrees, 1e-12, "just inside the upper pole");
            Near(-89.5, (start with { ElevationDegrees = -89 }).Orbit(0, -0.5).ElevationDegrees, 1e-12, "just inside the lower pole");
            // Through the pole and on: the screen up vector comes from the azimuth, so the projection never mirrors.
            var aft = new Point3(0.12, 0.2, 0);
            var fore = new Point3(0, 0.2, 0);
            var atPole = start.Orbit(180, 120);
            var pastPole = atPole.Orbit(0, 30);
            if (atPole.ElevationDegrees != 90 || pastPole.ElevationDegrees != 90)
                throw new Exception("Orbit passed the pole");
            if (Math.Sign(atPole.Project(aft, Viewport).Y - atPole.Project(fore, Viewport).Y) !=
                Math.Sign((atPole with { ElevationDegrees = 80 }).Project(aft, Viewport).Y - (atPole with { ElevationDegrees = 80 }).Project(fore, Viewport).Y))
                throw new Exception("The view flips at the pole");
            Near(0, Length(Forward(atPole) with { Z = 0 }), 1e-12, "at the pole the view looks straight down");
        });

        DesktopChecks.Check("ViewCamera_OrbitPivot_IsTarget", () =>
        {
            var start = Camera(NamedCamera.Iso).Pan(40, -25, Viewport);
            var centre = new Point(Viewport.Width / 2, Viewport.Height / 2);
            foreach (var (azimuth, elevation) in new[] { (15d, 0d), (90d, 0d), (0d, 45d), (-212d, -24d), (5d, 5d) })
            {
                var orbited = start.Orbit(azimuth, elevation);
                if (orbited.Target != start.Target || orbited.Distance != start.Distance)
                    throw new Exception("Orbit moved the pivot or the distance");
                var projected = orbited.Project(start.Target, Viewport);
                Near(centre.X, projected.X, 1e-9, "pivot x");
                Near(centre.Y, projected.Y, 1e-9, "pivot y");
            }
        });

        DesktopChecks.Check("ViewCamera_ZoomAboutPointer_PointUnderCursorFixed", () =>
        {
            var pointer = new Point(610, 140);
            foreach (var camera in new[] { Camera(NamedCamera.Front), Camera(NamedCamera.Iso), Camera(NamedCamera.Iso).Orbit(-40, 12) })
            {
                // The model point under the pointer at the target's depth: panning by the pointer's offset from the
                // centre brings it to the centre, so it sits as far before the target as the pan moved it after.
                var panned = camera.Pan(pointer.X - Viewport.Width / 2, pointer.Y - Viewport.Height / 2, Viewport);
                var underPointer = Subtract(Scale(camera.Target, 2), panned.Target);
                Near(camera.Distance, camera.Depth(underPointer), 1e-12, "setup depth");
                Near(pointer.X, camera.Project(underPointer, Viewport).X, 1e-6, "setup x");
                Near(pointer.Y, camera.Project(underPointer, Viewport).Y, 1e-6, "setup y");
                foreach (double factor in new[] { 1.25, 0.8, 3, 0.1 })
                {
                    var zoomed = camera.ZoomAbout(pointer, factor, Viewport);
                    var after = zoomed.Project(underPointer, Viewport);
                    Near(pointer.X, after.X, 1e-6, camera.Projection + " x after ×" + factor);
                    Near(pointer.Y, after.Y, 1e-6, camera.Projection + " y after ×" + factor);
                    Near(camera.Distance / factor, zoomed.Distance, camera.Distance * 1e-12, "distance");
                }
            }
        });

        DesktopChecks.Check("ViewCamera_Zoom_ClampedAtLimits", () =>
        {
            var fitted = Camera(NamedCamera.Iso);
            double fit = fitted.FitDistance;
            Near(fitted.Distance, fit, 0, "Fit records its distance");
            var centre = new Point(Viewport.Width / 2, Viewport.Height / 2);
            Near(0.01 * fit, fitted.ZoomAbout(centre, 1e9, Viewport).Distance, 0, "inner limit");
            Near(100 * fit, fitted.ZoomAbout(centre, 1e-9, Viewport).Distance, 0, "outer limit");
            Near(0.01 * fit, fitted.ZoomAbout(centre, 100, Viewport).Distance, fit * 1e-15, "exactly the inner limit");
            Near(fit / 99, fitted.ZoomAbout(centre, 99, Viewport).Distance, fit * 1e-15, "just inside the inner limit");
            Near(fit * 99, fitted.ZoomAbout(centre, 1.0 / 99, Viewport).Distance, fit * 1e-12, "just inside the outer limit");
            var stepped = fitted;
            for (int step = 0; step < 200; step++) stepped = stepped.ZoomAbout(new Point(700, 50), 1.5, Viewport);
            Near(0.01 * fit, stepped.Distance, 0, "repeated zoom stops at the limit");
            if (!double.IsFinite(stepped.Target.X)) throw new Exception("Zoom at the limit moved the target to infinity");
            if (fitted.ZoomAbout(centre, 0, Viewport) != fitted || fitted.ZoomAbout(centre, double.NaN, Viewport) != fitted)
                throw new Exception("A zero or NaN factor changed the camera");
        });

        DesktopChecks.Check("ViewCamera_Fit_BoundsInsideViewportMargin", () =>
        {
            foreach (var viewport in new[] { Viewport, new Size(320, 240), new Size(1200, 300) })
                foreach (var name in Enum.GetValues<NamedCamera>())
                {
                    var camera = ViewCamera.Named(name, Minimum, Maximum, viewport);
                    AssertInside(camera, Minimum, Maximum, viewport, name.ToString());
                    // Tight on at least one axis for the orthographic presets.
                    if (camera.Projection == Projection.Orthographic)
                    {
                        var (width, height) = Extent(camera, Minimum, Maximum, viewport);
                        if (Math.Max(width / (viewport.Width - 2 * ViewCamera.FitMarginPixels),
                                height / (viewport.Height - 2 * ViewCamera.FitMarginPixels)) < 0.999)
                            throw new Exception($"{name} at {viewport} does not fill the viewport");
                    }
                }
        });

        DesktopChecks.Check("ViewCamera_FitSelection_StationBoundsFill", () =>
        {
            var surface = Placement.Surface(CfdWorkbench.Cli.Cli.ExampleBytes(), "accepted", 0, CancellationToken.None);
            var tip = surface.Sections[^1];
            var (low, high) = WorkbenchController.SectionBounds(tip);
            if (!(high.Y == low.Y && low.Y > 0.44)) throw new Exception("Tip section bounds are not the tip");
            foreach (var name in new[] { NamedCamera.Side, NamedCamera.Iso, NamedCamera.Top })
            {
                var camera = Camera(name).Fit(low, high, Viewport);
                AssertInside(camera, low, high, Viewport, name + " station");
                var (width, height) = Extent(camera, low, high, Viewport);
                double fill = Math.Max(width / (Viewport.Width - 2 * ViewCamera.FitMarginPixels),
                    height / (Viewport.Height - 2 * ViewCamera.FitMarginPixels));
                if (fill < (camera.Projection == Projection.Orthographic ? 0.999 : 0.5))
                    throw new Exception($"{name}: the station fills only {fill:P0} of the view");
            }
        });

        DesktopChecks.Check("ViewCamera_RandomVerbs_FitRecentres", () =>
        {
            var random = new Random(20261002);
            var centre = new Point(Viewport.Width / 2, Viewport.Height / 2);
            var middle = new Point3((Minimum.X + Maximum.X) / 2, (Minimum.Y + Maximum.Y) / 2, (Minimum.Z + Maximum.Z) / 2);
            for (int run = 0; run < 200; run++)
            {
                var camera = Camera(Enum.GetValues<NamedCamera>()[random.Next(7)]);
                for (int verb = 0; verb < 12; verb++)
                    camera = random.Next(3) switch
                    {
                        0 => camera.Orbit(random.NextDouble() * 720 - 360, random.NextDouble() * 400 - 200),
                        1 => camera.Pan(random.NextDouble() * 600 - 300, random.NextDouble() * 600 - 300, Viewport),
                        _ => camera.ZoomAbout(new Point(random.NextDouble() * 800, random.NextDouble() * 600),
                            Math.Exp(random.NextDouble() * 6 - 3), Viewport)
                    };
                var refit = camera.Fit(Minimum, Maximum, Viewport);
                var projected = refit.Project(middle, Viewport);
                Near(centre.X, projected.X, 1e-9, "run " + run + " x");
                Near(centre.Y, projected.Y, 1e-9, "run " + run + " y");
                AssertInside(refit, Minimum, Maximum, Viewport, "run " + run);
            }
        });

        DesktopChecks.Check("ViewCamera_Title_NamedOrAzimuthElevation", () =>
        {
            if (Camera(NamedCamera.Iso).Title != "Iso" || Camera(NamedCamera.Front).Title != "Front")
                throw new Exception("Named titles: " + Camera(NamedCamera.Iso).Title + ", " + Camera(NamedCamera.Front).Title);
            var free = Camera(NamedCamera.Front).Orbit(212, 24);
            if (free.Title != "Free · az 212° · el 24°") throw new Exception("Free title: " + free.Title);
            var below = Camera(NamedCamera.Front).Orbit(-30, -24.4);
            if (below.Title != "Free · az 330° · el −24°") throw new Exception("Negative elevation title: " + below.Title);
        });
    }

    private static ViewCamera Camera(NamedCamera name) => ViewCamera.Named(name, Minimum, Maximum, Viewport);

    /// <summary>The view direction, read back from Depth (eye → target).</summary>
    private static Point3 Forward(ViewCamera camera)
    {
        double origin = camera.Depth(camera.Target);
        return new Point3(camera.Depth(Add(camera.Target, new Point3(1, 0, 0))) - origin,
            camera.Depth(Add(camera.Target, new Point3(0, 1, 0))) - origin,
            camera.Depth(Add(camera.Target, new Point3(0, 0, 1))) - origin);
    }

    private static void AssertInside(ViewCamera camera, Point3 low, Point3 high, Size viewport, string label)
    {
        foreach (double x in new[] { low.X, high.X })
            foreach (double y in new[] { low.Y, high.Y })
                foreach (double z in new[] { low.Z, high.Z })
                {
                    var at = camera.Project(new Point3(x, y, z), viewport);
                    double margin = ViewCamera.FitMarginPixels - 1e-6;
                    if (at.X < margin || at.Y < margin || at.X > viewport.Width - margin || at.Y > viewport.Height - margin)
                        throw new Exception($"{label}: corner {x},{y},{z} at {at} is outside the {viewport} margin");
                }
    }

    private static (double Width, double Height) Extent(ViewCamera camera, Point3 low, Point3 high, Size viewport)
    {
        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        foreach (double x in new[] { low.X, high.X })
            foreach (double y in new[] { low.Y, high.Y })
                foreach (double z in new[] { low.Z, high.Z })
                {
                    var at = camera.Project(new Point3(x, y, z), viewport);
                    minX = Math.Min(minX, at.X); maxX = Math.Max(maxX, at.X);
                    minY = Math.Min(minY, at.Y); maxY = Math.Max(maxY, at.Y);
                }
        return (maxX - minX, maxY - minY);
    }

    private static Point3 Add(Point3 a, Point3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    private static Point3 Subtract(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    private static Point3 Scale(Point3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);
    private static double Length(Point3 a) => Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);

    internal static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!(Math.Abs(expected - actual) <= tolerance))
            throw new Exception($"{label}: expected {expected:R}, actual {actual:R}");
    }
}
