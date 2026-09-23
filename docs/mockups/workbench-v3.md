---
id: mockup-workbench-v3
title: CFD-Workbench interactive design mockup v3 — thick-client shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, shell, thick-client, setup, cad, analysis, experiment, run, results, export, v3]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v2, rel: supersedes}
  - {to: review-ui-workbench-v3, rel: relates-to}
  - {to: thick-client-shell, rel: relates-to}
  - {to: decision-seven-areas, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-21
summary: >-
  Self-contained HTML workbench rebuilt as a thick-client shell: a fixed window that never scrolls — menu bar,
  one-row toolbar with measured overflow, parameter row, activity rail of the six document areas plus Export as a
  dialog, Navigator and Properties docks, document tabs over one viewport, a tabbed bottom panel and a status bar —
  with the v2 content re-homed per vignette. Illustrative throughout; no kernel, solver, file I/O or model call.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Interactive workbench mockup v3 — thick-client shell

[Open the mockup](workbench-v3.html) · [Specification v1.1](../specs/cfd-workbench-v1.html) · [Design language](../../DESIGN.md) · [Review](../reviews/ui-workbench-v3.md) · [Direction brief](../design/workbench-direction.md) · [Decision note](../notes/thick-client-shell.md) · [Browser evidence](../proof/workbench-v3-browser-check.json) · [Mockup v2 (superseded)](workbench-v2.md)

Open the HTML directly in a browser; no build, server, key, solver, internet or external font is needed, and the
page makes zero external requests. The review bar at the top is review chrome and never ships. The **window
preset** control sets the client window (1024 × 700 minimum · 1280 × 800 · 1440 × 900 lines-plan · 1600 × 1000 ·
640 × 400 reflow); the window itself never scrolls.

## The shell

Menu bar (native menus in the client; here every menu is a parity path that reports itself) · **toolbar** — one
44 px row filled from the spec's B1 verb table for the current area; the row is measured after every render and
groups that do not fit move, from the tail, into `More ▾` (none hidden at 1280 px and above; at most one at the
1024 minimum) · **parameter row** — CAD's derived strip (span, area, aspect ratio, mean chord, trailing edge and
its floor chip) or Analysis's operating point (point, speed, water, depth, incidence); absent elsewhere ·
**activity rail** — the six document areas in flow order as a vertical tab list whose accessible names carry the
readiness ("CAD — r4 · 3 stations", "Run — gated (SPIKE-03/04) · Ready"), then Export as a dialog, and Checks
(with its count) and Settings at the foot · **left dock** — the Navigator for the area (briefs · revisions and
stations · experiments · the run queue · samples), plus the Layers section in Analysis and Results · **editor** —
document tabs that are documents (one tab for the open design with a dirty dot until ⌘S; Settings opens beside
it; the rail selects the perspective), the named-view buttons and the lines-plan toggle for the viewport areas,
one viewport or document body · **bottom panel** — tabs per area, collapsible to its tab strip and maximizable
to the full editor (⌘J · ⌘⇧M) · **sashes** on both docks and the panel (drag, arrow keys, double-click reset) ·
gutter expand controls when a dock is collapsed (⌘B · ⌘⌥B) · F6 cycles panes · **right dock** —
Properties for the selection (station · run manifest and derived conditions · experiment · backend environment ·
sample provenance) and the Prompt entry section · **status bar**. Every dock, pane and document body scrolls
internally. Under macOS the top row is the title bar (menus are system-owned); under Windows it is the menu
strip, and shortcut labels follow the platform. At the 640 × 400 preset the docks are drawers opened from the
document-tab row and closed with Escape, and the bottom panel starts collapsed.

## The areas as arranged

1. **Setup** — a document: "Describe a starting design" beside the parameter form (purpose, four soft targets
   with weights and deviations, rider, water); the seeded preview and goal state; Feasibility and Operating
   points tabs below; the Setup brief in Properties; toolbar Seed · Goal · Go.
2. **CAD** — the viewport (plan · front · iso · lines-plan · fit) with the derived strip above; the **Curve
   editor** tab carries its own palette (mode · curve · rails · nudge step · fair) the way a sketch palette
   does, with Catalog and Checks beside it; stations and the DAT catalog on the toolbar; revisions, stations and
   runs in the Navigator; the station in Properties; the section editor as a dialog.
3. **Analysis** — the same viewport with the analysis layers (force vectors, loading strips, depth band, Cp on
   the section); the operating point in the parameter row; derived q, Re, h/c, Fr_h, σ and V_crit in Properties
   under the run manifest; Layers in the left dock; Results and Charts tabs. **CAD ⇄ Analysis** (⌘⇧A) is
   navigation: the rail follows, camera and selection persist, an open preview is hidden and restored.
4. **Experiment** — a document with the sweep grid or the optimize definition (multipoint set, A_cav, design
   vector with bounds and frozen flags, tier, budget, seed; the single-point refusal); the 12-case preview in the
   Cases tab; New · Describe · Queue on the toolbar; no Run verb.
5. **Run** — the console (state machine, residual and force histories, mesh-gate bars, resource meters) as the
   document; the queue as a compact list in the Navigator and the full table with reasons in the Queue tab; the
   solver log tail in the Log tab; the backend environment (substrate, pinned digest, smoke test, capability
   record, limits) and "Prepare my environment" in Properties; Check · Run · Cancel · Retry · Explain on the
   toolbar. The run is a fixture stepped from the review bar — nothing autoplays.
6. **Results** — the slice viewport (Cp flood, isolines, probe, streamlines with the labelled dash affordance,
   separation only with τ_w, force vectors, vortex-core candidates) with the sample in the parameter row; samples
   in the Navigator and Layers below them; the Timeline, Sweep and Candidates tabs; the sample's provenance in
   Properties; hold speed/angle, Compare, Ask and Open in ParaView on the toolbar. Below a 260 px slice scale the
   in-canvas labels drop and the caption and Properties carry them.
7. **Export** — a dialog from the rail (C1: a modal in the native client): STL/3MF, gated STEP, Fusion-ready
   STEP, 3DM via rhino3dm, DAT, AVL, CSV and the safety string.

A **prompt entry** sits in the Properties dock in every area with the area's fixed entry name and proposal kind;
no-key, unevaluated-model and cap states render their strings.

Every number is Illustrative and says so. The evaluator is an illustrative implementation of the A4.2 contract;
the physics are labelled fixtures; the run and the results are fixtures; the Fusion and Rhino exports are rows,
not files. Native accessibility trees, file I/O, solvers and model calls are outside this artifact.

## Executable control

`node tools/check-mockup-v3.mjs [<node_modules dir with playwright>]` first proves the **shell contract** at all
five window presets × six areas (30 cells: the window never scrolls, the toolbar is one 44 px row that never
overflows, the parameter row, status bar and document-tab row never clip, `More ▾` appears exactly when a group is
hidden and never at 1280 px and above, every dock scrolls internally, the visible bottom pane is ≥ 120 px), then
the **observed accessibility group** (a control moved into `More ▾` is visible and hit-testable and works; focus
survives rail, toolbar and bottom-tab re-renders; ⌘Z in the viewport is undo, not zoom; drawers take and return
focus; a collapsed dock leaves a visible expand control; sashes resize by keyboard and pointer and reset; the
panel maximizes; no viewport-only colour on dock text), then walks the areas in three themes, eight states and five windows
asserting contrast, target sizes, no NaN or placeholder and SVG text ≥ 12 CSS px, then the interaction contracts:
the rail and its readiness, Export as a dialog, the toolbar groups and bottom tabs per area, dock and panel
collapse, menu and ⌘K parity, both Setup roads, the curve editor pane, station add/remove from the toolbar,
GEO-13, focus after a nudge, the toggle preserving camera and selection, analysis layers with accessible names,
the experiment refusal and Queue, the run state machine with cancel and retry and the Log tab, the results layers,
replay and reduced motion, and every prompt entry. Evidence lands in `docs/proof/workbench-v3-browser-check.json`;
`ui-craft-gate.py` (`docs/proof/ui-craft-findings-v3.json`) and `design-lint.py --strict` run beside it.

Best first review: set the window preset to 1024 × 700 and note that nothing scrolls and the toolbar keeps one
row → CAD → add a station from the toolbar → collapse the bottom panel and the Navigator → ⌘⇧A to Analysis and
watch the vectors appear over the same view → Run → step the fixture through the mesh gate and cancel from the
toolbar → Results → Candidates tab under the success state → 640 × 400 and open the Properties drawer.
