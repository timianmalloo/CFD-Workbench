using System.IO.Compression;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography;
using CfdWorkbench.Core.SolverSetup;
using Microsoft.Win32;

namespace CfdWorkbench.Analysis.SolverSetup;

/// <summary>
/// Injected Windows per-user RunOnce abstraction (DR-SETUP-6).
/// Unit tests ALWAYS inject <see cref="InMemoryRunOnceStore"/> and never touch real HKCU!
/// </summary>
public interface IRunOnceStore
{
    void RegisterResume(string entryName, string commandLine);
    string? TryRead(string entryName);
    void Remove(string entryName);
}

public sealed class InMemoryRunOnceStore : IRunOnceStore
{
    private readonly Dictionary<string, string> entries = new(StringComparer.Ordinal);

    public void RegisterResume(string entryName, string commandLine) => entries[entryName] = commandLine;
    public string? TryRead(string entryName) => entries.TryGetValue(entryName, out var v) ? v : null;
    public void Remove(string entryName) => entries.Remove(entryName);
}

[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryRunOnceStore : IRunOnceStore
{
    private const string RunOnceSubKey = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";

    public void RegisterResume(string entryName, string commandLine)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunOnceSubKey, writable: true);
        key?.SetValue(entryName, commandLine, RegistryValueKind.String);
    }

    public string? TryRead(string entryName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunOnceSubKey, writable: false);
        return key?.GetValue(entryName) as string;
    }

    public void Remove(string entryName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunOnceSubKey, writable: true);
        key?.DeleteValue(entryName, throwOnMissingValue: false);
    }
}

public sealed record DownloadProgress(long BytesRead, long? TotalBytes);

public sealed record AdapterStepResult(
    bool Succeeded,
    string? CauseCode,
    string Message,
    long BytesDownloaded = 0,
    int? ExitCode = null,
    string? OutputExcerpt = null);

/// <summary>
/// T5 Windows setup adapters: download + SHA-256 verification (.part atomic rename / mismatch delete),
/// Ruling 133 ROUTE-PARENT-MISSING owned-parent check before every external command, WSL import,
/// SU2 unpack + verification, and Windows/WSL/apt failure classification.
/// </summary>
public static class WindowsSetupAdapters
{
    /// <summary>
    /// Ruling 133 (defect class ROUTE-PARENT-MISSING): before any external command, creates and verifies
    /// the destination's owned parent directory exists and is writable. Refuses cleanly before the command runs.
    /// </summary>
    public static AdapterStepResult EnsureOwnedParentDirectory(string destinationPath)
    {
        try
        {
            string full = Path.GetFullPath(destinationPath);
            string? parent = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(parent))
                return new AdapterStepResult(false, "route.parent-missing", "Destination has no parent directory.");

            if (File.Exists(parent))
                return new AdapterStepResult(false, "route.parent-missing", $"Cannot create folder because a file exists at '{parent}'.");

            Directory.CreateDirectory(parent);
            if (!Directory.Exists(parent))
                return new AdapterStepResult(false, "route.parent-missing", "Parent directory could not be verified after creation.");

            // Probe writability with an exclusive temporary file and delete it immediately.
            string probe = Path.Combine(parent, ".cfdw-probe-" + Guid.NewGuid().ToString("N"));
            using (var fs = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.WriteByte(0x4F);
                fs.Flush(flushToDisk: true);
            }
            File.Delete(probe);

            return new AdapterStepResult(true, null, "Owned parent directory verified writable.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new AdapterStepResult(false, "route.parent-missing",
                $"Owned parent directory could not be created or written ({ex.GetType().Name}).");
        }
    }

    /// <summary>
    /// Downloads a pinned artifact to <c>.part</c>, verifies size and SHA-256, and renames atomically.
    /// Any hash mismatch or truncation deletes <c>.part</c> and returns <c>download.hash-mismatch</c>.
    /// </summary>
    public static async Task<AdapterStepResult> DownloadAndVerifyAsync(
        Uri url,
        string destinationPath,
        string expectedSha256,
        long? expectedSizeBytes,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken,
        HttpClient? httpClient = null)
    {
        var parentOk = EnsureOwnedParentDirectory(destinationPath);
        if (!parentOk.Succeeded) return parentOk;

        string partPath = destinationPath + ".part";
        bool ownsClient = httpClient is null;
        var client = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(15) };

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AdapterStepResult(false, "net.offline",
                    $"Server returned HTTP {(int)response.StatusCode}.");
            }

            long? contentLength = response.Content.Headers.ContentLength ?? expectedSizeBytes;
            long totalRead = 0;

            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var remote = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var local = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[81920];
                int read;
                while ((read = await remote.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    hasher.AppendData(buffer, 0, read);
                    totalRead += read;
                    progress?.Report(new DownloadProgress(totalRead, contentLength));
                }
                local.Flush(flushToDisk: true);
            }

            string actualSha = Convert.ToHexStringLower(hasher.GetHashAndReset());
            bool sizeMismatch = expectedSizeBytes is { } expSize && totalRead != expSize;
            bool hashMismatch = !string.Equals(actualSha, expectedSha256, StringComparison.OrdinalIgnoreCase);

            if (sizeMismatch || hashMismatch)
            {
                TryDeleteQuietly(partPath);
                return new AdapterStepResult(false, "download.hash-mismatch",
                    sizeMismatch
                        ? $"Downloaded {totalRead} bytes; expected {expectedSizeBytes} bytes. Deleted partial file."
                        : "Downloaded file SHA-256 did not match the pinned hash. Deleted partial file.",
                    BytesDownloaded: totalRead);
            }

            File.Move(partPath, destinationPath, overwrite: true);
            return new AdapterStepResult(true, null, "Download verified and stored.", BytesDownloaded: totalRead);
        }
        catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException ||
                                              ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                                              ex.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase))
        {
            TryDeleteQuietly(partPath);
            return new AdapterStepResult(false, "net.proxy-tls", "A network filter or untrusted certificate blocked the download.");
        }
        catch (HttpRequestException)
        {
            TryDeleteQuietly(partPath);
            return new AdapterStepResult(false, "net.offline", "Network connection failed during download.");
        }
        catch (IOException)
        {
            TryDeleteQuietly(partPath);
            return new AdapterStepResult(false, "download.hash-mismatch", "Download stream ended unexpectedly; partial file deleted.");
        }
        catch (OperationCanceledException)
        {
            TryDeleteQuietly(partPath);
            throw;
        }
        finally
        {
            if (ownsClient) client.Dispose();
        }
    }

    /// <summary>
    /// Step 4 (`wsl.base.import`): enforces ROUTE-PARENT-MISSING owned-parent check BEFORE invoking `wsl.exe --import`.
    /// </summary>
    public static async Task<AdapterStepResult> RunWslImportStepAsync(
        string distroName,
        string destinationFolder,
        string wslImagePath,
        IProcessRunner runner,
        CancellationToken cancellationToken)
    {
        var parentCheck = EnsureOwnedParentDirectory(destinationFolder);
        if (!parentCheck.Succeeded) return parentCheck;

        if (!File.Exists(wslImagePath))
            return new AdapterStepResult(false, "wsl.import-failed", "Ubuntu WSL image file is missing.");

        var req = new ProcessRunRequest(
            Executable: "wsl.exe",
            Arguments: ["--import", distroName, destinationFolder, wslImagePath, "--version", "2"],
            WorkingDirectory: Path.GetDirectoryName(Path.GetFullPath(destinationFolder))!,
            Environment: SolverLauncherPolicy.BuildCleanEnvironment(
                Path.GetDirectoryName(Path.GetFullPath(destinationFolder))!,
                @"C:\Windows\System32;C:\Windows"),
            Timeout: TimeSpan.FromMinutes(10),
            KillGracePeriod: TimeSpan.FromSeconds(2));

        var res = await runner.RunAsync(req, cancellationToken);
        string excerpt = SetupReportBuilder.Redact(res.StandardOutput + "\n" + res.StandardError);
        if (res.ExitCode == 0)
            return new AdapterStepResult(true, null, "Private WSL 2 distribution imported.", ExitCode: 0, OutputExcerpt: excerpt);

        string cause = ClassifyWindowsFailure("wsl.base.import", res.ExitCode, excerpt);
        return new AdapterStepResult(false, cause, "WSL import failed.", ExitCode: res.ExitCode, OutputExcerpt: excerpt);
    }

    /// <summary>Unpacks SU2 zip safely (preventing Zip-Slip) and checks that SU2_CFD.exe exists and was not quarantined.</summary>
    public static AdapterStepResult UnpackAndVerifySu2(string zipPath, string destinationFolder)
    {
        var parentCheck = EnsureOwnedParentDirectory(Path.Combine(destinationFolder, "SU2_CFD.exe"));
        if (!parentCheck.Succeeded) return parentCheck;

        string fullDest = Path.GetFullPath(destinationFolder);
        Directory.CreateDirectory(fullDest);

        using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                string target = Path.GetFullPath(Path.Combine(fullDest, entry.FullName));
                if (!target.StartsWith(fullDest + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return new AdapterStepResult(false, "download.hash-mismatch", "Archive contained an unsafe path traversal entry.");
                string? dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(target, overwrite: true);
            }
        }

        string? exe = Directory.EnumerateFiles(fullDest, "SU2_CFD.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (exe is null || !File.Exists(exe))
            return new AdapterStepResult(false, "av.quarantined", "SU2_CFD.exe is missing after unpacking.");

        string sha = SolverLauncher.ComputeFileSha256(exe);
        return new AdapterStepResult(true, null, $"SU2_CFD.exe verified (sha256 {sha[..12]}...).");
    }

    public static string ClassifyWindowsFailure(string stepId, int exitCode, string output)
    {
        string text = output ?? "";
        if (exitCode == 1223 || text.Contains("ERROR_CANCELLED", StringComparison.OrdinalIgnoreCase))
            return "win.uac-declined";
        if (text.Contains("0x80370102", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("HCS_E_HYPERV_NOT_INSTALLED", StringComparison.OrdinalIgnoreCase))
            return "win.virtualization-off";
        if (text.Contains("0x8007019e", StringComparison.OrdinalIgnoreCase))
            return "win.wsl-feature-missing";
        if (text.Contains("Could not resolve", StringComparison.OrdinalIgnoreCase))
            return "apt.unreachable";
        if (text.Contains("was not found", StringComparison.OrdinalIgnoreCase) && stepId == "of.install")
            return "apt.version-gone";
        if (text.Contains("No space left on device", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ERROR_DISK_FULL", StringComparison.OrdinalIgnoreCase))
            return "disk.low";
        if (stepId == "wsl.base.import")
            return "wsl.import-failed";
        return "unknown";
    }

    private static void TryDeleteQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
