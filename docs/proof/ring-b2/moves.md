---
id: proof-ring-b2-moves
title: "Ring B2 move list: Analysis checks moved from the join ring to readiness"
type: proof-pack
status: active
owner: "@trk-b2"
phase: implementation
tags: [ring, test-cost, c-2]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: plan-test-cost, rel: relates-to }
summary: "Five convergence and oracle checks move from the Analysis join ring to readiness so C-2 holds at quiet load; PASS union unchanged."
review-by: "2026-11-04"
---

# Ring B2, item 1: Analysis under C-2 at quiet load

Problem (Verified, join log `join-tip-salvage-s1.log`): at end load 13 the Analysis harness took 5,386 ms against C-2's
5,000 ms. C-2 is unchanged. After the move the same harness took 4,089 ms in `tools/run-tests.sh` at end load 13.45
(`COST-MISS` 0, `test costs: 0 failures`), and 3.7-4.0 s alone on a quiet machine (before: 4.6-4.9 s).

## Move list (fast ring "A" to readiness "R")

| Check | Cost measured (fast ring, ms) | Why it is convergence or oracle evidence | Still runs |
|---|---|---|---|
| `F6_ObservedOrder` | 487-517 | Observed order p on the 32/64/128 trio, plus the wake-per-panel mutant (a second trio). It also builds the shared trio F-2, F-5, F-15 read. | `--readiness`, as a named `Check` (it ran as a bare call before, so it printed no PASS there) |
| `F15_EllipticWing_InducedAngleUniform` | 13 | Richardson alpha_i against the independent reference. Reads F-6's trio, so it moves with F-6 (otherwise it rebuilds the trio). | `--readiness`, after F-6 |
| `F15b_EllipticMidspan_InducedAngleMatchesCLOverPiAR` | 34 | Same trio; analytic anchor CL/(pi AR). | `--readiness`, after F-6 |
| `F18_Camber4_DefaultLatticeTipConverges` | 224-236 | n64 vs n128 tip alpha_i and CL convergence, kappa1 bound, camber-surface horseshoe mutant. Its n256 half is already readiness. | `--readiness` |
| `F19_Washin1_DefaultLatticeTipConverges` | 225-233 | Same for washin. | `--readiness` |

Total moved: about 1.0 s of 4.1 s check time. Readiness gains about 0.9 s (`F6` 443, `F18` 187, `F19` 183, `F15` 12,
`F15b` 32 ms measured there).

Not moved, and why: `F20_EllipticStraightQuarterChord_SweepZero` (85 ms) is placed in the fast ring by Ruling 77 (3).
The 100-175 ms service and freshness checks assert behaviour (cancel, supersede, undo), not convergence; their cost is
mostly first-call JIT, which moving one only hands to the next check.

## PASS-name union (Verified, diffed)

`union-before.txt` = fast-ring PASS names (Analysis, before) + `--readiness` PASS names (before): 157 names.
`union-after.txt` = the same after: 157 names. `diff` printed nothing. The fast ring lost exactly the five names above
(149 to 144 PASS lines); readiness gained exactly those five (8 to 13).

## No threshold loosened

No constant changed. `tools/check-test-costs.py` is untouched in this commit: C-2 stays 5,000 ms, C-5 stays 500 ms
(1,500 ms for the two named exemptions). The F-6 exemption in C-5 now only guards a name that no longer prints in the join
ring; it is kept because the checker's self-test and the readiness log still name it.

## For the test-architect lens (clearance needed)

1. **Design A8.4 says "observed order is ring 0".** `F6_ObservedOrder` leaves ring 0 under this move. The design rows
   (section 13.2 F-6, F-15, F-18, F-19, and the 18.8 ledger "Ring" column) now say so and cite this file. If the lens
   rejects it, the fallback is to keep F-6 and move only F-18, F-19 (about 0.47 s, Analysis about 4.5 s at end load 13).
2. The mutants for F-18, F-19 and F-6 (camber-surface horseshoe, wake-per-panel) now run only before a main move. A
   join no longer catches a regression in those solver laws; the readiness ring does, at the same HEAD, before main.
3. `tools/check-named-tests.py` for the VLM track reads fast-ring logs; the track is closed and not in `join.json`.
   F-5 and F-2 were already readiness-only.
