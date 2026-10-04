using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;

namespace BudgetProbe;

// Blend-certificate budget probe (Ruling 73). Every proof runs in Core: Geometry.Assess (admission) and Geometry.SectionAt
// (a query), each under a real ProofBudget (limit 1e9 bit-work). Fixtures are built with the as-built DatImport.FitToBasis,
// FoilSource.SqrtProfileBasis (reflection), SectionEdits.Apply and FoilSource.InsertOnce. Nothing here re-implements a proof.
// Against the as-built Core only the constant rule exists. Against the copy made by patch-core.py the probe sets the node
// rule and the all-query bound through ProbeHooks (reflection) and reads the counts the copy records.
internal static class Program
{
    private const BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
    private static readonly Type? Hooks = typeof(Geometry).Assembly.GetType("CfdWorkbench.Core.ProbeHooks");
    private static readonly MethodInfo SqrtBasis = typeof(FoilSource).GetMethod("SqrtProfileBasis", Any)
        ?? throw new InvalidOperationException("FoilSource.SqrtProfileBasis not found");
    private static readonly List<Row> Rows = new();
    private static readonly double[] Etas = Enumerable.Range(1, 23).Select(i => i / 24.0).ToArray();

    private static readonly (string Name, Func<int, int> Rule)[] AllRules =
    [
        ("256 (as built)", _ => 256),
        ("max(256, ceil(51.2 s))", s => Math.Max(256, (256 * s + 4) / 5)),
        ("max(256, 48 s)", s => Math.Max(256, 48 * s)),
        ("max(256, 96 s)", s => Math.Max(256, 96 * s)),
        ("unbounded (measures need)", _ => 1_000_000),
    ];

    private sealed record Row(string Fixture, int Spans, int Vertices, string Chord, string Rule, int Nodes,
        string Status, string Reason, long Work, double AdmitMs, long Operations, string StatusNoOpBound, string ReasonNoOpBound,
        int AdmitAtoms, double AdmitGap, int QueryNodesMax, long QueryWorkMax, double QueryMsMax, double QueryGapMax,
        int QueryRefusals, string QueryRefusal, string QueryHash, double FitResidual);

    private static int Main(string[] args)
    {
        string mode = args[0], root = args[1];
        Directory.CreateDirectory(Path.Combine(root, "output"));
        Directory.CreateDirectory(Path.Combine(root, "fixtures"));
        var rules = Hooks is null ? AllRules.Take(1).ToArray() : AllRules;
        string build = Hooks is null ? "as-built" : "hooked";
        var clock = Stopwatch.StartNew();
        if (mode == "stage1")
        {
            foreach (double chord in new[] { 120.0, 2000.0 })
                for (int spans = 5; spans <= 16; spans++)
                {
                    var (doc, residual) = Synthetic(chord, spans, 2);
                    Save(root, $"S1-two-s{spans}-c{chord:0}.foil", doc, build);
                    foreach (var (name, rule) in rules) Measure($"two sections, c={chord:0} mm", doc, spans, spans + 5, chord, name, rule, residual);
                }
        }
        else if (mode == "stage2")
        {
            for (int spans = 5; spans <= 16; spans++)
            {
                var (doc, residual) = Synthetic(120, spans, 3);
                Save(root, $"S2-chain-s{spans}-c120.foil", doc, build);
                foreach (var (name, rule) in rules) Measure("three sections Root-Mid-Tip, c=120 mm", doc, spans, spans + 5, 120, name, rule, residual);
            }
            foreach (bool chain in new[] { false, true })
            {
                foreach (var (label, doc, spans, vertices) in Anchored(chain))
                {
                    Save(root, "S2-" + Regex.Replace(label, "[^A-Za-z0-9]+", "-").Trim('-') + ".foil", doc, build);
                    foreach (var (name, rule) in rules) Measure(label, doc, spans, vertices, 0, name, rule, 0);
                }
            }
        }
        else if (mode == "four")
        {
            // Four distinct station profiles: the op model's per-profile term, measured (Inferred from the formula first).
            foreach (int spans in new[] { 1, 3, 5, 6 })
            {
                var (doc, residual) = Synthetic(120, spans, 4);
                Save(root, $"S2-four-s{spans}-c120.foil", doc, build);
                foreach (var (name, rule) in rules) Measure("four sections, c=120 mm", doc, spans, spans + 5, 120, name, rule, residual);
            }
        }
        else if (mode == "timing")
        {
            // Wall time under load is noisy; work is not. Re-time the heaviest fixtures (48 s rule, bound lifted): admission
            // x 9 and every eta x 5, reporting the minimum (the uncontended estimate) and the median.
            if (Hooks is null) throw new InvalidOperationException("timing needs the hooked Core");
            Set("Rule", (Func<int, int>)(s => Math.Max(256, 48 * s))); Set("OperationBound", long.MaxValue);
            var lines = new StringBuilder("| Fixture | Spans | Admission ms min / median (9) | Slowest-eta query ms min / median (5 each) | Query work at that eta |\n|---|---|---|---|---|\n");
            foreach (var (label, stations, spans) in new[] { ("two sections", 2, 6), ("two sections", 2, 16), ("three sections", 3, 13), ("four sections", 4, 6) })
            {
                var parsed = FoilSource.Parse(Synthetic(120, spans, stations).Doc);
                var admit = new List<double>();
                GeometryAssessment a = null!;
                for (int rep = 0; rep < 9; rep++) { var sw = Stopwatch.StartNew(); a = Geometry.Assess(parsed, new ProofBudget()); admit.Add(sw.Elapsed.TotalMilliseconds); }
                double worstMin = 0, worstMedian = 0; long worstWork = 0;
                foreach (double eta in Etas)
                {
                    var q = new List<double>(); long work = 0;
                    for (int rep = 0; rep < 5; rep++)
                    {
                        var watch = new ProofBudget(); var sw = Stopwatch.StartNew();
                        Geometry.SectionAt(a.Certificate!, eta, 0.3, watch);
                        q.Add(sw.Elapsed.TotalMilliseconds); work = watch.Spent;
                    }
                    q.Sort();
                    if (q[0] > worstMin) (worstMin, worstMedian, worstWork) = (q[0], q[2], work);
                }
                admit.Sort();
                lines.Append($"| {label} | {spans} | {admit[0]:F0} / {admit[4]:F0} | {worstMin:F0} / {worstMedian:F0} | {worstWork:N0} |\n");
            }
            File.WriteAllText(Path.Combine(root, "output", "timing.md"), TimingFront + "Load average during the run: see verdict.md section 7.\n\n" + lines);
            Console.Write(lines);
            return 0;
        }
        else throw new ArgumentException("mode is stage1, stage2, four or timing");
        string stem = mode + "-" + build;
        File.WriteAllText(Path.Combine(root, "output", stem + ".json"), JsonSerializer.Serialize(Rows, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.WriteAllText(Path.Combine(root, "output", stem + ".md"), Table(mode, build, clock.Elapsed.TotalSeconds));
        Console.WriteLine($"{stem}: {Rows.Count} rows in {clock.Elapsed.TotalSeconds:F1} s");
        return 0;
    }

    // Two (or three) differing shapes, each fitted onto the same sqrt spacing with spans + 5 vertices (simple knots).
    private static (byte[] Doc, double Residual) Synthetic(double chordMm, int spans, int stations)
    {
        var (knots, xs) = ((double[], double[]))SqrtBasis.Invoke(null, [spans + 5])!;
        var naca = Sampled(DefaultNaca(), 1, 1);
        var sectionA = Sampled(ExampleSection(), 1, 1);
        var mid = Sampled(DefaultNaca(), 1.2, 0.85);
        var fits = new List<ImportedProfile>
        {
            DatImport.FitToBasis(naca with { Name = "root" }, "root", knots, xs, 5)!,
            DatImport.FitToBasis(sectionA with { Name = "tip" }, "tip", knots, xs, 5)!,
        };
        if (stations >= 3) fits.Add(DatImport.FitToBasis(mid with { Name = "mid" }, "mid", knots, xs, 5)!);
        if (stations >= 4) fits.Add(DatImport.FitToBasis(Sampled(DefaultNaca(), 0.9, 1.1) with { Name = "outer" }, "outer", knots, xs, 5)!);
        string sections = stations == 4 ? "  sections { at root profile \"root\" at 33 % profile \"mid\" at 67 % profile \"outer\" at tip profile \"tip\" }\n" : stations == 3
            ? "  sections { at root profile \"root\" at 50 % profile \"mid\" at tip profile \"tip\" }\n"
            : "  sections { at root profile \"root\" at tip profile \"tip\" }\n";
        var text = new StringBuilder(Header(chordMm)).Append("  profiles {\n");
        foreach (var fit in fits) text.Append(Indent(fit.ProfileBlock)).Append('\n');
        text.Append("  }\n").Append(sections).Append("}\n");
        byte[] doc = FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(text.ToString())));
        return (doc, fits.Max(f => f.MaxResidual));
    }

    // New foil (10 points, 5 spans) and the Example foil (8 points, 3 spans): Root made distinct by a y move, then 0, 1 or 2
    // anchors on the edited profile, each propagated to the differing neighbour(s) by the same Boehm insertions (XPA case J).
    private static IEnumerable<(string Label, byte[] Doc, int Spans, int Vertices)> Anchored(bool chain)
    {
        foreach (var (foil, targets) in new (string, double[])[] { ("New foil", [0.35, 0.6]), ("Example", [0.35, 0.65]) })
        {
            byte[] s = foil == "New foil" ? FoilSource.NewDefault()
                : FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(FindUp("docs/examples/foildsl/foil-basic.foil"))));
            string shared = Def(s).Profiles[Def(s).Assignments[0].Profile].Name;
            if (chain)
            {
                string text = Encoding.UTF8.GetString(s);
                text = Regex.Replace(text, "sections \\{ at root profile \"([^\"]+)\" at tip", m => $"sections {{ at root profile \"{m.Groups[1].Value}\" at 50 % profile \"{m.Groups[1].Value}\" at tip");
                s = Encoding.UTF8.GetBytes(text);
            }
            int edited = chain ? 1 : 0;
            s = FoilSource.MakeIndependent(s, shared, 0).Source;
            if (chain) s = FoilSource.MakeIndependent(s, shared, 1).Source;
            var def = Def(s);
            string[] names = def.Assignments.Select(a => def.Profiles[a.Profile].Name).ToArray();
            // Each station differs from its neighbours by a y move on a different vertex.
            for (int station = 0; station < names.Length - 1; station++)
            {
                var p = Prof(s, names[station]);
                int v = Enumerable.Range(2, p.Upper.Points.Length - 4).OrderBy(i => Math.Abs(p.Upper.Points[i][0] - (0.3 + 0.1 * station))).First();
                s = Step(s, station, new SectionStep.Move(SurfaceSide.Upper, p.Upper.Ids[v], p.Upper.Points[v][0], p.Upper.Points[v][1] + 0.003));
            }
            string where = chain ? "three sections" : "two sections";
            int vertices0 = Prof(s, names[edited]).Upper.Points.Length;
            yield return ($"{foil} {vertices0} points, {where}, 0 anchors", s, Spans(Prof(s, names[edited])), vertices0);
            for (int count = 1; count <= targets.Length; count++)
            {
                var p = Prof(s, names[edited]);
                int vj = Enumerable.Range(2, p.Upper.Points.Length - 4)
                    .Where(i => !Anchor(p, i) && !Anchor(p, i - 1) && !Anchor(p, i + 1))
                    .OrderBy(i => Math.Abs(p.Upper.Points[i][0] - targets[count - 1])).First();
                byte[] next = Step(s, edited, new SectionStep.SetType(SurfaceSide.Upper, p.Upper.Ids[vj], true));
                double knot = NewKnot(p.Upper.Knots, Prof(next, names[edited]).Upper.Knots);
                int copies = Prof(next, names[edited]).Upper.Knots.Count(k => k == knot);
                for (int other = 0; other < names.Length; other++)
                    if (other != edited && names[other] != names[edited]) next = PropagateInsert(next, names[other], knot, copies);
                s = next;
                var pe = Prof(s, names[edited]);
                yield return ($"{foil} {vertices0} points, {where}, {count} anchor{(count > 1 ? "s" : "")}", s, Spans(pe), pe.Upper.Points.Length);
            }
        }
    }

    private static void Measure(string fixture, byte[] doc, int spans, int vertices, double chord, string ruleName, Func<int, int> rule, double residual)
    {
        var parsed = FoilSource.Parse(doc);
        if (Hooks is not null) { Set("Rule", rule); Set("OperationBound", 1_000_000L); }
        GeometryAssessment a = null!;
        var times = new List<double>();
        for (int rep = 0; rep < 3; rep++)
        {
            if (Hooks is not null) Set("Operations", -1L);
            var sw = Stopwatch.StartNew();
            a = Geometry.Assess(parsed, new ProofBudget());
            times.Add(sw.Elapsed.TotalMilliseconds);
        }
        times.Sort();
        long operations = -1;
        int nodes = 256;
        var b = a;
        if (Hooks is not null)
        {
            nodes = rule(Get<int>("SpanCount"));
            Set("OperationBound", long.MaxValue); Set("Operations", -1L);
            b = Geometry.Assess(parsed, new ProofBudget());
            operations = Get<long>("Operations");
            Set("OperationBound", 1_000_000L);
        }
        int qNodes = 0, refusals = 0; long qWork = 0; double qMs = 0, qGap = 0; string refusal = "", hash = "";
        if (b.Certificate is { } certificate)
        {
            ulong h = 1469598103934665603UL;
            foreach (double eta in Etas)
            {
                if (Hooks is not null) Hooks.GetMethod("ResetQuery", Any)!.Invoke(null, null);
                var watch = new ProofBudget();
                var sw = Stopwatch.StartNew();
                try
                {
                    var e = Geometry.SectionAt(certificate, eta, 0.3, watch);
                    foreach (double d in new[] { e.Upper.Lower, e.Upper.Upper, e.Lower.Lower, e.Lower.Upper })
                        h = (h ^ (ulong)BitConverter.DoubleToInt64Bits(d)) * 1099511628211UL;
                }
                catch (ContractError error) { refusals++; if (refusal.Length == 0) refusal = error.Code + " at eta " + G(eta); }
                qMs = Math.Max(qMs, sw.Elapsed.TotalMilliseconds);
                qWork = Math.Max(qWork, watch.Spent);
                if (Hooks is not null)
                {
                    int n = Get<int>("QueryNodesMax");
                    qNodes = Math.Max(qNodes, n);
                    if (n > 0)
                    {
                        var lower = Get<Rational>("LastLower"); var upper = Get<Rational>("LastUpper");
                        qGap = Math.Max(qGap, ((upper - lower) / lower).Nearest());
                    }
                }
            }
            hash = h.ToString("x16", CultureInfo.InvariantCulture);
        }
        var cert = a.Certificate ?? b.Certificate;
        Rows.Add(new Row(fixture, spans, vertices, chord == 0 ? "foil's own" : G(chord) + " mm", ruleName, nodes,
            a.Status.ToString(), a.Status == GeometryStatus.Certified ? "" : a.Reason, a.ProofWork, times[1], operations,
            b.Status.ToString(), b.Status == GeometryStatus.Certified ? "" : b.Reason,
            cert?.SubdivisionNodes ?? 0, cert?.NormalizationRelativeErrorUpper ?? 0, qNodes, qWork, qMs, qGap, refusals, refusal, hash, residual));
        var r = Rows[^1];
        Console.WriteLine($"{fixture} s={spans} {ruleName}: {r.Status} {r.Reason} work={r.Work} {r.AdmitMs:F0} ms ops={r.Operations} | no-op-bound {r.StatusNoOpBound} {r.ReasonNoOpBound} | query nodes={r.QueryNodesMax} work={r.QueryWorkMax} {r.QueryMsMax:F0} ms gap={r.QueryGapMax:G3} refusals={r.QueryRefusals}");
    }

    private const string TimingFront = "---\nid: proof-blend-certificate-budget-timing\n" +
        "title: \"Blend budget probe - re-timed heaviest fixtures (generated)\"\ntype: proof-pack\nstatus: in-review\n" +
        "owner: \"@timianmalloo\"\nphase: design\ntags: [certificate, blend, budget, probe, generated, ruling-73]\n" +
        "links:\n  - { to: proof-blend-certificate-budget, rel: depends-on }\nreview-by: 2026-11-30\n" +
        "summary: >-\n  Generated by the probe's timing mode: minimum and median wall time of the heaviest fixtures under machine load.\n" +
        "review-suggested: []\n---\n\n";

    private static string Table(string mode, string build, double seconds)
    {
        var t = new StringBuilder();
        t.AppendLine("---");
        t.AppendLine($"id: proof-blend-certificate-budget-{mode}-{build}");
        t.AppendLine($"title: \"Blend budget probe - {mode}, {build} Core (generated; do not edit, re-run the probe)\"");
        t.AppendLine("type: proof-pack\nstatus: in-review\nowner: \"@timianmalloo\"\nphase: design");
        t.AppendLine("tags: [certificate, blend, budget, probe, generated, ruling-73]");
        t.AppendLine("links:\n  - { to: proof-blend-certificate-budget, rel: depends-on }\nreview-by: 2026-11-30");
        t.AppendLine("summary: >-\n  Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.");
        t.AppendLine("review-suggested: []\n---\n");
        t.AppendLine($"Run: {mode}, {build} Core, {seconds:F0} s, {DateTime.UtcNow:yyyy-MM-dd}. Work is bit-work (ProofBudget unit). Admission ms is the median of 3 warm-ish runs. "
            + "Ops is the all-query operation count computed by QueryFeasibility with the bound lifted (-1: refused before it). Query columns: 23 SectionAt calls at eta = k/24, x = 0.3, each under its own ProofBudget.\n");
        t.AppendLine("| Fixture | Spans | Points | Chord | Rule | Nodes | Admission (bound 1e6) | Admission work | ms | All-query ops | Without the op bound | Admit atoms | Query nodes (max) | Query work (max) | Query ms (max) | max T0 gap | Query refusals | Result hash |");
        t.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in Rows)
            t.AppendLine($"| {r.Fixture} | {r.Spans} | {r.Vertices} | {r.Chord} | {r.Rule} | {r.Nodes} | {r.Status}{(r.Reason.Length > 0 ? ": " + r.Reason : "")} | {r.Work:N0} | {r.AdmitMs:F0} | {(r.Operations < 0 ? "-" : r.Operations.ToString("N0", CultureInfo.InvariantCulture))} | {r.StatusNoOpBound}{(r.ReasonNoOpBound.Length > 0 ? ": " + r.ReasonNoOpBound : "")} | {r.AdmitAtoms} | {r.QueryNodesMax} | {r.QueryWorkMax:N0} | {r.QueryMsMax:F0} | {G(r.QueryGapMax)} | {r.QueryRefusals}{(r.QueryRefusal.Length > 0 ? " (" + r.QueryRefusal + ")" : "")} | {(r.QueryHash.Length > 0 ? r.QueryHash[..8] : "-")} |");
        return t.ToString();
    }

    private static string Header(double chordMm)
    {
        string template = File.ReadAllText(FindUp("docs/proof/m12c-certificate-spike/fixtures/F4-dx0.02-c120.foil"));
        string head = template[..template.IndexOf("  profiles {", StringComparison.Ordinal)];
        head = head.Replace("# GSPK probe fixture (generated by docs/proof/m12c-certificate-spike/probe; see verdict.md for provenance).",
            "# Blend-budget probe fixture (generated by docs/proof/blend-certificate-budget/probe; planform from the GSPK fixtures).", StringComparison.Ordinal);
        string chord = chordMm.ToString("R", CultureInfo.InvariantCulture);
        return Regex.Replace(head, "(trailing cv \\{[^\\n]*)", m => m.Value.Replace(", 120)", ", " + chord + ")", StringComparison.Ordinal));
    }

    private static string Indent(string block) => string.Join("\n", block.Split('\n').Select(line => "    " + line));

    private static ProfileDefinition DefaultNaca() => Def(FoilSource.NewDefault()).Profiles.Single(p => p.Name == "naca-0012");
    private static ProfileDefinition ExampleSection() =>
        Def(FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(FindUp("docs/examples/foildsl/foil-basic.foil"))))).Profiles[0];

    private static DatProfile Sampled(ProfileDefinition p, double upperScale, double lowerScale)
    {
        var xs = Enumerable.Range(0, 161).Select(i => (1 - Math.Cos(Math.PI * i / 160.0)) / 2).ToArray();
        var upper = xs.Select(x => new ProfilePoint(x, x == 0 ? 0 : upperScale * ProfileEvaluator.OrdinateAt(p.Upper, x))).ToList();
        var lower = xs.Select(x => new ProfilePoint(x, x == 0 ? 0 : lowerScale * ProfileEvaluator.OrdinateAt(p.Lower, x))).ToList();
        return new DatProfile(p.Name, "probe-sampled", upper, lower, Encoding.UTF8.GetBytes(p.Name), 2 * xs.Length - 1);
    }

    private static byte[] PropagateInsert(byte[] bytes, string profile, double knot, int times)
    {
        var p = Prof(bytes, profile);
        double[] knots = p.Upper.Knots; var up = Copy(p.Upper.Points); var lo = Copy(p.Lower.Points);
        var ids = p.Upper.Ids.ToList();
        int next = ids.Where(i => i.StartsWith("cv-", StringComparison.Ordinal)).Select(i => int.Parse(i[3..], CultureInfo.InvariantCulture)).DefaultIfEmpty(-1).Max() + 1;
        for (int turn = 0; turn < times; turn++)
        {
            var u = FoilSource.InsertOnce(knots, up, 5, knot); var l = FoilSource.InsertOnce(knots, lo, 5, knot);
            knots = u.Knots; up = u.Points; lo = l.Points; ids.Insert(u.Inserted, "cv-" + (next++).ToString(CultureInfo.InvariantCulture));
        }
        return FoilSource.WriteSurfaces(bytes, profile, knots, up, ids.ToArray(), lo, ids.ToArray());
    }

    private static void Save(string root, string name, byte[] doc, string build)
    {
        if (build == "hooked") File.WriteAllBytes(Path.Combine(root, "fixtures", name), doc);
    }

    private static string FindUp(string relative)
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, relative))) return Path.Combine(dir.FullName, relative);
        throw new FileNotFoundException(relative);
    }

    private static int Spans(ProfileDefinition p) => p.Upper.Knots.Where(k => k > 0 && k < 1).Distinct().Count() + 1;
    private static bool Anchor(ProfileDefinition p, int i) => FoilSource.IsAnchor(p.Upper.Knots, p.Upper.Points.Length, 5, i);
    private static double NewKnot(double[] before, double[] after) => after.Distinct().First(k => after.Count(v => v == k) != before.Count(v => v == k));
    private static byte[] Step(byte[] bytes, int assignment, SectionStep step) => SectionEdits.Apply(bytes, assignment, step).Bytes;
    private static Definition Def(byte[] bytes) => FoilSource.Parse(bytes).Definition!;
    private static ProfileDefinition Prof(byte[] bytes, string name) => Def(bytes).Profiles.Single(p => p.Name == name);
    private static double[][] Copy(double[][] p) => p.Select(q => new[] { q[0], q[1] }).ToArray();
    private static void Set(string field, object value) => Hooks!.GetField(field, Any)!.SetValue(null, value);
    private static T Get<T>(string field) => (T)Hooks!.GetField(field, Any)!.GetValue(null)!;
    private static string G(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
}
