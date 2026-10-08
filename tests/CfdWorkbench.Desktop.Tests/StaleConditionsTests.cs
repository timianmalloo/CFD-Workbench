using Avalonia.Controls;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// STL (Ruling 140): a live edit of any conditions-band input makes the shown result Historical on every surface; returning the
/// input makes it Current with no new run; Evaluate makes the new point Current; a blank or malformed input reads as changed.
/// Ring 0, ~3 s (one small evaluation per check, a counted solver).
/// </summary>
public static class StaleConditionsTests
{
    public static void Run()
    {
        DesktopChecks.Check("StaleConditions_BandEdits_EverySurfaceReadsHistorical_RevertIsCurrent_NoRun_BadInputReadsChanged", () =>
        {
            var method = new CountingMethod(new ProductWingMethod(Small()));
            using var controller = Open(method);
            var run = Evaluate(controller, 2);
            var (host, window) = Show(controller);
            try
            {
                var band = Band(host);
                Same(host, controller, "Current", "Analysis: Current", false);
                int solves = method.Solves;
                string key = run.RunKey;
                band.FindControl<TextBox>("SpeedInput")!.Text = "6";
                Settle(host, window);
                Same(host, controller, "Historical", "Analysis: Historical", true);
                Equal("Historical — operating point changed (speed)", controller.AnalysisView.Banner, "the operating-point wording names the input");
                if (Environment.GetEnvironmentVariable("CFD_STL_CAPTURE") is { Length: > 0 } capture)
                {
                    using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(1500, 870));
                    bitmap.Render(window);
                    bitmap.Save(capture);
                }
                Equal(solves, method.Solves, "an edit runs no solve");
                band.FindControl<TextBox>("SpeedInput")!.Text = "5.14";
                Settle(host, window);
                Same(host, controller, "Current", "Analysis: Current", false);
                Equal(solves, method.Solves, "returning the input runs no solve");
                Equal(key, controller.AnalysisView.RunKey, "the shown run is untouched by the edits");
                Equal(RunRecord.RecomputedKey(run), run.RunKey, "the stored run's key is untouched");
                // α keeps the controller's approved wording.
                band.FindControl<TextBox>("AlphaInput")!.Text = "3.00";
                Settle(host, window);
                Equal("Historical — operating point changed (α 2.00° → 3.00°)", controller.AnalysisView.Banner, "the controller's α wording");
                band.FindControl<TextBox>("AlphaInput")!.Text = "2.00";
                Settle(host, window);
                Equal(RunState.Current, controller.AnalysisView.State, "α back is Current");
                // A blank or malformed input does not crash and reads as changed.
                foreach (string bad in new[] { "", "abc", "NaN", "-3" })
                {
                    band.FindControl<TextBox>("SpeedInput")!.Text = bad;
                    Settle(host, window);
                    Equal(RunState.Historical, controller.AnalysisView.State, $"speed '{bad}' reads as changed");
                    Equal(true, controller.AnalysisView.Banner!.StartsWith("Historical — ", StringComparison.Ordinal), $"speed '{bad}' banner");
                }
                band.FindControl<TextBox>("SpeedInput")!.Text = "5.14";
                band.FindControl<TextBox>("AlphaInput")!.Text = "x";
                Settle(host, window);
                Equal(RunState.Historical, controller.AnalysisView.State, "malformed α reads as changed");
                band.FindControl<TextBox>("AlphaInput")!.Text = "2.00";
                Settle(host, window);
                Same(host, controller, "Current", "Analysis: Current", false);
                Equal(true, band.FindControl<TextBlock>("DerivedDepth")!.Text!.Contains("Unavailable — depth not set", StringComparison.Ordinal),
                    "a blank depth shows COPY-45 as before");
                Equal(key, controller.AnalysisView.RunKey, "no edit changed the shown run");
                Equal(solves, method.Solves, "no edit ran a solve");
            }
            finally { window.Close(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }
        });
        DesktopChecks.Check("StaleConditions_WaterAndDepthEdits_ReadHistorical_EvaluateMakesTheNewPointCurrent", () =>
        {
            var method = new CountingMethod(new ProductWingMethod(Small()));
            using var controller = Open(method);
            _ = Evaluate(controller, 2);
            var (host, window) = Show(controller);
            try
            {
                var band = Band(host);
                band.FindControl<ComboBox>("WaterInput")!.SelectedIndex = 1;
                Settle(host, window);
                Equal(RunState.Historical, controller.AnalysisView.State, "a water change reads Historical");
                band.FindControl<ComboBox>("WaterInput")!.SelectedIndex = 0;
                Settle(host, window);
                Equal(RunState.Current, controller.AnalysisView.State, "water back is Current");
                band.FindControl<TextBox>("DepthInput")!.Text = "0.6";
                Settle(host, window);
                Equal(RunState.Historical, controller.AnalysisView.State, "a depth change reads Historical");
                int solves = method.Solves;
                var (op, water) = (band.BuildOperatingPoint(), band.BuildWater());
                var evaluated = Task.Run(() => controller.EvaluateAnalysisAsync(op, water))
                    .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult() ?? throw new Exception("Evaluate returned no run.");
                Settle(host, window);
                Same(host, controller, "Current", "Analysis: Current", false);
                Equal(evaluated.RunKey, controller.AnalysisView.RunKey, "the new run is the shown run");
                Equal(true, method.Solves > solves, "Evaluate solved");
            }
            finally { window.Close(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }
        });
    }

    /// <summary>The strip, the Properties chip, the model-area banner and the projection agree on one state.</summary>
    private static void Same(ShellHost host, WorkbenchController controller, string state, string strip, bool banner)
    {
        Equal(state, controller.AnalysisView.State.ToString(), "projection state");
        Equal(strip, host.StatusStrip.FindControl<TextBlock>("AnalysisItemText")!.Text, "status strip");
        var texts = host.Properties.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text))
            .Select(t => t.Text!).ToHashSet(StringComparer.Ordinal);
        Equal(state == "Historical", texts.Contains("Historical · VLM + strip"), "Properties tier chip");
        Equal(banner, host.ModelView.FindControl<Border>("HistoricalBanner")!.IsVisible, "model-area banner");
        Equal(banner, controller.AnalysisView.Banner is not null, "projection banner");
    }

    private static ConditionsBand Band(ShellHost host) => host.ModelView.FindControl<ConditionsBand>("AnalysisConditionsBand")!;

    private static RunSettings Small() => Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };

    private static WorkbenchController Open(IWingMethod method)
    {
        var controller = new WorkbenchController(analysisMethod: method);
        Task.Run(controller.OpenExampleAsync).WaitAsync(TimeSpan.FromSeconds(20)).GetAwaiter().GetResult();
        return controller;
    }

    private static AnalysisRun Evaluate(WorkbenchController controller, double alpha) =>
        Task.Run(() => controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, alpha, null), controller.AnalysisWater))
            .WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult() ?? throw new Exception("Fixture Evaluate returned no run.");

    private static (ShellHost Host, Window Window) Show(WorkbenchController controller)
    {
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1500, Height = 870 };
        window.Show();
        controller.ToggleAnalysis();
        controller.Layout = ViewLayout.Four;
        Settle(host, window);
        return (host, window);
    }

    private static void Settle(ShellHost host, Window window)
    {
        host.RefreshPanes();
        for (int pass = 0; pass < 6; pass++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Equal<T>(T expected, T actual, string what)
    {
        if (!Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }

    private sealed class CountingMethod(IWingMethod inner) : IWingMethod
    {
        public int Solves { get; private set; }
        public RunMethod Method => inner.Method;
        public RunSettings Settings => inner.Settings;
        public double ReconciliationTolerance => inner.ReconciliationTolerance;
        public RunReference Reference(byte[] source) => inner.Reference(source);

        public LatticeSolution Solve(IReadOnlyList<SectionSample> sections, OperatingPoint op, WaterRecord water, CancellationToken cancellation)
        {
            Solves++;
            return inner.Solve(sections, op, water, cancellation);
        }

        public IReadOnlyList<StripLoad> Couple(IReadOnlyList<SectionSample> sections, LatticeSolution solution, OperatingPoint op,
            WaterRecord water, CancellationToken cancellation) => inner.Couple(sections, solution, op, water, cancellation);
    }
}
