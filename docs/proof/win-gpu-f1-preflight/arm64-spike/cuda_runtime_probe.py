#!/usr/bin/env python3
"""Ruling 237 (5) spike, read-only: does the CUDA runtime built for arm64 servers (the NVIDIA `sbsa` repository) work on
top of the WSL driver on this host? Loads a libcudart.so.13 extracted from the cuda-cudart deb (dpkg -x into a scratch
directory, nothing installed) through ctypes and runs the smallest round trip that needs a live context: device count,
cudaMalloc, cudaMemset, cudaMemcpy back to the host, every byte checked, cudaFree. Prints one JSON object.
  python3 cuda_runtime_probe.py <path to libcudart.so.13>
Exit 0 when the round trip verified, 2 when the runtime loaded but a call refused, 3 when the library did not load.
"""
from __future__ import annotations

import ctypes
import json
import platform
import sys

BYTES = 1 << 20
FILL = 0x5A
cudaMemcpyDeviceToHost = 2


def main(path: str) -> int:
    result = {"schema": "cfdw-arm64-cuda-runtime-spike/1", "machine": platform.machine(), "library": path,
              "runtime_version": None, "device_count": None, "calls": [], "round_trip": None}
    try:
        rt = ctypes.CDLL(path)
    except OSError as error:
        result["load_error"] = str(error)
        print(json.dumps(result, indent=1, sort_keys=True))
        return 3

    def call(name: str, *args) -> int:
        code = getattr(rt, name)(*args)
        result["calls"].append({name: code})
        return code

    version = ctypes.c_int()
    if call("cudaRuntimeGetVersion", ctypes.byref(version)) == 0:
        result["runtime_version"] = {"raw": version.value, "cuda": f"{version.value // 1000}.{version.value % 1000 // 10}"}
    count = ctypes.c_int()
    if call("cudaGetDeviceCount", ctypes.byref(count)) != 0:
        print(json.dumps(result, indent=1, sort_keys=True))
        return 2
    result["device_count"] = count.value
    if count.value == 0 or call("cudaSetDevice", 0) != 0:
        print(json.dumps(result, indent=1, sort_keys=True))
        return 2
    device = ctypes.c_void_p()
    if call("cudaMalloc", ctypes.byref(device), ctypes.c_size_t(BYTES)) != 0:
        print(json.dumps(result, indent=1, sort_keys=True))
        return 2
    host = (ctypes.c_ubyte * BYTES)()
    ok = (call("cudaMemset", device, FILL, ctypes.c_size_t(BYTES)) == 0 and
          call("cudaDeviceSynchronize") == 0 and
          call("cudaMemcpy", host, device, ctypes.c_size_t(BYTES), cudaMemcpyDeviceToHost) == 0)
    call("cudaFree", device)
    matching = sum(1 for b in host if b == FILL) if ok else 0
    result["round_trip"] = {"bytes": BYTES, "fill": FILL, "matching": matching, "verified": ok and matching == BYTES}
    print(json.dumps(result, indent=1, sort_keys=True))
    return 0 if result["round_trip"]["verified"] else 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "libcudart.so.13"))
