---
id: mockup-section-force-vectors
title: Section force vectors (Rulings 127, 128) - Lift and Drag on the Cp-coloured profile, free-stream axes
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, ruling-127, ruling-128, section, analysis, lift, drag, centre-of-pressure, operator-show]
links:
  - { to: mockup-section-main-area, rel: refines }
  - { to: mockup-dx-section-polar-states, rel: refines }
  - { to: design-language, rel: depends-on }
review-by: 2026-12-31
summary: >-
  Five states of the Section view's profile after Ruling 128: Lift and Drag on free-stream axes (V-inf at alpha_geo), one model (the lattice),
  the centre of pressure as anchor only when it is on the section and |cl| >= 0.05, otherwise c/4 with the pitching-moment couple and x_cp
  Undefined. A mid-span strip, near zero lift, a flagged drag, Imperial, and a cambered section at cruise. Every number is illustrative.
---

# Section force vectors (Rulings 127, 128)

Open [`section-force-vectors.html`](section-force-vectors.html) over `file://`. Tabs A to E switch the state; the button top right
switches light and dark; the checkbox shows or hides the dashed "proposed copy" marks. Captures: [`section-force-vectors/`](section-force-vectors/)
(light; `-dark` beside each). Nothing is built from this page.

Base: the built Cp profile (`docs/proof/cpv/01-section-selected-light.png`) and state B of [`section-main-area.html`](section-main-area.html).
Tokens, plate look, the vik ramp and the selected-strip header (COPY-394) are reused. The profile and its Cp are stand-ins.
**Every number on the page is illustrative: none is read from the app.** The main example is a mid-span strip (eta 0.545, chord 0.16 m, q 13553 Pa). The tip strip is not shown; when shown it keeps "Not judged — tip strip".

## The five states

| State | Capture | What it shows |
|---|---|---|
| A. Mid-span strip | `01-mid-span-strip.png` | V-inf at alpha_geo 3.00 deg (solid), local inflow at alpha_eff 1.97 deg (faint, "tilts the flow by alpha_i"). Lift 479 N/m (lattice) at CP (lattice) x/c 0.27, perpendicular to V-inf. Drag along V-inf: profile 17.1-18.0 N/m (the Ncrit 2-4 band, a cap on its segment), then induced 8.40 N/m (lattice), at x10. Scale "1 c = 2000 N/m, fixed per run". |
| B. Near zero lift | `02-near-zero-lift.png` | Cl_local 0.012. The quotient would give x_cp 0.42, on the chord, but \|cl\| < 0.05, so x_cp is Undefined: arrows at c/4, the pitching-moment couple drawn, note shown. Drag at x1 so it stays on the plate. |
| C. Flagged drag | `03-flagged-drag.png` | As A. The profile label ends "low confidence"; COPY-316 and COPY-364 sit verbatim under the table. The induced part is the lattice's and not flagged. |
| D. Imperial | `04-imperial.png` | lbf/ft and lbf.ft/ft; scale "1 c = 200 lbf/ft"; V_crit 33.4 kn (Ruling 126). The scale is re-derived in the unit shown, so arrows differ from A. |
| E. Cambered, cruise | `05-cambered-cruise.png` | Cm c/4 -0.08, cl 0.10: x_cp would be 1.05, off the section, so x_cp is Undefined; arrows at c/4 with the couple (M' -27.8 N.m/m). The common case for a cambered section. |

**Correction to the brief for E.** With Cm -0.08 and cl 0.15, x_cp = 0.25 + 0.08/0.15 = 0.78, still on the chord. CP leaves the section only below cl 0.107. E therefore uses cl 0.10. If the build fixture wants cl 0.15, use Cm -0.12 or lower (x_cp = 1.05).

## Reading the picture

- Anchor: a filled point at the lattice x_cp on the chord line; a hollow ring at c/4 when x_cp is Undefined, with the couple drawn as an arc around it (nose-down for a negative M').
- V-inf: solid, at alpha_geo, left of the leading edge. Local inflow: faint dashed, at alpha_eff, its label giving the reason for the tilt. They differ by alpha_i, about 1 degree in A, so the gap is small, drawn without exaggeration.
- Lift: white, perpendicular to V-inf. Drag: amber, parallel to V-inf; profile segment solid with a cap box for the band, induced segment lighter and dashed, a tick between.
- Scales: lift is fixed per run: the largest strip's \|L'\| over 0.3 c, rounded up in the 1-2-5 series, shown on the lift label. Drag uses a stated multiple (x10, x5 or x1, on every drag label).
- A table repeats every value as text, each row naming its model, ending with the "Not modelled" row.

## Physics, stated plainly

Per unit span, strip at eta, chord c, q = 0.5 rho V^2.

| Quantity | Formula | Source in the code (read, not run; Inferred) |
|---|---|---|
| x_cp/c (lattice) | 0.25 - Cm_c/4 / cl, from the strip My and Fz about c/4 | Strip forces `Fx Fy Fz Mx My Mz` (`StripCoupler.cs:55`). Ruling 128: the build takes x_cp from the strip My/Fz. |
| alpha_geo | alpha_op + strip twist | `StripCoupler.cs:46`. |
| alpha_i (lattice) | `strip.InducedAngleDeg`; equals alpha_geo - alpha_eff | `StripCoupler.cs:45`. |
| alpha_eff (lattice) | alpha_op + twist - alpha_i | `StripCoupler.cs:46`. |
| L' (lattice) | strip lift / strip width | `AnalysisProjection.cs:338`. |
| M' c/4 (lattice) | strip moment about c/4, per span | The same strip force record. |
| D' profile | cd q c over the Ncrit 2-4 band | `StripLoad.Cd2`, `Cd4` (`StripCoupler.cs:52-55`), polar at alpha_eff. |
| D' induced (lattice) | d_i = 0.5 rho Gamma (-w_T); sums to the wing D_i | Ruling 128. The page value 8.40 N/m is illustrative, near L' sin(alpha_i) = 8.6. |
| cl (panel, 2D inviscid at alpha_eff) | panel method at alpha_eff | `SectionTier.cs:97` estimator; shown beside the arrow, not used for it. |
| Axes | V-inf at alpha_geo: lift (sin a, -cos a), drag (cos a, sin a) on screen, a = alpha_geo | Drawn with no exaggeration. |
| Imperial | 1 N/m = 0.068522 lbf/ft; 1 N.m/m = 0.737562 lbf.ft/ft | Units switch (Ruling 115). |

## Answers to the earlier open questions (the reviewer's, now ruled by Ruling 128)

1. Which lift and cl? One model, the lattice strip. L' is the lattice strip lift, x_cp comes from its My/Fz, and the panel cl stays beside it as "cl (panel, 2D inviscid at alpha_eff)".
2. Induced drag direction? Free-stream axes: V-inf at alpha_geo, lift perpendicular, drag parallel. The local inflow stays as a faint line.
3. Illustrative or read? Read: the strip's lifting-line share from the lattice. "Illustrative" is dropped from the label (the mockup's numbers are still not from the app).
4. alpha_geo? Drawn as V-inf; alpha_i (lattice) = alpha_geo - alpha_eff.
5. Profile band? A cap on the profile segment (Ncrit 2-4).
6. Inviscid Cm with viscous cd; where the switch to c/4 sits? One model; the anchor rule: CP when on the section and \|cl\| >= 0.05, else c/4 with the couple and x_cp "Undefined", never a number off the chord.
7. Cambered sections? State E shows the c/4 + couple case as common.
8. Lift scale? Fixed per run from the largest strip \|L'\|, 1-2-5 rounded, shown; drag at a stated multiple.

## Proposed copy (not in DESIGN.md COPY-293..405 or the Ruling 126 rows)

Reused as they stand: COPY-394 (header), COPY-328 (profile drag note), COPY-316 and COPY-364 (state C, verbatim), the Section plate labels of Ruling 126, kn for V_crit under Imperial, the "Not modelled" row text from the built Properties capture.

1. `L' <v> <unit> (lattice)` with a second line `1 c = <v> <unit>, fixed per run` - lift label and scale.
2. `V-inf at alpha_geo <a>°` - free-stream label.
3. `local inflow alpha_eff <a>°` with a second line `tilts the flow by alpha_i` - faint-line label.
4. `CP (lattice) · x/c <x>` - anchor label, CP on the section.
5. `c/4 · arrows start here · x_cp Undefined` - anchor label otherwise.
6. `M' c/4 (lattice) <v> <unit>` - couple label.
7. `D' profile (polar, Ncrit 2-4) <min>-<max> <unit> · x<k>` - profile label; the cap is the band.
8. `D' induced, lifting-line share (lattice) <v> <unit> · x<k>` - induced label.
9. `D' profile + induced, free-stream axes <v> <unit> · x<k>` - total label.
10. `· low confidence` (first words of COPY-316) and `· flagged` (table suffix), state C.
11. `Wing strip, per span; not the wing total` - table heading.
12. Table row labels: `cl (panel, 2D inviscid at alpha_eff)`, `Cm c/4 (panel)`, `Cl_local (lattice)`, `alpha_geo`, `alpha_eff (lattice)`, `alpha_i (lattice)`, `x_cp/c (lattice)`, `L' (lattice)`, `M' c/4 (lattice)`, `D' profile (polar, Ncrit 2-4)`, `D' induced (lattice)`, `D' profile + induced, free-stream axes`.
13. `Undefined · near zero lift: |cl| is below 0.05` - x_cp/c value, state B.
14. `Undefined · the centre of pressure is off the section` - x_cp/c value, state E.
15. `The centre of pressure is undefined here, so the arrows start at the quarter chord and the pitching-moment couple is drawn.` - note.
16. Key: `V-inf`, `Local inflow`, `Lift`, `Drag, profile (cap: Ncrit 2-4 band)`, `Drag, induced`, `Pitching-moment couple`.

## Open points still for the operator or the build

- Which cl the \|cl\| >= 0.05 test reads: this page uses the lattice Cl_local. Confirm.
- Every value has three significant figures ("479 N/m", "0.230 N/m"); trailing zeros are kept so the precision reads.
- The drag multiple (x10, x5, x1) is chosen per run so the arrow fits; a rule for it (for example, the largest strip's total drag at most 0.12 c) is a build choice.
- The profile and Cp are stand-ins; the rest of the Section document is as approved in `section-main-area.html`, not redrawn.

## Measurements

- `ui-craft-gate.py docs/mockups/section-force-vectors.html --markdown`: no findings (a floor, never a verdict).
- Browser load in system Chrome: no page errors in the ten captures (five states, light and dark).
