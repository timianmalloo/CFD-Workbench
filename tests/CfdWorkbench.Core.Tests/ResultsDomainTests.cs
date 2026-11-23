using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Red-first pure zero-I/O Core tests for Area 6 Results domain
/// (docs/design/results-and-candidates.md §2, §3, §4, §9.1).
/// Strictly STORE-SUBSET compliant (zero filesystem/env/PInvoke).
/// </summary>
internal static class ResultsDomainTests
{
    private static readonly string MeshA = new('a', 64);
    private static readonly string MeshB = new('b', 64);
    private static readonly string SurfaceA = new('1', 64);
    private static readonly string ArtifactHash = new('2', 64);
    private static readonly string TableHash = new('3', 64);

    internal static void Run()
    {
        Check("FieldEvidenceManifest_SeparationStrictlyGatedOnWallTauW", FieldEvidenceManifest_SeparationStrictlyGatedOnWallTauW);
        Check("ProvenanceStrip_AllNineFields_And_TableTwinParity", ProvenanceStrip_AllNineFields_And_TableTwinParity);
        Check("ReplaySequence_FixedRange_FailedSamplePausesAndClears_ZeroInterpolation", ReplaySequence_FixedRange_FailedSamplePausesAndClears_ZeroInterpolation);
        Check("DifferenceFlood_ZeroPinned_And_MeshHashMismatchUnavailable", DifferenceFlood_ZeroPinned_And_MeshHashMismatchUnavailable);
    }

    private static WaterRecord SampleWater() =>
        new(15.0, 35.16504, 1026.021, 1.18831e-6, 1705.1, "ITTC 7.5-02-01-03 Rev 03", TableHash);

    private static FieldEvidenceRecord MakeEvidence(
        string runId,
        string kind,
        string[] fields,
        bool hasWss = false,
        string? sepCriterion = null,
        bool hasTke = false,
        bool isTransient = false,
        double[]? timesteps = null) =>
        CfdRunRecords.Seal(new FieldEvidenceRecord(
            EvidenceId: Guid.NewGuid().ToString("D"),
            RunId: runId,
            ContentHash: "",
            Kind: kind,
            RelativePath: $"postProcessing/{kind}/500/out.raw",
            ArtifactHash: ArtifactHash,
            MeshHash: MeshA,
            Fields: fields,
            Association: "face",
            Ranges: [new FieldRangeRecord(fields[0], "dimensionless", -1.5, 1.0, "sample-range")],
            ColormapPolicyVersion: "crameri-8.0",
            ReductionTool: "cfdw-harvester",
            ReductionToolVersion: "1.0.0",
            HasWallShearStress: hasWss,
            SeparationCriterion: sepCriterion,
            HasTurbulentKineticEnergy: hasTke,
            IsTransient: isTransient,
            Timesteps: timesteps ?? []));

    private static void FieldEvidenceManifest_SeparationStrictlyGatedOnWallTauW()
    {
        string runId = Guid.NewGuid().ToString("D");

        // Case 1: Q/lambda2 isosurface + Cp surface present, NO wallShearStress -> Separation MUST be disabled.
        var noTauW = FieldEvidenceManifest.BuildManifest(
            runId,
            [
                MakeEvidence(runId, "surface", ["Cp", "p"], hasWss: false, sepCriterion: null, hasTke: true),
                MakeEvidence(runId, "isosurface", ["Q", "lambda2"], hasWss: false, sepCriterion: null),
                MakeEvidence(runId, "rake", ["U"], hasWss: false, sepCriterion: null)
            ],
            hasForceVectors: true,
            hasSpanwiseLoading: true,
            vortexThreshold: 120.0);

        var sepDisabled = noTauW.ResolveLayer(FieldLayerKind.Separation);
        Equal(false, sepDisabled.Available);
        Equal("No supported criterion in this sample", sepDisabled.UnavailableReason);

        // Q/lambda2 isosurface is strictly labelled vortex-core candidates, NEVER separation.
        var vortex = noTauW.ResolveLayer(FieldLayerKind.VortexCoreCandidates);
        Equal(true, vortex.Available);
        Equal("vortex-core candidates at threshold 120", vortex.BasisLabel);
        if (vortex.BasisLabel.Contains("separation", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Vortex-core label must never say separation.");

        // Streamlines & k basis labels match spec A5.11 verbatim.
        var streamlines = noTauW.ResolveLayer(FieldLayerKind.Streamlines3D);
        Equal(true, streamlines.Available);
        Equal("instantaneous integral curves of the mean field (steady RANS)", streamlines.BasisLabel);
        Equal("affordance, not fluid motion", streamlines.MovingDashLabel);

        var tke = noTauW.ResolveLayer(FieldLayerKind.TurbulentKineticEnergy);
        Equal(true, tke.Available);
        Equal("modeled turbulent kinetic energy, m²/s², model, mean-flow basis", tke.BasisLabel);

        // Steady RANS has no physical-time pathlines.
        var pathlines = noTauW.ResolveLayer(FieldLayerKind.Pathlines);
        Equal(false, pathlines.Available);
        Equal("Unavailable — steady RANS has no physical-time pathlines", pathlines.UnavailableReason);

        // AI-10 offline validator refuses to diagnose separation when tau_w is absent.
        var askNoSep = FieldEvidenceManifest.EvaluateAskAboutSeparation(noTauW, "Is there flow separation on the wing?");
        Equal(false, askNoSep.SeparationSupported);
        Equal("No supported criterion in this sample", askNoSep.Answer);

        // Case 2: Wall shear stress exists WITHOUT a named criterion -> Separation still strictly disabled.
        var tauWNoCriterion = FieldEvidenceManifest.BuildManifest(
            runId,
            [MakeEvidence(runId, "surface", ["Cp", "wallShearStress"], hasWss: true, sepCriterion: null)]);
        Equal(false, tauWNoCriterion.ResolveLayer(FieldLayerKind.Separation).Available);
        Equal("No supported criterion in this sample", tauWNoCriterion.ResolveLayer(FieldLayerKind.Separation).UnavailableReason);

        // Case 3: Wall shear stress exists WITH named criterion -> Separation enabled with exact criterion label.
        var withTauW = FieldEvidenceManifest.BuildManifest(
            runId,
            [MakeEvidence(runId, "surface", ["Cp", "Cf", "wallShearStress"], hasWss: true, sepCriterion: "skin-friction-line convergence")]);
        var sepEnabled = withTauW.ResolveLayer(FieldLayerKind.Separation);
        Equal(true, sepEnabled.Available);
        Equal("skin-friction-line convergence", sepEnabled.SeparationCriterion);
        Equal("Separation — criterion: skin-friction-line convergence", sepEnabled.BasisLabel);

        var askSep = FieldEvidenceManifest.EvaluateAskAboutSeparation(withTauW, "Where does flow separate?");
        Equal(true, askSep.SeparationSupported);
        Equal("skin-friction-line convergence", askSep.Criterion);

        // Case 4: Second supported criterion "C_f,x sign change on this cut" also admitted.
        var withCfx = FieldEvidenceManifest.BuildManifest(
            runId,
            [MakeEvidence(runId, "surface", ["Cf", "wallShearStress"], hasWss: true, sepCriterion: "C_f,x sign change on this cut")]);
        Equal(true, withCfx.ResolveLayer(FieldLayerKind.Separation).Available);

        // Case 4b: Bogus/vortex criterion on wallShearStress row MUST NOT enable Separation.
        var bogusCriterion = FieldEvidenceManifest.BuildManifest(
            runId,
            [MakeEvidence(runId, "surface", ["Cf", "wallShearStress"], hasWss: true, sepCriterion: "Q-criterion")]);
        Equal(false, bogusCriterion.ResolveLayer(FieldLayerKind.Separation).Available);
        Equal("No supported criterion in this sample", bogusCriterion.ResolveLayer(FieldLayerKind.Separation).UnavailableReason);

        // Case 4c: Transient run with >= 2 timesteps enables Pathlines.
        var transientManifest = FieldEvidenceManifest.BuildManifest(
            runId,
            [MakeEvidence(runId, "surface", ["Cp"], isTransient: true, timesteps: [0.0, 0.1, 0.2])]);
        Equal(true, transientManifest.ResolveLayer(FieldLayerKind.Pathlines).Available);
        Equal("physical-time pathlines (transient)", transientManifest.ResolveLayer(FieldLayerKind.Pathlines).BasisLabel);

        // Case 5: Local VLM manifest never enables Separation.
        var local = FieldEvidenceManifest.BuildLocalVlmManifest(runId, hasPanelCp: true, hasForceVectors: true, hasSpanwiseLoading: true);
        Equal(true, local.ResolveLayer(FieldLayerKind.SurfaceCp).Available);
        Equal(false, local.ResolveLayer(FieldLayerKind.Separation).Available);
        Equal("No supported criterion in this sample", local.ResolveLayer(FieldLayerKind.Separation).UnavailableReason);
    }

    private static void ProvenanceStrip_AllNineFields_And_TableTwinParity()
    {
        // Unmeasured/blank inputs normalize explicitly to "Not recorded", never empty or null.
        var strip = ProvenanceStrip.BuildProvenance(
            variable: "Cp",
            unit: "dimensionless",
            rangeWithBasis: ProvenanceStrip.FormatRange(-1.84, 1.00, "series-locked"),
            methodTierAndVersion: "OpenFOAM ESI v2512 · simpleFoam (SA)",
            sourceRunAndSample: "run 9a3f12c0e4b1 · α 4.0° · V 7.20 m/s",
            depthBasis: "",
            evidenceLabel: "Converged: residuals <= 4.2e-04 · outputs complete · grid uncertainty not quantified (single mesh)",
            colormapNameAndVersion: ProvenanceStrip.VikCrameri,
            associationAndTimeBasis: "face · steady RANS (mean field)",
            tableTwinRowCount: 3);

        Equal(true, ProvenanceStrip.HasAllNineFields(strip));
        Equal("Cp", strip.Variable);
        Equal("dimensionless", strip.Unit);
        Equal("[-1.84, 1.00] · series-locked", strip.RangeWithBasis);
        Equal("Not recorded", strip.DepthBasis);
        Equal("vik · Crameri 8.0", strip.ColormapNameAndVersion);
        Equal("vik · Crameri 8.0", strip.MapNameAndVersion);
        Equal("face · steady RANS (mean field)", strip.AssociationAndTimeBasis);
        Equal(3, strip.TableTwinRowCount);

        // TableTwin preserves exact values and NEVER substitutes 0.0 for missing/non-finite entries.
        var twin = ProvenanceStrip.BuildTableTwin("Cp", "dimensionless",
        [
            ("η 0.10", -1.84, null, "OK"),
            ("η 0.50", null, "masked trailing edge", null),
            ("η 0.70", double.NaN, "non-finite sample", null),
            ("η 0.90", 1.00, null, "OK")
        ]);

        Equal(4, twin.Rows.Count);
        Equal(-1.84, twin.Rows[0].Value);
        Equal((double?)null, twin.Rows[1].Value);
        Equal("Unavailable — masked trailing edge", twin.Rows[1].DisplayValue);
        Equal((double?)null, twin.Rows[2].Value);
        Equal("Unavailable — non-finite sample", twin.Rows[2].DisplayValue);
        if (twin.Rows[1].DisplayValue == "0" || twin.Rows[1].DisplayValue == "0.0" || twin.Rows[2].DisplayValue == "0")
            throw new InvalidOperationException("TableTwin must never substitute 0.0 for missing values.");
        Equal(1.00, twin.Rows[3].Value);
    }

    private static void ReplaySequence_FixedRange_FailedSamplePausesAndClears_ZeroInterpolation()
    {
        var water = SampleWater();
        var camera = new ReplayCameraState("starboard-iso", "rake-le-0.1c", ["y=0.25m"]);

        ReplaySampleSlot Slot(int idx, double alpha, double speed, string mesh, bool admitted, double[]? vals = null, string? fail = null)
        {
            string id = Guid.NewGuid().ToString("D");
            var prov = ProvenanceStrip.BuildProvenance(
                "Cp", "dimensionless", "[-1.50, 1.00] · series-locked",
                "OpenFOAM ESI v2512", $"run {id[..8]} · α {alpha:F1}° · V {speed:F2} m/s",
                "h_ref = 0.50 m", "Completed", ProvenanceStrip.VikCrameri, "face · steady RANS (mean field)", vals?.Length ?? 0);
            return new ReplaySampleSlot(
                CaseIndex: idx,
                RunId: id,
                SurfaceHash: SurfaceA,
                MeshHash: mesh,
                MethodId: "openfoam-esi",
                MethodVersion: "v2512",
                Water: water,
                FieldInventoryHash: "inv-cp-cf",
                SpeedMs: speed,
                AlphaDeg: alpha,
                Admitted: admitted,
                FailureReason: fail,
                RetrySampleRunId: admitted ? null : id,
                FieldValues: vals?.Select(v => (double?)v).ToArray(),
                Metrics: admitted ? new Dictionary<string, double?> { ["CL"] = 0.1 * alpha, ["CD"] = 0.015 } : null,
                Provenance: prov);
        }

        // Mixed meshHash strictly refused from series admission.
        Refuses("RES-SERIES-INCOMPATIBLE", () => ReplaySequence.CreateSeries(
            [Slot(0, 2.0, 7.0, MeshA, true, [-0.8, 0.5]), Slot(1, 4.0, 7.0, MeshB, true, [-1.2, 0.9])],
            ReplayHeldAxis.HeldSpeed,
            camera));

        // Compatible series ordered ascending by alpha (even if passed unsorted).
        var s0 = Slot(0, 2.0, 7.0, MeshA, true, [-0.8, 0.5]);
        var s1Fail = Slot(1, 4.0, 7.0, MeshA, false, null, "MeshGate failed: max non-orthogonality 78.2 deg");
        var s2 = Slot(2, 6.0, 7.0, MeshA, true, [-1.9, 1.0]);

        var series = ReplaySequence.CreateSeries([s2, s0, s1Fail], ReplayHeldAxis.HeldSpeed, camera, reducedMotion: false);
        Equal("Parametric sweep — sequence over admitted samples", series.Banner);
        Equal(3, series.Frames.Count);
        Equal(2.0, series.Frames[0].AlphaDeg);
        Equal(4.0, series.Frames[1].AlphaDeg);
        Equal(6.0, series.Frames[2].AlphaDeg);

        // Fixed scalar range computed once across admitted frames only.
        Equal(true, series.RangeLocked);
        Equal(-1.9, series.LockedScalarMin);
        Equal(1.0, series.LockedScalarMax);
        Equal(camera, series.CameraState);

        // Start playback on frame 0 (admitted).
        series = ReplaySequence.Play(series);
        Equal(true, series.IsPlaying);
        if (series.ActiveFieldData is null || series.ActiveMetrics is null)
            throw new InvalidOperationException("Frame 0 admitted sample must populate ActiveFieldData and ActiveMetrics.");

        // Step onto failed frame 1 -> MUST pause (IsPlaying=false), CLEAR ActiveFieldData & ActiveMetrics, surface failure + retry.
        series = ReplaySequence.AdvanceStep(series);
        Equal(1, series.CurrentIndex);
        Equal(false, series.IsPlaying);
        if (series.ActiveFieldData is not null)
            throw new InvalidOperationException("Failed sample must clear ActiveFieldData.");
        if (series.ActiveMetrics is not null)
            throw new InvalidOperationException("Failed sample must clear ActiveMetrics.");
        Equal("MeshGate failed: max non-orthogonality 78.2 deg", series.ActiveFailureReason);
        Equal(s1Fail.RunId, series.ActiveRetrySampleRunId);

        // Play on failed frame stays paused.
        series = ReplaySequence.Play(series);
        Equal(false, series.IsPlaying);

        // Step forward to admitted frame 2 restores data and keeps camera/seeds/slices pinned.
        series = ReplaySequence.AdvanceStep(series);
        Equal(2, series.CurrentIndex);
        Equal(camera, series.CameraState);
        Equal(-1.9, series.ActiveFieldData![0]);

        // ToggleRangeLock unlocks with explicit visible label.
        series = ReplaySequence.ToggleRangeLock(series);
        Equal(false, series.RangeLocked);
        Equal("Range unlocked — per-sample scale", series.RangeScaleLabel);

        // Reduced motion disables Play while keeping StepBackward & ScrubTo functional.
        series = ReplaySequence.SetReducedMotion(series, true);
        series = ReplaySequence.Play(series);
        Equal(false, series.IsPlaying);
        series = ReplaySequence.StepBackward(series);
        Equal(1, series.CurrentIndex);
        series = ReplaySequence.ScrubTo(series, 0);
        Equal(0, series.CurrentIndex);
        Equal(-0.8, series.ActiveFieldData![0]);

        // Out-of-bounds discrete frame refused (zero interpolation).
        Refuses("RES-FRAME-RANGE", () => ReplaySequence.SelectFrame(series, 3));
        Refuses("RES-FRAME-RANGE", () => ReplaySequence.SelectFrame(series, -1));
    }

    private static void DifferenceFlood_ZeroPinned_And_MeshHashMismatchUnavailable()
    {
        var baseSample = new SampleFieldData(
            RunId: Guid.NewGuid().ToString("D"),
            SampleLabel: "α 2.0° · V 7.0 m/s",
            MeshHash: MeshA,
            Variable: "Cp",
            Unit: "dimensionless",
            Association: "face",
            MethodTierAndVersion: "OpenFOAM ESI v2512 · simpleFoam (SA)",
            DepthBasis: "h_ref = 0.50 m",
            EvidenceLabel: "Completed",
            AssociationAndTimeBasis: "face · steady RANS (mean field)",
            Values: [-1.0, -0.5, 0.2, 0.8],
            Locations: ["η 0.1", "η 0.3", "η 0.6", "η 0.9"]);

        var targetSameMesh = baseSample with
        {
            RunId = Guid.NewGuid().ToString("D"),
            SampleLabel = "α 4.0° · V 7.0 m/s",
            Values = [-1.4, -0.4, 0.2, 1.0]
        };

        var diffOk = DifferenceFlood.ComputeDifference(baseSample, targetSameMesh, "Cp");
        Equal(true, diffOk.Available);
        Equal((string?)null, diffOk.UnavailableReason);
        Equal(4, diffOk.DeltaValues.Count);
        if (Math.Abs(diffOk.DeltaValues[0] - (-0.4)) > 1e-12) throw new InvalidOperationException($"Delta[0]={diffOk.DeltaValues[0]}");
        if (Math.Abs(diffOk.DeltaValues[1] - 0.1) > 1e-12) throw new InvalidOperationException($"Delta[1]={diffOk.DeltaValues[1]}");
        if (Math.Abs(diffOk.DeltaValues[2] - 0.0) > 1e-12) throw new InvalidOperationException($"Delta[2]={diffOk.DeltaValues[2]}");
        if (Math.Abs(diffOk.DeltaValues[3] - 0.2) > 1e-12) throw new InvalidOperationException($"Delta[3]={diffOk.DeltaValues[3]}");
        // Symmetric zero-pinned scale [-0.4, +0.4] on vik · Crameri 8.0.
        if (Math.Abs(diffOk.SymmetricAbsMax!.Value - 0.4) > 1e-12) throw new InvalidOperationException($"AbsMax={diffOk.SymmetricAbsMax}");
        if (Math.Abs(diffOk.ScaleMin!.Value - (-0.4)) > 1e-12) throw new InvalidOperationException($"ScaleMin={diffOk.ScaleMin}");
        if (Math.Abs(diffOk.ScaleMax!.Value - 0.4) > 1e-12) throw new InvalidOperationException($"ScaleMax={diffOk.ScaleMax}");
        Equal(true, ProvenanceStrip.HasAllNineFields(diffOk.Provenance!));
        Equal(ProvenanceStrip.VikCrameri, diffOk.Provenance!.ColormapNameAndVersion);
        if (!diffOk.Provenance.SourceRunAndSample.Contains(baseSample.RunId[..8], StringComparison.Ordinal) ||
            !diffOk.Provenance.SourceRunAndSample.Contains(targetSameMesh.RunId[..8], StringComparison.Ordinal))
            throw new InvalidOperationException("DifferenceFlood provenance must name both base and target runs.");

        // Mesh-hash mismatch HARD VETO -> strictly returns "Unavailable — mesh differs" and empty delta array.
        var targetOtherMesh = targetSameMesh with { MeshHash = MeshB };
        var diffBlocked = DifferenceFlood.ComputeDifference(baseSample, targetOtherMesh, "Cp");
        Equal(false, diffBlocked.Available);
        Equal("Unavailable — mesh differs", diffBlocked.UnavailableReason);
        Equal(0, diffBlocked.DeltaValues.Count);

        // Missing mesh hash also refused with "Unavailable — mesh differs".
        var targetNoMesh = targetSameMesh with { MeshHash = null };
        var diffNullMesh = DifferenceFlood.ComputeDifference(baseSample, targetNoMesh, "Cp");
        Equal(false, diffNullMesh.Available);
        Equal("Unavailable — mesh differs", diffNullMesh.UnavailableReason);

        // Layout mismatch refused with "Unavailable — field layout differs".
        var targetShort = targetSameMesh with { Values = [-1.4, -0.4] };
        var diffLayout = DifferenceFlood.ComputeDifference(baseSample, targetShort, "Cp");
        Equal(false, diffLayout.Available);
        Equal("Unavailable — field layout differs", diffLayout.UnavailableReason);
    }
}
