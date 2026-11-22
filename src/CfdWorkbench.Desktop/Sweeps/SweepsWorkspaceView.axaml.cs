using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace CfdWorkbench.Desktop.Sweeps;

public partial class SweepsWorkspaceView : UserControl
{
    private SweepController? controller;
    private bool syncingPicker;

    public SweepsWorkspaceView()
    {
        InitializeComponent();
        SubTabSetupButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.ExperimentSetup);
        SubTabRunButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.LocalRun);
        SubTabResultsButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.Results);
        ExperimentPicker.SelectionChanged += (_, _) =>
        {
            if (syncingPicker || controller is null) return;
            if (ExperimentPicker.SelectedItem is ComboBoxItem { Tag: string id })
                controller.SelectExperiment(id);
        };
    }

    public ExperimentSetupView ExperimentSetup => SetupView;
    public LocalRunView LocalRun => RunView;
    public SweepResultsView Results => ResultsView;

    public void Bind(SweepController? next)
    {
        if (ReferenceEquals(controller, next)) { Refresh(); return; }
        if (controller is not null) controller.Changed -= OnControllerChanged;
        controller = next;
        if (controller is not null) controller.Changed += OnControllerChanged;
        SetupView.Bind(controller);
        RunView.Bind(controller);
        ResultsView.Bind(controller);
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (controller is not null) controller.Changed -= OnControllerChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnControllerChanged()
    {
        if (Dispatcher.UIThread.CheckAccess()) Refresh();
        else Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Background);
    }

    public void Refresh()
    {
        if (controller is null) return;
        SubTabSetupButton.IsChecked = controller.ActiveSubTab == SweepSubTab.ExperimentSetup;
        SubTabRunButton.IsChecked = controller.ActiveSubTab == SweepSubTab.LocalRun;
        SubTabResultsButton.IsChecked = controller.ActiveSubTab == SweepSubTab.Results;

        SetupView.IsVisible = controller.ActiveSubTab == SweepSubTab.ExperimentSetup;
        RunView.IsVisible = controller.ActiveSubTab == SweepSubTab.LocalRun;
        ResultsView.IsVisible = controller.ActiveSubTab == SweepSubTab.Results;

        syncingPicker = true;
        try
        {
            var list = controller.Experiments;
            ExperimentPicker.Items.Clear();
            string? selectedId = controller.SelectedExperiment?.Experiment.Id;
            int selIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var exp = list[i].Experiment;
                ExperimentPicker.Items.Add(new ComboBoxItem { Content = $"{exp.Name} (v{exp.Version})", Tag = exp.Id });
                if (exp.Id == selectedId) selIndex = i;
            }
            ExperimentPicker.SelectedIndex = selIndex;
            ExperimentPicker.IsEnabled = list.Count > 0;
        }
        finally { syncingPicker = false; }

        SetupView.Refresh();
        RunView.Refresh();
        ResultsView.Refresh();
    }
}
