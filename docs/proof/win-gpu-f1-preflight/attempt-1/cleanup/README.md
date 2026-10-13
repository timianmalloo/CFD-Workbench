---
id: proof-win-gpu-f1-r229-cleanup-candidate
title: "R230 activation-adapted cleanup candidate for R228 attempt 1"
type: proof-pack
status: proposed
owner: "@pc-f1-r228-cleanup-author"
phase: preflight
tags: [windows, wsl, cleanup, authorization]
links:
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: plan-windows-gpu-f1-r230-cleanup, rel: relates-to }
  - { to: note-windows-gpu-f1-r230-cleanup-request, rel: relates-to }
review-by: "2026-10-13"
summary: >-
  Offline-only R230 candidate for exact cleanup of the target left by R228.
  It cannot run until Mac/Fable commits the exact authorization with the next
  owner ruling and a qualifying activation leader sample.
---

# R230 activation-adapted cleanup candidate

This directory isolates one lifecycle-only cleanup from the historical R217
recovery and the failed R228 attempt. `cleanup.py` reads target, protected and
original-run identities from `cleanup-authorization.json`, then refuses unless
they equal the exact values reviewed in R229. It permits only authority and
identity prechecks, read-only owned-process checks, exact-target termination
and unregistration, stable name/GUID/path absence, protected invariance, and
evidence freeze/commit. It has no `.deb`, preflight, solver, case or benchmark
path.

`cleanup-authorization.json` is the exact byte candidate Mac/Fable is asked to
commit with the requested owner heading. Its accepted status is inert until
that same commit also defines the heading on `origin/main` and adds
`leader-activation.json`. The executor proves the same-commit relationship and
requires the committed activation sample to show leader `mac`, epoch `8`, state
`live`, and at least 200 seconds remaining.

At execution, the PC must be exclusive under
`--exclusive-run-id pc-f1-r230-cleanup1`. Immediately before the first WSL
observation, the executor captures a second JSON leader sample and applies the
same 200-second floor. One failure freezes and commits a classified receipt,
then stops. A second attempt requires a new owner ruling.

The eventual invocation, only after Mac activation is merged locally, is:

```powershell
$env:AGENT_SESSION='pc-f1-r230-cleanup1'
py -3 docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py --authorization docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup-authorization.json --session pc-f1-r230-cleanup1 --epoch 8 --exclusive-run-id pc-f1-r230-cleanup1
```

Do not run that command from this proposed state.

## Exact offline candidate

| Artifact | SHA-256 |
| --- | --- |
| Source commit | `f75ae322574c5e3160012708b80fc610faa5a0e9` |
| Executor source | `0094cde3c83795ee7064e41aff18e6d70882bccb952e8cf581178792ee8abfb1` |
| Payload | `b5aecd640ce42c91760c7410a6e45c8a8ef867f0a45bbb7398b314bbfca82368` |
| Canonical authorization | `30f5a67158121ce9c1ba3fa622fd43db4ff692d439afce1fbb5ae8ca2d9f0822` |

The exact owner request is
`docs/notes/windows-gpu-f1-r230-cleanup-request.md`.
