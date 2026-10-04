---
id: mockup-m12d-catalog
title: M1.2d catalog Replace and My sections — the section editor's Replace sheet, Save to My sections, and the point-spacing refusal
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, m12d, section-editor, catalog, my-sections, replace, abscissa, operator-show]
links:
  - {to: design-m12d-catalog, rel: documents}
  - {to: mockup-m12c-section-editor, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: rulings, rel: implements}
review-by: 2026-12-31
summary: >-
  The operator asked to apply an existing catalog profile to a section (2026-10-04). This page extends the approved
  M1.2c section-editor look with nine screens of today's 1280 × 800 shell for the Example foil: the Section menu, the
  Replace sheet with NACA 4412 previewed on the shared section, Replace applied, Save to My sections with a duplicate name
  refused, the point-spacing refusal after Make unique (with Replace Root and Tip offered), a symmetric section that fits, a thin section scaled to the station t/c,
  My sections seen from any foil, and the hard states. Every fit and change number comes from the probe receipt; previews
  are the NACA closed form.
review-suggested: []
---

# M1.2d catalog Replace and My sections — the mockup

Open [`m12d-catalog.html`](m12d-catalog.html) over `file://`. The State list at the top right jumps to any screen or
decision; the button beside it switches the chrome to dark. Captures (light theme for every screen and decision; dark for screens 2 and 5) are in
`docs/proof/m12d-catalog/captures/`.

**Direction (in words, unchanged from M1.2c).** A precision CAD editor for one section; graphite viewport, quiet chrome,
numbers with units. The new surface is a **command sheet** at the right of the model area (the Fusion/Onshape command
panel), so the dashed preview stays visible on the section while choosing — CAD-18 asks for the preview on the section,
which a centred dialog would cover. The sheet is modal for the keyboard; pointer zoom and pan stay on the canvas.

**Where the numbers come from.** `docs/proof/m12d-catalog/output/` — the probe runs the as-built Core (`FitToBasis`,
`Geometry.Assess`) on NACA shapes generated from the closed form in the chord frame (closed TE). The page checks itself:
the largest change from section-a to the probe's NACA 4412 record, computed in the page by x inversion, equals the
probe's A4.5 oracle number (printed at the bottom of the page).

## The screens

| # | State | Shows | Implements |
|---|---|---|---|
| 1 | Section ▾ open | Replace from catalog… and Save to My sections… at the top of the Section menu; Import .dat… kept | CAD-21, design §0.1 1–2 |
| 2 | Replace sheet, NACA 4412, shared section | families NACA · Eppler · Speer · My sections; GEN rows "closed TE"; unbuilt NACA, Eppler (pending terms) and Speer (cite only) rows tagged; dashed preview and largest-change marker; detail line with fit in µm and % chord, limit, 8 → 15 points, t/c, frame; both thumbnails previewed | CAD-18, UI-38, §3.6 rule 3 |
| 3 | Replace applied | the fitted 15-point record over section-a (dotted); chip "Catalog original · NACA 4412"; Source row; Points pane 15 rows; status with ⌘Z | CAD-18 Replace clause |
| 4 | Save to My sections, duplicate refused | after one point edit (chip "Modified from NACA 4412"); name "kite ROOT 4412" refused (COPY-114), focus stays; provenance, rights and what is saved | CAD-19, §3.7 |
| 5 | Point-spacing refusal | Root made unique; NACA 4412 on Root's 8 points is 26.86 µm off; Replace disabled with the reason; **Replace Root and Tip (Tip changes too)** offered; Tip thumbnail previews B | §3.6 rule 4, Ruling 71 |
| 6 | Point-spacing fit | NACA 0012 on Root's 8 points: 9.75 µm; Replace enabled; points and types kept | §3.6 rule 4 (A) |
| 6b | Thickness scaling | NACA 0009 at the 12 % stations: the detail line says it will be scaled to 12.00 %, the option "Use NACA 0009's t/c (9.00 %) at these stations", and the chip text after Replace | §3.6 rule 6c, COPY-193b |
| 7 | My sections from any foil | the saved "Kite root 4412" previewed with the same readout (exact copy); a damaged file listed (COPY-198) | CAD-19 "another foil", ADR-0008 scan |
| 8 | Hard states | catalog unavailable (spec string) and My sections empty | UI-38 C2 rows |

## The decisions on the page

- **DR-M12D-1** — three cards: A + B (recommended, screen 5), D (accept over 10 µm with a "Fitted to …" chip), and C/E
  as text with their numbers.
- **DR-M12D-2 … DR-M12D-7** — one table with the recommendation and the alternative.

## What this page does not show

- Keyboard operation: the screens are static states. The focus rules and their tests are design §11.3.
- The Δ-vs-x deviation strip a Rhino user would expect (design F-8, a next step).

## Checks run on this page

- No page errors, no `NaN` / `undefined` / template leaks, in both themes (`check.mjs` in the session scratchpad; the
  captures are its output).
- Sheet, menu and dialog targets at full size are ≥ 24 px; the 9 smaller boxes counted are the half-scale decision
  cards (harness scaling, as on the M1.2c page).
