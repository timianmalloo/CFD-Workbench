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
/// The Export dialog for the section .dat (docs/design/export.md 6.2). Thin: every number and sentence comes from the
/// <see cref="ExportSession"/>. Only one format exists in this slice, so the format list has one row; STL and 3MF join it
/// when their slices land. Export... opens the native save panel through <c>pick</c>; a failed write stays here.
/// </summary>
public partial class ExportDialog : Window
{
    private readonly ExportSession session;
    private string? lastPath;
    private bool busy;

    public ExportOutcome? Outcome { get; private set; }

    public ExportDialog(ExportSession session, Func<string, string?, Task<string?>>? pick = null, Action? jump = null, Control? returnFocus = null)
    {
        this.session = session;
        Func<string, string?, Task<string?>> panel = pick ?? ((name, folder) => PickWithPanelAsync(this, name, folder));
        InitializeComponent();
        DatFormatItem.Content = ExportCopy.FormatDat;
        AutomationProperties.SetName(DatFormatItem, ExportCopy.FormatDat);
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
        JumpButton.Click += (_, _) => { jump?.Invoke(); Close(); };
        CancelButton.Click += (_, _) => Close();
        ExportButton.Click += async (_, _) => await ExportAsync(panel);
        AnotherPlaceButton.Click += async (_, _) => await ExportAsync(panel);
        TryAgainButton.Click += async (_, _) => await ExportAsync((_, _) => Task.FromResult(lastPath));
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape || busy) return;
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
        var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export section .dat",
            SuggestedFileName = suggestedName,
            DefaultExtension = "dat",
            FileTypeChoices = [new Avalonia.Platform.Storage.FilePickerFileType("Airfoil coordinates (.dat)") { Patterns = ["*.dat"] }]
        };
        if (folder is not null && Uri.TryCreate(folder, UriKind.Absolute, out var start)) options.SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(start);
        var file = await top.StorageProvider.SaveFilePickerAsync(options);
        if (file is null) return null;
        using (file) return file.Path.LocalPath;
    }

    private void Change(DatShape? shape = null, DatOrder? order = null, int? points = null, int? station = null)
    {
        session.Set(shape, order, points, station);
        Refresh();
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
        ShapeHelp.Text = session.ShapeHelp;
        FillSummary();
        PreviewText.Text = string.Join("\n", session.PreviewLines);
        PreviewBorder.IsVisible = session.PreviewLines.Count > 0;
        var finding = session.Finding;
        FindingBand.IsVisible = finding is not null;
        FindingText.Text = finding?.Text ?? "";
        JumpButton.Content = finding?.Jump ?? "";
        ExportButton.IsEnabled = session.CanExport && !busy;
        ExportButton.Content = blocked ? ExportCopy.Title : ExportCopy.ExportMenu;
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
            var outcome = await session.RunAsync(async (name, folder) => lastPath = await picker(name, folder));
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
