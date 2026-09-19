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
