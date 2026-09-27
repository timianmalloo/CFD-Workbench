---
id: adr-0008-section-library
title: "ADR-0008: the Profile catalog is bundled and read-only; My sections is a folder of immutable, create-only section documents"
type: adr
status: proposed
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [catalog, section-library, my-sections, persistence, provenance, rights, adr, dr-8]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: depends-on }
  - { to: decision-catalog-admission-classes, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Designs to the DR-8 default location. The catalog is a bundled, read-only set of GEN coordinate sets with generator
  and hash; VEND and LINK rows ship metadata only. My sections is a per-user folder of create-only standalone FoilDSL
  section documents named by their SHA-256, each carrying its own name and a flat provenance (origin plus a modified
  flag); the current library is a folder scan. Rights class is derived from the origin. A foil that uses an entry gets
  an inline copy — a stated deviation from the spec's content-addressed pin.
review-suggested: []
---

# ADR-0008: Profile catalog and My sections

- **Status:** Proposed — architect council 2026-09-26, repair cycle 1
- **Date:** 2026-09-26
- **Deciders:** the operator (DR-8 is theirs; this ADR designs to the default location), Data & Persistence (store),
  Security (rights), Computational Geometry (conversion, ADR-0007)
- **Context:** spec 1.6 A3.1 (My sections), A3.2 (Section library aggregate), A4.10, A4.13, A4.15, CAD-18, CAD-19; DR-8;
  `decision-catalog-admission-classes`.

## Context

No catalog exists in code (a search for `naca` and `catalog` under `src/` finds only the Example's comment,
`src/CfdWorkbench.Desktop/Assets/example.foil`:2). DAT import produces an inline profile block with provenance and a
residual (`DatImport.cs:169-291`; `AuthoringSession.cs:408-476`). FoilDSL has a standalone section form that carries a
name (`section`, string, …) and an optional `provenance` string on a profile body (`foildsl.md`:141-145), and it
addresses standalone section bytes by SHA-256 (`foildsl.md`:229-233). The native envelope has no asset set:
`Envelope(Format, ProjectId, Sources, Designs, Accepted, Cursors, Recovery)` (`AuthoringSession.cs:19`). There is no
installation preference or library store (no `SpecialFolder`, `ApplicationData` or `Preferences` use under `src/`).
CAD-19 asks for save, a unique name ignoring case, availability in another foil, and survival of Cancel and Undo. No
clause asks to rename or remove an entry.

## Decision

1. **Profile catalog (bounded context, read-only).** Bundled as an embedded resource: GEN entries as generated
   coordinate sets with generator id, version and output hash (A4.10); VEND entries pending terms and LINK entries as
   metadata only — **no coordinates are shipped for VEND-pending or LINK**, so "disabled but reachable" (CAD-18) cannot
   leak copyable coordinates. Conversion into the record happens at Replace (ADR-0007).
2. **My sections (Section library aggregate, installation scope — DR-8 default).**
   - *Location:* `<per-user application data>/CFD-Workbench/sections/` (macOS `~/Library/Application Support`, Windows
     `%APPDATA%`), resolved by one function; tests inject a temporary root.
   - *Entry:* one immutable standalone FoilDSL section document per entry, holding its own name and provenance, stored
     as `<sha256 of its bytes>.foil`. Published create-only with the as-built same-directory atomic pattern
     (`application.md` §6); the bytes are re-hashed after publication and must match the file name.
   - *Current library = a folder scan.* No index or facts file is kept (derive, don't store). The scan parses each file
     with the FoilDSL limits (SRC-09) and reports, never hides: a file whose hash does not match its name, a parse
     failure, or two entries whose names collide.
   - *Invariant:* names are unique among entries ignoring case — Unicode NFC, then `StringComparer.OrdinalIgnoreCase`.
     Save takes the as-built exclusive claim, re-scans under it, checks the name, publishes, then releases. A
     two-process duplicate-save test proves it.
   - *Out of scope:* rename and remove (no spec clause). A user who deletes a file by hand simply loses that entry.
   - *Independence from documents:* library writes are outside document history, so section Cancel and document Undo
     never remove an entry (CAD-19).
   - *DAT imports* (CAT-02) are saved as entries with `dat` origin (DR-8 default).
   - *Platform refusal:* where the store's publication is not proved for the platform (`DOC-UNSUPPORTED-PERSISTENCE`,
     ADR-0004), Save to My sections is refused with that reason and nothing is written.
   - *No port:* a concrete store in `CfdWorkbench.Persistence` is called by the Desktop, which passes the entry bytes
     into the Core Replace step. An interface is extracted only if DR-8 changes the location.
3. **Provenance and rights (flat, derive, don't store).** The profile's existing `provenance` string carries one origin —
   `gen:<catalog-id>`, `vend:<catalog-id>` or `dat:<sha256 of the original bytes>` — plus one `modified` flag. Saving an
   edited section keeps its origin and sets the flag. The rights class is a pure function of the origin (GEN → GEN,
   VEND → VEND, dat → user-supplied); an unknown or missing origin derives as **unknown**, never a guess. The chip text
   ("Catalog original · <source>" / "Modified from <source>", CAD-18) comes from the same two fields. LINK can never be
   saved because its coordinates are never present. `provenance` is outside geometry identity (`foildsl.md`:360). The
   exact spelling is a design-slice deliverable with a parser test.
4. **Using an entry in a foil inlines it** — the as-built import insertion (`AuthoringSession.cs:459-467`) — with the
   same origin and flag. The project stays self-contained, the native envelope is unchanged, and no rights lookup needs
   the installation library on another machine. **This deviates from the DR-8 default text**, which pins the entry
   as a content-addressed asset (A4.13, spec DR-8). Reason: the envelope has no asset set and the certified admission
   subset is built on inline profiles (`application.md` §5). The asset form is its own expand-migrate-contract change if
   the operator wants it.

## Alternatives considered

- **Library entries inside each project envelope.** Rejected for the default: CAD-19 requires an entry "in another
  foil"; kept as the DR-8 alternative.
- **An append-only facts file with rename/remove** (the first draft). Cut by the Simplifier: no spec clause needs it; a
  scan of immutable files gives the same current state with one file format fewer.
- **A database (SQLite).** Rejected: a folder of small immutable files needs no query engine (ADR-0002/0003).
- **Store the rights class, or a nested provenance chain.** Rejected: a stored class can disagree with its origin, and a
  nested `section:<sha>` chain needs the library to resolve on another machine.

## Consequences

- **Positive:** entries are plain FoilDSL files a user can read; rights follow the origin; no second store to reconcile.
- **Negative:** no rename or remove in the product yet. A stale claim blocks saves until it is cleared; it is reported,
  never deleted by age (as-built policy), and design-slice owns the recovery action the user is offered.
- **Follow-ups:** the GEN generator and its build-time hash (A4.10); Security reviews the bundled catalog's licence
  register before release; the spec owner amends DR-8's "pins it as a content-addressed profile" or the operator asks
  for the asset form.
