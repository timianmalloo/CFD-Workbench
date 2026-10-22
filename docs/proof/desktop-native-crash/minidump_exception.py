"""Read a Windows minidump's exception stream and module list without a debugger.

Prints the faulting thread id, the exception code and address, the module that contains the address with its offset,
and the exception parameters. Stdlib only. Usage: python -I minidump_exception.py <file.dmp>
"""
import struct
import sys

MINIDUMP_SIGNATURE = 0x504D444D  # 'MDMP'
STREAM_MODULE_LIST = 4
STREAM_EXCEPTION = 6
STREAM_SYSTEM_INFO = 7


def read_at(f, offset, size):
    f.seek(offset)
    data = f.read(size)
    if len(data) != size:
        raise SystemExit(f"short read at {offset}: wanted {size}, got {len(data)}")
    return data


def read_string(f, rva):
    (length,) = struct.unpack("<I", read_at(f, rva, 4))
    return read_at(f, rva + 4, length).decode("utf-16-le", errors="replace")


def main(path):
    with open(path, "rb") as f:
        sig, version, stream_count, stream_rva, checksum, timestamp, flags = struct.unpack("<IIIIIIQ", read_at(f, 0, 32))
        if sig != MINIDUMP_SIGNATURE:
            raise SystemExit(f"not a minidump: signature {sig:#x}")
        streams = {}
        for i in range(stream_count):
            stype, size, rva = struct.unpack("<III", read_at(f, stream_rva + 12 * i, 12))
            streams.setdefault(stype, []).append((size, rva))
        print(f"minidump: {stream_count} streams, flags {flags:#x}, timestamp {timestamp}")

        if STREAM_SYSTEM_INFO in streams:
            size, rva = streams[STREAM_SYSTEM_INFO][0]
            arch, = struct.unpack("<H", read_at(f, rva, 2))
            print(f"processor architecture: {arch} (12 = ARM64, 9 = x64)")

        modules = []
        if STREAM_MODULE_LIST in streams:
            size, rva = streams[STREAM_MODULE_LIST][0]
            (count,) = struct.unpack("<I", read_at(f, rva, 4))
            for i in range(count):
                base, msize, checksum, stamp, name_rva = struct.unpack("<QIIII", read_at(f, rva + 4 + 108 * i, 24))
                modules.append((base, msize, read_string(f, name_rva)))
            print(f"modules: {count}")

        def locate(address):
            for base, msize, name in modules:
                if base <= address < base + msize:
                    return f"{name}+{address - base:#x}"
            return "(no module)"

        if STREAM_EXCEPTION not in streams:
            print("no exception stream")
            return
        for size, rva in streams[STREAM_EXCEPTION]:
            thread_id, _align = struct.unpack("<II", read_at(f, rva, 8))
            code, eflags, record, address, nparams, _unused = struct.unpack("<IIQQII", read_at(f, rva + 8, 32))
            params = struct.unpack("<15Q", read_at(f, rva + 40, 120))
            print(f"exception thread id: {thread_id:#x} ({thread_id})")
            print(f"exception code: {code:#010x}  flags {eflags:#x}  nested record {record:#x}")
            print(f"exception address: {address:#x} = {locate(address)}")
            print(f"parameters ({nparams}): " + ", ".join(f"{p:#x}" for p in params[:nparams]))
            for p in params[:nparams]:
                if p:
                    print(f"  param {p:#x} = {locate(p)}")


if __name__ == "__main__":
    main(sys.argv[1])
