---
id: review-windows-gpu-f1-astra-r2
title: "Astra second and final Ruling 211 design review"
type: proof-pack
status: complete
owner: "@pc-f1-astra-r211-review2"
phase: design
tags: [windows, gpu, petsc, ruling-211, owner-review]
links:
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: proof-win-gpu-f1-dependency-worksheet, rel: relates-to }
  - { to: review-windows-gpu-f1-astra-r211, rel: relates-to }
  - { to: coordination-windows-gpu-f1-design, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  Second VETO: F1-ASTRA-1 remains open. The repaired inventory corrects payload provenance but does not supply the combined transaction or finite F1 capacity. The track ends; only a narrow preflight decision goes to Mac.
---

# Second and final owner verdict

**VETO — F1-ASTRA-1 is not cleared. This second veto ends the track.**
The reviewed base is `9069cd0ee5316ebf8c25c7d2a84a23decb89c217`. The repaired design is an honest blocked proposal,
not the exact, closed bill of materials and capacity package required by [Ruling 211](../notes/rulings.md), lines
1663–1665. The [coordination plan](../coordination/windows-gpu-f1-design.md), line 85, ends this track on the second
VETO. No further author repair is requested under this plan.

## Independent check of the repair

| Finding | Observed evidence | Verdict |
|---|---|---|
| Combined package/dependency closure | [Worksheet](../proof/win-gpu-f1-design/dependency-worksheet.md), lines 48–66, supplies five direct OpenFOAM rows and lists additional dependencies. Lines 72–75 still lack exact Fortran, MPI and BLAS/LAPACK provider/version/hash/size/licence records. Lines 117–127 request a future transaction simulation; no resulting complete transaction appears in this worksheet. | **Verified: remains open.** Five direct rows and the separate 135-row CUDA lock do not establish the complete F1 transaction. |
| Signed-index and licence closure | Worksheet lines 39–46 identify publisher index bytes while explicitly recording unverified key/signature authentication. Lines 50–54 leave binary notices Flagged; lines 122–126 propose later archive inspection. | **Verified: remains open.** Recorded advertised hashes are not independently verified binary hashes or authenticated repository evidence. |
| Finite F1 installed/capacity requirement | Worksheet lines 93–108 contain known subtotals and missing source, staging, build and case operands. Lines 109–113 have no finite guest or host/VHD threshold. The revised design's capacity section retains this absence explicitly. | **Verified: remains open.** The separate 19,684,091,961-byte proposed preflight host threshold at worksheet lines 129–134 is not a full F1 capacity requirement. |
| B2 payload provenance correction | Revised design line 53 and worksheet lines 32–35 distinguish 8,092,712,328 measured archive-member payload bytes from advertised Installed-Size and filesystem use. The B2 producer `docs/proof/win-gpu-b2/verify-debs.py`, lines 38–63 and 97–123, measures archive members. | **Verified: corrected.** This closes the first review's factual correction, not the dependency/capacity veto. |
| Authorization and claims | Worksheet lines 117–127 label the next stage a decision request and exclude installation, PETSc/adapter source acquisition, build, case execution and protected-distro commands. The design diff retains its execution prohibition and scoped pressure-path benchmark. | **Verified: boundary preserved.** No speedup, whole-solver offload or F1 runtime acceptance is established. |

These findings follow the actual missing rows and operands, not merely the author's VETO label. The worksheet's
known subtotals are useful evidence and remain explicitly partial. This review does not require an unauthorized
build to settle those missing design outputs.

## What remains a later execution gate

The [first review](windows-gpu-f1-astra.md) accepted the proposed containment/rollback, managed-memory attribution,
real-case numerical/performance and observer/ownership contracts. The repair diff does not change those contracts;
that acceptance remains limited to design. Actual compiler/header availability, configure/linkage results, CUDA
vector behavior, profiler coverage, field equivalence, speedup and rollback read-back remain **Flagged runtime
evidence** until their corresponding stages are authorized and observed. No current gate result substitutes for them.

## Only permitted handoff

Mac remains leader and decision owner at carried epoch 2. Send Mac/Fable the worksheet's **narrow metadata/preflight
decision**, then the operator decides whether to authorize it. Its requested scope is a fresh disposable distro,
authenticated frozen package indexes, exact rootfs/package-state capture and a zero-removal simulated transaction;
any acquisition of exact resolved package archives solely for notices/payload inspection must be expressly included
or remain closed. The operator may decline that acquisition and leave the corresponding gaps unresolved.

The proposed preflight ceiling is a design choice, not an observed or enforced bound. A later authorized runner must
make its identity, capacity stop, ownership/cleanup and evidence rules concrete before it runs. This review does not
approve that runner or any preflight action. It does not open installation, PETSc/adapter source download, build,
benchmarking or access to protected L3. Mac records the decision through the ruling route; no local execution follows
this second veto.

## Frozen inputs and review scope

| Input | SHA-256 |
|---|---|
| `docs/plans/windows-gpu-f1-r211.md` | `c727a8dbb6eb3393124ebcd3dc65153645f4546ea2df26a3d1c9a1bebebd52ec` |
| `docs/proof/win-gpu-f1-design/dependency-worksheet.md` | `d8d3881c7831e9f094748fd94121c87794f402e08affd0561b03e44bb8d8fe70` |

Review work consists of repository reads, local arithmetic checks and this new review with its required index/audit
updates. No network download, WSL action, installation, build or case execution occurred. The first review, design
and worksheet remain unchanged. The closing audit entry and commit handoff record measured duration and local gate
results; integrated readiness remains the coordinator's join responsibility.
