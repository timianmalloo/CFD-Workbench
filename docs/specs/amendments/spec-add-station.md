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
  Three proposed amendments from the Add station design (track S1). Promotion of an inspection slice is exact only when
  the two neighbouring sections share spacing and peak thickness location; otherwise it is an approximate path with a
  measured deviation (DR-S1-1). The thickness policy for Promote and Remove is Keep current thickness (DR-S1-5). Not
  approved; the Owner rules before any build.
---

# Proposed amendments: Add station and Promote

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** **proposed, not approved.** The leader raises DR-S1-1 and DR-S1-5 of
[the Add station design](../../design/add-station.md) §10; each row lands only on its ruling.

## How to read this

One row is one amendment, in the form of [spec 1.7.6](spec-1.7.6.md). *Before* quotes the 1.7.6 text; *After* is the
proposed text. The reason and the evidence are in the design's §2.4 (the proof) and §2.5 (a scratch model).

## The finding behind A-S1-1 and A-S1-3

Rule A renormalises the blended unit thickness by its own maximum. A promoted station's profile always has a unit
thickness peak of 1, so the loft between it and its neighbours equals the old loft only when the two neighbours' unit
thickness shapes peak at the same chord fraction. When they peak at different chord fractions, both adjacent intervals
move. A scratch model sized the change at 0.8 × 10⁻⁶ to 3.1 × 10⁻⁵ of chord for peaks at 30 % against 35 % to 50 %. The
section **at** the promoted η is exact in every case.

## Rows

| Id | Where | Before (1.7.6) | After (proposed) | Ruling |
|---|---|---|---|---|
| A-S1-1 | GEO-06 (`cfd-workbench-v1.md:1198`) | "**when** I promote it, **then** the reported deviation is ≤ 10⁻¹² relative (exact under rule A) and the station is selectable in every view." | "**when** I promote it, **then** the deviation over both adjacent intervals is measured and reported; it is ≤ 10⁻¹² relative (exact under rule A) when the neighbouring sections are the same, or share spacing and their unit thickness peaks at the same chord fraction; otherwise it is accepted at the 10 µm model/join tolerance at the local chord and Promote is refused above it; the station is selectable in every view." | DR-S1-1 (b) |
| A-S1-2 | CAD-02 (`cfd-workbench-v1.md:1245`) | "…selectable in every view, with an explicit thickness command policy and referenced profile closure;" | "…selectable in every view, with an explicit thickness command policy (Promote and Remove keep current thickness: the t/c channel is unchanged byte for byte) and referenced profile closure;" | DR-S1-5 (a) |
| A-S1-3 | A4.6 (`cfd-workbench-v1.md:540`) | "an exact path (knot insertion, mirror, promotion under rule A, an unmodified catalog copy) must report ≤ 10⁻¹² relative;" | "an exact path (knot insertion, mirror, promotion under rule A between sections of the same unit thickness peak location, an unmodified catalog copy) must report ≤ 10⁻¹² relative; promotion between sections whose peaks differ is an approximate path;" | DR-S1-1 (b) |

If the Owner rules DR-S1-1 (a) instead, A-S1-1 and A-S1-3 are withdrawn: the spec text stands, and Promote is refused
in that case with the measured number (design S1-COPY-11).
