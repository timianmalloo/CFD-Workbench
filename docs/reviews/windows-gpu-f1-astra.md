---
id: review-windows-gpu-f1-astra-r211
title: "Astra owner review of the Ruling 211 F1 design"
type: proof-pack
status: complete
owner: "@pc-f1-astra-r211"
phase: design
tags: [windows, gpu, petsc, openfoam, ruling-211, owner-review]
links:
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: coordination-windows-gpu-f1-design, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  VETO on design completion: the combined dependency lock and capacity closure required by Ruling 211 remain absent. Execution stays closed; the measurement, benchmark and containment proposals are acceptable design contracts.
---

# Astra independent owner verdict

**VETO — the frozen design is not yet the complete Ruling 211 package for operator approval.**
Reviewed joined base `322abf17a019298db5ce111c896915e7107a4650`, specifically
`docs/plans/windows-gpu-f1-r211.md` (SHA-256
`ab6d0039f00c0828fdfff7318f256a769953e9d9b4b373baa73462967686c158`).
The author correctly labels the unresolved evidence and keeps execution closed. Those labels make the document safe
to send to Mac/Fable as a **blocked decision request**, but do not satisfy the exact bill-of-materials and installed-size
deliverable in [Ruling 211](../notes/rulings.md), lines 1663–1665. This verdict authorizes no download, installation,
build, WSL mutation or case execution.

## Blocking finding and clearing evidence

**Verified F1-ASTRA-1 — dependency and capacity closure are deferred, not delivered.**
The [design](../plans/windows-gpu-f1-r211.md), lines 52 and 56, leaves the OpenFOAM/dev/toolchain transitive package
versions, hashes, signed indexes, licence notices and installed sizes unresolved. Its lines 74–80 give a formula with
unknown operands, not the installed size or a bounded total capacity requirement. Lines 252 and 254 explicitly retain
these as F1-1 and F1-3. The accepted CUDA-only lock is not a lock for the combined F1 environment. This is a missing
design output, independently of whether the eventual adapter build succeeds.

To clear this veto, provide the exact combined, architecture-specific dependency inventory against the selected
rootfs, with source/index provenance, versions, hashes, licence mapping and package-size totals. Identify the MPI,
BLAS/LAPACK and compiler providers and the proposed ABI contract. State finite guest-space and host/VHD thresholds,
including source, unpack, build-output and case reserves. An unmeasured build-output reserve may be a conservative,
explicitly **Proposed** ceiling with a stop rule; it must not be called measured. Fresh free-space readings and actual
installed sizes remain execution-time checks. If read-only metadata cannot close a dependency or transaction question,
name the smallest separate metadata/preflight authorization needed and ask Mac for that decision; do not present the
current document as an already closed installation plan.

**Verified factual correction required with the repair:** design line 53 calls 8,092,712,328 bytes
"installed KiB-derived payload bytes". The B2 producer [verify-debs.py](../proof/win-gpu-b2/verify-debs.py), lines
38–63 and 97–123, measures archive payload bytes, then totals them at lines 153–176. That quantity is neither a
KiB-derived installed-size total nor measured filesystem consumption. Preserve its actual provenance in the F1
capacity worksheet.

## Decisive checklist

| Ruling 211 obligation / hard-veto surface | Verdict and observed evidence | Remaining gate |
|---|---|---|
| Exact dependency, source, ABI and build closure | **VETO**, as above. Design lines 50–68 pin the rootfs, CUDA lock, PETSc tarball and adapter commit and propose a coherent scalar/index/compiler contract; lines 52/56 still omit the combined lock. | Metadata-resolvable pins, dependency providers, build command contract and bounded sizes must precede approval of the affected first stage. Successful compile/configure/linkage cannot be required before build authorization. |
| Integrity and licences | **ACCEPT as explicitly blocked proposal**, not acquisition permission. Lines 51–56 separate signed indexes/rootfs verification from a locally measured PETSc digest and unsigned adapter commit. No upstream signature is invented. F1-2 requires accepted provenance before source acquisition. | Mac/operator must accept a concrete provenance rule or require stronger upstream authentication before download; every dependency needs its licence mapping in the closed inventory. Post-fetch byte/tree verification precedes build. |
| Capacity | **VETO** under F1-ASTRA-1. Lines 72–80 distinguish the earlier B2 requirement from the missing F1 total, but do not fill the total. | Freeze conservative finite capacity bounds before the relevant import/install decision; measure host and guest capacity immediately before execution and stop on shortfall. |
| Disposable containment and rollback | **ACCEPT design contract.** Lines 89–99 select a new exact distro/install path and prohibit using the protected distro; lines 134–138 retain proof and require identity/protected-state checks before separately authorized unregister. | Later runner must prove exact target identity, owned process absence, protected snapshot equality and post-unregister absence. Existing B2 rollback is precedent, not F1 execution evidence. |
| `concurrent_managed_access=0` attribution | **ACCEPT measurement plan.** Lines 147–173 separate allocation, conversion/preparation, transfers, migration, kernels and synchronization; missing channels become `not recorded`, not zero. Diagnostic tracing is excluded from benchmark timing. | Source-bound spans, trace positive controls and supported migration channels must pass before attribution/benchmark admission. B2's attribute and SAXPY timing are not evidence of PETSc memory behavior. |
| One real-case numerical pass/fail contract | **ACCEPT proposed contract.** Lines 175–208 select `cases/win-spike04r3-g0-l6.yaml`, fixed 8000 iterations, identical physical inputs, CPU/GPU pressure arms, endpoint convergence, finite cell-field comparisons and force tolerances. | Freeze exact input/configuration hashes and obtain CFD-owner tolerance approval before any case run; qualify actual matrix/vector behavior and fail closed if the selected GPU path is invalid. |
| Performance pass/fail | **ACCEPT proposed contract.** Lines 194–216 define warmups, five trials per arm, fixed order, limited invalid-pair replacement and median end-to-end wall speedup of at least 1.20 with numerical/guard/device proof required. | Runtime timing/oracle evidence. Pressure-kernel gains alone cannot pass; no performance result exists in this design. |
| Observer, ownership and missing-data behavior | **ACCEPT design contract.** Lines 219–244 require an independently owned observer, full protected pre/post comparison, five-second samples, UTC/monotonic evidence and prefix binding, acknowledged owned groups and verified cleanup. An active L3 state invalidates the completed-run contract and requires a new ruling; missing critical telemetry stops. | Freeze exact runner, resource sampling/threshold definitions and approved observer mode before execution. Prior completed-guard authority does not automatically authorize F1. |
| Authorization and scope | **ACCEPT.** Lines 25–30 and 248–261 keep stage prerequisites closed until a second operator ruling; Ruling 199's native-fused route remains closed. Only the pressure linear-algebra path is proposed. | Mac/Fable review precedes operator decision. No whole-solver GPU residency, application speedup, macOS qualification or backend-selection claim is established. |

## Primary-source disconfirmation and evidence limits

**Verified source observation:** the pinned upstream
[petscSolver.C](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/src/petsc4Foam/solvers/petscSolver.C)
creates vectors from its matrix (line 195), uses host `VecPlaceArray` for solution and right-hand side (280–281), and
calls `KSPSolve` inside a logged stage (288–290). This supports the design's demand for actual type, transfer and kernel
evidence; selecting a CUDA matrix type is not observed offload. The pinned
[README](https://gitlab.com/petsc/petsc4foam/-/raw/cb1badc4978306e7001a4654dcdf53f284a92365/README.md)
and [PETSc installation contract](https://petsc.org/release/install/install/) were read as primary metadata. Their
existence is not a v2512 compatibility test.

**Verified prior-proof limit:** [B2 receipt](../proof/win-gpu-b2/receipt.md), GPU result and rollback sections,
records a managed-memory toy, unavailable concurrent managed access, successful disposable rollback and unchanged
protected projection. It expressly does not prove PETSc or OpenFOAM offload. This review makes no speedup claim.

**Flagged execution evidence, not reasons to violate Ruling 211 now:** actual compiler/header availability, configure
output, OpenFOAM/MPI/PETSc linkage, CUDA vector behavior, profiler support, numerical equivalence, performance and
rollback read-back can only be established after the corresponding operator authorization. A failed later gate stops
its stage; it cannot be filled with an inferred success.

## Handoff and verification scope

Return once to AUTHOR for the concrete missing BOM/capacity output and payload-provenance correction. Mac remains
leader and decision owner at carried epoch 2. A second VETO ends the track under the coordination plan. The design may
be shown to Mac now only with this blocked verdict; there is no local authority to soften the execution boundary.

Review operations were repository reads and primary-source metadata reads. No WSL command, package/input download,
installation, build or case run was performed. The coordinator must run final join/readiness gates on the integrated
head. This review's commit receipt reports its local documentation checks and verifies the design hash unchanged.
