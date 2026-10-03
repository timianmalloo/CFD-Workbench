using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using GspkProbe;

// GSPK probe: the x-overlay certificate run against the as-built admission proofs (PlacementWidth,
// BlendPlacementWidth, QueryFeasibility) as the oracle. Usage: dotnet run -c Release -- stage1 <spike dir>
string stage = args.Length > 0 ? args[0] : "stage1";
string root = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
Directory.CreateDirectory(Path.Combine(root, "fixtures"));
Directory.CreateDirectory(Path.Combine(root, "output"));
var log = new StringBuilder();
void Say(string line) { Console.WriteLine(line); log.AppendLine(line); }
const int AtomCap = 4096;
var tolerance = Rational.From(1e-12); // the one constant (Bernstein.Maximum :1015, BlendPlacementWidth :562)
var results = new List<Dictionary<string, object?>>();

// Stage 2 (F2, F3, F5) is not run: the row ends the spike on a measured stage-1 no-go (verdict.md).
if (stage != "stage1") throw new ArgumentException("Only stage1 exists; see verdict.md.");
{
    // F1: section-a, upper Control → Anchor at vertex 3 (35 %), lower unchanged, closed TE. 120 mm and 2 m.
    var anchored = Fixtures.ControlToAnchor(Fixtures.SectionAUpper, 3);
    Say($"F1 fixture: u* = {anchored.U:R}, vertex {anchored.Vertex}, Δy = {anchored.DeltaY:R} chord, points {Fixtures.SectionAUpper.Points.Length} → {anchored.Result.Points.Length}");
    foreach (var chord in new[] { 120.0, 2000.0 })
    {
        var profile = new ProfileText("section-a", anchored.Result, Fixtures.SectionALower, "closed");
        string text = Fixtures.Foil(chord, new[] { profile }, "at root profile \"section-a\" at tip profile \"section-a\"");
        results.Add(RunSingle($"F1-anchor35-c{chord:0}", text));
    }
    // F4: section-a ↔ an x-edited unique copy (vertex 3 x +0.02 and +0.20, both sides), at 120 mm and 2 m.
    foreach (var dx in new[] { 0.02, 0.20 })
        foreach (var chord in new[] { 120.0, 2000.0 })
        {
            var a = new ProfileText("section-a", Fixtures.SectionAUpper, Fixtures.SectionALower, "closed");
            var b = Fixtures.XEdited("section-b", 3, dx);
            string text = Fixtures.Foil(chord, new[] { a, b }, "at root profile \"section-a\" at tip profile \"section-b\"");
            results.Add(RunBlend($"F4-dx{dx:0.00}-c{chord:0}", text));
        }
}

File.WriteAllText(Path.Combine(root, "output", stage + ".json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(root, "output", stage + ".log"), log.ToString());
return;

// ---------------------------------------------------------------------------------------------------------------

Definition Parse(string name, string text)
{
    File.WriteAllText(Path.Combine(root, "fixtures", name + ".foil"), text);
    var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
    if (!parsed.IsParsed)
        throw new InvalidOperationException(name + ": " + string.Join(" | ", parsed.Diagnostics.Select(d => d.Code + "@" + d.Line + " " + d.Reason)));
    return parsed.Definition!;
}

Dictionary<string, PolynomialSpan[]> Spans(Definition definition, ProofBudget watch)
{
    var spans = definition.Curves.ToDictionary(pair => pair.Key, pair => Bernstein.Spans(pair.Value, watch));
    foreach (var curve in spans.Values)
        foreach (var span in curve)
        {
            var derivative = span.X.Zip(span.X.Skip(1), (a, b) => b - a).ToArray();
            Geometry.Require(derivative.All(x => x >= 0) && derivative.Any(x => x > 0), "Abscissa monotonicity is not certified.");
        }
    return spans;
}

// Separation (nose line + atoms + TE wedge or open gap) and the maximum of upper − lower for one profile.
(Dictionary<string, object?> Facts, Rational Lo, Rational Hi, bool Ok) Profile(ProfileDefinition profile, Dictionary<string, PolynomialSpan[]> spans, ProofBudget watch)
{
    var facts = new Dictionary<string, object?>();
    var upper = Piece.From(spans[profile.Upper.Path]);
    var lower = Piece.From(spans[profile.Lower.Path]);
    facts["pieces"] = $"{upper.Length} upper / {lower.Length} lower";
    var nose = Overlay.Nose(upper[0], lower[0], watch);
    facts["nose"] = nose.Ok ? $"line m = {nose.M.Nearest():R} proves x ∈ (0, {nose.CoveredTo.Nearest():R}] at nose depth {nose.Depth}; y = 0 rule {(nose.ZeroLineHolds ? "would also hold" : "FAILS")}" : "nose rule failed";
    if (!nose.Ok) return (facts, 0, 0, false);
    Rational teFrom;
    if (profile.Closure == "closed")
    {
        var te = Overlay.TrailingEdge(upper[^1], lower[^1], watch);
        facts["trailingEdge"] = te.Ok ? $"closed: wedge line m = {te.M.Nearest():R} proves x ∈ [{te.CoveredFrom.Nearest():R}, 1) at depth {te.Depth}" : "TE wedge rule failed";
        if (!te.Ok) return (facts, 0, 0, false);
        teFrom = te.CoveredFrom;
    }
    else
    {
        var gap = upper[^1].Y[^1] - lower[^1].Y[^1];
        facts["trailingEdge"] = $"open: TE gap {gap.Nearest():R} (exact > 0: {gap > 0})";
        if (!(gap > 0)) return (facts, 0, 0, false);
        teFrom = 1;
    }
    var separation = Overlay.Separation(upper, lower, nose.CoveredTo, teFrom, watch, AtomCap);
    facts["separation"] = $"{separation.Reason}; atoms {separation.Atoms}, splits {separation.Splits}, min lower bound {separation.MinLower.Nearest():E3}";
    if (!separation.Certified) return (facts, 0, 0, false);
    var max = Overlay.Maximum(new[] { upper, lower }, new[] { ((Rational)1, (Rational)1) }, tolerance, watch, AtomCap);
    var lo = max.Lower[0].At(0); var hi = max.Upper[0].At(0);
    facts["maximum"] = $"{max.Reason}; [{lo.Nearest():R}, {hi.Nearest():R}], relative gap {((hi - lo) / lo).Nearest():E3}; final atoms {max.FinalAtoms}, peak {max.PeakAtoms}, splits {max.Splits}, rounds {max.Rounds}, max piece depth {max.MaxDepth}";
    facts["maximumAtomsPeak"] = max.PeakAtoms;
    return (facts, lo, hi, max.Certified);
}

Dictionary<string, object?> RunSingle(string name, string text)
{
    Say($"\n== {name}");
    var definition = Parse(name, text);
    var result = new Dictionary<string, object?> { ["fixture"] = name };
    for (int pass = 0; pass < 2; pass++)
    {
        var watch = new ProofBudget();
        Rational.ResetWidest();
        var clock = Stopwatch.StartNew();
        var facts = new Dictionary<string, object?>();
        bool ok = false; string reason = "";
        Rational lo = 0, hi = 0;
        Dictionary<string, PolynomialSpan[]>? spans = null;
        var profile = definition.Profiles[0];
        try
        {
            spans = Spans(definition, watch);
            (facts, lo, hi, ok) = Profile(profile, spans, watch);
            if (ok)
            {
                var width = PlacementWidth(spans, profile, lo, hi, definition.HalfSpan, watch);
                facts["placementWidth"] = width is { } w ? $"{w.Nearest():E4} m (≤ 1e-8: {w <= Rational.From(1e-8)})" : "REFUSED by PlacementWidth";
                ok = width is not null;
                var feasibility = QueryFeasibility.Prove(spans, profile, lo, hi, definition.HalfSpan, watch);
                facts["queryFeasibility"] = $"max bits {feasibility.MaximumIntermediateBits} at {feasibility.MaximumBitPath}; ops {feasibility.RationalOperationsUpper}";
            }
            reason = ok ? "certified" : "not certified";
        }
        catch (ProofRefusal refusal) { reason = refusal.Code + ": " + refusal.Message; ok = false; }
        clock.Stop();
        facts["status"] = reason;
        facts["work"] = watch.Spent;
        facts["seconds"] = clock.Elapsed.TotalSeconds;
        facts["widestOperandBits"] = Rational.WidestOperandBits;
        facts["withinBudget"] = ok && watch.Spent < ProofBudget.DefaultWorkLimit && clock.Elapsed.TotalSeconds <= 1 && Rational.WidestOperandBits <= 32768;
        if (pass == 1 && ok && spans is not null)
        {
            var needed = NeededSingleDelta(spans, profile, lo, definition.HalfSpan);
            facts["deltaNeeded"] = $"{needed:E3} (largest relative gap PlacementWidth admits at this document's chord and hulls)";
        }
        result[pass == 0 ? "cold" : "warm"] = facts;
    }
    foreach (var pair in (Dictionary<string, object?>)result["warm"]!) Say($"  {pair.Key}: {Format(pair.Value)}");
    Say($"  cold seconds: {Format(((Dictionary<string, object?>)result["cold"]!)["seconds"])}");
    return result;
}

Dictionary<string, object?> RunBlend(string name, string text)
{
    Say($"\n== {name}");
    var definition = Parse(name, text);
    var result = new Dictionary<string, object?> { ["fixture"] = name };
    foreach (var mode in new[] { "constant-1e-12", "per-document-delta", "proposal-tightened", "diagnostic-unbounded", "diagnostic-unbounded-tightened" })
    {
        Rational target = tolerance;
        bool tight = mode is "proposal-tightened" or "diagnostic-unbounded-tightened";
        // Diagnostic only (outside the §3.5 criteria): the as-built per-document δ with the work limit raised to 5e10
        // and no atom cap, to measure what the 2 m cases would need instead of extrapolating it.
        bool diagnostic = mode.StartsWith("diagnostic", StringComparison.Ordinal);
        if (diagnostic && !name.EndsWith("c2000", StringComparison.Ordinal)) continue;
        if (mode != "constant-1e-12")
        {
            if (result.TryGetValue(tight ? "deltaNeededTightened" : "deltaNeeded", out var needed) && needed is double d) target = Rational.From(d);
            else continue;
        }
        for (int pass = 0; pass < 2; pass++)
        {
            var watch = diagnostic ? Unbounded() : new ProofBudget();
            Rational.ResetWidest();
            var clock = Stopwatch.StartNew();
            var facts = new Dictionary<string, object?> { ["targetGap"] = target.Nearest() };
            long excludedWork = 0; double excludedSeconds = 0;
            bool ok = false; string reason = "";
            try
            {
                var spans = Spans(definition, watch);
                var a = definition.Profiles[definition.Assignments[0].Profile];
                var b = definition.Profiles[definition.Assignments[1].Profile];
                var pa = Profile(a, spans, watch); var pb = Profile(b, spans, watch);
                facts["profileA"] = pa.Facts; facts["profileB"] = pb.Facts;
                if (pa.Ok && pb.Ok)
                {
                    var certified = new[]
                    {
                        new CertifiedProfile(a.Upper.Path, a.Lower.Path, new RationalInterval(pa.Lo, pa.Hi), Array.Empty<PolynomialSpan>(), 0),
                        new CertifiedProfile(b.Upper.Path, b.Lower.Path, new RationalInterval(pb.Lo, pb.Hi), Array.Empty<PolynomialSpan>(), 0),
                    };
                    var sides = new[] { Piece.From(spans[a.Upper.Path]), Piece.From(spans[a.Lower.Path]), Piece.From(spans[b.Upper.Path]), Piece.From(spans[b.Lower.Path]) };
                    // Outward-rounded reciprocal maxima: the upper pair rounds up from 1/M_lo, the lower pair down from 1/M_hi.
                    var scales = new[] { (((Rational)1 / pa.Lo).DyadicUp(Overlay.BoundBits), ((Rational)1 / pb.Lo).DyadicUp(Overlay.BoundBits)), (((Rational)1 / pa.Hi).DyadicDown(Overlay.BoundBits), ((Rational)1 / pb.Hi).DyadicDown(Overlay.BoundBits)) };
                    if (!result.ContainsKey("deltaNeeded"))
                    {
                        // Diagnostic, outside the proof's work and time: measured on a fresh thread-local budget below.
                        long before = Rational.Work;
                        var spent = Stopwatch.StartNew();
                        result["deltaNeeded"] = NeededBlendSubdivision(spans, certified, definition.HalfSpan, false);
                        result["deltaNeededTightened"] = NeededBlendSubdivision(spans, certified, definition.HalfSpan, true);
                        spent.Stop();
                        // Subtracted from this proof's reported work and seconds below.
                        excludedWork = Rational.Work - before; excludedSeconds = spent.Elapsed.TotalSeconds;
                    }
                    var blend = Overlay.Maximum(sides, scales, target, watch, diagnostic ? int.MaxValue : AtomCap);
                    var gap = blend.GapPerScale.Max();
                    facts["maxT0"] = $"{blend.Reason}; gap reached {gap.Nearest():E3} (uniform in w, both scale ends); T0 at w=0 [{blend.Lower[1].At(0).Nearest():R}, {blend.Upper[0].At(0).Nearest():R}], w=1 [{blend.Lower[1].At(1).Nearest():R}, {blend.Upper[0].At(1).Nearest():R}]; final atoms {blend.FinalAtoms}, peak {blend.PeakAtoms}, splits {blend.Splits}, rounds {blend.Rounds}, max piece depth {blend.MaxDepth}; envelope lines stored {blend.Lower.Sum(e => e.Lines.Count) + blend.Upper.Sum(e => e.Lines.Count)}";
                    facts["maxT0Atoms"] = blend.PeakAtoms;
                    var asBuilt = BlendPlacementWidth(spans, certified, definition.HalfSpan, watch);
                    var replica = BlendWidthReplica(spans, certified, definition.HalfSpan, tolerance);
                    facts["blendPlacementWidthAsBuilt(1e-12)"] = asBuilt is { } w ? $"{w.Nearest():E4} m; replica equal: {replica is { } r && r.CompareTo(w) == 0}" : $"REFUSED; replica refuses too: {replica is null}";
                    var achieved = BlendWidthReplica(spans, certified, definition.HalfSpan, gap, tight);
                    facts[tight ? "tightenedReplicaWidthAtAchievedGap(PROPOSAL)" : "blendPlacementWidthAtAchievedGap"] = achieved is { } x ? $"{x.Nearest():E4} m (≤ 1e-8)" : "REFUSED (> 1e-8 or floor consumed)";
                    var rootProfile = a;
                    var feasibility = QueryFeasibility.Prove(spans, rootProfile, pa.Lo, pa.Hi, definition.HalfSpan, watch);
                    var atomQuery = AtomQueryBits(blend.MaxLineNumeratorBits, blend.MaxLineDenominatorBits, feasibility, new[] { a.Upper.Path, b.Upper.Path }, new[] { a.Lower.Path, b.Lower.Path }, pa.Lo, pa.Hi);
                    long envelopeLines = blend.Lower.Sum(e => e.Lines.Count) + blend.Upper.Sum(e => e.Lines.Count);
                    long ops = feasibility.RationalOperationsUpper + 8L * envelopeLines + 64;
                    facts["queryFeasibility"] = $"non-blend part: max bits {feasibility.MaximumIntermediateBits} ({feasibility.MaximumBitPath}), ops {feasibility.RationalOperationsUpper}; envelope query model: line bits N{blend.MaxLineNumeratorBits}/D{blend.MaxLineDenominatorBits}, eval bits {atomQuery.Eval}, normalized-shape bound {atomQuery.Shape} (≤ 32768: {atomQuery.Shape <= 32768}); ops with envelope {ops} (≤ 1e6: {ops <= 1_000_000})";
                    ok = blend.Certified && achieved is not null && atomQuery.Shape <= 32768 && ops <= 1_000_000;
                }
                reason = ok ? "certified" : "not certified";
            }
            catch (ProofRefusal refusal) { reason = refusal.Code + ": " + refusal.Message; ok = false; }
            clock.Stop();
            facts["status"] = reason;
            facts["trace"] = string.Join(" | ", Overlay.Trace.TakeLast(4));
            facts["work"] = watch.Spent - excludedWork;
            facts["seconds"] = clock.Elapsed.TotalSeconds - excludedSeconds;
            facts["widestOperandBits"] = Rational.WidestOperandBits;
            facts["withinBudget"] = ok && watch.Spent - excludedWork < ProofBudget.DefaultWorkLimit && clock.Elapsed.TotalSeconds - excludedSeconds <= 1 && Rational.WidestOperandBits <= 32768;
            result[mode + (pass == 0 ? ":cold" : ":warm")] = facts;
            if (pass == 1 || diagnostic || !ok && facts["seconds"] is double s && s > 20) { if (pass == 0) result[mode + ":warm"] = diagnostic ? "single pass (diagnostic)" : "skipped (cold run > 20 s)"; break; }
        }
        Say($" [{mode}]");
        if (result[mode + ":warm"] is Dictionary<string, object?> warm)
        {
            foreach (var pair in warm) Say($"  {pair.Key}: {Format(pair.Value)}");
            Say($"  cold seconds: {Format(((Dictionary<string, object?>)result[mode + ":cold"]!)["seconds"])}");
        }
        else foreach (var pair in (Dictionary<string, object?>)result[mode + ":cold"]!) Say($"  {pair.Key}: {Format(pair.Value)}");
    }
    if (result.TryGetValue("deltaNeeded", out var dn)) Say($"  deltaNeeded (BlendPlacementWidth subdivision gap admitted at this chord): {Format(dn)}");
    if (result.TryGetValue("deltaNeededTightened", out var dt)) Say($"  deltaNeededTightened (PROPOSAL: floor 1/2, thickness hull): {Format(dt)}");
    return result;
}

string Format(object? value) => value switch
{
    Dictionary<string, object?> d => "{ " + string.Join("; ", d.Select(p => p.Key + ": " + Format(p.Value))) + " }",
    double v => v.ToString("G6", CultureInfo.InvariantCulture),
    _ => value?.ToString() ?? "null",
};

// ---- the oracle: the as-built admission proofs (private; reflection) ----

Rational? PlacementWidth(Dictionary<string, PolynomialSpan[]> spans, ProfileDefinition profile, Rational lo, Rational hi, double halfSpan, ProofBudget watch)
{
    var method = typeof(Geometry).GetMethod("PlacementWidth", BindingFlags.NonPublic | BindingFlags.Static)!;
    try { return (Rational)method.Invoke(null, new object[] { spans, profile, lo, hi, halfSpan, watch })!; }
    catch (TargetInvocationException e) when (e.InnerException is ProofRefusal) { return null; }
}

Rational? BlendPlacementWidth(Dictionary<string, PolynomialSpan[]> spans, CertifiedProfile[] profiles, double halfSpan, ProofBudget watch)
{
    var method = typeof(Geometry).GetMethod("BlendPlacementWidth", BindingFlags.NonPublic | BindingFlags.Static)!;
    try { return (Rational)method.Invoke(null, new object[] { spans, profiles, halfSpan, watch })!; }
    catch (TargetInvocationException e) when (e.InnerException is ProofRefusal) { return null; }
}

double NeededSingleDelta(Dictionary<string, PolynomialSpan[]> spans, ProfileDefinition profile, Rational lo, double halfSpan)
{
    double low = -18, high = -1; // log10 δ
    for (int i = 0; i < 48; i++)
    {
        double mid = (low + high) / 2;
        var hi = lo * (1 + Rational.From(Math.Pow(10, mid)));
        if (PlacementWidth(spans, profile, lo, hi, halfSpan, new ProofBudget()) is not null) low = mid; else high = mid;
    }
    return Math.Pow(10, low);
}

double NeededBlendSubdivision(Dictionary<string, PolynomialSpan[]> spans, CertifiedProfile[] profiles, double halfSpan, bool tight)
{
    double low = -18, high = -1;
    for (int i = 0; i < 48; i++)
    {
        double mid = (low + high) / 2;
        if (BlendWidthReplica(spans, profiles, halfSpan, Rational.From(Math.Pow(10, mid)), tight) is not null) low = mid; else high = mid;
    }
    return Math.Pow(10, low);
}

// BlendPlacementWidth (Geometry.cs :545–595) with the 1e-12 subdivision gap as a parameter. Checked bit-equal to the
// as-built method at 1e-12 on every F4 run ("replica equal").
Rational? BlendWidthReplica(Dictionary<string, PolynomialSpan[]> spans, CertifiedProfile[] profiles, double halfSpan, Rational subdivision, bool tight = false)
{
    Rational Abs(Rational v) => v < 0 ? 0 - v : v;
    Rational ProfileTolerance(Rational maximumLower) { Rational d = Rational.From(1e-14); return maximumLower < 1 ? d * maximumLower : d; }
    Rational delta = Rational.From(1e-14);
    var paths = new HashSet<string>(profiles.SelectMany(p => new[] { p.UpperPath, p.LowerPath }), StringComparer.Ordinal);
    Rational worstProfileDelta = 0, worstNormalized = 0, relativeGap = 0;
    foreach (var profile in profiles)
    {
        Rational profileDelta = ProfileTolerance(profile.Maximum.Lower);
        if (profileDelta > worstProfileDelta) worstProfileDelta = profileDelta;
        Rational normalizedWidth = 2 * profileDelta / profile.Maximum.Lower +
            (profile.Maximum.Upper + 2 * profileDelta) * (profile.Maximum.Upper - profile.Maximum.Lower) / (profile.Maximum.Lower * profile.Maximum.Upper);
        if (normalizedWidth > worstNormalized) worstNormalized = normalizedWidth;
        Rational gap = (profile.Maximum.Upper - profile.Maximum.Lower) / profile.Maximum.Lower;
        if (gap > relativeGap) relativeGap = gap;
    }
    Rational maxWidth = (relativeGap + 2 * subdivision) * (1 + worstNormalized);
    // PROPOSAL (tight): the a-priori floor max T0 ≥ max(w, 1 − w) ≥ 1/2 (true maxima), less the enclosure width.
    Rational maxLower = tight ? (Rational)1 / 2 - maxWidth : (Rational)1 / 4;
    // PROPOSAL (tight): the thickness channel hull maximum instead of 1.
    Rational thicknessBound = tight ? spans["thickness"].SelectMany(span => span.Y).Max() + delta : 1 + delta;
    if (!(maxWidth < maxLower)) return null;
    Rational normalizedWidthBound = worstNormalized / maxLower + (1 + worstNormalized) * maxWidth / (maxLower * maxLower);
    var factor = Rational.From(PlacementRule.RadiansPerDegree);
    foreach (var pair in spans)
    {
        Rational tol = paths.Contains(pair.Key) ? worstProfileDelta : delta;
        foreach (var span in pair.Value)
            if (!((span.Y.Length - 1) * (span.Y.Max() - span.Y.Min()) / new Rational(BigInteger.One << 127, 1) <= tol)) return null;
    }
    Rational zWidth = worstProfileDelta + (normalizedWidthBound * thicknessBound + delta * (1 + normalizedWidthBound)) / 2;
    var profileValues = profiles.SelectMany(p => spans[p.UpperPath].Concat(spans[p.LowerPath]).SelectMany(span => span.Y));
    Rational zMagnitude = profileValues.Select(Abs).Max() + worstProfileDelta + (1 + normalizedWidthBound) * thicknessBound / 2;
    Rational factorial = 1;
    for (int i = 2; i <= 2 * PlacementRule.TaylorTerms; i++) factorial *= i;
    Rational trigWidth = delta * factor + new Rational(1, BigInteger.One << 51) + new Rational(2, BigInteger.One << PlacementRule.AngleGridBits) + (Rational)2 / factorial;
    Rational componentWidth = trigWidth + zWidth * (1 + trigWidth) + zMagnitude * trigWidth;
    Rational componentMagnitude = (1 + zMagnitude) * (1 + trigWidth);
    Rational chordMagnitude = spans["trailing"].SelectMany(span => span.Y).Max() - spans["leading"].SelectMany(span => span.Y).Min();
    Rational width = delta + 2 * delta * componentMagnitude + chordMagnitude * componentWidth;
    Rational coordinateMagnitude = spans["leading"].Concat(spans["dihedral"]).SelectMany(span => span.Y).Select(Abs).Max() +
        chordMagnitude * componentMagnitude + Rational.From(halfSpan);
    width += (coordinateMagnitude + 1) / new Rational(BigInteger.One << 51, 1);
    return width <= Rational.From(1e-8) ? width : null;
}

// Query-time bit model for the stored envelope, in QueryFeasibility's Size algebra (Geometry.cs :602–721). The
// section's size is rebuilt from Prove's own per-span witnesses exactly as Prove composes it (camber + normalized
// thickness); the stored T0 bound at w is V0 + (V1 − V0)·w with w at Prove's blend-weight size (2200, 4300); the
// normalized shape is section × 1/T0, which replaces Prove's "blend-maximum-subdivision" path.
(int Eval, int Shape) AtomQueryBits(int lineN, int lineD, QueryFeasibilityWitness witness, string[] upperPaths, string[] lowerPaths, Rational maxLo, Rational maxHi)
{
    (int N, int D) Add((int N, int D) a, (int N, int D) b) => (Math.Max(a.N + b.D, b.N + a.D) + 1, a.D + b.D);
    (int N, int D) Mul((int N, int D) a, (int N, int D) b) => (a.N + b.N, a.D + b.D);
    (int N, int D) Div((int N, int D) a, (int N, int D) b) => (a.N + b.D, a.D + b.N);
    (int N, int D) Union((int N, int D) a, (int N, int D) b) => (Math.Max(a.N, b.N), Math.Max(a.D, b.D));
    (int N, int D) Actual(Rational v) => ((int)BigInteger.Abs(v.Numerator).GetBitLength() + 1, (int)v.Denominator.GetBitLength() + 1);
    (int N, int D) Bound(IEnumerable<string> paths) => witness.Spans.Where(s => paths.Contains(s.Curve))
        .Select(s => (s.RefinedNumeratorBits, s.RefinedDenominatorBits)).Aggregate((1, 1), (x, y) => Union(x, y));
    var up = Bound(upperPaths); var lo = Bound(lowerPaths); var thickness = Bound(new[] { "thickness" });
    var maximum = Union(Actual(maxLo), Actual(maxHi));
    var reciprocal = Div((1, 1), maximum);
    var half = (1, 2);
    var camber = Mul(Add(up, lo), half);
    var normalized = Mul(Mul(Mul(Add(up, lo), reciprocal), thickness), half);
    var section = Add(camber, normalized);
    var v = (lineN + 1, lineD + 1);
    var r = Add(v, Mul(Add(v, v), (2200, 4300)));
    var shape = Mul(section, Div((1, 1), r));
    return (r.Item1 + r.Item2, Math.Max(section.Item1 + section.Item2, shape.Item1 + shape.Item2));
}

ProofBudget Unbounded()
{
    var budget = new ProofBudget();
    typeof(ProofBudget).GetField("<Limit>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(budget, 50_000_000_000L);
    return budget;
}
