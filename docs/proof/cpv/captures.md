---
id: proof-cpv-captures
title: CPV captures of the Cp-coloured Section profile against the approved mockups
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [cpv, captures, section]
links:
  - { to: mockup-section-main-area, rel: refines }
  - { to: proof-cpv-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Light and dark captures of the Section document with a strip selected, opened and compared with mockup state B (and DX state row 5),
  with the differences and the mockup strings that have no approved COPY row named.
---

# CPV captures

Rendered from the app with a scratch harness (`RenderTargetBitmap` of the real `ShellHost`, 1500 x 870, the SMA method), Example foil, tip
strip selected, Section document open. The harness check was removed after the capture; it is not in the tree.

| File | Theme | Compared with |
|---|---|---|
| `01-section-selected-light.png` | light | `docs/mockups/section-main-area/02-analysis-section-selected.png` (state B) and DX state row 5 |
| `02-section-selected-dark.png` | dark | the same; the profile sits on the viewport colour in both themes, as the mockup's does |

## Compared, side by side

| Mockup shows | Built shows |
|---|---|
| Closed outline, each panel coloured by its Cp on a diverging ramp, 0 at the centre | Same: 5 px segments, vik ramp (5 stops from the DESIGN.md `vik-*` tokens), colour from the mean Cp of the two ends of each segment; suction side reads blue, the stagnation point red |
| Ring on the Cp_min panel, plate "Cp_min -0.47 - x/c 0.120 - upper" | Same: ring, plate "Cp_min -0.46 - x/c 0.091 - upper" (the built example has its own numbers). The plate sits below the marker for a lower-side minimum |
| Plate "Section - eta 1" top left; estimator plate "Estimator - local calculation - inviscid; no boundary layer" top right | Same text. On a narrow view the estimator plate drops to a second row |
| Legend "Cp - vik pinned at 0 - -0.99 to 0.99" over a ramp bar with ends and 0 | Legend title names the data's own range ("-0.46 to +0.99", COPY-294 form "-a to +b"); the bar's end labels are the ramp's extent, +/- max(|min|, max), 0 at the centre |
| "sigma 7.71 - -Cp_min 0.47 - clear of the 15 % margin - V_crit 40.4 kn" bottom left | Same line, built from the station's cavitation result ("sigma 7.80 - -Cp_min 0.46 - clear of the 15 % margin - V_crit 21.2 m/s"). Not drawn when the cavitation state is Undefined or Unavailable |
| "x/c 0 -> 1" plate under the profile | Same |
| Profile height about 230 px | 240 px (was 200) |

## Differences, named

1. **V_crit unit.** The mockup shows knots; the built Section document's cavitation table already shows V_crit in m/s, so the plate
   follows the table. Operator: say if the plate should follow the units switch (COPY-401 to 403) instead.
2. **Legend range versus bar ends.** The brief asks for the legend's min and max to equal the data's. The title does. The bar ends read
   the ramp's symmetric extent, because 0 must sit at the centre; for this foil that is -0.99 to 0.99 while the data is -0.46 to 0.99.
3. **The comb is gone.** The old profile was a z/c line chart with a Cp comb; `ChartModel` id "profile" no longer exists.

## Mockup strings with no approved COPY row (listed, not invented; DESIGN.md not changed)

Checked COPY-293 to COPY-405 and the DX rows: COPY-293, COPY-294 and the estimator chip (COPY-214) are approved and reused. These
mockup strings have no row and are drawn as the mockup draws them; the operator should approve or amend them:

- `Section · η <η>` (profile caption plate)
- `Cp_min <v> · x/c <x> · <side>` (marker plate)
- `σ <σ> · −Cp_min <v> · clear of | inside | at or past the 15 % margin · V_crit <v> <unit>` (cavitation line)
- `x/c 0 → 1` (axis plate)
- the inline ` · inviscid; no boundary layer` after the chip (its tail is the COPY-293 wording, the join is not a row)

Not checked: VoiceOver or contrast proof (deferred by the operator). The view has an automation name that names the Cp_min value and place.
