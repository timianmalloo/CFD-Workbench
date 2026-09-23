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
        Check("Patch_LeadingRail_ExactSiAndOnlyOwnedToken", () =>
        {
            var source = FoilSource.MaterializeIds(FoilSource.Parse(Example));
            var parsed = FoilSource.Parse(source);
            var patched = FoilSource.PatchRail(parsed, "leading", "cv-2", 0.014049);
            Equal(true, FoilSource.Parse(patched).IsParsed);
            Equal(true, source.AsSpan().SequenceEqual(parsed.Source));
            var originalText = Encoding.UTF8.GetString(source);
            var resultText = Encoding.UTF8.GetString(patched);
            int end = originalText.IndexOf("trailing", StringComparison.Ordinal);
            Equal(originalText[end..], resultText[resultText.IndexOf("trailing", StringComparison.Ordinal)..]);
            var twice = FoilSource.PatchRail(FoilSource.Parse(patched), "leading", "cv-2", 0.014049);
            Equal(true, patched.AsSpan().SequenceEqual(twice));
            Equal(false, source.AsSpan().SequenceEqual(patched));
        });
        Check("Patch_MissingIds_RefusesImplicitIdentityInsertion", () => Refuses("DSL-PATCH", () => FoilSource.PatchRail(FoilSource.Parse(Example), "leading", "cv-2", 1)));
        Check("Patch_NonRailTarget_Refuses", () => Refuses("DSL-PATCH", () => FoilSource.PatchRail(FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(Example))), "twist", "cv-2", 1)));
        Check("Patch_Nonfinite_Refuses", () => Refuses("DSL-PATCH", () => FoilSource.PatchRail(FoilSource.Parse(FoilSource.MaterializeIds(FoilSource.Parse(Example))), "leading", "cv-2", double.NaN)));
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
}
