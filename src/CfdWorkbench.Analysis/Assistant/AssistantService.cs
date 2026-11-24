using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.Assistant;

public enum AssistantGateBlockKind
{
    None,
    ExportUnsupported,
    NoApiKey,
    UnevaluatedModel,
    DailyCapReached,
    InvalidEndpoint,
    SettingsError
}

public sealed record AssistantTelemetryEvent(
    string EventName,
    string RequestId,
    AssistantArea Area,
    AssistantProposalKind ProposalKind,
    AssistantProviderKind ProviderKind,
    string ModelId,
    int? UsageInputTokens,
    int? UsageOutputTokens,
    long? DurationMs,
    string Verdict,
    string? ErrorCode);

public sealed record AssistantPreviewRequest(
    AssistantArea Area,
    AssistantProposalKind ProposalKind,
    string SystemPrompt,
    string UserPrompt,
    IReadOnlyList<UntrustedExcerpt>? Excerpts = null,
    string? KnowledgeQuery = null);

public sealed record PreparedAssistantCall(
    string RequestId,
    AssistantArea Area,
    AssistantProposalKind ProposalKind,
    AssistantSettings SettingsSnapshot,
    string EndpointOrigin,
    RedactedPromptPayload Payload,
    IReadOnlyList<string> KnowledgeIds,
    bool KnowledgeAvailable,
    string? KnowledgeUnavailableReason);

public sealed record AssistantPreviewResult(
    bool Allowed,
    AssistantGateBlockKind BlockKind,
    string? BlockCode,
    string? Message,
    PreparedAssistantCall? Prepared,
    ModelEligibilityStatus? Eligibility,
    int DailyUsedUtc,
    int DailyCap);

public sealed record AssistantExecutionResult(
    bool Succeeded,
    AssistantResponseValidation? Validation,
    LlmTransportResponse? Transport,
    AssistantAuditEntry AuditEntry,
    string? ErrorCode,
    string? ErrorMessage,
    string? FallbackText);

public sealed record AssistantStatusSnapshot(
    bool CredentialStoreSupported,
    bool HasApiKey,
    AssistantSettings Settings,
    string SettingsOutcome,
    IReadOnlyList<string> SettingsCodes,
    bool NeverWrite,
    ModelEligibilityStatus Eligibility,
    int DailyRequestsUsedUtc,
    bool KnowledgeAvailable,
    string? KnowledgeUnavailableReason,
    int KnowledgeEntryCount,
    string KnowledgeSha256,
    string EvalThresholdsVersion);

public sealed record AssistantEvalRunResult(
    AssistantModelEvalReceipt Receipt,
    ModelEligibilityStatus Eligibility,
    AssistantSettingsSaveResult SaveResult,
    bool LiveWire);

/// <summary>
/// Cross-area BYOK assistant orchestrator (docs/design/byok-assistants.md §3.4, C3).
/// Enforces Area 7 Export refusal, live OS key presence (<c>AI-01</c>), <c>AI-06</c> model evaluation gate,
/// UTC daily request cap, byte-exact <see cref="PromptRedactor"/> preview bound by recomputed wire UTF-8
/// SHA-256 (Advisory #3), BM25 <see cref="KnowledgeRetriever"/> grounding, <see cref="ByokLlmTransport"/>,
/// deterministic T0 <see cref="AssistantValidators"/>, local audit append, and <c>assistant.request</c> telemetry.
/// Zero reference to <c>CfdWorkbench.Persistence</c> and zero banned Core edit verbs (<c>ArchitectureTests</c>).
/// </summary>
public sealed class AssistantService
{
    public const string TelemetryEventName = "assistant.request";

    private readonly ICredentialStore credentials;
    private readonly IAssistantSettingsStore settingsStore;
    private readonly ByokLlmTransport transport;
    private readonly KnowledgeRetriever knowledge;
    private readonly Func<DateTimeOffset> clock;

    public AssistantService(
        ICredentialStore credentials,
        IAssistantSettingsStore settingsStore,
        ByokLlmTransport transport,
        KnowledgeRetriever? knowledge = null,
        Func<DateTimeOffset>? clock = null)
    {
        this.credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.knowledge = knowledge ?? KnowledgeRetriever.LoadEmbedded();
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public event Action<AssistantTelemetryEvent>? TelemetryEmitted;

    public async Task<AssistantStatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var loaded = await settingsStore.LoadSettingsAsync(ct).ConfigureAwait(false);
        var settings = loaded.Settings;
        bool hasKey = credentials.HasKey(settings.ProviderKind);
        var eligibility = AssistantEvalSuite.CheckModelEligibility(settings, settings.EvalReceipts);
        DateOnly todayUtc = DateOnly.FromDateTime(clock().UtcDateTime);
        int dailyUsed = await settingsStore.CountDailyRequestsUtcAsync(todayUtc, ct).ConfigureAwait(false);

        return new AssistantStatusSnapshot(
            CredentialStoreSupported: credentials.IsSupported,
            HasApiKey: hasKey,
            Settings: settings,
            SettingsOutcome: loaded.Outcome,
            SettingsCodes: loaded.Codes,
            NeverWrite: loaded.NeverWrite,
            Eligibility: eligibility,
            DailyRequestsUsedUtc: dailyUsed,
            KnowledgeAvailable: knowledge.Available,
            KnowledgeUnavailableReason: knowledge.UnavailableReason,
            KnowledgeEntryCount: knowledge.Entries.Count,
            KnowledgeSha256: knowledge.ActualSha256,
            EvalThresholdsVersion: AssistantEvalSuite.EvalThresholdsVersion);
    }

    public async Task<AssistantPreviewResult> PreparePreviewAsync(
        AssistantPreviewRequest request,
        RedactionContext? redaction = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Area == AssistantArea.Export || request.ProposalKind == AssistantProposalKind.None)
        {
            return new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.ExportUnsupported,
                BlockCode: "ASSISTANT-EXPORT-NONE",
                Message: AssistantValidators.ExportNoAssistantMessage,
                Prepared: null,
                Eligibility: null,
                DailyUsedUtc: 0,
                DailyCap: 0);
        }

        var loaded = await settingsStore.LoadSettingsAsync(ct).ConfigureAwait(false);
        var settings = loaded.Settings;
        DateOnly todayUtc = DateOnly.FromDateTime(clock().UtcDateTime);
        int dailyUsed = await settingsStore.CountDailyRequestsUtcAsync(todayUtc, ct).ConfigureAwait(false);
        var eligibility = AssistantEvalSuite.CheckModelEligibility(settings, settings.EvalReceipts);

        var endpointCheck = ByokLlmTransport.ValidateEndpointUri(settings.EndpointUrl);
        if (!endpointCheck.IsValid)
        {
            return new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.InvalidEndpoint,
                BlockCode: endpointCheck.ErrorCode,
                Message: endpointCheck.Message,
                Prepared: null,
                Eligibility: eligibility,
                DailyUsedUtc: dailyUsed,
                DailyCap: settings.DailyRequestCap);
        }

        if (!credentials.HasKey(settings.ProviderKind))
        {
            return new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.NoApiKey,
                BlockCode: "ASSISTANT-NO-KEY",
                Message: AssistantValidators.FormatNoKeyStatus(request.Area),
                Prepared: null,
                Eligibility: eligibility,
                DailyUsedUtc: dailyUsed,
                DailyCap: settings.DailyRequestCap);
        }

        if (!eligibility.ProposalsEnabled)
        {
            return new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.UnevaluatedModel,
                BlockCode: "ASSISTANT-UNEVALUATED",
                Message: eligibility.StatusLabel,
                Prepared: null,
                Eligibility: eligibility,
                DailyUsedUtc: dailyUsed,
                DailyCap: settings.DailyRequestCap);
        }

        if (dailyUsed >= settings.DailyRequestCap)
        {
            return new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.DailyCapReached,
                BlockCode: "ASSISTANT-DAILY-CAP",
                Message: Labels.AssistantDailyCapReached(dailyUsed, settings.DailyRequestCap),
                Prepared: null,
                Eligibility: eligibility,
                DailyUsedUtc: dailyUsed,
                DailyCap: settings.DailyRequestCap);
        }

        var effectiveRedaction = BuildEffectiveRedactionContext(settings.ProviderKind, redaction);
        var prepared = BuildOfflinePreview(request, settings, effectiveRedaction);

        return new AssistantPreviewResult(
            Allowed: true,
            BlockKind: AssistantGateBlockKind.None,
            BlockCode: null,
            Message: null,
            Prepared: prepared,
            Eligibility: eligibility,
            DailyUsedUtc: dailyUsed,
            DailyCap: settings.DailyRequestCap);
    }

    public PreparedAssistantCall BuildOfflinePreview(
        AssistantPreviewRequest request,
        AssistantSettings settings,
        RedactionContext? redaction = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);

        var combinedExcerpts = new List<UntrustedExcerpt>();
        if (request.Excerpts is { Count: > 0 })
            combinedExcerpts.AddRange(request.Excerpts);

        var knowledgeIds = new List<string>();
        if (knowledge.Available)
        {
            string query = string.IsNullOrWhiteSpace(request.KnowledgeQuery)
                ? request.UserPrompt
                : request.KnowledgeQuery;
            var hits = knowledge.Search(query, maxResults: 3, preferredArea: request.Area);
            foreach (var hit in hits)
            {
                knowledgeIds.Add(hit.Entry.Id);
                combinedExcerpts.Add(new UntrustedExcerpt(
                    Kind: "knowledge-snippet",
                    Id: hit.Entry.Id,
                    RawText: $"{hit.Entry.Title}: {hit.Entry.Body}"));
            }
        }

        var payload = PromptRedactor.RedactPayload(
            request.SystemPrompt,
            request.UserPrompt,
            combinedExcerpts,
            redaction ?? RedactionContext.Empty);

        string origin = AssistantEvalSuite.CanonicalizeEndpointOrigin(settings.EndpointUrl);

        return new PreparedAssistantCall(
            RequestId: Guid.NewGuid().ToString("D"),
            Area: request.Area,
            ProposalKind: request.ProposalKind,
            SettingsSnapshot: settings,
            EndpointOrigin: origin,
            Payload: payload,
            KnowledgeIds: knowledgeIds,
            KnowledgeAvailable: knowledge.Available,
            KnowledgeUnavailableReason: knowledge.UnavailableReason);
    }

    public async Task<AssistantExecutionResult> ExecuteConsentedAsync(
        PreparedAssistantCall prepared,
        string consentedPayloadSha256,
        AssistantValidationInput validationInput,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(validationInput);

        if (prepared.Area == AssistantArea.Export || prepared.ProposalKind == AssistantProposalKind.None)
        {
            return await RecordBlockedAsync(
                prepared,
                consentAction: "blocked-export",
                errorCode: "ASSISTANT-EXPORT-NONE",
                errorMessage: AssistantValidators.ExportNoAssistantMessage,
                fallbackText: validationInput.FallbackText,
                ct: ct).ConfigureAwait(false);
        }

        // Advisory #3 + C3: ALWAYS recompute SHA-256 directly over the actual wire UTF-8 bytes
        // and verify CanonicalWirePreview matches SystemPrompt + "\n---\n" + UserPrompt.
        string expectedCanonical = prepared.Payload.SystemPrompt + "\n---\n" + prepared.Payload.UserPrompt;
        string recomputedWireHash = PromptRedactor.ComputeUtf8Sha256Hex(prepared.Payload.CanonicalWirePreview);
        bool hashValid =
            string.Equals(prepared.Payload.CanonicalWirePreview, expectedCanonical, StringComparison.Ordinal) &&
            string.Equals(recomputedWireHash, consentedPayloadSha256, StringComparison.Ordinal) &&
            string.Equals(prepared.Payload.PayloadSha256, recomputedWireHash, StringComparison.Ordinal);

        if (!hashValid)
        {
            return await RecordBlockedAsync(
                prepared,
                consentAction: "blocked-consent-mismatch",
                errorCode: "ASSISTANT-CONSENT-HASH-MISMATCH",
                errorMessage: "Consented payload hash does not match the exact wire payload. Re-preview before sending.",
                fallbackText: validationInput.FallbackText,
                ct: ct).ConfigureAwait(false);
        }

        // AI-01: Live key re-read immediately prior to transmit so removing the key stops the next call without restart.
        var keyResult = credentials.ReadKey(prepared.SettingsSnapshot.ProviderKind);
        if (!keyResult.Succeeded || string.IsNullOrWhiteSpace(keyResult.Secret))
        {
            return await RecordBlockedAsync(
                prepared,
                consentAction: "blocked-no-key",
                errorCode: "ASSISTANT-NO-KEY",
                errorMessage: AssistantValidators.FormatNoKeyStatus(prepared.Area),
                fallbackText: validationInput.FallbackText,
                ct: ct).ConfigureAwait(false);
        }

        // AI-06 defense in depth: verify model eligibility on settings snapshot.
        var eligibility = AssistantEvalSuite.CheckModelEligibility(prepared.SettingsSnapshot, prepared.SettingsSnapshot.EvalReceipts);
        if (!eligibility.ProposalsEnabled)
        {
            return await RecordBlockedAsync(
                prepared,
                consentAction: "blocked-unevaluated",
                errorCode: "ASSISTANT-UNEVALUATED",
                errorMessage: eligibility.StatusLabel,
                fallbackText: validationInput.FallbackText,
                ct: ct).ConfigureAwait(false);
        }

        // Re-check UTC daily request cap right before transmit.
        DateOnly todayUtc = DateOnly.FromDateTime(clock().UtcDateTime);
        int dailyUsed = await settingsStore.CountDailyRequestsUtcAsync(todayUtc, ct).ConfigureAwait(false);
        if (dailyUsed >= prepared.SettingsSnapshot.DailyRequestCap)
        {
            return await RecordBlockedAsync(
                prepared,
                consentAction: "blocked-cap",
                errorCode: "ASSISTANT-DAILY-CAP",
                errorMessage: Labels.AssistantDailyCapReached(dailyUsed, prepared.SettingsSnapshot.DailyRequestCap),
                fallbackText: validationInput.FallbackText,
                ct: ct).ConfigureAwait(false);
        }

        var transportResponse = await transport.SendAsync(
            new LlmTransportRequest(
                ProviderKind: prepared.SettingsSnapshot.ProviderKind,
                EndpointUrl: prepared.SettingsSnapshot.EndpointUrl,
                ModelId: prepared.SettingsSnapshot.ModelId,
                ApiKey: keyResult.Secret,
                Payload: prepared.Payload,
                PerRequestMaxTokens: prepared.SettingsSnapshot.PerRequestMaxTokens,
                TimeoutSeconds: prepared.SettingsSnapshot.TimeoutSeconds),
            ct).ConfigureAwait(false);

        if (!transportResponse.Succeeded)
        {
            string safeErrorMsg = ByokLlmTransport.ScrubSecrets(transportResponse.ErrorMessage, keyResult.Secret);
            var errEntry = BuildAuditEntry(
                prepared,
                recomputedWireHash,
                consentAction: "consented",
                verdict: "error",
                usageIn: transportResponse.UsageInputTokens,
                usageOut: transportResponse.UsageOutputTokens,
                durationMs: transportResponse.DurationMs,
                errorCode: transportResponse.ErrorCode);

            await settingsStore.AppendAuditEntryAsync(errEntry, ct).ConfigureAwait(false);
            EmitTelemetry(errEntry);

            return new AssistantExecutionResult(
                Succeeded: false,
                Validation: null,
                Transport: transportResponse with { ErrorMessage = safeErrorMsg },
                AuditEntry: errEntry,
                ErrorCode: transportResponse.ErrorCode,
                ErrorMessage: safeErrorMsg,
                FallbackText: validationInput.FallbackText);
        }

        // T0 deterministic validation authority — raw LLM text is never trusted directly.
        var effectiveValidationInput = validationInput.KnownKnowledgeIds is null && knowledge.Available
            ? validationInput with { KnownKnowledgeIds = knowledge.KnownIds }
            : validationInput;

        var validation = AssistantValidators.ValidateResponse(transportResponse.RawText ?? "", effectiveValidationInput);
        string verdictLabel = validation.Verdict switch
        {
            AssistantVerdict.Shown => "shown",
            AssistantVerdict.Withheld => "withheld",
            _ => "refused"
        };

        string? valErrCode = validation.Verdict == AssistantVerdict.Shown ? null : validation.Reason;
        var entry = BuildAuditEntry(
            prepared,
            recomputedWireHash,
            consentAction: "consented",
            verdict: verdictLabel,
            usageIn: transportResponse.UsageInputTokens,
            usageOut: transportResponse.UsageOutputTokens,
            durationMs: transportResponse.DurationMs,
            errorCode: valErrCode);

        await settingsStore.AppendAuditEntryAsync(entry, ct).ConfigureAwait(false);
        EmitTelemetry(entry);

        return new AssistantExecutionResult(
            Succeeded: validation.Verdict == AssistantVerdict.Shown,
            Validation: validation,
            Transport: transportResponse,
            AuditEntry: entry,
            ErrorCode: valErrCode,
            ErrorMessage: validation.Verdict == AssistantVerdict.Shown
                ? null
                : ByokLlmTransport.ScrubSecrets(validation.Message, keyResult.Secret),
            FallbackText: validationInput.FallbackText);
    }

    public async Task<AssistantAuditEntry> RecordDeclinedAsync(
        PreparedAssistantCall prepared,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        string wireHash = PromptRedactor.ComputeUtf8Sha256Hex(prepared.Payload.CanonicalWirePreview);
        var entry = BuildAuditEntry(
            prepared,
            wireHash,
            consentAction: "declined",
            verdict: "declined",
            usageIn: null,
            usageOut: null,
            durationMs: null,
            errorCode: null);

        await settingsStore.AppendAuditEntryAsync(entry, ct).ConfigureAwait(false);
        EmitTelemetry(entry);
        return entry;
    }

    public async Task<AssistantEvalRunResult> RunEvaluationAsync(
        bool liveWire = false,
        CancellationToken ct = default)
    {
        var loaded = await settingsStore.LoadSettingsAsync(ct).ConfigureAwait(false);
        var settings = loaded.Settings;

        string? liveApiKey = null;
        if (liveWire)
        {
            var keyRes = credentials.ReadKey(settings.ProviderKind);
            if (!keyRes.Succeeded || string.IsNullOrWhiteSpace(keyRes.Secret))
                throw new InvalidOperationException("Live model evaluation requires a configured API key in the OS credential store.");
            liveApiKey = keyRes.Secret;
        }

        var suite = AssistantEvalSuite.GetSuiteDefinition();
        var itemResults = new List<AssistantEvalItemResult>(suite.Count);

        foreach (var item in suite)
        {
            if (item.Reserved)
            {
                itemResults.Add(new AssistantEvalItemResult(
                    ItemId: item.ItemId,
                    Title: item.Title,
                    PassedCases: 0,
                    TotalCases: 0,
                    PassRate: 0.0,
                    Threshold: item.Threshold,
                    Reserved: true,
                    MetThreshold: false));
                continue;
            }

            int passed = 0;
            foreach (var evalCase in item.Cases)
            {
                ct.ThrowIfCancellationRequested();
                string rawResponse;
                if (!liveWire)
                {
                    rawResponse = evalCase.ExpectedRawResponse;
                }
                else
                {
                    var evalPayload = PromptRedactor.RedactPayload(
                        "Return strict JSON matching the CFD-Workbench schema for the requested area.",
                        evalCase.Prompt,
                        [],
                        new RedactionContext(liveApiKey, [], [], [], []));

                    var resp = await transport.SendAsync(
                        new LlmTransportRequest(
                            ProviderKind: settings.ProviderKind,
                            EndpointUrl: settings.EndpointUrl,
                            ModelId: settings.ModelId,
                            ApiKey: liveApiKey!,
                            Payload: evalPayload,
                            PerRequestMaxTokens: settings.PerRequestMaxTokens,
                            TimeoutSeconds: settings.TimeoutSeconds),
                        ct).ConfigureAwait(false);

                    rawResponse = resp.Succeeded ? (resp.RawText ?? "") : "";
                }

                var outcome = AssistantEvalSuite.ScoreCaseResponse(evalCase, rawResponse);
                if (outcome.Passed)
                    passed++;
            }

            int total = item.Cases.Count;
            double rate = total > 0 ? (double)passed / total : 0.0;
            bool met = total > 0 && rate + 1e-12 >= item.Threshold;

            itemResults.Add(new AssistantEvalItemResult(
                ItemId: item.ItemId,
                Title: item.Title,
                PassedCases: passed,
                TotalCases: total,
                PassRate: rate,
                Threshold: item.Threshold,
                Reserved: false,
                MetThreshold: met));
        }

        var receipt = AssistantEvalSuite.BuildReceipt(
            providerKind: settings.ProviderKind,
            endpointOrigin: settings.EndpointUrl,
            modelId: settings.ModelId,
            evaluatedAtUtc: clock(),
            items: itemResults);

        var updatedReceipts = new List<AssistantModelEvalReceipt>(settings.EvalReceipts ?? []) { receipt };
        var updatedSettings = settings with { EvalReceipts = updatedReceipts };
        var saveResult = await settingsStore.SaveSettingsAsync(updatedSettings, ct).ConfigureAwait(false);
        var eligibility = AssistantEvalSuite.CheckModelEligibility(updatedSettings, updatedSettings.EvalReceipts);

        return new AssistantEvalRunResult(receipt, eligibility, saveResult, liveWire);
    }

    private RedactionContext BuildEffectiveRedactionContext(AssistantProviderKind providerKind, RedactionContext? callerContext)
    {
        var baseCtx = callerContext ?? RedactionContext.Empty;
        if (!string.IsNullOrEmpty(baseCtx.ActiveApiKey))
            return baseCtx;

        var keyRes = credentials.ReadKey(providerKind);
        if (!keyRes.Succeeded || string.IsNullOrWhiteSpace(keyRes.Secret))
            return baseCtx;

        return baseCtx with { ActiveApiKey = keyRes.Secret };
    }

    private async Task<AssistantExecutionResult> RecordBlockedAsync(
        PreparedAssistantCall prepared,
        string consentAction,
        string errorCode,
        string errorMessage,
        string? fallbackText,
        CancellationToken ct)
    {
        string wireHash = PromptRedactor.ComputeUtf8Sha256Hex(prepared.Payload.CanonicalWirePreview);
        var entry = BuildAuditEntry(
            prepared,
            wireHash,
            consentAction: consentAction,
            verdict: "blocked",
            usageIn: null,
            usageOut: null,
            durationMs: null,
            errorCode: errorCode);

        await settingsStore.AppendAuditEntryAsync(entry, ct).ConfigureAwait(false);
        EmitTelemetry(entry);

        return new AssistantExecutionResult(
            Succeeded: false,
            Validation: null,
            Transport: null,
            AuditEntry: entry,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            FallbackText: fallbackText);
    }

    private AssistantAuditEntry BuildAuditEntry(
        PreparedAssistantCall prepared,
        string wireHash,
        string consentAction,
        string verdict,
        int? usageIn,
        int? usageOut,
        long? durationMs,
        string? errorCode) => new(
            RequestId: prepared.RequestId,
            TimestampUtc: clock().ToUniversalTime(),
            Area: prepared.Area,
            ProposalKind: prepared.ProposalKind,
            ProviderKind: prepared.SettingsSnapshot.ProviderKind,
            EndpointOrigin: prepared.EndpointOrigin,
            ModelId: prepared.SettingsSnapshot.ModelId,
            PayloadSha256: wireHash,
            PromptUtf8Bytes: prepared.Payload.PromptUtf8Bytes,
            ConsentAction: consentAction,
            Verdict: verdict,
            UsageInputTokens: usageIn,
            UsageOutputTokens: usageOut,
            DurationMs: durationMs,
            ErrorCode: errorCode);

    private void EmitTelemetry(AssistantAuditEntry entry)
    {
        TelemetryEmitted?.Invoke(new AssistantTelemetryEvent(
            EventName: TelemetryEventName,
            RequestId: entry.RequestId,
            Area: entry.Area,
            ProposalKind: entry.ProposalKind,
            ProviderKind: entry.ProviderKind,
            ModelId: entry.ModelId,
            UsageInputTokens: entry.UsageInputTokens,
            UsageOutputTokens: entry.UsageOutputTokens,
            DurationMs: entry.DurationMs,
            Verdict: entry.Verdict,
            ErrorCode: entry.ErrorCode));
    }
}
