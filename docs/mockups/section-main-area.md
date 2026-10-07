---
id: mockup-section-main-area
title: Section in the main area (Ruling 124) - chip strip, Section document tab, compact bottom summary
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, ruling-124, plan, section, analysis, station-chips, operator-show]
links:
  - { to: mockup-dx-section-polar-states, rel: refines }
  - { to: mockup-area3-analysis, rel: refines }
  - { to: design-language, rel: depends-on }
review-by: 2026-12-31
summary: >-
  Four 1500 x 870 states of the shell after Ruling 124: the CAD Plan with station chips in a margin strip, the Analysis Section
  document tab for a selected strip and for the governing fallback, and the Plan tab with the compact bottom-panel Section
  summary and its Open in main area action. Two strings are proposed copy.
---

# Section in the main area (Ruling 124)

Open [`section-main-area.html`](section-main-area.html) over `file://`. The tabs A to D switch the state; the button top right
switches light and dark. Captures are in [`section-main-area/`](section-main-area/) (light; `-dark` beside each).

Shell chrome follows the real app captures (`docs/proof/dx/captures/02-section-cp-selected-strip.png`,
`docs/proof/m12b2-modelarea/plan-3d.png`). Charts, groups, copy and computed data are the approved
[`dx-section-polar-states.html`](dx-section-polar-states.html) (Karman-Trefftz Cp and the vortex lattice computed in the page; polar values
a spike-oracle stand-in). Nothing is built from this page.

## The four states

| State | Capture | What it shows |
|---|---|---|
| A. CAD, Plan | `01-cad-plan-chip-strip.png` | Chips sit in a band under the planform, each on its station line, never over the geometry. The tip chip is selected. Tabs: Plan, Section, Foil source. |
| B. Analysis, Section, a strip selected | `02-analysis-section-selected.png` | Header "Selected strip · η 1" (COPY-394), the chart selector, the section profile with Cp, the chart, the short Stations table, the polar and cavitation groups. |
| C. Analysis, Section, no strip selected | `03-analysis-section-governing.png` | Same, header "Governing cavitation station · η 0.012 (no strip selected)" (COPY-395). |
| D. Analysis, Plan, compact summary | `04-analysis-plan-compact-summary.png` | Bottom panel Section tab is one line: station name, cl (panel), -Cp_min, and "Open in main area". Chips in the strip over the loading Plan. |

Chip behaviour (unchanged from today): click selects the station; double-click opens the section editor.

## Proposed copy (not in DESIGN.md COPY-293..399)

Both are marked in the page with a dashed outline and "proposed copy".

1. **Open in main area** - the bottom-panel action. The words are Ruling 124's; there is no COPY row.
2. **Section** - the title of the new model-area document tab. The word is the bottom-panel tab name; no row covers the model-area tab.

Everything else is reused: "Plan", "Foil source" (as built), COPY-394, COPY-395, COPY-295 "cl (panel)", the group and chart notes of the approved states.
The chip label is the station's section name as built (the page uses "NACA 0012").

## Choices to confirm (stand-ins and open points)

- The CAD and Analysis Plan views are drawn as Plan plus 3D Iso ("Views Plan + 3D") as in the m12b2 capture; the real four-view Analysis layout is not redrawn.
- The 3D view is a schematic, not a render.
- The Section document keeps the CAD / Analysis navbar at the bottom; the Views and Display controls are dropped there because the document is not a viewport. Confirm.
- The bottom panel shrinks to a 68 px summary (from 232 px) so the Plan and the Section document get the room. Confirm the height.
- The summary line follows the document header: the selected strip when one is selected, else the governing station.
- Stations table columns (station, alpha_eff, -Cp_min, cavitation state) are computed from the same strips; the "short" length (5 stations) is a choice.

## What the build would change (from a quick read of src/CfdWorkbench.Desktop; Inferred, not verified by running)

| File | Change |
|---|---|
| `PlanCanvas.cs` | Station chips (`VisibleStationChips` :105, drawn :828, fit reserve `ChipDrop` :972): move the chip row into a margin strip below the planform, align each chip's x to its station, add the leader line; hit test (:342) and double-click unchanged. |
| `Shell/ShellLayout.cs` | Remove `SectionSampleDocument` (:17, :75-81, :115, :157); add a `SectionDocument` titled "Section"; visible dockables become Plan, Section, Foil source. |
| `Shell/ShellHost.cs` | :200 binds the sample view; bind the Section document to the Analysis section content instead. |
| `Analysis/SectionTabView.cs` | Reused at full size in the document (its fixed 220 x 118 profile and 118 high chart become sized by the host); the bottom-panel instance becomes the one-line summary. |
| `Analysis/AnalysisPanel.axaml` | :39 `SectionTab` content becomes the compact summary with the "Open in main area" button; the panel height shrinks. |

## Measurements

- `ui-craft-gate.py docs/mockups/section-main-area.html --markdown`: no findings (a floor, never a verdict).
- Browser load in system Chrome: no page errors in the eight captures (four states, light and dark).
