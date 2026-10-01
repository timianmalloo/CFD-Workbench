---
id: mockup-property-grid
title: CFD-Workbench — the Properties pane as a property grid
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, properties, property-grid, native-ui, m1.2b, m1.2b2, wing, tangent]
links:
  - {to: mockup-workbench-v10, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: design-m12b-points, rel: relates-to}
  - {to: design-m12b2-3d-elevations, rel: relates-to}
  - {to: review-m12b-native, rel: relates-to}
  - {to: review-ui-property-grid, rel: tested-by}
review-by: 2026-12-30
summary: >-
  F-1 from the M1.2b native review, elevated: Properties becomes one reusable property grid — a selection identity,
  collapsible groups and label | value | unit rows on a shared column — in every selection state (foil, control,
  anchor with a labelled Tangent group, handle with its anchor's tangent, named points, several points, station) and
  every hard state, with the Wing block pinned at the foot and an honest Unavailable state in place of "≈ —".
review-suggested: []
---

# Properties as a property grid

Open [`property-grid.html`](property-grid.html) over `file://`. The review bar switches persona (designer, keyboard
only, screen reader with the computed accessible names, reviewer with column guides), pane width (200 · 260 · 300 px),
window (1280 × 800 · 1440 × 900), state, theme (light · dark · high contrast), reduced motion, and **As built beside**,
which draws the c43711a pane next to the proposal. The table under the pane re-measures the state you are looking at.

**What it is.** One component in three parts, so M1.2b2's PNL track (`docs/design/m12b2-3d-elevations.md` §14) can
fill it from `PropertiesView.Build` without new layout:

- **Selection identity** — the Plan view's glyph, the object's name, and a crumb only when it adds something (a
  handle's parent anchor, as a link).
- **Property group** — a header band with a chevron and, when collapsed, a summary of its values (Premiere's
  twirl-down). Collapse is remembered per group. The Wing group is never collapsible and carries a state chip.
- **Property row** — label | value | unit on one shared column (VS Code settings rows, Premiere's aligned label
  column), a 3 px state rail, one description and one message line. Editable rows look editable; facts are plain text.

**States rendered (22):** empty · opening · pane error · foil · control point · anchor point (Tangent Smooth,
Symmetric, Corner) · handle (parent tangent, F-4) · LE root end (fixed) · TE root end (aft only) · three points
(Mixed, OI-3) · station · during a drag (≈ preview) · checking · typed chord above the fit limit (warning) · field
error · estimates unavailable · tip closes · section editor (Wing read-only) · foil not checked · recovered edit ·
overflow · a Twist channel point (M1.2b2 reuse).

**Evidence.** `tools/check-mockup-property-grid.mjs` sweeps 396 state × theme × width × window cells and 13
interaction paths; the result is `docs/proof/property-grid-browser-check.json`. The craft gate reports 4 Minor
findings, all on the window-frame chrome (see the review). The review, the ranked plan and the implementation brief
are in [`docs/reviews/ui-property-grid.md`](../reviews/ui-property-grid.md). Every number is Illustrative; the WCAG
2.2 AA veto is pending an independent UX & Accessibility review.
