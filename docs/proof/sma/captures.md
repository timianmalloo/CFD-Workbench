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
| B | `02-analysis-section-selected.png` | `02-analysis-section-selected.png` | Header "Selected strip · η 1"; chart selector (Cp, Polar, Transition, Bucket, Show table); the profile with Cp_min marker; the Cp chart; groups on the right; the bottom Section tab is one line with "Open in main area". | The conditions band is above the document (Evaluate and Find α work from here). The Stations table has the four columns with the shown row highlighted, but no "5 of 64" count and, here, all rows read Clear; the right column also keeps the Estimator and Under-read groups the mockup does not draw (see "Groups"); the profile has no colour ramp. |
| C | `03-analysis-section-governing.png` | `03-analysis-section-governing.png` | Header "Governing cavitation station · η 0.012 (no strip selected)"; same layout; the governing row is highlighted; summary line follows the header. | As B. |
| D | `04-analysis-plan-compact-summary.png` | `04-analysis-plan-compact-summary.png` | Plan with the chip strip under the loading view; bottom panel is the one summary line (station, cl (panel), -Cp_min, "Open in main area"). | The built Plan shows the app's four views (Plan, 3D, Side, Front); the mockup draws Plan plus 3D only, by its own note. |

## Repair cycle 2

- Band readouts (B, C, D): the clip was a stale measure. `WideContentFits` measures the row directly with an unbounded width, and the cells kept stale widths (q drawn 98.9 px in 85, Re 106 in 92). `SetAvailableWidth` now invalidates the cells and the row. After: every cell is as wide as its drawn text and the readouts are separate in B, C and D. The new check passes in the test process both before and after (the stale measure showed only in the capture harness), so it pins the result but is not a red receipt.
- Chart selector: the four chart buttons and "Show table" use the group value row's `prop-seg` / `prop-seg-box` style (row height, thin outline, the active side lightly filled); the table toggle is its own outlined box beside the selector.
- NOT done, by design: the mockup's profile on a dark viewport coloured by Cp. DX's Section view (the one hosted here, and in `docs/proof/dx/captures/01-02`) draws the profile as a z/c line chart with the Cp comb as two offset series; it has no dark viewport, no Cp colour ramp ("vik") and no chips. It is reused as it is. The coloured profile needs a new renderer (a Cp colour map, the viewport, the caption and estimator chips) in `SectionChartView` and `SectionDisplay`; that is a build step, not a hosting step.

## Groups

- Dropped from the document: the "Station" name row (the header says it) and the text "Stations" group (the table replaces it; the data stays in `SectionView.Groups` for the Analysis checks).
- Kept although the mockup does not draw them: "Estimator" (cl (panel), Cm, alpha_L0, cd bound, the Cp_min location note, and the source of the summary line) and "Under-read" (carries the Provisional flag). Dropping them would hide values and a label with no other home in the document; say so if you want them gone.
- Table headers: "Station", "α_eff °" and "Cavitation" have no DESIGN.md COPY row (they are the mockup's words); "−Cp_min" is `Labels.CpMinLabel` (COPY-360). The Cavitation cell is the leading word of COPY-301 to COPY-303 ("Clear", "Inside the margin", "Possible"); "Unavailable" for an unavailable or undefined station. Rows not solved at 400 panels carry COPY-384 under the row.
