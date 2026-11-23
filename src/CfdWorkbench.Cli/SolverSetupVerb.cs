using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench solver-setup survey|status|report [--store &lt;dir&gt;] [--route &lt;id&gt;] [--json]</c>
/// Zero-PII CLI parity for Guided Solver Setup (W8-SLV-CORE, Ruling 69).
/// </summary>
internal static class SolverSetupVerb
{
    internal const string Usage =
        "Usage: cfd-workbench solver-setup survey|status|report [--store <dir>] [--route <id>] [--json]";

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation)
    {
        if (args.Length < 2)
        {
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string sub = args[1];
        if (sub is not ("survey" or "status" or "report"))
        {
            await output.WriteLineAsync($"Error SOLVER-SETUP-USAGE: unknown subcommand '{sub}'.");
            await output.WriteLineAsync(Usage);
            return 2;
        }

        string? storeDir = null;
        string? routeId = null;
        bool json = false;
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--json") { json = true; continue; }
            if (args[i] == "--store" && i + 1 < args.Length) { storeDir = args[++i]; continue; }
            if (args[i] == "--route" && i + 1 < args.Length) { routeId = args[++i]; continue; }
            await output.WriteLineAsync($"Error SOLVER-SETUP-USAGE: unknown or incomplete option '{args[i]}'.");
            await output.WriteLineAsync(Usage);
            return 2;
        }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            var service = new SolverSetupService(storeDir);

            if (sub == "survey")
            {
                var res = await service.RunSurveyAsync(cancellation);
                var s = res.Survey;
                var payload = new
                {
                    schemaVersion = 1,
                    os = s.Os.ToString(),
                    osName = SetupReportBuilder.Redact(s.OsName, Environment.UserName, Environment.MachineName),
                    osBuild = s.OsBuild,
                    arch = s.Arch.ToString(),
                    freeDiskBytes = s.FreeDiskBytes,
                    rebootPending = s.RebootPending,
                    virtualizationFirmwareEnabled = s.VirtualizationFirmwareEnabled,
                    hypervisorPresent = s.HypervisorPresent,
                    wslAvailable = s.WslAvailable,
                    wslDefaultVersion = s.WslDefaultVersion,
                    wslDistros = s.WslDistros.Select(d => new { name = d.Name, state = d.State, version = d.Version, isDefault = d.IsDefault }).ToArray(),
                    recommendedRoute = res.RecommendedRoute?.Id,
                    availableRoutes = res.AvailableRoutes.Select(r => r.Id).ToArray(),
                    blockerCauseCode = res.BlockerCauseCode,
                    durationMs = s.DurationMs
                };
                if (json) await Cli.WriteAsync(output, payload);
                else await output.WriteLineAsync($"OS: {payload.osName} ({payload.arch}, build {payload.osBuild}) · recommended: {payload.recommendedRoute ?? "none"} · {payload.durationMs} ms");
                return 0;
            }

            if (sub == "status")
            {
                var vm = service.Project(selectedRouteId: routeId, hasApiKey: false);
                var st = vm.StatusSummary;
                var payload = new
                {
                    schemaVersion = 1,
                    readiness = st.Readiness.ToString(),
                    stripLabel = st.StripLabel,
                    routeId = vm.RouteId,
                    screen = vm.Screen.ToString(),
                    backend = st.Backend,
                    version = st.Version,
                    buildId = st.BuildId,
                    verifiedInstall = st.VerifiedInstall,
                    currentStepId = vm.CurrentCard?.StepId,
                    currentStepNumber = st.CurrentStepNumber,
                    totalSteps = st.TotalSteps,
                    reason = SetupReportBuilder.Redact(st.Reason, Environment.UserName, Environment.MachineName)
                };
                if (json) await Cli.WriteAsync(output, payload);
                else await output.WriteLineAsync($"{payload.stripLabel} (route={payload.routeId ?? "none"}, screen={payload.screen})");
                return 0;
            }

            string report = service.BuildSupportReport(
                typeof(SolverSetupVerb).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                selectedRouteId: routeId);
            if (json) await Cli.WriteAsync(output, new { schemaVersion = 1, report });
            else await output.WriteLineAsync(report);
            return 0;
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}");
            return 130;
        }
        catch (Core.ContractError err)
        {
            await Cli.WriteAsync(output, new { code = err.Code, reason = err.Reason });
            return Cli.ExitForCode(err.Code);
        }
    }
}
