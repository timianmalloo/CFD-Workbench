using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
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
        IdentityTests.Check(nameof(SectionPoints_RolesOnExample_NoseHandlesControlsTrailingEnds), SectionPoints_RolesOnExample_NoseHandlesControlsTrailingEnds);
        IdentityTests.Check(nameof(SectionPoints_NoseShared_OnePointTwoHandles), SectionPoints_NoseShared_OnePointTwoHandles);
        IdentityTests.Check(nameof(SectionPoints_OpenTrailingEnd_YOnly), SectionPoints_OpenTrailingEnd_YOnly);
        IdentityTests.Check(nameof(SectionPoints_NoseAndClosedTrailingEnd_Fixed), SectionPoints_NoseAndClosedTrailingEnd_Fixed);
        IdentityTests.Check(nameof(SectionPoints_VerticalOnInteriorAnchor_DisabledWithReason), SectionPoints_VerticalOnInteriorAnchor_DisabledWithReason);
        IdentityTests.Check(nameof(SectionPoints_SolverOutput_PassesItsRowCheck), SectionPoints_SolverOutput_PassesItsRowCheck);
        IdentityTests.Check(nameof(Sections_Facts_LeRadiusPerSurfaceMatchesOracle), Sections_Facts_LeRadiusPerSurfaceMatchesOracle);
        IdentityTests.Check(nameof(Sections_Facts_TrailingEdgeWedgeAndGap), Sections_Facts_TrailingEdgeWedgeAndGap);
        IdentityTests.Check(nameof(Sections_Facts_LeRadiusAndWedgeAtStation_MatchPlacedRuleA), Sections_Facts_LeRadiusAndWedgeAtStation_MatchPlacedRuleA);
        IdentityTests.Check(nameof(Sections_Probe_PlacedThicknessAtStation), Sections_Probe_PlacedThicknessAtStation);
        IdentityTests.Check(nameof(SectionComb_AfterMakeAnchor_BreakMarkerAtAnchor), SectionComb_AfterMakeAnchor_BreakMarkerAtAnchor);
        IdentityTests.Check(nameof(Assess_ProfileVerticalRowOffByOneUlp_Invalid), Assess_ProfileVerticalRowOffByOneUlp_Invalid);
        IdentityTests.Check(nameof(Assess_ProfileSmoothRowNearVertical_Certified), Assess_ProfileSmoothRowNearVertical_Certified);
        IdentityTests.Check(nameof(Assess_ProfileHorizontalRowUnlevel_InvalidDslLock), Assess_ProfileHorizontalRowUnlevel_InvalidDslLock);
        IdentityTests.Check(nameof(Assess_ProfileRowsJustOutsideTau_InvalidDslLock), Assess_ProfileRowsJustOutsideTau_InvalidDslLock);
        IdentityTests.Check(nameof(Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock), Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock);
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

    private static void SectionPoints_RolesOnExample_NoseHandlesControlsTrailingEnds()
    {
        var upper = Sections.View(FoilSourceTests.Example, 0, SurfaceSide.Upper, "accepted", 0);
        IdentityTests.Equal(PointRole.Nose, upper.Points[0].Role);
        IdentityTests.Equal(PointRole.NoseHandle, upper.Points[1].Role);
        IdentityTests.Equal(PointRole.Control, upper.Points[2].Role);
        IdentityTests.Equal(PointRole.Control, upper.Points[5].Role);
        IdentityTests.Equal(PointRole.TrailingHandle, upper.Points[^2].Role);
        IdentityTests.Equal(PointRole.TrailingEnd, upper.Points[^1].Role);
        IdentityTests.Equal("Anchor", Sections.PointType(upper.Points[0]));
        IdentityTests.Equal("Handle", Sections.PointType(upper.Points[1]));
        IdentityTests.Equal("Control", Sections.PointType(upper.Points[2]));
    }

    private static void SectionPoints_NoseShared_OnePointTwoHandles()
    {
        var upper = Sections.View(FoilSourceTests.Example, 0, SurfaceSide.Upper, "accepted", 0);
        var lower = Sections.View(FoilSourceTests.Example, 0, SurfaceSide.Lower, "accepted", 0);
        IdentityTests.Equal(0.0, upper.Points[0].SpanMeters);
        IdentityTests.Equal(0.0, upper.Points[0].Ordinate);
        IdentityTests.Equal(0.0, lower.Points[0].SpanMeters);
        IdentityTests.Equal(0.0, lower.Points[0].Ordinate);
        IdentityTests.Equal(PointRole.NoseHandle, upper.Points[1].Role);
        IdentityTests.Equal(PointRole.NoseHandle, lower.Points[1].Role);
        IdentityTests.Equal(true, upper.Points[1].Ordinate > 0);
        IdentityTests.Equal(true, lower.Points[1].Ordinate < 0);
    }

    private static void SectionPoints_OpenTrailingEnd_YOnly()
    {
        string text = Encoding.UTF8.GetString(FoilSourceTests.Example).Replace("    }\n  }\n  sections", "      closure open\n    }\n  }\n  sections", StringComparison.Ordinal);
        var end = Sections.View(Encoding.UTF8.GetBytes(text), 0, SurfaceSide.Upper, "accepted", 0).Points[^1];
        IdentityTests.Equal(PointRole.TrailingEnd, end.Role);
        IdentityTests.Equal(PointFreedom.ValueOnly, end.Freedom);
    }

    private static void SectionPoints_NoseAndClosedTrailingEnd_Fixed()
    {
        var upper = Sections.View(FoilSourceTests.Example, 0, SurfaceSide.Upper, "accepted", 0);
        IdentityTests.Equal(PointFreedom.Fixed, upper.Points[0].Freedom);
        IdentityTests.Equal(PointFreedom.Fixed, upper.Points[^1].Freedom);
        IdentityTests.Equal(PointFreedom.ValueOnly, upper.Points[1].Freedom);
    }

    private static void SectionPoints_VerticalOnInteriorAnchor_DisabledWithReason()
    {
        var anchor = new PointView("upper", "cv-5", 5, 0.5, 0.5, 0.04, PointRole.Anchor, null, TangentKind.Corner, PointFreedom.Free, Array.Empty<string>());
        var vertical = Sections.KindChoices(anchor).Single(choice => choice.Kind == TangentKind.Vertical);
        IdentityTests.Equal(false, vertical.Enabled);
        IdentityTests.Equal(Sections.VerticalInteriorReason, vertical.Reason);
    }

    private static void SectionPoints_SolverOutput_PassesItsRowCheck()
    {
        byte[] foil = Anchored("smooth", SmoothUpper(), SmoothLower());
        var parsed = FoilSource.Parse(foil);
        if (!parsed.IsParsed) throw new InvalidOperationException(parsed.Diagnostics[0].Code + " " + parsed.Diagnostics[0].Reason);
        var assessment = Geometry.Assess(parsed);
        if (assessment.Status != GeometryStatus.Certified)
            throw new InvalidOperationException(assessment.Status + " " + assessment.Code + " " + assessment.Reason);
    }

    private static void Sections_Facts_LeRadiusPerSurfaceMatchesOracle()
    {
        var facts = Sections.Facts(FoilSourceTests.Example, 0);
        Near(1.0 / 96.0, facts.UpperLeRadius, 1e-12);
        Near(1.0 / 96.0, facts.LowerLeRadius, 1e-12);
    }

    private static void Sections_Facts_TrailingEdgeWedgeAndGap()
    {
        var facts = Sections.Facts(FoilSourceTests.Example, 0);
        Near(2 * Math.Atan(0.1) * 180 / Math.PI, facts.TrailingWedgeDegrees, 1e-9);
        Near(0, facts.TrailingGap, 1e-12);
    }

    private static void Sections_Facts_LeRadiusAndWedgeAtStation_MatchPlacedRuleA()
    {
        var facts = Sections.Facts(FoilSourceTests.Example, 0);
        double k = facts.StationThicknessRatio / facts.OwnThickness;
        Near(facts.UpperLeRadius * k * k, facts.StationUpperLeRadius, 1e-9);
        Near(facts.LowerLeRadius * k * k, facts.StationLowerLeRadius, 1e-9);
        double half = facts.TrailingWedgeDegrees * Math.PI / 180 / 2;
        Near(2 * Math.Atan(k * Math.Tan(half)) * 180 / Math.PI, facts.StationTrailingWedgeDegrees, 1e-6);
        Near(facts.TrailingGap * k, facts.StationTrailingGap, 1e-12);
    }

    private static void Sections_Probe_PlacedThicknessAtStation()
    {
        var facts = Sections.Facts(FoilSourceTests.Example, 0);
        var probe = Sections.Probe(FoilSourceTests.Example, 0, 0.35);
        Near(probe.UpperY - probe.LowerY, probe.Thickness, 0);
        Near((probe.UpperY + probe.LowerY) / 2, probe.Camber, 1e-15);
        Near(probe.Thickness / facts.OwnThickness * facts.StationThicknessRatio * facts.StationChordMeters, probe.PlacedThicknessMeters, 1e-12);
    }

    private static void SectionComb_AfterMakeAnchor_BreakMarkerAtAnchor()
    {
        byte[] edited = SectionEdits.Apply(Identified(), 0, new SectionStep.SetType(SurfaceSide.Upper, "cv-4", true)).Bytes;
        var view = Sections.View(edited, 0, SurfaceSide.Upper, "accepted", 0);
        var breaks = Sections.Comb(view).Where(tooth => tooth.BreakBefore).ToArray();
        IdentityTests.Equal(1, breaks.Length);
        var anchor = view.Points.Single(point => point.Role == PointRole.Anchor);
        Near(anchor.SpanMeters, breaks[0].SpanMeters, 1e-9);
    }

    private static void Assess_ProfileVerticalRowOffByOneUlp_Invalid()
    {
        double below = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(0.5) - 1);
        string upper = Points(
            (0, 0), (0, 0.02), (0.2, 0.04), (0.35, 0.05), (below, 0.03), (0.5, 0.05), (0.5, 0.07),
            (0.7, 0.04), (0.85, 0.02), (0.95, 0.008), (1, 0));
        ExpectLock(Anchored("vertical", upper, NegateYs(upper)));
    }

    private static void Assess_ProfileSmoothRowNearVertical_Certified()
    {
        byte[] foil = Anchored("smooth", SmoothUpper(), SmoothLower());
        var assessment = Geometry.Assess(FoilSource.Parse(foil));
        if (assessment.Status != GeometryStatus.Certified)
            throw new InvalidOperationException(assessment.Status + " " + assessment.Code + " " + assessment.Reason);
    }

    private static void Assess_ProfileHorizontalRowUnlevel_InvalidDslLock()
    {
        string upper = Points(
            (0, 0), (0, 0.02), (0.2, 0.04), (0.35, 0.04), (0.45, 0.041), (0.5, 0.04), (0.55, 0.04),
            (0.7, 0.03), (0.85, 0.015), (0.95, 0.005), (1, 0));
        ExpectLock(Anchored("horizontal", upper, NegateYs(upper)));
    }

    private static void Assess_ProfileRowsJustOutsideTau_InvalidDslLock()
    {
        double line = 0.04;
        double over = line + 1e-9;
        while (over - line <= 1e-9)
            over = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(over) + 1);
        string upper = Points(
            (0, 0), (0, 0.02), (0.2, 0.04), (0.35, 0.04), (0.45, 0.04), (0.5, over), (0.55, 0.04),
            (0.7, 0.03), (0.85, 0.015), (0.95, 0.005), (1, 0));
        ExpectLock(Anchored("smooth", upper, NegateYs(upper)));
    }

    private static void Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock()
    {
        string upper = Points(
            (0, 0), (0, 0.02), (0.2, 0.04), (0.35, 0.05), (0.5, 0.06), (0.5, 0.05), (0.5, 0.07),
            (0.7, 0.04), (0.85, 0.02), (0.95, 0.008), (1, 0));
        ExpectLock(Anchored("vertical", upper, NegateYs(upper)));
    }

    internal static byte[] Identified() => FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));

    internal static void Near(double expected, double actual, double tolerance)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Expected {expected}; actual {actual}; tolerance {tolerance}");
    }

    private static void ExpectLock(byte[] foil)
    {
        var assessment = Geometry.Assess(FoilSource.Parse(foil));
        if (assessment.Status != GeometryStatus.Invalid || assessment.Code != "DSL-LOCK")
            throw new InvalidOperationException(assessment.Status + " " + assessment.Code + " " + assessment.Reason);
    }

    internal static byte[] Anchored(string kind, string upperPoints, string lowerPoints)
    {
        string knots = "0, 0, 0, 0, 0, 0, 0.5, 0.5, 0.5, 0.5, 0.5, 1, 1, 1, 1, 1, 1";
        string ids = string.Join(", ", Enumerable.Range(0, 11).Select(index => "\"cv-" + index + "\""));
        string upper = "upper cv { degree 5 knots [" + knots + "] points [" + upperPoints + "] ids [" + ids + "] tangents { \"cv-5\" " + kind + " } }";
        string lower = "lower cv { degree 5 knots [" + knots + "] points [" + lowerPoints + "] ids [" + ids + "] }";
        return Encoding.UTF8.GetBytes(ReplaceSurfaces(upper, lower, true));
    }

    internal static string SmoothUpper() => Points(
        (0, 0), (0, 0.02), (0.1, 0.04), (0.25, 0.05), (0.499, 0.03), (0.5, 0.05), (0.501, 0.07),
        (0.65, 0.04), (0.8, 0.02), (0.95, 0.008), (1, 0));

    internal static string SmoothLower() => NegateYs(SmoothUpper());

    internal static string Points(params (double X, double Y)[] points) =>
        string.Join(", ", points.Select(point => "(" + FoilSource.ExactDecimal(point.X) + ", " + FoilSource.ExactDecimal(point.Y) + ")"));

    internal static string NegateYs(string points)
    {
        var values = new List<string>();
        foreach (var part in points.Split("), "))
        {
            int comma = part.IndexOf(", ", StringComparison.Ordinal);
            string y = part[(comma + 2)..].Trim('(', ')', ' ');
            string x = part[..comma].Trim('(', ' ');
            double flipped = -double.Parse(y, CultureInfo.InvariantCulture);
            values.Add("(" + x + ", " + FoilSource.ExactDecimal(flipped) + ")");
        }
        return string.Join(", ", values);
    }

    internal static string ReplaceSurfaces(string upper, string lower, bool v41)
    {
        string text = Encoding.UTF8.GetString(FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example)));
        if (v41) text = text.Replace("foildsl \"4.0\"", "foildsl \"4.1\"", StringComparison.Ordinal);
        int start = text.IndexOf("      upper cv {", StringComparison.Ordinal);
        int end = text.IndexOf('}', start);
        text = string.Concat(text.AsSpan(0, start), "      ", upper, text.AsSpan(end + 1));
        start = text.IndexOf("      lower cv {", StringComparison.Ordinal);
        end = text.IndexOf('}', start);
        return string.Concat(text.AsSpan(0, start), "      ", lower, text.AsSpan(end + 1));
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
