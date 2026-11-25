using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Desktop.SolverSetup;

/// <summary>
/// Non-blocking Guided Solver Setup Dock document view (Ruling 69, docs/design/guided-solver-setup.md,
/// docs/mockups/solver-setup.html). Binds to <see cref="SolverSetupService"/> and <see cref="SolverSetupProjection"/>;
/// never mutates WSL distros or HKCU registry on view open.
/// </summary>
public partial class SolverSetupView : UserControl
{
    private const string AppVersion = "0.1.0";
    public const double AssistantCollapseWidthBreakpoint = 1100.0;

    private SolverSetupService? service;
    private Assistant.AssistantRuntimeHub? assistantHub;
    private string? lastCardStepId;
    private string? suggestedStepId;
    private bool? userAssistantOverride;

    public SolverSetupView()
    {
        InitializeComponent();

        HeaderTitleText.Text = SolverSetupCopy.VerbTitle;
        RefreshSurveyButton.Content = SolverSetupCopy.RefreshSurveyButton;
        CopyReportButton.Content = SolverSetupCopy.CopyReportButton;
        CloseSetupButton.Content = SolverSetupCopy.DoneButton;
        SurveyHeadingText.Text = SolverSetupCopy.SurveySectionHeading;
        SurveyEmptyNote.Text = SolverSetupCopy.SurveyNotRunNote;
        RouteBadgeText.Text = SolverSetupCopy.RecommendedBadge;
        OtherOptionsExpander.Header = SolverSetupCopy.OtherOptionsHeading;
        StartSetupButton.Content = SolverSetupCopy.StartSetupButton;
        RouteNotNowButton.Content = SolverSetupCopy.NotNowButton;
        StepNotNowButton.Content = SolverSetupCopy.NotNowButton;
        TermsHeadingText.Text = SolverSetupCopy.TermsHeading;
        InstallTestedNoteText.Text = SolverSetupCopy.InstallTestedNote;
        InstallTestedButton.Content = SolverSetupCopy.InstallTestedButton;
        UseMineAnywayButton.Content = SolverSetupCopy.UseMineAnywayButton;
        NeedsRestartBodyText.Text = SolverSetupCopy.NeedsRestartBody;
        RestartNowButton.Content = SolverSetupCopy.RestartNowButton;
        RestartLaterButton.Content = SolverSetupCopy.RestartLaterButton;
        FailureCopyReportButton.Content = SolverSetupCopy.CopyReportButton;
        BackToAnalysisButton.Content = SolverSetupCopy.BackToAnalysisButton;
        ReadyDoneButton.Content = SolverSetupCopy.DoneButton;
        RemoveInstalledButton.Content = SolverSetupCopy.RemoveInstalledButton;
        TechnicalDetailsExpander.Header = SolverSetupCopy.TechnicalDetailsHeading;
        CopyDetailsButton.Content = SolverSetupCopy.CopyDetailsButton;
        AssistantDisclosureText.Text = SolverSetupCopy.AssistantDisclosure;
        AssistantOfflineNoteText.Text = SolverSetupCopy.AssistantOfflineNote;
        AssistantAskButton.Content = SolverSetupCopy.AssistantAskButton;
        AssistantUseSuggestionButton.Content = SolverSetupCopy.AssistantUseSuggestion;

        RefreshSurveyButton.Click += (_, _) => _ = RunSurveyAsync();
        CopyReportButton.Click += (_, _) => CopyReport();
        FailureCopyReportButton.Click += (_, _) => CopyReport();
        CloseSetupButton.Click += (_, _) => CloseRequested?.Invoke();
        RouteNotNowButton.Click += (_, _) => CloseRequested?.Invoke();
        StepNotNowButton.Click += (_, _) => CloseRequested?.Invoke();
        ReadyDoneButton.Click += (_, _) => CloseRequested?.Invoke();
        BackToAnalysisButton.Click += (_, _) => BackToAnalysisRequested?.Invoke();
        StartSetupButton.Click += (_, _) => _ = RunSurveyAsync();
        PrimaryActionButton.Click += (_, _) => OnPrimaryStepAction();
        InstallTestedButton.Click += (_, _) => OnInstallTestedBuild();
        UseMineAnywayButton.Click += (_, _) => OnUseMineAnyway();
        RestartNowButton.Click += (_, _) => OnRestartNow();
        RestartLaterButton.Click += (_, _) => ShowReceipt(SolverSetupCopy.RestartLaterStatus);
        RetryStepButton.Click += (_, _) => _ = RunSurveyAsync();
        CopyDetailsButton.Click += (_, _) => CopyTechnicalDetails();
        RemoveInstalledButton.Click += (_, _) => OnRemoveInstalled();
        AssistantAskButton.Click += async (_, _) => await OnAskAssistantAsync();
        AssistantUseSuggestionButton.Click += (_, _) => OnUseAssistantSuggestion();
        AssistantConfigureKeyButton.Content = Assistant.AssistantCopy.ConfigureKeyButton;
        AssistantConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        ToggleAssistantButton.Click += (_, _) =>
        {
            userAssistantOverride = !AssistantPane.IsVisible;
            ApplyAssistantPaneVisibility();
        };
        SizeChanged += (_, e) => ApplyAssistantPaneVisibility(e.NewSize.Width);

        SessionId = "sess-" + Guid.NewGuid().ToString("N")[..8];
        ViewModel = SolverSetupProjection.Build(null, null, [], SessionId, hasApiKey: false);
        Render(ViewModel);
        ApplyAssistantPaneVisibility();
    }

    private void ApplyAssistantPaneVisibility(double? measuredWidth = null)
    {
        double width = measuredWidth ?? Bounds.Width;
        bool show = userAssistantOverride ?? (width <= 0 || width >= AssistantCollapseWidthBreakpoint);
        AssistantPane.IsVisible = show;
        ToggleAssistantButton.IsChecked = show;
    }

    /// <summary>Raised when an action inside Solver setup wants to report a status line to the shell strip.</summary>
    public event Action<StatusReport>? Reported;

    /// <summary>Raised whenever the derived <see cref="SolverStatusSummary"/> changes so the status strip follows.</summary>
    public event Action<SolverStatusSummary>? StatusChanged;

    /// <summary>Raised when the user clicks Done or Not now.</summary>
    public event Action? CloseRequested;

    /// <summary>Raised when the user clicks Back to my analysis in the Ready state.</summary>
    public event Action? BackToAnalysisRequested;

    /// <summary>Raised when the user clicks Configure key inside the AI-11 assistant card.</summary>
    public event Action? ConfigureKeyRequested;

    public void BindAssistant(Assistant.AssistantRuntimeHub hub)
    {
        assistantHub = hub ?? throw new ArgumentNullException(nameof(hub));
        RefreshAssistantStatus();
    }

    public void RefreshAssistantStatus()
    {
        if (assistantHub is not null)
            HasApiKey = assistantHub.HasApiKey;
        RefreshProjection();
    }

    public void SetApiKeyAvailable(bool available)
    {
        if (HasApiKey == available && assistantHub is null) return;
        HasApiKey = available;
        RefreshProjection();
    }

    public SolverSetupViewModel ViewModel { get; private set; }
    public string SessionId { get; private set; }
    public string? SelectedRouteId { get; private set; }
    public string? LastCopiedReport { get; private set; }
    public bool HasApiKey { get; private set; }

    public void BindService(
        SolverSetupService setupService,
        string? selectedRouteId = null,
        string? sessionId = null,
        bool hasApiKey = false)
    {
        ArgumentNullException.ThrowIfNull(setupService);
        service = setupService;
        if (!string.IsNullOrWhiteSpace(sessionId))
            SessionId = sessionId;
        SelectedRouteId = selectedRouteId;
        HasApiKey = hasApiKey;
        RefreshProjection();
    }

    public void RefreshProjection()
    {
        if (service is null)
        {
            ViewModel = SolverSetupProjection.Build(null, null, [], SessionId, HasApiKey);
            Render(ViewModel);
            return;
        }

        ViewModel = service.Project(SelectedRouteId, SessionId, HasApiKey);
        if (SelectedRouteId is null && ViewModel.RouteId is not null)
            SelectedRouteId = ViewModel.RouteId;
        Render(ViewModel);
        StatusChanged?.Invoke(ViewModel.StatusSummary);
    }

    public async Task RunSurveyAsync(CancellationToken cancellationToken = default)
    {
        if (service is null) return;
        RefreshSurveyButton.IsEnabled = false;
        try
        {
            var result = await service.RunSurveyAsync(cancellationToken);
            if (SelectedRouteId is null && result.RecommendedRoute is not null)
                SelectedRouteId = result.RecommendedRoute.Id;
            RefreshProjection();
        }
        finally
        {
            RefreshSurveyButton.IsEnabled = true;
        }
    }

    public void SelectRoute(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId)) return;
        SelectedRouteId = routeId;
        OtherOptionsExpander.IsExpanded = false;
        RefreshProjection();
    }

    /// <summary>
    /// Validates a model-authored JSON response through <see cref="SetupAssistantValidator"/> (AI-11)
    /// and renders either the verified explanation/suggestion or the withheld/refused fallback.
    /// </summary>
    public AssistantValidationResult ApplyAssistantResponse(
        string rawJson,
        IReadOnlySet<string>? knownKnowledgeIds = null)
    {
        var allowed = BuildAllowedNextSet();
        var excerptLines = ParseExcerptLines(ViewModel.OutputExcerptRedacted);
        var shared = string.Join("\n", new[]
        {
            ViewModel.RouteDisplayName ?? "",
            ViewModel.CurrentCard?.Title ?? "",
            ViewModel.CurrentCard?.PlainWhy ?? "",
            ViewModel.CauseSummary ?? "",
            ViewModel.NextStepText ?? "",
            ViewModel.OutputExcerptRedacted ?? ""
        });
        string fallback = !string.IsNullOrWhiteSpace(ViewModel.CauseSummary)
            ? $"{ViewModel.CauseSummary} {ViewModel.NextStepText}"
            : ViewModel.CurrentCard?.PlainWhy ?? SolverSetupCopy.RouteLead;

        var ctx = new AssistantValidationContext(
            AllowedNextStepIds: allowed,
            ExcerptLines: excerptLines,
            KnownKnowledgeIds: knownKnowledgeIds ?? new HashSet<string>(StringComparer.Ordinal),
            SharedContextText: shared,
            CauseIsUnknown: ViewModel.Screen == SetupScreenKind.FailedUnknown,
            FallbackFixedExplanation: fallback);

        var result = service is not null
            ? service.ValidateAssistantResponse(rawJson, ctx)
            : SetupAssistantValidator.Validate(rawJson, ctx);

        RenderAssistantResult(result);
        return result;
    }

    private void Render(SolverSetupViewModel vm)
    {
        HeaderStatusText.Text = HeaderLabelFor(vm.StatusSummary.Readiness);
        HeaderRouteText.Text = vm.RouteDisplayName ?? SolverSetupCopy.RouteHeading;

        // Resumed banner
        ResumedBanner.IsVisible = !string.IsNullOrWhiteSpace(vm.ResumedBannerText);
        ResumedBannerText.Text = vm.ResumedBannerText ?? "";

        // Left rail
        RailRouteTitle.Text = vm.RouteDisplayName ?? SolverSetupCopy.VerbTitle;
        RailSurveySubtitle.Text = vm.SurveyLines.Count > 0 ? vm.SurveyLines[0].Value : SolverSetupCopy.SurveyNotRunNote;
        RenderStepRail(vm);

        // Survey card
        RenderSurveyLines(vm);

        // Route card + Other options
        RouteCard.IsVisible = vm.RouteId is not null || vm.OtherRoutes.Count > 0;
        RouteNameText.Text = vm.RouteDisplayName ?? "";
        RouteReasonText.Text = vm.RouteReason ?? "";
        OtherOptionsExpander.IsVisible = vm.OtherRoutes.Count > 0;
        RenderOtherRoutes(vm);

        // Hard-state card visibility
        RouteSelectionActionsPanel.IsVisible = vm.Screen == SetupScreenKind.RouteSelection;
        StepReadyPanel.IsVisible = vm.Screen is SetupScreenKind.StepReady or SetupScreenKind.WaitingForOs or SetupScreenKind.Running or SetupScreenKind.ExistingMatching;
        ExistingUnverifiedPanel.IsVisible = vm.Screen == SetupScreenKind.ExistingUnverified;
        FailurePanel.IsVisible = vm.Screen is SetupScreenKind.FailedKnown or SetupScreenKind.FailedUnknown or SetupScreenKind.NoRoute;
        NeedsRestartPanel.IsVisible = vm.Screen == SetupScreenKind.NeedsRestart;
        ReadyPanel.IsVisible = vm.Screen == SetupScreenKind.Ready;

        // Card header & body by screen
        RenderMainCardContent(vm);

        // Assistant panel (R2-08 / C6: enforce IsReadyForProposals when assistantHub is bound; fall back cleanly to HasApiKey when null)
        AssistantHeadingText.Text = vm.Screen is SetupScreenKind.FailedKnown or SetupScreenKind.FailedUnknown
            ? SolverSetupCopy.AssistantExplainHeading
            : SolverSetupCopy.AssistantAskHeading;
        bool effectiveAvailable = assistantHub is not null
            ? assistantHub.IsReadyForProposals
            : vm.AssistantStatus.Available;
        bool hasKeyButUnevaluated = assistantHub is not null && assistantHub.HasApiKey && !assistantHub.Eligibility.ProposalsEnabled;

        if (assistantHub is not null)
        {
            AssistantUsageChipText.Text = Assistant.AssistantCopy.DailyUsageChip(
                assistantHub.DailyRequestsUsedUtc,
                assistantHub.CurrentSettings.DailyRequestCap);
            AssistantUsageChip.IsVisible = true;
        }
        else
        {
            AssistantUsageChip.IsVisible = false;
        }

        AssistantOfflineCard.IsVisible = !effectiveAvailable;
        if (hasKeyButUnevaluated)
        {
            AssistantOfflineNoticeText.Text = Assistant.AssistantCopy.UnevaluatedOnThisModel;
            AssistantOfflineNoteText.Text = Assistant.AssistantCopy.UnevaluatedDetail(
                CfdWorkbench.Core.Assistant.AssistantEvalSuite.EvalThresholdsVersion);
            AssistantConfigureKeyButton.Content = Assistant.AssistantCopy.OpenSettingsButton;
        }
        else
        {
            AssistantOfflineNoticeText.Text = vm.AssistantStatus.Notice;
            AssistantOfflineNoteText.Text = SolverSetupCopy.AssistantOfflineNote;
            AssistantConfigureKeyButton.Content = Assistant.AssistantCopy.ConfigureKeyButton;
        }
        AssistantPromptBox.IsEnabled = effectiveAvailable;
        AssistantAskButton.IsEnabled = effectiveAvailable;
        AssistantPromptBox.Watermark = effectiveAvailable
            ? SolverSetupCopy.AssistantAskPlaceholder
            : SolverSetupCopy.AssistantOffPlaceholder;
    }

    private void RenderStepRail(SolverSetupViewModel vm)
    {
        StepRailList.Children.Clear();
        foreach (var item in vm.Steps)
        {
            string glyph = item.Status switch
            {
                StepRailStatus.Done => "✓",
                StepRailStatus.Failed => "!",
                StepRailStatus.Waiting => "…",
                StepRailStatus.Skipped => "–",
                _ => item.StepNumber.ToString(CultureInfo.InvariantCulture)
            };

            var badge = new Border
            {
                Classes = { "solver-step-index" },
                Child = new TextBlock
                {
                    Text = glyph,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            if (item.Status == StepRailStatus.Done) badge.Classes.Add("done");
            else if (item.Status == StepRailStatus.Failed) badge.Classes.Add("failed");
            else if (item.Status == StepRailStatus.Waiting) badge.Classes.Add("waiting");

            var textStack = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = item.Title, TextWrapping = TextWrapping.Wrap, Classes = { "solver-rail-title" } },
                    new TextBlock { Text = item.Meta, TextWrapping = TextWrapping.Wrap, Classes = { "caption" } }
                }
            };

            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            Grid.SetColumn(badge, 0);
            Grid.SetColumn(textStack, 1);
            rowGrid.Children.Add(badge);
            rowGrid.Children.Add(textStack);

            var rowBorder = new Border { Classes = { "solver-rail-item" }, Child = rowGrid };
            if (item.Status is StepRailStatus.Current or StepRailStatus.Failed or StepRailStatus.Waiting)
                rowBorder.Classes.Add("active");
            StepRailList.Children.Add(rowBorder);
        }
    }

    private void RenderSurveyLines(SolverSetupViewModel vm)
    {
        SurveyRowsPanel.Children.Clear();
        SurveyEmptyNote.IsVisible = vm.SurveyLines.Count == 0;
        foreach (var (label, value) in vm.SurveyLines)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("180,*") };
            var lbl = new TextBlock { Text = label, Classes = { "caption" }, TextWrapping = TextWrapping.Wrap };
            var val = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            SurveyRowsPanel.Children.Add(grid);
        }
    }

    private void RenderOtherRoutes(SolverSetupViewModel vm)
    {
        OtherRoutesList.Children.Clear();
        foreach (var opt in vm.OtherRoutes)
        {
            var switchBtn = new Button
            {
                Content = SolverSetupCopy.SwitchRouteButton,
                Tag = opt.Id,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            AutomationProperties.SetName(switchBtn, $"{SolverSetupCopy.SwitchRouteButton}: {opt.DisplayName}");
            string routeId = opt.Id;
            switchBtn.Click += (_, _) => SelectRoute(routeId);

            var card = new Border
            {
                Classes = { "solver-other-route" },
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = opt.DisplayName, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
                        new TextBlock { Text = opt.Reason, Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
                        switchBtn
                    }
                }
            };
            OtherRoutesList.Children.Add(card);
        }
    }

    private void RenderMainCardContent(SolverSetupViewModel vm)
    {
        var card = vm.CurrentCard;
        NeedsBadgesPanel.Children.Clear();

        switch (vm.Screen)
        {
            case SetupScreenKind.RouteSelection:
                CardKicker.Text = SolverSetupCopy.VerbTitle;
                CardTitle.Text = SolverSetupCopy.RouteHeading;
                CardLead.Text = SolverSetupCopy.RouteLead;
                CardWhat.IsVisible = false;
                NeedsBadgesPanel.IsVisible = false;
                TechnicalDetailsExpander.IsVisible = false;
                break;

            case SetupScreenKind.ExistingUnverified:
                CardKicker.Text = card is not null
                    ? string.Create(CultureInfo.InvariantCulture, $"Step {card.StepNumber} of {card.TotalSteps}")
                    : SolverSetupCopy.VerbTitle;
                CardTitle.Text = SolverSetupCopy.ExistingUnverifiedHeading;
                CardLead.Text = vm.RouteDisplayName ?? SolverSetupCopy.RouteHeading;
                CardWhat.IsVisible = false;
                NeedsBadgesPanel.IsVisible = false;
                ExistingDisclosureText.Text = vm.ExistingInstallSummary ?? SolverSetupCopy.ExistingUnverifiedHeading;
                BindTechnicalDetails(card);
                break;

            case SetupScreenKind.NeedsRestart:
                CardKicker.Text = card is not null
                    ? string.Create(CultureInfo.InvariantCulture, $"Step {card.StepNumber} of {card.TotalSteps}")
                    : SolverSetupCopy.VerbTitle;
                CardTitle.Text = SolverSetupCopy.NeedsRestartHeading;
                CardLead.Text = card?.PlainWhy ?? SolverSetupCopy.NeedsRestartHeading;
                CardWhat.IsVisible = false;
                NeedsBadgesPanel.IsVisible = false;
                BindTechnicalDetails(card);
                break;

            case SetupScreenKind.FailedKnown:
            case SetupScreenKind.FailedUnknown:
            case SetupScreenKind.NoRoute:
                CardKicker.Text = card is not null
                    ? string.Create(CultureInfo.InvariantCulture, $"Step {card.StepNumber} of {card.TotalSteps}")
                    : SolverSetupCopy.VerbTitle;
                CardTitle.Text = vm.Screen == SetupScreenKind.NoRoute
                    ? SolverSetupCopy.NoRouteHeading
                    : card?.Title ?? SolverSetupCopy.StripNotReady;
                CardLead.Text = card?.PlainWhy ?? "";
                CardWhat.IsVisible = false;
                NeedsBadgesPanel.IsVisible = false;
                CauseSummaryText.Text = vm.CauseSummary ?? "";
                NextStepText.Text = vm.NextStepText ?? "";
                RenderClickPath(vm.ClickPathSteps);
                OutputExcerptExpander.IsVisible = !string.IsNullOrWhiteSpace(vm.OutputExcerptRedacted);
                OutputExcerptText.Text = vm.OutputExcerptRedacted ?? "";
                RetryStepButton.Content = vm.ClickPathSteps.Count > 0
                    ? SolverSetupCopy.CheckAgainButton
                    : SolverSetupCopy.TryStepAgainButton;
                BindTechnicalDetails(card);
                break;

            case SetupScreenKind.Ready:
                CardKicker.Text = SolverSetupCopy.DoneButton;
                CardTitle.Text = SolverSetupCopy.ReadyHeading;
                CardLead.Text = "";
                CardWhat.IsVisible = false;
                NeedsBadgesPanel.IsVisible = false;
                ReadySummaryText.Text = vm.ReadySummary ?? SolverSetupCopy.StripReady;
                RenderReadyFacts(vm);
                RemoveInstalledButton.IsVisible = vm.CanUndoSetupInstalled;
                TechnicalDetailsExpander.IsVisible = false;
                break;

            default:
                if (card is not null)
                {
                    CardKicker.Text = string.Create(CultureInfo.InvariantCulture, $"Step {card.StepNumber} of {card.TotalSteps}");
                    CardTitle.Text = card.Title;
                    CardLead.Text = card.PlainWhy;
                    CardWhat.Text = card.PlainWhat;
                    CardWhat.IsVisible = !string.IsNullOrWhiteSpace(card.PlainWhat);
                    RenderBadges(card);
                    TermsPanel.IsVisible = card.RequiresTermsAcceptance && !string.IsNullOrWhiteSpace(card.TermsCopy);
                    TermsText.Text = card.TermsCopy ?? "";
                    PrimaryActionButton.Content = card.PrimaryButtonLabel;
                    AutomationProperties.SetName(PrimaryActionButton, card.PrimaryButtonLabel);
                    PrimaryActionButton.IsEnabled = card.CanStart;
                    BindTechnicalDetails(card);
                }
                else
                {
                    CardKicker.Text = SolverSetupCopy.VerbTitle;
                    CardTitle.Text = SolverSetupCopy.RouteHeading;
                    CardLead.Text = SolverSetupCopy.RouteLead;
                    CardWhat.IsVisible = false;
                    NeedsBadgesPanel.IsVisible = false;
                    TechnicalDetailsExpander.IsVisible = false;
                }
                break;
        }
    }

    private void RenderBadges(SolverSetupStepCardViewModel card)
    {
        NeedsBadgesPanel.Children.Clear();
        if (!string.IsNullOrWhiteSpace(card.DownloadEstimate) && !string.Equals(card.DownloadEstimate, "none", StringComparison.OrdinalIgnoreCase))
            NeedsBadgesPanel.Children.Add(MakeBadge($"{card.DownloadEstimate} download", warning: false));
        if (!string.IsNullOrWhiteSpace(card.TimeEstimate))
            NeedsBadgesPanel.Children.Add(MakeBadge(card.TimeEstimate, warning: false));
        NeedsBadgesPanel.Children.Add(MakeBadge(
            card.NeedsAdmin ? SolverSetupCopy.NeedsAdminBadge : SolverSetupCopy.NoAdminBadge,
            warning: card.NeedsAdmin));
        NeedsBadgesPanel.Children.Add(MakeBadge(
            card.NeedsRestart ? SolverSetupCopy.NeedsRestartBadge : SolverSetupCopy.NoRestartBadge,
            warning: card.NeedsRestart));
        NeedsBadgesPanel.IsVisible = NeedsBadgesPanel.Children.Count > 0;
    }

    private static Border MakeBadge(string text, bool warning)
    {
        var border = new Border
        {
            Classes = { "solver-chip" },
            Child = new TextBlock { Text = text, Classes = { "caption" } }
        };
        if (warning) border.Classes.Add("warn");
        return border;
    }

    private void BindTechnicalDetails(SolverSetupStepCardViewModel? card)
    {
        if (card is null)
        {
            TechnicalDetailsExpander.IsVisible = false;
            lastCardStepId = null;
            return;
        }

        TechnicalDetailsExpander.IsVisible = true;
        TechnicalCommandText.Text = card.TechnicalCommand;
        TechnicalPinText.Text = card.TechnicalPinSummary;
        if (!string.Equals(lastCardStepId, card.StepId, StringComparison.Ordinal))
        {
            // DR-SETUP-5: collapsed by default on every step until expanded by the user.
            TechnicalDetailsExpander.IsExpanded = false;
            lastCardStepId = card.StepId;
        }
    }

    private void RenderClickPath(IReadOnlyList<string> steps)
    {
        ClickPathList.Children.Clear();
        ClickPathList.IsVisible = steps.Count > 0;
        for (int i = 0; i < steps.Count; i++)
        {
            ClickPathList.Children.Add(new TextBlock
            {
                Text = string.Create(CultureInfo.InvariantCulture, $"{i + 1}. {steps[i]}"),
                TextWrapping = TextWrapping.Wrap
            });
        }
    }

    private void RenderReadyFacts(SolverSetupViewModel vm)
    {
        ReadyFactsPanel.Children.Clear();
        var s = vm.StatusSummary;
        AddFactRow(ReadyFactsPanel, "Solver",
            $"{s.Backend ?? "Solver"} {s.Version ?? ""}" + (string.IsNullOrWhiteSpace(s.BuildId) ? "" : $", build {s.BuildId}"));
        AddFactRow(ReadyFactsPanel, "Install", s.VerifiedInstall ? "Matches the tested build" : "Accepted as unverified install");
        if (string.Equals(s.Backend, "OpenFOAM", StringComparison.OrdinalIgnoreCase))
            AddFactRow(ReadyFactsPanel, SolverSetupCopy.ReadyCaseCodeLabel, SolverSetupCopy.ReadyCaseCodeValue);
    }

    private static void AddFactRow(StackPanel panel, string label, string value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*") };
        var lbl = new TextBlock { Text = label, Classes = { "caption" } };
        var val = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(val, 1);
        grid.Children.Add(lbl);
        grid.Children.Add(val);
        panel.Children.Add(grid);
    }

    private void OnPrimaryStepAction()
    {
        if (service is null || ViewModel.RouteId is null || ViewModel.CurrentCard is not { } card)
            return;

        service.RecordConsent(
            SessionId,
            ViewModel.RouteId,
            card.StepId,
            card.TechnicalCommand,
            card.RequiresTermsAcceptance ? "COPY-782" : null,
            card.RequiresTermsAcceptance);
        ShowReceipt(SolverSetupCopy.StepConsentRecordedStatus);
        RefreshProjection();
    }

    private void OnInstallTestedBuild()
    {
        if (service is null || ViewModel.RouteId is null) return;
        var route = RouteCatalogue.GetRoute(ViewModel.RouteId);
        var downloadStep = route.Steps.FirstOrDefault(s => s.Kind == StepKind.Download) ?? route.Steps[0];
        service.RecordConsent(
            SessionId,
            route.Id,
            downloadStep.Id,
            downloadStep.TechnicalCommand,
            string.IsNullOrEmpty(downloadStep.TermsId) ? null : "COPY-782",
            !string.IsNullOrEmpty(downloadStep.TermsId));
        ShowReceipt(SolverSetupCopy.StepConsentRecordedStatus);
        RefreshProjection();
    }

    private void OnUseMineAnyway()
    {
        if (service is null || ViewModel.RouteId is null) return;
        var facts = service.Store.ReadAll();
        HostSurveyFact? latestSurvey = null;
        for (int i = facts.Count - 1; i >= 0; i--)
        {
            if (facts[i].Kind == SetupFactEnvelope.KindSurvey && facts[i].Survey is not null)
            {
                latestSurvey = facts[i].Survey;
                break;
            }
        }
        var route = RouteCatalogue.GetRoute(ViewModel.RouteId);
        var existing = latestSurvey?.ExistingInstalls.FirstOrDefault(e =>
            e.IsSupportedVersion &&
            string.Equals(e.Backend, route.BackendName, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return;

        service.AcceptUnverifiedInstall(route.Id, existing.InstallIdentitySha256, "COPY-793");
        ShowReceipt(SolverSetupCopy.UnverifiedAcceptedStatus);
        RefreshProjection();
    }

    private void OnRestartNow()
    {
        service?.RegisterRestartResume("cfd-workbench --resume-solver-setup");
        ShowReceipt(SolverSetupCopy.RestartScheduledStatus);
    }

    private void OnRemoveInstalled()
    {
        if (service is null || ViewModel.RouteId is null) return;
        var route = RouteCatalogue.GetRoute(ViewModel.RouteId);
        string? undoId = route.UndoStepIds.FirstOrDefault();
        if (undoId is null) return;
        service.RecordConsent(SessionId, route.Id, undoId, undoId, null, false);
        ShowReceipt(SolverSetupCopy.StepConsentRecordedStatus);
        RefreshProjection();
    }

    private void CopyReport()
    {
        string report = service is not null
            ? service.BuildSupportReport(AppVersion, SelectedRouteId)
            : SetupReportBuilder.BuildReport(AppVersion, null, null, []);
        LastCopiedReport = report;
        _ = TryWriteClipboardAsync(report);
        ShowReceipt(SolverSetupCopy.ReportCopiedStatus);
    }

    private void CopyTechnicalDetails()
    {
        string text = $"{TechnicalCommandText.Text}\n{TechnicalPinText.Text}".Trim();
        _ = TryWriteClipboardAsync(text);
        ShowReceipt(SolverSetupCopy.DetailsCopiedStatus);
    }

    private async Task TryWriteClipboardAsync(string text)
    {
        try
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(text);
        }
        catch (Exception)
        {
            // Headless or restricted clipboard environments still keep LastCopiedReport and inline receipt.
        }
    }

    private async Task OnAskAssistantAsync()
    {
        if (assistantHub is not null && !assistantHub.IsReadyForProposals)
        {
            ShowReceipt(!assistantHub.HasApiKey
                ? SolverSetupCopy.AssistantNoKeyNotice
                : Assistant.AssistantCopy.UnevaluatedOnThisModel);
            return;
        }
        if (!HasApiKey)
        {
            ShowReceipt(SolverSetupCopy.AssistantNoKeyNotice);
            return;
        }
        if (assistantHub is not null && assistantHub.IsReadyForProposals)
        {
            string prompt = (AssistantPromptBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(prompt)) prompt = "Explain the active solver setup step.";
            var preview = await assistantHub.PreparePreviewAsync(
                new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
                    CfdWorkbench.Core.Assistant.AssistantArea.SolverSetup,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.EnvironmentStep,
                    "Return strict AI-11 explanation or allow-listed environment-step JSON. Never suggest shell commands.",
                    prompt));
            if (preview.Allowed && preview.Prepared is not null && TopLevel.GetTopLevel(this) is Window owner)
            {
                var ctx = new AssistantValidationContext(
                    AllowedNextStepIds: BuildAllowedNextSet(),
                    ExcerptLines: ParseExcerptLines(ViewModel.OutputExcerptRedacted),
                    KnownKnowledgeIds: new HashSet<string>(StringComparer.Ordinal),
                    SharedContextText: ViewModel.CurrentCard?.PlainWhy ?? SolverSetupCopy.RouteLead,
                    CauseIsUnknown: ViewModel.Screen == SetupScreenKind.FailedUnknown,
                    FallbackFixedExplanation: ViewModel.CurrentCard?.PlainWhy ?? SolverSetupCopy.RouteLead);
                var dlg = new Assistant.AssistantConsentPreviewDialog(
                    assistantHub,
                    preview.Prepared,
                    new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                        CfdWorkbench.Core.Assistant.AssistantArea.SolverSetup,
                        CfdWorkbench.Core.Assistant.AssistantProposalKind.EnvironmentStep,
                        SetupContext: ctx))
                {
                    RequestedThemeVariant = owner.ActualThemeVariant
                };
                await dlg.ShowDialog(owner);
                if (dlg.EditPromptRequested)
                {
                    AssistantPromptBox.Focus();
                    return;
                }
                if (dlg.ExecutionResult?.Validation is { } v)
                {
                    RenderAssistantResult(new AssistantValidationResult(
                        v.Verdict, v.Reason, v.Message, v.DisplayText, v.SuggestedEnvironmentStepId, v.Citations));
                }
                return;
            }
        }

        // Offline deterministic fallback explanation when no external model transport is wired.
        string explanation = ViewModel.CurrentCard?.PlainWhy ?? SolverSetupCopy.RouteLead;
        RenderAssistantResult(new AssistantValidationResult(
            AssistantVerdict.Shown, "offline-fixed", SolverSetupCopy.AssistantAskHeading, explanation, null, []));
    }

    private void OnUseAssistantSuggestion()
    {
        if (string.IsNullOrWhiteSpace(suggestedStepId)) return;
        ShowReceipt($"Highlighted step: {suggestedStepId}. Review Technical details before starting.");
    }

    private void RenderAssistantResult(AssistantValidationResult result)
    {
        AssistantResultCard.IsVisible = true;
        AssistantResultCard.Classes.Set("withheld", result.Verdict != AssistantVerdict.Shown);
        AssistantVerdictText.Text = result.Verdict == AssistantVerdict.Shown
            ? SolverSetupCopy.AssistantBadge
            : result.Message;
        AssistantBodyText.Text = result.DisplayText;
        AssistantCitationsText.IsVisible = result.Citations.Count > 0;
        AssistantCitationsText.Text = result.Citations.Count > 0
            ? "Citations: " + string.Join(", ", result.Citations)
            : "";
        suggestedStepId = result.SuggestedStepId;
        AssistantUseSuggestionButton.IsVisible = !string.IsNullOrWhiteSpace(suggestedStepId);
    }

    private void ShowReceipt(string message)
    {
        ReportReceiptText.Text = message;
        ReportReceiptText.IsVisible = !string.IsNullOrWhiteSpace(message);
        Reported?.Invoke(new StatusReport(message));
    }

    private HashSet<string> BuildAllowedNextSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (service is not null && ViewModel.RouteId is not null && RouteCatalogue.TryGetRoute(ViewModel.RouteId, out var route) && route is not null)
        {
            var facts = service.Store.ReadAll();
            HostSurveyFact? survey = null;
            for (int i = facts.Count - 1; i >= 0; i--)
            {
                if (facts[i].Kind == SetupFactEnvelope.KindSurvey && facts[i].Survey is not null)
                {
                    survey = facts[i].Survey;
                    break;
                }
            }
            foreach (string id in StepSelector.AllowedNext(route, survey, facts, SessionId))
                set.Add(id);
        }
        else if (ViewModel.CurrentCard is { } card)
        {
            set.Add(card.StepId);
        }
        return set;
    }

    private static Dictionary<int, string> ParseExcerptLines(string? excerpt)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(excerpt)) return map;
        string[] lines = excerpt.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            map[i + 1] = lines[i].TrimEnd('\r');
        return map;
    }

    private static string HeaderLabelFor(SolverReadiness readiness) => readiness switch
    {
        SolverReadiness.Ready => SolverSetupCopy.HeaderReady,
        SolverReadiness.SettingUp => SolverSetupCopy.HeaderSettingUp,
        SolverReadiness.NeedsRestart => SolverSetupCopy.HeaderNeedsRestart,
        SolverReadiness.NotReady => SolverSetupCopy.HeaderNotReady,
        SolverReadiness.Blocked => SolverSetupCopy.HeaderBlocked,
        SolverReadiness.ExistingUnverified => SolverSetupCopy.HeaderDecide,
        _ => SolverSetupCopy.HeaderNotSetUp
    };
}
