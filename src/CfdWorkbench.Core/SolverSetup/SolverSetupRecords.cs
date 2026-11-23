namespace CfdWorkbench.Core.SolverSetup;

public enum SetupOs { Windows, MacOs, Other }

public enum SetupArch { X64, Arm64, Other }

public enum StepKind { Survey, Download, Verify, Place, OsHandoff, Run, Smoke, Undo }

public enum StepOutcome { Started, Succeeded, Failed, Declined, Refused, NeedsRestart, Interrupted }

public enum BackendCheckKind { Identify, Smoke }

public enum SolverReadiness { NotSetUp, SettingUp, Ready, NotReady, Blocked, NeedsRestart, ExistingUnverified }

public enum SetupScreenKind
{
    RouteSelection,
    StepReady,
    WaitingForOs,
    NeedsRestart,
    Running,
    ExistingMatching,
    ExistingUnverified,
    FailedKnown,
    FailedUnknown,
    Ready,
    NoRoute
}

public enum OpenFoamApp
{
    FoamVersion,
    BlockMesh,
    IcoFoam,
    CheckMesh,
    SimpleFoam,
    PotentialFoam,
    PimpleFoam,
    DecomposePar,
    ReconstructPar
}

public enum AssistantVerdict { Shown, Withheld, Refused }

public sealed record RoutePin(
    string Id,
    string FileName,
    string Url,
    long SizeBytes,
    string Sha256,
    string? LicenseId = null,
    string? LicenseText = null);

public sealed record CauseEntry(
    string Code,
    string Summary,
    string NextStep,
    string? SuggestedStepId = null,
    string Label = "Inferred");

public sealed record StepDefinition(
    string Id,
    StepKind Kind,
    string Title,
    string PlainWhy,
    string PlainWhat,
    string TimeEstimate,
    string DownloadEstimate,
    bool NeedsAdmin,
    bool NeedsRestart,
    bool Reversible,
    string? UndoStepId,
    string DoneCheckId,
    string TechnicalCommand,
    string? TermsId,
    IReadOnlyList<string> CauseCodes);

public sealed record RouteDefinition(
    string Id,
    string Version,
    string DisplayName,
    string Reason,
    SetupOs Os,
    IReadOnlyList<SetupArch> SupportedArches,
    int MinOsBuild,
    string BackendName,
    string BackendVersion,
    long RequiredDiskBytes,
    string? ExpectedBuildId,
    string? SmokeScalarName,
    double? SmokeScalarReference,
    double? SmokeRelativeTolerance,
    IReadOnlyList<RoutePin> Pins,
    IReadOnlyList<StepDefinition> Steps,
    IReadOnlyList<string> UndoStepIds,
    IReadOnlyList<CauseEntry> Causes);

public sealed record FirmwareClickPath(
    string MakerKey,
    string DisplayMaker,
    string KeyPrompt,
    IReadOnlyList<string> Steps,
    string Label = "Flagged");

public sealed record ExistingInstallInfo(
    string Backend,
    string Version,
    string InstallRoot,
    string InstallIdentitySha256,
    bool MatchesPin,
    bool IsSupportedVersion);

public sealed record WslDistroInfo(
    string Name,
    string State,
    int Version,
    bool IsDefault);

/// <summary>Append-only Host survey fact (docs/design/guided-solver-setup.md §2).</summary>
public sealed record HostSurveyFact(
    string FactId,
    DateTimeOffset AtUtc,
    SetupOs Os,
    string OsName,
    int OsBuild,
    SetupArch Arch,
    long? FreeDiskBytes,
    bool? RebootPending,
    bool? VirtualizationFirmwareEnabled,
    bool? HypervisorPresent,
    bool? WslAvailable,
    int? WslDefaultVersion,
    IReadOnlyList<WslDistroInfo> WslDistros,
    IReadOnlyList<ExistingInstallInfo> ExistingInstalls,
    string? DeviceMaker,
    bool? ManagedDeviceHint,
    bool? ProxyDetected,
    long DurationMs);

/// <summary>Append-only per-step Consent fact (A5.10, DR-SETUP-5).</summary>
public sealed record ConsentRecordFact(
    string ConsentId,
    DateTimeOffset AtUtc,
    string SessionId,
    string RouteId,
    string StepId,
    string TechnicalCommandShown,
    string? TermsCopyId,
    bool TermsAccepted);

/// <summary>Append-only Environment step attempt fact (<c>environment.step</c>).</summary>
public sealed record EnvironmentStepFact(
    string FactId,
    string SessionId,
    string RouteId,
    string RouteVersion,
    string StepId,
    string? ConsentId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? EndedUtc,
    StepOutcome Outcome,
    string? CauseCode,
    int? ExitCode,
    bool ElevationUsed,
    long BytesDownloaded,
    string? OutputExcerptRedacted,
    long? DurationMs);

/// <summary>Append-only Install acceptance fact (Type-2 by install identity hash; DR-SEC-1 A).</summary>
public sealed record InstallAcceptanceFact(
    string FactId,
    DateTimeOffset AtUtc,
    string InstallIdentitySha256,
    string RouteId,
    string DisclosureCopyId);

/// <summary>Append-only Backend check fact (identify or smoke; sole source of Ready, A3.1, SETUP-08).</summary>
public sealed record BackendCheckFact(
    string FactId,
    DateTimeOffset AtUtc,
    string RouteId,
    string Backend,
    string Version,
    string InstallRoot,
    string InstallIdentitySha256,
    bool VerifiedInstall,
    BackendCheckKind Kind,
    bool Passed,
    string? BuildId,
    string? BannerStatus,
    string? SmokeScalarName,
    double? SmokeScalarValue,
    double? SmokeRelativeTolerance,
    bool OutputFilesPresent,
    long DurationMs,
    string? CauseCode);

/// <summary>Discriminated append-only setup fact envelope stored as one JSONL line.</summary>
public sealed record SetupFactEnvelope(
    string Kind,
    DateTimeOffset AtUtc,
    HostSurveyFact? Survey = null,
    ConsentRecordFact? Consent = null,
    EnvironmentStepFact? Step = null,
    InstallAcceptanceFact? Acceptance = null,
    BackendCheckFact? Check = null)
{
    public const string KindSurvey = "setup.survey";
    public const string KindConsent = "setup.consent";
    public const string KindStep = "environment.step";
    public const string KindAcceptance = "install.acceptance";
    public const string KindCheck = "backend.check";

    public static SetupFactEnvelope ForSurvey(HostSurveyFact fact) =>
        new(KindSurvey, fact.AtUtc, Survey: fact);

    public static SetupFactEnvelope ForConsent(ConsentRecordFact fact) =>
        new(KindConsent, fact.AtUtc, Consent: fact);

    public static SetupFactEnvelope ForStep(EnvironmentStepFact fact) =>
        new(KindStep, fact.EndedUtc ?? fact.StartedUtc, Step: fact);

    public static SetupFactEnvelope ForAcceptance(InstallAcceptanceFact fact) =>
        new(KindAcceptance, fact.AtUtc, Acceptance: fact);

    public static SetupFactEnvelope ForCheck(BackendCheckFact fact) =>
        new(KindCheck, fact.AtUtc, Check: fact);
}

/// <summary>Derived setup session state (never stored; resume = re-derivation, R5, SETUP-05).</summary>
public sealed record SetupSessionState(
    string? SessionId,
    string? RouteId,
    SetupScreenKind Screen,
    IReadOnlyList<string> AllowedNext,
    string? RailsNext,
    string? ActiveStepId,
    bool HasOpenAttempt,
    bool ResumedFromInterruption,
    bool Ready,
    bool VerifiedInstall,
    string? CauseCode,
    string? CauseSummary,
    string? NextStepText,
    int CompletedSteps,
    int TotalSteps,
    double? ProgressFraction,
    string? BlockerReason);

/// <summary>Derived solver readiness summary for StatusStrip and CLI parity.</summary>
public sealed record SolverStatusSummary(
    SolverReadiness Readiness,
    string StripLabel,
    string? Backend,
    string? Version,
    string? BuildId,
    bool VerifiedInstall,
    int? CurrentStepNumber,
    int? TotalSteps,
    double? ProgressFraction,
    DateTimeOffset? LastVerifiedUtc,
    long? LastSmokeDurationMs,
    string? Reason);
