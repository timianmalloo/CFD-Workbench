"""Resolve the module+offset frames printed by minidump_native_stack.py to symbol names with dbghelp and a local PDB folder.

Loads each System32 module named in the frames at the base the dump recorded, then asks dbghelp for the nearest
public symbol. Stdlib + ctypes only; dbghelp.dll is the one in System32 (it reads PDBs; it cannot download them).
Usage: python -I symbolize.py <file.dmp> <frames.txt> <pdb-folder>
"""
import ctypes
import ctypes.wintypes as wt
import os
import re
import struct
import sys

STREAM_MODULE_LIST = 4


def modules_of(path):
    with open(path, "rb") as f:
        head = f.read(32)
        _sig, _ver, count, rva = struct.unpack("<IIII", head[:16])
        f.seek(rva)
        dirs = f.read(12 * count)
        out = []
        for i in range(count):
            stype, size, srva = struct.unpack_from("<III", dirs, 12 * i)
            if stype != STREAM_MODULE_LIST:
                continue
            f.seek(srva)
            (n,) = struct.unpack("<I", f.read(4))
            for j in range(n):
                f.seek(srva + 4 + 108 * j)
                base, msize, _cs, _ts, name_rva = struct.unpack("<QIIII", f.read(24))
                f.seek(name_rva)
                (length,) = struct.unpack("<I", f.read(4))
                name = f.read(length).decode("utf-16-le", errors="replace")
                out.append((base, msize, name))
        return out


def main(dump, frames_file, pdb_folder):
    modules = modules_of(dump)
    by_short = {name.rsplit("\\", 1)[-1].lower(): (base, size, name) for base, size, name in modules}

    dbghelp = ctypes.WinDLL("dbghelp")
    dbghelp.SymSetOptions.restype = wt.DWORD
    dbghelp.SymSetOptions(0x2 | 0x4 | 0x10)  # UNDNAME | DEFERRED_LOADS | LOAD_LINES
    dbghelp.SymInitializeW.argtypes = [wt.HANDLE, wt.LPCWSTR, wt.BOOL]
    dbghelp.SymInitializeW.restype = wt.BOOL
    dbghelp.SymLoadModuleExW.argtypes = [wt.HANDLE, wt.HANDLE, wt.LPCWSTR, wt.LPCWSTR, ctypes.c_ulonglong, wt.DWORD, ctypes.c_void_p, wt.DWORD]
    dbghelp.SymLoadModuleExW.restype = ctypes.c_ulonglong
    dbghelp.SymFromAddrW.argtypes = [wt.HANDLE, ctypes.c_ulonglong, ctypes.POINTER(ctypes.c_ulonglong), ctypes.c_void_p]
    dbghelp.SymFromAddrW.restype = wt.BOOL
    h = wt.HANDLE(0x4711)
    if not dbghelp.SymInitializeW(h, os.path.abspath(pdb_folder), False):
        raise SystemExit(f"SymInitialize failed: {ctypes.GetLastError()}")

    loaded = {}

    def ensure(short):
        if short in loaded:
            return loaded[short]
        base, size, name = by_short[short]
        local = os.path.join(os.environ.get("SystemRoot", r"C:\Windows"), "System32", short)
        if not os.path.exists(local):
            local = name
        got = dbghelp.SymLoadModuleExW(h, None, local, None, base, size, None, 0)
        loaded[short] = got
        print(f"load {short} at {base:#x} size {size:#x} from {local}: {'ok' if got else 'failed ' + str(ctypes.GetLastError())}")
        return got

    buf = ctypes.create_string_buffer(88 + 2 * 512)
    struct.pack_into("<I", buf, 0, 88)
    struct.pack_into("<I", buf, 80, 512)  # MaxNameLen

    pattern = re.compile(r"(0x[0-9a-f]+)\s+([A-Za-z0-9_.]+)\+0x([0-9a-f]+)")
    with open(frames_file, encoding="utf-8") as f:
        for line in f:
            m = pattern.search(line)
            if not m:
                continue
            addr = int(m.group(1), 16)
            short = m.group(2).lower()
            if short not in by_short:
                print(line.rstrip() + "   (module not in dump list)")
                continue
            if not ensure(short):
                print(line.rstrip() + "   (module load failed)")
                continue
            disp = ctypes.c_ulonglong(0)
            if dbghelp.SymFromAddrW(h, addr, ctypes.byref(disp), buf):
                namelen, = struct.unpack_from("<I", buf, 76)
                name = buf.raw[84:84 + 2 * namelen].decode("utf-16-le", errors="replace")
                print(f"{line.rstrip()}   {short}!{name}+{disp.value:#x}")
            else:
                print(f"{line.rstrip()}   (no symbol, error {ctypes.GetLastError()})")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
