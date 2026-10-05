using System.Diagnostics;
using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class BlendTests
{
    internal static void Run()
    {
        Check("Blend_TwoProfiles_Certified", () =>
        {
            var assessment = Geometry.Assess(Prepared(Cambered()));
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.Certificate is not null);
        });
        Check("Blend_Endpoints_EqualStationProfiles", () =>
        {
            var blended = Geometry.Assess(Prepared(Cambered())).Certificate!;
            var root = Geometry.Assess(Prepared(Only("a"))).Certificate!;
            var tip = Geometry.Assess(Prepared(Only("b"))).Certificate!;
            foreach (double x in new[] { 0d, Peak, .5, 1d })
            {
                Agree(Geometry.SectionAt(blended, 0, x), Geometry.SectionAt(root, 0, x));
                Agree(Geometry.SectionAt(blended, 1, x), Geometry.SectionAt(tip, 1, x));
                Agree(Geometry.PointAt(blended, 0, x, true), Geometry.PointAt(root, 0, x, true));
                Agree(Geometry.PointAt(blended, 1, x, false, true), Geometry.PointAt(tip, 1, x, false, true));
            }
        });
        Check("Blend_IdenticalProfileBytes_MatchSingleProfileExample", () =>
        {
            var example = Geometry.Assess(Prepared(Text)).Certificate!;
            var renamed = Geometry.Assess(Prepared(DuplicateExample("at root profile \"section-a\" at tip profile \"section-b\""))).Certificate!;
            foreach (double eta in new[] { 0d, .25, .5, 1d })
                foreach (double x in new[] { 0d, .25, .5, 1d })
                {
                    Agree(Geometry.SectionAt(renamed, eta, x), Geometry.SectionAt(example, eta, x));
                    Agree(Geometry.PointAt(renamed, eta, x, true), Geometry.PointAt(example, eta, x, true));
                }
        });
        Check("Blend_Midpoint_CamberIsMeanAndThicknessPeaksAtOne", () =>
        {
            var blended = Geometry.Assess(Prepared(Cambered())).Certificate!;
            var root = Geometry.Assess(Prepared(Only("a"))).Certificate!;
            var tip = Geometry.Assess(Prepared(Only("b"))).Certificate!;
            var mean = Mean(Camber(Geometry.SectionAt(root, .5, Peak)), Camber(Geometry.SectionAt(tip, .5, Peak)));
            Agree(Camber(Geometry.SectionAt(blended, .5, Peak)), mean);
            var section = Geometry.SectionAt(blended, .5, Peak);
            double low = (section.Upper.Lower - section.Lower.Upper) / .12;
            double high = (section.Upper.Upper - section.Lower.Lower) / .12;
            Agree(new EnclosedOrdinate(low, high), new EnclosedOrdinate(1, 1));
            var placed = Geometry.PointAt(blended, .5, Peak, true);
            Equal(true, placed.Z.Upper - placed.Z.Lower <= 1e-8);
        });
        Check("Blend_ThreeStations_ContinuousAtMiddle", () =>
        {
            string source = ReplaceProfiles(ProfileA() + ProfileB(), "at root profile \"section-a\" at 50 % profile \"section-b\" at tip profile \"section-a\"");
            var certificate = Geometry.Assess(Prepared(source)).Certificate!;
            var tip = Geometry.Assess(Prepared(Only("b"))).Certificate!;
            var middle = Geometry.SectionAt(certificate, .5, Peak);
            Agree(middle, Geometry.SectionAt(tip, .5, Peak));
            Agree(Geometry.SectionAt(certificate, Math.BitDecrement(.5), Peak), middle);
            Agree(Geometry.SectionAt(certificate, Math.BitIncrement(.5), Peak), middle);
        });
        Check("Blend_IndependentAbscissa_RefusesUnenclosedMaximum", () =>
        {
            string source = Cambered().Replace("(.375,.15625)", "(.5,.15625)", StringComparison.Ordinal)
                .Replace("(.375,-.09375)", "(.5,-.09375)", StringComparison.Ordinal);
            Equal(GeometryStatus.Unsupported, Geometry.Assess(Prepared(source)).Status);
        });
        Check("Blend_HeapMaximum_MatchesListOracleAndComparisonBound", () =>
        {
            // Seeded ties, repeated spans, and budgets across the admitted 1..1296 node range.
            var random = new Random(74);
            int[] budgets = [1, 2, 3, 4, 5, 6, 8, 12, 16, 24, 32, 64, 128, 256, 768, 1296];
            int bothRefused = 0;
            double worstRatio = 0;
            var clock = Stopwatch.StartNew();
            for (int trial = 0; trial < 320; trial++)
            {
                int count = 1 + trial % Geometry.BlendSpanLimit();
                int budget = trial == 28 ? 3 : budgets[trial % budgets.Length];
                bool flat = budget >= 128;
                int range = trial % 3 switch { 0 => 2, 1 => 4, _ => 8 };
                var spans = new Rational[count][];
                for (int span = 0; span < count; span++)
                {
                    if (span > 0 && random.Next(4) == 0) { spans[span] = spans[random.Next(span)]; continue; }
                    spans[span] = flat
                        ? Enumerable.Repeat((Rational)(1 + random.Next(3)), FoilSource.ProfileDegree + 1).ToArray()
                        : Enumerable.Range(0, FoilSource.ProfileDegree + 1)
                            .Select(_ => (Rational)random.Next(-range, range + 1) / (trial % 2 == 0 ? 1 : 7)).ToArray();
                    if (!flat && trial % 5 == 0) { spans[span][0] = 0; spans[span][^1] = 0; }
                }
                // Exercise the reviewer's equal-maximum pair in both insertion orders, beyond the first pop.
                if (trial is 1 or 28)
                {
                    Rational[][] tied = [[0, 1, 0, 0, 0, 0], [0, 1, (Rational)1 / 1024, 0, 0, 0]];
                    spans = trial == 1 ? tied : [tied[1], tied[0]];
                }
                var actual = MaximumResult(spans, budget, heap: true);
                long comparisons = Bernstein.LastMaximumComparisons;
                var expected = MaximumResult(spans, budget, heap: false);
                if (!actual.Equals(expected))
                    throw new InvalidOperationException($"trial={trial} spans={count} budget={budget}: heap={actual}; list={expected}");
                if (actual.Refused && expected.Refused) bothRefused++;
                long bound = QueryFeasibility.MaximumComparisons(budget, count, FoilSource.ProfileDegree);
                worstRatio = Math.Max(worstRatio, (double)comparisons / bound);
                if (comparisons > bound)
                    throw new InvalidOperationException($"trial={trial} spans={count} budget={budget}: comparisons {comparisons} > {bound}");
            }
            clock.Stop();
            Equal(true, bothRefused > 0);
            Console.WriteLine($"Blend heap differential: 320 cases, {bothRefused} both refused, worst comparisons/bound {worstRatio.ToString("F3", CultureInfo.InvariantCulture)}, {clock.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        });
        Check("Blend_FourDifferingStations_Certify_SevenRefusedByOperations", () =>
        {
            var four = Geometry.Assess(Prepared(DifferingStations(4)));
            Equal(GeometryStatus.Certified, four.Status);
            Equal(true, four.Certificate!.QueryFeasibility.RationalOperationsUpper <= 1_000_000);
            string fixture = Path.Combine(PlacementTests.RepoRoot(), "docs", "proof", "blend-certificate-heap", "fixtures", "C-4st-s27-c120.foil");
            var atCeiling = Geometry.Assess(Prepared(File.ReadAllText(fixture)));
            Equal(GeometryStatus.Certified, atCeiling.Status);
            Equal(48 * 27, atCeiling.Certificate!.BlendNodeBudget);
            Equal(925_354L, atCeiling.Certificate.QueryFeasibility.RationalOperationsUpper);
            var seven = Geometry.Assess(Prepared(DifferingStations(7)));
            Equal(GeometryStatus.NotAssessed, seven.Status);
            Equal(Geometry.OperationBoundCode, seven.Code);
        });
        Check("Blend_MakeUniqueThreeDiffering_DraftCertifies", () =>
        {
            string source = ReplaceProfiles(ProfileVariant(0) + ProfileVariant(1) + ProfileVariant(2),
                "at root profile \"section-0\" at 25 % profile \"section-0\" at 50 % profile \"section-1\" at tip profile \"section-2\"");
            using var session = new AuthoringSession();
            session.Open(Encoding.UTF8.GetBytes(source), SectionDraftTests.Id(), true);
            string id = SectionDraftTests.Id();
            var view = session.BeginSectionDraft(id, 0);
            view = session.ApplySectionStep(id, view.Generation, new SectionStep.MakeUnique());
            Equal(4, FoilSource.Parse(view.Bytes).Definition!.Profiles.Length);
            Equal(GeometryStatus.Certified, session.AssessSection(id, view.Generation, CancellationToken.None).Status);
        });
    }

    private readonly record struct MaximumOutcome(bool Refused, Rational Lower, Rational Upper, int Nodes, long Spent, string? Reason);

    private static MaximumOutcome MaximumResult(Rational[][] spans, int budget, bool heap)
    {
        var watch = new ProofBudget();
        try
        {
            var result = heap ? Bernstein.Maximum(spans, watch, budget) : ListMaximum(spans, watch, budget);
            return new(false, result.Lower, result.Upper, result.Nodes, watch.Spent, null);
        }
        catch (ProofRefusal refusal)
        {
            return new(true, default, default, 0, watch.Spent, refusal.Message);
        }
    }

    // Pre-heap list algorithm: each step rescans all pending coefficients and splits the first maximum.
    private static (Rational Lower, Rational Upper, int Nodes) ListMaximum(Rational[][] spans, ProofBudget watch, int budget)
    {
        var pending = spans.Select(coefficients => (Coefficients: coefficients, Depth: 0)).ToList();
        Rational lower = pending.SelectMany(item => new[] { item.Coefficients[0], item.Coefficients[^1] }).Max();
        var tolerance = Rational.From(1e-12);
        for (int nodes = 0; nodes < budget; nodes++)
        {
            watch.Check();
            var upper = pending.SelectMany(item => item.Coefficients).Max();
            if (lower > 0 && upper - lower <= tolerance * lower) return (lower, upper, nodes);
            int index = pending.FindIndex(item => item.Coefficients.Max().CompareTo(upper) == 0);
            var chosen = pending[index];
            Geometry.Require(chosen.Depth < 64, "Maximum enclosure depth budget exhausted.");
            pending.RemoveAt(index);
            var (left, right) = SplitForListOracle(chosen.Coefficients);
            if (left[^1] > lower) lower = left[^1];
            pending.Add((left, chosen.Depth + 1)); pending.Add((right, chosen.Depth + 1));
        }
        throw new ProofRefusal("Maximum enclosure node budget exhausted.");
    }

    private static (Rational[] Left, Rational[] Right) SplitForListOracle(Rational[] coefficients)
    {
        var working = coefficients.ToArray();
        var left = new Rational[working.Length]; var right = new Rational[working.Length];
        left[0] = working[0]; right[^1] = working[^1];
        for (int level = 1; level < working.Length; level++)
        {
            for (int index = 0; index < working.Length - level; index++) working[index] = (working[index] + working[index + 1]) / 2;
            left[level] = working[0]; right[^(level + 1)] = working[working.Length - level - 1];
        }
        return (left, right);
    }

    // Symmetric degree-5 Bézier thickness peaks at t=1/2, and these x controls put that parameter at x=1/2.
    private const double Peak = 0.5;
    private static string Text => Encoding.UTF8.GetString(FoilSourceTests.Example);
    private static SourceParse Prepared(string text)
    {
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        if (!parsed.IsParsed)
            throw new InvalidOperationException(string.Join(" | ", parsed.Diagnostics.Select(item => item.Code + "@" + item.Line + ":" + item.ScalarColumn + " " + item.Reason)));
        return FoilSource.Parse(FoilSource.MaterializeIds(parsed));
    }

    private const string Basis = "degree 5 knots [0,0,0,0,0,0,1,1,1,1,1,1] points ";
    private static string Profile(string name, string upper, string lower) =>
        "    profile \"" + name + "\" {\n      upper cv { " + Basis + upper + " }\n      lower cv { " + Basis + lower + " }\n    }\n";
    private static string ProfileA() => Profile("section-a",
        "[(0,0),(.125,.0625),(.375,.125),(.625,.125),(.875,.0625),(1,0)]",
        "[(0,0),(.125,-.0625),(.375,-.125),(.625,-.125),(.875,-.0625),(1,0)]");
    private static string ProfileB() => Profile("section-b",
        "[(0,0),(.125,.078125),(.375,.15625),(.625,.15625),(.875,.078125),(1,0)]",
        "[(0,0),(.125,-.046875),(.375,-.09375),(.625,-.09375),(.875,-.046875),(1,0)]");
    private static string Cambered() => ReplaceProfiles(ProfileA() + ProfileB(), "at root profile \"section-a\" at tip profile \"section-b\"");
    private static string DifferingStations(int count)
    {
        double[] positions = count switch { 3 => [0, 50, 100], 4 => [0, 25, 50, 100], 7 => [0, 12.5, 25, 37.5, 50, 75, 100], _ => throw new ArgumentOutOfRangeException(nameof(count)) };
        string[] assignments = Enumerable.Range(0, count).Select(index =>
            "at " + (index == 0 ? "root" : index == count - 1 ? "tip" : positions[index].ToString(CultureInfo.InvariantCulture) + " %") +
            " profile \"section-" + index.ToString(CultureInfo.InvariantCulture) + "\"").ToArray();
        return ReplaceProfiles(string.Concat(Enumerable.Range(0, count).Select(ProfileVariant)), string.Join(" ", assignments));
    }

    private static string ProfileVariant(int index)
    {
        double shoulder = .0625 + index * .0078125, peak = 2 * shoulder;
        string Number(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);
        return Profile("section-" + index.ToString(CultureInfo.InvariantCulture),
            "[(0,0),(.125," + Number(shoulder) + "),(.375," + Number(peak) + "),(.625," + Number(peak) + "),(.875," + Number(shoulder) + "),(1,0)]",
            "[(0,0),(.125," + Number(-shoulder) + "),(.375," + Number(-peak) + "),(.625," + Number(-peak) + "),(.875," + Number(-shoulder) + "),(1,0)]");
    }
    private static string Only(string which) => which == "a"
        ? ReplaceProfiles(ProfileA(), "at root profile \"section-a\" at tip profile \"section-a\"")
        : ReplaceProfiles(ProfileB(), "at root profile \"section-b\" at tip profile \"section-b\"");

    private static string DuplicateExample(string sections)
    {
        const string mark = "    profile \"section-a\" {";
        int start = Text.IndexOf(mark, StringComparison.Ordinal);
        int end = Text.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        string block = Text[start..(end + "\n    }".Length)];
        return Text.Insert(end + "\n    }".Length, "\n" + block.Replace("section-a", "section-b", StringComparison.Ordinal))
            .Replace("sections { at root profile \"section-a\" at tip profile \"section-a\" }", "sections { " + sections + " }", StringComparison.Ordinal);
    }

    private static string ReplaceProfiles(string profiles, string sections)
    {
        int start = Text.IndexOf("  profiles {", StringComparison.Ordinal);
        int sectionsAt = Text.IndexOf("  sections {", StringComparison.Ordinal);
        int lineEnd = Text.IndexOf('\n', sectionsAt);
        return Text[..start] + "  profiles {\n" + profiles + "  }\n  sections { " + sections + " }" + Text[lineEnd..];
    }

    private static EnclosedOrdinate Camber(SectionEnclosure section) =>
        new((section.Upper.Lower + section.Lower.Lower) / 2, (section.Upper.Upper + section.Lower.Upper) / 2);
    private static EnclosedOrdinate Mean(EnclosedOrdinate left, EnclosedOrdinate right) =>
        new((left.Lower + right.Lower) / 2, (left.Upper + right.Upper) / 2);
    private static void Agree(SectionEnclosure actual, SectionEnclosure expected)
    {
        Agree(actual.Upper, expected.Upper);
        Agree(actual.Lower, expected.Lower);
    }
    private static void Agree(PlacedPointEnclosure actual, PlacedPointEnclosure expected)
    {
        Agree(actual.X, expected.X); Agree(actual.Y, expected.Y); Agree(actual.Z, expected.Z);
    }
    private static void Agree(EnclosedOrdinate actual, EnclosedOrdinate expected)
    {
        double width = Math.Max(actual.Upper - actual.Lower, expected.Upper - expected.Lower);
        Equal(true, Math.Abs(actual.Lower - expected.Lower) <= width && Math.Abs(actual.Upper - expected.Upper) <= width);
    }
}
