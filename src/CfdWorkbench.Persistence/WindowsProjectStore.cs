using CfdWorkbench.Core;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace CfdWorkbench.Persistence;

/// <summary>
/// The Windows adapter of <see cref="ProjectStore"/> (W-2 B2; Rulings 135, 136, 145). Every file operation is relative to a
/// retained directory handle: private create (ACL set at creation), FlushFileBuffers on the temp file, then one handle-relative
/// rename with FileRenameInformationEx (POSIX semantics, replace-if-exists only for an overwrite), identity check, owned cleanup.
/// The approved Windows durability level is the file flush plus the atomic rename (Ruling 135): no directory flush is made or
/// claimed, and a power loss in the instant after Save may lose that save. There is no System.IO or path-based fallback.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsProjectStore(StoreHooks? hooks, Action<string, string, double, int?, int?, string, bool?, bool?> emit,
    Func<bool>? isClosed = null)
{
    private const uint ShareAll = WindowsNative.ShareRead | WindowsNative.ShareWrite | WindowsNative.ShareDelete;
    private static readonly Regex ReservedDevice = new(@"\A(CON|PRN|AUX|NUL|COM[0-9¹²³]|LPT[0-9¹²³])(\..*)?\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Windows 10 build 14393 introduced the Ex rename flags (ntifs.h guard); x64 and ARM64 only (Ruling 238).</summary>
    internal static bool PlatformAdmitted() =>
        (RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64) &&
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) && Marshal.SizeOf<WindowsNative.RenameHeader>() == 24;

    internal ReadResult Read(string path, CancellationToken cancellation, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); string code = "OK"; int? count = null;
        try
        {
            ProjectStore.Supported(); cancellation.ThrowIfCancellationRequested();
            using var parent = Parent.Open(path); using var target = OpenRegular(parent, parent.Name);
            byte[] bytes = ReadAll(target, cancellation);
            parent.Verify(); Require(SameEntry(parent, parent.Name, target), "DOC-CONFLICT"); count = bytes.Length;
            return new ReadResult(bytes, Identity.Sha256(bytes));
        }
        catch (OperationCanceledException) { code = "DOC-CANCELLED"; throw new ContractError(code); }
        catch (ContractError error) { code = error.Code; throw; }
        catch (Exception error) when (IsStoreFailure(error)) { code = CodeOf(error); throw new ContractError(code); }
        finally { emit("store.read", code, timer.Elapsed.TotalMilliseconds, count, count, traceId, null, null); }
    }

    internal SaveResult Save(string path, SaveRequest request, CancellationToken cancellation, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        bool published = false;
        byte[] image = request.Image; string hash = Identity.Sha256(image);
        Parent? parent = null; Owned? claim = null, temp = null; SafeFileHandle? target = null;
        string code = "OK"; bool durable = false;
        try
        {
            ProjectStore.Supported(); cancellation.ThrowIfCancellationRequested(); parent = Parent.Open(path);
            hooks?.Visit(StoreStage.ParentOpened);
            _ = Exists(parent, parent.Name);
            if (!request.CreateOnly)
            {
                claim = Owned.Create(parent, ProjectStore.ClaimName()); hooks?.Visit(StoreStage.ClaimCreated);
                target = OpenRegular(parent, parent.Name);
                byte[] disk = ReadAll(target, cancellation);
                Require(Identity.Sha256(disk) == request.ExpectedDiskSha256, "DOC-CONFLICT");
                // Design section 5: an overwrite replaces only an object that already carries the private owner/DACL policy.
                Require(Owned.HasPrivateDescriptor(target), "DOC-UNSUPPORTED-PERSISTENCE");
                // ADR-0011 section 6: the first save that turns a -1 file into -2 keeps the -1 bytes beside it before the publish.
                if (NativeProject.FormatOf(image) == "cfdw-project-2" && NativeProject.FormatOf(disk) == "cfdw-project-1")
                    WriteBackup(parent, disk, request.OperationId, cancellation, traceId);
            }
            temp = Owned.Create(parent, ProjectStore.TempName(request.OperationId)); hooks?.Visit(StoreStage.TempCreated);
            WriteAll(temp.Handle, image, cancellation);
            Flush(temp.Handle, image.Length, traceId);
            hooks?.Visit(StoreStage.FileFlushed);
            cancellation.ThrowIfCancellationRequested(); hooks?.Visit(StoreStage.BeforePublish);
            cancellation.ThrowIfCancellationRequested(); parent.Verify();
            Require(temp.IsOwned && (claim is null || claim.IsOwned), "DOC-CONFLICT");
            if (target is not null)
            {
                Require(SameEntry(parent, parent.Name, target), "DOC-CONFLICT");
                // Re-read through a freshly held handle immediately before cooperative publication.
                using var fresh = OpenRegular(parent, parent.Name);
                Require(WindowsNative.Identity(target) == WindowsNative.Identity(fresh) &&
                    Identity.Sha256(ReadAll(fresh, cancellation)) == request.ExpectedDiskSha256, "DOC-CONFLICT");
            }
            var publicationTimer = System.Diagnostics.Stopwatch.StartNew(); string publicationCode = "OK";
            try
            {
                // One handle-relative rename (class 65): replace only for an overwrite, never an absent-check/rename fallback.
                if (hooks?.PublicationError is int error) throw new WindowsNative.NativeFailure(null, error);
                WindowsNative.Rename(temp.Handle, parent.Handle, parent.Name, replace: target is not null);
                published = true;
            }
            catch (Exception failure) when (IsStoreFailure(failure))
            {
                string mapped = CodeOf(failure);
                publicationCode = mapped is "DOC-CONFLICT" or "DOC-UNSUPPORTED-PERSISTENCE" ? mapped : "DOC-SAVE-UNCERTAIN";
                throw new ContractError(publicationCode);
            }
            finally { emit("store.publish", publicationCode, publicationTimer.Elapsed.TotalMilliseconds, image.Length, published ? image.Length : 0, traceId, published, false); }
            var publishedIdentity = temp.Transfer();
            hooks?.Visit(StoreStage.Published);
            using (var held = OpenRegular(parent, parent.Name))
                Require(WindowsNative.Identity(held) == publishedIdentity && Identity.Sha256(ReadAll(held, CancellationToken.None)) == hash, "DOC-CONFLICT");
            parent.Verify();
            // Cancellation after publication does not manufacture a not-saved result.
            Require(claim?.Cleanup() ?? true, "DOC-IO");
            hooks?.Visit(StoreStage.BeforeDirectoryFlush);
            durable = true;
        }
        catch (OperationCanceledException) { code = published ? "DOC-SAVE-UNCERTAIN" : "DOC-CANCELLED"; }
        catch (ContractError error) { code = published ? "DOC-SAVE-UNCERTAIN" : error.Code; }
        catch (Exception error) when (IsStoreFailure(error)) { code = published ? "DOC-SAVE-UNCERTAIN" : CodeOf(error); }
        finally
        {
            // Cleanup is handle-relative and removes only an entry still matching its retained owned handle.
            // A collided/replaced name is preserved. A cleanup failure is never reported as a clean success.
            bool cleaned = (temp?.Cleanup() ?? true) & (claim?.Cleanup() ?? true);
            if (!cleaned && code == "OK") { code = published ? "DOC-SAVE-UNCERTAIN" : "DOC-IO"; durable = false; }
            target?.Dispose(); temp?.Dispose(); claim?.Dispose(); parent?.Dispose();
        }
        emit("store.save", code, timer.Elapsed.TotalMilliseconds, image.Length, published ? image.Length : 0, traceId, published, durable && code == "OK");
        return new(code, published ? hash : null, published, durable && code == "OK");
    }

    /// <summary>Claim-scoped create-only publication for an immutable, hash-named library entry.</summary>
    internal SaveResult PublishUnderClaim(string dir, string fileName, byte[] bytes, Func<bool> precondition)
    {
        string traceId = Guid.NewGuid().ToString("N");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        string code = "OK", hash = Identity.Sha256(bytes);
        bool published = false, durable = false;
        Parent? parent = null; Owned? claim = null, temp = null;
        try
        {
            Require(!(isClosed?.Invoke() ?? false), "DOC-CLOSED");
            ProjectStore.Supported();
            Require(bytes.Length <= 1_048_576 &&
                Regex.IsMatch(fileName, @"\A[0-9a-f]{64}\.foil\z") && fileName == hash + ".foil", "DOC-HASH");
            parent = Parent.Open(Path.Combine(dir, fileName));
            hooks?.Visit(StoreStage.ParentOpened);
            claim = Owned.Create(parent, ProjectStore.ClaimName());
            hooks?.Visit(StoreStage.ClaimCreated);
            Require(precondition(), "DOC-CONFLICT");
            Require(!Exists(parent, parent.Name), "DOC-CONFLICT");
            temp = Owned.Create(parent, ProjectStore.TempName(Guid.NewGuid().ToString("D")));
            hooks?.Visit(StoreStage.TempCreated);
            WriteAll(temp.Handle, bytes, CancellationToken.None);
            Flush(temp.Handle, bytes.Length, traceId); hooks?.Visit(StoreStage.FileFlushed);
            hooks?.Visit(StoreStage.BeforePublish);
            parent.Verify(); Require(claim.IsOwned && temp.IsOwned, "DOC-CONFLICT");
            try
            {
                if (hooks?.PublicationError is int error) throw new WindowsNative.NativeFailure(null, error);
                WindowsNative.Rename(temp.Handle, parent.Handle, parent.Name, replace: false);
                published = true;
            }
            catch (Exception failure) when (IsStoreFailure(failure)) { throw new ContractError(CodeOf(failure)); }
            var publishedIdentity = temp.Transfer();
            hooks?.Visit(StoreStage.Published);
            using (var held = OpenRegular(parent, parent.Name))
                Require(WindowsNative.Identity(held) == publishedIdentity && Identity.Sha256(ReadAll(held, CancellationToken.None)) == hash, "DOC-CONFLICT");
            Require(claim.Cleanup(), "DOC-IO");
            hooks?.Visit(StoreStage.BeforeDirectoryFlush);
            durable = true;
        }
        catch (ContractError error) { code = published ? "DOC-SAVE-UNCERTAIN" : error.Code; }
        catch (Exception error) when (IsStoreFailure(error)) { code = published ? "DOC-SAVE-UNCERTAIN" : CodeOf(error); }
        finally
        {
            bool cleaned = (temp?.Cleanup() ?? true) & (claim?.Cleanup() ?? true);
            if (!cleaned && code == "OK") code = published ? "DOC-SAVE-UNCERTAIN" : "DOC-IO";
            temp?.Dispose(); claim?.Dispose(); parent?.Dispose();
            emit("library.save", code, timer.Elapsed.TotalMilliseconds, bytes.Length, published ? bytes.Length : 0,
                traceId, published, durable && code == "OK");
        }
        return new(code, published ? hash : null, published, durable && code == "OK");
    }

    // Writes <name>.v1.bak by private temp, flush, no-replace rename. An existing backup is never overwritten: the first -1
    // image is the one an older build needs. A failure propagates, so the save fails before the project is replaced.
    private void WriteBackup(Parent parent, byte[] disk, string operationId, CancellationToken cancellation, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        string name = ProjectStore.BackupName(parent.Name);
        if (Exists(parent, name))
        {
            emit("store.backup", "EXISTS", timer.Elapsed.TotalMilliseconds, disk.Length, 0, traceId, false, false);
            return;
        }
        string code = "OK"; bool linked = false; Owned? temp = null;
        try
        {
            temp = Owned.Create(parent, ProjectStore.BackupTempName(operationId));
            WriteAll(temp.Handle, disk, cancellation);
            WindowsNative.Flush(temp.Handle);
            cancellation.ThrowIfCancellationRequested(); parent.Verify();
            try { WindowsNative.Rename(temp.Handle, parent.Handle, name, replace: false); linked = true; temp.Transfer(); }
            // ERROR_FILE_EXISTS / ERROR_ALREADY_EXISTS: another writer made the backup first; it is kept, never replaced.
            catch (WindowsNative.NativeFailure failure) when (failure.Win32Error is 80 or 183) { code = "EXISTS"; }
            catch (Exception failure) when (IsStoreFailure(failure)) { throw new ContractError(CodeOf(failure)); }
            Require(temp.Cleanup(), "DOC-IO");
        }
        catch (ContractError error) { code = error.Code; throw; }
        catch (OperationCanceledException) { code = "DOC-CANCELLED"; throw; }
        finally
        {
            temp?.Cleanup(); temp?.Dispose();
            emit("store.backup", code, timer.Elapsed.TotalMilliseconds, disk.Length, linked ? disk.Length : 0, traceId, linked, linked && code == "OK");
        }
    }

    private void Flush(SafeFileHandle file, int length, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); string code = "OK";
        try { WindowsNative.Flush(file); }
        catch (Exception error) when (IsStoreFailure(error)) { code = CodeOf(error); throw new ContractError(code); }
        finally { emit("store.file-flush", code, timer.Elapsed.TotalMilliseconds, length, null, traceId, false, false); }
    }

    private void WriteAll(SafeFileHandle file, byte[] image, CancellationToken cancellation)
    {
        int offset = 0;
        while (offset < image.Length)
        {
            cancellation.ThrowIfCancellationRequested(); hooks?.Visit(StoreStage.Writing);
            if (hooks?.FailWriteAfter is int failure && offset >= failure) throw new ContractError("DOC-DISK-FULL");
            int length = Math.Min(image.Length - offset, Math.Min(65536, hooks?.WriteFragment ?? 65536));
            Require(length > 0, "DOC-IO");
            RandomAccess.Write(file, image.AsSpan(offset, length), offset); offset += length;
        }
    }

    private static byte[] ReadAll(SafeFileHandle file, CancellationToken cancellation)
    {
        long length = RandomAccess.GetLength(file); Require(length is >= 0 and <= NativeProject.MaxBytes, "DOC-SIZE");
        byte[] bytes = new byte[length]; int offset = 0;
        while (offset < bytes.Length)
        {
            cancellation.ThrowIfCancellationRequested();
            int count = RandomAccess.Read(file, bytes.AsSpan(offset, Math.Min(65536, bytes.Length - offset)), offset);
            Require(count > 0, "DOC-CONFLICT"); offset += count;
        }
        Require(RandomAccess.GetLength(file) == length, "DOC-CONFLICT");
        return bytes;
    }

    /// <summary>A regular, single-link, not-pending-delete file with no reparse tag, opened relative to the retained parent.</summary>
    private static SafeFileHandle OpenRegular(Parent parent, string name, uint share = WindowsNative.ShareRead | WindowsNative.ShareDelete)
    {
        var handle = WindowsNative.OpenReader(parent.Handle, name, share);
        try
        {
            Require((WindowsNative.AttributeTag(handle).Attributes & (WindowsNative.AttributeDirectory | WindowsNative.AttributeReparsePoint)) == 0,
                "DOC-UNSUPPORTED-PERSISTENCE");
            var standard = WindowsNative.Standard(handle);
            Require(standard.DeletePending == 0, "DOC-CONFLICT");
            Require(standard.NumberOfLinks == 1, "DOC-UNSUPPORTED-PERSISTENCE");
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }

    private static bool Exists(Parent parent, string name)
    {
        try { using var handle = OpenRegular(parent, name, ShareAll); return true; }
        catch (WindowsNative.NativeFailure failure) when (failure.Win32Error is 2 or 3) { return false; } // FILE_NOT_FOUND, PATH_NOT_FOUND
    }

    private static bool SameEntry(Parent parent, string name, SafeFileHandle held)
    {
        try { using var current = WindowsNative.OpenReader(parent.Handle, name, ShareAll); return WindowsNative.Identity(current) == WindowsNative.Identity(held); }
        catch (WindowsNative.NativeFailure failure) when (failure.Win32Error is 2 or 3) { return false; }
    }

    private static void Require(bool condition, string code) { if (!condition) throw new ContractError(code); }

    private static bool IsStoreFailure(Exception error) =>
        error is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    /// <summary>Rulings 145 and 152 (4): a Win32 error with a qualified product code keeps it (32 is DOC-CONFLICT); every other
    /// error is mapped here by the listed winerror.h values, and anything unlisted is DOC-IO, never a guessed code.</summary>
    private static string CodeOf(Exception error) => error switch
    {
        ContractError contract => contract.Code,
        WindowsNative.NativeFailure failure => failure.ProductCode ?? failure.Win32Error switch
        {
            // ERROR_FILE_NOT_FOUND, ERROR_PATH_NOT_FOUND, ERROR_FILE_EXISTS, ERROR_ALREADY_EXISTS.
            2 or 3 or 80 or 183 => "DOC-CONFLICT",
            // ERROR_HANDLE_DISK_FULL, ERROR_DISK_FULL.
            39 or 112 => "DOC-DISK-FULL",
            // ERROR_INVALID_FUNCTION, ERROR_NOT_SUPPORTED, ERROR_INVALID_NAME, ERROR_BAD_PATHNAME, ERROR_FILENAME_EXCED_RANGE,
            // ERROR_DIRECTORY, ERROR_REPARSE_POINT_ENCOUNTERED.
            1 or 50 or 123 or 161 or 206 or 267 or 4390 => "DOC-UNSUPPORTED-PERSISTENCE",
            _ => "DOC-IO"
        },
        DllNotFoundException or EntryPointNotFoundException or BadImageFormatException => "DOC-UNSUPPORTED-PERSISTENCE",
        _ => "DOC-IO"
    };

    internal static bool ValidComponent(string name) =>
        name.Length is >= 1 and <= 255 && name is not "." and not ".." && name[^1] is not ('.' or ' ') &&
        name.IndexOfAny(['\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0']) < 0 && name.All(c => c >= ' ') && !ReservedDevice.IsMatch(name);

    /// <summary>Drive-letter absolute paths only. Bounds: 32,767 UTF-16 units, 256 components, no .cfd- leaf (design section 4).</summary>
    internal static (string Root, string[] Parts) ParsePath(string path)
    {
        Require(path.Length is >= 4 and <= 32767 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] == '\\', "DOC-UNSUPPORTED-PERSISTENCE");
        try { _ = new UTF8Encoding(false, true).GetBytes(path); } catch (EncoderFallbackException) { throw new ContractError("DOC-UNSUPPORTED-PERSISTENCE"); }
        string[] parts = path[3..].Split('\\');
        Require(parts.Length is >= 1 and <= 256 && parts.All(ValidComponent), "DOC-UNSUPPORTED-PERSISTENCE");
        Require(!parts[^1].StartsWith(".cfd-", StringComparison.OrdinalIgnoreCase), "DOC-UNSUPPORTED-PERSISTENCE");
        return (path[..3], parts);
    }

    /// <summary>The retained chain of directory handles from the drive root to the leaf's parent.</summary>
    private sealed class Parent : IDisposable
    {
        private readonly List<(SafeFileHandle Handle, string Name)> chain = [];
        internal SafeFileHandle Handle => chain[^1].Handle;
        internal string Name { get; private set; } = "";
        internal static Parent Open(string path)
        {
            var (root, parts) = ParsePath(path);
            var result = new Parent { Name = parts[^1] };
            try
            {
                var rootHandle = WindowsNative.OpenRoot(root); result.chain.Add((rootHandle, ""));
                // The qualified candidate is a local NTFS volume (design section 4); ReFS, SMB and removable media stay closed.
                Require(WindowsNative.VolumeFileSystem(rootHandle) == "NTFS", "DOC-UNSUPPORTED-PERSISTENCE");
                RequireDirectory(rootHandle);
                foreach (string part in parts.SkipLast(1))
                {
                    var handle = WindowsNative.OpenDirectory(result.Handle, part); result.chain.Add((handle, part));
                    RequireDirectory(handle);
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        // Every ancestor is a real directory (no symlink, junction, mount point or cloud tag) in a case-insensitive directory.
        private static void RequireDirectory(SafeFileHandle directory)
        {
            var tag = WindowsNative.AttributeTag(directory);
            Require((tag.Attributes & WindowsNative.AttributeDirectory) != 0 && (tag.Attributes & WindowsNative.AttributeReparsePoint) == 0,
                "DOC-UNSUPPORTED-PERSISTENCE");
            Require(!WindowsNative.IsCaseSensitive(directory), "DOC-UNSUPPORTED-PERSISTENCE");
        }
        internal void Verify()
        {
            for (int i = 1; i < chain.Count; i++)
            {
                using var again = WindowsNative.OpenDirectory(chain[i - 1].Handle, chain[i].Name);
                Require(WindowsNative.Identity(again) == WindowsNative.Identity(chain[i].Handle), "DOC-CONFLICT");
            }
        }
        public void Dispose() { for (int i = chain.Count - 1; i >= 0; i--) chain[i].Handle.Dispose(); }
    }

    /// <summary>A claim or temp file this save created: private descriptor checked before any byte, removed only through its own handle.</summary>
    private sealed class Owned(Parent parent, string name, SafeFileHandle handle) : IDisposable
    {
        private bool released, transferred;
        private WindowsNative.FileIdentity identity;
        internal SafeFileHandle Handle => handle;
        internal bool IsOwned => !released && !transferred && SameEntry(parent, name, handle);
        internal static Owned Create(Parent parent, string name)
        {
            Require(ValidComponent(name), "DOC-UNSUPPORTED-PERSISTENCE");
            var entry = new Owned(parent, name, WindowsNative.CreatePrivate(parent.Handle, name));
            try
            {
                entry.identity = WindowsNative.Identity(entry.Handle);
                // Effective owner and DACL are read back through the created handle before the first payload byte.
                Require(HasPrivateDescriptor(entry.Handle) && RandomAccess.GetLength(entry.Handle) == 0, "DOC-UNSUPPORTED-PERSISTENCE");
                return entry;
            }
            catch
            {
                bool cleaned = entry.Cleanup(); entry.Dispose();
                if (!cleaned) throw new ContractError("DOC-IO");
                throw;
            }
        }
        internal static bool HasPrivateDescriptor(SafeFileHandle file) =>
            Canonical(WindowsNative.ReadOwnerAndDacl(file)) == Canonical(WindowsNative.PrivateDescriptor());
        // Windows renders a well-known account by its alias, so equal descriptors can differ as strings.
        private static string Canonical(string sddl) =>
            new RawSecurityDescriptor(sddl).GetSddlForm(AccessControlSections.Owner | AccessControlSections.Access);
        /// <summary>After a successful rename the handle denotes the published file: close it without removing anything.</summary>
        internal WindowsNative.FileIdentity Transfer() { transferred = true; handle.Dispose(); return identity; }
        internal bool Cleanup()
        {
            if (released || transferred) return true;
            try
            {
                if (!Exists(parent, name)) { handle.Dispose(); released = true; return true; }
                if (!SameEntry(parent, name, handle)) return false;
                WindowsNative.DisposeEntry(handle);
                handle.Dispose(); // POSIX disposition removes the name when the deleting handle closes.
                released = !Exists(parent, name); return released;
            }
            catch (Exception error) when (IsStoreFailure(error) || error is ContractError) { return false; }
        }
        public void Dispose() => handle.Dispose();
    }
}
