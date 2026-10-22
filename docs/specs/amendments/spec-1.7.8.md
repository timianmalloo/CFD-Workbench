---
id: spec-amendments-1-7-8
title: "Spec 1.7.8 amendment note — dimensional L and D_i deltas when only the reference quantities differ (ANA-06, AF)"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, analysis, compare, ana-06, dimensional-delta]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-7, rel: relates-to}
  - {to: design-a3d-compare, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: >-
  Three proposed amendments to cfd-workbench-v1 lines 172, 259 and 1217, from Ruling 250 Q3 (c) and section 12 of the A3d
  Compare design: when two runs differ only in their reference quantities (same operating point and water), the
  dimensional loads L and D_i are compared with deltas normalised to the base value; coefficient deltas stay blocked; e
  is not compared; nothing new is stored. Proposed; the operator's take-up of Q3 (c) is confirmed by the leader (DR-DF-1).
---

# Spec 1.7.8 amendment note — dimensional deltas at differing reference quantities

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6 (with
the proposed [1.7.7](spec-1.7.7.md) note). **Status:** all three rows are **proposed**. Ruling 250 Q3 (a) is built:
every numeric δ is blocked when the reference quantities differ. Q3 (c) was recorded as the operator's next step; the
design is [a3d-compare.md §12](../../design/a3d-compare.md). DR-DF-1 asks the leader to confirm that the operator took
(c) up on 2026-10-22 before the AF build.

## How to read this

- One row is one amendment, in the form of [spec 1.7.7](spec-1.7.7.md). *Before* quotes the 1.7.6 text (line numbers
  of 1.7.6); *After* is the exact proposed text.
- Why a spec change is needed: ANA-06 says "incompatible reference quantities block a numeric delta". A δL shown for a
  pair whose S_ref differs is a numeric delta on such a pair, so the clause blocks it as written.

## Amendments (3)

| ID | Spec clause | Before (quoted) | After (exact proposed text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.8-1 | ANA-06 acceptance (:1217) | `incompatible reference quantities block a numeric delta` | incompatible reference quantities block every coefficient delta (CL, CDi, e); when the reference quantities are the only difference (the operating point and the water are equal), the dimensional loads L and D_i are compared with deltas normalised to the base value, e is not compared, and the pair stores no Discrepancy record | Ruling 250 Q3 (c); design §12.1-12.2 | proposed |
| AM-1.7.8-2 | Milestone acceptance summary (:172) | `a comparison with incompatible reference quantities is blocked until reconciled (ANA-06)` | a comparison with incompatible reference quantities shows no coefficient delta; at an equal operating point and water it compares the dimensional L and D_i (ANA-06) | design §12.1 | proposed |
| AM-1.7.8-3 | Glossary, Discrepancy record (:259), after AM-1.7.7-3 | (AM-1.7.7-3, proposed) `Dimensional loads (L, D_i) are not stored lines: …` | Dimensional loads (L, D_i) are not stored lines. At a compatible pair they differ by the same fraction as CL and CDi and are shown side by side without a δ. At a pair that differs only in its reference quantities, their δ is derived on read from the two runs, each with its own q and S_ref, and is never stored. | Ruling 250 Q5 (a), Ruling 251; DM7; design §12.2 | proposed |

## What does not change

- "Each compared pair stores a Discrepancy record" keeps the 1.7.7 reading: a blocked pair stores no record
  (AM-1.7.7-1). A reference-only pair is still blocked for coefficients, so it stores nothing.
- The Discrepancy record's stored shape, its rule version (`cfdw.compare` 1.0.0) and its retention (Ruling 251, Ruling
  258) are unchanged.
- The operating-point and water blocks (DR-A3D-5 reading) are unchanged: any difference there shows no delta at all.
- No copy row of the spec's copy table (line 2306) changes; the AF copy is design copy (AF-COPY-1..7).
