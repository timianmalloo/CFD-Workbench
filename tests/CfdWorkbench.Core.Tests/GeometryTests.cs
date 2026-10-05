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
            Refuses("GEOMETRY-BUDGET", () => Geometry.PointAt(certificate, .5, .5, true, false, new ProofBudget(0)));
            Refuses("GEOMETRY-BUDGET", () => Geometry.SectionAt(certificate, .5, .5, new ProofBudget(0)));
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
        // Ruling 88 (tip-handling S1): a closing tip is not certified, so no session ever holds one and the VLM never
        // sees one. Pins the status, not the message (the copy is operator-held).
        Check("Geometry_TipPoint_UnsupportedAndNeverAdmitted", () =>
        {
            string text = Encoding.UTF8.GetString(FoilSourceTests.Example);
            byte[] closing = Encoding.UTF8.GetBytes(text[..text.LastIndexOf('}')] + "  tip point\n}\n");
            var assessment = Geometry.Assess(Prepared(Encoding.UTF8.GetString(closing)));
            Equal(GeometryStatus.Unsupported, assessment.Status);
            Equal(null, assessment.Certificate);
            byte[] withIds = FoilSource.MaterializeIds(FoilSource.Parse(closing));
            using var session = new AuthoringSession();
            Refuses("DSL-NOT-ASSESSED", () => session.Open(withIds, Guid.NewGuid().ToString("D"), true));
        });
        Check("Geometry_ExhaustedWorkLimit_ProducesNoCertificate", () =>
        {
            var parsed = Prepared(DyadicProfile());
            var result = Geometry.Assess(parsed, new ProofBudget(0));
            Equal(GeometryStatus.NotAssessed, result.Status);
            Equal(true, result.Certificate is null);
            Equal("GEOMETRY-BUDGET", result.Code);
        });
        Check("Geometry_CallerCannotRaiseWorkCeiling", () =>
        {
            Refuses("DSL-RANGE", () => _ = new ProofBudget(ProofBudget.DefaultWorkLimit + 1));
            Refuses("DSL-RANGE", () => _ = new ProofBudget(-1));
        });
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
        Check("Assess_SmoothRowSatisfied_Certified", () => Equal(GeometryStatus.Certified, AssessFixture("foil-41-tangents.foil").Status));
        Check("Assess_SmoothRowOffByHalfDegree_Invalid", () =>
        {
            var assessment = AssessText(BendLeadingHandle("foil-41-tangents.foil", "(0.7, 0)", "(0.7, 0.7853881948536542)"));
            Equal(GeometryStatus.Invalid, assessment.Status);
            Equal("DSL-LOCK", assessment.Code);
        });
        Check("Assess_SymmetricRowNotMidpoint_Invalid", () =>
        {
            var assessment = AssessText(BendLeadingHandle("foil-41-tangents.foil", "(0.5, 0)", "(0.55, 0)").Replace("\"cv-3\" smooth", "\"cv-3\" symmetric", StringComparison.Ordinal));
            Equal(GeometryStatus.Invalid, assessment.Status);
            Equal("DSL-LOCK", assessment.Code);
        });
        Check("Assess_SixteenPointThreeAnchors_WorkCountBounded", () =>
        {
            var assessment = AssessFixture("foil-41-sixteen-three-anchors.foil");
            Equal(GeometryStatus.Certified, assessment.Status);
            long operations = assessment.Certificate!.QueryFeasibility!.RationalOperationsUpper;
            Equal(true, operations > 0 && operations <= 1_000_000);
        });
        Check("Geometry_TwistDomain_LargestAssessableDegreesPinned", () =>
        {
            double domain = Geometry.TwistDomainDegrees;
            Console.WriteLine("MEASURE twist_domain_degrees=" + domain.ToString("G17", System.Globalization.CultureInfo.InvariantCulture));
            Equal(true, domain > 57 && domain < 58);
            Equal(BitConverter.DoubleToInt64Bits(Geometry.LargestAdmissibleTwist()), BitConverter.DoubleToInt64Bits(domain));
            Equal(true, TwistAdmissible(domain) && TwistAdmissible(0 - domain));
            Equal(false, TwistAdmissible(Math.BitIncrement(domain)));
            Equal(false, TwistAdmissible(Math.BitDecrement(0 - domain)));
            Equal(true, TwistAdmissible(Math.BitDecrement(domain)));
        });
        Check("Geometry_ThicknessDomain_OpenIntervalOnQuantumGrid", () =>
        {
            Equal(BitConverter.DoubleToUInt64Bits(1e-7), BitConverter.DoubleToUInt64Bits(Geometry.ThicknessDomain.Lower));
            Equal(BitConverter.DoubleToUInt64Bits(1 - 1e-7), BitConverter.DoubleToUInt64Bits(Geometry.ThicknessDomain.Upper));
            Equal(true, Geometry.ThicknessDomain.Lower > 0 && Geometry.ThicknessDomain.Upper < 1);
        });
        Check("Assess_TwistAtDomainLimit_Certified", () =>
        {
            Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(ConstantTwist(Geometry.TwistDomainDegrees))).Status);
            Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(ConstantTwist(0 - Geometry.TwistDomainDegrees))).Status);
        });
        Check("Assess_TwistNextBinary64PastDomain_NotAssessed", () =>
        {
            var positive = Geometry.Assess(Prepared(ConstantTwist(Math.BitIncrement(Geometry.TwistDomainDegrees))));
            var negative = Geometry.Assess(Prepared(ConstantTwist(Math.BitDecrement(0 - Geometry.TwistDomainDegrees))));
            Equal(GeometryStatus.NotAssessed, positive.Status);
            Equal(GeometryStatus.NotAssessed, negative.Status);
            Equal(true, positive.Reason.Contains("Taylor", StringComparison.Ordinal));
            Equal(true, negative.Reason.Contains("Taylor", StringComparison.Ordinal));
        });
        Check("Assess_ThicknessAtUpperDomainLimit_Certified", () =>
        {
            string spelled = Jcs.Number(Geometry.ThicknessDomain.Upper);
            Equal(Geometry.ThicknessDomain.Upper, DecimalSi.Parse(spelled));
            int start = Text.IndexOf("thickness cv", StringComparison.Ordinal);
            int end = Text.IndexOf('}', start);
            string changed = Text[..start] + Text[start..end].Replace("0.12)", spelled + ")", StringComparison.Ordinal) + Text[end..];
            Equal(GeometryStatus.Certified, Geometry.Assess(Prepared(changed)).Status);
        });
        Check("Assess_SmoothRowOnTwistChannel_Certified", () =>
            Equal(GeometryStatus.Certified, AssessText(ChannelRow("twist", "(0.5, -1)", "smooth")).Status));
        Check("Assess_TwistSmoothRowOrdinateOffByTwoTolerances_Invalid", () =>
        {
            var assessment = AssessText(ChannelRow("twist", "(0.5, -0.999998)", "smooth"));
            Equal(GeometryStatus.Invalid, assessment.Status);
            Equal("DSL-LOCK", assessment.Code);
            Equal("Smooth row is off the handle line.", assessment.Reason);
        });
        Check("Assess_SymmetricRowOnThicknessNotMidpoint_Invalid", () =>
        {
            var assessment = AssessText(ChannelRow("thickness", "(0.500000002, 0.12)", "symmetric"));
            Equal(GeometryStatus.Invalid, assessment.Status);
            Equal("DSL-LOCK", assessment.Code);
            Equal("Symmetric row is not the handle midpoint.", assessment.Reason);
        });
        Check("Assess_SmoothRowOnDihedral_Certified", () =>
            Equal(GeometryStatus.Certified, AssessText(ChannelRow("dihedral", "(0.5, 0.0005)", "smooth")).Status));
    }

    private static GeometryAssessment AssessFixture(string name) => AssessText(File.ReadAllText(M12bFixtures.Path(name)));

    private static GeometryAssessment AssessText(string text)
    {
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        if (parsed.IsParsed && parsed.Definition!.Curves.Values.Any(curve => curve.MissingIds))
            parsed = FoilSource.Parse(FoilSource.MaterializeIds(parsed));
        return Geometry.Assess(parsed);
    }

    private static bool TwistAdmissible(double degrees)
    {
        var product = Rational.From(degrees) * Rational.From(0.017453292519943295);
        var rounded = Rational.From(product.Nearest());
        return rounded >= -1 && rounded <= 1;
    }

    private static string ConstantTwist(double degrees)
    {
        string spelled = Jcs.Number(degrees);
        if (DecimalSi.Parse(spelled) != degrees) throw new InvalidOperationException("Twist spelling did not round-trip.");
        string points = string.Join(", ", new[] { "0", "0.1", "0.3", "0.5", "0.7", "0.9", "1" }.Select(eta => "(" + eta + ", " + spelled + ")"));
        return System.Text.RegularExpressions.Regex.Replace(Text, @"twist cv \{[^}]+\}",
            "twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [" + points + "] }");
    }

    private static string ChannelRow(string channel, string anchor, string kind)
    {
        string text = File.ReadAllText(M12bFixtures.Path("foil-41-tangents.foil"));
        string points = channel switch
        {
            "twist" => "[(0, 0), (0.1, 0), (0.3, -0.6), " + anchor + ", (0.7, -1.4), (0.9, -1.8), (1, -2)]",
            "thickness" => "[(0, 0.12), (0.1, 0.12), (0.4, 0.12), " + anchor + ", (0.6, 0.12), (0.9, 0.12), (1, 0.12)]",
            "dihedral" => "[(0, 0), (0.1, 0), (0.499, 0), " + anchor + ", (0.501, 0), (0.9, 0), (1, 0)]",
            _ => throw new InvalidOperationException(channel),
        };
        string body = "degree 3 knots [0, 0, 0, 0, 0.5, 0.5, 0.5, 1, 1, 1, 1] points " + points +
            " ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] tangents { \"cv-3\" " + kind + " }";
        int at = text.IndexOf(channel + " cv {", StringComparison.Ordinal);
        int end = text.IndexOf('\n', at);
        return text[..at] + channel + " cv { " + body + " }" + text[end..];
    }

    private static string BendLeadingHandle(string name, string from, string to)
    {
        string text = File.ReadAllText(M12bFixtures.Path(name));
        int at = text.IndexOf("leading cv", StringComparison.Ordinal);
        int next = text.IndexOf("trailing cv", at, StringComparison.Ordinal);
        string leading = text[at..next].Replace(from, to, StringComparison.Ordinal);
        return text[..at] + leading + text[next..];
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
