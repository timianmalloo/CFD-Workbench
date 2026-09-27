---
id: mockup-workbench-v10
title: CFD-Workbench v10 — first run, focus-safe floats, point types, catalog and a Wing block
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, cad, docking, properties, catalog, point-types, estimates, first-run]
links:
  - {to: mockup-workbench-v9, rel: supersedes}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: review-ui-workbench-v10, rel: tested-by}
review-by: 2026-12-26
summary: >-
  v9's docked-pane CAD workspace, elevated: first-run, opening and open-failed states inside the workspace; floats
  move clear of any focused target in the model area; per-point Anchor / Control type; a Wing block with typed span
  and chords above running estimates; and, in the section editor, Replace from catalog and Save to My sections.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-26, reason: "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims." }
---

# CFD-Workbench v10

Open [`workbench-v10.html`](workbench-v10.html) over `file://`. The review bar switches platform (macOS / Windows),
screen, layout, selection, state, theme and window size; the audit under the window measures contrast and target
size live.

- **First run:** with no foil open the model area shows *Start a foil* (New foil · New from example · Open… · Recent).
  Recent → *Opening …* with Cancel → the workspace. `old-kite-wing.foil` shows the open-failed state.
- **Floats never hide focus:** a pane floating over the model moves to the nearest clear corner when a point,
  station chip or control under it takes focus, and announces the move.
- **Properties:** readable at the 200 px dock; one precision per quantity; a **Type** row (Anchor point / Control
  point; named end points locked); tangent Smooth / Symmetric / Corner; always ends with the **Wing** block — Span,
  Root chord, Tip chord as typed dimensions, then estimates (mean chord S/b, MAC, max t/c, aspect ratio, area).
- **Section editor:** **Section ▾** → *Replace from catalog…* (search, families NACA / Eppler / Speer / My sections,
  pending sections disabled with their reason, dashed preview, undoable), *Save to My sections…* (name and
  provenance), *Smooth…*.
- **Browser** is an outline of the points and sections in the current view; **Undo / Redo** work (⌘Z / ⇧⌘Z,
  Ctrl+Z / Ctrl+Y).

Review record: [`docs/reviews/ui-workbench-v10.md`](../reviews/ui-workbench-v10.md).
