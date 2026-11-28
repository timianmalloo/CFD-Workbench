namespace CfdWorkbench.Core;

/// <summary>
/// Pure zero-I/O kernel for Wave 13A rounded &amp; radiused wing tips (<c>FoilDSL 4.2</c>).
/// Shared by <see cref="WingEstimates"/>, <see cref="Placement"/>, <see cref="StlExport"/>,
/// <see cref="ThreeMfExport"/>, <see cref="CadExportExtractor"/>, <see cref="StepAp203Export"/>,
/// <see cref="Rhino3dmExport"/>, and Desktop display (<c>ADR-0010</c> single authority).
/// </summary>
public static class TipCapMath
{
    // simplify: TipCapMath evaluates G0-watertight semi-elliptical cap P(x, theta); ceiling: finite non-zero tip thickness & ratio in [0.25, 2.0]; upgrade trigger: if CNC/CFD qualification requires true G1-tangent-matched variable-cross-section tip fairings across nonzero dihedral/chord slopes.

    public const double MinCapRatio = 0.25;
    public const double MaxCapRatio = 2.0;
    public const double DefaultCapRatio = 1.0;
    public const int MinThetaSteps = 12;
    public const int MaxThetaSteps = 64;

    private const int CapCollocationCount = 13;
    private static readonly (double[] KnotsV, double[] Alpha, double[] Beta) Collocation = BuildCapCollocation();

    /// <summary>
    /// Exact analytical protrusion of the semi-elliptical tip cap beyond <c>y = ±half_span</c>, in metres (O(1)).
    /// Under Rule A, <c>max_x(unitThickness(x)) == 1.0</c> at <c>η = 1</c>, so maximum half-thickness at the tip
    /// is <c>0.5 * tipChordMeters * tipThicknessRatio</c>.
    /// </summary>
    public static double MaxCapProtrusionMeters(double tipChordMeters, double tipThicknessRatio, double capRatio)
    {
        if (!double.IsFinite(tipChordMeters) || !double.IsFinite(tipThicknessRatio) || !double.IsFinite(capRatio))
            return 0.0;
        if (tipChordMeters <= 0.0 || tipThicknessRatio <= 0.0 || capRatio <= 0.0)
            return 0.0;
        return 0.5 * capRatio * tipChordMeters * tipThicknessRatio;
    }

    /// <summary>
    /// Evaluates one point <c>P(x, θ)</c> on the analytical semi-elliptical 3D tip cap at
    /// <paramref name="thetaOverPi"/> ∈ [0, 1] (<c>θ = π · thetaOverPi</c>) between placed upper and lower tip points.
    /// Uses managed <see cref="double.SinCosPi(double)"/> (<c>Ruling 156</c>).
    /// </summary>
    public static Point3 EvaluateCapPoint(Point3 upper, Point3 lower, double thetaOverPi, double capRatio, bool port = false)
    {
        double mx = 0.5 * (upper.X + lower.X);
        double my = 0.5 * (upper.Y + lower.Y);
        double mz = 0.5 * (upper.Z + lower.Z);
        double hx = 0.5 * (upper.X - lower.X);
        double hz = 0.5 * (upper.Z - lower.Z);
        double halfThick = Math.Sqrt(hx * hx + hz * hz);
        var (sin, cos) = double.SinCosPi(thetaOverPi);
        double sign = port ? -1.0 : 1.0;
        return new Point3(
            mx + cos * hx,
            my + sign * sin * capRatio * halfThick,
            mz + cos * hz);
    }

    /// <summary>
    /// Evaluates the maximum half-thickness in metres across a placed tip section.
    /// </summary>
    public static double MaxHalfThicknessMeters(PlacedSection tipSection)
    {
        ArgumentNullException.ThrowIfNull(tipSection);
        double maxHalf = 0.0;
        int n = Math.Min(tipSection.Upper.Count, tipSection.Lower.Count);
        for (int j = 0; j < n; j++)
        {
            double hx = 0.5 * (tipSection.Upper[j].X - tipSection.Lower[j].X);
            double hz = 0.5 * (tipSection.Upper[j].Z - tipSection.Lower[j].Z);
            double half = Math.Sqrt(hx * hx + hz * hz);
            if (half > maxHalf) maxHalf = half;
        }
        return maxHalf;
    }

    /// <summary>
    /// Smallest even angular step count <c>N_θ ∈ [12, 64]</c> such that the semi-elliptical cap's angular chordal sag
    /// satisfies <c>ratio * maxHalfThickMm * (1 - cos(π / (2 * N_θ))) &lt;= toleranceMm</c>.
    /// </summary>
    public static int CapThetaSteps(double maxHalfThickMm, double capRatio, double toleranceMm)
    {
        if (!double.IsFinite(maxHalfThickMm) || maxHalfThickMm <= 0.0 ||
            !double.IsFinite(capRatio) || capRatio <= 0.0 ||
            !double.IsFinite(toleranceMm) || toleranceMm <= 0.0)
            return MinThetaSteps;

        double radiusMm = capRatio * maxHalfThickMm;
        for (int n = MinThetaSteps; n <= MaxThetaSteps; n += 2)
        {
            double sagMm = radiusMm * (1.0 - double.CosPi(0.5 / n));
            if (sagMm <= toleranceMm) return n;
        }
        return MaxThetaSteps;
    }

    /// <summary>
    /// Builds the shared-knot cubic tensor-product NURBS surface for the starboard 3D rounded tip cap
    /// directly from <paramref name="upperTip"/> and <paramref name="lowerTip"/> poles (in millimetres),
    /// guaranteeing bit-for-bit pole identity at <c>v = 0</c> (upper) and <c>v = 1</c> (lower).
    /// </summary>
    public static CadNurbsSurface BuildStarboardTipCap(CadNurbsCurve upperTip, CadNurbsCurve lowerTip, double capRatio)
    {
        ArgumentNullException.ThrowIfNull(upperTip);
        ArgumentNullException.ThrowIfNull(lowerTip);
        Guard.Require(upperTip.Poles.Length == lowerTip.Poles.Length && upperTip.Degree == lowerTip.Degree, "DSL-CURVE");

        int nu = upperTip.Poles.Length;
        var (knotsV, alpha, beta) = Collocation;
        int nv = alpha.Length;
        var poles = new CadVec3[nu, nv];

        for (int i = 0; i < nu; i++)
        {
            var u = upperTip.Poles[i];
            var l = lowerTip.Poles[i];
            var m = 0.5 * (u + l);
            var h = 0.5 * (u - l);
            double halfThick = Math.Sqrt(h.X * h.X + h.Z * h.Z);

            poles[i, 0] = u;
            poles[i, nv - 1] = l;
            for (int j = 1; j < nv - 1; j++)
            {
                poles[i, j] = new CadVec3(
                    m.X + alpha[j] * h.X,
                    m.Y + beta[j] * capRatio * halfThick,
                    m.Z + alpha[j] * h.Z);
            }
        }

        return new CadNurbsSurface(
            upperTip.Degree,
            3,
            (double[])upperTip.Knots.Clone(),
            (double[])knotsV.Clone(),
            poles);
    }

    /// <summary>
    /// Builds the planar NURBS surface closing the semi-elliptical trailing-edge end of a rounded tip cap
    /// when <c>closure open</c> is active (in millimetres), for Rhino 3DM export.
    /// </summary>
    public static CadNurbsSurface BuildStarboardTipCapTeEnd(CadNurbsCurve capTeArc, CadVec3 upperTe, CadVec3 lowerTe, bool nonSingular = false)
    {
        ArgumentNullException.ThrowIfNull(capTeArc);
        int nv = capTeArc.Poles.Length;
        if (!nonSingular)
        {
            var mid = 0.5 * (upperTe + lowerTe);
            var poles = new CadVec3[2, nv];
            for (int j = 0; j < nv; j++)
            {
                poles[0, j] = mid;
                poles[1, j] = capTeArc.Poles[j];
            }
            return new CadNurbsSurface(1, capTeArc.Degree, [0.0, 0.0, 1.0, 1.0], (double[])capTeArc.Knots.Clone(), poles);
        }

        // Non-singular ruled planar NURBS between the straight TE chord segment U(1)->L(1) and the cap TE arc P(1, theta).
        // Linear chord poles interpolated along v match the 13-point collocation parameterization cleanly with nonzero u-spans everywhere except endpoints.
        var chordPts = new CadVec3[nv];
        for (int j = 0; j < nv; j++)
        {
            double frac = j / (double)(nv - 1);
            chordPts[j] = (1.0 - frac) * upperTe + frac * lowerTe;
        }
        var chordParams = new double[nv];
        for (int j = 0; j < nv; j++) chordParams[j] = j / (double)(nv - 1);
        var chordCurve = CadNurbsMath.InterpolateCurve(chordParams, chordPts, degree: capTeArc.Degree);
        var ruledPoles = new CadVec3[2, nv];
        for (int j = 0; j < nv; j++)
        {
            ruledPoles[0, j] = chordCurve.Poles[j];
            ruledPoles[1, j] = capTeArc.Poles[j];
        }
        return new CadNurbsSurface(1, capTeArc.Degree, [0.0, 0.0, 1.0, 1.0], (double[])capTeArc.Knots.Clone(), ruledPoles);
    }

    internal static bool IsVerticalTip(Curve curve) =>
        curve.Path is "leading" or "trailing" &&
        curve.Points.Length >= 4 &&
        curve.Points[^2][0] == 1.0;

    internal static bool HasVerticalTip(Definition definition) =>
        definition.Kind == "foil" &&
        (IsVerticalTip(definition.Curves["leading"]) || IsVerticalTip(definition.Curves["trailing"]));

    private static (double[] KnotsV, double[] Alpha, double[] Beta) BuildCapCollocation()
    {
        var vParams = new double[CapCollocationCount];
        var pts = new CadVec3[CapCollocationCount];
        for (int j = 0; j < CapCollocationCount; j++)
        {
            double frac = j / (double)(CapCollocationCount - 1);
            vParams[j] = frac;
            var (sin, cos) = double.SinCosPi(frac);
            pts[j] = new CadVec3(cos, sin, 0.0);
        }
        var curve = CadNurbsMath.InterpolateCurve(vParams, pts, degree: 3);
        var alpha = new double[CapCollocationCount];
        var beta = new double[CapCollocationCount];
        for (int j = 0; j < CapCollocationCount; j++)
        {
            alpha[j] = curve.Poles[j].X;
            beta[j] = curve.Poles[j].Y;
        }
        alpha[0] = 1.0;
        alpha[^1] = -1.0;
        beta[0] = 0.0;
        beta[^1] = 0.0;
        return (curve.Knots, alpha, beta);
    }
}
