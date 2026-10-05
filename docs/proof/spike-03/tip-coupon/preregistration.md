---
id: proof-spike-03-tip-coupon-prereg
title: SPIKE-03 tip mesh coupon S4 — pre-registration (variants, metrics, receipts, decision rules)
type: proof-pack
status: in-review
owner: "@trk-s4"
phase: spike
tags: [spike-03, gmsh, mesh-gate, tip, coupon, pre-registration, ruling-88]
links:
  - {to: plan-tip-handling, rel: implements}
  - {to: proof-spike-03-round3, rel: relates-to}
  - {to: plan-fluids-round3, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-05
summary: >-
  Written and committed before any coupon mesh is generated. Four variants of a 25 mm tip-only coupon (V0 current
  revolve, V1 planar flat cut at b/2, V2 short flats at the poles, V3 stack height cut at fixed first height), the
  metrics and receipts for each, and the decision rules of docs/plans/tip-handling.md section 4.2 verbatim, with
  every term that needed a number given one here, now.
review-suggested: []
---

# S4 tip mesh coupon — pre-registration

Track S4, round-oct05. Authority: [tip-handling.md](../../../plans/tip-handling.md) §4.2 and §6 row S4; Ruling 88 D5
(approve V0-V3; lifts Ruling 80's mesh hold for one session; amends DR-F3-5 so K4 (V2) may be tested without K2
failing first; K4 is ruled only if V1 fails and V2 passes); Ruling 93 (the tip is a finite chord; V1, the planar flat
cut at b/2, is the tip of record). Baseline: [verdict-round3.md](../verdict-round3.md) (R3-M1b: 246 faces above 70
degrees, tip pole 176, tip 34, TE 2, main 34; 4 faces with weight below 0.05; the DR-F3-1 A floor).
Nothing below changes after the first mesh run. Where a result forces a change, it is reported as a deviation.

## Coupon and settings (all variants)

- Half-span b/2 = 25 mm (section, chord 0.12 m, NACA 0012 with TE blunted to 0.5 mm, round TE base radius 0.25 mm). Root at z = 0 on the symmetry plane, as in round 3.
- Every other setting is R3-M1b's, unchanged: Gmsh 4.15.2, Delaunay 3-D, 20 layers from 8 um at ER 1.2 (stack 1.49 mm), uniform 1.5 mm surface size, K1' TE strip (4 segments per quarter arc, span size 0.26 mm) and the K1 field at 0.26 mm, same domain box and sizes, 6 threads, ASCII write. One checkMesh per variant with the same options as round 3 (`-allGeometry -allTopology -writeSets vtk -writeFields (nonOrthoAngle cellDeterminant cellShapes faceWeight)`). No solver, no y+ screen.
- Budget: at most 6 mesh runs (4 planned, 2 spare for a void run caused by a generator defect, each void run recorded). Fan-out 0. No tuning of any knob beyond the four variants. Inside this step there is no K2, no structured generator and no cfMesh.
- The load gate before each mesh generation: 1-minute load below 30, polled every 30 s up to 20 min, then proceed and record the load.

## Variants

| Id | Geometry at the tip | Mesh change |
|---|---|---|
| V0 | The current revolve: the four tip section curves revolved 2 x 90 degrees about the chord line at b/2 (Gmsh example construction), as R3-M1b. | None. |
| V1 | The tip of record: the panel ends at a planar flat cut at z = b/2 (a planar cap bounded by the section curves). The DR-F2-4 TE radius (0.25 mm half circle) is kept. The cap-to-skin seams are convex edges of about 90 degrees. | None. |
| V2 | K4: a short flat at each pole before the revolve. The revolve is stopped short of the axis and closed by a planar half-disc perpendicular to the chord line. LE flat: the revolve starts at the section grid point x/c = 3.43e-4 (flat radius 0.396 mm, x = 41 um). TE flat: the aft arc is cut at 30 degrees from the chord line (flat radius 0.125 mm). The nose and aft arc pieces between the flats and the panel end at z = b/2 on planar caps. Span, S_ref and b are unchanged; the tip chord is 82 um shorter. | None. |
| V3 | V0 geometry. Stack height cut at a fixed first height 8 um, ER 1.2, 14 layers: stack = 0.474 mm (inside the 0.3-0.6 mm range; 32 % of 1.49 mm). | n = 14 instead of 20. |

The V3 layer-count criterion (at least 20 layers) fails by construction. V3 is a diagnostic for the tip classes only.

## Regions

First match wins, as `mesh-locate-r3.py`: tip_pole (within 2 mm of a pole point (0,0,b/2) or (c + r_TE, 0, b/2)), te (x at least c - 1 mm and wall distance below 2.5 mm), root (z below 2 mm), interface (a prism/tet face), tip (z above b/2), main. For V1 a face within the stack height (1.49 mm) of the cap-to-skin seam is also flagged `edge`; the flag does not change the primary region.

**Tip region** = every face above 70 degrees (or below weight 0.05) with z at least b/2 - 2 mm. That is tip_pole + tip + any te/main/interface face in the last 2 mm of span. The whole-coupon gate is also reported, but the root end of a 25 mm coupon is not a product tip, so root-region faces never count toward a tip decision.

## Metrics per variant (all Verified when read from a file; Inferred otherwise)

1. Faces above 70 degrees by region: tip pole, tip, main, TE (plus root, interface, edge) and the tip-region total.
2. Max non-orthogonality (degrees).
3. Faces with weight below 0.05 (count, minimum, region).
4. checkMesh result against DR-F3-1 A: the checkMesh validity set without the determinant test, non-orthogonality at most 70 degrees, skewness at most 4 internal and 20 boundary, volumes positive, face pyramids OK, face weight at least 0.05, layers 20 on at least 95 % and 10 on 100 % of wing faces (V3: reported, expected to fail).
5. Cells, prisms, peak RSS (checkMesh and Gmsh), wall times, load at start.

## B-rep and surface-mesh receipts per variant (section 4.2 item 4)

- `mesh-locate` by region and wall distance (the coupon locator, a copy of `mesh-locate-r3.py` with the variant geometry).
- Count of Gmsh "Skipping boundary layer extrusion of degenerate curve" lines (target 0).
- Watertight within the 10 um join tolerance: B-rep curves used by one wing surface only (excluding the root plane), and surface-mesh free edges (excluding the root plane); both must be 0.
- Measured continuity of the cap-to-skin seam: for every mesh edge shared by two wing triangles of different B-rep surfaces, the fold angle (180 degrees minus the interior angle between the two triangles); report max and median for tip seams (z at least b/2 - 1 um) and panel seams.
- Wall nodes within 10 um of the reference surface: the largest distance of any wing-surface node to the analytic reference (section profile on the panel; revolve for V0 and V3; planar cap plus profile for V1; revolve for V2, which differs from the reference by at most the flat, reported).
- Wall-triangle aspect ratio per region from the surface mesh: longest edge divided by the shortest altitude (equilateral = 1.155); report median, p95 and max.
- Low-weight face count and region (the 4 such faces of R3-M1b sit at the tip TE).

## V0 must reproduce first

"Reproduce the tip-pole and tip face classes" means all three hold for V0: (a) at least one tip_pole face and at least one tip face above 70 degrees; (b) tip_pole + tip is between 105 and 420 (0.5x and 2x of R3-M1b's 210); (c) the tip-pole faces lie at x/c within 0.98 to 1.00 or within 0.02 of the LE pole, low in the stack (median wall distance below 0.5 mm). If V0 does not reproduce, stop: V1-V3 are not run, and the verdict says "V0 did not reproduce" (plan §6 row S4 stop rule).

## Decision rules (plan section 4.2 item 3, verbatim, with the terms defined above)

Verbatim:

- V1 passes the tip region of DR-F3-1 A → adopt V1 and retire the DR-F2-4 tip exception.
- V1 fails at its convex edges and V2 passes → pole degeneracy (H-A) is supported, and K4 goes to the operator under the amended DR-F3-5.
- Counts fall with H in V3 and do not depend on pole treatment → fan or interference (H-B/H-C).
- All variants fail regardless of pole and H → stop and report. Inside this step there is no K2, no structured generator and no cfMesh.

Terms:

- **Passes the tip region** = in the tip region, zero faces above 70 degrees and zero faces with weight below 0.05, and no checkMesh error line (other than determinant) located in the tip region. The whole-coupon gate result is reported beside it.
- **V1 fails at its convex edges** = V1 does not pass the tip region and at least half of its tip-region failing faces carry the `edge` flag.
- **V2 passes** = V2 passes the tip region by the same definition.
- **Counts fall with H in V3** = V3's tip-region count is at most 0.5 x V0's. **Do not depend on pole treatment** = V2's tip-region count is at least 0.5 x V0's.
- **All variants fail regardless of pole and H** = V1, V2 and V3 each have a nonzero tip-region count and V3's count exceeds 0.5 x V0's.
- Rules apply in the order listed. If rule 1 fires, V2 and V3 are still reported but do not change the adoption. If no rule fires, the verdict is "not separated" with the reason. A cause named by a rule is Inferred from one mesh per variant, never Verified.
- Gmsh repeats bitwise on the same input (round 3). A single run per variant is therefore the whole sample, and a void run (generator defect) is repeated once.
- O5 stays rejected: a mesh below the floor never feeds a result shown as current.
