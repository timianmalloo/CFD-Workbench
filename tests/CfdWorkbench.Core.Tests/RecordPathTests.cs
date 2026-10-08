using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Ruling 157, track RWF: the record-write path. The New-project default is a bit golden (fast ring, under 0.1 s).
// The fitted-project families are tolerance checks: each regenerates committed Mac-written project bytes from a committed
// input and asserts every knot and control-point coordinate is within the 1e-6 chord identity tolerance (profile coordinates
// are chord-normalised), and prints the measured max drift, so the PC ring reports a real cross-OS number
// (docs/proof/rwf/red-first.md). A drift line reads `DRIFT <family> max_abs=<value> limit=1e-06`.
internal static class RecordPathTests
{
    private const double IdentityTolerance = 1e-6;
    private static readonly Regex IdsList = new(@"\]\s+ids\b", RegexOptions.CultureInvariant);
    private static readonly Regex Number =new(@"-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);

    internal static void Run()
    {
        Check("NewDefault_ControlPointDoubles_BitGolden", NewDefaultBitGolden);
        Check("RecordPath_DatRotation_ReplaceBytesWithinIdentityTolerance", DatRotation);
        Check("RecordPath_LambdaFit_FairBytesWithinIdentityTolerance", LambdaFit);
        Check("RecordPath_TangentAngle_FairBytesWithinIdentityTolerance", TangentAngle);
        Check("RecordPath_HandlePolar_SetTangentAndHandleTargetWithinIdentityTolerance", HandlePolar);
    }

    private static string Fixture(string name) =>
        Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures", "record-path", name);

    // One row per curve: the curve's label, then every knot and control-point coordinate. The text of a source prints each
    // double with its exact decimal expansion, so parsing it back is lossless.
    private static List<(string Label, double[] Values)> Curves(byte[] source)
    {
        var rows = new List<(string, double[])>();
        string context = "";
        foreach (string line in Encoding.UTF8.GetString(source).ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("profile ", StringComparison.Ordinal)) context = trimmed.Split(' ')[1].Trim('"') + "/";
            int cv = trimmed.IndexOf(" cv {", StringComparison.Ordinal);
            int knots = trimmed.IndexOf("knots [", StringComparison.Ordinal);
            var idsMatch = IdsList.Match(trimmed);
            int ids = idsMatch.Success ? idsMatch.Index : -1;
            if (cv < 0 || knots < 0 || ids < 0) continue;
            rows.Add((context + trimmed[..cv], Number.Matches(trimmed[(knots + "knots [".Length)..ids])
                .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray()));
        }
        return rows;
    }

    // IEEE bits (hex) of every curve double, one row per curve.
    internal static string BitRows(byte[] source) => string.Join("\n", Curves(source).Select(row =>
        row.Label + " " + string.Join(" ", row.Values.Select(value => BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture)))));

    private static void NewDefaultBitGolden()
    {
        string golden = File.ReadAllText(Fixture("new-default-bits.txt")).ReplaceLineEndings("\n").TrimEnd('\n');
        Equal(golden, BitRows(FoilSource.NewDefault()));
    }

    // The same curves, the same shape, every double within the identity tolerance. Returns the measured max drift.
    private static void WithinTolerance(string family, byte[] regenerated, string expectedName)
    {
        var expected = Curves(File.ReadAllBytes(Fixture(expectedName)));
        var actual = Curves(regenerated);
        // A parser that finds no curve would pass vacuously: the planform has 5 channel curves and the profile 2.
        Equal(7, expected.Count);
        Equal(true, expected.All(row => row.Values.Length >= 8));   // a row with no values would compare nothing
        Equal(expected.Count, actual.Count);
        double drift = 0;
        for (int row = 0; row < expected.Count; row++)
        {
            Equal(expected[row].Label, actual[row].Label);
            Equal(expected[row].Values.Length, actual[row].Values.Length);
            for (int index = 0; index < expected[row].Values.Length; index++)
                drift = Math.Max(drift, Math.Abs(expected[row].Values[index] - actual[row].Values[index]));
        }
        Console.WriteLine("DRIFT " + family + " max_abs=" + drift.ToString("G17", CultureInfo.InvariantCulture) + " limit=1e-06");
        Equal(true, drift <= IdentityTolerance);
    }

    private static byte[] Example() => FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil")));

    // DatImport.ParseInChordFrame (:524 Atan2/Cos/Sin) and SourceCurve (:460): a user .dat turned 3 degrees, replaced into the example.
    private static void DatRotation()
    {
        byte[] dat = File.ReadAllBytes(Fixture("turned-naca4412-3deg.dat"));
        byte[] bytes = Example();
        var definition = FoilSource.Parse(bytes).Definition!;
        int profile = definition.Assignments[0].Profile;
        var scope = definition.Assignments.Count(item => item.Profile == profile) > 1 ? SectionScope.Shared : SectionScope.Independent;
        var source = new ReplaceSource.Coordinates("NACA 4412 turned", new Provenance("dat:sha256:" + Identity.Sha256(dat), false), dat);
        var preview = SectionReplace.Preview(bytes, 0, scope, source, ReplaceScope.Draft);
        Equal(true, preview.Bytes is not null);
        WithinTolerance("dat-rotation", preview.Bytes!, "dat-rotation.out.foil");
    }

    // ProfileFair.FitSide (:281): the lambda bisection steps by the geometric midpoint Exp((Log lo + Log hi) / 2).
    private static void LambdaFit()
    {
        var applied = SectionEdits.Apply(File.ReadAllBytes(Fixture("fair-lambda.in.foil")), 0, new SectionStep.Fair(null, 1e-3, PreserveEnds.Position));
        WithinTolerance("lambda-fit", applied.Bytes, "fair-lambda.out.foil");
    }

    // ProfileFair.AddTangentRows (:400): the input carries an `angle` tangent row, so Fair adds the cos/sin equality rows.
    private static void TangentAngle()
    {
        byte[] input = File.ReadAllBytes(Fixture("angle-tangent.in.foil"));
        Equal(true, Encoding.UTF8.GetString(input).Contains("angle", StringComparison.Ordinal));
        var applied = SectionEdits.Apply(input, 0, new SectionStep.Fair(null, 1e-3, PreserveEnds.Position));
        WithinTolerance("tangent-angle", applied.Bytes, "angle-tangent.out.foil");
    }

    // SectionEdits.PlaceHandles (:415, angle kind) writes the section; Planform.HandleTarget (:91-92) gives the handle the
    // Properties pane then moves (PropertiesView.Destination -> RunTypedGesture -> a point move written to the rail).
    private static void HandlePolar()
    {
        var applied = SectionEdits.Apply(File.ReadAllBytes(Fixture("handle-polar.in.foil")), 0,
            new SectionStep.SetTangent(SurfaceSide.Upper, "cv-4", TangentKind.Angle, 20, null));
        WithinTolerance("handle-polar-section", applied.Bytes, "handle-polar.out.foil");
        var view = Planform.View(File.ReadAllBytes(M12bFixtures.Path("foil-41-tangents.foil")), "spline", 1);
        var rows = HandleRows(view);
        string[] expected = File.ReadAllText(Fixture("handle-target.tsv")).ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        Equal(expected.Length, rows.Count);
        double drift = 0;
        for (int row = 0; row < rows.Count; row++)
        {
            string[] fields = expected[row].Split('\t');
            Equal(rows[row].Label, fields[0]);
            drift = Math.Max(drift, Math.Abs(rows[row].Span - double.Parse(fields[1], CultureInfo.InvariantCulture)));
            drift = Math.Max(drift, Math.Abs(rows[row].Aft - double.Parse(fields[2], CultureInfo.InvariantCulture)));
        }
        Console.WriteLine("DRIFT handle-polar-target max_abs=" + drift.ToString("G17", CultureInfo.InvariantCulture) + " limit=1e-06");
        Equal(true, drift <= IdentityTolerance);
    }

    private static List<(string Label, double Span, double Aft)> HandleRows(PlanformView view)
    {
        var rows = new List<(string, double, double)>();
        foreach (double angle in new[] { 0, 20, 37.5, 90, 123.4, -15, 180 })
        {
            var target = Planform.HandleTarget(view, "leading", "cv-4", angle, 0.05);
            rows.Add((angle.ToString("G", CultureInfo.InvariantCulture), target.SpanMeters, target.Ordinate));
        }
        return rows;
    }
}
