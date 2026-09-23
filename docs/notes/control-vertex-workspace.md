---
id: control-vertex-workspace
title: The vertices are the record; the workspace is four viewports and a palette — the v5 CAD model
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [cad, control-vertex, splines, levers, viewports, palette, cage, kernel, geometry]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: design-language, rel: refines}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v5, rel: relates-to}
  - {to: cad-editing-views, rel: supersedes}
  - {to: defect-classes, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: relates-to}
review-by: 2027-03-21
summary: Specification 1.3 — the geometry of record is a control-vertex B-spline per master curve (degree 3, seven vertices, levers at the ends; Fit points and Fair are constructions with reported residuals; locks are vertex constraints), the CAD workspace is four viewports with title menus and a nine-verb tool palette, the 3D body is a NURBS loft with a display cage (never a T-spline), and the geometry kernel is an owned evaluator plus OCCT and rhino3dm behind a spike gate.
review-suggested:
  - { by: mockup-workbench-v5, on: 2026-09-21, reason: "Mockup v5 (control-vertex splines, four viewports, tool palette) supersedes v4; spec 1.3 GEO-03/05/13/15, CAD-01/04/06/07/08, A4.2, A4.12, UX-24, UI-25–27; oracle tools/check-mockup-v5.mjs" }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# The vertices are the record; the workspace is four viewports and a palette

**Decision.** (1) **The control vertices are the geometry of record** (A4.1, A4.2). A master curve is a clamped
B-spline evaluated from its vertices: the end vertices lie on the curve, the second and penultimate are the
**levers** (end tangent direction and magnitude), every interior vertex *pulls* the curve and never lies on it —
the Fusion 360 control-point-spline and Rhino contract the operator asked for. The 1.2 modes are retired: *Through
points* becomes the **Fit points** construction (anchors kept as provenance, residual reported), *Smooth* with
influence weights becomes **Fair** (weighted least squares within a tolerance) — neither is an authority. Locks are
vertex constraints (root mirror P₁.y = P₀.y; value-at-tip and value-at-station pins by KKT projection with a
Newton solve for t); a locked vertex refuses and names its lock. (2) **Degree 3 with seven vertices is the default
for a master curve, and the degree is a record field**; sections stay degree 5. This departs from the knowledge
base's degree-5 reading and is named as an ADR in the spec's Open decisions rather than settled here. (3) **The
workspace is four viewports** — Top · Perspective over Front · Starboard, each with a title menu (any view
including the η-plot; Frame · Comb · Ghost; Body Smooth · Box · Cage over smooth) and double-click/Return
maximise; the curve pane and the lines-plan toggle are gone. (4) **The verbs are a tool palette** (Select ·
Insert CV · Add station · Measure · Fair · Rebuild · Fit points · Edit section · Ghost; icons with names; single
keys scoped to the focused workspace) and the parameter row is an options strip; the toolbar keeps Edit · Find ·
View and grows Draft only while a draft is open; the nudge step, locks and station removal live in Properties;
the bottom panel is the Checks drawer. (5) **The 3D body is a NURBS loft with a display cage.** The operator's
"T-spline bodies" ask is met by drawing the record's polygons over or instead of the skin; nothing is called a
T-spline (patented; no open kernel; a wing has no star points or T-junctions to need one). (6) **The geometry
kernel is named as a dependency** (A4.12): the product owns the curve evaluator (evaluation, knot insertion, the
constructions, constraints, analytic derivatives, the comb, the identity oracle) and licenses the surface work —
OCCT (LGPL 2.1 + exception, via P/Invoke) for the loft, STEP and tessellation, rhino3dm (MIT) for 3DM, geomdl and
scipy as test oracles — behind a spike whose exit evidence is written down; the kernel ADR re-decides the knowledge
base's earlier exclusion of OCCT.

**Why.** The operator found the CAD experience busy and the anchors *through* points, and named the comparables:
Fusion control-point splines and sculpt bodies, Rhino's viewports and control-point editing, Shape3d's master
curves, MultiSurf's relational NURBS. Measured on mockup v4 at 1280 × 800: 71 visible chrome controls, ten
regions, 203 text elements; the curve pane, the toolbar verbs, the parameter row and the Properties dock all
carried editing controls. The v5 mockup measures 45 on entry. The geometry choice was made with the
computational-geometry lens (a control-vertex record removes the second authority that anchors-plus-weights
created); the vertex count and degree were taken from Alias practice (few CVs, degree 3 for shaping) and are
recorded as Inferred pending the ADR.

**Rejected alternatives.** *Keeping Smooth weights beside the vertices* — two ways to say what a vertex says;
the mockup measured the polygon itself as the lever the operator was missing. *Calling the body a T-spline* — no
open kernel implements T-splines and the paradigm does not need them; the cage gives the look without the claim.
*Direct 3D vertex dragging* — still no unambiguous drag plane without a gizmo; double-click on a 3D vertex
maximises the elevation that edits it. *A ≤ 35 chrome target* — the direction brief's number was not met; 45 is
the floor of seven areas, nine verbs, four viewport titles, the Checks strip and Properties, and cutting further
removes a verb or an area (recorded in D4). *Icon-only palette* — the craft standard and the a11y floor both
require names; the palette keeps them.

**Constraints discovered.** A viewport rendered for one box and stretched to another scales its SVG, and a
scaled SVG shrinks its text and its hit targets under the floors (class UI-L): every viewport now renders at its
true pixel size and re-renders synchronously from a ResizeObserver (a deferred frame can be throttled). A closed
`<details>` menu's items still have a box and a size — the audit counted 48 of them as small targets — so closed
menus are `display:none` and the title bars are dense regions. `innerText` of a hidden viewport includes its
closed menu items, which is how the oracle found the forced-single rule firing at 586 px workspaces; the rule is
now 480 × 240. A workspace Return must stop propagating or the document-level station handler applies-and-closes
the document the same keystroke opened. The seed anchors are provenance and carry their area-scale factor rather
than being rescaled in parallel with the record. A degree-5 section fit needs centripetal parameters and knots by
averaging to meet a 10 µm acceptance with twelve vertices on a 13-point catalog; uniform index parameters gave
841 µm. The two surface lenses found what no oracle row had asked: the 2D Starboard body plan drew the nose to
the left while the Starboard camera (a right-handed frame viewed from +y) puts it on the right; positive twist
rendered nose-down and pivoted at the quarter chord against A4.4/A4.7; the cage placed twist and t/c polygons at
invented 3D positions; the root vertex claimed a "Locked by closure" lock that Properties never listed while its
lever silently rewrote it; Z/⇧Z were inverted against the recorded per-OS table; Insert CV and Measure were
pointer-only; `renderShape` and `renderPalette` were the two renderers not wrapped for focus; and the title menu
carried `role=menu` with no keyboard behaviour. Each is now an oracle row.

**Control.** `tools/check-mockup-v5.mjs` groups 6 and 13 (the record's gap, local support, levers, locks,
constructions, one draft, pointer paths, the station residual across three surfaces; the four viewports, title
menus, maximise, cage, camera, forced single viewport, quiet entry, chrome count); the spec's GEO-03/05/13/15,
CAD-01/04/06/07/08, UX-24, UI-25–27 name it as their oracle; the geometry kernel and the master-curve degree are
Open decisions with exit evidence named.
