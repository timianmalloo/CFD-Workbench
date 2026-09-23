using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class FoilSourceTests
{
    internal static byte[] Example => File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil");
    internal static void Run()
    {
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
    private static SourceParse Parse(string text) => FoilSource.Parse(Encoding.UTF8.GetBytes(text));
    private static void Code(string file, string code) => Equal(code, FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/" + file)).Diagnostics[0].Code);
}
