---
id: mockup-workbench-v4
title: CFD-Workbench interactive design mockup v4 — CAD editing views
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, cad, camera, elevations, control-curves, station, splines, v4]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v3, rel: supersedes}
  - {to: review-ui-workbench-v4, rel: relates-to}
  - {to: cad-editing-views, rel: relates-to}
  - {to: thick-client-shell, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-21
summary: >-
  The v3 thick-client shell with the CAD editing views of specification 1.2: an icon rail, splines everywhere, one
  camera with named views, a view cube and free orbit, editing elevations (Top over Front, Starboard beside) where the
  outline rails, dihedral/anhedral, twist and thickness are explicit control curves, and a Station document that
  replaces the modal section editor. Illustrative throughout; no kernel, solver, file I/O or model call.
---

# Interactive workbench mockup v4 — CAD editing views

[Open the mockup](workbench-v4.html) · [Specification v1.2](../specs/cfd-workbench-v1.html) · [Design language](../../DESIGN.md) · [Review](../reviews/ui-workbench-v4.md) · [Direction brief](../design/workbench-direction.md) · [Decision note](../notes/cad-editing-views.md) · [Browser evidence](../proof/workbench-v4-browser-check.json) · [Mockup v3 (superseded)](workbench-v3.md)

Open the HTML directly in a browser; no build, server, key, solver, internet or external font is needed, and the
page makes zero external requests. The review bar at the top is review chrome and never ships. Everything the v3
hub describes (the shell, the docks, sashes, drawers, the per-area arrangements) still holds; this page lists what
v4 adds.

## What v4 adds

- **Icons on the rail.** An inline glyph per area with the name beneath it; the accessible name still carries the
  readiness string ("CAD — r4 · 3 stations"); Checks shows its count. No numerals.
- **Splines.** Every curve on screen is a spline path — rails, sections, control curves, the section editor, the
  fitted curves in charts. The control polygon appears only in Smooth mode.
- **One camera.** Top · Front · Starboard · Iso (and Bottom · Back · Port) are presets over the same model, from the
  document-tab row, the **view cube** in the viewport corner (faces with area only, four orbit chevrons, current face filled) and the keyboard
  (Option/Alt+←→ 15° · ⇧ 90°; Option/Alt+↑↓ 15° · ⇧ 45°; `[` `]` 5°; ⇧+arrows pan; Z/⇧Z zoom; F fit; Home = Iso).
  In CAD the three orthographic presets *are* the editing elevations. Free orbit, pan
  and zoom by pointer follow the navigation preset: Workbench — Alt+drag orbit, Shift+drag pan, wheel zoom;
  Rhino — RMB orbit, MMB pan, wheel zoom. The 3D view draws the sections at every station and slice, both rails,
  a translucent skin and the selected control curve with its anchors; a section is selectable in 3D and becomes
  the selection everywhere. In Analysis the layers are projected into any camera.
- **Editing elevations.** Lines-plan is now the classic lines drawing — **Top over Front sharing the span axis,
  Starboard beside them** — and each is an editing view: Top carries the Outline's LE and TE rail control points,
  Front the Dihedral/Anhedral curve (centre line) and the Thickness (t/c) curve (band edge), Starboard is a body
  plan — one row per station — with the Twist handle (a lever from the quarter chord) on each. Dragging or arrow-nudging a point opens the same Preview → Apply (Return) / Cancel (Escape) draft as
  the curve pane, selects that channel and station everywhere, and the other views follow.
- **The Station document.** Edit section opens a tab beside the design — a full 2D section editor with grid,
  chord dimension, control points, the catalog original ghosted and the comb; the section palette (mode · step ·
  value) sits on the toolbar with Apply/Cancel, the section's rows in Properties, Catalog and Checks below.
  Return applies as a Modified Profile revision (one undo item); Escape, Delete on the tab or the tab's × discards
  the draft onto the undo stack and returns focus to Edit section; Edit section is disabled while a geometry draft
  is open (one draft at a time). The dialog is gone.

**Deferred, with the risk named:** direct 3D handle dragging (a 3D drag has no unambiguous plane without a
gizmo); 3D is a looking and selecting view in this iteration.

**Mockup-only simplifications:** the twist drag maps a fixed 0.25° per pixel (the product should take the angle
from the quarter-chord pivot with `atan2`); the context menu and trackpad orbit of the B7 contract are not rendered;
the pointer paths are mouse events only (touch and pen are outside the declared desktop medium).

## Executable control

`node tools/check-mockup-v4.mjs [<node_modules dir with playwright>]` runs the v3 shell contract and interaction
groups re-targeted to v4 (the station editor is asserted as a document: no open dialog, the tab, the palette on
the toolbar, first control focused, Escape returns focus, Return applies a revision) plus group 13, the CAD views:
rail icons and no numerals; no polylines in the viewport or elevations; Top/Front/Starboard elevations with handles for
all five channels; keyboard nudges on the Front (dihedral, thickness) and Starboard (twist) handles opening previews
on the right channel, and a handle of another curve refusing while a draft is open; twist handles measured ≥ 32 px
apart; 3D sections as pressed buttons; the station draft discarding to the undo stack and its tab closing by Escape,
Delete or the sibling close button; keyboard pan; cube faces and chevrons ≥ 20 px at every named view; a focused
SVG handle drawing its ring; a pointer drag on the TE rail lengthening the chord and Return applying it as a revision; Alt-drag
orbit to a free camera, wheel zoom, view-cube and tab presets, RMB orbit in the Rhino preset; a section selected
from 3D; analysis layers present in the 3D view. Evidence lands in `docs/proof/workbench-v4-browser-check.json`;
`ui-craft-gate.py` (`docs/proof/ui-craft-findings-v4.json`) and `design-lint.py --strict` run beside it.

Best first review: CAD → drag the viewport with Alt held, click a cube face, press Home → Lines-plan (1440 × 900
preset) → drag a TE rail point in Top and watch Front, Starboard and the η-plot follow → press Return → select η 0.60
and Edit section → nudge an upper control, read the chord dimension, press Escape → Analysis → orbit and watch the
vectors and the tip → surface dimension follow the camera.
