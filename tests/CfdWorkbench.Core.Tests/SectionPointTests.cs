using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

// M1.2c (docs/design/m12c-section-editor.md §12.4): registered empty by the PRE track; the owning track adds the named checks.
internal static class SectionPointTests
{
    internal static void Run()
    {
        IdentityTests.Check(nameof(IsAnchor_DegreeFive_OnlyMultiplicityFiveTrue), IsAnchor_DegreeFive_OnlyMultiplicityFiveTrue);
        IdentityTests.Check(nameof(Parse_ProfileHorizontalRowOnAnchor_Accepted), Parse_ProfileHorizontalRowOnAnchor_Accepted);
        IdentityTests.Check(nameof(Parse_ChannelHorizontalRow_StillDslLock), Parse_ChannelHorizontalRow_StillDslLock);
    }

    private static void IsAnchor_DegreeFive_OnlyMultiplicityFiveTrue()
    {
        double[] quintic = Clamped(11, 5, 0.5, 5);
        IdentityTests.Equal(true, FoilSource.IsAnchor(quintic, 11, 5, 5));
        IdentityTests.Equal(false, FoilSource.IsAnchor(quintic, 11, 5, 4));
        IdentityTests.Equal(false, FoilSource.IsAnchor(quintic, 11, 5, 6));
        double[] triple = Clamped(11, 5, 0.5, 3);
        IdentityTests.Equal(false, FoilSource.IsAnchor(triple, 11, 5, 5));
        double[] cubic = Clamped(8, 3, 0.4, 3);
        IdentityTests.Equal(true, FoilSource.IsAnchor(cubic, 8, 3, 3));
        IdentityTests.Equal(false, FoilSource.IsAnchor(cubic, 8, 3, 4));
    }

    private static void Parse_ProfileHorizontalRowOnAnchor_Accepted()
    {
        string upper = "upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.5, 0.5, 0.5, 0.5, 0.5, 1, 1, 1, 1, 1, 1] "
            + "points [(0, 0), (0, 0.02), (0.2, 0.04), (0.35, 0.04), (0.45, 0.04), (0.5, 0.04), (0.55, 0.04), (0.7, 0.03), (0.85, 0.015), (1, 0.005), (1, 0)] "
            + "ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\", \"cv-7\", \"cv-8\", \"cv-9\", \"cv-10\"] "
            + "tangents { \"cv-5\" horizontal } }";
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(WithUpper(upper, v41: true)));
        if (!parsed.IsParsed)
            throw new InvalidOperationException(parsed.Diagnostics[0].Code + " " + parsed.Diagnostics[0].Reason);
        IdentityTests.Equal(true, parsed.IsParsed);
        if (!parsed.IsParsed) return;
        var row = parsed.Definition!.Profiles[0].Upper.Tangents.Single();
        IdentityTests.Equal("cv-5", row.Id);
        IdentityTests.Equal("horizontal", row.Kind);
    }

    private static void Parse_ChannelHorizontalRow_StillDslLock()
    {
        byte[] identified = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
        string text = Encoding.UTF8.GetString(identified).Replace("foildsl \"4.0\"", "foildsl \"4.1\"", StringComparison.Ordinal);
        int lead = text.IndexOf("leading cv {", StringComparison.Ordinal);
        int close = text.IndexOf('}', lead);
        text = text.Insert(close, " tangents { \"cv-1\" horizontal }");
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        IdentityTests.Equal(false, parsed.IsParsed);
        IdentityTests.Equal("DSL-LOCK", parsed.Diagnostics[0].Code);
        IdentityTests.Equal(true, parsed.Diagnostics[0].Reason.Contains("smooth or symmetric", StringComparison.Ordinal));
    }

    private static double[] Clamped(int count, int degree, double knot, int multiplicity)
    {
        var knots = new double[count + degree + 1];
        for (int index = 0; index <= degree; index++) knots[index] = 0;
        for (int index = 0; index < multiplicity; index++) knots[degree + 1 + index] = knot;
        for (int index = degree + 1 + multiplicity; index < knots.Length; index++) knots[index] = 1;
        return knots;
    }

    private static string WithUpper(string upper, bool v41)
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
        if (v41) text = text.Replace("foildsl \"4.0\"", "foildsl \"4.1\"", StringComparison.Ordinal);
        int start = text.IndexOf("      upper cv {", StringComparison.Ordinal);
        int end = text.IndexOf('}', start);
        return string.Concat(text.AsSpan(0, start), "      ", upper, text.AsSpan(end + 1));
    }
}
