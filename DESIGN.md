---
id: design-language
title: "CFD-Workbench — design language"
type: design-language
status: in-review
owner: Product and UX
phase: specify
tags: [ui, hydrofoil, tokens]
links:
  - { to: mockup-workbench-v6, rel: relates-to }
  - { to: design-foildsl-authoring, rel: relates-to }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-cfd-workbench, rel: relates-to }
  - { to: workbench-direction, rel: refines }
  - { to: mockup-workbench-v1, rel: relates-to }
  - { to: mockup-workbench-v2, rel: relates-to }
  - { to: mockup-workbench-v3, rel: relates-to }
  - { to: mockup-workbench-v4, rel: relates-to }
  - { to: cad-editing-views, rel: relates-to }
  - { to: thick-client-shell, rel: relates-to }
  - { to: mockup-property-grid, rel: relates-to }
  - { to: mockup-status-bar, rel: relates-to }
review-by: 2027-03-19
summary: >-
  The instrument-panel vocabulary for a cross-platform hydrofoil workbench.
  Light or dark technical panes surround one geometric canvas; a single teal
  selection joins curves, stations, and the precision inspector.
designmd_version: alpha
archetype: "ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact; Nav:TaskTabs+CommandPalette; Viewport:DesktopBound; Input:PrecisionPointer+SpatialGestures+KeyboardFirst; Color:DarkAdaptive; Type:Utilitarian; Depth:Diegetic3D; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic+Confirmed; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }"
modes: [light, dark, contrast, compact, comfortable]
colors:
  primary: "#006c67"
  on-primary: "#ffffff"
  canvas: "#f0f2f1"
  surface: "#fbfcfb"
  surface-soft: "#e8edeb"
  ink: "#1b2929"
  ink-mute: "#526362"
  hairline: "#c9d3cf"
  control-line: "#788d87"
  selection: "#d8eeea"
  success: "#24653f"
  warning: "#895900"
  danger: "#a92e37"
  focus-ring: "#006c67"
  viewport: "#17272c"
  viewport-soft: "#243a40"
  viewport-ink: "#edf4f2"
  viewport-mute: "#afc6c7"
  viewport-grid: "#344b50"
  foil: "#85c9c4"
  foil-edge: "#c4e7df"
  station: "#66ddc8"
  focus-ring-viewport: "#66ddc8"
  danger-viewport: "#ffaeb5"
  warning-viewport: "#efc576"
  foil-shade-lit: "#3f6a6c"
  cividis-0: "#00224e"
  cividis-1: "#434e6c"
  cividis-2: "#7d7c78"
  cividis-3: "#bcae6c"
  cividis-4: "#fee838"
  batlow-0: "#011959"
  batlow-1: "#215f61"
  batlow-2: "#818232"
  batlow-3: "#f19d6b"
  batlow-4: "#faccfa"
  vik-neg2: "#001261"
  vik-neg1: "#2d7ba5"
  vik-zero: "#ebe6e2"
  vik-pos1: "#c37243"
  vik-pos2: "#590008"
  dark-canvas: "#172326"
  dark-surface: "#1e2d31"
  dark-soft: "#2a3d40"
  dark-ink: "#ebf3f0"
  dark-mute: "#b2c4bf"
  dark-line: "#49605b"
  dark-control: "#79928c"
  dark-primary: "#88d8c6"
  dark-on-primary: "#172326"
  dark-selection: "#274c47"
  dark-success: "#92d1a5"
  dark-warning: "#efc576"
  dark-danger: "#ffaeb5"
  contrast-bg: "#000000"
  contrast-ink: "#ffffff"
  contrast-primary: "#ffee58"
typography:
  welcome: { fontFamily: "system-ui, sans-serif", fontSize: 32px, fontWeight: 600, lineHeight: 1.2 }
  title: { fontFamily: "system-ui, sans-serif", fontSize: 24px, fontWeight: 600, lineHeight: 1.2 }
  heading: { fontFamily: "system-ui, sans-serif", fontSize: 18px, fontWeight: 600, lineHeight: 1.3 }
  body: { fontFamily: "system-ui, sans-serif", fontSize: 14px, fontWeight: 400, lineHeight: 1.5 }
  label: { fontFamily: "system-ui, sans-serif", fontSize: 13px, fontWeight: 500, lineHeight: 1.4 }
  caption: { fontFamily: "system-ui, sans-serif", fontSize: 12px, fontWeight: 400, lineHeight: 1.4 }
  numeric: { fontFamily: "ui-monospace, monospace", fontSize: 13px, fontWeight: 400, lineHeight: 1.4 }
  prop: { fontFamily: "system-ui, sans-serif", fontSize: 11px, fontWeight: 400, lineHeight: 14px, fontFeature: "tnum lnum" }
  prop-title: { fontFamily: "system-ui, sans-serif", fontSize: 13px, fontWeight: 600, lineHeight: 18px }
  prop-note: { fontFamily: "system-ui, sans-serif", fontSize: 11px, fontWeight: 400, lineHeight: 14px }
  prop-text-scale: { values: "1, 1.25, 1.5, 2", stackedFrom: 1.5 }
rounded: { none: 0px, sm: 4px, md: 8px, pill: 9999px }
spacing:
  scale: [2, 4, 8, 12, 16, 24, 32, 48, 64]
  target: 44px
  target-dense: 32px
  dense-gap: 8px
  sidebar: 216px
  inspector: 286px
  titlebar: 64px
  taskbar: 52px
  statusbar: 36px
  rail: 68px
  menubar: 32px
  toolbar: 44px
  paramrow: 40px
  dock-left: 260px
  dock-right: 300px
  bottom: "clamp(200px, 30%, 360px)"
  bottom-tabs: 32px
  palette: 56px
  viewport-title: 32px
  shell-statusbar: 24px
  w-num-sm: 64px
  w-num-md: 80px
  w-num-lg: 160px
  prop-head: 24px
  prop-row-ro: 20px
  prop-row-input: 24px
  prop-edit-box: 20px
  prop-value: 62px
  prop-unit: 24px
  prop-indent: 22px
  prop-indent-sub: 32px
  prop-inset: 8px
  toast-w: 420px
  toast-inset: 12px
  view-gutter: 4px
  mode-bar: 32px
  mode-bar-inset: 8px
  mode-bar-gap: 4px
  mode-bar-button-pad: "0 8px"
  scope-chip-pad: "0 8px"
  mode-plate-pad: "5px 8px"
  mode-reason-pad: "6px 8px"
  mode-reason-w: 360px
  station-strip: 84px
  station-strip-inset: 4px
  station-thumb-w: 170px
elevation: { flat: "none", popover: "0 8px 24px rgba(0,0,0,0.16)" }
motion: { fast: 120ms, base: 200ms, toast-hold: 8000ms, easing: "cubic-bezier(0.2,0,0,1)" }
review-suggested:
  - { by: design-m12b2-3d-elevations, on: 2026-09-30, reason: "M1.2b2 adds token foil-shade-lit (#3f6a6c), the Shaded surface and Elevation lane rows (silhouette stroke carries the shape; ramp capped for 3:1 overlays) and updates the View cube row (24 px targets, focus-ring gap, focus relocation)." }
---

# CFD-Workbench design language

This is the proposed visual vocabulary for iteration. The spec owns behavior; this
file owns the tokens and copy. The [direction brief](docs/design/workbench-direction.md)
records the words-first rationale and evidence. The paired archetype is G1 with
local-device persistence, with G2 inside Results. The separate
[architecture decision](docs/adr/0003-application-stack.md) selects C#/.NET and
Avalonia for the first offline milestone; this file remains the token authority.

## 1. Atmosphere and hierarchy

Precise, composed, tactile. One large modeling view receives the strongest contrast.
The project tree establishes context at left; the selected object has one inspector
at right. A single lower work area exposes the active curve or section. Switch tasks
in place. No dashboard of independent cards, repeated box hierarchy, or hero copy.

## 2. Color and contrast

**Viewport-only state tokens (v4).** Focus and refusal on the graphite viewport use {colors.focus-ring-viewport}
(9.3:1 on {colors.viewport}) and {colors.danger-viewport} (8.78:1, re-measured at M1.2b), never {colors.focus-ring} or {colors.danger},
whose 2.45:1 and 2.30:1 on graphite fail SC 1.4.11; the in-artifact audit lists both pairs and the v4 oracle
measures the focused handle's stroke against the viewport background in all three themes.

**Viewport warning (M1.2b).** A lock or coupling ring on the viewport uses {colors.warning-viewport} (9.48:1 on
{colors.viewport}); {colors.warning} measures 2.56:1 on graphite and fails SC 1.4.11 there, so the v5 row's
"{colors.warning} lock ring" is corrected by the Point row below (`docs/design/m12b-points.md` §11.2).

**Shaded surface (M1.2b2).** The 3D view and the elevation bands shade the foil with one single-hue ramp from
{colors.viewport-soft} (grazing) to {colors.foil-shade-lit} (facing the headlight) — never a rainbow. The ramp stops at
{colors.foil-shade-lit} so every overlay keeps 3:1 on the brightest face: {colors.foil} 3.20:1, {colors.foil-edge}
4.54:1, {colors.station} 3.65:1, {colors.warning-viewport} 3.70:1. The fill is decorative (1.29–2.56:1 on
{colors.viewport}); the shape is carried by the edge strokes (`docs/design/m12b2-3d-elevations.md` §11.2).

The frontmatter is the palette record. The default chrome uses {colors.surface};
the modeling view uses {colors.viewport}. Selection uses {colors.primary} on light
chrome and {colors.station} on the canvas. **Colormap policy (spec v1 C2, KB-20):**
sequential magnitude uses **batlow** — {colors.batlow-0} through {colors.batlow-4},
five stops sampled at rows 1/64/128/192/256 of Fabio Crameri's 256-entry map (MIT,
© 2020 Fabio Crameri, via cmcrameri) — with cividis {colors.cividis-0} through
{colors.cividis-4} (CC0) as the documented alternative; signed fields (Cp, C𝒻,
vorticity, signed components) use the diverging **vik** map {colors.vik-neg2},
{colors.vik-neg1}, {colors.vik-zero}, {colors.vik-pos1}, {colors.vik-pos2} (same
licence) **pinned at the physical zero** on {colors.vik-zero}; jet and turbo are
rejected. Every flood or coloured series carries a legend with variable, unit, range
and its basis, map name and version, and a table twin. These are data colors, never
success or validity indicators.

The preview and mockup compute contrast from the rendered token values. Required
pairs are body and muted text on canvas/surface/soft/selection, labels on primary,
semantic text on surface, focus and control boundaries, and viewport text. A token
pair is accepted only at ≥4.5:1 for text and ≥3:1 for control/focus boundaries.
Decorative separators are measured separately and do not claim control contrast.
The measured table is recorded in the review evidence after rendering all themes.

## 3. Typography and numbers

{typography.title} names the current high-level task; {typography.heading} identifies
the selected section or tool; {typography.body} carries explanatory text;
{typography.label} is the normal instrument label; {typography.caption} carries
provenance; {typography.numeric} carries tabular, right-aligned numeric data. System
fonts use the platform face. DejaVu Sans and DejaVu Sans Mono are open substitutes.
Every physical value has a visible unit. Dimensionless values are labeled as such.
Coordinates, scales, and chart axes never rely on a tooltip for their unit.

## 4. Components and state contract

| Component | Default / hover / focus / active | Disabled / loading | Empty / error / success | Overflow |
|---|---|---|---|---|
| Task tab | Label; soft hover; visible focus ring; active label and underline | Disabled reason remains visible; workspace skeleton preserves shell | First run has an entry path; errors retain navigation | Scroll task strip without clipping focus |
| Action | Plain or primary by importance; hover surface; focus ring; pressed fill | Native disabled semantics with explanation; progress in reserved space | Recovery verb states the action; success in live status | Label wraps, never ellipsis on action |
| Quantity input | Label, value, unit; border hover; focus ring; exact text entry | Derived values say “From curve”; recomputation carries status | Missing value is “Not set”; invalid input has field text; valid commit announces | Horizontal precision text scroll; no truncation in value |
| Station row | Name + span + anchor role; linked canvas hover/selection; focus ring | Read-only selection still works; rows reserve loading space | Empty list teaches adding first station; duplicate span error links field | Full name via wrap/details; numeric columns retain width |
| Curve | Labeled axes, active handle, numeric equivalent; visible selected handle | Read-only plot retains data table; skeleton occupies same plot box | Root/tip starter points; invalid draft retains last valid surface; committed status | Panning preserves units and selected point |
| Geometry canvas | One foil, selected station plane, camera controls and summary | Recomputing overlay is labeled; last valid model explicitly marked | First-run example; invalid geometry points to field; updated revision in status | Fit-view and inspector collapse at narrow width |
| Scientific plot | Unit-bearing axes, line names, fixture and uncertainty labels | Stale stamp after input changes; no new result implied | No run → setup action; partial run identifies missing values; completed ≠ validated | Table alternate scrolls by region |
| Run lifecycle | Explicit prerequisites, geometry revision, method and conditions | Ready / queued / running / unavailable / cancelled | Setup guidance, log/error code, retry from retained inputs | Long paths wrap; large cell counts group digits |
| Result field | Named scalar, cividis legend, provenance and table alternative | Stale field stays labeled; missing field is not interpolated | Empty → run; partial field mask; success means artifact saved only | Legend never clips or disappears |

| Conditions band (A3a, as built; `docs/mockups/area3-analysis.html`; `src/CfdWorkbench.Desktop/Analysis/ConditionsBand.axaml`) | one row at the top of the model area in {colors.surface} with a {colors.hairline} rule below, only in Analysis: Speed (m/s) · Water ▾ (Salt/Fresh · 15 °C) · Depth h_ref (m, "Not set" watermark) · α (°) · **Evaluate** as a primary action ({colors.primary} fill, {colors.on-primary} label, the `modebar primary` look), then a {colors.hairline} divider and the derived q (Pa), Re_ref (`0.###E+0`, the Properties format), h/c, Fr_h, σ in {colors.ink} with tabular figures; every input carries its unit in its accessible name | hover and focus as the toolbar buttons; while a run is Running the one control reads **Cancel** (outline, not primary) and its accessible name changes; derived cells recompute as the inputs change | depth unset → h/c, Fr_h, σ read "Unavailable — depth not set"; speed ≤ 0 → "Undefined — speed ≤ 0"; a refused input shows its stable code beside the control (assertive live region) | at or under the compact width the derived group keeps q and σ and moves Re_ref, h/c and Fr_h into `More ▾`; no cell ever clips |
| Layer legend (A3a; Plan, 3D, Side/Front; `PlanLoadLayer`, `View3dLoadLayer`, `ElevationDepthLayer`) | one plate line per layer on the view, in {colors.viewport-ink} on {colors.viewport-soft}: variable, colormap, range and run ("Γ per strip · batlow 1.0 · 0–<max> m²/s · run <key>"); Γ strips use {colors.batlow-0} to {colors.batlow-4} left to right of the ramp, and the Plan's plate carries that same ramp as a bar under its text with its 0 and maximum ends (bottom right of the view, the outside-strip count stacked above it); the loading curve and the lift arrows are {colors.foil}-family strokes; the depth band and free-surface line draw in the Side and Front views | a hidden layer disappears from the canvas and from the view's accessible name (its numbers stay in the bottom panel's table twin); Historical draws the run's own revision with the Historical banner above the views | no result → no layer and no plate; outside the method envelope → a dashed outline per strip and a count plate in {colors.warning-viewport} ("dashed outline: <n> strips outside the method envelope"), never colour alone; Unavailable layer → its reason in the Layers pane | the plate wraps inside the view and never exceeds it; in 3D the lift legend starts in the caption's column, right of the axes plate, and the root-moment label slides left and wraps instead of clipping (AUX-F3, closed by POL in `docs/reviews/a3a-native.md`) |
| Analysis bottom panel (A3a; `AnalysisPanel`) | a shell slot under the dock host, Analysis only, in {colors.surface} with a {colors.hairline} rule above: a Historical banner line and an error card ({colors.danger} 1 px edge, assertive live region) when needed, then tabs **Spanwise loading · Section · Loads · Provenance** (Checks omitted: no data source, OD-4 a) in {typography.prop}, at least {spacing.prop-row-input} high, the selected tab on {colors.surface} with a {colors.primary} underline; Loads carries COPY-60 and the row "Structural: Not assessed" with COPY-227; Spanwise loading is a chart with a **Show table** twin; its axes carry five x ticks (0.00 to 1.00, root to tip) and three y ticks with faint rules, the symbols η and Cl·c/c̄ as titles (COPY-238) and the solid series named "VLM + strip" (the tier name of COPY-213); the dashed reference is named by the caption | tabs read the selected run (never the latest attempt); Running shows a static skeleton and keeps the prior run Historical | no result → COPY-206; failed → COPY-208 in the card and the prior run's tabs kept; Section → Unavailable with its reason (COPY-212) | long cells wrap; the tab content scrolls, the tab strip never does |
| Layers pane (A3a; `LayersPane`, the left side bar tab beside Properties and Browser) | one check row per layer of the selected run, at least {spacing.prop-row-input} high, in {typography.prop}, with the layer's legend beneath in {typography.prop-note} and {colors.ink-mute}; the visible flag is the controller's, the pane keeps no state | hover and focus as a check box; toggling raises one `LayersChanged` | no run → COPY-206 ("No analysis yet. Set the conditions, then Evaluate.") | the legend wraps; the pane scrolls |
| Analysis result groups (A3a; `PropertiesPane` in Analysis) | the identity, then the tier as a pill chip ({rounded.pill}, the `modebar-chip` look, text in {colors.ink-mute}; COPY-213), then the projection's groups (Wing result, Conditions, Labels, Section (2D)) and the read-only Wing last, all in one scrolling column whose bar stays visible; the Wing is not pinned in Analysis and has no scroll of its own | Conditions is collapsed at first (its numbers are in the band and the Provenance tab); a group twirls as in CAD | the Tier is a chip, never a row; Historical and Failed keep the chip and add the banner and card above the groups | at 1500 x 870 the Wing result and Labels headers and the first Labels rows are in view; the rest is one visible scroll away |
| Feasibility matrix | Constraint × operating point; satisfied / violated / Unavailable with reason; tier chip and both Ncrit per cell | Cells reserve space while recomputing; read-only for a Historical run | Empty until a goal state exists (teaches "New brief"); Unavailable cells name the reason | Seven columns scroll horizontally with a sticky first column |
| Checks drawer | Rule id · version · severity · located violation · Jump · Exclude with reason | Evaluating shows a skeleton row set; excluded rows stay listed struck | Zero findings is a stated state; a failed evaluation names the rule family | Long rule messages wrap; count in status strip |
| Assistant | Collapsed affordance with the destination's named entry point or COPY-56 | COPY-53 (no key) · COPY-54 (unevaluated) · COPY-58 (cap) | Rejected field COPY-55; withheld numeral COPY-57; explanation cites run or knowledge id | Long proposal diff scrolls inside the panel |
| Area strip | Seven areas in flow order, each with a readiness chip (text + shape) and the selected token on the current one | A gated area reads "gated (SPIKE-03/04)" and stays reachable; loading shows the chip's skeleton | Empty project: Setup is the only lit chip; an area's error is its chip text | Chips truncate their number, never their word |
| Prompt entry | Panel with the area's fixed entry name, capability disclosure, text box, Propose | COPY-53 / COPY-54 / COPY-58; "No assistant action here" in Export and Settings | Rejected field COPY-55; refused step COPY-78; withheld numeral COPY-57; proposal preview with per-field provenance and Accept · Discard | Payload preview scrolls; diff scrolls |
| Case preview | Table of cases with derived quantities and estimate; invalid rows flagged with the input named | Estimate "Not recorded" until measured | Empty grid teaches the first row; conflicting definition names both fields | 24+ cases scroll with a sticky header |
| Run console | State machine as a labelled sequence; residual history (log axis); force history; mesh-gate bars; resource meters with units | Queued rows reserve their space; "remaining Not recorded" | Unsupported, mesh-gate failed, failed, cancelled each with COPY-77/80/81/82 | Long log lines wrap in the log pane only |
| Results layer list | Layers from the evidence manifest with present/absent state and reason | Reduction in progress shows the layer's skeleton | Absent layer COPY-84; no supported criterion COPY-85 | Many samples scroll; the selected sample is sticky |
| Replay timeline | Held variable named, sample ticks, Play · step back · step forward · scrub | Paused on a failed sample with its reason | Empty when no compatible series | Long series compress ticks, never labels |
| Candidate card | Status, tier, evaluations, objective and constraint values per point, Accept opens an edit draft | — | No candidates: the experiment's terminal reason | Many candidates paginate |
| Point (v10, M1.2b — supersedes the v5 control-vertex row on the Plan view) | a point of a rail drawn on the Plan view: Anchor = 12 px hollow square on the curve, Control = 11 px filled circle off the curve, root/tip end = 14 px hollow diamond, handle = 9 px hollow circle joined to its point by a 1 px line, strokes {colors.foil} (handles {colors.focus-ring-viewport}); a 28 px hit circle; the dashed control polygon behind; a named button peer with bounds, focus, selection and Invoke whose name carries curve, index, type and span/aft in mm (handles: direction, angle, length) | default · hover (ring r 10, 1.5 px {colors.viewport-mute}) · selected (anchor, end, handle filled {colors.station}; a control point turns hollow with a 2 px {colors.station} stroke and centre dot — fill and shape, never colour alone) · focus (ring r 13, 3 px {colors.focus-ring-viewport}, 1 px viewport gap) · locked/coupled (dashed ring r 11 {colors.warning-viewport}) · draft (curve follows, crossing marker {colors.danger-viewport}) · read-only (dimmed, no hover); high contrast: {colors.contrast-ink} strokes, {colors.contrast-primary} selection and ring | drag or ←→/↑↓ nudge 0.01 · 0.1 · 1 mm (⌘ · plain · Shift) commits one undo step at release; Shift-drag locks to one axis; Escape cancels; Return types a value; Tab leaves after the last target |
| View cube | viewport corner, 3 depth-sorted faces of 6, lettered T·F·S·B·K·P | default · face hover · current view filled with {colors.station} · keyboard focus ring | face = named view; label names the camera or "Free · az · el" · M1.2b2: a face is a target only while a 24 px circle fits inside it (else View ▸ Camera is its equivalent); chevrons ≥ 24 px; the current face's letter is {colors.viewport}; the focus ring keeps a 1 px {colors.viewport} gap (it is {colors.station}-coloured on a {colors.station} face); when a focused face loses its area or the cube hides, focus moves to the current face or the view; the faces and four orbit chevrons (90°) are Buttons in Tab order ("Front view", "Orbit left 90°"); Home = Iso; no transition animation |
| Shaded surface (M1.2b2) | the placed foil (both halves) as painter-sorted triangles on the {colors.viewport-soft} → {colors.foil-shade-lit} ramp with one headlight; the silhouette (edges between front- and back-facing triangles, and boundary edges) and the LE, TE and tip outline 1.5 px {colors.foil} in every view and band — the fill is decorative, so these strokes carry the shape (SC 1.4.11); overlay text sits on a {colors.viewport-soft} plate; authored sections 1 px {colors.foil-edge}; the selected station 3 px {colors.station} with a name chip | default shaded · wireframe (no fill; ten intermediate sections 1 px {colors.viewport-mute}) · not checked (dimmed, title "· not checked") · error (last mesh kept with a note) | display only — never an edit surface; caption "Display · x aft, y starboard, z up · twist about the leading edge" |
| Elevation lane (M1.2b2) | a captioned strip under an elevation band: one channel against span (t/c under Front on the band's span axis; twist under Side, root left, zero line {colors.viewport-mute}, positive up); curve 2 px {colors.foil}, dashed polygon and Point-row glyphs | as the Point row; a clamped drag shows its reason in the probe | caption names channel, unit and span direction ("Thickness t/c (%) · tip ← root" under the Front band, "Twist (°) · root → tip"); tick labels thin when they collide |
| Control vertex (v5; superseded on the Plan view by the Point row below — its `role=slider` and {colors.warning} lock ring do not apply there) | `role=slider` vertex of a curve's control frame — every curve's frame is drawn in the elevation that shapes it, the active one at full opacity, the others at 0.62 but operable: 13 px square (interior), circle r 6.5 (lever), diamond (end vertex, on the curve), a 20 px transparent hit circle, the dashed polygon behind; η and value in the accessible value | default · selected · coupled (dashed ring on both root vertices under the root-mirror lock) · locked (a {colors.warning} lock ring, `aria-readonly`, the lock named in the accessible value) · draft open (curve solid, frame dashed) · focus ring {colors.focus-ring-viewport} ≥ 3 px | drag, ↑↓ value, ←→ η, Shift ×10 = draft; Return applies and keeps focus; Escape cancels and returns to Select; Delete removes (focus to the previous vertex); the curve is pulled, never passed through |
| Tool palette (v5) | vertical strip {spacing.palette} wide beside the workspace; nine verbs, each an icon with its visible name and its key in the accessible name; separators group edit · construct · display; at the 640 × 400 reflow preset a horizontal row above the viewports (names visually hidden, 44 px targets) | `aria-pressed` on the toggle tools only; disabled with the reason while a station document owns the verb; single keys act only with the workspace focused; Enter on a tool keeps focus on it | Escape cancels the draft and returns to Select; the options strip shows the tool's parameters and every pointer verb's keyboard equivalent (Insert at η · Add station at η · Measure between two η); a construction (Fair · Rebuild · Fit points · Insert · Delete) opens the one draft |
| Viewport title bar (v5) | {spacing.viewport-title} row: the view name as a button (double-click or Return maximises), a `details` menu (`summary` with `aria-haspopup=menu`; View · Display · Body · Maximise) whose closed items are not rendered | current view checked in the menu; opening focuses the first item; arrows and Home/End move, Escape closes and returns focus to the button, choosing an item returns focus before the items leave; maximised state restores with the same gesture | one viewport below 480 × 240 px; every viewport renders at its own pixel size (a scaled drawing is a defect, class UI-L) |
| Station document | editor-group tab with a full 2D section view; palette on the toolbar; section Properties | catalog original · draft open (tab dot) · modified · infeasible | Return applies as a Modified Profile revision; Escape or × closes and returns focus to Edit section |
| Selection identity (property grid, 2026-10-01; F-1, O-6) | the first block of Properties: the Plan view's glyph for the selection (square anchor, filled circle control, diamond end, small circle handle, three dots for several, dashed line for a station) at 12 px in {colors.ink}, the object's name in {typography.prop-title} ("Trailing edge · point 7 of 14", "Handle toward the tip", "3 points"), and a crumb in {typography.caption} {colors.ink-mute} only when it adds something the rows do not say (a handle's parent anchor as a link; never the Type value again) | one per selection, never two; a handle never reuses its anchor's name or helper | empty: COPY-136 + COPY-137, no identity · opening: the file name and skeleton rows · pane error: COPY-138 + Try again | the name wraps; it is never truncated |
| Property group (property grid; structure B, DR-CELL-1) | a twirl row {spacing.prop-head} high with no band: a 10 px chevron in {colors.ink-mute} at the left, the group name in {typography.prop} 600; a {colors.hairline} rule under the group | collapsed state remembered per group; the header is a Button with `aria-expanded`; on a point, position and tangent rows share one "Point" group; the Wing group is never collapsible (UI-36) and carries a state chip ("≈ preview", "Checking…", "Unavailable") | no rows → the group is not drawn | the summary truncates with an ellipsis; the name never does |
| Property row (property grid; structure B, DR-CELL-1 — the look is fixed by the operator) | one line per property, indented {spacing.prop-indent} ({spacing.prop-indent-sub} under a handle subhead): the label left in {typography.prop} {colors.ink-mute}; the value right-aligned, at least {spacing.prop-value} wide, in {typography.prop} with tabular lining figures; the unit after it in a {spacing.prop-unit} column in {colors.ink-mute}; a half-strength {colors.hairline} rule between rows; read-only rows {spacing.prop-row-ro}, editable rows {spacing.prop-row-input} | **editable value:** {colors.primary} text with a 1 px dotted underline offset 2 px (the non-colour cue, SC 1.4.1), no box; **enum** (Type, Tangent kind): {colors.primary} text + ▾ in {colors.ink-mute}, one commit rule — arrows stage, Return or a pick applies, Esc keeps, leaving drops and is announced (DR-CELL-2); **read-only:** {colors.ink} text, no underline, no ▾ (a lock glyph and reason when locked); **estimate:** "≈ " prefix, read-only; **focused** (Tab or click): an {spacing.prop-edit-box} box with a 1 px {colors.primary} boundary inside the 24 px band and the text turns {colors.ink} (SC 2.4.7); **error:** unfocused, a 1 px {colors.danger} box; focused, the 1 px {colors.primary} focus box with the 1 px {colors.danger} box just outside it (DR-CELL-3); always rail + icon + text; **warning / unavailable:** rail + icon + text | Return or leaving commits (one undo step); Escape restores; once the text differs from the committed value the field widens across the row so an expression shows whole (DC-1); the whole row is the target (clicking the label focuses the value); help text (descriptions, the angle reference) shows under the row while it has focus and is always its accessible description; a fact speaks "<label>, <value> <unit>[, locked]" | labels wrap, values never clip; at ≥ 150 % text the value drops under its label |
| Status strip (DR-STATUS-1, 2026-10-02; `docs/mockups/status-bar.html` V2) | one row {spacing.shell-statusbar} along the bottom of the window, under the dock host and every pane, full window width; {colors.surface} with a 1 px {colors.hairline} top rule; {typography.prop} (11 px, DR-DEN-3; scales with Text size). Left: the **last report**, one line, a 12 px icon by kind (info = check in {colors.ink}; warning = triangle in {colors.warning}; error = circled cross in {colors.danger}) and the text in the same colour; a warning or error also draws a 3 px inset rail of its colour at the strip's left edge. Right, read-only items (not buttons in this slice; YAGNI), each 24 px tall, split by a {colors.hairline} rule: selection ("TE · pt 7 of 14"), units ("mm"), the estimate note ("≈ estimates"), Text size ("Text 100 %") | No foil: the strip shows the shell's own messages ("Opening cancelled. Nothing changed."), and the selection item is absent. Busy: "Checking the last change…" until its report replaces it. An optional action (today only **Try again** for the recent-files write) sits right after the message as a 24 px button | Every report replaces the last one: **one slot, no history, no scrolling, no list** (DR-STATUS-1). A background completion never replaces a newer report (STATUS-CLOBBER). The strip is the **one polite status live region**; a field error is not repeated there (it speaks assertively at its field) | One line; the message ellipsizes at the right and its full text is the tooltip and the accessible name. At 200 % Text size the strip grows to fit one line of the larger text; the right items drop in the order Text size, ≈ estimates, units (selection stays) |
| Toast (DR-STATUS-1; warnings only) | one card at the **bottom-right of the model area**, {spacing.toast-inset} in from its right edge and {spacing.toast-inset} above the strip; width {spacing.toast-w} or the model area less 2 × {spacing.toast-inset}, whichever is smaller; {colors.surface}, 1 px {colors.control-line} border, 3 px {colors.warning} left rail, {rounded.sm}, {elevation.popover}; a 14 px warning icon, the full report text in {typography.prop} wrapped, and a 24 × 24 px **×** ("Dismiss") | Never covers the Properties dock or the strip. It takes no focus when it opens: focus stays in the field or the view that made the change | Opens for a **warning that results from a commit** (today: the typed-chord fit above the limit). The strip shows the same report, so nothing is lost when the toast closes. A warning during a gesture (the angle run stops) goes to the strip only. **Errors never toast:** a field error stays under its field; a refused gesture shows in the strip with its marker on the Plan | Stays {motion.toast-hold}, paused while the pointer is over it or focus is inside it; closes on ×, on Esc while focus is inside it, or when the next commit starts. **One at a time:** a newer warning replaces its text and restarts the hold; an info report does not close it. Keyboard: while open the toast joins the F6 ring after the model area; Esc returns focus where it came from |
| Section mode bar (M1.2c §11.1; `docs/mockups/m12c-section-editor.html` .modebar) | one row {spacing.mode-bar} at the top of the model area in {colors.surface} with a {colors.hairline} rule below, inset {spacing.mode-bar-inset}, gap {spacing.mode-bar-gap}: the title COPY-173 (12 px 600, the station in {colors.primary}), the scope chip (a {rounded.pill} hairline pill, {spacing.scope-chip-pad}, {colors.ink-mute} text, "Shared with <stations> · " then the {colors.primary} underlined link "Make unique to <station>"; "Only <station> uses this section" when no other station shares it), Section ▾, the Curvature and Thickness ×2 toggles, a spacer, Cancel (a {colors.control-line} outline) and **Finish section** (the one {colors.primary} primary). Buttons are 24 px with {spacing.mode-bar-button-pad} and {rounded.sm}; a pressed toggle is {colors.surface-soft} with a {colors.hairline} border, never the platform accent | hover {colors.surface-soft}; focus ring on each control; Finish disabled is {colors.surface-soft} with {colors.ink-mute} text and its reason in its help text | "Checking…" in the reason box while a step is assessed; the error is the Finish reason (COPY-123 / COPY-182); success is COPY-179 in the strip | the chip truncates with its full text as a tooltip; buttons never truncate |
| Section canvas plates (M1.2c §11.1; mockup .plate, .why, .lb) | one plate line along the canvas top, 6 px in: the view plate "Section · <station> · <d> mm from root · display" ({colors.viewport-soft}, {colors.viewport-ink}, {spacing.mode-plate-pad}, 600, the station underline) at the left, and at the right the reason box over the Tracing probe plate. The reason box is {colors.surface} with a 1 px {colors.danger} border and a 3 px left rail, {spacing.mode-reason-pad}, at most {spacing.mode-reason-w}, text in {colors.danger}. The comb plate "Comb · auto scale · <n> teeth clipped (×)" sits at the bottom left while Curvature is on. The chord axis is a {colors.viewport-grid} line every 10 % (5 % past 60 % zoom) labelled "<n> %" in {colors.viewport-mute} | the probe follows the pointer (focused point when outside; Δx/Δy during a drag) | the reason box shows a state reason only while its state holds; a gesture refusal stays until the draft changes | the crossing is a 4 px dashed {colors.danger-viewport} line along both curves; a refused refit is a dashed {colors.danger-viewport} mark labelled with the measured move | the probe trims with an ellipsis; the reason wraps within {spacing.mode-reason-w} |
| Station strip (M1.2c §11.1; mockup .strip, .thumb) | a row {spacing.station-strip} high in {colors.surface}, inset {spacing.station-strip-inset}: one thumbnail per station, {spacing.station-thumb-w} wide, the station's section outline drawn on {colors.viewport} (thickness ×2), then "<name> · <d> mm" and "c <chord> mm · t/c <t/c> %" in tabular figures; the current one has a {colors.station} border and 2 px underline (`aria-current`) | hover a {colors.primary} border; roving focus; arrows move along the strip | a switch with edits is refused with "Finish or cancel <station> before editing <other>." in the reason box | — | scrolls horizontally |

All controls use {rounded.sm}; panels are square joins; floating dialogs use
{rounded.md}. Primary actions are at least {spacing.target}; dense scientific
controls (nudge steps, table row actions, chart toggles) are at least
{spacing.target-dense} with {spacing.dense-gap} separation (WCAG 2.2 SC 2.5.8 with
the spacing exception); compact density reduces padding around controls, not their
target size. The property grid is the one exception: its field, header and Kind targets are {spacing.prop-row-input}
(24 px, SC 2.5.8 met by size) — density pass 2026-10-01, ruled DR-DEN-1 (accepted). Keyboard focus uses an outside ring
with a gap so both adjacent surface and the control remain identifiable.

## 5. Layout and modes

Use {spacing.scale} for spacing. Default pane widths are {spacing.sidebar} and
{spacing.inspector}; the center absorbs available space. Light and dark retarget
semantic chrome; the viewport remains graphite. High contrast replaces backgrounds
with {colors.contrast-bg}, text/boundaries with {colors.contrast-ink}, selection with
{colors.contrast-primary}; model contours and section labels remain redundant cues.

**The window is the unit (mockup v3, 2026-09-21).** The client is a fixed frame that never scrolls
as a page: menu bar {spacing.menubar} · one-row toolbar {spacing.toolbar} · an optional parameter
row {spacing.paramrow} · the work area · status bar {spacing.shell-statusbar}. The work area is
an activity rail {spacing.rail} (the six document areas in flow order, readiness in the accessible
name, then Export as a dialog, Checks and Settings at the foot), a left dock {spacing.dock-left}
(Navigator, plus the Layers section in Analysis and Results), the editor (document tabs over one
viewport or document body) with a tabbed bottom panel {spacing.bottom} that collapses to its tab
strip {spacing.bottom-tabs}, and a right dock {spacing.dock-right} (Properties, plus the Prompt
entry section). Every dock, pane and document body scrolls internally; the window never does.
The toolbar is filled from the verb table per area and **measured**: groups that do not fit move,
from the tail, into `More ▾`; at 1,280 px and above every group is visible, at the 1,024 × 700
minimum at most one group is hidden. Numeric inputs use {spacing.w-num-sm} / {spacing.w-num-md} /
{spacing.w-num-lg}; the parameter row is one row and never wraps. **The workspace is four viewports (mockup v5, 2026-09-21):** Top · Perspective over Front · Starboard,
the lines drawing, each with a {spacing.viewport-title} title bar (name button + title menu) and maximised
by double-click or Return; the **tool palette** {spacing.palette} stands beside them with nine verbs (icon
*with* name; single keys scoped to the focused workspace); the parameter row is an **options strip** (curve
selector · the active tool's options · the draft chip · derived readouts); the application toolbar keeps
Edit · Find · [Draft] · View; the nudge step, the locks and station removal live in Properties; the bottom
panel is the Checks drawer in CAD (Catalog · Checks on a Station document); the Navigator and the bottom
panel start collapsed on the first entry to CAD; below 480 × 240 px the workspace shows one viewport. The 1.2
curve pane and lines-plan toggle are gone. *Earlier rule (v3–v4):* the curve editor's own palette lived in a
bottom pane and the lines-plan was a toggle at ≥ 1,440 px. **Document tabs are documents, not areas**: one tab per
open design (dirty dot until saved), Settings opens beside it, and the rail selects the perspective.
Docks and the bottom panel are resized by **sashes** (pointer drag, arrow keys, double-click resets to
the token) and collapse behind named toggles that leave a visible expand control in a 24-px gutter;
at the 640 × 400 reflow preset (WCAG 1.4.10) the docks become drawers over the editor opened from the
document-tab row and closed with Escape, and the bottom panel starts collapsed. Under macOS the top
row is the title bar (menus are system-owned); under Windows it is the menu strip; shortcut labels
follow the platform (⇧⌘A vs Ctrl+Shift+A). Native window chrome, per-monitor DPI change mid-drag
(Windows PerMonitorV2), Retina ↔ non-Retina moves and 125 %/150 % fractional scaling of 1-px borders
are **unproved** by this artifact. This is a desktop review artifact; mobile authoring is not promised.
Comfortable density adds grouping space.

*Earlier rule (mockups v1–v2, superseded):* the page scrolled and the panes stacked below 900 px.
That shape was measured at 1,450–6,500 px tall at every width and is recorded as defect class
UI-H2 (web-page habits in a client mockup); the browser oracle now fails on any window scroll or
toolbar wrap.

macOS uses Command shortcuts and system menus; Windows uses Control shortcuts and
Windows menus. The HTML shortcut hint detects platform for the review only. Native
window chrome, assistive technology, DPI, signing, and distribution remain unproved.

## 6. Motion inventory

| Moment | Purpose | Timing | Reduced motion |
|---|---|---|---|
| Hover/focus color | Confirm interaction | {motion.fast}, {motion.easing} | Instant |
| Property group chevron | Show a group opening or closing | {motion.fast}, {motion.easing} (mockup); none natively (HardCut, PG-16) | Instant |
| Status appearance | Explain completed local change | Instant, reserved status area | Instant |
| Warning toast | Draw attention to a commit's warning without moving focus | Fade in {motion.fast}; hold {motion.toast-hold} (paused on hover or focus); fade out {motion.fast} | Appears and disappears instantly; the hold is unchanged |
| Camera manipulation | Maintain spatial orientation | Direct response, no inertia | Direct response |
| Task/state switch | Change work context | Hard cut | Hard cut |
| Loading skeleton | Reserve shape during wait | Static, no shimmer | Static |
| Operating-point replay *(reserved with S6)* | Inspect a sweep at held speed or held angle | Explicit Play; 900 ms per discrete sample; hard cut | Same explicit controls; no tween or automatic startup |
| Preview regeneration | Show the candidate curve replacing the accepted one | Hard cut, status text "Preview open" | Hard cut |

No decorative motion, automatic rotating model or animated layout dimensions is
included. Sweep replay changes operating points, never physical transient time.
Camera, rake, slice and scalar range remain fixed during playback; missing samples
pause and clear absent quantities. The mockup has no transient physics data.

## 7. Copy record

The following are the oracle strings for review; quote them exactly in checks.

| ID | Copy added by this section |
|---|---|
| COPY-01 | Five curves. One parametric shape. |
| COPY-02 | Stations anchor section identity. Curves control how the shape changes between them. |
| COPY-03 | Illustrative data · uncertainty not quantified |
| COPY-04 | Geometry changed. Results belong to revision 12. |
| COPY-05 | This edit detaches the starter recipe. The explicit model stays parametric. |
| COPY-06 | Chord must be greater than 0 mm. Your last valid shape is still visible. |
| COPY-07 | Backend unavailable. Save the setup or select another configured backend. |
| COPY-08 | Start with a foil, then make it yours. |
| COPY-09 | Create foil |
| COPY-10 | Open project |
| COPY-11 | Open example |
| COPY-12 | Restore last valid value |
| COPY-13 | Rebuilding preview… |
| COPY-14 | No solver ran. This is an interaction prototype. |
| COPY-15 | Missing pressure values are masked, not filled. |
| COPY-16 | Data retained. Review the failed step before retrying. |
| COPY-17 | No estimate is available for discretization or model-form uncertainty. |
| COPY-18 | Read-only review. Select any item to inspect its definition. |
| COPY-19 | Through points |
| COPY-20 | Smooth · weighted controls |
| COPY-21 | Edit section |
| COPY-22 | Operating-point replay · not physical transient time. Camera, slice, rake and scalar range stay fixed. Missing samples pause playback. |
| COPY-23 | Preview sweep results |
| COPY-24 | Sample unavailable |
| COPY-25 | Modeled turbulent kinetic energy k · m²/s² |
| COPY-26 | Signed wall-shear coefficient C𝒻 · dimensionless |
| COPY-27 | Field unavailable |
| COPY-28 | Example · race light — Illustrative, not computed for this design |
| COPY-29 | Example fixture missing or corrupt: <file> — nothing was overwritten · New · Open — superseded by COPY-143 in the M1 start card (1.6) |
| COPY-30 | This file was saved by a newer version (<n>) — not opened; your active document is unchanged · <path> |
| COPY-31 | Save failed: <cause> — the previous file is intact and your changes are kept · Retry · Save as |
| COPY-32 | A recovery revision from <time> exists beside the saved one · Compare · Keep saved · Use recovery |
| COPY-33 | Influence control (weight w) |
| COPY-34 | Evaluated station value |
| COPY-35 | Locked by <source> |
| COPY-36 | Preview open — deviation <d> · <n> locks |
| COPY-37 | Weight must be a positive finite number |
| COPY-38 | Cannot satisfy <n> locks with <k> degrees of freedom · Release: <list> |
| COPY-39 | Flagged — <reason> · Acknowledge to continue |
| COPY-40 | Pending admission — <reason> |
| COPY-41 | Conversion residual <d> (acceptance <a>) |
| COPY-42 | Structural: Not assessed |
| COPY-43 | geometry only (Structural: Not assessed) |
| COPY-44 | practitioner range; no measured water N-factor; Day 2019 used 4 |
| COPY-45 | Unavailable — depth not set |
| COPY-46 | Deep-water result; free surface not modelled (h/c = x, Fr_h = y; effects measured below h/c 5) |
| COPY-47 | Static geometry; steady analysis cannot predict ventilation onset; onset is dynamic and hysteretic |
| COPY-48 | Cavitation screening (sheet, by −Cp_min): inception is possible above V_crit; not a prediction of inception, extent, tip-vortex or cloud cavitation; Cp_min resolution: N stations |
| COPY-49 | Out-of-envelope observation — <bound, source, date> |
| COPY-50 | Tiers disagree: δ = <v> — Discrepancy record stored · value preferred by reported uncertainty: <tier> |
| COPY-51 | Model uncertainty not quantified |
| COPY-52 | Unavailable — outside the ITTC table (0–50 °C) |
| COPY-53 | No API key configured — every design and analysis tool works without it · Configure key |
| COPY-54 | Unevaluated on this model — proposals disabled until the eval suite passes |
| COPY-55 | <field>: <value> outside <bound> — not applied |
| COPY-56 | No assistant action here |
| COPY-57 | Response withheld: it contains a number not in the shared context |
| COPY-58 | Assistant cap reached (<per-request/daily>) — raise it in Settings or wait |
| COPY-59 | STEP export unavailable until the open-and-measure fixture exists |
| COPY-60 | Loads are hydrodynamic estimates. Not a structural assessment. Strength, stiffness and fatigue are not evaluated. |
| COPY-61 | This geometry has not been checked for strength, manufacturability or ride safety. No standard for hydrofoil-wing strength applies (RCD 2013/53/EU excludes hydrofoils and surfboards; ISO 25649 excludes rigid surf-sport devices). Test before use. |
| COPY-62 | Trailing edge <t> mm below the floor <f> mm (practitioner value, unverified) |
| COPY-63 | Computed estimate · Model uncertainty not quantified |
| COPY-64 | Historical — <what changed> |
| COPY-65 | single-point comparison — not a recommendation |
| COPY-66 | XFOIL-class surrogate; accuracy relative to XFOIL, not experiment |
| COPY-67 | Pending admission — terms requested from <source> · GEN sections remain |
| COPY-68 | Illustrative — not computed for this design |
| COPY-69 | Undefined — <cause> |
| COPY-70 | Unavailable — <reason> |
| COPY-71 | AR (projected, b²/S) |
| COPY-72 | Section-based inference |
| COPY-73 | Targets conflict: <a> and <b> — seeded the nearest feasible design; targets stay as preferences |
| COPY-74 | Single-point optimization fills the design to one condition and degrades the others (Drela 1998; Garg 2017) — add at least a second operating point or accept the multipoint default |
| COPY-75 | Backend not ready — <substrate> <version>: <what is missing> · Check · Prepare my environment |
| COPY-76 | Step <n> of <m>: <action> — runs `<command>` · consequence: <text> · terms: <link, verbatim> |
| COPY-77 | Unsupported on <backend>: <capability missing> — case stays Pending |
| COPY-78 | Step refused: outside the allow-list — <reason> |
| COPY-79 | Describe a starting design |
| COPY-80 | Mesh gate failed: <measure> <value> vs threshold <t> — stopped before solving |
| COPY-81 | Solving · iteration <i> · residuals <r> · elapsed <t> · remaining Not recorded |
| COPY-82 | Cancelled — process tree terminated at the substrate; partial outputs retained |
| COPY-83 | Failed — no outputs (exit 0 but the final time directory is missing) |
| COPY-84 | Unavailable — <field missing in run · not computed · failed> |
| COPY-85 | No supported criterion in this sample — τ_w absent; vortex-core candidates are not separation |
| COPY-86 | Parametric sweep — sequence over admitted samples · held <speed or angle> |
| COPY-87 | Moving dashes are an affordance, not fluid motion |
| COPY-88 | <status> · tier <t> · <n> evaluations · Accept opens an edit draft |
| COPY-89 | Fusion-ready STEP (closed shell) — Fusion has no public native format |
| COPY-90 | Converged: residuals <r> · outputs complete · grid uncertainty <U or not quantified (single mesh)> |
| COPY-91 | Describe a change to the shape |
| COPY-92 | Ask about this calculation |
| COPY-93 | Describe the experiment |
| COPY-94 | Prepare my environment |
| COPY-95 | Explain this failure |
| COPY-96 | Ask about this result |
| COPY-97 | gated (SPIKE-03/04) |
| COPY-98 | <curve> control vertex <i> of <n> (tangent lever · end, on the curve · interior) |
| COPY-99 | Locked by station value (tip) — release the lock in Properties to move this vertex |
| COPY-100 | Apply or cancel the open <curve> draft first (one draft at a time) |
| COPY-101 | Fair · deviation <d> mm above the <t> mm tolerance — Apply disabled |
| COPY-102 | Conversion residual <r> µm (acceptance 10 µm at local chord · measured at the catalog points) |
| COPY-103 | “<file>” didn't open. It was saved by a newer version of CFD Workbench. The file hasn't been changed. |
| COPY-104 | Opening <file>… |
| COPY-105 | Opening cancelled. Nothing changed. |
| COPY-106 | Enter a length greater than 0 mm. <Dimension> is unchanged. |
| COPY-107 | That would make the leading and trailing edges cross. Enter a different value. |
| COPY-108 | Tip closes — edit the tip station |
| COPY-109 | Pending admission — terms requested from UIUC · GEN sections remain |
| COPY-110 | Cite only (LINK) — its coordinates may not be copied, so it cannot be edited here |
| COPY-111 | Catalog original · <source> |
| COPY-112 | Modified from <source> |
| COPY-113 | Name the section to save it. |
| COPY-114 | “<name>” is already in My sections. Choose another name. |
| COPY-115 | No sections match “<query>”. Try NACA, Eppler or a name. |
| COPY-116 | <Pane> moved so it doesn't cover <target> |
| COPY-117 | A control point pulls the curve toward it. The curve does not pass through it. |
| COPY-118 | Enter a number. <Field> is unchanged. |
| COPY-119 | This section has unsaved changes. Cancel discards them; Finish keeps them. |
| COPY-120 | <Pane> docked so it doesn't cover <target> |
| COPY-121 | <n> sections match |
| COPY-122 | Set these in the workspace. |
| COPY-123 | ⚠ Upper and lower surfaces cross. Move the point back to finish. |
| COPY-124 | Surfaces no longer cross. Finish is available. |
| COPY-125 | “<file>” didn't open. It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed. · Locate… · Open another file… · Remove from Recent |
| COPY-126 | “<file>” didn't open. CFD Workbench isn't allowed to read it. The file hasn't been changed. Check its permissions in Finder, or open another file. · Open another file… |
| COPY-127 | “<file>” didn't open. It couldn't be read from the disk. The file hasn't been changed. · Try again · Open another file… |
| COPY-128 | “<file>” didn't open. It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed. · Open another file… |
| COPY-129 | “<file>” didn't open. It is larger than CFD Workbench can open (<limit>). The file hasn't been changed. · Open another file… |
| COPY-130 | “<file>” didn't open. It contains parts this version doesn't understand. The file hasn't been changed. A newer version of CFD Workbench may open it. · Open another file… |
| COPY-131 | “<file>” was saved by an earlier version. CFD Workbench can open a converted copy; the original stays as it is. · Open a copy · Cancel |
| COPY-132 | No saved sections yet. To keep one here, choose Save to My sections… from the Section menu in the section editor. |
| COPY-133 | The catalog didn't load: <cause>. Your section hasn't changed. Choose Cancel to go back. |
| COPY-134 | a catalog file is missing from this installation |
| COPY-135 | a catalog file failed its check |
| COPY-136 | No foil open |
| COPY-137 | Open or start a foil. Whatever you select in it shows its properties here. |
| COPY-138 | <Pane> couldn't be shown. Your foil hasn't changed. · Try again |
| COPY-139 | <Pane> couldn't be shown. · Try again |
| COPY-140 | “<file>” has no control-point IDs. CFD Workbench can add them. The file hasn't been changed. · Accept candidate IDs |
| COPY-141 | “<file>” couldn't be checked, so it wasn't opened for editing. The file hasn't been changed. |
| COPY-142 | The candidate IDs couldn't be accepted. The file hasn't been changed. |
| COPY-143 | “<file>” didn't open. The built-in example is missing or damaged. Nothing was overwritten. · New foil · Open another file… |
| COPY-144 | Dismiss |
| COPY-145 | The new span couldn't be checked. Span is unchanged. Try again or enter a different value. |
| COPY-146 | The recent-files list wasn't cleared: <reason>. The list is unchanged. · Try again |
| COPY-147 | it was saved by a newer version of CFD Workbench |
| COPY-148 | it couldn't be saved |
| COPY-149 | An anchor point is on the curve. Its handles set the curve's direction on each side. |
| COPY-150 | Handles stay in line. Their lengths can differ. |
| COPY-151 | Handles stay in line and equal in length. |
| COPY-152 | Each handle moves on its own. The curve can turn a corner here. |
| COPY-153 | Anchor point — adds handles (rail gains up to 3 points) |
| COPY-154 | <Curve> point <i> is now an anchor point with 2 handles. The rail gained <k> points (<m> → <n>). Largest change <d> mm. |
| COPY-155 | Unavailable — <reason>. Undo, or edit again, to recompute. |
| COPY-156 | Square to the centre line (root mirror). Only its length can change. |
| COPY-157 | <typed> = <value> <unit>. |
| COPY-158 | Enter an angle between −90° and 90°, from the span axis, + aft. <Field> is unchanged. |
| COPY-159 | This is the root chord. Typing here moves only this point; Root chord under Wing rescales the planform. |
| COPY-160 | <What> unavailable — <reason>. |
| COPY-161 | <typed> = <value> <unit> (set once; doesn’t follow <reference>) |
| COPY-162 | Keeps this handle; the other one moves. |
| COPY-163 | Up and Down arrows step 0.1 <unit>; with Command (Ctrl on Windows) 0.01; with Shift 1. Release to apply; Esc cancels. |
| COPY-164 | Angles are measured from the span axis, + aft. |
| COPY-165 | <Curve> point <i> is now a control point. Its handles are removed; the rail has <n> points (was <m>). Largest change <d> mm. |
| COPY-166 | Press Return to change the type, or Esc to keep it. |
| COPY-167 | Press Return or Space to make it <kind>, or Esc to keep <kind>. |
| COPY-168 | <typed> typed; set to <value>, the <largest or smallest> that can be checked. |
| COPY-169 | <value> % — for 12 %, type 12 or 0.12 × 100. |
| COPY-170 | Stops here: the angle stays between −90° and 90° from the span axis. |
| COPY-171 | Changing one handle's twist moves the other onto the line. |
| COPY-172 | Edit section… |
| COPY-173 | Editing <station> section |
| COPY-174 | Upper point <n> is now an anchor (now point <m> of <N>). Upper surface <a> → <b> points; lower unchanged. Largest change <d> % chord. Curvature now breaks at <x> % chord. (and the mirror for lower; superseded for paired types by COPY-185) |
| COPY-175 | Upper point <n> is now a control point. Upper surface <a> → <b> points. Largest change <d> % chord. |
| COPY-176 | Moved upper point <n> by <d> % chord. Own t/c <t> %; <station> stays <s> % t/c. |
| COPY-177 | The nose is always an anchor. It stays at the leading edge with a vertical tangent. |
| COPY-178 | The trailing-edge point is always an anchor. It moves up and down only. / closed: … It stays on the chord line: the trailing edge is closed. |
| COPY-179 | Finished <station> section: <n> changes in one undo step. |
| COPY-180 | Cancelled. <station> section is as it was. |
| COPY-181 | Nothing to undo in this section. |
| COPY-182 | This section couldn't be checked, so Finish is off. <reason> Undo the last change or try another. |
| COPY-183 | Angles are in the section's own chord coordinates. A flat crest stays flat at a station only if the section is symmetric or uses its own thickness. |
| COPY-184 | A vertical tangent inside a surface makes a step. The nose already has one. |
| COPY-185 | Point <n> is now an anchor on both surfaces (point <m> of <N>; <a> → <b> points each). Largest change <d> % chord, <surface>; <other> shape unchanged. Curvature now breaks at <x> % chord. |
| COPY-186 | Moved point <n> on both surfaces: x <x0> → <x1> % chord. Own t/c <t> %; <station> stays <s> % t/c. |
| COPY-187 | Point <n> stays an anchor. As a control point the <other> surface would move <d> mm, over the <limit> mm limit at <c> mm chord. Nothing changed. (<d> to 0.0001 mm, so a move just over the limit never reads as equal to it) |
| COPY-188 | Type and Kind apply to both surfaces. x is shared. |
| COPY-189 | Paired with <other> point <n>. Type, kind and x are shared. |
| COPY-206 | No analysis yet. Set the conditions, then Evaluate. — approved — Ruling 82 |
| COPY-207 | Evaluating — VLM + strip · <n> panels… — approved — Ruling 82 |
| COPY-208 | Analysis failed — <reason> (<code>). The previous result is kept as Historical. — approved — Ruling 82 |
| COPY-209 | Analysis complete — VLM + strip · <t> s — approved — Ruling 82 |
| COPY-210 | Unavailable — no polar method installed — approved — Ruling 82 |
| COPY-211 | Unavailable — run payload failed its check — approved — Ruling 82 |
| COPY-212 | Unavailable — no section Cp method (DR-ANA-2) — approved — Ruling 82 |
| COPY-213 | VLM + strip · local calculation — approved — Ruling 82 |
| COPY-214 | Estimator · local calculation — approved — Ruling 82 |
| COPY-215 | Polar · local calculation — approved — Ruling 82 |
| COPY-216 | outside the verified lattice family — approved — Ruling 82 |
| COPY-217 | Verified fixture family: rectangular and elliptic planforms; ±20° dihedral at 32 × 4 cosine span, uniform chord (F-8); 45° sweep, AR 5, at 4 × 1 uniform (F-16); 4% camber at 32/64/128 × 4 cosine/cosine (F-18); 1° washin at 32/64/128 × 4 cosine/cosine (F-19); F-6 order at 32/64/128 × 4 cosine span, uniform chord; F-21 at 16 × 4 cosine/cosine per half — approved — Ruling 82 |
| COPY-218 | provisional — tip law cannot judge this strip (ANA-TIP-PROVISIONAL) — retired — Ruling 92 |
| COPY-219 | at the bound (+-U) — retired — Ruling 92 |
| COPY-220 | Not judged — tip strip — approved — Ruling 92 |
| COPY-221 | inside the method envelope — approved — Ruling 82 |
| COPY-222 | outside the method envelope — approved — Ruling 82 |
| COPY-223 | attached flow; no stall; no ventilation; deep water — approved — Ruling 82 |
| COPY-224 | attached flow; no stall; no ventilation; free surface not modelled — approved — Ruling 82 |
| COPY-225 | Not modelled: ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”. — approved — Ruling 82 |
| COPY-226 | Not modelled: free surface, ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”. — approved — Ruling 82 |
| COPY-227 | Not assessed: take-off, pumping, breach and slam, ventilation shock, impact, fatigue. — approved — Ruling 82 |
| COPY-228 | Unavailable — station depth not recorded — approved — Ruling 82 |
| COPY-229 | Unavailable — root thickness not recorded — approved — Ruling 82 |
| COPY-230 | Undefined — CD ≤ 0 — approved — Ruling 82 |
| COPY-231 | Near-field diagnostics flagged outside the verified lattice family — approved — Ruling 82 |
| COPY-232 | e below 0.85; result remains available — approved — Ruling 82 |
| COPY-233 | 1 < e ≤ 1.02 at 64 × 4 — small lattice bias (~+0.01); result remains available — approved — Ruling 82 |
| COPY-234 | Unavailable — needs −Cp_min — approved — Ruling 82 |
| COPY-235 | Unavailable — strip width not recorded; vector omitted — approved — Ruling 82 |
| COPY-236 | Body axes: +x aft, +y starboard, +z up; lift and drag in wind axes — approved — Ruling 82 |
| COPY-237 | Root bending moment about the root plane; positive sense about +x — approved — Ruling 82 |
| COPY-238 | Spanwise loading Cl·c/c̄ vs η; dashed elliptic reference at the same CL — approved — Ruling 82 |
| COPY-239 | Strip of wing run (α_eff) · η <η> — approved — Ruling 82 |
| COPY-240 | e above 1 — check the lattice — approved — Ruling 82 |
| COPY-241 | Unavailable — tip chord under the minimum (<min>). The tip is not certified for analysis. — approved — Ruling 94 |
| COPY-242 | Tip chord can't go below <min> (the larger of 5 mm and 2 % of the root chord). — approved — Ruling 94 |
| COPY-243 | Tip chord can't go below <min> (the larger of 5 mm and 2 % of the root chord). Enter <min> or more. — approved — Ruling 96 (typed entry; Ruling 94's text plus the way out; COPY-A) |
| COPY-244 | Tip chord is at its minimum, <min>. — approved — Ruling 96 (drag and nudge hold, tip; COPY-B) |
| COPY-245 | Root chord is at its maximum, <max>, for a <tip> tip. Widen the tip first. — approved — Ruling 96 (drag and nudge hold, root; COPY-C) |
| COPY-246 | Root chord can't go above <max> while the tip chord is <tip> (the tip must stay at least 2 % of the root). Widen the tip first. — approved — Ruling 96 (typed Root chord refused; COPY-D) |
| COPY-247 | This tip is already under the minimum. It can't go lower. — approved — Ruling 96 (legacy file; COPY-E) |
| COPY-248 | <value> mm · minimum — approved — Ruling 96 (held tip chord in the Wing block; COPY-F) |
| COPY-249 | Use <value> — approved — Ruling 96 (action on a refused typed chord, never applied by itself; the Ruling's "Use <min>") |
| COPY-250 | Unavailable — <reason> — approved — Ruling 101 (the COPY-70 form wherever a value cell read a bare "Unavailable": AnalysisProjection.cs VerdictUnavailable and Val, the Wing loading cell, the failed-run Result row; the reason is the code's own) |
| COPY-251 | ; <n> strips: Unavailable — <reason> — approved — Ruling 101 (run-sentence suffix for strips with no verdict (AnalysisProjection.cs RunVerdict)) |
| COPY-252 | ; <n> Not judged — tip strip — approved — Ruling 101 (run-sentence suffix for the provisional tip strips (COPY-220 text)) |
| COPY-253 | Unavailable — this run's geometry revision is not held by this session; verdicts, stations and normals omitted — approved — Ruling 101 (unheld-revision note (Labels.FeedRevisionNotHeld)) |
| COPY-254 | Inside the method envelope at this strip (<parts>) — approved — Ruling 101 (per-strip verdict; <parts> reads as in the next row; extends COPY-221) |
| COPY-255 | Outside the method envelope at this strip — exceeded: <names> (<parts>) — approved — Ruling 101 (per-strip verdict; extends COPY-222) |
| COPY-256 | <name> <value><unit> ≤ <bound><unit> — approved — Ruling 101 (one part of a verdict; reads > when that quantity is the one exceeded; names are |α_eff − α_L0|, Cl_local, sweep) |
| COPY-257 | Inside the method envelope (\|α_eff − α_L0\| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) at all <n> strips — approved — Ruling 101 (run sentence, all strips inside) |
| COPY-258 | Outside the method envelope (\|α_eff − α_L0\| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) — <k> of <n> strips; exceeded: <names> — approved — Ruling 101 (run sentence, some strips outside) |
| COPY-259 | Unavailable — no attachment point named (DR-ANA-5) — approved — Ruling 101 (Moment about attachment point (Loads.AttachmentReason)) |
| COPY-260 | Analysis: no result — approved — Ruling 101 (status-strip item (AnalysisProjection.cs StatusText)) |
| COPY-261 | Analysis: Unavailable — approved — Ruling 101 (status-strip item (AnalysisProjection.cs StatusText)) |
| COPY-262 | Analysis: Failed — approved — Ruling 101 (status-strip item (AnalysisProjection.cs StatusText)) |
| COPY-263 | Analysis: Current — approved — Ruling 101 (status-strip item (AnalysisProjection.cs StatusText)) |
| COPY-264 | Analysis: Historical — approved — Ruling 101 (status-strip item (AnalysisProjection.cs StatusText)) |
| COPY-265 | Unavailable — surface piercing — approved — Ruling 101 (Tip depth cell when the tip is at or above the surface (COPY-45 covers "depth not set" only)) |
| COPY-266 | Undefined — speed ≤ 0 — approved — Ruling 101 (derived condition cells (Conditions group and the band) when the speed is not above zero) |
| COPY-267 | tip depth <value> m — approved — Ruling 101 (3D and side-view depth annotation) |
| COPY-268 | free surface and tip depth, values in the conditions table — approved — Ruling 101 (alt-text clause after "; " when the depth layer is visible (View3d.LoadLayer.cs)) |
| COPY-269 | dashed outline: <n> strip outside the method envelope — approved — Ruling 101 (plan-view legend, <n> = 1 (singular)) |
| COPY-270 | dashed outline: <n> strips outside the method envelope — approved — Ruling 101 (plan-view legend, <n> other than 1) |
| COPY-271 | Historical — previous result — approved — Ruling 101 (banner while a failed run shows the previous result (an instance of COPY-64)) |
| COPY-272 | Preview hidden — Apply or Cancel in CAD — approved — Ruling 101 (model-area notice while a CAD preview is pending) |
| COPY-273 | Outside strips have dashed outlines and a text count. — approved — Ruling 101 (note on the Γ-per-strip layer) |
| COPY-274 | The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run. — approved — Ruling 101 (tampered-run note under COPY-211, mockup screen 8) |
| COPY-275 | η (root → tip) — approved — Ruling 101 (Spanwise loading chart, x-axis title) |
| COPY-276 | Cl·c/c̄ (–) — approved — Ruling 101 (Spanwise loading chart, y-axis title) |
| COPY-277 | dashed: elliptic, same CL — approved — Ruling 101 (Spanwise loading chart, legend) |
| COPY-278 | VLM + strip — approved — Ruling 101 (Spanwise loading chart, series label (its own row; also a fragment of COPY-213)) |
| COPY-279 | Historical · VLM + strip — approved — Ruling 101 (tier chip while the run is Historical or Failed) |
| COPY-280 | 10 kn · salt 15 °C · as the band — approved — Ruling 101 (Conditions group summary line (mockup values; the units follow the Units setting)) |
| COPY-281 | Moving <n> <curve> points. — approved — Ruling 107 (DR-GM-7, COPY-G1) (docs/design/group-move-node-m.md section 5, row COPY-G1) |
| COPY-282 | Moved <n> <curve> points. Tip chord <value> mm. — approved — Ruling 107 (DR-GM-7, COPY-G2) (docs/design/group-move-node-m.md section 5, row COPY-G2; the tip clause shows only when an end vertex moved) |
| COPY-283 | <Point name> is locked. Deselect it to move the others. — approved — Ruling 107 (DR-GM-7, COPY-G3) (docs/design/group-move-node-m.md section 5, row COPY-G3) |
| COPY-284 | The <point> can't move along the span, so the selection moves aft only. — approved — Ruling 107 (DR-GM-7, COPY-G4) (docs/design/group-move-node-m.md section 5, row COPY-G4) |
| COPY-285 | Handles move on their own, or with their anchor. Deselect the handle or select its anchor. — approved — Ruling 107 (DR-GM-7, COPY-G5) (docs/design/group-move-node-m.md section 5, row COPY-G5) |
| COPY-286 | The selection is held by point <n>. Points can't close up on a neighbour. — approved — Ruling 107 (DR-GM-7, COPY-G6) (docs/design/group-move-node-m.md section 5, row COPY-G6) |
| COPY-287 | Select points on one curve to move them together. — approved — Ruling 107 (DR-GM-7, COPY-G7) (docs/design/group-move-node-m.md section 5, row COPY-G7) |
| COPY-288 | Moving these points by <typed> would take the tip chord below <min>. The most they can move that way is <amount>. — approved — Ruling 107 (DR-GM-7, COPY-G8) (docs/design/group-move-node-m.md section 5, row COPY-G8) |
| COPY-289 | Points can't share a position along the span. Move them by an amount instead. — approved — Ruling 107 (DR-GM-7, COPY-G9) (docs/design/group-move-node-m.md section 5, row COPY-G9) |
| COPY-290 | Moving these points by <typed> would pass point <n>. The most they can move that way is <amount>. — approved — Ruling 107 (DR-GM-7, COPY-G10) (docs/design/group-move-node-m.md section 5, row COPY-G10) |
| COPY-291 | Set <row> of <n> points to <value>. — approved — Ruling 107 (DR-GM-7, COPY-G11) (docs/design/group-move-node-m.md section 5, row COPY-G11) |
| COPY-292 | Moved <n> points by <signed value>. — approved — Ruling 107 (DR-GM-7, COPY-G12) (docs/design/group-move-node-m.md section 5, row COPY-G12) |
| COPY-293 | inviscid + turbulent-friction bound; deep water; steady · inviscid; no boundary layer — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 3: Cp and estimator fixed label; depth unset: "free surface not modelled" replaces "deep water") |
| COPY-294 | Cp · vik pinned at 0 · −a to +b — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 5: Section view legend) |
| COPY-295 | cl (panel) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 6: Section result row label) |
| COPY-296 | Cm c/4 — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 6: Section result row label) |
| COPY-297 | α_L0 (panel) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 6: Section result row label) |
| COPY-298 | Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 7: estimator cd note; also the estimator-sourced profile drag, row 47) |
| COPY-299 | at x/c <x> on the <side> surface · 200 stations · three trailing-edge panels per side excluded — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 8: −Cp_min note) |
| COPY-300 | 15 % margin — practitioner assumption, not sourced — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 13: cavitation margin label) |
| COPY-301 | Clear — σ is above −Cp_min plus the margin — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 14: margin state) |
| COPY-302 | Inside the margin — σ is above −Cp_min but within the margin — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 15: margin state) |
| COPY-303 | Possible — σ is at or below −Cp_min; speed is above V_crit — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 16: margin state) |
| COPY-304 | Governing station: η <η> · depth <h> m · smallest σ / (−Cp_min) of <n> stations — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 17: governing station line) |
| COPY-305 | Unavailable — vapour pressure missing — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 19: cavitation; reason code ANA-CAV-PV-MISSING) |
| COPY-306 | Unavailable — local station is surface piercing — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 20: cavitation; reason code ANA-CAV-SURFACE-PIERCING) |
| COPY-307 | Unavailable — water is invalid — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 21: cavitation; reason code ANA-CAV-WATER-INVALID) |
| COPY-308 | Unavailable — local depth is invalid — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 21: cavitation; reason code ANA-CAV-DEPTH-INVALID) |
| COPY-309 | Undefined — −Cp_min ≤ 0 — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 22: cavitation; COPY-69 with this cause; reason code ANA-CAV-NO-SUCTION) |
| COPY-310 | Undefined — static pressure does not exceed vapour pressure — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 23: cavitation; reason code ANA-CAV-PRESSURE-NONPOSITIVE) |
| COPY-311 | Cp_min under-read, 200 vs 400 panels — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 24: per-run measured under-read) |
| COPY-312 | Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 25: provisional row (DR-DXM-7)) |
| COPY-313 | surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 29: surrogate label beside COPY-66 (DR-DXM-3)) |
| COPY-314 | NeuralFoil-0.3.2/xxxlarge/94638c04 — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 30: Provenance Method value) |
| COPY-315 | analysis_confidence 0.97 · advisory, not an error bar — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 31: example value 0.97 is a fixture) |
| COPY-316 | Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar. — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 33: advisory; example value 0.31 is a fixture; reason code ANA-POLAR-LOW-CONFIDENCE) |
| COPY-317 | CST fit residual: max 1.09 × 10⁻⁴ c · RMS 2.93 × 10⁻⁵ c (shape residual, not an aerodynamic error) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 34: example values are fixtures) |
| COPY-318 | Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 35: bracket flag) |
| COPY-319 | Outside the validated bracket — α 7.50° is beyond ±6°. Computed, not validated. — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 36: bracket flag; one row per axis (α, Re, Ncrit, section family), only the α sentence is drafted) |
| COPY-320 | Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°) — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 37: non-computable; example values are fixtures; reason code ANA-POLAR-ALPHA-OUTSIDE) |
| COPY-321 | Unavailable — section fit residual 4.2 × 10⁻⁴ c exceeds the limit 3.6 × 10⁻⁴ c — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 37: non-computable; example values are fixtures; reason code ANA-POLAR-NONCOMPUTABLE) |
| COPY-322 | Re_local <Re> inside <Re_min> to <Re_max> — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 38: strip row) |
| COPY-323 | Re_local <Re> outside the polar’s Re range <Re_min> to <Re_max> — cd not extrapolated — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 39: strip row) |
| COPY-324 | Δ vs lattice Cl_local — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 41: strip row label) |
| COPY-325 | polar cl at α_eff against the lattice, a per-strip consistency check — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 41: note of the Δ vs lattice Cl_local row) |
| COPY-326 | Polar bracket: — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 42: strip row label (DR-DXM-2)) |
| COPY-327 | Overlay a section ▾ — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 44: transition overlay control) |
| COPY-328 | Profile drag from the polar at α_eff, both Ncrit; band, not a prediction — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 46: polar-sourced profile drag note) |
| COPY-329 | Unavailable — cd missing at <n> strips; the estimator bound is not substituted — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 48: profile drag with strips missing cd; reason code ANA-PROFILE-DRAG-MISSING-CD) |
| COPY-330 | Wing only: induced (VLM + strip) plus profile (polar). Not a total. — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 49: wing drag note (DR-DXM-5)) |
| COPY-331 | Unavailable — missing: junction, mast, wave, spray — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 50: craft Total drag when the profile part is present; reason code ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY) |
| COPY-332 | α <a>° meets CL <t> within 1 % — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 53: Find α, a found α) |
| COPY-333 | Find α found no α — <reason>. Nothing was extrapolated. — approved — Ruling 108 (DR-DXM-1) (dx-screen-states row 54: Find α with no root; the five <reason> sentences are not drafted) |
| COPY-334 | Unavailable — drag could not be computed. Evaluate again. — proposed — awaiting operator (reason code ANA-DRAG-UNAVAILABLE; shows at AnalysisProjection.cs:232 (drag row value, fallback)) |
| COPY-335 | Unavailable — wing drag is missing or zero, so CL/CD can't be formed. — proposed — awaiting operator (reason code ANA-WING-RATIO-UNAVAILABLE; shows at AnalysisProjection.cs:245 (Wing-only CL/CD value)) |
| COPY-336 | Wing only: lift over wing drag. Not a craft CL/CD. — proposed — awaiting operator (reason code ANA-WING-ONLY-RATIO; shows at AnalysisProjection.cs:248 (Wing-only CL/CD note)) |
| COPY-337 | Cd (turbulent bound) — proposed — awaiting operator (reason code ANA-SECTION-ITTC1957-BOUND; shows at AnalysisProjection.cs:192 (Section row label; the DX row 7 sentence is its note)) |
| COPY-338 | Unavailable — induced drag is missing. — proposed — awaiting operator (reason code ANA-TOTAL-DRAG-MISSING-INDUCED; shows at Loads.cs:51 via AnalysisProjection.cs:232 (Total drag value)) |
| COPY-339 | Unavailable — missing: profile, junction, mast, wave, spray — proposed — awaiting operator (reason code ANA-TOTAL-DRAG-MISSING-PROFILE; shows at Loads.cs:53 via AnalysisProjection.cs:232 (Total drag value when a polar is installed but the profile part is missing)) |
| COPY-340 | Unavailable — the run has no strips. — proposed — awaiting operator (reason codes ANA-PROFILE-DRAG-MISSING-STRIPS, ANA-INDUCED-DRAG-MISSING-STRIPS; shows at Loads.cs:68, :88 via AnalysisProjection.cs:232 (Profile and Induced drag value)) |
| COPY-341 | Unavailable — a strip width is not recorded. — proposed — awaiting operator (reason codes ANA-PROFILE-DRAG-MISSING-WIDTH, ANA-INDUCED-DRAG-MISSING-WIDTH; shows at Loads.cs:77, :93 via AnalysisProjection.cs:232) |
| COPY-342 | Unavailable — a drag sum is not a finite number. Evaluate again. — proposed — awaiting operator (reason codes ANA-PROFILE-DRAG-NONFINITE, ANA-INDUCED-DRAG-NONFINITE; shows at Loads.cs:83, :97 via AnalysisProjection.cs:232) |
| COPY-343 | Unavailable — no section profile for this strip. — proposed — awaiting operator (reason code ANA-POLAR-PROFILE-MISSING; shows at StripCoupler.cs:33 and NeuralFoilPolarSource.cs:100 via AnalysisProjection.cs:232 and :213 (PolarText)) |
| COPY-344 | Unavailable — the polar gave no result for this strip. — proposed — awaiting operator (reason code ANA-POLAR-UNAVAILABLE; shows at StripCoupler.cs:67 via AnalysisProjection.cs:232) |
| COPY-345 | Unavailable — the polar gave no drag value at this strip. — proposed — awaiting operator (reason code ANA-POLAR-CD-UNAVAILABLE; shows at StripCoupler.cs:72 via AnalysisProjection.cs:232) |
| COPY-346 | Unavailable — the stored polar was made with another method or profile. Evaluate to compute a new run. — proposed — awaiting operator (reason code ANA-POLAR-METHOD-MISMATCH; shows at SectionTier.cs:42 and NeuralFoilPolarSource.cs:126 via AnalysisProjection.cs:213 (Ncrit rows)) |
| COPY-347 | Unavailable — the section revision for this polar is not held by this session. — proposed — awaiting operator (reason codes ANA-POLAR-REVISION-MISSING, ANA-POLAR-REVISION-MISMATCH, ANA-POLAR-PROFILE-HASH; shows at RunPolarResolver.cs:15, :19 and NeuralFoilPolarSource.cs:102 via StripCoupler.cs:74 / SectionTier.cs:59) |
| COPY-348 | Unavailable — Re <Re> is outside the surrogate’s training range (<min> to <max>) — proposed — awaiting operator (reason code ANA-POLAR-RE-OUTSIDE; shows at IPolarSource.cs:28 via SectionTier.cs:57 (Ncrit rows); the α sentence is DX row 37) |
| COPY-349 | Unavailable — Ncrit <n> is outside the surrogate’s training range (<min> to <max>) — proposed — awaiting operator (reason code ANA-POLAR-NCRIT-OUTSIDE; shows at IPolarSource.cs:31 via SectionTier.cs:57) |
| COPY-350 | Unavailable — this section family is not covered by the surrogate. — proposed — awaiting operator (reason code ANA-POLAR-SECTION-UNVALIDATED; shows at IPolarSource.cs:29 via SectionTier.cs:57) |
| COPY-351 | Unavailable — the polar did not converge at this strip. — proposed — awaiting operator (reason code ANA-POLAR-NOT-CONVERGED; shows at IPolarSource.cs:32 via SectionTier.cs:57) |
| COPY-352 | Unavailable — the polar could not be computed for this section. This is a program fault; the run is kept. — proposed — awaiting operator (reason codes ANA-POLAR-NONFINITE, ANA-POLAR-INPUT, ANA-POLAR-CST-INPUT, ANA-POLAR-CST-FIT, ANA-POLAR-WEIGHTS-MISSING, ANA-POLAR-WEIGHTS-HASH, ANA-POLAR-WEIGHTS-FORMAT; shows at NeuralFoilNetwork.cs, CstFit.cs, Naca0012Reference.cs via StripCoupler.cs:74 / SectionTier.cs:59 (any ANA-POLAR- code becomes the reason)) |
| COPY-353 | Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray — approved — Ruling 101 (one string for Total drag and the craft CL/CD (Loads.TotalDragReason replaces "Unavailable — total drag missing"); the craft Total drag and craft CL/CD stay Unavailable (hydrodynamicist condition)) |
| COPY-354 | Drag (Wing only) — approved — Ruling 109 (row label; one row replaces the "Wing-only drag" row (AnalysisProjection.cs:81 and :134); Ruling 108 text was "Total drag" marked "Wing only") |
| COPY-355 | <min>–<max> <force unit> — approved — Ruling 109 (value: the Ncrit 2–4 band; keeps the surrogate label and the low-confidence flag; the unit follows the Units setting (N in Metric, lbf in Imperial)) |
| COPY-356 | Not included: junction, mast, wave, spray — approved — Ruling 109 (new reason line under the Drag (Wing only) row; tip-vortex cavitation stays under Not modelled) |

COPY-172 to COPY-184 are quoted from `docs/design/m12c-section-editor.md` §11.4 and COPY-185 to COPY-189 from
`docs/reviews/ui-m12c-paired.md` (paired point types, Ruling 60), recorded by track UXR (2026-10-04). COPY-187's
precision rule is UXR's: the measured move is printed to 0.0001 mm.

COPY-125 to COPY-139 are quoted verbatim from `docs/design/app-shell.md` §11 (track U1a, 2026-09-30).
COPY-125 to COPY-131 are the open failures other than COPY-103; the built start card renders the
sentence after “<file>” didn't open. as a second text line. COPY-131 has no instance in M1.
COPY-132 to COPY-135 belong to the catalog dialog (M1.2d; COPY-134 and COPY-135 are the two
`<cause>` values of COPY-133) and are not in the M1.2a build. Where the M1.2a build differs from a
row, the difference is a finding in `docs/reviews/app-shell-native.md`, not a second record.
COPY-140 to COPY-148 were added by track COPYFIX (2026-09-30). COPY-145 and COPY-146 quote §11 verbatim;
COPY-147 and COPY-148 are the two `<reason>` values of COPY-146. COPY-140 (ID candidate) and COPY-141 (refused)
are the alert-band strings for states §11 names without a string; COPY-142 is the failed Accept candidate IDs.
COPY-143 is the M1 start-card rendering of COPY-29's state (DOC-01: the fixture is named, nothing is
overwritten, New foil stays available); the spec's COPY-29 row still needs its 1.6 supersession mark.
COPY-144 is the Dismiss button on the start-card alert and the model-area alert band.

COPY-28 to COPY-97 are quoted verbatim from specification v1.1's C2 state table, its
fixed strings (A5.1, A5.3, A5.4, A5.6, A5.9, A7) and the A5.12 entry-point names; the spec is their authority and this
table is the copy record reviews cite. Angle-bracket fields are substituted at render
time and never rendered literally. Errors state the failing condition, retained state,
and recovery. Scientific states never use “validated,” “safe,” or “optimized”
without evidence. UI fixture values are
illustrative and cannot support a design decision.

## 8. Performance budget and evidence limit

Target for the self-contained artifact: under 250 KB uncompressed, zero external
requests, no build step, main view rendered on file open, local edits respond in
under 100 ms on the review machine. These are review targets; per-edit latency has
not been measured. The browser report measures state rendering, errors, external
requests, contrast and target sizes. These observations are not proof
of a geometry kernel, native app, solver, or million-cell renderer. Production target:
input acknowledgment ≤100 ms, cancellable expensive work, preview progress after
250 ms, and 30 fps camera interaction on the reference workload to be selected in
architecture. The spec defines scale and required benchmark environment.

## 9. AI and technical honesty

An optional BYOK assistant proposes starting parameters and answers grounded result
questions. HAX G1/G2 declares capabilities and limitations; G7/G8 offers explicit
invocation and dismissal; G9 supports correcting the request; G10 scopes unsupported
questions; G11 supplies provenance; G16 previews consequences; G17 provides an off
switch. Shape-of-AI: Wayfinders (starter prompts), Tuners (request text), Governors
(review fields before Apply), Trust builders (source-linked answers), Identifiers
(explicit AI label). No key means disabled invocation with a Configure key action.
Unsupported questions show no fabricated conclusion and link available evidence.
All prototype responses are deterministic fixtures, with no key entry or network call.

CFD is numerical modeling; uncertainty, model
assumptions, revision, method, convergence, and unavailable evidence are first-class
technical states. Prototype results are all fixtures. The plot must say
COPY-03 and COPY-17; no confidence interval is fabricated. Pressure and performance
do not become trustworthy because the fixture looks plausible.

## 10. Guardrails and iteration

Use the same selection in tree, plot, canvas, and inspector. Editing a scalar at a
station updates the owning distribution. Explicit stations, handles, and interpolation
rules remain the parametric definition after a starter recipe is detached. A station
profile is section identity in a fixed span-normal plane. Leading and trailing rails are
independently authored absolute aft positions. Chord is derived as TE minus LE; thickness
has one authority. Moving either planform rail leaves the other unchanged, including its
control abscissae. Properties labels the rail coordinate **aft position** and exposes
**Chord (TE − LE)** as a readout. The top-view transform stays fixed during a rail edit.

Keep units and validity beside the number. Keep supporting explanation accessible
through named help, not an always-open second inspector. Iterate the curve–station
handoff first. Add no tool until its task, authority, hard states, and keyboard path
are defined. Run the design token linter and deterministic craft detector after edits.

## 11. Provenance and residual risk

Format follows the pack's extended DESIGN.md template. Direction references and
license posture are in [workbench-direction.md](docs/design/workbench-direction.md).
No generated or copied assets. Native platform proof, real geometry continuity,
solver validity, performance at project scale, and assistive technology testing on
Windows/macOS are **Flagged** future implementation obligations.

## 12. Prototype interaction boundary and verification

### 12.0f Property grid (2026-10-01; repair cycle 1) — `docs/mockups/property-grid.html`

The Properties pane becomes a **property grid** (F-1 of the M1.2b native review). It is one reusable component —
selection identity, property groups, property rows — built by a dedicated track after the M1.2b fix track and before
M1.2b2's PNL track (ruling DR-UID-4, `docs/notes/property-grid-rulings.md`). **Its structure is B, Premiere Pro Effect
Controls (DR-CELL-1); the look is fixed by the operator** (`docs/mockups/property-grid-cells.html` is the record of the
pick). Tokens: {spacing.prop-head}, {spacing.prop-row-ro}, {spacing.prop-row-input}, {spacing.prop-edit-box},
{spacing.prop-value}, {spacing.prop-unit}, {spacing.prop-indent}, {spacing.prop-indent-sub}, {spacing.prop-inset},
{typography.prop}, {typography.prop-title}, {typography.prop-note}. No new colour. Rules added to the language
(where a rule below speaks of a grid column or a boxed field, structure B supersedes it — see "Structure B"):

- **A grid, not a column of text.** Every row in a pane shares one label column and one right value edge; units have
  their own column. The browser check asserts one label x and one value edge per state.
- **Labels wrap, never truncate.** A label or a Kind option is never ellipsized; at the narrow dock a label wraps
  (PG-02, PG-03).
- **Editable looks editable; a fact looks like a fact.** Inputs carry a border. Read-only values are plain text (a lock
  glyph and the reason when locked), never a disabled input. Estimates are plain text with "≈ ".
- **Units everywhere, one spelling, spoken too.** Lengths "mm", angles "°" (never "deg"), t/c "%", area "cm²". AR
  carries its convention "b²/S" in the unit column; η is dimensionless. An input's name carries the unit. A fact or
  estimate row speaks one line, "<label>, <value> <unit>[, locked]" (PG-01).
- **Entry by unit family.** Lengths accept mm · cm · m and `#span`, `#root_chord`, `#tip_chord`. Angles accept ° · deg ·
  rad. t/c accepts %. The echo is in the field's unit (COPY-157). An expression with a reference is set once and says
  so (COPY-161).
- **Precision follows the quantity, not the row (DR-UID-1).**
  - Typed or placed lengths: 0.01 mm. This includes Span ("1000.00") and a station chord at the root or tip, which is
    the typed dimension.
  - Derived lengths: 0.1 mm with "≈" (Mean chord, MAC).
  - Angles: placed or typed 0.01°, derived 0.1°.
  - t/c: placed 0.01 %; Max t/c 0.1 % ("12.0 %", not "0.12").
  - AR: two decimals. Area: 1 cm². Δ and "largest change": 0.01 mm.
  - The status line reads "MAC 101.3 mm".
- **"From root", not "Span", for a point (MC-6).** A point's spanwise coordinate is "From root", with η as a read-only
  fact. "Span" means only the wing span b. The rename is cross-surface (brief §10.9).
- **An estimate is never blank, and availability is per quantity.** An estimate that cannot be computed reads
  "Unavailable", and the group states which one and why (COPY-155). When all of them fail, the Wing header chip says
  "Unavailable". The status line announces the change (COPY-160). "≈ —" is retired; it was defect D-4's only visible
  symptom.
- **One identity per selection.** A handle is named as a handle, with its parent anchor or end as a link. The Type value
  is not repeated in the identity. Helper text lives under the row it explains (COPY-117 and COPY-149 under Type;
  COPY-150 to COPY-152 under Kind).
- **Tangent is a labelled group with a vertical Kind list.**
  - It appears on an anchor and on a handle; on a handle it is the parent anchor's kind, editable, with COPY-162.
  - Arrow keys move the check only and do not wrap. Return or Space, or leaving the group, commits one undo row. This
    is a recorded APG deviation (PG-06 = MC-1).
  - The Type option for an anchor names its consequence (COPY-153), and its report counts the rail from the result
    (COPY-154, COPY-165).
  - Arrows on the closed Type box are pending (COPY-166); Return commits.
- **Field nudge (DR-UID-2).** Point and handle fields only. The field run is the canvas Nudging gesture: the ≈ preview
  chip during the run, one undo row on release, Esc cancels, ignored while the text is dirty, ⌘ on macOS and Ctrl on
  Windows. COPY-163 is its help text.
- **Root-chord authority (MC-2).** The TE root end's Aft and the Wing's Root chord are both editable. COPY-159 under
  Aft says which operation each one is.
- **Wing always visible (DR-UID-5, ruled; amends UI-36).** The Wing block is pinned below the selection and takes at
  most 55 % of the pane.
  - It is fully visible in every state at 1280 × 800 on the 260 and 300 px docks (gated).
  - The selection section may scroll; groups stay collapsible and remember their state.
  - At the 200 px dock the Wing stays pinned but is not fully visible in four recorded states: chord warning,
    unavailable, MAC unavailable, section editor. Its own note lines scroll inside it.
- **Typed values past the certificate domain are clamped, not refused (MC-19).** Twist and t/c go to Core. The field
  shows Core's clamped value on a warning rail with COPY-168. A typed t/c under 1 % commits with COPY-169. A field run
  stops at a bound (COPY-170) and makes no undo row when nothing changed (MC-23).
- **Facts are not Tab stops (D2).** A fact or estimate row is announced through its named container; copying a value
  is a Copy command. Leaving the Type box drops a pending type (D1). An error is announced once per failed commit
  (PG-22). Abbreviations are spoken in full: "aspect ratio", "t over c" (PG-24).
- **Structure B (DR-CELL-1, 2026-10-01; supersedes the density pass's grid columns and drawn field box).** The
  operator saw three cell layouts and chose B, Premiere Pro Effect Controls, "as is". Its look is fixed. Numbers are in
  `docs/reviews/ui-property-grid-cells.md` and pinned by the browser check.
  - **Groups.** One twirl group per object; for a point, position and tangent share the "Point" group. Handle rows sit
    under a muted semibold subhead, indented one more step.
  - **Rows.** Label left in muted ink. Value right-aligned:
    - editable values: {colors.primary} text with a dotted underline;
    - enums: {colors.primary} text + ▾;
    - read-only values: {colors.ink}, with no cue.
    The unit follows in muted ink. A half-strength rule sits between rows. There is no box until a value has focus;
    then a 20 px box appears inside the 24 px row band.
  - **11 px, nothing below 11 (DR-DEN-3).** Every grid text, including messages, notes and the crumb, is
    {typography.prop}. Only the identity title is larger.
  - **Targets (DR-DEN-1).** Editable rows are 24 px, and the whole row is the target (clicking the label focuses the
    value). Read-only rows are 20 px and are not targets; copying uses the group header menu and the keyboard (DN-3,
    PG-25).
  - **Help.** Descriptions and the angle reference show under a row while it has focus, and are always the field's
    accessible description. The TE root's root-chord authority line (COPY-159) always shows.
  - **Links** ("Estimates · definitions", the crumb) take a **solid** underline; the dotted underline is reserved for
    editable values (DR-CELL-5). The angle reference stays focus-only (DR-CELL-4).
  - **Focused value in error** (DR-CELL-3): the accent focus box with the danger box 1 px outside it; unfocused, the
    danger box only.
  - **Editing.** Kind and Type are enums with one commit rule (DR-CELL-2, amending PG-06): arrows on the closed box are pending, Return or a
    pointer pick commits, Esc keeps, leaving drops. The field widens across the row once its text differs, so an
    expression shows whole (DC-1).
- **Text size (DN-5).** The app's setting: View ▸ Text size 100 / 125 / 150 / 200 %, ⌘+ / ⌘− (Ctrl on Windows),
  persisted per user. One multiplier ({typography.prop-text-scale}) scales every Prop type and row token. At ≥ 150 % the
  value drops under its label. ⌘= / ⌘− zoom a focused model view and step the Text size anywhere else (DR-DEN-4, ruled).
- **B never trades a floor.**
  - The underline is the non-colour cue for editability (SC 1.4.1). It is {colors.primary} at ≥ 3:1 in every theme,
    and yellow in high contrast.
  - A focused value always shows its box (SC 2.4.7), and every editable value is a Tab stop.
  - At 200 % text the Wing stays pinned and scrolls inside itself; a focused field is brought into view (DR-DEN-2).
  - Contrast tokens are unchanged.
- **Motion.** One moment: the chevron turns in {motion.fast} with {motion.easing}; under reduced motion it is instant.
  Natively there is no chevron or Expander transition (PG-16).

Copy: COPY-149 to COPY-171, proposed for the spec owner (COPY-168 to COPY-171 added in cycle 2). COPY-149 quotes `PropertiesPane.axaml.cs` and
`docs/design/m12b-points.md` §11.4. Cycle 1 supersedes the cycle-0 text of COPY-152, 153, 154 and 157. Evidence:
`docs/reviews/ui-property-grid.md` and `docs/proof/property-grid-browser-check.json`.

### 12.0e Mockup v10 (2026-09-26) — `docs/mockups/workbench-v10.html`

No token changes: every colour, size and radius in v10 is an existing token (the craft gate reports no findings, and
an injected off-token colour is caught, so the run scanned a live corpus). v10 adds these rules to the language:

- **One precision per quantity.** A length you type or place shows 0.01 mm (the finest nudge); a derived length
  (chord, span estimate, MAC, LE radius) shows 0.1 mm; angles 0.1°; ratios 0.1 %. A value appears at the same
  precision in Properties, the Points grid and the canvas label.
- **Properties at 200 px.** Field labels take 56 px (72 px for the Wing dimensions) so a value such as −175.60 is
  never clipped. Smooth and symmetric anchors show one Angle and two lengths.
- **Wing block.** Properties always ends with the Wing block while a foil is open: typed driving dimensions (Span,
  Root chord, Tip chord) above read-only estimates prefixed "≈", separated by a rule, with an "Estimates ·
  definitions" disclosure. MAC is (2/S)∫₀^{b/2} c(y)² dy and is never labelled as S/b.
- **Point glyphs.** Anchor: square; end point: diamond (dashed when locked); control point: circle joined to its
  neighbours by a dashed control polygon in `model-dim`. The selected point paints over a coincident neighbour.
- **Floats and focus.** A control in the model area that takes focus is never under a float: the float moves to the
  nearest clear corner of the model area and says so (COPY-116); when no corner clears it, the pane docks back (COPY-120). Decorative drawing never takes the pointer.
- **Typed numbers.** Every typed number is checked the same way: a non-number, or a length ≤ 0, is refused with
  COPY-118 or COPY-106, `aria-invalid` and an alert tied by `aria-describedby`; the geometry is unchanged; Escape restores.
- **Escape never discards work.** In the section editor Escape steps back (drag → handle → point → selection); with
  unsaved edits it moves focus to Cancel and says COPY-119. Only Cancel discards.
- **Motion.** None. v10 has no animation; the opening state is a static skeleton, so the reduced-motion path is
  identical by construction.

Copy added: COPY-103 to COPY-124 (§7); COPY-08 is reused on the first-run card. Oracle:
`tools/check-mockup-v10.mjs` (evidence `docs/proof/workbench-v10-browser-check.json`, craft findings
`docs/proof/ui-craft-findings-v10.json`).

### 12.0d Mockup v5 (2026-09-21) — `docs/mockups/workbench-v5.html`

The v4 shell and camera with the CAD experience rebuilt around the **control-vertex record** of specification
1.3: every master curve is a degree-3 clamped B-spline with seven vertices (sections degree 5, the count chosen
by the conversion to meet its 10 µm acceptance) drawn as a **control frame** — dashed polygon, square vertices,
circle **levers**, diamond ends — in the elevation that shapes it, one frame at a time, the other curves pickable
ghosts; a vertex pulls the curve and never lies on it (the oracle measures the gap and the local support). The
workspace is **four viewports** (Top · Perspective / Front · Starboard) with title menus (any view including the
η-plot; Frame · Comb · Ghost; Body Smooth · Box · Cage over smooth) and double-click/Return maximise; a **tool
palette** (Select · Insert CV · Add station · Measure · Fair · Rebuild · Fit points · Edit section · Ghost, icons
with names, single keys on the focused workspace) replaces the toolbar verbs and the curve pane; an **options
strip** replaces the parameter row; the nudge step, locks and station removal moved to Properties; the bottom
panel is the Checks drawer; the 3D body shows the loft or its **display cage** (never a T-spline). Visible chrome
in CAD at 1280 × 800 on entry: **45** (v4: 71). The section document edits section vertices with the residual
**measured** and shown identically in the HUD, strip and Properties. The executable control is
`tools/check-mockup-v5.mjs` (15 groups; group 6 the record — influenced-not-through gap, local support, levers,
locks, Insert/Delete/Fair/Rebuild, palette keys, one draft; group 13 the workspace — maximise, title menus, cage,
camera, chrome count); evidence in `docs/proof/workbench-v5-browser-check.json` and
`docs/proof/ui-craft-findings-v5.json`. Illustrative throughout; the kernel of A4.12 is a dependency, not a
mockup claim.

### 12.0c Mockup v4 (2026-09-21) — `docs/mockups/workbench-v4.html` (superseded as review artifact by v5)

The v3 shell plus the CAD editing views of specification 1.2: an **icon rail** (inline glyphs, names beneath,
readiness in the accessible name); **splines** everywhere (Catmull–Rom through the evaluated samples; the control
polygon on demand); **one camera** over one model — Top · Front · Side · Iso (+ Bottom · Back · Port) as presets in
the document-tab row and on a depth-sorted **view cube**, free orbit/pan/zoom by pointer per navigation preset
(Workbench: Alt+drag · Shift+drag · wheel; Rhino: RMB · MMB · wheel) and by keyboard, sections selectable in 3D,
the analysis layers projected into any camera (the free-surface plane drawn clipped with its true h_ref in the
label); the **lines-plan as editing elevations** — Top over Front sharing the span axis, Side beside — where the
Outline's LE/TE rails (Top), the Dihedral/Anhedral and Thickness curves (Front) and the Twist handles (Starboard,
a body plan with one row per station) are explicit control curves with `role=slider` anchors (drag, arrows, Return, Escape) opening the same draft as the curve pane; and the
**Station document** — Edit section opens a tab in the editor group with a full 2D section editor (grid, chord
dimension, control points, catalog ghost, comb), the section palette on the toolbar and the section's Properties;
Return applies, Escape closes. Direct 3D handle dragging is deferred (no unambiguous drag plane without a gizmo).
The executable control is `tools/check-mockup-v4.mjs` (16 groups; group 13 is the CAD views: icons without
numerals, no polylines, elevation handles for all five channels with keyboard and pointer edits applied as
revisions, keyboard and pointer orbit in both presets, wheel zoom, view-cube and tab views, 3D selection, the
station document's open/apply/escape/focus contract); evidence in `docs/proof/workbench-v4-browser-check.json`
and `docs/proof/ui-craft-findings-v4.json`. Illustrative throughout.

### 12.0b Mockup v3 (2026-09-21) — `docs/mockups/workbench-v3.html` (superseded as review artifact by v4)

The thick-client shell over the v2 content: the same fixtures, evaluator, harness and the seven areas'
strings, re-homed into a fixed window (menu bar · one-row toolbar · parameter row · activity rail · left
dock · editor with document tabs and a tabbed bottom panel · right dock · status bar). Per area: Setup is a
document (language + parameters) with Feasibility and Operating points tabs; CAD is the viewport with the
Curve editor, Catalog and Checks tabs, stations and catalog on the toolbar, the derived strip in the
parameter row; Analysis is the same viewport with the operating point in the parameter row, derived
conditions in Properties, Layers in the left dock and Results/Charts tabs; Experiment is a document with
the Cases tab; Run is the console with the queue in the Navigator and Queue tab, the log in its tab and
the backend environment in Properties; Results is the slice viewport with Timeline, Sweep and Candidates
tabs and Layers in the dock; Export is a dialog from the rail. Window presets: 1024 × 700 · 1280 × 800 ·
1440 × 900 · 1600 × 1000 · 640 × 400 (drawers). The executable control is `tools/check-mockup-v3.mjs`
(14 groups; 30 shell cells: no window scroll, toolbar one row and never overflowing, parameter row one
row, `More ▾` only when a group is hidden and never at ≥ 1,280 px, docks internal); evidence in
`docs/proof/workbench-v3-browser-check.json`, craft gate in `docs/proof/ui-craft-findings-v3.json`.
Every number is Illustrative; no solver, file I/O or model call exists in the artifact.

### 12.0a Mockup v2 (2026-09-21) — `docs/mockups/workbench-v2.html` (superseded as review artifact by v3)

Built against specification v1.1: an area strip in flow order — Setup · CAD · Analysis · Experiment · Run ·
Results · Export — with readiness chips; a prompt entry in every area with the fixed entry name and the proposal
preview; Setup from language (fixture proposal) or parameters with soft targets, weights and deviations; CAD with
Outline · Twist · Dihedral · Thickness curves, add/remove station and the section editor as a panel; Analysis as a
layer set over the same canvas (force vectors, loading strips, Cp on the section, depth and ventilation bands)
with the CAD ↔ Analysis toggle preserving selection and camera; Experiment with sweep preview and the optimize
definition including the single-point refusal; Run as a process console over a deterministic run fixture stepped
explicitly (state machine, residual and force histories, mesh-gate bars, resource meters, Cancel, Retry,
environment steps with consent); Results with layers from a fixture manifest (flood, isolines, probe, streamlines
with the labelled dash affordance, separation only with τ_w, force vectors), replay over admitted samples, small
multiples, metric-vs-α with gaps, difference flood and a candidate card; Export with 3DM and Fusion-ready STEP.
Run and Results carry the "gated (SPIKE-03/04)" chip. The executable control is `tools/check-mockup-v2.mjs`;
evidence in `docs/proof/workbench-v2-browser-check.json`. Every number is Illustrative; no solver, file I/O or
model call exists in the artifact.

### 12.0 Mockup v1 (2026-09-20) — `docs/mockups/workbench-v1.html` (superseded as review artifact by v2)

The v1 mockup is built against specification v1 and supersedes the 2026-09-19
prototype below as the review artifact. Six product destinations — Brief, Shape,
Sections, Analyze, Checks (drawer), Settings — plus Export and the assistant; two
bundled Examples (race light 1000 cm², race strong 750 cm²) whose seven-point
goal-state arithmetic is computed live from the pinned inputs of GOAL-02. The Smooth
mode is a genuine constrained weighted least-squares B-spline (degree 5, clamped, KKT
equality rows for locks) embedded as an illustrative evaluator — it exhibits the
GEO-13 monotone-approach property in the artifact but certifies no production kernel.
The Analyze destination renders every quantity with its basis (tier chip, depth basis,
omissions, Ncrit band, surface-state band), the cavitation and ventilation screens with
their fixed strings, the Not-assessed Loads panel with COPY-60, comparison with
normalised deltas and a Discrepancy record, and a Cp trace coloured by the vik map
pinned at Cp = 0 with a legend carrying map name and version and a table twin. The
harness switches persona (designer · keyboard-only · screen-reader · reviewer),
viewport (1024 · 1280 · 1440 lines-plan · 1600), state, theme, density, capability
(key none · key unevaluated · key evaluated; backend absent), reduced motion,
navigation preset (Workbench · Rhino), modifier scheme and trackpad mode. The
executable control is `tools/check-mockup-v1.mjs`; its evidence lands in
`docs/proof/workbench-v1-browser-check.json`. Native accessibility, scientific
validity, file I/O and any solver remain outside this artifact.

### 12.1 Prototype of 2026-09-19 (superseded as review artifact; kept for its measurements)

The delivered mockup has five task destinations and first-run review, with a shared
station selection, exact scalar edits, degree-five curve segments, a selected tangent
handle and a precision tangent-rise field. Its normalized curvature comb is clipped
for display and explicitly labeled; it is not a continuity certification. The same
illustrative evaluator drives the projected foil and the numerical reference-area
integration. Accepted profile camber edits flow into that foil; Undo restores the
profile and recipe state together. Run results use an immutable fixture snapshot.

Mirror break preserves the port fixture before independent starboard edits. A ghost
second wing is context only. Section `.dat` admission has a Selig/Lednicer preview
contract and error choices; the prototype does not parse an external file. Production
profile fitting, loft validation, mesh generation, solver execution, file I/O and AI
requests are not implemented by this artifact.

Catalog sections now open an editable analytic copy with upper/lower curve control
offsets, Through points and Smooth weighted modes, numeric influence weights and
keyboard/drag alternatives. The original source is dashed. Unaccepted previews
report normalized deviation, preserve effective t/c and can be cancelled; Apply
detaches the recipe as one undoable revision. The bounded rational curve preview
does not certify production spline fitting or continuity. Outline smooth previews
show evaluated station quantities separately from influence-control ordinates.

Analyze displays Cₗ, C𝒹, their ratio and finite-wing lift/drag at a specified speed
and water preset. Newtons are the default; lbf is a display conversion. Section
coefficients alone do not supply wing total forces. Coefficient formula fixtures
hold shape independent of Reynolds number and explicitly disclose that limitation.
Fresh/salt presets display density, viscosity, salinity and fixed 20°C temperature.

Simulation setup builds a velocity × angle sample matrix. Results links the selected
case across field, forces, coefficient plots, row and replay position. The main
render view stays dominant above a bounded, keyboard-scrollable evidence pane.
Pressure, velocity, modeled turbulent kinetic energy and a synthetic signed wall
shear coefficient have fixed unit-bearing legends. The wall-shear example uses
hatched C𝒻 < 0 areas to explain local reversal relative to +x free stream; it is not
physical evidence of real foil separation. A partial state shows absent wall data.
Steady streamlines are not pathlines or resolved turbulent motion.

The executable review control is `tools/check-mockup.mjs`. It accepts an optional
installed Node module directory containing Playwright and launches local Chrome
through its cross-platform channel. It writes measured evidence to
`docs/proof/workbench-browser-check.json` and screenshots to the system temporary
directory. The current measured counts and outcomes live in that JSON, including
snapshot immutability, focus restoration, real partial-field masks, weighted-curve
preview/acceptance, curve→area and profile→loft consistency, water/force dimensional
checks, reversible units, linked sweep replay, unavailable fields and undo. This
remains HTML evidence; native accessibility and scientific validity require their
own proof.


## 13. FoilDSL source authoring (v6)

The [FoilDSL direction and transaction contract](docs/design/foildsl-authoring-direction.md) extends
the existing ParametricWorkbench archetype. All existing colors, fonts, sizes, spacing and focus
tokens apply. Source uses the mono caption token; actions use compact 32px targets; diagnostics
use readable text and a labelled status, not color alone. No new motion or decorative asset.

The FoilDSL tab is a CAD document. At 640×400 it replaces redundant area toolbar/options space
with source editing; secondary file/history actions use a named disclosure. Internal document
scrolling keeps several source lines available. The preview is inert geometry with all readouts
outside the scaled drawing, preventing scaled text and controls from appearing undersized.

### Source state matrix and copy

| State | Surface / exact copy |
|---|---|
| Accepted | FoilDSL tab; editable source with accepted geometry and revision labels |
| Dirty | Source draft. Validate before Apply. Accepted geometry is unchanged. |
| Validating | Validate measures input bytes and elapsed milliseconds; Apply waits for the current generation |
| Valid | Valid prototype subset. Preview is illustrative; full geometry proof is not performed. |
| Error / incomplete | Source rejected. Accepted geometry is unchanged. Diagnostic adds code, line and repair. |
| Applied | Source applied. Geometry and text share one definition. |
| Cancelled | Draft cancelled. Accepted source and geometry restored. |
| Other visual draft | Apply or cancel the visual draft before editing FoilDSL. |
| Other source draft | Apply or cancel the FoilDSL draft before editing geometry. |
| Unsupported v3 | FoilDSL 3 requires migration preview; no automatic conversion is performed. |
| Session tip pin | session-only prototype pin; not saved in .foil |
| Empty | New foil creates an editable candidate; the empty harness retains the existing recovery banner |
| Read-only | Source is read-only; Apply is disabled; inspection and saving accepted source remain available |
| Overflow | Source scrolls internally, names use textContent/escaping, the reflow layout stacks panes |

Production diagnostics additionally expose full source ranges and byte offsets per the language spec.
The prototype reports lines and an explicit support boundary. The mockup is not proof of native
accessibility, stable IDs, immutable archive history or certified geometry. Performance budget remains
A8; the prototype emits measured per-validation duration and source bytes, never an invented percentile.
