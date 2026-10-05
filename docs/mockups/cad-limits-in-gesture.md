---
id: mockup-cad-limits-in-gesture
title: CAD limits in the gesture — a planform drag at the minimum tip chord, three variants side by side
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, planform, tip-chord, gesture, clamp, ruling-93, ruling-94, operator-show]
links:
  - {to: design-next-cad-increment, rel: documents}
  - {to: design-m12b-points, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
review-by: 2026-12-31
summary: >-
  Four screens for the operator's approval before any build: the tip-vertex drag at the Ruling 93 limit as today (free,
  refused at release), A hold at the limit, and B land at the limit on release, side by side with the Wing block and
  status strip; the root-chord refusal copy before and after; a keyboard run of ten presses; and the hard states
  (legacy file, closing tip, empty, mixed, Escape, Analysis). Every number is computed in the page from the rule
  max(5 mm, 2 % of root); positions are scripted, not captured from the product.
---

# CAD limits in the gesture

Open [`cad-limits-in-gesture.html`](cad-limits-in-gesture.html) over `file://`. The buttons top right switch the theme and
the width (560 px). The strip under the title is the in-artifact audit (text contrast, target size, NaN and placeholder scan,
text size, and a sweep of the rule: variants A and B never produce a tip under the minimum; root maximum for a 6 mm tip is
300 mm). Proposal and decision requests: [`docs/design/next-cad-increment.md`](../design/next-cad-increment.md).

## Direction brief

- **Who and state on arrival:** the one foil designer, mid-edit, dragging the outline toward a finer tip.
- **Job:** shape the tip without losing the gesture, and know what limits it and what lifts the limit.
- **Archetype:** unchanged (G1 Parametric Modeling Workbench); this is a feedback rule inside an existing gesture.
- **Choice shown:** DR-LIM-1 (A hold, B land on release, C today), DR-LIM-2 (root holds too), copy COPY-B to COPY-F.
