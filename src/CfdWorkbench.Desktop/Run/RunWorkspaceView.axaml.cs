using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis.CfdRun;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Desktop.CfdRun;

/// <summary>
/// Area 5 CFD Run Pipeline workspace (rail slot 5, docs/design/cfd-run-pipeline.md §6–§7).
/// Displays the 11-state run queue &amp; live status, DR-F3-1 A / DR-F2-3 MeshGate breakdown,
/// DR-F2-1 residuals/forces table with LAB-01 three-part Converged label, Run/Cancel/Retry/Set up a solver
/// controls, and deterministic offline key-free Explain this failure / Preview case diff (AI-01/AI-11).
/// </summary>
public partial class RunWorkspaceView : UserControl
{
    private static readonly CfdRunState[] HappyPathStates =
    [
        CfdRunState.Pending,
        CfdRunState.Readiness,
        CfdRunState.Meshing,
        CfdRunState.MeshGate,
        CfdRunState.Solving,
        CfdRunState.Harvesting,
        CfdRunState.Completed
    ];

    private WorkbenchController? controller;
    private IReadOnlyList<StoredCfdRun> storedRuns = [];
    private IReadOnlyList<CfdRunSummaryView> summaries = [];
    private string? selectedRunId;
    private bool solverReady;
    private string? activeLogExcerpt;
    private CancellationTokenSource? activeRunCts;
    private bool syncingSelection;

    public event Action? SolverSetupRequested;
    public event Action? ConfigureKeyRequested;
    public event Action? OpenRepairedDraftRequested;
    public event Action<StatusReport>? StatusReported;

    /// <summary>Optional injectable substrate for live Run/Retry execution in tests or host adapters.</summary>
    public ICfdRunSubstrate? SubstrateOverride { get; set; }

    public int DisplayedStateCount => ElevenStatePanel.Children.Count;
    public string? SelectedRunId => selectedRunId;

    public RunWorkspaceView()
    {
        InitializeComponent();
        UpdatePipelineStepper(active: null, events: null);

        RunQueueList.SelectionChanged += (_, _) =>
        {
            if (syncingSelection) return;
            if (RunQueueList.SelectedIndex is >= 0 and int idx && idx < summaries.Count)
            {
                selectedRunId = summaries[idx].RunId;
                RenderSelectedRun();
            }
        };

        SetUpSolverButton.Click += (_, _) => SolverSetupRequested?.Invoke();
        RunButton.Click += async (_, _) => await HandleRunActionAsync(isRetry: false);
        RetrySampleButton.Click += async (_, _) => await HandleRunActionAsync(isRetry: true);
        CancelButton.Click += (_, _) => HandleCancelAction();
        ExplainFailureButton.Click += (_, _) => HandleExplainFailure();
        PreviewCaseDiffButton.Click += (_, _) => HandlePreviewCaseDiff();
        ExplainFailureLiveButton.Click += async (_, _) => await HandleExplainFailureLiveAsync();
        ConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        OpenRepairedDraftButton.Click += (_, _) => OpenRepairedDraftRequested?.Invoke();
        ExplainFailureLiveButton.Content = Assistant.AssistantCopy.RunExplainLiveButton;
        ConfigureKeyButton.Content = Assistant.AssistantCopy.ConfigureKeyButton;
        OpenRepairedDraftButton.Content = Assistant.AssistantCopy.OpenRepairedDraftChip;
    }

    public void RefreshAssistantStatus()
    {
        var hub = controller?.Assistant;
        bool hasKey = hub?.HasApiKey == true;
        bool ready = hub?.IsReadyForProposals == true;
        ConfigureKeyButton.IsVisible = !hasKey;
        ExplainFailureLiveButton.IsEnabled = ready && SelectedStoredRun() is not null;
        if (hub is not null)
        {
            RunAssistantUsageChipText.Text = Assistant.AssistantCopy.DailyUsageChip(
                hub.DailyRequestsUsedUtc,
                hub.CurrentSettings.DailyRequestCap);
            RunAssistantUsageChip.IsVisible = true;
        }
        else
        {
            RunAssistantUsageChip.IsVisible = false;
        }
        if (!hasKey)
        {
            OfflineNoKeyNoticeText.Text = RunCopy.OfflineNoKeyNotice;
        }
        else if (!ready)
        {
            OfflineNoKeyNoticeText.Text = Assistant.AssistantCopy.UnevaluatedOnThisModel;
        }
        else
        {
            OfflineNoKeyNoticeText.Text = $"BYOK assistant ready ({hub!.CurrentSettings.ModelId}). Offline diagnostics remain available.";
        }
    }

    /// <summary>
    /// Applies a checked <see cref="CfdWorkbench.Core.Assistant.CaseDiffProposal"/> (AI-05) by creating a new Draft
    /// Experiment version (never mutating a Queued experiment) and exposing the "Open repaired draft" jump chip.
    /// </summary>
    public void ApplyCaseDiffProposal(CfdWorkbench.Core.Assistant.CaseDiffProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (controller is not null)
        {
            if (controller.Sweeps.SelectedExperiment is not null)
                controller.Sweeps.NewVersionFromSelected();
            else
                controller.Sweeps.NewSweep();
        }
        string changeSummary = string.Join(", ", proposal.Changes.Select(c => $"{c.Field}={c.ValueDisplay}"));
        CaseDiffResultText.Text = $"New Draft experiment version created for run {proposal.FailedRunId} ({changeSummary}). Queued experiments unchanged.";
        CaseDiffResultText.IsVisible = true;
        OpenRepairedDraftButton.IsVisible = true;
    }

    private async Task HandleExplainFailureLiveAsync()
    {
        var run = SelectedStoredRun()?.Run;
        if (run is null || controller is null) return;
        var excerpts = string.IsNullOrWhiteSpace(activeLogExcerpt)
            ? Array.Empty<CfdWorkbench.Core.Assistant.UntrustedExcerpt>()
            : [new CfdWorkbench.Core.Assistant.UntrustedExcerpt("solver-log", "lines:1-50", activeLogExcerpt)];
        var preview = await controller.Assistant.PreparePreviewAsync(
            new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
                CfdWorkbench.Core.Assistant.AssistantArea.Run,
                CfdWorkbench.Core.Assistant.AssistantProposalKind.CaseDiff,
                "Analyze the failed CFD run and propose a typed case-diff JSON creating a Draft experiment version.",
                $"Propose repair for failed run {run.RunId}.",
                Excerpts: excerpts));
        if (!preview.Allowed || preview.Prepared is null)
        {
            CaseDiffResultText.Text = preview.Message ?? Assistant.AssistantCopy.NoKeyNotice;
            CaseDiffResultText.IsVisible = true;
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dlg = new Assistant.AssistantConsentPreviewDialog(
                controller.Assistant,
                preview.Prepared,
                new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                    CfdWorkbench.Core.Assistant.AssistantArea.Run,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.CaseDiff,
                    TargetExperimentIsQueued: true))
            {
                RequestedThemeVariant = owner.ActualThemeVariant
            };
            await dlg.ShowDialog(owner);
            if (dlg.Accepted && dlg.ExecutionResult?.Validation?.CaseDiff is { } diff)
            {
                ApplyCaseDiffProposal(diff);
            }
        }
    }

    public void Bind(WorkbenchController workbenchController, bool isSolverReady)
    {
        ArgumentNullException.ThrowIfNull(workbenchController);
        controller = workbenchController;
        solverReady = isSolverReady;
        RefreshFromController();
    }

    public void RefreshFromController()
    {
        if (controller is null) return;

        SolverReadinessText.Text = solverReady ? RunCopy.SolverReadyBadge : RunCopy.SolverNotReadyBadge;
        bool hasFoil = controller.Inspection is not null;
        RunButton.IsEnabled = hasFoil && activeRunCts is null;
        CancelButton.IsEnabled = activeRunCts is not null;

        CfdLedger ledger = hasFoil ? controller.ReadCfdLedger() : new CfdLedger([], [], []);
        storedRuns = ledger.Runs;
        summaries = CfdRunProjection.ProjectLedger(ledger);

        QueueSummaryText.Text = string.Create(CultureInfo.InvariantCulture,
            $"{summaries.Count} {(summaries.Count == 1 ? "run" : "runs")} recorded");
        EmptyQueueText.IsVisible = summaries.Count == 0;

        syncingSelection = true;
        try
        {
            var units = controller.AnalysisUnits;
            string speedUnit = CfdWorkbench.Analysis.Labels.SpeedUnit(units);
            var items = summaries
                .Select(s => string.Create(CultureInfo.InvariantCulture,
                    $"{s.RunKey12} · α {s.AlphaDeg:0.#}° · V {CfdWorkbench.Analysis.Labels.Speed(s.SpeedMs, units):0.##} {speedUnit} · {s.StateDisplay}"))
                .ToArray();
            RunQueueList.ItemsSource = items;

            if (summaries.Count == 0)
            {
                selectedRunId = null;
                RunQueueList.SelectedIndex = -1;
            }
            else
            {
                int idx = selectedRunId is null
                    ? summaries.Count - 1
                    : summaries.ToList().FindIndex(s => string.Equals(s.RunId, selectedRunId, StringComparison.Ordinal));
                if (idx < 0) idx = summaries.Count - 1;
                selectedRunId = summaries[idx].RunId;
                RunQueueList.SelectedIndex = idx;
            }
        }
        finally
        {
            syncingSelection = false;
        }

        RenderSelectedRun();
    }

    public void SelectRun(string runId)
    {
        selectedRunId = runId;
        int idx = summaries.ToList().FindIndex(s => string.Equals(s.RunId, runId, StringComparison.Ordinal));
        if (idx >= 0)
        {
            syncingSelection = true;
            try { RunQueueList.SelectedIndex = idx; }
            finally { syncingSelection = false; }
        }
        RenderSelectedRun();
    }

    public void SetLogExcerptForInspection(string? redactedLogExcerpt) => activeLogExcerpt = redactedLogExcerpt;

    private void RenderSelectedRun()
    {
        int idx = selectedRunId is null
            ? -1
            : summaries.ToList().FindIndex(s => string.Equals(s.RunId, selectedRunId, StringComparison.Ordinal));

        if (idx < 0 || idx >= storedRuns.Count)
        {
            SelectedRunStateText.Text = RunCopy.NoRunSelectedNote;
            BackendPinText.Text = "";
            IntegrityWarningBanner.IsVisible = false;
            ConvergedBanner.IsVisible = false;
            RetrySampleButton.IsEnabled = false;
            ExplainFailureButton.IsEnabled = false;
            PreviewCaseDiffButton.IsEnabled = false;
            UpdatePipelineStepper(active: null, events: null);
            RenderMeshGate(null, "Not evaluated");
            RenderResiduals(null, null);
            RefreshAssistantStatus();
            return;
        }

        var summary = summaries[idx];
        var stored = storedRuns[idx];
        var run = stored.Run;

        SelectedRunStateText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Run {summary.RunKey12} — {summary.StateDisplay}");
        BackendPinText.Text = summary.BackendLabel;

        UpdatePipelineStepper(summary.State, run.Events);

        IntegrityWarningBanner.IsVisible = summary.IntegrityWarning is not null;
        IntegrityWarningText.Text = summary.IntegrityWarning ?? "";

        ConvergedBanner.IsVisible = !string.IsNullOrWhiteSpace(summary.ConvergedLabel);
        ConvergedLabelText.Text = summary.ConvergedLabel ?? "";

        RetrySampleButton.IsEnabled = activeRunCts is null && CfdRunStateMachine.IsTerminal(summary.State);
        ExplainFailureButton.IsEnabled = true;
        PreviewCaseDiffButton.IsEnabled = true;

        RenderMeshGate(run.MeshGate, summary.MeshGateLabel);
        RenderResiduals(summary.ResidualSummary, run.Resources);
        RefreshAssistantStatus();
    }

    private readonly Dictionary<CfdRunState, Border> statePills = new();

    private void UpdatePipelineStepper(CfdRunState? active, IReadOnlyList<CfdRunStateEvent>? events)
    {
        if (statePills.Count == 0)
        {
            foreach (var state in HappyPathStates)
            {
                string label = state == CfdRunState.MeshGate ? "Mesh gate" : state.ToString();
                var text = new TextBlock { Text = label };
                var pill = new Border
                {
                    Classes = { "run-step-pill" },
                    Child = text
                };
                statePills[state] = pill;
                ElevenStatePanel.Children.Add(pill);
            }
        }

        HashSet<CfdRunState>? reached = events is null
            ? null
            : new HashSet<CfdRunState>(events.Select(e => e.State));

        foreach (var state in HappyPathStates)
        {
            bool isCurrent = active == state;
            bool isReached = !isCurrent && reached is not null && reached.Contains(state);
            var pill = statePills[state];
            pill.Classes.Set("active", isCurrent);
            pill.Classes.Set("reached", isReached);
        }

        switch (active)
        {
            case CfdRunState.Failed:
                TerminalStatusBadgeText.Text = "Failed";
                TerminalStatusBadge.Classes.Set("danger", true);
                TerminalStatusBadge.Classes.Set("warning", false);
                TerminalStatusBadge.IsVisible = true;
                break;
            case CfdRunState.CompletedResidualNotMet:
                TerminalStatusBadgeText.Text = "Residual not met";
                TerminalStatusBadge.Classes.Set("danger", false);
                TerminalStatusBadge.Classes.Set("warning", true);
                TerminalStatusBadge.IsVisible = true;
                break;
            case CfdRunState.Cancelled:
                TerminalStatusBadgeText.Text = "Cancelled";
                TerminalStatusBadge.Classes.Set("danger", false);
                TerminalStatusBadge.Classes.Set("warning", true);
                TerminalStatusBadge.IsVisible = true;
                break;
            case CfdRunState.Unsupported:
                TerminalStatusBadgeText.Text = "Unsupported";
                TerminalStatusBadge.Classes.Set("danger", false);
                TerminalStatusBadge.Classes.Set("warning", true);
                TerminalStatusBadge.IsVisible = true;
                break;
            default:
                TerminalStatusBadgeText.Text = "";
                TerminalStatusBadge.Classes.Set("danger", false);
                TerminalStatusBadge.Classes.Set("warning", false);
                TerminalStatusBadge.IsVisible = false;
                break;
        }
    }

    private void RenderMeshGate(MeshGateResult? gate, string label)
    {
        MeshGateStatusText.Text = label;
        if (gate is null)
        {
            MeshGateScreenOnlyBanner.IsVisible = false;
            MeshGateNonOrthoText.Text = "Non-orthogonality: Not recorded";
            MeshGateInternalSkewText.Text = "Internal skewness: Not recorded";
            MeshGateBoundarySkewText.Text = "Boundary skewness: Not recorded";
            MeshGateDeterminantText.Text = "Cell determinant: Not recorded";
            MeshGateLayerCoverageText.Text = "Layer coverage: Not recorded";
            MeshGateYPlusText.Text = "y+ (p95 / max): Not recorded";
            MeshGateFailuresList.IsVisible = false;
            MeshGateFailuresList.ItemsSource = Array.Empty<string>();
            return;
        }

        MeshGateScreenOnlyBanner.IsVisible = gate.IsScreenOnly;
        MeshGateScreenOnlyText.Text = gate.IsScreenOnly ? RunCopy.ScreenOnlyNotice : "";

        MeshGateNonOrthoText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Non-orthogonality: {gate.MaxNonOrthogonalityDeg:0.0}° (limit <= 70°)");
        MeshGateInternalSkewText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Internal skewness: {gate.MaxInternalSkewness:0.00} (limit <= 4.0)");
        MeshGateBoundarySkewText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Boundary skewness: {gate.MaxBoundarySkewness:0.00} (limit <= 20.0)");
        MeshGateDeterminantText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Cell determinant: layer {gate.MinCellDeterminantLayer:0.000} / core {gate.MinCellDeterminantCore:0.000} (limit >= 0.001)");
        MeshGateLayerCoverageText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Layer coverage: >=20 on {gate.LayerCoverage20PlusFraction * 100:0.0}% / >=10 on {gate.LayerCoverage10PlusFraction * 100:0.0}%");
        MeshGateYPlusText.Text = gate.YPlusP95 is { } p95 && gate.YPlusMax is { } max
            ? string.Create(CultureInfo.InvariantCulture, $"y+ (p95 / max): {p95:0.00} / {max:0.00}")
            : "y+ (p95 / max): Not recorded";

        if (gate.Failures.Count > 0)
        {
            MeshGateFailuresList.ItemsSource = gate.Failures
                .Select(f => string.Create(CultureInfo.InvariantCulture, $"{f.Measure}: {f.Observed:G4} vs {f.Threshold:G4} — {f.Message}"))
                .ToArray();
            MeshGateFailuresList.IsVisible = true;
        }
        else
        {
            MeshGateFailuresList.ItemsSource = Array.Empty<string>();
            MeshGateFailuresList.IsVisible = false;
        }
    }

    private void RenderResiduals(ResidualSummary? summary, CfdRunResources? resources)
    {
        if (summary is null)
        {
            ResidualsDropText.Text = "Residual drop: Not recorded";
            ForcesClText.Text = "CL (window): Not recorded";
            ForcesCdText.Text = "CD (window): Not recorded";
            ForcesCmText.Text = "CM (window): Not recorded";
            WindowUiText.Text = "Half-bands U_I: Not recorded";
            TurbulenceBoundingText.Text = "Turbulence bounding: Not recorded";
        }
        else
        {
            ResidualsDropText.Text = string.Create(CultureInfo.InvariantCulture,
                $"Residual drop: {summary.MaxInitialResidualRatio:0.0e+00} (target <= 1.0e-03)");
            ForcesClText.Text = string.Create(CultureInfo.InvariantCulture, $"CL (window): {summary.ClWindowMean:0.000}");
            ForcesCdText.Text = string.Create(CultureInfo.InvariantCulture, $"CD (window): {summary.CdWindowMean:0.0000}");
            ForcesCmText.Text = string.Create(CultureInfo.InvariantCulture, $"CM (window): {summary.CmWindowMean:0.000}");
            WindowUiText.Text = string.Create(CultureInfo.InvariantCulture,
                $"Half-bands U_I: CL {summary.ClHalfBandUi:0.0e+00} / CD {summary.CdHalfBandUi:0.0e+00} (W={summary.AveragingWindowIterations})");
            TurbulenceBoundingText.Text = string.Create(CultureInfo.InvariantCulture,
                $"Turbulence bounding: {summary.TurbulenceBoundingCountInWindow} event(s)");
        }

        ResourcesText.Text = resources is null
            ? "Resources: Not recorded"
            : string.Create(CultureInfo.InvariantCulture,
                $"Resources: {resources.Cells:N0} cells · {resources.Ranks} rank(s) · {resources.WallMs / 1000.0:0.1} s wall · {resources.CpuSeconds:0.1} cpu-s");
    }

    private async Task HandleRunActionAsync(bool isRetry)
    {
        if (controller is null || controller.Inspection is null)
        {
            StatusReported?.Invoke(new StatusReport("Open a foil before starting a CFD run.", ReportKind.Warning));
            return;
        }
        if (activeRunCts is not null)
        {
            return;
        }
        if (SubstrateOverride is null)
        {
            string msg = !solverReady
                ? "Solver is not ready. Open Solver setup to verify an installation."
                : isRetry
                    ? "Retry sample requires a configured solver substrate."
                    : "CFD run requires a configured solver substrate.";
            StatusReported?.Invoke(new StatusReport(msg, ReportKind.Warning));
            return;
        }

        var priorRun = isRetry ? SelectedStoredRun()?.Run : null;
        var (model, capability) = BuildTypedCaseModel(controller, priorRun, solverReady);

        var cts = new CancellationTokenSource();
        activeRunCts = cts;
        RunButton.IsEnabled = false;
        RetrySampleButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusReported?.Invoke(new StatusReport(isRetry ? "Retrying CFD sample…" : "Starting CFD run…"));

        try
        {
            var pipeline = new CfdCasePipelineService(controller.ActiveSession, SubstrateOverride);
            var outcome = await pipeline.ExecuteAsync(
                model,
                capability,
                experimentId: priorRun?.ExperimentId,
                caseIndex: priorRun?.CaseIndex ?? 0,
                priorRunId: priorRun?.RunId,
                ct: cts.Token);

            selectedRunId = outcome.Run.RunId;
            var lastEvent = outcome.Run.Events[^1];
            string display = CfdRunStateMachine.DisplayString(outcome.Run.CurrentState, lastEvent.Reason);
            ReportKind kind = outcome.Run.CurrentState switch
            {
                CfdRunState.Completed => ReportKind.Info,
                CfdRunState.Failed => ReportKind.Error,
                _ => ReportKind.Warning
            };
            StatusReported?.Invoke(new StatusReport($"CFD run {outcome.Run.RunKey[..12]} finished: {display}", kind));
        }
        catch (OperationCanceledException)
        {
            StatusReported?.Invoke(new StatusReport("CFD run cancelled.", ReportKind.Warning));
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke(new StatusReport($"CFD run failed: {ex.Message}", ReportKind.Error));
        }
        finally
        {
            if (ReferenceEquals(activeRunCts, cts))
                activeRunCts = null;
            cts.Dispose();
            CancelButton.IsEnabled = false;
            RunButton.IsEnabled = controller?.Inspection is not null;
            RefreshFromController();
        }
    }

    private static (TypedCaseModel Model, BackendCapability Capability) BuildTypedCaseModel(
        WorkbenchController wbController,
        CfdRunRecord? priorRun,
        bool isSolverReady)
    {
        var snap = wbController.ActiveSession.Snapshot();
        var authored = FoilSource.Parse(snap.Source).Authored();
        string[] profiles = authored.Assignments.Select(a => a.ProfileIdentity).ToArray();
        if (profiles.Length == 0) profiles = [new string('0', 64)];

        double halfSpan = wbController.Inspection?.Authored.HalfSpanMeters is { } hs && double.IsFinite(hs) && hs > 0.01 ? hs : 0.45;
        double span = halfSpan * 2.0;
        double chord = 0.12;
        double area = Math.Max(0.01, span * chord);

        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, profiles, authored.Binding.Evaluator ?? Evaluator.IdFor(snap.Source), "rule-a/1");
        var reference = priorRun?.Reference ?? new RunReference(area, span, chord, "frame origin", "body");
        var bounds = new WingBounds(0.0, chord, 0.0, halfSpan, -0.02, 0.02);
        var water = priorRun?.Water ?? wbController.AnalysisWater;
        var op = priorRun?.Op ?? wbController.AnalysisOperatingPoint;
        var pin = priorRun?.BackendPin ?? new CfdBackendPin(
            "openfoam-esi",
            "v2512",
            OperatingSystem.IsWindows() ? "wsl2" : "local",
            "local-substrate",
            new string('0', 64),
            isSolverReady);
        var capability = string.Equals(pin.Backend, "su2", StringComparison.OrdinalIgnoreCase)
            ? BackendCapability.Su2V850
            : BackendCapability.OpenFoamEsiV2512;

        return (new TypedCaseModel(inputs, reference, bounds, water, op, pin), capability);
    }

    private void HandleCancelAction()
    {
        var cts = activeRunCts;
        if (cts is null) return;
        CancelButton.IsEnabled = false;
        try { cts.Cancel(); }
        catch (ObjectDisposedException) { }
        StatusReported?.Invoke(new StatusReport("CFD run cancellation requested."));
    }

    private void HandleExplainFailure()
    {
        var run = SelectedStoredRun()?.Run;
        if (run is null) return;

        var explanation = CfdRunProjection.ExplainFailure(run, activeLogExcerpt);
        FailureExplanationSummaryText.Text = explanation.Summary;
        FailureExplanationSummaryText.IsVisible = true;
        FailureNextStepText.Text = $"Next step: {explanation.SuggestedNextStep}";
        FailureNextStepText.IsVisible = true;
        FailureCitedLinesText.Text = explanation.CitedLineNumbers.Count > 0
            ? "Cited log lines: " + string.Join(", ", explanation.CitedLineNumbers)
            : "Cited log lines: None (no log excerpt lines flagged)";
        FailureCitedLinesText.IsVisible = true;
    }

    private void HandlePreviewCaseDiff()
    {
        var run = SelectedStoredRun()?.Run;
        if (run is null) return;

        try
        {
            var proposal = new CaseDiffProposal(
                SourceRunId: run.RunId,
                MaxIterations: 2500,
                SurfaceLayers: 24,
                ExpansionRatio: 1.15,
                LogNote: activeLogExcerpt);
            var result = CfdRunProjection.ValidateCaseDiffProposal(proposal, experimentIsQueued: false);
            CaseDiffResultText.Text = result.Summary;
            CaseDiffResultText.IsVisible = true;
        }
        catch (ContractError err)
        {
            CaseDiffResultText.Text = err.Reason ?? err.Code;
            CaseDiffResultText.IsVisible = true;
        }
    }

    private StoredCfdRun? SelectedStoredRun()
    {
        if (selectedRunId is null) return null;
        for (int i = 0; i < storedRuns.Count; i++)
        {
            if (string.Equals(storedRuns[i].Run.RunId, selectedRunId, StringComparison.Ordinal))
                return storedRuns[i];
        }
        return null;
    }
}
