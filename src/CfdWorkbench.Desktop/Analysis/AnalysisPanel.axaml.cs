using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Analysis bottom panel: Spanwise loading (chart and table twin), Section, Loads, Provenance. Every cell is the
/// projection's own <see cref="ResultRow"/> text, verbatim. The tabs read <see cref="WorkbenchController.AnalysisView"/>,
/// the selected run's projection (design §3.3), so a failed latest attempt keeps the previous run's tables, Historical,
/// beside the error card. A layer's table twin is always drawn: hiding the layer never hides its table.
/// </summary>
public partial class AnalysisPanel : UserControl
{
    private readonly LoadingChart loading = new();
    private WorkbenchController? boundController;
    private bool binding;

    public AnalysisPanel()
    {
        InitializeComponent();
        LoadingHost.Content = loading;
        OpenInMainAreaButton.Content = Labels.OpenInMainArea;
        AutomationProperties.SetName(OpenInMainAreaButton, Labels.OpenInMainArea);
        OpenInMainAreaButton.Click += (_, _) => OpenSectionRequested?.Invoke();
        AskCalculationButtonText.Text = Assistant.AssistantCopy.AnalysisEntryTitle;
        AutomationProperties.SetName(AskCalculationButton, Assistant.AssistantCopy.AnalysisEntryTitle);
        AskCalculationButton.Click += (_, _) => ToggleAssistantDrawer();
        AnalysisAssistantBar.Configure(
            Assistant.AssistantCopy.AnalysisEntryTitle,
            "Ask",
            "Ask about loads, drag, cavitation, or what-if conditions…",
            "Ask about this calculation");
        AnalysisAssistantBar.ConfigureKeyRequested += () => ConfigureKeyRequested?.Invoke();
        AnalysisAssistantBar.PromptSubmitted += prompt => _ = HandleAnalysisPromptAsync(prompt);
        SolverSetupButtonText.Text = SolverSetup.SolverSetupCopy.VerbTitle;
        AutomationProperties.SetName(SolverSetupButton, SolverSetup.SolverSetupCopy.VerbTitle);
        SolverSetupButton.Click += (_, _) => SolverSetupRequested?.Invoke();
        CompareTab.Header = Labels.CompareTab;
        PanelTabs.SelectionChanged += (_, args) =>
        {
            if (!ReferenceEquals(args.Source, PanelTabs)) return;
            UpdateHeight();
            if (!binding && ReferenceEquals(PanelTabs.SelectedItem, CompareTab) && boundController is not null)
                ComparePaneHost.Bind(boundController);
        };
        // The slot height is the Bottom region's preset size (LayoutCodec Chrome: 190), 150 in a short window (HeightFor);
        // the skeleton bars are static placeholders.
        Height = FullHeight;
        SkeletonLong.Width = 320;
        SkeletonShort.Width = 240;
    }

    /// <summary>The panel's height, and its height in a window shorter than <see cref="ShortWindowClientHeight"/> (Ruling 101 3d, layout C).</summary>
    public const double FullHeight = 190, ShortHeight = 150;

    /// <summary>
    /// A window client under 820 px tall gets the short panel: 1280x800 (client 800) fires it and 1500x870 (client 860) does not,
    /// so four views fit at 1280x800 with the 320x240 floor unchanged (measured margin: docs/proof/lay-1280/options.md, C + D).
    /// </summary>
    public const double ShortWindowClientHeight = 820;

    public static double HeightFor(double windowClientHeight) =>
        windowClientHeight > 0 && windowClientHeight < ShortWindowClientHeight ? ShortHeight : FullHeight;

    /// <summary>The panel's height while the Section tab (one summary line) is the shown tab and nothing sits above the tabs (Ruling 125, state D).</summary>
    public const double CompactHeight = 68;

    /// <summary>Expanded height while the inline Analysis assistant drawer is open (R2-08).</summary>
    public const double ExpandedAssistantHeight = 320;

    /// <summary>The Section view, hosted by the model area's Section document (Ruling 124). The bottom tab shows only its summary.</summary>
    public SectionTabView SectionView { get; } = new();

    /// <summary>Raised by the summary's "Open in main area" action; the shell opens and focuses the Section document.</summary>
    public event Action? OpenSectionRequested;

    /// <summary>Raised by the subtle "Set up a solver" entry in the Analysis panel (Ruling 69, F7a).</summary>
    public event Action? SolverSetupRequested;

    /// <summary>Raised by the inline Area 3 Analysis assistant bar when the user clicks Configure key / Open Settings.</summary>
    public event Action? ConfigureKeyRequested;

    public void ToggleAssistantDrawer()
    {
        AnalysisAssistantDrawer.IsVisible = !AnalysisAssistantDrawer.IsVisible;
        if (AnalysisAssistantDrawer.IsVisible)
            RefreshAssistantStatus(boundController?.Assistant);
        UpdateHeight();
    }

    public void RefreshAssistantStatus(Assistant.AssistantRuntimeHub? hub)
    {
        AnalysisAssistantBar.RefreshStatus(hub);
    }

    public void ShowAssistantAnswer(string displayText, IReadOnlyList<string>? citations = null)
    {
        AnalysisAnswerText.Text = displayText;
        if (citations is { Count: > 0 })
        {
            AnalysisAnswerCitationsText.Text = "Citations: " + string.Join(", ", citations);
            AnalysisAnswerCitationsText.IsVisible = true;
        }
        else
        {
            AnalysisAnswerCitationsText.IsVisible = false;
        }
        AnalysisAnswerCard.IsVisible = !string.IsNullOrWhiteSpace(displayText);
        UpdateHeight();
    }

    private async Task HandleAnalysisPromptAsync(string prompt)
    {
        if (boundController is null) return;
        var view = boundController.AnalysisView;
        string runId = view.RunKey ?? "no-run";
        string contextText = $"{view.StatusText}. {view.Banner ?? ""}";
        var req = new CfdWorkbench.Analysis.Assistant.AssistantPreviewRequest(
            CfdWorkbench.Core.Assistant.AssistantArea.Analysis,
            CfdWorkbench.Core.Assistant.AssistantProposalKind.AnalysisExplanation,
            "Explain the selected CFD-Workbench hydrofoil calculation using only values present in context. Return strict JSON.",
            prompt);
        var preview = await boundController.Assistant.PreparePreviewAsync(req);
        if (!preview.Allowed || preview.Prepared is null)
        {
            AnalysisAssistantBar.ShowFeedback(preview.Message ?? Assistant.AssistantCopy.NoKeyNotice);
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var valInput = new CfdWorkbench.Core.Assistant.AssistantValidationInput(
                CfdWorkbench.Core.Assistant.AssistantArea.Analysis,
                CfdWorkbench.Core.Assistant.AssistantProposalKind.AnalysisExplanation,
                AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { runId },
                SharedContextText: contextText);
            var dlg = new Assistant.AssistantConsentPreviewDialog(
                boundController.Assistant,
                preview.Prepared,
                valInput,
                onWhatIfRecompute: async offer =>
                {
                    var op = OperatingPoints.Custom(
                        offer.SpeedMps ?? boundController.AnalysisOperatingPoint.Speed,
                        offer.AlphaDeg ?? boundController.AnalysisOperatingPoint.AlphaDeg,
                        offer.HRefMeters ?? boundController.AnalysisOperatingPoint.HRef);
                    var water = WaterTable.At(
                        offer.WaterTemperatureC ?? boundController.AnalysisWater.TemperatureC,
                        (offer.IsSaltWater ?? boundController.AnalysisWater.SalinityGPerKg > 1.0)
                            ? OperatingPoints.SaltSalinityGPerKg
                            : 0.0);
                    await boundController.EvaluateAnalysisAsync(op, water);
                })
            {
                RequestedThemeVariant = owner.ActualThemeVariant
            };
            await dlg.ShowDialog(owner);
            if (dlg.EditPromptRequested)
            {
                AnalysisAssistantBar.FocusPrompt();
                return;
            }
            if (dlg.ExecutionResult?.Validation is { } val)
            {
                ShowAssistantAnswer(val.DisplayText, val.Citations);
            }
        }
    }

    private double windowHeight;

    private void UpdateHeight()
    {
        if (AnalysisAssistantDrawer.IsVisible)
        {
            Height = ExpandedAssistantHeight;
            return;
        }
        bool compact = PanelTabs.SelectedItem == SectionTab && !PanelBanner.IsVisible && !PanelErrorCard.IsVisible && !PanelSkeleton.IsVisible;
        Height = compact ? CompactHeight : HeightFor(windowHeight);
    }

    private Visual? sizedBy;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        sizedBy = e.Root as Visual;
        if (sizedBy is null) return;
        sizedBy.PropertyChanged += OnRootBounds;
        windowHeight = sizedBy.Bounds.Height;
        UpdateHeight();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (sizedBy is not null) sizedBy.PropertyChanged -= OnRootBounds;
        sizedBy = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnRootBounds(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property != BoundsProperty || sender is not Visual root) return;
        windowHeight = root.Bounds.Height;
        UpdateHeight();
    }

    public LoadingChart LoadingView => loading;

    /// <summary>The Compare tab's pane (A3d).</summary>
    public ComparePane ComparePaneView => ComparePaneHost;

    /// <summary>The run key of the view last bound (null before a result), for the tests and the shell.</summary>
    public string? BoundRunKey { get; private set; }

    public void Bind(WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        boundController = controller;
        binding = true;
        try
        {
        var view = controller.Inspection is null
            ? new AnalysisViewModel(RunState.NoResult, "Analysis: no result", null, null, [], [], null)
            : controller.AnalysisView;
        BoundRunKey = view.RunKey;
        PanelBanner.IsVisible = view.Banner is not null;
        PanelBanner.Text = view.Banner ?? "";
        PanelErrorCard.IsVisible = view.ErrorCard is not null;
        PanelErrorText.Text = view.ErrorCard ?? "";
        PanelSkeleton.IsVisible = view.State == RunState.Running;
        bool hasResult = view.Groups.Count > 0 && view.RunKey is not null;
        bool unavailable = view.State == RunState.Unavailable;
        LoadingEmpty.IsVisible = !hasResult || view.Loading.Count == 0;
        LoadingEmpty.Text = LoadingEmpty.IsVisible ? EmptyText(view) : "";
        loading.IsVisible = !LoadingEmpty.IsVisible;
        loading.Update(unavailable ? [] : view.Loading, controller.AnalysisUnits);
        SectionView.Bind(view, controller);
        ShowSummary();
        RefreshAssistantStatus(controller.Assistant);
        UpdateHeight();
        Fill(LoadsBody, view, ("Loads", "loads-table"), ("Strips", "strips-table"));
        Fill(ProvenanceBody, view, ("Conditions", "conditions-table"), ("Labels", "labels-table"), ("Provenance", "provenance-table"));
        // The Compare pane is bound only while its tab is shown or requested: its derivations are not free.
        bool compareRequested = controller.CompareTabRequested;
        if (compareRequested) PanelTabs.SelectedItem = CompareTab;
        if (ReferenceEquals(PanelTabs.SelectedItem, CompareTab)) ComparePaneHost.Bind(controller);
        if (compareRequested)
        {
            ComparePaneHost.FocusBase();
            controller.ConsumeCompareRequest();
        }
        }
        finally { binding = false; }
    }

    // One line: the shown station, cl (panel) and -Cp_min of the Section document's content, then the action that opens it.
    private void ShowSummary()
    {
        var summary = SectionView.Summary;
        SectionSummaryStation.Text = summary?.Station ?? Labels.NoResult;
        SectionSummaryCl.Text = summary is null ? "" : Labels.ClPanel + " " + summary.Cl;
        SectionSummaryCp.Text = summary is null ? "" : Labels.CpMinLabel + " " + summary.CpMin;
        OpenInMainAreaButton.IsEnabled = summary is not null;
    }

    // The tampered view (COPY-211) names its note (COPY-274) beside the verdict.
    private static string EmptyText(AnalysisViewModel view) =>
        view.Groups.FirstOrDefault()?.Rows.FirstOrDefault() is { } row
            ? view.State == RunState.Unavailable && row.Note is not null ? row.Value + ". " + row.Note : row.Value
            : Labels.NoResult;

    private static void Fill(Panel host, AnalysisViewModel view, params (string Group, string Name)[] tables)
    {
        host.Children.Clear();
        bool any = false;
        foreach (var (title, name) in tables)
        {
            if (view.Groups.FirstOrDefault(group => group.Title == title) is not { } group || view.RunKey is null) continue;
            host.Children.Add(Table(name, group));
            any = true;
        }
        if (!any) host.Children.Add(new TextBlock { Text = EmptyText(view), Classes = { "pnl-note" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
    }

    internal static StackPanel Table(string name, ResultGroup group)
    {
        var table = new StackPanel { Name = name, Margin = new Thickness(0, 0, 0, 6) };
        AutomationProperties.SetName(table, group.Title);
        table.Children.Add(new TextBlock { Text = group.Title, Classes = { "pnl-head" } });
        foreach (var row in group.Rows)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("160,160,*") };
            grid.Children.Add(Cell(row.Label, 0));
            grid.Children.Add(Cell(row.Unit is null ? row.Value : row.Value + " " + row.Unit, 1));
            grid.Children.Add(Cell(row.Note ?? "", 2));
            table.Children.Add(grid);
        }
        return table;
    }

    private static TextBlock Cell(string text, int column)
    {
        var cell = new TextBlock
        {
            Text = text,
            Classes = { "pnl-cell" },
            Margin = new Thickness(4, 1),
            TextAlignment = column == 1 && IsNumericValue(text) ? Avalonia.Media.TextAlignment.Right : Avalonia.Media.TextAlignment.Left
        };
        Grid.SetColumn(cell, column);
        return cell;
    }

    internal static bool IsNumericValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.TrimStart('≠', ' ', '+', '-', '−', '±');
        return trimmed.Length > 0 && char.IsDigit(trimmed[0]) &&
            !trimmed.Contains(" · ", StringComparison.Ordinal) &&
            !trimmed.StartsWith("Unavailable", StringComparison.Ordinal);
    }
}
