using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure-C# Option B STEP AP203 export &amp; CadExportExtractor tests (ADR-0013, docs/design/export-step-3dm.md).
/// Strictly pure in-memory (STORE-SUBSET compliant): reads only committed fixtures via PlacementTests.RepoRoot().
/// </summary>
internal static class StepExportTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    internal static void Run()
    {
        Check("StepExport_ChannelKnotMapping_MapsParamToPhysicalEta", ChannelKnotMapping);
        Check("StepExport_FiveBenchmarkWings_DeviationUnder10Um_AndTeMatchesStl", FiveBenchmarkWings);
        Check("StepExport_All20Combinations_WholeHalf_ClosedUntrimmed_PassVerifierAndDeterministic", All20Combinations);
        Check("StepExport_Verifier_OpenShell_SeparatesBoundaryFromInteriorEdges", OpenShellBoundaryVsInterior);
        Check("StepExport_NegativeControls_TwistOmissionAndPiiCaughtRed", NegativeControls);
        Check("StepExport_FileName_SanitizesHostileNamesAndVariants", FileNames);
    }

    internal static string RepoFile(params string[] parts) => Path.Combine([PlacementTests.RepoRoot(), .. parts]);

    internal static string ExampleText() => File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Assets", "example.foil"));

    internal static byte[] OpenTrailingEdge(double half) => Encoding.UTF8.GetBytes(ExampleText()
        .Replace("(0.9, 0.01), (1, 0)] ids", $"(0.9, 0.01), (1, {half.ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("(0.9, -0.01), (1, 0)] ids", $"(0.9, -0.01), (1, {(-half).ToString("R", Invariant)})] ids", StringComparison.Ordinal)
        .Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal)
        .Replace("\"cv-6\", \"cv-7\"] }\r\n    }", "\"cv-6\", \"cv-7\"] }\r\n      closure open\r\n    }", StringComparison.Ordinal));

    internal static byte[] Adr0010BlendedTwisted()
    {
        string blendedText = File.ReadAllText(RepoFile("tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2", "blended-dihedral.foil"));
        string twisted = blendedText.Replace(
            "points [(0, 0), (0.1, 0), (0.3, -0.25), (0.5, -0.5), (0.7, -1), (0.9, -1.5), (1, -2)]",
            "points [(0, 0), (0.1, 0), (0.3, -1), (0.5, -2), (0.7, -4), (0.9, -6), (1, -8)]",
            StringComparison.Ordinal);
        return FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(twisted)));
    }

    internal static IReadOnlyList<(string Id, string Name, byte[] Source, bool ExpectedOpenTe)> FiveWings() =>
    [
        ("example-open-026", "Basic foil open 0.26", OpenTrailingEdge(0.001), true),
        ("example-open-036", "Basic foil open 0.36", OpenTrailingEdge(0.0014), true),
        ("example-closed", "Basic foil", Encoding.UTF8.GetBytes(ExampleText()), false),
        ("untitled-naca0012-closed", "Untitled NACA 0012", FoilSource.NewDefault(), false),
        ("adr0010-blended-twisted", "ADR0010 Blended Twisted", Adr0010BlendedTwisted(), false),
    ];

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // KRN-SPK Phase 1 note: channel knot parameter t_i must map to physical eta_i = x_channel(t_i), never raw t_i.
    private static void ChannelKnotMapping()
    {
        // Skew the twist channel's 7 control-point abscissae so interior knots t=0.25, 0.5, 0.75 map to physical
        // etas well away from raw t_i (for knots [0,0,0,0, 0.25, 0.5, 0.75, 1,1,1,1] at t=0.5, basis N_2=1/6, N_3=4/6, N_4=1/6).
        string example = ExampleText();
        string skewed = Regex.Replace(
            example,
            @"twist cv \{[^\}]+\}",
            "twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.45, 0), (0.60, -0.25), (0.72, -0.5), (0.82, -1), (0.92, -1.5), (1, -2)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] }");
        byte[] bytes = Encoding.UTF8.GetBytes(skewed);
        var parsed = FoilSource.Parse(bytes);
        True(parsed.IsParsed, "skewed foil must parse: " + string.Join("; ", parsed.Diagnostics.Select(d => d.Code + ":" + d.Reason)));

        double[] mapped = CadExportExtractor.ExtractChannelKnotEtas(bytes);
        double expectedEtaMid = (0.60 + 4.0 * 0.72 + 0.82) / 6.0; // 0.7166666667 (not raw t=0.5)
        True(mapped.Any(e => Math.Abs(e - expectedEtaMid) < 1e-9),
            $"expected mapped knot eta {expectedEtaMid:F6} in [{string.Join(", ", mapped.Select(v => v.ToString("F4", Invariant)))}]");
        // Confirm the skin itself seeded that exact physical eta (not missing it due to raw-knot assumption).
        var skin = CadExportExtractor.BuildSkin(bytes);
        True(skin.MappedChannelKnotEtas.Any(e => Math.Abs(e - expectedEtaMid) < 1e-9), "skin must record mapped channel knot eta");
        True(skin.StationEtas.Any(e => Math.Abs(e - expectedEtaMid) < 1e-6), "skin station etas must include x_channel(t_i)");
        True(skin.Deviation.MaxClosestPointMm <= 0.010, $"skewed wing deviation {skin.Deviation.MaxClosestPointMm * 1000:F3} um > 10 um");
    }

    private static void FiveBenchmarkWings()
    {
        foreach (var (id, name, source, expectedOpenTe) in FiveWings())
        {
            var skin = CadExportExtractor.BuildSkin(source);
            var stl = StlExport.Build(source, name, 1, StlScope.Half, StlExport.FineMm);
            Equal(expectedOpenTe, skin.IsOpenTrailingEdge);
            Equal(3, skin.UpperStarboard.DegreeU);
            Equal(3, skin.UpperStarboard.DegreeV);
            True(skin.Deviation.MaxClosestPointMm <= 0.010,
                $"{id}: max closest-point deviation {skin.Deviation.MaxClosestPointMm * 1000:F3} um exceeds 10 um");
            True(skin.Deviation.TipPlanarityErrorMm <= 0.001, $"{id}: tip planarity error {skin.Deviation.TipPlanarityErrorMm * 1000:F4} um > 1 um");
            True(skin.Deviation.RootPlanarityErrorMm <= 0.001, $"{id}: root planarity error {skin.Deviation.RootPlanarityErrorMm * 1000:F4} um > 1 um");
            double teDiffUm = Math.Abs(skin.TrailingEdgeMinMm - stl.TrailingEdgeMm) * 1000.0;
            True(teDiffUm <= 1.0, $"{id}: TE disagreement vs StlExport {teDiffUm:F3} um > 1 um");
        }
    }

    private static void All20Combinations()
    {
        ICadStepExporter exporter = new StepAp203Export();
        Equal("option-b-pure-csharp-ap203", exporter.EngineId);
        foreach (var (id, name, source, expectedOpenTe) in FiveWings())
        {
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                foreach (var variant in new[] { StepVariant.ClosedShell, StepVariant.Untrimmed })
                {
                    var r1 = exporter.Export(source, name, 1, scope, variant);
                    var r2 = exporter.Export(source, name, 1, scope, variant);
                    True(r1.Bytes.AsSpan().SequenceEqual(r2.Bytes), $"{id}/{scope}/{variant} is not byte-deterministic");
                    True(r1.Verification.Passed, $"{id}/{scope}/{variant} verification failed: {string.Join("; ", r1.Verification.Errors)}");
                    Equal(expectedOpenTe, r1.IsOpenTrailingEdge);
                    True(r1.DeviationMm <= 0.010, $"{id}/{scope}/{variant} deviation {r1.DeviationMm * 1000:F3} um > 10 um");
                    if (variant == StepVariant.ClosedShell)
                    {
                        Equal(2, r1.Verification.EulerCharacteristic);
                        Equal(0, r1.Verification.BoundaryEdgeCount);
                        Equal(0, r1.Verification.NonManifoldEdgeCount);
                        Equal(r1.Verification.EdgeCount, r1.Verification.InteriorEdgeCount);
                        Equal(true, r1.Verification.EveryEdgePairedOpposite);
                    }
                    else
                    {
                        Equal(0, r1.Verification.EulerCharacteristic);
                        True(r1.Verification.BoundaryEdgeCount > 0, $"{id}/{scope}/untrimmed must report boundary edges");
                        True(r1.Verification.InteriorEdgeCount > 0, $"{id}/{scope}/untrimmed must report interior edges");
                        Equal(0, r1.Verification.NonManifoldEdgeCount);
                    }
                }
            }
        }
    }

    // KRN-SPK Phase 1 note: StepVerifier must explicitly separate boundary (degree 1) vs interior (degree 2, opposite sense) edges on OPEN_SHELL.
    private static void OpenShellBoundaryVsInterior()
    {
        byte[] openSource = OpenTrailingEdge(0.001);
        var halfUntrimmed = StepAp203Export.Build(openSource, "Basic foil", 1, StlScope.Half, StepVariant.Untrimmed);
        var v = halfUntrimmed.Verification;
        True(v.Passed, "half open-TE untrimmed must pass");
        // Half open-TE untrimmed has 3 faces (upper, lower, TE strip): 3 interior seams (stbd_le, stbd_te_u, stbd_te_l)
        // and 6 boundary edges (root_u, root_l, root_te, tip_u, tip_l, tip_te).
        Equal(6, v.BoundaryEdgeCount);
        Equal(3, v.InteriorEdgeCount);
        Equal(0, v.NonManifoldEdgeCount);

        // Mutate one interior edge's orientation in ORIENTED_EDGE so both faces traverse e_stbd_le in the same sense (+2, -0).
        string text = Encoding.UTF8.GetString(halfUntrimmed.Bytes);
        var m = Regex.Match(text, @"#(\d+)=EDGE_CURVE\('e_stbd_le',");
        True(m.Success, "must locate e_stbd_le entity");
        string leId = m.Groups[1].Value;
        string mutated = text.Replace($"#{leId}, .F.)", $"#{leId}, .T.)", StringComparison.Ordinal);
        True(mutated != text, "mutation must flip one ORIENTED_EDGE sense on e_stbd_le");

        var bad = StepVerifier.Verify(mutated, StepVariant.Untrimmed);
        Equal(false, bad.Passed);
        Equal(1, bad.NonManifoldEdgeCount);
        True(bad.Errors.Any(e => e.Contains("OPEN_SHELL", StringComparison.Ordinal)), "error must cite OPEN_SHELL interior edge orientation");
    }

    private static void NegativeControls()
    {
        byte[] twisted = Adr0010BlendedTwisted();
        string twistedText = Encoding.UTF8.GetString(twisted);
        string untwistedText = Regex.Replace(
            twistedText,
            @"twist cv \{[^\}]+\}",
            "twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }");
        byte[] untwisted = Encoding.UTF8.GetBytes(untwistedText);

        var mutantSkin = CadExportExtractor.BuildSkin(untwisted);
        var devAgainstTrue = CadExportExtractor.MeasureDeviationAgainstPlacementSurface(twisted, mutantSkin);
        True(devAgainstTrue.MaxClosestPointMm > 0.010,
            $"twist omission must fail RED (> 10 um), got {devAgainstTrue.MaxClosestPointMm * 1000:F2} um");

        var baselineSkin = CadExportExtractor.BuildSkin(twisted);
        const string marker = "SYNTH-PII-MARKER-9X";
        string piiStep = StepAp203Export.WriteStepText(baselineSkin, "ADR0010", 1, StlScope.Half, StepVariant.ClosedShell,
            new StepHeaderOverrides(Author: marker));
        var verPii = StepVerifier.Verify(piiStep, StepVariant.ClosedShell, [marker]);
        Equal(false, verPii.Passed);
        Equal(false, verPii.ZeroPiiHeaderValid);

        string tsStep = StepAp203Export.WriteStepText(baselineSkin, "ADR0010", 1, StlScope.Half, StepVariant.ClosedShell,
            new StepHeaderOverrides(Timestamp: "2026-11-23T12:00:00"));
        var verTs = StepVerifier.Verify(tsStep, StepVariant.ClosedShell);
        Equal(false, verTs.Passed);
        Equal(false, verTs.ZeroPiiHeaderValid);
    }

    private static void FileNames()
    {
        Equal("basic-foil-r1.step", StepAp203Export.FileName("Basic foil", 1, StlScope.Whole, StepVariant.ClosedShell));
        Equal("basic-foil-r1-half.step", StepAp203Export.FileName("Basic foil", 1, StlScope.Half, StepVariant.ClosedShell));
        Equal("basic-foil-r1-untrimmed.step", StepAp203Export.FileName("Basic foil", 1, StlScope.Whole, StepVariant.Untrimmed));
        Equal("basic-foil-r1-half-untrimmed.step", StepAp203Export.FileName("Basic foil", 1, StlScope.Half, StepVariant.Untrimmed));
        string hostile = StepAp203Export.FileName("../../C:\\evil", 2, StlScope.Whole, StepVariant.ClosedShell);
        True(!hostile.Contains('/') && !hostile.Contains('\\') && !hostile.Contains("..") && !hostile.Contains(':'), hostile);
    }
}
