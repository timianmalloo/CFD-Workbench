namespace CfdWorkbench.Core.Assistant;

public enum AssistantArea
{
    Setup,
    Cad,
    Analysis,
    Experiment,
    Run,
    SolverSetup,
    Results,
    Export,
    Notebook
}

public enum AssistantProposalKind
{
    None,
    SetupSeed,
    GeometryEdit,
    AnalysisExplanation,
    ExperimentConfig,
    EnvironmentStep,
    RunFailureExplanation,
    CaseDiff,
    ResultsExplanation,
    RequirementDecomp,
    CandidateIntentDelta
}

public enum AssistantProviderKind
{
    Anthropic,
    OpenAiCompatible
}

public enum ProvenanceTag
{
    Stated,
    Inferred,
    Defaulted
}

public enum ProposalDisposition
{
    Pending,
    Accepted,
    Discarded,
    Refused,
    Withheld
}

public sealed record AssistantFieldProvenance(
    string FieldName,
    ProvenanceTag Tag,
    string ValueDisplay,
    string Reason);

/// <summary>
/// Closed-DTO installation-local settings (cfdw-assistant v1).
/// INVARIANT: Contains ZERO API key, secret, or token property.
/// </summary>
public sealed record AssistantSettings(
    string Format,
    int Version,
    AssistantProviderKind ProviderKind,
    string EndpointUrl,
    string ModelId,
    int PerRequestMaxTokens,
    int DailyRequestCap,
    int TimeoutSeconds,
    IReadOnlyList<AssistantModelEvalReceipt> EvalReceipts,
    string? OptionalFastModelId = null)
{
    public const string FormatId = "cfdw-assistant";
    public const int CurrentVersion = 1;
    public const int MinPerRequestMaxTokens = 128;
    public const int MaxPerRequestMaxTokens = 16_384;
    public const int DefaultPerRequestMaxTokens = 2_048;
    public const int MinDailyRequestCap = 1;
    public const int MaxDailyRequestCap = 500;
    public const int DefaultDailyRequestCap = 50;
    public const int MinTimeoutSeconds = 5;
    public const int MaxTimeoutSeconds = 120;
    public const int DefaultTimeoutSeconds = 30;
    public const string DefaultAnthropicEndpoint = "https://api.anthropic.com";
    public const string DefaultOpenAiEndpoint = "https://api.openai.com";

    public static AssistantSettings CreateDefault() => new(
        Format: FormatId,
        Version: CurrentVersion,
        ProviderKind: AssistantProviderKind.Anthropic,
        EndpointUrl: DefaultAnthropicEndpoint,
        ModelId: "claude-sonnet-4-5",
        PerRequestMaxTokens: DefaultPerRequestMaxTokens,
        DailyRequestCap: DefaultDailyRequestCap,
        TimeoutSeconds: DefaultTimeoutSeconds,
        EvalReceipts: []);
}

public sealed record AssistantEvalItemResult(
    string ItemId,
    string Title,
    int PassedCases,
    int TotalCases,
    double PassRate,
    double Threshold,
    bool Reserved,
    bool MetThreshold);

public sealed record AssistantModelEvalReceipt(
    string ReceiptId,
    AssistantProviderKind ProviderKind,
    string EndpointOrigin,
    string ModelId,
    string EvalThresholdsVersion,
    DateTimeOffset EvaluatedAtUtc,
    bool AllActiveItemsMetThreshold,
    IReadOnlyList<AssistantEvalItemResult> Items,
    string ContentHash);

public sealed record AssistantAuditEntry(
    string RequestId,
    DateTimeOffset TimestampUtc,
    AssistantArea Area,
    AssistantProposalKind ProposalKind,
    AssistantProviderKind ProviderKind,
    string EndpointOrigin,
    string ModelId,
    string PayloadSha256,
    int PromptUtf8Bytes,
    string ConsentAction,
    string Verdict,
    int? UsageInputTokens,
    int? UsageOutputTokens,
    long? DurationMs,
    string? ErrorCode);

public sealed record CredentialStoreResult(
    bool Succeeded,
    string? Secret,
    string? ErrorCode,
    string? Message);

public interface ICredentialStore
{
    bool IsSupported { get; }
    bool HasKey(AssistantProviderKind providerKind);
    CredentialStoreResult ReadKey(AssistantProviderKind providerKind);
    CredentialStoreResult WriteKey(AssistantProviderKind providerKind, string apiKey);
    CredentialStoreResult DeleteKey(AssistantProviderKind providerKind);
}

public sealed record AssistantSettingsLoadResult(
    AssistantSettings Settings,
    string Outcome,
    IReadOnlyList<string> Codes,
    bool NeverWrite,
    bool SessionOnly);

public sealed record AssistantSettingsSaveResult(
    bool Succeeded,
    string Outcome,
    string? Code,
    string? Message);

public interface IAssistantSettingsStore
{
    Task<AssistantSettingsLoadResult> LoadSettingsAsync(CancellationToken ct = default);
    Task<AssistantSettingsSaveResult> SaveSettingsAsync(AssistantSettings settings, CancellationToken ct = default);
    Task<bool> AppendAuditEntryAsync(AssistantAuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AssistantAuditEntry>> ReadAuditEntriesAsync(int maxEntries = 200, CancellationToken ct = default);
    Task<int> CountDailyRequestsUtcAsync(DateOnly utcDate, CancellationToken ct = default);
}
