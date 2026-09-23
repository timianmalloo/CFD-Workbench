using CfdWorkbench.Core;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("CfdWorkbench.Core.Tests")]
namespace CfdWorkbench.Persistence;

public sealed class SaveRequest
{
    private readonly byte[] image;
    public SaveRequest(byte[] image, string? expectedDiskSha256, string operationId)
    {
        if (image.Length > NativeProject.MaxBytes) throw new ContractError("DOC-SIZE");
        if (expectedDiskSha256 is not null && !Regex.IsMatch(expectedDiskSha256, @"\A[0-9a-f]{64}\z")) throw new ContractError("DOC-HASH");
        if (!Guid.TryParseExact(operationId, "D", out _)) throw new ContractError("DOC-OPERATION");
        this.image = image.ToArray(); ExpectedDiskSha256 = expectedDiskSha256; OperationId = operationId;
    }
    public byte[] Image => image.ToArray();
    public string? ExpectedDiskSha256 { get; }
    public string OperationId { get; }
    public bool CreateOnly => ExpectedDiskSha256 is null;
}

/// <summary>DurabilityConfirmed means file fsync and final-directory fsync succeeded after owned cleanup;
/// it is not a hardware power-loss guarantee. An uncertain result requires reopen/compare before retry.</summary>
public sealed record SaveResult(string Code, string? PublishedSha256, bool PublicationKnown, bool DurabilityConfirmed);
public sealed class ReadResult(byte[] image, string diskSha256)
{
    private readonly byte[] image = image.ToArray();
    public byte[] Image => image.ToArray();
    public string DiskSha256 { get; } = diskSha256;
}
public interface IProjectStore : IDisposable
{
    Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default);
    Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default);
}

public sealed class ProjectStore : IProjectStore, IDisposable
{
    private readonly StoreHooks? hooks;
    private readonly AuthoringSession telemetry;
    private readonly bool ownsTelemetry;
    private readonly object lifecycle = new();
    private bool disposed;
    public ProjectStore() { telemetry = new(); ownsTelemetry = true; }
    public ProjectStore(AuthoringSession session) { telemetry = session; }
    internal ProjectStore(StoreHooks hooks) : this() { this.hooks = hooks; }
    public IReadOnlyList<SessionEvent> ReadLocalEvents() => telemetry.ReadLocalEvents();
    public void Dispose() { lock (lifecycle) { disposed = true; if (ownsTelemetry) telemetry.Dispose(); } }
    public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
    {
        lock (lifecycle)
        {
            if (disposed) return Task.FromResult(new SaveResult("DOC-CLOSED", null, false, false));
            string traceId = Guid.NewGuid().ToString("N");
            return Task.Run(() => Save(path, request, cancellation, traceId));
        }
    }
    public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default)
    {
        lock (lifecycle)
        {
            if (disposed) return Task.FromException<ReadResult>(new ContractError("DOC-CLOSED"));
            string traceId = Guid.NewGuid().ToString("N");
            return Task.Run(() => Read(path, cancellation, traceId));
        }
    }
    private ReadResult Read(string path, CancellationToken cancellation, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew(); string code = "OK"; int? count = null;
        try
        {
            Supported(); cancellation.ThrowIfCancellationRequested();
            using var parent = ParentPath.Open(path); using var target = OpenRegular(parent.Fd, parent.Name);
            byte[] bytes = ReadAll(target.Fd, cancellation);
            parent.Verify(); Require(SameEntry(parent.Fd, parent.Name, target.Fd), "DOC-CONFLICT"); count = bytes.Length;
            return new ReadResult(bytes, Identity.Sha256(bytes));
        }
        catch (OperationCanceledException) { code = "DOC-CANCELLED"; throw new ContractError(code); }
        catch (ContractError error) { code = error.Code; throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
        { code = error is DllNotFoundException or EntryPointNotFoundException ? "DOC-UNSUPPORTED-PERSISTENCE" : "DOC-IO"; throw new ContractError(code); }
        finally { Emit("store.read", code, timer.Elapsed.TotalMilliseconds, count, count, traceId, null, null); }
    }
    private void Emit(string action, string code, double elapsed, int? input, int? output, string traceId, bool? published, bool? durable)
    {
        try { telemetry.RecordPersistence(action, code, elapsed, input, output, traceId, published, durable); }
        catch (Exception) { /* Instrumentation failure cannot change a known I/O outcome; measurement is absent. */ }
    }

    private SaveResult Save(string path, SaveRequest request, CancellationToken cancellation, string traceId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        bool published = false;
        byte[] image = request.Image; string hash = Identity.Sha256(image);
        ParentPath? parent = null; OwnedEntry? claim = null, temp = null; NativeFd? target = null;
        string code = "OK"; bool durable = false;
        try
        {
            Supported(); cancellation.ThrowIfCancellationRequested(); parent = ParentPath.Open(path);
            hooks?.Visit(StoreStage.ParentOpened);
            if (Exists(parent.Fd, parent.Name, out var prior)) Require(IsRegular(prior), "DOC-UNSUPPORTED-PERSISTENCE");
            if (!request.CreateOnly)
            {
                claim = OwnedEntry.Create(parent.Fd, ClaimName()); hooks?.Visit(StoreStage.ClaimCreated);
                target = OpenRegular(parent.Fd, parent.Name);
                Require(Identity.Sha256(ReadAll(target.Fd, cancellation)) == request.ExpectedDiskSha256, "DOC-CONFLICT");
            }
            temp = OwnedEntry.Create(parent.Fd, TempName(request.OperationId)); hooks?.Visit(StoreStage.TempCreated);
            WriteAll(temp.Fd, image, cancellation);
            var fileFlushTimer = System.Diagnostics.Stopwatch.StartNew(); string fileFlushCode = "OK";
            try { Check(Native.Fsync(temp.Fd)); }
            catch (ContractError error) { fileFlushCode = error.Code; throw; }
            finally { Emit("store.file-flush", fileFlushCode, fileFlushTimer.Elapsed.TotalMilliseconds, image.Length, null, traceId, false, false); }
            hooks?.Visit(StoreStage.FileFlushed);
            cancellation.ThrowIfCancellationRequested(); hooks?.Visit(StoreStage.BeforePublish);
            cancellation.ThrowIfCancellationRequested(); parent.Verify();
            Require(temp.IsOwned && (claim is null || claim.IsOwned), "DOC-CONFLICT");
            if (target is not null)
            {
                Require(SameEntry(parent.Fd, parent.Name, target.Fd), "DOC-CONFLICT");
                // Re-read through a freshly held handle immediately before cooperative publication.
                using var fresh = OpenRegular(parent.Fd, parent.Name);
                Require(Same(target.Fd, fresh.Fd) && Identity.Sha256(ReadAll(fresh.Fd, cancellation)) == request.ExpectedDiskSha256, "DOC-CONFLICT");
            }
            var publicationTimer = System.Diagnostics.Stopwatch.StartNew(); string publicationCode = "OK";
            try
            {
                // linkat is atomic no-replace: there is deliberately no absent-check/rename fallback.
                if (hooks?.PublicationError is int error) throw Failure(error);
                Check(target is null ? Native.LinkAt(parent.Fd, temp.Name, parent.Fd, parent.Name, 0) : Native.RenameAt(parent.Fd, temp.Name, parent.Fd, parent.Name));
                published = true;
            }
            catch (ContractError error)
            {
                publicationCode = error.Code is "DOC-CONFLICT" or "DOC-UNSUPPORTED-PERSISTENCE" ? error.Code : "DOC-SAVE-UNCERTAIN";
                throw new ContractError(publicationCode);
            }
            finally { Emit("store.publish", publicationCode, publicationTimer.Elapsed.TotalMilliseconds, image.Length, published ? image.Length : 0, traceId, published, false); }
            hooks?.Visit(StoreStage.Published);
            Require(SameEntry(parent.Fd, parent.Name, temp.Fd), "DOC-CONFLICT"); parent.Verify();
            // Cancellation after publication does not manufacture a not-saved result.
            Require(temp.Cleanup() & (claim?.Cleanup() ?? true), "DOC-IO");
            hooks?.Visit(StoreStage.BeforeDirectoryFlush);
            var flushTimer = System.Diagnostics.Stopwatch.StartNew(); string flushCode = "OK";
            try { Check(Native.Fsync(parent.Fd)); durable = true; }
            catch (ContractError error) { flushCode = error.Code; throw; }
            finally { Emit("store.flush", flushCode, flushTimer.Elapsed.TotalMilliseconds, null, null, traceId, published, durable); }
        }
        catch (OperationCanceledException) { code = published ? "DOC-SAVE-UNCERTAIN" : "DOC-CANCELLED"; }
        catch (ContractError error) { code = published ? "DOC-SAVE-UNCERTAIN" : error.Code; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
        { code = published ? "DOC-SAVE-UNCERTAIN" : error is DllNotFoundException or EntryPointNotFoundException ? "DOC-UNSUPPORTED-PERSISTENCE" : "DOC-IO"; }
        finally
        {
            // Cleanup is handle-relative and removes only an entry still matching its retained owned handle.
            // A collided/replaced name is preserved. A cleanup failure is never reported as a clean success.
            bool cleaned = (temp?.Cleanup() ?? true) & (claim?.Cleanup() ?? true);
            if (!cleaned && code == "OK") { code = published ? "DOC-SAVE-UNCERTAIN" : "DOC-IO"; durable = false; }
            target?.Dispose(); temp?.Dispose(); claim?.Dispose(); parent?.Dispose();
        }
        Emit("store.save", code, timer.Elapsed.TotalMilliseconds, image.Length, published ? image.Length : 0, traceId, published, durable && code == "OK");
        return new(code, published ? hash : null, published, durable && code == "OK");
    }

    internal static string TempName(string operation) => ".cfd-" + Guid.Parse(operation).ToString("D") + ".tmp";
    // simplify: serialize cooperative overwrites in the selected directory. This prevents case/Unicode
    // aliases bypassing a raw-name lock. Upgrade only with measured per-file identity-lock lifetimes.
    internal static string ClaimName() => ".cfd-writer.claim";
    private static void Supported()
    {
        // Windows handle/reparse/sharing guarantees have not been measured. No path-only fallback.
        Require(OperatingSystem.IsMacOS() && Marshal.SizeOf<Native.Stat>() == 144, "DOC-UNSUPPORTED-PERSISTENCE");
    }
    private static void Require(bool condition, string code) { if (!condition) throw new ContractError(code); }
    private static void Check(int result) { if (result < 0) throw Failure(Marshal.GetLastPInvokeError()); }
    private static ContractError Failure(int error) => new(error switch
    {
        17 => "DOC-CONFLICT", 2 => "DOC-CONFLICT", 28 => "DOC-DISK-FULL",
        // Darwin errno.h: ENOTDIR, ENOTSUP, ELOOP, ENAMETOOLONG, ENOSYS.
        20 or 45 or 62 or 63 or 78 => "DOC-UNSUPPORTED-PERSISTENCE", _ => "DOC-IO"
    });
    private static bool IsRegular(Native.Stat stat) => (stat.Mode & 0xf000) == 0x8000;
    private static bool Exists(int parent, string name, out Native.Stat stat)
    {
        if (Native.FstatAt(parent, name, out stat, 0x20) == 0) return true;
        int error = Marshal.GetLastPInvokeError(); if (error == 2) return false; throw Failure(error);
    }
    private static bool SameStat(Native.Stat a, Native.Stat b) => a.Device == b.Device && a.Inode == b.Inode && (a.Mode & 0xf000) == (b.Mode & 0xf000);
    private static bool Same(int a, int b) { Check(Native.Fstat(a, out var x)); Check(Native.Fstat(b, out var y)); return SameStat(x, y); }
    private static bool SameEntry(int parent, string name, int fd)
    { Check(Native.Fstat(fd, out var held)); return Exists(parent, name, out var entry) && SameStat(held, entry); }
    private static NativeFd OpenRegular(int parent, string name)
    {
        var handle = new NativeFd(Native.OpenAt(parent, name, Native.NoFollow | Native.NonBlock, 0));
        try { Check(Native.Fstat(handle.Fd, out var stat)); Require(IsRegular(stat), "DOC-UNSUPPORTED-PERSISTENCE"); return handle; }
        catch { handle.Dispose(); throw; }
    }
    private static byte[] ReadAll(int fd, CancellationToken cancellation)
    {
        Check(Native.Fstat(fd, out var before)); Require(before.Size >= 0 && before.Size <= NativeProject.MaxBytes, "DOC-SIZE");
        using var output = new MemoryStream(); byte[] buffer = new byte[65536];
        while (true)
        {
            cancellation.ThrowIfCancellationRequested(); nint count = Native.Read(fd, buffer, (nuint)buffer.Length);
            if (count < 0) { int error = Marshal.GetLastPInvokeError(); if (error == 4) continue; throw Failure(error); }
            if (count == 0) break;
            Require(output.Length + count <= NativeProject.MaxBytes, "DOC-SIZE"); output.Write(buffer, 0, checked((int)count));
        }
        Check(Native.Fstat(fd, out var after));
        Require(before.Size == after.Size && before.ModifySeconds == after.ModifySeconds && before.ModifyNanoseconds == after.ModifyNanoseconds &&
            before.ChangeSeconds == after.ChangeSeconds && before.ChangeNanoseconds == after.ChangeNanoseconds, "DOC-CONFLICT");
        return output.ToArray();
    }
    private void WriteAll(int fd, byte[] image, CancellationToken cancellation)
    {
        int offset = 0;
        while (offset < image.Length)
        {
            cancellation.ThrowIfCancellationRequested(); hooks?.Visit(StoreStage.Writing);
            if (hooks?.FailWriteAfter is int failure && offset >= failure) throw new ContractError("DOC-DISK-FULL");
            int length = Math.Min(image.Length - offset, Math.Min(65536, hooks?.WriteFragment ?? 65536));
            Require(length > 0, "DOC-IO"); byte[] chunk = image.AsSpan(offset, length).ToArray();
            nint count = Native.Write(fd, chunk, (nuint)chunk.Length);
            if (count < 0) { int error = Marshal.GetLastPInvokeError(); if (error == 4) continue; throw Failure(error); }
            Require(count > 0 && count <= length, "DOC-IO"); offset += checked((int)count);
        }
    }
    private sealed class NativeFd : SafeHandle
    {
        internal NativeFd(int fd) : base(new IntPtr(-1), true) { if (fd < 0) throw Failure(Marshal.GetLastPInvokeError()); SetHandle(new IntPtr(fd)); }
        internal int Fd => checked((int)handle);
        public override bool IsInvalid => handle == new IntPtr(-1);
        protected override bool ReleaseHandle() => Native.Close(checked((int)handle)) == 0;
    }
    private sealed class ParentPath : IDisposable
    {
        private readonly List<(NativeFd Handle, string Name)> chain = [];
        internal int Fd => chain[^1].Handle.Fd;
        internal string Name { get; private set; } = "";
        internal static ParentPath Open(string path)
        {
            Require(path.Length <= 32768 && path.StartsWith('/') && !path.Contains('\0'), "DOC-UNSUPPORTED-PERSISTENCE");
            try { _ = new UTF8Encoding(false, true).GetBytes(path); } catch (EncoderFallbackException) { throw new ContractError("DOC-UNSUPPORTED-PERSISTENCE"); }
            string[] parts = path.Split('/'); Require(parts.Length is >= 2 and <= 258 && parts.Skip(1).All(p => p.Length != 0 && p is not "." and not ".."), "DOC-UNSUPPORTED-PERSISTENCE");
            Require(!parts[^1].StartsWith(".cfd-", StringComparison.OrdinalIgnoreCase), "DOC-UNSUPPORTED-PERSISTENCE");
            var result = new ParentPath { Name = parts[^1] };
            try
            {
                result.chain.Add((new NativeFd(Native.Open("/", Native.DirectoryFlags, 0)), ""));
                foreach (string part in parts.Skip(1).SkipLast(1))
                {
                    var handle = new NativeFd(Native.OpenAt(result.Fd, part, Native.DirectoryFlags, 0));
                    result.chain.Add((handle, part));
                    Check(Native.Fstat(handle.Fd, out var stat)); Require((stat.Mode & 0xf000) == 0x4000, "DOC-UNSUPPORTED-PERSISTENCE");
                }
                result.Verify(); return result;
            }
            catch { result.Dispose(); throw; }
        }
        internal void Verify()
        {
            for (int i = 1; i < chain.Count; i++) Require(SameEntry(chain[i - 1].Handle.Fd, chain[i].Name, chain[i].Handle.Fd), "DOC-CONFLICT");
        }
        public void Dispose() { for (int i = chain.Count - 1; i >= 0; i--) chain[i].Handle.Dispose(); }
    }
    private sealed class OwnedEntry(int parent, string name, NativeFd handle) : IDisposable
    {
        private bool released;
        internal int Fd => handle.Fd;
        internal string Name => name;
        internal bool IsOwned => SameEntry(parent, name, Fd);
        internal static OwnedEntry Create(int parent, string name) => new(parent, name,
            new NativeFd(Native.OpenAt(parent, name, Native.NoFollow | 0x200 | 0x800 | 2, 0x180)));
        internal bool Cleanup()
        {
            if (released) return true;
            try
            {
                if (!Exists(parent, name, out _)) { released = true; return true; }
                if (!IsOwned) return false;
                released = Native.UnlinkAt(parent, name, 0) == 0; return released;
            }
            catch (ContractError) { return false; }
        }
        public void Dispose() => handle.Dispose();
    }

    private static class Native
    {
        internal const int NoFollow = 0x01000100, NonBlock = 4, DirectoryFlags = NoFollow | 0x00100000;
        [StructLayout(LayoutKind.Sequential)]
        internal struct Stat
        {
            internal int Device; internal ushort Mode, Links; internal ulong Inode;
            internal uint User, Group; internal int RDevice;
            internal long AccessSeconds, AccessNanoseconds, ModifySeconds, ModifyNanoseconds;
            internal long ChangeSeconds, ChangeNanoseconds, BirthSeconds, BirthNanoseconds;
            internal long Size, Blocks; internal int BlockSize; internal uint Flags, Generation;
            internal int Spare; internal long Spare0, Spare1;
        }
        [DllImport("libSystem.B.dylib", EntryPoint = "open", SetLastError = true)] internal static extern int Open(string path, int flags, uint mode);
        [DllImport("libSystem.B.dylib", EntryPoint = "openat", SetLastError = true)] internal static extern int OpenAt(int parent, string name, int flags, uint mode);
        [DllImport("libSystem.B.dylib", EntryPoint = "fstat64", SetLastError = true)] internal static extern int Fstat(int fd, out Stat stat);
        [DllImport("libSystem.B.dylib", EntryPoint = "fstatat64", SetLastError = true)] internal static extern int FstatAt(int parent, string name, out Stat stat, int flags);
        [DllImport("libSystem.B.dylib", EntryPoint = "linkat", SetLastError = true)] internal static extern int LinkAt(int a, string x, int b, string y, int flags);
        [DllImport("libSystem.B.dylib", EntryPoint = "renameat", SetLastError = true)] internal static extern int RenameAt(int a, string x, int b, string y);
        [DllImport("libSystem.B.dylib", EntryPoint = "unlinkat", SetLastError = true)] internal static extern int UnlinkAt(int parent, string name, int flags);
        [DllImport("libSystem.B.dylib", EntryPoint = "fsync", SetLastError = true)] internal static extern int Fsync(int fd);
        [DllImport("libSystem.B.dylib", EntryPoint = "close", SetLastError = true)] internal static extern int Close(int fd);
        [DllImport("libSystem.B.dylib", EntryPoint = "read", SetLastError = true)] internal static extern nint Read(int fd, byte[] bytes, nuint count);
        [DllImport("libSystem.B.dylib", EntryPoint = "write", SetLastError = true)] internal static extern nint Write(int fd, byte[] bytes, nuint count);
    }
}

internal enum StoreStage { ParentOpened, ClaimCreated, TempCreated, Writing, FileFlushed, BeforePublish, Published, BeforeDirectoryFlush }
internal sealed class StoreHooks
{
    internal Action<StoreStage>? OnStage { get; init; }
    internal int? WriteFragment { get; init; }
    internal int? FailWriteAfter { get; init; }
    internal int? PublicationError { get; init; }
    internal void Visit(StoreStage stage) => OnStage?.Invoke(stage);
}
