using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using System.Globalization;

namespace CfdWorkbench.Desktop.Panes;

public partial class RailEditorPane : UserControl
{
    private WorkbenchController? boundController;
    private readonly List<(string Rail, AuthoredControl Control, string Unit)> targets = [];
    private readonly NumericBindingGuard numericBinding = new();
    private bool refreshing;

    public RailEditorPane()
    {
        InitializeComponent();

        ControlList.SelectionChanged += OnControlSelectionChanged;
        ControlList.KeyDown += OnControlKeyDown;
        NumericInput.TextChanged += OnNumericChanged;
        NumericInput.KeyDown += OnNumericKeyDown;

        PreviewButton.Click += async (_, _) =>
        {
            if (boundController != null) await boundController.PreviewAsync();
        };

        ApplyButton.Click += (_, _) =>
        {
            boundController?.Apply();
        };

        CancelButton.Click += (_, _) =>
        {
            boundController?.Cancel();
        };

        TryAgainButton.Click += (_, _) =>
        {
            ErrorPanel.IsVisible = false;
            if (boundController != null) Bind(boundController);
        };
    }

    public void ShowRenderFailure(bool foilOpen)
    {
        ErrorText.Text = "Rail editor couldn't be shown." + (foilOpen ? " Your foil hasn't changed." : "");
        ErrorPanel.IsVisible = true;
    }

    public void Bind(WorkbenchController controller)
    {
        boundController = controller;
        try
        {
            ErrorPanel.IsVisible = false;
            var authored = controller.CurrentProjection;
            if (authored is null || authored.Rails.Count == 0)
            {
                EmptyPanel.IsVisible = true;
                ContentPanel.IsVisible = false;
                targets.Clear();
                ControlList.ItemsSource = null;
                return;
            }

            EmptyPanel.IsVisible = false;
            ContentPanel.IsVisible = true;

            refreshing = true;
            targets.Clear();
            var items = new List<ListBoxItem>();
            foreach (var rail in authored.Rails)
            {
                foreach (var ctrl in rail.Controls)
                {
                    targets.Add((rail.Name, ctrl, rail.SourceUnit));
                    string status = ctrl.Editable ? "editable" : $"locked: {string.Join(", ", ctrl.ApplicableLocks)}";
                    string display = $"{rail.Name} {ctrl.Id} · η {ctrl.Eta:G3} · {ctrl.OrdinateSi * 1000:G6} mm ({status})";
                    var item = new ListBoxItem { Content = display };
                    items.Add(item);
                }
            }
            ControlList.ItemsSource = items;

            // Update draft / numeric input
            if (controller.Draft is { } draft)
            {
                DraftReadout.Text = $"Draft: {draft.Rail} {draft.VertexId} gen {draft.Generation}";
                var target = targets.FirstOrDefault(t => t.Rail == draft.Rail && t.Control.Id == draft.VertexId);
                if (target.Control != null)
                {
                    UnitLabel.Text = target.Unit;
                    NumericInput.IsEnabled = true;
                    var projection = controller.DraftProjection ?? authored;
                    if (projection != null && MainWindow.TryDraftField(projection, draft) is { } df)
                    {
                        numericBinding.NoteProgrammatic(df.Text);
                        NumericInput.Text = df.Text;
                    }
                }
            }
            else
            {
                DraftReadout.Text = "No draft.";
                if (ControlList.SelectedIndex >= 0 && ControlList.SelectedIndex < targets.Count)
                {
                    var selected = targets[ControlList.SelectedIndex];
                    UnitLabel.Text = selected.Unit;
                    NumericInput.IsEnabled = selected.Control.Editable;
                    var ac = MainWindow.AcceptedControlField(selected.Control, selected.Unit);
                    numericBinding.NoteProgrammatic(ac.Text);
                    NumericInput.Text = ac.Text;
                }
                else
                {
                    NumericInput.IsEnabled = false;
                    numericBinding.NoteProgrammatic("");
                    NumericInput.Text = "";
                }
            }
            refreshing = false;
        }
        catch (Exception ex)
        {
            targets.Clear();
            ControlList.ItemsSource = null;
            ContentPanel.IsVisible = false;
            EmptyPanel.IsVisible = false;
            ShowRenderFailure(controller?.Inspection is not null);
            ShellEvents.Record("shell.pane.render", "error", 0, "pane-bind", exceptionType: ex.GetType().Name);
            refreshing = false;
            return;
        }
    }

    private void OnControlSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (refreshing || boundController == null) return;
        if (ControlList.SelectedIndex >= 0 && ControlList.SelectedIndex < targets.Count)
        {
            var selected = targets[ControlList.SelectedIndex];
            NumericInput.IsEnabled = selected.Control.Editable;
            if (selected.Control.Editable && boundController.Draft == null)
            {
                try
                {
                    boundController.BeginEdit(selected.Rail, selected.Control.Id);
                }
                catch
                {
                    // refusal or error
                }
            }
        }
    }

    private void OnControlKeyDown(object? sender, KeyEventArgs e)
    {
        if (MainWindow.IsReeditKey(e.Key))
        {
            if (ControlList.SelectedIndex >= 0 && ControlList.SelectedIndex < targets.Count)
            {
                var selected = targets[ControlList.SelectedIndex];
                if (MainWindow.CanRestartSelectedEdit(boundController?.Draft, true, selected.Control.Editable))
                {
                    e.Handled = true;
                    boundController?.BeginEdit(selected.Rail, selected.Control.Id);
                }
            }
        }
    }

    private void OnNumericChanged(object? sender, TextChangedEventArgs e)
    {
        if (refreshing || boundController?.Draft == null) return;
        if (!numericBinding.ShouldProcess(NumericInput.Text, NumericInput.IsEnabled)) return;

        if (double.TryParse(NumericInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            var target = targets.FirstOrDefault(t => t.Rail == boundController.Draft.Rail && t.Control.Id == boundController.Draft.VertexId);
            double scale = target.Unit switch { "mm" => 1000, "cm" => 100, _ => 1 };
            try
            {
                boundController.UpdateDraft(val / scale);
            }
            catch
            {
                boundController.InvalidateDraftInput();
            }
        }
        else
        {
            boundController.InvalidateDraftInput();
        }
    }

    private async void OnNumericKeyDown(object? sender, KeyEventArgs e)
    {
        if (boundController?.Draft == null) return;
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            boundController.Cancel();
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (boundController.Provenance == "preview") boundController.Apply();
            else await boundController.PreviewAsync();
        }
    }
}
