---
id: mockup-planform-point-verbs
title: Planform point verbs — Add point, Remove point and Rebuild to N on the outline, in today's shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, planform, rail, point-verbs, rebuild, ruling-62, operator-show]
links:
  - {to: design-planform-point-verbs, rel: documents}
  - {to: mockup-m12b2-views, rel: refines}
  - {to: mockup-m12c-section-editor, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: rulings, rel: implements}
review-by: 2026-12-31
summary: >-
  Ruling 62 asked for fewer points on the planform outline, with Add point, Remove point and Rebuild to N. This page
  draws six screens of today's 1280 × 800 shell on the New foil: where the verbs live (Edit menu and context menus), the
  Rebuild preview from 10 to 4 points with the measured largest change, the rebuilt curve, Add point by double-click,
  Remove point with ⌫, and the refusal at the floor of 4. The rails and every number are computed in the page with the
  design's algorithms.
review-suggested: []
---

# Planform point verbs — the mockup

Open [`planform-point-verbs.html`](planform-point-verbs.html) over `file://`. The State list at the top right jumps to
any screen; the button beside it switches the chrome to dark. The viewports stay graphite in every theme (M1.2b D-4).
Captures: `docs/proof/planform-point-verbs/{light,dark}-{menus,preview,applied,add,remove,refused}.png`, and the
measurements in `measure.json` beside them.

**Why a new page, not an extension of `m12b2-views.html`:** that page is the operator-approved record of the M1.2b2
views. These screens need a different story (the New foil, one view, a popover and menus), and changing that page
would blur what was approved. The tokens, the shell, the structure-B Properties, the V2 status strip and the Point
glyphs are copied from it and from `m12c-section-editor.html`.

## The screens

| Screen | Shows | Implements |
|---|---|---|
| 1 · Where the verbs live | New foil, trailing-edge point 6 of 10 selected. The macOS Edit menu with Add point… (asks for From root in mm) · Remove point ⌫ · Rebuild trailing edge… above the point-type rows. Below: the point context menu, the same menu at the floor (Remove Point disabled with "A curve needs at least 4 points."), and the outline context menu (Add Point Here) | design §11.1 (1), §11.3, §5.2; CAD-21 |
| 2 · Rebuild preview | the popover below the drawing, Points = 4; the rebuilt trailing edge dashed in the station colour with its four points; a warning tick and plate at the largest change; readouts: points 10 → 4, largest change and where, curvature breaks 0 → 0, what is kept exactly (root end, its square tangent, tip end), the tip direction's turn, anchors | §6.4 Rebuild, §11.2, COPY-199 |
| 3 · Rebuild applied | the 4-point trailing edge; the selected point no longer exists, so the point selection clears (Marine-CAD at the gate); Properties shows the foil with the Trailing edge group; the Wing area recomputed | §3.5, COPY-200 |
| 4 · Add point | a double-click on the 4-point trailing edge at 250 mm from root: point 3 of 5 added and selected; the strip reports the measured change | §6.4 Add, §11.3, COPY-190 |
| 5 · Remove point | ⌫ on leading-edge point 6 of 10: 9 points, the measured change and where; the next point toward the tip selected | §6.4 Remove, §3.5, COPY-191 |
| 6 · Refused at the floor | ⌫ on point 3 of the 4-point trailing edge: the warning strip names the floor; a dashed warning ring; nothing changed | §3.4 order, COPY-192, §11.5 |

The layout is One view (Plan) with Fit Selection on the trailing edge, so the starboard half fills the view and a
change of a few millimetres is visible (1.81 px per mm).

## How the geometry is made

- **The New foil rails** are rebuilt the way `FoilSource.NewDefault` builds them: 10 points, degree 3, interior knots
  1 − (1 − i/7)³ (tip-clustered), Greville abscissae, least squares to the unit chord √(1 − 0.99 η²) with the quarter
  chord straight, the end pins (root on the centre line with a square tangent, tip position), scaled so the area is
  0.1 m² (root chord 126.74 mm, tip chord 12.67 mm).
- **Rebuild, Add and Remove** run the design's algorithms (§6.4): knots that follow the current spacing, abscissae from
  the old curve at the new Greville parameters, least squares with pins (a KKT system); Boehm insertion; one knot
  removed and two ordinates refitted with every other point held.
- **Every number** printed is measured on the A4.5 oracle (201 uniform η plus every interior knot of both curves).
  Curvature breaks come from analytic derivatives (derivative control points), never finite differences.

## Measured (Playwright, system Chrome, `capture.mjs` in the session scratchpad; `measure.json`)

| Quantity | Value |
|---|---|
| New foil root chord · tip chord · root square | 126.738 mm · 12.674 mm · true on both rails |
| Rebuild trailing edge 10 → 4 (screen 2) | largest change 8.972 mm at 467.5 mm from root; knots [0,0,0,0,1,1,1,1]; ordinates 126.738, 126.738, 131.354, 41.190 mm; root kept exactly, root square kept, tip kept |
| Rebuild trailing edge, current spacing (default) · even spacing | 4: 8.97 · 8.97 — 5: 1.36 · 5.61 — 6: 0.68 · 3.93 — 7: 0.32 · 3.03 — 8: 0.15 · 2.44 — 9: 0.10 · 2.06 — 10: 0.000 · 1.75 mm |
| Rebuild leading edge, current spacing | 4: 2.99 — 5: 0.45 — 6: 0.23 — 7: 0.11 — 8: 0.05 — 9: 0.03 — 10: 0.000 mm |
| Curvature breaks before → after (10 → 4) · tip direction turn | 0 → 0 · 35.03° (the tip tangent is not kept) |
| Ids after Rebuild to 4 | `cv-0`, `cv-10`, `cv-11`, `cv-9` (ends kept, handles fresh) |
| Add point at 250 mm on the 4-point trailing edge | 5 points; new id `cv-12` at index 2; points 2 and 4 moved; change 7.1 × 10⁻¹⁴ mm |
| Remove leading-edge point 6 of 10 | 9 points; removed `cv-5`; change 0.2596 mm at 480 mm; selected `cv-6` |
| Wing area after Rebuild / after Remove | 998.2 cm² / 998.1 cm² (1000.0 before) |
| As-built display sampling of a 4-point curve (finding F-1) | 8 samples, first at η 0.0625: 31.25 mm short of the root |
| Shells · smallest text · console errors · NaN/undefined/null leaks | six at 1280 × 800 · 11 px · none · none |

**UI craft gate** (`ui-craft-gate.py`): one Minor, `side-tab` on `.sb .msg.warn`. This is a **recorded deviation**:
DESIGN.md §4 *Status strip* requires the 3 px warning rail (DR-STATUS-1 V2), as on the M1.2c page.

## Not shown, and why

- **Interaction.** This is a static show page, like the M1.2b2 and M1.2c pages. Double-click, ⌫, the stepper, Return
  and Escape are specified in the design (§11.3) and proven by the build's tests (§12.4).
- **Plan + 3D and the elevations.** The verbs behave the same in the Front band, the t/c lane and the twist lane
  (DR-PV-4 A). One view of the Plan shows the change at a readable scale.
- **Windows.** The menus are in the window there (app-shell); the rows and their order are the same.
