using CfdWorkbench.Core;

namespace CadExportSpike;

internal readonly record struct Vec3(double X, double Y, double Z)
{
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(double s, Vec3 a) => new(s * a.X, s * a.Y, s * a.Z);
    public static Vec3 operator *(Vec3 a, double s) => new(s * a.X, s * a.Y, s * a.Z);
    public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    public double LengthSquared() => X * X + Y * Y + Z * Z;
    public double Length() => Math.Sqrt(LengthSquared());
    public Vec3 MirrorY() => new(X, -Y, Z);
    public static Vec3 FromMillimetres(Point3 p) => new(p.X * 1000.0, p.Y * 1000.0, p.Z * 1000.0);
}

internal sealed record NurbsCurve3(int Degree, double[] Knots, Vec3[] Poles)
{
    public Vec3 Evaluate(double u)
    {
        int n = Poles.Length;
        int span = NurbsMath.FindSpan(n, Degree, u, Knots);
        Span<double> basis = stackalloc double[Degree + 1];
        NurbsMath.BasisFunctions(span, u, Degree, Knots, basis);
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

    public NurbsCurve3 MirrorY() => new(Degree, (double[])Knots.Clone(), Poles.Select(p => p.MirrorY()).ToArray());
}

internal sealed record NurbsSurface3(int DegreeU, int DegreeV, double[] KnotsU, double[] KnotsV, Vec3[,] Poles)
{
    public int CountU => Poles.GetLength(0);
    public int CountV => Poles.GetLength(1);

    public Vec3 Evaluate(double u, double v)
    {
        int nu = CountU;
        int nv = CountV;
        int spanU = NurbsMath.FindSpan(nu, DegreeU, u, KnotsU);
        int spanV = NurbsMath.FindSpan(nv, DegreeV, v, KnotsV);
        Span<double> bu = stackalloc double[DegreeU + 1];
        Span<double> bv = stackalloc double[DegreeV + 1];
        NurbsMath.BasisFunctions(spanU, u, DegreeU, KnotsU, bu);
        NurbsMath.BasisFunctions(spanV, v, DegreeV, KnotsV, bv);

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

    public (Vec3 S, Vec3 Su, Vec3 Sv, Vec3 Suu, Vec3 Suv, Vec3 Svv) EvaluateDers2(double u, double v)
    {
        int nu = CountU;
        int nv = CountV;
        int spanU = NurbsMath.FindSpan(nu, DegreeU, u, KnotsU);
        int spanV = NurbsMath.FindSpan(nv, DegreeV, v, KnotsV);

        double[,] du = NurbsMath.DersBasisFunctions(spanU, u, DegreeU, 2, KnotsU);
        double[,] dv = NurbsMath.DersBasisFunctions(spanV, v, DegreeV, 2, KnotsV);

        Vec3 EvalDeriv(int ku, int kv)
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

    public NurbsCurve3 IsoU(int uIndex)
    {
        var pts = new Vec3[CountV];
        for (int j = 0; j < CountV; j++) pts[j] = Poles[uIndex, j];
        return new(DegreeV, (double[])KnotsV.Clone(), pts);
    }

    public NurbsCurve3 IsoV(int vIndex)
    {
        var pts = new Vec3[CountU];
        for (int i = 0; i < CountU; i++) pts[i] = Poles[i, vIndex];
        return new(DegreeU, (double[])KnotsU.Clone(), pts);
    }

    public NurbsSurface3 MirrorY()
    {
        var mirrored = new Vec3[CountU, CountV];
        for (int i = 0; i < CountU; i++)
            for (int j = 0; j < CountV; j++)
                mirrored[i, j] = Poles[i, j].MirrorY();
        return new(DegreeU, DegreeV, (double[])KnotsU.Clone(), (double[])KnotsV.Clone(), mirrored);
    }

    /// <summary>Closest-point distance from <paramref name="target"/> to this surface starting at initial guess <paramref name="u0"/>, <paramref name="v0"/>.</summary>
    public (double DistanceMm, double U, double V) ClosestPoint(Vec3 target, double u0, double v0)
    {
        double u = Math.Clamp(u0, 0.0, 1.0);
        double v = Math.Clamp(v0, 0.0, 1.0);
        Vec3 bestPt = Evaluate(u, v);
        double bestDistSq = (bestPt - target).LengthSquared();

        // Safeguarded 2D Gauss-Newton / Newton projection with damping.
        for (int iter = 0; iter < 14; iter++)
        {
            var (s, su, sv, suu, suv, svv) = EvaluateDers2(u, v);
            Vec3 r = s - target;
            double distSq = r.LengthSquared();
            if (distSq < bestDistSq) bestDistSq = distSq;
            if (distSq < 1e-26) break;

            double f1 = Vec3.Dot(r, su);
            double f2 = Vec3.Dot(r, sv);
            if (Math.Abs(f1) < 1e-13 && Math.Abs(f2) < 1e-13) break;

            // Exact Hessian (with Gauss-Newton fallback if not SPD).
            double j11 = Vec3.Dot(su, su) + Vec3.Dot(r, suu);
            double j12 = Vec3.Dot(su, sv) + Vec3.Dot(r, suv);
            double j22 = Vec3.Dot(sv, sv) + Vec3.Dot(r, svv);
            double det = j11 * j22 - j12 * j12;
            if (det <= 1e-24 || j11 <= 0)
            {
                j11 = Vec3.Dot(su, su) + 1e-14;
                j12 = Vec3.Dot(su, sv);
                j22 = Vec3.Dot(sv, sv) + 1e-14;
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

internal static class NurbsMath
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

    // Piegl & Tiller Algorithm A2.3
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

    /// <summary>Piegl &amp; Tiller Eq. 9.8 clamped knot vector by parameter averaging.</summary>
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

    /// <summary>
    /// Global tensor-product B-spline surface interpolation over N sections (at parameters <paramref name="vParams"/>),
    /// each with M points at shared parameters <paramref name="uParams"/>.
    /// </summary>
    public static NurbsSurface3 SkinSections(double[] uParams, double[] vParams, Vec3[,] sectionPoints, int degreeU = 3, int degreeV = 3)
    {
        int nu = uParams.Length;
        int nv = vParams.Length;
        int pu = Math.Min(degreeU, nu - 1);
        int pv = Math.Min(degreeV, nv - 1);

        double[] knotsU = AveragedClampedKnots(uParams, pu);
        double[] knotsV = AveragedClampedKnots(vParams, pv);

        // Step 1: Solve chordwise (u) collocation matrix once for all nv sections.
        var luU = FactorCollocation(uParams, knotsU, pu);
        var q = new Vec3[nu, nv];
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

        // Step 2: Solve spanwise (v) collocation matrix once for all nu chordwise poles.
        var luV = FactorCollocation(vParams, knotsV, pv);
        var poles = new Vec3[nu, nv];
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

        // Enforce exact boundary coordinate preservation at root/tip Y and LE/TE endpoints.
        for (int k = 0; k < nv; k++)
        {
            // Y(u, v_k) is constant across every section k (all points on section k share Y = h * eta_k).
            double yExact = sectionPoints[0, k].Y;
            if (k == 0 || k == nv - 1)
            {
                for (int i = 0; i < nu; i++)
                    poles[i, k] = new(poles[i, k].X, yExact, poles[i, k].Z);
            }
        }

        return new NurbsSurface3(pu, pv, knotsU, knotsV, poles);
    }

    /// <summary>Builds a ruled bilinear-in-u, degree-pv-in-v trailing-edge strip between two matching trailing-edge curves.</summary>
    public static NurbsSurface3 RuledStrip(NurbsCurve3 edgeU0, NurbsCurve3 edgeU1)
    {
        if (edgeU0.Poles.Length != edgeU1.Poles.Length || edgeU0.Degree != edgeU1.Degree)
            throw new InvalidOperationException("Trailing-edge curves must be compatible.");
        int nv = edgeU0.Poles.Length;
        var poles = new Vec3[2, nv];
        for (int j = 0; j < nv; j++)
        {
            poles[0, j] = edgeU0.Poles[j];
            poles[1, j] = edgeU1.Poles[j];
        }
        return new NurbsSurface3(1, edgeU0.Degree, [0.0, 0.0, 1.0, 1.0], (double[])edgeU0.Knots.Clone(), poles);
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
