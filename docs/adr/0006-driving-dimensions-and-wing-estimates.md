---
id: adr-0006-driving-dimensions
title: "ADR-0006: driving dimensions are commands that refit the rails of record; Wing estimates are derived in Core"
type: adr
status: proposed
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [geometry, planform, driving-dimension, wing-estimates, mac, adr, dr-2, dr-9]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0001-master-curve-degree, rel: depends-on }
  - { to: adr-0005-point-types, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Typed Span patches half_span only (exact). Typed Root or Tip chord refits the moved rail on its own knots and
  abscissae, ordinates only, with the typed end pinned exactly and every lock a hard row; the held line (DR-2) is one
  parameter and the quarter-chord option re-sets the frame. A spike shows the operator's linear chord blend cannot meet
  10 µm on a rail with the default root-mirror lock (0.08–4 mm), so DR-9 asks which rule wins; until then A4.6 is
  strict and the residual against the operator's rule is always reported. Wing estimates are the FoilDSL metric
  definitions, computed by one pure Core function and never stored.
review-suggested: []
---

# ADR-0006: driving dimensions refit the rails of record; Wing estimates are derived in Core

- **Status:** Proposed — architect council 2026-09-26, repair cycle 1
- **Date:** 2026-09-26
- **Deciders:** the operator (behaviour, decided 2026-09-26), Computational Geometry lens (mapping), Data & Persistence
- **Context:** spec 1.6 A4.15 (driving dimensions, Wing estimates), CAD-16, CAD-17, A3.3, A4.5–A4.7, A4.14; DR-2;
  ADR-0002 (no second authority); FoilDSL §5.2 and the assertion metrics (`foildsl.md`:276-277).

## Context

Span, Root chord and Tip chord are typed, direct-commit commands (one undo step, no preview; operator decision). The
estimates (span, root chord, tip chord, S/b, MAC, max t/c, AR, area) are derived on read and follow a drag live. The spec
leaves open how a typed value maps to the curves of record and notes that a linear factor times a B-spline is not, in
general, a B-spline of the same degree (A4.15). As built, Core computes no estimate: the parser knows the assertion
metric names (`FoilSource.cs:769`) but `Geometry.Assess` certifies only documents without assertions (`Geometry.cs:288-289`).
FoilDSL already defines the metrics: S = 2∫chord(y)dy, aspect = b²/S, mean_chord = S/b, mac = (2/S)∫chord(y)²dy
(`foildsl.md`:276-277). `leading(0) = 0` by definition (`foildsl.md`:181; checked at `Geometry.cs:298`), and root-mirror
locks default ON for every channel when a document has no `locks` block (`foildsl.md`:219).

## Decision

1. **Wing estimates are the FoilDSL metric definitions.** One pure function `WingEstimates.From(definition)` in Core,
   run on the accepted definition or the open draft's bytes, computes exactly the `foildsl.md`:276-277 quantities plus
   root chord c(0), tip chord at the outermost assignment and max t/c (the maximum of the thickness channel on [0,1]).
   It is the display path; a later certified assertion path must reuse these definitions, and a conformance test binds
   the two. Chord basis: c(η) = trailing(η) − leading(η) in the unrotated planform (A4.15 table).
   - *Accuracy:* S (and so mean chord and AR) is integrated **exactly per rail in its own parameter**, ∫x dη =
     ∫x(u)·η′(u) du, a polynomial of degree 2p − 1 on each span, so p-point Gauss–Legendre is exact up to round-off.
     MAC's cross term ∫leading·trailing dη mixes two parameterisations; it uses adaptive Gauss–Legendre in η over the
     images of both rails' knots, iterated until successive values agree to relative 10⁻⁹ (100× inside the scalar
     oracle's 10⁻⁶), and the convergence is part of the result. A fixture compares the binary64 values with the rational
     certificate's enclosures and with the CAD-17 fixtures (S = 0.125 m², MAC = 140.0 mm, AR = 8.00, council probe).
   - *Evaluator:* estimates use the shared binary64 `SplineBasis` (`ConstrainedFit.cs:576`); the one duplicate private
     evaluator (`FoilSource.cs:574`) is folded into it under a byte-identical golden master
     (decision note `note-20260926-binary64-evaluator`).
   - *Freshness:* the value carries its basis ("accepted" or "preview") and the source generation it came from; a stale
     result is dropped. It is never written: not to FoilDSL, not to the native envelope, not to `derived_check` (A3.3,
     CAD-17).
   - *Cost:* a 7-vertex rail pair has at most eight integration intervals — about 10³ basis evaluations per frame,
     **Inferred**. The function emits `estimates.compute` (duration, interval count, iterations, outcome) on the normal
     path, and CAD-17's "changes during the drag" test reads it.
2. **Span** patches the `half_span` token to b/2 in the source unit (exact decimal halving). Station and `value`-lock
   positions spelled as absolute lengths are rewritten in the same patch so every η is unchanged (CAD-16, scalar
   oracle); channel control vertices are untouched, so chords at every η and elevation z(η) are unchanged (the dihedral
   angle changes; A4.14 Keep relative).
3. **Root chord / Tip chord** set a target rail function and refit each moved rail on **its own knots and control
   abscissae, ordinates only** (the `FitToBasis` pattern, `DatImport.cs:267-291`, via `ConstrainedFit.Solve`). Point ids,
   point types (ADR-0005) and tangent rows survive because the basis is unchanged. Hard rows: the typed end (exact:
   TE(0) = typed root chord since LE(0) = 0), the other end unchanged, `root_mirror`, `freeze`, `value`, `bounds` and
   tangent-kind rows. A lock the target must break refuses the commit, naming the lock.
   - **Held line (DR-2) is one enum parameter.** *Leading edge held* (default): TE′ = LE + s·c; LE unchanged
     byte-for-byte. *Quarter chord held*: LE′ = LE + (1 − s)c/4 and TE′ = LE + c/4 + 3s·c/4. For a root edit this gives
     LE′(0) = (1 − f)c(0)/4 ≠ 0, which the frame forbids (`foildsl.md`:181), so both rails are then translated by
     −LE′(0): the quarter-chord line is held up to that rigid x-translation of the planform, and the status line says so.
     Tip edits have s(0) = 1 and need no translation.
   - **Blend rule and acceptance are DR-9** (below). Every commit reports, with its rule id, the maximum deviation of
     the result from the operator's rule c′ = s·c with linear s, measured on the distribution-curve oracle
     (201 η + knots, A4.5), in Messages and the status line.
4. **Refusals keep geometry and undo depth unchanged:** not a number, ≤ 0, crossing or zero chord (the rail certificate,
   `Geometry.cs:319-321`), a lock, a residual above acceptance (DR-9 default), or tip `point` (not editable; tip-closing
   foils are Unsupported as built, `Geometry.cs:288`). A conservative **Not assessed** is a different message from
   "edges cross"; design-slice owns that string and the residual string (the spec has neither yet).
5. **One commit = one core command** `ApplyDimension(operationId, command)` that begins, updates, validates and applies
   one draft under the session lock and is memoized by `operationId` (LOA P8). It appends exactly one accepted row or
   none, and emits the existing apply event with `edit_kind = dimension`.

## Evidence — spike `docs/proof/cad-first-spikes/typed-chord-map/` (run 2026-09-26, numpy, degree 3, 7 CVs, LE held)

Scripts and `results.txt` are committed there. Residual = maximum |fitted rail − target| on the distribution-curve oracle
set, both ends pinned (**Verified**, observed output):

| Fixture | Rule | Root-mirror lock | Residual vs the rule | Deviation from the operator's linear rule |
|---|---|---|---|---|
| Example rails, constant chord 120 mm, root ×1.5 | linear | off | 0.00 µm | 0.00 µm |
| Example rails, root ×1.5 / tip ×0.8 | linear | **on** (default) | 2 785 / 1 114 µm | same |
| CAD-17 linear taper 200→50 mm, root ×1.2 / tip ×0.8 | linear | off | 0.00 / 0.00 µm | same |
| Mirrored swept taper 200→80 mm, root ×1.2 | linear | on / off | 1 581 / 16 µm | same |
| Same, root ×1.01 | linear | on | 79 µm | same |
| Same, root ×1.2, exactly refined to 8 / 9 / 10 CVs | linear | on | 835 / 424 / 419 µm | same |
| Example root ×1.5 · swept root ×1.2 · swept root ×1.01 | root-flat s = f + (1 − f)η² | on | 56 · 32 · 1.6 µm | 14 951 · 8 710 · 436 µm |

Reading: the linear blend puts a chord slope at the root that the root-mirror lock forbids (a mirrored planform would
kink at the centre line), and exact refinement up to the 10-vertex ceiling does not cure it. The root-flat rule respects
the lock but is a different shape — 0.4–15 mm from the operator's rule — so it cannot be substituted silently.

## Decision requests raised

| ID | Question | Default in this ADR | Owner | If overturned |
|---|---|---|---|---|
| DR-9 | The operator's linear chord blend cannot meet 10 µm on a rail with the default root-mirror lock. Which rule wins, and is a residual above 10 µm a refusal? | **Strict A4.6 with the operator's rule** (`chord-blend-linear/1`): the residual is reported; above 10 µm the commit is refused with the number. Consequence: on documents without a `locks` block most typed chords are refused | Operator, then Computational Geometry | (a) *Root-flat rule* (`chord-blend-rootflat/1`) on locked rails, reporting both numbers; (b) *the command releases the root-mirror lock* in the same undo step (a visible lock change, root kink accepted); (c) *report, don't refuse* above 10 µm |

## Alternatives considered

- **Raise the rail degree to hold s·c exactly.** Rejected: ADR-0001 fixes degree 3 for channels; FoilDSL 4.0 fixes p = 3.
- **Scale the moved rail's ordinates vertex by vertex.** Rejected: it is not c′ = s·c (uncontrolled, unreported error)
  and it breaks the root-mirror pair P₀, P₁.
- **Substitute the root-flat rule silently.** Rejected by the council: 0.4–15 mm from the decided behaviour.
- **Store the typed values.** Rejected: a second authority (A4.15 "never stored").
- **Compute estimates in the Desktop.** Rejected: CLI parity (CLI-01) and one definition of each quantity.

## Consequences

- **Positive:** one mapping function serves both held lines; DR-2 is a one-parameter change. Estimates have one home and
  one definition shared with the language.
- **Negative:** until DR-9 is answered, typed chords on default documents are mostly refused; the CAD-17 fixtures (locks
  off) are exact. Absolute station spellings change spelling on a span commit (η unchanged).
- **Follow-ups:** DR-9 answer; evaluator fold under a golden master; design-slice strings for Not assessed and the
  residual; the conformance test binding estimates to the assertion metrics.
