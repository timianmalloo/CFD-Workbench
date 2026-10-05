---
id: proof-ring-b2-profile
title: "Ring B2 profile: the fast ring under a concurrent build"
type: proof-pack
status: active
owner: "@trk-b2"
phase: implementation
tags: [ring, test-cost, profile, concurrency]
links:
  - { to: plan-test-cost, rel: relates-to }
  - { to: proof-ring-b2-moves, rel: relates-to }
summary: "Measured profile of tools/run-tests.sh with and without one concurrent heavy build, the levers tried, what shipped, and the Ruling 84 condition 3 verdict."
review-by: "2026-11-04"
---

# Ring B2, item 2: the fast ring beside another heavy job

Machine: 16 logical CPUs (12 P, 4 E), macOS, Release. Other agents ran on it all session (ambient 1-minute load 15-125), so
every wall time below is a **ceiling** for a quiet machine, and CPU-seconds (`times`, user + sys of every child) are the
load-independent figure. Load is printed beside each row. Raw logs: scratchpad `b2/ring-*.log` (not committed; the table is the record).

## Where the time went (Verified)

| Reading | Value | Source |
|---|---|---|
| Idle baseline (3 runs) | wall 57/51/50 s, Desktop 51/50/50 s, Core parts 38-40 s | `docs/proof/ring-oct05/baseline-2026-10-05.csv` |
| Ring CPU before | 553-589 CPU-s (16 cores: floor 35 s) | 5 runs, `cpu` line |
| Desktop children, summed | about 300-330 s over 8 slots | `SUITE-TIME` lines |
| Core, 2 parts | part 1 33 s, part 2 47 s (the second held the heavier checks); under a build, part 2 outlasted Desktop (57-61 s vs 54-56 s) | ring logs |
| Desktop tail | greedy fill of the old mode order ended at 44.7 s although work / 8 slots is 37.6 s | simulation from `SUITE-TIME`, matched the 44.7 s measured |

The long pole was Desktop, then Core part 2, both CPU-bound on a CPU-saturated machine.

## Levers tried (paired or repeated runs, same tree)

| Lever | Result | Verdict |
|---|---|---|
| `DOTNET_TieredPGO=0` (dynamic PGO instruments and re-JITs every hot method; a 5-50 s process never earns it back) | CPU 553-568 to 476-487 (-15 %), wall 3-6 s shorter, 2 pairs | **shipped** (`export` in `tools/run-tests.sh`) |
| `DOTNET_gcConcurrent=0` | CPU 556, but the run sat at load 92-106; no gain visible | not shipped |
| `DOTNET_GCgen0size=128 MB` | CPU 481, same as PGO off alone | not shipped |
| `TC_CallCountThreshold=512`, `OSR_HitLimit=2` | CPU 469 and 473, inside the noise of PGO off alone | not shipped |
| Core in 3 parts, not 2 (`jobs=` in `run-tests.sh`) | Core long part 47 s to 25-28 s quiet; wall 60-63 s to 58 s under a build | **shipped** |
| Desktop slots 8 to 12 (`Spawn`) | worse: wall 71-84 s at load 143-176, one run with 6 checks failing from starvation | reverted |
| Analysis started 2 s before the long jobs (`run-tests.sh`) | Analysis 4.8-5.0 s to 4.3-4.4 s in the denser ring | **shipped** (item 1) |
| `nice -n 5` on the long jobs instead | Analysis 4.8-5.0 s, no change | dropped |
| Desktop modes in true longest-first order (`WorkbenchTests.cs`, one list) | Desktop 44.7 to 41.6-42.8 s in 4 clean runs (ambient load 51-65); idealised fill 44.7 to 40.6 s | **shipped** |

## Result: the ring beside one concurrent heavy job

The heavy job is `dotnet build CFDWorkbench.slnx -c Release --no-incremental` in a second worktree, **repeated back to back
for the whole ring** (11-16 builds per ring; one build is 3-4 s). That is a harder load than one build, and it drove the
1-minute load to 100-125. Ambient load from other agents was added to it.

| Config | Run | wall s | net s | Desktop s | Core part 2 s (2/2 before) | CPU-s |
|---|---|---|---|---|---|---|
| before (tip of main) | 1 | 63.2 | 61.2 | 59.6 | 60.2 | 576 |
| before | 2 | 60.9 | 59.5 | 58.4 | 58.2 | 562 |
| before | 3 | 63.0 | 61.8 | 58.8 | 60.8 | 561 |
| PGO off + Core 3 parts | 1-3 | 56.8 / 55.0 / 56.3 | 55.4 / 53.8 / 55.1 | 54.4 / 52.9 / 54.0 | 34-38 | 503 / 487 / 488 |
| **final** (+ Desktop order, cheaper Analysis, 2 s stagger) | 1 | 57.2 (5 s cold build) | 52.3 | 49.2 | 32.2 | 489 |
| **final** | 2 | 53.4 | 52.1 | 49.0 | 34.3 | 497 |
| **final** | 3 | 52.8 | 51.6 | 48.5 | 31.7 | 482 |

The target (ring <= 60 s with one concurrent heavy job, 3 runs) holds: 57.2 / 53.4 / 52.8 s, against 63.2 / 60.9 / 63.0 s before. (Ambient load was lower in these three runs, 12-40, than in the earlier rows; the heavy-job load is the same.)

## Ring alone, no extra job (ambient load 15-65)

| Config | wall s | Desktop s | CPU-s | end load |
|---|---|---|---|---|
| before | 51.1 | 49.0 | 533 | 16.6 |
| PGO off + Core 3 parts | 46.5 / 46.9 / 48.4 | 44.7 / 45.1 / 46.5 | 467 / 468 / 471 | 15.7 / 20.1 / 40.7 |
| final, Desktop order only | 44.6 / 43.9 / 44.4 / 44.2 | 42.8 / 41.6 / 42.6 / 42.4 | 474 / 464 / 470 / 472 | 89.6 / 63.2 / 64.8 / 66.5 |
| **final** (all of item 2 and the item-1 rework), end load 12-18 | 44.4 / 46.2 / 45.7 | 40.3 / 42.4 / 42.0 | 460 / 485 / 479 | 12.5 / 17.8 / 17.9 |

## Ruling 84 condition 3

Desktop <= 43 s and wall <= 50 s: three runs of the final config at end load 12-18 (a quiet machine by Ruling 84's own gate, <= 24) read
Desktop 40.3 / 42.4 / 42.0 s and wall 44.4 / 46.2 / 45.7 s (net 43.0 / 45.2 / 44.7 s); four earlier runs of the same ring order, at load 51-90,
read Desktop 41.6-42.8 s. One run of a variant that was dropped (nice +5) read 43.2 s. **C-3 and C-4 are reverted to the section 13.4 limits**
(C-3 50,000 ms net of build, C-4 43,000 ms); the deltas, the bases and the baseline constant are removed from `tools/check-test-costs.py`; the
load gate stays (Ruling 87). C-3 stays net of the build because a cold build (5 s) is not a test cost, the reading Ruling 84 itself used for its bases.

Risk: the margin on C-4 is 0.6-2.7 s. If a join at end load <= 24 reads Desktop over 43 s, that is a real red under this rule; the
fix is the Desktop order list (one line) or a ruling, not a quiet-load re-base without a 3-run record.

## Not covered by a number

The `SectionEditor_DragMove_DrawsWithinOneFrame` check failed once in a run at load 100 (6 checks, one cascading
`DSL-DRAFT-OWNED`), and a 12-slot run at load 143-176. Both are CPU starvation of a timing-sensitive check; Ruling 81 (a load-gated
frame check) is still open and was not in this dispatch.
