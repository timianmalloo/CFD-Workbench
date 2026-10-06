---
id: proof-spike-03-tip-bl-route
title: SPIKE-03 tip BL route S6 — verdict
type: proof-pack
status: in-review
owner: "@trk-s6"
phase: spike
tags: [spike-03, gmsh, snappyhexmesh, mesh-gate, tip, boundary-layer, ruling-97]
links:
  - {to: proof-spike-03-tip-bl-route-prereg, rel: implements}
  - {to: proof-spike-03-tip-coupon, rel: relates-to}
  - {to: plan-tip-handling, rel: implements}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-05
summary: >-
  No boundary-layer route meshes the flat tip of record to DR-F3-1 A. W1 (Gmsh fan option) is byte-identical to S4 V1:
  the option has no 3-D effect. W2a and W2b (snappyHexMesh) cut the failures from 3,813 faces to 61 and 6 and remove the
  negative cells in W2b, but reach 0 % full layer columns on the tip (at most 14 of 20). W4 (8 um round) fails before a
  mesh exists (Gmsh PLC error). W3 had no tool. The rule says stop and report; S5 stays untriggered. W2b is the nearest
  and had one untested cause (surface cells vs stack). W2c (Ruling 98) tested it: finer refinement gave cleaner tip cells but fewer layers (mean 2.8 of 20), so the hypothesis is refuted at +1 and +2 did not finish. S5 stays untriggered. macOS arm64 only.
review-suggested: []
---

# S6 tip BL route — verdict

Track S6, round-oct05 (Ruling 97). Readings and thresholds: [preregistration.md](preregistration.md), committed before any
mesh (commit 2b03999). Receipts: [receipts/](receipts/). Labels: **Verified** = read from a file in receipts or a run;
**Inferred** = my reading; **Flagged** = not tested. Every mesh is the S4 V1 coupon: **TE blunted for analysis; tip variant
for a mesh coupon only; no flow solution**. No physical result is reported.

## W2c (Ruling 98): finer snappy refinement did not grow the layers — H-S refuted at +1; +2 did not finish

Pre-registered in [preregistration.md](preregistration.md) Amendment 2 (commit 70bf20d), before any W2c mesh. **Rule 2
applies: neither run passes; stop and report the trend.** S5 is **not triggered**.

| Tip region, same coupon, W2b layer controls | W2b (levels 5-6) | W2c-1 (+1, levels 6-7) | W2c-2 (+2, levels 7-8) |
|---|---|---|---|
| Surface cell on the cap | 1.5 / 0.75 mm | 0.75 / 0.375 mm | 0.375 / 0.19 mm |
| Mesh produced | yes | yes | **no: killed at the 30 min cap** (castellated 5.55 M cells and snapped in 29 min; layer iteration 0 of the first outer iteration) |
| Cells | 374,422 | 1,059,695 | n/a |
| Tip-region wing faces with the full 20 layers | 0 % | **0 %** | n/a |
| Tip-region wing faces with no layer at all | 12.4 % | **94.8 %** (47,274 of 49,866) | n/a |
| Whole-coupon mean layers (of 20); thickness of target | 4.88; 70 % | **2.79; 24.6 %** | n/a |
| Faces > 70 deg, tip region (whole coupon) | 6 (6) | 4 (130) | n/a |
| Faces with weight < 0.05, tip region (whole) | 50 (62) | 4 (32) | n/a |
| Negative-volume cells | 0 | 0 | n/a |
| Max non-orthogonality; max skewness | 136.2; 5.58 | 80.3; 1.55 | n/a |
| checkMesh error lines | 7 failed checks | 3: face tets (4 faces), concave cells, low weight (32) | n/a |
| Snappy wall, peak RSS | 238 s, 1.43 GB | 1,628 s, 3.28 GB | killed at 1,800 s |
| Tip region passes | no | **no** (4 faces > 70 deg, 4 low-weight faces, 0 % full layers) | no mesh |

Verified from `receipts/*w2c1`, `*w2c2` (`log.locate`, `log.checkMesh`, `snappy-layers.txt`, `run-ledger.txt`). The 126 of
W2c-1's 130 faces above 70 degrees outside the tip region sit at the root end (span 0.0004 m), where the STL crosses the
domain boundary; they are not tip faces.

**Trend (measured):** mesh quality at the tip improves with level (faces above 70 degrees 6 to 4, maximum non-orthogonality
136 to 80 degrees, skewness 5.6 to 1.5, no negative cells), but the layers get **worse**, not better: mean layers 4.9 to
2.8, thickness 70 % to 25 % of target, and nearly every cap face has no layer at +1. **H-S (coarse surface cells cause the
collapse) is refuted at +1** (the full-layer share did not rise; it was 0 % at both levels, and every other layer measure
fell). +2 gives no reading: the run could not finish in the cap, so H-S is refuted by the one level that ran, not tested at
the second. Cause of the fall: **Flagged**, not separated. A reading consistent with the data (Inferred): finer cells make
the 1.49 mm, 20-layer stack thick relative to the cell, so the shrinker removes more columns; this contradicts the
pre-registered direction. The coupon cannot say whether the stack itself (8 um, ER 1.2, 20 layers) can be grown by snappy at
any resolution: that was not varied (no other tuning, by Ruling 98).

**Consequence for S5.** S5 (one AR 8 run on the tip of record) is not triggered: no BL route passes the tip region. The
snappy route as configured gives clean tip cells without a boundary layer, not a boundary layer. What remains is outside
Ruling 98: K2 hand extrusion, cfMesh, a thinner or fewer-layer stack in snappy (a recipe change, itself an operator
decision because DR-F3-1 A names 20 layers), or a larger edge round (an operator decision under Rulings 88 and 93).

**Not possible or not done (W2c).** W2c-2 produced no mesh (killed at the 30 min cap by `cap-watch.sh`; no snappy process
remains; not repeated, per the pre-registration). Hydrodynamicist and CAGD reviewers did not run. Layer share is read from
`0/nSurfaceLayers` with owner-cell centres as the tip-region test (a proxy). No load wait was recorded in the W2c ledgers; one
mesh per level, no tuning.

## Result (Ruling 97 set, before W2c)

**Rule applied: "If no variant passes: stop and report, no tuning" (rule 4).** Verified: no variant has a tip region with
zero faces above 70 degrees, zero low-weight faces, zero negative cells and 95 % full layer columns. No non-rounded route is
recommended. W4 did not pass, so rule 2 (operator decision on the round) did not fire either. S5 is **not triggered**.

| Pre-registered variant | Outcome |
|---|---|
| W1 Gmsh BL with the fan option set | **No effect in 3-D.** The msh file is byte-identical to S4 V1 (sha256 `7d5174a3...`). Same 3,813 faces, same 263 negative cells. |
| W2a snappy, layers around the edge | **Fails.** 61 faces above 70 degrees, 61 low-weight faces, 6 negative cells, 0 % full layer columns. |
| W2b snappy, layers terminate at the edge | **Fails.** 6 faces above 70 degrees, 50 low-weight faces in the tip region, 0 negative cells, 0 % full layer columns. |
| W3 K2 normal-smoothed extrusion | **Not run: no tool.** (Stated in the pre-registration.) |
| W4 8 um perimeter round (operator ruling needed) | **No mesh.** Gmsh 3-D stops with "PLC Error: A segment and a facet intersect". Not a pass. |

## Metrics per variant

All runs: 25 mm half-span V1 coupon, 20 layers from 8 um at ER 1.2 (stack 1.49 mm), one run each, no tuning. "Tip
region" = centre at span >= b/2 - 2 mm. Faces above 70 degrees = the `nonOrthoFaces` set (severe plus errors).

| | S4 V1 (reference) | W1 Gmsh + fan option | W2a snappy, extrude around | W2b snappy, terminate at edge | W4 Gmsh + 8 um round |
|---|---|---|---|---|---|
| Mesh produced | yes | yes (same file as V1) | yes | yes | **no** |
| Cells | 309,031 | 309,031 | 404,578 | 374,422 | n/a |
| Faces > 70 deg, tip region (whole coupon) | 3,813 (3,813) | 3,813 (3,813) | 61 (61) | 6 (6) | n/a |
| of which region `edge` flag (Gmsh frame) | 3,778 | 3,778 | n/a | n/a | n/a |
| Max non-orthogonality | 174.5 | 174.5 | 177.5 | 136.2 | n/a |
| Faces with weight < 0.05: tip region (whole) | 250 (250) | 250 (250) | 61 (61) | 50 (62) | n/a |
| Minimum face weight | 3.7e-5 | 3.7e-5 | 0.0220 | 0.0229 | n/a |
| Negative-volume cells: tip (whole coupon) | 263 (263) | 263 (263) | 6 (6) | 0 (0) | n/a |
| Max skewness | 2,834 | 2,834 | 16.8 | 5.58 | n/a |
| Tip-region wing faces with the full 20 layers | 91.4 % have a prism owner (Gmsh) | 91.4 % (prism owner) | 0 % (max 14 layers; 36 % have none) | 0 % (max 12 layers; 12 % have none) | n/a |
| Whole-coupon layers (mean of 20) | 100 % prisms (180 faces without a column) | same | 6.15 (54 % of target thickness) | 4.88 (70 % of target thickness) | n/a |
| checkMesh failed checks | 11 | 11 | 10 | 7 | n/a |
| **Tip region passes (pre-registered)** | no | no | **no** | **no** | no mesh |
| Mesh wall time, peak RSS | 2 s, 310 MB | 2 s, 316 MB | 276 s, 1.66 GB (snappy) | 238 s, 1.43 GB (snappy) | 2 s to failure |
| 1-minute load at start | 5.3 | 19.0 | 20.5 (waited once, 31.1 > 30) | 26.3 (waited once, 34.8 > 30) | 14.4 |

The Gmsh "tip-region wing faces with a prism owner" figure for W1 is a different quantity from snappy's layer count: Gmsh
extrudes all 20 layers or none, and a prism owner does not prove 20 layers there (the 180 faces without a column sit at the
edge: Verified in S4's locate output and repeated in W1's). The rule's 95 % test fails W1 on the other counts first.

Locations (Verified, `log.locate`):

- **W2a**: the 61 faces above 70 degrees are all in the tip region, x/c 0.50-0.85, span 0.0239-0.0251 m (the cap and the
  seam, mid-chord). The 6 negative cells are hexes at span 0.0241-0.0245 m. The 61 low-weight faces sit at span 0.0231-0.0252 m.
- **W2b**: the 6 faces above 70 degrees sit at x/c 0.239-0.242, span 0.0241-0.0243 m (one spot, 0.1 mm from the seam).
  Whole-coupon low-weight faces: 62, 50 in the tip region (span 0.0230-0.0252 m), the rest at span 0.0169 m and up.
- **W4**: the failure is in Gmsh's boundary recovery after the layer extrusion (the layer top surface and an edge
  intersect), before any cell exists: `receipts/20261005T232859Z-spike03-s6-w4/generator.txt`.

## Reading (Inferred unless stated)

1. **Gmsh has no 3-D corner fan.** Verified: the `BoundaryLayer` field has no surface list, its fan option text says 2-D, and
   setting the fan option left the 3-D mesh byte-identical. Gmsh's own 3-D built-in extrusion therefore cannot be tuned out
   of the V1 failure by an option. (Research in the pre-registration.)
2. **snappyHexMesh moves the failure from the edge to a few spots, but cannot grow the layers on this coupon.** Verified:
   W2b has 0 negative cells and 6 faces above 70 degrees (S4 V1: 263 and 3,813). Layers, however, reach at most 12-14 of 20,
   mean 4.9-6.2, and 54-70 % of the target thickness on the whole coupon. **Inferred, not tested:** the round-2 surface
   cells (level 5-6: 1.5-0.75 mm; 0.19 mm at feature edges) are about the 1.49 mm stack or smaller, so addLayers collapses most columns. Round 2's M1b settings
   were used unchanged on purpose (no tuning), so this is a setting, not a snappy limit. The coupon cannot say whether a
   finer surface (cells at least as large as the stack's last layers, or a thinner stack) clears it.
3. **Terminating the layers at the edge (W2b) is better on every quality count than extruding around it (W2a):** 6 vs 61
   faces, 0 vs 6 negative cells, 136 vs 177 degrees maximum, skewness 5.6 vs 16.8. Both are single meshes; the difference is
   Verified as a difference, not as a cause.
4. **W4 shows the 8 um round is not a cheap fix in Gmsh.** OCC `fillet` refuses every radius below 0.15 mm on this section
   (Verified, `receipts/filletprobe.py`), the loft round builds, and then the layer extrusion over strips 3 um wide and
   1.5 mm long self-intersects (PLC error). Cause **Flagged**: not separated (strip width, normals at unaligned strip
   nodes, or the 1.5 mm surface size). It was not retried: the PLC error is the route's outcome, not a tool defect, and the
   budget allowed spares only for defects.
5. **O5 stays rejected.** No mesh here feeds a result shown as current.

## Recommendation

**No BL route is recommended.** The tip of record (Ruling 93's planar cut) has no meshable closure in Gmsh built-in
extrusion (V1, W1) and none yet in snappyHexMesh at round 2's settings. The operator decides what to test next, because
each option below goes beyond Ruling 97's four:

1. **Cheapest, most informative: one more snappy coupon (W2c)** with the surface and feature levels raised so surface cells
   on the cap and seam are about the stack height or smaller, W2b's layer controls, everything else unchanged. It tests
   the one untested reading (reading 2). Cost: minutes, one run (snappy ran in 4 minutes here; 3-4x more cells would take
   longer). Needs a pre-registration first.
2. **K2 hand-written normal-smoothed extrusion** (plan §4.2): no tool is installed; a few days of work, not a spike step.
3. **cfMesh**: not installed here; an install plus a case. Flagged as an option only.
4. **A larger round (operator decision, Ruling 88/93):** OCC `fillet` works at 0.15-0.24 mm on this section. That is a
   geometry change and a larger one than W4's 8 um; it would be worth a ruling only if the snappy route fails too.

## What S5 becomes

S5 as written (one AR 8 run on the tip of record when V1 passes) is **still not triggered**: no BL route passes the tip
region. Round 3 stays NO-GO at AR 8 (R3-M2..M4 not run). If the operator approves item 1, S5 becomes "a snappy coupon at
finer surface levels; if its tip region passes, one AR 8 snappy run on the tip of record" (a new pre-registration, not an
edit of this one). If not, the DR-F2-4 tip exception stands.

## What was not possible or not done

- **W3 (K2)** did not run: no tool is installed, and a hand extrusion is outside this box.
- **W4 produced no mesh**; its checkMesh and tip metrics do not exist. No spare run was used (the failure is a Gmsh 3-D
  meshing error, not a tool or generator defect).
- **W1 is a probe, not a corner treatment**: the option exists for 2-D fans only. The result is "no effect".
- **No tuning** of any knob. One consequence: the snappy surface-to-stack ratio is untested (reading 2).
- The snappy runs ran **serial** (so the layer fields match the mesh face order); the load gate (below 30) delayed the
  checkMesh step of W2a and W2b once each, the 1-minute load at each start is in the table.
- The W2a driver ran before the snappy locator existed; the locator was then run by hand on the finished run (same inputs,
  same code as the driver calls). `log.locate` in that receipt is that run's output; its `pipeline.log` ends with the
  missing-file line.
- Layer coverage for snappy is read from `0/nSurfaceLayers` and face order, with the tip region taken by the owner-cell
  centre (a few tenths of a mm from the wall); it is a proxy for the face centre.
- Hydrodynamicist and CAGD reviewers did not run (fan-out 0).
- S4's tools and receipts are untouched. New files only: `cases/tools/make-tip-bl-gmsh.py`,
  `make-tip-bl-snappy.py`, `mesh-gate-tipbl.sh`, `mesh-gate-snappy.sh`, `mesh-locate-tipbl.py`, `mesh-locate-snappy.py` and
  `cases/spike03-s6-*.yaml`.
- Operator decision raised, not taken: any in-mesh round of the tip perimeter (W4 or larger) needs a ruling; none is adopted here.
