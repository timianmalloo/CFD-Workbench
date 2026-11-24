using System.Globalization;
using Avalonia.Controls;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Results;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using CfdWorkbench.Core.Results;

namespace CfdWorkbench.Desktop.Results;

/// <summary>
/// Area 6 Results, Replay, Difference Flood, Candidate Ladder &amp; ParaView handoff workspace (rail slot 6,
/// docs/design/results-and-candidates.md §2–§7).
/// Enforces tau_w-gated separation, 9-field ProvenanceStrip + linked TableTwin, Replay failed-sample pause &amp; clear,
/// DifferenceFlood "Unavailable — mesh differs", COMMIT-01 Accept as draft CAD handoff &amp; Rebase guard,
/// and process-only ParaView handoff with honest absence copy.
/// </summary>
public partial class ResultsWorkspaceView : UserControl
{
    private WorkbenchController? controller;
    private ResultsWorkspaceModel? workspace;
    private IReadOnlyList<StoredCandidate> storedCandidates = [];
    private string? selectedSampleId;
    private string? selectedCandidateId;
    private string activeVariable = "Cp";
    private bool syncing;

    public event Action<StatusReport>? StatusReported;
    public event Action<string>? RetrySampleRequested;
    public event Action? ConfigureKeyRequested;

    /// <summary>Optional injectable ParaView locator/starter seams for deterministic tests.</summary>
    public IParaViewLocator? ParaViewLocatorOverride { get; set; }
    public IParaViewProcessStarter? ParaViewStarterOverride { get; set; }
    public string? CaseRootDirectory { get; set; }

    public ReplaySeries? ActiveReplay { get; private set; }
    public DifferenceFloodResult? ActiveDifference { get; private set; }

    public ResultsWorkspaceView()
    {
        InitializeComponent();
        ReplayHoldAxisPicker.ItemsSource = new[] { "Hold speed", "Hold angle" };
        ReplayHoldAxisPicker.SelectedIndex = 0;

        SamplePicker.SelectionChanged += (_, _) =>
        {
            if (syncing || workspace is null) return;
            if (SamplePicker.SelectedIndex is >= 0 and int idx && idx < workspace.Samples.Count)
            {
                selectedSampleId = workspace.Samples[idx].SampleId;
                RenderSelectedSample();
            }
        };

        TableTwinToggle.IsCheckedChanged += (_, _) =>
        {
            TableTwinContainer.IsVisible = TableTwinToggle.IsChecked == true;
        };

        LayerSurfaceCpToggle.Click += (_, _) =>
        {
            if (LayerSurfaceCpToggle.IsChecked == true)
            {
                activeVariable = "Cp";
                LayerSurfaceCfToggle.IsChecked = false;
                RenderSelectedSample();
            }
        };
        LayerSurfaceCfToggle.Click += (_, _) =>
        {
            if (LayerSurfaceCfToggle.IsChecked == true)
            {
                activeVariable = "Cf";
                LayerSurfaceCpToggle.IsChecked = false;
                RenderSelectedSample();
            }
        };
        LayerSeparationToggle.Click += (_, _) => RenderSelectedSample();
        LayerVortexCoreToggle.Click += (_, _) => RenderSelectedSample();

        ReplayPlayButton.Click += (_, _) =>
        {
            if (ActiveReplay is null) return;
            ActiveReplay = ReplaySequence.Play(ActiveReplay);
            RenderReplayState();
        };
        ReplayHoldButton.Click += (_, _) =>
        {
            if (ActiveReplay is null) return;
            ActiveReplay = ReplaySequence.Hold(ActiveReplay);
            RenderReplayState();
        };
        ReplayStepBackButton.Click += (_, _) =>
        {
            if (ActiveReplay is null) return;
            ActiveReplay = ReplaySequence.StepBackward(ActiveReplay);
            RenderReplayState();
        };
        ReplayStepForwardButton.Click += (_, _) =>
        {
            if (ActiveReplay is null) return;
            ActiveReplay = ReplaySequence.AdvanceStep(ActiveReplay);
            RenderReplayState();
        };
        ReplayLockRangeToggle.Click += (_, _) =>
        {
            if (ActiveReplay is null) return;
            ActiveReplay = ReplaySequence.ToggleRangeLock(ActiveReplay);
            RenderReplayState();
        };
        ReplayScrubSlider.PropertyChanged += (_, args) =>
        {
            if (syncing || ActiveReplay is null || args.Property != Slider.ValueProperty) return;
            int idx = Math.Clamp((int)Math.Round(ReplayScrubSlider.Value), 0, ActiveReplay.Frames.Count - 1);
            if (idx != ActiveReplay.CurrentIndex)
            {
                ActiveReplay = ReplaySequence.ScrubTo(ActiveReplay, idx);
                RenderReplayState();
            }
        };
        ReplayRetrySampleButton.Click += (_, _) =>
        {
            if (ActiveReplay?.ActiveRetrySampleRunId is { } runId)
                RetrySampleRequested?.Invoke(runId);
        };

        ComputeDiffButton.Click += (_, _) =>
        {
            if (workspace is null) return;
            int b = DiffBasePicker.SelectedIndex;
            int t = DiffTargetPicker.SelectedIndex;
            if (b >= 0 && b < workspace.Samples.Count && t >= 0 && t < workspace.Samples.Count)
            {
                ComputeDifferenceBetween(workspace.Samples[b].SampleId, workspace.Samples[t].SampleId, activeVariable);
            }
        };

        CandidateList.SelectionChanged += (_, _) =>
        {
            if (syncing || workspace is null) return;
            int idx = CandidateList.SelectedIndex;
            if (idx >= 0 && idx < workspace.CandidateLadder.Candidates.Count)
            {
                selectedCandidateId = workspace.CandidateLadder.Candidates[idx].CandidateId;
                RenderCandidateActions();
            }
        };

        AcceptAsDraftButton.Click += (_, _) => HandleAcceptCandidateDraft(rebase: false);
        RebaseDraftButton.Click += (_, _) => HandleAcceptCandidateDraft(rebase: true);
        OpenParaViewButton.Click += (_, _) => HandleOpenParaView();
        AskResultButton.Click += async (_, _) => await HandleAskAboutResultAsync();
        AskResultConfigureKeyButton.Content = Assistant.AssistantCopy.ConfigureKeyButton;
        AskResultConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
    }

    public void RefreshAssistantStatus()
    {
        bool hasKey = controller?.Assistant.HasApiKey == true;
        bool ready = controller?.Assistant.IsReadyForProposals == true;
        AskResultConfigureKeyButton.IsVisible = !hasKey;
        if (hasKey && !ready)
        {
            AskResultStatusText.Text = Assistant.AssistantCopy.UnevaluatedOnThisModel;
            AskResultStatusText.IsVisible = true;
        }
        else if (ready)
        {
            AskResultStatusText.Text = $"BYOK ready ({controller!.Assistant.CurrentSettings.ModelId}) · tau_w separation gate active.";
            AskResultStatusText.IsVisible = true;
        }
        else
        {
            AskResultStatusText.IsVisible = false;
        }
    }

    public void Bind(WorkbenchController workbenchController)
    {
        ArgumentNullException.ThrowIfNull(workbenchController);
        controller = workbenchController;
        RefreshFromController();
    }

    public void RefreshFromController()
    {
        if (controller is null) return;
        if (controller.Inspection is null)
        {
            workspace = null;
            storedCandidates = [];
            ActiveReplay = null;
            ActiveDifference = null;
            SamplePicker.ItemsSource = Array.Empty<string>();
            DiffBasePicker.ItemsSource = Array.Empty<string>();
            DiffTargetPicker.ItemsSource = Array.Empty<string>();
            NoCrossTierNoteText.Text = ResultsCopy.EmptySamplesNote;
            ParaViewStatusText.Text = Labels.ParaViewNotFound;
            OpenParaViewButton.IsEnabled = false;
            FieldCanvas.UpdateField(null, showSeparation: false, showVortexCore: false);
            RenderProvenance(null);
            TableTwinList.ItemsSource = Array.Empty<string>();
            RenderCandidateLadder();
            return;
        }

        workspace = controller.ProjectResults(
            caseRootDirectory: CaseRootDirectory,
            paraViewLocator: ParaViewLocatorOverride);
        storedCandidates = controller.ReadCfdLedger().Candidates;

        NoCrossTierNoteText.Text = workspace.NoCrossTierAveragingNote;
        ParaViewStatusText.Text = workspace.ParaView.Message;
        OpenParaViewButton.IsEnabled = workspace.ParaView.Available;

        syncing = true;
        try
        {
            var sampleLabels = workspace.Samples
                .Select(s => string.Create(CultureInfo.InvariantCulture,
                    $"{s.Tier} · α {s.AlphaDeg:0.#}° · V {s.SpeedMs:0.##} m/s · {(s.Admitted ? "Admitted" : s.StatusLabel)}"))
                .ToArray();
            SamplePicker.ItemsSource = sampleLabels;
            DiffBasePicker.ItemsSource = sampleLabels;
            DiffTargetPicker.ItemsSource = sampleLabels;

            if (workspace.Samples.Count == 0)
            {
                selectedSampleId = null;
                SamplePicker.SelectedIndex = -1;
            }
            else
            {
                int idx = selectedSampleId is null
                    ? 0
                    : workspace.Samples.ToList().FindIndex(s => string.Equals(s.SampleId, selectedSampleId, StringComparison.Ordinal));
                if (idx < 0) idx = 0;
                selectedSampleId = workspace.Samples[idx].SampleId;
                SamplePicker.SelectedIndex = idx;
                if (DiffBasePicker.SelectedIndex < 0) DiffBasePicker.SelectedIndex = 0;
                if (DiffTargetPicker.SelectedIndex < 0) DiffTargetPicker.SelectedIndex = Math.Min(1, workspace.Samples.Count - 1);
            }
        }
        finally
        {
            syncing = false;
        }

        RenderSelectedSample();
        RenderCandidateLadder();
        RefreshAssistantStatus();
    }

    public void SelectSample(string sampleId)
    {
        selectedSampleId = sampleId;
        if (workspace is not null)
        {
            int idx = workspace.Samples.ToList().FindIndex(s => string.Equals(s.SampleId, sampleId, StringComparison.Ordinal));
            if (idx >= 0)
            {
                syncing = true;
                try { SamplePicker.SelectedIndex = idx; }
                finally { syncing = false; }
            }
        }
        RenderSelectedSample();
    }

    public void SelectCandidate(string candidateId)
    {
        selectedCandidateId = candidateId;
        if (workspace is not null)
        {
            int idx = workspace.CandidateLadder.Candidates.ToList()
                .FindIndex(c => string.Equals(c.CandidateId, candidateId, StringComparison.Ordinal));
            if (idx >= 0)
            {
                syncing = true;
                try { CandidateList.SelectedIndex = idx; }
                finally { syncing = false; }
            }
        }
        RenderCandidateActions();
    }

    /// <summary>Builds a compatible ReplaySeries centered on the named sample's mesh hash &amp; held axis.</summary>
    public void BuildReplayForSample(string anchorSampleId, bool reducedMotion = false)
    {
        if (workspace is null || controller is null) return;
        var anchor = workspace.Samples.FirstOrDefault(s => string.Equals(s.SampleId, anchorSampleId, StringComparison.Ordinal));
        if (anchor is null || string.IsNullOrWhiteSpace(anchor.MeshHash))
        {
            ActiveReplay = null;
            RenderReplayState();
            return;
        }

        var axis = ReplayHoldAxisPicker.SelectedIndex == 1 ? ReplayHeldAxis.HeldAlpha : ReplayHeldAxis.HeldSpeed;
        double heldVal = axis == ReplayHeldAxis.HeldSpeed ? anchor.SpeedMs : anchor.AlphaDeg;
        var water = controller.AnalysisWater;

        var compatible = workspace.Samples
            .Where(s =>
                string.Equals(s.MeshHash, anchor.MeshHash, StringComparison.Ordinal) &&
                string.Equals(s.SurfaceHash, anchor.SurfaceHash, StringComparison.Ordinal) &&
                string.Equals(s.Tier, anchor.Tier, StringComparison.Ordinal) &&
                string.Equals(s.MethodTierAndVersion, anchor.MethodTierAndVersion, StringComparison.Ordinal) &&
                Math.Abs((axis == ReplayHeldAxis.HeldSpeed ? s.SpeedMs : s.AlphaDeg) - heldVal) <= 1e-9)
            .ToArray();

        if (compatible.Length == 0)
        {
            ActiveReplay = null;
            RenderReplayState();
            return;
        }

        // Normalize FieldInventoryHash across slots of one compatible series so failed slots (whose manifest is empty)
        // sit in their true discrete gap position in the series.
        string seriesInventory = string.Join(",", anchor.Manifest.Layers.Where(l => l.Available).Select(l => l.Kind.ToString()));
        if (string.IsNullOrEmpty(seriesInventory)) seriesInventory = "none";

        var slots = new List<ReplaySampleSlot>(compatible.Length);
        for (int i = 0; i < compatible.Length; i++)
        {
            var s = compatible[i];
            var field = string.Equals(activeVariable, "Cf", StringComparison.Ordinal) ? s.SurfaceCfData : s.SurfaceCpData;
            IReadOnlyList<double?>? fieldValues = s.Admitted && field is not null
                ? field.Values.Select(v => (double?)v).ToArray()
                : null;
            IReadOnlyDictionary<string, double?>? metrics = s.Admitted
                ? new Dictionary<string, double?> { ["CL"] = s.Cl, ["CD"] = s.Cd, ["CM"] = s.Cm }
                : null;

            slots.Add(new ReplaySampleSlot(
                CaseIndex: i,
                RunId: s.RunId,
                SurfaceHash: s.SurfaceHash,
                MeshHash: s.MeshHash,
                MethodId: s.Tier,
                MethodVersion: s.MethodTierAndVersion,
                Water: water,
                FieldInventoryHash: seriesInventory,
                SpeedMs: s.SpeedMs,
                AlphaDeg: s.AlphaDeg,
                Admitted: s.Admitted,
                FailureReason: s.Admitted ? null : s.StatusLabel,
                RetrySampleRunId: s.Admitted ? null : s.RunId,
                FieldValues: fieldValues,
                Metrics: metrics,
                Provenance: s.Provenance));
        }

        ActiveReplay = ReplaySequence.CreateSeries(slots, axis, reducedMotion: reducedMotion);
        RenderReplayState();
    }

    public void ComputeDifferenceBetween(string baseSampleId, string targetSampleId, string variable)
    {
        if (workspace is null || controller is null) return;
        var baseSample = workspace.Samples.FirstOrDefault(s => string.Equals(s.SampleId, baseSampleId, StringComparison.Ordinal));
        var targetSample = workspace.Samples.FirstOrDefault(s => string.Equals(s.SampleId, targetSampleId, StringComparison.Ordinal));
        if (baseSample is null || targetSample is null) return;

        var diff = ResultsService.ComputeDifferenceFlood(baseSample, targetSample, variable);
        ActiveDifference = diff;

        if (!diff.Available)
        {
            DiffStatusText.Text = diff.UnavailableReason ?? DifferenceFlood.MeshDiffersReason;
            DiffRangeText.Text = "";
            FieldCanvas.ShowDifference(null, diff);
            return;
        }

        DiffStatusText.Text = string.Create(CultureInfo.InvariantCulture,
            $"{diff.Variable} computed over {diff.DeltaValues.Count} values (zero-pinned vik).");
        DiffRangeText.Text = diff.Provenance?.RangeWithBasis ?? "";

        var surfaceProj = controller.ProjectSurfaceField(baseSample, variable);
        FieldCanvas.ShowDifference(surfaceProj, diff);
        if (diff.Provenance is not null) RenderProvenance(diff.Provenance);
        if (diff.TableTwin is not null) RenderTableTwin(diff.TableTwin);
    }

    private void RenderSelectedSample()
    {
        var sample = SelectedSample();
        if (sample is null || controller is null)
        {
            FieldCanvas.UpdateField(null, showSeparation: false, showVortexCore: false);
            RenderProvenance(null);
            TableTwinList.ItemsSource = Array.Empty<string>();
            return;
        }

        ApplyManifestToLayerToggles(sample.Manifest);

        bool sepAllowed = sample.Manifest.ResolveLayer(FieldLayerKind.Separation).Available;
        bool vortexAllowed = sample.Manifest.ResolveLayer(FieldLayerKind.VortexCoreCandidates).Available;

        if (!sample.Admitted)
        {
            FieldCanvas.ClearForFailedSample(sample.StatusLabel);
            RenderProvenance(sample.Provenance);
            RenderTableTwin(sample.TableTwin);
            return;
        }

        string effectiveVar = activeVariable == "Cf" && sample.SurfaceCfData is not null ? "Cf" : "Cp";
        var surfaceProj = controller.ProjectSurfaceField(sample, effectiveVar);
        FieldCanvas.UpdateField(
            surfaceProj,
            showSeparation: sepAllowed && LayerSeparationToggle.IsChecked == true,
            showVortexCore: vortexAllowed && LayerVortexCoreToggle.IsChecked == true);
        RenderProvenance(surfaceProj.Provenance);
        RenderTableTwin(surfaceProj.TableTwin);
    }

    private void ApplyManifestToLayerToggles(FieldEvidenceManifest manifest)
    {
        void BindLayer(FieldLayerKind kind, Avalonia.Controls.Primitives.ToggleButton toggle, TextBlock basisText)
        {
            var desc = manifest.ResolveLayer(kind);
            toggle.IsEnabled = desc.Available;
            if (!desc.Available) toggle.IsChecked = false;
            basisText.Text = desc.Available
                ? (desc.MovingDashLabel is not null ? $"{desc.BasisLabel} · {desc.MovingDashLabel}" : desc.BasisLabel)
                : (desc.UnavailableReason ?? "Unavailable");
        }

        BindLayer(FieldLayerKind.SurfaceCp, LayerSurfaceCpToggle, LayerSurfaceCpBasisText);
        BindLayer(FieldLayerKind.SurfaceCf, LayerSurfaceCfToggle, LayerSurfaceCfBasisText);
        BindLayer(FieldLayerKind.SliceVelocity, LayerSliceVelocityToggle, LayerSliceVelocityBasisText);
        BindLayer(FieldLayerKind.Streamlines3D, LayerStreamlinesToggle, LayerStreamlinesBasisText);
        BindLayer(FieldLayerKind.TurbulentKineticEnergy, LayerTkeToggle, LayerTkeBasisText);
        BindLayer(FieldLayerKind.ForceVectors, LayerForceVectorsToggle, LayerForceVectorsBasisText);
        BindLayer(FieldLayerKind.SpanwiseLoading, LayerSpanwiseLoadingToggle, LayerSpanwiseLoadingBasisText);
        BindLayer(FieldLayerKind.VortexCoreCandidates, LayerVortexCoreToggle, LayerVortexCoreBasisText);
        BindLayer(FieldLayerKind.Separation, LayerSeparationToggle, LayerSeparationBasisText);
        BindLayer(FieldLayerKind.Pathlines, LayerPathlinesToggle, LayerPathlinesBasisText);
    }

    private void RenderProvenance(ProvenanceStrip? strip)
    {
        if (strip is null)
        {
            ProvVariableText.Text = "Variable: Not recorded";
            ProvUnitText.Text = "Unit: Not recorded";
            ProvRangeText.Text = "Range: Not recorded";
            ProvMethodText.Text = "Method: Not recorded";
            ProvSourceRunText.Text = "Sample: Not recorded";
            ProvDepthBasisText.Text = "Depth: Not recorded";
            ProvEvidenceLabelText.Text = "Evidence: Not recorded";
            ProvColormapText.Text = "Colormap: Not recorded";
            ProvAssociationText.Text = "Basis: Not recorded";
            return;
        }

        ProvVariableText.Text = $"Variable: {strip.Variable}";
        ProvUnitText.Text = $"Unit: {strip.Unit}";
        ProvRangeText.Text = $"Range: {strip.RangeWithBasis}";
        ProvMethodText.Text = $"Method: {strip.MethodTierAndVersion}";
        ProvSourceRunText.Text = $"Sample: {strip.SourceRunAndSample}";
        ProvDepthBasisText.Text = $"Depth: {strip.DepthBasis}";
        ProvEvidenceLabelText.Text = $"Evidence: {strip.EvidenceLabel}";
        ProvColormapText.Text = $"Colormap: {strip.ColormapNameAndVersion}";
        ProvAssociationText.Text = $"Basis: {strip.AssociationAndTimeBasis}";
    }

    private void RenderTableTwin(TableTwin twin)
    {
        TableTwinHeadingText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Linked table twin — {twin.Variable} ({twin.Unit}) · {twin.Rows.Count} rows");
        TableTwinList.ItemsSource = twin.Rows
            .Select(r => string.Create(CultureInfo.InvariantCulture,
                $"#{r.Index} · {r.Location} · {r.Variable} = {r.DisplayValue} ({r.StatusFlag})"))
            .ToArray();
    }

    private void RenderReplayState()
    {
        if (ActiveReplay is null)
        {
            ReplayBannerText.Text = ReplaySequence.SeriesBanner;
            ReplayRangeLabelText.Text = ReplaySequence.RangeLockedLabel;
            ReplayFrameStatusText.Text = "No compatible replay series built.";
            ReplayFailureBanner.IsVisible = false;
            ReplayPlayButton.IsEnabled = false;
            ReplayHoldButton.IsEnabled = false;
            ReplayStepBackButton.IsEnabled = false;
            ReplayStepForwardButton.IsEnabled = false;
            return;
        }

        var series = ActiveReplay;
        ReplayBannerText.Text = series.Banner;
        ReplayRangeLabelText.Text = series.RangeScaleLabel;

        syncing = true;
        try
        {
            ReplayLockRangeToggle.IsChecked = series.RangeLocked;
            ReplayScrubSlider.Minimum = 0;
            ReplayScrubSlider.Maximum = Math.Max(0, series.Frames.Count - 1);
            ReplayScrubSlider.Value = series.CurrentIndex;
        }
        finally
        {
            syncing = false;
        }

        ReplayPlayButton.IsEnabled = !series.ReducedMotion && !series.IsPlaying && series.Frames[series.CurrentIndex].Admitted;
        ReplayHoldButton.IsEnabled = series.IsPlaying;
        ReplayStepBackButton.IsEnabled = series.CurrentIndex > 0;
        ReplayStepForwardButton.IsEnabled = series.CurrentIndex + 1 < series.Frames.Count;

        var frame = series.Frames[series.CurrentIndex];
        ReplayFrameStatusText.Text = string.Create(CultureInfo.InvariantCulture,
            $"Frame {series.CurrentIndex + 1} of {series.Frames.Count} · α {frame.AlphaDeg:0.#}° · V {frame.SpeedMs:0.##} m/s · {(series.IsPlaying ? "Playing" : "Held")}");

        if (!frame.Admitted || series.ActiveFailureReason is not null)
        {
            // Failed-sample pause & clear invariant: clear canvas and metrics BEFORE showing banner.
            FieldCanvas.ClearForFailedSample(series.ActiveFailureReason ?? "Sample failed");
            ReplayFailureText.Text = $"Playback paused — {series.ActiveFailureReason ?? "Sample failed"}";
            ReplayFailureBanner.IsVisible = true;
            return;
        }

        ReplayFailureBanner.IsVisible = false;
        if (workspace is not null && controller is not null)
        {
            var matched = workspace.Samples.FirstOrDefault(s => string.Equals(s.RunId, frame.RunId, StringComparison.Ordinal));
            if (matched is not null)
            {
                var proj = controller.ProjectSurfaceField(matched, activeVariable == "Cf" && matched.SurfaceCfData is not null ? "Cf" : "Cp");
                FieldCanvas.UpdateField(
                    proj,
                    showSeparation: matched.Manifest.ResolveLayer(FieldLayerKind.Separation).Available && LayerSeparationToggle.IsChecked == true,
                    showVortexCore: matched.Manifest.ResolveLayer(FieldLayerKind.VortexCoreCandidates).Available && LayerVortexCoreToggle.IsChecked == true,
                    rangeMin: series.RangeLocked ? series.LockedScalarMin : null,
                    rangeMax: series.RangeLocked ? series.LockedScalarMax : null);
                RenderProvenance(proj.Provenance);
                RenderTableTwin(proj.TableTwin);
            }
        }
    }

    private void RenderCandidateLadder()
    {
        if (workspace is null)
        {
            CandidateList.ItemsSource = Array.Empty<string>();
            CandidateParetoSummaryText.Text = "";
            CandidateEmptyReasonText.IsVisible = false;
            AcceptAsDraftButton.IsEnabled = false;
            RebaseDraftButton.IsEnabled = false;
            return;
        }

        var ladder = workspace.CandidateLadder;
        CandidateParetoSummaryText.Text = string.Create(CultureInfo.InvariantCulture,
            $"{ladder.Candidates.Count} candidate(s) · {ladder.ParetoCandidateIds.Count} Pareto-optimal");

        CandidateEmptyReasonText.IsVisible = !string.IsNullOrWhiteSpace(ladder.EmptyReason);
        CandidateEmptyReasonText.Text = ladder.EmptyReason ?? "";

        syncing = true;
        try
        {
            CandidateList.ItemsSource = ladder.Candidates.Select(c =>
            {
                string tierSummary = string.Join(" | ", c.TierGroups.Select(tg =>
                    $"{tg.Tier}: " + string.Join(", ", tg.Objectives.Select(o =>
                        string.Create(CultureInfo.InvariantCulture, $"{o.Name}={o.Value:G4}")))));
                return $"{c.CandidateId[..Math.Min(8, c.CandidateId.Length)]} · {c.Status}{(c.IsParetoOptimal ? " · Pareto" : "")} · {tierSummary}";
            }).ToArray();

            if (ladder.Candidates.Count == 0)
            {
                selectedCandidateId = null;
                CandidateList.SelectedIndex = -1;
            }
            else
            {
                int idx = selectedCandidateId is null
                    ? 0
                    : ladder.Candidates.ToList().FindIndex(c => string.Equals(c.CandidateId, selectedCandidateId, StringComparison.Ordinal));
                if (idx < 0) idx = 0;
                selectedCandidateId = ladder.Candidates[idx].CandidateId;
                CandidateList.SelectedIndex = idx;
            }
        }
        finally
        {
            syncing = false;
        }

        RenderCandidateActions();
    }

    private void RenderCandidateActions()
    {
        if (workspace is null || selectedCandidateId is null)
        {
            AcceptAsDraftButton.IsEnabled = false;
            RebaseDraftButton.IsEnabled = false;
            CandidateDraftStatusText.Text = "";
            return;
        }

        var row = workspace.CandidateLadder.Candidates
            .FirstOrDefault(c => string.Equals(c.CandidateId, selectedCandidateId, StringComparison.Ordinal));
        if (row is null)
        {
            AcceptAsDraftButton.IsEnabled = false;
            RebaseDraftButton.IsEnabled = false;
            return;
        }

        AcceptAsDraftButton.IsEnabled = row.AcceptEnabled;
        RebaseDraftButton.IsEnabled = !row.AcceptEnabled;
        CandidateDraftStatusText.Text = row.AcceptEnabled
            ? "Ready to open as a previewed CAD draft (accepted geometry stays unchanged until Apply)."
            : (row.AcceptDisabledReason ?? "");
    }

    private void HandleAcceptCandidateDraft(bool rebase)
    {
        if (controller is null || workspace is null || selectedCandidateId is null) return;
        var stored = storedCandidates.FirstOrDefault(s =>
            s.Integrity == CfdRecordIntegrity.Intact &&
            string.Equals(s.Candidate.CandidateId, selectedCandidateId, StringComparison.Ordinal));
        if (stored is null) return;

        CandidateGeometryDraft draft = rebase
            ? CandidateLadder.BuildRebasedDraft(
                stored.Candidate,
                workspace.CurrentAcceptedId,
                workspace.CurrentSurfaceHash,
                measuredDeviationMeters: 0.0)
            : CandidateLadder.BuildGeometryDraft(
                stored.Candidate,
                workspace.CurrentAcceptedId,
                workspace.CurrentSurfaceHash);

        if (!draft.AcceptEnabled)
        {
            CandidateDraftStatusText.Text = draft.RefusalReason ?? "Accept disabled — base revision moved.";
            StatusReported?.Invoke(new StatusReport(CandidateDraftStatusText.Text, ReportKind.Warning));
            return;
        }

        controller.AcceptCandidateAsDraft(draft);
        CandidateDraftStatusText.Text = draft.IsRebased
            ? $"Rebased candidate {draft.CandidateId[..Math.Min(8, draft.CandidateId.Length)]} opened as CAD draft."
            : $"Candidate {draft.CandidateId[..Math.Min(8, draft.CandidateId.Length)]} opened as CAD draft.";
        StatusReported?.Invoke(new StatusReport(CandidateDraftStatusText.Text));
    }

    private void HandleOpenParaView()
    {
        if (string.IsNullOrWhiteSpace(CaseRootDirectory))
        {
            var avail = ParaViewHandoff.Prepare(ParaViewLocatorOverride);
            ParaViewStatusText.Text = avail.Message;
            StatusReported?.Invoke(new StatusReport(avail.Message, ReportKind.Warning));
            return;
        }

        try
        {
            var result = ParaViewHandoff.Launch(CaseRootDirectory, ParaViewLocatorOverride, starter: ParaViewStarterOverride);
            ParaViewStatusText.Text = result.Message;
            StatusReported?.Invoke(new StatusReport(result.Message, result.Launched ? ReportKind.Info : ReportKind.Warning));
        }
        catch (ContractError err)
        {
            ParaViewStatusText.Text = err.Reason ?? err.Code;
            StatusReported?.Invoke(new StatusReport(ParaViewStatusText.Text, ReportKind.Warning));
        }
    }

    private async Task HandleAskAboutResultAsync()
    {
        var sample = SelectedSample();
        if (sample is null) return;
        string question = AskResultInput.Text ?? "";

        if (controller is not null && controller.Assistant.IsReadyForProposals && TopLevel.GetTopLevel(this) is Window owner)
        {
            bool sepAvailable = sample.Manifest.ResolveLayer(FieldLayerKind.Separation).Available;
            var preview = await controller.Assistant.PreparePreviewAsync(
                new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
                    CfdWorkbench.Core.Assistant.AssistantArea.Results,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.ResultsExplanation,
                    "Explain the selected CFD result sample using only supported field evidence. Never diagnose separation without wall tau_w.",
                    question));
            if (preview.Allowed && preview.Prepared is not null)
            {
                var valInput = new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                    CfdWorkbench.Core.Assistant.AssistantArea.Results,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.ResultsExplanation,
                    UserPrompt: question,
                    SampleRunId: sample.RunId,
                    SeparationLayerAvailable: sepAvailable,
                    AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { sample.RunId },
                    SharedContextText: $"{sample.RunId} {sample.Tier} alpha={sample.AlphaDeg} speed={sample.SpeedMs}");
                var dlg = new Assistant.AssistantConsentPreviewDialog(controller.Assistant, preview.Prepared, valInput)
                {
                    RequestedThemeVariant = owner.ActualThemeVariant
                };
                await dlg.ShowDialog(owner);
                if (dlg.ExecutionResult?.Validation is { } val)
                {
                    AskResultAnswerText.Text = val.DisplayText;
                    AskResultAnswerText.IsVisible = true;
                }
                return;
            }
        }

        var answer = ResultsService.AskAboutResult(sample, question);
        AskResultAnswerText.Text = answer.Answer;
        AskResultAnswerText.IsVisible = true;
    }

    private UnifiedResultSample? SelectedSample()
    {
        if (workspace is null || selectedSampleId is null) return null;
        return workspace.Samples.FirstOrDefault(s => string.Equals(s.SampleId, selectedSampleId, StringComparison.Ordinal));
    }
}
