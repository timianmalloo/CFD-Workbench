using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Sweeps;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Node SWP-4 red-first tests (T18 in docs/design/local-sweeps.md §8, Ruling 271):
/// No Run button in Experiment setup (XS-03 / UX-17), linked selection across chart/table-twin/small-multiples/inspector,
/// live Cancel from console and status strip (UX-21), non-bridged gaps in PerformanceCurveChart, theme-aware tokens
/// across Light/Dark/HighContrast, and 0 hits on the 7 LAB-01 forbidden words.
/// </summary>
public static class SweepViewTests
{
    private static readonly string[] Lab01Forbidden =
    [
        "cavitation-free", "ventilation-safe", "validated", "optimized", "best", "recommended", "certified"
    ];

    public static void Run()
    {
        DesktopChecks.Check("Desktop_Sweeps_NoRunButtonInSetup_LinkedSelection_Lab01Clean", T18_FullDesktopContract);
        DesktopChecks.Check("Desktop_Sweeps_LiveCancelFromStatusStripAndConsole", CancelFromStatusStrip);
        DesktopChecks.Check("Desktop_Sweeps_ThemesLightDarkHighContrast_RenderCleanly", ThemesRenderCleanly);
    }

    private static void T18_FullDesktopContract()
    {
        double v14 = 14.0 / Labels.KnotsPerMeterSecond;
        var baseMethod = new FastSweepMethod(Settings.WithStations(Settings.Default with { NSpanPerHalf = 12, NChord = 2 }));
        // Inject a failure on interior case (14 kn, alpha 2 deg) to prove non-bridged gap & inspector failure display!
        var method = new FaultingSweepMethod(baseMethod, op => Math.Abs(op.Speed - v14) < 1e-6 && Math.Abs(op.AlphaDeg - 2.0) < 1e-9);

        using var controller = new WorkbenchController(analysisMethod: method);
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        window.Show();
        try
        {
            OpenExample(controller);
            Flush();

            // 1. Switch to Sweeps via third navbar segment (CAD | Analysis | Sweeps — DR-SWP-6 (a)).
            var navSweeps = host.ModelView.FindControl<ToggleButton>("NavSweepsButton")
                ?? throw new InvalidOperationException("NavSweepsButton missing from ModelArea navbar");
            controller.EnterSweeps();
            Flush();
            Require(controller.IsSweeps, "Controller.IsSweeps must be true");
            Require(navSweeps.IsChecked == true, "NavSweepsButton must be checked");
            Require(host.ModelView.SweepsView.IsVisible, "SweepsWorkspaceView must be visible");

            var sweepsView = host.ModelView.SweepsView;
            var setupView = sweepsView.ExperimentSetup;
            var runView = sweepsView.LocalRun;
            var resultsView = sweepsView.Results;

            // 2. Empty state: Queue disabled, status strip shows "Sweep: none", Goal-state radio disabled.
            var queueBtn = setupView.FindControl<Button>("QueueButton")!;
            Require(!queueBtn.IsEnabled, "Queue must be disabled in empty state");
            var goalRadio = setupView.FindControl<RadioButton>("SourceGoalRadio")!;
            Require(!goalRadio.IsEnabled, "Goal-state radio must be disabled when no Goal state exists");
            var stripText = host.StatusStrip.FindControl<TextBlock>("SweepItemText")!;
            Require(stripText.Text == "Sweep: none", $"Expected 'Sweep: none' in status strip, got '{stripText.Text}'");

            // 3. XS-03 / UX-17: ZERO Run button in ExperimentSetupView (empty AND populated states)!
            AssertNoRunButton(setupView);

            // 4. Invalid input blocked: duplicate speed disables Queue and shows ValidationBanner.
            controller.Sweeps.NewSweep();
            controller.Sweeps.SpeedsText = "10, 14, 14";
            controller.Sweeps.RefreshPreview();
            Flush();
            Require(!queueBtn.IsEnabled, "Queue must be disabled on duplicate speed");
            var valBanner = setupView.FindControl<Border>("ValidationBanner")!;
            var valText = setupView.FindControl<TextBlock>("ValidationBannerText")!;
            Require(valBanner.IsVisible && (valText.Text?.Contains("14", StringComparison.Ordinal) == true),
                $"ValidationBanner must be visible naming 14, got '{valText.Text}'");

            // 5. Valid 3x3 grid -> Preview ready, wall time "Not recorded" (IO8), still ZERO Run button in setup!
            controller.Sweeps.SpeedsText = "10, 14, 18";
            controller.Sweeps.SpeedUnit = SweepSpeedUnit.Knots;
            controller.Sweeps.AnglesText = "0, 2, 4";
            controller.Sweeps.RefreshPreview();
            Flush();
            Require(queueBtn.IsEnabled, "Queue must be enabled on valid preview");
            var wallEst = setupView.FindControl<TextBlock>("EstimateWallText")!;
            Require(wallEst.Text == Labels.SweepEstimatedWallNotRecorded, $"Expected Not recorded wall estimate, got '{wallEst.Text}'");
            AssertNoRunButton(setupView);

            // 6. Queue immutable v1 -> Run locally -> verify 8/9 completed + 1 interior failed case.
            var queued = controller.Sweeps.Queue() ?? throw new InvalidOperationException("Queue failed");
            controller.Sweeps.SetSubTab(SweepSubTab.LocalRun);
            Flush();
            var outcome = WaitTask(controller.Sweeps.RunSelectedAsync())
                ?? throw new InvalidOperationException("RunSelectedAsync returned null");
            Flush();
            Require(outcome.CompletedCases == 8 && outcome.FailedCases == 1,
                $"Expected 8 completed / 1 failed, got {outcome.CompletedCases}/{outcome.FailedCases}");

            // 7. Results: non-bridged gap on interior failed case (case index 4: 14 kn, alpha 2 deg)!
            controller.Sweeps.SetSubTab(SweepSubTab.Results);
            Flush();
            // 3 speed series: 10 kn (1 seg), 14 kn (2 non-bridged segs around alpha 2 gap!), 18 kn (1 seg) = 4 segments total!
            Require(resultsView.Chart.RenderedGapCount == 1, $"Expected 1 gap marker, got {resultsView.Chart.RenderedGapCount}");
            Require(resultsView.Chart.RenderedSegmentCount == 4, $"Expected 4 non-bridged segments, got {resultsView.Chart.RenderedSegmentCount}");

            // 8. Linked selection across Chart, Table Twin, Small Multiples, and Case Inspector (ANA-08, UX-10).
            controller.Sweeps.SetShowTableTwin(true);
            controller.Sweeps.SelectCase(5);
            Flush();
            Require(resultsView.Chart.SelectedCaseIndex == 5, "Chart SelectedCaseIndex must be 5");
            var tableTwinPanel = resultsView.FindControl<Border>("TableTwinPanel")!;
            Require(tableTwinPanel.IsVisible, "TableTwinPanel must be visible when Show table is toggled on");
            var inspectorHeading = resultsView.FindControl<TextBlock>("InspectorHeading")!;
            Require(inspectorHeading.Text?.StartsWith("Case 6", StringComparison.Ordinal) == true,
                $"Inspector heading should show Case 6 for index 5, got '{inspectorHeading.Text}'");
            var gapBox = resultsView.FindControl<Border>("InspectorGapBox")!;
            Require(!gapBox.IsVisible, "Completed case 5 must not show gap box");

            // Selecting failed interior case 4 clears numeric scalars and shows failure reason + Retry jump!
            controller.Sweeps.SelectCase(4);
            Flush();
            var scalarsHost = resultsView.FindControl<StackPanel>("InspectorScalarsHost")!;
            Require(scalarsHost.Children.Count == 0, "Selecting failed case must clear numeric scalar readouts");
            Require(gapBox.IsVisible, "Selecting failed case must show InspectorGapBox");

            // 9. LAB-01 scan: ZERO forbidden words across all 22 Sweep Labels AND all rendered SweepsWorkspaceView text!
            CheckLab01Clean(CollectText(sweepsView), "SweepsWorkspaceView");
            string allCopy = string.Join(" | ",
                Labels.SweepEmpty, Labels.SweepSetupFooterNote, Labels.SweepDuplicateValue("Speeds", "14"),
                Labels.SweepInvalidValue("Speeds", "x"), Labels.SweepSpeedNonPositive("-5"),
                Labels.SweepCaseCapExceeded(121, 120), Labels.SweepPreviewOutOfDate("r1 → r2"),
                Labels.SweepEstimatedWallNotRecorded, Labels.SweepEstimatedStorage("1.5 MB", "2.0 MB"),
                Labels.SweepQueuedBanner("VLM + strip", 1), Labels.SweepRunningProgress(1, 12),
                Labels.SweepCancelledBanner(6, 12), Labels.SweepPartialCompletedBanner(11, 12, 1),
                Labels.SweepAllFailedBanner(12), Labels.SweepGapAnnotation("ANA-SOLVE-RESIDUAL", "limit"),
                Labels.SweepDocSizeStopped(5), Labels.SweepCmSubtitle, Labels.SweepCmUnavailableEstimator,
                Labels.SweepDragUnavailableEstimator, Labels.SweepNoGoalState, Labels.SweepArchivedBanner,
                Labels.SweepPrunedCaseCell);
            CheckLab01Clean(allCopy, "Labels.Sweep* (COPY-706..727)");
        }
        finally
        {
            window.Close();
        }
    }

    private static void CancelFromStatusStrip()
    {
        var method = new FastSweepMethod(Settings.WithStations(Settings.Default with { NSpanPerHalf = 12, NChord = 2 }));
        using var enteredBarrier = new ManualResetEventSlim(false);
        using var releaseBarrier = new ManualResetEventSlim(false);
        int count = 0;
        var barrier = new HoldBarrier((op, token) =>
        {
            if (Interlocked.Increment(ref count) == 2)
            {
                enteredBarrier.Set();
                while (!token.IsCancellationRequested && !releaseBarrier.IsSet)
                    Thread.Sleep(5);
                token.ThrowIfCancellationRequested();
            }
            return ValueTask.CompletedTask;
        });

        using var controller = new WorkbenchController(analysisMethod: method, analysisBarrier: barrier);
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        window.Show();
        try
        {
            OpenExample(controller);
            controller.EnterSweeps();
            controller.Sweeps.NewSweep();
            controller.Sweeps.SpeedsText = "10, 14";
            controller.Sweeps.AnglesText = "0, 2";
            controller.Sweeps.RefreshPreview();
            _ = controller.Sweeps.Queue() ?? throw new InvalidOperationException("Queue failed");
            controller.Sweeps.SetSubTab(SweepSubTab.LocalRun);
            Flush();

            var runTask = controller.Sweeps.RunSelectedAsync();
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!enteredBarrier.IsSet && DateTime.UtcNow < deadline) { Flush(); Thread.Sleep(10); }
            Require(enteredBarrier.IsSet, "Sweep should reach barrier on case 2");
            Flush();

            // Status strip Cancel button MUST be visible while running (UX-21) and clicking it cancels the sweep!
            var stripCancel = host.StatusStrip.FindControl<Button>("SweepCancelButton")!;
            Require(stripCancel.IsVisible, "StatusStrip SweepCancelButton must be visible while sweep runs");
            var runCancel = host.ModelView.SweepsView.LocalRun.FindControl<Button>("CancelRunButton")!;
            Require(runCancel.IsEnabled, "LocalRunView CancelRunButton must be enabled while sweep runs");

            controller.Sweeps.CancelRun();
            releaseBarrier.Set();
            var outcome = WaitTask(runTask) ?? throw new InvalidOperationException("Run outcome null");
            Flush();
            Require(outcome.Cancelled && outcome.CompletedCases == 1,
                $"Expected Cancelled with 1 completed case, got cancelled={outcome.Cancelled} completed={outcome.CompletedCases}");
        }
        finally
        {
            releaseBarrier.Set();
            window.Close();
        }
    }

    private static void ThemesRenderCleanly()
    {
        var method = new FastSweepMethod(Settings.WithStations(Settings.Default with { NSpanPerHalf = 8, NChord = 2 }));
        using var controller = new WorkbenchController(analysisMethod: method);
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        window.Show();
        try
        {
            OpenExample(controller);
            controller.EnterSweeps();
            controller.Sweeps.NewSweep();
            controller.Sweeps.SpeedsText = "10";
            controller.Sweeps.AnglesText = "0, 2";
            controller.Sweeps.RefreshPreview();
            _ = controller.Sweeps.Queue();
            _ = WaitTask(controller.Sweeps.RunSelectedAsync());
            controller.Sweeps.SetSubTab(SweepSubTab.Results);
            foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark, NativeReviewThemes.HighContrast })
            {
                window.RequestedThemeVariant = variant;
                Flush();
                Require(host.ModelView.SweepsView.Results.Chart.RenderedSegmentCount == 1, $"Chart rendered under {variant}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertNoRunButton(Control root)
    {
        foreach (var btn in root.GetLogicalDescendants().OfType<Button>())
        {
            string label = (btn.Content?.ToString() ?? "").Trim();
            string a11y = (AutomationProperties.GetName(btn) ?? "").Trim();
            bool forbidden = label.Equals("Run", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Run locally", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Start", StringComparison.OrdinalIgnoreCase)
                || label.Equals("Execute", StringComparison.OrdinalIgnoreCase)
                || a11y.StartsWith("Run ", StringComparison.OrdinalIgnoreCase);
            Require(!forbidden, $"XS-03 violation: found Run button ('{label}' / '{a11y}') inside ExperimentSetupView");
        }
    }

    private static string CollectText(Control root)
    {
        var parts = new List<string>();
        foreach (var node in root.GetLogicalDescendants())
        {
            if (node is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text)) parts.Add(tb.Text);
            else if (node is ContentControl cc && cc.Content is string s && !string.IsNullOrWhiteSpace(s)) parts.Add(s);
        }
        return string.Join(" | ", parts);
    }

    private static void CheckLab01Clean(string text, string surface)
    {
        foreach (string word in Lab01Forbidden)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException($"LAB-01 violation on {surface}: found forbidden word '{word}'");
        }
    }

    private static T WaitTask<T>(Task<T> task, int timeoutSeconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Flush();
            Thread.Sleep(10);
        }
        if (!task.IsCompleted) throw new TimeoutException($"Async task timed out after {timeoutSeconds}s");
        Flush();
        return task.GetAwaiter().GetResult();
    }

    private static void OpenExample(WorkbenchController controller) =>
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();

    private static void Flush() => Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class HoldBarrier(Func<OperatingPoint, CancellationToken, ValueTask> onComputed) : IEvaluationBarrier
    {
        public ValueTask ComputedAsync(OperatingPoint op, CancellationToken cancellation) => onComputed(op, cancellation);
    }

    private sealed class FastSweepMethod(RunSettings settings) : IWingMethod
    {
        private readonly ProductWingMethod inner = new(settings);
        private static readonly string FakeHash = new('1', 64);
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Solve(sections, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            var loads = StripCoupler.Couple(sections, solution, op, water, new FixedPolar(), _ => FakeHash, cancellation);
            return loads.Select((l, i) => l with { YLow = solution.Strips[i].YInboard, YHigh = solution.Strips[i].YOutboard }).ToArray();
        }
        public IReadOnlyList<StripLoad> Couple(byte[] source, IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            Couple(sections, solution, op, water, cancellation);
    }

    private sealed class FixedPolar : IPolarSource
    {
        public string? UnavailableReason => null;
        public PolarResult? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg, WaterRecord water, CancellationToken cancellation) =>
            new(new PolarSample(profileHash, "test", "1.0", reynolds, ncrit, "smooth", alphaDeg, water.TableHash,
                0.3, Math.Abs(ncrit - 2) < 0.5 ? 0.012 : 0.009, -0.05, 0.5, 0.5, -0.5, 200, 0.99, true), [], false, 0, 0);
    }

    private sealed class FaultingSweepMethod(IWingMethod inner, Func<OperatingPoint, bool> fail) : IWingMethod
    {
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);
        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            fail(op) ? throw new ContractError("ANA-SOLVE-RESIDUAL", "lattice residual exceeded limit") : inner.Solve(sections, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Couple(sections, solution, op, water, cancellation);
        public IReadOnlyList<StripLoad> Couple(byte[] source, IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op, WaterRecord water, CancellationToken cancellation) =>
            inner.Couple(source, sections, solution, op, water, cancellation);
    }
}
