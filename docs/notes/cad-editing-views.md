---
id: cad-editing-views
title: Elevations edit, 3D looks, a station is a document — the CAD editing model
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [cad, camera, elevations, control-curves, station, splines]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: design-language, rel: refines}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v4, rel: relates-to}
  - {to: thick-client-shell, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2027-03-21
summary: CAD-04–06 (spec 1.2) — the four control curves are edited in the elevation that shapes them (Top · Front · Starboard), the 3D viewport is one free camera used for looking and selecting, and a station is a document tab with a full 2D section editor; every curve is a spline and the rail carries icons.
review-suggested:
  - { by: mockup-workbench-v4, on: 2026-09-20, reason: "Mockup v4 (CAD editing views) supersedes v3; spec 1.2 CAD-04–06, UX-23, UI-24–25; oracle tools/check-mockup-v4.mjs." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: defect-classes, on: 2026-09-24, reason: "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions." }
---

# Elevations edit, 3D looks, a station is a document — the CAD editing model

**Decision.** (1) **Elevations are editing views.** The lines-plan is the classic lines drawing — Top over Front
sharing the span axis, Starboard beside — and each carries the control curve it shapes as explicit `role=slider`
anchors on the geometry: Top → Outline (LE rail, TE rail = chord), Front → Dihedral/Anhedral (centre line) and Thickness (t/c on the
band edge), Starboard → Twist (a body plan, one row per station, a lever from the quarter chord each). A drag or arrow-nudge opens the *same* draft as the curve
pane (Preview → Return applies · Escape cancels, one undo item), selects that channel and station everywhere,
and every other view follows. (2) **The 3D viewport is one camera.** Named views are presets over the same
model; free orbit, pan and zoom by pointer follow the navigation preset's contract (Workbench Alt+drag / Rhino
RMB) and by keyboard; a view cube shows the camera and its faces are the presets; sections are selectable in 3D.
In this iteration 3D is a *looking and selecting* view: direct 3D handle dragging is deferred. (3) **A station is
a document.** Edit section opens a Station tab in the editor group with a full 2D section editor (grid, chord
dimension, control points, catalog ghost, comb), its palette on the toolbar and its Properties; Return applies as
a Modified Profile revision, Escape closes. (4) **Every curve is a spline** (Catmull–Rom through the evaluated
samples; the control polygon only on demand) and (5) **the rail carries icons with names**, never numerals.

**Why.** The operator's brief named the little things a CAD user reaches for first — icons, splines, a viewpoint
that rotates freely, a station editor that is a 2D CAD view rather than a modal, top/side/isometric elevations
for curve editing, and the dihedral/anhedral, thickness and twist curves as explicit control curves. Rhino edits
in ortho views and looks in perspective; Fusion enters a sketch as a mode with its own toolbar and a Finish;
Shape3d draws the outline, rocker and thickness as control curves on the elevation they shape. Each decision is
one of those idioms, adapted.

**Rejected alternatives.** *Direct 3D handle drag* — without a gizmo a 3D drag has no unambiguous plane, so a
pointer edit would guess which channel it moved; deferred, and named as such in the spec (CAD-06). *Thickness in the side view* — the IA lens showed that a spanwise distribution collapses to one point in a chord × z
view; thickness reads with dihedral on the Front axis, and Starboard became a body plan (one row per station) so the
twist handles never stack (measured: ≥ 32 px apart at 1440 × 900). *Keeping the station editor as a
dialog with a bigger canvas* — a modal blocks the shell (no Properties, no Catalog beside it, no camera) and is not
how any CAD program edits a profile. *Numbered rail with icons added* — the numbers were the flow order; the
readiness badge and the accessible name carry the order, the icons carry recognition.

**Constraints discovered.** The half-span elevation (root at the left) doubles the editing scale; the full-span
plan belongs to the 3D camera. Analysis layers had to be projected into 3D or the toggle would lose them in any
camera but Top; the free-surface plane at true h_ref leaves the view, so a tip → surface dimension that stays in frame carries
h_tip and h_ref (a plane drawn at a false height was a rendered untruth, caught by the accessibility lens). The section palette on the toolbar only fits when the mode labels are short and the echo is visually
hidden (still present for `aria-describedby`); the status bar carries the draft line. A control handle's drag
scale is the elevation's own y-scale (metres per pixel in Top/Front, degrees per pixel for twist, chord fraction
for t/c), so the same `setPreview` serves all four curves. The v3 view functions and the dialog were deleted, not
overridden (HYG-A).

**Control.** `tools/check-mockup-v4.mjs` group 13 (icons without numerals, no polylines, handles for all five
channels, keyboard and pointer edits applied as revisions, keyboard and pointer orbit in both presets, view cube,
3D selection, the station document's open/apply/escape/focus contract); the spec's CAD-04–06 / UX-23 / UI-24–25
name it as their oracle.
