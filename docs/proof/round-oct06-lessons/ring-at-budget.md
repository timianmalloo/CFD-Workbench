---
id: proof-round-oct06-ring-at-budget
title: "RING-AT-BUDGET recommendation (operator decision)"
type: proof-pack
status: draft
owner: "@track-ci"
phase: implementation
tags: [round-oct06, test-ring, capacity, c-3, operator-decision]
links:
  - { to: plan-test-cost, rel: relates-to }
review-by: "2026-11-06"
summary: >-
  The fast ring rose from about 47 s to about 49.5 s net in round-oct06 against a 50 s limit. The measured series per
  join, three options with their cost, and a recommendation. The operator decides.
---

# RING-AT-BUDGET: the fast ring is at its limit

**Decision for the operator:** how to buy headroom on C-3 (net ring wall, limit 50.0 s, `docs/plans/test-cost.md` §9).
Nothing was changed by this track.

## Measured series (net ring time, C-3)

Only the readings the round's audit entries and proofs carry. The first row is the figure in the track brief, not
re-observed here (Inferred); `docs/proof/ring-b4/baseline-desktop.csv` gives Ring B4's quiet wall 44.2-51.3 s.

| Where | Net or wall | Source | Note |
|---|---|---|---|
| Start of round | about 47 s | track brief (Inferred) | |
| POL, `pol-a3a-polish` | wall 50 s (Desktop child 44 s) | audit entry | wall, not net |
| `join-cpy` | net 48.3 s | audit entry | after the 1280x800 four-view check moved to readiness (1.6 s) |
| `trk-grp-desk` | net 49.6 s | audit entry | |
| `trk-dx` | net 49.4 s | `docs/proof/dx/red-first.md:39` | seven DX checks moved to readiness first; C-2 read at load 15-19 |
| `trk-grp-fix` | net 50.2 s | audit entry | over the limit; load not recorded in the entry |
| `trk-dx3` | net 50.6 s | audit entry | over the limit; "all PASS but" C-3 |

The brief counts 5 joins in the round; the audit entries carry a net figure for only some of them, so the series has
gaps (Verified: grep of `docs/audit/audit-log.jsonl` for `net` and `wall`). `join-pnl` and `join-grp-core` carry no ring
time. C-3 is load-gated (Ruling 84/87): a reading above 50 s at an end load
over 24 is a COST-MISS, not a failure, so the two readings over 50 s may be load, not growth.

## Options

| Option | What it does | Cost | Headroom bought |
|---|---|---|---|
| A. Re-base C-3 once, as Ruling 99 did for C-4 | Raise the 50.0 s limit to max observed plus 2 s, about 52.6 s (50.6 + 2) | One ruling, one constant and its self-test in `tools/check-test-costs.py`; no test change. Loosens a limit; the 60 s TEST-BUDGET still binds (wall = net + build, build 0-5 s), so the real room is about 55 s net at most (Inferred from `test-cost.md` §9.5) | About 2.6 s, about one round of growth at the 2.5 s per round seen here |
| B. Split the Desktop harness further | Shorten the critical path: Desktop is the long pole at 40-47 s; its 17 children total 274 s CPU on 8 slots, a floor of about 34 s (`test-cost.md` §9.9, arithmetic Inferred) | A build track in `tests/` (partition the longest children, plan-canvas 24 + 20 s alone); §9.8 recorded that a two-job split bought no C-4 margin, so the saving is uncertain, plausibly 2-6 s (Inferred) | 2-6 s if the CPU is not already saturated; none if it is |
| C. Keep moving checks to readiness | Move the costliest fast-ring checks to the readiness ring | About 0.4-1.6 s each (the 1280x800 check read 1.6 s, the DX checks 0.13-0.82 s by COST lines); readiness is at 102-124 s of 240 s, so it has room; the checks then run at merge to `main`, not at every join | 1-5 s for 3-6 checks; 11 moved this round |

## Recommendation (the operator decides)

1. Now: option C for the next 2-3 joins, because it is cheap, reversible and needs no ruling. Pick the checks by `COST`
   line, most expensive first, and keep every moved check in `tools/run-readiness.py`.
2. Next build track: option B, measured first. Run the Desktop children alone with the per-child clock and split only
   the children that sit on the critical path. If it buys under 2 s, stop.
3. Option A only if B fails to deliver, and with a ruling that records the one-time re-base. Re-basing first would move
   the limit and hide the growth rate, which is the signal this round exposed.

A standing caution: the ring grew about 2.5 s over five joins by adding checks that each looked free. The new control
`tools/check-wallclock-asserts.py` does not address this; a per-join net-time series written by the join itself would
(the join prints `wall`; it does not append it to a file). Proposal for the Coordinator, not done here.
