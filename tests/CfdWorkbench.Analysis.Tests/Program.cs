namespace CfdWorkbench.Analysis.Tests;

/// <summary>`--harness …` runs the Analysis console harness; anything else is xunit (tests/HarnessTests.cs).</summary>
internal static class Program
{
    private static Task<int> Main(string[] args) =>
        CfdWorkbench.Tests.HarnessEntry.Run(args, rest => Task.FromResult(AnalysisChecks.Run(rest)), SelfRegisteredExtensions.AddSelfRegisteredExtensions);
}
