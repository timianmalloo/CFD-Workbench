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

## 4. The pair (2026-10-20)

Produced by `contract.py prepare --attempt 4` in a clean worktree at `main` = source commit
`ace7b708c1d9aa873fe961c84f782d373461c549` (`PREPARATION-GENERATED outputs=2 inputs=7 architecture=arm64 protected=none
live_authority=False`, `capture_attributes_added=3`).

| File | SHA-256 | Status |
|---|---|---|
| `invocation-contract.json` | `2565960a1df5cafbd0acd4ef882fde50eb8c6a5c6c972d1e0077d53659826e90` | committed here |
| `attempt-authorization.json` (proposal) | `83a8bba1c2d8709ebe596dc1af346ac8f9c3ad4ad532dcd447c2be9b3760b8b2` | committed here; authorizes nothing |
| `attempt-authorization.json` (accepted) | `5c01cba89c0f20ee83f21bf0b12a9569b1905db1f7830f41e62e39f9ced44332` | the proposal re-projected by `attempt.authorization` with status accepted and ruling 246; committed in place of the proposal after the ruling |

Bindings: attempt 4, leader `mac` at epoch 5, run id `f1-attempt4-20261020`, session `pc-f1-attempt4`, branch
`win/gpu-f1-attempt4`, run root `docs/proof/win-gpu-f1-preflight/attempt-4`, execution worktree
`D:\Projects\CFD-Workbench-f1-attempt4`, arm64, protected none, target `D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`,
host floors 19669226335 / 10737418240, the seven inputs at the pins of `../attempt-2/inputs-receipt.json`.

**The rehearsal for this run root** (`rehearsal-prepare.json`): the first run, with the attribute rules written but
not yet committed, was red on `bind` only (the scratch worktree reads `.gitattributes` from HEAD): the rehearsal
distinguishes a rule in the working tree from a rule on the commit the attempt will run from. After the pair commit
it is `F1-DRY-REHEARSAL-GREEN` (own-child 5/5, attributes 3/3, `GIT-BINDING-PASS`).

## 5. Residual risks

Those of `../attempt-3/README.md` section 6. The rehearsal covers the own-child registration, the capture attributes
and the bind; the launcher paths after target creation (the WSL stages, the owned stop, the final freeze) have still
run live only on the old PC at attempt 1, and the first arm64 run of them is this attempt.

## 6. Outcome (2026-10-21 UTC): attempt 4 refused 5 seconds into the import; the import rolled back; attempts 5 and 6 remain

Ruling 246 accepted the pair; the accepted authorization (`5c01cba8`) was committed in place of the proposal (`4f8bd9f5`,
main `4e6f67bc`), the execution worktree was created from that commit, the rehearsal ran green inside it
(`F1-DRY-REHEARSAL-GREEN own_child=5/5 attributes=ok bind=ok`), and the launcher ran from native pwsh 7.6.7 (arm64) as
`pc-f1-attempt4` from 00:10:46 UTC with no Bash-tool call in flight.

Recorded in order (`checkpoint/`, `final/`): `INVOCATION-CONTRACT-PASS`; the host observations (00:10:49); the leader
supervisor started and renewed epoch 5 (`leader-renew-events.jsonl`, `expires_in_before` 282 s); `ROOTFS` signature
`GOODSIG`/`VALIDSIG` by the cloud-image key through Git's gpgv 2.4.9 (`SIGNER-PASS`, `KEYRING-PASS keys=2`); the
activation sample (00:10:54); the final precheck (00:10:55, host free 3,998,443,864,064 bytes against the
19,669,226,335 floor, registrations `Ubuntu-24.04` only); two `wsl --list --quiet` children registered as own launchers
at 00:10:54.90 and 00:10:56.80 (`Test-OwnLauncherCreation` passed both: the attempt-3 control held); the host
identity; then `wsl.exe --install --from-file <rootfs> --name cfdw-petsc-cuda132-f1 --location <target> --vhd-size
8GB --no-launch` registered as owned pid 25184 at 00:10:56.97. The utility VM resumed at 00:10:58.8 (the Hyper-V
VmSwitch log) and the target VHD existed at 4,194,304 bytes at 00:10:59.19 (`host-capacity.jsonl`). At second 15.6
(about 00:11:02) the runtime identity sample refused: `F1-EXCLUSIVITY-REFUSED: additional or changed WSL actor:
wsl.exe pid 8464`. The launcher stopped its import child; the receipt's stage is `not-started` (the stage counter
counts Linux stages) and `creation_uncertain` is true.

The cleanup withheld containment three times (`owned-stop: MAC-DECISION-REQUIRED: created target GUID unbound;
containment withheld`; `owned process not proved stopped; archive deletion/unregister withheld`), then proved the
checkpoint (`b8dda9bd`, 36 files) and the final commit (`930078ea`, 52 files, `GIT-BINDING-PASS` read from the live
folder before it was removed): the first attempt on which both binds passed live, so the Ruling 245 capture-attribute
control held as well. `final/cleanup-receipt.json` is the classified receipt Ruling 246 names
(`target_absent: Unknown or blocked`, `cleanup_proved: false`).

**Target absence, verified by hand after the run (00:14 UTC):** `wsl -l -v` lists `Ubuntu-24.04` only; the `Lxss`
registry carries that one key; the target location does not exist (its parent directory exists and is empty); no
`wsl.exe` was alive. The WSL service rolls an interrupted registration back itself: in
`LxssUserSessionImpl::RegisterDistribution` (microsoft/WSL, master) a scope-exit deletes the VHD and the registration
unless the import completed, and the import relays through a handle to the calling client, which the launcher
killed. The rollback seen here is that path (the absence is Verified; the mechanism is Inferred from the source).

**What pid 8464 was.** The receipt did not keep it: the refusal records a name and a pid and drops the parent, the
command line, the creation time and the executable path (the attempt-2 control named the actor, not its identity).
Verified afterwards on this machine: `C:\Windows\System32\wsl.exe` (10.0.28000.2336, 207 KiB) is a stub that launches
`C:\Program Files\WSL\wsl.exe` (WSL 3.0.3.0) as a child about 43 ms later carrying the stub's own command-line text
(observed 00:18:51 UTC: stub pid 8836, child pid 16104). `Register-AttemptLauncher` records the children present at the
instant of registration (here: the conhost rows), so the forwarded child of the five-second import was an unowned
`wsl.exe` at the next identity sample (the 5 s throttle), while the forwarded children of the sub-second list calls
were gone before any sample. The WSL client's from-file install creates no process of its own and the service launches
only the settings OOBE after a completed registration (both read in the source), so the forwarded child is the only
`wsl.exe` the run itself produced. Confidence: the mechanism is Verified; the identity of pid 8464 is Inferred, because
its row was not kept. Class: OWNED-PROCESS-TREE-REGISTERED-ONCE-WHILE-IT-GROWS in `docs/lessons/defect-classes.md`;
the same snapshot also feeds `Stop-OwnedOrContain`, which is why the owned stop could not be proved.

**A defect of the diagnosis itself, reported.** The first process-tree probe declared its parameter as `$Args`, which
is PowerShell's automatic variable, so the stub was started with no arguments: that launches the default distro's
shell. `Ubuntu-24.04` ran from 00:18:51 and again from 00:21:23 UTC until the probe's processes were stopped by hand;
it is `Stopped` since 00:22, with no registration or file changed. This was a WSL effect outside the attempt, within
the already-registered distro, and it is recorded as a recurrence of CASE-INSENSITIVE-VARIABLE-SHADOWS-A-PARAMETER
(the automatic-variable variant). The corrected probe passed its arguments and produced the observation above.

Attempts consumed: 1 to 4. Attempts 5 and 6 remain under Ruling 245; the repair loop restarted by that ruling has
its two cycles. The control is proposed to the operator as DR-6 (ownership as a relation over the process tree at
each sample, the owned stop over the whole tree, the refusal keeping the refused row and the actor population, a
rehearsal step over the real stub, red-first tests), not applied in this node.
