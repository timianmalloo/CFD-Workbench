using System.Runtime.InteropServices;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class ProjectStoreTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsMacOS()) { Console.WriteLine("NOT ASSESSED native persistence primitives on this platform"); return; }
        Check("NativePrimitive_MacHandleRelativeNoReplaceAndFlush", Probe);
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
}
