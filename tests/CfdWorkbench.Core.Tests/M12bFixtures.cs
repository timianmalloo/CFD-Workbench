namespace CfdWorkbench.Core.Tests;

// TEST-REPO-LAYOUT: fixture reads must survive a published test run whose cwd is not the repo
// root (docs/lessons/defect-classes.md). CfdWorkbench.Core.Tests.csproj's
// `Content Include="Fixtures\m12b\**\*" CopyToOutputDirectory="PreserveNewest"` item guarantees
// this directory sits beside the test binary under every build shape (build, run, publish) the
// readiness gate uses, so a single AppContext.BaseDirectory-relative path is enough. No
// existence probe of the filesystem is needed here, which matters beyond style: verify-
// application-core.py's STORE-SUBSET guard treats most System dot-IO member access outside
// ProjectStoreTests.cs/LayoutFileTests.cs/PreferenceStoreTests.cs as umask-sensitive code that
// must run under every mask; a plain path join stays outside that reach.
internal static class M12bFixtures
{
    internal static string Path(string name) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "m12b", name);
}
