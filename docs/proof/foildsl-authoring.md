---
id: proof-foildsl-authoring
title: FoilDSL authoring specification and mockup proof
type: proof-pack
status: in-review
owner: "@timianmalloo"
tags: [foildsl, proof, specification, mockup]
links:
  - {to: spec-foildsl, rel: documents}
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: mockup-workbench-v6, rel: documents}
  - {to: review-foildsl-independent, rel: relates-to}
  - {to: examples-foildsl, rel: relates-to}
review-by: 2026-12-22
summary: Executed browser and documentation evidence for the bounded review artifact, independent findings and explicit production obligations; no scientific or full-language certification.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Review artifact proof

**Scope:** product revision 1.4, proposed normative FoilDSL 4.0, interactive workbench v6.
The three supplied v3 references are tracked byte-for-byte; their executable checker observations
remain separate from normative intent in the [reconciliation](../notes/foildsl-reconciliation.md).

**Verified:** the [source oracle](foildsl-browser-check.json) exercises editable source, supported
fixtures, 64 generated round-trip/idempotency cases, both recipe generators, malformed input,
four recovery states, preview/apply/cancel/history, actual download/reopen, shared validation,
profile-to-skin reach, metadata preservation and fifteen layout/theme cells. Initial missing-source
RED was observed on v5. The [preserved CAD oracle](workbench-v6-browser-check.json) covers sixteen
groups, 77 measurements and thirty shell cells. Neither browser run made network requests or
reported a page error. Commands are `node tools/check-foildsl.mjs <node_modules>` and
`node tools/check-mockup-v6.mjs <node_modules>`; Playwright is a local
verification tool, not a mockup dependency.

The independent-edge correction adds [six transaction/invariance checks](independent-edges.json)
and an [independent review](../reviews/independent-edges.md). The previous source-plus-chord model
failed the new oracle: moving LE moved TE by 1.602926 mm. Authored independent rails now leave
the opposite controls and sampled planform exactly unchanged; chord is only their difference.

**Verified:** the [independent review](../reviews/foildsl-independent.md) records fourteen findings,
resolutions, actual negative GUI interaction and rendered screenshot inspection. Its
[measurements](foildsl-independent-browser.json) include the corrected knot payload and sampled
initial normalization deviation. [Craft findings](ui-craft-findings-v6.json) are fourteen Minor
copy/density/heuristic findings, with no blocking craft result; this is a floor, not a verdict.
The design lint, craft gate, rendered-spec content/hash/layout checks and repository documentation
check are part of the closing verification. Rendered evidence:
[product](spec-html-check-cfd-workbench-v1.json), [language](spec-html-check-foildsl.json).

**Unverified production obligations:** complete v4 parser/evaluator and certified geometry,
stable entity identifiers and BLAKE3 identity, lossless syntax-tree patching, native project archive
transactions/crash recovery/migrations/assets, OS accessibility and native Windows/macOS behavior,
solver provenance integration and scientific validation. The prototype uses bounded sampled
checks and one inline shared profile; unsupported valid constructs are rejected explicitly.
It reformats source on visual acceptance while retaining comments. Its tip pin is session-only.
Its source semantic comparison and undo are interaction demonstrations, not production identity
or append-only persistence implementations. See the [walkthrough and limits](../mockups/workbench-v6.md).

**Human review decisions:** accept or revise the v4 break from v3; the explicit CV/knots and fixed
section-frame contract; one-source/one-draft transaction design; and migration/normalization policy.
The application stack and simulation backend remain unselected. No deployment or merge is part of
this proof. The isolated branch is retained for review.
