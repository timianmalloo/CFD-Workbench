---
id: proof-win-gpu-b1-r199
title: "Windows GPU B1 inventory and live L3 guard"
type: proof-pack
status: blocked
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, gpu, cuda, openfoam, ruling-199, ruling-200]
links:
  - { to: proof-win-l3-observer, rel: depends-on }
  - { to: review-pr-30, rel: implements }
review-by: "2026-11-10"
summary: >-
  B1 stopped at the two-live-attempt cap before verified probe ownership; B2 and F1 remain closed.
---

# Windows GPU B1 inventory and live L3 guard

Ruling 199 authorizes one read-only B1 inspection on `cfdw-openfoam2512`. The probe runs on CPU 0 at nice 10 and has
a ten-host-minute ceiling including owned-process termination. It does not execute `wmake`, change apt sources, update
package metadata, install software, or alter the active OpenFOAM run.

The separate observer appends through the existing Ruling 197 observer. Its first two session-bound rows form a fresh
prelaunch baseline. Every later complete row is compared with that frozen baseline on both UTC and `/proc/uptime`
iteration rates. A slowdown of at least five percent on either clock stops the owned B1 process group. Invalid, stale,
reset, mismatched, skipped, or inactive observations fail closed and remain stopped. Observation continues to its
bounded end after a stop so the post-stop evidence remains available.

The B1 capture records the driver-reported CUDA ceiling, Linux and Windows free space, the v2512 development surface,
configured apt policy, retained official CUDA/PETSc metadata, and CUDA 13.2 and 13.4 repository inventories. These
inventories are not installable locks: unresolved external dependencies, unverified metadata signatures, missing
installed sizes, or package-level license gaps block B2. The final receipt binds an immutable N-row byte prefix of
`../win-l3-observer/samples.jsonl`.

B2 remains limited to a fresh disposable Ubuntu 24.04 WSL distro. It cannot target `cfdw-openfoam2512`, install a Linux
GPU driver, touch an L3 path/process/unit/file, build PETSc/OpenFOAM, or reopen F1.

## Result

B1 is incomplete and blocked. Both live attempts formed valid fresh baselines but stopped before verified probe
ownership. No driver, disk, OpenFOAM development-surface, apt-policy, PETSc-contract, or package-inventory result was
accepted. `receipt.md` records the attempts, the bounded observer result, the residual process uncertainty, and the
smallest repair. B2 and F1 remain closed.
