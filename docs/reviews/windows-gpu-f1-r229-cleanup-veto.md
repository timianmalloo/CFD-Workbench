---
id: review-windows-gpu-f1-r229-cleanup-veto
title: "Independent VETO of R229 cleanup candidate 115f3b05"
type: doc
status: blocked
owner: "@pc-f1-r225-attempt1-review"
phase: preflight
tags: [windows, cleanup, independent-review, veto]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  VETO of exact cleanup source eb1e0e64 and candidate 115f3b05. Production
  identity/absence checks accept additional registrations sharing reviewed
  storage paths. Three offline probes reproduce the missing uniqueness guard.
---

# Exact verdict

**VETO** candidate `115f3b05f5b4b4f8d13c22e9a7b40e1fbc62c519`, source
`eb1e0e64c5fcbc9a77ea10d8593e99e2912a0c1f`. No cleanup authority or activation
acceptance follows. The coordinator accepted the finding and directed this
exact VETO to be committed without repair. No WSL, registry read, lifecycle
action or GitHub message was performed by the reviewer.

Goal: independently accept or veto the exact cleanup-only candidate. Done when
bindings and safety boundaries are checked and the verdict is pushed. Tier T1,
no delegation. Graph: exact failure/source → bindings/negative controls →
verdict/audit/publication. The demonstrated hard veto ends acceptance work;
no broad gate result is represented as clearing it.

# Hard veto: storage-path identity is not unique

`cleanup.py:exact_row` checks the named registration's GUID, path and version,
but does not require its normalized path to occur exactly once among all rows.
`validate_identity` checks GUID uniqueness only. `validate_absence` checks target
aliases but accepts an extra registration sharing the protected path.

The committed independent `r229-cleanup-review-probe.py aliases` loads the
actual source and the committed post-failure registry fixture. It adds one
distinct name/GUID at a reviewed path. All three observations were accepted:

| Production function | Planted observation | Required behavior / observed |
| --- | --- | --- |
| `validate_identity` | Extra name/GUID using target BasePath | Refuse ambiguity / accepted |
| `validate_identity` | Extra name/GUID using protected BasePath | Refuse ambiguity / accepted |
| `validate_absence` | Target absent; extra name/GUID using protected BasePath | Refuse unproved protected uniqueness / accepted |

The probe exited 1 with `REVIEW-VETO: ambiguous registration paths admitted:
target-path-alias, protected-path-alias, protected-absence-alias`. This is a
measured admission defect. **Inferred destructive risk:** unregistering the
target may remove storage also referenced by an unreviewed registration. No
live experiment was performed. Stable repeated observations cannot resolve a
stable ambiguity.

Sibling evidence: existing `recovery/recover.py:validate_identity` explicitly
requires each reviewed normalized path to occur exactly once. The new cleanup
omitted that guard. The bounded repair needs path-uniqueness refusal at target
and protected admission, plus protected absence/invariance, with executable
negative controls against those production functions. The reviewer changed no
candidate source or tests.

# Verified bindings and positive evidence

| Artifact | Independently observed SHA-256 |
| --- | --- |
| Payload | `05779548b652f380222357df515766629d3bd096d7526be95af4a8c2c259a5fa` |
| Proposed authorization | `8cf8d84ab7049e681ae45670db78cd3662ffcb632a00a23a8c24387eff401de4` |
| Accepted projection | `fd0b7f668844a730a2623eff7a798d8b7a5c217533a48fd3039fd9e1859193cd` |
| Cleanup source | `9803961cd86bf4aff8f87da0ad4a0173df530e3717de02352e1acefcbe8c0a07` |
| Cleanup tests | `5f590743025b379680aad5fbc206acc75bc46c223cc54f8d3341f77ae1cd8ed2` |

All ten payload entries match working bytes and source Git blobs. Source is an
ancestor of the candidate. Canonical `cleanup/authorization.json` is absent.
Proposed authorization refuses; the projection passes the exact schema/scope
checker and differs only in status/ruling. The complete failure manifest
verified against the source: **85 files, GIT-BINDING-PASS**. The candidate's
eleven tests independently pass in 0.010 seconds despite the missing guard.

The target is `cfdw-petsc-cuda132-f1`, GUID
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`, version 2, path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`. The protected identity is
`cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
These match the committed failure registration and created-target evidence.

Source inspection confirms the fixed sequence: identity, owned-process refusal,
stable identity before terminate, terminate, stable identity before unregister,
unregister, stable absence and protected registration invariance. It has no
preflight retry or manual host-path deletion. Its 600-second budget includes
evidence binding. Protected registration invariance is the claimed scope;
this source does not produce a new completed-L3 content snapshot.

An empty Git diff from byte-restoration base `d6edbc63` through the candidate
covers attempt checkpoint/final evidence and historical `run-1`/`recovery-1`.
Earlier capture-byte restoration remains in history; this review does not
rewrite it or convert the R228 failure to success. These passing checks do not
clear the identity veto. A later exact amended candidate requires independent
review, and a separate accepted ruling/activation is required before live use.
