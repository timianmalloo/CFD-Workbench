---
id: proof-win-gpu-f1-attempt4-plan
title: "F1 attempt 4: the first attempt under Ruling 245, with the dry rehearsal in front of it"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, offline, f1, attempt-4, decision-request]
links:
  - { to: rulings, rel: depends-on }
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-attempt3-plan, rel: depends-on }
  - { to: proof-win-gpu-f1-dry-rehearsal, rel: depends-on }
  - { to: proof-win-gpu-f1-inputs, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Attempt 4 of the Ruling 217 dependency preflight, the first under Ruling 245's cap of three (4, 5, 6), prepared on
  the Windows ARM64 laptop: the selections, the inputs (unchanged from attempts 2 and 3), the pair produced by
  contract.py prepare --attempt 4 (which also writes this run root's capture-attribute rules), the dry rehearsal green
  for this run root, and the live authorization request.
---

# F1 attempt 4: the first attempt under Ruling 245, with the dry rehearsal in front of it

**Where this stands.** Attempts 1 to 3 of Ruling 225 are consumed; 2 and 3 refused before any WSL effect on launcher
paths that had never run live (`../attempt-2/README.md` section 7, `../attempt-3/README.md` section 7). Ruling 245
gave a new cap of three and put a dry rehearsal in front of every attempt (`../rehearsal/README.md`): the own-child
identity now compares at microsecond precision, the run root's capture folders are proven byte-exact before the first
WSL effect, and `contract.py prepare` writes those rules. This attempt is the first with the rehearsal green for its
own run root before launch.

## 1. Selections

| Field | Value | Note |
|---|---|---|
| `attempt` | `4` | the first under Ruling 245's cap (4, 5, 6) |
| `execution_worktree` | `D:\Projects\CFD-Workbench-f1-attempt4` | absolute, uncreated at prepare time, created from the accepted commit after the ruling (`git worktree add`) |
| `execution_branch` / `session` / `run_id` | `win/gpu-f1-attempt4` / `pc-f1-attempt4` / `f1-attempt4-<date>` | derived from the attempt number (Rulings 243 (2), 245 (4)) |
| `run_root` | `docs/proof/win-gpu-f1-preflight/attempt-4` | this folder; its `checkpoint/**`, `final/**` and `live/**` rules in `.gitattributes` are written by `prepare` |
| `architecture` / `protected` | `arm64` / `none` | as attempts 2 and 3 |
| `leader` / `leader_epoch` | `mac` / the epoch a fresh `coord leader pin mac --reclaim` returns | epoch 4 lapsed after attempt 3; the TTL is at most 300 s, so a renewer keeps the pin live until the launcher's own supervisor takes over |
| host floors | `host_start_floor_bytes 19669226335`, `host_reserve_bytes 10737418240` | the attempt-2 worksheet (`../attempt-2/inputs-receipt.json`) |

## 2. Inputs

Unchanged from attempts 2 and 3: the five external inputs in `D:\reviewed` at the pins recorded in
`../attempt-2/inputs-receipt.json`, the cloud-image keyring under the execution worktree (`../inputs/README.md`), the
B2 licence map under the execution worktree. The launcher hashes every one before the first WSL effect.

## 3. Sequence

1. This document joins `main` with the pair (step 2) in one docs-only join.
2. `AGENT_SESSION=mac coord leader pin mac --ttl 300 --reclaim`; `coord leader who` gives the epoch; a renewer keeps it
   live. In a fresh worktree at `main`: `py -3 contract.py prepare --source-commit <HEAD> --attempt 4
   --execution-worktree D:\Projects\CFD-Workbench-f1-attempt4 --leader-epoch <epoch> --requested-ruling <next>
   --architecture arm64 --protected none --host-start-floor-bytes 19669226335 --host-reserve-bytes 10737418240
   --branch win/gpu-f1-attempt4 --session pc-f1-attempt4 --run-id f1-attempt4-<date>` plus the seven input paths. It
   writes the pair here, prints both hashes and `capture_attributes_added=3` (the `.gitattributes` lines for this run
   root, committed with the pair).
3. **The rehearsal, green for this run root:** `pwsh -NoProfile -File tools/run-f1-dry-rehearsal.ps1 -RunRoot
   docs/proof/win-gpu-f1-preflight/attempt-4 -Receipt docs/proof/win-gpu-f1-preflight/attempt-4/rehearsal-prepare.json`
   must print `F1-DRY-REHEARSAL-GREEN` at the source commit; the receipt is committed here.
4. A decision request to the operator carrying every `attempt.check_ruling` token; the ruling takes the next free
   number and is pushed to `origin/main`; the accepted authorization replaces the proposal and is pushed.
5. The execution worktree is created from the accepted commit on `win/gpu-f1-attempt4`; the rehearsal runs green
   again there; the renewer stops; the launcher runs from native `pwsh` as `pc-f1-attempt4`. No Bash-tool call and no
   bash-based watcher while it runs. The evidence commits land on the branch and join `main`.

## 4. Residual risks

Those of `../attempt-3/README.md` section 6. The rehearsal covers the own-child registration, the capture attributes
and the bind; the launcher paths after target creation (the WSL stages, the owned stop, the final freeze) have still
run live only on the old PC at attempt 1, and the first arm64 run of them is this attempt.
