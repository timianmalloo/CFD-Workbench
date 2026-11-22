using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Setup;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Node SET-4 tests (T14 in docs/design/setup-seeding.md §7, Ruling 272 DR-SET-5):
/// Exercises all 6 Area 1 Setup hard states (clean-race, flagged-gate, conflict-targets, class-violation,
/// reseed-edited, no-key-language) on a real Avalonia window, verifies non-modal Setup | CAD | Analysis | Sweeps
/// navbar switching preserving CAD selection, verifies live Area 4 Sweeps Goal-state seam activation after Seed,
/// verifies Light/Dark/HighContrast live theme switching, and scans every rendered Setup string for 0 LAB-01 hits.
/// </summary>
public static class SetupViewTests
{
    private static readonly string[] Lab01Forbidden =
    [
        "cavitation-free", "ventilation-safe", "validated", "optimized", "best", "recommended", "certified"
    ];

    public static void Run()
    {
        DesktopChecks.Check("Desktop_SetupHardStates_NoKeyAndLab01Clean", T14_AllSixHardStatesAndLab01);
        DesktopChecks.Check("Desktop_SetupThemes_LightDarkHighContrast_AndCadSelectionPreserved", ThemesAndSelectionPreserved);
    }

    // T14 (AI-01, LAB-01, UX-19, DR-SET-5): Renders all 6 hard states; no-key state disables prompt box with COPY-53
    // while parameter Seed stays active; scan of all rendered Setup strings finds 0 LAB-01 forbidden words.
    // Mutant: enabling LanguagePromptBox without API key or injecting "recommended" into Setup copy turns this RED.
    private static void T14_AllSixHardStatesAndLab01()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        window.Show();
        try
        {
            // Enter Setup directly from the shell (non-modal first navbar segment, DR-SET-5 (a)).
            controller.EnterSetup();
            Flush();
            Require(controller.IsSetup, "Controller.IsSetup must be true");
            var navSetup = host.ModelView.FindControl<ToggleButton>("NavSetupButton")
                ?? throw new InvalidOperationException("NavSetupButton missing from ModelArea navbar");
            Require(navSetup.IsChecked == true, "NavSetupButton must be checked");
            var setupView = host.ModelView.SetupView;
            Require(setupView.IsVisible, "SetupWorkspaceView must be visible");

            var seedBtn = setupView.FindControl<Button>("SeedButton")!;
            var promptBox = setupView.FindControl<TextBox>("LanguagePromptBox")!;
            var submitBtn = setupView.FindControl<Button>("LanguageSubmitButton")!;
            var noKeyText = setupView.FindControl<TextBlock>("LanguageNoKeyText")!;
            var flaggedCard = setupView.FindControl<Border>("FlaggedGateCard")!;
            var wakeBanner = setupView.FindControl<Border>("WakefoilBanner")!;
            var fallbackBanner = setupView.FindControl<Border>("SectionFallbackBanner")!;
            var conflictBanner = setupView.FindControl<Border>("ConflictBanner")!;
            var conflictText = setupView.FindControl<TextBlock>("ConflictBannerText")!;
            var reseedBanner = setupView.FindControl<Border>("ReseedBanner")!;
            var drcSummary = setupView.FindControl<TextBlock>("DrcSummaryText")!;

            // ---- Hard State 6: no-key-language (AI-01 / COPY-53) ----
            Require(!controller.Setup.HasApiKey, "HasApiKey defaults false");
            Require(noKeyText.IsVisible && noKeyText.Text == SetupController.Copy53NoApiKey,
                $"Expected COPY-53 text, got '{noKeyText.Text}'");
            Require(!promptBox.IsEnabled && !submitBtn.IsEnabled, "Language prompt & submit must be disabled without API key");
            AssertLab01Clean(setupView, "no-key-language");

            // ---- Hard State 1: clean-race + GEN fallback disclosure ----
            controller.Setup.SelectPurpose("wingfoil-race");
            Flush();
            Require(fallbackBanner.IsVisible, "GEN fallback disclosure must be visible for wingfoil-race");
            Require(flaggedCard.IsVisible && !seedBtn.IsEnabled, "Seed must stay disabled until Flagged defaults acknowledged");
            controller.Setup.SetFlaggedAcknowledged(true);
            Flush();
            Require(seedBtn.IsEnabled, "Seed must enable once Flagged assumptions are acknowledged (offline AI-01)");
            controller.Setup.SetSubTab("goal");
            Flush();
            AssertLab01Clean(setupView, "clean-race-goal");

            // ---- Hard State 2: flagged-gate (wakefoil -> surf mapping) ----
            controller.Setup.SetSubTab("brief");
            controller.Setup.SelectPurpose("wakefoil");
            Flush();
            Require(wakeBanner.IsVisible, "WakefoilBanner must be visible when wakefoil selected");
            Require(flaggedCard.IsVisible && !seedBtn.IsEnabled, "Seed must be disabled while wakefoil Flagged gate is unacknowledged");
            AssertLab01Clean(setupView, "flagged-gate");

            // ---- Hard State 3: conflict-targets (SET-01 conflicting triple) ----
            controller.Setup.SelectPurpose("wingfoil-race");
            controller.Setup.MaxSpanMeters = 0.80;
            controller.Setup.MaxChordMm = 100.0;
            controller.Setup.TargetAreaCm2 = 1000.0;
            controller.Setup.WeightSpan = 1.0;
            controller.Setup.WeightChord = 0.0;
            controller.Setup.WeightArea = 0.0;
            controller.Setup.WeightAspectRatio = 0.0;
            controller.Setup.SetFlaggedAcknowledged(true);
            controller.Setup.RecomputePreview();
            Flush();
            Require(conflictBanner.IsVisible, "ConflictBanner must be visible on conflicting triple");
            Require(conflictText.Text?.Contains("Targets conflict:", StringComparison.Ordinal) == true,
                $"Expected COPY-73 conflict notice, got '{conflictText.Text}'");
            Require(seedBtn.IsEnabled, "Conflict is soft preference — Seed remains enabled");
            AssertLab01Clean(setupView, "conflict-targets");

            // ---- Hard State 4: class-violation (GWA S = 650 cm2 < 700, mast = 120 cm > 115) ----
            controller.Setup.SelectPurpose("wingfoil-race");
            controller.Setup.TargetAreaCm2 = 650.0;
            controller.Setup.MastLengthCm = 120.0;
            controller.Setup.SetFlaggedAcknowledged(true);
            controller.Setup.RecomputePreview();
            controller.Setup.SetSubTab("checks");
            Flush();
            Require(drcSummary.Text?.Contains("class.gwa.front_area", StringComparison.Ordinal) == true,
                $"Expected class.gwa.front_area in DRC summary, got '{drcSummary.Text}'");
            Require(drcSummary.Text?.Contains("class.gwa.mast_length", StringComparison.Ordinal) == true,
                $"Expected class.gwa.mast_length in DRC summary, got '{drcSummary.Text}'");
            AssertLab01Clean(setupView, "class-violation");

            // ---- Seed r1 -> verifies live Area 4 Sweeps Goal-state seam activates! ----
            controller.Setup.SetSubTab("brief");
            controller.Setup.SelectPurpose("wingfoil-race");
            controller.Setup.SetFlaggedAcknowledged(true);
            var seeded1 = controller.Setup.SeedDesign();
            Flush();
            Require(seeded1 is not null && seeded1.RevisionOrdinal == 1, "Initial Seed must create r1");
            Require(controller.Inspection is not null, "WorkbenchController.Inspection must populate after Seed");
            Require(controller.Sweeps.GoalSnapshot is not null,
                "Area 4 Sweeps must receive live GoalStateSweepSnapshot automatically via Core seam!");
            var setupStrip = host.StatusStrip.FindControl<TextBlock>("SetupItemText")!;
            Require(setupStrip.Text?.Contains("Linked", StringComparison.Ordinal) == true,
                $"Expected Linked status in strip, got '{setupStrip.Text}'");

            // ---- Hard State 5: reseed-edited (manual edit -> Direct parametric -> Re-seed as r3) ----
            var dimTask = controller.ApplySpanAsync("1050");
            DesktopChecks.PumpToCompletion(dimTask, "Setup reseed span edit", TimeSpan.FromSeconds(5));
            controller.EnterSetup();
            Flush();
            Require(controller.Setup.IsReseed, "IsReseed must be true on an edited document");
            Require(reseedBanner.IsVisible, "ReseedBanner must be visible when re-seeding");
            Require((string?)seedBtn.Content == "Re-seed as r3", $"Expected 'Re-seed as r3', got '{seedBtn.Content}'");
            Require(setupStrip.Text == Labels.SetupDirectParametric,
                $"Expected 'Direct parametric' after manual edit, got '{setupStrip.Text}'");
            AssertLab01Clean(setupView, "reseed-edited");

            // Execute Re-seed as r3 and confirm Linked status returns and Goal state constraint edit leaves geometry unchanged!
            controller.Setup.SelectPurpose("downwind");
            controller.Setup.SetFlaggedAcknowledged(true);
            var seeded3 = controller.Setup.SeedDesign();
            Flush();
            Require(seeded3 is not null && seeded3.RevisionOrdinal == 3 && seeded3.IsReseed, "Re-seed must append r3");
            string hashBeforeGoalEdit = controller.Inspection!.Authored.Binding.SurfaceHash!;
            var savedGoal = controller.Setup.UpdateConstraintBound("c-cav", 1.25);
            Flush();
            Require(savedGoal is not null, "UpdateConstraintBound must append GoalStateVersion");
            Require(controller.Inspection.Authored.Binding.SurfaceHash == hashBeforeGoalEdit,
                "Goal state edit must never modify geometry (GOAL-03)");
            AssertLab01Clean(setupView, "post-reseed");
        }
        finally
        {
            window.Close();
        }
    }

    private static void ThemesAndSelectionPreserved()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        window.Show();
        try
        {
            controller.Setup.SelectPurpose("wingfoil-race");
            controller.Setup.SetFlaggedAcknowledged(true);
            controller.Setup.SeedDesign();
            controller.EnterCad();
            Flush();
            var sel = new Selection.Station(0, 0.0);
            controller.Select(sel);
            Flush();

            // Toggle CAD -> Setup -> CAD and verify selection is preserved verbatim (UX-04, DR-SET-5).
            controller.EnterSetup();
            Flush();
            Require(controller.IsSetup && host.ModelView.SetupView.IsVisible, "Setup must open cleanly");

            // Switch live across Light, Dark, and High Contrast themes without exceptions.
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark, NativeReviewThemes.HighContrast })
            {
                if (Application.Current is not null) Application.Current.RequestedThemeVariant = theme;
                window.RequestedThemeVariant = theme;
                controller.Setup.RecomputePreview();
                Flush();
                AssertLab01Clean(host.ModelView.SetupView, $"theme-{theme}");
            }
            if (Application.Current is not null) Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            window.RequestedThemeVariant = ThemeVariant.Light;

            controller.EnterCad();
            Flush();
            Require(!controller.IsSetup, "EnterCad must leave Setup");
            Require(Equals(controller.Selection, sel), "Switching Setup <-> CAD must preserve CAD selection (UX-04)");
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertLab01Clean(Control root, string stateName)
    {
        var texts = new List<string>();
        foreach (var node in root.GetLogicalDescendants())
        {
            switch (node)
            {
                case TextBlock tb when !string.IsNullOrEmpty(tb.Text): texts.Add(tb.Text); break;
                case ContentControl cc when cc.Content is string s && !string.IsNullOrEmpty(s): texts.Add(s); break;
                case TextBox box when !string.IsNullOrEmpty(box.Watermark): texts.Add(box.Watermark); break;
            }
        }
        foreach (string text in texts)
        {
            foreach (string word in Lab01Forbidden)
            {
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    throw new InvalidOperationException($"LAB-01 forbidden word '{word}' found in Setup ({stateName}): \"{text}\"");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Flush() => Dispatcher.UIThread.RunJobs();
}
