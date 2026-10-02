---
id: mockup-m12b2-views
title: M1.2b2 views — the 3D view and the Front and Side elevations in today's shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, m12b2, 3d-view, elevations, twist, dihedral, thickness, operator-show]
links:
  - {to: design-m12b2-3d-elevations, rel: documents}
  - {to: mockup-workbench-v10, rel: refines}
  - {to: mockup-property-grid, rel: relates-to}
  - {to: mockup-status-bar, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  The operator asked to see the 3D view and elevation mockups. Only workbench-v10.html existed, and the M1.2b2 design
  has moved on from it. This page draws four screens of the 1280 × 800 shell for the Example foil: Plan + 3D (default),
  Four views, One view 3D in Wireframe, and One view Side with a twist point selected. The shell is today's: structure-B
  Properties, the V2 status strip, and 4 px gutters with 1 px frames between views. The geometry is computed from the
  fixture's B-spline channels and the FoilDSL §6 placement rule, plus a 60 mm tip dihedral added so the Front view shows
  it. Three choices the design leaves open are listed with recommendations.
review-suggested: []
---

# M1.2b2 views — the 3D view and the elevations

Open [`m12b2-views.html`](m12b2-views.html) over `file://`. The four screens are stacked. The button at the top right
switches the chrome to dark; the viewports stay graphite in every theme (M1.2b D-4). Capture:
[`screens.png`](../proof/m12b2-views/screens.png) (all four screens, light chrome, 1.5×).

| Screen | Shows | Implements |
|---|---|---|
| 1 · Plan + 3D | Plan 2* beside 3D · Iso 1*: the shaded surface (both halves), silhouette, rails, root section, the selected Tip station, ground grid, view cube, axis triad, caption | m12b2 §11.1, §11.2; DESIGN.md *Shaded surface*, *View cube*, *Point (v10)* |
| 2 · Four views | Plan \| 3D over Side \| Front. Side: root and tip at true placement, nose right, Twist lane. Front: starboard on the left, silhouette, dihedral on the LE line, t/c lane. Probe plates | §11.1 Four views, Front, Side, Probe; DESIGN.md *Elevation lane*; §11.7 |
| 3a · One view: 3D Wireframe | Display ▾ open with Wireframe chosen: no fill, ten intermediate sections | §11.1 3D *Wireframe*; §11.7 Display ▾ row |
| 3b · One view: Side | Twist point 5 of 7 selected and focused; Properties shows Type, From root, Twist; the strip shows the commit report | §11.1, §11.3, §11.4; DR-STATUS-1 |

The shell around them is today's: structure-B Properties (DR-CELL-1), the V2 status strip (DR-STATUS-1), and DR-VIEW-1
(4 px gutter between views, 1 px frame per view in the line colour). DR-VIEW-1 comes from the Coordinator; it is not
recorded under `docs/` at this base.

## How the geometry is made

Everything in the viewports is computed in the page from `src/CfdWorkbench.Desktop/Assets/example.foil`:

- the rails (LE 0 mm, TE 120 mm, half span 450 mm);
- the twist, dihedral and thickness channels as cubic B-splines (de Boor);
- section-a as two degree-5 curves under Rule A (C = (u + l)/2, T = (u − l)·max⁻¹, one profile → C ± T·t/2);
- the FoilDSL §6 placement X = L + c(x cos φ + z sin φ), Z = D + c(z cos φ − x sin φ), with the pinned constant
  0.017453292519943295;
- the 3D view: a 41 × 72 mesh (5,760 triangles), painter-sorted, headlight shading on the `viewport-soft` →
  `foil-shade-lit` ramp, and silhouette edges found where front- and back-facing quads meet.

**One change from the fixture:** the dihedral channel rises to 60 mm at the tip (the fixture's is flat), so the Front
view has something to show. The probe uses "from root" for the spanwise coordinate (MC-6), where §11.1's example
still says "span".

## Measured (Playwright, `capture-views.mjs`)

- No page errors. All four shells are 1280 × 800.
- No text under 11 px. No chrome button under 24 px. The gutter between views is 4 px.
- Fitted scales in Four views:
  - Plan: 0.472 px/mm
  - Side: 1.752 px/mm
  - Front: 0.494 px/mm
- The −2° tip twist raises the tip TE by 4.19 mm. That is 7.3 px in Four views and 19.3 px in One view Side.

## Open choices (recommendations)

1. **OI-9: perspective or orthographic in 3D.** The design makes the named axis cameras orthographic and Iso and free
   orbit perspective, with no toggle. **Recommend:** keep that and add no toggle yet. The page draws Iso in mild
   perspective.
2. **OI-10: Front and Side sharing one vertical scale.** The design lets each elevation fit itself. The fitted scales
   differ by a factor of 3.5 (Side 1.752, Front 0.494 px/mm). A shared scale would shrink Side and hide the twist.
   **Recommend:** each fits itself, with a scale bar in every view, as drawn.
3. **The selected station in the Front band.** §11.1 marks it in 3D, Plan and Side, but not in Front. **Recommend:** a
   3 px station tick at its span on the Front band, so the selection reads the same in all four views. It is not drawn
   here.
