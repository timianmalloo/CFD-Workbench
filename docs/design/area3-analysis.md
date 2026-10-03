---
id: design-area3-analysis
title: "Design: Area 3 — Analysis (local tiers): the section (2D) tier, the VLM + strip (3D) tier, the Run manifest and the CAD ↔ Analysis toggle"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — Area 3 (Ruling 60, lane F2; documents only; architecture increment M3)
tags: [analysis, vlm, strip-theory, polar, estimator, run-manifest, run-key, freshness, loads, cavitation, depth, data-model, area-3, m3, fluids-f2]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: architecture-application, rel: refines }
  - { to: decision-freshness-by-run-key, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-application-stack, rel: depends-on }
  - { to: design-app-shell, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: kb-hw-low-order-hydrodynamics, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: decision-seven-areas, rel: relates-to }
  - { to: mockup-m12b2-views, rel: relates-to }
  - { to: mockup-status-bar, rel: relates-to }
  - { to: mockup-area3-analysis, rel: relates-to }
  - { to: note-area3-analysis-reading-contract, rel: relates-to }
  - { to: review-area3-analysis-personas, rel: tested-by }
  - { to: note-area3-fixture-arithmetic, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-03
summary: >-
  Detailed design of spec Area 3: Design revision + Operating point → Analysis run on the local tiers. The data model
  comes first: the Analysis run is an immutable fact (one evaluation of one Surface revision at one Operating point by
  one method version under one settings hash), Strip loads are its child facts, wing totals, Trefftz quantities and
  freshness are derived. Analysis reads the accepted revision by identity through the session snapshot and one new Core
  read built on the placement rule, never a draft. The estimator and VLM + strip tiers are own code in process; the
  polar tier is DR-ANA-1. Includes the fixture suite with mutants, rings and costs, a story-to-test matrix, the toggle
  contract, telemetry and fourteen decision requests.
---

# Design: Area 3 — Analysis (local tiers)

- **Status:** In review, revision 3 (repair cycle 2 of 2: the hydrodynamicist's and test architect's rev 2 findings
  folded, §17). Documents only (Ruling 60 item 4: "F2 the Area 3
  Analysis design, documents only"). Nothing here is built until the operator approves the mockup
  (`docs/mockups/area3-analysis.html`) and rules the DR-ANA batch (§15).
- **Spec:** [spec rev 1.6](../specs/cfd-workbench-v1.md) A2 row 3 (:170), A3.1 rows Analysis run · Run manifest ·
  Strip load · Polar sample · Operating point · Water record (:232–247), A3.2–A3.4, A5.1–A5.7 (:889–992), ANA-01–11,
  ANA-15–16, ANA-18–23 (:1153–1195), A7, A8.1, A8.4, A8.5, B5 (Flow F4), C2 (charts, colormaps, hard-state strings).
- **Architecture:** [application.md](../architecture/application.md) §3 (worker reserved for solvers), §4 (Core has no
  subprocess; "Analysis owns immutable method results"), §8 row **M3 admitted local analysis**.
- **Grounding traversal (V15):** `design-area3-analysis` → implements `spec-cfd-workbench-v1` → depends-on
  `decision-freshness-by-run-key`, `kb-hw-low-order-hydrodynamics` (07), `adr-0010-one-placement-rule`,
  `design-app-shell` (§3.6 selection, §6.7 modes) → `design-language` (COPY-42…72, status strip V2).
- **Author lens:** Orchestrator in Peer Mode; five adversaries at §17.

## 0. What the operator will see, and what not

**At the end of slice A3a (§16), packaged macOS app.** Open the Example foil. Press **Analysis** in the CAD | Analysis
toggle (one action; camera, selected station and layout stay). A **conditions band** appears at the top of the model
area: Speed, Water (salt · 15 °C), Depth h_ref, Incidence α, **Evaluate**, then the derived q, Re_ref, h/c, Fr_h and σ
with units. Press **Evaluate**. The views gain layers: on the Plan, the circulation per strip as batlow strips and the
loading curve; in 3D, one lift arrow per strip, the total at the centre line and the root-moment arc, the free-surface
line and the tip depth. Properties shows the wing result (CL, CDi, computed e, L, D_i, Total drag Unavailable with its
missing parts) under the tier chip **VLM + strip · local calculation** and COPY-63, with the method envelope, the depth
basis and the omissions. The bottom panel has **Spanwise loading**, **Loads** (with **Structural: Not assessed** and
COPY-60 verbatim), **Provenance** (the Run manifest). Edit a point in CAD, return: the layers carry
**Historical — geometry changed (r4 → r5)** until the next Evaluate.

**Not yet (named per slice in §16):** profile drag, Cd, Cp on the section and the cavitation screen need a polar or a
panel method (DR-ANA-1, DR-ANA-2) — each reads **Unavailable — <reason>** and Total drag names "profile" among its
missing parts; Find operating α (ANA-05, A3c); Compare and Discrepancy records (ANA-06, A3d); Goal-state operating
points (Area 1 does not exist yet — DR-ANA-9); the free-surface correction layer (A5.2, A3c); the assistant entry (M5).

## 1. Grounding — what this design must satisfy (quoted)

| Source | Statement this design must satisfy |
|---|---|
| A2 :170 | "**3 · Analysis** · Design revision + Operating point → Analysis run (local tiers) · section (2D) and wing (3D) results with every label, basis and omission visible; force, loading and pressure drawn on the geometry; CAD ↔ analysis toggle preserves selection and camera; VLM + strip fixture suite green" |
| A2 :162 | areas are discrete: "no two areas edit the same object and every hand-off is a reference by identity" |
| A2 :189 (non-goal) | "linking VTK, XFOIL, XFLR5 or OpenFOAM into the process; averaging across analysis tiers; … Oswald efficiency as a user input" |
| A3.1 Analysis run | "entity, immutable · One evaluation of one Surface revision at one Operating point by one method + version, with its Run manifest and evidence" |
| A3.1 Run manifest | input ids and hashes, Water record, Operating point, reference quantities, method id + version, settings hash, reconciliation tolerance, DRC outcomes, output hashes, wall time, platform; "**The run key has one definition:** BLAKE3 over (Surface revision hash, the referenced Profile revision hashes, Water record, Operating point, method id + version, settings hash)" |
| A3.1 Strip load | "fact · One strip of one VLM + strip run: y, chord, Cl_local, α_eff, Re_local, force and moment components; owned by the Analysis run" |
| A3.2 | "A result identifies, by hash, every input and the method version that produced it; freshness is derived by run-key equality, never written" |
| A3.3 | Strip load "forces are additive across strips within one run only, never across attempts or tiers" |
| A3.4 | Current iff the run key equals the key recomputed; "Undo to an identical definition makes a run Current again; save and reopen leaves freshness unchanged" |
| A5.6 :958–963 | per-tier "May claim", "Fixed label parts", "Omissions always listed" (§5.4) |
| A5.6 :965 | section results per span (N/m, lowercase); wing results N (uppercase, "Wing only", S_ref and force axes named); Total drag Unavailable when a component is missing |
| ANA-04 :1156 | the VLM fixture rows (quoted in §13.2) |
| ANA-22 | toggle is one action; camera, selection, station and viewport size preserved; layers draw over the **accepted** revision, never the preview |
| A7 :1226 | "Verified numerical implementation" is granted per method by "its ring-0 fixture suite (A8.4) observed red then green on both platforms" |
| A8.1 Observability | `analysis.run` (tier, key, duration, outcome, confidence) on the normal path; toggle p95 ≤ 250 ms is a product target, not an M1 gate |
| A8.4 | analytic oracles; observed-order tests in ring 0; tolerance class 10⁻¹² relative for every hydrodynamic output; run key bitwise |
| A8.5 | XFOIL, XFLR5, AVL GPL and OpenVSP NOSA: process-only or excluded; "A NeuralFoil sidecar pulls CasADi (LGPL-3.0) and IPOPT (EPL-2.0) and needs its own review before it ships" |
| Architecture §4 | "Core has no Avalonia, filesystem, subprocess, network or UI-thread dependency" |
| Ruling 60 (4) | "F2 the Area 3 Analysis design, documents only" |

**Drift and findings surfaced at grounding and at the gate (each carried to §15 or §14):**

- **G-1 (Verified, `AuthoringSession.cs`:527–537).** `ProfileAt(i)` **may** return the draft's profile: when a profile
  draft is open on that assignment it reads `draft.Bytes`, otherwise `CurrentBytes`. ANA-22 forbids analysis over a
  preview, so Analysis never calls `ProfileAt`; it reads the accepted bytes from `Snapshot()` (`SnapshotCore`,
  :1583–1588: `CurrentBytes`, `AcceptedId`, `SurfaceHash`; the draft is a separate field).
- **G-2 (Verified, `Placement.cs`:35–92, 153–303).** `PlacementRule` (incl. `Place`) and `ProfileEvaluator` are
  `internal`; the public reads are `Placement.Surface` and `Placement.Frame`, and `Frame` re-parses the source per call
  (:300). Analysis must neither re-implement the §6 placement nor Rule A: §4 names the one Core addition.
- **G-3 (Verified, `Placement.cs`:157, 206, 263–271).** `ChannelEvaluations`, `ProfileEvaluations` and `AfterStation`
  are process-global statics written with `++`; a worker-thread analysis races them (FM-12).
- **G-4 (Verified, `WorkbenchController.cs`:30, 161–162, 219–314).** Cameras and `Selection` are controller session
  values, not view instances, so the toggle preserves them by not touching them.
- **G-5 (Verified, `PropertiesView.cs`:7–21).** `ShellMode` and `Mode` duplicate one concept (SPEC-B shape). Analysis is
  added to one enum only; the duplicate is a finding for the Desktop owner.
- **G-6 (Verified, `AuthoringSession.cs`:1764–1772).** The native reader rejects unknown members (`Exact`, before the
  `"cfdw-project-1"` check) — persisting runs is a format expansion (§3.6).
- **G-7 (Verified by search, 2026-10-03).** No polar data, water table, analysis code, `cases/` or `schemas/` exist. The
  ITTC water values exist in `docs/knowledge/hydrofoil-workbench/data-and-constants.md`:142–146.
- **G-8 (spec gap).** The Analysis verb row (B1) has no verb that computes; F4 node S says "Historical banner;
  recompute". → DR-ANA-6.
- **G-9 (spec gap).** No 2D Cp source exists outside the polar tier, yet ANA-02/ANA-21 need Cp on the profile. → DR-ANA-2.
- **G-10 (spec drift, from the CFD and hydrodynamics lenses; Inferred by their independent re-runs).** ANA-04's elliptic
  row ("CL within 1 % of lifting line inside the Helmbold–Prandtl band") cannot be met by a correct lattice: a VLM
  converges to the lifting-surface answer (re-run: CL 0.4197 at 32 × 6, 0.4167 at 128 spanwise, AR 8, α 5°; lifting
  line 0.4386, Helmbold 0.4282). ANA-04's flat-plate row uses Helmbold, an elliptic-planform formula, on a rectangular
  plate (re-run: −0.85 % to −0.96 %, growing with refinement). → DR-ANA-12.
- **G-11 (spec conflict, hydrodynamics lens).** A5.6's fixed VLM label contains "deep water"; A5.1/ANA-19 say "with
  depth unset … no deep-water value or label is printed". A5.6 :962 also says a correction layer "never changes this
  label", so DR-ANA-13 (a) is a **spec amendment**, requested of the spec owner in §15 — not a design-side reading.
- **G-12 (copy drift, hydrodynamics lens; Verified).** A7 writes "Computed estimate · model uncertainty not
  quantified"; DESIGN.md COPY-63 capitalises "Model". DESIGN.md is the copy authority (C2); the spec owner reconciles.
- **G-13 (Verified, `tools/run-tests.sh`:36–54).** Every harness launches at once (no throttle). A fifth harness runs
  concurrently with Desktop, the critical path (§13.4).

## 2. Responsibility and phasing

**One responsibility:** given an accepted Design revision (by identity) and an Operating point, produce an immutable,
fully labelled Analysis run on a local tier, record it, and present it over the same geometry with its basis, envelope
and omissions. **Not** this component's job: editing geometry (CAD), authoring a Goal state (Setup), sweeps (Experiment,
Run), field evidence (Results).

**Boundaries.** In: `SessionView` (accepted bytes + `AcceptedId` + `SurfaceHash`), the Water table, the user's
Operating point. Out: `AnalysisRun` facts appended to the document; projections for views, panes and the status strip.
Owned: Analysis runs, Strip loads, Polar samples (cache facts), Discrepancy records (A3d). Borrowed read-only by hash:
the Design, Surface and Profile revisions.

**Phasing (architecture §8 row M3), slices in §16.** Mock-substitutable seams: `IPolarSource` (Unavailable stub until
DR-ANA-1 lands), `IWaterTable` (pinned rows in tests), `IAnalysisClock`, and an `IEvaluationBarrier` test seam for the
cancel/supersede tests (§13.3).

## 3. Data model (settled first — DM1–DM18)

### 3.1 Bounded context and ubiquitous language

Context **Analysis** (A3.1). Spec terms are used as defined: Operating point, Water record, Analysis run, Run manifest,
run key, Strip load, Polar sample, Discrepancy record, Current, Historical, Undefined, Unavailable, tier, method id +
version, settings hash, reference quantities, datum. Proposed glossary rows:

| Term | Kind | Meaning |
|---|---|---|
| **Run outcome** | value | Completed · Failed(code, reason). Cancelled is a telemetry outcome, never a stored row |
| **Method envelope** | value, per method version | the input range inside which the method's claims hold (VLM: \|α_eff − α_L0\| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°; §5.4) — a property of the method record, not of the design |
| **Lattice** | derived | the VLM panelling of one Surface revision under one settings value; rebuilt on read, never stored |
| **Section sample** | derived | the Rule A section at one η from Core: placed camber points, normalised camber, thickness and camber slope (§4); never stored |
| **Pending operating point** | session value | what the conditions band holds between an edit and Evaluate; not document data |

### 3.2 Aggregates, roots and the one invariant each protects

| Aggregate root | Owns | Invariant | Referenced by identity |
|---|---|---|---|
| **Analysis run** | Run manifest (value), outcome, solver diagnostics, Strip load facts, content hash | every output resolves by hash to every input and the method version; nothing on the row changes after it is recorded | Design revision (`AcceptedId`), Surface revision (`SurfaceHash`), Profile revisions, Polar samples (by grain key) |
| **Polar sample** (cache fact) | Cl, Cd, Cm, x_tr, Cp_min + station count, confidence, converged, Cp distribution hash | one row per grain key; a repeat evaluation is a read | Profile revision (hash) |
| **Discrepancy record** (A3d) | δ, ρ, uncertainties | references two evaluations, never the reverse; re-comparing a key is a read | two Analysis runs or two Polar samples |

One aggregate per transaction (DM2): recording a run appends one row with its strips; Apply in CAD touches no run.

### 3.3 Grain, additivity, history rule (grain before columns)

| Fact | Grain — "one row is exactly one …" | Identified by · recorded when | Measures and class |
|---|---|---|---|
| **Analysis run** | evaluation attempt of one Surface revision (with its Profile revisions) at one Operating point by one method id + version under one settings hash | `runId` (UUID v4); `runKey` (§3.4) — at most one **Completed** row per key (store invariant); Failed rows may repeat · recorded when the method returns | solver residual ‖AΓ − b‖∞, condition estimate κ₁, wall time — **non-additive**. Everything else on a run is derived (§3.5) |
| **Strip load** | spanwise lattice row j of one Completed VLM + strip run | (`runId`, `j`), j contiguous 0…n−1 · with its run | y, η, chord, Γ (strip total), α_i, α_eff, Re_local, Cl_local, cd_profile at Ncrit 2 and at Ncrit 4 (each or Unavailable + reason), near-field force (Fx, Fy, Fz) and moment (Mx, My, Mz) about the frame origin in **body axes**, Trefftz downwash w_T, envelope flag. Forces and moments **additive across strips within one run only**; the rest **non-additive** |
| **Polar sample** | (Profile revision hash, method + version, Re, Ncrit, surface state, α, Water record) evaluation (A3.3) | its grain key, unique · when the method returns | Cl, Cd, Cm, x_tr upper/lower, Cp_min (+ N stations), confidence or "not recorded", converged — **non-additive** |
| **Section result** | not a fact: an Analysis run of a section method at one station η (an Operating-point attribute), referencing its Polar samples (both Ncrit) by key | as Analysis run | as Polar sample |

**History rule per attribute.** Every attribute of a run, strip and polar sample is immutable (a change is a new row).
Inputs whose change rewrites a past run's meaning — Surface/Profile revision, evaluator id + version, placement-rule
version, Water record, Operating point, method version, every setting (lattice, singularity cutoff, wake direction,
Ncrit pair, surface state, TE floor) — are in the run key, so a change makes a new run and the old one reads Historical.
**Type-1 by decision (history discarded, recorded):** display units (ANA-18), the named attachment point (moments about
it are derived on read), layer visibility, chart choice, the reconciliation tolerance (it judges, it does not compute —
recorded in the manifest, outside the key).

**Selected run (derived).** For (tier, scope) the views show the **latest Completed run in document order** (array
order, never wall time); if its key differs from the current key it is Historical. A Failed latest attempt shows its
error beside the previous Completed run as Historical (F4 node O).

### 3.4 Run key and freshness (one definition — cited, not restated)

The run key is the A3.1 Run-manifest row's definition, bound to existing code: BLAKE3 (`Identity.Blake3`,
`Identity.cs`:117) over RFC 8785 JCS (`Jcs.Write`, used for `SurfaceHash` at `FoilSource.cs`:25) of `{surface:
SurfaceHash, profiles: [ProfileIdentity per assignment, in order], water, op, method: {id, version}, settings:
settingsHash}`. The Surface-revision hash is taken **with** its evaluator id + version and placement-rule version
(they are part of what "the Surface revision evaluates to"); `settingsHash` = BLAKE3(JCS(settings)) and settings include
every numeric choice (§5.6). Freshness = key equality; nothing writes a freshness flag (decision-freshness-by-run-key).
"What changed" (COPY-64) is derived by comparing manifest fields with the current inputs.

**Two guards against silent key or output drift (from the test and CFD lenses):** a **pinned run-key vector** (one
manifest → one hex key, committed) catches any JCS field or order change that would turn every saved run Historical
after an upgrade; a **pinned output hash per method version** (the Example foil at the default settings) goes red when
outputs move without a method-version bump.

*assume:* `SurfaceHash` already covers the profiles, so listing profile hashes again is redundant but keeps the spec's
definition literal. Confirm by a test that edits one profile coordinate and asserts both hashes change.

### 3.5 Derive, don't store (DM7)

Derived on read: q, Re_ref, Re(y), h(y) = h_ref − (z(y) − z_datum) after the α rotation, h/c, Fr_h, σ(y), V_crit; wing
CL, CDi, L, D_i, side force, total moment, centre of lift, root and integrated bending moments, moment about the named
attachment point (M_P = M_O + (O − P) × F), computed e, S_ref, b, AR (b²/S), wing loading; **the Trefftz quantities**
L_T = ρV ΣΓ_jΔy_j and D_i,T = ½ρ ΣΓ_j(−w_T,j)Δy_j (one function, from stored Γ and w_T — the data lens showed storing
them gave two homes); panel count (from settings); the lattice; the Section sample; selected run; Current/Historical;
the envelope verdict per strip; the omissions list. Stored: only facts and solver diagnostics. Materialised caches: none.

### 3.6 Durable representation, integrity and migration (expand-migrate-contract)

- **Representation (DR-ANA-4 (a)).** Runs are append-only facts in the native project: a new optional top-level
  `analysis` member `{ runs: [...], polarSamples: [...], pruned: [...] }`, written only when at least one run exists,
  under format `cfdw-project-2`. The format string is derived in **one function** from the run count; the member carries
  `WhenWritingNull`, so a document with no run is written as `cfdw-project-1`, **byte-identical to today** (test).
- **Store invariants, enforced in `NativeProject.Check` on read and in `RecordRun` under the session lock (check and
  append in one step):** `runId` unique; at most one Completed row per `runKey`; strips contiguous 0…n−1 and n ≤ 2,048
  (a settings-validation cap); polar grain key unique; every hash `[0-9a-f]{64}`; numbers finite; line ≤ 4096 bytes.
- **Integrity per run.** Each run carries `contentHash` = BLAKE3(JCS(the row without `contentHash`)). On read a mismatch
  makes **that run** "Unavailable — run payload failed its check" (never Current, never deleted); the stored `runKey` is
  **recomputed from the manifest and never trusted** — a stored key that disagrees is the same Unavailable.
- **Expand:** the reader accepts `-1` and `-2`. **Migrate:** none (`-1` documents have no runs; DM16 — no backfill).
  **Contract:** none; `-1` stays readable indefinitely.
- **Rollback (data lens, condition 1).** An older build opening a `-2` file fails closed with COPY-130 before it can
  write (G-6). The first save that turns a `-1` file into `-2` first writes `<name>.v1.bak` (flushed to disk), never
  overwrites an existing `.bak`, then replaces the document by the store's existing save path (`ProjectStore`,
  `CfdWorkbench.Persistence`; *assume:* its replace is atomic as the app-shell design states — confirm in the build
  track; if not, this step makes it so). Test: today's reader opens the `.bak` and it round-trips byte-equal.
- **Writer and compute reader per field (DM15):** writer `AnalysisRecorder` via `AuthoringSession.RecordRun`; readers:
  freshness (`RunKey.Recompute`), `AnalysisProjection`, Compare (A3d), CLI `inspect --runs`.

### 3.7 Change-surface list (E7) — written before the work

| Surface | What must reach it |
|---|---|
| Store | `cfdw-project-2` `analysis` member; reader/writer; invariants; per-run content hash; `.v1.bak`; size preflight |
| Domain (`CfdWorkbench.Analysis`) | `OperatingPoint`, `WaterRecord`, `RunManifest`, `AnalysisRun`, `StripLoad`, `PolarSample`, `RunKey`, `Tier`, `MethodRecord` (id, version, envelope, convergence order), `Settings`; `Estimator`, `VortexLattice`, `StripCoupler`; `IPolarSource` |
| Core read (one addition) | `Placement.Sections(source, etas, xs)` (§4) |
| Session | `AuthoringSession.RecordRun`, `ReadRuns`; runs in `Envelope`; dirty on record; never on the undo stack |
| Projection | `AnalysisProjection.Build(run, current inputs, units) → AnalysisViewModel` — every rendered string |
| Client type (Desktop) | `ShellMode.Analysis` (one enum, G-5); `LayerSet`; conditions band; Layers pane; bottom-panel tabs |
| UI | layers on Plan/3D/Side/Front; Properties analysis groups; status strip; toggle command |
| CLI | `cfdw analyse <file> --op <json>`, `cfdw inspect --runs` (CLI-01 run-key equality) |
| Compute reader | freshness on every projection rebuild; Compare (A3d) |
| Telemetry | `analysis.run`, `analysis.toggle`, `analysis.project` (§11) |
| Copy | new rows (§12.6) into DESIGN.md §7 on mockup approval |

## 4. The reading contract — Analysis never edits the revision

1. **Identity first.** Analysis calls `AuthoringSession.Snapshot()` once per evaluation and keeps `(AcceptedId,
   SurfaceHash, SourceHash, Source)`: the **accepted** bytes, never `Draft.Bytes` (G-1). The manifest records them.
2. **No `ProfileAt`, no edit verbs.** An architecture test asserts the Analysis assembly references none of `ProfileAt`,
   `Begin*`, `Update*`, `Apply*`, `Cancel`, `Undo`, `Redo`.
3. **One placement authority (ADR-0010; geometry lens findings 1, 2, 6).** One new public Core read, implemented with
   the existing internal `PlacementRule.Section/Blend/Place` and `ProfileEvaluator.Jet` (no second evaluator):

   ```csharp
   // Core, Placement.cs — new; parses the source once. Pattern: Query (read-only projection over the record).
   public sealed record SectionSample(StationFrame Frame, IReadOnlyList<double> X,
       IReadOnlyList<double> Camber,        // (zu + zl)/2 from Section/Blend at η — normalised to local chord
       IReadOnlyList<double> Thickness,     // zu − zl = t(η)·T(x) — normalised
       IReadOnlyList<double> CamberSlope,   // analytic dz_c/dx from ProfileEvaluator.Jet (Yt/Xt), never a difference
       IReadOnlyList<Point3> PlacedCamber); // PlacementRule.Place(L, T, D, twist, x, camber) in metres — Core computes it
   public static IReadOnlyList<SectionSample> Sections(byte[] source, IReadOnlyList<double> etas, IReadOnlyList<double> xs);
   ```

   The lattice takes its panel corners from `PlacedCamber`; Analysis never evaluates X = L + c(x cos φ + z sin φ) itself,
   so the 3D layers cannot disagree with the drawn foil (GEO-B). Test: `Place(Sections)` equals `Surface`'s
   Upper/Lower midline bit for bit at the same η and xs. Quadratures over the camber split at C⁰ knots.
4. **Profile hashes** come from `AuthoredProjection.Assignments[i].ProfileIdentity` (`Contracts.cs`:36).
5. **Never writes** except `RecordRun(run)` (the Analysis aggregate).

## 5. The tiers

### 5.1 Section (2D) tier

| Method | Computes | Source | Status |
|---|---|---|---|
| `cfdw.estimator.section` v1 | thin-airfoil Cl(α) = 2π(α − α_L0) and Cm_c/4 from `CamberSlope` (Glauert integrals, Gauss–Chebyshev, split at knots); drag as the **fully turbulent bound (pessimistic): 2 C_F(Re)(1 + 2 t/c + 60 (t/c)⁴), ITTC-1957 line; no lift-dependent profile drag** | own C#, in process | specified |
| polar (XFOIL-class) | Cl, Cd, Cm, x_tr, Cp at the Ncrit pair {2, 4} and the surface state | **DR-ANA-1** | `IPolarSource` stub: "Unavailable — no polar method installed" |
| Cp on the profile and the cavitation screen | Cp(x) upper/lower, Cp_min with station count | **DR-ANA-2** | Unavailable until ruled |

**Cavitation screen (A3b, conditions from the hydrodynamics lens).** The A5.4 string verbatim (COPY-48) with N; the
margin setting (default 15 %, "practitioner assumption, not sourced"); σ evaluated at h(y) of the station whose −Cp_min
is governing; Cp_min taken at that strip's **α_eff from the wing run** (not α_geo) when the screen is wing-level; the
governing station and its depth named; −Cp_min ≤ 0 → Undefined; missing p_v or depth → Unavailable.

Section results are per span: N/m, lowercase Cl, Cd (A5.6).

### 5.2 Wing (3D) tier — VLM + strip (`cfdw.vlm-strip` v1)

- **Lattice.** Horseshoe vortices on the mean camber surface (`PlacedCamber`), both halves (full span): bound segment at
  the panel quarter chord, control point at three-quarter chord, no-penetration (V∞ + v)·n = 0; trailing legs to
  `wakeSpans` spans along +x (the wake direction is a setting). Panel normals from the cross product of the panel
  diagonals, never from an edge (a closing tip panel is a triangle with real area). A strip is excluded only when its
  control-point chord is below 10 µm, and the exclusion is listed. Spacing laws and counts: DR-ANA-7.
- **Solve.** Dense LU with partial pivoting; record ‖AΓ − b‖∞ and a 1-norm condition estimate κ₁. Near-field forces use
  a **singularity cutoff** ε = 10⁻⁸ × segment length (a setting, in the key) for collinear segments; any non-finite Γ or
  force fails closed (`ANA-NONFINITE`).
- **Forces.** Near field: Kutta–Joukowski ρ(V∞ + v) × Γℓ per bound segment → per-strip body-axis forces and moments
  about the frame origin (root LE, +x aft, +y starboard, +z up). Far field (derived): Trefftz-plane L_T and D_i,T with
  the quadrature stated — trailing vortices at strip edges, downwash evaluated at strip mid-span,
  w_T = Σ G_k/(2π) [1/(y − y_b,k) − 1/(y − y_a,k)]. CDi is the Trefftz value; the near-field value is shown beside it
  when they differ by > 5 % (lattice-quality signal). **e = CL²/(π AR CDi,T), computed, never entered.** A planar wing
  with e > 1.000 at the default lattice is a lattice finding (it converges from above, p ≈ 1 — CFD lens re-run and this
  design's own mockup probe), extending ANA-20's near-elliptic rule.
- **Strip coupling (hydrodynamics + CFD lenses).** α_i per strip from the **trailing-wake-only** downwash at the bound
  vortex (≈ w_T/2), never from the total induced velocity at the control points (which no-penetration cancels).
  α_eff = α + twist − α_i; Re_local = V c(y)/ν at strip mid-span. With a polar: cd_profile at **both** Ncrit, looked up
  at **α_eff**; the polar's Cl(α_eff) is shown against the lattice Cl_local as a per-strip consistency check; a strip
  whose Re_local is outside the polar's Re envelope (e.g. a 40 mm tip at 10 kn, Re 1.7 × 10⁵ < 2 × 10⁵) has cd
  Unavailable and is named — never extrapolated. Without a polar, cd is Unavailable and **Total drag is Unavailable**
  listing "profile". The coupling is one-way (no nonlinear lifting-line iteration, so no unphysical branch near stall).
  Separation is never claimed; a strip outside the envelope reads COPY-72 "Section-based inference" where a section
  claim would follow.
- **Loads (ANA-11).** Centre of lift, computed wing loading L/S_ref, root bending moment of the half span
  ρV∫₀^{b/2} Γ y dy and integrated bending moment (steady, n = 1, about the root plane), moment about the named
  attachment point, drag breakdown (induced + profile; Total Unavailable when a part is). **Bookkeeping check, not a
  verification** (CFD lens 4): near-field Σ strips rotated into **wind axes** vs L_T, and half-span ΣMx vs the Trefftz
  half-span root moment, normalised by L·b/2, within the manifest's reconciliation tolerance (DR-ANA-7). The verification
  is the fixture suite (§13.2).

### 5.3 Estimator tier (wing) — `cfdw.estimator.wing` v1

CL from lifting line with the Helmbold slope; CDi = CL²/(π AR) labelled **"elliptic loading assumed; planar lower bound
on induced drag"** (DR-ANA-11); kept out of the computed-e envelope check and out of Compare's e deltas; drag as the
fully turbulent bound. It exists so a wing number is available without the lattice and as Compare's second tier.

### 5.4 Labels, basis, envelope and omissions — every one visible (A5.6, A7, A5.1, A5.4)

| Tier | Tier chip | Fixed label parts (A5.6) | Method envelope (shown beside the number; strips outside flagged) | Always with | Omissions always listed |
|---|---|---|---|---|---|
| Estimator | "Estimator · local calculation" | "inviscid + turbulent-friction bound; deep water; steady" | AR ≥ 4 for Prandtl, Helmbold below; sweep ≤ 15°; attached | COPY-63; method id + version | free surface, ventilation, junctions, unsteady, tip-vortex cavitation, surface state |
| Polar | "Polar · local calculation" | surrogate name + version; confidence or "not recorded"; station count; COPY-66 when a surrogate | the polar's Re grid and converged α bracket at both Ncrit | COPY-63; COPY-44; surface state | as above plus 3D effects |
| VLM + strip | "VLM + strip · local calculation" | "attached flow; no stall; no ventilation; deep water" — with depth unset, "… free surface not modelled" in place of "deep water" (DR-ANA-13, a spec amendment) | **\|α_eff − α_L0\| ≤ 10°, Cl_local ≤ 1.0 (or the polar's converged bracket when present), quarter-chord sweep ≤ 30°**; with a polar, also Re_local inside the polar's Re range (07 §3D methods; Inferred bounds, proposed — DR-ANA-14) | COPY-63; lattice n_span × n_chord; S_ref, b, moment datum, force axes; "Wing only" | as above; separation only as COPY-72 |

**The envelope verdict is derived per strip and per run** (hydrodynamics veto, finding 1). The wing result always
carries the run verdict **in the Wing result group, on the row after CL** (rev 2 finding M-H1: it rendered only in
Labels), and the strips outside are marked in the layer; the result is never shown bare. The bound is always written in
full, every part named (rev 2 minor: the string had dropped α_L0 and the sweep limit):

- outside: "Outside the method envelope (|α_eff − α_L0| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°) — <n> of
  <m> strips; exceeded: <parts>" (new copy row, replaces rev 2's "— <bound> at <n> strips");
- inside: "Inside the method envelope (|α_eff − α_L0| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°) at all <m>
  strips" (new copy row).

With a polar the parenthesis gains "Re_local in <Re range>" and Cl_local's bound becomes the polar's bracket. COPY-49
stays the ANA-20 *catalog* finding — a different thing.

**The strip readout carries its own verdict and omissions** (rev 2 blocker B-H1). A station selected in Analysis shows
"Strip of wing run (α_eff) · η <η>" and, under its values, three things the wing result has and the strip did not:

| Row | Content | With no polar (A3a) |
|---|---|---|
| Envelope (this strip) | "Inside" or "Outside" with each part's value against its bound: "\|α_eff − α_L0\| <x>° ≤ 10° · Cl_local <y> ≤ 1.0 · sweep <z>° ≤ 30°"; an exceeded part is named first and the row reads "Outside the method envelope at this strip" | as written; Cl_local's bound is 1.0 |
| Re_local vs polar Re range | "Re_local <Re> inside <Re_min>–<Re_max>" or "Re_local <Re> outside the polar's Re range <Re_min>–<Re_max> — cd not extrapolated" (§5.2 rule) | "Unavailable — no polar method installed" (there is no range to test against; never "inside") |
| Not modelled | the VLM omissions list verbatim (free surface — or the depth basis line when depth is set —, ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as COPY-72) | same |

The strip's tier chip, COPY-63 and the fixed label (with the depth-unset form) appear as on the wing result. Tested by
`Station_StripReadout_EnvelopeVerdictPerPart`, `Station_StripReadout_ReAgainstPolarRange` and
`Station_StripReadout_NotModelledList` (§13.5).

**Depth basis on every result (ANA-19):** depth unset → σ, Fr_h, V_crit read COPY-45; h/c < 5 at any station → COPY-46
with its numbers on every low-order result; h(y) ≤ 0 → that station's estimator Unavailable and only the
surface-piercing flag; depth set and every h(y) > 0 → tip-depth margin, Fr_h and COPY-47. Undefined / Unavailable have
one rendering each (COPY-69 / COPY-70). With depth unset the VLM fixed label reads "attached flow; no stall; no
ventilation; free surface not modelled" (DR-ANA-13) — never "deep water", and COPY-46/47 do not print (there is no
depth to state). **A station selected in Analysis shows the "Strip of wing run (α_eff)" result**, labelled so,
distinct from a 2D section result at α_geo, with its own verdict and omissions (above). LAB-01 lints every string this
area renders.

### 5.5 In-process vs process boundary, per tier

| Tier | Where it runs | Licence path (COMMIT-02) | Never |
|---|---|---|---|
| Estimator (section, wing) | in process, `CfdWorkbench.Analysis`, worker thread | own code | — |
| VLM + strip | in process, own C# lattice | own code; AVL (GPL) only as an offline oracle, never linked or shipped | linking AVL, XFLR5, VSPAERO |
| Polar | DR-ANA-1: (b) in process (C# inference of an MIT network) or (c)/(d) a **child process** via CliWrap with an argv array, a run directory named by the run key, outputs read as files and hashed, the process tree killed on Cancel or timeout | process-only for GPL/LGPL | linking XFOIL, XFLR5, CasADi |
| Rendering | Avalonia drawing | — | linking VTK |

Core keeps its rule (no subprocess). A process-backed polar lives in an adapter assembly (`CfdWorkbench.Polar.Process`)
wired at the composition root, mirroring `CfdWorkbench.Persistence`.

### 5.6 Run manifest — method id, version, settings hash

```jsonc
{ "runId": "uuid", "runKey": "blake3-hex (recomputed on read, never trusted)", "contentHash": "blake3-hex",
  "outcome": "Completed" /* | { "failed": { "code": "ANA-SOLVE-RESIDUAL", "reason": "…" } } */,
  "tier": "vlm-strip", "method": { "id": "cfdw.vlm-strip", "version": "1.0.0", "order": 1 },
  "settings": { "nSpanPerHalf": 64, "nChord": 4, "spanSpacing": "cosine", "chordSpacing": "cosine",
                "wakeSpans": 20, "wakeDirection": "+x", "singularityCutoff": 1e-8, "envelope": "vlm-envelope/1",
                "polar": null /* or { "id", "version", "model" } */, "ncrit": [2, 4], "surfaceState": "clean",
                "teFloorMm": 0.3 },
  "settingsHash": "blake3(JCS(settings))",
  "inputs": { "acceptedId": "…", "surfaceHash": "…", "profileHashes": ["…"], "evaluator": "cfdw-cv/2",
              "placementRule": "foildsl-6/1" },
  "water": { "temperatureC": 15, "salinityGPerKg": 35.16504, "rho": 1026.02, "nu": 1.1892e-6, "pv": 1670.9,
             "source": "ITTC 7.5-02-01-03 Rev 03 (2024)", "tableHash": "…" },
  "op": { "speed": 5.14444, "pAtm": 101325, "hRef": 0.5, "datum": "root LE", "alphaDeg": 3.0, "load": null },
  "reference": { "sRef": 0.108, "bRef": 0.9, "cRef": 0.12, "momentDatum": "frame origin", "axes": "body; wind for lift/drag" },
  "reconciliationTolerance": 0.01, "drc": [ { "rule": "…", "version": "…", "outcome": "…" } ],
  "diagnostics": { "residualInf": 0, "kappa1": 0 },
  "strips": [ /* Strip load rows, §3.3 */ ],
  "wallMs": 0, "platform": { "os": "macOS 26", "arch": "arm64", "dotnet": "10.0.x" } }
```

`reference` records the values used (derived at run time) so a later evaluator change is visible as a manifest
difference. `teFloorMm` is copied per the A3.1 reserved-terms row. `placementRule` names ADR-0010's rule version
(*assume:* no such version string exists yet; the Core track adds a constant beside the evaluator id).

## 6. Contracts

### 6.1 Exposed

| Contract | Shape | Notes |
|---|---|---|
| `AnalysisService.EvaluateAsync(OperatingPoint, Tier, Scope, CancellationToken) → AnalysisRun` | Scope = Wing \| Station(index, η) | snapshot → compute → `RecordRun`; idempotent by run key: an existing Completed run with that key is returned |
| `AnalysisProjection.Build(AnalysisRun?, CurrentInputs, Units) → AnalysisViewModel` | pure | every rendered string comes from here |
| `AuthoringSession.RecordRun(AnalysisRun)` · `ReadRuns()` | append-only, check-and-append under the session lock | not on the undo stack; marks the document dirty; refuses after close (`DOC-CLOSED`) |
| `Placement.Sections(...)` | §4 | the one Core addition |
| CLI `analyse`, `inspect --runs` | JSON | same run key as the GUI (CLI-01) |

### 6.2 Consumed

| Contract | Source (verified) |
|---|---|
| `AuthoringSession.Snapshot() → SessionView(AcceptedId, SourceHash, SurfaceHash, Source, Draft, …)` | `AuthoringSession.cs`:25, 233, 1583 |
| `PlacementRule.Section/Blend/Place`, `ProfileEvaluator.Jet`, `Placement.Frame` | `Placement.cs`:63–90, 153–170, 298 |
| `AuthoredProjection.Assignments[i].ProfileIdentity` | `Contracts.cs`:36 |
| `Identity.Blake3`, `Jcs.Write` | `Identity.cs`:117; `FoilSource.cs`:25 |
| `WorkbenchController.Selection`, cameras | `WorkbenchController.cs`:30, 161–314; app-shell §3.6 |
| ITTC water table (new data file, hashed at load — A8.5 bundled-data row) | values `data-and-constants.md`:142–146 |

## 7. Patterns (named, justified; ladder climbed)

| Pattern | Where | Why it passes both the Patterns Expert and the Simplifier |
|---|---|---|
| **Append-only fact log** (DM5) | runs and strips | the audit trail is the data; Historical runs are the evidence ANA-07 needs |
| **Content-addressed identity** | run key, settings hash, content hash | freshness and integrity by equality; reuses `Identity.Blake3` + `Jcs` |
| **Query / read model** | `Placement.Sections`, `AnalysisProjection` | pure functions over immutable inputs; one place for every string (UI-I, UI-M) |
| **Strategy** | `IPolarSource` | two implementations on day one (the Unavailable stub is shipped behaviour) |
| **Idempotent receiver** | `EvaluateAsync`, `RecordRun` | a repeated Evaluate is a read; enforced at the store |
| **Latest-wins cancellation** | one in-flight evaluation per scope | the architecture's existing rule (§3) |
| **Anti-corruption layer** | polar adapter (if a process) | foreign files never enter the domain unparsed |

**Rejected:** a message bus or background service; a method plugin system; a separate result store; storing wing totals
or Trefftz quantities (DM7); Math.NET for the LU (*simplify:* plain dense LU; settings validation caps unknowns
(strips × chordwise panels) at 2,048 — the default is 512; upgrade trigger: `analysis.run` p95 > 1 s on the reference
fixture).

## 8. Error and concurrency model

- **Threads.** Compute on a worker; the UI thread only projects. One in-flight evaluation per scope; a new Evaluate
  cancels the older (generation token), checked per lattice row and per strip.
- **Binding.** A run computed against r4 is recorded as r4 evidence even if the user moved to r5; it shows Historical.
  The manifest carries the snapshot's `AcceptedId`/`SurfaceHash`, captured before compute.
- **Errors (stable codes).** `ANA-INPUT-*` (V ≤ 0, non-finite, water outside table — refused before compute, recorded as
  nothing), `ANA-GEOM-*` (h(y) ≤ 0 everywhere, zero area), `ANA-SOLVE-SINGULAR`, `ANA-SOLVE-RESIDUAL`, `ANA-NONFINITE`,
  `ANA-POLAR-UNAVAILABLE`, `ANA-CANCELLED` (telemetry only). Compute errors become a Failed row.
- **Idempotency.** `RecordRun` checks and appends in one step under the session lock; a Completed row with the key
  short-circuits `EvaluateAsync`. Failed rows do not block a retry.

## 9. Failure-mode analysis (mode → disposition)

| # | Mode | Disposition | Detection / test |
|---|---|---|---|
| FM-1 | Analysis reads a draft | **prevent** — snapshot bytes; architecture test bans edit verbs and `ProfileAt` | `Analysis_DraftOpen_EvaluatesAcceptedRevision` |
| FM-2 | Lattice and drawn foil disagree | **prevent** — `PlacedCamber` from Core only | `Sections_PlaceEqualsSurfaceMidline_Bitwise` |
| FM-3 | Closing (zero-chord) tip strip | **prevent** — diagonal normals; triangle tip panel kept; exclude only below 10 µm control-point chord, listed | `Vlm_ClosingTip_FiniteAndListed`; elliptic fixture F-2 closes its tip |
| FM-4 | Non-finite result | **prevent** — fail closed `ANA-NONFINITE` | `Vlm_NonFinite_RecordsFailedNotZero` |
| FM-5 | Stale result shown as current | **prevent** — freshness derived per projection | `Freshness_SurfaceEdit_Historical`, `Freshness_UndoToEqualKey_CurrentAgain`, `Freshness_SaveReopen_Unchanged` and the other freshness tests of §13.3 |
| FM-6 | Units change invalidates | **prevent** — units outside the key | `Units_Lbf_KeyUnchanged` |
| FM-7 | Water outside 0–50 °C | **prevent** — COPY-52 before compute | `Water_OutsideTable_Unavailable` |
| FM-8 | Depth unset printed as deep water | **prevent** — projection rule (DR-ANA-13) | `Depth_Unset_NoDeepWaterLabel`, `Labels_DepthUnset_FreeSurfaceNotModelled` |
| FM-9 | Polar unavailable zeroes profile drag | **prevent** — Total drag Unavailable naming "profile" | `Loads_PolarUnavailable_TotalDragNamesProfile` |
| FM-10 | Process polar hangs or orphans | **mitigate** — timeout, kill tree, outputs by files (only if DR-ANA-1 picks a process) | fault injection with a sleeping stub |
| FM-11 | Document grows past 8 MB | **mitigate** — strip cap, DR-ANA-4 retention, preflight `DOC-SIZE` | `Store_SizeAtStripCap_UnderDocLimit` |
| FM-12 | Global evaluator counters raced (G-3) | **prevent** — counters `Interlocked` in the Core track; counter tests run with analysis idle | counter test under a concurrent evaluation |
| FM-13 | Evaluate during a CAD gesture | **accept** — snapshot is the accepted revision; residual: one wasted evaluation | — |
| FM-14 | Duplicate row for one key | **prevent** — store invariant | `RecordRun_SameKeyTwice_OneCompletedRow` |
| FM-15 | Coarse lattice, e > 1 | **detect** — lattice finding (§5.2) | F-2, F-6 |
| FM-16 | Run recorded after close | **prevent** — `DOC-CLOSED` | `Evaluate_CloseMidCompute_DocClosedNoRow` |
| FM-17 | Toggle loses camera/selection | **prevent** — toggle touches neither | `Toggle_RoundTrip_CameraSelectionStationViewportEqual` |
| FM-18 | Old build overwrites a `-2` file | **prevent** — old reader fails closed | `Project2_TodaysReader_FailsClosedCopy130` |
| FM-19 | A tampered run shown as Current | **prevent** — content hash + recomputed key | `Tamper_EditedStripValue_RunUnavailable`, `Tamper_StoredKeySetToCurrent_RunUnavailable` |
| FM-20 | Pruning deletes a run Undo would revive | **prevent** — DR-ANA-4 reachability rule + tombstone | `Retention_PruneThenUndo_TombstoneReadsPruned` |
| FM-21 | Result outside the method envelope shown bare | **prevent** — envelope verdict per strip | `Vlm_AlphaBeyondEnvelope_ShowsEnvelopeFinding`, `Envelope_RunVerdict_BesideCL_FullBound`, `Station_StripReadout_EnvelopeVerdictPerPart` |

## 10. Adversarial analysis (STRIDE-lite)

| Boundary | Threat | Disposition | Negative test |
|---|---|---|---|
| Native document `analysis` member (user-writable JSON) | **T**ampering: edited strip values or a forged key | **mitigate** — per-run content hash; key recomputed from the manifest, never trusted; mismatch → that run Unavailable. Residual: someone who recomputes the hash can forge a run; accepted — it is the user's own local calculation and is recomputable | edit a strip value; set the stored key to the current key |
| same | **D**oS: huge arrays | **mitigate** — 8 MB / 4 kB-line caps, `MaxDepth`, strip cap 2,048 per run | 8 MB+ file; 10⁵ strips |
| Water table data file | **T**ampering changes ρ or p_v | **mitigate** — hash at load against the build manifest; mismatch → water Unavailable | one-bit flip |
| Polar process (DR-ANA-1 c/d) | **E**levation / injection via argv or names | **mitigate** — argv array, never a shell string; run directory from the run key; coordinates written by us | profile name with `;`, `$(`, newline |
| CLI `analyse` | malformed op JSON | **mitigate** — the GUI's validators; stable codes | non-finite speed; unknown member |

No identity, secret or network boundary is added.

## 11. Telemetry (instrumentation over inference, IO1–IO12)

Emitted on the normal path through the session's `SessionEvent` queue (`AuthoringSession.Run/Record`, :113–158):

| Question | Event · fields |
|---|---|
| How long, which tier, did it fail? | `analysis.run` · tier, method id + version, run key (12 hex), scope, outcome (`OK` · code · `ANA-CANCELLED`), duration ms, unknowns, strips, residual, κ₁, strips outside envelope, idempotent hit |
| Where does the time go? | `analysis.run` sub-durations `snapshotMs`, `sectionsMs`, `assembleMs`, `solveMs`, `stripMs`, `recordMs` ("not recorded" when not reached) |
| Does the toggle meet 250 ms p95? | `analysis.toggle` · from, to, ms to first layer frame, layers drawn |
| How often is a viewed result Historical? | `analysis.project` · freshness, what-changed field, tier |
| Polar process health (if any) | `polar.process` · exit, wall ms, timeout, tree killed, outputs found |

Missing measurements read "not recorded", never a plausible number (IO8). No file names or user text in events.

## 12. UI and interaction design

### 12.1 The CAD ↔ Analysis toggle (ANA-22)

- **One action:** the **CAD | Analysis** segment at the left of the model-area navbar (beside Views ▾ · Display ▾ ·
  Fit), View ▸ Analysis, and a shortcut (DR-ANA-8); the activity rail when built (DR-7).
- **Preserved:** `Selection`, `PlanCamera`, `Camera3d`, elevation cameras, layout, view sizes, docks — the toggle
  changes `ShellMode` and the `LayerSet` only (G-4); tested by equality before/after both ways.
- **Open draft:** a point draft or a section draft is hidden, not lost: banner "Preview hidden — Apply or Cancel in CAD"
  (C2), layers over the accepted revision; toggling back restores the draft and, for a section draft, the section editor.
  A Historical run's layers carry the Historical banner in either mode.
- **In Analysis**, points are drawn dimmed and inert; a press reports "Points are edited in CAD. Switch with the CAD |
  Analysis toggle." (new copy).

### 12.2 Layout in the shipped shell (DESIGN.md §5, V2 status strip, m12b2 views)

| Place | CAD | Analysis |
|---|---|---|
| Model-area top | alert band (when needed) | **Conditions band** (40 px, one row): Speed · Water ▾ · Depth h_ref · α · **Evaluate** · derived q · Re_ref · h/c · Fr_h · σ with units; at 1024 px the derived group keeps q and σ and moves the rest into `More ▾` (the measured-toolbar rule) |
| Views | geometry, points | same geometry, points dimmed, + **layers**: Plan — Γ per strip (batlow, legend with variable, unit, range, map, run) and the loading curve; 3D — strip lift arrows (∝ N/m, along the local normal), total at the centre line, root-moment arc, free-surface line and tip depth; Side/Front — depth band, free surface at h_ref, tip-depth margin |
| Left side bar | Properties · Browser · Points | Properties shows the result for the selection (Foil → wing result, with b, moment datum and force axes beside S_ref and the run verdict on the row after CL; Station → "Strip of wing run (α_eff)" with its own envelope verdict, Re_local against the polar's Re range and the "Not modelled" list, §5.4); **Layers** replaces Points (visibility, legend fields) |
| Bottom panel | Checks / Messages | **Spanwise loading** (Cl·c/c̄ vs η with the elliptic reference; table twin) · **Section** · **Loads** · **Provenance** · Checks |
| Status strip (V2) | last report · counts | last report + item **Analysis: no result / Running / Current / Historical / Failed / Unavailable** |

Archetype: G1 workbench with G2 charts (C1), unchanged; charts follow C2.

### 12.3 Hard states (U9) — the mockup renders each

| State | What shows | String |
|---|---|---|
| No result | conditions band with Evaluate primary; no layers; "No analysis yet" group | "No analysis yet. Set the conditions, then Evaluate." (new) |
| Depth unset | σ, Fr_h, V_crit cells; on a result, the VLM fixed label's depth form; no COPY-46/47, no free-surface line | COPY-45; "attached flow; no stall; no ventilation; free surface not modelled" (DR-ANA-13, spec amendment) |
| Running | Evaluate → **Cancel**; status "Running"; static skeleton; a prior run's layers stay, Historical | "Evaluating — <tier> · <n> panels…" (new) |
| Completed, Current | layers, results, tier chip, COPY-63, envelope, depth basis, omissions | §5.4 |
| Inside / outside the method envelope | the run verdict on the row after CL, the full bound named; strips outside marked (dashed outline and a count, not colour alone) | "Outside the method envelope (\|α_eff − α_L0\| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°) — <n> of <m> strips; exceeded: <parts>" / "Inside the method envelope (…) at all <m> strips" (new) |
| Station selected (strip of wing run) | strip values, then its own envelope verdict per part, Re_local against the polar's Re range, and the "Not modelled" list | §5.4 strip readout (new rows) |
| Failed | previous run stays Historical; error card with the next step; error report in the strip | "Analysis failed — <reason> (<code>). The previous result is kept as Historical." (new) + card next step "Change the panel count or the tip, then Evaluate." (new) |
| Historical | banner on the area and every result header | COPY-64 |
| Preview hidden | banner | "Preview hidden — Apply or Cancel in CAD" (C2) |
| Unavailable / Undefined parts | cell level | COPY-70 / COPY-69 with the reason |
| Tampered run | that run's result group | "Unavailable — run payload failed its check" (new) |

### 12.4 Accessibility and performance

Every layer has a table twin and an accessible name (a 3D arrow is never the only carrier of a number); batlow/vik plus
labels, never colour alone; vik pinned at 0 for Cp; the conditions band is a labelled form with units in the accessible
names; Evaluate/Cancel is one control whose name changes and whose completion is announced through the status strip's
live region. Text ≥ 11 px, targets ≥ 24 px. Toggle p95 ≤ 250 ms and Evaluate duration are measured by telemetry; neither
is an M1 gate (M1 scope D1).

### 12.5 Medium

Native desktop (Avalonia), macOS first. Windows x64 runtime is deferred (M1 scope D2), so **no "Verified
implementation" label (A7) may render until the ring-0 fixture suite is green on both platforms** (CFD lens veto
condition) — until then every tier shows Computed estimate only.

### 12.6 New copy rows (to DESIGN.md §7 on approval; next ids after COPY-171)

"No analysis yet. Set the conditions, then Evaluate." · "Evaluating — <tier> · <n> panels…" · "Analysis failed —
<reason> (<code>). The previous result is kept as Historical." · "Analysis complete — <tier> · <t> s" · "Points are
edited in CAD. Switch with the CAD | Analysis toggle." · "Outside the method envelope (|α_eff − α_L0| ≤ 10°, Cl_local
≤ 1.0, quarter-chord sweep ≤ 30°) — <n> of <m> strips; exceeded: <parts>" · "Inside the method envelope (|α_eff −
α_L0| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°) at all <m> strips" · "Outside the method envelope at this
strip" · "Re_local <Re> outside the polar's Re range <Re_min>–<Re_max> — cd not extrapolated" · "attached flow; no stall;
no ventilation; free surface not modelled" (on DR-ANA-13's amendment) ·
"Unavailable — no polar method installed" · "Unavailable — run payload failed its check" · "Change the panel count or the tip, then Evaluate." · "Strip of wing run (α_eff)" ·
"elliptic loading assumed; planar lower bound on induced drag" · tier chips "Estimator · local calculation", "Polar ·
local calculation", "VLM + strip · local calculation".

## 13. Test plan

### 13.1 Triggered directives (Testing Strategy)

D0 hygiene; D1 pure-function unit tests; D2 analytic oracles, observed order and metamorphic tests (A8.4); D3 property
invariants; D4 persistence round trip, fail-closed parsing and forbidden updates; D5 concurrency through a barrier seam;
D6 UI behaviour and copy as content; D7 GUI ↔ CLI run-key equality. **Every row is observed red first against a named
mutant** (§13.2 column), not only a stub — a constant or zero stub satisfies the invariance rows (test lens finding 2).

### 13.2 The VLM + strip fixture suite — Area 3's gate (ANA-04 + 07, revised by DR-ANA-12)

| ID | Fixture | Expected · tolerance | Red-first mutant | Ring · cost |
|---|---|---|---|---|
| F-1 | rectangular flat plate, AR 25…200, 1 chordwise panel | Richardson in 1/AR → 2π within **0.5 %** (CFD re-run 0.15–0.3 %); Helmbold not used on a rectangle (DR-ANA-12) | bound vortex at mid-chord | ring 0 · ≈ 1 s (measured by the CFD lens' numpy re-run; C# measured at red-first) |
| F-2 | elliptic AR 8, lattice from sampled chord, α 5° | F-6's three lattices (32/64/128 per half × 4 chordwise); Richardson CL in **[0.4156, 0.4198]** (0.4177 ± 0.5 %; reference 0.41766 from this revision's independent JS lattice, 16/32/64 gives 0.41785; the CFD lens' numpy 0.4167 at 128 × 6 lies inside — [note](../notes/area3-fixture-arithmetic.md)). An input fails it: lifting line 0.4386, Helmbold 0.4282, Jones ≈ 0.427, an unextrapolated 32-span 0.4206. Extrapolated e within 1.000 ± 0.005 (reference 0.99859) | control point at mid-panel → Richardson CL 0.2391, outside the band (measured, [note](../notes/area3-fixture-arithmetic.md)) | ring 0 · shares F-6's solves |
| F-3 | symmetric section | Cl(0) = 0 within 10⁻⁶ **and** Cl(−α) = −Cl(α) within 10⁻¹² rel, Cl(4°) > 0.3 | camber read from the upper surface | ring 0 · < 10 ms |
| F-4 | mirrored wing, β 0 | side force, roll and yaw = 0 within 10⁻¹² of lift; CL > 0.1 | one half mirrored with the wrong sign | ring 0 · ≈ 50 ms |
| F-5 | Trefftz vs near-field induced drag | within 1 % at the default lattice | near-field drag without induced velocity | ring 0 · shares F-2 |
| F-6 | refinement, elliptic AR 8, **32/64/128** per half (r = 2) | observed order p = ln((f₂ − f₁)/(f₃ − f₂))/ln 2 within 1.0 ± 0.2 for CL and e (reference p(CL) 1.070, p(e) 1.013; 16/32/64 is not used — p(CL) 1.183 sits at the band edge); the method record states p = 1 | **O(1) mutant: the wake length read per panel (`wakeSpans` × panel span, not × wing span).** The error grows with refinement (CL +5.9, +11.9, +21.3 %), so p(CL) = −0.735 and p(e) = 0.515 — red (measured, [note](../notes/area3-fixture-arithmetic.md)). Rev 2's off-by-one strip is O(h) and leaves p near 1 | ring 0 by A8.4 (observed order is ring 0) · ≈ 0.8 s in the JS reference; C# measured at red-first; exempt from the 0.5 s rule by name (§13.4) |
| F-7 | twist sign, −3° linear washout | tip α_eff < root by ≈ 3° minus the induced change; CL below untwisted | twist sign flipped | ring 0 · est. 0.1 s (two 512-unknown solves) |
| F-8 | dihedral **±20°**, Example-foil rectangle, α 5°, 32 × 4 | S_ref **pinned to the developed area** (b·c): CL ratio to planar **0.8938 ± 1 %** (both signs; [note](../notes/area3-fixture-arithmetic.md)) | dihedral ignored in placement → ratio 1.0000, **11.9 % off** — 12 × the tolerance. Rev 2's ±10° projected target let it pass (0.8 % on the CFD lens' wing, 1.25 % here) | ring 0 · est. 50 ms (three 256-unknown solves) |
| F-9 | ANA-03 arithmetic | L 2688 N, D 156.8 N, CL/CD 17.142857 | q without ½ | ring 0 · µs |
| F-10 | bookkeeping (ANA-11) | near-field wind-axis Σ vs L_T and half-span root moment, normalised by L·b/2, within the manifest tolerance; up to α 10° | drop or double one strip | ring 0 · est. 0.2 s (two solves) |
| F-11 | metamorphic scale | geometry × k → CL, CDi, e unchanged (10⁻¹² rel); forces × k²; CL > 0.1 | chord used unscaled | ring 0 · est. 50 ms (coarse lattice) |
| F-12 | metamorphic speed | V × k at fixed α → forces × k², coefficients unchanged | q with V not V² | ring 0 · est. 50 ms |
| F-13a | water, inviscid (ANA-15, A3a) | fresh → salt at 15 °C, fixed V·c: Re_ref and every Re_local fall 4.25 ± 0.05 %; inviscid loads change by ρ_sea/ρ_fresh (correct for an inviscid tier). The Historical part is `Freshness_WaterChange_Historical` (§13.3) | ν not read from the water record (fresh ν kept) → Re unchanged, red | ring 0 · est. 0.2 s (two solves) |
| F-13b | water, with a polar (ANA-15, A3c) | fresh → salt at 15 °C: cd at both Ncrit is re-retrieved at the new Re_local; a result whose loads change by exactly 1.0269 (= 1026.02/999.10) with unchanged coefficients fails | polar looked up at the stored fresh-water Re (only ρ updated) → loads × 1.0269 exactly, coefficients unchanged, red | ring 0 · est. 0.2 s with the stub polar of SPIKE-ANA-1 |
| F-14 | external cross-check | the Example foil at a **matched** lattice vs an AeroSandbox VLM run, Trefftz vs Trefftz, within 0.5 %; provenance recorded (AeroSandbox version, case YAML under `cases/` with its hash, platform, commit) before admission; *assume:* AeroSandbox's VLM is a comparable horseshoe model — confirmed at admission | — (an oracle, not a product row) | readiness · data only |
| F-15 | induced angle (CFD lens) | elliptic wing: α_i uniform within 1 % and equal to CL/(π AR) | α_i from the total control-point velocity | ring 0 · shares F-6's 64-span solve |
| F-16 | swept (Bertin–Smith) | AR 5, Λ_c/4 45°, 4 panels per half, 1 chordwise: C_Lα 3.443 /rad ± 0.5 % (CFD re-run 3.4440) | sweep ignored in the bound-vortex placement | ring 0 · µs (8 unknowns) |
| F-17 | own-code golden master | Example foil, default settings: outputs equal a committed vector within 10⁻¹² rel with platform, runtime and commit provenance; the same vector on Windows when it resumes | any numeric change without a version bump | ring 0 · est. 0.1 s (one 512-unknown solve) |
| — | Warren-12 | admitted only after its reference numbers are re-established (07 open question 3) | — | not in v1 |

### 13.3 Domain, persistence and UI tests — each named, with its failing input, ring and cost

"Ring 0" is the new `CfdWorkbench.Analysis.Tests` harness at every join; "Desktop" is the Desktop harness at every
join; "Cli" the Cli harness. Costs marked est. are **Inferred** (projection tests run on a pinned recorded run, no
solve); each is measured at red-first and recorded by its `COST` line (§13.4 C-5). Every row is observed red first
against the failing input or mutant named.

| Test | Failing input (red first) | Ring | Cost |
|---|---|---|---|
| `Freshness_SurfaceEdit_Historical` | move one twist vertex; mutant: `SurfaceHash` left out of the key → stays Current | 0 | est. < 5 ms |
| `Freshness_ProfileEdit_Historical` | edit one profile coordinate; also settles the §3.4 *assume* (both hashes change) | 0 | est. < 5 ms |
| `Freshness_WaterChange_Historical` | fresh → salt at 15 °C; mutant: water left out of the key | 0 | est. < 5 ms |
| `Freshness_OperatingPointChange_Historical` | α 3.00° → 3.01° | 0 | est. < 5 ms |
| `Freshness_MethodVersionBump_Historical` | 1.0.0 → 1.0.1 | 0 | est. < 5 ms |
| `Freshness_EachSettingsField_Historical` | each settings field changed alone, the field named in the failure; mutant: `wakeDirection` left out of `settingsHash` | 0 | est. < 10 ms |
| `Freshness_UndoToEqualKey_CurrentAgain` | edit, then Undo → Current; mutant: freshness stored as a flag | 0 | est. < 5 ms |
| `Freshness_SaveReopen_Unchanged` | save and reopen one Current and one Historical run | 0 | est. < 50 ms |
| `Units_Lbf_KeyUnchanged` | N → lbf; mutant: units inside `settings` | 0 | est. < 5 ms |
| `RunKey_PinnedVector_HexEqual` | one committed manifest → one committed hex key; mutant: two JCS members swapped | 0 | est. < 1 ms |
| `Project2_RoundTrip_ByteEqual` | a document with two runs written, read, written | 0 | est. < 50 ms |
| `Project1_NoRun_ByteIdenticalToToday` | no run → `cfdw-project-1`, bytes equal today's writer; mutant: format string hard-coded `-2` | 0 | est. < 50 ms |
| `Backup_V1Bak_TodayReaderByteEqual` | first `-1` → `-2` save; today's reader opens the `.bak` byte-equal; an existing `.bak` is never overwritten | 0 | est. < 100 ms |
| `Project2_TodaysReader_FailsClosedCopy130` | a `-2` sample through today's reader → COPY-130, nothing written | 0 | est. < 50 ms |
| `RecordRun_SameRunIdTwice_Refused` | the same `runId` recorded twice | 0 | est. < 5 ms |
| `RecordRun_SameKeyTwice_OneCompletedRow` | two Completed runs with one key | 0 | est. < 5 ms |
| `RecordRun_StripGap_Refused` | strips j = 0, 1, 3 | 0 | est. < 5 ms |
| `Tamper_EditedStripValue_RunUnavailable` | one strip's Fz edited in the file → that run "Unavailable — run payload failed its check" | 0 | est. < 50 ms |
| `Tamper_StoredKeySetToCurrent_RunUnavailable` | stored `runKey` overwritten with the current key → key recomputed, run Unavailable | 0 | est. < 50 ms |
| `Retention_PruneThenUndo_TombstoneReadsPruned` | prune, then Undo to the pruned key → "pruned", never a silent Current | 0 | est. < 50 ms |
| `Store_SizeAtStripCap_UnderDocLimit` | a run at 2,048 strips, size measured; red when the document passes 8 MB | 0 | est. 0.2 s |
| `Store_HundredThousandStrips_RefusedDocSize` | 10⁵ strips in the file → refused, `DOC-SIZE` | readiness | est. 2 s |
| `Evaluate_Supersede_OlderCancelledViaBarrier` | two Evaluates; the barrier holds the first; the first ends `ANA-CANCELLED`, one row | 0 | est. < 20 ms |
| `Evaluate_Cancel_NoRowRecorded` | Cancel while the barrier holds → no row | 0 | est. < 20 ms |
| `Evaluate_CloseMidCompute_DocClosedNoRow` | close while the barrier holds → `DOC-CLOSED`, no row | 0 | est. < 20 ms |
| `Cli_AnalyseRunKey_EqualsGui` | `cfdw analyse` and the GUI path on one op → equal keys; mutant: CLI defaults one setting differently | Cli | est. 0.3 s |
| `Toggle_RoundTrip_CameraSelectionStationViewportEqual` | CAD → Analysis → CAD with a station selected and an orbited camera; each value equal | Desktop | est. < 50 ms |
| `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | a point draft open; the banner shows; on return the draft bytes are equal | Desktop | est. < 50 ms |
| `Toggle_PreviewOpen_LayersOverAcceptedRevision` | a draft with a moved vertex; the layers' surface hash equals the accepted `SurfaceHash`; mutant: layers built from the draft bytes | Desktop | est. < 50 ms |
| `Toggle_HistoricalRun_BannerInBothModes` | a Historical run; the banner in CAD and in Analysis | Desktop | est. < 50 ms |
| `Toggle_NeverEvaluates` | ten toggles → zero `EvaluateAsync` calls (barrier seam); mutant: the toggle evaluates on entry | Desktop | est. < 50 ms |
| `Toggle_LayersFirstFrame_P95WithinPreviewBudget` | 20 toggles on the reference fixture (A8.1) with a recorded run; p95 of `analysis.toggle` ms ≤ 250. Failing input: the barrier seam adds 300 ms to the projection → p95 ≥ 300, red | readiness (timing is never ring 0; a product target, not an M1 release gate — spec 1.6 D1) | est. 3 s |
| `Copy_AnalysisStrings_MatchDesignMd` | every COPY id in §12.3 and every §12.6 row compared verbatim, the failing row named; mutant: one character changed | 0 | est. < 10 ms |
| `Lab01_EveryProjectionString_Lints` | every string the projection emits through LAB-01; mutant: one unit dropped | 0 | est. < 50 ms |
| `PolarProcess_HangingStub_TimeoutKillsTree` | a sleeping stub process → timeout, tree killed, Failed row (only if DR-ANA-1 picks c/d) | readiness | est. 5 s |

### 13.4 Rings and cost — where the tests run (measured, then enforced)

**Measured 2026-10-03 on this HEAD (`b47fcba`), `tools/run-tests.sh`, Release:** wall **46 s** of the 60 s budget;
build 5 s; Core 1/2 14 s, Core 2/2 27 s, Desktop **41 s (critical path)**, Cli 1 s. Every harness launches at once
(G-13). Headroom 14 s.

- **Ring 0 (every join):** F-1…F-13b, F-15…F-17 and the ring-0 rows of §13.3 and §13.5, in a new
  `CfdWorkbench.Analysis.Tests` harness that runs **concurrently** with the other four. Budgets: **≤ 5 s Release for
  the harness, run-tests wall ≤ 50 s** (10 s kept for contention with Desktop), and **≤ 0.5 s per check** except the
  two named A8.4 exemptions **F-1 and F-6 at ≤ 1.5 s each** (A8.4 keeps analytic oracles and observed order in ring 0,
  so they cannot move). A check over its limit fails the join (C-2…C-5); the fix is to move a non-exempt check to
  readiness with its name and measured cost recorded, or to make it cheaper — never to raise the number unmeasured.
- **Readiness (merge to main):** F-14, `Store_HundredThousandStrips_RefusedDocSize`,
  `PolarProcess_HangingStub_TimeoutKillsTree`, `Toggle_LayersFirstFrame_P95WithinPreviewBudget`, and the 4-level
  refinement if F-6's three levels exceed 1.5 s.
- **Desktop UI rows** (toggle, states, copy) join the Desktop harness at ≤ 2 s added; over 43 s Desktop triggers
  DR-ANA-10 (C-4).
- **Gate meaning:** "VLM + strip fixture suite green" (A2) = the ring-0 rows green on macOS; "Verified numerical
  implementation" waits for Windows (§12.5).

**The enforcement is build-track work (slice A3a), designed here (rev 2 finding M-T3).** Rev 2 stated these limits as
prose. `run-tests.sh` enforces only the 60 s wall (:24, :95), and it times with bash `SECONDS` (:32, :44, :50, :92),
which counts whole seconds — a 5 s limit read from it lets 5.9 s pass. The checks below measure milliseconds and land
red-first, each with the failing input its `--self-test` feeds it.

| Check | Lives in | Measures | Fails when | Failing input (self-test) |
|---|---|---|---|---|
| C-1 | `tools/run-tests.sh`: beside each `<name>.seconds` it writes `<name>.ms`, and `wall.ms` for the run, from a millisecond clock (`python3 -c 'import time; print(time.time_ns() // 1000000)'` before and after — the wall clock, one clock for every process; bash 3.2 on macOS has no `EPOCHREALTIME`) | each harness's wall and the run's wall, in ms | — (it records) | — |
| C-2 | `tools/check-test-costs.py` (new, stdlib), called by `run-tests.sh` after the wait loop, so every join runs it | `Analysis.ms` | > 5000 | `Analysis.ms` = 5900 → exit 1 (whole-second `SECONDS` would read 5 and pass) |
| C-3 | same script | `wall.ms` while `Analysis` is in `jobs` | > 50000 | 50400 → exit 1 |
| C-4 | same script | `Desktop.ms` | > 43000 → exit 1 naming DR-ANA-10 | 43100 → exit 1, message contains "DR-ANA-10" |
| C-5 | the Analysis harness's check helper times each check with `Stopwatch` and prints `COST <name> <ms>` (0.1 ms) on its own line beside `PASS <name>`, so `check-named-tests.py`'s `PASS <name>` contract is untouched; the script reads the `COST` lines | each ring-0 check | > 500 ms, or > 1500 ms for F-1 and F-6 | `COST Units_Lbf_KeyUnchanged 512.3` → exit 1 naming it; `COST F6_ObservedOrder 1612.0` → exit 1 |
| C-6 | same script | presence | a harness in `jobs` without its `.ms`, or an Analysis `PASS` without a `COST` line ("not recorded" never passes, IO8) | `Analysis.ms` deleted → exit 1 |

Ring of C-1…C-6: every join (inside `run-tests.sh`). Cost: est. < 0.2 s per run (two clock reads per harness at
≈ 30 ms each, one log scan). Residual: a system clock step during a run skews one reading; accepted for a 60 s run.

### 13.5 Story → test → slice (test lens findings 1 and B-T1)

Each clause names its tests; no group names. Ring and cost as in §13.3; F rows as in §13.2. Rows in **bold** are the
clauses rev 2 left at story level (B-T1).

| Story clause | Test — failing input | Ring | Cost | Slice |
|---|---|---|---|---|
| ANA-01 Ncrit band, labels; Unavailable for edited profile / out of Re grid | `Section_EditedProfileNoPolar_Unavailable` — edited profile, stub polar; mutant: the catalog original's polar used | 0 | est. < 5 ms | A3a (Unavailable) · A3c (values) |
| ANA-02 Cp on the profile | `PanelCp_KarmanTrefftz_100_200_400` — Kármán–Trefftz section at 100/200/400 panels; mutant: TE panel dropped | 0 | est. 0.2 s | A3b |
| ANA-02 screen string with station count | `Cavitation_ScreenString_NamesStationCount` — mutant: N omitted | 0 | est. < 5 ms | A3b |
| ANA-02 margin | `Cavitation_Margin15Percent_Applied` — σ at 1.10 × −Cp_min reads inside the margin; mutant: margin ignored | 0 | est. < 1 ms | A3b |
| ANA-02 Undefined | `Cavitation_NegCpMinNonPositive_Undefined` — −Cp_min = 0 and −0.1 | 0 | est. < 1 ms | A3b |
| ANA-02 Unavailable | `Cavitation_PvOrDepthMissing_Unavailable` — p_v missing; depth unset | 0 | est. < 1 ms | A3b |
| ANA-02 governing station | `Cavitation_GoverningStation_AtAlphaEffAndLocalDepth` — mutant: α_geo and h_ref used | 0 | est. 0.1 s | A3b |
| ANA-03 per span N/m; wing CL, CD, L, D with basis; arithmetic | F-9; `Projection_SectionVsWingUnits` — mutant: section loads in N | 0 | µs · est. < 5 ms | A3a |
| **ANA-03 "CD ≤ 0 → Undefined"** | `Projection_CdZeroOrNegative_ClCdUndefined` — CD = 0 and CD = −0.001: CL/CD reads "Undefined — CD ≤ 0", never ∞ or a negative ratio; mutant: plain division (renders "Infinity") | 0 | est. < 1 ms | A3a |
| **ANA-03 "V ≤ 0 → Undefined"** | `OperatingPoint_SpeedZeroOrNegative_Undefined` — V = 0 and V = −1 kn: q, Re_ref, Fr_h and σ read "Undefined — speed ≤ 0", Evaluate is refused (`ANA-INPUT-SPEED`), no run is recorded; mutant: \|V\| used | 0 | est. < 1 ms | A3a |
| ANA-03 missing component | `Loads_PolarUnavailable_TotalDragNamesProfile` — no polar; mutant: profile drag 0 | 0 | est. < 5 ms | A3a |
| ANA-04 lattice rows | F-1, F-2, F-3, F-4, F-5, F-6, F-7, F-8, F-15, F-16 (§13.2, each with its mutant) | 0 | §13.2 | A3a |
| ANA-05 Find α / take-off | `FindAlpha_TargetCL_RootWithinTolerance` — target CL 0.3 on the Example foil; mutant: degrees fed to a radians step; `FindAlpha_NoRoot_UnavailableWithReason` — a target above the envelope's CL | 0 (readiness if measured > 0.5 s) | est. 0.5 s each | A3c |
| ANA-06 compare, Discrepancy | `Compare_TwoTiers_DeltaAndDiscrepancyRecord`; `Compare_SameKeyTwice_ReadNotNewRecord`; `Compare_EstimatorE_ExcludedFromEDeltas` — mutant: estimator e = 1 enters the delta | 0 | est. < 10 ms each | A3d |
| ANA-07 freshness | `Freshness_SurfaceEdit_Historical`, `Freshness_ProfileEdit_Historical`, `Freshness_WaterChange_Historical`, `Freshness_OperatingPointChange_Historical`, `Freshness_MethodVersionBump_Historical`, `Freshness_EachSettingsField_Historical`, `Freshness_UndoToEqualKey_CurrentAgain`, `Freshness_SaveReopen_Unchanged` (inputs in §13.3) | 0 | §13.3 | A3a |
| ANA-08 case-schedule reading | Results area, not Area 3 — out of scope here (Area 6) | — | — | — |
| ANA-09 provenance inspection | `Provenance_WaterTableHash_Shown` — one-bit flip in the table → water Unavailable; `Provenance_RunContentHash_Shown` | 0 | est. < 5 ms | A3a |
| ANA-10 x_tr, bucket, overlays; strips → "Section-based inference" | `Strips_OutsideEnvelope_SectionBasedInference` — mutant: a separation claim rendered (A3a); `Charts_TransitionAndBucket_Overlays` (A3c) | 0 | est. < 5 ms | A3a · A3c |
| ANA-11 reconciliation | F-10 | 0 | est. 0.2 s | A3a |
| ANA-11 safety string verbatim | `Loads_RendersSafetyVerbatim` — mutant: one word changed | 0 | est. < 5 ms | A3a |
| ANA-11 stiffness readout beside t/c | `Loads_StiffnessReadoutBesideTc` — t/c 12 % → 1.728 | 0 | est. < 5 ms | A3a |
| **ANA-11 "missing terms are Unavailable, never zero"** | `Loads_MissingTerm_UnavailableNeverZero` — a run with no polar and no attachment point: profile drag, Total drag and the attachment moment each read "Unavailable — <reason>", and no Loads cell renders 0 or 0.00; mutant: the projection's null-to-zero default (`?? 0`) | 0 | est. < 5 ms | A3a |
| **ANA-11 the Not-assessed list** | `Loads_StructuralNotAssessed_ListComplete` — the Loads panel shows COPY-42 and every item of A5.6's list (take-off, pumping, breach and slam, ventilation shock, impact, fatigue; spec :975), item by item; mutant: the list cut to three items | 0 | est. < 5 ms | A3a |
| **ANA-11 the attachment moment** | `Loads_AttachmentMoment_TransferAboutNamedPoint` — F = (0, 0, 100) N and M_O = 0 at the frame origin, named point P = (0.1, 0, 0) m: M_P = M_O + (O − P) × F = (0, +10, 0) N·m; mutant: (P − O) × F gives (0, −10, 0), red. No point named → Unavailable (`Loads_MissingTerm_UnavailableNeverZero`) | 0 | µs | A3a |
| ANA-15 water | F-13a, F-13b | 0 | §13.2 | A3a · A3c |
| **ANA-15 "outside 0–50 °C → Unavailable"** | `Water_OutsideTable_Unavailable` — −0.1 °C and 50.1 °C read "Unavailable — outside the ITTC table (0–50 °C)", Evaluate is refused, no run is recorded; 0 °C and 50 °C are accepted (the bounds are inside); mutant: clamp to the nearest table row | 0 | est. < 1 ms | A3a |
| ANA-16 performance curves vs α and speed | needs a schedule (Experiment) — DR-gated to Area 4/6 | — | — | — |
| ANA-18 unit conversion | `Units_Lbf_KeyUnchanged` | 0 | est. < 5 ms | A3a |
| ANA-19 depth row 1 (unset) | `Depth_Unset_NoDeepWaterLabel` — depth unset: σ, Fr_h and V_crit read COPY-45 and "deep water" appears nowhere; mutant: the label a constant | 0 | est. < 5 ms | A3a |
| ANA-19 depth row 2 (h/c < 5) | `Depth_HcBelow5_Copy46WithNumbers` — h_ref 0.5 m, c 0.12 m → h/c 4.17: COPY-46 with h/c and Fr_h; mutant: the threshold compared with h, not h/c | 0 | est. < 5 ms | A3a |
| ANA-19 depth row 3 (h(y) ≤ 0) | `Depth_StationPiercing_EstimatorUnavailableFlagOnly` — h_ref 0.05 m with 20° dihedral → h(y) ≤ 0 at the tip: that station's estimator Unavailable, only the surface-piercing flag; mutant: h_ref used for every station | 0 | est. < 5 ms | A3a |
| ANA-19 depth row 4 (set, submerged) | `Depth_SetAllSubmerged_TipMarginFrCopy47` — tip-depth margin, Fr_h and COPY-47 shown | 0 | est. < 5 ms | A3a |
| ANA-20 catalog envelope, e 0.85–1.00, lattice flag, never blocks | `Envelope_EOutOfBand_AdvisoryNotBlocking`; F-2 lattice flag | 0 | est. < 5 ms | A3a |
| ANA-21 vectors ∝ load along the local normal, strips batlow, depth band, legends, table twin, tier chip | `Layers_ArrowLengthProportional_NormalDirection`; `Layers_EveryVisualHasTwinAndChip`; Cp on the profile in A3b (`PanelCp_KarmanTrefftz_100_200_400`) | 0 | est. < 10 ms | A3a · A3b |
| ANA-22 camera, selection, station, viewport size | `Toggle_RoundTrip_CameraSelectionStationViewportEqual` | Desktop | est. < 50 ms | A3a |
| ANA-22 preview hidden and restored | `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | Desktop | est. < 50 ms | A3a |
| ANA-22 layers over the accepted revision | `Toggle_PreviewOpen_LayersOverAcceptedRevision` | Desktop | est. < 50 ms | A3a |
| ANA-22 Historical banner in both views | `Toggle_HistoricalRun_BannerInBothModes` | Desktop | est. < 50 ms | A3a |
| **ANA-22 "within the preview budget"** | `Toggle_NeverEvaluates` (the cause, deterministic) and `Toggle_LayersFirstFrame_P95WithinPreviewBudget` (the measurement: p95 ≤ 250 ms, A8.1; failing input a 300 ms injected delay) | Desktop · readiness | est. < 50 ms · est. 3 s | A3a |
| ANA-23 Undefined/Unavailable vectors not drawn | `Layers_UnavailableVectorNotDrawn_AbsenceStated` | 0 | est. < 10 ms | A3a |
| **ANA-23 the moment arc** | `Layers_MomentArc_SenseFollowsSignAboutNamedDatum` — the Example foil at α +3° and −3°: the arc's label names the datum ("about the root plane", or the named attachment point) and N·m, and its sense follows the sign of M_x (right-hand about +x); mutant: the arc built from \|M_x\| — the −3° case keeps the positive sense, red | 0 | est. < 10 ms (two recorded runs) | A3a |
| **ANA-23 the sign convention** | `Layers_Vectors_BodyFrameSignConventionLabelled` — α +3°: lift along +z, drag along +x (aft), the legend reads "+x aft, +y starboard, +z up"; α −3°: lift along −z; mutant: the vector direction taken from \|L\| — the −3° case red | 0 | est. < 10 ms | A3a |
| §5.4 per-tier fixed labels and omissions | `Labels_PerTier_FixedPartsAndOmissions`; `Labels_DepthUnset_FreeSurfaceNotModelled` — depth unset: "… free surface not modelled", never "deep water" (DR-ANA-13); mutant: the label a constant | 0 | est. < 5 ms | A3a |
| §5.4 run verdict beside CL, full bound | `Envelope_RunVerdict_BesideCL_FullBound` — α 12°: the row after CL names all three bounds and "<n> of <m> strips"; mutant: the verdict only in Labels | 0 | est. < 5 ms | A3a |
| §5.4 strip readout verdict (B-H1) | `Station_StripReadout_EnvelopeVerdictPerPart` — a strip at α 12° with \|α_eff − α_L0\| > 10° reads "Outside the method envelope at this strip" with the α part named first; mutant: the strip shows the run verdict | 0 | est. < 5 ms | A3a |
| §5.4 strip Re against the polar range (B-H1) | `Station_StripReadout_ReAgainstPolarRange` — no polar → "Unavailable — no polar method installed", never "inside"; a stub polar with Re 2 × 10⁵–10⁷ and a strip at 1.7 × 10⁵ → "outside … cd not extrapolated"; mutant: the verdict defaults to inside | 0 | est. < 5 ms | A3a (stub) · A3c |
| §5.4 strip omissions (B-H1) | `Station_StripReadout_NotModelledList` — mutant: the list omitted on the strip | 0 | est. < 5 ms | A3a |

## 14. Conformance notes and findings

- ADR-0010 one placement rule: honoured (§4, `PlacedCamber` computed in Core).
- A2 non-goals: no XFOIL/XFLR5/VTK/OpenFOAM linked; e computed; no averaging across tiers.
- DM5/DM7/DM9/DM10/DM11/DM15/DM16 applied in §3; DM13 asks an ADR → DR-ANA-4's ruling becomes ADR-0011.
- Findings for other owners (not chased): G-5 duplicate enums (Desktop), G-3 global counters (Core), G-12 COPY-63 case
  (spec owner), and the DR-ANA-13 amendment request to A5.6 :962 (spec owner, §15).

## 15. Decision requests — DR-ANA batch (one batch; options · evidence · recommendation)

| ID | Question | Options | Evidence | Recommendation |
|---|---|---|---|---|
| **DR-ANA-1** | Where do polars (Cl, Cd, Cm, x_tr, Cp) come from? | (a) none yet — polar Unavailable · (b) C# in-process inference of NeuralFoil's MIT network after **SPIKE-ANA-1**, which tests two things separately: port fidelity (C# vs Python NeuralFoil ≈ 10⁻⁶) and model accuracy (vs process XFOIL on NACA 0012 at Re 10⁶ and 2 × 10⁵, Ncrit 2, 4 and 9; Cl ± 0.02, ln Cd ± 0.03), with the weights' licence and the CST-fit residual reported and the model size in the version · (c) NeuralFoil Python sidecar (a runtime to ship; CasADi LGPL + IPOPT EPL review) · (d) user-installed XFOIL as a process (GPL; an oracle) | 07 finding 2; A8.5; ANA-01 permits Unavailable | **(a) for A3a, then (b) behind SPIKE-ANA-1** |
| **DR-ANA-2** | Source of Cp on the section and the cavitation screen | (a) an inviscid linear-vorticity panel method in the **estimator** tier (spec 1.7 row; label "inviscid; no boundary layer"; Kármán–Trefftz Cp oracle at 100/200/400 panels; then the estimator's section Cl comes from the panel — one definition) · (b) Cp only from the polar (64 stations; under-reads the suction peak, 07 finding 4) · (c) none in v1 | ANA-02, ANA-21; A5.4 | **(a)**, with the §5.1 screen conditions (hydrodynamics lens: approved with them) |
| **DR-ANA-3** | Assembly placement | (a) new `CfdWorkbench.Analysis` + `Placement.Sections` in Core · (b) inside Core | architecture §4; Core tests 41 s across two parts | **(a)** |
| **DR-ANA-4** | Durable representation and retention | (a) in the native project as `cfdw-project-2` (only when a run exists), `.v1.bak` on the first upgrade, store invariants and per-run content hash; **retention:** keep every run whose key is reachable from any retained accepted revision or the redo stack, plus the latest 20 others per tier; the latest Failed row per key; prune the rest at save, listing them in the save report and leaving a tombstone fact (runId, key, prunedAt); never prune a run a Discrepancy record references · (b) a sidecar file · (c) session-only (ANA-07 unmet) | ANA-07; A3.4 Undo clause; COMMIT-03; DM5/DM11; G-6; size unmeasured | **(a)**; plus a spec 1.7 note that interactive attempts are run rows with an outcome (the spec's attempt fact is Experiment-only) |
| **DR-ANA-5** | Where the named attachment point is authored | (a) a project value in the Loads tab (Type-1; moment derived) · (b) per run (in the key) | §3.5 | **(a)** |
| **DR-ANA-6** | What computes; is recompute automatic? | (a) explicit **Evaluate** (a new verb, spec 1.7); Historical stays until pressed · (b) auto-evaluate on every operating-point commit and on entering Analysis | B1 verbs; F4 node S; ANA-07 | **(a)** |
| **DR-ANA-7** | Default lattice and reconciliation tolerance | (a) **64 spanwise per half (cosine) × 4 chordwise (cosine)**, wake 20 spans, tolerance 1 % · (b) 32 × 6 · (c) adaptive to AR | CFD re-run: e 1.016 at 32 × 6 on the elliptic wing, converging from above at p ≈ 1; this mockup's probe: Example foil e 1.013 at 32 × 6, 1.003 at 64 × 4, untwisted rectangle 0.990 → 0.982 at 32 → 64 | **(a)**, confirmed by F-6 before A3a ships; e > 1.000 on a planar wing reads as a lattice finding |
| **DR-ANA-8** | Toggle shortcut | (a) ⌘/Ctrl+Shift+A · (b) ⌘/Ctrl+2 / 3 for areas 2/3 · (c) menu only | ANA-22; SC 2.1.4 | **(b)**, after checking the command table for conflicts |
| **DR-ANA-9** | Operating point before Area 1 exists | (a) **Custom** only; "From Goal state point n" disabled with its reason · (b) wait | Area 1 not built | **(a)** |
| **DR-ANA-10** | If Desktop UI rows push Desktop over 43 s | (a) move them to readiness · (b) split Desktop into two parts | measured 41 s | **(b)**, decided on the measurement |
| **DR-ANA-11** | Estimator wing e | (a) e = 1 labelled "elliptic loading assumed; planar lower bound on induced drag", out of the e envelope and Compare deltas · (b) a Fourier lifting line (computed e) | A2 non-goal | **(a)** |
| **DR-ANA-12** | ANA-04's lattice oracles (G-10) | (a) spec 1.7: flat plate by Richardson in 1/AR to 2π (0.5 %); elliptic CL against a recorded lifting-surface reference, e → 1 after Richardson; Bertin–Smith admitted at ± 0.5 % · (b) keep the 1.6 text (a correct lattice fails it) | CFD lens re-run (Inferred); hydrodynamics lens (Jones factor CL ≈ 0.427) | **(a)** |
| **DR-ANA-13** | VLM fixed label with depth unset (G-11) | (a) depth unset → the fixed part reads "free surface not modelled" in place of "deep water" · (b) keep "deep water" always | A5.1/ANA-19 vs A5.6 | **(a)**. **Spec amendment request to the spec owner:** A5.6 :962 says the VLM label "never changes" (it guards against a correction layer rewriting it); (a) changes it on one input. Proposed 1.7 text: "attached flow; no stall; no ventilation; deep water — or, with depth unset, free surface not modelled (A5.1, ANA-19); a correction layer is shown beside, never changes this label". Mockup screen 3b shows (a) |
| **DR-ANA-14** | VLM method envelope bounds (§5.4) | (a) \|α_eff − α_L0\| ≤ 10°, Cl_local ≤ 1.0 or the polar bracket, sweep ≤ 30° · (b) tighter (8°, 0.8) | 07 §3D methods (Inferred) | **(a)**, revisited when the polar lands. **Inferred; tighten per strip when the polar brackets land (low-Re tips stall earlier)** — with a polar, each strip's Cl bound becomes its own polar's converged bracket at its Re_local, so a 40 mm tip at Re 1.7 × 10⁵ gets a lower bound than the root |

## 16. Delivery slices (each a vertical, demoable increment)

| Slice | Real end to end | Gate |
|---|---|---|
| **A3a** wing VLM (inviscid) | toggle, conditions band (Custom op), Evaluate, VLM + strip lift / induced drag / loads with envelope, layers, Properties, Loads with safety copy, Provenance, Historical, `-2` persistence | ring-0 rows of §13.2–13.3 and §13.5 marked A3a; operator demo |
| **A3b** section tier | estimator section; DR-ANA-2 panel Cp + cavitation screen; Section tab | panel oracle; ANA-01/02 |
| **A3c** polar + strip drag + Find α | DR-ANA-1 (b); profile drag at both Ncrit; Total drag; ANA-05; A5.2 correction layer | SPIKE-ANA-1; ANA-05; F-13 A3c clause |
| **A3d** Compare | tiers and revisions; Discrepancy records | ANA-06 |

## Privacy analysis (LINDDUN-lite)

No personal data: runs hold geometry hashes, physical inputs and timings; no user name, path or rider mass (when Area 1
exists, the load — not the mass — enters the manifest). Telemetry carries no file names.

## Flagged risks and residual unknowns

- The default lattice and fixture costs are measured only by independent re-runs (numpy, JavaScript); the C# numbers
  are measured at red-first (DR-ANA-7, §13.4).
- The envelope bounds (DR-ANA-14) are Inferred.
- NeuralFoil weight licence and CST-fit residual are unverified (SPIKE-ANA-1).
- The ITTC water table must be transcribed with its hash and source (G-7); a two-person check against the PDF.
- F-14 is a cross-check, never a validation; nothing here raises any label above Computed estimate (A7).
- Run size, retention behaviour across save/reopen with undo/redo, and hand-merged files (data lens residual).
- Not reviewed by any lens: tandem/stabiliser downwash, junctions and mast, wake geometry under dihedral.

## Status & next action

| | |
|---|---|
| **Completed** | Area 3 design rev 3 (data model, reading contract, tiers, envelope, manifest, toggle, fixtures with mutants, rings, story matrix with ring and cost per test, timing checks C-1…C-6, telemetry, DR-ANA-1…14); mockup `docs/mockups/area3-analysis.html` (nine screens); fixture numbers `docs/notes/area3-fixture-arithmetic.md`; persona review folded (rev 3 repairs) |
| **Remaining** | operator approval of the mockup; rulings on DR-ANA-1…14; SPIKE-ANA-1; spec 1.7 rows (DR-ANA-2, -4, -6, -12, and the DR-ANA-13 amendment of A5.6 :962); ADR-0011 (DR-ANA-4); clearance of the two rev 2 blocks by their lenses |
| **Best next action** | operator reviews the mockup and rules the DR-ANA batch; then `/implement` slice A3a |

## 17. Gate record

Five lenses in Adversary Mode on revision 1; every finding folded into this revision or carried as a DR. Verdicts and
folds: [docs/reviews/area3-analysis-personas.md](../reviews/area3-analysis-personas.md). Two hard vetoes fired on
revision 1 (hydrodynamicist: VLM result shown without its method envelope; test architect: in-scope story clauses
without tests). Revision 2 answered both (§5.4 envelope, FM-21; §13.5 matrix, §13.2 mutants); both lenses re-reviewed
revision 2 and still blocked, narrowly (B-H1: the station readout had no verdict or omissions; B-T1: clauses traced only
to story level), with majors M-H1, M-T1…M-T3. Revision 3 is repair cycle 2 of 2 and folds every one (mapping in the
review file, "Rev 3 repairs"). Clearance is still for the lens, not the author.

**Re-review of revision 3 (2026-10-03).** Hydrodynamicist: **PASS-WITH-CONDITIONS, veto cleared** (B-H1, M-H1 and
every Minor verified, including a node run of the page's script). Test architect: **PASS-WITH-CONDITIONS, design-time
block cleared** (B-T1, M-T1…M-T3, F-2, F-13 verified; the note's lattice script re-run and matched); the build-time
veto stays open until red-first and the Proof Pack exist. The repair cap (2 cycles) is reached, so the remaining findings
are **conditions on the build track (A3a)**, not a third design cycle:

- **BC-1 [Major, test architect]:** about ten §13.5 tests name no mutant, against §13.1's own rule (:731, :734, :749,
  :750, :751, :757 — the ANA-23 vectors row — and :760). The build writes one mutant per row before the code, and
  check-named-tests runs it.
- **BC-2 [Minor, test architect]:** F-2, F-5 and F-15 share F-6's solves, so a per-check COST (C-5) depends on test
  order and F-2 is not exempt from the 0.5 s limit. Time the shared fixture once as F-6, or exempt the sharers by name.
- **BC-3 [Flagged, test architect]:** F-2's band is ±0.5 % around one discretisation; a C# lattice with different
  spacing may fall outside it. Confirm at red-first; a miss is a ruling, not a widened band.
- **BC-4 [Nit, hydrodynamicist]:** Re_local uses the local chord c(y), never the global chord (the mockup's rectangular
  Example hides the difference).
- Fixed by the Coordinator in this revision: an exceeded envelope part printed "≤" ("Cl_local 1.003 ≤ 1.0"); the
  strip verdict now prints ">" for an exceeded part (mockup `stripEnvOf`).
- Residual (both lenses): DR-ANA-14 bounds are Inferred until the polar brackets land; timing is estimated until the C#
  build measures it; Windows parity is untested.
