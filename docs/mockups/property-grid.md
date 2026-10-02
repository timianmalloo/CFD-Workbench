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
  - {to: property-grid-rulings, rel: relates-to}
  - {to: review-ui-property-grid-density, rel: tested-by}
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

**States rendered (30):**
- **Nothing to show:** empty · opening · pane error.
- **Selections:**
  - foil;
  - control point;
  - anchor point (Kind Smooth, Symmetric, Corner as a vertical list);
  - handle (the parent anchor's kind, F-4);
  - LE root end (fixed);
  - TE root end (its Aft is the root chord, MC-2);
  - root-mirror handle;
  - TE tip end (aft only);
  - tip handle;
  - three points (Mixed, OI-3);
  - station.
- **Hard states:**
  - during a drag (≈ preview);
  - checking;
  - typed chord above the fit limit (warning);
  - field error;
  - estimates unavailable;
  - MAC alone unavailable;
  - tip closes;
  - section editor (Wing read-only);
  - foil not checked;
  - recovered edit;
  - overflow.
- **M1.2b2 reuse:** Twist control point · Twist anchor and handle (handles set by From root and Twist) · Twist clamped
  · Thickness point (t/c at 0.01 %).

**Repair cycle 1** (after the UX & Accessibility and Marine-CAD reviews of `2c014a6`):
- Facts and estimates speak their unit.
- The Kind list is vertical; its arrows move the check only.
- Arrows on the closed Type box are pending.
- Entry is unit-aware (° / deg / rad, %), and a `#reference` expression is set once and says so.
- A point uses "From root" with η.
- Point and handle fields nudge with ↑/↓ as the canvas gesture.
- Precision follows the quantity.

The rulings are in [`docs/notes/property-grid-rulings.md`](../notes/property-grid-rulings.md).

**Density pass (2026-10-01, after the native build).**
- The harness has **Density: Dense (proposal) / As built (native, dade554)** and a live before/after table under the
  pane.
- Dense (after repair cycle 1): one 11/14 type size for label, value, unit and messages, with nothing below 11 px;
  18 px read-only rows; 24 px input rows, each one element with a 20 px drawn field inside the 24 px target; 24 px
  headers and Kind options; the value next to its label; a focused field shows the whole expression.
- The app's **View ▸ Text size** control (100–200 %, ⌘+ / ⌘−) sits in the title bar; rows stack at 150 % and above.
- The harness has a **Text spacing (SC 1.4.12)** check.
- Review and build brief: [`docs/reviews/ui-property-grid-density.md`](../reviews/ui-property-grid-density.md).

**Evidence.**
- `tools/check-mockup-property-grid.mjs` sweeps 540 state × theme × width × window cells and 30 interaction paths. The
  result is `docs/proof/property-grid-browser-check.json`.
- Planted regressions of PG-01 (units not spoken) and PG-02 (a truncated Kind row) both turn the check red.
- The craft gate reports 4 Minor findings, all on the window-frame chrome.
- The review, the ranked plan and the implementation brief are in
  [`docs/reviews/ui-property-grid.md`](../reviews/ui-property-grid.md).
- Every number is Illustrative. The WCAG 2.2 AA veto is held by the UX & Accessibility lens pending its re-review of
  cycle 1.
