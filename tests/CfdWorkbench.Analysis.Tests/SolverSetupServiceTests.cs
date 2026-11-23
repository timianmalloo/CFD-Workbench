using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core.SolverSetup;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// SetupFactStore (append-only JSONL, FileShare.Read | FileShare.Delete, interrupted recovery),
/// SolverSetupService orchestration, SolverSetupProjection (UI-ready view models for Wave 8C),
/// and Labels.Solver* SETUP-04 copy lint compliance.
/// </summary>
internal static class SolverSetupServiceTests
{
    internal static void Run()
    {
        Check("SolverSetup_FactStore_AppendReadAndInterruptedRecovery", FactStoreAppendAndRecovery);
        Check("SolverSetup_Service_FlowProjectionAndLabelsLint", ServiceFlowAndProjection);
    }

    private static void FactStoreAppendAndRecovery()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cfdw-factstore-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SetupFactStore(dir);
            var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");
            var route = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);

            var survey = new HostSurveyFact("s1", t0, SetupOs.Windows, "Windows 11", 26100, SetupArch.X64,
                150_000_000_000L, false, true, true, false, null, [], [], "Dell", false, false, 95);
            store.Append(SetupFactEnvelope.ForSurvey(survey));
            store.Append(SetupFactEnvelope.ForStep(new EnvironmentStepFact(
                "st-open", "sess-1", route.Id, route.Version, "wsl.enable", "c-1",
                t0.AddSeconds(1), null, StepOutcome.Started, null, null, true, 0, null, null)));

            // Concurrent read while file exists succeeds cleanly (FileShare.Read | FileShare.Delete).
            var raw = store.ReadAll();
            Equal(2, raw.Count);

            // Recovery closes the open attempt as Interrupted.
            var recovered = store.RecoverInterruptedOnLaunch(t0.AddMinutes(1));
            Equal(3, recovered.Count);
            Equal(StepOutcome.Interrupted, recovered[2].Step!.Outcome);

            // Re-running recovery is idempotent (no second Interrupted written).
            var recoveredAgain = store.RecoverInterruptedOnLaunch(t0.AddMinutes(2));
            Equal(3, recoveredAgain.Count);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    private static void ServiceFlowAndProjection()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cfdw-slvsvc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var t0 = DateTimeOffset.Parse("2026-11-23T12:00:00Z");
            var fakeSurvey = new HostSurveyFact("sv-1", t0, SetupOs.Windows, "Windows 11", 26100, SetupArch.X64,
                200_000_000_000L, false, true, true, true, 2, [], [], "Lenovo", false, false, 40);
            var probe = new StubSurveyProbe(fakeSurvey);
            var runOnce = new InMemoryRunOnceStore();
            var service = new SolverSetupService(dir, probe, runOnce);

            // Before survey: status strip reads "Solver: not set up".
            var initialVm = service.Project(hasApiKey: false);
            Equal(SolverReadiness.NotSetUp, initialVm.StatusSummary.Readiness);
            Equal(Labels.SolverNotSetUp, initialVm.StatusSummary.StripLabel);

            // Run survey -> recommends win-wsl-of2512 and lists win-su2-850 under Other options.
            var surveyed = service.RunSurveyAsync(CancellationToken.None).GetAwaiter().GetResult();
            Equal(RouteCatalogue.WinWslOpenFoamId, surveyed.RecommendedRoute?.Id);
            var routeVm = service.Project(RouteCatalogue.WinWslOpenFoamId, hasApiKey: false);
            Equal(1, routeVm.OtherRoutes.Count);
            Equal(RouteCatalogue.WinSu2Id, routeVm.OtherRoutes[0].Id);
            Equal("wsl.base.download", routeVm.CurrentCard?.StepId);

            // Consent + record identify & smoke passing -> projection derives Ready!
            var route = RouteCatalogue.GetRoute(RouteCatalogue.WinWslOpenFoamId);
            var consent = service.RecordConsent("sess-1", route.Id, "wsl.base.download", "GET ...", null, false);
            Equal("wsl.base.download", consent.StepId);

            service.RecordBackendCheck(new BackendCheckFact(
                "bc-id", t0.AddSeconds(10), route.Id, "OpenFOAM", "v2512", "wsl://cfdw-openfoam2512",
                "id-hash-1", true, BackendCheckKind.Identify, true, route.ExpectedBuildId, "Disallowing",
                null, null, null, true, 300, null));
            service.RecordBackendCheck(new BackendCheckFact(
                "bc-sm", t0.AddSeconds(15), route.Id, "OpenFOAM", "v2512", "wsl://cfdw-openfoam2512",
                "id-hash-1", true, BackendCheckKind.Smoke, true, route.ExpectedBuildId, "Disallowing",
                "courant_mean", 0.222158, 1e-3, true, 4100, null));

            var readyVm = service.Project(route.Id, hasApiKey: false);
            Equal(SetupScreenKind.Ready, readyVm.Screen);
            Equal(SolverReadiness.Ready, readyVm.StatusSummary.Readiness);
            Equal(Labels.SolverReady, readyVm.StatusSummary.StripLabel);

            // Support report is generated cleanly with zero PII.
            string report = service.BuildSupportReport("1.0.0-test", route.Id);
            Equal(true, report.Contains(route.Id, StringComparison.Ordinal));

            // Every Labels.Solver* constant passes SETUP-04 no-command copy lint!
            string[] solverLabels =
            [
                Labels.SolverNotSetUp,
                Labels.SolverReady,
                Labels.SolverNotReady,
                Labels.SolverSettingUp(3, 8, null),
                Labels.SolverSettingUp(6, 8, 60),
                Labels.SolverUnverifiedDisclosure("OpenFOAM"),
                Labels.SolverUnsignedAppDisclosure,
                Labels.SolverReadySentence("OpenFOAM", "v2512", "4 s", "16:43"),
                Labels.SolverNotReadySentence("drive C: is full", "Free up space and check again"),
                Labels.SolverStepRefused("outside the allow-list")
            ];
            foreach (string s in solverLabels)
                Equal(true, SetupCopyLint.Lint(s).Passed, "SETUP-04 copy lint failed on label: " + s);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubSurveyProbe(HostSurveyFact fact) : IHostSurveyProbe
    {
        public Task<HostSurveyFact> SurveyAsync(CancellationToken cancellationToken) => Task.FromResult(fact);
    }
}
