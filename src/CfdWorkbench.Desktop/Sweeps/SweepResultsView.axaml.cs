using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Sweeps;

public partial class SweepResultsView : UserControl
{
    private SweepController? controller;

    public SweepResultsView()
    {
        InitializeComponent();
        MetricClButton.Click += (_, _) => controller?.SetMetric(SweepCurveMetric.Cl);
        MetricCdButton.Click += (_, _) => controller?.SetMetric(SweepCurveMetric.CdWing);
        MetricLdButton.Click += (_, _) => controller?.SetMetric(SweepCurveMetric.LdWing);
        MetricCmButton.Click += (_, _) => controller?.SetMetric(SweepCurveMetric.Cm);
        AxisAlphaButton.Click += (_, _) => controller?.SetXAxis(SweepXAxis.AlphaDeg);
        AxisSpeedButton.Click += (_, _) => controller?.SetXAxis(SweepXAxis.Speed);
        ShowTableTwinButton.Click += (_, _) => controller?.SetShowTableTwin(ShowTableTwinButton.IsChecked == true);
        NewVersionFromResultsButton.Click += (_, _) => controller?.NewVersionFromSelected();
        ArchiveSweepButton.Click += (_, _) => controller?.ArchiveSelected();
        InspectorRetryButton.Click += (_, _) => controller?.SetSubTab(SweepSubTab.LocalRun);
        CurveChart.CaseSelected += idx => controller?.SelectCase(idx);
    }

    public PerformanceCurveChart Chart => CurveChart;

    public void Bind(SweepController? sweepController)
    {
        controller = sweepController;
        Refresh();
    }

    public void Refresh()
    {
        SmallMultiplesStrip.Children.Clear();
        TableTwinRowsHost.Children.Clear();
        InspectorScalarsHost.Children.Clear();

        if (controller is null || controller.CurrentProjection() is not { } proj)
        {
            FreshnessBanner.IsVisible = false;
            CurveChart.Update(null, -1);
            TableTwinPanel.IsVisible = false;
            InspectorHeading.Text = "Case inspector";
            InspectorFreshnessText.Text = Labels.SweepEmpty;
            InspectorGapBox.IsVisible = false;
            InspectorBasisText.Text = "";
            InspectorNotModelledText.Text = "";
            InspectorGoalBandText.Text = "";
            return;
        }

        var exp = proj.Stored.Experiment;
        MetricClButton.IsChecked = controller.ActiveMetric == SweepCurveMetric.Cl;
        MetricCdButton.IsChecked = controller.ActiveMetric == SweepCurveMetric.CdWing;
        MetricLdButton.IsChecked = controller.ActiveMetric == SweepCurveMetric.LdWing;
        MetricCmButton.IsChecked = controller.ActiveMetric == SweepCurveMetric.Cm;
        AxisAlphaButton.IsChecked = controller.ActiveXAxis == SweepXAxis.AlphaDeg;
        AxisSpeedButton.IsChecked = controller.ActiveXAxis == SweepXAxis.Speed;
        ShowTableTwinButton.IsChecked = controller.ShowTableTwin;
        TableTwinPanel.IsVisible = controller.ShowTableTwin;

        bool showBanner = proj.IsHistorical || exp.IsArchived || proj.Stored.Integrity != SweepIntegrity.Intact;
        FreshnessBanner.IsVisible = showBanner;
        FreshnessBannerText.Text = proj.FreshnessBanner;
        ArchiveSweepButton.IsEnabled = !exp.IsArchived && !controller.IsRunning;

        var chartData = proj.Chart(controller.ActiveMetric, controller.ActiveXAxis, controller.DisplayUnits, controller.GoalSnapshot);
        int selectedIdx = controller.SelectedCaseIndex >= 0 && controller.SelectedCaseIndex < proj.Cases.Count
            ? controller.SelectedCaseIndex
            : (proj.Cases.Count > 0 ? 0 : -1);
        CurveChart.Update(chartData, selectedIdx);

        // Table Twin (UI-17, R2-18): aligned tabular Grid columns with pnl-head / pnl-cell (+tnum) and zero ASCII pipes.
        string unit = Labels.SpeedUnit(controller.DisplayUnits);
        const string tableCols = "28,76,54,60,68,68,68,72,72,66,*";
        TableTwinRowsHost.Children.Add(BuildTableRow(
            tableCols,
            isHeader: true,
            "#", $"Speed ({unit})", "α (°)", "CL", "CDi", "CD (N2)", "CD (N4)", "L/D (N2)", "L/D (N4)", "Cm", "Status / Freshness"));

        foreach (var row in chartData.TableRows)
        {
            int idx = row.CaseIndex;
            string speedText = Labels.Speed(row.SpeedMetersPerSecond, controller.DisplayUnits).ToString("0.##", CultureInfo.InvariantCulture);
            Grid rowGrid = row.IsGap
                ? BuildTableRow(
                    tableCols,
                    isHeader: false,
                    (idx + 1).ToString(CultureInfo.InvariantCulture),
                    speedText,
                    Fmt(row.AlphaDeg),
                    "GAP", "GAP", "GAP", "GAP", "GAP", "GAP", "GAP",
                    Labels.SweepGapAnnotation(row.FailureCode ?? row.State.ToString(), row.FailureReason ?? row.FreshnessText))
                : BuildTableRow(
                    tableCols,
                    isHeader: false,
                    (idx + 1).ToString(CultureInfo.InvariantCulture),
                    speedText,
                    Fmt(row.AlphaDeg),
                    Fmt(row.Cl, "0.###"),
                    Fmt(row.Cdi, "0.####"),
                    Fmt(row.CdWingNcrit2, "0.####"),
                    Fmt(row.CdWingNcrit4, "0.####"),
                    Fmt(row.LdWingNcrit2, "0.##"),
                    Fmt(row.LdWingNcrit4, "0.##"),
                    Fmt(row.Cm, "0.####"),
                    row.FreshnessText);

            var rowBtn = new ToggleButton
            {
                Content = rowGrid,
                IsChecked = idx == selectedIdx,
                Classes = { "sweep-table-row" }
            };
            rowBtn.Click += (_, _) => controller?.SelectCase(idx);
            TableTwinRowsHost.Children.Add(rowBtn);
        }

        // Small-multiples horizontal case strip
        foreach (var row in proj.Cases)
        {
            int idx = row.CaseIndex;
            string chipText = row.IsGap
                ? $"#{idx + 1} × {row.State}"
                : string.Create(CultureInfo.InvariantCulture, $"#{idx + 1} CL {Fmt(row.Cl, "0.##")}");
            var chip = new ToggleButton
            {
                Content = new TextBlock { Text = chipText, Classes = { "pnl-cell" } },
                IsChecked = idx == selectedIdx,
                Classes = { "sweep-chip" }
            };
            chip.Click += (_, _) => controller?.SelectCase(idx);
            SmallMultiplesStrip.Children.Add(chip);
        }

        // Synchronized Case Inspector
        InspectorBasisText.Text = proj.BasisLabel;
        InspectorNotModelledText.Text = proj.NotModelledLabel;
        InspectorGoalBandText.Text = chartData.GoalBandNote ?? "";

        if (selectedIdx >= 0 && selectedIdx < proj.Cases.Count)
        {
            var sel = proj.Cases[selectedIdx];
            InspectorHeading.Text = string.Create(CultureInfo.InvariantCulture,
                $"Case {sel.CaseIndex + 1} · {Labels.Speed(sel.SpeedMetersPerSecond, controller.DisplayUnits):0.##} {unit} · α {Fmt(sel.AlphaDeg)}°");
            InspectorFreshnessText.Text = sel.FreshnessText;

            if (sel.IsGap)
            {
                // ANA-08: selecting a failed/gap case clears numeric readouts and displays error reason + Retry jump.
                InspectorGapBox.IsVisible = true;
                InspectorGapReason.Text = Labels.SweepGapAnnotation(sel.FailureCode ?? sel.State.ToString(), sel.FailureReason ?? sel.FreshnessText);
            }
            else
            {
                InspectorGapBox.IsVisible = false;
                AddScalar("CL", Fmt(sel.Cl, "0.####"));
                AddScalar("CDi (Trefftz / Est.)", Fmt(sel.Cdi, "0.#####"));
                AddScalar("CD wing (Ncrit 2 / 4)", sel.CdWingNcrit2.HasValue
                    ? $"{Fmt(sel.CdWingNcrit2, "0.#####")} – {Fmt(sel.CdWingNcrit4, "0.#####")}"
                    : (sel.DragUnavailableReason ?? "Unavailable"));
                AddScalar("Wing-only L/D (Ncrit 2 / 4)", sel.LdWingNcrit2.HasValue
                    ? $"{Fmt(sel.LdWingNcrit2, "0.##")} – {Fmt(sel.LdWingNcrit4, "0.##")}"
                    : (sel.DragUnavailableReason ?? "Unavailable"));
                AddScalar("Cm (frame origin)", sel.Cm.HasValue
                    ? Fmt(sel.Cm, "0.####")
                    : (sel.CmUnavailableReason ?? "Unavailable"));
                AddScalar("Oswald e", Fmt(sel.OswaldE, "0.###"));
                AddScalar("h/c · Fr_h · σ", sel.Conditions.DepthOverChord.Value is double hc
                    ? string.Create(CultureInfo.InvariantCulture, $"{hc:0.##} · {sel.Conditions.FroudeDepth.Value:0.##} · {sel.Conditions.Sigma.Value:0.##}")
                    : Labels.DepthNotSet);
                AddScalar("Envelope", sel.EnvelopeVerdict);
            }
        }
    }

    private static Grid BuildTableRow(string colSpec, bool isHeader, params string[] values)
    {
        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(colSpec), ColumnSpacing = 6 };
        string cls = isHeader ? "pnl-head" : "pnl-cell";
        for (int i = 0; i < values.Length; i++)
        {
            bool numeric = i < values.Length - 1;
            var cell = new TextBlock
            {
                Text = values[i],
                Classes = { cls },
                TextAlignment = numeric ? Avalonia.Media.TextAlignment.Right : Avalonia.Media.TextAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }
        return grid;
    }

    private void AddScalar(string name, string value)
    {
        InspectorScalarsHost.Children.Add(new TextBlock
        {
            Text = $"{name}: {value}",
            Classes = { "pnl-cell" }
        });
    }

    private static string Fmt(double? value, string format = "0.##") =>
        value is double v ? v.ToString(format, CultureInfo.InvariantCulture) : "—";
}
