"""Read the HEAP_FAILURE_INFORMATION a 0xC0000374 exception points at (ExceptionInformation[0]) out of a full minidump,
then show the memory around the failing block with every pointer-sized word resolved to a module. Stdlib only.

Usage: python -I heap_failure.py <file.dmp>
"""
import struct
import sys

FAILURE_TYPES = {
    0: "heap_failure_unknown", 1: "heap_failure_generic", 2: "heap_failure_entry_corruption",
    3: "heap_failure_multiple_entries_corruption", 4: "heap_failure_virtual_block_corruption",
    5: "heap_failure_buffer_overrun", 6: "heap_failure_buffer_underrun", 7: "heap_failure_block_not_busy",
    8: "heap_failure_invalid_argument", 9: "heap_failure_invalid_allocation_type", 10: "heap_failure_usage_after_free",
    11: "heap_failure_cross_heap_operation", 12: "heap_failure_freelists_corruption",
    13: "heap_failure_listentry_corruption", 14: "heap_failure_lfh_bitmap_mismatch",
    15: "heap_failure_segment_lfh_bitmap_corruption", 16: "heap_failure_segment_lfh_double_free",
    17: "heap_failure_vs_subsegment_corruption", 18: "heap_failure_null_heap", 19: "heap_failure_allocation_limit",
    20: "heap_failure_commit_limit", 21: "heap_failure_invalid_va_mgmt", 22: "heap_failure_invalid_rtl_heap",
    23: "heap_failure_invalid_segment_heap", 24: "heap_failure_ptr_not_in_heap", 25: "heap_failure_segment_lfh_delayed_free_corruption",
}

sys.path.insert(0, __file__.rsplit("\\", 1)[0].rsplit("/", 1)[0])
from minidump_native_stack import Dump  # noqa: E402  (same folder, my own script)


def words(dump, address, count):
    raw = dump.mem(address, 8 * count)
    if raw is None:
        return None
    return struct.unpack(f"<{count}Q", raw)


def show_block(dump, label, address, before=64, after=192):
    print(f"\n{label} {address:#x}: memory {address - before:#x}..{address + after:#x}")
    start = address - before
    ws = words(dump, start, (before + after) // 8)
    if ws is None:
        print("  (not in dump)")
        return
    for i, w in enumerate(ws):
        a = start + 8 * i
        mark = "<--" if a == address else "   "
        asc = "".join(chr(b) if 32 <= b < 127 else "." for b in w.to_bytes(8, "little"))
        loc = dump.locate(w) if 0x10000 <= w < 0x7FFFFFFFFFFF else ""
        print(f"  {a:#018x} {mark} {w:#018x}  {asc}  {loc}")


def main(path):
    with open(path, "rb") as f:
        dump = Dump(f)
        size, rva = dump.streams[6][0]
        thread_id, = struct.unpack("<I", dump.read(rva, 4))
        code, _fl, _rec, addr, nparams, _u = struct.unpack("<IIQQII", dump.read(rva + 8, 32))
        params = struct.unpack("<15Q", dump.read(rva + 40, 120))
        info = params[0]
        print(f"exception {code:#x} on thread {thread_id:#x} at {dump.locate(addr)}; failure info at {info:#x} ({dump.locate(info)})")
        raw = dump.mem(info, 96)
        if raw is None:
            raise SystemExit("failure info not in dump")
        version, ssize, ftype = struct.unpack_from("<III", raw, 0)
        heap, bad, p1, p2, p3, prev, nxt = struct.unpack_from("<7Q", raw, 16)
        exp = struct.unpack_from("<2Q", raw, 72)
        print(f"version {version} size {ssize} failure type {ftype} = {FAILURE_TYPES.get(ftype, '?')}")
        print(f"heap {heap:#x}  address {bad:#x}  param1 {p1:#x} param2 {p2:#x} param3 {p3:#x}")
        print(f"previous block {prev:#x}  next block {nxt:#x}  expected decoded entry {exp[0]:#x} {exp[1]:#x}")
        trace = words(dump, info + 88, 32)
        if trace:
            frames = [t for t in trace if t]
            if frames:
                print("stack trace recorded in the failure info:")
                for t in frames:
                    print(f"  {t:#018x} {dump.locate(t)}")
            else:
                print("no stack trace recorded in the failure info")
        before = int(sys.argv[2]) if len(sys.argv) > 2 else 64
        after = int(sys.argv[3]) if len(sys.argv) > 3 else 192
        for label, a in (("address", bad), ("param1", p1), ("param2", p2), ("previous", prev), ("next", nxt)):
            if 0x10000 <= a < 0x7FFFFFFFFFFF:
                show_block(dump, label, a, before, after)
        if 0x10000 <= heap < 0x7FFFFFFFFFFF:
            sig = words(dump, heap, 4)
            print(f"\nheap header words at {heap:#x}: " + (" ".join(f"{w:#x}" for w in sig) if sig else "(not in dump)"))


if __name__ == "__main__":
    main(sys.argv[1])
