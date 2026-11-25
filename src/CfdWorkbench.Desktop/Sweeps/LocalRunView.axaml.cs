using System.Globalization;
using Avalonia.Controls;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Sweeps;

public partial class LocalRunView : UserControl
{
    private SweepController? controller;

    public LocalRunView()
    {
        InitializeComponent();
        RunLocalButton.Click += (_, _) => { if (controller is not null) _ = controller.RunSelectedAsync(); };
        CancelRunButton.Click += (_, _) => controller?.CancelRun();
        OpenResultsButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.Results);
    }

    public void Bind(SweepController? sweepController)
    {
        controller = sweepController;
        Refresh();
    }

    public void Refresh()
    {
        CaseRowsHost.Children.Clear();
        if (controller is null || controller.CurrentProjection() is not { } proj)
        {
            RunTitleText.Text = "2. Local queue (in-process)";
            PinSummaryText.Text = "Queue a sweep in 1. Sweep setup to run it locally.";
            RunStatusBanner.Text = Labels.SweepEmpty;
            RunProgressBar.Value = 0;
            RunLocalButton.IsEnabled = false;
            CancelRunButton.IsEnabled = false;
            OpenResultsButton.IsEnabled = false;
            return;
        }

        var exp = proj.Stored.Experiment;
        var status = proj.Status;
        RunTitleText.Text = $"{exp.Name} · {status.BadgeText}";
        PinSummaryText.Text = exp.BackendPin is { } pin
            ? $"Pinned backend: {pin.Id} · {pin.Substrate} ({pin.Tier} · {pin.MethodId} {pin.MethodVersion})"
            : "Unpinned draft";
        RunStatusBanner.Text = controller.IsRunning && controller.LiveProgress is { } live ? live.Message : status.BannerText;

        int done = status.Completed + status.Failed;
        RunProgressBar.Maximum = Math.Max(1, status.Total);
        RunProgressBar.Value = done;

        bool canRun = !controller.IsRunning && exp.IsQueued && !exp.IsArchived && status.Completed < status.Total;
        RunLocalButton.IsEnabled = canRun;
        RunLocalButton.Content = status.Completed > 0 && status.Completed < status.Total ? "Resume / Retry remaining" : "Run locally";
        CancelRunButton.IsEnabled = controller.IsRunning;
        OpenResultsButton.IsEnabled = status.Completed > 0 || status.Failed > 0;

        foreach (var c in proj.Cases)
        {
            int idx = c.CaseIndex;
            var rowGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto"), ColumnSpacing = 12 };
            string unit = Labels.SpeedUnit(controller.DisplayUnits);
            string condText = c.AlphaDeg is double a
                ? string.Create(CultureInfo.InvariantCulture, $"Case {idx + 1} · {Labels.Speed(c.SpeedMetersPerSecond, controller.DisplayUnits):0.##} {unit} · α {a:0.##}°")
                : string.Create(CultureInfo.InvariantCulture, $"Case {idx + 1} · {c.Label ?? "Goal"} · {Labels.Speed(c.SpeedMetersPerSecond, controller.DisplayUnits):0.##} {unit}");
            string detail = c.State == SweepAttemptState.Completed
                ? $"Completed (att {c.AttemptCount}) · key {c.RunKey?[..Math.Min(12, c.RunKey.Length)]}"
                : c.FailureCode is not null
                    ? $"{c.State} ({c.FailureCode}): {c.FailureReason}"
                    : c.State.ToString();

            var label = new TextBlock { Text = condText, Classes = { "pnl-head" }, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var stateText = new TextBlock { Text = detail, Classes = { "pnl-cell" }, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetColumn(label, 0);
            Grid.SetColumn(stateText, 1);
            rowGrid.Children.Add(label);
            rowGrid.Children.Add(stateText);

            if (c.State is SweepAttemptState.Failed or SweepAttemptState.Cancelled && !exp.IsArchived)
            {
                var retryBtn = new Button { Content = "Retry sample", IsEnabled = !controller.IsRunning };
                retryBtn.Click += (_, _) => { if (controller is not null) _ = controller.RetrySampleAsync(idx); };
                Grid.SetColumn(retryBtn, 3);
                rowGrid.Children.Add(retryBtn);
            }
            CaseRowsHost.Children.Add(rowGrid);
        }
    }
}
