namespace CfdWorkbench.Core.Tests;

// TEST-REPO-LAYOUT: fixture reads must survive a published test run whose cwd is not the repo
// root (docs/lessons/defect-classes.md). Mirrors LayoutFileTests.Fixture's BaseDirectory-first,
// source-tree-fallback resolution, scoped to Fixtures/m12b (csproj copies it CopyToOutputDirectory).
internal static class M12bFixtures
{
    internal static string Path(string name)
    {
        string[] candidates =
        [
            System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "m12b", name),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "m12b", name))
        ];
        foreach (var candidate in candidates)
            if (File.Exists(candidate)) return candidate;
        throw new FileNotFoundException(name);
    }
}
