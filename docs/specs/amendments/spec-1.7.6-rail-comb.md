---
id: spec-amendments-1-7-6-rail-comb
title: "Spec 1.7.6 amendment batch — the rail comb on the planform rails (A4.9), as exact text"
type: spec
status: accepted
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, rulings, cad, comb, curvature, planform, ruling-196]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-5, rel: relates-to}
  - {to: design-rail-comb, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  Five amendments to cfd-workbench-v1 A4.9, traced to Ruling 196 (AM-RC-1 to AM-RC-5 approved as written) and the design
  rail-comb.md (Rulings 193 and 194). Revision 1.7.6 of the spec carries the batch; the change record is Appendix H, section
  H.6. The copy rows are DESIGN.md COPY-458 to COPY-473.
---

# Spec 1.7.6 amendment batch — the rail comb

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** **approved** (Ruling 196, 2026-10-09: "approved as written").

## How to read this

- **One row is one amendment**, in the form of [spec 1.7.5](spec-1.7.5.md). *Before* quotes the 1.7.5 text exactly; *After* is
  the exact text inserted in revision 1.7.6 (struck text stays in place with `~~`).
- **Source:** Ruling 196 (AM-RC-1 to AM-RC-5); the design [rail-comb.md](../../design/rail-comb.md), section 7.
- The copy rows are DESIGN.md section 7, COPY-458 to COPY-473 (Ruling 196 numbered COPY-RC-1 to COPY-RC-16 in order).

## Amendments (5)

| ID | Spec clause (the 1.7.5 spec) | Before (quoted) | After (exact new text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.6-1 | A4.9, the comb sentence | `it **scales to its longest tooth and never clips**, its scale is a stepper, and the **monotone-piece count** beside it counts the pieces of the curvature plot on which κ is monotone (sign changes of dκ/dη with a dead band — Farin–Sapidis), never inflections.` | the bold clause struck, then *(1.7.6, Ruling 196, AM-RC-1: its Auto scale fits the 90th-percentile tooth of both rails to 30 px; a tooth over 60 px is clipped and marked ×, and the plate counts the clipped teeth)*; after "its scale is a stepper": *(1.7.6, AM-RC-1: on the planform rails the scale is Auto or a fixed step stated as "30 px = N per metre"; Auto is held during an edit gesture and refits once at its end; density is 16, 32, 64 or 128 teeth per rail at an even arc-length pitch; hovered κ is signed (+ convex, LE +κ and TE −κ) and the radius is the outline's curvature in the plan, read in the Tracing strip; the monotone count is on signed curvature, ends a piece when κ reverses by more than τ / L (τ = 0.02, L the rail's arc length, the resolved value printed), counts extrema not inflections, and is a reading, not a grade)*; "dκ/dη" struck and followed by *(1.7.6, AM-RC-1: dκ/ds, with the threshold stated)* | Ruling 196 AM-RC-1 | approved |
| AM-1.7.6-2 | A4.9, the recorded deviation | `the planform rails' physical curvature along the normal is a product option, not a 1.3 promise.` | the same, then *(1.7.6, AM-RC-2, Ruling 196: the rail radius is physical, the curvature of the outline in the plan; the deviation for master curves stands.)* | Ruling 196 AM-RC-2 | approved |
| AM-1.7.6-3 | A4.9, new sign convention | (no statement of sign) | *(1.7.6, AM-RC-3: positive curvature means convex on both rails: κ_display = +κ_raw on the leading edge and −κ_raw on the trailing edge.)* | Ruling 196 AM-RC-3 | approved |
| AM-1.7.6-4 | A4.9, new focus order (an addition to DR-NAV-1 of m12b-points.md) | (Tab from a selected point goes to Properties' first value) | *(1.7.6, AM-RC-4, an addition to DR-NAV-1: when the comb plate is shown, Tab in the plan goes to the plate, then to Properties, and Shift+Tab reverses; the keyboard reading of a rail is point walking with [ and ], not the arrow keys, which nudge.)* | Ruling 196 AM-RC-4 | approved |
| AM-1.7.6-5 | A4.9, new anchor wording ("C1 only" becomes "G1 only") | (no statement of an anchor whose curvature jumps) | *(1.7.6, AM-RC-5: a smooth anchor, a tangent angle jump of 0.1° or less, whose curvature jumps is reported as "G1 only" and "curvature jumps at this anchor"; a corner is a measured tangent jump above 0.1°, not the authored tangent kind.)* | Ruling 196 AM-RC-5 | approved |

## Note on the build

The build measures a corner by the tangent angle between the two one-sided derivatives at a repeated knot (above 0.1 degrees),
never by `TangentKind` (`Planform.ReadAt`, `StationKind.Corner`). The existing Core tests that named a corner by its authored
kind now name it by the measured angle.
