---
id: spec-amendments-1-7-7
title: "Spec 1.7.7 amendment note — the Discrepancy record grain, ρ derived, dimensional loads not stored (ANA-06, A3d)"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, analysis, compare, discrepancy-record, ana-06]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-6, rel: relates-to}
  - {to: design-a3d-compare, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: >-
  Three proposed amendments to cfd-workbench-v1 lines 259 and 329, from Ruling 250 Q2 and Q5 and the A3d Compare design:
  one Discrepancy record per unordered pair of evaluations in canonical orientation with one line per quantity, rho
  derived on read and never stored, and dimensional loads kept out of the stored lines. Proposed; the Data & Persistence
  lens confirms the text before the A3d build.
---

# Spec 1.7.7 amendment note — the Discrepancy record

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** all three rows are **proposed**. Ruling 250 (Q2 (c), Q5 (a)) decided the substance; the Data & Persistence
Architect holds the veto and confirms this text before the A3d build dispatches. The design is
[docs/design/a3d-compare.md](../../design/a3d-compare.md) (§1.0, §1.3, §1.4).

## How to read this

- **One row is one amendment**, in the form of [spec 1.7.6](spec-1.7.6.md). *Before* quotes the 1.7.6 text; *After* is
  the exact proposed text. Line numbers are those of the 1.7.6 spec.
- ANA-06 (line 1217) is unchanged: "each compared pair stores a Discrepancy record" holds, with "pair" read as
  unordered.

## Amendments (3)

| ID | Spec clause | Before (quoted) | After (exact proposed text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.7-1 | Grain list, Discrepancy record (:329) | `one row is one (evaluation_hi, evaluation_lo, quantity, operating point); δ and ρ are non-additive.` | one Discrepancy record is one unordered pair of evaluations (evaluation_lo, evaluation_hi) in canonical orientation — lower tier to higher tier, or earlier to later revision — under one compare-rule version; each of its lines is one (evaluation_hi, evaluation_lo, quantity, operating point); δ is non-additive; re-comparing the pair in either order is a read. | Ruling 250 Q2 (c); design §1.3 | proposed |
| AM-1.7.7-2 | Glossary, Discrepancy record (:259) | `(evaluation_hi, evaluation_lo, quantity, operating point, δ, ρ, per-tier uncertainty where reported)` | `(evaluation_hi, evaluation_lo, quantity, operating point, δ, per-tier uncertainty where reported)`; ρ = compared / base is derived on read from the two evaluations and never stored. | Ruling 250 Q5 (a); DM7 (with δ normalised to the base, a stored ρ is a second definition of one quantity) | proposed |
| AM-1.7.7-3 | Glossary, Discrepancy record (:259), appended | (no clause on which quantities are stored) | Dimensional loads (L, D_i) are not stored lines: at a compatible pair (equal reference quantities, operating point and water) they differ by the same fraction as CL and CDi, and are shown side by side without a δ. | Ruling 250 Q5 (a); design §1.4 | proposed |

## Not in this note

The dimensional-δ option for planform-changing revision compares (DR-A3D-3 (c)) is a separate amendment candidate,
recorded by Ruling 250 Q3 as a next step outside this wave for the operator.
