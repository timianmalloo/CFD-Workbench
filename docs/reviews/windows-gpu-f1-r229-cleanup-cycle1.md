---
id: review-windows-gpu-f1-r229-cleanup-cycle1
title: "Independent VETO of R229 cleanup repair cycle 1"
type: doc
status: blocked
owner: "@pc-f1-r229-cleanup-review1"
phase: preflight
tags: [windows, cleanup, independent-review]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: plan-windows-gpu-f1-r229-cleanup-review1, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Independent VETO of candidate 6946da40: the required ruling-citation gate
  fails on its README. Repaired source ad4cedbb closes all three prior
  production path-alias failures. No live authority follows.
---

# Verdict and authority boundary

**VETO** exact candidate `6946da407fe0e3412aa22b205a56debac1a82d35`, repaired
source `ad4cedbbd34f4d0178576330c98072c84f4dcc69`, because its required
ruling-citation gate fails. The previous identity VETO remains valid for
`115f3b05` / `eb1e0e64` and is
preserved at review commit `e3929771d53d5b4ba83bd8c9c7602ffb87faecf5`.

This review grants no live authority. Mac/Fable must accept exact R229 bindings
on main; canonical activation must then equal the accepted projection and
receive a separate independent rehash. Canonical `cleanup/authorization.json`
is absent in this candidate. No WSL, registry read, lifecycle action or GitHub
message was performed. Candidate source, tests and failure evidence were not
edited by the reviewer.

# Blocking required gate

The configured safe bundle returned exit 1: **9 of 10 executed gates passed**,
with the three application/store skips prescribed in `docs/coordination/join.json`.
The independent direct rerun of `verify-ruling-citations.py` also returned exit 1
and identified exactly one file:
`docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/README.md`. The candidate
references the requested ruling as authority without a corresponding heading
in `docs/notes/rulings.md`. This is the gate's measured finding; it does not
mean a live action occurred. The candidate remains proposed and inert.

The coordinator directed this exact VETO to be published without repair.
The bounded disposition is to reconcile the proposal's authority wording and
rerun the existing gate on a later exact candidate; no scanner exemption or
fabricated ruling is authorized by this review. The green safety evidence below
does not clear this required gate. Graph validation reports zero defects and
retains 134 existing review suggestions. The privacy/machine-path checks pass.
`tools/check-docs.py` completed with exit 0 and "Documentation checks passed";
that result does not override the separate ruling-citation failure.

# Verified repair and independent controls

The production diff adds normalized target/protected path multiplicity checks
to `validate_identity` and protected GUID/path multiplicity checks to
`validate_absence`. All sequence, scope, helper and evidence-binding behavior
is unchanged. Source is an ancestor of the candidate.

The committed `r229-cleanup-cycle1-probe.py` loads real production functions and
the committed failure registry fixture. It executes the old source from its Git
blob, preserving a red/green comparison independent of the author's tests:

| Case | Old source eb1e | Repaired source ad4 |
| --- | --- | --- |
| Extra name/GUID at target path during admission | Admitted | Refused |
| Extra name/GUID at protected path during admission | Admitted | Refused |
| Extra name/GUID at protected path during absence | Admitted | Refused |

The repaired functions also refuse these aliases with expanded variables,
uppercase, extended path prefix and trailing separator. Exact fixture identities
pass. Stable identical absence observations pass; a target reappearance refuses
without writing absence proof. Expired deadline refuses. The existing recovery
reader's path-uniqueness guard is now represented on the new cleanup's admission
and absence surfaces; no further sibling repair is required for this finding.

All fourteen author tests pass independently (0.008 seconds). They cover identity
and absence mutations, first-failure stopping, exact lifecycle argv, authorization,
ruling tokens, manifest invocation and read-only helper scope. The independent
probe also rejects an invalid value for every authorization field. Missing
canonical authorization, proposed authorization and the noncanonical accepted
projection all refuse in production `precheck` with zero subprocess launches.
The probe completed in 0.921 seconds of measured tool wall time; its ring is
offline-fast. A valid source SHA is additionally bound by payload/source blobs,
not by the authorization schema check alone.

# Exact bindings and conservation

| Artifact | Independently verified SHA-256 |
| --- | --- |
| Payload | `dd41827d0e26b9452bcbe4c9040d23f417449a43e55ae0863e9270a05e154370` |
| Proposed authorization | `e4806d0188185bc76a59c5ee06f39e639d597e4e09e5c1c486b00c417d735211` |
| Accepted projection | `82799f9938781b9f2aed94988dde9b5ab90cb61f9262d667eba6ec4796972e71` |
| Cleanup source | `148e996cf93b23b9d4f12dd24880b6838550d4d2d734ca23f2fdd11faf08a904` |
| Cleanup tests | `ff4f00d81b2db638ddcb256a716f71c849335dafc5a593275a2679e1e0768941` |
| Owned-process helper | `6b1a9757dadd4fffb8525e8acad85cb10322cc801758840830eee50fe36fde6b` |
| Full failure manifest | `7d89c6d6a4577deac44d507390a680b38f687f6eed6e784f7a577210086e72c6` |

All ten payload entries match working bytes and source Git blobs. Payload and
authorization bytes also match the candidate Git blobs. Proposed and projection
objects differ only in status/ruling. The complete failure manifest returned
`GIT-BINDING-PASS` for **85 files** against source ad4cedbb.

The target fixture and payload bind `cfdw-petsc-cuda132-f1`, GUID
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`, version 2, path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`. Protected identity is
`cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
The bound run remains `r225-attempt1-epoch8-20261012`, cleanup session
`pc-f1-r229-cleanup1`, branch `win/gpu-f1-r228-cleanup`, Mac epoch 8.

Recursive Git diffs are empty for attempt checkpoint/final and historical
run-1/recovery-1, both from byte-restoration base d6edbc63 and old source eb1e
through this candidate. The failed R228 outcome is not rewritten.

# Reachable operations and limits

Inspection confirms authority and stable identity prechecks, read-only residual
owned-process refusal, stable identity before each exact-target terminate and
unregister, stable target name/GUID/path absence, protected registration
invariance and separate frozen evidence/binding commits. The helper reads the
exact original run's ownership records and refuses residual groups or unresolved
stage intents; it sends no process signals. Deadline remains 600 seconds.

No preflight retry, package download/install, source fetch, build, case,
benchmark or manual host-path deletion is reachable. Failure stops the sequence;
the executor attempts to freeze/bind its classified outcome within the shared
deadline. Deadline exhaustion or an external Git failure can prevent receipt
commit; this offline review cannot promise that commit under those conditions.
Protected invariance here means registration identity and path presence, not a
new completed-L3 content snapshot. Live success remains unmeasured.
