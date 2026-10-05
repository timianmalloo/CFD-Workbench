using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

// Tip-strip study probe (Ruling 75). Args: wing:N:alpha:tipTwist ... ; prints one CSV block per case.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
const double V = 10, Rho = 1, Half = 1;
var cases = args.Select(a => a.Split(':')).ToArray();
var results = new string[cases.Length];
Parallel.For(0, cases.Length, new ParallelOptions { MaxDegreeOfParallelism = 3 }, i =>
{
    string wing = cases[i][0];
    int n = int.Parse(cases[i][1]);
    double alpha = double.Parse(cases[i][2]);
    double twist = double.Parse(cases[i][3]);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    double[] nodes = Nodes(-Half, Half, n, cases[i].Length > 5 ? cases[i][5] : "cosine");
    bool cambered = wing.EndsWith("c"); string plan = cambered ? wing[..^1] : wing;
    var sections = nodes.Select(y => Section(y, Chord(plan, y / Half), twist * Math.Abs(y) / Half, cambered)).ToList();
    string spanSp = cases[i].Length > 5 ? cases[i][5] : "cosine";
    var settings = Settings.Default with { NSpanPerHalf = n, NChord = cases[i].Length > 4 ? int.Parse(cases[i][4]) : 4, SpanSpacing = spanSp };
    LatticeSolution sol;
    try { sol = VortexLattice.Solve(sections, settings, new OperatingPoint(V, 101325, null, "root LE", alpha, null), Rho, default); }
    catch (LatticeFailedException ex) { results[i] = "FAIL," + string.Join(":", cases[i]) + "," + ex.Code + "\n"; return; }
    double area = 0; foreach (var s in sol.Strips) area += s.Chord * s.Dy;
    double lift = 0; foreach (var s in sol.Strips) lift += s.Gamma * s.Dy;
    double cl = Rho * V * lift / (0.5 * Rho * V * V * area);
    double ar = 4 * Half * Half / area;
    var verdicts = MethodRecord.Verdicts(sol, alpha, 0);
    var sb = new System.Text.StringBuilder();
    int m = sol.Strips.Count;
    for (int k = 1; k <= 4; k++)
    {
        var s = sol.Strips[m - k];
        double selfDeg = (180 / Math.PI) * (s.Gamma / (Math.PI * V * s.Dy));
        sb.AppendLine(string.Join(",", wing, n, alpha, twist, k, s.Eta.ToString("R"), (s.Dy / Half).ToString("R"), s.InducedAngleDeg.ToString("R"),
            s.Gamma.ToString("R"), s.ClLocal.ToString("R"), s.Chord.ToString("R"), selfDeg.ToString("R"),
            (alpha + s.TwistDeg - s.InducedAngleDeg).ToString("R"), (verdicts[m - k].Inside ? "in" : "out:" + string.Join("+", verdicts[m - k].Exceeded).Replace("|α_eff − α_L0|", "alpha").Replace("Cl_local","cl")) + ";sweep=" + s.SweepDeg.ToString("F1"),
            cl.ToString("R"), ar.ToString("R"), sw.Elapsed.TotalSeconds.ToString("F2"), sol.Diagnostics.ToString().Replace(',', ';')));
    }
    // the inboard profile (right half) for the whole-wing reading
    for (int j = m / 2; j < m - 4; j++)
    {
        var s = sol.Strips[j];
        sb.AppendLine(string.Join(",", "P" + wing, n, alpha, twist, m - j, s.Eta.ToString("R"), (s.Dy / Half).ToString("R"), s.InducedAngleDeg.ToString("R"),
            s.Gamma.ToString("R"), s.ClLocal.ToString("R"), s.Chord.ToString("R"), "", (alpha + s.TwistDeg - s.InducedAngleDeg).ToString("R"),
            verdicts[j].Inside ? "in" : "out", "", "", "", ""));
    }
    results[i] = sb.ToString();
});
Console.WriteLine("wing,n,alpha,twist,k_from_tip,eta,deta,alpha_i_deg,gamma,cl_local,chord,self_deg,alpha_eff,verdict,CL,AR,seconds,diag");
foreach (var r in results) Console.Write(r);

static double Chord(string wing, double eta) => wing switch
{
    "ell" => (1 / Math.PI) * Math.Sqrt(Math.Max(0, 1 - eta * eta)),
    "rect" => 0.25,
    "tap" => (1.0 / 3) * (1 - 0.5 * Math.Abs(eta)),
    "tri" => 0.5 * (1 - Math.Abs(eta)),
    _ => throw new ArgumentException(wing)
};

static SectionSample Section(double y, double chord, double twistDeg, bool cambered)
{
    double xLe = -chord / 4;
    double[] fr = cambered ? Enumerable.Range(0, 21).Select(i => i / 20.0).ToArray() : [0, 1];
    double rad = twistDeg * (Math.PI / 180);
    double cs = Math.Cos(rad), sn = Math.Sin(rad);
    double pivot = xLe + 0.25 * chord;
    var placed = new Point3[fr.Length];
    var camb = new double[fr.Length];
    for (int i = 0; i < fr.Length; i++)
    {
        double dx = xLe + fr[i] * chord - pivot;
        double local = cambered ? 4 * 0.04 * fr[i] * (1 - fr[i]) * chord : 0;
        camb[i] = cambered ? 4 * 0.04 * fr[i] * (1 - fr[i]) : 0;
        placed[i] = new Point3(pivot + cs * dx + sn * local, y, -sn * dx + cs * local);
    }
    var zeros = new double[fr.Length];
    var slopes = fr.Select(f => cambered ? 0.16 * (1 - 2 * f) : 0).ToArray();
    return new SectionSample(new StationFrame(y / 1, 2, xLe, xLe + chord, 0, twistDeg, 0), fr, camb, zeros, slopes, placed);
}

static double[] Nodes(double yMin, double yMax, int nPerHalf, string sp)
{
    double[] e = VortexLattice.SpanStations(yMin, yMax, nPerHalf, sp);
    var nodes = new List<double>();
    for (int i = 0; i < e.Length; i++) { nodes.Add(e[i]); if (i + 1 < e.Length) nodes.Add(0.5 * (e[i] + e[i + 1])); }
    return nodes.ToArray();
}
