#!/usr/bin/env python3
"""Run atomic_spike under a Windows-shaped `os` (Inferred stand-in, not a Windows run).

The shim reports os.name == "nt", has no O_NOFOLLOW / O_NONBLOCK / killpg / getuid (as on Windows),
and makes os.open of a directory raise PermissionError (what Windows does). Everything else is the real os.
Usage: python3 spike-windows-shim.py   (exit 0: atomic_spike works under the shim; 1: it raised)
"""
import importlib.util
import os
import sys
import tempfile
import types
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ABSENT = {"O_NOFOLLOW", "O_NONBLOCK", "O_CLOEXEC", "O_DIRECTORY", "killpg", "getuid", "fchmod"}


class WindowsShapedOs(types.ModuleType):
    name = "nt"

    def __getattr__(self, attribute):
        if attribute in ABSENT:
            raise AttributeError("module 'os' has no attribute '{0}' (as on Windows)".format(attribute))
        return getattr(os, attribute)

    def open(self, path, flags, mode=0o777):
        if Path(path).is_dir():
            raise PermissionError(13, "Permission denied (os.open of a directory, as on Windows)", str(path))
        return os.open(path, flags, mode)


def main():
    for stub in ("blake3", "rfc8785"):
        sys.modules.setdefault(stub, types.ModuleType(stub))
    spec = importlib.util.spec_from_file_location("spike", ROOT / "tools/spikes/application-contract-vectors.py")
    spike = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(spike)
    spike.os = WindowsShapedOs("os")
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory).resolve() / "project.cfdw.json"
        try:
            spike.atomic_spike(path, b"complete envelope", None)
            spike.atomic_spike(path, b"second envelope", spike.hash_bytes(b"complete envelope"))
        except Exception as error:  # noqa: BLE001 - the proof reports any failure shape
            print("FAIL under the Windows-shaped os: {0}: {1}".format(type(error).__name__, error))
            return 1
        if path.read_bytes() != b"second envelope" or list(Path(directory).glob("*.lock")) \
                or list(Path(directory).glob("*.temporary")):
            print("FAIL: wrong content or leftover claim/temporary file")
            return 1
    print("OK: atomic_spike create and replace work under the Windows-shaped os")
    return 0


if __name__ == "__main__":
    sys.exit(main())
