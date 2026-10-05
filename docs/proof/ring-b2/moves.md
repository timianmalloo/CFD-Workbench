---
id: proof-ring-b2-moves
title: "Ring B2 move list: Analysis under C-2 with every A8.4 check kept in ring 0"
type: proof-pack
status: active
owner: "@trk-b2"
phase: implementation
tags: [ring, test-cost, c-2]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: plan-test-cost, rel: relates-to }
summary: "Analysis harness made cheaper without moving any A8.4 oracle or observed-order check out of the fast ring; two n128 convergence halves move to readiness; PASS union loses no name."
review-by: "2026-11-04"
---

# Ring B2, item 1 (rework after the test-architect's condition 2)

Problem (Verified, join log `join-tip-salvage-s1.log`): at end load 13 the Analysis harness took 5,386 ms against C-2's 5,000 ms.
C-2 is unchanged. The first version of this item moved F-6, F-15, F-15b, F-18 and F-19 to readiness; the spec (A8.4, design
13.4: "analytic oracles and observed order stay in ring 0") does not allow that, so it is replaced by this version.

## What changed, per check (fast ring, in-ring ms at end load 12-18)

| Check | Ring | Before | After | How, and what did not change |
|---|---|---|---|---|
| `F6_ObservedOrder` | A (kept) | 517 | 346 (237 alone) | The honest trio and the wake-per-panel mutant trio share no data; they are built in one `Parallel.Invoke`. Every assertion, tolerance and the mutant check are unchanged. |
| `F15_EllipticWing_InducedAngleUniform`, `F15b_...` | A (kept) | 14 / 34 | 15 / 36 | untouched; they read F-6's shared trio |
| `F2`, `F5` | R (already) | | | untouched |
| `F18_Camber4_DefaultLatticeTipConverges` | A (kept, smoke) | 236 | 43 | Fast half is n32 and n64 only (one n64 solve each of the wing and the horseshoe mutant; the flat n64 solve is a `Lazy` shared by F-18 and F-19). Asserts: tip alpha_i finite and within +-10 deg (measured 4.96 deg; mutant -3.5e6 deg), within 0.1 deg of n32, kappa1 <= 10x flat, and the mutant outside +-10 deg. |
| `F19_Washin1_DefaultLatticeTipConverges` | A (kept, smoke) | 233 | 43 | Same (measured 2.37 deg; mutant 725 deg). |
| `Readiness_Camber4_N128Convergence` (new, R) | R | | est. 0.4 s | The original F-18 body unchanged: n64 vs n128 tip alpha_i within 0.1 deg, CL within 1 %, kappa1, horseshoe mutant outside the 0.1 deg band of n128. |
| `Readiness_Washin1_N128Convergence` (new, R) | R | | est. 0.4 s | The original F-19 body, same. |
| `tools/run-tests.sh` | | | | The long jobs (Core parts, Desktop) start 2 s after Analysis and Cli, before their own clocks start (so C-4 is not charged). The first 2-3 s of 13 busy processes is a JIT surge Analysis was queued behind. Total CPU is unchanged; the ring's wall grows by the 2 s. |

Not moved: every service, freshness and round-trip check (about 100-170 ms each; item 3 of the brief was not needed). `F20`
stays fast (Ruling 77 (3)).

## Result (Verified)

Analysis in `tools/run-tests.sh`: 4,331 / 4,410 / 4,398 ms at end load 12.5 / 17.8 / 17.9 (three runs, `COST-MISS` 0, 0 failures); before 5,386 ms at load 13.
Alone, `dotnet run` of the harness: 3.9 s against 4.6 s. A run with the long jobs started together (no stagger, in the denser ring that item 2 produces) read
4.8-5.0 s; the stagger is what brings it back. A nice +5 on the long jobs did not help (4.8-5.0 s) and was dropped.

## PASS-name union (Verified, diffed)

`union-before.txt`: fast-ring Analysis PASS names plus `--readiness` PASS names, before: 157. `union-after.txt`: 159. `diff` shows only two added lines,
`Readiness_Camber4_N128Convergence` and `Readiness_Washin1_N128Convergence`. No name is dropped; every fast-ring name stays in the fast ring.

## No threshold loosened

C-2 5,000 ms, C-5 500 ms (1,500 ms for the two named exemptions) are unchanged. No assertion in F-6 changed. In F-18 and F-19 the n128 assertions
moved to readiness unchanged; the fast half adds a bound (+-10 deg) that is looser than the n128 band by design: it is the smoke the brief asked for, and the
n128 band still runs before every main move.

## For the test-architect lens

1. F-18 and F-19 keep their names in the fast ring with a smaller body; the full body now runs at readiness under a new name. A join no longer proves the
   n64-against-n128 convergence of the horseshoe law; it proves finite and bounded at n64 and within 0.1 deg of n32. Readiness proves the rest.
2. The 2 s stagger is a scheduling choice that shields one wall-clock measurement from startup contention. It does not change the work measured.
