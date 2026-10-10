---
id: proof-win-gpu-g1
title: "Windows GPU G1 exact-source and toolchain inspection"
type: proof-pack
status: done
owner: "@win-gpu-qualification"
phase: implementation
tags: [proof, windows, wsl, openfoam, gpu, ruling-197]
links:
  - { to: proof-win-gpu-qualification, rel: follows }
  - { to: proof-win-l3-observer, rel: tested-by }
  - { to: plan-win-openfoam-gpu-qualification, rel: implements }
  - { to: review-pr-29, rel: depends-on }
review-by: "2026-11-10"
summary: >-
  Ruling 197 G1 finds a conditional partial v2512 offload path, but no complete discrete-RTX/WSL build contract; installed
  libraries are CPU-linked and F1 remains closed pending owner disposition of compiler, memory and dependency gaps.
---

# Windows GPU G1 exact-source and toolchain inspection

## Result

**Verified:** G1 completed the authorized read-only source and installed-library inspection. It did not install or build
software, run a solver, alter the OpenFOAM installation, or access the live L3 run. The installed-library child ran on
CPU 0 at effective nice 10 for 919 ms. The separate PC-coordinator observer remained active.

**Disposition:** OpenFOAM v2512 contains a conditional, partial GPU-offload path for expression evaluation. This machine's
installed v2512 libraries are GCC CPU builds with no GPU runtime linkage. The official v2512 source and official NVIDIA
documentation do not establish a complete recipe for the discrete RTX 3080 Ti Laptop GPU under WSL. The native-fused
route is therefore **not ready for F1**. G1 supplies the bill of materials and rejects the two unqualified adapter/fork
routes; it does not authorize an install, build, capability run or GPU trial.

## Captured identity and installed linkage

The official GitLab tag API returned protected tag `OpenFOAM-v2512` at commit
`87ed40d256d22ea38fcc648dfc82a22162427b18`. The official release archive was 54,700,141 bytes with SHA-256
`ae9a0a133a2e996b88bd1d0f3cc229e3c49968c368feae61bd3ec63deaf337aa`; its embedded build identity was
`_87ed40d256-20251219`. Source headers and the v2512 Umpire configuration identify the license as
**GPL-3.0-or-later**. The archive remains in the proof folder's ignored `.source-cache/` through owner review; its URL,
size and hash are retained in `source.json`.

The activated package is OpenFOAM v2512, `linux64GccDPInt32Opt`; Debian packages `openfoam2512:amd64` and
`openfoam2512-common` are both `2512.0-2`.

| Installed library | SHA-256 | Direct/runtime tree result |
|---|---|---|
| `libfusedFiniteVolume.so` | `fd4b339753873e87a9dc08fcec539303c20b815388ee42c23ee7e1c301f2ff36` | Links to OpenFOAM finite-volume/core libraries, GNU C++/math/runtime, OpenMPI and ordinary OS libraries. No CUDA, NVIDIA OpenMP, Umpire, PETSc or AmgX library appears. |
| `libOpenFOAM.so` | `94bf08c7912b44d70a6767ef9c820585b62869ea5ca54240dbb3f9132fe66cba` | Links to zlib, Pstream/OpenMPI, GNU C++/math/runtime and ordinary OS libraries. No CUDA, NVIDIA OpenMP, Umpire, PETSc or AmgX library appears. |

These `ldd` results prove the linkage of the installed bytes. They do not prove that every possible runtime plugin is
absent. The fused tutorial itself loads `fusedFiniteVolume` through `controlDict`, so `simpleFoam`'s direct dependencies
alone cannot settle plugin behavior.

## F1 bill of materials

| Item | Exact G1 evidence | F1 state |
|---|---|---|
| OpenFOAM source | Official v2512 tag/commit and archive hash above; GPL-3.0-or-later. | **Pinned.** |
| Offload backend | `ListExpression.H` uses `omp target teams distribute parallel for` when `_OPENMP` is defined and the list length exceeds 1,000; otherwise it can use `std::execution::par_unseq`. | **Verified partial backend.** OpenMP target and C++ standard parallelism are two conditional compilation paths, not proof of device execution. |
| OpenFOAM NVIDIA compiler rule | v2512 selects `nvc++$(COMPILER_VERSION) -std=c++17`; its OpenMP rule supplies `-fopenmp` and `-lnvomp`. | **Incomplete.** No exact NVIDIA HPC SDK version, CUDA toolkit, GPU architecture flag, target-offload flag or full compile/link command is pinned by v2512. |
| NVIDIA compiler contract | NVIDIA documents `-mp=gpu` for OpenMP GPU offload, `-stdpar=gpu` for parallel algorithms and `-gpu=ccXX` for a chosen architecture. It distinguishes separate, managed and unified memory modes. | **Candidate only.** These are vendor compiler capabilities, not a verified OpenFOAM v2512 recipe. F1 needs an exact version, hashes, license, flags and link closure. |
| Umpire allocation | v2512 pins `umpire-2025.03.0`. With `FOAM_USE_UMPIRE`, OpenFOAM can select host, device or managed pools; managed uses Umpire allocator `UM`. | **Conditional.** A pool selection establishes allocation behavior, not compute offload or accessibility of every referenced object. |
| WSL/discrete memory | NVIDIA documents limited managed-memory behavior on Windows/WSL, no full managed-memory support and no concurrent CPU/GPU access. | **Hard unresolved boundary.** The official v2512 note reports testing on unified-memory AMD and NVIDIA architectures; this machine is a discrete RTX 3080 Ti Laptop GPU under WSL. |
| `simpleFoam` reach | The official `pitzDaily_fused` tutorial loads `fusedFiniteVolume`, runs `simpleFoam`, selects `fusedGauss` schemes, and its pressure equation calls `fvm::laplacian`. The scalar-gamma fused Laplacian specializes into expression assembly; expression assignment can reach the OpenMP-target evaluator. | **Verified conditional partial reach.** This does not offload the linear solve or prove that all fused gradient/divergence work reaches a device. |
| Installed runtime | Both required `ldd` trees and library hashes are captured above. | **CPU-only linkage observed.** A fresh isolated build would be required for any candidate. |
| `petsc4Foam` contract | The exact v2512 archive includes the GPL-3.0-or-later external-solver module. Its README requires OpenFOAM v1912+ and PETSc 3.10+, optionally Hypre; it converts the OpenFOAM LDU matrix for PETSc, loads `petscFoam`, and selects `solver petsc`/`preconditioner petsc` per equation. | **Integration point verified.** This substitutes linear algebra; it does not offload the whole solver. PETSc/CUDA/AmgX versions, hashes, precision/index/MPI settings and WSL support remain unpinned. |
| `amgx4Foam` | Its build options require separate AmgX and `foam2csr` dependencies and hard-code `/usr/local/cuda-11.6/include`; its repository provides no pinned v2512 release/build contract. | **Rejected for F1.** This is an unqualified extra adapter route. The evidence does not claim that adaptation is impossible. |
| RapidCFD | Its own source identifies a separate `RapidCFD` development fork and NVCC compiler; its README targets Ubuntu 16.04 and CUDA 8. | **Rejected for F1.** It leaves the pinned v2512 source/runtime contract and would require a separate port and numerical qualification. |

## Source trace

Primary sources retrieved 2026-10-10:

- [OpenFOAM v2512 protected tag API](https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tags/OpenFOAM-v2512)
  and [official archive directory](https://dl.openfoam.com/source/v2512/);
- [expression evaluator and OpenMP target directive](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/src/OpenFOAM/expressionTemplates/ListExpression.H),
  [NVIDIA compiler rule](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/wmake/rules/General/Nvidia/c++),
  and [NVIDIA OpenMP rule](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/wmake/rules/General/Nvidia/openmp);
- official fused tutorial [controlDict](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/tutorials/incompressible/simpleFoam/pitzDaily_fused/system/controlDict),
  [fvSchemes](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/tutorials/incompressible/simpleFoam/pitzDaily_fused/system/fvSchemes),
  [`simpleFoam` pressure equation](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/applications/solvers/incompressible/simpleFoam/pEqn.H),
  and [fused Laplacian specialization](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/src/fused/finiteVolume/fusedGaussLaplacianSchemes.C);
- [v2512 Umpire version](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/etc/config.sh/umpire)
  and [OpenFOAM memory-pool implementation](https://gitlab.com/openfoam/core/openfoam/-/raw/OpenFOAM-v2512/src/OSspecific/POSIX/memory/MemoryPool.cxx);
- [NVIDIA HPC compiler guide](https://docs.nvidia.com/hpc-sdk/compilers/hpc-compilers-user-guide/index.html)
  and [CUDA on WSL limitations](https://docs.nvidia.com/cuda/archive/13.0.2/wsl-user-guide/index.html);
- the exact v2512 archive's `modules/external-solver/README.md` and `doc/README.md`, cross-checked against the
  [official external-solver repository](https://gitlab.com/openfoam/modules/external-solver/-/raw/main/README.md),
  plus PETSc's [GPU installation contract](https://petsc.org/main/install/install/) and
  [`PCAMGX` contract](https://petsc.org/release/manualpages/PC/PCAMGX/);
- [amgx4Foam build options](https://raw.githubusercontent.com/maorz1998/amgx4foam/main/src/amgx4Foam/Make/options)
  and RapidCFD's [project identity](https://raw.githubusercontent.com/SimFlowCFD/RapidCFD-dev/master/etc/bashrc) and
  [build instructions](https://raw.githubusercontent.com/SimFlowCFD/RapidCFD-dev/master/README.md).

## Observation window

`observer-window.json` binds eight append-only rows at samples-file SHA-256
`f9c174dd595ee97d3e0ada37396971f898426660fbe9c30d9d180f683addeade`, the final capture SHA-256, and the observer
script SHA-256 `188ad0675c97ab6295c3b8c17f39feccc1f5cfb274628b11f9da671085cf0cb1`. Every row names the same run and records
the unit as active. The capture UTC bounds identify the interval containing the probe; adjacent 30-minute windows use
the monotonic clock because WSL UTC moved relative to `/proc/uptime` during the observation.

| Window | Valid 600-second intervals | Median iterations/s |
|---|---:|---:|
| Before | 2 | 0.7916 |
| During | 1 | 0.7733 |
| After | 2 | 0.7908 |

The during median was **2.32% below** the before median. The 5% slowdown threshold did **not** fire. The short 175.24-s
interval created when the bounded observer was restarted is retained but excluded from every median. One older valid
interval falls outside the monotonic 30-minute window and is also retained as outside-window. No GPU-track process was
running when the threshold result became available.

This is an observed association over 600-second intervals. The 919-ms linkage probe occupies only a small part of its
containing interval, so the result neither attributes the difference to G1 nor proves zero impact.

## Repair ledger and limits

G1 used both allowed capture cycles:

1. The first WSL child stopped before either `ldd` because Bash `errexit` was inherited while sourcing OpenFOAM's vendor
   activation script; Bash reported `pop_var_context` at `etc/config.sh/setup:207`. The official source download and hash
   completed, but no `capture.json` was admitted from that stopped attempt.
2. The committed repair applies the repository's established activation pattern: suspend strict flags only around the
   vendor `source`, capture its exit, restore strict flags, and stop on a nonzero activation result. The second child
   exited 0 and produced both required linkage trees.

The repair cap is **2 of 2**. No further capture rerun is authorized on this track. The retained final files overwrite
the same named transient outputs from attempt 1; the repair class and boundary remain in this receipt.

## Owner decision requested

Do not authorize the native-fused G2 build from this evidence. Fable may either close that route on this WSL/discrete
machine, or require a separately reviewed, exact recipe that resolves compiler version and hashes, CUDA/toolkit and
Umpire versions, complete flags, object accessibility and synchronization, isolation, device-work telemetry and a
numerical oracle. If the operator still prioritizes GPU acceleration, a separate F1 proposal may pin the exact v2512
`petsc4Foam` module plus PETSc's CUDA/AmgX stack; this G1 does not yet provide that dependency closure and therefore
does not request its installation or build.
