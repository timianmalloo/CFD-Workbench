using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Desktop.Assistant;

/// <summary>
/// DialogChrome-conformant BYOK Assistant Settings modal (docs/design/byok-assistants.md §4.1, C4, Wave 12B R2-10/R2-11).
/// Opened from ActivityRail Settings button, CommandTable <c>view.settings</c> (<c>Modal: true</c>),
/// and every area's "Configure key" action. Password-masked OS credential store Save/Remove takes effect
/// immediately without restart; model/provider/endpoint edits immediately enforce <c>AI-06</c>.
/// </summary>
public partial class AssistantSettingsDialog : Window
{
    private readonly AssistantRuntimeHub hub;
    private CancellationTokenSource? activeEvalCts;
    private bool syncing;

    public AssistantSettingsDialog()
        : this(new AssistantRuntimeHub())
    {
    }

    public AssistantSettingsDialog(AssistantRuntimeHub hub)
    {
        this.hub = hub ?? throw new ArgumentNullException(nameof(hub));
        InitializeComponent();
        Icon = App.AppIcon;
        Title = AssistantCopy.SettingsDialogTitle;
        TitleText.Text = AssistantCopy.SettingsDialogTitle;
        CredentialSectionHeadingText.Text = AssistantCopy.CredentialSectionHeading;
        SaveKeyButton.Content = AssistantCopy.SaveKeyButton;
        RemoveKeyButton.Content = AssistantCopy.RemoveKeyButton;
        ProviderSectionHeadingText.Text = AssistantCopy.ProviderSectionHeading;
        EndpointUrlLabelText.Text = AssistantCopy.EndpointUrlShortLabel;
        EndpointHelperText.Text = AssistantCopy.EndpointUrlHelperText;
        ModelIdLabelText.Text = AssistantCopy.ModelIdLabel;
        CapsSectionHeadingText.Text = AssistantCopy.CapsSectionHeading;
        PerRequestCapLabelText.Text = AssistantCopy.PerRequestCapLabel;
        DailyCapLabelText.Text = AssistantCopy.DailyCapLabel;
        TimeoutLabelText.Text = AssistantCopy.TimeoutLabel;
        EvalSectionHeadingText.Text = AssistantCopy.EvalSectionHeading;
        RunEvalButton.Content = AssistantCopy.RunEvalButton;
        CancelEvalButton.Content = AssistantCopy.CancelConsentButton;
        EvalBusyStatusText.Text = AssistantCopy.RunningEvaluationStatus;
        AuditSectionHeadingText.Text = AssistantCopy.AuditSectionHeading;
        AuditEmptyText.Text = AssistantCopy.AuditEmptyNotice;

        if (OperatingSystem.IsMacOS())
        {
            FooterButtons.Children.Clear();
            FooterButtons.Children.Add(CloseButton);
            FooterButtons.Children.Add(SaveSettingsButton);
        }

        ProviderPicker.ItemsSource = new[]
        {
            AssistantCopy.ProviderAnthropic,
            AssistantCopy.ProviderOpenAiCompatible
        };

        SaveKeyButton.Click += (_, _) => HandleSaveKey();
        RemoveKeyButton.Click += (_, _) => HandleRemoveKey();
        SaveSettingsButton.Click += async (_, _) => await SaveSettingsNowAsync();
        RunEvalButton.Click += async (_, _) => await RunEvaluationNowAsync();
        CancelEvalButton.Click += (_, _) => CancelEvaluation();
        CloseButton.Click += (_, _) =>
        {
            CancelEvaluation();
            Close();
        };

        ProviderPicker.SelectionChanged += (_, _) => OnProviderChanged();
        EndpointUrlBox.TextChanged += (_, _) => OnModelOrEndpointEdited();
        ModelIdBox.TextChanged += (_, _) => OnModelOrEndpointEdited();

        Opened += (_, _) =>
        {
            MaxHeight = DialogChrome.BoundedMaxHeight(Owner as Window);
            _ = RefreshFromStoreAsync();
        };
        Closed += (_, _) => CancelEvaluation();

        BindSettings(hub.CurrentSettings, hub.Eligibility, hub.DailyRequestsUsedUtc);
    }

    /// <summary>Optional flag allowing tests or operators to toggle live-wire vs deterministic suite evaluation.</summary>
    public bool UseLiveWireEval { get; set; }

    public void CancelEvaluation()
    {
        try { activeEvalCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public async Task RefreshFromStoreAsync()
    {
        var status = await hub.RefreshStatusAsync();
        var audit = await hub.ReadAuditEntriesAsync(50);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BindSettings(status.Settings, status.Eligibility, status.DailyRequestsUsedUtc);
            RenderAudit(audit);
        });
    }

    public async Task<AssistantSettingsSaveResult> SaveSettingsNowAsync()
    {
        if (!TryBuildCandidateSettings(out var candidate, out string? error))
        {
            ShowEndpointOrSettingsError(error ?? AssistantCopy.EndpointInvalidError);
            return new AssistantSettingsSaveResult(false, "rejected", "ASSISTANT-UI-VALIDATION", error);
        }

        var result = await hub.SaveSettingsAsync(candidate!);
        if (result.Succeeded)
        {
            ClearEndpointError();
            SettingsFeedbackText.Text = "Settings saved.";
            SettingsFeedbackText.IsVisible = true;
            BindSettings(hub.CurrentSettings, hub.Eligibility, hub.DailyRequestsUsedUtc);
        }
        else
        {
            ShowEndpointOrSettingsError(result.Message ?? "Failed to save settings.");
        }
        return result;
    }

    public async Task<AssistantEvalRunResult?> RunEvaluationNowAsync(CancellationToken ct = default)
    {
        if (activeEvalCts is not null)
            return null;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        activeEvalCts = linkedCts;
        RunEvalButton.IsEnabled = false;
        CancelEvalButton.IsVisible = true;
        CancelEvalButton.IsEnabled = true;
        EvalBusyBanner.IsVisible = true;
        EvalBusyStatusText.Text = AssistantCopy.RunningEvaluationStatus;
        try
        {
            if (TryBuildCandidateSettings(out var candidate, out _))
            {
                await hub.SaveSettingsAsync(candidate!, linkedCts.Token);
            }

            var res = await hub.RunEvaluationAsync(liveWire: UseLiveWireEval, ct: linkedCts.Token);
            var audit = await hub.ReadAuditEntriesAsync(50, linkedCts.Token);
            BindSettings(hub.CurrentSettings, res.Eligibility, hub.DailyRequestsUsedUtc);
            RenderAudit(audit);
            return res;
        }
        catch (OperationCanceledException)
        {
            SettingsFeedbackText.Text = AssistantCopy.EvaluationCancelledNotice;
            SettingsFeedbackText.IsVisible = true;
            return null;
        }
        finally
        {
            activeEvalCts = null;
            EvalBusyBanner.IsVisible = false;
            CancelEvalButton.IsVisible = false;
            RunEvalButton.IsEnabled = true;
        }
    }

    private void HandleSaveKey()
    {
        var provider = SelectedProvider();
        string raw = ApiKeyBox.Text ?? "";
        var res = hub.SaveKey(provider, raw);
        ApiKeyBox.Text = "";
        if (res.Succeeded)
        {
            KeyFeedbackText.Text = AssistantCopy.KeySavedNotice;
            KeyFeedbackText.IsVisible = true;
        }
        else
        {
            KeyFeedbackText.Text = res.Message ?? AssistantCopy.KeyEmptyError;
            KeyFeedbackText.IsVisible = true;
        }
        UpdateCredentialChrome();
    }

    private void HandleRemoveKey()
    {
        var provider = SelectedProvider();
        var res = hub.RemoveKey(provider);
        ApiKeyBox.Text = "";
        KeyFeedbackText.Text = res.Succeeded
            ? AssistantCopy.KeyRemovedReceipt
            : (res.Message ?? AssistantCopy.CredentialStoreUnsupported);
        KeyFeedbackText.IsVisible = true;
        UpdateCredentialChrome();
    }

    private void OnProviderChanged()
    {
        if (syncing) return;
        var provider = SelectedProvider();
        string currentEndpoint = (EndpointUrlBox.Text ?? "").Trim();
        if (provider == AssistantProviderKind.Anthropic &&
            string.Equals(currentEndpoint, AssistantSettings.DefaultOpenAiEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            EndpointUrlBox.Text = AssistantSettings.DefaultAnthropicEndpoint;
        }
        else if (provider == AssistantProviderKind.OpenAiCompatible &&
                 string.Equals(currentEndpoint, AssistantSettings.DefaultAnthropicEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            EndpointUrlBox.Text = AssistantSettings.DefaultOpenAiEndpoint;
        }
        UpdateCredentialChrome();
        OnModelOrEndpointEdited();
    }

    private void OnModelOrEndpointEdited()
    {
        if (syncing) return;

        string endpoint = (EndpointUrlBox.Text ?? "").Trim();
        var check = ByokLlmTransport.ValidateEndpointUri(endpoint);
        if (!check.IsValid)
        {
            ShowEndpointError(check.Message ?? AssistantCopy.EndpointInvalidError);
        }
        else
        {
            ClearEndpointError();
        }

        // AI-06: immediately evaluate eligibility for the edited provider/endpoint/model so any model change
        // immediately shows "Unevaluated on this model" before even closing Settings.
        var candidate = hub.CurrentSettings with
        {
            ProviderKind = SelectedProvider(),
            EndpointUrl = endpoint,
            ModelId = (ModelIdBox.Text ?? "").Trim()
        };
        var eligibility = AssistantEvalSuite.CheckModelEligibility(candidate, hub.CurrentSettings.EvalReceipts);
        RenderEligibility(candidate, eligibility);
    }

    private void BindSettings(AssistantSettings settings, ModelEligibilityStatus eligibility, int dailyUsed)
    {
        syncing = true;
        try
        {
            ProviderPicker.SelectedIndex = settings.ProviderKind == AssistantProviderKind.OpenAiCompatible ? 1 : 0;
            EndpointUrlBox.Text = settings.EndpointUrl;
            ModelIdBox.Text = settings.ModelId;
            PerRequestMaxTokensBox.Text = settings.PerRequestMaxTokens.ToString(CultureInfo.InvariantCulture);
            DailyCapBox.Text = settings.DailyRequestCap.ToString(CultureInfo.InvariantCulture);
            TimeoutSecondsBox.Text = settings.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
            DailyUsageText.Text = AssistantCopy.DailyUsageSummary(dailyUsed, settings.DailyRequestCap);
        }
        finally
        {
            syncing = false;
        }

        UpdateCredentialChrome();
        ClearEndpointError();
        RenderEligibility(settings, eligibility);
    }

    private void UpdateCredentialChrome()
    {
        bool supported = hub.Credentials.IsSupported;
        bool hasKey = supported && hub.Credentials.HasKey(SelectedProvider());
        KeyStatusText.Text = !supported
            ? AssistantCopy.CredentialStoreUnsupported
            : hasKey
                ? AssistantCopy.KeySavedNotice
                : AssistantCopy.NoKeyNotice;

        SaveKeyButton.IsEnabled = supported;
        SetAutomationState(SaveKeyButton, supported ? null : "disabled");

        RemoveKeyButton.IsEnabled = hasKey;
        SetAutomationState(RemoveKeyButton, hasKey ? null : "disabled");
    }

    private void RenderEligibility(AssistantSettings settings, ModelEligibilityStatus eligibility)
    {
        EvalStatusBadgeText.Text = eligibility.StatusLabel;
        bool passed = eligibility.ProposalsEnabled && eligibility.MatchingReceipt is not null;
        bool failed = !passed && eligibility.FailingItemIds.Count > 0;
        EvalStatusBanner.Classes.Set("eval-pass", passed);
        EvalStatusBanner.Classes.Set("eval-fail", failed);

        if (passed && eligibility.MatchingReceipt is { } receipt)
        {
            EvalStatusDetailText.Text = AssistantCopy.EvalPassedStatus(
                receipt.ModelId, receipt.EvaluatedAtUtc, receipt.EvalThresholdsVersion);
        }
        else if (failed)
        {
            EvalStatusDetailText.Text = AssistantCopy.EvalFailedStatus(eligibility.FailingItemIds);
        }
        else
        {
            EvalStatusDetailText.Text = AssistantCopy.UnevaluatedDetail(AssistantEvalSuite.EvalThresholdsVersion);
        }

        var rows = new List<EvalRowItem>();
        var suite = AssistantEvalSuite.GetSuiteDefinition();
        var byId = eligibility.MatchingReceipt?.Items.ToDictionary(i => i.ItemId, StringComparer.Ordinal);
        foreach (var def in suite)
        {
            if (def.Reserved)
            {
                rows.Add(new EvalRowItem(def.ItemId, def.Title, "Not active in v1", AssistantCopy.EvalItemReserved, IsReserved: true, HasRun: false));
            }
            else if (byId is not null && byId.TryGetValue(def.ItemId, out var res))
            {
                string status = res.MetThreshold ? "PASS" : "FAIL";
                string score = string.Create(CultureInfo.InvariantCulture,
                    $"{res.PassedCases}/{res.TotalCases} ({res.PassRate * 100:0.#}% vs >= {res.Threshold * 100:0.#}%)");
                rows.Add(new EvalRowItem(res.ItemId, def.Title, score, status, IsReserved: false, HasRun: true));
            }
            else
            {
                string threshold = string.Create(CultureInfo.InvariantCulture, $"threshold >= {def.Threshold * 100:0.#}%");
                rows.Add(new EvalRowItem(def.ItemId, def.Title, threshold, "Not run", IsReserved: false, HasRun: false));
            }
        }
        EvalItemsList.ItemsSource = rows;
    }

    private void RenderAudit(IReadOnlyList<AssistantAuditEntry> entries)
    {
        AuditEmptyText.IsVisible = entries.Count == 0;
        AuditEntriesList.ItemsSource = entries
            .Reverse()
            .Select(e =>
            {
                string hash12 = e.PayloadSha256.Length >= 12 ? e.PayloadSha256[..12] : e.PayloadSha256;
                string usage = e.UsageInputTokens is { } || e.UsageOutputTokens is { }
                    ? string.Create(CultureInfo.InvariantCulture, $"in={e.UsageInputTokens?.ToString(CultureInfo.InvariantCulture) ?? AssistantCopy.NotRecorded} out={e.UsageOutputTokens?.ToString(CultureInfo.InvariantCulture) ?? AssistantCopy.NotRecorded}")
                    : AssistantCopy.NotRecorded;
                string dur = e.DurationMs is { } ms
                    ? string.Create(CultureInfo.InvariantCulture, $"{ms} ms")
                    : AssistantCopy.NotRecorded;
                string header = string.Create(CultureInfo.InvariantCulture, $"{e.TimestampUtc:yyyy-MM-dd HH:mm:ss}Z · {e.Area} · {e.ModelId}");
                string metrics = $"sha={hash12} · {usage} · {dur}";
                string outcome = $"{e.ConsentAction}/{e.Verdict}";
                string summary = $"{header} · {metrics} · {outcome}";
                return new AuditRowItem(header, metrics, outcome, summary);
            })
            .ToArray();
    }

    private bool TryBuildCandidateSettings(out AssistantSettings? settings, out string? error)
    {
        settings = null;
        string endpoint = (EndpointUrlBox.Text ?? "").Trim();
        var endpointCheck = ByokLlmTransport.ValidateEndpointUri(endpoint);
        if (!endpointCheck.IsValid)
        {
            error = endpointCheck.Message ?? AssistantCopy.EndpointInvalidError;
            return false;
        }

        string modelId = (ModelIdBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(modelId))
        {
            error = "Model identifier is required.";
            return false;
        }

        if (!int.TryParse(PerRequestMaxTokensBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxTokens) ||
            maxTokens < AssistantSettings.MinPerRequestMaxTokens || maxTokens > AssistantSettings.MaxPerRequestMaxTokens)
        {
            error = "Max output tokens must be between 128 and 16384.";
            return false;
        }

        if (!int.TryParse(DailyCapBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dailyCap) ||
            dailyCap < AssistantSettings.MinDailyRequestCap || dailyCap > AssistantSettings.MaxDailyRequestCap)
        {
            error = "Daily UTC request cap must be between 1 and 500.";
            return false;
        }

        if (!int.TryParse(TimeoutSecondsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeoutSec) ||
            timeoutSec < AssistantSettings.MinTimeoutSeconds || timeoutSec > AssistantSettings.MaxTimeoutSeconds)
        {
            error = "Timeout must be between 5 and 120 seconds.";
            return false;
        }

        settings = hub.CurrentSettings with
        {
            ProviderKind = SelectedProvider(),
            EndpointUrl = endpoint,
            ModelId = modelId,
            PerRequestMaxTokens = maxTokens,
            DailyRequestCap = dailyCap,
            TimeoutSeconds = timeoutSec
        };
        error = null;
        return true;
    }

    private AssistantProviderKind SelectedProvider() =>
        ProviderPicker.SelectedIndex == 1 ? AssistantProviderKind.OpenAiCompatible : AssistantProviderKind.Anthropic;

    private void ShowEndpointError(string message)
    {
        EndpointErrorText.Text = message;
        EndpointErrorText.IsVisible = true;
        SetAutomationState(EndpointUrlBox, "invalid");
    }

    private void ClearEndpointError()
    {
        EndpointErrorText.Text = "";
        EndpointErrorText.IsVisible = false;
        SetAutomationState(EndpointUrlBox, null);
    }

    private void ShowEndpointOrSettingsError(string message)
    {
        ShowEndpointError(message);
        SettingsFeedbackText.Text = message;
        SettingsFeedbackText.IsVisible = true;
    }

    private static void SetAutomationState(Control control, string? status)
    {
        AutomationProperties.SetItemStatus(control, status ?? "");
        control.Classes.Set("disabled", string.Equals(status, "disabled", StringComparison.Ordinal));
        control.Classes.Set("invalid", string.Equals(status, "invalid", StringComparison.Ordinal));
    }
}
