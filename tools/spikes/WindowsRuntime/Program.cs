using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

// Disposable ABI experiment, never a production IProjectStore implementation.
// Primary signatures and supported-subset limits are in docs/design/windows-runtime.md.
internal static class Program
{
    private static readonly string[] Cases = ["create", "create-collision", "read", "overwrite", "conflict", "immutable-input",
        "cancel-before", "cancel-after", "write-fault", "replace-fault", "sharing", "dacl-create", "dacl-inheritance",
        "dacl-replacement", "denial", "case-alias", "unicode-alias", "ads-device-unc", "hard-link", "leaf-reparse",
        "ancestor-reparse", "ancestor-substitution", "owned-cleanup", "cleanup-refusal", "file-flush", "directory-durability"];
    private static string source = "", binary = "";
    private static byte[] a = [], b = [];
    private static readonly Dictionary<string, object> evidence = [];
    private static bool published;

    private static int Main(string[] args)
    {
        source = Environment.GetEnvironmentVariable("W0_SOURCE") ?? "not recorded";
        string executable = Environment.ProcessPath!, directory = Path.GetDirectoryName(executable)!;
        string[] binaries = [Path.GetFileName(executable), "WindowsRuntime.dll", "WindowsRuntime.deps.json", "WindowsRuntime.runtimeconfig.json"];
        binary = Hash(Encoding.UTF8.GetBytes(string.Concat(binaries.Order(StringComparer.Ordinal)
            .Select(name => name + "\0" + Hash(File.ReadAllBytes(Path.Combine(directory, name))) + "\n"))));
        if (args.Length == 1 && args[0].StartsWith("--tree-", StringComparison.Ordinal))
            return Tree(args[0]);
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            foreach (string name in Cases) Emit(name, "Not assessed", "W0-UNSUPPORTED-HOST");
            return 3;
        }
        if (args.Length != 3) return 2;
        try
        {
            Require(Marshal.SizeOf<Native.SecurityAttributes>() == 24 && Marshal.SizeOf<Native.FileInfo>() == 52,
                "W0-ABI");
            a = File.ReadAllBytes(args[1]); b = File.ReadAllBytes(args[2]);
            Require(a.Length > 0 && b.Length > 0 && Hash(a) != Hash(b), "W0-FIXTURES");
            string root = Path.GetFullPath(args[0]);
            Require(!Directory.Exists(root), "W0-ROOT-EXISTS");
            using var ancestors = new PinnedPath(Path.GetDirectoryName(root)!);
            CreateDirectory(root, ProtectedSddl());
            int failures = 0;
            foreach (string name in Cases)
            {
                evidence.Clear(); published = false;
                evidence["os"] = RuntimeInformation.OSDescription;
                evidence["architecture"] = RuntimeInformation.ProcessArchitecture.ToString();
                evidence["filesystem"] = new DriveInfo(Path.GetPathRoot(root)!).DriveFormat;
                evidence["fixtureA"] = Hash(a); evidence["fixtureB"] = Hash(b);
                string folder = Path.Combine(root, name);
                CreateDirectory(folder, ProtectedSddl());
                var timer = Stopwatch.StartNew();
                try { Scenario(name, folder); Emit(name, "Pass", "OK", timer.Elapsed.TotalMilliseconds); }
                catch (Unsupported error) { failures++; Emit(name, "Not assessed", "W0-UNSUPPORTED-" + error.Message, timer.Elapsed.TotalMilliseconds); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
                { failures++; evidence["error"] = error.Message; Emit(name, "Fail", "W0-ORACLE-FAILED", timer.Elapsed.TotalMilliseconds); }
            }
            // Retain all fixtures. No recursive path cleanup, including on failure.
            return failures == 0 ? 0 : 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        { Console.Error.WriteLine(JsonSerializer.Serialize(new { code = "W0-SETUP-FAILED", error = error.Message })); return 1; }
    }

    private static void Emit(string name, string status, string code, double milliseconds = 0) =>
        Console.WriteLine(JsonSerializer.Serialize(new { @case = name, status, code, source, binary,
            publication = published, durability = false, elapsedMilliseconds = milliseconds, evidence }));
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Require(bool value, string code) { if (!value) throw new InvalidOperationException(code); }
    private static void Check(bool value) { if (!value) throw new Win32Exception(Marshal.GetLastPInvokeError()); }
    private static string Sid => WindowsIdentity.GetCurrent().User?.Value ?? throw new Unsupported("SID");
    private static string ProtectedSddl() => "O:" + Sid + "D:P(A;;FA;;;" + Sid + ")";

    private static void Scenario(string name, string folder)
    {
        string target = Path.Combine(folder, "target.bin"), temp = Path.Combine(folder, "temp.bin");
        using var parent = new PinnedPath(folder);
        if (name is "create" or "create-collision" or "overwrite" or "conflict" or "immutable-input" or
            "cancel-before" or "cancel-after" or "write-fault" or "replace-fault")
        {
            bool existing = name is "create-collision" or "overwrite" or "conflict" or "cancel-after" or "replace-fault";
            if (existing) { using var old = Owned.Create(target); old.Write(a); }
            byte[] caller = b.ToArray();
            string? expected = existing && name != "create-collision" ? Hash(name == "conflict" ? b : a) : null;
            using var cancellation = new CancellationTokenSource();
            if (name == "cancel-before") cancellation.Cancel();
            SaveObservation result = Publish(target, caller, expected, cancellation.Token,
                afterSnapshot: name == "immutable-input" ? () => caller[0] ^= 0xff : null,
                fault: name is "write-fault" or "replace-fault" ? name : null,
                afterPublish: name == "cancel-after" ? cancellation.Cancel : null);
            published = result.PublicationKnown;
            evidence["saveCode"] = result.Code;
            evidence["candidateDurability"] = result.DurabilityConfirmed;
            evidence["cancellationRequested"] = cancellation.IsCancellationRequested;
            evidence["expectedDiskHash"] = expected ?? "absent";
            string code = name switch
            {
                "cancel-before" => "DOC-CANCELLED",
                "conflict" or "create-collision" => "DOC-CONFLICT",
                "write-fault" => "DOC-IO",
                _ => "DOC-SAVE-UNCERTAIN"
            };
            Require(result.Code == code, "W0-WRONG-SAVE-RESULT");
            bool shouldPublish = name is "create" or "overwrite" or "immutable-input" or "cancel-after";
            Require(published == shouldPublish && !result.DurabilityConfirmed, "W0-WRONG-SAVE-STATE");
            if (published)
            {
                using var fresh = OpenRegular(target);
                Require(Read(fresh).SequenceEqual(b) && result.PublishedSha256 == Hash(b), "W0-WRONG-SAVED-BYTES");
                evidence["after"] = Hash(Read(fresh)); evidence["identityAfter"] = Identity(fresh);
            }
            else if (existing)
            {
                using var preserved = OpenRegular(target);
                Require(Read(preserved).SequenceEqual(a), "W0-ORIGINAL-MUTATED");
                evidence["after"] = Hash(Read(preserved));
            }
            else Require(!File.Exists(target), "W0-UNEXPECTED-PUBLICATION");
            evidence["targetExists"] = File.Exists(target);
            evidence["claimExists"] = File.Exists(Path.Combine(folder, "writer.claim"));
            evidence["tempExists"] = File.Exists(Path.Combine(folder, "staged.bin"));
            Require(!(bool)evidence["claimExists"] && !(bool)evidence["tempExists"], "W0-OWNED-LEFTOVER");
            return;
        }
        if (name == "directory-durability")
        {
            evidence["contract"] = "No accepted equivalent to final-directory fsync; volume flush requires elevation and is forbidden.";
            throw new Unsupported("DIRECTORY-DURABILITY");
        }
        if (name == "unicode-alias")
        {
            RejectPath(Path.Combine(folder, "caf\u00e9.bin"));
            RejectPath(Path.Combine(folder, "cafe\u0301.bin"));
            evidence["rejectedForms"] = 2; return;
        }
        if (name == "ads-device-unc")
        {
            foreach (string path in new[] { target + ":stream", "\\\\server\\share\\target.bin", "\\\\?\\C:\\target.bin",
                Path.Combine(folder, "NUL"), Path.Combine(folder, "target.bin."), Path.Combine(folder, "..", "outside.bin") }) RejectPath(path);
            evidence["rejectedForms"] = 6; return;
        }
        if (name is "leaf-reparse" or "ancestor-reparse")
        {
            string original = Path.Combine(folder, "original");
            CreateDirectory(original, ProtectedSddl());
            string originalFile = Path.Combine(original, "original.bin");
            using (var file = Owned.Create(originalFile)) file.Write(a);
            string link = Path.Combine(folder, "redirect");
            bool directory = name == "ancestor-reparse";
            if (!Native.CreateSymbolicLinkW(link, directory ? original : originalFile, directory ? 3u : 2u))
            {
                int error = Marshal.GetLastPInvokeError(); evidence["win32"] = error;
                if (error is 1314 or 5) throw new Unsupported("REPARSE-CREATION-RIGHTS");
                throw new Win32Exception(error);
            }
            bool rejected = false;
            try
            {
                if (directory) { using var pinned = new PinnedPath(link); }
                else { using var file = OpenRegular(link); }
            }
            catch (Unsupported) { rejected = true; }
            Require(rejected && File.ReadAllBytes(originalFile).SequenceEqual(a), "W0-REDIRECT-FOLLOWED");
            evidence["redirectRejected"] = rejected; evidence["after"] = Hash(File.ReadAllBytes(originalFile)); return;
        }
        if (name == "ancestor-substitution")
        {
            bool moved = Native.MoveFileExW(folder, folder + "-moved", 0);
            int error = Marshal.GetLastPInvokeError();
            Require(!moved && error is 5 or 32, "W0-ANCESTOR-MOVED");
            evidence["win32"] = error; evidence["heldAncestors"] = parent.Identities; return;
        }
        if (name == "dacl-inheritance")
        {
            string permissive = Path.Combine(folder, "permissive");
            CreateDirectory(permissive, "D:(A;OICI;FA;;;WD)");
            using var fixture = Owned.Create(Path.Combine(permissive, "protected.bin"));
            Require(PrivateDacl(fixture.Handle), "W0-INHERITANCE-LEAK");
            using var inherited = Open(Path.Combine(permissive, "inherited.bin"), Native.ReadWrite, 7, 1);
            Require(!PrivateDacl(inherited), "W0-NEGATIVE-DACL-NOT-DETECTED");
            evidence["protectedPrivate"] = PrivateDacl(fixture.Handle);
            evidence["inheritedControlDetected"] = !PrivateDacl(inherited); return;
        }
        if (name == "denial")
        {
            using (var sd = new Descriptor("D:P(D;;GA;;;" + Sid + ")"))
            using (var denied = Open(target, 0, 7, 1, sd)) { }
            using var attempt = Native.CreateFileW(target, Native.ReadWrite, 7, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
            int error = Marshal.GetLastPInvokeError(); Require(attempt.IsInvalid && error == 5, "W0-DENIAL-NOT-ENFORCED");
            evidence["win32"] = error; return;
        }
        using var held = Owned.Create(target); held.Write(a);
        evidence["before"] = Hash(held.Read()); evidence["identityBefore"] = Identity(held.Handle);
        if (name == "read") { Require(held.Read().SequenceEqual(a), "W0-BYTES"); evidence["after"] = Hash(held.Read()); return; }
        if (name is "dacl-create" or "dacl-replacement")
        {
            Require(PrivateDacl(held.Handle), "W0-DACL");
            if (name == "dacl-replacement")
            {
                using var replacement = Owned.Create(temp); replacement.Write(b);
                replacement.Rename(target, true); published = true;
                Require(PrivateDacl(replacement.Handle), "W0-REPLACEMENT-DACL");
                evidence["replacementDacl"] = DescriptorText(replacement.Handle);
            }
            evidence["creationDacl"] = DescriptorText(held.Handle); return;
        }
        if (name == "file-flush") { Check(Native.FlushFileBuffers(held.Handle)); evidence["fileFlushed"] = true; return; }
        if (name == "sharing")
        {
            using var writer = Native.CreateFileW(target, Native.ReadWrite, 7, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
            int error = Marshal.GetLastPInvokeError(); Require(writer.IsInvalid && error == 32, "W0-WRITER-NOT-EXCLUDED");
            evidence["win32"] = error; return;
        }
        if (name == "case-alias")
        {
            using var alias = Open(target.ToUpperInvariant(), 0, 7, 3);
            Require(Identity(alias) == Identity(held.Handle), "W0-CASE-SENSITIVE-DIRECTORY");
            evidence["aliasIdentity"] = Identity(alias); return;
        }
        if (name == "hard-link")
        {
            Check(Native.CreateHardLinkW(temp, target, IntPtr.Zero));
            bool rejected = false;
            try { using var linked = OpenRegular(temp); } catch (Unsupported) { rejected = true; }
            Require(rejected && held.Read().SequenceEqual(a), "W0-HARDLINK-ACCEPTED");
            evidence["links"] = Info(held.Handle).Links; evidence["after"] = Hash(held.Read()); return;
        }
        if (name == "owned-cleanup")
        {
            bool deleted = held.Cleanup();
            Require(deleted && !File.Exists(target), "W0-CLEANUP");
            evidence["ownedDeleted"] = deleted; evidence["targetExists"] = File.Exists(target); return;
        }
        if (name == "cleanup-refusal")
        {
            held.Rename(temp, false); // Owned original moved; foreign entry occupies the old name.
            using var foreign = Owned.Create(target); foreign.Write(b);
            bool refused = !held.Cleanup();
            Require(refused && foreign.Read().SequenceEqual(b), "W0-FOREIGN-DELETED");
            evidence["foreignAfter"] = Hash(foreign.Read()); evidence["cleanupRefused"] = refused;
            evidence["targetExists"] = File.Exists(target); evidence["ownedOriginalExists"] = File.Exists(temp); return;
        }
        throw new InvalidOperationException("W0-UNKNOWN-CASE");
    }

    private sealed record SaveObservation(string Code, string? PublishedSha256, bool PublicationKnown, bool DurabilityConfirmed);
    private static SaveObservation Publish(string path, byte[] input, string? expected, CancellationToken cancellation,
        Action? afterSnapshot = null, string? fault = null, Action? afterPublish = null)
    {
        byte[] snapshot = input.ToArray(); afterSnapshot?.Invoke();
        bool known = false; string code = "DOC-SAVE-UNCERTAIN";
        Owned? claim = null, staged = null;
        SafeFileHandle? target = null;
        using var parent = new PinnedPath(Path.GetDirectoryName(path)!);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            claim = Owned.Create(Path.Combine(Path.GetDirectoryName(path)!, "writer.claim"));
            if (expected is not null)
            {
                target = OpenRegular(path);
                if (!PrivateDacl(target)) throw new Unsupported("OVERWRITE-DACL");
                if (Hash(Read(target)) != expected) return new("DOC-CONFLICT", null, false, false);
            }
            staged = Owned.Create(Path.Combine(Path.GetDirectoryName(path)!, "staged.bin"));
            if (fault == "write-fault")
            { staged.Write(snapshot[..1]); evidence["writtenBeforeFault"] = checked((int)RandomAccess.GetLength(staged.Handle)); throw new IOException("injected partial-write failure"); }
            staged.Write(snapshot);
            cancellation.ThrowIfCancellationRequested();
            if (target is not null)
            {
                using var fresh = OpenRegular(path);
                if (Identity(fresh) != Identity(target) || Hash(Read(fresh)) != expected)
                    return new("DOC-CONFLICT", null, false, false);
            }
            try
            {
                staged.Rename(fault == "replace-fault" ? Path.Combine(Path.GetDirectoryName(path)!, "missing", "target.bin") : path, expected is not null);
                known = true; published = true;
            }
            catch (Win32Exception error) when (error.NativeErrorCode is 80 or 183)
            { evidence["nativeError"] = error.NativeErrorCode; code = "DOC-CONFLICT"; }
            catch (Win32Exception error) { evidence["nativeError"] = error.NativeErrorCode; code = "DOC-SAVE-UNCERTAIN"; }
            if (known)
            {
                afterPublish?.Invoke();
                using var fresh = OpenRegular(path);
                Require(Identity(fresh) == Identity(staged.Handle) && Read(fresh).SequenceEqual(snapshot), "W0-PUBLISH-IDENTITY");
            }
        }
        catch (OperationCanceledException) { code = known ? "DOC-SAVE-UNCERTAIN" : "DOC-CANCELLED"; }
        catch (IOException) { code = known ? "DOC-SAVE-UNCERTAIN" : "DOC-IO"; }
        finally
        {
            // Deleted by retained handle only; moved/collided entries are preserved and refuse success.
            bool clean = true;
            try
            {
                if (!known && staged is not null) clean &= staged.Cleanup();
                if (claim is not null) clean &= claim.Cleanup();
            }
            finally { target?.Dispose(); staged?.Dispose(); claim?.Dispose(); }
            Require(clean, "W0-CLEANUP-REFUSED");
        }
        return new(code, known ? Hash(snapshot) : null, known, false);
    }

    private static void ValidatePath(string path)
    {
        if (!Regex.IsMatch(path, @"\A[A-Za-z]:\\[A-Za-z0-9_. \\-]+\z") || path.Length > 240)
            throw new Unsupported("PATH");
        foreach (string part in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
            if (part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                Regex.IsMatch(part, @"\A(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|\z)", RegexOptions.IgnoreCase))
                throw new Unsupported("PATH");
    }
    private static void RejectPath(string path)
    {
        bool rejected = false;
        try { ValidatePath(path); } catch (Unsupported) { rejected = true; }
        Require(rejected, "W0-PATH-ACCEPTED");
    }
    private sealed class PinnedPath : IDisposable
    {
        private readonly List<SafeFileHandle> handles = [];
        public readonly List<string> Identities = [];
        public PinnedPath(string path)
        {
            ValidatePath(path);
            var drive = new DriveInfo(Path.GetPathRoot(path)!);
            if (drive.DriveType != DriveType.Fixed || drive.DriveFormat != "NTFS") throw new Unsupported("FILESYSTEM");
            try
            {
                string cursor = Path.GetPathRoot(path)!;
                Pin(cursor);
                foreach (string part in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
                { cursor = Path.Combine(cursor, part); Pin(cursor); }
            }
            catch { Dispose(); throw; }
        }
        private void Pin(string path)
        {
            // Keep every ancestor without FILE_SHARE_DELETE until all file operations finish.
            var handle = Open(path, 0, 3, 3, flags: 0x02200000);
            handles.Add(handle);
            var info = Info(handle);
            if ((info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0) throw new Unsupported("ANCESTOR-REPARSE");
            Identities.Add(Identity(handle));
        }
        public void Dispose() { foreach (var handle in handles) handle.Dispose(); }
    }
    private sealed class Unsupported(string message) : InvalidOperationException(message);
    private sealed class Descriptor : IDisposable
    {
        public IntPtr Pointer;
        public Descriptor(string sddl) => Check(Native.ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, 1, out Pointer, out _));
        public void Dispose() { if (Pointer != IntPtr.Zero) { Native.LocalFree(Pointer); Pointer = IntPtr.Zero; } }
    }
    private static void CreateDirectory(string path, string sddl)
    {
        using var descriptor = new Descriptor(sddl);
        var attributes = new Native.SecurityAttributes { Length = 24, Descriptor = descriptor.Pointer };
        Check(Native.CreateDirectoryW(path, ref attributes));
    }
    private static SafeFileHandle Open(string path, uint access, uint share, uint disposition, Descriptor? descriptor = null, uint flags = 0x00200000)
    {
        IntPtr pointer = IntPtr.Zero;
        try
        {
            if (descriptor is not null)
            {
                pointer = Marshal.AllocHGlobal(24);
                Marshal.StructureToPtr(new Native.SecurityAttributes { Length = 24, Descriptor = descriptor.Pointer }, pointer, false);
            }
            var handle = Native.CreateFileW(path, access, share, pointer, disposition, flags, IntPtr.Zero);
            if (handle.IsInvalid) { int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new Win32Exception(error); }
            return handle;
        }
        finally { if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer); }
    }
    private static SafeFileHandle OpenRegular(string path)
    {
        ValidatePath(path);
        // Permit the already-retained writer handle; it still denies new writers.
        var handle = Open(path, 0x80000000, 7, 3);
        try
        {
            var info = Info(handle);
            if ((info.Attributes & 0x410) != 0 || info.Links != 1) throw new Unsupported("FILE-KIND");
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }
    private static Native.FileInfo Info(SafeFileHandle handle) { Check(Native.GetFileInformationByHandle(handle, out var info)); return info; }
    private static string Identity(SafeFileHandle handle)
    { var i = Info(handle); return $"{i.Volume:x8}:{i.IndexHigh:x8}{i.IndexLow:x8}"; }
    private static byte[] Read(SafeFileHandle handle)
    {
        long length = RandomAccess.GetLength(handle); Require(length <= 1024 * 1024, "W0-FIXTURE-SIZE");
        byte[] bytes = new byte[checked((int)length)]; int read = 0;
        while (read < bytes.Length) { int count = RandomAccess.Read(handle, bytes.AsSpan(read), read); Require(count > 0, "W0-SHORT-READ"); read += count; }
        return bytes;
    }
    private static string DescriptorText(SafeFileHandle handle)
    {
        uint error = Native.GetSecurityInfo(handle, 1, 7, out _, out _, out _, out _, out var descriptor);
        if (error != 0) throw new Win32Exception((int)error);
        try
        {
            int size = checked((int)Native.GetSecurityDescriptorLength(descriptor)); byte[] bytes = new byte[size];
            Marshal.Copy(descriptor, bytes, 0, size);
            return new RawSecurityDescriptor(bytes, 0).GetSddlForm(AccessControlSections.Owner | AccessControlSections.Access);
        }
        finally { Native.LocalFree(descriptor); }
    }
    private static bool PrivateDacl(SafeFileHandle handle)
    {
        var descriptor = new RawSecurityDescriptor(DescriptorText(handle));
        return (descriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0 &&
            descriptor.DiscretionaryAcl is { Count: 1 } acl && acl[0] is CommonAce ace &&
            ace.AceQualifier == AceQualifier.AccessAllowed && !ace.IsInherited && ace.SecurityIdentifier.Value == Sid &&
            ace.AccessMask == 0x1f01ff;
    }
    private sealed class Owned(string path, SafeFileHandle handle) : IDisposable
    {
        public SafeFileHandle Handle { get; } = handle;
        public static Owned Create(string path)
        {
            using var descriptor = new Descriptor(ProtectedSddl());
            var handle = Open(path, Native.ReadWrite | 0x10000 | 0x20000, 5, 1, descriptor);
            try { Require(PrivateDacl(handle), "W0-DACL-BEFORE-PAYLOAD"); return new Owned(path, handle); }
            catch { handle.Dispose(); throw; }
        }
        public void Write(byte[] bytes) { RandomAccess.Write(Handle, bytes, 0); Check(Native.FlushFileBuffers(Handle)); }
        public byte[] Read() => Program.Read(Handle);
        public void Rename(string target, bool replace)
        {
            // WinBase.h FILE_RENAME_INFO x64: union at 0, HANDLE at 8, DWORD at 16, WCHAR at 20.
            byte[] name = Encoding.Unicode.GetBytes(target); byte[] buffer = new byte[20 + name.Length + 2];
            buffer[0] = replace ? (byte)1 : (byte)0; BitConverter.GetBytes(name.Length).CopyTo(buffer, 16); name.CopyTo(buffer, 20);
            Check(Native.SetFileInformationByHandle(Handle, 3, buffer, (uint)buffer.Length));
        }
        public bool Cleanup()
        {
            using var entry = Open(path, 0, 7, 3);
            if (Identity(entry) != Identity(Handle)) return false;
            Check(Native.SetFileInformationByHandle(Handle, 4, [1], 1));
            Handle.Dispose(); return true;
        }
        public void Dispose() => Handle.Dispose();
    }
    private static int Tree(string mode)
    {
        if (!OperatingSystem.IsWindows()) return 3;
        if (mode != "--tree-grandchild")
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            start.ArgumentList.Add(mode == "--tree-root" ? "--tree-child" : "--tree-grandchild");
            using var child = Process.Start(start) ?? throw new InvalidOperationException("W0-CHILD");
        }
        // Finite timeout target, owned by the driver's already-assigned job. No detached child.
        using var wait = new ManualResetEvent(false); wait.WaitOne(TimeSpan.FromSeconds(30)); return 0;
    }
}

internal static class Native
{
    internal const uint ReadWrite = 0xc0000000;
    [StructLayout(LayoutKind.Sequential)] internal struct SecurityAttributes { public uint Length; public IntPtr Descriptor; public int Inherit; }
    [StructLayout(LayoutKind.Sequential)] internal struct FileInfo
    { public uint Attributes, CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh, Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateDirectoryW(string path, ref SecurityAttributes security);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInfo info);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetFileInformationByHandle(SafeFileHandle handle, int infoClass, byte[] info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlushFileBuffers(SafeFileHandle handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MoveFileExW(string source, string target, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool CreateSymbolicLinkW(string link, string target, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateHardLinkW(string link, string target, IntPtr security);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string sddl, uint revision, out IntPtr descriptor, out uint size);
    [DllImport("advapi32.dll")] internal static extern uint GetSecurityInfo(SafeFileHandle handle, int type, uint information,
        out IntPtr owner, out IntPtr group, out IntPtr dacl, out IntPtr sacl, out IntPtr descriptor);
    [DllImport("advapi32.dll")] internal static extern uint GetSecurityDescriptorLength(IntPtr descriptor);
    [DllImport("kernel32.dll")] internal static extern IntPtr LocalFree(IntPtr memory);
}
