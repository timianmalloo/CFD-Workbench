---
id: decision-parametric-authority
title: One parametric definition, two editing views
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [geometry, curves, stations]
links:
  - {to: spec-cfd-workbench, rel: refines}
  - {to: kb-cfd-workbench-grounding, rel: depends-on}
review-by: 2026-12-19
summary: Curves and station profiles are coordinated views of one complete explicit surface definition. Detaching the initial recipe changes which parameters drive the shape, not whether the model is parametric.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
---

# One parametric definition, two editing views

The user wants simple direct shaping and a parametric account of the result. CFD-Bench already establishes one-way recipe generation and explicit stations. The new requirement to preserve editable spline anchors/levers means sampled station rows are insufficient: the connecting rules and their parameters must be part of the explicit definition too.

Decision proposed for iteration: five nonredundant channels (leading-edge offset, chord, elevation, twist, thickness), plus station profile assignments and loft/closure rules. The curve editor and station inspector change the same owned parameter. Profile blending normalizes intermediate thickness shape before the sole effective t/c channel is applied. Unit-bearing dimensions and curve handles continue to parameterize a directly edited model.

Rejected: synchronized independent curve/table models (competing truth), mesh-as-record (lost handles), automatic inverse fitting to the original six-number recipe (false exactness), general feature-tree CAD (unrequested complexity). Direct edits detach the starting recipe visibly and reversibly. Regeneration is a preview/new revision, never a silent overwrite.

This constrains save/load, geometry evaluation, analysis identity and export. The exact spline/loft algorithm and serialization are architecture decisions. Independent geometry review found and closed catalog-thickness ownership and intermediate-profile normalization conflicts; see the specification gate record.

Revision 0.2 extends that same decision to **Through points** and **Smooth · weighted controls**. The current user explicitly requested weights that influence a curve without forcing it through every control. In Smooth mode an influence ordinate is not a station readout; exact station dimensions become explicit constraints on the single evaluated distribution. Catalog sections open as editable source-linked drafts, and accepted edits create a modified revision. The section's effective thickness remains the one channel named by the preview's thickness policy. Cancel and Undo cover the complete change.

This is an **Inferred product interpretation**, reviewed against the user's words and existing source contracts. Constant mathematical curvature was rejected as the meaning of “constant curve”; the chosen meaning is continuously faired geometry. Exact evaluator, constraint solver and continuity evidence remain future design gates. The distinction reaches A3/A4, GEO-03/08/13/14, the section/outline mockups and their preview/acceptance checks.
