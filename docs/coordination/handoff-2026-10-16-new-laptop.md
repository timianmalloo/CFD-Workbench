---
id: coordination-handoff-2026-10-16-new-laptop
title: "Handoff 2026-10-16: moving the PC work to a new laptop"
type: proof-pack
status: draft
owner: "@operator"
tags: [coordination, handoff, windows, laptop, f1]
links:
  - {to: coordination-handoff-2026-10-13-pc-sole-leader, rel: relates-to}
review-by: 2026-10-30
summary: >-
  What the old PC pushed before the move, what it could not push, and the steps to resume the PC sole-leader work on a new
  Windows laptop. Read it after the 2026-10-13 PC sole-leader handoff.
---

# Handoff 2026-10-16: moving the PC work to a new laptop

The old PC is retired. This file lists where its work is on origin and the steps to continue on the new laptop. The
Ruling 236 handoff (`docs/coordination/handoff-2026-10-13-pc-sole-leader.md`) still holds: the laptop is now the PC sole
leader.

## 1. Where the old PC's work is on origin

| Item | Ref on origin | State |
|---|---|---|
| F1 attempt-2 contract candidate (Ruling 235, cycle 3) | branch `win/gpu-f1-attempt2-contract` | One WIP commit on top of `main` at `80935463`. **Unreviewed.** It is the old PC's uncommitted working tree, committed only to move it. |
| 58 PC session logs (`.agents/log/*.jsonl`) and this file | branch `win/old-pc-session-logs` | Not on `main`. Readiness was RED on the old PC (see section 3). |
| Commits that existed only on the old PC | tags `archive/docs/windows-grounding-summary`, `archive/win/gpu-g2-b1-r199`, `archive/win/pr19-followups` | Archived, same scheme as the 2026-10-14 clean-up. |
| All other old local branches | `main` or the existing `archive/*` tags | Nothing left to move. |

**Not on origin yet (see section 4):** the leftover evidence files in four old worktrees. If the old PC is already gone,
they are lost. Their loss is small: the old PC compared every file with origin, and apart from the files below all the
bytes were already on origin (for example as `run-1/failed-live`).

| Old worktree | Files not on origin |
|---|---|
| `CFD-Workbench-win-gpu-f1-preflight-r217` | One uncommitted `docs/audit/audit-log.jsonl` line (prompt "the mac has unblocked you hours ago", 2026-10-12) and its `audit-data.js` view. |
| `CFD-Workbench-win-gpu-f1-r217-execute-r220` | `run-1/live/checkpoint-freeze.stdout.txt`, `run-1/live/invocation-contract.stdout.txt`, `Save-Checkpoint/invocation-contract.stdout.txt` (the stray `Save-Checkpoint/` folder at the tree root is otherwise a copy of `run-1/live`). |
| `CFD-Workbench-win-gpu-f1-r228-cleanup` | Eight post-commit outputs in `attempt-1/cleanup/run-1/live/` (`033`, `035`, `036`, `037`, `039`, `040`). |
| `CFD-Workbench-dtj-pc-calibration` | `docs/proof/ring-pc-win/desktop-slots-high.csv`: the raw N=10 and N=12 rows behind the PR #40 join note. |

## 2. Set up the laptop

1. Install Git, Python 3 (use `py -3` on Windows), PowerShell 7 and WSL as in `.claude/knowledge/windows-proof-runbook.md`.
2. Install the **.NET SDK 10.0.203** exactly. `global.json` pins it. The old PC had only 10.0.301 and 9.0.315, so
   readiness failed with "A compatible .NET SDK was not found". Check with `dotnet --list-sdks`.
3. Clone and fetch the tags:

   ```powershell
   git clone https://github.com/timianmalloo/CFD-Workbench.git C:\Projects\CFD-Workbench
   cd C:\Projects\CFD-Workbench
   git fetch --tags origin
   ```

4. Check the machine with `py -3 docs/ai-forward-pack/scripts/pack-doctor.py`.
5. Run `py -3 tools/check-proof-pii.py` once. The laptop has a new hostname, and the check always scans for it.

## 3. Bring the session logs onto main

1. `git fetch origin`, then on `main`: `git merge --ff-only origin/win/old-pc-session-logs`. The branch is one or two
   commits on top of `main` at `80935463`. If `main` has moved, use a normal merge.
2. `py -3 tools/check-docs.py` must exit 0.
3. `py -3 tools/run-readiness.py` must print GREEN for HEAD. If it is RED, read the receipt
   (`.tmp-tests/readiness.json`). Do not push a RED HEAD.
4. `git push origin main`, then `bash tools/verify-push.sh`.
5. Delete the move branch: `git push origin --delete win/old-pc-session-logs`.

## 4. Push the leftover evidence (only while the old PC still exists)

The old PC's Claude Code session could not push these: the auto-mode classifier blocked it twice. On the old PC, the
operator runs the script below from a terminal. It commits the files on each worktree's own branch, tags each commit
`archive/<branch>-pc-leftovers` and pushes the four tags. It does not touch `main`.

Save it as `leftovers.sh` outside the repo and run `bash leftovers.sh` in Git Bash.

```bash
#!/usr/bin/env bash
set -euo pipefail
R=/c/Projects
TR="Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
leftover() { # worktree tag-name paths...
  local wt=$1 tag=$2; shift 2
  git -C "$R/$wt" add -- "$@"
  git -C "$R/$wt" commit -q -m "chore: archive uncommitted files left on the old PC" -m "Preserved before the move to a new laptop; not reviewed." -m "$TR"
  git -C "$R/$wt" tag "$tag"
}
cd /c/Projects/CFD-Workbench
leftover CFD-Workbench-win-gpu-f1-preflight-r217 archive/win/gpu-f1-preflight-r217-pc-leftovers docs/audit/audit-data.js docs/audit/audit-log.jsonl
leftover CFD-Workbench-win-gpu-f1-r217-execute-r220 archive/win/gpu-f1-r217-execute-r220-pc-leftovers Save-Checkpoint docs/proof/win-gpu-f1-preflight/run-1/live
leftover CFD-Workbench-win-gpu-f1-r228-cleanup archive/win/gpu-f1-r228-cleanup-pc-leftovers docs/proof/win-gpu-f1-preflight/attempt-1/cleanup/run-1/live
leftover CFD-Workbench-dtj-pc-calibration archive/dtj/pc-calibration-pc-leftovers docs/proof/ring-pc-win/desktop-slots-high.csv
git push origin \
  archive/win/gpu-f1-preflight-r217-pc-leftovers archive/win/gpu-f1-r217-execute-r220-pc-leftovers \
  archive/win/gpu-f1-r228-cleanup-pc-leftovers archive/dtj/pc-calibration-pc-leftovers
```

To read one on the laptop: `git fetch origin tag archive/<name>`, then `git show archive/<name> --stat`.

## 5. Resume the F1 attempt-2 track

1. Create the worktree for the candidate (WT1):

   ```powershell
   git worktree add C:\Projects\CFD-Workbench-win-gpu-f1-attempt2-contract win/gpu-f1-attempt2-contract
   ```

2. Treat the WIP commit as **unreviewed**. Its diff could not be matched to cycle 2's recorded SHA-256 (`73f84ab7…`),
   because the handoff does not say how that hash was computed. Read the diff (`git show HEAD`) before building on it.
3. Do cycle 3 exactly as section 3 of the 2026-10-13 handoff says (Ruling 235). The three items are: byte-exact empty
   stderr, the `expires_in_before >= 200` activation floor, and the committed contract pair. If cycle 3 fails review,
   the track stops and goes back to the operator.
4. The F1 packet's leader designation stays named `mac`. It is a PC-local designation (Ruling 236), so the laptop creates
   its own: `coord leader who --json`, then reclaim if it is not `live` with `expires_in >= 200` (Ruling 226).
5. **Hardware:** attempt 2 runs the GPU preflight in WSL. Check that the laptop has a GPU and a WSL setup able to run it,
   before you prepare the contract. The runbook lists what the preflight needs. If the laptop cannot run it, raise that
   with the operator before you take a ruling number.

## 6. Open items carried over

The open items in section 4 of the 2026-10-13 handoff are unchanged. Two of them depend on the machine, so measure them
again on the laptop and do not reuse the old PC's numbers:

- the Windows ring budget, and `limit.desktop_slots=8` for `ring-pc-win` (measured on the old PC's 20 logical processors);
- the stale Windows store evidence (`verify-windows-store`).
