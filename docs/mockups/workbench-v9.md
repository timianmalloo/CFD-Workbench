---
id: mockup-workbench-v9
title: CFD-Workbench v9 — docked panes and a Properties pane
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, cad, docking, properties, window-management, workspaces]
links:
  - {to: mockup-workbench-v8, rel: supersedes}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: review-ui-workbench-v9, rel: tested-by}
review-by: 2026-12-26
summary: >-
  v8's CAD surface with VS Code / Premiere Pro window management: one narrow left panel holds a selection-driven
  Properties pane by default; the right side bar and bottom panel are optional; panes tab, dock, float and maximize;
  workspaces are task presets. Direction evidence only — native floats and menus are build requirements.
---

# CFD-Workbench v9 — docked panes

Open [`workbench-v9.html`](workbench-v9.html) over `file://`. The review bar switches platform (macOS / Windows), screen,
layout, selection, state, theme and window size; the audit under the window measures contrast and target size live.

- **Default (Planform):** left panel with **Properties** | Browser; Plan large with 3D beside it; right and bottom closed.
- **Properties follows the selection:** point (position, tangent, handles, constraints only when locked), station (section,
  sharing, t/c, Edit section), nothing (foil metrics), several points (Mixed / "—").
- **Window management:** drag a tab onto a drop zone or ⋯ → Move / Float / Size / Maximize / Close; floats dock back with
  ⤓ or Escape; ⌘B / ⌘J / ⌥⌘B (Ctrl on Windows); F6 cycles regions; Window menu → workspaces (Planform, Precision, Review),
  panes and Reset layout. Each workspace remembers its own layout.

Review record: [`docs/reviews/ui-workbench-v9.md`](../reviews/ui-workbench-v9.md).
