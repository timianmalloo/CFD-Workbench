---
id: mockup-section-force-vectors
title: Section force vectors (Ruling 127) - Lift and Drag on the Cp-coloured profile
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, ruling-127, section, analysis, lift, drag, centre-of-pressure, operator-show]
links:
  - { to: mockup-section-main-area, rel: refines }
  - { to: mockup-dx-section-polar-states, rel: refines }
  - { to: design-language, rel: depends-on }
review-by: 2026-12-31
summary: >-
  Four states of the Section view's profile after Ruling 127: Lift and Drag vectors anchored at the centre of pressure, a faint
  inflow, lift perpendicular and drag parallel to it, drag in two segments (profile, induced) at its own labelled scale. A normal
  strip, near zero lift (anchor falls back to the quarter chord), a flagged drag, and Imperial. Thirteen strings are proposed copy.
---

# Section force vectors (Ruling 127)

Open [`section-force-vectors.html`](section-force-vectors.html) over `file://`. Tabs A to D switch the state; the button top right
switches light and dark; the checkbox shows or hides the dashed "proposed copy" marks. Captures: [`section-force-vectors/`](section-force-vectors/)
(light; `-dark` beside each). Nothing is built from this page.

Base: the built Cp profile (`docs/proof/cpv/01-section-selected-light.png`) and state B of [`section-main-area.html`](section-main-area.html).
Tokens, plate look, the vik ramp and the selected-strip header (COPY-394) are reused. The profile is a stand-in (NACA 0012 thickness, hand-shaped Cp
with Cp_min -0.46 at x/c 0.091); the numbers are the capture's example station (eta 1).

## The four states

| State | Capture | What it shows |
|---|---|---|
| A. A normal strip | `01-normal-strip.png` | CP at x/c 0.267. Lift 134.44 N/m up from CP, scale "1 c = 1000 N/m". Drag along the inflow from CP: profile 13.06 N/m then induced 2.44 N/m (illustrative), drawn x10. |
| B. Near zero lift | `02-near-zero-lift.png` | cl 0.0015 gives x_cp/c 1.58, off the section. Arrows start at c/4; a marker at the trailing edge reads "CP x/c 1.58 - off the section"; a note says so. Lift is a hair because it is drawn to scale. |
| C. Flagged drag | `03-flagged-drag.png` | As A. The profile drag label ends "low confidence"; COPY-316 and COPY-364 sit verbatim under the table. The induced part is not flagged (it is lattice, not polar). |
| D. Imperial | `04-imperial.png` | Values in lbf/ft; lift scale "1 c = 70 lbf/ft"; V_crit 41.2 kn (Ruling 126). Same geometry as A. |

## Reading the picture

- Anchor: a filled point at x_cp on the chord line. In B it is a hollow ring at c/4.
- Inflow: faint dashed arrow ahead of the leading edge, tilted by alpha_eff.
- Lift: white, perpendicular to the inflow. Drag: amber, parallel to it; profile segment solid, induced segment lighter and dashed, a tick between them. Colour is never the only cue: the two drag segments differ in dash and in label.
- Scales: the lift scale is on the lift label. Drag is "x10" relative to the lift scale, on each drag label. Both are plain per-chord statements, so a reader can measure.
- A table under the plate repeats every number as text (the arrows alone are not an accessible reading).

## Physics, stated plainly

Per unit span, strip at eta, chord c = 0.12 m, q = 0.5 rho V^2 = 13553 Pa (capture).

| Quantity | Formula | Capture value | Where the input comes from (code read; Inferred, not run) |
|---|---|---|---|
| x_cp/c | 0.25 - Cm_c/4 / cl | 0.25 + 0.002 / 0.115 = 0.267 | cl and Cm c/4 are the section estimator's, rows "cl (panel)" and "Cm c/4" (`SectionDisplay.cs:79`, `AnalysisProjection.cs:203`, `station.Estimate.CmQuarter`). |
| alpha_eff | alpha_op + twist - alpha_i | 0.96 deg | `StripCoupler.cs:46`. |
| alpha_i | `strip.InducedAngleDeg` (from the lattice); equals alpha_geo - alpha_eff | 2.00 - 0.96 = 1.04 deg | `StripCoupler.cs:45`. alpha_geo = alpha_op + strip twist. The 2.00 deg is the operating alpha in the capture; whether tip twist is zero there is not verified. |
| Lift L' | strip force / strip width (the "Lift / span" row) | 134.44 N/m | `AnalysisProjection.cs:338`, `localLift / width`; lattice force `Fz` of the strip. |
| Profile drag D'_p | cd q c | 0.00803 x 13553 x 0.12 = 13.06 N/m | `StripLoad.Cd2` and `Cd4` (`StripCoupler.cs:52-55`) are the polar band at alpha_eff for Ncrit 2 and 4; the capture shows one value, cd (profile) 0.00803. |
| Induced drag D'_i | L' sin(alpha_i) | 134.44 x sin(1.04 deg) = 2.44 N/m | Illustrative: formed from L' and alpha_i. The lattice strip force has its own induced part (`Fx`); this page does not read it. |
| Direction | inflow unit vector (cos a, -sin a), lift (sin a, cos a), a = alpha_eff | a = 0.96 deg: the tilt is barely visible, as it is in life | Drawn with no exaggeration. |
| Imperial | 1 N/m = 0.068522 lbf/ft | L' 9.21, D'_p 0.89, D'_i 0.17 lbf/ft | Units switch (Ruling 115). |

## Proposed copy (not in DESIGN.md COPY-293..405 or the Ruling 126 rows)

Checked first: COPY-293..405 and Ruling 126. Reused as they stand: COPY-394 (header), COPY-295 "cl (panel)", COPY-296 "Cm c/4", the "alpha_eff" row label, the Section plate labels
of Ruling 126, COPY-328 (profile drag note, under the table), COPY-316 and COPY-364 (state C, verbatim), kn for V_crit under Imperial. COPY-379 is about total lift, not this, so it is not reused.

1. `L' <v> <unit>` with a second line `1 c = <v> <unit>` - lift label and scale.
2. `D' profile <v> <unit> · x10` - profile drag label.
3. `D' induced <v> <unit> · x10 · illustrative` - induced drag label.
4. `CP · x/c <x>` - anchor label.
5. `inflow alpha_eff <a>°` - inflow label.
6. `c/4 · arrows start here` - anchor label, state B.
7. `CP x/c <x> · off the section` - marker at the trailing edge, state B.
8. `Centre of pressure is outside the section at this lift. The arrows start at the quarter chord.` - note, state B.
9. `· low confidence` - suffix on the profile drag label (the first words of COPY-316); `· flagged` - suffix on the table value.
10. `Force on this strip, per span` - table heading.
11. Table row labels: `Lift L'`, `Profile drag D' profile`, `Induced drag D' induced`, `Drag D'`, `Centre of pressure x_cp/c`, `alpha_i`.
12. `Induced drag here is L' · sin(alpha_i), with alpha_i = alpha_geo - alpha_eff. Illustrative.` - table note.
13. Key: `Inflow`, `Lift`, `Drag, profile`, `Drag, induced`.

## Open questions for the hydrodynamicist

1. **Which lift, and which cl?** The capture's L' (134.44 N/m) is the lattice strip lift, cl_local 0.083. x_cp uses the panel cl (0.115) and Cm c/4. They differ (q c x 0.115 = 187 N/m; `SectionTier.cs:110` computes that as `LiftPerSpan`). The arrow and the anchor then come from two models. Use the polar's cl and Cm at alpha_eff for both, or keep the mix and label it?
2. **Induced drag direction.** L' sin(alpha_i) is the streamwise (free-stream) component. Drawing it parallel to the local inflow at alpha_eff is an approximation. At 1 deg the error is below 0.02 %, but is it the right frame to teach?
3. **Illustrative vs read.** The lattice strip force already has a streamwise part (`Fx`). Should the build read it instead of L' sin(alpha_i)? The mockup labels the formula "illustrative" until you rule.
4. **alpha_geo.** The brief takes alpha_geo = 2.00 deg. The code's alpha_geo is alpha_op + strip twist. Confirm the twist at eta 1 in the capture is zero, and that "alpha_geo - alpha_eff" is the induced angle you want shown.
5. **Profile band.** The polar gives a Ncrit 2 and 4 band; the capture row shows one cd. Draw the band (a thin cap on the profile segment) or the Ncrit 2 value?
6. **Pitching moment sign and Cm source.** The estimator Cm c/4 is inviscid. Is x_cp from an inviscid Cm and a viscous cd mixture honest on one strip? Near zero lift the quotient is unstable: where should the switch to c/4 sit (the mockup uses "off the section", x_cp < 0 or > 1)?
7. **Foil sections with large camber** carry real Cm and an x_cp far from c/4 at low cl; the symmetric NACA 0012 here is the easy case.
8. **Lift scale.** 1 c = 1000 N/m is a fixed scale in the mockup. Auto-scale to the largest strip, or fixed per run so strips compare?

## Choices to confirm (stand-ins)

- The profile and its Cp are stand-ins, not the solver's; the rest of the Section document (chart, stations table, groups) is as approved in `section-main-area.html` and is not redrawn here.
- The lift arrow in B is true to scale and almost invisible by design; a minimum stub length would be a build choice.
- Under Imperial the lift scale is 70 lbf/ft per chord (about 1022 N/m), so arrows keep their size; a metric-equal scale would be 68.5 lbf/ft.
- Arrow labels are plates on the profile, like the Cp_min label; the drag labels sit below the foil with leaders so they do not cover the Cp ring.

## Measurements

- `ui-craft-gate.py docs/mockups/section-force-vectors.html --markdown`: no findings (a floor, never a verdict).
- Browser load in system Chrome: no page errors in the eight captures (four states, light and dark).
