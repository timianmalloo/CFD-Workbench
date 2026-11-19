---
id: spec-amendment-setup-seeding
title: "Spec amendment proposal: Area 1 Setup & Goal State clarifications (GEN polar fallback, soft-target norm, OperatingPoint.Load, and 8->9 preset mapping)"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification — proposal (not applied; awaits operator ruling on DR-SET-1..5)
tags: [setup, goal-state, seeding, presets, soft-targets, catalog, persistence, spec-amendment, wave5]
links:
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: design-setup-seeding, rel: tested-by }
  - { to: mockup-setup-seeding, rel: relates-to }
  - { to: adr-0011-analysis-run-storage, rel: depends-on }
  - { to: design-language, rel: relates-to }
  - { to: rulings, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Four bounded spec clarifications uncovered while grounding Area 1 Setup & Goal State against the built codebase
  (CatalogData/catalog.tsv, RunRecord.cs, OperatingPoint.cs, AuthoringSession.cs): (1) CAT-03 GEN fallback and
  honest Unavailable polar cells while UnavailablePolar is active; (2) SET-01 exact one-sided ceiling vs two-sided
  soft-target deviation norm with lexicographic tie-breaking; (3) confirmation of RunRecord.cs:76 OperatingPoint.Load
  and additive Envelope.setup persistence; (4) explicit 8-purpose to 9-discipline-preset mapping table. Nothing here
  is applied until the operator rules on DR-SET-1..5.
---

# Spec amendment proposal: Area 1 Setup & Goal State

**Status: proposed (not applied).** Grounded against `docs/specs/cfd-workbench-v1.md` (rev 1.7.6) and the built Core/Analysis codebase on 2026-11-19. Companion design: [`docs/design/setup-seeding.md`](../../design/setup-seeding.md). Companion mockup: [`docs/mockups/setup-seeding.html`](../../mockups/setup-seeding.html).

---

## AM-SET-S1 · `CAT-03`: GEN default sections and polar availability in the offline milestone

- **Observed gap (`src/CfdWorkbench.Core/CatalogData/catalog.tsv:1-20`, `src/CfdWorkbench.Analysis/UnavailablePolar.cs`):**
  Spec `CAT-03` states: *"its GEN default section has polar samples at the preset's Re grid and the Ncrit pair; a pending preferred section names its fallback and reason."* In the built repository today, only three GEN sections exist (`naca-0009`, `naca-0012`, `naca-4412`) and no section polar solver or precomputed polar table is yet bundled (`RunRecord.cs:112`, `UnavailablePolar.cs`).
- **Proposed clarification (`DR-SET-4` option a):**
  1. Every discipline preset maps its preferred hydrofoil section (`e817`, `e818`, `e874`, `e904`, `speer-h105`, or `naca-4412`) to one of the three admitted GEN sections (`naca-4412` or `naca-0009`) with an explicit fallback disclosure naming the preferred section and its admission status (`COPY-109` / `COPY-110`).
  2. While no polar source is installed for the GEN section, every geometric and hydrodynamic feasibility readout (`q`, `CL_req`, `Re_ref`, `h/c`, `Fr_h`, `σ`, class-rule DRC, sanity bounds) computes live, and every polar-dependent feasibility cell (`−Cp_min` cavitation screen, `CL_max` stall check) reads `"Unavailable — polar data not computed for this section"` (never guessed, never `0`). The full `CAT-03` GEN-polar-samples clause activates automatically when the polar source lands.

## AM-SET-S2 · `SET-01`: Exact soft-target deviation norm and zero-weight tie-breaking

- **Observed ambiguity (`docs/specs/cfd-workbench-v1.md:1240`):**
  `SET-01` names `max span`, `max chord`, `target area`, and `target AR` as soft targets minimizing the *"weighted sum of relative deviations"* and specifies that `(area 1000 cm², span 0.8 m, max chord 100 mm)` with span weight `1` and others `0` yields `span = 0.8 m` exactly plus the conflict notice. Without distinguishing one-sided ceilings (`max span`, `max chord`) from two-sided targets (`target area`, `target AR`) and specifying tie-breaking when some weights are `0`, a valid non-conflicting ceiling (`max span 1.10 m` with `S = 1000 cm², AR = 12 → b = 1.0954 m`) could either distort `S`/`AR` or leave `b` under-determined when only `w_b = 1`.
- **Proposed clarification (`DR-SET-3` option a):**
  Define $d_b = \max(0, (b - b_{\max}^*)/b_{\max}^*)$, $d_c = \max(0, (c_{\text{root}} - c_{\max}^*)/c_{\max}^*)$, $d_S = |S - S^*|/S^*$, and $d_{\text{AR}} = |\text{AR} - \text{AR}^*|/\text{AR}^*$. Minimize primary $J_1 = \sum w_i d_i$ over the feasible FoilDSL/Ruling-93 box $\Omega$, breaking exact ties lexicographically by $J_2 = \sum_{w_i=0} d_i$ (supplied zero-weight targets) and then $J_3$ (preset default distance). Both non-conflicting ceilings (`d_i = 0`) and `SET-01`'s conflicting triple (`b = 0.800000 m` exact + `COPY-73`) hold uniquely.

## AM-SET-S3 · `RunRecord.cs:76` `OperatingPoint.Load` and additive `Envelope.setup`

- **Observed open `assume:` (`src/CfdWorkbench.Core/RunRecord.cs:71-75`, `src/CfdWorkbench.Analysis/OperatingPoint.cs:61`):**
  `RunRecord.cs` carries `double? Load` on `OperatingPoint` with an explicit `assume:` awaiting Area 1 confirmation. Meanwhile `NativeProject.Read` (`AuthoringSession.cs:2631-2643`) enforces `Exact(root, [...], ["analysis"])` and `format == RunRecord.Format(runs.Length)`.
- **Proposed clarification (`DR-SET-1`, `DR-SET-2`):**
  1. Confirm `OperatingPoint.Load` is the Goal-state front-wing required load in Newtons (`requiredLoadN`), kept `null` on custom fixed-α runs (preserving all A3a/A3d run keys) and set to `requiredLoadN` on Goal-state load-solved runs once `OperatingPoints.Validate` permits finite positive `Load`.
  2. Persist `SetupBriefVersion[]`, `GoalStateVersion[]`, and `DrcExclusion[]` under one optional top-level `setup` member on `Envelope` (omitted when null so unseeded run-free projects stay `cfdw-project-1` byte-identical to today), updating `NativeProject.Read` (`AuthoringSession.cs:2631, 2641, 2643`) to accept optional `"setup"` and derive `format` via `NativeProject.Format(runCount, hasSetup)` per `DR-SET-1`.

## AM-SET-S4 · `SET-04`: Explicit 8-purpose to 9-discipline-preset mapping

- **Observed cardinality difference (`docs/specs/cfd-workbench-v1.md:236, 991-993, 1243`):**
  A3.1 lists 8 `Setup brief` purposes (`wingfoil freeride`, `wingfoil race`, `wingfoil surf`, `windfoil race`, `windfoil freeride`, `wakefoil`, `surffoil`, `downwind`), while A5.5 lists 9 discipline presets (`surf`, `pump`, `downwind`, `parawing`, `wingfoil freeride`, `wingfoil race`, `windfoil`, `kitefoil`, `e-foil`).
- **Proposed clarification:**
  Record the exact mapping in A5.5 / `SET-04`: `wingfoil race → wingfoil-race`; `wingfoil freeride → wingfoil-freeride`; `surffoil`, `wingfoil surf`, and `wakefoil` → `surf` (with `wakefoil` attaching a `Flagged` boat-wake speed note requiring acknowledgement); `downwind → downwind`; `windfoil race → windfoil` (`iqfoil-2025` benchmark); `windfoil freeride → windfoil` (`open-class`); and keep `pump`, `parawing`, `kitefoil`, and `e-foil` directly selectable in the Setup discipline preset picker so all nine A5.5 presets are reachable offline.
