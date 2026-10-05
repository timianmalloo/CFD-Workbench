---
id: proof-spike-03-tip-coupon
title: SPIKE-03 tip mesh coupon S4 — verdict
type: proof-pack
status: in-review
owner: "@trk-s4"
phase: spike
tags: [spike-03, gmsh, mesh-gate, tip, coupon, ruling-88]
links:
  - {to: proof-spike-03-tip-coupon-prereg, rel: implements}
  - {to: plan-tip-handling, rel: implements}
  - {to: proof-spike-03-round3, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-05
summary: >-
  All four coupon variants fail DR-F3-1 A in the tip region, so the pre-registered stop rule fires and S5 is not
  triggered. V0 reproduces round 3 (178 tip-pole and 37 tip faces above 70 degrees). V1, the tip of record, is far
  worse: 3,813 faces, 99 % at the convex cap edge, 263 negative-volume cells. V2 (short pole flats) leaves 312 faces
  and 63 negative-volume cells. V3 (stack cut to 0.47 mm) leaves 213, 5 % below V0. The cause is not separated;
  the stack-height fan reading is not supported. macOS arm64 only.
review-suggested: []
---

# S4 tip mesh coupon — verdict

Track S4, round-oct05 (Ruling 88 D5). Readings and thresholds are in [preregistration.md](preregistration.md), committed
before any mesh (with one arithmetic amendment, also committed before any mesh). Receipts are under
[receipts/](receipts/). Labels: **Verified** = read from a file in receipts; **Inferred** = my reading. Every mesh here
carries **TE blunted for analysis; tip variant for a mesh coupon only; no flow solution**. No physical result is reported.

## Result

**Rule applied: "All variants fail regardless of pole and H → stop and report."** (Verified: V1, V2 and V3 each have
tip-region failures, and V3's count 213 exceeds 0.5 × V0's 224 = 112.) Rule 1 did not fire (V1 does not pass). Rule 2 did
not fire (V1 fails at its convex edges, but V2 does not pass). Rule 3 did not fire (V3 did not fall to 0.5 × V0). The
cause of the V0 failure is **not separated**.

| Pre-registered reading | Outcome |
|---|---|
| V0 reproduces the tip-pole and tip classes | **Yes.** tip_pole 178 and tip 37 (R3-M1b: 176 and 34); sum 215 is inside 105-420; x/c 0.9867-0.9968 and 0.9803-0.9894 (identical to R3-M1b); median wall distance 67 µm. |
| V1 passes the tip region → adopt V1, retire the DR-F2-4 tip exception | **No.** 3,813 faces; the mesh is invalid. |
| V1 fails at convex edges and V2 passes → H-A, K4 to the operator | **No.** V1 fails at the edges (3,778 of 3,813 = 99 % carry the edge flag), but V2 does not pass. |
| V3 falls with H, independent of pole treatment → H-B/H-C | **No.** 213 vs 224 (−5 %) for a 3.2× lower stack. |
| All fail regardless of pole and H → stop | **Yes.** |

## Metrics per variant

All runs: 25 mm half-span coupon, R3-M1b settings, one checkMesh each. "Tip region" = faces at z ≥ b/2 − 2 mm
(preregistration). Faces above 70° = the `nonOrthoFaces` set, which checkMesh fills with the severe faces (70°-90°) plus
the non-orthogonality errors; V1 = 321 + 3,492, V2 = 136 + 176 (Verified, log.checkMesh).

| | V0 revolve | V1 planar flat cut | V2 pole flats | V3 stack 0.47 mm |
|---|---|---|---|---|
| Cells (prism + tet) | 322,666 | 309,031 (4,564 of shape type 0) | 328,143 (50 of type 0) | 269,771 |
| Faces > 70°: tip pole | 178 | 467 | 311 | 175 |
| tip | 37 | 0 | 0 | 29 |
| main | 10 | 3,328 | 1 | 9 |
| TE | 0 | 2 | 0 | 0 |
| prism/tet interface | 0 | 16 | 0 | 0 (1 flagged) |
| **Tip-region total** | **224** | **3,813** | **312** | **213** |
| of which at V1's convex edge (edge flag) | n/a | 3,778 | n/a | n/a |
| Max non-orthogonality | 85.24° | 174.5° | 176.1° | 85.24° |
| Faces with weight < 0.05 (min) | 0 (0.0602) | 250 (3.7e-5) | 29 (2.0e-4) | 0 (0.0506) |
| Negative-volume cells | 0 | 263 | 63 | 0 |
| Open cells | 0 | 4,959 | 318 | 0 |
| Max skewness | 1.82 | 2,834 | 1,223 | 1.76 |
| Wing faces without a layer column | 0 | 180 | 0 | 0 |
| **DR-F3-1 A gate** | FAIL (non-orthogonality only) | FAIL (6 of 7 checks) | FAIL (6 of 7 checks) | FAIL (non-orthogonality; layers < 20 by construction) |
| Gmsh "Skipping … degenerate curve" lines (target 0) | 4 | **0** | **0** | 4 |
| B-rep wing curves used by one surface, off root | 4 (all zero length) | 0 | 0 | 4 (zero length) |
| Watertight: free surface-mesh edges off root; node pairs < 10 µm sharing no edge | 0; 0 | 0; 0 | 0; 0 | 0; 0 |
| Seam fold angle at tip seams, max / median | 47.9° / 9.2° | **90.0° / 90.0°** | 90.0° / 9.7° | 47.9° / 9.2° |
| Wall nodes off the reference surface, max | 4.7 µm | 4.7 µm | 33.5 µm (2 nodes > 10 µm, the flats by design) | 4.7 µm |
| Wall-triangle aspect ratio, tip pole: p95 / max | 6.3 / 35.8 | 3.0 / 3.0 | 3.3 / 13.6 | 6.3 / 35.8 |
| Gmsh wall, peak RSS | 2.8 s, 317 MB | 2.2 s, 310 MB | 2.5 s, 315 MB | 2.7 s, 302 MB |
| checkMesh wall, peak RSS | 3 s, 390 MB | 3 s, 372 MB | 3 s, 388 MB | 2 s, 320 MB |
| 1-minute load at start (limit 30) | 4.2 | 5.3 | 5.6 | 5.6 |

Locations (Verified, `locate.txt`): V0 and V3 tip-pole faces sit at x/c 0.9867-0.9968 at median wall distance 67 µm and
64 µm (low in the stack, as R3-M1b). V1 faces are within the stack height of the cap-to-skin seam, across the whole chord
(x/c −0.006 to 1.011). V2 tip-pole faces are at x/c −0.004 to 1.011, median wall distance 256 µm.

## Reading (Inferred unless stated)

1. **The stack-height fan reading (H-B/H-C) is not supported.** Cutting H from 1.49 mm to 0.47 mm at fixed first height
   left the tip-pole and tip faces at the same x/c and the same wall distance, and the count at 95 % of V0 (Verified).
   The fan mechanism predicts a fall with H. A fall below 0.5 × V0 was the pre-registered test.
2. **Pole degeneracy (H-A) is not separated.** V2 removed the degenerate curves (0 Skipping lines, 0 free B-rep curves;
   Verified) and still failed, with invalid cells at the flats. V2's flats (LE radius 0.278 mm, TE radius 0.125 mm) are
   smaller than the stack (1.49 mm) and than the 1.5 mm surface size. So V2 tested "no degenerate curve", not "a flat
   larger than the stack". Whether a larger flat clears the faces was not tested (no tuning inside this step).
3. **V1, the tip of record, is not meshable with this route at this recipe.** 99 % of its failing faces are at the
   convex cap edge, and 263 cells have negative volume (Verified). The built-in `extrudeBoundaryLayer` has no corner
   treatment, and a 1.49 mm stack at a 90° edge inverts cells (**Inferred**, not isolated: no variant changed the layer
   recipe at the edge). Ruling 93's tip of record therefore has no meshable closure in this route today.
4. **The persistent class is near the TE end of the tip, low in the stack, and independent of H** (V0 and V3 agree to
   within 5 %). The coupon cannot say whether the cause is the revolve's small radius there (0.25-0.6 mm), the
   transfinite TE strip meeting the dome, or layer-normal behaviour at the dome. That reading is **Flagged**, not tested.
5. O5 stays rejected. No mesh here feeds a result shown as current.

## Next step S5 implies

S5 as written ("V1 passes → one AR 8 run on the tip of record; V1 fails and V2 passes → operator rules DR-F3-5, then one
AR 8 run on K4") is **not triggered**. Neither precondition holds, so no AR 8 run is justified and round 3 stays NO-GO at
AR 8 (R3-M2..M4 not run). The next step needs an operator ruling, because every option is outside Ruling 88 D5's V0-V3:

1. **Zero mesh cost, first:** read the existing V0 and V3 meshes for the persistent faces (layer index, cell shapes,
   normals at the dome near x/c 0.99). It separates the three readings of point 4 from files already in hand.
2. **One more pre-registered coupon, if the operator approves:** V2' with pole flats at least as large as the stack
   (radius at least 1.5 mm), the one untested reading of H-A. Same settings otherwise.
3. **Corner treatment for V1:** the tip of record needs a boundary-layer route that handles a 90° convex edge (the plan's
   K2 normals field or a structured generator, both excluded inside S4). The alternative the plan names is a rounded cap
   (§4.3 item 3), which Ruling 88 does not allow without an operator decision.

## What was not possible or not done

- No variant could be tuned or repeated by rule (no tuning beyond four variants). One launch attempt failed before Gmsh
  because `runs/` did not exist in the new worktree; it left an empty directory and no mesh. It is not a run.
- Hydrodynamicist and CAGD reviewers did not run (fan-out 0). The B-rep predicates in the table are mine, from
  `cases/tools/make-tip-coupon-gmsh.py` and `tip-surface-receipts.py`.
- The 4 low-weight faces of R3-M1b (tip TE) did not recur in V0 (0 faces, minimum 0.060). The coupon domain is smaller
  in span only, so this is a difference between coupon and wing that I did not explain.
- The seam fold angle includes mesh faceting: the TE arc has 4 segments per quarter arc (22.5° each), so a fold of 20-48°
  at TE seams is faceting, not a geometric corner. Read V1's 90° against V0's 9.2° median, not against zero.
- V2's wall-node deviation (33.5 µm) is the flats, which differ from the revolve reference by design.
- The coupon ran in about 3 s and under 0.4 GB per variant, against the plan's estimate of 1.5 min and 2 GB (Estimate
  from M1b). The estimate scaled the 4.5 M-cell wing; the coupon has 0.27-0.33 M cells.
- One shared file changed outside the new ones: `cases/tools/of-run.sh` reads `CFDW_MAX_LOAD` (default 10, unchanged for
  every existing caller) so the coupon driver can apply the session's load limit of 30.
