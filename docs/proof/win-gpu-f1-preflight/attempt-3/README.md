---
id: proof-win-gpu-f1-attempt3-plan
title: "F1 attempt 3: the last attempt under Ruling 225 (Ruling 243)"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, offline, f1, attempt-3, decision-request]
links:
  - { to: rulings, rel: depends-on }
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-attempt2-offline-request, rel: depends-on }
  - { to: proof-win-gpu-f1-inputs, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Attempt 3 of Ruling 225, the last under its three-attempt cap, prepared on the Windows ARM64 laptop under Ruling 243:
  the packet generalized to the contract's attempt number, the bash.exe rule of DR-5 (a), the selections, the inputs
  (unchanged from attempt 2), the pair produced by contract.py prepare, and the live authorization request.
---

# F1 attempt 3: the last attempt under Ruling 225 (Ruling 243)

**Where this stands.** Attempt 2 ran under Ruling 242 and refused before any WSL effect (`../attempt-2/README.md`
section 7): a hand copy of the input list in the supervisor's admission, repaired on main, and the exclusive-PC scan
counting the agent harness's Git-for-Windows `bash.exe` as a WSL actor by name (DR-5). Ruling 243 settled DR-5 as (a)
and directed this preparation. Attempts consumed: 1 and 2; this is the last under Ruling 225's cap.

## 1. What changed in the packet for this attempt (Ruling 243)

| Change | Where | Proof |
|---|---|---|
| `bash.exe` is a WSL actor only when its `ExecutablePath` is `%SystemRoot%\System32\bash.exe`; an unknown path stays an actor; a command line that invokes `wsl` stays an actor | `packet/Control.ps1` `Assert-AttemptExclusivity` | `tests/f1_attempt2_controls.ps1` plants Git's bash (passes under both protected states), the System32 bash, a bash without a path, an empty path, and a Git bash invoking wsl (each refuses); both runtimes |
| The fresh-attempt scheme keys on the contract's attempt number, 2 or 3, names derived from it | `attempt.FRESH_ATTEMPTS`; `contract.fresh_attempt`, `prepare --attempt`; `leader_supervisor`; `Control.ps1 Get-InvocationRunRoot`; the launcher's gates (`-ge 2`) | `tests/test_f1_attempt2_admission.py` (attempt 3 admitted, mixed bindings refused, no literal `== 2` gate left), `tests/test_f1_attempt2_prepare.py` (`--attempt 3` lands under `attempt-3`; a missing or foreign number refuses without outputs), `tests/test_f1_attempt2_renewal.py` (the supervisor admits 2 and 3 only), `tests/f1_attempt2_controls.ps1` (`Get-InvocationRunRoot` for attempt 3) |

## 2. Selections

| Field | Value | Note |
|---|---|---|
| `attempt` | `3` | the last under Ruling 225 |
| `execution_worktree` | `D:\Projects\CFD-Workbench-f1-attempt3` | absolute, uncreated at prepare time, created from the accepted commit after the ruling (`git worktree add`, since `coord worktree new` derives its path from the branch) |
| `execution_branch` / `session` / `run_id` | `win/gpu-f1-attempt3` / `pc-f1-attempt3` / `f1-attempt3-<date>` | derived from the attempt number (Ruling 243 (2)) |
| `run_root` | `docs/proof/win-gpu-f1-preflight/attempt-3` | this folder |
| `architecture` / `protected` | `arm64` / `none` | as attempt 2 |
| `leader` / `leader_epoch` | `mac` / the epoch a fresh `coord leader pin mac` returns | epoch 3 lapsed after attempt 2; the TTL is at most 300 s, so a renewer keeps the pin live until the launcher's own supervisor takes over |
| host floors | `host_start_floor_bytes 19669226335`, `host_reserve_bytes 10737418240` | the attempt-2 worksheet (`../attempt-2/inputs-receipt.json`); the D: free space is unchanged in kind |

## 3. Inputs

Unchanged from attempt 2: the five external inputs in `D:\reviewed` at the pins recorded in
`../attempt-2/inputs-receipt.json`, the cloud-image keyring under the execution worktree (`../inputs/README.md`,
fingerprints confirmed by a reviewer other than the author), the B2 licence map under the execution worktree. The
launcher hashes every one before the first WSL effect.

## 4. Sequence

1. Ruling 243, the packet change above and this document join `main` (code ring, both runtimes).
2. `AGENT_SESSION=mac coord leader pin mac --ttl 300`; `coord leader who` gives the epoch; a renewer keeps it live.
3. In a fresh worktree at `main`: `py -3 contract.py prepare --source-commit <HEAD> --attempt 3 --execution-worktree
   D:\Projects\CFD-Workbench-f1-attempt3 --leader-epoch <epoch> --requested-ruling <next> --architecture arm64
   --protected none --host-start-floor-bytes 19669226335 --host-reserve-bytes 10737418240 --branch win/gpu-f1-attempt3
   --session pc-f1-attempt3 --run-id f1-attempt3-<date>` plus the seven input paths; the pair joins `main` (docs only).
4. A decision request to the operator with every `attempt.check_ruling` token; the ruling is recorded and pushed.
5. The accepted authorization replaces the proposal; the execution worktree is created from that commit; the renewer
   stops; the launcher runs from native `pwsh` as `pc-f1-attempt3`. **No Bash-tool call and no bash-based watcher while
   it runs**: Git's bash is no longer an actor under Ruling 243 (1), but a bash that invokes `wsl` still is, and the
   PowerShell tool is the only safe console during the attempt.

## 5. The pair (A2 for attempt 3, 2026-10-20)

Produced by `contract.py prepare --attempt 3` in a clean worktree at `main` = source commit
`5995f9397a973ab1fbd8286c20e3590462db3759` (`PREPARATION-GENERATED outputs=2 inputs=7 architecture=arm64 protected=none
live_authority=False`).

| File | SHA-256 | Status |
|---|---|---|
| `invocation-contract.json` | `d1de0caa0fcb7475784a7519ef2ef2f884a86de252d2299cdcd4b3a29b9493d5` | committed here |
| `attempt-authorization.json` (proposal) | `0a9171870ce04de737b8001d42e5d1608f4399aac5edbb7c0ae9a79958fe5a47` | committed here; authorizes nothing |
| `attempt-authorization.json` (accepted) | `f419edc76413ffb65b956dc82b6e10e683a15bb7f03b4d54c473292874d2f887` | the proposal re-projected by `attempt.authorization` with status accepted and ruling 244; committed in place of the proposal after the ruling |

Bindings: attempt 3, leader `mac` at epoch 4, run id `f1-attempt3-20261020`, session `pc-f1-attempt3`, branch
`win/gpu-f1-attempt3`, run root `docs/proof/win-gpu-f1-preflight/attempt-3`, execution worktree
`D:\Projects\CFD-Workbench-f1-attempt3`, arm64, protected none, target `D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`,
host floors 19669226335 / 10737418240, the seven inputs at the pins of `../attempt-2/inputs-receipt.json`.

## 6. Residual risks

Those of `../attempt-2/README.md` section 6, plus: the generalized attempt gates (`-ge 2`) are proven by the suites on
both runtimes, not by a live run; the first live run under them is this attempt. A red in the live attempt is a
failed attempt with an immutable receipt and the cap of Ruling 225 is reached.

## 7. Outcome (2026-10-20): attempt 3 refused before any WSL effect; the cap of Ruling 225 is reached

Ruling 244 accepted the pair; the execution worktree was created from `6b1bf813` and the launcher ran from native
pwsh 7.6.7 (arm64) as `pc-f1-attempt3` with no Bash-tool call in flight. Recorded in order: `INVOCATION-CONTRACT-PASS`,
`HOST-IDENTITY-PASS architecture=arm64 cpu=1 gpu=2`, the leader supervisor started and renewed (`leader-renew-events.jsonl`,
`expires_in_before` 287 s), `ROOTFS-SIGNED-PASS`, the activation and immediately-pre-live leader and keepalive samples,
the final precheck (`final-precheck.json`: host free 3,998,435,037,184 bytes against the 19,669,226,335 floor), the
first `wsl --list --quiet` registered as an own launcher. Then `F1-OWN-LAUNCHER-IDENTITY` at second 10 on the second
`wsl --list --quiet`: `Register-AttemptLauncher` equates the CIM creation time (microseconds) with `Process.StartTime`
(100-nanosecond ticks); the first child's ticks happened to end in zero, the second's did not. Stage `not-started`;
no target was created; the registrations are `Ubuntu-24.04` only; `D:\Projects\CFD-Workbench-wsl` does not exist.

The cleanup refused the same identity on its own `wsl --list` children (`owner stop unproved`), committed the
checkpoint (`0e24418f`), and could not bind it: this run root had no `-text` rule in `.gitattributes`, Git normalized
two captures to LF, `manifest.py verify` refused and the final commit was withheld (`MAC-DECISION-REQUIRED:
checkpoint unproved; archive deletion/unregister withheld`). The receipts are `checkpoint/` (the launcher's commit)
and `live/` (`cleanup-receipt.json` is the classified receipt Ruling 244 names), re-committed with their captured
bytes under the added attribute rules (`1d2b9695`, `GIT-BINDING-PASS` on that commit).

Attempts consumed under Ruling 225: 1, 2 and 3. The cap is reached; a further attempt needs the operator's new cap.
The two defects are recorded in `docs/lessons/defect-classes.md` (TIMESTAMP-IDENTITY-ACROSS-PRECISIONS;
RAW-CAPTURE-BLOB-MISMATCH recurrence 4) with their controls proposed, not applied: the track's repair cap is spent.
