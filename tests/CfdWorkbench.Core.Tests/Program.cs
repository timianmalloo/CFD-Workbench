namespace CfdWorkbench.Core.Tests;

/// <summary>`--harness …` runs the Core console harness; anything else is xunit (tests/HarnessTests.cs).</summary>
internal static class Program
{
    private static Task<int> Main(string[] args) =>
        CfdWorkbench.Tests.HarnessEntry.Run(args, rest => Task.FromResult(IdentityTests.Run(rest)), SelfRegisteredExtensions.AddSelfRegisteredExtensions);
}
