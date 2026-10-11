---
id: proof-win-gpu-f1-dependency-worksheet
title: "F1 combined dependency and capacity closure worksheet"
type: proof-pack
status: proposed
owner: "@pc-f1-author-repair-r211"
phase: design
tags: [windows, wsl, gpu, cuda, petsc, openfoam, ruling-211]
links:
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-18"
summary: >-
  Exact verified B2 and OpenFOAM package metadata, with the unclosed Ubuntu transaction and capacity operands flagged. No build-input acquisition or WSL action is authorized.
---

# F1 combined dependency worksheet — VETO/open

**Status:** this is a partial, source-identified inventory, **not an installable combined lock**. Ruling 211
prohibits acquisition of build inputs and WSL mutation until a second operator ruling. Astra finding F1-ASTRA-1
therefore remains open. The purpose of this worksheet is to prevent the known rows from being mistaken for a
complete Ubuntu/OpenFOAM/CUDA/PETSc transaction. Access date for live upstream metadata: **2026-10-11 UTC**.

## Exact recorded rows and identity basis

The [B2 package lock](../win-gpu-b2/package-lock.json) is incorporated as its **135 individual amd64/all rows**,
not summarized into a fictitious single CUDA package. For each row it records exact package, version, architecture,
Ubuntu or NVIDIA repository, source URL, filename, SHA-256, archive download bytes and `Installed-Size` KiB where
published. The [B2 licence map](../win-gpu-b2/license-map.json) maps the same 135 rows to embedded package notices
or NVIDIA's EULA, with the original [signed-source record](../win-gpu-b2/source-downloads.json). Its download total
is **3,405,443,104 bytes**. Exactly one row, `nsight-systems-2025.6.3=2025.6.3.541-256337736014v0`, has
`Installed-Size: Not recorded`; the other 134 records sum to **6,796,931 KiB**. The prior
[archive verifier](../win-gpu-b2/verify-debs.py) measured **8,092,712,328 archive-member payload bytes** across
the downloaded packages. That is a distinct measurement from `Installed-Size` and from actual filesystem use.
The B2 rootfs [before-state](../win-gpu-b2/attempt-4-success/packages-before.tsv) held 528 packages. A new F1
import must match that rootfs hash and package state before the B2 lock can be reused without re-resolution.

The OpenFOAM publisher [Noble `Packages.gz`](https://dl.openfoam.com/repos/deb/dists/noble/main/binary-amd64/Packages.gz)
was read as index text at the date above: 34,572 compressed bytes, SHA-256
`235cee0e6311c836ac13b7fc11092345f06cd06524772f62b9cd55a630402cd2`. Its
[`InRelease`](https://dl.openfoam.com/repos/deb/dists/noble/InRelease) was read as PGP-signed text: 3,624 bytes,
SHA-256 `50cb05b774c0038b0cea099e68c8ba69544881aa79ce8fef2dfc4a719c2b0887`. **Flagged:** the signing
key was not authenticated and the signature was not cryptographically verified in this design session. Each row
below uses that exact publisher index as its source identity; the row SHA-256 is the advertised `.deb` hash, not
a hash independently measured from package bytes. `all` packages are part of the amd64 apt transaction.

| Package = version | Arch / ABI role | SHA-256 of advertised `.deb` | Download bytes | `Installed-Size` KiB | Licence basis |
|---|---|---|---:|---:|---|
| `openfoam2512=2512.0-2` | amd64; runtime, DP/Int32 OpenMPI target | `c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed` | 68,264,976 | 333,725 | OpenFOAM publisher GPL source statement; embedded binary notice Flagged |
| `openfoam2512-common=2512.0-2` | all; runtime data | `f438ecbfaff0248b9273502f3b43a6cb1b4f81c07224dbda811f1b48d989026f` | 1,311,782 | 7,172 | Same; binary notice Flagged |
| `openfoam2512-dev=2512.0-2` | amd64; headers/development selector | `cb21f87814cf11d0cf396b60d471594a747d797908cbb846029ea8d9df2b3bf3` | 11,228 | 22 | Same; binary notice Flagged |
| `openfoam2512-source=2512.0-2` | all; development source dependency | `3fa3396a40b41c35e23d3544ac8e49d65e0d15ad46d12ddf480626c23c822b1b` | 7,567,038 | 99,913 | Same; binary notice Flagged |
| `openfoam2512-tools=2512.0-2` | amd64; `wmake` tools | `fc0d19c1152819cc7d2a6165eec911c0f1ea3ab7d88cc04ee4a8070d11aa2e62` | 53,974 | 138 | Same; binary notice Flagged |
| **Five-row subtotal** |  |  | **77,208,998** | **440,970** | **Not a dependency closure** |

The five advertised filenames are under the same exact
[`dists/noble/main/pool/2512_0/`](https://dl.openfoam.com/repos/deb/dists/noble/main/pool/2512_0/)
index prefix: runtime/dev/tools in `binary-amd64/`, common/source in `binary-all/`. No package archive was fetched.
The publisher's [`openfoam2512-dev` metadata](https://dl.openfoam.com/repos/deb/dists/noble/main/binary-amd64/Packages.gz)
requires the four matching packages above plus `flex`, `libfl-dev`, `mpi-default-bin`, `mpi-default-dev`,
`gfortran`, `libreadline-dev`, `libscotch-dev`, `libptscotch-dev`, `libboost-system-dev`, `libcgal-dev`,
`libfftw3-dev`, and `zlib1g-dev`. The runtime adds `libboost-system1.83.0`, `libfftw3-double3`,
`libopenmpi3t64`, `libptscotch-7.0`, `libscotch-7.0`, and the listed libc/GCC/GMP/MPFR/zlib runtime libraries;
the exact version constraints are in that index. Some may already exist in the rootfs or B2 lock. No assumption
about which are new is made here.

## Required combined ABI rows that remain Flagged

| Role | Proposed numerical/build contract | Missing fact that prevents an exact package row |
|---|---|---|
| C/C++ compiler | GCC/G++ 13, C++17, `linux64GccDPInt32Opt`; B2 lock includes `gcc-13`/`g++-13` family at `13.3.0-6ubuntu2~24.04.1` | Exact F1 transaction may upgrade those rows or select a different Ubuntu pocket; signed index and simulation required. |
| Fortran compiler | `gfortran` matching GCC 13 | Exact `gfortran`/`gfortran-13` provider, version, hash, size and notice absent from B2 lock. |
| MPI | Ubuntu OpenMPI 4.1 ABI; `mpicc`, `mpicxx`, `mpif90` from one provider, one rank | `mpi-default-dev` virtual/default provider, `libopenmpi-dev`, `openmpi-bin` and runtime exact versions and ABI paths unresolved. |
| BLAS/LAPACK | System real-double BLAS/LAPACK; same provider in PETSc configure and at runtime | `libblas-dev`/`liblapack-dev` or their selected provider, exact versions, hashes, sizes and licence notices unresolved. |
| PETSc | 3.26.0; real double, 32-bit indices, CUDA sm_86, shared optimized build | Source tarball is 16,933,940 bytes with B1-measured SHA-256 `f5230023e6e22ee607802a13c82bc25f3d81e71654ad386a5b9bdff17fed93df`; extracted/build sizes and upstream digest authority unresolved. |
| petsc4Foam | Fixed Git commit `cb1badc4978306e7001a4654dcdf53f284a92365`, linked to same OpenFOAM/PETSc/MPI ABI | Git tree/archive bytes and tree digest unresolved; source acquisition forbidden now. |

**Source identity for the compiler/MPI/BLAS proposal:** B2's exact GCC/G++ rows are in the linked lock;
the OpenFOAM package dependency expression comes from the publisher index above. PETSc's upstream
[installation contract](https://petsc.org/release/install/install/) supplies the compiler-wrapper,
precision/index and system BLAS/MPI configuration meaning. Neither it nor an Ubuntu package name proves the
actual provider selected by apt. Ubuntu's official
[`noble` main index](https://archive.ubuntu.com/ubuntu/dists/noble/main/binary-amd64/Packages.xz),
[`noble` universe index](https://archive.ubuntu.com/ubuntu/dists/noble/universe/binary-amd64/Packages.xz),
their updates/security pockets and authenticated Release signatures must be frozen together with the rootfs
status before exact versions can be called Verified.

## Capacity arithmetic and missing operands

| Axis | Exact known bytes or required future operand | Provenance / status |
|---|---:|---|
| B2 135-row download | 3,405,443,104 | B2 lock, Verified prior transaction |
| Five OpenFOAM row download | 77,208,998 | Publisher package index, Verified metadata |
| **Known package-download subtotal** | **3,482,652,102** | Excludes Ubuntu transitive additions/upgrades; **not F1 total** |
| B2 package archive-member payload | 8,092,712,328 | B2 archive verifier; not installed filesystem size |
| B2 134-row advertised Installed-Size | 6,960,057,344 | 6,796,931 KiB × 1,024; one NVIDIA row missing |
| OpenFOAM five-row advertised Installed-Size | 451,553,280 | 440,970 KiB × 1,024; excludes dependencies |
| **Known Installed-Size subtotal** | **7,411,610,624** | Excludes one NVIDIA row and Ubuntu transitive additions; estimate, not actual occupancy |
| Ubuntu rootfs archive | 356,739,129 | B2 authenticated source record; presence at same hash still needs preflight |
| PETSc source archive | 16,933,940 | B1 measured hash/bytes; provenance gate remains |
| **Known rootfs plus PETSc archive subtotal** | **373,673,069** | Excludes adapter Git tree and all extracted source occupancy |
| Unpack staging | 2,147,483,648 was B2-only reserve | F1 extension and peak overlap not derived |
| Build outputs | 1,073,741,824 was B2-only reserve | F1 PETSc/adapter reserve may be a conservative **Proposed** bound, set before execution |
| Frozen case copies | **Not recorded** | Exact input and repeated output bounds to be measured from repository inputs and predeclared ceilings |
| Guest post-install headroom | **10,737,418,240** | Fixed 10 GiB, count once in guest threshold |
| Host/VHD headroom | **10,737,418,240** | Separate Windows C: headroom after bounded VHD growth |

There is **no finite derived F1 guest or Windows-host threshold yet**. Adding the known numbers would invent a
capacity result because the missing Ubuntu package set, one NVIDIA installed-size field, extracted sources,
build-output bound, case copies and VHD growth are not zero. A future closed worksheet must count newly installed
packages once, distinguish archive payload from advertised `Installed-Size`, define simultaneous peak staging,
publish the exact byte sum, and stop if either fresh free-space reading falls below its respective threshold.

## Smallest next decision for Mac and the operator

**Decision requested; no permission is inferred:** a second, tightly scoped ruling may authorize only a
metadata/preflight stage in a **new disposable** `cfdw-petsc-cuda132-f1` distro: verify the exact rootfs against
Ubuntu's signed `SHA256SUMS`, import it at the dedicated F1 path, capture its `dpkg` status, authenticate and freeze
Ubuntu/OpenFOAM/NVIDIA Release and Packages indices, run `apt-get -s --no-install-recommends` for the pinned CUDA,
OpenFOAM runtime/dev, GCC/G++/gfortran, OpenMPI and system BLAS/LAPACK roots, and export the complete zero-removal
transaction with `apt-cache show` metadata. For rows whose licence or installed-size metadata is absent, that same
ruling would have to authorize acquisition of the **exact resolved `.deb` files solely to inspect their notices and
archive payloads**, with each hash checked against the frozen authenticated index. No `apt install`, PETSc/adapter
source acquisition, build, case run or protected-distro command is part of this requested stage. If Mac/operator
declines archive acquisition, those per-binary notices/size gaps remain Flagged. After this stage, author the full
combined lock and finite guest/host thresholds for separate review and a later execution decision.

The preflight itself needs a separate finite containment gate: **Proposed**, cap its VHD at 8 GiB and require at
least 10 GiB additional free on Windows C: plus the 356,739,129-byte rootfs archive before import, or
**19,684,091,961 free bytes**. B2 measured a 5,179,965,440-byte VHD *after* CUDA, which is evidence for scale,
not a bound for F1. Stop the disposable preflight if its VHD reaches 8 GiB or the host reserve falls below 10 GiB;
retain evidence without install. This preflight threshold does **not** substitute for the still-unset full F1
guest/host thresholds.

The current work performed **no** distro import, WSL command, package/archive/binary download, installation,
compilation, source checkout or case execution. Only publisher and Ubuntu package-index text/HEAD metadata were read.
