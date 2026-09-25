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
