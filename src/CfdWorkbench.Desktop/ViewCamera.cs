using Avalonia;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public enum Projection { Orthographic, Perspective }

/// <summary>The named cameras of CAD-06 (Side is the Starboard camera, spec §B1).</summary>
public enum NamedCamera { Top, Front, Side, Iso, Bottom, Back, Port }

public enum DisplayMode { Shaded, Wireframe }

/// <summary>The model-area layout, a session value (M1.2e persists it): Plan + 3D · Four views · One view.</summary>
public readonly record struct ViewLayout(ViewArrangement Arrangement, SingleView Single)
{
    public static ViewLayout Plan3d => new(ViewArrangement.Plan3d, SingleView.Plan);
    public static ViewLayout Four => new(ViewArrangement.Four, SingleView.Plan);
    public static ViewLayout One(SingleView view) => new(ViewArrangement.One, view);

    public bool Shows(SingleView view) => Arrangement switch
    {
        ViewArrangement.Plan3d => view is SingleView.Plan or SingleView.ThreeD,
        ViewArrangement.Four => true,
        _ => Single == view
    };
}

/// <summary>
/// A camera as an immutable value: every navigation verb is a pure function, so pointer, trackpad, keyboard and menu
/// produce the same camera. Body frame: +x aft, +y starboard, +z up (FoilDSL §5.2). Azimuth 0° looks aft from ahead of
/// the nose (Front), 90° looks inboard from starboard (Side); elevation +90° looks down. The screen right vector comes
/// from the azimuth alone, so the poles never flip and Top matches the Plan (starboard right, aft down). Angles use
/// half-turn trigonometry (<see cref="double.SinPi"/>), exact at every quarter turn; a camera angle is never a placement.
/// </summary>
public readonly record struct ViewCamera(Point3 Target, double AzimuthDegrees, double ElevationDegrees, double Distance,
    Projection Projection)
{
    public const double FitMarginPixels = 24;
    public const double MinimumZoom = 0.01;
    public const double MaximumZoom = 100;

    // A 30° vertical field of view: tan 15° as a half-turn fraction.
    private static readonly double TanHalfField = double.TanPi(1.0 / 12);

    /// <summary>The preset this camera still looks along, or null once it has been orbited (Free).</summary>
    public NamedCamera? Name { get; init; }

    /// <summary>The distance the last Fit chose; zoom is clamped to [0.01, 100] × it.</summary>
    public double FitDistance { get; init; }

    public static ViewCamera Named(NamedCamera name, Point3 minimum, Point3 maximum, Size viewport)
    {
        var (azimuth, elevation, projection) = name switch
        {
            NamedCamera.Top => (180d, 90d, Projection.Orthographic),
            NamedCamera.Front => (0d, 0d, Projection.Orthographic),
            NamedCamera.Side => (90d, 0d, Projection.Orthographic),
            NamedCamera.Bottom => (0d, -90d, Projection.Orthographic),
            NamedCamera.Back => (180d, 0d, Projection.Orthographic),
            NamedCamera.Port => (270d, 0d, Projection.Orthographic),
            // DR-VIEW-5: from the front, starboard and above (the approved mockup), leading edge toward the viewer.
            NamedCamera.Iso => (45d, 30d, Projection.Perspective),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        return new ViewCamera(default, azimuth, elevation, 1, projection) { Name = name }.Fit(minimum, maximum, viewport);
    }

    /// <summary>Turntable about +z around the target; elevation clamped to ±90° (no flip); the result is Free and perspective.</summary>
    public ViewCamera Orbit(double deltaAzimuth, double deltaElevation)
    {
        double azimuth = (AzimuthDegrees + deltaAzimuth) % 360;
        if (azimuth < 0) azimuth += 360;
        return this with
        {
            AzimuthDegrees = azimuth,
            ElevationDegrees = Math.Clamp(ElevationDegrees + deltaElevation, -90, 90),
            Projection = Projection.Perspective,
            Name = null
        };
    }

    /// <summary>Moves the view so the model follows the pointer by (dx, dy) pixels at the target's depth.</summary>
    public ViewCamera Pan(double dxPixels, double dyPixels, Size viewport)
    {
        var (right, up, _) = Basis();
        double scale = Focal(viewport) / Distance;
        return this with { Target = Add(Target, Add(Scale(right, -dxPixels / scale), Scale(up, dyPixels / scale))) };
    }

    /// <summary>Zooms by <paramref name="factor"/> (&gt; 1 zooms in) keeping the model point under the pointer, at the target's depth, fixed.</summary>
    public ViewCamera ZoomAbout(Point pointer, double factor, Size viewport)
    {
        if (!(factor > 0) || !double.IsFinite(factor)) return this;
        double fitted = FitDistance > 0 ? FitDistance : Distance;
        double distance = Math.Clamp(Distance / factor, MinimumZoom * fitted, MaximumZoom * fitted);
        var (right, up, _) = Basis();
        double focal = Focal(viewport);
        double offsetX = pointer.X - viewport.Width / 2, offsetY = pointer.Y - viewport.Height / 2;
        double before = Distance / focal, after = distance / focal;
        var underPointer = Add(Target, Add(Scale(right, offsetX * before), Scale(up, -offsetY * before)));
        return this with
        {
            Distance = distance,
            FitDistance = fitted,
            Target = Add(underPointer, Add(Scale(right, -offsetX * after), Scale(up, offsetY * after)))
        };
    }

    /// <summary>Centres the bounds and chooses the distance that keeps them inside the viewport less <see cref="FitMarginPixels"/>.</summary>
    public ViewCamera Fit(Point3 minimum, Point3 maximum, Size viewport)
    {
        var target = new Point3((minimum.X + maximum.X) / 2, (minimum.Y + maximum.Y) / 2, (minimum.Z + maximum.Z) / 2);
        double halfWidth = Math.Max(1, viewport.Width / 2 - FitMarginPixels);
        double halfHeight = Math.Max(1, viewport.Height / 2 - FitMarginPixels);
        double focal = Focal(viewport);
        double distance;
        if (Projection == Projection.Orthographic)
        {
            var (right, up, _) = Basis();
            double extentX = 0, extentY = 0;
            foreach (var corner in Corners(minimum, maximum))
            {
                var offset = Subtract(corner, target);
                extentX = Math.Max(extentX, Math.Abs(Dot(offset, right)));
                extentY = Math.Max(extentY, Math.Abs(Dot(offset, up)));
            }
            double scale = Math.Min(halfWidth / Math.Max(extentX, 1e-9), halfHeight / Math.Max(extentY, 1e-9));
            distance = focal / scale;
        }
        else
        {
            // DR-VIEW-6: the box's projection fills the view less the margin, centred as the mockup centres it. Perspective
            // draws the near side larger, so the camera slides parallel to the screen until the projected bounds are
            // centred, and at each step takes the nearest distance that keeps every corner inside the margin: a corner at
            // screen offset a·f/(c + d) stays inside half-extent h while d ≥ |a|·f/h − c (closed form). Each slide is a
            // Newton step: the two extreme corners move by f·s/(their depth), so it converges in a few steps; 64 is a cap.
            var (right, up, forward) = Basis();
            double radius = Math.Max(1e-9, Length(Subtract(maximum, minimum)) / 2);
            var corners = Corners(minimum, maximum).ToArray();
            distance = 1;
            for (int step = 0; step < 64; step++)
            {
                distance = double.NegativeInfinity;
                foreach (var corner in corners)
                {
                    var offset = Subtract(corner, target);
                    double along = Dot(offset, forward);
                    distance = Math.Max(distance, Math.Max(Math.Abs(Dot(offset, right)) * focal / halfWidth,
                        Math.Abs(Dot(offset, up)) * focal / halfHeight) - along);
                    distance = Math.Max(distance, radius * 1e-3 - along);
                }
                // Screen offsets of the extreme corners on each axis, with the scale (f / depth) of each.
                (double At, double Scale) left = (double.PositiveInfinity, 0), rightmost = (double.NegativeInfinity, 0);
                (double At, double Scale) low = (double.PositiveInfinity, 0), high = (double.NegativeInfinity, 0);
                foreach (var corner in corners)
                {
                    var offset = Subtract(corner, target);
                    double scale = focal / (Dot(offset, forward) + distance);
                    double x = Dot(offset, right) * scale, y = Dot(offset, up) * scale;
                    if (x < left.At) left = (x, scale);
                    if (x > rightmost.At) rightmost = (x, scale);
                    if (y < low.At) low = (y, scale);
                    if (y > high.At) high = (y, scale);
                }
                double shiftX = (left.At + rightmost.At) / 2, shiftY = (low.At + high.At) / 2;
                if (Math.Abs(shiftX) < 1e-9 && Math.Abs(shiftY) < 1e-9) break;
                // Sliding the camera by s along an axis moves a corner by −s·scale; centring the pair needs s = shift / mean scale.
                target = Add(target, Add(Scale(right, shiftX * 2 / (left.Scale + rightmost.Scale)), Scale(up, shiftY * 2 / (low.Scale + high.Scale))));
            }
        }
        return this with { Target = target, Distance = distance, FitDistance = distance };
    }

    public Point Project(Point3 point, Size viewport)
    {
        var (right, up, forward) = Basis();
        var offset = Subtract(point, Target);
        double scale = Focal(viewport) / (Projection == Projection.Orthographic ? Distance : Dot(offset, forward) + Distance);
        return new Point(viewport.Width / 2 + Dot(offset, right) * scale, viewport.Height / 2 - Dot(offset, up) * scale);
    }

    /// <summary>Distance from the eye along the view axis (larger is farther); the painter's order sorts on it.</summary>
    public double Depth(Point3 point) => Dot(Subtract(point, Target), Basis().Forward) + Distance;

    /// <summary>The unit vector from a point toward the viewer, for back-face culling and the headlight.</summary>
    public Point3 TowardViewer(Point3 point)
    {
        var forward = Basis().Forward;
        if (Projection == Projection.Orthographic) return Scale(forward, -1);
        var eye = Subtract(Target, Scale(forward, Distance));
        var toward = Subtract(eye, point);
        return Scale(toward, 1 / Math.Max(1e-300, Length(toward)));
    }

    public string Title => Name switch
    {
        NamedCamera named => named.ToString(),
        _ => "Free · az " + Degrees(AzimuthDegrees) + "° · el " + Degrees(ElevationDegrees) + "°"
    };

    private static string Degrees(double value)
    {
        long rounded = (long)Math.Round(value, MidpointRounding.AwayFromZero);
        return rounded < 0 ? "−" + (-rounded).ToString(CultureInfo.InvariantCulture) : rounded.ToString(CultureInfo.InvariantCulture);
    }

    private (Point3 Right, Point3 Up, Point3 Forward) Basis()
    {
        double cosA = double.CosPi(AzimuthDegrees / 180), sinA = double.SinPi(AzimuthDegrees / 180);
        double cosE = double.CosPi(ElevationDegrees / 180), sinE = double.SinPi(ElevationDegrees / 180);
        var forward = new Point3(cosE * cosA, -cosE * sinA, -sinE);
        var right = new Point3(-sinA, -cosA, 0);
        var up = new Point3(cosA * sinE, -sinA * sinE, cosE);   // right × forward
        return (right, up, forward);
    }

    private static double Focal(Size viewport) => Math.Min(viewport.Width, viewport.Height) / (2 * TanHalfField);

    private static IEnumerable<Point3> Corners(Point3 a, Point3 b)
    {
        foreach (double x in new[] { a.X, b.X })
            foreach (double y in new[] { a.Y, b.Y })
                foreach (double z in new[] { a.Z, b.Z })
                    yield return new Point3(x, y, z);
    }

    internal static Point3 Add(Point3 a, Point3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    internal static Point3 Subtract(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    internal static Point3 Scale(Point3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);
    internal static double Dot(Point3 a, Point3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    internal static double Length(Point3 a) => Math.Sqrt(Dot(a, a));
    internal static Point3 Cross(Point3 a, Point3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
}
