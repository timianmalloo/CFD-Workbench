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
    private static readonly CfdRunState[] AllElevenStates =
    [
        CfdRunState.Pending,
        CfdRunState.Unsupported,
        CfdRunState.Readiness,
        CfdRunState.Meshing,
        CfdRunState.MeshGate,
        CfdRunState.Solving,
        CfdRunState.Harvesting,
        CfdRunState.Completed,
        CfdRunState.CompletedResidualNotMet,
        CfdRunState.Failed,
        CfdRunState.Cancelled
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
    public event Action<StatusReport>? StatusReported;

    /// <summary>Optional injectable substrate for live Run/Retry execution in tests or host adapters.</summary>
    public ICfdRunSubstrate? SubstrateOverride { get; set; }

    public int DisplayedStateCount => ElevenStatePanel.Children.Count;
    public string? SelectedRunId => selectedRunId;

    public RunWorkspaceView()
    {
        InitializeComponent();
        BuildElevenStatePills(active: null);

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
        RunButton.Click += (_, _) => HandleRunAction(isRetry: false);
        RetrySampleButton.Click += (_, _) => HandleRunAction(isRetry: true);
        CancelButton.Click += (_, _) => HandleCancelAction();
        ExplainFailureButton.Click += (_, _) => HandleExplainFailure();
        PreviewCaseDiffButton.Click += (_, _) => HandlePreviewCaseDiff();
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
            var items = summaries
                .Select(s => string.Create(CultureInfo.InvariantCulture,
                    $"{s.RunKey12} · α {s.AlphaDeg:0.#}° · V {s.SpeedMs:0.##} m/s · {s.StateDisplay}"))
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
            BuildElevenStatePills(active: null);
            RenderMeshGate(null, "Not evaluated");
            RenderResiduals(null, null);
            return;
        }

        var summary = summaries[idx];
        var stored = storedRuns[idx];
        var run = stored.Run;

        SelectedRunStateText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Run {summary.RunKey12} — {summary.StateDisplay}");
        BackendPinText.Text = summary.BackendLabel;

        BuildElevenStatePills(summary.State);

        IntegrityWarningBanner.IsVisible = summary.IntegrityWarning is not null;
        IntegrityWarningText.Text = summary.IntegrityWarning ?? "";

        ConvergedBanner.IsVisible = !string.IsNullOrWhiteSpace(summary.ConvergedLabel);
        ConvergedLabelText.Text = summary.ConvergedLabel ?? "";

        RetrySampleButton.IsEnabled = activeRunCts is null && CfdRunStateMachine.IsTerminal(summary.State);
        ExplainFailureButton.IsEnabled = true;
        PreviewCaseDiffButton.IsEnabled = true;

        RenderMeshGate(run.MeshGate, summary.MeshGateLabel);
        RenderResiduals(summary.ResidualSummary, run.Resources);
    }

    private readonly Dictionary<CfdRunState, (Border Pill, TextBlock Label)> statePills = new();

    private void BuildElevenStatePills(CfdRunState? active)
    {
        var activeBg = ResolveBrush("PrimaryBrush");
        var activeFg = ResolveBrush("OnPrimaryBrush");
        var idleBg = ResolveBrush("SurfaceSoftBrush");
        var idleFg = ResolveBrush("MutedBrush");
        var lineBrush = ResolveBrush("LineBrush");
        double fontSize = Math.Max(CanvasTypography.MinimumFontSize, CanvasTypography.ResolveFontSize(this));

        if (statePills.Count == 0)
        {
            foreach (var state in AllElevenStates)
            {
                string label = state switch
                {
                    CfdRunState.MeshGate => "Mesh gate",
                    CfdRunState.CompletedResidualNotMet => "Residual not met",
                    _ => state.ToString()
                };
                var text = new TextBlock { Text = label, FontSize = fontSize };
                var pill = new Border
                {
                    BorderBrush = lineBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Child = text
                };
                statePills[state] = (pill, text);
                ElevenStatePanel.Children.Add(pill);
            }
        }

        foreach (var state in AllElevenStates)
        {
            bool isCurrent = active == state;
            var (pill, text) = statePills[state];
            pill.Background = isCurrent ? activeBg : idleBg;
            pill.BorderBrush = lineBrush;
            text.FontSize = fontSize;
            text.FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal;
            text.Foreground = isCurrent ? activeFg : idleFg;
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

    private void HandleRunAction(bool isRetry)
    {
        if (controller is null || controller.Inspection is null)
        {
            StatusReported?.Invoke(new StatusReport("Open a foil before starting a CFD run.", ReportKind.Warning));
            return;
        }
        if (!solverReady && SubstrateOverride is null)
        {
            StatusReported?.Invoke(new StatusReport("Solver is not ready. Open Solver setup to verify an installation.", ReportKind.Warning));
            return;
        }
        if (SubstrateOverride is null)
        {
            StatusReported?.Invoke(new StatusReport(
                isRetry ? "Retry sample requires a configured solver substrate." : "CFD run requires a configured solver substrate.",
                ReportKind.Warning));
            return;
        }

        activeRunCts?.Dispose();
        activeRunCts = new CancellationTokenSource();
        RunButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusReported?.Invoke(new StatusReport(isRetry ? "Retrying CFD sample…" : "Starting CFD run…"));
    }

    private void HandleCancelAction()
    {
        if (activeRunCts is null) return;
        activeRunCts.Cancel();
        activeRunCts.Dispose();
        activeRunCts = null;
        CancelButton.IsEnabled = false;
        RunButton.IsEnabled = controller?.Inspection is not null;
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

    private IBrush ResolveBrush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var val) && val is IBrush b
            ? b
            : Brushes.Transparent;
}
