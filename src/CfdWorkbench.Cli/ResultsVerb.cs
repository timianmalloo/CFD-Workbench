using CfdWorkbench.Analysis.Results;
using CfdWorkbench.Core;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench results inspect|candidates|paraview --json</c> (docs/design/results-and-candidates.md §9.1, CLI-01).
/// Zero-PII JSON output with exact ResultsProjection, ProvenanceStrip, FieldEvidenceManifest, CandidateLadder, and ParaViewHandoff parity.
/// </summary>
internal static class ResultsVerb
{
    internal const string Usage =
        "Usage: cfd-workbench results inspect <path.cfdw.json> [--run-id <uuid>] [--json]\n" +
        "       cfd-workbench results candidates <path.cfdw.json> [--json]\n" +
        "       cfd-workbench results paraview <path.cfdw.json|case-dir> [--paraview <exe>] [--launch] [--json]";

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation)
    {
        if (args.Length < 3)
        {
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string sub = args[1];
        string input = args[2];
        if (sub is not ("inspect" or "candidates" or "paraview"))
        {
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string? runIdFilter = null;
        string? paraviewOverride = null;
        bool launch = false;

        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--json") continue;
            if (args[i] == "--launch") { launch = true; continue; }
            if (i + 1 >= args.Length) { await output.WriteLineAsync(Usage); return 2; }
            switch (args[i])
            {
                case "--run-id": runIdFilter = args[++i]; break;
                case "--paraview": paraviewOverride = args[++i]; break;
                default:
                    await output.WriteLineAsync(Usage);
                    return 2;
            }
        }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            return sub switch
            {
                "inspect" => await InspectAsync(input, runIdFilter, output, cancellation),
                "candidates" => await CandidatesAsync(input, output, cancellation),
                "paraview" => await ParaViewAsync(input, paraviewOverride, launch, output, cancellation),
                _ => 2
            };
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

    private static async Task<int> InspectAsync(
        string input,
        string? runIdFilter,
        TextWriter output,
        CancellationToken cancellation)
    {
        if (!input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }

        using var session = await Cli.OpenAsync(input, cancellation);
        var model = ResultsService.ProjectWorkspace(session);
        var samples = string.IsNullOrWhiteSpace(runIdFilter)
            ? model.Samples
            : model.Samples.Where(s => string.Equals(s.RunId, runIdFilter, StringComparison.Ordinal)).ToArray();

        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            currentAcceptedId = model.CurrentAcceptedId,
            currentSurfaceHash = model.CurrentSurfaceHash,
            noCrossTierAveragingNote = model.NoCrossTierAveragingNote,
            samples = samples.Select(s => new
            {
                runId = s.RunId,
                tier = s.Tier,
                sourceKind = s.SourceKind,
                alphaDeg = s.AlphaDeg,
                speedMs = s.SpeedMs,
                cl = s.Cl,
                cd = s.Cd,
                cm = s.Cm,
                meshHash = s.MeshHash,
                admitted = s.Admitted,
                statusLabel = s.StatusLabel,
                evidenceLabel = s.EvidenceLabel,
                provenance = s.Provenance,
                tableTwinRowCount = s.TableTwin.Rows.Count,
                manifest = new
                {
                    runId = s.Manifest.RunId,
                    sourceBasis = s.Manifest.SourceBasis,
                    layers = s.Manifest.Layers.Select(l => new
                    {
                        kind = l.Kind.ToString(),
                        available = l.Available,
                        colormapPolicy = l.ColormapPolicy,
                        basisLabel = l.BasisLabel,
                        unavailableReason = l.UnavailableReason,
                        separationCriterion = l.SeparationCriterion
                    }).ToArray()
                }
            }).ToArray(),
            smallMultipleCount = model.SmallMultiples.Count
        });
        return 0;
    }

    private static async Task<int> CandidatesAsync(
        string input,
        TextWriter output,
        CancellationToken cancellation)
    {
        if (!input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }

        using var session = await Cli.OpenAsync(input, cancellation);
        var model = ResultsService.ProjectWorkspace(session);
        var ladder = model.CandidateLadder;

        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            currentAcceptedId = model.CurrentAcceptedId,
            currentSurfaceHash = model.CurrentSurfaceHash,
            emptyReason = ladder.EmptyReason,
            paretoCandidateIds = ladder.ParetoCandidateIds,
            parallelAxes = ladder.ParallelAxes,
            candidates = ladder.Candidates
        });
        return 0;
    }

    private static async Task<int> ParaViewAsync(
        string input,
        string? paraviewOverride,
        bool launch,
        TextWriter output,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var availability = ParaViewHandoff.Prepare(explicitOverridePath: paraviewOverride);

        if (launch)
        {
            string caseDir = Directory.Exists(input)
                ? input
                : Path.GetDirectoryName(Path.GetFullPath(input)) ?? ".";
            var result = ParaViewHandoff.Launch(caseDir, explicitOverridePath: paraviewOverride);
            // Zero-PII JSON: omit raw host executable and user-profile paths.
            await Cli.WriteAsync(output, new
            {
                schemaVersion = 1,
                available = availability.Available,
                launched = result.Launched,
                message = result.Message,
                useShellExecute = result.UseShellExecute,
                createNoWindow = result.CreateNoWindow,
                vtkLinked = false
            });
            return result.Launched ? 0 : 3;
        }

        // Zero-PII availability report (exact UX-06 absence message when absent).
        await Cli.WriteAsync(output, new
        {
            schemaVersion = 1,
            available = availability.Available,
            launched = false,
            message = availability.Message,
            useShellExecute = false,
            createNoWindow = true,
            vtkLinked = false
        });
        return 0;
    }
}
