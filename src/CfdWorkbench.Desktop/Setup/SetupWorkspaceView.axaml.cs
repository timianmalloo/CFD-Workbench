using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Setup;

/// <summary>
/// Code-behind for <see cref="SetupWorkspaceView"/>. Renders all six Area 1 hard states cleanly across
/// Light, Dark, and High Contrast themes with strict event-subscriber lifetime hygiene.
/// </summary>
public partial class SetupWorkspaceView : UserControl
{
    private sealed record PickerOption(string Id, string Label)
    {
        public override string ToString() => Label;
    }

    private static readonly PickerOption[] PurposeOptions =
    [
        new("wingfoil-race", "Wingfoil race"),
        new("wingfoil-freeride", "Wingfoil freeride"),
        new("surf", "Surf foil"),
        new("wakefoil", "Wakefoil (uses Surf preset · Flagged)"),
        new("pump", "Pump / dock-start"),
        new("downwind", "Downwind SUP"),
        new("parawing", "Parawing"),
        new("windfoil-race", "Windfoil race (iQFOiL)"),
        new("windfoil-freeride", "Windfoil freeride (open class)"),
        new("kitefoil", "Kitefoil race"),
        new("efoil", "E-foil")
    ];

    private static readonly PickerOption[] ClassRuleOptions =
        ClassRules.Presets.Select(p => new PickerOption(p.Id, p.Name)).ToArray();

    private static readonly PickerOption[] WaterOptions =
    [
        new("salt", "Salt"),
        new("fresh", "Fresh")
    ];

    private SetupController? bound;
    private Assistant.AssistantRuntimeHub? assistantHub;
    private Action? onOpenInCad;
    private bool syncing;

    public event Action? ConfigureKeyRequested;

    public SetupWorkspaceView()
    {
        InitializeComponent();

        PurposePicker.ItemsSource = PurposeOptions;
        ClassRulePicker.ItemsSource = ClassRuleOptions;
        WaterKindPicker.ItemsSource = WaterOptions;

        SubTabBriefButton.Click += (_, _) => bound?.SetSubTab("brief");
        SubTabGoalButton.Click += (_, _) => bound?.SetSubTab("goal");
        SubTabChecksButton.Click += (_, _) => bound?.SetSubTab("checks");
        OpenInCadButton.Click += (_, _) => onOpenInCad?.Invoke();

        PurposePicker.SelectionChanged += (_, _) =>
        {
            if (syncing || bound is null) return;
            if (PurposePicker.SelectedItem is PickerOption opt) bound.SelectPurpose(opt.Id);
        };
        ClassRulePicker.SelectionChanged += (_, _) =>
        {
            if (syncing || bound is null) return;
            if (ClassRulePicker.SelectedItem is PickerOption opt) bound.SelectClassRule(opt.Id);
        };
        WaterKindPicker.SelectionChanged += (_, _) =>
        {
            if (syncing || bound is null) return;
            if (WaterKindPicker.SelectedItem is PickerOption opt)
            {
                bound.IsSaltWater = opt.Id == "salt";
                bound.RecomputePreview();
            }
        };

        WireNumeric(MaxSpanBox, v => bound!.MaxSpanMeters = v);
        WireNumeric(WeightSpanBox, v => bound!.WeightSpan = v);
        WireNumeric(MaxChordBox, v => bound!.MaxChordMm = v);
        WireNumeric(WeightChordBox, v => bound!.WeightChord = v);
        WireNumeric(TargetAreaBox, v => bound!.TargetAreaCm2 = v);
        WireNumeric(WeightAreaBox, v => bound!.WeightArea = v);
        WireNumeric(TargetArBox, v => bound!.TargetAspectRatio = v);
        WireNumeric(WeightArBox, v => bound!.WeightAspectRatio = v);
        WireNumeric(TaperBox, v => bound!.Taper = v);
        WireNumeric(ThicknessBox, v => bound!.RootThicknessPercent = v);
        WireNumeric(RiderMassBox, v => bound!.RiderMassKg = v);
        WireNumeric(MastLengthBox, v => bound!.MastLengthCm = v);
        WireNumeric(WaterTempBox, v => bound!.WaterTemperatureC = v);
        WireNumeric(HRefBox, v => bound!.HRefMeters = v);

        FlaggedAckCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (syncing || bound is null) return;
            bound.SetFlaggedAcknowledged(FlaggedAckCheckBox.IsChecked == true);
        };

        SeedButton.Click += (_, _) => bound?.SeedDesign();
        ResetPresetButton.Click += (_, _) => { if (bound is not null) bound.SelectPurpose(bound.Purpose); };
        LanguageConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        LanguageSubmitButton.Click += async (_, _) => await HandleLanguageSubmitAsync();

        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    public void Bind(SetupController? controller, Action? openInCad = null, Assistant.AssistantRuntimeHub? hub = null)
    {
        onOpenInCad = openInCad ?? onOpenInCad;
        assistantHub = hub ?? assistantHub;
        if (ReferenceEquals(bound, controller))
        {
            Refresh();
            return;
        }
        if (bound is not null) bound.Changed -= OnControllerChanged;
        bound = controller;
        if (bound is not null) bound.Changed += OnControllerChanged;
        Refresh();
    }

    private async Task HandleLanguageSubmitAsync()
    {
        if (bound is null || assistantHub is null) return;
        string prompt = (LanguagePromptBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return;

        var preview = await assistantHub.PreparePreviewAsync(new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
            CfdWorkbench.Core.Assistant.AssistantArea.Setup,
            CfdWorkbench.Core.Assistant.AssistantProposalKind.SetupSeed,
            "Return strict setup-seed JSON with Stated/Inferred/Defaulted provenance.",
            prompt));
        if (!preview.Allowed || preview.Prepared is null)
        {
            LanguageStatusText.Text = preview.Message ?? Assistant.AssistantCopy.NoKeyNotice;
            LanguageStatusText.IsVisible = true;
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dlg = new Assistant.AssistantConsentPreviewDialog(
                assistantHub,
                preview.Prepared,
                new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                    CfdWorkbench.Core.Assistant.AssistantArea.Setup,
                    CfdWorkbench.Core.Assistant.AssistantProposalKind.SetupSeed))
            {
                RequestedThemeVariant = owner.ActualThemeVariant
            };
            await dlg.ShowDialog(owner);
            if (dlg.Accepted && dlg.ExecutionResult?.Validation?.SetupSeed is { } seed)
            {
                bound.ApplySetupSeedProposal(seed);
                LanguageStatusText.Text = bound.LastReceipt ?? "";
                LanguageStatusText.IsVisible = !string.IsNullOrWhiteSpace(LanguageStatusText.Text);
            }
        }
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (bound is not null) bound.Changed -= OnControllerChanged;
        bound = null;
    }

    private void OnControllerChanged()
    {
        if (Dispatcher.UIThread.CheckAccess()) Refresh();
        else Dispatcher.UIThread.Post(Refresh);
    }

    private void WireNumeric(TextBox box, Action<double> assign)
    {
        box.LostFocus += (_, _) =>
        {
            if (syncing || bound is null) return;
            if (double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                assign(val);
                bound.RecomputePreview();
            }
            else
            {
                assign(double.NaN);
                bound.RecomputePreview();
            }
        };
    }

    private void Refresh()
    {
        if (bound is null) return;
        syncing = true;
        try
        {
            // Header chips & sub-tabs
            RecipeStatusChip.Text = bound.StatusStripText;
            VersionChip.Text = $"Brief v{bound.BriefHistory.Count} · Goal v{bound.CurrentGoalVersion}";
            SubTabBriefButton.IsChecked = bound.ActiveSubTab == "brief";
            SubTabGoalButton.IsChecked = bound.ActiveSubTab == "goal";
            SubTabChecksButton.IsChecked = bound.ActiveSubTab == "checks";
            BriefPanel.IsVisible = bound.ActiveSubTab == "brief";
            GoalPanel.IsVisible = bound.ActiveSubTab == "goal";
            ChecksPanel.IsVisible = bound.ActiveSubTab == "checks";

            // AI-02 language entry slot (COPY-79 / COPY-53, AI-01: never disables parameter form)
            LanguageCardTitle.Text = SetupController.Copy79LanguageTitle;
            LanguageNoKeyText.IsVisible = !bound.HasApiKey;
            LanguageNoKeyText.Text = SetupController.Copy53NoApiKey;
            LanguageConfigureKeyButton.IsVisible = !bound.HasApiKey || (assistantHub is not null && !assistantHub.Eligibility.ProposalsEnabled);
            bool readyForBrief = bound.HasApiKey && (assistantHub is null || assistantHub.Eligibility.ProposalsEnabled);
            LanguagePromptBox.IsEnabled = readyForBrief;
            LanguageSubmitButton.IsEnabled = readyForBrief;
            if (bound.HasApiKey && assistantHub is not null && !assistantHub.Eligibility.ProposalsEnabled)
            {
                LanguageStatusText.Text = Assistant.AssistantCopy.UnevaluatedOnThisModel;
                LanguageStatusText.IsVisible = true;
            }
            else if (bound.LastProposalProvenance.Count > 0)
            {
                LanguageStatusText.Text = string.Join(" · ", bound.LastProposalProvenance.Take(3).Select(p => $"[{p.Tag}] {p.FieldName}={p.ValueDisplay}"));
                LanguageStatusText.IsVisible = true;
            }
            else
            {
                LanguageStatusText.IsVisible = false;
            }

            // Pickers
            PurposePicker.SelectedItem = PurposeOptions.FirstOrDefault(o => o.Id == bound.Purpose) ?? PurposeOptions[0];
            ClassRulePicker.SelectedItem = ClassRuleOptions.FirstOrDefault(o => o.Id == bound.ClassRulePresetId) ?? ClassRuleOptions[0];
            WaterKindPicker.SelectedItem = WaterOptions[bound.IsSaltWater ? 0 : 1];

            // Numeric boxes
            SetBox(MaxSpanBox, bound.MaxSpanMeters, "0.###");
            SetBox(WeightSpanBox, bound.WeightSpan, "0.##");
            SetBox(MaxChordBox, bound.MaxChordMm, "0.#");
            SetBox(WeightChordBox, bound.WeightChord, "0.##");
            SetBox(TargetAreaBox, bound.TargetAreaCm2, "0.#");
            SetBox(WeightAreaBox, bound.WeightArea, "0.##");
            SetBox(TargetArBox, bound.TargetAspectRatio, "0.##");
            SetBox(WeightArBox, bound.WeightAspectRatio, "0.##");
            SetBox(TaperBox, bound.Taper, "0.##");
            SetBox(ThicknessBox, bound.RootThicknessPercent, "0.#");
            SetBox(RiderMassBox, bound.RiderMassKg, "0.#");
            SetBox(MastLengthBox, bound.MastLengthCm, "0.#");
            SetBox(WaterTempBox, bound.WaterTemperatureC, "0.#");
            SetBox(HRefBox, bound.HRefMeters, "0.##");

            // Banners
            WakefoilBanner.IsVisible = bound.WakefoilNoticeText is not null;
            WakefoilBannerText.Text = bound.WakefoilNoticeText ?? "";

            SectionFallbackBanner.IsVisible = bound.SectionFallbackText is not null;
            SectionFallbackText.Text = bound.SectionFallbackText ?? "";

            ValidationBanner.IsVisible = bound.ValidationError is not null;
            ValidationBannerText.Text = bound.ValidationError ?? "";

            // Live preview & estimates
            PlanformPreview.Update(bound.PreviewSolve?.Recipe, bound.IsReseed ? bound.PriorEstimates : null);
            if (bound.PreviewEstimates is { } est)
            {
                EstimatesReadout.Text = string.Create(CultureInfo.InvariantCulture,
                    $"≈ Span {est.SpanMeters * 1000:0.#} mm · Area {est.AreaSquareMeters * 1e4:0.#} cm² · AR (projected, b²/S) {est.AspectRatio:0.00} · Root {est.RootChordMeters * 1000:0.#} mm · Tip {est.TipChordMeters * 1000:0.#} mm · MAC {est.MacMeters * 1000:0.#} mm");
            }
            else
            {
                EstimatesReadout.Text = "Unavailable — enter valid soft targets to preview geometry.";
            }

            // Deviation badges
            DeviationsList.Children.Clear();
            if (bound.PreviewSolve is { } solve)
            {
                foreach (var d in solve.Recipe.Deviations)
                {
                    if (Math.Abs(d.RelativeDelta) < 1e-4) continue;
                    string label = Labels.SetupTargetDeviation(
                        d.Target,
                        FormatTarget(d.Target, d.TargetValue),
                        FormatTarget(d.Target, d.SeededValue),
                        d.RelativeDelta);
                    DeviationsList.Children.Add(new TextBlock
                    {
                        Text = label,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = ResolveBrush("WarningBrush")
                    });
                }
            }

            // Conflict / tip clamp / re-seed banners
            ConflictBanner.IsVisible = bound.PreviewSolve?.Recipe.ConflictNotice is not null;
            ConflictBannerText.Text = bound.PreviewSolve?.Recipe.ConflictNotice ?? "";

            TipClampBanner.IsVisible = bound.TipClampNoticeText is not null;
            TipClampBannerText.Text = bound.TipClampNoticeText ?? "";

            ReseedBanner.IsVisible = bound.ReseedNoticeText is not null;
            ReseedBannerText.Text = bound.ReseedNoticeText ?? "";

            // Flagged acknowledgement gate
            FlaggedGateCard.IsVisible = bound.RequiredFlagged.Count > 0;
            FlaggedItemsList.Children.Clear();
            foreach (var item in bound.RequiredFlagged)
            {
                FlaggedItemsList.Children.Add(new TextBlock
                {
                    Text = $"[Flagged] {item.Source}",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = ResolveBrush("InkBrush")
                });
            }
            FlaggedAckCheckBox.Content = Labels.SetupAcknowledgeFlagged(bound.RequiredFlagged.Count);
            FlaggedAckCheckBox.IsChecked = bound.FlaggedAcknowledged;

            // DRC summary card
            var allFindings = bound.ClassFindings.Concat(bound.SanityFindings).ToArray();
            int warnings = bound.ClassFindings.Count(f => !f.Excluded && f.Severity is DrcSeverity.Error or DrcSeverity.Warning);
            int advisories = bound.SanityFindings.Count(f => !f.Excluded) + bound.ClassFindings.Count(f => !f.Excluded && f.Severity == DrcSeverity.Advisory);
            DrcSummaryHeading.Text = $"Checks & class rules · {warnings} warning(s) · {advisories} advisory";
            DrcSummaryText.Text = allFindings.Length == 0
                ? "0 findings."
                : string.Join("\n", allFindings.Take(4).Select(f => $"[{f.Severity}] {f.RuleId} ({f.RuleVersion}, {f.RuleDate}): {f.Message}{(f.Excluded ? " [Excluded]" : "")}"));

            // Seed action & receipt
            SeedButton.Content = bound.SeedButtonLabel;
            SeedButton.IsEnabled = bound.CanSeed;
            AutomationProperties.SetName(SeedButton, bound.SeedButtonLabel);

            ReceiptText.IsVisible = bound.LastReceipt is not null;
            ReceiptText.Text = bound.LastReceipt ?? "";

            // Populate Goal state & Feasibility sub-tab
            RebuildGoalPanel();

            // Populate Checks & DRC sub-tab
            RebuildChecksPanel(allFindings);
        }
        finally
        {
            syncing = false;
        }
    }

    private void RebuildGoalPanel()
    {
        OperatingPointsList.Children.Clear();
        ConstraintsList.Children.Clear();
        if (bound?.PreviewFeasibility is not { } report || bound.PreviewGoalState is not { } goal)
        {
            GoalSummaryText.Text = "Unavailable — enter valid targets first.";
            return;
        }

        GoalSummaryText.Text = string.Create(CultureInfo.InvariantCulture,
            $"{report.TierChip} · System mass ≈ {goal.SystemMassKg:0.#} kg · Front-wing load {goal.FrontWingLoadN:0.#} N · Wing loading ≈ {report.WingLoadingKPa:0.0} kPa");

        foreach (var pt in report.Points)
        {
            double kn = pt.Point.SpeedMetersPerSecond / DisciplinePresets.KnotsToMps;
            string clText = pt.ClRequired is { } cl ? cl.ToString("0.000", CultureInfo.InvariantCulture) : "—";
            string sigText = pt.Derived.Sigma.Value is { } s ? s.ToString("0.00", CultureInfo.InvariantCulture) : "Unavailable";
            string hcText = pt.Derived.DepthOverChord.Value is { } hc ? hc.ToString("0.00", CultureInfo.InvariantCulture) : "Unavailable";
            string cavText = pt.Cells.FirstOrDefault(c => c.ConstraintKind == "cavitation-margin")?.DisplayText ?? Labels.SetupPolarUnavailable;
            string shallowTag = pt.ShallowWaterNotice ? " · h/c < 5 (free surface not modelled)" : "";

            OperatingPointsList.Children.Add(new TextBlock
            {
                Text = string.Create(CultureInfo.InvariantCulture,
                    $"{pt.Point.Label} ({kn:0.#} kn, h_ref {pt.Point.HRefMeters:0.##} m): CL_req {clText} · σ {sigText} · h/c {hcText}{shallowTag} · Cavitation: {cavText}"),
                TextWrapping = TextWrapping.Wrap,
                Foreground = ResolveBrush("InkBrush")
            });
        }

        foreach (var row in goal.Constraints)
        {
            string editedTag = row.EditedByUser ? $" [{Labels.SetupEditedByUser}]" : "";
            string boundStr = row.Bound is { } b
                ? string.Create(CultureInfo.InvariantCulture, $" {row.Operator} {b:0.##} {row.Unit}")
                : row.BoundMin is { } bMin && row.BoundMax is { } bMax
                    ? string.Create(CultureInfo.InvariantCulture, $" {bMin:0.0e0}–{bMax:0.0e0} {row.Unit}")
                    : "";
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = $"{row.Label}{boundStr}{editedTag}",
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = ResolveBrush("InkBrush")
            });
            ConstraintsList.Children.Add(panel);
        }
    }

    private void RebuildChecksPanel(IReadOnlyList<DrcFinding> findings)
    {
        DrcFindingsList.Children.Clear();
        ExclusionErrorText.IsVisible = bound?.ExclusionError is not null;
        ExclusionErrorText.Text = bound?.ExclusionError ?? "";

        foreach (var f in findings)
        {
            var box = new StackPanel();
            string statusTag = f.Excluded ? $" [Excluded: {f.ExclusionReason}]" : "";
            box.Children.Add(new TextBlock
            {
                Text = $"[{f.Severity}] {f.RuleId} · {f.RuleVersion} ({f.RuleDate}){statusTag}\n{f.Message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = ResolveBrush("InkBrush")
            });

            if (!f.Excluded && f.Severity is DrcSeverity.Error or DrcSeverity.Warning or DrcSeverity.Advisory)
            {
                var actionRow = new StackPanel { Orientation = Orientation.Horizontal };
                var reasonBox = new TextBox { Watermark = "Exclusion reason…" };
                var excludeBtn = new Button { Content = Labels.SetupExcludeFinding };
                excludeBtn.Click += (_, _) => bound?.ExcludeFinding(f, reasonBox.Text ?? "", "");
                actionRow.Children.Add(reasonBox);
                actionRow.Children.Add(excludeBtn);
                box.Children.Add(actionRow);
            }
            DrcFindingsList.Children.Add(box);
        }
    }

    private static void SetBox(TextBox box, double value, string fmt)
    {
        if (box.IsFocused) return;
        box.Text = double.IsFinite(value) ? value.ToString(fmt, CultureInfo.InvariantCulture) : "";
    }

    private static string FormatTarget(string target, double value) => target switch
    {
        "maxSpan" => string.Create(CultureInfo.InvariantCulture, $"{value:0.###} m"),
        "maxChord" => string.Create(CultureInfo.InvariantCulture, $"{value * 1000:0.#} mm"),
        "targetArea" => string.Create(CultureInfo.InvariantCulture, $"{value * 1e4:0.#} cm²"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{value:0.##}")
    };

    private IBrush ResolveBrush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out object? res) && res is IBrush brush ? brush : Brushes.Gray;
}
