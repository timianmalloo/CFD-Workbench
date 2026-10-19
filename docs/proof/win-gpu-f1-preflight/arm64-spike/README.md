---
id: proof-win-gpu-f1-arm64-spike
title: "Ruling 237 (5) spike: CUDA in WSL on the Windows ARM64 laptop"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, wsl, cuda, f1, spike]
links:
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-attempt2-offline-request, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  The read-only spike Ruling 237 (5) asked for before any packet change. On the Windows ARM64 laptop WSL 2 exposes the NVIDIA
  GPU: the WSL driver reports CUDA 13.4, and the arm64 CUDA 13.2 runtime from NVIDIA's sbsa repository completes a device
  round trip (Verified). The toolkit 13.2.2 and OpenFOAM 2512.0-2 exist for arm64. The spike does not refuse; what it did not
  run (nvcc, a PETSc-CUDA build) is listed. §5 adds the lock-resolution probe for the re-scope plan.
---

# Ruling 237 (5) spike: CUDA in WSL on the Windows ARM64 laptop

**Question (Ruling 237 (5)).** Whether CUDA-in-WSL and the PETSc-CUDA/OpenFOAM stack run on Windows ARM64 is unverified;
the plan starts with a read-only spike that establishes it. If the spike refuses, the track stops.

**Answer.** CUDA in WSL works on this host (Verified, two probes). The packaged stack exists for arm64 (Verified, repository
indexes). The spike does not refuse. Two things it did not run are named in §4; §5 adds the lock-resolution probe.

## 1. Host and method

- Windows 11 ARM64 laptop, NVIDIA RTX Spark N1X (6144-core Blackwell), driver 617.42 on the Windows side. The PC sole
  leader per Ruling 236; the machine's name and user paths are not in this proof.
- WSL 2, Ubuntu 24.04 (noble, the release the F1 packet pins), installed 2026-10-18 with the operator's go-ahead
  (`wsl --install -d Ubuntu-24.04 --no-launch`); every command ran as root with an explicit command line, so no first-run
  user was created. Kernel `6.18.54.1-microsoft-standard-WSL2`, `uname -m` = `aarch64`.
- Read-only: nothing was installed inside the distribution. Probe 2 fetched one `.deb` into a `/tmp` scratch directory,
  extracted it with `dpkg -x`, ran the probe, and removed the directory. Repository facts were read from the public index
  pages and apt `Packages` files over HTTP from the Windows host.
- Probes: `cuda_driver_probe.py` (driver API through ctypes, no toolkit) and `cuda_runtime_probe.py` (runtime API through
  ctypes against the extracted `libcudart`). Their receipts are the JSON files beside them.

## 2. Measurements

| Probe | What | Result | Receipt |
|---|---|---|---|
| WSL GPU surface | `/dev/dxg`, `/usr/lib/wsl/lib` | present; `libcuda.so.1`, `libnvidia-ml.so.1`, `nvidia-smi` and 18 more | `cuda-driver-probe.json` |
| `nvidia-smi` in WSL | driver, GPU | NVIDIA-SMI 615.78.05, KMD 617.42, CUDA UMD 13.4; GPU 0 RTX Spark N1X, 64000 MiB shown | this file (console) |
| Driver API | `cuInit`, `cuDriverGetVersion`, devices | `cuInit` 0; driver CUDA 13.4; 1 device, compute capability 12.1, 117,808,041,984 bytes | `cuda-driver-probe.json` |
| Runtime API | arm64 `libcudart.so.13.2.86` from `cuda-cudart-13-2_13.2.86-1_arm64.deb` (sbsa) | runtime 13.2; 1 device; `cudaMalloc` 1 MiB, `cudaMemset` 0x5A, `cudaMemcpy` back: 1,048,576 of 1,048,576 bytes match; every call 0 | `cuda-runtime-probe.json`, `cuda-runtime-source.json` |

Repository facts (`repository-evidence.json`, read 2026-10-18):

| Source | Finding |
|---|---|
| NVIDIA `wsl-ubuntu` repository | x86_64 only. The packet cannot use it on arm64 (it does not use it today either: it pins `ubuntu2404/x86_64`). |
| NVIDIA `ubuntu2404` repository | `sbsa/` carries the arm64 toolkit: `cuda-toolkit-13-2` 13.2.0-1, 13.2.1-1, **13.2.2-1** (the amd64 pin), `cuda-nvcc-13-2` up to 13.2.86-1. `arm64/` holds cuDNN and cuDSS only. |
| OpenFOAM `dl.openfoam.com` noble | Release architectures `all amd64 arm64`; `openfoam2512` and its `-common`, `-dev`, `-source`, `-tools` at **2512.0-2** for arm64 (and 2512.0-1). |

## 3. What this settles for the re-scope (Ruling 237 (1))

- The apt source is per architecture: `ubuntu2404/x86_64` on amd64, `ubuntu2404/sbsa` on arm64; the same toolkit version
  locks on both. The packet's `deb [arch=amd64 ...]` lines become `arch=<detected>` with the matching URI.
- OpenFOAM 2512.0-2 locks on both architectures from the same suite; the `.deb` hashes differ per architecture, as (1) says.
- The CUDA runtime that the toolkit ships (sbsa build) runs on the WSL driver on this chip; a context, allocation, device
  memset and copy work. The driver is newer (13.4) than the pinned toolkit (13.2), the supported direction.

## 4. Not run, and why

- **nvcc / a compiled kernel.** Needs the toolkit installed; the spike is read-only. The runtime's `cudaMemset` executes on
  the device, so a live context is Verified, but "nvcc targets this device (compute capability 12.1)" is **Inferred** from
  the 13.x toolkit's Blackwell support until the packet's own build runs.
- **PETSc with CUDA, petsc4foam, the solve.** Source builds that the packet performs; architecture-neutral in source, not
  proven here. The first packet run on arm64 proves them, under the re-scope's own two-cycle cap.
- **Compute capability 12.1 against the toolkit's `sm_` list.** Not read; recorded as the first thing the packet's nvcc step
  prints.

## 5. Added for the re-scope plan (2026-10-19): does the amd64 lock resolve on arm64?

`lock_resolution_probe.py` reads the arm64 binary indices the re-scoped packet would use (Ubuntu ports `noble`,
`noble-updates`, `noble-security`, main and universe; NVIDIA `ubuntu2404/sbsa`; OpenFOAM `noble`) over HTTP, unsigned,
nothing installed, and looks up every row of the frozen 135-row B2 amd64 lock and the 14 F1 extra roots.
`lock-resolution-arm64.json` is its receipt (each index with its SHA-256 and entry count; every row with the arm64
versions, filename, size and SHA-256). Result: 142 of 149 resolve at the locked version; `cpp-13`, `gcc-13` and
`g++-13` `-x86-64-linux-gnu` exist on arm64 only as cross packages (the native rows are `-aarch64-linux-gnu` at the same
version); `libquadmath0`, `cuda-nsight-13-2` and `cuda-opencl-13-2` are absent on arm64; `linux-libc-dev` is at
6.8.0-147.147, and the same read of amd64 `noble-updates` no longer carries the locked 146.146 either. What this
settles is in the plan (`docs/plans/f1-rescope-r237.md` §4.2) and the register entry LOCK-PINS-A-MOVING-SUITE.
The probe exits 3 when any locked version is missing, so its exit today is 3 by design, and the receipt is the result.

Two more receipts back the plan's §3, so no fact there rests on a session transcript: `sources_evidence_probe.py` →
`sources-evidence.json` (the six noble WSL rootfs `SHA256SUMS` lines with that file's own SHA-256, an unsigned read; and
the amd64 `noble-updates` main index's `linux-libc-dev` versions, where the B2-locked 6.8.0-146.146 is absent), and
`host_identity_probe.ps1` with `wsl_tools_check.sh` → `host-identity-evidence.pwsh.json` and
`host-identity-evidence.ps51.json` (the architecture identifiers under PowerShell 7 and 5.1, CPU and GPU names, the WSL
registrations with the profile prefix redacted, the stock distro's tools, keyrings, apt sources and architecture from
inside, and the Git-for-Windows `gpgv.exe` path and version). All read-only.

## 6. Files

- `cuda_driver_probe.py`, `cuda-driver-probe.json`: the driver-API probe and its receipt.
- `cuda_runtime_probe.py`, `cuda-runtime-probe.json`, `cuda-runtime-source.json`: the runtime round trip, its receipt, and
  the `.deb` it loaded (name, SHA-256, size, repository; extracted with `dpkg -x`, not installed).
- `repository-evidence.json`: the repository index facts and the URLs read.
- `lock_resolution_probe.py`, `lock-resolution-arm64.json`: the lock-resolution probe (§5) and its receipt.
- `sources_evidence_probe.py`, `sources-evidence.json`: the rootfs checksums per architecture and the amd64 index drift (§5).
- `host_identity_probe.ps1`, `wsl_tools_check.sh`, `host-identity-evidence.pwsh.json`, `host-identity-evidence.ps51.json`:
  the host-identity receipts under both PowerShell runtimes (§5).
