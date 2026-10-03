---
id: mockup-m12c-section-editor
title: M1.2c section editor — editing a section from the Side view, the editor mode, and the four decisions
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, m12c, section-editor, point-types, paired, points-pane, messages, operator-show]
links:
  - {to: design-m12c-section-editor, rel: documents}
  - {to: mockup-m12b2-views, rel: refines}
  - {to: mockup-status-bar, rel: relates-to}
  - {to: mockup-property-grid, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: note-m12c-rulings, rel: relates-to}
  - {to: rulings, rel: implements}
  - {to: review-ui-m12c-paired, rel: relates-to}
review-by: 2026-12-31
summary: >-
  The operator asked to edit sections (2026-10-03). This page draws five screens of today's 1280 × 800 shell for the
  Example foil: picking the Root station in the Side view; the section editor with point 4 made an anchor on both
  surfaces (paired point types, Ruling 60; comb, pointer probe, readouts, the pairing cue); a paired x move; a paired
  Anchor → Control refused over 10 µm; and a Finish blocked by crossing surfaces. It then shows the four decisions as
  side-by-side variants. All geometry, and every number printed, is computed in the page from section-a.
review-suggested: []
---

# M1.2c section editor — the mockup

Open [`m12c-section-editor.html`](m12c-section-editor.html) over `file://`. The screens come first, then the decisions.
The State list at the top right jumps to any screen or decision; the button beside it switches the chrome to dark.
The viewports stay graphite in every theme (M1.2b D-4).

**Paired point types (Ruling 60, 2026-10-03).** The certificate spike was a no-go, so M1.2c ships paired types
(OD-4 a). Screens 2, 2b, 2c and 3 draw them. Every change against the approved page is listed in
[`ui-m12c-paired.md`](../reviews/ui-m12c-paired.md), with captures in `docs/proof/m12c-paired-mockup/`.

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
- **The canvas:** 1:1, after Fit Selection. Point 4 has been made an anchor (Horizontal) on **both surfaces**: the
  lower surface gets the same knots (shape exact) and its own anchor at the same chord position. On it:
  - the entry shape of both surfaces, as a dashed ghost;
  - the comb, with the break at both anchors and the clipped nose teeth;
  - the pairing cue: the lower partner ringed and linked at the shared x;
  - the Tracing probe at the pointer.
- **The station strip:** each station's chord and t/c.
- **Properties:**
  - the line "Paired with lower point 7. Type, kind and x are shared.";
  - **Point:** Type · both surfaces, x, y.
  - **Tangent:** Kind · both surfaces, and each upper handle's Angle and Length.
  - **Section:** own t/c, the t/c at each station with its consequence line, Station t/c ▾, LE radius, TE gap and
    wedge.
  - **Wing:** read-only.
- **The Points pane:** the "Control net · degree 5" grid, with a Kind column, the note "Type and Kind apply to both
  surfaces. x is shared.", and the selected row and its partner marked ⇄.

**Implements:** §0.1 steps 3–4, §3.4, §11.1–11.4.

### 2b · A paired x move

**Shows:** upper point 10 typed from 64.36 % to 60.00 % chord; lower point 10 moves with it (rings at the old place,
arrows); the strip with proposed COPY-186.

**Implements:** §3.4 fallback, "x moves stay paired".

### 2c · Anchor → Control refused

**Shows:** the lower refit would move 0.0185 mm, over the 0.010 mm limit at 120.00 mm; the warning strip with both
numbers (proposed COPY-187) and **Show**; a red marker where the lower surface would move most; nothing changed.

**Implements:** §3.4 fallback; `SectionEdits_PairedAnchorToControlRefitOverLimit_Refused`.

### 3 · Finish blocked

**Shows:**

- Lower point 11 of 13 dragged across the upper surface, with the crossing marker.
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
- **Control → Anchor, paired** (ADR-0005 §6). On the selected (upper) surface:
  - u\* where x(u\*) = 35 %, by bisection on the monotone x;
  - five Boehm insertions (Piegl–Tiller A5.1);
  - the Δy shift of the anchor and its handles (ADR-0005 decision 5).

  On the lower surface: the same five insertions, so its vertex at u\* becomes an anchor and its shape is unchanged.
- **Tangent kind, paired:** Horizontal levels both surfaces' handles.
- **x move, paired:** the vertex's x is written on both surfaces.
- **Anchor → Control, paired:** the upper loses its handles; the lower is refitted on the new basis by least squares at
  equal x, with the nose and TE held, and measured against 10 µm at the smallest chord using the section.
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
| Points before → after | 8 → 13 on each surface. Knots equal and abscissae equal (`true`, `true`) |
| Lower shape change from the paired knots · from the paired Horizontal | 4.4 × 10⁻¹⁶ · 0.028 % chord |
| Paired x move (point 10, 64.36 → 60.00 %) | largest change upper 0.17 · lower 0.16 % chord; abscissae equal |
| Paired Anchor → Control | refused: lower refit 0.0185 mm (0.0154 % chord) at 33.4 % chord, limit 0.010 mm at 120.00 mm |
| Boehm shape change before the shift | 4.4 × 10⁻¹⁶ |
| Anchor shift Δy (= the largest change) | 0.88 % chord |
| Anchor → Control on the new anchor | 13 → 11 points, change 0.33 % chord (800 samples; the CG lens's probe: 0.31 %) |
| One-sided curvature at the anchor | upper −6.29 / −1.73, lower −0.167 / 0.643 per chord: the curvature breaks on both |
| LE radius upper · lower, own → at Root (edited state, k = 0.990) | 1.04 · 1.04 → 1.02 · 1.02 % c |
| TE gap · wedge, own → at Root | 0.00 % c · 11.42° → 0.00 % c · 11.31° |
| Own t/c | 11.26 % at 33.1 % → 12.12 % at 34.9 % |
| Crossing (lower point 11 at 10 % chord) | 77.6–89.1 % chord |
| OD-1 C | record upper point 4 raised 0.60 mm → placed upper +0.126 mm and placed lower +0.123 mm at 35 % chord |
| Anchor's nearest neighbour on screen (1:1) | 24.7 px after Fit Selection. At full chord: 8.7 px (B) and 11.9 px (A), so the glyphs overlap |
| Canvas scale at full chord | A 8.85 · B 6.44 · C 8.85 · D 8.85 px per % chord |
| Points rows visible without scrolling | A 3 of 25 · B 25 of 25 · D 25 of 25 |
| Comb teeth clipped | 50–54 per view |
| Console errors · NaN / undefined / null leaks | none · none |

**UI craft gate** (`ui-craft-gate.py`): one Minor, `side-tab` on `.sb .msg.warn`. This is a **recorded deviation**:
DESIGN.md §4 *Status strip* requires the 3 px warning rail (DR-STATUS-1 V2).

## Not shown, and why

- **Interaction.** This is a static show page, like the M1.2b2 views page. Drag, nudge, insert and delete, the Escape
  cascade and Finish are specified in the design (§6.1, §11.3) and proven by the build's rendered tests.
- **Replace from catalog and Save to My sections.** These are M1.2d. Section ▾ in the build leaves them out; it does
  not show them disabled.
- **Thickness ×2.** Off by default; the variant cards draw at 1:1 too.
