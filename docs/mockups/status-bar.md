---
id: mockup-status-bar
title: Status bar — where reports go (V2 chosen, DR-STATUS-1)
type: design
status: accepted
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, status-bar, toast, shell, properties, operator-pick]
links:
  - {to: mockup-property-grid, rel: relates-to}
  - {to: mockup-property-grid-cells, rel: relates-to}
  - {to: property-grid-rulings, rel: relates-to}
  - {to: design-app-shell, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  The operator asked for reports such as the point-type change to leave the Properties sheet and render in a status
  bar at the bottom of the shell. The page shows the whole shell at 1280 × 800 (structure-B Properties, the Plan, a
  24 px bottom strip) in three variants and three moments. The operator chose V2 (DR-STATUS-1): a status strip plus a
  transient warning toast; errors stay where they arise. V1 and V3 remain only as the record of the pick; V3's
  scrolling Messages pane in the bottom bar is rejected.
review-suggested: []
---

# Status bar — where reports go

Open [`status-bar.html`](status-bar.html) over `file://`. It opens on **V2, the chosen design**. The *Variant* and
*Moment* menus switch the live shell; the button at the top right switches to the dark theme. Below the shell are the
policy table (each message kind: where it renders today, and in each variant) and a contact sheet of all nine
variant × moment cells.

Captures: [`variants.png`](../proof/status-bar/variants.png) (contact sheet, V2 first) and the chosen V2 warning moment
in [light](../proof/status-bar/v2-warning-light.png) and [dark](../proof/status-bar/v2-warning-dark.png).

## The pick

The operator's request (2026-10-02): "the status from the change of the point from anchor to tangent should not be in
the property sheet... it should be in a status bar, we should have it render in a status bar at bottom of the shell".

| Variant | What it is | Outcome |
|---|---|---|
| **V2** — Premiere / Fusion | A 24 px strip shows the last report with an icon by kind. A warning also opens one toast above the strip. Errors stay where they arise | **Chosen** (DR-STATUS-1) |
| V1 — VS Code | The strip plus a click-to-open popover of recent reports | Not chosen; record only |
| V3 — Messages pane | The strip plus a collapsible, scrolling Messages log docked to the bottom bar | **Rejected**: "i really dont want the bottom bar being scrollable … thats awful" |

The three moments are the same in each variant: after a type change (anchor), after a typed root chord with a fit
warning, and after a refused edit (the edges would cross).

## Measured

`capture-status-bar.mjs` (scratchpad Playwright root) loads each of the nine variant × moment shells: no page errors,
every shell 1280 × 800, no text under 11 px, no bar or toast button under 24 px. The strip is 24 px plus a 1 px top
rule.

