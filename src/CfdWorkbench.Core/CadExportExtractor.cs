namespace CfdWorkbench.Core;

/// <summary>Millimetre 3D vector used by pure-C# NURBS skinning and STEP/3DM export.</summary>
public readonly record struct CadVec3(double X, double Y, double Z)
{
    public static CadVec3 operator +(CadVec3 a, CadVec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static CadVec3 operator -(CadVec3 a, CadVec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static CadVec3 operator *(double s, CadVec3 a) => new(s * a.X, s * a.Y, s * a.Z);
    public static CadVec3 operator *(CadVec3 a, double s) => new(s * a.X, s * a.Y, s * a.Z);
    public static double Dot(CadVec3 a, CadVec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static CadVec3 Cross(CadVec3 a, CadVec3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    public double LengthSquared() => X * X + Y * Y + Z * Z;
    public double Length() => Math.Sqrt(LengthSquared());
    public CadVec3 MirrorY() => new(X, -Y, Z);
    public static CadVec3 FromMillimetres(Point3 p) => new(p.X * 1000.0, p.Y * 1000.0, p.Z * 1000.0);
}

/// <summary>Clamped non-rational 3D B-spline curve in millimetres.</summary>
public sealed record CadNurbsCurve(int Degree, double[] Knots, CadVec3[] Poles)
{
    public CadVec3 Evaluate(double u)
    {
        int n = Poles.Length;
        int span = CadNurbsMath.FindSpan(n, Degree, u, Knots);
        Span<double> basis = stackalloc double[Degree + 1];
        CadNurbsMath.BasisFunctions(span, u, Degree, Knots, basis);
        double x = 0, y = 0, z = 0;
        for (int i = 0; i <= Degree; i++)
        {
            var p = Poles[span - Degree + i];
            double b = basis[i];
            x += b * p.X;
            y += b * p.Y;
            z += b * p.Z;
        }
        return new(x, y, z);
    }

    public CadNurbsCurve MirrorY() => new(Degree, (double[])Knots.Clone(), Poles.Select(p => p.MirrorY()).ToArray());
}

/// <summary>Clamped tensor-product non-rational 3D B-spline surface in millimetres.</summary>
public sealed record CadNurbsSurface(int DegreeU, int DegreeV, double[] KnotsU, double[] KnotsV, CadVec3[,] Poles)
{
    public int CountU => Poles.GetLength(0);
    public int CountV => Poles.GetLength(1);

    public CadVec3 Evaluate(double u, double v)
    {
        int nu = CountU;
        int nv = CountV;
        int spanU = CadNurbsMath.FindSpan(nu, DegreeU, u, KnotsU);
        int spanV = CadNurbsMath.FindSpan(nv, DegreeV, v, KnotsV);
        Span<double> bu = stackalloc double[DegreeU + 1];
        Span<double> bv = stackalloc double[DegreeV + 1];
        CadNurbsMath.BasisFunctions(spanU, u, DegreeU, KnotsU, bu);
        CadNurbsMath.BasisFunctions(spanV, v, DegreeV, KnotsV, bv);

        double x = 0, y = 0, z = 0;
        for (int i = 0; i <= DegreeU; i++)
        {
            double px = 0, py = 0, pz = 0;
            int ui = spanU - DegreeU + i;
            for (int j = 0; j <= DegreeV; j++)
            {
                var p = Poles[ui, spanV - DegreeV + j];
                double wv = bv[j];
                px += wv * p.X;
                py += wv * p.Y;
                pz += wv * p.Z;
            }
            double wu = bu[i];
            x += wu * px;
            y += wu * py;
            z += wu * pz;
        }
        return new(x, y, z);
    }

    public (CadVec3 S, CadVec3 Su, CadVec3 Sv, CadVec3 Suu, CadVec3 Suv, CadVec3 Svv) EvaluateDers2(double u, double v)
    {
        int nu = CountU;
        int nv = CountV;
        int spanU = CadNurbsMath.FindSpan(nu, DegreeU, u, KnotsU);
        int spanV = CadNurbsMath.FindSpan(nv, DegreeV, v, KnotsV);

        double[,] du = CadNurbsMath.DersBasisFunctions(spanU, u, DegreeU, 2, KnotsU);
        double[,] dv = CadNurbsMath.DersBasisFunctions(spanV, v, DegreeV, 2, KnotsV);

        CadVec3 EvalDeriv(int ku, int kv)
        {
            double x = 0, y = 0, z = 0;
            for (int i = 0; i <= DegreeU; i++)
            {
                double px = 0, py = 0, pz = 0;
                int ui = spanU - DegreeU + i;
                for (int j = 0; j <= DegreeV; j++)
                {
                    var p = Poles[ui, spanV - DegreeV + j];
                    double wv = dv[kv, j];
                    px += wv * p.X;
                    py += wv * p.Y;
                    pz += wv * p.Z;
                }
                double wu = du[ku, i];
                x += wu * px;
                y += wu * py;
                z += wu * pz;
            }
            return new(x, y, z);
        }

        return (
            EvalDeriv(0, 0),
            EvalDeriv(1, 0),
            EvalDeriv(0, 1),
            EvalDeriv(2, 0),
            EvalDeriv(1, 1),
            EvalDeriv(0, 2));
    }

    public CadNurbsCurve IsoU(int uIndex)
    {
        var pts = new CadVec3[CountV];
        for (int j = 0; j < CountV; j++) pts[j] = Poles[uIndex, j];
        return new(DegreeV, (double[])KnotsV.Clone(), pts);
    }

    public CadNurbsCurve IsoV(int vIndex)
    {
        var pts = new CadVec3[CountU];
        for (int i = 0; i < CountU; i++) pts[i] = Poles[i, vIndex];
        return new(DegreeU, (double[])KnotsU.Clone(), pts);
    }

    public CadNurbsSurface MirrorY()
    {
        var mirrored = new CadVec3[CountU, CountV];
        for (int i = 0; i < CountU; i++)
            for (int j = 0; j < CountV; j++)
                mirrored[i, j] = Poles[i, j].MirrorY();
        return new(DegreeU, DegreeV, (double[])KnotsU.Clone(), (double[])KnotsV.Clone(), mirrored);
    }

    /// <summary>Closest-point distance in mm from <paramref name="target"/> to this surface starting at <paramref name="u0"/>, <paramref name="v0"/>.</summary>
    public (double DistanceMm, double U, double V) ClosestPoint(CadVec3 target, double u0, double v0)
    {
        double u = Math.Clamp(u0, 0.0, 1.0);
        double v = Math.Clamp(v0, 0.0, 1.0);
        CadVec3 bestPt = Evaluate(u, v);
        double bestDistSq = (bestPt - target).LengthSquared();

        for (int iter = 0; iter < 14; iter++)
        {
            var (s, su, sv, suu, suv, svv) = EvaluateDers2(u, v);
            CadVec3 r = s - target;
            double distSq = r.LengthSquared();
            if (distSq < bestDistSq) bestDistSq = distSq;
            if (distSq < 1e-26) break;

            double f1 = CadVec3.Dot(r, su);
            double f2 = CadVec3.Dot(r, sv);
            if (Math.Abs(f1) < 1e-13 && Math.Abs(f2) < 1e-13) break;

            double j11 = CadVec3.Dot(su, su) + CadVec3.Dot(r, suu);
            double j12 = CadVec3.Dot(su, sv) + CadVec3.Dot(r, suv);
            double j22 = CadVec3.Dot(sv, sv) + CadVec3.Dot(r, svv);
            double det = j11 * j22 - j12 * j12;
            if (det <= 1e-24 || j11 <= 0)
            {
                j11 = CadVec3.Dot(su, su) + 1e-14;
                j12 = CadVec3.Dot(su, sv);
                j22 = CadVec3.Dot(sv, sv) + 1e-14;
                det = j11 * j22 - j12 * j12;
                if (Math.Abs(det) < 1e-28) break;
            }

            double du = -(j22 * f1 - j12 * f2) / det;
            double dv = -(-j12 * f1 + j11 * f2) / det;

            double step = 1.0;
            bool improved = false;
            for (int ls = 0; ls < 8; ls++)
            {
                double nu = Math.Clamp(u + step * du, 0.0, 1.0);
                double nv = Math.Clamp(v + step * dv, 0.0, 1.0);
                double candSq = (Evaluate(nu, nv) - target).LengthSquared();
                if (candSq < distSq)
                {
                    u = nu;
                    v = nv;
                    bestDistSq = candSq;
                    improved = true;
                    break;
                }
                step *= 0.5;
            }
            if (!improved || (Math.Abs(step * du) < 1e-12 && Math.Abs(step * dv) < 1e-12))
                break;
        }
        return (Math.Sqrt(bestDistSq), u, v);
    }
}

/// <summary>Pure-C# Piegl &amp; Tiller B-spline math and global tensor-product surface skinning.</summary>
public static class CadNurbsMath
{
    public static int FindSpan(int nPoles, int degree, double u, double[] knots)
    {
        if (u >= knots[nPoles]) return nPoles - 1;
        if (u <= knots[degree]) return degree;
        int low = degree;
        int high = nPoles;
        int mid = (low + high) / 2;
        while (u < knots[mid] || u >= knots[mid + 1])
        {
            if (u < knots[mid]) high = mid;
            else low = mid;
            mid = (low + high) / 2;
        }
        return mid;
    }

    public static void BasisFunctions(int span, double u, int degree, double[] knots, Span<double> basis)
    {
        Span<double> left = stackalloc double[degree + 1];
        Span<double> right = stackalloc double[degree + 1];
        basis[0] = 1.0;
        for (int j = 1; j <= degree; j++)
        {
            left[j] = u - knots[span + 1 - j];
            right[j] = knots[span + j] - u;
            double saved = 0.0;
            for (int r = 0; r < j; r++)
            {
                double temp = basis[r] / (right[r + 1] + left[j - r]);
                basis[r] = saved + right[r + 1] * temp;
                saved = left[j - r] * temp;
            }
            basis[j] = saved;
        }
    }

    public static double[,] DersBasisFunctions(int span, double u, int degree, int maxDeriv, double[] knots)
    {
        int n = Math.Min(maxDeriv, degree);
        var ders = new double[maxDeriv + 1, degree + 1];
        var ndu = new double[degree + 1, degree + 1];
        var left = new double[degree + 1];
        var right = new double[degree + 1];
        ndu[0, 0] = 1.0;

        for (int j = 1; j <= degree; j++)
        {
            left[j] = u - knots[span + 1 - j];
            right[j] = knots[span + j] - u;
            double saved = 0.0;
            for (int r = 0; r < j; r++)
            {
                ndu[j, r] = right[r + 1] + left[j - r];
                double temp = ndu[r, j - 1] / ndu[j, r];
                ndu[r, j] = saved + right[r + 1] * temp;
                saved = left[j - r] * temp;
            }
            ndu[j, j] = saved;
        }

        for (int j = 0; j <= degree; j++) ders[0, j] = ndu[j, degree];

        var a = new double[2, degree + 1];
        for (int r = 0; r <= degree; r++)
        {
            int s1 = 0, s2 = 1;
            a[0, 0] = 1.0;
            for (int k = 1; k <= n; k++)
            {
                double d = 0.0;
                int rk = r - k;
                int pk = degree - k;
                if (r >= k)
                {
                    a[s2, 0] = a[s1, 0] / ndu[pk + 1, rk];
                    d = a[s2, 0] * ndu[rk, pk];
                }
                int j1 = rk >= -1 ? 1 : -rk;
                int j2 = (r - 1 <= pk) ? k - 1 : degree - r;
                for (int j = j1; j <= j2; j++)
                {
                    a[s2, j] = (a[s1, j] - a[s1, j - 1]) / ndu[pk + 1, rk + j];
                    d += a[s2, j] * ndu[rk + j, pk];
                }
                if (r <= pk)
                {
                    a[s2, k] = -a[s1, k - 1] / ndu[pk + 1, r];
                    d += a[s2, k] * ndu[r, pk];
                }
                ders[k, r] = d;
                (s1, s2) = (s2, s1);
            }
        }

        int factor = degree;
        for (int k = 1; k <= n; k++)
        {
            for (int j = 0; j <= degree; j++) ders[k, j] *= factor;
            factor *= (degree - k);
        }
        return ders;
    }

    public static double[] AveragedClampedKnots(double[] parameters, int degree)
    {
        int n = parameters.Length;
        int m = n + degree + 1;
        var knots = new double[m];
        for (int i = 0; i <= degree; i++) knots[i] = 0.0;
        for (int i = n; i < m; i++) knots[i] = 1.0;
        for (int j = 1; j <= n - degree - 1; j++)
        {
            double sum = 0.0;
            for (int i = j; i < j + degree; i++) sum += parameters[i];
            knots[j + degree] = sum / degree;
        }
        return knots;
    }

    public static (double[] UniqueKnots, int[] Multiplicities) CompressKnots(double[] knots, double eps = 1e-13)
    {
        var uk = new List<double>();
        var mult = new List<int>();
        foreach (double k in knots)
        {
            if (uk.Count > 0 && Math.Abs(k - uk[^1]) <= eps)
                mult[^1]++;
            else
            {
                uk.Add(k);
                mult.Add(1);
            }
        }
        return (uk.ToArray(), mult.ToArray());
    }

    public static CadNurbsCurve InterpolateCurve(double[] parameters, CadVec3[] points, int degree = 3)
    {
        int n = parameters.Length;
        int p = Math.Min(degree, n - 1);
        double[] knots = AveragedClampedKnots(parameters, p);
        var lu = FactorCollocation(parameters, knots, p);
        var rx = new double[n];
        var ry = new double[n];
        var rz = new double[n];
        for (int i = 0; i < n; i++)
        {
            rx[i] = points[i].X;
            ry[i] = points[i].Y;
            rz[i] = points[i].Z;
        }
        double[] px = SolveLU(lu, rx);
        double[] py = SolveLU(lu, ry);
        double[] pz = SolveLU(lu, rz);
        var poles = new CadVec3[n];
        for (int i = 0; i < n; i++) poles[i] = new(px[i], py[i], pz[i]);
        return new CadNurbsCurve(p, knots, poles);
    }

    public static CadNurbsSurface SkinSections(double[] uParams, double[] vParams, CadVec3[,] sectionPoints, int degreeU = 3, int degreeV = 3)
    {
        int nu = uParams.Length;
        int nv = vParams.Length;
        int pu = Math.Min(degreeU, nu - 1);
        int pv = Math.Min(degreeV, nv - 1);

        double[] knotsU = AveragedClampedKnots(uParams, pu);
        double[] knotsV = AveragedClampedKnots(vParams, pv);

        var luU = FactorCollocation(uParams, knotsU, pu);
        var q = new CadVec3[nu, nv];
        var rx = new double[nu];
        var ry = new double[nu];
        var rz = new double[nu];
        for (int k = 0; k < nv; k++)
        {
            for (int i = 0; i < nu; i++)
            {
                rx[i] = sectionPoints[i, k].X;
                ry[i] = sectionPoints[i, k].Y;
                rz[i] = sectionPoints[i, k].Z;
            }
            double[] px = SolveLU(luU, rx);
            double[] py = SolveLU(luU, ry);
            double[] pz = SolveLU(luU, rz);
            for (int i = 0; i < nu; i++) q[i, k] = new(px[i], py[i], pz[i]);
        }

        var luV = FactorCollocation(vParams, knotsV, pv);
        var poles = new CadVec3[nu, nv];
        var sx = new double[nv];
        var sy = new double[nv];
        var sz = new double[nv];
        for (int i = 0; i < nu; i++)
        {
            for (int k = 0; k < nv; k++)
            {
                sx[k] = q[i, k].X;
                sy[k] = q[i, k].Y;
                sz[k] = q[i, k].Z;
            }
            double[] cx = SolveLU(luV, sx);
            double[] cy = SolveLU(luV, sy);
            double[] cz = SolveLU(luV, sz);
            for (int k = 0; k < nv; k++) poles[i, k] = new(cx[k], cy[k], cz[k]);
        }

        // Preserve exact planar Y at root (v = 0) and tip (v = 1) boundaries.
        for (int k = 0; k < nv; k++)
        {
            if (k == 0 || k == nv - 1)
            {
                double yExact = sectionPoints[0, k].Y;
                for (int i = 0; i < nu; i++)
                    poles[i, k] = new(poles[i, k].X, yExact, poles[i, k].Z);
            }
        }

        return new CadNurbsSurface(pu, pv, knotsU, knotsV, poles);
    }

    public static CadNurbsSurface RuledStrip(CadNurbsCurve edgeU0, CadNurbsCurve edgeU1)
    {
        if (edgeU0.Poles.Length != edgeU1.Poles.Length || edgeU0.Degree != edgeU1.Degree)
            throw new InvalidOperationException("Trailing-edge curves must be compatible.");
        int nv = edgeU0.Poles.Length;
        var poles = new CadVec3[2, nv];
        for (int j = 0; j < nv; j++)
        {
            poles[0, j] = edgeU0.Poles[j];
            poles[1, j] = edgeU1.Poles[j];
        }
        return new CadNurbsSurface(1, edgeU0.Degree, [0.0, 0.0, 1.0, 1.0], (double[])edgeU0.Knots.Clone(), poles);
    }

    private sealed record LuFactor(double[,] Lu, int[] Pivot, int N);

    private static LuFactor FactorCollocation(double[] parameters, double[] knots, int degree)
    {
        int n = parameters.Length;
        var a = new double[n, n];
        Span<double> basis = stackalloc double[degree + 1];
        for (int row = 0; row < n; row++)
        {
            double t = parameters[row];
            int span = FindSpan(n, degree, t, knots);
            BasisFunctions(span, t, degree, knots, basis);
            for (int c = 0; c <= degree; c++)
                a[row, span - degree + c] = basis[c];
        }

        var pivot = new int[n];
        for (int i = 0; i < n; i++) pivot[i] = i;

        for (int k = 0; k < n; k++)
        {
            int maxRow = k;
            double maxVal = Math.Abs(a[k, k]);
            for (int i = k + 1; i < n; i++)
            {
                double v = Math.Abs(a[i, k]);
                if (v > maxVal) { maxVal = v; maxRow = i; }
            }
            if (maxVal < 1e-15) throw new InvalidOperationException($"Singular B-spline collocation matrix at row {k}.");
            if (maxRow != k)
            {
                (pivot[k], pivot[maxRow]) = (pivot[maxRow], pivot[k]);
                for (int j = 0; j < n; j++) (a[k, j], a[maxRow, j]) = (a[maxRow, j], a[k, j]);
            }
            double diag = a[k, k];
            for (int i = k + 1; i < n; i++)
            {
                double f = a[i, k] / diag;
                if (f == 0.0) continue;
                a[i, k] = f;
                for (int j = k + 1; j < n; j++)
                    a[i, j] -= f * a[k, j];
            }
        }
        return new LuFactor(a, pivot, n);
    }

    private static double[] SolveLU(LuFactor f, double[] rhs)
    {
        int n = f.N;
        var x = new double[n];
        for (int i = 0; i < n; i++) x[i] = rhs[f.Pivot[i]];
        for (int i = 1; i < n; i++)
        {
            double sum = x[i];
            for (int j = 0; j < i; j++) sum -= f.Lu[i, j] * x[j];
            x[i] = sum;
        }
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = x[i];
            for (int j = i + 1; j < n; j++) sum -= f.Lu[i, j] * x[j];
            x[i] = sum / f.Lu[i, i];
        }
        return x;
    }
}

/// <summary>A-vs-B closest-point skin deviation measured directly against <see cref="Placement.Surface"/>.</summary>
public sealed record CadDeviationReport(
    int SampleCount,
    double MaxClosestPointMm,
    double MaxParametricMm,
    double TipPlanarityErrorMm,
    double RootPlanarityErrorMm,
    string WorstSide,
    double WorstEta,
    double WorstChordX);

/// <summary>Named 3D curve extracted for Rhino 3DM export layers.</summary>
public sealed record NamedCadCurve(string Name, CadNurbsCurve Curve);

/// <summary>
/// Skinned starboard wing CAD representation extracted strictly from <see cref="Placement"/> (ADR-0010 single authority),
/// including A-vs-B closest-point deviation and always-on trailing-edge thickness metrics (Ruling 195).
/// </summary>
public sealed record WingCadSkin(
    CadNurbsSurface UpperStarboard,
    CadNurbsSurface LowerStarboard,
    bool IsOpenTrailingEdge,
    double HalfSpanMm,
    double TrailingEdgeMinMm,
    double TrailingEdgeMaxMm,
    double TrailingEdgeMinYMm,
    bool TrailingEdgeAtTip,
    bool TrailingEdgeAlongSpan,
    int SectionCount,
    int ChordCount,
    int RefinementRounds,
    double[] StationEtas,
    double[] MappedChannelKnotEtas,
    double SizeXMm,
    double HalfSizeYMm,
    double SizeZMm,
    CadDeviationReport Deviation)
{
    public double SizeYMm(StlScope scope) => scope == StlScope.Half ? HalfSizeYMm : 2.0 * HalfSizeYMm;
}

/// <summary>
/// Pure-C# CAD geometry extractor over <see cref="Placement"/> (ADR-0010, ADR-0013 §Decision 1).
/// Explicitly implements the <c>KRN-SPK</c> Phase 1 production refinement: maps every channel interior knot
/// parameter <c>t_i</c> to its physical spanwise station <c>eta_i = x_channel(t_i)</c> via <see cref="SplineBasis.Values"/>
/// rather than seeding raw knot parameters.
/// </summary>
public static class CadExportExtractor
{
    public const double AcceptanceFloorMm = 0.010;  // 10 um (ADR-0013 / A4.12)
    public const double TargetSkinMm = 0.004;       // 4 um internal adaptive target (2.5x safety margin)
    public const int DefaultChordPoints = 65;       // cosine chord grid for cubic B-spline skinning
    public const int MaxRefinementRounds = 4;       // bounded termination variant R <= 4
    public const int MaxSections = 257;             // bounded termination variant N <= 257

    /// <summary>
    /// Maps every distinct interior knot parameter <c>t_i</c> across all 5 FoilDSL master channels
    /// (<c>leading</c>, <c>trailing</c>, <c>dihedral</c>, <c>twist</c>, <c>thickness</c>) to its physical
    /// spanwise coordinate <c>eta_i = x_channel(t_i)</c>.
    /// </summary>
    public static double[] ExtractChannelKnotEtas(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var parsed = FoilSource.Parse(source);
        Guard.Require(parsed.IsParsed && parsed.Definition is not null, "DSL-INVALID");
        var set = new SortedSet<double>();
        foreach (var curve in parsed.Definition!.Curves.Values)
        {
            var seenT = new HashSet<double>();
            foreach (double t in curve.Knots)
            {
                if (t <= 0.0 || t >= 1.0 || !seenT.Add(t)) continue;
                double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, t);
                double eta = 0.0;
                for (int i = 0; i < basis.Length; i++)
                    eta += basis[i] * curve.Points[i][0];
                if (double.IsFinite(eta) && eta > 1e-9 && eta < 1.0 - 1e-9)
                    set.Add(eta);
            }
        }
        return [.. set];
    }

    /// <summary>
    /// Builds an adaptively refined cubic (<c>3×3</c>) tensor-product NURBS wing skin over <see cref="Placement"/>
    /// and measures its A-vs-B closest-point deviation against <see cref="Placement.Surface"/>.
    /// </summary>
    public static WingCadSkin BuildSkin(byte[] source, CancellationToken cancellation = default, int verifyStations = 51, int verifyChordSamples = 201)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellation.ThrowIfCancellationRequested();
        var parsed = FoilSource.Parse(source);
        Guard.Require(parsed.IsParsed && parsed.Definition is not null, "DSL-INVALID");

        double[] mappedKnots = ExtractChannelKnotEtas(source);
        var etas = BuildInitialEtas(parsed, mappedKnots);
        var xs = Placement.ChordGrid(DefaultChordPoints).ToArray();
        var uParams = new double[DefaultChordPoints];
        for (int j = 0; j < DefaultChordPoints; j++)
            uParams[j] = j / (double)(DefaultChordPoints - 1);

        CadNurbsSurface upperSrf = null!;
        CadNurbsSurface lowerSrf = null!;
        double minTeMm = double.PositiveInfinity;
        double maxTeMm = 0.0;
        double minTeYMm = 0.0;
        double halfSpanMm = 0.0;
        int rounds = 0;
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
        double minZ = double.PositiveInfinity, maxZ = double.NegativeInfinity;

        for (int round = 0; round <= MaxRefinementRounds; round++)
        {
            cancellation.ThrowIfCancellationRequested();
            rounds = round;
            var (upperGrid, lowerGrid, teMin, teMax, teMinY, spanMm, bxMin, bxMax, bzMin, bzMax) =
                EvaluatePlacedSections(source, etas, xs, cancellation);
            minTeMm = teMin;
            maxTeMm = teMax;
            minTeYMm = teMinY;
            halfSpanMm = spanMm;
            minX = bxMin;
            maxX = bxMax;
            minZ = bzMin;
            maxZ = bzMax;

            upperSrf = CadNurbsMath.SkinSections(uParams, [.. etas], upperGrid, degreeU: 3, degreeV: 3);
            lowerSrf = CadNurbsMath.SkinSections(uParams, [.. etas], lowerGrid, degreeU: 3, degreeV: 3);

            if (round == MaxRefinementRounds || etas.Count >= MaxSections)
                break;

            var midEtas = new List<double>(etas.Count - 1);
            for (int k = 0; k < etas.Count - 1; k++)
                midEtas.Add(0.5 * (etas[k] + etas[k + 1]));

            var (midU, midL, _, _, _, _, _, _, _, _) = EvaluatePlacedSections(source, midEtas, xs, cancellation);
            var toInsert = new List<double>();
            for (int k = 0; k < midEtas.Count; k++)
            {
                double v = midEtas[k];
                double intervalMaxMm = 0.0;
                for (int j = 0; j < DefaultChordPoints; j += 2)
                {
                    double u = uParams[j];
                    double du = (upperSrf.Evaluate(u, v) - midU[j, k]).Length();
                    double dl = (lowerSrf.Evaluate(u, v) - midL[j, k]).Length();
                    intervalMaxMm = Math.Max(intervalMaxMm, Math.Max(du, dl));
                }
                if (intervalMaxMm > TargetSkinMm)
                    toInsert.Add(v);
            }

            if (toInsert.Count == 0)
                break;

            foreach (double v in toInsert)
            {
                if (etas.Count >= MaxSections) break;
                etas.Add(v);
            }
            etas.Sort();
        }

        bool isOpenTe = maxTeMm > 1e-6;
        bool alongSpan = maxTeMm - minTeMm <= 1e-6;
        bool atTip = Math.Abs(minTeYMm - halfSpanMm) <= 1e-6;

        var provisional = new WingCadSkin(
            upperSrf,
            lowerSrf,
            isOpenTe,
            halfSpanMm,
            minTeMm,
            maxTeMm,
            minTeYMm,
            atTip,
            alongSpan,
            etas.Count,
            DefaultChordPoints,
            rounds,
            [.. etas],
            mappedKnots,
            maxX - minX,
            halfSpanMm,
            maxZ - minZ,
            new CadDeviationReport(0, 0, 0, 0, 0, "upper", 0, 0));

        var deviation = MeasureDeviationAgainstPlacementSurface(source, provisional, verifyStations, verifyChordSamples, cancellation);
        return provisional with { Deviation = deviation };
    }

    /// <summary>Measures true A-vs-B closest-point skin deviation directly against <see cref="Placement.Surface"/>.</summary>
    public static CadDeviationReport MeasureDeviationAgainstPlacementSurface(
        byte[] source,
        WingCadSkin skin,
        int stations = 51,
        int chordSamples = 201,
        CancellationToken cancellation = default)
    {
        var surf = Placement.Surface(source, "accepted", 0, cancellation, stations, chordSamples);
        var xs = Placement.ChordGrid(chordSamples);

        double maxClosest = 0.0;
        double maxParam = 0.0;
        string worstSide = "upper";
        double worstEta = 0.0;
        double worstX = 0.0;
        int sampleCount = 0;

        for (int k = 0; k < surf.Sections.Count; k++)
        {
            var sec = surf.Sections[k];
            double v0 = sec.Eta;
            for (int j = 0; j < chordSamples; j++)
            {
                double u0 = j / (double)(chordSamples - 1);

                var puRef = CadVec3.FromMillimetres(sec.Upper[j]);
                double paramU = (skin.UpperStarboard.Evaluate(u0, v0) - puRef).Length();
                double closestU = paramU <= 1e-9 ? paramU : skin.UpperStarboard.ClosestPoint(puRef, u0, v0).DistanceMm;
                maxParam = Math.Max(maxParam, paramU);
                if (closestU > maxClosest)
                {
                    maxClosest = closestU;
                    worstSide = "upper";
                    worstEta = sec.Eta;
                    worstX = xs[j];
                }

                var plRef = CadVec3.FromMillimetres(sec.Lower[j]);
                double paramL = (skin.LowerStarboard.Evaluate(u0, v0) - plRef).Length();
                double closestL = paramL <= 1e-9 ? paramL : skin.LowerStarboard.ClosestPoint(plRef, u0, v0).DistanceMm;
                maxParam = Math.Max(maxParam, paramL);
                if (closestL > maxClosest)
                {
                    maxClosest = closestL;
                    worstSide = "lower";
                    worstEta = sec.Eta;
                    worstX = xs[j];
                }
                sampleCount += 2;
            }
        }

        double rootPlanarErr = 0.0;
        double tipPlanarErr = 0.0;
        for (int j = 0; j <= 100; j++)
        {
            double u = j / 100.0;
            rootPlanarErr = Math.Max(rootPlanarErr, Math.Abs(skin.UpperStarboard.Evaluate(u, 0.0).Y));
            rootPlanarErr = Math.Max(rootPlanarErr, Math.Abs(skin.LowerStarboard.Evaluate(u, 0.0).Y));
            tipPlanarErr = Math.Max(tipPlanarErr, Math.Abs(skin.UpperStarboard.Evaluate(u, 1.0).Y - skin.HalfSpanMm));
            tipPlanarErr = Math.Max(tipPlanarErr, Math.Abs(skin.LowerStarboard.Evaluate(u, 1.0).Y - skin.HalfSpanMm));
        }

        return new CadDeviationReport(sampleCount, maxClosest, maxParam, tipPlanarErr, rootPlanarErr, worstSide, worstEta, worstX);
    }

    /// <summary>
    /// Extracts the 5 3D master curves in millimetres for Rhino 3DM export (<c>Master Curves</c> layer):
    /// Leading Edge, Trailing Edge, Dihedral Quarter-Chord Spine, Twist Aft-Half Reference, and Max-Thickness Crest
    /// (mirrored to Port when <paramref name="scope"/> is <see cref="StlScope.Whole"/>).
    /// </summary>
    public static IReadOnlyList<NamedCadCurve> ExtractMasterCurvesMm(byte[] source, WingCadSkin skin, StlScope scope, CancellationToken cancellation = default)
    {
        var etas = skin.StationEtas;
        double[] xs = [0.0, 0.25, 0.30, 0.75, 1.0];
        var samples = Placement.Sections(source, etas, xs, cancellation);
        int nv = etas.Length;
        var le = new CadVec3[nv];
        var te = new CadVec3[nv];
        var spine = new CadVec3[nv];
        var twistRef = new CadVec3[nv];
        var crest = new CadVec3[nv];

        for (int k = 0; k < nv; k++)
        {
            var s = samples[k];
            double chord = s.Frame.ChordMeters;
            var (sin, cos) = double.SinCosPi(s.Frame.TwistDegrees / 180.0);
            le[k] = CadVec3.FromMillimetres(s.PlacedCamber[0]);
            te[k] = CadVec3.FromMillimetres(s.PlacedCamber[4]);
            spine[k] = CadVec3.FromMillimetres(s.PlacedCamber[1]);
            twistRef[k] = CadVec3.FromMillimetres(s.PlacedCamber[3]);
            var midCrest = s.PlacedCamber[2];
            double halfT = 0.5 * s.Thickness[2] * chord;
            crest[k] = new((midCrest.X + halfT * sin) * 1000.0, midCrest.Y * 1000.0, (midCrest.Z + halfT * cos) * 1000.0);
        }

        var stbd = new NamedCadCurve[]
        {
            new("Leading Edge (Starboard)", CadNurbsMath.InterpolateCurve(etas, le)),
            new("Trailing Edge (Starboard)", CadNurbsMath.InterpolateCurve(etas, te)),
            new("Dihedral Spine (Starboard)", CadNurbsMath.InterpolateCurve(etas, spine)),
            new("Twist Reference (Starboard)", CadNurbsMath.InterpolateCurve(etas, twistRef)),
            new("Thickness Crest (Starboard)", CadNurbsMath.InterpolateCurve(etas, crest)),
        };

        var result = new List<NamedCadCurve>(scope == StlScope.Whole ? stbd.Length * 2 : stbd.Length);
        result.AddRange(stbd);
        if (scope == StlScope.Whole)
        {
            foreach (var c in stbd)
                result.Add(new NamedCadCurve(c.Name.Replace("(Starboard)", "(Port)", StringComparison.Ordinal), c.Curve.MirrorY()));
        }
        return result;
    }

    /// <summary>
    /// Extracts placed upper and lower NURBS profiles in millimetres at every authored station
    /// for Rhino 3DM export (<c>Authored Stations</c> layer).
    /// </summary>
    public static IReadOnlyList<NamedCadCurve> ExtractAuthoredStationCurvesMm(byte[] source, StlScope scope, CancellationToken cancellation = default)
    {
        var parsed = FoilSource.Parse(source);
        Guard.Require(parsed.IsParsed, "DSL-INVALID");
        var assignments = parsed.Authored().Assignments;
        var etas = assignments.Select(a => a.Eta).ToArray();
        var xs = Placement.ChordGrid(DefaultChordPoints).ToArray();
        var uParams = new double[DefaultChordPoints];
        for (int j = 0; j < DefaultChordPoints; j++)
            uParams[j] = j / (double)(DefaultChordPoints - 1);

        var (upperGrid, lowerGrid, _, _, _, _, _, _, _, _) = EvaluatePlacedSections(source, etas, xs, cancellation);
        var curves = new List<NamedCadCurve>();
        for (int k = 0; k < etas.Length; k++)
        {
            string stationName = k == 0 ? "Root" : k == etas.Length - 1 ? "Tip" : $"Station {k + 1}";
            var uPts = new CadVec3[DefaultChordPoints];
            var lPts = new CadVec3[DefaultChordPoints];
            for (int j = 0; j < DefaultChordPoints; j++)
            {
                uPts[j] = upperGrid[j, k];
                lPts[j] = lowerGrid[j, k];
            }
            var uCurve = CadNurbsMath.InterpolateCurve(uParams, uPts);
            var lCurve = CadNurbsMath.InterpolateCurve(uParams, lPts);
            curves.Add(new NamedCadCurve($"{stationName} Upper (Starboard)", uCurve));
            curves.Add(new NamedCadCurve($"{stationName} Lower (Starboard)", lCurve));
            if (scope == StlScope.Whole && etas[k] > 1e-9)
            {
                curves.Add(new NamedCadCurve($"{stationName} Upper (Port)", uCurve.MirrorY()));
                curves.Add(new NamedCadCurve($"{stationName} Lower (Port)", lCurve.MirrorY()));
            }
        }
        return curves;
    }

    private static List<double> BuildInitialEtas(SourceParse parsed, double[] mappedChannelKnots)
    {
        var set = new SortedSet<double> { 0.0, 1.0 };
        foreach (var assignment in parsed.Authored().Assignments)
            set.Add(assignment.Eta);
        foreach (double eta in mappedChannelKnots)
            set.Add(eta);
        for (int i = 0; i <= 16; i++)
            set.Add(i / 16.0);

        var result = new List<double>();
        foreach (double v in set)
        {
            if (result.Count == 0 || v - result[^1] > 1e-6)
                result.Add(v);
        }
        if (result[^1] < 1.0) result[^1] = 1.0;
        return result;
    }

    private static (CadVec3[,] Upper, CadVec3[,] Lower, double MinTeMm, double MaxTeMm, double MinTeYMm, double HalfSpanMm,
        double MinX, double MaxX, double MinZ, double MaxZ)
        EvaluatePlacedSections(byte[] source, IReadOnlyList<double> etas, double[] xs, CancellationToken cancellation)
    {
        var samples = Placement.Sections(source, etas, xs, cancellation);
        int nu = xs.Length;
        int nv = etas.Count;
        var upper = new CadVec3[nu, nv];
        var lower = new CadVec3[nu, nv];
        double minTeMm = double.PositiveInfinity;
        double maxTeMm = 0.0;
        double minTeYMm = 0.0;
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
        double minZ = double.PositiveInfinity, maxZ = double.NegativeInfinity;

        for (int k = 0; k < nv; k++)
        {
            var s = samples[k];
            double chord = s.Frame.ChordMeters;
            var (sin, cos) = double.SinCosPi(s.Frame.TwistDegrees / 180.0);
            for (int j = 0; j < nu; j++)
            {
                var mid = s.PlacedCamber[j];
                double halfThick = 0.5 * s.Thickness[j] * chord;
                double dx = halfThick * sin;
                double dz = halfThick * cos;
                var up = new CadVec3((mid.X + dx) * 1000.0, mid.Y * 1000.0, (mid.Z + dz) * 1000.0);
                var lo = new CadVec3((mid.X - dx) * 1000.0, mid.Y * 1000.0, (mid.Z - dz) * 1000.0);
                upper[j, k] = up;
                lower[j, k] = lo;
                minX = Math.Min(minX, Math.Min(up.X, lo.X));
                maxX = Math.Max(maxX, Math.Max(up.X, lo.X));
                minZ = Math.Min(minZ, Math.Min(up.Z, lo.Z));
                maxZ = Math.Max(maxZ, Math.Max(up.Z, lo.Z));
            }
            double teMm = (upper[nu - 1, k] - lower[nu - 1, k]).Length();
            if (teMm < minTeMm)
            {
                minTeMm = teMm;
                minTeYMm = upper[nu - 1, k].Y;
            }
            maxTeMm = Math.Max(maxTeMm, teMm);
        }

        double halfSpanMm = samples[^1].Frame.SpanMeters * 1000.0;
        return (upper, lower, minTeMm, maxTeMm, minTeYMm, halfSpanMm, minX, maxX, minZ, maxZ);
    }
}
