---
id: review-spec-v02-critique
title: Critique of specification revision 0.2 against the knowledge base
type: proof-pack
status: accepted
owner: "@timianmalloo"
tags: [review, specification, critique, knowledge, personas]
links:
  - {to: spec-cfd-workbench, rel: documents}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
  - {to: domain-experts, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2026-12-19
summary: >-
  Fourteen lenses (the seven new domain experts and seven pack lenses) attacked specification revision 0.2 in
  Adversary Mode against the hydrofoil knowledge base. Every veto-holding lens returned BLOCK: 22 Blockers and
  110 Majors, resolved into a consolidated list of what v1 must add and what it must tighten. Revision 1.0 is
  written against this list; each finding names the v1 section that resolves it.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Critique of specification revision 0.2

Date: 2026-09-20. Session `kb-experts-spec-20260920`. Six independent critique runs, each wearing two or three
lenses, read revision 0.2 in full, the knowledge-base index in full and the area files their lens cites, and
attacked the specification with the rubric of `.claude/knowledge/ui-design-craft.md` DX22 (location · severity ·
confidence · evidence · fix). The raw persona reports are archived verbatim beside this file in
`spec-v02-critique/` (kept out of the documentation graph as `.md.txt`). This artifact records the verdicts, the
consolidated findings, and the disposition each receives in revision 1.0.

## Verdicts

| Lens (mode: Adversary) | Verdict on 0.2 | Blockers | Majors | Veto clears when (abridged) |
|---|---|---|---|---|
| Hydrofoil Hydrodynamicist | **BLOCK** | 1 | 10 | depth basis on every computed number; screens labelled as screens; A6 grant mechanics; sign fixture |
| Design Optimization Expert | PASS-WITH-CONDITIONS | 0 | 8 | goal state, control ids with bounds/frozen, candidate status enum, discrepancy record, structural proxy in v1 |
| Computational Geometry Expert | **BLOCK** | 2 | 11 | record payload complete; loft rule recorded; residual on every conversion; continuity measured |
| Marine CAD UX Expert | **BLOCK** (soft) | 0 | 12 | mockup semantics match the record; comb on-curve; three-step nudge; per-OS table; status strip |
| CFD & Numerical Verification Expert | **BLOCK** | 5 | 9 | "validated" reserved for ITTC use; three-part converged label; label gates named; tolerance table |
| Structures & Materials Expert | **BLOCK** | 2 | 6 | Not assessed state and fixed safety copy; fibre-angle sign convention; relative-stiffness readout |
| Manufacturing & CAM Expert | PASS-WITH-CONDITIONS | 0 | 5 | STEP shell contract with ≥ 2 named CAM systems; mesh units split; TE floor labelled |
| Test Architect | **BLOCK** | 2 | 11 | per-label proof named; CLI-01 tolerance table; identity oracle paragraph; red-first mandated |
| Data & Persistence Architect | **BLOCK** | 2 | 6 | evaluator/method version in the invariants; grain declared per fact; concepts homed |
| UX Researcher / IA | **BLOCK** | 5 | 8 | depth in F3; Results IA; six homeless concepts homed; DAT layout node; lock-release node |
| UX & Accessibility | **BLOCK** | 2 | 7 | hard states in the table; colormap policy; keyboard orbit; named native proof |
| Product Strategist | PASS-WITH-CONDITIONS | 0 | 8 | core scenario opens with the goal state offline; comparables sourced; persona decided |
| The Simplifier | **BLOCK** (soft) | 0 | 4 | S5/S6 to an appendix; ANA-13, GEO-11, AI-05 deleted or defended; duplicates merged |
| AI Systems Engineer | PASS-WITH-CONDITIONS | 0 | 3 | ten-item harness; prompt/schema versioning; read-only tool surface |
| Security & Identity Architect | **BLOCK** | 1 | 3 | boundary table with dispositions and negative tests; licence register; secret store named |

Totals: 22 Blockers, 110 Majors, 47 Minors, 6 Nits across 14 lenses. No author cleared its own veto; the
specification author (this session's main agent) did not sit on any panel.

## The consolidated finding — what revision 0.2 got wrong in one paragraph

Revision 0.2 specified the *shape* of the product correctly (one parametric definition, two curve modes,
provenance-pinned results, honest labels) but specified it as prose where the knowledge base demands data and
mechanics: no depth or Froude number reaches any computed quantity; the geometry record cannot reproduce its own
surface (no knots, weights, constraint sources, evaluator version or loft rule); every correctness label
("converged", "verified", "validated", "within 1 µm", "published tolerance") names no proof; the goal state the
user gave has no home; six load-bearing concepts (goal state, class rules, DRC findings, discrepancy records, run
manifests, design-point metadata) are absent from the domain model and the information architecture; the catalog
asserts bundled Eppler sections whose redistribution right does not exist; the hard states the domain produces
(Not assessed, Froude not modelled, Pending admission with reason, surface state, converged-not-quantified) are not
in the state table; and the current mockup's "Smooth" mode is a rational Bézier over the station values, the exact
anchor-as-influence anti-pattern the spec forbids.

## Consolidated findings and their disposition in revision 1.0

Findings are grouped by the surface they change. Each row names the lens, the severity, and the v1 section or
story that resolves it. "Reserved" means moved to the S5/S6 appendix of v1 (not v1 acceptance).

### Domain model (A3)

| Finding | Lens · severity | v1 disposition |
|---|---|---|
| Surface revision cannot reproduce the surface: no degree, knots, explicit weights, attachment parameters, constraint sources, fairness λ, blend/loft rule ids, evaluator id+version, tolerance triple, frame | Geometry Blocker; Data Blocker | A3 Surface revision owns the record payload; A4.1 lists every field; KB-1 |
| Loft rule left to the exporter | Geometry Blocker | A4.4 records rule A (channel-evaluated); skins are derived exports with a reported deviation |
| Analysis run invariant unenforceable without method version and content hashes; freshness by prose | Data Blocker; Test Major | A3 Run manifest; freshness = run key equality (A5.7; ANA-07 rewritten with three discriminating Givens) |
| Grain inferred, not declared, for sweep attempts, polar samples, field evidence, discrepancy records | Data Blocker | A3.3 grain statements per fact; "selected attempt" derived, never stored |
| Operating point carries depth but derives nothing from it; no Fr_h, h/c, σ, p_atm, h(y), target-load source | Hydro Blocker; Data Major; UX-IA Blocker; Product Major | A3 Operating point and Water record; A5.1; ANA-19 |
| Goal state, class-rule preset, DRC finding, Discrepancy record, Run manifest, design-point metadata, Candidate (reserved) absent | Data Major; UX-IA Blocker; Product Major; Optimization Major | A3 adds each with owner and invariant; GOAL-01–03, CAT-04, DRC-01 |
| Two homes: Profile revision holds coordinates *and* an editable curve with no authority; "input snapshot" vs "reference by identity"; Sweep stores ranges and resolved values | Data Major | A3: B-spline pair is the record, coordinates are provenance with residual; runs reference by identity + hash; the resolved case list is pinned, the range is recipe provenance |
| History asserted, not modelled; Backend installation smoke test is Type-1 | Data Major | A3.4 Design revision is an append-only node with parent and content hash; smoke-test evidence is a fact referenced by id |
| Apply modifies four aggregates in one transaction | Data Major (Inferred) | A3.4: Apply appends exactly one Design revision referencing new immutable Surface/Profile revisions; freshness is derived |
| Reserved structural vocabulary absent; fibre-angle sign convention absent | Structures Blocker; Data Minor | A3.5 reserved terms; A4.7 sign table incl. fibre angle; KB-18 |
| "revision", "sample", "sweep", "station", "depth", "supported", "admitted" each carry two meanings | Data, UX-IA, Product Minor/Major | A3.1 ubiquitous language fixes one word per meaning: Design revision · Example · Case schedule · LE offset channel · CFD cut · h · supported platform / in envelope / method-capable · admission class |

### Geometry contract (A4)

| Finding | Lens · severity | v1 disposition |
|---|---|---|
| "G2 within a smoothed region to a declared evaluator tolerance" unmeasurable; degree ⇒ G4 assumed | Geometry Major; Test Minor | A4.3 continuity measured one-sided per knot and junction against the tolerance triple; deliberate breaks stored as intent |
| One "1 µm" doing three jobs; GEO-06 accepts 1 µm for an exact operation | Geometry Major; Test Major; CFD Blocker | A4.5 three named tolerances; identity oracle paragraph cited by every criterion; GEO-06 ≤ round-off |
| Smooth formulation unstated while GEO-13's behaviour is formulation-specific; mockup uses rational NURBS weights | Geometry Major; CAD-UX Major | A4.2 constrained weighted least squares with KKT rows; rational weights forbidden for shape; GEO-13 property test |
| Constraint rows have no type catalogue or source; root-mirror tangent absent; GEO-11 G0 only | Geometry Major; Simplifier Major | A4.2 constraint catalogue with sources; root-mirror tangent default lock; GEO-11 replaced by the root-continuity criterion |
| Thickness "about the camber line" ambiguous | Geometry Major | A4.4: t/c is the ψ-aligned half-difference at equal ψ (max); the channel scales the unit-thickness shape exactly; cambered NACA 4/5-digit are conversions because their *definition* is normal-to-camber (corrected at the v1 gate — the first v1 text wrote the rejected rule) |
| Conversion residual contracted only for Edit section; 6-series regenerated from a formula | Geometry Major | A4.6 residual on every conversion path; NACA 6/6A are generated coordinates with generator hash |
| Blend has no shared knot vector or fixed-u correspondence; no different-control-count fixture | Geometry Major | A4.4 shared knot vector, fixed u_TE/u_LE, two mandatory fixtures |
| Nudge has two steps; no expressions; no click-handle-to-type; no Tracing probe; comb is one phrase; no Fair action; no DOF list; loft vocabulary and closure names not established; no per-OS table or presets | CAD-UX Major ×9; UX-IA Major | A4.8 precision input contract (three-step table, weight nudge, expression rule); A4.9 diagnostics contract; GEO-15 Fair; DRC lock release with DOF; A4.4 three-word loft vocabulary and OpenVSP closure names; B4 per-OS table and presets |
| Catalog asserts bundled Eppler with no rights; two admission classes | Product Major; Security Major; Hydro Major | A4.10 three admission classes with reason; generated NACA default; Eppler/H105 pending with reason |
| DAT import cannot catch Lednicer-as-Selig | UX-IA Blocker; Test Major | A4.10 two-layout detector with fixtures; CAT-02 differential criterion; F2 layout-confirm node |
| STEP "written" ≠ machinable; mesh units ambiguous; sharp TE exportable as drawn | Manufacturing Major ×3; Geometry Major | A4.11 export matrix; EXP-02 shell-in-mm with ≥ 2 named CAM readers; print vs CFD mesh units; TE floor finding repeated in every geometric export |

### Analysis contract (A5) and labels (A7)

| Finding | Lens · severity | v1 disposition |
|---|---|---|
| Free-surface rule inverted (deferred to CFD); ANA-05/12 "within 1 %" unfalsifiable without the depth basis | Hydro Major; Test Major | A5.2 depth-only correction layer, labelled, beside not instead; ANA-05 states the basis |
| Ncrit constant 2.0 | Hydro, Product, Test, Structures, UX-A11y Major | A5.3 Ncrit pair {2, 4} as a band with the fixed label |
| Polars silently clean-surface | Hydro, Manufacturing, Structures Major | A5.3 surface state on every polar sample; clean/tripped band |
| Cavitation and ventilation screens lack fixed copy, margin, resolution, "not screened" list, Fr_h | Hydro Major ×2 | A5.4 fixed strings; margin setting; governing station; forbidden-string lint |
| Seawater rule has no defect-signature control | Hydro Major | ANA-15 fixture: Re −4.25 % at 15 °C; a load change of exactly 1.0269 with unchanged coefficients fails |
| No sanity bounds; presets four and dated; no class rules; no wingfoil-race preset | Hydro, Product Major | A5.5 advisory envelope DRC with data fixture; CAT-03 eight presets with labels; CAT-04 GWA validator |
| CAT-01 ranking is a single-point objective | Optimization Major | CAT-01 requires an operating-point set, both Ncrit, surface state, tier chip, per-point rank |
| COMMIT-01 is prose; no design-vector contract; no evaluation provenance; no discrepancy record; no structural proxy | Optimization Major ×5 | A2 COMMIT-01 as the Candidate status enum (reserved); A4.1 stable control ids with bounds/frozen; A3 Run manifest fields; A3 Discrepancy record; GEO-12 relative-stiffness readout |
| A6 labels have no grant mechanics; "validated" used for a verification gate; "Complete" conflated with "Converged" | CFD Blocker ×3; Test Blocker; Hydro Major | A7 label table (label · granted per · evidence · fixed string); "validated" reserved for ITTC use; three-part Converged label (reserved with S5) |
| CLI-01 "published tolerance" absent; bitwise implied for transcendental arithmetic | CFD Blocker; Test Blocker | A8.4 tolerance table: bitwise for + − × ÷ √ FMA; relative 1e-12 otherwise; platform provenance |
| Testing floors absent; only exact-integrable geometry oracles; no VLM oracle; red-first not required | Test Major ×4; CFD Blocker | A8.4 testing floors (KB-23); GEO-02b manufactured elliptic planform; ANA-04 fixture rows; A6 preamble mandates observed-red |
| Loads with no Not-assessed state, no safety copy, no load-case vocabulary, no root-moment datum | Structures Blocker ×2, Major ×3 | ANA-11 rewritten; A5.6 fixed safety strings; A3.5 reserved vocabulary; sign table |
| AI harness five of ten; no prompt/schema versioning; tool surface unspecified; egress under-specified; secret store unnamed | AI Major ×3; Security Major ×2, Minor | A8.6 AI allocation with the ten evals, versioning, read-only tools, egress rules; AI-06 rewritten |
| Threat model is one paragraph; boundaries unenumerated; licence register absent | Security Blocker, Major | A8.5 boundary table with dispositions and negative tests; A8.5 licence register |

### UX layer (Part B)

| Finding | Lens · severity | v1 disposition |
|---|---|---|
| F3 has no depth node; F2 has no lock-release, layout-confirm or preset-label nodes; F4 fuses setup and results; assistant is an orphan; out-of-envelope is a dead end contradicting A6 | UX-IA Blocker ×4, Major ×2 | B3–B6 redrawn; Results flow reserved with S6; assistant entry points per destination |
| Six concepts homeless in the IA | UX-IA Blocker | B1 IA table concept → destination → panel → entry action; Brief destination; Checks drawer |
| First launch is a choice screen; no-sample path undrawn | UX-IA Major | B2 first launch opens the goal-state Example with a selected station; missing-fixture path |
| Need unevidenced; persona undecided; no falsifier | UX-IA, Product Major | A1 target persona (Inferred) with non-targets and a falsifier; UX-05 protocol |
| One-canvas/one-inspector only; status strip thin; findability fails for polar overlay and class rule | UX-IA Major ×3 | B7 lines-plan layout mode; status strip contract; UX-16 step budgets |
| UX-03/06 unfalsifiable | UX-IA Major | B8 error set enumerated by flow node; absence states named |

### UI layer (Part C)

| Finding | Lens · severity | v1 disposition |
|---|---|---|
| State table omits the hard states | UX-A11y Blocker | C2 state table extended with each state and its string |
| Colormap policy cividis-only; signed fields; no isolines/table twin; mockup encodes signed C_f sequentially | UX-A11y Blocker | C2 colormap policy (KB-20); UI-07 rewritten; DESIGN.md tokens |
| G2 signature unwritten; Nav facet wrong | UX-A11y Major | C1 signatures; Nav = TaskTabs+CommandPalette |
| Single-line polar as point estimate | UX-A11y Major | UI-15 band by default |
| No screen-reader summary or provenance schema; harness lacks additions; copy record lacks fixed strings; keyboard orbit absent; chart set unnamed; target size contradiction | UX-A11y Major ×5, Minor | UI-14, UI-16, UI-17, C3 copy table, UI-03 keyboard orbit, C2 chart set, target token |

### Scope (Simplifier delete-list and its disposition)

| Item | Simplifier | Disposition in v1 |
|---|---|---|
| S5/S6 stories, F4, states and criteria at full acceptance depth | move to appendix | **Applied** — Appendix R "Reserved contracts"; the v1 body keeps only the run manifest, evidence labels, field-by-reference and the three-part Converged label as reserved vocabulary |
| ANA-13 sideslip | delete | **Applied**; tip-depth clause moves to ANA-19 |
| GEO-11 Break symmetry | delete; add the root-mirror constraint | **Applied** |
| AI-05 Diagnose failure | delete from v1 | **Applied** — reserved with S5 |
| VIZ-04 transient playback | shrink to one prohibition | **Applied** in Appendix R |
| GEO-12 second-file ghost | shrink to same-document prior revision | **Applied** |
| ANA-05+12, ANA-03+17, ANA-10+14 | merge | **Applied** (ANA-05, ANA-03, ANA-10) |
| Fixed-coefficient what-if; salinity override; lbf/ft line loads; strip-width N | delete | **Applied** (salinity fixed at 35.16504 g/kg standard seawater; N/m section loads; N and lbf only) |
| Command palette | yagni | **Overridden by written rationale** — the 2025 novice study ranks icon/text comprehension first and the palette is the primary discoverability control in area 01 (Marine CAD UX and UX-IA both require it); native menus remain the parity path |
| A2 six-stage table | shrink to names + gates | **Applied** |
| Net | −14 stories, ≈ −120 lines | v1 keeps 42 of 0.2's 58 functional stories, deletes/merges/reserves 16 and adds 12 (GOAL-01–03, GEO-11 root continuity, GEO-15, CAT-04, ANA-19, ANA-20, DRC-01, LAB-01, EXP-03, CLI-02): 54 functional stories plus 16 UX and 17 UI criteria, 10 stories reserved |

## What the critique did not cover (residual)

The critique read the mockup only by grep and the prior reviews only for overlap; the VLM fixture numbers for
Warren-12 and Bertin–Smith stay Flagged in the knowledge base and cannot become criteria; the exploitation threshold
and cavitation margin defaults are placeholders; the tank Re confound behind the 17 % Froude loss stands; no CAM
system or MCP was exercised; the persona remains n = 1 until the debrief protocol runs.

`GATE critique-0.2 · 2026-09-20 · 14 lenses in six independent runs · verdict on 0.2: BLOCK · every finding
carries location, severity, confidence, evidence and fix · disposition recorded above; revision 1.0 is the
resolution artifact and is gated separately.`
