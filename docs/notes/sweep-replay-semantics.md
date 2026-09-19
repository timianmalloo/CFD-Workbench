---
id: note-sweep-replay-semantics
title: Sweep playback selects an operating point, not physical time
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [decision-note, simulation, visualization, provenance]
links:
  - {to: spec-cfd-workbench, rel: relates-to}
  - {to: mockup-workbench, rel: relates-to}
  - {to: kb-cfd-workbench-grounding, rel: depends-on}
review-by: 2027-03-18
summary: A velocity-by-incidence sweep has discrete case identities, and every result surface follows one selected case. Playback cannot imply transient fluid time or carry fields from a missing case's predecessor.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract." }
  - { by: mockup-workbench, on: 2026-09-19, reason: "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof." }
---

# Sweep playback selects an operating point, not physical time

Kind: decision. Confidence: **Inferred product interpretation** of the explicit user request and verified ParaView/CFD-Bench source documentation. Made during specification revision 0.2 and the 2026-09-19 ui-design iteration.

The sweep resolves velocity and incidence samples into a Cartesian schedule. A replay selects one axis while holding the other fixed. Each step selects an immutable sample identity shared by metric values, plot selection, table selection and field view. Display speed is a presentation rate. Camera, seeds, slice and scalar range remain fixed by default. On a failed or missing case, playback pauses and the affected evidence clears with a reason and recovery actions.

This follows CFD-Bench's one-sweep-for-all-views intent and [ParaView's distinction between generated sequences and recorded timesteps](https://docs.paraview.org/en/latest/UsersGuide/animation.html). Physical-time playback belongs to actual transient data, within a single run. A streamline is a snapshot integration; pathlines and resolved turbulent motion require appropriate temporal fields. Modeled turbulent kinetic energy and a signed wall-shear separation diagnostic remain separately named evidence.

Rejected alternatives: interpolated morphing between case fields can fabricate an unsolved operating point; a generic time label conflates two different domains; retaining the previous successful field under a failed case label corrupts provenance. Separate timelines and explicit missing states make those ambiguities visible.

The mockup demonstrates these interactions with labeled synthetic data, including an illustrative signed wall-shear state. That choice does not admit a solver result or validate a separation criterion. Future backend design must establish field availability, dimensions, reference direction, averaging/time basis and numerical validation before claiming physical evidence. Promote this decision to an ADR if a load-bearing result schema or backend interface adopts it.
