---
id: spec-amendments-add-station
title: "Proposed spec amendments for Add station and Promote (GEO-06, CAD-02, A4.6) — for the Owner's ruling"
type: spec
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, station, promote, rule-a, blend, geo-06, cad-02]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: design-add-station, rel: depends-on}
  - {to: decision-loft-rule-a, rel: relates-to}
review-by: 2027-01-31
summary: >-
  Three proposed amendments from the Add station design (track S1), written for the recommended options. Promotion is an
  exact path when its measured deviation is ≤ 1e-12; otherwise it is a separately labelled, previewed approximate
  promotion admitted at 10 µm (S1-Q1, option d). Promote and Remove keep current thickness (S1-Q2). Pending the Owner's
  ruling; nothing applies before it.
---

# Proposed amendments: Add station and Promote

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** **proposed, pending Owner ruling (S1-Q1, S1-Q2).** The leader files the request; each row lands only on its
ruling. Design: [Add station](../../design/add-station.md) §2.4–2.6 and §10.

## How to read this

One row is one amendment, in the form of [spec 1.7.6](spec-1.7.6.md). *Before* quotes the 1.7.6 text; *After* is the
proposed text for the recommended option.

## The finding behind A-S1-1 and A-S1-3

Rule A renormalises the blended unit thickness by its own maximum. A promoted station's profile always has a unit
thickness peak of 1, so the loft between it and its neighbours equals the old loft only when the two neighbours' unit
thickness shapes peak at a common chord fraction; no other choice of profile is exact (design §2.4). The section **at**
the promoted η is exact in every case.

**Measured size, a model of `Placement.cs` rule A, not the product** (script `r2_real_pair.py` in the track's scratch
folder; inputs `docs/examples/foildsl/foil-basic.foil` profile `section-a` and
`src/CfdWorkbench.Core/CatalogData/naca-0012.dat`, root at η 0, tip at η 1, promotion at η 0.5, t/c 0.12, 1999 η × 2001 x
samples): largest deviation **5.1 × 10⁻⁷ of chord (0.08 µm at 150 mm chord)** at η 0.25. The catalog pair
`naca-0012.dat` / `naca-4412.dat` gives 3.4 × 10⁻¹⁰, already above 10⁻¹². Across thickness-shape families the size
reaches 1.5 × 10⁻⁴ of chord (23 µm at 150 mm, the Adversary's model for peaks at 30 % and 50 %), so some real pairs are
refused under any 10 µm gate.

## Rows

| Id | Where | Before (1.7.6) | After (proposed) | Ruling |
|---|---|---|---|---|
| A-S1-1 | GEO-06 (`cfd-workbench-v1.md:1198`) | "**when** I promote it, **then** the reported deviation is ≤ 10⁻¹² relative (exact under rule A) and the station is selectable in every view." | "**when** I promote it, **then** the deviation over both adjacent intervals is measured and reported; when the measured deviation is ≤ 10⁻¹² relative the promotion is exact and applies in one action; otherwise it is offered only as a labelled approximate promotion that previews the measured deviation and applies with Apply when it is ≤ the 10 µm model/join tolerance at the local chord, Apply disabled above; the station is selectable in every view." | pending S1-Q1 (d) |
| A-S1-2 | CAD-02 (`cfd-workbench-v1.md:1245`) | "…selectable in every view, with an explicit thickness command policy and referenced profile closure;" | "…selectable in every view, with an explicit thickness command policy (Promote and Remove keep current thickness: the t/c channel is unchanged byte for byte) and referenced profile closure;" | pending S1-Q2 (a) |
| A-S1-3 | A4.6 (`cfd-workbench-v1.md:540`) | "an exact path (knot insertion, mirror, promotion under rule A, an unmodified catalog copy) must report ≤ 10⁻¹² relative;" | "an exact path (knot insertion, mirror, promotion under rule A whose measured deviation is ≤ 10⁻¹² relative, an unmodified catalog copy) must report ≤ 10⁻¹² relative; a promotion whose measured deviation is larger is an approximate path (an approximate promotion);" | pending S1-Q1 (d) |

If the Owner rules S1-Q1 (a) instead, A-S1-1 and A-S1-3 are withdrawn: the spec text stands and an R2 promotion is
refused with its measured number.
