// Spike M1.2b2-P: does a binary64 evaluation of FoilDSL §6 placement land inside the certified
// Geometry.PointAt enclosure, and what does each path cost per point? Read + run, 2026-09-30.
using System.Diagnostics;
using System.Text;
using CfdWorkbench.Core;

string root = "/Users/mallalieut/projects/CFD-Workbench-design-m12b2-3d-elevations";
string example = File.ReadAllText(Path.Combine(root, "src/CfdWorkbench.Desktop/Assets/example.foil"));
string dihedral = example.Replace(
    "dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)]",
    "dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 5), (0.5, 15), (0.7, 30), (0.9, 50), (1, 60)]")
    .Replace("(0.9, -1.5), (1, -2)]", "(0.9, -6), (1, -8)]");
if (dihedral == example) throw new Exception("dihedral patch failed");
// Two distinct profiles: section-b scales section-a (upper x1.2, lower x0.8) on the same abscissae.
string blend = BuildBlend(dihedral);
var cases = new (string Name, byte[] Bytes)[]
{
    ("example (twist -2 deg, flat)", Encoding.UTF8.GetBytes(example)),
    ("example + dihedral 60 mm, twist -8 deg", Encoding.UTF8.GetBytes(dihedral)),
    ("two profiles blended + dihedral + twist", Encoding.UTF8.GetBytes(blend)),
    ("New foil (NewDefault)", FoilSource.NewDefault()),
};
foreach (var (name, bytes) in cases)
{
    var parse = FoilSource.Parse(bytes);
    if (!parse.IsParsed) { Console.WriteLine($"{name}: parse failed {string.Join(",", parse.Diagnostics.Select(d => d.Code))}"); continue; }
    var assess = Geometry.Assess(parse);
    if (assess.Certificate is null) { Console.WriteLine($"{name}: {assess.Status} {assess.Code} {assess.Reason}"); continue; }
    var cert = assess.Certificate;
    var def = parse.Definition!;
    var display = new Binary64Placement(def);
    int n = 0; double worstOutside = 0, worstMid = 0, certMs = 0, dispMs = 0;
    var etas = Enumerable.Range(0, 11).Select(i => i / 10.0).ToArray();
    var xs = Enumerable.Range(0, 21).Select(i => i / 20.0).ToArray();
    foreach (double eta in etas)
        foreach (double x in xs)
            foreach (bool upper in new[] { true, false })
                foreach (bool port in new[] { false, true })
                {
                    var t0 = Stopwatch.GetTimestamp();
                    var enc = Geometry.PointAt(cert, eta, x, upper, port);
                    certMs += Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
                    var t1 = Stopwatch.GetTimestamp();
                    var (X, Y, Z) = display.Place(eta, x, upper, port);
                    dispMs += Stopwatch.GetElapsedTime(t1).TotalMilliseconds;
                    worstOutside = Math.Max(worstOutside, Outside(enc.X, X));
                    worstOutside = Math.Max(worstOutside, Outside(enc.Y, Y));
                    worstOutside = Math.Max(worstOutside, Outside(enc.Z, Z));
                    worstMid = Math.Max(worstMid, Math.Abs(X - (enc.X.Lower + enc.X.Upper) / 2));
                    worstMid = Math.Max(worstMid, Math.Abs(Z - (enc.Z.Lower + enc.Z.Upper) / 2));
                    n++;
                }
    // A display-sized grid: 41 stations x 101 chord samples x 2 sides x 2 halves.
    var t2 = Stopwatch.GetTimestamp();
    var grid = new Binary64Placement(def);
    int m = 0;
    for (int i = 0; i <= 40; i++) for (int j = 0; j <= 100; j++) foreach (bool u in new[] { true, false }) foreach (bool p in new[] { false, true }) { grid.Place(i / 40.0, j / 100.0, u, p); m++; }
    double gridMs = Stopwatch.GetElapsedTime(t2).TotalMilliseconds;
    // Separable grid: timing (warm) and agreement with the certificate at the 11 x 21 probe set.
    var etasG = Enumerable.Range(0, 41).Select(i => i / 40.0).ToArray(); var xsG = Enumerable.Range(0, 101).Select(i => i / 100.0).ToArray();
    Separable.Grid(def, etasG, xsG);
    var t3 = Stopwatch.GetTimestamp(); Separable.Grid(def, etasG, xsG); double sepMs = Stopwatch.GetElapsedTime(t3).TotalMilliseconds;
    var probe = Separable.Grid(def, etas, xs); double sepWorst = 0;
    for (int a = 0; a < etas.Length; a++) for (int b2 = 0; b2 < xs.Length; b2++) for (int s2 = 0; s2 < 2; s2++)
    { var e = Geometry.PointAt(cert, etas[a], xs[b2], s2 == 0); var q = probe[a, b2, s2]; sepWorst = Math.Max(sepWorst, Math.Max(Outside(e.X, q.X), Math.Max(Outside(e.Y, q.Y), Outside(e.Z, q.Z)))); }
    Console.WriteLine($"  separable: 41x101x2 = {41 * 101 * 2} pts (one half; mirror is a sign) in {sepMs:F1} ms warm; max outside certified enclosure {sepWorst:E2} m");
    Console.WriteLine($"{name}: {n} points; certified {certMs / n:F3} ms/pt; binary64 {dispMs / n * 1000:F1} us/pt; " +
        $"max outside enclosure {worstOutside:E2} m; max |display - enclosure midpoint| {worstMid:E2} m; display grid {m} pts in {gridMs:F1} ms (cold, incl. JIT)");
}

static double Outside(EnclosedOrdinate e, double v) => v < e.Lower ? e.Lower - v : v > e.Upper ? v - e.Upper : 0;

static string BuildBlend(string text)
{
    int start = text.IndexOf("    profile \"section-a\"", StringComparison.Ordinal);
    int end = text.IndexOf("    }\n", start, StringComparison.Ordinal) + "    }\n".Length;
    string block = text[start..end];
    string b = block.Replace("section-a", "section-b");
    b = ScaleOrdinates(b, "upper cv", 1.2);
    b = ScaleOrdinates(b, "lower cv", 0.8);
    string result = text.Insert(end, b).Replace("at tip profile \"section-a\"", "at tip profile \"section-b\"");
    return result;
}

static string ScaleOrdinates(string block, string side, double factor)
{
    int s = block.IndexOf(side, StringComparison.Ordinal);
    int p = block.IndexOf("points [", s, StringComparison.Ordinal) + "points [".Length;
    int q = block.IndexOf("]", p, StringComparison.Ordinal);
    var pairs = block[p..q].Split("), (").Select(t => t.Trim('(', ')', ' ')).Select(t => t.Split(", ")).ToArray();
    var scaled = pairs.Select(pr => $"({pr[0]}, {(double.Parse(pr[1], System.Globalization.CultureInfo.InvariantCulture) * factor).ToString("R", System.Globalization.CultureInfo.InvariantCulture)})");
    return block[..p] + string.Join(", ", scaled) + block[q..];
}

/// <summary>FoilDSL §6 evaluated in binary64 (display path candidate). Not the certificate.</summary>
internal sealed class Binary64Placement(Definition def)
{
    private const double RadiansPerDegree = 0.017453292519943295;
    private readonly Dictionary<(int, int, double), double> maxCache = new();

    public (double X, double Y, double Z) Place(double eta, double x, bool upper, bool port)
    {
        double le = Channel("leading", eta), te = Channel("trailing", eta);
        double elev = Channel("dihedral", eta), twistDeg = Channel("twist", eta), t = Channel("thickness", eta);
        double z = Section(eta, x, t, upper);
        double phi = twistDeg * RadiansPerDegree;            // one binary64 rounding, as §6 pins
        var (sin, cos) = Math.SinCos(phi);
        double c = te - le;
        double X = le + c * (x * cos + z * sin);
        double Z = elev + c * (z * cos - x * sin);
        double Y = def.HalfSpan * eta * (port ? -1 : 1);
        return (X, Y, Z);
    }

    private double Section(double eta, double x, double thickness, bool upper)
    {
        var st = def.Assignments;
        int i = 0;
        while (i + 1 < st.Length && st[i + 1].Eta < eta) i++;
        var (ea, pa) = st[i]; var (eb, pb) = st[Math.Min(i + 1, st.Length - 1)];
        double w = pa == pb || eb == ea ? 0 : (eta - ea) / (eb - ea);
        var (ca, ta) = Components(pa, x);
        var (cb, tb) = Components(pb, x);
        double camber = (1 - w) * ca + w * cb;
        double t0 = (1 - w) * ta + w * tb;
        double max = pa == pb ? 1 : MaxT0(pa, pb, w);
        double half = t0 / max * thickness / 2;
        return upper ? camber + half : camber - half;
    }

    private (double Camber, double Unit) Components(int profile, double x)
    {
        var p = def.Profiles[profile];
        double u = Inverse(p.Upper, x), l = Inverse(p.Lower, x);
        return ((u + l) / 2, (u - l) / ProfileMax(profile));
    }

    private readonly Dictionary<int, double> profileMax = new();
    private double ProfileMax(int profile)
    {
        if (profileMax.TryGetValue(profile, out var m)) return m;
        var p = def.Profiles[profile];
        m = Maximize(x => Inverse(p.Upper, x) - Inverse(p.Lower, x));
        profileMax[profile] = m;
        return m;
    }

    private double MaxT0(int a, int b, double w)
    {
        if (maxCache.TryGetValue((a, b, w), out var m)) return m;
        m = Maximize(x => (1 - w) * Components(a, x).Unit + w * Components(b, x).Unit);
        maxCache[(a, b, w)] = m;
        return m;
    }

    private static double Maximize(Func<double, double> f)
    {
        int best = 0; double bestValue = double.NegativeInfinity;
        for (int k = 0; k <= 400; k++) { double v = f(k / 400.0); if (v > bestValue) { bestValue = v; best = k; } }
        double lo = Math.Max(0, (best - 1) / 400.0), hi = Math.Min(1, (best + 1) / 400.0);
        const double g = 0.6180339887498949;
        for (int it = 0; it < 80; it++)
        {
            double m1 = hi - g * (hi - lo), m2 = lo + g * (hi - lo);
            if (f(m1) < f(m2)) lo = m1; else hi = m2;
        }
        return Math.Max(bestValue, f((lo + hi) / 2));
    }

    private double Channel(string name, double eta) => Inverse(def.Curves[name], eta);

    private static double Inverse(Curve curve, double value)
    {
        double lo = 0, hi = 1;
        if (value <= curve.Points[0][0]) return curve.Points[0][1];
        if (value >= curve.Points[^1][0]) return curve.Points[^1][1];
        for (int step = 0; step < 80; step++)
        {
            double mid = (lo + hi) / 2;
            if (Dot(curve, mid, 0) < value) lo = mid; else hi = mid;
        }
        return Dot(curve, (lo + hi) / 2, 1);
    }

    private static double Dot(Curve curve, double t, int coordinate)
    {
        var basis = SplineBasis.Values(curve.Knots, curve.Degree, t);
        double v = 0;
        for (int i = 0; i < basis.Length; i++) v += basis[i] * curve.Points[i][coordinate];
        return v;
    }
}
