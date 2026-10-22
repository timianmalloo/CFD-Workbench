using System.Text.RegularExpressions;

namespace CfdWorkbench.Persistence;

/// <summary>What Clear unfinished save did (design windows-store-recovery 2.4). Failed carries the code; InUse is a live writer.</summary>
public enum ClearOutcome { Cleared, InUse, Failed }

/// <summary><paramref name="Code"/> is OK for Cleared, DOC-CONFLICT for InUse, and the failure code for Failed; <paramref name="Removed"/> counts the entries deleted.</summary>
public sealed record ClearResult(ClearOutcome Outcome, string Code, int Removed);

/// <summary>
/// What an unfinished save leaves behind (measured by WindowsStore_CrashInjection_LeftoverInventoryMeasured): a claim file
/// <c>.cfd-writer.claim</c> in the project's folder, and when it stopped before the rename a <c>.cfd-&lt;operation&gt;.tmp</c>.
/// The claim refuses every later overwrite in that folder (DOC-CONFLICT), whatever the project name.
/// </summary>
public static class UnfinishedSave
{
    // The lowercase D form Guid.ToString("D") produces (ProjectStore.TempName, BackupTempName); a case-variant temp blocks nothing.
    private static readonly Regex TempPattern = new(@"\A\.cfd-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(\.bak)?\.tmp\z",
        RegexOptions.CultureInvariant);

    /// <summary>True when the folder of <paramref name="projectPath"/> holds a claim file, so a save there is refused until it is removed.</summary>
    public static bool BlocksFolder(string projectPath)
    {
        try { return Path.GetDirectoryName(projectPath) is { Length: > 0 } folder && File.Exists(Path.Combine(folder, ProjectStore.ClaimName())); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
    }

    /// <summary>
    /// Clear's candidates among a folder's entry names (design 2.2): the claim compared case-insensitively (the kernel's collision rule, so a
    /// case variant blocks the claim create exactly as the exact name does) and the lowercase temp pattern. A name is a candidate, never provenance.
    /// </summary>
    internal static List<string> Candidates(IEnumerable<string> names) =>
        names.Where(name => string.Equals(name, ProjectStore.ClaimName(), StringComparison.OrdinalIgnoreCase) || TempPattern.IsMatch(name))
            .OrderBy(name => name, StringComparer.Ordinal).ToList();

    internal static bool IsClaim(string name) => string.Equals(name, ProjectStore.ClaimName(), StringComparison.OrdinalIgnoreCase);
}
