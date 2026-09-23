---
id: mockup-workbench-v6
title: CFD-Workbench v6 — FoilDSL and spatial authoring
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, foildsl, cad, language]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: design-foildsl-authoring, rel: refines}
  - {to: mockup-workbench-v5, rel: supersedes}
  - {to: review-foildsl-independent, rel: relates-to}
review-by: 2026-12-22
summary: The four-viewport workbench gains a FoilDSL source document, validation, shared transactions, file round-trip and revision freshness. A bounded language prototype, not the product evaluator or a CFD solver.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions." }
---

# Workbench v6: one definition, two authoring views

[Open the interactive mockup](workbench-v6.html) · [Product specification 1.4](../specs/cfd-workbench-v1.html)
· [Normative language](../specs/foildsl.md) · [Reference reconciliation](../notes/foildsl-reconciliation.md)
· [Independent review](../reviews/foildsl-independent.md)

Open the HTML directly; no build, server, network, key or solver is needed. The design remains the
expert ParametricWorkbench: four CAD viewports, precision Properties, document tabs, internal panel
scrolling, seven product areas and the existing review harness. FoilDSL is an alternate authored view.

## Review walkthrough

1. Choose **FoilDSL** beside the foil document. Read the source, shared-profile schedule and accepted
   geometry label. The initial source converts the existing illustrative sample with explicit normalization.
2. Change `half_span 0.55 m` to `half_span 0.6 m`. **Preview** shows the candidate; the accepted
   revision and run remain unchanged. **Apply** updates geometry and marks the illustrative run Historical.
3. **Undo**, then **Redo**. Source, span and geometry move together. Add a comment and Apply: the
   source changes without inventing a geometry revision or new simulation.
4. Use the failure-state selector: invalid syntax, incomplete text, invalid geometry, unsupported v3.
   Apply is disabled and the accepted shape stays visible. **Cancel** recovers the accepted source.
5. **CAD view** returns to the four viewports. Edit a control, Apply, and return to FoilDSL to see
   the changed value. A geometry edit with an outstanding source draft is refused.
6. **Save .foil**, then **Open .foil** using the downloaded file. This is browser file serialization;
   native project-archive atomic saves and crash recovery remain product gates. At the reflow size,
   file/history actions live in **File & history**.

## Demonstrated subset and limits

The prototype accepts a foil with one shared inline profile, degree-3 uniform-knot master curves,
degree-5 upper/lower sections with their actual separate knot vectors, explicit units, ordered
stations, open foil tip and all-or-none root mirror locks. It rejects unsupported constructs rather
than dropping them. The normative language additionally specifies standalone section import, multiple
profile assignments, assets, stable IDs, detailed locks/assertions, point tips and full geometry proof.

The section editor changes the shared profile and the body reads it; it is not independent per-station
profile editing. The existing tip pin is explicitly **session-only**, not a persisted FoilDSL constraint.
Visual edits preserve comment text but normalize source formatting; production lossless source patches
are specified, not claimed implemented. Source emission uses SI and round-trip numeric text.
Prototype freshness compares semantic records; it does not implement production BLAKE3 hashing,
immutable archive history or migration. Undo's visible revision numbers are fixture navigation.

The evaluator is illustrative: section crossing checks sample 199 interior positions; profile thickness
normalization is sampled; global foldover, certified positivity and full numerical identity are not proven.
Acceptance says **Valid prototype subset**, not scientifically safe. The 3D cage displays source controls
through the skin's camber/thickness mapping; it is not a production surface control net. The supplied
reference files remain unchanged and are not product code.

## Evidence

The source-control check failed on v5 before v6 was authored. `tools/check-foildsl.mjs` exercises source
actions, malformed inputs, generated round-trip laws, shared section/3D readers, real file download/reopen
and source layouts. Its report is [browser proof](../proof/foildsl-browser-check.json). The inherited CAD
oracle is run against v6 separately. Independent review distinguishes observed browser results from
native accessibility, full language conformance and scientific validation.
