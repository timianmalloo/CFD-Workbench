---
id: coordination-windows-gpu-f1-preflight-r217
title: "Coordination plan - Windows GPU F1 dependency preflight"
type: plan
status: proposed
owner: "@pc-f1-preflight-coordinator"
tags: [coordination, worktrees, windows, wsl, gpu, ruling-217]
links:
  - { to: plan-windows-gpu-f1-preflight-r217, rel: implements }
  - { to: plan-windows-gpu-f1-r211, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  One Sol packet author, one deterministic live executor and an Astra owner at both hard gates run the Ruling 217
  preflight serially; the Mac remains leader and all install, build and case execution stays closed.
---

# Coordination plan — Windows GPU F1 dependency preflight

## Layer state

| check | result | meaning |
|---|---|---|
| Pack doctor | 0 FAIL, 3 WARN, 19 PASS | Coordination scripts and all harness surfaces are present; use `py -3` on Windows. |
| Artifact registry | 8 classified patterns; drivers registered | Derived and register artifacts use their configured mechanisms. |
| Worktrees | 45 observed; this coordinator has an isolated tree at `origin/main` `3d428734` | Each writing track receives another isolated worktree; no shared index. |
| Sessions | 7 old sessions observed plus this coordinator | Old F1 sessions have no claims and must be ended; they are not reused as write locations. |
| Leadership | Mac remains leader; epoch 5 is current after epochs 3 and 4 expired during bounded packet work | Read the live epoch at every remaining join/dispatch, pass that value to `conductor-join.py`, and bind the separately reviewed invocation contract to the live value; any change stops the pending action. |
| Doorbells | Not recorded | GitHub PR comments plus the committed xmsg register are the cross-host path. |
| Host preflight | 1,844,653,740,032 C: bytes free; only protected distro present | Recheck inside the reviewed live packet; current observation is not a future guarantee. |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/plans/windows-gpu-f1-preflight-r217.md` | authored | Coordinator owns optimized graph. | Single owner. |
| `docs/coordination/windows-gpu-f1-preflight-r217.{md,html}` | authored | Coordinator owns plan pair. | Single owner. |
| `docs/proof/win-gpu-f1-preflight/packet/**` | authored packet | Sol packet author. | Single owner until frozen. |
| `docs/proof/win-gpu-f1-preflight/run-1/**`, `receipt.md`, lock/licence/capacity artifacts | authored evidence | Deterministic executor writes captures; evidence author derives only from them. | Serial ownership transfer after packet freeze. |
| `docs/reviews/windows-gpu-f1-preflight-packet.md` | authored | Astra packet reviewer. | Astra only. |
| `docs/reviews/windows-gpu-f1-preflight-result.md` | authored | Astra result reviewer. | Astra only. |
| `docs/docs-index.js`, `docs/audit/audit-data.js` | derived | `coord regen` / docs graph. | No coordination needed. |
| `docs/audit/audit-log.jsonl`, `docs/coordination/xmsg.jsonl` | register | Append through repository tools; union merge. | No coordination needed. |
| `docs/notes/rulings.md`, protected distro, existing B2 evidence | read-only authority | Mac/operator or prior immutable evidence. | No PC authoring authority. |

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---:|---|---|---|
| PACKET — owner `pc-f1-r217-packet`; decision owner `mac` | exactly `docs/proof/win-gpu-f1-preflight/packet/**` | This plan; captured live leader epoch | T1 | 0 | 30 calls; 30k tokens; 30 min; 100k context. Deadline/budget: stop before WSL mutation and post blocked handoff. Termination: frozen self-tested commit or blocker. | Self-tests, prohibited-command scan, two-core/nice capture, exact pre-cleanup checkpoint, continuous guard, idempotent cleanup and whole-step deadline; no live run | Codex Sol; observed-only edit boundary; isolated worktree; doorbell GitHub/xmsg observed-only |
| PACKET-REVIEW — owner `pc-f1-r217-astra-packet`; decision owner `mac` | exactly `docs/reviews/windows-gpu-f1-preflight-packet.md` | Frozen PACKET commit and carried epoch | T1 | 0 | 10 calls; 15k tokens; 15 min; 100k context. One repair maximum; second VETO terminates before live run and posts Mac handoff. | Astra ACCEPT/VETO over authority, trust, CPU/nice, VHD cap, guard, checkpoint order, timeout and cleanup | Codex Astra; observed-only edit boundary; isolated worktree; doorbell GitHub/xmsg observed-only |
| EXECUTE — owner `pc-f1-r217-executor`; decision owner `mac` | exactly `docs/proof/win-gpu-f1-preflight/run-1/**`, `receipt.md`, `transaction-lock.json`, `license-map.json`, `capacity.json`, `capture-manifest.json` | Joined ACCEPTED packet; same live epoch; exact host prechecks | T1 | 0 | One live attempt; 60 min live cap including final guard/absence; 40k tokens and 30 min post-live closeout. At 45 min stop new work; checkpoint by 50 min; reserve 10 min for cleanup. Any failure terminates live work, checkpoints available evidence, cleans up and posts blocker; no retry. | Pre-cleanup commit precedes deletion; target absent, protected continuous-guard binding equal, exact stage exits, row-complete evidence or blocked receipt | Codex Sol coordinating deterministic scripts; isolated worktree and exclusive WSL resource; doorbell GitHub/xmsg observed-only |
| RESULT-REVIEW — owner `pc-f1-r217-astra-result`; decision owner `mac` | exactly `docs/reviews/windows-gpu-f1-preflight-result.md` | Frozen EXECUTE cleanup commit and carried epoch | T1 | 0 | 10 calls; 15k tokens; 15 min; 100k context. Deadline: VETO if observed evidence is incomplete; termination: committed ACCEPT/VETO, no live retry. | Astra ACCEPT/VETO of transaction, trust, reconciliation, capacity, checkpoint order and cleanup | Codex Astra; observed-only edit boundary; isolated worktree; doorbell GitHub/xmsg observed-only |
| REVIEW — owner and decision owner `mac` | Mac review and rulings only | Joined result and Astra verdict | T1 | 0 | 20 min target; if unavailable, execution stays closed and PC posts blocked status; termination is Fable disposition or explicit hold | Fable disposition and any later build/benchmark ruling | Mac/Claude/Fable through GitHub; GitHub is the observed cross-host doorbell |

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Packet freeze before review | Review input must be byte-stable. | PACKET then PACKET-REVIEW |
| Packet acceptance before live run | A reviewer must clear authorization and cleanup boundaries before mutation. | PACKET-REVIEW |
| Target creation through pre-cleanup commit | All stages share one target distro, VHD, clock and protected continuous guard; evidence must be committed before deletion. | EXECUTE |
| Deletion and unregister after checkpoint | Ruling 217 orders cleanup after the manifest-bound evidence checkpoint; the same owner preserves the initiating error and exact target identity. | EXECUTE |
| Final evidence after cleanup | The cleanup receipt binds the pre-cleanup commit and verified absence without re-reading deleted packages. | EXECUTE |
| Result review before join | Executor cannot clear its own evidence veto. | RESULT-REVIEW |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| Coordinator → PACKET | Encode Ruling 217 as one fail-closed packet. | Frozen commit with planted-negative self-tests. |
| PACKET → PACKET-REVIEW | Clear the packet for exactly one live attempt. | Astra ACCEPT or VETO; one repair maximum. |
| PACKET-REVIEW → EXECUTE | Run only the reviewed hashes under a live epoch. | Pre-run hash and authority receipt. |
| EXECUTE → RESULT-REVIEW | Adjudicate committed raw and derived evidence. | Astra ACCEPT or VETO; no repeat live run. |
| RESULT-REVIEW → Mac | Decide whether dependency/capacity closure is sufficient for another ruling. | GitHub review and ruling; build remains closed. |

Shared-surface guards are jointly satisfiable: only PACKET writes script sources; EXECUTE verifies their hashes and
writes run evidence; reviewers write separate files and do not edit inputs. Scan guards recurse only within
`docs/proof/win-gpu-f1-preflight/packet`, token-match prohibited install/build/source/case commands, and allowlist only
the self-test fixtures that must be rejected.

The coordination layer is installed once in the primary checkout. Linked worktrees inherit the clone-local drivers
and hooks; no track runs `coord install` in its worktree. Every dispatch contract carries the renewed Mac epoch, and
every join passes that exact epoch to `conductor-join.py`; an expired or changed epoch stops the track.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Separate repository/index agents | Transaction closure depends on one apt state; splitting creates version seams. |
| Parallel licence extractor | It consumes downloaded packages and cannot start before trust verification. |
| Parallel capacity monitor | It shares the exclusive WSL/VHD transaction and must stop that same owner. |
| Grok executor | No qualified mutation/cleanup boundary is observed for this host. |
| Build or benchmark track | Ruling 217 keeps both closed. |

The 15× coordination multiplier is paid only for Sol context isolation and Astra independent vetoes. Maximum active
width remains one; no machine-time speed-up is claimed.

## Order of operations

| # | action | cost | why now |
|---:|---|---|---|
| 1 | Repair the Astra plan VETO; reclaim Mac leadership at epoch 3; commit/push the accepted graph/coordination pair. Later, stop on the expired lease and reconcile the same Mac leader at epoch 4 before the packet repair. | One docs-only change plus one authority-seam reconciliation | Immutable authority and a live fence precede each dispatch. |
| 2 | Create PACKET worktree; Sol authors and self-tests the packet without WSL mutation. | 30 calls / 30 min | Removes live-script uncertainty before authority review. |
| 3 | Join packet; create Astra review tree; accept or return one repair. | 10 calls / 15 min | Independent hard gate. |
| 4 | Join accepted packet; create EXECUTE tree; recheck epoch, host space, distro set and script hashes. | Deterministic | Last safe stop before effects. |
| 5 | Execute one 60-minute transaction: continuous guard; target-only 8 GiB creation; CPU 0-1/nice 10 helpers; signed simulation/download/inspection; manifest-bound pre-cleanup commit; deletion and unregister; final guard/absence receipt. | One live attempt | Exact Ruling 217 scope and ordering. |
| 6 | Commit result; run Astra result review; join verdict through conductor. | 10 calls / 15 min plus gates | Executor cannot self-clear. |
| 7 | Run final docs/readiness gates, push a PR and xmsg Mac. | One convergence pass | Mac owns the next ruling. |

## Status

| state | item |
|---|---|
| Completed | Ruling 217 read; coordinator and packet worktrees created; accepted plan and initial packet committed; Astra VETOed the exact packet on six repair classes; the sole repair is committed; Mac remains leader and is currently epoch 5. No live packet ran. |
| Remaining | One packet repair and Astra re-review; if accepted, one live preflight, result review, and gated PR handoff. |
| Best next action | Join the sole packet repair with the live epoch, then dispatch Astra's exact-byte re-review without WSL mutation. |
