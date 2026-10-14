---
id: coordination-handoff-2026-10-13-pc-sole-leader
title: "Handoff 2026-10-13: the PC becomes the sole leader"
type: proof-pack
status: reviewed
owner: "@leader-3122f106"
tags: [coordination, handoff, leader, windows, f1]
links:
  - {to: plan-test-cost, rel: relates-to}
review-by: 2026-10-27
summary: >-
  Ruling 236 ends the two-machine split. From 2026-10-14 the PC's Claude Code session is the single executor and owns main.
  This file lists the state of main, the work in flight, the open items, and the commands the PC leader needs.
---

# Handoff 2026-10-13: the PC becomes the sole leader

**Read this first on the PC, after `git fetch` and `git pull` on main.** Then read `.claude/knowledge/windows-proof-runbook.md`
(AGENTS.md requires it for PC sessions) and the newest rulings in `docs/notes/rulings.md`, from Ruling 225 to the end.

## 1. Who decides what now

- **Ruling 236 (operator):** the PC session is the single executor and the sole leader. It joins, runs readiness and pushes main.
  No Mac leader, Mac track or Mac review exists any more. Do not wait for one.
- The Mac leader session (3122f106) has stopped. Its leader lease was local to the Mac and has been released. `refs/coord/leader`
  was never on origin, so nothing on the PC needs to be reclaimed from the Mac.
- **Operator matters** still go to the operator: spec, copy, UI direction, irreversible actions, and raising a repair cap.
- **Non-operator matters** go to a Fable-owner reviewer if the PC session can run one. If it cannot, they go to the operator.
- Rules carried over unchanged:
  - the two-cycle repair cap;
  - no Windows user path, machine SID or raw hostname in the public repo or on GitHub;
  - red-first tests;
  - class → sweep → derive → prevent in `docs/lessons/defect-classes.md`;
  - never cite a ruling number before its heading exists (the next one is "the next free ruling number").

## 2. State of main at handoff

- Main is pushed with readiness green. The last product and tooling joins this session were:
  - PR #39: F1 attempt-2 offline admission source;
  - PR #40: PC desktop-slot calibration, `limit.desktop_slots=8` for `ring-pc-win`;
  - DTJ3: the slot picker reports `AT-EDGE` (exit 3), and the measurement loop stops on a killed child (exit 126 or more) or on an interrupt.
- Open PRs: none.
- Rulings end at **236**.

## 3. Work in flight (PC)

### F1 attempt 2: contract preparation, cycle 3 of 3 (Ruling 235)

- **Where:** the uncommitted candidate is in `C:\Projects\CFD-Workbench-win-gpu-f1-attempt2-contract` on branch
  `win/gpu-f1-attempt2-contract`. Cycle 2's diff SHA-256 was `73f84ab7…`.
- **Cycle 3 scope, all offline (Ruling 235).** Nothing else without a new ruling:
  1. **Empty stderr is byte-exact.** The `leader-renew.stderr.txt` check in `Invoke-Preflight.ps1` compares the file's byte length
     to 0, never a decoded or trimmed string. Red-first tests: a BOM-only file (`EF BB BF`) and a whitespace-only file are both
     refused. Sweep the packet's other "must be empty" evidence checks for the same shape.
  2. **Activation floor evidence (PR #39 review C1).**
     - `leader_supervisor.renew()` returns `expires_in_before`, read before the CAS write.
     - `Get-AttemptLeaderEvidence` requires the first renewal event to carry `expires_in_before >= 200`. Write that test red first.
     - **Do not** move `Capture-AttemptLeader 'activation'` above `Start-AttemptRenewal`. `Start-AttemptRenewal` refuses a
       pre-existing `leader-activation.json` (`F1-RENEW-PREEXISTING-ARTIFACT`), and the evidence check refuses any sample older
       than the renewal start marker.
  3. **The contract pair.** Commit `invocation-contract.json` and the requested-form `attempt-authorization.json` from the
     packet's own `prepare` path.
- **If cycle 3 fails review, the track stops** and goes back to the operator.

**Selections already made for attempt 2 (posted on PR #39):**

| Item | Value |
|---|---|
| Execution worktree | `C:\Projects\cfd-workbench-win-gpu-f1-r225-attempt2` (new) |
| Branch / session | `win/gpu-f1-attempt2` / `pc-f1-attempt2` |
| Run root | `docs/proof/win-gpu-f1-preflight/attempt-2` |
| Run id | `f1-attempt2-<YYYYMMDD>`, the preparation day |
| Leader epoch | The PC-local `mac` designation that is live at preparation. Reuse it if `coord leader who --json` shows `live` with `expires_in >= 200`; otherwise reclaim (Ruling 226). TTL stays at the default 300 s: never below 260, because the supervisor renews every 60 s and refuses below 200. |

The F1 packet requires the PC-local designation to be named `mac` (`leader='mac'` in `attempt.py` and `contract.py`). That is
only a label in the code. Ruling 236 does not rename it.

**The live authorization (after cycle 3 joins)** is now committed by the PC leader itself (Ruling 236), with a review it records:

1. Take the next free ruling number N.
2. Build the accepted-form authorization with the packet code:
   `attempt.authorization(document, contract_sha, accepted=True, ruling=N)`. Write it to
   `docs/proof/win-gpu-f1-preflight/attempt-2/attempt-authorization.json`.
3. Commit **one** main commit that holds both that file and the `### Ruling N` section. `check_ruling` (`attempt.py`) needs these
   tokens in the section body:
   - `ACCEPT ONE PREFLIGHT ATTEMPT`;
   - the contract path and its SHA-256;
   - the source commit and the run id;
   - `pc-f1-attempt2` and `win/gpu-f1-attempt2`;
   - the exact token `epoch <E>`;
   - the SHA-256 of the committed authorization bytes and the path `…/attempt-2/attempt-authorization.json`.

   The word `PROPOSED` must not appear anywhere in the section, not even as "not PROPOSED".
4. Create the execution worktree at that main commit, so `HEAD:` holds the same bytes.
5. State these points in the ruling too, so a refusal can be read:
   - rollback (`--terminate`/`--unregister` of the target only) stays allowed after lease or supervisor loss;
   - forward work and archive deletion are blocked after admission loss;
   - the keepalive's parent PID 15528 is bound, as well as PID 19296 / child 3524;
   - `leader-renew.stderr.txt` must be exactly 0 bytes.

**Attempts:** attempt 1 is consumed and failed closed; its leftover target was unregistered by hand. Attempt 2 is next. Attempt 3
needs its own source update and ruling. Ruling 225 allows at most three attempts.

## 4. Open items (not started)

| Item | What is known | Next step |
|---|---|---|
| Ring classifier passes a killed Windows harness | `check-expected-failures.py --status 127 --host windows` returns 0 when the partial log holds only listed failures. `run-tests.sh` and `verify-windows-store.py` make that call (class MEASURE-CONTINUES-AFTER-INTERRUPT). | Record each harness's raw exit on one normal Windows ring run (Core, Desktop parts, Analysis, Cli, Python unittests). If every listed-failure exit is 0 or 1, make the checker refuse 126 or more, with a red-first self-test case. |
| Windows ring over budget | It was 210 s against the 60 s budget before `limit.desktop_slots=8`. The Desktop harness alone now takes about 134 s at 8 slots. | Re-measure the ring with the new slot count, then decide the Windows budget or the job shape (`docs/plans/test-cost.md` §9). |
| Windows store evidence stale | `verify-windows-store` is NOT ASSESSED: the evidence is stale since `src/CfdWorkbench.Core` moved `e6121133 → ed2209ce`. | Rerun the store verifier on Windows when convenient. |
| TMX 150 % diagnostic | Ruling 221 allows one PC run with `CFD_DIAG_SCROLL`. It was never run. | Run it once. A product fix follows only if the scroll falls short on Windows. |
| Properties-view hang | The `--properties-view --part=2/2` hang cause is unknown. A TIMEOUT line now names the in-flight check (PVH). | Read the named check on the next real TIMEOUT. |
| Mac slot default was also an edge pick | The Mac's 10-of-16 count came from measuring 8 and 10 only. | Low priority. Only matters if the Mac runs the ring again. |
| Remote branch clean-up | **Done 2026-10-14 (operator).** 59 branches whose commits were all on main were deleted. 23 branches with commits not on main were tagged `archive/<branch>` on origin and then deleted. Origin now has only `main`. | None. To revive one: `git fetch origin tag archive/<branch>` then `git switch -c <branch> archive/<branch>`. New PR branches are deleted after their join. |

## 5. Commands the PC leader needs (Windows: `py -3` for `python3`)

| Purpose | Command |
|---|---|
| Join a branch | `py -3 docs/ai-forward-pack/scripts/conductor-join.py <branch> --epoch <your epoch> --session <your session> --no-push --title "<title>" --audit-shortname <name>` (add `--docs-only` when nothing under `src/`, `tests/`, `tools/` or `cases/` changes). On a condition, fix it on top of the merge, commit, then `--continue`. |
| Before push | `git add .agents/log/` and commit, then `py -3 tools/run-readiness.py` (it must print GREEN for HEAD) |
| Push | `git push origin main`, then `bash tools/verify-push.sh` |
| PII | `py -3 tools/check-proof-pii.py` always scans the machine's own hostname. `py -3 tools/scan-github-text.py` audits PR text. Post GitHub comments only with `py -3 tools/post-github-comment.py --pr N --body-file F`. |
| Rulings | `coord request` then `coord rule next` (`docs/ai-forward-pack/scripts/coord-core.py`). Write the ruling only after its number exists. |
| Docs | `py -3 tools/check-docs.py` (exit 0 before any commit that touches docs) |

The Mac-only helper scripts (`rule_one.py`, `pc-watch.sh`, the track briefs) stay on the Mac and are not needed on the PC.
