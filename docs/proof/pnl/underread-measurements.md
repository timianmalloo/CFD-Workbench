---
id: proof-pnl-underread-measurements
title: "PNL: measured 200-vs-400 under-read by thickness (Ruling 110 (6))"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, panel-method, measurement]
links:
  - { to: proof-pnl-step0-other-stations, rel: relates-to }
  - { to: proof-pnl-red-first, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Measured two-grid (p assumed 1) 200-vs-400 suction under-read for 1-12 % thick sections at alpha 3, 6, 10 deg.
  The 6 %-thick value is 3.60 % at alpha 3 and 4.39 % at alpha 6; thinner sections exceed 10 %.
review-suggested: []
---

# Measured 200-vs-400 under-read (two-grid, p assumed 1)

Track `trk-pnl`, 2026-10-06, macOS arm64, Release. Quantity: (S400 - S200)/S400 where S is the suction peak -Cp_min at
the same alpha_eff, from `SectionTier.UnderreadAt` (the NACA-fitted default wing, thickness channel replaced by a
linear taper from 12 % at the root to the tip t/c; section at the quoted eta; Re 5e5, which Cp does not use). Values are
measured, not fitted.

## The Ruling 110 (6) run: 6 %-thick

| t/c | alpha | under-read | Ruling expectation |
|---|---|---|---|
| 6.0 % (uniform) | 3 deg | **3.598 %** | about 3.5-3.9 %: inside |
| 6.0 % (uniform) | 6 deg | **4.385 %** | about 3.5-3.9 %: **above** by 0.5 point |

The alpha 6 value is above the expected band. It is reported, not tuned. Ruling 110 (6) says a larger delta reopens the
2x width; 4.4 % is still 2.3x below the 10 % gate. The expectation was Inferred (first order, delta about E200/2, from the
reviewer's 6.9-7.7 % exact-error figure); the measurement shows the delta is 0.47x (alpha 3) to 0.64x (alpha 6) of that
figure, so the first-order halving holds at alpha 3 and is slightly optimistic at alpha 6.

## Thickness sweep (tapered wing, quoted station)

| tip t/c | station t/c | alpha 3 | alpha 6 | alpha 10 |
|---|---|---|---|---|
| 0.06 | 9.0 % (eta 0.5) | 1.97 % | 3.27 % | 3.47 % |
| 0.06 | 6.6 % (eta 0.9) | 3.44 % | 3.99 % | 5.16 % |
| 0.06 | 6.0 % (eta 1) | 3.60 % | 4.38 % | 5.95 % |
| 0.03 | 7.5 % (eta 0.5) | 3.33 % | 3.72 % | 3.71 % |
| 0.03 | 3.9 % (eta 0.9) | 7.41 % | 7.56 % | 7.85 % |
| 0.03 | 3.0 % (eta 1) | 12.97 % | 10.52 % | 12.67 % |
| 0.02 | 7.0 % (eta 0.5) | 2.87 % | 4.57 % | 4.46 % |
| 0.02 | 3.0 % (eta 0.9) | 12.97 % | 10.52 % | 12.67 % |
| 0.02 | 2.0 % (eta 1) | 16.51 % | 20.56 % | 22.34 % |
| 0.01 | 6.5 % (eta 0.5) | 3.79 % | 3.84 % | 5.59 % |
| 0.01 | 2.1 % (eta 0.9) | 15.05 % | 19.23 % | 21.05 % |
| 0.01 | 1.0 % (eta 1) | 38.39 % | 41.15 % | 42.41 % |

Reading: the delta is 2-5 % for sections 6 % and thicker, passes 7 % at 4 % thick and passes the 10 % gate at 3 % thick
and below. Because the under-read grows as the section thins, a thin station that is near-tied at 200 panels usually
becomes the governing station at 400; that is why the near-tie width (2x the governing under-read) re-selects it (see the
near-tie check in `red-first.md`). A thin station outside the width stays "not measured" in the run and is read on demand
through `SectionTier.UnderreadAt`.
