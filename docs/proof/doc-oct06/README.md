---
id: proof-doc-oct06
title: "Track DOC, round-oct06 — new copy rows and spec 1.7.5"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: implementation
tags: [proof, copy, spec, rulings]
links:
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  Rows COPY-250 to COPY-356 added to DESIGN.md section 7 for Rulings 101, 107 and 108, the reason-code drafts, and spec 1.7.5.
---

# Track DOC — copy rows and spec 1.7.5 (round-oct06)

107 rows were appended to DESIGN.md section 7 (COPY-250 to COPY-356). No existing `| COPY-` row changed. `compare.txt` shows one line per row against its source; `rows.py` made both the rows and `compare.txt` (`python3 rows.py compare <repo> <out>`). Rows with a templated or concatenated source (marked with `<…>`) are checked by their literal fragments in the cited file, not by whole-string equality.

| Group | Ids | Count | Source | Status |
|---|---|---|---|---|
| Ruling 101: built strings without a row, and the mockup-only strings | COPY-250 to COPY-280 | 31 | the cited code line, or docs/mockups/area3-analysis.html rev 3 | approved — Ruling 101 |
| Ruling 107: COPY-G1 to G12 | COPY-281 to COPY-292 | 12 | docs/design/group-move-node-m.md section 5 | approved — Ruling 107 |
| Ruling 108: NEW rows of the DX table | COPY-293 to COPY-333 | 41 | docs/design/dx-screen-states.md | approved — Ruling 108 (DR-DXM-1) |
| Reason-code drafts | COPY-334 to COPY-352 | 19 | drafted by DOC | proposed — awaiting operator |
| Total drag | COPY-353 to COPY-356 | 4 | Loads.cs:9 (Ruling 101); the hydrodynamicist's verdict | Ruling 101 row approved; three rows proposed — awaiting operator (HYD conditions on Ruling 108 DXM-5) |

## Row to source

| Id | Text | Source |
|---|---|---|
| COPY-250 | Unavailable — <reason> | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-251 | ; <n> strips: Unavailable — <reason> | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-252 | ; <n> Not judged — tip strip | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-253 | Unavailable — this run's geometry revision is not held by this session; verdicts, stations and normals omitted | src/CfdWorkbench.Analysis/Labels.cs |
| COPY-254 | Inside the method envelope at this strip (<parts>) | src/CfdWorkbench.Analysis/MethodRecord.cs |
| COPY-255 | Outside the method envelope at this strip — exceeded: <names> (<parts>) | src/CfdWorkbench.Analysis/MethodRecord.cs |
| COPY-256 | <name> <value><unit> ≤ <bound><unit> | src/CfdWorkbench.Analysis/MethodRecord.cs |
| COPY-257 | Inside the method envelope (\|α_eff − α_L0\| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) at all <n> strips | src/CfdWorkbench.Analysis/MethodRecord.cs |
| COPY-258 | Outside the method envelope (\|α_eff − α_L0\| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) — <k> of <n> strips; exceeded: <names> | src/CfdWorkbench.Analysis/MethodRecord.cs |
| COPY-259 | Unavailable — no attachment point named (DR-ANA-5) | src/CfdWorkbench.Analysis/Loads.cs |
| COPY-260 | Analysis: no result | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-261 | Analysis: Unavailable | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-262 | Analysis: Failed | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-263 | Analysis: Current | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-264 | Analysis: Historical | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-265 | Unavailable — surface piercing | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-266 | Undefined — speed ≤ 0 | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-267 | tip depth <value> m | src/CfdWorkbench.Desktop/Analysis/View3dLoadLayer.cs |
| COPY-268 | free surface and tip depth, values in the conditions table | src/CfdWorkbench.Desktop/Analysis/View3d.LoadLayer.cs |
| COPY-269 | dashed outline: <n> strip outside the method envelope | src/CfdWorkbench.Desktop/Analysis/PlanLoadLayer.cs |
| COPY-270 | dashed outline: <n> strips outside the method envelope | src/CfdWorkbench.Desktop/Analysis/PlanLoadLayer.cs |
| COPY-271 | Historical — previous result | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-272 | Preview hidden — Apply or Cancel in CAD | src/CfdWorkbench.Desktop/ModelArea.axaml |
| COPY-273 | Outside strips have dashed outlines and a text count. | src/CfdWorkbench.Analysis/AnalysisProjection.cs |
| COPY-274 | The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run. | docs/mockups/area3-analysis.html |
| COPY-275 | η (root → tip) | docs/mockups/area3-analysis.html |
| COPY-276 | Cl·c/c̄ (–) | docs/mockups/area3-analysis.html |
| COPY-277 | dashed: elliptic, same CL | docs/mockups/area3-analysis.html |
| COPY-278 | VLM + strip | docs/mockups/area3-analysis.html |
| COPY-279 | Historical · VLM + strip | docs/mockups/area3-analysis.html |
| COPY-280 | 10 kn · salt 15 °C · as the band | docs/mockups/area3-analysis.html |
| COPY-281 | Moving <n> <curve> points. | docs/design/group-move-node-m.md |
| COPY-282 | Moved <n> <curve> points. Tip chord <value> mm. | docs/design/group-move-node-m.md |
| COPY-283 | <Point name> is locked. Deselect it to move the others. | docs/design/group-move-node-m.md |
| COPY-284 | The <point> can't move along the span, so the selection moves aft only. | docs/design/group-move-node-m.md |
| COPY-285 | Handles move on their own, or with their anchor. Deselect the handle or select its anchor. | docs/design/group-move-node-m.md |
| COPY-286 | The selection is held by point <n>. Points can't close up on a neighbour. | docs/design/group-move-node-m.md |
| COPY-287 | Select points on one curve to move them together. | docs/design/group-move-node-m.md |
| COPY-288 | Moving these points by <typed> would take the tip chord below <min>. The most they can move that way is <amount>. | docs/design/group-move-node-m.md |
| COPY-289 | Points can't share a position along the span. Move them by an amount instead. | docs/design/group-move-node-m.md |
| COPY-290 | Moving these points by <typed> would pass point <n>. The most they can move that way is <amount>. | docs/design/group-move-node-m.md |
| COPY-291 | Set <row> of <n> points to <value>. | docs/design/group-move-node-m.md |
| COPY-292 | Moved <n> points by <signed value>. | docs/design/group-move-node-m.md |
| COPY-293 | inviscid + turbulent-friction bound; deep water; steady · inviscid; no boundary layer | docs/design/dx-screen-states.md |
| COPY-294 | Cp · vik pinned at 0 · −a to +b | docs/design/dx-screen-states.md |
| COPY-295 | cl (panel) | docs/design/dx-screen-states.md |
| COPY-296 | Cm c/4 | docs/design/dx-screen-states.md |
| COPY-297 | α_L0 (panel) | docs/design/dx-screen-states.md |
| COPY-298 | Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value | docs/design/dx-screen-states.md |
| COPY-299 | at x/c <x> on the <side> surface · 200 stations · three trailing-edge panels per side excluded | docs/design/dx-screen-states.md |
| COPY-300 | 15 % margin — practitioner assumption, not sourced | docs/design/dx-screen-states.md |
| COPY-301 | Clear — σ is above −Cp_min plus the margin | docs/design/dx-screen-states.md |
| COPY-302 | Inside the margin — σ is above −Cp_min but within the margin | docs/design/dx-screen-states.md |
| COPY-303 | Possible — σ is at or below −Cp_min; speed is above V_crit | docs/design/dx-screen-states.md |
| COPY-304 | Governing station: η <η> · depth <h> m · smallest σ / (−Cp_min) of <n> stations | docs/design/dx-screen-states.md |
| COPY-305 | Unavailable — vapour pressure missing | docs/design/dx-screen-states.md |
| COPY-306 | Unavailable — local station is surface piercing | docs/design/dx-screen-states.md |
| COPY-307 | Unavailable — water is invalid | docs/design/dx-screen-states.md |
| COPY-308 | Unavailable — local depth is invalid | docs/design/dx-screen-states.md |
| COPY-309 | Undefined — −Cp_min ≤ 0 | docs/design/dx-screen-states.md |
| COPY-310 | Undefined — static pressure does not exceed vapour pressure | docs/design/dx-screen-states.md |
| COPY-311 | Cp_min under-read, 200 vs 400 panels | docs/design/dx-screen-states.md |
| COPY-312 | Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels) | docs/design/dx-screen-states.md |
| COPY-313 | surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only | docs/design/dx-screen-states.md |
| COPY-314 | NeuralFoil-0.3.2/xxxlarge/94638c04 | docs/design/dx-screen-states.md |
| COPY-315 | analysis_confidence 0.97 · advisory, not an error bar | docs/design/dx-screen-states.md |
| COPY-316 | Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar. | docs/design/dx-screen-states.md |
| COPY-317 | CST fit residual: max 1.09 × 10⁻⁴ c · RMS 2.93 × 10⁻⁵ c (shape residual, not an aerodynamic error) | docs/design/dx-screen-states.md |
| COPY-318 | Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family) | docs/design/dx-screen-states.md |
| COPY-319 | Outside the validated bracket — α 7.50° is beyond ±6°. Computed, not validated. | docs/design/dx-screen-states.md |
| COPY-320 | Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°) | docs/design/dx-screen-states.md |
| COPY-321 | Unavailable — section fit residual 4.2 × 10⁻⁴ c exceeds the limit 3.6 × 10⁻⁴ c | docs/design/dx-screen-states.md |
| COPY-322 | Re_local <Re> inside <Re_min> to <Re_max> | docs/design/dx-screen-states.md |
| COPY-323 | Re_local <Re> outside the polar’s Re range <Re_min> to <Re_max> — cd not extrapolated | docs/design/dx-screen-states.md |
| COPY-324 | Δ vs lattice Cl_local | docs/design/dx-screen-states.md |
| COPY-325 | polar cl at α_eff against the lattice, a per-strip consistency check | docs/design/dx-screen-states.md |
| COPY-326 | Polar bracket: | docs/design/dx-screen-states.md |
| COPY-327 | Overlay a section ▾ | docs/design/dx-screen-states.md |
| COPY-328 | Profile drag from the polar at α_eff, both Ncrit; band, not a prediction | docs/design/dx-screen-states.md |
| COPY-329 | Unavailable — cd missing at <n> strips; the estimator bound is not substituted | docs/design/dx-screen-states.md |
| COPY-330 | Wing only: induced (VLM + strip) plus profile (polar). Not a total. | docs/design/dx-screen-states.md |
| COPY-331 | Unavailable — missing: junction, mast, wave, spray | docs/design/dx-screen-states.md |
| COPY-332 | α <a>° meets CL <t> within 1 % | docs/design/dx-screen-states.md |
| COPY-333 | Find α found no α — <reason>. Nothing was extrapolated. | docs/design/dx-screen-states.md |
| COPY-334 | Unavailable — drag could not be computed. Evaluate again. | drafted by DOC |
| COPY-335 | Unavailable — wing drag is missing or zero, so CL/CD can't be formed. | drafted by DOC |
| COPY-336 | Wing only: lift over wing drag. Not a craft CL/CD. | drafted by DOC |
| COPY-337 | Cd (turbulent bound) | drafted by DOC |
| COPY-338 | Unavailable — induced drag is missing. | drafted by DOC |
| COPY-339 | Unavailable — missing: profile, junction, mast, wave, spray | drafted by DOC |
| COPY-340 | Unavailable — the run has no strips. | drafted by DOC |
| COPY-341 | Unavailable — a strip width is not recorded. | drafted by DOC |
| COPY-342 | Unavailable — a drag sum is not a finite number. Evaluate again. | drafted by DOC |
| COPY-343 | Unavailable — no section profile for this strip. | drafted by DOC |
| COPY-344 | Unavailable — the polar gave no result for this strip. | drafted by DOC |
| COPY-345 | Unavailable — the polar gave no drag value at this strip. | drafted by DOC |
| COPY-346 | Unavailable — the stored polar was made with another method or profile. Evaluate to compute a new run. | drafted by DOC |
| COPY-347 | Unavailable — the section revision for this polar is not held by this session. | drafted by DOC |
| COPY-348 | Unavailable — Re <Re> is outside the surrogate’s training range (<min> to <max>) | drafted by DOC |
| COPY-349 | Unavailable — Ncrit <n> is outside the surrogate’s training range (<min> to <max>) | drafted by DOC |
| COPY-350 | Unavailable — this section family is not covered by the surrogate. | drafted by DOC |
| COPY-351 | Unavailable — the polar did not converge at this strip. | drafted by DOC |
| COPY-352 | Unavailable — the polar could not be computed for this section. This is a program fault; the run is kept. | drafted by DOC |
| COPY-353 | Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray | src/CfdWorkbench.Analysis/Loads.cs |
| COPY-354 | Drag (Wing only) | hydrodynamicist verdict (message) |
| COPY-355 | <min>–<max> N | hydrodynamicist verdict (message) |
| COPY-356 | Not included: junction, mast, wave, spray | hydrodynamicist verdict (message) |

## COPY-G mapping (Ruling 107)

| Group-move id | DESIGN.md id |
|---|---|
| COPY-G1 | COPY-281 |
| COPY-G2 | COPY-282 |
| COPY-G3 | COPY-283 |
| COPY-G4 | COPY-284 |
| COPY-G5 | COPY-285 |
| COPY-G6 | COPY-286 |
| COPY-G7 | COPY-287 |
| COPY-G8 | COPY-288 |
| COPY-G9 | COPY-289 |
| COPY-G10 | COPY-290 |
| COPY-G11 | COPY-291 |
| COPY-G12 | COPY-292 |

## NEW rows in docs/design/dx-screen-states.md

40 table lines contain "NEW" (grep on lines 50-115). They give 41 distinct strings with drafted text, all added (the COPY-293 to COPY-333 rows). Rows 47 and 49-50 share or extend strings of other rows; row 47 repeats row 7's string. NEW but with **no text drafted**, so no row could be written verbatim (the operator supplies the words): row 10 (none proposed), row 11 (cavitation row labels), row 44 (the transition legend line), row 45 (the bucket legend text), row 53 (Find α row labels), row 54 (the five reason sentences), row 36 (the Re, Ncrit and section-family variants; only the α sentence is drafted), row 31 and 33 and 34 use fixture numbers as examples.

## Reason codes a user can see

Traced from `grep -rhoE '"ANA-[A-Z0-9-]+' src/CfdWorkbench.Analysis`. Ruling 108 approved display text for these but none was drafted. Codes that reach a projection row, note or banner are below; each line is a draft for the operator (rows marked proposed), or maps to an already approved row. Not reaching a row: ANA-INPUT-*, ANA-NONFINITE, ANA-SOLVE-*, ANA-PANEL-*, ANA-CAV-INPUT, ANA-SECTION-GEOMETRY/-RE/-ZEROLIFT/-UNAVAILABLE/-REVISION-MISMATCH, ANA-POLAR-CST-INPUT (thrown as errors; a failed run shows its own sentence plus the code in parentheses, AnalysisProjection.cs:37, and the Section group catches the section errors, AnalysisProjection.cs:73-76), ANA-UNEXPECTED and ANA-CANCELLED (telemetry outcome, AnalysisService.cs:84, :106), ANA-TIP-BELOW-FLOOR (refusal sentence COPY-241), ANA-EDIT-INERT (refusal sentence), ANA-FIND-* (no Find alpha UI yet; OperatingSearch.cs), ANA-FREE-SURFACE-* (on the view model only; no Desktop reader), ANA-TIP-PROVISIONAL and ANA-POLAR-CONSISTENCY-* (PolarConsistency on the view model only, AnalysisProjection.cs:168; DX build will need text).

| Code | Where it shows | Text |
|---|---|---|
| ANA-DRAG-UNAVAILABLE | AnalysisProjection.cs:232 (drag row value, fallback) | COPY-334: Unavailable — drag could not be computed. Evaluate again. |
| ANA-WING-RATIO-UNAVAILABLE | AnalysisProjection.cs:245 (Wing-only CL/CD value) | COPY-335: Unavailable — wing drag is missing or zero, so CL/CD can't be formed. |
| ANA-WING-ONLY-RATIO | AnalysisProjection.cs:248 (Wing-only CL/CD note) | COPY-336: Wing only: lift over wing drag. Not a craft CL/CD. |
| ANA-SECTION-ITTC1957-BOUND | AnalysisProjection.cs:192 (Section row label; the DX row 7 sentence is its note) | COPY-337: Cd (turbulent bound) |
| ANA-TOTAL-DRAG-MISSING-INDUCED | Loads.cs:51 via AnalysisProjection.cs:232 (Total drag value) | COPY-338: Unavailable — induced drag is missing. |
| ANA-TOTAL-DRAG-MISSING-PROFILE | Loads.cs:53 via AnalysisProjection.cs:232 (Total drag value when a polar is installed but the profile part is missing) | COPY-339: Unavailable — missing: profile, junction, mast, wave, spray |
| ANA-PROFILE-DRAG-MISSING-STRIPS, ANA-INDUCED-DRAG-MISSING-STRIPS | Loads.cs:68, :88 via AnalysisProjection.cs:232 (Profile and Induced drag value) | COPY-340: Unavailable — the run has no strips. |
| ANA-PROFILE-DRAG-MISSING-WIDTH, ANA-INDUCED-DRAG-MISSING-WIDTH | Loads.cs:77, :93 via AnalysisProjection.cs:232 | COPY-341: Unavailable — a strip width is not recorded. |
| ANA-PROFILE-DRAG-NONFINITE, ANA-INDUCED-DRAG-NONFINITE | Loads.cs:83, :97 via AnalysisProjection.cs:232 | COPY-342: Unavailable — a drag sum is not a finite number. Evaluate again. |
| ANA-POLAR-PROFILE-MISSING | StripCoupler.cs:33 and NeuralFoilPolarSource.cs:100 via AnalysisProjection.cs:232 and :213 (PolarText) | COPY-343: Unavailable — no section profile for this strip. |
| ANA-POLAR-UNAVAILABLE | StripCoupler.cs:67 via AnalysisProjection.cs:232 | COPY-344: Unavailable — the polar gave no result for this strip. |
| ANA-POLAR-CD-UNAVAILABLE | StripCoupler.cs:72 via AnalysisProjection.cs:232 | COPY-345: Unavailable — the polar gave no drag value at this strip. |
| ANA-POLAR-METHOD-MISMATCH | SectionTier.cs:42 and NeuralFoilPolarSource.cs:126 via AnalysisProjection.cs:213 (Ncrit rows) | COPY-346: Unavailable — the stored polar was made with another method or profile. Evaluate to compute a new run. |
| ANA-POLAR-REVISION-MISSING, ANA-POLAR-REVISION-MISMATCH, ANA-POLAR-PROFILE-HASH | RunPolarResolver.cs:15, :19 and NeuralFoilPolarSource.cs:102 via StripCoupler.cs:74 / SectionTier.cs:59 | COPY-347: Unavailable — the section revision for this polar is not held by this session. |
| ANA-POLAR-RE-OUTSIDE | IPolarSource.cs:28 via SectionTier.cs:57 (Ncrit rows); the α sentence is DX row 37 | COPY-348: Unavailable — Re <Re> is outside the surrogate’s training range (<min> to <max>) |
| ANA-POLAR-NCRIT-OUTSIDE | IPolarSource.cs:31 via SectionTier.cs:57 | COPY-349: Unavailable — Ncrit <n> is outside the surrogate’s training range (<min> to <max>) |
| ANA-POLAR-SECTION-UNVALIDATED | IPolarSource.cs:29 via SectionTier.cs:57 | COPY-350: Unavailable — this section family is not covered by the surrogate. |
| ANA-POLAR-NOT-CONVERGED | IPolarSource.cs:32 via SectionTier.cs:57 | COPY-351: Unavailable — the polar did not converge at this strip. |
| ANA-POLAR-NONFINITE, ANA-POLAR-INPUT, ANA-POLAR-CST-INPUT, ANA-POLAR-CST-FIT, ANA-POLAR-WEIGHTS-MISSING, ANA-POLAR-WEIGHTS-HASH, ANA-POLAR-WEIGHTS-FORMAT | NeuralFoilNetwork.cs, CstFit.cs, Naca0012Reference.cs via StripCoupler.cs:74 / SectionTier.cs:59 (any ANA-POLAR- code becomes the reason) | COPY-352: Unavailable — the polar could not be computed for this section. This is a program fault; the run is kept. |
| ANA-CAV-DEPTH-NOT-SET | Cavitation.cs:21, shown at AnalysisProjection.cs:198-199 (Cavitation row) | already COPY-45 `Unavailable — depth not set` |
| ANA-CAV-PV-MISSING | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-305 (Ruling 108, DR-DXM-1): Unavailable — vapour pressure missing |
| ANA-CAV-SURFACE-PIERCING | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-306 (Ruling 108, DR-DXM-1): Unavailable — local station is surface piercing |
| ANA-CAV-WATER-INVALID | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-307 (Ruling 108, DR-DXM-1): Unavailable — water is invalid |
| ANA-CAV-DEPTH-INVALID | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-308 (Ruling 108, DR-DXM-1): Unavailable — local depth is invalid |
| ANA-CAV-NO-SUCTION | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-309 (Ruling 108, DR-DXM-1): Undefined — −Cp_min ≤ 0 |
| ANA-CAV-PRESSURE-NONPOSITIVE | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-310 (Ruling 108, DR-DXM-1): Undefined — static pressure does not exceed vapour pressure |
| ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-331 (Ruling 108, DR-DXM-1): Unavailable — missing: junction, mast, wave, spray |
| ANA-PROFILE-DRAG-MISSING-CD | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-329 (Ruling 108, DR-DXM-1): Unavailable — cd missing at <n> strips; the estimator bound is not substituted |
| ANA-POLAR-LOW-CONFIDENCE | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-316 (Ruling 108, DR-DXM-1): Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar. |
| ANA-POLAR-ALPHA-OUTSIDE | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-320 (Ruling 108, DR-DXM-1): Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°) |
| ANA-POLAR-NONCOMPUTABLE | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-321 (Ruling 108, DR-DXM-1): Unavailable — section fit residual 4.2 × 10⁻⁴ c exceeds the limit 3.6 × 10⁻⁴ c |
| ANA-WING-ONLY-DRAG | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-330 (Ruling 108, DR-DXM-1): Wing only: induced (VLM + strip) plus profile (polar). Not a total. |
| ANA-SECTION-ITTC1957-BOUND (note) | Cavitation.cs:20-26 / Loads.cs / AnalysisProjection.cs (value or note cell) | already COPY-298 (Ruling 108, DR-DXM-1): Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value |

The code line `ANA-PROFILE-DRAG-MISSING-CD:<reason>` carries the first missing strip's reason, not a count of strips; the approved text names `<n>` strips. The code must give the count (a DX build item). The DX rows 19 to 23 text is not in the code now: Cavitation.cs returns the codes, so the code and not the sentence reaches the cell until the DX build maps them.

## Findings

- The Ruling 101 table in `docs/reviews/a3a-native.md` section 7 is stale against the head: the cited lines moved, the cavitation reasons are now codes (not sentences), and the total-drag strings are now `Loads.TotalDragReason` plus the code path. Each row was taken from the head's code or the mockup, not the table.
- The AM-1.7-15 note is under F11 node M in the spec (the paragraph beginning "node M's typed clause"), not by that id; it is struck through in 1.7.5.
- A bare `Unavailable` is still built at the head (AnalysisProjection.cs:20 and :402). COPY-250 records the approved form; the CPY track changes the code.
- The hydrodynamicist's value is `<min>–<max> N`; the code shows lbf under Imperial units. COPY-355 keeps the verdict's text; the operator confirms the unit.

## Spec 1.7.5

Revision line `Product specification · revision 1.7.5 · 6 October 2026 ·` is a plain prefix (grep count 1). CAD-04 clause and the node M note retirement: approved (Ruling 107 DR-GM-8). A5.6 and ANA-03: the hydrodynamicist's form, marked "proposed — awaiting operator (HYD conditions on Ruling 108 DXM-5)" in the spec text, in section H.5 and in `docs/specs/amendments/spec-1.7.5.md`. The change record is Appendix H, section H.5.

## What waits on the operator

1. The A5.6 and ANA-03 clause and COPY-354 to COPY-356: the hydrodynamicist's form differs from Ruling 108's text (label `Drag (Wing only)`, craft Total drag stays Unavailable, new reason line). Also the Imperial unit of the band.
2. The reason-code drafts (rows marked proposed, 19 rows).
3. NEW rows with no drafted text (listed above).
4. The `<n>` count for ANA-PROFILE-DRAG-MISSING-CD needs a code change (CPY or DX), not a copy decision.
