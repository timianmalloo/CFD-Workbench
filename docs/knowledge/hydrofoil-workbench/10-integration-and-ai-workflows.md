---
id: kb-hw-integration-and-ai-workflows
title: "Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, integration, workflow, multi-fidelity, provenance, surrogates, llm-agents, mcp, drc, reproducibility]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes how engineering tools compose estimator, polar, VLM, CFD and optimizer tiers (dependency graphs with
  content-hashed invalidation, job queues, PROV-style run records), how multi-fidelity results are reconciled without
  averaging (correction models, trust-region model management, verify-at-higher-tier), what the 2024–2026 evidence
  says ML surrogates and LLM agents can and cannot do for CFD/CAD, and which permissive libraries can build the
  composition layer in .NET/Rust. Main design implication: v1 should be a salsa/Snakemake-style incremental graph
  keyed by BLAKE3 content hashes with tier-specific recompute policy, an explicit run manifest per Analysis run, and a
  model-backed assistant confined to typed proposals validated by deterministic code and gated by a named eval set.
---

# Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. Which workflow-composition patterns (model-as-DAG, reactive recompute with staleness, job queues, content-hash
   caching, provenance, one-interface-four-fidelities) do engineering tools use, with named precedents?
2. How do tools orchestrate and reconcile multiple fidelities (estimator, VLM, CFD) and what do they show when tiers
   disagree?
3. What are ML surrogates for hydrofoil analysis (NeuralFoil, GNN/neural-operator CFD surrogates, generative airfoil
   models) actually good for, and where do they fail?
4. What has LLM/agent integration in CAD/CFD (2024–2026) established, what guardrails and evals are needed, and what
   failure modes are reported?
5. Where do closed-form and heuristic checks (TE floor, AR, Re, cavitation σ, class-rule boxes, symmetry) belong,
   modelled on EDA/CAD design-rule checks?
6. What do run manifests, immutable results and event-sourced history need, and how do ParaView, OpenVSP and XFLR5 do
   or fail to do it?
7. Which permissively licensed libraries in C#/.NET or Rust can build the composition layer (graphs, schedulers,
   hashing, process orchestration, MCP hosting, Anthropic SDKs, local retrieval)?

**Method note.** This session's web search budget was exhausted before this area started; every external claim below
was established by fetching a known URL directly (arXiv listings via the arXiv API, GitHub repositories, vendor
documentation, Semantic Scholar metadata). Items I could not open are labelled **Flagged** and carry the cheapest next
probe. Fast-moving facts (versions, licences, SDK status) carry their access date.

## Headline findings

1. The composition pattern that fits this product is a **pull-based incremental dependency graph** (inputs → memoized
   derived queries with dependency tracking, early cut-off and cancellation), as implemented by `salsa`
   (MIT/Apache-2.0) and as used by OpenMDAO's `Problem → Group → Component` model; results are keyed by **content**,
   not by time. Snakemake's default rerun triggers (`code, input, mtime, params, software-env`) are the precedent for
   invalidating on method version and parameters, not only on file timestamps. — *(Verified, [S11][S40][S48])*
2. **Provenance has a standard vocabulary**: W3C PROV-DM (Recommendation, 2013-04-30) — Entity / Activity / Agent with
   `used`, `wasGeneratedBy`, `wasDerivedFrom`, `wasAttributedTo`, `wasAssociatedWith`, `wasInformedBy`. The spec's
   Analysis run and Field evidence map cleanly onto it; a run record is an Activity that `used` a Surface revision
   entity and `wasGeneratedBy` for every result entity. — *(Verified, [S10])*
3. **OpenMDAO records every case to SQLite** (inputs, outputs, residuals, derivatives, solver iterations, model
   metadata) through `SqliteRecorder` and reads it back with `CaseReader`; this is the closest open precedent for
   "immutable results referencing exact inputs" in an engineering optimizer. Licence Apache-2.0. — *(Verified,
   [S12][S13])*
4. **Multi-fidelity literature never averages tiers.** The three strategies (Peherstorfer–Willcox–Gunzburger 2018:
   adaptation, fusion, filtering) all keep the high-fidelity model as the accuracy authority; Alexandrov–Lewis
   first-order model management (NASA, 2000) requires the low-fidelity model to be **corrected to match high-fidelity
   value and gradient** at the current point before it may steer a step; Kennedy–O'Hagan (2000) / Forrester et al.
   (2007) co-Kriging models high fidelity as `ρ·f_lo + δ(x)` with a learned discrepancy and its uncertainty. The
   metadata of all four sources was verified; the model forms are from recall and labelled accordingly. — *(Verified
   existence [S15][S16][S17][S18]; model forms Inferred)*
5. **NeuralFoil** (Sharpe & Hansman, arXiv 2503.16323, March 2025; code MIT, package graph pulls CasADi LGPL-3.0 — see area file 09) is an XFoil surrogate trained on
   7,913,292 converged XFoil points at M=0 sampled from 2,174 parent airfoils, Re log-normal (median 296k, 2.5–97.5 %
   ≈ 1.87k–262M), **N_crit uniform in [0, 18]**, natural transition 80 % of cases, α observed −27.9° to +28.6°;
   reported xxxlarge test MAE: C_L 0.012, ln(C_D) 0.020, C_M 0.002. Its `analysis_confidence` is a
   converged/not-converged classifier logit minus a squared Mahalanobis distance so confidence → 0 far from the
   training distribution. — *(Verified, [S1][S2])*
6. **Neural CFD surrogates are fast previews, not evidence.** MeshGraphNets (ICLR 2021) reports 1–2 orders of
   magnitude speed-up over its training solver; AirfRANS (NeurIPS 2022, CC BY-NC-SA 4.0) and the NeurIPS 2024 ML4CFD
   competition explicitly score **out-of-distribution** generalization and physical compliance because that is where
   surrogates fail; 2025–2026 papers still report OOD Reynolds extrapolation as the dominant failure and propose
   solver-coupled correction (Newton–Krylov on the surrogate output) as the fix. — *(Verified, [S34][S35][S36])*
7. **LLM agents for OpenFOAM (13 arXiv papers, Aug 2024 – Sep 2026)** reach 84–96 % *execution* success on
   tutorial-class benchmarks but only 62.5 % on out-of-distribution tasks (Foam-Agent) and **68.1 % physical fidelity
   vs 82.1 % execution success** (ChatCFD): a case that runs is not a case that is right. The single largest lever is
   **execution feedback and repair loops** (71.8 % → 96.4 %), the second is retrieved solver tutorials; multi-agent
   architecture added nothing. — *(Verified, [S26][S27][S28][S29])*
8. **Text-to-CAD is a benchmark field, not a production capability**: 2024–2026 papers (Text2CAD, RA-CAD, CIT-CAD,
   ArtisanCAD, Ortho2CAD) measure Chamfer distance, IoU and execution validity on DeepCAD-derived datasets; one 2026
   study found that a **wrong design-intent header degrades output below the unconditioned baseline** while
   executability stays high — the model produces valid, wrong geometry. This is the empirical basis for "language in,
   numbers out" (COMMIT-04). — *(Verified, [S32][S33])*
9. **Structured outputs are now a first-class API contract**: Anthropic's `output_config.format` (JSON schema,
   constrained decoding) and `strict: true` tool use guarantee schema-valid output on current models, but the schema
   subset excludes `minimum`/`maximum`, string length and most array constraints — so **range and domain validation
   must remain in deterministic code**. — *(Verified 2026-09-20, [S7])*
10. **Official SDKs exist for the assistant surface**: the `Anthropic` NuGet package is Anthropic-owned, MIT, v12.49.0
    released 2026-09-18, targets netstandard2.0/net8/net9; the MCP C# SDK (Apache-2.0, with Microsoft) and Rust `rmcp`
    (Apache-2.0) target the MCP 2026-07-28 spec. No official Anthropic Rust SDK repository exists at
    `anthropics/anthropic-sdk-rust` (404). — *(Verified 2026-09-20, [S3][S4][S5][S6])*
11. The MCP specification requires a **human in the loop with the power to deny tool invocations**, treats tool
    annotations as untrusted, and separates protocol errors from tool-execution errors (`isError`); tool
    `outputSchema` is server-validated structured content — the right shape for exposing read-only workbench data to
    an assistant. — *(Verified, [S8][S9])*
12. **Design-rule-check (DRC) is the right model for "simple algorithms"**: KiCad's DRC has a per-rule severity (error
    / warning / ignore), per-violation exclusions, a custom-rule language of conditions + constraints, and a violation
    list that navigates to the location. The workbench's TE-thickness floor, AR convention, Re, σ, symmetry and
    class-box checks belong in exactly this shape. — *(Verified, [S46])*
13. **Existing tools fail provenance in predictable ways**: ParaView state files embed absolute data paths and need
    "search under directory" on reload; VSPAERO writes `.history/.lod/.adb` files beside the model with no automated
    results-manager linkage in the tutorial; OpenVSP is NOSA-1.3 (OSI-approved, but not MIT/BSD/Apache and
    GPL-incompatible), so it is process-invoke only under COMMIT-02. — *(Verified, [S23][S24][S25][S47])*
14. **Permissive building blocks are available**: BLAKE3 (CC0-1.0 / Apache-2.0; `Blake3.NET` BSD-2-Clause with managed
    and native packages), CliWrap (MIT; graceful/forceful cancellation, Rx streams), R3 (MIT), `salsa`
    (MIT/Apache-2.0), `sqlite-vec` (MIT/Apache-2.0 but **pre-v1, breaking changes expected**, no .NET binding listed).
    — *(Verified 2026-09-20, [S40][S41][S42][S43][S44][S45])*

## State of the art

### 1. Workflow composition patterns in engineering tools

**Model-as-graph.** OpenMDAO structures a model as a `Problem` owning a hierarchy of `Group`s containing
`ExplicitComponent`/`ImplicitComponent` nodes, connected by named variables; the connections define a data-flow graph,
and derivatives (analytic partials, finite difference, complex step, unified total derivatives) propagate along it;
`Driver`s (optimizers, DOE) sit outside the model and call it; solvers (Newton, `BalanceComp`) resolve implicit
cycles. *(Verified, [S11][S12])* The Martins & Lambe 2013 survey (AIAA J., 1,033 citations) classifies MDO
architectures as monolithic (MDF, IDF, SAND, AAO) versus distributed and introduced the XDSM diagram; the
classification is from recall and the survey's metadata only was confirmed. *(Verified existence [S14]; content
Flagged)* Martins & Ning's *Engineering Design Optimization* (Cambridge 2022, free PDF/HTML) covers surrogate-based
optimization (ch. 10) and MDO (ch. 13) and is the citable textbook route for the optimizer plug-in. *(Verified,
[S19])*

**Incremental computation as the recompute engine.** `salsa` (Rust, MIT/Apache-2.0) models a system as **inputs** that
may change and **pure tracked functions** memoized with dependency tracking; when an input changes it re-validates
only the dependent queries, uses **early cut-off** (a recomputed intermediate that is equal to its old value stops
propagation), **backdating**, **durability** classes (rarely-changing inputs are verified cheaply) and
**cancellation** of in-flight queries when inputs change. It is inspired by rustc's query system; rust-analyzer is its
best-known user (association from recall). *(Verified, [S40]; user list Flagged)* This is precisely the semantics
ANA-07 asks for: a result is *Current* iff every input it depended on is unchanged, and a changed input marks the
result Historical before any new value appears.

**Dataset-oriented orchestration.** Snakemake (MIT) decides what to re-run from **five rerun triggers — `code`,
`input`, `mtime`, `params`, `software-env` — all on by default**, "which guarantees that results are consistent with
the workflow code and configuration"; pure-timestamp behaviour is an opt-out (`--rerun-trigger mtime`). *(Verified,
[S48])* Dagster (Apache-2.0) frames pipelines as **software-defined assets** with integrated lineage and
materialization state. *(Verified, [S49])* Nextflow and Prefect are both Apache-2.0 to my recollection and add nothing
the two above do not; not opened this session. *(Flagged)* The lesson for the workbench is the trigger set, not the
engines: invalidate on geometry hash, operating point, fluid revision, method id **and method version**, and the
backend build identity.

**Expensive-evaluation queues.** The proposal already frames CFD sweeps as an embarrassingly parallel job queue
*(Verified internal, [S52] §7.3)*. Dakota (LGPL-2.1-or-later, v6.24, 2026-05-15) is the reference design for a driver
talking to a black-box simulation through **fork/system/direct interfaces** with parameter and results files, plus
surrogate-based optimization (Surfpack, PECOS). It is a precedent to copy in shape, not a dependency (LGPL and Python
bindings). *(Verified licence/version, [S20]; interface taxonomy from README and recall)* preCICE (LGPL-3.0, v3.4.1)
is the reference for *co-simulation coupling* (OpenFOAM, CalculiX, SU2 adapters exist in its ecosystem); it is
irrelevant for v1 (no FSI) and licence-excluded from linking. *(Verified, [S21])*

**Content-addressed caching.** The sequence document already keys results by a content hash of the geometry file
*(Verified internal, [S52] §3.2)*. BLAKE3 (default 256-bit, Merkle-tree internal so it parallelizes across
threads/SIMD; CC0-1.0 or Apache-2.0) has an official Rust crate and C implementation; `Blake3.NET` (BSD-2-Clause)
ships a fully managed package and a native package wrapping Rust 1.8.2 for win/linux/macOS x64 and ARM64. *(Verified,
[S41][S42])* SHA-256 from the .NET BCL is a zero-dependency alternative with no licence question; the choice is
performance, not correctness. *(Inferred)*

**The façade pattern ("one interface, four fidelities").** AeroSandbox (MIT) offers `AeroBuildup`,
`VortexLatticeMethod`, `LiftingLine` and wrappers for AVL/XFoil/XFLR5/ASWING/MSES behind a common
airplane/operating-point model, with NeuralFoil as the section-level provider; `asb.Opti` (CasADi-backed) makes the
whole chain differentiable. *(Verified, [S22])* VSPAERO is invoked by the OpenVSP GUI, the command line (`vspaero -omp
4 -stab model_degengeom`) or the API; it consumes DegenGeom `.csv` or Cart3D `.tri` plus a `.vspaero` setup file and
writes `.history` (integrated coefficients per iteration), `.lod` (spanwise loads), `.adb` (viewer), `.stab`, `.fem`.
*(Verified, [S25])* The sequence's `ISolverBackend` façade *(internal, [S52] §9)* is the same shape; the evidence here
says the façade must expose **capability flags** (what fields, what steadiness, what transition model) because the
tiers are not interchangeable, and must carry the **method version** into the cache key.

### 2. Multi-fidelity orchestration

**What the literature establishes.** Peherstorfer, Willcox & Gunzburger's SIAM Review survey (2018, 989 citations)
organizes multifidelity methods by how the low-fidelity models are managed — **adaptation** (correct/update the
low-fidelity model with high-fidelity information, e.g. trust-region model management), **fusion** (combine outputs,
e.g. co-Kriging, control variates), **filtering** (use the low-fidelity model to decide *when* to call the
high-fidelity one) — and states that accuracy guarantees come from keeping the high-fidelity model in the loop.
Metadata verified; the taxonomy is from recall. *(Verified existence [S15]; taxonomy Flagged)* Alexandrov, Nielsen,
Lewis & Anderson (NASA NTRS 20000097390, 2000) and Alexandrov & Lewis (NTRS 20040086473, 2000) define
**Approximation/Model Management Optimization (AMMO)**: a "rigorous methodology for attaining solutions of
high-fidelity optimization problems with minimal expense in high-fidelity function and derivative evaluation", where
the low-fidelity model is corrected so that its value **and gradient** match the high-fidelity model at the current
iterate (first-order consistency), then used inside a trust region whose radius grows or shrinks on the
actual-vs-predicted improvement ratio. *(Verified, [S18])* Kennedy & O'Hagan (Biometrika 2000, 1,716 citations) and
Forrester, Sóbester & Keane (Proc. R. Soc. A 2007, 1,158 citations) are the co-Kriging references: high fidelity is
modelled as a scaled low-fidelity model plus a Gaussian-process discrepancy, so the prediction carries a posterior
variance. *(Verified existence [S16][S17]; model form Inferred)*

**What this means for a UI.** None of these methods report a blended "best number" to a user; they either (a) correct
the cheap model at specific verified points and *bound* where the correction is trusted, or (b) report a fused
estimate **with its variance**. The spec's rule "no averaging or automatic best truth" (ANA-06) is therefore aligned
with practice; what is missing is the *discrepancy record*: for every pair of runs on the same Surface revision and
Operating point, store `δ = f_hi − f_lo` and `ρ = f_hi / f_lo` per quantity with both run ids. Accumulated
discrepancies are the training data for a future correction model; presenting them today as "Estimator over-predicts
L/D by 12 % at this point per CFD run #…" is a finding, not a prediction. *(Inferred from [S15][S18])*

**Verify-at-higher-tier is a filtering strategy.** COMMIT-01 (no certification on estimator evidence alone) is the
*filtering* pattern: the cheap tier ranks and screens candidates; the expensive tier verifies the survivors. The
literature adds one thing the spec lacks: the cheap tier's ranking is only trustworthy where its error is *monotone*
or *bounded*, so the discrepancy record should be inspected before trusting an estimator-ranked sweep. *(Inferred)*

**Calibration with sparse high-fidelity results.** With one or two CFD points per design, a Gaussian-process
discrepancy is under-determined; the honest v1 is a **per-quantity additive or multiplicative correction with declared
validity** (same Surface revision, same Re decade, same tier pair) and "Model uncertainty not quantified" displayed
unless an actual interval exists (A6). NeuralFoil's own approach — a convergence-classifier confidence penalized by
Mahalanobis distance from the training distribution — is a citable pattern for "how far from verified points am I"
that a correction layer can reuse. *(Inferred from [S1])*

### 3. ML surrogates for hydrofoil analysis

**NeuralFoil (the one the product already uses).** Training set: XFoil at M∞ = 0, 7,913,292 data points from runs of
which 56 % converged; geometry by merging three random parents from a 2,174-airfoil database with thickness scaling
and Kulfan-parameter perturbation; Re log-normal (median 296k; 2.5th–97.5th percentiles 1.87k–262M); **N_crit uniform
in [0, 18]**; forced transition location uniform in [0, 1] in 20 % of cases; α from a uniform-plus-normal mixture
spanning −27.9° to +28.6°. Test-set (395,665 cases) MAE for the xxxlarge model: C_L 0.012, ln C_D 0.020, C_M 0.002,
x_tr 0.007; on easy NACA cases mean relative drag error 0.37 %. Inputs: 18 CST/Kulfan parameters (8 per side + LE
modification + TE thickness), α, Re, n_crit, x_tr; 8 model sizes; ≈30× faster than XFoil per case and ≈1000× batched.
Licence MIT; requires Python ≥3.10, NumPy and AeroSandbox ≥4.2.4. `analysis_confidence` is trained on whether XFoil
converged for those inputs and its logit has the squared Mahalanobis distance to the training distribution subtracted,
so it tends to zero off-distribution. Stated limits: post-stall moments less accurate; no reversed-flow reattachment
near α = 180°; transonic window not captured (irrelevant in water). *(Verified, [S1][S2])* Consequences for the
workbench: water Ncrit values (the repo uses an Ncrit-for-water policy) lie inside the [0, 18] training range; water
Re of 10⁵–3×10⁶ lies in the dense part of the distribution; **NeuralFoil is a surrogate of XFoil, so it inherits
XFoil's model error and cannot be more valid than XFoil at the same point**; `analysis_confidence` must be surfaced
and stored with every polar row, and a low value must degrade the row's evidence label, never be hidden. *(Inferred
from [S1])*

**Field surrogates (GNN / neural operators / diffusion).** MeshGraphNets (Pfaff et al., ICLR 2021) learns mesh-based
dynamics by message passing and runs 1–2 orders of magnitude faster than its training simulator across aerodynamics,
structures and cloth. *(Verified, [S35])* AirfRANS (Bonnet et al., NeurIPS 2022 D&B) is the reference 2D
incompressible steady RANS airfoil dataset with four tasks — full data, scarce data, Reynolds extrapolation,
angle-of-attack extrapolation — and surface-stress metrics; **licence CC BY-NC-SA 4.0**, which forbids commercial
redistribution in a product. *(Verified, [S36])* NVIDIA PhysicsNeMo (Apache-2.0, PyTorch, CUDA 12/13) bundles FNO,
MeshGraphNet, Transolver, DoMINO, diffusion U-Nets and more. *(Verified, [S37])* The OOD literature is consistent: the
NeurIPS 2024 ML4CFD competition scored OOD generalization and physical compliance explicitly and found no standardized
uncertainty estimation among entrants; 2025–2026 work reports 23× error reduction "under out-of-distribution Reynolds
extrapolation" only with domain-decomposed architectures, boundary GNNs beating volumetric models by 85 % on OOD blade
sections, metamorphic-testing frameworks that separate model violations from out-of-domain use, and **solver-coupled
correction (Newton–Krylov iterations started from the surrogate prediction) as the way to make a surrogate reliable
OOD**. *(Verified, [S34])* Uncertainty: deep ensembles (Lakshminarayanan et al., NeurIPS 2017, 8,587 citations) remain
the default practical UQ for such regressors; existence verified, method from recall. *(Verified existence [S39];
method Flagged)*

**Generative airfoil models.** BézierGAN (Chen & Fuge, 2018) generates smooth curves from a low-dimensional latent
space by emitting rational-Bézier control points, targeting airfoils and hulls, and reports latent-space consistency
favourable for optimization. *(Verified, [S38])* The 2025–2026 stream is diffusion: latent diffusion with automatic
parameterization (DiffGeo, Sept 2026), **AirfoilGen** (May 2026) claims "valid-by-construction" geometry via a
circle-sweeping representation, a study comparing PCA/coordinate/SDF encodings found direct coordinates best and
explicitly discusses extrapolation beyond the training set, OptiWing3D (Dec 2025) pairs 2D and 3D optimized wings and
shows 3D optima diverge from 2D most near the tip, and reward-directed diffusion claims >10 % L/D gains "beyond
training distributions" — a claim that by construction has no independent high-fidelity check inside the paper's loop.
*(Verified listing, [S31]; individual claims read from abstracts only)*

**What a workbench may honestly use these for.** (a) *Fast preview*: NeuralFoil-class section polars with confidence
shown, consistent with the current spec. (b) *Candidate sampling*: a generative model may propose section shapes only
as a **Geometry edit draft** that enters the same validation and Apply path as a hand edit; the shape is Inferred
until a deterministic evaluation runs. (c) *Never certification*: no surrogate output is ever "Verified numerical
implementation" or "Experimentally compared" (A6); COMMIT-01 already forbids estimator-only certification, and a
learned surrogate sits **below** the estimator on the evidence ladder because its error is not derivable from its
inputs. (d) *Training-data hygiene*: AirfRANS is NC-licensed, UIUC data has its own terms (see
`06-foil-section-catalog.md`), so any in-product model must be trained on data whose terms allow it, and the training
manifest is part of the model's provenance. *(Inferred from [S1][S34][S36])*

### 4. LLM and agent integration in CAD/CFD (2024–2026)

**OpenFOAM agents — what is measured.** Thirteen arXiv papers from MetaOpenFOAM (Aug 2024, 85 % pass on 8 tutorial
tasks, $0.22/case) through OpenFOAMGPT (Jan 2025, RAG over tutorials; authors state "human oversight remains crucial"
and note performance fluctuation over time requiring monitoring), fine-tuned Qwen2.5-7B on 28,716 NL→OpenFOAM pairs
(Apr 2025, 88.7 % on 21 cases), Foam-Agent (v3 Aug 2026: 88.2 % on 110 basic FoamBench tasks, **62.5 % on the
out-of-distribution tier**, six specialist agents exposed through MCP, repairs conditioned on the accumulated error
trajectory), ChatCFD (Feb 2026: 315 cases, **82.1 % execution success vs 68.12 % physical fidelity**; removing the
solver template database collapses accuracy to 48 %; $0.208 and 192k tokens per case), SwarmFoam (Jan 2026, 84 % on 25
cases), PhyNiKCE (Feb 2026, neurosymbolic, fewer self-correction loops), AutoFOAM (May 2026, self-refining fine-tune
on 252 prompts), IteraSim RAG (Jul 2026, 28-case benchmark; diagnosed two corrupted cases from solver logs) to *What
Do CAE Simulation Agents Really Need Beyond a Generic Harness?* (Sept 2026: a plain single-agent harness scored **96.4
% vs 88.2 % for specialized multi-agent systems**; execution-feedback repair moved 71.8 % → 96.4 %; tutorial knowledge
80.9 % → 96.4 %; scripted reflection added nothing). *(Verified, [S26][S27][S28][S29][S30])*

**Reading the numbers honestly.** Every benchmark is tutorial-derived case setup; none evaluates a novel geometry with
a mesh the agent generated from scratch against experiment. The consistent pattern is: models can write syntactically
runnable OpenFOAM dictionaries when given retrieved templates and a compile-run-repair loop; they are markedly worse
off-distribution; and "it ran" overstates "it is physically meaningful" by ~14 points in the only paper that measured
both. The named repair mechanisms — retrieval over solver tutorials, dependency-aware cross-file consistency, minimal
edits conditioned on the error log — are exactly the shape of AI-05 (Diagnose failure → typed case-parameter diff →
user runs it). *(Inferred from [S26]–[S30])* Failure modes named or implied across the set: inconsistent entries
across files (`controlDict`/`fvSchemes`/boundary files), wrong solver for the physics, hallucinated or deprecated
dictionary keys (implied by the need for template databases and error locators), cases that converge to physically
wrong fields, and non-stationary model behaviour over time. *(Inferred; the abstracts do not enumerate a taxonomy —
Flagged as a gap)*

**Text-to-CAD.** Text2CAD (NeurIPS 2024 spotlight) extends DeepCAD to ~170k models / ~660k text annotations and
generates sketch-extrude sequences with a transformer; the 2026 successors (RA-CAD's generate-execute-critique-rewrite
loop, CIT-CAD's constraint-intent verification, HierCAD, ArtisanCAD distilling CATIA procedures, Ortho2CAD reporting
100 % valid code with GPT-5.5 on drawings-to-CAD, foundation-model surveys reporting ~99 % mesh success and IoU
0.885–0.890 on canonical families but "systematic difficulties with rotationally symmetric geometries", ASSEMCAD for
assemblies) measure Chamfer distance, IoU and execution validity. The most important finding for this product: *Wrong
Design Intent Is Worse Than Never Conditioning* (Jul 2026) — on 400 held-out programs, a semantically wrong intent
header **lowers adherence below the unconditioned baseline while executability stays flat**, and even ground-truth
intent reaches only 0.567 on its adherence metric. *(Verified listing and abstracts, [S32][S33])* Commercially, Zoo's
Zookeeper turns prompts into editable models with an API and credits pricing; its capabilities/limitations text was
not readable in the fetched page. Onshape AI Advisor and Autodesk Fusion AI features were not verifiable this session
(404 / not opened). *(Verified partial [S50]; Onshape/Fusion Flagged)*

**Structured outputs, tool use, MCP.** Anthropic structured outputs: `output_config.format` (JSON schema) and `strict:
true` tool use guarantee schema-valid responses by constrained decoding on Opus 4/5, Sonnet 4/5, Haiku 4.5 (and newer
families); supported schema features include types, `enum`, `const`, `anyOf`/`allOf`, internal `$ref`, `required`,
`additionalProperties: false`, string formats; **unsupported: recursive schemas, `minimum`/`maximum`,
`minLength`/`maxLength`, array constraints beyond `minItems` 0/1**; beta headers no longer required. *(Verified
2026-09-20, [S7])* MCP (spec 2026-07-28) defines tools (name, `inputSchema`, optional `outputSchema` that servers MUST
honour and clients SHOULD validate, annotations that clients MUST treat as untrusted), resources, prompts,
elicitation, and a Tasks extension for long-running operations; it states "there SHOULD always be a human in the loop
with the ability to deny tool invocations", requires servers to validate inputs, rate-limit and sanitize outputs, and
asks clients to show tool inputs before calling, time out, and log tool usage for audit. Tool-execution errors are
returned as `isError: true` content for the model to self-correct; protocol errors are JSON-RPC errors. *(Verified,
[S8][S9])*

**Retrieval over the domain knowledge base.** The proposal plans bundled markdown with citations *(internal, [S52]
§8.3)*. SQLite FTS5 (public-domain SQLite core; from recall — not opened) gives BM25 ranking without a dependency;
`sqlite-vec` (MIT/Apache-2.0) adds brute-force KNN over float/int8/binary vectors in `vec0` virtual tables but is
**pre-v1 with breaking changes expected** and lists no .NET binding. *(Verified [S43]; FTS5 Flagged)* Given a corpus
of tens of documents, BM25 over FTS5 is the smallest correct choice; embeddings add a network or model dependency the
offline rule forbids on the critical path. *(Inferred)*

**Guardrails established by the evidence.** (1) Schema validity at the API (structured outputs) **plus** deterministic
range/domain validation in code, because the schema subset cannot express bounds. (2) Numeral check — every numeral in
shown prose must exist in the packed context (the repo's SPIKE-04 rule; no external paper measures this exact control,
so it is a product rule, not established literature). (3) No side effects from model output: proposals are Entities
with acceptance state (A3), and MCP's human-in-the-loop principle is the external statement of the same rule. (4)
Injection isolation: imported files and solver logs are quoted data (AI-04; MCP says annotations and tool results are
untrusted). (5) Execution feedback: for AI-05 the "repair" is a typed diff applied by the user, so the loop the
literature says matters most runs *through the human*, at the cost of fewer automatic iterations — an accepted trade
in a safety-relevant tool. *(Verified [S7][S9]; product rules internal [S51][S52])*

### 5. Composition of "simple algorithms" — the DRC layer

KiCad's DRC checks connectivity, clearances, widths, courtyards and board-outline integrity; each rule type has a
configurable severity (**error marker, warning marker, or ignored**), individual violations can be **excluded**
without disabling the rule, custom rules are written in a DSL of conditions and constraints with syntax validation,
and the violation list navigates to the offending location. *(Verified, [S46])* Fusion's manufacturing checks were not
opened this session. *(Flagged)*

The workbench's cheap checks are of the same kind and should be one subsystem: a **rule** has an id, a severity
policy, a scope (surface, station, profile, operating point, run), a pure evaluator over the current document, and a
message with the located element; a **violation** carries the rule id, severity, location, measured value versus
threshold and an optional user exclusion with reason and revision. Candidate v1 rules (each from existing repo
knowledge unless noted): TE thickness floor (manufacturing; GAP-07), AR = b²/S_proj with full span (grounding
register), Re per station from chord × speed × ν(fluid revision), cavitation σ vs −Cp_min at the operating point,
symmetry of mirrored channels, monotone/positive chord, station ordering, profile applicability envelope (Re, Ncrit)
versus polar source, class-box limits (GAP-13; rule values are user-supplied until a class rule is admitted), and
manufacturability screens (min thickness, draft — values Flagged until GAP-07 research lands). Rules run on every edit
(they are microseconds), block Apply only at severity *error*, never silently mutate geometry, and their outcomes are
recorded with the Surface revision so a later run's provenance includes the checks that passed. *(Inferred from
[S46][S53])*

### 6. Reproducibility and audit

**Run manifest.** A PROV-DM mapping for the Analysis run aggregate (A3): the run is an `Activity`; it `used` the
Surface revision, Profile revisions, Water conditions/fluid-property revision, Operating point and Reference
quantities (all `Entity`s identified by content hash); it `wasAssociatedWith` the method (solver id, version, build
digest, settings hash) and the machine; every result and Field evidence `wasGeneratedBy` the run and `wasDerivedFrom`
its inputs; an Assistance proposal `wasAttributedTo` the model identifier and `wasDerivedFrom` the context pack it
saw. *(Verified vocabulary [S10]; mapping Inferred)* OpenMDAO's `SqliteRecorder` records inputs, outputs, residuals,
derivatives, solver iterations and model metadata per case with a `CaseReader` to reload — the working example of
"results are the record" in a design tool. *(Verified, [S13])*

**Immutable results by hash.** The proposal's rule — results in a sidecar keyed by content hash of the geometry file,
derived quantities absent from the file *(internal, [S52] §3.2)* — matches Snakemake's default that parameters, code
and software environment are all rerun triggers. The key must include: canonical serialization of the Surface revision
(not the file bytes — formatting must not change identity), the profile revision hashes, the fluid-property revision
id, the operating point values in SI, the method id **and version/build digest**, and the settings hash. Display
units, file paths and UI state are excluded (A7 Compatibility). *(Inferred from [S48][S52])*

**Event-sourced document history.** The spec demands lossless history and immutable evidence without naming a store
(A3). Append-only revisions with parent links, plus runs referencing revisions by hash, give
comparison-across-revisions for free (ANA-06: geometry, conditions and methods side by side; a numeric delta is
blocked when reference quantities differ). A hash-keyed store also makes deduplication of identical sweep samples
automatic. *(Inferred)*

**How established tools do or fail to do this.** ParaView `.pvsm`/Python state files capture the whole pipeline, views
and camera but reference data by **absolute path**, so reloads offer "use file names from state / search under
directory / choose file names"; screenshots and animations are exported separately with no link back to the state that
produced them. *(Verified, [S47])* VSPAERO writes `.history`, `.lod`, `.adb`, `.stab`, `.fem` next to the DegenGeom
file and the tutorial describes no automated results-manager linkage — provenance is by filename convention.
*(Verified, [S25])* XFLR5 is GPL and stores analyses inside a binary project file with, to my recollection, no
revision identity or invalidation of results after geometry edits; not opened this session. *(Flagged)*

### 7. Architecture patterns and libraries for the composition layer

| Concern | .NET candidate | Rust candidate | Licence (accessed 2026-09-20) | Confidence |
|---|---|---|---|---|
| Incremental dependency graph | Own small graph over records + `R3`/`System.Reactive` for change propagation | `salsa` | R3 MIT [S44]; System.Reactive MIT (recall); salsa MIT/Apache-2.0 [S40] | R3/salsa Verified; System.Reactive Flagged |
| Graph data structure | Own adjacency lists (few hundred nodes) | `petgraph` | petgraph MIT/Apache-2.0 (recall) | Flagged |
| Job scheduler / queue | TPL Dataflow (`System.Threading.Tasks.Dataflow`, part of dotnet, MIT), `Channel<T>` | `tokio` (MIT) | recall | Flagged |
| Content hashing | `Blake3` / `Blake3.Native` (BSD-2-Clause) or BCL SHA-256 | `blake3` crate | BLAKE3 CC0-1.0/Apache-2.0 [S41]; Blake3.NET BSD-2-Clause [S42] | Verified |
| Process orchestration | `CliWrap` (MIT): stdout/stderr piping, graceful then forceful cancellation, exit-code validation, async/Rx event streams | `tokio::process` | [S45] | Verified |
| MCP hosting (assistant tools) | `ModelContextProtocol` / `.Core` / `.AspNetCore` (Apache-2.0, with Microsoft) | `rmcp` + `rmcp-macros` (Apache-2.0; stdio, streamable HTTP; targets spec 2026-07-28) | [S5][S6] | Verified |
| Anthropic API | `Anthropic` NuGet v12.49.0 (2026-09-18), MIT, official, netstandard2.0/net8/net9; versions ≤3.x were community `tryAGI` | No official repo (`anthropics/anthropic-sdk-rust` 404); use `reqwest` + JSON | [S3][S4] | Verified |
| Local retrieval | SQLite FTS5 (BM25) via `Microsoft.Data.Sqlite` | `rusqlite` + FTS5 | FTS5 public domain (recall); `sqlite-vec` MIT/Apache-2.0 but pre-v1 [S43] | FTS5 Flagged; sqlite-vec Verified |
| Optimizer plug-in (future) | Own interface; reference designs OpenMDAO (Apache-2.0), Dakota (LGPL-2.1+, process-only), AeroSandbox `Opti` (MIT, Python) | — | [S12][S20][S22] | Verified |

Notes: `Blake3.NET`'s managed package removes the native-binary question on both platforms; the native package pins
Rust BLAKE3 1.8.2. The MCP C# SDK page did not show a version or stable/preview marker — check NuGet before pinning.
*(Verified, [S5][S42])*

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| OpenMDAO [S11][S12][S13] | Model = graph of components with derivatives; driver outside | Problem/Group/Component, unified derivatives, SqliteRecorder | Provenance of every case; gradient plumbing; textbook MDO home | Python-only; batch, not interactive; no UI notion of staleness | Apache-2.0 | Verified |
| Dakota [S20] | Driver ↔ black-box simulation via files/processes | fork/system/direct interfaces; surrogate-based opt; UQ | Interface taxonomy; mature UQ | LGPL; heavy; not embeddable under COMMIT-02 | LGPL-2.1-or-later | Verified |
| AeroSandbox + NeuralFoil [S1][S2][S22] | One airplane model, several fidelities, differentiable | AeroBuildup/VLM/LiftingLine + NeuralFoil sections; CasADi Opti | Façade over fidelities; confidence output; MIT | Python; aircraft conventions; no provenance store | MIT | Verified |
| OpenVSP / VSPAERO [S23][S24][S25] | Parametric geometry + external VLM/panel run | DegenGeom + `.vspaero` → `.history/.lod/.adb` | Clear file contracts; API + CLI | Provenance by filename; NOSA-1.3 excludes linking | NOSA-1.3 (OSI-approved, GPL-incompatible) | Verified |
| preCICE [S21] | Partitioned multi-physics coupling | Library + adapters (OpenFOAM, CalculiX, …) | Co-simulation done right | Irrelevant for v1; LGPL-3.0 | LGPL-3.0 | Verified |
| salsa [S40] | Incremental computation as query memoization | Inputs, tracked functions, early cut-off, durability, cancellation | Exactly the staleness semantics ANA-07 wants | Rust only; no persistence of memo across sessions by default | MIT/Apache-2.0 | Verified |
| Snakemake [S48] | Files-as-DAG with rerun triggers | code/input/mtime/params/software-env | Invalidation on method and params, not just time | Batch CLI; Python | MIT | Verified |
| Dagster [S49] | Software-defined assets with lineage | Materialization + lineage UI | Asset staleness as a product concept | Cloud data-pipeline scale, not desktop | Apache-2.0 | Verified |
| ParaView state [S47] | Pipeline serialization | `.pvsm` / `.py` state; path-relocation dialog | Complete scene capture | Absolute paths; exports not linked to state | BSD-3-Clause (recall) | Verified behaviour; licence Flagged |
| KiCad DRC [S46] | Rules with severities over a design | Built-in + custom rule DSL; exclusions; navigable violations | The DRC model to copy | EDA-specific | GPL-3.0 (recall; pattern only) | Verified behaviour |
| Foam-Agent / ChatCFD / harness study [S27][S28][S29] | LLM agents set up and repair OpenFOAM cases | RAG over tutorials + run-repair loops | Runnable cases in-distribution | 62.5 % OOD; physical fidelity < execution success | Papers (code licences vary; not checked) | Verified |
| Text2CAD and successors [S32][S33] | Text → CAD program | Autoregressive / LLM code gen with critique loops | Benchmarks and metrics exist | Valid-but-wrong geometry; intent errors worse than none | Research code | Verified |
| Anthropic structured outputs + MCP [S7][S8][S9] | Schema-constrained model output; tool protocol | Constrained decoding; JSON-RPC tools with human consent | Guarantees the shape | Cannot express numeric bounds; annotations untrusted | API / spec | Verified |
| Zoo Zookeeper [S50] | Prompt → editable CAD | Hosted model + API, credits | Commercial proof that text-to-CAD ships | Limits page unreadable; hosted-only | Proprietary | Verified partial |

## Reference information

- **W3C PROV-DM (Recommendation 2013-04-30)** [S10] — Entity/Activity/Agent and the six core relations. *Requires of
  us:* every Analysis run, Sweep sample, Field evidence and Assistance proposal must be expressible as these
  relations; use the vocabulary in the run manifest so exports are interoperable.
- **Model Context Protocol, revision 2026-07-28** [S8][S9] — JSON-RPC 2.0; tools with
  `inputSchema`/`outputSchema`/annotations; human-in-the-loop SHOULD; servers MUST validate inputs, rate-limit,
  sanitize outputs; clients SHOULD show inputs before calls, validate results, time out and log for audit. *Requires
  of us:* any assistant tool the workbench exposes is read-only or proposal-producing, its `outputSchema` is validated
  server-side, and the host logs every call under `assistant.request` (A7 observability).
- **Anthropic structured outputs** [S7] — `output_config.format` and `strict: true`; unsupported numeric bounds.
  *Requires of us:* deterministic post-validation of every field (range, unit, domain) after schema parsing; AI-02's
  Stated/Inferred/Defaulted per-field provenance is a schema field, not prose.
- **Anthropic C# SDK** [S3][S4] — official `Anthropic` NuGet, MIT, v12.49.0 (2026-09-18), netstandard2.0/net8/net9;
  note it does not list a net10 TFM, which is fine for a net10 consumer via netstandard2.0/net9 but should be
  smoke-tested. *Requires of us:* the sequence's "official .NET SDK" plan holds; pin the version and record it in
  `assistant.request` events.
- **MCP C# SDK / Rust `rmcp`** [S5][S6] — Apache-2.0; stdio and streamable HTTP; `rmcp` targets spec 2026-07-28.
  *Requires of us:* if the assistant's tools are hosted in-process, prefer stdio to a local child or in-process
  transport; never expose a network listener by default (A7 privacy).
- **NeuralFoil paper and repository** [S1][S2] — training envelope and `analysis_confidence` definition. *Requires of
  us:* store `analysis_confidence`, n_crit, Re, x_tr and NeuralFoil model size + package version on every polar row;
  treat confidence below a documented threshold as out-of-envelope (A6 "explicitly unsupported observations").
- **OpenMDAO** [S11][S12][S13] — Apache-2.0; component graph; case recording. *Requires of us:* nothing to link; the
  optimizer plug-in interface should be expressible as an OpenMDAO-style explicit component (inputs → outputs,
  optional partials) so an external OpenMDAO/AeroSandbox driver could call the headless CLI (CLI-01).
- **Alexandrov & Lewis AMMO, NASA NTRS 20000097390 and 20040086473** [S18] — first-order consistency and trust-region
  management of variable-fidelity models. *Requires of us:* if an optimizer ever uses the estimator or VLM to propose
  steps, the step is accepted only after a higher-tier evaluation confirms the predicted improvement (this is
  COMMIT-01 in trust-region form).
- **Peherstorfer–Willcox–Gunzburger 2018; Kennedy–O'Hagan 2000; Forrester–Sóbester–Keane 2007** [S15][S16][S17] — the
  multifidelity canon. *Requires of us:* discrepancy records now, so a fusion model is possible later; never present a
  fused number without its variance.
- **Snakemake rerun triggers** [S48] — code, input, mtime, params, software-env. *Requires of us:* the cache key
  includes method version and settings; timestamp is never a freshness criterion.
- **KiCad DRC** [S46] — severity per rule, exclusions per violation, navigable list. *Requires of us:* the validator
  subsystem's data model.
- **AirfRANS licence CC BY-NC-SA 4.0** [S36] — *Requires of us:* no bundling or commercial retraining on AirfRANS;
  cite only.
- **OpenVSP NOSA-1.3** [S23][S24] — OSI-approved, not GPL-compatible, not MIT/BSD/Apache. *Requires of us:* under
  COMMIT-02, OpenVSP/VSPAERO may only be invoked as an external process, never linked or redistributed inside the
  product.

## Data, constants, formulae and invariants

**Cache / identity key (Inferred design; hash algorithm Verified [S41]).**

```
run_key = BLAKE3( canon(surface_revision) ‖ canon(profile_revisions[]) ‖ fluid_property_revision_id
                 ‖ canon(operating_point_SI) ‖ canon(reference_quantities) ‖ method_id ‖ method_version
                 ‖ backend_build_digest ‖ canon(settings) )
```
`canon()` is a canonical serialization (sorted keys, fixed float formatting, SI units, no display state). Invariants:
(i) re-serializing an unchanged document yields the same key; (ii) changing display units or file path does not change
the key (A7 Compatibility); (iii) changing method version changes the key even when inputs are identical (Snakemake
`code`/`software-env` triggers [S48]); (iv) a result is *Current* iff `run_key(result) == run_key(current document)`
for its tier, else *Historical* (ANA-07).

**Recompute policy per tier (Inferred; latency floors from spec A7 [S51]).**

| Tier | Trigger | Latency target | Cancellation |
|---|---|---|---|
| DRC validators, estimator | Every edit, synchronous | ≤100 ms editing feedback p95 | n/a |
| 2D polars (catalog lookup / NeuralFoil sidecar) | Debounced after edit settles (order 200–500 ms; value is a design choice, not evidence) | ≤250 ms preview regeneration when cached | Cancel superseded query (salsa semantics [S40]) |
| VLM + strip theory | Debounced; may run on a worker | seconds | Cancel superseded run |
| CFD (SU2 / OpenFOAM process) | **Never automatic**; explicit Run creates an Analysis run and queues it | minutes–hours | Graceful then forceful kill (CliWrap [S45]); ack ≤250 ms; shutdown ≤5 s (A7) |
| Sweeps | Explicit; embarrassingly parallel over (V, α) pairs; priority = user-selected sample first | — | Per-sample cancel; retained attempt provenance (A3 Sweep) |

**Multi-fidelity correction forms (Inferred from [S15][S16][S17][S18]; model form from recall).**

- Additive discrepancy at a verified point `x*`: `δ_q = q_hi(x*) − q_lo(x*)`; corrected low-fidelity `q̃_lo(x) =
  q_lo(x) + δ_q`, valid only in a declared neighbourhood (same Surface revision, same Re decade, same tier pair, same
  fluid).
- Multiplicative: `ρ_q = q_hi(x*)/q_lo(x*)`, `q̃_lo = ρ_q · q_lo`; prefer for positive quantities (C_D, L/D); never
  for quantities that cross zero (C_M, C_L near α₀).
- Kennedy–O'Hagan autoregressive form: `q_hi(x) = ρ · q_lo(x) + δ(x)`, `δ ~ GP(0, k)`, giving a posterior variance;
  requires ≥ several high-fidelity points — not a v1 capability.
- First-order consistency (AMMO): a corrected surrogate `f̃` used to steer a step must satisfy `f̃(x_k) = f(x_k)` and
  `∇f̃(x_k) = ∇f(x_k)`; acceptance ratio `r_k = [f(x_k) − f(x_k + s_k)] / [f̃(x_k) − f̃(x_k + s_k)]` computed with the
  **high-fidelity** `f`, shrinking the trust region when `r_k` is small. With VLM/CFD gradients unavailable in v1,
  only the value condition can be checked — which is exactly "verify at higher tier before certify".

**NeuralFoil envelope constants (Verified [S1][S2]).** Inputs: 18 Kulfan parameters, α [deg], Re, n_crit, x_tr
(upper/lower). Training: Re median 296k, 2.5–97.5 % ≈ 1.87k–262M; n_crit ∈ [0, 18]; α observed −27.9°…+28.6°
(post-stall cases present but less accurate for C_M); M = 0. Test MAE (xxxlarge): C_L 0.012, ln C_D 0.020 (≈2 %
relative), C_M 0.002, x_tr 0.007. `analysis_confidence = σ(logit_converged − d_M²)` where `d_M` is the Mahalanobis
distance of the input to the training distribution (form as described in the paper text; exact constants not
extracted).

**LLM-agent benchmark figures (Verified [S26]–[S29]).** Foam-Agent: 88.2 % (110 basic) / 62.5 % (advanced OOD).
ChatCFD: 82.1 % execution / 68.12 % physical fidelity; 192.1k tokens and $0.208 per case. Harness study: 96.4 %
single-agent vs 88.2 % multi-agent; repair loop 71.8→96.4 %; tutorials 80.9→96.4 %. MetaOpenFOAM: 85 % on 8 tasks,
$0.22/case. These are tutorial-derived setups, not validated physics.

**Invariants that must always hold.**
1. No result is displayed as Current whose `run_key` differs from the current document's key for that tier.
2. A model-produced proposal has no effect on any aggregate until deterministic validation passes and the user
   accepts; a proposal created against a document that has since changed is blocked until re-diffed (AI-02).
3. Every numeral in assistant prose exists in the packed context; otherwise the reply is rejected (product rule;
   SPIKE-04).
4. Disagreement between tiers is stored as a discrepancy record, displayed as a finding, and never averaged (ANA-06).
5. A DRC *error* blocks Apply; a *warning* is visible on the revision and in the run manifest; an exclusion carries a
   reason and the revision at which it was made.
6. A surrogate's own confidence below threshold demotes its row to "explicitly unsupported observation" (A6) — it
   never promotes.

## Design implications for CFD-Workbench

1. **Composition kernel (A3 Analysis run, Sweep; ANA-07).** Implement the document → derived-value layer as an
   in-process incremental graph with salsa semantics — inputs, memoized pure queries, dependency tracking, early
   cut-off, cancellation — regardless of language; in C# this is a small custom graph (a few hundred nodes) with `R3`
   for change notification, not a framework. *(Inferred from [S40][S44])*
2. **Identity by content, freshness by key equality (ANA-07, A7).** Adopt the `run_key` above; store it on every
   Analysis run and Field evidence; compute "Current/Historical" by comparison, never by timestamp or by a dirty flag
   that can be forgotten. Hash with BLAKE3 (`Blake3` managed package, BSD-2-Clause) or BCL SHA-256; record the
   algorithm id in the manifest. *(Inferred; [S41][S42][S48])*
3. **Tier recompute policy is explicit configuration, not code paths.** Estimator/DRC synchronous; polars and VLM
   debounced with cancellation; CFD never automatic (CFD-xx, ANA-07). Emit `analysis.run` events with tier, key,
   duration, outcome on the normal path (A7 observability / IO). *(Inferred)*
4. **Job queue for CFD and sweeps (A3 Sweep, CFD-05/06).** Local queue with priority, per-job cancellation via CliWrap
   graceful→forceful, checkpointing by retaining solver output directories keyed by run_key, sweeps as independent
   jobs per (V, α) pair with an explicit attempt history. Dakota's fork interface (parameters file in, results file
   out, process per evaluation) is the shape; do not depend on Dakota. *(Inferred from [S20][S45][S52])*
5. **Run manifest = PROV-DM record (A3 Analysis run, Field evidence).** Persist, per run: input entity hashes, method
   id/version/build digest, settings hash, machine/OS, backend installation id and smoke-test evidence, DRC outcomes
   at run time, start/end, exit status, resource observations, and the list of generated evidence entities. Use PROV
   terms so an export is standard. *(Inferred from [S10][S13])*
6. **Multi-fidelity reconciliation (ANA-06, GAP-06).** Add a *Discrepancy record* value object: (run_hi, run_lo,
   quantity, δ, ρ, operating point, validity scope). UI shows all tiers with assumptions and the discrepancy as a
   finding; the *only* correction permitted in v1 is a user-visible additive/multiplicative correction with declared
   scope and "Model uncertainty not quantified". Co-Kriging/GP fusion is a later plug-in fed by these records.
   *(Inferred from [S15][S17][S18])*
7. **COMMIT-01 as trust-region acceptance for the future optimizer.** The optimizer plug-in proposes candidates with
   the cheap tier and may only *rank*; certification requires the predicted improvement to be confirmed by the next
   tier at the candidate point (value-level first-order consistency). Expose the plug-in as an explicit component
   (inputs → outputs, optional partials) callable through the headless CLI (CLI-01) so OpenMDAO/AeroSandbox drivers
   can use it externally. *(Inferred from [S11][S18][S22])*
8. **Surrogates below the estimator on the evidence ladder (A6, COMMIT-01/04).** NeuralFoil rows carry
   `analysis_confidence`, n_crit, Re, model size and package version; a confidence threshold (to be set by a fixture
   study, not assumed) demotes rows. Generative airfoil models, if ever used, produce Geometry edit drafts only.
   AirfRANS and other NC-licensed datasets are cite-only. *(Verified envelope [S1][S36]; policy Inferred)*
9. **Assistant surface (AI-01–06, COMMIT-04).** Use the official `Anthropic` NuGet with `output_config.format` for
   AI-02 recipe proposals and AI-05 case-parameter diffs, `strict: true` for any tool the model may call, and
   deterministic validation of every numeric field after parsing (bounds are not expressible in the supported schema
   subset). Expose workbench data to the model as read-only MCP-style tools with `outputSchema` and `readOnlyHint`
   semantics — or an in-process equivalent — never a write tool; the human accepts proposals (MCP human-in-the-loop
   principle). Log provider/model/usage per request; "Not recorded" when unavailable. *(Verified [S3][S7][S9]; mapping
   Inferred)*
10. **Failure diagnosis loop (AI-05).** Give the model retrieved solver-tutorial snippets and the error log (the two
    levers the harness study found decisive), constrain its output to a typed case diff, and require the user to run.
    Expect in-distribution success but plan for OOD failure: the diagnosis must be able to say "no supported claim".
    *(Inferred from [S27][S29])*
11. **Retrieval (AI-03).** SQLite FTS5/BM25 over the bundled knowledge files with citation ids; no vector index in v1
    (sqlite-vec is pre-v1 and has no .NET binding). *(Verified [S43]; FTS5 Flagged; choice Inferred)*
12. **DRC subsystem (GEO-xx, ANA-xx, GAP-07/13).** One validator registry with rule id, severity policy
    (error/warning/ignore), scope, pure evaluator, located violation, and per-violation exclusion with reason and
    revision; DRC results recorded on the revision and in run manifests. Model on KiCad. *(Verified pattern [S46];
    mapping Inferred)*
13. **Headless parity (CLI-01).** The CLI and GUI share the composition kernel; the CLI prints the run_key it
    evaluated so CI can assert GUI/CLI identity by key equality, not by re-deriving inputs. *(Inferred)*
14. **Reference composition architecture for v1 (Inferred).** `Document store (append-only revisions, hash-addressed)`
    → `Incremental graph (inputs: revisions, operating point, fluid revision, settings; queries: DRC, estimator, polar
    lookup, VLM, results projections)` → `Job service (queue, priority, cancel, sweeps)` → `Backend façade
    (capabilities, process launch, harvest, manifest)` → `Evidence store (runs, field evidence, discrepancy records
    keyed by run_key)` → `Projections (UI view models, CLI JSON)`; the assistant attaches to Projections (read) and to
    Proposals (typed write requests) only.
15. **Eval harness a model-backed capability must pass before it ships (extends A7's five evals; Inferred from
    [S7][S9][S28][S32]).**
    - *Extraction*: brief → recipe proposals against a fixture set with per-field Stated/Inferred/Defaulted labels;
      score field accuracy and label accuracy separately.
    - *Schema and domain validity*: 100 % schema-valid (guaranteed by API) **and** ≥ a set threshold passing
      deterministic range/domain validation; log every domain rejection class.
    - *Unsupported-answer*: questions whose answer is not in the context must yield a decline; measure false-claim
      rate.
    - *Numerical-meaning*: numeral check passes; unit/scope preserved; rounding fixtures.
    - *Attribution*: every claim cites the run or knowledge id; measure citation precision.
    - *Prompt injection*: instructions embedded in imported `.dat` files, solver logs and knowledge snippets never
      produce an action or a changed proposal field.
    - *Coordinate/force refusal*: prompts asking for coordinates, meshes or force numbers are refused or redirected to
      a re-solve action.
    - *Physical-fidelity check for AI-05*: on a fixture set of failed cases with known causes, measure correct-cause
      rate separately from "produced a runnable diff" (the ChatCFD execution-vs-fidelity gap).
    - *Drift*: re-run the suite on each model identifier change (AI-06) and on a schedule; OpenFOAMGPT's authors
      report performance fluctuation over time.
    - *Cost/latency budget*: tokens and wall time per capability recorded; thresholds set in architecture.

## Open questions and domain failure modes

**Open questions (cheapest next probe in parentheses).**
1. Does the official `Anthropic` NuGet build and run under net10.0 with structured outputs and strict tools? (Spike:
   `dotnet run` a 20-line probe under `spikes/`, record SDK version.)
2. Is `Blake3` managed throughput adequate for hashing a 21-station/201-slice document at edit rate, or is SHA-256
   simpler? (Micro-benchmark on the A7 fixture.)
3. What `analysis_confidence` threshold corresponds to XFoil non-convergence in the water Re/Ncrit band? (Sweep the
   catalog sections over Re 10⁵–3×10⁶, Ncrit water policy; compare against XFoil convergence.)
4. Do ISolverBackend capability flags need a formal schema shared with the CLI? (Draft in `/define-architecture`;
   verify with SU2 and OpenFOAM smoke tests.)
5. The Peherstorfer taxonomy and the Martins & Lambe architecture classification were confirmed by metadata only.
   (Open the SIAM Review PDF via a library or the authors' site and the AIAA J. paper; 2 fetches.)
6. XFLR5's project-file provenance behaviour and Fusion's manufacturing checks were not opened. (One fetch each.)
7. Is there a published failure-mode taxonomy for OpenFOAM agents (hallucinated keys, cross-file inconsistency, wrong
   solver)? (Read the Foam-Agent and ChatCFD full texts; extract their error categories.)
8. Nextflow/Prefect licences and Grasshopper's dataflow model are recall-only. (Fetch repositories if the roll-up
   needs them.)

**Domain failure modes.**
- *Silent staleness*: a result stays labelled Current after an input changed because the dirty flag lived outside the
  graph — prevented by key equality (Implication 2).
- *Averaged truth*: a UI shows one L/D that is a blend of tiers — forbidden by ANA-06; the discrepancy record makes
  disagreement visible.
- *Runs-but-wrong*: an agent-produced or auto-repaired case converges to a physically meaningless field (ChatCFD's
  14-point gap); prevented by keeping the human on the Run action and by physical-fidelity evals.
- *Valid-but-wrong geometry*: a generative or LLM proposal yields a schema-valid, executable, wrong shape (text-to-CAD
  finding); prevented by the draft → validate → preview → accept path.
- *Off-distribution confidence*: a surrogate returns a plausible number where it has no data — NeuralFoil's confidence
  exists; other surrogates often have none; do not adopt a surrogate without a confidence output or an ensemble.
- *Path-bound provenance*: results referencing files by absolute path (ParaView pattern) break on move/share;
  reference by hash.
- *Version-blind cache*: a cache keyed on inputs only serves results from an older solver build; include method
  version and build digest.
- *Licence contamination*: linking NOSA/LGPL code or training on NC data; COMMIT-02 review at dependency admission.

## Disconfirming views sought

- **"A reactive/incremental graph is over-engineering for one wing; recompute everything on every edit."** Fared
  partly: the estimator and DRC can indeed be recomputed wholesale within 100 ms, but polars via a sidecar and VLM
  cannot, and CFD must never auto-run; the graph is what makes tier policy and staleness explicit. Kept, with the
  scope reduced to a small custom graph (Implication 1).
- **"Multi-fidelity fusion (co-Kriging) is mature; ship a corrected estimator now."** Fared badly for v1: fusion needs
  several high-fidelity points per design and yields a variance the UI must display; with one CFD run per design the
  correction is a scalar with a declared scope. The canon supports recording discrepancies now and fusing later.
  [S15][S17]
- **"LLM agents can now run CFD end to end — let the assistant set up and run cases."** Fared badly: 62.5 % OOD and 68
  % physical fidelity on tutorial-class tasks, plus the authors' own call for human oversight; the human-run rule
  (AI-05) stands. The literature does support feeding the model the error log plus retrieved tutorials for diagnosis.
  [S27][S28][S29][S30]
- **"Structured outputs make deterministic validation redundant."** Fared badly: the supported schema subset cannot
  express numeric bounds or string lengths, so range/domain checks stay in code. [S7]
- **"Text-to-CAD works; let the model emit CST coefficients."** Fared partly: benchmarks report high executability and
  IoU on canonical shapes, but intent errors degrade results below the unconditioned baseline while remaining
  executable; a CST proposal is only acceptable as a Geometry edit draft with fit residual and deterministic
  evaluation, which is what AI-06 already defers. [S32]
- **"NeuralFoil generalizes unusually well, so its confidence output is unnecessary."** Fared partly: the README
  claims strong generalization, but the paper defines confidence precisely because XFoil non-convergence and distance
  from training data are real; storing it costs nothing. [S1][S2]
- **"Use a workflow engine (Snakemake/Dagster/Prefect) instead of writing a graph."** Fared badly: they are
  batch/cloud process orchestrators in Python; the pattern (rerun triggers, asset lineage) transfers, the engines do
  not. [S48][S49]
- **"OpenVSP/VSPAERO is free — embed it as the VLM."** Fared badly: NOSA-1.3 is OSI-approved but GPL-incompatible and
  not in the permitted set; process-invoke only, and the product already commits to its own VLM. [S23][S24]

## Glossary terms

- **Incremental computation (query memoization)** — Model in which pure derived values are memoized with tracked
  dependencies and re-validated only when a dependency changed; includes early cut-off and cancellation. *(Verified,
  [S40])*
- **Rerun trigger** — A class of change (code, input, mtime, params, software-env) that marks a downstream result
  outdated. *(Verified, [S48])*
- **Content hash / run_key** — Digest of the canonical inputs, method identity and version that identifies a result;
  freshness is key equality. *(Inferred; hash Verified [S41])*
- **PROV-DM** — W3C provenance data model: Entity, Activity, Agent and relations `used`, `wasGeneratedBy`,
  `wasDerivedFrom`, `wasAttributedTo`, `wasAssociatedWith`, `wasInformedBy`. *(Verified, [S10])*
- **Run manifest** — The persisted PROV record of one Analysis run: inputs by hash, method/version/build, settings,
  environment, outcomes and generated evidence. *(Inferred)*
- **Discrepancy record** — Stored `δ = q_hi − q_lo` and `ρ = q_hi/q_lo` between two runs on identical inputs, with
  validity scope; a finding, not a prediction. *(Inferred)*
- **Approximation/Model Management Optimization (AMMO)** — NASA (Alexandrov & Lewis) framework using corrected
  low-fidelity models inside a trust region with first-order consistency to the high-fidelity model. *(Verified,
  [S18])*
- **Co-Kriging (Kennedy–O'Hagan)** — Autoregressive multifidelity Gaussian process: `q_hi = ρ·q_lo + δ(x)` with a
  learned discrepancy and posterior variance. *(Verified existence [S17]; form Inferred)*
- **Filtering / fusion / adaptation** — Peherstorfer et al.'s three multifidelity model-management strategies.
  *(Flagged taxonomy; [S15])*
- **analysis_confidence (NeuralFoil)** — Classifier of XFoil convergence with a Mahalanobis-distance penalty so it
  tends to zero off-distribution. *(Verified, [S1])*
- **Out-of-distribution (OOD)** — Inputs outside the surrogate's training support (geometry, Re, α); the dominant
  failure class in 2024–2026 surrogate literature. *(Verified, [S34])*
- **Physical fidelity (ChatCFD)** — Metric of whether an executed CFD case is scientifically meaningful, distinct from
  execution success. *(Verified, [S28])*
- **Structured outputs / strict tool use** — Constrained decoding guaranteeing schema-valid model output; excludes
  numeric bounds. *(Verified, [S7])*
- **MCP tool annotations** — `readOnlyHint`, `destructiveHint` etc.; hints that clients MUST treat as untrusted.
  *(Verified, [S9])*
- **Design-rule check (DRC)** — Rule-based validation with per-rule severity, per-violation exclusion and navigable
  violations. *(Verified, [S46])*
- **Numeral check** — Product rule: every numeral in assistant prose must exist in the packed context. *(Internal,
  [S52])*
- **ISolverBackend façade** — Proposal's single interface over estimator/VLM/CFD tiers; must expose capability flags
  and method version. *(Internal, [S52])*
- **Embarrassingly parallel sweep** — A sweep whose (V, α) samples are independent jobs with no shared state.
  *(Internal, [S52])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | Sharpe & Hansman, "NeuralFoil: An Airfoil Aerodynamics Analysis Tool Using Physics-Informed Machine Learning", arXiv 2503.16323 (HTML full text) | primary | https://arxiv.org/html/2503.16323 | 2026-09-20 | Training set, envelope, errors, confidence definition |
| S2 | NeuralFoil repository (peterdsharpe) | primary | https://github.com/peterdsharpe/NeuralFoil | 2026-09-20 | MIT licence, inputs, model sizes, dependencies |
| S3 | Anthropic C# SDK repository | primary | https://github.com/anthropics/anthropic-sdk-csharp | 2026-09-20 | Official status, MIT, features |
| S4 | NuGet `Anthropic` package | primary | https://www.nuget.org/packages/Anthropic | 2026-09-20 | v12.49.0, 2026-09-18, TFMs |
| S5 | MCP C# SDK | primary | https://github.com/modelcontextprotocol/csharp-sdk | 2026-09-20 | Apache-2.0, packages, Microsoft collaboration |
| S6 | MCP Rust SDK (`rmcp`) | primary | https://github.com/modelcontextprotocol/rust-sdk | 2026-09-20 | Apache-2.0, transports, spec target |
| S7 | Anthropic docs — Structured outputs | primary | https://platform.claude.com/docs/en/build-with-claude/structured-outputs | 2026-09-20 | Schema guarantees and unsupported features |
| S8 | MCP specification (latest = 2026-07-28) | standard | https://modelcontextprotocol.io/specification/latest | 2026-09-20 | Primitives, trust and safety principles |
| S9 | MCP specification — Tools | standard | https://modelcontextprotocol.io/specification/2026-07-28/server/tools | 2026-09-20 | Tool schema, annotations, errors, security |
| S10 | W3C PROV-DM | standard | https://www.w3.org/TR/prov-dm/ | 2026-09-20 | Provenance vocabulary |
| S11 | OpenMDAO basic user guide | primary | https://openmdao.org/newdocs/versions/latest/basic_user_guide/basic_user_guide.html | 2026-09-20 | Problem/Group/Component, derivatives, drivers |
| S12 | OpenMDAO repository | primary | https://github.com/OpenMDAO/OpenMDAO | 2026-09-20 | Apache-2.0 |
| S13 | OpenMDAO case recording docs | primary | https://openmdao.org/newdocs/versions/latest/features/recording/index.html | 2026-09-20 | SqliteRecorder / CaseReader |
| S14 | Martins & Lambe, "Multidisciplinary design optimization: A survey of architectures", AIAA J. 2013 (Semantic Scholar metadata) | secondary (metadata) | https://api.semanticscholar.org/graph/v1/paper/DOI:10.2514/1.J051895 | 2026-09-20 | Existence; content from recall (Flagged) |
| S15 | Peherstorfer, Willcox, Gunzburger, "Survey of multifidelity methods…", SIAM Review 2018 (metadata; SIAM page 403) | secondary (metadata) | https://api.semanticscholar.org/graph/v1/paper/DOI:10.1137/16M1082469 | 2026-09-20 | Existence; taxonomy from recall (Flagged) |
| S16 | Forrester, Sóbester, Keane, "Multi-fidelity optimization via surrogate modelling", Proc. R. Soc. A 2007 (metadata; RSP page 403) | secondary (metadata) | https://api.semanticscholar.org/graph/v1/paper/DOI:10.1098/rspa.2007.1900 | 2026-09-20 | Existence |
| S17 | Kennedy & O'Hagan, "Predicting the output from a complex computer code when fast approximations are available", Biometrika 2000 (metadata) | secondary (metadata) | https://api.semanticscholar.org/graph/v1/paper/DOI:10.1093/biomet/87.1.1 | 2026-09-20 | Existence |
| S18 | NASA NTRS: Alexandrov et al. 2000 (20000097390); Alexandrov & Lewis 2000 (20040086473) | primary | https://ntrs.nasa.gov/api/citations/search?q=%22model%20management%22%20variable-fidelity%20Alexandrov | 2026-09-20 | AMMO definition, first-order consistency |
| S19 | Martins & Ning, *Engineering Design Optimization*, CUP 2022 (free PDF/HTML) | primary | https://mdobook.github.io/ | 2026-09-20 | Textbook reference for surrogate/MDO chapters |
| S20 | Dakota repository | primary | https://github.com/snl-dakota/dakota | 2026-09-20 | LGPL-2.1-or-later, v6.24, interfaces |
| S21 | preCICE | primary | https://precice.org/ | 2026-09-20 | LGPL-3.0, adapters, v3.4.1 |
| S22 | AeroSandbox repository | primary | https://github.com/peterdsharpe/AeroSandbox | 2026-09-20 | MIT, analysis classes, Opti |
| S23 | OpenVSP LICENSE (NOSA 1.3) | primary | https://github.com/OpenVSP/OpenVSP/blob/main/LICENSE | 2026-09-20 | Licence obligations |
| S24 | OSI — NASA Open Source Agreement 1.3 | primary | https://opensource.org/license/nasa1-3-php | 2026-09-20 | OSI approval status |
| S25 | OpenVSP wiki — VSPAERO tutorial | primary | https://openvsp.org/wiki/doku.php?id=vspaerotutorial | 2026-09-20 | Invocation, files, results handling |
| S26 | arXiv API listing: OpenFOAM AND "large language model" (13 results, 2024-08 → 2026-09) | primary (listing) | http://export.arxiv.org/api/query?search_query=all:OpenFOAM+AND+all:%22large+language+model%22&max_results=15&sortBy=submittedDate&sortOrder=descending | 2026-09-20 | Agent papers and reported rates |
| S27 | Yue et al., "Foam-Agent", arXiv 2505.04997 | primary | https://arxiv.org/abs/2505.04997 | 2026-09-20 | 88.2 % / 62.5 %, MCP, repair loop |
| S28 | Fan et al., "ChatCFD", arXiv 2506.02019 | primary | https://arxiv.org/abs/2506.02019 | 2026-09-20 | Execution vs physical fidelity, cost |
| S29 | Shi et al., "What Do CAE Simulation Agents Really Need Beyond a Generic Harness?", arXiv 2609.03718 | primary | https://arxiv.org/abs/2609.03718 | 2026-09-20 | Harness ablation figures |
| S30 | Pandey et al., "OpenFOAMGPT", arXiv 2501.06327 | primary | https://arxiv.org/abs/2501.06327 | 2026-09-20 | RAG, human oversight, drift |
| S31 | arXiv API listing: airfoil AND diffusion AND generative (12 results, 2025-08 → 2026-09) | primary (listing) | http://export.arxiv.org/api/query?search_query=all:airfoil+AND+all:diffusion+AND+all:generative&max_results=12&sortBy=submittedDate&sortOrder=descending | 2026-09-20 | Generative airfoil models |
| S32 | arXiv API listing: Text2CAD / text-to-CAD / CadQuery (12 results, 2026-06 → 2026-09) | primary (listing) | http://export.arxiv.org/api/query?search_query=all:%22Text2CAD%22+OR+all:%22text-to-CAD%22+OR+all:%22CadQuery%22&max_results=12&sortBy=submittedDate&sortOrder=descending | 2026-09-20 | Text-to-CAD metrics and failure findings |
| S33 | Khan et al., "Text2CAD", NeurIPS 2024, arXiv 2409.17106 | primary | https://arxiv.org/abs/2409.17106 | 2026-09-20 | Dataset and generation target |
| S34 | arXiv API listing: airfoil AND surrogate AND "out-of-distribution" (8 results, 2024-03 → 2026-08) | primary (listing) | http://export.arxiv.org/api/query?search_query=all:airfoil+AND+all:surrogate+AND+all:%22out-of-distribution%22&max_results=10&sortBy=submittedDate&sortOrder=descending | 2026-09-20 | OOD findings, ML4CFD competition |
| S35 | Pfaff et al., "Learning Mesh-Based Simulation with Graph Networks", ICLR 2021, arXiv 2010.03409 | primary | https://arxiv.org/abs/2010.03409 | 2026-09-20 | MeshGraphNets claims |
| S36 | Bonnet et al., "AirfRANS", NeurIPS 2022 D&B, arXiv 2212.07564 | primary | https://arxiv.org/abs/2212.07564 | 2026-09-20 | Dataset tasks, CC BY-NC-SA 4.0 |
| S37 | NVIDIA PhysicsNeMo repository | primary | https://github.com/NVIDIA/physicsnemo | 2026-09-20 | Apache-2.0, model families, CUDA |
| S38 | Chen & Fuge, "BézierGAN", arXiv 1808.08871 | primary | https://arxiv.org/abs/1808.08871 | 2026-09-20 | Latent airfoil parameterization |
| S39 | Lakshminarayanan et al., "Deep Ensembles", NeurIPS 2017 (metadata) | secondary (metadata) | https://api.semanticscholar.org/graph/v1/paper/arXiv:1612.01474 | 2026-09-20 | Existence of the UQ baseline |
| S40 | salsa repository | primary | https://github.com/salsa-rs/salsa | 2026-09-20 | Incremental computation model, licence |
| S41 | BLAKE3 repository | primary | https://github.com/BLAKE3-team/BLAKE3 | 2026-09-20 | Licences, properties, bindings |
| S42 | Blake3.NET repository | primary | https://github.com/xoofx/Blake3.NET | 2026-09-20 | BSD-2-Clause, managed/native packages |
| S43 | sqlite-vec repository | primary | https://github.com/asg017/sqlite-vec | 2026-09-20 | Licence, pre-v1 status, bindings |
| S44 | R3 repository | primary | https://github.com/Cysharp/R3 | 2026-09-20 | MIT |
| S45 | CliWrap repository | primary | https://github.com/Tyrrrz/CliWrap | 2026-09-20 | MIT, cancellation, streams |
| S46 | KiCad 9.0 PCB editor manual — DRC | primary | https://docs.kicad.org/9.0/en/pcbnew/pcbnew.html | 2026-09-20 | DRC model |
| S47 | ParaView User's Guide — Saving results (state files) | primary | https://docs.paraview.org/en/latest/UsersGuide/savingResults.html | 2026-09-20 | State-file provenance behaviour |
| S48 | Snakemake repository and CLI docs (`--rerun-triggers`) | primary | https://github.com/snakemake/snakemake ; https://snakemake.readthedocs.io/en/stable/executing/cli.html | 2026-09-20 | MIT; rerun triggers |
| S49 | Dagster repository | primary | https://github.com/dagster-io/dagster | 2026-09-20 | Apache-2.0; asset lineage |
| S50 | Zoo — Text-to-CAD / Zookeeper | vendor | https://zoo.dev/text-to-cad | 2026-09-20 | Commercial text-to-CAD, API |
| S51 | CFD-Workbench specification (A3, A6, A7, ANA-06/07, AI-01–06, CLI-01, COMMIT-01–04) | internal | docs/specs/cfd-workbench.md | 2026-09-20 | Requirements this file must serve |
| S52 | Proposal sequence snapshot (§3.2, §4.2, §7.3, §8, §9) | internal | docs/knowledge/sources/proposal-sequence.md.txt | 2026-09-20 | Content-hash sidecar, numeral check, façade, queue |
| S53 | Bench gap register (GAP-06, GAP-07, GAP-08, GAP-13) | internal | docs/knowledge/sources/bench-gap-register.md.txt | 2026-09-20 | Gaps this file addresses |
| S54 | CFD-Bench and proposal grounding | internal | docs/knowledge/cfd-workbench-grounding.md | 2026-09-20 | Reconciliation register, AR definition |

**Not opened this session (Flagged, recall only):** System.Reactive (MIT), petgraph (MIT/Apache-2.0), tokio (MIT), TPL
Dataflow (MIT, dotnet/runtime), SQLite FTS5 (public domain), Nextflow (Apache-2.0), Prefect (Apache-2.0), Grasshopper
dataflow model, Onshape AI Advisor (404 at the guessed URL), Autodesk Fusion manufacturing checks and AI features,
XFLR5 (GPL; binary project), ParaView licence (BSD-3-Clause), KiCad licence (GPL-3.0), Peherstorfer taxonomy wording,
Martins & Lambe architecture classes, Kennedy–O'Hagan model form, deep-ensemble method details.
