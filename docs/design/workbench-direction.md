---
id: workbench-direction
title: "CFD-Workbench — interface direction"
type: design
status: in-review
owner: Product and UX
phase: specify
tags: [hydrofoil, ui, direction]
links:
  - { to: spec-cfd-workbench, rel: refines }
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: kb-hydrofoil-workbench, rel: depends-on }
  - { to: design-language, rel: documents }
review-by: 2027-03-18
summary: >-
  Words-first creative direction for the hydrofoil workbench, extended on 2026-09-20 with the v1 elevation brief (candid, not reassuring; the basis travels with the number). A bounded parametric
  canvas joins scalar span distributions and station section anchors in one model,
  with precision editing and visible evidence limits.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: design-language, on: 2026-09-19, reason: "Initial cross-platform workbench token and interaction language created for review." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)." }
---

# CFD-Workbench interface direction

Mode: **elevate** for the 2026-09-19 geometry/results iteration. The established direction is retained and extended below before changing screens. Status: proposed direction for iteration;
the product stack and simulation backend remain undecided.

## Person, need, and register

The primary user is a technically confident hydrofoil designer who arrives with
a shape in mind and needs to make, inspect, and explain it. They may understand
hydrodynamics better than a general-purpose CAD system. Their concern is losing
control of a smooth shape, or trusting an analysis whose inputs are no longer clear.
The job is to turn a few meaningful spanwise decisions into a reproducible 3D foil,
then compare evidence about its behavior without losing the originating geometry.
These persona details are **Inferred** from the user's request and proposal intent;
they require user observation, not a fabricated usability study.

Three qualities govern the design:

| Choose | Avoid | Concrete consequence |
|---|---|---|
| Precise | Cryptic | Unit-bearing values, named dependencies, explicit evidence status. |
| Composed | Busy | One dominant canvas and one inspector; secondary plots stay subordinate. |
| Tactile | Decorative | Select a section in space, adjust its curve, then enter the exact value. |

## Archetype and the task shape

**G1 Parametric Modeling Workbench** is selected because the dominant job is editing
a bounded geometric object, using direct manipulation and precision input. Shape
editing is serial: a single selection owns a single inspector. The designer can
read multiple derived quantities in parallel without being invited to edit several
independent definitions of the same shape. Results use **G2 Scientific Visualization
Pipeline** inside the same shell because their job is inspecting fields and provenance.

`ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact; Nav:Ribbon+CommandPalette; Viewport:DesktopBound; Input:PrecisionPointer+SpatialGestures+KeyboardFirst; Color:DarkAdaptive; Type:Utilitarian; Depth:Diegetic3D; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic+Confirmed; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }`

Deviation: the catalog's Cloud persistence is replaced by LocalDevice to keep the
cross-platform document workflow independent of a cloud account. The ribbon is a
short task strip, not an inventory of every CAD operation. This is a structural
direction, not a framework choice. AI construction is not implied by the catalog.

## Personality in three moves

**Type:** the platform system sans for labels and tabular monospace numerics. Familiar
desktop text keeps attention on geometry; a restrained title scale names the task.

**Color:** warm light instrument panels around a graphite modeling canvas. A single
teal selection color ties the active station, distribution handle, and inspector.
Semantic status colors stay textual and symbolic; scientific fields use cividis.

**Space:** tight groups within a tool, clear gaps between tasks, and the largest
unbroken region reserved for the foil. Thin dividers delineate panes; boxes do not
repeatedly enclose boxes. A station list remains a list, not a grid of cards.

## References and adaptation

| Reference | Evidence observed | Adaptation, not copied assets |
|---|---|---|
| [Onshape Loft documentation](https://cad.onshape.com/help/Content/PartStudio/loft.htm) | **Verified:** documents profiles, guide curves, and a path to control a loft. | Make section profiles and global shape control legible together, while eliminating generic loft setup choices irrelevant to the foil. |
| [ParaView displaying data](https://docs.paraview.org/en/latest/UsersGuide/displayingData.html) | **Verified:** central render views, selection-linked properties, separate display changes and compute operations. | One main result view, an explicit scalar legend, source provenance, and a deliberate Run operation. |
| [Windows keyboard interactions](https://learn.microsoft.com/en-in/windows/apps/design/input/keyboard-interactions) | **Verified:** guidance addresses keyboard power users and accessibility. | Visible focus, documented commands, logical traversal, and precision alternatives to dragging. |
| [Apple macOS HIG](https://developer.apple.com/design/human-interface-guidelines/designing-for-macos) | **Flagged:** official source identified, body requires JavaScript and was not text-inspectable in this run. | macOS command/menu and window behavior is a production proof obligation, not an HTML claim. |

These are interaction references. No logo, icon, screenshot, font file, or source
implementation is copied. Framework samples and their licenses remain outside this
design task.

## Constraints and anti-goals

Windows and macOS desktop, mouse/trackpad plus full keyboard, resizable windows,
AA contrast, reduced motion, high contrast, and a chart/table equivalent. The review
artifact is a self-contained HTML document, not evidence of native platform quality.
No hero page, promotional cards, decorative illustrations, persistent competing chat sidebar, generic
feature history, or duplicate station and curve geometry authority. No automatic
solver execution when a designer changes a number. No scientific result presented
as validated when uncertainty is unavailable.

## Trigger map

| Trigger | Applies? | Consequence |
|---|---|---|
| UI-T1 expert quantities | Yes | G1/G2, tabular numbers, units, direct plus exact editing, provenance, staleness, honest uncertainty. |
| UI-T2 generated assets | No | Geometry is native SVG/canvas; no generated visual assets. |
| UI-T3 AI model surface | Yes: optional BYOK assistant | A quiet prompt bar can propose starter fields and answer result questions. Proposed changes require review and Apply; answers carry provenance, unsupported questions are declined, no key disables invocation, and this prototype makes no network calls. |
| UI-T4 native client | Product target yes; framework undecided | Native desktop, cross-platform, framework and distribution undecided. HTML validates direction only. Native accessibility tree, DPI/windowing, keyboard, signing, and OS integration remain Flagged. No XAML exists to lint. |

## Initial surface inventory

Shape (viewport, five distribution curves, selected station inspector); Sections
(section family, anchors, normalized profile plot); Analysis (2D evidence and validity);
Simulate (setup, prerequisites, backend readiness and run lifecycle); Results (field,
legend, provenance and engineering questions); first run (create, open, example).
The settled specification will determine the final contracts before screens are built.

Every surface has default, hover, focus, active, disabled, loading, empty, error,
success, and overflow behavior. Technical dependents also have stale/recomputing;
backend workflows also have unavailable/partial/cancelled. Demonstration results carry
an explicit illustrative-data label. Review controls are outside the product shell.

## Highest-leverage direction decision

**One shape authority, one selection, one inspector.** Curves express continuous
scalar distributions; stations anchor section identity and expose evaluated values.
Editing a station's scalar value updates its owning distribution, never a shadow
number. This is the interaction to iterate first, before adding commands or panels.

## Geometry and evidence iteration — 2026-09-19

**Verified user intent:** catalog sections must become editable; section and outline curves need a weighted smoothing mode; Analyze needs coefficients and total loads with water and speed context; Simulate needs fresh/salt water and velocity-by-angle sweeps; Results needs richer ParaView-inspired inspection and replay.

**Inferred direction:** retain precise, composed, tactile and G1/G2. Make the curve's control polygon, approximating curve, selected weight and preserved constraints visible together. Exact interpolation and weighted smoothing are named modes. Preview/accept/cancel and Undo explain when the document changes. A catalog name remains provenance for an editable copy, never a claim that a modified section is still the catalog original.

Results gain one dominant field view with a source/field inspector, a compact operating-point metric strip, linked plots/table and a shared sweep transport. Adapt ParaView's field/representation separation, seed controls, scalar legends and synchronized views. Use explicit 2D section and 3D wing choices. Plotting style remains technical and restrained; no new promotional layout or decorative imagery. Coefficients are dimensionless, total force defaults to N with lbf display, and section-only force remains per unit span.

**Verified documentation reference (2026-09-19):** ParaView 6.1's [animation guide](https://docs.paraview.org/en/latest/UsersGuide/animation.html) distinguishes generated sequences from data timesteps. Its [filtering guide](https://docs.paraview.org/en/latest/UsersGuide/filteringData.html) makes operations depend on the input's available data. Workbench adapts these two principles as explicitly named sweep playback and field-capability gating; no ParaView runtime integration is selected.

Replay progresses through discrete operating points. Its speed is a presentation rate, not physical time. Keep camera, seeds and scalar range stable across frames; show missing cases as gaps. Modeled turbulence and separation diagnostics require named source fields and criteria. Illustrative prototype graphics state their limits visibly. Transient physical-time playback is separate and unavailable without time-resolved fields.

Additional state inventory: catalog/editable/conversion-preview; exact/weighted with selected control and invalid constraint; SI/imperial force display; fresh/salt properties; valid/invalid sweep bounds; queued/completed/failed/missing sample; paused/playing/end; 2D/3D and unavailable field. Existing theme, reduced-motion, narrow-window, keyboard and review-persona contracts apply to each.

The trigger union is unchanged: UI-T1 expert quantities and existing UI-T3 optional assistant apply; UI-T2 is absent; UI-T4 remains a future native proof obligation. No framework or CFD implementation is selected by these mockups.

## v1 elevation — 2026-09-20 (mode: elevate)

**Why elevate, not iterate.** The 2026-09-19 prototype was measured before this decision (`ui-craft-gate.py`: two
Minor findings; `tools/check-mockup.mjs`: 144 measurements, 22 oracles green). Its structure holds — G1 with one
selection and one inspector — but three things the specification now requires are absent or wrong in kind, not in
polish: the Smooth mode is a rational Bézier over the station values (the anchor-as-influence anti-pattern A4.2
forbids), the operating point has no depth so no result can state its basis, and the hard states the domain
produces (Not assessed, Pending admission with reason, Froude not modelled, infeasible locks with a removable list,
Flagged preset field) do not exist. Those are archetype-level and model-level gaps; the weakest layer is rebuilt.

**Who, and the state they arrive in.** The engineer/maker designing their own race foil, in flow, sceptical of
every number, moving between a curve and a chart many times an hour. They arrive with a shape in mind and a
speed in mind, and they want to know what they cannot trust before they trust anything.

**Job.** Turn a brief into a fair wing and a defensible comparison without leaving the document, and never be
shown a number without its basis.

**Archetype.** G1 Parametric Modeling Workbench (serial editing, one selection, one inspector) with G2 Scientific
Visualization Pipeline governing the Analyze charts; `Nav:TaskTabs+CommandPalette`; `Persistence:LocalDevice`.
Verified against the task shape: entering is serial (one control, one value), reading is parallel (the feasibility
matrix, the polar band, the loads panel) — the workbench separates them by pane, never by mode. Rejected again: a
dashboard of stat tiles, a chat-first shell, a feature tree.

**Three adjectives and their opposites.** **Precise, not vague** — every value has a unit, a step and an echo.
**Composed, not crowded** — one canvas dominates; the inspector, the Checks drawer and the assistant are
subordinate and collapsible. **Candid, not reassuring** — every honest limit is a rendered state with a fixed
string (Not assessed; Deep-water polar; practitioner range), never a footnote. Tactile is retained as a craft rule
(drag, then type the exact value) rather than a personality claim.

**Named references and what is taken (adapt, never clone; area 01 and 03 are the sources).** Shape3d — the
Tracing pointer readout and the five tangent kinds. Rhino — three-step nudge, the Fair contract (tolerance,
PreserveEnds), the comb as a persistent overlay. Onshape — comb evaluated at isolines and live on drag, stepped
keyboard orbit 15°/90°/5°, unit-aware expressions. SolveSpace — the DOF count and the removable-constraint list on
an infeasible lock set. Blender — Return/Esc modal rule, no undo step on cancel. KiCad — the DRC drawer shape (rule
id, severity, located violation, exclusion with reason). ParaView — the permanent provenance strip. No third-party
asset, icon or screenshot is used.

**Anti-goals.** No hero, no promotional stat tiles, no violet gradient, no cards inside cards, no jet or turbo, no
"safe", "validated", "optimized" or "cavitation-free" anywhere, no autoplay, no second inspector, no maker's AR
shown as b²/S.

**Constraints.** Native macOS and Windows desktop (framework unselected; HTML is the interaction prototype);
keyboard-first with a per-OS shortcut table and navigation presets; WCAG 2.2 AA with the accessibility target above
the market (Fusion's own VPAT lists keyboard and assistive-technology exceptions); a self-contained artifact under
300 KB with zero external requests; the tokens of `DESIGN.md`.

**Personality in three moves.** *Type* — the platform sans for labels and a tabular monospace for every quantity,
because the numbers are the product and misaligned decimals are a correctness defect (TQ2). *Colour* — the light
instrument chrome around a graphite canvas is kept; the single teal accent remains reserved for selection; scientific
colour is a documented policy (batlow or cividis sequential; a diverging map pinned at Cp = 0 for signed fields) and
never a validity indicator. *Space* — compact density with a stronger hierarchy than the prototype: the canvas
region is the one focal point, groups are separated by spacing before rules, and the status strip carries the mode
so the inspector need not.

**Trigger map (unchanged in kind; UI-T1, UI-T3, UI-T4 fire; UI-T2 does not).** UI-T1 moves the archetype to
catalog §G and makes colormap, uncertainty band and provenance strip correctness items. UI-T3 adds the wrong-answer,
unevaluated-model and withheld-numeral states. UI-T4 makes the Apple HIG and Windows guidance authoritative for
menus, modifiers and DPI, and keeps the native proof pack a Flagged obligation.

**Surface inventory for v1 (each with default / hover / focus / active / disabled / loading / empty / error /
success / overflow, plus the domain's hard states).** First launch on the Example · Brief (goal-state form,
per-field labels, class-rule validator, feasibility matrix) · Shape (canvas, five channels, station list, inspector,
Tracing, status strip, lines-plan mode) · Sections (catalog with admission classes and pending reasons; editor with
Through points and a genuine weighted least-squares Smooth, dashed polygon, off-curve controls, comb, break markers,
DOF and removable list) · Analyze (conditions band with depth, Fr_h, h/c, σ; section results as the Ncrit band and
the surface-state band; cavitation and ventilation screens with fixed strings; wing results with labels, omissions
and the Not-assessed loads panel; comparison with normalised deltas and a Discrepancy record) · Checks drawer ·
Export dialog (format matrix, STEP pending the CAM fixture, safety string) · Assistant (no key, unevaluated model,
rejected field, withheld numeral) · Settings (units, TE floor with its label, navigation preset, modifier scheme).

**Highest-leverage direction decision.** *The basis travels with the number.* Every quantity in Analyze is rendered
with its tier chip, its depth basis and its omissions in the same visual unit, so a screenshot cannot separate a
value from what it may claim. This is the interaction to build first; the weighted-control editor is second.

## v2 — seven areas, first-class (2026-09-21, mode: elevate)

**Why rethink, not extend.** The v1 mockup proved the honest-number contract inside four destinations. The
operator's direction makes the *jobs* explicit and discrete — Setup, CAD, Analysis, Experiment setup, Run,
Results, Export — with an AI prompt entry in every one, and asks for a rich representation of the target build
state including the areas v1 held in reserve. That is an information-architecture change, so the shell is rebuilt
around an **area strip in flow order** and the v1 panels are re-homed under it; nothing in the honest-number
contract is loosened.

**Who, and the state they arrive in.** The same engineer/maker, but now moving through a *pipeline*: they
arrive with a purpose and a rider, leave Setup with a seeded wing, iterate in CAD and Analysis until the local
numbers hold, define one experiment, wait on a run they can watch, read what came back, and export. Each hand-off
is a moment of doubt ("did it take what I meant?"), so every area shows the typed object it received and the one
it produces.

**Job.** Turn intent into a wing, a wing into evidence, evidence into a decision — in seven discrete steps that
never edit each other's objects, with a language entry at every step whose output is a typed proposal the user
accepts.

**Archetype per area (verified against the shape of the task).** Setup: Configurator with a live preview
(entering is serial; the preview is the one parallel read). CAD and Analysis: G1 Parametric Modeling Workbench
sharing one canvas; a toggle switches the layer set, never the room. Experiment: Configurator with a data-bounded
preview table (reading the cases is parallel, defining them serial). Run: a **process console** — queue, state
machine, live metrics, logs; the case is the unit; no dashboard tiles. Results: G2 Scientific Visualization
Pipeline — source → variable → view with the provenance strip permanent. Export: a modal. Rejected: a wizard
that locks areas until the previous is "done" (the areas are complementary, not sequential gates), a chat-first
shell (the prompt entry is subordinate to the area), a KPI dashboard for Run.

**Three adjectives and their opposites.** **Discrete, not conflated** — one job per area, one verb set per area,
hand-offs as chips. **Candid, not reassuring** — every state of a run and every absent layer is a rendered
string; a gated area says gated. **Rich, not decorative** — force vectors, loading strips, floods, streamlines
and replay exist because each answers a question the number alone cannot; every visual carries its basis and a
table twin. Precise and composed carry over from v1 as craft rules.

**Named references and what is taken.** ParaView — source/filter/view, Sequence over samples, the provenance
strip, "Open in ParaView" as the honest hand-off (11). OpenFOAM/SU2 practice — evidence by files, the mesh gate,
cancellation to the substrate (08). KiCad — the DRC drawer. Onshape/Fusion — the area/task strip as primary
navigation with readiness state. XFLR5 — polar and sweep chart conventions (conventions only; GPL). OpenMDAO /
pyOptSparse — the candidate provenance card and the multipoint definition (09). No third-party asset, icon or
screenshot is used.

**Anti-goals.** No wizard lock-step; no run progress bar that lies (elapsed and "remaining: Not recorded" until
measured); no autoplay; no "optimized" badge; no diagnosis without the named criterion; no `.f3d` promise; no
free-form shell from the assistant; no invented values across a failed sample.

**Constraints.** As v1 (native macOS and Windows target; HTML prototype; WCAG 2.2 AA above the market; the
DESIGN.md tokens; self-contained under 300 KB, zero requests), plus: the Run and Results areas render their full
target state on fixtures and are labelled "gated (SPIKE-03/04)" in the area strip.

**Personality in three moves.** *Type* — unchanged. *Colour* — unchanged; the area strip's readiness chips use
the semantic tokens (ok · warn · danger) with text, never colour alone. *Space* — the area strip is a single
row above the canvas; each area keeps the three-pane shell so the eye never relearns the room; Run and Results
trade the inspector's lower half for the console and the layer list.

**Trigger map.** UI-T1, UI-T3 and UI-T4 fire as in v1. New under UI-T3: the geometry-edit, experiment-config and
environment-step proposal kinds each add a rejected-field, a refused-step and an "outside allow-list" state.

**Surface inventory for v2 (each with default / hover / focus / active / disabled / loading / empty / error /
success / overflow plus the domain's hard states).** Area strip with readiness chips · Setup (language box with
redacted-payload preview and seeded planform; parameter form with purpose, soft targets with weight and deviation,
rider mass, water; goal panel; feasibility matrix; conflict state) · CAD (Outline · Twist · Dihedral · Thickness
curve selector; planform with both rails; station list with add/remove; section editor panel; catalog panel;
lines-plan) · Analysis (layer list; vectors, loading strips, Cp on section, depth and ventilation bands, cavitation
margin on the geometry; 2D/3D; charts; compare; the CAD ↔ Analysis toggle) · Experiment (sweep grid with case
preview and estimate; optimize form with objective, constraints incl. A_cav, design vector table, robustness, tier,
budget, seed; the single-point refusal; Queue) · Run (environment panel with detection, smoke test, allow-listed
steps with consent; queue with per-case state; console with the state machine, residual and force histories, mesh
gate bars, resource meters, Cancel, Retry; failed-case reasons; gated label) · Results (sample list; layer list with
absent reasons; viewport with flood, isolines, probe, streamlines with the labelled dash affordance, separation or
"No supported criterion", force vectors; timeline with Play/step/scrub and the held variable; small multiples;
metric-vs-α with gaps; difference flood; candidates with Pareto and provenance card; Open in ParaView) · Export
(STL/3MF, STEP gated, Fusion-ready STEP, 3DM, DAT, AVL, CSV) · Prompt entry per area with the capability states.

**Highest-leverage direction decision.** *One canvas, two layer sets.* CAD and Analysis share the geometry,
camera and selection; the toggle swaps layers. It is the interaction that makes "seamless" true and keeps the
areas discrete at the same time.

## v3 — a thick-client shell (2026-09-21, mode: elevate)

**Why rebuild the shell, measured.** The v2 artifact's window is 1,450–6,500 px tall at every window size (page
scroll everywhere; no panel scrolls internally) and its area strip wraps into two or three rows inside a 64 px title
bar, so the primary navigation clips. Both are web-page habits. The product is a desktop client: the window is a
fixed frame, every region scrolls inside itself, and the toolbar either fits or overflows into a menu — never
wraps, never scrolls. The archetype is unchanged (G1 workbench with G2 inside Results); the *layout facet* was wrong
(`Layout:ViewportWorkbench` was declared but a stacked page was built).

**Metaphors taken, and what specifically (adapt, never clone).** **VS Code / Eclipse** — the activity bar as the
primary navigation (one column of areas with badges), dockable side bars that collapse, a tabbed bottom panel
(Problems · Output · Terminal → Curve editor · Checks · Log · Cases · Timeline), editor tabs for open documents, the
status bar, the command palette. **Fusion 360** — contextual tool tabs whose toolbar changes with the workspace,
the Browser (tree) on the left, the parameter row beneath the toolbar, the Comments/Properties dock on the right.
**Rhino** — four named viewports with a title in each, the Layers panel as a docked list with visibility toggles,
the Properties panel following the selection, the command line as a text entry that mirrors every tool. **Shape3d**
— the lines-plan arrangement (plan · front · section) as a viewport layout rather than a page section, the station
list as a persistent panel, the Tracing readout inside the viewport. Nothing from the web-app playbook: no page
scroll, no hero, no stacked sections.

**Who and the state they arrive in; the job.** Unchanged from v2 (the engineer/maker moving through the seven
areas). The new constraint: they sit at a 13-inch laptop or a 27-inch monitor, keep the app open for hours, and
expect the room to stay still while the content changes.

**Shell (every area).** Menu bar (native on macOS; a strip on Windows) · **activity rail** (68 px as built — 56 px was too narrow for the area names: 1 Setup … 6 Results as a
tab list, Export as a dialog, Checks and Settings at the foot) · **toolbar** (one row, 44 px; groups from the area's
verb table; a `More ▾` overflow menu when the width is short — measured, never wrapped) · a **parameter row** where
the area has live inputs (Analysis conditions; Experiment grid) · **left dock** (Navigator: the area's tree —
stations · revisions · runs · queue · samples · layers; collapsible) · **editor area** (the dominant region: the
viewport with named-view tabs, or the area's document; document tabs above) · **right dock** (Properties/Inspector
following the selection, with the **prompt entry** as a second collapsible section) · **bottom panel** (tabs per
area; collapsible; a fixed share of the height) · **status bar** (24 px). Every dock scrolls inside itself; the
window never scrolls. Window presets: 1024 × 700 (minimum), 1280 × 800, 1440 × 900, 1600 × 1000, and 640 × 400 as
the 200 % scale case, where the docks become drawers.

**Per vignette — the best thick-client arrangement for the job.**
1. *Setup* — editor: the brief document (two roads side by side, the seeded preview beneath); bottom: Feasibility
   and Operating points tabs; right: brief properties + "Describe a starting design".
2. *CAD* — editor: the viewport (Plan · Front · Iso · Lines-plan tabs) with Tracing inside; bottom: Curve editor
   (Outline · Twist · Dihedral · Thickness as toolbar segments) · Checks · Catalog; left: Stations and Revisions;
   right: Properties + "Describe a change to the shape"; toolbar: mode, curve, nudge step, Fair, Add/Remove station,
   Edit section, Add profile from DAT.
3. *Analysis* — the same viewport with the layer set; parameter row: operating point, speed, water, depth, α with
   the derived q · Re · h/c · Fr_h · σ · V_crit as read-only chips; bottom: Results (quantities and strings) ·
   Charts · Compare; left: Layers (Rhino-style list) beneath Stations; right: Run manifest + "Ask about this
   calculation". The toggle with CAD is a toolbar segment and the chord; the viewport, camera and selection stay.
4. *Experiment* — editor: the definition document (Sweep | Optimize as document tabs); bottom: Cases (preview
   table with derived quantities and the estimate); right: experiment properties + "Describe the experiment".
5. *Run* — editor: the console for the selected case (state machine, residual and force histories, mesh gate,
   resources); left: the queue as the navigator; bottom: Log · Environment; right: backend environment + "Explain
   this failure" / "Prepare my environment"; toolbar: Run, Cancel, Retry, Step (fixture, review harness only).
6. *Results* — editor: the results viewport with the layer list docked left; bottom: Timeline · Sweep (small
   multiples, metric vs α, difference flood) · Candidates; right: provenance + "Ask about this result".
7. *Export* — a modal dialog from the rail (the declared C1 archetype), never a page.

**Three adjectives and their opposites.** **Still, not scrolling** — the room stays put; content changes inside it.
**Dense, not cramped** — 32 px dense controls, 8 px separation, one row per toolbar, tables in panels that scroll.
**Native, not webby** — menus, docks, tabs, status bar; no hero, no cards, no page.

**Anti-goals.** No page scroll; no wrapping toolbar; no toolbar that scrolls; no panel taller than the window; no
duplicate navigation (the rail is the only area navigation; document tabs are documents, not areas); no floating
chat window (the prompt entry is a dock section); no animated layout.

**Constraints.** Native macOS and Windows target (framework unselected; HTML prototype); WCAG 2.2 AA; the tokens
of `DESIGN.md` plus the shell tokens added in this run (rail, dock, bottom panel, status bar, menu bar); zero
requests; ≤ 300 KB. The v2 renderers are reused where their content is right; their targets are re-homed into the
docks, and any renderer that emitted a whole page is split into editor · bottom · right pieces.

**Personality in three moves.** *Type* — unchanged. *Colour* — unchanged; the activity rail is the graphite of the
viewport so the eye reads rail + viewport as the instrument and the docks as paper. *Space* — the window is the
unit; docks take fixed widths, the editor takes the remainder; the bottom panel takes 30 % of the height or
collapses to its tab strip.

**Trigger map.** UI-T1, UI-T3 and UI-T4 fire as before; UI-T4 now shapes the shell directly (menus, docks, per-OS
modifiers). UI-T2 does not fire.

**Highest-leverage direction decision.** *The window is the unit.* Fixed frame, internal scroll, one-row toolbar
with measured overflow. Everything else in v3 follows from it.

## v4 — CAD editing views: elevations, control curves, a station document, free 3D (2026-09-21, mode: elevate)

**What is being elevated, measured.** v3 settled the shell. Its CAD is still a *viewer with a curve pane*: the
viewport draws one projection at a time (plan, front or a fixed-pitch iso rotated by keyboard only), the outline is a
60-point polyline, the station editor is a modal `<dialog>` over the shell, the four channels are edited only as
η-plots in the bottom pane, the rail counts the areas with numerals, and there is no pointer orbit or wheel zoom
(v3 review, ranked plan item 2). The operator's list is the little things a CAD user reaches for first.

**Metaphors, taken specifically.** **Rhino** — four named viewports (Top · Front · Right · Perspective) that are
*the same model* under different cameras; editing happens in the ortho views, the perspective view is for looking;
free orbit by pointer with a pan/zoom that never surprises. **Fusion 360** — the sketch environment: entering a
sketch is a *mode* with its own toolbar and a Finish button, not a dialog; the view cube in the corner for named
views and free rotation; the browser tree shows what is being edited. **Shape3d** — the board's rocker (side),
outline (top) and thickness (front) as three explicit control curves drawn *on* the elevation they shape, each with
its own control points; the slice editor as a full 2D view with the reference profile ghosted. **VS Code** — the
activity bar as icons with names on hover and in the accessible name, never numerals. **Curves** — every curve on
screen is a spline (Catmull–Rom through the evaluated samples, or the B-spline's own control polygon on demand);
polylines are a tell.

**Who arrives and the job.** The engineer/maker in CAD wants to *shape*: look at the wing from any angle, then edit
the outline in Top, the dihedral/anhedral in Front, the twist and thickness in Side, and a station's profile in a
2D section editor, each with control points on the curve being shaped and the other curves ghosted for reference.

**Decisions.**
1. **The viewport is one camera over one model.** Named views (Top · Front · Side · Iso) set azimuth and elevation;
   free orbit (Alt+LMB in the Workbench preset, RMB in the Rhino preset; keyboard Alt+arrows, `[` `]`), pan (MMB or
   Shift+drag) and wheel zoom change the same camera; a **view cube** in the corner shows the camera and its faces
   are the named views. Fit (F) frames the model. The pointer contract joins the spec (B7).
2. **Elevations are editing views.** Top edits the **Outline** (LE and TE rails); Front edits the
   **Dihedral/Anhedral** curve (elevation z along span) *and* **Thickness** (t/c on the band edge — both are spanwise
   distributions and read on the same axis, as Shape3d draws them); Starboard is a **body plan** (one row per
   station) that edits **Twist** (a lever from the quarter chord per station). Each control curve is drawn on its elevation with its control points, the
   other curves ghosted; dragging a point opens the same Preview → Apply/Cancel draft as the curve pane; the curve
   pane stays as the η-plot twin. Lines-plan shows Top · Front · Side together with the same selection.
3. **A station is a document.** "Edit section" opens a **Station document tab** (`Station η 0.60 · NACA 66-209`) in
   the editor group: a full 2D section view with grid, chord and thickness dimensions, upper and lower control
   points, the catalog original ghosted, its own toolbar (mode · step · Apply · Cancel) and Properties; Escape or
   Cancel closes the tab and discards, Apply commits and closes. The dialog is gone.
4. **Icons on the rail.** Inline SVG glyphs per area with the name beneath at desktop widths; the accessible name
   still carries the readiness string.
5. **Free-form 3D is a *looking* view in this iteration.** The 3D view shows sections at stations, both rails and the
   selected control curve as splines; a station is selectable in 3D (it becomes the selection everywhere); editing
   stays in the elevations and the station document, which is the Rhino discipline and keeps every edit a typed,
   previewable draft. Direct 3D handle dragging is recorded as a next step with the risk named (a 3D drag has no
   unambiguous plane without a gizmo).

**Anti-goals.** No second geometry model for the elevations (they read the same channels); no modal for editing; no
polyline where a spline belongs; no numerals in the rail; no camera that a named view cannot reach.

**Tells to self-check.** A curve rendered as straight segments; a station editor that hides the shell; a view that
cannot be orbited back to Top; a control point that edits a curve it does not belong to; a rail icon without a name.

## v5 — a first-class CAD experience for the wing paradigm (2026-09-21, mode: elevate)

**What is being elevated, measured.** In CAD at 1280 × 800 the v4 screen shows **71 interactive controls in ten
regions** and 203 text-bearing elements (rail 9 · toolbar 6 · navigator 7 · document-tab row 7 · viewport 13 ·
bottom-tab strip 5 · the curve-editor pane 19 · properties 5). The anchors on the curves are *fit points* —
the curve is forced through them — and the "Smooth · weighted controls" mode is a second fit, so nothing on
screen behaves like the control-point spline a Fusion 360, Rhino, Shape3d or MultiSurf user expects: a control
polygon whose vertices *pull* the curve, tangent levers at the ends, a comb on demand. The 3D view shows sections
and rails but no body to shape. The operator's diagnosis, verbatim: *busy; through points; no levers*.

**Metaphors, taken specifically.** **Fusion 360 control-point spline** — the curve lies inside its control
frame; drag a vertex and the curve follows without passing through it; the frame is the lever; the *fit-point*
spline (through points, with tangent handles) is the other tool, for data you must honour. **Fusion 360 Sculpt
(T-spline bodies)** — one body, a control cage; *box* and *smooth* display modes; edit the cage, see the body.
**Rhino** — four viewports (Top · Front · Right · Perspective) over one model, a viewport maximised by a
double-click on its title; control points as squares on a dashed polygon; `PointsOn`, the comb (`CurvatureGraph`),
the Gumball; commands typed or clicked, options in a strip. **Shape3d** — the board is three master curves
(outline · rocker · thickness) with control points, stacked on the elevation they shape; slices edited in their
own 2D view against a ghost. **MultiSurf** — *relational geometry*: master curves are B-splines (NURBS) defined by
control points; every dependent curve, section and surface is a function of the masters and updates when they do;
nothing is edited twice. **NURBS is the representation** the product must own — this iteration names the kernel.

**Who arrives and the job.** A wing designer who *shapes*: pulls a control vertex and watches the outline,
the section rows and the body change; tunes the twist and thickness distributions the same way; fairs with a comb;
then reads dimensions. Precision entry, nudge and locks remain, but the pointer path is primary.

**Decisions (direction; the geometry and marine-CAD peers confirm the rows marked ◆).**
1. **Control vertices are the default.** Each master curve (LE rail, TE rail/chord, dihedral/anhedral, twist,
   thickness) is a B-spline whose control polygon is the editing frame: CVs as squares on a dashed polygon, dragged
   directly (no fit); end tangents as levers on the clamped ends; the comb on demand; **Fit points** is the second
   tool (through anchors with tangent handles) for measured data and DAT imports; the weighted-LSQ "Smooth" mode
   folds into **Fair** (a tolerance operation), not a mode. ◆
2. **One focal region.** CAD opens as **four viewports** over one model — Top · Front · Starboard (body plan) ·
   Perspective — each maximisable by double-click on its title; the elevations edit, the perspective looks (with
   a cage). The η-plot pane and the bottom tab strip leave the default CAD screen; the η-plot is a twin one action
   away (View ▸ Curve plot). Target: ≤ 35 visible controls at 1280 px.
3. **A tool palette, not a verb toolbar.** A vertical palette beside the viewports carries the CAD tools as
   modes and one-shot commands (Select · Move CV · Insert CV · Delete CV · Lever · Fit points · Fair · Comb ·
   Station · Section · Measure); the active tool's options sit in the parameter row (Fusion's options strip); the
   application toolbar keeps only what is not a drawing tool. ◆
4. **The body has a cage.** The perspective view shows the lofted NURBS surface as a *body* with **box** (cage =
   the CV net of rails × sections) and **smooth** display modes; selecting a cage row selects that master curve or
   section everywhere. Cage-vertex dragging in 3D arrives with a Gumball (axis-constrained) or not at all. ◆
5. **Sections are CV curves too.** The Station document edits the upper and lower curves as control polygons
   with closure and LE-tangency as CV constraints; the catalog original stays the ghost.
6. **The kernel is named.** The spec states the geometry kernel the real implementation needs — NURBS curve and
   surface evaluation, lofting, constraint solving, STEP/3DM I/O — with candidates, licences, the honest T-spline
   position (a lofted NURBS surface with a cage; true T-splines need a licensed kernel) and the spike that settles
   the choice.
7. **The wider UX is cut back around CAD.** What a CAD-first user would hide or merge (a rail entry, a dock
   section, a duplicated readout) is cut, not restyled.

**Anti-goals.** No third fit mode; no drawing tool on the application toolbar; no readout that appears twice; no
"T-spline" claim without star points; no control that survives the count only because it was already there.

**Tells to self-check.** A curve passing through its handles by default; a polygon without levers at the ends;
a viewport that cannot be maximised; a palette item that is really a menu; a body without a cage; a 3D drag with
no plane; the control count creeping back over 35.

### v5 — measured outcome (2026-09-21)

Visible chrome controls in CAD at 1280 × 800 on entry: **45** (v4: 71; the ≤ 35 target was not met — the remaining set is the rail's seven areas plus Checks and Settings, four toolbar controls, nine palette verbs, four viewport titles with their menus, the Checks tab strip, the options strip's curve selector and Properties' nine controls; cutting further removes a verb or an area). Mid-session with docks open and a station selected: 52. Every vertex is a named slider; the oracle measures that a vertex pulls its curve without reaching it (gap ≈ 0.3 × the move, curve moves ≈ 0.6 × the move) and that a tip-side vertex leaves η 0.10 unchanged to 10⁻¹². The station conversion meets its 10 µm acceptance with twelve vertices per side (7.5 µm upper, 7.1 µm lower at the 89.6 mm chord) once the parameters are centripetal and the knots are placed by averaging — the uniform-index fit the first draft used measured 841 µm and was hidden behind a constant "0.000". Craft gate: fourteen Minors (nine "cramped padding" on the edge-to-edge viewport panes and their 32 px title bars — accepted, a viewport is edge-to-edge in every comparable; one side-tab stripe on the pressed palette tool — accepted as the pressed-state indicator the rail already uses; two clipped positioned children and one monotonous-spacing note carried from v3; the em-dash count, a recorded deviation).

### v5 — the operator's finding after publication (2026-09-21)

"I still don't see the control handles for the CV splines." The v4 review had cut the frames to one curve at a time to fight busyness, which hid the primary affordance: in Front and Starboard nothing could be grabbed until a curve was first clicked. Every curve's frame now renders in the elevation that shapes it (active emphasised, the others at 0.62 opacity and draggable), glyphs 13 px. Busyness is measured in chrome controls, not in handles on the geometry — the chrome count is unchanged (handles are excluded from it by definition).

