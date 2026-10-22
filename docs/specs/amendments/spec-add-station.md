---
id: spec-amendments-add-station
title: "Spec amendments for Add station and Promote (GEO-06, CAD-02, A4.6) — Ruling 252"
type: spec
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, station, promote, rule-a, blend, geo-06, cad-02, ruling-252]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: design-add-station, rel: depends-on}
  - {to: decision-loft-rule-a, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: >-
  Four amendments from the Add station design (track S1), accepted by Ruling 252. A promotion is exact and one press when
  its measured deviation is ≤ 1e-12; otherwise it is a separately labelled, previewed approximate promotion applied only
  with Apply at ≤ 10 µm (S1-Q1 d). Promote and Remove keep current thickness (S1-Q2 a). A station within the 10 µm join
  tolerance in span of another is coincident (S1-Q3 b). The operator may revisit any of them.
---

# Amendments: Add station and Promote (Ruling 252)

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** **accepted by Ruling 252** (Opus Owner seat, 2026-10-22, on request `req-01M5GPPSADJVK0MSXDPQ04X2YK`). These
amendments land through this note as the spec owner's record; the operator may revisit any of them. Design:
[Add station](../../design/add-station.md) §2.4–2.6, §3.2, §3.3 and §10.

## How to read this

One row is one amendment, in the form of [spec 1.7.6](spec-1.7.6.md). *Before* quotes the 1.7.6 text; *After* is the
accepted text.

## The finding behind A-S1-1 and A-S1-3

Rule A renormalises the blended unit thickness by its own maximum. A promoted station's profile always has a unit
thickness peak of 1, so the loft between it and its neighbours equals the old loft only when the two neighbours' unit
thickness shapes peak at a common chord fraction; no other choice of profile is exact (design §2.4). The section **at**
the promoted η is exact in every case.

**Measured size, a model of `Placement.cs` rule A, not the product** (Inferred for the product; T4 measures the real pairs
in the product). Script `r2_real_pair.py` in the track's scratch folder; inputs `docs/examples/foildsl/foil-basic.foil`
profile `section-a` and `src/CfdWorkbench.Core/CatalogData/naca-0012.dat`, root at η 0, tip at η 1, promotion at η 0.5,
t/c 0.12, 1999 η × 2001 x samples: largest deviation **5.1 × 10⁻⁷ of chord (0.08 µm at 150 mm chord)** at η 0.25. The
catalog pair `naca-0012.dat` / `naca-4412.dat` gives 3.4 × 10⁻¹⁰, already above 10⁻¹². Across thickness-shape families
the size reaches 1.5 × 10⁻⁴ of chord (23 µm at 150 mm), so some real pairs stay refused under the 10 µm gate.

## The rule (Ruling 252, S1-Q1 (d))

The regime is decided by the measured deviation over both adjacent intervals, never predicted from peak positions; a
promotion measured at or below 1e-12 relative is one press and exact; any larger one is offered only as a separately
labelled approximate promotion that opens a preview with the measured deviation, where it was found and the 10 µm limit
at the local chord, and applies only with Apply at or below 10 µm, with Apply disabled and the number shown above it; a
one-press call on such a pair is refused with STATION-INEXACT. The reported number is labelled sampled and names its
grid; it is never presented as a bound.

## Rows

| Id | Where | Before (1.7.6) | After (accepted) | Ruling |
|---|---|---|---|---|
| A-S1-1 | GEO-06 (`cfd-workbench-v1.md:1198`) | "**when** I promote it, **then** the reported deviation is ≤ 10⁻¹² relative (exact under rule A) and the station is selectable in every view." | "**when** I promote it, **then** the deviation over both adjacent intervals is measured and reported, labelled sampled with its grid; when the measured deviation is ≤ 10⁻¹² relative the promotion is exact and applies in one action; otherwise it is offered only as a labelled approximate promotion that previews the measured deviation, where it was found and the 10 µm limit at the local chord, and applies only with Apply when it is ≤ 10 µm, Apply disabled with the number shown above; the station is selectable in every view." | Ruling 252 (S1-Q1 d) |
| A-S1-2 | CAD-02 (`cfd-workbench-v1.md:1245`) | "…selectable in every view, with an explicit thickness command policy and referenced profile closure;" | "…selectable in every view, with an explicit thickness command policy (Promote and Remove keep current thickness: the t/c channel is unchanged byte for byte, and the receipt says so) and referenced profile closure;" | Ruling 252 (S1-Q2 a) |
| A-S1-3 | A4.6 (`cfd-workbench-v1.md:540`) | "an exact path (knot insertion, mirror, promotion under rule A, an unmodified catalog copy) must report ≤ 10⁻¹² relative;" | "an exact path (knot insertion, mirror, promotion under rule A whose measured deviation is ≤ 10⁻¹² relative, an unmodified catalog copy) must report ≤ 10⁻¹² relative; a promotion whose measured deviation is larger is an approximate path (an approximate promotion);" | Ruling 252 (S1-Q1 d) |
| A-S1-4 | GEO-06 (`cfd-workbench-v1.md:1198`) | "**Given** a coincident η, **then** insertion is rejected and the existing station selected." | "**Given** a coincident η — a station whose resolved span position is within the 10 µm join tolerance of an existing station's — **then** insertion is rejected and the existing station selected." | Ruling 252 (S1-Q3 b) |
