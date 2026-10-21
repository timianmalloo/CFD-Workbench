namespace CfdWorkbench.Cli.Tests;

/// <summary>`--harness` runs the CLI console harness; anything else is xunit (tests/HarnessTests.cs).</summary>
internal static class Program
{
    private static Task<int> Main(string[] args) =>
        CfdWorkbench.Tests.HarnessEntry.Run(args, CliHarness.Run, SelfRegisteredExtensions.AddSelfRegisteredExtensions);
}
