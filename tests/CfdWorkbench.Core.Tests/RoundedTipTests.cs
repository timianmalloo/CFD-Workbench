using System.Globalization;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Wave 13A Rounded &amp; Radiused Wing Tips (<c>FoilDSL 4.2</c>) test suite covering T-RT-01 through T-RT-07
/// plus STRIDE/LINDDUN negative controls (docs/design/rounded-wing-tips.md §9). Pure zero-I/O (STORE-SUBSET clean).
/// </summary>
internal static class RoundedTipTests
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static void Run()
    {
        // T-RT-01: FoilDSL 4.2 parse/print/round-trip, 4.0/4.1 identity preservation, EnsureHeader41 non-downgrading
        Check("RoundedTip_01_ParsePrintRoundTripAndIdentity", T01_ParsePrintRoundTripAndIdentity);
        // T-RT-02: Hostile parser & geometry bounds (STRIDE)
        Check("Parse_Foil42_HostileTipAndTangentInputs_Refused", T02_HostileTipAndTangentInputsRefused);
        // T-RT-03: Exact general-N tip radius math across N = 4, 5, 7 (incl. triple knot) & +/-90 deg TipAngleDegrees
        Check("RoundedTip_03_TipRadiusMatchesCoxDeBoorAcrossN4_5_7", T03_TipRadiusMatchesCoxDeBoor);
        // T-RT-04: Bernstein.CertifyPositiveChord BFS (25 deg swept wing, crossing -> DSL-EDGES-CROSS, hostile 16-CV -> NotAssessed)
        Check("RoundedTip_04_PositiveChordCertifies25DegSweepAndRefusesCrossing", T04_PositiveChordSweepAndCrossing);
        Check("Assess_HostileNearTouching16CvRails_BoundedNotAssessed", T04_HostileNearTouching16CvNotAssessed);
        // T-RT-05: ChannelEdits, PointModel, ChordDimension, AuthoringSession tip edits, Undo, save/reopen, forged receipt, telemetry
        Check("RoundedTip_05_EditingVerbsUndoAndLegacyTipChord", T05_EditingVerbsUndoAndLegacyTipChord);
        Check("Reopen_ForgedTipEditReceipt_DocReference", T05_ForgedTipEditReceiptRefused);
        Check("Telemetry_TipEdits_NoNamesPathsOrIds", T05_TelemetryZeroPii);
        // T-RT-06: WingEstimates s^2 terminal MAC quadrature & OverallSpanMeters vs STL/STEP
        Check("RoundedTip_06_WingEstimatesMacAndOverallSpan", T06_WingEstimatesMacAndOverallSpan);
        // T-RT-07: Watertight STL/3MF/STEP AP203/Rhino 3DM for closed-TE & open-TE x Whole & Half + chordal-v skinning
        Check("RoundedTip_07_WatertightExportsClosedAndOpenTe", T07_WatertightExportsClosedAndOpenTe);
        Check("StepExport_RoundedTip_ZeroPiiHeaderVerified", T07_StepExportZeroPiiHeaderVerified);
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{label}: expected {expected:G12} ± {tolerance:G4}, actual {actual:G12}");
    }

    /// <summary>Builds a valid clean FoilDSL document with configurable version, rails, tip clause, and closure.</summary>
    private static byte[] BuildFoil(
        string version = "4.2",
        double halfSpanMm = 457.5,
        string? leadingBody = null,
        string? trailingBody = null,
        string tipClause = "tip open",
        bool openTe = false,
        double openTeHalfGap = 0.01,
        string name = "Test Foil")
    {
        string le = leadingBody ??
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.33, 0), (0.67, 20), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]";
        string te = trailingBody ??
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 112.4), (0.33, 112.4), (0.67, 92), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]";
        string flat = "degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.08, 0), (0.25, 0), (0.5, 0), (0.75, 0), (0.92, 0), (1, 0)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"]";
        string thick = "degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0.10), (0.08, 0.10), (0.25, 0.10), (0.5, 0.098), (0.75, 0.095), (0.92, 0.093), (1, 0.092)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"]";
        string teU = openTe ? openTeHalfGap.ToString("G17", CultureInfo.InvariantCulture) : "0";
        string teL = openTe ? (-openTeHalfGap).ToString("G17", CultureInfo.InvariantCulture) : "0";
        string closure = openTe ? "open" : "closed";
        string upper = $"degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.03), (0.2, 0.06), (0.5, 0.05), (0.8, 0.025), (1, {teU})] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        string lower = $"degree 5 knots [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.03), (0.2, -0.06), (0.5, -0.05), (0.8, -0.025), (1, {teL})] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        string text =
            $"foildsl \"{version}\"\n" +
            $"foil \"{name}\" {{\n" +
            $"  units mm\n" +
            $"  half_span {halfSpanMm.ToString("G17", CultureInfo.InvariantCulture)} mm\n" +
            $"  evaluator \"cfdw-cv\" \"2\"\n" +
            $"  symmetry mirror_y\n" +
            $"  planform {{\n" +
            $"    leading cv {{ {le} }}\n" +
            $"    trailing cv {{ {te} }}\n" +
            $"  }}\n" +
            $"  dihedral cv {{ {flat} }}\n" +
            $"  twist cv {{ {flat} }}\n" +
            $"  thickness cv {{ {thick} }}\n" +
            $"  profiles {{\n" +
            $"    profile \"sym\" {{\n" +
            $"      upper cv {{ {upper} }}\n" +
            $"      lower cv {{ {lower} }}\n" +
            $"      closure {closure}\n" +
            $"    }}\n" +
            $"  }}\n" +
            $"  sections {{ at root profile \"sym\" at tip profile \"sym\" }}\n" +
            $"  {tipClause}\n" +
            $"}}\n";
        return Utf8.GetBytes(text);
    }

    /// <summary>Builds the §A3 worked-example wingfoil (span 915 mm, c_root 112.4 mm, c_tip 25 mm, R_le 25 mm, R_te 10 mm).</summary>
    private static byte[] BuildA3RoundedWing(string tipClause = "tip rounded 1", bool openTe = false, double sweepMm = 0.0)
    {
        // N = 5 clamped cubic knots [0,0,0,0, 0.7, 1,1,1,1], P2.eta = 0.85, dy = 457.5 * 0.15 = 68.625 mm
        // du1 = 0.30, du2 = 1.00 => dx_le = sqrt(0.2 * 25 * 68.625) = 18.52363355..., dx_te = sqrt(0.2 * 10 * 68.625) = 11.71537451...
        double dy = 457.5 * (1.0 - 0.85);
        double dxLe = Math.Sqrt(0.2 * 25.0 * dy);
        double dxTe = Math.Sqrt(0.2 * 10.0 * dy);
        double leTip = 42.0 + sweepMm;
        double teTip = 67.0 + sweepMm;
        double leMid = 18.0 + 0.55 * sweepMm;
        double teMid = 94.0 + 0.55 * sweepMm;
        string F(double v) => v.ToString("G17", CultureInfo.InvariantCulture);
        string le = $"degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, {F(leMid)}), (1, {F(leTip - dxLe)}), (1, {F(leTip)})] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents {{ \"cv-4\" vertical }}";
        string te = $"degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 112.4), (0.35, 112.4), (0.85, {F(teMid)}), (1, {F(teTip + dxTe)}), (1, {F(teTip)})] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents {{ \"cv-4\" vertical }}";
        return BuildFoil("4.2", 457.5, le, te, tipClause, openTe, openTeHalfGap: 0.01); // 0.02 * 25 mm = 0.5 mm TE gap at tip
    }

    private static void T01_ParsePrintRoundTripAndIdentity()
    {
        // 4.0 / 4.1 / 4.2 open tip produce byte-identical SurfaceHash
        string le6 = "degree 3 knots [0, 0, 0, 0, 0.33, 0.67, 1, 1, 1, 1] points [(0, 0), (0.15, 0), (0.35, 5), (0.65, 15), (0.85, 30), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        string te6 = "degree 3 knots [0, 0, 0, 0, 0.33, 0.67, 1, 1, 1, 1] points [(0, 112.4), (0.15, 112.4), (0.35, 105), (0.65, 92), (0.85, 80), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        var p40 = FoilSource.Parse(BuildFoil("4.0", 457.5, le6, te6, "tip open"));
        var p41 = FoilSource.Parse(BuildFoil("4.1", 457.5, le6, te6, "tip open"));
        var p42 = FoilSource.Parse(BuildFoil("4.2", 457.5, le6, te6, "tip open"));
        True(p40.IsParsed && p41.IsParsed && p42.IsParsed, "4.0/4.1/4.2 open-tip foils must parse");
        Equal(p40.SurfaceHash, p41.SurfaceHash);
        Equal(p41.SurfaceHash, p42.SurfaceHash);

        // AuthoredProjection exposes Tip and TipCapRatio
        var aOpen = p42.Authored();
        Equal("open", aOpen.Tip);
        Equal((double?)null, aOpen.TipCapRatio);

        // tip rounded (omitted) == tip rounded 1.0 identical SurfaceHash
        byte[] roundedOmitted = BuildA3RoundedWing("tip rounded");
        byte[] roundedExplicit = BuildA3RoundedWing("tip rounded 1.0");
        byte[] roundedHalf = BuildA3RoundedWing("tip rounded 0.5");
        var prOmitted = FoilSource.Parse(roundedOmitted);
        var prExplicit = FoilSource.Parse(roundedExplicit);
        var prHalf = FoilSource.Parse(roundedHalf);
        True(prOmitted.IsParsed && prExplicit.IsParsed && prHalf.IsParsed, "4.2 rounded-tip foils must parse");
        Equal(prOmitted.SurfaceHash, prExplicit.SurfaceHash);
        True(prOmitted.SurfaceHash != prHalf.SurfaceHash, "distinct tip cap ratio must change SurfaceHash");
        True(prOmitted.SurfaceHash != FoilSource.Parse(BuildA3RoundedWing("tip open")).SurfaceHash, "rounded vs open tip must change SurfaceHash");

        var aRounded = prOmitted.Authored();
        Equal("rounded", aRounded.Tip);
        Equal((double?)1.0, aRounded.TipCapRatio);
        Equal((double?)0.5, prHalf.Authored().TipCapRatio);

        // Print round-trip emits explicit tip rounded <r> and preserves SurfaceHash
        byte[] printed = FoilSource.Print(prOmitted.Definition!);
        string printedText = Utf8.GetString(printed);
        True(printedText.Contains("tip rounded 1\n", StringComparison.Ordinal), "Print must emit explicit tip rounded 1");
        var prRoundTrip = FoilSource.Parse(printed);
        True(prRoundTrip.IsParsed, "printed 4.2 foil must re-parse");
        Equal(prOmitted.SurfaceHash, prRoundTrip.SurfaceHash);

        // EnsureHeader41 is non-downgrading no-op on 4.2 (Mutant kill: throwing DSL-VERSION on 4.2 turns RED)
        byte[] after41 = FoilSource.EnsureHeader41(roundedExplicit);
        True(after41.SequenceEqual(roundedExplicit), "EnsureHeader41 must be a no-op on 4.2");

        // EnsureHeader42 upgrades 4.0 and 4.1 to 4.2 and is a no-op on 4.2
        byte[] up40 = FoilSource.EnsureHeader42(BuildFoil("4.0", 457.5, le6, te6, "tip open"));
        byte[] up41 = FoilSource.EnsureHeader42(FoilSource.NewDefault());
        True(Utf8.GetString(up40).StartsWith("foildsl \"4.2\"", StringComparison.Ordinal), "4.0 upgrades to 4.2");
        True(Utf8.GetString(up41).StartsWith("foildsl \"4.2\"", StringComparison.Ordinal), "4.1 upgrades to 4.2");
        True(FoilSource.EnsureHeader42(roundedExplicit).SequenceEqual(roundedExplicit), "EnsureHeader42 is no-op on 4.2");

        // Geometry.Assess certifies tip rounded and updates ProofScope
        var assessment = Geometry.Assess(prExplicit);
        Equal(GeometryStatus.Certified, assessment.Status);
        True(assessment.Certificate!.ProofScope.Contains("tip-cap fairing", StringComparison.Ordinal), "rounded tip ProofScope must disclose cap fairing scope");
    }

    private static void T02_HostileTipAndTangentInputsRefused()
    {
        // 4.0 / 4.1 reject tip rounded -> DSL-SYNTAX
        string le6 = "degree 3 knots [0, 0, 0, 0, 0.33, 0.67, 1, 1, 1, 1] points [(0, 0), (0.15, 0), (0.35, 5), (0.65, 15), (0.85, 30), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        string te6 = "degree 3 knots [0, 0, 0, 0, 0.33, 0.67, 1, 1, 1, 1] points [(0, 112.4), (0.15, 112.4), (0.35, 105), (0.65, 92), (0.85, 80), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\"]";
        Equal("DSL-SYNTAX", FoilSource.Parse(BuildFoil("4.0", 457.5, le6, te6, "tip rounded 1")).Diagnostics[0].Code);
        Equal("DSL-SYNTAX", FoilSource.Parse(BuildFoil("4.1", 457.5, le6, te6, "tip rounded 1")).Diagnostics[0].Code);

        // 4.1 rejects rail vertical tangent -> DSL-SYNTAX (NOT DSL-LOCK)
        string leVert = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 24), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        Equal("DSL-SYNTAX", FoilSource.Parse(BuildFoil("4.1", 457.5, leVert, null, "tip open")).Diagnostics[0].Code);

        // 4.1 rejects repeated terminal abscissa eta_{N-2} = 1 on rail -> DSL-SYNTAX (NOT DSL-CURVE; Mutant kill!)
        string leRepeat = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 24), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"]";
        Equal("DSL-SYNTAX", FoilSource.Parse(BuildFoil("4.1", 457.5, leRepeat, null, "tip open")).Diagnostics[0].Code);

        // 4.2 rejects double repeated tip abscissa eta_{N-3} = eta_{N-2} = eta_{N-1} = 1 -> DSL-CURVE
        string leDoubleRepeat = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (1, 18), (1, 24), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"]";
        Equal("DSL-CURVE", FoilSource.Parse(BuildFoil("4.2", 457.5, leDoubleRepeat, null, "tip open")).Diagnostics[0].Code);

        // 4.2 rejects vertical tangent on interior CV -> DSL-LOCK
        string leInteriorVert = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 24), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-2\" vertical }";
        Equal("DSL-LOCK", FoilSource.Parse(BuildFoil("4.2", 457.5, leInteriorVert, null, "tip open")).Diagnostics[0].Code);

        // 4.2 rejects cusp (zero handle length) and inverted handle in Geometry.CheckTangentRows -> Invalid + DSL-LOCK
        string leCusp = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 42), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        var cuspAssess = Geometry.Assess(FoilSource.Parse(BuildFoil("4.2", 457.5, leCusp, null, "tip open")));
        Equal(GeometryStatus.Invalid, cuspAssess.Status);
        Equal("DSL-LOCK", cuspAssess.Code);

        string leInverted = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 50), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        var invAssess = Geometry.Assess(FoilSource.Parse(BuildFoil("4.2", 457.5, leInverted, null, "tip open")));
        Equal(GeometryStatus.Invalid, invAssess.Status);
        Equal("DSL-LOCK", invAssess.Code);

        string teInverted = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 112.4), (0.35, 112.4), (0.85, 94), (1, 60), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        var teInvAssess = Geometry.Assess(FoilSource.Parse(BuildFoil("4.2", 457.5, null, teInverted, "tip open")));
        Equal(GeometryStatus.Invalid, teInvAssess.Status);
        Equal("DSL-LOCK", teInvAssess.Code);

        // Ruling 15 numeric bounds for tip rounded <r>
        Equal("DSL-LEX", FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded NaN")).Diagnostics[0].Code);
        Equal("DSL-LEX", FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded 1e309")).Diagnostics[0].Code);
        Equal("DSL-UNIT", FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded 0.24")).Diagnostics[0].Code);
        Equal("DSL-UNIT", FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded 2.01")).Diagnostics[0].Code);

        // Boundary ratios 0.25 and 2.0 are accepted
        True(FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded 0.25")).IsParsed, "0.25 accepted");
        True(FoilSource.Parse(BuildFoil("4.2", 457.5, null, null, "tip rounded 2.0")).IsParsed, "2.0 accepted");
    }

    private static void T03_TipRadiusMatchesCoxDeBoor()
    {
        // N = 4, N = 5, and N = 7 (including triple interior knot at 0.4)
        var cases = new (string Label, string Leading, string Trailing)[]
        {
            ("N=4",
                "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.4, 0), (1, 20), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"] tangents { \"cv-3\" vertical }",
                "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 112.4), (0.4, 112.4), (1, 82), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"] tangents { \"cv-3\" vertical }"),
            ("N=5",
                "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 18), (1, 23.4763664473), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }",
                "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 112.4), (0.35, 112.4), (0.85, 94), (1, 78.7153745139), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }"),
            ("N=7-triple-knot",
                "degree 3 knots [0, 0, 0, 0, 0.4, 0.4, 0.4, 1, 1, 1, 1] points [(0, 0), (0.15, 0), (0.30, 6), (0.40, 12), (0.72, 22), (1, 25), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] tangents { \"cv-6\" vertical }",
                "degree 3 knots [0, 0, 0, 0, 0.4, 0.4, 0.4, 1, 1, 1, 1] points [(0, 112.4), (0.15, 112.4), (0.30, 106), (0.40, 100), (0.72, 86), (1, 80), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\", \"cv-5\", \"cv-6\"] tangents { \"cv-6\" vertical }")
        };

        foreach (var (label, le, te) in cases)
        {
            byte[] src = BuildFoil("4.2", 457.5, le, te, "tip open");
            var parsed = FoilSource.Parse(src);
            True(parsed.IsParsed, $"{label} must parse");
            var view = Planform.View(src, "Accepted", 0);

            foreach (var (curveView, curve, expectedAngle) in new[]
            {
                (view.Leading, parsed.Definition!.Curves["leading"], 90.0),
                (view.Trailing, parsed.Definition!.Curves["trailing"], -90.0)
            })
            {
                double formulaR = Planform.TipRadiusMeters(curveView);
                double jetR = Planform.CurvatureAt(curveView, 1.0, CurveSide.Before).RadiusMeters;
                double relErr = Math.Abs(formulaR - jetR) / jetR;
                True(relErr < 1e-9, $"{label} {curve.Path}: analytic R={formulaR} vs Cox-de Boor jet R={jetR} relErr={relErr}");
                Near(expectedAngle, ChannelEdits.TipAngleDegrees(curve, parsed.Definition.HalfSpan), 1e-12, $"{label} {curve.Path} TipAngleDegrees");
            }
        }
    }

    private static void T04_PositiveChordSweepAndCrossing()
    {
        // 25 deg swept vertical-tangent wing: halfSpan = 457.5 mm, tan(25 deg) * 457.5 = 213.3 mm >> root chord 112.4 mm
        // Global rail hulls overlap massively (leading(1) = 255.3 mm > trailing(0) = 112.4 mm).
        byte[] swept = BuildA3RoundedWing("tip rounded 1", openTe: false, sweepMm: 213.3);
        var parsed = FoilSource.Parse(swept);
        True(parsed.IsParsed, "25 deg swept vertical-tip wing must parse");
        Bernstein.LastPositiveChord = null;
        var assess = Geometry.Assess(parsed);
        Equal(GeometryStatus.Certified, assess.Status);
        True(Bernstein.LastPositiveChord is { FastPath: false, Outcome: "certified", Splits: > 0 and <= 64 },
            $"Expected BFS certification with small splits, got {Bernstein.LastPositiveChord}");

        // Interior crossing pair -> Invalid + ChordDimension maps to DSL-EDGES-CROSS
        string leCross = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 0), (0.35, 0), (0.85, 160), (1, 24), (1, 42)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        string teCross = "degree 3 knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1] points [(0, 112.4), (0.35, 112.4), (0.85, 80), (1, 78), (1, 67)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\", \"cv-4\"] tangents { \"cv-4\" vertical }";
        var crossAssess = Geometry.Assess(FoilSource.Parse(BuildFoil("4.2", 457.5, leCross, teCross, "tip open")));
        Equal(GeometryStatus.Invalid, crossAssess.Status);
        Equal("Continuous chord is nonpositive.", crossAssess.Reason);

        // Verify ChordDimension maps crossing -> DSL-EDGES-CROSS
        Refuses("DSL-EDGES-CROSS", () => ChordDimension.Evaluate(BuildFoil("4.2", 457.5, leCross, teCross, "tip open"), new DimensionCommand("tip-chord", "25 mm")));
    }

    private static void T04_HostileNearTouching16CvNotAssessed()
    {
        // Construct hostile 16-CV rails that remain strictly positive (gap ~ 1e-7 m along the entire span) with 25 deg sweep,
        // forcing Bernstein.CertifyPositiveChord BFS to hit maxNodes=2048 -> NotAssessed ("Positive-chord subdivision budget exceeded.").
        int n = 16;
        var knots = new List<double> { 0, 0, 0, 0 };
        for (int i = 1; i <= n - 4; i++) knots.Add(i / (double)(n - 3));
        knots.AddRange([1, 1, 1, 1]);
        string kStr = string.Join(", ", knots.Select(k => k.ToString("G17", CultureInfo.InvariantCulture)));
        var lePts = new List<string>();
        var tePts = new List<string>();
        var ids = new List<string>();
        for (int i = 0; i < n; i++)
        {
            double eta = i / (double)(n - 1);
            double xBase = i < 2 ? 0.0 : 200.0 * eta;
            double gap = 1e-4; // 0.1 um gap across all 16 CVs (satisfies root_mirror at i=0,1 while swept 200 mm)
            lePts.Add($"({eta.ToString("G17", CultureInfo.InvariantCulture)}, {xBase.ToString("G17", CultureInfo.InvariantCulture)})");
            tePts.Add($"({eta.ToString("G17", CultureInfo.InvariantCulture)}, {(xBase + gap).ToString("G17", CultureInfo.InvariantCulture)})");
            ids.Add($"\"cv-{i}\"");
        }
        string le = $"degree 3 knots [{kStr}] points [{string.Join(", ", lePts)}] ids [{string.Join(", ", ids)}]";
        string te = $"degree 3 knots [{kStr}] points [{string.Join(", ", tePts)}] ids [{string.Join(", ", ids)}]";
        byte[] hostile = BuildFoil("4.2", 457.5, le, te, "tip open");
        var parsed = FoilSource.Parse(hostile);
        True(parsed.IsParsed, "hostile 16-CV foil must parse");
        var assess = Geometry.Assess(parsed);
        Equal(GeometryStatus.NotAssessed, assess.Status);
        Equal("Positive-chord subdivision budget exceeded.", assess.Reason);

        // ChordDimension must classify budget exhaustion as DSL-NOT-ASSESSED (never misclassified as DSL-EDGES-CROSS!)
        Refuses("DSL-NOT-ASSESSED", () => ChordDimension.Evaluate(hostile, new DimensionCommand("tip-chord", "0.5 mm")));
    }

    private static void T05_EditingVerbsUndoAndLegacyTipChord()
    {
        // Start from a 4.1 swept wing with aft-swept TE (tests SetTipVertical inverted-handle rescue + 4.1 -> 4.2 upgrade + 1-step Undo)
        byte[] start41 = BuildFoil("4.1", 457.5,
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.33, 0), (0.67, 140), (1, 210)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]",
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 112.4), (0.33, 112.4), (0.67, 190), (1, 235)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]",
            "tip open");
        using var session = new AuthoringSession();
        session.Open(start41, Id(), acceptIdInsertion: true);

        // Enable vertical TE tip tangent on aft-swept TE (originally P_{N-2}.x = 190 < P_{N-1}.x = 235 => inverted!)
        var teVert = session.SetTipTangent(Id(), "trailing", true);
        True(teVert.Committed, $"SetTipTangent on aft-swept TE must commit: {teVert.Code} {teVert.Reason}");
        True(Utf8.GetString(session.Snapshot().Source).StartsWith("foildsl \"4.2\"", StringComparison.Ordinal), "must upgrade header to 4.2");

        // 1-step Undo restores byte-identical 4.1 source
        session.Undo(Id());
        True(session.Snapshot().Source.SequenceEqual(start41), "1-step Undo must restore byte-identical 4.1 source");
        session.Redo(Id());

        // Enable vertical LE tip tangent and set exact radii (25 mm LE, 10 mm TE)
        var leVert = session.SetTipTangent(Id(), "leading", true);
        True(leVert.Committed, $"SetTipTangent LE must commit: {leVert.Code}");
        var setLeR = session.SetTipRadius(Id(), "leading", 0.025);
        True(setLeR.Committed, $"SetTipRadius LE 25 mm must commit: {setLeR.Code} {setLeR.Reason}");
        var setTeR = session.SetTipRadius(Id(), "trailing", 0.010);
        True(setTeR.Committed, $"SetTipRadius TE 10 mm must commit: {setTeR.Code} {setTeR.Reason}");

        var view = Planform.View(session.Snapshot().Source, "Accepted", 0);
        Near(0.025, Planform.TipRadiusMeters(view.Leading), 1e-6, "LE tip radius");
        Near(0.010, Planform.TipRadiusMeters(view.Trailing), 1e-6, "TE tip radius");
        // PointModel sets TipEnd Vertical and TipHandle ValueOnly
        Equal(TangentKind.Vertical, view.Leading.Points[^1].Kind);
        Equal(PointFreedom.ValueOnly, view.Leading.Points[^2].Freedom);
        Equal(TangentKind.Vertical, view.Trailing.Points[^1].Kind);
        Equal(PointFreedom.ValueOnly, view.Trailing.Points[^2].Freedom);

        // Infeasible radius (500 mm on 112.4 mm root chord wing) -> DSL-TIP-RADIUS-BOUNDS with LimitMeters guaranteed admitted!
        var tooBig = session.SetTipRadius(Id(), "leading", 0.500);
        Equal(false, tooBig.Committed);
        Equal("DSL-TIP-RADIUS-BOUNDS", tooBig.Code);
        True(tooBig.LimitMeters is > 0.0 and < 0.500, "infeasible radius refusal must return positive LimitMeters < requested");
        var repaired = session.SetTipRadius(Id(), "leading", tooBig.LimitMeters!.Value);
        True(repaired.Committed, $"SetTipRadius at LimitMeters={tooBig.LimitMeters} must be guaranteed admitted: {repaired.Code} {repaired.Reason}");

        // Restore 25 mm LE and enable 3D rounded tip cap
        True(session.SetTipRadius(Id(), "leading", 0.025).Committed, "restore 25 mm LE");
        var capOn = session.SetTipCap(Id(), "rounded", 1.0);
        True(capOn.Committed, $"SetTipCap rounded must commit: {capOn.Code}");
        Equal("rounded", session.InspectAccepted().Authored.Tip);
        Equal((double?)1.0, session.InspectAccepted().Authored.TipCapRatio);

        // ChannelEdits.Add, Remove(count-3) preserving eta_{N-2}=1, Rebuild(4, 5, 7) preserving vertical tip
        var addRes = session.ApplyPointCommand(Id(), new PointCommand.AddPoint("leading", 0.50));
        Equal(6, addRes.PointsAfter);
        var viewAfterAdd = Planform.View(session.Snapshot().Source, "Accepted", 0);
        Equal(1.0, viewAfterAdd.Leading.Points[^2].Eta);
        // Remove index count-3 (right == nextCount-2 hits vertical TipHandle!)
        string removeId = viewAfterAdd.Leading.Points[^3].Id;
        var remRes = session.ApplyPointCommand(Id(), new PointCommand.RemovePoint("leading", removeId));
        Equal(5, remRes.PointsAfter);
        var viewAfterRem = Planform.View(session.Snapshot().Source, "Accepted", 0);
        Equal(1.0, viewAfterRem.Leading.Points[^2].Eta);
        Equal(TangentKind.Vertical, viewAfterRem.Leading.Points[^1].Kind);

        foreach (int targetCount in new[] { 4, 5, 7 })
        {
            var defBefore = FoilSource.Parse(session.Snapshot().Source).Definition!;
            var rebuilt = ChannelEdits.Rebuild(defBefore.Curves["leading"], targetCount, rootMirror: true);
            Equal(targetCount, rebuilt.Curve.Points.Length);
            Equal(1.0, rebuilt.Curve.Points[^2][0]);
            True(rebuilt.Curve.Tangents.Any(t => t.Id == rebuilt.Curve.Ids[^1] && t.Kind == "vertical"), $"Rebuild({targetCount}) preserves vertical tip tangent");
        }

        // ChordDimension tip-chord edit on tip rounded + vertical tip rides TipHandle & preserves R_tip
        double rLeBefore = Planform.TipRadiusMeters(Planform.View(session.Snapshot().Source, "Accepted", 0).Leading);
        var (_, patchedDim) = ChordDimension.Evaluate(session.Snapshot().Source, new DimensionCommand("tip-chord", "30 mm"));
        var dimView = Planform.View(patchedDim, "Accepted", 0);
        Near(0.030, dimView.Trailing.Points[^1].Ordinate - dimView.Leading.Points[^1].Ordinate, 1e-6, "tip chord updated to 30 mm");
        Near(rLeBefore, Planform.TipRadiusMeters(dimView.Leading), 1e-5, "ChordDimension tip-chord preserves LE tip radius");

        // Save/reopen round-trip with tip-tangent, tip-radius, tip-cap receipts
        byte[] saved = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(saved);
        Equal("rounded", reopened.InspectAccepted().Authored.Tip);

        // Legacy below-floor wing (c_tip = 4.0 mm < 5.0 mm floor): non-worsening tip edits admitted via TipChord.Admits
        byte[] legacySmallTip = BuildFoil("4.1", 457.5,
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.33, 0), (0.67, 25), (1, 50)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]",
            "degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 112.4), (0.33, 112.4), (0.67, 85), (1, 54)] ids [\"cv-0\", \"cv-1\", \"cv-2\", \"cv-3\"]",
            "tip open");
        using var legacy = new AuthoringSession();
        legacy.Open(legacySmallTip, Id(), acceptIdInsertion: true);
        True(legacy.SetTipCap(Id(), "rounded", 1.0).Committed, "legacy below-floor wing allows non-worsening tip rounded");
        True(legacy.SetTipTangent(Id(), "leading", true).Committed, "legacy below-floor wing allows non-worsening vertical tip");
        // Worsening tip chord via drag is held at 4.0 mm; direct Commit below 4.0 mm fails DSL-TIP-CHORD-MIN
        var legacyView = Planform.View(legacy.Snapshot().Source, "Accepted", 0);
        var dId = Id();
        var g = legacy.BeginPointGesture(dId, "trailing", legacyView.Trailing.Points[^1].Id);
        var heldFrame = legacy.UpdatePointGesture(dId, g.Generation, legacyView.Trailing.Points[^1].SpanMeters, legacyView.Trailing.Points[^1].Ordinate - 0.002);
        True(heldFrame.Limit is { Kind: GestureLimitKind.TipAlreadyUnder }, "legacy below-floor tip drag must hold at TipAlreadyUnder");
        legacy.Cancel(dId);
    }

    private static void T05_ForgedTipEditReceiptRefused()
    {
        using var session = new AuthoringSession();
        session.Open(BuildA3RoundedWing("tip open"), Id(), acceptIdInsertion: true);
        True(session.SetTipCap(Id(), "rounded", 1.0).Committed, "valid tip-cap commit");
        var env1 = session.Envelope();
        var last1 = env1.Accepted[^1];
        Equal("tip-cap", last1.Edit!.Rail);
        var forgedAcc1 = env1.Accepted.ToArray();
        forgedAcc1[^1] = last1 with { Edit = last1.Edit with { Curve = "leading" } };
        byte[] forgedCap = NativeProject.Encode(env1 with { Accepted = forgedAcc1 });
        Refuses("DOC-REFERENCE", () =>
        {
            using var s = new AuthoringSession();
            s.Reopen(forgedCap);
        });

        // Forge 2: tip-radius with non-null Rule -> DOC-REFERENCE
        using var s2 = new AuthoringSession();
        s2.Open(BuildA3RoundedWing("tip open"), Id(), acceptIdInsertion: true);
        True(s2.SetTipRadius(Id(), "leading", 0.020).Committed, "valid tip-radius commit");
        var env2 = s2.Envelope();
        var last2 = env2.Accepted[^1];
        Equal("tip-radius", last2.Edit!.Rail);
        var forgedAcc2 = env2.Accepted.ToArray();
        forgedAcc2[^1] = last2 with { Edit = last2.Edit with { Rule = "vertical" } };
        byte[] forgedRadius = NativeProject.Encode(env2 with { Accepted = forgedAcc2 });
        Refuses("DOC-REFERENCE", () =>
        {
            using var s = new AuthoringSession();
            s.Reopen(forgedRadius);
        });
    }

    private static void T05_TelemetryZeroPii()
    {
        const string secretFoilName = "SecretUserWing_C_Users_Alice";
        using var session = new AuthoringSession();
        session.Open(BuildFoil("4.2", 457.5, null, null, "tip open", name: secretFoilName), Id(), acceptIdInsertion: true);
        True(session.SetTipTangent(Id(), "leading", true).Committed, "set vertical");
        True(session.SetTipRadius(Id(), "leading", 0.020).Committed, "set radius");
        True(session.SetTipCap(Id(), "rounded", 1.0).Committed, "set cap");
        session.RecordTipCapExport(new("step", "rounded", false, 12, 0.003, 12.5));

        var events = session.ReadLocalEvents();
        True(events.Any(e => e.Operation == "tip.edit" && e.TipEdit is { Kind: "tangent", Rail: "leading", Outcome: "ok" }), "tip.edit tangent recorded");
        True(events.Any(e => e.Operation == "tip.edit" && e.TipEdit is { Kind: "radius", Rail: "leading", Outcome: "ok" }), "tip.edit radius recorded");
        True(events.Any(e => e.Operation == "tip.edit" && e.TipEdit is { Kind: "cap", Rail: null, Outcome: "ok" }), "tip.edit cap recorded");
        True(events.Any(e => e.Operation == "geometry.positive_chord" && e.PositiveChord is not null), "geometry.positive_chord recorded");
        True(events.Any(e => e.Operation == "export.tipcap" && e.TipCapExport is { Format: "step", TipKind: "rounded" }), "export.tipcap recorded");

        string serialized = JsonSerializer.Serialize(events);
        True(!serialized.Contains(secretFoilName, StringComparison.OrdinalIgnoreCase), "telemetry must never contain foil name");
        True(!serialized.Contains("cv-4", StringComparison.OrdinalIgnoreCase), "telemetry must never contain vertex IDs");
        True(!serialized.Contains("Alice", StringComparison.OrdinalIgnoreCase), "telemetry must never contain user identifiers");
    }

    private static void T06_WingEstimatesMacAndOverallSpan()
    {
        byte[] wing = BuildA3RoundedWing("tip rounded 1", openTe: false);
        var est = WingEstimates.From(wing, "accepted", 0);
        True(est.Converged, "WingEstimates must converge on vertical-tangent rounded tip (s^2 substitution)");

        // Reference 2048-point Gauss/composite midpoint integral in s = sqrt(1 - eta)
        const int steps = 2048;
        double refAreaInt = 0.0, refMacInt = 0.0;
        for (int i = 0; i < steps; i++)
        {
            double s = (i + 0.5) / steps;
            double eta = 1.0 - s * s;
            double c = WingEstimates.ChordMeters(wing, eta);
            double weight = 2.0 * s / steps;
            refAreaInt += c * weight;
            refMacInt += c * c * weight;
        }
        double refArea = est.SpanMeters * refAreaInt;
        double refMac = est.SpanMeters * refMacInt / refArea;
        True(Math.Abs(est.AreaSquareMeters - refArea) / refArea < 1e-6, $"Area mismatch: {est.AreaSquareMeters} vs {refArea}");
        True(Math.Abs(est.MacMeters - refMac) / refMac < 1e-6, $"MAC mismatch: {est.MacMeters} vs {refMac}");

        // Analytic OverallSpanMeters == 2 * (halfSpan + 0.5 * ratio * c_tip * tc_tip)
        // A3 wing: halfSpan = 0.4575 m, c_tip = 0.025 m, tc_tip = 0.092, ratio = 1.0 => 2 * (0.4575 + 0.00115) = 0.9173 m
        Near(0.915, est.SpanMeters, 1e-9, "Projected SpanMeters");
        Near(0.9173, est.OverallSpanMeters, 1e-9, "Analytic OverallSpanMeters");

        // Agrees with STL and STEP SizeYMm within < 0.01 mm
        var stl = StlExport.Build(wing, "a3-rounded", 1, StlScope.Whole, StlExport.PrintMm);
        Near(est.OverallSpanMeters * 1000.0, stl.SizeYMm, 0.01, "STL SizeYMm vs OverallSpanMeters");
        var step = StepAp203Export.Build(wing, "a3-rounded", 1, StlScope.Whole, StepVariant.ClosedShell);
        Near(est.OverallSpanMeters * 1000.0, step.SizeYMm, 0.01, "STEP SizeYMm vs OverallSpanMeters");
    }

    private static void T07_WatertightExportsClosedAndOpenTe()
    {
        foreach (bool openTe in new[] { false, true })
        {
            string teTag = openTe ? "open-TE" : "closed-TE";
            byte[] wing = BuildA3RoundedWing("tip rounded 1", openTe: openTe);
            var est = WingEstimates.From(wing, "accepted", 0);
            double expectedWholeSpanMm = est.OverallSpanMeters * 1000.0;

            // CadExportExtractor chordal-v skinning <= 0.010 mm without runaway refinement, and HalfSizeYMm matches OverallSpanMeters / 2
            var skin = CadExportExtractor.BuildSkin(wing);
            True(skin.Deviation.MaxClosestPointMm <= CadExportExtractor.AcceptanceFloorMm,
                $"{teTag}: chordal-v skin deviation {skin.Deviation.MaxClosestPointMm:F5} mm exceeds 0.010 mm floor");
            True(skin.RefinementRounds <= 1 && skin.SectionCount <= 65,
                $"{teTag}: chordal-v skin should converge cleanly (observed rounds={skin.RefinementRounds}, sections={skin.SectionCount})");
            Near(expectedWholeSpanMm * 0.5, skin.HalfSizeYMm, 0.01, $"{teTag} WingCadSkin.HalfSizeYMm");
            Equal("rounded", skin.TipMode);
            True(skin.StarboardTipCap is not null, $"{teTag}: StarboardTipCap must be built");
            Equal(openTe, skin.StarboardTipCapTeEnd is not null);

            foreach (StlScope scope in new[] { StlScope.Whole, StlScope.Half })
            {
                double expectedSpanMm = scope == StlScope.Whole ? expectedWholeSpanMm : expectedWholeSpanMm * 0.5;
                string tag = $"{teTag}/{scope}";

                // STL Print 0.02 mm
                var stl = StlExport.Build(wing, "a3-rounded", 1, scope, StlExport.PrintMm);
                var stlCheck = StlExport.Check(stl.Bytes);
                True(stlCheck.Closed, $"{tag} STL must be closed (unpaired={stlCheck.UnpairedEdges}, zeroArea={stlCheck.ZeroAreaTriangles})");
                Equal(0, stlCheck.UnpairedEdges);
                Equal(0, stlCheck.ZeroAreaTriangles);
                Equal(2, stlCheck.Euler);
                True(stlCheck.VolumeMm3 > 0, $"{tag} STL volume must be positive");
                True(stl.DeviationMm <= StlExport.PrintMm, $"{tag} STL deviation {stl.DeviationMm} > 0.02 mm");
                Near(expectedSpanMm, stl.SizeYMm, 0.01, $"{tag} STL SizeYMm");

                // 3MF Print 0.02 mm
                var tmf = ThreeMfExport.Build(wing, "a3-rounded", 1, scope, StlExport.PrintMm);
                var tmfCheck = ThreeMfExport.Check(tmf.Bytes);
                True(tmfCheck.Closed, $"{tag} 3MF must be closed");
                Equal(0, tmfCheck.UnpairedEdges);
                Equal(0, tmfCheck.ZeroAreaTriangles);
                Equal(2, tmfCheck.Euler);
                Near(expectedSpanMm, tmf.SizeYMm, 0.01, $"{tag} 3MF SizeYMm");

                // STEP AP203 ClosedShell + Untrimmed
                var stepClosed = StepAp203Export.Build(wing, "a3-rounded", 1, scope, StepVariant.ClosedShell);
                True(stepClosed.Verification.Passed, $"{tag} STEP ClosedShell failed: {string.Join("; ", stepClosed.Verification.Errors)}");
                Equal(2, stepClosed.Verification.EulerCharacteristic);
                True(stepClosed.Verification.EveryEdgePairedOpposite, $"{tag} STEP every edge (+1,-1)");
                int expectedFaces = (openTe, scope) switch
                {
                    (false, StlScope.Half) => 4,
                    (false, StlScope.Whole) => 6,
                    (true, StlScope.Half) => 6,
                    (true, StlScope.Whole) => 10,
                    _ => 0
                };
                Equal(expectedFaces, stepClosed.Verification.FaceCount);
                Near(expectedSpanMm, stepClosed.SizeYMm, 0.01, $"{tag} STEP SizeYMm");

                var stepUntrimmed = StepAp203Export.Build(wing, "a3-rounded", 1, scope, StepVariant.Untrimmed);
                True(stepUntrimmed.Verification.Passed, $"{tag} STEP Untrimmed failed: {string.Join("; ", stepUntrimmed.Verification.Errors)}");
                True(stepUntrimmed.Verification.BoundaryEdgeCount > 0, $"{tag} STEP Untrimmed must have open boundary edges");

                // Mandatory live Rhino3dm 8.35.0 gate (every NurbsSurface.IsValid == true)
                var dm = Rhino3dmExport.Build(wing, "a3-rounded", 1, scope);
                True(dm.Verification.Passed, $"{tag} Rhino3dm 8.35.0 failed: {string.Join("; ", dm.Verification.Errors)}");
                Equal(0, dm.Verification.Errors.Count);
                int expectedDmSurfaces = (openTe, scope) switch
                {
                    (false, StlScope.Half) => 3,  // Upper, Lower, Tip Cap
                    (false, StlScope.Whole) => 6,
                    (true, StlScope.Half) => 5,   // Upper, Lower, TE Strip, Tip Cap, Tip Cap TE End
                    (true, StlScope.Whole) => 10,
                    _ => 0
                };
                Equal(expectedDmSurfaces, dm.Verification.SurfaceCount);
                Near(expectedSpanMm, dm.SizeYMm, 0.01, $"{tag} 3DM SizeYMm");
            }
        }
    }

    private static void T07_StepExportZeroPiiHeaderVerified()
    {
        byte[] wing = BuildA3RoundedWing("tip rounded 1.25", openTe: true);
        string syntheticUserPath = string.Concat("C:", "\\Us", "ers\\Alice");
        var step = StepAp203Export.Build(wing, syntheticUserPath + "\\MySecretFoil", 3, StlScope.Whole, StepVariant.ClosedShell);
        True(step.Verification.Passed && step.Verification.ZeroPiiHeaderValid, "STEP rounded-tip header must pass zero-PII verification");
        string text = Utf8.GetString(step.Bytes);
        True(text.Contains(StepAp203Export.FixedZeroPiiTimestamp, StringComparison.Ordinal), "STEP header must carry fixed zero-PII timestamp");
        True(!text.Contains(syntheticUserPath, StringComparison.OrdinalIgnoreCase), "STEP header must sanitize user paths");
    }
}
