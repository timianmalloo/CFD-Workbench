---
id: mockup-property-grid-cells
title: Property sheet — cell layouts for the operator to pick (Visual Studio Properties window first)
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, properties, property-grid, cells, operator-pick]
links:
  - {to: mockup-property-grid, rel: refines}
  - {to: review-ui-property-grid-density, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  The operator rejected the row-and-box structure ("it needs to look more like cells and labels") and then picked the
  Visual Studio Properties window. The page shows the same anchor point and Wing content at the real 260 px pane,
  11 px text. Primary: a faithful VS Properties grid. Secondary: Premiere Effect Controls and a VS Code compact table
  for comparison. Nothing is built from it yet.
review-suggested: []
---

# Property sheet — cell layouts

Open [`property-grid-cells.html`](property-grid-cells.html) over `file://`; the button at the top right switches to the
dark theme. Captures: [`variants.png`](../proof/property-grid-cells/variants.png) (light) and
[`variants-dark.png`](../proof/property-grid-cells/variants-dark.png).

**The pick (DR-CELL-1):** the operator chose **B**, "as is". B is now the structure of
[`property-grid.html`](property-grid.html), with its review and brief in
[`ui-property-grid-cells.md`](../reviews/ui-property-grid-cells.md). This page stays as the record of the choice.

**A — Visual Studio Properties window / WinForms PropertyGrid (shown first after the operator's earlier note).**
- **Toolbar:** Categorized and Alphabetical toggles.
- **Categories:** each is a row with ▾/▸ spanning both columns.
- **Properties:** each is a label cell and a value cell with hairline grid lines both ways.
- **Splitter:** draggable between the columns (also ← / → when focused).
- **Value cell:** it is the in-place editor; enums are in-cell ▾ dropdowns; read-only values are greyed; ≈ estimates
  carry their units.
- **Description panel:** at the bottom, it shows the selected property's name and help text. That is where reasons,
  notes and help can live instead of inline rows.
- **Wing:** pinned below the scrolling categories.

**B — Premiere Pro Effect Controls (secondary).** Twirl groups and indented rows; accent-coloured value text,
right-aligned, with no boxes until clicked.

**C — VS Code compact table (secondary).** A Property | Value header, striped rows, a column divider and tree-node
categories.

Each variant also renders the edit state: Aft focused with the caret after 118.52.

**Floors:**
- **Text:** 11 px, nothing below 11.
- **Editable rows and toggles:** 24 px. Read-only rows: 20 px.
- **Focus:** visible.

A capture-time check on the page found no page errors, no text under 11 px, no target under 24 px and no clipped
label or value. Every number is Illustrative.
