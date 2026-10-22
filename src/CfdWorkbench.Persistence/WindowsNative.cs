using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace CfdWorkbench.Persistence;

// The native boundary of WindowsProjectStore (ProjectStore dispatches to it on Windows).
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
    private const uint DirectoryFile = 0x1, SynchronousNonalert = 0x20, NonDirectory = 0x40;
    private const uint CaseInsensitive = 0x40, DontReparse = 0x1000;

    // minwinbase.h FILE_INFO_BY_HANDLE_CLASS ordinal values (SDK declaration and Learn).
    // https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ne-minwinbase-file_info_by_handle_class
    private const int FileStandardInfo = 1, FileRenameInfo = 3, FileAttributeTagInfo = 9, FileIdInfo = 18,
        FileDispositionInfoEx = 21, FileRenameInfoEx = 22, FileCaseSensitiveInfo = 23;
    // winnt.h: FILE_ATTRIBUTE_DIRECTORY=0x10, FILE_ATTRIBUTE_REPARSE_POINT=0x400,
    // FILE_ATTRIBUTE_RECALL_ON_OPEN=0x40000, FILE_ATTRIBUTE_PINNED=0x80000, FILE_ATTRIBUTE_UNPINNED=0x100000,
    // FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS=0x400000. FILE_READ_ATTRIBUTES=0x80 (winnt.h).
    // https://learn.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants
    internal const uint AttributeDirectory = 0x10, AttributeReparsePoint = 0x400, AttributeRecallOnOpen = 0x40000,
        AttributePinned = 0x80000, AttributeUnpinned = 0x100000, AttributeRecallOnDataAccess = 0x400000;
    private const uint ReadAttributes = 0x80;
    // winnt.h: SYNCHRONIZE=0x00100000, READ_CONTROL=0x00020000, FILE_LIST_DIRECTORY=0x1 (a directory handle's enumeration right).
    internal const uint ListDirectoryAccess = 0x1;
    private const uint Synchronize = 0x00100000, ReadControl = 0x00020000;
    // wdm.h FILE_INFORMATION_CLASS: FileNamesInformation = 12 (NtQueryDirectoryFile). ntstatus.h: STATUS_NO_MORE_FILES 0x80000006 and
    // STATUS_NO_SUCH_FILE 0xC000000F end an enumeration. Win32 ERROR_SHARING_VIOLATION = 32 (winerror.h).
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntquerydirectoryfile
    private const int FileNamesInformation = 12;
    private const int StatusNoMoreFiles = unchecked((int)0x80000006), StatusNoSuchFile = unchecked((int)0xC000000F);
    // winnt.h: FILE_ATTRIBUTE_READONLY = 0x1, FILE_ATTRIBUTE_ENCRYPTED = 0x4000.
    internal const uint AttributeReadOnly = 0x1, AttributeEncrypted = 0x4000;
    // wdm.h FS_INFORMATION_CLASS: FileFsDeviceInformation = 4 (NtQueryVolumeInformationFile).
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_file_fs_device_information
    private const int FileFsDeviceInformation = 4;
    // ntifs.h / wdm.h device type: FILE_DEVICE_DISK = 0x7 (a local disk volume; FILE_DEVICE_NETWORK_FILE_SYSTEM = 0x14 is a redirector).
    // Characteristics: FILE_REMOVABLE_MEDIA 0x1, FILE_FLOPPY_DISKETTE 0x4, FILE_REMOTE_DEVICE 0x10,
    // FILE_CHARACTERISTIC_TS_DEVICE 0x1000, FILE_CHARACTERISTIC_WEBDAV_DEVICE 0x2000.
    private const uint DeviceDisk = 0x7, NotLocalFixedCharacteristics = 0x1 | 0x4 | 0x10 | 0x1000 | 0x2000;
    // ntifs.h: PHCM_APPLICATION_DEFAULT 0, PHCM_DISGUISE_PLACEHOLDER 1, PHCM_EXPOSE_PLACEHOLDERS 2 (CHAR); negative values are errors.
    private const sbyte PlaceholdersExposed = 2;
    // winnt.h FILE_CS_FLAG_CASE_SENSITIVE_DIR = 1 in FILE_CASE_SENSITIVE_INFO.Flags.
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_case_sensitive_info
    private const uint CaseSensitiveDirectory = 1;
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

    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_attribute_tag_info
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileAttributeTag { internal uint Attributes, ReparseTag; }
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_standard_info
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileStandard { internal long AllocationSize, EndOfFile; internal uint NumberOfLinks; internal byte DeletePending, Directory; }
    // https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_case_sensitive_info
    [StructLayout(LayoutKind.Sequential)]
    internal struct CaseSensitiveInfo { internal uint Flags; }
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_file_fs_device_information
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileFsDevice { internal uint DeviceType, Characteristics; }

    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_names_information
    // The fixed header; the WCHAR name follows it (FileNameLength bytes), without padding.
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileNamesHeader { internal uint NextEntryOffset, FileIndex, FileNameLength; }

    internal readonly record struct FileIdentity(ulong Volume, ulong Low, ulong High);

    /// <summary>Opens a drive root (or a trusted fixture directory) by path; every later component is opened relative to it.</summary>
    internal static SafeFileHandle OpenRoot(string absolutePath, uint access = DirectoryAccess)
    {
        var handle = CreateFileW(absolutePath, access, ShareRead | ShareWrite, 0,
            OpenExisting, BackupSemantics | OpenReparsePoint, 0);
        if (!handle.IsInvalid) return handle;
        int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new NativeFailure(null, error);
    }

    internal static SafeFileHandle OpenFixtureDirectory(string absolutePath) => OpenRoot(absolutePath);

    /// <summary>One directory component opened relative to a retained parent; share read and write, never delete.</summary>
    internal static SafeFileHandle OpenDirectory(SafeFileHandle parent, string child, uint access = DirectoryAccess) =>
        OpenRelative(parent, child, access, ShareRead | ShareWrite, NtOpen, 0, directory: true);

    /// <summary>
    /// Clear's open of a leftover (design 2.3): DELETE | READ_CONTROL | FILE_READ_ATTRIBUTES | SYNCHRONIZE, share mode FILE_SHARE_READ only,
    /// the entry itself (FILE_OPEN_REPARSE_POINT, no reparse followed). A live writer holds write and DELETE access and shares read only,
    /// so this open fails with ERROR_SHARING_VIOLATION (32) while any writer holds the entry; a reader that shares write and delete
    /// does not block it.
    /// </summary>
    internal static SafeFileHandle OpenExclusiveForDelete(SafeFileHandle parent, string child) =>
        OpenRelative(parent, child, DeleteAccess | ReadControl | ReadAttributes | Synchronize, ShareRead, NtOpen, 0);

    /// <summary>True when the name answers (case-insensitively) in the directory: an attribute-only open, any share mode. Not found is false; any other error throws.</summary>
    internal static bool EntryExists(SafeFileHandle parent, string child)
    {
        try { using var entry = OpenRelative(parent, child, ReadAttributes | Synchronize, ShareRead | ShareWrite | ShareDelete, NtOpen, 0); return true; }
        catch (NativeFailure failure) when (failure.Win32Error is 2 or 3) { return false; } // ERROR_FILE_NOT_FOUND, ERROR_PATH_NOT_FOUND
    }

    /// <summary>The entry names of a directory handle opened with FILE_LIST_DIRECTORY (NtQueryDirectoryFile, FileNamesInformation), without "." and "..".</summary>
    internal static List<string> ListNames(SafeFileHandle directory)
    {
        var names = new List<string>();
        byte[] buffer = new byte[65536];
        bool restart = true;
        fixed (byte* data = buffer)
        {
            while (true)
            {
                int status = NtQueryDirectoryFile(directory, 0, 0, 0, out IoStatusBlock io, data, (uint)buffer.Length, FileNamesInformation,
                    0, 0, restart ? (byte)1 : (byte)0);
                restart = false;
                if (status is StatusNoMoreFiles or StatusNoSuchFile) return names;
                if (status != 0) throw new NativeFailure(status, checked((int)RtlNtStatusToDosError(status)));
                for (int offset = 0; ;)
                {
                    var header = (FileNamesHeader*)(data + offset);
                    string name = new((char*)(data + offset + sizeof(FileNamesHeader)), 0, checked((int)header->FileNameLength) / sizeof(char));
                    if (name is not "." and not "..") names.Add(name);
                    if (header->NextEntryOffset == 0) break;
                    offset += checked((int)header->NextEntryOffset);
                }
            }
        }
    }

    internal static FileAttributeTag AttributeTag(SafeFileHandle handle)
    {
        FileAttributeTag tag = default;
        Check(GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, (uint)sizeof(FileAttributeTag)));
        return tag;
    }

    /// <summary>Attributes and reparse tag of a path, opened without following a reparse point (cloud-folder detection only).</summary>
    internal static FileAttributeTag AttributeTag(string absolutePath)
    {
        // The extended-length form lifts MAX_PATH; the caller passes a fully qualified drive path.
        using var handle = CreateFileW(@"\\?\" + absolutePath, ReadAttributes, ShareRead | ShareWrite | ShareDelete, 0,
            OpenExisting, BackupSemantics | OpenReparsePoint, 0);
        if (handle.IsInvalid) throw new NativeFailure(null, Marshal.GetLastPInvokeError());
        return AttributeTag(handle);
    }

    internal static FileStandard Standard(SafeFileHandle handle)
    {
        FileStandard standard = default;
        Check(GetFileInformationByHandleEx(handle, FileStandardInfo, &standard, (uint)sizeof(FileStandard)));
        return standard;
    }

    /// <summary>
    /// A normal process runs with OneDrive placeholders disguised (mode 1): the reparse attribute and the cloud tag are hidden.
    /// This exposes them (mode 2) for the calling thread until the scope is disposed, which restores the previous mode.
    /// The mode is thread-scoped, so the set, the work and the restore must run synchronously on one thread, with no await between.
    /// </summary>
    internal static PlaceholderScope ExposePlaceholders()
    {
        sbyte previous = RtlSetThreadPlaceholderCompatibilityMode(PlaceholdersExposed);
        if (previous < 0) throw new NativeFailure(null, 50); // ERROR_NOT_SUPPORTED: without exposure the cloud check is blind, so refuse
        return new PlaceholderScope(previous);
    }

    internal readonly struct PlaceholderScope(sbyte previous) : IDisposable
    {
        public void Dispose() => RtlSetThreadPlaceholderCompatibilityMode(previous);
    }

    /// <summary>Device type and characteristics of the volume that holds the handle (FILE_FS_DEVICE_INFORMATION).</summary>
    internal static (uint DeviceType, uint Characteristics) VolumeDevice(SafeFileHandle handle)
    {
        FileFsDevice device = default;
        int status = NtQueryVolumeInformationFile(handle, out IoStatusBlock io, &device, (uint)sizeof(FileFsDevice), FileFsDeviceInformation);
        if (status != 0) throw new NativeFailure(status, checked((int)RtlNtStatusToDosError(status)));
        return (device.DeviceType, device.Characteristics);
    }

    /// <summary>A fixed local disk: a disk device that is not removable, remote, a floppy, terminal-services redirected or WebDAV.</summary>
    internal static bool IsFixedLocalVolume(uint deviceType, uint characteristics) =>
        deviceType == DeviceDisk && (characteristics & NotLocalFixedCharacteristics) == 0;

    internal static bool IsCaseSensitive(SafeFileHandle directory)
    {
        CaseSensitiveInfo info = default;
        Check(GetFileInformationByHandleEx(directory, FileCaseSensitiveInfo, &info, (uint)sizeof(CaseSensitiveInfo)));
        return (info.Flags & CaseSensitiveDirectory) != 0;
    }

    /// <summary>The file system name of the volume that holds the handle, for example NTFS.</summary>
    internal static string VolumeFileSystem(SafeFileHandle handle)
    {
        char* name = stackalloc char[261];
        Check(GetVolumeInformationByHandleW(handle, null, 0, null, null, null, name, 261));
        return new string(name);
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

    // winnt.h READ_CONTROL = 0x00020000, WRITE_DAC = 0x00040000; DACL_SECURITY_INFORMATION = 0x4,
    // PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000 (SetSecurityInfo stops inheritance); winerror.h ERROR_ALREADY_EXISTS = 183.
    // https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-setsecurityinfo
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createdirectoryw
    private const uint ReadControl = 0x00020000, WriteDac = 0x00040000, DaclProtected = 0x80000004;
    private const int AlreadyExists = 183;

    // https://learn.microsoft.com/en-us/windows/win32/api/wtypesbase/ns-wtypesbase-security_attributes
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes { internal uint Length; internal nint SecurityDescriptor; internal int InheritHandle; }

    /// <summary>
    /// The preference folder is private: created with the private descriptor, or, if it already exists (an earlier build made it
    /// inheriting the profile's ACL), tightened in place when the current user owns it. False means the folder belongs to someone
    /// else and is left unchanged. The parent is made by the caller and keeps its inherited ACL.
    /// </summary>
    internal static bool EnsurePrivateDirectory(string path)
    {
        Check(ConvertStringSecurityDescriptorToSecurityDescriptorW(PrivateDirectoryDescriptor(), SddlRevision, out LocalMemory descriptor, out _));
        using (descriptor)
        {
            bool retained = false;
            try
            {
                descriptor.DangerousAddRef(ref retained);
                var attributes = new SecurityAttributes { Length = (uint)sizeof(SecurityAttributes), SecurityDescriptor = descriptor.DangerousGetHandle() };
                if (CreateDirectoryW(path, &attributes) != 0) return true;
                int error = Marshal.GetLastPInvokeError();
                if (error != AlreadyExists) throw new NativeFailure(null, error);
                if (!OwnedByCurrentUser(path)) return false;
                using var handle = CreateFileW(path, ReadControl | WriteDac, ShareRead | ShareWrite | ShareDelete, 0, OpenExisting,
                    BackupSemantics | OpenReparsePoint, 0);
                if (handle.IsInvalid) throw new NativeFailure(null, Marshal.GetLastPInvokeError());
                Check(GetSecurityDescriptorDacl(descriptor, out _, out nint acl, out _));
                uint result = SetSecurityInfo(handle, FileObject, DaclProtected, 0, 0, acl, 0);
                if (result != 0) throw new NativeFailure(null, checked((int)result));
                return true;
            }
            finally { if (retained) descriptor.DangerousRelease(); }
        }
    }

    private static bool OwnedByCurrentUser(string path)
    {
        using var handle = CreateFileW(path, ReadControl, ShareRead | ShareWrite | ShareDelete, 0, OpenExisting,
            BackupSemantics | OpenReparsePoint, 0);
        if (handle.IsInvalid) throw new NativeFailure(null, Marshal.GetLastPInvokeError());
        var owner = new System.Security.AccessControl.RawSecurityDescriptor(ReadOwnerAndDacl(handle)).Owner;
        using var current = WindowsIdentity.GetCurrent();
        return owner is not null && owner.Value == current.User?.Value;
    }

    internal static SafeFileHandle OpenReader(SafeFileHandle parent, string child, uint share = ShareRead | ShareDelete) =>
        OpenRelative(parent, child, ReadAccess, share, NtOpen, 0);

    private static SafeFileHandle OpenRelative(SafeFileHandle parent, string child, uint access, uint share,
        uint disposition, nint securityDescriptor, bool directory = false)
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
                    NormalAttributes, share, disposition,
                    SynchronousNonalert | (directory ? DirectoryFile : NonDirectory) | OpenReparsePoint, 0, 0);
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
    private enum RenameApi { NtRelative, Win32Qualification, Win32AbsoluteQualification, Win32LegacyQualification }

    internal static RenameCompletion Rename(SafeFileHandle source, SafeFileHandle parent, string destination, bool replace) =>
        RenameCore(source, parent, destination, replace, RenameApi.NtRelative);

    // Fresh-fixture comparison only. Never called as an alternative after a failing NT operation.
    internal static RenameCompletion ProbeWin32Rename(SafeFileHandle source, SafeFileHandle parent, string destination, bool replace) =>
        RenameCore(source, parent, destination, replace, RenameApi.Win32Qualification);

    // Ruling 145 attribution only, on an independent trusted fixture. Never a store mutation route.
    internal static RenameCompletion ProbeWin32AbsoluteRename(SafeFileHandle source, SafeFileHandle parent, string absoluteDestination) =>
        RenameCore(source, parent, absoluteDestination, false, RenameApi.Win32AbsoluteQualification);

    internal static RenameCompletion ProbeWin32LegacyRename(SafeFileHandle source, SafeFileHandle parent, string destination) =>
        RenameCore(source, parent, destination, false, RenameApi.Win32LegacyQualification);

    private static RenameCompletion RenameCore(SafeFileHandle source, SafeFileHandle parent, string destination,
        bool replace, RenameApi api)
    {
        if (api == RenameApi.Win32AbsoluteQualification)
        {
            if (!Path.IsPathFullyQualified(destination) || destination.Length < 3 || destination[1] != ':')
                throw new ArgumentException("An absolute DOS fixture path is required.", nameof(destination));
        }
        else RequireChild(destination);
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
                header->RootDirectory = api == RenameApi.Win32AbsoluteQualification ? 0 : parent.DangerousGetHandle();
                header->FileNameLength = (uint)byteCount;
                destination.AsSpan().CopyTo(new Span<char>(&header->FirstCharacter, destination.Length));
                if (api != RenameApi.NtRelative)
                {
                    int informationClass = api == RenameApi.Win32LegacyQualification ? FileRenameInfo : FileRenameInfoEx;
                    Check(SetFileInformationByHandle(source, informationClass, data, (uint)length));
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

    /// <summary>
    /// The folder form of the private descriptor: the same single current-owner ACE, marked container-inherit and object-inherit
    /// (SDDL OI, CI = winnt.h OBJECT_INHERIT_ACE 0x1, CONTAINER_INHERIT_ACE 0x2). Without it a protected folder DACL passes nothing
    /// down and the files in an existing folder, which inherited their ACL, would lose all access when the DACL is propagated.
    /// </summary>
    internal static string PrivateDirectoryDescriptor() => PrivateDescriptor().Replace("(A;;FA;", "(A;OICI;FA;");

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
        // Rulings 145 and 152: only the qualified sharing violation has a product code.
        internal string? ProductCode => Win32Error == 32 ? "DOC-CONFLICT" : null;
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
    // ntifs.h RtlSetThreadPlaceholderCompatibilityMode (ntdll export): returns the previous mode, or a negative error.
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial sbyte RtlSetThreadPlaceholderCompatibilityMode(sbyte mode);
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntquerydirectoryfile
    // Event, ApcRoutine, ApcContext and FileName are null (0); ReturnSingleEntry = FALSE; RestartScan is a BOOLEAN (1 byte).
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtQueryDirectoryFile(SafeFileHandle handle, nint evt, nint apcRoutine, nint apcContext,
        out IoStatusBlock status, void* information, uint length, int informationClass, byte returnSingleEntry, nint fileName, byte restartScan);
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntqueryvolumeinformationfile
    [LibraryImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int NtQueryVolumeInformationFile(SafeFileHandle handle, out IoStatusBlock status,
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
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumeinformationbyhandlew
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetVolumeInformationByHandleW(SafeFileHandle handle, char* volumeName, uint volumeNameSize,
        uint* serial, uint* maximumComponentLength, uint* flags, char* fileSystemName, uint fileSystemNameSize);
    // https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo
    [LibraryImport("advapi32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetSecurityInfo(SafeFileHandle handle, int kind, uint information,
        nint owner, nint group, nint dacl, nint sacl, out LocalMemory descriptor);
    // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createdirectoryw
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CreateDirectoryW(string path, SecurityAttributes* attributes);
    // https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-getsecuritydescriptordacl
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetSecurityDescriptorDacl(LocalMemory descriptor, out int present, out nint acl, out int defaulted);
    // https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-setsecurityinfo
    [LibraryImport("advapi32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint SetSecurityInfo(SafeFileHandle handle, int kind, uint information,
        nint owner, nint group, nint dacl, nint sacl);
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
