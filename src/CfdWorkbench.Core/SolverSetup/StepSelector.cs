namespace CfdWorkbench.Core.SolverSetup;

/// <summary>
/// Pure zero-I/O step selector and state derivation (docs/design/guided-solver-setup.md §2 &amp; §5).
/// Enforces R1-R5, one-step-at-a-time invariant, resume = re-derivation (SETUP-05),
/// existing-install Type-2 identity acceptance (DR-SEC-1 A, SETUP-07), and Ready from BackendCheck facts only (SETUP-08).
/// </summary>
public static class StepSelector
{
    public static (RouteDefinition? Route, string? BlockerCauseCode) RecommendRoute(HostSurveyFact survey)
    {
        if (survey.Os == SetupOs.MacOs)
        {
            if (survey.Arch != SetupArch.Arm64) return (null, "mac.intel");
            if (survey.OsBuild < 14) return (null, "mac.os-too-old");
            return (RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId), null);
        }

        if (survey.Os == SetupOs.Windows)
        {
            if (survey.OsBuild < 19041) return (null, "win.build-too-old");
            return (RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId), null);
        }

        return (null, "unknown");
    }

    public static IReadOnlyList<RouteDefinition> AvailableRoutes(HostSurveyFact survey)
    {
        var list = new List<RouteDefinition>();
        foreach (var route in RouteCatalogue.AllRoutes)
        {
            if (route.Os != survey.Os) continue;
            if (!route.SupportedArches.Contains(survey.Arch)) continue;
            if (survey.OsBuild < route.MinOsBuild) continue;
            list.Add(route);
        }
        return list;
    }

    /// <summary>Finds open <see cref="StepOutcome.Started"/> attempts with no subsequent terminal fact.</summary>
    public static IReadOnlyList<EnvironmentStepFact> FindOpenAttempts(IReadOnlyList<SetupFactEnvelope> facts, string? sessionId = null)
    {
        var open = new Dictionary<(string Session, string Step, DateTimeOffset Started), EnvironmentStepFact>();
        foreach (var env in facts)
        {
            if (env.Kind != SetupFactEnvelope.KindStep || env.Step is not { } step) continue;
            if (sessionId is not null && !string.Equals(step.SessionId, sessionId, StringComparison.Ordinal)) continue;
            var key = (step.SessionId, step.StepId, step.StartedUtc);
            if (step.Outcome == StepOutcome.Started)
                open[key] = step;
            else
                open.Remove(key);
        }
        return open.Values.OrderBy(s => s.StartedUtc).ToArray();
    }

    /// <summary>Produces closing <see cref="StepOutcome.Interrupted"/> facts for every open attempt (SETUP-05).</summary>
    public static IReadOnlyList<SetupFactEnvelope> CloseInterrupted(IReadOnlyList<SetupFactEnvelope> facts, DateTimeOffset closedAtUtc)
    {
        var open = FindOpenAttempts(facts);
        if (open.Count == 0) return [];
        var closed = new List<SetupFactEnvelope>(open.Count);
        foreach (var attempt in open)
        {
            long elapsed = Math.Max(0L, (long)(closedAtUtc - attempt.StartedUtc).TotalMilliseconds);
            var closedStep = attempt with
            {
                FactId = attempt.FactId + "-interrupted",
                EndedUtc = closedAtUtc,
                Outcome = StepOutcome.Interrupted,
                DurationMs = elapsed
            };
            closed.Add(SetupFactEnvelope.ForStep(closedStep));
        }
        return closed;
    }

    /// <summary>
    /// Pure allowed-next derivation. Enforces the one-step-at-a-time invariant: returns empty whenever an open
    /// <see cref="StepOutcome.Started"/> attempt exists for this session.
    /// </summary>
    public static IReadOnlyList<string> AllowedNext(
        RouteDefinition route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId = null)
    {
        if (FindOpenAttempts(facts, sessionId).Count > 0)
            return [];

        if (survey is null)
            return [route.Steps[0].Id];

        if (survey.Os != route.Os || !route.SupportedArches.Contains(survey.Arch) || survey.OsBuild < route.MinOsBuild)
            return [];

        var allowed = new List<string>();
        var rails = ComputeRailsStepId(route, survey, facts, sessionId);
        if (rails is not null)
            allowed.Add(rails);

        // Include reversible undo steps ONLY for artifacts that a setup EnvironmentStepFact actually placed
        // (never for pre-existing user installs, §3.4 / F7a-10).
        foreach (string undoId in route.UndoStepIds)
        {
            if (CanOfferUndo(route, undoId, facts) && !allowed.Contains(undoId, StringComparer.Ordinal))
                allowed.Add(undoId);
        }

        return allowed;
    }

    /// <summary>First non-undo step in <see cref="AllowedNext"/> whose done-check is false (R1, R5).</summary>
    public static string? RailsNext(
        RouteDefinition route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId = null)
    {
        if (FindOpenAttempts(facts, sessionId).Count > 0)
            return null;
        if (survey is null)
            return route.Steps[0].Id;
        if (survey.Os != route.Os || !route.SupportedArches.Contains(survey.Arch) || survey.OsBuild < route.MinOsBuild)
            return null;
        return ComputeRailsStepId(route, survey, facts, sessionId);
    }

    /// <summary>
    /// Ready is strictly derived from passing Identify + Smoke <see cref="BackendCheckFact"/> facts (R4, SETUP-08, M2).
    /// No stored boolean or user action can ever set Ready.
    /// </summary>
    public static bool IsReady(RouteDefinition route, IReadOnlyList<SetupFactEnvelope> facts)
    {
        var (identify, smoke) = LatestChecks(route.Id, facts);
        if (identify is null || smoke is null) return false;
        if (!identify.Passed || !smoke.Passed) return false;
        if (!string.Equals(identify.InstallIdentitySha256, smoke.InstallIdentitySha256, StringComparison.Ordinal))
            return false;
        if (!smoke.OutputFilesPresent) return false;

        bool hasValidAcceptance = HasMatchingAcceptance(route.Id, smoke.InstallIdentitySha256, facts);

        if (string.Equals(route.BackendName, "OpenFOAM", StringComparison.Ordinal))
        {
            if (!string.Equals(smoke.BannerStatus, "Disallowing", StringComparison.Ordinal))
                return false;
            if (!smoke.VerifiedInstall && !hasValidAcceptance)
                return false;
            if (smoke.VerifiedInstall && !string.IsNullOrEmpty(route.ExpectedBuildId) &&
                !string.Equals(smoke.BuildId, route.ExpectedBuildId, StringComparison.Ordinal))
                return false;
        }
        else if (!smoke.VerifiedInstall && !hasValidAcceptance)
        {
            return false;
        }

        if (route.SmokeScalarReference is { } reference && route.SmokeRelativeTolerance is { } tol)
        {
            if (smoke.SmokeScalarValue is not { } observed || !double.IsFinite(observed))
                return false;
            double denom = Math.Max(Math.Abs(reference), 1e-12);
            if (Math.Abs(observed - reference) / denom > tol)
                return false;
        }

        return true;
    }

    public static SetupSessionState DeriveSessionState(
        RouteDefinition? route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId = null)
    {
        var latestSurvey = survey ?? LatestSurvey(facts);
        var openAttempts = FindOpenAttempts(facts, sessionId);
        bool hasOpen = openAttempts.Count > 0;
        bool resumed = facts.Any(f =>
            f.Kind == SetupFactEnvelope.KindStep &&
            f.Step is { Outcome: StepOutcome.Interrupted } s &&
            (sessionId is null || s.SessionId == sessionId));

        if (route is null)
        {
            if (latestSurvey is not null)
            {
                var (rec, blocker) = RecommendRoute(latestSurvey);
                if (rec is null)
                {
                    var cause = RouteCatalogue.GetCause(blocker);
                    return new SetupSessionState(
                        sessionId, null, SetupScreenKind.NoRoute, [], null, null, hasOpen, resumed,
                        false, false, cause.Code, cause.Summary, cause.NextStep, 0, 0, null, cause.Code);
                }
            }
            return new SetupSessionState(
                sessionId, null, SetupScreenKind.RouteSelection, [], null, null, hasOpen, resumed,
                false, false, null, null, null, 0, 0, null, null);
        }

        if (latestSurvey is not null &&
            (latestSurvey.Os != route.Os || !route.SupportedArches.Contains(latestSurvey.Arch) || latestSurvey.OsBuild < route.MinOsBuild))
        {
            string blocker = latestSurvey.Os == SetupOs.MacOs
                ? (latestSurvey.Arch != SetupArch.Arm64 ? "mac.intel" : "mac.os-too-old")
                : (latestSurvey.Os == SetupOs.Windows && latestSurvey.OsBuild < 19041 ? "win.build-too-old" : "unknown");
            var cause = RouteCatalogue.GetCause(blocker);
            return new SetupSessionState(
                sessionId, route.Id, SetupScreenKind.NoRoute, [], null, null, hasOpen, resumed,
                false, false, cause.Code, cause.Summary, cause.NextStep, 0, route.Steps.Count, null, cause.Code);
        }

        bool ready = IsReady(route, facts);
        var (_, latestSmoke) = LatestChecks(route.Id, facts);
        bool verified = latestSmoke?.VerifiedInstall ?? false;
        int totalSteps = route.Steps.Count;
        int completedSteps = latestSurvey is null
            ? 0
            : route.Steps.Count(step => IsStepDone(route, step.Id, latestSurvey, facts, sessionId));

        if (ready)
        {
            return new SetupSessionState(
                sessionId, route.Id, SetupScreenKind.Ready,
                AllowedNext(route, latestSurvey, facts, sessionId),
                null, null, false, resumed, true, verified,
                null, null, null, totalSteps, totalSteps, 1.0, null);
        }

        var allowed = AllowedNext(route, latestSurvey, facts, sessionId);
        string? rails = RailsNext(route, latestSurvey, facts, sessionId);

        if (hasOpen)
        {
            var open = openAttempts[^1];
            var openDef = route.Steps.FirstOrDefault(s => s.Id == open.StepId);
            var screen = openDef?.Kind == StepKind.OsHandoff ? SetupScreenKind.WaitingForOs : SetupScreenKind.Running;
            return new SetupSessionState(
                sessionId, route.Id, screen, [], null, open.StepId, true, resumed,
                false, false, null, null, null, completedSteps, totalSteps, null, null);
        }

        // Check for existing unverified install awaiting decision (DR-SEC-1 A).
        var existing = FindSupportedExistingInstall(route, latestSurvey);
        if (existing is { MatchesPin: false } && !HasMatchingAcceptance(route.Id, existing.InstallIdentitySha256, facts))
        {
            // Only show ExistingUnverified if the user hasn't already started downloading the tested build.
            bool downloadedTested = HasStepSucceeded(route.Id, "ofapp.download", facts, sessionId) ||
                                    HasStepSucceeded(route.Id, "of.install", facts, sessionId) ||
                                    HasStepSucceeded(route.Id, "su2.download", facts, sessionId);
            if (!downloadedTested)
            {
                return new SetupSessionState(
                    sessionId, route.Id, SetupScreenKind.ExistingUnverified, allowed, rails, rails,
                    false, resumed, false, false, null, null, null, completedSteps, totalSteps, null, null);
            }
        }

        if (rails == "os.restart")
        {
            return new SetupSessionState(
                sessionId, route.Id, SetupScreenKind.NeedsRestart, allowed, rails, rails,
                false, resumed, false, false, null, null, null, completedSteps, totalSteps, null, null);
        }

        // Check latest failed step for this route/session.
        var lastStep = LatestStepFact(route.Id, facts, sessionId);
        if (lastStep is { Outcome: StepOutcome.Failed or StepOutcome.Refused or StepOutcome.Declined })
        {
            string code = string.IsNullOrWhiteSpace(lastStep.CauseCode) ? "unknown" : lastStep.CauseCode;
            var cause = RouteCatalogue.GetCause(code);
            var screen = code == "unknown" ? SetupScreenKind.FailedUnknown : SetupScreenKind.FailedKnown;
            return new SetupSessionState(
                sessionId, route.Id, screen, allowed, rails, lastStep.StepId,
                false, resumed, false, false, cause.Code, cause.Summary, cause.NextStep,
                completedSteps, totalSteps, null, null);
        }

        if (existing is { MatchesPin: true } && completedSteps <= 2)
        {
            return new SetupSessionState(
                sessionId, route.Id, SetupScreenKind.ExistingMatching, allowed, rails, rails,
                false, resumed, false, true, null, null, null, completedSteps, totalSteps, null, null);
        }

        return new SetupSessionState(
            sessionId, route.Id, SetupScreenKind.StepReady, allowed, rails, rails,
            false, resumed, false, verified, null, null, null, completedSteps, totalSteps, null, null);
    }

    public static SolverStatusSummary DeriveSolverStatus(
        RouteDefinition? route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId = null)
    {
        if (route is null)
        {
            // Check if any route already has Ready derived from BackendCheck facts.
            foreach (var candidate in RouteCatalogue.AllRoutes)
            {
                if (IsReady(candidate, facts))
                {
                    var (_, smoke) = LatestChecks(candidate.Id, facts);
                    return new SolverStatusSummary(
                        SolverReadiness.Ready, "Solver: Ready",
                        candidate.BackendName, candidate.BackendVersion, smoke?.BuildId,
                        smoke?.VerifiedInstall ?? false, null, null, null,
                        smoke?.AtUtc, smoke?.DurationMs, null);
                }
            }
            return new SolverStatusSummary(
                SolverReadiness.NotSetUp, "Solver: not set up",
                null, null, null, false, null, null, null, null, null, null);
        }

        var state = DeriveSessionState(route, survey, facts, sessionId);
        if (state.Ready)
        {
            var (_, smoke) = LatestChecks(route.Id, facts);
            return new SolverStatusSummary(
                SolverReadiness.Ready, "Solver: Ready",
                route.BackendName, route.BackendVersion, smoke?.BuildId,
                smoke?.VerifiedInstall ?? false, null, null, null,
                smoke?.AtUtc, smoke?.DurationMs, null);
        }

        if (state.Screen == SetupScreenKind.NoRoute)
        {
            return new SolverStatusSummary(
                SolverReadiness.Blocked, "Solver: blocked",
                route.BackendName, route.BackendVersion, null, false,
                null, null, null, null, null, state.CauseSummary);
        }

        if (state.Screen == SetupScreenKind.NeedsRestart)
        {
            return new SolverStatusSummary(
                SolverReadiness.NeedsRestart, "Solver: restart needed",
                route.BackendName, route.BackendVersion, null, false,
                Math.Min(state.CompletedSteps + 1, state.TotalSteps), state.TotalSteps, null,
                null, null, "Restart needed to finish turning on WSL.");
        }

        if (state.Screen == SetupScreenKind.ExistingUnverified)
        {
            return new SolverStatusSummary(
                SolverReadiness.ExistingUnverified, "Solver: decide",
                route.BackendName, route.BackendVersion, null, false,
                1, state.TotalSteps, null, null, null, "Existing install does not match the tested build.");
        }

        if (state.Screen is SetupScreenKind.FailedKnown or SetupScreenKind.FailedUnknown)
        {
            return new SolverStatusSummary(
                SolverReadiness.NotReady, "Solver: not ready",
                route.BackendName, route.BackendVersion, null, false,
                Math.Min(state.CompletedSteps + 1, state.TotalSteps), state.TotalSteps, null,
                null, null, state.CauseSummary);
        }

        bool hasSetupActivity = facts.Any(f =>
            f.Kind is SetupFactEnvelope.KindStep or SetupFactEnvelope.KindConsent or SetupFactEnvelope.KindAcceptance);
        if (!hasSetupActivity)
        {
            return new SolverStatusSummary(
                SolverReadiness.NotSetUp, "Solver: not set up",
                route.BackendName, route.BackendVersion, null, false, null, null, null, null, null, null);
        }

        int stepNum = Math.Clamp(state.CompletedSteps + 1, 1, Math.Max(1, state.TotalSteps));
        string label = state.TotalSteps > 0 ? $"Solver: step {stepNum} of {state.TotalSteps}" : "Solver: not set up";
        return new SolverStatusSummary(
            SolverReadiness.SettingUp, label,
            route.BackendName, route.BackendVersion, null, false,
            stepNum, state.TotalSteps, state.ProgressFraction, null, null, null);
    }

    private static string? ComputeRailsStepId(
        RouteDefinition route,
        HostSurveyFact survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId)
    {
        foreach (var step in route.Steps)
        {
            if (!IsStepDone(route, step.Id, survey, facts, sessionId))
                return step.Id;
        }
        return null;
    }

    public static bool IsStepDone(
        RouteDefinition route,
        string stepId,
        HostSurveyFact survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId)
    {
        if (stepId is "survey.mac" or "survey.win")
            return true;

        var existing = FindSupportedExistingInstall(route, survey);
        bool existingUsable = existing is not null &&
            (existing.MatchesPin || HasMatchingAcceptance(route.Id, existing.InstallIdentitySha256, facts));

        if (route.Id == RouteCatalogue.MacOpenFoamId)
        {
            if (stepId == "ofapp.check-existing")
                return existing is not null || HasStepSucceeded(route.Id, stepId, facts, sessionId);
            if (stepId is "ofapp.download" or "ofapp.place")
                return existingUsable || HasStepSucceeded(route.Id, stepId, facts, sessionId);
        }

        if (route.Id == RouteCatalogue.WinWslOpenFoamId)
        {
            if (stepId == "wsl.enable")
            {
                bool wslReady = survey.WslAvailable == true && (survey.WslDefaultVersion ?? 0) >= 2;
                bool askedRestart = LatestOutcomeForStep(route.Id, "wsl.enable", facts, sessionId) == StepOutcome.NeedsRestart;
                return wslReady || askedRestart || HasStepSucceeded(route.Id, stepId, facts, sessionId);
            }
            if (stepId == "os.restart")
            {
                bool wslReady = survey.WslAvailable == true && (survey.WslDefaultVersion ?? 0) >= 2 && survey.RebootPending != true;
                return wslReady || HasStepSucceeded(route.Id, stepId, facts, sessionId);
            }
            if (stepId is "wsl.base.download" or "wsl.base.import")
            {
                bool distroExists = survey.WslDistros.Any(d =>
                    d.Name.Equals(RouteCatalogue.DefaultWslDistroName, StringComparison.OrdinalIgnoreCase) && d.Version >= 2);
                return distroExists || existingUsable || HasStepSucceeded(route.Id, stepId, facts, sessionId);
            }
            if (stepId is "of.repo" or "of.install")
                return existingUsable || HasStepSucceeded(route.Id, stepId, facts, sessionId);
        }

        if (route.Id == RouteCatalogue.WinSu2Id && stepId is "su2.download" or "su2.place")
            return existingUsable || HasStepSucceeded(route.Id, stepId, facts, sessionId);

        if (stepId is "ofapp.identify" or "of.identify")
        {
            var (identify, _) = LatestChecks(route.Id, facts);
            return identify is { Passed: true };
        }

        if (stepId is "smoke.openfoam" or "smoke.su2")
            return IsReady(route, facts);

        return HasStepSucceeded(route.Id, stepId, facts, sessionId);
    }

    private static bool CanOfferUndo(RouteDefinition route, string undoStepId, IReadOnlyList<SetupFactEnvelope> facts)
    {
        return undoStepId switch
        {
            "ofapp.remove" => HasAnySucceededStep(route.Id, "ofapp.place", facts),
            "wsl.unregister" => HasAnySucceededStep(route.Id, "wsl.base.import", facts),
            "su2.remove" => HasAnySucceededStep(route.Id, "su2.place", facts),
            "cache.clear" => HasAnySucceededStep(route.Id, "ofapp.download", facts) ||
                             HasAnySucceededStep(route.Id, "wsl.base.download", facts) ||
                             HasAnySucceededStep(route.Id, "su2.download", facts),
            _ => false
        };
    }

    private static ExistingInstallInfo? FindSupportedExistingInstall(RouteDefinition route, HostSurveyFact? survey)
    {
        if (survey is null) return null;
        return survey.ExistingInstalls.FirstOrDefault(i =>
            i.IsSupportedVersion &&
            string.Equals(i.Backend, route.BackendName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.Version, route.BackendVersion, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMatchingAcceptance(string routeId, string installIdentitySha256, IReadOnlyList<SetupFactEnvelope> facts) =>
        facts.Any(f => f.Kind == SetupFactEnvelope.KindAcceptance &&
                       f.Acceptance is { } acc &&
                       string.Equals(acc.RouteId, routeId, StringComparison.Ordinal) &&
                       string.Equals(acc.InstallIdentitySha256, installIdentitySha256, StringComparison.Ordinal));

    private static (BackendCheckFact? Identify, BackendCheckFact? Smoke) LatestChecks(string routeId, IReadOnlyList<SetupFactEnvelope> facts)
    {
        BackendCheckFact? identify = null, smoke = null;
        foreach (var f in facts)
        {
            if (f.Kind != SetupFactEnvelope.KindCheck || f.Check is not { } c) continue;
            if (!string.Equals(c.RouteId, routeId, StringComparison.Ordinal)) continue;
            if (c.Kind == BackendCheckKind.Identify) identify = c;
            else if (c.Kind == BackendCheckKind.Smoke) smoke = c;
        }
        return (identify, smoke);
    }

    private static HostSurveyFact? LatestSurvey(IReadOnlyList<SetupFactEnvelope> facts)
    {
        for (int i = facts.Count - 1; i >= 0; i--)
            if (facts[i].Kind == SetupFactEnvelope.KindSurvey && facts[i].Survey is not null)
                return facts[i].Survey;
        return null;
    }

    private static EnvironmentStepFact? LatestStepFact(string routeId, IReadOnlyList<SetupFactEnvelope> facts, string? sessionId)
    {
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindStep && facts[i].Step is { } s &&
                string.Equals(s.RouteId, routeId, StringComparison.Ordinal) &&
                (sessionId is null || string.Equals(s.SessionId, sessionId, StringComparison.Ordinal)))
                return s;
        }
        return null;
    }

    private static StepOutcome? LatestOutcomeForStep(string routeId, string stepId, IReadOnlyList<SetupFactEnvelope> facts, string? sessionId)
    {
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindStep && facts[i].Step is { } s &&
                string.Equals(s.RouteId, routeId, StringComparison.Ordinal) &&
                string.Equals(s.StepId, stepId, StringComparison.Ordinal) &&
                (sessionId is null || string.Equals(s.SessionId, sessionId, StringComparison.Ordinal)))
                return s.Outcome;
        }
        return null;
    }

    private static bool HasStepSucceeded(string routeId, string stepId, IReadOnlyList<SetupFactEnvelope> facts, string? sessionId) =>
        facts.Any(f => f.Kind == SetupFactEnvelope.KindStep &&
                       f.Step is { Outcome: StepOutcome.Succeeded } s &&
                       string.Equals(s.RouteId, routeId, StringComparison.Ordinal) &&
                       string.Equals(s.StepId, stepId, StringComparison.Ordinal) &&
                       (sessionId is null || string.Equals(s.SessionId, sessionId, StringComparison.Ordinal)));

    private static bool HasAnySucceededStep(string routeId, string stepId, IReadOnlyList<SetupFactEnvelope> facts) =>
        facts.Any(f => f.Kind == SetupFactEnvelope.KindStep &&
                       f.Step is { Outcome: StepOutcome.Succeeded } s &&
                       string.Equals(s.RouteId, routeId, StringComparison.Ordinal) &&
                       string.Equals(s.StepId, stepId, StringComparison.Ordinal));
}
