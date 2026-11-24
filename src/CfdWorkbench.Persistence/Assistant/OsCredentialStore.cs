using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Persistence.Assistant;

/// <summary>
/// OS-native BYOK API key store (docs/design/byok-assistants.md §3.1, C1, spec A6 AI-01).
/// <para>
/// Windows: Win32 Credential Manager (<c>advapi32.dll</c> loaded strictly from <c>System32</c>) using
/// DPAPI-protected <c>CRED_TYPE_GENERIC</c> (<c>CRED_PERSIST_LOCAL_MACHINE</c>), <c>&lt;= 2560</c> byte cap,
/// <c>CredFree</c> in <c>finally</c>, and <see cref="CryptographicOperations.ZeroMemory"/> on secret buffers.
/// </para>
/// <para>
/// macOS: Keychain via <c>/usr/bin/security -i</c> fed strictly over UTF-8 <c>stdin</c> (the API key is NEVER
/// passed on process <c>argv</c>), wiping the stdin buffer in <c>finally</c>.
/// </para>
/// <para>
/// Unsupported OS: fails closed with <c>CRED-UNSUPPORTED-OS</c>. Zero plaintext file fallback and zero
/// environment-variable reads anywhere.
/// </para>
/// </summary>
public sealed partial class OsCredentialStore : ICredentialStore
{
    public const string DefaultTargetPrefix = "CfdWorkbench/BYOK";
    public const int MaxCredentialBlobBytes = 2_560;
    private const string CredentialUserName = "cfd-workbench";
    private const string CredentialComment = "CFD-Workbench BYOK API key";
    private const int MacSecurityTimeoutMs = 5_000;

    private readonly string targetPrefix;

    public OsCredentialStore(string targetPrefix = DefaultTargetPrefix)
    {
        if (string.IsNullOrWhiteSpace(targetPrefix))
            throw new ArgumentException("Target prefix is required.", nameof(targetPrefix));
        string trimmed = targetPrefix.Trim();
        foreach (char c in trimmed)
        {
            if (c < 0x20 || c == 0x7F || c == '"' || c == '\\')
                throw new ArgumentException("Target prefix contains disallowed characters.", nameof(targetPrefix));
        }
        this.targetPrefix = trimmed;
    }

    public bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    public bool HasKey(AssistantProviderKind providerKind)
    {
        if (OperatingSystem.IsWindows())
            return WindowsHasKey(TargetName(providerKind));
        if (OperatingSystem.IsMacOS())
            return MacHasKey(TargetName(providerKind));
        return false;
    }

    public CredentialStoreResult ReadKey(AssistantProviderKind providerKind)
    {
        if (OperatingSystem.IsWindows())
            return WindowsReadKey(TargetName(providerKind));
        if (OperatingSystem.IsMacOS())
            return MacReadKey(TargetName(providerKind));
        return UnsupportedOs();
    }

    public CredentialStoreResult WriteKey(AssistantProviderKind providerKind, string apiKey)
    {
        if (!IsSupported)
            return UnsupportedOs();

        if (string.IsNullOrWhiteSpace(apiKey))
            return new CredentialStoreResult(false, null, "CRED-EMPTY", "Enter an API key before saving.");

        foreach (char c in apiKey)
        {
            if (c < 0x20 || c == 0x7F)
                return new CredentialStoreResult(false, null, "CRED-INVALID", "API key contains unsupported control characters.");
        }

        byte[] secretBytes = Encoding.UTF8.GetBytes(apiKey);
        try
        {
            if (secretBytes.Length > MaxCredentialBlobBytes)
            {
                return new CredentialStoreResult(
                    false,
                    null,
                    "CRED-TOO-LARGE",
                    "API key exceeds the 2560-byte OS credential store limit.");
            }

            string target = TargetName(providerKind);
            if (OperatingSystem.IsWindows())
                return WindowsWriteKey(target, secretBytes);
            if (OperatingSystem.IsMacOS())
                return MacWriteKey(target, apiKey);
            return UnsupportedOs();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }

    public CredentialStoreResult DeleteKey(AssistantProviderKind providerKind)
    {
        if (OperatingSystem.IsWindows())
            return WindowsDeleteKey(TargetName(providerKind));
        if (OperatingSystem.IsMacOS())
            return MacDeleteKey(TargetName(providerKind));
        return UnsupportedOs();
    }

    private string TargetName(AssistantProviderKind providerKind) =>
        $"{targetPrefix}/{ProviderSlug(providerKind)}";

    private static string ProviderSlug(AssistantProviderKind providerKind) => providerKind switch
    {
        AssistantProviderKind.Anthropic => "anthropic",
        AssistantProviderKind.OpenAiCompatible => "openai-compatible",
        _ => throw new ArgumentOutOfRangeException(nameof(providerKind))
    };

    private static CredentialStoreResult UnsupportedOs() => new(
        Succeeded: false,
        Secret: null,
        ErrorCode: "CRED-UNSUPPORTED-OS",
        Message: "OS credential store is unavailable on this platform. Keys are never stored in files or environment variables.");

    // ---- Windows Credential Manager (advapi32.dll, DPAPI-protected generic credential) ----

    [SupportedOSPlatform("windows")]
    private static unsafe bool WindowsHasKey(string targetName)
    {
        if (WindowsCredNative.CredReadW(targetName, WindowsCredNative.CredTypeGeneric, 0, out nint ptr) == 0)
            return false;

        try
        {
            var cred = *(WindowsCredNative.NativeCredential*)ptr;
            return cred.CredentialBlob != 0 &&
                   cred.CredentialBlobSize > 0 &&
                   cred.CredentialBlobSize <= MaxCredentialBlobBytes;
        }
        finally
        {
            if (ptr != 0)
                WindowsCredNative.CredFree(ptr);
        }
    }

    [SupportedOSPlatform("windows")]
    private static unsafe CredentialStoreResult WindowsReadKey(string targetName)
    {
        if (WindowsCredNative.CredReadW(targetName, WindowsCredNative.CredTypeGeneric, 0, out nint ptr) == 0)
        {
            int err = Marshal.GetLastPInvokeError();
            if (err == WindowsCredNative.ErrorNotFound)
                return new CredentialStoreResult(false, null, "CRED-NOT-FOUND", "No API key configured.");
            return new CredentialStoreResult(false, null, "CRED-READ-FAILED", $"OS credential read failed (Win32 {err}).");
        }

        try
        {
            var cred = *(WindowsCredNative.NativeCredential*)ptr;
            if (cred.CredentialBlob == 0 || cred.CredentialBlobSize == 0)
                return new CredentialStoreResult(false, null, "CRED-NOT-FOUND", "No API key configured.");
            if (cred.CredentialBlobSize > MaxCredentialBlobBytes)
                return new CredentialStoreResult(false, null, "CRED-TOO-LARGE", "Stored credential exceeds the 2560-byte limit.");

            byte[] managed = new byte[cred.CredentialBlobSize];
            try
            {
                new ReadOnlySpan<byte>((void*)cred.CredentialBlob, managed.Length).CopyTo(managed);
                string secret = Encoding.UTF8.GetString(managed);
                return new CredentialStoreResult(true, secret, null, null);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(managed);
            }
        }
        finally
        {
            if (ptr != 0)
                WindowsCredNative.CredFree(ptr);
        }
    }

    [SupportedOSPlatform("windows")]
    private static unsafe CredentialStoreResult WindowsWriteKey(string targetName, byte[] secretBytes)
    {
        fixed (char* targetPtr = targetName)
        fixed (char* userPtr = CredentialUserName)
        fixed (char* commentPtr = CredentialComment)
        fixed (byte* blobPtr = secretBytes)
        {
            var cred = new WindowsCredNative.NativeCredential
            {
                Flags = 0,
                Type = WindowsCredNative.CredTypeGeneric,
                TargetName = (nint)targetPtr,
                Comment = (nint)commentPtr,
                LastWrittenLow = 0,
                LastWrittenHigh = 0,
                CredentialBlobSize = (uint)secretBytes.Length,
                CredentialBlob = (nint)blobPtr,
                Persist = WindowsCredNative.CredPersistLocalMachine,
                AttributeCount = 0,
                Attributes = 0,
                TargetAlias = 0,
                UserName = (nint)userPtr
            };

            if (WindowsCredNative.CredWriteW(&cred, 0) != 0)
                return new CredentialStoreResult(true, null, null, null);

            int err = Marshal.GetLastPInvokeError();
            return new CredentialStoreResult(false, null, "CRED-WRITE-FAILED", $"OS credential write failed (Win32 {err}).");
        }
    }

    [SupportedOSPlatform("windows")]
    private static CredentialStoreResult WindowsDeleteKey(string targetName)
    {
        if (WindowsCredNative.CredDeleteW(targetName, WindowsCredNative.CredTypeGeneric, 0) != 0)
            return new CredentialStoreResult(true, null, null, null);

        int err = Marshal.GetLastPInvokeError();
        if (err == WindowsCredNative.ErrorNotFound)
            return new CredentialStoreResult(true, null, null, null);

        return new CredentialStoreResult(false, null, "CRED-DELETE-FAILED", $"OS credential delete failed (Win32 {err}).");
    }

    // ---- macOS Keychain (/usr/bin/security -i via UTF-8 stdin ONLY — zero secrets on argv!) ----

    [SupportedOSPlatform("macos")]
    private static bool MacHasKey(string serviceName)
    {
        string cmd = $"find-generic-password -s \"{EscapeSecurityInteractive(serviceName)}\" -a \"{CredentialUserName}\"\n";
        var (ok, _, _) = RunMacSecurityInteractive(cmd, activeSecret: null);
        return ok;
    }

    [SupportedOSPlatform("macos")]
    private static CredentialStoreResult MacReadKey(string serviceName)
    {
        string cmd = $"find-generic-password -w -s \"{EscapeSecurityInteractive(serviceName)}\" -a \"{CredentialUserName}\"\n";
        var (ok, stdout, stderr) = RunMacSecurityInteractive(cmd, activeSecret: null);
        if (!ok)
        {
            if (stderr.Contains("could not be found", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("SecKeychainSearchCopyNext", StringComparison.OrdinalIgnoreCase))
                return new CredentialStoreResult(false, null, "CRED-NOT-FOUND", "No API key configured.");
            return new CredentialStoreResult(false, null, "CRED-READ-FAILED", "macOS Keychain read failed.");
        }

        string secret = stdout.TrimEnd('\r', '\n');
        if (string.IsNullOrEmpty(secret))
            return new CredentialStoreResult(false, null, "CRED-NOT-FOUND", "No API key configured.");
        if (Encoding.UTF8.GetByteCount(secret) > MaxCredentialBlobBytes)
            return new CredentialStoreResult(false, null, "CRED-TOO-LARGE", "Stored credential exceeds the 2560-byte limit.");

        return new CredentialStoreResult(true, secret, null, null);
    }

    [SupportedOSPlatform("macos")]
    private static CredentialStoreResult MacWriteKey(string serviceName, string apiKey)
    {
        string cmd = $"add-generic-password -U -s \"{EscapeSecurityInteractive(serviceName)}\" -a \"{CredentialUserName}\" -w \"{EscapeSecurityInteractive(apiKey)}\"\n";
        var (ok, _, _) = RunMacSecurityInteractive(cmd, activeSecret: apiKey);
        return ok
            ? new CredentialStoreResult(true, null, null, null)
            : new CredentialStoreResult(false, null, "CRED-WRITE-FAILED", "macOS Keychain write failed.");
    }

    [SupportedOSPlatform("macos")]
    private static CredentialStoreResult MacDeleteKey(string serviceName)
    {
        string cmd = $"delete-generic-password -s \"{EscapeSecurityInteractive(serviceName)}\" -a \"{CredentialUserName}\"\n";
        var (ok, _, stderr) = RunMacSecurityInteractive(cmd, activeSecret: null);
        if (ok || stderr.Contains("could not be found", StringComparison.OrdinalIgnoreCase))
            return new CredentialStoreResult(true, null, null, null);
        return new CredentialStoreResult(false, null, "CRED-DELETE-FAILED", "macOS Keychain delete failed.");
    }

    [SupportedOSPlatform("macos")]
    private static (bool Succeeded, string Stdout, string Stderr) RunMacSecurityInteractive(string stdinCommand, string? activeSecret)
    {
        byte[] commandBytes = Encoding.UTF8.GetBytes(stdinCommand);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/security",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false)
            };
            psi.ArgumentList.Add("-i");

            using var process = Process.Start(psi);
            if (process is null)
                return (false, "", "Failed to start /usr/bin/security.");

            Task<string> outTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errTask = process.StandardError.ReadToEndAsync();

            process.StandardInput.BaseStream.Write(commandBytes, 0, commandBytes.Length);
            process.StandardInput.BaseStream.Flush();
            process.StandardInput.Close();

            if (!process.WaitForExit(MacSecurityTimeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return (false, "", "Keychain command timed out.");
            }

            string stdout = outTask.GetAwaiter().GetResult();
            string stderr = errTask.GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(activeSecret))
            {
                stderr = stderr.Replace(activeSecret, "[REDACTED_SECRET]", StringComparison.Ordinal);
            }

            return (process.ExitCode == 0, stdout, stderr);
        }
        catch (Exception)
        {
            return (false, "", "macOS Keychain invocation error.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(commandBytes);
        }
    }

    private static string EscapeSecurityInteractive(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);

    [SupportedOSPlatform("windows")]
    private static unsafe partial class WindowsCredNative
    {
        internal const uint CredTypeGeneric = 1;
        internal const uint CredPersistLocalMachine = 2;
        internal const int ErrorNotFound = 1168;

        // wincred.h CREDENTIALW layout (80 bytes on x64, 52 bytes on x86).
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeCredential
        {
            internal uint Flags;
            internal uint Type;
            internal nint TargetName;
            internal nint Comment;
            internal uint LastWrittenLow;
            internal uint LastWrittenHigh;
            internal uint CredentialBlobSize;
            internal nint CredentialBlob;
            internal uint Persist;
            internal uint AttributeCount;
            internal nint Attributes;
            internal nint TargetAlias;
            internal nint UserName;
        }

        [LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int CredWriteW(NativeCredential* credential, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredReadW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int CredReadW(string targetName, uint type, uint flags, out nint credential);

        [LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int CredDeleteW(string targetName, uint type, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredFree")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial void CredFree(nint buffer);
    }
}
