---
id: adr-0007-edit-transactions
title: "ADR-0007: one multi-step section draft per section-editor visit; a workspace gesture commits at its end; catalog Replace is a draft step"
type: adr
status: proposed
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [authoring-session, draft, undo, section-editor, gesture, catalog, adr, dr-4, dr-6]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0005-point-types, rel: relates-to }
  - { to: design-section-editor, rel: refines }
  - { to: architecture-application, rel: relates-to }
review-by: "none while accepted"
summary: >-
  The section editor holds one draft whose bytes advance through an ordered list of source-patch steps (moves, type
  changes, Replace, constructions); inner Undo pops a step, Cancel discards the draft, Finish applies it as exactly one
  accepted revision. A workspace point gesture (DR-6 default) is a draft opened at pointer-down and applied at release
  when certified. Catalog Replace (DR-4) reuses the as-built import fit as one draft step with its residual reported.
review-suggested:
  - { by: design-m12c-section-editor, on: 2026-10-03, reason: "M1.2c names the section-draft step record (SectionStep), the receipt (rail section) and recovery (rail section), the mode state machine, and the contract-step deletion of the M1.1 single-vertex members (seam S-3)." }
---

# ADR-0007: section draft, gesture commit and catalog Replace

- **Status:** Proposed (architect council 2026-09-26)
- **Date:** 2026-09-26
- **Deciders:** the operator (DR-6 is theirs; this ADR designs to the default), Computational Geometry (DR-4), Data &
  Persistence (history grain)
- **Context:** spec 1.6 A3.2 (draft invariant, 1.6 note), A4.15 undo contract (GEO-14), CAD-18, CAD-20; DR-4, DR-6;
  as-built `AuthoringSession`.

## Context

As built, one draft targets one vertex: `BeginProfileEdit` fixes `(side, vertexId)` on the draft
(`src/CfdWorkbench.Core/AuthoringSession.cs:361-385`) and `UpdateProfileDraft` moves only that vertex
(`AuthoringSession.cs:477-508`, `:488`). Each construction (`BeginProfileInsert/Delete/Fair/Rebuild/Import`,
`AuthoringSession.cs:158-168`) opens its own draft. One `Apply` appends one accepted row; history is the append-only
accepted-row chain with cursor facts (`AuthoringSession.cs:12-19`). CAD-20 needs a mode whose one Finish commits "however
many moves, type changes and Replace steps it held", with Undo inside the draft that never passes its entry. The v10
workspace commits a point drag when it ends (DR-6 default).

## Decision

1. **Section draft.** Entering the section editor opens one draft bound to (base accepted revision, assignment,
   target profile, scope Shared/Independent, thickness intent). The draft holds an ordered **step list**; each step is
   a source patch over the previous step's bytes with a kind (move, point type, tangent kind, Replace, Insert, Delete,
   Smooth/Fair, Rebuild, thickness proposal) and its own report (residual, deviation). Bytes are the only state — no
   step keeps a parallel profile model (ADR-0002; `section-editor.md` §3).
   - *Inner Undo/Redo* move a cursor over the step list and never pass the entry (bytes at entry = base bytes, plus
     the Make-independent copy when scope is Independent).
   - *Cancel* discards the draft: no accepted row, undo depth unchanged, assignments and source as at entry.
   - *Finish* validates the latest bytes (the full certificate) and applies exactly **one** accepted row whose receipt
     carries the step count. A crossing (`DSL-PROFILE-CROSS`) or Not assessed keeps Finish disabled.
   - *Save while drafting* keeps SRC-07: Save asks to Finish or Cancel first; nothing is silently discarded.
   - *Recovery* keeps the as-built row shape expand-only: the latest draft bytes and base, plus "N steps before
     recovery". Scope is derived from the bytes, not stored. The inner step history is session memory and is not
     recovered (**`simplify:`** ceiling: recovery resumes at the latest bytes with an empty inner Undo; upgrade trigger:
     an operator report of lost inner undo after a crash).
   - *History schema (expand-only, declared):* the as-built receipt admits exactly `draftId, generation, rail,
     vertexId` plus optional `intent` (`AuthoringSession.cs:871`), with `rail` from a closed set of eight values and one
     vertex. A section Finish, a typed dimension (ADR-0006) and a point-type change (ADR-0005) need new optional
     receipt fields and `rail` values; design-slice names them. A build that predates them refuses the project and
     leaves the file unchanged — `DOC-UNSUPPORTED-FIELD` for a new field (`AuthoringSession.cs:851`), `DOC-REFERENCE`
     for a new `rail` value alone (`AuthoringSession.cs:897-904`, `:949`) — the same one-way consequence as FoilDSL 4.1
     (ADR-0005). One fixture per new receipt kind asserts the actual code and the unchanged bytes.
2. **Session API is expanded, not replaced** (expand-migrate-contract in code): new members `BeginSectionDraft`,
   `ApplySectionStep(draftId, generation, step)`, `UndoSectionStep`, `RedoSectionStep`, `FinishSection`; the
   single-vertex members stay until the Desktop no longer calls them, then are deleted in one change with their tests.
   The one-draft rule (`DSL-DRAFT-OWNED`) is unchanged: a section draft is the one draft.
3. **Workspace gesture commit (DR-6 default).** Pointer-down on a point opens a draft (the existing rail/vertex draft);
   each move advances its generation; release validates and, when Certified, applies one accepted row; otherwise the
   draft is cancelled, the refusal is shown and geometry and undo depth are unchanged. Escape during a drag cancels.
   Keyboard nudges commit on key release, and also on focus loss and window deactivation, because Avalonia 11.3's
   `KeyEventArgs` has no repeat flag and a KeyUp can be lost to a focus change or a menu shortcut (native desktop
   council finding); a held-key repeat run is one gesture. Estimates during the drag read the draft bytes (ADR-0006).
   If the operator overturns DR-6, only the controller's release handler changes (no Apply until Return); Core is
   unchanged. Measured: the existing apply event with `edit_kind = gesture` (duration, outcome, certificate status).
4. **Catalog Replace (DR-4) is a section-draft step.** The chosen catalog or My sections entry's coordinates go through
   the as-built import fit (`AuthoringSession.cs:433-455`): first the edited profile's current basis, then a
   neighbour's basis (`NeighbourBases`, `:386-407`), then the own-basis 8–16 vertex fit (`DatImport.Fit`,
   `DatImport.cs:169-265`). The step records `ImportReport` (residual, vertex count, basis, provenance). Fitting on the
   current basis keeps point count, ids and point types, and takes the existing tangent rows as KKT constraint rows so
   the result still certifies (ADR-0005); the other two replace them (all interior points are Control points after a
   fresh fit, and the rows are dropped in the same step, reported). The chip text derives from the profile's provenance (ADR-0008). Original polars never
   attach to the modified profile. v10's "nose plus three points per surface" is mockup scope, not the record.
5. **Finding carried to design-slice:** the as-built acceptance compares the residual with 1e-5 in normalised chord
   (`DatImport.cs:255`, `:290`; `AuthoringSession.cs:445`), independent of chord; A4.6 says 10 µm at the station's local
   chord. The as-built rule is stricter on chords under 1 m; reconcile it to A4.6 with a test.

## Alternatives considered

- **Keep one draft per vertex and group them with a macro undo item.** Rejected: several drafts would each carry a base
  and a certificate, and Cancel would have to unwind accepted rows (history rewrite).
- **Store each step as an accepted row and squash on Finish.** Rejected: squashing rewrites append-only history.
- **Replace outside the draft (its own Apply).** Rejected: CAD-18/CAD-20 require Replace to be undoable inside the draft
  and committed with Finish.

## Consequences

- **Positive:** GEO-14's contract (Cancel adds no step; Finish adds exactly one) falls out of the structure; every step
  reuses the existing validated source-patch functions.
- **Negative:** the step list costs memory proportional to steps × source bytes (source ≤ 1 MiB, `application.md` §6);
  **`simplify:`** keep full byte copies; upgrade trigger: a measured draft over 64 MB.
- **Follow-ups:** design-slice owns the step record shape and the Desktop mode state machine.

## Amendment 1 (proposed 2026-10-03 by the M1.2c design-slice; pending Owner acceptance)

- **Change:** decision 1 says a section Finish applies one accepted row "whose receipt carries the step count" and that
  recovery records "N steps before recovery". **Both are struck.**
- **Why:** this is the Data & Persistence ruling at the M1.2c gate (OI-12C-2), on the DM15 dead-weight shape.
  - Nothing computes from a step count; the checker only validates it.
  - After a resume from recovery the count would be undefined.
  - The `section.finish` telemetry already answers "how many steps per Finish".
- **Receipt:** `rail "section"`, with `VertexId` holding the profile name at Finish, and nothing more.
- **Old builds:** they refuse it with `DOC-REFERENCE`, or with `DOC-SCHEMA` when a retained source carries a profile
  row; the actual codes are observed in `docs/proof/m12c-old-build/`. The file is unchanged.
- **Grain:** one accepted row is one Finish of a draft whose bytes differ from its base. A Finish whose bytes equal the
  base adds no row.
- **Reference:** `docs/design/m12c-section-editor.md` §3.3.
