namespace CfdWorkbench.Desktop.SolverSetup;

/// <summary>
/// Plain-words Guided Solver Setup UI copy constants (Ruling 69, DESIGN.md COPY-766..805 proposed).
/// Every constant here is user-facing copy outside the Technical details command slot and MUST pass
/// <see cref="CfdWorkbench.Core.SolverSetup.SetupCopyLint.Lint"/> (SETUP-04 zero commands to type).
/// </summary>
public static class SolverSetupCopy
{
    public const string VerbTitle = "Set up a solver";                                                      // COPY-766
    public const string TabTitle = "Solver setup";                                                          // COPY-767
    public const string StripNotSetUp = "Solver: not set up";                                               // COPY-768
    public const string StripReady = "Solver: Ready";                                                       // COPY-769
    public const string StripNotReady = "Solver: not ready";                                                // COPY-770
    public const string StripRestartNeeded = "Solver: restart needed";                                      // COPY-771
    public const string StripDecide = "Solver: decide";                                                     // COPY-772
    public const string StripBlocked = "Solver: blocked";                                                   // COPY-773

    public const string RouteHeading = "No solver is set up on this computer yet";                          // COPY-774
    public const string RouteLead = "Your designs and the quick analyses work without one. A solver is needed for full CFD runs."; // COPY-775
    public const string RecommendedBadge = "Recommended";                                                   // COPY-776
    public const string OtherOptionsHeading = "Other options";                                              // COPY-777
    public const string SwitchRouteButton = "Use this route instead";                                       // COPY-778
    public const string StartSetupButton = "Start setup";                                                   // COPY-779
    public const string NotNowButton = "Not now";                                                           // COPY-780
    public const string StartStepButton = "Start this step";                                                // COPY-781
    public const string AcceptAndStartButton = "I accept — start this step";                                // COPY-782

    public const string TechnicalDetailsHeading = "Technical details";                                      // COPY-783
    public const string CopyDetailsButton = "Copy details";                                                 // COPY-784
    public const string CopyReportButton = "Copy a report";                                                 // COPY-785
    public const string ReportCopiedStatus = "Report copied to clipboard without user names, paths, or keys."; // COPY-786
    public const string DetailsCopiedStatus = "Technical details copied to clipboard.";

    public const string NeedsAdminBadge = "Administrator approval required";                                // COPY-787
    public const string NoAdminBadge = "No administrator approval";                                         // COPY-788
    public const string NeedsRestartBadge = "Restart required afterwards";                                  // COPY-789
    public const string NoRestartBadge = "No restart";                                                      // COPY-790

    public const string ExistingUnverifiedHeading = "Existing solver found, but it is not the tested build"; // COPY-791
    public const string InstallTestedButton = "Install the tested build";                                   // COPY-792
    public const string UseMineAnywayButton = "Use mine anyway";                                            // COPY-793
    public const string InstallTestedNote = "Your own copy stays where it is. The tested copy is checked file by file."; // COPY-794

    public const string NeedsRestartHeading = "Restart needed";                                             // COPY-795
    public const string NeedsRestartBody = "Save your work first. After the restart, CFD Workbench opens again and continues from the next step. Nothing is lost if you restart later."; // COPY-796
    public const string RestartNowButton = "Restart now";                                                   // COPY-797
    public const string RestartLaterButton = "I'll restart later";                                          // COPY-797

    public const string CheckAgainButton = "Check again";                                                   // COPY-798
    public const string TryStepAgainButton = "Try this step again";                                         // COPY-798
    public const string CauseHeading = "What happened";                                                     // COPY-799
    public const string NextStepHeading = "Next step";                                                      // COPY-799
    public const string OutputExcerptHeading = "Last lines of output";                                      // COPY-799

    public const string ReadyHeading = "Ready";                                                             // COPY-800
    public const string BackToAnalysisButton = "Back to my analysis";                                       // COPY-800
    public const string DoneButton = "Done";                                                                // COPY-800
    public const string RemoveInstalledButton = "Remove what setup installed…";                             // COPY-800

    public const string AssistantAskHeading = "Ask about this step";                                        // COPY-801
    public const string AssistantExplainHeading = "Explain this failure";                                   // COPY-801
    public const string AssistantBadge = "Assistant";
    public const string AssistantDisclosure = "Explains steps and errors in plain words. It can be wrong. It cannot run anything or change your computer."; // COPY-802
    public const string AssistantNoKeyNotice = "No API key configured — every setup step, known error, and Copy a report work without it."; // COPY-803
    public const string AssistantOfflineNote = "Setup works the same without the assistant: every step, every known error, and Copy a report."; // COPY-804
    public const string AssistantOffPlaceholder = "Assistant off — setup works without it";
    public const string AssistantAskPlaceholder = "Ask a question about this step…";
    public const string AssistantAskButton = "Ask";
    public const string AssistantUseSuggestion = "Use this suggestion";

    public const string RefreshSurveyButton = "Run read-only check";                                        // COPY-805
    public const string HeaderNotSetUp = "Status: Not set up";                                              // COPY-805
    public const string HeaderSettingUp = "Status: Setting up";                                             // COPY-805
    public const string HeaderReady = "Status: Ready";                                                      // COPY-805
    public const string HeaderNeedsRestart = "Status: Restart needed";
    public const string HeaderNotReady = "Status: Not ready";
    public const string HeaderBlocked = "Status: Blocked";
    public const string HeaderDecide = "Status: Existing install";

    public const string SurveySectionHeading = "What CFD Workbench found on this computer";
    public const string SurveyNotRunNote = "No check has been recorded yet. Click Run read-only check to inspect this computer without changing anything.";
    public const string RestartScheduledStatus = "One-time resume entry registered. Save your work and restart your computer when ready.";
    public const string RestartLaterStatus = "Nothing is lost. Restart when ready and reopen CFD Workbench to continue.";
    public const string StepConsentRecordedStatus = "Consent recorded with technical details shown.";
    public const string UnverifiedAcceptedStatus = "Unverified install accepted. Runs will be marked unverified install.";
    public const string ReadyCaseCodeLabel = "Case code";
    public const string ReadyCaseCodeValue = "Switched off and checked (Disallowing)";
    public const string TermsHeading = "Licence (shown as written by its authors):";
    public const string NoRouteHeading = "No supported solver route on this computer";

    /// <summary>Every user-facing string constant for automated SETUP-04 no-command copy lint.</summary>
    public static IReadOnlyList<(string Name, string Text)> AllUserFacingCopy() =>
    [
        (nameof(VerbTitle), VerbTitle),
        (nameof(TabTitle), TabTitle),
        (nameof(StripNotSetUp), StripNotSetUp),
        (nameof(StripReady), StripReady),
        (nameof(StripNotReady), StripNotReady),
        (nameof(StripRestartNeeded), StripRestartNeeded),
        (nameof(StripDecide), StripDecide),
        (nameof(StripBlocked), StripBlocked),
        (nameof(RouteHeading), RouteHeading),
        (nameof(RouteLead), RouteLead),
        (nameof(RecommendedBadge), RecommendedBadge),
        (nameof(OtherOptionsHeading), OtherOptionsHeading),
        (nameof(SwitchRouteButton), SwitchRouteButton),
        (nameof(StartSetupButton), StartSetupButton),
        (nameof(NotNowButton), NotNowButton),
        (nameof(StartStepButton), StartStepButton),
        (nameof(AcceptAndStartButton), AcceptAndStartButton),
        (nameof(TechnicalDetailsHeading), TechnicalDetailsHeading),
        (nameof(CopyDetailsButton), CopyDetailsButton),
        (nameof(CopyReportButton), CopyReportButton),
        (nameof(ReportCopiedStatus), ReportCopiedStatus),
        (nameof(DetailsCopiedStatus), DetailsCopiedStatus),
        (nameof(NeedsAdminBadge), NeedsAdminBadge),
        (nameof(NoAdminBadge), NoAdminBadge),
        (nameof(NeedsRestartBadge), NeedsRestartBadge),
        (nameof(NoRestartBadge), NoRestartBadge),
        (nameof(ExistingUnverifiedHeading), ExistingUnverifiedHeading),
        (nameof(InstallTestedButton), InstallTestedButton),
        (nameof(UseMineAnywayButton), UseMineAnywayButton),
        (nameof(InstallTestedNote), InstallTestedNote),
        (nameof(NeedsRestartHeading), NeedsRestartHeading),
        (nameof(NeedsRestartBody), NeedsRestartBody),
        (nameof(RestartNowButton), RestartNowButton),
        (nameof(RestartLaterButton), RestartLaterButton),
        (nameof(CheckAgainButton), CheckAgainButton),
        (nameof(TryStepAgainButton), TryStepAgainButton),
        (nameof(CauseHeading), CauseHeading),
        (nameof(NextStepHeading), NextStepHeading),
        (nameof(OutputExcerptHeading), OutputExcerptHeading),
        (nameof(ReadyHeading), ReadyHeading),
        (nameof(BackToAnalysisButton), BackToAnalysisButton),
        (nameof(DoneButton), DoneButton),
        (nameof(RemoveInstalledButton), RemoveInstalledButton),
        (nameof(AssistantAskHeading), AssistantAskHeading),
        (nameof(AssistantExplainHeading), AssistantExplainHeading),
        (nameof(AssistantBadge), AssistantBadge),
        (nameof(AssistantDisclosure), AssistantDisclosure),
        (nameof(AssistantNoKeyNotice), AssistantNoKeyNotice),
        (nameof(AssistantOfflineNote), AssistantOfflineNote),
        (nameof(AssistantOffPlaceholder), AssistantOffPlaceholder),
        (nameof(AssistantAskPlaceholder), AssistantAskPlaceholder),
        (nameof(AssistantAskButton), AssistantAskButton),
        (nameof(AssistantUseSuggestion), AssistantUseSuggestion),
        (nameof(RefreshSurveyButton), RefreshSurveyButton),
        (nameof(HeaderNotSetUp), HeaderNotSetUp),
        (nameof(HeaderSettingUp), HeaderSettingUp),
        (nameof(HeaderReady), HeaderReady),
        (nameof(HeaderNeedsRestart), HeaderNeedsRestart),
        (nameof(HeaderNotReady), HeaderNotReady),
        (nameof(HeaderBlocked), HeaderBlocked),
        (nameof(HeaderDecide), HeaderDecide),
        (nameof(SurveySectionHeading), SurveySectionHeading),
        (nameof(SurveyNotRunNote), SurveyNotRunNote),
        (nameof(RestartScheduledStatus), RestartScheduledStatus),
        (nameof(RestartLaterStatus), RestartLaterStatus),
        (nameof(StepConsentRecordedStatus), StepConsentRecordedStatus),
        (nameof(UnverifiedAcceptedStatus), UnverifiedAcceptedStatus),
        (nameof(ReadyCaseCodeLabel), ReadyCaseCodeLabel),
        (nameof(ReadyCaseCodeValue), ReadyCaseCodeValue),
        (nameof(TermsHeading), TermsHeading),
        (nameof(NoRouteHeading), NoRouteHeading)
    ];
}
