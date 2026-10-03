---
id: proof-spike-04-round2
title: SPIKE-04 round 2 verdict — A4 convergence and three-grid study on NASA TMR NACA 0012 (OpenFOAM v2512)
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-04, openfoam, verification, gci, tmr, naca0012, spalart-allmaras, round-2, a4]
links:
  - {to: proof-spike-04, rel: supersedes}
  - {to: plan-fluids-round2, rel: implements}
  - {to: rulings, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: proof-spike-03-round2, rel: relates-to}
review-by: 2026-11-03
summary: >-
  NO-GO on the GCI clause, GO on the iterative oracle. After two numerics repairs on level 6 (first-order upwind
  nuTilda, then relaxation 0.7), OpenFOAM v2512 simpleFoam SA-noft2 meets the A4 criterion on TMR Family II levels
  6, 5 and 4 and rhoSimpleFoam meets it on level 5. The three-grid sequence is oscillatory for Cl and Cd (R = -0.010
  and -0.053), so there is no observed order and no GCI. The measured compressible-minus-incompressible delta at TMR
  conditions on level 5 (one grid, alpha 10, Re 6e6, M 0.15) is +1.147 % in Cl and +0.81 % in Cd, U_Delta not
  stated. Fully turbulent air, 2-D. macOS arm64 only.
review-suggested: []
---

# SPIKE-04 round 2 verdict — A4 convergence and the three-grid study

Lane F1, round 2. Case: TMR 2DN00, NACA 0012, α 10°, Re 6e6, **fully turbulent (no trip, no transition model), as
the TMR case**, SA-noft2 (ESI `SpalartAllmaras`, Ŝ limiter Cs 0.3). Authority: [plan](../../plans/fluids-round2.md) §2 and §5, [Ruling 65](../../notes/rulings.md)
(DR-F2-1 A4 adopted; DR-F2-2 = (c) no SU2, compressibility by `rhoSimpleFoam`; DR-F2-7 macOS only; DR-F2-9 TMR + GCI
is a pin-change fixture). Round 1: [verdict.md](verdict.md) (kept unchanged). Lead lens: CFD numerical verification.
Every number is read from a file under [receipts/](receipts/) (`*-spike04r2-*` directories, copied from the
git-ignored `runs/`) or [reference/](reference/), unless it is labelled **Estimate** or **Inferred**.

## Verdict

| Plan criterion (§2.4 acceptance) | Result | Evidence |
|---|---|---|
| Three grids admitted under A4 | **A4 met on L6, L5, L4** (L6 after repair 2 of 2) | C-1r2, C-2, C-3 `convergence.txt` |
| A4 clause 5 (U_I ≤ 0.01 \|ε\| of the neighbouring pair) | Cd: both pairs yes. Cl: L6/L5 yes, **L5/L4 no** (U_I 9.8e-6 against 2.8e-6) | `cases/tools/gci.py` output below |
| Observed order and GCI on Cl and Cd, monotonic (0 < R < 1) | **not met** — oscillatory: R = −0.0105 (Cl), −0.0526 (Cd) | below |
| \|S_ext + Δcomp − D_TMR\| ≤ √(U_SN² + U_Δ² + U_D²) | **not computable** — no S_ext, no U_SN | — |

**SPIKE-04: NO-GO on the GCI clause; the iterative oracle (A4) is a GO.** Per the plan, a failed acceptance is a
recorded finding, not a retune. The backend ADR can adopt A4 and the round-2 numerics; it cannot claim a grid
uncertainty for OpenFOAM on this case.

## Criterion and numerics (stated before each run, copied into each case file)

- **A4** (DR-F2-1): (1) every equation's initial residual, max over the last 2 iterations, ≤ 1e-3 × its iteration-1
  value; (2) over the last W = 2,000 iterations, U_I = ½(max − min) ≤ 1e-5 for Cl and ≤ 1e-6 for Cd; (3) no
  "bounding nuTilda" line inside W; (4) [`a4-monitor.py`](../../../cases/tools/a4-monitor.py) checks at each
  1,000-iteration multiple (polled every 5 s) and stops the run with SIGUSR1. The bundled controlDict sets
  `stopAtWriteNowSignal 30`, so the run writes and stops; `runTimeModifiable` stays false. The reported value is the
  window mean.
- **Stop mechanism, observed:** every A4 stop wrote within 4 iterations of the signal (C-1r2 6371 → 6375; C-2 17038
  → 17042; C-3 41052 → 41055; C-4 15168 → 15172). The plan's `assume:` (runtime `stopAt` edit) was not needed.
- **Launcher:** every process ran through [`of-run.sh`](../../../cases/tools/of-run.sh) and printed
  `allowSystemOperations : Disallowing user-supplied system call operations` (every ledger, `banner=Disallowing`).
  C-1 to C-3's solver ran on launcher v1 (env allow-list, bundle, lint, pre-flight); C-3's reconstructPar onward and
  C-4 ran on v2 (security review cycle 1 applied). v2 refused C-3's reconstructPar because C-3 had no launcher record;
  the record was initialised after the solve (trust on first use, labelled here), a v2 checkMesh pre-flight ran, then
  reconstructPar ([receipt](receipts/20261003T205313Z-spike04r2-c3-l4/log.record-reconstructPar)).

## Runs

| Id (case file) | Grid · ranks | nuTilda advection · relaxation (U, ν̃) | Iterations · solver wall | A4 | Cl (window mean · U_I) | Cd (window mean · U_I) |
|---|---|---|---|---|---|---|
| C-1 ([c1-l6](../../../cases/spike04r2-c1-l6.yaml)) | L6 225×65 · 2 | limitedLinear 1 (B1) · 0.9 | 40,000 (cap) · 238 s | **no**: clauses 2, 3 | 1.055223 · 2.3e-5 | 0.0151177 · 3.0e-6 |
| C-1 repair 1 ([c1r1-l6](../../../cases/spike04r2-c1r1-l6.yaml)) | L6 · 2 | upwind (B2) · 0.9 | 40,000 (cap) · 249 s | **no**: clause 2 (Cd) | 1.0515837 · 3.5e-7 | 0.0147946 · 1.4e-5 |
| C-1 repair 2 ([c1r2-l6](../../../cases/spike04r2-c1r2-l6.yaml)) | L6 · 2 | upwind (B2) · 0.7 | 6,375 · 50 s | **met** | 1.051704 · 6.6e-6 | 0.0148088 · 7.1e-7 |
| C-2 ([c2-l5](../../../cases/spike04r2-c2-l5.yaml)) | L5 449×129 · 4 | upwind · 0.7 | 17,042 · 317 s | **met** | 1.078072 · 6.5e-6 | 0.0119032 · 9.8e-7 |
| C-3 ([c3-l4](../../../cases/spike04r2-c3-l4.yaml)) | L4 897×257 (229,376 cells) · 6 | upwind · 0.7 | 41,055 · 3,701 s | **met** | 1.077796 · 9.8e-6 | 0.0120560 · 5.6e-7 |
| C-4 ([c4-l5-comp](../../../cases/spike04r2-c4-l5-comp.yaml)) | L5 · 6 · `rhoSimpleFoam` M 0.15 | upwind · 0.7 (e 0.7) | 15,172 · 378 s | **met** | 1.090442 · 9.6e-6 | 0.0119993 · 7.7e-7 |

Clause 1 (three orders) held in every run; at the four A4 stops the drop was ≥ 7.9 orders on every equation. The numerics after
repair 2 (B2, relaxation 0.7, SIMPLEC p 1) are identical on all three grids. C-4 uses the same schemes and
relaxation but a different application (`rhoSimpleFoam`: energy equation, ρ relaxation 1, pMin/pMaxFactor 0.5/1.5, Prt 1.0 (v2512 default; TMR 0.90),
Sutherland μ(T), adiabatic wall).

### What the two repairs showed (measured)

- **C-1 (B1, TVD):** the limiter clipped nuTilda in **8,000 of 40,000** iterations (every fifth; 400 inside the final
  window) and Cl/Cd chattered (U_I 2.3e-5 / 3.0e-6). Round 1's L6 TVD run is reproduced: Cl 1.055223 vs 1.055216–1.055233.
- **Repair 1 (B2 upwind):** clipping stopped (0 lines), residuals fell below 1e-6 of their start, but the solution
  locked into an **exact period-2 cycle**: Cd 0.0147804 ↔ 0.0148088, Cl moving 7e-7.
- **Repair 2 (relaxation 0.9 → 0.7):** A4 met at 6,371 iterations. The steady answer is **Cl 1.051704**, not the
  period-2 mean 1.051584: **the cycle mean is off by 1.2e-4 in Cl and 1.4e-5 in Cd** while every residual had
  dropped 6+ orders. So an ITTC residual-drop-only criterion (plan option A2) would have admitted a wrong number; the
  A4 stationarity clause is what rejected it (**Verified**, both receipts). The round-1 period-2 means are therefore
  not steady values either (**Inferred**).

## Grid study (ASME V&V 20 / Celik et al., r = 2, Fs 1.25; [`gci.py`](../../../cases/tools/gci.py))

| QoI | S1 (L4, fine) | S2 (L5) | S3 (L6) | ε21 | ε32 | R | Result |
|---|---|---|---|---|---|---|---|
| Cl | 1.077795851 | 1.078071568 | 1.051703819 | +2.76e-4 | −2.637e-2 | −0.0105 | **oscillatory**: no p, no GCI |
| Cd | 0.012056016 | 0.011903206 | 0.014808825 | −1.53e-4 | +2.906e-3 | −0.0526 | **oscillatory**: no p, no GCI |

- The L5→L4 change is two orders smaller than the L6→L5 change, with the opposite sign. L6 is outside the
  asymptotic range (**Inferred**: with the L5 Δcomp added, its Cl is 1.74 % below CFL3D's L6 value while L5 and L4
  are within 0.3 %).
- Cl's L5/L4 pair also fails A4 clause 5: the grid change (2.8e-4) is so small that 1 % of it (2.8e-6) is below the
  L4 iterative half-band (9.8e-6). A tighter Cl cap would cost more iterations on L4 (**Estimate** from the C-3 decay:
  roughly 10–15 k more for 3e-6).
- Hypotheses for the non-monotone sequence, **not tested** (no retune after a failed acceptance): the first-order
  nuTilda advection (TMR warns it raises GCI), the `limited corrected 0.5` laplacian on the far-wake faces with
  non-orthogonality > 70° (206 faces on L6), and SA-noft2's Ŝ limiter (Cs 0.3).

## Comparison with TMR (code-to-code verification, never "validated")

| Level | OpenFOAM Cl (incomp.) | CFL3D Cl (1st-order turb., compressible, Family II) | Δ | OpenFOAM Cd | CFL3D Cd | Δ |
|---|---|---|---|---|---|---|
| L6 | 1.051704 | 1.082906 | −2.88 % | 0.0148088 | 0.0149924 | −1.22 % |
| L5 | 1.078072 | 1.087349 | −0.85 % | 0.0119032 | 0.0128284 | −7.21 % |
| L4 | 1.077796 | 1.088501 | −0.98 % | 0.0120560 | 0.0123622 | −2.48 % |

CFL3D values from [reference/cfl3d_results_sa_nopv_withN.dat](reference/cfl3d_results_sa_nopv_withN.dat) (rows
N = 14,336 / 57,344 / 229,376). CFL3D is compressible (M 0.15); the OpenFOAM column is not yet corrected.

**Compressibility delta, measured (C-4 minus C-2):** same OpenFOAM v2512 build, SA-noft2, L5 grid, schemes and
relaxation; `rhoSimpleFoam` against `simpleFoam`. So Δcomp holds the Mach, variable-property (Sutherland, adiabatic
wall) and compressible-formulation effects together, at TMR conditions. ΔCl = **+0.012370 (+1.147 %)**,
ΔCd = **+0.000096 (+0.81 %)**: one grid (L5), α 10°, Re 6e6, M 0.15. Prandtl–Glauert gives 1.144 % on Cl (plan
§2.4): agreement in magnitude, not a precision check (the α 10° suction peak is non-linear; **Inferred**). U_Δ is at
least the L5 numerical uncertainty, which is not available (no GCI), so U_Δ is not stated. Δcomp is used only to
compare with compressible references; it is never applied to water results (M ≈ 0.007 at 10 m/s).

Comparability of the table: both sides no point vortex and first-order turbulence advection; Ŝ limiter OpenFOAM
0.3 Ω clip against CFL3D's (not stated by TMR); OpenFOAM incompressible against compressible CFL3D. With the L5
Δcomp added to every level, OpenFOAM's Cl is −1.74 % (L6), +0.28 % (L5) and +0.15 % (L4) from CFL3D's (Δcomp from
L5 applied to L6 and L4: the grid mismatch is unquantified).

**Informative only (not the acceptance test; Δcomp from L5 applied to L4, grid mismatch unquantified):** L4 + Δcomp = Cl 1.090166, Cd 0.012152. The TMR grid-converged Family II
band is Cl 1.09085–1.09103, Cd 0.0122715–0.0122740 ([round 1](verdict.md#case-and-sources)). The differences are
−7.7e-4 (−0.07 %) in Cl and −1.2e-4 (−1.0 %) in Cd, but L4 is a single grid with no extrapolation and no U_SN, so
no pass/fail is claimed.

## Durations (measured) against the plan

| Block / run | Plan (Estimate) | Measured | Note |
|---|---|---|---|
| C-1 (+ two repairs) | ≤ 4.3 min | 11.6 min wall (solver 238 + 249 + 50 s) | repairs inside the cap of 2 |
| C-2 | ≤ 11 min | 5.3 min (317 s) | A4 stop at 17,038 |
| C-3 | ≤ 50 min (30 ms/it) | **61.7 min** (3,701 s; **90 ms/it** on 6 ranks) | the round-1 cells-per-rank scaling under-predicted 3× |
| C-4 | ≈ 20–25 min | 6.3 min (378 s; 24.7 ms/it) | no repair needed |
| C block, wall incl. generation and waits | 85 min | **85 min** (20:36–22:01 UTC) | within budget; no load or join-lock wait occurred |

Measured solve cost per iteration: L6 7.8 ms (2 ranks), L5 18.5 ms (4 ranks), L4 90 ms (6 ranks), L5 compressible
24.7 ms (6 ranks). **Peak RSS** (`/usr/bin/time -l`, maximum resident set size of the launched process tree as
reported): solver 117–195 MB, checkMesh L4 628 MB; `ps` during C-3 showed 171–181 MB per rank (6 ranks ≈ 1.05 GB in
total, **Verified** sample at iteration ≈ 4,300). `time -l` reports one process's maximum, not the sum over ranks
(**Inferred** from the C-3 value matching one rank). Peak 1-minute load at launch boundaries: 8.20 (sampled only at
each start and end).

## What the backend ADR can decide now (macOS arm64 only)

- **Adopt A4** as the iterative criterion: it ran, it stopped runs within 4 iterations, and it rejected a period-2
  cycle whose mean was off by 1.2e-4 in Cl while residuals had dropped 6 orders.
- **Candidate SA numerics (met A4 on this case only):** nuTilda `bounded Gauss upwind`, U and ν̃ relaxation 0.7,
  SIMPLEC (p 1); TVD limitedLinear clipped and chattered on TMR grids. Grid convergence is not shown, and nothing
  here is tested at hydrofoil Re (2e5–2e6, where transition matters) or in 3-D. Record the Ŝ limiter (Cs 0.3).
- **The compressibility delta is measured, not modelled:** +1.147 % Cl, +0.81 % Cd — one grid (L5), α 10°, Re 6e6,
  M 0.15, SA-noft2; U_Δ not stated (≥ the unknown L5 numerical uncertainty). Only for comparison with compressible
  references; never applied to water results.
- **Cannot decide:** a grid uncertainty for OpenFOAM on this case (oscillatory sequence); whether second-order
  turbulence advection with a different limiter or a non-limited laplacian restores monotone convergence (not run —
  no retune after failure); SA-neg; Windows (DR-F2-7); v2606 (plan §6).
- **Fixture (DR-F2-9):** the TMR + A4 run set (C-1r2, C-2, C-3, C-4: about 78 min solver time here) is the pin-change
  fixture; the GCI clause needs a decision first (accept the oscillatory bound, or change numerics and re-run).

## Reviewer verdicts

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| CFD numerical verification (author) | — | does not clear its own veto | no "converged" label without A4 evidence; no GCI number from an oscillatory sequence |
| Hydrofoil hydrodynamicist (Adversary, read only) | PASS WITH CONDITIONS | not triggered | Δcomp is not "same numerics" (application, energy equation, Sutherland, adiabatic wall) and carries its envelope; "fully turbulent" stated; candidate numerics scoped to this case; comparability line; L5 Δcomp on L4 flagged; Prt deviation recorded (v2512 default 1.0, TMR 0.90); reference SOURCE.txt node/cell counts corrected — all applied above |
