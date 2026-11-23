using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Core.SolverSetup;
using Microsoft.Win32;

namespace CfdWorkbench.Analysis.SolverSetup;

public interface IHostSurveyProbe
{
    Task<HostSurveyFact> SurveyAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Pure parsers for Windows wsl.exe (UTF-16 LE / UTF-8) and macOS sw_vers / sysctl outputs.
/// </summary>
public static class HostSurveyParsers
{
    private static readonly Regex WslDistroLine = new(
        @"^\s*(\*)?\s*(\S+)\s+(\S+)\s+(\d+)\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex WslDefaultVersionRegex = new(
        @"Default\s+Version\s*:\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Decodes wsl.exe raw stdout bytes whether UTF-16 LE (with or without BOM) or UTF-8.</summary>
    public static string DecodeWslOutput(byte[] raw)
    {
        if (raw.Length == 0) return "";
        if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
            return Encoding.Unicode.GetString(raw, 2, raw.Length - 2);
        if (raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2);

        int samplePairs = Math.Min(raw.Length, 512) / 2;
        if (samplePairs >= 2)
        {
            int oddNulls = 0, evenNulls = 0;
            for (int i = 0; i < samplePairs * 2; i += 2)
            {
                if (raw[i] == 0) evenNulls++;
                if (raw[i + 1] == 0) oddNulls++;
            }
            if (oddNulls >= samplePairs * 0.7 && evenNulls <= samplePairs * 0.1)
                return Encoding.Unicode.GetString(raw);
            if (evenNulls >= samplePairs * 0.7 && oddNulls <= samplePairs * 0.1)
                return Encoding.BigEndianUnicode.GetString(raw);
        }

        return Encoding.UTF8.GetString(raw);
    }

    public static (bool Available, int? DefaultVersion) ParseWslStatus(int exitCode, string decodedText)
    {
        if (exitCode != 0 && string.IsNullOrWhiteSpace(decodedText))
            return (false, null);

        var verMatch = WslDefaultVersionRegex.Match(decodedText);
        int? version = verMatch.Success && int.TryParse(verMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int v)
            ? v
            : null;
        bool available = exitCode == 0 || version is >= 2;
        return (available, version);
    }

    public static IReadOnlyList<WslDistroInfo> ParseWslListVerbose(string decodedText)
    {
        var result = new List<WslDistroInfo>();
        if (string.IsNullOrWhiteSpace(decodedText)) return result;

        foreach (string rawLine in decodedText.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Replace("\0", "");
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Contains("NAME", StringComparison.Ordinal) && line.Contains("VERSION", StringComparison.Ordinal))
                continue;

            var m = WslDistroLine.Match(line);
            if (!m.Success) continue;
            bool isDefault = m.Groups[1].Success && m.Groups[1].Value == "*";
            string name = m.Groups[2].Value;
            string state = m.Groups[3].Value;
            if (!int.TryParse(m.Groups[4].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int version))
                continue;
            result.Add(new WslDistroInfo(name, state, version, isDefault));
        }

        return result;
    }

    public static int ParseMacMajorVersion(string swVersOutput)
    {
        foreach (string rawLine in swVersOutput.Split('\n'))
        {
            string line = rawLine.Trim();
            if (!line.StartsWith("ProductVersion:", StringComparison.OrdinalIgnoreCase)) continue;
            string ver = line["ProductVersion:".Length..].Trim();
            string major = ver.Split('.')[0];
            if (int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out int m))
                return m;
        }
        return 0;
    }

    public static SetupArch ParseMacArch(string sysctlOutput)
    {
        if (sysctlOutput.Contains(": 1", StringComparison.Ordinal) || sysctlOutput.Trim() == "1")
            return SetupArch.Arm64;
        if (sysctlOutput.Contains(": 0", StringComparison.Ordinal) || sysctlOutput.Trim() == "0")
            return SetupArch.X64;
        return SetupArch.Other;
    }
}

/// <summary>
/// Strictly read-only Windows host survey executing under a 5 s budget.
/// Runs only <c>wsl.exe --status</c> and <c>wsl.exe --list --verbose</c> — NEVER starts, mutates,
/// imports, or unregisters any WSL distribution!
/// </summary>
public sealed class WindowsHostSurveyProbe : IHostSurveyProbe
{
    public async Task<HostSurveyFact> SurveyAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => SetupArch.X64,
            Architecture.Arm64 => SetupArch.Arm64,
            _ => SetupArch.Other
        };

        int osBuild = Environment.OSVersion.Version.Build;
        string osName = osBuild >= 22000 ? "Windows 11" : "Windows 10";
        bool? rebootPending = null;
        string? maker = null;
        bool? managedHint = null;
        bool? hypervisorPresent = null;

        if (OperatingSystem.IsWindows())
        {
            (osName, osBuild) = ReadWindowsVersionRegistry(osName, osBuild);
            rebootPending = ReadRebootPendingRegistry();
            maker = ReadSystemManufacturerRegistry();
            managedHint = ReadManagedDeviceHintRegistry();
            hypervisorPresent = ReadHypervisorRegistryHint();
        }

        long? freeDisk = ReadSystemDriveFreeBytes();

        bool? wslAvailable = null;
        int? wslDefaultVersion = null;
        IReadOnlyList<WslDistroInfo> distros = [];

        if (OperatingSystem.IsWindows())
        {
            var statusRun = await RunReadOnlyWslAsync(["--status"], cancellationToken);
            if (statusRun is not null)
            {
                string decoded = HostSurveyParsers.DecodeWslOutput(statusRun.Value.RawOut);
                var parsed = HostSurveyParsers.ParseWslStatus(statusRun.Value.ExitCode, decoded);
                wslAvailable = parsed.Available;
                wslDefaultVersion = parsed.DefaultVersion;
                if (parsed.DefaultVersion >= 2 && hypervisorPresent is null)
                    hypervisorPresent = true;
            }

            var listRun = await RunReadOnlyWslAsync(["--list", "--verbose"], cancellationToken);
            if (listRun is not null)
            {
                string decoded = HostSurveyParsers.DecodeWslOutput(listRun.Value.RawOut);
                distros = HostSurveyParsers.ParseWslListVerbose(decoded);
            }
        }

        var existingInstalls = DetectLocalSu2Installs();

        sw.Stop();
        return new HostSurveyFact(
            FactId: "survey-" + Guid.NewGuid().ToString("N")[..12],
            AtUtc: DateTimeOffset.UtcNow,
            Os: SetupOs.Windows,
            OsName: osName,
            OsBuild: osBuild,
            Arch: arch,
            FreeDiskBytes: freeDisk,
            RebootPending: rebootPending,
            VirtualizationFirmwareEnabled: hypervisorPresent,
            HypervisorPresent: hypervisorPresent,
            WslAvailable: wslAvailable,
            WslDefaultVersion: wslDefaultVersion,
            WslDistros: distros,
            ExistingInstalls: existingInstalls,
            DeviceMaker: maker,
            ManagedDeviceHint: managedHint,
            ProxyDetected: null,
            DurationMs: sw.ElapsedMilliseconds);
    }

    private static async Task<(int ExitCode, byte[] RawOut)?> RunReadOnlyWslAsync(string[] args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("wsl.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string a in args) psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi);
            if (proc is null) return null;

            using var ms = new MemoryStream();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(2500));

            var copyTask = proc.StandardOutput.BaseStream.CopyToAsync(ms, timeout.Token);
            var errDrain = proc.StandardError.BaseStream.CopyToAsync(Stream.Null, timeout.Token);
            await Task.WhenAll(copyTask, errDrain, proc.WaitForExitAsync(timeout.Token));
            return (proc.ExitCode, ms.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static long? ReadSystemDriveFreeBytes()
    {
        try
        {
            string sysRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(sysRoot);
            return drive.IsReady ? drive.AvailableFreeSpace : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<ExistingInstallInfo> DetectLocalSu2Installs()
    {
        var list = new List<ExistingInstallInfo>();
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData)) return list;
            string su2Exe = Path.Combine(localAppData, "CFD Workbench", "su2-8.5.0", "SU2_CFD.exe");
            if (File.Exists(su2Exe))
            {
                string sha = SolverLauncher.ComputeFileSha256(su2Exe);
                list.Add(new ExistingInstallInfo("SU2", "v8.5.0", Path.GetDirectoryName(su2Exe)!, sha, true, true));
            }
        }
        catch
        {
            // Ignore unreadable optional paths.
        }
        return list;
    }

    [SupportedOSPlatform("windows")]
    private static (string Name, int Build) ReadWindowsVersionRegistry(string fallbackName, int fallbackBuild)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);
            if (key is null) return (fallbackName, fallbackBuild);
            int build = fallbackBuild;
            if (key.GetValue("CurrentBuildNumber") is string bStr &&
                int.TryParse(bStr, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
                build = parsed;
            string product = key.GetValue("ProductName") as string ?? fallbackName;
            if (build >= 22000 && product.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
                product = product.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
            return (product, build);
        }
        catch
        {
            return (fallbackName, fallbackBuild);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? ReadRebootPendingRegistry()
    {
        try
        {
            using var cbs = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", false);
            if (cbs is not null) return true;
            using var wu = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired", false);
            if (wu is not null) return true;
            return false;
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadSystemManufacturerRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS", false);
            return key?.GetValue("SystemManufacturer") as string;
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? ReadManagedDeviceHintRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Subsystem for Linux", false);
            if (key?.GetValue("AllowWSL") is int allow && allow == 0) return true;
            return false;
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? ReadHypervisorRegistryHint()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\hvboot", false);
            if (key?.GetValue("Start") is int start && start <= 2) return true;
            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>macOS host survey probe supporting both live execution on macOS and deterministic fixture testing on Windows.</summary>
public sealed class MacHostSurveyProbe : IHostSurveyProbe
{
    private readonly Func<string> runSwVers;
    private readonly Func<string> runSysctlArm64;
    private readonly Func<long?> freeDiskProvider;
    private readonly IReadOnlyList<string> appSearchRoots;

    public MacHostSurveyProbe(
        Func<string>? runSwVers = null,
        Func<string>? runSysctlArm64 = null,
        Func<long?>? freeDiskProvider = null,
        IReadOnlyList<string>? appSearchRoots = null)
    {
        this.runSwVers = runSwVers ?? (() => RunQuickMacCommand("/usr/bin/sw_vers", []));
        this.runSysctlArm64 = runSysctlArm64 ?? (() => RunQuickMacCommand("/usr/sbin/sysctl", ["hw.optional.arm64"]));
        this.freeDiskProvider = freeDiskProvider ?? ReadRootFreeDisk;
        this.appSearchRoots = appSearchRoots ??
        [
            "/Applications/OpenFOAM-v2512.app",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "OpenFOAM-v2512.app")
        ];
    }

    public Task<HostSurveyFact> SurveyAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        string swVers = runSwVers();
        string sysctl = runSysctlArm64();
        int major = HostSurveyParsers.ParseMacMajorVersion(swVers);
        SetupArch arch = HostSurveyParsers.ParseMacArch(sysctl);

        var existing = new List<ExistingInstallInfo>();
        foreach (string appRoot in appSearchRoots)
        {
            if (MacSetupAdapters.InspectExistingBundle(appRoot) is { } found)
                existing.Add(found);
        }

        sw.Stop();
        var fact = new HostSurveyFact(
            FactId: "survey-" + Guid.NewGuid().ToString("N")[..12],
            AtUtc: DateTimeOffset.UtcNow,
            Os: SetupOs.MacOs,
            OsName: major > 0 ? $"macOS {major}" : "macOS",
            OsBuild: major,
            Arch: arch,
            FreeDiskBytes: freeDiskProvider(),
            RebootPending: false,
            VirtualizationFirmwareEnabled: null,
            HypervisorPresent: null,
            WslAvailable: null,
            WslDefaultVersion: null,
            WslDistros: [],
            ExistingInstalls: existing,
            DeviceMaker: "Apple",
            ManagedDeviceHint: null,
            ProxyDetected: null,
            DurationMs: sw.ElapsedMilliseconds);
        return Task.FromResult(fact);
    }

    private static string RunQuickMacCommand(string exe, string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string a in args) psi.ArgumentList.Add(a);
            using var proc = Process.Start(psi);
            if (proc is null) return "";
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);
            return output;
        }
        catch
        {
            return "";
        }
    }

    private static long? ReadRootFreeDisk()
    {
        try
        {
            var drive = new DriveInfo("/");
            return drive.IsReady ? drive.AvailableFreeSpace : null;
        }
        catch
        {
            return null;
        }
    }
}
