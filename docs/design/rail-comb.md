---
id: design-rail-comb
title: "Rail comb: scale and density, a radius readout and the monotone-piece count on the planform rails"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — operator sees the mockup before any build (memory rule); track CMB, Ruling 193 (B), Ruling 107 (DR-GM-9 C)
tags: [desktop, core, cad, planform, comb, curvature, radius, monotone, a4-9, oi-5, proposal, operator-show]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: depends-on }
  - { to: design-m12b-points, rel: refines }
  - { to: design-m12c-section-editor, rel: relates-to }
  - { to: design-next-cad-increment, rel: refines }
  - { to: mockup-rail-comb, rel: relates-to }
  - { to: design-language, rel: depends-on }
review-by: 2027-04-01
summary: >-
  Design for OI-5 on the planform rails. The Plan comb keeps its toggle (Curvature, C) and gains a small plate in the
  viewport with a scale stepper (Auto, or a fixed 1-2-5 gain stated as "30 px = N per metre") and a density stepper
  (16, 32, 64, 128 teeth per rail). The Tracing strip beneath the viewport gains the radius of each rail at the pointer's
  station (R in the project length unit, straight rails read "straight"). The plate also shows the monotone-piece count
  of curvature per rail, with a numbered tick on the rail at each curvature extremum. No new single keys, no Properties
  rows, no file change. One new Core function pair (curvature at a station, monotone pieces of curvature) and one spec
  clarification. Adversary pass (marine-cad-ux-expert) is recorded in docs/proof/cmb/adversary.md.
---

# Rail comb: scale, density, radius, monotone pieces

- **Track / tier:** CMB, T1, docs and mockup only. No `src/` or `tests/` change. Code read at `fd5b185c`.
- **Mockup:** [`docs/mockups/rail-comb.html`](../mockups/rail-comb.html). The operator gives a visual yes before any build.
- **Rulings:** 193 (B): build the rail comb readouts "matching the section editor"; 107 (DR-GM-9 C): a real
  marine-cad-ux-expert Adversary pass before the build (done: [`adversary.md`](../proof/cmb/adversary.md)).
- **Spec:** A4.9 "Diagnostics and readouts", CAD-08 (the Tracing strip), CAD-21, GEO-05, OI-5 in
  [`m12b-points.md`](m12b-points.md) (:120, :1047).

## 1. What exists today (read, not recalled)

| Fact | Evidence |
|---|---|
| The Plan comb is toggled by Curvature (toolbar) and `C` with the canvas focused. | `PlanCanvas.cs:309-312`, `:662` (Verified, read) |
| It draws only for the rail of the first selected point (or the rebuild preview), never for both rails, never with nothing selected. | `PlanCanvas.cs:837-846`, `:817-822` (Verified, read) |
| Tooth length is a fixed gain: `clamp(|κ| × 100, 6, 24)` px. Curvature above 0.24 per metre (R under 4.2 m) all reads 24 px, and a straight rail still draws 6 px teeth, so a fair and an unfair rail look alike. | `PlanCanvas.cs:844`, `:821` (Verified, read) |
| Teeth are 8 per knot span spaced by arc length, plus one-sided teeth either side of each anchor and the break tooth at a doubled knot. κ is signed, from analytic derivatives, in per metre (span and aft are in metres). | `PointModel.cs:40-74`, `:182-225` (Verified, read) |
| The probe strip writes η, from root, % half-span, LE, TE and chord at the pointer. No curvature or radius. | `PlanCanvas.cs:242-244` (Verified, read) |
| The section editor's comb: auto scale only, p90 tooth = 30 px, teeth over 60 px clipped and marked ×, a plate "Comb · auto scale · N teeth clipped (×)", recomputed on every paint. There is no stepper. | `SectionCanvas.cs:394-426`, `:99-105` (Verified, read) |
| The only "monotone piece" count in Core is `SignChanges` of the **ordinate slope** (first derivative), used by Fair. It is not a count of pieces of the curvature plot, and it returns changes, not pieces. | `ConstrainedFit.cs:515-535` (Verified, read) |

Two consequences. (1) "Matching the section editor" cannot mean copying a stepper, because the section editor has none:
A4.9 says "its scale is a stepper" and the section build stopped at auto. This design adds the stepper to the rails and
names the section editor as a follow-up (§8, OQ-2). (2) The spec's monotone count is on **curvature** (Farin-Sapidis);
the built count is on **slope**. The rail count is new Core code; it must not reuse `SignChanges` by name (§6).

## 2. Direction

- **Who, when:** the one foil designer, fairing the outline after a drag, asking "is the curvature of this edge smooth
  and where does it kink".
- **Job:** read the shape of curvature along each rail, change the reading scale without changing the foil, and get one
  number (radius) at a place, one count (pieces) per rail.
- **Archetype:** unchanged (G1 Parametric Modeling Workbench); view diagnostics inside an existing viewport.
- **Adjectives:** quiet, quantitative, honest (not decorative, not a score, not a verdict).
- **References taken:** the section editor's plate and clipping (m12c §11.2); Rhino and Alias curvature graph, Fusion
  curvature comb: a scale and a density control beside the toggle (recalled from the area 01 file, Inferred for exact
  control names). Nothing is taken for its look.
- **Anti-goals:** no fairness grade, no pass/fail colour on the count, no auto-fair, no new document state.

## 3. What the user controls

Scale and density. Both are **view settings**: they never touch the foil, never enter undo, never change the file.
They persist for the session and across files in the user's view preferences (the same home as the Curvature toggle).

| Control | Values | Default | Notes |
|---|---|---|---|
| Curvature (toggle) | on / off | off (as built) | toolbar and `C`, unchanged |
| Scale | **Auto**, or a fixed gain "30 px = κ" on the ladder 0.5, 1, 2, 5, 10, 20, 50, 100, 200 per metre (per inch in an inch project, 1-2-5 in that unit, not the metre ladder converted) | Auto | Auto: the 90th-percentile tooth over **both rails** (one shared gain, so the rails compare) is 30 px; refit when the comb is switched on, when Auto is chosen, and once at the end of an edit gesture. **Held fixed during a gesture** (§3.1). A fixed step never changes by itself. The steppers disable at the ends with the reason. |
| Density | 16, 32, 64, 128 teeth per rail, spaced evenly by arc length over the whole rail | 32 (about the built 8 per span on a four-span rail) | One-sided teeth at anchors and the break tooth at doubled knots are always drawn. Teeth closer than 3 px are thinned. Even sampling can step over a very narrow spike; the piece ticks (§5) mark curvature extrema, and a clipped × marks the tallest. |

There is **no typed channel** for scale or density: they are view settings with steppers and palette verbs, not
dimensions (the precision-entry rule applies to geometry).

**Where the controls live: a plate in the Plan viewport, top right, shown only while Curvature is on.** Reasons:
the toolbar is at its control ceiling (CAD-21 lists the verbs it carries); Properties describes the *selection*, and a
view setting there would sit among model values (it would also push the one-point-selected control count past 25);
the section editor already puts its comb plate on the canvas (`.lb`, `SectionCanvas.cs:99`). Each stepper is a pair
of 24 px buttons with a text label ("Smaller teeth", "Larger teeth", "Auto"; "Sparser", "Denser"), reachable by Tab
in the viewport's tab order after the canvas. Command-palette verbs give the keyboard route (CAD-21): *Comb: larger
teeth, smaller teeth, auto scale, denser, sparser*. **No new single-key shortcut** (SC 2.1.4, and none to collide
with `[`/`]` point walking).

### 3.1 Why Auto is held during a drag (deviation from the section editor)

The section editor re-fits on every paint (`SectionCanvas.cs:394`). On a rail that makes the comb breathe: pull a
control point and the whole comb shortens as the p90 tooth grows, so a worse curve can look calmer. Onshape evaluates
the comb live on drag at a fixed gain (area 01, Inferred from the persona's note). Rail rule: **Auto fits at press and
at release, not between**. A "gesture" is a pointer drag, a nudge run (refit at key release plus a short idle, not on each
repeat) or an open rebuild preview (its comb uses the gain at the preview's start). If the refit changes the gain by
more than 2x, the legend line flashes once and the live region says "Comb scale 30 px = 5 per m." The legend line stays
true throughout. After a manual step the Auto button reads as not active (`aria-pressed`). The section editor is not
changed here; bringing it to this rule is the blocker for calling the two editors consistent (OQ-2).

## 4. The radius readout

**Where:** the Tracing strip beneath the Plan (CAD-08: a strip, never an overlay), appended to the existing text. The
probe already evaluates both rails at the pointer's station η, so the radius is the same shape: both rails, at η,
**on the curve**. On leave the strip returns to the selected point's reading.

- Hover, prefixed "At pointer:": `η 0.612 · from root 550.8 mm · chord 61.4 mm · LE R 312 mm · κ +3.2 per m convex · TE R straight · κ 0`
- Selected point (pointer away), prefixed "Point 4:": the rail's curvature at the curve position where the point's basis
  function peaks (its Greville position: stable and local, unlike a Euclidean nearest point, which jumps between spans as
  the point is dragged). On-curve for an anchor and an end point. For an off-curve control point the strip adds "where
  point 4 pulls hardest" and a ring marks the place on the curve. A point never reports a radius "of the control point"
  (False-CAD-Model).
- A ring on each rail marks the evaluated station while a radius is shown.
- **Hovered κ is shown** (A4.9 requires it): signed, `κ +3.2 per m`, positive when the centre of curvature is inside the
  wing (convex), negative when outside (concave). The sign convention is also in the strip's tooltip.
- The strip text is stable on pointer leave (it returns to the selected point's reading, or empties).

**Units and form.** R is in the project length unit with the same precision rule as lengths (`Quantity`): mm up to
9 999 mm, then metres to 2 decimals ("12.40 m"). κ is secondary and in the reciprocal of the same family, per metre in
millimetre and metre projects, per inch in inch projects. Sign is stated in words, not by a minus: "convex" when the
centre of curvature lies inside the wing, "concave" when outside. Teeth point away from the centre of curvature, as in
the section editor (m12c §11.2, re-reviewed by the marine-CAD lens). **The Plan comb as built does not do this:** it
draws every tooth along the fixed left normal at length |κ| and ignores the sign (`PlanCanvas.cs:841-846`; the map does not
flip y, `:1047-1052`), so on a convex outline one rail's teeth point inward and the other's outward. The build adds the
`-sign(κ)` flip (tests `PlanComb_ConvexLE_TeethOutward`, `PlanComb_ConvexTE_TeethOutward`,
`PlanComb_Inflection_TeethSwitchSide`) and the plate says "Teeth point away from the centre of curvature". Whether
Rhino, Alias and Fusion draw hairs on the same side is Flagged (recalled, not cited); the declared rule is applied to
every curve and written on the plate, which is what the convention requires.

**Equal scale is a requirement.** Span and aft share one px-per-metre in the Plan, and the comb needs it for true
normals (`PlanCanvas.cs:1047-1052` does this today). Test: the screen tooth is perpendicular to the screen tangent within
0.5 degrees at three zooms.

**This is the curvature of the outline in the plan (span, aft), in physical units.** It is not the (η, value) graph
curvature of the dihedral, twist or t/c curves (A4.9's recorded deviation); those curves keep their own label.

## 5. The monotone-piece count

**Definition (spec A4.9):** the number of pieces of the **signed** curvature plot κ(s) of one rail on which κ is
monotone, separated by reversals of κ. It counts *extrema of curvature*, **not inflections**: κ crossing zero while
still falling does not split a piece. The variable is arc length s (A4.9 says η; the count is the same for a monotone
reparametrisation, the threshold is not, so s is stated). A rail with κ rising then falling reads 2; a rail whose
curvature only grows toward the tip reads 1. A rail with N pieces has N-1 boundaries.

**Threshold (replaces a relative dead band).** A piece ends only when κ **reverses by more than 0.02 per metre**
(zigzag with hysteresis on κ itself). A band relative to the rail's maximum |dκ/ds| would let the tip rounding swallow a
real wobble on the long, nearly straight run, which is the flaw a fairness check exists to find. The threshold is printed
in the help line and tied at build to the tolerance triple (`assume:` 0.02 per m; confirmed when fixture "tip round plus
a 0.3 mm mid-rail wobble" reads 3 and "straight run" reads 1; if false the fixture fails). Sampling is adaptive: at
every knot, every knot span, and both sides of every anchor.

**Curvature jumps.** At an anchor (doubled knot) the curve is only C1 and κ jumps. The count compares κ on either side
and ends a piece there when the jump reverses the monotone direction. The mockup computes this with one-sided samples.

**Where:** the comb plate, one line per rail: `Leading edge · 2 pieces`, `Trailing edge · 1 piece` (a count of the whole
rail, not a pointer reading, so it stays on the plate; the Tracing strip carries pointer readings only). On the rail,
a non-interactive tick labelled `LE1`, `LE2`, `TE1`… at each boundary in the diagnostic colour, off the focus order and
ignored by hit-testing. Drawn only while Curvature is on. Evaluated on the rail in full, recomputed live with the comb.

**Copy discipline:** the count is a reading, not a grade. The plate never colours the count and never says "good" or
"bad", and the help line states only what is counted and the threshold (COPY-RC-9).

## 6. Hard states

| State | Rule |
|---|---|
| **Straight segment** (|κ| below 1e-3 per metre, i.e. R beyond 1 km; tied at build to the tolerance triple) | Radius reads "straight" and κ reads 0, never "∞" alone and never a huge number. Teeth are zero length and drawn as a dot so the comb shows it is evaluated there. A straight rail is 1 piece. **An inflection of a curved rail is not "straight":** where κ crosses zero between curved neighbours the strip reads "inflection: R infinite here". |
| **Corner, curvature jump or spike** | Teeth over 60 px are clipped at 60 px and marked × (as the section editor); the plate counts them and says "Smaller teeth shows them". Every anchor draws its two one-sided teeth and the dashed break mark (as built, `PointModel.cs:55-63`). **"Corner" is reserved for a tangent corner** (`TangentKind.Corner`). At a smooth anchor whose curvature still jumps (C1 only) the strip reads "curvature jumps at this anchor: R 210 mm root side, 48 mm tip side". "Root side / tip side", never "before / after". |
| **Comb crosses other overlays** | Paint order, bottom to top: foil fill, grid, **comb**, curve and polygon, station chips, limit marker (Ruling 93), probe rings, glyphs. Teeth are 1 px at half strength (`viewport-mute`) so they never hide a glyph; hover and selection hit-tests ignore teeth. The comb clips to the viewport, never to the foil. The rebuild preview's own comb uses the station colour and replaces the rail's comb while the preview is open. |
| **Analysis mode** | The comb is a CAD-edit aid: it is not drawn in Analysis and the plate is absent. The toolbar Curvature toggle is disabled with the reason "Curvature is shown in CAD" (existing disabled-with-reason convention). Scale and density are remembered. |
| **Comb off** | No plate, no ticks, no count; the radius still appears in the Tracing strip (it does not need the comb). |
| **No foil / empty** | Nothing is drawn; the Start card is unchanged. |
| **Tooth crowding at 128 per rail on a small viewport** | Teeth are thinned to a 3 px minimum pitch; the plate shows the drawn count. |
| **Narrow width (560 px)** | The plate collapses to a one-line summary "Comb · Auto · 8" that opens the steppers (a popover, Escape closes). |
| **Small gain, large radius** | A fixed step too large for the foil draws only dots; the plate says "No tooth over 3 px. Larger teeth shows more." |

## 7. Copy (proposed COPY-RC rows; the operator batch assigns the COPY numbers)

| Row | Surface | String |
|---|---|---|
| COPY-RC-1 | plate title | `Comb` |
| COPY-RC-2 | scale label and value | `Scale` · `Auto` · `30 px = 5 per m (R 200 mm)` · `Shared by both rails. Teeth point away from the centre of curvature.` |
| COPY-RC-3 | scale stepper names | `Smaller teeth` · `Larger teeth` · `Auto scale` |
| COPY-RC-4 | density label and value | `Density` · `32 per rail` ; names `Sparser` · `Denser` |
| COPY-RC-5 | clipped note | `3 teeth clipped (×). Smaller teeth shows them.` |
| COPY-RC-6 | pieces line | `Leading edge · 2 pieces` · `Trailing edge · 1 piece` |
| COPY-RC-7 | radius and curvature in the strip | `At pointer: … LE R 312 mm · κ +3.2 per m convex` · `TE R straight · κ 0` · `LE inflection: R infinite here · κ 0` · `LE corner: R 210 mm root side, R 48 mm tip side` · `LE curvature jumps at this anchor: R 210 mm root side, 48 mm tip side` |
| COPY-RC-8 | selected control point | `Point 4: LE R 312 mm · κ +3.2 per m convex where point 4 pulls hardest` |
| COPY-RC-9 | pieces help (tooltip on the line) | `Pieces of the curvature plot between its peaks and dips. Reversals under 0.02 per m are ignored.` |
| COPY-RC-10 | Analysis disabled reason | `Curvature is shown in CAD.` |
| COPY-RC-11 | no visible tooth | `No tooth over 3 px. Larger teeth shows more.` |
| COPY-RC-12 | palette verbs | `Comb: larger teeth` · `Comb: smaller teeth` · `Comb: auto scale` · `Comb: denser` · `Comb: sparser` |
| COPY-RC-13 | status (live region, once per change) | `Comb scale 30 px = 2 per m.` · `Comb density 64 per rail.` |

## 8. Spec amendments and open items, batched for the operator

- **AM-RC-1 (A4.9, amendment, not only clarification):** (a) A4.9 says the comb "scales to its longest tooth and never
  clips". The section editor and this design use p90 = 30 px with clipping at 60 px and a counted × mark; amend A4.9 to
  say so. (b) After "its scale is a stepper" add: *(1.8: on the planform rails the scale is Auto or a fixed step stated as
  "30 px = N per metre"; Auto is held during an edit gesture and refits once at its end; density is 16, 32, 64 or 128
  teeth per rail at an even arc-length pitch; hovered κ is signed and the radius is the outline's curvature in the plan,
  read in the Tracing strip; the monotone count is on signed curvature with a stated reversal threshold and is a reading,
  not a grade.)* (c) A4.9 names dκ/dη for the count; the build uses dκ/ds (same count for a monotone reparametrisation;
  the threshold differs, so it is stated).
- **AM-RC-2 (A4.9 recorded deviation):** the rail radius is physical (the outline in the plan). The deviation for master
  curves stands.
- **OQ-2 (follow-up, not this build):** the section editor gets the same stepper, Auto-hold during a drag, and a pieces
  line for its two curves. Today it re-fits on every paint and shows no count.
- **OQ-3 (follow-up):** Fair (`ConstrainedFit.SignChanges`) is labelled "monotone pieces" but counts slope sign changes;
  rename or re-base it when the section line is added. Candidate defect class: *same name, two definitions*.
- **OQ-4 (follow-up, Inferred, not run):** the section editor's Thickness x2 branch multiplies the tooth's y component by 2
  under a 2x y stretch; under a stretch the true normal's y component scales by 1/2, so the teeth tilt in that mode.

## 9. Build shape (estimate, not a commitment)

Surface list (E7): Core `Planform` (curvature at a station, one-sided at anchors; monotone pieces of curvature; teeth by whole-rail arc length; all pure, analytic
derivatives) → `PlanformView` unchanged → Desktop `PlanCanvas` (comb both rails, gain, density, ticks, ring) → plate
control → Tracing strip text → command table verbs → view preferences → Analysis disable → DESIGN.md copy rows.

- **Size: S-M, about 2 days.** Core 0.75 day (curvature at a station with one-sided values at anchors, monotone pieces of signed curvature with the reversal threshold, whole-rail arc-length teeth; eight fixtures); canvas and plate 0.75 day; copy, verbs,
  disabled state, narrow collapse 0.25 day.
- **Tests (named, each states its ring):** `Planform_CurvatureAt_StraightRun_ReportsStraight`;
  `Planform_CurvatureAt_Circle_RadiusWithin1e-9`; `Planform_MonotonePieces_StraightRun_One`;
  `Planform_MonotonePieces_TipRound_Two`; `Planform_MonotonePieces_Wobble_Three`;
  `Planform_MonotonePieces_TipRoundPlusWobble_Three` (0.3 mm mid-rail wobble); `Planform_Teeth_PitchConstantWithin5Percent`; `PlanComb_ConvexLE_TeethOutward`; `PlanComb_ConvexTE_TeethOutward`; `PlanComb_Inflection_TeethSwitchSide`; `PlanComb_TeethPerpendicularToTangent_AtThreeZooms`; `PlanComb_PlateDoesNotCoverRail_AtTipFit`; `PlanProbe_StripStableOnLeave`; `PlanComb_BothRails_NothingSelected`; `PlanComb_AutoHeldDuringDrag_RefitOnRelease`;
  `PlanComb_Density_ChangesToothCountOnly_FoilBytesUnchanged`; `PlanComb_Analysis_NotDrawnToggleDisabled`;
  `PlanProbe_RadiusInProjectUnit`. All fast-ring Core or headless-Avalonia; no new slow test.
- **Instrumentation:** none new. View settings are not model state; the existing frame-time budget for the drag is the
  measure (the comb recompute must stay inside the 100 ms edit budget; 2 rails × 128 teeth at the densest step is 256 evaluations).

## 10. Adversary pass and operator questions

Adversary record: [`docs/proof/cmb/adversary.md`](../proof/cmb/adversary.md). Operator questions are at the end of the
mockup page and in the track Return; there are three, each with a recommendation.

1. **Comb on both rails at once** (today: only the selected rail, nothing with no selection). Recommended: both, the
   selected rail at full strength.
2. **Auto scale held during an edit gesture**, refit once at its end (the section editor refits every paint). Recommended:
   hold; bring the section editor to the same rule in a later slice (AM-RC-1, OQ-2).
3. **Pieces ignore curvature reversals under 0.02 per metre**, printed in the help line. Recommended: yes; the build's
   fixtures confirm the number.
