using CfdWorkbench.Analysis.Export;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>
/// The Export dialog for the section .dat, the wing STL and the wing 3MF (docs/design/export.md 6.2). Thin: every number and sentence comes
/// from the <see cref="ExportSession"/>. Export... opens the native save panel through <c>pick</c>; a failed write stays here. The wing
/// mesh (STL or 3MF, one set of options) is built off the UI thread: <see cref="Preparation"/>
/// completes when the summary has been redrawn from it.
/// </summary>
public partial class ExportDialog : Window
{
    private readonly ExportSession session;
    private ListBoxItem? stepFormatItem;
    private ListBoxItem? threeDmFormatItem;
    private string? lastPath;
    private bool busy;
    private CancellationTokenSource? writing;

    public ExportOutcome? Outcome { get; private set; }

    /// <summary>The last mesh build and the redraw that follows it; already complete when nothing is being prepared.</summary>
    public Task Preparation { get; private set; } = Task.CompletedTask;

    public ExportDialog(ExportSession session, Func<string, string?, Task<string?>>? pick = null, Action? jump = null, Control? returnFocus = null)
    {
        this.session = session;
        Func<string, string?, Task<string?>> panel = pick ?? ((name, folder) => PickWithPanelAsync(this, name, folder));
        InitializeComponent();
        Icon = App.AppIcon;
        // F-35: consistent OS dialog button order (Windows: primary left, Cancel right; macOS: Cancel left, primary right).
        if (OperatingSystem.IsMacOS())
        {
            FooterButtons.Children.Clear();
            FooterButtons.Children.Add(CancelButton);
            FooterButtons.Children.Add(ExportButton);
        }
        DatFormatItem.Content = ExportCopy.FormatDat;
        AutomationProperties.SetName(DatFormatItem, ExportCopy.FormatDat);
        StlFormatItem.IsSelected = session.Format == ExportFormat.Stl;
        DatFormatItem.IsSelected = session.Format == ExportFormat.Dat;
        StlFormatItem.Content = ExportCopy.FormatStl;
        AutomationProperties.SetName(StlFormatItem, ExportCopy.FormatStl);
        ThreeMfFormatItem.IsSelected = session.Format == ExportFormat.ThreeMf;
        ThreeMfFormatItem.Content = ExportCopy.FormatThreeMf;
        AutomationProperties.SetName(ThreeMfFormatItem, ExportCopy.FormatThreeMf);
        if (session.CadGate.StepUnlocked)
        {
            stepFormatItem = new ListBoxItem { Name = "StepFormatItem", Content = ExportCopy.FormatStep, IsSelected = session.Format == ExportFormat.Step };
            AutomationProperties.SetName(stepFormatItem, ExportCopy.FormatStep);
            FormatList.Items.Add(stepFormatItem);
            StepButton.IsVisible = false;
            StepWhy.IsVisible = false;
        }
        else
        {
            StepButton.IsVisible = true;
            StepButton.IsEnabled = false;
            StepWhy.IsVisible = true;
            StepWhy.Text = session.CadGate.StepReason;
            AutomationProperties.SetHelpText(StepButton, session.CadGate.StepReason);
        }
        if (session.CadGate.ThreeDmUnlocked)
        {
            threeDmFormatItem = new ListBoxItem { Name = "ThreeDmFormatItem", Content = ExportCopy.FormatThreeDm, IsSelected = session.Format == ExportFormat.ThreeDm };
            AutomationProperties.SetName(threeDmFormatItem, ExportCopy.FormatThreeDm);
            FormatList.Items.Add(threeDmFormatItem);
        }
        ScopeLegend.Text = ExportCopy.ScopeLabel;
        ScopeWhole.Content = ExportCopy.ScopeWhole;
        ScopeHalf.Content = ExportCopy.ScopeHalf;
        AutomationProperties.SetLabeledBy(ScopeWhole, ScopeLegend);
        AutomationProperties.SetLabeledBy(ScopeHalf, ScopeLegend);
        ScopeHelp.Text = ExportCopy.ScopeHalfHelp;
        CadScopeLegend.Text = ExportCopy.ScopeLabel;
        CadScopeWhole.Content = ExportCopy.ScopeWhole;
        CadScopeHalf.Content = ExportCopy.ScopeHalf;
        AutomationProperties.SetLabeledBy(CadScopeWhole, CadScopeLegend);
        AutomationProperties.SetLabeledBy(CadScopeHalf, CadScopeLegend);
        CadUnitLegend.Text = ExportCopy.UnitLabel;
        CadUnitText.Text = "mm. " + ExportCopy.UnitFixedInFile;
        CadScopeHelp.Text = ExportCopy.ScopeHalfHelp;
        StepVariantLegend.Text = ExportCopy.StepVariantLabel;
        StepVariantClosed.Content = ExportCopy.StepVariantClosed;
        StepVariantUntrimmed.Content = ExportCopy.StepVariantUntrimmed;
        AutomationProperties.SetLabeledBy(StepVariantClosed, StepVariantLegend);
        AutomationProperties.SetLabeledBy(StepVariantUntrimmed, StepVariantLegend);
        ToleranceLegend.Text = ExportCopy.ToleranceLabel;
        ToleranceDraft.Content = ExportCopy.ToleranceDraft;
        TolerancePrint.Content = ExportCopy.TolerancePrint;
        ToleranceFine.Content = ExportCopy.ToleranceFine;
        AutomationProperties.SetLabeledBy(ToleranceDraft, ToleranceLegend);
        AutomationProperties.SetLabeledBy(TolerancePrint, ToleranceLegend);
        AutomationProperties.SetLabeledBy(ToleranceFine, ToleranceLegend);
        UnitLegend.Text = ExportCopy.UnitLabel;
        UnitText.Text = "mm. " + ExportCopy.UnitFixed;
        ClosureText.Text = ExportCopy.MeshNotClosed;
        ClosureDetails.Text = ExportCopy.MeshNotClosedDetails;
        ShapeLegend.Text = ExportCopy.ShapeLabel;
        ShapeAtStation.Content = ExportCopy.ShapeAtStation;
        ShapeOwn.Content = ExportCopy.ShapeOwn;
        AutomationProperties.SetLabeledBy(ShapeAtStation, ShapeLegend);
        AutomationProperties.SetLabeledBy(ShapeOwn, ShapeLegend);
        StationLegend.Text = ExportCopy.StationLabel;
        AutomationProperties.SetName(StationBox, ExportCopy.StationLabel);
        OrderLegend.Text = ExportCopy.OrderLabel;
        OrderSelig.Content = ExportCopy.OrderSelig;
        OrderLednicer.Content = ExportCopy.OrderLednicer;
        AutomationProperties.SetLabeledBy(OrderSelig, OrderLegend);
        AutomationProperties.SetLabeledBy(OrderLednicer, OrderLegend);
        PointsLegend.Text = ExportCopy.PointsLabel;
        AutomationProperties.SetLabeledBy(Points61, PointsLegend);
        AutomationProperties.SetLabeledBy(Points101, PointsLegend);
        AutomationProperties.SetLabeledBy(Points201, PointsLegend);
        SummaryHeading.Text = ExportCopy.SummaryHeading;
        LimitLine.Text = ExportCopy.Limit;
        SafetyLine.Text = ExportCopy.Safety;
        CancelButton.Content = ExportCopy.Cancel;
        ExportButton.Content = ExportCopy.ExportMenu;
        AnotherPlaceButton.Content = ExportCopy.ChooseAnotherPlace;
        TryAgainButton.Content = ExportCopy.TryAgain;
        foreach (var station in session.Source.Stations)
            StationBox.Items.Add($"{station.Name} (chord {ExportCopy.Fixed(station.ChordMm, 2)} mm)");
        StationBox.SelectedIndex = session.StationIndex;
        StationBox.SelectionChanged += (_, _) =>
        {
            if (StationBox.SelectedIndex >= 0 && StationBox.SelectedIndex != session.StationIndex) Change(station: StationBox.SelectedIndex);
        };
        ShapeAtStation.IsCheckedChanged += (_, _) => { if (ShapeAtStation.IsChecked == true) Change(shape: DatShape.AtStation); };
        ShapeOwn.IsCheckedChanged += (_, _) => { if (ShapeOwn.IsChecked == true) Change(shape: DatShape.Own); };
        OrderSelig.IsCheckedChanged += (_, _) => { if (OrderSelig.IsChecked == true) Change(order: DatOrder.Selig); };
        OrderLednicer.IsCheckedChanged += (_, _) => { if (OrderLednicer.IsChecked == true) Change(order: DatOrder.Lednicer); };
        foreach (var (button, count) in new[] { (Points61, 61), (Points101, 101), (Points201, 201) })
            button.IsCheckedChanged += (_, _) => { if (button.IsChecked == true) Change(points: count); };
        FormatList.SelectionChanged += (_, _) =>
        {
            var format = threeDmFormatItem?.IsSelected == true ? ExportFormat.ThreeDm
                : stepFormatItem?.IsSelected == true ? ExportFormat.Step
                : ThreeMfFormatItem.IsSelected ? ExportFormat.ThreeMf
                : StlFormatItem.IsSelected ? ExportFormat.Stl
                : ExportFormat.Dat;
            if (format != session.Format) Change(format: format);
        };
        ScopeWhole.IsCheckedChanged += (_, _) => { if (ScopeWhole.IsChecked == true && session.Scope != StlScope.Whole) Change(scope: StlScope.Whole); };
        ScopeHalf.IsCheckedChanged += (_, _) => { if (ScopeHalf.IsChecked == true && session.Scope != StlScope.Half) Change(scope: StlScope.Half); };
        CadScopeWhole.IsCheckedChanged += (_, _) => { if (CadScopeWhole.IsChecked == true && session.Scope != StlScope.Whole) Change(scope: StlScope.Whole); };
        CadScopeHalf.IsCheckedChanged += (_, _) => { if (CadScopeHalf.IsChecked == true && session.Scope != StlScope.Half) Change(scope: StlScope.Half); };
        StepVariantClosed.IsCheckedChanged += (_, _) => { if (StepVariantClosed.IsChecked == true && session.StepVariant != StepVariant.ClosedShell) Change(stepVariant: StepVariant.ClosedShell); };
        StepVariantUntrimmed.IsCheckedChanged += (_, _) => { if (StepVariantUntrimmed.IsChecked == true && session.StepVariant != StepVariant.Untrimmed) Change(stepVariant: StepVariant.Untrimmed); };
        ToleranceDraft.IsCheckedChanged += (_, _) => { if (ToleranceDraft.IsChecked == true) Change(preset: StlPreset.Draft); };
        TolerancePrint.IsCheckedChanged += (_, _) => { if (TolerancePrint.IsChecked == true) Change(preset: StlPreset.Print); };
        ToleranceFine.IsCheckedChanged += (_, _) => { if (ToleranceFine.IsChecked == true) Change(preset: StlPreset.Fine); };
        JumpButton.Click += (_, _) => { jump?.Invoke(); Close(); };
        CancelButton.Click += (_, _) =>
        {
            if (writing is not null) writing.Cancel();
            else Close();
        };
        ExportButton.Click += async (_, _) => await ExportAsync(panel);
        AnotherPlaceButton.Click += async (_, _) => await ExportAsync(panel);
        TryAgainButton.Click += async (_, _) => await ExportAsync((_, _) => Task.FromResult(lastPath));
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape) return;
            if (writing is not null) { args.Handled = true; writing.Cancel(); return; }
            if (busy) return;
            args.Handled = true;
            Close();
        }, RoutingStrategies.Tunnel);
        if (OsAccessibility.IsReducedMotion())
        {
            WritingProgress.IsIndeterminate = false;
            WritingProgress.Value = 50;
            LayoutUpdated += (_, _) => MainWindow.SuppressTransitions(this);
        }
        Opened += (_, _) =>
        {
            MaxHeight = DialogChrome.BoundedMaxHeight(Owner as Window);
            ((FormatList.SelectedItem as Control) ?? FormatList).Focus();
        };
        Closed += (_, _) => returnFocus?.Focus();
        Refresh();
    }

    /// <summary>The native save panel, owned by <paramref name="top"/> (the dialog, so a modal sheet is never hidden behind it). Null is Cancel.</summary>
    public static async Task<string?> PickWithPanelAsync(TopLevel top, string suggestedName, string? folder)
    {
        string extension = Path.GetExtension(suggestedName).TrimStart('.').ToLowerInvariant();
        var (title, label) = extension switch
        {
            "stl" => ("Export wing STL", "Wing mesh (.stl)"),
            "3mf" => ("Export wing 3MF", "Wing mesh (.3mf)"),
            "step" => ("Export wing STEP", "STEP AP203 (.step)"),
            "3dm" => ("Export wing 3DM", "Rhino 3D (.3dm)"),
            _ => ("Export section .dat", "Airfoil coordinates (.dat)")
        };
        var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [new Avalonia.Platform.Storage.FilePickerFileType(label) { Patterns = [$"*.{extension}"] }]
        };
        if (folder is not null && Uri.TryCreate(folder, UriKind.Absolute, out var start)) options.SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(start);
        var file = await top.StorageProvider.SaveFilePickerAsync(options);
        if (file is null) return null;
        using (file) return file.Path.LocalPath;
    }

    private void Change(DatShape? shape = null, DatOrder? order = null, int? points = null, int? station = null,
        ExportFormat? format = null, StlScope? scope = null, StlPreset? preset = null, StepVariant? stepVariant = null)
    {
        session.Set(shape, order, points, station, format, scope, preset, stepVariant);
        Refresh();
        if (session.Preparing) Preparation = PrepareAsync();
    }

    // The mesh or CAD surface is built off the UI thread; the summary shows its skeleton meanwhile (H10). A newer option drops this build, and its own call redraws.
    private async Task PrepareAsync()
    {
        await session.PrepareAsync();
        if (!session.Preparing) Refresh();
    }

    /// <summary>Redraws the summary, the findings and the buttons from the session. Reads only the session.</summary>
    public void Refresh()
    {
        bool blocked = session.BlockedReason is not null;
        TitleText.Text = blocked ? ExportCopy.BlockedTitle : ExportCopy.Title;
        Title = TitleText.Text;
        OptionsPanel.IsEnabled = !blocked;
        SummaryBlock.IsVisible = !blocked;
        BlockedBand.IsVisible = blocked;
        BlockedText.Text = session.BlockedReason ?? "";
        bool stl = session.IsMesh;
        bool cad = session.IsCad;
        DatOptions.IsVisible = !stl && !cad;
        StlOptions.IsVisible = stl;
        CadOptions.IsVisible = cad;
        StepVariantPanel.IsVisible = session.Format == ExportFormat.Step;
        ScopeHelp.IsVisible = stl && session.Scope == StlScope.Half;
        CadScopeHelp.IsVisible = cad && session.Scope == StlScope.Half;
        CadScopeWhole.IsChecked = session.Scope == StlScope.Whole;
        CadScopeHalf.IsChecked = session.Scope == StlScope.Half;
        StepVariantClosed.IsChecked = session.StepVariant == StepVariant.ClosedShell;
        StepVariantUntrimmed.IsChecked = session.StepVariant == StepVariant.Untrimmed;
        ShapeHelp.Text = session.ShapeHelp;
        FillSummary();
        LimitLine.Text = session.Preparing ? (cad ? ExportCopy.PreparingCad : ExportCopy.Preparing) : session.LimitText;
        PreviewText.Text = string.Join("\n", session.PreviewLines);
        PreviewBorder.IsVisible = session.PreviewLines.Count > 0;
        var finding = session.Finding;
        FindingBand.IsVisible = finding is not null;
        FindingText.Text = finding?.Text ?? "";
        JumpButton.Content = finding?.Jump ?? "";
        LargeText.Text = session.LargeMeshBand ?? "";
        LargeBand.IsVisible = LargeText.Text.Length > 0;
        ToleranceText.Text = session.ToleranceNotReachedBand ?? "";
        ToleranceBand.IsVisible = ToleranceText.Text.Length > 0;
        ClosureText.Text = session.ClosureMessage ?? ExportCopy.MeshNotClosed;
        ClosureBand.IsVisible = session.ClosureFailed;
        ExportButton.IsEnabled = session.CanExport && !busy;
        ExportButton.Content = session.ButtonLabel;
    }

    private void FillSummary()
    {
        SummaryGrid.Children.Clear();
        SummaryGrid.RowDefinitions.Clear();
        int row = 0;
        foreach (var (label, value) in session.SummaryRows)
        {
            SummaryGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var name = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Top };
            name.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("MutedBrush"));
            var text = new TextBlock { Text = value, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            Grid.SetRow(name, row); Grid.SetColumn(name, 0);
            Grid.SetRow(text, row); Grid.SetColumn(text, 1);
            SummaryGrid.Children.Add(name);
            SummaryGrid.Children.Add(text);
            row++;
        }
    }

    /// <summary>Export... : the save panel, the write, then close (Written, Cancelled) or stay with the cause (Failed).</summary>
    public async Task ExportAsync(Func<string, string?, Task<string?>> picker)
    {
        if (busy || !session.CanExport) return;
        busy = true;
        FailureBand.IsVisible = false;
        ExportButton.IsEnabled = false;
        try
        {
            // A mesh over 100,000 triangles shows its progress and a Cancel that removes the temp file (H10); the .dat and small meshes write in a frame.
            bool progress = session.Stl is { } mesh && session.IsMesh && mesh.Triangles > ExportSession.ProgressTriangles;
            var outcome = await session.RunAsync(async (name, folder) => lastPath = await picker(name, folder), async (target, bytes) =>
            {
                using var cancel = new CancellationTokenSource();
                writing = progress ? cancel : null;
                if (progress)
                {
                    WritingText.Text = ExportCopy.Writing(session.Stl!.Triangles.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), session.FileMegabytes(session.Stl));
                    WritingBand.IsVisible = true;
                    Title = TitleText.Text = ExportCopy.WritingTitle;
                    await Task.Yield();
                }
                try { await ExportSession.WriteAtomicAsync(target, bytes, cancel.Token); }
                finally { writing = null; WritingBand.IsVisible = false; Title = TitleText.Text = ExportCopy.Title; }
            });
            if (outcome.Kind == ExportOutcomeKind.Failed)
            {
                FailureText.Text = outcome.Message;
                FailureBand.IsVisible = true;
                TryAgainButton.IsEnabled = lastPath is not null;
                return;
            }
            Outcome = outcome;
            busy = false;
            Close(outcome);
        }
        finally
        {
            busy = false;
            Refresh();
        }
    }
}
