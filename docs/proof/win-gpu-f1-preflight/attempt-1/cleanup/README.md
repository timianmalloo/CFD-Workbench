---
id: proof-win-gpu-f1-r229-cleanup-candidate
title: "R229 cleanup-only candidate for R228 attempt 1"
type: proof-pack
status: proposed
owner: "@pc-f1-r228-cleanup-author"
phase: preflight
tags: [windows, wsl, cleanup, authorization]
links:
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: depends-on }
  - { to: plan-windows-gpu-f1-r229-cleanup, rel: relates-to }
review-by: "2026-10-13"
summary: >-
  Offline-only source and controls for exact cleanup of the target left by R228.
  No live authority exists until Mac/Fable accepts the projected bytes in R229.
---

# R229 cleanup-only candidate

This directory isolates a lifecycle-only action from the historical R217
recovery and the failed R228 attempt. The source permits only authority and
identity prechecks, read-only owned-process checks, exact target terminate and
unregister, stable name/GUID/path absence, protected registration invariance,
and immutable evidence freeze/commit. It has no package, solver or case path.

`authorization.proposed.json` is review material and always refuses execution.
`authorization.accepted-projection.json` is the exact byte form requested from
Mac/Fable. It is still inert outside the canonical `authorization.json` path.
After the requested owner ruling is defined in `docs/notes/rulings.md` and
reaches `origin/main`, activation copies the projection byte-for-byte to that
canonical path and requires a separate independent rehash before any live command.

The runtime refuses before WSL unless the worktree is clean, the exact source is
an ancestor, every payload file matches its source blob, the live authorization
equals the committed projection, and that defined owner ruling includes every
required hash, identity and forbidden-action sentence. One failure stops the
action and commits a classified receipt. There is no automatic retry.

The expected invocation after activation and review is:

```powershell
$env:AGENT_SESSION='pc-f1-r229-cleanup1'
py -3 docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup.py --authorization docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/authorization.json --session pc-f1-r229-cleanup1 --epoch 8
```

Do not run that command from this proposed state.

## Exact offline candidate

| Artifact | SHA-256 |
| --- | --- |
| Source commit | `ad4cedbbd34f4d0178576330c98072c84f4dcc69` |
| Payload | `dd41827d0e26b9452bcbe4c9040d23f417449a43e55ae0863e9270a05e154370` |
| Proposed authorization | `e4806d0188185bc76a59c5ee06f39e639d597e4e09e5c1c486b00c417d735211` |
| Accepted projection | `82799f9938781b9f2aed94988dde9b5ab90cb61f9262d667eba6ec4796972e71` |

The exact owner request is
`docs/notes/windows-gpu-f1-r229-cleanup-request.md`.
