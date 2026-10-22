"""List the loaded modules of every running process with the given image name, via psapi. Stdlib + ctypes only.

Usage: python -I modules_of_process.py CfdWorkbench.Desktop.Tests.exe [name-substring ...]
With substrings, prints per process only whether each substring is among its modules.
"""
import ctypes
import ctypes.wintypes as wt
import sys

k32 = ctypes.WinDLL("kernel32", use_last_error=True)
psapi = ctypes.WinDLL("psapi", use_last_error=True)

TH32CS_SNAPPROCESS = 0x2
PROCESS_QUERY_INFORMATION = 0x400
PROCESS_VM_READ = 0x10
LIST_MODULES_ALL = 0x3


class PROCESSENTRY32W(ctypes.Structure):
    _fields_ = [("dwSize", wt.DWORD), ("cntUsage", wt.DWORD), ("th32ProcessID", wt.DWORD),
                ("th32DefaultHeapID", ctypes.c_void_p), ("th32ModuleID", wt.DWORD), ("cntThreads", wt.DWORD),
                ("th32ParentProcessID", wt.DWORD), ("pcPriClassBase", wt.LONG), ("dwFlags", wt.DWORD),
                ("szExeFile", wt.WCHAR * 260)]


k32.CreateToolhelp32Snapshot.restype = wt.HANDLE
k32.Process32FirstW.argtypes = [wt.HANDLE, ctypes.POINTER(PROCESSENTRY32W)]
k32.Process32NextW.argtypes = [wt.HANDLE, ctypes.POINTER(PROCESSENTRY32W)]
k32.OpenProcess.restype = wt.HANDLE
k32.OpenProcess.argtypes = [wt.DWORD, wt.BOOL, wt.DWORD]
psapi.EnumProcessModulesEx.argtypes = [wt.HANDLE, ctypes.POINTER(wt.HMODULE), wt.DWORD, ctypes.POINTER(wt.DWORD), wt.DWORD]
psapi.GetModuleFileNameExW.argtypes = [wt.HANDLE, wt.HMODULE, wt.LPWSTR, wt.DWORD]


def processes_named(name):
    snap = k32.CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0)
    entry = PROCESSENTRY32W()
    entry.dwSize = ctypes.sizeof(entry)
    out = []
    ok = k32.Process32FirstW(snap, ctypes.byref(entry))
    while ok:
        if entry.szExeFile.lower() == name.lower():
            out.append((entry.th32ProcessID, entry.th32ParentProcessID))
        ok = k32.Process32NextW(snap, ctypes.byref(entry))
    k32.CloseHandle(snap)
    return out


def modules(pid):
    h = k32.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, pid)
    if not h:
        return None
    arr = (wt.HMODULE * 2048)()
    needed = wt.DWORD(0)
    if not psapi.EnumProcessModulesEx(h, arr, ctypes.sizeof(arr), ctypes.byref(needed), LIST_MODULES_ALL):
        k32.CloseHandle(h)
        return None
    names = []
    buf = ctypes.create_unicode_buffer(1024)
    for i in range(needed.value // ctypes.sizeof(wt.HMODULE)):
        if psapi.GetModuleFileNameExW(h, arr[i], buf, 1024):
            names.append(buf.value)
    k32.CloseHandle(h)
    return names


def main(name, wanted):
    procs = processes_named(name)
    print(f"{len(procs)} process(es) named {name}")
    for pid, ppid in procs:
        mods = modules(pid)
        if mods is None:
            print(f"  pid {pid} (parent {ppid}): modules not readable, error {ctypes.get_last_error()}")
            continue
        if wanted:
            flags = " ".join(f"{w}={'yes' if any(w.lower() in m.lower() for m in mods) else 'no'}" for w in wanted)
            print(f"  pid {pid} (parent {ppid}) {len(mods)} modules: {flags}")
            for w in wanted:
                for m in mods:
                    if w.lower() in m.lower():
                        print(f"      {m}")
        else:
            print(f"  pid {pid} (parent {ppid}): " + " ".join(sorted(mods)))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2:])
