using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Desktop.Assistant;

/// <summary>
/// DialogChrome-conformant two-step BYOK Consent &amp; Proposal Review modal
/// (docs/design/byok-assistants.md §4.1, C3, AI-02..AI-11, Wave 12B R2-03/R2-11/C5/C6).
/// Step 1 displays the byte-exact redacted wire payload bound by SHA-256; Step 2 displays
/// the deterministically checked proposal with wrapped Before → After Stated/Inferred/Defaulted provenance,
/// withheld/refused/cancelled error recovery (Retry / Edit prompt / Discard), live CancellationTokenSource
/// busy cancellation, and C5 base-revision drift refresh back to Step 1 Consent Preview (never forging BaseAcceptedId).
/// </summary>
public partial class AssistantConsentPreviewDialog : Window
{
    private readonly AssistantRuntimeHub? hub;
    private PreparedAssistantCall? prepared;
    private AssistantValidationInput? validationInput;
    private readonly Func<AssistantValidationInput>? refreshValidationInput;
    private readonly Func<string?>? currentAcceptedIdProvider;
    private readonly Func<WhatIfOffer, Task>? onWhatIfRecompute;
    private readonly Func<Task<(PreparedAssistantCall Prepared, AssistantValidationInput ValidationInput)?>>? onRefreshOverCurrentRevision;
    private CancellationTokenSource? activeSendCts;
    private bool sent;
    private bool busy;

    public AssistantConsentPreviewDialog()
    {
        InitializeComponent();
        Icon = App.AppIcon;
        OrderFooterButtons();
        CancelButton.Click += (_, _) => Close();
        RetryButton.Click += (_, _) => { };
        EditPromptButton.Click += (_, _) => RequestEditPrompt();
        RefreshBaseButton.Click += (_, _) => { };
    }

    public AssistantConsentPreviewDialog(
        AssistantRuntimeHub hub,
        PreparedAssistantCall prepared,
        AssistantValidationInput validationInput,
        Func<AssistantValidationInput>? refreshValidationInput = null,
        Func<string?>? currentAcceptedIdProvider = null,
        Func<WhatIfOffer, Task>? onWhatIfRecompute = null,
        Func<Task<(PreparedAssistantCall Prepared, AssistantValidationInput ValidationInput)?>>? onRefreshOverCurrentRevision = null)
    {
        this.hub = hub ?? throw new ArgumentNullException(nameof(hub));
        this.prepared = prepared ?? throw new ArgumentNullException(nameof(prepared));
        this.validationInput = validationInput ?? throw new ArgumentNullException(nameof(validationInput));
        this.refreshValidationInput = refreshValidationInput;
        this.currentAcceptedIdProvider = currentAcceptedIdProvider;
        this.onWhatIfRecompute = onWhatIfRecompute;
        this.onRefreshOverCurrentRevision = onRefreshOverCurrentRevision;

        InitializeComponent();
        Icon = App.AppIcon;
        SendButton.Content = AssistantCopy.SendToModelButton;
        AcceptButton.Content = AssistantCopy.AcceptProposalButton;
        CancelButton.Content = AssistantCopy.CancelConsentButton;
        RefreshBaseButton.Content = AssistantCopy.RefreshBaseButton;
        RetryButton.Content = AssistantCopy.RetryButton;
        EditPromptButton.Content = AssistantCopy.EditPromptButton;
        WhatIfRecomputeButton.Content = AssistantCopy.WhatIfRecomputeButton;
        TruncationBannerText.Text = AssistantCopy.TruncationNotice;
        KnowledgeUnavailableText.Text = AssistantCopy.KnowledgeUnavailableNotice;
        BaseDriftText.Text = AssistantCopy.BaseRevisionDriftNotice;
        BusyStatusText.Text = AssistantCopy.SendingToModelStatus;

        OrderFooterButtons();
        BindConsentStep(this.prepared);

        SendButton.Click += async (_, _) => await SendAsync();
        AcceptButton.Click += (_, _) => Accept();
        CancelButton.Click += async (_, _) =>
        {
            if (busy)
            {
                CancelActiveRequest();
                return;
            }
            await DiscardAsync();
        };
        RefreshBaseButton.Click += async (_, _) => await RefreshOverCurrentRevisionAsync();
        RetryButton.Click += async (_, _) => await SendAsync();
        EditPromptButton.Click += (_, _) => RequestEditPrompt();
        WhatIfRecomputeButton.Click += async (_, _) =>
        {
            if (ExecutionResult?.Validation?.AnalysisExplanation?.WhatIfOffer is { } offer && onWhatIfRecompute is not null)
            {
                WhatIfRecomputeButton.IsEnabled = false;
                try { await onWhatIfRecompute(offer); }
                finally { WhatIfRecomputeButton.IsEnabled = true; }
            }
        };

        Opened += (_, _) =>
        {
            MaxHeight = DialogChrome.BoundedMaxHeight(Owner as Window);
        };
        Closed += (_, _) => CancelActiveRequest();
    }

    public AssistantExecutionResult? ExecutionResult { get; private set; }
    public bool Accepted { get; private set; }
    public bool EditPromptRequested { get; private set; }

    public void CancelActiveRequest()
    {
        try { activeSendCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public async Task<AssistantExecutionResult> SendAsync(CancellationToken ct = default)
    {
        if (hub is null || prepared is null || validationInput is null)
            throw new InvalidOperationException("Dialog was not bound to an assistant call.");
        if (busy)
            return ExecutionResult!;

        busy = true;
        SetBusyUi(true);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        activeSendCts = linkedCts;
        try
        {
            sent = true;
            var result = await hub.ExecuteConsentedAsync(
                prepared,
                prepared.Payload.PayloadSha256,
                validationInput,
                linkedCts.Token);
            ExecutionResult = result;
            RenderProposalStep(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            sent = false;
            var cancelledEntry = new AssistantAuditEntry(
                RequestId: prepared.RequestId,
                TimestampUtc: DateTimeOffset.UtcNow,
                Area: prepared.Area,
                ProposalKind: prepared.ProposalKind,
                ProviderKind: prepared.SettingsSnapshot.ProviderKind,
                EndpointOrigin: prepared.EndpointOrigin,
                ModelId: prepared.SettingsSnapshot.ModelId,
                PayloadSha256: prepared.Payload.PayloadSha256,
                PromptUtf8Bytes: prepared.Payload.PromptUtf8Bytes,
                ConsentAction: "cancelled",
                Verdict: "cancelled",
                UsageInputTokens: null,
                UsageOutputTokens: null,
                DurationMs: null,
                ErrorCode: "ASSISTANT-CANCELLED");
            var cancelledResult = new AssistantExecutionResult(
                Succeeded: false,
                Validation: null,
                Transport: null,
                AuditEntry: cancelledEntry,
                ErrorCode: "ASSISTANT-CANCELLED",
                ErrorMessage: AssistantCopy.RequestCancelledNotice,
                FallbackText: validationInput.FallbackText);
            ExecutionResult = cancelledResult;
            RenderProposalStep(cancelledResult, isCancelled: true);
            return cancelledResult;
        }
        finally
        {
            activeSendCts = null;
            busy = false;
            SetBusyUi(false);
        }
    }

    public void Accept()
    {
        if (ExecutionResult is not { Succeeded: true })
            return;
        if (IsBaseDrifted(ExecutionResult.Validation?.GeometryEdit))
        {
            ShowBaseDrift(true);
            return;
        }
        Accepted = true;
        Close();
    }

    public async Task DiscardAsync()
    {
        if (busy)
        {
            CancelActiveRequest();
            return;
        }
        if (!sent && hub is not null && prepared is not null)
        {
            await hub.RecordDeclinedAsync(prepared);
        }
        Accepted = false;
        Close();
    }

    /// <summary>
    /// R2-03 (C5 — Security HARD VETO): Re-prepares a brand-new Step 1 Consent Preview over the current
    /// AcceptedId (re-checking AI-01 key, AI-06 eligibility, UTC daily cap, and AI-04 payloadSha256).
    /// NEVER forges <c>BaseAcceptedId</c> on a stale proposal.
    /// </summary>
    public async Task<bool> RefreshOverCurrentRevisionAsync()
    {
        if (busy) return false;
        if (onRefreshOverCurrentRevision is not null)
        {
            RefreshBaseButton.IsEnabled = false;
            try
            {
                var refreshed = await onRefreshOverCurrentRevision();
                if (refreshed is { } next)
                {
                    prepared = next.Prepared;
                    validationInput = next.ValidationInput;
                    ExecutionResult = null;
                    Accepted = false;
                    sent = false;
                    BaseDriftText.Text = AssistantCopy.BaseRevisionDriftNotice;
                    BindConsentStep(prepared);
                    return true;
                }
                BaseDriftText.Text = AssistantCopy.RefreshBaseFailedNotice;
                ShowBaseDrift(true);
                return false;
            }
            finally
            {
                RefreshBaseButton.IsEnabled = true;
            }
        }

        if (refreshValidationInput is not null)
            validationInput = refreshValidationInput();
        if (ExecutionResult?.Validation?.GeometryEdit is { } edit)
        {
            bool drifted = IsBaseDrifted(edit);
            ShowBaseDrift(drifted);
            return !drifted;
        }
        return false;
    }

    private void RequestEditPrompt()
    {
        CancelActiveRequest();
        EditPromptRequested = true;
        Accepted = false;
        Close();
    }

    private void BindConsentStep(PreparedAssistantCall call)
    {
        Title = AssistantCopy.ConsentStepHeading;
        TitleText.Text = AssistantCopy.ConsentStepHeading;
        ConsentPanel.IsVisible = true;
        ProposalPanel.IsVisible = false;
        SendButton.IsVisible = true;
        SendButton.IsEnabled = true;
        AcceptButton.IsVisible = false;
        CancelButton.Content = AssistantCopy.CancelConsentButton;
        CancelButton.IsEnabled = true;
        VerdictBanner.IsVisible = false;
        RetryButton.IsVisible = false;
        EditPromptButton.IsVisible = false;
        BaseDriftBanner.IsVisible = false;
        ProvenanceCard.IsVisible = false;
        WhatIfRecomputeButton.IsVisible = false;

        WireIdentityText.Text = AssistantCopy.ConsentWireSummary(
            call.EndpointOrigin,
            call.SettingsSnapshot.ModelId,
            call.Payload.PromptUtf8Bytes,
            call.Payload.PayloadSha256);
        RedactionSummaryText.Text = AssistantCopy.RedactionSummaryLine(call.Payload.Summary);
        TruncationBanner.IsVisible = call.Payload.Summary.PromptTruncated ||
                                     call.Payload.Summary.TruncatedExcerptIds.Count > 0;
        KnowledgeUnavailableBanner.IsVisible = !call.KnowledgeAvailable;
        PayloadPreviewBox.Text = call.Payload.CanonicalWirePreview;
    }

    private void SetBusyUi(bool isBusy)
    {
        BusyBanner.IsVisible = isBusy;
        BusyStatusText.Text = AssistantCopy.SendingToModelStatus;
        SendButton.IsEnabled = !isBusy;
        RetryButton.IsEnabled = !isBusy;
        EditPromptButton.IsEnabled = !isBusy;
        RefreshBaseButton.IsEnabled = !isBusy;
        if (isBusy)
        {
            SetAcceptEnabled(false);
            CancelButton.Content = AssistantCopy.CancelConsentButton;
            CancelButton.IsEnabled = true;
        }
    }

    private void RenderProposalStep(AssistantExecutionResult result, bool isCancelled = false)
    {
        Title = AssistantCopy.ProposalStepHeading;
        TitleText.Text = AssistantCopy.ProposalStepHeading;
        ConsentPanel.IsVisible = false;
        ProposalPanel.IsVisible = true;
        SendButton.IsVisible = false;
        AcceptButton.IsVisible = true;
        CancelButton.Content = AssistantCopy.DiscardProposalButton;

        if (result.Validation is null)
        {
            string notice = isCancelled
                ? AssistantCopy.RequestCancelledNotice
                : AssistantCopy.TransportErrorNotice(
                    result.Transport?.ErrorKind.ToString() ?? result.ErrorCode ?? "Error",
                    result.ErrorMessage ?? "Request failed.");
            VerdictBannerText.Text = notice;
            VerdictBanner.IsVisible = true;
            RetryButton.IsVisible = true;
            RetryButton.IsEnabled = true;
            EditPromptButton.IsVisible = true;
            EditPromptButton.IsEnabled = true;
            BaseDriftBanner.IsVisible = false;
            ProposalSummaryText.Text = result.FallbackText ?? notice;
            ProposalCitationsText.IsVisible = false;
            WhatIfRecomputeButton.IsVisible = false;
            ProvenanceCard.IsVisible = false;
            SetAcceptEnabled(false);
            return;
        }

        var val = result.Validation;
        if (val.Verdict == AssistantVerdict.Refused)
        {
            VerdictBannerText.Text = AssistantCopy.ProposalRefusedBanner(val.Message);
            VerdictBanner.IsVisible = true;
            RetryButton.IsVisible = true;
            RetryButton.IsEnabled = true;
            EditPromptButton.IsVisible = true;
            EditPromptButton.IsEnabled = true;
            BaseDriftBanner.IsVisible = false;
            ProposalSummaryText.Text = string.IsNullOrWhiteSpace(val.DisplayText) ? val.Message : val.DisplayText;
            SetAcceptEnabled(false);
        }
        else if (val.Verdict == AssistantVerdict.Withheld)
        {
            VerdictBannerText.Text = AssistantCopy.ResponseWithheldBanner(val.Message);
            VerdictBanner.IsVisible = true;
            RetryButton.IsVisible = true;
            RetryButton.IsEnabled = true;
            EditPromptButton.IsVisible = true;
            EditPromptButton.IsEnabled = true;
            BaseDriftBanner.IsVisible = false;
            ProposalSummaryText.Text = string.IsNullOrWhiteSpace(val.DisplayText) ? val.Message : val.DisplayText;
            SetAcceptEnabled(false);
        }
        else
        {
            VerdictBanner.IsVisible = false;
            RetryButton.IsVisible = false;
            EditPromptButton.IsVisible = false;
            ProposalSummaryText.Text = string.IsNullOrWhiteSpace(val.DisplayText) ? val.Message : val.DisplayText;
            bool drifted = IsBaseDrifted(val.GeometryEdit);
            ShowBaseDrift(drifted);
            SetAcceptEnabled(!drifted);
        }

        if (val.Citations.Count > 0)
        {
            ProposalCitationsText.Text = "Citations: " + string.Join(", ", val.Citations);
            ProposalCitationsText.IsVisible = true;
        }
        else
        {
            ProposalCitationsText.IsVisible = false;
        }

        WhatIfRecomputeButton.IsVisible = val.Verdict == AssistantVerdict.Shown &&
                                          val.AnalysisExplanation?.WhatIfOffer is not null &&
                                          onWhatIfRecompute is not null;

        var provenance = ExtractProvenance(val);
        if (provenance.Count > 0)
        {
            ProvenanceRowsList.ItemsSource = provenance
                .Select(p => BuildProvenanceRow(p, val))
                .ToArray();
            ProvenanceCard.IsVisible = true;
        }
        else
        {
            ProvenanceCard.IsVisible = false;
        }
    }

    private static ProvenanceRowItem BuildProvenanceRow(AssistantFieldProvenance p, AssistantResponseValidation val)
    {
        string tag = p.Tag switch
        {
            ProvenanceTag.Stated => AssistantCopy.ProvenanceStated,
            ProvenanceTag.Inferred => AssistantCopy.ProvenanceInferred,
            _ => AssistantCopy.ProvenanceDefaulted
        };

        string value = p.ValueDisplay.Replace("->", "→", StringComparison.Ordinal);
        string changeDisplay;
        if (value.Contains('→', StringComparison.Ordinal))
        {
            changeDisplay = value;
        }
        else if (val.GeometryEdit is { } geo)
        {
            var lockOp = geo.Operations.FirstOrDefault(op =>
                op.Kind == GeometryEditOpKind.LockChange &&
                op.Channel is not null &&
                p.FieldName.Contains(op.Channel, StringComparison.OrdinalIgnoreCase));
            changeDisplay = lockOp is not null
                ? (lockOp.Locked == true ? "Unlocked → Locked" : "Locked → Unlocked")
                : $"Current → {value}";
        }
        else
        {
            changeDisplay = $"Current → {value}";
        }

        return new ProvenanceRowItem(tag, p.FieldName, changeDisplay, p.Reason);
    }

    private bool IsBaseDrifted(GeometryEditDraftSpec? edit)
    {
        if (edit is null) return false;
        if (edit.BaseDriftDetected || !edit.AcceptAllowed) return true;
        string? currentId = currentAcceptedIdProvider?.Invoke();
        return !string.IsNullOrWhiteSpace(currentId) &&
               !string.Equals(currentId, edit.BaseAcceptedId, StringComparison.Ordinal);
    }

    private void ShowBaseDrift(bool drifted)
    {
        BaseDriftBanner.IsVisible = drifted;
        if (drifted) SetAcceptEnabled(false);
    }

    private void SetAcceptEnabled(bool enabled)
    {
        AcceptButton.IsEnabled = enabled;
        AutomationProperties.SetItemStatus(AcceptButton, enabled ? "" : "disabled");
        AcceptButton.Classes.Set("disabled", !enabled);
    }

    private static IReadOnlyList<AssistantFieldProvenance> ExtractProvenance(AssistantResponseValidation val)
    {
        if (val.SetupSeed is { Provenance.Count: > 0 } s) return s.Provenance;
        if (val.GeometryEdit is { Provenance.Count: > 0 } g) return g.Provenance;
        if (val.ExperimentConfig is { Provenance.Count: > 0 } e) return e.Provenance;
        if (val.CaseDiff is { Provenance.Count: > 0 } c) return c.Provenance;
        return [];
    }

    private void OrderFooterButtons()
    {
        if (OperatingSystem.IsMacOS())
        {
            FooterButtons.Children.Clear();
            FooterButtons.Children.Add(CancelButton);
            FooterButtons.Children.Add(SendButton);
            FooterButtons.Children.Add(AcceptButton);
        }
    }
}
