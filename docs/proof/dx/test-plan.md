---
id: proof-dx-test-plan
title: "DX test plan and state coverage (round-oct06 step 1)"
type: proof-pack
status: in-review
owner: "@trk-dx"
phase: implementation
tags: [analysis, a3b, a3c, test-plan, dx]
links:
  - {to: design-dx-screen-states, rel: depends-on}
  - {to: mockup-dx-section-polar-states, rel: relates-to}
review-by: 2026-12-31
summary: >-
  Which of the 54 A3b and A3c states the approved mockup draws, the states to show the operator first, and 39 named
  checks (35 fast, 4 readiness) covering every state, DXM-2..9 and Charts_TransitionAndBucket_Overlays.
---

# DX test plan and state coverage (round-oct06, step 1)

Track `trk-dx` · branch `feature/dx-section-polar` · inputs: `docs/design/dx-screen-states.md` (rows 1-54), mockup
`docs/mockups/dx-section-polar-states.html` (approved, Ruling 108), Rulings 108, 109, 110, 113, 114.
Status: names for the test-architect to clear; nothing is built.

## 1. Coverage of the 54 states by the approved mockup

Read from the mockup source (screens D1-D6 and the D7 state sheet). Key: D1 Cp, D2 polar, D3 transition, D4 bucket,
D5 drag, D6 Find α, D7 sheet; "approved" = the earlier approved Area 3 mockup (rev 3), per the state table.

| Rows | In an approved mockup? |
|---|---|
| 1-8, 11-13, 17 | Yes: D1 (17 also D4) |
| 9, 26, 40, 51 | Yes: approved Area 3 mockup (row 9 is stale once A3b is wired; see 3) |
| 14, 16, 19-25 | Yes: D7 sheet (cavitation block); 15 in D4 and D7 |
| 18 | Yes in part: σ cell unavailable (approved); the cavitation group "depth not set" cell is in D7 |
| 27-30, 41, 42 | Yes: D2 (42's verdict is in the approved mockup) |
| 31-39, 43, 46-48, 50, 52, 54 | Yes: D7 sheet (polar, drag, Find α blocks) |
| 44 | Yes: D3 |
| 45 | Yes: D4 |
| 49 | Drawn in D5, but **superseded** by Ruling 109 (see 2) |
| 53 | Yes: D6 (drawn as a popover; Ruling 108 DXM-6 says dialog; same fields) |

## 2. States NOT in the mockup as approved (show the operator before the build)

1. **Row 10** (Cp Unavailable after A3b: no accepted profile or failed panel solve). Text approved (Ruling 116: "no accepted
   section profile at this station", "the panel solve failed at this station. Evaluate again."); still not drawn. It is a plain
   `.row.na` cell like row 24's; build it and show it in the app capture.
2. **Drag (Wing only), the merged row (Ruling 109).** D5 draws separate "Wing drag" and "Total drag" rows (Ruling 108's
   superseded wording). The approved shape is one row, labelled "Drag (Wing only)", band plus surrogate label plus
   low-confidence flag, note "Wing only: induced (VLM + strip) plus profile (polar). Not a total.", reason line "Not included:
   junction, mast, wave, spray". Rows 49 and 50 are therefore drawn in a layout the operator replaced. Ruling 109 says the
   operator saw a preview, but no committed mockup shows it. CPY built the Loads-tab row; DX adds the Section-tab
   polar/profile parts only. The capture of this row from the app goes beside Ruling 109's text for sign-off.
3. **The not-measured under-read state (Rulings 110, 113).** Text approved ("Cp_min under-read not measured at this station
   (200 panels only)") but no rendering of it, or of the on-demand solve for the shown station, is in the mockup. The
   rendering is a `.row.na` cell like row 24's; low risk. Show it in the app capture.
4. **Station naming (DXM-9)**: D1 names "Station η" in the status bar only; the Section tab's rule (selected strip, else
   governing station, named) has no drawing for the "selected strip" variant. Show both variants in the app capture.
5. **Stale row 9** (COPY-212 "no section Cp method"): approved text that can no longer occur once A3b is wired; the
   build retires it (check below). Not a missing state.
6. **Provisional row 25 with 400-panel governing re-selection (Ruling 110(2), 114)**: the drawing exists (D7); the
   behaviour change (governing chosen at 400) is not a screen state.

Copy: Ruling 116 (main 812f966e, merged here) approves COPY-334..352, the undrafted DX rows 10, 11, 36, 44, 45, 53, 54 and
plain text for CPY's eight `ANA-*` codes. No cell keeps a raw code. All text is read through the one `Labels` lookup.

## 3. Named checks

Ring 0 = fast ring (every join). Analysis-level checks run in the Analysis harness (tens of ms each). A Desktop check
that builds a window costs about 150-600 ms; those go to the readiness ring (R), the rest are fast (F). The fast ring is
at 48 of 50 s, so each fast check below stays under about 10 ms except where marked. Costs are estimates until
measured; `COST` lines will replace them in the proof.

### 3.1 Section tab, estimator, cavitation screen (rows 1-26)

| Check | Rows | Assertion (fails on the old code) | Ring · est. |
|---|---|---|---|
| `Section_TabBody_BuildsChartSelector` | 1, 2 (DXM-4) | the Section tab body has one chart selector with exactly Cp, Polar, Transition, Bucket and a table-twin toggle; the old text table of the group is gone | R · 300 ms |
| `SectionProjection_CpPlot_SeriesAndMarker` | 2, 8 | for the example foil, the Cp series has 200 stations; upper and lower are separate series (upper solid, lower dashed flags); Cp_min equals the section tier's value and its marker carries side and x/c; the three TE panels per side are named as excluded | F · 20 ms |
| `SectionProjection_EstimatorLabel_DepthAware` | 3 | the label reads the approved string; with depth unset "free surface not modelled" replaces "deep water" and "deep water" appears nowhere | F · 2 ms |
| `SectionProjection_EstimatorChip_Copy214` | 4 | the estimator chip text is COPY-214 | F · 1 ms |
| `SectionProjection_Values_ClCmAlphaL0PerSpan` | 5, 6, 7 | rows cl (panel), Cm c/4, α_L0 (panel) carry values and units; the cd row is the turbulent bound with its "a bound, not a polar value" note; vik pinned at 0 in the legend | F · 5 ms |
| `SectionView_CpOnProfile_DrawsAndPinsVik` | 5 | the Section view's Cp overlay draws a polyline per side with vik at 0 and the Cp_min marker (geometry test on the model, no window) | F · 10 ms |
| `Cavitation_Screen_ValueStateAndFixedString` | 11, 12, 13 | σ, −Cp_min, V_crit rows plus COPY-48 with N = 200 and the margin label "15 % margin — practitioner assumption, not sourced" | F · 5 ms |
| `Cavitation_MarginStates_ClearInsidePossible` | 14, 15, 16 | σ at 1.20, 1.07 and 0.95 times −Cp_min gives the three state sentences; the boundary σ = −Cp_min is Possible; the margin edge σ = 1.15 × is Inside | F · 2 ms |
| `Cavitation_GoverningStation_NamedWithDepth` | 17 | the group names η, depth and the station count ("smallest σ / (−Cp_min) of n stations"); mutant: α_geo or h_ref used | F · 30 ms |
| `Cavitation_UnavailableAndUndefined_ReasonRows` | 18-23 | each cause (depth unset, vapour pressure, piercing, water invalid, local depth invalid; −Cp_min ≤ 0; static pressure not above vapour) shows its row through `Labels`, never blank, never a number; depth unset keeps Cp plotting | F · 5 ms |
| `Cavitation_PanelUnderread_MeasuredNotConstant` | 24 | the under-read row shows the run's measured value; the constant 1.61 % appears nowhere | F · 20 ms |
| `Cavitation_Provisional_AboveTenPercent` | 25 (DXM-7) | under-read 12.4 % gives COPY-312; 9.99 % and exactly 10 % do not; a planted thin non-governing station above 10 % shows it | F · 5 ms |
| `Section_UnderreadNotMeasured_ShowsRatifiedText` | 24, 25 (Ruling 113) | a station not solved at 400 shows the Ruling 113 text; never blank, never OK, never COPY-312 | F · 5 ms |
| `Section_Cp_NoMethodString_Retired` | 9 | the COPY-212 string is no longer reachable from the Section (2D) group when a panel result exists | F · 2 ms |
| `Section_StripCdNoPolar_Copy210` | 26, 40 | with no polar installed the strip cd and "Polar Re range" cells read COPY-210; "inside" never appears | F · 2 ms |

### 3.2 Polar tier, flags, drag, Find α (rows 27-54)

| Check | Rows | Assertion | Ring · est. |
|---|---|---|---|
| `Polar_Chart_BandAndAlphaEffMarker` | 27 | the polar chart model has cl and cd series at Ncrit 4 and 2, the cd band between them, the α_eff marker; points outside the validated bracket are absent (not extrapolated); band note is COPY-44 | F · 10 ms |
| `Polar_TierChipAndLabels_Dxm3` | 28, 29 (DXM-3) | chip text is "Polar · local calculation"; COPY-66 and "surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only" are both present | F · 2 ms |
| `Provenance_PolarMethodId_Shown` | 30 | the Method row reads `NeuralFoil-0.3.2/xxxlarge/94638c04` style, not "polar none", when a polar ran | F · 2 ms |
| `Polar_Confidence_PresentNotRecordedLow` | 31, 32, 33 | 0.97 shows the advisory string; null shows "analysis_confidence: not recorded"; 0.31 shows the low-confidence advisory and the value is still shown (never refused); 0.5 exactly is not low | F · 3 ms |
| `Polar_CstResidual_ShownAndLimit` | 34, 37 | residual max and RMS strings shown for a section; a residual over 3.6 × 10⁻⁴ c shows the Unavailable string and no number | F · 3 ms |
| `Polar_Bracket_FlaggedPerAxis_Dxm2Dxm8` | 35, 36, 42 (DXM-2, DXM-8) | inside shows the "Inside the validated bracket" row; α 7.5°, Re 1.7 × 10⁵, Ncrit 3 and a non-NACA 0012 section each show a flag naming its axis and a computed value; the attached-flow envelope verdict row is unchanged beside it | F · 5 ms |
| `Polar_OutsideTrainingRange_UnavailableNoNumber_Dxm2` | 37 (DXM-2) | α 31°, Re 80 and a CST residual past the limit each give Unavailable with the reason and no value or chart point | F · 3 ms |
| `Strips_PolarRe_InsideOutsideNotExtrapolated` | 38, 39, 41 | Re_local inside the range shows the inside row; outside shows the "cd not extrapolated" row and cd Unavailable; the polar-vs-lattice Δ row shows the Δ value with its note | F · 5 ms |
| `Polar_TrippedSurface_NotComputed` | 43 | a tripped surface shows "Unavailable — not computed" | F · 1 ms |
| `Charts_TransitionAndBucket_Overlays` | 44, 45 (ANA-10, `area3-analysis.md:771`) | transition chart: four series (upper and lower, Ncrit 4 and 2), each with distinct dash and marker, legend line naming revision, Re, Ncrit, surface state and α range; bucket chart: σ-required and V_crit against Cl with the operating line, labelled inviscid, and COPY-48 beside; an overlay of a section other than the shown one is offered only through "Overlay a section" | F · 20 ms |
| `Drag_ProfileSources_BandBoundKeptApart` | 46, 47 | the polar profile drag shows a band at both Ncrit with its note; the estimator bound is a separate value with its own chip; no cell sums the two | F · 5 ms |
| `Drag_StripsMissingCd_NamesCountNoSubstitute` | 48 | n strips without cd gives "cd missing at n strips; the estimator bound is not substituted"; mutant: bound substituted | F · 5 ms |
| `Drag_WingOnly_OneRow_Ruling109` | 49, 50 | one row "Drag (Wing only)" holds the band, the note, the surrogate label, the low-confidence flag and the reason line "Not included: junction, mast, wave, spray"; no separate "Wing drag" or "Total drag" value appears; craft Total drag and craft CL/CD stay Unavailable; only Wing-only CL/CD shows a number; the force unit follows the Units setting (kept from CPY) | F · 5 ms |
| `Drag_TotalNoProfile_StillNamesProfile` | 51 | with no polar, the Wing-only row reads Unavailable and names profile among the missing; this keeps the approved string | F · 2 ms |
| `Drag_CdNonPositive_Undefined_Copy230` | 52 | CD ≤ 0 reads COPY-230 | F · 1 ms |
| `FindAlpha_Dialog_ResultAndBasisRows` | 53 | for a target CL the result sentence "α a° meets CL t within 1 %", bracket, CL at both ends, solves, termination, polar bracket and confidence, and the CL_max string (ANA-05) are present; the basis row carries the A5.1 string at h/c below 5 | F · 500 ms (lattice solves; moves to R if measured above 500 ms) |
| `FindAlpha_NoRoot_FiveReasons` | 54 | each of the five reasons (no sign change, polar not converged, confidence below floor, out of Re envelope, h/c below floor) shows "Find α found no α — reason. Nothing was extrapolated." and no α | F · 5 ms |
| `FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6` | 53 (DXM-6) | the Find α button sits in the conditions band next to Evaluate; Apply writes α into the band and records no run; the run list and the Section tab stay as before until Evaluate is pressed | R · 400 ms |

### 3.3 Decisions DXM-2..9 and rulings, one pin each

| Pin | Check (above or here) | Ring · est. |
|---|---|---|
| DXM-2 flagged inside training range, Unavailable outside or past CST limit | `Polar_Bracket_FlaggedPerAxis_Dxm2Dxm8` and `Polar_OutsideTrainingRange_UnavailableNoNumber_Dxm2` | F |
| DXM-3 chip and labels | `Polar_TierChipAndLabels_Dxm3` | F |
| DXM-4 one Section tab and view, chart selector | `Section_TabBody_BuildsChartSelector`; new `SectionView_OneView_SelectorDrivesChart` asserts the chart selector changes the chart and the Section view stays one view | R · 300 ms |
| DXM-5 → Ruling 109 | `Drag_WingOnly_OneRow_Ruling109` | F |
| DXM-6 Find α button and Apply | `FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6` | R |
| DXM-7 provisional row | `Cavitation_Provisional_AboveTenPercent` | F |
| DXM-8 as built | `Polar_Bracket_FlaggedPerAxis_Dxm2Dxm8` | F |
| DXM-9 selected strip else governing, named, calls `SectionTier.UnderreadAt` | new `Section_ShownStation_SelectedElseGoverning_Dxm9`: selected strip shown and named; none selected shows the governing station and its η; the on-demand under-read is requested for the shown station only and never changes the screen's governing result (Ruling 110(3)) | F · 40 ms |

### 3.4 Cross-cutting

| Check | Assertion | Ring · est. |
|---|---|---|
| `Labels_Dx_EveryRowResolvesOnce` | every COPY-293..333 string resolves through one `Labels` lookup; no DX string literal outside it; COPY-312 flagged provisional | F · 3 ms |
| `Labels_ReasonCodes_AllApprovedTextNoRawCode` | every ANA-* reason code (CPY's eight, ANA-OSWALD-UNDEFINED to ANA-VERDICT-MISSING; two reuse COPY-341 and COPY-266) and every Ruling 116 DX string (rows 10, 11, 36 Re, Ncrit and family variants, 44, 45, 53, 54 with its five reasons) resolves to its approved text through the one `Labels` lookup; an unknown code fails the test instead of being shown | F · 5 ms |
| `Projection_NoRawAnaCodeInAnyCell` | over runs that trigger each of the eight codes, no projected cell value matches `ANA-[A-Z-]+` | F · 5 ms |
| `SectionTab_Desktop_RendersAllChartsWithoutThrowing` | one window build; each of the four charts renders; table twin toggles and keeps focus (existing `LoadingChart_TableTwin_TogglesAndKeepsFocus` pattern) | R · 600 ms |

Count: 39 names (35 fast, 4 readiness).

## 4. Budget and rings

Fast ring: 35 checks, est. 0.8 s total (largest single: `FindAlpha_Dialog_ResultAndBasisRows`, 0.5 s, moved to R if
measured over 500 ms). Readiness: 4 checks, est. 1.7 s. The fast ring is at 48 s of 50 s net; the sum added to it is 0.8 s,
so C-3 stays under 50 s only if the Analysis harness build is not slowed by the new files; measured at the end of step 2.

## 5. Open points for the Coordinator

- Row 10 is approved text with no drawing (section 2, item 1).
- Row 49 and 50 mockup is superseded by Ruling 109; the Section-tab drag cell needs the operator's eyes in the capture (section 2, item 2).
- `WorkbenchController.cs` is needed only for a Find α apply method; GRP edits the same file in other regions.
- `Analysis/ConditionsBand.axaml` is on DX's list; confirm CPY's 41 px band height stays when the button is added.

## Repair cycle 1 (trk-dx2): changes to the plan

Moved fast to readiness (C-2: Analysis part cost over 5 s; the four costliest DX checks by their COST lines): `SectionProjection_CpPlot_SeriesAndMarker`, `Cavitation_GoverningStation_NamedWithDepth`, `Cavitation_Provisional_AboveTenPercent`, `Section_UnderreadNotMeasured_ShowsRatifiedText`. Added: `Polar_NonNaca0012InsideTrainingRange_ComputedAndFlagged_Ruling117` (R), `Section_CpUnavailable_PanelSolveFailed_ShowsCopy358InApp` (R, Desktop), `Labels_Ruling118_ApprovedTextsAndDesignRows` (F). PASS lines for every name: `docs/proof/dx/pass-names.txt`.
