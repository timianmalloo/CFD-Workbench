---
id: proof-win-gpu-f1-dry-rehearsal
title: "F1 dry rehearsal: the launcher's Windows-side paths with no WSL effect (Ruling 245)"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, f1, rehearsal, control]
links:
  - { to: rulings, rel: depends-on }
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-attempt3-plan, rel: depends-on }
  - { to: proof-win-gpu-f1-rescope-verification, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  The dry rehearsal Ruling 245 (3) names: tools/run-f1-dry-rehearsal.ps1 exercises the launcher's own-child
  registration against real processes, the run root's capture attributes and a freeze-commit-bind cycle in a scratch
  worktree, with no WSL effect. It ran red on the attempt-3 source against both classes that refused attempt 3, then
  green at the repaired source; it runs in the F1 verification runner and before every attempt.
---

# F1 dry rehearsal: the launcher's Windows-side paths with no WSL effect (Ruling 245)

**Why it exists.** Attempts 2 and 3 of Ruling 225 refused before any WSL effect on launcher paths that had never run
live: the supervisor's admission (a hand copy of the input list), the own-child identity (a timestamp equality across
two precisions) and the checkpoint bind (a run root with no `-text` rule). Each had green unit suites, because every
fixture set both sides of the equality to one literal or never reached the line. The rehearsal runs those paths
against real processes and the real repository, so a red appears before an attempt is spent.

## 1. What it runs

`pwsh -NoProfile -File tools/run-f1-dry-rehearsal.ps1 -RunRoot docs/proof/win-gpu-f1-preflight/attempt-<n> [-Receipt <path>] [-Repetitions 5]`

| Step | What | Red means |
|---|---|---|
| `own-child` | `Register-AttemptLauncher`, loaded from the launcher's AST, against `-Repetitions` real `cmd.exe` children; every registration must pass | the identity equality across the CIM (microsecond) and .NET (100 ns) timestamps fails on most real processes (TIMESTAMP-IDENTITY-ACROSS-PRECISIONS) |
| `attributes` | `git check-attr text` on `<run root>/{checkpoint,final,live}/probe.txt`, each `unset` | Git would normalize a CRLF capture; the launcher's bind refuses after the commit (RAW-CAPTURE-BLOB-MISMATCH recurrence 4) |
| `bind` | `manifest.py freeze` of a CRLF capture under `<run root>/checkpoint` in a detached, no-checkout scratch worktree of this repository, `git add -f`, `git commit`, `manifest.py verify`; the worktree is removed | the exact refusal attempt 3 recorded (`commit blob differs from evidence`) |

Exit 0 green, 1 red, 2 usage. The receipt (schema `cfdw-f1-dry-rehearsal/1`) carries the head, the run root, each
step's result and summary, scrubbed of the repository and user paths. Cost: about 1.5 s plus `Repetitions` × 2 s of
child processes. Ring: `tools/run-f1-verification.py` (the `dry-rehearsal` step, against the highest `attempt-<n>`
run root present) and the attempt README's pre-attempt list; not the fast ring.

## 2. Red first, then green (2026-10-20)

| Run root | Source | Result | Receipt |
|---|---|---|---|
| attempt-3 | `554885da` (the attempt-3 source, before the controls) | RED: `own-child` 0/5 (`F1-OWN-LAUNCHER-IDENTITY` five times); `attributes` ok (the rules landed with the attempt-3 evidence); `bind` red on a harness defect (a scratch path too long for a full checkout, fixed: no checkout) | `rehearsal-red-attempt-3.log` (the console lines; the receipt of these two runs was lost to the harness's own `$receipt` defect below) |
| attempt-4 | `554885da` | RED: `own-child` 1/5 (one child's ticks happened to end in zero, the same luck as attempt 3's first child); `attributes` 0/3 `text: auto`; `bind` red | `rehearsal-red-attempt-4.log` |
| attempt-3 | the repaired source (this node) | GREEN: 5/5, 3/3, `GIT-BINDING-PASS files=4` | `rehearsal-attempt-3.json` |
| attempt-4 | the repaired source, before `contract.py prepare --attempt 4` writes its rules | RED on `attributes` and `bind` only: the exact refusal attempt 3 recorded, now visible before an attempt; `own-child` 5/5 | `rehearsal-attempt-4.json` |

The harness itself had two defects on first contact, both the register's classes: a `$receipt` variable replaced the
`-Receipt` parameter (PowerShell variable names are case-insensitive; the same shape as `$Protected` at node C4) and
wrote the receipt to a file named after the dictionary's type; and a full scratch checkout failed on this repository's
long paths. Both are fixed in the script with a comment at the line.

## 3. The controls it proved (Ruling 245 (3))

- `Control.ps1 Test-OwnLauncherCreation`: both timestamps truncated to whole microseconds before the compare; a real
  microsecond difference still refuses; an offset-less timestamp throws. `Register-AttemptLauncher` uses it.
  `tests/f1_attempt2_controls.ps1` plants 8 and 9 ticks (pass), 10 ticks and one second (refuse), an unspecified kind
  (throws); red before the change.
- `Control.ps1 Assert-CaptureAttributes`, called by the launcher after the contract precheck and before its first
  evidence write; `contract.py prepare` writes the run root's three `-text` lines once (`capture_attributes_added=<n>`).
  `tests/f1_attempt2_controls.ps1` (a temporary repository with and without the rules; the launcher's call precedes
  `Write-HostObservations`, on the AST) and `tests/test_f1_attempt2_prepare.py` (the lines written, `check-attr` unset,
  idempotent); red before the change.
- `attempt.FRESH_ATTEMPTS = (2, 3, 4, 5, 6)` (Ruling 245 (4)); the names derive from the number.

## 4. Before every attempt

The attempt README's sequence runs this rehearsal with the attempt's run root after `contract.py prepare` has written
its rules, and the attempt is launched only on `F1-DRY-REHEARSAL-GREEN` at the source commit the pair names.
