using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;

namespace XpaProbe;

// Cross-profile abscissa probe (docs/design/cross-profile-abscissa.md). Every step and measure goes through the as-built
// Core: SectionEdits.Apply, FoilSource.InsertOnce/RemoveOnce/SelectRemovable/WriteSurfaces/WriteSideTangents/
// InsertProfileKnot/DeleteProfileVertex, ProfileFair.Rebuild, SectionEdits.Refit (reflection), Geometry.Assess,
// FoilSource.MaxOrdinateDeviation and Placement.Frame. Nothing here re-implements geometry.
internal static class Program
{
    private static readonly Dictionary<string, object> Results = new();
    private static readonly StringBuilder Table = new();
    private static readonly MethodInfo RefitMethod = typeof(SectionEdits).GetMethod("Refit", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("SectionEdits.Refit not found");

    private static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(Path.Combine(root, "output"));
        Table.AppendLine("| Case | Measure | Value |").AppendLine("|---|---|---|");

        byte[] baseDoc = FoilSource.NewDefault();
        byte[] unique = FoilSource.MakeIndependent(baseDoc, "naca-0012", 0).Source;
        string rootName = Def(unique).Profiles[Def(unique).Assignments[0].Profile].Name;
        string tipName = Def(unique).Profiles[Def(unique).Assignments[1].Profile].Name;
        double rootChord = Placement.Frame(unique, 0).ChordMeters, tipChord = Placement.Frame(unique, 1).ChordMeters;
        Row("setup", "profiles", rootName + " at root, " + tipName + " at tip");
        Row("setup", "chord root / tip (m)", G(rootChord) + " / " + G(tipChord));
        var rootP = Prof(unique, rootName);
        int vertex = Enumerable.Range(2, rootP.Upper.Points.Length - 4).OrderBy(i => Math.Abs(rootP.Upper.Points[i][0] - 0.35)).First();
        string vid = rootP.Upper.Ids[vertex];
        Row("setup", "vertices per surface; chosen vertex", rootP.Upper.Points.Length + "; " + vid + " at x=" + G(rootP.Upper.Points[vertex][0]));

        // A0: Root differs from Tip by a y move only. Shared abscissa -> certified.
        byte[] a0 = Step(unique, 0, new SectionStep.Move(SurfaceSide.Upper, vid, rootP.Upper.Points[vertex][0], rootP.Upper.Points[vertex][1] + 0.005));
        Row("A0 y move on Root", "certificate", Verdict(a0));

        // A: Control -> Anchor on Root alone (the Ruling 71 defect).
        byte[] a = Step(a0, 0, new SectionStep.SetType(SurfaceSide.Upper, vid, true));
        Row("A Root Control->Anchor, Tip untouched", "certificate", Verdict(a));
        double knot = NewKnot(Prof(a0, rootName).Upper.Knots, Prof(a, rootName).Upper.Knots);
        int additions = Prof(a, rootName).Upper.Knots.Count(k => k == knot);
        Row("A", "anchor knot u*; copies inserted", G(knot) + "; " + additions);

        // B: propagate the same Boehm insertions to Tip, both surfaces.
        var tipBefore = Prof(a, tipName);
        byte[] b = PropagateInsert(a, tipName, knot, additions, out string tipAnchorId);
        var tipAfter = Prof(b, tipName);
        Row("B propagate insertion to Tip", "knots equal Root (bitwise)", Prof(b, rootName).Upper.Knots.SequenceEqual(tipAfter.Upper.Knots).ToString());
        Row("B", "control x differing from Root (count of 2N)", XDiff(Prof(b, rootName), tipAfter).ToString());
        double bDev = FoilSource.MaxOrdinateDeviation(tipBefore, tipAfter);
        Row("B", "Tip shape change (chord; um at tip chord)", G(bDev) + "; " + G(bDev * tipChord * 1e6));
        Row("B", "Tip vertices per surface", tipBefore.Upper.Points.Length + " -> " + tipAfter.Upper.Points.Length);
        Row("B", "certificate", Verdict(b));
        int ta = Array.IndexOf(tipAfter.Upper.Ids, tipAnchorId);
        Row("B", "Tip new vertex is an anchor (derived)", FoilSource.IsAnchor(tipAfter.Upper.Knots, tipAfter.Upper.Points.Length, 5, ta).ToString());
        Row("B", "Tip anchor handle-line distance upper/lower (chord)", G(LineDistance(tipAfter.Upper.Points, ta)) + " / " + G(LineDistance(tipAfter.Lower.Points, ta)));
        byte[] bRows = FoilSource.WriteSideTangents(b, tipName, SurfaceSide.Upper, [new TangentRow(tipAnchorId, "smooth", null)]);
        bRows = FoilSource.WriteSideTangents(bRows, tipName, SurfaceSide.Lower, [new TangentRow(tipAnchorId, "smooth", null)]);
        Row("B", "certificate with smooth rows on Tip's new anchor", Verdict(bRows));

        // C1: Anchor -> Control on Root, propagated to an untouched Tip.
        RemovalCase("C1 Root Anchor->Control, Tip untouched since B", bRows, rootName, tipName, vid, tipAnchorId, knot, tipChord, 0);
        // C2: the same after Tip's anchor (and handles) moved 0.3 % chord in y: Tip no longer holds the knot exactly.
        RemovalCase("C2 Root Anchor->Control, Tip anchor moved 0.3 % c", bRows, rootName, tipName, vid, tipAnchorId, knot, tipChord, 0.003);

        // D: x move on Root control, paired to Tip with and without a Tip y refit.
        foreach (double dx in new[] { 0.002, 0.01, 0.03 }) XMoveCase(a0, rootName, tipName, vertex, dx, tipChord);
        // D': the reverse direction - edit Tip (assignment 1), Root (0.127 m) is the partner. "tip chord" in these rows
        // reads "partner chord".
        foreach (double dx in new[] { 0.002, 0.01, 0.03 }) XMoveCase(a0, tipName, rootName, vertex, dx, rootChord, 1, "D' reverse x move (edit Tip, partner Root)");

        // E: Delete on Root, propagated.
        DeleteCase(a0, rootName, tipName, vertex, tipChord);

        // F: Insert (simple knot) at x = 0.5, propagated.
        {
            byte[] f = FoilSource.InsertProfileKnot(a0, rootName, 0.5).Source;
            var tb = Prof(f, tipName);
            byte[] f2 = FoilSource.InsertProfileKnot(f, tipName, 0.5).Source;
            Row("F Insert at x=0.5, Root only", "certificate", Verdict(f));
            Row("F propagated", "control x differing from Root", XDiff(Prof(f2, rootName), Prof(f2, tipName)).ToString());
            Row("F propagated", "Tip shape change (chord)", G(FoilSource.MaxOrdinateDeviation(tb, Prof(f2, tipName))));
            Row("F propagated", "certificate", Verdict(f2));
        }

        // G: SetTangent Symmetric / Angle on Root's anchor (from B with rows): within-profile and cross-profile x.
        foreach (var (kind, angle) in new (TangentKind, double?)[] { (TangentKind.Symmetric, null), (TangentKind.Angle, 10.0), (TangentKind.Horizontal, null) })
        {
            try
            {
                byte[] g = Step(bRows, 0, new SectionStep.SetTangent(SurfaceSide.Upper, vid, kind, angle, null));
                var rp = Prof(g, rootName);
                int sameSide = 0;
                for (int i = 0; i < rp.Upper.Points.Length; i++) if (rp.Upper.Points[i][0] != rp.Lower.Points[i][0]) sameSide++;
                Row("G SetTangent " + kind, "Root upper/lower x differing; Root/Tip x differing", sameSide + "; " + XDiff(rp, Prof(g, tipName)));
                Row("G SetTangent " + kind, "certificate", Verdict(g));
            }
            catch (ContractError error) { Row("G SetTangent " + kind, "refused", error.Code + " " + error.Message); }
        }

        // H: Rebuild Root to N at tolerance 1e-4 chord, then Tip rebuilt on the same basis.
        foreach (int n in new[] { 8, 10, 14 })
        {
            var rr = ProfileFair.Rebuild(Prof(a0, rootName), n, 1e-4, PreserveEnds.Position);
            var tr = ProfileFair.Rebuild(Prof(a0, tipName), n, 1e-4, PreserveEnds.Position);
            bool sameBasis = rr.Upper.Knots.SequenceEqual(tr.Upper.Knots) && rr.Upper.Points.Select(p => p[0]).SequenceEqual(tr.Upper.Points.Select(p => p[0]));
            Row("H Rebuild to " + n + " at 1e-4 c", "Root and Tip on the same basis (bitwise)", sameBasis.ToString());
            Row("H Rebuild to " + n + " at 1e-4 c", "accepted Root / Tip; deviation Root / Tip (um at tip chord)",
                rr.WithinTolerance + " / " + tr.WithinTolerance + "; " + G(rr.MaxDeviation * tipChord * 1e6) + " / " + G(tr.MaxDeviation * tipChord * 1e6));
        }

        // J: several anchors on Root, each propagated to Tip: span count, certificate and admission time.
        {
            byte[] j = a0;
            foreach (double target in new[] { 0.35, 0.6, 0.2, 0.8, 0.5 })
            {
                var rpj = Prof(j, rootName);
                int vj = Enumerable.Range(2, rpj.Upper.Points.Length - 4)
                    .Where(i => !FoilSource.IsAnchor(rpj.Upper.Knots, rpj.Upper.Points.Length, 5, i) && !FoilSource.IsAnchor(rpj.Upper.Knots, rpj.Upper.Points.Length, 5, i - 1)
                        && !FoilSource.IsAnchor(rpj.Upper.Knots, rpj.Upper.Points.Length, 5, i + 1))
                    .OrderBy(i => Math.Abs(rpj.Upper.Points[i][0] - target)).First();
                byte[] next;
                try { next = Step(j, 0, new SectionStep.SetType(SurfaceSide.Upper, rpj.Upper.Ids[vj], true)); }
                catch (ContractError error) { Row("J anchor near x=" + G(target), "refused", error.Code + " " + error.Message); break; }
                double kj = NewKnot(rpj.Upper.Knots, Prof(next, rootName).Upper.Knots);
                j = PropagateInsert(next, tipName, kj, Prof(next, rootName).Upper.Knots.Count(k => k == kj), out _);
                var pj = Prof(j, rootName);
                int spans = pj.Upper.Knots.Where(k => k > 0 && k < 1).Distinct().Count() + 1;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string verdict = "";
                var times = new List<double>();
                for (int rep = 0; rep < 3; rep++) { watch.Restart(); verdict = Verdict(j); times.Add(watch.Elapsed.TotalMilliseconds); }
                times.Sort();
                Row("J anchor near x=" + G(target), "vertices; spans; certificate; admission ms (median of 3)",
                    pj.Upper.Points.Length + "; " + spans + "; " + verdict + "; " + G(times[1]));
            }
        }

        // L: compatible fit (the coordinator's primary option). Each station profile keeps its authored points; a derived
        // copy of the non-basis profile is fitted onto the basis profile's spacing with the as-built DatImport.FitToBasis,
        // its deviation from the authored section measured, and the compatible document certified.
        CompatibleSection(unique, a, rootName, tipName, rootChord, tipChord);

        // I: chain Root(A) - Mid(B) - Tip(C): an anchor on Mid propagated to both neighbours.
        ChainCase();

        // K: the Example foil (docs/examples/foildsl/foil-basic.foil): Make unique to Root, y move, then anchors on Root
        // propagated to Tip, as built.
        ExampleCase(FindUp("docs/examples/foildsl/foil-basic.foil"));

        // args[1] names a variant run (the budget-patched Core copy): "-budget-variant".
        string suffix = args.Length > 1 ? args[1] : "";
        string front = "---\nid: proof-cross-profile-abscissa-table" + suffix + "\n" +
            "title: \"XPA probe — generated table" + (suffix.Length > 0 ? " (" + suffix.TrimStart('-') + ")" : " (as-built Core)") + " (do not edit; re-run the probe)\"\n" +
            "type: proof-pack\nstatus: in-review\nowner: \"@timianmalloo\"\nphase: design\ntags: [xpa, abscissa, probe, generated]\n" +
            "links:\n  - { to: proof-cross-profile-abscissa, rel: depends-on }\nreview-by: 2026-11-30\n" +
            "summary: >-\n  Generated by docs/proof/cross-profile-abscissa/probe. Timings vary by machine; every other row is deterministic.\n" +
            "review-suggested: []\n---\n\n";
        File.WriteAllText(Path.Combine(root, "output", "table" + suffix + ".md"), front + Table.ToString());
        File.WriteAllText(Path.Combine(root, "output", "results" + suffix + ".json"), JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.Write(Table.ToString());
        return 0;
    }

    private static void RemovalCase(string name, byte[] start, string rootName, string tipName, string vid, string tipAnchorId, double knot, double tipChord, double tipShift)
    {
        byte[] s = start;
        if (tipShift != 0)
        {
            var t = Prof(s, tipName);
            int ai = Array.IndexOf(t.Upper.Ids, tipAnchorId);
            var up = Copy(t.Upper.Points);
            for (int o = -1; o <= 1; o++) up[ai + o][1] += tipShift;
            s = FoilSource.WriteSurfaces(s, tipName, t.Upper.Knots, up, t.Upper.Ids, Copy(t.Lower.Points), t.Lower.Ids);
        }
        byte[] c = Step(s, 0, new SectionStep.SetType(SurfaceSide.Upper, vid, false));
        Row(name, "certificate, Root only", Verdict(c));
        // Tip: strip its rows on that anchor, then the same two removals on both surfaces.
        var tip = Prof(c, tipName);
        byte[] stripped = FoilSource.WriteSideTangents(c, tipName, SurfaceSide.Upper, tip.Upper.Tangents.Where(r => r.Id != tipAnchorId).ToArray());
        stripped = FoilSource.WriteSideTangents(stripped, tipName, SurfaceSide.Lower, tip.Lower.Tangents.Where(r => r.Id != tipAnchorId).ToArray());
        tip = Prof(stripped, tipName);
        int index = Array.IndexOf(tip.Upper.Ids, tipAnchorId);
        double left = Bound(tip.Upper, index, knot, true), right = Bound(tip.Upper, index, knot, false);
        double[] knots = tip.Upper.Knots; var up2 = Copy(tip.Upper.Points); var lo2 = Copy(tip.Lower.Points);
        var ids = tip.Upper.Ids.ToList();
        for (int pass = 0; pass < 2; pass++)
        {
            int r = FoilSource.SelectRemovable(knots, 5, knot);
            var u = FoilSource.RemoveOnce(knots, up2, 5, r); var l = FoilSource.RemoveOnce(knots, lo2, 5, r);
            knots = u.Knots; up2 = u.Points; lo2 = l.Points; ids.RemoveAt(u.Removed);
        }
        var removedUpper = tip.Upper with { Knots = knots, Points = up2 };
        var removedLower = tip.Lower with { Knots = knots, Points = lo2 };
        double rawU = Dev(tip.Upper, removedUpper, 0, 1), rawL = Dev(tip.Lower, removedLower, 0, 1);
        Row(name, "Tip x differing from Root after the same removal", XDiffPoints(Prof(c, rootName), knots, up2, lo2).ToString());
        Row(name, "Tip raw removal deviation upper/lower (um at tip chord)", G(rawU * tipChord * 1e6) + " / " + G(rawL * tipChord * 1e6));
        var fu = (double[][])RefitMethod.Invoke(null, [tip.Upper, knots, up2, left, right])!;
        var fl = (double[][])RefitMethod.Invoke(null, [tip.Lower, knots, lo2, left, right])!;
        double refU = Dev(tip.Upper, tip.Upper with { Knots = knots, Points = fu }, 0, 1);
        double refL = Dev(tip.Lower, tip.Lower with { Knots = knots, Points = fl }, 0, 1);
        Row(name, "Tip refit deviation upper/lower (um at tip chord); 10 um rule", G(refU * tipChord * 1e6) + " / " + G(refL * tipChord * 1e6) + "; " + (Math.Max(refU, refL) * tipChord <= 10e-6 ? "within" : "refuse"));
        byte[] done = FoilSource.WriteSurfaces(stripped, tipName, knots, fu, ids.ToArray(), fl, ids.ToArray());
        Row(name, "certificate after Tip refit", Verdict(done));
        Row(name, "placed-surface change from the Tip refit alone, max over eta 0..1 (um); at eta (1)", PlacedChange(stripped, done));
    }

    // The partner's effect on the evaluated wing: the same Root, Tip before vs after its follow, compared on the placed
    // Rule A sections (Placement.Sections: chord-normalised camber and thickness) times the local chord, at 11 eta.
    private static string PlacedChange(byte[] before, byte[] after)
    {
        var etas = Enumerable.Range(0, 11).Select(i => i / 10.0).ToArray();
        var xs = Enumerable.Range(0, 201).Select(i => (1 - Math.Cos(Math.PI * i / 200.0)) / 2).ToArray();
        var a = Placement.Sections(before, etas, xs, CancellationToken.None);
        var b = Placement.Sections(after, etas, xs, CancellationToken.None);
        double worst = 0, worstEta = 0;
        for (int e = 0; e < etas.Length; e++)
        {
            double chord = a[e].Frame.ChordMeters;
            for (int i = 0; i < xs.Length; i++)
            {
                double dc = b[e].Camber[i] - a[e].Camber[i], dt = (b[e].Thickness[i] - a[e].Thickness[i]) / 2;
                double change = Math.Max(Math.Abs(dc + dt), Math.Abs(dc - dt)) * chord * 1e6;
                if (change > worst) (worst, worstEta) = (change, etas[e]);
            }
        }
        return G(worst) + " (" + G(worstEta) + ")";
    }

    private static void XMoveCase(byte[] start, string rootName, string tipName, int vertex, double dx, double tipChord, int assignment = 0, string label = "D x move")
    {
        string name = label + " dx=" + G(dx);
        var rp = Prof(start, rootName);
        double x = rp.Upper.Points[vertex][0] + dx;
        byte[] d;
        try { d = Step(start, assignment, new SectionStep.Move(SurfaceSide.Upper, rp.Upper.Ids[vertex], x, rp.Upper.Points[vertex][1])); }
        catch (ContractError error) { Row(name, "refused", error.Code); return; }
        Row(name, "certificate, Root only", Verdict(d));
        var tip = Prof(d, tipName);
        var up = Copy(tip.Upper.Points); var lo = Copy(tip.Lower.Points);
        up[vertex][0] = x; lo[vertex][0] = x;
        var movedU = tip.Upper with { Points = up }; var movedL = tip.Lower with { Points = lo };
        double raw = Math.Max(Dev(tip.Upper, movedU, 0, 1), Dev(tip.Lower, movedL, 0, 1));
        Row(name, "Tip x paired, no refit: deviation (um at tip chord)", G(raw * tipChord * 1e6));
        double xl = up[Math.Max(1, vertex - 3)][0], xr = up[Math.Min(up.Length - 2, vertex + 3)][0];
        var fu = (double[][])RefitMethod.Invoke(null, [tip.Upper, tip.Upper.Knots, up, xl, xr])!;
        var fl = (double[][])RefitMethod.Invoke(null, [tip.Lower, tip.Lower.Knots, lo, xl, xr])!;
        double local = Math.Max(Dev(tip.Upper, tip.Upper with { Points = fu }, 0, 1), Dev(tip.Lower, tip.Lower with { Points = fl }, 0, 1));
        Row(name, "Tip y refit on x-neighbours +-3: deviation (um at tip chord); 10 um rule", G(local * tipChord * 1e6) + "; " + (local * tipChord <= 10e-6 ? "within" : "refuse"));
        var gu = (double[][])RefitMethod.Invoke(null, [tip.Upper, tip.Upper.Knots, up, 0.0, 1.0])!;
        var gl = (double[][])RefitMethod.Invoke(null, [tip.Lower, tip.Lower.Knots, lo, 0.0, 1.0])!;
        double global = Math.Max(Dev(tip.Upper, tip.Upper with { Points = gu }, 0, 1), Dev(tip.Lower, tip.Lower with { Points = gl }, 0, 1));
        Row(name, "Tip y refit, whole surface free: deviation (um at tip chord)", G(global * tipChord * 1e6));
        byte[] done = FoilSource.WriteSurfaces(d, tipName, tip.Upper.Knots, fu, tip.Upper.Ids, fl, tip.Lower.Ids);
        Row(name, "certificate after paired move + local refit", Verdict(done));
        Row(name, "placed-surface change from the partner refit alone, max over eta 0..1 (um); at eta (1)", PlacedChange(d, done));
    }

    private static void DeleteCase(byte[] start, string rootName, string tipName, int vertex, double tipChord)
    {
        var rp = Prof(start, rootName);
        byte[] e = FoilSource.DeleteProfileVertex(start, rootName, vertex).Source;
        Row("E Delete on Root only", "certificate", Verdict(e));
        var tb = Prof(e, tipName);
        byte[] e2 = FoilSource.DeleteProfileVertex(e, tipName, vertex).Source;
        var ta = Prof(e2, tipName);
        Row("E propagated", "Tip x differing from Root", XDiff(Prof(e2, rootName), ta).ToString());
        double raw = FoilSource.MaxOrdinateDeviation(tb, ta);
        Row("E propagated", "Tip raw removal deviation (um at tip chord)", G(raw * tipChord * 1e6));
        var fu = (double[][])RefitMethod.Invoke(null, [tb.Upper, ta.Upper.Knots, Copy(ta.Upper.Points), 0.0, 1.0])!;
        var fl = (double[][])RefitMethod.Invoke(null, [tb.Lower, ta.Lower.Knots, Copy(ta.Lower.Points), 0.0, 1.0])!;
        double refit = Math.Max(Dev(tb.Upper, ta.Upper with { Points = fu }, 0, 1), Dev(tb.Lower, ta.Lower with { Points = fl }, 0, 1));
        Row("E propagated", "Tip refit deviation (um at tip chord); 10 um rule", G(refit * tipChord * 1e6) + "; " + (refit * tipChord <= 10e-6 ? "within" : "refuse"));
        Row("E Root itself", "Root's own Delete change (um at root chord)", G(FoilSource.MaxOrdinateDeviation(rp, Prof(e, rootName)) * Placement.Frame(start, 0).ChordMeters * 1e6));
    }

    private static void ChainCase()
    {
        string text = Encoding.UTF8.GetString(FoilSource.NewDefault()).Replace(
            "sections { at root profile \"naca-0012\" at tip profile \"naca-0012\" }",
            "sections { at root profile \"naca-0012\" at 50 % profile \"naca-0012\" at tip profile \"naca-0012\" }", StringComparison.Ordinal);
        byte[] s = Encoding.UTF8.GetBytes(text);
        s = FoilSource.MakeIndependent(s, "naca-0012", 1).Source;
        s = FoilSource.MakeIndependent(s, "naca-0012", 2).Source;
        var def = Def(s);
        string[] names = def.Assignments.Select(a => def.Profiles[a.Profile].Name).ToArray();
        Row("I chain", "profiles at root / 50 % / tip", string.Join(" / ", names));
        var mid = Prof(s, names[1]);
        int v = Enumerable.Range(2, mid.Upper.Points.Length - 4).OrderBy(i => Math.Abs(mid.Upper.Points[i][0] - 0.35)).First();
        byte[] m0 = Step(s, 1, new SectionStep.Move(SurfaceSide.Upper, mid.Upper.Ids[v], mid.Upper.Points[v][0], mid.Upper.Points[v][1] + 0.004));
        byte[] m1 = Step(m0, 1, new SectionStep.SetType(SurfaceSide.Upper, mid.Upper.Ids[v], true));
        Row("I chain", "Mid Control->Anchor alone: certificate", Verdict(m1));
        double knot = NewKnot(Prof(m0, names[1]).Upper.Knots, Prof(m1, names[1]).Upper.Knots);
        int copies = Prof(m1, names[1]).Upper.Knots.Count(k => k == knot);
        byte[] m2 = PropagateInsert(m1, names[0], knot, copies, out _);
        Row("I chain", "propagated to Root only: certificate", Verdict(m2));
        byte[] m3 = PropagateInsert(m2, names[2], knot, copies, out _);
        Row("I chain", "propagated to Root and Tip: certificate", Verdict(m3));
        Row("I chain", "max Root/Tip shape change (chord)", G(Math.Max(FoilSource.MaxOrdinateDeviation(Prof(m1, names[0]), Prof(m3, names[0])),
            FoilSource.MaxOrdinateDeviation(Prof(m1, names[2]), Prof(m3, names[2])))));
    }

    private static readonly MethodInfo SqrtBasisMethod = typeof(FoilSource).GetMethod("SqrtProfileBasis", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("FoilSource.SqrtProfileBasis not found");

    private static void CompatibleSection(byte[] unique, byte[] rootWithAnchor, string rootName, string tipName, double rootChord, double tipChord)
    {
        // Authored Tip6: the Example foil's section-a, fitted once onto its own 6-vertex sqrt spacing (single span).
        byte[] example = FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(FindUp("docs/examples/foildsl/foil-basic.foil"))));
        var sectionA = Def(example).Profiles[0];
        var (k6, x6) = ((double[], double[]))SqrtBasisMethod.Invoke(null, [6])!;
        var tip6 = Prof(Splice(unique, tipName, DatImport.FitToBasis(Dat(sectionA), tipName, k6, x6, 5)!.ProfileBlock), tipName);
        byte[] p1 = FoilSource.WriteSurfaces(unique, tipName, tip6.Upper.Knots, Copy(tip6.Upper.Points), Ids(6), Copy(tip6.Lower.Points), Ids(6));
        Row("L setup", "Tip6 authored: vertices; spans; certificate of Root10 + Tip6 as authored",
            Prof(p1, tipName).Upper.Points.Length + "; 1; " + Verdict(p1));
        // P1: basis Root (NACA 0012, 10 vertices, 5 spans); Tip6 fitted onto it. And the reverse (Root onto Tip6's basis).
        Compatible("L P1 Root10 basis, Tip6 fitted", p1, rootName, tipName, tipChord, rootChord);
        Compatible("L P1r Tip6 basis, Root10 fitted (why the richer basis)", p1, tipName, rootName, rootChord, tipChord);
        // P2: Root = section-a (8 vertices, its own spacing) vs Tip = NACA 0012 10: basis Tip, Root fitted.
        var sa = Prof(Splice(unique, rootName, DatImport.FitToBasis(Dat(sectionA), rootName, sectionA.Upper.Knots, sectionA.Upper.Points.Select(p => p[0]).ToArray(), 5)!.ProfileBlock), rootName);
        byte[] p2 = FoilSource.WriteSurfaces(unique, rootName, sectionA.Upper.Knots, Copy(sectionA.Upper.Points), Ids(sectionA.Upper.Points.Length), Copy(sectionA.Lower.Points), Ids(sectionA.Upper.Points.Length));
        _ = sa;
        Compatible("L P2 Tip NACA10 basis, Root section-a 8 fitted", p2, tipName, rootName, rootChord, tipChord);
        // P3: Root NACA10 with an anchor at 0.58 (15 vertices, 6 spans) as the basis; Tip6 fitted.
        var ra = Prof(rootWithAnchor, rootName);
        byte[] p3 = FoilSource.WriteSurfaces(rootWithAnchor, tipName, tip6.Upper.Knots, Copy(tip6.Upper.Points), Ids(6), Copy(tip6.Lower.Points), Ids(6));
        _ = ra;
        Compatible("L P3 Root10+anchor basis, Tip6 fitted", p3, rootName, tipName, tipChord, rootChord);
        // P4: Tip6 with an anchor at x=0.3 (11 vertices); the basis is Root10 with Tip's anchor x inserted (Boehm, exact
        // on Root); Tip fitted onto it.
        var t1 = Prof(p1, tipName);
        int va = Enumerable.Range(2, t1.Upper.Points.Length - 4).OrderBy(i => Math.Abs(t1.Upper.Points[i][0] - 0.3)).First();
        byte[] p4 = Step(p1, 1, new SectionStep.SetType(SurfaceSide.Upper, t1.Upper.Ids[va], true));
        var t4 = Prof(p4, tipName);
        double xa = t4.Upper.Points[Enumerable.Range(5, t4.Upper.Points.Length - 10).First(i => FoilSource.IsAnchor(t4.Upper.Knots, t4.Upper.Points.Length, 5, i))][0];
        double u = ProfileEvaluator.ParameterFor(Prof(p4, rootName).Upper, xa);
        var rootBefore = Prof(p4, rootName);
        byte[] p4b = PropagateInsert(p4, rootName, u, 5, out _);
        Row("L P4 Tip6+anchor at x=" + G(xa) + "; Root basis + that anchor", "Root change from the inserted anchor (chord)",
            G(FoilSource.MaxOrdinateDeviation(rootBefore, Prof(p4b, rootName))));
        Compatible("L P4 Root10+Tip's anchor basis, Tip6+anchor fitted", p4b, rootName, tipName, tipChord, rootChord);
        Compatible("L P4n Root10 basis without Tip's anchor, Tip6+anchor fitted", p4, rootName, tipName, tipChord, rootChord);
    }

    // Fits `fitted`'s authored shape onto `basis`'s spacing; reports the deviation, vertices, spans, certificate and cost.
    private static void Compatible(string name, byte[] doc, string basis, string fitted, double fittedChord, double otherChord)
    {
        var b = Prof(doc, basis);
        var authored = Prof(doc, fitted);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var fit = DatImport.FitToBasis(Dat(authored), fitted, b.Upper.Knots, b.Upper.Points.Select(p => p[0]).ToArray(), 5);
        if (fit is null) { Row(name, "fit", "FitToBasis returned null"); return; }
        byte[] compatible = Splice(doc, fitted, fit.ProfileBlock);
        double deviation = FoilSource.MaxOrdinateDeviation(authored, Prof(compatible, fitted));
        double fitMs = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        string verdict = Verdict(compatible);
        double assessMs = watch.Elapsed.TotalMilliseconds;
        int spans = b.Upper.Knots.Where(k => k > 0 && k < 1).Distinct().Count() + 1;
        Row(name, "basis vertices, spans; fitted profile deviation from authored (chord; um at 12.7 mm / 127 mm); 10 um at 127 mm",
            b.Upper.Points.Length + ", " + spans + "; " + G(deviation) + "; " + G(deviation * 0.0127e6) + " / " + G(deviation * 0.127e6) + "; " + (deviation * 0.127 <= 10e-6 ? "within" : "over"));
        Row(name, "certificate of the compatible pair; fit+measure ms; assess ms", verdict + "; " + G(fitMs) + "; " + G(assessMs));
        // Surface level: Rule A on the authored profiles at equal chord fraction (what Placement draws from the record
        // today) vs Rule A on the compatible pair, at 11 eta, in metres at the probe's chords.
        Row(name, "placed-surface difference authored vs compatible, max over eta 0..1 (um); at eta", PlacedChange(doc, compatible));
        _ = fittedChord; _ = otherChord;
    }

    private static DatProfile Dat(ProfileDefinition p)
    {
        var xs = Enumerable.Range(0, 161).Select(i => (1 - Math.Cos(Math.PI * i / 160.0)) / 2).ToArray();
        var upper = xs.Select(x => new ProfilePoint(x, x == 0 ? 0 : ProfileEvaluator.OrdinateAt(p.Upper, x))).ToList();
        var lower = xs.Select(x => new ProfilePoint(x, x == 0 ? 0 : ProfileEvaluator.OrdinateAt(p.Lower, x))).ToList();
        return new DatProfile(p.Name, "probe-sampled", upper, lower, Encoding.UTF8.GetBytes(p.Name), 2 * xs.Length - 1);
    }

    private static byte[] Splice(byte[] doc, string profile, string block)
    {
        var p = Prof(doc, profile);
        string text = Encoding.UTF8.GetString(doc);
        return Encoding.UTF8.GetBytes(text[..p.BlockStart] + block + text[p.BlockEnd..]);
    }

    private static string[] Ids(int n) => Enumerable.Range(0, n).Select(i => "cv-" + i.ToString(CultureInfo.InvariantCulture)).ToArray();

    // The repo file, found from the probe binary or the working directory upward, so any output directory works.
    private static string FindUp(string relative)
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, relative))) return Path.Combine(dir.FullName, relative);
        throw new FileNotFoundException(relative);
    }

    private static void ExampleCase(string path)
    {
        byte[] s = FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(path)));
        var def = Def(s);
        string shared = def.Profiles[def.Assignments[0].Profile].Name;
        s = FoilSource.MakeIndependent(s, shared, 0).Source;
        def = Def(s);
        string rootName = def.Profiles[def.Assignments[0].Profile].Name, tipName = def.Profiles[def.Assignments[^1].Profile].Name;
        var rp = Prof(s, rootName);
        int spans0 = rp.Upper.Knots.Where(k => k > 0 && k < 1).Distinct().Count() + 1;
        Row("K Example foil", "profiles; vertices; spans", rootName + " / " + tipName + "; " + rp.Upper.Points.Length + "; " + spans0);
        int v = Enumerable.Range(2, rp.Upper.Points.Length - 4).OrderBy(i => Math.Abs(rp.Upper.Points[i][0] - 0.35)).First();
        byte[] j = Step(s, 0, new SectionStep.Move(SurfaceSide.Upper, rp.Upper.Ids[v], rp.Upper.Points[v][0], rp.Upper.Points[v][1] + 0.002));
        Row("K Example foil", "y move on Root: certificate", Verdict(j));
        foreach (double target in new[] { 0.35, 0.65, 0.2 })
        {
            var p = Prof(j, rootName);
            int vj = Enumerable.Range(2, p.Upper.Points.Length - 4)
                .Where(i => !FoilSource.IsAnchor(p.Upper.Knots, p.Upper.Points.Length, 5, i) && !FoilSource.IsAnchor(p.Upper.Knots, p.Upper.Points.Length, 5, i - 1)
                    && !FoilSource.IsAnchor(p.Upper.Knots, p.Upper.Points.Length, 5, i + 1))
                .OrderBy(i => Math.Abs(p.Upper.Points[i][0] - target)).First();
            byte[] next;
            try { next = Step(j, 0, new SectionStep.SetType(SurfaceSide.Upper, p.Upper.Ids[vj], true)); }
            catch (ContractError error) { Row("K Example anchor near x=" + G(target), "refused", error.Code + " " + error.Message); break; }
            string alone = Verdict(next);
            double kj = NewKnot(p.Upper.Knots, Prof(next, rootName).Upper.Knots);
            var tipBefore = Prof(next, tipName);
            j = PropagateInsert(next, tipName, kj, Prof(next, rootName).Upper.Knots.Count(k => k == kj), out _);
            var pj = Prof(j, rootName);
            int spans = pj.Upper.Knots.Where(k => k > 0 && k < 1).Distinct().Count() + 1;
            Row("K Example anchor near x=" + G(target), "Root alone; propagated: vertices, spans, Tip change (chord), certificate",
                alone + "; " + pj.Upper.Points.Length + ", " + spans + ", " + G(FoilSource.MaxOrdinateDeviation(tipBefore, Prof(j, tipName))) + ", " + Verdict(j));
        }
    }

    private static byte[] PropagateInsert(byte[] bytes, string profile, double knot, int times, out string anchorId)
    {
        var p = Prof(bytes, profile);
        double[] knots = p.Upper.Knots; var up = Copy(p.Upper.Points); var lo = Copy(p.Lower.Points);
        var ids = p.Upper.Ids.ToList();
        int next = ids.Where(i => i.StartsWith("cv-", StringComparison.Ordinal)).Select(i => int.Parse(i[3..], CultureInfo.InvariantCulture)).DefaultIfEmpty(-1).Max() + 1;
        for (int turn = 0; turn < times; turn++)
        {
            var u = FoilSource.InsertOnce(knots, up, 5, knot); var l = FoilSource.InsertOnce(knots, lo, 5, knot);
            if (u.Inserted != l.Inserted) throw new InvalidOperationException("paired insert index differs");
            knots = u.Knots; up = u.Points; lo = l.Points; ids.Insert(u.Inserted, "cv-" + (next++).ToString(CultureInfo.InvariantCulture));
        }
        int a = Enumerable.Range(5, up.Length - 10).First(i => FoilSource.IsAnchor(knots, up.Length, 5, i) && knots[i + 1] == knot);
        anchorId = ids[a];
        return FoilSource.WriteSurfaces(bytes, profile, knots, up, ids.ToArray(), lo, ids.ToArray());
    }

    private static double Bound(Curve c, int anchor, double knot, bool left)
    {
        double bound = left ? 0 : 1;
        for (int i = 5; i <= c.Points.Length - 6; i++)
        {
            if (i == anchor || !FoilSource.IsAnchor(c.Knots, c.Points.Length, 5, i)) continue;
            double k = c.Knots[i + 1];
            if (left && k < knot && c.Points[i][0] > bound) bound = c.Points[i][0];
            if (!left && k > knot && c.Points[i][0] < bound) bound = c.Points[i][0];
        }
        return bound;
    }

    private static double Dev(Curve before, Curve after, double x0, double x1)
    {
        double worst = 0;
        for (int s = 0; s <= 2000; s++)
        {
            double x = Math.Clamp(x0 + (x1 - x0) * s / 2000.0, 0, 1);
            worst = Math.Max(worst, Math.Abs(ProfileEvaluator.OrdinateAt(before, x) - ProfileEvaluator.OrdinateAt(after, x)));
        }
        return worst;
    }

    private static double LineDistance(double[][] p, int i)
    {
        double sx = p[i + 1][0] - p[i - 1][0], sy = p[i + 1][1] - p[i - 1][1];
        return Math.Abs(sx * (p[i][1] - p[i - 1][1]) - sy * (p[i][0] - p[i - 1][0])) / Math.Sqrt(sx * sx + sy * sy);
    }

    private static double NewKnot(double[] before, double[] after) =>
        after.Distinct().First(k => after.Count(v => v == k) != before.Count(v => v == k));

    private static int XDiff(ProfileDefinition a, ProfileDefinition b) =>
        a.Upper.Knots.SequenceEqual(b.Upper.Knots) ? XDiffPoints(a, b.Upper.Knots, b.Upper.Points, b.Lower.Points) : -1;

    private static int XDiffPoints(ProfileDefinition a, double[] knots, double[][] up, double[][] lo)
    {
        if (!a.Upper.Knots.SequenceEqual(knots) || a.Upper.Points.Length != up.Length) return -1;
        int n = 0;
        for (int i = 0; i < up.Length; i++)
        {
            if (BitConverter.DoubleToInt64Bits(a.Upper.Points[i][0]) != BitConverter.DoubleToInt64Bits(up[i][0])) n++;
            if (BitConverter.DoubleToInt64Bits(a.Lower.Points[i][0]) != BitConverter.DoubleToInt64Bits(lo[i][0])) n++;
        }
        return n;
    }

    private static byte[] Step(byte[] bytes, int assignment, SectionStep step) => SectionEdits.Apply(bytes, assignment, step).Bytes;
    private static Definition Def(byte[] bytes) => FoilSource.Parse(bytes).Definition!;
    private static ProfileDefinition Prof(byte[] bytes, string name) => Def(bytes).Profiles.Single(p => p.Name == name);
    private static double[][] Copy(double[][] p) => p.Select(q => new[] { q[0], q[1] }).ToArray();

    private static string Verdict(byte[] bytes)
    {
        var assessment = Geometry.Assess(FoilSource.Parse(bytes));
        return assessment.Status + (assessment.Status == GeometryStatus.Certified ? "" : " - " + assessment.Reason);
    }

    private static string G(double value) => value.ToString("G4", CultureInfo.InvariantCulture);

    private static void Row(string scenario, string measure, string value)
    {
        Results[scenario + " | " + measure] = value;
        Table.Append("| ").Append(scenario).Append(" | ").Append(measure).Append(" | ").Append(value.Replace("|", "\\|")).AppendLine(" |");
    }
}
