using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class GeometryTests
{
    internal static void Run()
    {
        Check("Geometry_TwistOutsideWholeDomainTaylorProof_NotAssessed", () => Equal(GeometryStatus.NotAssessed,
            Geometry.Assess(Prepared(Text.Replace("(1, -2)", "(1, -90)"))).Status));
        Check("Geometry_PlacementWidthBeyondBudget_NotAssessed", () => Equal(GeometryStatus.NotAssessed,
            Geometry.Assess(Prepared(Text.Replace("120)", "1e20)"))).Status));
        Check("Geometry_ExhaustedTimeBudget_ProducesNoCertificate", () =>
        {
            var parsed = Prepared(DyadicProfile());
            var result = Geometry.Assess(parsed, TimeSpan.Zero);
            Equal(GeometryStatus.NotAssessed, result.Status);
            Equal(true, result.Certificate is null);
            Equal("GEOMETRY-BUDGET", result.Code);
        });
        Check("Geometry_CallerCannotRaiseTimeCeiling", () => Refuses("DSL-RANGE", () => Geometry.Assess(Prepared(DyadicProfile()), TimeSpan.FromSeconds(2))));
        Check("Geometry_PlacedRootPoint_EnclosesIndependentZeroTwistCoordinates", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            var point = Geometry.PointAt(certificate, 0, .5, true);
            Contains(point.X, .06); Contains(point.Y, 0); Contains(point.Z, .12 * .06);
        });
        Check("Geometry_PlacedTip_MirrorsOnlySpan", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            var starboard = Geometry.PointAt(certificate, 1, 1, true);
            var port = Geometry.PointAt(certificate, 1, 1, true, true);
            Equal(starboard.X, port.X); Equal(starboard.Z, port.Z);
            Equal(starboard.Y.Lower, -port.Y.Upper); Equal(starboard.Y.Upper, -port.Y.Lower);
            // Independent libm comparison is a bounded check, not proof authority.
            Contains(starboard.X, .12 * Math.Cos(-2 * 0.017453292519943295));
            Contains(starboard.Z, -.12 * Math.Sin(-2 * 0.017453292519943295));
        });
        Check("Geometry_NormalizedSection_PropagatesExactMaximumEnclosure", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            var section = Geometry.SectionAt(certificate, .5, .5);
            Equal(true, section.Upper.Lower <= .06 && section.Upper.Upper >= .06);
            Equal(true, section.Lower.Lower <= -.06 && section.Lower.Upper >= -.06);
            Equal(true, section.Upper.Upper - section.Upper.Lower < 1e-10);
            Equal(true, section.Lower.Upper - section.Lower.Lower < 1e-10);
        });
        Check("Geometry_NormalizedSection_RejectsOutOfDomain", () => Refuses("DSL-RANGE", () => Geometry.SectionAt(Geometry.Assess(Prepared(DyadicProfile())).Certificate!, -1, .5)));
        Check("Geometry_DyadicBezier_EnclosesIndependentExactMaximum", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate ?? throw new InvalidOperationException("Expected dyadic certificate");
            // Symmetric degree-5 Bernstein polynomial peaks at t=1/2:
            // sum binomial(5,k)/32 * [0,1/8,1/4,1/4,1/8,0] = 25/128.
            Equal(true, certificate.ThicknessMaximumLower <= 25d / 128);
            Equal(true, certificate.ThicknessMaximumUpper >= 25d / 128);
            Equal(true, certificate.NormalizationRelativeErrorUpper <= 1e-12);
            Equal(true, certificate.Witnesses.Count > 0);
            Equal(true, certificate.SubdivisionNodes < 4096);
        });
        Check("Geometry_RepeatedInteriorKnots_ContinuousCertificate", () => Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(Text.Replace("0.25, 0.5, 0.75", "0.5, 0.5, 0.5"))).Status));
        Check("Geometry_ThicknessImmediatelyBelowOne_Admitted", () =>
        {
            int start = Text.IndexOf("thickness cv", StringComparison.Ordinal);
            int end = Text.IndexOf('}', start);
            string channel = Text[start..end];
            Equal(7, channel.Split("0.12)").Length - 1);
            string changed = Text[..start] + channel.Replace("0.12)", "0.9999999999999999)") + Text[end..];
            Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(changed)).Status);
        });
        Check("Geometry_SubnormalPositiveChord_Admitted", () => Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(Text.Replace("120)", "5e-321)"))).Status));
        Check("Geometry_ProvenNegativeChord_IsInvalid", () => Equal(GeometryStatus.Invalid, Geometry.Assess(Prepared(Text.Replace("120)", "-120)"))).Status));
        Check("Geometry_ViolatedRootLock_HasStableLockCode", () => Equal("DSL-LOCK", Geometry.Assess(Prepared(Text.Replace("(0.1, 0)", "(0.1, 1)"))).Code));
        Check("Geometry_UnsupportedBasis_DistinguishedFromUncertainty", () => Equal(GeometryStatus.Unsupported, Geometry.Assess(Prepared(Text.Replace("(0.15, -0.055)", "(0.16, -0.055)"))).Status));
        Check("Geometry_CommonBasisProfile_ContinuousCertificate", () =>
        {
            var parsed = Prepared(Text);
            var assessment = Geometry.Assess(parsed);
            Equal(GeometryStatus.Certified, assessment.Status);
            var certificate = assessment.Certificate!;
            Equal(parsed.SourceHash, certificate.SourceHash);
            Equal(parsed.SurfaceHash, certificate.SurfaceHash);
            Equal(true, certificate.ThicknessMaximumLower > 0);
            Equal(true, certificate.ThicknessMaximumUpper >= certificate.ThicknessMaximumLower);
            Equal(true, certificate.ThicknessMaximumUpper - certificate.ThicknessMaximumLower <= 1e-10);
        });
        Check("Geometry_CrossingChord_BlocksCertificate", () => Equal(GeometryStatus.Invalid, Geometry.Assess(Prepared(Text.Replace("120)", "-120)"))).Status));
        Check("Geometry_InteriorNegativeThickness_BlocksCertificate", () => Equal(GeometryStatus.NotAssessed, Geometry.Assess(Prepared(Text.Replace("(0.5, 0.12)", "(0.5, -3)"))).Status));
        Check("Geometry_UpperLowerCrossing_BlocksCertificate", () => Equal(GeometryStatus.NotAssessed, Geometry.Assess(Prepared(Text.Replace("(0.35, 0.065)", "(0.35, -3)"))).Status));
        Check("Geometry_IndependentProfileBasis_Unsupported", () => Equal(GeometryStatus.Unsupported, Geometry.Assess(Prepared(Text.Replace("(0.15, -0.055)", "(0.16, -0.055)"))).Status));
        Check("Geometry_RootLockViolation_BlocksCertificate", () => Equal(GeometryStatus.Invalid, Geometry.Assess(Prepared(Text.Replace("(0.1, 0)", "(0.1, 1)"))).Status));
        Check("Geometry_MissingIds_NotCertified", () => Equal(GeometryStatus.Unsupported, Geometry.Assess(FoilSource.Parse(Encoding.UTF8.GetBytes(Text))).Status));
        Check("Geometry_OpaqueCertificate_NoPublicConstructor", () => Equal(0, typeof(GeometryCertificate).GetConstructors().Length));
    }
    private static string Text => Encoding.UTF8.GetString(FoilSourceTests.Example);
    private static void Contains(EnclosedOrdinate interval, double value)
    {
        Equal(true, interval.Lower <= value && interval.Upper >= value);
        Equal(true, interval.Upper - interval.Lower < 1e-10);
    }
    private static string DyadicProfile()
    {
        const string basis = "degree 5 knots [0,0,0,0,0,0,1,1,1,1,1,1] points ";
        int start = Text.IndexOf("upper cv", StringComparison.Ordinal);
        int end = Text.IndexOf("\n    }", start, StringComparison.Ordinal);
        return Text[..start] + "upper cv { " + basis + "[(0,0),(.125,.0625),(.375,.125),(.625,.125),(.875,.0625),(1,0)] }\n" +
            "lower cv { " + basis + "[(0,0),(.125,-.0625),(.375,-.125),(.625,-.125),(.875,-.0625),(1,0)] }" + Text[end..];
    }
    private static SourceParse Prepared(string text) => FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(text))));
}
