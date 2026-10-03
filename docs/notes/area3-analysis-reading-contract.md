---
id: note-area3-analysis-reading-contract
title: Analysis reads the accepted revision through the snapshot and one Core read, never ProfileAt
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [analysis, reading-contract, placement, draft-safety, area-3]
links:
  - {to: design-area3-analysis, rel: refines}
  - {to: adr-0010-one-placement-rule, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2027-04-03
summary: >-
  Analysis takes the accepted bytes, AcceptedId and SurfaceHash from AuthoringSession.Snapshot(), never ProfileAt
  (which may return a profile draft), and takes every lattice coordinate from one new Core read, Placement.Sections,
  built on the existing internal placement rule — so analysis can neither draw over a preview nor disagree with the
  drawn foil.
---

# Analysis reads the accepted revision through the snapshot and one Core read

**Decision (proposed, design-area3-analysis §4).** Area 3 reads geometry only through
`AuthoringSession.Snapshot()` (accepted bytes, `AcceptedId`, `SurfaceHash`) and one new public Core read,
`Placement.Sections(source, etas, xs)`, which returns the placed camber points, normalised camber and thickness, and the
analytic camber slope, all computed in Core with the existing internal `PlacementRule` and `ProfileEvaluator`.

**Why.** Verified in code on 2026-10-03: `ProfileAtCore` (`AuthoringSession.cs`:527–537) reads `draft.Bytes` when a
profile draft is open on that assignment, so an analysis that called it could compute over a preview (ANA-22 forbids
it). `PlacementRule.Place` is internal (`Placement.cs`:83–90); without a Core read, Analysis would re-implement the §6
placement — a second geometry authority (ADR-0010, class GEO-B). The computational-geometry lens added that camber and
thickness must come from the `Section/Blend` outputs (camber (zu + zl)/2, thickness zu − zl), not from `Components`.

**Consequences.** An architecture test bans edit verbs and `ProfileAt` in the Analysis assembly; a bitwise test ties
`Place(Sections)` to `Surface`; the run manifest records the evaluator id and a placement-rule version so a rule change
invalidates runs by key.
