---
id: adr-foildsl-authority
title: FoilDSL is the authored surface definition
type: adr
status: in-review
owner: "@timianmalloo"
tags: [foildsl, geometry, persistence, authority]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: decision-parametric-authority, rel: supersedes}
  - {to: control-vertex-workspace, rel: relates-to}
review-by: 2026-12-22
summary: Proposed adoption of a versioned FoilDSL control-vertex language as the sole authored shape representation, with concrete source preservation, deterministic semantic identity and append-only project revisions.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
---

# ADR 0002: FoilDSL is the authored surface definition

**Status: proposed for human review.** This decision replaces the representation ambiguity in the
earlier parametric-authority note; it preserves the five channels, control vertices and section model.
It does not select an application language, toolkit, database, geometry kernel or simulation backend.

## Problem and candidates

The user requires the supplied language or its evolution to become the core artifact. A4.1 requires
that a surface payload alone reproduce the shape. GEO-03/05 and CAD-04–08 require direct edits of
control vertices, not a sampled approximation. The supplied JSX interpolates anchors and emits rounded
numbers; direct adoption would alter both geometry semantics and editing behavior.

Considered: (A) make the supplied v3 program authoritative and replace CV editing with its anchor
model; (B) keep the old record and offer DSL as an import/export view with two independently writable
models; (C) evolve the language to carry the existing explicit CV record and let both editors transact
the same parsed definition. Choose C. A loses existing accepted UX commitments; B admits divergence.
The strongest argument for A is its compact, approachable authoring. Retain its named blocks, readable
station schedule, live diagnostics and integrated explanation; future concise constructions must expand
through a reviewable transaction rather than acquire a second hidden geometry authority.

## Model and durable representation

Design owns the Foil definition, its profile definitions and assignments. The **accepted definition**
is the single semantic record. Its lossless concrete source preserves comments, names and formatting;
the AST, control polygons, station readouts, mesh and derived dimensions are interpretations or caches.
Any cache may be discarded and rebuilt from source and pinned assets. Editing a control issues an AST
edit and source patch as one transaction; a text draft does not mutate accepted geometry.

One **source revision** is one accepted source edit, including trivia-only edits. One **Design revision**
is one accepted design-semantic change. One immutable **Surface revision** identifies one evaluated
surface definition under one language/evaluator version. Profile revisions remain immutable and
referenced; package embedding is transport, not a second independently editable copy. Apply resolves
all references, validates and atomically appends the revision before selecting it. Undo/Redo move the
active history cursor; they never erase historical runs or rewrite past revision payloads.

The native `.cfdw.json` envelope remains the project carrier: versioned FoilDSL source revisions,
profile/source assets by content identity, history, setup/goals and run manifests. It must not contain
an independently writable duplicate of the same geometry fields. The interchange `.foil` program is
shape-only, never a substitute for a project archive containing analysis evidence. Standalone section
documents use the normative language's section form. Unresolved assets stop Apply, without network fetch.

This is a reasoned DM13 alternative to relational dimensions/facts: local document snapshots plus
append-only history fit a portable single-user project and require no database selection. The grain
is one source revision per accepted text/visual edit, one design revision per semantic edit, and one
analysis fact per run key. Geometry parameters and ratios are non-additive; source byte counts and
operation durations may be summed across distinct operations. Geometry, evaluator and asset attributes
are immutable/versioned (Type-2 semantics); viewport layout is replaceable preference state. Current
reads follow the history cursor, with rebuildable parsed/evaluated caches. No analytical shadow store.

## Contracts, failure and trust boundaries

The normative language companion owns normalization and identity. Raw source and semantic hashes have
different purposes. Formatting/comments do not invalidate runs. Exact shortest-round-trip numerics
replace the reference's rounded emitter; a 32-bit UI checksum is not a production identity.
Language version and evaluator version are part of the identity boundary. Unsupported version,
unknown field, unresolved asset, invalid curve or incomplete draft cannot produce a partial acceptance.

The source is data: no eval, file includes, shell calls, network imports or implicit code execution.
Bound input size, token count, nesting, profile/station/CV counts and validation time before evaluation.
Diagnostics carry code, source range, cause and repair. A failed or stale validation result cannot
enable Apply. Save writes a new archive atomically in the product; the mockup's downloadable file
demonstrates serialization, not native filesystem atomicity or crash recovery.

For operators: `language.parse`, `geometry.preview`, `document.apply` and `document.save` record input
bytes, language/evaluator version, base revision, elapsed time, outcome and stable diagnostic code;
no full user source in telemetry by default. Geometry and source changes have distinct events.
Absent measurements read “not recorded”. The mockup displays its measured validation duration and
input bytes; this is not a native product performance claim.

## Architecture boundary and delivery

Logical path: source/document store → deterministic parser and validator → accepted definition →
geometry evaluator → visual/text projections; proposal/draft → validation → explicit Apply feeds back
to the store. Analysis reads accepted revision identity only. LOA allocation at this seam is T0
deterministic mechanics; optional existing assistance proposes typed edits and cannot bypass Apply.
No new model call, distributed service or external SDK is introduced. A fully agentic editor and
independently synchronized stores are rejected because neither buys this requirement.

Future vertical slices (not implemented here):
1. Open → validate → inspect → save/reopen a foil on both OSes, with fake analysis explicitly labelled.
2. Text and CV edits → one draft → Apply/Cancel/Undo/Redo → source and geometry equivalence.
3. Section assets and historical run identity → immutable pinning → analysis freshness comparison.
Each slice must have end-to-end automated fixtures and the same human walkthrough as its product flow.
This task supplies only the specification and bounded browser demonstrator.

## Evidence and remaining obligations

Verified source conflicts and migration decisions are in the reconciliation note. The language grammar,
examples and independent gate define the review contract. Production cross-platform numeric identity,
full constrained geometry validation, archive migration/crash recovery and native accessibility remain
release gates. No solver or fabrication validity follows from accepting a language document.
