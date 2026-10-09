---
id: design-rail-comb
title: "Rail comb: scale and density, a radius readout and the monotone-piece count on the planform rails"
type: design
status: proposed
owner: "@timianmalloo"
phase: design, revision 2 (track CMR) — Ruling 194 answers and the reviewers' conditions folded in; final copy returns to the operator as one table (§7)
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
  Design for OI-5 on the planform rails, revision 2. The Plan comb keeps its toggle (Curvature, C) and gains a plate in
  the viewport with a scale stepper (Auto, or a fixed 1-2-5 gain stated as "30 px = N per metre") and a density stepper
  (16, 32, 64, 128 teeth per rail). The Tracing strip gains the radius and signed curvature of each rail at the pointer's
  station, or at the selected point's station on the curve. The plate shows the monotone-piece count per rail, names each
  piece boundary in text, and prints the resolved reversal threshold (0.02 divided by the rail's arc length). Ruling 194:
  both rails, Auto held during a gesture, threshold tau / L. The geometry rules (extrema by root-finding, one-sided
  curvature at anchors, sign, Greville station) and the keyboard, focus and narrow-width rules are fixed here, with ten
  build conditions and named fixtures. Adversary and review records are in docs/proof/cmb/adversary.md.
---

# Rail comb: scale, density, radius, monotone pieces (revision 2)

- **Track / tier:** CMB (revision 1), CMR (revision 2), T1, docs and mockup only. No `src/` or `tests/` change. Code read at `fd5b185c` and again at `c6a4a404` for the files cited in §3.2 and §5.
- **Mockup:** [`docs/mockups/rail-comb.html`](../mockups/rail-comb.html), approved visually by the operator (Ruling 194). Revision 2 changes it only where a condition needs it (§10).
- **Rulings:** 193 (B); 107 (DR-GM-9 C); **194** (the three answers below).
- **Spec:** A4.9 "Diagnostics and readouts", CAD-08 (the Tracing strip), CAD-21, GEO-05, OI-5 in [`m12b-points.md`](m12b-points.md).

**Ruling 194, recorded.** (1) The comb draws on **both rails** whenever Curvature is on, the selected rail at full strength.
(2) **Auto is held during an edit gesture** and refits once at its end (§3.1). (3) The monotone reversal threshold **scales
with rail length: tau / L**, tau = 0.02 dimensionless, L the rail's arc length; the plate prints the resolved per-metre
value (§5).

## 1. What exists today (read, not recalled)

| Fact | Evidence |
|---|---|
| The Plan comb is toggled by Curvature (toolbar) and `C` with the canvas focused. | `PlanCanvas.cs:309-312`, `:662` (Verified, read) |
| It draws only for the rail of the first selected point (or the rebuild preview), never for both rails, never with nothing selected. | `PlanCanvas.cs:837-846`, `:817-822` (Verified, read) |
| Tooth length is a fixed gain: `clamp(|κ| × 100, 6, 24)` px. A fair and an unfair rail look alike. | `PlanCanvas.cs:844`, `:821` (Verified, read) |
| Teeth are 8 per knot span spaced by arc length, plus one-sided teeth either side of each anchor (read as knot ± 1e-9) and a tooth at a doubled knot. κ is signed, from analytic first and second derivatives, in per metre. | `PointModel.cs:40-74`, `:182-225` (Verified, read) |
| An **anchor is a triple knot**: `IsAnchor` requires `knots[i+1] = knots[i+2] = knots[i+3]` and different neighbours. A cubic is C0 at a triple knot and C1 at a doubled knot. | `FoilSource.cs:307-315` (Verified, read) |
| `Tooth` returns curvature 0 when `speed2 == 0`: a degenerate tangent reads as a straight rail. | `PointModel.cs:223` (Verified, read) |
| The probe strip writes η, from root, % half-span, LE, TE and chord at the pointer. No curvature or radius. | `PlanCanvas.cs:242-244` (Verified, read) |
| In the canvas, `[` and `]` walk the points (`FocusNext`), the arrow keys **nudge** the focused point, and **Tab calls `TabToProperties()`** when a point is selected, which leaves the canvas for the Properties pane. | `PlanCanvas.cs:597`, `:614-619`, `:670-687` (Verified, read) |
| The section editor's comb: auto scale only, p90 tooth = 30 px, teeth over 60 px clipped and marked ×, recomputed on every paint. No stepper. | `SectionCanvas.cs:394-426`, `:99-105` (Verified, read) |
| The only "monotone piece" count in Core is `SignChanges` of the **ordinate slope**, used by Fair. It is not a count of pieces of curvature. | `ConstrainedFit.cs:515-535` (Verified, read) |
| `SplineBasis.Jet` carries N, D1, D2 only (no D3). | `ConstrainedFit.cs:660` (Verified, read) |

Two consequences. (1) "Matching the section editor" cannot mean copying a stepper: the section editor has none. This design
adds the stepper to the rails and names the section editor as a follow-up (OQ-2). (2) The spec's monotone count is on
**curvature**; the built count is on **slope**. The rail count is new Core code and must not reuse `SignChanges` by name.

## 2. Direction

- **Who, when:** the one foil designer, fairing the outline after a drag, asking "is the curvature of this edge smooth and where does it kink".
- **Job:** read the shape of curvature along each rail, change the reading scale without changing the foil, and get one number (radius) at a place, one count (pieces) per rail.
- **Archetype:** unchanged (G1 Parametric Modeling Workbench); view diagnostics inside an existing viewport.
- **Adjectives:** quiet, quantitative, honest (not decorative, not a score, not a verdict).
- **References taken:** the section editor's plate and clipping (m12c §11.2); Rhino and Alias curvature graph, Fusion curvature comb (recalled, Inferred for exact control names). Nothing is taken for its look.
- **Anti-goals:** no fairness grade, no pass/fail colour on the count, no auto-fair, no new document state.

## 3. What the user controls

Scale and density are **view settings**: they never touch the foil, never enter undo, never change the file. They persist for
the session and across files in the user's view preferences (the home of the Curvature toggle).

| Control | Values | Default | Notes |
|---|---|---|---|
| Curvature (toggle) | on / off | off (as built) | toolbar and `C`, unchanged |
| Scale | **Auto**, or a fixed gain "30 px = κ" on the ladder 0.5, 1, 2, 5, 10, 20, 50, 100, 200 per metre (per inch in an inch project: 1-2-5 in that unit) | Auto | Auto: the 90th-percentile tooth over **both rails** (one shared gain) is 30 px. **Held during a gesture** (§3.1). A fixed step never changes by itself. |
| Density | 16, 32, 64, 128 teeth per rail, even by arc length over the whole rail | 32 | One-sided teeth at every curvature jump are always drawn. Teeth closer than 3 px are thinned. Even sampling can step over a narrow spike; **the piece count does not depend on density** (§5 finds extrema by root-finding), and the ticks mark them. |

No typed channel for scale or density. **Where:** a plate in the Plan viewport, top right, shown only while Curvature is on
(the toolbar is at its control ceiling; Properties describes the selection; the section editor puts its comb plate on the
canvas). Command-palette verbs give a second route (CAD-21). **No new single-key shortcut** (SC 2.1.4).

### 3.1 Why Auto is held during a drag (Ruling 194 answer 2)

The section editor refits on every paint, so the comb breathes and a worse curve can look calmer. Rail rule: **Auto fits at
press and at release, not between.** A "gesture" is a pointer drag, a nudge run (refit at key release plus a short idle) or an
open rebuild preview (it uses the gain at its start). If the refit changes the gain by more than 2x, the legend line flashes
once and the live region says "Comb scale 30 px = 5 per m." After a manual step the Auto button is not pressed. The section
editor is not changed here (OQ-2).

### 3.2 Keyboard, focus and the stepper limit (conditions M2, M3, M4)

**The real keyboard route to a reading** (M2). The arrow keys in the canvas already nudge the focused point
(`PlanCanvas.cs:670-687`), so they cannot probe. The route is: `[` and `]` walk the points; the Tracing strip reads
"Point N: …" at that point's station on the curve (§4); and each piece boundary is given in text on the plate's pieces line
(§5, M6). The mockup's arrow-key probe is removed; the harness has a labelled range input ("Probe station (harness only)",
native slider semantics) that stands in for the pointer and is not part of the product.

**Focus order** (M3). Today Tab in the canvas with a point selected goes straight to Properties, so a plate placed "after the
canvas" would be skipped by exactly the user who needs it. Revised order: **plan canvas, then the plate (smaller, larger,
Auto, sparser, denser; the one-line summary at narrow width), then Properties' first value.** Shift+Tab reverses. Required
change: `TabToProperties()` (`PlanCanvas.cs:597`) hands focus to the plate's first control when the plate is shown (Curvature
on, CAD), and the plate's last control calls `TabOut`. With the plate hidden, or no point selected, nothing changes. The
existing DR-NAV-1 return (Shift+Tab from Properties' first value to the selected point, `FocusSelectedPoint`) is unchanged.
Test: `PlanCanvas_Tab_VisitsPlateBeforeProperties` (and `_ShiftTab_ReturnsToCanvas`, `_PlateHidden_TabGoesToProperties`).

**A stepper at its limit keeps focus** (M4). A disabled button cannot hold focus, so the focus would jump (to Auto in revision 1).
The build uses an `aria-disabled` equivalent: the button stays in the tab order, looks disabled (dashed), ignores the press
and announces the limit ("Largest teeth reached.", COPY-RC-14). If a native disabled state were ever used, focus falls to the
**paired** stepper (larger to smaller, sparser to denser), never to Auto. The mockup shows the aria-disabled form
(`data-act="larger"` keeps focus and the live region says "Largest teeth reached."; checked in the browser).

**Viewport focus ring** (M1). The plan's focus ring uses `focus-ring-viewport` (#66ddc8), **not `--accent`**, which is 2.45:1
on `--vp` in the light theme. The pair `--focus-vp` on `--vp` is in the in-page audit (and passes at 4.5:1; the plate's own
buttons sit on the surface and keep the accent ring at 6.11:1 on `--surface`). Browser check: the focused plan's computed
outline colour is rgb(102, 221, 200).

## 4. The radius readout

**Where:** the Tracing strip beneath the Plan (CAD-08: a strip, never an overlay), appended to the existing text.

- Hover, prefixed "At pointer:": both rails at the pointer's station η, on the curve.
- Selected point (pointer away), prefixed "Point N:": the rail's reading **at point N's station on the curve**: the point's
  Greville abscissa **ξᵢ = (t[i+1] + t[i+2] + t[i+3]) / 3**, evaluated at **parameter t = ξ directly**, not through
  `ChannelEvaluator`'s η inversion (which would add an inversion error and, at an anchor, pick a side). The Greville
  station is stable and local, unlike a Euclidean nearest point. For an off-curve control point the strip adds "at point N's
  station on the curve" and a ring marks the place. The copy does **not** claim the station is where the basis function peaks
  or where the point "pulls hardest": the Greville abscissa is the basis function's mean position, not its peak. A point never reports a radius "of the control point".
- A ring on each rail marks the evaluated station. On pointer leave the strip returns to the selected point's reading.

**Sign convention (stated here, in the plate legend and in the strip tooltip).** Displayed curvature is **κ_display = +κ_raw on
the leading edge and −κ_raw on the trailing edge**, so **positive means convex** (the centre of curvature lies inside the
wing) on both rails. The strip says it in words: "convex" or "concave". Teeth point away from the centre of curvature, as in
the section editor; the Plan as built draws every tooth along the fixed left normal and ignores the sign
(`PlanCanvas.cs:841-846`), so the build adds the `-sign(κ)` flip (tests `PlanComb_ConvexLE_TeethOutward`,
`PlanComb_ConvexTE_TeethOutward`, `PlanComb_Inflection_TeethSwitchSide`). Whether Rhino, Alias and Fusion draw hairs on the
same side is Flagged (recalled, not cited); the rule is declared and written on the plate.

**Degenerate tangent.** The `speed2 == 0` branch (`PointModel.cs:223`) **throws** in the build; it must never return 0 (which would read as "straight").

**Anchors read both sides** (Computational Geometry). At a knot of multiplicity 2 or more (doubled: C1, triple anchor: C0) κ
is read **one-sided by explicit span selection** (evaluate the left span's polynomial at its end and the right span's at its
start), not by `knot ± 1e-9`. The readout gives R and signed κ **on both sides**: "LE corner: root side R 1910 mm, κ +0.52 per m
· tip side R 284 mm, κ −3.53 per m". **When the two signs differ, the strip adds "curvature changes sign at this anchor".**
"Corner" is classified by the **measured tangent-angle jump against 0.1°**, not by `TangentKind`; a smooth (G1) anchor whose
curvature jumps by more than τ / L reads "curvature jumps at this anchor". The mockup measures both (anchor kinds row: the
G1 example jumps 0.000°, the corner example 32.4°). "Root side / tip side", never "before / after".

**Units and form.** R is in the project length unit with the `Quantity` precision rule (mm up to 9 999 mm, then metres to 2
decimals). κ is per metre in millimetre and metre projects, per inch in inch projects. **Reported lengths are Gauss–Legendre
arc length, never the display polyline** (the mockup's audit compares them: 932.39 mm both, to 2 decimals).

**Equal scale is a requirement** (true normals; `PlanCanvas.cs:1047-1052`). Test: the screen tooth is perpendicular to the
screen tangent within 0.5° at three zooms. **This is the curvature of the outline in the plan**, not the (η, value) curvature of the dihedral, twist or t/c curves (A4.9's recorded deviation).

## 5. The monotone-piece count

**Definition (spec A4.9):** the number of pieces of the **signed** curvature plot κ_display(s) of one rail on which κ is
monotone, separated by reversals. It counts **extrema of curvature, not inflections**. The variable is arc length s.

**Threshold (Ruling 194 answer 3).** A piece ends when κ **reverses by more than τ / L**: L is the rail's Gauss–Legendre arc
length, τ = 0.02 is dimensionless (κ·L is dimensionless, so the threshold scales with the rail). The plate **prints the
resolved per-metre value** per rail, as a plate line, not a tooltip (condition M7): "Ignores reversals under 0.02 ÷ rail
length: 0.021 per m (LE), 0.022 per m (TE)." (example rails, L = 932 mm and a little longer.) `assume:` τ = 0.02; confirmed
when F4 reads 3 and F5 changes at 2τ (§9); if false those fixtures fail. A band relative to the rail's maximum would let the
tip rounding hide a mid-rail wobble; a fixed per-metre value would make the same shape read differently at 0.3 m and 2 m.

**Extrema (replaces sampling).** Per knot span the numerator of dκ/dt is a polynomial of degree at most 6:
`N(t) = (x'y''' − y'x''')(x'² + y'²) − 3(x'y'' − y'x'')(x'x'' + y'y'')`. The build adds **D3 to `SplineBasis.Jet`** (constant on
a cubic span) and finds the roots of N in each span by dense sampling of its sign (128 per span) with bisection on each
bracket. **Every simple knot is also a candidate**, and so are the one-sided values at both ends of every span. Between two
consecutive candidates κ is monotone, so the zigzag with hysteresis over the candidate list is exact and independent of
the comb's density. `simplify:` sampling plus bisection; upgrade to a Sturm sequence if a fixture shows a missed root pair
(ceiling: two roots closer than 1/128 of a span).

**Anchors and jumps.** The anchor is a triple knot, so the curve is C0 there and κ is read one-sided (§4). The two one-sided
values are consecutive candidates, so **a jump against the trend splits a piece and a jump with the trend does not**
(fixture F8). §6 says "G1 only", not "C1 only", for a smooth anchor.

**Where:** the plate, one line per rail, with each boundary named in text (M6): `Leading edge · 2 pieces · LE1 at η 0.69`
(η is the span fraction of the boundary). On the rail, a non-interactive tick `LE1`, `LE2`, … in the diagnostic colour, off
the focus order and ignored by hit-testing. Drawn only while Curvature is on, recomputed live with the comb.

**Comb envelope.** The envelope joining the tooth tips is drawn **per continuous piece**, with a visible step (a thin line
joining the two one-sided tips) at each curvature jump; a corner also keeps its dashed break mark.

**Copy discipline:** the count is a reading, not a grade; never coloured, never "good" or "bad".

## 6. Hard states

| State | Rule |
|---|---|
| **Straight segment** | A rail point is straight when **|κ| · L² / 8 < 10 µm** (the sagitta of the rail's arc at that curvature, a tolerance independent of unit). Radius reads "straight", κ 0, never "∞" alone. A straight rail is 1 piece. **Inflection** reads "inflection: R infinite here" **only when the neighbouring stations (±0.02 in t) have opposite curved sign**; a runout from curved to straight reads "straight". |
| **Corner, curvature jump or spike** | Teeth over 60 px are clipped and marked ×; the plate counts them. Every curvature jump draws two one-sided teeth and a step in the envelope; a corner (tangent angle > 0.1°) adds the dashed break mark. A **smooth anchor whose curvature jumps is "G1 only"** and reads "curvature jumps at this anchor". |
| **Comb crosses other overlays** | Paint order, bottom to top: foil fill, grid, comb, curve and polygon, station chips, limit marker (Ruling 93), probe rings, glyphs. Teeth are 1 px at half strength; hit-tests ignore teeth. |
| **Analysis mode** | No comb, no plate; the toolbar toggle is disabled with "Curvature is shown in CAD." Scale and density are remembered. |
| **Comb off** | No plate, no ticks; the radius still appears in the Tracing strip. |
| **No foil / empty** | Nothing is drawn; the Start card is unchanged. |
| **Crowding at 128 per rail** | Teeth thinned to a 3 px minimum pitch; the plate shows the drawn count. |
| **Narrow width (560 px)** (M5) | The plate is a one-line summary "Comb · Auto · 32". Opened, **the steppers open in flow between the viewport and the Tracing strip** (a disclosure, Escape closes), never as an overlay, so the tip and the readout strip are never covered. The mockup has the open-state card; its audit row checks viewport, plate and strip in order (5683 ≤ 5690, 5915 ≤ 5921 px) and that the open stage plate is clear of the viewport. |
| **Small gain, large radius** | A step too large for the foil draws dots; the plate says "No tooth over 3 px. Larger teeth shows more." |
| **Plate position** | The plate at 1320 px covers 0 rail or tooth points on the example planform (audit, light and dark) and at 520 px. On other planforms the product places it by the `ScaleBarBounds` exclusion pattern; the mockup's fixed corner covers up to 3 points on its straight and wobble variants and is not the placement rule. |

## 7. Final copy and amendments for operator approval

One table: every COPY-RC row in final form, then the amendments. The operator batch assigns the COPY numbers.

| Id | Surface | Final string or text |
|---|---|---|
| COPY-RC-1 | plate title | `Comb` |
| COPY-RC-2 | scale label, value, note | `Scale` · `Auto` · `Auto · 30 px = 1 per m (R 978 mm)` · `Shared by both rails. Teeth point away from the centre of curvature.` |
| COPY-RC-3 | scale stepper names | `Smaller teeth` · `Larger teeth` · `Auto scale` |
| COPY-RC-4 | density label and value | `Density` · `32 per rail`; names `Sparser` · `Denser` |
| COPY-RC-5 | clipped note | `3 teeth clipped (×). Smaller teeth shows them.` |
| COPY-RC-6 | pieces line (names every boundary) | `Leading edge` · `2 pieces · LE1 at η 0.69` ; `Trailing edge` · `1 piece` |
| COPY-RC-7 | radius and curvature in the strip | `At pointer: … LE R 312 mm · κ +3.2 per m convex` · `TE R straight · κ 0` · `LE inflection: R infinite here · κ 0` · `LE corner: root side R 1910 mm, κ +0.52 per m · tip side R 284 mm, κ −3.53 per m · curvature changes sign at this anchor` · `LE curvature jumps at this anchor: root side R 1910 mm, κ +0.52 per m · tip side R 623 mm, κ +1.61 per m` |
| COPY-RC-8 | selected control point | `Point 4: LE R 312 mm · κ +3.2 per m convex at point 4's station on the curve` |
| COPY-RC-9 | threshold, a plate line (not a tooltip) | `Ignores reversals under 0.02 ÷ rail length: 0.021 per m (LE), 0.022 per m (TE).` |
| COPY-RC-10 | Analysis disabled reason | `Curvature is shown in CAD.` |
| COPY-RC-11 | no visible tooth | `No tooth over 3 px. Larger teeth shows more.` |
| COPY-RC-12 | palette verbs | `Comb: larger teeth` · `Comb: smaller teeth` · `Comb: auto scale` · `Comb: denser` · `Comb: sparser` |
| COPY-RC-13 | status (live region, once per change) | `Comb scale 30 px = 2 per m.` · `Comb density 64 per rail.` |
| COPY-RC-14 | stepper at its limit (live region, no focus move) | `Largest teeth reached.` · `Smallest teeth reached.` · `Sparsest density reached.` · `Densest density reached.` |
| COPY-RC-15 | plan canvas accessible name | `Plan view of the half-wing with the curvature comb. Press [ and ] to walk the points; the Tracing strip reads the radius at the selected point.` |
| AM-RC-1 | spec A4.9 amendment | (a) A4.9 says the comb "scales to its longest tooth and never clips"; amend to p90 = 30 px with clipping at 60 px and a counted × mark. (b) After "its scale is a stepper": *on the planform rails the scale is Auto or a fixed step stated as "30 px = N per metre"; Auto is held during an edit gesture and refits once at its end; density is 16, 32, 64 or 128 teeth per rail at an even arc-length pitch; hovered κ is signed (+ convex, LE +κ and TE −κ) and the radius is the outline's curvature in the plan, read in the Tracing strip; the monotone count is on signed curvature, ends a piece when κ reverses by more than τ / L (τ = 0.02, L the rail's arc length, the resolved value printed), counts extrema not inflections, and is a reading, not a grade.* (c) A4.9 names dκ/dη; the build uses dκ/ds, and the threshold is stated. |
| AM-RC-2 | A4.9 recorded deviation | The rail radius is physical (the outline in the plan). The deviation for master curves stands. |
| AM-RC-3 | new: A4.9 sign convention | Positive curvature means convex on both rails: κ_display = +κ_raw on the leading edge, −κ_raw on the trailing edge. |
| AM-RC-4 | new: DR-NAV-1 focus order | When the comb plate is shown, Tab in the plan goes to the plate, then to Properties; Shift+Tab reverses. Keyboard reading of a rail is `[` and `]` point walking, not arrow keys (arrows nudge). |
| AM-RC-5 | new: "C1 only" becomes "G1 only" | A smooth anchor (tangent angle jump ≤ 0.1°) whose curvature jumps is reported as such; "corner" is the measured tangent jump above 0.1°, not `TangentKind`. |

## 8. Open items (not this build)

- **OQ-2:** the section editor gets the same stepper, Auto-hold and a pieces line. Today it refits every paint and shows no count.
- **OQ-3:** Fair (`ConstrainedFit.SignChanges`) is labelled "monotone pieces" but counts slope sign changes; rename or re-base. Candidate defect class: *same name, two definitions*.
- **OQ-4 (Inferred, not run):** the section editor's Thickness x2 branch tilts its teeth under a 2x y stretch.
- **Minors m1 to m6 and nits n1, n2 of the accessibility review:** their wording is in the leader's record and was not in the CMR brief; this revision fixes the items the brief names (M1 to M7) and the geometry conditions, and the leader maps the remaining minors and nits (§11 residual).

## 9. Build shape, build conditions and tests

Surface list (E7): Core `Planform` (curvature at a station with one-sided values by span selection; D3 in `SplineBasis.Jet`;
extrema by root-finding; monotone pieces with τ / L; Gauss–Legendre length; teeth by whole-rail arc length; all pure) →
`PlanformView` unchanged → Desktop `PlanCanvas` (comb both rails, gain, density, per-piece envelope, ticks, ring, `TabToProperties`
change) → plate control (steppers with aria-disabled, narrow in-flow disclosure) → Tracing strip text → command table verbs →
view preferences → Analysis disable → DESIGN.md copy rows and token use.

### Build conditions (C1 to C10; C1 is the reviewer's, C2 to C10 are numbered here from the named conditions)

| # | Condition | Test (each states its ring; all fast-ring Core or headless-Avalonia) |
|---|---|---|
| C1 | A stepper at its limit keeps focus (aria-disabled equivalent) and announces the limit | `PlanComb_StepperAtLimit_KeepsFocusAndAnnounces` |
| C2 | Focus order plan, plate, Properties; `TabToProperties` hands to the plate when shown | `PlanCanvas_Tab_VisitsPlateBeforeProperties`, `_ShiftTab_ReturnsToCanvas`, `_PlateHidden_TabGoesToProperties` |
| C3 | Keyboard reading by `[` `]` walking with "Point N:"; no arrow-key probe; arrows still nudge | `PlanCanvas_BracketWalk_ReadsPointStation`, `PlanCanvas_ArrowKeys_StillNudge` |
| C4 | Plan focus ring is `focus-ring-viewport` | `PlanCanvas_FocusRing_UsesViewportToken` and the token pair at least 3:1 on `--vp` |
| C5 | Narrow plate opens in flow and covers neither tip nor strip | `PlanComb_NarrowOpenPlate_DoesNotCoverTipOrStrip`, `PlanComb_PlateDoesNotCoverRail_AtTipFit` |
| C6 | Pieces line names every boundary in text; ticks non-interactive | `PlanComb_PiecesLine_NamesEveryTick` |
| C7 | Threshold printed as a plate line with the resolved per-m value per rail | `PlanComb_PlateThresholdLine_PrintsResolvedPerMetre` |
| C8 | Gesture, limit and scale changes announce once per change (live region) | `PlanComb_LiveRegion_OncePerChange` |
| C9 | Geometry: the fixtures F1 to F10 below | `Planform_MonotonePieces_*` |
| C10 | Curvature core: D3; one-sided span selection (no knot ± ε); `speed2 == 0` throws; Gauss–Legendre length; sign; straight tolerance; Greville at t = ξ | `Planform_CurvatureAt_*` (below) |

**Core fixtures.** Layer 1 is the pure zigzag over a candidate list (`MonotonePieces(candidates, θ)`), driven by analytic
κ(s) with its extrema found numerically. Layer 2 is the spline layer on exact cubic pieces (a cubic Bezier is an exact cubic B-spline).
Dimensionless: σ = s / L, κ̂ = κ·L, so a fixture at L = 0.3, 1 and 2 m is the same list (F6).

| # | Fixture | Expected |
|---|---|---|
| F1 | straight run (collinear points) `Planform_MonotonePieces_StraightRun_One` | 1 |
| F2 | monotone tip: κ̂(σ) = 2σ⁴ `…_MonotoneTip_One` | 1 (τ → 0 and τ) |
| F3 | interior peak: parabola y = x² on x ∈ [−1, 1], exact cubic; κ̂ = κ·L rises from about 0.53 at the ends to 5.9 at the vertex (L = 2.96, κ = 2 there; arithmetic, not run) and falls `…_InteriorPeak_Two` | 2 (τ → 0 and τ) |
| F4 | **tip round + wobble, analytic:** κ̂(σ) = 2σ⁴ + 0.20·exp(−((σ − 0.5)/0.06)²): trend 2σ⁴ (the tip round), a Gaussian bump of amplitude 0.20, width 0.06, centred at σ = 0.5. Extrema at σ = 0.5098 (peak, κ̂ 0.3298) and σ = 0.5790 (dip, κ̂ 0.2601); drop 0.0697 = 3.5 τ `…_TipRoundPlusWobble_Three` | **3 as τ → 0; 3 at τ** (computed by `docs/proof/cmb/fixture-profile.py`, run) |
| F5 | threshold edges: the same profile with amplitude 0.15: extrema at 0.5137 and 0.5703, drop 0.0321 = 1.6 τ `…_ThresholdEdges` | threshold 0.5 τ → 3; τ → 3; 2 τ → **1** (computed, run) |
| F6 | scale invariance: F4 realised at L = 0.3, 1, 2 m `…_ScaleInvariant` | same count 3 at each (mockup audit: example TE and wobble TE read 2 and 4 at ×0.3, ×1, ×2, run) |
| F7 | narrow spike between density samples: F4 with width 0.01 at density 16 `…_IndependentOfDensity` | 3 at density 16 and 128 (counted from roots, not teeth) |
| F8 | anchor jump: κ̂ rising trend with a one-sided jump of 3 τ at an anchor, against the trend (down) and with it (up) `…_AnchorJump_AgainstTrend_Splits`, `…_WithTrend_DoesNotSplit` | 2 against, 1 with |
| F9 | reversal exactly at a simple knot: mirror-symmetric points about a simple knot `…_ReversalAtSimpleKnot_Counted` | 2 |
| F10 | mirror invariance: reflect the aft axis (κ_display unchanged) `…_MirrorInvariant` | same count (mockup audit: 4 rails, run) |

Curvature core: `Planform_CurvatureAt_Circle_RadiusWithin1e-9`, `…_StraightRun_ReportsStraight`, `…_AnchorOneSided_BothSides`,
`…_CornerClassifiedByAngle_0p1Degrees`, `…_DegenerateTangent_Throws`, `…_GrevilleAtT_EqualsXi`, `…_ArcLength_GaussLegendre`,
`Planform_Teeth_PitchConstantWithin5Percent`. Canvas: `PlanComb_BothRails_NothingSelected`, `PlanComb_AutoHeldDuringDrag_RefitOnRelease`,
`PlanComb_Density_ChangesToothCountOnly_FoilBytesUnchanged`, `PlanComb_Analysis_NotDrawnToggleDisabled`, `PlanProbe_RadiusInProjectUnit`,
`PlanProbe_StripStableOnLeave`, `PlanComb_TeethPerpendicularToTangent_AtThreeZooms`, `PlanComb_EnvelopePerPiece_StepAtJump`.

**Size (revised): M, about 3 days** (was S-M, 2 days). Core 1.25 day (D3, per-span root-finding, one-sided spans, Gauss–Legendre
length, ten fixtures); canvas and plate 1.0 day (both rails, envelope per piece, aria-disabled steppers, in-flow narrow plate);
focus order and live regions 0.5 day (`TabToProperties` change, automation names, three tab tests); copy, verbs, disabled state 0.25 day.
**Instrumentation:** none new; the comb recompute stays inside the 100 ms edit budget (2 rails x 128 teeth = 256 evaluations; root-finding runs on edit end, not per tooth).

## 10. Mockup revision record (what changed, and only because a condition needed it)

Focus ring token (M1); arrow probe removed, `[` `]` walking and a harness slider (M2); a stand-in Properties button shows the tab
order (M3); aria-disabled steppers with live announcements (M4); open plate in flow at narrow width plus an open-state card
(M5); pieces line names each tick (M6); the threshold is a plate line, no `title` (M7); τ / L per rail; extrema by root-finding
on the numerator of dκ/dt with explicit span selection; one-sided values at every knot of multiplicity 2 or more; corner by measured
angle; both-sided readout with the sign-change phrase; Greville at t = ξ; Gauss–Legendre length; envelope per piece with a step; a
G1 planform; sign by wing side; straight by the 10 µm rule; inflection only between opposite curved neighbours; the plate is wider (360 px) and its
pieces rows are one line each so it still covers nothing on the example. Results of the re-run audit are in [`adversary.md`](../proof/cmb/adversary.md).

## 11. Residual risk

Vendor tooth side is Flagged. The Computational Geometry expert's conditions are folded in but the persona has not re-reviewed this
revision. The accessibility conditions are fixed in the design and mockup; native screen-reader and focus proof is a build
condition (C1 to C8), not shown by this HTML. The accessibility minors and nits are not in this brief (§8). The plate's fixed corner
is not the placement rule.
