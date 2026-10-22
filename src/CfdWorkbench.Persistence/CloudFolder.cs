namespace CfdWorkbench.Persistence;

/// <summary>Where a chosen save folder is: a local folder, a OneDrive or other cloud-files folder, or one the check could not read.</summary>
public enum FolderLocation { Local, Cloud, CheckFailed }

/// <summary>
/// The OneDrive refusal of Ruling 135 and Ruling 160: a folder synced by OneDrive is refused after the pick and before any write.
/// A folder is cloud when it is at or under a OneDrive environment-variable path, or when it or any ancestor up to the volume root
/// carries a cloud-files reparse tag or a cloud-files attribute. If the check cannot run, the answer is <see cref="FolderLocation.CheckFailed"/>
/// (fail closed, Ruling 160 (5)); the caller refuses the save.
/// </summary>
public static class CloudFolder
{
    // winnt.h: IO_REPARSE_TAG_ONEDRIVE = 0x80000021; IO_REPARSE_TAG_CLOUD = 0x9000001A and the family IO_REPARSE_TAG_CLOUD_1 ...
    // IO_REPARSE_TAG_CLOUD_F = 0x9000F01A, which differ only in bits 12 to 15.
    // https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-point-tags
    private const uint OneDriveTag = 0x80000021, CloudTag = 0x9000001A, CloudTagMask = 0xFFFF0FFF;
    private static readonly string[] OneDriveVariables = ["OneDrive", "OneDriveConsumer", "OneDriveCommercial"];

    public static FolderLocation Classify(string folder)
    {
        if (!OperatingSystem.IsWindows()) return FolderLocation.Local; // the rule is a Windows rule: macOS uses its own save panel
        return Classify(folder, Environment.GetEnvironmentVariable, ReadAttributes);
    }

    internal static FolderLocation Classify(string folder, Func<string, string?> environment, Func<string, (uint Attributes, uint ReparseTag)> read)
    {
        try
        {
            string path = Full(folder);
            foreach (string variable in OneDriveVariables)
                if (environment(variable) is { Length: > 0 } root && IsAtOrUnder(path, Full(root))) return FolderLocation.Cloud;
            for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            {
                var (attributes, tag) = read(current);
                if ((attributes & AttributeMask) != 0 || tag == OneDriveTag || (tag & CloudTagMask) == CloudTag) return FolderLocation.Cloud;
            }
            return FolderLocation.Local;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return FolderLocation.CheckFailed; }
    }

    // FILE_ATTRIBUTE_RECALL_ON_OPEN | PINNED | UNPINNED | RECALL_ON_DATA_ACCESS (winnt.h, values on WindowsNative).
    private const uint AttributeMask = 0x40000 | 0x80000 | 0x100000 | 0x400000;

    private static string Full(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("A fully qualified path is required.");
        string full = Path.GetFullPath(path);
        if (full.Length < 3 || !char.IsAsciiLetter(full[0]) || full[1] != ':' || full[2] != '\\') throw new ArgumentException("A drive-letter path is required.");
        return full.Length > 3 ? full.TrimEnd('\\') : full;
    }

    private static bool IsAtOrUnder(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root.EndsWith('\\') ? root : root + "\\", StringComparison.OrdinalIgnoreCase);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static (uint Attributes, uint ReparseTag) ReadAttributes(string path)
    {
        var tag = WindowsNative.AttributeTag(path);
        return (tag.Attributes, tag.ReparseTag);
    }
}
