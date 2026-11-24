using System.Globalization;
using Avalonia.Controls;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Sweeps;

public partial class ExperimentSetupView : UserControl
{
    private SweepController? controller;
    private Assistant.AssistantRuntimeHub? assistantHub;
    private bool syncing;

    public event Action? ConfigureKeyRequested;

    public ExperimentSetupView()
    {
        InitializeComponent();
        EmptyStateText.Text = Labels.SweepEmpty;
        FooterNoteText.Text = Labels.SweepSetupFooterNote;
        GoalSourceNote.Text = Labels.SweepNoGoalState;

        ExperimentAssistantBar.Configure(
            Assistant.AssistantCopy.ExperimentEntryTitle,
            "Draft sweep",
            "Describe speeds, angles, water, and depth…",
            "Draft experiment configuration");
        ExperimentAssistantBar.ConfigureKeyRequested += () => ConfigureKeyRequested?.Invoke();
        ExperimentAssistantBar.PromptSubmitted += prompt => _ = HandleExperimentAssistantSubmitAsync(prompt);

        NewSweepButton.Click += (_, _) => controller?.NewSweep();
        EmptyNewSweepButton.Click += (_, _) => controller?.NewSweep();
        NewVersionButton.Click += (_, _) => controller?.NewVersionFromSelected();
        RefreshPreviewButton.Click += (_, _) => { PushInputs(); controller?.RefreshPreview(); };
        QueueButton.Click += (_, _) => { PushInputs(); controller?.Queue(); };
        OpenInRunButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.LocalRun);

        SweepNameBox.TextChanged += (_, _) => OnInputEdited();
        SpeedsBox.TextChanged += (_, _) => OnInputEdited();
        AnglesBox.TextChanged += (_, _) => OnInputEdited();
        WaterTempBox.TextChanged += (_, _) => OnInputEdited();
        DepthBox.TextChanged += (_, _) => OnInputEdited();
        SpeedUnitBox.SelectionChanged += (_, _) => OnInputEdited();
        SaltWaterCheck.IsCheckedChanged += (_, _) => OnInputEdited();
        DepthSetCheck.IsCheckedChanged += (_, _) => { DepthBox.IsEnabled = DepthSetCheck.IsChecked == true; OnInputEdited(); };
        SourceCartesianRadio.IsCheckedChanged += (_, _) => OnInputEdited();
        SourceGoalRadio.IsCheckedChanged += (_, _) => OnInputEdited();
        TierVlmRadio.IsCheckedChanged += (_, _) => OnInputEdited();
        TierEstimatorRadio.IsCheckedChanged += (_, _) => OnInputEdited();
    }

    public void Bind(SweepController? sweepController)
    {
        controller = sweepController;
        Refresh();
    }

    public void RefreshAssistantStatus(Assistant.AssistantRuntimeHub? hub)
    {
        assistantHub = hub ?? assistantHub;
        ExperimentAssistantBar.RefreshStatus(assistantHub);
    }

    /// <summary>
    /// Populates the Experiment sweep draft from a deterministically checked
    /// <see cref="CfdWorkbench.Core.Assistant.ExperimentConfigProposal"/> (AI-09). Strictly preview-only:
    /// never sets Queued or triggers Run (XS-03 / UX-17).
    /// </summary>
    public void ApplyExperimentConfigProposal(CfdWorkbench.Core.Assistant.ExperimentConfigProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (controller is null) return;

        if (!controller.HasDraft) controller.NewSweep();
        if (proposal.Speeds is { Count: > 0 })
            controller.SpeedsText = string.Join(", ", proposal.Speeds.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));
        if (proposal.SpeedUnit is not null)
            controller.SpeedUnit = string.Equals(proposal.SpeedUnit, "m/s", StringComparison.OrdinalIgnoreCase)
                ? SweepSpeedUnit.MetersPerSecond
                : SweepSpeedUnit.Knots;
        if (proposal.AnglesDeg is { Count: > 0 })
            controller.AnglesText = string.Join(", ", proposal.AnglesDeg.Select(a => a.ToString("0.###", CultureInfo.InvariantCulture)));
        if (proposal.WaterTemperatureC is { } temp)
            controller.WaterTempText = temp.ToString("0.##", CultureInfo.InvariantCulture);
        if (proposal.IsSaltWater is { } salt)
            controller.SaltWater = salt;
        if (proposal.HRefMeters is { } href)
        {
            controller.DepthSet = true;
            controller.DepthText = href.ToString("0.###", CultureInfo.InvariantCulture);
        }
        if (string.Equals(proposal.Tier, SweepRecords.TierEstimator, StringComparison.OrdinalIgnoreCase))
            controller.Tier = SweepRecords.TierEstimator;
        else if (string.Equals(proposal.Tier, SweepRecords.TierVlmStrip, StringComparison.OrdinalIgnoreCase))
            controller.Tier = SweepRecords.TierVlmStrip;

        controller.RefreshPreview();
        Refresh();
    }

    private async Task HandleExperimentAssistantSubmitAsync(string prompt)
    {
        if (assistantHub is null || controller is null) return;
        var preview = await assistantHub.PreparePreviewAsync(
            new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
                CfdWorkbench.Core.Assistant.AssistantArea.Experiment,
                CfdWorkbench.Core.Assistant.AssistantProposalKind.ExperimentConfig,
                "Return strict experiment-config JSON (sweep or multipoint optimize). Preview only; never queue or execute.",
                prompt));
        if (!preview.Allowed || preview.Prepared is null)
        {
            ExperimentAssistantBar.ShowFeedback(preview.Message ?? Assistant.AssistantCopy.NoKeyNotice);
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dlg = new Assistant.AssistantConsentPreviewDialog(
                assistantHub,
                preview.Prepared,
                new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                    CfdWorkbench.Core.Assistant.AssistantArea.Experiment,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.ExperimentConfig))
            {
                RequestedThemeVariant = owner.ActualThemeVariant
            };
            await dlg.ShowDialog(owner);
            if (dlg.Accepted && dlg.ExecutionResult?.Validation?.ExperimentConfig is { } cfg)
            {
                ApplyExperimentConfigProposal(cfg);
                ExperimentAssistantBar.ShowFeedback("Draft preview updated. Review and Queue when ready.");
            }
        }
    }

    public void Refresh()
    {
        if (controller is null) return;
        syncing = true;
        try
        {
            EmptyStatePanel.IsVisible = !controller.HasDraft;
            EditorGrid.IsVisible = controller.HasDraft;
            NewVersionButton.IsEnabled = controller.SelectedExperiment is not null;

            bool hasGoal = controller.GoalSnapshot is not null;
            SourceGoalRadio.IsEnabled = hasGoal;
            GoalSourceNote.IsVisible = !hasGoal;

            if (SweepNameBox.Text != controller.SweepName) SweepNameBox.Text = controller.SweepName;
            if (SpeedsBox.Text != controller.SpeedsText) SpeedsBox.Text = controller.SpeedsText;
            if (AnglesBox.Text != controller.AnglesText) AnglesBox.Text = controller.AnglesText;
            if (WaterTempBox.Text != controller.WaterTempText) WaterTempBox.Text = controller.WaterTempText;
            if (DepthBox.Text != controller.DepthText) DepthBox.Text = controller.DepthText;
            SpeedUnitBox.SelectedIndex = controller.SpeedUnit == SweepSpeedUnit.Knots ? 0 : 1;
            SaltWaterCheck.IsChecked = controller.SaltWater;
            DepthSetCheck.IsChecked = controller.DepthSet;
            DepthBox.IsEnabled = controller.DepthSet;
            SourceCartesianRadio.IsChecked = controller.SourceKind == SweepRecords.SourceCartesian;
            SourceGoalRadio.IsChecked = controller.SourceKind == SweepRecords.SourceGoalState;
            TierVlmRadio.IsChecked = controller.Tier == SweepRecords.TierVlmStrip;
            TierEstimatorRadio.IsChecked = controller.Tier == SweepRecords.TierEstimator;

            StaleBanner.IsVisible = controller.StalePreviewBanner is not null;
            StaleBannerText.Text = controller.StalePreviewBanner ?? "";
            ValidationBanner.IsVisible = controller.ValidationError is not null;
            ValidationBannerText.Text = controller.ValidationError ?? "";

            QueueButton.IsEnabled = controller.CanQueue;
            bool hasQueued = controller.SelectedExperiment?.Experiment.IsQueued == true;
            OpenInRunButton.IsEnabled = hasQueued;
            QueuedStatusText.Text = controller.SelectedExperiment is { } sel
                ? $"Selected: {sel.Experiment.Name} · {SweepProjection.StatusOf(sel.Experiment, controller.ActiveRunningCaseIndex).BadgeText}"
                : "No queued sweep yet";

            RenderPreview(controller.Preview);
        }
        finally { syncing = false; }
    }

    private void OnInputEdited()
    {
        if (syncing || controller is null || !controller.HasDraft) return;
        PushInputs();
        controller.RefreshPreview();
    }

    private void PushInputs()
    {
        if (controller is null) return;
        controller.SweepName = SweepNameBox.Text ?? "";
        controller.SpeedsText = SpeedsBox.Text ?? "";
        controller.AnglesText = AnglesBox.Text ?? "";
        controller.WaterTempText = WaterTempBox.Text ?? "";
        controller.DepthText = DepthBox.Text ?? "";
        controller.SpeedUnit = SpeedUnitBox.SelectedIndex == 0 ? SweepSpeedUnit.Knots : SweepSpeedUnit.MetersPerSecond;
        controller.SaltWater = SaltWaterCheck.IsChecked == true;
        controller.DepthSet = DepthSetCheck.IsChecked == true;
        controller.SourceKind = SourceGoalRadio.IsChecked == true ? SweepRecords.SourceGoalState : SweepRecords.SourceCartesian;
        controller.Tier = TierEstimatorRadio.IsChecked == true ? SweepRecords.TierEstimator : SweepRecords.TierVlmStrip;
    }

    private void RenderPreview(SweepPreview? preview)
    {
        PreviewRowsHost.Children.Clear();
        if (preview is null)
        {
            PreviewCountText.Text = "Preview blocked — fix highlighted inputs above.";
            EstimateWallText.Text = Labels.SweepEstimatedWallNotRecorded;
            EstimateStorageText.Text = "";
            BasisPreviewText.Text = "";
            return;
        }

        PreviewCountText.Text = $"{preview.Cases.Count} cases · Panels/run: {(preview.PanelsPerRun == 0 ? "0 (closed form)" : preview.PanelsPerRun.ToString(CultureInfo.InvariantCulture))} · {preview.CacheHits} of {preview.Cases.Count} already computed — reused by run key";
        EstimateWallText.Text = preview.EstimatedWallMs is double estMs
            ? string.Create(CultureInfo.InvariantCulture, $"Estimated wall time: ≈ {estMs / 1000.0:0.0} s (from {preview.MeasuredRunSamples} local runs, Inferred)")
            : Labels.SweepEstimatedWallNotRecorded;
        EstimateStorageText.Text = Labels.SweepEstimatedStorage(FormatBytes(preview.EstimatedStorageBytes), FormatBytes(preview.DocumentBytes));
        BasisPreviewText.Text = $"Basis: {preview.BasisLabel} · {preview.NotModelledLabel}";

        PreviewRowsHost.Children.Add(new TextBlock
        {
            Text = "#   | Speed (m/s) | α (°) / Load | q (Pa)   | Re_ref     | h/c   | Fr_h  | σ     | Cache",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 11
        });
        foreach (var row in preview.Rows)
        {
            string alphaOrLoad = row.AlphaDeg is double a
                ? string.Create(CultureInfo.InvariantCulture, $"{a:0.##}°")
                : string.Create(CultureInfo.InvariantCulture, $"Solve ({row.TargetLoadN:0.#} N)");
            string hc = row.Conditions.DepthOverChord.Value is double vHc ? vHc.ToString("0.00", CultureInfo.InvariantCulture) : "Unavail (depth unset)";
            string fr = row.Conditions.FroudeDepth.Value is double vFr ? vFr.ToString("0.00", CultureInfo.InvariantCulture) : "—";
            string sig = row.Conditions.Sigma.Value is double vSig ? vSig.ToString("0.00", CultureInfo.InvariantCulture) : "—";
            string line = string.Create(CultureInfo.InvariantCulture,
                $"{row.CaseIndex + 1,2}  | {row.SpeedMetersPerSecond,11:0.###} | {alphaOrLoad,12} | {row.Conditions.Q.Value ?? 0,8:0.0} | {row.Conditions.ReRef.Value ?? 0,10:0.00E+0} | {hc,5} | {fr,5} | {sig,5} | {(row.CacheHit ? "Hit" : "New")}");
            PreviewRowsHost.Children.Add(new TextBlock { Text = line, FontSize = 11 });
        }
    }

    private static string FormatBytes(int bytes) =>
        bytes >= 1_000_000
            ? string.Create(CultureInfo.InvariantCulture, $"{bytes / 1_000_000.0:0.00} MB")
            : string.Create(CultureInfo.InvariantCulture, $"{bytes / 1_000.0:0.0} kB");
}
