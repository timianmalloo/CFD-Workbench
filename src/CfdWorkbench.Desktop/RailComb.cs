using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>The teeth, piece counts and automatic gain of both rails at one density (docs/design/rail-comb.md).</summary>
public sealed record RailCombFrame(string SourceHash, int Density, IReadOnlyList<RailTooth> Leading, IReadOnlyList<RailTooth> Trailing,
    MonotoneCount LeadingPieces, MonotoneCount TrailingPieces, double AutoPerMetre);

/// <summary>One drawn tooth, in pixels from its foot: the length after the 60 px clip, whether it was clipped, and whether it is a dot.</summary>
public readonly record struct CombMark(RailTooth Tooth, double Length, bool Clipped, bool Dot);

/// <summary>The rail comb's ladders, the gain rule, and every string it shows (COPY-458 to COPY-473).</summary>
public static class RailComb
{
    /// <summary>"30 px = N per metre": a fixed step, the 1-2-5 ladder (design section 3).</summary>
    public static readonly IReadOnlyList<double> Gains = [0.5, 1, 2, 5, 10, 20, 50, 100, 200];
    public static readonly IReadOnlyList<int> Densities = [16, 32, 64, 128];
    public const int DefaultDensity = 32;
    public const double ToothPixels = 30, ClipPixels = 60, DotPixels = 0.5, MinimumPitchPixels = 3, VisibleFloorPixels = 3;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const char Minus = '−';

    public static RailCombFrame Build(PlanformView plan, int density)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var leading = Planform.Teeth(plan.Leading, density);
        var trailing = Planform.Teeth(plan.Trailing, density);
        var magnitudes = leading.Concat(trailing).Select(tooth => Math.Abs(tooth.Curvature)).Order().ToArray();
        double auto = magnitudes.Length == 0 ? 0 : magnitudes[(int)Math.Floor(0.9 * (magnitudes.Length - 1))];
        return new(plan.SourceHash, density, leading, trailing, Planform.MonotonePieces(plan.Leading), Planform.MonotonePieces(plan.Trailing), auto);
    }

    /// <summary>Teeth to draw at a gain: those closer than 3 px to the last kept tooth are thinned (a one-sided tooth at a jump is
    /// always kept); lengths clip at 60 px; under half a pixel a tooth is a dot.</summary>
    public static IReadOnlyList<CombMark> Marks(IReadOnlyList<RailTooth> teeth, double perMetre, Func<RailTooth, (double X, double Y)> toScreen)
    {
        var marks = new List<CombMark>();
        var always = new bool[teeth.Count];
        for (int i = 1; i < teeth.Count; i++)
            if (teeth[i].StartsPiece) always[i] = always[i - 1] = true;
        (double X, double Y)? last = null;
        double pixelsPerCurvature = perMetre < 1e-9 ? 0 : ToothPixels / perMetre;
        for (int i = 0; i < teeth.Count; i++)
        {
            var tooth = teeth[i];
            var at = toScreen(tooth);
            if (!always[i] && last is { } previous && double.Hypot(at.X - previous.X, at.Y - previous.Y) < MinimumPitchPixels) continue;
            last = at;
            double length = Math.Abs(tooth.Curvature) * pixelsPerCurvature;
            marks.Add(new(tooth, Math.Min(length, ClipPixels), length > ClipPixels, length < DotPixels));
        }
        return marks;
    }

    public static string Radius(double meters)
    {
        double millimetres = meters * 1000;
        return millimetres >= 10_000 ? (millimetres / 1000).ToString("0.00", Invariant) + " m"
            : millimetres < 100 ? millimetres.ToString("0.0", Invariant) + " mm"
            : millimetres.ToString("0", Invariant) + " mm";
    }

    /// <summary>Signed curvature per metre: "κ +3.20 per m", "κ −0.52 per m", or "κ 0" below a thousandth.</summary>
    public static string Kappa(double perMetre)
    {
        double magnitude = Math.Abs(perMetre);
        if (magnitude < 1e-3) return "κ 0";
        return $"κ {(perMetre > 0 ? '+' : Minus)}{magnitude.ToString(magnitude < 10 ? "0.00" : "0.0", Invariant)} per m";
    }

    public static string Gain(double perMetre)
    {
        string text = perMetre < 10 ? perMetre.ToString(perMetre < 1 ? "0.00" : "0.0", Invariant) : perMetre.ToString("0", Invariant);
        if (text.Contains('.')) text = text.TrimEnd('0').TrimEnd('.');
        return text;
    }

    /// <summary>COPY-459 legend: "30 px = 1 per m (R 978 mm)".</summary>
    public static string Legend(double perMetre) =>
        perMetre < 1e-9 ? "no curvature" : $"30 px = {Gain(perMetre)} per m (R {Radius(1 / perMetre)})";

    /// <summary>COPY-470: "Comb scale 30 px = 2 per m."</summary>
    public static string ScaleStatus(double perMetre) => $"Comb scale 30 px = {Gain(perMetre)} per m.";
    public static string DensityStatus(int density) => $"Comb density {density} per rail.";

    public static string LimitStatus(string stepper) => stepper switch
    {
        "larger" => "Largest teeth reached.",
        "smaller" => "Smallest teeth reached.",
        "sparser" => "Sparsest density reached.",
        "denser" => "Densest density reached.",
        _ => throw new ArgumentOutOfRangeException(nameof(stepper))
    };

    public static string Summary(double? gain, int density) =>
        $"Comb · {(gain is { } fixedGain ? $"30 px = {Gain(fixedGain)} per m" : "Auto")} · {density} per rail";

    public static string PiecesLine(string rail, MonotoneCount pieces)
    {
        string railName = rail == "leading" ? "Leading edge" : "Trailing edge";
        string prefix = rail == "leading" ? "LE" : "TE";
        string count = pieces.Count == 1 ? "1 piece" : pieces.Count + " pieces";
        var boundaries = pieces.Boundaries.Select((boundary, i) => $"{prefix}{i + 1} at η {boundary.Eta.ToString("0.00", Invariant)}");
        return railName + " · " + string.Join(" · ", boundaries.Prepend(count));
    }

    public static string ThresholdLine(MonotoneCount leading, MonotoneCount trailing) =>
        $"Ignores reversals under 0.02 ÷ rail length: {leading.ThresholdPerMeter.ToString("0.000", Invariant)} per m (LE), " +
        $"{trailing.ThresholdPerMeter.ToString("0.000", Invariant)} per m (TE).";

    public static string ClippedNote(int clipped) => $"{clipped} teeth clipped (×). Smaller teeth shows them.";
    public const string NoVisibleTooth = "No tooth over 3 px. Larger teeth shows more.";
    public const string CentreNote = "Shared by both rails. Teeth point away from the centre of curvature.";
    public const string AnalysisReason = "Curvature is shown in CAD.";
    public const string CanvasName = "Plan view of the half-wing with the curvature comb. Press [ and ] to walk the points; the Tracing strip reads the radius at the selected point.";

    private static string Side(CurvatureReading reading) =>
        reading.Straight ? "straight, κ 0" : $"R {Radius(reading.RadiusMeters)}, {Kappa(reading.Curvature)}";

    /// <summary>COPY-464: one rail's reading at a station, for the Tracing strip.</summary>
    public static string Reading(string rail, StationReading station)
    {
        string prefix = rail == "leading" ? "LE" : "TE";
        switch (station.Kind)
        {
            case StationKind.Corner or StationKind.CurvatureJump:
                string label = station.Kind == StationKind.Corner ? "corner" : "curvature jumps at this anchor";
                string sign = station.SignChanges ? " · curvature changes sign at this anchor" : "";
                return $"{prefix} {label}: root side {Side(station.Root)} · tip side {Side(station.Tip)}{sign}";
            case StationKind.Inflection:
                return $"{prefix} inflection: R infinite here · κ 0";
            case StationKind.Straight:
                return $"{prefix} R straight · κ 0";
            default:
                return $"{prefix} R {Radius(station.Root.RadiusMeters)} · {Kappa(station.Root.Curvature)} {(station.Root.Curvature > 0 ? "convex" : "concave")}";
        }
    }

    /// <summary>The station a hover at <paramref name="t"/> reads: within 0.004 of a repeated knot it is the knot, both sides.</summary>
    public static StationReading ReadNear(CurveView curve, double t)
    {
        var knots = curve.Knots;
        for (int i = 4; i + 1 < knots.Count - 4; i++)
            if (knots[i] == knots[i + 1] && knots[i] > 0 && knots[i] < 1 && knots[i - 1] != knots[i] && Math.Abs(knots[i] - t) < 0.004
                && Planform.ReadAt(curve, knots[i]) is { Kind: StationKind.Corner or StationKind.CurvatureJump } jump)
                return jump;
        return Planform.ReadAt(curve, t);
    }
}
