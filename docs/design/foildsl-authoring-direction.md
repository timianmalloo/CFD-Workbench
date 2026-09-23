---
id: design-foildsl-authoring
title: FoilDSL authoring direction and transaction contract
type: design
status: in-review
owner: "@timianmalloo"
tags: [foildsl, ux, cad, direction]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: adr-foildsl-authority, rel: depends-on}
review-by: 2026-12-22
summary: Keep the spatial CAD workbench and add a source document with explicit validation and shared transactions, drawing useful authoring ideas from the supplied JSX without importing its scrolling page or alternate geometry model.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Direction before pixels

For the technically fluent designer arriving with an existing foil and an exact change in mind.
Job: describe a repeatable foil, inspect its geometry, and make a reversible edit without losing its
explanation or confusing an old simulation with the new shape. The user intent is Verified from the
request; no usability study is claimed.

**Precise / not decorative. Reversible / not surprising. Inspectable / not opaque.**
Retain DESIGN.md's ParametricWorkbench archetype and §G expert spatial-workbench selection:
`Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact;
Nav:TaskTabs+CommandPalette; Input:PrecisionPointer+SpatialGestures+KeyboardFirst;
Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic+Confirmed; Pacing:Freeform`.
Existing remaining facets and tokens are unchanged. Reading the shape and source is parallel;
editing has one transaction owner. A source document is a sibling of the foil and station documents.

References: current v5 gives the four-view CAD shell, direct controls and precision Properties;
the supplied FoilDSL Explorer JSX gives named language blocks, source diagnostics and inspectable
section schedules; its full-page sequence is not the window layout. The existing Eclipse/VS Code
document metaphor and Fusion/Rhino CV metaphors remain as documented in workbench-direction.
Type: existing sans for controls and monospace for source/numbers. Color: existing adaptive tokens
with teal selection and danger diagnostics. Space: internally scrolling source inside the fixed
window; the geometric canvas remains the spatial view, not a dashboard of cards.

Anti-goals: a second foil model, silent source reformatting, arbitrary executable scripting,
pretend physics, a mandatory source-code workflow, or a new application stack.

## Trigger union and surface inventory

UI-T1 fires: units, precise edits, provenance and uncertainty are required. UI-T2 does not: no generated
assets. UI-T3 remains the existing optional assistant's contract; this change introduces no model call.
UI-T4 is a future cross-platform native product obligation; the artifact is HTML direction evidence,
framework undetermined, not native accessibility/DPI/signing proof. Native proof remains flagged.

| Surface | State and behavior |
|---|---|
| FoilDSL document | accepted, dirty, validating, valid preview, syntax error, incomplete, semantic error, unsupported; source never replaced while typing |
| Source actions | New/Open begin candidate; Validate checks; Preview shows labelled candidate; Apply only after successful current validation; Cancel restores accepted source |
| CAD controls | normal/focus/hover, shared draft, source-draft exclusion, locked/invalid edits, accepted/undo/redo |
| Section schedule | named assignments, authored station versus inspection slice, source location jump, shared-profile consequence |
| Diagnostics | live status with code/location/repair; invalid input keeps accepted shape; unavailable geometry proof explicitly named |
| Persistence | save accepted source, reopen into a draft, malformed/oversize/unsupported file leaves accepted state intact |
| Freshness | accepted definition versus pinned illustrative run; trivia-only changes keep freshness; geometry changes mark Historical |
| Review harness | all inherited personas/windows/themes/density/hard states/reduced motion plus source error fixtures |

## Interaction contract

Entering FoilDSL with a visual draft presents its generated candidate as read-only until Apply/Cancel
in CAD; entering a visual editing verb with a source draft refuses with a route back to that draft.
Source validation has a captured base revision and input generation. Preview is transient, carries its
label, and never feeds Analysis. Editing again invalidates the prior validation. Apply is atomic;
Cancel returns accepted source and shape. Repeated Apply without new input does nothing.

A visual edit patches the same definition and publishes text with exact numeric precision. Existing
comments survive; a bounded prototype may preserve comments but canonicalize whitespace, visibly
declared. No production source-preservation claim is made from this prototype behavior.
The language declares profiles explicitly; a shared profile edit must name all affected stations.
The prototype can scope support to one shared profile, but must reject unsupported assignments.

## Copy added by this section

| State | Exact copy |
|---|---|
| Source document tab | FoilDSL |
| Dirty source | Source draft. Validate before Apply. Accepted geometry is unchanged. |
| Valid candidate | Valid prototype subset. Preview is illustrative; full geometry proof is not performed. |
| Invalid candidate | Source rejected. Accepted geometry is unchanged. |
| Visual exclusion | Apply or cancel the visual draft before editing FoilDSL. |
| Source exclusion | Apply or cancel the FoilDSL draft before editing geometry. |
| Applied source | Source applied. Geometry and text share one definition. |
| Cancel | Draft cancelled. Accepted source and geometry restored. |
| Unsupported v3 | FoilDSL 3 requires migration preview; no automatic conversion is performed. |

## Motion and performance

No new animation. Document switch is a hard cut; inherited spatial gestures honor reduced motion.
Source validation is explicit and bounded. Display measured elapsed milliseconds and input bytes;
do not invent a percentile from one sample. Product A8 performance targets remain release criteria.

## Review and proof

Before UI edits, baseline Chrome inspection found the fixed 1280×800 shell rendered without required
token-contrast/target failures and no source entry. New-control test observed RED. Independent
ownership/geometry gate precedes UI; browser oracle and independent rendered review follow it.
Acceptance includes parse/emit/parse equivalence, idempotent serialization, one-draft exclusion,
text and visual edits feeding the same 3D reader, bounded invalid input, source/geometry history,
freshness, real download/reopen and five-window shell regression. Native and scientific proof remains
out of scope, with explicit release gates rather than manufactured passes.
