using CfdWorkbench.Persistence;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

[SupportedOSPlatform("windows")]
internal static class WindowsProjectStoreTests
{
    internal static void Run()
    {
        if (Environment.GetEnvironmentVariable("CFD_WIN_CRASH_STAGE") is { } crashStage) { CrashChild(crashStage); return; }
        if (Environment.GetEnvironmentVariable("CFD_WIN_CLEAR_STAGE") is { } clearStage) { ClearCrashChild(clearStage); return; }
        Check("WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped", () =>
        {
            var failure = new WindowsNative.NativeFailure(null, 183);
            Equal<string?>(null, failure.ProductCode);
        });
        Check("WindowsStore_NtfsFixture_SaveOpenReopenSucceeds", () =>
        {
            string root = TestTemp.Combine("cfd-win-native-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string path = Path.Combine(root, "project.cfdw");
                using var store = new ProjectStore();
                var created = store.SaveAsync(path, new SaveRequest([1, 2, 3], null, Guid.NewGuid().ToString("D"))).GetAwaiter().GetResult();
                Equal("OK", created.Code);
                Equal(true, created.PublicationKnown);
                Equal(true, created.DurabilityConfirmed);
                var first = store.ReadAsync(path).GetAwaiter().GetResult();
                Equal(true, first.Image.AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }));
                Equal(created.PublishedSha256, first.DiskSha256);
                var replaced = store.SaveAsync(path, new SaveRequest([4, 5], first.DiskSha256, Guid.NewGuid().ToString("D"))).GetAwaiter().GetResult();
                Equal("OK", replaced.Code);
                var second = store.ReadAsync(path).GetAwaiter().GetResult();
                Equal(true, second.Image.AsSpan().SequenceEqual(new byte[] { 4, 5 }));
                Equal(replaced.PublishedSha256, second.DiskSha256);
                Equal(1, Directory.GetFileSystemEntries(root).Length);
            }
            finally { Directory.Delete(root, true); }
        });
        Check("WindowsNative_Environment_RealNtfsX64", () =>
        {
            // Ruling 238: Windows x64 and ARM64 are both admitted; the NATIVE-ENV line records which one ran.
            Equal(true, RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64);
            Equal("NTFS", new DriveInfo(Path.GetPathRoot(TestTemp.Root)!).DriveFormat);
            Equal(true, OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393));
            Console.WriteLine($"NATIVE-ENV os={Environment.OSVersion.Version} arch={RuntimeInformation.ProcessArchitecture} filesystem=NTFS");
        });
        Check("WindowsNative_Layouts_SdkX64", () =>
        {
            Layout<WindowsNative.UnicodeString>(16, ("Length", 0), ("MaximumLength", 2), ("Buffer", 8));
            Layout<WindowsNative.ObjectAttributes>(48, ("Length", 0), ("RootDirectory", 8), ("ObjectName", 16),
                ("Attributes", 24), ("SecurityDescriptor", 32), ("SecurityQualityOfService", 40));
            Layout<WindowsNative.IoStatusBlock>(16, ("Status", 0), ("Information", 8));
            Layout<WindowsNative.FileId128>(16, ("Low", 0), ("High", 8));
            Layout<WindowsNative.FileId>(24, ("VolumeSerialNumber", 0), ("Id", 8));
            Layout<WindowsNative.RenameHeader>(24, ("Flags", 0), ("RootDirectory", 8), ("FileNameLength", 16), ("FirstCharacter", 20));
            Layout<WindowsNative.Disposition>(4, ("Flags", 0));
            Layout<WindowsNative.FileAttributeTag>(8, ("Attributes", 0), ("ReparseTag", 4));
            Layout<WindowsNative.FileStandard>(24, ("AllocationSize", 0), ("EndOfFile", 8), ("NumberOfLinks", 16), ("DeletePending", 20), ("Directory", 21));
            Layout<WindowsNative.CaseSensitiveInfo>(4, ("Flags", 0));
            Layout<WindowsNative.SecurityAttributes>(24, ("Length", 0), ("SecurityDescriptor", 8), ("InheritHandle", 16));
            Layout<WindowsNative.FileNamesHeader>(12, ("NextEntryOffset", 0), ("FileIndex", 4), ("FileNameLength", 8)); // ntifs.h FILE_NAMES_INFORMATION before the name
        });
        Check("WindowsNative_RelativeCreate_PrivateAclBeforeAnyBytes", () => Fixture((root, parent) =>
        {
            using var file = WindowsNative.CreatePrivate(parent, "private.tmp");
            Equal(0L, RandomAccess.GetLength(file));
            Equal(CanonicalSddl(WindowsNative.PrivateDescriptor()), CanonicalSddl(WindowsNative.ReadOwnerAndDacl(file)));
            RandomAccess.Write(file, [1, 2, 3], 0);
            WindowsNative.Flush(file);
            Equal(true, Read(file).AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }));
        }));
        // Ruling 145 qualification/readiness cases; measured cost is recorded in the fresh receipt.
        Check("WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage", () => Fixture((root, parent) =>
        {
            using (var old = WindowsNative.CreatePrivate(parent, "project.cfdw"))
            { RandomAccess.Write(old, [1, 1], 0); WindowsNative.Flush(old); }
            using var reader = WindowsNative.OpenReader(parent, "project.cfdw",
                WindowsNative.ShareRead | WindowsNative.ShareDelete);
            var oldIdentity = WindowsNative.Identity(reader);
            using var temp = WindowsNative.CreatePrivate(parent, "new.tmp");
            RandomAccess.Write(temp, [2, 2, 2], 0); WindowsNative.Flush(temp);
            var newIdentity = WindowsNative.Identity(temp);

            var completion = WindowsNative.Rename(temp, parent, "project.cfdw", replace: true);
            Equal(new WindowsNative.RenameCompletion(0, 0, 0), completion);
            Equal(newIdentity, WindowsNative.Identity(temp));
            temp.Dispose();

            Equal(true, Read(reader).AsSpan().SequenceEqual(new byte[] { 1, 1 }));
            Equal(oldIdentity, WindowsNative.Identity(reader));
            using var fresh = WindowsNative.OpenReader(parent, "project.cfdw");
            Equal(true, Read(fresh).AsSpan().SequenceEqual(new byte[] { 2, 2, 2 }));
            Equal(newIdentity, WindowsNative.Identity(fresh));
            Equal(false, oldIdentity == newIdentity);
            Equal(false, File.Exists(Path.Combine(root, "new.tmp")));
            Console.WriteLine($"SHARE-READ-DELETE NTSTATUS=0x{completion.NtStatus:x8} IO_STATUS=0x{completion.IoStatus:x8} Win32={completion.Win32Error} old={oldIdentity} new={newIdentity} held_bytes=0101 fresh_bytes=020202 temp_exists=false");
        }));
        Check("WindowsNative_Replace_ShareReadOnlyReaderRefused", () => Fixture((root, parent) =>
        {
            using (var old = WindowsNative.CreatePrivate(parent, "project.cfdw"))
            { RandomAccess.Write(old, [1, 1], 0); WindowsNative.Flush(old); }
            using var reader = WindowsNative.OpenReader(parent, "project.cfdw", WindowsNative.ShareRead);
            var oldIdentity = WindowsNative.Identity(reader);
            using var temp = WindowsNative.CreatePrivate(parent, "new.tmp");
            RandomAccess.Write(temp, [2, 2, 2], 0); WindowsNative.Flush(temp);
            var tempIdentity = WindowsNative.Identity(temp);

            WindowsNative.NativeFailure? refusal = null;
            try { WindowsNative.Rename(temp, parent, "project.cfdw", replace: true); }
            catch (WindowsNative.NativeFailure error) { refusal = error; }

            Equal(true, refusal is not null);
            Equal(32, refusal!.Win32Error); // ERROR_SHARING_VIOLATION
            Equal(unchecked((int)0xc0000043), refusal.NtStatus); // STATUS_SHARING_VIOLATION
            // The IO_STATUS_BLOCK of a refused NtSetInformationFile reads 0 on this laptop (Windows 11 build 28000) and the NTSTATUS
            // on the GitHub windows-latest runner: both are the same refusal; the API documents neither value.
            Equal(true, refusal.IoStatus == 0 || refusal.IoStatus == refusal.NtStatus);
            Equal(oldIdentity, WindowsNative.Identity(reader));
            Equal(true, Read(reader).AsSpan().SequenceEqual(new byte[] { 1, 1 }));
            using var fresh = WindowsNative.OpenReader(parent, "project.cfdw");
            Equal(oldIdentity, WindowsNative.Identity(fresh));
            Equal(true, Read(fresh).AsSpan().SequenceEqual(new byte[] { 1, 1 }));
            Equal(tempIdentity, WindowsNative.Identity(temp));
            Equal(true, Read(temp).AsSpan().SequenceEqual(new byte[] { 2, 2, 2 }));
            Equal(true, File.Exists(Path.Combine(root, "new.tmp")));
            Equal(2, Directory.GetFileSystemEntries(root).Length);
            Console.WriteLine($"SHARE-READ-ONLY {refusal.Message} incumbent={oldIdentity} temp={tempIdentity} held_bytes=0101 fresh_bytes=0101 temp_bytes=020202 temp_exists=true");
            Equal("DOC-CONFLICT", refusal.ProductCode);
        }));
        Check("WindowsNative_ClaimDispose_HeldObserverNamespaceRemoved", () => Fixture((root, parent) =>
        {
            using var claim = WindowsNative.CreatePrivate(parent, "claim.tmp");
            RandomAccess.Write(claim, [7], 0);
            using var observer = WindowsNative.OpenReader(parent, "claim.tmp",
                WindowsNative.ShareRead | WindowsNative.ShareWrite | WindowsNative.ShareDelete);

            WindowsNative.DisposeEntry(claim);
            // Microsoft POSIX contract removes the link when the deleting handle closes.
            // The independently held observer must remain open across this assertion.
            claim.Dispose();

            Equal(false, Directory.EnumerateFileSystemEntries(root).Any());
            Equal(true, Read(observer).AsSpan().SequenceEqual(new byte[] { 7 }));
        }));
        Check("WindowsNative_Rename_FreshWin32VersusNtFixtures", () =>
        {
            Fixture((root, parent) =>
            {
                using var source = WindowsNative.CreatePrivate(parent, "source.tmp");
                RandomAccess.Write(source, [4, 5], 0); WindowsNative.Flush(source);
                bool failed = false;
                try { WindowsNative.ProbeWin32Rename(source, parent, "destination.cfdw", replace: false); }
                catch (WindowsNative.NativeFailure error)
                { failed = true; Console.WriteLine("FRESH-WIN32 " + error.Message); Equal(87, error.Win32Error); }
                Equal(true, failed);
                Equal(true, Read(source).AsSpan().SequenceEqual(new byte[] { 4, 5 }));
                Equal(true, File.Exists(Path.Combine(root, "source.tmp")));
                Equal(false, File.Exists(Path.Combine(root, "destination.cfdw")));
            });
            Fixture((root, parent) =>
            {
                using var source = WindowsNative.CreatePrivate(parent, "source.tmp");
                RandomAccess.Write(source, [4, 5], 0); WindowsNative.Flush(source);
                var identity = WindowsNative.Identity(source);
                var completion = WindowsNative.Rename(source, parent, "destination.cfdw", replace: false);
                Console.WriteLine($"FRESH-NT NTSTATUS=0x{completion.NtStatus:x8} IO_STATUS=0x{completion.IoStatus:x8} Win32={completion.Win32Error}");
                using var destination = WindowsNative.OpenReader(parent, "destination.cfdw",
                    WindowsNative.ShareRead | WindowsNative.ShareWrite | WindowsNative.ShareDelete);
                Equal(identity, WindowsNative.Identity(destination));
                Equal(true, Read(destination).AsSpan().SequenceEqual(new byte[] { 4, 5 }));
                Equal(false, File.Exists(Path.Combine(root, "source.tmp")));
            });
        });
        Check("WindowsNative_Rename_AttributionNullRootExAndRootedLegacy", () =>
        {
            Probe("WIN32-EX22-NULL-ROOT-ABSOLUTE", 0, (source, parent, root) =>
                WindowsNative.ProbeWin32AbsoluteRename(source, parent, Path.Combine(root, "destination.cfdw")));
            Probe("WIN32-CLASS3-ROOTED", 87, (source, parent, root) =>
                WindowsNative.ProbeWin32LegacyRename(source, parent, "destination.cfdw"));
        });
        Check("WindowsNative_CreateOnly_CollisionPreservesBothObjects", () => Fixture((root, parent) =>
        {
            using var incumbent = WindowsNative.CreatePrivate(parent, "project.cfdw");
            RandomAccess.Write(incumbent, [9], 0);
            using var temp = WindowsNative.CreatePrivate(parent, "new.tmp");
            RandomAccess.Write(temp, [8], 0); WindowsNative.Flush(temp);
            var identity = WindowsNative.Identity(incumbent);

            bool refused = false;
            try { WindowsNative.Rename(temp, parent, "project.cfdw", replace: false); }
            catch (WindowsNative.NativeFailure error)
            { Console.WriteLine("COLLISION-RAW " + error.Message); refused = true; Equal(true, error.Win32Error is 80 or 183); }

            Equal(true, refused);
            Equal(identity, WindowsNative.Identity(incumbent));
            Equal(true, Read(incumbent).AsSpan().SequenceEqual(new byte[] { 9 }));
            Equal(true, Read(temp).AsSpan().SequenceEqual(new byte[] { 8 }));
            Equal(2, Directory.GetFileSystemEntries(root).Length);
        }));
        Check("WindowsNative_RelativeName_TraversalRefusedBeforeCreate", () => Fixture((root, parent) =>
        {
            string[] invalid = ["", ".", "..", "../outside", "..\\outside", "file:stream", "bad\0name", new string('a', 256)];
            foreach (string name in invalid)
            {
                bool refused = false;
                try { using var file = WindowsNative.CreatePrivate(parent, name); }
                catch (ArgumentException) { refused = true; }
                Equal(true, refused);
            }
            // Boundary oracle: largest admitted simple name succeeds; no full-path claim is made.
            using var boundary = WindowsNative.CreatePrivate(parent, new string('a', 255));
            Equal(1, Directory.GetFileSystemEntries(root).Length);
        }));
        // Ruling 135 binding conditions, through the public store: the held-reader overwrite pair (red on the closed store,
        // green on the adapter), path admission, long path, reparse points, case-sensitive directories, crash leftovers.
        Check("WindowsStore_Overwrite_ShareReadDeleteReaderKeepsOldImage", OverwriteKeepsHeldReader);
        Check("WindowsStore_Overwrite_ShareReadOnlyReaderRefused_IncumbentKept", OverwriteRefusedByShareReadReader);
        Check("WindowsStore_CreatedFiles_PrivateDescriptorBeforeAnyByteAndAfterPublication", PrivateBeforeAnyByte);
        Check("WindowsStore_PathAdmission_RefusedBeforeAnyWrite", PathAdmission);
        Check("WindowsStore_LongPath_Over260_SaveOpenRoundTrip", LongPath);
        Check("WindowsStore_ReparseAndHardLink_RefusedNothingWrittenThrough", ReparseAndHardLink);
        Check("WindowsStore_CaseSensitiveDirectory_QueryReadsFlag", CaseSensitiveQuery);
        CaseSensitiveRefusal(); // registers its check only where the fixture can be made; otherwise a NOT ASSESSED line and no PASS
        // Repair cycle 2: OneDrive placeholders are disguised in a normal process (mode 1), so the walk must expose them.
        Check("WindowsStore_PlaceholderMode_ExposedInSaveWalkAndCloudFolder", PlaceholderModeExposed);
        Check("WindowsStore_Save_UnderEnvironmentOneDriveFolder_RefusedNothingWritten", SaveUnderOneDriveRefused);
        Check("WindowsStore_VolumeDevice_RemoteRemovableRefused_LocalAdmitted", VolumeDevice);
        Check("WindowsStore_ForeignDaclTarget_Overwrite", ForeignDaclTarget);
        Check("WindowsStore_ReadOnlyTarget_OverwriteRefused", ReadOnlyTarget);
        EfsTarget();
        Check("WindowsStore_AncestorRenameRefusedWhileRetained", AncestorRenameRefused);
        Check("WindowsStore_OwnedTempRenameWriteDeleteRefusedWhileHeld", OwnedTempProtected);
        Check("WindowsStore_IoEvents_NoDirectoryFlushClaimed", EventsWithoutDirectoryFlush);
        Check("WindowsStore_CrashInjection_LeftoverInventoryMeasured", CrashInjection);
        // Windows store recovery, node A (docs/design/windows-store-recovery.md 2.2 to 2.4, section 7): Clear unfinished save.
        Check("WindowsStore_Clear_RemovesCrashLeftovers_ClaimLast", ClearRemovesCrashLeftovers);
        Check("WindowsStore_Clear_ForeignClaim_Kept_NothingRemoved", ClearForeignClaim);
        Check("WindowsStore_Clear_LiveWriterClaimHeld_RefusedInUse", ClearLiveWriterClaim);
        Check("WindowsStore_Clear_LiveCreateOnlyTemp_RefusedInUse", ClearLiveCreateOnlyTemp);
        Check("WindowsStore_Clear_NewEntryBetweenChecks_RefusedNothingRemoved", ClearNewEntryBetweenChecks);
        Check("WindowsStore_Clear_CrashDuringClear_ClaimLastAndRetryConverges", ClearCrashDuringClear);
        Check("WindowsStore_Clear_HardLinkOrReparseCandidate_Kept", ClearHardLinkOrReparse);
        Check("WindowsStore_Clear_UnderOneDrive_RefusedNothingRemoved", ClearUnderOneDrive);
        Check("WindowsStore_Clear_TelemetryEventCodeAndCounts", ClearTelemetry);
        Check("WindowsStore_Clear_CaseVariantClaim_NotReportedCleared", ClearCaseVariantClaim);
        Check("WindowsStore_Clear_ReadOnlyCandidate_FailsAtC5Delete_DocIo_NothingRemoved", ClearReadOnlyCandidateFailsAtDelete);
        Check("WindowsStore_Clear_DeletePendingCandidate_C2OpenError_FailedNothingRemoved", ClearNonSharingOpenError);
        Check("WindowsStore_Clear_ReaderOnTemp_NotInUse", ClearReaderOnTemp);
        Check("WindowsNative_ListNames_ManyEntriesAcrossBuffers_ExactSet", ClearListsEntries);
        // Ruling 160 (1) and (5): the OneDrive refusal and its fail-closed form, decided in Persistence so no window is needed.
        Check("WindowsStore_CloudFolder_OneDriveEnvironmentPathRefused", CloudEnvironmentPath);
        Check("WindowsStore_CloudFolder_CloudAttributeOrReparseTagOnFolderOrAncestorRefused", CloudAttributes);
        Check("WindowsStore_CloudFolder_CheckFailureRefusedFailClosed", CloudCheckFailure);
        Check("WindowsStore_CloudFolder_LocalFolderAdmitted_RealAttributeQuery", CloudLocalFolder);
    }

    private static (uint, uint) Plain(string path) => (0x10u, 0u); // FILE_ATTRIBUTE_DIRECTORY, no reparse tag

    private static void CloudEnvironmentPath()
    {
        string root = NewRoot(), oneDrive = Path.Combine(root, "OneDrive"), inside = Path.Combine(oneDrive, "Projects", "wing");
        foreach (string variable in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
        {
            Func<string, string?> env = name => name == variable ? oneDrive : null;
            Equal(FolderLocation.Cloud, CloudFolder.Classify(inside, env, Plain));
            Equal(FolderLocation.Cloud, CloudFolder.Classify(oneDrive + @"\", env, Plain));
            Equal(FolderLocation.Cloud, CloudFolder.Classify(inside.ToUpperInvariant(), env, Plain));
            Equal(FolderLocation.Cloud, CloudFolder.Classify(Path.Combine(oneDrive, "..", "OneDrive", "x"), env, Plain));
            Equal(FolderLocation.Local, CloudFolder.Classify(oneDrive + "-archive", env, Plain)); // a sibling that shares the prefix
            Equal(FolderLocation.Local, CloudFolder.Classify(Path.Combine(root, "elsewhere"), env, Plain));
        }
        Equal(FolderLocation.Local, CloudFolder.Classify(inside, _ => null, Plain)); // no variable set and no attribute: local
    }

    private static void CloudAttributes()
    {
        string root = NewRoot(), folder = Path.Combine(root, "a", "b");
        // winnt.h: RECALL_ON_OPEN 0x40000, PINNED 0x80000, UNPINNED 0x100000, RECALL_ON_DATA_ACCESS 0x400000.
        foreach (uint attribute in new uint[] { 0x40000, 0x80000, 0x100000, 0x400000 })
        {
            Equal(FolderLocation.Cloud, CloudFolder.Classify(folder, _ => null, path => path == folder ? (0x10u | attribute, 0u) : (0x10u, 0u)));
            Equal(FolderLocation.Cloud, CloudFolder.Classify(folder, _ => null, path => path == root ? (0x10u | attribute, 0u) : (0x10u, 0u)));
        }
        // IO_REPARSE_TAG_CLOUD, IO_REPARSE_TAG_CLOUD_F, IO_REPARSE_TAG_ONEDRIVE on an ancestor.
        foreach (uint tag in new uint[] { 0x9000001A, 0x9000F01A, 0x9000701A, 0x80000021 })
            Equal(FolderLocation.Cloud, CloudFolder.Classify(folder, _ => null, path => path == Path.GetDirectoryName(folder) ? (0x410u, tag) : (0x10u, 0u)));
        // A symbolic link tag (IO_REPARSE_TAG_SYMLINK 0xA000000C) is not a cloud tag; the store refuses it on its own.
        Equal(FolderLocation.Local, CloudFolder.Classify(folder, _ => null, path => path == folder ? (0x410u, 0xA000000C) : (0x10u, 0u)));
        // The walk reaches the volume root.
        var visited = new List<string>(); CloudFolder.Classify(folder, _ => null, path => { visited.Add(path); return (0x10u, 0u); });
        Equal<string?>(Path.GetPathRoot(folder), visited.Last()); Equal(folder, visited.First());
    }

    private static void CloudCheckFailure()
    {
        string root = NewRoot();
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(root, _ => null, _ => throw new IOException("denied")));
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(root, _ => null, _ => throw new WindowsNative.NativeFailure(null, 5)));
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(root, _ => null, _ => throw new UnauthorizedAccessException()));
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(@"\\server\share\folder", _ => null, Plain));
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(root, name => name == "OneDrive" ? "not-a-drive-path" : null, Plain));
        // The real query on a folder that does not exist cannot say; it refuses rather than guess.
        Equal(FolderLocation.CheckFailed, CloudFolder.Classify(Path.Combine(root, "missing")));
    }

    private static void CloudLocalFolder()
    {
        string root = NewRoot();
        Equal(FolderLocation.Local, CloudFolder.Classify(root, _ => null, path => { var tag = WindowsNative.AttributeTag(path); return (tag.Attributes, tag.ReparseTag); }));
        Equal(FolderLocation.Local, CloudFolder.Classify(root)); // the real environment variables: the test folder is not under OneDrive
        // A real cloud attribute on a folder, where this account may set one (attrib +P sets FILE_ATTRIBUTE_PINNED).
        string pinned = Path.Combine(root, "pinned"); Directory.CreateDirectory(pinned);
        var start = new System.Diagnostics.ProcessStartInfo("attrib.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("+P"); start.ArgumentList.Add(pinned);
        using var process = System.Diagnostics.Process.Start(start)!; process.StandardOutput.ReadToEnd(); process.StandardError.ReadToEnd(); process.WaitForExit(30_000);
        bool set = process.ExitCode == 0 && ((uint)File.GetAttributes(pinned) & 0x80000) != 0;
        if (set) Equal(FolderLocation.Cloud, CloudFolder.Classify(pinned));
        Console.WriteLine("CLOUD-REAL-ATTRIBUTE pinned_set=" + set + (set ? " classified=Cloud" : " (attrib +P not accepted here; the seam cases above cover the attribute)"));
    }

    private static string Id() => Guid.NewGuid().ToString("D");
    private static string NewRoot() => TestTemp.NewDirectory("winstore-");
    private static SaveResult Save(ProjectStore store, string path, SaveRequest request) => store.SaveAsync(path, request).GetAwaiter().GetResult();

    private static void OverwriteKeepsHeldReader()
    {
        string path = Path.Combine(NewRoot(), "project.cfdw"); using var store = new ProjectStore();
        var created = Save(store, path, new([1, 1], null, Id())); Equal("OK", created.Code);
        using var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var replaced = Save(store, path, new([2, 2, 2], created.PublishedSha256, Id()));
        Equal("OK", replaced.Code); Equal(true, replaced.DurabilityConfirmed);
        byte[] old = new byte[4]; Equal(2, held.Read(old, 0, 4)); Equal(true, old.AsSpan(0, 2).SequenceEqual(new byte[] { 1, 1 }));
        Equal(true, store.ReadAsync(path).GetAwaiter().GetResult().Image.AsSpan().SequenceEqual(new byte[] { 2, 2, 2 }));
        Equal(1, Directory.GetFileSystemEntries(Path.GetDirectoryName(path)!).Length);
        Console.WriteLine("STORE-HELD-READER-OVERWRITE code=OK held_bytes=0101 fresh_bytes=020202 entries=1");
    }

    private static void OverwriteRefusedByShareReadReader()
    {
        string path = Path.Combine(NewRoot(), "project.cfdw"); using var store = new ProjectStore();
        var created = Save(store, path, new([1, 1], null, Id())); Equal("OK", created.Code);
        SaveResult refused;
        using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            refused = Save(store, path, new([2, 2, 2], created.PublishedSha256, Id()));
            Equal("DOC-CONFLICT", refused.Code); // Win32 32 maps to the stable product code, never DOC-IO (Ruling 145 (3))
            Equal(false, refused.PublicationKnown); Equal(false, refused.DurabilityConfirmed);
            byte[] bytes = new byte[4]; Equal(2, held.Read(bytes, 0, 4));
            Equal(1, Directory.GetFileSystemEntries(Path.GetDirectoryName(path)!).Length); // temp and claim removed through their own handles
        }
        Equal(true, store.ReadAsync(path).GetAwaiter().GetResult().Image.AsSpan().SequenceEqual(new byte[] { 1, 1 }));
        Equal("OK", Save(store, path, new([2, 2, 2], created.PublishedSha256, Id())).Code); // the retry succeeds once the reader is gone
        Console.WriteLine("STORE-SHARE-READ-ONLY-REFUSAL code=" + refused.Code + " incumbent_bytes=0101 entries_after_refusal=1");
    }

    private static void PrivateBeforeAnyByte()
    {
        string root = NewRoot(), path = Path.Combine(root, "project.cfdw"); var seen = new List<string>();
        foreach (bool overwrite in new[] { false, true })
        {
            string operation = Id(); seen.Clear();
            if (overwrite) File.WriteAllBytes(path, [7]); // inherited DACL: the replacement is private anyway
            using var store = new ProjectStore(new StoreHooks { OnStage = stage =>
            {
                if (stage is not (StoreStage.TempCreated or StoreStage.ClaimCreated)) return;
                string entry = Path.Combine(root, stage == StoreStage.TempCreated ? ProjectStore.TempName(operation) : ProjectStore.ClaimName());
                Equal(0L, new FileInfo(entry).Length); AssertPrivateFile(entry); seen.Add(stage.ToString());
            } });
            var result = Save(store, path, new([1, 2, 3], overwrite ? Identity.Sha256([7]) : null, operation));
            Equal("OK", result.Code); Equal(overwrite ? 2 : 1, seen.Count); AssertPrivateFile(path);
        }
    }

    private static void PathAdmission()
    {
        string root = NewRoot(); using var store = new ProjectStore();
        string[] refused = ["relative.cfdw", @"C:drive-relative.cfdw", @"\\server\share\project.cfdw", @"\\?\" + Path.Combine(root, "x.cfdw"),
            root + "/forward.cfdw", Path.Combine(root, "NUL"), Path.Combine(root, "com1.txt"), Path.Combine(root, "a:stream"),
            Path.Combine(root, "trailing."), Path.Combine(root, "trailing "), Path.Combine(root, ".cfd-claim"), Path.Combine(root, ".CFD-x.TMP"),
            Path.Combine(root, "a|b"), Path.Combine(root, "q?x"), Path.Combine(root, new string('x', 256)), Path.Combine(root, "..", "up.cfdw")];
        foreach (string path in refused)
        {
            var result = Save(store, path, new([1], null, Id()));
            if (result.Code != "DOC-UNSUPPORTED-PERSISTENCE") throw new InvalidOperationException($"{path.Replace(root, "<root>")} -> {result.Code}");
            Equal(false, result.PublicationKnown);
        }
        Equal("DOC-CONFLICT", Save(store, Path.Combine(root, "missing-dir", "x.cfdw"), new([1], null, Id())).Code); // ERROR_PATH_NOT_FOUND
        Equal(0, Directory.GetFileSystemEntries(root).Length);
        // A directory in the leaf's place is refused too (design section 4: a disk regular file for leaves).
        string folder = Path.Combine(root, "folder.cfdw"); Directory.CreateDirectory(folder);
        Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, folder, new([1], null, Id())).Code);
        Equal(1, Directory.GetFileSystemEntries(root).Length);
    }

    private static void LongPath()
    {
        string dir = NewRoot();
        for (int i = 0; i < 3; i++) dir = Path.Combine(dir, new string((char)('a' + i), 120));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "project-" + new string('p', 60) + ".cfdw");
        Equal(true, path.Length > 400);
        using var store = new ProjectStore();
        var created = Save(store, path, new([4, 5, 6], null, Id())); Equal("OK", created.Code);
        Equal(true, store.ReadAsync(path).GetAwaiter().GetResult().Image.AsSpan().SequenceEqual(new byte[] { 4, 5, 6 }));
        Equal("OK", Save(store, path, new([7], created.PublishedSha256, Id())).Code);
        Console.WriteLine($"STORE-LONG-PATH utf16_units={path.Length} components={path.Split('\\').Length}");
    }

    private static void RunProcess(string file, params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        process.StandardOutput.ReadToEnd(); process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30_000)) { process.Kill(); throw new TimeoutException(file); }
        Equal(0, process.ExitCode);
    }

    private static void ReparseAndHardLink()
    {
        string root = NewRoot(), real = Path.Combine(root, "real"); Directory.CreateDirectory(real);
        File.WriteAllBytes(Path.Combine(real, "inside.cfdw"), [7]);
        using var store = new ProjectStore();
        // An ancestor that is a junction: nothing is written through it.
        string junction = Path.Combine(root, "junction");
        Equal(true, TestLinks.TryDirectoryLink(junction, real)); // a junction on Windows: it needs no privilege
        Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, Path.Combine(junction, "new.cfdw"), new([1], null, Id())).Code);
        Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, Path.Combine(junction, "inside.cfdw"), new([1], Identity.Sha256([7]), Id())).Code);
        Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => store.ReadAsync(Path.Combine(junction, "inside.cfdw")).GetAwaiter().GetResult());
        Equal(1, Directory.GetFileSystemEntries(real).Length); Equal((byte)7, File.ReadAllBytes(Path.Combine(real, "inside.cfdw"))[0]);
        // A leaf that is a symbolic link (when this account may create one) or, failing that, a junction.
        string leaf = Path.Combine(root, "leaf.cfdw");
        string kind = TestLinks.TryFileLink(leaf, Path.Combine(real, "inside.cfdw")) ? "file-symlink" : "junction-as-leaf";
        if (kind == "junction-as-leaf") Equal(true, TestLinks.TryDirectoryLink(leaf, real));
        Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, leaf, new([1], Identity.Sha256([7]), Id())).Code);
        Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => store.ReadAsync(leaf).GetAwaiter().GetResult());
        Equal((byte)7, File.ReadAllBytes(Path.Combine(real, "inside.cfdw"))[0]);
        // A second hard link: link count other than one is refused for read and overwrite, both names preserved.
        string linked = Path.Combine(root, "linked.cfdw");
        RunProcess("cmd.exe", "/c", "mklink", "/H", linked, Path.Combine(real, "inside.cfdw"));
        Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, linked, new([1], Identity.Sha256([7]), Id())).Code);
        Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => store.ReadAsync(linked).GetAwaiter().GetResult());
        Equal((byte)7, File.ReadAllBytes(linked)[0]); Equal((byte)7, File.ReadAllBytes(Path.Combine(real, "inside.cfdw"))[0]);
        Equal("OK", Save(store, Path.Combine(root, "control.cfdw"), new([1], null, Id())).Code); // a plain folder beside the refused ones is admitted
        Console.WriteLine("STORE-REPARSE ancestor=junction leaf=" + kind + " hardlink=refused control=OK");
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int informationClass, ref uint information, uint length);

    private static void CaseSensitiveQuery()
    {
        string normal = NewRoot(); using var store = new ProjectStore();
        Equal("OK", Save(store, Path.Combine(normal, "project.cfdw"), new([1], null, Id())).Code);
        using (var directory = WindowsNative.OpenFixtureDirectory(normal)) Equal(false, WindowsNative.IsCaseSensitive(directory));
    }

    // The refusal branch needs a case-sensitive directory, which some volumes refuse to create (Win32 5, observed on D: and not on C:, unelevated). Then it is NOT ASSESSED
    // and prints no PASS; the query above is the only thing observed here. winbase.h FileCaseSensitiveInfo = 23, FILE_CS_FLAG_CASE_SENSITIVE_DIR = 1.
    private static void CaseSensitiveRefusal()
    {
        string sensitive = NewRoot(); uint flags = 1; int enableError = 0;
        using (var handle = CreateFileW(sensitive, 0x100 | 0x00100000, 7, 0, 3, 0x02000000, 0)) // FILE_WRITE_ATTRIBUTES | SYNCHRONIZE; OPEN_EXISTING; BACKUP_SEMANTICS
        {
            if (handle.IsInvalid || !SetFileInformationByHandle(handle, 23, ref flags, 4)) enableError = Marshal.GetLastWin32Error();
        }
        if (enableError != 0)
        {
            Console.WriteLine($"NOT ASSESSED WindowsStore_CaseSensitiveDirectory_RefusalBranch volume-refuses-case-sensitive-flag Win32={enableError}");
            return;
        }
        Check("WindowsStore_CaseSensitiveDirectory_RefusalBranch", () =>
        {
            using var store = new ProjectStore();
            Equal("DOC-UNSUPPORTED-PERSISTENCE", Save(store, Path.Combine(sensitive, "project.cfdw"), new([1], null, Id())).Code);
            Equal(0, Directory.GetFileSystemEntries(sensitive).Length);
        });
    }

    // ntifs.h: RtlQueryThreadPlaceholderCompatibilityMode returns the calling thread's mode; PHCM_DISGUISE_PLACEHOLDER = 1, PHCM_EXPOSE_PLACEHOLDERS = 2.
    [DllImport("ntdll.dll")]
    private static extern sbyte RtlQueryThreadPlaceholderCompatibilityMode();

    private static void WithEnvironment(string name, string? value, Action action)
    {
        string? old = Environment.GetEnvironmentVariable(name);
        try { Environment.SetEnvironmentVariable(name, value); action(); }
        finally { Environment.SetEnvironmentVariable(name, old); }
    }

    private static void PlaceholderModeExposed()
    {
        sbyte before = RtlQueryThreadPlaceholderCompatibilityMode();
        var stages = new List<sbyte>();
        using var store = new ProjectStore(new StoreHooks { OnStage = _ => stages.Add(RtlQueryThreadPlaceholderCompatibilityMode()) });
        Equal("OK", Save(store, Path.Combine(NewRoot(), "project.cfdw"), new([1], null, Id())).Code);
        Equal(true, stages.Count > 0 && stages.All(mode => mode == 2)); // the Save walk and everything after it ran exposed
        Equal(before, RtlQueryThreadPlaceholderCompatibilityMode());    // and the thread is back in the mode it had
        var walked = new List<sbyte>();
        CloudFolder.Classify(NewRoot(), _ => null, _ => { walked.Add(RtlQueryThreadPlaceholderCompatibilityMode()); return (0x10u, 0u); });
        Equal(true, walked.Count > 0 && walked.All(mode => mode == 2));
        Equal(before, RtlQueryThreadPlaceholderCompatibilityMode());
        Console.WriteLine($"PLACEHOLDER-MODE thread_before={before} save_stages={stages.Count} all_exposed=true cloud_reads={walked.Count} all_exposed=true restored=true");
    }

    private static void SaveUnderOneDriveRefused()
    {
        string root = NewRoot(), oneDrive = Path.Combine(root, "OneDrive"), path = Path.Combine(oneDrive, "project.cfdw");
        Directory.CreateDirectory(oneDrive);
        using var plain = new ProjectStore();
        var created = Save(plain, path, new([1, 2], null, Id())); Equal("OK", created.Code); // seeded before the variable names this folder
        var stages = new List<StoreStage>();
        WithEnvironment("OneDrive", oneDrive, () =>
        {
            using var store = new ProjectStore(new StoreHooks { OnStage = stages.Add });
            foreach (var request in new[] { new SaveRequest([9], created.PublishedSha256, Id()), new SaveRequest([9], null, Id()) })
            {
                var result = Save(store, request.CreateOnly ? Path.Combine(oneDrive, "new.cfdw") : path, request);
                Equal("DOC-UNSUPPORTED-PERSISTENCE", result.Code); Equal(false, result.PublicationKnown); Equal(false, result.DurabilityConfirmed);
            }
        });
        Equal(false, stages.Any(stage => stage is StoreStage.ClaimCreated or StoreStage.TempCreated or StoreStage.Writing));
        Equal(1, Directory.GetFileSystemEntries(oneDrive).Length); // no temp, no claim, no new file
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2 }));
    }

    private static void VolumeDevice()
    {
        // ntifs.h / wdm.h: FILE_DEVICE_DISK 0x7, FILE_DEVICE_NETWORK_FILE_SYSTEM 0x14; FILE_REMOVABLE_MEDIA 0x1, FILE_FLOPPY_DISKETTE 0x4,
        // FILE_REMOTE_DEVICE 0x10, FILE_DEVICE_IS_MOUNTED 0x20, FILE_CHARACTERISTIC_TS_DEVICE 0x1000, FILE_CHARACTERISTIC_WEBDAV_DEVICE 0x2000.
        Equal(true, WindowsNative.IsFixedLocalVolume(0x7, 0x20));
        Equal(true, WindowsNative.IsFixedLocalVolume(0x7, 0x0));
        Equal(false, WindowsNative.IsFixedLocalVolume(0x14, 0x10)); // a mapped SMB drive: remote, network file system
        Equal(false, WindowsNative.IsFixedLocalVolume(0x7, 0x10));  // remote flag alone
        Equal(false, WindowsNative.IsFixedLocalVolume(0x14, 0x0));  // network device type alone
        Equal(false, WindowsNative.IsFixedLocalVolume(0x7, 0x21));  // removable media (a USB stick)
        Equal(false, WindowsNative.IsFixedLocalVolume(0x7, 0x24));  // floppy
        Equal(false, WindowsNative.IsFixedLocalVolume(0x7, 0x1000)); // terminal-services redirected
        Equal(false, WindowsNative.IsFixedLocalVolume(0x7, 0x2000)); // WebDAV
        string root = Path.GetPathRoot(NewRoot())!;
        using var handle = WindowsNative.OpenRoot(root);
        var (type, characteristics) = WindowsNative.VolumeDevice(handle);
        Equal(true, WindowsNative.IsFixedLocalVolume(type, characteristics)); // the local NTFS fixture volume is still admitted
        Console.WriteLine($"VOLUME-DEVICE local_ntfs type=0x{type:x} characteristics=0x{characteristics:x} admitted=true; real remote and removable volumes: NOT ASSESSED");
        Layout<WindowsNative.FileFsDevice>(8, ("DeviceType", 0), ("Characteristics", 4));
    }

    // Ruling 253 (W2-Q1 b): a target that does not carry the private owner/DACL is overwritten; the result is the private temp, so the
    // target's owner, extra ACEs, compression and named streams are not carried over.
    private static void ForeignDaclTarget()
    {
        string root = NewRoot(), path = Path.Combine(root, "project.cfdw"); File.WriteAllBytes(path, [7]); // inherited DACL
        using var store = new ProjectStore();
        var result = Save(store, path, new([1], Identity.Sha256([7]), Id()));
        Equal("OK", result.Code); Equal(true, result.DurabilityConfirmed);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1 })); Equal(1, Directory.GetFileSystemEntries(root).Length);
        AssertPrivateFile(path);
    }

    // Ruling 253: refused before the claim with a definite code (not DOC-SAVE-UNCERTAIN); the target, its attributes and the folder are untouched.
    private static void ReadOnlyTarget()
    {
        string root = NewRoot(), path = Path.Combine(root, "project.cfdw"); File.WriteAllBytes(path, [7]);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var stages = new List<StoreStage>();
            using var store = new ProjectStore(new StoreHooks { OnStage = stages.Add });
            var result = Save(store, path, new([1], Identity.Sha256([7]), Id()));
            Equal("DOC-UNSUPPORTED-PERSISTENCE", result.Code); Equal(false, result.PublicationKnown);
            Equal(false, stages.Contains(StoreStage.ClaimCreated)); Equal(1, Directory.GetFileSystemEntries(root).Length);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 7 })); Equal(true, (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0);
        }
        finally { File.SetAttributes(path, FileAttributes.Normal); }
    }

    // EFS needs a SKU and a profile that can encrypt; where File.Encrypt fails the check is not registered and a NOT ASSESSED line is printed, never a PASS.
    private static void EfsTarget()
    {
        string root = NewRoot(), path = Path.Combine(root, "project.cfdw"); File.WriteAllBytes(path, [7]);
        try { File.Encrypt(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        { Console.WriteLine($"NOT ASSESSED WindowsStore_EfsTarget_OverwriteRefused efs-unavailable {error.GetType().Name}"); return; }
        if ((File.GetAttributes(path) & FileAttributes.Encrypted) == 0)
        { Console.WriteLine("NOT ASSESSED WindowsStore_EfsTarget_OverwriteRefused efs-attribute-not-set"); return; }
        Check("WindowsStore_EfsTarget_OverwriteRefused", () =>
        {
            var stages = new List<StoreStage>();
            using var store = new ProjectStore(new StoreHooks { OnStage = stages.Add });
            var result = Save(store, path, new([1], Identity.Sha256([7]), Id()));
            Equal("DOC-UNSUPPORTED-PERSISTENCE", result.Code); Equal(false, result.PublicationKnown);
            Equal(false, stages.Contains(StoreStage.ClaimCreated)); Equal(1, Directory.GetFileSystemEntries(root).Length);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 7 })); Equal(true, (File.GetAttributes(path) & FileAttributes.Encrypted) != 0);
        });
    }

    private static void AncestorRenameRefused()
    {
        string root = NewRoot(), selected = Path.Combine(root, "selected"); Directory.CreateDirectory(selected);
        string path = Path.Combine(selected, "project"); File.WriteAllBytes(path, [7]); Exception? refused = null;
        var store = new ProjectStore(new StoreHooks { OnStage = stage =>
        {
            if (stage != StoreStage.BeforePublish) return;
            try { Directory.Move(selected, Path.Combine(root, "moved")); } catch (IOException error) { refused = error; }
        } });
        Equal("OK", Save(store, path, new([1], Identity.Sha256([7]), Id())).Code);
        Equal(true, refused is not null); Equal(true, Directory.Exists(selected)); Equal(false, Directory.Exists(Path.Combine(root, "moved")));
        Equal((byte)1, File.ReadAllBytes(path)[0]);
    }

    private static void OwnedTempProtected()
    {
        string root = NewRoot(), path = Path.Combine(root, "project"), operation = Id(), temp = Path.Combine(root, ProjectStore.TempName(operation));
        int refusals = 0;
        var store = new ProjectStore(new StoreHooks { OnStage = stage =>
        {
            if (stage != StoreStage.BeforePublish) return;
            try { File.Move(temp, temp + ".moved"); } catch (IOException) { refusals++; }
            try { File.WriteAllBytes(temp, [9]); } catch (IOException) { refusals++; }
            try { File.Delete(temp); } catch (IOException) { refusals++; }
        } });
        Equal("OK", Save(store, path, new([1], null, operation)).Code);
        Equal(3, refusals); Equal((byte)1, File.ReadAllBytes(path)[0]); Equal(1, Directory.GetFileSystemEntries(root).Length);
    }

    private static void EventsWithoutDirectoryFlush()
    {
        string root = NewRoot(), path = Path.Combine(root, "project"); using var store = new ProjectStore();
        Equal("OK", Save(store, path, new([1, 2], null, Id())).Code);
        var events = store.ReadLocalEvents();
        // Ruling 135: the Windows level is file flush plus atomic rename; no store.flush (directory) event is claimed.
        Equal("store.file-flush,store.publish,store.save", string.Join(',', events.Select(e => e.Action)));
        Equal(1, events.Select(e => e.TraceId).Distinct().Count());
        Equal(true, events.Last().PublicationKnown); Equal(true, events.Last().DurabilityConfirmed);
        Equal("DOC-CONFLICT", Save(store, path, new([9], null, Id())).Code);
        Equal("DOC-CONFLICT", store.ReadLocalEvents().Last().Outcome); Equal(false, store.ReadLocalEvents().Last().PublicationKnown);
        Equal(false, System.Text.Json.JsonSerializer.Serialize(store.ReadLocalEvents()).Contains(root, StringComparison.Ordinal));
    }

    // Ruling 190: kill the save in a child process at three points and list exactly what is left. The listing is the basis
    // of the COPY-447 wording ("the leftover files").
    private static void CrashInjection()
    {
        foreach (string stage in new[] { "Writing", "BeforePublish", "Published" })
        {
            string root = NewRoot(), path = Path.Combine(root, "project.cfdw"); using var store = new ProjectStore();
            var seeded = Save(store, path, new([1, 2, 3], null, Id())); Equal("OK", seeded.Code);
            var child = RunChild(stage, ("CFD_WIN_CRASH_STAGE", stage), ("CFD_WIN_CRASH_PATH", path), ("CFD_WIN_CRASH_HASH", seeded.PublishedSha256!));
            var entries = new DirectoryInfo(root).GetFileSystemInfos().OrderBy(e => e.Name, StringComparer.Ordinal).ToArray();
            string inventory = string.Join("; ", entries.Select(e => $"{(e.Name.StartsWith(".cfd-", StringComparison.Ordinal) && e.Name.EndsWith(".tmp", StringComparison.Ordinal) ? ".cfd-<operation>.tmp" : e.Name)} {((FileInfo)e).Length} bytes {e.Attributes}"));
            Console.WriteLine($"CRASH-INVENTORY stage={stage} child_exit={child.ExitCode} entries={entries.Length}: {inventory}");
            bool published = stage == "Published";
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(published ? new byte[] { 9, 9, 9, 9, 9, 9 } : new byte[] { 1, 2, 3 }));
            Equal(true, File.Exists(Path.Combine(root, ProjectStore.ClaimName())));
            Equal(published ? 2 : 3, entries.Length);
            if (!published) Equal(1, entries.Count(e => e.Name.StartsWith(".cfd-", StringComparison.Ordinal) && e.Name.EndsWith(".tmp", StringComparison.Ordinal)));
            // The unfinished save blocks the next overwrite (a crash-left claim is never reclaimed by age) and changes nothing.
            string current = Identity.Sha256(File.ReadAllBytes(path));
            Equal("DOC-CONFLICT", Save(store, path, new([5], current, Id())).Code);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(published ? new byte[] { 9, 9, 9, 9, 9, 9 } : new byte[] { 1, 2, 3 }));
            // The saved file stays openable, and removing the leftovers by hand unblocks the project.
            Equal(current, store.ReadAsync(path).GetAwaiter().GetResult().DiskSha256);
            foreach (var entry in entries.Where(e => e.Name.StartsWith(".cfd-", StringComparison.Ordinal))) entry.Delete();
            Equal("OK", Save(store, path, new([5], current, Id())).Code);
        }
    }

    // ---- Clear unfinished save (design windows-store-recovery section 7, node A) ----

    /// <summary>Runs this harness as a child with the named environment and returns when it was killed at <paramref name="stage"/> (the crash-child mechanism).</summary>
    private static (int ExitCode, string Output) RunChild(string stage, params (string Name, string Value)[] environment)
    {
        string host = Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".exe"); // the apphost, as HarnessRun starts it
        var start = new System.Diagnostics.ProcessStartInfo(host) { UseShellExecute = false, RedirectStandardOutput = true };
        start.ArgumentList.Add("--harness");
        start.Environment["CFD_TEST_ONLY"] = "WindowsStore_CrashChild_NeverMatches";
        foreach (var (name, value) in environment) start.Environment[name] = value;
        using var child = System.Diagnostics.Process.Start(start)!;
        string output = child.StandardOutput.ReadToEnd();
        if (!child.WaitForExit(120_000)) { child.Kill(); throw new TimeoutException("crash child"); }
        Equal(true, output.Contains("CRASH-AT " + stage, StringComparison.Ordinal));
        Equal(false, output.Contains("CRASH-CHILD-NOT-KILLED", StringComparison.Ordinal));
        Equal(true, child.ExitCode != 0);
        return (child.ExitCode, output);
    }

    private static ClearResult Clear(ProjectStore store, string path) => store.ClearUnfinishedSaveAsync(path).GetAwaiter().GetResult();
    private static string Listing(string root) => string.Join(",", Directory.GetFileSystemEntries(root).Select(entry => Path.GetFileName(entry)!).Order(StringComparer.Ordinal));
    private static string Seed(string root)
    {
        string path = Path.Combine(root, "project.cfdw"); using var store = new ProjectStore();
        Equal("OK", Save(store, path, new([1, 2, 3], null, Id())).Code);
        return path;
    }
    /// <summary>A leftover the way the store makes one: the private descriptor, created and closed.</summary>
    private static void Leftover(string root, string name, byte[]? bytes = null)
    {
        using var parent = WindowsNative.OpenFixtureDirectory(root);
        using var file = WindowsNative.CreatePrivate(parent, name);
        if (bytes is not null) RandomAccess.Write(file, bytes, 0);
    }
    private static string LeftoverTemp(string root) { string name = ProjectStore.TempName(Id()); Leftover(root, name, [1, 2]); return name; }

    /// <summary>A save paused at <paramref name="stage"/> on the store's own thread: its handles are open until <c>Release</c>.</summary>
    private static (Task<SaveResult> Saving, Action Release) PausedSave(string path, SaveRequest request, StoreStage stage)
    {
        var reached = new ManualResetEventSlim(); var release = new ManualResetEventSlim();
        var store = new ProjectStore(new StoreHooks { OnStage = visited => { if (visited == stage) { reached.Set(); release.Wait(60_000); } } });
        var saving = store.SaveAsync(path, request);
        Equal(true, reached.Wait(30_000));
        return (saving, release.Set);
    }

    private static void ClearRemovesCrashLeftovers()
    {
        foreach (string stage in new[] { "Writing", "BeforePublish", "Published" })
        {
            string root = NewRoot(), path = Seed(root), hash = Identity.Sha256(File.ReadAllBytes(path));
            RunChild(stage, ("CFD_WIN_CRASH_STAGE", stage), ("CFD_WIN_CRASH_PATH", path), ("CFD_WIN_CRASH_HASH", hash));
            int leftovers = stage == "Published" ? 1 : 2;
            Equal(1 + leftovers, Directory.GetFileSystemEntries(root).Length);
            Equal(true, UnfinishedSave.BlocksFolder(path));
            byte[] project = File.ReadAllBytes(path);
            string atTempsDisposed = "";
            using var store = new ProjectStore(new StoreHooks { OnStage = visited => { if (visited == StoreStage.ClearTempsDisposed) atTempsDisposed = Listing(root); } });
            var result = Clear(store, path);
            Equal(new ClearResult(ClearOutcome.Cleared, "OK", leftovers), result);
            Equal("project.cfdw", Listing(root));
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(project));
            Equal(".cfd-writer.claim,project.cfdw", atTempsDisposed); // the claim is deleted last: at that stage the temps are gone and the claim remains
            Equal(false, UnfinishedSave.BlocksFolder(path));
            Equal("OK", Save(store, path, new([5], Identity.Sha256(project), Id())).Code);
            Console.WriteLine($"CLEAR-CRASH-LEFTOVERS stage={stage} outcome={result.Outcome} removed={result.Removed} claim_last=true then_save=OK");
        }
    }

    private static void ClearForeignClaim()
    {
        string root = NewRoot(), path = Seed(root);
        File.WriteAllBytes(Path.Combine(root, ".cfd-writer.claim"), []); // inherits the folder's DACL: not this store's private descriptor
        LeftoverTemp(root);
        string before = Listing(root);
        using var store = new ProjectStore();
        Equal(new ClearResult(ClearOutcome.Failed, "DOC-CONFLICT", 0), Clear(store, path));
        Equal(before, Listing(root));
        Equal(true, UnfinishedSave.BlocksFolder(path));
    }

    private static void ClearLiveWriterClaim()
    {
        string root = NewRoot(), path = Seed(root), hash = Identity.Sha256(File.ReadAllBytes(path));
        var (saving, release) = PausedSave(path, new([9], hash, Id()), StoreStage.ClaimCreated);
        try
        {
            using var store = new ProjectStore();
            var result = Clear(store, path);
            Equal(new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0), result);
            Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim")));
        }
        finally { release(); }
        Equal("OK", saving.GetAwaiter().GetResult().Code);
        Equal("project.cfdw", Listing(root));
    }

    private static void ClearLiveCreateOnlyTemp()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim");
        var (saving, release) = PausedSave(Path.Combine(root, "new.cfdw"), new([9], null, Id()), StoreStage.TempCreated);
        try
        {
            using var store = new ProjectStore();
            var result = Clear(store, path);
            Equal(new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0), result);
            Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim"))); // nothing removed, not even the leftover claim
        }
        finally { release(); }
        Equal("OK", saving.GetAwaiter().GetResult().Code);
    }

    private static void ClearNewEntryBetweenChecks()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); LeftoverTemp(root);
        int before = Directory.GetFileSystemEntries(root).Length;
        using var store = new ProjectStore(new StoreHooks { OnStage = visited =>
        {
            if (visited == StoreStage.ClearHeld) Leftover(root, ProjectStore.TempName(Id()), [7]); // a writer starts after check 1
        } });
        var result = Clear(store, path);
        Equal(new ClearResult(ClearOutcome.InUse, "DOC-CONFLICT", 0), result);
        Equal(before + 1, Directory.GetFileSystemEntries(root).Length); // nothing removed
        Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim")));
    }

    private static void ClearCrashDuringClear()
    {
        foreach (string stage in new[] { "ClearHeld", "ClearTempsDisposed" })
        {
            string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); string temp = LeftoverTemp(root);
            RunChild(stage, ("CFD_WIN_CLEAR_STAGE", stage), ("CFD_WIN_CRASH_PATH", path));
            bool tempsGone = stage == "ClearTempsDisposed";
            Equal(tempsGone, !File.Exists(Path.Combine(root, temp)));
            Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim"))); // the claim is last: every partial state still blocks the folder
            Equal(true, UnfinishedSave.BlocksFolder(path));
            using var store = new ProjectStore();
            var retry = Clear(store, path);
            Equal(new ClearResult(ClearOutcome.Cleared, "OK", tempsGone ? 1 : 2), retry);
            Equal("project.cfdw", Listing(root));
            Equal("OK", Save(store, path, new([5], Identity.Sha256(File.ReadAllBytes(path)), Id())).Code);
            Console.WriteLine($"CLEAR-CRASH-DURING stage={stage} killed=true folder_blocked=true retry={retry.Outcome} removed={retry.Removed}");
        }
    }

    private static void ClearHardLinkOrReparse()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); string temp = LeftoverTemp(root);
        RunProcess("cmd.exe", "/c", "mklink", "/H", Path.Combine(root, ProjectStore.TempName(Id())), Path.Combine(root, temp));
        string before = Listing(root);
        using var store = new ProjectStore();
        Equal(new ClearResult(ClearOutcome.Failed, "DOC-CONFLICT", 0), Clear(store, path)); // two links: P2 fails
        Equal(before, Listing(root));
        string other = NewRoot(), otherPath = Seed(other); Leftover(other, ".cfd-writer.claim");
        Equal(true, TestLinks.TryDirectoryLink(Path.Combine(other, ProjectStore.TempName(Id())), root)); // a junction named like a temp
        string beforeOther = Listing(other);
        var reparse = Clear(store, otherPath);
        Equal(ClearOutcome.Failed, reparse.Outcome); Equal(0, reparse.Removed);
        Equal(beforeOther, Listing(other));
        Equal(before, Listing(root)); // the junction's target was not touched
        Console.WriteLine($"CLEAR-HARDLINK-REPARSE hardlink=Failed:DOC-CONFLICT junction={reparse.Outcome}:{reparse.Code} removed=0");
    }

    private static void ClearUnderOneDrive()
    {
        string root = NewRoot(), oneDrive = Path.Combine(root, "OneDrive"); Directory.CreateDirectory(oneDrive);
        string path = Seed(oneDrive); Leftover(oneDrive, ".cfd-writer.claim"); LeftoverTemp(oneDrive);
        string before = Listing(oneDrive);
        WithEnvironment("OneDrive", oneDrive, () =>
        {
            using var store = new ProjectStore();
            Equal(new ClearResult(ClearOutcome.Failed, "DOC-UNSUPPORTED-PERSISTENCE", 0), Clear(store, path));
        });
        Equal(before, Listing(oneDrive));
    }

    private static void ClearTelemetry()
    {
        string root = NewRoot(), path = Seed(root), claim = Path.Combine(root, ".cfd-writer.claim");
        File.WriteAllBytes(claim, []); // foreign
        using var store = new ProjectStore();
        Clear(store, path);
        File.Delete(claim); Leftover(root, ".cfd-writer.claim"); LeftoverTemp(root);
        Clear(store, path);
        var events = store.ReadLocalEvents().Where(entry => entry.Action == "store.clear").ToArray();
        Equal(2, events.Length);
        Equal("DOC-CONFLICT", events[0].Outcome); Equal(1, events[0].InputBytes); Equal(0, events[0].OutputBytes); // code, candidates, removed
        Equal("OK", events[1].Outcome); Equal(2, events[1].InputBytes); Equal(2, events[1].OutputBytes);
        Equal(true, events.All(entry => entry.DurationMilliseconds is >= 0));
        Equal(false, System.Text.Json.JsonSerializer.Serialize(events).Contains(root, StringComparison.Ordinal)); // no path
    }

    private static void ClearCaseVariantClaim()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".CFD-Writer.Claim"); // private descriptor, case variant
        using var store = new ProjectStore();
        Equal(new ClearResult(ClearOutcome.Cleared, "OK", 1), Clear(store, path));
        Equal("project.cfdw", Listing(root));
        string other = NewRoot(), otherPath = Seed(other);
        File.WriteAllBytes(Path.Combine(other, ".CFD-Writer.Claim"), []); // inherited DACL, case variant: selected, held, refused by P3, kept
        Equal(new ClearResult(ClearOutcome.Failed, "DOC-CONFLICT", 0), Clear(store, otherPath));
        Equal(true, UnfinishedSave.BlocksFolder(otherPath)); // never "Cleared" while a claim name still answers
    }

    // C5: a read-only candidate opens for DELETE (the open succeeds, probed) and the delete itself fails with Win32 5: Failed DOC-IO, claim kept.
    private static void ClearReadOnlyCandidateFailsAtDelete()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); string temp = LeftoverTemp(root), tempPath = Path.Combine(root, temp);
        using var store = new ProjectStore();
        File.SetAttributes(tempPath, FileAttributes.ReadOnly);
        var denied = Clear(store, path);
        File.SetAttributes(tempPath, FileAttributes.Normal);
        Equal(new ClearResult(ClearOutcome.Failed, "DOC-IO", 0), denied);
        Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim")) && File.Exists(tempPath)); // nothing removed
        Console.WriteLine($"CLEAR-C5-DELETE readonly={denied.Outcome}:{denied.Code} removed={denied.Removed}");
    }

    // C2: the open of a delete-pending candidate fails before the share check; any other open error is Failed, not InUse, and drops nothing.
    private static void ClearNonSharingOpenError()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); string temp = LeftoverTemp(root), tempPath = Path.Combine(root, temp);
        using var store = new ProjectStore();
        // delete pending: the legacy disposition on a handle that stays open (FileDispositionInfo = 4)
        using var holder = CreateFileW(tempPath, 0x00010000, 7, 0, 3, 0x80, 0); // DELETE, share all, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL
        Equal(false, holder.IsInvalid);
        uint one = 1; Equal(true, SetFileInformationByHandle(holder, 4, ref one, 1));
        var pending = Clear(store, path);
        Equal(ClearOutcome.Failed, pending.Outcome); Equal(0, pending.Removed);
        Equal(true, File.Exists(Path.Combine(root, ".cfd-writer.claim")));
        holder.Dispose();
        Console.WriteLine($"CLEAR-NONSHARING-OPEN delete_pending={pending.Outcome}:{pending.Code} removed=0");
    }

    private static void ClearReaderOnTemp()
    {
        string root = NewRoot(), path = Seed(root); Leftover(root, ".cfd-writer.claim"); string temp = LeftoverTemp(root);
        using var reader = new FileStream(Path.Combine(root, temp), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); // an indexer or scanner
        using var store = new ProjectStore();
        Equal(new ClearResult(ClearOutcome.Cleared, "OK", 2), Clear(store, path)); // a reader is not a writer: no false COPY-449
    }

    private static void ClearListsEntries()
    {
        string root = NewRoot();
        for (int i = 0; i < 2000; i++) File.WriteAllBytes(Path.Combine(root, "entry-" + i.ToString("D5") + "-with-a-long-enough-name-to-cross-the-buffer.txt"), []);
        using var directory = WindowsNative.OpenRoot(root, WindowsNative.DirectoryAccess | WindowsNative.ListDirectoryAccess);
        var names = WindowsNative.ListNames(directory);
        Equal(2000, names.Count); Equal(2000, names.Distinct().Count());
        Equal(false, names.Contains(".") || names.Contains(".."));
    }

    private static void ClearCrashChild(string stage)
    {
        string path = Environment.GetEnvironmentVariable("CFD_WIN_CRASH_PATH")!;
        var hooks = new StoreHooks { OnStage = visited => { if (visited.ToString() == stage) Die(stage); } };
        Clear(new ProjectStore(hooks), path);
        Console.WriteLine("CRASH-CHILD-NOT-KILLED");
    }

    private static void CrashChild(string stage)
    {
        string path = Environment.GetEnvironmentVariable("CFD_WIN_CRASH_PATH")!, expected = Environment.GetEnvironmentVariable("CFD_WIN_CRASH_HASH")!;
        int writes = 0;
        var hooks = new StoreHooks { WriteFragment = 2, OnStage = visited =>
        {
            if (stage == "Writing" ? visited == StoreStage.Writing && ++writes == 2 : visited.ToString() == stage) Die(stage);
        } };
        new ProjectStore(hooks).SaveAsync(path, new SaveRequest([9, 9, 9, 9, 9, 9], expected, Id())).GetAwaiter().GetResult();
        Console.WriteLine("CRASH-CHILD-NOT-KILLED");
    }

    private static void Die(string stage)
    {
        Console.WriteLine("CRASH-AT " + stage); Console.Out.Flush();
        System.Diagnostics.Process.GetCurrentProcess().Kill();
        Thread.Sleep(Timeout.Infinite);
    }

    private static void Probe(string label, int expectedWin32,
        Func<SafeFileHandle, SafeFileHandle, string, WindowsNative.RenameCompletion> rename) => Fixture((root, parent) =>
    {
        using var source = WindowsNative.CreatePrivate(parent, "source.tmp");
        RandomAccess.Write(source, [4, 5], 0); WindowsNative.Flush(source);
        var identity = WindowsNative.Identity(source);
        int win32 = 0;
        try
        {
            var completion = rename(source, parent, root);
            Equal(new WindowsNative.RenameCompletion(0, 0, 0), completion);
            Console.WriteLine($"{label} NTSTATUS=not recorded IO_STATUS=not recorded Win32=0");
        }
        catch (WindowsNative.NativeFailure error)
        {
            win32 = error.Win32Error;
            Equal<int?>(null, error.NtStatus);
            Equal<int?>(null, error.IoStatus);
            Console.WriteLine(label + " " + error.Message);
        }
        Equal(expectedWin32, win32);
        Equal(identity, WindowsNative.Identity(source));
        Equal(true, Read(source).AsSpan().SequenceEqual(new byte[] { 4, 5 }));
        Equal(win32 != 0, File.Exists(Path.Combine(root, "source.tmp")));
        Equal(win32 == 0, File.Exists(Path.Combine(root, "destination.cfdw")));
        source.Dispose();
        using var observed = WindowsNative.OpenReader(parent, win32 == 0 ? "destination.cfdw" : "source.tmp");
        Equal(identity, WindowsNative.Identity(observed));
        Equal(true, Read(observed).AsSpan().SequenceEqual(new byte[] { 4, 5 }));
        Console.WriteLine($"{label} identity={identity} bytes=0405 source_exists={win32 != 0} destination_exists={win32 == 0}");
    });

    /// <summary>
    /// One rendering for two SDDL strings that name the same owner and DACL. Windows renders a well-known account as its
    /// two-letter alias (the built-in Administrator, RID 500, as LA; the GitHub Windows runner runs as that account) while
    /// PrivateDescriptor writes the literal SID, so the strings differ although the descriptors are equal.
    /// </summary>
    private static string CanonicalSddl(string sddl) =>
        new RawSecurityDescriptor(sddl).GetSddlForm(AccessControlSections.Owner | AccessControlSections.Access);

    /// <summary>Another instance of the app publishes: a private temp renamed over the target by handle (never an in-place write).</summary>
    internal static void ExternalReplace(string path, byte[] bytes)
    {
        using var parent = WindowsNative.OpenFixtureDirectory(Path.GetDirectoryName(path)!);
        string temp = ".external-" + Guid.NewGuid().ToString("N") + ".tmp";
        using var file = WindowsNative.CreatePrivate(parent, temp);
        RandomAccess.Write(file, bytes, 0); WindowsNative.Flush(file);
        WindowsNative.Rename(file, parent, Path.GetFileName(path), replace: true);
    }

    /// <summary>The DACL of a directory in numbers, no account names or SIDs: whether it is protected, how many ACEs, how many inherited.</summary>
    internal static string DirectoryAclSummary(string path)
    {
        using var directory = WindowsNative.OpenFixtureDirectory(path);
        var descriptor = new RawSecurityDescriptor(WindowsNative.ReadOwnerAndDacl(directory));
        var acl = descriptor.DiscretionaryAcl;
        bool protectedAcl = (descriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0;
        int inherited = acl?.OfType<CommonAce>().Count(ace => (ace.AceFlags & AceFlags.Inherited) != 0) ?? 0;
        return $"protected={protectedAcl} aces={acl?.Count ?? 0} inherited={inherited} private_policy={(protectedAcl && inherited == 0 && acl?.Count == 1)}";
    }

    /// <summary>The file carries the store's private owner/DACL (Windows has no mode bits).</summary>
    internal static void AssertPrivateFile(string path)
    {
        using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Equal(CanonicalSddl(WindowsNative.PrivateDescriptor()), CanonicalSddl(WindowsNative.ReadOwnerAndDacl(file)));
    }

    private static byte[] Read(SafeFileHandle file)
    {
        byte[] bytes = new byte[checked((int)RandomAccess.GetLength(file))];
        Equal(bytes.Length, RandomAccess.Read(file, bytes, 0));
        return bytes;
    }

    private static void Fixture(Action<string, SafeFileHandle> check)
    {
        string root = TestTemp.Combine("cfd-win-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { using var parent = WindowsNative.OpenFixtureDirectory(root); check(root, parent); }
        finally { Directory.Delete(root, true); }
    }

    private static void Layout<T>(int size, params (string Field, int Offset)[] fields) where T : struct
    {
        Equal(size, Marshal.SizeOf<T>());
        foreach (var (field, offset) in fields) Equal(offset, checked((int)Marshal.OffsetOf<T>(field)));
        Console.WriteLine($"LAYOUT {typeof(T).Name} size={size} " + string.Join(' ', fields.Select(f => $"{f.Field}={f.Offset}")));
    }
}
