using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;

namespace SpkProbe;

/// <summary>A degree-3 channel: clamped knots and (η, v) control points, as the record stores them.</summary>
internal sealed record Ch(double[] K, double[][] P)
{
    internal int N => P.Length;
    internal Ch WithY(double[] y) => new(K, P.Select((p, i) => new[] { p[0], y[i] }).ToArray());
}

/// <summary>Every evaluation goes through the as-built Core. Nothing here re-implements the evaluator.</summary>
internal static class Core
{
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly MethodInfo FitOrdinatesMethod = Find(typeof(FoilSource), "FitOrdinates");
    private static readonly MethodInfo TipClusteredMethod = Find(typeof(FoilSource), "TipClusteredChannel");
    private static readonly MethodInfo UnitChordMethod = Find(typeof(FoilSource), "UnitChord");
    private static readonly MethodInfo IntegrateOrdinateMethod = Find(typeof(WingEstimates), "IntegrateOrdinate");

    private static MethodInfo Find(Type type, string name) =>
        type.GetMethod(name, Hidden) ?? throw new InvalidOperationException("Core member not found: " + type.Name + "." + name);

    internal static double V(Ch c, double eta) => ChannelEvaluator.Value(c.K, 3, c.P, eta);
    internal static double T(Ch c, double eta) => ChannelEvaluator.Parameter(c.K, 3, c.P, eta);

    internal static (double X, double Y, double X1, double Y1, double X2, double Y2) Jet(Ch c, double t)
    {
        var jet = SplineBasis.Evaluate(c.K, 3, t);
        double x = 0, y = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0;
        for (int i = 0; i < c.N; i++)
        {
            x += jet.N[i] * c.P[i][0]; y += jet.N[i] * c.P[i][1];
            x1 += jet.D1[i] * c.P[i][0]; y1 += jet.D1[i] * c.P[i][1];
            x2 += jet.D2[i] * c.P[i][0]; y2 += jet.D2[i] * c.P[i][1];
        }
        return (x, y, x1, y1, x2, y2);
    }

    internal static double[] FitOrdinates(double[] knots, double[] parameter, double[] target, (int Index, double Value)[] pins) =>
        (double[])FitOrdinatesMethod.Invoke(null, [knots, 3, parameter, target, pins])!;

    internal static (double[] Knots, double[] X) TipClusteredChannel(int count) =>
        ((double[], double[]))TipClusteredMethod.Invoke(null, [count, 3.0])!;

    internal static double UnitChord(double eta) => (double)UnitChordMethod.Invoke(null, [eta])!;

    internal static double IntegrateOrdinate(Ch c) => (double)IntegrateOrdinateMethod.Invoke(null, [AsCurve(c)])!;

    internal static Curve AsCurve(Ch c) => new("probe", 3, c.K, c.P,
        Enumerable.Range(0, c.N).Select(i => "cv-" + i.ToString(CultureInfo.InvariantCulture)).ToArray(),
        [], 0, false, [], null, 0, 0, 0, 0);

    internal static Dictionary<string, Ch> Channels(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        var definition = parsed.Definition ?? throw new InvalidOperationException("parse failed: " + parsed.Diagnostics[0].Code);
        return definition.Curves.Where(pair => pair.Value.Degree == 3 && !pair.Key.Contains('/'))
            .ToDictionary(pair => pair.Key, pair => new Ch(pair.Value.Knots, pair.Value.Points));
    }

    internal static double HalfSpan(byte[] source) => FoilSource.Parse(source).Definition!.HalfSpan;
}

internal sealed record Settings(int FitUniformSamples, int FitGaussPerSpan, int OracleUniformSamples, int SupportSamples,
    double SupportThreshold, int KappaSamplesPerSpan, double SignTolerance, double BreakTolerance, double BreakOffset, double SignFloor);

internal sealed record Fairness(double Energy, int Pieces, int CombSignChanges, int Breaks, double Support, double Lever, int Vertex, double[] CombAt);

internal sealed record Change(double Max, double Eta);

internal static class Measure
{
    internal static Settings S = null!;

    internal static double[] Uniform(int count) => Enumerable.Range(0, count).Select(i => i / (double)(count - 1)).ToArray();

    internal static double[] UniformKnots(int n)
    {
        var knots = new List<double> { 0, 0, 0, 0 };
        for (int j = 1; j <= n - 4; j++) knots.Add(j / (double)(n - 3));
        knots.AddRange([1, 1, 1, 1]);
        return [.. knots];
    }

    internal static double[] Greville(double[] k) =>
        Enumerable.Range(0, k.Length - 4).Select(i => (k[i + 1] + k[i + 2] + k[i + 3]) / 3).ToArray();

    internal static IEnumerable<(double A, double B)> Spans(double[] k)
    {
        for (int s = 3; s < k.Length - 4; s++)
            if (k[s + 1] > k[s]) yield return (k[s], k[s + 1]);
    }

    internal static double[] InteriorKnots(double[] k) => k.Distinct().Where(v => v > 0 && v < 1).ToArray();

    /// <summary>Least squares through the six anchors with the root-tangent row P1.y = P0.y (ADR-0001's fixture rule).</summary>
    internal static (Ch Curve, double Residual) AnchorFit(double[] eta, double[] values, int n)
    {
        double[] knots = UniformKnots(n);
        var c0 = new Ch(knots, Greville(knots).Select(x => new[] { x, 0d }).ToArray());
        var design = new double[eta.Length, n];
        for (int row = 0; row < eta.Length; row++)
        {
            double[] basis = SplineBasis.Values(knots, 3, Core.T(c0, eta[row]));
            for (int col = 0; col < n; col++) design[row, col] = basis[col];
        }
        var a = new double[1, n];
        a[0, 0] = -1; a[0, 1] = 1;
        double[] y = ConstrainedFit.Solve(design, values, Enumerable.Repeat(1d, eta.Length).ToArray(), new double[n, n], 0, a, [0]);
        var curve = c0.WithY(y);
        double residual = eta.Select((e, i) => Math.Abs(Core.V(curve, e) - values[i])).Max();
        return (curve, residual);
    }

    private static double Kappa(Ch c, double t)
    {
        var j = Core.Jet(c, t);
        return (j.X1 * j.Y2 - j.Y1 * j.X2) / Math.Pow(j.X1 * j.X1 + j.Y1 * j.Y1, 1.5);
    }

    /// <summary>The η of each sign change; a value within max(tolerance · max|v|, floor) of zero carries no sign.</summary>
    private static List<double> SignChanges(List<double> values, List<double> eta)
    {
        double ignore = Math.Max(S.SignTolerance * (values.Count == 0 ? 0 : values.Max(Math.Abs)), S.SignFloor);
        var at = new List<double>();
        int last = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (Math.Abs(values[i]) <= ignore) continue;
            int sign = Math.Sign(values[i]);
            if (last != 0 && sign != last) at.Add(eta[i]);
            last = sign;
        }
        return at;
    }

    internal static Fairness Fair(Ch c)
    {
        var kappa = new List<double>();
        var slope = new List<double>();
        var etas = new List<double>();
        double energy = 0;
        foreach (var (a, b) in Spans(c.K))
        {
            int m = S.KappaSamplesPerSpan;
            double h = (b - a) * 1e-5;
            double previous = double.NaN, previousT = a;
            for (int k = 0; k <= m; k++)
            {
                double t = a + (b - a) * k / m;
                double tl = Math.Max(a, t - h), tr = Math.Min(b, t + h);
                double dkdt = (Kappa(c, tr) - Kappa(c, tl)) / (tr - tl);
                double dx = Core.Jet(c, t).X1;
                double integrand = dkdt * dkdt / dx;
                if (k > 0) energy += 0.5 * (integrand + previous) * (t - previousT);
                previous = integrand; previousT = t;
                kappa.Add(Kappa(c, t));
                slope.Add(dkdt / dx);
                etas.Add(Core.Jet(c, t).X);
            }
        }
        double kappaMax = kappa.Max(Math.Abs);
        int breaks = InteriorKnots(c.K).Count(knot =>
            Math.Abs(Kappa(c, knot + S.BreakOffset) - Kappa(c, knot - S.BreakOffset)) > S.BreakTolerance * kappaMax);
        int vertex = c.N / 2;
        var moved = c.WithY(c.P.Select((p, i) => i == vertex ? p[1] + 1 : p[1]).ToArray());
        double[] grid = Uniform(S.SupportSamples);
        double[] delta = grid.Select(e => Math.Abs(Core.V(moved, e) - Core.V(c, e))).ToArray();
        var comb = SignChanges(kappa, etas);
        return new Fairness(energy, SignChanges(slope, etas).Count + 1, comb.Count, breaks,
            delta.Count(d => d > S.SupportThreshold) / (double)grid.Length, delta.Max(), vertex, [.. comb]);
    }

    /// <summary>Design §6.4 Rebuild (curve, N): knots follow the current spacing; abscissae at the new Greville
    /// parameters; ordinates by least squares to the current v(η) with the root, tip and root-square pins.</summary>
    internal static Ch Rebuild(Ch current, int n)
    {
        double[] d = current.K.Distinct().OrderBy(v => v).ToArray();
        int m = d.Length - 1;
        var knots = new List<double> { 0, 0, 0, 0 };
        for (int j = 1; j <= n - 4; j++)
        {
            double s = j / (double)(n - 3) * m;
            int i = (int)Math.Floor(s);
            knots.Add(i >= m ? d[m] : d[i] + (s - i) * (d[i + 1] - d[i]));
        }
        knots.AddRange([1, 1, 1, 1]);
        double[] k = [.. knots];
        double[] x = Greville(k).Select(g => Core.Jet(current, g).X).ToArray();
        x[0] = 0; x[^1] = 1;
        for (int i = 1; i < n; i++)
            if (!(x[i] - x[i - 1] >= 1e-7)) throw new InvalidOperationException("COPY-197b would fold");
        var candidate = new Ch(k, x.Select(v => new[] { v, 0d }).ToArray());
        var eta = Uniform(S.FitUniformSamples).ToList();
        double[] gauss = [-0.8611363115940526, -0.3399810435848563, 0.3399810435848563, 0.8611363115940526];
        foreach (var (a, b) in Spans(k))
            foreach (double g in gauss.Take(S.FitGaussPerSpan))
                eta.Add(Core.Jet(candidate, a + (b - a) * (g + 1) / 2).X);
        double[] parameter = eta.Select(e => Core.T(candidate, e)).ToArray();
        double[] target = eta.Select(e => Core.V(current, e)).ToArray();
        var pins = new List<(int, double)> { (0, current.P[0][1]), (n - 1, current.P[^1][1]) };
        if (current.P[1][1].Equals(current.P[0][1])) pins.Add((1, current.P[0][1]));
        return candidate.WithY(Core.FitOrdinates(k, parameter, target, [.. pins]));
    }

    /// <summary>The A4.5 oracle (MaxChange): 201 uniform η plus the η of every interior knot of both curves.</summary>
    internal static Change MaxChange(Ch a, Ch b)
    {
        var eta = Uniform(S.OracleUniformSamples)
            .Concat(InteriorKnots(a.K).Select(t => Core.Jet(a, t).X))
            .Concat(InteriorKnots(b.K).Select(t => Core.Jet(b, t).X));
        double best = -1, at = 0;
        foreach (double e in eta)
        {
            double d = Math.Abs(Core.V(a, e) - Core.V(b, e));
            if (d > best) { best = d; at = e; }
        }
        return new Change(best, at);
    }

    /// <summary>End direction in the plan, degrees from the span axis; η scaled by the half span, v as stored.</summary>
    internal static double TipAngle(Ch c, double halfSpan) =>
        Math.Atan2(c.P[^1][1] - c.P[^2][1], (c.P[^1][0] - c.P[^2][0]) * halfSpan) * 180 / Math.PI;

    internal static double RootAngle(Ch c, double halfSpan) =>
        Math.Atan2(c.P[1][1] - c.P[0][1], (c.P[1][0] - c.P[0][0]) * halfSpan) * 180 / Math.PI;

    /// <summary>New foil's rails built exactly as FoilSource.NewDefault does, at any vertex count (Ruling 64).</summary>
    internal static (Ch Lead, Ch Trail, double Area) NewFoilRails(int count)
    {
        var (knots, x) = Core.TipClusteredChannel(count);
        double[] eta = Uniform(401);
        double tipChord = Core.UnitChord(1);
        double[] lead = Core.FitOrdinates(knots, eta, eta.Select(e => (1 - Core.UnitChord(e)) / 4).ToArray(),
            [(0, 0d), (1, 0d), (count - 1, (1 - tipChord) / 4)]);
        double[] trail = Core.FitOrdinates(knots, eta, eta.Select(e => (1 - Core.UnitChord(e)) / 4 + Core.UnitChord(e)).ToArray(),
            [(0, 1d), (1, 1d), (count - 1, (1 - tipChord) / 4 + tipChord)]);
        double scale = 1;
        for (int step = 0; step < 4; step++)
        {
            var l = new Ch(knots, x.Select((v, i) => new[] { v, lead[i] * scale }).ToArray());
            var t = new Ch(knots, x.Select((v, i) => new[] { v, trail[i] * scale }).ToArray());
            double area = 2 * 0.5 * (Core.IntegrateOrdinate(t) - Core.IntegrateOrdinate(l));
            if (Math.Abs(area - 0.1) / 0.1 <= 1e-12) return (l, t, area);
            scale *= 0.1 / area;
        }
        throw new InvalidOperationException("area did not converge");
    }
}

internal static class Program
{
    private static string F(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);
    private static string G(double v) => Math.Abs(v) < 1e-12 ? "0" : Math.Abs(v) < 1e-9 ? "<1e-9" : F(v, "G4");

    private static int Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        string repo = Path.GetFullPath(Path.Combine(dir, "../../.."));
        using var fixtureDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, "fixture.json")));
        var fx = fixtureDoc.RootElement;
        var st = fx.GetProperty("settings");
        Measure.S = new Settings(st.GetProperty("fitUniformSamples").GetInt32(), st.GetProperty("fitGaussPerSpan").GetInt32(),
            st.GetProperty("oracleUniformSamples").GetInt32(), st.GetProperty("supportSamples").GetInt32(),
            st.GetProperty("supportThreshold").GetDouble(), st.GetProperty("kappaSamplesPerSpan").GetInt32(),
            st.GetProperty("signTolerance").GetDouble(), st.GetProperty("breakTolerance").GetDouble(),
            st.GetProperty("breakOffset").GetDouble(), st.GetProperty("signFloor").GetDouble());
        int[] counts = fx.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        int[] rebuildTo = fx.GetProperty("rebuildTargets").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        var anchors = fx.GetProperty("exampleAnchors");
        double[] anchorEta = anchors.GetProperty("eta").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        var md = new StringBuilder();
        md.AppendLine("---");
        md.AppendLine("id: proof-planform-verbs-fairness-table");
        md.AppendLine("title: \"SPK — generated fairness and Rebuild table (do not edit; re-run the probe)\"");
        md.AppendLine("type: proof-pack");
        md.AppendLine("status: in-review");
        md.AppendLine("owner: \"@track-spk\"");
        md.AppendLine("phase: implementation");
        md.AppendLine("tags: [planform-verbs, fairness, rebuild, spk, generated]");
        md.AppendLine("links:");
        md.AppendLine("  - { to: proof-planform-verbs-fairness, rel: depends-on }");
        md.AppendLine("review-by: 2026-10-31");
        md.AppendLine("summary: >-");
        md.AppendLine("  Generated by docs/proof/planform-verbs-fairness/probe from fixture.json; the full SPK table.");
        md.AppendLine("review-suggested: []");
        md.AppendLine("---");
        md.AppendLine();
        md.AppendLine("# SPK — fairness and Rebuild table (generated)");
        md.AppendLine();
        var json = new Dictionary<string, object>();

        // 1. The five Example curves (ADR-0001's six anchors) at d3 · 4, 5 and 7.
        md.AppendLine("## 1. The five Example curves (six anchors, root-tangent row) — d3 · N");
        md.AppendLine();
        md.AppendLine("Cell: κ′ energy · pieces · comb sign changes · breaks · support · lever · anchor residual (curve unit).");
        md.AppendLine();
        md.AppendLine("| Curve | " + string.Join(" | ", counts.Select(n => "d3 · " + n)) + " | Rebuild 7 → " + string.Join(" · 7 → ", rebuildTo) + " (max Δ @ η) |");
        md.AppendLine("|---|" + string.Concat(counts.Select(_ => "---|")) + "---|");
        var exampleJson = new List<object>();
        foreach (var curve in anchors.GetProperty("curves").EnumerateArray())
        {
            string name = curve.GetProperty("name").GetString()!;
            string unit = curve.GetProperty("unit").GetString()!;
            double[] values = curve.GetProperty("values").EnumerateArray().Select(e => e.GetDouble()).ToArray();
            var cells = new List<string>();
            var perCount = new Dictionary<string, object>();
            Ch? seven = null;
            foreach (int n in counts)
            {
                var (fit, residual) = Measure.AnchorFit(anchorEta, values, n);
                if (n == 7) seven = fit;
                var f = Measure.Fair(fit);
                cells.Add($"{G(f.Energy)} · {f.Pieces} · {f.CombSignChanges} · {f.Breaks} · {F(f.Support, "F3")} · {F(f.Lever, "F3")} · {G(residual)}");
                perCount["d3x" + n] = new { f.Energy, f.Pieces, f.CombSignChanges, f.Breaks, f.Support, f.Lever, f.Vertex, AnchorResidual = residual, Points = fit.P, Knots = fit.K };
            }
            var rebuilt = rebuildTo.Select(n => Measure.MaxChange(seven!, Measure.Rebuild(seven!, n))).ToArray();
            md.AppendLine($"| {name} ({unit}) | " + string.Join(" | ", cells) + " | " +
                string.Join(" · ", rebuilt.Select(r => $"{G(r.Max)} @ {F(r.Eta, "F3")}")) + " |");
            exampleJson.Add(new { name, unit, measures = perCount, rebuild = rebuildTo.Zip(rebuilt, (n, r) => new { to = n, r.Max, r.Eta }) });
        }
        json["example"] = exampleJson;

        // 2. The shipped Example (example.foil, the CLI's `inspect example`): Rebuild 7 → 4 and 5 per channel.
        byte[] shipped = File.ReadAllBytes(Path.Combine(repo, fx.GetProperty("shippedExample").GetString()!));
        var shippedChannels = Core.Channels(shipped);
        md.AppendLine();
        md.AppendLine("## 2. Shipped Example (`example.foil`) — Rebuild 7 → N, max Δ (stored unit) @ η; κ′ energy at 7 → N");
        md.AppendLine();
        md.AppendLine("| Channel | " + string.Join(" | ", rebuildTo.Select(n => "7 → " + n)) + " |");
        md.AppendLine("|---|" + string.Concat(rebuildTo.Select(_ => "---|")));
        var shippedJson = new List<object>();
        foreach (var (name, ch) in shippedChannels.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var e7 = Measure.Fair(ch).Energy;
            var cells = rebuildTo.Select(n =>
            {
                var r = Measure.Rebuild(ch, n);
                var c = Measure.MaxChange(ch, r);
                var f = Measure.Fair(r);
                shippedJson.Add(new { name, to = n, c.Max, c.Eta, EnergyBefore = e7, EnergyAfter = f.Energy, f.CombSignChanges });
                return $"{G(c.Max)} @ {F(c.Eta, "F3")}; κ′E {G(e7)} → {G(f.Energy)}; comb {f.CombSignChanges}";
            });
            md.AppendLine($"| {name} | " + string.Join(" | ", cells) + " |");
        }
        json["shippedExample"] = shippedJson;

        // 3. New foil rails: today's 10, Ruling 64's 4, and 5.
        byte[] newFoil = FoilSource.NewDefault();
        var today = Core.Channels(newFoil);
        double halfSpanM = Core.HalfSpan(newFoil);
        var replica10 = Measure.NewFoilRails(10);
        double replicaMismatch = 0;
        int replicaBitDiffs = 0;
        foreach (var (mine, theirs) in new[] { (replica10.Lead, today["leading"]), (replica10.Trail, today["trailing"]) })
            for (int i = 0; i < mine.N; i++)
                for (int a = 0; a < 2; a++)
                {
                    if (!mine.P[i][a].Equals(theirs.P[i][a])) replicaBitDiffs++;
                    replicaMismatch = Math.Max(replicaMismatch, Math.Abs(mine.P[i][a] - theirs.P[i][a]));
                }
        double wingArea = WingEstimates.From(newFoil, "accepted", 0).AreaSquareMeters;

        int[] newCounts = fx.GetProperty("newFoilCounts").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        md.AppendLine();
        md.AppendLine("## 3. New foil rails (units m; half span " + G(halfSpanM) + " m) — NewDefault's construction at N vertices");
        md.AppendLine();
        md.AppendLine("Cell: κ′ energy · pieces · comb sign changes · breaks · support · lever · max fit residual to the analytic target (mm).");
        md.AppendLine();
        md.AppendLine("| Rail | " + string.Join(" | ", newCounts.Select(n => "d3 · " + n)) + " |");
        md.AppendLine("|---|" + string.Concat(newCounts.Select(_ => "---|")));
        var built = newCounts.ToDictionary(n => n, n => n == 10 ? (today["leading"], today["trailing"], wingArea) : Measure.NewFoilRails(n));
        var railJson = new List<object>();
        foreach (var (label, pick, target) in new (string, Func<(Ch, Ch, double), Ch>, Func<double, double>)[]
                 {
                     ("LE", b => b.Item1, e => (1 - Core.UnitChord(e)) / 4),
                     ("TE", b => b.Item2, e => (1 - Core.UnitChord(e)) / 4 + Core.UnitChord(e)),
                 })
        {
            var cells = newCounts.Select(n =>
            {
                var c = pick(built[n]);
                var f = Measure.Fair(c);
                double scale = c.P[0][1] == 0 ? c.P[^1][1] / ((1 - Core.UnitChord(1)) / 4) : c.P[0][1];
                double residual = Measure.Uniform(401).Max(e => Math.Abs(Core.V(c, e) - target(e) * scale)) * 1000;
                railJson.Add(new { rail = label, count = n, f.Energy, f.Pieces, f.CombSignChanges, f.Breaks, f.CombAt, f.Support, f.Lever, f.Vertex, TargetResidualMm = residual, Points = c.P, Knots = c.K });
                string inflections = f.CombAt.Length == 0 ? "" : " (at " + string.Join(", ", f.CombAt.Select(e => F(e * halfSpanM * 1000, "F0") + " mm")) + ")";
                return $"{G(f.Energy)} · {f.Pieces} · {f.CombSignChanges}{inflections} · {f.Breaks} · {F(f.Support, "F3")} · {F(f.Lever, "F3")} · {F(residual, "F3")}";
            });
            md.AppendLine($"| {label} | " + string.Join(" | ", cells) + " |");
        }
        json["newFoilRails"] = railJson;
        md.AppendLine();
        var overshootJson = new List<object>();
        foreach (int n in newCounts)
        {
            var (lead, trail, _) = built[n];
            double[] grid = Measure.Uniform(Measure.S.SupportSamples);
            double leAt = grid.MaxBy(e => -Core.V(lead, e));
            double teAt = grid.MaxBy(e => Core.V(trail, e) - trail.P[0][1]);
            double leForward = -Core.V(lead, leAt) * 1000, teAft = (Core.V(trail, teAt) - trail.P[0][1]) * 1000;
            overshootJson.Add(new { count = n, LeForwardOfRootMm = leForward, LeAtEta = leAt, TeAftOfRootMm = teAft, TeAtEta = teAt });
            string Over(double mm, double eta) => mm <= 0 ? "none" : $"{F(mm, "F3")} mm at {F(eta * halfSpanM * 1000, "F1")} mm";
            md.AppendLine($"- d3 · {n}: LE forward of the root LE: {Over(leForward, leAt)}; TE aft of the root TE: {Over(teAft, teAt)} ({Measure.S.SupportSamples} uniform η).");
        }
        json["newFoilRootOvershoot"] = overshootJson;

        // 4. New foil 10 → 4 and 5: Ruling 64's construction and the design's Rebuild, against today's 10.
        md.AppendLine();
        md.AppendLine("## 4. New foil deviation from today's 10-point rails (A4.5 oracle; mm, η → mm from root)");
        md.AppendLine();
        md.AppendLine("| Route | Rail | max Δ (mm) @ from root | tip direction 10 → N (°) | turn (°) | root turn (°) | area (cm²) |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        var devJson = new List<object>();
        foreach (int n in rebuildTo)
        {
            var construction = built.TryGetValue(n, out var b) ? b : Measure.NewFoilRails(n);
            var rebuildLead = Measure.Rebuild(today["leading"], n);
            var rebuildTrail = Measure.Rebuild(today["trailing"], n);
            double rebuildArea = 2 * halfSpanM * (Core.IntegrateOrdinate(rebuildTrail) - Core.IntegrateOrdinate(rebuildLead));
            foreach (var (route, lead, trail, area) in new[]
                     {
                         ($"NewDefault at {n} (Ruling 64)", construction.Item1, construction.Item2, construction.Item3),
                         ($"Rebuild 10 → {n} (§6.4)", rebuildLead, rebuildTrail, rebuildArea),
                     })
            {
                foreach (var (rail, before, after) in new[] { ("LE", today["leading"], lead), ("TE", today["trailing"], trail) })
                {
                    var c = Measure.MaxChange(before, after);
                    double a0 = Measure.TipAngle(before, halfSpanM), a1 = Measure.TipAngle(after, halfSpanM);
                    double r0 = Measure.RootAngle(before, halfSpanM), r1 = Measure.RootAngle(after, halfSpanM);
                    md.AppendLine($"| {route} | {rail} | {F(c.Max * 1000, "F2")} @ {F(c.Eta * halfSpanM * 1000, "F1")} | {F(a0, "F2")} → {F(a1, "F2")} | {F(a1 - a0, "F2")} | {F(r1 - r0, "F2")} | {F(wingArea * 1e4, "F1")} → {F(area * 1e4, "F1")} |");
                    devJson.Add(new { route, rail, count = n, MaxMm = c.Max * 1000, AtMmFromRoot = c.Eta * halfSpanM * 1000, TipBefore = a0, TipAfter = a1, RootTurn = r1 - r0, AreaBefore = wingArea, AreaAfter = area });
                }
            }
        }
        json["newFoilDeviation"] = devJson;

        // 5. New foil's other three channels at 10 → 4 (Ruling 64 asks whether their defaults should move).
        md.AppendLine();
        md.AppendLine("## 5. New foil's other channels — Rebuild 10 → N, max Δ (stored unit); κ′ energy");
        md.AppendLine();
        md.AppendLine("| Channel | " + string.Join(" | ", rebuildTo.Select(n => "10 → " + n)) + " |");
        md.AppendLine("|---|" + string.Concat(rebuildTo.Select(_ => "---|")));
        var otherJson = new List<object>();
        foreach (string name in new[] { "dihedral", "twist", "thickness" })
        {
            var ch = today[name];
            var cells = rebuildTo.Select(n =>
            {
                var r = Measure.Rebuild(ch, n);
                var c = Measure.MaxChange(ch, r);
                var f = Measure.Fair(r);
                otherJson.Add(new { name, to = n, c.Max, EnergyBefore = Measure.Fair(ch).Energy, EnergyAfter = f.Energy });
                return $"{G(c.Max)}; κ′E {G(Measure.Fair(ch).Energy)} → {G(f.Energy)}";
            });
            md.AppendLine($"| {name} | " + string.Join(" | ", cells) + " |");
        }
        json["newFoilOtherChannels"] = otherJson;

        // 6. Cross-checks against the repo's own code paths.
        md.AppendLine();
        md.AppendLine("## 6. Cross-checks");
        md.AppendLine();
        md.AppendLine($"- New foil construction replayed at 10 vertices against `FoilSource.NewDefault()` parsed: {replicaBitDiffs} of 40 rail coordinates differ in bits; max |Δ| {G(replicaMismatch)} m.");
        md.AppendLine($"- Area: `WingEstimates.From(NewDefault)` {F(wingArea * 1e4, "F6")} cm²; the probe's area for the replayed 10-point rails {F(replica10.Area * 1e4, "F6")} cm² (residual {G(Math.Abs(wingArea - replica10.Area) * 1e4)} cm²).");
        var identity = Measure.MaxChange(today["trailing"], Measure.Rebuild(today["trailing"], 10));
        md.AppendLine($"- Rebuild 10 → 10 on today's New foil TE (the design's identity claim): max Δ {G(identity.Max * 1000)} mm.");
        json["crossChecks"] = new { replicaBitDiffs, replicaMismatch, wingArea, replicaArea = replica10.Area, identityMaxMm = identity.Max * 1000 };

        string outDir = Path.Combine(dir, "output");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "table.md"), md.ToString());
        File.WriteAllText(Path.Combine(outDir, "results.json"),
            JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.Write(md.ToString());
        return 0;
    }
}
