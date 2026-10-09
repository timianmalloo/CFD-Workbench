---
id: mockup-rail-comb
title: Rail comb — scale, density, radius readout and monotone-piece count on the planform rails
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, planform, comb, curvature, radius, monotone, oi-5, ruling-193, ruling-107, operator-show]
links:
  - { to: design-rail-comb, rel: documents }
  - { to: design-m12c-section-editor, rel: relates-to }
  - { to: design-language, rel: depends-on }
  - { to: spec-cfd-workbench-v1, rel: implements }
review-by: 2026-12-31
summary: >-
  One self-contained page for the operator's visual yes before any build: the Plan view of a 900 mm half-wing with the
  curvature comb on both rails, a live stage (pointer and arrow-key probe, scale and density steppers, planform, area,
  selected point, narrow and theme switches), the same planform at two scales, the monotone-piece ticks on a fair and a
  wobbled rail, the hard states (straight, corner, clipped, overlays, Analysis, comb off, selected control point, no
  foil), and density. Every number is computed in the page from cubic B-spline rails; positions are scripted, not captured
  from the product.
---

# Rail comb

**Revision 2 (track CMR, Ruling 194):** approved look kept; changed only where a reviewer condition needs it: viewport focus-ring token,
`[` `]` point walking in place of the arrow probe (a labelled harness slider stands in for the pointer), aria-disabled steppers that keep
focus, the open plate in flow at narrow width with an open-state card, tick stations in text on the pieces line, the threshold as a plate
line (tau / L per rail), extrema by root-finding with one-sided anchor values, a G1 planform, the comb envelope per piece. Audit results:
[`adversary.md`](../proof/cmb/adversary.md).

Open [`rail-comb.html`](rail-comb.html) over `file://`. The harness (top right) switches planform, area, selected point,
curvature, narrow width and theme; the strip under the title is the in-page audit (Auto p90 tooth = 30 px, analytic radius
against a three-point circle, straight rail reads 1 piece, the plate covers nothing, targets at least 24 px, contrast,
placeholder scan). Design note: [`docs/design/rail-comb.md`](../design/rail-comb.md). Adversary record:
[`docs/proof/cmb/adversary.md`](../proof/cmb/adversary.md).

## Direction brief

- **Who and state on arrival:** the one foil designer, fairing the outline after a drag.
- **Job:** read the curvature along each edge, change the reading scale without touching the foil, get a radius at a place
  and a count of pieces per rail.
- **Archetype:** unchanged (G1 Parametric Modeling Workbench); diagnostics inside an existing viewport.
- **Adjectives:** quiet, quantitative, honest (not decorative, not a score).
- **Anti-goals:** no fairness grade, no new document state, no new single-key shortcut.
- **Triggered standards:** UI-T1 expert/quantitative fires (technical-ui-design); UI-T4 native client fires (Avalonia), so
  this HTML is not native accessibility proof; UI-T2 generated assets and UI-T3 model-backed do not fire.
