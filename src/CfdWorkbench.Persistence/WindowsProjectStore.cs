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
        // Placeholders are exposed for this whole synchronous call (thread-scoped; no await in this class).
        using var exposure = WindowsNative.ExposePlaceholders();
        var timer = System.Diagnostics.Stopwatch.StartNew(); string code = "OK"; int? count = null;
        try
        {
            ProjectStore.Supported(); cancellation.ThrowIfCancellationRequested();
            using var parent = Parent.Open(path, ParentMode.OpenRead, hooks);                       // O1
            var origin = OriginOf(path, parent);                                                    // O2
            using var attribute = WindowsNative.OpenAttributes(parent.Handle, parent.Name);         // O3: attribute handle A, no data access
            AdmitLeaf(attribute, parent.Name, origin);                                              // O3: the only DOC-CLOUD-ONLY point (I-14)
            hooks?.Visit(StoreStage.AttributeHandleOpened);
            byte[] bytes;
            try
            {
                using var data = WindowsNative.OpenDataFromHeld(attribute);                         // O4: data handle D from A, empty name (S1)
                hooks?.Visit(StoreStage.DataHandleOpened);
                Require(WindowsNative.Identity(data) == WindowsNative.Identity(attribute), "DOC-CONFLICT");
                RequireStillLocal(data, "data", parent.Name);
                long length = WindowsNative.Standard(data).EndOfFile, changed = WindowsNative.ChangeTime(data);
                hooks?.Visit(StoreStage.Reading);
                bytes = ReadAll(data, cancellation);                                                // O5
                RequireStillLocal(attribute, "attribute-after", parent.Name); RequireStillLocal(data, "data-after", parent.Name); // O6
                Require(WindowsNative.Standard(data).EndOfFile == length && WindowsNative.ChangeTime(data) == changed, "DOC-CONFLICT");
                parent.Verify(); Require(SameEntryNoData(parent, parent.Name, attribute), "DOC-CONFLICT");
            }
            // After D exists every failure is a conflict, never DOC-CLOUD-ONLY and never a partial document (Ruling 261, I-11, I-14).
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { throw new ContractError("DOC-CONFLICT"); }
            count = bytes.Length;
            return new ReadResult(bytes, Identity.Sha256(bytes), origin);
        }
        catch (OperationCanceledException) { code = "DOC-CANCELLED"; throw new ContractError(code); }
        catch (ContractError error) { code = error.Code; throw; }
        catch (Exception error) when (IsStoreFailure(error)) { code = CodeOf(error); throw new ContractError(code); }
        finally { emit("store.read", code, timer.Elapsed.TotalMilliseconds, count, count, traceId, null, null); }
    }

    // winnt.h FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS 0x400000, RECALL_ON_OPEN 0x40000, OFFLINE 0x1000 (values on WindowsNative): Ruling 260's three bits.
    private const uint CloudOnlyBits = WindowsNative.AttributeRecallOnDataAccess | WindowsNative.AttributeRecallOnOpen | WindowsNative.AttributeOffline;

    /// <summary>The attributes and reparse tag of a handle, or what the test seam says they are at <paramref name="site"/> (design 2.6).</summary>
    private (uint Attributes, uint Tag) Observe(SafeFileHandle handle, string site, string name)
    {
        var real = WindowsNative.AttributeTag(handle);
        (uint Attributes, uint Tag) seen = (real.Attributes, real.ReparseTag);
        return hooks?.AttributeOverride is { } replace ? replace(site, name, seen) : seen;
    }

    /// <summary>O2: Local, or OneDriveReadOnly when the folder is under a OneDrive root and O1 applied the exception for it; anything else is refused.</summary>
    private static DocumentOrigin OriginOf(string path, Parent parent) => CloudFolder.ClassifyForOpen(Path.GetDirectoryName(path)!) switch
    {
        OpenLocation.Local => DocumentOrigin.Local,
        OpenLocation.OneDrive when parent.ExceptionApplied => DocumentOrigin.OneDriveReadOnly,
        _ => throw new ContractError("DOC-UNSUPPORTED-PERSISTENCE")
    };

    /// <summary>
    /// O3: the leaf on Open. Origin Local keeps today's rule (no reparse point at all). Origin OneDriveReadOnly is Ruling 260's predicate: not a
    /// directory, one link, not delete-pending, none of the three recall bits (DOC-CLOUD-ONLY, before any data handle), and either no reparse
    /// point or a cloud-family tag; any other tag is DOC-UNSUPPORTED-PERSISTENCE.
    /// </summary>
    private void AdmitLeaf(SafeFileHandle attribute, string name, DocumentOrigin origin)
    {
        var (attributes, tag) = Observe(attribute, "attribute", name);
        var standard = WindowsNative.Standard(attribute);
        Require((attributes & WindowsNative.AttributeDirectory) == 0 && standard.Directory == 0, "DOC-UNSUPPORTED-PERSISTENCE");
        Require(standard.DeletePending == 0, "DOC-CONFLICT");
        Require(standard.NumberOfLinks == 1, "DOC-UNSUPPORTED-PERSISTENCE");
        if (origin == DocumentOrigin.Local) { Require((attributes & WindowsNative.AttributeReparsePoint) == 0, "DOC-UNSUPPORTED-PERSISTENCE"); return; }
        Require((attributes & CloudOnlyBits) == 0, "DOC-CLOUD-ONLY");
        Require((attributes & WindowsNative.AttributeReparsePoint) == 0 || CloudFolder.IsCloudFamily(tag), "DOC-UNSUPPORTED-PERSISTENCE");
    }

    /// <summary>O4 and O6: the handle still shows a fully local file. A recall or offline bit seen once D exists is a conflict (S4, Ruling 261).</summary>
    private void RequireStillLocal(SafeFileHandle handle, string site, string name) =>
        Require((Observe(handle, site, name).Attributes & CloudOnlyBits) == 0, "DOC-CONFLICT");

    /// <summary>The name still denotes the held file, asked through an attribute-only open (no data access, no recall).</summary>
    private static bool SameEntryNoData(Parent parent, string name, SafeFileHandle held)
    {
        try { using var current = WindowsNative.OpenAttributes(parent.Handle, name, ShareAll); return WindowsNative.Identity(current) == WindowsNative.Identity(held); }
        catch (WindowsNative.NativeFailure failure) when (failure.Win32Error is 2 or 3) { return false; }
    }

    /// <summary>
    /// Clear unfinished save (design windows-store-recovery 2.2 to 2.4). Removes only provenance-matched leftovers (the claim and the temps
    /// this store created), only when no live writer holds any of them (an exclusive open for delete, share read), all or nothing, the
    /// claim last. Returns Cleared only when no entry answers the claim name afterwards.
    /// </summary>
    internal ClearResult Clear(string path, CancellationToken cancellation, string traceId)
    {
        // Placeholders are exposed for this whole synchronous call (thread-scoped; no await in this class).
        using var exposure = WindowsNative.ExposePlaceholders();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        int candidates = 0, removed = 0;
        var held = new List<(string Name, SafeFileHandle Handle)>();
        Parent? parent = null; ClearResult result;
        try
        {
            ProjectStore.Supported(); cancellation.ThrowIfCancellationRequested();
            parent = Parent.Open(path, ParentMode.Save, hooks); RequireLocalFolder(path);       // C0
            var first = Candidates(parent); candidates = first.Count;                             // C1, check 1
            ClearResult? refused = null;
            foreach (string name in first)                                                        // C2
            {
                try { held.Add((name, WindowsNative.OpenExclusiveForDelete(parent.Handle, name))); }
                catch (WindowsNative.NativeFailure failure) when (failure.Win32Error is 2 or 3) { } // gone since C1: the writer finished
                catch (WindowsNative.NativeFailure failure) when (failure.Win32Error == 32) { refused = new(ClearOutcome.InUse, "DOC-CONFLICT", 0); break; }
                // D3: any other open error (access denied, delete pending, ...) is a failure with its code, never "in use", never skipped.
                catch (Exception failure) when (IsStoreFailure(failure)) { refused = new(ClearOutcome.Failed, CodeOf(failure), 0); break; }
            }
            if (refused is null)
            {
                hooks?.Visit(StoreStage.ClearHeld);
                foreach (var (name, handle) in held)                                              // C3, provenance P1 to P5
                    if (!ProvenanceMatches(handle, UnfinishedSave.IsClaim(name))) { refused = new(ClearOutcome.Failed, "DOC-CONFLICT", 0); break; }
            }
            if (refused is null)                                                                  // C4, check 2
            {
                parent.Verify();
                foreach (var (name, handle) in held)
                    if (!SameEntry(parent, name, handle)) { refused = new(ClearOutcome.Failed, "DOC-CONFLICT", 0); break; }
                if (refused is null)
                {
                    var heldNames = held.Select(entry => entry.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var second = Candidates(parent);
                    if (second.Any(name => !heldNames.Contains(name))) refused = new(ClearOutcome.InUse, "DOC-CONFLICT", 0); // a new entry: a writer started
                    else if (heldNames.Any(name => !second.Contains(name, StringComparer.OrdinalIgnoreCase))) refused = new(ClearOutcome.Failed, "DOC-CONFLICT", 0);
                }
            }
            if (refused is null)
            {
                try
                {
                    foreach (var entry in held.Where(entry => !UnfinishedSave.IsClaim(entry.Name))) { DeleteHeld(parent, entry); removed++; } // C5, temps
                    hooks?.Visit(StoreStage.ClearTempsDisposed);
                    foreach (var entry in held.Where(entry => UnfinishedSave.IsClaim(entry.Name))) { DeleteHeld(parent, entry); removed++; }   // C6, the claim last
                }
                catch (Exception failure) when (failure is ContractError or WindowsNative.NativeFailure || IsStoreFailure(failure)) { refused = new(ClearOutcome.Failed, "DOC-IO", removed); }
            }
            // C7: Cleared only when no entry still answers the claim name (case-insensitively); also when C1 found nothing.
            refused ??= WindowsNative.EntryExists(parent.Handle, ProjectStore.ClaimName())
                ? new(ClearOutcome.Failed, "DOC-CONFLICT", removed) : new ClearResult(ClearOutcome.Cleared, "OK", removed);
            result = refused;
        }
        catch (OperationCanceledException) { result = new(ClearOutcome.Failed, "DOC-CANCELLED", removed); }
        catch (ContractError error) { result = new(ClearOutcome.Failed, error.Code, removed); }
        catch (Exception error) when (IsStoreFailure(error)) { result = new(ClearOutcome.Failed, CodeOf(error), removed); }
        finally
        {
            foreach (var (_, handle) in held) handle.Dispose();
            parent?.Dispose();
        }
        emit("store.clear", result.Outcome == ClearOutcome.Cleared ? "OK" : result.Code, timer.Elapsed.TotalMilliseconds, candidates, removed, traceId, null, null);
        return result;
    }

    /// <summary>The candidate names of the retained folder, listed through a listing handle on it.</summary>
    private static List<string> Candidates(Parent parent)
    {
        using var listing = parent.OpenListing();
        return UnfinishedSave.Candidates(WindowsNative.ListNames(listing));
    }

    /// <summary>P1 to P4 (design 2.2) on the handle Clear holds: a regular, single-link, not-pending-delete file with the store's private descriptor; a claim is empty.</summary>
    private static bool ProvenanceMatches(SafeFileHandle handle, bool claim)
    {
        if ((WindowsNative.AttributeTag(handle).Attributes & (WindowsNative.AttributeDirectory | WindowsNative.AttributeReparsePoint)) != 0) return false;
        var standard = WindowsNative.Standard(handle);
        if (standard.Directory != 0 || standard.DeletePending != 0 || standard.NumberOfLinks != 1) return false;
        if (!Owned.HasPrivateDescriptor(handle)) return false;
        return claim ? standard.EndOfFile == 0 : standard.EndOfFile <= NativeProject.MaxBytes;
    }

    /// <summary>POSIX delete disposition on the held handle, close it, and confirm the name is gone.</summary>
    private static void DeleteHeld(Parent parent, (string Name, SafeFileHandle Handle) entry)
    {
        WindowsNative.DisposeEntry(entry.Handle);
        entry.Handle.Dispose();
        Require(!WindowsNative.EntryExists(parent.Handle, entry.Name), "DOC-IO");
    }

    internal SaveResult Save(string path, SaveRequest request, CancellationToken cancellation, string traceId)
    {
        // Placeholders are exposed for this whole synchronous call (thread-scoped; no await in this class).
        using var exposure = WindowsNative.ExposePlaceholders();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        bool published = false;
        byte[] image = request.Image; string hash = Identity.Sha256(image);
        Parent? parent = null; Owned? claim = null, temp = null; SafeFileHandle? target = null;
        string code = "OK"; bool durable = false;
        try
        {
            ProjectStore.Supported(); cancellation.ThrowIfCancellationRequested(); parent = Parent.Open(path, ParentMode.Save, hooks);
            RequireLocalFolder(path);
            hooks?.Visit(StoreStage.ParentOpened);
            _ = Exists(parent, parent.Name);
            if (!request.CreateOnly)
            {
                // Ruling 253: refuse, before the claim and with a definite code, an EFS-encrypted target (a plaintext replacement would
                // downgrade it) and a read-only one (the rename would fail into DOC-SAVE-UNCERTAIN). Any other DACL is admitted.
                using (var probe = OpenRegular(parent, parent.Name))
                    Require((WindowsNative.AttributeTag(probe).Attributes & (WindowsNative.AttributeEncrypted | WindowsNative.AttributeReadOnly)) == 0,
                        "DOC-UNSUPPORTED-PERSISTENCE");
                claim = Owned.Create(parent, ProjectStore.ClaimName()); hooks?.Visit(StoreStage.ClaimCreated);
                target = OpenRegular(parent, parent.Name);
                byte[] disk = ReadAll(target, cancellation);
                Require(Identity.Sha256(disk) == request.ExpectedDiskSha256, "DOC-CONFLICT");
                // Design section 5 would refuse an overwrite of a file with an imported (non-private) DACL. That rule is not
                // applied: a file written by another tool is a routine overwrite target, and the replacement is always the
                // private temp. Decision request raised to the leader (W2 brief, seam); the proof records the deviation.
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
        // Placeholders are exposed for this whole synchronous call (thread-scoped; no await in this class).
        using var exposure = WindowsNative.ExposePlaceholders();
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
            parent = Parent.Open(Path.Combine(dir, fileName), ParentMode.Save, hooks); RequireLocalFolder(Path.Combine(dir, fileName));
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

    /// <summary>The folder is not OneDrive-rooted and carries no cloud attribute or tag on it or an ancestor; a check that cannot run refuses.</summary>
    private static void RequireLocalFolder(string path) =>
        Require(CloudFolder.Classify(Path.GetDirectoryName(path)!) == FolderLocation.Local, "DOC-UNSUPPORTED-PERSISTENCE");

    private static void Require(bool condition, string code) { if (!condition) throw new ContractError(code); }

    private static bool IsStoreFailure(Exception error) =>
        error is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    /// <summary>Rulings 145 and 152 (4): a Win32 error with a qualified product code keeps it (32 is DOC-CONFLICT); every other
    /// error is mapped here by the listed winerror.h values, and anything unlisted is DOC-IO, never a guessed code.</summary>
    private static string CodeOf(Exception error) => error switch
    {
        ContractError contract => contract.Code,
        // ntstatus.h STATUS_FILE_IS_A_DIRECTORY (0xC00000BA) reaches Win32 as ERROR_ACCESS_DENIED, so it is read from the NTSTATUS.
        WindowsNative.NativeFailure { NtStatus: unchecked((int)0xC00000BA) } => "DOC-UNSUPPORTED-PERSISTENCE",
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

    /// <summary>Which walk this is. Save (Save, PublishUnderClaim, Clear) is today's, byte for byte; OpenRead (Read only) may apply the placeholder exception (I-12).</summary>
    private enum ParentMode { Save, OpenRead }

    /// <summary>The retained chain of directory handles from the drive root to the leaf's parent.</summary>
    private sealed class Parent : IDisposable
    {
        private readonly List<(SafeFileHandle Handle, string Name)> chain = [];
        internal SafeFileHandle Handle => chain[^1].Handle;
        internal string Name { get; private set; } = "";
        private string rootPath = "";
        /// <summary>A second handle on the same folder with the right to list it (the walk's handles have traverse only); its identity must equal the retained one.</summary>
        internal SafeFileHandle OpenListing()
        {
            uint access = WindowsNative.DirectoryAccess | WindowsNative.ListDirectoryAccess;
            var listing = chain.Count == 1 ? WindowsNative.OpenRoot(rootPath, access) : WindowsNative.OpenDirectory(chain[^2].Handle, chain[^1].Name, access);
            if (WindowsNative.Identity(listing) != WindowsNative.Identity(Handle)) { listing.Dispose(); throw new ContractError("DOC-CONFLICT"); }
            return listing;
        }
        /// <summary>True when the walk applied the placeholder exception (design 2.6 O1): only for <see cref="ParentMode.OpenRead"/>, S2 met.</summary>
        internal bool ExceptionApplied { get; private set; }

        internal static Parent Open(string path, ParentMode mode = ParentMode.Save, StoreHooks? hooks = null)
        {
            var (root, parts) = ParsePath(path);
            var result = new Parent { Name = parts[^1], rootPath = root };
            // O1 (S2): the exception can only apply when the environment-named OneDrive root is at least one folder below the drive root.
            // Its directory depth in this path is the index (plus one) of the root directory; zero means no exception can apply.
            string[] ancestors = parts.SkipLast(1).ToArray();
            string? oneDrive = mode == ParentMode.OpenRead ? CloudFolder.OneDriveRootOf(Path.GetDirectoryName(path)!, Environment.GetEnvironmentVariable) : null;
            string[] rootParts = oneDrive is not null && CloudFolder.RootQualifies(oneDrive) ? oneDrive[3..].Split('\\') : [];
            int rootDepth = rootParts.Length > 0 && rootParts.Length <= ancestors.Length &&
                ancestors.Take(rootParts.Length).SequenceEqual(rootParts, StringComparer.OrdinalIgnoreCase) ? rootParts.Length : 0;
            try
            {
                var rootHandle = WindowsNative.OpenRoot(root); result.chain.Add((rootHandle, ""));
                // The qualified candidate is a local NTFS volume (design section 4); ReFS, SMB and removable media stay closed.
                Require(WindowsNative.VolumeFileSystem(rootHandle) == "NTFS", "DOC-UNSUPPORTED-PERSISTENCE");
                var driveRoot = WindowsNative.AttributeTag(rootHandle);
                RequireDirectory(rootHandle, (driveRoot.Attributes, driveRoot.ReparseTag), false);
                // A mapped network drive reports the server's file system name, so the device itself is asked (design section 4).
                var (deviceType, characteristics) = WindowsNative.VolumeDevice(rootHandle);
                Require(WindowsNative.IsFixedLocalVolume(deviceType, characteristics), "DOC-UNSUPPORTED-PERSISTENCE");
                for (int i = 0; i < ancestors.Length; i++)
                {
                    var handle = WindowsNative.OpenDirectory(result.Handle, ancestors[i]); result.chain.Add((handle, ancestors[i]));
                    var real = WindowsNative.AttributeTag(handle); (uint Attributes, uint Tag) seen = (real.Attributes, real.ReparseTag);
                    (uint Attributes, uint Tag) tag = hooks?.AttributeOverride is { } replace ? replace("ancestor", ancestors[i], seen) : seen;
                    // The exception: the root directory itself carries a cloud-family tag (S2), and then it and every ancestor under it may.
                    if (rootDepth > 0 && i == rootDepth - 1 && (tag.Attributes & WindowsNative.AttributeReparsePoint) != 0 && CloudFolder.IsCloudFamily(tag.Tag))
                    { result.ExceptionApplied = true; hooks?.Visit(StoreStage.PlaceholderExceptionApplied); }
                    RequireDirectory(handle, tag, result.ExceptionApplied && i >= rootDepth - 1);
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        // Every ancestor is a real directory (no symlink, junction, mount point or cloud tag) in a case-insensitive directory.
        // Under the placeholder exception a directory with a cloud-family tag (and no other reparse point) is admitted too.
        private static void RequireDirectory(SafeFileHandle directory, (uint Attributes, uint Tag) tag, bool allowCloud)
        {
            Require((tag.Attributes & WindowsNative.AttributeDirectory) != 0, "DOC-UNSUPPORTED-PERSISTENCE");
            Require((tag.Attributes & WindowsNative.AttributeReparsePoint) == 0 || allowCloud && CloudFolder.IsCloudFamily(tag.Tag), "DOC-UNSUPPORTED-PERSISTENCE");
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
