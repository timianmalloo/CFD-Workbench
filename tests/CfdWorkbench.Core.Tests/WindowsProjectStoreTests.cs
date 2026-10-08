using CfdWorkbench.Persistence;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

[SupportedOSPlatform("windows")]
internal static class WindowsProjectStoreTests
{
    internal static void Run()
    {
        Check("WindowsStore_Unqualified_ProductionAdmissionRemainsClosed", () =>
        {
            string root = Path.Combine(Path.GetTempPath(), "cfd-win-native-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                using var store = new ProjectStore();
                var result = store.SaveAsync(Path.Combine(root, "project.cfdw"),
                    new SaveRequest([1, 2, 3], null, Guid.NewGuid().ToString("D"))).GetAwaiter().GetResult();
                Equal("DOC-UNSUPPORTED-PERSISTENCE", result.Code);
                Equal(false, result.PublicationKnown);
                Equal(false, result.DurabilityConfirmed);
                Equal(0, Directory.GetFileSystemEntries(root).Length);
            }
            finally { Directory.Delete(root, true); }
        });
        Check("WindowsNative_Environment_RealNtfsX64", () =>
        {
            Equal(Architecture.X64, RuntimeInformation.ProcessArchitecture);
            Equal("NTFS", new DriveInfo(Path.GetPathRoot(Path.GetTempPath())!).DriveFormat);
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
        });
        Check("WindowsNative_RelativeCreate_PrivateAclBeforeAnyBytes", () => Fixture((root, parent) =>
        {
            using var file = WindowsNative.CreatePrivate(parent, "private.tmp");
            Equal(0L, RandomAccess.GetLength(file));
            Equal(WindowsNative.PrivateDescriptor(), WindowsNative.ReadOwnerAndDacl(file));
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
            Equal(0, refusal.IoStatus);
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
        Check("WindowsNative_Replace_HeldReaderKeepsOldImage", () => Fixture((root, parent) =>
        {
            using (var old = WindowsNative.CreatePrivate(parent, "project.cfdw"))
            { RandomAccess.Write(old, [1, 1], 0); WindowsNative.Flush(old); }
            using var reader = WindowsNative.OpenReader(parent, "project.cfdw", WindowsNative.ShareRead);
            var oldIdentity = WindowsNative.Identity(reader);
            using var temp = WindowsNative.CreatePrivate(parent, "new.tmp");
            RandomAccess.Write(temp, [2, 2, 2], 0); WindowsNative.Flush(temp);
            var newIdentity = WindowsNative.Identity(temp);

            var completion = WindowsNative.Rename(temp, parent, "project.cfdw", replace: true);
            Console.WriteLine($"NT-RENAME NTSTATUS=0x{completion.NtStatus:x8} IO_STATUS=0x{completion.IoStatus:x8} Win32={completion.Win32Error}");

            // The read handle deliberately omits DELETE sharing: qualify the stronger real-reader case.
            Equal(true, Read(reader).AsSpan().SequenceEqual(new byte[] { 1, 1 }));
            using var fresh = WindowsNative.OpenReader(parent, "project.cfdw",
                WindowsNative.ShareRead | WindowsNative.ShareWrite | WindowsNative.ShareDelete);
            Equal(true, Read(fresh).AsSpan().SequenceEqual(new byte[] { 2, 2, 2 }));
            Equal(newIdentity, WindowsNative.Identity(fresh));
            Equal(false, oldIdentity == newIdentity);
            Equal(false, File.Exists(Path.Combine(root, "new.tmp")));
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

    private static byte[] Read(SafeFileHandle file)
    {
        byte[] bytes = new byte[checked((int)RandomAccess.GetLength(file))];
        Equal(bytes.Length, RandomAccess.Read(file, bytes, 0));
        return bytes;
    }

    private static void Fixture(Action<string, SafeFileHandle> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "cfd-win-native-" + Guid.NewGuid().ToString("N"));
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
