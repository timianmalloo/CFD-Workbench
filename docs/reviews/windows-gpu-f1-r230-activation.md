---
id: review-windows-gpu-f1-r230-activation
title: "Independent ACCEPT of exact R230 activation fd4f738a"
type: doc
status: accepted
owner: "@pc-f1-r230-activation-review"
phase: preflight
tags: [windows, cleanup, activation, independent-review]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: note-windows-gpu-f1-r230-cleanup-request, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Exact activation fd4f738a accepted under owner R230. Four hashes, same-commit
  authority/sample, ancestry and the real read-only production precheck pass.
---

# Verdict and exact commits

**ACCEPT activation** at executed-head candidate
`fd4f738ad01f6a4be89c92d844c5ef372c47ea07`, under Ruling 230 at
`2f22eea93b2f06388e4931b60aa6129a0f53fd4d`, contained in `origin/main`
`04354ecf0b1cc6ac6fffcab16dba4e5b34b34014`. Source
`6e788efbb7c2bc6f708927b183515c9ced60226d` and hashed-byte candidate
`2a7c269f08149af2bb6165e94249c7d25dcb46c2` are ancestors of that executed head.
The prior conformance review remains `983bc70358d621e4b012b39eb0b88d2c8a5c5d90`.

This review checks the owner-authorized activation; it does not expand the
single cleanup action or claim cleanup success. No WSL, registry read,
lifecycle, cleanup, GitHub message, mail, source repair or activation mutation
occurred during review. The fresh second leader sample remains a runtime gate.

# Exact byte and same-commit verification

| Artifact | Independently verified SHA-256 |
| --- | --- |
| Canonical cleanup authorization | `1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d` |
| Payload | `3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89` |
| Executor | `0241d5f5d4f7e6cfeda33581af51884dacfb54c2015f5a58484b7b2999d41e3a` |
| Activation leader sample | `cd1dca8e45cdef55cec87a1a91c333415ceceb481f36385342769dbfc6788270` |

Each artifact equals its executed-head blob and the execution checkout bytes.
Payload/auth also equal the reviewed byte-candidate blobs. The eleven payload
entries match working bytes and source blobs. The defining owner commit changes
`docs/notes/rulings.md`, canonical `cleanup-authorization.json` and
`leader-activation.json` together; both JSON blobs equal the execution bytes.
The exact owner section passes production `ruling_section` and `verify_ruling`.

The raw committed activation sample records leader `mac`, epoch `8`, state
`live`, finite `expires_in=836.3857350349426`, above the required 200 seconds.
It names ref object `f19989ba852286a963aedcee2a019b3013876998`. This is the
activation-time observation, not a claim about remaining TTL at future execution.

# Real offline precheck

`r230-activation-probe.py` invokes only production `Cleanup.precheck` against
the actual execution checkout. Every subprocess is gated to read-only Git
subcommands or the manifest verifier. It never calls `execute`, leader capture,
registry or lifecycle methods. Result: **ACTIVATION-PRECHECK-PASS**, 28 allowed
commands, WSL=0, registry=0, writes=0; measured tool wall time 8.295 seconds.

The execution checkout is on `win/gpu-f1-r228-cleanup`, clean before and after,
at exact fd4f738a, equal to its pushed origin branch. Both source and checkpoint
failure-manifest verifications ran through production precheck. Checkpoint
`d6edbc63a39c7f59d9e4ad853c92e7821406e371` and prerequisites 7c8a7984/8e10f78e
remain source ancestors. Historical attempt checkpoint/final and run-1/recovery-1
have an empty recursive diff against d6edbc63. No cleanup run output exists.

# Authorized scope and remaining runtime conditions

The authorization and owner section agree on session/exclusive id
`pc-f1-r230-cleanup1`, original run `r225-attempt1-epoch8-20261012`, branch
`win/gpu-f1-r228-cleanup`, Mac epoch 8 and deadline 600 seconds including binding.
Target is `cfdw-petsc-cuda132-f1`, GUID
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`, path
`C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`, version 2. Protected is
`cfdw-openfoam2512`, GUID `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
path `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.

Only precheck, read-only owned-process refusal, exact target terminate/unregister,
stable absence, protected registration invariance and evidence freeze/binding
are allowed. No delete-debs, preflight retry, download, install, source fetch,
build, case, benchmark or manual host-path deletion is admitted.

R223(a)-(d) remain binding: same-commit authority/source ancestry; two live Mac
epoch-8 samples with finite TTL at least 200 seconds; PC exclusivity with no
other WSL-touching track; one attempt, failure receipt and stop, no second run
without a new ruling. The reviewed `execute` captures and records the fresh
second sample before `cleanup_sequence`, inside the classified failure path.
PC exclusivity must be maintained operationally. No current registry identity
or future lease sufficiency is inferred from this offline acceptance.
