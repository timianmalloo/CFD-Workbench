using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;

namespace ReplaceProbe;

// M1.2d Replace-from-catalog probe (docs/design/m12d-catalog.md §3.6). Measures, through the as-built Core only:
// the residual of a catalog shape fitted onto a station's existing basis (DatImport.FitToBasis), the own-basis vertex
// count (DatImport.Fit), the as-built Import step's basis choice (AuthoringSession.ImportPatch, by reflection) and the
// certificate after each Replace option (Geometry.Assess). NACA coordinates come from the 4- and 5-digit closed forms
// (Abbott & von Doenhoff), 81 cosine-spaced stations per surface, standard open trailing edge.
internal static class Program
{
    private static readonly StringBuilder Table = new();
    private static readonly Dictionary<string, object> Results = new();
    private static readonly MethodInfo ImportPatch = typeof(AuthoringSession).GetMethod("ImportPatch", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("AuthoringSession.ImportPatch not found");
    private static readonly MethodInfo OwnSqrtBasis = typeof(DatImport).GetMethod("OwnSqrtBasis", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("DatImport.OwnSqrtBasis not found");
    private static readonly MethodInfo FormatNumber = typeof(DatImport).GetMethod("FormatNumber", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("DatImport.FormatNumber not found");

    private const double ExampleChordMeters = 0.120;
    private const double Acceptance = 10e-6; // A4.6: 10 um at the local chord

    private static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : ".";
        string repo = args.Length > 1 ? args[1] : "../../../..";
        Directory.CreateDirectory(Path.Combine(root, "output"));
        Table.AppendLine("| Case | Measure | Value |").AppendLine("|---|---|---|");

        byte[] example40 = File.ReadAllBytes(Path.Combine(repo, "src/CfdWorkbench.Desktop/Assets/example.foil"));
        string asBuilt40;
        try { Import(example40, 0, Naca("4412")); asBuilt40 = "applied"; }
        catch (TargetInvocationException error) { asBuilt40 = "refused " + (error.InnerException as ContractError)?.Code; }
        Row("setup", "as-built Import step on the Example as shipped (FoilDSL 4.0 header)", asBuilt40);
        byte[] example = FoilSource.EnsureHeader41(example40);
        Row("setup", "every case below uses the Example with its header raised to 4.1 (FoilSource.EnsureHeader41)", "yes");
        byte[] newFoil = FoilSource.NewDefault();
        var sectionA = Prof(example, "section-a");
        var naca12 = Def(newFoil).Profiles[Def(newFoil).Assignments[0].Profile];
        Row("setup", "Example: profile, vertices per surface, stations", sectionA.Name + ", " + sectionA.Upper.Points.Length + ", " + Def(example).Assignments.Length + " (root and tip share it)");
        Row("setup", "New foil: profile, vertices per surface", naca12.Name + ", " + naca12.Upper.Points.Length + " (sqrt basis)");
        Row("setup", "Example chord (m); 10 um in chord fractions", G(ExampleChordMeters) + "; " + G(Acceptance / ExampleChordMeters));

        // The Example basis refined to 10 vertices by exact knot insertion (both surfaces; the shape is unchanged).
        byte[] refined = FoilSource.InsertProfileKnot(example, "section-a", 0.04).Source;
        refined = FoilSource.InsertProfileKnot(refined, "section-a", 0.5).Source;
        var sectionA10 = Prof(refined, "section-a");
        Row("setup", "Example basis refined by insertion at x = 0.04, 0.5: vertices; shape change (chord)",
            sectionA10.Upper.Points.Length + "; " + G(FoilSource.MaxOrdinateDeviation(sectionA, sectionA10)));

        var bases = new (string Name, double[] Knots, double[] X)[]
        {
            ("Example section-a (8)", sectionA.Upper.Knots, sectionA.Upper.Points.Select(p => p[0]).ToArray()),
            ("Example refined (10)", sectionA10.Upper.Knots, sectionA10.Upper.Points.Select(p => p[0]).ToArray()),
            ("New foil sqrt (10)", naca12.Upper.Knots, naca12.Upper.Points.Select(p => p[0]).ToArray()),
        };

        foreach (string code in new[] { "0009", "0012", "2412", "4412", "23012" })
        {
            var dat = DatImport.Parse(Naca(code));
            var fits = new Dictionary<string, object>();
            foreach (var basis in bases)
            {
                var fit = DatImport.FitToBasis(dat, "probe", basis.Knots, basis.X, 5);
                double r = fit?.MaxResidual ?? double.NaN;
                Row("NACA " + code, "fit on " + basis.Name + ": residual chord; um at 120 mm; within 10 um", G(r) + "; " + G(r * ExampleChordMeters * 1e6) + "; " + (r * ExampleChordMeters <= Acceptance ? "yes" : "NO"));
                fits[basis.Name] = r;
            }
            var own = DatImport.Fit(dat, "probe");
            Row("NACA " + code, "own sqrt basis (as built, stops at 1e-5): vertices; residual", own.VertexCount + "; " + G(own.MaxResidual));
            int within = -1;
            for (int n = 8; n <= 16 && within < 0; n++)
            {
                var (k, x) = Sqrt(n);
                var fit = DatImport.FitToBasis(dat, "probe", k, x, 5);
                if (fit is not null && fit.MaxResidual * ExampleChordMeters <= Acceptance) within = n;
            }
            Row("NACA " + code, "smallest own sqrt basis within 10 um at 120 mm", within.ToString(CultureInfo.InvariantCulture));
            fits["own"] = own.MaxResidual; fits["ownVertices"] = own.VertexCount; fits["ownWithin10umAt120mm"] = within;
            Results["naca" + code] = fits;
        }

        // E1. Example, shared section (root and tip): the as-built Import at root only, then also at tip.
        byte[] dat4412 = Naca("4412");
        var (e1a, e1aReport) = Import(example, 0, dat4412);
        Row("E1 Example shared, Import 4412 at root only (as built)", "basis used; residual; certificate", e1aReport.Basis + "; " + G(e1aReport.MaxResidual) + "; " + Verdict(e1a));
        try
        {
            var (e1b, e1bReport) = Import(e1a, 1, dat4412);
            Row("E1 then Import 4412 at tip too (as built)", "basis used; residual; certificate", e1bReport.Basis + "; " + G(e1bReport.MaxResidual) + "; " + Verdict(e1b));
        }
        catch (TargetInvocationException error)
        {
            Row("E1 then Import 4412 at tip too (as built)", "outcome", "refused " + (error.InnerException as ContractError)?.Code + " (section-a would be left unreferenced)");
        }
        // E1c. Replace in place at every station sharing the profile: the shared profile's block rewritten on the own basis.
        var own4412 = DatImport.Fit(DatImport.Parse(dat4412), "section-a");
        byte[] e1c = WriteProfile(example, "section-a", own4412.ProfileBlock.Replace("profile \"section-a\"", "profile \"probe\""));
        Row("E1c Replace shared in place (root and tip), own basis", "vertices; residual um at 120 mm; certificate", own4412.VertexCount + "; " + G(own4412.MaxResidual * ExampleChordMeters * 1e6) + "; " + Verdict(e1c));

        // E2. Example after Make unique to Root: the as-built Import at root (neighbour = tip's section-a).
        byte[] unique = FoilSource.MakeIndependent(example, "section-a", 0).Source;
        string rootName = Def(unique).Profiles[Def(unique).Assignments[0].Profile].Name;
        Row("E2 Example, Make unique to Root, Import 4412 at root (as built)", "outcome", TryImport(unique, 0, dat4412, 0.120));

        // E3. Option A forced: 4412 fitted on the shared (section-a) basis, written at root whatever the residual.
        var a = DatImport.FitToBasis(DatImport.Parse(dat4412), rootName, bases[0].Knots, bases[0].X, 5)!;
        byte[] e3 = WriteProfile(unique, rootName, a.ProfileBlock);
        Row("E3 Option A: 4412 on the shared basis at root", "residual um at 120 mm; certificate", G(a.MaxResidual * ExampleChordMeters * 1e6) + "; " + Verdict(e3));

        // E4. Option C: 4412 on its own sqrt-n basis at root; tip's section-a refitted onto the same basis.
        foreach (int n in new[] { 10, 12 })
        {
            var (k, x) = Sqrt(n);
            var fitRoot = DatImport.FitToBasis(DatImport.Parse(dat4412), rootName, k, x, 5)!;
            var sampled = Sample(Prof(unique, "section-a"));
            var fitTip = DatImport.FitToBasis(sampled, "section-a", k, x, 5)!;
            byte[] e4 = WriteProfile(WriteProfile(unique, rootName, fitRoot.ProfileBlock), "section-a", fitTip.ProfileBlock);
            Row("E4 Option C, common sqrt basis n=" + n, "root 4412 residual um; tip section-a refit um (at 120 mm); certificate",
                G(fitRoot.MaxResidual * ExampleChordMeters * 1e6) + "; " + G(fitTip.MaxResidual * ExampleChordMeters * 1e6) + "; " + Verdict(e4));
        }

        // E5. New foil (sqrt-10 at root and tip), Make unique to Root, the as-built Import of 4412 at root.
        byte[] nfUnique = FoilSource.MakeIndependent(newFoil, naca12.Name, 0).Source;
        double nfRootChord = Placement.Frame(nfUnique, 0).ChordMeters, nfTipChord = Placement.Frame(nfUnique, 1).ChordMeters;
        Row("E5 New foil, Make unique to Root, Import 4412 at root (as built)", "chord root/tip (m)", G(nfRootChord) + "/" + G(nfTipChord));
        Row("E5 New foil, Make unique to Root, Import 4412 at root (as built)", "outcome", TryImport(nfUnique, 0, dat4412, nfRootChord));
        Row("E5b New foil shared, Import 4412 at root only (as built)", "outcome", TryImport(newFoil, 0, dat4412, nfRootChord));

        // E6. Node budget: two distinct profiles sharing an 11- or 12-vertex basis (blend spans > 5).
        foreach (int n in new[] { 10, 11, 12 })
        {
            var (k, x) = Sqrt(n);
            var fitRoot = DatImport.FitToBasis(DatImport.Parse(dat4412), rootName, k, x, 5)!;
            var fitTip = DatImport.FitToBasis(DatImport.Parse(Naca("0012")), "section-a", k, x, 5)!;
            byte[] e6 = WriteProfile(WriteProfile(unique, rootName, fitRoot.ProfileBlock), "section-a", fitTip.ProfileBlock);
            Row("E6 distinct root 4412 / tip 0012 on one sqrt basis n=" + n, "certificate", Verdict(e6));
        }

        // E7. A My sections entry on the same basis: an exact copy (residual 0, shape identical).
        var entry = Prof(e3, rootName);
        byte[] e7 = WriteProfile(unique, rootName, BlockText(e3, rootName));
        Row("E7 My sections entry already on the shared basis", "shape change vs the entry (chord); certificate", G(FoilSource.MaxOrdinateDeviation(entry, Prof(e7, rootName))) + "; " + Verdict(e7));

        // E8. The largest change each Replace makes to the current section (A4.5 profile oracle), and where.
        var e1cProfile = Prof(e1c, "section-a");
        Row("E8 largest change, shared Replace 4412 (E1c) vs section-a", "chord; mm at 120 mm; at x (chord)", Change(sectionA, e1cProfile));
        var fit0012 = DatImport.FitToBasis(DatImport.Parse(Naca("0012")), rootName, bases[0].Knots, bases[0].X, 5)!;
        byte[] e8b = WriteProfile(unique, rootName, fit0012.ProfileBlock);
        Row("E8 Option A at unique Root: NACA 0012 on the shared spacing", "residual um at 120 mm; certificate", G(fit0012.MaxResidual * ExampleChordMeters * 1e6) + "; " + Verdict(e8b));
        Row("E8 largest change, unique Root 0012 on the shared spacing vs section-a", "chord; mm at 120 mm; at x (chord)", Change(sectionA, Prof(e8b, rootName)));
        Row("E8 largest change, unique Root 4412 on the shared spacing (refused) vs section-a", "chord; mm at 120 mm; at x (chord)", Change(sectionA, Prof(e3, rootName)));
        Row("E8 own t/c (max upper-lower at equal x): section-a; 4412 own-16", "chord", G(OwnThickness(sectionA)) + "; " + G(OwnThickness(e1cProfile)));

        // E9. The design's rule 3 (no differing neighbour): the smallest own sqrt spacing within 10 um at 120 mm, in place.
        {
            var (k15, x15) = Sqrt(15);
            var fit15 = DatImport.FitToBasis(DatImport.Parse(dat4412), "section-a", k15, x15, 5)!;
            byte[] e9 = WriteProfile(example, "section-a", fit15.ProfileBlock.Replace("profile \"section-a\"", "profile \"probe\""));
            Row("E9 Rule 3: shared Replace 4412 in place, own sqrt 15", "residual um at 120 mm; certificate", G(fit15.MaxResidual * ExampleChordMeters * 1e6) + "; " + Verdict(e9));
            Row("E9 largest change vs section-a", "chord; mm at 120 mm; at x (chord)", Change(sectionA, Prof(e9, "section-a")));
            Results["record_shared4412_15"] = Rec(Prof(e9, "section-a"));
            var symmetricCurrent = DatImport.FitToBasis(DatImport.Parse(Naca("0012")), "section-a", bases[0].Knots, bases[0].X, 5)!;
            Row("E9 Rule 3: shared Replace 0012, current spacing first", "residual um at 120 mm; points kept", G(symmetricCurrent.MaxResidual * ExampleChordMeters * 1e6) + "; " + symmetricCurrent.VertexCount);
        }

        // CF. Repair cycle 1 (geometry lens finding 1, 7): GEN shapes from the closed form in the chord frame, never through
        // DatImport.Parse. LE = the continuous minimum-x point; frame = LE -> TE-midpoint chord at unit length (rotation and
        // scale reported). Residual = the largest Euclidean distance from the closed form (201 cosine samples per side) to the
        // fitted curve; the as-built FitToBasis vertical residual is printed beside it.
        foreach (string code in new[] { "0009", "0012", "4412", "2412", "4412c", "0009c", "0012c" })
        {
            var (cf, rot, scale) = ClosedForm(code);
            Row("CF NACA " + code, "frame: LE x before translate (chord); rotation (deg); scale", G(cf.LeX) + "; " + G(rot) + "; " + G(scale));
            Row("CF NACA " + code, "TE end x upper / lower in the frame (the record ends both sides at x = 1)", G(cf.DenseUpper[^1].X) + " / " + G(cf.DenseLower[^1].X));
            foreach (var basis in bases)
            {
                var fit = DatImport.FitToBasis(cf.Profile, "probe", basis.Knots, basis.X, 5);
                Row("CF NACA " + code, "on " + basis.Name + ": Euclidean um at 120 mm (FitToBasis vertical um); within 10 um", fit is null ? "no fit" :
                    G(Euclid(cf, fit) * ExampleChordMeters * 1e6) + " (" + G(fit.MaxResidual * ExampleChordMeters * 1e6) + "); " + (Euclid(cf, fit) * ExampleChordMeters <= Acceptance ? "yes" : "NO"));
            }
            int within = -1; double atWithin = double.NaN;
            for (int n = 8; n <= 16 && within < 0; n++)
            {
                var (k, x) = Sqrt(n);
                var fit = DatImport.FitToBasis(cf.Profile, "probe", k, x, 5);
                if (fit is null) continue;
                double e = Euclid(cf, fit);
                if (e * ExampleChordMeters <= Acceptance) { within = n; atWithin = e; }
            }
            Row("CF NACA " + code, "smallest own sqrt spacing within 10 um at 120 mm (Euclidean); its um", within + "; " + G(atWithin * ExampleChordMeters * 1e6));
            if (code is "4412" or "4412c")
            {
                var series = new List<string>();
                for (int n = 8; n <= 16; n++) { var (k, x) = Sqrt(n); var f = DatImport.FitToBasis(cf.Profile, "probe", k, x, 5); series.Add(n + ":" + (f is null ? "-" : G(Euclid(cf, f) * ExampleChordMeters * 1e6))); }
                Row("CF NACA " + code, "own sqrt spacing n: Euclidean um at 120 mm", string.Join(" ", series));
            }
            if (code == "4412c" && within > 0)
            {
                var (k, x) = Sqrt(within);
                var fit = DatImport.FitToBasis(cf.Profile, "section-a", k, x, 5)!;
                byte[] shared = WriteProfile(example, "section-a", fit.ProfileBlock.Replace("profile \"section-a\"", "profile \"probe\""));
                Row("CF rule 3: shared Replace 4412 (closed TE) in place, own " + within, "certificate; largest change vs section-a (chord; mm; at x)", Verdict(shared) + "; " + Change(sectionA, Prof(shared, "section-a")));
                Results["record_cf_shared4412"] = Rec(Prof(shared, "section-a"));
                var onShared = DatImport.FitToBasis(cf.Profile, rootName, bases[0].Knots, bases[0].X, 5)!;
                Row("CF rule 4: unique Root 4412 (closed TE) on the shared spacing", "Euclidean um at 120 mm (refused if > 10)", G(Euclid(cf, onShared) * ExampleChordMeters * 1e6));
            }
            if (code is "0012" or "0012c")
            {
                var onShared = DatImport.FitToBasis(cf.Profile, rootName, bases[0].Knots, bases[0].X, 5)!;
                byte[] uniq = WriteProfile(unique, rootName, onShared.ProfileBlock);
                Row("CF rule 4: unique Root NACA " + code + " on the shared spacing", "Euclidean um at 120 mm; certificate; largest change (chord; mm; at x)",
                    G(Euclid(cf, onShared) * ExampleChordMeters * 1e6) + "; " + Verdict(uniq) + "; " + Change(sectionA, Prof(uniq, rootName)));
            }
        }

        // B3. Repair cycle 1 (geometry lens finding 2): three stations Root X, Mid X, Tip Y; Replace at Tip only fails over
        // 10 um, so B is offered. B over {Mid, Tip} (the first draft's "chain") vs B over every station of the foil.
        {
            string text3 = FoilSource.Utf8.GetString(example).Replace("sections { at root profile \"section-a\" at tip profile \"section-a\" }",
                "sections { at root profile \"section-a\" at 50 % profile \"section-a\" at tip profile \"section-a\" }");
            byte[] three = FoilSource.Utf8.GetBytes(text3);
            Row("B3 setup", "three stations parse; certificate", FoilSource.Parse(three).IsParsed + "; " + Verdict(three));
            byte[] tipUnique = FoilSource.MakeIndependent(three, "section-a", 2).Source;
            string tipName = Def(tipUnique).Profiles[Def(tipUnique).Assignments[2].Profile].Name;
            var tipProfile = Prof(tipUnique, tipName);
            var movedUpper = tipProfile.Upper.Points.Select(q => new[] { q[0], q[1] }).ToArray(); movedUpper[3][1] += 0.004;
            byte[] tipY = WriteProfile(tipUnique, tipName, BlockText(tipUnique, tipName).Replace(
                "(" + F(tipProfile.Upper.Points[3][0]) + ", " + F(tipProfile.Upper.Points[3][1]) + ")", "(" + F(movedUpper[3][0]) + ", " + F(movedUpper[3][1]) + ")"));
            Row("B3 setup", "Root X, Mid X, Tip Y (Tip upper point 4 raised 0.4 % chord): certificate", Verdict(tipY));
            var (cf, _, _) = ClosedForm("4412c");
            var (k15, x15) = Sqrt(15);
            var own = DatImport.FitToBasis(cf.Profile, "probe", k15, x15, 5)!;
            // wrong chain {Mid, Tip}: Mid and Tip take 4412 (Mid via its own unique copy), Root keeps section-a
            byte[] midUnique = FoilSource.MakeIndependent(tipY, "section-a", 1).Source;
            string midName = Def(midUnique).Profiles[Def(midUnique).Assignments[1].Profile].Name;
            byte[] partial = WriteProfile(WriteProfile(midUnique, tipName, own.ProfileBlock.Replace("profile \"probe\"", "profile \"" + tipName + "\"")), midName,
                own.ProfileBlock.Replace("profile \"probe\"", "profile \"" + midName + "\""));
            Row("B3 B over {Mid, Tip} only", "certificate", Verdict(partial));
            // B over every station: rewrite Tip's block, re-point every assignment to it, delete the blocks left unreferenced
            string t = FoilSource.Utf8.GetString(WriteProfile(tipY, tipName, own.ProfileBlock.Replace("profile \"probe\"", "profile \"" + tipName + "\"")));
            var defAll = FoilSource.Parse(FoilSource.Utf8.GetBytes(t)).Definition!;
            var orphan = defAll.Profiles.Single(pr => pr.Name == "section-a");
            t = t[..orphan.BlockStart] + t[orphan.BlockEnd..];
            t = t.Replace("at root profile \"section-a\" at 50 % profile \"section-a\"", "at root profile \"" + tipName + "\" at 50 % profile \"" + tipName + "\"");
            byte[] all = FoilSource.Utf8.GetBytes(t);
            Row("B3 B over every station (one block, all re-pointed, unreferenced deleted)", "parses; certificate", FoilSource.Parse(all).IsParsed + "; " + Verdict(all));
        }

        // Records the mockup draws (docs/mockups/m12d-catalog.html): knots and control points as fitted.
        Results["record_shared4412"] = Rec(e1cProfile);
        Results["record_uniqueRoot0012"] = Rec(Prof(e8b, rootName));
        Results["record_uniqueRoot4412Refused"] = Rec(Prof(e3, rootName));

        const string Front = "---\nid: proof-m12d-catalog-probe\ntitle: \"M1.2d Replace probe — fits, certificates and the shared point-spacing rule\"\ntype: proof-pack\nstatus: in-review\nowner: \"@timianmalloo\"\nphase: design — M1.2d\ntags: [m1.2d, catalog, replace, abscissa, probe]\nlinks:\n  - { to: design-m12d-catalog, rel: depends-on }\nreview-by: 2027-04-01\nsummary: >-\n  Generated by docs/proof/m12d-catalog/probe (dotnet run -c Release -- <proof dir> <repo>) through the as-built Core.\n  Rows CF and B3 are the design's evidence; the earlier NACA rows through DatImport.Parse are the as-built DAT path.\n---\n\n";
        File.WriteAllText(Path.Combine(root, "output", "table.md"), Front + Table.ToString());
        File.WriteAllText(Path.Combine(root, "output", "results.json"), JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        Console.Write(Table.ToString());
        return 0;
    }

    private static string TryImport(byte[] bytes, int assignment, byte[] dat, double chord)
    {
        try
        {
            var (next, report) = Import(bytes, assignment, dat);
            return "basis " + report.Basis + "; residual " + G(report.MaxResidual) + " (" + G(report.MaxResidual * chord * 1e6) + " um); " + Verdict(next);
        }
        catch (TargetInvocationException error) { return "refused " + (error.InnerException as ContractError)?.Code; }
    }

    private static (byte[] Bytes, ImportReport Report) Import(byte[] bytes, int assignment, byte[] dat)
    {
        object result = ImportPatch.Invoke(null, [bytes, assignment, dat])!;
        var fields = result.GetType().GetFields();
        return ((byte[])fields[0].GetValue(result)!, (ImportReport)fields[2].GetValue(result)!);
    }

    private static (double[] Knots, double[] X) Sqrt(int n)
    {
        object tuple = OwnSqrtBasis.Invoke(null, [n])!;
        var fields = tuple.GetType().GetFields();
        double[] Round(double[] values) => values.Select(v => double.Parse((string)FormatNumber.Invoke(null, [v])!, CultureInfo.InvariantCulture)).ToArray();
        return (Round((double[])fields[0].GetValue(tuple)!), Round((double[])fields[1].GetValue(tuple)!));
    }

    // Samples a record profile at 161 cosine-spaced chord stations per surface, as a coordinate set for a refit.
    private static DatProfile Sample(ProfileDefinition profile)
    {
        List<ProfilePoint> Side(Curve c)
        {
            var points = new List<ProfilePoint>();
            for (int i = 0; i <= 160; i++)
            {
                double u = i / 160.0;
                double[] n = SplineBasis.Values(c.Knots, c.Degree, u);
                double x = 0, y = 0;
                for (int j = 0; j < n.Length; j++) { x += n[j] * c.Points[j][0]; y += n[j] * c.Points[j][1]; }
                points.Add(new ProfilePoint(x, y));
            }
            return points;
        }
        return new DatProfile(profile.Name, "sampled", Side(profile.Upper), Side(profile.Lower), [], 322);
    }

    private static byte[] WriteProfile(byte[] bytes, string name, string block)
    {
        var target = Prof(bytes, name);
        string text = FoilSource.Utf8.GetString(bytes);
        int newline = text.LastIndexOf('\n', target.BlockStart);
        string indent = newline < 0 ? "" : text[(newline + 1)..target.BlockStart];
        string named = block.Replace("profile \"probe\"", "profile \"" + name + "\"");
        string result = text[..target.BlockStart] + string.Join("\n" + indent, named.Split('\n')) + text[target.BlockEnd..];
        byte[] candidate = FoilSource.Utf8.GetBytes(result);
        if (!FoilSource.Parse(candidate).IsParsed) throw new InvalidOperationException("patch did not parse");
        return candidate;
    }

    private static string BlockText(byte[] bytes, string name)
    {
        var target = Prof(bytes, name);
        return FoilSource.Utf8.GetString(bytes)[target.BlockStart..target.BlockEnd];
    }

    // NACA 4-digit (mpxx) and 5-digit 230xx, Selig order (TE upper -> LE -> TE lower), standard open trailing edge.
    private static byte[] Naca(string code)
    {
        double t = int.Parse(code[^2..], CultureInfo.InvariantCulture) / 100.0;
        Func<double, (double Yc, double Slope)> camber;
        if (code.Length == 4)
        {
            double m = (code[0] - '0') / 100.0, p = (code[1] - '0') / 10.0;
            camber = x => m == 0 ? (0, 0) : x < p
                ? (m / (p * p) * (2 * p * x - x * x), 2 * m / (p * p) * (p - x))
                : (m / ((1 - p) * (1 - p)) * (1 - 2 * p + 2 * p * x - x * x), 2 * m / ((1 - p) * (1 - p)) * (p - x));
        }
        else
        {
            const double r = 0.2025, k1 = 15.957; // 230: p = 0.15
            camber = x => x < r
                ? (k1 / 6 * (x * x * x - 3 * r * x * x + r * r * (3 - r) * x), k1 / 6 * (3 * x * x - 6 * r * x + r * r * (3 - r)))
                : (k1 * r * r * r / 6 * (1 - x), -k1 * r * r * r / 6);
        }
        var upper = new List<(double, double)>(); var lower = new List<(double, double)>();
        for (int i = 0; i <= 80; i++)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / 80));
            double yt = 5 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x - 0.1015 * x * x * x * x);
            var (yc, slope) = camber(x);
            double th = Math.Atan(slope);
            upper.Add((x - yt * Math.Sin(th), yc + yt * Math.Cos(th)));
            lower.Add((x + yt * Math.Sin(th), yc - yt * Math.Cos(th)));
        }
        var sb = new StringBuilder("NACA " + code + "\n");
        for (int i = 80; i >= 0; i--) sb.Append(F(upper[i].Item1)).Append(' ').Append(F(upper[i].Item2)).Append('\n');
        for (int i = 1; i <= 80; i++) sb.Append(F(lower[i].Item1)).Append(' ').Append(F(lower[i].Item2)).Append('\n');
        return Encoding.UTF8.GetBytes(sb.ToString());
        static string F(double v) => v.ToString("F8", CultureInfo.InvariantCulture);
    }

    private static double YAt(Curve c, double x)
    {
        double lo = 0, hi = 1;
        for (int i = 0; i < 80; i++)
        {
            double mid = (lo + hi) / 2;
            double[] n = SplineBasis.Values(c.Knots, c.Degree, mid);
            double xm = 0; for (int j = 0; j < n.Length; j++) xm += n[j] * c.Points[j][0];
            if (xm < x) lo = mid; else hi = mid;
        }
        double[] nb = SplineBasis.Values(c.Knots, c.Degree, (lo + hi) / 2);
        double y = 0; for (int j = 0; j < nb.Length; j++) y += nb[j] * c.Points[j][1];
        return y;
    }

    private static string Change(ProfileDefinition a, ProfileDefinition b)
    {
        double worst = 0, at = 0;
        for (int i = 1; i < 2000; i++)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * i / 2000));
            double d = Math.Max(Math.Abs(YAt(a.Upper, x) - YAt(b.Upper, x)), Math.Abs(YAt(a.Lower, x) - YAt(b.Lower, x)));
            if (d > worst) { worst = d; at = x; }
        }
        return G(FoilSource.MaxOrdinateDeviation(a, b)) + " (oracle); " + G(worst * ExampleChordMeters * 1e3) + "; " + G(at);
    }

    private static double OwnThickness(ProfileDefinition p)
    {
        double t = 0;
        for (int i = 1; i < 2000; i++) { double x = i / 2000.0; t = Math.Max(t, YAt(p.Upper, x) - YAt(p.Lower, x)); }
        return t;
    }

    private static object Rec(ProfileDefinition p) => new { knots = p.Upper.Knots, upper = p.Upper.Points, lower = p.Lower.Points };

    private sealed record ClosedShape(DatProfile Profile, double LeX, List<(double X, double Y)> DenseUpper, List<(double X, double Y)> DenseLower);

    // NACA 4-digit closed form in the chord frame: LE at the continuous minimum-x point, chord to the TE midpoint, unit length.
    private static (ClosedShape Shape, double RotationDeg, double Scale) ClosedForm(string code)
    {
        bool closedTe = code.EndsWith('c');
        string digits = closedTe ? code[..^1] : code;
        double m = (digits[0] - '0') / 100.0, p = (digits[1] - '0') / 10.0, t = int.Parse(digits[2..], CultureInfo.InvariantCulture) / 100.0;
        double a4 = closedTe ? -0.1036 : -0.1015;
        (double X, double Y) Upper(double x, int sign)
        {
            double yt = 5 * t * (0.2969 * Math.Sqrt(x) - 0.1260 * x - 0.3516 * x * x + 0.2843 * x * x * x + a4 * x * x * x * x);
            double yc = m == 0 ? 0 : x < p ? m / (p * p) * (2 * p * x - x * x) : m / ((1 - p) * (1 - p)) * (1 - 2 * p + 2 * p * x - x * x);
            double dy = m == 0 ? 0 : x < p ? 2 * m / (p * p) * (p - x) : 2 * m / ((1 - p) * (1 - p)) * (p - x);
            double th = Math.Atan(dy);
            return (x - sign * yt * Math.Sin(th), yc + sign * yt * Math.Cos(th));
        }
        // the leading edge: the minimum of x along the upper branch near the nose (golden section on the parameter)
        double a = 0, b = 0.05;
        for (int i = 0; i < 200; i++) { double c1 = b - (b - a) / 1.618033988749895, c2 = a + (b - a) / 1.618033988749895; if (Upper(c1, 1).X < Upper(c2, 1).X) b = c2; else a = c1; }
        double xs = (a + b) / 2; if (Upper(xs, 1).X > 0) xs = 0;
        var le = Upper(xs, 1);
        var teU = Upper(1, 1); var teL = Upper(1, -1);
        double mx = (teU.X + teL.X) / 2 - le.X, my = (teU.Y + teL.Y) / 2 - le.Y, len = Math.Sqrt(mx * mx + my * my), ang = Math.Atan2(my, mx);
        (double X, double Y) Frame((double X, double Y) q) { double dx = q.X - le.X, dy2 = q.Y - le.Y; return ((dx * Math.Cos(-ang) - dy2 * Math.Sin(-ang)) / len, (dx * Math.Sin(-ang) + dy2 * Math.Cos(-ang)) / len); }
        List<(double X, double Y)> Side(bool upper, int count)
        {
            var list = new List<(double X, double Y)>();
            for (int i = 0; i <= count; i++)
            {
                double s = 0.5 * (1 - Math.Cos(Math.PI * i / count));
                if (upper) list.Add(Frame(Upper(xs + (1 - xs) * s, 1)));
                else
                {
                    // lower side from the LE: the upper branch back to x = 0, then the lower branch to the TE
                    double total = xs + 1, d = s * total;
                    list.Add(Frame(d <= xs ? Upper(xs - d, 1) : Upper(d - xs, -1)));
                }
            }
            list[0] = (0, 0);
            return list;
        }
        var u81 = Side(true, 80); var l81 = Side(false, 80);
        var profile = new DatProfile("NACA " + code, "closed-form", u81.Select(q => new ProfilePoint(q.X, q.Y)).ToList(), l81.Select(q => new ProfilePoint(q.X, q.Y)).ToList(), [], 162);
        return (new ClosedShape(profile, le.X, Side(true, 200), Side(false, 200)), ang * 180 / Math.PI, len);
    }

    private static double Euclid(ClosedShape shape, ImportedProfile fit)
    {
        string block = fit.ProfileBlock.Replace("profile \"probe\"", "profile \"e\"").Replace("profile \"section-a\"", "profile \"e\"");
        string doc = "foildsl \"4.1\"\nsection \"e\" { evaluator \"cfdw-cv\" \"2\" " + block[(block.IndexOf('{') + 1)..block.LastIndexOf('}')].Trim() + " }\n";
        var parsed = FoilSource.Parse(FoilSource.Utf8.GetBytes(doc));
        var prof = parsed.Definition?.Profiles.FirstOrDefault() ?? throw new InvalidOperationException("standalone section did not parse: " + string.Join(" | ", parsed.Diagnostics));
        double worst = 0;
        foreach (var (dense, curve) in new[] { (shape.DenseUpper, prof.Upper), (shape.DenseLower, prof.Lower) })
        {
            var poly = new List<(double X, double Y)>();
            for (int i = 0; i <= 6000; i++)
            {
                double u = i / 6000.0; double[] n = SplineBasis.Values(curve.Knots, curve.Degree, u);
                double x = 0, y = 0; for (int j = 0; j < n.Length; j++) { x += n[j] * curve.Points[j][0]; y += n[j] * curve.Points[j][1]; }
                poly.Add((x, y));
            }
            foreach (var q in dense)
            {
                double best = double.PositiveInfinity;
                for (int i = 0; i + 1 < poly.Count; i++)
                {
                    var (ax, ay) = poly[i]; var (bx, by) = poly[i + 1];
                    double vx = bx - ax, vy = by - ay, w = vx * vx + vy * vy, tt = w == 0 ? 0 : Math.Clamp(((q.X - ax) * vx + (q.Y - ay) * vy) / w, 0, 1);
                    double ex = ax + tt * vx - q.X, ey = ay + tt * vy - q.Y, d = ex * ex + ey * ey;
                    if (d < best) best = d;
                }
                worst = Math.Max(worst, Math.Sqrt(best));
            }
        }
        return worst;
    }

    private static string F(double v) => v == 0 ? "0" : v == 1 ? "1" : v.ToString("R", CultureInfo.InvariantCulture);

    private static Definition Def(byte[] bytes) => FoilSource.Parse(bytes).Definition!;
    private static ProfileDefinition Prof(byte[] bytes, string name) => Def(bytes).Profiles.Single(p => p.Name == name);
    private static string Verdict(byte[] bytes)
    {
        var assessment = Geometry.Assess(FoilSource.Parse(bytes));
        return assessment.Status + (assessment.Status == GeometryStatus.Certified ? "" : " - " + assessment.Reason);
    }
    private static string G(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    private static void Row(string scenario, string measure, string value)
    {
        Table.Append("| ").Append(scenario).Append(" | ").Append(measure).Append(" | ").Append(value).AppendLine(" |");
        Results[scenario + " :: " + measure] = value;
    }
}
