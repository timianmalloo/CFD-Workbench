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
  prop-label: 88px
  prop-label-narrow: 56px
  prop-unit: 32px
  prop-unit-narrow: 28px
  prop-head: 24px
  prop-row-ro: 24px
elevation: { flat: "none", popover: "0 8px 24px rgba(0,0,0,0.16)" }
motion: { fast: 120ms, base: 200ms, easing: "cubic-bezier(0.2,0,0,1)" }
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

| Conditions band | Speed, water, h_ref, incidence or load; derived q, Re, h/c, Fr_h, σ, V_crit with units | Derived cells recompute with status; a pinned run shows its own conditions | Depth unset → COPY-45 on σ, Fr_h, V_crit; water out of range → COPY-52 | Long water-source revision wraps in the provenance row |
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
| Selection identity (property grid, 2026-10-01; F-1, O-6) | the first block of Properties: the Plan view's glyph for the selection (square anchor, filled circle control, diamond end, small circle handle, three dots for several, dashed line for a station) at 16 px in {colors.ink}, the object's name in {typography.body} 600 ("Trailing edge · point 7 of 14", "Handle toward the tip", "3 points"), and a crumb in {typography.caption} {colors.ink-mute} only when it adds something the rows do not say (a handle's parent anchor as a link; never the Type value again) | one per selection, never two; a handle never reuses its anchor's name or helper | empty: COPY-136 + COPY-137, no identity · opening: the file name and skeleton rows · pane error: COPY-138 + Try again | the name wraps; it is never truncated |
| Property group (property grid) | a header band {spacing.prop-head} high on {colors.surface-soft}: a disclosure chevron, the group name in {typography.label} 600, and — while collapsed — a one-line summary of its values in {typography.numeric} {colors.ink-mute}; then its rows; groups are separated by a {colors.hairline} rule | collapsed state is remembered per group across selections (Premiere's twirl-down memory); the header is a Button with `aria-expanded`; the Wing group is never collapsible (UI-36) and its header carries a state chip: "≈ preview" during a gesture, "Checking…" while a commit is checked, "Unavailable" when the estimates are | no rows → the group is not drawn | the summary truncates with an ellipsis; the name never does |
| Property row (property grid) | one grid per row with a shared label column: label {spacing.prop-label} ({spacing.prop-label-narrow} when the pane is narrower than 230 px) in {typography.label} {colors.ink-mute} · value 1fr, right-aligned {typography.numeric} with tabular figures · unit {spacing.prop-unit} ({spacing.prop-unit-narrow} narrow) in {typography.caption}; a 3 px state rail on the left; an optional description and one message line beneath, full row width | **kinds:** input (bordered, {colors.control-line} boundary, {rounded.sm}, row ≥ {spacing.target-dense}) · read-only fact (plain text, no border, row {spacing.prop-row-ro}; a lock glyph and a reason when it is locked) · estimate ("≈ " prefix, plain text) · select · segmented (a radio group with fill **and** weight **and** a check mark on the chosen option) · action; **states:** focus (the rail turns {colors.focus-ring}; no row fill, which would drop the input boundary under 3:1) · warning (rail + icon + text in {colors.warning}) · error (rail + 2 px {colors.danger} input border + COPY-118 / COPY-106 as an alert tied by `aria-describedby`) · unavailable ("Unavailable", never "≈ —", with the reason in the group) · mixed ("Mixed", read-only, OI-3) | Return or leaving the field commits (one undo step) and keeps focus where it was sent; Escape restores the shown value; a typed expression is echoed under the field ("15 cm = 150.00 mm."); every number field's accessible name contains its visible label and its unit ("Span position in millimetres") | values wrap, never clip, at the 200 px dock; every quantity has a unit or is declared dimensionless |

All controls use {rounded.sm}; panels are square joins; floating dialogs use
{rounded.md}. Primary actions are at least {spacing.target}; dense scientific
controls (nudge steps, table row actions, chart toggles) are at least
{spacing.target-dense} with {spacing.dense-gap} separation (WCAG 2.2 SC 2.5.8 with
the spacing exception); compact density reduces padding around controls, not their
target size. Keyboard focus uses an outside ring
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
| Status appearance | Explain completed local change | Instant, reserved status area | Instant |
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
| COPY-152 | Each handle moves on its own. The curve has a corner here. |
| COPY-153 | Anchor point — adds 2 handles |
| COPY-154 | <Curve> point <i> is now an anchor point with 2 handles. The rail has <n> points (was <m>). Largest change <d> mm. |
| COPY-155 | Unavailable — <reason>. Undo, or edit again, to recompute. |
| COPY-156 | Square to the centre line (root mirror). Only its length can change. |
| COPY-157 | <typed> = <value> mm. |

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

### 12.0f Property grid (2026-10-01) — `docs/mockups/property-grid.html`

The Properties pane becomes a **property grid** (F-1 of the M1.2b native review): one reusable component — selection
identity, property groups, property rows — that M1.2b's fix track and M1.2b2's PNL track build from. New tokens:
{spacing.prop-label}, {spacing.prop-label-narrow}, {spacing.prop-unit}, {spacing.prop-unit-narrow},
{spacing.prop-head}, {spacing.prop-row-ro}; no new colour. Rules added to the language:

- **A grid, not a column of text.** Every row in a pane shares one label column and one right value edge; units have
  their own column. The browser check asserts one label x and one value edge per state.
- **Editable looks editable; a fact looks like a fact.** Inputs carry a border; read-only values are plain text (a lock
  glyph and the reason when locked) — never a disabled input. Estimates are plain text with "≈ ".
- **Units everywhere, one spelling.** Lengths "mm", angles "°" (never "deg"), t/c "%", area "cm²"; aspect ratio and η
  are declared dimensionless. A field's accessible name carries the unit.
- **Precision (completes UI-40).** Typed or placed lengths 0.01 mm (Span included: "1000.00"); derived lengths 0.1 mm
  (Mean chord, MAC); angles typed 0.01°, shown 0.01° in the field; ratios 0.1 % (Max t/c "12.0 %", not "0.12");
  aspect ratio two decimals; area 1 cm².
- **An estimate is never blank.** When the estimates cannot be computed every row reads "Unavailable", the Wing header
  says so, and the group states the reason with COPY-155. "≈ —" is retired (it was defect D-4's only visible symptom).
- **One identity per selection.** A handle is named as a handle with its parent anchor as a link; the Type value is not
  repeated in the identity; helper text lives under the row it explains (COPY-117, COPY-149 under Type; COPY-150 to
  COPY-152 under Tangent).
- **Tangent is a labelled group** on an anchor and on a handle (where it is the parent anchor's kind, editable); the
  Type option for an anchor names its consequence (COPY-153) and its report counts the handles and points (COPY-154).
- **Wing at the foot.** The Wing block is pinned below the selection and keeps at most 55 % of the pane; at 1280 × 800
  with the 260 px dock, every selection state and the Wing fit without scrolling (measured; the overflow fixture and
  three states at the 200 px dock scroll inside the selection area).
- **Motion.** One moment: the chevron turns in {motion.fast} with {motion.easing}; under reduced motion it is instant.

Copy added: COPY-149 to COPY-157 (proposed for the spec owner; COPY-149 quotes `PropertiesPane.axaml.cs` and
`docs/design/m12b-points.md` §11.4). Evidence: `docs/reviews/ui-property-grid.md`.

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
