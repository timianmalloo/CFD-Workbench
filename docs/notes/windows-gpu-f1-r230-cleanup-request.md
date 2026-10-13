---
id: note-windows-gpu-f1-r230-cleanup-request
title: "R230 request for one activation-adapted cleanup action"
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
  Exact R230 request for one cleanup-only action against the residual R228
  target. This request and authorization candidate grant no live authority.
---

# R230 request — one cleanup-only action

The PC asks Mac/Fable to accept or refuse this exact offline candidate. Mac must
independently review it. Acceptance uses owner heading number `230`, includes
the uppercase grant `ACCEPT ONE CLEANUP-ONLY ACTION`, and binds every value and
condition below. This request grants no authority.

| Binding | Exact value |
| --- | --- |
| Source commit | `c8df1291bb96f164cafb94b5f6eb106472bcfd7d` |
| Executor SHA-256 | `69f1dc6028c0f573ef3b9b1158891168f7f08bad439ae9b4b989bb373117d219` |
| Payload SHA-256 | `4a8337ddd0548f7f7cbd8fa306d188db9f8a28ef0a41b4c77882b735f3564e55` |
| Canonical authorization path | `docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/cleanup-authorization.json` |
| Canonical authorization SHA-256 | `b3143f40ffa541accca45797eb6e620ebcc7272b505434a66c63582e1a960890` |
| Session / branch / leader epoch | `pc-f1-r230-cleanup1` / `win/gpu-f1-r228-cleanup` / `8` |
| Exclusive run id | `pc-f1-r230-cleanup1` |
| Checkpoint | `d6edbc63a39c7f59d9e4ad853c92e7821406e371` |
| Checkpoint prerequisites | `7c8a7984fcb101a4f29c212f221488aa1a44e637`, `8e10f78ea4c99fc60184c8b7372e751a149ff3b4` |
| Original run | `r225-attempt1-epoch8-20261012` |
| Target | `cfdw-petsc-cuda132-f1` / `{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}` / `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` |
| Protected | `cfdw-openfoam2512` / `{b7566a82-9cee-47ce-a246-f83632353cea}` / `%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512` / version `2` |
| Deadline | `600` seconds including evidence binding |

Allowed actions are exactly: `precheck`, `owned-process-refusal`,
`terminate-target`, `unregister-target`, `prove-absence`, and
`freeze-and-commit-evidence`. The source reads both identities from the
authorization and then enforces the exact reviewed values above. There is no
`delete-debs` action because no `.debs` exist. There is no preflight action.

The accepted owner section must state: **never resume preflight; never download;
never install; never fetch source; never build; never run a case; never
benchmark; never delete host paths manually.**

It must also encode the R223 conditions in these exact terms so the executor can
fail closed:

- `R223(a)`: the owner section and byte-exact canonical authorization are in the
  same commit, and the source commit is an ancestor of the executed HEAD.
- `R223(b)`: two leader samples show Mac leader epoch 8 live with at least 200
  seconds remaining. Mac captures `leader-activation.json` at activation and
  commits it with the owner section. The executor captures
  `leader-immediately-pre-live.json` immediately before its first WSL command.
- `R223(c)`: the PC is exclusive for this action, with no other WSL-touching
  session or track. Invocation binds `--exclusive-run-id pc-f1-r230-cleanup1`.
- `R223(d)`: one attempt only. Any failure commits a receipt and stops. There is
  no second run without a new ruling.

Mac activation procedure:

1. Independently verify source, payload and authorization hashes.
2. Renew the PC-local Mac leader designation at epoch 8.
3. On the PC, run `py -3 docs/ai-forward-pack/scripts/coord-core.py leader who
   --json`; refuse if the result is not live or has less than 200 seconds.
4. Save that exact public JSON as
   `docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/leader-activation.json`.
5. Commit the byte-exact `cleanup-authorization.json`, leader sample and owner
   section together. Do not rewrite the authorization bytes.
6. Push `main`. The PC fetches and merges `origin/main`, rehashes all bindings,
   and only then may invoke the executor under PC exclusivity.

No WSL, registry, lifecycle or cleanup action is authorized before all six
steps are complete.
