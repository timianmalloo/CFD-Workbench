using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.SolverSetup;

public sealed record SurveyRunResult(
    HostSurveyFact Survey,
    RouteDefinition? RecommendedRoute,
    IReadOnlyList<RouteDefinition> AvailableRoutes,
    string? BlockerCauseCode);

/// <summary>
/// Guided Solver Setup orchestrator combining <see cref="SetupFactStore"/>, <see cref="IHostSurveyProbe"/>,
/// <see cref="SolverLauncher"/>, <see cref="IRunOnceStore"/>, and the pure Core policy/selector.
/// </summary>
public sealed class SolverSetupService
{
    public const string RunOnceEntryName = "CFDWorkbenchSolverSetupResume";

    private readonly SetupFactStore store;
    private readonly IHostSurveyProbe surveyProbe;
    private readonly IRunOnceStore runOnceStore;
    private readonly SolverLauncher launcher;

    public SolverSetupService(
        string? storeDirectory = null,
        IHostSurveyProbe? surveyProbe = null,
        IRunOnceStore? runOnceStore = null,
        IProcessRunner? processRunner = null)
    {
        RouteCatalogue.VerifyManifest();
        store = new SetupFactStore(storeDirectory);
        this.surveyProbe = surveyProbe ?? CreateDefaultSurveyProbe();
        this.runOnceStore = runOnceStore ?? new InMemoryRunOnceStore();
        launcher = new SolverLauncher(store.DirectoryPath, processRunner);
    }

    public SetupFactStore Store => store;
    public SolverLauncher Launcher => launcher;

    public static IHostSurveyProbe CreateDefaultSurveyProbe() =>
        OperatingSystem.IsMacOS() ? new MacHostSurveyProbe() : new WindowsHostSurveyProbe();

    /// <summary>Closes any interrupted attempts from a prior crash/restart and clears any one-time RunOnce resume entry.</summary>
    public IReadOnlyList<SetupFactEnvelope> RecoverOnLaunch(DateTimeOffset? nowUtc = null)
    {
        runOnceStore.Remove(RunOnceEntryName);
        return store.RecoverInterruptedOnLaunch(nowUtc);
    }

    /// <summary>Runs the read-only host survey without consent and appends a <see cref="HostSurveyFact"/>.</summary>
    public async Task<SurveyRunResult> RunSurveyAsync(CancellationToken cancellationToken = default)
    {
        var survey = await surveyProbe.SurveyAsync(cancellationToken);
        store.Append(SetupFactEnvelope.ForSurvey(survey));
        var (recommended, blocker) = StepSelector.RecommendRoute(survey);
        var available = StepSelector.AvailableRoutes(survey);
        return new SurveyRunResult(survey, recommended, available, blocker);
    }

    public ConsentRecordFact RecordConsent(
        string sessionId,
        string routeId,
        string stepId,
        string technicalCommandShown,
        string? termsCopyId,
        bool termsAccepted)
    {
        var route = RouteCatalogue.GetRoute(routeId);
        if (!route.Steps.Any(s => s.Id == stepId) && !route.UndoStepIds.Contains(stepId, StringComparer.Ordinal))
            throw new Core.ContractError("SETUP-STEP-UNKNOWN", $"Step '{stepId}' does not belong to route '{routeId}'.");

        var consent = new ConsentRecordFact(
            ConsentId: "consent-" + Guid.NewGuid().ToString("N")[..12],
            AtUtc: DateTimeOffset.UtcNow,
            SessionId: sessionId,
            RouteId: routeId,
            StepId: stepId,
            TechnicalCommandShown: technicalCommandShown,
            TermsCopyId: termsCopyId,
            TermsAccepted: termsAccepted);
        store.Append(SetupFactEnvelope.ForConsent(consent));
        return consent;
    }

    public InstallAcceptanceFact AcceptUnverifiedInstall(
        string routeId,
        string installIdentitySha256,
        string disclosureCopyId = "COPY-UNVERIFIED-INSTALL")
    {
        RouteCatalogue.GetRoute(routeId);
        var fact = new InstallAcceptanceFact(
            FactId: "acc-" + Guid.NewGuid().ToString("N")[..12],
            AtUtc: DateTimeOffset.UtcNow,
            InstallIdentitySha256: installIdentitySha256,
            RouteId: routeId,
            DisclosureCopyId: disclosureCopyId);
        store.Append(SetupFactEnvelope.ForAcceptance(fact));
        return fact;
    }

    public void RecordStepOutcome(EnvironmentStepFact stepFact) =>
        store.Append(SetupFactEnvelope.ForStep(stepFact));

    public void RecordBackendCheck(BackendCheckFact checkFact) =>
        store.Append(SetupFactEnvelope.ForCheck(checkFact));

    /// <summary>Registers a one-time per-user restart-resume entry via injected <see cref="IRunOnceStore"/> (DR-SETUP-6).</summary>
    public void RegisterRestartResume(string commandLine) =>
        runOnceStore.RegisterResume(RunOnceEntryName, commandLine);

    public SolverSetupViewModel Project(string? selectedRouteId = null, string? sessionId = null, bool hasApiKey = false)
    {
        var facts = store.ReadAll();
        HostSurveyFact? latestSurvey = null;
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindSurvey && facts[i].Survey is not null)
            {
                latestSurvey = facts[i].Survey;
                break;
            }
        }

        RouteDefinition? route = null;
        if (!string.IsNullOrWhiteSpace(selectedRouteId) && RouteCatalogue.TryGetRoute(selectedRouteId, out var explicitRoute))
            route = explicitRoute;
        else if (latestSurvey is not null)
            route = StepSelector.RecommendRoute(latestSurvey).Route;

        return SolverSetupProjection.Build(route, latestSurvey, facts, sessionId, hasApiKey);
    }

    public string BuildSupportReport(string appVersion, string? selectedRouteId = null, IReadOnlyList<string>? extraSecrets = null)
    {
        var facts = store.ReadAll();
        HostSurveyFact? latestSurvey = null;
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindSurvey && facts[i].Survey is not null)
            {
                latestSurvey = facts[i].Survey;
                break;
            }
        }

        RouteDefinition? route = null;
        if (!string.IsNullOrWhiteSpace(selectedRouteId) && RouteCatalogue.TryGetRoute(selectedRouteId, out var explicitRoute))
            route = explicitRoute;
        else if (latestSurvey is not null)
            route = StepSelector.RecommendRoute(latestSurvey).Route;

        return SetupReportBuilder.BuildReport(
            appVersion, route, latestSurvey, facts,
            currentUserName: Environment.UserName,
            currentMachineName: Environment.MachineName,
            extraSecrets: extraSecrets);
    }

    public AssistantValidationResult ValidateAssistantResponse(string rawJson, AssistantValidationContext context) =>
        SetupAssistantValidator.Validate(rawJson, context);
}
