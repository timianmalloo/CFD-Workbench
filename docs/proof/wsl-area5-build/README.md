---
id: proof-wsl-area5-build
title: "Area 5 WSL2 arm64 build & qualification: CUDA 13.2 + PETSc-CUDA sm_121 + OpenFOAM v2512 + petsc4Foam + Live C# Launcher PASS"
type: proof-pack
status: active
owner: "@w6-wsl-build"
phase: qualification
tags: [wsl, arm64, cuda, blackwell, sm121, petsc, openfoam, petsc4foam, net-proxy-tls, area5]
links:
  - { to: coordination-wave6-and-area5-wsl, rel: implements }
  - { to: proof-wsl-preflight, rel: refines }
  - { to: design-guided-solver-setup, rel: relates-to }
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-20"
summary: >-
  Dedicated arm64 WSL2 distro cfdw-openfoam2512-arm64 imported with host Ubuntu-24.04 untouched (Stopped pre/post).
  CUDA 13.2.2-1 (nvcc 13.2.86, native Blackwell compute_121) + GCC 13.3.0 / OpenMPI 4.1.6 / BLAS / LAPACK + PETSc 3.26.0
  (--with-cuda=1 --with-cuda-arch=121, zero --download-*) PASS make check 4/4 and verify 100% GPU flops + identical
  error norms on 160k-DOF and 1M-DOF CG+Jacobi solves (12.5x clean wall reduction on 1M-DOF synthetic Laplacian).
  Resumed 2026-11-26 on an unfiltered network (A5-RESUME): signed OpenFOAM v2512 arm64 (2512.0-2, build
  _bd2b6720-20260127) installed with 0 removals, M1 icoFoam/cavity PASS (Disallowing, Courant mean 0.222158),
  petsc4Foam (cb1badc4...) compiled & loaded (Can load "petscFoam"), OpenFOAM CPU vs Blackwell sm_121 GPU pressure-solve
  trials PASS with 100% GPU flops on seqaijcusparse and 2e-14 Cd parity on NASA TMR NACA 0012 L6 (14,336 cells; honest
  small-mesh wall finding: native GAMG 5.18 s vs PETSc GPU 27.77 s), and live Windows 11 ARM64 C# SolverLauncher M1/M2
  smoke + native SU2 v8.5.0 smoke both PASS.
---

# Area 5 WSL2 arm64 build & qualification (`A5-WSL` + `A5-RESUME`)

**Goal.** Provision a dedicated arm64 WSL2 distribution (`cfdw-openfoam2512-arm64`) on the Windows ARM64 Blackwell RTX
Spark N1X host without touching the host's existing `Ubuntu-24.04`, qualify signed CUDA 13.2 + GCC-13/OpenMPI/BLAS/LAPACK +
PETSc 3.26.0 `--with-cuda=1` (`sm_121`) + OpenFOAM v2512 + `petsc4Foam`, prove real OpenFOAM CPU vs GPU pressure solves
and live Windows 11 ARM64 C# `SolverLauncher` (`M1–M8`) execution, and commit reproducible automation and scrubbed receipts.

## Verdict summary (Verified 2026-11-22 & `A5-RESUME` 2026-11-26 UTC)

| Component | Verdict | Evidence receipt | Notes |
|---|---|---|---|
| Host `Ubuntu-24.04` isolation | **PASS** | `cuda-toolchain-receipt.json`, `openfoam-petsc4foam-qualification.json` | `wsl.exe --list --verbose` confirms `* Ubuntu-24.04 Stopped 2` both before and after both sessions; never invoked, cloned, or mounted |
| Dedicated WSL2 rootfs import | **PASS** | `cuda-toolchain-receipt.json` | `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz` (341,873,503 B, SHA-256 `fecec1d9...f2ef11`) matched `SHA256SUMS`; `gpgv` `VALIDSIG D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81` (`ubuntu-cloudimage-keyring.gpg`); imported at `D:\wsl\cfdw-openfoam2512-arm64` |
| Clean-distro `tools/wsl-preflight.sh` (historical 2026-11-22) | **BLOCKED (`net.proxy-tls`)** | `preflight-receipt.json`, `net-proxy-tls-receipt.json` | Historical capture on Cisco Umbrella network closing the previously `Flagged` `net.proxy-tls` row in `docs/design/guided-solver-setup.md` §8; superseded by 2026-11-26 unfiltered-network `A5-RESUME` below |
| Signed CUDA 13.2 + toolchain apt install | **PASS** | `cuda-toolchain-receipt.json` | 4/4 reachable `InRelease` indices verified with `gpgv VALIDSIG`; 184 packages installed (`cuda-toolkit-13-2=13.2.2-1`, GCC 13.3.0, OpenMPI 4.1.6, BLAS/LAPACK 3.12.0, make, git, curl, python3, time), **0 removals**, **0 Linux NVIDIA drivers**; `nvcc` V13.2.86 natively lists `compute_121` |
| PETSc 3.26.0 `--with-cuda=1 --with-cuda-arch=121` | **PASS** | `petsc-cuda-qualification.json` | Tarball verified (`16,933,940` B, SHA-256 `f5230023...`), zero `--download-*`, configure 78 s, build 96 s, **`make check` 4/4 PASS** (21 s, incl. `src/snes/tutorials/ex19` CUDA on `SM 121`) |
| PETSc CPU vs GPU KSP smoke (`ex2`) | **PASS** | `petsc-cuda-qualification.json` | Exact error norm & iteration parity on 160k DOFs (`1.23398e-07`, 790 iters) and 1M DOFs (`4.69193e-05`, 1,715 iters); **100% GPU flops** (`seqaijcusparse` / `seqcuda`, `18.1` Gflop/s `MatMult`, `14.9` Gflop/s `KSPSolve`); 1M-DOF clean wall **2.98 s GPU vs 37.27 s CPU** |
| Signed OpenFOAM v2512 arm64 apt install (`A5-RESUME`) | **PASS** | `openfoam-petsc4foam-qualification.json` | Verified `openfoam-archive-keyring.gpg` + `gpgv VALIDSIG DC93C096174122E256DA24063386DD74948D208F`; 53 packages installed (`openfoam2512*=2512.0-2` arm64), **0 removals**, **0 Linux NVIDIA drivers**; `WM_OPTIONS=linuxARM64GccDPInt32Opt`, build `_bd2b6720-20260127` |
| OpenFOAM v2512 arm64 M1 `icoFoam/cavity` CPU smoke (`A5-RESUME`) | **PASS** | `openfoam-petsc4foam-qualification.json` | `allowSystemOperations : Disallowing`, `checkMesh` `Mesh OK.` (400 cells), `Time = 0.5`, **`Courant Number mean: 0.222158 max: 0.852134`** (0.000000 diff vs macOS & Windows x64), `40 ms` wall, `35,368 KiB` RSS |
| `petsc4Foam` `Allwmake` + `foamHasLibrary` (`A5-RESUME`) | **PASS** | `openfoam-petsc4foam-qualification.json` | Commit `cb1badc4978306e7001a4654dcdf53f284a92365` built `/opt/cfdw-f1/openfoam-module/lib/libpetscFoam.so` (`169,352` B, SHA-256 `30e0fd46...`), linked to `libpetsc.so.3.26` + CUDA 13.2; `foamHasLibrary -verbose petscFoam` → **`Can load "petscFoam"`**; `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` exits `0` |
| OpenFOAM CPU vs `petsc4Foam` Blackwell `sm_121` GPU trials (`A5-RESUME`) | **PASS (functional/numeric); small-mesh overhead measured** | `openfoam-petsc4foam-qualification.json` | `seqaijcusparse` executes **100% GPU flops** on both `icoFoam/cavity` (`Courant mean 0.222158` exact parity) and frozen NASA TMR NACA 0012 L6 `simpleFoam` (`14,336` cells, 200 iters, `|ΔCd| = 2e-14`, `|ΔCl| = 0.0` vs PETSc CPU). At 14.3k cells, kernel-launch overhead makes GPU slower than native CPU `GAMG` (`27.77 s` vs `5.18 s`); GPU offload is only worthwhile on large 3D meshes (`10^5–10^6+` cells) |
| Live Windows 11 ARM64 C# `SolverLauncher` (`M1–M8`) & SU2 v8.5.0 smoke | **PASS** | `openfoam-petsc4foam-qualification.json` | Caught & fixed defect class **`WSL-CLEAN-ENV-M1`** (`SystemRoot` + argv-only `/usr/bin/env -i HOME=<wsl-home>`); live `CFDW_LIVE_WSL_SMOKE=1` `SolverLauncher.RunOpenFoamSmokeAsync` passed (`_bd2b6720-20260127`, `Disallowing`, `0.222158`, `15.984 s`). Pinned `SU2-v8.5.0-win64-omp.zip` (`4466fe21...`) + `SU2_CFD.exe` (`3cb60646...`) passed on ARM64 Prism emulation (`2.653 s`, `CD = 2.885552317`, `CL = -2.157833374e-16`, exact match to Windows x64) |

## 1. Host isolation & rootfs provenance

Pre-run and post-run `wsl.exe --list --verbose` both observed:

- **Before:** `* Ubuntu-24.04 Stopped 2` (`cfdw-openfoam2512-arm64` and `D:\wsl\cfdw-openfoam2512-arm64` absent).
- **After (`wsl.exe --terminate cfdw-openfoam2512-arm64`):** `* Ubuntu-24.04 Stopped 2`, `cfdw-openfoam2512-arm64 Stopped 2`.

Verified environment inside `cfdw-openfoam2512-arm64`:

- Architecture: `aarch64` / `arm64`, Linux kernel `6.18.58.1-microsoft-standard-WSL2`, Ubuntu 24.04 LTS (`noble`).
- GPU: `NVIDIA RTX Spark N1X (6144-core Blackwell RTX GPU)`, Windows driver `617.42`, compute capability `12.1`, `64000 MiB`.
- Disk usage on `/`: `1.2G` fresh import (`955G` free) → `12G` post CUDA 13.2 + toolchain (`945G` free) → `13G` post PETSc 3.26.0 + `petsc4Foam` (`944G` free).

## 2. Verified `net.proxy-tls` network blocker (OpenFOAM repo)

Running `tools/wsl-preflight.sh --keyrings /mnt/d/reviewed --out /root/cfdw-receipts/preflight-receipt.json` on the fresh
distro exited `1` at `step=update` in `11 s` (`preflight-receipt.json`). Direct diagnosis (`net-proxy-tls-receipt.json`)
verified:

1. `https://dl.openfoam.com/repos/deb/dists/noble/InRelease` itself completes TLS cleanly (`dl.openfoam.com`) and returns
   `HTTP/2 302` with `location: https://sourceforge.net/projects/openfoam/files/repos/deb/dists/noble/InRelease`.
2. On this network, DNS resolves `sourceforge.net` to `::ffff:146.112.61.104` (Cisco Umbrella / OpenDNS).
3. `openssl s_client` inside WSL observes `issuer=O = Cisco, CN = Cisco Umbrella Secondary SubCA lax-SG` with
   `Verify return code: 20 (unable to get local issuer certificate)`.
4. `apt-get update` fails closed with `Certificate verification failed: The certificate is NOT trusted. The certificate issuer is unknown.`
5. Native Windows `curl.exe` fails closed identically with
   `curl: (60) schannel: SEC_E_UNTRUSTED_ROOT (0x80090325) - The certificate chain was issued by an authority that is not trusted.`

No TLS verification bypass, DNS override, or proxy circumvention was attempted. Meanwhile, `ports.ubuntu.com`,
`developer.download.nvidia.com`, `web.cels.anl.gov`, and `gitlab.com` all verified cleanly over TLS. This real Windows
capture provides concrete `net.proxy-tls` failure text for `docs/design/guided-solver-setup.md` §8.

## 3. Signed CUDA 13.2 + GCC-13 / OpenMPI / BLAS / LAPACK installation

Executed via `tools/wsl-build-openfoam-cuda.sh --mode cuda-apt --keyrings /mnt/d/reviewed` (`cuda-toolchain-receipt.json`):

- Verified `cuda-archive-keyring.gpg` SHA-256 `25100d6f...24a376` and `/usr/share/keyrings/ubuntu-archive-keyring.gpg`.
- Verified 4/4 `InRelease` files with `gpgv VALIDSIG`:
  - NVIDIA `ubuntu2404/sbsa`: `EB693B3035CD5710E231E123A4B469963BF863CC`
  - Ubuntu `noble`, `noble-updates`, `noble-security`: `F6ECB3762474EDA9D21B7022871920D1991BC93C`
- `apt-get --simulate --no-install-recommends` verified **184 packages installed, 0 removals, 0 forbidden Linux NVIDIA
  driver packages**.
- `nvcc --version` reported `Cuda compilation tools, release 13.2, V13.2.86` (`cuda_13.2.r13.2/compiler.37953736_0`).
- `nvcc --list-gpu-arch` confirmed **native Blackwell `compute_121`** support (`compute_75` through `compute_121`), so no
  `sm_120` PTX fallback was needed.

## 4. PETSc 3.26.0 `--with-cuda=1` (`SM 121`) build & GPU qualification

Executed via `tools/wsl-build-openfoam-cuda.sh --mode petsc-cuda` (`petsc-cuda-qualification.json`):

- **Source verification:** `petsc-3.26.0.tar.gz` fetched from ANL (`16,933,940` B, SHA-256
  `f5230023e6e22ee607802a13c82bc25f3d81e71654ad386a5b9bdff17fed93df` — exact match).
- **Verified `./configure --help` flags:** `--with-precision=<__fp16,single,double,__float128>`,
  `--with-scalar-type=<real or complex>`, `--with-64-bit-indices=<bool>`, `--with-cuda=<bool>`, `--with-cuda-dir=<dir>`,
  `--with-cuda-arch`.
- **Two real environment findings captured into `tools/wsl-build-openfoam-cuda.sh`:**
  1. Installing `gcc-13`, `g++-13`, `gfortran-13` without the unversioned `gfortran` meta-package leaves `mpif90` unable to
     find `gfortran`; setting `OMPI_CC=gcc-13 OMPI_CXX=g++-13 OMPI_FC=gfortran-13` binds OpenMPI cleanly to GCC 13.3.0.
  2. Ubuntu Noble's system OpenMPI 4.1.6 is not built with CUDA/GPU-aware UCX; PETSc 3.26.0 aborts CUDA runs unless
     `-use_gpu_aware_mpi 0` is configured (written to `~/.petscrc` and `PETSC_OPTIONS`; single-rank execution per
     `docs/plans/windows-gpu-f1-r211.md`), and root-mode `mpiexec` requires `OMPI_ALLOW_RUN_AS_ROOT=1` +
     `OMPI_ALLOW_RUN_AS_ROOT_CONFIRM=1`.
- **Build & `make check`:** Configure `78 s`, `make all -j8` `96 s` (`libpetsc.so`: `139,217,536` B, SHA-256
  `5adefba7...edd2fdb`, linked against `libcudart.so.13`, `libcublas.so.13`, `libcusparse.so.12`, `libcusolver.so.12`,
  `libcurand.so.10`, `libmpi.so.40`, `liblapack.so.3`, `libblas.so.3`; zero `--download-*`). `make check` passed **4/4**
  in `21 s` (`ex19` 1 MPI, `ex19` 2 MPI, **`ex19` CUDA**, `ex5f` Fortran).

### Standalone PETSc CPU vs GPU KSP smoke (`src/ksp/ksp/tutorials/ex2`, CG + Jacobi, 1 rank, `OMP_NUM_THREADS=1`)

> **Scope note:** This is a standalone PETSc 2D 5-point Laplacian smoke proving native `SM 121` CUDA execution,
> `seqaijcusparse` / `seqcuda` dispatch, and numerical parity — **not** an OpenFOAM whole-solver F1 benchmark (initially
> blocked on `openfoam2512` repo reachability in Wave 6; see §5 below for the 2026-11-26 `A5-RESUME` OpenFOAM +
> `petsc4Foam` qualification trials).

| Problem | Arm | Mat / Vec type | Iterations | Norm of error | `KSPSolve` (s) | `MatMult` (s) | GPU `%F` | GPU Mflop/s (`KSPSolve` / `MatMult`) | Process wall (s) | Max RSS (KiB) |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 400×400 (160k DOFs, `rtol=1e-10`) | CPU | `seqaij` / `standard` | 790 | `1.23398e-07` | 0.694 | 0.321 | 0% | 0 / 0 | 1.19 | 282,772 |
| 400×400 (160k DOFs, `rtol=1e-10`) | GPU (diagnostic `-log_view_gpu_time`) | `seqaijcusparse` / `cuda` | 790 | `1.23398e-07` | 0.753 | 0.106 | **100%** | 6,761 / 20,155 | 1.42 | 445,512 |
| 400×400 (160k DOFs, `rtol=1e-10`) | GPU (clean) | `aijcusparse` / `cuda` | 790 | `1.23398e-07` | — | — | — | — | **1.12** | 445,376 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | CPU | `seqaij` / `standard` | 1,715 | `4.69193e-05` | 19.834 | 7.912 | 0% | 0 / 0 | 37.27 | 393,244 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | GPU (diagnostic `-log_view_gpu_time`) | `seqaijcusparse` / `cuda` | 1,715 | `4.69193e-05` | 3.681 | 1.227 | **100%** | 14,905 / 18,113 | 5.60 | 513,020 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | GPU (clean) | `aijcusparse` / `cuda` | 1,715 | `4.69193e-05` | — | — | — | — | **2.98** | 512,944 |

## 5. `A5-RESUME` completion on unfiltered network (Verified 2026-11-26 UTC)

Connected to an unfiltered mobile hotspot (`dl.openfoam.com` and `sourceforge.net` both `HTTP 200`, `ssl=0` from host and inside `cfdw-openfoam2512-arm64`), `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` completed all remaining stages and exited `0` (`openfoam-petsc4foam-qualification.json`):

- **Signed OpenFOAM v2512 arm64 install:** Verified `openfoam-archive-keyring.gpg` SHA-256 (`6d17d37b…1a93`) and `gpgv VALIDSIG DC93C096174122E256DA24063386DD74948D208F` (`openfoam-com <admin@opencfd.co.uk>`). Simulated and installed **53 packages** (`openfoam2512*=2512.0-2` arm64: `openfoam2512_2512.0-2_arm64.deb` `61,532,936` B, SHA-256 `f01ec409…588a5`), **0 removals**, **0 forbidden Linux NVIDIA drivers**. Disk usage `/` moved from `13G` → `14G` (`943G` free).
- **Environment & build identity:** `WM_OPTIONS=linuxARM64GccDPInt32Opt`, `FOAM_API=2512`, `WM_PROJECT_VERSION=v2512`, `Build: _bd2b6720-20260127` (`LSB;label=32;scalar=64`) — matching `RouteCatalogue.WinWslOpenFoamId`'s pinned `ExpectedBuildId` byte-for-byte.
- **M1 `icoFoam/cavity` CPU smoke:** `allowSystemOperations : Disallowing`, `checkMesh` `Mesh OK.` (`400` cells), `Time = 0.5`, **`Courant Number mean: 0.222158 max: 0.852134`** (`0.000000` delta vs macOS and Windows x64), wall `0.04 s`, Max RSS `35,368 KiB`.
- **`petsc4Foam` `Allwmake` & load verification:** Compiled pinned commit `cb1badc4978306e7001a4654dcdf53f284a92365` against OpenFOAM v2512 arm64 + PETSc 3.26.0 CUDA 13.2 into `/opt/cfdw-f1/openfoam-module/lib/libpetscFoam.so` (`169,352` B, SHA-256 `30e0fd46…434`), linked against `libpetsc.so.3.26` and CUDA 13.2 runtime/cuSPARSE/cuBLAS/cuSOLVER. Fixed `tools/wsl-build-openfoam-cuda.sh` `LD_LIBRARY_PATH` to include `${opt_root}/openfoam-module/lib`; `foamHasLibrary -verbose petscFoam` verified **`Can load "petscFoam"`** and `--mode openfoam-resume` exited `0`.

### OpenFOAM v2512 + `petsc4Foam` CPU vs Blackwell `sm_121` GPU pressure-solve qualification

| Case | Arm | Pressure solver / Mat | GPU `%F` (`KSPSolve` / `MatMult`) | Physics metric | Clean wall (s) | Max RSS (KiB) |
|---|---|---|---:|---|---:|---:|
| M1 `icoFoam/cavity` (`400` cells, `t = 0.5`) | Native CPU | `PCG/DIC` | — | Courant mean `0.222158` | 0.03 | 35,368 |
| M1 `icoFoam/cavity` (`400` cells, `t = 0.5`) | `petsc4Foam` CPU (diag) | `cg`+`jacobi` / `seqaij` | 0% / 0% | Courant mean `0.222158` | 1.39 | 300,144 |
| M1 `icoFoam/cavity` (`400` cells, `t = 0.5`) | `petsc4Foam` GPU (diag) | `cg`+`jacobi` / `seqaijcusparse` | **100% / 100%** | Courant mean `0.222158` | 7.49 | 478,228 |
| NASA TMR NACA 0012 L6 `simpleFoam` (`14,336` cells, 200 iters) | Native CPU | `GAMG` | — | `Cd = 0.0186461475`, `Cl = 0.9758586222` | **5.18** | 102,760 |
| NASA TMR NACA 0012 L6 `simpleFoam` (`14,336` cells, 200 iters) | `petsc4Foam` CPU (clean) | `cg`+`jacobi` / `seqaij` | 0% / 0% | `Cd = 0.01845323966119`, `Cl = 0.9825616365107` | 17.22 | 370,576 |
| NASA TMR NACA 0012 L6 `simpleFoam` (`14,336` cells, 200 iters) | `petsc4Foam` GPU (clean) | `cg`+`jacobi` / `seqaijcusparse` | **100% / 100%** (50-iter diag: `3,057 GPU Mflop/s`) | `Cd = 0.01845323966121`, `Cl = 0.9825616365107` (`abs(ΔCd) = 2e-14`, `abs(ΔCl) = 0.0` vs PETSc CPU) | 27.77 | 548,524 |

**Honest engineering verdict:** Functional and numerical GPU qualification **passes** (`seqaijcusparse` executes 100% of KSP/MatMult flops on `SM 121` with `2e-14` `Cd` parity vs PETSc CPU). At `14,336` cells, ~416 tiny `MatMult` CUDA kernel launches per SIMPLE iteration plus H2D/D2H transfers make GPU slower than native CPU `GAMG` (`27.77 s` vs `5.18 s`); GPU pressure offload is reserved for large 3D hydrofoil meshes (`10^5–10^6+` cells), consistent with the `12.5×` 1M-DOF standalone PETSc `ex2` speedup above.

### Live Windows 11 ARM64 C# `SolverLauncher` (`M1–M8`) & native SU2 v8.5.0 verification

- **Real Windows WSL defect caught & fixed (`WSL-CLEAN-ENV-M1`, `docs/lessons/defect-classes.md`):** Live probing of `SolverLauncher` against `cfdw-openfoam2512-arm64` caught that omitting `SystemRoot` crashes `wsl.exe` (`Wsl/Service/RPC_S_SYSTEM_HANDLE_TYPE_MISMATCH`), and `wsl.exe` ignores Windows `HOME` (leaving OpenFOAM at `Allowing`). Fixed in `SolverLauncherPolicy`, `SolverLauncher`, `LocalCfdRunSubstrate`, and `WindowsSetupAdapters` via `SystemRoot=C:\Windows` + argv-only `/usr/bin/env -i HOME=<wsl-home>` (zero shell). Live opt-in `CFDW_LIVE_WSL_SMOKE=1` `SolverLauncher_WindowsWsl_CleanEnvSystemRootAndLinuxHome_M1M6` passed end-to-end (`build=_bd2b6720-20260127`, `banner=Disallowing`, `courant=0.222158`, `15,984 ms`).
- **Live Windows 11 ARM64 SU2 v8.5.0 smoke:** Verified `SU2-v8.5.0-win64-omp.zip` (`28,043,921` B, SHA-256 `4466fe21…4bc11`) and `SU2_CFD.exe` (`3cb60646…99248`, Harrier) under Windows 11 ARM64 Prism x64 emulation: 2,048-cell cylinder smoke exited `0` in `2.653 s` (iter `61`, `rms[P] = -10.02477646`, `CD = 2.885552317`, `CL = -2.157833374e-16` — bit-identical printed digits to Windows x64).
- **Post-qualification host isolation:** `wsl.exe --terminate cfdw-openfoam2512-arm64` completed cleanly; `wsl.exe --list --verbose` confirmed `* Ubuntu-24.04 Stopped 2` untouched and `cfdw-openfoam2512-arm64 Stopped 2`.
