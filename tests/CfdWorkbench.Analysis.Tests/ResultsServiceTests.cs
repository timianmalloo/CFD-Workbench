using System.Diagnostics;
using CfdWorkbench.Analysis.Results;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Red-first Analysis tests for ResultsService, ResultsProjection, offline AI-10 validator,
/// and process-only ParaViewHandoff (docs/design/results-and-candidates.md §6, §9.1).
/// </summary>
internal static class ResultsServiceTests
{
    private sealed class FakeParaViewLocator(string? path) : IParaViewLocator
    {
        public string? FindExecutable(string? explicitOverridePath = null) => explicitOverridePath ?? path;
    }

    private sealed class RecordingStarter : IParaViewProcessStarter
    {
        public ProcessStartInfo? LastStartInfo { get; private set; }
        public int StartDetached(ProcessStartInfo startInfo)
        {
            LastStartInfo = startInfo;
            return 4242;
        }
    }

    internal static void Run()
    {
        Check("ResultsService_UnifiesLocalVlmAndCfd_NoCrossTierAveraging", ResultsService_UnifiesLocalVlmAndCfd_NoCrossTierAveraging);
        Check("ResultsService_TryReadSurfaceRaw_RejectsTraversalRootedAndSymlinkPaths", ResultsService_TryReadSurfaceRaw_RejectsTraversalRootedAndSymlinkPaths);
        Check("ParaViewHandoff_HonestAbsence_And_InjectedProcessOnlyLaunch", ParaViewHandoff_HonestAbsence_And_InjectedProcessOnlyLaunch);
    }

    private static void ResultsService_UnifiesLocalVlmAndCfd_NoCrossTierAveraging()
    {
        using var session = new AuthoringSession();
        byte[] source = FoilSource.NewDefault();
        session.Open(source, Guid.NewGuid().ToString("D"), true);
        var snap = session.Snapshot();

        // 1. Add one local VLM+strip run.
        var wingMethod = new ProductWingMethod();
        var analysisService = new AnalysisService(session, wingMethod);
        var water = WaterTable.At(15.0, 35.16504);
        var vlmRun = analysisService.EvaluateAsync(
            OperatingPoints.Custom(7.0, 4.0, 0.5),
            water,
            Tier.VlmStrip,
            new Scope.Wing(),
            CancellationToken.None).GetAwaiter().GetResult();

        // 2. Add one Area 5 CFD run + surface field evidence (with wallShearStress absent on run 1, present on run 2).
        var parsed = FoilSource.Parse(snap.Source);
        var profiles = parsed.Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, profiles, Evaluator.IdFor(snap.Source), Placement.PlacementRuleVersion);
        var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "_bd2b6720", new string('a', 64), true);
        var op = OperatingPoints.Custom(7.0, 4.0, 0.5);
        string caseModelHash = new string('b', 64);
        string meshHash = new string('c', 64);
        string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, caseModelHash);
        string cfdRunId = Guid.NewGuid().ToString("D");

        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(cfdRunId, now)];
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now);
        events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, now);

        var cfdRun = CfdRunRecords.Seal(new CfdRunRecord(
            RunId: cfdRunId,
            PriorRunId: null,
            ExperimentId: null,
            CaseIndex: 0,
            RunKey: runKey,
            ContentHash: "",
            CaseModelHash: caseModelHash,
            MeshHash: meshHash,
            BackendPin: pin,
            Inputs: inputs,
            Water: water,
            Op: op,
            Reference: wingMethod.Reference(snap.Source),
            MeshGate: null,
            ResidualSummary: new ResidualSummary(3.2e-4, 0.512, 0.0185, -0.042, 2e-4, 4e-5, 1e-3, 2e-4, 25, 0, false, true),
            Resources: new CfdRunResources(620_000, 4, 2400, 90, 512_000_000),
            Checkpoint: null,
            Events: events));
        session.RecordCfdRun(cfdRun);

        var evidence = CfdRunRecords.Seal(new FieldEvidenceRecord(
            EvidenceId: Guid.NewGuid().ToString("D"),
            RunId: cfdRunId,
            ContentHash: "",
            Kind: "surface",
            RelativePath: "postProcessing/wingSurface/500/wing.raw",
            ArtifactHash: new string('d', 64),
            MeshHash: meshHash,
            Fields: ["Cp", "Cf"],
            Association: "face",
            Ranges:
            [
                new FieldRangeRecord("Cp", "dimensionless", -1.84, 1.00, "sample-range"),
                new FieldRangeRecord("Cf", "dimensionless", 0.001, 0.008, "sample-range")
            ],
            ColormapPolicyVersion: "crameri-8.0",
            ReductionTool: "cfdw-harvester",
            ReductionToolVersion: "1.0.0",
            HasWallShearStress: false,
            SeparationCriterion: null,
            HasTurbulentKineticEnergy: false,
            IsTransient: false,
            Timesteps: []));
        session.RecordFieldEvidence(evidence);

        // 3. Project unified ResultsWorkspaceModel.
        var model = ResultsService.ProjectWorkspace(session, paraViewLocator: new FakeParaViewLocator(null));
        Equal(2, model.Samples.Count, "unified sample count");
        Equal(Labels.CompareFootnote, model.NoCrossTierAveragingNote);

        var vlmSample = model.Samples.Single(s => s.RunId == vlmRun.RunId);
        var cfdSample = model.Samples.Single(s => s.RunId == cfdRunId);

        Equal("vlm-strip", vlmSample.Tier);
        Equal("cfd", cfdSample.Tier);

        // CL values stay strictly per-tier (never averaged together!).
        Equal(0.512, cfdSample.Cl!.Value);
        if (vlmSample.Cl is null || Math.Abs(vlmSample.Cl.Value - cfdSample.Cl.Value) < 1e-6)
            throw new InvalidOperationException($"Expected distinct per-tier CL values; VLM={vlmSample.Cl}, CFD={cfdSample.Cl}.");

        // All 9 ProvenanceStrip fields populated on both samples.
        Equal(true, ProvenanceStrip.HasAllNineFields(vlmSample.Provenance));
        Equal(true, ProvenanceStrip.HasAllNineFields(cfdSample.Provenance));

        // Project surface Cp over Placement.Surface (ADR-0010).
        var surfaceProj = ResultsService.ProjectSurfaceField(session, cfdSample, "Cp");
        if (surfaceProj.Nodes.Count == 0)
            throw new InvalidOperationException("Surface field projection over Placement.Surface must produce placed nodes.");
        Equal(snap.SurfaceHash, surfaceProj.SurfaceHash);
        Equal(true, ProvenanceStrip.HasAllNineFields(surfaceProj.Provenance));
        Equal(surfaceProj.Nodes.Count, surfaceProj.TableTwin.Rows.Count);

        // Offline AI-10 AskAboutResult strictly refuses separation when tau_w is absent.
        var ask = ResultsService.AskAboutResult(cfdSample, "Does this wing show flow separation?");
        Equal(false, ask.SeparationSupported);
        Equal("No supported criterion in this sample", ask.Answer);

        // Difference flood between same-mesh samples succeeds; between differing meshes returns "Unavailable — mesh differs".
        var sameMeshDiff = ResultsService.ComputeDifferenceFlood(cfdSample, cfdSample, "Cp");
        Equal(true, sameMeshDiff.Available);
        Equal(0.0, sameMeshDiff.SymmetricAbsMax!.Value);
        var otherMeshSample = cfdSample with { SurfaceCpData = cfdSample.SurfaceCpData! with { MeshHash = new string('9', 64) } };
        var diffMeshBlocked = ResultsService.ComputeDifferenceFlood(cfdSample, otherMeshSample, "Cp");
        Equal(false, diffMeshBlocked.Available);
        Equal("Unavailable — mesh differs", diffMeshBlocked.UnavailableReason);

        // Replay series built via ResultsService honors compatible series and locks scalar range.
        var replay = ResultsService.BuildReplaySeries([cfdSample], water, "Cp", ReplayHeldAxis.HeldSpeed);
        Equal(1, replay.Frames.Count);
        Equal(true, replay.RangeLocked);
    }

    private static void ResultsService_TryReadSurfaceRaw_RejectsTraversalRootedAndSymlinkPaths()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "cfdw-res-m5-" + Guid.NewGuid().ToString("N"));
        string caseRoot = Path.Combine(tempRoot, "cases");
        string outsideDir = Path.Combine(tempRoot, "outside");
        string linkDir = Path.Combine(caseRoot, "linked");
        try
        {
            Directory.CreateDirectory(caseRoot);
            Directory.CreateDirectory(outsideDir);
            string secretRaw = Path.Combine(outsideDir, "secret.raw");
            File.WriteAllText(secretRaw, "# x y z Cp\n0.0 0.0 0.0 -99.5\n0.1 0.1 0.0 99.5\n");

            string safeDir = Path.Combine(caseRoot, "safe");
            Directory.CreateDirectory(safeDir);
            string safeRaw = Path.Combine(safeDir, "wing.raw");
            File.WriteAllText(safeRaw, "# x y z Cp\n0.0 0.0 0.0 -1.25\n0.1 0.1 0.0 0.75\n");

            UnifiedResultSample ProjectWithRelativePath(string relPath)
            {
                using var session = new AuthoringSession();
                session.Open(FoilSource.NewDefault(), Guid.NewGuid().ToString("D"), true);
                var snap = session.Snapshot();
                var parsed = FoilSource.Parse(snap.Source);
                var profiles = parsed.Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
                var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, profiles, Evaluator.IdFor(snap.Source), Placement.PlacementRuleVersion);
                var water = WaterTable.At(15.0, 35.16504);
                var op = OperatingPoints.Custom(7.0, 4.0, 0.5);
                var pin = new CfdBackendPin("openfoam-esi", "v2512", "wsl2", "build-1", new string('a', 64), true);
                string caseModelHash = new string('b', 64);
                string meshHash = new string('c', 64);
                string runKey = CfdRunRecords.ComputeRunKey(inputs, water, op, pin, caseModelHash);
                string runId = Guid.NewGuid().ToString("D");
                var now = DateTimeOffset.UtcNow;
                IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(runId, now)];
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, now);

                session.RecordCfdRun(CfdRunRecords.Seal(new CfdRunRecord(
                    runId, null, null, 0, runKey, "", caseModelHash, meshHash, pin, inputs, water, op,
                    new ProductWingMethod().Reference(snap.Source), null,
                    new ResidualSummary(3e-4, 0.5, 0.018, -0.04, 1e-4, 3e-5, 1e-3, 2e-4, 25, 0, false, true),
                    new CfdRunResources(500_000, 4, 1200, 45, 256_000_000), null, events)));

                session.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                    Guid.NewGuid().ToString("D"), runId, "", "surface", relPath,
                    new string('d', 64), meshHash, ["Cp"], "face",
                    [new FieldRangeRecord("Cp", "dimensionless", -1.5, 1.0, "sample-range")],
                    "crameri-8.0", "cfdw-harvester", "1.0.0", false, null, false, false, [])));

                return ResultsService.ProjectWorkspace(session, caseRootDirectory: caseRoot, paraViewLocator: new FakeParaViewLocator(null))
                    .Samples.Single();
            }

            // 1. Positive control: valid in-root relative path IS read from disk (-1.25, 0.75).
            var safeSample = ProjectWithRelativePath("safe/wing.raw");
            Equal(2, safeSample.SurfaceCpData!.Values.Count, "valid in-root .raw count");
            Equal(-1.25, safeSample.SurfaceCpData.Values[0], "valid in-root .raw value[0]");
            Equal(0.75, safeSample.SurfaceCpData.Values[1], "valid in-root .raw value[1]");

            // 2. '..' traversal outside caseRootDirectory MUST NOT be read (falls back to recorded metadata range, never -99.5).
            var traversalSample = ProjectWithRelativePath("../outside/secret.raw");
            if (traversalSample.SurfaceCpData!.Values.Any(v => Math.Abs(v - (-99.5)) < 1e-6))
                throw new InvalidOperationException("M5 violation: TryReadSurfaceRaw read a '..' traversal file outside caseRootDirectory.");

            // 3. Rooted absolute path targeting outside file MUST NOT be read.
            var rootedSample = ProjectWithRelativePath(secretRaw);
            if (rootedSample.SurfaceCpData!.Values.Any(v => Math.Abs(v - (-99.5)) < 1e-6))
                throw new InvalidOperationException("M5 violation: TryReadSurfaceRaw read a rooted path outside caseRootDirectory.");

            // 4. Directory junction / symlink inside caseRootDirectory targeting outsideDir MUST NOT be read.
            if (TestLinks.TryDirectoryLink(linkDir, outsideDir))
            {
                var symlinkSample = ProjectWithRelativePath("linked/secret.raw");
                if (symlinkSample.SurfaceCpData!.Values.Any(v => Math.Abs(v - (-99.5)) < 1e-6))
                    throw new InvalidOperationException("M5 violation: TryReadSurfaceRaw followed a symlink/junction outside caseRootDirectory.");
            }
        }
        finally
        {
            if (Directory.Exists(linkDir)) Directory.Delete(linkDir, recursive: false);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void ParaViewHandoff_HonestAbsence_And_InjectedProcessOnlyLaunch()
    {
        // 1. Honest absence message matches spec UX-06 verbatim when ParaView 5.12+ is absent.
        var absent = ParaViewHandoff.Prepare(new FakeParaViewLocator(null));
        Equal(false, absent.Available);
        Equal(Labels.ParaViewNotFound, absent.Message);
        Equal(
            "ParaView 5.12+ not found — surface floods and forces remain; slices, streamlines and isosurfaces need ParaView (BSD-3) · Open in ParaView disabled",
            absent.Message);

        // 2. Zero VTK references in CfdWorkbench.Analysis assembly.
        var refs = typeof(ParaViewHandoff).Assembly.GetReferencedAssemblies();
        if (refs.Any(r => r.Name is not null && (r.Name.Contains("vtk", StringComparison.OrdinalIgnoreCase) || r.Name.Contains("Kitware", StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("CfdWorkbench.Analysis must never link VTK or ParaView native libraries.");

        // 3. Injected valid executable + real temp case dir -> argv-only launch (UseShellExecute=false, CreateNoWindow=true).
        string tempRoot = Path.Combine(Path.GetTempPath(), "cfdw-pv-test-" + Guid.NewGuid().ToString("N"));
        string caseDir = Path.Combine(tempRoot, "case-01");
        string fakeExe = Path.Combine(tempRoot, OperatingSystem.IsWindows() ? "paraview-probe.exe" : "paraview-probe");
        try
        {
            Directory.CreateDirectory(caseDir);
            File.WriteAllText(fakeExe, "probe");

            var recorder = new RecordingStarter();
            var launched = ParaViewHandoff.Launch(caseDir, new FakeParaViewLocator(fakeExe), starter: recorder);
            Equal(true, launched.Launched);
            Equal(false, launched.UseShellExecute);
            Equal(true, launched.CreateNoWindow);
            if (recorder.LastStartInfo is null)
                throw new InvalidOperationException("Expected ProcessStartInfo to be passed to starter.");
            Equal(false, recorder.LastStartInfo.UseShellExecute);
            Equal(true, recorder.LastStartInfo.CreateNoWindow);
            Equal(1, recorder.LastStartInfo.ArgumentList.Count);
            Equal(Path.GetFullPath(caseDir), recorder.LastStartInfo.ArgumentList[0]);
            Equal("", recorder.LastStartInfo.Arguments);

            // 4. Absent ParaView returns Launched=false with honest absence string.
            var noLaunch = ParaViewHandoff.Launch(caseDir, new FakeParaViewLocator(null), starter: recorder);
            Equal(false, noLaunch.Launched);
            Equal(Labels.ParaViewNotFound, noLaunch.Message);

            // 5. Path traversal refused.
            bool traversalRefused = false;
            try
            {
                ParaViewHandoff.Launch(Path.Combine(caseDir, "..", "escape"), new FakeParaViewLocator(fakeExe), starter: recorder);
            }
            catch (ContractError ce) when (ce.Code == "RES-PARAVIEW-PATH")
            {
                traversalRefused = true;
            }
            Equal(true, traversalRefused, "traversal refusal");

            // 6. Symlink / junction case directory refused (M5).
            string linkPath = Path.Combine(tempRoot, "case-link");
            try
            {
                if (TestLinks.TryDirectoryLink(linkPath, caseDir))
                {
                    bool symlinkRefused = false;
                    try
                    {
                        ParaViewHandoff.Launch(linkPath, new FakeParaViewLocator(fakeExe), starter: recorder);
                    }
                    catch (ContractError ce) when (ce.Code == "RES-PARAVIEW-SYMLINK")
                    {
                        symlinkRefused = true;
                    }
                    Equal(true, symlinkRefused, "symlink refusal");
                }
            }
            finally
            {
                if (Directory.Exists(linkPath)) Directory.Delete(linkPath, recursive: false);
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }
}
