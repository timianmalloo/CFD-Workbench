using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace CfdWorkbench.Persistence;

// Qualification boundary only: ProjectStore does not dispatch here yet.
// Ruling 136: SDK declarations, LibraryImport and SafeHandle; no private native DLL.
[SupportedOSPlatform("windows")]
internal static unsafe partial class WindowsNative
{
    // winnt.h file rights, share modes and attributes. Values checked against SDK 10.0.26100.0.
    // https://learn.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
    internal const uint ReadAccess = 0x00120089; // FILE_GENERIC_READ
    internal const uint WriteAccess = 0x00120116; // FILE_GENERIC_WRITE
    internal const uint DeleteAccess = 0x00010000; // DELETE
    internal const uint DirectoryAccess = 0x001200a0; // SYNCHRONIZE | READ_CONTROL | FILE_TRAVERSE | FILE_READ_ATTRIBUTES
    internal const uint ShareRead = 1, ShareWrite = 2, ShareDelete = 4;
    private const uint NormalAttributes = 0x80; // FILE_ATTRIBUTE_NORMAL
    private const uint OpenExisting = 3; // OPEN_EXISTING, fileapi.h CreateFileW
    private const uint BackupSemantics = 0x02000000, OpenReparsePoint = 0x00200000; // winbase.h

    // winternl.h/ntdef.h; NtCreateFile has NT dispositions, not CreateFileW dispositions.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntcreatefile
    // https://learn.microsoft.com/en-us/windows/win32/api/ntdef/ns-ntdef-_object_attributes
    private const uint NtOpen = 1, NtCreate = 2;
    private const uint SynchronousNonalert = 0x20, NonDirectory = 0x40;
    private const uint CaseInsensitive = 0x40, DontReparse = 0x1000;

    // minwinbase.h FILE_INFO_BY_HANDLE_CLASS ordinal values (SDK declaration and Learn).
    // https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ne-minwinbase-file_info_by_handle_class
    private const int FileIdInfo = 18, FileDispositionInfoEx = 21, FileRenameInfoEx = 22;
    // wdm.h FILE_INFORMATION_CLASS; different enumeration from Win32 FILE_INFO_BY_HANDLE_CLASS.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ne-wdm-_file_information_class
    private const int FileRenameInformationEx = 65;
    // ntifs.h: FILE_RENAME_REPLACE_IF_EXISTS=1, FILE_RENAME_POSIX_SEMANTICS=2.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_rename_information
    private const uint RenameReplace = 1, RenamePosix = 2;
    // ntddk.h: FILE_DISPOSITION_DELETE=1, FILE_DISPOSITION_POSIX_SEMANTICS=2.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_file_disposition_information_ex
    private const uint DispositionDelete = 1, DispositionPosix = 2;

    // accctrl.h SE_FILE_OBJECT=1; winnt.h OWNER_SECURITY_INFORMATION=1/DACL_SECURITY_INFORMATION=4;
    // shared/sddl.h SDDL_REVISION_1=1.
    // https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo
    // https://learn.microsoft.com/en-us/windows/win32/api/sddl/nf-sddl-convertstringsecuritydescriptortosecuritydescriptorw
    private const int FileObject = 1;
    private const uint OwnerAndDacl = 5, SddlRevision = 1;

    // Each ABI is independently checked by WindowsNative_Layouts_SdkX64.
    // https://learn.microsoft.com/en-us/windows/win32/api/ntdef/ns-ntdef-_unicode_string
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnicodeString { internal ushort Length, MaximumLength; internal nint Buffer; }
    // https://learn.microsoft.com/en-us/windows/win32/api/ntdef/ns-ntdef-_object_attributes
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectAttributes
    {
        internal uint Length;
        internal nint RootDirectory, ObjectName;
        internal uint Attributes;
        internal nint SecurityDescriptor, SecurityQualityOfService;
    }
    // https://learn.microsoft.com/en-us/windows/win32/api/winternl/ns-winternl-io_status_block
    // The status/pointer union occupies a pointer-sized field; its NTSTATUS is low 32 bits.
    [StructLayout(LayoutKind.Sequential)]
    internal struct IoStatusBlock { internal nint Status; internal nuint Information; }
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info
    // https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-file_id_128
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileId128 { internal ulong Low, High; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileId { internal ulong VolumeSerialNumber; internal FileId128 Id; }
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info
    // Flags is the Ex union member; first WCHAR follows the DWORD byte count, without pointer padding.
    [StructLayout(LayoutKind.Sequential)]
    internal struct RenameHeader
    {
        internal uint Flags;
        internal nint RootDirectory;
        internal uint FileNameLength;
        internal ushort FirstCharacter;
    }
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info_ex
    [StructLayout(LayoutKind.Sequential)]
    internal struct Disposition { internal uint Flags; }

    internal readonly record struct FileIdentity(ulong Volume, ulong Low, ulong High);

    // This open is for a trusted qualification fixture. Full root/ancestor admission is a later slice.
    internal static SafeFileHandle OpenFixtureDirectory(string absolutePath)
    {
        var handle = CreateFileW(absolutePath, DirectoryAccess, ShareRead | ShareWrite, 0,
            OpenExisting, BackupSemantics | OpenReparsePoint, 0);
        if (!handle.IsInvalid) return handle;
        int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new NativeFailure(null, error);
    }

    internal static SafeFileHandle CreatePrivate(SafeFileHandle parent, string child)
    {
        Check(ConvertStringSecurityDescriptorToSecurityDescriptorW(PrivateDescriptor(), SddlRevision,
            out LocalMemory descriptor, out _));
        using (descriptor)
        {
            bool retained = false;
            try
            {
                descriptor.DangerousAddRef(ref retained);
                return OpenRelative(parent, child, ReadAccess | WriteAccess | DeleteAccess, ShareRead,
                    NtCreate, descriptor.DangerousGetHandle());
            }
            finally { if (retained) descriptor.DangerousRelease(); }
        }
    }

    internal static SafeFileHandle OpenReader(SafeFileHandle parent, string child, uint share = ShareRead | ShareDelete) =>
        OpenRelative(parent, child, ReadAccess, share, NtOpen, 0);

    private static SafeFileHandle OpenRelative(SafeFileHandle parent, string child, uint access, uint share,
        uint disposition, nint securityDescriptor)
    {
        RequireChild(child);
        bool retained = false;
        try
        {
            parent.DangerousAddRef(ref retained);
            fixed (char* name = child)
            {
                var unicode = new UnicodeString { Length = checked((ushort)(child.Length * sizeof(char))),
                    MaximumLength = checked((ushort)((child.Length + 1) * sizeof(char))), Buffer = (nint)name };
                var attributes = new ObjectAttributes { Length = (uint)sizeof(ObjectAttributes),
                    RootDirectory = parent.DangerousGetHandle(), ObjectName = (nint)(&unicode),
                    Attributes = CaseInsensitive | DontReparse, SecurityDescriptor = securityDescriptor };
                int status = NtCreateFile(out nint raw, access, &attributes, out IoStatusBlock io, 0,
                    NormalAttributes, share, disposition, SynchronousNonalert | NonDirectory | OpenReparsePoint, 0, 0);
                if (status == 0 && (int)io.Status == 0) return new SafeFileHandle(raw, true);
                if (raw != 0 && raw != -1) new SafeFileHandle(raw, true).Dispose();
                int completion = status == 0 ? (int)io.Status : status;
                throw new NativeFailure(completion, checked((int)RtlNtStatusToDosError(completion)));
            }
        }
        finally { if (retained) parent.DangerousRelease(); }
    }

    internal static void RequireChild(string child)
    {
        // Bound to the NTFS simple-component qualification domain; full user-path rules are not admitted.
        if (child.Length is 0 or > 255 || child is "." or ".." || child.IndexOfAny(['\\', '/', ':', '\0']) >= 0)
            throw new ArgumentException("A bounded simple component is required.", nameof(child));
    }

    internal static FileIdentity Identity(SafeFileHandle handle)
    {
        FileId identity = default;
        Check(GetFileInformationByHandleEx(handle, FileIdInfo, &identity, (uint)sizeof(FileId)));
        return new(identity.VolumeSerialNumber, identity.Id.Low, identity.Id.High);
    }

    internal static void Flush(SafeFileHandle handle) => Check(FlushFileBuffers(handle));

    internal readonly record struct RenameCompletion(int NtStatus, int IoStatus, int Win32Error);
    private enum RenameApi { NtRelative, Win32Qualification }

    internal static RenameCompletion Rename(SafeFileHandle source, SafeFileHandle parent, string destination, bool replace) =>
        RenameCore(source, parent, destination, replace, RenameApi.NtRelative);

    // Fresh-fixture comparison only. Never called as an alternative after a failing NT operation.
    internal static RenameCompletion ProbeWin32Rename(SafeFileHandle source, SafeFileHandle parent, string destination, bool replace) =>
        RenameCore(source, parent, destination, replace, RenameApi.Win32Qualification);

    private static RenameCompletion RenameCore(SafeFileHandle source, SafeFileHandle parent, string destination,
        bool replace, RenameApi api)
    {
        RequireChild(destination);
        int byteCount = checked(destination.Length * sizeof(char));
        int length = checked(sizeof(RenameHeader) + byteCount);
        byte[] buffer = new byte[length];
        bool retained = false;
        try
        {
            parent.DangerousAddRef(ref retained);
            fixed (byte* data = buffer)
            {
                var header = (RenameHeader*)data;
                header->Flags = replace ? RenameReplace | RenamePosix : 0;
                header->RootDirectory = parent.DangerousGetHandle();
                header->FileNameLength = (uint)byteCount;
                destination.AsSpan().CopyTo(new Span<char>(&header->FirstCharacter, destination.Length));
                if (api == RenameApi.Win32Qualification)
                {
                    Check(SetFileInformationByHandle(source, FileRenameInfoEx, data, (uint)length));
                    return new(0, 0, 0);
                }
                int status = NtSetInformationFile(source, out IoStatusBlock io, data, (uint)length, FileRenameInformationEx);
                int ioStatus = (int)io.Status;
                if (status != 0 || ioStatus != 0)
                {
                    int completion = status != 0 ? status : ioStatus;
                    throw new NativeFailure(status, checked((int)RtlNtStatusToDosError(completion)), ioStatus);
                }
                return new(status, ioStatus, 0);
            }
        }
        finally { if (retained) parent.DangerousRelease(); }
    }

    // Caller must prove ownership/current name before disposition; this primitive grants no cleanup authority.
    internal static void DisposeEntry(SafeFileHandle owned)
    {
        var disposition = new Disposition { Flags = DispositionDelete | DispositionPosix };
        Check(SetFileInformationByHandle(owned, FileDispositionInfoEx, &disposition, (uint)sizeof(Disposition)));
    }

    internal static string PrivateDescriptor()
    {
        using var current = WindowsIdentity.GetCurrent();
        string sid = current.User?.Value ?? throw new InvalidOperationException("Current owner SID unavailable.");
        // https://learn.microsoft.com/en-us/windows/win32/secauthz/security-descriptor-string-format
        // Protected DACL, a single current-owner full-access ACE, no inherited or group grant.
        return "O:" + sid + "D:P(A;;FA;;;" + sid + ")";
    }

    internal static string ReadOwnerAndDacl(SafeFileHandle handle)
    {
        uint result = GetSecurityInfo(handle, FileObject, OwnerAndDacl, 0, 0, 0, 0, out LocalMemory descriptor);
        using (descriptor)
        {
            if (result != 0) throw new NativeFailure(null, checked((int)result));
            Check(ConvertSecurityDescriptorToStringSecurityDescriptorW(descriptor, SddlRevision, OwnerAndDacl,
                out LocalMemory text, out _));
            using (text) return Marshal.PtrToStringUni(text.DangerousGetHandle())
                ?? throw new InvalidOperationException("Native descriptor string unavailable.");
        }
    }

    private static void Check(int result) { if (result == 0) throw new NativeFailure(null, Marshal.GetLastPInvokeError()); }

    internal sealed class NativeFailure(int? ntStatus, int win32Error, int? ioStatus = null) : IOException(
        $"NTSTATUS={(ntStatus is int status ? $"0x{status:x8}" : "not recorded")}; IO_STATUS={(ioStatus is int io ? $"0x{io:x8}" : "not recorded")}; Win32={win32Error}: {new Win32Exception(win32Error).Message}")
    {
        internal int? NtStatus { get; } = ntStatus;
        internal int Win32Error { get; } = win32Error;
        internal int? IoStatus { get; } = ioStatus;
    }

    // LocalFree owns descriptors/strings returned by advapi32. No descriptor buffer escapes its lifetime.
    internal sealed class LocalMemory : SafeHandleZeroOrMinusOneIsInvalid
    {
        public LocalMemory() : base(true) { }
        protected override bool ReleaseHandle() => LocalFree(handle) == 0;
    }

    // Signatures: Microsoft Learn per function. System32-only resolution, never application DLL search.
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, nint security,
        uint disposition, uint flags, nint template);
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntcreatefile
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtCreateFile(out nint handle, uint access, ObjectAttributes* attributes,
        out IoStatusBlock status, nint allocationSize, uint fileAttributes, uint share, uint disposition,
        uint options, nint eaBuffer, uint eaLength);
    // Microsoft explicitly specifies NtSetInformationFile for user-mode calls to this service.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-zwsetinformationfile
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtSetInformationFile(SafeFileHandle handle, out IoStatusBlock status,
        void* information, uint length, int informationClass);
    // https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-rtlntstatustodoserror
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint RtlNtStatusToDosError(int status);
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandleex
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetFileInformationByHandleEx(SafeFileHandle handle, int informationClass, void* data, uint length);
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SetFileInformationByHandle(SafeFileHandle handle, int informationClass, void* data, uint length);
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-flushfilebuffers
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int FlushFileBuffers(SafeFileHandle handle);
    // https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo
    [LibraryImport("advapi32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetSecurityInfo(SafeFileHandle handle, int kind, uint information,
        nint owner, nint group, nint dacl, nint sacl, out LocalMemory descriptor);
    // https://learn.microsoft.com/en-us/windows/win32/api/sddl/nf-sddl-convertstringsecuritydescriptortosecuritydescriptorw
    [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int ConvertStringSecurityDescriptorToSecurityDescriptorW(string text, uint revision,
        out LocalMemory descriptor, out uint length);
    // https://learn.microsoft.com/en-us/windows/win32/api/sddl/nf-sddl-convertsecuritydescriptortostringsecuritydescriptorw
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int ConvertSecurityDescriptorToStringSecurityDescriptorW(LocalMemory descriptor, uint revision,
        uint information, out LocalMemory text, out uint length);
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-localfree
    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint LocalFree(nint memory);
}
