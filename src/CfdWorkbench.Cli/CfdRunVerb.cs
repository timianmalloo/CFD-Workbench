using System.Globalization;
using System.Text;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.CfdRun;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench cfd-run emit|status|harvest --json</c> (docs/design/cfd-run-pipeline.md §7.3, CLI-01).
/// Zero-PII JSON output with exact Core runKey/caseModelHash/meshHash/Converged-label parity.
/// </summary>
internal static class CfdRunVerb
{
    internal const string Usage =
        "Usage: cfd-workbench cfd-run emit <example|path.foil|path.cfdw.json> --backend openfoam|su2 --out <dir> [--alpha <deg>] [--speed <m/s>] [--json]\n" +
        "       cfd-workbench cfd-run status <path.cfdw.json> [--json]\n" +
        "       cfd-workbench cfd-run harvest <path.cfdw.json> --case <dir> --run-id <uuid> [--backend openfoam|su2] [--alpha <deg>] [--speed <m/s>] [--json]";

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation)
    {
        if (args.Length < 3)
        {
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string sub = args[1];
        string input = args[2];
        if (sub is not ("emit" or "status" or "harvest"))
        {
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string backendName = "openfoam";
        string? outDir = null;
        string? caseDir = null;
        string? runId = null;
        double alphaDeg = 3.0;
        double speedMs = 7.0;

        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--json") continue;
            if (i + 1 >= args.Length) { await output.WriteLineAsync(Usage); return 2; }
            switch (args[i])
            {
                case "--backend": backendName = args[++i].ToLowerInvariant(); break;
                case "--out": outDir = args[++i]; break;
                case "--case": caseDir = args[++i]; break;
                case "--run-id": runId = args[++i]; break;
                case "--alpha":
                    if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out alphaDeg))
                    { await output.WriteLineAsync("{\"code\":\"CFD-CASE-NONFINITE\"}"); return 2; }
                    break;
                case "--speed":
                    if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out speedMs))
                    { await output.WriteLineAsync("{\"code\":\"CFD-CASE-NONFINITE\"}"); return 2; }
                    break;
                default:
                    await output.WriteLineAsync(Usage);
                    return 2;
            }
        }

        if (backendName is not ("openfoam" or "su2"))
        {
            await output.WriteLineAsync("{\"code\":\"CFD-BACKEND-UNKNOWN\"}");
            return 2;
        }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (sub == "status")
                return await StatusAsync(input, output, cancellation);

            var backend = backendName == "su2" ? CfdBackendKind.Su2 : CfdBackendKind.OpenFoamEsi;
            if (sub == "emit")
            {
                if (string.IsNullOrWhiteSpace(outDir)) { await output.WriteLineAsync(Usage); return 2; }
                return await EmitAsync(input, backend, outDir, alphaDeg, speedMs, output, cancellation);
            }

            if (string.IsNullOrWhiteSpace(caseDir) || string.IsNullOrWhiteSpace(runId))
            {
                await output.WriteLineAsync(Usage);
                return 2;
            }
            return await HarvestAsync(input, backend, caseDir, runId, alphaDeg, speedMs, output, cancellation);
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error)
        {
            await Cli.WriteAsync(output, new { code = error.Code, reason = error.Reason });
            return Cli.ExitForCode(error.Code);
        }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    private static async Task<int> EmitAsync(
        string input,
        CfdBackendKind backend,
        string outDir,
        double alphaDeg,
        double speedMs,
        TextWriter output,
        CancellationToken cancellation)
    {
        using var session = await Cli.OpenAsync(input, cancellation);
        var model = BuildModelFromSession(session, backend, alphaDeg, speedMs);
        var files = backend == CfdBackendKind.OpenFoamEsi
            ? OpenFoamCaseEmitter.EmitCaseFiles(model)
            : Su2CaseEmitter.EmitConfigFiles(model);

        Directory.CreateDirectory(outDir);
        foreach (var (rel, content) in files)
        {
            string full = Path.Combine(outDir, rel.Replace('/', Path.DirectorySeparatorChar));
            string? parent = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            await File.WriteAllTextAsync(full, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellation);
        }

        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            backend = model.BackendPin.Backend,
            version = model.BackendPin.Version,
            runKey = model.ComputeCaseKey(),
            caseModelHash = model.ComputeCaseModelHash(),
            meshHash = model.ComputeMeshHash(),
            filesEmitted = files.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray()
        });
        return 0;
    }

    private static async Task<int> StatusAsync(string input, TextWriter output, CancellationToken cancellation)
    {
        if (!input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }
        using var session = await Cli.OpenAsync(input, cancellation);
        var ledger = session.ReadCfdLedger();
        var views = CfdRunProjection.ProjectLedger(ledger);
        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            runs = views.Select(v => new
            {
                runId = v.RunId,
                runKey = v.RunKey,
                meshHash = v.MeshHash,
                state = v.State.ToString(),
                stateDisplay = v.StateDisplay,
                integrity = v.Integrity.ToString(),
                convergedLabel = v.ConvergedLabel,
                meshGatePassed = v.MeshGatePassed,
                meshGateIsScreenOnly = v.MeshGateIsScreenOnly,
                residualSummary = v.ResidualSummary,
                backend = v.BackendLabel,
                canResume = v.CanResume
            }).ToArray(),
            fieldEvidenceCount = ledger.FieldEvidence.Count,
            candidateCount = ledger.Candidates.Count
        });
        return 0;
    }

    private static async Task<int> HarvestAsync(
        string input,
        CfdBackendKind backend,
        string caseDir,
        string runId,
        double alphaDeg,
        double speedMs,
        TextWriter output,
        CancellationToken cancellation)
    {
        using var session = await Cli.OpenAsync(input, cancellation);
        var model = BuildModelFromSession(session, backend, alphaDeg, speedMs);
        string meshHash = model.ComputeMeshHash();
        string runKey = model.ComputeCaseKey();
        string caseModelHash = model.ComputeCaseModelHash();

        var harvested = backend == CfdBackendKind.OpenFoamEsi
            ? CfdOutputHarvester.HarvestOpenFoam(caseDir, runId, meshHash, model)
            : CfdOutputHarvester.HarvestSu2(caseDir, runId, meshHash, model);

        var terminalState = harvested.ResidualSummary.CriterionMet
            ? CfdRunState.Completed
            : CfdRunState.CompletedResidualNotMet;
        string? convergedLabel = CfdRunStateMachine.FormatConvergedLabel(harvested.ResidualSummary, outputsComplete: true);

        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            runId,
            runKey,
            caseModelHash,
            meshHash,
            state = terminalState.ToString(),
            finalTimeDirectory = harvested.FinalTimeDirectory,
            filesParsed = harvested.FilesParsed,
            hasWallShearStress = harvested.HasWallShearStress,
            yPlusP95 = harvested.YPlusP95,
            yPlusMax = harvested.YPlusMax,
            residualSummary = harvested.ResidualSummary,
            convergedLabel,
            evidenceCount = harvested.FieldEvidence.Count
        });
        return 0;
    }

    private static TypedCaseModel BuildModelFromSession(
        AuthoringSession session,
        CfdBackendKind backend,
        double alphaDeg,
        double speedMs)
    {
        var snapshot = session.Snapshot();
        var parsed = FoilSource.Parse(snapshot.Source);
        var profiles = parsed.Authored().Assignments.Select(a => a.ProfileIdentity).ToArray();
        string evaluator = Evaluator.IdFor(snapshot.Source);
        var inputs = new RunInputs(snapshot.AcceptedId, snapshot.SurfaceHash, profiles, evaluator, "rule-a/1");
        var reference = new ProductWingMethod().Reference(snapshot.Source);
        double halfSpan = reference.BRef * 0.5;
        double rootChord = reference.CRef;
        var bounds = new WingBounds(0.0, rootChord, 0.0, halfSpan, -0.05 * rootChord, 0.05 * rootChord);
        var water = WaterTable.At(OperatingPoints.DefaultTemperatureC, OperatingPoints.SaltSalinityGPerKg);
        var op = OperatingPoints.Custom(speedMs, alphaDeg, hRef: 0.5);
        var pin = backend == CfdBackendKind.OpenFoamEsi
            ? new CfdBackendPin("openfoam-esi", "v2512", OperatingSystem.IsMacOS() ? "macos-app" : "wsl2", "_bd2b6720-20260127", new string('0', 64), true)
            : new CfdBackendPin("su2", "v8.5.0", OperatingSystem.IsMacOS() ? "macos-app" : "win-native", "su2-8.5.0", new string('0', 64), true);

        return new TypedCaseModel(inputs, reference, bounds, water, op, pin);
    }
}
