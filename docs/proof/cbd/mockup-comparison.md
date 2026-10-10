---
id: proof-cbd-mockup-comparison
title: "Rail comb build (track CBD): the built Plan view against the approved mockup, and the unproven accessibility traces"
type: doc
status: in-review
owner: "@timianmalloo"
phase: implementation
tags: [proof, rail-comb, screenshot, accessibility]
links:
  - { to: design-rail-comb, rel: documents }
  - { to: mockup-rail-comb, rel: relates-to }
review-by: 2027-04-01
summary: >-
  The built Plan view with the comb (the Desktop harness's capture) beside the mockup's main state, what matches and what
  differs, and C8 and C9 recorded as unproven traces, not passes.
---

# Built Plan view against the mockup's main state

- Built: [`plan-comb-built.png`](plan-comb-built.png), from `CFD_PROOF_PNG=<path> CFD_TEST_ONLY=PlanComb_Screenshot` in the
  `--plan-canvas` harness (`PlanComb_Screenshot_WhenAsked`; window 1500 × 800, light theme; the trailing edge's point 5 selected and
  focused, the pointer parked below the leading edge's point 5).
- Mockup: [`mockup-main-state.png`](mockup-main-state.png), `docs/mockups/rail-comb.html` in Chrome at 1400 × 1000 (top of the page,
  section 1, example planform, nothing selected, probe at η 0.62).

| Mockup's main state | Built | Same? |
|---|---|---|
| Comb on both rails, teeth away from the centre of curvature, 1 px, one shared gain | both rails, teeth outward on the convex edges | yes |
| Selected rail in the ink tone, the other muted | built as ink at 0.9 against muted at 0.75; the difference is slight at this size and I did not measure it in the capture | built, not measured |
| Envelope per piece, a step at a jump | drawn per piece; no jump in this planform | yes (steps are tested in `PlanComb_EnvelopePerPiece_StepAtJump`) |
| Ticks LE1, LE2 … and TE1 … in the diagnostic colour, off the focus order | LE1, LE2, TE1, TE2 in the station colour | yes |
| Rings at the evaluated station on each rail | a ring on each rail at the pointer's station (with nothing hovered, a ring at the selected point's station) | yes |
| Plate, top right: Comb, Scale (Smaller, Larger, Auto), legend, note, Density, pieces lines, threshold | the same rows, in the same order and words | yes |
| Plate position fixed at the top-right corner | placed by exclusion (C11): here the bottom-right corner, because the top-right corner holds the leading edge's teeth | **differs, by design** (the mockup's fixed corner covers up to 3 points on other planforms) |
| Tracing strip beneath the viewport, one line | beneath the viewport, wrapping to two lines at this width; 52 px above the window bottom so the floating navbar never covers it | yes in kind; the strip wraps because the pointer reading names both rails |
| Example planform: 2 and 2 pieces | the test planform reads 3 and 3 pieces (the fixture is a different planform: the example foil's rails are straight) | not comparable |
| Axis, root label, "Span from root" captions | the app's own scale bar and station chips | differs (the mockup's chrome is not product chrome) |

# Unproven traces (Ruling: accessibility proof is deferred)

These are recorded as **not run**, not as passes.

| Condition | What it asks | State |
|---|---|---|
| C8 | The viewport overlays keep at least 3:1 under the system high-contrast theme, or the build records that the canvas keeps its own palette; one trace on Windows and one on macOS | **Unproven.** Not run. The canvas draws its comb, ticks and rings with the Plan brushes (`PlanFoilBrush`, `PlanMuteBrush`, `ViewportInkBrush`, `PlanSelectionBrush`); the high-contrast resource dictionary (`Styles.axaml`) maps the Plan brushes, but no trace of the comb under it exists. |
| C9 | A screen-reader trace (VoiceOver and Narrator or NVDA) of the plate, the strip and the pieces line | **Unproven.** Not run. The automation properties are set (names, help text, one polite live region on the plate, one persistent polite or quiet strip) and checked by headless tests; no reader has been run. |
