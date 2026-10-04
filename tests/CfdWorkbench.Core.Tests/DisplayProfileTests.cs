using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// M1.2c display profile (docs/design/m12c-section-editor.md §12.4, track DSP; ADR-0010 Amendment 1).
internal static class DisplayProfileTests
{
    private const double Binding = 1e-9;

    internal static void Run()
    {
        Check("ProfileEvaluator_BoundToCertificate_Within1e9Chord", BoundToCertificate);
        Check("ProfileEvaluator_DisplayMaximum_WithinCertifiedMaximum", DisplayMaximum);
        Check("ProfileEvaluator_SingleSite_NoOtherProfileInversionInSource", SingleSite);
        Check("ProfileView_Samples_CosineSpacedAtNose", CosineSpaced);
        Check("ProfileView_RebuiltProfile_NeverUsesProofBudget", NeverUsesProofBudget);
        Check("Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged", SurfaceBitsUnchanged);
    }

    internal static void RunReadiness()
    {
        Check("Readiness_ProfileViewRebuilt_Under5Ms", RebuiltUnder5Ms);
    }

    private static void BoundToCertificate()
    {
        foreach (var (name, path) in Fixtures())
        {
            byte[] source = File.ReadAllBytes(path);
            var profile = Profile(source);
            if (name == "rebuilt-nondyadic")
            {
                string text = File.ReadAllText(path);
                Equal(true, text.Contains("0.200000000000000011102230246251565404236316680908203125", StringComparison.Ordinal));
                Equal(true, profile.Upper.Knots.Any(knot => !ShortDyadic(knot)));
            }
            if (name == "c0-knot") Equal(true, InteriorMultiplicity(profile.Upper.Knots) >= 5);
            if (name == "le-vertical") VerticalLeadingEdge(profile.Upper);
            if (name == "section-a") Equal("section-a", profile.Name);
            BoundSide(name, profile.Upper);
            BoundSide(name, profile.Lower);
        }
    }

    private static void BoundSide(string name, Curve curve)
    {
        var samples = ProfileEvaluator.Samples(curve, 101);
        var watch = new ProofBudget();
        var spans = Bernstein.Spans(curve, watch);
        // The enclosure is requested tighter than the 1e-9 chord allowance, so a sample sitting in a wide box cannot pass.
        var accuracy = Rational.From(1e-12);
        foreach (var sample in samples)
        {
            var box = Bernstein.EncloseAt(spans, Rational.From(sample.X), watch, accuracy);
            double outside = Outside(box.Lower.Down(), box.Upper.Up(), sample.Y);
            if (outside > Binding)
                throw new InvalidOperationException(name + " x=" + sample.X.ToString("G17", CultureInfo.InvariantCulture)
                    + " outside=" + outside.ToString("G17", CultureInfo.InvariantCulture));
        }
    }

    private static void DisplayMaximum()
    {
        foreach (var (name, path) in Fixtures())
        {
            var parsed = Parse(File.ReadAllBytes(path));
            var assessment = Geometry.Assess(parsed);
            var certificate = assessment.Certificate ?? throw new InvalidOperationException(name + " did not certify: " + assessment.Code);
            var profiles = parsed.Definition!.Profiles;
            Equal(profiles.Length, certificate.Profiles.Length);
            for (int index = 0; index < profiles.Length; index++)
            {
                double display = Placement.ProfileDifferenceMaximum(profiles[index].Upper, profiles[index].Lower);
                var box = certificate.Profiles[index].Maximum;
                double outside = Outside(box.Lower.Down(), box.Upper.Up(), display);
                if (outside > Binding)
                    throw new InvalidOperationException(name + " maximum outside=" + outside.ToString("G17", CultureInfo.InvariantCulture));
            }
        }
    }

    // The display inversion is ParameterFor, once, inside ProfileEvaluator. Every other bracket bisection
    // already in src/ is pinned here (channel η, the fair residual, the two fit inversions, a slope root).
    // A new loop is the extra profile inversion the design forbids. y-at-chord-x exists only in
    // ProfileEvaluator and FoilSource's fair residual.
    private static void SingleSite()
    {
        string root = Path.Combine(PlacementTests.RepoRoot(), "src");
        var loop = new Regex(@"for\s*\(\s*int\s+step\s*=\s*0;\s*step\s*<\s*(\d+);\s*step\+\+\s*\)");
        var signature = new Regex(@"(?:private|internal|public)\s+static\s+[\w<>\[\],\s]+?\s+(\w+)\s*\(");
        var shrink = new Regex(@"\b(?:lo|low|hi|high)\s*=\s*mid(?:dle)?\b");
        var hits = new List<string>();
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(path);
            string file = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            foreach (Match match in loop.Matches(text))
            {
                int to = Math.Min(text.Length, match.Index + 800);
                if (!shrink.IsMatch(text[match.Index..to])) continue;
                var names = signature.Matches(text[..match.Index]);
                string method = names.Count == 0 ? "?" : names[^1].Groups[1].Value;
                hits.Add(file + " " + method + " " + match.Groups[1].Value);
            }
        }
        Equal(string.Join("\n", new[]
        {
            "CfdWorkbench.Core/AuthoringSession.cs MakeAnchor 64",
            "CfdWorkbench.Core/ChordDimension.cs ParameterAt 60",
            "CfdWorkbench.Core/ConstrainedFit.cs ParameterAt 50",
            "CfdWorkbench.Core/DatImport.cs ParameterAt 60",
            "CfdWorkbench.Core/FoilSource.cs OrdinateAt 80",
            "CfdWorkbench.Core/FoilSource.cs ParameterAt 80",
            "CfdWorkbench.Core/Placement.cs Parameter 60",
            "CfdWorkbench.Core/Placement.cs ParameterFor 60",
            "CfdWorkbench.Core/ThicknessFit.cs Parameter 60",
            "CfdWorkbench.Core/WingEstimates.cs Root 50",
        }), string.Join("\n", hits));
        string placement = File.ReadAllText(Path.Combine(root, "CfdWorkbench.Core", "Placement.cs"));
        string foil = File.ReadAllText(Path.Combine(root, "CfdWorkbench.Core", "FoilSource.cs"));
        Equal(1, Regex.Matches(placement, @"OrdinateAt\(Curve curve, double x\)").Count);
        Equal(1, Regex.Matches(foil, @"OrdinateAt\(Curve curve, double x\)").Count);
        int ordinateAtX = 0;
        foreach (string path in Directory.EnumerateFiles(Path.Combine(root, "CfdWorkbench.Core"), "*.cs"))
            ordinateAtX += Regex.Matches(File.ReadAllText(path), @"OrdinateAt\(Curve curve, double x\)").Count;
        Equal(2, ordinateAtX);
        Equal(1, Regex.Matches(placement, @"class ProfileEvaluator\b").Count);
        Equal(1, Regex.Matches(placement, @"ParameterFor\(Curve curve, double x\)").Count);
        string session = File.ReadAllText(Path.Combine(root, "CfdWorkbench.Core", "AuthoringSession.cs"));
        string sample = Between(session, "internal static ProfilePoint[] Sample", "private static BlendInterval[] MergeIntervals");
        Equal(false, sample.Contains("Bernstein", StringComparison.Ordinal));
        Equal(true, sample.Contains("ProfileEvaluator.Samples", StringComparison.Ordinal));
        string core = Between(session, "private ProfileView ProfileAtCore", "private ScopeImpact DescribeScopeCore");
        Equal(false, core.Contains("ProofBudget", StringComparison.Ordinal));
        Equal(true, core.Contains("Sample(profile.Upper)", StringComparison.Ordinal));
        Equal(true, core.Contains("Sample(profile.Lower)", StringComparison.Ordinal));
    }

    private static void CosineSpaced()
    {
        byte[] source = File.ReadAllBytes(FixturePath("section-a"));
        using var session = Open(source);
        var view = session.ProfileAt(0);
        Equal(101, view.UpperCurve.Count);
        Equal(101, view.LowerCurve.Count);
        var profile = Profile(source);
        for (int index = 0; index < 101; index++)
        {
            double x = Cosine(index, 101);
            Equal(Bits(x), Bits(view.UpperCurve[index].X));
            Equal(Bits(x), Bits(view.LowerCurve[index].X));
            Equal(Bits(ProfileEvaluator.OrdinateAt(profile.Upper, x)), Bits(view.UpperCurve[index].Y));
            Equal(Bits(ProfileEvaluator.OrdinateAt(profile.Lower, x)), Bits(view.LowerCurve[index].Y));
        }
        Equal(true, view.UpperCurve[1].X < view.UpperCurve[51].X - view.UpperCurve[50].X);
        Equal(true, view.UpperCurve[1].X < 1d / 100);
    }

    private static void NeverUsesProofBudget()
    {
        byte[] source = File.ReadAllBytes(FixturePath("rebuilt-nondyadic"));
        using var session = Open(source);
        int entries = ProofBudget.Entries;
        _ = session.ProfileAt(0);
        Equal(entries, ProofBudget.Entries);
        var profile = Profile(source);
        var budget = new ProofBudget();
        long spent = budget.Spent;
        _ = AuthoringSession.Sample(profile.Upper, budget);
        _ = AuthoringSession.Sample(profile.Lower, budget);
        Equal(spent, budget.Spent);
    }

    private static void RebuiltUnder5Ms()
    {
        // The retired proof sample was ~450 ms per side. value_ms is the best of five warmed ProfileAt
        // calls on the rebuilt profile (both sides, including the parse). A collection can pause one call.
        byte[] source = File.ReadAllBytes(FixturePath("rebuilt-nondyadic"));
        using var session = Open(source);
        _ = session.ProfileAt(0);
        double best = double.PositiveInfinity;
        for (int index = 0; index < 5; index++)
        {
            var once = Stopwatch.StartNew();
            _ = session.ProfileAt(0);
            once.Stop();
            if (once.Elapsed.TotalMilliseconds < best) best = once.Elapsed.TotalMilliseconds;
        }
        Console.WriteLine("READINESS ProfileViewRebuilt value_ms=" + best.ToString("G17", CultureInfo.InvariantCulture));
        Equal(true, best < 5);
    }

    // Captured before ProfileEvaluator left Placement's private methods. A bit change in the placed
    // mesh fails here even when the certificate golden stays green.
    private static void SurfaceBitsUnchanged()
    {
        string root = PlacementTests.RepoRoot();
        string folder = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2");
        string golden = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12c", "display", "placement-surface-bits.txt");
        var lines = new List<string>();
        foreach (string path in Directory.EnumerateFiles(folder, "*.foil").Order(StringComparer.Ordinal))
        {
            var view = Placement.Surface(File.ReadAllBytes(path), "accepted", 0, CancellationToken.None);
            lines.Add(Path.GetFileName(path) + " " + Hash(view));
        }
        Equal(File.ReadAllText(golden).ReplaceLineEndings("\n").TrimEnd('\n'), string.Join("\n", lines));
    }

    private static string Hash(SurfaceView view)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buf = new byte[8];
        void D(double value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buf, BitConverter.DoubleToInt64Bits(value));
            sha.AppendData(buf);
        }
        D(view.MinimumX); D(view.MinimumY); D(view.MinimumZ);
        D(view.MaximumX); D(view.MaximumY); D(view.MaximumZ);
        foreach (var section in view.Sections)
        {
            D(section.Eta);
            BinaryPrimitives.WriteInt32LittleEndian(buf, section.Assignment ?? int.MinValue);
            sha.AppendData(buf.AsSpan(0, 4));
            foreach (var point in section.Upper.Concat(section.Lower))
            {
                D(point.X); D(point.Y); D(point.Z);
            }
        }
        return Convert.ToHexString(sha.GetHashAndReset());
    }

    private static IEnumerable<(string Name, string Path)> Fixtures()
    {
        yield return ("section-a", FixturePath("section-a"));
        yield return ("rebuilt-nondyadic", FixturePath("rebuilt-nondyadic"));
        yield return ("c0-knot", FixturePath("c0-knot"));
        yield return ("le-vertical", FixturePath("le-vertical"));
    }

    private static string FixturePath(string name) => name == "section-a"
        ? Path.Combine(PlacementTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil")
        : Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12c", "display", name + ".foil");

    private static AuthoringSession Open(byte[] source)
    {
        var session = new AuthoringSession();
        session.Open(source, Guid.NewGuid().ToString("D"), true);
        return session;
    }

    private static SourceParse Parse(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is null)
            throw new InvalidOperationException(parsed.Diagnostics.Count > 0 ? parsed.Diagnostics[0].Code : "DSL-PATCH");
        if (parsed.Definition.Curves.Values.Any(curve => curve.MissingIds))
            parsed = FoilSource.Parse(FoilSource.MaterializeIds(parsed));
        return parsed;
    }

    private static ProfileDefinition Profile(byte[] source)
    {
        var parsed = Parse(source);
        return parsed.Definition!.Profiles[parsed.Definition.Assignments[0].Profile];
    }

    private static double Cosine(int index, int count)
    {
        if (index == 0) return 0;
        if (index == count - 1) return 1;
        return (1 - Math.Cos(Math.PI * index / (count - 1))) / 2;
    }

    private static bool ShortDyadic(double knot)
    {
        double scaled = knot * 16;
        return scaled == Math.Round(scaled);
    }

    private static int InteriorMultiplicity(double[] knots)
    {
        int run = 1, best = 1;
        for (int index = 1; index < knots.Length; index++)
        {
            if (knots[index] == knots[index - 1] && knots[index] > 0 && knots[index] < 1) { run++; best = Math.Max(best, run); }
            else run = 1;
        }
        return best;
    }

    private static void VerticalLeadingEdge(Curve curve)
    {
        Equal(0d, curve.Points[0][0]);
        Equal(0d, curve.Points[1][0]);
        Equal(true, curve.Points[0][1] != curve.Points[1][1]);
    }

    private static double Outside(double lower, double upper, double value)
    {
        if (value < lower) return lower - value;
        if (value > upper) return value - upper;
        return 0;
    }

    private static string Between(string text, string start, string end)
    {
        int from = text.IndexOf(start, StringComparison.Ordinal);
        int to = text.IndexOf(end, from, StringComparison.Ordinal);
        if (from < 0 || to < 0) throw new InvalidOperationException("Missing " + start);
        return text[from..to];
    }

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);
}
