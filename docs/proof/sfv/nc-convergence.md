---
id: proof-sfv-nc-convergence
title: SFV chordwise-convergence measurement of the strip Cm c/4 and x_cp (nc 2, 4, 8, 16)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sfv, lattice, centre-of-pressure, ruling-131, convergence]
links:
  - { to: proof-sfv-model, rel: refines }
review-by: 2026-12-31
summary: >-
  Measured bias of the 4-chordwise-panel lattice strip pitching moment and centre of pressure on a cambered section: at nc = 4, x_cp is
  0.02 to 0.04 of a chord forward of its converged value and the Cm c/4 magnitude is 13 to 21 percent low; observed order near 1. A measurement
  for the next ruling on nc, not a gate.
---

# SFV: how far the 4-panel strip is from its chordwise limit (Ruling 131)

**What this is.** Ruling 131 approved the label "lattice, 4 chordwise panels; biased forward at low lift" and asked for the bias to be measured.
This file is the measurement. It is a record for the next ruling on nc; nothing gates on it. Changing nc is not in this track's scope.

**How it was made.** The check `SectionForce_ChordwiseConvergence_MeasuredNotGated_Ruling131`
(`tests/CfdWorkbench.Analysis.Tests/SectionForceTests.cs`, readiness ring, about 7 s wall for 12 lattice runs) solves the product wing with the
cambered NACA 2412 shape at the default cosine laws, 32 strips per half, `NChord` = 2, 4, 8, 16 (the section placements use the same
count), at alpha = -1, 1 and 4 degrees. The strip at eta = 0.5 is read through the same code the Section view uses
(`SectionDisplay.Build`, `SectionForceModel.Compute`). Cm c/4 = M' c/4 / (q c^2), with M' c/4 the strip couple about the quarter chord
(nose-up positive) and q at the run's speed; x_cp/c is the strip's own moment over its normal force at the strip leading edge. All numbers
below are printed by that check (`MEASURE SFV` lines); they were observed, not modelled.

## The table (32 strips per half, eta 0.5)

| nc | Cl_local (lattice) | Cm c/4 | x_cp/c |
|---|---|---|---|
| **alpha = -1 deg** | | | |
| 2 | 0.1003 | -0.03291 | 0.5782 |
| 4 | 0.0993 | -0.04448 | 0.6979 |
| 8 | 0.0981 | -0.04955 | 0.7553 |
| 16 | 0.1018 | -0.05049 | 0.7461 |
| **alpha = 1 deg** | | | |
| 2 | 0.2662 | -0.03224 | 0.3711 |
| 4 | 0.2657 | -0.04395 | 0.4154 |
| 8 | 0.2650 | -0.04938 | 0.4364 |
| 16 | 0.2697 | -0.05102 | 0.4392 |
| **alpha = 4 deg** | | | |
| 2 | 0.5144 | -0.03109 | 0.3106 |
| 4 | 0.5145 | -0.04295 | 0.3337 |
| 8 | 0.5148 | -0.04891 | 0.3452 |
| 16 | 0.5210 | -0.05156 | 0.3492 |

## Observed order

Order from three successive doublings, p = log2(|C(nc/2) - C(nc/4)| / |C(nc) - C(nc/2)|), as the check prints it:

| alpha | Cm c/4, nc 2-4-8 | Cm c/4, nc 4-8-16 | x_cp, nc 2-4-8 | x_cp, nc 4-8-16 |
|---|---|---|---|---|
| -1 | 1.19 | 2.43 | 1.06 | 2.65 |
| 1 | 1.11 | 1.74 | 1.08 | 2.90 |
| 4 | 0.99 | 1.16 | 1.00 | 1.53 |

Reading: from nc 2 to 8 the order is about 1 for both quantities at all three angles (0.99 to 1.19). The 4-8-16 order is higher and scattered (1.2 to
2.9) because the nc = 16 point is not on the same curve: Cl_local itself moves at nc = 16 (0.0981 to 0.1018, 0.2650 to 0.2697, 0.5148 to 0.5210), which
points to the panel aspect ratio (16 chordwise panels of a strip 1/32 of the half span wide) and not to the chordwise limit. So the order is
Verified near 1 for nc 2 to 8, and Inferred only for the limit beyond nc = 8.

**Coarse spanwise count.** At 8 or 16 strips per half the same study is not in its asymptotic range: Cl_local rises with nc (alpha = -1:
0.1157, 0.1325, 0.1648 for nc 4, 8, 16 at 8 strips) and Cm c/4 turns back at nc = 16 (-0.0448, -0.0493, -0.0425). The default product
lattice has 64 strips per half; the measurement above is at 32, so the nc study at the default span is a next step if nc is raised.

## The bias at nc = 4 (the 4-panel strip against the first-order limit)

Limit = C(16) + (C(16) - C(8)) for p = 1 (the order observed at nc 2-4-8). It is an estimate (Inferred), good to the size of the nc 8 to 16 step.

| alpha | Cl_local | x_cp/c at nc 4 | limit | nc 4 minus limit | Cm c/4 at nc 4 | limit | nc 4 relative to limit |
|---|---|---|---|---|---|---|---|
| -1 | 0.099 | 0.698 | 0.737 | -0.039 (forward) | -0.0445 | -0.0514 | magnitude 13.5 % low |
| 1 | 0.266 | 0.415 | 0.442 | -0.027 (forward) | -0.0440 | -0.0527 | magnitude 16.5 % low |
| 4 | 0.514 | 0.334 | 0.353 | -0.020 (forward) | -0.0430 | -0.0542 | magnitude 20.8 % low |

- **x_cp is biased forward at every lift tested**, by 0.02 to 0.04 of a chord, and the offset is largest at the lowest lift (0.039 at Cl 0.10
  against 0.020 at Cl 0.51). This matches the approved wording "biased forward at low lift".
- **The Cm c/4 magnitude is low by 13 to 21 percent at nc = 4**, and the shortfall grows with lift in relative terms.
- x_cp = 0.25 - Cm/Cl, so at Cl near 0.1 a Cm error of 0.0065 moves x_cp by 0.065 before the second-order terms: the quotient is the quantity most
  exposed to the bias, which is why the anchor rule already refuses x_cp below |Cl_local| 0.05 (Ruling 130).

## What this settles and what it leaves

- The label "lattice, 4 chordwise panels; biased forward at low lift" is true as measured: forward, and larger at low lift.
- It does not say the Section view's anchor is wrong at the default: 0.02 to 0.04 chord is a small shift of an arrow root. It says the **number**
  x_cp/c and the couple M' c/4 carry a first-order discretisation error of that size at nc = 4.
- The next ruling is whether to raise nc (cost: unknowns grow with nc; the cap is 2048 unknowns, so nc = 8 at 64 strips per half is 1024) or to keep
  nc = 4 with the label. This file gives the size of the gain from each step: nc 4 to 8 recovers most of the difference to the limit; nc 8 to 16 adds less than
  0.01 of a chord in x_cp at all three angles.
