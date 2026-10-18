#!/usr/bin/env python3
"""Ruling 237 (5) spike, read-only: is CUDA reachable from WSL on this host? Loads the WSL-provided CUDA driver library
through ctypes (no toolkit, no install) and prints one JSON object: CPU architecture, kernel, the driver library found,
cuInit's result, the driver's CUDA version, the device count and each device's name and compute capability.
Every failure is recorded as a value, never raised: an absent library or a refused cuInit is the finding.
  python3 cuda_driver_probe.py            (inside WSL)
Exit 0 when a device was enumerated, 2 when the library loaded but CUDA refused, 3 when no driver library was found.
"""
from __future__ import annotations

import ctypes
import json
import os
import platform
import sys

CANDIDATES = ["/usr/lib/wsl/lib/libcuda.so.1", "/usr/lib/wsl/lib/libcuda.so", "libcuda.so.1", "libcuda.so"]
CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MAJOR = 75
CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MINOR = 76


def main() -> int:
    result = {
        "schema": "cfdw-arm64-cuda-spike/1",
        "machine": platform.machine(),
        "kernel": platform.release(),
        "wsl_lib_dir": sorted(os.listdir("/usr/lib/wsl/lib")) if os.path.isdir("/usr/lib/wsl/lib") else "absent",
        "dxg_device": os.path.exists("/dev/dxg"),
        "library": None, "cuInit": None, "driver_version": None, "device_count": None, "devices": [],
    }
    lib = None
    for name in CANDIDATES:
        try:
            lib = ctypes.CDLL(name)
            result["library"] = name
            break
        except OSError as error:
            result.setdefault("load_errors", []).append({name: str(error)})
    if lib is None:
        print(json.dumps(result, indent=1, sort_keys=True))
        return 3
    code = lib.cuInit(ctypes.c_uint(0))
    result["cuInit"] = code
    if code != 0:
        print(json.dumps(result, indent=1, sort_keys=True))
        return 2
    version = ctypes.c_int()
    lib.cuDriverGetVersion(ctypes.byref(version))
    result["driver_version"] = {"raw": version.value, "cuda": f"{version.value // 1000}.{version.value % 1000 // 10}"}
    count = ctypes.c_int()
    result["device_count"] = count.value if lib.cuDeviceGetCount(ctypes.byref(count)) == 0 else None
    for index in range(count.value):
        device = ctypes.c_int()
        name = ctypes.create_string_buffer(256)
        major, minor = ctypes.c_int(), ctypes.c_int()
        entry = {"index": index}
        if lib.cuDeviceGet(ctypes.byref(device), index) == 0:
            lib.cuDeviceGetName(name, 256, device)
            lib.cuDeviceGetAttribute(ctypes.byref(major), CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MAJOR, device)
            lib.cuDeviceGetAttribute(ctypes.byref(minor), CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MINOR, device)
            total = ctypes.c_size_t()
            lib.cuDeviceTotalMem_v2(ctypes.byref(total), device)
            entry.update(name=name.value.decode(errors="replace"), compute_capability=f"{major.value}.{minor.value}",
                         total_memory_bytes=total.value)
        result["devices"].append(entry)
    print(json.dumps(result, indent=1, sort_keys=True))
    return 0 if result["devices"] else 2


if __name__ == "__main__":
    sys.exit(main())
