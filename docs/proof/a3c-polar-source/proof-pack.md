---
id: proof-a3c-polar-source
title: "A3c-1 production NeuralFoil polar source proof"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, neuralfoil, polar, cst, weights, licence, ruling-85]
links:
  - { to: proof-spike-ana-1, rel: depends-on }
  - { to: design-area3-analysis, rel: implements }
  - { to: coordination-round-oct05, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Ports the xxxlarge NeuralFoil 0.3.2 model into an integrity-checked in-process polar source, with CST fit,
  envelope refusals, measured fidelity, pinned wheel conversion and third-party notice gates. The source is
  not yet wired into the analysis service or UI.
review-suggested: []
---

# Proof Pack: A3c-1 NeuralFoil polar source

**Contract:** Rulings 63/85, SPIKE-ANA-1 verdict Conditions for A3c, area3-analysis §5.1/5.4–5.6/6,
DR-ANA-1. **Tier:** T2. **Branch:** `feature/a3c-polar-source`.

## Claims and evidence

| Claim | Oracle and observed result | Red | Confidence and limit |
|---|---|---|---|
| C# inference reproduces Python NeuralFoil 0.3.2 xxxlarge for Cl, Cd, Cm, upper/lower x_tr and analysis_confidence | `NeuralFoil_Fidelity_Python032`: five rows from spike `cases.tsv`/`python.tsv`, max absolute difference 2.4424906541753444e-15; tolerance 1e-10 on macOS arm64 | `a79bd8c`, compile red before port | Verified for the five shared-CST cases; Windows not measured; this does not validate Cm or x_tr against XFOIL |
| The model resource is intact and identical to the pinned wheel conversion | `NeuralFoil_CorruptWeights_Refused` catches `ANA-POLAR-WEIGHTS-HASH`; `python3 tools/check-neuralfoil-weights.py` PASS with wheel e0067af2… and converted e037bd2b… (5,717,832 B) | `a79bd8c`; wheel comparison independently fails on a bad hash | Verified on this wheel; offline empty cache exits 2 with `NOT RECORDED` |
| Every section receives a CST fit residual; a point outside the network training range is non-computable with a reason | `NeuralFoil_Envelope_{Alpha,Re,Ncrit}_OutsideTraining_NonComputable`, `_CstResidual`; each has a reason and a finite residual. `NeuralFoil_Source_NoncomputableNamesReason` catches silent null | `a79bd8c`; `836264c` for the interface reason; `3806278` for the training-range tests | Verified for fixture geometry; the training range is cited from the repo knowledge doc (paper values), not re-read from upstream |
| Inside the training range but outside the XFOIL-validated bracket (alpha, Re, Ncrit, section geometry) the point is computed and flagged | `NeuralFoil_Envelope_*_InsideTraining_ComputedFlagged`, `_Ncrit_IntermediateValue_Flagged`, `_InsideBracket_NotFlagged`, `_OutsideValidatedBracket` | `3806278` | Verified as computation only; no XFOIL accuracy claim |
| The validated family is derived from geometry; an edited NACA 0012 loses it | `NeuralFoil_Family_EditedNaca0012_LosesValidatedFamily`, `_CatalogNaca0012_DerivedNotLabelled`; spike-open section differs 1.26e-3 c from the catalog, edited 3.87e-3 c, tolerance 1.5e-3 c | `69bf9de` | Verified; a tolerance between 1.26e-3 and 3.87e-3 is a chosen limit, and the margin to the spike-open section is thin |
| `analysis_confidence` is advisory: low values are computed and flagged, never refused | `NeuralFoil_Confidence_Low_ComputedFlaggedNeverRefused` (alpha 27, Re 1e3, Ncrit 0: Python 6.7e-6) | `e175525` | Verified; the 0.5 threshold only places the flag, with no accuracy evidence behind it |
| Section coordinates through CstFit and the network match Python end to end | `NeuralFoil_EndToEnd_SectionToPolar_Python032`: 6 rows, max difference 7.8e-4, tolerance 2e-3 (a fit-path difference) | not red observed | Verified on macOS arm64; Windows not measured |
| Inference cost and normal-path telemetry are observable | selected harness `COST NeuralFoil_InferencePerCall 2.169` ms; `NeuralFoil_Telemetry_EmittedOnNormalPath` observed one call and duration | `a79bd8c`; `6053d79` | Verified on macOS arm64 under this run's load; no Windows measurement |
| Notice and XFOIL exclusion gates fail on violations | `check-notices.py`: planted missing NeuralFoil entry exit 1; planted tracked `xfoil/binary` exit 1; planted missing C# weights constant exit 1; clean exit 0 | planted faults, uncommitted | Verified for gate inputs and current tracked paths |

## Model and envelope

The polar sample grain remains the design's `(profile revision hash, method id/version, Re, Ncrit, surface state, alpha,
water hash)`; coefficients and confidence are non-additive. The source is transient and does not add a second durable
definition. `NeuralFoilPolarSource.Method` supplies `NeuralFoil` and
`NeuralFoil-0.3.2/xxxlarge/94638c04/weights=5717832B` for run keys.

The hard refusals are the network training range (alpha −27.9…+28.6 degrees, Re 1e2…1e10, Ncrit 0…18; source
`docs/knowledge/hydrofoil-workbench/07-low-order-hydrodynamics.md:38`, citing the NeuralFoil paper) and a CST maximum
ordinate residual above 3.6e-4 c (the largest measured was 3.58e-4 c). The XFOIL-validated bracket is NACA 0012 geometry,
Re 2e5…1e6, Ncrit exactly 2, 4 or 9, alpha −6…+6 degrees, Cl/Cd at pre-stall conditions; a computed point outside it
carries a reason per axis in `OutsideBracketReasons`. NACA 0012 is recognised from the fitted geometry against the
catalog section, never from a caller's family string. `analysis_confidence` is advisory: below 0.5 a warning is attached
and the point is still computed; it is not an error bar and not an accuracy statement.

An embedded .NET assembly resource is chosen because the same manifest-resource lookup and hash check runs on macOS
and Windows, and packaging cannot silently omit a loose adjacent file. The source has no Python process, CasADi,
IPOPT or XFOIL dependency. `THIRD-PARTY-NOTICES.md` carries the full MIT notice and training-data statement; its About
link is a Desktop seam.

## Surface reach and seams

| Surface | Reached here | Reader |
|---|---|---|
| Embedded weights | Analysis project resource | `NeuralFoilNetwork.FromEmbedded` verifies SHA-256 before parsing |
| Section model | `NeuralFoilSection` and `CstFitResult` | `NeuralFoilPolarSource.Evaluate` |
| Polar point | `NeuralFoilEvaluation` and `PolarSample` | `IPolarSource.Sample` and test harness |
| Stored run, service, projection, UI | Not owned by A3c-1 | A3c-2 needs the residual, bracket flag, refusal reason and method identity seams |

`IPolarSource.Sample` has no result field for the residual, outside-bracket flag, or a per-point refusal reason. This
implementation throws a stable `ContractError` with the reason on a non-computable sample and exposes all fields on
the concrete `Evaluate` result. The interface documentation and consuming service should be reconciled before wiring.
The resolver also must use the accepted profile revision, not its original catalog profile.

## Testing, costs and residual risks

Triggered strategy: D0 deterministic cases; D1 calculation and boundary checks; D2 cross-language numeric oracle;
D3 assembly/resource boundary; D4 integrity failure; D6/A1 not triggered (no UI or LLM composition in this track).
The wheel gate uses only Python's standard library and the downloaded wheel. It repeats the spike converter's exact
array order, dtype and little-endian binary layout. It is in the readiness ring; a cached run took about 0.12 s here,
first network download cost is not recorded. The notices gate is also readiness and costs about 0.04 s locally.

Residual risks: Windows fidelity not measured; NACA 2412/4412 and intermediate Ncrit are fidelity-only; the 0.5
confidence floor is an assumption; family identity depends on the future accepted-revision resolver; source results
are not yet persisted or rendered. The five-case fixture checks six requested outputs, not the 192 boundary-layer
outputs of the spike's wider grid. The two extra fit/source tests in `red-first.md` were added after implementation.

The About-box notice link and the run/result contract fields are seam requests. The label copy proposal is:
“surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only”.

## Close-out findings (trk-a3c1 takeover)

- The first full ring was red on `PlacementRule_RadiansConstant_SingleSiteInSource` (`Math.PI / 180` in `NeuralFoilNetwork.cs`). Fixed by calling the Analysis assembly's `VortexLattice.ToRadians`; after the fix the full ring (124 Analysis checks, 0 COST-MISS), `check-docs.py` and all 12 verify gates pass.
- `verify-portable-text-io.py` flagged both new gate scripts (no stdio guard); fixed in this close-out.
- Analysis harness 4.3 s under load 36 to 40 against the 5 s C-2 limit; `COST NeuralFoil_InferencePerCall 1.901` ms.
- Offline: `check-neuralfoil-weights.py --offline --cache-dir <empty>` prints `NOT RECORDED` and exits 2 (a red readiness
  entry offline, by design); with a warm cache it passes without network.

## Repair cycle 1 (numerical-verification review conditions)

1. Envelope: training-range refusal, per-axis bracket flag (`3806278` red, `47d4716` green).
2. Confidence advisory (`e175525` red, `d9f2f33` green). The same fix removed a mismatch: the network input guard refused
   Ncrit 0, which is inside the training range.
3. End-to-end fixture `NeuralFoilE2eTable.cs` generated by `Fixtures/neuralfoil/generate_e2e.py` with neuralfoil 0.3.2 and
   aerosandbox 4.2.9 under `~/dev/sim/.venv`; tolerance 2e-3, observed 7.8e-4. The test header says the case table is a
   copy of the spike `cases.tsv`/`python.tsv`.
4. Family derived from geometry (`69bf9de` red, `4934147` green); no seam needed because the catalog generator is public in Core.
5. `IPolarSource.Sample` documentation now states the current per-source behaviour.

## Tracked items

| Item | State |
|---|---|
| Windows fidelity (inference and end-to-end) | **Not measured, pending** |
| A3c-2 must reconcile `Sample`'s contract (null versus `ContractError`) before any caller wires it | Open |
| `PolarSample` has no field for the outside-bracket reasons, the low-confidence warning or the CST residual; only `NeuralFoilPolarSource.Evaluate` carries them | Open, A3c-2 seam |
| Resolver must return the accepted profile revision (not the catalog ancestor) | Open, A3c-2 |
| Training range cited from the repo knowledge doc, not re-read from upstream | Open, confirm against arXiv 2503.16323 |
| NACA 0012 match tolerance 1.5e-3 c is a chosen limit | Open, revisit with the wider-bracket spike |
