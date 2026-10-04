---
id: proof-a3a-vlm-red-first
title: "A3a VLM red-first receipt"
type: proof-pack
status: active
owner: "@track-vlm"
phase: implementation
tags: [a3a, vlm, analysis, red-first, fixtures]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: note-area3-fixture-arithmetic, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The red run of the A3a VLM track. Planting the O(1) wake mutant (wake length per panel instead of per wing)
  turned F-6 red. The mutant was removed before the commit. The 2026-10-04 repair adds the red lines for the LU pivot
  defect (one-solve residual 1.98 on a 12 x 12 matrix), the convergence form of F-5 and the pointwise form of F-15.
---

# A3a VLM red-first receipt

Design: `docs/design/area3-analysis.md` §13.2 F-6 and §18.2 (VLM). Session `track-vlm`, 2026-10-04.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| `F6_ObservedOrder` | wake length = `WakeSpans * span / nStrips` for every lattice, not only the planted `LatticePlant.WakePerPanel` path | `FAIL F6_ObservedOrder InvalidOperationException: p(CL) -0.63208432840675044 outside [0.8, 1.2]` | mutant removed, `PASS` (p(CL) = 1.0696906502361061) |

The same check, on the unplanted lattice, also builds the wake-per-panel trio and fails the run if that order stays inside 1 ± 0.2. F-2 fails its run if the mid-panel control-point mutant's Richardson CL stays inside [0.4156, 0.4198].

## Repair (review 2026-10-04, session `vlm-repair`)

The CFD review blocked the track on F-4 (a solver defect), F-5 and F-15 (weakened assertions). Each row below was
observed red, then green. The mutant was removed, or left only behind its `LatticePlant`, before the commit.

| Test | Mutant or input | Red line | Then |
|---|---|---|---|
| `Vlm_PivotingSolve_ResidualAfterOneSolve` (new) | the base solver: `Factor` swaps whole rows, one solve, no refinement | `FAIL Vlm_PivotingSolve_ResidualAfterOneSolve LatticeFailedException: The solve residual ‖AΓ − b‖∞ 1.98 is a backward error of 0.0468, above the 1E-10 tolerance.` | trailing-column swap: `PASS`, residual 2.44 × 10⁻¹⁵ after one solve, 8 interchanges; `LatticePlant.PivotWholeRow` stays as the in-test mutant (→ `ANA-SOLVE-RESIDUAL`) |
| `F4_MirroredWing_NoSideForceRollYaw` | the same base solver, refinement removed | `FAIL F4_MirroredWing_NoSideForceRollYaw LatticeFailedException: The solve residual ‖AΓ − b‖∞ 0.524 is a backward error of 0.108, above the 1E-10 tolerance.` | `PASS`, residual 2.78 × 10⁻¹⁶, κ₁ 18.97 |
| `F6_ObservedOrder` | the same base solver | `FAIL F6_ObservedOrder AggregateException: … residual ‖AΓ − b‖∞ 1.32 … 3.91 … 5.4 …` | `PASS`, p(CL) 1.0697, p(e) 1.0127 (unchanged) |
| `F5_InducedDrag_TrefftzWithin1PercentOfNearField` (rewritten) | the base 3-point Gauss rule, on F-6's uniform-chord trio | `FAIL … near-field/Trefftz gap does not shrink 32 → 64 → 128: 1.0186326371688494 1.0030243749944463 0.99514982450142242` | midpoint rule (design §5.2) on the default cosine chord: `PASS`, 0.99082, 0.99104, 0.99107 |
| `F5_…` | trailing legs omitted from the near-field induced velocity, planted in the product path | `FAIL … near-field/Trefftz 0.021679393713306502 0.025650575018400273 0.029045374018974827, Richardson 0.049040193714064512: outside 1 %` | mutant removed; `LatticePlant.NearFieldTrailingOmitted` stays as the in-test mutant |
| `F15_EllipticWing_InducedAngleUniform` (rewritten) | w_T assigned to the neighbouring strip, planted in the product path | `FAIL … 32 per half: α_i ratio 0.92000075800558578 vs reference 0.93735000000000002 at η 0.80000000000000004` | mutant removed; `LatticePlant.DownwashNeighbour` stays as the in-test mutant |
| `F15_…` | α_i from the total control-point velocity, planted in the product path | `FAIL … Richardson α_i ratio 5.251313855015586 vs reference 1.0274300000000001 at η 0: 5.2143660520845616 5.2336150322199382 5.2428357236112229` | mutant removed; `PASS` |

F-7 was re-checked after F-15 and still `PASS`. Its 0.9-half strip stays, because the outermost strip's α_i diverges
with refinement whichever w_T evaluation point is used (note §Repair).
