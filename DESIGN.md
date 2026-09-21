---
id: design-language
title: "CFD-Workbench — design language"
type: design-language
status: in-review
owner: Product and UX
phase: specify
tags: [ui, hydrofoil, tokens]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-cfd-workbench, rel: relates-to }
  - { to: workbench-direction, rel: refines }
  - { to: mockup-workbench-v1, rel: relates-to }
  - { to: mockup-workbench-v2, rel: relates-to }
  - { to: mockup-workbench-v3, rel: relates-to }
  - { to: mockup-workbench-v4, rel: relates-to }
  - { to: cad-editing-views, rel: relates-to }
  - { to: thick-client-shell, rel: relates-to }
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
elevation: { flat: "none", popover: "0 8px 24px rgba(0,0,0,0.16)" }
motion: { fast: 120ms, base: 200ms, easing: "cubic-bezier(0.2,0,0,1)" }
---

# CFD-Workbench design language

This is the proposed visual vocabulary for iteration. The spec owns behavior; this
file owns the tokens and copy. The [direction brief](docs/design/workbench-direction.md)
records the words-first rationale and evidence. The paired archetype is G1 with
local-device persistence, with G2 inside Results. No application stack is selected.

## 1. Atmosphere and hierarchy

Precise, composed, tactile. One large modeling view receives the strongest contrast.
The project tree establishes context at left; the selected object has one inspector
at right. A single lower work area exposes the active curve or section. Switch tasks
in place. No dashboard of independent cards, repeated box hierarchy, or hero copy.

## 2. Color and contrast

**Viewport-only state tokens (v4).** Focus and refusal on the graphite viewport use {colors.focus-ring-viewport}
(9.3:1 on {colors.viewport}) and {colors.danger-viewport} (7.4:1), never {colors.focus-ring} or {colors.danger},
whose 2.45:1 and 2.30:1 on graphite fail SC 1.4.11; the in-artifact audit lists both pairs and the v4 oracle
measures the focused handle's stroke against the viewport background in all three themes.

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
| View cube | viewport corner, 3 depth-sorted faces of 6, lettered T·F·S·B·K·P | default · face hover · current view filled with {colors.station} · keyboard focus ring | face = named view; label names the camera or "Free · az · el" |
| Control vertex (v5) | `role=slider` vertex of the active curve's control frame: 11 px square (interior), circle (lever), diamond (end vertex, on the curve), a 20 px transparent hit circle, the dashed polygon behind; η and value in the accessible value | default · selected · coupled (dashed ring on both root vertices under the root-mirror lock) · locked (a {colors.warning} lock ring, `aria-readonly`, the lock named in the accessible value) · draft open (curve solid, frame dashed) · focus ring {colors.focus-ring-viewport} ≥ 3 px | drag, ↑↓ value, ←→ η, Shift ×10 = draft; Return applies and keeps focus; Escape cancels and returns to Select; Delete removes (focus to the previous vertex); the curve is pulled, never passed through |
| Tool palette (v5) | vertical strip {spacing.palette} wide beside the workspace; nine verbs, each an icon with its visible name and its key in the accessible name; separators group edit · construct · display; at the 640 × 400 reflow preset a horizontal row above the viewports (names visually hidden, 44 px targets) | `aria-pressed` on the toggle tools only; disabled with the reason while a station document owns the verb; single keys act only with the workspace focused; Enter on a tool keeps focus on it | Escape cancels the draft and returns to Select; the options strip shows the tool's parameters and every pointer verb's keyboard equivalent (Insert at η · Add station at η · Measure between two η); a construction (Fair · Rebuild · Fit points · Insert · Delete) opens the one draft |
| Viewport title bar (v5) | {spacing.viewport-title} row: the view name as a button (double-click or Return maximises), a `details` menu (`summary` with `aria-haspopup=menu`; View · Display · Body · Maximise) whose closed items are not rendered | current view checked in the menu; opening focuses the first item; arrows and Home/End move, Escape closes and returns focus to the button, choosing an item returns focus before the items leave; maximised state restores with the same gesture | one viewport below 480 × 240 px; every viewport renders at its own pixel size (a scaled drawing is a defect, class UI-L) |
| Station document | editor-group tab with a full 2D section view; palette on the toolbar; section Properties | catalog original · draft open (tab dot) · modified · infeasible | Return applies as a Modified Profile revision; Escape or × closes and returns focus to Edit section |

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
| COPY-29 | Example fixture missing or corrupt: <file> — nothing was overwritten · New · Open |
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
profile is section identity in a fixed span-normal plane. The trailing edge follows
leading-edge offset plus chord; thickness has one authority.

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
