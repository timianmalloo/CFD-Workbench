---
id: mockup-workbench
title: CFD-Workbench interactive design prototype
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, curves, stations]
links:
  - {to: spec-cfd-workbench, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: proof-native-ui-workbench, rel: relates-to}
review-by: 2027-03-18
summary: Self-contained HTML workbench for iterating curves, station profiles and the shape-to-evidence workflow. The prototype includes review controls and illustrative scientific data; it is not a production geometry kernel or solver.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: design-language, on: 2026-09-19, reason: "Initial cross-platform workbench token and interaction language created for review." }
---

# Interactive workbench

[Open the high-fidelity HTML](workbench.html) · [Specification](../specs/cfd-workbench.html) · [Design vocabulary](design-language.html)

Open the HTML directly in a browser. No build, server, API key, solver, internet or external font is needed. Use the review bar to change persona, viewport, state, theme, density, capability and motion. Review controls are not proposed application chrome.

The product destinations are **Shape, Sections, Analyze, Simulate and Results**, with an explicit first-run state. The central iteration is selecting a station, editing a curve or exact dimension, reviewing recipe detachment and using Undo. Native file dialogs, solver runs, installations and AI responses are labeled demonstrations. Scientific values are illustrative fixtures with unquantified model uncertainty.

The spec is the future product contract; this mockup is interaction guidance. Its geometry is a bounded illustrative evaluator. Production fairness/continuity, scientific validity, native keyboard/accessibility trees, backend setup and signed distribution still require the [native proof obligations](../proof/native-ui-workbench.md) and specification release gates.

Best first review: do the station/curve/inspector controls make the active shape parameter obvious, and can you fair a wing without losing your parametric explanation? The subsequent implementation must preserve that relationship rather than copying static pixels alone.
