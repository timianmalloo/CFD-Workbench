---
id: proof-spike-04-round3
title: SPIKE-04 round 3 verdict — searching for a monotone TMR grid family (OpenFOAM v2512)
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-04, openfoam, verification, gci, tmr, naca0012, spalart-allmaras, round-3, a4]
links:
  - {to: proof-spike-04-round2, rel: supersedes}
  - {to: plan-fluids-round3, rel: implements}
  - {to: rulings, rel: depends-on}
  - {to: adr-0012-openfoam-backend-macos, rel: relates-to}
  - {to: proof-spike-03-round3, rel: relates-to}
review-by: 2026-11-03
summary: >-
  GCI NO-GO: no monotone triplet was admitted, so no observed order and no GCI. R3-G0 rejects H1: the unlimited
  laplacian moves L6 Cl by -1.03e-4, below the 2.8e-4 threshold. Numerics cycle 1 (limitedLinear 1 nuTilda at
  relaxation 0.7) misses A4 on L6 on clause 3 (5,013 clipped iterations in 40,000), so cycle 2 is not run. Per DR-F3-3
  the L3 run used the D4 numerics. It reached the 10 h solve cap at 41,005 iterations without A4 (Cl half-band 9.8e-5
  against 1e-5), so the L5/L4/L3 triplet is not admitted. A4 and D4 stand. DR-F3-7 default (c) is recommended.
review-suggested: []
---

# SPIKE-04 round 3 verdict — a monotone TMR grid family

Lane F1, round 3. Case: TMR 2DN00, NACA 0012, α 10°, Re 6e6, **fully turbulent (no trip, no transition model)**,
SA-noft2 (ESI `SpalartAllmaras`, Ŝ limiter Cs 0.3), incompressible `simpleFoam`, Family II grids. Authority:
[plan](../../plans/fluids-round3.md) §3 and §4; [Ruling 68](../../notes/rulings.md) (DR-F3-3: the L3 triplet only if
the first numerics cycle fails, 12 h cap, unattended; DR-F3-7 decided after round 3). Round 2:
[verdict-round2.md](verdict-round2.md) (kept unchanged). Lead lens: CFD numerical verification; the hydrofoil
hydrodynamicist reviews the physical claims. Every number is read from a file under [receipts/](receipts/)
(`*-spike04r3-*`) or [reference/](reference/), unless it is labelled **Estimate** or **Inferred**.

## Verdict

| Plan criterion (§3.4 GCI acceptance, unchanged from round 2) | Result | Evidence |
|---|---|---|
| Three grids admitted under A4 with clause 5 | **no**. Cycle 1: L6 misses A4. R3-G2: L3 misses A4 at the cap | G1b-L6 and G2-L3 `convergence.txt` |
| 0 < R < 1 on Cl and Cd | **not computable** (no admitted triplet) | — |
| p and GCI_fine (Celik, Fs 1.25), labelled "pre-asymptotic; not shown conservative" | **none** | — |

**SPIKE-04 round 3: NO-GO on the GCI clause.** Every hypothesis the plan could test within its cycles was tested. The
order was fixed before the runs. A failed acceptance is recorded as a finding. No numerics were retuned after a
failure.

## Runs

| Id (case file) | Grid · ranks | Change from D4 | Iterations · solver wall | A4 | Cl (window mean · U_I) | Cd (window mean · U_I) |
|---|---|---|---|---|---|---|
| R3-G0 ([g0-l6](../../../cases/spike04r3-g0-l6.yaml)) | L6 225×65 · 2 | laplacian `Gauss linear corrected`, snGrad `corrected` (no limiter) | 6,545 · 39 s | **met** | 1.051600898 · 7.9e-6 | 0.014812212 · 3.3e-7 |
| R3-G1b L6 ([g1b-l6](../../../cases/spike04r3-g1b-l6.yaml)) | L6 · 2 | nuTilda `bounded Gauss limitedLinear 1` at relaxation 0.7 | 40,000 (cap) · 203 s | **no**: clause 3 (250 clipped iterations in W) | not reported (A4 not met) | not reported |
| R3-G2 L3 ([g2-l3](../../../cases/spike04r3-g2-l3.yaml)) | L3 1793×513 (917,504 cells) · 6 | none (D4: upwind nuTilda, relaxation 0.7, SIMPLEC p 1, limited corrected 0.5) | 41,005 · 36,038 s (10 h wall cap) | **no**: clause 2 (Cl U_I 9.8e-5 vs 1e-5; Cd 3.9e-6 vs 1e-6) | not reported (A4 not met) | not reported |

R3-G0's values are a **scheme-sensitivity diagnostic**: incompressible, code-to-code, grid U not quantified. L6 sits
−2.9 % from CFL3D in Cl, and Cd is fully turbulent. They are not a result for the family.

Residual drop at the last window: ≥ 7.5 orders on every equation in R3-G2 (7.57 at worst, nuTilda) (clause 1 passes). There was no clipping
(clause 3 passes). The forces were still drifting: Cl moved by about −1e-3 per 10,000 iterations between 20,000 and
41,000 ([every 10k](receipts/20261004T181801Z-spike04r3-g2-l3/cl-cd-every-10k.txt)). By the round-2 rule a run that
ends without A4 is "Completed — residual criterion not met (ran to limit)": a gap, not a number. Its forces are
therefore not compared with any reference.

### R3-G0 — H1 (the limited laplacian makes L6 a different discretisation): rejected

- **Reading stated before the run:** supported if |Cl − 1.051704| > 2.8e-3 and Cl moves toward 1.078; rejected if the
  change is < 2.8e-4.
- **Measured:** ΔCl = 1.051600898 − 1.051704 = **−1.03e-4**, 0.4 % of the L6→L5 gap (2.637e-2), and *away* from L5/L4.
  ΔCd = +3.4e-6. L6 still has 206 faces > 70° (max 79.7°, `log.checkMesh`). With no limiter it still met A4, at 6,545
  iterations against 6,375. **H1 rejected.** The family's L6 offset is not the laplacian limiter.

### R3-G1b on L6 — cycle 1 (H2: second-order nuTilda advection): misses A4

- The cycle was chosen because G0 did not support H1 (plan §3.4).
- `limitedLinear 1` at relaxation 0.7 (never run before) clipped nuTilda in **5,013 of 40,000** iterations, and in 250
  of the final 2,000 ([count](receipts/20261004T181213Z-spike04r3-g1b-l6/bounding-count.txt)). Round-2 C-1 at
  relaxation 0.9 clipped in 8,000 of 40,000. Lowering the relaxation did not remove the clipping.
- **The solution is an exact period-8 cycle** (Verified, same receipt). Clipping falls every 8th iteration (…39,985,
  39,993), and Cl and Cd repeat with period 8 to about 1e-8 (Cl 1.0550942 ↔ 1.0550905). 2,000 / 8 = 250 clipped
  iterations per window, and the window mean is constant across monitor polls. The monitor re-read the table: Cl_last
  changed between polls. So stationarity "passes" (Cl 1.9e-6, Cd 8.3e-7) on a limit cycle. Clause 3 is what rejects it,
  as clause 2 rejected round 2's period-2 cycle.
- **Cycle 1 fails on its coarsest grid**, so the triplet cannot be admitted. L5 and L4 with these numerics were **not
  run**: a triplet with one grid outside A4 cannot pass. Cycle 2 (G1a) runs "only if cycle 1 meets A4 but is
  non-monotone" (plan §3.4), so it was not run. H2 is **untested**: the one second-order scheme tried could not be admitted under A4.

### R3-G2 — L3 (H3: L6 is outside the asymptotic range; test the finer triplet), D4 numerics

- **Run under DR-F3-3:** the first numerics cycle failed. Stated before the run: A4 first; then the same run extends,
  with no setting changed, until U_I ≤ 2.0e-6 (Cl) and 2.0e-7 (Cd), or +20,000 iterations. A 10 h solve cap was set,
  leaving about 2 h of the 12 h cap for the L4 and L5 re-runs that clause 5 needs (round-2 L4's Cl half-band 9.8e-6
  fails the L5/L4 bound of 2.8e-6).
- **Measured:** the 10 h cap stopped the run at 41,005 iterations without A4. L4 had needed 41,055 iterations at a
  quarter of the cells. The run averaged **0.879 s/it** on 6 ranks under shared load. The plan's Estimate was
  ≈ 0.36 s/it (L4's rate scaled by cells). The measured rate is 2.4× slower. Ten-minute samples ranged from about 0.6
  s/it (1-min load 8–20) to 1.4 s/it (load 20–40).
- **Consequence:** the L4 and L5 re-runs were not run (the triplet cannot be admitted without L3). **H3 is untested.**
  L3 to A4 needs more than 41,005 iterations. **Estimate** (model: round-2 iterations to A4 grew 2.67× and then 2.41×
  per level, applied to L4's 41,055): 99k–110k iterations, or 24–27 h at 0.88 s/it and 16–18 h at 0.6 s/it. The L3
  trajectory does not yet show a decay. Over the last polls U_I rose from 8.7e-5 to 9.8e-5, and Cl rose and then fell.
  So even the Estimate may be low. It does not fit a 12 h cap on this shared host.

## The GCI procedure on CFL3D, reported beside the result (hydrodynamicist condition)

Run with [`gci.py`](../../../cases/tools/gci.py) on [reference/cfl3d_results_sa_nopv_withN.dat](reference/cfl3d_results_sa_nopv_withN.dat)
(Family II, first-order turbulence advection):

| Triplet · QoI | R | p | GCI_fine (absolute) | Error of the fine grid vs CFL3D L1 (L1 itself not error-free) | Ratio |
|---|---|---|---|---|---|
| L6/L5/L4 · Cl | 0.259 | 1.95 | 5.0e-4 | 2.15e-3 | **under-states 4.3×** |
| L5/L4/L3 · Cl | 0.836 | 0.26 | 6.1e-3 | 1.18e-3 | conservative (5.2×) |
| L5/L4/L3 · Cd | 0.197 | 2.34 | 2.8e-5 | 8.1e-6 | conservative (3.5×) |

So even for the reference code, L6/L5/L4 is not a safe GCI triplet. L5/L4/L3 was conservative here, but only because
p = 0.26 for Cl, far below the formal order, makes the factor 1/(r^p − 1) about 5.1. That a usable GCI on this case
needs L3 or finer is **Inferred**.

## Durations, load and memory (measured)

| Block / run | Plan (Estimate) | Measured compute | Measured wall incl. waits |
|---|---|---|---|
| R3-G0 | ≤ 5 min | 39 s solve (+ about 10 s generation, checkMesh, decompose) | 18 min (17:52–18:10 UTC; load waits) |
| R3-G1b, L6 only (cycle 1 stopped) | 70–90 min for the cycle | 203 s solve (5.1 ms/it on 2 ranks) | 3.6 min |
| Block G core (G0 + cycle 1) | 100 min compute, 2.2–2.5 h wall | **4.0 min** | 22 min |
| R3-G2 L3 (DR-F3-3) | 4.1–9.9 h + ≈ 1 h fetch and generation; cap 12 h | fetch 2 s; solve **36,038 s (10.0 h)** | **10 h 57 min** (18:18–05:15 UTC; about 55 min of load and join-lock waits before the solve) |

- **Peak RSS** (`/usr/bin/time -l`, one process): L3 solver 400 MB, checkMesh 2.22 GB, reconstructPar 1.16 GB. L6
  solver 117–193 MB.
- **Load:** the 1-minute load at our launch boundaries was 7.2–9.6. During the L3 solve, other tracks raised it to
  40 in samples. Every process ran `nice 10`, ≤ 6 ranks, one job at a time. All 12 SPIKE-04 launches printed `Disallowing` (21 in round 3 with SPIKE-03); no stop file.

## Defects found and fixed (class → control)

- **Monitor start race:** `a4-monitor.py` treated "no solver process for 120 s" as "solver gone". `of-run.sh` can wait
  up to 60 min for load or the join lock before it launches the solver. A delayed launch would have run unmonitored,
  with no A4 stop. Fixed: the monitor exits only after it has seen the solver, or after 65 min unseen. The wall cap
  counts from the first sighting. In R3-G2 the solver launched 56 min after generation, and the monitor saw it.
- **Clause-5 extension** (`numerics.a4.clause5_extension`) is new in `a4-monitor.py`. It continues the same run past
  A4, with no setting changed, until the pair bound or target caps hold. It was configured for L3. L3 never reached
  A4, so it never acted, and it is **untested in a run**.

## What ADR-0012 can add now (macOS arm64 only)

- **A4 and D4 stand** as decided in round 2 (D4 met A4 on L6, L5 and L4). R3-G0 adds that the `limited corrected 0.5`
  laplacian is not why L6 is offset.
- **The two second-order nuTilda schemes tried fail A4 clause 3 on TMR L6** (OpenFOAM v2512):
  - `limitedLinear 1` at relaxation 0.9 (round 2) and 0.7 (round 3, a period-8 limit cycle);
  - `linearUpwind` (round 1: clipped in 39,994 of 40,000 iterations).
  Other limiters, L5/L4 and the 3-D wing cases are untested. The round-2 wing cases used `limitedLinear 1`, so this
  says nothing yet about them. A 3-D solve uses D4 or declares the deviation (ADR-0012 D4).
- **GCI: documented absence.** No monotone admitted triplet exists among L6, L5, L4 and L3 within round 3's cycles and
  caps. The application shows "Grid U: not quantified". It never shows a GCI from L6/L5/L4, which under-states the
  error 4.3× even for CFL3D.
- **Cost of L3 on this host:** more than 10 h and more than 41k iterations without reaching A4. The L3 triplet is
  therefore not a pin-change fixture.
- **DR-F3-7 recommendation: (c).** Keep the TMR + A4 fixture (C-1r2, C-2, C-3, C-4) without its GCI clause.
  - The fixture's label stays "code-to-code comparison; U_num not computable". It never says "verified" or
    "validated against TMR".
  - Its envelope is Re 6e6, 2-D, single-phase and fully turbulent. That is outside the product's transitional
    Re 5e5–2e6 band.
  - Option (a), an oscillatory-bound uncertainty, needs the ITTC 7.5-03-01-01 text re-opened (**Flagged**). It would
    rest on the round-2 L6/L5/L4 oscillation, which includes the L6 outlier.
  - Option (b), a least-squares fit over ≥ 4 grids, needs an admitted L3, which this round could not produce.
- **Not decided:** H3; any GCI; Windows (DR-F2-7); v2606.

## Reviewer verdicts

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| CFD numerical verification (author) | — | does not clear its own veto | readings stated before each run; no value reported from a run without A4; no GCI from a non-admitted triplet |
| Hydrofoil hydrodynamicist (Adversary, read only) | **PASS WITH CONDITIONS** (re-ran G0's delta and thresholds, the CFL3D GCI table, the L3 rate and the 24 h Estimate) | not triggered | 1 the second-order statement scoped to the schemes, grid and code tried (ADR section) · 2 "H2 untested" (G1b) · 3 a receipt for the 5,013 count (G1b; it also showed the period-8 cycle) · 4 the L3 Estimate given as 99k–110k with its model and the missing decay (G2) · 5 the CFL3D L1 column labelled and "needs L3 or finer" marked Inferred (GCI table) · 6 G0 values scoped as a diagnostic (runs) · 7 "≥ 7.5 orders" (runs) · 8 the DR-F3-7 (c) label and envelope (ADR section) — all applied |
