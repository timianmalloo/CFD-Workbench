---
id: coordination-openfoam-gpu-status-r220
title: "OpenFOAM GPU work status after R232 reader acceptance"
type: doc
status: blocked
owner: "@pc-gpu-status-r228"
tags: [coordination, windows, wsl, openfoam, gpu, cuda, petsc, ruling-232]
links:
  - { to: plan-win-openfoam-gpu-qualification, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: review-windows-gpu-f1-preflight-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  B2 CUDA capability is proved and F1 remains the selected route. The R230 cleanup action failed closed before any
  terminate or unregister operation. The bounded cleanup-helper repair, its Windows ring, and the Ruling-229 preflight
  reader repair are independently accepted. Both candidates remain review-only and await a new exact Mac-leader ruling.
---

# OpenFOAM GPU work status after R232 reader acceptance

**Snapshot:** 2026-10-13 05:44 UTC. Primary Windows checkout and this status branch include `origin/main` at
`64dbed61`. Mac remains repository leader.

## Current result

CUDA capability through WSL remains proved. B2 installed the pinned CUDA 13.2 toolchain in a disposable distro and ran
managed-memory SAXPY on the RTX 3080 Ti Laptop GPU with zero mismatches, then rolled the distro back. F1 remains the
selected PETSc-CUDA plus petsc4Foam qualification route. No PETSc/petsc4Foam build or GPU OpenFOAM benchmark has run.

R228 dependency-preflight attempt 1 failed closed after `probe` and `repositories`. Ruling 230 then authorized one
cleanup-only action. That action also failed closed, at the owned-stage evidence check, before terminate or unregister.
No second cleanup action ran. The exact target was last verified registered before that stopped action; its current
runtime state has intentionally not been re-read without a new ruling.

The failure review identified an offline loader defect in the cleanup helper. The final bounded repair is
`37b96e4749a13d2fee3a7d73444d66a6a39a6403`. It refuses missing, file, and unreadable evidence roots before process
inspection. Repair cycle 2 of 2 passed focused tests 13/13, the Python union 94/94, documentation checks, and safe gates
10/10. Astra accepted the helper subject to the Windows ring.

The corrected ring evidence is `73cf89c667386225fe646115a8191e6bedc067bf`. Astra independently accepted it at
`bc4ba58203fe9f652127e071fc0e8e160695d9ac`: Desktop 776 passed, 33 manifest-expected, zero unexpected; all 18 child
modes and 15 partitions completed; the other suites and Python 94/94 matched. Both capture manifests verify 85/85 and
6/6, and all 91 capture blobs are unchanged. The run took 207.380 seconds and retained 16 advisory cost misses.

The prior HOLD is cleared. The helper is **PREPARED-UNAUTHORIZED**. A new exact Mac-leader ruling is the next gate.

The Ruling-229 preflight reader repair is `70d022bf4369c8cb58888f88e6efba00070f35db`, independently accepted at
`8ee609f355ce002e59b2d06f459d6f8ff0b0a8f6`. Both live guard-file readers now use explicit `ReadWrite|Delete`
sharing. PowerShell 5.1 and 7 behavioral probes, reader controls, safe gates 10/10, documentation, and privacy checks
pass. Exactly the two F1 exceptions were removed from the control; the seven immutable B1/B2 proof entries remain.

## Stage ledger

| Stage | State | Verified result | Next gate |
|---|---|---|---|
| G0/G1 route inspection | Complete | Installed v2512 does not establish whole-solver GPU offload; native-fused nvc++ route is closed on this machine. | None. |
| B1 inventory and guard | Complete | Read-only machine/package inventory and completed-L3 guard evidence accepted. | None. |
| B2 CUDA capability | Complete and rolled back | CUDA 13.2 managed-memory SAXPY passed on the RTX 3080 Ti Laptop GPU. | None. |
| F1 design | Selected | PETSc-CUDA plus petsc4Foam and a three-arm benchmark remain the qualification route. | Dependency closure, build, then benchmark rulings. |
| R228 dependency preflight | Failed closed; attempt 1 consumed | Only probe and repositories ran; the reader-sharing repair and capture-byte control are now accepted offline. | Re-bind the repaired packet before a separately ruled attempt 2 or 3. |
| R230 cleanup-only action | Failed closed; authority consumed | Stopped at owned-stage evidence validation; no terminate or unregister ran. | New exact ruling before any new live action. |
| Cleanup helper repair | Accepted; review-only | Candidate `37b96e47`; repair cycle cap 2/2 consumed. | Mac leader decides whether and how it may be activated. |
| Windows ring | Accepted | Desktop 776 pass, 33 expected, 0 unexpected; all child modes and partitions complete. | None for the review-only candidate. |
| Preflight reader repair | Accepted; review-only | Candidate `70d022bf`; Astra review `8ee609f3`; explicit `ReadWrite|Delete` sharing. | Include it in the Mac leader's next packet/ruling decision. |
| Exact target cleanup | Not authorized | No post-R230 target-state read or absence proof exists. | Fresh ruling, activation, identity and ownership admission, then exact action if authorized. |
| PETSc/petsc4Foam build and benchmark | Not run | No build, CFD case, accuracy comparison, or speed-up evidence exists. | Separate authority after dependency preflight succeeds. |

## Next actions

1. Mac reviews PR #34 comment `6138580964` and issues a new exact ruling.
2. Windows fetches and merges that ruling from `origin/main`, then verifies its byte-exact bindings.
3. If the ruling authorizes cleanup, capture fresh leader activation, exact identity, and ownership admission before the
   first WSL command; execute only the named action and stop on any mismatch.
4. Commit the cleanup receipt and obtain independent review.
5. Re-bind the accepted reader repair and capture-byte control into a fresh dependency-preflight packet before attempt 2 or 3.
6. After dependency closure, request separate build authority for PETSc-CUDA plus petsc4Foam and run the native GAMG,
   PETSc CPU, and PETSc GPU comparison.

## Authority boundary

The accepted helper and ring evidence do not authorize a live action. Ruling 230's one cleanup action is consumed. No
current WSL, distro, or process state is asserted beyond the last committed evidence. The Windows session will not
terminate, unregister, install, build, or benchmark until the Mac leader commits a ruling that names the allowed action
and its exact bindings.

## Confidence and limits

**Verified:** B2 CUDA capability and rollback; F1 route selection; R228 stopped after two stages; R230 cleanup stopped
before terminate/unregister; helper candidate `37b96e47`; corrected evidence `73cf89c6`; independent ACCEPT
`bc4ba582`; reader candidate `70d022bf`; reader ACCEPT `8ee609f3`; full ring counts and manifest bindings; no new live
action was run.

**Inferred:** the loader defect explains the offline helper failure class. Live causation was not observed because the
failed action stopped before process inspection.

**Flagged / not recorded:** current target registration, target absence, dependency-preflight attempts 2 or 3,
dependency closure, PETSc/petsc4Foam build, GPU solver execution, numerical equivalence, and speed-up.
