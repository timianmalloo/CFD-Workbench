using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>One of an ended child's standard pipes as the parent holds it: the read end and the pipe's identity.</summary>
/// <remarks>The identity is the named-pipe file system's index number, the same on both ends of one pipe and different between
/// pipes (observed 2026-10-18, DRAIN-ON-ARM64-LAPTOP). Null when the host cannot give one.</remarks>
internal readonly record struct ChildPipe(string Stream, nint Handle, ulong? Index);

/// <summary>
/// Names the processes that still hold an ended child's standard pipes (DRAIN-ON-ARM64-LAPTOP). The pipes are anonymous (no
/// object name) and an unelevated process cannot see kernel object addresses, so a holder is matched by the pipe's index number:
/// every pipe handle of every process this user can open is duplicated and queried, read-only, and one whose index is the
/// child's stdout or stderr is a holder. Lineage (recorded parent chain) says what the holder is to the child.
/// </summary>
internal static partial class PipeHolders
{
    /// <summary>The parent's end of a redirected standard stream, identified before any reader runs on it (a query on a
    /// synchronous pipe handle with a read in flight can block behind that read).</summary>
    public static ChildPipe Of(string stream, StreamReader reader)
    {
        nint handle = reader.BaseStream is FileStream file ? file.SafeFileHandle.DangerousGetHandle() : 0;
        return new ChildPipe(stream, handle, handle == 0 || !OperatingSystem.IsWindows() ? null : PipeIndex(handle));
    }

    /// <summary>The holders of the ended child's pipes, or what was scanned when none was found.</summary>
    public static string Describe(int childPid, DateTime childStartedUtc, ChildPipe[] pipes)
    {
        if (!OperatingSystem.IsWindows()) return $"holder scan: not available on {RuntimeInformation.OSDescription}";
        if (pipes.Any(pipe => pipe.Index is null)) return "holder scan: the child's pipe identity was not available";
        return DescribeWindows(childPid, childStartedUtc, pipes);
    }

    [SupportedOSPlatform("windows")]
    private static string DescribeWindows(int childPid, DateTime childStartedUtc, ChildPipe[] pipes)
    {
        int self = Environment.ProcessId;
        // The holder may be short-lived (a grandchild about to exit), so: snapshot the processes first, scan the child's lineage and
        // this harness's children before the rest, and run every index query concurrently under one deadline rather than one by one
        // (a query stuck behind its handle's own in-flight I/O would otherwise cost its full timeout each; 29 such were observed).
        var known = Processes(childPid, childStartedUtc, out var lineage);
        var table = HandleTable();
        ushort fileType = table.First(entry => entry.Pid == self && entry.Handle == pipes[0].Handle).Type;
        int Priority(int pid) => lineage.Contains(pid) ? 0 : known.Any(k => k.Pid == pid && k.Parent == self) ? 1 : known.Any(k => k.Pid == pid) ? 2 : 3;
        var queries = new List<(int Pid, nint Copy, Thread Thread, ulong?[] Index)>();
        int processes = 0;
        foreach (var group in table.Where(entry => entry.Pid != self && entry.Type == fileType).GroupBy(entry => entry.Pid).OrderBy(group => Priority(group.Key)))
        {
            nint process = OpenProcess(ProcessDupHandle | ProcessQueryLimitedInformation, false, (uint)group.Key);
            if (process == 0) continue;
            processes++;
            try
            {
                foreach (var entry in group)
                {
                    if (!DuplicateHandle(process, entry.Handle, GetCurrentProcess(), out nint copy, 0, false, DuplicateSameAccess)) continue;
                    if (GetFileType(copy) != FileTypePipe) { CloseHandle(copy); continue; }
                    var index = new ulong?[1];
                    var query = new Thread(() => { try { index[0] = PipeIndex(copy); } catch (Exception) { } }) { IsBackground = true };
                    query.Start();
                    queries.Add((group.Key, copy, query, index));
                }
            }
            finally { CloseHandle(process); }
        }
        long deadline = Stopwatch.GetTimestamp() + (long)(Stopwatch.Frequency * 1.0);
        var held = new Dictionary<int, List<string>>();   // holder pid -> streams
        int hung = 0;
        foreach (var (pid, copy, query, index) in queries)
        {
            var remaining = TimeSpan.FromSeconds(Math.Max(0, (deadline - Stopwatch.GetTimestamp()) / (double)Stopwatch.Frequency));
            if (!query.Join(remaining)) { hung++; continue; }   // a query stuck behind that handle's own I/O keeps its copy; one leaked handle beats a close under it
            foreach (var pipe in pipes.Where(pipe => index[0] is not null && pipe.Index == index[0]))
                (held.TryGetValue(pid, out var streams) ? streams : held[pid] = []).Add(pipe.Stream);
            CloseHandle(copy);
        }
        int checkedPipes = queries.Count;
        if (held.Count > 0)
            return "holders: " + string.Join("; ", held.OrderBy(pair => pair.Key).Select(pair =>
            {
                string streams = string.Join(", ", new[] { "stdout", "stderr" }.Where(pair.Value.Contains));
                return $"{Identify(pair.Key, known, lineage, childPid)} holds {streams} (verified by pipe id)";
            }));
        string others = string.Join(", ", known.Where(k => k.Pid != childPid).Select(k => $"{k.Name}({k.Pid})"));
        return $"holders: none ({processes} processes opened, {checkedPipes} pipe handles checked, {hung} queries hung); started during the child's life: {(others.Length == 0 ? "none" : others)}";
    }

    private static string Identify(int pid, List<(int Pid, int Parent, string Name, DateTime StartedUtc)> known, HashSet<int> lineage, int childPid)
    {
        var entry = known.FirstOrDefault(k => k.Pid == pid);
        if (entry.Name is null)
        {
            string name;
            try { using var process = Process.GetProcessById(pid); name = process.ProcessName; }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException) { name = "(gone)"; }
            return $"pid {pid} {name} (started before the child, outside its tree)";
        }
        string relation = lineage.Contains(pid) ? "a descendant of the child" : entry.Parent == Environment.ProcessId ? "a child of this harness" : "outside the tree";
        return $"pid {pid} {entry.Name} (parent {entry.Parent}, started {entry.StartedUtc:HH:mm:ss.fff}Z, {relation})";
    }

    /// <summary>Every process this user can read that started after the child did, with its recorded parent; <paramref name="lineage"/>
    /// is the subset whose parent chain reaches the child.</summary>
    [SupportedOSPlatform("windows")]
    private static List<(int Pid, int Parent, string Name, DateTime StartedUtc)> Processes(int childPid, DateTime childStartedUtc, out HashSet<int> lineage)
    {
        var known = new List<(int Pid, int Parent, string Name, DateTime StartedUtc)>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    DateTime started = process.StartTime.ToUniversalTime();
                    if (started < childStartedUtc) continue;   // older than the child: not its descendant, whatever a reused parent id says
                    int parent = ParentPid(process.Id);
                    if (parent >= 0) known.Add((process.Id, parent, process.ProcessName, started));
                }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }   // gone, or not this user's to read
            }
        }
        lineage = [];
        var frontier = new Queue<int>();
        frontier.Enqueue(childPid);
        while (frontier.TryDequeue(out int parent))
            foreach (var entry in known.Where(k => k.Parent == parent))
                if (lineage.Add(entry.Pid)) frontier.Enqueue(entry.Pid);
        return known;
    }

    /// <summary>The system handle table: (owning pid, handle value, object type index) for every handle on the host.</summary>
    [SupportedOSPlatform("windows")]
    private static List<(int Pid, nint Handle, ushort Type)> HandleTable()
    {
        byte[] buffer = new byte[1 << 20];
        while (true)
        {
            int status = NtQuerySystemInformation(SystemExtendedHandleInformation, ref MemoryMarshal.GetArrayDataReference(buffer), (uint)buffer.Length, out _);
            if (status == 0) break;
            if (unchecked((uint)status) != StatusInfoLengthMismatch) throw new InvalidOperationException($"NtQuerySystemInformation: 0x{status:X8}");
            buffer = new byte[buffer.Length * 2];
        }
        var span = buffer.AsSpan();
        long count = BinaryPrimitives.ReadInt64LittleEndian(span);
        var table = new List<(int Pid, nint Handle, ushort Type)>((int)count);
        for (int i = 0; i < count; i++)
        {
            var entry = span.Slice(16 + 40 * i, 40);   // SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX on a 64-bit host
            table.Add(((int)BinaryPrimitives.ReadUInt64LittleEndian(entry[8..]), (nint)BinaryPrimitives.ReadUInt64LittleEndian(entry[16..]),
                BinaryPrimitives.ReadUInt16LittleEndian(entry[30..])));
        }
        return table;
    }

    [SupportedOSPlatform("windows")]
    private static ulong? PipeIndex(nint handle)
    {
        var block = new byte[16];   // IO_STATUS_BLOCK
        var index = new byte[8];    // FILE_INTERNAL_INFORMATION.IndexNumber
        int status = NtQueryInformationFile(handle, ref MemoryMarshal.GetArrayDataReference(block), ref MemoryMarshal.GetArrayDataReference(index), 8, FileInternalInformation);
        return status == 0 ? BinaryPrimitives.ReadUInt64LittleEndian(index) : null;
    }

    /// <summary>The recorded parent process id, or -1 when the process cannot be opened or queried.</summary>
    [SupportedOSPlatform("windows")]
    private static int ParentPid(int pid)
    {
        nint process = OpenProcess(ProcessQueryLimitedInformation, false, (uint)pid);
        if (process == 0) return -1;
        try
        {
            var basic = new byte[48];   // PROCESS_BASIC_INFORMATION: InheritedFromUniqueProcessId at offset 40 on a 64-bit host
            if (NtQueryInformationProcess(process, ProcessBasicInformation, ref MemoryMarshal.GetArrayDataReference(basic), (uint)basic.Length, out _) != 0) return -1;
            return (int)BinaryPrimitives.ReadUInt64LittleEndian(basic.AsSpan(40));
        }
        finally { CloseHandle(process); }
    }

    private const uint ProcessDupHandle = 0x0040, ProcessQueryLimitedInformation = 0x1000, DuplicateSameAccess = 0x0002, FileTypePipe = 0x0003;
    private const uint StatusInfoLengthMismatch = 0xC0000004;
    private const int SystemExtendedHandleInformation = 64, FileInternalInformation = 6, ProcessBasicInformation = 0;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DuplicateHandle(nint sourceProcess, nint sourceHandle, nint targetProcess, out nint targetHandle, uint access,
        [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetFileType(nint handle);

    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtQuerySystemInformation(int informationClass, ref byte information, uint length, out uint returned);

    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtQueryInformationFile(nint handle, ref byte statusBlock, ref byte information, uint length, int informationClass);

    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtQueryInformationProcess(nint process, int informationClass, ref byte information, uint length, out uint returned);
}
