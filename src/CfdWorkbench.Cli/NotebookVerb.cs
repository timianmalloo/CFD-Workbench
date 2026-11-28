using CfdWorkbench.Core;
using CfdWorkbench.Core.Notebook;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench notebook inspect|validate &lt;path.cfdw.json&gt; [--json]</c>
/// Offline-capable CLI lens for Wave 13B AI-Guided Design Notebook sessions persisted in <c>Envelope.notebook</c>
/// (<c>docs/design/ai-guided-design-notebook.md §1.2</c>).
/// </summary>
public static class NotebookVerb
{
    public const string Usage =
        "Usage: cfd-workbench notebook inspect|validate <path.cfdw.json> [--json]";

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);

        if (args.Length < 3)
        {
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        string sub = args[1];
        if (sub is not ("inspect" or "validate"))
        {
            await output.WriteLineAsync($"Error NB-USAGE: unknown subcommand '{sub}'.").ConfigureAwait(false);
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        string path = args[2];
        bool json = false;
        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--json") { json = true; continue; }
            await output.WriteLineAsync($"Error NB-USAGE: unknown option '{args[i]}'.").ConfigureAwait(false);
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        if (!path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            if (json) await Cli.WriteAsync(output, new { schemaVersion = 1, code = "DOC-TYPE" }).ConfigureAwait(false);
            else await output.WriteLineAsync("Error DOC-TYPE: notebook inspect|validate requires a .cfdw.json project file.").ConfigureAwait(false);
            return 2;
        }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            using var session = new AuthoringSession();
            using var store = new ProjectStore(session);
            var read = await store.ReadAsync(path, cancellation).ConfigureAwait(false);
            session.Reopen(read.Image);

            var nb = session.ReadNotebookSession();
            if (nb is null)
            {
                var emptyPayload = new
                {
                    schemaVersion = 1,
                    hasNotebook = false,
                    code = "NB-ABSENT",
                    message = "Project contains no AI-guided design notebook session."
                };
                if (json) await Cli.WriteAsync(output, emptyPayload).ConfigureAwait(false);
                else await output.WriteLineAsync("Notebook: none attached to this project.").ConfigureAwait(false);
                return sub == "validate" ? 3 : 0;
            }

            string? latestReqId = nb.Requirements.Count > 0 ? nb.Requirements[^1].ReqVersionId : null;
            var reqSummaries = new List<object>(nb.Requirements.Count);
            int blockingErrors = 0;
            int warnings = 0;

            foreach (var r in nb.Requirements)
            {
                string dsl = NotebookRecords.JoinChunks(r.DslLines);
                var parsed = FoilReqParser.Parse(dsl);
                var val = parsed.IsValid && parsed.Document is not null ? FoilReqValidator.Validate(parsed.Document) : null;
                if (!parsed.IsValid || val is { HasBlockingError: true }) blockingErrors++;
                if (val is not null) warnings += val.Findings.Count(f => f.Severity == "warning");

                reqSummaries.Add(new
                {
                    reqVersionId = r.ReqVersionId,
                    ordinalLabel = r.OrdinalLabel,
                    contentHash = r.ContentHash,
                    parseValid = parsed.IsValid,
                    findingCount = val?.Findings.Count ?? 0,
                    unitFlagged = val?.Findings.Any(f => f.Code == FoilReqValidator.UnitSanityCode) ?? false,
                    physicsTension = val?.Findings.Any(f => f.Code == FoilReqValidator.PhysicsTensionCode) ?? false
                });
            }

            var candidateSummaries = new List<object>(nb.Candidates.Count);
            int rebuildMismatches = 0;
            foreach (var c in nb.Candidates)
            {
                bool rebuiltMatches = false;
                if (string.Equals(c.GeometryStatus, nameof(GeometryStatus.Certified), StringComparison.Ordinal))
                {
                    byte[] rebuilt = NotebookRecords.MaterializeCandidateDsl(c.Delta);
                    var rebuiltParse = FoilSource.Parse(rebuilt);
                    rebuiltMatches = rebuiltParse.IsParsed && rebuiltParse.SurfaceHash == c.SurfaceHash;
                    if (!rebuiltMatches) rebuildMismatches++;
                }

                bool isStale = latestReqId is not null && GrillTreeEngine.IsCandidateStale(c, latestReqId);
                candidateSummaries.Add(new
                {
                    candidateId = c.CandidateId,
                    ordinal = c.Ordinal,
                    reqVersionId = c.ReqVersionId,
                    isStale,
                    geometryStatus = c.GeometryStatus,
                    refusalCode = c.RefusalCode,
                    surfaceHash = c.SurfaceHash,
                    rebuildVerified = rebuiltMatches,
                    spanMm = Math.Round(c.Delta.SpanMeters * 1000.0, 2),
                    areaCm2 = Math.Round(c.Delta.AreaSquareMeters * 1e4, 1),
                    aspectRatio = Math.Round(c.Delta.AspectRatio, 2),
                    rootProfileId = c.Delta.RootProfileId,
                    tipProfileId = c.Delta.TipProfileId,
                    operatingPointCount = c.OperatingPoints.Count,
                    repairAttemptIndex = c.RepairAttemptIndex
                });
            }

            bool allValid = blockingErrors == 0 && rebuildMismatches == 0 && NotebookRecords.VerifySessionHash(nb);

            var payload = new
            {
                schemaVersion = 1,
                command = sub,
                hasNotebook = true,
                valid = allValid,
                sessionId = nb.SessionId,
                title = nb.Title,
                createdUtc = nb.CreatedUtc,
                contentHash = nb.ContentHash,
                requirementCount = nb.Requirements.Count,
                turnCount = nb.Turns.Count,
                candidateCount = nb.Candidates.Count,
                handoffCount = nb.Handoffs.Count,
                blockingErrors,
                warnings,
                rebuildMismatches,
                requirements = reqSummaries,
                candidates = candidateSummaries,
                handoffs = nb.Handoffs.Select(h => new
                {
                    handoffId = h.HandoffId,
                    candidateId = h.CandidateId,
                    surfaceHash = h.SurfaceHash,
                    seededBriefId = h.SeededBriefId,
                    seededGoalStateId = h.SeededGoalStateId,
                    seededAcceptedId = h.SeededAcceptedId,
                    handedOffUtc = h.HandedOffUtc
                }).ToArray()
            };

            if (json) await Cli.WriteAsync(output, payload).ConfigureAwait(false);
            else await output.WriteLineAsync(
                $"Notebook ({nb.Title}): {(allValid ? "valid" : "invalid")} · {nb.Requirements.Count} reqs · {nb.Turns.Count} turns · {nb.Candidates.Count} candidates · {nb.Handoffs.Count} handoffs").ConfigureAwait(false);

            return allValid ? 0 : 3;
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}").ConfigureAwait(false);
            return 130;
        }
        catch (ContractError error)
        {
            if (json) await Cli.WriteAsync(output, new { schemaVersion = 1, code = error.Code, reason = error.Reason }).ConfigureAwait(false);
            else await output.WriteLineAsync($"Error {error.Code}: {error.Reason ?? error.Message}").ConfigureAwait(false);
            return Cli.ExitForCode(error.Code);
        }
        catch (IOException)
        {
            await output.WriteLineAsync("{\"code\":\"DOC-IO\"}").ConfigureAwait(false);
            return 5;
        }
        catch (UnauthorizedAccessException)
        {
            await output.WriteLineAsync("{\"code\":\"DOC-IO\"}").ConfigureAwait(false);
            return 5;
        }
    }
}
