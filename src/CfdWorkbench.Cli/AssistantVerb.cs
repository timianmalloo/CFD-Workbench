using System.Text;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Persistence;
using CfdWorkbench.Persistence.Assistant;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench assistant status|eval|preview|audit [options] [--store &lt;dir&gt;] [--json]</c>
/// Offline-capable BYOK assistant CLI (docs/design/byok-assistants.md §3.4). Never prints API keys.
/// </summary>
public static class AssistantVerb
{
    public const string Usage =
        "Usage: cfd-workbench assistant status|eval|preview|audit [options] [--store <dir>] [--json]";

    private const int MaxInputExcerptBytes = 65_536;

    public static Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation) =>
        RunAsync(args, output, new OsCredentialStore(), cancellation);

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        ICredentialStore credentials,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(credentials);

        if (args.Length < 2)
        {
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        string sub = args[1];
        if (sub is not ("status" or "eval" or "preview" or "audit"))
        {
            await output.WriteLineAsync($"Error ASSISTANT-USAGE: unknown subcommand '{sub}'.").ConfigureAwait(false);
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        string? storeDir = null;
        bool json = false;
        bool live = false;
        string? modelArg = null;
        string? areaArg = null;
        string? promptArg = null;
        string? inputPath = null;
        int limit = 50;

        for (int i = 2; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--json") { json = true; continue; }
            if (arg == "--live" && sub == "eval") { live = true; continue; }
            if (arg == "--model" && sub == "eval" && i + 1 < args.Length) { modelArg = args[++i]; continue; }
            if (arg == "--store" && i + 1 < args.Length) { storeDir = args[++i]; continue; }
            if (arg == "--area" && sub == "preview" && i + 1 < args.Length) { areaArg = args[++i]; continue; }
            if (arg == "--prompt" && sub == "preview" && i + 1 < args.Length) { promptArg = args[++i]; continue; }
            if (arg == "--input" && sub == "preview" && i + 1 < args.Length) { inputPath = args[++i]; continue; }
            if (arg == "--limit" && sub == "audit" && i + 1 < args.Length)
            {
                if (!int.TryParse(args[++i], out limit) || limit < 1 || limit > 1000)
                {
                    await output.WriteLineAsync("Error ASSISTANT-USAGE: --limit must be an integer between 1 and 1000.").ConfigureAwait(false);
                    return 2;
                }
                continue;
            }

            await output.WriteLineAsync($"Error ASSISTANT-USAGE: unknown or incomplete option '{arg}'.").ConfigureAwait(false);
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        string resolvedStore = string.IsNullOrWhiteSpace(storeDir) ? DefaultStoreDirectory() : storeDir;
        var settingsStore = new AssistantSettingsStore(resolvedStore);
        using var transport = new ByokLlmTransport();
        var service = new AssistantService(credentials, settingsStore, transport, KnowledgeRetriever.LoadEmbedded());

        // Production telemetry subscriber (satisfies check-event-subscribers.py and records session-local counts).
        int telemetryCount = 0;
        service.TelemetryEmitted += _ => Interlocked.Increment(ref telemetryCount);

        try
        {
            cancellation.ThrowIfCancellationRequested();

            if (sub == "status")
            {
                var status = await service.GetStatusAsync(cancellation).ConfigureAwait(false);
                var payload = new
                {
                    schemaVersion = 1,
                    credentialStoreSupported = status.CredentialStoreSupported,
                    hasApiKey = status.HasApiKey,
                    settingsOutcome = status.SettingsOutcome,
                    settingsCodes = status.SettingsCodes,
                    neverWrite = status.NeverWrite,
                    providerKind = status.Settings.ProviderKind.ToString(),
                    endpointOrigin = AssistantEvalSuite.CanonicalizeEndpointOrigin(status.Settings.EndpointUrl),
                    modelId = status.Settings.ModelId,
                    optionalFastModelId = status.Settings.OptionalFastModelId,
                    perRequestMaxTokens = status.Settings.PerRequestMaxTokens,
                    dailyRequestCap = status.Settings.DailyRequestCap,
                    dailyRequestsUsedUtc = status.DailyRequestsUsedUtc,
                    timeoutSeconds = status.Settings.TimeoutSeconds,
                    evalThresholdsVersion = status.EvalThresholdsVersion,
                    eligibility = new
                    {
                        proposalsEnabled = status.Eligibility.ProposalsEnabled,
                        statusLabel = status.Eligibility.StatusLabel,
                        failingItemIds = status.Eligibility.FailingItemIds,
                        receiptId = status.Eligibility.MatchingReceipt?.ReceiptId,
                        evaluatedAtUtc = status.Eligibility.MatchingReceipt?.EvaluatedAtUtc
                    },
                    knowledge = new
                    {
                        available = status.KnowledgeAvailable,
                        reason = status.KnowledgeUnavailableReason,
                        entryCount = status.KnowledgeEntryCount,
                        sha256 = status.KnowledgeSha256
                    }
                };

                if (json) await Cli.WriteAsync(output, payload).ConfigureAwait(false);
                else await output.WriteLineAsync($"Assistant: key={(status.HasApiKey ? "set" : "none")} · model={status.Settings.ModelId} · {status.Eligibility.StatusLabel} · today={status.DailyRequestsUsedUtc}/{status.Settings.DailyRequestCap}").ConfigureAwait(false);
                return status.NeverWrite ? 3 : 0;
            }

            if (sub == "eval")
            {
                if (live && !credentials.HasKey((await settingsStore.LoadSettingsAsync(cancellation).ConfigureAwait(false)).Settings.ProviderKind))
                {
                    var blocked = new { schemaVersion = 1, code = "ASSISTANT-NO-KEY", message = Labels.AssistantNoKey };
                    if (json) await Cli.WriteAsync(output, blocked).ConfigureAwait(false);
                    else await output.WriteLineAsync(Labels.AssistantNoKey).ConfigureAwait(false);
                    return 3;
                }

                var evalRes = await service.RunEvaluationAsync(liveWire: live, targetModelId: modelArg, ct: cancellation).ConfigureAwait(false);
                var payload = new
                {
                    schemaVersion = 1,
                    liveWire = evalRes.LiveWire,
                    saveOutcome = evalRes.SaveResult.Outcome,
                    saveSucceeded = evalRes.SaveResult.Succeeded,
                    eligibility = new
                    {
                        proposalsEnabled = evalRes.Eligibility.ProposalsEnabled,
                        statusLabel = evalRes.Eligibility.StatusLabel,
                        failingItemIds = evalRes.Eligibility.FailingItemIds
                    },
                    receipt = new
                    {
                        receiptId = evalRes.Receipt.ReceiptId,
                        providerKind = evalRes.Receipt.ProviderKind.ToString(),
                        endpointOrigin = evalRes.Receipt.EndpointOrigin,
                        modelId = evalRes.Receipt.ModelId,
                        evalThresholdsVersion = evalRes.Receipt.EvalThresholdsVersion,
                        evaluatedAtUtc = evalRes.Receipt.EvaluatedAtUtc,
                        allActiveItemsMetThreshold = evalRes.Receipt.AllActiveItemsMetThreshold,
                        contentHash = evalRes.Receipt.ContentHash,
                        items = evalRes.Receipt.Items.Select(i => new
                        {
                            itemId = i.ItemId,
                            title = i.Title,
                            passedCases = i.PassedCases,
                            totalCases = i.TotalCases,
                            passRate = i.PassRate,
                            threshold = i.Threshold,
                            reserved = i.Reserved,
                            metThreshold = i.MetThreshold
                        }).ToArray()
                    }
                };

                int activeCount = evalRes.Receipt.Items.Count(i => !i.Reserved);
                int activeMet = evalRes.Receipt.Items.Count(i => !i.Reserved && i.MetThreshold);
                if (json) await Cli.WriteAsync(output, payload).ConfigureAwait(false);
                else await output.WriteLineAsync($"Eval ({evalRes.Receipt.ModelId}, v{evalRes.Receipt.EvalThresholdsVersion}): {evalRes.Eligibility.StatusLabel} ({activeMet}/{activeCount} active met)").ConfigureAwait(false);
                return evalRes.SaveResult.Succeeded ? 0 : 3;
            }

            if (sub == "preview")
            {
                if (string.IsNullOrWhiteSpace(areaArg) || !TryParseArea(areaArg, out var area, out var defaultKind))
                {
                    await output.WriteLineAsync("Error ASSISTANT-USAGE: --area must be one of setup|cad|analysis|experiment|run|solver-setup|results|export|notebook.").ConfigureAwait(false);
                    return 2;
                }

                if (area == AssistantArea.Export)
                {
                    var refused = new { schemaVersion = 1, area = "Export", code = "ASSISTANT-EXPORT-NONE", message = AssistantValidators.ExportNoAssistantMessage };
                    if (json) await Cli.WriteAsync(output, refused).ConfigureAwait(false);
                    else await output.WriteLineAsync(AssistantValidators.ExportNoAssistantMessage).ConfigureAwait(false);
                    return 3;
                }

                if (string.IsNullOrWhiteSpace(promptArg))
                {
                    await output.WriteLineAsync("Error ASSISTANT-USAGE: --prompt <text> is required for assistant preview.").ConfigureAwait(false);
                    return 2;
                }

                var excerpts = new List<UntrustedExcerpt>();
                if (!string.IsNullOrWhiteSpace(inputPath))
                {
                    string excerptText = await ReadBoundedExcerptTextAsync(inputPath, cancellation).ConfigureAwait(false);
                    excerpts.Add(new UntrustedExcerpt("project-note", "input:1", excerptText));
                }

                var status = await service.GetStatusAsync(cancellation).ConfigureAwait(false);
                var keyRes = credentials.ReadKey(status.Settings.ProviderKind);
                var redaction = new RedactionContext(
                    ActiveApiKey: keyRes.Succeeded ? keyRes.Secret : null,
                    AdditionalSecrets: [],
                    DrcAttributions: [],
                    KnownUserNames: BuildKnownTokens(Environment.UserName),
                    KnownHostNames: BuildKnownTokens(Environment.MachineName));

                var previewReq = new AssistantPreviewRequest(
                    Area: area,
                    ProposalKind: defaultKind,
                    SystemPrompt: $"CFD-Workbench {area} assistant — return strict JSON matching the area schema.",
                    UserPrompt: promptArg,
                    Excerpts: excerpts,
                    KnowledgeQuery: promptArg);

                var prepared = service.BuildOfflinePreview(previewReq, status.Settings, redaction);
                var payload = new
                {
                    schemaVersion = 1,
                    requestId = prepared.RequestId,
                    area = prepared.Area.ToString(),
                    proposalKind = prepared.ProposalKind.ToString(),
                    endpointOrigin = prepared.EndpointOrigin,
                    modelId = prepared.SettingsSnapshot.ModelId,
                    hasApiKey = status.HasApiKey,
                    proposalsEnabled = status.Eligibility.ProposalsEnabled,
                    eligibilityStatus = status.Eligibility.StatusLabel,
                    promptUtf8Bytes = prepared.Payload.PromptUtf8Bytes,
                    maxPromptUtf8Bytes = PromptRedactor.MaxPromptUtf8Bytes,
                    payloadSha256 = prepared.Payload.PayloadSha256,
                    canonicalWirePreview = prepared.Payload.CanonicalWirePreview,
                    knowledgeAvailable = prepared.KnowledgeAvailable,
                    knowledgeIds = prepared.KnowledgeIds,
                    summary = new
                    {
                        pathsRedacted = prepared.Payload.Summary.PathsRedacted,
                        userNamesRedacted = prepared.Payload.Summary.UserNamesRedacted,
                        hostNamesRedacted = prepared.Payload.Summary.HostNamesRedacted,
                        drcAttributionsRedacted = prepared.Payload.Summary.DrcAttributionsRedacted,
                        secretsRedacted = prepared.Payload.Summary.SecretsRedacted,
                        delimitersNeutralized = prepared.Payload.Summary.DelimitersNeutralized,
                        truncatedExcerptIds = prepared.Payload.Summary.TruncatedExcerptIds,
                        promptTruncated = prepared.Payload.Summary.PromptTruncated
                    }
                };

                if (json) await Cli.WriteAsync(output, payload).ConfigureAwait(false);
                else await output.WriteLineAsync($"Preview {prepared.Area} ({prepared.Payload.PromptUtf8Bytes} B, sha256={prepared.Payload.PayloadSha256[..12]}): {prepared.Payload.Summary.PathsRedacted} paths, {prepared.Payload.Summary.SecretsRedacted} secrets redacted").ConfigureAwait(false);
                return 0;
            }

            // sub == "audit"
            var auditEntries = await settingsStore.ReadAuditEntriesAsync(limit, cancellation).ConfigureAwait(false);
            var auditPayload = new
            {
                schemaVersion = 1,
                count = auditEntries.Count,
                telemetryEventsInSession = Volatile.Read(ref telemetryCount),
                entries = auditEntries.Select(e => new
                {
                    requestId = e.RequestId,
                    timestampUtc = e.TimestampUtc,
                    area = e.Area.ToString(),
                    proposalKind = e.ProposalKind.ToString(),
                    providerKind = e.ProviderKind.ToString(),
                    endpointOrigin = e.EndpointOrigin,
                    modelId = e.ModelId,
                    payloadSha256 = e.PayloadSha256,
                    promptUtf8Bytes = e.PromptUtf8Bytes,
                    consentAction = e.ConsentAction,
                    verdict = e.Verdict,
                    usageInputTokens = e.UsageInputTokens,
                    usageInputTokensDisplay = e.UsageInputTokens?.ToString() ?? Labels.AssistantNotRecorded,
                    usageOutputTokens = e.UsageOutputTokens,
                    usageOutputTokensDisplay = e.UsageOutputTokens?.ToString() ?? Labels.AssistantNotRecorded,
                    durationMs = e.DurationMs,
                    durationMsDisplay = e.DurationMs?.ToString() ?? Labels.AssistantNotRecorded,
                    errorCode = e.ErrorCode
                }).ToArray()
            };

            if (json) await Cli.WriteAsync(output, auditPayload).ConfigureAwait(false);
            else await output.WriteLineAsync($"Audit entries: {auditEntries.Count}").ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}").ConfigureAwait(false);
            return 130;
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

    private static string DefaultStoreDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "CfdWorkbench");
        }
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "CfdWorkbench");
    }

    private static IReadOnlyList<string> BuildKnownTokens(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 2)
            return [];
        return [raw.Trim()];
    }

    private static async Task<string> ReadBoundedExcerptTextAsync(string path, CancellationToken ct)
    {
        await using var stream = UserFile.OpenRead(path, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] buffer = new byte[MaxInputExcerptBytes];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct).ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
        }
        return Encoding.UTF8.GetString(buffer, 0, total);
    }

    private static bool TryParseArea(string raw, out AssistantArea area, out AssistantProposalKind defaultKind)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "setup":
                area = AssistantArea.Setup;
                defaultKind = AssistantProposalKind.SetupSeed;
                return true;
            case "cad":
                area = AssistantArea.Cad;
                defaultKind = AssistantProposalKind.GeometryEdit;
                return true;
            case "analysis":
                area = AssistantArea.Analysis;
                defaultKind = AssistantProposalKind.AnalysisExplanation;
                return true;
            case "experiment":
                area = AssistantArea.Experiment;
                defaultKind = AssistantProposalKind.ExperimentConfig;
                return true;
            case "run":
                area = AssistantArea.Run;
                defaultKind = AssistantProposalKind.RunFailureExplanation;
                return true;
            case "solver-setup":
                area = AssistantArea.SolverSetup;
                defaultKind = AssistantProposalKind.EnvironmentStep;
                return true;
            case "results":
                area = AssistantArea.Results;
                defaultKind = AssistantProposalKind.ResultsExplanation;
                return true;
            case "export":
                area = AssistantArea.Export;
                defaultKind = AssistantProposalKind.None;
                return true;
            case "notebook":
                area = AssistantArea.Notebook;
                defaultKind = AssistantProposalKind.RequirementDecomp;
                return true;
            default:
                area = default;
                defaultKind = default;
                return false;
        }
    }
}
