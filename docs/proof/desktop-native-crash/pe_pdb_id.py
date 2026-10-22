"""Print the symbol-server key (GUID+age) and PDB name of a PE file's RSDS record. Stdlib only.

Usage: python -I pe_pdb_id.py <file.dll>   ->   <pdbname> <key>
"""
import struct
import sys
import uuid


def main(path):
    with open(path, "rb") as f:
        data = f.read()
    (e_lfanew,) = struct.unpack_from("<I", data, 0x3C)
    if data[e_lfanew:e_lfanew + 4] != b"PE\0\0":
        raise SystemExit("not a PE file")
    coff = e_lfanew + 4
    nsections, = struct.unpack_from("<H", data, coff + 2)
    opt_size, = struct.unpack_from("<H", data, coff + 16)
    opt = coff + 20
    magic, = struct.unpack_from("<H", data, opt)
    dd = opt + (112 if magic == 0x20B else 96)
    debug_rva, debug_size = struct.unpack_from("<II", data, dd + 6 * 8)
    sections = []
    sec = opt + opt_size
    for i in range(nsections):
        vsize, vaddr, rawsize, rawptr = struct.unpack_from("<IIII", data, sec + 40 * i + 8)
        sections.append((vaddr, max(vsize, rawsize), rawptr))

    def to_offset(rva):
        for vaddr, size, rawptr in sections:
            if vaddr <= rva < vaddr + size:
                return rawptr + (rva - vaddr)
        raise SystemExit(f"rva {rva:#x} outside sections")

    off = to_offset(debug_rva)
    for i in range(debug_size // 28):
        dtype, dsize, draw_rva, draw_ptr = struct.unpack_from("<IIII", data, off + 28 * i + 12)
        if dtype == 2 and data[draw_ptr:draw_ptr + 4] == b"RSDS":
            guid = uuid.UUID(bytes_le=data[draw_ptr + 4:draw_ptr + 20])
            age, = struct.unpack_from("<I", data, draw_ptr + 20)
            name = data[draw_ptr + 24:data.index(b"\0", draw_ptr + 24)].decode("ascii", errors="replace")
            print(f"{name} {guid.hex.upper()}{age:X}")
            return
    raise SystemExit("no RSDS record")


if __name__ == "__main__":
    main(sys.argv[1])
