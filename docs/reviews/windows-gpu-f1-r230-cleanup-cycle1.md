---
id: review-windows-gpu-f1-r230-cleanup-cycle1
title: "Independent ACCEPT of R230 cleanup repair cycle 1"
type: doc
status: accepted
owner: "@pc-f1-r230-cleanup-review1"
phase: preflight
tags: [windows, cleanup, independent-review]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: note-windows-gpu-f1-r230-cleanup-request, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-20"
summary: >-
  Exact candidate e669ad1c/source c8df1291 clears both production vetoes.
  Failed second-sample refusal binds once and non-finite TTL refuses.
  Twenty synthetic production precheck cases and ten safe gates pass.
---

# Verdict and authority boundary

**ACCEPT** exact candidate `e669ad1c68698680f97ac30d6c6c1dc8a26d9550`, source
`c8df1291bb96f164cafb94b5f6eb106472bcfd7d`, as an offline R230 candidate.
Historical VETO `9354f9409163a98994653122ac6348ca8c826963` remains unchanged
for source f75ae322/candidate 4044b77a. No source repair, WSL, registry read,
lifecycle, GitHub message, mail or activation occurred in this review.

This is not live authority. The canonical accepted-shaped JSON is still inert:
the activation sample is absent and `origin/main` lacks the requested owner
section. The production section reader refuses that actual text. Mac/Fable
must commit the exact authority and qualifying sample with its owner decision;
the resulting activation must be independently rehashed before any execution.

# Independent production proof

The exact source diff adds `math.isfinite` to the TTL guard and moves the
second-sample capture inside the classified failure boundary. No identity,
operation, same-commit, checkpoint or deadline rule was relaxed.

The committed offline-fast probe `r230-cleanup-cycle1-probe.py` repeats the
original production calls. With a real parsed second sample at 199 seconds,
`execute` refuses, invokes no lifecycle actions, and calls `bind` exactly once
with `failed-stop-no-retry`. The actual `receipt` builder produces that outcome,
the classified ValueError and exact authorization digest. Binding is intercepted
in the test; no real Git receipt commit or live cleanup is claimed.

NaN, positive infinity and negative infinity all refuse in the real sample
validator. A finite 200-second sample passes. Both activation and immediate
samples share this validator. The earlier source's measured red results remain
in the historical VETO. All nineteen author tests independently pass (0.038
seconds). The complete independent probe passes in 2.316 seconds tool wall time.

Twenty synthetic activation/precheck cases exercise the real `precheck`,
replacing only its subprocess boundary and using temporary copies of exact
payload bytes. One valid case completes and propagates the authorized identity;
nineteen negatives refuse: session, epoch, exclusive id, used attempt, proposed
status, dirty tree, wrong branch, HEAD blob, source ancestry, source blob,
altered payload file, either manifest verification, missing ruling, missing
defining commit, split ruling/auth commit, ruling authorization mismatch,
ruling sample mismatch and low activation TTL. Zero WSL commands occur.
Synthetic activation proves the branches; it is not an actual owner decision.

All three prior path-alias controls refuse. Exact target/protected identities
match committed failure fixtures and payload. Invalid values for every
authorization field refuse. Source inspection and author controls preserve
exact-target lifecycle argv, first-failure stopping and all prohibited actions.

# Exact bindings and historical conservation

| Artifact | Independently verified SHA-256 |
| --- | --- |
| Payload | `4a8337ddd0548f7f7cbd8fa306d188db9f8a28ef0a41b4c77882b735f3564e55` |
| Canonical cleanup authorization | `b3143f40ffa541accca45797eb6e620ebcc7272b505434a66c63582e1a960890` |
| Request note | `3ae84f590c980f8ef6ae8abf758e61db644ef8170bcf1fbbe3504bd7d34fcca6` |
| Executor | `69f1dc6028c0f573ef3b9b1158891168f7f08bad439ae9b4b989bb373117d219` |
| Author tests | `d864d9db781e9a998be5f66066aabe003bbc40f11c5781a38336322f59c9f903` |

All ten payload entries equal working bytes and source Git blobs. The three
top-level artifacts equal candidate blobs. Source is an ancestor of candidate;
checkpoint d6edbc63 and prerequisites 7c8a7984/8e10f78e are ancestors of source.
Both source and checkpoint independently return **GIT-BINDING-PASS, 85 files**
for the full failure manifest. Recursive historical comparisons from d6edbc63
through candidate are empty for attempt checkpoint/final and run-1/recovery-1.
The R228 failure remains failed.

Target: `cfdw-petsc-cuda132-f1`, `{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`,
version 2, `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`. Protected:
`cfdw-openfoam2512`, `{b7566a82-9cee-47ce-a246-f83632353cea}`, version 2,
`%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512`.
Run remains `r225-attempt1-epoch8-20261012`; cleanup session/exclusive id
`pc-f1-r230-cleanup1`; branch `win/gpu-f1-r228-cleanup`; Mac epoch 8.

# Contract, gates and residual limits

The committed owner decision numbered 229 and R223(a)-(d) remain the grounding:
authorization-driven exact identity; checkpoint preservation; no delete-debs;
source ancestry and same ruling/auth commit; live leadership; PC exclusivity;
one attempt and a failure receipt, then stop until a new ruling. The request's
two leader samples require finite TTL at least 200 seconds. The receipt includes
the activation sample; the immediate sample is a captured evidence file.

Allowed operations remain precheck, read-only owned-process refusal, exact
target terminate/unregister, stable absence, protected registration invariance,
and evidence freeze/binding. No preflight retry, download, install, source fetch,
build, case, benchmark or manual host-path deletion is reachable. PC exclusivity
remains an external operational condition; its exact invocation id is checked
but alone cannot prove machine-wide exclusion. Local one-attempt protection
uses existing-output refusal and atomic run-directory creation.

The safe bundle passes **10/10**, with only the three application/store skips
prescribed by `docs/coordination/join.json`. This includes ruling citations,
privacy and portability. The deadline remains 600 seconds including evidence
binding; an external Git/disk failure or exhausted deadline can still prevent
a receipt commit. Protected invariance here means registration identity and
path presence, not a new completed-L3 content snapshot. Live success is unmeasured.
