---
id: defect-classes
title: CFD-Workbench defect-class register
type: doc
status: accepted
owner: "@timianmalloo"
tags: [lessons, controls, geometry, specification]
links:
  - {to: spec-cfd-workbench, rel: relates-to}
review-by: 2026-12-19
summary: Design-time failure classes and their mandatory checks, loaded at session grounding under AGENTS.md. Product-runtime controls remain explicitly pending until the corresponding implementation exists.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
---

# Defect classes

This register is an always-loaded grounding control under AGENTS.md. Each row makes a future author check the class before changing its surface. Acceptance oracles below are specified controls, not claims of executed application tests.

| Class | Class → sweep → derive → prevent | Control and status |
|---|---|---|
| GEO-A · Competing shape authority | Catalog assignment, thickness interpolation and a t/c channel can each appear to own thickness. Swept A4, GEO-07, profile conversion and result invalidation. Derive effective coordinates from one t/c owner after normalized shape blending. | Standing review rule: blend profiles with different thickness-peak locations, renormalize before t/c; test retained override versus source thickness. A4/GEO-07 specify it; runtime fixture pending. |
| UX-A · Optional path becomes mandatory in flow | A flowchart linked export only from accepted AI output despite global export prose. Swept F1–F5 and IA for optional-dependency bottlenecks. Derive independent entry for global actions and a failure return path. | Standing flow review rule: trace no-key New→edit→save→export without AI nodes. F5 corrected; future keyboard E2E trace is required. |
| SPEC-A · Scope named but not falsifiable | Broad “analysis” requirements omitted transition/load/critical-speed outputs named in proposals. Swept all six source stages against story IDs. Derive explicit supported and unavailable paths. | Source-to-story coverage matrix plus ANA-10–14/AI-05; independent Product/Test review confirmed closure. |
| FRAME-A · Handedness asserted from intuition | A transient draft mistakenly called aft/starboard/up inconsistent. Swept coordinate, incidence and mirrored-half wording. Derive from forward/starboard/down: flipping x and z preserves determinant +1. | Always-loaded rule: express basis against a known physical reference and calculate determinant before changing signs; never “correct” a source from mental rotation alone. Final A4 uses the source's right-handed frame; numerical geometry tests remain a handoff requirement. |
| FIT-A · Approximate conversion treated as categorical error | Draft prohibited a fit from reporting zero, even if measured. Swept conversion, CST and source-identity claims. Derive error from observed comparison, not method label. | Always-loaded rule: conversion reports measured residual and tolerance; zero is permitted only with evidence, never assumed. A4 updated. |
| UI-B · Hidden state overridden by authored display rule | The specification contents filter set `hidden`, but the navigation `display:block` rule kept all 27 links visible. Swept prototype author guidance for the same selector shape. Derive proof from rendered visibility, not the attribute alone. | Explicit `[hidden]` rule; browser oracle observed 27 visible links before the fix and exactly 1 after the geometry query. Standing rule: harness/filter assertions inspect rendered visibility. |
| UI-C · Rerender discards focus or capabilities | Workspace replacement dropped keyboard focus to BODY; narrow CSS hid project navigation. Swept all task routes and review selectors. Derive semantic focus restoration and an explicit project drawer instead of hidden functionality. | Portable `tools/check-mockup.mjs` covers repeated keyboard edits, dialog return focus and narrow project access. Independent reviewer observed the original failures and corrected behavior. |
| EVID-A · Historical presentation reads mutable current state | Results were labeled revision 12 while rendering current edited stations. Swept snapshot geometry, pressure masks and source labels. Derive the result view from its immutable fixture input; partial evidence must actually be masked. | Browser oracle edits the current shape and requires historical Results SVG unchanged; partial Results must contain masked paths. |
| GEO-B · Readout and preview use different evaluators | Quintic preview replaced linear geometry, but area still summed linear station segments. Swept area/AR and profile preview commit semantics. Derive readouts from the same curve evaluator, or label an unaccepted profile preview without mutating the design. | Tangent-edit regression requires linked dimensions to update; unaccepted section preview leaves design revision/geometry intact. Controls live in the portable mockup check. |

No product defects or scientific validation tests are claimed executed in this documentation/prototype task. Controls will fail the implementation gate if these fixtures or end-to-end paths are absent.
