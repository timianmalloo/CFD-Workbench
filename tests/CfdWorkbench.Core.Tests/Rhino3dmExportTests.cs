using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure in-memory MIT Rhino3dm 8.35.0 export &amp; round-trip verification tests (ADR-0013, STORE-SUBSET compliant).
/// Uses File3dm.ToByteArray() / File3dm.FromByteArray(byte[]) with zero filesystem I/O.
/// </summary>
internal static class Rhino3dmExportTests
{
    internal static void Run()
    {
        Check("Rhino3dmExport_WholeAndHalf_LiveRoundTrip_MillimetresAndThreeLayers", WholeAndHalfRoundTrip);
        Check("Rhino3dmExport_ZeroPii_AndPlantedMarkerCaughtRed", ZeroPiiAndNegativeControl);
        Check("Rhino3dmExport_FiveBenchmarkWings_DeviationUnder10Um_AndTeMatchesStl", FiveBenchmarkWings);
        Check("Rhino3dmExport_FileName_SanitizesHostileNames", FileNames);
    }

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void WholeAndHalfRoundTrip()
    {
        foreach (var (label, source, openTe) in new[]
        {
            ("open-026", StepExportTests.OpenTrailingEdge(0.001), true),
            ("closed", Encoding.UTF8.GetBytes(StepExportTests.ExampleText()), false),
        })
        {
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                var result = Rhino3dmExport.Build(source, "Basic foil", 1, scope);
                True(result.Bytes.Length > 1000, $"{label}/{scope}: empty 3dm payload");
                True(result.Verification.Passed, $"{label}/{scope}: verification failed: {string.Join("; ", result.Verification.Errors)}");
                Equal("Millimeters", result.Verification.UnitSystem);
                Equal(3, result.Verification.LayerNames.Count);
                Equal("Wing Skin", result.Verification.LayerNames[0]);
                Equal("Master Curves", result.Verification.LayerNames[1]);
                Equal("Authored Stations", result.Verification.LayerNames[2]);
                int expectedSurfaces = (scope == StlScope.Whole ? 2 : 1) * (openTe ? 3 : 2);
                Equal(expectedSurfaces, result.Verification.SurfaceCount);
                True(result.Verification.MasterCurveCount >= 5, $"{label}/{scope}: expected >= 5 master curves, got {result.Verification.MasterCurveCount}");
                True(result.Verification.AuthoredStationCurveCount >= 2, $"{label}/{scope}: expected >= 2 authored station curves");
                Equal(3, result.DegreeU);
                Equal(3, result.DegreeV);
                True(result.DeviationMm <= 0.010, $"{label}/{scope}: deviation {result.DeviationMm * 1000:F3} um > 10 um");

                // Verify openNURBS knot slicing reproduces exact CadNurbsSurface evaluation on read-back.
                var skin = CadExportExtractor.BuildSkin(source);
                using var readBack = Rhino.FileIO.File3dm.FromByteArray(result.Bytes);
                var upperRb = readBack.Objects.First(o => o.Attributes.Name == "Wing Skin Upper (Starboard)").Geometry as Rhino.Geometry.NurbsSurface
                    ?? throw new InvalidOperationException("Missing read-back Wing Skin Upper (Starboard)");
                foreach (double u in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
                {
                    foreach (double v in new[] { 0.0, 0.3, 0.7, 1.0 })
                    {
                        var expectedPt = skin.UpperStarboard.Evaluate(u, v);
                        var rbPt = upperRb.PointAt(u, v);
                        double diffMm = Math.Sqrt(
                            Math.Pow(expectedPt.X - rbPt.X, 2) +
                            Math.Pow(expectedPt.Y - rbPt.Y, 2) +
                            Math.Pow(expectedPt.Z - rbPt.Z, 2));
                        True(diffMm < 1e-6, $"{label}/{scope} 3DM read-back surface mismatch at ({u},{v}): {diffMm:E3} mm");
                    }
                }
            }
        }
    }

    private static void ZeroPiiAndNegativeControl()
    {
        byte[] source = Encoding.UTF8.GetBytes(StepExportTests.ExampleText());
        var result = Rhino3dmExport.Build(source, "Basic foil", 1, StlScope.Half);
        Equal(true, result.Verification.ZeroPiiVerified);

        // Verify raw Latin-1/ASCII scan rejects a planted synthetic PII token inside the 3DM notes/bytes.
        const string marker = "SYNTH-3DM-PII-9X";
        byte[] tainted = Rhino3dmExport.BuildWithNotesForTest(source, "Basic foil", 1, StlScope.Half, marker);
        var ver = Rhino3dmExport.Verify(tainted, StlScope.Half, [marker]);
        Equal(false, ver.Passed);
        Equal(false, ver.ZeroPiiVerified);
    }

    private static void FiveBenchmarkWings()
    {
        foreach (var (id, name, source, expectedOpenTe) in StepExportTests.FiveWings())
        {
            var res = Rhino3dmExport.Build(source, name, 1, StlScope.Half);
            var stl = StlExport.Build(source, name, 1, StlScope.Half, StlExport.FineMm);
            Equal(expectedOpenTe, res.IsOpenTrailingEdge);
            True(res.Verification.Passed, $"{id}: {string.Join("; ", res.Verification.Errors)}");
            True(res.DeviationMm <= 0.010, $"{id}: deviation {res.DeviationMm * 1000:F3} um > 10 um");
            double teDiffUm = Math.Abs(res.TrailingEdgeMm - stl.TrailingEdgeMm) * 1000.0;
            True(teDiffUm <= 1.0, $"{id}: TE disagreement vs StlExport {teDiffUm:F3} um > 1 um");
        }
    }

    private static void FileNames()
    {
        Equal("basic-foil-r1.3dm", Rhino3dmExport.FileName("Basic foil", 1, StlScope.Whole));
        Equal("basic-foil-r1-half.3dm", Rhino3dmExport.FileName("Basic foil", 1, StlScope.Half));
        string hostile = Rhino3dmExport.FileName("../../C:\\evil", 3, StlScope.Half);
        True(!hostile.Contains('/') && !hostile.Contains('\\') && !hostile.Contains("..") && !hostile.Contains(':'), hostile);
    }
}
