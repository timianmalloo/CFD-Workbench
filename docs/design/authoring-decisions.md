---
id: design-authoring-decisions
title: Section authoring and design-decision interaction direction
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ux, cad, profiles, authoring, comparison]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: decision-design-iteration, rel: depends-on}
  - {to: design-language, rel: depends-on}
  - {to: design-foildsl-authoring, rel: refines}
review-by: 2026-12-22
summary: >-
  Elevates the existing spatial workbench around the complete alternative-to-decision task: visible section
  entry, explicit shared scope and thickness intent, read-only draft inspection, and honest baseline evidence.
  Reuses the established design language and separates prototype proof from native/scientific obligations.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Direction: make scope and consequence visible

The operator has a foil and wants to try a specific change, understand which span it affects, then retain or
discard it against a fixed reference. The unit of work is one design decision, not one isolated control move.
**Verified intent:** the authorized five recommendations and spec correction. **Inferred usability benefit:**
keeping the section entry and draft owner visible reduces target confusion. UX-26 must test that inference;
automated clicks are not participant evidence.

Retain the ParametricWorkbench archetype and DESIGN.md tokens:
`Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact;
Nav:TaskTabs+CommandPalette; Input:PrecisionPointer+SpatialGestures+KeyboardFirst;
Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic+Confirmed; Pacing:Freeform`.
The prototype's session-only alternative storage is a disclosed implementation limit, not a change to the
product's persistence facet. Inspection can happen in parallel; mutation has one draft owner. Source remains
a view of the same shape authority. No new product area, solver, network service or application stack is added.

## Structure before surface

The existing v6 workbench supplies four viewports, source transactions and precision Properties. The v3
Explorer contributes visible section relationships, without adopting its immediate commits or page layout.
The new persistent station card connects selected location to its actual section and Edit section. It sits
with document context, survives maximization, and has a compact stacked form when space is narrow. Do not
cover the geometry with controls. Use existing sans/monospace, adaptive tokens and focus rules.

Before section mutation, choose scope and thickness policy. Both the text and highlighted geometry name the
same affected assignments/intervals. Make independent means assignment independence; neighboring blends can
still change. A persistent draft owner separates the editable target from what is currently inspected.
Inspection never starts, redirects or accepts a write.

An explicit intent dialog handles chord and span because “change a dimension” leaves what stays fixed
ambiguous. It shows held rail or station-position policy, units, changed controls, frame/pivot disclosure,
preview and Apply/Cancel. A separate alternative dialog groups name, baseline, comparison and rationale.
The spatial workspace remains the primary editing surface; those dialogs are short decision steps.

**Measured density adjustment:** the coordinator's v7 CAD run observed 49 visible chrome controls at
1280 × 800; its inherited ceiling of 48 rejected that entry state. Revision 1.5 sets CAD-07 to at most 51,
allocating exactly three additional persistent entry actions: Edit section, Edit intent and Alternatives.
This is the explicit discoverability tradeoff authorized by the five recommendations, not an exemption from
target-size, focus, keyboard, overflow or viewport checks. The final browser rerun confirms 49 controls
within the 51 ceiling and passes the inherited 16-group CAD oracle (77 measurements, 30 shell cells), as
recorded in the [proof](../proof/authoring-decisions.md). Historical
v5 measurements and its earlier ceiling remain historical evidence.

## State and accessibility contract

| Surface | Required states and recovery |
|---|---|
| Station card | selected assignment, shared/independent, no selection, derived slice, missing profile, inspection during draft; name next available action |
| Section draft | scope/policy selected, active target, another station inspected, valid candidate, invalid geometry, infeasible thickness pins; Apply only when valid; Cancel always restores accepted source |
| Dimension intent | default held edge/policy, preview, locked/infeasible target, outside/duplicate station, accepted/cancelled; errors name the blocking item |
| Alternative | no baseline, named active trial, fixed baseline, pending draft, accepted comparison, retained/archived decision; rationale empty/valid and readable historical state |
| Evidence | compatible, absent, historical, incompatible; missing scientific evidence contains its reason, never a plausible number |
| Source/native | shape-only import/export and project save/open remain distinct; prototype labels session-only decisions |

Every control has a visible label, keyboard path and focus indication. Overlay differences use line style
and text in addition to color. Thumbnails have a text equivalent; interval reach is written as well as drawn.
Keyboard users can inspect another station and return to the draft target. Dialog Escape, close and focus
return must preserve the draft according to its explicit action. Workspace single-letter tools never fire
while typing names, rationale or source. Reduced motion changes no semantics. Reflow retains access to every
action and readable errors without document-wide overflow. No new animation is needed.

## Trigger union and review

UI-T1 remains active for dimensional precision and provenance. UI-T2 adds no generated assets. UI-T3 adds
no assistant behavior. UI-T4 native Windows/macOS proof remains a future product obligation; HTML accessibility
does not establish native accessibility, signing or DPI behavior. Review the actual rendered surface at
inherited harness sizes, personas, themes and reduced motion. Run deterministic craft, token, accessibility
and overflow checks, then inspect structure and truthful copy. A clean detector is not an independent verdict.

The executable review path is alternative → middle station → shared/independent section draft → inspect
impact → Apply → compare → rationale → Keep/Discard, with cancellation, infeasibility and missing evidence.
Record normal-path timing, events and failures without source/rationale contents; absent measurements say
Not recorded. Browser proof must cross source/profile/assignment/readout/viewport/history consumers. Root's
independent reviewer holds the geometry/data and UX/accessibility veto after actual v7 inspection.

The functional/UX/UI acceptance is product CAD-09–13, UX-26–27 and UI-31–35. Native persistence, exact blended
slice promotion, certified continuous geometry, scientific comparison and the formative study remain clearly
named obligations. Do not expand this design task into implementing those systems.
