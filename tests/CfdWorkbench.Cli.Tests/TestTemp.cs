/// <summary>The one scratch root every Cli test uses. The project store refuses a path with a symlinked
/// component by design, and macOS's default TMPDIR (/var/folders/...) sits under the /var link, so a raw
/// <c>Path.GetTempPath()</c> passes only inside tools/run-tests.sh's exported TMPDIR. This resolves every link in
/// the root, so a harness gives the same result alone as in the ring; production still refuses linked paths.
/// The same helper sits in each test project (TEST-TMP-ALIAS, docs/lessons/defect-classes.md).</summary>
internal static class TestTemp
{
    /// <summary>The temp directory with every symlink resolved, ending in a separator.</summary>
    internal static string Root { get; } = Resolve(Path.GetTempPath());

    /// <summary>A path under <see cref="Root"/>; nothing is created.</summary>
    internal static string Combine(string name) => Path.Combine(Root, name);

    /// <summary>A new empty directory under <see cref="Root"/> named <paramref name="prefix"/> plus a GUID.</summary>
    internal static string NewDirectory(string prefix)
    {
        string dir = Path.Combine(Root, prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Resolve(string path)
    {
        string full = Path.GetFullPath(path);
        string current = Path.GetPathRoot(full)!;
        foreach (string part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (new DirectoryInfo(current).ResolveLinkTarget(returnFinalTarget: true) is { } target)
                current = target.FullName;
        }
        return current.EndsWith(Path.DirectorySeparatorChar) ? current : current + Path.DirectorySeparatorChar;
    }
}
