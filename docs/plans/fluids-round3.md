---
id: plan-fluids-round3
title: Fluids round 3 — bring the Gmsh wing mesh inside the gate (SPIKE-03) and find a monotone TMR grid family (SPIKE-04)
type: doc
status: in-review
owner: "@fluids-f1"
tags: [plan, spike-03, spike-04, openfoam, gmsh, mesh-gate, gci, tmr, round-3]
links:
  - {to: adr-0012-openfoam-backend-macos, rel: depends-on}
  - {to: proof-spike-03-round2, rel: depends-on}
  - {to: proof-spike-04-round2, rel: depends-on}
  - {to: plan-fluids-round2, rel: supersedes}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-03
summary: >-
  Round 3 plan, documents only, for the two round-2 NO-GOs. Mesh: locate the 811 faces above 70 degrees on the
  Gmsh AR 8 mesh with checkMesh sets (no new app), then two unattended variants aimed at the measured cluster (TE arc
  resolution, tip poles, the prism-top/tet size jump), the first height cut to 6.0 um, then AR 5 and 12. GCI: a cheap
  L6 test of the one scheme that acts on L6 only (the limited laplacian), then one numerics cycle on L6/L5/L4 with
  the iteration band held at 1 % of the grid change; L3 (4-10 h) only by ruling. Core budget about 4.5 h wall plus
  1.5 h authoring.
review-suggested:
  - { by: adr-0012-openfoam-backend-macos, on: 2026-10-04, reason: "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)" }
---

# Fluids round 3 — plan (documents only)

**Goal.** Plan how round 3 closes the two round-2 NO-GOs, with evidence, cost and stop rules:

- SPIKE-03 meshing: the Gmsh route at AR 8, then AR 5 and AR 12;
- SPIKE-04: a GCI from a monotone grid family.

**Done when:** each question has its facts, hypotheses, knobs, a run list with expected wall times from round-2
measurements, stop rules and a budget, and each open decision has a recommendation.
**Not in scope:**

- any run;
- snappy (it reached 20 layers on 0 % of faces, and round 3 does not revisit it);
- Windows, v2606 and Docker (ADR-0012);
- the security probes (S-1..S-8 ran 2026-10-04; Ruling 68 keeps only S-1 and S-2 serial, as the pin-change check).

**Tier:** T1. **Lead:** CFD numerical verification. **Reviewer:** the hydrofoil hydrodynamicist.

Labels: **Verified** = observed in a receipt, log, source line or reference file; **Inferred** = reasoned from
Verified facts; **Estimate** = a number from a stated model; **Flagged** = recall not re-opened this session.
`assume:` lines carry the belief, its check and its blast radius.

## 0. Grounding (opened, not recalled)

- [ADR-0012](../adr/0012-openfoam-backend-macos.md) (D1–D7, DR-F3-1).
- [SPIKE-03 round 2](../proof/spike-03/verdict-round2.md) and its M-2c receipts:
  `receipts/20261003T224952Z-spike03r2-m2c-ar8/` (`log.checkMesh`, `generator.txt`, `times.txt`).
- [SPIKE-04 round 2](../proof/spike-04/verdict-round2.md);
  [`cfl3d_results_sa_nopv_withN.dat`](../proof/spike-04/reference/cfl3d_results_sa_nopv_withN.dat) (all six
  Family II levels).
- [`make-wing-gmsh.py`](../../cases/tools/make-wing-gmsh.py), `cases/spike03r2-m2c-ar8.yaml`,
  `cases/spike04r2-c3-l4.yaml`, [`faceset-location.py`](../../cases/tools/faceset-location.py).
- Gmsh 4.15.2 API docstring (`lib/gmsh.py:6214`) and shipped examples (`examples/api/naca_boundary_layer_*.py`,
  `examples/boolean/number_of_tets.geo`).
- Arithmetic: a scratch script (not committed) over the reference file and the round-2 rates. Its outputs are
  quoted below.

## 1. Summary

| Question | Plan | Core cost (wall) |
|---|---|---|
| Mesh (SPIKE-03) | R3-M0 locates the bad faces on the existing M-2c mesh. R3-M1a/b run two variants aimed at the measured cluster. R3-M2 rebuilds the winner at h1 6.0 µm and runs a y+ screen. R3-M3 runs AR 5 and AR 12 | ≈ 90 min compute, ≈ 2.0 h with waits |
| GCI (SPIKE-04) | R3-G0 tests the L6-only limiter on L6 (≤ 5 min). Then one numerics cycle (G1a or G1b) on L6/L5/L4, with clause 5 enforced by extending the finer run. A second cycle runs only if the first meets A4 but is still non-monotone. L3 needs a ruling | ≈ 2.0 h compute, ≈ 2.5 h with waits |
| Total | one job at a time; +30 % for load and join-lock waits | **≈ 4.5 h wall core**, + ≈ 1.5 h authoring (**Estimate**) |
| Conditional | R3-G2 L3 triplet (DR-F3-3) · R3-M4 AR 8 3-D A4 solve for the y+ gate (DR-F3-4) | 4–10 h · 2.4–7 h (**Estimate**) |

## 2. Mesh — the Gmsh route at AR 8 (SPIKE-03)

### 2.1 Facts (M-2c, Verified from receipts)

- 3,207,020 cells: 2,515,440 prisms (20 × 125,772 wing triangles) and 691,580 tets. Layer coverage is **100 %** by
  count. First height 8 µm, ER 1.2, stack 1.4935 mm.
- checkMesh: max non-orthogonality **86.26°**, 811 faces > 70°, with "Non-orthogonality check OK". checkMesh does
  not fail non-orthogonality; the **≤ 70° limit is the gate's** (ADR-0012 D6).
- The checkMesh failures are determinant (1,128,345 cells < 0.001, min 0) and **1 face with interpolation weight
  0.046** (< 0.05).
- Max skewness 1.74, max aspect ratio 118, min volume 9.0e-14 m³, face pyramids OK, concave OK. There are **256
  cells with two internal faces** (`twoInternalFacesCells`).
- Gmsh log:
  - **"Skipping boundary layer extrusion of degenerate curve 19 / 22 / 29 / 31"**;
  - BOPAlgo "IntersectionOfPairOfShapesFailed" warnings in the fragment;
  - 22 nodes not inserted;
  - worst tet quality 0.0249 after optimisation;
  - Gmsh is effectively single-threaded (20.2 s user for 20.1 s real).
- Cost: Gmsh 20 s, gmshToFoam 7 s, checkMesh **17.5 s at 3.34 GB RSS**, 500-iteration y+ screen 930 s on 6 ranks.
- Sizes: `lc_le = lc_mid = lc_te = lc_near = 1.5 mm`, `near_dist 2 mm`, Delaunay 3-D (HXT failed in M-2a). Root
  symmetry tolerance 1e-4 m.
- **Where the 811 faces are: not measured.** checkMesh wrote `nonOrthoFaces` into the run directory. That directory
  is git-ignored but still on disk: `CFD-Workbench-spike-fluids-round2/runs/20261003T224952Z-spike03r2-m2c-ar8`.
  `faceset-location.py` reads only ASCII quad meshes (the TMR C-grid), and M-2c is binary with prisms and tets.

### 2.2 Likely causes (Inferred, ranked; R3-M0 decides)

1. **The TE arc is under-resolved.** The TE base is a half circle of radius t_TE/2 = 0.25 mm, so the arc is
   π × 0.25 ≈ **0.79 mm** long. It is meshed at 1.5 mm, which **may** give only one segment. Gmsh's
   `Mesh.MinimumCirclePoints` can raise that count (CHANGELOG; its default is Flagged), so R3-M0 reads the segment
   count from the mesh. The reading below holds only if the arc has about one segment. The extrusion normals at the
   arc ends average the surface normal (≈ ±y) and a one-facet arc normal (≈ +x). So the TE prisms shear through
   ≈ 45° and fan through 180° over less than 1 mm, while the stack is 1.49 mm, 3× the TE thickness. The surface-to-arc
   kink itself is small, about 8°. That is arithmetic on the generator's `yt`: the surface slope at x = c is −0.143
   and the arc tangent there is along x. The shear and fan reading is Inferred.
2. **Tip poles.** The round tip is revolved about the chord line. Gmsh skipped boundary-layer extrusion on four
   degenerate curves (Verified), which are the pole edges. Prisms in the fan around a pole collapse toward wedges.
   They are candidates for the 256 two-internal-face cells, the determinant minimum of 0 and high non-orthogonality.
3. **The size jump from prism top to tet.** The last layer is 8 µm × 1.2¹⁹ = **0.256 mm** thick (Verified
   arithmetic). The tets above it are about 1.5 mm, a jump of about 6×. Tet optimisation acts on tets only. Its
   worst quality, 0.025, may sit at this interface.
4. **The root junction** (lowest risk). The wing is prismatic, so the root normals lie in the symmetry plane. The
   side faces of the layer at z = 0 are found by a 1e-4 m tolerance.

### 2.3 Unattended knobs (all scripted in `make-wing-gmsh.py`; no human step)

| Knob | For cause | Evidence it exists |
|---|---|---|
| K1 Size at the TE-arc points about π·r_TE/8 ≈ 0.1 mm (≥ 8 arc segments), with a smaller `lc_te` over the last 3 % of chord | 1 | `setSize` per point is already used (Verified, :83-85) |
| K2 Per-node normals or height scale from a post-processing **view** passed as `extrudeBoundaryLayer(..., viewIndex=v)`: "use the corresponding view to either specify the normals (vector field) or scale the normals (scalar)". Smoothed normals at the TE arc ends; a height scale < 1 at the tip poles. Twenty layers stay, so coverage is unchanged | 1, 2 | Verified, `gmsh.py:6214-6227` docstring |
| K3 `Mesh.OptimizeNetgen 1` plus `Mesh.Smoothing` on the tet core; `lc_near` 0.75 mm inside `near_dist` | 3 | the options appear in shipped examples (Verified); their effect here is unmeasured |
| K4 A tip without poles, for example a short flat at each pole before the revolve | 2 | a geometry change: it must stay inside DR-F2-4 "rounded tip, declared and labelled" (DR-F3-5) |
| Not available | — | 3-D fans: `BoundaryLayerFanElements` exists only in the 2-D BoundaryLayer field (Verified, `naca_boundary_layer_2d.py:129`; the 3-D example uses `extrudeBoundaryLayer` without fans) |

`assume:` v2512 `checkMesh` accepts `-writeSets vtk` and `-writeFields '(nonOrthoAngle cellDeterminant)'`.
*Check:* `checkMesh -help` through the launcher, as the first R3-M0 step. *If false:* R3-M0 extends
`faceset-location.py` to read the binary polyMesh and the `nonOrthoFaces` set. That is about 30 min of authoring, and
it adds no OpenFOAM app to the harness's app list.

### 2.4 Run list M (stop rule: two variants; the first to pass the gate advances; none → stop and report)

| Id | Run | Expected wall | Basis |
|---|---|---|---|
| R3-M0 | On the existing M-2c mesh, or a re-generated M-2c (20 s + 7 s; record whether `msh_sha256` repeats): `checkMesh -allGeometry -writeSets vtk -writeFields '(nonOrthoAngle cellDeterminant)'`. A locator classifies the 811 faces, the 1 low-weight face and the 256 two-internal-face cells by region: TE arc ±1 mm, tip within 2 mm of a pole, root z < 2 mm, prism-top band (wall distance 1.2–1.8 mm), main. It also splits determinant < 0.001 into prisms and tets (evidence for DR-F3-1 option B) | ≤ 10 min | checkMesh 17.5 s (measured) + writing + script |
| R3-M1a | The variant aimed at the largest cluster, set **before** the run from this table: TE → K1 + K2 (normals); tip → K2 (scale); interface → K3 | ≤ 5 min | Gmsh 20 s (measured) × ≤ 3 for the finer TE (**Estimate**) + 7 s + 18 s + coverage |
| R3-M1b | Repair 1: the second cluster's knob added to M1a | ≤ 5 min | as above |
| R3-M2 | The passing variant rebuilt at **h1 6.0 µm**, re-gated, then the 500-iteration y+ screen (labelled a screen). Since y+ ∝ h and the screen read p95 1.26 at 8 µm, 6.35 µm would project p95 = 1.00, which leaves no margin. 6.0 µm projects p95 0.95 and max 1.72 (**Estimate**). The last layer shrinks to 0.19 mm, so the prism-top to tet jump grows from 5.9× to 7.8×; **K3 (`lc_near` 0.75 mm) is applied with the h1 cut** | ≈ 21 min | gate 1 min + screen 930 s (measured) |
| R3-M3 | Winner frozen, AR 5 and AR 12: mesh, gate, coverage per region, y+ screen | ≈ 50 min | AR 5 ≈ 2.0 M cells and AR 12 ≈ 4.8 M by span (**Estimate**). Screens ≈ 10 and ≈ 23 min, scaled from 930 s. checkMesh at AR 12 ≈ 5 GB RSS (**Estimate**, 3.34 GB × 1.5) |
| R3-M4 | *(conditional, DR-F3-4)* AR 8 3-D A4 solve on the winner: **the y+ gate** (p95 ≤ 1, max ≤ 2 on a stationary field) | 2.4–7 h | 1.7 s/it measured in the M-2y screen × 5k–15k iterations (**Estimate**; no 3-D A4 prior). Uses the D4 numerics or declares the deviation; the round-2 wing cases used `limitedLinear 1` for nuTilda. The TE arc gets **its own force patch**, so base drag is measured, not estimated |

**Pass (each AR).** Gate measures:

- max non-orthogonality ≤ 70°;
- skewness ≤ 4 internal and ≤ 20 boundary;
- all volumes positive and face pyramids OK;
- face weight ≥ 0.05 (if DR-F3-1 is ruled A);
- otherwise, DR-F2-6 as ruled, which fails by construction and is reported as such.

Layer coverage: ≥ 20 layers on ≥ 95 % of faces and ≥ 10 on 100 %, per region. The y+ **screen** is reported; the
y+ **gate** needs R3-M4. **A pass holds only at the stated operating point:** U 5 m/s, α 4°, ν 1.0e-6 m²/s (Re_c
6.0e5). A higher speed needs a new y+ check.

**Physics checks on R3-M2 (hydrodynamicist).**

- At 6.0 µm the 20-layer stack is **1.12 mm**. That is 0.95 δ at x/c 0.3 (δ ≈ 1.18 mm) and 36 % of δ_TE (≈ 3.1 mm)
  (**Estimate**, round 2; at 6.35 µm the stack would be 1.19 mm). Note that round-2 verdict :163 pairs 1.12 mm with
  6.35 µm; that is an arithmetic slip, recorded here and not edited there. R3-M2 reports, per region, the ratio of
  stack to δ and the number of cells across δ. If K2 thins the stack anywhere, it reports the same numbers there.
- The LE y+ peak (max 2.29) is Inferred to come from skin friction above the flat-plate value. If the peak stays above
  2 at 6.0 µm, the next variant is a local height scale at the LE (K2). It is not a global cut.
- Every result keeps "TE blunted for analysis", "fully turbulent; transition not modelled" and "Cd is an upper
  bound vs free transition" (spec A5 RANS row). The TE shape stays
  the half circle, so round-3 TE results compare with M-2c, not with snappy.

## 3. GCI — a monotone grid family on TMR (SPIKE-04)

### 3.1 Facts

- OpenFOAM, Family II, round 2 (Verified):

  | Pair | Cl change | Cd change | Ratio R (Cl, Cd) |
  |---|---|---|---|
  | L6 → L5 | ε32 = −2.637e-2 | +2.906e-3 | — |
  | L5 → L4 | ε21 = +2.76e-4 | −1.53e-4 | −0.0105, −0.0526: **oscillatory** |

  Clause 5 fails on Cl for L5/L4: U_I is 9.8e-6, against an allowed 2.8e-6.
- **CFL3D, same family, same first-order turbulence advection** (Verified from the reference file; ratios computed
  with r = 2):

  | Triplet | Cl: R · p | Cd: R · p |
  |---|---|---|
  | L6/L5/L4 | **0.259 · 1.95** | 0.215 · 2.21 |
  | L5/L4/L3 | 0.836 · **0.26** | 0.197 · 2.34 |
  | L4/L3/L2 | 0.888 · 0.17 | 0.118 · 3.08 |
  | L3/L2/L1 | 0.384 · 1.38 | **−0.257, oscillatory** |

- So **L6 is inside a monotone range for CFL3D**, and the anomaly is in OpenFOAM's L6 result (**Inferred**). After the L5 Δcomp,
  OpenFOAM's Cl is −1.74 % from CFL3D at L6, against +0.28 % at L5 and +0.15 % at L4 (Verified, round 2). Δcomp was
  measured on L5 only; applying it to L6 and L4 is an assumption. On finer
  levels even CFL3D converges slowly in Cl (p 0.2–0.3 on L5/L4/L3), and its Cd oscillates at the finest triplet.
  **A monotone finer triplet is not guaranteed by refinement alone** (Inferred).
- **One scheme term acts on L6 only.** L6 has 206 far-wake faces above 70° (|y| < 1.6e-5, x 1.02–443 c). L5's
  maximum is 57.9° and L4's is 31.6° (Verified, checkMesh logs). `limited corrected 0.5` is applied everywhere.
  Round 1 states it does not act below 70°. That claim has not been re-checked against `limitedSnGrad`, and it may
  depend on the field gradient as well as the angle (**Inferred**). Either way, the family is not discretised the same
  way on every level.
- Measured cost per iteration (6 ranks unless stated):

  | Grid | Rate | Rate per cell-rank | Iterations to A4 |
  |---|---|---|---|
  | L6 (2 ranks) | 7.8 ms | 1.09 µs | 6,375 |
  | L5 (4 ranks) | 18.5 ms | 1.29 µs | 17,042 |
  | L4 | 90 ms | 2.35 µs | 41,055 |

  Iterations grow 2.67× and then 2.41× per level.
- Cost of the finer levels (**Estimate**, the L4 rate scaled by cells, 6 ranks):

  | Level | Cells | Rate | Iterations | Wall |
  |---|---|---|---|---|
  | **L3** | 917,504 | ≈ 360 ms/it | 41k–99k | **4.1–9.9 h** |
  | **L2** | 3.67 M | ≈ 1.44 s/it | — | **16–95 h**: not on this host |

### 3.2 Hypotheses (pre-registered; tested cheapest first)

- **H1:** the limited laplacian/snGrad on L6's > 70° faces makes L6 a different discretisation, so L6 is the outlier.
- **H2:** first-order nuTilda advection raises the grid error and its non-monotonicity (TMR's warning, round 2).
- **H3:** OpenFOAM's L6 is outside the asymptotic range for these numerics, whatever the scheme. Only a finer
  triplet (L3) can test H3.

### 3.3 Iteration band held below the grid change

Clause 5 (U_I ≤ 0.01 |ε| for each pair, Cl and Cd) is the admission test:

1. Each grid first runs to the A4 caps.
2. Compute the pair's ε.
3. Where clause 5 fails, **extend the finer run** from its own last time, with no setting changed, until clause 5
   holds or a cap of +20,000 iterations. On L4 that is ≤ 30 min. C-3's decay suggests 10–15k iterations are enough
   for 3e-6, about 15–22 min (**Estimate**).
4. A pair with |ε| < 10 × the larger U_I is recorded as "grid change within iterative noise". No order is computed
   from it.

### 3.4 Run list G (stop rule: G0 is a diagnostic; at most 2 numerics cycles; the order is fixed)

| Id | Run | Expected wall | Reading, stated before the run |
|---|---|---|---|
| R3-G0 | L6, D4 numerics but `laplacian Gauss linear corrected` and `snGrad corrected` (no limiter), A4 | ≤ 5 min (cap 40k × 7.8 ms) | **H1 supported** if \|Cl − 1.051704\| > 2.8e-3 (10 × \|ε21\|) and Cl moves toward the L5/L4 level. **H1 rejected** if the change is < 2.8e-4. **Untestable this way** if A4 fails (unlimited correction on > 70° faces). Then go to G1b |
| R3-G1a | *(cycle 1 if H1)* L5 and L4 with the same unlimited schemes, restarted from the C-2 and C-3 final fields (copied in at generation, so the harness's case record holds them), A4 + §3.3 | ≈ 50–95 min | L5 ≤ 17k × 18.5 ms ≈ 5 min; L4 10k–41k × 90 ms ≈ 15–62 min; extension ≤ 30 min |
| R3-G1b | *(cycle 1 if not H1)* `bounded Gauss limitedLinear 1` for nuTilda at **relaxation 0.7** on L6/L5/L4. Never run: C-1 ran TVD at 0.9. This matches FUN3D's 2nd-order numerics | ≈ 70–90 min | measured rates × round-2 iteration counts (**Estimate**). Worst case at the caps ≈ 2.9 h, so the 25 % overrun stop fires at 113 min |
| Cycle 2 | the other of G1a/G1b, only if cycle 1 meets A4 but is non-monotone | ≈ 70–95 min | as above |
| R3-G2 | *(conditional, DR-F3-3)* add L3 for the L5/L4/L3 triplet with the best numerics | 4.1–9.9 h + ≈ 1 h fetch and generation | §3.1 estimate. Initialise from freestream: `mapFields` is not on the harness's app list (DR-F3-2; the security reason no longer holds, Ruling 68) |

**GCI acceptance (unchanged from round 2).** All three grids are admitted under A4 with clause 5, and 0 < R < 1 on
Cl and Cd. The result is p and GCI_fine per Celik et al. (Fs 1.25). The TMR comparison stays code-to-code
verification. It stays "not computable" while U_Δ is unstated. A failed acceptance is a recorded finding, not a
retune.

**The GCI procedure is also run on CFL3D's L6/L5/L4 and reported beside the result** (hydrodynamicist condition,
Verified arithmetic). On that family it gives an absolute Cl GCI of 5.0e-4, while the true L4 error against L1 is
2.15e-3. So it under-states the error 4.3×, even with monotone convergence and p 1.95. **Any OpenFOAM GCI from
L6/L5/L4 is labelled "pre-asymptotic; not shown conservative".** Monotone convergence on this triplet is necessary
but not sufficient (H3).

## 4. Resources, stop rules and budget

- **Resources:** every OpenFOAM process goes through the spike harness (`cases/tools/of-run.sh`):
  - *Ruling 68:* ADR-0012 D2 is now the six short product rules; the case record, per-launch lint, checkMesh
    pre-flight, stop file and the two hashed controlDict sources are no longer product requirements, and the
    S-1..S-8 gate is dropped. **On this dev machine round 3 still runs through the harness unchanged** (its record,
    lint, hashed bundle and `Disallowing` banner check) because it is the working tool and costs nothing to keep;
    the load and `join.lock` waits below stay because they serve this repo's agents, not the product.
  - ≤ 6 ranks, `nice 10`, waits while the 1-minute load is above 10 or `join.lock` exists, one job at a time;
  - peak RSS via `/usr/bin/time -l`;
  - memory ceiling: checkMesh at AR 12 is ≈ 5 GB (**Estimate**). Do not overlap it with a join.
  Gmsh runs as a separate process (COMMIT-02) with `nice 10` and the same waits.
- **Stop rules:**
  - ≤ 2 repair cycles per question (mesh: M1a, then M1b; GCI: two numerics cycles; G0 is a diagnostic and does not
    count);
  - a block that runs > 25 % over its budget stops and reports;
  - any "Allowing" banner stops everything;
  - no setting changes after its run starts;
  - a failed acceptance is a finding, not a retune.

| Block | Runs | Compute (Estimate unless measured) | With +30 % waits |
|---|---|---|---|
| M | R3-M0, M1a, M1b, M2, M3 | 10 + 5 + 5 + 21 + 50 ≈ **91 min** | ≈ 2.0 h |
| G | R3-G0, one cycle, the clause-5 extension | 5 + 95 ≈ **100 min**; with a second cycle ≈ 3.2 h | ≈ 2.2–2.5 h |
| Core | | | **≈ 4.5 h wall** |
| Authoring | generator knobs K1–K3, R3-M0 locator, case YAMLs, the G-case scheme variants | ≈ 1.5 h | |
| Conditional | R3-G2 (L3) · R3-M4 (3-D A4 y+ gate) | 4–10 h · 2.4–7 h | operator rulings |

Round 2 for comparison: the planned core was 4.1 h. The measured blocks were C 85 min (on budget) and M 66 min
(under its 100 min budget).

## 5. Open decisions for the operator

| Id | Decision | Recommendation |
|---|---|---|
| DR-F3-1 | Amend DR-F2-6 (ADR-0012): on OpenFOAM unstructured meshes, use checkMesh's validity set in place of `cellDeterminant` ≥ 0.001, beside non-orthogonality ≤ 70° and skewness ≤ 4/20, and report the determinant split by layer and core cells | **A**, then revisit a core-only floor after R3-M0 |
| DR-F3-2 | Launcher allow-list in round 3: no new app (`checkMesh -writeSets/-writeFields` instead of `foamToVTK`; freestream start instead of `mapFields`) | **no additions** (still the plan). Ruling 68: the security reason (a change to the launcher hash, the probe pins and the review) no longer holds, so Gmsh may join the launcher's app enum if a later round needs it; round 3 adds nothing |
| DR-F3-3 | The L3 run (4.1–9.9 h, Estimate) if both numerics cycles fail. L2 (16–95 h) is never run on this host | approve **only after** R3-G1 fails, cap 12 h, overnight, no other track running |
| DR-F3-4 | R3-M4: an AR 8 3-D A4 solve for the y+ gate (2.4–7 h, Estimate) once the AR 8 mesh passes | **yes**, cap 8 h. It is the only route to a SPIKE-03 GO at AR 8 |
| DR-F3-5 | If R3-M0 puts the bad faces at the tip poles and K2 does not clear them: a pole-free tip variant (K4) as a declared analysis-geometry change under DR-F2-4, with the planform change measured and recorded. Record design and analysis b and S: the round tip already extends the half-span by up to 7.28 mm (+1.5 %). S_ref and b stay at their design values. Label "tip modified for analysis". Results compare only with the same tip | yes, declared and labelled |
| DR-F3-6 | Run the S-1..S-8 probes before round 3 starts (≈ 5 min, operator; script sha256 `f0e8242c…7a5ce`) | **Done 2026-10-04** (Ruling 68: S-1..S-8 ran; only S-1 and S-2 serial stay, as the pin-change check). Round 3 adds no app. Its only new options are `-allGeometry -writeSets vtk -writeFields '(nonOrthoAngle cellDeterminant)'`, and both fields are on the lint's `OF_OUTPUT_FIELDS` list. Where these options write is **Inferred** to be inside the case; R3-M0 records the paths written. ADR-0012 D2 rule 1 is now Verified (S-1, S-2, S-4) |
| DR-F3-7 | If no monotone triplet is found within budget: (a) an oscillatory-bound uncertainty, U = ½(S_U − S_L) (ITTC 7.5-03-01-01, **Flagged**: re-open the text before ruling); (b) a least-squares fit over ≥ 4 grids (Eça–Hoekstra, **Flagged**; needs L3); (c) keep the TMR + A4 fixture without its GCI clause | decide after round 3. Default (c) |

## 6. Reviewer verdicts

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| CFD numerical verification (author) | — | does not clear its own veto | hypotheses and readings stated before runs; no GCI from an oscillatory or noise-level pair |
| Hydrofoil hydrodynamicist (Adversary, read only) | **PASS WITH CONDITIONS** (re-ran the TE arc, the stack, δ, the CFL3D R/p and the L6 offset) | not triggered | 1 the stack at 6.35 µm is 1.19 mm, not 1.12 mm (§2.4; verdict-r2 :163 slip noted) · 2 build at 6.0 µm (p95 0.95, max 1.72 projected), with K3 applied for the 7.8× size jump (R3-M2; the reviewer's 7.4× re-computed as 7.8×) · 3 CFL3D L6/L5/L4 GCI under-states the error 4.3×, so OpenFOAM L6/L5/L4 GCI is labelled "pre-asymptotic; not shown conservative" (§3.4) · 4 the TE segment count is read from the mesh, and per-region stack/δ and cells across δ are reported (§2.2, §2.4) · 5 a pass holds only at U 5 m/s, α 4°, Re 6.0e5 (§2.4) · 6 tip records and label (DR-F3-5) · 7 D7 label wording and a TE force patch in R3-M4 (ADR D7, R3-M4) · 8 "Cd upper bound vs free transition"; R3-M4 uses D4 numerics or declares the deviation (§2.4, ADR D4) · 9 the L6 anomaly labelled Inferred, and Δcomp L5-only noted (§3.1, ADR D5). Reading: H2 is the weakest prior (CFL3D is monotone with first-order advection), but cheapest-first still stands |
