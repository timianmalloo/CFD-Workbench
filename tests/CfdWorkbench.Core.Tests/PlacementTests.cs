using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class PlacementTests
{
    internal static void Run()
    {
        Check("PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged", CertificatePointBits);
        Check("PlacementRule_CertificateGoldenMaster_AssessWitnessesAndRefusalsUnchanged", CertificateAssess);
        Check("PlacementRule_RadiansConstant_SingleSiteInSource", RadiansSingleSite);
        Check("PlacementRule_TaylorAndGridConstants_SharedByAllModels", ConstantsShared);
        Check("PlacementRule_SelectBlend_SameStationsAsCertificate", SelectMatchesGolden);
        Check("PlacementRule_FoilFixtures_MatchGoldenSources", FoilFixturesMatchGolden);
        Check("Placement_DisplayWithinCertifiedEnclosure_Fixtures", () => DisplayWithin(false));
        Check("Placement_RandomFixtures_WithinCertifiedEnclosure", () => DisplayWithin(true));
        Check("Placement_DisplayMaximum_WithinCertifiedMaximum", DisplayMaximum);
        Check("Placement_FrameLeadingEdge_EqualsCertifiedPointAtXZero", FrameLeadingEdge);
        Check("Placement_SignFixture_PositiveTwistTrailingEdgeDown", PositiveTwistDown);
        Check("Placement_SignFixture_PositiveElevationRaisesSection", PositiveElevationUp);
        Check("Placement_PortHalf_MirrorsYOnly", PortMirrorsY);
        Check("Placement_Surface_AuthoredStationsIncludedExactly", AuthoredStations);
        Check("Placement_Surface_AuthoredEtaOnUniformGridOnce", UniformEtaOnce);
        Check("Placement_Surface_NeverUsesProofBudget", NeverUsesProofBudget);
        Check("Placement_Surface_EvaluatorCallsBounded", EvaluatorCallsBounded);
        Check("Placement_Surface_CancelledBetweenStations", CancelledBetweenStations);
        Check("Placement_UnparsedDraft_DslPatch", UnparsedDraft);
        Check("Placement_StationFrame_ChordDerived", ChordDerived);
        // Characterization of today's bits. The call-site fold into WingEstimates and Planform.View is PL0b.
        Check("ChannelEvaluator_WingEstimatesFold_BitsUnchanged", WingGolden);
        Check("ChannelEvaluator_PlanformViewFold_SamplesUnchanged", PlanformGolden);
        Check("Placement_ProbeChord_EqualsWingEstimatesOnPl0Fixtures", ProbeEqualsEstimates);
    }

    internal static void RunReadiness()
    {
        Check("Readiness_Surface41x101_Under25Ms", () =>
        {
            byte[] source = Encoding.UTF8.GetBytes(Example());
            _ = Placement.Surface(source, "accepted", 0, CancellationToken.None);
            var watch = Stopwatch.StartNew();
            var view = Placement.Surface(source, "accepted", 0, CancellationToken.None);
            watch.Stop();
            Console.WriteLine("MEASURE surface_41x101_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture));
            Equal(41, view.Sections.Count);
            Equal(101, view.Sections[0].Upper.Count);
            Equal(true, watch.Elapsed < TimeSpan.FromMilliseconds(25));
        });
    }

    private static void CertificatePointBits() => CertificateGolden(false);

    private static void CertificateAssess() => CertificateGolden(true);

    private static void RadiansSingleSite()
    {
        string literal = PlacementRule.RadiansPerDegree.ToString("G17", CultureInfo.InvariantCulture);
        Equal("0.017453292519943295", literal);
        string[] hits = Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("0.017453292519943295", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Equal(1, hits.Length);
        Equal("Placement.cs", hits[0]);
        // The same constant spelled as an expression is the same second site. Degrees to radians is the
        // placement rule's constant and lives in Placement.cs only. Radians to degrees is the inverse; its one
        // use is the smooth-row angle tolerance in Geometry.cs, which is not a placement.
        Equal(Bits(Math.PI / 180), Bits(PlacementRule.RadiansPerDegree));
        var toRadians = new Regex(@"(Math\.PI|double\.Pi)\s*/\s*180(\.0*)?(?![\d.])");
        var toDegrees = new Regex(@"(?<![\d.])180(\.0*)?\s*/\s*(Math\.PI|double\.Pi)");
        var spellings = new List<string>();
        foreach (string path in Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "CfdWorkbench.Core"), "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
        {
            string file = Path.GetFileName(path)!;
            if (file == "Placement.cs") continue;
            string text = File.ReadAllText(path);
            foreach (Match match in toRadians.Matches(text)) spellings.Add(file + ": " + match.Value);
            foreach (Match match in toDegrees.Matches(text)) spellings.Add(file + ": " + match.Value);
        }
        Equal("Geometry.cs: 180 / Math.PI", string.Join("; ", spellings));
    }

    private static ulong Bits(double value) => BitConverter.DoubleToUInt64Bits(value);

    private static void ConstantsShared()
    {
        string geometry = File.ReadAllText(Path.Combine(RepoRoot(), "src", "CfdWorkbench.Core", "Geometry.cs"));
        Equal(false, geometry.Contains("0.017453292519943295", StringComparison.Ordinal));
        Equal(false, geometry.Contains("DyadicDown(64)", StringComparison.Ordinal));
        Equal(false, geometry.Contains("<< 64", StringComparison.Ordinal));
        Region(geometry, "Trigonometry(RationalInterval angle)", "public static GeometryAssessment Assess", "PlacementRule.TaylorTerms");
        Region(geometry, "Rational PlacementWidth(", "Rational BlendPlacementWidth(", "PlacementRule.TaylorTerms", "PlacementRule.AngleGridBits", "PlacementRule.RadiansPerDegree");
        Region(geometry, "Rational BlendPlacementWidth(", "private static Rational Abs(", "PlacementRule.TaylorTerms", "PlacementRule.AngleGridBits", "PlacementRule.RadiansPerDegree");
        Region(geometry, "sealed class QueryFeasibility", "sealed class ProofBudget", "PlacementRule.TaylorTerms", "PlacementRule.AngleGridBits", "PlacementRule.RadiansPerDegree");
    }

    private static void Region(string source, string start, string end, params string[] required)
    {
        int at = source.IndexOf(start, StringComparison.Ordinal);
        int next = source.IndexOf(end, at + start.Length, StringComparison.Ordinal);
        if (at < 0 || next < 0) throw new InvalidOperationException("Missing region " + start);
        string slice = source[at..next];
        foreach (string token in required)
            if (!slice.Contains(token, StringComparison.Ordinal))
                throw new InvalidOperationException(start + " does not read " + token);
    }

    private static void FoilFixturesMatchGolden()
    {
        string folder = Path.Combine(RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2");
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        string[] names = document.RootElement.GetProperty("fixtures").EnumerateArray().Select(item => item.GetProperty("name").GetString()!).Order(StringComparer.Ordinal).ToArray();
        string[] files = Directory.EnumerateFiles(folder, "*.foil").Select(path => Path.GetFileNameWithoutExtension(path)!).Order(StringComparer.Ordinal).ToArray();
        Equal(string.Join(",", names), string.Join(",", files));
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            string name = fixture.GetProperty("name").GetString()!;
            Equal(fixture.GetProperty("source").GetString()!, File.ReadAllText(Path.Combine(folder, name + ".foil")).Replace("\r\n", "\n", StringComparison.Ordinal));
        }
    }

    // Degree 5, 8 points, two thickness humps of 0.0904 whose heights cross at a second-hump ordinate
    // of 0.06303158... ; the values below sit a few grid steps short of the crossover on the low side.
    private static string TwoHumpFoil(params string[] secondHumps)
    {
        const string abscissae = "0, 0, 0.1, 0.3, 0.5, 0.7, 0.9, 1";
        const string knots = "[0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1]";
        string[] x = abscissae.Split(", ");
        var profiles = new StringBuilder();
        for (int index = 0; index < secondHumps.Length; index++)
        {
            string[] y = ["0", "0.03", "0.07", "0.01", "0.01", secondHumps[index], "0.04", "0"];
            string upper = string.Join(", ", y.Select((value, at) => "(" + x[at] + ", " + value + ")"));
            string lower = string.Join(", ", y.Select((value, at) => "(" + x[at] + ", " + (value == "0" ? "0" : "-" + value) + ")"));
            profiles.Append("    profile \"section-" + (char)('a' + index) + "\" {\n      upper cv { degree 5 knots " + knots + " points [" + upper + "] }\n      lower cv { degree 5 knots " + knots + " points [" + lower + "] }\n    }\n");
        }
        string text = ReplaceCurve(Example(), "trailing", Ordinates(2000));
        int at0 = text.IndexOf("    profile \"section-a\"", StringComparison.Ordinal);
        int end = text.IndexOf("  }\n  sections", at0, StringComparison.Ordinal);
        text = text[..at0] + profiles + text[end..];
        return text.Replace("at tip profile \"section-a\"", "at tip profile \"section-" + (char)('a' + secondHumps.Length - 1) + "\"", StringComparison.Ordinal);
    }

    private static IEnumerable<(string Name, string Source)> TwoHumpFixtures()
    {
        yield return ("two-hump-2m", TwoHumpFoil("0.06303158896"));
        yield return ("two-hump-blend-2m", TwoHumpFoil("0.06303158896", "0.0630318"));
    }

    private static void SelectMatchesGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            string name = fixture.GetProperty("name").GetString()!;
            var certificate = Certify(name, fixture.GetProperty("source").GetString()!);
            double[] etas = certificate.Stations.Select(station => station.Eta.Nearest()).ToArray();
            int[] profiles = certificate.Stations.Select(station => station.Profile).ToArray();
            foreach (var probe in fixture.GetProperty("probes").EnumerateArray())
            {
                double eta = probe.GetProperty("eta").GetDouble();
                var (left, right) = PlacementRule.Select(etas, profiles, eta, (a, b) => SameProfile(certificate, a, b));
                var select = probe.GetProperty("select");
                if (select[0].GetInt32() != left || select[1].GetInt32() != right)
                    throw new InvalidOperationException(name + " eta " + eta.ToString(CultureInfo.InvariantCulture) + " select changed");
            }
        }
    }

    private static void DisplayWithin(bool random)
    {
        if (!random)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
            foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray())
                MeasureOutside(fixture.GetProperty("name").GetString()!, fixture.GetProperty("source").GetString()!);
            foreach (var (name, source) in TwoHumpFixtures())
                MeasureOutside(name, source);
            return;
        }
        string example = Example();
        var rng = new Random(20261001);
        for (int index = 0; index < 4; index++)
        {
            decimal twist = decimal.Round((decimal)(rng.NextDouble() * 80 - 40), 4, MidpointRounding.ToZero);
            decimal dihedralMm = decimal.Round((decimal)(rng.NextDouble() * 400 - 200), 3, MidpointRounding.ToZero);
            decimal chordMm = decimal.Round(50m + (decimal)rng.NextDouble() * 1950m, 3, MidpointRounding.ToZero);
            MeasureOutside("random-" + index, TwoProfiles(example, twist, dihedralMm, chordMm), false);
        }
    }

    // Fixtures sample every chord position. The random fixtures vary twist, dihedral and chord, not chord
    // resolution, so they sample the two ends and the middle: eleven samples there cost 18 s of the 60 s budget.
    private static void MeasureOutside(string name, string source, bool everySample = true)
    {
        var certificate = Certify(name, source);
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var view = Placement.Surface(bytes, "accepted", 0, CancellationToken.None, 11, 11);
        double[] xs = Cosine(view.Sections[0].Upper.Count);
        double worst = 0;
        int stride = Math.Max(1, view.Sections.Count / 5);
        for (int station = 0; station < view.Sections.Count; station += stride)
        {
            var section = view.Sections[station];
            foreach (int sample in everySample ? Enumerable.Range(0, xs.Length) : [0, xs.Length / 2, xs.Length - 1])
            {
                worst = Math.Max(worst, Outside(Geometry.PointAt(certificate, section.Eta, xs[sample], true, false), section.Upper[sample]));
                worst = Math.Max(worst, Outside(Geometry.PointAt(certificate, section.Eta, xs[sample], false, false), section.Lower[sample]));
            }
        }
        Console.WriteLine("MEASURE fixture=" + name + " max_outside_m=" + worst.ToString("G17", CultureInfo.InvariantCulture));
        Equal(true, worst <= 1e-9);
    }

    private static void DisplayMaximum()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var fixtures = document.RootElement.GetProperty("fixtures").EnumerateArray()
            .Select(fixture => (Name: fixture.GetProperty("name").GetString()!, Source: fixture.GetProperty("source").GetString()!))
            .Concat(TwoHumpFixtures());
        foreach (var (name, source) in fixtures)
        {
            var certificate = Certify(name, source);
            var profiles = Prepare(source).Definition!.Profiles;
            double worst = 0;
            for (int index = 0; index < profiles.Length; index++)
            {
                double display = Placement.ProfileDifferenceMaximum(profiles[index].Upper, profiles[index].Lower);
                var box = certificate.Profiles[index].Maximum;
                worst = Math.Max(worst, Outside(box.Lower.Down(), box.Upper.Up(), display));
            }
            Console.WriteLine("MEASURE fixture=" + name + " max_thickness_outside=" + worst.ToString("G17", CultureInfo.InvariantCulture));
            Equal(true, worst <= 1e-9);
        }
    }

    private static void FrameLeadingEdge()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            string source = fixture.GetProperty("source").GetString()!;
            var certificate = Certify(fixture.GetProperty("name").GetString()!, source);
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            var frame = Placement.Frame(bytes, 0.5);
            var point = Geometry.PointAt(certificate, 0.5, 0, true, false);
            Equal(true, frame.LeadingMeters >= point.X.Lower - 1e-9 && frame.LeadingMeters <= point.X.Upper + 1e-9);
            var view = Placement.Surface(bytes, "accepted", 0, CancellationToken.None, 5, 3);
            var section = view.Sections.Single(item => item.Eta == 0.5);
            Equal(frame.LeadingMeters, section.Upper[0].X);
        }
    }

    private static void PositiveTwistDown()
    {
        var view = Placement.Surface(Encoding.UTF8.GetBytes(Twist(Example(), 4)), "accepted", 0, CancellationToken.None, 3, 3);
        var tip = view.Sections[^1];
        Equal(true, tip.Upper[^1].Z < tip.Upper[0].Z);
        Equal(true, tip.Lower[^1].Z < tip.Lower[0].Z);
    }

    private static void PositiveElevationUp()
    {
        var view = Placement.Surface(Encoding.UTF8.GetBytes(Dihedral(Example(), 60)), "accepted", 0, CancellationToken.None, 3, 3);
        Equal(true, view.Sections[^1].Upper[0].Z > view.Sections[0].Upper[0].Z);
    }

    private static void PortMirrorsY()
    {
        var point = new Point3(0.12, 0.45, -0.02);
        var port = point.Port();
        Equal(point.X, port.X);
        Equal(0 - point.Y, port.Y);
        Equal(point.Z, port.Z);
        var view = Placement.Surface(Encoding.UTF8.GetBytes(Example()), "accepted", 0, CancellationToken.None, 3, 3);
        Equal(true, view.Sections.SelectMany(section => section.Upper.Concat(section.Lower)).All(item => item.Y >= 0));
    }

    private static void AuthoredStations()
    {
        var view = MeshWithStations();
        double[] etas = view.Sections.Select(section => section.Eta).ToArray();
        for (int index = 1; index < etas.Length; index++) Equal(true, etas[index] > etas[index - 1]);
        Equal(42, etas.Length);
        Equal(1, etas.Count(eta => eta == AuthoredOffGrid()));
        double[] xs = Cosine(view.Sections[0].Upper.Count);
        for (int index = 0; index < xs.Length; index++)
            Equal(true, Math.Abs(view.Sections[0].Upper[index].X - xs[index] * 0.12) <= 1e-12);
    }

    private static void UniformEtaOnce()
    {
        double[] etas = MeshWithStations().Sections.Select(section => section.Eta).ToArray();
        for (int index = 0; index < 41; index++) Equal(1, etas.Count(eta => eta == index / 40d));
    }

    private static void NeverUsesProofBudget()
    {
        int before = ProofBudget.Entries;
        _ = Placement.Surface(Encoding.UTF8.GetBytes(Example()), "accepted", 0, CancellationToken.None);
        Equal(before, ProofBudget.Entries);
    }

    private static void EvaluatorCallsBounded()
    {
        Placement.ResetEvaluatorCounts();
        var view = Placement.Surface(Encoding.UTF8.GetBytes(Example()), "accepted", 0, CancellationToken.None);
        int perPoint = view.Sections.Count * view.Sections[0].Upper.Count * 2 * 60;
        Console.WriteLine("MEASURE channel_evals=" + Placement.ChannelEvaluations + " profile_evals=" + Placement.ProfileEvaluations + " per_point=" + perPoint);
        Equal(5 * view.Sections.Count, Placement.ChannelEvaluations);
        Equal(true, Placement.ProfileEvaluations > 0 && Placement.ProfileEvaluations < perPoint / 2);
    }

    private static void CancelledBetweenStations()
    {
        using var cancel = new CancellationTokenSource();
        int seen = 0;
        Placement.AfterStation = () => { if (++seen == 1) cancel.Cancel(); };
        try
        {
            Placement.Surface(Encoding.UTF8.GetBytes(Example()), "accepted", 0, cancel.Token);
            throw new InvalidOperationException("Surface ran past cancellation.");
        }
        catch (OperationCanceledException) { Equal(true, seen >= 1); }
        finally { Placement.AfterStation = null; }
    }

    private static void UnparsedDraft()
    {
        byte[] section = File.ReadAllBytes(Path.Combine(RepoRoot(), "docs", "examples", "foildsl", "section-basic.foil"));
        try
        {
            Placement.Surface(section, "accepted", 0, CancellationToken.None);
            throw new InvalidOperationException("A section draft produced a wing surface.");
        }
        catch (ContractError error) { Equal("DSL-PATCH", error.Code); }
    }

    private static void ChordDerived()
    {
        var frame = Placement.Frame(Encoding.UTF8.GetBytes(Example()), 0);
        Equal(frame.TrailingMeters - frame.LeadingMeters, frame.ChordMeters);
        Equal(true, Math.Abs(frame.ChordMeters - 0.12) <= 1e-12);
        Equal(0d, frame.LeadingMeters);
    }

    private static SurfaceView MeshWithStations() =>
        Placement.Surface(Encoding.UTF8.GetBytes(WithStations(Example())), "accepted", 7, CancellationToken.None);

    private static double AuthoredOffGrid()
    {
        var parsed = Prepare(WithStations(Example()));
        return parsed.Definition!.Assignments.Single(assignment => assignment.Profile == 0 && assignment.Eta > 0 && assignment.Eta < 0.5).Eta;
    }

    private static string WithStations(string text) => text.Replace(
        "sections { at root profile \"section-a\" at tip profile \"section-a\" }",
        "sections { at root profile \"section-a\" at 31 % profile \"section-a\" at 50 % profile \"section-a\" at tip profile \"section-a\" }",
        StringComparison.Ordinal);

    private static string Example()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        return document.RootElement.GetProperty("fixtures")[0].GetProperty("source").GetString()!;
    }

    private static string TwoProfiles(string text, decimal twist, decimal dihedralMillimetres, decimal chordMillimetres)
    {
        text = Twist(text, twist);
        text = Dihedral(text, dihedralMillimetres);
        text = ReplaceCurve(text, "trailing", Ordinates(chordMillimetres));
        const string second = """
                profile "section-b" {
                  upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.02), (0.15, 0.04), (0.35, 0.05), (0.55, 0.04), (0.75, 0.02), (0.9, 0.008), (1, 0)] }
                  lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.02), (0.15, -0.04), (0.35, -0.05), (0.55, -0.04), (0.75, -0.02), (0.9, -0.008), (1, 0)] }
                }
            
            """;
        return text.Replace("  profiles {\n    profile \"section-a\" {", "  profiles {\n" + second + "    profile \"section-a\" {", StringComparison.Ordinal)
            .Replace("at tip profile \"section-a\"", "at tip profile \"section-b\"", StringComparison.Ordinal);
    }

    private static string Twist(string text, decimal degrees) => ReplaceCurve(text, "twist", Ordinates(degrees));

    private static string Dihedral(string text, decimal tipMillimetres)
    {
        string points = string.Join(", ", new[] { 0m, 0.1m, 0.3m, 0.5m, 0.7m, 0.9m, 1m }
            .Select((eta, index) => "(" + eta.ToString(CultureInfo.InvariantCulture) + ", " +
                (index < 2 ? 0m : eta * tipMillimetres).ToString(CultureInfo.InvariantCulture) + ")"));
        return ReplaceCurve(text, "dihedral", points);
    }

    private static string Ordinates(decimal value)
    {
        string spelled = value.ToString(CultureInfo.InvariantCulture);
        return string.Join(", ", new[] { "0", "0.1", "0.3", "0.5", "0.7", "0.9", "1" }.Select(eta => "(" + eta + ", " + spelled + ")"));
    }

    private static string ReplaceCurve(string text, string channel, string points)
    {
        const string knots = "degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [";
        int at = text.IndexOf(channel + " cv { " + knots, StringComparison.Ordinal);
        if (at < 0) throw new InvalidOperationException("Missing " + channel);
        int pointsAt = at + (channel + " cv { " + knots).Length;
        int end = text.IndexOf(']', pointsAt);
        return text[..pointsAt] + points + text[end..];
    }

    private static GeometryCertificate Certify(string name, string source)
    {
        if (Certificates.TryGetValue(name, out var cached)) return cached;
        var assessment = Geometry.Assess(Prepare(source));
        var certificate = assessment.Certificate ?? throw new InvalidOperationException(name + " " + assessment.Status + " " + assessment.Reason);
        Certificates[name] = certificate;
        return certificate;
    }

    private static readonly Dictionary<string, GeometryCertificate> Certificates = new(StringComparer.Ordinal);

    private static bool SameProfile(GeometryCertificate certificate, int left, int right)
    {
        if (left == right) return true;
        var a = certificate.Profiles[left];
        var b = certificate.Profiles[right];
        return SameSpans(certificate.Spans[a.UpperPath], certificate.Spans[b.UpperPath])
            && SameSpans(certificate.Spans[a.LowerPath], certificate.Spans[b.LowerPath]);
    }

    private static bool SameSpans(PolynomialSpan[] left, PolynomialSpan[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index].Start.CompareTo(right[index].Start) != 0 || left[index].End.CompareTo(right[index].End) != 0) return false;
            if (!SameCoefficients(left[index].X, right[index].X) || !SameCoefficients(left[index].Y, right[index].Y)) return false;
        }
        return true;
    }

    private static bool SameCoefficients(Rational[] left, Rational[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++) if (left[index].CompareTo(right[index]) != 0) return false;
        return true;
    }

    private static double Outside(PlacedPointEnclosure box, Point3 point) =>
        Math.Max(Outside(box.X.Lower, box.X.Upper, point.X), Math.Max(Outside(box.Y.Lower, box.Y.Upper, point.Y), Outside(box.Z.Lower, box.Z.Upper, point.Z)));

    private static double Outside(double lower, double upper, double value)
    {
        if (value < lower) return lower - value;
        if (value > upper) return value - upper;
        return 0;
    }

    private static double[] Cosine(int count)
    {
        var samples = new double[count];
        samples[0] = 0;
        samples[^1] = 1;
        for (int index = 1; index < count - 1; index++)
            samples[index] = (1 - Math.Cos(Math.PI * index / (count - 1))) / 2;
        return samples;
    }

    private static string RepoRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static void CertificateGolden(bool assess)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var root = document.RootElement;
        foreach (var fixture in root.GetProperty("fixtures").EnumerateArray())
        {
            string name = fixture.GetProperty("name").GetString()!;
            var assessment = Geometry.Assess(Prepare(fixture.GetProperty("source").GetString()!));
            var certificate = assessment.Certificate ?? throw new InvalidOperationException(name + " did not certify");
            if (assess)
            {
                Same(name + " status", fixture.GetProperty("status").GetString(), assessment.Status.ToString());
                Same(name + " code", fixture.GetProperty("code").GetString(), assessment.Code);
                Same(name + " placement width", fixture.GetProperty("placementWidthUpper").GetString(), Hex(certificate.PlacementWidthUpper));
                var width = fixture.GetProperty("exactPlacementWidth");
                Same(name + " exact width numerator", width.GetProperty("n").GetString(), certificate.ExactPlacementWidthUpper.Numerator);
                Same(name + " exact width denominator", width.GetProperty("d").GetString(), certificate.ExactPlacementWidthUpper.Denominator);
                var thickness = fixture.GetProperty("thicknessMaximum");
                Same(name + " thickness lower", thickness.GetProperty("lower").GetString(), Hex(certificate.ThicknessMaximumLower));
                Same(name + " thickness upper", thickness.GetProperty("upper").GetString(), Hex(certificate.ThicknessMaximumUpper));
                SameFeasibility(name, fixture.GetProperty("feasibility"), certificate.QueryFeasibility);
                SameWitnesses(name, fixture.GetProperty("witnesses"), certificate.Witnesses);
            }
            if (assess) continue;
            foreach (var probe in fixture.GetProperty("probes").EnumerateArray())
            {
                double eta = probe.GetProperty("eta").GetDouble();
                double x = probe.GetProperty("x").GetDouble();
                bool upper = probe.GetProperty("upper").GetBoolean();
                bool port = probe.GetProperty("port").GetBoolean();
                string where = name + " eta " + eta + " x " + x + " upper " + upper + " port " + port;
                var point = Geometry.PointAt(certificate, eta, x, upper, port);
                var expectedPoint = probe.GetProperty("point");
                SameOrdinate(where + " X", expectedPoint.GetProperty("x"), point.X);
                SameOrdinate(where + " Y", expectedPoint.GetProperty("y"), point.Y);
                SameOrdinate(where + " Z", expectedPoint.GetProperty("z"), point.Z);
                var section = Geometry.SectionAt(certificate, eta, x);
                SameOrdinate(where + " section upper", probe.GetProperty("section").GetProperty("upper"), section.Upper);
                SameOrdinate(where + " section lower", probe.GetProperty("section").GetProperty("lower"), section.Lower);
            }
        }
        if (!assess) return;
        string example = root.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        foreach (var refusal in root.GetProperty("refusals").EnumerateArray())
            SameRefusal(refusal, example);
    }

    private static void ProbeEqualsEstimates()
    {
        string folder = Path.Combine(RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2");
        string[] files = Directory.EnumerateFiles(folder, "*.foil").Order(StringComparer.Ordinal).ToArray();
        Equal(true, files.Length >= 1);
        foreach (string path in files)
        {
            byte[] source = File.ReadAllBytes(path);
            var parsed = FoilSource.Parse(source);
            if (parsed.Definition!.Curves.Values.Any(curve => curve.MissingIds))
                source = FoilSource.MaterializeIds(parsed);
            for (int sample = 0; sample <= 1000; sample++)
            {
                double eta = sample / 1000d;
                double frame = Placement.Frame(source, eta).ChordMeters;
                double wing = WingEstimates.ChordMeters(source, eta);
                if (frame != wing)
                    throw new InvalidOperationException(Path.GetFileName(path) + " chord bits differ at η " + eta.ToString("G17", CultureInfo.InvariantCulture));
            }
        }
    }

    private static void WingGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var expected = document.RootElement.GetProperty("wingEstimates");
        string example = document.RootElement.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        var wing = WingEstimates.From(FoilSource.MaterializeIds(Prepare(example)), "accepted", 0);
        Same("wing span", expected.GetProperty("span").GetString(), Hex(wing.SpanMeters));
        Same("wing root", expected.GetProperty("rootChord").GetString(), Hex(wing.RootChordMeters));
        Same("wing tip", expected.GetProperty("tipChord").GetString(), Hex(wing.TipChordMeters));
        Same("wing mean", expected.GetProperty("meanChord").GetString(), Hex(wing.MeanChordMeters));
        Same("wing mac", expected.GetProperty("mac").GetString(), Hex(wing.MacMeters));
        Same("wing thickness", expected.GetProperty("maxThickness").GetString(), Hex(wing.MaxThicknessRatio));
        Same("wing aspect", expected.GetProperty("aspect").GetString(), Hex(wing.AspectRatio));
        Same("wing area", expected.GetProperty("area").GetString(), Hex(wing.AreaSquareMeters));
        Same("wing basis", expected.GetProperty("basis").GetString(), wing.Basis);
        Equal(expected.GetProperty("generation").GetInt64(), wing.Generation);
        Equal(expected.GetProperty("converged").GetBoolean(), wing.Converged);
        Equal(expected.GetProperty("intervals").GetInt32(), wing.Compute.IntervalCount);
        Equal(expected.GetProperty("iterations").GetInt32(), wing.Compute.Iterations);
        Same("wing outcome", expected.GetProperty("outcome").GetString(), wing.Compute.Outcome);
    }

    private static void PlanformGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var expected = document.RootElement.GetProperty("planform");
        string example = document.RootElement.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        var plan = Planform.View(FoilSource.MaterializeIds(Prepare(example)), "accepted", 0);
        Same("plan half span", expected.GetProperty("halfSpan").GetString(), Hex(plan.HalfSpanMeters));
        SameSamples("plan leading", expected.GetProperty("leading"), plan.Leading.Samples);
        SameSamples("plan trailing", expected.GetProperty("trailing"), plan.Trailing.Samples);
        var stations = expected.GetProperty("stations").EnumerateArray().ToArray();
        Equal(stations.Length, plan.Stations.Count);
        for (int index = 0; index < stations.Length; index++)
        {
            Same("plan station eta " + index, stations[index].GetProperty("eta").GetString(), Hex(plan.Stations[index].Eta));
            Same("plan station span " + index, stations[index].GetProperty("span").GetString(), Hex(plan.Stations[index].SpanMeters));
        }
    }

    private static void SameFeasibility(string name, JsonElement expected, QueryFeasibilityWitness actual)
    {
        Same(name + " algorithm", expected.GetProperty("Algorithm").GetString(), actual.Algorithm);
        Equal(expected.GetProperty("RationalBitLimit").GetInt32(), actual.RationalBitLimit);
        Equal(expected.GetProperty("MaximumIntermediateBits").GetInt32(), actual.MaximumIntermediateBits);
        Equal(expected.GetProperty("RationalOperationsUpper").GetInt64(), actual.RationalOperationsUpper);
        Equal(expected.GetProperty("InverseDepth").GetInt32(), actual.InverseDepth);
        Equal(expected.GetProperty("AngleGridBits").GetInt32(), actual.AngleGridBits);
        Same(name + " bit path", expected.GetProperty("MaximumBitPath").GetString(), actual.MaximumBitPath);
        var spans = expected.GetProperty("spans").EnumerateArray().ToArray();
        Equal(spans.Length, actual.Spans.Count);
        for (int index = 0; index < spans.Length; index++)
        {
            var span = spans[index];
            var got = actual.Spans[index];
            Same(name + " span curve " + index, span.GetProperty("Curve").GetString(), got.Curve);
            Equal(span.GetProperty("Span").GetInt32(), got.Span);
            Equal(span.GetProperty("Degree").GetInt32(), got.Degree);
            Equal(span.GetProperty("CommonDenominatorBits").GetInt32(), got.CommonDenominatorBits);
            Equal(span.GetProperty("RefinedNumeratorBits").GetInt32(), got.RefinedNumeratorBits);
            Equal(span.GetProperty("RefinedDenominatorBits").GetInt32(), got.RefinedDenominatorBits);
        }
    }

    private static void SameWitnesses(string name, JsonElement expected, IReadOnlyList<GeometryWitness> actual)
    {
        var rows = expected.EnumerateArray().ToArray();
        Equal(rows.Length, actual.Count);
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var got = actual[index];
            Same(name + " witness " + index, row.GetProperty("Curve").GetString(), got.Curve);
            SameRatio(name + " witness " + index + " start", row.GetProperty("domainStart"), got.DomainStart);
            SameRatio(name + " witness " + index + " end", row.GetProperty("domainEnd"), got.DomainEnd);
            SameRatio(name + " witness " + index + " abscissa lower", row.GetProperty("abscissaLower"), got.AbscissaDifferenceLower);
            SameRatio(name + " witness " + index + " abscissa upper", row.GetProperty("abscissaUpper"), got.AbscissaDifferenceUpper);
            SameRatio(name + " witness " + index + " ordinate lower", row.GetProperty("ordinateLower"), got.OrdinateLower);
            SameRatio(name + " witness " + index + " ordinate upper", row.GetProperty("ordinateUpper"), got.OrdinateUpper);
        }
    }

    private static void SameRefusal(JsonElement expected, string example)
    {
        string name = expected.GetProperty("name").GetString()!;
        var parsed = Prepare(example);
        var certificate = Geometry.Assess(parsed).Certificate ?? throw new InvalidOperationException("example did not certify");
        (string Kind, string Code, string Status, string Reason) actual = name switch
        {
            "point-budget" => Caught(() => Geometry.PointAt(certificate, .5, .5, true, timeBudget: TimeSpan.Zero)),
            "section-budget" => Caught(() => Geometry.SectionAt(certificate, .5, .5, TimeSpan.Zero)),
            "point-cancel" => Caught(() => Geometry.PointAt(certificate, .5, .5, true, cancellationToken: new CancellationToken(true))),
            "section-cancel" => Caught(() => Geometry.SectionAt(certificate, .5, .5, cancellationToken: new CancellationToken(true))),
            "assess-budget" => Assessed(example, TimeSpan.Zero),
            "taylor-domain" => Assessed(example.Replace("(1, -2)", "(1, -90)", StringComparison.Ordinal), null),
            "ten-nanometre-budget" => Assessed(Trailing(example), null),
            _ => throw new InvalidOperationException("Unknown refusal " + name),
        };
        Same(name + " kind", expected.GetProperty("kind").GetString(), actual.Kind);
        Same(name + " code", expected.GetProperty("code").GetString(), actual.Code);
        Same(name + " status", expected.GetProperty("status").GetString(), actual.Status);
        Same(name + " reason", expected.GetProperty("reason").GetString(), actual.Reason);
    }

    private static (string Kind, string Code, string Status, string Reason) Caught(Action action)
    {
        try { action(); }
        catch (ContractError error) { return ("contract", error.Code, "", error.Message); }
        throw new InvalidOperationException("Expected a contract refusal.");
    }

    private static (string Kind, string Code, string Status, string Reason) Assessed(string text, TimeSpan? budget)
    {
        var parsed = Prepare(text);
        if (!parsed.IsParsed) return ("parse", parsed.Diagnostics[0].Code, "", parsed.Diagnostics[0].Reason);
        var assessment = Geometry.Assess(parsed, budget);
        return ("assess", assessment.Code, assessment.Status.ToString(), assessment.Reason);
    }

    private static void SameSamples(string where, JsonElement expected, IReadOnlyList<PlanSample> actual)
    {
        var rows = expected.EnumerateArray().ToArray();
        Equal(rows.Length, actual.Count);
        for (int index = 0; index < rows.Length; index++)
        {
            Same(where + " span " + index, rows[index].GetProperty("span").GetString(), Hex(actual[index].SpanMeters));
            Same(where + " aft " + index, rows[index].GetProperty("aft").GetString(), Hex(actual[index].AftMeters));
        }
    }

    private static void SameOrdinate(string where, JsonElement expected, EnclosedOrdinate actual)
    {
        Same(where + " lower", expected[0].GetString(), Hex(actual.Lower));
        Same(where + " upper", expected[1].GetString(), Hex(actual.Upper));
    }

    private static void SameRatio(string where, JsonElement expected, ExactRatio actual)
    {
        Same(where + " n", expected.GetProperty("n").GetString(), actual.Numerator);
        Same(where + " d", expected.GetProperty("d").GetString(), actual.Denominator);
    }

    private static void Same(string where, string? expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException(where + " changed. Certificate bits changed. Review the hand-kept models QueryFeasibility, PlacementWidth and BlendPlacementWidth.");
    }

    private static string Trailing(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int index = 0; index < lines.Length; index++)
            if (lines[index].StartsWith("    trailing cv ", StringComparison.Ordinal))
                lines[index] = lines[index].Replace(", 120)", ", 1e20)", StringComparison.Ordinal);
        return string.Join('\n', lines);
    }

    private static SourceParse Prepare(string text)
    {
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        if (!parsed.IsParsed) return parsed;
        return FoilSource.Parse(FoilSource.MaterializeIds(parsed));
    }

    private static string Hex(double value) => BitConverter.DoubleToUInt64Bits(value).ToString("x16");

    private static string GoldenPath([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "docs", "proof", "m12b2-golden", "certificate-bits.json"));
}
