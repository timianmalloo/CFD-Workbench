---
id: thick-client-shell
title: The window is the unit — a thick-client shell, not a scrolling page
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [ui, shell, thick-client, layout, toolbar]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: design-language, rel: refines}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v3, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2027-03-21
summary: The CFD-Workbench client is a fixed window whose regions scroll inside themselves — menu bar, one-row measured toolbar, parameter row, activity rail, docks, editor with document tabs and a tabbed bottom panel, status bar — with each area's content arranged for that vignette; page scroll and toolbar wrapping are defects the oracle fails.
review-suggested:
  - { by: mockup-workbench-v3, on: 2026-09-20, reason: "Mockup v3 (thick-client shell) supersedes v2 as the review artifact; shell contract proven by tools/check-mockup-v3.mjs; UI-23 and the activity rail in spec 1.1a." }
---

# The window is the unit — a thick-client shell, not a scrolling page

**Decision.** The client's layout facet is `ViewportWorkbench` in fact, not only in the archetype signature: a
fixed window (`data-window` presets 1024 × 700 minimum · 1280 × 800 · 1440 × 900 · 1600 × 1000 · 640 × 400
reflow) whose regions scroll inside themselves and whose toolbar is one 44 px row filled from the B1 verb table
and **measured** after every render, moving trailing groups into `More ▾` rather than wrapping or scrolling. The
seven areas keep their objects and verbs; what changed is *where each vignette's content lives*: the document
areas (Setup, Experiment) fill the editor; the viewport areas (CAD, Analysis, Results) put their live inputs in a
parameter row, their trees and layers in the left dock, their selection in the right dock and their secondary
views in a tabbed bottom panel; Run is a console with the queue in the navigator and the environment in
Properties; Export is a dialog from the rail. The curve editor's own palette (mode · curve · rails · step · fair)
lives in its pane, not on the application toolbar, as a sketch palette does.

**Why.** Measured, not argued: the v2 artifact's window was 1,450–6,500 px tall at every width, the page scrolled
everywhere, no panel scrolled internally, and the area strip wrapped into two or three rows inside a 64 px title
bar (direction brief, v3 section). The operator named the metaphors — Eclipse/VS Code (activity bar, side bars,
editor group, panel, status bar), Fusion 360 (contextual toolbar per workspace, browser, parameter row),
Shape3d (lines-plan as a viewport layout, persistent station list), Rhino (named viewports, layers panel,
properties following the selection) — and asked for the best thick-client experience per vignette rather than a
linear edit of what existed.

**Rejected alternatives.** (1) *Keep the v2 page and add sticky headers* — leaves the page scrolling and the
toolbar wrapping; treats the symptom. (2) *A responsive toolbar that wraps to two rows* — a second row steals
viewport height and moves every control the user has learned; a measured overflow menu keeps the row and the
positions. (3) *Per-area toolbars that include the curve editor's mode/curve/step* — 60 % of the CAD toolbar hid
behind `More ▾` at 1280 px; the palette belongs to the pane it edits. (4) *Rail labels as icons only* — the areas
are numbered stages in a flow, and the readiness text is the point; the rail is 68 px with number, name and a
badge, and the accessible name carries the readiness.

**Constraints discovered.** A grid row with an unplaced `grid-column: 1/-1` item auto-places into an implicit
sixth row when the rail spans the status-bar row; the rail and status bar now carry explicit rows. An implicit
grid column sized `auto` lets the editor overflow the window; every editor column is `minmax(0, 1fr)`. A
`display:none` dock shifts auto-placement so the editor lands in the 0 px column; the docks and editor carry
explicit columns. A `<details>` overflow menu contributes to `scrollWidth` when closed unless its content is
hidden; the closed menu is `display:none`. Duplicate ids (`#add-station` in the navigator and on the toolbar)
bind handlers to the wrong element; the navigator lost its copies. A `role=tablist` may contain only tabs; the
rail's Export, Checks and Settings sit outside it. The 640 × 400 reflow preset needs the docks reachable: they
are drawers opened from the document-tab row and closed with Escape.

**What the two lenses added (2026-09-21).** The UX & Accessibility lens (BLOCK on the first read) found the
`More ▾` menu clipped by the toolbar's own `overflow:hidden`, the bottom panel collapsing to 0 px at the reflow
preset (auto-placement again: a hidden sash row swallowed the panel), focus dropped by every re-render (recorded
class UI-C), composite ARIA roles without their keyboard patterns, a one-way dock collapse, drawer focus that
never landed, a viewport colour token on dock text at 1.74:1, ⌘Z fighting the viewport's own Z, overlay banners
covering the HUD, and document tabs that were area labels. The Native Desktop lens (PASS-WITH-CONDITIONS) asked
for sashes, a panel maximize, real document tabs, the macOS title bar instead of an in-window menu strip,
platform-correct key labels, Ctrl+Y, F6 pane cycling, tooltips and a slimmer status bar; its claim that the curve
editor had no pointer path was wrong (the anchors drag on `mousedown`), which is recorded here rather than acted
on. Everything else was built and is now observed by the oracle, not asserted. Still open as next steps: context
menus on stations, layers and tabs; area-specific state copy; pointer orbit and wheel zoom in the viewport (the
spec must declare the pointer contract first); the Queue tab beside the navigator list (kept: the list is compact,
the tab carries reasons and retry).

**Control.** Defect class **UI-H2** (web-page habits in a client mockup): `tools/check-mockup-v3.mjs` fails on any
window scroll, any toolbar taller than 44 px or overflowing, any parameter row taller than one row, `More ▾`
present without a hidden group or any hidden group at ≥ 1280 px, and any dock that does not scroll internally,
at all five presets × six areas; the in-artifact audit prints the same shell verdict on every render.
