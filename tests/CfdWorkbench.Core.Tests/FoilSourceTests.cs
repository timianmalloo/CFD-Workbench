using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class FoilSourceTests
{
    internal static byte[] Example => File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil");
    internal static void Run()
    {
        Check("Ruling17_DegreeIdentity_DoesNotCollapseDistinctInputs", () =>
        {
            var a = Parse(Text.Replace("(0.3, -0.25)", "(0.3, 1.791)"));
            var b = Parse(Text.Replace("(0.3, -0.25)", "(0.3, 1.7910000000000001)"));
            Equal(true, a.IsParsed); Equal(true, b.IsParsed); Equal(false, a.SurfaceHash == b.SurfaceHash);
        });
        Check("Ruling17_LegacySource_RefusedWithOriginalBytes", () =>
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Text.Replace("\"cfdw-cv\" \"2\"", "\"cfdw-cv\" \"1\""));
            var legacy = FoilSource.Parse(bytes); Equal(false, legacy.IsParsed); Equal("DSL-VERSION", legacy.Diagnostics[0].Code);
            Equal(true, bytes.AsSpan().SequenceEqual(legacy.Source)); Equal(null, legacy.SurfaceHash);
        });
        Check("Ruling15_UnknownUnits_DoNotInventOverflow", () => DiagnosticCase(Text.Replace("units mm", "units alien").Replace("(0.5, 0)", "(0.5, 1e309)"), "DSL-UNIT", "Structural", "alien"));
        Check("Ruling15_MissingUnit_PreventsOverflowBinding", () => DiagnosticCase(Text.Replace("450 mm", "1e309"), "DSL-SYNTAX", "Syntactic", "\"cfdw-cv\""));
        Check("Ruling15_UnknownChannel_DoesNotInventOverflow", () => DiagnosticCase(Text.TrimEnd()[..^1] + " locks { value alien at root 1e309 } }", "DSL-SYNTAX", "Syntactic", "alien"));
        Check("Ruling15_KnownOverflow_PrecedesBadReference", () => DiagnosticCase(Text.Replace("450 mm", "1e309 m").Replace("at tip profile \"section-a\"", "at tip profile \"missing\""), "DSL-LEX", "Lexical", "1e309"));
        Check("Ruling15_KnownOverflow_PrecedesBadDegree", () => DiagnosticCase(Text.Replace("450 mm", "1e309 m").Replace("degree 3", "degree 2"), "DSL-LEX", "Lexical", "1e309"));
        Check("Ruling15_BoundMillimeters_CompensateLargeDecimal", () => Equal(true, Parse(Text.Replace("450 mm", "1e309 mm")).IsParsed));
        Check("Ruling15_BoundMeters_OverflowLexically", () => DiagnosticCase(Text.Replace("450 mm", "1e309 m"), "DSL-LEX", "Lexical", "1e309"));
        Check("Ruling15_MalformedToken_PrecedesBlockingSyntax", () => DiagnosticCase(Text.Replace("450 mm", "1e309") + "@", "DSL-LEX", "Lexical", "@"));
        Check("Ruling15_UnknownEvaluator_PreventsGuessedConversion", () => DiagnosticCase(Text.Replace("450 mm", "1e309 m").Replace("cfdw-cv", "unknown"), "DSL-VERSION", "Version", "\"unknown\""));
        Check("Parse_FoilGrammar_RecognizesAllChannels", () => Equal(true, FoilSource.Parse(Example).IsParsed));
        Check("Parse_StandaloneSection_RecognizesWholeGrammar", () => Equal(true, FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/section-basic.foil")).IsParsed));
        Check("Parse_Assertions_RecognizesWholeGrammar", () => Equal(true, FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/foil-assertions.foil")).IsParsed));
        Check("Parse_InvalidVersion_ReportsVersion", () => Code("invalid-version.foil", "DSL-VERSION"));
        Check("Parse_InvalidUnits_ReportsUnits", () => Code("invalid-units.foil", "DSL-UNIT"));
        Check("Parse_InvalidReference_ReportsReference", () => Code("invalid-reference.foil", "DSL-REFERENCE"));
        Check("Parse_IncompleteSource_ReportsSyntax", () => Code("invalid-syntax.foil", "DSL-SYNTAX"));
        Check("Parse_LegacyChord_ReportsExplicitConversion", () => Code("invalid-legacy-draft.foil", "DSL-LEGACY"));
        Check("Parse_LexicalErrorAfterVersionError_ReportsLexicalFirst", () => Equal("DSL-LEX", Parse(Text.Replace("cfdw-cv", "unknown") + "@").Diagnostics[0].Code));
        Check("Parse_SyntaxAfterStructuralError_ReportsSyntaxFirst", () => Equal("DSL-SYNTAX", Parse(Text.Replace("degree 3", "degree 2") + "extra").Diagnostics[0].Code));
        Check("Parse_InvalidUtf8_ReportsLexical", () => Equal("DSL-LEX", FoilSource.Parse([0xff]).Diagnostics[0].Code));
        Check("Parse_NumberWordWithoutWhitespace_ReportsLexical", () => Equal("DSL-LEX", Parse(Text.Replace("450 mm", "450mm")).Diagnostics[0].Code));
        Check("Parse_UnknownTailInUnsupportedSection_ReportsSyntax", () => Equal("DSL-SYNTAX", Parse(File.ReadAllText("docs/examples/foildsl/section-basic.foil") + "alien").Diagnostics[0].Code));
        Check("Parse_OversizedSource_RefusesBeforeDecoding", () => Equal("DSL-LIMIT", FoilSource.Parse(new byte[1_048_577]).Diagnostics[0].Code));
        Check("Source_OutwardMutation_PreservesAuthority", () =>
        {
            var parsed = FoilSource.Parse(Example);
            var borrowed = parsed.Source;
            borrowed[0] = 0;
            Equal((byte)'f', parsed.Source[0]);
        });
        Check("Source_PublicConstructor_CannotForgeSuccess", () => Equal(0, typeof(SourceParse).GetConstructors().Length));
        Check("Source_DiagnosticsMutation_Refused", () =>
        {
            var result = FoilSource.Parse([0xff]);
            var list = (IList<Diagnostic>)result.Diagnostics;
            bool refused = false;
            try { list.Clear(); } catch (NotSupportedException) { refused = true; }
            Equal(true, refused);
            Equal(false, result.IsParsed);
            Equal(1, result.Diagnostics.Count);
        });
        Check("Parse_StandaloneAsset_IsSyntaxError", () => Equal("DSL-SYNTAX", Parse("foildsl \"4.0\" section \"s\" { evaluator \"cfdw-cv\" \"1\" asset sha256 \"" + new string('a', 64) + "\" }").Diagnostics[0].Code));
        Check("Parse_FoilSpoofedEof_RejectsTail", () => Equal(false, Parse(Text + " EOF alien").IsParsed));
        Check("Parse_SectionSpoofedEof_RejectsTail", () => Equal(false, Parse(File.ReadAllText("docs/examples/foildsl/section-basic.foil") + " EOF alien").IsParsed));
        Check("Parse_LateNumericOverflow_PrecedesStructuralFailure", () => Equal("DSL-LEX", Parse(Text.Replace("degree 3", "degree 2").Replace("(0.1, 0.12)", "(0.1, 1e400)")).Diagnostics[0].Code));
        Check("Parse_OneAssignment_ReportsSyntax", () => Equal("DSL-SYNTAX", Parse(Text.Replace("at tip profile \"section-a\"", "")).Diagnostics[0].Code));
        Check("Parse_UnpairedEscapedSurrogate_ReportsLexical", () => Equal("DSL-LEX", Parse(Text.Replace("Basic foil", "\\ud800")).Diagnostics[0].Code));
        Check("Diagnostic_CurveError_NamesCurveAndRequirement", () =>
        {
            var diagnostic = Parse(Text.Replace("degree 3", "degree 2")).Diagnostics[0];
            Equal("leading", diagnostic.Entity);
            Equal(true, diagnostic.Reason.Contains("degree 3", StringComparison.Ordinal));
        });
        Check("Diagnostic_MissingReference_NamesTarget", () =>
        {
            var diagnostic = FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/invalid-reference.foil")).Diagnostics[0];
            Equal("missing", diagnostic.Entity);
            Equal(true, diagnostic.Reason.Contains("missing", StringComparison.Ordinal));
        });
        Check("Source_MissingIds_MaterializationPreservesMeaning", () =>
        {
            var parsed = FoilSource.Parse(Example);
            var materialized = FoilSource.MaterializeIds(parsed);
            Equal(parsed.SurfaceHash, FoilSource.Parse(materialized).SurfaceHash);
            Equal(true, materialized.AsSpan().SequenceEqual(FoilSource.MaterializeIds(FoilSource.Parse(materialized))));
        });
        Check("NewDefault_ParsesAndCertifies", () =>
        {
            byte[] source = FoilSource.NewDefault();
            var parsed = FoilSource.Parse(source);
            Equal(true, parsed.IsParsed);
            var definition = parsed.Definition ?? throw new InvalidOperationException("Parsed foil has no definition.");
            Equal("open", definition.Tip);
            Equal("foil", definition.Kind);
            var assessment = Geometry.Assess(parsed);
            Equal(GeometryStatus.Certified, assessment.Status);
            var estimates = WingEstimates.From(source, "accepted", 0);
            Near(1, estimates.SpanMeters, 1e-9);
            Near(0.1, estimates.MeanChordMeters, 1e-6);
            Near(0.1, estimates.TipChordMeters / estimates.RootChordMeters, 1e-9);
            Equal(true, definition.Curves["twist"].Points.All(point => point[1] == 0));
            Equal(true, definition.Curves["dihedral"].Points.All(point => point[1] == 0));
            Equal(0d, definition.Curves["leading"].Points[0][1]);
            Equal(0d, definition.Curves["dihedral"].Points[0][1]);
            Equal(5, definition.Profiles[0].Upper.Degree);
            Equal(3, definition.Curves["leading"].Degree);
            Equal(3, definition.Curves["trailing"].Degree);
            int profileCount = definition.Profiles[0].Upper.Points.Length;
            Equal(true, profileCount is >= 6 and <= 10);
            Equal("4.1", definition.Version);
            Equal(4, definition.Curves["leading"].Points.Length);
            Equal(4, definition.Curves["trailing"].Points.Length);
            Equal(10, definition.Curves["dihedral"].Points.Length);
            Equal(10, definition.Curves["twist"].Points.Length);
            Equal(10, definition.Curves["thickness"].Points.Length);
        });
        Check("NewDefault_SectionResidualWithinAcceptance", () =>
        {
            byte[] source = FoilSource.NewDefault();
            var definition = FoilSource.Parse(source).Definition ?? throw new InvalidOperationException("Parsed foil has no definition.");
            var profile = definition.Profiles[0];
            double normalised = SectionResidual(profile);
            var estimates = WingEstimates.From(source, "accepted", 0);
            double atRoot = normalised * estimates.RootChordMeters;
            double atTip = normalised * estimates.TipChordMeters;
            Console.WriteLine("NACA0012 residual root " + atRoot.ToString("G17") + " m tip " + atTip.ToString("G17") + " m vertices " + profile.Upper.Points.Length.ToString() + " normalised " + normalised.ToString("G17"));
            Equal(true, atRoot <= 10e-6);
            Equal(true, atTip <= 10e-6);
        });
        Check("NewDefault_NoExampleDependency", () =>
        {
            byte[] source = FoilSource.NewDefault();
            string text = Encoding.UTF8.GetString(source);
            foreach (string token in new[] { "Basic foil", "section-a", "Embedded Example", "example.foil", "CfdWorkbench.Example", "foil-basic" })
                Equal(false, text.Contains(token, StringComparison.Ordinal));
            Equal(false, source.AsSpan().SequenceEqual(Example));
        });
        Check("NewDefault_WingEstimates_AreaAndAspectRatioExact", () =>
        {
            byte[] source = FoilSource.NewDefault();
            var estimates = WingEstimates.From(source, "accepted", 0);
            Near(1, estimates.SpanMeters, 1e-6);
            Near(0.1, estimates.AreaSquareMeters, 1e-6);
            Near(10, estimates.AspectRatio, 1e-6);
            Near(0.1, estimates.MeanChordMeters, 1e-6);
            double analyticMac = AnalyticMac(estimates.RootChordMeters);
            double deviation = ChordOracle(source);
            Console.WriteLine("planform deviation " + deviation.ToString("G17") + " m analytic MAC " + analyticMac.ToString("G17") + " m fitted MAC " + estimates.MacMeters.ToString("G17"));
            Equal(true, Math.Abs(estimates.MacMeters - analyticMac) <= deviation);
        });
        Check("NewDefault_PlanformNearElliptic", () =>
        {
            byte[] source = FoilSource.NewDefault();
            var estimates = WingEstimates.From(source, "accepted", 0);
            double deviation = ChordOracle(source);
            double tipError = Math.Abs(estimates.TipChordMeters - 0.10 * estimates.RootChordMeters);
            Console.WriteLine("chord oracle deviation " + deviation.ToString("G17") + " m tip error " + tipError.ToString("G17") + " m");
            Equal(true, tipError <= deviation);
            Near(0.011972960968201975, deviation, 1e-12);
        });
        M12bChecks();
    }

    private static string Fx(string name) => M12bFixtures.Path(name);

    private static void M12bChecks()
    {
        Check("Parse_Foil41WithTangents_RowsParsed", () =>
        {
            var parsed = FoilSource.Parse(File.ReadAllBytes(Fx("foil-41-tangents.foil")));
            Equal(true, parsed.IsParsed);
            var row = parsed.Definition!.Curves["leading"].Tangents.Single();
            Equal("cv-3", row.Id);
            Equal("smooth", row.Kind);
            Equal(true, row.Angle is null);
        });
        Check("Parse_TangentsUnder40_DslSyntax", () => CodeOf(Fx("foil-40-tangents.foil"), "DSL-SYNTAX"));
        Check("Parse_TangentRowOnControlPoint_DslLock", () => CodeOf(Fx("foil-41-row-on-control.foil"), "DSL-LOCK"));
        Check("Parse_AngleKindOnChannel_DslLock", () => CodeOf(Fx("foil-41-angle-on-channel.foil"), "DSL-LOCK"));
        Check("Parse_ElevenChannelPointsUnder40_DslCurve", () => CodeOf(Fx("foil-40-eleven-points.foil"), "DSL-CURVE"));
        Check("Parse_SixteenChannelPointsUnder41_Parsed", () =>
        {
            var parsed = FoilSource.Parse(File.ReadAllBytes(Fx("foil-41-sixteen-three-anchors.foil")));
            Equal(true, parsed.IsParsed);
            var leading = parsed.Definition!.Curves["leading"];
            Equal(16, leading.Points.Length);
            Equal(3, leading.Tangents.Length);
            Equal(true, leading.Tangents.All(row => row.Kind == "smooth"));
        });
        Check("Parse_SeventeenChannelPointsUnder41_DslCurve", () => CodeOf(Fx("foil-41-seventeen-points.foil"), "DSL-CURVE"));
        Check("Parse_Foil42UnknownBlock_DslVersion", () =>
        {
            byte[] source = File.ReadAllBytes(Fx("foil-42-unknown-block.foil"));
            var parsed = FoilSource.Parse(source);
            Equal("DSL-VERSION", parsed.Diagnostics[0].Code);
            Equal(true, source.AsSpan().SequenceEqual(parsed.Source));
        });
        Check("Parse_Foil41RoundTrip_RandomRowsStable", () =>
        {
            var parsed = FoilSource.Parse(File.ReadAllBytes(Fx("foil-41-tangents.foil")));
            Equal(true, parsed.IsParsed);
            var rng = new Random(12041);
            var curves = new Dictionary<string, Curve>(StringComparer.Ordinal);
            foreach (var pair in parsed.Definition!.Curves)
            {
                var points = pair.Value.Points.Select(point => (double[])point.Clone()).ToArray();
                for (int index = 2; index < points.Length - 1; index++)
                    points[index][1] += (rng.NextDouble() - 0.5) * 1e-4;
                var rows = pair.Value.Tangents;
                if (rows.Length > 0 && rng.Next(2) == 0)
                    rows = rows.Select(row => row with { Kind = row.Kind == "smooth" ? "symmetric" : "smooth" }).ToArray();
                curves[pair.Key] = pair.Value with { Points = points, Tangents = rows };
            }
            var mutated = parsed.Definition with { Curves = curves };
            byte[] once = FoilSource.Print(mutated);
            var again = FoilSource.Parse(once);
            Equal(true, again.IsParsed);
            byte[] twice = FoilSource.Print(again.Definition!);
            Equal(true, once.AsSpan().SequenceEqual(twice));
            Equal(mutated.Curves["leading"].Tangents[0].Kind, again.Definition!.Curves["leading"].Tangents[0].Kind);
            Equal(again.SurfaceHash, FoilSource.Parse(twice).SurfaceHash);
        });
        Check("Identity_TangentsRows_DefinitionHashUnchanged", () =>
        {
            byte[] withRows = File.ReadAllBytes(Fx("foil-41-tangents.foil"));
            byte[] stripped = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(withRows).Replace(" tangents { \"cv-3\" smooth }", ""));
            var left = FoilSource.Parse(withRows);
            var right = FoilSource.Parse(stripped);
            Equal(true, left.IsParsed && right.IsParsed);
            Equal(1, left.Definition!.Curves["leading"].Tangents.Length);
            Equal(0, right.Definition!.Curves["leading"].Tangents.Length);
            Equal(left.SurfaceHash, right.SurfaceHash);
        });
        Check("Identity_Header41RewriteSameGeometry_DefinitionHashUnchanged", () =>
        {
            byte[] original = File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil");
            byte[] rewritten = FoilSource.EnsureHeader41(original);
            string text = Encoding.UTF8.GetString(rewritten);
            Equal(true, text.Contains("foildsl \"4.1\"", StringComparison.Ordinal));
            Equal(false, text.Contains("foildsl \"4.0\"", StringComparison.Ordinal));
            var left = FoilSource.Parse(original);
            var right = FoilSource.Parse(rewritten);
            Equal(true, left.IsParsed && right.IsParsed);
            Equal(left.SurfaceHash, right.SurfaceHash);
        });
        Check("EnsureHeader41_FirstRow_HeaderRewritten", () =>
        {
            string labeled40 = File.ReadAllText(Fx("foil-41-tangents.foil")).Replace("foildsl \"4.1\"", "foildsl \"4.0\"", StringComparison.Ordinal);
            byte[] rewritten = FoilSource.EnsureHeader41(Encoding.UTF8.GetBytes(labeled40));
            Equal(true, Encoding.UTF8.GetString(rewritten).Contains("foildsl \"4.1\"", StringComparison.Ordinal));
            var parsed = FoilSource.Parse(rewritten);
            Equal(true, parsed.IsParsed);
            Equal("smooth", parsed.Definition!.Curves["leading"].Tangents[0].Kind);
        });
        Check("EnsureHeader41_Already41_Unchanged", () =>
        {
            byte[] source = File.ReadAllBytes(Fx("foil-41-tangents.foil"));
            byte[] again = FoilSource.EnsureHeader41(source);
            Equal(true, ReferenceEquals(source, again));
        });
    }

    private static void CodeOf(string path, string code)
    {
        var parsed = FoilSource.Parse(File.ReadAllBytes(path));
        Equal(false, parsed.IsParsed);
        Equal(code, parsed.Diagnostics[0].Code);
    }

    private static string Text => Encoding.UTF8.GetString(Example);
    private static void DiagnosticCase(string text, string code, string phase, string span)
    {
        var result = Parse(text);
        Equal(false, result.IsParsed);
        Equal(text, Encoding.UTF8.GetString(result.Source));
        Equal(code, result.Diagnostics[0].Code);
        Equal(phase, result.Diagnostics[0].Phase);
        Equal(span, Encoding.UTF8.GetString(result.Source.AsSpan(result.Diagnostics[0].ByteStart, result.Diagnostics[0].ByteLength)));
    }
    private static SourceParse Parse(string text) => FoilSource.Parse(Encoding.UTF8.GetBytes(text));
    private static void Code(string file, string code) => Equal(code, FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/" + file)).Diagnostics[0].Code);

    private static void Near(double expected, double actual, double relative)
    {
        double scale = Math.Max(1e-30, Math.Abs(expected));
        if (Math.Abs(actual - expected) / scale > relative)
            throw new InvalidOperationException("Expected " + expected.ToString("G17", CultureInfo.InvariantCulture) + "; actual " + actual.ToString("G17", CultureInfo.InvariantCulture));
    }

    private static double SectionResidual(ProfileDefinition profile)
    {
        double worst = 0;
        void Sample(double x)
        {
            double analytic = Naca(x);
            worst = Math.Max(worst, Math.Abs(OrdinateAt(profile.Upper, x) - analytic));
            worst = Math.Max(worst, Math.Abs(OrdinateAt(profile.Lower, x) - (-analytic)));
        }
        for (int index = 0; index < 201; index++)
            Sample(0.5 * (1 - Math.Cos(Math.PI * index / 200d)));
        foreach (double knot in profile.Upper.Knots)
            Sample(Math.Clamp(Abscissa(profile.Upper, knot), 0, 1));
        for (int index = 0; index < 2001; index++)
            Sample(index / 2000d);
        return worst;
    }

    private static double Naca(double x)
    {
        if (x <= 0 || x >= 1) return 0;
        double s = Math.Sqrt(x);
        double x2 = x * x;
        return 0.6 * (0.2969 * s - 0.1260 * x - 0.3516 * x2 + 0.2843 * x2 * x - 0.1036 * x2 * x2);
    }

    private static double ChordOracle(byte[] source)
    {
        var definition = FoilSource.Parse(source).Definition ?? throw new InvalidOperationException("Parsed foil has no definition.");
        var leading = definition.Curves["leading"];
        var trailing = definition.Curves["trailing"];
        double root = ChannelAt(trailing, 0) - ChannelAt(leading, 0);
        var stations = new List<double>();
        for (int index = 0; index < 201; index++) stations.Add(index / 200d);
        foreach (var curve in new[] { leading, trailing })
            foreach (double knot in curve.Knots)
                stations.Add(Math.Clamp(Abscissa(curve, knot), 0, 1));
        double deviation = 0;
        foreach (double eta in stations)
        {
            double chord = ChannelAt(trailing, eta) - ChannelAt(leading, eta);
            double analytic = root * Math.Sqrt(Math.Max(0, 1 - 0.99 * eta * eta));
            deviation = Math.Max(deviation, Math.Abs(chord - analytic));
        }
        return deviation;
    }

    private static double AnalyticMac(double rootChord)
    {
        double Chord(double eta) => rootChord * Math.Sqrt(Math.Max(0, 1 - 0.99 * eta * eta));
        const int panels = 2000;
        double h = 1d / panels;
        double Integrate(Func<double, double> f)
        {
            double sum = f(0) + f(1);
            for (int index = 1; index < panels; index++)
                sum += (index % 2 == 0 ? 2 : 4) * f(index * h);
            return sum * h / 3;
        }
        double area = Integrate(Chord);
        double square = Integrate(eta => { double chord = Chord(eta); return chord * chord; });
        return square / area;
    }

    private static double ChannelAt(Curve curve, double eta)
    {
        if (eta <= 0) return Ordinate(curve, 0);
        if (eta >= 1) return Ordinate(curve, 1);
        double low = 0, high = 1;
        for (int step = 0; step < 60; step++)
        {
            double mid = 0.5 * (low + high);
            if (Abscissa(curve, mid) < eta) low = mid;
            else high = mid;
        }
        return Ordinate(curve, 0.5 * (low + high));
    }

    private static double OrdinateAt(Curve curve, double x)
    {
        double At(double parameter) => Abscissa(curve, parameter);
        if (x <= At(0)) return Ordinate(curve, 0);
        if (x >= At(1)) return Ordinate(curve, 1);
        double low = 0, high = 1;
        for (int step = 0; step < 80; step++)
        {
            double mid = 0.5 * (low + high);
            if (At(mid) < x) low = mid;
            else high = mid;
        }
        return Ordinate(curve, 0.5 * (low + high));
    }

    private static double Abscissa(Curve curve, double parameter) => Dot(curve, parameter, 0);
    private static double Ordinate(Curve curve, double parameter) => Dot(curve, parameter, 1);

    private static double Dot(Curve curve, double parameter, int coordinate)
    {
        double[] basis = SplineBasis.Values(curve.Knots, curve.Degree, parameter);
        double sum = 0;
        for (int index = 0; index < curve.Points.Length; index++) sum += basis[index] * curve.Points[index][coordinate];
        return sum;
    }
}
