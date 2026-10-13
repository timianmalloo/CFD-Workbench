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
| Source commit | `6e788efbb7c2bc6f708927b183515c9ced60226d` |
| Executor source | `0241d5f5d4f7e6cfeda33581af51884dacfb54c2015f5a58484b7b2999d41e3a` |
| Payload | `3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89` |
| Canonical authorization | `1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d` |

The exact owner request is
`docs/notes/windows-gpu-f1-r230-cleanup-request.md`.
