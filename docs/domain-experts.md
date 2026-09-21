---
id: domain-experts
title: CFD-Workbench domain-expert roster
type: doc
status: in-review
owner: "@timianmalloo"
phase: knowledge
tags: [personas, domain-experts, hydrofoil, roster]
links:
  - { to: kb-hydrofoil-workbench, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
  - { to: plan-knowledge-experts-spec-v1, rel: relates-to }
review-by: 2027-03-19
summary: >-
  Seven subject-matter lenses derived from the repository's own evidence and the hydrofoil knowledge
  base: hydrodynamicist, CFD and numerical verification, computational geometry, marine CAD interaction,
  structures and materials, manufacturing and CAM, and design optimization. Each is a §8-conformant
  dual-mode card with a proportional veto; nine candidate roles were merged or rejected with reasons.
---

# CFD-Workbench domain-expert roster

*The project-local, domain-scoped analogue of the persona audit. The twenty-three general lenses in
`.claude/knowledge/persona-cards.md` do not know whether a lift coefficient is valid at h/c = 3, whether a
degree-5 spline is G4 at a repeated knot, or whether a 0.08 mm trailing edge can be moulded. These seven do.*

Run: `/adddomainexperts`, 2026-09-20, session `kb-experts-spec-20260920`. **Roster confirmation:** the driving
prompt pre-delegated the choice ("the above are just candidates … you choose the right domain experts to add");
that delegation is recorded here as the developer's confirmation of the derivation method, and the roster below
stays open to trimming at the user's next review.

## The domain, from the repo's own evidence

The README, `AGENTS.md`, the specification (`docs/specs/cfd-workbench.md`) and the knowledge base
(`docs/knowledge/hydrofoil-workbench/`) establish one product: a Mac/Windows desktop workbench for designing,
analysing, simulating and later optimising water-sports hydrofoil wings, whose geometry of record is one explicit
parametric surface and whose analysis runs on a ladder of tiers with a validation ladder of labels. There is no
production code yet; the ubiquitous language is in the spec's A3 and the knowledge base's glossary (200 terms).

## The domain failure map (where an error is expensive, silent or irreversible)

| Failure | Cost | Silent? | Irreversible? | Evidence |
|---|---|---|---|---|
| A physically wrong hydrodynamic number shown as current (deep-water polar near the surface, density multiplier for salt, section force as wing lift, "cavitation-free") | A rider hurt at speed; a foil that never lifts | Yes — looks like a right answer | Yes once ridden | KB 04, 07, 13 |
| A computation labelled converged/verified/validated without evidence (single mesh, y+ off-window, e > 1 from a coarse lattice, bitwise golden masters) | Every downstream decision | Yes | Expensive | KB 08, 13 |
| A second geometry authority or an unreported refit residual | The record is corrupted; COMMIT-03 broken for every file ever saved | Yes — below visual threshold | Yes | KB 02, defect classes GEO-A/GEO-B |
| An editing affordance that teaches a false CAD model (comb at controls, influence drawn as anchor) | Users fair the wrong thing; expert rejection | Yes | No | KB 01, 03 |
| A strength or safety claim the tool cannot support; missing "Not assessed" | Injury; no standard to hide behind | Yes | Yes | KB 12 |
| An export that is "written" but not machinable; unbuildable-as-drawn TE | A wasted mould; a failed launch | Yes | Expensive | KB 05, 12 |
| An "optimized" candidate that exploited the surrogate or a single point | A worse foil sold as better | Yes | Expensive | KB 09 |

## The roster

| Expert | Lens (one line) | Mode | Veto | Convene-when (headline) | Backing capability | Card |
|---|---|---|---|---|---|---|
| **Hydrofoil Hydrodynamicist** (with design-practice branch) | Physically correct and honestly labelled hydrodynamics, water, free surface, cavitation, ventilation, envelopes, presets, class rules | Peer + Adversary | **Hard (narrow)** on unsupported physical-validity or safety-relevant claims | any hydrodynamic quantity, operating point, preset, sanity bound, flow-evidence overlay, A6 label, sign convention | `Bash` arithmetic against ITTC tables | `hydrofoil-hydrodynamicist` |
| **CFD & Numerical Verification Expert** | Numerically trustworthy computation: mesh, convergence, GCI, V&V, fixtures, cross-platform equivalence, interop correctness | Peer + Adversary | **Hard (narrow)** on converged/verified/validated labels without evidence | any analysis tier, backend, mesh, golden master, equivalence claim, optimizer-in-the-loop | `engineering:testing-strategy`; FreeCAD MCP as oracle | `cfd-numerical-verification-expert` |
| **Computational Geometry Expert** | Mathematically sound, lossless geometry of record: B-spline record, constrained solve, measured continuity, tolerances, conversions, loft, interchange | Peer + Adversary | **Hard (narrow)** on a second authority, unreported residual, continuity from degree | any change to representation, evaluator, constraints, tolerances, serialization, import/export | geomdl/scipy probes; FreeCAD and Fusion MCPs as STEP oracles | `computational-geometry-expert` |
| **Marine CAD UX Expert** | Precision-CAD and lines-plan interaction correctness: control semantics, combs, nudge, Tracing, loft vocabulary, per-OS conventions | Peer + Adversary | **Soft** on a violated established convention | any curve/station editor, viewport, loft option, navigation, CAD readout | Fusion MCP to observe a comparable | `marine-cad-ux-expert` |
| **Structures & Materials Expert** (structural + materials branches) | Loads to strength: beam/hydroelastic fidelity, bend–twist sign, layup and material provenance, safety copy, regulatory register | Peer + Adversary | **Hard (narrow)** on any strength/safety claim without evidence or missing "Not assessed" | t/c, loads, layup, mast, beam tier, export for manufacture, safety copy | `Bash` section-inertia checks; DCFoil.jl and Fusion MCP as oracles | `structures-materials-expert` |
| **Manufacturing & CAM Expert** | Buildable by the declared route; machinable export (shell, parting curve, datums, units); finish → roughness | Peer + Adversary | **Advisory**, escalates to Structures or Tech Lead | manufacturing policy, TE/LE floors, CAM/print export, mould workflow, surface state | Fusion 360 MCP CAM tools; FreeCAD MCP | `manufacturing-cam-expert` |
| **Design Optimization Expert** | Well-posed, honestly solved and reported optimization: multipoint, typed constraints, exploitation gate, COMMIT-01 ladder as data | Peer + Adversary | **Hard (narrow)** on certified-without-re-verification or single-point objective | goal state, objective, constraints, design vector, surrogate-in-loop, any "optimized" label | scipy/pymoo spikes; `data:statistical-analysis` | `design-optimization-expert` |

Every card: `.claude/agents/<name>.md` (Claude Code, with `tools:`), mirrored to `.github/agents/<name>.agent.md`
(Copilot) and `.grok/agents/<name>.md` (Grok) without the `tools:` key, as the existing lenses are.

## Seams (so no two lenses are confused)

- **Every expert vs the Domain Researcher:** the Researcher establishes the *contract* of an SDK, format or paper by
  reading and running it (research method); the expert judges whether the *content* is correct per the domain's body
  of knowledge (subject matter). The Researcher establishes what NeuralFoil returns; the Hydrodynamicist judges
  whether it may be called lift at this depth.
- **Hydrodynamicist vs CFD & Numerical Verification:** physical validity of the model vs numerical validity of its
  discretisation and proof. A solver can converge to physically wrong flow; a right model can be discretised wrongly.
- **CFD & Numerical Verification vs Test Architect:** numerical validity (fixtures, GCI, tolerances) vs software-test
  verifiability (red-first, Proof Pack). They pair on every numerical fixture; neither clears the other.
- **Computational Geometry vs Data & Persistence Architect:** the mathematical content of the record vs its durable
  schema, grain, history and migration.
- **Computational Geometry vs Marine CAD UX:** what the mathematics is vs how the interaction presents it.
- **Marine CAD UX vs UX Researcher/IA vs UX & Accessibility:** CAD-domain interaction correctness vs general IA/flows
  vs visual surface, tokens, states and WCAG (the accessibility veto stays with UX & Accessibility).
- **Structures & Materials vs Hydrodynamicist:** what the structure does with the loads vs the loads themselves.
- **Manufacturing & CAM vs Computational Geometry:** machinability of the exported shell vs its mathematical exactness.
- **Design Optimization vs AI Systems Engineer:** posedness and the certification ladder vs the eval harness and
  non-determinism of any learned surrogate in the loop.

## Candidates considered and rejected or merged (the Simplifier's test)

| Candidate (from the prompt or the KB) | Disposition | Reason |
|---|---|---|
| CFD expert; Numerical Methods expert | **Merged** into CFD & Numerical Verification Expert | Both judge whether a computed number is trustworthy; the interrogation (verification → validation → uncertainty) is one structure across the estimator, polar, VLM and RANS tiers. Two seats would duplicate it. |
| Applied Mathematician | **Merged** into Computational Geometry (spline/KKT/tolerance mathematics) and CFD & Numerical Verification (numerics) | No failure class it catches alone; every mathematical failure in this product is either geometric or numerical. |
| CAD expert; 3D modeling expert | **Split** into Computational Geometry (representation) and Marine CAD UX (interaction) | "CAD expert" conflates two bodies of knowledge with different owners and different failure modes (silent refit vs false interaction model). |
| UI/EX workbench expert | **Merged** into Marine CAD UX Expert; general workbench UX stays with UX Researcher/IA and UX & Accessibility | The pack already owns general workbench UX; what it lacks is the CAD/marine domain layer. |
| Materials engineer | **Branch** of Structures & Materials Expert (one lens, two branches, like the Mobile lens's iOS/Android) | The interrogation is shared (loads → layup → reserve factor); a materials seat alone would re-ask the structural questions. |
| CAD/CAM additive and subtractive manufacturing expert | **Kept** (Manufacturing & CAM), advisory and conditional-convene | The user explicitly wants it for mould design; nobody else catches "STEP written ≠ STEP machinable" or the TE floor as a design-space constraint. Advisory because the release gate (EXP-02) and the safety copy already carry vetoes. |
| Scientific Visualization expert (suggested by KB 11) | **Rejected as a seat**; rules distributed | Colormap and uncertainty floors (TQ3, TQ5) are already owned by UX & Accessibility; the physics-honesty rules (Q ≠ separation, steady animation ≠ transient, k labelling) are owned by the Hydrodynamicist; the rendering stack is an architecture decision. A seat would duplicate three owners. |
| Hydrofoil design practitioner / rider-domain expert (suggested by KB 04) | **Merged** into the Hydrodynamicist as its design-practice branch | Presets, product envelopes and class rules are the practice half of the same body of knowledge; the seat would otherwise be convened on every preset alongside the Hydrodynamicist. GAP-03 (who the user is) remains a research item for the UX Researcher, not a persona. |
| Design Optimization / MDO expert (not in the prompt; from KB 09) | **Added** | The exploitation pathology and COMMIT-01 are invisible to every other lens; the cost (a worse foil sold as better) is high and the seat is conditional-convene, so it costs nothing until the optimizer stage. |
| Regulatory / safety compliance expert | **Rejected** | KB 12 established that no product standard applies; the safety framing is owned by Structures & Materials through the safety copy and the regulatory register. |
| Marine hydrodynamics vs aero split | **Rejected** | One Hydrodynamicist; the water-specific physics (free surface, cavitation, ventilation, salinity) is the reason the seat exists. |

Net: nine named candidates → seven seats, one of them not in the original list.

## Anti-patterns the experts own (extends `persona-audit.md` §8.8)

| Anti-pattern | Owner |
|---|---|
| Physically-Plausible-Wrong (a converged or computed number outside its physics envelope shown as current) | Hydrofoil Hydrodynamicist |
| Converged-but-Unverified (a result labelled trustworthy with no verification path) | CFD & Numerical Verification Expert (+ Test Architect) |
| Silent-Refit (a representation change altering shape without a reported deviation); Second-Geometry-Authority (GEO-A/GEO-B) | Computational Geometry Expert (+ Data & Persistence) |
| False-CAD-Model (an affordance that teaches a model the geometry does not have) | Marine CAD UX Expert |
| Structure-Unassessed-but-Implied | Structures & Materials Expert |
| Exportable-in-Name-Only | Manufacturing & CAM Expert |
| Exploited-Model-Optimum | Design Optimization Expert (+ AI Systems Engineer for learned surrogates) |

## Casting (extends `collaborative-personas.md` §5)

| Workflow | Peers | Adversaries | Hard vetoes added |
|---|---|---|---|
| `/specify` | Hydrodynamicist, Computational Geometry, Marine CAD UX, Structures & Materials (safety copy, structural vocabulary), Design Optimization (goal-state object) | all seven by convene-when | Hydrodynamicist; Computational Geometry; Structures & Materials |
| `/define-architecture` | Computational Geometry (evaluator, payload), CFD & Numerical Verification (backend matrix, fixture rings), Design Optimization (plug-in contract) | Hydrodynamicist, CFD & Numerical Verification, Computational Geometry, Manufacturing & CAM (export path) | CFD & Numerical Verification; Computational Geometry |
| `/design-slice` | the expert(s) whose convene-when fires | same, in Adversary Mode | per card |
| `/implement` | CFD & Numerical Verification ⇄ Test Architect on every numerical fixture; Computational Geometry on kernel code | same | per card |
| `/ui-design` | Marine CAD UX (with UX & Accessibility and UX Researcher/IA), Hydrodynamicist (evidence labels, Results honesty) | Marine CAD UX (soft), Hydrodynamicist (labels) | Hydrodynamicist on labels; UX & Accessibility keeps the a11y veto |
| `/investigate` | the expert whose domain the defect is in | same | per card |

The **yield rule** (`persona-audit.md` §8.7a) applies: hard-veto experts always re-convene when their predicate is
true; the advisory Manufacturing & CAM lens re-convenes on the same work only when a prior finding was accepted.

## Gate record

`GATE adddomainexperts · 2026-09-20 · peers: Orchestrator, Product Strategist (stakes of a domain error from the
failure map), Domain Researcher (standards per card from the knowledge base; existing skills and MCP servers
discovered: engineering:testing-strategy, data:statistical-analysis, dataviz, freecad-headless MCP, fusion360 MCP
incl. CAM tools) · adversaries: The Simplifier (roster sprawl — nine candidates reduced to seven seats, three merges,
one split, two rejections, one addition, each with a reason above), Patterns Expert (no existing persona or skill
already supplies any seat; the three MCP servers are wired as out-of-process oracles, not components), Tech Lead
(maintenance: seven cards, all conditional-convene except the Hydrodynamicist and Computational Geometry on
geometry/analysis changes), Data & Persistence / Security / Privacy (seams stated above; no overlap on schema, trust
boundary or personal data — rider mass handed to Privacy) · developer confirmation: pre-delegated in the driving
prompt and recorded above · verdict: PASS-WITH-CONDITIONS · condition: the user trims or confirms the seven seats at
the next review; the anti-patterns above are proposed additions to §8.8, not yet in the pack source.`
