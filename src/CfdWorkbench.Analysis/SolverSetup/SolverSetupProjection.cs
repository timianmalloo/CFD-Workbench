using System.Globalization;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.SolverSetup;

public enum StepRailStatus { Done, Current, Waiting, Failed, Skipped, Todo }

public sealed record SolverSetupRailItem(
    string StepId,
    int StepNumber,
    string Title,
    string Meta,
    StepRailStatus Status);

public sealed record SolverSetupStepCardViewModel(
    string StepId,
    int StepNumber,
    int TotalSteps,
    string Title,
    string PlainWhy,
    string PlainWhat,
    string TimeEstimate,
    string DownloadEstimate,
    bool NeedsAdmin,
    bool NeedsRestart,
    string? TermsCopy,
    bool RequiresTermsAcceptance,
    string TechnicalCommand,
    string TechnicalPinSummary,
    string PrimaryButtonLabel,
    bool CanStart);

public sealed record SolverSetupRouteOption(
    string Id,
    string DisplayName,
    string Reason,
    bool IsRecommended);

public sealed record SolverSetupViewModel(
    SetupScreenKind Screen,
    string? RouteId,
    string? RouteDisplayName,
    string? RouteReason,
    IReadOnlyList<SolverSetupRouteOption> OtherRoutes,
    IReadOnlyList<(string Label, string Value)> SurveyLines,
    string? BlockerReason,
    string? ResumedBannerText,
    IReadOnlyList<SolverSetupRailItem> Steps,
    SolverSetupStepCardViewModel? CurrentCard,
    string? CauseCode,
    string? CauseSummary,
    string? NextStepText,
    IReadOnlyList<string> ClickPathSteps,
    string? OutputExcerptRedacted,
    string? ExistingInstallSummary,
    string? ReadySummary,
    SolverStatusSummary StatusSummary,
    bool CanCopyReport,
    bool CanUndoSetupInstalled,
    AssistantOfflineStatus AssistantStatus);

/// <summary>
/// Pure UI-ready projection for Wave 8C <c>W8-SLV-UI</c> (SolverSetupView + StatusStrip Solver item).
/// Derives every visible field from append-only facts (derive-don't-store).
/// </summary>
public static class SolverSetupProjection
{
    public static SolverSetupViewModel Build(
        RouteDefinition? route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        string? sessionId = null,
        bool hasApiKey = false)
    {
        var state = StepSelector.DeriveSessionState(route, survey, facts, sessionId);
        var statusSummary = StepSelector.DeriveSolverStatus(route, survey, facts, sessionId);
        var assistantStatus = SetupAssistantValidator.OfflineStatus(hasApiKey);

        var surveyLines = BuildSurveyLines(survey, route);
        var otherRoutes = BuildOtherRoutes(survey, route);
        var railItems = BuildRailItems(route, survey, facts, state, sessionId);
        var currentCard = BuildCurrentCard(route, state);

        IReadOnlyList<string> clickPath = [];
        if (state.CauseCode == "win.virtualization-off")
            clickPath = RouteCatalogue.GetFirmwareClickPath(survey?.DeviceMaker).Steps;
        else if (state.CauseCode == "mac.blocked")
            clickPath =
            [
                "Open System Settings, then Privacy and Security.",
                "Scroll to Security. Next to OpenFOAM-v2512 was blocked, click Open Anyway.",
                "Confirm with your Mac password or Touch ID.",
                "Return to CFD Workbench and click Check again."
            ];

        string? resumedBanner = state.ResumedFromInterruption && state.ActiveStepId is not null
            ? $"Welcome back. Setup continues from step {Math.Min(state.CompletedSteps + 1, Math.Max(1, state.TotalSteps))}."
            : null;

        string? excerpt = null;
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindStep && facts[i].Step is { } st &&
                (route is null || st.RouteId == route.Id) &&
                !string.IsNullOrWhiteSpace(st.OutputExcerptRedacted))
            {
                excerpt = st.OutputExcerptRedacted;
                break;
            }
        }

        string? existingSummary = null;
        if (survey is not null && route is not null)
        {
            var found = survey.ExistingInstalls.FirstOrDefault(e =>
                e.IsSupportedVersion && string.Equals(e.Backend, route.BackendName, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                existingSummary = found.MatchesPin
                    ? $"{found.Backend} {found.Version} is already on this computer and matches the tested build."
                    : Labels.SolverUnverifiedDisclosure(found.Backend);
            }
        }

        string? readySummary = null;
        if (state.Ready && route is not null)
        {
            string dur = statusSummary.LastSmokeDurationMs is { } ms
                ? string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1, (int)Math.Round(ms / 1000.0))} s")
                : "Not recorded";
            string time = statusSummary.LastVerifiedUtc?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "just now";
            readySummary = Labels.SolverReadySentence(route.BackendName, route.BackendVersion, dur, time);
        }

        bool canUndo = route is not null &&
            state.AllowedNext.Any(id => route.UndoStepIds.Contains(id, StringComparer.Ordinal));

        return new SolverSetupViewModel(
            Screen: state.Screen,
            RouteId: route?.Id,
            RouteDisplayName: route?.DisplayName,
            RouteReason: route?.Reason,
            OtherRoutes: otherRoutes,
            SurveyLines: surveyLines,
            BlockerReason: state.BlockerReason,
            ResumedBannerText: resumedBanner,
            Steps: railItems,
            CurrentCard: currentCard,
            CauseCode: state.CauseCode,
            CauseSummary: state.CauseSummary,
            NextStepText: state.NextStepText,
            ClickPathSteps: clickPath,
            OutputExcerptRedacted: excerpt,
            ExistingInstallSummary: existingSummary,
            ReadySummary: readySummary,
            StatusSummary: statusSummary,
            CanCopyReport: true,
            CanUndoSetupInstalled: canUndo,
            AssistantStatus: assistantStatus);
    }

    private static IReadOnlyList<(string Label, string Value)> BuildSurveyLines(HostSurveyFact? survey, RouteDefinition? route)
    {
        if (survey is null) return [];
        var rows = new List<(string, string)>();
        string disk = survey.FreeDiskBytes is { } fb
            ? string.Create(CultureInfo.InvariantCulture, $"{fb / 1_073_741_824.0:F0} GB free")
            : "free disk Not recorded";
        rows.Add((survey.Os == SetupOs.MacOs ? "This Mac" : "This PC", $"{survey.OsName} · {survey.Arch} · {disk}"));

        var existing = survey.ExistingInstalls.FirstOrDefault();
        rows.Add(("Solver", existing is null
            ? "Not found"
            : $"{existing.Backend} {existing.Version} ({(existing.MatchesPin ? "matches tested build" : "unverified build")})"));

        if (survey.Os == SetupOs.Windows)
        {
            string wslText = survey.WslAvailable == true
                ? $"Turned on (WSL {survey.WslDefaultVersion ?? 2})"
                : survey.WslAvailable == false ? "Not turned on yet" : "Not recorded";
            rows.Add(("WSL (Windows' built-in Linux)", wslText));
        }

        string blocking = "Nothing";
        if (route is not null && survey.FreeDiskBytes is { } bytes && bytes < route.RequiredDiskBytes)
            blocking = "Not enough free disk space";
        else if (survey.VirtualizationFirmwareEnabled == false)
            blocking = "Virtualization is off in firmware";
        rows.Add(("Anything blocking", blocking));
        return rows;
    }

    private static IReadOnlyList<SolverSetupRouteOption> BuildOtherRoutes(HostSurveyFact? survey, RouteDefinition? currentRoute)
    {
        if (survey is null) return [];
        var (rec, _) = StepSelector.RecommendRoute(survey);
        return StepSelector.AvailableRoutes(survey)
            .Where(r => currentRoute is null || !string.Equals(r.Id, currentRoute.Id, StringComparison.Ordinal))
            .Select(r => new SolverSetupRouteOption(r.Id, r.DisplayName, r.Reason, rec?.Id == r.Id))
            .ToArray();
    }

    private static IReadOnlyList<SolverSetupRailItem> BuildRailItems(
        RouteDefinition? route,
        HostSurveyFact? survey,
        IReadOnlyList<SetupFactEnvelope> facts,
        SetupSessionState state,
        string? sessionId)
    {
        if (route is null) return [];
        var items = new List<SolverSetupRailItem>(route.Steps.Count);
        for (int i = 0; i < route.Steps.Count; i++)
        {
            var step = route.Steps[i];
            StepRailStatus status;
            if (state.Ready || (survey is not null && StepSelector.IsStepDone(route, step.Id, survey, facts, sessionId)))
                status = StepRailStatus.Done;
            else if (state.ActiveStepId == step.Id)
                status = state.Screen switch
                {
                    SetupScreenKind.FailedKnown or SetupScreenKind.FailedUnknown => StepRailStatus.Failed,
                    SetupScreenKind.WaitingForOs or SetupScreenKind.NeedsRestart or SetupScreenKind.ExistingUnverified => StepRailStatus.Waiting,
                    _ => StepRailStatus.Current
                };
            else
                status = StepRailStatus.Todo;

            string meta = status == StepRailStatus.Skipped ? "not needed" : step.TimeEstimate;
            items.Add(new SolverSetupRailItem(step.Id, i + 1, step.Title, meta, status));
        }
        return items;
    }

    private static SolverSetupStepCardViewModel? BuildCurrentCard(RouteDefinition? route, SetupSessionState state)
    {
        if (route is null || state.ActiveStepId is null) return null;
        int index = -1;
        for (int i = 0; i < route.Steps.Count; i++)
        {
            if (route.Steps[i].Id == state.ActiveStepId) { index = i; break; }
        }
        if (index < 0) return null;

        var step = route.Steps[index];
        var pin = route.Pins.FirstOrDefault(p => p.LicenseId == step.TermsId) ?? route.Pins.FirstOrDefault();
        string pinSummary = pin is null
            ? "No external download pin"
            : $"{pin.FileName} · sha256 {pin.Sha256[..Math.Min(16, pin.Sha256.Length)]}…";

        bool needsTerms = !string.IsNullOrEmpty(step.TermsId);
        string? termsText = step.TermsId switch
        {
            "GPL-3.0" => RouteCatalogue.Gpl3SummaryText,
            "LGPL-2.1" => RouteCatalogue.Lgpl21SummaryText,
            _ => null
        };

        string buttonLabel = state.Screen switch
        {
            SetupScreenKind.NeedsRestart => "Restart now",
            SetupScreenKind.WaitingForOs => "Waiting for OS…",
            SetupScreenKind.FailedKnown or SetupScreenKind.FailedUnknown => "Try this step again",
            _ when needsTerms => "I accept — start this step",
            _ => "Start this step"
        };

        bool canStart = !state.HasOpenAttempt && state.AllowedNext.Contains(step.Id, StringComparer.Ordinal);
        return new SolverSetupStepCardViewModel(
            StepId: step.Id,
            StepNumber: index + 1,
            TotalSteps: route.Steps.Count,
            Title: step.Title,
            PlainWhy: step.PlainWhy,
            PlainWhat: step.PlainWhat,
            TimeEstimate: step.TimeEstimate,
            DownloadEstimate: step.DownloadEstimate,
            NeedsAdmin: step.NeedsAdmin,
            NeedsRestart: step.NeedsRestart,
            TermsCopy: termsText,
            RequiresTermsAcceptance: needsTerms,
            TechnicalCommand: step.TechnicalCommand,
            TechnicalPinSummary: pinSummary,
            PrimaryButtonLabel: buttonLabel,
            CanStart: canStart);
    }
}
