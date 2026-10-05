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
| C# inference reproduces Python NeuralFoil 0.3.2 xxxlarge for Cl, Cd, Cm, upper/lower x_tr and analysis_confidence | `NeuralFoil_Fidelity_Python032`: five rows from spike `cases.tsv`/`python.tsv`, max absolute difference 2.4424906541753444e-15; tolerance 1e-10 on macOS arm64 | `4840786`, compile red before port | Verified for the five shared-CST cases; Windows not measured; this does not validate Cm or x_tr against XFOIL |
| The model resource is intact and identical to the pinned wheel conversion | `NeuralFoil_CorruptWeights_Refused` catches `ANA-POLAR-WEIGHTS-HASH`; `python3 tools/check-neuralfoil-weights.py` PASS with wheel e0067af2… and converted e037bd2b… (5,717,832 B) | `4840786`; wheel comparison independently fails on a bad hash | Verified on this wheel; offline empty cache exits 2 with `NOT RECORDED` |
| Every section receives a CST fit residual; the guard names each refused limit | `NeuralFoil_Envelope_Alpha/Re/Ncrit/Family/CstResidual`; each has a reason and finite residual. `NeuralFoil_Source_NoncomputableNamesReason` catches silent null | `4840786`; `830130b` for interface reason | Verified for fixture geometry; real edited sections require the resolver seam |
| Inside the probe grid but outside the XFOIL-validated bracket, prediction stays visible and flagged | `NeuralFoil_Envelope_OutsideValidatedBracket`: NACA 2412, Ncrit 5, α 3°, Re 5e5 is computed with flag | `4840786` | Verified as computation only; no XFOIL accuracy claim |
| Low advisory confidence blocks a sample and stays available in the detailed result | `NeuralFoil_Confidence_BelowFloorNonComputable`, 0.49 blocked and 0.50 admitted | `3ff82f9` | Verified boundary; floor 0.5 is an explicit assumption, not an accuracy threshold established by the spike |
| Inference cost and normal-path telemetry are observable | selected harness `COST NeuralFoil_InferencePerCall 2.169` ms; `NeuralFoil_Telemetry_EmittedOnNormalPath` observed one call and duration | `4840786`; `3ebc929` | Verified on macOS arm64 under this run's load; no Windows measurement |
| Notice and XFOIL exclusion gates fail on violations | `check-notices.py`: planted missing NeuralFoil entry exit 1; planted tracked `xfoil/binary` exit 1; planted missing C# weights constant exit 1; clean exit 0 | planted faults, uncommitted | Verified for gate inputs and current tracked paths |

## Model and envelope

The polar sample grain remains the design's `(profile revision hash, method id/version, Re, Ncrit, surface state, alpha,
water hash)`; coefficients and confidence are non-additive. The source is transient and does not add a second durable
definition. `NeuralFoilPolarSource.Method` supplies `NeuralFoil` and
`NeuralFoil-0.3.2/xxxlarge/94638c04/weights=5717832B` for run keys.

The hard guard uses the spike's observed fidelity grid: α −6…+6 degrees, Re 2e5…1e6, Ncrit 2…9, families NACA
0012/2412/4412, and CST maximum ordinate residual ≤ 3.6e-4 c (the largest measured was 3.58e-4 c). Only NACA 0012
with Ncrit exactly 2, 4 or 9 has the XFOIL validation claim, and then only Cl/Cd at pre-stall conditions. The
guard computes the fitted residual before it returns any refusal. `analysis_confidence` is advisory, not an error bar.

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
