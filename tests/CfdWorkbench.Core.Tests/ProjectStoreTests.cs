using System.Runtime.InteropServices;
using CfdWorkbench.Persistence;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class ProjectStoreTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsMacOS()) { Console.WriteLine("NOT ASSESSED native persistence primitives on this platform"); return; }
        Check("NativePrimitive_MacHandleRelativeNoReplaceAndFlush", Probe);
        Check("NativePrimitive_MacReadWriteUnlink", ReadWriteProbe);
        Check("Store_CreateReadAndDefensiveImage", () =>
        {
            string path = Path.Combine(Root(), "project.cfdw"); byte[] image = [1, 2, 3];
            var request = new SaveRequest(image, null, Id()); image[0] = 9; request.Image[0] = 8;
            var store = new ProjectStore(); var saved = Save(store, path, request); Equal("OK", saved.Code);
            Equal(true, saved.PublicationKnown); Equal(true, saved.DurabilityConfirmed);
            var read = store.ReadAsync(path).GetAwaiter().GetResult(); Equal(true, read.Image.AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }));
            read.Image[0] = 7; Equal((byte)1, read.Image[0]); Equal(saved.PublishedSha256, read.DiskSha256);
            Equal(1, Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
        });
        Check("Store_CreateCollision_PreservesCompetingBytes", () =>
        {
            string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7, 8]);
            Equal("DOC-CONFLICT", Save(new(), path, new([1, 2], null, Id())).Code);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 7, 8 }));
        });
        Check("Store_Overwrite_ExactTokenAndHeldReader", () =>
        {
            string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7, 8]); var store = new ProjectStore();
            using var old = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            string hash = Identity.Sha256([7, 8]);
            Equal("DOC-CONFLICT", Save(store, path, new([1, 2], Identity.Sha256([0]), Id())).Code);
            var result = Save(store, path, new([1, 2], hash, Id())); Equal("OK", result.Code);
            Equal(7, old.ReadByte()); Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2 }));
        });
        Check("Store_SymlinkAncestorAndTarget_Refused", () =>
        {
            string root = Root(), real = Path.Combine(root, "real"); Directory.CreateDirectory(real);
            Directory.CreateSymbolicLink(Path.Combine(root, "alias"), real);
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(new(), Path.Combine(root, "alias", "project"), new([1], null, Id())).Code);
            string path = Path.Combine(real, "project"); File.CreateSymbolicLink(path, Path.Combine(real, "missing"));
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(new(), path, new([1], null, Id())).Code);
            Equal(false, File.Exists(Path.Combine(real, "missing")));
        });
        Check("Store_CancelBeforePublication_PreservesOriginal", () =>
        {
            string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7]);
            var result = new ProjectStore().SaveAsync(path, new([1], Identity.Sha256([7]), Id()), new(true)).GetAwaiter().GetResult();
            Equal("DOC-CANCELLED", result.Code); Equal(false, result.PublicationKnown); Equal((byte)7, File.ReadAllBytes(path)[0]);
        });
        Check("Store_FlushOccursAfterOwnedEntryCleanup", () =>
        {
            string root = Root(), path = Path.Combine(root, "project"); File.WriteAllBytes(path, [7]); bool observed = false;
            var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.BeforeDirectoryFlush) { observed = true; Equal(1, Directory.GetFiles(root).Length); } } });
            Equal("OK", Save(store, path, new([1], Identity.Sha256([7]), Id())).Code); Equal(true, observed);
        });
        Check("Store_EquivalentCaseAlias_CannotAcquireSecondWriterClaim", () =>
        {
            string root = Root(), path = Path.Combine(root, "project"), alias = Path.Combine(root, "PROJECT"); File.WriteAllBytes(path, [7]);
            Equal(true, File.Exists(alias));
            using var claimed = new ManualResetEventSlim(); using var release = new ManualResetEventSlim(); bool secondClaim = false;
            var first = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.ClaimCreated) { claimed.Set(); Equal(true, release.Wait(TimeSpan.FromSeconds(5))); } } });
            Task<SaveResult> pending = first.SaveAsync(path, new([1], Identity.Sha256([7]), Id()));
            try
            {
                Equal(true, claimed.Wait(TimeSpan.FromSeconds(5)));
                var second = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.ClaimCreated) secondClaim = true; } });
                var result = Save(second, alias, new([2], Identity.Sha256([7]), Id()));
                Equal(false, secondClaim); Equal("DOC-CONFLICT", result.Code);
            }
            finally { release.Set(); _ = pending.GetAwaiter().GetResult(); }
        });
        Check("Store_RealCompetingCreators_ExactlyOneCompleteImage", () =>
        {
            string path = Path.Combine(Root(), "project"); using var barrier = new Barrier(2);
            var hooks = new StoreHooks { OnStage = stage => { if (stage == StoreStage.BeforePublish) Equal(true, barrier.SignalAndWait(TimeSpan.FromSeconds(5))); } };
            Task<SaveResult> a = new ProjectStore(hooks).SaveAsync(path, new([1, 1], null, Id()));
            Task<SaveResult> b = new ProjectStore(hooks).SaveAsync(path, new([2, 2], null, Id()));
            Task.WaitAll(a, b); Equal(1, new[] { a.Result, b.Result }.Count(x => x.Code == "OK"));
            Equal(1, new[] { a.Result, b.Result }.Count(x => x.Code == "DOC-CONFLICT"));
            byte[] image = File.ReadAllBytes(path); Equal(2, image.Length); Equal(image[0], image[1]);
            Equal(1, Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
        });
        Check("Store_AncestorReplacement_PreservesBothTrees", () =>
        {
            string root = Root(), selected = Path.Combine(root, "selected"), moved = Path.Combine(root, "moved"); Directory.CreateDirectory(selected);
            string path = Path.Combine(selected, "project"); File.WriteAllBytes(path, [7]);
            var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.BeforePublish) { Directory.Move(selected, moved); Directory.CreateDirectory(selected); File.WriteAllBytes(path, [9]); } } });
            Equal("DOC-CONFLICT", Save(store, path, new([1], Identity.Sha256([7]), Id())).Code);
            Equal((byte)9, File.ReadAllBytes(path)[0]); Equal((byte)7, File.ReadAllBytes(Path.Combine(moved, "project"))[0]);
            Equal(1, Directory.GetFiles(moved).Length);
        });
        Check("Store_CollidedTempAndClaim_AreNeverDeleted", () =>
        {
            string root = Root(), path = Path.Combine(root, "project"), operation = Id();
            string temp = Path.Combine(root, ProjectStore.TempName(operation)); File.WriteAllBytes(temp, [9]);
            Equal("DOC-CONFLICT", Save(new(), path, new([1], null, operation)).Code); Equal((byte)9, File.ReadAllBytes(temp)[0]); Equal(false, File.Exists(path));
            File.WriteAllBytes(path, [7]); string claim = Path.Combine(root, ProjectStore.ClaimName()); File.WriteAllBytes(claim, [8]);
            Equal("DOC-CONFLICT", Save(new(), path, new([1], Identity.Sha256([7]), Id())).Code);
            Equal((byte)8, File.ReadAllBytes(claim)[0]); Equal((byte)7, File.ReadAllBytes(path)[0]);
        });
        Check("Store_ReplacedOwnedTemp_IsPreservedAndRefused", () =>
        {
            string root = Root(), path = Path.Combine(root, "project"), operation = Id(); string temp = Path.Combine(root, ProjectStore.TempName(operation));
            var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.BeforePublish) { File.Move(temp, temp + ".moved"); File.WriteAllBytes(temp, [9]); } } });
            Equal("DOC-CONFLICT", Save(store, path, new([1], null, operation)).Code);
            Equal(false, File.Exists(path)); Equal((byte)9, File.ReadAllBytes(temp)[0]); Equal((byte)1, File.ReadAllBytes(temp + ".moved")[0]);
        });
        Check("Store_PartialWriteAndInjectedDiskFull_KeepOriginal", () =>
        {
            string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7]);
            var store = new ProjectStore(new StoreHooks { WriteFragment = 2, FailWriteAfter = 2 });
            Equal("DOC-DISK-FULL", Save(store, path, new([1, 2, 3, 4, 5], Identity.Sha256([7]), Id())).Code);
            Equal((byte)7, File.ReadAllBytes(path)[0]); Equal(1, Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
            Equal("OK", Save(new(new StoreHooks { WriteFragment = 2 }), path, new([1, 2, 3, 4, 5], Identity.Sha256([7]), Id())).Code);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }));
        });
        Check("Store_PrepublicationFaultStages_OldImageAndOwnedCleanup", () =>
        {
            foreach (StoreStage fault in new[] { StoreStage.ParentOpened, StoreStage.ClaimCreated, StoreStage.TempCreated, StoreStage.Writing, StoreStage.FileFlushed, StoreStage.BeforePublish })
            {
                string root = Root(), path = Path.Combine(root, "project"); File.WriteAllBytes(path, [7]);
                var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == fault) throw new IOException("private details"); } });
                var result = Save(store, path, new([1, 2], Identity.Sha256([7]), Id())); Equal("DOC-IO", result.Code);
                Equal(false, result.PublicationKnown); Equal((byte)7, File.ReadAllBytes(path)[0]); Equal(1, Directory.GetFiles(root).Length);
            }
        });
        Check("Store_PostpublicationFault_IsExplicitCompleteNewUncertainty", () =>
        {
            foreach (StoreStage fault in new[] { StoreStage.Published, StoreStage.BeforeDirectoryFlush })
            {
                string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7]);
                var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == fault) throw new IOException("private details"); } });
                var result = Save(store, path, new([1, 2], Identity.Sha256([7]), Id())); Equal("DOC-SAVE-UNCERTAIN", result.Code);
                Equal(true, result.PublicationKnown); Equal(false, result.DurabilityConfirmed);
                Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2 }));
            }
        });
        Check("Store_CancellationAtPublication_ResolvesCompleteNew", () =>
        {
            string path = Path.Combine(Root(), "project"); using var cancellation = new CancellationTokenSource();
            var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.Published) cancellation.Cancel(); } });
            var result = store.SaveAsync(path, new([1, 2], null, Id()), cancellation.Token).GetAwaiter().GetResult();
            Equal("OK", result.Code); Equal(true, result.PublicationKnown); Equal(true, result.DurabilityConfirmed);
        });
        Check("Store_SaveReopenSession_UsesExactDiskToken", () =>
        {
            using var session = new AuthoringSession(); session.Open(FoilSourceTests.Example, Id(), true);
            string path = Path.Combine(Root(), "project.cfdw"); byte[] captured = session.SaveImage(); var store = new ProjectStore();
            var result = Save(store, path, new(captured, null, Id())); Equal("OK", result.Code); session.AcknowledgeSaved(captured);
            var read = store.ReadAsync(path).GetAwaiter().GetResult(); using var reopened = new AuthoringSession(); reopened.Reopen(read.Image);
            Equal(session.Snapshot().AcceptedId, reopened.Snapshot().AcceptedId); Equal(Identity.Sha256(captured), read.DiskSha256);
        });
        Check("Store_ActualIoTelemetry_IsSessionBoundAndRedacted", () =>
        {
            using var session = new AuthoringSession(); using var store = new ProjectStore(session);
            string path = Path.Combine(Root(), "PRIVATE-PATH-NAME");
            Equal("OK", Save(store, path, new([1, 2, 3], null, Id())).Code);
            _ = store.ReadAsync(path).GetAwaiter().GetResult();
            var events = session.ReadLocalEvents();
            var save = events.Single(item => item.Action == "store.save"); Equal("document.save", save.Operation);
            Equal(3, save.InputBytes); Equal(3, save.OutputBytes); Equal("OK", save.Outcome); Equal(true, save.DurationMilliseconds >= 0);
            Equal(true, save.TraceId is not null); Equal(null, save.Generation); Equal(null, save.Evaluator);
            Equal(true, events.Any(item => item.Operation == "document.reopen" && item.Action == "store.read" && item.OutputBytes == 3));
            Equal(false, System.Text.Json.JsonSerializer.Serialize(events).Contains("PRIVATE-PATH-NAME", StringComparison.Ordinal));
            session.Dispose(); Equal(0, store.ReadLocalEvents().Count);
        });
        Check("Store_ReservedAsciiNamespace_RejectsCaseAliases", () =>
        {
            string root = Root(); using var store = new ProjectStore();
            foreach (string name in new[] { ".cfd-writer.claim", ".CFD-WRITER.CLAIM", ".cFd-" + Id() + ".TMP" })
            { Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, Path.Combine(root, name), new([1], null, Id())).Code); }
            Equal(0, Directory.GetFiles(root).Length);
        });
        Check("Store_DirectoryClaim_ContendsAcrossNamesAndParentAliases", () =>
        {
            string root = Root(), parent = Path.Combine(root, "Parent"); Directory.CreateDirectory(parent);
            string firstPath = Path.Combine(parent, "one"), secondPath = Path.Combine(root, "PARENT", "two"); File.WriteAllBytes(firstPath, [7]); File.WriteAllBytes(secondPath, [8]);
            using var claimed = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
            using var first = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.ClaimCreated) { claimed.Set(); Equal(true, release.Wait(TimeSpan.FromSeconds(5))); } } });
            var pending = first.SaveAsync(firstPath, new([1], Identity.Sha256([7]), Id()));
            try { Equal(true, claimed.Wait(TimeSpan.FromSeconds(5))); Equal("DOC-CONFLICT", Save(new(), secondPath, new([2], Identity.Sha256([8]), Id())).Code); }
            finally { release.Set(); Equal("OK", pending.GetAwaiter().GetResult().Code); }
            Equal((byte)8, File.ReadAllBytes(secondPath)[0]);
        });
        Check("Store_StandaloneRingBounded_CloseDiscards_RefusesNewIo", () =>
        {
            string path = Path.Combine(Root(), "project"); var store = new ProjectStore();
            for (int i = 0; i < 260; i++) Equal("DOC-CANCELLED", store.SaveAsync(path, new([1], null, Id()), new(true)).GetAwaiter().GetResult().Code);
            Equal(256, store.ReadLocalEvents().Count); Equal(true, store.ReadLocalEvents().All(e => e.Outcome == "DOC-CANCELLED" && e.PublicationKnown == false));
            store.Dispose(); Equal(0, store.ReadLocalEvents().Count); Equal("DOC-CLOSED", Save(store, path, new([1], null, Id())).Code);
            Refuses("DOC-CLOSED", () => store.ReadAsync(path).GetAwaiter().GetResult()); Equal(0, store.ReadLocalEvents().Count);
        });
        Check("Store_InjectedDispose_DoesNotCloseCallerSession", () =>
        {
            using var session = new AuthoringSession(); session.Open(FoilSourceTests.Example, Id(), true); var store = new ProjectStore(session);
            store.Dispose(); Equal(true, session.Snapshot().Source.Length > 0); Equal(true, session.ReadLocalEvents().Count > 0);
        });
        Check("Store_InFlightDispose_KeepsTruthfulPublication_NoOwnedEvents", () =>
        {
            string path = Path.Combine(Root(), "project"); using var reached = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
            var store = new ProjectStore(new StoreHooks { OnStage = stage => { if (stage == StoreStage.Published) { reached.Set(); Equal(true, release.Wait(TimeSpan.FromSeconds(5))); } } });
            var pending = store.SaveAsync(path, new([1, 2], null, Id()));
            try { Equal(true, reached.Wait(TimeSpan.FromSeconds(5))); store.Dispose(); }
            finally { release.Set(); }
            var result = pending.GetAwaiter().GetResult(); Equal("OK", result.Code); Equal(true, result.PublicationKnown); Equal(true, result.DurabilityConfirmed);
            Equal(0, store.ReadLocalEvents().Count); Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2 }));
        });
        Check("Store_IoEvents_CorrelateAndRetainFailureFacts", () =>
        {
            string root = Root(), path = Path.Combine(root, "project"); using var store = new ProjectStore();
            Equal("OK", Save(store, path, new([1, 2], null, Id())).Code);
            var successful = store.ReadLocalEvents(); Equal(4, successful.Count); Equal(1, successful.Select(e => e.TraceId).Distinct().Count());
            Equal(true, successful.Last().PublicationKnown); Equal(true, successful.Last().DurabilityConfirmed);
            Equal("DOC-CONFLICT", Save(store, path, new([9], null, Id())).Code);
            Equal("DOC-CONFLICT", store.ReadLocalEvents().Last().Outcome); Equal(false, store.ReadLocalEvents().Last().PublicationKnown);
            Equal(false, successful.Last().TraceId == store.ReadLocalEvents().Last().TraceId);
            Refuses("DOC-CANCELLED", () => store.ReadAsync(path, new(true)).GetAwaiter().GetResult());
            Equal("DOC-CANCELLED", store.ReadLocalEvents().Last().Outcome); Equal(null, store.ReadLocalEvents().Last().OutputBytes);
            Equal(false, System.Text.Json.JsonSerializer.Serialize(store.ReadLocalEvents()).Contains(root, StringComparison.Ordinal));
        });
        Check("Store_PublicationIoFailure_IsUncertainNotFalselyUnsaved", () =>
        {
            string path = Path.Combine(Root(), "project"); File.WriteAllBytes(path, [7]);
            using var store = new ProjectStore(new StoreHooks { PublicationError = 5 });
            var result = Save(store, path, new([1, 2], Identity.Sha256([7]), Id()));
            Equal("DOC-SAVE-UNCERTAIN", result.Code); Equal(false, result.PublicationKnown); Equal(false, result.DurabilityConfirmed);
            Equal((byte)7, File.ReadAllBytes(path)[0]);
        });
        Check("NativePrimitive_NonblockingFifo_AndStoreRegularOnly", () =>
        {
            string path = Path.Combine(Root(), "fifo"); Equal(0, MacProbe.MkFifo(path, 0x180));
            int fd = MacProbe.Open(path, MacProbe.NoFollow | 4, 0); Equal(true, fd >= 0);
            try { Equal(0, MacProbe.Fstat(fd, out var stat)); Equal((ushort)0x1000, (ushort)(stat.Mode & 0xf000)); }
            finally { Equal(0, MacProbe.Close(fd)); }
            using var store = new ProjectStore(); Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => store.ReadAsync(path).GetAwaiter().GetResult());
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, path, new([1], null, Id())).Code);
        });
        Check("Store_ReadCapAndRelativePaths_RefuseWithoutMutation", () =>
        {
            string root = Root(), path = Path.Combine(root, "large");
            using (var file = File.Create(path)) file.SetLength(NativeProject.MaxBytes + 1L);
            using var store = new ProjectStore(); Refuses("DOC-SIZE", () => store.ReadAsync(path).GetAwaiter().GetResult());
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, "relative", new([1], null, Id())).Code);
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, root + "/../project", new([1], null, Id())).Code);
        });
        Check("Store_ReadSymlink_UsesMacStableCapabilityRefusal", () =>
        {
            string root = Root(), target = Path.Combine(root, "original"), link = Path.Combine(root, "link");
            File.WriteAllBytes(target, [7]); File.CreateSymbolicLink(link, target);
            using var store = new ProjectStore(); Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => store.ReadAsync(link).GetAwaiter().GetResult());
            Equal((byte)7, File.ReadAllBytes(target)[0]);
        });
    }

    private static string Id() => Guid.NewGuid().ToString("D");
    private static string Root()
    {
        string temp = Path.GetTempPath(); if (temp.StartsWith("/tmp/", StringComparison.Ordinal)) temp = "/private" + temp;
        string root = Path.Combine(temp, "store-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); return root;
    }
    private static SaveResult Save(ProjectStore store, string path, SaveRequest request) => store.SaveAsync(path, request).GetAwaiter().GetResult();

    private static void ReadWriteProbe()
    {
        string root = Root(); int parent = MacProbe.Open(root, MacProbe.DirectoryFlags, 0); Equal(true, parent >= 0);
        try
        {
            int fd = MacProbe.OpenAt(parent, "temp", MacProbe.NoFollow | 0x200 | 0x800 | 2, 0x180); Equal(true, fd >= 0);
            try { Equal((nint)3, MacProbe.Write(fd, [1, 2, 3], 3)); Equal(0, MacProbe.Fsync(fd)); }
            finally { Equal(0, MacProbe.Close(fd)); }
            fd = MacProbe.OpenAt(parent, "temp", MacProbe.NoFollow, 0); Equal(true, fd >= 0);
            try { byte[] bytes = new byte[4]; Equal((nint)3, MacProbe.Read(fd, bytes, 4)); Equal((byte)3, bytes[2]); Equal((nint)0, MacProbe.Read(fd, bytes, 4)); }
            finally { Equal(0, MacProbe.Close(fd)); }
            Equal(0, MacProbe.UnlinkAt(parent, "temp", 0)); Equal(false, File.Exists(Path.Combine(root, "temp")));
        }
        finally { Equal(0, MacProbe.Close(parent)); }
    }

    private static void Probe()
    {
        string temp = Path.GetTempPath();
        if (temp.StartsWith("/tmp/", StringComparison.Ordinal)) temp = "/private" + temp;
        string root = Path.Combine(temp, "native-primitives-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "original"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(root, "replacement"), [4, 5, 6]);
        int parent = MacProbe.Open(root, MacProbe.DirectoryFlags, 0);
        Equal(true, parent >= 0);
        try
        {
            Equal(144, Marshal.SizeOf<MacProbe.Stat>());
            Equal(0, MacProbe.Fstat(parent, out var directory)); Equal((ushort)0x4000, (ushort)(directory.Mode & 0xf000));
            int original = MacProbe.OpenAt(parent, "original", MacProbe.NoFollow, 0);
            Equal(true, original >= 0);
            try
            {
                Equal(0, MacProbe.Fstat(original, out var stat)); Equal(3L, stat.Size); Equal((ushort)0x8000, (ushort)(stat.Mode & 0xf000));
                Equal(true, stat.Inode != 0);
                Equal(0, MacProbe.LinkAt(parent, "original", parent, "new", 0));
                Equal(-1, MacProbe.LinkAt(parent, "replacement", parent, "new", 0)); Equal(17, Marshal.GetLastPInvokeError());
                Equal(true, File.ReadAllBytes(Path.Combine(root, "new")).AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }));
                Equal(0, MacProbe.SymlinkAt("original", parent, "link"));
                Equal(-1, MacProbe.OpenAt(parent, "link", MacProbe.NoFollow, 0));
                Equal(0, MacProbe.RenameAt(parent, "replacement", parent, "original"));
                Equal(0, MacProbe.Fstat(original, out var held)); Equal(stat.Inode, held.Inode);
                Equal(0, MacProbe.FstatAt(parent, "original", out var published, 0x20)); Equal(false, stat.Inode == published.Inode);
                Equal(0, MacProbe.Fsync(original)); Equal(0, MacProbe.Fsync(parent));
                Console.WriteLine("NATIVE PRIMITIVE RECEIPT " + System.Text.Json.JsonSerializer.Serialize(new
                { root, directory.Device, directory.Inode, originalInode = stat.Inode, newInode = published.Inode,
                    noReplaceCollision = true, symlinkRefused = true, heldReaderOldIdentity = true, fileFlush = true, directoryFlush = true }));
            }
            finally { Equal(0, MacProbe.Close(original)); }
        }
        finally { Equal(0, MacProbe.Close(parent)); }
    }
}

// Native signatures/flags grounded in the installed MacOSX.sdk sys/fcntl.h,
// sys/stat.h and sys/unistd.h before any production adapter implementation.
internal static class MacProbe
{
    internal const int NoFollow = 0x01000100;
    internal const int DirectoryFlags = NoFollow | 0x00100000;
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
    [DllImport("libSystem.B.dylib", EntryPoint = "linkat", SetLastError = true)] internal static extern int LinkAt(int oldParent, string oldName, int newParent, string newName, int flags);
    [DllImport("libSystem.B.dylib", EntryPoint = "symlinkat", SetLastError = true)] internal static extern int SymlinkAt(string target, int parent, string name);
    [DllImport("libSystem.B.dylib", EntryPoint = "renameat", SetLastError = true)] internal static extern int RenameAt(int oldParent, string oldName, int newParent, string newName);
    [DllImport("libSystem.B.dylib", EntryPoint = "fsync", SetLastError = true)] internal static extern int Fsync(int fd);
    [DllImport("libSystem.B.dylib", EntryPoint = "close", SetLastError = true)] internal static extern int Close(int fd);
    [DllImport("libSystem.B.dylib", EntryPoint = "read", SetLastError = true)] internal static extern nint Read(int fd, byte[] bytes, nuint count);
    [DllImport("libSystem.B.dylib", EntryPoint = "write", SetLastError = true)] internal static extern nint Write(int fd, byte[] bytes, nuint count);
    [DllImport("libSystem.B.dylib", EntryPoint = "unlinkat", SetLastError = true)] internal static extern int UnlinkAt(int parent, string name, int flags);
    [DllImport("libSystem.B.dylib", EntryPoint = "mkfifo", SetLastError = true)] internal static extern int MkFifo(string path, uint mode);
}
