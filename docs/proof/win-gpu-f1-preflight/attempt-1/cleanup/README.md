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
After R229 reaches `origin/main`, activation copies the projection byte-for-byte
to that canonical path and requires a separate independent rehash before any
live command.

The runtime refuses before WSL unless the worktree is clean, the exact source is
an ancestor, every payload file matches its source blob, the live authorization
equals the committed projection, and `origin/main` Ruling 229 includes every
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
| Source commit | `eb1e0e64c5fcbc9a77ea10d8593e99e2912a0c1f` |
| Payload | `05779548b652f380222357df515766629d3bd096d7526be95af4a8c2c259a5fa` |
| Proposed authorization | `8cf8d84ab7049e681ae45670db78cd3662ffcb632a00a23a8c24387eff401de4` |
| Accepted projection | `fd0b7f668844a730a2623eff7a798d8b7a5c217533a48fd3039fd9e1859193cd` |

The exact owner request is
`docs/notes/windows-gpu-f1-r229-cleanup-request.md`.
