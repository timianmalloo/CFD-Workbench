---
id: review-windows-gpu-f1-recovery-integration
title: "Astra independent F1 recovery integration review"
type: proof-pack
status: complete
owner: "@pc-f1-r217-recovery-integration-astra"
phase: preflight
tags: [windows, wsl, recovery, integration, independent-review]
links:
  - { to: proof-win-gpu-f1-recovery-integration, rel: depends-on }
  - { to: review-windows-gpu-f1-cleanup-recovery-cycle1, rel: depends-on }
  - { to: review-windows-gpu-f1-live-defect-repair, rel: depends-on }
  - { to: review-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  ACCEPT exact integrated candidate 5b1a9bb4 for offline source integration.
  Both accepted repairs, retained evidence and parent records are conserved.
  Combined offline checks pass. No cleanup or retry authority is granted.
---

# Independent verdict

**ACCEPT** `5b1a9bb460df4ae2955a3bb922b0e68138492cc0` for the scoped
offline integration of the accepted packet repair and cleanup recovery.
No integration blocker was found. This verdict grants no cleanup, preflight
retry, live execution or broader stage authority. The historical VETO remains
unchanged and valid for its original candidate.

**Verified; high confidence:** exact parents, in order, are
`02485495951fbd04ea07a2e8bc276b0df4ab93cd` and
`65456e1cc858ff62b87c7a8d7030a6e83715b354`; their merge base is
`6445bb6e1c6b9f27969724c711cc2ff322f01867`. The reviewer inspected the
integration helper and reconciliation code, recomputed conservation independently,
and reran the combined offline proofs in a separate worktree. Implementation,
tests, authority and original failed-run evidence were not modified. No WSL
invocation, recovery entry point, guest deletion, distro lifecycle action,
download, install, source acquisition, application build or CFD workload ran.

## Exact source and behavior conservation

The candidate's [source map](../proof/win-gpu-f1-preflight/integration/source-hashes.json)
was verified without rewriting it. All 15 packet files have the intended origin:

| Surface | Observed origin and semantic check |
| --- | --- |
| Thirteen packet files | Byte-identical to the accepted packet-repair parent `65456e1c`. This includes the PowerShell checkpoint/finalizer changes and strict URI transaction handling. |
| `preflight.sh` | Byte-identical to cleanup parent `02485495`. Its only delta from the original shell is the reviewed `delete-debs-only` case. Bytes preceding and following that case remain identical. |
| `packet_selftest.py` | Exact deterministic three-way union of both accepted parents. Independent AST inspection confirmed both parents' ordered `main()` statement sequences remain present, including invoked URI and cleanup-scope fixtures. |
| Recovery helpers and tests | All three helpers and the 35-test suite are byte-identical to accepted cleanup parent `02485495`; its earlier cycle-1 review remains applicable to those bytes. |

The URI repair decodes the final raw URL segment exactly once with strict percent
handling, rejects separators, controls and malformed encoding, and preserves the
raw URL, exact cache name, size and uniqueness checks. The PowerShell repair uses
the canonical checkpoint path for freeze and publication; renamed `*Action`
callback parameters retain aliases while avoiding dynamic-scope collision.
The actual AST-body tests execute these boundaries with external actions mocked.
Both Windows PowerShell and PowerShell 7 passed them on the integrated candidate.

The recovery CR-1/2/3 controls survive integration: deadline admission precedes
both child-launch sites and their immediate launch checks; final absence examines
the admitted target GUID even after rename or move; and initiating deletion,
ownership, absence and publication failures retain deterministic ordering and
public redaction. The unchanged 35-test suite passed, including the prior
counterexamples, guard/final-binding expiry, intent/acknowledgement, identity
stability, protected-path and pinned cache-deletion fixtures. Prior red-baseline
observations remain documented in the unchanged cycle-1 review; this integration
review reran candidate tests rather than claiming a new baseline replay.

Host atomic intent precedes every child launch. Host PID acknowledgement follows
launch and is synchronous; it is not an atomic replacement. Guest intent precedes
WSL launch, and the guest entry acknowledgement is atomic before helper execution.
The distinction remains important: a failed acknowledgement prevents success but
does not prove that an already launched child never ran. Fresh completed-L3
monitoring remains active across delete-debs, terminate and unregister. Deadline
incompleteness cannot emit a successful final binding.

## Parent-record and evidence conservation

**Verified; high confidence:** the repository-contained
[integration verifier](../proof/win-gpu-f1-preflight/integration/verify_integration.py)
passed in 13.153 seconds. Its local register helper was independently inspected
and pinned to Git blob `caa877ce432571df602f3a84a2ddcd6dd9983d1b`.
No shared primary-checkout merge helper or merge configuration was invoked or
changed. The check recomputes the exact union from the two parents and merge base,
then compares it to the candidate's register, with the single recorded integration
conservation lesson as the explicit addition.

Independent probes confirmed **212 unique parent register entries exactly once**,
including their retained body lines. Removing one parent entry in memory makes
the conservation predicate fail. The register helper's own self-tests, including
deletion and reorder cases, and the merge-binding check also passed. Full JSON
comparison independently found **1,030 unique parent audit records preserved
exactly once** in the candidate's 1,033 records; none was changed or duplicated.

Both historical payload trees and committed manifests remain unchanged:

| Retained surface | Observed identity |
| --- | --- |
| Original checkpoint payload | 75 original files; tree `0e1054281495e3677b1948b1a9bc5ab15a0e9bb0` |
| Original failed-live payload | 93 original captures; tree `b08d17f2eeced7e2291bf2fb262d8b44cddf441b` |
| Checkpoint manifest SHA-256 | `76ab6e220a21c10c694a7eecd9d00679d1a0a42b3444598dae2682ac627f0bea` |
| Failed-live manifest SHA-256 | `56470fa05eaeca14327ce1e63a4f76f3127b2dcbbe3a94d093684037197f8e93` |
| Historical invocation contract SHA-256 | `f109d4f253f436d8b178e40ea5af497316faf36b0c4398c8a598abc52748336c` |
| Historical VETO SHA-256 | `2be0ba9c514056cac232126c8cb4f13fa57f618623414f81ef8ec94efea0214b` |

Actual `manifest.py verify` calls bound the exact integrated commit to **76**
checkpoint blobs and **94** failed-live blobs, including each committed manifest.
These are the original 75/93 payloads plus their manifests, not additional live
captures. The integration verifier also conserved all ten historical binding,
ruling and review artifacts, including both accepted independent reviews.

## Authority and merge-only seam probes

The old invocation contract is deliberately unchanged. Independent calls to its
actual hash checker reject the integrated bytes of `Control.ps1`,
`Invoke-Preflight.ps1`, `README.md`, `SelfTest.ps1`, `packet_selftest.py`,
`preflight.sh` and `transaction.py`. Historical permission cannot silently admit
the repaired packet.

An isolated, schema-valid authority fixture was supplied to the actual recovery
`precheck()` with only host-command boundaries mocked. It refused the missing new
committed numbered Mac ruling before any WSL boundary. No fixture was installed
as authority. The integrated candidate contains no new cleanup ruling. The
separate recovery contract still binds packet and recovery bytes, retained
manifests, target and protected identities, expiry and the explicit operation
allowlist. Manifest verification remains before any cleanup action.

The merged `delete-debs-only` case invokes `../recovery/check_cache.py`. Recovery
admission binds that helper as well as the packet. The historical preflight
contract covers `packet/` and cannot alone establish this outside-packet helper's
bytes for a future preflight retry. Any future retry approval must explicitly
review and bind this dependency along with its new attempt/environment contract.
That is a future admission requirement, not permission conveyed by this review.

## Offline checks rerun

| Check | Observed result |
| --- | --- |
| `tests/test_f1_cleanup_recovery.py` | 35/35 passed, 1.046 seconds |
| Packet Python self-test | Passed, 0.549 seconds; both merged fixture sets execute |
| Packet PowerShell self-test | Passed on Windows PowerShell, 0.402 seconds, and PowerShell 7, 0.279 seconds |
| Git Bash syntax | All four packet shell scripts passed `-n`; no WSL used |
| Authority, transaction, manifest and completed-guard self-tests | Passed, including planted authority negative |
| Exact candidate frozen binders | Passed, 76 checkpoint and 94 failed-live blobs |
| Integration verifier and independent probes | Passed; parent sources, AST sequences, register/audit conservation and authority refusal observed |
| Local register helper and merge bindings | Passed |
| Ten safe verification gates | Passed; application-core, application-adapters and Windows-store gates explicitly skipped to preserve the no-build/no-live scope |
| Proof privacy | Passed: zero Windows user paths or machine SIDs, one existing allowlist item; 365 binary files skipped by the scanner |
| Documentation graph | Passed with zero defects; 134 existing review suggestions remain advisory |

The documentation check and final graph derivation are recorded in this review's
closing audit. Tests are offline regression proof; they do not demonstrate a
successful recovery on the retained machine state.

## Residual risk and boundaries

**Verified source limitation; live behavior unverified:** repeated stable registry
and path observations reduce detectable change but cannot make observation atomic
with name-based WSL lifecycle calls. An external actor could change registration
after the last observation. Pinned no-follow directory handles and checked
ancestry/mount/file identities contain ancestor redirection, but checking a file
entry and unlinking that relative name are not one atomic operation. Exclusive
machine access is not mechanically enforced. These known windows require an
explicit operator risk decision before any separately authorized live action.

Actual WSL teardown, Linux descriptor behavior, protected-distro runtime
invariance and post-deadline live cleanup remain unmeasured. Expiry or failed
publication can leave unknown work or incomplete evidence; the program must
remain failed closed in that state. Public evidence redacts classified failures,
but the privacy scanner does not inspect binary payloads. A future recovery must
publish its own receipt and commit without changing the frozen failed attempt.
No retry, simulation, download, installation, source acquisition, build or CFD
authority is implied by offline integration acceptance.

## Review execution graph

Goal: decide the exact integrated source, evidence and authority boundaries.
Done when: an independently supported ACCEPT/VETO is committed and pushed with
the audit and derived index. Not in scope: implementation repair or live action.
Tier T2; fan-out cap zero. Adversarial Security, Test Architect and SRE lenses were
applied inline; the reviewer did not author the candidate being accepted.

Planned and actual dependency graph: exact-parent/source comparison and seam
inspection; then independent register/audit/evidence and authority probes; then
combined offline suites and safe gates; then verdict, audit, derived graph and
publication. Independent reads and tests were batched. There was no repair loop,
source change or expanded execution scope. The remaining step after this review
is the coordinator's disposition; any cleanup or retry requires new authority.
