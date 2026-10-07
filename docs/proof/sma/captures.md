---
id: proof-sma-captures
title: SMA captures A-D against the approved mockup (Ruling 125)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sma, captures, ruling-125]
links:
  - { to: mockup-section-main-area, rel: refines }
  - { to: proof-sma-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Four 1500 x 870 captures of the built app (light theme) for states A to D of the section-in-main-area mockup, each opened and
  compared, with the differences named.
---

# SMA captures

Rendered from the app with a scratch harness (`RenderTargetBitmap` of the real `ShellHost`, the DX capture method): the Example foil, the
VLM + strip run at 5.14 m/s, depth 0.6 m, alpha 3. Mockup states are in `docs/mockups/section-main-area/`. Each PNG was opened and read.

| State | Built | Mockup | Same | Differs |
|---|---|---|---|---|
| A | `01-cad-plan-chip-strip.png` | `01-cad-plan-chip-strip.png` | Chips in a band under the planform, each on its station line; the tip chip selected (heavier line and outline); tabs Plan, Section, Foil source. | The chip text is the section name as built ("section-a"; the mockup says "NACA 0012"). The harness has no window title bar. |
| B | `02-analysis-section-selected.png` | `02-analysis-section-selected.png` | Header "Selected strip · η 1"; chart selector (Cp, Polar, Transition, Bucket, Show table); the profile with Cp_min marker; the Cp chart; groups on the right; the bottom Section tab is one line with "Open in main area". | The right column is the existing group tables (Station, Estimator, Cavitation, Under-read, Stations, Polar), not the mockup's compact Stations table; the profile has no colour ramp. See "Open".
| C | `03-analysis-section-governing.png` | `03-analysis-section-governing.png` | Header "Governing cavitation station · η 0.012 (no strip selected)"; same layout; summary line follows the header. | As B. |
| D | `04-analysis-plan-compact-summary.png` | `04-analysis-plan-compact-summary.png` | Plan with the chip strip under the loading view; bottom panel is the one summary line (station, cl (panel), -Cp_min, "Open in main area"). | The built Plan shows the app's four views (Plan, 3D, Side, Front); the mockup draws Plan plus 3D only, by its own note. |

## Open

- The mockup draws the conditions band (Speed, Water, Depth, alpha, Evaluate, Find alpha) above the Section document. In the build the band is part of the Plan document, so it does not show over the Section document (B and C). Evaluate and Find alpha need the Plan tab. Moving the band above all documents is a shell layout change outside SMA's owned paths; it needs a decision.
- The mockup's Stations table (five columns, "5 of 64") is not built; the document shows the groups `SectionDisplay` already produces. New table content would be a new design for `SectionDisplay`, not owned.
