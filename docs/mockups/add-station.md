---
id: mockup-add-station
title: Add station — the inspection slice, Promote and Remove station in today's shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, cad, station, inspection-slice, promote, remove-station, hard-states, cad-02, cad-09, geo-06]
links:
  - {to: design-add-station, rel: documents}
  - {to: mockup-planform-point-verbs, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
review-by: 2027-01-31
summary: >-
  Eight states of the 1280 × 800 shell (and the 640 × 400 reflow) on a stated two-station fixture wing: no slice, a slice
  placed with its label and Promote, the promoted station selected in the Plan chip strip, Front, Side, Browser and
  Properties, a coincident η refused with the existing station selected, Remove with the change above 10 µm (Apply
  disabled with its reason), Root remove refused, the certificate budget refused, and a competing draft. For the
  operator's review after the Adversary pass.
---

# Mockup: Add station (`add-station.html`)

The page is self-contained: no network, no external fonts or scripts. Colours are DESIGN.md tokens (light, dark and the
`contrast-*` set). The harness switches **state**, **viewport** (1280 × 800, 640 × 400 reflow), **theme** (light, dark,
high contrast) and **reduced motion** (nothing in the page animates; the switch proves no state depends on motion).

## Screens (the state switcher)

| # | State | What to check | Design § |
|---|---|---|---|
| 1 | No slice | two station chips in the strip under the planform (Ruling 124); the η entry empty | 5.2 |
| 2 | Slice placed | hollow dashed slice chip "Slice · η 35.0 %" with Promote and ×; dashed lines in Plan and Front; dashed outline in Side; the Slice panel with "Derived view — not saved" | 5.2, 5.3 |
| 3 | Promoted | "Station 2" selected in the strip, Front, Side, the Browser row and the Properties heading; the receipt with the deviation and "Thickness: keep current" | 5.3, E7 |
| 4 | Coincident η refused | "35" typed again: Station 2 selected, the refusal in the status line, nothing changed | 3.2, GEO-06 |
| 5 | Remove above the acceptance | the flyout: measured change 412 µm, limit 10 µm, Apply disabled with its reason | 2.9, 5.4 |
| 6 | Root remove refused | Remove station disabled with "Root and tip stations cannot be removed." | 5.4 |
| 7 | Certificate budget refused | a second slice at η 70.0 %: the refusal names the four differing sections; the slice stays | 2.7 |
| 8 | Competing draft | the draft chip on the options strip; the slice still placeable; Promote disabled with the draft named | 5.5, CAD-09 |

## The fixture

Half-span 0.50 m; chord c(η) = 0.130 − 0.080 η² m; leading-edge aft offset 0.030 η² m; no twist, no dihedral;
t/c(η) = 0.12 − 0.04 η. Root "NACA 0012", Tip "NACA 0008", both closed, on one 10-point spacing. NACA 00xx sections
share one unit thickness shape, so promotion on this wing is the exact regime R1 of the design. The planform, the t/c
lane and the body-plan outlines are computed in the page from these numbers. **Deviation numbers are illustrative**
(2.2 × 10⁻¹⁶ for the R1 receipt; 412 µm for the edited Station 2): the product measures them by design §2.6.

## Not on the page

- The 3D view (DR-S1-6: the slice is not drawn in 3D in this slice).
- The R2 and R3 states (peaks differ; spacing differs): their copy is S1-COPY-11 and S1-COPY-9 in the design; they look
  like state 7 with other words.
- The "Checking…" state (a one-word chip change).
- The section editor, the command palette and the native menu (`station.add`, `station.remove` are rows there; the
  design's copy table holds their labels).
- Any number computed by the product. The page computes geometry only.

## Measurements

- `ui-craft-gate.py docs/mockups/add-station.html` (2026-10-21): **not run** — exit 2, "the detector produced no JSON,
  so nothing was scanned": Impeccable has no binary for windows-arm64 on the design machine. A clean report would have
  been a false pass. Run it on a machine with the detector before the operator review.
- The page script passes `node --check`. Token colours only; the viewport keeps its dark tokens in every theme, as the
  shell does.
