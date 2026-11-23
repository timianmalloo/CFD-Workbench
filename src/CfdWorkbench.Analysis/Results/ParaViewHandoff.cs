using System.Diagnostics;
using CfdWorkbench.Analysis.SolverSetup;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Results;

/// <summary>Injectable locator seam for ParaView 5.12+ executable discovery (docs/design/results-and-candidates.md §6).</summary>
public interface IParaViewLocator
{
    string? FindExecutable(string? explicitOverridePath = null);
}

/// <summary>Injectable process starter seam so tests verify exact argv-only ProcessStartInfo without spawning GUI binaries.</summary>
public interface IParaViewProcessStarter
{
    int StartDetached(ProcessStartInfo startInfo);
}

/// <summary>
/// Default ParaView 5.12+ locator checking well-known Windows/macOS install locations and PATH without executing a shell.
/// Zero VTK or ParaView native libraries are linked into the application (COMMIT-02, spec A5.11).
/// </summary>
public sealed class DefaultParaViewLocator : IParaViewLocator
{
    public string? FindExecutable(string? explicitOverridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitOverridePath))
            return File.Exists(explicitOverridePath) ? explicitOverridePath : null;

        if (OperatingSystem.IsWindows())
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles) && Directory.Exists(programFiles))
            {
                foreach (string pattern in new[] { "ParaView 5.13*", "ParaView 5.12*" })
                {
                    foreach (string dir in Directory.EnumerateDirectories(programFiles, pattern).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                    {
                        string candidate = Path.Combine(dir, "bin", "paraview.exe");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }

            return FindOnPath("paraview.exe");
        }

        if (OperatingSystem.IsMacOS())
        {
            const string apps = "/Applications";
            if (Directory.Exists(apps))
            {
                foreach (string pattern in new[] { "ParaView-5.13*.app", "ParaView-5.12*.app" })
                {
                    foreach (string app in Directory.EnumerateDirectories(apps, pattern).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                    {
                        string candidate = Path.Combine(app, "Contents", "MacOS", "paraview");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }

            return FindOnPath("paraview");
        }

        return FindOnPath("paraview");
    }

    private static string? FindOnPath(string exeName)
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv)) return null;
        foreach (string segment in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                string candidate = Path.Combine(segment, exeName);
                if (File.Exists(candidate)) return candidate;
            }
            catch
            {
                // Ignore malformed PATH entries.
            }
        }
        return null;
    }
}

public sealed class SystemParaViewProcessStarter : IParaViewProcessStarter
{
    public int StartDetached(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        using var proc = Process.Start(startInfo)
            ?? throw new ContractError("RES-PARAVIEW-LAUNCH", "Failed to start ParaView process.");
        return proc.Id;
    }
}

public sealed record ParaViewAvailability(
    bool Available,
    string? ExecutablePath,
    string Message);

public sealed record ParaViewLaunchResult(
    bool Launched,
    int? ProcessId,
    string? ExecutablePath,
    string? TargetCasePath,
    string Message,
    IReadOnlyList<string> Arguments,
    bool UseShellExecute,
    bool CreateNoWindow);

/// <summary>
/// Process-only "Open in ParaView" handoff (spec A5.11, UX-06, COMMIT-02, docs/design/results-and-candidates.md §6).
/// Never links VTK, never invokes a shell, refuses symlinks/path traversal (M5), and surfaces exact UX-06 absence copy.
/// </summary>
public static class ParaViewHandoff
{
    public static ParaViewAvailability Prepare(
        IParaViewLocator? locator = null,
        string? explicitOverridePath = null)
    {
        var effectiveLocator = locator ?? new DefaultParaViewLocator();
        string? exe = effectiveLocator.FindExecutable(explicitOverridePath);
        if (string.IsNullOrWhiteSpace(exe))
            return new ParaViewAvailability(false, null, Labels.ParaViewNotFound);

        return new ParaViewAvailability(true, exe, Labels.ParaViewReady);
    }

    public static ParaViewLaunchResult Launch(
        string caseDirectory,
        IParaViewLocator? locator = null,
        string? explicitOverridePath = null,
        IParaViewProcessStarter? starter = null)
    {
        ValidateSafePath(caseDirectory, "case directory");
        string fullCaseDir = Path.GetFullPath(caseDirectory);
        if (SolverLauncher.IsReparseOrLink(fullCaseDir))
            throw new ContractError("RES-PARAVIEW-SYMLINK", "Refused to hand off a symlink or junction case directory to ParaView.");
        if (!Directory.Exists(fullCaseDir))
            throw new ContractError("RES-PARAVIEW-PATH", "Case directory does not exist.");

        var availability = Prepare(locator, explicitOverridePath);
        if (!availability.Available || string.IsNullOrWhiteSpace(availability.ExecutablePath))
        {
            return new ParaViewLaunchResult(
                Launched: false,
                ProcessId: null,
                ExecutablePath: null,
                TargetCasePath: fullCaseDir,
                Message: Labels.ParaViewNotFound,
                Arguments: [],
                UseShellExecute: false,
                CreateNoWindow: true);
        }

        string exePath = availability.ExecutablePath;
        ValidateSafePath(exePath, "ParaView executable");
        string fullExe = Path.GetFullPath(exePath);
        if (SolverLauncher.IsReparseOrLink(fullExe))
            throw new ContractError("RES-PARAVIEW-SYMLINK", "Refused to launch a symlinked ParaView executable.");
        if (!File.Exists(fullExe))
            throw new ContractError("RES-PARAVIEW-PATH", "ParaView executable does not exist.");

        var psi = new ProcessStartInfo(fullExe)
        {
            WorkingDirectory = fullCaseDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(fullCaseDir);

        var effectiveStarter = starter ?? new SystemParaViewProcessStarter();
        int pid = effectiveStarter.StartDetached(psi);

        return new ParaViewLaunchResult(
            Launched: true,
            ProcessId: pid,
            ExecutablePath: fullExe,
            TargetCasePath: fullCaseDir,
            Message: "Opened case directory in ParaView.",
            Arguments: [fullCaseDir],
            UseShellExecute: psi.UseShellExecute,
            CreateNoWindow: psi.CreateNoWindow);
    }

    private static void ValidateSafePath(string path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ContractError("RES-PARAVIEW-PATH", $"Empty {kind} path.");
        string[] segments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == ".."))
            throw new ContractError("RES-PARAVIEW-PATH", $"Path traversal segment '..' refused in {kind} path.");
    }
}
