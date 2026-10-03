---
id: plan-fluids-round2
title: Fluids round 2 — convergence, meshing and security plan for SPIKE-03 / SPIKE-04
type: doc
status: in-review
owner: "@fluids-f1"
tags: [plan, spike-03, spike-04, openfoam, su2, verification, meshing, security, backend]
links:
  - {to: proof-spike-03, rel: depends-on}
  - {to: proof-spike-04, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-03
summary: >-
  Round 2 plan, documents only. Convergence: judge the iterative part on ITTC's three-order residual drop plus a
  stated Cl/Cd stationarity band, run OpenFOAM simpleFoam SA with a TVD nuTilda scheme on TMR levels 6/5/4, and
  measure (not model) the compressibility delta; SU2 v8.5.0 (SA-neg, x86_64-only macOS binary) is a referee leg
  only on operator approval. Meshing: wall-resolved y+ <= 1 (the wall-function floor does not fit inside the
  boundary layer over most of the chord at Re_c 6e5), snappy with a finite trailing edge plus a Gmsh layer probe at AR 8, and the "determinant > 0.3" floor
  replaced (OpenFOAM's cellDeterminant is not the ITTC Jacobian measure). Security: pin a product-owned
  controlDict, prove refusal with an operator-run probe set. Core budget about 4.1 h wall.
review-suggested: []
---

# Fluids round 2 — plan (documents only)

**Goal.** Decide, with evidence and cost, how round 2 closes SPIKE-04 (convergence oracle), SPIKE-03 (meshing)
and the backend-substrate security row, and size the run list. **Done when** each question has options,
evidence, cost and one recommendation; the run list has a stop rule and a budget; the open decisions are named.
**Not in scope:** any solver, mesher or probe run; edits to the spec, schema, tools or `~/.zshrc`. **Tier:** T1.
Lead lens: CFD numerical verification. Reviewers: hydrofoil hydrodynamicist, security & identity architect.

Labels: **Verified** = observed in a file, a source line, a receipt or a cited page; **Inferred** = reasoned
from Verified facts; **Estimate** = a number from a stated model. `assume:` lines carry a belief, its check and
its blast radius.

## 0. Grounding (opened, not recalled)

- Round 1: [SPIKE-03 verdict](../proof/spike-03/verdict.md), [SPIKE-04 verdict](../proof/spike-04/verdict.md),
  SPIKE-04 receipts (`solverInfo` and `convergence.txt` per run), `cases/*.yaml`, `cases/tools/of-run.sh`,
  `schemas/cfd-case.schema.json`.
- Spec [cfd-workbench-v1](../specs/cfd-workbench-v1.md): area 5 Run (:172), A5.10 (:1038–1076; mesh gate :1062–1065;
  Converged :1075), A7 Converged label (:1227), readiness fixture (:1290), A8.5 backend-substrate row (:1306).
- [Rulings](../notes/rulings.md) 60–64. Only Ruling 60 governs fluids: F1 runs on OpenFOAM v2512, ≤ 6 cores,
  nice 10, no solve during a join or readiness; the backend ADR waits on F1; every run has a `cases/` YAML.
- OpenFOAM v2512 source on the mounted app volume `/Volumes/OpenFOAM-v2512` (read only; nothing executed).
- ITTC 7.5-03-02-03 Rev 02 (2024), text extracted from <https://www.ittc.info/media/11958/75-03-02-03.pdf>.
- TMR pages, SU2 docs and GitHub releases (URLs inline).

## 1. Summary

| Question | Recommendation | Cost (wall, Estimate unless marked) |
|---|---|---|
| Q1 Convergence | Iterative criterion = ITTC ≥ 3-order residual drop **and** Cl/Cd stationarity (half-band U_I ≤ 1 % of the grid-to-grid change, absolute caps). Product leg: OpenFOAM `simpleFoam` SA (SA-noft2) with `bounded Gauss limitedLinear 1` for nuTilda on TMR L6/L5/L4. Incompressible run, compressibility delta **measured** on L5. SU2 SA-neg as a referee leg only if DR-F2-2 approves. | ≈ 85 min core; + ≈ 95 min if the SU2 referee runs |
| Q2 Meshing | Wall-resolved y+ ≤ 1 (≥ 20 layers, ER ≤ 1.2), labelled fully turbulent; a ruling gates the wall-function branch behind a feasibility check. AR 8 probes: snappy with a finite-thickness TE and tuned layer controls, then a Gmsh boundary-layer probe; the first to pass runs AR 5 and 12. Replace "determinant > 0.3" for unstructured meshes. | ≈ 100 min |
| Q3 Security | Every OpenFOAM process runs from an allow-listed environment with a product-owned `HOME` **and** an explicit `FOAM_CONTROLDICT`, both from one hashed bundle. A pre-flight banner check ("Disallowing") gates the solver. An allow-list lint runs on expanded dictionaries, because `libs` has no gate. Live proof = eight probes, operator-run with `!`. | ≈ 5 min compute, ≈ 30 min authoring |
| Total | Core (S + C + M), one job at a time, +30 % for load and join-lock waits | **≈ 4.1 h**; with the SU2 referee **≈ 6.1 h** |

## 2. Q1 — Convergence (SPIKE-04)

### 2.1 Facts

1. **Round 1 met ITTC's residual criterion but not the case file's.** OpenFOAM normalises each equation's
   initial residual to 1.0 at iteration 1 (Verified: first `solverInfo` row of all three receipts reads
   `Ux 1.0 · Uy 1.0 · nuTilda 1.0 · p 1.0`). The final initial residuals are U and p ≤ 4.6e-7 and nuTilda
   4.3e-5 to 1.9e-4 (Verified, SPIKE-04 table). That is ≥ 3.7 orders on every equation. ITTC 7.5-03-02-03
   §3.8: "The recommended criterion is the drop of scaled residuals by at least three orders of magnitude off
   their initial values. However … due to … oscillatory convergence this criterion may not be satisfied. In
   such cases, the convergence of forces and moments … can be monitored" (Verified, PDF text). Round 1's
   absolute 1e-7 floor was **stricter than the standard it cites**. Round 1 is not relabelled; the criterion
   changes before the next run.
2. **TMR's reference codes also stopped short of full turbulence convergence.** "Full convergence of this
   particular quantity in CFL3D on the finest grids had to be abandoned" (eddy viscosity in the wake) —
   <https://tmbwg.github.io/turbmodels/naca0012numerics_val_sa_withoutpv.html> (Verified). Round 1 put 92 % of
   the nuTilda residual in 100 wake cells 1–100 c downstream (Verified, receipt).
3. **Iterative band against grid change (round 1, gaps).** The L6→L5 change is ΔCl = 0.0244, ΔCd = 0.0031. The
   1,000-iteration bands are Cl ≤ 2e-5 and Cd ≤ 2.3e-5 (Verified, SPIKE-04 table): 3–4 orders smaller.
4. **SA variants.** v2512 ships `SpalartAllmaras` as SA-noft2 with the Ŝ ≥ Cs·Ω limiter, Cs = 0.3
   (Verified, `SpalartAllmaras.H:49-53, 87-91`). It has no SA-neg (round 1). TMR: SA-neg "is the same as the
   'standard' version (SA)" when ν̃ ≥ 0, and is "passive … in well-resolved flowfields"; "(SA-noft2) is not
   compatible with (SA-neg)"; the Ŝ limiting method "should always be reported" —
   <https://tmbwg.github.io/turbmodels/spalart.html> (Verified).
5. **Reference numerics.** FUN3D and TAU used SA-neg with 2nd-order turbulence advection. CFL3D used SA with
   1st-order turbulence advection by default (Verified, TMR SA page above). So a 1st-order-nuTilda OpenFOAM run
   has a matched per-level reference (CFL3D), and a TVD run has the FUN3D band.
6. **Clipping is the failure signature.** `linearUpwind` clipped nuTilda on 39,994 of 40,000 iterations and froze
   at a clip fixed point. `limitedLinear 1` on L5 never clipped ("bounding" 0 lines) and settled into a period-2
   cycle with Cd 0.0120347 ↔ 0.0120334 (Verified, SPIKE-04).
7. **SU2 v8.5.0.**
   - `SA_OPTIONS` lists `NEGATIVE, EDWARDS, WITHFT2, QCR2000, COMPRESSIBILITY, ROTATION, BCM, EXPERIMENTAL`;
     the template reads "File Version 8.5.0 "Harrier"". Source:
     <https://raw.githubusercontent.com/su2code/SU2/master/config_template.cfg> (Verified). **SA-neg ships.**
   - **TMR NACA 0012:** a *tutorial*, not a V&V case.
     <https://su2code.github.io/tutorials/Inc_Turbulent_NACA0012/> runs `INC_RANS` SA on the TMR 897×257 C-grid
     at Re 6e6, α 0° and 10°. It compares with Gregory–O'Reilly data and CFL3D ("nearly indistinguishable"). It
     has no grid study and no GCI. The V&V index <https://su2code.github.io/vandv/home/> lists the flat plate,
     the bump, 30p30n and others, but **no NACA 0012** (Verified).
   - **The macOS binary is x86_64 only.** Release assets: macos64, macos64-mpi, win64 and linux64
     (Verified, `gh api …/releases/tags/v8.5.0`). `SU2-v8.5.0-macos64-mpi.zip` has sha256 `8328e3bf…901af9`.
     `file bin/SU2_CFD` → "Mach-O 64-bit executable x86_64", "not signed at all" (Verified; downloaded to the
     scratchpad, not run). On this arm64 host it needs Rosetta 2.
   - `assume:` Rosetta 2 is not installed here — `/Library/Apple/usr/libexec/oah` holds only `RosettaLinux`.
     *Confirm:* `arch -x86_64 /usr/bin/true`. *If false:* the setup step drops out.
   - Apple's plan keeps general-purpose Rosetta only through macOS 27, and this host runs Darwin 27 (Verified:
     secondary press citing Apple developer documentation, e.g.
     <https://www.macrumors.com/2026/02/16/macos-tahoe-26-4-rosetta-2-warnings/>). The spec's "SU2 v8.5.0 release
     binaries" route on macOS arm64 therefore has a **known end date** (Inferred).
8. **OpenFOAM SST has no oracle here.** TMR 2DN00 lists SA only ("Other turbulence models results may be added
   in the future") — <https://tmbwg.github.io/turbmodels/naca0012numerics_val.html> (Verified).
9. **TMR requires compressible.** "The turbulent NACA 0012 airfoil case should be run with a compressible CFD
   code" (Verified, same page). The product's hydrofoil solver is incompressible (`simpleFoam`, spec A5.10).

### 2.2 (a) Criterion — options

| Option | Evidence | Cost | Verdict |
|---|---|---|---|
| A1 Absolute floor, all initial residuals ≤ 1e-7 (round 1) | Not met by any SA run on TMR grids. Stricter than ITTC. TMR's own CFL3D could not converge the wake ν̃ | Unbounded: no numerics change has met it | reject |
| A2 ITTC residual drop ≥ 3 orders only | Met by every round-1 run, **including the clip fixed point** | none | reject alone: it admits a clipped fixed point |
| A3 QoI stationarity only | Matches CFL3D practice; round-1 bands are 3–4 orders below the grid change | none | reject alone: it ignores a stalled solve |
| **A4 Composite (recommended)** | ITTC §3.8 text (Verified) + the band/change ratio (Verified) + the clipping signature (Verified) | one harvester script change in round 2 | **adopt** |

**A4, stated before any round-2 run (copied verbatim into each case file's `numerics.residual_criterion`):**

1. Residual drop: every equation's initial residual ≤ 1e-3 × its iteration-1 value (ITTC three orders).
2. Stationarity over a window W = the last 2,000 iterations (even, so a period-2 cycle is whole): for Cl and Cd,
   U_I = ½ (max − min) ≤ caps of **Cl 1e-5** and **Cd 1e-6** (0.01 drag count). These caps sit below the TMR
   grid-converged band widths (Cl 1.8e-4, Cd 2.5e-6, Verified in SPIKE-04).
3. Clean window: zero "bounding nuTilda" lines inside W.
4. Stop: the harvester polls `solverInfo` and `forceCoeffs` every 1,000 iterations. When 1–3 hold, it sets
   `stopAt writeNow;` in the case `system/controlDict`. A run that reaches `max_iterations` is the existing
   state "Completed — residual criterion not met (ran to limit)", a gap and not a number.
5. Post-hoc admission into GCI: U_I ≤ 0.01 × |ε| of its neighbouring grid pair, for Cl and Cd. The reported
   value is the window mean.

Why the harvester and not `runTimeControl`: v2512's conditions are `average` ("satisfied when average does not
change by more than a given value", a running-mean test), `equationInitialResidual`, `equationMaxIter`,
`minMax`, `maxDuration` and `minTimeStep` (Verified, `runTimeControl/runTimeCondition/`). None of them expresses
a max−min band over a window. `assume:` the runtime edit of `stopAt` is honoured, because the generated
controlDict sets `runTimeModifiable true`. *Confirm:* in C-1 (the ledger shows the stop iteration). *If false:*
runs go to the cap, which costs at most the C-1..C-3 caps in §2.5.

**Spec effect.** A7 part 1 reads "residual criterion fired". A4 is the residual criterion with a stated
stationarity clause. Making that explicit needs a spec amendment (**DR-F2-1**).

### 2.3 (b) Solver route — options

| Option | Evidence | Cost | Verdict |
|---|---|---|---|
| **B1 OpenFOAM v2512 `simpleFoam` SA-noft2, `bounded Gauss limitedLinear 1` for nuTilda, A4** | Same pinned backend as the product. L5 clean, no clipping. SA-neg = SA where ν̃ ≥ 0 (TMR). Ŝ limiter Cs 0.3 recorded per TMR | 3 runs, ≈ 65 min | **product leg — adopt** |
| B2 Same with `bounded Gauss upwind` for nuTilda | Matched-numerics reference: CFL3D 1st-order per level (`reference/cfl3d_results_sa_nopv_withN.dat`). TMR warns 1st order raises GCI | 3 runs, ≈ 65 min | diagnostic only: run if B1 misses the band beyond U |
| B3 SU2 v8.5.0 SA `NEGATIVE`, compressible M 0.15 | Exact TMR model and conditions; matches FUN3D. Checks the grids, converter and GCI procedure independently of OpenFOAM | Setup: Rosetta install (operator) or native arm64 build (Estimate 20–40 min at ≤ 6 cores). Runs ≈ 55 min | **referee leg — DR-F2-2** |
| B4 SA-neg as a compiled, pinned OpenFOAM library | A8.5 allows only compiled and pinned code. A new numerical code to verify in its own right | Days (Estimate) | reject for round 2 |
| B5 OpenFOAM kOmegaSST | No TMR 2DN00 SST reference (Verified) | — | reject for the oracle |

### 2.4 (c) Compressible vs incompressible — options

| Option | Evidence | Cost | Verdict |
|---|---|---|---|
| C1 Incompressible + Prandtl–Glauert (round-1 `assume:`, ≈ 1.1 % on Cl) | An inviscid model correction with no stated uncertainty. TMR requires compressible | 0 | reject as a gate |
| **C2 Incompressible product leg + measured Δcomp on L5** (same code, model and grid; compressible minus incompressible) | The delta is measured, not modelled | SU2: 2 runs on L5 (flip `SOLVER= RANS`/`INC_RANS`), ≈ 20 min. Without SU2: `rhoSimpleFoam` L5 vs C-2, ≈ 25 min, new thermo/BC set-up, repair-loop risk | **adopt** |
| C3 Compressible-only oracle in SU2 | TMR-exact, but verifies SU2, not the product backend | ≈ 55 min | only as the B3 referee |

`assume:` Δcomp from SU2 SA-neg transfers to OpenFOAM SA-noft2, because both models give the same converged field
where ν̃ ≥ 0. *Confirm:* if `rhoSimpleFoam` is also run, compare the two deltas. *If false:* U_Δ grows to |Δ_SU2|.
Even when the assumption holds, U_Δ is at least the L5 numerical uncertainty, because Δ comes from one grid.
For reference, Prandtl–Glauert at M 0.15 gives 1.144 % (Verified arithmetic, hydrodynamicist); it is a
sanity check only. The comparison with TMR is **code-to-code verification**, never "validated".

**Acceptance (SPIKE-04 GO).** Three grids admitted under A4. Observed order and GCI per ITTC 7.5-03-01-01 on Cl
and Cd, monotonic convergence (0 < R < 1). The comparison passes when |S_OF,ext + Δcomp − D_TMR| ≤
√(U_SN² + U_Δ² + U_D²), with D_TMR the Family II mid-band and U_D its half-width. Failure is a recorded finding,
not a retune.

### 2.5 Run list C (stop rule: repair cap 2 on numerics; any A4 failure on L6 stops C-2/C-3)

| Id | Run | Ranks | Cap | Expected wall | Basis |
|---|---|---|---|---|---|
| C-1 | OF L6, B1, A4 | 2 | 40,000 it | ≤ 4.3 min | measured 258 s / 40k (L6 TVD) |
| C-2 | OF L5, B1, A4 | 4 | 60,000 it | ≤ 11 min | measured 660 s / 60k |
| C-3 | OF L4 (897×257, 229k cells), B1, A4 | 6 | 100,000 it | ≤ 50 min | **Estimate** 30 ms/it (SPIKE-04 cells-per-rank scaling) |
| C-4 | Δcomp L5 (SU2 pair, or `rhoSimpleFoam`) | 4–6 | per code | ≈ 20–25 min | **Estimate**; no SU2 prior on this host |
| C-5 | (conditional) B2 on L6/L5/L4 | 2/4/6 | as C-1..3 | ≈ 65 min | runs only if acceptance fails |
| R-1..3 | (DR-F2-2) SU2 SA-neg compressible L6/L5/L4 | 6 | Cauchy on CL and CD + residual −8 | ≈ 3 / 10 / 40 min | **Estimate**; re-plan if R-1 > 10 min |

## 3. Q2 — Meshing (SPIKE-03)

### 3.1 Facts and arithmetic

1. **ITTC Table 1** (Verified, PDF): near wall y+ ≤ 1, ER 1.2, 20 points in the boundary layer. Wall functions:
   30 < y+ < 100, ER 1.2, 15 points. The text is softer than the spec: "when wall functions are used, then less
   than 15 grid points could suffice". The spec (A5.10 :1063) turns these recommendations into hard floors.
2. **ITTC Eq. (10) as printed uses `ln`.** It reads Cf = 0.455/[ln(Re_L)]^2.58 (Verified, PDF). The
   Prandtl–Schlichting correlation it reproduces uses log10: <https://www.cfd-online.com/Wiki/Skin_friction_coefficient>
   (Verified). At Re 6e5 the `ln` form gives Cf 5.73e-4, about 8.6× too small, so first-cell heights come out
   ≈ 2.9× too large. The knowledge base copies the `ln` form (`data-and-constants.md:370`, `08-…:46, 440`) and its
   worked example inherits the error. **Finding F-1** (§8).
3. **Numbers at the SPIKE-03 operating point.** Inputs: c = 0.12 m, U = 5 m/s, ν = 1e-6, Re_c = 6.0e5, log10
   form. These are flat-plate **Estimates** that assume turbulent flow from the LE (the hydrodynamicist
   reproduced them).
   - Cf 4.93e-3, u_τ 0.248 m/s.
   - First point: y(y+ = 1) = 4.0 µm, y(30) = 0.12 mm, y(100) = 0.40 mm.
   - Turbulent δ ≈ 0.37 x Re_x^-0.2: 3.1 mm at the TE and 1.18 mm at x/c 0.3.

   **Wall functions cannot meet the floor over most of the chord.** Fifteen uniform cells whose first centre
   is at y+ 30 are 15 × 0.24 mm = 3.6 mm thick. That is more than δ at every station, even before expansion:
   3× δ at x/c 0.3, though only 17 % over δ_TE. With node counting rather than cell centres the stack is
   1.8 mm, so the TE alone is marginal. The argument rests on the forward and mid chord.

   **The wall-resolved floor fits a turbulent layer.** A first cell of 8 µm (centre at y+ ≈ 1) with 20 layers
   at ER 1.2 is 1.50 mm thick, inside turbulent δ_TE. A *laminar* layer (δ ≈ 5x/√Re_x) holds only 11–16 of
   those layers between x/c 0.1 and 1.0 (hydrodynamicist, Verified arithmetic). So the floor counts **layers
   in the stack**; points inside δ99 are measured from the solution.

   Round 1 quoted 0.26–0.9 mm for y+ 30–100; neither form of Eq. (10) reproduces that range at Re_c. The
   a-posteriori y+ (measured) is what the gate reads.
4. **The wing operates in transitional territory.** At Re_c 6e5 a smooth foil has a laminar run, and wall
   functions assume a log-law turbulent layer (Inferred; the hydrodynamicist lens owns this claim). The spec
   already carries transition fields (A5.10 typed case model).
5. **What v2512 offers** (Verified, source on the app volume):
   - Mesh generators: `blockMesh`, `extrude`, `extrude2DMesh`, `foamyMesh`, `PDRblockMesh`, `snappyHexMesh`.
     **No cfMesh** (`cartesianMesh` not in `platforms/*/bin`). `gmshToFoam` and `extrudeMesh` are present.
   - Snappy layer controls in `etc/caseDicts/annotated/snappyHexMeshDict`: `thicknessModel`
     (`firstAndExpansion`, …), `featureAngle`, `layerTerminationAngle` ("Set to -180 always attempt
     extrusion"), `slipFeatureAngle`, `minMedialAxisAngle`, `maxThicknessToMedialRatio`, `nRelaxedIter`,
     `nLayerIter`, `meshShrinker displacementMotionSolver`, `detectExtrusionIsland`.
   - `nOuterIter`: `layerParameters.C:397`, default 1.
6. **Gmsh 4.15.2-git is installed** at `/opt/homebrew/bin/gmsh` (Verified). It is GPL and process-only under
   COMMIT-02. It is not in the spec's backend matrix.
7. **The Example closes its trailing edge** (Verified, `example.foil`: both surfaces end at (1, 0)). The spec
   already carries a **TE floor setting** in the run manifest's settings hash (:237, :269).

### 3.2 (a) Near-wall strategy — options

| Option | Evidence | Cost | Verdict |
|---|---|---|---|
| W1 Wall functions, 30–100, ≥ 15 points (spec today) | Infeasible over most of the chord at Re_c 6e5 (3.6 mm against δ 1.2–3.1 mm). Log-law physics are wrong over a laminar run | — | gate behind a feasibility check (DR-F2-3) |
| W2 Wall functions with fewer points (ITTC "less than 15 could suffice") | Meshable, but the physics objection stands | lower cells | reject |
| **W3 Wall-resolved, y+ ≤ 1, ≥ 20 points, ER ≤ 1.2 (ITTC branch 1)** | The stack fits (1.5 mm). Low-Re SA/SST and later LM transition are possible. Matches the SPIKE-04 oracle's y+ regime (L5 max 1.01) | layer aspect ratio ≈ 190 at the wall; harder for snappy | **adopt** |
| W4 Adaptive/all-y+ (Spalding, SU2 ADAPTIVE) | Outside ITTC Table 1; adds an unquantified modelling term | — | reject for the gate |

**The spec floor needs a ruling (DR-F2-3).**
1. Bound the wall-function branch by a **feasibility check**: it is offered only when ≥ 15 points with
   y+ ≥ 30 fit inside the estimated turbulent δ over the chord. A blanket retirement for "hydrofoil Re" is
   not claimed, because only Re_c 6e5 was checked.
2. State the y+ floor on the **measured** distribution: y+ at the cell centre, area-weighted p95 ≤ 1 and
   max ≤ 2. Measure it on an A4-stationary field at the case's highest U and α. A 500-iteration field is a
   *mesh screen*, not the gate.
3. State the layer floor as a **coverage rule per region**: ≥ 20 *layers* on ≥ 95 % of wing faces and ≥ 10 on
   100 %, reported separately for LE, main surface, TE 3 % and tip.

**Label (hydrodynamicist condition).** SA-noft2 and SST without LM are fully turbulent, which is also wrong
over a laminar run, and drag will read high (Inferred; Ncrit band 2–4, knowledge doc 06). Every W3 result
carries "fully turbulent; transition not modelled" until an LM case exists.

### 3.3 (b) Layers at the TE and tip — options

| Option | Unattended? | Evidence | Cost | Verdict |
|---|---|---|---|---|
| **L1 snappy, tuned**: absolute `firstAndExpansion` (8 µm, ER 1.2, 20–25 layers), `featureAngle 180`, `layerTerminationAngle -180`, `nOuterIter` > 1, `meshShrinker displacementMotionSolver` as variant 2 | yes | Controls exist (Verified). Round 1 iteration 2 (absolute 0.39 mm, featureAngle 180) fell to 7.6 % coverage, so success on a sharp TE is **unlikely** (Inferred) | 2 variants × ≤ 5 min | **run, with L2** |
| **L2 Analysis geometry with a finite TE at the TE floor setting, and a rounded tip cap** | yes | Gives snappy a TE face to wrap. The TE floor is already a run-key setting (:237). Adds base drag, so it must be labelled (hydrodynamicist) | geometry generator change | **run with L1 — DR-F2-4** |
| L3 cfMesh | yes, once installed | Not in the v2512 app (Verified); needs a separate build and a matrix entry | build + learning, ≥ 1 h | defer |
| **L4 Gmsh 3D layer extrusion → `gmshToFoam`** | yes (scripted `.geo`/API) | Both tools present (Verified). 3D boundary-layer extrusion with a sharp-TE fan is **unproven here** (Inferred) | ≈ 1 h authoring + 2 attempts | **run as probe 2 — DR-F2-5 (matrix)** |
| L5 Product-written structured C-H multi-block hex mesh | yes, by construction | The product owns exact geometry. TE and tip coverage are 100 % by construction, and the ITTC 3×3 determinant becomes computable | a mesher feature, weeks (Estimate) | ADR fallback if L1+L2 and L4 fail |

### 3.4 (c) "Determinant ≥ 0.3" — settled

- **ITTC's measure** (Verified, PDF §2.3.7): "Typically, the 3x3 determinant **for structured grids** should be
  greater than 0.3, as a measure of the Jacobian and associated skewness … a few small cells … no better than
  0.15." It is a structured-grid Jacobian measure.
- **OpenFOAM's `cellDeterminant`** (Verified, `primitiveMeshTools.C:848-946`) is a different quantity:
  `mag(det(Σ_f (S_f/S̄)²))/8` over a cell's internal faces only, with S̄ their mean area. It is "1" for a cube and
  measures "minimum connectivity", per the source comment.
- **Arithmetic** (Inferred from that formula). For an a × a × h hex with all faces internal,
  det/8 = r⁴ · 6⁶/(2+4r)⁶, with r = h/a. That gives r = 0.25 → 0.25 and r = 0.278 → 0.31. So **any hex with an
  aspect ratio above about 3.6 falls below 0.3**. Wall cells, which lose their wall face, score lower still.
  Every boundary-layer cell (aspect ratio 10–1,000) fails by construction, so round 1's 32–35 % is the layer
  share, not a defect.
- **OpenFOAM's own floor** is `minDeterminant 0.001` (Verified, `etc/caseDicts/meshQualityDict:57`,
  `mesh/generation/meshQualityDict.cfg:42`).
- **Recommendation (DR-F2-6).** For OpenFOAM meshes the gate uses `cellDeterminant` ≥ 0.001 alongside the
  existing `checkMesh` measures (positive volumes, face pyramids, non-orthogonality ≤ 70°, skewness ≤ 4).
  "3×3 determinant > 0.3 (≥ 0.15 locally)" stays only for a structured mesh (L5), where the harvester computes
  the Jacobian.

### 3.5 Run list M (stop rule: two variants per route; first route to pass coverage + y+ advances; none → stop and report)

| Id | Run | Expected wall | Basis |
|---|---|---|---|
| M-0 | Generator: finite TE at the floor, rounded tip, AR 8 (no solve) | < 1 min | round-1 generator |
| M-1a/b | snappy L1+L2 at AR 8, two variants, `checkMesh -allGeometry`, coverage by region | ≤ 5 min each | measured 31–63 s snappy, < 2.5 min pipeline; + layers (**Estimate**) |
| M-1y | 500-iteration `simpleFoam` y+ **screen** on the passing variant only (the gate's y+ needs the A4-stationary field, which is SPIKE-03's later solve, not this round) | ≈ 10–15 min | measured ≈ 1.05 s/it at 1.73 M cells on 6 ranks (157 s/150); 2–3 M cells (**Estimate**) |
| M-2a/b | Gmsh L4 at AR 8, `gmshToFoam`, `checkMesh`, coverage | ≤ 10 min each | **Estimate** (Gmsh meshes serially) |
| M-2y | 500-iteration y+ run on the passing variant | ≈ 15 min | as M-1y |
| M-3 | Winner at AR 5 and AR 12, each with its y+ run | ≈ 2 × 20 min | as above |
| — | Windows (WSL2/Docker) | not in round 2 | SPIKE-03 "both OSes" stays unmet (DR-F2-7) |

## 4. Q3 — Security (backend substrate)

### 4.1 Facts from source (Verified by reading; nothing executed)

1. `allowSystemOperations` is a plain static int, read once from the *global* controlDict
   (`dynamicCode.C:44-46`). It gates `#codeStream`/`#calc` (`dynamicCode.C:81-96`, FatalIOError) and the
   `systemCall` function object (`systemCall.C:131-139`, FatalError).
2. The global controlDict comes from `debug::controlDict()` (`debug.C:146-178`):
   - If the **`FOAM_CONTROLDICT`** environment variable is set, its text *is* the dictionary and no file is read.
   - Otherwise the user, group and other files from `findEtcFiles` are merged, user last, so user wins:
     `~/.OpenFOAM/<api>/controlDict` → `$WM_PROJECT_SITE/etc` → `$WM_PROJECT_DIR/etc`.
3. **A case cannot switch it back on** (Verified from source; S-4 proves it live). The int is set once at static
   init, and nothing else writes it (grep; security reviewer). A case's `system/controlDict` `InfoSwitches`
   block goes through `simpleObjectRegistry::setValues` (`TimeIO.C:130`), which skips unregistered names
   ("(unregistered)", `simpleObjectRegistry.C:57-67`). This matches the observed `-info-switch` result.
4. **The round-1 `assume:` "`FOAM_CONFIG_ETC` gives the same effect" is false for applications** (Inferred from
   source). It appears only in the shell set-up (`etc/config.sh/setup:165-177`). The C++ lookup in `etcFiles.C`
   has no reference to it.
5. **Not covered by the gate.** `dlLibraryTable.C` never consults `allowSystemOperations`, so library loads have
   no gate in OpenFOAM.
   - `libs` (old name `functionObjectLibs`, `functionObject.C:94`) loads an arbitrary shared library, with path
     expansion.
   - It is read from controlDict, function objects, fvOptions, motionSolver and renumber (security reviewer:
     `Time.C:478`, `fvOption.C:96`, `motionSolver.C:119`, `renumberMethod.C:64`).
   - `#include`, `#includeEtc` and `#includeFunc` pull in text a case-level lint never sees.

   The gate's absence is Verified by grep; that a load runs load-time code is standard `dlopen` behaviour
   (Inferred).
6. The global `etc/controlDict` is 37,375 bytes and carries `OptimisationSwitches` and `DimensionSets`
   (Verified). A `FOAM_CONTROLDICT` pin must carry the whole file, not one switch.
7. **The inherited environment is a trust boundary** (security reviewer, Verified).
   - An inherited `FOAM_CONTROLDICT` overrides every file, product HOME included (`debug.C:148-154`).
   - `WM_PROJECT_SITE` and `WM_PROJECT_DIR` move the etc lookup (`etcFiles.C:137,181`). `BASH_ENV`, `DYLD_*`
     and `prefs.sh` (found via `WM_PROJECT_SITE`) all run before the solver.
   - The banner prints once, from the master rank, before the case is read (`argList.C:2187`). It is evidence
     after the fact, not a gate: killing the run after reading it races the solver.
   - `dynamicCode/` is created at the case root (`dynamicCode.C:293`, `codeRoot_ = envGlobalPath()/…`), never
     under `processor*` (Verified).
8. **The launcher keeps argv intact** (Verified, `etc/openfoam:355-361`). `openfoam <app> <args…>` sources the
   environment and runs `exec "$@"`. The `-c` form runs `exec bash -c "$@"` (`:330`), a shell string.

### 4.2 Ship and pin — options

| Option | Evidence | Verdict |
|---|---|---|
| **P0 Environment from an allow-list** (`env -i` plus named variables only; no inherited `FOAM_*`, `WM_PROJECT_SITE`, `BASH_ENV`, `DYLD_*`) | Required by fact 7; without it P1 and P2 can be overridden | **adopt (precondition)** |
| **P1 Product-owned `HOME`** holding `.OpenFOAM/2512/controlDict` with `InfoSwitches { allowSystemOperations 0; }` | Observed working in round 1 ("Disallowing"). The user's real `~/.OpenFOAM` is invisible under the product HOME | **adopt** |
| **P2 `FOAM_CONTROLDICT` set explicitly** to the pinned etc/controlDict with the switch flipped | Strongest by source: no file merge at all. Needs the full 37 kB text in the environment | **adopt with P1** (defence in depth); S-5 proves it |
| P3 `WM_PROJECT_SITE` | A user-level file still wins | reject |
| P4 `-info-switch` | Observed not to work | reject |

**Pin.**
- The product ships the controlDict as bundled decision data. Its hash goes in the build manifest, as the A8.5
  row for bundled data already requires.
- At each launch the product writes it into the run's HOME from the bundle, sets `FOAM_CONTROLDICT` from the same
  bytes, and re-verifies the hash.
- **The gate comes before the solver.** The mesh gate's `checkMesh`, which already runs before any solve
  (A5.10), is the pre-flight process. Under the same environment it must print `allowSystemOperations :
  Disallowing user-supplied system call operations`. Round 1 observed `checkMesh` printing this banner; it
  cannot come from `foamDictionary`, which calls `noBanner` (Verified, `foamDictionary.C`). Without
  it the solver never starts, and the run is Failed ("substrate not hardened"). The solver's own banner is
  re-checked as a record.
- **The lint is an allow-list, not a deny-list.**
  - Each generated dictionary may contain only the keywords its typed template emits.
  - Refused: any `#`-directive (`#include*`, `#codeStream`, `#calc`, `#eval`), any regex keyword, `libs` and
    `functionObjectLibs`, `coded*` and `systemCall`.
  - The lint reads the **raw text** and refuses every `#` directive, so no expansion is needed. If one is ever
    needed, `foamDictionary -expand` parses directives and so runs only under P0+P1/P2.
- **Order is fixed: lint → `checkMesh` (pre-flight banner) → solver.** `checkMesh` builds `Time`, which loads
  `libs` from controlDict without the gate (`Time.C:478`), and it reads the case after its banner prints. So
  the lint must pass before any OpenFOAM process touches the case.
- **Launch by argv**: `openfoam <app> <args…>` (fact 8), never `-c`.

### 4.3 Live proof — probe set S (authored in round 2, run by the operator with `!`)

The harness refused an agent-run `#codeStream` probe once. So round 2 writes the probe set as inert text plus
one script, `cases/tools/security-probe.sh`. The operator runs it: `! bash cases/tools/security-probe.sh`. The
script writes receipts to `runs/<ts>-security-probe/` and copies them to `docs/proof/spike-03/security/`. Each
probe is a **fresh** cavity copy with one change. The code inside every probe is benign: `#codeStream` emits the
constant `0.5` as `endTime`, and `systemCall` runs `/usr/bin/true`. The operator runs the script only after its
sha256 matches the one recorded in the review of the round-2 commit. Receipts redact the username and home path.

| Id | Probe | Environment | Expected (pass) | Observable |
|---|---|---|---|---|
| S-1 | `#codeStream` in `system/controlDict` (positive control; DR-F2-8) | app default | compiles and runs (shows the probe can execute) | banner "Allowing"; `dynamicCode/` created |
| S-2 | same | P0+P1, serial **and** `mpirun -np 2` | refused | FatalIOError "…case-supplied code may have been disabled…" from every rank (per-rank output collected); exit ≠ 0; no `<case>/dynamicCode` |
| S-3 | `systemCall` function object | P0+P1 | refused | FatalError "Executing user-supplied system calls…"; exit ≠ 0 |
| S-4 | case `InfoSwitches { allowSystemOperations 1; }` + `#codeStream` | P0+P1 | still refused | "(unregistered)" + S-2 error |
| S-5 | S-2 under P0+P2 (`FOAM_CONTROLDICT` set), serial and `mpirun -np 2` | P0+P2 | refused on every rank | pre-flight "Disallowing"; per-rank FatalIOError; exit ≠ 0; no `<case>/dynamicCode` |
| S-6 | `libs ("libcfdwNoSuchLib.dylib")` | P0+P1 | the load is **attempted** (documents the ungated path) | warning naming the library; motivates the allow-list lint |
| S-7 | Inherited `FOAM_CONTROLDICT` carrying `allowSystemOperations 1`, then the product launcher | hostile parent environment | P0 strips it; pre-flight reads "Disallowing"; S-2 refusal | pre-flight banner; exit ≠ 0 |
| S-8 | Lint: case with `#include "x"`, a regex keyword and `functionObjectLibs` | lint only (no OpenFOAM process) | refused before launch | lint error naming each keyword |

Pass = S-2..S-5, S-7 and S-8 refused, and S-1 executes (or is skipped by the operator, with the refusal text as
mechanism evidence). Any "Allowing" under the product launcher **stops all round-2 runs**.

## 5. Resources and budget

Every run uses `cases/tools/of-run.sh` (≤ 6 cores, nice 10, waits while the 1-min load > 10 or `join.lock`
exists, one job at a time). SU2 runs need an equivalent `su2-run.sh` under the same rules.

**Round-2 tooling change:** wrap each command in `/usr/bin/time -l` so **peak RSS is recorded**. Round 1 left it
"Not recorded", and A5.10 status needs it.

| Block | Runs | Compute (Estimate unless measured) |
|---|---|---|
| S security | S-1..S-6 | ≈ 5 min |
| C convergence (core) | C-1, C-2, C-3, C-4 | ≈ 4 + 11 + 50 + 20 = **85 min** |
| M meshing | M-0..M-3 (two routes, winner at AR 5/12) | ≈ **100 min** |
| Core total | | 190 min + 30 % waits (round 1: 9 of 31 min was waiting) ≈ **4.1 h** |
| SU2 referee (DR-F2-2) | setup + R-1..R-3 | ≈ 0–40 + 55 min, + 30 % ≈ **2 h** |
| C-5 (conditional) | B2 on three grids | ≈ 65 min, only on an acceptance failure |

Agent authoring (case files, generator flags, probe script, harvester A4 check) is extra; **Estimate ≈ 1.5 h**.
Round-wide stop rule: the repair cap of 2 per question; a block that exceeds its budget by 25 % stops and
reports; a security failure stops everything.

## 6. What the backend ADR can and cannot decide after round 2

**Can decide (macOS arm64 only):**
- the substrate and pin: native app by DMG hash and build;
- the controlDict pin mechanism (P1 or P2) and the banner check;
- the iterative criterion A4;
- the SA numerics for the OpenFOAM tier;
- whether SPIKE-04 passes on OpenFOAM, with GCI;
- the mesh route for AR 5/8/12, if one passes;
- the mesh-gate definitions (DR-F2-3, DR-F2-6);
- whether SU2 enters as a referee, if DR-F2-2 ran.

**Cannot decide:**
- Windows (WSL2 argv rule, Docker Desktop); no Windows host in this round.
- Image-digest pins; Docker was never used.
- **v2512 vs v2606.** The spec says v2606 is untagged (A5.10), but gerlero/openfoam-app has shipped
  `openfoam2606-app-arm64.zip` since v2.2.0 (2026-06-29; Verified, GitHub releases). Round 2 stays on v2512 so
  results stay comparable; the ADR needs a v2606 re-smoke.
- SU2 on macOS beyond Rosetta's end, unless a native arm64 build is pinned.
- Free-surface and cavitation physics; not exercised.
- SPIKE-03's "both OSes" clause.

## 7. Operator's `of` function (outside the repo; do not edit `~/.zshrc` from here)

Today `~/.zshrc:51`, inside the `mac-setup managed` block, reads
`of() { source "$(brew --prefix)/share/openfoam/etc/bashrc" 2>/dev/null && … }`. It sources a missing file and
the `2>/dev/null` hides the failure (Verified, round 1 and the file).

Fix it through the mac-setup source of that managed block, then re-run the phase:

```zsh
of() { /Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam "$@"; }
```

- With no arguments it opens an OpenFOAM bash session; the app README documents this launcher.
- `of -c "<cmd>"` runs one command, the form round 1 used in `of-run.sh` (Verified).
- `assume:` the launcher's `-c` pass-through stays stable across app updates. *Confirm:* `of -c 'echo $WM_PROJECT_VERSION'`
  prints `v2512`. *If false:* `of-run.sh` breaks the same way and fails loudly.

## 8. Findings outside this plan's scope (recorded, not fixed)

- **F-1 (defect-class candidate).** The knowledge base faithfully copies ITTC Eq. (10) with `ln`, where the
  Prandtl–Schlichting form uses log10, so first-cell estimates are ≈ 2.9× too thick. Class: "a standard's
  typo carried verbatim into a derived formula". Control idea: a unit test that evaluates every correlation in
  `data-and-constants.md` against an independent reference value.
- **F-2.** The spec's "v2606 untagged" statement (A5.10) is contradicted by the published v2606 app.
- **F-3.** `of-run.sh` launches with `openfoam -c "$*"`, which becomes `bash -c` (`etc/openfoam:330`). That is
  acceptable for spike tooling, but the product rule is argv arrays (A8.5). The product launcher uses
  `openfoam <app> <args…>` → `exec "$@"` (`:361`).
- **F-4.** `schemas/cfd-case.schema.json` needs round-2 members: `purpose.spike` lacks a round-2 value,
  `mesh.generator` lacks a value for a product-written mesh, and `numerics` has no structured A4 fields.
  These are a schema change for the round-2 track, not this plan.

## 9. Reviewer verdicts

The author (CFD numerical verification) does not clear its own veto. Both reviewers ran in Adversary mode and
read only; neither ran a solver or a probe.

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| Hydrofoil hydrodynamicist | PASS WITH CONDITIONS | cleared (nothing is displayed or claimed as validated) | δ argument restated over the chord with node vs centre counting (§3.1.3); layers vs points inside δ (§3.1.3, DR-F2-3); "fully turbulent; transition not modelled" label (§3.2); wall-function feasibility check instead of blanket retirement (DR-F2-3); y+ at cell centre, area-weighted, on an A4-stationary field, so 500 iterations is a screen only (§3.2, M-1y); blunt-TE records and base-drag band, shedding as physics (DR-F2-4); U_Δ ≥ L5 numerical uncertainty and "code-to-code verification" wording (§2.4); F-1 line reference corrected |
| Security & identity architect | PASS WITH CONDITIONS. Pass 1: veto not cleared. Re-check (repair cycle 1 of 2): **hard veto cleared, no blockers**; its two remaining conditions (lint before `checkMesh`; raw-text lint with expansion only under P0+P1/P2) are applied in §4.2 | cleared on re-check | inherited environment named as a boundary and P0 `env -i` allow-list added (§4.1.7, §4.2); P2 adopted with P1; pre-flight banner gate before the solver (§4.2); S-5 checks `<case>/dynamicCode` and per-rank errors, and S-2 also runs under `mpirun` (§4.3); S-7 (inherited `FOAM_CONTROLDICT`) and S-8 (lint bypass) added; the lint is an allow-list on raw text, run before any OpenFOAM process, covering `functionObjectLibs` and all `#`-directives (§4.2); argv launch form (fact 8, F-3); probe-script hash and username redaction (§4.3); case-InfoSwitches claim upgraded to Verified (§4.1.3) |

**Residual risks (not reviewed or not closable here).**
- OpenFOAM never gates the `libs` load path, so only the lint and the typed templates stop it (security).
- Not reviewed: free surface, cavitation, and the 3D tip vortex (hydrodynamicist).
- The GCI's numerical trustworthiness is this plan's own lens (CFD verification), so it is author-held until
  round 2's evidence exists.

## 10. Open decisions for the operator

| Id | Decision | Recommendation |
|---|---|---|
| DR-F2-1 | Adopt A4 (ITTC 3-order drop + Cl/Cd stationarity + clean window) as the "residual criterion" in A5.10/A7, and amend the spec wording | yes |
| DR-F2-2 | SU2 referee leg: (a) install Rosetta 2 and use the x86_64 release binary (ends after macOS 27); (b) build SU2 v8.5.0 natively for arm64, pinned by tag and hash; (c) skip, and measure Δcomp with `rhoSimpleFoam` | (b) if the ≈ 2 h is acceptable, else (c) |
| DR-F2-3 | Mesh floor: bound the wall-function branch by a feasibility check (15 points at y+ ≥ 30 inside δ); y+ at the cell centre on an A4-stationary field, area-weighted p95 ≤ 1, max ≤ 2; layers as per-region coverage | yes |
| DR-F2-4 | Analysis geometry may apply a finite TE at the TE floor setting, plus a rounded tip, as a declared run-key setting. Record t_TE; truncate vs thicken (truncating changes c and S_ref); design vs analysis S and b; and a base-drag band beside Cd (hydrodynamicist estimate ≈ 2–5 % for t/c 0.25–0.5 %, Flagged recall). Label "TE blunted for analysis". If a blunt TE sheds vortices (Re_t ≈ 1,500) and A4 fails, that is recorded as physics, not as a numerics retune | yes |
| DR-F2-5 | Admit Gmsh (GPL, process-only) to the backend matrix as a mesher for the probe | yes, probe only |
| DR-F2-6 | Replace "determinant > 0.3" with `cellDeterminant` ≥ 0.001 for OpenFOAM meshes; keep 0.3 for structured meshes only | yes |
| DR-F2-7 | Windows leg of SPIKE-03: provide a host, or accept a macOS-only round 2 with the clause left open | operator |
| DR-F2-8 | Include S-1 (a positive control that executes benign case code under the app default) | yes, operator-run |
| DR-F2-9 | TMR + GCI as a pin-change fixture (run when the backend pin changes), not every readiness run; readiness keeps the cavity smoke (:1290, A5.10) | yes |
