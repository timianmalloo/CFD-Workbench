using System.Globalization;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Desktop.Assistant;

/// <summary>
/// Single source of truth for Wave 11 BYOK Assistant UI strings (COPY-866..915,
/// docs/design/byok-assistants.md §5). Zero raw hex colors, zero LAB-01 forbidden claims,
/// and zero SETUP-04 shell commands.
/// </summary>
public static class AssistantCopy
{
    // COPY-866
    public const string SettingsTitle = "Settings";
    public const string SettingsCommandTitle = "Settings…";

    // COPY-867
    public const string BadgeKeySet = "Key set";
    public const string BadgeNoKey = "No key";
    public const string BadgeEvalNeeded = "Eval needed";

    // COPY-868
    public const string SettingsDialogTitle = "Assistant settings (BYOK)";

    // COPY-869
    public const string CredentialSectionHeading = "API key (OS credential store)";

    // COPY-870
    public const string KeySavedNotice =
        "Key saved in OS credential store (Windows Credential Manager DPAPI / macOS Keychain). Never written to files, logs, exports, or environment variables.";

    // COPY-871
    public const string NoKeyNotice =
        "No API key configured — every design, analysis, solver setup, run, results, and export tool works without it.";

    // COPY-872
    public const string ConfigureKeyButton = "Configure key";

    // COPY-873
    public const string SaveKeyButton = "Save key";
    public const string RemoveKeyButton = "Remove key";

    // COPY-874
    public const string KeyRemovedReceipt =
        "Key removed. New assistant calls are stopped immediately without restarting.";

    // COPY-875
    public const string KeyEmptyError = "Enter an API key before saving.";
    public const string KeyTooLargeError = "API key exceeds the 2560-byte OS credential store limit.";

    // COPY-876
    public const string CredentialStoreUnsupported =
        "OS credential store is unavailable on this platform. Keys are never stored in files or environment variables.";

    // COPY-877
    public const string ProviderSectionHeading = "Provider & endpoint";

    // COPY-878
    public const string ProviderAnthropic = "Anthropic (/v1/messages)";
    public const string ProviderOpenAiCompatible = "OpenAI-compatible (/v1/chat/completions)";

    // COPY-879
    public const string EndpointUrlLabel =
        "Endpoint URL (single HTTPS endpoint; HTTP allowed for exact localhost / 127.0.0.1 / ::1 only)";
    public const string EndpointUrlShortLabel = "Endpoint URL";
    public const string EndpointUrlHelperText =
        "Single HTTPS endpoint; HTTP allowed for exact localhost / 127.0.0.1 / ::1 only.";

    // COPY-880
    public const string EndpointInvalidError =
        "Endpoint refused: use HTTPS (or exact localhost / 127.0.0.1 / ::1) with no userinfo or lookalike host.";

    // COPY-881
    public const string ModelIdLabel = "Model identifier";
    public const string OptionalFastModelIdLabel = "Fast model";
    public const string OptionalFastModelHelperText =
        "Optional same-endpoint fast model pin for translation/condensation; requires its own AI-06 evaluation receipt. Leave blank to use primary model.";
    public const string FastModelSecretError =
        "Model identifier appears to contain an API key or secret. Store API keys only in the OS credential store above.";

    // COPY-882
    public const string CapsSectionHeading = "Caps & timeout";

    // COPY-883
    public const string PerRequestCapLabel = "Max output tokens per request (128–16384)";
    public const string DailyCapLabel = "Daily UTC request cap (1–500)";
    public const string TimeoutLabel = "Request timeout in seconds (5–120)";

    // COPY-884
    public static string DailyUsageSummary(int used, int cap) =>
        string.Create(CultureInfo.InvariantCulture, $"Used today (UTC): {used} of {cap} requests");

    public static string DailyUsageChip(int used, int cap) =>
        string.Create(CultureInfo.InvariantCulture, $"Used {used}/{cap} today");

    // COPY-885
    public static string DailyCapReached(int used, int cap) =>
        string.Create(CultureInfo.InvariantCulture,
            $"Daily UTC request cap reached ({used} of {cap}). Raise the cap in Settings or wait for the next UTC day.");

    /// <summary>Dynamic count of active (non-reserved) evaluation items from <see cref="AssistantEvalSuite.GetSuiteDefinition"/>.</summary>
    public static int ActiveEvalItemCount => AssistantEvalSuite.GetSuiteDefinition().Count(i => !i.Reserved);

    /// <summary>Dynamic total count of evaluation items (active + reserved) from <see cref="AssistantEvalSuite.GetSuiteDefinition"/>.</summary>
    public static int TotalEvalItemCount => AssistantEvalSuite.GetSuiteDefinition().Count;

    // COPY-886 (dynamic count synced with AssistantEvalSuite.GetSuiteDefinition())
    public static string EvalSectionHeading =>
        string.Create(CultureInfo.InvariantCulture,
            $"Model evaluation ({ActiveEvalItemCount} active items; physical-fidelity reserved in v1)");

    // COPY-887
    public const string UnevaluatedOnThisModel = AssistantEvalSuite.UnevaluatedStatusLabel;

    // COPY-888
    public static string UnevaluatedDetail(string thresholdsVersion) =>
        string.Create(CultureInfo.InvariantCulture,
            $"Proposals are disabled until the {ActiveEvalItemCount}-item evaluation suite passes on this provider, endpoint, model, and threshold version ({thresholdsVersion}). Saved geometry identities are unchanged.");

    // COPY-889
    public static string EvalPassedStatus(string modelId, DateTimeOffset evaluatedAtUtc, string thresholdsVersion) =>
        string.Create(CultureInfo.InvariantCulture,
            $"Evaluation passed on {modelId} ({evaluatedAtUtc.ToUniversalTime():yyyy-MM-dd}, thresholds v{thresholdsVersion}). Proposals enabled.");

    // COPY-890
    public static string EvalFailedStatus(IReadOnlyList<string> failingItemIds) =>
        $"Evaluation failed — below threshold on: {string.Join(", ", failingItemIds)}. Proposals remain disabled.";

    // COPY-891 (dynamic count synced with AssistantEvalSuite.GetSuiteDefinition())
    public static string RunEvalButton =>
        string.Create(CultureInfo.InvariantCulture, $"Run {ActiveEvalItemCount}-item evaluation");
    public static string RunFastEvalButton =>
        string.Create(CultureInfo.InvariantCulture, $"Evaluate fast model ({ActiveEvalItemCount} items)");
    public const string OpenSettingsButton = "Open Settings";

    // COPY-892
    public const string EvalItemReserved = "Reserved (v1)";

    // COPY-893
    public const string AuditSectionHeading = "Local assistant audit log";

    // COPY-894
    public const string AuditEmptyNotice = "No assistant requests recorded yet.";

    // COPY-895
    public const string NotRecorded = "Not recorded";

    // COPY-896
    public const string ConsentStepHeading = "Review redacted payload before sending";

    // COPY-897
    public static string ConsentWireSummary(string origin, string modelId, int bytes, string sha256)
    {
        string shortHash = sha256.Length >= 12 ? sha256[..12] : sha256;
        return string.Create(CultureInfo.InvariantCulture,
            $"Endpoint: {origin} · Model: {modelId} · Payload: {bytes:N0} / {PromptRedactor.MaxPromptUtf8Bytes:N0} UTF-8 B · SHA-256: {shortHash}");
    }

    // COPY-898
    public static string RedactionSummaryLine(RedactionSummary summary) =>
        string.Create(CultureInfo.InvariantCulture,
            $"Redacted: {summary.PathsRedacted} paths · {summary.UserNamesRedacted} user names · {summary.HostNamesRedacted} hostnames · {summary.DrcAttributionsRedacted} DRC attributions · {summary.SecretsRedacted} secrets");

    // COPY-899
    public const string TruncationNotice =
        "One or more excerpts exceeded the 4,096-byte cap and were truncated with a visible marker.";

    // COPY-900
    public const string SendToModelButton = "Send to model";
    public const string CancelConsentButton = "Cancel";

    // COPY-901
    public const string ProposalStepHeading = "Review checked proposal";

    // COPY-902
    public const string ProvenanceStated = "Stated";
    public const string ProvenanceInferred = "Inferred";
    public const string ProvenanceDefaulted = "Defaulted";

    // COPY-903
    public const string AcceptProposalButton = "Accept";
    public const string DiscardProposalButton = "Discard";
    public const string RefreshBaseButton = "Refresh over current revision";

    // COPY-904
    public const string BaseRevisionDriftNotice =
        "Accept disabled — base design revision moved since preview. Refresh over the current revision before accepting.";

    // COPY-905
    public static string ProposalRefusedBanner(string reason) =>
        $"Proposal refused by deterministic validator: {reason}";

    // COPY-906
    public static string ResponseWithheldBanner(string reason) =>
        $"Response withheld: {reason}";

    // COPY-907
    public const string CadEntryTitle = "Describe a change to the shape";

    // COPY-908
    public const string AnalysisEntryTitle = "Ask about this calculation";

    // COPY-909
    public const string WhatIfRecomputeButton = "Run deterministic recomputation";

    // COPY-910
    public const string ExperimentEntryTitle = "Describe the experiment";

    // COPY-911
    public const string OpenRepairedDraftChip = "Open repaired draft";

    // COPY-912
    public const string RunExplainLiveButton = "Explain with assistant (BYOK)";

    // COPY-913
    public const string ExportNoAssistantNote = "No assistant action here.";

    // COPY-914
    public static string TransportErrorNotice(string errorKind, string detail) =>
        $"Assistant request failed ({errorKind}): {detail}. Manual workflow is unaffected.";

    // COPY-915
    public const string KnowledgeUnavailableNotice =
        "Bundled knowledge corpus unavailable (integrity check failed).";

    // Wave 12B operational status & error-recovery copy (R2-03, R2-11; LAB-01 & SETUP-04 clean).
    public const string SendingToModelStatus = "Sending to model…";
    public const string RunningEvaluationStatus = "Running evaluation…";
    public const string RequestCancelledNotice = "Request cancelled. You can retry or edit your prompt.";
    public const string EvaluationCancelledNotice = "Evaluation cancelled.";
    public const string RetryButton = "Retry";
    public const string EditPromptButton = "Edit prompt";
    public const string RefreshBaseFailedNotice =
        "Could not prepare a refreshed preview over the current revision. Check your API key, model evaluation, and daily cap.";
}

/// <summary>Structured wrapped provenance row for <see cref="AssistantConsentPreviewDialog"/> (R2-11, C6).</summary>
public sealed record ProvenanceRowItem(
    string Tag,
    string FieldName,
    string ChangeDisplay,
    string Reason)
{
    public override string ToString() => $"[{Tag}] {FieldName}: {ChangeDisplay} — {Reason}";
}

/// <summary>Structured wrapped evaluation item row for <see cref="AssistantSettingsDialog"/> (R2-10, C6).</summary>
public sealed record EvalRowItem(
    string ItemId,
    string Title,
    string ScoreText,
    string StatusText,
    bool IsReserved,
    bool HasRun)
{
    public override string ToString() =>
        IsReserved ? $"{ItemId} · {Title} · {StatusText}"
        : HasRun ? $"{ItemId} · {ScoreText} · {StatusText}"
        : $"{ItemId} · {Title} · {ScoreText} · {StatusText}";
}

/// <summary>Structured wrapped audit log entry row for <see cref="AssistantSettingsDialog"/> (R2-10).</summary>
public sealed record AuditRowItem(
    string HeaderLine,
    string MetricsLine,
    string OutcomeText,
    string SummaryLine)
{
    public override string ToString() => SummaryLine;
}

/// <summary>
/// Isolated in-memory <see cref="ICredentialStore"/> used as the default in headless/test construction so existing
/// Desktop tests remain 100% deterministic even on developer PCs that hold a saved OS BYOK key (C5).
/// </summary>
public sealed class InMemoryCredentialStore : ICredentialStore
{
    private const int MaxCredentialBlobBytes = 2_560;
    private readonly Dictionary<AssistantProviderKind, string> keys = new();
    private readonly object gate = new();

    public bool IsSupported => true;

    public bool HasKey(AssistantProviderKind providerKind)
    {
        lock (gate) return keys.TryGetValue(providerKind, out var s) && !string.IsNullOrWhiteSpace(s);
    }

    public CredentialStoreResult ReadKey(AssistantProviderKind providerKind)
    {
        lock (gate)
        {
            return keys.TryGetValue(providerKind, out var secret) && !string.IsNullOrWhiteSpace(secret)
                ? new CredentialStoreResult(true, secret, null, null)
                : new CredentialStoreResult(false, null, "CRED-NOT-FOUND", "No API key configured.");
        }
    }

    public CredentialStoreResult WriteKey(AssistantProviderKind providerKind, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new CredentialStoreResult(false, null, "CRED-EMPTY", AssistantCopy.KeyEmptyError);
        if (System.Text.Encoding.UTF8.GetByteCount(apiKey) > MaxCredentialBlobBytes)
            return new CredentialStoreResult(false, null, "CRED-TOO-LARGE", AssistantCopy.KeyTooLargeError);

        lock (gate) keys[providerKind] = apiKey;
        return new CredentialStoreResult(true, null, null, null);
    }

    public CredentialStoreResult DeleteKey(AssistantProviderKind providerKind)
    {
        lock (gate) keys.Remove(providerKind);
        return new CredentialStoreResult(true, null, null, null);
    }
}

/// <summary>
/// Isolated in-memory <see cref="IAssistantSettingsStore"/> paired with <see cref="InMemoryCredentialStore"/>
/// for deterministic Desktop tests.
/// </summary>
public sealed class InMemoryAssistantSettingsStore : IAssistantSettingsStore
{
    private AssistantSettings current = AssistantSettings.CreateDefault();
    private readonly List<AssistantAuditEntry> audit = [];
    private readonly object gate = new();

    public Task<AssistantSettingsLoadResult> LoadSettingsAsync(CancellationToken ct = default)
    {
        lock (gate)
        {
            return Task.FromResult(new AssistantSettingsLoadResult(current, "restored", [], false, false));
        }
    }

    public Task<AssistantSettingsSaveResult> SaveSettingsAsync(AssistantSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (gate) current = settings;
        return Task.FromResult(new AssistantSettingsSaveResult(true, "saved", null, null));
    }

    public Task<bool> AppendAuditEntryAsync(AssistantAuditEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (gate) audit.Add(entry);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<AssistantAuditEntry>> ReadAuditEntriesAsync(int maxEntries = 200, CancellationToken ct = default)
    {
        lock (gate)
        {
            if (maxEntries <= 0 || audit.Count == 0)
                return Task.FromResult<IReadOnlyList<AssistantAuditEntry>>([]);
            if (audit.Count <= maxEntries)
                return Task.FromResult<IReadOnlyList<AssistantAuditEntry>>(audit.ToArray());
            return Task.FromResult<IReadOnlyList<AssistantAuditEntry>>(audit.GetRange(audit.Count - maxEntries, maxEntries).ToArray());
        }
    }

    public Task<int> CountDailyRequestsUtcAsync(DateOnly utcDate, CancellationToken ct = default)
    {
        lock (gate)
        {
            int count = 0;
            foreach (var entry in audit)
            {
                if (DateOnly.FromDateTime(entry.TimestampUtc.UtcDateTime) == utcDate &&
                    string.Equals(entry.ConsentAction, "consented", StringComparison.Ordinal))
                    count++;
            }
            return Task.FromResult(count);
        }
    }
}

/// <summary>
/// Desktop runtime coordinator over <see cref="ICredentialStore"/>, <see cref="IAssistantSettingsStore"/>,
/// <see cref="ByokLlmTransport"/>, and <see cref="AssistantService"/>. Never caches API keys across key removal
/// (<c>AI-01</c>) and immediately notifies subscribers on key, settings, or evaluation changes without restart.
/// </summary>
public sealed class AssistantRuntimeHub : IDisposable
{
    private readonly ByokLlmTransport transport;
    private bool disposed;

    public AssistantRuntimeHub(
        ICredentialStore? credentials = null,
        IAssistantSettingsStore? settingsStore = null)
    {
        Credentials = credentials ?? new InMemoryCredentialStore();
        SettingsStore = settingsStore ?? new InMemoryAssistantSettingsStore();
        transport = new ByokLlmTransport();
        Service = new AssistantService(Credentials, SettingsStore, transport);
        Service.TelemetryEmitted += OnTelemetryEmitted;

        CurrentSettings = AssistantSettings.CreateDefault();
        Eligibility = AssistantEvalSuite.CheckModelEligibility(CurrentSettings, CurrentSettings.EvalReceipts);
        FastModelEligibility = ComputeFastModelEligibility(CurrentSettings);
    }

    public ICredentialStore Credentials { get; }
    public IAssistantSettingsStore SettingsStore { get; }
    public AssistantService Service { get; }

    public AssistantSettings CurrentSettings { get; private set; }
    public ModelEligibilityStatus Eligibility { get; private set; }
    public ModelEligibilityStatus? FastModelEligibility { get; private set; }
    public int DailyRequestsUsedUtc { get; private set; }
    public AssistantTelemetryEvent? LastTelemetry { get; private set; }

    /// <summary>Derived live from OS credential store on every read (DM7, AI-01).</summary>
    public bool HasApiKey => Credentials.HasKey(CurrentSettings.ProviderKind);

    /// <summary>True only when a live key is present AND the current provider/endpoint/model/thresholds passed the active eval suite.</summary>
    public bool IsReadyForProposals => HasApiKey && Eligibility.ProposalsEnabled;

    public string RailSettingsBadge =>
        !HasApiKey ? AssistantCopy.BadgeNoKey
        : !Eligibility.ProposalsEnabled ? AssistantCopy.BadgeEvalNeeded
        : AssistantCopy.BadgeKeySet;

    public event Action? StateChanged;

    public async Task<AssistantStatusSnapshot> RefreshStatusAsync(CancellationToken ct = default)
    {
        var snapshot = await Service.GetStatusAsync(ct).ConfigureAwait(false);
        CurrentSettings = snapshot.Settings;
        Eligibility = snapshot.Eligibility;
        FastModelEligibility = ComputeFastModelEligibility(CurrentSettings);
        DailyRequestsUsedUtc = snapshot.DailyRequestsUsedUtc;
        StateChanged?.Invoke();
        return snapshot;
    }

    public CredentialStoreResult SaveKey(AssistantProviderKind providerKind, string apiKey)
    {
        var res = Credentials.WriteKey(providerKind, apiKey);
        if (res.Succeeded)
            StateChanged?.Invoke();
        return res;
    }

    public CredentialStoreResult RemoveKey(AssistantProviderKind providerKind)
    {
        var res = Credentials.DeleteKey(providerKind);
        if (res.Succeeded)
            StateChanged?.Invoke();
        return res;
    }

    public async Task<AssistantSettingsSaveResult> SaveSettingsAsync(AssistantSettings updated, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updated);
        var endpoint = ByokLlmTransport.ValidateEndpointUri(updated.EndpointUrl);
        if (!endpoint.IsValid)
            return new AssistantSettingsSaveResult(false, "rejected", endpoint.ErrorCode, endpoint.Message);

        if (ContainsSecret(updated.ModelId) || ContainsSecret(updated.OptionalFastModelId))
            return new AssistantSettingsSaveResult(false, "rejected", "ASSISTANT-SECRET-FORBIDDEN", AssistantCopy.FastModelSecretError);

        var res = await SettingsStore.SaveSettingsAsync(updated, ct).ConfigureAwait(false);
        if (res.Succeeded)
        {
            CurrentSettings = updated;
            Eligibility = AssistantEvalSuite.CheckModelEligibility(CurrentSettings, CurrentSettings.EvalReceipts);
            FastModelEligibility = ComputeFastModelEligibility(CurrentSettings);
            StateChanged?.Invoke();
        }
        return res;
    }

    public Task<AssistantEvalRunResult> RunEvaluationAsync(bool liveWire = false, CancellationToken ct = default) =>
        RunEvaluationAsync(liveWire, targetModelId: null, ct: ct);

    public async Task<AssistantEvalRunResult> RunEvaluationAsync(
        bool liveWire,
        string? targetModelId,
        CancellationToken ct = default)
    {
        var res = await Service.RunEvaluationAsync(liveWire, targetModelId, ct).ConfigureAwait(false);
        var loaded = await SettingsStore.LoadSettingsAsync(ct).ConfigureAwait(false);
        CurrentSettings = loaded.Settings;
        Eligibility = AssistantEvalSuite.CheckModelEligibility(CurrentSettings, CurrentSettings.EvalReceipts);
        FastModelEligibility = ComputeFastModelEligibility(CurrentSettings);
        StateChanged?.Invoke();
        return res;
    }

    private static ModelEligibilityStatus? ComputeFastModelEligibility(AssistantSettings settings) =>
        string.IsNullOrWhiteSpace(settings.OptionalFastModelId)
            ? null
            : AssistantEvalSuite.CheckModelEligibility(settings, settings.EvalReceipts, settings.OptionalFastModelId);

    private static bool ContainsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        PromptRedactor.ScrubSecrets(value, RedactionContext.Empty).SecretsRedacted > 0;

    public Task<IReadOnlyList<AssistantAuditEntry>> ReadAuditEntriesAsync(int maxEntries = 200, CancellationToken ct = default) =>
        SettingsStore.ReadAuditEntriesAsync(maxEntries, ct);

    public Task<AssistantPreviewResult> PreparePreviewAsync(
        AssistantPreviewRequest request,
        RedactionContext? redaction = null,
        CancellationToken ct = default) =>
        Service.PreparePreviewAsync(request, redaction, ct);

    public async Task<AssistantExecutionResult> ExecuteConsentedAsync(
        PreparedAssistantCall prepared,
        string consentedPayloadSha256,
        AssistantValidationInput validationInput,
        CancellationToken ct = default)
    {
        var result = await Service.ExecuteConsentedAsync(prepared, consentedPayloadSha256, validationInput, ct).ConfigureAwait(false);
        DailyRequestsUsedUtc = await SettingsStore.CountDailyRequestsUtcAsync(DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime), ct).ConfigureAwait(false);
        StateChanged?.Invoke();
        return result;
    }

    public async Task<AssistantAuditEntry> RecordDeclinedAsync(PreparedAssistantCall prepared, CancellationToken ct = default)
    {
        var entry = await Service.RecordDeclinedAsync(prepared, ct).ConfigureAwait(false);
        StateChanged?.Invoke();
        return entry;
    }

    private void OnTelemetryEmitted(AssistantTelemetryEvent evt)
    {
        LastTelemetry = evt;
        CfdWorkbench.Desktop.Shell.ShellEvents.Record(
            evt.EventName,
            evt.Verdict,
            evt.DurationMs ?? 0,
            evt.RequestId,
            code: evt.ErrorCode);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Service.TelemetryEmitted -= OnTelemetryEmitted;
        transport.Dispose();
    }
}
