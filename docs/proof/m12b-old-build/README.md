---
id: proof-m12b-old-build
title: M1.2b old-build characterization of FoilDSL 4.1
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation
tags: [m12b, foildsl, characterization, b0]
links:
  - {to: design-m12b-points, rel: documents}
  - {to: spec-foildsl, rel: depends-on}
  - {to: adr-0005-point-types, rel: depends-on}
review-by: 2026-12-30
summary: >-
  Codes the committed 4.1 fixtures and a drag, nudge and span project draw from the
  parser at 10f0628, recorded before B0 changes that parser.
---

# Old-build characterization

The parser files `src/CfdWorkbench.Core/FoilSource.cs` and `Geometry.cs` at HEAD
`6e050a10454875c2589fd22c9088c502ee391543` are identical to `10f0628`. The
program `/tmp/b0-char` called `FoilSource.Parse` on each fixture and reopened a
project saved by that same build. This receipt is committed before the parser change.

| Fixture | Code | Phase |
|---|---|---|
| `foil-40-eleven-points.foil` | DSL-CURVE | Structural |
| `foil-40-tangents.foil` | DSL-SYNTAX | Syntactic |
| `foil-41-angle-on-channel.foil` | DSL-SYNTAX | Syntactic |
| `foil-41-multiplicity-two.foil` | DSL-VERSION | Version |
| `foil-41-row-on-control.foil` | DSL-SYNTAX | Syntactic |
| `foil-41-seventeen-points.foil` | DSL-VERSION | Version |
| `foil-41-sixteen-three-anchors.foil` | DSL-SYNTAX | Syntactic |
| `foil-41-tangents.foil` | DSL-SYNTAX | Syntactic |
| `foil-42-unknown-block.foil` | DSL-SYNTAX | Syntactic |

`span-drag-nudge-only.cfdw.json`, beside this note, is a project saved after one leading drag
(`cv-2` to 4 mm), one trailing nudge (`cv-3` to 121 mm) and one span edit
(`1000`). Reopen parses. The image is 15019 bytes and contains neither a
`"curve"` key nor a `"rule"` key. `foil-basic.foil` parses.

A 4.1 document that the grammar can read fails `DSL-VERSION`. A 4.1 document
with a `tangents` row or an unknown block fails `DSL-SYNTAX`, because the
version gate runs only after the document has been read. Eleven channel points
under 4.0 already fail `DSL-CURVE`.
