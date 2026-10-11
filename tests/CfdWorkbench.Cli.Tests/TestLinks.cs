using System.Diagnostics;

/// <summary>The one place a Cli test makes a link. <c>File.CreateSymbolicLink</c> and <c>Directory.CreateSymbolicLink</c> need a
/// privilege on Windows (IOException "A required privilege is not held"). A directory link there is a junction, which needs none; a
/// file link has no junction equivalent, so when the host refuses it the case is NOT ASSESSED, said out loud, never a silent pass.
/// macOS always makes the real symbolic link, and a failure there is a failure. The same helper sits in each test project
/// (TEST-FOREIGN-OS-BRANCH, docs/lessons/defect-classes.md); <c>SelfLaunchTests.NoBareSymbolicLinkOutsideTestLinks</c> keeps it the only caller.</summary>
internal static class TestLinks
{
    /// <summary>The message a Windows host gives when the account may not create symbolic links (ERROR_PRIVILEGE_NOT_HELD, 1314).</summary>
    internal static bool IsMissingPrivilege(Exception error) =>
        error is IOException or UnauthorizedAccessException &&
        (error.HResult == unchecked((int)0x80070522) || error.Message.Contains("privilege", StringComparison.OrdinalIgnoreCase));

    /// <summary>Makes a file link. False (after printing NOT ASSESSED) only on Windows without the privilege; every other error throws.</summary>
    internal static bool TryFileLink(string link, string target)
    {
        try { File.CreateSymbolicLink(link, target); return true; }
        catch (Exception error) when (OperatingSystem.IsWindows() && IsMissingPrivilege(error))
        {
            Console.WriteLine("NOT ASSESSED file-link (Windows: creating a file link needs a privilege; enable Developer Mode)");
            return false;
        }
    }

    /// <summary>Makes a directory link: a symbolic link, or on Windows a junction. False (after printing NOT ASSESSED) when the junction cannot be made.</summary>
    internal static bool TryDirectoryLink(string link, string target)
    {
        if (TryDirectoryLink(link, target, OperatingSystem.IsWindows(), info =>
        {
            info.RedirectStandardOutput = true;
            using var child = Process.Start(info)!;
            child.StandardOutput.ReadToEnd();
            child.WaitForExit();
            return child.ExitCode;
        })) return true;
        Console.WriteLine("NOT ASSESSED directory-link: mklink /J could not create a junction; the linked-directory case did not run");
        return false;
    }

    /// <summary>The branch under test: <paramref name="windows"/> picks the junction command, <paramref name="run"/> runs it.</summary>
    internal static bool TryDirectoryLink(string link, string target, bool windows, Func<ProcessStartInfo, int> run)
    {
        if (!windows) { Directory.CreateSymbolicLink(link, target); return true; }
        var info = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in new[] { "/c", "mklink", "/J", link, target }) info.ArgumentList.Add(argument);
        return run(info) == 0 && Directory.Exists(link);
    }
}
