---
id: proof-wsl-area5-build
title: "Area 5 WSL2 arm64 build & qualification: CUDA 13.2 + PETSc-CUDA sm_121 PASS; OpenFOAM 2512 BLOCKED (net.proxy-tls)"
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
  - { to: rulings, rel: depends-on }
review-by: "2027-02-20"
summary: >-
  Dedicated arm64 WSL2 distro cfdw-openfoam2512-arm64 imported with host Ubuntu-24.04 untouched (Stopped pre/post).
  CUDA 13.2.2-1 (nvcc 13.2.86, native Blackwell compute_121) + GCC 13.3.0 / OpenMPI 4.1.6 / BLAS / LAPACK + PETSc 3.26.0
  (--with-cuda=1 --with-cuda-arch=121, zero --download-*) PASS make check 4/4 and verify 100% GPU flops + identical
  error norms on 160k-DOF and 1M-DOF CG+Jacobi solves (12.5x clean wall reduction on 1M-DOF synthetic Laplacian).
  OpenFOAM v2512 apt install, M1 cavity smoke, and petsc4Foam Allwmake are honestly recorded as BLOCKED (net.proxy-tls)
  because dl.openfoam.com 302-redirects to sourceforge.net which Cisco Umbrella intercepts (146.112.61.104, untrusted root);
  petsc4Foam commit cb1badc4978306e7001a4654dcdf53f284a92365 is staged and tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume
  completes the remaining stages on an unfiltered network.
---

# Area 5 WSL2 arm64 build & qualification (`A5-WSL`)

**Goal.** Provision a dedicated arm64 WSL2 distribution (`cfdw-openfoam2512-arm64`) on the Windows ARM64 Blackwell RTX
Spark N1X host without touching the host's existing `Ubuntu-24.04`, qualify signed CUDA 13.2 + GCC-13/OpenMPI/BLAS/LAPACK +
PETSc 3.26.0 `--with-cuda=1` (`sm_121`) + OpenFOAM v2512 + `petsc4Foam`, and commit reproducible automation and scrubbed
receipts.

## Verdict summary (Verified 2026-11-22 UTC)

| Component | Verdict | Evidence receipt | Notes |
|---|---|---|---|
| Host `Ubuntu-24.04` isolation | **PASS** | `cuda-toolchain-receipt.json` | `wsl.exe --list --verbose` confirms `* Ubuntu-24.04 Stopped 2` both before and after; never invoked, cloned, or mounted |
| Dedicated WSL2 rootfs import | **PASS** | `cuda-toolchain-receipt.json` | `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz` (341,873,503 B, SHA-256 `fecec1d9...f2ef11`) matched `SHA256SUMS`; `gpgv` `VALIDSIG D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81` (`ubuntu-cloudimage-keyring.gpg`); imported at `D:\wsl\cfdw-openfoam2512-arm64` |
| Clean-distro `tools/wsl-preflight.sh` | **BLOCKED (`net.proxy-tls`)** | `preflight-receipt.json`, `net-proxy-tls-receipt.json` | Exit 1 at `step=update`: `dl.openfoam.com/repos/deb/dists/noble/InRelease` 302-redirects to `sourceforge.net`, which resolves to `146.112.61.104` (Cisco Umbrella `lax-SG` untrusted root on both WSL and Windows SChannel). Real evidence closing a previously `Flagged` `net.proxy-tls` row in `docs/design/guided-solver-setup.md` §8 |
| Signed CUDA 13.2 + toolchain apt install | **PASS** | `cuda-toolchain-receipt.json` | 4/4 reachable `InRelease` indices verified with `gpgv VALIDSIG`; 184 packages installed (`cuda-toolkit-13-2=13.2.2-1`, GCC 13.3.0, OpenMPI 4.1.6, BLAS/LAPACK 3.12.0, make, git, curl, python3, time), **0 removals**, **0 Linux NVIDIA drivers**; `nvcc` V13.2.86 natively lists `compute_121` |
| PETSc 3.26.0 `--with-cuda=1 --with-cuda-arch=121` | **PASS** | `petsc-cuda-qualification.json` | Tarball verified (`16,933,940` B, SHA-256 `f5230023...`), zero `--download-*`, configure 78 s, build 96 s, **`make check` 4/4 PASS** (21 s, incl. `src/snes/tutorials/ex19` CUDA on `SM 121`) |
| PETSc CPU vs GPU KSP solve smoke (`ex2`) | **PASS** | `petsc-cuda-qualification.json` | Exact error norm & iteration parity on 160k DOFs (`1.23398e-07`, 790 iters) and 1M DOFs (`4.69193e-05`, 1,715 iters); **100% GPU flops** (`seqaijcusparse` / `seqcuda`, `18.1` Gflop/s `MatMult`, `14.9` Gflop/s `KSPSolve`); 1M-DOF clean wall **2.98 s GPU vs 37.27 s CPU** |
| `petsc4Foam` pinned source staging | **PASS** | `petsc-cuda-qualification.json` | Cloned `https://gitlab.com/petsc/petsc4foam.git` at `/opt/cfdw-f1/petsc4foam`; verified `HEAD == cb1badc4978306e7001a4654dcdf53f284a92365`, tree `74e5dbf216effe0768687dec8e66c56f79c9df85` |
| OpenFOAM v2512 apt install, M1 cavity smoke & `petsc4Foam` `Allwmake` | **BLOCKED (`net.proxy-tls`)** | `net-proxy-tls-receipt.json`, `petsc-cuda-qualification.json` | Blocked exclusively by the Cisco Umbrella TLS filter on `sourceforge.net` (zero TLS/DNS/proxy bypass attempted). Distro `cfdw-openfoam2512-arm64` is cleanly stopped and retained ready for `--mode openfoam-resume` on an unfiltered network |

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
> `seqaijcusparse` / `seqcuda` dispatch, and numerical parity — **not** an OpenFOAM whole-solver F1 benchmark (which
> remains blocked on `openfoam2512` repo reachability).

| Problem | Arm | Mat / Vec type | Iterations | Norm of error | `KSPSolve` (s) | `MatMult` (s) | GPU `%F` | GPU Mflop/s (`KSPSolve` / `MatMult`) | Process wall (s) | Max RSS (KiB) |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 400×400 (160k DOFs, `rtol=1e-10`) | CPU | `seqaij` / `standard` | 790 | `1.23398e-07` | 0.694 | 0.321 | 0% | 0 / 0 | 1.19 | 282,772 |
| 400×400 (160k DOFs, `rtol=1e-10`) | GPU (diagnostic `-log_view_gpu_time`) | `seqaijcusparse` / `cuda` | 790 | `1.23398e-07` | 0.753 | 0.106 | **100%** | 6,761 / 20,155 | 1.42 | 445,512 |
| 400×400 (160k DOFs, `rtol=1e-10`) | GPU (clean) | `aijcusparse` / `cuda` | 790 | `1.23398e-07` | — | — | — | — | **1.12** | 445,376 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | CPU | `seqaij` / `standard` | 1,715 | `4.69193e-05` | 19.834 | 7.912 | 0% | 0 / 0 | 37.27 | 393,244 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | GPU (diagnostic `-log_view_gpu_time`) | `seqaijcusparse` / `cuda` | 1,715 | `4.69193e-05` | 3.681 | 1.227 | **100%** | 14,905 / 18,113 | 5.60 | 513,020 |
| 1000×1000 (1M DOFs, `rtol=1e-8`) | GPU (clean) | `aijcusparse` / `cuda` | 1,715 | `4.69193e-05` | — | — | — | — | **2.98** | 512,944 |

## 5. Resuming OpenFOAM v2512 + `petsc4Foam` on an unfiltered network

The dedicated distro `cfdw-openfoam2512-arm64` already holds the installed CUDA 13.2 + GCC-13/OpenMPI toolchain, built
PETSc 3.26.0 (`arch-linux-cuda132-opt`), and staged `petsc4Foam` commit `cb1badc4978306e7001a4654dcdf53f284a92365`
(tree `74e5dbf216effe0768687dec8e66c56f79c9df85`). Once connected to a network where `sourceforge.net` is not TLS-filtered:

```bash
wsl.exe -d cfdw-openfoam2512-arm64 -u root -- bash <repo-wsl-path>/tools/wsl-build-openfoam-cuda.sh \
    --mode openfoam-resume --keyrings /mnt/d/reviewed --receipt-dir /root/cfdw-receipts
```

This adds the verified OpenCFD apt repo (`openfoam-archive-keyring.gpg`, `gpgv VALIDSIG`), installs the five pinned
`openfoam2512*=2512.0-2` packages (with zero removals and zero Linux GPU driver), runs the Ruling 133 M1 `icoFoam/cavity`
CPU smoke (`allowSystemOperations 0`, `t=0.5`, expected Courant mean `0.222158`), compiles `petsc4Foam` via `./Allwmake`,
and checks `foamHasLibrary -verbose petscFoam`.
