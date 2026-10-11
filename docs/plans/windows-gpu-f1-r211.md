---
id: plan-windows-gpu-f1-r211
title: "F1 design for PETSc CUDA and petsc4Foam on Windows WSL"
type: plan
status: proposed
owner: "@pc-f1-author-r211"
phase: design
tags: [windows, wsl, gpu, cuda, petsc, openfoam, ruling-211]
links:
  - { to: coordination-windows-gpu-f1-design, rel: implements }
  - { to: proof-win-gpu-b1-r199, rel: depends-on }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: proof-win-gpu-g1, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-18"
summary: >-
  One gated PETSc-CUDA plus petsc4Foam candidate, with exact build sequence, WSL memory measurement,
  one-case CPU/GPU oracle, and owned observer/rollback. Flagged input and ABI gaps block execution.
---

# F1: PETSc CUDA plus petsc4Foam, design only

## Authority, result, and decision boundary

**Proposed; execution blocked.** Ruling 211 permits this design and read-only metadata only. Its exact boundary is:
“No install, build or download of build inputs beyond read-only metadata until a second operator ruling approves the
plan.” This document authorizes none of those actions. The second ruling must name this revision, a resolved lock,
ceilings, measurement plan, and exact first executable stage. Preserve `cfdw-openfoam2512`. Ruling 199 closed native
fused/offload on this host. This proposal changes the OpenFOAM **linear algebra used for pressure**, not the whole CFD
solver or application backend. [Rulings 199 and 211](../notes/rulings.md).

The selected route is **one** serial `simpleFoam` process in a new Ubuntu 24.04 WSL distro, using the installed
`openfoam2512=2512.0-2` runtime plus its matching development package, PETSc 3.26.0 built with CUDA 13.2, and the
PETSc-maintained `petsc4foam` commit `cb1badc4978306e7001a4654dcdf53f284a92365`. The CPU and GPU arms use the
same adapter and pressure KSP; only PETSc's matrix type differs. U and nuTilda retain the same OpenFOAM solvers.
One GPU and one process avoid an unproven GPU-aware MPI contract. **Flagged:** source-level support does not prove
that the fork builds against the exact `2512.0-2` development package, that its `VecPlaceArray` path works with a
CUDA vector on this PETSc release, or that the matrix type remains on device through repeated updates. These are
hard qualification gates before any benchmark. [Fork source](#sources-and-confidence).

The decision constrains these surfaces: immutable WSL image and package state → PETSc ABI → adapter build and
pressure solver selection → PETSc/device telemetry → copied case inputs → CPU/GPU comparison → L3 observer and
owned stop. There is no product UI or persistent application data change. The only proposed outputs are a
manifest-bound qualification receipt and a pass/fail benchmark receipt after authorization.

## Dependency and ABI bill of materials

| Component | Exact selection and provenance | Integrity, licence, and closure |
|---|---|---|
| Host | Prior B1 probe: RTX 3080 Ti Laptop, compute capability 8.6, Windows driver 596.47, CUDA ceiling 13.2; Ubuntu 24.04.5 in WSL. Prior B2: `concurrent_managed_access=0`. | **Verified for those capture times**, not a guarantee at execution. Recheck driver, GPU, WSL, free space and device attribute before work; mismatch stops. [B1](../proof/win-gpu-b1/receipt.md), [B2](../proof/win-gpu-b2/receipt.md). |
| Fresh WSL root | Ubuntu Noble WSL amd64 rootfs, `ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz`, 356,739,129 bytes, SHA-256 `8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea`, as B2 captured. | Canonical `SHA256SUMS` plus detached `.gpg` and Ubuntu signing-key verification precede import. URL has mutable `current`; **Flagged** if bytes or signature differ. B2 [source record](../proof/win-gpu-b2/source-downloads.json) and [verification receipt](../proof/win-gpu-b2/receipt.md). Ubuntu image package licences are individual package notices. |
| OpenFOAM | `openfoam2512=2512.0-2`, `openfoam2512-common=2512.0-2`, `openfoam2512-dev=2512.0-2` from the publisher's Noble repository. `WM_OPTIONS=linux64GccDPInt32Opt`; GCC/OpenMPI ABI must match installed runtime. | B1 observed runtime and dev candidate but not the dev bytes or dependency lock. **Flagged** package SHA-256, signed repository index, dependencies, licence notices, installed size, and whether `wmake`/required headers are present after the dev install. Stop before install until exact amd64 lock and signed index are frozen. OpenFOAM source is GPL-3.0-or-later. [B1](../proof/win-gpu-b1/receipt.md), [official v2512](https://www.openfoam.com/news/main-news/openfoam-v2512), [publisher package directory](https://dl.openfoam.com/repos/deb/dists/noble/main/pool/2512_0/binary-amd64/). |
| CUDA | `cuda-toolkit-13-2=13.2.2-1`, nvcc 13.2.86; **reuse the exact 135-package amd64 B2 lock**, with recommendations excluded. No Linux driver/meta-package. | 3,405,443,104 download bytes and 8,092,712,328 installed KiB-derived payload bytes in the accepted lock. Verify NVIDIA `InRelease`, each package SHA-256 and licence notice using B2 [lock](../proof/win-gpu-b2/package-lock.json), [licence map](../proof/win-gpu-b2/license-map.json), and [source verification](../proof/win-gpu-b2/receipt.md). Re-resolve if the rootfs differs; no silent apt substitution. NVIDIA CUDA Toolkit EULA applies. WSL uses the Windows host driver; the Linux driver must not be installed. [NVIDIA WSL guide](https://docs.nvidia.com/cuda/archive/13.2.1/wsl-user-guide/index.html). |
| PETSc | `petsc-3.26.0.tar.gz`, 16,933,940 bytes, SHA-256 `f5230023e6e22ee607802a13c82bc25f3d81e71654ad386a5b9bdff17fed93df` as B1 measured from official PETSc download. Build in-place at `/opt/cfdw-f1/petsc-3.26.0`, `PETSC_ARCH=arch-linux-cuda132-opt`, real double scalar, 32-bit indices, shared libraries, optimized, logging on, CUDA architecture 86. | SHA-256 is a **measured artifact hash**, not an upstream detached signature. **Flagged:** authenticated upstream checksum/signature or an explicitly accepted hash-provenance rule before download. PETSc is BSD-2-Clause; optional `--download-*` packages are excluded. [B1](../proof/win-gpu-b1/receipt.md), [official download](https://petsc.org/release/install/download/), [licence](https://petsc.org/release/install/license/), [configure](https://petsc.org/release/install/install/). |
| petsc4Foam | PETSc group fork commit `cb1badc4978306e7001a4654dcdf53f284a92365`, Git object id, from `https://gitlab.com/petsc/petsc4foam.git`. Build only its `Allwmake` against the installed OpenFOAM runtime/dev and the in-place PETSc. | Commit and source path are fixed; no moving `main`. GitLab API metadata identifies the commit. **Flagged:** no independently published signed tag/archive digest, and exact v2512 build compatibility is untested. Require reviewed commit identity and post-fetch tree hash before build. GPL-3.0-or-later. [commit API](https://gitlab.com/api/v4/projects/petsc%2Fpetsc4foam/repository/commits/cb1badc4978306e7001a4654dcdf53f284a92365), [README](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/README.md), [build options](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/Make/options). |
| Build dependencies | Same Ubuntu Noble GCC 13 toolchain and OpenMPI used by OpenFOAM. Use the **system OpenMPI compiler wrappers** (`mpicc`, `mpicxx`, `mpif90`) and system BLAS/LAPACK, not PETSc-downloaded MPI or BLAS. Disable Hypre/AmgX/Scotch additions. | **Flagged:** exact versions, transitive amd64 package lock, licences, ABI of OpenFOAM's MPI and BLAS provider, and PETSc configure detection. A 135-package CUDA lock alone is insufficient. Read-only apt metadata plus signed index, `dpkg`/`ldd`/`mpicc --showme` after authorized import, and a PETSc configure report must close this gate. [PETSc compiler/MPI/BLAS contracts](https://petsc.org/release/install/install/). |

The chosen ABI is one process, one MPI rank, `OMP_NUM_THREADS=1`, GCC/G++/gfortran 13 via the installed OpenMPI
wrappers, C++17/OpenFOAM `linux64GccDPInt32Opt`, real IEEE float64 `PetscScalar`, 32-bit `PetscInt`, system BLAS/LAPACK,
CUDA sm_86, PETSc in-place shared-library build. No `--download-*` configure flag is allowed; those would introduce
unpinned build inputs and licences. The 32-bit index choice matches OpenFOAM's `Int32` and PETSc's cuSPARSE matrix
internal indices. [G1 installed ABI](../proof/win-gpu-g1/receipt.md), [cuSPARSE matrix contract](https://petsc.org/release/manualpages/Mat/MATSEQAIJCUSPARSE/).

**Flagged compatibility gate:** PETSc documents WSL CUDA builds as experimental and outside its tested configurations.
The fork's pressure path creates a PETSc matrix and vectors, sets matrix type from options, places OpenFOAM host arrays
into vectors, then invokes `KSPSolve`; its matrix update writes host AIJ arrays. The candidate `aijcusparse` option
therefore does not itself prove a device solve or correct vector transfer. Build, `-ksp_view`, PETSc log, CUDA trace,
and a numerical pressure solve must establish the actual path before any speed claim. [PETSc WSL note](https://petsc.org/release/install/install/), [fork solver source](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/solvers/petscSolver.C), [CUDA vector](https://petsc.org/release/manualpages/Vec/VECCUDA/).

## Capacity, identity, and ordered execution proposal

The B2 base recipe required 25,456,799,144 free bytes, comprising 3,405,443,104 download + 8,092,712,328
package payload + 2,147,483,648 unpack reserve + 1,073,741,824 build reserve + 10,737,418,240 headroom. It is
**not** the F1 total. For F1, calculate `required_free = B2 base + newly locked OpenFOAM/dependency download bytes +
their installed KiB × 1024 + PETSc/fork source bytes + measured build-output reserve + case copies + 10 GiB headroom`.
Count only packages absent from the rootfs to avoid double count. Also require Windows C: free bytes greater than the
maximum planned VHD growth plus 10 GiB headroom; a Linux free-space check alone is insufficient for WSL. B2 measured
~5.18 GB VHD after CUDA install, but that is an earlier run, not F1 capacity. **Flagged:** PETSc/fork build sizes,
OpenFOAM-dev closure and final VHD estimate have no measured metadata yet. Freeze a source- and apt-metadata worksheet,
then stop if either host or guest threshold fails. [B2 capacity](../proof/win-gpu-b2/capacity-linux.json), [host capacity](../proof/win-gpu-b2/capacity-host.json).

The following is the **one proposed order**, to be transcribed into a reviewable wrapper after the blockers above are
closed and a second operator ruling opens the exact stage. These commands are not run by this design track. Any
unexpected package, changed hash, nonzero exit, warning about ABI mismatch, or missing measurement is a stop, not a
reason for `apt upgrade` or a replacement package. `F1-ROOTFS`, `F1-LOCK`, and `F1-SOURCES` denote reviewed
immutable files whose SHA-256 and absolute paths must be entered in the second ruling; no path is inferred from a
user home directory.

1. Freeze a fresh named distro `cfdw-petsc-cuda132-f1` at a dedicated Windows install path. Before import, record
   `wsl.exe --list --verbose`, the protected distro's immutable terminal projection, disk free bytes, driver identity,
   an exact-name absence check, and SHA-256/signature verification of `F1-ROOTFS`. Abort on collision. After approval:
   `wsl.exe --import cfdw-petsc-cuda132-f1 C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1 F1-ROOTFS --version 2`.
   The import path must be absent before this command. Never clone, export, or mount `cfdw-openfoam2512`.
2. In only the new distro, set `DEBIAN_FRONTEND=noninteractive`, validate `/etc/os-release` Noble amd64, recheck
   `/usr/lib/wsl/lib/nvidia-smi`, device attributes, 13.2 driver ceiling, and source manifests. Apply the pinned
   NVIDIA and Ubuntu signed repository indices and exact package lock using B2's verified source/lock scripts. Install
   the fully frozen OpenFOAM-dev and build dependency extension with `apt-get install --no-install-recommends` and
   `name=version` arguments generated from the *reviewed* F1 lock. Simulate first; require zero removals and exactly
   the expected install/upgrade set. Forbid `cuda`, `cuda-drivers`, `nvidia-driver-*`, and a Linux display driver.
3. Activate `/usr/lib/openfoam/openfoam2512/etc/bashrc` in a clean noninteractive shell; record `WM_OPTIONS`,
   `WM_MPLIB`, `FOAM_MPI`, `mpicc --showme`, `mpicxx --showme`, OpenFOAM and MPI linked-library identities, and
   `wmake` plus required headers. Stop unless the values match the ABI row and frozen lock. Place the verified PETSc
   source at `/opt/cfdw-f1/petsc-3.26.0`; no build input may be fetched by configure.
4. In that source root, run exactly one PETSc configuration, with the option spelling first checked against that
   archive's `./configure --help` and the resulting `configure.log` retained:

   ```sh
   export PETSC_DIR=/opt/cfdw-f1/petsc-3.26.0 PETSC_ARCH=arch-linux-cuda132-opt
   export PATH=/usr/local/cuda-13.2/bin:$PATH
   ./configure --with-cc=mpicc --with-cxx=mpicxx --with-fc=mpif90 \
     --with-cuda=1 --with-cuda-dir=/usr/local/cuda-13.2 --with-cuda-arch=86 \
     --with-precision=double --with-scalar-type=real --with-64-bit-indices=0 \
     --with-debugging=0 --with-shared-libraries=1 --with-make-np=2
   make PETSC_DIR="$PETSC_DIR" PETSC_ARCH="$PETSC_ARCH" -j2 all
   make PETSC_DIR="$PETSC_DIR" PETSC_ARCH="$PETSC_ARCH" check
   ```

   **Flagged:** `--with-cuda-arch=86` and `--with-precision=double` are proposed spellings; this exact tarball's
   help/configure must verify them before the second ruling accepts the command. `configure` must report the selected
   MPI, BLAS/LAPACK, float64, index width, CUDA 13.2, and sm_86; no silent fallback or network access is admitted.
   PETSc tests and a CUDA matrix/vector smoke must pass before the adapter build. Cap the build at two cores,
   effective nice 10, 12 GiB RSS, 4 hours wall, and one repair cycle followed by stop at the second failure.
5. Check out only the pinned `petsc4foam` commit and verify `git rev-parse HEAD` plus a frozen source tree digest.
   Set `PETSC_DIR`, `PETSC_ARCH`, and the PETSc library path, activate the same OpenFOAM runtime, and run
   `./Allwmake -j2 -prefix=/opt/cfdw-f1/openfoam-module`. Require `foamHasLibrary -verbose petscFoam`, matching
   MPI/libstdc++/PETSc dynamic linkage, and no load from the protected distro. **Flagged:** whether the prefixed
   `Allwmake` syntax and module-path config resolve correctly against this exact package must be checked in a
   no-run build-stage preflight and by the actual build. [fork Allwmake](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/Allwmake).
6. Only after those gates, copy one frozen case into a new directory inside the disposable distro, perform a pressure
   solve qualification, then the benchmark below. Keep raw stdout/stderr, exact argv, package list, binary hashes,
   configure summary, PETSc version/view, CUDA trace, sampler rows, checksums, and numeric comparison in a
   manifest-bound receipt. Preserve raw failure evidence before stopping the owned process group.

The rollback is `wsl.exe --terminate cfdw-petsc-cuda132-f1`, verify the exact owned process group is gone, retain and
commit a manifest-bound proof, recheck the protected distro projection, then, only after a separate cleanup decision,
`wsl.exe --unregister cfdw-petsc-cuda132-f1`. Verify the registration and dedicated install directory are absent and
the protected distro still exists with its before-state hash. Any uncertainty leaves the disposable distro stopped and
retained; it never triggers a wider delete. [B2 rollback precedent](../proof/win-gpu-b2/receipt.md), [Windows proof runbook](../../.claude/knowledge/windows-proof-runbook.md).

## Managed-memory and transfer measurement, declared before use

The operator questions are: Does the pressure solve really use the GPU? Where does its elapsed time go? What H2D,
D2H, or migration bytes and stall time occur? Does repeated conversion or synchronization erase the kernel gain?
Does memory/RSS grow? Each arm emits on its normal path; a trace run is separately labelled as a diagnostic whose
overhead bars it from the benchmark time statistic.

| Quantity (one pressure solve, plus sum per CFD run) | Source and accounting rule | Missing-data rule |
|---|---|---|
| Allocation | CUDA API allocation calls from Nsight Systems CUDA trace, plus PETSc memory log; classify host/device/managed allocation and bytes. | `not recorded` if API trace or allocator identity unavailable. Do not infer managed allocation from a CUDA build. |
| Preparation | Monotonic host spans from entry to adapter context/preconditioner preparation and each matrix assembly; fork's `foam_p_mat` PETSc stage is an outer check. | Instrumentation patch or exact source event mapping is a **Flagged prerequisite**; do not subtract unrelated PETSc events as a proxy. |
| H2D/D2H and migration | Nsight Systems CUDA memcpy and Unified Memory transfer trace, direction, bytes, count, duration, within a bound pressure-solve NVTX range; mark migration separately from explicit copies. | If WSL/profiler omits a channel, state `not recorded` and block the migration-cost conclusion. Zero is valid only with a working channel and zero events. |
| Matrix conversion | Host span around LDU-to-CSR/preallocation and update, separate from PETSc host-to-device migration; retain matrix update count and bytes. | Missing span blocks attribution. |
| GPU solve | CUDA kernel intervals and PETSc `foam_p_ksp`/KSP stage; record KSP iterations, residual, matrix/vector types, GPU device id and nonzero CUDA kernel count. | No observed device kernels or missing type proof = **GPU arm invalid**. PETSc `-log_view_gpu_time` is diagnostic because it can slow the solve. |
| Synchronization | CUDA API sync calls and elapsed host waits, with location relative to KSP and field update. | Missing sync trace blocks attribution; do not treat a kernel event time as total solve. |
| Total wall | One host monotonic span from immediately before `simpleFoam` child launch to after its reaped exit; separately record case setup and trace overhead. | Clock reset, unmatched process, or missing exit = invalid trial. |

Run a standalone pressure qualification and one traced CPU/GPU warm-up on the frozen case after authorization.
Validate trace coverage with a known transfer and known synchronization event; a missing expected event fails the
measurement path. Collect PETSc `-log_view` and optional GPU time in the diagnostic pass, but the timed pass uses
only normal-path lightweight spans and the observer. Report exclusive durations **only** for nonoverlapping intervals;
GPU kernels, API waits, and migration can overlap, so sum them as attributed activity, never as a partition of wall.
Use exact source and profiler versions in the receipt. NVIDIA documents limited WSL managed memory, migration on
device execution and return after synchronization, and possible high RAM use; B2 observed the device attribute zero.
[NVIDIA CUDA memory guide](https://docs.nvidia.com/cuda/cuda-programming-guide/02-basics/understanding-memory.html),
[WSL limits](https://docs.nvidia.com/cuda/archive/13.2.1/wsl-user-guide/index.html),
[Nsight trace contract](https://docs.nvidia.com/nsight-systems/UserGuide/index.html),
[PETSc logging](https://petsc.org/release/manualpages/Log/PetscLogView/).

**Flagged measurement gate:** Nsight Systems trace support and availability of Unified Memory events in this WSL
build have not been observed. The lock includes `cuda-nsight-systems-13-2`, but a package name is not evidence of
trace coverage. The adapter has only coarse PETSc stages; precise allocation/preparation/conversion/synchronization
spans require a reviewed, source-bound instrumentation patch or a demonstrated profiler mapping. No cost claim is
admitted until the named channels emit and the team reads a complete sample back.

## One real-case CPU versus GPU pass/fail contract

**Case:** `cases/win-spike04r3-g0-l6.yaml`, the Windows/WSL NACA 0012 L6 `simpleFoam` case. Its prior CPU proof
met A4 after 7,577 iterations; it is a diagnostic case, not a grid-family validation or product accuracy claim.
[Case](../../cases/win-spike04r3-g0-l6.yaml), [prior receipt](../proof/win-naca/receipt.md),
[input manifest](../proof/win-naca/l6-inputs/cfdw-manifest.json). After authorization, copy exactly that input tree
and verify all manifest hashes, mesh topology, initial and boundary fields, schemes, turbulence, decomposition and
reference geometry. Alter only `controlDict` to a fixed 8,000 iterations with no early A4 stop and `fvSolution`'s
pressure entry plus the load of `libpetscFoam.so`. Freeze the two derived dictionary hashes before the first trial.
Use one rank, one host thread, one GPU, same PETSc build, source, compiler, CPU affinity, nice 10, pressure `ksp_type
cg`, `pc_type jacobi`, absolute tolerance `1e-14`, relative tolerance `0.05`, same initial guess and matrix/preconditioner
update policy `always`. CPU uses `mat_type aij`; GPU uses `mat_type aijcusparse`. U and nuTilda remain the original
`smoothSolver` settings. No Hypre, mixed precision, or algorithm switch. The fork's options are equation-prefixed,
and its example uses `mat_type "aijcusparse"`; dump the effective KSP/PC/Mat/Vec types before trial admission.
**Flagged:** the exact vector type and `VecPlaceArray` CUDA behavior must be proven in qualification. If CPU and GPU
cannot use the same numerical contract, this comparison stops rather than retuning one arm.
[fork option example](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/tutorials/basic/laplacianFoam/pipeOneD/system/fvSolution-petsc-gamg-device),
[fork solver](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/solvers/petscSolver.C).

Qualify with one untimed warm-up per arm. Then schedule **five trials per arm** in fixed ABBAA/B AABBA order
(`CPU,GPU,GPU,CPU,CPU` then `GPU,CPU,CPU,GPU,GPU`); each arm gets five trials, and each starts from a fresh copy of
the exact frozen 0/constant/system inputs. Do not reuse a converged field, PETSc cache, or output directory. At most
one environmental invalidation may be replaced by its matching pair, using the same order; a second invalidation
stops the benchmark. The two-cycle repair cap still applies to code/build defects and is not a retry budget for
benchmark selection. Run neither arm concurrently with the other. Record all trials, including invalid ones.

Each valid trial must exit zero at exactly 8,000 iterations, with finite fields and coefficients, no solver divergence
or bounding nuTilda in the final 2,000 iterations, and meet the existing A4 residual/Cl/Cd half-band limits at that
fixed endpoint: every equation's final two initial residuals at most `1e-3` of iteration 1; Cl half-band at most
`1e-5`; Cd half-band at most `1e-6`. A run reaching 8,000 without A4 is **invalid**, not a slow data point.
Compare CPU/GPU final cell values after aligning mesh/cell order and subtracting each pressure field's volume-weighted
mean: every U component, p, and nuTilda cell must satisfy
`abs(gpu-cpu) <= 1e-8 + 1e-5*max(abs(cpu),abs(gpu))`. The final 2,000-iteration means must differ by at most `2e-5`
for Cl and `2e-6` for Cd. These are **predeclared F1 acceptance tolerances**, not prior-run measurements. A CFD owner
must accept their adequacy before the execution ruling; otherwise the oracle is Flagged and execution stays closed.

Primary performance metric: median of five valid **end-to-end solver-child monotonic wall seconds** for each arm;
`speedup = median(CPU wall)/median(GPU wall)`. **Worthwhile** is `speedup >= 1.20` with all numerical and guard gates
passing, all five accepted trials per arm measured completely, and GPU memory within the ceiling. Invalidated trials
and any replacement pair remain visible in the receipt. Also report per-solve pressure KSP
median/p95, iteration counts, matrix conversion, transfer/migration, kernel and sync time, CPU RSS, device memory,
and the complete trial distributions. A KSP-only gain does not count as an F1 win. A GPU arm that executes no observed
device kernels fails GPU qualification even if wall time is lower. No post-run threshold change is allowed.

## Observer, ownership, stop ceilings, and rollback proposal

The prior B2 run found the protected L3 unit **completed**, so the proposed default is its completed-state guard:
record a canonical before projection of unit state, final iteration, end markers, log size/mtime/tail/full hashes;
sample every 5 seconds throughout import/install/build/qualification/benchmark; require each new sample to preserve
that projection; and compare a final full projection. [B2 guard evidence](../proof/win-gpu-b2/receipt.md).
The observer is a separately owned process, started before the target and stopped/reaped after final projection.
Each row records UTC, monotonic timestamp, observer source hash, run id, read bounds, freshness, and decision.
Bind the exact N-row append-only sample prefix in the receipt. The target launcher owns a fresh Linux session/process
group; acknowledge `pid==pgid`, exact cmdline and distro before work. On any stop, signal and verify **only that group**,
then recheck the protected snapshot. A WSL launcher exit alone is not Linux-child completion.
[Windows proof runbook](../../.claude/knowledge/windows-proof-runbook.md).

If L3 becomes live before a later run, the completed-state contract is invalid: stop before heavy work and obtain a
fresh operator ruling for a live observer. Proposed live ceiling is a 5% iteration-rate drop evaluated on **each new
valid row**, with both UTC and monotonic rates reported and a predeclared fresh baseline. Mismatched run identity,
counter reset, stale/partial reads, nonpositive elapsed time, missing or invalid baseline, or a missing observer row
means **stop**, never “no impact.” Clock domains are not interchangeable; prior G1 found `/proc/uptime` and UTC rates
different. [Ruling 199](../notes/rulings.md), [G1 clock result](../proof/win-gpu-g1/receipt.md).

Additional proposed ceilings are two bound CPU cores under nice 10, 12 GiB build RSS, 4 hours build wall, 8,000
iterations and 2 hours per solver trial, one GPU, 80% of sampled GPU memory or host RAM (whichever is reached first),
and the frozen free-space requirement above. Missing GPU memory utilization under WSL is `not recorded` and blocks
continued heavy work unless a verified alternate device-memory channel exists; NVIDIA warns NVML utilization queries
may be unsupported in WSL. A stopped observer, any guard mismatch, unmet capacity, unowned process, unsupported CUDA
trace, solver divergence, package drift, or timeout stops the target group and preserves evidence. Thermal/power
readings are recorded when available and `not recorded` otherwise; no synthetic safe value is emitted.
[NVIDIA WSL limits](https://docs.nvidia.com/cuda/archive/13.2.1/wsl-user-guide/index.html).

## Blocking ledger and second-ruling input

| ID | Flagged fact that blocks the next stage | Closure evidence before the second ruling or named later gate |
|---|---|---|
| F1-1 | Fresh rootfs and CUDA lock availability may drift; OpenFOAM-dev and system toolchain/MPI/BLAS lock is incomplete. | Signed Ubuntu/OpenFOAM/NVIDIA metadata, exact amd64 name/version/hash/download/install bytes and licence notice per package; zero-removal apt simulation against the exact rootfs. **Before install ruling.** |
| F1-2 | PETSc tarball has measured SHA-256 but no authenticated upstream signature established; fork commit has no detached signature/digest established. | Operator accepts a stated integrity basis or obtains authenticated upstream digest/signature, and records exact source/tree hashes. **Before source download ruling.** |
| F1-3 | Total F1 installed/build/VHD sizes and live free space are not established. | Complete worksheet using this formula, fresh host/guest free-space measurements, and reviewed 10 GiB headroom. **Before import/install ruling.** |
| F1-4 | PETSc configure flag spelling, OpenFOAM `2512.0-2` header/wmake/MPI/BLAS ABI, fork build/link and CUDA Vec/Mat behavior are untested. | Source-bound configure-help and ABI preflight; authorized build, load, type dump, CUDA kernel and pressure numeric qualification. **Before case benchmark.** No performance claim if this gate fails. |
| F1-5 | WSL profiler's managed-migration coverage and adapter phase spans are unobserved. | Traced known transfer/sync, source-bound phase instrumentation, and read-back sample showing all named channels or explicit unavailable state. **Before cost conclusion or benchmark admission.** |
| F1-6 | CFD owner has not accepted F1 field/force tolerance; L3 may change state. | Owner acceptance of the exact oracle and fresh observer state/ruling. **Before case execution.** |

The second operator ruling can approve only a **closed, exact first stage**. If any prerequisite for that stage remains
Flagged, keep it closed. Build and benchmark are separate gates even if source acquisition is approved. A successful
F1 benchmark would establish only this one host, case, pressure solver path and build; macOS and the application
backend remain separate decisions.

## Sources and confidence

Primary external sources were accessed **2026-10-11** as read-only web/GitLab API metadata; the links below name
what they prove, not a runtime result. Repository proof receipts are observations at their recorded capture times.

| Exact upstream URL | What the source establishes |
|---|---|
| https://gitlab.com/api/v4/projects/petsc%2Fpetsc4foam/repository/commits/cb1badc4978306e7001a4654dcdf53f284a92365 | Fixed fork commit identity and date. |
| https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/README.md | Fork requirements (OpenFOAM v1912+, PETSc 3.23+), GPL licence, build and per-equation options. |
| https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/solvers/petscSolver.C | Mat/Vec creation, option prefix, host-array placement, KSP solve, matrix-update path, and PETSc stages. |
| https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/Make/options | OpenFOAM and PETSc header/library link requirements. |
| https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/Allwmake | Module build entry point and `-prefix` support. |
| https://petsc.org/release/install/install/ | PETSc MPI/BLAS/CUDA configuration and WSL experimental status. |
| https://petsc.org/release/manualpages/Mat/MATSEQAIJCUSPARSE/ and https://petsc.org/release/manualpages/Vec/VECCUDA/ | CUDA matrix/vector types and their option keys. |
| https://petsc.org/release/manualpages/Log/PetscLogView/ | PETSc event/GPU time logging and its possible overhead. |
| https://docs.nvidia.com/cuda/archive/13.2.1/wsl-user-guide/index.html | WSL driver boundary, managed memory and NVML limitations. |
| https://docs.nvidia.com/cuda/cuda-programming-guide/02-basics/understanding-memory.html | Limited WSL unified-memory migration behavior. |
| https://docs.nvidia.com/nsight-systems/UserGuide/index.html | CUDA/Unified Memory transfer-trace semantics; actual WSL support remains Flagged. |
| https://www.openfoam.com/news/main-news/openfoam-v2512 and https://dl.openfoam.com/repos/deb/dists/noble/main/pool/2512_0/binary-amd64/ | Publisher release and matching Ubuntu package family, not an apt closure. |

**Confidence:** Verified means the cited source/receipt was read or previously measured; Proposed means a pre-run
choice frozen here; Flagged means a specific execution blocker. No speedup, whole-solver offload, installed-size
total, profiler coverage or exact-build compatibility is claimed as Verified.
