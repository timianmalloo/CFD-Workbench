using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Analysis;

/// <summary>
/// The Compare pane (design a3d-compare.md 5): two run pickers, the headline, the status of the record, the inputs side by side
/// (a differing value is bold with a "≠" glyph, never colour alone), the outputs with the delta to the base, and the footnote.
/// It renders <see cref="CompareViewModel"/> verbatim; the commands and the recording are the controller's.
/// </summary>
public partial class ComparePane : UserControl
{
    private WorkbenchController? controller;
    private bool filling;

    public ComparePane()
    {
        InitializeComponent();
        BasePicker.MinWidth = ComparedPicker.MinWidth = 180;
        BasePicker.MaxWidth = ComparedPicker.MaxWidth = 340;
        BaseLabel.Text = Labels.BaseRunLabel;
        ComparedLabel.Text = Labels.ComparedRunLabel;
        AutomationProperties.SetName(BasePicker, Labels.BaseRunLabel);
        AutomationProperties.SetName(ComparedPicker, Labels.ComparedRunLabel);
        EstimatorButton.Content = Labels.CompareTiersCommand;
        AutomationProperties.SetName(EstimatorButton, Labels.CompareTiersCommand);
        BasePicker.SelectionChanged += (_, _) => PickerChanged();
        ComparedPicker.SelectionChanged += (_, _) => PickerChanged();
        EstimatorButton.Click += (_, _) => { if (controller is not null) _ = controller.CompareWithEstimatorAsync(); };
        FootnoteText.Text = Labels.CompareFootnote;
    }

    private IReadOnlyList<WorkbenchController.CompareOption> options = [];

    /// <summary>The accessible names of the focusable controls the Tab key visits, in order.</summary>
    public IEnumerable<string> TabOrder
    {
        get
        {
            var stops = new List<Control> { BasePicker, ComparedPicker };
            if (EstimatorButton.IsVisible) stops.Add(EstimatorButton);
            stops.Add(InputsTable);
            stops.Add(OutputsTable);
            return stops.Where(control => control.Focusable).Select(control => AutomationProperties.GetName(control) ?? "");
        }
    }

    /// <summary>Focus lands on the Base picker when the tab opens; posted, because the tab content is realised by the same layout pass.</summary>
    public void FocusBase() => Avalonia.Threading.Dispatcher.UIThread.Post(() => BasePicker.Focus(), Avalonia.Threading.DispatcherPriority.Input);

    public void Bind(WorkbenchController source)
    {
        ArgumentNullException.ThrowIfNull(source);
        controller = source;
        if (source.Inspection is null)
        {
            Render(new CompareViewModel(CompareState.NoSecondRun, "", null, [Labels.NeedsTwoRuns], null, null, [], [], null), [], null, null, false);
            return;
        }
        var view = source.CompareView;
        Render(view, source.CompareOptions, view.Base?.RunId, view.Compared?.RunId, source.CompareEvaluating);
        EstimatorButton.IsEnabled = source.CanCompare;
    }

    public void Render(CompareViewModel view, IReadOnlyList<WorkbenchController.CompareOption> runs, string? baseId, string? comparedId, bool evaluating)
    {
        options = runs;
        filling = true;
        try
        {
            Fill(BasePicker, baseId);
            Fill(ComparedPicker, comparedId);
        }
        finally { filling = false; }
        BasePicker.IsEnabled = ComparedPicker.IsEnabled = !evaluating;
        EstimatorButton.IsVisible = view.State == CompareState.NoSecondRun;
        EstimatorButton.IsEnabled = !evaluating;
        HeadlineText.Text = evaluating ? Labels.EvaluatingEstimator : view.Headline;
        StatusText.IsVisible = view.RecordStatus is not null;
        StatusText.Text = view.RecordStatus ?? "";
        NoticeHost.Children.Clear();
        foreach (string notice in view.Notices)
            NoticeHost.Children.Add(new TextBlock { Text = notice, Classes = { "pnl-note" }, TextWrapping = TextWrapping.Wrap });
        ColumnsHost.Children.Clear();
        foreach (var column in new[] { view.Base, view.Compared }.OfType<CompareColumn>())
            ColumnsHost.Children.Add(new TextBlock
            {
                Text = $"{column.Title} · {column.Method} · {column.Freshness}", Classes = { "pnl-head" }, TextWrapping = TextWrapping.Wrap
            });
        InputsTable.Children.Clear();
        if (view.Inputs.Count > 0)
        {
            string baseTitle = view.Base?.Title ?? Labels.BaseRunLabel;
            string comparedTitle = view.Compared?.Title ?? Labels.ComparedRunLabel;
            InputsTable.Children.Add(HeaderLine("160,*,*", ["Field", baseTitle, comparedTitle], rightColumns: []));
        }
        foreach (var row in view.Inputs)
        {
            bool numeric = AnalysisPanel.IsNumericValue(row.Base) && AnalysisPanel.IsNumericValue(row.Compared);
            InputsTable.Children.Add(Line("160,*,*", [row.Field, Mark(row.Base, row.Differs), Mark(row.Compared, row.Differs)],
                bold: row.Differs, rightColumns: numeric ? [1, 2] : []));
        }
        OutputsTable.Children.Clear();
        if (view.Outputs.Count > 0)
        {
            string baseTitle = view.Base?.Title ?? Labels.BaseRunLabel;
            string comparedTitle = view.Compared?.Title ?? Labels.ComparedRunLabel;
            OutputsTable.Children.Add(HeaderLine("160,120,120,*,*", ["Quantity", baseTitle, comparedTitle, "Delta", "Note"], rightColumns: [1, 2, 3]));
        }
        foreach (var row in view.Outputs)
        {
            var line = Line("160,120,120,*,*", [row.Quantity, row.Base, row.Compared, row.Delta, row.Note ?? ""], bold: false, rightColumns: [1, 2, 3]);
            if (line.Children[3] is TextBlock delta) AutomationProperties.SetName(delta, row.AccessibleName);
            OutputsTable.Children.Add(line);
        }
        FootnoteText.IsVisible = view.Footnote is not null;
    }

    private static string Mark(string value, bool differs) => differs ? "≠ " + value : value;

    private void Fill(ComboBox picker, string? selectedId)
    {
        picker.Items.Clear();
        int selected = -1;
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            picker.Items.Add(new ComboBoxItem { Content = option.Reason is null ? option.Label : option.Label + " — " + option.Reason, IsEnabled = option.Enabled });
            if (option.RunId == selectedId) selected = i;
        }
        picker.SelectedIndex = selected;
    }

    private void PickerChanged()
    {
        if (filling || controller is null) return;
        string? baseId = BasePicker.SelectedIndex >= 0 ? options[BasePicker.SelectedIndex].RunId : null;
        string? comparedId = ComparedPicker.SelectedIndex >= 0 ? options[ComparedPicker.SelectedIndex].RunId : null;
        controller.SetCompareRuns(baseId, comparedId);
    }

    private static Grid HeaderLine(string columns, string[] cells, int[] rightColumns)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(columns) };
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = new TextBlock
            {
                Text = cells[i],
                Classes = { "pnl-cell" },
                Margin = new Thickness(4, 2),
                FontWeight = FontWeight.SemiBold,
                TextAlignment = rightColumns.Contains(i) ? TextAlignment.Right : TextAlignment.Left
            };
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }
        return grid;
    }

    private static Grid Line(string columns, string[] cells, bool bold, int[] rightColumns)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(columns) };
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = new TextBlock
            {
                Text = cells[i],
                Classes = { "pnl-cell" },
                Margin = new Thickness(4, 1),
                FontWeight = bold && i > 0 ? FontWeight.Bold : FontWeight.Normal,
                TextAlignment = rightColumns.Contains(i) ? TextAlignment.Right : TextAlignment.Left
            };
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }
        return grid;
    }
}
