---
id: mockup-m12c-section-editor
title: M1.2c section editor — editing a section from the Side view, the editor mode, and the four decisions
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, m12c, section-editor, point-types, points-pane, messages, operator-show]
links:
  - {to: design-m12c-section-editor, rel: documents}
  - {to: mockup-m12b2-views, rel: refines}
  - {to: mockup-status-bar, rel: relates-to}
  - {to: mockup-property-grid, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: note-m12c-rulings, rel: relates-to}
review-by: 2026-12-31
summary: >-
  The operator asked to edit sections (2026-10-03). This page draws three screens of today's 1280 × 800 shell for the
  Example foil: picking the Root station in the Side view; the section editor with upper point 4 made an anchor on the
  upper surface only (comb, pointer probe, readouts); and a Finish blocked by crossing surfaces. It then shows the four
  open decisions as side-by-side variants. All geometry, and every number printed, is computed in the page from
  section-a.
review-suggested: []
---

# M1.2c section editor — the mockup

Open [`m12c-section-editor.html`](m12c-section-editor.html) over `file://`. The screens come first, then the decisions.
The button at the top right switches the chrome to dark. The viewports stay graphite in every theme (M1.2b D-4).

## The screens

### 1 · Side view → Edit section

**Shows:**

- Four views, with the Root section picked in the Side view.
- Properties' Station group, ending in **Edit section…**.
- The status strip, naming the Return and double-click routes into the editor.

**Implements:** design §0.1 steps 1–2, §5.2 `section.edit`, §11.3.

### 2 · The section editor

**Shows:**

- **The mode bar:** Curvature on and Thickness ×2 off by default.
- **The canvas:** 1:1, after Fit Selection. Upper point 4 has been made an anchor (Horizontal) on the upper surface
  only. On it:
  - the entry shape of both surfaces, as a dashed ghost;
  - the comb, with the break at the anchor and the clipped nose teeth;
  - the Tracing probe at the pointer.
- **The station strip:** each station's chord and t/c.
- **Properties:**
  - **Point:** Type, x, y.
  - **Tangent:** Kind, and each handle's Angle and Length.
  - **Section:** own t/c, the t/c at each station with its consequence line, Station t/c ▾, LE radius, TE gap and
    wedge.
  - **Wing:** read-only.
- **The Points pane:** the "Control net · degree 5" grid, with a Kind column.

**Implements:** §0.1 steps 3–4, §3.4, §11.1–11.4.

### 3 · Finish blocked

**Shows:**

- Lower point 6 dragged across the upper surface, with the crossing marker.
- Finish off, with its reason.
- The strip warning, with one **Show** action.
- The probe reading "t here −0.06 %".

**Implements:** §0.1 step 5, §9, CAD-20, COPY-123.

## The decisions

**Ruled by the operator on 2026-10-03:** OD-1 A, OD-2 A, OD-3 B, OD-4 a. The rulings are recorded in
[`docs/notes/m12c-rulings.md`](../notes/m12c-rulings.md).

- On the page, each chosen card is marked "Chosen · operator 2026-10-03".
- The other cards stay as the record, marked "Not chosen · kept as the record".
- The editor's plate and probe now say "display", following the section display-path ruling (ADR-0010 Amendment 1).

| Decision | Variants | Implements |
|---|---|---|
| OD-1 | **A** editor in the model area (recommended). **B** editor in the Side slot, with Plan, 3D and Front redrawn from the draft. **C** why the placed Side section is not an edit surface (a computed Δ plot) | §13 OD-1, §3.6 |
| OD-2 | **A** no Messages pane, with Show in the strip (recommended). **B** an Issues popover from the strip. **C** a Messages pane in the right side bar | §13 OD-2, DR-STATUS-1 |
| OD-3 | **A** Points in the bottom panel. **B** right side bar (recommended). **C** no Points pane. **D** a Points tab in the left side bar. Each card prints its measured canvas scale and the number of rows visible | §13 OD-3 |
| OD-4 | the fallback if the per-surface certificate cannot be built (text) | §13 OD-4, §3.5 |

## How the geometry is made

Everything in the viewports is computed in the page from `src/CfdWorkbench.Desktop/Assets/example.foil`:

- **section-a:** two degree-5 surfaces, 8 points each, with shared knots.
- **Control → Anchor on the upper surface only:**
  - u\* where x(u\*) = 35 %, by bisection on the monotone x;
  - five Boehm insertions (Piegl–Tiller A5.1);
  - the Δy shift of the anchor and its handles (ADR-0005 decision 5).
- **Tangent Horizontal:** levels both handles at the anchor's y.
- **Anchor → Control on the new anchor:** removes the handles and lowers the knot from multiplicity 5 to 3.
- **Curvature** from the analytic B-spline derivatives, with derivative control points (never finite differences).
  This gives:
  - the comb, auto-scaled so the 90th-percentile tooth is 30 px, with teeth over 60 px clipped and marked (×). The
    page samples the teeth by parameter; the build spaces them by arc length (M1.2b contract);
  - the one-sided curvature at the anchor;
  - the LE radius, r = 1/κ(0).
- **Per-surface x inversion** for the own t/c, the crossing interval and the probe.
- **Rule A at equal x** (C = (u + l)/2, T = (u − l)/max) and the FoilDSL §6 placement. These drive the 3D, Side and
  Front views and the OD-1 C Δ plot.
- **The dihedral channel** is the fixture's (flat).

## Measured

Playwright and system Chrome, page viewport 1360 × 900, shells 1280 × 800, read from `window.__measure`.

| Quantity | Value |
|---|---|
| Upper points before → after | 8 → 13. Lower stays at 8, bytes unchanged (`true`) |
| Boehm shape change before the shift | 4.4 × 10⁻¹⁶ |
| Anchor shift Δy (= the largest change) | 0.88 % chord |
| Anchor → Control on the new anchor | 13 → 11 points, change 0.33 % chord (800 samples; the CG lens's probe: 0.31 %) |
| One-sided curvature at the anchor | −6.29 / −1.73 per chord: the curvature breaks |
| LE radius upper · lower, own → at Root (edited state, k = 0.990) | 1.04 · 1.04 → 1.02 · 1.02 % c |
| TE gap · wedge, own → at Root | 0.00 % c · 11.42° → 0.00 % c · 11.31° |
| Own t/c | 11.26 % at 33.1 % → 12.12 % at 34.9 % |
| Crossing (lower point 6 at 8 % chord) | 78.4–83.4 % chord |
| OD-1 C | record upper point 4 raised 0.60 mm → placed upper +0.126 mm and placed lower +0.123 mm at 35 % chord |
| Anchor's nearest neighbour on screen (1:1) | 24.7 px after Fit Selection. At full chord: 8.7 px (B) and 11.9 px (A), so the glyphs overlap |
| Canvas scale at full chord | A 8.85 · B 6.44 · C 8.85 · D 8.85 px per % chord |
| Points rows visible without scrolling | A 4 of 20 · B 20 of 20 · D 20 of 20 |
| Comb teeth clipped (full-chord views) | about 52–54 |
| Console errors · NaN / undefined / null leaks | none · none |

**UI craft gate** (`ui-craft-gate.py`): one Minor, `side-tab` on `.sb .msg.warn`. This is a **recorded deviation**:
DESIGN.md §4 *Status strip* requires the 3 px warning rail (DR-STATUS-1 V2).

## Not shown, and why

- **Interaction.** This is a static show page, like the M1.2b2 views page. Drag, nudge, insert and delete, the Escape
  cascade and Finish are specified in the design (§6.1, §11.3) and proven by the build's rendered tests.
- **Replace from catalog and Save to My sections.** These are M1.2d. Section ▾ in the build leaves them out; it does
  not show them disabled.
- **Thickness ×2.** Off by default; the variant cards draw at 1:1 too.
