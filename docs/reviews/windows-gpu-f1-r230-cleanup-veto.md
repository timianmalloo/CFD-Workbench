---
id: review-windows-gpu-f1-r230-cleanup-veto
title: "Independent VETO of R230 activation candidate 4044b77a"
type: doc
status: blocked
owner: "@pc-f1-r230-cleanup-review"
phase: preflight
tags: [windows, cleanup, independent-review, veto]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: note-windows-gpu-f1-r230-cleanup-request, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  VETO exact source f75ae322 and candidate 4044b77a. A low-TTL second sample
  bypasses receipt binding; the leader validator admits NaN and infinity.
---

# Exact verdict

**VETO** candidate `4044b77a08fcbdc9b5b19151fb3e9b93b043d009`, source
`f75ae322574c5e3160012708b80fc610faa5a0e9`. The coordinator directed these exact
findings to be committed without repair. No source/test/candidate evidence was
edited. No WSL, registry observation, lifecycle action, GitHub message, mail or
live activation occurred during review. This verdict grants no authority.

Grounding: the committed owner decision numbered 229 requires authorization-driven
exact identities, no delete-debs, checkpoint d6edbc63 and its predecessors, and
the next hash-bound decision carrying R223(a)-(d). The latter require ruling and
authorization in one commit, live PC-local leadership, PC exclusivity, and one
attempt with a committed failure receipt and no second run without a new ruling.
The new request adds two samples with at least 200 seconds remaining.

# Measured hard vetoes

| Production boundary | Planted input | Observed result | Contract violation |
| --- | --- | --- | --- |
| `Cleanup.execute` → real `capture_pre_live_leader` and validator | Second sample: mac, epoch 8, live, 199 seconds | Refused; lifecycle calls 0; `bind` calls 0 | Expected lease refusal bypasses failure receipt |
| `validate_leader_sample` | Otherwise valid sample, TTL NaN | Accepted | Does not prove at least 200 seconds |
| `validate_leader_sample` | Otherwise valid sample, TTL positive infinity | Accepted | Non-finite value is not a valid measured remaining lease |

The executable independent probe is `r230-cleanup-review-probe.py`. It calls
the production `execute` method with process/lifecycle boundaries mocked and
the real second-sample parsing/validation intact. The 199-second sample exits
before `cleanup_sequence`, but also before the `try` responsible for calling
`bind`. It creates the run directory and leaves an unbound outcome. This is an
ordinary required refusal, not an external Git or exhausted-binding failure.

The validator uses a numeric type check and `expires_in < 200`; NaN makes that
comparison false and positive infinity also passes. Both accepted values were
measured directly. Python's JSON reader can admit these non-finite tokens; no
claim is made that the current coordinator producer emitted them. The validator
must reject an invalid measurement rather than promote it to sufficient TTL.

The probe exits 1 after printing both findings. The source's seventeen tests
still pass (0.021 seconds), demonstrating that their green result does not
cover these production boundaries. The independent probe tool wall time was
1.154 seconds. Ring: offline-fast, no live dependencies.

Bounded repair requirements: include the second-sample refusal in the classified
failure/receipt path without permitting any WSL call; reject non-finite TTL
before applying the threshold. Sweep sibling lease comparisons and pre-live
failure boundaries, preserving the one-attempt directory/receipt controls.
The reviewer performed no repair or retry.

# Verified positive evidence

| Artifact | Independently verified SHA-256 |
| --- | --- |
| Payload | `b5aecd640ce42c91760c7410a6e45c8a8ef867f0a45bbb7398b314bbfca82368` |
| Canonical cleanup authorization | `30f5a67158121ce9c1ba3fa622fd43db4ff692d439afce1fbb5ae8ca2d9f0822` |
| Request note | `c39fcc493212fef2448ee82bea1dcabdd9b53ab4ad4d867d9a6bd8645dc46e5d` |
| Executor source | `0094cde3c83795ee7064e41aff18e6d70882bccb952e8cf581178792ee8abfb1` |
| Author tests | `4a8fd09d9ad5a26d2a86b6260a0933e0c048a8b24c9ef5118507a5b3bf74c436` |

All ten payload entries match working bytes and source Git blobs. The three
top-level binding artifacts also match exact candidate blobs. Source ancestry,
checkpoint d6edbc63 and both 7c8a7984/8e10f78e prerequisites are verified. The
full failure manifest independently verifies **85 files** against both source
f75ae322 and checkpoint d6edbc63. Recursive Git comparisons leave attempt
checkpoint/final, run-1 and recovery-1 byte-identical to d6edbc63.

Actual authorization passes the exact-value schema; invalid values for every
field refuse. Authorization-derived identities match the committed failure
fixture: target `cfdw-petsc-cuda132-f1`, GUID
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`, version 2, path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`; protected
`cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
All three previous production alias probes still refuse.

The canonical authorization has accepted-shaped bytes but remains inert:
the required activation sample is absent and the exact `origin/main` text has
no requested owner section; production `ruling_section` refuses that text.
Source inspection shows the same-commit check requires both ruling/auth paths
in the defining commit, exact authorization/sample bytes, and source ancestry.
This is not an assertion that future activation has passed those checks.

Identity values propagate into observations, admission/absence, ownership
checks, lifecycle argv and receipt. Allowed operations remain precheck,
read-only owned-process refusal, exact target terminate/unregister, absence and
evidence binding. No delete-debs, preflight retry, download, install, source
fetch, build, case, benchmark or manual host-path deletion is reachable.
PC exclusivity is an external operational condition with an exact invocation
identifier check; the identifier alone does not establish machine-wide exclusion.
Existing-output refusal and atomic run-directory creation bound local reuse;
the reviewer did not measure live exclusivity or claim an activation sample.

The configured safe bundle passes **10/10**, with the three application/store
skips prescribed by `docs/coordination/join.json`. Its ruling-citation gate sees
229 cited and defined headings. These positive checks do not clear either veto.
