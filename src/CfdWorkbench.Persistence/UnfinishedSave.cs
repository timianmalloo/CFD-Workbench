namespace CfdWorkbench.Persistence;

/// <summary>
/// What an unfinished save leaves behind (measured by WindowsStore_CrashInjection_LeftoverInventoryMeasured): a claim file
/// <c>.cfd-writer.claim</c> in the project's folder, and when it stopped before the rename a <c>.cfd-&lt;operation&gt;.tmp</c>.
/// The claim refuses every later overwrite in that folder (DOC-CONFLICT), whatever the project name.
/// </summary>
public static class UnfinishedSave
{
    /// <summary>True when the folder of <paramref name="projectPath"/> holds a claim file, so a save there is refused until it is removed.</summary>
    public static bool BlocksFolder(string projectPath)
    {
        try { return Path.GetDirectoryName(projectPath) is { Length: > 0 } folder && File.Exists(Path.Combine(folder, ProjectStore.ClaimName())); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
    }
}
