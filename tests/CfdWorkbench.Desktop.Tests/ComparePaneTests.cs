using Avalonia.Controls;
using Avalonia.LogicalTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// A3d-2 (design a3d-compare.md 5, 7.5): the Compare tab of the Analysis panel, rendered from the controller's Compare view.
/// Every check reads its copy through the Labels constant. Real VLM runs on a small lattice and the real estimator.
/// </summary>
public static class ComparePaneTests
{
    public static void Run()
    {
        DesktopChecks.Check("AnalysisPanel_ReadsOnlyVlmRuns_AfterEstimatorRunRecorded", () =>
        {
            using var controller = Open();
            var vlm = Evaluate(controller, 2);
            Task.Run(() => controller.CompareWithEstimatorAsync()).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            Equal(true, controller.CompareRuns.Runs.Any(stored => stored.Run.Tier == WingEstimator.TierId), "an estimator run is recorded;");
            var view = controller.AnalysisView;
            Equal(vlm.RunKey, view.RunKey, "the panel still shows the VLM run");
            Equal(true, view.Groups.First().Rows.First(row => row.Label == "Tier").Value == Labels.VlmChip, "labelled as the VLM tier;");
            Equal(false, view.Groups.First().Rows.First(row => row.Label == "CL").Value == "0.000", "its CL is the VLM's, not an estimator row's zero strips;");
        });
        DesktopChecks.Check("ComparePane_Commands_RowsInViewMenuWithLabelsCopy", () =>
        {
            var rows = CfdWorkbench.Desktop.Shell.CommandTable.Rows;
            var compare = rows.Single(row => row.Id == "analysis.compare");
            var tiers = rows.Single(row => row.Id == "analysis.compare-tiers");
            Equal(Labels.CompareRunsCommand, compare.Title, "Compare runs… title");
            Equal(Labels.CompareTiersCommand, tiers.Title, "Compare with estimator title");
            Equal(true, compare.Menu == "View" && tiers.Menu == "View" && compare.Gesture is null && tiers.Gesture is null, "View group, no keyboard gesture;");
            using var controller = Open();
            Equal(false, controller.CanCompare, "disabled with no Completed VLM run;");
            _ = Evaluate(controller, 2);
            Equal(true, controller.CanCompare, "enabled in Analysis with a selected Completed run;");
        });
        DesktopChecks.Check("ComparePane_Compatible_RendersTablesAndHeadline", () =>
        {
            using var controller = Open();
            _ = Evaluate(controller, 2);
            var pane = TierPane(controller);
            var text = AllText(pane);
            Equal(true, text.Any(t => t.StartsWith("Tiers disagree: δ = ", StringComparison.Ordinal) && t.Contains(Labels.NoUncertainty, StringComparison.Ordinal)), "the tier headline;");
            Equal(true, text.Contains(Labels.RecordStored), "the record status;");
            Equal(true, pane.InputsTable.Children.Count > 15 && pane.OutputsTable.Children.Count >= 4, "both tables are drawn;");
            Equal(true, text.Contains(Labels.CompareFootnote), "the footnote;");
            // The headline's delta is CL's: the same text the shown CL row carries.
            string delta = pane.OutputsTable.Children.OfType<Grid>().First(row => ((TextBlock)row.Children[0]).Text == "CL").Children.OfType<TextBlock>().ElementAt(3).Text!;
            Equal(true, text.Any(t => t.Contains(delta, StringComparison.Ordinal) && t.StartsWith("Tiers", StringComparison.Ordinal)), "the headline quotes the CL delta, not CDi's;");
        });
        DesktopChecks.Check("ComparePane_Envelope_SideBySide", () =>
        {
            using var controller = Open();
            _ = Evaluate(controller, 2);
            var pane = TierPane(controller);
            var row = Row(pane.InputsTable, "Envelope");
            Equal(true, row[1].Contains("vlm-envelope", StringComparison.Ordinal) && row[2].Contains("estimator-envelope", StringComparison.Ordinal), "both envelopes are shown: " + string.Join(" | ", row));
        });
        DesktopChecks.Check("ComparePane_EstimatorAfterTwistEdit_RunsOnTheBasesRevision", () =>
        {
            using var controller = Open();
            var vlm = Evaluate(controller, 2);
            controller.ToggleAnalysis();   // points are edited in CAD
            var added = Task.Run(() => controller.ApplyPointCommandAsync(new PointCommand.AddPoint("twist", 0.45))).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (added is not CommitOutcome.Committed) throw new Exception("Fixture twist edit was refused.");
            controller.ToggleAnalysis();
            var pane = TierPane(controller);
            var estimator = controller.CompareRuns.Runs.Single(stored => stored.Run.Tier == WingEstimator.TierId).Run;
            Equal(vlm.Inputs.AcceptedId, estimator.Inputs.AcceptedId, "the estimator run is on the base run's revision;");
            Equal(true, AllText(pane).Any(t => t.StartsWith("Tiers disagree", StringComparison.Ordinal)), "the tier headline, as the pair shares one revision;");
        });
        DesktopChecks.Check("ComparePane_NoSecondRun_EmptyState", () =>
        {
            using var controller = Open();
            _ = Evaluate(controller, 2);
            controller.OpenCompare();
            var pane = new ComparePane();
            pane.Bind(controller);
            var text = AllText(pane);
            Equal(true, text.Contains(Labels.NeedsTwoRuns), "the empty-state copy;");
            Equal(true, pane.EstimatorButton.IsVisible && pane.EstimatorButton.IsEnabled && Equals(pane.EstimatorButton.Content, Labels.CompareTiersCommand), "the estimator button, enabled for a VLM run;");
            Equal(0, pane.OutputsTable.Children.Count, "no table twin of an empty table;");
        });
        DesktopChecks.Check("ComparePane_ReferenceDiffers_DimensionalDeltasOnly", () =>
        {
            using var controller = Open();
            var first = Evaluate(controller, 2);
            var second = SpanEditedRun(controller, 2);
            controller.SetCompareRuns(first.RunId, second.RunId);
            var pane = new ComparePane();
            pane.Bind(controller);
            var text = AllText(pane);
            Equal(true, text.Any(t => t.StartsWith("Coefficient Δ blocked — reference quantities differ: ", StringComparison.Ordinal)), "AF-COPY-1;");
            Equal(true, text.Any(t => t.StartsWith("Revisions compared: δ L = ", StringComparison.Ordinal) && t.EndsWith("reference quantities differ", StringComparison.Ordinal)), "AF-COPY-5;");
            Equal(true, text.Contains(Labels.DimensionalNotRecorded), "the not-recorded status line;");
            Equal(false, text.Contains(Labels.RecordStored) || text.Contains(Labels.RecordRead), "no 'recorded' status;");
            string[] Cells(string quantity) => pane.OutputsTable.Children.OfType<Grid>()
                .Select(grid => grid.Children.OfType<TextBlock>().Select(block => block.Text ?? "").ToArray()).First(cells => cells[0] == quantity);
            foreach (string coefficient in new[] { "CL", "CDi" }) Equal(Labels.ReferenceDiffersCell, Cells(coefficient)[3], coefficient + " delta cell (AF-COPY-2)");
            Equal(Labels.ENotComparedSpan, Cells("e")[3], "e delta cell (AF-COPY-3)");
            foreach (string load in new[] { "L", "D_i" })
            {
                Equal(true, Cells(load)[3].EndsWith(" %", StringComparison.Ordinal) && !Cells(load)[3].StartsWith("Blocked", StringComparison.Ordinal), load + " carries a percentage delta;");
                Equal(Labels.DimensionalNote, Cells(load)[4], load + " note (AF-COPY-4)");
            }
            Equal(false, pane.OutputsTable.Children.OfType<Grid>().Where(grid => ((TextBlock)grid.Children[0]).Text is "CL" or "CDi" or "e")
                .Any(grid => ((TextBlock)grid.Children[3]).Text!.Contains(" %", StringComparison.Ordinal)), "no percentage in a coefficient cell;");
            Equal(true, Row(pane.InputsTable, "S_ref")[1].StartsWith("≠ ", StringComparison.Ordinal), "the differing field is marked;");
            // assume: L from CL * q * S_ref is the near-field strip lift to rounding, on a real VLM run (design 12.2).
            foreach (var run in new[] { first, second })
            {
                double lift = Compare.Dimensional(run, Compare.Derivation(_ => null)(run)).L!.Value;
                double alpha = run.Op.AlphaDeg * Math.PI / 180;
                double strips = run.Strips.Sum(strip => -strip.Fx * Math.Sin(alpha) + strip.Fz * Math.Cos(alpha));
                Console.WriteLine($"INFO assume L from CL q S_ref: L {lift:R} N, strip lift {strips:R} N, relative difference {Math.Abs(lift - strips) / Math.Abs(strips):E2}");
                Equal(true, Math.Abs(lift - strips) <= 1e-12 * Math.Abs(strips), "L equals the strip lift to rounding;");
            }
        });
        DesktopChecks.Check("ComparePane_ReferenceAndWater_BlockedNoDelta", () =>
        {
            using var controller = Open();
            var first = Evaluate(controller, 2);
            controller.ToggleAnalysis();   // points and dimensions are edited in CAD
            var edited = Task.Run(() => controller.ApplySpanAsync("1400")).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
            if (edited is not CommitOutcome.Committed) throw new Exception("Fixture geometry edit was refused.");
            controller.ToggleAnalysis();
            var warm = Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, null), WaterTable.At(25, 0))).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()!;
            controller.SetCompareRuns(first.RunId, warm.RunId);
            var pane = new ComparePane();
            pane.Bind(controller);
            var text = AllText(pane);
            Equal(true, text.Any(t => t.StartsWith("Δ blocked — reference quantities differ: ", StringComparison.Ordinal)), "the reference block copy;");
            Equal(true, text.Any(t => t.StartsWith("Δ blocked — the runs use different water: ", StringComparison.Ordinal)), "the water block copy;");
            Equal(0, pane.OutputsTable.Children.Count, "no output row, so no delta cell;");
            Equal(false, text.Any(t => t.Contains(" %", StringComparison.Ordinal)), "no percentage anywhere;");
        });
        DesktopChecks.Check("ComparePane_DifferentWater_OwnReason", () =>
        {
            using var controller = Open();
            var first = Evaluate(controller, 2);
            var other = Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, null), WaterTable.At(25, 0))).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult()!;
            controller.SetCompareRuns(first.RunId, other.RunId);
            var pane = new ComparePane();
            pane.Bind(controller);
            var text = AllText(pane);
            Equal(true, text.Any(t => t.StartsWith("Δ blocked — the runs use different water: ", StringComparison.Ordinal)), "the water copy;");
            Equal(false, text.Any(t => t.StartsWith("Δ blocked — the runs are at different operating points", StringComparison.Ordinal)), "not the point copy;");
        });
        DesktopChecks.Check("ComparePane_HistoricalBase_ChipAndDelta", () =>
        {
            using var controller = Open();
            var vlm = Evaluate(controller, 2);
            controller.SetAnalysisConditions(OperatingPoints.Custom(5.14, 3, null), controller.AnalysisWater);
            controller.SetCompareRuns(vlm.RunId, null);
            var pane = TierPane(controller);
            var text = AllText(pane);
            Equal(true, text.Contains("Historical"), "the base column's Historical chip;");
            Equal(true, text.Contains(Labels.HistoricalCompareNote), "the Historical note;");
            Equal(true, text.Any(t => t.StartsWith("Tiers disagree: δ = ", StringComparison.Ordinal)), "the delta is still shown;");
        });
        DesktopChecks.Check("ComparePane_FailedRun_Refused", () =>
        {
            var method = new FailOnce(new ProductWingMethod(Small()));
            using var controller = Open(method);
            var ok = Evaluate(controller, 2);
            method.FailNext = true;
            var failed = Evaluate(controller, 3);
            Equal(true, failed.Outcome is RunOutcome.Failed, "fixture failed");
            var option = controller.CompareOptions.Single(item => item.RunId == failed.RunId);
            Equal(false, option.Enabled, "a Failed option is disabled;");
            controller.SetCompareRuns(failed.RunId, ok.RunId);
            Equal(CompareState.FailedRun, controller.CompareView.State, "state");
            var pane = new ComparePane();
            pane.Bind(controller);
            Equal(true, AllText(pane).Contains(Labels.FailedRunCompare), "the failed-run copy replaces the tables;");
            Equal(0, pane.OutputsTable.Children.Count, "no table");
        });
        DesktopChecks.Check("ComparePane_EstimatorE_NotCompared_CdiLabelled", () =>
        {
            using var controller = Open();
            _ = Evaluate(controller, 2);
            var pane = TierPane(controller);
            Equal(true, Row(pane.OutputsTable, "e")[3] == Labels.EstimatorENotCompared, "the e cell: " + string.Join(" | ", Row(pane.OutputsTable, "e")));
            Equal(true, Row(pane.OutputsTable, "CDi")[3].EndsWith(Labels.EstimatorCdiNote, StringComparison.Ordinal), "the CDi cell carries the elliptic-loading label in the cell;");
        });
        DesktopChecks.Check("ComparePane_Error_NoDelta", () =>
        {
            using var controller = Open();
            var a = Evaluate(controller, 2);
            Task.Run(() => controller.CompareWithEstimatorAsync()).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            var estimator = controller.CompareRuns.Runs.Single(stored => stored.Run.Tier == WingEstimator.TierId).Run;
            var view = CompareProjection.Build(a, estimator, controller.CompareContextFor() with
                { ErrorReason = "the document is at its size limit", ErrorCode = "DOC-SIZE" });
            var pane = new ComparePane();
            pane.Render(view, controller.CompareOptions, a.RunId, estimator.RunId, false);
            var text = AllText(pane);
            Equal(true, text.Contains(Labels.NotRecorded("the document is at its size limit", "DOC-SIZE")), "the error card copy;");
            Equal(0, pane.OutputsTable.Children.Count, "no delta shown beside the error card;");
            Equal(false, text.Any(t => t.Contains(" %", StringComparison.Ordinal)), "no percentage;");
        });
        DesktopChecks.Check("ComparePane_KeyboardPath_FocusOrderAndNames", () =>
        {
            using var controller = Open();
            _ = Evaluate(controller, 2);
            controller.OpenCompare();
            var panel = new AnalysisPanel();
            var window = new Window { Content = panel, Width = 1100, Height = 300 };
            window.Show();
            try
            {
                panel.Bind(controller);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Equal(true, ReferenceEquals(panel.PanelTabs.SelectedItem, panel.CompareTab), "the command opens the Compare tab;");
                Equal(Labels.BaseRunLabel, Avalonia.Automation.AutomationProperties.GetName(panel.ComparePaneView.BasePicker), "Base picker name");
                Equal(Labels.ComparedRunLabel, Avalonia.Automation.AutomationProperties.GetName(panel.ComparePaneView.ComparedPicker), "Compared picker name");
                Equal("Compared inputs", Avalonia.Automation.AutomationProperties.GetName(panel.ComparePaneView.InputsTable), "inputs table name");
                Equal("Compared outputs", Avalonia.Automation.AutomationProperties.GetName(panel.ComparePaneView.OutputsTable), "outputs table name");
                Equal(true, panel.ComparePaneView.BasePicker.IsFocused, "focus lands on the Base picker;");
                var order = panel.ComparePaneView.TabOrder.ToArray();
                Equal("Base run|Compared run|Compared inputs|Compared outputs", string.Join("|", order), "tab order");
            }
            finally { window.Close(); }
        });
    }

    // ---- fixtures ----

    private static RunSettings Small() => Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };

    private static WorkbenchController Open(IWingMethod? method = null)
    {
        var controller = new WorkbenchController(analysisMethod: method ?? new ProductWingMethod(Small()));
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        if (!controller.IsAnalysis) controller.ToggleAnalysis();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double alpha) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, null), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult() ?? throw new Exception("Fixture Evaluate returned no run.");

    // A VLM run after a span edit: S_ref, b_ref and c_ref differ from the run before it; the point and the water do not.
    private static AnalysisRun SpanEditedRun(WorkbenchController controller, double alpha)
    {
        controller.ToggleAnalysis();   // points and dimensions are edited in CAD
        var edited = Task.Run(() => controller.ApplySpanAsync("1400")).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        if (edited is not CommitOutcome.Committed) throw new Exception("Fixture geometry edit was refused.");
        controller.ToggleAnalysis();
        return Evaluate(controller, alpha);
    }

    // The pane after "Compare with estimator" on the latest VLM run.
    private static ComparePane TierPane(WorkbenchController controller)
    {
        Task.Run(() => controller.CompareWithEstimatorAsync()).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        var pane = new ComparePane();
        pane.Bind(controller);
        return pane;
    }

    private static List<string> AllText(Control root) => root.GetLogicalDescendants().OfType<TextBlock>().Select(block => block.Text ?? "").ToList();

    private static string[] Row(StackPanel table, string field) => table.Children.OfType<Grid>()
        .Select(grid => grid.Children.OfType<TextBlock>().Select(block => block.Text ?? "").ToArray()).First(cells => cells[0] == field);

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{what} expected {expected}; actual {actual}");
    }

    private sealed class FailOnce(IWingMethod inner) : IWingMethod
    {
        public bool FailNext { get; set; }
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            if (FailNext) { FailNext = false; throw new ContractError("ANA-SOLVE-SINGULAR", "forced"); }
            return inner.Solve(sections, op, water, cancellation);
        }
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
            WaterRecord water, CancellationToken cancellation) => inner.Couple(sections, solution, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(byte[] source, IReadOnlyList<SectionSample> sections, LatticeSolution solution,
            OperatingPoint op, WaterRecord water, CancellationToken cancellation) => inner.Couple(source, sections, solution, op, water, cancellation);
    }
}
