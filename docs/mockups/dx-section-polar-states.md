---
id: mockup-dx-section-polar-states
title: Area 3 section and polar states (A3b, A3c) in the approved shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, area-3, analysis, a3b, a3c, cp, cavitation, polar, find-alpha, hard-states, operator-show]
links:
  - { to: design-dx-screen-states, rel: documents }
  - { to: mockup-area3-analysis, rel: refines }
  - { to: design-area3-analysis, rel: documents }
  - { to: design-language, rel: depends-on }
review-by: 2026-12-31
summary: >-
  Six screens of the approved 1280 x 800 shell and a state sheet for the A3b and A3c states the approved Area 3 mockup
  does not draw: the Section view with Cp on the profile, the Section tab (Cp, Polar, Transition, Bucket), the cavitation screen
  with its governing station and margin states, the polar tier chip and flags, the drag sources, Total drag, and Find alpha.
  Nine decision requests for the operator.
---

# Area 3 section and polar states

Open [`dx-section-polar-states.html`](dx-section-polar-states.html) over `file://`. Screens D1 to D6 are full shells; D7 is
a state sheet of cells in the shell's own row components. The buttons top right switch the chrome to dark and the shell to
1024 × 700. The strip under the title is the in-artifact audit (contrast, target size, NaN scan, text size, clipping, and
reference checks). The state inventory and copy table are in
[`docs/design/dx-screen-states.md`](../design/dx-screen-states.md).

## What is computed and what is a stand-in

| Item | Source |
|---|---|
| Wing loads, strip α_eff, Find α root | Computed: the approved page's reference vortex lattice (64 × 4), a secant on it |
| Section Cp, Cl, Cm, t/c | Computed: a Karman-Trefftz analytic section (the A3b oracle family) at the strip's α_eff; a stand-in for the product panel tier. Checks: t/c 0.120, Cl against the thin-airfoil value, symmetric at 0° |
| Cavitation state, σ, V_crit, governing station | Computed from the Cp above, with Cavitation.cs's rules and Ruling 86 |
| Polar cl, cd, x_tr | Stand-in: SPIKE-ANA-1's XFOIL 6.99 NACA 0012 oracle at Re 2 × 10⁵ and 10⁶, Ncrit 2 and 4, interpolated in ln Re. Not NeuralFoil output |
| analysis_confidence, panel under-read, low-confidence value | Illustrative; marked on the sheet |
| CST residual | The spike's measured NACA 0012 value |

## Direction

Same brief as the approved page: quiet, direct, honest; no colour that says "good"; every number with its basis. Archetype
unchanged (G1 workbench with G2 charts). New elements are limited to: a Section view beside Plan, a chart selector inside the
approved Section tab, a Find α button beside Evaluate, and Properties groups. Tokens are the approved page's; the Cp colour
map is vik pinned at 0 (ANA-21), approximated in five stops as batlow is.

## Measurements (Stage 3)

- In-artifact audit, light and dark, 1280 × 800 and 1024 × 700: contrast ≥ 4.5:1 (lowest 5.08 light, 6.51 dark), targets
  ≥ 24 px, text ≥ 11 px, no NaN or placeholder, nothing clipped, reference checks green; no page errors.
- Authoring caught three defects: the conditions band clipped once Find α was added (inputs and gaps narrowed); chart tick
  labels collided with the axis title; and the cd-against-cl chart clipped the Ncrit 2 curve at the first y range.
- `ui-craft-gate.py`: no findings (a floor, never a verdict).

## Decision requests

1. **DR-DXM-1 copy batch.** Approve, edit or reject every NEW string (design table). Recommended: approve as proposed.
2. **DR-DXM-2 envelope meaning (Ruling 88 D14 b).** The attached-flow verdict stays as approved; "Polar bracket" is its own
   row. Open: outside the bracket, show a flagged value (as built) or none. Recommended: flagged inside the training range,
   Unavailable outside it or past the CST limit.
3. **DR-DXM-3 tier chip.** (a) Keep "Polar · local calculation" and carry the surrogate in two label lines (COPY-66 and
   "surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only"); (b) rename the chip. Recommended (a).
4. **DR-DXM-4 chart place.** Selector inside the Section tab and a Section view, or one tab per chart. Recommended: selector.
5. **DR-DXM-5 Total drag.** (a) stays Unavailable naming junction, mast, wave, spray, with "Wing drag" as the claim; (b) shows
   Wing drag as "Wing only". Recommended (a), which is A5.6.
6. **DR-DXM-6 Find α entry.** Button beside Evaluate, dialog, Apply writes α, Evaluate stays explicit. Recommended.
7. **DR-DXM-7 provisional state.** One row for an under-read above 10 % (Ruling 90 allows no new copy; Ruling 92 retired the
   only provisional string). Recommended: approve it.
8. **DR-DXM-8 polar Re range.** Validated bracket for the flag, training range for Unavailable. Recommended, as built.
9. **DR-DXM-9 Section tab station.** The selected strip; none selected, the governing cavitation station, named. Recommended.
