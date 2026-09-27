---
id: mockup-workbench-v8
title: CFD-Workbench v8 — CAD-first direction (curves and points)
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, cad, direction, section-editor, points, handles]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: design-section-editor, rel: refines}
  - {to: mockup-workbench-v7, rel: supersedes}
  - {to: review-ui-workbench-v8, rel: tested-by}
review-by: 2026-12-26
summary: >-
  A CAD-first rethink at the Fusion 360 / Shape3D bar: open or create a foil, edit it in model-filling views, then enter
  the section editor as a mode. Every curve is on-curve points with tangent handles; ends are named points with typed
  values. Direction evidence only — not native proof.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-26, reason: "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims." }
---

# CFD-Workbench v8 — CAD-first direction

Open [`workbench-v8.html`](workbench-v8.html) over `file://`. The review bar switches screen, state, theme and window
size; the audit under the window measures contrast and target size live.

**Journey:** Start (New foil · New from example · Open… · Recent) → Design workspace (four views, one toolbar, collapsed
browser, properties only for a selection) → select a station → **Edit section** (a mode with **Finish section** /
**Cancel**) → back to the workspace.

**Vocabulary:** everything is a curve or a point. Squares are points on a curve, circles are tangent handles, diamonds are
end points. Tangent kinds: Smooth, Corner, Horizontal, Vertical, Fixed angle. Arrows move 0.1 mm, ⌘/Ctrl 0.01 mm,
Shift 1 mm; on a handle ← → turn it and ↑ ↓ lengthen it. Right-click or Shift F10 opens the point menu.

Review record, measurements and open decisions: [`docs/reviews/ui-workbench-v8.md`](../reviews/ui-workbench-v8.md).
