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
        DatFormatItem.Content = ExportCopy.FormatDat;
        AutomationProperties.SetName(DatFormatItem, ExportCopy.FormatDat);
        StlFormatItem.IsSelected = session.Format == ExportFormat.Stl;
        DatFormatItem.IsSelected = session.Format == ExportFormat.Dat;
        StlFormatItem.Content = ExportCopy.FormatStl;
        AutomationProperties.SetName(StlFormatItem, ExportCopy.FormatStl);
        ThreeMfFormatItem.IsSelected = session.Format == ExportFormat.ThreeMf;
        ThreeMfFormatItem.Content = ExportCopy.FormatThreeMf;
        AutomationProperties.SetName(ThreeMfFormatItem, ExportCopy.FormatThreeMf);
        ScopeLegend.Text = ExportCopy.ScopeLabel;
        ScopeWhole.Content = ExportCopy.ScopeWhole;
        ScopeHalf.Content = ExportCopy.ScopeHalf;
        ScopeHelp.Text = ExportCopy.ScopeHalfHelp;
        ToleranceLegend.Text = ExportCopy.ToleranceLabel;
        ToleranceDraft.Content = ExportCopy.ToleranceDraft;
        TolerancePrint.Content = ExportCopy.TolerancePrint;
        ToleranceFine.Content = ExportCopy.ToleranceFine;
        UnitLegend.Text = ExportCopy.UnitLabel;
        UnitText.Text = "mm. " + ExportCopy.UnitFixed;
        ClosureText.Text = ExportCopy.MeshNotClosed;
        ClosureDetails.Text = ExportCopy.MeshNotClosedDetails;
        StepWhy.Text = ExportCopy.StepUnavailable;
        AutomationProperties.SetHelpText(StepButton, ExportCopy.StepUnavailable);
        ShapeLegend.Text = ExportCopy.ShapeLabel;
        ShapeAtStation.Content = ExportCopy.ShapeAtStation;
        ShapeOwn.Content = ExportCopy.ShapeOwn;
        StationLegend.Text = ExportCopy.StationLabel;
        AutomationProperties.SetName(StationBox, ExportCopy.StationLabel);
        OrderLegend.Text = ExportCopy.OrderLabel;
        OrderSelig.Content = ExportCopy.OrderSelig;
        OrderLednicer.Content = ExportCopy.OrderLednicer;
        PointsLegend.Text = ExportCopy.PointsLabel;
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
            var format = ThreeMfFormatItem.IsSelected ? ExportFormat.ThreeMf : StlFormatItem.IsSelected ? ExportFormat.Stl : ExportFormat.Dat;
            if (format != session.Format) Change(format: format);
        };
        ScopeWhole.IsCheckedChanged += (_, _) => { if (ScopeWhole.IsChecked == true) Change(scope: StlScope.Whole); };
        ScopeHalf.IsCheckedChanged += (_, _) => { if (ScopeHalf.IsChecked == true) Change(scope: StlScope.Half); };
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
        Opened += (_, _) => (ExportButton.IsEnabled ? ExportButton : CancelButton).Focus();
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
        ExportFormat? format = null, StlScope? scope = null, StlPreset? preset = null)
    {
        session.Set(shape, order, points, station, format, scope, preset);
        Refresh();
        if (session.Preparing) Preparation = PrepareAsync();
    }

    // The mesh is built off the UI thread; the summary shows its skeleton meanwhile (H10). A newer option drops this build, and its own call redraws.
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
        DatOptions.IsVisible = !stl;
        StlOptions.IsVisible = stl;
        ScopeHelp.IsVisible = stl && session.Scope == StlScope.Half;
        ShapeHelp.Text = session.ShapeHelp;
        FillSummary();
        LimitLine.Text = session.Preparing ? ExportCopy.Preparing : session.LimitText;
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
