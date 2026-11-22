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

        // Table Twin (UI-17): exact plotted values including both Ncrit 2 and Ncrit 4, CDi, Cm, freshness, and failed-case reasons.
        string unit = controller.DisplayUnits == Units.Imperial ? "kn" : "m/s";
        TableTwinRowsHost.Children.Add(new TextBlock
        {
            Text = $"#  | Speed ({unit}) | α (°) | CL     | CDi     | CD (N2) | CD (N4) | L/D (N2) | L/D (N4) | Cm      | Status / Freshness",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 11
        });
        foreach (var row in chartData.TableRows)
        {
            int idx = row.CaseIndex;
            string line = row.IsGap
                ? string.Create(CultureInfo.InvariantCulture,
                    $"{idx + 1,2} | {Labels.Speed(row.SpeedMetersPerSecond, controller.DisplayUnits),10:0.##} | {Fmt(row.AlphaDeg),5} | GAP    | GAP     | GAP     | GAP     | GAP      | GAP      | GAP     | {Labels.SweepGapAnnotation(row.FailureCode ?? row.State.ToString(), row.FailureReason ?? row.FreshnessText)}")
                : string.Create(CultureInfo.InvariantCulture,
                    $"{idx + 1,2} | {Labels.Speed(row.SpeedMetersPerSecond, controller.DisplayUnits),10:0.##} | {Fmt(row.AlphaDeg),5} | {Fmt(row.Cl, "0.###"),6} | {Fmt(row.Cdi, "0.####"),7} | {Fmt(row.CdWingNcrit2, "0.####"),7} | {Fmt(row.CdWingNcrit4, "0.####"),7} | {Fmt(row.LdWingNcrit2, "0.##"),8} | {Fmt(row.LdWingNcrit4, "0.##"),8} | {Fmt(row.Cm, "0.####"),7} | {row.FreshnessText}");
            var rowBtn = new ToggleButton
            {
                Content = line,
                IsChecked = idx == selectedIdx,
                FontSize = 11,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left
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
            var chip = new ToggleButton { Content = chipText, IsChecked = idx == selectedIdx, FontSize = 11 };
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

    private void AddScalar(string name, string value)
    {
        InspectorScalarsHost.Children.Add(new TextBlock
        {
            Text = $"{name}: {value}",
            FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
    }

    private static string Fmt(double? value, string format = "0.##") =>
        value is double v ? v.ToString(format, CultureInfo.InvariantCulture) : "—";
}
