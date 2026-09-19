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
  - { to: design-language, rel: documents }
review-by: 2027-03-18
summary: >-
  Words-first creative direction for the hydrofoil workbench. A bounded parametric
  canvas joins scalar span distributions and station section anchors in one model,
  with precision editing and visible evidence limits.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: design-language, on: 2026-09-19, reason: "Initial cross-platform workbench token and interaction language created for review." }
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
