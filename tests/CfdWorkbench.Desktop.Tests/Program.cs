namespace CfdWorkbench.Desktop.Tests;

/// <summary>`--harness …` runs the Desktop console harness (the parent, or one suite mode); anything else is xunit (tests/HarnessTests.cs).</summary>
internal static class Program
{
    private static Task<int> Main(string[] args) =>
        CfdWorkbench.Tests.HarnessEntry.Run(args, DesktopHarness.Run, SelfRegisteredExtensions.AddSelfRegisteredExtensions);
}
