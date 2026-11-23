using System.Text.Json;
using CfdWorkbench.Cli;

/// <summary>
/// CLI tests for `cfd-workbench solver-setup survey|status|report --json` (W8-SLV-CORE).
/// Verifies schema, zero-PII scrubbing, and usage error handling.
/// </summary>
internal static class SolverSetupCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunOne("Cli_SolverSetup_SurveyStatusReportJson_ZeroPii", SurveyStatusReportAsync);
        return failures;
    }

    private static async Task<int> RunOne(string name, Func<Task> check)
    {
        try
        {
            await check();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {name} {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static async Task SurveyStatusReportAsync()
    {
        string storeDir = TestTemp.NewDirectory("solver-setup-cli-");
        try
        {
            // 1. solver-setup survey --json (live read-only survey, zero PII).
            var surveyOut = new StringWriter();
            int surveyExit = await Cli.RunAsync(["solver-setup", "survey", "--store", storeDir, "--json"], surveyOut);
            if (surveyExit != 0)
                throw new Exception($"solver-setup survey failed ({surveyExit}): {surveyOut}");
            string surveyJsonText = surveyOut.ToString();
            AssertNoPii(surveyJsonText, "survey");
            using (var doc = JsonDocument.Parse(surveyJsonText))
            {
                var root = doc.RootElement;
                if (root.GetProperty("schemaVersion").GetInt32() != 1) throw new Exception("survey schemaVersion != 1");
                if (string.IsNullOrWhiteSpace(root.GetProperty("os").GetString())) throw new Exception("survey os missing");
                if (root.GetProperty("durationMs").GetInt64() >= 5000) throw new Exception("survey exceeded 5 s budget");
            }

            // 2. solver-setup status --json.
            var statusOut = new StringWriter();
            int statusExit = await Cli.RunAsync(["solver-setup", "status", "--store", storeDir, "--json"], statusOut);
            if (statusExit != 0)
                throw new Exception($"solver-setup status failed ({statusExit}): {statusOut}");
            string statusJsonText = statusOut.ToString();
            AssertNoPii(statusJsonText, "status");
            using (var doc = JsonDocument.Parse(statusJsonText))
            {
                var root = doc.RootElement;
                if (root.GetProperty("readiness").GetString() != "NotSetUp")
                    throw new Exception("expected NotSetUp, got " + root.GetProperty("readiness").GetString());
                if (root.GetProperty("stripLabel").GetString() != "Solver: not set up")
                    throw new Exception("unexpected stripLabel: " + root.GetProperty("stripLabel").GetString());
            }

            // 3. solver-setup report --json.
            var reportOut = new StringWriter();
            int reportExit = await Cli.RunAsync(["solver-setup", "report", "--store", storeDir, "--json"], reportOut);
            if (reportExit != 0)
                throw new Exception($"solver-setup report failed ({reportExit}): {reportOut}");
            string reportJsonText = reportOut.ToString();
            AssertNoPii(reportJsonText, "report");
            using (var doc = JsonDocument.Parse(reportJsonText))
            {
                string text = doc.RootElement.GetProperty("report").GetString() ?? "";
                if (!text.Contains("CFD Workbench Solver Setup Report", StringComparison.Ordinal))
                    throw new Exception("report header missing");
            }

            // 4. Unknown subcommand -> exit 2 with usage.
            var badOut = new StringWriter();
            int badExit = await Cli.RunAsync(["solver-setup", "bogus"], badOut);
            if (badExit != 2)
                throw new Exception($"expected exit 2 on bogus subcommand, got {badExit}: {badOut}");
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    private static void AssertNoPii(string text, string label)
    {
        string user = Environment.UserName;
        if (user.Length >= 4 && text.Contains(user, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"CLI solver-setup {label} leaked username");
        string host = Environment.MachineName;
        if (host.Length >= 4 && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            text.Contains(host, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"CLI solver-setup {label} leaked hostname");
    }
}
