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

Mode: **create**. Written before screens. Status: proposed direction for iteration;
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
