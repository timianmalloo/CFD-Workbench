---
id: examples-foildsl
title: FoilDSL language conformance examples
type: doc
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [foildsl, fixtures, conformance]
links:
  - {to: spec-foildsl, rel: documents}
  - {to: decision-foildsl-reconciliation, rel: documents}
review-by: 2027-03-22
summary: >-
  Complete foil and section examples, invalid documents and precision/comment variants with explicit expected outcomes.
  A reproducible probe records the supplied v3 checker behavior; normative 4.0 fixtures are acceptance vectors,
  not a claim of an implemented production evaluator.
review-suggested:
  - { by: spec-foildsl, on: 2026-09-22, reason: "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions." }
---

# Conformance fixtures

`cases.json` is the expected-outcome manifest. **Normative expected**, not a report of production conformance.
The review mockup may reject a syntactically valid feature with DSL-UNSUPPORTED when outside its advertised subset.

| File | Required outcome |
|---|---|
| `foil-basic.foil` | Accept: full span 0.9 m, constant chord 0.12 m, S=0.108 m², AR=7.5, mean chord=MAC=0.12 m. Twist does not change reference area. Two endpoint assignments share one manufactured symmetric profile. |
| `section-basic.foil` | Accept as standalone normalized 2D section; no assumed span, loads or NACA designation. |
| `foil-precision.foil` | Accept; leading CV at eta-coordinate 0.3 is 14.049 mm and survives parse/emit; identity differs from a 14 mm counterpart. |
| `foil-comment.foil` | Accept; same geometry identity as foil-basic, different exact source identity. |
| `foil-assertions.foil` | Accept with both explicit-tolerance assertions passing, or prototype DSL-UNSUPPORTED before any mutation. |
| `invalid-syntax.foil` | Missing final brace → DSL-SYNTAX; preserve accepted shape and draft. |
| `invalid-geometry.foil` | Negative root chord → DSL-GEOMETRY; no Apply. |
| `invalid-units.foil` | Area unit in half-span → DSL-UNIT; no reinterpretation. |
| `invalid-version.foil` | Version 9.0 → DSL-VERSION; read-only source. |
| `invalid-reference.foil` | Unknown profile reference → DSL-REFERENCE; never substitute default. |

Additional mandatory production vectors (define expected failures before implementation):

1. Duplicate singleton `half_span`: reject; out-of-order/duplicate stations: reject and locate.
2. Noninteger degree, wrong knot count, decreasing knots, degree-incompatible CV count: reject.
3. `half_span 0 mm`, nonfinite/overflow length, and wrong-dimension assertion: reject.
4. Source byte size 1 MiB + 1: DSL-LIMIT before parsing; names with HTML remain inert text.
5. A wrong-hash or missing profile asset: DSL-REFERENCE; no ambient path/network resolution.
6. Open tip with zero chord: reject; explicit point tip with positive interior chord: geometry validation required.
7. Two valid profiles with different knot/control counts: normalize/blend without equal-count restriction.
8. A crossing strictly between a coarse checker's sample positions: reject or Not assessed, never “valid”.
9. Source-only comment, lock, assertion and profile-name changes: preserve geometry identity; geometric edits stale runs.
10. Unit spellings representing the same exact dimensional decimal, and canonical SI round trips: equal identity.
11. Cancel after text and GUI draft: restore exact source and geometry; Undo/Redo restore source + definition + freshness.
12. A base-revision conflict, incomplete recovery draft and interrupted save: preserve last accepted source.

`reference-probe.cjs` executes supplied v3 functions without changing them. Run from repository root:

```
node docs/examples/foildsl/reference-probe.cjs "reference/FoilDSL v3 — Reference Grammar & Conformance Checker.html"
```

`reference-observations.json` is the observed output on 22 September 2026, separate from the normative v4 manifest.
It establishes default-reference parity, rounding loss, nominal missing-file sections, fractional degree acceptance
and last-writer duplicate behavior. It is not the v4 parser.
