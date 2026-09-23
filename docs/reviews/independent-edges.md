---
id: review-independent-edges
title: Independent foil edges — adversarial review
type: doc
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [foildsl, review, geometry, testing]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: spec-foildsl, rel: documents}
  - {to: mockup-workbench-v6, rel: relates-to}
  - {to: adr-foildsl-authority, rel: relates-to}
review-by: 2026-12-22
summary: Independent geometry, test and simplification gates for separately authored leading and trailing planform rails. Includes observed browser proof, resolved findings and bounded residual risks.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Independent foil edges — adversarial review

Reviewer: an agent separate from both the language author and mockup author. Date: 2026-09-22.
Scope: the user correction that moving either planform edge must leave the opposite edge unchanged.
The prior FoilDSL review remains applicable outside this correction.

## Contract and plan review

**Verified source inspection.** The old representation stored leading and chord. Thus a leading edit
also moved trailing. The revised record stores independent absolute leading and trailing x ordinates,
each with its own CV abscissae and knot vector. Chord is derived as trailing minus leading at the
same evaluated eta. Pointer mapping and the precision field now edit absolute aft position.

The reviewer required opposite-record equality through preview, Apply, Cancel, Undo/Redo and source
edits, including unequal bases and eta changes. A constant opposite rail was rejected as a weak
fixture because it hides evaluation with the wrong inverse abscissa. The resulting conformance
fixture and runtime oracle use nonconstant leading and trailing rails with different CV counts,
abscissae and knot vectors.

The reviewer also required an explicit compatibility disposition. The in-review 4.0 draft is
superseded openly; its former `chord cv` field is rejected. Adding old LE/chord ordinates is exact
only with the same degree, knots and abscissa control arrays. Other conversions need a proved common
representation or a measured fit and explicit acceptance. Source opening does not silently migrate.

**Verified scope distinction.** Independence refers to the authored unrotated planform rails.
Placed sections retain the established LE-pivot twist contract. At nonzero twist, moving LE can
therefore move placed 3D points on TE. The language specification states this distinction; this
review does not claim literal independence of every placed 3D boundary point.

## Evidence and findings

- [Edge oracle](../proof/independent-edges.json): six passing checks. Both directions use actual
  pointer movement in eta and ordinate, keyboard Apply, Cancel, Undo/Redo, and source edits.
  Opposite CV records, 401 evaluated samples and rendered top-view path remain unchanged. A root
  TE preview also keeps the opposite screen transform fixed. Positive TE controls with crossing
  rails are rejected, showing that positivity applies to the derived difference.
- [Independent runtime proof](../proof/independent-edges-review.json): four passing checks, zero
  page errors. A 1 m LE precision edit crosses TE and is refused through the actual numeric UI;
  accepted source, CVs, revision and run state remain unchanged. Keyboard Delete on either rail
  changes that rail's knot vector while retaining the opposite record, knots and 301 evaluated
  samples exactly. Accepted source re-parses to the current record.
- [Source regression](../proof/foildsl-browser-check.json): 13 checks and 15 layout/theme cells;
  zero page errors or external requests. [CAD regression](../proof/workbench-v6-browser-check.json):
  16 passing oracles, 77 measurements and 30 shell cells; zero page errors or external requests.
  These are bounded browser results, not scientific or native application conformance.
- **Resolved, Major, Verified:** the first legacy rejection was only a generic `Expected trailing`
  error, contrary to DSL-14. The final parser emits `DSL-LEGACY` and explicitly requires preserving
  the earlier draft and requesting conversion. The final oracle asserts the message as well as
  the code; the independent run observed it.
- **Residual, Minor, Verified:** the rendered 1280 × 800 Properties field wraps the longer
  `aft position` label across lines; its unit and helper text are close together. The field remains
  labelled, visible and usable by keyboard. This is polish, not an edge-independence blocker.
- **Residual, Minor, Verified:** the reviewed screenshot can show the inherited window title
  `r4 · Saved` while the status strip shows `r5 · edited` after a GUI edit. Source/revision state
  is correct in the executed oracle. Title freshness was reported to the author and is outside
  this narrow correction; it is not claimed repaired.

The reviewer visually inspected the rendered CAD screenshot, including both rail control polygons,
the absolute-position input, the opposite-fixed explanation, and the unchanged desktop workspace.
Root keeps the original screenshot location in the proof; screenshots are local review aids, while
the committed measurements and runnable artifact are the reproducible evidence.

## Hard-veto dispositions

```text
PERSONA: computational-geometry-expert   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Independent rail authority and own inverse-abscissa evaluation replace coupled LE/chord editing. evidence: normative §5/§6/§10; separate-record and opposite-sample oracles. fix: completed.
CLEARS-THE-VETO: yes — independent unrotated planform records, positive derived chord, compatibility limits and matching readers are specified and observed in the bounded prototype.
RESIDUAL RISK: Arbitrary nonuniform master knots, interval-certified positivity, exact migration and production geometry tolerances are not implemented or proved by this HTML artifact. LE-pivot placement is intentionally retained.
```

```text
PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) Opposite-edge invariance now covers different CV counts/knots, eta changes, accepted source and transaction history. evidence: red-before-green result in edge proof; six edge checks; four independent checks; source and CAD regression packs. fix: completed.
CLEARS-THE-VETO: yes — traced DSL-13/DSL-14 and SRC-11, observed final state, red-before-green evidence and attached Proof Pack support this scoped correction.
RESIDUAL RISK: Sampled browser geometry does not prove interval validity, native platform behavior or scientific correctness. The inherited title-status inconsistency remains recorded.
```

```text
PERSONA: the-simplifier   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [resolved Major] (Verified) One independently authored record per edge and a derived chord accessor avoid compensating control-index arithmetic or a third authoritative curve. evidence: M.cv.le/M.cv.te, chanAt('chord'), direct top-view mapping. fix: completed; obsolete chord-record comment and unused cage values removed.
CLEARS-THE-VETO: yes — the representation change is necessary for exact independence; no new runtime dependency, stack selection or production implementation was introduced.
RESIDUAL RISK: The prototype retains uniform-by-count master knots as an explicit subset; broadening that subset is a separate obligation.
```

Ready for the user's review of this correction. No deployment, merge or production approval is implied.
