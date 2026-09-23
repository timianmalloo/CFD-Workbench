using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class GeometryTests
{
    internal static void Run()
    {
        foreach (string value in new[] { "0", "1e-300", "-1e-300", "5e-324", "-5e-324" })
            Check("Ruling18_ConstantTwist_" + value + "_QueriesComplete", () =>
            {
                string source = System.Text.RegularExpressions.Regex.Replace(DyadicProfile(), @"twist cv \{[^}]+\}",
                    "twist cv { degree 3 knots [0,0,0,0,.5,.5,.5,1,1,1,1] points [" +
                    string.Join(",", new[] { "0", ".125", ".25", ".5", ".75", ".875", "1" }.Select(x => "(" + x + "," + value + ")")) + "] }");
                var certificate = Geometry.Assess(Prepared(source)).Certificate ?? throw new InvalidOperationException("Constant twist expected admitted");
                var point = Geometry.PointAt(certificate, .203125, 1, true); Contains(point.X, .12);
                // Tiny angles perturb Z by much less than the declared enclosure, but are never replaced by zero.
                Contains(point.Z, -.12 * Math.Sin(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture) * 0.017453292519943295));
                _ = Geometry.SectionAt(certificate, double.Epsilon, Math.BitDecrement(1));
            });
        Check("Ruling18_Certificate_CarriesActualAllQueryArithmeticWitness", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            Equal(true, certificate.QueryFeasibility is not null);
            Equal(true, certificate.QueryFeasibility!.MaximumIntermediateBits <= certificate.QueryFeasibility.RationalBitLimit);
            Equal(true, certificate.QueryFeasibility.Spans.Count > 0);
            Equal(true, certificate.QueryFeasibility.RationalOperationsUpper <= 1000000);
            Console.WriteLine("QUERY FEASIBILITY RECEIPT " + System.Text.Json.JsonSerializer.Serialize(certificate.QueryFeasibility));
        });
        Check("Ruling18_QueryEnvironmentalOutcomes_SeparateFromDeterministicCaps", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            Refuses("GEOMETRY-BUDGET", () => Geometry.PointAt(certificate, .5, .5, true, timeBudget: TimeSpan.Zero));
            Refuses("GEOMETRY-BUDGET", () => Geometry.SectionAt(certificate, .5, .5, TimeSpan.Zero));
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            Refuses("GEOMETRY-CANCELLED", () => Geometry.PointAt(certificate, .5, .5, true, cancellationToken: cancellation.Token));
            Refuses("GEOMETRY-CANCELLED", () => Geometry.SectionAt(certificate, .5, .5, cancellationToken: cancellation.Token));
            _ = Geometry.PointAt(certificate, .5, .5, true);
        });
        Check("Ruling18_FiniteBinary64QueryBoundaries_CompleteBothSidesAndPorts", () =>
        {
            var certificate = Geometry.Assess(Prepared(DyadicProfile())).Certificate!;
            double[] queries = [0, double.Epsilon, Math.ScaleB(1, -1022), Math.ScaleB(1, -512), Math.ScaleB(1, -53),
                Math.BitDecrement(.5), .5, Math.BitIncrement(.5), Math.BitDecrement(1), 1];
            foreach (double x in queries)
                foreach (bool upper in new[] { false, true })
                    foreach (bool port in new[] { false, true })
                    {
                        var point = Geometry.PointAt(certificate, x, 1 - x, upper, port);
                        foreach (var coordinate in new[] { point.X, point.Y, point.Z })
                            Equal(true, double.IsFinite(coordinate.Lower) && double.IsFinite(coordinate.Upper) &&
                                coordinate.Upper - coordinate.Lower <= certificate.PlacementWidthUpper);
                        _ = Geometry.SectionAt(certificate, x, 1 - x);
                    }
        });
        Check("Ruling18_ExpensiveExactSpan_PreAdmissionRefusal", () =>
        {
            var assessment = Geometry.Assess(Prepared(Text.Replace("0.25, 0.5, 0.75", "5e-324, 0.5, 0.75")));
            Equal(GeometryStatus.NotAssessed, assessment.Status);
            Equal("GEOMETRY-QUERY-RESOURCE", assessment.Code);
            Equal(null, assessment.Certificate);
        });
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
