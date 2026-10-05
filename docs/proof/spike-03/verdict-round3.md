---
id: proof-spike-03-round3
title: SPIKE-03 round 3 verdict — locating and repairing the Gmsh AR 8 wing mesh against the DR-F3-1 A gate
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-03, openfoam, gmsh, mesh-gate, boundary-layer, round-3]
links:
  - {to: proof-spike-03-round2, rel: supersedes}
  - {to: plan-fluids-round3, rel: implements}
  - {to: rulings, rel: depends-on}
  - {to: adr-0012-openfoam-backend-macos, rel: relates-to}
  - {to: proof-spike-04-round3, rel: relates-to}
review-by: 2026-11-03
summary: >-
  Mesh NO-GO at AR 8 after both repair cycles, so R3-M2, R3-M3 (AR 5, 12) and R3-M4 (the 3-D y+ gate solve) were not
  run. R3-M0 located the 811 faces above 70 deg: 616 on the TE arc strip, 173 at the tip's TE end, none at the
  prism/tet interface. The cause is measured, not assumed: the 1.49 mm layer stack spreads each prism column 7.0x
  around the 0.25 mm TE arc and not along the span. A structured TE strip sized by that ratio (R3-M1b) took the panel TE
  from 616 faces to 1. The gate still fails on the revolved tip (246 faces > 70 deg, max 85.2 deg) and on 4 faces with
  weight < 0.05. Gmsh repeated M-2c bitwise. macOS arm64 only.
review-suggested: []
---

# SPIKE-03 round 3 verdict — the Gmsh AR 8 mesh against the DR-F3-1 A gate

Lane F1, round 3. Authority: [plan](../../plans/fluids-round3.md) §2 and §4 and [Ruling 68](../../notes/rulings.md)
(DR-F3-1 option A: OpenFOAM mesh floor = checkMesh validity set + non-orthogonality ≤ 70° + skewness ≤ 4/20, with
cellDeterminant reported by layer and core cells; DR-F3-4 the 3-D y+ gate solve once AR 8 passes; DR-F3-5 the pole-free
tip only after K2 fails). Round 2: [verdict-round2.md](verdict-round2.md) (kept unchanged). Lead lens: CFD numerical
verification; the hydrofoil hydrodynamicist reviews the physical claims. Every number is read from a file under
[receipts/](receipts/) (`*-spike03r3-*` directories), unless it is labelled **Estimate** or **Inferred**. Every wing
result keeps the labels **TE blunted for analysis; fully turbulent; transition not modelled**. No flow solution was run
in this round, so no physical result (y+, Cl, Cd) is reported.

## Verdict

| Plan criterion (§2.4 pass, each AR) | R3-M0 (= M-2c) | R3-M1a (K1) | R3-M1b (K1') |
|---|---|---|---|
| Max non-orthogonality ≤ 70° | **86.26°**, 811 faces > 70° | **106.7°**, 70,183 faces | **85.24°**, 246 faces |
| Skewness ≤ 4 internal, ≤ 20 boundary | 1.74 ok | 3.33 ok | 1.82 ok |
| Volumes positive, face pyramids OK | ok | **4 negative cells, 34 bad pyramids** | ok |
| Face weight ≥ 0.05 (DR-F3-1 A) | **0.046** (1 face) | **0.0020** (12,254 faces) | **0.032** (4 faces) |
| checkMesh validity set without the determinant test | **fails** (weight) | **fails** (10 error lines) | **fails** (weight) |
| ≥ 20 layers on ≥ 95 % and ≥ 10 on 100 % of wing faces | 100 % by count | 99.998 %: 120 prisms short, 120 pyramids | 100 % by count |
| Cells (prisms + tets) | 3,207,020 | 8,084,718 | 4,518,553 |
| **Gate (DR-F3-1 A)** | **FAIL** | **FAIL** | **FAIL** |

**SPIKE-03 round 3: NO-GO at AR 8.** Both repair cycles were used (M1a, then M1b), and no variant passes. The plan's
stop rule applies: R3-M2 (h1 6.0 µm and the y+ screen), R3-M3 (AR 5 and AR 12) and R3-M4 (the AR 8 3-D A4 solve for
the y+ gate, DR-F3-4) were **not run**. The round still answers *where* and *why*, with measurements, and it clears the
largest cluster.

## R3-M0 — where the faces are (diagnostic, no setting changed)

M-2c was re-generated with every mesh setting unchanged and written in ASCII. One checkMesh wrote the gate, the sets and
the fields; [`mesh-locate-r3.py`](../../../cases/tools/mesh-locate-r3.py) placed each set by region
([locate.txt](receipts/20261004T163118Z-spike03r3-m0-ar8/locate.txt)).

- **Gmsh repeats bitwise.** `msh_sha256` c5fbe13f…05d4b6 equals M-2c's, and the checkMesh numbers match round 2
  (86.263394°, 811 faces, weight 0.046005, 1,128,345 cells with determinant < 0.001) (**Verified**, generator.txt and
  log.checkMesh).
- **The plan's `assume:` holds.** v2512 checkMesh takes `-writeSets vtk` and `-writeFields '(nonOrthoAngle
  cellDeterminant cellShapes faceWeight)'` (source `checkMesh.C:147`, `writeFields.C:157-397`, and the run). The new
  options wrote only inside the case: `constant/<field>`, `constant/polyMesh/sets/<set>` and
  `postProcessing/constant/<set>/<set>.vtp`
  ([list](receipts/20261004T163118Z-spike03r3-m0-ar8/written-by-checkMesh-and-postProcess.txt)) (**Verified**).
- **Faces above 70° (811).** First match wins: TE arc 616 (all prism-prism, at x/c 1.0014, over the whole span z
  0.002–0.478 m), tip 112, tip pole 61 (within 2 mm of the TE pole), main 22 (12 prism-prism, 10 tet-tet). **Prism/tet
  interface: 0** (cause 3 of the plan's §2.2 is not seen). Root: 0. Median wall distance 83 µm: low in the stack.
- **TE arc resolution.** The root TE arc had **4 segments** (`Mesh.MinimumCirclePoints` = 7, Verified). So the arc
  strip carried triangles about 0.20 mm around the arc by 1.5 mm along the span.
- **The low-weight face** sits near the LE in the tets just above the stack: x −0.33 mm, y 3.2 mm, z 0.160 m, wall
  distance 1.87 mm.
- **The 256 two-internal-face cells are far-field tets** (wall distance 1.16–2.90 m), and so are 258 of the 691,580
  tets with determinant < 0.001 (median wall distance 1.75 m). The determinant minimum of 0 is a domain-corner tet, not
  a wing cell (**Verified**). Plan cause 2 (tip poles) is therefore not behind the 256 cells.
- **Determinant split (DR-F3-1 A).** Below 0.001: **1,128,087 of 2,515,440 prisms (44.85 %)** and **258 of 691,580
  tets (0.04 %)**. Thin prisms have a small determinant by construction. The core tets that fail are far-field corner
  cells. A determinant floor says nothing about the layer cells. That supports option A (**Inferred**).

## R3-M1a — knob K1 (isotropic 0.1 mm at the TE): worse

Set before the run from the M0 table (largest cluster: TE). A Gmsh Distance field from every curve aft of x = c sets
0.1 mm (π r_TE / 8) within 0.3 mm, graded to 1.5 mm at 3.6 mm (3 % of chord). The arc got 8 segments.

- **Deviation from the plan:** the plan names "K1 + K2 (normals)". K2 is not built. The docstring
  (`gmsh.py:6214-6227`) does not say what the extrusion does at a node outside the view's support (**Flagged**). K1
  alone addresses the measured mechanism.
- **Attempt 1 was void** ([VOID.txt](receipts/20261004T163551Z-spike03r3-m1a-ar8-void/VOID.txt)): a generator defect.
  The K1 threshold had SizeMax = lc_near, so the Min field floored the whole domain at 1.5 mm. Gmsh was killed at
  632 s and 8.5 GB RSS. The fix ramps K1 to lc_far, and the generator now has a wall cap (`GMSH_CAP_S`, SIGALRM). The
  knob itself did not change.
- **Result** ([locate.txt](receipts/20261004T165903Z-spike03r3-m1a-ar8/locate.txt)): **70,195 faces > 70°**, max
  106.7°. 69,499 are on the panel TE, at median wall distance **1.35 mm**, which is the top of the 1.49 mm stack. There
  are also 4 negative-volume cells, 21 open cells, 12,254 low-weight faces, 120 prisms short of 20 x the wing faces
  (120 pyramids in the mesh) and a `defaultFaces` patch of 78 faces.

### The mechanism (from M0 and M1a together)

Around the TE arc (r = 0.25 mm) a stack of height H = 1.49 mm spreads each prism column (r + H)/r = **7.0 times around
the arc**. Along the span it does not spread at all. So:

- a column that is isotropic at the wall (M1a) is 7:1 at the top;
- a column that is about 7.6:1 at the wall (M0: 0.20 × 1.5 mm) is isotropic at the top.

A right-split a × b cell pair has a centre-to-centre line at acos(2ab/(a² + b²)) to its shared face. That passes 70° at
a ratio of 5.7:1 (arithmetic). The bad faces moved from the bottom of the stack (M0, median 83 µm) to the top (M1a,
median 1.35 mm), as this reading predicts (**Inferred**, with two measurements behind it). Plan cause 1 named the TE
arc. The specific failure is this column anisotropy, not the shear at the arc ends.

## R3-M1b — knob K1' (structured TE strip sized by the fan ratio): panel TE cleared, tip remains

Set before the run: the two TE arc strip surfaces made transfinite, with 4 segments per quarter arc (0.098 mm) by 1,846
along the span (0.26 mm). 0.26 mm is the geometric mean, √7.0 = 2.64 times the arc cell, so a column is 2.6:1 at the
wall and 2.6:1 the other way at the top (about 48°). The K1 field is set to 0.26 mm near the TE. The second cluster's
knob (tip) was not added: K2 is not built, and K4 (DR-F3-5) applies only after K2 fails.

- **Pre-registered reading:** "TE mechanism supported if TE-flag faces > 70° fall below 68 (10 % of M0's 677)".
  Measured: **73 TE-flag faces**, so this is **not met as written**. 72 of the 73 also carry the tip or tip-pole flag:
  the TE flag (x ≥ c − 1 mm) includes the TE end of the tip. On the panel (z ≤ b/2 and more than 2 mm from a pole), TE
  faces fell from **616 (M0) and 69,499 (M1a) to 1**
  ([split](receipts/20261004T181030Z-spike03r3-m1b-ar8/te-panel-split.txt)). The panel split was computed after the run.
  It is reported beside the pre-registered number and does not replace it.
- **What still fails** ([locate.txt](receipts/20261004T181030Z-spike03r3-m1b-ar8/locate.txt)):
  - 246 faces > 70°, max 85.24°: tip pole 176, tip 34, main 34 (13 prism-prism, 21 tet-tet), TE 2;
  - 4 faces with weight < 0.05 (min 0.032), in the tets 1.7–2.0 mm off the wall near the tip TE and at the tip;
  - everything else in the gate passes: skewness 1.82, volumes positive, pyramids OK, 20 layers on 100 % of 183,938
    wing faces.
  - Cells: 4.52 M (3.68 M prisms). checkMesh peak RSS 4.8 GB.
- **Where the tip faces are** ([per region](receipts/20261004T181030Z-spike03r3-m1b-ar8/region-wall-distance.txt)):
  tip pole 176 faces at x/c 0.9867–0.9968, median wall distance 66 µm; tip 34 at x/c 0.9803–0.9896, median 106 µm.
  Both sit **low in the stack**.
- **Reading (Inferred, cause not separated):** the column-spread mechanism alone does not explain the tip faces. With
  isotropic wall cells it predicts failures at the top of the stack, and only where the revolve radius (the local
  half-thickness: 0.59 mm at x/c 0.98, 0.34 mm at 0.995, 0.25 mm at 1.0) is below H/4.67 = 0.32 mm, that is aft of
  x/c ≈ 0.996. The faces reach forward to 0.980 and sit near the wall. Two other causes fit the receipts:
  - the pole degeneracy itself: Gmsh logs "Skipping boundary layer extrusion of degenerate curve 19/22/29/31" on every
    run, and the revolve leaves sliver triangles at the pole;
  - anisotropic wall triangles on the revolved TE region.
  Separating them needs a run. The next knob is either a structured treatment of the tip's TE region or a pole-free tip
  (K4, DR-F3-5).

## Physics and labels (for the hydrodynamicist)

- No flow was solved, so there is no y+, stack-to-δ ratio or force result in this round. The plan's R3-M2 physics checks
  (stack 1.12 mm at h1 6.0 µm, y+ projection p95 0.95 / max 1.72) stay **Estimates** from the plan. They are not
  measured here.
- Geometry is unchanged from M-2c: chord 0.12 m, half span 0.48 m, round TE base of radius 0.25 mm, round revolved tip.
  The tip was **not** modified, so DR-F3-5 is not triggered. S_ref and b stay at design values. The analysis geometry
  already differs from them: the round tip extends the half span by 7.28 mm (+1.5 %) and adds about 6.0e-4 m² of
  planform per tip (+1.05 % of the half-wing S) (hydrodynamicist's re-computation, plan :287).
- A pass would hold only at U 5 m/s, α 4°, ν 1.0e-6 m²/s (Re_c 6.0e5). ν is nominal, not a water record (no T or S_A;
  ITTC fresh water at 20 °C is 1.0034e-6). No pass is claimed.

## Durations, load and memory (measured)

| Block / run | Plan (Estimate) | Measured compute | Measured wall incl. waits |
|---|---|---|---|
| R3-M0 | ≤ 10 min | 67 s (Gmsh 22 + gmshToFoam 10 + checkMesh 24 + postProcess 8 + locator 3) | 7.3 min (6 min load wait) |
| R3-M1a attempt 1 (void) | — | 632 s (killed) | 10.5 min |
| R3-M1a | ≤ 5 min | 4.2 min (Gmsh 124 s, gmshToFoam 35, checkMesh 65, postProcess 24) | about 65 min, 16:47–17:52 UTC (12 min wait before Gmsh, about 44 min join-lock waits before gmshToFoam; one resume) |
| R3-M1b | ≤ 5 min | 1.5 min (Gmsh 32 s, gmshToFoam 14, checkMesh 33, postProcess 10) | 1.7 min |
| Block M (run part) | 20 min compute (M0 + M1a + M1b), ≈ 26 min with +30 % waits | **17.4 min incl. the void attempt** | **1 h 47 min** (16:25–18:12 UTC) |

- **Over-budget stop rule:** compute was under budget. Wall time was 4× budget because other tracks held the join lock
  and the load. This verdict applies the 25 % rule to compute plus the plan's 30 % wait allowance, and reports the wall
  time beside it. The waits were real and are not the run's cost.
- **Peak RSS** (`/usr/bin/time -l`): Gmsh 2.38 / 5.85 / 3.16 GB (M0 / M1a / M1b); the void attempt about 8.5 GB
  (`ps`); gmshToFoam up to 7.63 GB and checkMesh up to **8.68 GB** (M1a, 8.08 M cells). The plan's 5 GB checkMesh
  Estimate was for AR 12 at 4.8 M cells. Measured RSS is about 1.07 kB per cell (8.68 GB / 8.08 M; 4.82 GB / 4.52 M).
- **Load:** the 1-minute load at our own launch and end boundaries was ≤ 22.2. During waits (other tracks) it reached
  **111.7** (sampled 18:03 UTC). Every OpenFOAM process ran `nice 10`, ≤ 6 ranks, one job at a time.
- **Launcher:** all 21 round-3 OpenFOAM launches (9 here, 12 in SPIKE-04) printed `Disallowing`. No stop file was
  written.

## Defects found and fixed (class → control)

- **Join-lock path:** round 2's `mesh-gate-gmsh.sh` waited on a scratchpad `join.lock`, not the repository's
  `coord/join.lock`, so its Gmsh step never saw a join. Fixed in place. The round-3 pipeline uses
  `git rev-parse --git-common-dir`. A sibling remains in `security-probe.sh`. It is not edited, because its sha256 is
  pinned for the operator's probes. It is recorded here for the security right-size work.
- **Size-field floor:** a Threshold with SizeMax below the far size, inside a Min field, floors the whole domain (M1a
  attempt 1). The control is the ramp to lc_far, plus the generator wall cap.
- **Monitor start race (SPIKE-04 tooling):** see the SPIKE-04 round-3 verdict.

## What ADR-0012 can add now (macOS arm64 only)

- **Mesh route:** Gmsh 4.15.2 boundary-layer extrusion (prisms on a tet core) stays the only route that gives 20
  layers on 100 % of the wing. It is repeatable bitwise on this machine. Sizing rule (**Inferred**: derived, then
  confirmed once, at r 0.25 mm and H 1.49 mm): **a triangulated prism column on a singly curved convex surface of
  radius r under a stack of height H should be about √((r + H)/r) : 1 at the wall, with the long side along the axis
  of curvature**. Isotropic columns (M1a) and 7.6:1 columns (M0) both failed. The TE strip built this way passed on the
  panel (M1b). The rule does not cover doubly curved or pole regions (the revolved tip), where the next cause is not yet
  separated. The tip is still open: the options are a structured tip TE region or a pole-free tip (K4, DR-F3-5).
- **The floor (DR-F3-1 A):** measured on three meshes. The determinant test fails 31–45 % of prisms by construction and
  only far-field corner tets in the core. So A (validity set without the determinant, non-orthogonality ≤ 70°,
  skewness ≤ 4/20, weight ≥ 0.05) is the floor to write. The determinant is reported by layer/core.
  `mesh-locate-r3.py` is the reference locator for the floor.
- **Not decided:** any AR 5 / AR 12 result, the y+ gate (needs R3-M4 on a passing AR 8 mesh), memory for AR 12 (about
  1.07 kB per cell measured, so 5–13 GB depending on the TE/tip treatment: **Estimate**).

## Reviewer verdicts

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| CFD numerical verification (author) | — | does not clear its own veto | readings stated before each run; the missed pre-registered threshold is reported as missed; no physical result from an unsolved mesh |
| Hydrofoil hydrodynamicist (Adversary, read only) | **PASS WITH CONDITIONS** (re-ran the fan ratio, the 5.67:1 limit, the 2.64 strip, the half-thickness at x/c 0.98–1.0, the tip's b and S) | not triggered (no hydrodynamic quantity displayed) | 1 the tip reading names pole degeneracy beside the fan mechanism, with wall distance per region and x/c 0.980–0.997 (R3-M1b) · 2 the ADR sizing rule is labelled Inferred and scoped to singly curved surfaces, not poles (ADR section) · 3 tip b and S increments recorded (physics section) · 4 ν marked nominal, not a water record (physics section) — all applied |
