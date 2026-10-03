---
id: adr-0005-point-types
title: "ADR-0005: point types are knot multiplicity in the existing B-spline record; tangent kinds are a FoilDSL 4.1 curve annotation"
type: adr
status: proposed
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [geometry, b-spline, point-type, anchor, control-point, foildsl, adr, dr-5, dr-10, dr-11]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: refines }
  - { to: adr-0001-master-curve-degree, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: design-section-editor, rel: relates-to }
  - { to: review-ui-workbench-v10, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Settles DR-5. A Point is a control vertex of the clamped non-rational B-spline of record. An interior vertex is an
  Anchor point exactly when one interior knot of multiplicity p sits at it, so point type is derived from the knot
  vector and never stored. Tangent kinds are editing intent in an optional FoilDSL 4.1 `tangents` block outside
  geometry identity. Every type change is measured and reported on the A4.5 oracle. On a section the two surfaces share
  one chord basis, so point types are paired across the surfaces (DR-11, default); the other surface's shape is exact
  on Anchor creation and refitted within 10 µm, reported, on Anchor removal.
review-suggested:
  - { by: design-m12c-section-editor, on: 2026-10-03, reason: "M1.2c designs section point types per surface (Ruling 53 DR-11): Control->Anchor and Anchor->Control act on one surface; §6's paired default and its other-surface refit survive only as the OD-4 fallback. Profile tangent rows get a unit-free rule (horizontal/vertical exact, smooth/symmetric/angle within 1e-9 chord) and IsAnchor becomes degree-general." }
  - { by: adr-0001-master-curve-degree, on: 2026-09-30, reason: "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)." }
  - { by: design-m12b2-3d-elevations, on: 2026-09-30, reason: "M1.2b2 applies tangent rows to the dihedral, twist and thickness channels with a unit-free rule (ordinate deviation from the handle line within tau_c: 1 um, 1e-6 deg, 1e-8) instead of the 0.1 deg direction tolerance, which is meaningless in a metres x degrees plane (docs/design/m12b2-3d-elevations.md 3.6)." }
---

# ADR-0005: point types are knot multiplicity in the existing B-spline record

- **Status:** Proposed — architect council 2026-09-26, repair cycle 1 (record in `architecture-application` §10.8)
- **Date:** 2026-09-26
- **Deciders:** the operator (behaviour, A4.15), Computational Geometry lens (record), Data & Persistence (grammar change)
- **Context:** spec 1.6 A4.15 and CAD-15 (point types, decided by the operator); DR-5; ADR-0001 (degree 3 master curves,
  degree 5 sections); ADR-0002 (FoilDSL is the only authority); FoilDSL 4.0 §4–§5, §8.

## Context

The operator decided that the Properties pane sets each point's type: **Anchor point** (on the curve, with handles and a
tangent kind) or **Control point** (off the curve, pulls it). Named points keep a fixed type (spec A4.15). The record is a
clamped, non-rational B-spline per curve: degree 3 for the five channels, degree 5 for section sides (ADR-0001:38;
`foildsl.md`:189). The 4.0 grammar writes only degree, knots, points and ids (`foildsl.md`:147-148). The v10 mockup raised
the segment degree for each control point between two anchors (`ui-workbench-v10.md`:58). That is not a single B-spline
of one stored degree.

Code as built constrains the choice:

- A section is certified only when its upper and lower sides share degree, knots **and** control-vertex abscissae
  (`src/CfdWorkbench.Core/Geometry.cs:302-304`, `:329`), and distinct neighbouring profiles only on a shared abscissa basis
  (`Geometry.cs:352-353`). The editor moves an x edit onto the same-index vertex of the other side
  (`AuthoringSession.cs:494-502`; `docs/design/section-editor.md`:46). Certifying independent bases was deferred after two
  measured cycles (`section-editor.md`:153).
- Only `root_mirror` locks are certified; any other lock kind makes a document Unsupported (`Geometry.cs:292`).
- Exact Boehm knot insertion exists and refuses multiplicity ≥ p (`FoilSource.cs:440-470`, guard `:449`); Tiller knot
  removal exists (`FoilSource.cs:478-500`); a section side is capped below 32 vertices on insertion (`FoilSource.cs:317`).
- The parser accepts only `foildsl "4.0"` (`FoilSource.cs:873`), and reopening a project parses every stored source
  revision (`AuthoringSession.cs:923`).

## Decision

1. **The record does not change.** A Point is a control vertex of the curve of record, with its stable id.
2. **Point type is derived, never stored.** With Piegl–Tiller indexing, an interior vertex *i* is an **Anchor point**
   iff the knot vector holds one interior knot of multiplicity exactly *p* at *t₍ᵢ₊₁₎ = … = t₍ᵢ₊ₚ₎*. The curve then
   interpolates vertex *i*, and vertices *i − 1* and *i + 1* are its handles. Every other interior vertex is a **Control
   point**. The multiplicity-p knot splits the curve into two independent clamped pieces, so the curve on each side
   depends only on that side's vertices: CAD-15's locality is exact (council probe: 2.2 × 10⁻¹⁶ outside the support).
3. **Named points are derived from position and the existing record:** root end and tip end are the first and last
   vertices of a channel (the root-mirror tangent is the existing `root_mirror` lock); the nose is the shared first vertex
   `(0,0)` with the vertical tangent given by the repeated zero abscissa (`foildsl.md`:196-197), its upper and lower handle
   lengths are the second vertices' ordinates; the upper and lower trailing-edge terminal points are the last vertices
   (x fixed at 1; closure `closed` fixes y). No new field is needed.
4. **Tangent kind is editing intent, persisted in FoilDSL 4.1** as an optional curve annotation:
   ```ebnf
   curve   = "cv", "{", "degree", integer, "knots", numbers, "points", points,
             ["ids", strings], ["tangents", "{", {tangent}, "}"], "}" ;
   tangent = string, ("smooth" | "symmetric" | "horizontal" | "vertical" | "angle", number) ;
   ```
   The string is the id of an interior Anchor point of that curve (`ids` required). **Corner has one spelling: no row**
   (there is no `corner` keyword). A row on a Control point or a named point is `DSL-LOCK`. `tangents` joins ids, locks
   and provenance in the strip list of canonical semantic input (`foildsl.md`:360), so the definition hash, the
   `foildsl-geometry-4.0` canonical format and every run key are unchanged by the grammar. The edit solver enforces a
   kind as a constraint row (A4.2); `Geometry.Assess` checks it — horizontal and vertical by exact equality (the
   `root_mirror` pattern, `Geometry.cs:296`), smooth, symmetric and angle within the A4.5 angle tolerance (0.1°) and a
   handle-length ratio of relative 10⁻⁶. Symmetric means equal handle lengths (the spec's definition); it is C¹ only
   where the two adjacent knot spans are equal, and the comb shows that.
5. **Operations** — each is one undo step, and **each one's shape change is measured on the A4.5 distribution-curve or
   profile oracle and reported** with the step (the A4.2 Delete CV contract):
   - *Control → Anchor* at vertex P between neighbouring anchors: find the unique u\* in that segment with
     x(u\*) = P.x (x is monotone on every span, `Geometry.cs:315`); when u\* is within 10⁻¹² relative of an existing
     knot, snap to it. Insert u\* to multiplicity p by Boehm (exact). Then move the new interpolated vertex **and both
     of its handles** by the same Δy = P.y − C(u\*).y, so the handles stay collinear and the `smooth` row written is
     true. The new anchor takes P's id. The curve changes only between the neighbouring anchors; up to p vertices are
     added.
   - *Anchor → Control*: delete the anchor's two handles and lower that knot's multiplicity by two (p = 3 → a simple
     knot; p = 5 → multiplicity 3). Every other vertex keeps its position and id; the change is confined between the
     neighbouring anchors. **It is not the inverse of Control → Anchor** (council probe: a round trip leaves 8 vertices
     from 7 and a 3.9 × 10⁻² chord deviation); Undo is the inverse.
   - *Tangent kind change*: rewrite the row; if the kind is not already satisfied the solver moves the handle(s) and the
     change is a geometry revision; otherwise it is a source-only revision.
   - *Continuity* after any change is bounded below by knot multiplicity (C^(p−m)); it is measured by the comb and the
     A4.3 break markers, never asserted.
   - *Other constructions keep rows valid:* Insert, Delete, Fair, Rebuild and a fresh-fit Replace remove any row whose
     vertex stops being an anchor in the same patch and report it; a fit on a basis that has anchors takes their rows as
     KKT constraint rows.
6. **Sections keep one shared chord basis; point types are paired across the surfaces (DR-11 default).** Because the two
   surfaces share knots and abscissae (`Geometry.cs:302-304`), a type change at a chord position is a change for both:
   - *Control → Anchor* inserts the same knots in the other surface. That is exact — Boehm's alphas depend only on the
     knots — so its shape is unchanged within 10⁻¹² relative; its vertex at u\* becomes an Anchor with `smooth`.
   - *Anchor → Control* changes the shared x-mapping. The other surface is refitted **only on the affected segment**,
     with every vertex outside it and the neighbouring anchors as hard rows; its change is measured on the A4.5 profile
     oracle and reported; above the model/join tolerance (10 µm at the station's local chord, A4.6) the command is
     refused, the number is shown and nothing changes. CAD-15 lets the representation set this tolerance.
   - A change that breaks the basis shared with a distinct neighbouring profile is refused, naming the neighbour, as for
     x edits today (`section-editor.md`:46), until the deferred B6 certificate lands.
7. **Capacity.** Channels stay at 6–10 vertices and sections at 6–32 (`foildsl.md`:189). Control → Anchor adds two or
   three vertices on a channel, so a seven-vertex default rail holds one interior anchor. A change that would exceed the
   ceiling is refused, naming it (DR-10).

## FoilDSL change — expand · migrate · contract

- **Expand (this ADR):** the parser accepts `"4.0"` and `"4.1"`; `tangents` is legal only under `"4.1"`. Evaluator id
  `cfdw-cv/2` and canonical format `foildsl-geometry-4.0` are unchanged.
- **Migrate:** none is forced. The GUI patch that first writes a row also patches the header to `"4.1"` in the same
  transaction (visible in the source diff). A document without rows stays `"4.0"` byte-for-byte.
- **Contract:** nothing is removed.
- **Rollback consequence (stated, not hidden):** accepted history is append-only and every stored source is reparsed on
  open (`AuthoringSession.cs:923`), so once a project has one 4.1 revision a 4.0-only build can never open it. The file
  is not changed — the as-built parser refuses with `DSL-VERSION` (`FoilSource.cs:873`) — so nothing is destroyed.
  Required fixture: the current build opens a 4.1 project fixture, refuses it naming the version, and leaves the bytes
  identical. A later **Export as 4.0** (strip rows; geometry identical by construction) is the downgrade path if asked for.
- The companion `foildsl.md` gains §4/§5/§8 text and conformance cases in the same change as the parser, never before.

## Alternatives considered

- **Segment degree rises with the control-point count (v10).** Rejected: a curve would have no single degree, against
  ADR-0001 and `foildsl.md`:189; a segment with more than p − 1 interior points cannot be reduced to degree p exactly.
- **Anchors as C² quintic Hermite joins (multiplicity 3 at degree 5; the pre-1.6 brief's default).** Rejected as the
  base model: the anchor is then not a vertex, so its position becomes a second value to keep consistent, and Corner
  needs a different multiplicity anyway. Kept as a possible later tangent kind under its own ADR.
- **Remove p − 1 vertices on Anchor → Control** (full continuity back). Rejected as the default: it moves more of the
  curve than the user touched; its larger measured change would still have to be reported.
- **Fit-point curves (anchors as construction provenance).** Superseded by the operator's decision (spec Appendix G).
- **Store the point type as a field.** Rejected: it duplicates what the knot vector says (derive, don't store).
- **Tangent kind in the foil-level `locks` block.** Rejected: profiles and standalone sections have no locks block
  (`foildsl.md`:141-145).
- **Independent knot vectors per section surface now (unpaired types).** Rejected for this slice: it needs the
  independent-basis separation and blend certificates deferred after two measured cycles (`section-editor.md`:153).

## Consequences

- **Positive:** no record migration; every existing document already reads as named anchors plus control points
  (as-built curves have simple interior knots). Locality is exact. Identity and run freshness are untouched by rows.
- **Negative / trade-offs:** an interior anchor is G1 (Smooth) or C¹ at best (Symmetric on equal spans); curvature jumps
  there and the comb shows it. Section point types are paired (DR-11). A seven-vertex rail holds one interior anchor.
  Multi-profile foils refuse section type changes that break a neighbour's basis until B6. A project with a 4.1
  revision cannot be opened by a 4.0-only build.
- **Spec findings for the spec owner:** CAD-15's "measured gap > the identity tolerance" cannot hold when the point lies on
  the line through its neighbours (council probe: 10⁻¹⁷ on a flat rail; `foildsl.md`:194-195 already concedes interior
  CVs can lie on the curve) — the fixture must use a point off that line. CAD-15's "no other curve changes" becomes, on
  a section, "the other surface's shape changes by at most 10 µm, reported" under DR-11.
- **Follow-ups:** `Geometry.Assess` must admit and check rows (today it refuses non-`root_mirror` locks); A4.3 comb
  fixtures for anchors; `foildsl.md` text and conformance cases; CAD-15's save/reopen clause is un-gated by this ADR.

## Decision requests raised

| ID | Question | Default | Owner | If overturned |
|---|---|---|---|---|
| DR-10 | Raise the channel vertex ceiling above 10 so a rail can hold more than one interior anchor? | Keep 6–10 (ADR-0001); refuse with the ceiling named | Operator, then Computational Geometry | ADR-0001 amended to 6–16; `foildsl.md`:189 range relaxed (expand-only). **Ruled:** "raise the channel vertex ceiling to 16. ADR-0001 is amended and the FoilDSL range relaxed expand-only, in the M1.2b design-slice." (Ruling 53) |
| DR-11 | On a section, is a point type per surface or per chord position? | Per chord position (paired), because the surfaces share one certified basis; removal refits the other surface ≤ 10 µm, reported | Operator, then Computational Geometry | Per surface needs independent side bases and their separation/blend certificates (B6-class work) before it can ship. **Ruled:** "Independent section point types. This requires the B6 restart in the M1.2c wave, and D4 waits on M1.2c." (Ruling 53) |

## Evidence

- Interpolation at a multiplicity-p knot, C^(p−m) bounds, Boehm insertion, Tiller removal, curve splitting: Piegl &
  Tiller, *The NURBS Book* (2nd ed.), §2.3, §5.2–5.4 — **Verified** by the council's probe (2026-09-26: interpolation
  rule, exact locality 2.2 × 10⁻¹⁶, valid counts, exact paired insertion) and against the as-built insertion/removal
  code cited above. The probe script lived in the session scratchpad and is not committed; its numbers are quoted here.
- Handle rotation of 63.5° when only the anchor is moved to P.y (council probe) — the reason decision 5 moves the handles.
- As-built shared-basis rule, version check and reparse-on-open: code cited above — **Verified** (read).
