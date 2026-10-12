---
id: coordination-openfoam-gpu-status-r220
title: "OpenFOAM GPU work status at Ruling 220"
type: doc
status: in-progress
owner: "@pc-gpu-status-r217-cont"
tags: [coordination, windows, wsl, openfoam, gpu, cuda, petsc, ruling-220]
links:
  - { to: plan-win-openfoam-gpu-qualification, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: review-windows-gpu-f1-preflight-packet, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  CUDA capability and rollback are proved, the PETSc-CUDA plus petsc4Foam design and three-arm benchmark are specified,
  the R220 P1/P3 packet is Astra-accepted, and the exact run-1 environment contract is under independent review.
---

# OpenFOAM GPU work status at Ruling 220

**Snapshot:** 2026-10-12, Windows coordinator branch `win/gpu-f1-preflight-r217`. Mac remains repository leader at
epoch 6. Windows owns the reviewed dependency-only preflight and its evidence.

## Current result

The machine can execute CUDA work through WSL, but OpenFOAM GPU acceleration is not yet qualified. B2 proved the
pinned CUDA 13.2 toolchain and a managed-memory SAXPY workload on the RTX 3080 Ti Laptop GPU, with zero mismatches and
complete rollback from its disposable distro. The protected `cfdw-openfoam2512` distro remained unchanged.

The installed OpenFOAM v2512 runtime does not prove whole-solver GPU use. Source inspection found a conditional,
partial expression-offload path, while the installed libraries were CPU-linked. F1 therefore uses the separately
planned PETSc-CUDA plus petsc4Foam route. `amgx4Foam` and RapidCFD are rejected for this F1 scope.

## Stage ledger

| Stage | State | Verified result | Next gate |
|---|---|---|---|
| G0/G1 route inspection | Complete | GPU visible in WSL; installed v2512 does not establish whole-solver offload; exact PETSc/petsc4Foam route selected. | None. |
| B1 inventory and guard | Complete | Machine, package, OpenFOAM development gaps, and completed-L3 guard evidence accepted. | None. |
| B2 CUDA capability | Complete and rolled back | Exact 135-package CUDA 13.2 lock installed in a disposable distro; managed-memory SAXPY passed; distro unregistered; protected OpenFOAM state unchanged. | None. |
| F1 design | Accepted as blocked evidence | PETSc 3.26.0, petsc4Foam commit, ABI/build outline, managed-memory measures, guard, rollback, and benchmark contract are recorded. | Close dependency transaction through R217 preflight. |
| F1 benchmark contract | Accepted with amendments | Three serial arms: native GAMG, PETSc CPU, PETSc GPU. GPU must be at least 1.20x faster than native GAMG with separation, spread, convergence, field, force, determinism, power, and y+ checks. | A later operator ruling after dependency preflight and build review. |
| R217 dependency preflight packet | **Accepted under R220** | The repair commit `d5bb68fe` cleared P1 final cleanup/reap and P3 exact process identity. Astra independently reproduced the old failures and accepted all 15 repaired packet files at review commit `00933e65`. | Complete. |
| Run-1 environment contract | **Independent Astra review active** | Contract commit `754b23e0` binds all 15 accepted packet hashes, seven exact inputs, signer allowlists, execution identity, target paths, and live Mac epoch 6. Local regeneration, hashes, rootfs signature, key fingerprints, docs graph, and docs gate passed. | Astra must independently ACCEPT the exact commit before any WSL effect. |
| Live dependency preflight | Closed pending contract ACCEPT | No F1 distro import, apt transaction, package download, install, source acquisition, build, or case run has occurred. | Join Astra contract ACCEPT, then pass `contract.py verify` on a clean execution branch. |
| PETSc/petsc4Foam build and CFD benchmark | Closed | No execution evidence exists. | Separate operator ruling after preflight evidence. |

## Ruling 220 packet result

Ruling 220 authorized one additional cycle and only two changes:

1. **P1 cleanup:** a fresh bounded stop-and-reap check must run in a `finally` path after every cleanup-stage launch;
   nothing may launch after that check.
2. **P3 exact identity:** residual discovery must bind recorded PID/PGID and exact argv tokens; `runA` must never select
   `runAB`.

The author added red-first fixtures, repaired both defects, and passed the complete offline packet suite. Astra then
reviewed the exact packet independently and returned **ACCEPT**. The accepted source is
`d5bb68fe7db5e511ac46708e14564bb6a738db37`; the joined review is on the coordinator branch at `88ae5819`.

## Active gate: run-1 environment contract

Packet ACCEPT allowed the Windows coordinator to construct the real environment contract. Commit `754b23e0` binds
the accepted packet commit, seven input hashes, exact signer sets, execution branch/session, target paths, and live
Mac leader epoch 6. Astra is independently reviewing that immutable commit. After ACCEPT is joined and a clean-tree
`contract.py verify` passes, the R217 preflight may:

- import disposable distro `cfdw-petsc-cuda132-f1` at its dedicated path;
- freeze signed Ubuntu, OpenFOAM, and NVIDIA indices;
- simulate the complete zero-removal dependency transaction;
- download only the resolved `.deb` files to inspect licences, hashes, and `Installed-Size`;
- commit manifest-bound evidence; then delete the downloads and unregister the disposable distro.

The preflight remains capped at two cores, `nice 10`, 60 minutes, an 8 GiB VHD, and the recorded host-space floor.
It may not run `dpkg -i`, install packages, fetch PETSc or petsc4Foam source, compile, or run a CFD case.

## End-to-end route

```mermaid
flowchart LR
  A[CUDA capability proved] --> B[F1 design and benchmark frozen]
  B --> C[R220 P1/P3 repair complete]
  C --> D[Fresh Astra packet ACCEPT]
  D --> E{Independent contract review}
  E -->|REJECT| X[Stop before WSL; repair contract]
  E -->|ACCEPT| F[R217 dependency-only preflight]
  F --> G[Dependency and capacity evidence]
  G --> H{Later operator build ruling}
  H --> I[PETSc-CUDA plus petsc4Foam build]
  I --> J[Native GAMG / PETSc CPU / PETSc GPU benchmark]
  J --> K[Case-specific worth-it verdict]
```

## Coordination and proof

- GitHub draft handoff: PR #33, `win/gpu-f1-preflight-r217`.
- Mac Ruling 220 is on `main` at `63eb3e0f` and is merged into the Windows coordinator branch.
- Mac remains the designated leader at live coordination epoch 6.
- The accepted packet review is joined and pushed at `88ae5819`; the contract under review is pushed at `754b23e0`.
- Exact Windows readiness failures requested by Mac are posted on PR #33.
- The full repository readiness receipt remains RED on Windows due to a POSIX-only architecture-spike path, a
  temporary-file access failure, one desktop theme test, and the 240-second budget. These are recorded separately from
  the green R217 packet-specific offline gates.

## Confidence and limits

**Verified:** repository receipts, hashes, signatures, gates, and reviews establish every completed-stage claim above.
**Pending independent review:** the run-1 contract. **Not recorded:** F1 package closure, PETSc/petsc4Foam build
success, OpenFOAM GPU solver use, numerical equivalence, and speed-up. Those values stay unknown until their authorized
measurement stages execute.

