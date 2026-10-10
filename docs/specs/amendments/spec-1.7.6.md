---
id: spec-amendments-1-7-6
title: "Spec 1.7.6 amendment batch — Export (EXP-02, EXP-03, the format table, F5, the TE-floor label), A1 to A14, as exact text"
type: spec
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, rulings, export, dat, te-floor]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-5, rel: relates-to}
  - {to: design-export, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  Fourteen amendments to cfd-workbench-v1, traced to Rulings 193 to 196 and to section 14 of the Export design. The operator
  approved the batch as one in Ruling 196. Revision 1.7.6 of the spec carries it; the change record is Appendix H, section H.6.
---

# Spec 1.7.6 amendment batch

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Status:** all fourteen rows are **approved — Ruling 196** (operator, 2026-10-09), applied by track `trk-dat` with the first Export slice (the section .dat).

## How to read this

- **One row is one amendment**, in the form of [spec 1.7.5](spec-1.7.5.md). *Before* quotes the 1.7.5 text; *After* is the exact new text in
  revision 1.7.6 (struck text is superseded and stays in place). The ids A1 to A14 are those of
  [docs/design/export.md](../../design/export.md) section 14, where each row also gives its reason.
- **Source:** Rulings 193, 194, 195 (the TE floor, "app default, no source") and 196 (the batch). The copy rows are DESIGN.md §7 COPY-474 onward.
- Line numbers are those of the 1.7.5 spec.

## Amendments (14)

| ID | Spec clause | Before (quoted) | After (exact new text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.6-1 (A1) | F5 node L (:1655) | `Choose format, unit and tolerance` | `Choose format and tolerance; the unit is fixed at millimetres for STL and 3MF` | Ruling 196; EXP-03 fixes the unit | approved |
| AM-1.7.6-2 (A2) | EXP-02 (:1217) | (no clause on the STL/3MF deviation) | **Given** STL or 3MF, **then** the deviation shown is the largest value measured at cell midpoints against the evaluator's own surface and is labelled sampled, not a bound. | Ruling 196; the geometry certificate has no export certificate | approved |
| AM-1.7.6-3 (A3) | Format table, STL / 3MF (print) row (:625) | `millimetres; 3MF unit attribute; STL unit in the file name and dialog; never pre-scaled` | the same, then: a closed solid: the evaluator's skin at a user-chosen tolerance plus tip caps, a trailing-edge strip when the section is open and a root weld by mirror, or a starboard half closed with a root cap at y = 0; no CAD kernel; refused if any edge of the written bytes is not shared by exactly two triangles. | Ruling 194 (3); build condition B2 | approved |
| AM-1.7.6-4 (A4) | Format table, DAT row (:623) | `shortest-round-trip digits; original bytes retained on import` | the row, then: one section per file: at a station (default; camber plus and minus half the thickness measured vertically, peak thickness equal to the station t/c) or the authored profile, unscaled; chord fractions, twist not applied; the name line carries foil, station and revision and never begins with two numbers | Ruling 194 (1); build condition B6 | approved |
| AM-1.7.6-5 (A5) | EXP-02 (:1217), the sentence under the format table (:633) | `Every geometric export repeats the TE-floor DRC finding when the trailing edge is below the setting.` | EXP-02 adds: every geometric export shows a trailing-edge row: the least trailing-edge thickness along the exported surface in mm (for a .dat, at that station from the written points), where it is, the floor with its label, and "Manufacturing: not assessed (no process chosen)"; a closed trailing edge reads 0.00 mm; a thickness below the floor adds the advisory finding. The sentence under the table points to it. | Ruling 195 | approved |
| AM-1.7.6-6 (A6) | EXP-03 (:1218) | (no 3MF metadata clause) | the 3MF metadata keys are Title, Description (revision, tolerance, measured deviation) and Application; never the user name, a path or the machine name | Ruling 196; privacy rule (:1353, :1355) | approved |
| AM-1.7.6-7 (A7) | A5.6 Export dialog string (:1012-1017) | `Export dialog — "This geometry has not been checked for strength, …"` | the string shows for every geometric format, including .dat | Ruling 196 | approved |
| AM-1.7.6-8 (A8) | EXP-02 (:1217) | (no reading rule) | Export reads the accepted revision, never an open draft, and refuses when the geometry check has not passed | Ruling 196; AnalysisService gate | approved |
| AM-1.7.6-9 (A9) | Area table, Export row (:1422) | `Choose format · Write` | `Choose format · Export…`; Section ▸ Export .dat…; the `cfdw export` verb | Ruling 194 (4) | approved |
| AM-1.7.6-10 (A10) | Format table, DAT row (:623) | `shortest-round-trip digits` | shortest round-trip digits in positional notation, never exponent form | Ruling 196; build condition B5 (the first probe wrote `3.585447714271229E-05`) | approved |
| AM-1.7.6-11 (A11) | Setting registry `manufacturing.te_floor` (:1035), GEO-12 (:1190) | `label "practitioner value, unverified"` | label "app default, no source"; the value 0.3 mm, the analysis numerics and the settings hash do not change | Ruling 195; build condition B10 | approved |
| AM-1.7.6-12 (A12) | F5 (:1657, :1660) | `M -->|No| N[Explain; TE floor finding; return to geometry]` | the TE-floor finding is an advisory on the success path and never blocks; the failure branch keeps only the explanation | Ruling 194 (5); GEO-12 | approved |
| AM-1.7.6-13 (A13) | Copy table, TE below floor (:2302) | `"Trailing edge <t> mm below the floor <f> mm (practitioner value, unverified)"` | `"Trailing edge <t> mm, below the floor of <f> mm (app default, no source)"` | Ruling 195 | approved |
| AM-1.7.6-14 (A14) | EXP-02 (:1217), F5 (:1660) | (the spec already says "deviation" for meshes) | "deviation" is the word for mesh and .dat fidelity; "gap" stays for the trailing-edge gap | Ruling 196 | approved |

## Built by the .dat slice

The section .dat (track `trk-dat`) builds A4, A5, A7, A8, A10, A11, A12 and A13 for the .dat. A1 and A14 are wording. A2, A3, A6 and the `cfdw export` verb of A9 belong to
the STL, 3MF and CLI slices; their spec text is applied now so the spec matches the approved batch.
