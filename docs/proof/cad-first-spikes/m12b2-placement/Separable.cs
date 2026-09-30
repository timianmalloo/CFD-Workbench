// Separable display evaluation: channels once per station, profile sides once per chord sample,
// then FoilDSL §6 composed per point. Same rule, same binary64 evaluator, different loop order.
using CfdWorkbench.Core;
internal static class Separable
{
    private const double RadiansPerDegree = 0.017453292519943295;
    public static (double X, double Y, double Z)[,,] Grid(Definition def, double[] etas, double[] xs)
    {
        int P = def.Profiles.Length;
        var u = new double[P, xs.Length]; var l = new double[P, xs.Length]; var max = new double[P];
        const int dense = 401;
        var du = new double[P, dense]; var dl = new double[P, dense];
        for (int p = 0; p < P; p++)
        {
            for (int k = 0; k < dense; k++) { du[p, k] = Inv(def.Profiles[p].Upper, k / 400.0); dl[p, k] = Inv(def.Profiles[p].Lower, k / 400.0); }
            max[p] = Peak(k => du[p, k] - dl[p, k], dense);
            for (int j = 0; j < xs.Length; j++) { u[p, j] = Inv(def.Profiles[p].Upper, xs[j]); l[p, j] = Inv(def.Profiles[p].Lower, xs[j]); }
        }
        var result = new (double, double, double)[etas.Length, xs.Length, 2];
        for (int i = 0; i < etas.Length; i++)
        {
            double eta = etas[i];
            double le = Inv(def.Curves["leading"], eta), te = Inv(def.Curves["trailing"], eta), el = Inv(def.Curves["dihedral"], eta);
            double tw = Inv(def.Curves["twist"], eta), th = Inv(def.Curves["thickness"], eta);
            var st = def.Assignments; int s = 0; while (s + 1 < st.Length && st[s + 1].Eta < eta) s++;
            var (ea, pa) = st[s]; var (eb, pb) = st[Math.Min(s + 1, st.Length - 1)];
            double w = pa == pb || eb == ea ? 0 : (eta - ea) / (eb - ea);
            double m0 = pa == pb ? 1 : Peak(k => (1 - w) * (du[pa, k] - dl[pa, k]) / max[pa] + w * (du[pb, k] - dl[pb, k]) / max[pb], dense);
            var (sin, cos) = Math.SinCos(tw * RadiansPerDegree);
            double c = te - le;
            for (int j = 0; j < xs.Length; j++)
            {
                double x = xs[j];
                double camber = (1 - w) * (u[pa, j] + l[pa, j]) / 2 + w * (u[pb, j] + l[pb, j]) / 2;
                double t0 = (1 - w) * (u[pa, j] - l[pa, j]) / max[pa] + w * (u[pb, j] - l[pb, j]) / max[pb];
                double half = t0 / m0 * th / 2;
                for (int side = 0; side < 2; side++)
                {
                    double z = side == 0 ? camber + half : camber - half;
                    result[i, j, side] = (le + c * (x * cos + z * sin), def.HalfSpan * eta, el + c * (z * cos - x * sin));
                }
            }
        }
        return result;
    }
    // Grid maximum with a three-point parabolic refinement (display evaluator; the certificate uses Bernstein.Maximum).
    private static double Peak(Func<int, double> f, int n)
    {
        int b = 0; double bv = double.NegativeInfinity;
        for (int k = 0; k < n; k++) { double v = f(k); if (v > bv) { bv = v; b = k; } }
        if (b == 0 || b == n - 1) return bv;
        double y0 = f(b - 1), y1 = bv, y2 = f(b + 1), d = y0 - 2 * y1 + y2;
        return d >= 0 ? bv : y1 - (y2 - y0) * (y2 - y0) / (8 * d);
    }
    private static double Inv(Curve curve, double value)
    {
        if (value <= curve.Points[0][0]) return curve.Points[0][1];
        if (value >= curve.Points[^1][0]) return curve.Points[^1][1];
        double lo = 0, hi = 1;
        for (int step = 0; step < 60; step++) { double mid = (lo + hi) / 2; if (Dot(curve, mid, 0) < value) lo = mid; else hi = mid; }
        return Dot(curve, (lo + hi) / 2, 1);
    }
    private static double Dot(Curve curve, double t, int c)
    { var b = SplineBasis.Values(curve.Knots, curve.Degree, t); double v = 0; for (int i = 0; i < b.Length; i++) v += b[i] * curve.Points[i][c]; return v; }
}
