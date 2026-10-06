---
id: mockup-group-move-node-m
title: Group move and typed value — several selected points as one gesture, three typed-value variants side by side
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, planform, group-move, typed-value, node-m, oi-3, ruling-96, operator-show]
links:
  - {to: design-group-move-node-m, rel: documents}
  - {to: design-next-cad-increment, rel: relates-to}
  - {to: mockup-cad-limits-in-gesture, rel: relates-to}
  - {to: design-m12b-points, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
review-by: 2026-12-31
summary: >-
  Five screens for the operator's approval before any build: a group drag to the Ruling 96 tip limit with three ways to
  hold (whole group, limited point only, today); the typed value for several points as set-all, move-by or both per row;
  typed refusals with a Use action; a ten-press keyboard run; and the hard states (mixed types, two curves, locked point,
  locked axis, orphan handle, neighbour hold, Escape, Analysis, legacy file, one point). Every number is computed in the
  page from the rule max(5 mm, 2 % of root); positions are scripted, not captured from the product.
---

# Group move and typed value

Open [`group-move-node-m.html`](group-move-node-m.html) over `file://`. The buttons top right switch the theme and the
width (560 px). The strip under the title is the in-artifact audit: text contrast, target size, NaN and placeholder scan,
text size, and a sweep of the hold rule (variant A never leaves the tip under the minimum and keeps the gaps between the
selected points; variant B breaks them; the most the example group can move toward the leading edge is 25 mm).
Proposal and decision requests: [`docs/design/group-move-node-m.md`](../design/group-move-node-m.md).

## Direction brief

- **Who and state on arrival:** the one foil designer, mid-edit, with two or three outline points selected, wanting to move
  them together or give them one value.
- **Job:** change several points in one step, keep the shape between them, and know what limits the move.
- **Archetype:** unchanged (G1 Parametric Modeling Workbench); this extends one gesture and one Properties group.
- **Choices shown:** DR-GM-3 (screen 1, A whole group, B limited point only, C today) and DR-GM-2 (screen 2, A set-all,
  B move-by, C both per row). The other decision requests are text in the design note.
- **Not shown:** the 3D view and the other viewports (they follow the draft as for one point); the Wing estimates beyond
  the tip chord (they update live and are not modelled here).
