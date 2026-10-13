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
| Source commit | `0f478493dccafb9965e3083fb68007f7b3740cc5` |
| Payload | `c801342babe37f96b4e02b233236cc554fef5adb0d7a99e385ccd9a7e990674f` |
| Proposed authorization | `2a436adc78850090c7b98eb24c1286eac8fa4a36a1e583470d43656daa15a6cf` |
| Accepted projection | `aa3f67827c95efd99b62d5ece018d1d62320267bbb5737b1d4bc3409c7c57898` |

The exact owner request is
`docs/notes/windows-gpu-f1-r229-cleanup-request.md`.
