---
id: design-language
title: "CFD-Workbench — design language"
type: design-language
status: in-review
owner: Product and UX
phase: specify
tags: [ui, hydrofoil, tokens]
links:
  - { to: spec-cfd-workbench, rel: implements }
  - { to: workbench-direction, rel: refines }
review-by: 2027-03-18
summary: >-
  The instrument-panel vocabulary for a cross-platform hydrofoil workbench.
  Light or dark technical panes surround one geometric canvas; a single teal
  selection joins curves, stations, and the precision inspector.
designmd_version: alpha
archetype: "ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact; Nav:Ribbon+CommandPalette; Viewport:DesktopBound; Input:PrecisionPointer+SpatialGestures+KeyboardFirst; Color:DarkAdaptive; Type:Utilitarian; Depth:Diegetic3D; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic+Confirmed; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }"
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
  cividis-0: "#00224e"
  cividis-1: "#434e6c"
  cividis-2: "#7d7c78"
  cividis-3: "#bcae6c"
  cividis-4: "#fee838"
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
  sidebar: 216px
  inspector: 286px
  titlebar: 64px
  taskbar: 52px
  statusbar: 36px
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

The frontmatter is the palette record. The default chrome uses {colors.surface};
the modeling view uses {colors.viewport}. Selection uses {colors.primary} on light
chrome and {colors.station} on the canvas. Scientific magnitude uses the five
{colors.cividis-0} through {colors.cividis-4} stops; labels and a unit-bearing legend
remain visible. These are data colors, never success or validity indicators.

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

All controls use {rounded.sm}; panels are square joins; floating dialogs use
{rounded.md}. Pointer controls are at least {spacing.target}; compact density reduces
padding around controls, not their target size. Keyboard focus uses an outside ring
with a gap so both adjacent surface and the control remain identifiable.

## 5. Layout and modes

Use {spacing.scale} for spacing. Default pane widths are {spacing.sidebar} and
{spacing.inspector}; the center absorbs available space. Light and dark retarget
semantic chrome; the viewport remains graphite. High contrast replaces backgrounds
with {colors.contrast-bg}, text/boundaries with {colors.contrast-ink}, selection with
{colors.contrast-primary}; model contours and section labels remain redundant cues.

At 1,440 CSS px and above all three panes are visible. At 1,100–1,439 px the project
pane narrows and work area remains dominant. At 900–1,099 px the project pane closes
behind a named toggle. Below 900 px the inspector moves below the canvas and the
entire page scrolls, preserving access at 200% zoom. This is a desktop review artifact;
mobile authoring is not promised. Comfortable density adds grouping space.

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
| Operating-point replay | Inspect a sweep at held speed or held angle | Explicit Play; 900 ms per discrete sample; hard cut | Same explicit controls; no tween or automatic startup |

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

Errors state the failing condition, retained state, and recovery. Scientific states
never use “validated,” “safe,” or “optimized” without evidence. UI fixture values are
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
