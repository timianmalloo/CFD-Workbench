using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Near-field force and moment of one solved strip, body axes, about the frame origin. Additive across strips of one run.</summary>
public sealed record StripForce(int J, double Y, double Eta, double Chord, double Gamma,
    double Fx, double Fy, double Fz, double Mx, double My, double Mz);

/// <summary>One solved strip. Γ and w_T are what the run stores; the rest is derived on read from the same solve.</summary>
public sealed record LatticeStrip(int J, double Y, double Eta, double Chord, double Dy, double Gamma, double Downwash,
    double InducedAngleDeg, double TwistDeg, double SweepDeg, double ClLocal, double YInboard, double YOutboard);

/// <summary>
/// The solved lattice: Γ per strip (strip totals), the Trefftz downwash w_T per strip, and the solver diagnostics.
/// Derived; the lattice itself is rebuilt on read, never stored (design §3.1).
/// </summary>
public sealed record LatticeSolution(IReadOnlyList<double> Gamma, IReadOnlyList<double> DownwashTrefftz, RunDiagnostics Diagnostics)
{
    public IReadOnlyList<LatticeStrip> Strips { get; init; } = Array.Empty<LatticeStrip>();
    public IReadOnlyList<StripForce> Forces { get; init; } = Array.Empty<StripForce>();
    public IReadOnlyList<string> Exclusions { get; init; } = Array.Empty<string>();
    public double MinimumPanelArea { get; init; }
}

/// <summary>A lattice that cannot return a finite solution. <see cref="Code"/> is the stable failure code.</summary>
public sealed class LatticeFailedException : Exception
{
    public LatticeFailedException(string code, string reason) : base(reason) => Code = code;
    public string Code { get; }
}

/// <summary>§13.2 plants. The product path is <see cref="None"/>; the fixture harness passes the others.</summary>
internal enum LatticePlant
{
    None,
    WakePerPanel,
    ControlAtMidPanel,
    BoundAtMidChord,
    NormalFromLeadingEdge,
    InducedFromControlPoint,
    NearFieldTrailingOmitted,
    NonFiniteAsZero,
    BoundUnswept,
    PivotWholeRow,
    DownwashNeighbour,
    CamberSurfaceHorseshoe,
    FrontBoundSweep
}

/// <summary>
/// Horseshoe vortices in each panel's local uncambered plane on both halves (design §5.2).
/// Control-point camber slope and twist supply normals; the frame supplies the elevated vortex geometry.
/// </summary>
public static class VortexLattice
{
    // Degrees factor without the Placement.cs-only spellings (the single-site gate).
    private const double StraightAngleDegrees = 180;
    internal static double ToRadians(double degrees) => degrees * (Math.PI / StraightAngleDegrees);
    internal static double ToDegrees(double radians) => radians * (StraightAngleDegrees / Math.PI);

    // assume: the PRE signature has no water record. It returns forces at ρ = 1 kg/m³. Callers that have a
    // WaterRecord use the density overload. Confirmed by this signature. If a caller treats the ρ = 1 forces as
    // newtons, every load is low by ρ; CL and the Trefftz/near-field ratio are unchanged because both scale.
    public static LatticeSolution Solve(IReadOnlyList<SectionSample> sections, RunSettings settings, OperatingPoint op,
        CancellationToken cancellation) =>
        Solve(sections, settings, op, 1, LatticePlant.None, cancellation);

    public static LatticeSolution Solve(IReadOnlyList<SectionSample> sections, RunSettings settings, OperatingPoint op,
        double rho, CancellationToken cancellation) =>
        Solve(sections, settings, op, rho, LatticePlant.None, cancellation);

    internal static LatticeSolution Solve(IReadOnlyList<SectionSample> sections, RunSettings settings, OperatingPoint op,
        double rho, LatticePlant plant, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(op);
        if (sections.Count < 2) throw new ArgumentException("A lattice needs two sections.", nameof(sections));
        if (!double.IsFinite(op.Speed) || op.Speed <= 0)
            throw new ArgumentOutOfRangeException(nameof(op), op.Speed, "Speed must be finite and positive.");
        if (!double.IsFinite(op.AlphaDeg)) throw new LatticeFailedException("ANA-NONFINITE", "The angle of attack is not finite.");
        if (!double.IsFinite(rho) || rho <= 0) throw new ArgumentOutOfRangeException(nameof(rho), rho, "Density must be finite and positive.");
        if (settings.WakeDirection != "+x")
            throw new ArgumentOutOfRangeException(nameof(settings), settings.WakeDirection, "The wake direction is +x.");
        if (settings.NSpanPerHalf < 1 || settings.NChord < 1 || settings.WakeSpans < 1)
            throw new ArgumentOutOfRangeException(nameof(settings), "Span, chord and wake counts are at least 1.");
        cancellation.ThrowIfCancellationRequested();

        SectionSample[] samples = sections.ToArray();
        Array.Sort(samples, (a, b) => SectionY(a).CompareTo(SectionY(b)));
        double yMin = SectionY(samples[0]), yMax = SectionY(samples[^1]);
        if (!(yMax > yMin)) throw new ArgumentException("The sections have no span.", nameof(sections));
        double[] edges = SpanStations(yMin, yMax, settings.NSpanPerHalf, settings.SpanSpacing);
        double span = yMax - yMin;
        int nStrips = edges.Length - 1;
        double[] fractions = Fractions(settings.NChord, settings.ChordSpacing);
        double boundFrac = plant == LatticePlant.BoundAtMidChord ? 0.5 : 0.25;
        double controlFrac = plant == LatticePlant.ControlAtMidPanel ? 0.5 : 0.75;
        double wakeReach = plant == LatticePlant.WakePerPanel
            ? settings.WakeSpans * span / nStrips
            : settings.WakeSpans * span;

        var horses = new List<Horseshoe>(nStrips * settings.NChord);
        var kept = new List<StripGeom>(nStrips);
        var exclusions = new List<string>();
        double minArea = double.PositiveInfinity;
        double rootBoundX = double.NaN;
        for (int s = 0; s < nStrips; s++)
        {
            cancellation.ThrowIfCancellationRequested();
            double ya = edges[s], yb = edges[s + 1], ym = 0.5 * (ya + yb);
            SectionSample sideA = At(samples, ya, span), sideB = At(samples, yb, span), mid = At(samples, ym, span);
            double chord = ChordOf(mid);
            if (!(chord >= Settings.MinimumControlChordMeters))
            {
                exclusions.Add("strip η " + mid.Frame.Eta.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)
                    + " y " + ym.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)
                    + " chord " + chord.ToString("G3", System.Globalization.CultureInfo.InvariantCulture)
                    + " m excluded: control-point chord below 10 µm");
                continue;
            }
            int strip = kept.Count;
            double sweep = SweepOf(sideA, sideB, ya, yb);
            kept.Add(new StripGeom(strip, ym, mid.Frame.Eta, chord, yb - ya, mid.Frame.TwistDegrees, sweep, ya, yb));
            for (int k = 0; k < settings.NChord; k++)
            {
                double f0 = fractions[k], f1 = fractions[k + 1];
                double fb = f0 + boundFrac * (f1 - f0);
                double fc = f0 + controlFrac * (f1 - f0);
                Point3 c00 = OnCamber(sideA, f0), c10 = OnCamber(sideA, f1);
                Point3 c01 = OnCamber(sideB, f0), c11 = OnCamber(sideB, f1);
                Point3 a = OnPlane(sideA, fb, ya), b = OnPlane(sideB, fb, yb);
                Point3 midPoint = OnPlane(mid, fc, ym);
                Point3 cp = new(midPoint.X, ym, 0.5 * (a.Z + b.Z));
                if (plant == LatticePlant.CamberSurfaceHorseshoe)
                {
                    a = OnCamber(sideA, fb);
                    b = OnCamber(sideB, fb);
                    cp = OnCamber(mid, fc);
                }
                if (!Finite(a) || !Finite(b) || !Finite(cp) || !Finite(c00) || !Finite(c10) || !Finite(c01) || !Finite(c11))
                    throw new LatticeFailedException("ANA-NONFINITE", "A panel coordinate is not finite.");
                if (plant == LatticePlant.BoundUnswept)
                {
                    if (double.IsNaN(rootBoundX)) rootBoundX = BoundXNearRoot(samples, fractions, boundFrac);
                    a = new Point3(rootBoundX, a.Y, a.Z);
                    b = new Point3(rootBoundX, b.Y, b.Z);
                }
                Vector3 cornerNormal = plant == LatticePlant.NormalFromLeadingEdge
                    ? Cross(Sub(c01, c00), Sub(c11, c01))
                    : Cross(Sub(c11, c00), Sub(c10, c01));
                double area = 0.5 * cornerNormal.Length;
                if (area < minArea) minArea = area;
                if (!(area > 1e-14) || !cornerNormal.IsFinite)
                    throw Fail(plant, "ANA-SOLVE-SINGULAR", "A panel has no area.");
                double slope = CamberSlopeAt(mid, fc);
                double twist = ToRadians(mid.Frame.TwistDegrees);
                Point3 tangent = new(Math.Cos(twist) + Math.Sin(twist) * slope, 0,
                    -Math.Sin(twist) + Math.Cos(twist) * slope);
                // The bound segment retains the elevated local plane; the chord tangent reads the
                // camber derivative at this panel's control point, rotated by the section twist.
                Vector3 normal = plant == LatticePlant.NormalFromLeadingEdge
                    ? cornerNormal : Cross(tangent, Sub(b, a));
                if (!normal.IsFinite || !(normal.Length > 0))
                    throw Fail(plant, "ANA-NONFINITE", "A panel normal is not finite.");
                if (normal.Z < 0) normal = new Vector3(-normal.X, -normal.Y, -normal.Z);
                normal = normal.Unit();
                var farA = new Point3(a.X + wakeReach, a.Y, a.Z);
                var farB = new Point3(b.X + wakeReach, b.Y, b.Z);
                horses.Add(new Horseshoe(a, b, farA, farB, cp, normal, strip, area));
            }
        }
        if (horses.Count == 0) throw new LatticeFailedException("ANA-SOLVE-SINGULAR", "Every strip was excluded.");
        if (horses.Count > Settings.UnknownCap)
            throw new ArgumentOutOfRangeException(nameof(settings), horses.Count,
                "VLM: " + horses.Count + " unknowns exceed the " + Settings.UnknownCap + " cap.");
        if (double.IsPositiveInfinity(minArea)) minArea = 0;

        int n = horses.Count;
        var matrix = new double[n * n];
        var rhs = new double[n];
        double alpha = ToRadians(op.AlphaDeg);
        double vx = op.Speed * Math.Cos(alpha), vz = op.Speed * Math.Sin(alpha);
        for (int i = 0; i < n; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            Horseshoe hi = horses[i];
            int row = i * n;
            for (int j = 0; j < n; j++)
            {
                Velocity(horses[j], hi.Cp, 0, false, out double ux, out double uy, out double uz);
                matrix[row + j] = ux * hi.Normal.X + uy * hi.Normal.Y + uz * hi.Normal.Z;
            }
            rhs[i] = -(vx * hi.Normal.X + vz * hi.Normal.Z);
        }

        DenseSolution dense = SolveDense(matrix, rhs, n, plant, cancellation);
        double[] gamma = dense.X;
        double residual = dense.ResidualInf;
        double kappa = dense.Kappa1;

        var stripGamma = new double[kept.Count];
        for (int j = 0; j < n; j++) stripGamma[horses[j].Strip] += gamma[j];

        var yA = new double[kept.Count];
        var yB = new double[kept.Count];
        var dy = new double[kept.Count];
        for (int s = 0; s < kept.Count; s++)
        {
            yA[s] = kept[s].Ya;
            yB[s] = kept[s].Yb;
            dy[s] = kept[s].Dy;
        }
        var strips = new LatticeStrip[kept.Count];
        var gammas = new double[kept.Count];
        var washes = new double[kept.Count];
        for (int s = 0; s < kept.Count; s++)
        {
            // w_T at the strip y-midpoint (§5.2). The θ-midpoint was measured and rejected: e turns non-monotone in the
            // lattice and the outermost strip reads +48° at the default lattice (note §Repair).
            int at = plant == LatticePlant.DownwashNeighbour ? (s + 1 < kept.Count ? s + 1 : s - 1) : s;
            double w = Trefftz.Downwash(kept[at].Y, stripGamma, yA, yB);
            double ai = plant == LatticePlant.InducedFromControlPoint
                ? InducedFromTotal(horses, gamma, kept[s], op)
                : ToDegrees(-w / (2 * op.Speed));
            double cl = 2 * stripGamma[s] / (op.Speed * kept[s].Chord);
            strips[s] = new LatticeStrip(s, kept[s].Y, kept[s].Eta, kept[s].Chord, kept[s].Dy, stripGamma[s], w,
                ai, kept[s].Twist, plant == LatticePlant.FrontBoundSweep ? FrontBoundSweepOf(horses, s) : kept[s].Sweep,
                cl, kept[s].Ya, kept[s].Yb);
            gammas[s] = stripGamma[s];
            washes[s] = w;
        }

        var forces = new StripForce[kept.Count];
        var fx = new double[kept.Count];
        var fy = new double[kept.Count];
        var fz = new double[kept.Count];
        var mx = new double[kept.Count];
        var my = new double[kept.Count];
        var mz = new double[kept.Count];
        double cutoff = settings.SingularityCutoff;
        for (int j = 0; j < n; j++)
        {
            Horseshoe horse = horses[j];
            double px = 0.5 * (horse.A.X + horse.B.X);
            double py = 0.5 * (horse.A.Y + horse.B.Y);
            double pz = 0.5 * (horse.A.Z + horse.B.Z);
            double ux = 0, uy = 0, uz = 0;
            InducedAtBound(horses, gamma, horse, cutoff, plant == LatticePlant.NearFieldTrailingOmitted, ref ux, ref uy, ref uz);
            double lx = horse.B.X - horse.A.X, ly = horse.B.Y - horse.A.Y, lz = horse.B.Z - horse.A.Z;
            double qx = vx + ux, qy = uy, qz = vz + uz;
            double g = gamma[j];
            // ρ (V∞ + v) × Γℓ
            double ffx = rho * g * (qy * lz - qz * ly);
            double ffy = rho * g * (qz * lx - qx * lz);
            double ffz = rho * g * (qx * ly - qy * lx);
            if (!double.IsFinite(ffx) || !double.IsFinite(ffy) || !double.IsFinite(ffz))
            {
                if (plant == LatticePlant.NonFiniteAsZero) continue;
                throw new LatticeFailedException("ANA-NONFINITE", "A panel force is not finite.");
            }
            int s = horse.Strip;
            fx[s] += ffx;
            fy[s] += ffy;
            fz[s] += ffz;
            mx[s] += py * ffz - pz * ffy;
            my[s] += pz * ffx - px * ffz;
            mz[s] += px * ffy - py * ffx;
        }
        for (int s = 0; s < kept.Count; s++)
            forces[s] = new StripForce(s, kept[s].Y, kept[s].Eta, kept[s].Chord, stripGamma[s], fx[s], fy[s], fz[s], mx[s], my[s], mz[s]);

        return new LatticeSolution(gammas, washes, new RunDiagnostics(residual, kappa))
        {
            Strips = strips,
            Forces = forces,
            Exclusions = exclusions,
            MinimumPanelArea = minArea
        };
    }

    /// <summary>Spanwise stations from <paramref name="yMin"/> to <paramref name="yMax"/>. Index 0 is <paramref name="yMin"/>.</summary>
    public static double[] SpanStations(double yMin, double yMax, int nPerHalf, string spacing)
    {
        int n = 2 * nPerHalf;
        var y = new double[n + 1];
        double mid = 0.5 * (yMin + yMax), half = 0.5 * (yMax - yMin);
        if (spacing == "cosine")
            for (int i = 0; i <= n; i++) y[i] = mid - half * Math.Cos(Math.PI * i / n);
        else if (spacing == "uniform")
            for (int i = 0; i <= n; i++) y[i] = yMin + (yMax - yMin) * i / n;
        else throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Span spacing is cosine or uniform.");
        return y;
    }

    private static double[] Fractions(int n, string spacing)
    {
        var f = new double[n + 1];
        if (spacing == "cosine")
            for (int k = 0; k <= n; k++) f[k] = 0.5 * (1 - Math.Cos(Math.PI * k / n));
        else if (spacing == "uniform")
            for (int k = 0; k <= n; k++) f[k] = (double)k / n;
        else throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Chord spacing is cosine or uniform.");
        return f;
    }

    private static double BoundXNearRoot(SectionSample[] samples, double[] fractions, double boundFrac)
    {
        SectionSample root = samples[0];
        double best = Math.Abs(SectionY(root));
        foreach (SectionSample sample in samples)
        {
            double d = Math.Abs(SectionY(sample));
            if (d < best) { best = d; root = sample; }
        }
        double f = fractions[0] + boundFrac * (fractions[1] - fractions[0]);
        return OnCamber(root, f).X;
    }

    /// <summary>
    /// The sweep of each strip given its span edges, by the lattice's own section lookup (<c>At</c>) and definition
    /// (<c>SweepOf</c>), so a verdict derived on read and the solve share one sweep. <paramref name="wing"/> is the
    /// mirrored section set the solve ran on; the span is that set's, never the kept strips'.
    /// </summary>
    internal static double[] StripSweeps(IReadOnlyList<SectionSample> wing, IReadOnlyList<(double Low, double High)> edges)
    {
        SectionSample[] samples = wing.ToArray();
        Array.Sort(samples, (a, b) => SectionY(a).CompareTo(SectionY(b)));
        double span = SectionY(samples[^1]) - SectionY(samples[0]);
        var sweeps = new double[edges.Count];
        for (int i = 0; i < sweeps.Length; i++)
            sweeps[i] = SweepOf(At(samples, edges[i].Low, span), At(samples, edges[i].High, span), edges[i].Low, edges[i].High);
        return sweeps;
    }

    private static double SweepOf(SectionSample a, SectionSample b, double ya, double yb)
    {
        double quarterA = a.Frame.LeadingMeters + 0.25 * a.Frame.ChordMeters;
        double quarterB = b.Frame.LeadingMeters + 0.25 * b.Frame.ChordMeters;
        return ToDegrees(Math.Atan2(quarterB - quarterA, yb - ya));
    }

    private static double FrontBoundSweepOf(List<Horseshoe> horses, int strip)
    {
        foreach (Horseshoe horse in horses)
            if (horse.Strip == strip)
                return ToDegrees(Math.Atan2(horse.B.X - horse.A.X, horse.B.Y - horse.A.Y));
        return 0;
    }

    private static Point3 OnPlane(SectionSample section, double fraction, double y) =>
        new(section.Frame.LeadingMeters + fraction * section.Frame.ChordMeters, y, section.Frame.ElevationMeters);

    private static double InducedFromTotal(List<Horseshoe> horses, double[] gamma, StripGeom strip, OperatingPoint op)
    {
        // The no-penetration residual is zero, so the control-point flow lies in the panel and the angle read from it
        // is the geometric incidence, not the trailing-wake α_i.
        Horseshoe horse = default;
        foreach (Horseshoe candidate in horses)
            if (candidate.Strip == strip.Index) { horse = candidate; break; }
        double ux = 0, uy = 0, uz = 0;
        for (int k = 0; k < horses.Count; k++)
        {
            Velocity(horses[k], horse.Cp, 0, false, out double sx, out double sy, out double sz);
            ux += gamma[k] * sx;
            uy += gamma[k] * sy;
            uz += gamma[k] * sz;
        }
        double alpha = ToRadians(op.AlphaDeg);
        double qx = op.Speed * Math.Cos(alpha) + ux;
        double qz = op.Speed * Math.Sin(alpha) + uz;
        return op.AlphaDeg - ToDegrees(Math.Atan2(qz, qx));
    }

    private static LatticeFailedException Fail(LatticePlant plant, string code, string reason)
    {
        if (plant == LatticePlant.NonFiniteAsZero && code == "ANA-NONFINITE")
            return new LatticeFailedException(code, reason);
        return new LatticeFailedException(code, reason);
    }

    /// <summary>
    /// One dense solve of A x = b: LU with partial pivoting, one forward and one back substitution, no refinement.
    /// A non-finite x fails closed (<c>ANA-NONFINITE</c>). A normwise backward error
    /// ‖b − A x‖∞ / (‖A‖∞ ‖x‖∞ + ‖b‖∞) above <see cref="Settings.SolveBackwardErrorTolerance"/> fails closed
    /// (<c>ANA-SOLVE-RESIDUAL</c>): the run records Failed with the reason, never a number.
    /// </summary>
    internal static DenseSolution SolveDense(double[] matrix, double[] rhs, int n, LatticePlant plant, CancellationToken cancellation)
    {
        var factors = (double[])matrix.Clone();
        var pivot = new int[n];
        Factor(factors, pivot, n, plant, cancellation);
        var x = (double[])rhs.Clone();
        Substitute(factors, pivot, x, n);
        for (int i = 0; i < n; i++)
        {
            if (double.IsFinite(x[i])) continue;
            if (plant == LatticePlant.NonFiniteAsZero) { x[i] = 0; continue; }
            throw new LatticeFailedException("ANA-NONFINITE", "A circulation is not finite.");
        }
        double residual = Residual(matrix, x, rhs, n);
        double scale = InfNorm(matrix, n) * MaxAbs(x) + MaxAbs(rhs);
        double backward = scale > 0 ? residual / scale : residual;
        if (!(backward <= Settings.SolveBackwardErrorTolerance))
            throw new LatticeFailedException("ANA-SOLVE-RESIDUAL",
                "The solve residual ‖AΓ − b‖∞ " + residual.ToString("G3", System.Globalization.CultureInfo.InvariantCulture)
                + " is a backward error of " + backward.ToString("G3", System.Globalization.CultureInfo.InvariantCulture)
                + ", above the " + Settings.SolveBackwardErrorTolerance.ToString("G3", System.Globalization.CultureInfo.InvariantCulture)
                + " tolerance.");
        int interchanges = 0;
        for (int k = 0; k < n; k++) if (pivot[k] != k) interchanges++;
        return new DenseSolution(x, residual, backward, Condition(factors, pivot, OneNorm(matrix, n), n), interchanges);
    }

    internal sealed record DenseSolution(double[] X, double ResidualInf, double BackwardError, double Kappa1, int Interchanges);

    private static void Factor(double[] a, int[] pivot, int n, LatticePlant plant, CancellationToken cancellation)
    {
        for (int k = 0; k < n; k++)
        {
            cancellation.ThrowIfCancellationRequested();
            int row = k;
            double max = Math.Abs(a[k * n + k]);
            for (int i = k + 1; i < n; i++)
            {
                double v = Math.Abs(a[i * n + k]);
                if (v > max) { max = v; row = i; }
            }
            pivot[k] = row;
            // LINPACK order: interchange columns k..n-1 only. The multipliers already stored left of k stay in their
            // rows, because the forward substitution applies each interchange at its own step.
            if (row != k) Swap(a, n, k, row, plant == LatticePlant.PivotWholeRow ? 0 : k);
            double diag = a[k * n + k];
            if (!double.IsFinite(diag))
            {
                if (plant == LatticePlant.NonFiniteAsZero) { a[k * n + k] = 1; continue; }
                throw new LatticeFailedException("ANA-NONFINITE", "The influence matrix is not finite.");
            }
            if (!(Math.Abs(diag) > 1e-14 * Math.Max(1, max)))
                throw new LatticeFailedException("ANA-SOLVE-SINGULAR", "The influence matrix is singular.");
            int length = n - k - 1;
            for (int i = k + 1; i < n; i++)
            {
                double f = a[i * n + k] / diag;
                a[i * n + k] = f;
                int dst = i * n + k + 1;
                int src = k * n + k + 1;
                for (int j = 0; j < length; j++) a[dst + j] -= f * a[src + j];
            }
        }
    }

    private static void Substitute(double[] a, int[] pivot, double[] x, int n)
    {
        for (int k = 0; k < n; k++)
        {
            int row = pivot[k];
            if (row != k) (x[k], x[row]) = (x[row], x[k]);
            double xk = x[k];
            for (int i = k + 1; i < n; i++) x[i] -= a[i * n + k] * xk;
        }
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = x[i];
            int row = i * n;
            for (int j = i + 1; j < n; j++) sum -= a[row + j] * x[j];
            x[i] = sum / a[row + i];
        }
    }

    private static double Residual(double[] original, double[] gamma, double[] rhs, int n)
    {
        double residual = 0;
        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            int row = i * n;
            for (int j = 0; j < n; j++) sum += original[row + j] * gamma[j];
            residual = Math.Max(residual, Math.Abs(sum - rhs[i]));
        }
        return residual;
    }

    // κ₁ ≈ ‖A‖₁ ‖A⁻¹ e‖₁ / ‖e‖₁ with e the ones vector. A lower estimate of the 1-norm condition.
    private static double Condition(double[] factors, int[] pivot, double norm, int n)
    {
        var e = new double[n];
        for (int i = 0; i < n; i++) e[i] = 1;
        Substitute(factors, pivot, e, n);
        double inv = 0;
        for (int i = 0; i < n; i++) inv += Math.Abs(e[i]);
        return norm * inv / n;
    }

    private static double OneNorm(double[] a, int n)
    {
        double norm = 0;
        for (int j = 0; j < n; j++)
        {
            double sum = 0;
            for (int i = 0; i < n; i++) sum += Math.Abs(a[i * n + j]);
            if (sum > norm) norm = sum;
        }
        return norm;
    }

    private static double InfNorm(double[] a, int n)
    {
        double norm = 0;
        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++) sum += Math.Abs(a[i * n + j]);
            if (sum > norm) norm = sum;
        }
        return norm;
    }

    private static double MaxAbs(double[] v)
    {
        double max = 0;
        foreach (double value in v) max = Math.Max(max, Math.Abs(value));
        return max;
    }

    private static void Swap(double[] a, int n, int i, int j, int from)
    {
        for (int k = from; k < n; k++) (a[i * n + k], a[j * n + k]) = (a[j * n + k], a[i * n + k]);
    }

    // Design §5.2: one evaluation per bound segment, at its midpoint, not a quadrature tuned to land inside 1 % at one
    // lattice. On the default chord law F-5 shows the gap to Trefftz shrinking 32 → 64 → 128 toward 0.89 % (the
    // nc-4 chordwise error; docs/notes/area3-fixture-arithmetic.md §Repair).
    private static void InducedAtBound(List<Horseshoe> horses, double[] gamma, in Horseshoe horse, double cutoff, bool boundOnly,
        ref double ux, ref double uy, ref double uz)
    {
        var point = new Point3(
            0.5 * (horse.A.X + horse.B.X),
            0.5 * (horse.A.Y + horse.B.Y),
            0.5 * (horse.A.Z + horse.B.Z));
        for (int k = 0; k < horses.Count; k++)
        {
            double vx, vy, vz;
            if (boundOnly) Segment(point, horses[k].A, horses[k].B, cutoff, true, out vx, out vy, out vz);
            else Velocity(horses[k], point, cutoff, true, out vx, out vy, out vz);
            ux += gamma[k] * vx;
            uy += gamma[k] * vy;
            uz += gamma[k] * vz;
        }
    }

    private static bool Finite(Point3 p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && double.IsFinite(p.Z);

    private static void Velocity(in Horseshoe horse, Point3 p, double cutoff, bool cut, out double vx, out double vy, out double vz)
    {
        Segment(p, horse.FarA, horse.A, cutoff, cut, out double a1, out double b1, out double c1);
        Segment(p, horse.A, horse.B, cutoff, cut, out double a2, out double b2, out double c2);
        Segment(p, horse.B, horse.FarB, cutoff, cut, out double a3, out double b3, out double c3);
        vx = a1 + a2 + a3;
        vy = b1 + b2 + b3;
        vz = c1 + c2 + c3;
    }

    // Biot–Savart of a unit-strength segment (Katz & Plotkin). The c² < 10⁻²⁰ guard matches the fixture script.
    // Near-field forces also drop a segment whose perpendicular distance is below cutoff × its length.
    private static void Segment(Point3 p, Point3 a, Point3 b, double cutoff, bool cut, out double vx, out double vy, out double vz)
    {
        double r1x = p.X - a.X, r1y = p.Y - a.Y, r1z = p.Z - a.Z;
        double r2x = p.X - b.X, r2y = p.Y - b.Y, r2z = p.Z - b.Z;
        double r0x = b.X - a.X, r0y = b.Y - a.Y, r0z = b.Z - a.Z;
        double cx = r1y * r2z - r1z * r2y;
        double cy = r1z * r2x - r1x * r2z;
        double cz = r1x * r2y - r1y * r2x;
        double c2 = cx * cx + cy * cy + cz * cz;
        double length2 = r0x * r0x + r0y * r0y + r0z * r0z;
        if (c2 < 1e-20 || (cut && length2 > 0 && c2 < cutoff * cutoff * length2 * length2))
        {
            vx = vy = vz = 0;
            return;
        }
        double n1 = Math.Sqrt(r1x * r1x + r1y * r1y + r1z * r1z);
        double n2 = Math.Sqrt(r2x * r2x + r2y * r2y + r2z * r2z);
        if (n1 == 0 || n2 == 0) { vx = vy = vz = 0; return; }
        double dot = r0x * (r1x / n1 - r2x / n2) + r0y * (r1y / n1 - r2y / n2) + r0z * (r1z / n1 - r2z / n2);
        double scale = dot / (4 * Math.PI * c2);
        vx = cx * scale;
        vy = cy * scale;
        vz = cz * scale;
    }

    private static SectionSample At(SectionSample[] samples, double y, double span)
    {
        int lo = 0, hi = samples.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (SectionY(samples[mid]) < y) lo = mid + 1;
            else hi = mid;
        }
        double tol = 1e-9 * span;
        if (Math.Abs(SectionY(samples[lo]) - y) <= tol) return samples[lo];
        if (lo > 0 && Math.Abs(SectionY(samples[lo - 1]) - y) <= tol) return samples[lo - 1];
        int right = lo;
        int left = Math.Max(0, lo - 1);
        if (right == left) return samples[left];
        if (SectionY(samples[right]) < y && right + 1 < samples.Length) { left = right; right++; }
        double y0 = SectionY(samples[left]), y1 = SectionY(samples[right]);
        double t = y1 == y0 ? 0 : (y - y0) / (y1 - y0);
        return Blend(samples[left], samples[right], t);
    }

    private static SectionSample Blend(SectionSample a, SectionSample b, double t)
    {
        int n = a.PlacedCamber.Count;
        var x = new double[n];
        var camber = new double[n];
        var thickness = new double[n];
        var slope = new double[n];
        var placed = new Point3[n];
        for (int i = 0; i < n; i++)
        {
            x[i] = a.X[i] + t * (b.X[i] - a.X[i]);
            camber[i] = a.Camber[i] + t * (b.Camber[i] - a.Camber[i]);
            thickness[i] = a.Thickness[i] + t * (b.Thickness[i] - a.Thickness[i]);
            slope[i] = a.CamberSlope[i] + t * (b.CamberSlope[i] - a.CamberSlope[i]);
            placed[i] = new Point3(
                a.PlacedCamber[i].X + t * (b.PlacedCamber[i].X - a.PlacedCamber[i].X),
                a.PlacedCamber[i].Y + t * (b.PlacedCamber[i].Y - a.PlacedCamber[i].Y),
                a.PlacedCamber[i].Z + t * (b.PlacedCamber[i].Z - a.PlacedCamber[i].Z));
        }
        StationFrame fa = a.Frame, fb = b.Frame;
        var frame = new StationFrame(
            fa.Eta + t * (fb.Eta - fa.Eta),
            fa.SpanMeters + t * (fb.SpanMeters - fa.SpanMeters),
            fa.LeadingMeters + t * (fb.LeadingMeters - fa.LeadingMeters),
            fa.TrailingMeters + t * (fb.TrailingMeters - fa.TrailingMeters),
            fa.ElevationMeters + t * (fb.ElevationMeters - fa.ElevationMeters),
            fa.TwistDegrees + t * (fb.TwistDegrees - fa.TwistDegrees),
            fa.ThicknessRatio + t * (fb.ThicknessRatio - fa.ThicknessRatio));
        return new SectionSample(frame, x, camber, thickness, slope, placed);
    }

    private static Point3 OnCamber(SectionSample section, double fraction)
    {
        IReadOnlyList<double> x = section.X;
        IReadOnlyList<Point3> p = section.PlacedCamber;
        if (fraction <= x[0]) return p[0];
        int last = x.Count - 1;
        if (fraction >= x[last]) return p[last];
        int i = 0;
        while (i + 1 < last && x[i + 1] < fraction) i++;
        double den = x[i + 1] - x[i];
        double t = den == 0 ? 0 : (fraction - x[i]) / den;
        Point3 a = p[i], b = p[i + 1];
        return new Point3(a.X + t * (b.X - a.X), a.Y + t * (b.Y - a.Y), a.Z + t * (b.Z - a.Z));
    }

    private static double CamberSlopeAt(SectionSample section, double fraction)
    {
        IReadOnlyList<double> x = section.X;
        IReadOnlyList<double> slope = section.CamberSlope;
        if (fraction <= x[0]) return slope[0];
        int last = x.Count - 1;
        if (fraction >= x[last]) return slope[last];
        int i = 0;
        while (i + 1 < last && x[i + 1] < fraction) i++;
        double t = (fraction - x[i]) / (x[i + 1] - x[i]);
        return slope[i] + t * (slope[i + 1] - slope[i]);
    }

    private static double ChordOf(SectionSample section)
    {
        Point3 a = OnCamber(section, 0), b = OnCamber(section, 1);
        double dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static double SectionY(SectionSample section) => section.PlacedCamber[0].Y;

    private readonly record struct StripGeom(int Index, double Y, double Eta, double Chord, double Dy, double Twist, double Sweep, double Ya, double Yb);

    private readonly record struct Vector3(double X, double Y, double Z)
    {
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
        public Vector3 Unit() { double l = Length; return new Vector3(X / l, Y / l, Z / l); }
    }

    private readonly struct Horseshoe
    {
        public Horseshoe(Point3 a, Point3 b, Point3 farA, Point3 farB, Point3 cp, Vector3 normal, int strip, double area)
        {
            A = a; B = b; FarA = farA; FarB = farB; Cp = cp; Normal = normal; Strip = strip; Area = area;
        }
        public Point3 A { get; }
        public Point3 B { get; }
        public Point3 FarA { get; }
        public Point3 FarB { get; }
        public Point3 Cp { get; }
        public Vector3 Normal { get; }
        public int Strip { get; }
        public double Area { get; }
    }

    private static Point3 Sub(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private static Vector3 Cross(Point3 u, Point3 v) => new(
        u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
}
