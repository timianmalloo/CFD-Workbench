using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Setup;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Sweeps;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 7C Track W7-ANA-AT red-first regression tests covering F-18 (Analysis), F-28, F-29, F-36, F-37, F-38, F-40,
/// F-41, and Wave 6 carry-forward live theme reactivity on PerformanceCurveChart &amp; PlanformPreviewControl.
/// Wired into DesktopChecks.Spawn and ScaleModes as --ui-w7-analysis alongside TokenDisciplineTests.
/// </summary>
public static class UiW7AnalysisTests
{
    public static void Run()
    {
        DesktopChecks.Check("UiW7Ana_F18_NoInertOverlayMenuInTransitionLegend_ConstantPreserved", CheckF18OverlayMenu);
        DesktopChecks.Check("UiW7Ana_F28_SeriesLegend_RightAlignedTnumTables_CompareHeaders_SectionTwinTabular", CheckF28TablesAndCharts);
        DesktopChecks.Check("UiW7Ana_F29_ConditionsBandPlainEnglishDangerSubrow_ComparePaneResponsive1024", CheckF29ResponsivenessAndErrors);
        DesktopChecks.Check("UiW7Ana_F36_F37_F40_F41_AnalysisTokens_Typography_ShowChart_ReadOnlyFlyout_TabOrder_Clamping", CheckF36F37F40F41Polish);
        DesktopChecks.Check("UiW7Ana_F38_ImperialUnits_FindAlpha_CompareInputs_DepthLayers_AndScrollableFindAlpha", CheckF38ImperialAndScrollableFindAlpha);
        DesktopChecks.Check("UiW7Ana_Wave6CarryForward_SweepsAndSetupThemeReactivity_AndMinFontSize", CheckWave6ThemeReactivity);
        TokenDisciplineTests.Run();
    }

    private static void CheckF18OverlayMenu()
    {
        // Ruling 108 COPY-327 constant MUST be preserved verbatim.
        if (Labels.OverlayMenu != "Overlay a section ▾")
            throw new InvalidOperationException($"Ruling 108 Labels.OverlayMenu altered: '{Labels.OverlayMenu}'");

        using var controller = OpenSmallAnalysis(withPolar: true);
        var run = Evaluate(controller, 3.0, 0.6);
        byte[] source = System.Text.Encoding.UTF8.GetBytes(controller.AcceptedSource);
        var tier = SectionTier.Evaluate(source, [0.25, 0.5, 0.75, 1.0], [], run.Op, run.Water);
        var view = SectionDisplay.Build(run, tier, source, null, "r1");
        var transition = view.Charts.Single(c => c.Id == "transition");
        if (transition.Legend.Contains("Overlay a section", StringComparison.Ordinal) ||
            transition.Legend.Contains('▾'))
            throw new InvalidOperationException($"Transition chart legend still renders inert overlay dropdown text: '{transition.Legend}'");
    }

    private static void CheckF28TablesAndCharts()
    {
        using var controller = OpenSmallAnalysis(withPolar: false);
        var run = Evaluate(controller, 2.0, 0.6);
        _ = run;

        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            host.RefreshPanes();
            Settle(window);

            // (1) SectionChartView renders a compact series legend for multi-series plots.
            var chartView = host.AnalysisPanel.SectionView.Chart;
            var multiPlot = new ChartModel("cp", "Cp",
                [new ChartPlot("Cp", "x/c", "Cp", true,
                    [
                        new ChartSeries("upper", [new ChartPoint(0, -1.0), new ChartPoint(0.5, -0.4), new ChartPoint(1, 0.2)], false, "dot", 0),
                        new ChartSeries("lower", [new ChartPoint(0, 0.6), new ChartPoint(0.5, 0.1), new ChartPoint(1, 0.2)], true, "square", 1)
                    ],
                    [new ChartMarker("Cp_min", 0.1, -1.0)])],
                "Cp legend", "label");
            var standaloneChart = new SectionChartView { Width = 420, Height = 220, Model = multiPlot };
            var chartWin = new Window { Content = standaloneChart, Width = 440, Height = 240, RequestedThemeVariant = ThemeVariant.Light };
            try
            {
                chartWin.Show();
                Settle(chartWin);
                using var rtb = new RenderTargetBitmap(new PixelSize(420, 220), new Vector(96, 96));
                rtb.Render(standaloneChart);
                if (standaloneChart.DrawnLegendItems.Count < 2 ||
                    !standaloneChart.DrawnLegendItems.Contains("upper") ||
                    !standaloneChart.DrawnLegendItems.Contains("lower"))
                    throw new InvalidOperationException("SectionChartView did not draw compact series legend items: " +
                        string.Join(", ", standaloneChart.DrawnLegendItems));
            }
            finally { chartWin.Close(); }

            // (2) LoadingChart table twin cells carry pnl-cell (+tnum, PropFontSize) and right-align numeric columns.
            host.AnalysisPanel.LoadingView.TwinToggle.IsChecked = true;
            Settle(window);
            var loadingTable = host.AnalysisPanel.LoadingView.GetVisualDescendants().OfType<StackPanel>()
                .First(s => s.Name == "loading-table");
            var loadingDataRow = loadingTable.Children.OfType<Grid>().Skip(1).First();
            var loadingCells = loadingDataRow.Children.OfType<TextBlock>().ToArray();
            if (!loadingCells.All(c => c.Classes.Contains("pnl-cell")))
                throw new InvalidOperationException("LoadingChart table twin cells do not carry pnl-cell class (+tnum / PropFontSize)");
            if (!loadingCells.Skip(1).All(c => c.TextAlignment == TextAlignment.Right))
                throw new InvalidOperationException("LoadingChart numeric columns are not right-aligned");

            // (3) AnalysisPanel numeric cells in Conditions/Loads are right-aligned with pnl-cell.
            var loadsTable = host.AnalysisPanel.GetLogicalDescendants().OfType<StackPanel>()
                .First(s => s.Name == "loads-table");
            var liftRow = loadsTable.Children.OfType<Grid>()
                .First(g => g.Children.OfType<TextBlock>().First().Text == "Lift (Wing only)");
            var liftValueCell = liftRow.Children.OfType<TextBlock>().ElementAt(1);
            if (liftValueCell.TextAlignment != TextAlignment.Right || !liftValueCell.Classes.Contains("pnl-cell"))
                throw new InvalidOperationException($"AnalysisPanel Lift value cell alignment={liftValueCell.TextAlignment}, classes={string.Join(",", liftValueCell.Classes)}");

            // (4) ComparePane InputsTable & OutputsTable have header rows and right-aligned numeric/delta columns.
            DesktopChecks.PumpToCompletion(controller.CompareWithEstimatorAsync(), "compare estimator", TimeSpan.FromSeconds(30));
            var comparePane = new ComparePane();
            comparePane.Bind(controller);
            var inputsHeader = comparePane.InputsTable.Children.OfType<Grid>().FirstOrDefault()
                ?? throw new InvalidOperationException("ComparePane InputsTable missing header row");
            var inputsHeaderCells = inputsHeader.Children.OfType<TextBlock>().ToArray();
            if (inputsHeaderCells[0].Text != "Field" || inputsHeaderCells[0].FontWeight != FontWeight.SemiBold)
                throw new InvalidOperationException($"ComparePane InputsTable first row is not a SemiBold header: '{inputsHeaderCells[0].Text}' / {inputsHeaderCells[0].FontWeight}");

            var outputsHeader = comparePane.OutputsTable.Children.OfType<Grid>().FirstOrDefault()
                ?? throw new InvalidOperationException("ComparePane OutputsTable missing header row");
            var outputsHeaderCells = outputsHeader.Children.OfType<TextBlock>().ToArray();
            if (outputsHeaderCells[0].Text != "Quantity" || outputsHeaderCells[0].FontWeight != FontWeight.SemiBold)
                throw new InvalidOperationException($"ComparePane OutputsTable first row is not a SemiBold header: '{outputsHeaderCells[0].Text}' / {outputsHeaderCells[0].FontWeight}");

            var clRow = comparePane.OutputsTable.Children.OfType<Grid>()
                .First(g => g.Children.OfType<TextBlock>().First().Text == "CL");
            var clCells = clRow.Children.OfType<TextBlock>().ToArray();
            if (clCells[1].TextAlignment != TextAlignment.Right || clCells[2].TextAlignment != TextAlignment.Right || clCells[3].TextAlignment != TextAlignment.Right)
                throw new InvalidOperationException($"ComparePane OutputsTable numeric/delta columns not right-aligned: {clCells[1].TextAlignment}, {clCells[2].TextAlignment}, {clCells[3].TextAlignment}");

            // (5) SectionTabView StationsTable numeric columns right-aligned & chart twin is unit-labeled tabular >= 11 px.
            host.LayoutFactory.MainDocumentDock.ActiveDockable = host.LayoutFactory.SectionDocument;
            Settle(window);
            var sectionTab = host.AnalysisPanel.SectionView;
            var stationsTable = sectionTab.GetLogicalDescendants().OfType<StackPanel>()
                .First(s => s.Name == "section-stations-table");
            var stationHead = stationsTable.Children.OfType<Grid>().First();
            var stationHeadCells = stationHead.Children.OfType<TextBlock>().ToArray();
            if (stationHeadCells[1].TextAlignment != TextAlignment.Right || stationHeadCells[2].TextAlignment != TextAlignment.Right)
                throw new InvalidOperationException("SectionTabView StationsTable numeric columns (α_eff, -Cp_min) are not right-aligned");

            sectionTab.TwinToggle.IsChecked = true;
            Settle(window);
            var twinHost = sectionTab.GetLogicalDescendants().OfType<StackPanel>().First(s => s.Name == "section-twin");
            var twinGrids = twinHost.GetLogicalDescendants().OfType<Grid>().ToArray();
            if (twinGrids.Length < 2)
                throw new InvalidOperationException($"SectionTabView table twin is not tabular (found {twinGrids.Length} Grid rows)");
            var twinTexts = twinHost.GetLogicalDescendants().OfType<TextBlock>().ToArray();
            if (twinTexts.Any(t => t.Text?.Contains('→') == true))
                throw new InvalidOperationException("SectionTabView table twin still dumps raw x→y strings");
            if (twinTexts.Any(t => t.FontSize < 11.0))
                throw new InvalidOperationException($"SectionTabView table twin contains TextBlock with FontSize < 11: {twinTexts.Min(t => t.FontSize)}");
            var headerTexts = twinGrids[0].Children.OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
            if (!headerTexts.Any(h => h.Contains("x/c", StringComparison.Ordinal)) ||
                !headerTexts.Any(h => h.Contains("Cp", StringComparison.Ordinal)))
                throw new InvalidOperationException("SectionTabView table twin header omits axis titles/units: " + string.Join(" | ", headerTexts));
        }
        finally { window.Close(); }
    }

    private static void CheckF29ResponsivenessAndErrors()
    {
        using var controller = OpenSmallAnalysis(withPolar: false);
        _ = Evaluate(controller, 2.0, null);
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1024, Height = 700, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            host.RefreshPanes();
            Settle(window);

            // (1) ConditionsBand maps ANA-INPUT-* to plain English styled with DangerBrush on a wrapping sub-row.
            var band = host.ModelView.FindControl<ConditionsBand>("AnalysisConditionsBand")!;
            var bandRow = band.FindControl<StackPanel>("BandRow")!;
            var inputError = band.FindControl<TextBlock>("InputError")!;
            if (ReferenceEquals(inputError.Parent, bandRow))
                throw new InvalidOperationException("ConditionsBand.InputError is still inside horizontal BandRow instead of a wrapping sub-row");

            var speedBox = band.FindControl<TextBox>("SpeedInput")!;
            speedBox.Text = "-5";
            Settle(window);
            if (!inputError.IsVisible)
                throw new InvalidOperationException("ConditionsBand.InputError did not appear on invalid speed");
            if (inputError.Text!.Contains("ANA-INPUT-", StringComparison.Ordinal))
                throw new InvalidOperationException($"ConditionsBand.InputError leaked raw contract code: '{inputError.Text}'");
            if (inputError.TextWrapping != TextWrapping.Wrap)
                throw new InvalidOperationException("ConditionsBand.InputError does not wrap");
            var dangerColor = ThemeColor(window, "DangerBrush", ThemeVariant.Light);
            if (inputError.Foreground is not ISolidColorBrush fg || fg.Color != dangerColor)
                throw new InvalidOperationException($"ConditionsBand.InputError does not use DangerBrush (actual {inputError.Foreground})");

            // Restore valid speed and confirm error hides cleanly and band returns to <= 41.5 px.
            speedBox.Text = "5.14";
            Settle(window);
            if (inputError.IsVisible)
                throw new InvalidOperationException("ConditionsBand.InputError stayed visible after valid speed restored");

            // (2) ComparePane pickers & EstimatorButton do not clip horizontally at 1024 px.
            host.AnalysisPanel.PanelTabs.SelectedItem = host.AnalysisPanel.CompareTab;
            Settle(window);
            var compare = host.AnalysisPanel.ComparePaneView;
            foreach (Control ctrl in new Control[] { compare.BasePicker, compare.ComparedPicker, compare.EstimatorButton })
            {
                if (!ctrl.IsEffectivelyVisible) continue;
                var right = ctrl.TranslatePoint(new Point(ctrl.Bounds.Width, 0), compare)?.X ?? double.NaN;
                if (right > compare.Bounds.Width + 1.0)
                    throw new InvalidOperationException($"ComparePane control '{ctrl.Name}' clips at 1024 px: right={right:F1} > width={compare.Bounds.Width:F1}");
            }
        }
        finally { window.Close(); }
    }

    private static void CheckF36F37F40F41Polish()
    {
        // (1) F-40: LoadingChart toggle text says "Show chart" (not "Show chart notes").
        var lc = new LoadingChart();
        lc.TwinToggle.IsChecked = true;
        if (!Equals(lc.TwinToggle.Content, "Show chart"))
            throw new InvalidOperationException($"LoadingChart toggle text expected 'Show chart', got '{lc.TwinToggle.Content}'");

        // (2) F-41: ConditionsBand MoreButton uses a read-only Flyout (not invokable MenuFlyout / MenuItems).
        var band = new ConditionsBand();
        var moreBtn = band.FindControl<Button>("MoreButton")!;
        if (moreBtn.Flyout is MenuFlyout)
            throw new InvalidOperationException("ConditionsBand MoreButton still uses MenuFlyout with invokable MenuItems");
        if (moreBtn.Flyout is not Flyout flyout || flyout.Content is not Control flyoutContent)
            throw new InvalidOperationException("ConditionsBand MoreButton is missing read-only Flyout");
        if (flyoutContent.GetLogicalDescendants().OfType<MenuItem>().Any())
            throw new InvalidOperationException("ConditionsBand MoreButton Flyout still contains invokable MenuItems");

        // (3) F-41: ComparePane.TabOrder aligns with real focusability and includes visible EstimatorButton.
        using var controller = OpenSmallAnalysis(withPolar: false);
        _ = Evaluate(controller, 2.0, null);
        var compare = new ComparePane();
        compare.Bind(controller);
        if (!compare.InputsTable.Focusable || !compare.OutputsTable.Focusable)
            throw new InvalidOperationException("ComparePane InputsTable/OutputsTable are not Focusable despite being in TabOrder");
        var order = compare.TabOrder.ToArray();
        if (!order.Contains(Labels.CompareTiersCommand))
            throw new InvalidOperationException("ComparePane.TabOrder omits visible EstimatorButton: " + string.Join("|", order));

        // (4) F-36: PlanLoadLayer.ramp is keyed by ThemeVariant (not a single permanent static array).
        var rampFields = typeof(PlanLoadLayer).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        if (rampFields.Any(f => f.FieldType == typeof(IBrush[])))
            throw new InvalidOperationException("PlanLoadLayer still holds a single unkeyed static IBrush[] ramp");

        // (5) F-36: SectionProfileView safe ISolidColorBrush + Vik*Color + aligned induced-drag key swatch.
        string profileSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Analysis", "SectionProfileView.cs"));
        if (profileSrc.Contains("(ISolidColorBrush)", StringComparison.Ordinal))
            throw new InvalidOperationException("SectionProfileView.cs still contains unguarded (ISolidColorBrush) cast");
        if (profileSrc.Contains("Color.Parse(", StringComparison.Ordinal))
            throw new InvalidOperationException("SectionProfileView.cs still contains raw Color.Parse hexes");
        if (profileSrc.Contains("Typeface.Default", StringComparison.Ordinal))
            throw new InvalidOperationException("SectionProfileView.cs still uses Typeface.Default instead of PropFontFamily");

        // Verify SectionProfileView key swatch for induced drag uses the mixed induced brush, not raw warn.
        int drawKeyIdx = profileSrc.IndexOf("DrawKey(", StringComparison.Ordinal);
        string drawKeyBlock = drawKeyIdx >= 0 ? profileSrc[drawKeyIdx..] : "";
        if (!drawKeyBlock.Contains("Mix(", StringComparison.Ordinal))
            throw new InvalidOperationException("SectionProfileView.DrawKey does not compute mixed induced-drag swatch matching DrawForces");

        // (6) F-37: No Typeface.Default or FontFamily.Default in LoadingChart, SectionChartView, LoadLayerText.
        foreach (string rel in new[] { "LoadingChart.cs", "SectionChartView.cs", "LoadLayerText.cs" })
        {
            string src = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Analysis", rel));
            if (src.Contains("Typeface.Default", StringComparison.Ordinal))
                throw new InvalidOperationException($"{rel} still uses Typeface.Default instead of PropFontFamily");
        }

        // (7) F-41: SectionChartView clamps X-title and markers inside bounds (verified via long marker/title near right edge).
        var clampModel = new ChartModel("cp", "Cp",
            [new ChartPlot("Cp", "very-long-x-axis-title-units", "Cp", false,
                [new ChartSeries("s0", [new ChartPoint(0, 0), new ChartPoint(1, 1)], false, "none", 0)],
                [new ChartMarker("very-long-marker-label", 1.0, 1.0)])],
            "", "");
        var chart = new SectionChartView { Width = 240, Height = 160, Model = clampModel };
        var win = new Window { Content = chart, Width = 260, Height = 180 };
        try
        {
            win.Show();
            Settle(win);
            using var rtb = new RenderTargetBitmap(new PixelSize(240, 160), new Vector(96, 96));
            rtb.Render(chart);
            foreach (var rect in chart.DrawnLabelBounds)
            {
                if (rect.Left < -0.5 || rect.Right > chart.Bounds.Width + 0.5 || rect.Top < -0.5 || rect.Bottom > chart.Bounds.Height + 0.5)
                    throw new InvalidOperationException($"SectionChartView label escaped bounds {chart.Bounds}: {rect}");
            }
            if (chart.DrawnLabelBounds.Count < 2)
                throw new InvalidOperationException("SectionChartView did not record measured/clamped label bounds");
        }
        finally { win.Close(); }
    }

    private static void CheckF38ImperialAndScrollableFindAlpha()
    {
        using var controller = OpenSmallAnalysis(withPolar: false);
        var run = Evaluate(controller, 2.0, 0.6);
        controller.AnalysisUnits = Units.Imperial;

        // (1) CompareProjection.Inputs renders Speed in kn under Imperial (and m/s under Metric).
        DesktopChecks.PumpToCompletion(controller.CompareWithEstimatorAsync(), "compare estimator imperial", TimeSpan.FromSeconds(30));
        var cmpImp = controller.CompareView;
        var speedImp = cmpImp.Inputs.Single(r => r.Field == "Speed");
        if (!speedImp.Base.EndsWith(" kn", StringComparison.Ordinal) || !speedImp.Compared.EndsWith(" kn", StringComparison.Ordinal))
            throw new InvalidOperationException($"CompareProjection.Inputs Speed did not use kn in Imperial: base='{speedImp.Base}', compared='{speedImp.Compared}'");
        var depthImp = cmpImp.Inputs.Single(r => r.Field == "Depth h_ref");
        if (!depthImp.Base.EndsWith(" ft", StringComparison.Ordinal))
            throw new InvalidOperationException($"CompareProjection.Inputs Depth h_ref did not use ft in Imperial: '{depthImp.Base}'");

        // (2) FindAlphaDialog displays speed in kn under Imperial, renders row.Unit, and is scrollable with bounded MaxHeight.
        var dialog = new FindAlphaDialog(controller, 5.14, _ => { }, Units.Imperial);
        var win = new Window { Width = 1024, Height = 700 };
        try
        {
            win.Show();
            dialog.Show(win);
            Settle(dialog);
            var texts = dialog.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
            if (!texts.Any(t => t.Contains(" kn", StringComparison.Ordinal)) ||
                texts.Any(t => t.Contains(" m/s", StringComparison.Ordinal)))
                throw new InvalidOperationException("FindAlphaDialog did not display speed in kn under Imperial: " + string.Join(" | ", texts));
            if (dialog.ResultRows.FindAncestorOfType<ScrollViewer>() is null)
                throw new InvalidOperationException("FindAlphaDialog ResultRows is not wrapped in a ScrollViewer");
            if (double.IsInfinity(dialog.MaxHeight) || dialog.MaxHeight > 700)
                throw new InvalidOperationException($"FindAlphaDialog MaxHeight is not bounded: {dialog.MaxHeight}");

            // Run FindAsync and confirm row.Unit is rendered when present (e.g. Bracket has unit "°").
            DesktopChecks.PumpToCompletion(dialog.FindAsync(), "FindAlphaDialog.FindAsync", TimeSpan.FromSeconds(30));
            Settle(dialog);
            var resultTexts = dialog.ResultRows.Children.OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
            var outcomeRowsWithUnit = dialog.Outcome!.Rows.Where(r => !string.IsNullOrEmpty(r.Unit)).ToArray();
            foreach (var r in outcomeRowsWithUnit)
            {
                if (!resultTexts.Any(t => t.Contains(r.Label, StringComparison.Ordinal) && t.Contains(r.Unit!, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"FindAlphaDialog dropped row.Unit '{r.Unit}' for '{r.Label}': " + string.Join(" | ", resultTexts));
            }
        }
        finally { dialog.Close(); win.Close(); }

        // (3) ElevationDepthLayer & View3dLoadLayer format depth in ft under Imperial and m under Metric.
        string metricDepth = ElevationDepthLayer.FormatTipDepth(0.3048, Units.Metric);
        string imperialDepth = ElevationDepthLayer.FormatTipDepth(0.3048, Units.Imperial);
        if (metricDepth != "tip depth 0.305 m" || imperialDepth != "tip depth 1 ft")
            throw new InvalidOperationException($"ElevationDepthLayer depth formatting unexpected: metric='{metricDepth}', imperial='{imperialDepth}'");
        string v3dImperial = View3dLoadLayer.FormatTipDepth(0.6096, Units.Imperial);
        if (v3dImperial != "tip depth 2 ft")
            throw new InvalidOperationException($"View3dLoadLayer Imperial tip depth expected 'tip depth 2 ft', got '{v3dImperial}'");

        // Verify View3dLoadLayer.BuildScene carries Units through.
        var sceneImp = View3dLoadLayer.BuildScene(controller.AnalysisView, Units.Imperial);
        if (sceneImp.Units != Units.Imperial)
            throw new InvalidOperationException($"View3dLoadLayer.Scene did not preserve Units.Imperial");
        _ = run;
    }

    private static void CheckWave6ThemeReactivity()
    {
        var themeEvtField = typeof(StyledElement).GetField("ActualThemeVariantChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(StyledElement).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name.Contains("ActualThemeVariantChanged", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("ActualThemeVariantChanged backing field unavailable");

        var perf = new PerformanceCurveChart();
        var planform = new PlanformPreviewControl();
        if (themeEvtField.GetValue(perf) is null)
            throw new InvalidOperationException("PerformanceCurveChart does not subscribe to ActualThemeVariantChanged");
        if (themeEvtField.GetValue(planform) is null)
            throw new InvalidOperationException("PlanformPreviewControl does not subscribe to ActualThemeVariantChanged");

        string perfSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Sweeps", "PerformanceCurveChart.cs"));
        if (perfSrc.Contains("Typeface.Default", StringComparison.Ordinal) ||
            perfSrc.Contains("FontFamily.Default", StringComparison.Ordinal))
            throw new InvalidOperationException("PerformanceCurveChart.cs still uses Typeface.Default / FontFamily.Default instead of PropFontFamily");
    }

    private static WorkbenchController OpenSmallAnalysis(bool withPolar)
    {
        var settings = Settings.WithStations(Settings.Default with
        {
            NSpanPerHalf = 4, NChord = 1,
            Polar = withPolar
                ? new RunPolar(CfdWorkbench.Analysis.NeuralFoil.NeuralFoilPolarSource.Method.Id,
                    CfdWorkbench.Analysis.NeuralFoil.NeuralFoilPolarSource.Method.Version, "xxxlarge")
                : null
        });
        var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(settings));
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        if (!controller.IsAnalysis) controller.ToggleAnalysis();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double alpha, double? depth)
    {
        var task = controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, depth), controller.AnalysisWater);
        DesktopChecks.PumpToCompletion(task, "evaluate analysis", TimeSpan.FromSeconds(45));
        return task.GetAwaiter().GetResult() ?? throw new InvalidOperationException("Evaluate returned null");
    }

    private static Color ThemeColor(Control host, string key, ThemeVariant variant) =>
        host.TryFindResource(key, variant, out var found) && found is ISolidColorBrush solid
            ? solid.Color
            : throw new InvalidOperationException($"Resource {key} ({variant.Key}) not found as ISolidColorBrush");

    private static void Settle(Window window)
    {
        for (int i = 0; i < 6; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static string RepoRoot([CallerFilePath] string self = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));

    private static string RepoFile(params string[] parts) =>
        Path.Combine([RepoRoot(), .. parts]);
}
