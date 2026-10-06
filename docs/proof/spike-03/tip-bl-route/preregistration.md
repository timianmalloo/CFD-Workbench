---
id: proof-spike-03-tip-bl-route-prereg
title: SPIKE-03 tip BL route S6 — pre-registration (variants, metrics, decision rule)
type: proof-pack
status: in-review
owner: "@trk-s6"
phase: spike
tags: [spike-03, gmsh, snappyhexmesh, mesh-gate, tip, boundary-layer, pre-registration, ruling-97]
links:
  - {to: proof-spike-03-tip-coupon, rel: relates-to}
  - {to: proof-spike-03-tip-coupon-prereg, rel: relates-to}
  - {to: plan-tip-handling, rel: implements}
  - {to: plan-fluids-round3, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-05
summary: >-
  Written and committed before any S6 mesh. Four boundary-layer route variants on S4's V1 coupon (the planar flat cut
  at b/2 of Ruling 93): W1 Gmsh with the corner-fan option set, W2a and W2b snappyHexMesh addLayers (layers around the
  edge, layers terminated at the edge), W4 an in-mesh 8 um perimeter round (needs an operator ruling). W3 (K2 normal
  smoothing) has no available tool and is not run. The S4 metrics, the pass definition and the decision rule.
review-suggested: []
---

# S6 tip BL route — pre-registration

Track S6, round-oct05. Authority: Ruling 97 (change the BL route so the Ruling 93 tip of record can mesh; lifts Ruling
80's mesh hold for this spike only); Ruling 93 (the tip is a finite planar cut at b/2); S4's
[verdict](../tip-coupon/verdict.md) (V1: 3,813 tip-region faces above 70 degrees, 263 negative-volume cells, 99 % at the
convex cap edge, Gmsh built-in `extrudeBoundaryLayer`, no corner treatment); [tip-handling.md](../../../plans/tip-handling.md)
§4.2 (K2); [fluids-round3.md](../../../plans/fluids-round3.md) (DR-F3-1 A). Nothing below changes after the first mesh run.
Where a result forces a change, it is reported as a deviation. Labels: **Verified** = read from a file or a run;
**Inferred** = my reading; **Flagged** = not tested.

## Research (brief)

How BL meshes treat a convex 90 degree edge, and what is installed here.

1. **Gmsh, 3-D.** The built-in `extrudeBoundaryLayer` extrudes along surface-mesh normals. Gmsh's own issue and list
   history says the BL code "does not try to solve the hard problems, such as handling sharp corners or edges"
   ([gmsh issue 2764](https://gitlab.onelab.info/gmsh/gmsh/-/issues/2764); [list, 2013](https://onelab.info/pipermail/gmsh/2013/008518.html)).
   Verified here (4.15.2, API probe `scratchpad/s6/fieldprobe.py`): the `BoundaryLayer` field takes `CurvesList`, `PointsList`,
   `EdgesList`, `FanPointsList`, `FanNodesList`, `FanPointsSizesList`, `IntersectMetrics`, `Quads`, `Ratio`, `Size`,
   `Thickness`, `NbLayers`, `ExcludedSurfacesList`, but **no `SurfacesList`** (the call is refused), and the library's own
   text for `Mesh.BoundaryLayerFanElements` reads "Number of elements (per Pi radians) for 2D boundary layer fans". So a
   corner fan is a 2-D feature. `Mesh.SmoothNormals` / `AngleSmoothNormals` are viewer options ("Smooth the mesh normals?"),
   not layer-normal controls (Inferred from their text; not exercised). Inferred: no 3-D corner-fan control exists in Gmsh
   4.15.2. W1 tests that by setting the fan option and reading whether the mesh changes.
2. **snappyHexMesh addLayers.** `featureAngle` ("when not to extrude surface. 0 is flat, 90 is right angle"),
   `nSmoothSurfaceNormals`, `nSmoothNormals`, `nBufferCellsNoExtrude` (a buffer region that steps the layer count down at
   terminations), `minMedialAxisAngle`, `maxThicknessToMedialRatio` and `layerTerminationAngle` are the edge controls
   ([CFD Online, addLayersControls](https://www.cfd-online.com/Forums/openfoam-meshing/139074-snappyhexmesh-addlayerscontrols-doubt-next-thread.html);
   [cfdsupport.com](https://cfdsupport.com/node128.html); [OpenFOAM issue 2320](https://develop.openfoam.com/Development/openfoam/-/issues/2320)).
   Practice at a 90 degree edge is one of two things: extrude around it (featureAngle above 90) and let the medial-axis
   shrinker settle the cells, or terminate the layers at it (featureAngle below 90) with a buffer. `snappyHexMesh`,
   `surfaceFeatureExtract` and `blockMesh` are in the OpenFOAM-v2512 app and on the launcher allow-list
   (`cases/tools/of-run.sh`), and round 2 already ran this route (M1a/M1b) with the same dictionary templates.
3. **cfMesh.** Not installed (`cartesianMesh` not on the path; not in the v2512 app). Not run.
4. **K2 normal-smoothed extrusion.** No tool is installed. A hand-written extrusion plus tet fill is outside this box.
   W3 is **not run** (stated here, not discovered later). Flagged as the next candidate if W1, W2 and W4 all fail.
5. **A small edge round, only in the mesh geometry.** A round turns the 90 degree edge into a short fan of cells whose
   normals rotate in steps. It is a **geometry change**: Ruling 93 says the tip of record is the planar cut. A round with
   radius at most the first cell height (8 um) is far below any hydrodynamic scale, so it may be acceptable as a meshing
   tolerance, but that is **an operator decision**. W4 is run only to learn whether the route works; it is never adopted
   by this track. Verified: OCC `fillet` on the section solid fails for radii below 0.15 mm (probe `scratchpad/s6/filletprobe.py`:
   4 um to 100 um refused, 150 um to 240 um accepted), so W4 builds the round as a ruled loft (below).

## Coupon and settings

The S4 V1 coupon: half-span b/2 = 25 mm, chord 0.12 m, NACA 0012 with TE blunted to 0.5 mm, round TE base radius 0.25 mm,
planar cap at b/2, root on the symmetry plane. Layers: 20 from 8 um at ER 1.2 (stack 1.49 mm). Gmsh routes (W1, W4)
keep every S4 V1 setting (R3-M1b: Delaunay 3-D, uniform 1.5 mm surface size, K1' TE strip, K1 field). snappy routes (W2a,
W2b) use round 2's M1b settings **unchanged** (base cell 48 mm, surface level 5-6, feature levels 8/7, TE box level 7,
3 cells between levels, `mesh_shrinker displacementMotionSolver`, `minThickness` 1e-6, `maxThicknessToMedialRatio` 0.3,
`nLayerIter` 50, `nRelaxedIter` 20, `nOuterIter` 3), the same domain box, the same layer recipe, and the S4 V1 section and
round TE base as the STL. Case files: `cases/spike03-s6-w1.yaml`, `-w2a.yaml`, `-w2b.yaml`, `-w4.yaml`. Tools and cases
exist before any mesh; their sha256 (no mesh run yet):

```
ee9e8f7c4e10cfcddd5cb519c262473a0a568148c35e1824df385c69af0ab41f  cases/tools/make-tip-bl-gmsh.py
67840c32d5e74ddd433073d9896f72b65aa4b9e4720bf8fe436fe60c0754480d  cases/tools/make-tip-bl-snappy.py
a633bf9546f7e7cf319eec384b6ef87778a2c96757c4da3e63b247bd808f84f0  cases/spike03-s6-w1.yaml
bfabc66743a4db2b8fcab536e70f6d1b3257c0a4179afa93cf51de54303a966a  cases/spike03-s6-w2a.yaml
587cac67edca74ce7edc27250eab58beb52b658e0187f867c17852c0511d0b2b  cases/spike03-s6-w2b.yaml
3f2a31648ac05f83233d0cfd731d05f17ee21b5d736853d06a4b12251f036726  cases/spike03-s6-w4.yaml
```

Budget: one mesh run per variant (4), plus at most 2 spare runs, each only for a void run (a tool or generator defect, or
a wall-time kill), each recorded. No tuning of any knob. Fan-out 0. No solver. The machine-courtesy rule of S4 applies
(1-minute load below 30, poll 30 s up to 20 min, then proceed and record the load). snappy runs serial, wall cap 40 min.

## Variants

| Id | Route | Edge treatment |
|---|---|---|
| W1 | Gmsh built-in BL on V1 | `Mesh.BoundaryLayerFanElements = 8` set (the documented 2-D fan option). Reading: if the 3-D mesh is byte-identical to S4 V1 (msh sha256 `7d5174a3...`) the option has no 3-D effect. |
| W2a | snappyHexMesh addLayers on the V1 STL | `featureAngle 180`, `nBufferCellsNoExtrude 0`: layers extruded around the cap edge, shrinker resolves it. |
| W2b | snappyHexMesh addLayers on the V1 STL | `featureAngle 80`, `nBufferCellsNoExtrude 1`: layers terminate at the 90 degree edge. |
| W3 | K2 normal-smoothed extrusion | **Not run: no tool available** (Research 4). |
| W4 | Gmsh built-in BL on V1 plus an in-mesh perimeter round | Radius 8 um (= first cell height), 4 chords of a quarter circle (ruled loft: sections at z = b/2 - r and four inset sections, each stepping r(1 - cos th) inward and r sin th outward, th = k pi/8; the last is the planar cap). **A geometry change: needs an operator ruling before adoption.** |

## Regions and metrics

Region definitions are S4's: tip_pole, te, root, interface, tip, main, plus the `edge` flag (within the stack height 1.49 mm
of the cap-to-skin seam). **Tip region** = every face, cell or wing face with centre at span coordinate >= b/2 - 2 mm
(snappy frame: y; Gmsh frame: z). Per variant, all read from files:

1. Faces above 70 degrees (`nonOrthoFaces`) by region, with the tip-region total and the `edge`-flagged count.
2. Maximum non-orthogonality (degrees).
3. Faces with weight below 0.05 (count, minimum, tip-region count).
4. Negative-volume cells: whole coupon (checkMesh) and tip region (cell volume field written by checkMesh, `cellVolume`).
5. checkMesh against DR-F3-1 A (validity set without the determinant test, non-orthogonality at most 70, skewness at
   most 4 internal and 20 boundary, volumes positive, face pyramids, face weight at least 0.05, layers).
6. Layer coverage in the tip region: the share of wing boundary faces in the tip region whose owner cell is a layer cell
   (Gmsh: prism; snappy: owner is a prism or a hex whose cell centre is within the stack of the wall, read from the owner-cell
   shape and `nSurfaceLayers` where written). Also the whole-coupon layer figures from the generator or the snappy log.
7. Cells, wall time and peak RSS per step, and the load at start.

## Decision rule

**A variant passes the tip region** when, in the tip region, all of: zero faces above 70 degrees; zero faces with weight
below 0.05; zero negative-volume cells; no checkMesh error line (other than determinant) that the tip-region sets or cell
volumes locate there (a whole-coupon error line with no location is reported and, if it names a tip-region quantity, counts
against the variant); and at least 95 % of tip-region wing faces carry a full layer column. The whole-coupon gate is
reported beside it and never rescues a failing tip region. A route that terminates its layers at the edge can pass only
by clearing the 95 % layer test too; if it passes with fewer layers on a smaller share, it is reported as "passes, layers
terminated", **not** as a pass.

1. The first variant, in the order W1, W2a, W2b, that passes, is **recommended** as the BL route for the tip of record
   (not rounded). W1 passing means the 3-D mesh differs from S4 V1 and passes.
2. If W4 passes and no non-rounded variant does, the verdict is **operator decision** (rule: Ruling 93 vs a meshing-
   tolerance round of at most 8 um). W4 is never recommended on its own.
3. If more than one non-rounded variant passes, recommend the earliest in the order above and report the others.
4. If no variant passes: stop and report, with the failure location per variant; **no tuning**. S5 stays untriggered and
   the next candidates (K2 hand extrusion, cfMesh, a larger-radius round as an operator decision) are named, not run.
5. A void run is repeated once (a spare); a repeat that voids again is reported as "not possible".

Terms not defined here take S4's [preregistration](../tip-coupon/preregistration.md) meaning. A cause named by any result
is Inferred from one mesh per variant, never Verified. O5 stays rejected: no mesh here feeds a result shown as current.

## Amendment 2 — W2c (Ruling 98), written and committed before any W2c mesh

Authority: Ruling 98 (operator, 2026-10-05): one more snappyHexMesh coupon set at finer surface and feature refinement;
no other tuning. Amendment 1 does not exist; this numbering follows the verdict's reference to "a new pre-registration".

**Hypothesis (H-S).** The layer collapse seen in W2a and W2b (mean 4.9-6.2 of 20 layers, 0 % full columns on the tip) is
caused by surface cells that are coarse relative to the layer stack. Round 2's M1b levels give wing surface cells of 1.5 mm
(level 5) and 0.75 mm (level 6), 0.19 mm at feature edges, against a 1.49 mm stack. The prediction: with finer cells the
tip-region full-layer share rises. H-S is **refuted** if it does not rise at either level (below).
**Not tested here:** whether the stack itself (8 um, ER 1.2, 20 layers) is too thick for snappy at any resolution.

**Variants** (W2b exactly: `featureAngle 80`, `nBufferCellsNoExtrude 1`, same STL, domain, layer recipe, shrinker and every
other setting; base cell 48 mm; only the levels change). Cell size = 48 mm / 2^level:

| Id | Wing surface level (min, max) | Feature-edge levels (distance, level) | TE box level | Surface cell on the cap |
|---|---|---|---|---|
| W2b (reference) | 5, 6 | (0.5 mm, 8), (2 mm, 7) | 7 | 1.5 / 0.75 mm |
| W2c-1 (+1) | 6, 7 | (0.5 mm, 9), (2 mm, 8) | 8 | 0.75 / 0.375 mm |
| W2c-2 (+2) | 7, 8 | (0.5 mm, 10), (2 mm, 9) | 9 | 0.375 / 0.19 mm |

W2c-2's surface cell (0.19-0.375 mm) is at most about a quarter of the stack. Cases: `cases/spike03-s6-w2c1.yaml`,
`cases/spike03-s6-w2c2.yaml` (sha256 `705e3a3009f48ab04f6958b413d63becdebcb2b69587a8d5fc5902756cd0a068`,
`1cac401c97636d48ce1502ed5f98bb7e0e7ad2e5b284765aedb4a54db6222f75`). Driver, generator and locator are W2b's, unchanged
(`mesh-gate-snappy.sh`, `make-tip-bl-snappy.py`, `mesh-locate-snappy.py`).

**Order and budget.** W2c-1 first, W2c-2 second, one run each, serial, 1-minute load below 30 before each (of-run polls).
Wall cap 30 min per run: a run that does not finish is killed and reported "not possible" with the last snappy iteration
read; a kill is not repeated. Cell count rises about 4x per level (Inferred from the surface face count), so W2c-2 may not
fit the box; if W2c-1 has not finished by minute 45 of the 75-minute box, W2c-2 is not started and that is reported.
Metrics: the same as the pre-registration (faces above 70 degrees, weight below 0.05, negative cells, checkMesh against
DR-F3-1 A, layer coverage in the tip region from `nSurfaceLayers`, cells, time, RSS, load).

**Decision rule** (the same pass definition, applied verbatim to each run):
1. A W2c run **passes** when its tip region has zero faces above 70 degrees, zero faces with weight below 0.05, zero
   negative-volume cells, no located checkMesh error line, and at least 95 % of tip wing faces with the full 20 layers.
   Then S5 becomes one AR 8 snappyHexMesh run on the tip of record, under a new pre-registration (not an edit of this one).
2. If neither run passes: stop and report the measured trend (tip-region full-layer share, mean layers, faces above 70
   degrees and negative cells against level: W2b, +1, +2). H-S is **supported** if the full-layer share rises with level and
   **refuted** if it does not rise at either level. A rise short of 95 % is a trend, not a pass.
3. A pass at +1 makes +2 unnecessary only if +2 has not been started; if it was started it is reported too.
