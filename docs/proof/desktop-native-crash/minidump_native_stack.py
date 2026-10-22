"""Walk the ARM64 frame chain of a minidump's exception thread without a debugger, naming each frame module+offset.

Windows ARM64 code keeps the frame chain in x29 (fp): [fp] is the caller's fp and [fp+8] its lr. The walk is
approximate for leaf frames and for frames that omit the chain; every frame printed is a return address that was
actually on the stack. Stdlib only. Usage: python -I minidump_native_stack.py <file.dmp> [thread-id-hex]
"""
import struct
import sys

STREAM_THREAD_LIST = 3
STREAM_MODULE_LIST = 4
STREAM_EXCEPTION = 6
STREAM_MEMORY64_LIST = 9


class Dump:
    def __init__(self, f):
        self.f = f
        sig, version, count, rva, _cs, _ts, _flags = struct.unpack("<IIIIIIQ", self.read(0, 32))
        if sig != 0x504D444D:
            raise SystemExit("not a minidump")
        self.streams = {}
        for i in range(count):
            stype, size, srva = struct.unpack("<III", self.read(rva + 12 * i, 12))
            self.streams.setdefault(stype, []).append((size, srva))
        self.modules = self.read_modules()
        self.ranges = self.read_memory64()

    def read(self, offset, size):
        self.f.seek(offset)
        data = self.f.read(size)
        if len(data) != size:
            raise SystemExit(f"short read at {offset}")
        return data

    def read_string(self, rva):
        (length,) = struct.unpack("<I", self.read(rva, 4))
        return self.read(rva + 4, length).decode("utf-16-le", errors="replace")

    def read_modules(self):
        size, rva = self.streams[STREAM_MODULE_LIST][0]
        (count,) = struct.unpack("<I", self.read(rva, 4))
        out = []
        for i in range(count):
            base, msize, _cs, _ts, name_rva = struct.unpack("<QIIII", self.read(rva + 4 + 108 * i, 24))
            out.append((base, msize, self.read_string(name_rva)))
        return out

    def read_memory64(self):
        size, rva = self.streams[STREAM_MEMORY64_LIST][0]
        count, base_rva = struct.unpack("<QQ", self.read(rva, 16))
        out = []
        offset = base_rva
        for i in range(count):
            start, dsize = struct.unpack("<QQ", self.read(rva + 16 + 16 * i, 16))
            out.append((start, dsize, offset))
            offset += dsize
        return out

    def mem(self, address, size):
        for start, dsize, offset in self.ranges:
            if start <= address and address + size <= start + dsize:
                return self.read(offset + (address - start), size)
        return None

    def locate(self, address):
        for base, msize, name in self.modules:
            if base <= address < base + msize:
                short = name.rsplit("\\", 1)[-1]
                return f"{short}+{address - base:#x}"
        return "(no module)"

    def thread_contexts(self):
        size, rva = self.streams[STREAM_THREAD_LIST][0]
        (count,) = struct.unpack("<I", self.read(rva, 4))
        out = {}
        for i in range(count):
            tid, _sc, _pc, _pr, _teb, stack_start, stack_size, _stack_rva, ctx_size, ctx_rva = struct.unpack(
                "<IIIIQQIIII", self.read(rva + 4 + 48 * i, 48))
            out[tid] = (stack_start, stack_size, ctx_size, ctx_rva)
        return out


def arm64_regs(dump, ctx_rva, ctx_size):
    raw = dump.read(ctx_rva, ctx_size)
    # ARM64_NT_CONTEXT: ContextFlags u32, Cpsr u32, X0..X28, Fp(X29), Lr(X30), Sp, Pc
    x = struct.unpack_from("<31Q", raw, 8)
    sp, pc = struct.unpack_from("<QQ", raw, 8 + 31 * 8)
    return {"fp": x[29], "lr": x[30], "sp": sp, "pc": pc}


def walk(dump, regs, limit=48):
    frames = [("pc", regs["pc"]), ("lr", regs["lr"])]
    fp = regs["fp"]
    seen = set()
    while fp and fp not in seen and len(frames) < limit:
        seen.add(fp)
        link = dump.mem(fp, 16)
        if link is None:
            break
        next_fp, lr = struct.unpack("<QQ", link)
        if lr == 0:
            break
        frames.append(("fp", lr))
        if next_fp <= fp:
            break
        fp = next_fp
    return frames


def main(path, want=None):
    with open(path, "rb") as f:
        dump = Dump(f)
        contexts = dump.thread_contexts()
        if want is None:
            size, rva = dump.streams[STREAM_EXCEPTION][0]
            (want,) = struct.unpack("<I", dump.read(rva, 4))
        print(f"thread {want:#x}")
        if want not in contexts:
            raise SystemExit("thread not in dump")
        stack_start, stack_size, ctx_size, ctx_rva = contexts[want]
        regs = arm64_regs(dump, ctx_rva, ctx_size)
        print(f"  pc {regs['pc']:#x} {dump.locate(regs['pc'])}")
        print(f"  lr {regs['lr']:#x} {dump.locate(regs['lr'])}")
        print(f"  sp {regs['sp']:#x} fp {regs['fp']:#x} stack {stack_start:#x}+{stack_size:#x}")
        for i, (kind, addr) in enumerate(walk(dump, regs)):
            print(f"  {i:2d} {kind} {addr:#018x} {dump.locate(addr)}")
        print("every thread's pc module:")
        for tid, (_ss, _sz, csize, crva) in contexts.items():
            r = arm64_regs(dump, crva, csize)
            print(f"  {tid:#07x} pc {dump.locate(r['pc'])}  lr {dump.locate(r['lr'])}")


if __name__ == "__main__":
    main(sys.argv[1], int(sys.argv[2], 16) if len(sys.argv) > 2 else None)
