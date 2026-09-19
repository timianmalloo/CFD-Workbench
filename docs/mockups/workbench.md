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
summary: Self-contained HTML workbench with editable catalog section curves, weighted smoothing, fluid-aware force previews and linked velocity–angle sweep visualization. The review harness and synthetic field fixtures demonstrate interaction contracts, not a production geometry kernel or solver.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: design-language, on: 2026-09-19, reason: "Initial cross-platform workbench token and interaction language created for review." }
---

# Interactive workbench

[Open the high-fidelity HTML](workbench.html) · [Specification](../specs/cfd-workbench.html) · [Design vocabulary](design-language.html)

Open the HTML directly in a browser. No build, server, API key, solver, internet or external font is needed. Use the review bar to change persona, viewport, state, theme, density, capability and motion. Review controls are not proposed application chrome.

The product destinations are **Shape, Sections, Analyze, Simulate and Results**, with an explicit first-run state. Shape and Sections expose Through points and Smooth weighted modes. A catalog NACA 0012 opens an editable copy; upper/lower curve controls, exact offsets and influence weights reshape its profile. Preview, Cancel, Apply and Undo preserve the source and accepted revision. Effective thickness has one owner, and the loft reads the same profile definition.

Analyze shows section or wing coefficients, coefficient ratio and finite-wing total Lift/Drag for fresh or salt water and a chosen velocity/angle. Forces default to N with lbf conversion. Simulation setup creates a Cartesian velocity × angle fixture schedule. Results links case selection, scalar coloring, streamline rake, slice, force metrics, charts and sample table. Play, pause, step and scrub traverse operating points at held velocity or angle; this is never transient physical time. The bounded evidence region scrolls independently beneath the dominant canvas.

Pressure, velocity, modeled turbulent kinetic energy and signed wall-shear fixtures have fixed scalar legends. The signed wall-shear example marks synthetic C𝒻 < 0 reversal with hatching; partial state shows absent wall data. Missing cases pause playback and clear fields/quantities. Pathlines and resolved turbulent motion explain their unavailable dependencies. Native file dialogs, solver runs, installations and AI responses remain labeled demonstrations. Scientific values are illustrative, not measured, with unquantified model uncertainty.

The spec is the future product contract; this mockup is interaction guidance. Its geometry is a bounded illustrative evaluator. Production fairness/continuity, scientific validity, native keyboard/accessibility trees, backend setup and signed distribution still require the [native proof obligations](../proof/native-ui-workbench.md) and specification release gates.

Best first review: change a section control and its Smooth weight, compare preview against the source, cancel once, then apply and undo. Set a water type and velocity–angle sweep, select 6 m/s at 8°, choose signed wall shear, switch 2D/3D and replay toward the failed 12° case. Observe the explicit gap, fixed scalar scale and linked case identity. The subsequent implementation must preserve these relationships rather than copying static pixels alone.
