using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

// S2 sweep probe (tip-handling plan, section 6 row S2). Adapted from the held branch probe (fix/a3a-vlm-tip-law).
// Args: r:n:alpha ... ; r = tip chord / root chord of a straight-quarter-chord tapered rectangle. One CSV row per case.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
const double V = 10, Rho = 1, Half = 1, CRoot = 0.25;
var cases = args.Select(a => a.Split(':')).ToArray();
var rows = new string[cases.Length];
Parallel.For(0, cases.Length, new ParallelOptions { MaxDegreeOfParallelism = 3 }, i =>
{
    double r = double.Parse(cases[i][0]);
    int n = int.Parse(cases[i][1]);
    double alpha = double.Parse(cases[i][2]);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    double[] nodes = Nodes(-Half, Half, n);
    var sections = nodes.Select(y => Section(y, CRoot * (1 - (1 - r) * Math.Abs(y) / Half))).ToList();
    var settings = Settings.Default with { NSpanPerHalf = n, NChord = 4, SpanSpacing = "cosine" };
    LatticeSolution sol;
    try { sol = VortexLattice.Solve(sections, settings, new OperatingPoint(V, 101325, null, "root LE", alpha, null), Rho, default); }
    catch (LatticeFailedException ex) { rows[i] = string.Join(",", r, n, alpha, "FAIL:" + ex.Code) + "\n"; return; }
    double secs = sw.Elapsed.TotalSeconds;
    double area = 0, lift = 0;
    foreach (var s in sol.Strips) { area += s.Chord * s.Dy; lift += s.Gamma * s.Dy; }
    double cl = Rho * V * lift / (0.5 * Rho * V * V * area);
    var verdicts = MethodRecord.Verdicts(sol, alpha, 0);
    int m = sol.Strips.Count;
    // judged strips: every strip except the outermost one per half (Ruling 78)
    bool anyJudgedOut = false;
    for (int j = 1; j < m - 1; j++) if (!verdicts[j].Inside) { anyJudgedOut = true; break; }
    // consistency index along the starboard half, root outward
    var half = Enumerable.Range(0, m).Where(j => sol.Strips[j].Eta > 0).OrderBy(j => sol.Strips[j].Eta).ToList();
    double Ci(int j) { var s = sol.Strips[j]; double ae = alpha + s.TwistDeg - s.InducedAngleDeg; return s.ClLocal / (2 * Math.PI * (ae * Math.PI / 180)); }
    double cRoot = Ci(half[0]);
    string edgeEta = "none", edgeChords = "none", edgeRatio = "";
    foreach (int j in half)
    {
        double q = Ci(j) / cRoot;
        if (q < 0.9) { var s = sol.Strips[j]; edgeEta = s.YInboard.ToString("R"); edgeChords = ((Half - s.YInboard) / (CRoot * r)).ToString("F3"); edgeRatio = q.ToString("F4"); break; }
    }
    var f = new List<string> { r.ToString("R"), n.ToString(), alpha.ToString("R") };
    for (int k = 1; k <= 3; k++)
    {
        var s = sol.Strips[m - k];
        var v = verdicts[m - k];
        string verdict = v.Inside ? "in" : "out:" + string.Join("+", v.Exceeded).Replace("|α_eff − α_L0|", "alpha").Replace("Cl_local", "cl");
        f.AddRange([s.Eta.ToString("R"), s.Chord.ToString("R"), (alpha + s.TwistDeg - s.InducedAngleDeg).ToString("R"), s.ClLocal.ToString("R"), verdict]);
    }
    f.AddRange([anyJudgedOut ? "1" : "0", edgeEta, edgeChords, edgeRatio, secs.ToString("F3"), cl.ToString("R"), (4 * Half * Half / area).ToString("R"), m.ToString()]);
    rows[i] = string.Join(",", f) + "\n";
});
Console.WriteLine("r,n,alpha," + string.Join(",", Enumerable.Range(1, 3).SelectMany(k => new[] { "eta", "chord", "alpha_eff", "cl_local", "verdict" }.Select(c => c + "_k" + k))) +
    ",any_judged_out,edge_eta,edge_tip_chords,edge_ratio,seconds,CL,AR,strips");
foreach (var row in rows) Console.Write(row);

static SectionSample Section(double y, double chord)
{
    double xLe = -chord / 4;
    double[] fr = [0, 1];
    var placed = new Point3[] { new(xLe, y, 0), new(xLe + chord, y, 0) };
    var zeros = new double[2];
    return new SectionSample(new StationFrame(y, 2, xLe, xLe + chord, 0, 0, 0), fr, zeros, zeros, zeros, placed);
}

static double[] Nodes(double yMin, double yMax, int nPerHalf)
{
    double[] e = VortexLattice.SpanStations(yMin, yMax, nPerHalf, "cosine");
    var nodes = new List<double>();
    for (int i = 0; i < e.Length; i++) { nodes.Add(e[i]); if (i + 1 < e.Length) nodes.Add(0.5 * (e[i] + e[i + 1])); }
    return nodes.ToArray();
}
