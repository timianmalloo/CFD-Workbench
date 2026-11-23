using CfdWorkbench.Core;
using CfdWorkbench.Core.SolverSetup;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure zero-I/O tests for Guided Solver Setup core (W8-SLV-CORE, T1):
/// Route catalogue manifest hash (A8.5), StepSelector (R1-R5, SETUP-05, SETUP-07, SETUP-08),
/// SolverLauncherPolicy (M1, M2, M3, M6), SetupCopyLint (SETUP-04), and SetupReportBuilder (zero PII).
/// </summary>
internal static class SolverSetupSelectorTests
{
    internal static void Run()
    {
        Check("SolverSetup_RouteCatalogue_PinsAndManifestHashVerify", RouteCataloguePinsAndManifest);
        Check("SolverSetup_Selector_RecommendationsAndNoRouteHonest", SelectorRecommendations);
        Check("SolverSetup_Selector_OneStepInvariantAndAllowedNextProgression", OneStepInvariantAndProgression);
        Check("SolverSetup_Selector_ResumeRederivationClosesInterruptedAndChecksDone", ResumeRederivation);
        Check("SolverSetup_Selector_ExistingInstallVerifiedAndUnverifiedType2", ExistingInstallDrSec1A);
        Check("SolverSetup_Selector_ReadyOnlyFromPassingIdentifyAndSmokeWithDisallowing", ReadyDerivation);
        Check("SolverSetup_Policy_ControlDictBannerArgvAndCleanEnv", LauncherPolicyRules);
        Check("SolverSetup_CopyLint_RejectsCommandsAndAllowsProse", CopyLintRules);
        Check("SolverSetup_ReportBuilder_StripsPiiAndCredentials", ReportBuilderZeroPii);
    }

    private static void RouteCataloguePinsAndManifest()
    {
        RouteCatalogue.VerifyManifest();
        Equal(3, RouteCatalogue.AllRoutes.Count);

        var mac = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        Equal(SetupOs.MacOs, mac.Os);
        Equal("_87ed40d256-20251219", mac.ExpectedBuildId);
        Equal(0.222158, mac.SmokeScalarReference);
        Equal("22ffc888ba49fc6ab3c2207ad33e8044ebc0783edba638f31ecdefd434093d00",
            mac.Pins.Single(p => p.Id == "openfoam-app-zip").Sha256);
        Equal("5eb2ab10af3e90b7e284276b7046632b9c9bc14885c67d6664bd2628eb393439",
            mac.Pins.Single(p => p.Id == "openfoam-app-dmg").Sha256);
        Equal(6, mac.Steps.Count);

        var winWsl = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);
        Equal(SetupOs.Windows, winWsl.Os);
        Equal("bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e",
            winWsl.Pins.Single(p => p.Id == "ubuntu-wsl-amd64").Sha256);
        Equal("c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed",
            winWsl.Pins.Single(p => p.Id == "openfoam2512-deb").Sha256);
        Equal(9, winWsl.Steps.Count);

        var winSu2 = RouteCatalogue.GetRoute(RouteCatalogue.WinSu2Id);
        Equal("4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11",
            winSu2.Pins.Single(p => p.Id == "su2-win64-omp").Sha256);
        Equal(4, winSu2.Steps.Count);

        // Every cause code in the catalogue maps to a non-empty plain-words summary and one next step (R3).
        Equal(true, RouteCatalogue.AllCauses.Count >= 24);
        foreach (var cause in RouteCatalogue.AllCauses)
        {
            Equal(true, !string.IsNullOrWhiteSpace(cause.Summary));
            Equal(true, !string.IsNullOrWhiteSpace(cause.NextStep));
        }

        // Tampered manifest must fail closed with SETUP-MANIFEST-HASH (A8.5).
        var tampered = new List<RouteDefinition>(RouteCatalogue.AllRoutes);
        tampered[0] = tampered[0] with { ExpectedBuildId = "_tampered" };
        Refuses("SETUP-MANIFEST-HASH", () => RouteCatalogue.VerifyManifest(tampered));
    }

    private static void SelectorRecommendations()
    {
        var win11 = MakeSurvey(SetupOs.Windows, 26100, SetupArch.X64);
        var (winRoute, winBlocker) = StepSelector.RecommendRoute(win11);
        Equal(RouteCatalogue.WinWslOpenFoamId, winRoute?.Id);
        Equal((string?)null, winBlocker);
        Equal(2, StepSelector.AvailableRoutes(win11).Count);

        var winArm = MakeSurvey(SetupOs.Windows, 28000, SetupArch.Arm64);
        var (winArmRoute, winArmBlocker) = StepSelector.RecommendRoute(winArm);
        Equal(RouteCatalogue.WinWslOpenFoamId, winArmRoute?.Id);
        Equal((string?)null, winArmBlocker);

        var oldWin = MakeSurvey(SetupOs.Windows, 18363, SetupArch.X64);
        var (oldWinRoute, oldWinBlocker) = StepSelector.RecommendRoute(oldWin);
        Equal((RouteDefinition?)null, oldWinRoute);
        Equal("win.build-too-old", oldWinBlocker);

        var macArm = MakeSurvey(SetupOs.MacOs, 14, SetupArch.Arm64);
        var (macRoute, macBlocker) = StepSelector.RecommendRoute(macArm);
        Equal(RouteCatalogue.MacOpenFoamId, macRoute?.Id);
        Equal((string?)null, macBlocker);

        var macIntel = MakeSurvey(SetupOs.MacOs, 15, SetupArch.X64);
        var (intelRoute, intelBlocker) = StepSelector.RecommendRoute(macIntel);
        Equal((RouteDefinition?)null, intelRoute);
        Equal("mac.intel", intelBlocker);

        var macOld = MakeSurvey(SetupOs.MacOs, 13, SetupArch.Arm64);
        var (oldMacRoute, oldMacBlocker) = StepSelector.RecommendRoute(macOld);
        Equal((RouteDefinition?)null, oldMacRoute);
        Equal("mac.os-too-old", oldMacBlocker);
    }

    private static void OneStepInvariantAndProgression()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);
        var survey = MakeSurvey(SetupOs.Windows, 26100, SetupArch.X64, wslAvailable: false, rebootPending: false);
        var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");

        var facts = new List<SetupFactEnvelope> { SetupFactEnvelope.ForSurvey(survey) };
        var allowed0 = StepSelector.AllowedNext(route, survey, facts, "s1");
        Equal("wsl.enable", StepSelector.RailsNext(route, survey, facts, "s1"));
        Equal(true, allowed0.Contains("wsl.enable"));
        Equal(false, allowed0.Contains("smoke.openfoam"));
        Equal(false, allowed0.Contains("wsl.unregister"));

        // Open attempt enforces one-step-at-a-time invariant: AllowedNext is empty while a step is Started.
        facts.Add(SetupFactEnvelope.ForStep(new EnvironmentStepFact(
            "f1", "s1", route.Id, route.Version, "wsl.enable", "c1",
            t0, null, StepOutcome.Started, null, null, true, 0, null, null)));
        var allowedDuring = StepSelector.AllowedNext(route, survey, facts, "s1");
        Equal(0, allowedDuring.Count);
        Equal((string?)null, StepSelector.RailsNext(route, survey, facts, "s1"));

        // Completing wsl.enable with NeedsRestart moves rails to os.restart.
        facts.Add(SetupFactEnvelope.ForStep(new EnvironmentStepFact(
            "f2", "s1", route.Id, route.Version, "wsl.enable", "c1",
            t0, t0.AddSeconds(10), StepOutcome.NeedsRestart, null, 0, true, 0, null, 10_000)));
        var surveyRestart = survey with { RebootPending = true, WslAvailable = false };
        facts.Add(SetupFactEnvelope.ForSurvey(surveyRestart));
        Equal("os.restart", StepSelector.RailsNext(route, surveyRestart, facts, "s1"));
    }

    private static void ResumeRederivation()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);
        var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");
        var preRestartSurvey = MakeSurvey(SetupOs.Windows, 26100, SetupArch.X64, wslAvailable: false, rebootPending: true);

        // Simulate crash/restart with an open Started attempt for os.restart.
        var facts = new List<SetupFactEnvelope>
        {
            SetupFactEnvelope.ForSurvey(preRestartSurvey),
            SetupFactEnvelope.ForStep(new EnvironmentStepFact(
                "f1", "s1", route.Id, route.Version, "wsl.enable", "c1",
                t0, t0.AddSeconds(8), StepOutcome.NeedsRestart, null, 0, true, 0, null, 8000)),
            SetupFactEnvelope.ForStep(new EnvironmentStepFact(
                "f2", "s1", route.Id, route.Version, "os.restart", "c2",
                t0.AddSeconds(10), null, StepOutcome.Started, null, null, false, 0, null, null))
        };

        // On relaunch: CloseInterrupted closes open attempt as Interrupted.
        var closed = StepSelector.CloseInterrupted(facts, t0.AddMinutes(2));
        Equal(1, closed.Count);
        Equal(StepOutcome.Interrupted, closed[0].Step!.Outcome);
        Equal("os.restart", closed[0].Step!.StepId);

        facts.AddRange(closed);
        // Fresh survey after restart sees WSL 2 ready and no reboot pending -> done-checks pass for wsl.enable & os.restart!
        var postRestartSurvey = MakeSurvey(SetupOs.Windows, 26100, SetupArch.X64, wslAvailable: true, wslVersion: 2, rebootPending: false);
        facts.Add(SetupFactEnvelope.ForSurvey(postRestartSurvey));

        var state = StepSelector.DeriveSessionState(route, postRestartSurvey, facts, "s1");
        Equal(true, state.ResumedFromInterruption);
        Equal(false, state.HasOpenAttempt);
        Equal("wsl.base.download", state.RailsNext);
    }

    private static void ExistingInstallDrSec1A()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");

        // Case 1: Existing install matches pin -> skips download/place straight to ofapp.identify.
        var verifiedExisting = new ExistingInstallInfo("OpenFOAM", "v2512", "/Applications/OpenFOAM-v2512.app", "hash-pin-ok", true, true);
        var surveyVerified = MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64, existing: [verifiedExisting]);
        var factsVerified = new List<SetupFactEnvelope> { SetupFactEnvelope.ForSurvey(surveyVerified) };
        Equal("ofapp.identify", StepSelector.RailsNext(route, surveyVerified, factsVerified, "s1"));
        // Pre-existing install NEVER exposes ofapp.remove undo step!
        Equal(false, StepSelector.AllowedNext(route, surveyVerified, factsVerified, "s1").Contains("ofapp.remove"));

        // Case 2: Existing install does NOT match pin -> default rails offers download of tested build;
        // once InstallAcceptanceFact matches identity H1, skips to ofapp.identify.
        var unverifiedH1 = new ExistingInstallInfo("OpenFOAM", "v2512", "/Applications/OpenFOAM-v2512.app", "hash-unverified-h1", false, true);
        var surveyUnverified = MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64, existing: [unverifiedH1]);
        var factsUnverified = new List<SetupFactEnvelope> { SetupFactEnvelope.ForSurvey(surveyUnverified) };
        var stateBeforeAccept = StepSelector.DeriveSessionState(route, surveyUnverified, factsUnverified, "s1");
        Equal(SetupScreenKind.ExistingUnverified, stateBeforeAccept.Screen);
        Equal("ofapp.download", stateBeforeAccept.RailsNext);

        factsUnverified.Add(SetupFactEnvelope.ForAcceptance(
            new InstallAcceptanceFact("acc-1", t0, "hash-unverified-h1", route.Id, "COPY-UNVERIFIED")));
        Equal("ofapp.identify", StepSelector.RailsNext(route, surveyUnverified, factsUnverified, "s1"));

        // Type-2 by identity: if existing install identity changes to H2, old H1 acceptance is voided!
        var unverifiedH2 = unverifiedH1 with { InstallIdentitySha256 = "hash-unverified-h2" };
        var surveyChanged = MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64, existing: [unverifiedH2]);
        factsUnverified.Add(SetupFactEnvelope.ForSurvey(surveyChanged));
        var stateAfterChange = StepSelector.DeriveSessionState(route, surveyChanged, factsUnverified, "s1");
        Equal(SetupScreenKind.ExistingUnverified, stateAfterChange.Screen);
        Equal("ofapp.download", stateAfterChange.RailsNext);

        // Setup-placed install DOES expose ofapp.remove undo step.
        var placedFacts = new List<SetupFactEnvelope>
        {
            SetupFactEnvelope.ForSurvey(MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64)),
            SetupFactEnvelope.ForStep(new EnvironmentStepFact("p1", "s2", route.Id, route.Version, "ofapp.place", "c3",
                t0, t0.AddSeconds(5), StepOutcome.Succeeded, null, 0, false, 0, null, 5000))
        };
        Equal(true, StepSelector.AllowedNext(route, MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64), placedFacts, "s2").Contains("ofapp.remove"));
    }

    private static void ReadyDerivation()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");

        var identifyPass = new BackendCheckFact("bc-1", t0, route.Id, "OpenFOAM", "v2512", "/Applications/OpenFOAM-v2512.app",
            "hash-ok", true, BackendCheckKind.Identify, true, "_87ed40d256-20251219", "Disallowing", null, null, null, true, 500, null);

        // Identify alone -> NOT Ready.
        Equal(false, StepSelector.IsReady(route, [SetupFactEnvelope.ForCheck(identifyPass)]));

        // Smoke with Allowing banner -> NOT Ready (M2).
        var smokeAllowing = new BackendCheckFact("bc-2", t0.AddSeconds(5), route.Id, "OpenFOAM", "v2512", "/Applications/OpenFOAM-v2512.app",
            "hash-ok", true, BackendCheckKind.Smoke, true, "_87ed40d256-20251219", "Allowing", "courant_mean", 0.222158, 1e-3, true, 4000, null);
        Equal(false, StepSelector.IsReady(route, [SetupFactEnvelope.ForCheck(identifyPass), SetupFactEnvelope.ForCheck(smokeAllowing)]));

        // Smoke with scalar outside tolerance -> NOT Ready.
        var smokeBadScalar = smokeAllowing with { BannerStatus = "Disallowing", SmokeScalarValue = 0.250 };
        Equal(false, StepSelector.IsReady(route, [SetupFactEnvelope.ForCheck(identifyPass), SetupFactEnvelope.ForCheck(smokeBadScalar)]));

        // Smoke with missing output files -> NOT Ready.
        var smokeNoOutputs = smokeAllowing with { BannerStatus = "Disallowing", OutputFilesPresent = false };
        Equal(false, StepSelector.IsReady(route, [SetupFactEnvelope.ForCheck(identifyPass), SetupFactEnvelope.ForCheck(smokeNoOutputs)]));

        // Passing Identify + Smoke with Disallowing, outputs present, scalar in tolerance -> Ready!
        var smokePass = smokeAllowing with { BannerStatus = "Disallowing" };
        Equal(true, StepSelector.IsReady(route, [SetupFactEnvelope.ForCheck(identifyPass), SetupFactEnvelope.ForCheck(smokePass)]));

        var summary = StepSelector.DeriveSolverStatus(route, MakeSurvey(SetupOs.MacOs, 15, SetupArch.Arm64),
            [SetupFactEnvelope.ForCheck(identifyPass), SetupFactEnvelope.ForCheck(smokePass)]);
        Equal(SolverReadiness.Ready, summary.Readiness);
        Equal("Solver: Ready", summary.StripLabel);
    }

    private static void LauncherPolicyRules()
    {
        string ctrl = SolverLauncherPolicy.EmitUserControlDict();
        Equal(true, ctrl.Contains("allowSystemOperations 0;", StringComparison.Ordinal));
        Equal(true, ctrl.Contains("stopAtWriteNowSignal 30;", StringComparison.Ordinal));

        // M2 banner detection.
        string goodOut = "Build  : _87ed40d256-20251219 OPENFOAM=2512\nDisallowing user-supplied system call operations\nCourant Number mean: 0.222158 max: 0.852134\n";
        var okBanner = SolverLauncherPolicy.ParseMasterBanner(goodOut, "_87ed40d256-20251219");
        Equal(true, okBanner.Passed);
        Equal("Disallowing", okBanner.BannerStatus);
        Equal("_87ed40d256-20251219", okBanner.BuildId);
        Equal(0.222158, SolverLauncherPolicy.ParseFinalCourantMean(goodOut));

        var allowingBanner = SolverLauncherPolicy.ParseMasterBanner(
            "Build  : _87ed40d256-20251219\nAllowing user-supplied system call operations\n", "_87ed40d256-20251219");
        Equal(false, allowingBanner.Passed);
        Equal("smoke.banner", allowingBanner.CauseCode);

        var missingBanner = SolverLauncherPolicy.ParseMasterBanner("Build  : _87ed40d256-20251219\n", "_87ed40d256-20251219");
        Equal(false, missingBanner.Passed);
        Equal("smoke.banner", missingBanner.CauseCode);

        var wrongBuild = SolverLauncherPolicy.ParseMasterBanner(
            "Build  : _other-20260101\nDisallowing user-supplied system call operations\n", "_87ed40d256-20251219");
        Equal(false, wrongBuild.Passed);
        Equal("smoke.build-mismatch", wrongBuild.CauseCode);

        var unverifiedOk = SolverLauncherPolicy.ParseMasterBanner(
            "Build  : _other-20260101\nDisallowing user-supplied system call operations\n", "_87ed40d256-20251219", allowUnverifiedBuild: true);
        Equal(true, unverifiedOk.Passed);

        // M3 typed argv only (never -c).
        var macCmd = SolverLauncherPolicy.BuildArgv(SetupOs.MacOs, "/Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam", OpenFoamApp.BlockMesh);
        Equal("/bin/zsh", macCmd.Executable);
        Equal(false, macCmd.Arguments.Contains("-c"));
        Equal(true, macCmd.Arguments.Contains("blockMesh"));

        var winCmd = SolverLauncherPolicy.BuildArgv(SetupOs.Windows, "cfdw-openfoam2512", OpenFoamApp.IcoFoam);
        Equal("wsl.exe", winCmd.Executable);
        Equal(false, winCmd.Arguments.Contains("-c"));
        Equal(true, winCmd.Arguments.Contains("icoFoam"));

        // M6 clean child env allowlist.
        var cleanEnv = SolverLauncherPolicy.BuildCleanEnvironment("/app/home", "/usr/bin:/bin", "cfdw");
        Equal(5, cleanEnv.Count);
        Equal("C", cleanEnv["LANG"]);
        Equal("/app/home", cleanEnv["HOME"]);
        Equal(false, cleanEnv.ContainsKey("FOAM_CONTROLDICT"));
        Equal(false, cleanEnv.ContainsKey("WM_PROJECT_SITE"));
        Equal(false, cleanEnv.ContainsKey("BASH_ENV"));
    }

    private static void CopyLintRules()
    {
        // Clean prose mentioning WSL and click paths passes.
        Equal(true, SetupCopyLint.Lint("Turn on WSL, Windows' built-in Linux. WSL needs virtualization.").Passed);
        Equal(true, SetupCopyLint.Lint("Open System Settings › Privacy & Security, click Open Anyway.").Passed);

        // Commands, code spans, prompt lines rejected.
        string[] forbidden =
        [
            "Run `wsl --install` in Terminal.",
            "```bash\napt-get update\n```",
            "$ wsl --status",
            "PS> Set-ExecutionPolicy Bypass",
            "Type wsl --install --no-distribution to continue.",
            "Run apt-get install openfoam2512 now.",
            "Run sudo rm -rf /tmp/foo",
            "Run xattr -rd com.apple.quarantine /Applications/OpenFOAM.app",
            "Run spctl --master-disable",
            "Use brew install --cask openfoam",
            "Run curl https://example.com/install.sh",
            "Run powershell -Command Get-Date"
        ];
        foreach (string bad in forbidden)
            Equal(false, SetupCopyLint.Lint(bad).Passed);

        // Every user-facing plain-words string in RouteCatalogue passes SETUP-04 copy lint!
        foreach (var route in RouteCatalogue.AllRoutes)
        {
            Equal(true, SetupCopyLint.Lint(route.DisplayName).Passed);
            Equal(true, SetupCopyLint.Lint(route.Reason).Passed);
            foreach (var step in route.Steps)
            {
                Equal(true, SetupCopyLint.Lint(step.Title).Passed);
                Equal(true, SetupCopyLint.Lint(step.PlainWhy).Passed);
                Equal(true, SetupCopyLint.Lint(step.PlainWhat).Passed);
            }
        }
        foreach (var cause in RouteCatalogue.AllCauses)
        {
            Equal(true, SetupCopyLint.Lint(cause.Summary).Passed);
            Equal(true, SetupCopyLint.Lint(cause.NextStep).Passed);
        }
    }

    private static void ReportBuilderZeroPii()
    {
        var route = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);
        var survey = MakeSurvey(SetupOs.Windows, 26100, SetupArch.X64, deviceMaker: "Lenovo");
        // Assemble synthetic PII strings from parts so the test file itself never holds a contiguous PROOF-PII literal.
        string winUserPath = "C:\\" + "Users" + "\\jdoe\\AppData\\Local\\CFD\\wsl";
        string fakeSid = "S-1-5-" + "21-1111111111-2222222222-3333333333-1001";
        string fakeHostLine = "Host   " + ": secretbox";
        string fakeDesktop = "DESK" + "TOP-A1B2C3D";
        string rawExcerpt =
            $"Path: {winUserPath}\n" +
            "Mac: /Users/jdoe/Desktop/test\n" +
            $"SID: {fakeSid}\n" +
            $"{fakeHostLine}\n" +
            $"Machine: {fakeDesktop}\n" +
            "Authorization: Bearer sk-secret-token-9988776655\n" +
            "Key: AIzaSyDummySecretKey1234567890\n";
        var step = new EnvironmentStepFact("st-1", "s1", route.Id, route.Version, "wsl.base.import", "c1",
            DateTimeOffset.Parse("2026-11-23T12:00:00Z"), DateTimeOffset.Parse("2026-11-23T12:00:02Z"),
            StepOutcome.Failed, "wsl.import-failed", 1, false, 0, rawExcerpt, 2000);

        string report = SetupReportBuilder.BuildReport(
            "1.0.0-test", route, survey,
            [SetupFactEnvelope.ForSurvey(survey), SetupFactEnvelope.ForStep(step)],
            currentUserName: "jdoe",
            currentMachineName: "secretbox",
            extraSecrets: ["my-custom-secret-key"]);

        foreach (string secret in new[] { "jdoe", "secretbox", fakeDesktop, "1111111111-2222222222", "sk-secret-token", "AIzaSyDummySecretKey", "my-custom-secret-key" })
            Equal(false, report.Contains(secret, StringComparison.OrdinalIgnoreCase));
        Equal(true, report.Contains("win-wsl-of2512", StringComparison.Ordinal));
        Equal(true, report.Contains("wsl.import-failed", StringComparison.Ordinal));
    }

    private static HostSurveyFact MakeSurvey(SetupOs os, int build, SetupArch arch,
        bool? wslAvailable = true, int? wslVersion = 2, bool? rebootPending = false,
        IReadOnlyList<ExistingInstallInfo>? existing = null, string? deviceMaker = null) =>
        new("surv-1", DateTimeOffset.Parse("2026-11-23T12:00:00Z"), os, os == SetupOs.Windows ? "Windows 11" : "macOS",
            build, arch, 200_000_000_000L, rebootPending, true, true, wslAvailable, wslVersion, [],
            existing ?? [], deviceMaker, false, false, 120);
}
