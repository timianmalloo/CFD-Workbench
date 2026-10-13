---
id: note-windows-gpu-f1-r229-cleanup-request
title: "Request for R229 cleanup-only authority"
type: decision-note
status: proposed
owner: "@pc-f1-r228-cleanup-author"
phase: preflight
tags: [windows, wsl, cleanup, ruling-request]
links:
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: depends-on }
  - { to: proof-win-gpu-f1-r228-attempt1-failure, rel: relates-to }
review-by: "2026-10-13"
summary: >-
  Exact R229 request for one cleanup-only action against the target left by the
  failed R228 attempt. This request and its projection grant no live authority.
---

# R229 request — one cleanup-only action

The PC requests that Mac/Fable accept or refuse the exact candidate below after
independent review. The requested ruling text must include the uppercase grant
`ACCEPT ONE CLEANUP-ONLY ACTION` and every binding and prohibition in this note.
This proposed note and the accepted projection do not grant authority.

| Binding | Exact value |
| --- | --- |
| Source commit | `ad4cedbbd34f4d0178576330c98072c84f4dcc69` |
| Payload SHA-256 | `dd41827d0e26b9452bcbe4c9040d23f417449a43e55ae0863e9270a05e154370` |
| Proposed authorization SHA-256 | `e4806d0188185bc76a59c5ee06f39e639d597e4e09e5c1c486b00c417d735211` |
| Accepted projection SHA-256 | `82799f9938781b9f2aed94988dde9b5ab90cb61f9262d667eba6ec4796972e71` |
| Session / branch / leader epoch | `pc-f1-r229-cleanup1` / `win/gpu-f1-r228-cleanup` / `8` |
| Original run | `r225-attempt1-epoch8-20261012` |
| Target | `cfdw-petsc-cuda132-f1` / `{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}` / `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` |
| Protected | `cfdw-openfoam2512` / `{b7566a82-9cee-47ce-a246-f83632353cea}` / `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512` / version `2` |
| Deadline | `600` seconds including evidence binding |

Allowed actions are exactly: precheck; read-only owned-process refusal; exact
identity binding; terminate the exact target; unregister the exact target;
stable name/GUID/path absence; protected identity invariance; evidence
freeze/commit. A failure stops and returns a committed classified receipt. It
does not authorize a retry.

The ruling must say: **never resume preflight; never download; never install;
never fetch source; never build; never run a case; never benchmark; never delete
host paths manually.** It must bind the accepted projection SHA-256, payload
SHA-256, source commit, target and protected identities above. The canonical
live authorization remains absent until a byte-identical activation after the
ruling reaches `origin/main`; an independent reviewer must rehash that activation
before execution.
