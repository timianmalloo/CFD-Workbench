---
id: proof-spike-04
title: SPIKE-04 verdict — three-grid convergence oracle on NASA TMR NACA 0012 (OpenFOAM v2512)
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-04, openfoam, verification, gci, tmr, naca0012, spalart-allmaras]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: rulings, rel: depends-on}
  - {to: proof-spike-03, rel: relates-to}
review-by: 2026-11-03
summary: >-
  NO-GO; stopped at Stage 1. On TMR Family II levels 6 and 5 (SA, alpha 10, Re 6e6), simpleFoam never met the
  stated residual floor (1e-7). With linearUpwind, bound() clipped negative nuTilda in 39,994 of 40,000
  iterations and residuals froze at a fixed point. With TVD limitedLinear, a period-2 limiter cycle kept nuTilda
  at 4-5e-5. The repair cap (2) was reached, so the third grid and GCI were not run. v2512 has no SA-neg model.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# SPIKE-04 verdict — three-grid convergence oracle on NASA TMR NACA 0012

Lane F1, Ruling 60. Lenses: CFD numerical verification (author), hydrofoil hydrodynamicist (physical claims).
Backend, activation and hashes as in [SPIKE-03 § Backend](../spike-03/verdict.md#backend-step-0). Every number is
read from a file under [receipts/](receipts/) or [reference/](reference/), unless labelled **Estimate** or
**Inferred**.

## Verdict

**NO-GO — stopped at Stage 1.** Appendix R asks for "the three-grid convergence oracle on NASA TMR NACA 0012
with GCI". No grid met the stated residual floor, so every run is a **recorded gap**. The repair cap (2 cycles)
was reached on the coarsest grid's numerics. The third grid, the observed order, GCI_fine, the asymptotic-range
check and the Cl/Cd comparison with the TMR reference were **not computed**. Run and Results acceptance stays
gated.

## Case and sources

- **Case:** TMR "2D NACA 0012 Airfoil Validation for Turbulence Model Numerical Analysis" (2DN00):
  <https://tmbwg.github.io/turbmodels/naca0012numerics_val.html> (the old `turbmodels.larc.nasa.gov` URL now
  redirects to a NASA landing page that links this GitHub Pages mirror). M 0.15, Re_c 6e6, α 10°, SA, sharp-TE
  NACA 0012, far field about 500 c.
- **Grids:** Family II (finest trailing-edge spacing; TMR: "believed to yield the most accurate results"), levels
  6 / 5 / 4 = 225×65, 449×129, 897×257, refinement ratio 2. Fetched from the NASA zip
  <https://www.nasa.gov/wp-content/uploads/2026/02/naca0012numerics-grids.zip> (2.98 GB) by HTTP range requests
  ([`cases/tools/fetch-tmr-grid.py`](../../../cases/tools/fetch-tmr-grid.py)); the GitHub copies of the grid files
  return 404 (only the `.nmf` maps are in the repo). sha256: level 6 `56643365…aff3244`, level 5 `63b34d47…800d93`,
  level 4 `fb0e235c…c77057`.
- **Reference Cl / Cd:** FUN3D and CFL3D SA, no point vortex, per grid level:
  [reference/fun3d_results_sa_nopv_withN.dat](reference/fun3d_results_sa_nopv_withN.dat),
  [reference/cfl3d_results_sa_nopv_withN.dat](reference/cfl3d_results_sa_nopv_withN.dat), from
  <https://tmbwg.github.io/turbmodels/naca0012numerics_val_sa_withoutpv.html>. Grid-converged Family II band
  (three codes): **Cl 1.09085–1.09103, Cd 0.0122715–0.0122740**. Reference Cp: FUN3D
  <https://tmbwg.github.io/turbmodels/NACA0012numerics_val/fun3d_cp_sa_nopv.dat> (cited, not compared here).
- **Mesh conversion:** [`cases/tools/make-tmr-case.py`](../../../cases/tools/make-tmr-case.py) writes the
  polyMesh directly from PLOT3D and merges the C-grid wake cut into internal faces. The detected cut matches the
  TMR neutral map file: lower TE at i = 48 / 96 / 192 (0-based) and 129 / 257 / 513 airfoil points; checkMesh
  "Upper triangular ordering OK", one region.

## Deviations (stated before the runs)

1. **Incompressible `simpleFoam`** (the solver a hydrofoil case uses) instead of a compressible code at M 0.15
   with Sutherland's law and an adiabatic wall. *assume:* this lowers Cl by about 1.1 % (Prandtl–Glauert
   1/√(1−0.15²) = 1.0114, **Estimate**) and leaves Cd nearly unchanged.
2. **SA-noft2**: ESI's `SpalartAllmaras` header cites the TMR SA-noft2 form; v2512 has no SA-neg RAS model
   (source tree checked). FUN3D used SA-neg; CFL3D used SA.
3. Standard far field (`freestreamVelocity` / `freestreamPressure`), no point-vortex correction — compared with
   the matching TMR "no PV" tables.
4. `laplacian`/`snGrad` `limited corrected 0.5`: checkMesh finds 206 faces above 70° non-orthogonality on level 6,
   all on long trapezoidal far-wake cells at |y| < 1.6e-5, x 1.02–443 (none near the airfoil; the merged cut
   itself is 0°) — [receipt](receipts/20261003T172453Z-spike04-tmr-l6/nonortho-location.txt). Below 70° the
   limiter does not act, so the airfoil region is discretised exactly as `corrected`.

**Residual floor (stated before the runs, in each case file):** converged = `simpleFoam` `residualControl` stops
the run with the initial residuals of p, Ux, Uy and nuTilda all ≤ 1e-7. A run that reaches `max_iterations`
without that is a recorded gap, not a number.

## Stage 1 — the two coarsest grids

| Run (case file) | Grid · ranks | nuTilda advection | Iterations · wall | Stopped by floor? | Final initial residuals (Ux · Uy · p · nuTilda) | Status |
|---|---|---|---|---|---|---|
| [spike04-tmr-l6](../../../cases/spike04-tmr-l6.yaml) | L6 225×65 (14,336 cells) · 2 | `bounded Gauss linearUpwind` | 40,000 · 233 s | **no** | 8.2e-8 · 3.7e-7 · 5.4e-8 · **1.9e-4** | gap |
| [spike04-tmr-l6-diag](../../../cases/spike04-tmr-l6-diag.yaml) | L6 · 2 | same, residual fields written | 2,000 · 19 s | — | — | diagnostic |
| [spike04-tmr-l6-tvd](../../../cases/spike04-tmr-l6-tvd.yaml) | L6 · 2 | `bounded Gauss limitedLinear 1` | 40,000 · 258 s | **no** | 9.6e-8 · 4.6e-7 · 7.1e-8 · **5.4e-5** | gap |
| [spike04-tmr-l5-tvd](../../../cases/spike04-tmr-l5-tvd.yaml) | L5 449×129 (57,344 cells) · 4 | `bounded Gauss limitedLinear 1` | 60,000 · 660 s | **no** | 1.5e-8 · 1.6e-7 · 2.4e-8 · **4.3e-5** | gap |

A first level-6 run directory (`…172453Z…`) holds only `checkMesh`; it is marked `VOID.txt` because the
laplacian scheme was changed after that check and before any solve.

Level 5 has max non-orthogonality 57.9°, so the `limited 0.5` scheme never acts there. It also never clips
(0 "bounding" lines). Its stall is the `limitedLinear` limiter alone: a period-2 cycle with Cd 0.0120347 ↔
0.0120334.

### Root cause of the stall (measured)

- **Run 1 (`linearUpwind` for nuTilda):** `bound()` printed "bounding nuTilda" in **39,994 of 40,000**
  iterations (min −6.1e-6 = 12 × the free-stream ν̃) — [sample](receipts/20261003T172635Z-spike04-tmr-l6/bounding.sample.txt).
  The solution froze (Cl constant to 1e-11 from about 14,000 iterations) while each iteration's clip restored the
  same state, so the nuTilda residual stays at 1.91e-4 at a fixed point. A 2,000-iteration diagnostic writing
  residual fields puts **92 % of the nuTilda residual in 100 cells, 1–100 chords downstream** along the wake, and
  98 % of the Uy residual within one chord, at the leading edge —
  [receipt](receipts/20261003T173120Z-spike04-tmr-l6-diag/residual-location.txt).
- **Run 2 (repair 2 of 2: `bounded Gauss limitedLinear 1`, TVD):** clipping falls to 8,019 iterations
  (min −2.8e-8), but the limiter chatters: a **period-2 limit cycle**, Cd alternating 0.015106 ↔ 0.015129 and
  Cl 1.055233 ↔ 1.055216, residual nuTilda 5.4e-5.
- The repair cap (2) is reached, so SPIKE-04 stops at Stage 1. Stage 2 (third grid, GCI) was **not run**: GCI
  over gaps would be a number the rule forbids.

### Sizing the iterative band (for ADR decision 2; these are gaps, not results)

The last-iterate forces are reported only to size the iterative band against the grid-to-grid change. They are
**not** admitted as Cl/Cd values, and no accuracy claim against TMR is made from them.

| Run | Cl last iterate · band over last 1,000 | Cd last iterate · band over last 1,000 |
|---|---|---|
| L6 `linearUpwind` | 1.056940 · 1.5e-5 (constant to 1e-11 from 14k iterations on) | 0.0150908 · 1.2e-6 |
| L6 TVD | 1.055216 · 1.7e-5 (period-2) | 0.0151288 · 2.3e-5 (period-2) |
| L5 TVD | 1.079626 · 2.0e-6 (period-2) | 0.0120347 · 1.4e-6 (period-2) |

The L6→L5 change (TVD) is ΔCl = +0.0244 and ΔCd = −0.0031. The iterative bands are 2–4 orders of magnitude smaller. So a
stated QoI-stationarity criterion would likely admit these runs (**Inferred**), but that is the operator's or
the ADR's call before the next run, not a re-labelling of these.

The y+ on the wall (yPlus function object): L6 avg 0.75, max 3.0; L5 avg 0.28, max 1.01. The wall-resolved
branch (y+ ≤ 1) holds on L5 (max 1.01) but not everywhere on L6 (max 3.0).

## Durations (the new prior)

| Stage | Start (UTC) | End (UTC) | Duration |
|---|---|---|---|
| 1 — sources, grid fetch, converter, L6 × 2 + diagnostic, L5 | 17:17 (TMR pages read during SPIKE-03 waits) / 17:22 | 17:49 | 27-32 min |
| 2 — third grid (L4), GCI | not run (repair cap; inputs are gaps) | — | — |

Measured solve cost: L6 (14k cells, 2 ranks) 5.8-6.2 ms per iteration; L5 (57k cells, 4 ranks) 10.8 ms per
iteration. **Estimate** for L4 (229k cells, 6 ranks) by cells per rank: about 30 ms per iteration, so 80,000
iterations is about 40 min. Peak 1-minute host load across both spikes: 57.66 (SPIKE-03 AR 12). Peak RSS: Not
recorded.

## What the backend ADR must decide

1. **SA variant.** v2512 has no SA-neg. Either accept a residual criterion that SA + `bound()` can meet on TMR
   grids, or carry an SA-neg implementation (a coded model is barred by the A8.5 rule; it would have to be a
   compiled, pinned library), or use another backend (SU2 has SA-neg) for the oracle.
2. **The convergence criterion itself.** A5.10's "Converged" needs an iterative part. Run 1 shows a solution
   stationary to 1e-11 in Cl with residuals frozen above any floor. The ADR must say whether the iterative part is
   judged on residuals (as stated here — fails) or on quantity-of-interest stationarity with a stated band. That
   change must be made before the next run, not after this one.
3. **Compressible vs incompressible oracle.** TMR recommends M 0.15 in a compressible code. An incompressible
   oracle needs its own reference (or a stated compressibility correction with its uncertainty).
4. **Substrate, pin, resource limits:** as SPIKE-03 — native app by DMG hash on macOS; Windows not exercised;
   2–6 ranks, nice 10, load wait and join lock; peak RSS not recorded.

## Security

Nothing new beyond SPIKE-03: every run here printed `allowSystemOperations : Allowing user-supplied system call
operations`. The generated TMR dictionaries use no `#calc`, `#codeStream`, `#include`/`#includeEtc` or coded
objects.
