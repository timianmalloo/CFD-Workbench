---
id: decision-seven-areas
title: Seven discrete areas, each with a typed AI proposal kind
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [ia, ai, experiment, run, results]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: mockup-workbench-v2, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-20
summary: The product is seven discrete, complementary areas — Setup, CAD, Analysis, Experiment setup, Run, Results, Export — each owning a typed input and output object and a verb set, with one AI prompt entry per area whose output is a validated, previewed proposal the user accepts.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table." }
  - { by: mockup-workbench-v2, on: 2026-09-20, reason: "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact." }
---

# Seven discrete areas, each with a typed AI proposal kind

**Decision.** Specification revision 1.1 organises the product as seven areas in flow order (A2). An area is
*discrete* when its input and output objects are typed and named and no two areas edit the same object; areas
are *complementary* where one reads what another wrote. The B1 verb × area table is the oracle: every verb
appears in exactly one area or in the global column, and a hand-off is a jump chip. CAD and Analysis share one
canvas; the toggle between them *is* navigation. Every area has one prompt entry with a fixed name and one
proposal kind — setup-seed, geometry-edit, experiment-config, environment-step, case-diff, explanation — and
COMMIT-04 enumerates exactly those six.

**Why.** The operator asked for the goals to be explicit and not conflated, for an AI entry in every place, and
for a rich representation of the target build state including simulation and results. Revision 1.0 had folded
setup into a brief, CAD into two destinations, and held Run and Results in reserve; a user could not tell where a
job began or ended, and an AI capability with no typed output would have leaked geometry, queueing or a shell
into a model's hands. Typing the proposal per area is what lets "the AI modifies the shape" be true without
violating "the model never emits coordinates": the geometry-edit kind is the same edit draft a pointer edit
creates.

**Rejected.** A wizard that locks areas in sequence (the areas are complementary, not gates); a chat-first
shell (the prompt entry is subordinate to the area); a free-form environment assistant with a shell (the
environment-step kind carries only a step id and the tool binds every parameter); optimization as a non-goal
(it is an experiment kind, gated by tier and by the Candidate ladder).

**What it constrains.** The data model (Setup brief, Experiment with its backend pin and optimizer record,
Backend environment and checks, Evaluation and Candidate with promotion evidence — all first-class); the flows
F6–F8 with 80 enumerated non-happy edges; the C2 state table (sixteen new rows); the mockup v2's area strip and
prompt entries; the acceptance gate for Run and Results, which stays on SPIKE-03/03b/04.

**Confidence.** Verified against the knowledge base for the backend, optimization and visualization contracts
(08, 09, 11) and gated by two independent panels (three Blockers fixed in place). The area boundaries are an
Inferred product interpretation of the operator's words; the UX-05 formative study is the falsifier. Owner lenses:
UX Researcher/IA · Product Strategist · AI Systems Engineer.
