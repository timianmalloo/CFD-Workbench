---
id: proof-vlm-tip-s2-verdict
title: "S2 small-tip-chord VLM sweep: verdict"
type: proof-pack
status: verified
owner: "@trk-s2"
phase: implementation
tags: [analysis, vlm, tip-strip, taper, ruling-78, ruling-88]
links:
  - { to: proof-vlm-tip-s2-preregistration, rel: implements }
  - { to: plan-tip-handling, rel: implements }
review-by: "2027-04-05"
summary: "One judged-strip flip in 15 (ratio, alpha) pairs: r = 0.01 at alpha 8, coarse lattice only, in the conservative direction."
---

# S2 verdict

Rules applied verbatim from `preregistration.md` (committed before the solve, 106b588). Source: `sweep.csv` (72 cases, 0 failures,
29.5 s total solve, 10.5 s wall), reduced by `apply-rules.py` into `rules-output.csv`. Probe built against this tree's `src` (main,
production VLM, no eta* law), Release, .NET 10.0.203. All numbers below were printed by the probe. Confidence is Verified unless marked.

## Result first

- **One judged-strip flip across 15 pairs: r = 0.01, alpha 8 deg.** At n = 32 the k3 strip reads out (Cl_local 1.02 above the Cl_local limit; alpha_eff 9.35 deg is under 10). At n = 64, `any_judged_out` is still 1 (a strip other than k1, k2, k3 reads out; which one is Inferred as k4, the probe did not record it). At n = 128 and 256 every judged strip reads in. Verified.
- **Direction: coarse n reads out, fine n reads in.** That is a false-out at coarse n, the conservative direction. No case reads in at a coarse n and out at a fine n. Verified.
- **The product default is n = 64** (`Labels.DefaultLattice`, Verified). At n = 64 the k2 and k3 verdicts are stable at every ratio. Only `any_judged_out` differs from n = 128 at r = 0.01, alpha 8.
- **No flip at r >= 0.02, and none at the r = 0.25 and r = 1.0 control.** Rule 3 does not fire. The control matches the held branch: rect alpha 5 k1 alpha_eff 3.077 / 3.066 / 3.061 / 3.059 deg at n = 32..256.
- **D2 (r = 0.01 and 0.02 at alpha 18): no flip.** k1, k2, k3 and `any_judged_out` read out at all four n. The alpha-18 falsifier survives at small chord. Verified.
- Applying rule 2: flips only at r <= r_flip = 0.01. The fallback band is the measured consistency edge, below.

## Per ratio

| r | flip (any alpha) | where | consistency edge, n = 32/64/128/256 (tip chords from the tip) | edge eta at n = 256 |
|---|---|---|---|---|
| 0.01 | FLIP | alpha 8: k3 out at n = 32, any_judged_out at n = 32, 64 | 0.48 / 0.48 / 0.48 / 0.61 (alpha 5, 8, 18 same) | 0.99848 |
| 0.02 | no | | 0.24 / 0.54 / 0.54 / 0.54 | 0.99729 |
| 0.05 | no | | 0.39 / 0.60 / 0.60 / 0.60 | 0.99248 |
| 0.1 | no | | 0.43 / 0.59 / 0.59 / 0.63 | 0.98421 |
| 0.25 | no | | 0.69 / 0.69 / 0.69 / 0.69 | 0.95694 |
| 1.0 (control) | no | | 0.91 / 0.85 / 0.88 / 0.89 (alpha 5, 8) | 0.77689 |

The edge is the inboard edge of the first strip, walking from the root, with C/C_root < 0.9. Every case has an edge (none = 0 cases).
The edge sits within about one tip chord of the tip for every ratio, so its absolute width shrinks with r.
Edge values depend on alpha only through alpha_eff and on n through the strip layout. The 0.9 crossing is a coarse reading: one cosine strip
at n = 32 spans more than a tip chord for r <= 0.05.

## Alpha_eff at k1..k3 (alpha 8, the flip alpha), by n = 32 / 64 / 128 / 256

| r | k1 | k2 | k3 |
|---|---|---|---|
| 0.01 | 8.17 / 7.46 / 7.15 / 7.04 | 9.06 / 7.96 / 7.32 / 7.08 | 9.35 / 8.55 / 7.59 / 7.16 |
| 0.02 | 7.42 / 6.96 / 6.79 / 6.72 | 8.13 / 7.25 / 6.87 / 6.74 | 8.68 / 7.67 / 7.01 / 6.78 |
| 0.05 | 6.66 / 6.43 / 6.34 / 6.31 | 7.05 / 6.55 / 6.37 / 6.32 | 7.54 / 6.76 / 6.43 / 6.33 |

Full k1-k3 alpha_eff, Cl_local and verdicts for every case are in `sweep.csv`. Alpha_eff at the tip falls monotonically with n at every ratio and alpha, so alpha_eff at the tip converges downward (no divergence). Verified for the grid, Inferred beyond it.

## S3 implication (Ruling 78)

- **Recommendation: keep Ruling 78 and add a written scope with a tip-chord-ratio floor at r = 0.02 (the measured r_flip is 0.01, so refuse or flag the lattice below it).** The only flip is at r = 0.01, coarse n, in the conservative direction. At the default n = 64 no k2 or k3 verdict flips at any ratio. The ceiling for the unchanged law is therefore r >= 0.02 on this grid.
- This is a floor on the planform, not a band width and not a correction law. Nothing here was fitted.
- If the operator prefers not to add a floor: the cost of doing nothing at r = 0.01 is extra false-out strips at coarse n, never a false-in. That is Inferred from one flip case; the grid does not span r < 0.01.
- Judge k1 on finite tips: **not supported** by this sweep. k1 alpha_eff changes between n = 32 and 256 by 0.02 deg (r = 1.0, alpha 5) up to 2.5 deg (r = 0.01, alpha 18), and for r <= 0.05 k1 lies outboard of the consistency edge, so its C/C_root is below 0.9 (rule 5 fails). Verified for the grid.
- The consistency edge at r = 1.0 is 0.85 to 0.91 tip chords, in line with the held branch's rect measurement. It is a strip-theory measure, not a flow measurement.

## Limits

- One run set, flat plate, zero sweep, no twist, alpha_L0 = 0. Taper changes the root-to-tip chord, so Re_local and Cl_max are not in play here. Camber and twist at the tip are out of scope (the held study measured them separately).
- The `any_judged_out` flip at n = 64 names no strip. A single diagnostic print would identify it. It was not run, to hold the one-run-set cap.
- The envelope uses alpha_L0 = 0. A cambered section shifts the alpha_eff reading by alpha_L0.
- The optional second arm (AeroSandbox VLM / lifting line) was not run.
