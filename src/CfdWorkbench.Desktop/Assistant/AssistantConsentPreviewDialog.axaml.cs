using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Desktop.Assistant;

/// <summary>
/// DialogChrome-conformant two-step BYOK Consent &amp; Proposal Review modal
/// (docs/design/byok-assistants.md §4.1, C3, AI-02..AI-11).
/// Step 1 displays the byte-exact redacted wire payload bound by SHA-256; Step 2 displays
/// the deterministically checked proposal with per-field Stated/Inferred/Defaulted provenance,
/// withheld/refused reasons, base-revision drift guard, and Accept/Discard actions.
/// </summary>
public partial class AssistantConsentPreviewDialog : Window
{
    private readonly AssistantRuntimeHub? hub;
    private readonly PreparedAssistantCall? prepared;
    private AssistantValidationInput? validationInput;
    private readonly Func<AssistantValidationInput>? refreshValidationInput;
    private readonly Func<string?>? currentAcceptedIdProvider;
    private readonly Func<WhatIfOffer, Task>? onWhatIfRecompute;
    private bool sent;
    private bool busy;

    public AssistantConsentPreviewDialog()
    {
        InitializeComponent();
        Icon = App.AppIcon;
        OrderFooterButtons();
        CancelButton.Click += (_, _) => Close();
    }

    public AssistantConsentPreviewDialog(
        AssistantRuntimeHub hub,
        PreparedAssistantCall prepared,
        AssistantValidationInput validationInput,
        Func<AssistantValidationInput>? refreshValidationInput = null,
        Func<string?>? currentAcceptedIdProvider = null,
        Func<WhatIfOffer, Task>? onWhatIfRecompute = null)
    {
        this.hub = hub ?? throw new ArgumentNullException(nameof(hub));
        this.prepared = prepared ?? throw new ArgumentNullException(nameof(prepared));
        this.validationInput = validationInput ?? throw new ArgumentNullException(nameof(validationInput));
        this.refreshValidationInput = refreshValidationInput;
        this.currentAcceptedIdProvider = currentAcceptedIdProvider;
        this.onWhatIfRecompute = onWhatIfRecompute;

        InitializeComponent();
        Icon = App.AppIcon;
        Title = AssistantCopy.ConsentStepHeading;
        TitleText.Text = AssistantCopy.ConsentStepHeading;
        SendButton.Content = AssistantCopy.SendToModelButton;
        AcceptButton.Content = AssistantCopy.AcceptProposalButton;
        CancelButton.Content = AssistantCopy.CancelConsentButton;
        RefreshBaseButton.Content = AssistantCopy.RefreshBaseButton;
        WhatIfRecomputeButton.Content = AssistantCopy.WhatIfRecomputeButton;
        TruncationBannerText.Text = AssistantCopy.TruncationNotice;
        KnowledgeUnavailableText.Text = AssistantCopy.KnowledgeUnavailableNotice;
        BaseDriftText.Text = AssistantCopy.BaseRevisionDriftNotice;

        OrderFooterButtons();

        WireIdentityText.Text = AssistantCopy.ConsentWireSummary(
            prepared.EndpointOrigin,
            prepared.SettingsSnapshot.ModelId,
            prepared.Payload.PromptUtf8Bytes,
            prepared.Payload.PayloadSha256);
        RedactionSummaryText.Text = AssistantCopy.RedactionSummaryLine(prepared.Payload.Summary);
        TruncationBanner.IsVisible = prepared.Payload.Summary.PromptTruncated ||
                                     prepared.Payload.Summary.TruncatedExcerptIds.Count > 0;
        KnowledgeUnavailableBanner.IsVisible = !prepared.KnowledgeAvailable;
        PayloadPreviewBox.Text = prepared.Payload.CanonicalWirePreview;

        SendButton.Click += async (_, _) => await SendAsync();
        AcceptButton.Click += (_, _) => Accept();
        CancelButton.Click += async (_, _) => await DiscardAsync();
        RefreshBaseButton.Click += (_, _) => HandleRefreshBase();
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
    }

    public AssistantExecutionResult? ExecutionResult { get; private set; }
    public bool Accepted { get; private set; }

    public async Task<AssistantExecutionResult> SendAsync(CancellationToken ct = default)
    {
        if (hub is null || prepared is null || validationInput is null)
            throw new InvalidOperationException("Dialog was not bound to an assistant call.");
        if (busy)
            return ExecutionResult!;

        busy = true;
        SendButton.IsEnabled = false;
        try
        {
            sent = true;
            var result = await hub.ExecuteConsentedAsync(
                prepared,
                prepared.Payload.PayloadSha256,
                validationInput,
                ct);
            ExecutionResult = result;
            RenderProposalStep(result);
            return result;
        }
        finally
        {
            busy = false;
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
        if (!sent && hub is not null && prepared is not null)
        {
            await hub.RecordDeclinedAsync(prepared);
        }
        Accepted = false;
        Close();
    }

    private void HandleRefreshBase()
    {
        if (refreshValidationInput is not null)
            validationInput = refreshValidationInput();
        if (ExecutionResult?.Validation?.GeometryEdit is { } edit)
        {
            bool drifted = IsBaseDrifted(edit);
            ShowBaseDrift(drifted);
        }
    }

    private void RenderProposalStep(AssistantExecutionResult result)
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
            string kind = result.Transport?.ErrorKind.ToString() ?? result.ErrorCode ?? "Error";
            VerdictBannerText.Text = AssistantCopy.TransportErrorNotice(kind, result.ErrorMessage ?? "Request failed.");
            VerdictBanner.IsVisible = true;
            ProposalSummaryText.Text = result.FallbackText ?? AssistantCopy.TransportErrorNotice(kind, result.ErrorMessage ?? "Request failed.");
            SetAcceptEnabled(false);
            return;
        }

        var val = result.Validation;
        if (val.Verdict == AssistantVerdict.Refused)
        {
            VerdictBannerText.Text = AssistantCopy.ProposalRefusedBanner(val.Message);
            VerdictBanner.IsVisible = true;
            ProposalSummaryText.Text = string.IsNullOrWhiteSpace(val.DisplayText) ? val.Message : val.DisplayText;
            SetAcceptEnabled(false);
        }
        else if (val.Verdict == AssistantVerdict.Withheld)
        {
            VerdictBannerText.Text = AssistantCopy.ResponseWithheldBanner(val.Message);
            VerdictBanner.IsVisible = true;
            ProposalSummaryText.Text = string.IsNullOrWhiteSpace(val.DisplayText) ? val.Message : val.DisplayText;
            SetAcceptEnabled(false);
        }
        else
        {
            VerdictBanner.IsVisible = false;
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
                .Select(p =>
                {
                    string tag = p.Tag switch
                    {
                        ProvenanceTag.Stated => AssistantCopy.ProvenanceStated,
                        ProvenanceTag.Inferred => AssistantCopy.ProvenanceInferred,
                        _ => AssistantCopy.ProvenanceDefaulted
                    };
                    return $"[{tag}] {p.FieldName}: {p.ValueDisplay} — {p.Reason}";
                })
                .ToArray();
            ProvenanceCard.IsVisible = true;
        }
        else
        {
            ProvenanceCard.IsVisible = false;
        }
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
