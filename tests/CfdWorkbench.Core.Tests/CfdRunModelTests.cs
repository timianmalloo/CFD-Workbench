using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Red-first pure Core tests for Area 5 CFD Run Pipeline (docs/design/cfd-run-pipeline.md §9):
/// TypedCaseModel alpha-imposition &amp; mesh-hash stability, non-finite refusal, deterministic OpenFOAM/SU2
/// emitters honoring foam-dict-lint.py TYPES_ALLOWED, DR-F3-1 A / DR-F2-3 MeshGateEvaluator, 11-state
/// CfdRunStateMachine, DR-F2-1 three-part residual criterion, and LAB-01 three-part Converged label.
/// Strictly zero-I/O (recursive STORE-SUBSET verified).
/// </summary>
internal static class CfdRunModelTests
{
    private static readonly DateTimeOffset FixedUtc = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly HashSet<string> TypesAllowed = new(StringComparer.Ordinal)
    {
        "forceCoeffs", "solverInfo", "yPlus", "surfaces", "wallShearStress",
        "patch", "wall", "empty", "symmetryPlane", "freestreamVelocity",
        "freestreamPressure", "noSlip", "zeroGradient", "inletOutlet", "fixedValue",
        "calculated", "triSurfaceMesh", "box", "cellSet", "hePsiThermo", "symmetry"
    };

    internal static void Run()
    {
        Check("TypedCaseModel_InflowAlpha_SameMeshHashAcrossAngles", InflowAlpha_SameMeshHashAcrossAngles);
        Check("TypedCaseModel_NonFiniteOrLocaleComma_RefusedBeforeEmit", NonFiniteOrLocaleComma_RefusedBeforeEmit);
        Check("OpenFoamEmitter_DeterministicAndStrictTypesAllowed", OpenFoamEmitter_DeterministicAndStrictTypesAllowed);
        Check("Su2Emitter_FreeSurfaceOrCavitation_UnsupportedBeforeLeavingPending", Su2Emitter_CapabilityGateAndEmit);
        Check("MeshGate_DrF31A_And_DrF23_ExactBoundaries", MeshGate_ExactBoundaries);
        Check("StateMachine_ElevenStates_And_DrF21_ThreePartCriterion", StateMachine_And_DrF21);
    }

    private static void InflowAlpha_SameMeshHashAcrossAngles()
    {
        var m0 = SampleCase(alphaDeg: 0.0, speed: 7.5);
        var m4 = SampleCase(alphaDeg: 4.0, speed: 7.5);
        var mSpeed = SampleCase(alphaDeg: 0.0, speed: 9.0);

        // Held geometry & speed across angles share identical meshHash, but differ in runKey!
        Equal(m0.ComputeMeshHash(), m4.ComputeMeshHash());
        Equal(false, string.Equals(m0.ComputeCaseKey(), m4.ComputeCaseKey(), StringComparison.Ordinal));
        Equal(false, string.Equals(m0.ComputeMeshHash(), mSpeed.ComputeMeshHash(), StringComparison.Ordinal));

        // Exact inflow vector (V cos α, 0, V sin α) and wind-axis lift/drag directions.
        var u0 = m0.InflowVelocity();
        Equal(true, Math.Abs(u0.X - 7.5) < 1e-12);
        Equal(true, Math.Abs(u0.Y) < 1e-12);
        Equal(true, Math.Abs(u0.Z) < 1e-12);

        double rad = 4.0 * Math.PI / 180.0;
        var u4 = m4.InflowVelocity();
        Equal(true, Math.Abs(u4.X - 7.5 * Math.Cos(rad)) < 1e-12);
        Equal(true, Math.Abs(u4.Y) < 1e-12);
        Equal(true, Math.Abs(u4.Z - 7.5 * Math.Sin(rad)) < 1e-12);

        var lift = m4.LiftDirection();
        var drag = m4.DragDirection();
        Equal(true, Math.Abs(lift.X - (-Math.Sin(rad))) < 1e-12);
        Equal(true, Math.Abs(lift.Z - Math.Cos(rad)) < 1e-12);
        Equal(true, Math.Abs(drag.X - Math.Cos(rad)) < 1e-12);
        Equal(true, Math.Abs(drag.Z - Math.Sin(rad)) < 1e-12);
    }

    private static void NonFiniteOrLocaleComma_RefusedBeforeEmit()
    {
        var nanAlpha = SampleCase(alphaDeg: double.NaN);
        var infSpeed = SampleCase(speed: double.PositiveInfinity);
        Refuses("CFD-CASE-NONFINITE", () => OpenFoamCaseEmitter.EmitCaseFiles(nanAlpha));
        Refuses("CFD-CASE-NONFINITE", () => OpenFoamCaseEmitter.EmitCaseFiles(infSpeed));
        Refuses("CFD-CASE-NONFINITE", () => Su2CaseEmitter.EmitConfigFiles(nanAlpha));

        // Culture-independence check under a comma-decimal culture (e.g., fr-FR).
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var ofFiles = OpenFoamCaseEmitter.EmitCaseFiles(SampleCase(alphaDeg: 3.5, speed: 6.25));
            foreach (var (rel, content) in ofFiles)
            {
                if (rel == "cfdw-manifest.json") continue;
                Equal(false, Regex.IsMatch(content, @"\d,\d"));
                Equal(false, content.Contains('\r'));
            }
            var su2Files = Su2CaseEmitter.EmitConfigFiles(SampleCase(backend: CfdBackendKind.Su2, alphaDeg: 3.5, speed: 6.25));
            Equal(false, Regex.IsMatch(su2Files["case.cfg"], @"\d,\d"));
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    private static void OpenFoamEmitter_DeterministicAndStrictTypesAllowed()
    {
        var model = SampleCase(alphaDeg: 3.0, speed: 7.0);
        var run1 = OpenFoamCaseEmitter.EmitCaseFiles(model);
        var run2 = OpenFoamCaseEmitter.EmitCaseFiles(model);

        Equal(run1.Count, run2.Count);
        foreach (var (k, v) in run1) Equal(v, run2[k]);

        // Must contain cfdw-manifest.json listing exact 11 SA files.
        Equal(true, run1.ContainsKey("cfdw-manifest.json"));
        var manifest = JsonSerializer.Deserialize<string[]>(run1["cfdw-manifest.json"])!;
        Equal(11, manifest.Length);
        foreach (string rel in manifest) Equal(true, run1.ContainsKey(rel));

        // Mandatory runTimeModifiable false in system/controlDict; zero InfoSwitches in case (lives in M1 HOME controlDict).
        Equal(true, run1["system/controlDict"].Contains("runTimeModifiable false;", StringComparison.Ordinal));
        foreach (var (rel, content) in run1)
        {
            if (rel == "cfdw-manifest.json") continue;
            Equal(false, content.Contains("InfoSwitches", StringComparison.Ordinal));
            Equal(false, content.Contains("allowSystemOperations", StringComparison.Ordinal));
            Equal(false, content.Contains("#codeStream", StringComparison.Ordinal));
            Equal(false, content.Contains("#calc", StringComparison.Ordinal));
            Equal(false, content.Contains("#include", StringComparison.Ordinal));

            // Every `type <v>;` token must stay strictly inside 21-entry TYPES_ALLOWED (zero `type forces;`).
            foreach (Match m in Regex.Matches(content, @"(?:^|[{;}])\s*type\s+([A-Za-z_][\w]*)\s*;", RegexOptions.Multiline))
            {
                string val = m.Groups[1].Value;
                Equal(true, TypesAllowed.Contains(val));
            }
        }

        // OpenFOAM WallFunction refuses unsupported wall-function BC types cleanly in Wave 10.
        var wfModel = model with { WallTreatment = WallTreatmentKind.WallFunction };
        Refuses("CFD-UNSUPPORTED-BC", () => OpenFoamCaseEmitter.EmitCaseFiles(wfModel));
    }

    private static void Su2Emitter_CapabilityGateAndEmit()
    {
        var freeSurfaceCase = SampleCase(backend: CfdBackendKind.Su2, freeSurface: true);
        var capFree = freeSurfaceCase.CheckCapability(BackendCapability.Su2V850);
        Equal(false, capFree.Supported);
        Equal(true, capFree.Reason!.Contains("SU2", StringComparison.Ordinal));
        Refuses("CFD-UNSUPPORTED-PHYSICS", () => Su2CaseEmitter.EmitConfigFiles(freeSurfaceCase));

        var cavitationCase = SampleCase(backend: CfdBackendKind.Su2, cavitation: true);
        Equal(false, cavitationCase.CheckCapability(BackendCapability.Su2V850).Supported);
        Refuses("CFD-UNSUPPORTED-PHYSICS", () => Su2CaseEmitter.EmitConfigFiles(cavitationCase));

        // State machine transitions Pending -> Unsupported cleanly without leaving Pending to Readiness.
        var pending = CfdRunStateMachine.CreatePending(Guid.NewGuid().ToString("D"), FixedUtc);
        var unsupported = CfdRunStateMachine.AppendEvent([pending], CfdRunState.Unsupported, FixedUtc.AddSeconds(1), "CFD-UNSUPPORTED-PHYSICS", capFree.Reason);
        Equal(CfdRunState.Unsupported, unsupported[^1].State);
        Equal(true, CfdRunStateMachine.IsTerminal(CfdRunState.Unsupported));

        // Valid single-phase SU2 emits deterministic case.cfg + cfdw-manifest.json with HISTORY_OUTPUT.
        var validSu2 = SampleCase(backend: CfdBackendKind.Su2, alphaDeg: 4.0, speed: 6.0);
        var f1 = Su2CaseEmitter.EmitConfigFiles(validSu2);
        var f2 = Su2CaseEmitter.EmitConfigFiles(validSu2);
        Equal(f1["case.cfg"], f2["case.cfg"]);
        Equal(true, f1.ContainsKey("cfdw-manifest.json"));
        Equal(true, f1["case.cfg"].Contains("SOLVER= INC_RANS", StringComparison.Ordinal));
        Equal(true, f1["case.cfg"].Contains("HISTORY_OUTPUT=", StringComparison.Ordinal));
        Equal(true, f1["case.cfg"].Contains("CONV_FILENAME= history", StringComparison.Ordinal));
        Equal(false, f1["case.cfg"].Contains('#'));
    }

    private static void MeshGate_ExactBoundaries()
    {
        var goodMetrics = GoodMetrics();
        var wallResolvedOpts = new MeshGateOptions(WallTreatmentKind.WallResolved, MeshKind.UnstructuredOpenFoam, FieldIsStationary: true);

        // Exact passing boundary (70.0, 4.0, 20.0, 0.001, 0.95, 1.0, yPlusP95=1.0, yPlusMax=2.0).
        var boundaryPass = MeshGateEvaluator.EvaluateMeshGate(goodMetrics with
        {
            MaxNonOrthogonalityDeg = 70.0,
            MaxInternalSkewness = 4.0,
            MaxBoundarySkewness = 20.0,
            MinCellDeterminantLayer = 0.001,
            MinCellDeterminantCore = 0.001,
            UpstreamChords = 10.0,
            DownstreamChords = 20.0,
            LayerExpansionRatio = 1.2,
            LayerCoverage20PlusFraction = 0.95,
            LayerCoverage10PlusFraction = 1.0,
            YPlusP95 = 1.0,
            YPlusMax = 2.0
        }, wallResolvedOpts);
        Equal(true, boundaryPass.Passed);
        Equal(false, boundaryPass.IsScreenOnly);
        Equal(0, boundaryPass.Failures.Count);

        // Exact failing boundaries just past each threshold.
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MaxNonOrthogonalityDeg = 70.1 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MaxInternalSkewness = 4.1 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MaxBoundarySkewness = 20.1 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MinCellVolume = 0.0 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MinCellDeterminantLayer = 0.0009 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { MinCellDeterminantCore = 0.0009 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { LayerCoverage20PlusFraction = 0.949 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { LayerCoverage10PlusFraction = 0.999 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { YPlusP95 = 1.01 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { YPlusMax = 2.01 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { UpstreamChords = 9.9 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { DownstreamChords = 19.9 }, wallResolvedOpts).Passed);
        Equal(false, MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { LayerExpansionRatio = 1.21 }, wallResolvedOpts).Passed);

        // Pre-stationary evaluation marks IsScreenOnly = true.
        var screenOnly = MeshGateEvaluator.EvaluateMeshGate(goodMetrics with { YPlusP95 = null, YPlusMax = null },
            new MeshGateOptions(WallTreatmentKind.WallResolved, MeshKind.UnstructuredOpenFoam, FieldIsStationary: false));
        Equal(true, screenOnly.Passed);
        Equal(true, screenOnly.IsScreenOnly);

        // DR-F2-3 wall-function feasibility rejects typical thin hydrofoil boundary layer (Re ~ 7e5).
        var hydrofoil = SampleCase(speed: 7.0);
        var wfHydrofoil = hydrofoil.CheckWallFunctionFeasibility();
        Equal(false, wfHydrofoil.Feasible);
        Equal(true, wfHydrofoil.StackHeight15Meters > wfHydrofoil.BoundaryLayerThicknessMeters);

        // High-Re large chord admits wall functions.
        var shipScale = hydrofoil with
        {
            Reference = new RunReference(100.0, 10.0, 10.0, "frame origin", "body"),
            Op = hydrofoil.Op with { Speed = 15.0 }
        };
        var wfShip = shipScale.CheckWallFunctionFeasibility();
        Equal(true, wfShip.Feasible);
    }

    private static void StateMachine_And_DrF21()
    {
        // All 11 enum states exist.
        Equal(11, Enum.GetValues<CfdRunState>().Length);

        // Legal happy path: Pending -> Readiness -> Meshing -> MeshGate -> Solving -> Harvesting -> Completed.
        var events = new List<CfdRunStateEvent> { CfdRunStateMachine.CreatePending(Guid.NewGuid().ToString("D"), FixedUtc) };
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, FixedUtc.AddSeconds(1))];
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, FixedUtc.AddSeconds(2))];
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, FixedUtc.AddSeconds(3))];
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, FixedUtc.AddSeconds(4))];
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, FixedUtc.AddSeconds(5))];
        events = [.. CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, FixedUtc.AddSeconds(6))];
        Equal(7, events.Count);

        // Illegal edges refused deterministically with CFD-STATE-INVALID.
        Equal(false, CfdRunStateMachine.EvaluateTransition(CfdRunState.Pending, CfdRunState.Solving).Legal);
        Equal("CFD-STATE-INVALID", CfdRunStateMachine.EvaluateTransition(CfdRunState.Pending, CfdRunState.Solving).Code);
        Equal(false, CfdRunStateMachine.EvaluateTransition(CfdRunState.Completed, CfdRunState.Solving).Legal);
        Equal(false, CfdRunStateMachine.EvaluateTransition(CfdRunState.Unsupported, CfdRunState.Readiness).Legal);
        Equal(false, CfdRunStateMachine.EvaluateTransition(CfdRunState.Cancelled, CfdRunState.Solving).Legal);
        // Checkpoint resume legal edge: Readiness -> Solving.
        Equal(true, CfdRunStateMachine.EvaluateTransition(CfdRunState.Readiness, CfdRunState.Solving).Legal);

        // DR-F2-1 three-part residual criterion.
        var clWin = Enumerable.Range(0, 30).Select(i => 0.412 + (i % 2 == 0 ? 1e-4 : -1e-4)).ToArray();
        var cdWin = Enumerable.Range(0, 30).Select(i => 0.0184 + (i % 2 == 0 ? 2e-5 : -2e-5)).ToArray();
        var cmWin = Enumerable.Range(0, 30).Select(_ => -0.041).ToArray();
        var eqsPass = new Dictionary<string, (double First, double Final)>
        {
            ["p"] = (0.5, 2.0e-4),
            ["U"] = (0.8, 3.0e-4),
            ["nuTilda"] = (0.2, 5.0e-5)
        };

        var passSummary = CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(eqsPass, clWin, cdWin, cmWin, TurbulenceBoundingCountInWindow: 0));
        Equal(true, passSummary.CriterionMet);

        // Part 1 fail: residual ratio > 1e-3.
        var eqsFail = new Dictionary<string, (double First, double Final)>(eqsPass) { ["p"] = (0.5, 6.0e-4) };
        Equal(false, CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(eqsFail, clWin, cdWin, cmWin)).CriterionMet);

        // Part 2 fail: Cl half-band > 1e-3.
        var clNoisy = Enumerable.Range(0, 30).Select(i => 0.412 + (i % 2 == 0 ? 2e-3 : -2e-3)).ToArray();
        Equal(false, CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(eqsPass, clNoisy, cdWin, cmWin)).CriterionMet);

        // Part 3 fail: turbulence bounding > 0 inside window!
        Equal(false, CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(eqsPass, clWin, cdWin, cmWin, TurbulenceBoundingCountInWindow: 1)).CriterionMet);

        // LAB-01 three-part Converged label.
        string? label = CfdRunStateMachine.FormatConvergedLabel(passSummary, outputsComplete: true);
        Equal(true, label is not null);
        Equal(true, label!.StartsWith("Converged: residuals <= ", StringComparison.Ordinal));
        Equal(true, label.Contains("outputs complete", StringComparison.Ordinal));
        Equal(true, label.Contains("grid uncertainty not quantified (single mesh)", StringComparison.Ordinal));

        // Never returns Converged when outputs incomplete or criterion not met.
        Equal<string?>(null, CfdRunStateMachine.FormatConvergedLabel(passSummary, outputsComplete: false));
        var failedSummary = CfdRunStateMachine.EvaluateResiduals(new ResidualWindowInput(eqsPass, clWin, cdWin, cmWin, TurbulenceBoundingCountInWindow: 1));
        Equal<string?>(null, CfdRunStateMachine.FormatConvergedLabel(failedSummary, outputsComplete: true));
    }

    internal static TypedCaseModel SampleCase(
        CfdBackendKind backend = CfdBackendKind.OpenFoamEsi,
        double alphaDeg = 3.0,
        double speed = 7.0,
        bool freeSurface = false,
        bool cavitation = false)
    {
        var inputs = new RunInputs(
            Guid.NewGuid().ToString("D"),
            new string('a', 64),
            [new string('b', 64)],
            "cfdw-cv/2",
            "rule-a/1");
        var reference = new RunReference(0.108, 0.9, 0.12, "frame origin", "body");
        var bounds = new WingBounds(0.0, 0.15, 0.0, 0.45, -0.02, 0.02);
        var water = new WaterRecord(15.0, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", new string('c', 64));
        var op = new OperatingPoint(speed, 101325.0, 0.5, "root LE", alphaDeg, null);
        var pin = backend == CfdBackendKind.OpenFoamEsi
            ? new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720-20260127", new string('d', 64), true)
            : new CfdBackendPin("su2", "v8.5.0", "win-native", "su2-8.5.0", new string('e', 64), true);

        return new TypedCaseModel(
            inputs, reference, bounds, water, op, pin,
            Physics: new PhysicsFlags(RequiresFreeSurface: freeSurface, RequiresCavitation: cavitation, Transient: false));
    }

    internal static MeshMetrics GoodMetrics() => new(
        MinCellVolume: 1.2e-11,
        InvalidFaces: 0,
        ZeroAreaFaces: 0,
        OpenCells: 0,
        FailedCheckMeshCount: 0,
        MaxNonOrthogonalityDeg: 42.3,
        MaxInternalSkewness: 1.8,
        MaxBoundarySkewness: 3.4,
        MinCellDeterminantLayer: 0.042,
        MinCellDeterminantCore: 0.68,
        MinStructuredDeterminant: null,
        MeanStructuredDeterminant: null,
        UpstreamChords: 12.0,
        DownstreamChords: 24.0,
        LayerExpansionRatio: 1.18,
        LayerCoverage20PlusFraction: 0.985,
        LayerCoverage10PlusFraction: 1.0,
        RegionCoverage:
        [
            new RegionLayerCoverage("leading-edge", 20, 0.99, 1.0),
            new RegionLayerCoverage("main-surface", 20, 0.99, 1.0),
            new RegionLayerCoverage("trailing-edge-3pct", 16, 0.95, 1.0),
            new RegionLayerCoverage("tip", 15, 0.96, 1.0)
        ],
        YPlusMin: 0.12,
        YPlusP95: 0.62,
        YPlusMax: 1.38,
        WallFunctionLayerPoints: null);
}
