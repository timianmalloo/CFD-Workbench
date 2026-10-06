---
id: proof-ring-split-profile
title: "Ring split profile: a Desktop 1/2 + 2/2 split cannot pay"
type: proof-pack
status: active
owner: "@trk-spl"
phase: implementation
tags: [ring, test-cost, profile, split]
links:
  - { to: proof-ring-b2-profile, rel: relates-to }
  - { to: plan-test-cost, rel: relates-to }
summary: "Round-oct06 track SPL profile of tools/run-tests.sh at main fedac036 (Ruling 122): the ring is CPU-bound and Core 3/3 is co-critical, so splitting the Desktop job into two parent jobs saves nothing; no split shipped."
review-by: "2026-11-06"
---

# Ring split profile (2026-10-06, main fedac036, Release, 16 logical CPUs: 12 P + 4 E)

Verdict: **stop, no split shipped** (Ruling 122: a split that saves under 2 s is not shipped). Every number below was observed;
ambient load was 12-21 during the runs (a quiet machine by the C-3/C-4 gate, load <= 24, until the last two runs).
Raw logs are scratchpad files, not committed; this table is the record.

## Full ring, three runs (tools/run-tests.sh, dispatch gate GO before each)

| Run | build s | net ms | Core 1/3, 2/3, 3/3 ms | Desktop ms | Analysis 1/2, 2/2 ms | CPU-s | load start -> end | C-3 |
|---|---|---|---|---|---|---|---|---|
| 1 | 5 | 49,727 | 31,662 / 31,974 / 45,500 | 46,982 | 4,587 / 4,555 | 538 | 14.0 -> 20.1 | ok |
| 2 | 2 | 50,166 | 32,563 / 32,892 / 46,438 | 47,423 | 4,258 / 4,405 | 548 | 18.6 -> 20.6 | **red, 166 ms over** |
| 3 | 1 | 49,290 | 31,957 / 32,087 / 45,228 | 46,540 | 4,263 / 4,409 | 544 | 20.6 -> 20.2 | ok |

(The 51,855 ms net in the brief was one run at load 10-15; these three bracket the 50,000 ms C-3 limit.)

## Desktop alone (same binaries, TieredPGO off, own 10 slots), three runs

42.5 / 42.5 / 42.7 s at load 12-17. In the ring it reads 46.5-47.4 s: sharing the CPU with Core and Analysis costs it 4-5 s.

## Questions from the brief

| Question | Answer (observed) |
|---|---|
| One serial chain (fixture setup, shared window)? | **No.** Desktop is already 18 child processes over 10 slots (`Spawn`, `WorkbenchTests.cs`). The longest child is `--plan-canvas --part=1/2`, 31.4-31.8 s; the rest are 0.6-30 s. Each child owns its own window fixture. The sum of child times is 358 s (run 1); 358 / 10 slots = 35.8 s. |
| Process start and fixture setup cost? | Shell-model, the smallest child, runs in 0.6-0.8 s including process start; fixture setup is inside each child and parallel. No serial prefix: the 2 s stagger in `run-tests.sh` is outside the clock. |
| Slot use? | 10 slots at about 1.4 cores per child = 14 cores of demand from Desktop alone, on 16 logical CPUs of which 4 are E cores. Core adds 3 processes and Analysis 2. Demand exceeds capacity for the whole run. |
| Bound by CPU? | **Yes.** The ring spends 538-548 CPU-s; 16 cores give a floor of 34 s, the measured net is 49-50 s (68 %); on 12 P + 4 E cores the practical floor is about 41 s. |
| Is Desktop the only long pole? | **No.** Core 3/3 ends at 45.2-46.4 s, within 1.3-1.5 s of Desktop (46.5-47.4 s). It holds 240 checks, but runs 14 s longer than Core 1/3 and 2/3 (32 s). Cutting Desktop alone moves the ring to the Core 3/3 wall. |

## Does a split pay? No

A `Desktop 1/2` + `Desktop 2/2` split is two parent processes sharing the same 18 children. It changes how slots are held, not how much CPU is used.
Two outcomes are possible, and both were measured or bounded:

1. **Same total slots (5 + 5):** the same 10 children run at once; the schedule is the greedy fill of the same durations, so no change. Expected saving 0 s.
2. **More total slots (6 + 6 = 12, the most favourable split):** run with `Spawn` at `ProcessorCount * 3 / 4` (12 slots, a temporary edit, reverted).
   Simulation of the measured child times predicts 43.3 s -> 39.8 s for Desktop (-3.5 s). **Measured: worse.**

| Run (12 slots) | net ms | Core 1/3, 2/3, 3/3 ms | Desktop ms | CPU-s | load start -> end | C-3 |
|---|---|---|---|---|---|---|
| 1 | 53,986 | 38,345 / 38,664 / 51,248 | 48,751 | 551 | 14.5 -> 22.8 | **red** |
| 2 | 53,839 | 38,317 / 39,338 / 51,075 | 48,546 | 554 | 22.8 -> 24.9 | COST-MISS (end load 24.9 is over the 24 gate) |

Desktop did not get faster (48.5-48.8 s against 46.5-47.4 s) and Core got 6-7 s slower, because the extra children took CPU from it.
Net is 3.8-4.7 s worse than 12 slots' baseline of 49.3-50.2 s. This matches the B2 finding (slots 8 -> 12 worse) and the B3 choice of 10.

## Conclusion and next options (not done here)

- No split shipped; `tools/run-tests.sh`, `tools/check-test-costs.py` and the Desktop harness are unchanged. No threshold moved.
- The ring is CPU-bound with Core 3/3 and Desktop both near 46-47 s. Levers that remain, each outside this track's scope:
  1. **Rebalance Core 3/3** (45-46 s against 32 s for parts 1 and 2; four parts, or move the heavy checks by measured cost). Needed together with any Desktop gain, because Core 3/3 is the next wall. Core parts are out of scope for SPL.
  2. **Cut CPU, not the schedule:** the ring costs 540 CPU-s; every second of CPU removed from the Desktop children (for example the plan-canvas and properties-view fixtures, the two longest) is worth about 1/16 s of net.
  3. **Ruling 122 options A and C** (the readiness move and the limit options named in the ruling): this profile is the evidence that a split alone does not recover the margin.
