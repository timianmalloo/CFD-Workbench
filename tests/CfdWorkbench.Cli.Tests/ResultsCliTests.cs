using System.Text.Json;
using CfdWorkbench.Analysis;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;

/// <summary>
/// Red-first CLI tests for `cfd-workbench results inspect|candidates|paraview --json`
/// (docs/design/results-and-candidates.md §9.1, CLI-01 zero-PII parity).
/// </summary>
internal static class ResultsCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunCheckAsync("Cli_Results_InspectCandidatesParaView_JsonParity", InspectCandidatesParaView_JsonParityAsync);
        return failures;
    }

    private static async Task<int> RunCheckAsync(string name, Func<Task> body)
    {
        try
        {
            await body();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {name} {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static async Task InspectCandidatesParaView_JsonParityAsync()
    {
        string tempRoot = TestTemp.NewDirectory("results-cli-");
        string projPath = Path.Combine(tempRoot, "wing.cfdw.json");
        try
        {
            string cfdRunId = Guid.NewGuid().ToString("D");
            string candId = Guid.NewGuid().ToString("D");
            using (var session = new AuthoringSession())
            {
                session.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
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
                var now = DateTimeOffset.UtcNow;
                IReadOnlyList<CfdRunStateEvent> events = [CfdRunStateMachine.CreatePending(cfdRunId, now)];
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Readiness, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Meshing, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.MeshGate, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Solving, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Harvesting, now);
                events = CfdRunStateMachine.AppendEvent(events, CfdRunState.Completed, now);

                session.RecordCfdRun(CfdRunRecords.Seal(new CfdRunRecord(
                    cfdRunId, null, null, 0, runKey, "", caseModelHash, meshHash, pin, inputs, water, op,
                    new ProductWingMethod().Reference(snap.Source), null,
                    new ResidualSummary(3e-4, 0.48, 0.018, -0.04, 1e-4, 3e-5, 1e-3, 2e-4, 25, 0, false, true),
                    new CfdRunResources(500_000, 4, 1500, 60, 256_000_000), null, events)));

                session.RecordFieldEvidence(CfdRunRecords.Seal(new FieldEvidenceRecord(
                    Guid.NewGuid().ToString("D"), cfdRunId, "", "surface", "postProcessing/wingSurface/500/wing.raw",
                    new string('d', 64), meshHash, ["Cp"], "face",
                    [new FieldRangeRecord("Cp", "dimensionless", -1.6, 1.0, "sample-range")],
                    "crameri-8.0", "cfdw-harvester", "1.0.0", false, null, false, false, [])));

                session.RecordCandidate(CfdRunRecords.Seal(new CandidateRecord(
                    candId, null, "", Guid.NewGuid().ToString("D"), "eval-1", snap.AcceptedId, snap.SurfaceHash, null,
                    "vlm-checked", true, "vlm-strip",
                    [new CandidateObjectiveRecord("L/D", "dimensionless", 26.5), new CandidateObjectiveRecord("CD", "dimensionless", 0.017)],
                    [new CandidateConstraintRecord("cavitation_margin", "%", 19.0, 15.0, true)],
                    [], [new string('e', 64)],
                    [new CandidateParameterDelta("twist", "cv-2", -0.5, "deg")])));

                await File.WriteAllBytesAsync(projPath, session.SaveImage());
            }

            // 1. `results inspect <project> --json`
            var output = new StringWriter();
            int exit = await Cli.RunAsync(["results", "inspect", projPath, "--json"], output);
            if (exit != 0) throw new Exception($"results inspect returned {exit}: {output}");
            using (var doc = JsonDocument.Parse(output.ToString()))
            {
                var root = doc.RootElement;
                var samples = root.GetProperty("samples");
                if (samples.GetArrayLength() != 1) throw new Exception($"expected 1 sample, got {samples.GetArrayLength()}");
                var s0 = samples[0];
                if (s0.GetProperty("runId").GetString() != cfdRunId) throw new Exception("runId mismatch");
                var prov = s0.GetProperty("provenance");
                foreach (string field in new[]
                {
                    "variable", "unit", "rangeWithBasis", "methodTierAndVersion",
                    "sourceRunAndSample", "depthBasis", "evidenceLabel",
                    "colormapNameAndVersion", "associationAndTimeBasis"
                })
                {
                    if (!prov.TryGetProperty(field, out var prop) || string.IsNullOrWhiteSpace(prop.GetString()))
                        throw new Exception($"Missing or empty provenance field '{field}'");
                }
                var sepLayer = s0.GetProperty("manifest").GetProperty("layers").EnumerateArray()
                    .Single(l => l.GetProperty("kind").GetString() == "Separation");
                if (sepLayer.GetProperty("available").GetBoolean())
                    throw new Exception("Separation must be unavailable without wallShearStress");
                if (sepLayer.GetProperty("unavailableReason").GetString() != FieldEvidenceManifest.SeparationUnavailableReason)
                    throw new Exception("Unexpected separation unavailable reason");
            }

            // 2. `results candidates <project> --json`
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["results", "candidates", projPath, "--json"], output);
            if (exit != 0) throw new Exception($"results candidates returned {exit}: {output}");
            using (var candDoc = JsonDocument.Parse(output.ToString()))
            {
                var root = candDoc.RootElement;
                var cands = root.GetProperty("candidates");
                if (cands.GetArrayLength() != 1) throw new Exception($"expected 1 candidate, got {cands.GetArrayLength()}");
                if (cands[0].GetProperty("candidateId").GetString() != candId) throw new Exception("candidateId mismatch");
                if (!cands[0].GetProperty("acceptEnabled").GetBoolean()) throw new Exception("expected acceptEnabled=true");
            }

            // 3. `results paraview <project> --json` (honest absence on host without ParaView; zero PII paths)
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["results", "paraview", projPath, "--json"], output);
            if (exit != 0) throw new Exception($"results paraview returned {exit}: {output}");
            string pvText = output.ToString();
            using (var pvDoc = JsonDocument.Parse(pvText))
            {
                var root = pvDoc.RootElement;
                bool available = root.GetProperty("available").GetBoolean();
                string msg = root.GetProperty("message").GetString() ?? "";
                if (!available && msg != Labels.ParaViewNotFound)
                    throw new Exception($"Unexpected ParaView absence message: {msg}");
                if (pvText.Contains("Users", StringComparison.Ordinal) || pvText.Contains("C:\\", StringComparison.Ordinal))
                    throw new Exception($"ParaView JSON output leaked machine path: {pvText}");
            }

            // 4. Invalid subcommand returns exit 2.
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["results", "bogus"], output);
            if (exit != 2) throw new Exception($"expected exit 2 for invalid subcommand, got {exit}");
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }
}
