using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;

namespace CadExportSpike;

internal static class Program
{
    private const double AcceptanceFloorMm = 0.010; // 10 um (ADR-0013 / A4.12)
    private const double TargetSkinMm = 0.004;      // 4 um internal adaptive target (2.5x safety margin)
    private const int ChordPointsPerSide = 65;      // cosine chord grid for cubic B-spline skinning
    private const int MaxRefinementRounds = 4;      // termination variant R <= 4
    private const int MaxSections = 257;            // termination variant N <= 257

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex KnotsBlockRx = new(@"knots\s*\[([^\]]+)\]", RegexOptions.Compiled);

    private sealed record BenchmarkWing(string Id, string Label, string FoilName, byte[] Source, bool ExpectedOpenTe, string OriginNote);

    public static int Main(string[] args)
    {
        string repoRoot = FindRepoRoot();
        string outDir = args.Length >= 2 && args[0] == "--out-dir"
            ? args[1]
            : Path.Combine(repoRoot, "docs", "proof", "spike-cad-01");
        Directory.CreateDirectory(outDir);

        var wings = BuildBenchmarkWings(repoRoot);
        var deviationRows = new List<object>();
        var stepRows = new List<object>();

        Console.WriteLine($"Running SPIKE-CAD-01 Option B Pure-C# NURBS Skinner + STEP AP203 Benchmark ({wings.Count} wings x 2 scopes x 2 variants)...");

        bool allWingsPassed = true;
        double worstDeviationUm = 0.0;

        foreach (var wing in wings)
        {
            var parsed = FoilSource.Parse(wing.Source);
            if (!parsed.IsParsed)
                throw new InvalidOperationException($"Wing '{wing.Id}' failed to parse: {parsed.Diagnostics[0].Code}");
            var assess = Geometry.Assess(parsed);
            if (assess.Status != GeometryStatus.Certified)
                throw new InvalidOperationException($"Wing '{wing.Id}' is not Certified: {assess.Status} ({assess.Code}: {assess.Reason})");

            // Reference STL check at Fine tolerance for volume & TE sanity comparison
            var stlHalf = StlExport.Build(wing.Source, wing.FoilName, 1, StlScope.Half, StlExport.FineMm);
            var stlWhole = StlExport.Build(wing.Source, wing.FoilName, 1, StlScope.Whole, StlExport.FineMm);

            // Skin starboard wing adaptively starting from {rail knots U authored stations}
            var swSkin = Stopwatch.StartNew();
            var skin = BuildAdaptiveSkin(wing.Source);
            swSkin.Stop();

            // Verify A-vs-B closest-point skin deviation against Placement.Surface (51 x 201 + rail knots + tip)
            var swDev = Stopwatch.StartNew();
            var dev = MeasureDeviationAgainstPlacementSurface(wing.Source, skin, stations: 51, chordSamples: 201);
            swDev.Stop();

            double maxDevUm = dev.MaxClosestPointMm * 1000.0;
            double maxParamUm = dev.MaxParametricMm * 1000.0;
            double tipPlanarUm = dev.TipPlanarityErrorMm * 1000.0;
            double rootPlanarUm = dev.RootPlanarityErrorMm * 1000.0;
            double teDiffUm = Math.Abs(skin.TrailingEdgeMinMm - stlHalf.TrailingEdgeMm) * 1000.0;
            bool wingDevPass = dev.MaxClosestPointMm <= AcceptanceFloorMm
                && dev.TipPlanarityErrorMm <= 0.001
                && dev.RootPlanarityErrorMm <= 0.001
                && teDiffUm <= 1.0
                && skin.IsOpenTrailingEdge == wing.ExpectedOpenTe;

            worstDeviationUm = Math.Max(worstDeviationUm, maxDevUm);
            if (!wingDevPass) allWingsPassed = false;

            deviationRows.Add(new
            {
                wingId = wing.Id,
                label = wing.Label,
                origin = wing.OriginNote,
                geometryStatus = assess.Status.ToString(),
                expectedOpenTe = wing.ExpectedOpenTe,
                detectedOpenTe = skin.IsOpenTrailingEdge,
                halfSpanMm = Math.Round(skin.HalfSpanMm, 4),
                surfaceDegreeU = skin.UpperStarboard.DegreeU,
                surfaceDegreeV = skin.UpperStarboard.DegreeV,
                sectionCount = skin.SectionCount,
                chordControlPoints = skin.ChordCount,
                refinementRounds = skin.RefinementRounds,
                skinBuildMs = Math.Round(swSkin.Elapsed.TotalMilliseconds, 2),
                verifySampleCount = dev.SampleCount,
                verifyMs = Math.Round(swDev.Elapsed.TotalMilliseconds, 2),
                maxClosestPointDeviationMm = Math.Round(dev.MaxClosestPointMm, 7),
                maxClosestPointDeviationUm = Math.Round(maxDevUm, 4),
                maxParametricDeviationUm = Math.Round(maxParamUm, 4),
                worstSampleLocation = new
                {
                    side = dev.WorstSide,
                    eta = Math.Round(dev.WorstEta, 5),
                    chordFraction = Math.Round(dev.WorstChordX, 5)
                },
                tipPlanarityErrorUm = Math.Round(tipPlanarUm, 6),
                rootPlanarityErrorUm = Math.Round(rootPlanarUm, 6),
                trailingEdgeMinMm = Math.Round(skin.TrailingEdgeMinMm, 5),
                stlReferenceTrailingEdgeMinMm = Math.Round(stlHalf.TrailingEdgeMm, 5),
                trailingEdgeAgreementUm = Math.Round(teDiffUm, 4),
                stlFineHalfVolumeMm3 = Math.Round(stlHalf.VolumeMm3, 2),
                stlFineWholeVolumeMm3 = Math.Round(stlWhole.VolumeMm3, 2),
                acceptanceFloorUm = 10.0,
                passed = wingDevPass
            });

            Console.WriteLine(
                $"  [{wing.Id}] N={skin.SectionCount} (R={skin.RefinementRounds}) M={skin.ChordCount} deg=({skin.UpperStarboard.DegreeU},{skin.UpperStarboard.DegreeV}) " +
                $"maxDev={maxDevUm:F3} um (<=10 um: {wingDevPass}) TE={skin.TrailingEdgeMinMm:F4} mm skinMs={swSkin.Elapsed.TotalMilliseconds:F1}");

            // Generate and verify all 4 combinations: (Whole, Half) x (ClosedShell, Untrimmed)
            foreach (var scope in new[] { StlScope.Whole, StlScope.Half })
            {
                foreach (var variant in new[] { StepVariant.ClosedShell, StepVariant.Untrimmed })
                {
                    var swWrite = Stopwatch.StartNew();
                    string stepText = StepAp203Writer.Write(skin, wing.FoilName, 1, scope, variant);
                    swWrite.Stop();

                    var swVer = Stopwatch.StartNew();
                    var ver = StepVerifier.Verify(stepText, variant);
                    swVer.Stop();

                    // Also verify byte-determinism: second write must be bit-identical
                    string stepText2 = StepAp203Writer.Write(skin, wing.FoilName, 1, scope, variant);
                    bool byteDeterministic = string.Equals(stepText, stepText2, StringComparison.Ordinal);

                    bool stepPass = ver.Passed && byteDeterministic;
                    if (!stepPass) allWingsPassed = false;

                    stepRows.Add(new
                    {
                        wingId = wing.Id,
                        scope = scope.ToString().ToLowerInvariant(),
                        variant = variant == StepVariant.ClosedShell ? "closed-shell" : "untrimmed",
                        bytes = Encoding.UTF8.GetByteCount(stepText),
                        writeMs = Math.Round(swWrite.Elapsed.TotalMilliseconds, 2),
                        verifyMs = Math.Round(swVer.Elapsed.TotalMilliseconds, 2),
                        byteDeterministic,
                        entityCount = ver.EntityCount,
                        bsplineSurfaceCount = ver.BSplineSurfaceCount,
                        bsplineCurveCount = ver.BSplineCurveCount,
                        structureValid = ver.StructureValid,
                        ap203SchemaValid = ver.Ap203SchemaValid,
                        millimetreUnitValid = ver.MillimetreUnitValid,
                        knotWhereRulesValid = ver.KnotWhereRulesValid,
                        zeroPiiHeaderValid = ver.ZeroPiiHeaderValid,
                        faceCount = ver.FaceCount,
                        edgeCount = ver.EdgeCount,
                        vertexCount = ver.VertexCount,
                        eulerCharacteristic = ver.EulerCharacteristic,
                        everyEdgePairedOpposite = ver.EveryEdgePairedOpposite,
                        allEdgeLoopsClosed = ver.AllEdgeLoopsClosed,
                        maxVertexEdgeEndpointMismatchMm = ver.MaxVertexEdgeEndpointMismatchMm,
                        brepCheckAnalyzerReadBack = "Flagged / deferred to SPIKE-CAD-02 (no local OCCT/FreeCAD STEP reader installed on this win-arm64 host)",
                        passed = stepPass,
                        errors = ver.Errors
                    });
                }
            }
        }

        // Mandatory Red-First Negative Controls (Ruling 270 / C5)
        var negControls = RunNegativeControls(wings);
        bool negPassed = negControls.TwistOmissionDetectedRed && negControls.PiiHostnameDetectedRed && negControls.PiiTimestampDetectedRed;

        var deviationDoc = new
        {
            spike = "SPIKE-CAD-01",
            part = "Part 2 - S1-DEV-4WINGS + ADR-0010 Twisted/Blended Wing Deviation Matrix",
            acceptanceFloorUm = 10.0,
            allWingsPassed,
            worstClosestPointDeviationUm = Math.Round(worstDeviationUm, 4),
            wings = deviationRows
        };

        var pureStepDoc = new
        {
            spike = "SPIKE-CAD-01",
            part = "Part 2 - S1-PURE-STEP Pure-C# Option B STEP AP203 Results",
            allPassed = allWingsPassed,
            totalFilesChecked = stepRows.Count,
            occtBrepCheckAnalyzerStatus = "Flagged / deferred to SPIKE-CAD-02 (no local OCCT or FreeCAD STEP reader installed on this Windows ARM64 host)",
            fixedHeaderTimestamp = StepAp203Writer.FixedZeroPiiTimestamp,
            exports = stepRows
        };

        File.WriteAllText(Path.Combine(outDir, "deviation-matrix.json"), JsonSerializer.Serialize(deviationDoc, JsonOptions) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "pure-step-results.json"), JsonSerializer.Serialize(pureStepDoc, JsonOptions) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "negative-controls.json"), JsonSerializer.Serialize(negControls, JsonOptions) + "\n", new UTF8Encoding(false));

        Console.WriteLine($"Option B summary: allWingsPassed={allWingsPassed}, worstMaxDev={worstDeviationUm:F3} um (floor 10.0 um), negativeControlsAllRed={negPassed}");
        return (allWingsPassed && negPassed) ? 0 : 1;
    }

    private sealed record NegativeControlsReport(
        string Spike,
        string Part,
        bool TwistOmissionDetectedRed,
        double TwistOmissionMeasuredDeviationUm,
        double TwistOmissionBaselineDeviationUm,
        bool PiiHostnameDetectedRed,
        List<string> PiiHostnameErrors,
        bool PiiTimestampDetectedRed,
        List<string> PiiTimestampErrors,
        bool AllNegativeControlsProvedRed);

    private static NegativeControlsReport RunNegativeControls(List<BenchmarkWing> wings)
    {
        // Control A: Planted twist omission on ADR-0010 twisted/blended wing (-8 deg twist omitted -> 0 deg twist)
        var twistedWing = wings.First(w => w.Id == "adr0010-blended-twisted");
        string twistedText = Encoding.UTF8.GetString(twistedWing.Source);
        string untwistedText = Regex.Replace(
            twistedText,
            @"twist cv \{[^\}]+\}",
            "twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }");
        byte[] untwistedSource = Encoding.UTF8.GetBytes(untwistedText);

        var baselineSkin = BuildAdaptiveSkin(twistedWing.Source);
        double baselineUm = MeasureDeviationAgainstPlacementSurface(twistedWing.Source, baselineSkin, 51, 201).MaxClosestPointMm * 1000.0;

        // Build skin from untwisted source, then verify it against the TRUE twisted Placement.Surface!
        var mutantSkin = BuildAdaptiveSkin(untwistedSource);
        double mutantUm = MeasureDeviationAgainstPlacementSurface(twistedWing.Source, mutantSkin, 51, 201).MaxClosestPointMm * 1000.0;
        bool twistRed = mutantUm > 10.0;

        // Control B1: Planted PII hostname marker in STEP header author field (synthetic dummy marker, never a real host)
        string plantedHostMarker = "SYNTH-PII-MARKER-9X";
        string piiHostStep = StepAp203Writer.Write(
            baselineSkin, twistedWing.FoilName, 1, StlScope.Half, StepVariant.ClosedShell,
            new StepHeaderOverrides(Author: plantedHostMarker));
        var verHost = StepVerifier.Verify(piiHostStep, StepVariant.ClosedShell, [plantedHostMarker]);
        bool piiHostRed = !verHost.Passed && !verHost.ZeroPiiHeaderValid;

        // Control B2: Planted non-1980 timestamp in STEP header
        string piiTsStep = StepAp203Writer.Write(
            baselineSkin, twistedWing.FoilName, 1, StlScope.Half, StepVariant.ClosedShell,
            new StepHeaderOverrides(Timestamp: "2026-11-22T03:00:00"));
        var verTs = StepVerifier.Verify(piiTsStep, StepVariant.ClosedShell);
        bool piiTsRed = !verTs.Passed && !verTs.ZeroPiiHeaderValid;

        Console.WriteLine($"  [NEG-1 Twist omission] baseline={baselineUm:F3} um -> mutant={mutantUm:F1} um (RED={twistRed})");
        Console.WriteLine($"  [NEG-2a Planted PII author/host] RED={piiHostRed} ({string.Join("; ", verHost.Errors)})");
        Console.WriteLine($"  [NEG-2b Planted non-1980 timestamp] RED={piiTsRed} ({string.Join("; ", verTs.Errors)})");

        return new NegativeControlsReport(
            "SPIKE-CAD-01",
            "Mandatory Red-First Negative Controls (Ruling 270 / C5)",
            twistRed,
            Math.Round(mutantUm, 2),
            Math.Round(baselineUm, 4),
            piiHostRed,
            verHost.Errors,
            piiTsRed,
            verTs.Errors,
            twistRed && piiHostRed && piiTsRed);
    }

    private static List<BenchmarkWing> BuildBenchmarkWings(string repoRoot)
    {
        string exampleText = File.ReadAllText(Path.Combine(repoRoot, "src", "CfdWorkbench.Desktop", "Assets", "example.foil"));
        string open026Text = ApplyOpenTe(exampleText, 0.001);
        string open036Text = ApplyOpenTe(exampleText, 0.0014);

        string blendedText = File.ReadAllText(Path.Combine(repoRoot, "tests", "CfdWorkbench.Core.Tests", "Fixtures", "m12b2", "blended-dihedral.foil"));
        // Raise twist to -8 deg at tip per ADR-0010 / cad-export-kernel.md §5.1 (+60 mm dihedral, -8 deg twist, 2 blended profiles)
        string adr0010TwistedBlended = blendedText.Replace(
            "points [(0, 0), (0.1, 0), (0.3, -0.25), (0.5, -0.5), (0.7, -1), (0.9, -1.5), (1, -2)]",
            "points [(0, 0), (0.1, 0), (0.3, -1), (0.5, -2), (0.7, -4), (0.9, -6), (1, -8)]",
            StringComparison.Ordinal);
        byte[] adr0010Bytes = FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(adr0010TwistedBlended)));

        return
        [
            new("example-open-026", "Example foil (open TE 0.26 mm)", "Basic foil open 0.26", Encoding.UTF8.GetBytes(open026Text), true, "docs/design/export.md §3.3 (example.foil + teHalf=0.001)"),
            new("example-open-036", "Example foil (open TE 0.36 mm)", "Basic foil open 0.36", Encoding.UTF8.GetBytes(open036Text), true, "docs/design/export.md §3.3 (example.foil + teHalf=0.0014)"),
            new("example-closed", "Example foil as shipped (closed TE 0.00 mm)", "Basic foil", Encoding.UTF8.GetBytes(exampleText), false, "docs/design/export.md §3.3 (example.foil shipped)"),
            new("untitled-naca0012-closed", "Untitled NACA 0012 wing (closed TE 0.00 mm)", "Untitled NACA 0012", FoilSource.NewDefault(), false, "docs/design/export.md §3.3 (FoilSource.NewDefault)"),
            new("adr0010-blended-twisted", "ADR-0010 twisted/blended wing (+60 mm dihedral, -8 deg twist)", "ADR0010 Blended Twisted", adr0010Bytes, false, "ADR-0010 / cad-export-kernel.md §5.1 (blended-dihedral.foil @ -8 deg twist)")
        ];
    }

    private static string ApplyOpenTe(string text, double teHalf)
    {
        string th = teHalf.ToString(CultureInfo.InvariantCulture);
        string nth = (-teHalf).ToString(CultureInfo.InvariantCulture);
        text = text.Replace("(0.9, 0.01), (1, 0)] ids", $"(0.9, 0.01), (1, {th})] ids", StringComparison.Ordinal);
        text = text.Replace("(0.9, -0.01), (1, 0)] ids", $"(0.9, -0.01), (1, {nth})] ids", StringComparison.Ordinal);
        text = text.Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal);
        text = text.Replace("\"cv-6\", \"cv-7\"] }\r\n    }", "\"cv-6\", \"cv-7\"] }\r\n      closure open\r\n    }", StringComparison.Ordinal);
        return text;
    }

    /// <summary>
    /// Extracts interior channel knot values from FoilDSL text + authored stations from SourceParse.Authored(),
    /// then runs bounded adaptive spanwise refinement (R <= 4, N <= 257) until midpoint A-vs-B deviation <= TargetSkinMm.
    /// All section points come strictly from Placement (single geometry authority, ADR-0010).
    /// </summary>
    private static WingSkin BuildAdaptiveSkin(byte[] source)
    {
        var etas = ExtractInitialEtas(source);
        var xs = Placement.ChordGrid(ChordPointsPerSide).ToArray();
        var uParams = new double[ChordPointsPerSide];
        for (int j = 0; j < ChordPointsPerSide; j++)
            uParams[j] = j / (double)(ChordPointsPerSide - 1);

        NurbsSurface3 upperSrf = null!;
        NurbsSurface3 lowerSrf = null!;
        double minTeMm = double.PositiveInfinity;
        double maxTeMm = 0.0;
        double halfSpanMm = 0.0;
        int rounds = 0;

        for (int round = 0; round <= MaxRefinementRounds; round++)
        {
            rounds = round;
            var (upperGrid, lowerGrid, teMin, teMax, spanMm) = EvaluatePlacedSections(source, etas, xs);
            minTeMm = teMin;
            maxTeMm = teMax;
            halfSpanMm = spanMm;

            upperSrf = NurbsMath.SkinSections(uParams, etas.ToArray(), upperGrid, degreeU: 3, degreeV: 3);
            lowerSrf = NurbsMath.SkinSections(uParams, etas.ToArray(), lowerGrid, degreeU: 3, degreeV: 3);

            if (round == MaxRefinementRounds || etas.Count >= MaxSections)
                break;

            // Check spanwise midpoints between adjacent eta stations to guide adaptive refinement.
            var midEtas = new List<double>(etas.Count - 1);
            for (int k = 0; k < etas.Count - 1; k++)
                midEtas.Add(0.5 * (etas[k] + etas[k + 1]));

            var (midU, midL, _, _, _) = EvaluatePlacedSections(source, midEtas, xs);
            var toInsert = new List<double>();
            for (int k = 0; k < midEtas.Count; k++)
            {
                double v = midEtas[k];
                double intervalMaxMm = 0.0;
                for (int j = 0; j < ChordPointsPerSide; j += 2)
                {
                    double u = uParams[j];
                    double du = (upperSrf.Evaluate(u, v) - midU[j, k]).Length();
                    double dl = (lowerSrf.Evaluate(u, v) - midL[j, k]).Length();
                    intervalMaxMm = Math.Max(intervalMaxMm, Math.Max(du, dl));
                }
                if (intervalMaxMm > TargetSkinMm)
                    toInsert.Add(v);
            }

            if (toInsert.Count == 0)
                break;

            foreach (double v in toInsert)
            {
                if (etas.Count >= MaxSections) break;
                etas.Add(v);
            }
            etas.Sort();
        }

        bool isOpenTe = maxTeMm > 1e-6;
        return new WingSkin(upperSrf, lowerSrf, isOpenTe, halfSpanMm, minTeMm, maxTeMm, etas.Count, ChordPointsPerSide, rounds);
    }

    private static List<double> ExtractInitialEtas(byte[] source)
    {
        var set = new SortedSet<double> { 0.0, 1.0 };
        var parsed = FoilSource.Parse(source);
        foreach (var assignment in parsed.Authored().Assignments)
            set.Add(assignment.Eta);

        // Extract channel knot values (leading/trailing/dihedral/twist/thickness) from FoilDSL source text.
        string text = Encoding.UTF8.GetString(source);
        // Only read knots appearing before the "profiles {" block so we capture channel/rail knots.
        int profilesAt = text.IndexOf("profiles", StringComparison.Ordinal);
        string channelText = profilesAt > 0 ? text[..profilesAt] : text;
        foreach (Match m in KnotsBlockRx.Matches(channelText))
        {
            foreach (string tok in m.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(tok.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double k) && k > 0.0 && k < 1.0)
                    set.Add(k);
            }
        }

        // Seed with at least 16 uniform spanwise intervals so cubic B-spline skinning starts well-conditioned.
        for (int i = 0; i <= 16; i++)
            set.Add(i / 16.0);

        // Merge any near-duplicate knots within 1e-7 so collocation matrix stays well-conditioned.
        var result = new List<double>();
        foreach (double v in set)
        {
            if (result.Count == 0 || v - result[^1] > 1e-6)
                result.Add(v);
        }
        if (result[^1] < 1.0) result[^1] = 1.0;
        return result;
    }

    /// <summary>
    /// Evaluates exact Rule A placed upper and lower section points at arbitrary <paramref name="etas"/> and chord abscissae
    /// using <see cref="Placement.Sections"/> (single placement authority, ADR-0010).
    /// </summary>
    private static (Vec3[,] Upper, Vec3[,] Lower, double MinTeMm, double MaxTeMm, double HalfSpanMm)
        EvaluatePlacedSections(byte[] source, IReadOnlyList<double> etas, double[] xs)
    {
        var samples = Placement.Sections(source, etas, xs, CancellationToken.None);
        int nu = xs.Length;
        int nv = etas.Count;
        var upper = new Vec3[nu, nv];
        var lower = new Vec3[nu, nv];
        double minTeMm = double.PositiveInfinity;
        double maxTeMm = 0.0;

        for (int k = 0; k < nv; k++)
        {
            var s = samples[k];
            double chord = s.Frame.ChordMeters;
            var (sin, cos) = double.SinCosPi(s.Frame.TwistDegrees / 180.0);
            for (int j = 0; j < nu; j++)
            {
                var mid = s.PlacedCamber[j];
                double halfThick = 0.5 * s.Thickness[j] * chord;
                double dx = halfThick * sin;
                double dz = halfThick * cos;
                upper[j, k] = new((mid.X + dx) * 1000.0, mid.Y * 1000.0, (mid.Z + dz) * 1000.0);
                lower[j, k] = new((mid.X - dx) * 1000.0, mid.Y * 1000.0, (mid.Z - dz) * 1000.0);
            }
            double teMm = (upper[nu - 1, k] - lower[nu - 1, k]).Length();
            minTeMm = Math.Min(minTeMm, teMm);
            maxTeMm = Math.Max(maxTeMm, teMm);
        }

        double halfSpanMm = samples[^1].Frame.SpanMeters * 1000.0;
        return (upper, lower, minTeMm, maxTeMm, halfSpanMm);
    }

    private sealed record DeviationMeasurement(
        int SampleCount,
        double MaxClosestPointMm,
        double MaxParametricMm,
        double TipPlanarityErrorMm,
        double RootPlanarityErrorMm,
        string WorstSide,
        double WorstEta,
        double WorstChordX);

    /// <summary>
    /// Measures true A-vs-B closest-point skin deviation directly against <see cref="Placement.Surface"/> (51 x 201 + authored stations),
    /// plus exact planarity of the skinned tip and root boundary curves.
    /// </summary>
    private static DeviationMeasurement MeasureDeviationAgainstPlacementSurface(byte[] source, WingSkin skin, int stations, int chordSamples)
    {
        var surf = Placement.Surface(source, "accepted", 0, CancellationToken.None, stations, chordSamples);
        var xs = Placement.ChordGrid(chordSamples);

        double maxClosest = 0.0;
        double maxParam = 0.0;
        string worstSide = "upper";
        double worstEta = 0.0;
        double worstX = 0.0;
        int sampleCount = 0;

        for (int k = 0; k < surf.Sections.Count; k++)
        {
            var sec = surf.Sections[k];
            double v0 = sec.Eta;
            for (int j = 0; j < chordSamples; j++)
            {
                double u0 = j / (double)(chordSamples - 1);

                var puRef = Vec3.FromMillimetres(sec.Upper[j]);
                double paramU = (skin.UpperStarboard.Evaluate(u0, v0) - puRef).Length();
                double closestU = paramU <= 1e-9 ? paramU : skin.UpperStarboard.ClosestPoint(puRef, u0, v0).DistanceMm;
                maxParam = Math.Max(maxParam, paramU);
                if (closestU > maxClosest)
                {
                    maxClosest = closestU;
                    worstSide = "upper";
                    worstEta = sec.Eta;
                    worstX = xs[j];
                }

                var plRef = Vec3.FromMillimetres(sec.Lower[j]);
                double paramL = (skin.LowerStarboard.Evaluate(u0, v0) - plRef).Length();
                double closestL = paramL <= 1e-9 ? paramL : skin.LowerStarboard.ClosestPoint(plRef, u0, v0).DistanceMm;
                maxParam = Math.Max(maxParam, paramL);
                if (closestL > maxClosest)
                {
                    maxClosest = closestL;
                    worstSide = "lower";
                    worstEta = sec.Eta;
                    worstX = xs[j];
                }
                sampleCount += 2;
            }
        }

        // Check boundary planarity at root (y = 0) and tip (y = HalfSpanMm)
        double rootPlanarErr = 0.0;
        double tipPlanarErr = 0.0;
        for (int j = 0; j <= 100; j++)
        {
            double u = j / 100.0;
            rootPlanarErr = Math.Max(rootPlanarErr, Math.Abs(skin.UpperStarboard.Evaluate(u, 0.0).Y));
            rootPlanarErr = Math.Max(rootPlanarErr, Math.Abs(skin.LowerStarboard.Evaluate(u, 0.0).Y));
            tipPlanarErr = Math.Max(tipPlanarErr, Math.Abs(skin.UpperStarboard.Evaluate(u, 1.0).Y - skin.HalfSpanMm));
            tipPlanarErr = Math.Max(tipPlanarErr, Math.Abs(skin.LowerStarboard.Evaluate(u, 1.0).Y - skin.HalfSpanMm));
        }

        return new DeviationMeasurement(sampleCount, maxClosest, maxParam, tipPlanarErr, rootPlanarErr, worstSide, worstEta, worstX);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CFDWorkbench.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
