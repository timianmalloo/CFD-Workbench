---
id: mockup-workbench-v5
title: CFD-Workbench interactive design mockup v5 — control-vertex splines, four viewports, a tool palette
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, cad, control-vertex, splines, levers, viewports, palette, cage, v5]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v4, rel: supersedes}
  - {to: review-ui-workbench-v5, rel: relates-to}
  - {to: control-vertex-workspace, rel: relates-to}
  - {to: cad-editing-views, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-21
summary: >-
  The v4 shell and camera with the CAD experience rebuilt around specification 1.3's control-vertex record: every
  master curve is a clamped B-spline whose vertices and levers are the editing surface (a vertex pulls the curve and
  never lies on it), a four-viewport lines-drawing workspace with title menus and maximise, a nine-verb tool
  palette with an options strip, a display cage for the 3D body, and a station document whose conversion residual
  is measured. Illustrative throughout; no kernel, solver, file I/O or model call.
---

# Interactive workbench mockup v5 — control-vertex splines, four viewports, a tool palette

[Open the mockup](workbench-v5.html) · [Specification v1.3](../specs/cfd-workbench-v1.html) · [Design language](../../DESIGN.md) · [Review](../reviews/ui-workbench-v5.md) · [Direction brief](../design/workbench-direction.md) · [Decision note](../notes/control-vertex-workspace.md) · [Browser evidence](../proof/workbench-v5-browser-check.json) · [Craft gate](../proof/ui-craft-findings-v5.json) · [Mockup v4 (superseded)](workbench-v4.md)

Open the HTML directly in a browser; no build, server, key, solver, internet or external font is needed, and the
page makes zero external requests. The review bar at the top is review chrome and never ships. Everything the v3
and v4 hubs describe (the shell, docks, sashes, drawers, the camera, the station document) still holds; this page
lists what v5 changes.

## What v5 changes

- **The vertices are the record.** Each master curve — LE rail, TE rail (chord), Dihedral/Anhedral, Twist,
  Thickness t/c — is a degree-3 clamped B-spline with seven control vertices (six to ten allowed). The selected
  curve shows its **control frame** in the elevation that shapes it: a dashed polygon, square interior vertices,
  circle **levers** (the second and penultimate vertices set the end tangents), diamond end vertices on the curve.
  A vertex *pulls* the curve and never lies on it; the oracle measures the gap and the local support. The
  "through points" anchors, Smooth mode and influence weights of v4 are gone; the seed anchors remain as
  provenance for **Fit points**.
- **Levers and locks.** The root vertex and its lever are coupled by the root-mirror lock (the first leg stays
  horizontal: G1 across the mirror plane; either moves, both are named as coupled); the tip vertex is pinned by the
  value-at-tip lock and refuses with its lock named and a lock ring. ↑↓ nudge the value, ←→ move the vertex along η
  (an η ladder), Shift ×10 everywhere, Delete removes (the measured shape change reported; floor six), Return applies
  and keeps focus, Escape cancels and returns to Select. Properties takes a value with a unit suffix (mm · m · in ·
  ° · % · chord) and echoes the resolved model value.
- **Four viewports.** Top · Perspective over Front · Starboard — the lines drawing. Each title bar is a button
  (double-click or Return maximises) and a menu: View (Top · Front · Starboard · Perspective · η-plot), Display
  (Control frame · Curvature comb · Ghost), Body (Smooth · Box · Cage over smooth), Maximise / restore. The η-plot
  is a view like any other and edits the same vertices; the 1.2 curve pane is gone. Every viewport draws at its own
  pixel size and re-renders when its box changes; below 480 × 240 px the workspace shows one viewport.
- **A tool palette and an options strip.** Select · Insert CV · Add station · Measure · Fair · Rebuild · Fit
  points · Edit section · Ghost, icons *with* names, single keys (S · I · A · M · ⇧F · R · P · Return · G) that act
  only while the workspace has focus; at the 640 × 400 reflow preset the palette is a row above the viewports.
  The row beneath the toolbar is the options strip: the curve selector first, then every pointer verb's keyboard
  equivalent and options (Insert at η; η for Add station; Measure between two η values; tolerance and
  PreserveEnds position · tangency · curvature for Fair; vertex count 6–10 for Rebuild; the comb scale and the
  monotone-piece count while the comb is shown), the draft chip ("Draft open — <kind> · deviation <d>", or the Fair
  above-tolerance string), the derived b · S · AR · c̄ · TE readouts. The Tracing strip is a pointer probe: moving
  over any elevation reads η, % half-span, the channels and the active curve's graph curvature κ and radius. The toolbar keeps Edit · Find · View and grows Draft
  (Apply · Cancel) only while a draft is open. The nudge step, the locks and Remove station live in Properties.
  The bottom panel is the Checks drawer in CAD; the Navigator and the panel start collapsed on first entry.
  Visible chrome in CAD at 1280 × 800 on entry: **45** (v4: 71).
- **Constructions, one rule.** Fair (weighted least squares within a tolerance; Apply disabled above it), Rebuild
  (a chosen vertex count, deviation reported), Fit points (the curve refit through its anchors, residual reported),
  Insert CV and Delete CV each open the one draft; every construction, lock change and curve pick refuses while a
  draft is open, naming the draft to apply or cancel. Add station (by η in the strip or a click in an elevation)
  and Remove station are undo items.
- **The 3D body.** Body: Smooth (the loft), Box (the display cage — every station's section polygon and the LE
  and TE rail polygons as a named group; the dihedral, twist and t/c polygons stay in their 2D lanes) or Cage over
  smooth. It is a NURBS loft with a cage, never a T-spline (specification A4.12); 3D vertex dragging stays
  deferred, and a double-click on a rail polygon in the cage maximises the elevation that edits it. The Starboard
  body plan and the Starboard camera agree on handedness (nose to the right), positive twist is nose-up about the
  station LE in every view, and Z zooms out while ⇧Z zooms in (the recorded per-OS table).
- **The station document.** The section's own vertices and levers (degree 5; the conversion chooses the smallest
  vertex count, 8–13 here, whose residual meets the 10 µm acceptance — centripetal parameters, knots by
  averaging); the residual is **measured** at the catalog points and shown identically in the HUD, the strip and
  Properties. Add profile from DAT lives on the Catalog tab.

**Mockup-only simplifications (labelled in the artifact):** the curvature comb and the tangent rows use finite
differences (the comb is wrong at the two ends; the product uses analytic derivatives, A4.9); Insert CV splits the
polygon leg rather than inserting a knot (the reported shape change is the honest number; the product's Boehm
insertion is exact); the knot vector is keyed by vertex count rather than stored; the Measure tool reports Δη and
Δvalue in one lane; there is no context menu or trackpad orbit; the pointer paths are mouse events only.

## Executable control

`node tools/check-mockup-v5.mjs [<node_modules dir with playwright>]` runs the v3/v4 shell and area groups
re-targeted to v5 plus two new groups. **Group 6 (the record):** degree, count and knot form; only the active
curve carries handles; vertices as named sliders with levers and `aria-valuetext`; ten nudges move a vertex by the
step and the curve by a basis fraction *without* passing through it (the gap is measured); local support (an
off-centre vertex leaves η 0.10 untouched to 10⁻¹² and moves η 0.90); Shift+→ moves η; the root mirror couples
vertices 1 and 2; the tip pin refuses; Delete reports its measured shape change; a pointer drag lengthens the chord
and Return applies a revision; Insert by click, Fair gated by tolerance (Return refuses above it), Rebuild with a
count and deviation, Fit points with a residual, Rebuild and a lock change refusing under a draft, Ghost, Add
station by η and by a click (undone by ⌘Z), Remove station from Properties (undone by ⌘Z), Measure by two picks; the
options strip and a click on a curve select it; one draft at a time; the station document with its measured
residual agreeing across three surfaces; the lens rows — the root vertex moves with its lever (no phantom lock),
←→ nudge η at the ladder, Return applies and keeps focus, Enter on a palette tool keeps focus, Insert at η and
Measure between two η by keyboard, PreserveEnds chosen and reported (curvature falls back with an honest label on
seven vertices), the Fair-above-tolerance chip string, a typed "0.1 m" resolving to 0.1 in the model, Rebuild
never below six. **Group 13 (the workspace):** four named viewports, maximise by
double-click and Return, title menus (any view incl. the η-plot with the same vertices; comb; box cage as a named
group), the camera (free Alt-drag orbit with the title reporting the free camera, wheel, cube faces and chevrons,
Rhino RMB, keyboard pan, sliver-free faces), a focused vertex ring ≥ 3:1 in three themes, Shift+arrow on a vertex
never panning, 3D sections as pressed buttons, Return opening the selected station, the station tab surviving
behind the design document and closing with its station, the UX-15 status strip, the forced single viewport at
the reflow preset with the palette as a row of nine 44 px tools, the quiet first entry, the title menu's keys
(open focuses the first item, arrows, End, Escape back to the button, a choice returns focus), Enter on a cage
polygon keeping focus, the cage holding only section and rail polygons, the Starboard handedness and the twist
sign in both views, Z out and ⇧Z in, the comb's monotone-piece count and scale stepper, pointer Tracing with κ,
and the chrome count on entry (45) and mid-session. Evidence lands in
`docs/proof/workbench-v5-browser-check.json`; `ui-craft-gate.py` writes `docs/proof/ui-craft-findings-v5.json`
(fourteen Minors, dispositions in the review) and `design-lint.py --strict` runs beside it.

Best first review: CAD → drag a square on the TE rail in Top and watch the curve follow *without* reaching it →
drag a circle lever near the tip → press Return → double-click "Front" to maximise, click the dihedral curve, nudge
with the arrows, Escape → open the Perspective menu, choose Body › Box → press ⇧F for a Fair draft and lower the
tolerance until Apply disables → select η 0.60 in the Navigator or in 3D and press Return to open the station
document → nudge an upper vertex, read the residual, Escape.
