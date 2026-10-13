---
id: review-windows-gpu-f1-r229-cleanup-cycle2
title: "Independent ACCEPT of final R229 cleanup candidate"
type: doc
status: accepted
owner: "@pc-f1-r229-cleanup-review2"
phase: preflight
tags: [windows, cleanup, independent-review]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: plan-windows-gpu-f1-r229-cleanup-review2, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Independent offline acceptance of exact candidate e624ebce and source
  0f478493 after both prior vetoes are cleared. No live authority follows.
---

# Exact verdict and limits

**ACCEPT** candidate `e624ebce9fa01dd30e4cf3fa9395438f53c81427`, source anchor
`0f478493dccafb9965e3083fb68007f7b3740cc5`, as an offline cleanup-only candidate.
This does not authorize execution. An accepted owner ruling on main must bind
the exact source, payload, accepted projection, identities and prohibitions;
canonical activation then requires a separate independent rehash. Canonical
`cleanup/authorization.json` is absent. No WSL, registry read, lifecycle action
or GitHub message was performed during this review. No candidate source, tests
or historical evidence were changed by the reviewer.

The first VETO remains preserved at `e3929771d53d5b4ba83bd8c9c7602ffb87faecf5`;
the cycle-1 required-gate VETO remains preserved at
`4d6e200b90465f8871815c667c7acba56ce75783`. Neither historical verdict is rewritten.
The final repair changes proposal wording and regenerated bindings only.
Production source and tests are byte-identical to the repaired cycle-1 source.

# Measured controls

`r229-cleanup-cycle2-probe.py` repeats the cycle-1 independent controls with only
the candidate/source/hash constants changed. It loads real production functions
and committed failure fixtures. Old source eb1e admits all three planted aliases;
the final source refuses target-path and protected-path aliases at admission
and protected-path aliases at absence. Expanded-variable, uppercase, extended
prefix and trailing-separator variants also refuse. Exact identities pass.
Stable identical absence passes; target reappearance refuses without publishing
absence proof. Expired deadline refuses. These are offline-fast controls; the
probe completed in 1.021 seconds of measured tool wall time.

All fourteen author tests independently pass (0.027 seconds), including exact
lifecycle argv, first-failure stopping, identity mutations, forbidden operations,
authorization/ruling checks and read-only ownership helper scope. Independent
invalid-field controls cover every authorization field. Missing canonical
authorization, proposed authorization and noncanonical accepted projection all
refuse in real `precheck` with **zero subprocess launches**. A valid source SHA
is additionally checked through payload equality and source Git blobs.

The direct ruling-citation gate now passes: 228 cited rulings, 228 defined
headings. The candidate README states that the requested owner ruling must be
defined and reach main before activation; it no longer presents a missing
heading as established authority. The gate itself was not edited or exempted.
The configured safe bundle passes **10 of 10 executed gates**, with only the
three application/store skips prescribed by `docs/coordination/join.json`.
This includes machine-path privacy, ruling citations, conflict markers and
subprocess/text portability. The prior gate VETO is cleared for this exact tip.

# Exact hashes and historical conservation

| Artifact | Independently verified SHA-256 |
| --- | --- |
| Payload | `c801342babe37f96b4e02b233236cc554fef5adb0d7a99e385ccd9a7e990674f` |
| Proposed authorization | `2a436adc78850090c7b98eb24c1286eac8fa4a36a1e583470d43656daa15a6cf` |
| Accepted projection | `aa3f67827c95efd99b62d5ece018d1d62320267bbb5737b1d4bc3409c7c57898` |
| Cleanup source | `148e996cf93b23b9d4f12dd24880b6838550d4d2d734ca23f2fdd11faf08a904` |
| Cleanup tests | `ff4f00d81b2db638ddcb256a716f71c849335dafc5a593275a2679e1e0768941` |
| Owned-process helper | `6b1a9757dadd4fffb8525e8acad85cb10322cc801758840830eee50fe36fde6b` |
| Full failure manifest | `7d89c6d6a4577deac44d507390a680b38f687f6eed6e784f7a577210086e72c6` |

All ten payload entries match working bytes and source blobs. Source is an
ancestor of the candidate; payload and authorization bytes equal candidate
blobs. Proposed and projection objects differ only in status/ruling. The entire
failure manifest verifies against source 0f478493: **GIT-BINDING-PASS, 85 files**.
Recursive history comparisons from d6edbc63 and eb1e to this candidate are empty
for attempt checkpoint/final and run-1/recovery-1. The R228 failure stays failed.

Target: `cfdw-petsc-cuda132-f1`, GUID `{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`,
version 2, path `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`.
Protected: `cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`,
version 2, path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
The payload matches the committed failure identities. Run remains
`r225-attempt1-epoch8-20261012`, cleanup session `pc-f1-r229-cleanup1`, branch
`win/gpu-f1-r228-cleanup`, Mac epoch 8.

# Reachable behavior and residual limits

The unchanged production sequence performs authority/stable identity prechecks,
read-only owned-process refusal, stable identity before exact-target terminate
and unregister, stable absence, protected registration invariance, and separate
evidence/binding commits. No preflight retry, download, install, source fetch,
build, case, benchmark or manual host-path deletion is reachable. The ownership
helper sends no process signals. One failure stops the sequence.

The shared deadline remains 600 seconds, including evidence binding. The executor
attempts a classified receipt; deadline exhaustion or external Git failure can
prevent its commit. Protected invariance means registration identity and path
presence, not a new completed-L3 content snapshot. This review makes no claim of
live success. The two-cycle repair cap is consumed; another required failure
would stop the track rather than authorize a third repair.
