---
id: proof-a3c-polar-source-red-first
title: "A3c-1 NeuralFoil red-first evidence"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, neuralfoil, polar, red-first]
links:
  - { to: proof-a3c-polar-source, rel: relates-to }
  - { to: proof-spike-ana-1, rel: depends-on }
review-by: 2026-11-30
summary: >-
  Records the observed red and green commits for the NeuralFoil fixture, integrity, envelope, confidence,
  telemetry and non-computable-result checks, including the limits of the first compile-red observation.
review-suggested: []
---

# Red-first record

The first nine tests were committed in `a79bd8c` before the production namespace existed. The selected Analysis
harness exited 1 with CS0234/CS0246, then the same tests printed PASS after `3df9b6e`. The failing build establishes
that the implementation was absent; it does not by itself establish mutation sensitivity for each assertion.

| Test | Incorrect behavior caught | Red commit and observation | Green commit |
|---|---|---|---|
| `NeuralFoil_Fidelity_Python032` | Wrong forward pass or output scaling against five Python cases | `a79bd8c`, missing namespace, exit 1 | `3df9b6e`, max absolute difference 2.44e-15 |
| `NeuralFoil_CorruptWeights_Refused` | Tampered resource accepted | `a79bd8c`, missing namespace, exit 1 | `3df9b6e`, PASS with `ANA-POLAR-WEIGHTS-HASH` |
| `NeuralFoil_Envelope_{Alpha,Re,Ncrit}_OutsideTraining_NonComputable` | A point outside the network training range is computed instead of refused | `3806278`, compile error (no `OutsideBracketReasons`), exit 1 | `47d4716`, PASS |
| `NeuralFoil_Envelope_{Alpha,Re,Ncrit}_InsideTraining_ComputedFlagged`, `_Ncrit_IntermediateValue_Flagged` | A point inside the training range but outside the validated bracket is refused or loses its flag | `3806278`, compile error, exit 1 | `47d4716`, PASS |
| `NeuralFoil_Envelope_InsideBracket_NotFlagged` | A validated point is flagged | `3806278`, compile error | `47d4716`, PASS |
| `NeuralFoil_Confidence_Low_ComputedFlaggedNeverRefused` | Low `analysis_confidence` refuses the point, or is described as accuracy | `e175525`, compile error (no `LowConfidence`), exit 1 | `d9f2f33`, PASS; also needed `ncrit >= 0` in the network input guard |
| `NeuralFoil_Family_EditedNaca0012_LosesValidatedFamily` | An edited section labelled `naca0012` keeps the validated claim | `69bf9de`, compile error (no `Naca0012Reference`), exit 1 | `4934147`, PASS; measured match 1.26e-3 spike-open, 3.87e-3 edited, tolerance 1.5e-3 |
| `NeuralFoil_Family_CatalogNaca0012_DerivedNotLabelled` | The catalog NACA 0012 is not recognised from geometry | `69bf9de`, compile error | `4934147`, PASS |
| `NeuralFoil_EndToEnd_SectionToPolar_Python032` | CstFit or the fit-to-network path diverges from Python's | **not red observed**; first run exceeded a 1e-4 guess, tolerance then set from the measurement | `8878ac4`, PASS, max difference 7.8e-4, tolerance 2e-3 |
| `NeuralFoil_Envelope_CstResidual` | A high-fit-error section computed | `a79bd8c`, missing namespace, exit 1 | `3df9b6e`, PASS |
| `NeuralFoil_Envelope_OutsideValidatedBracket` | A cambered/intermediate-Ncrit result loses its flag or is dropped | `a79bd8c`, missing namespace, exit 1 | `3df9b6e`, PASS |
| `NeuralFoil_InferenceCost` | Inference cost not emitted by the harness | `a79bd8c`, missing namespace, exit 1 | `3df9b6e`, `COST NeuralFoil_InferencePerCall 2.169` ms in one selected run |
| `NeuralFoil_CstFit_RecoversSection` | CST fit does not recover exact synthetic geometry | **not red observed**; added during `3df9b6e` | `3df9b6e`, PASS |
| `NeuralFoil_Source_ProducesPolarSample` | Interface loses a coefficient, confidence, method version, or water hash | **not red observed**; added during `3df9b6e` | `3df9b6e`, PASS |
| `NeuralFoil_Telemetry_EmittedOnNormalPath` | Call count and duration absent | `6053d79`, observed `durations=0, calls=0`, exit 1 | `448a28f`, PASS |
| `NeuralFoil_Source_NoncomputableNamesReason` | Interface silently drops a refused point | `836264c`, observed failure “silently dropped”, exit 1 | `94d8446`, PASS |

The two additional fit/source checks were added after implementation and are recorded as unverified greens. They are
not used alone to claim red-first proof; the earlier residual and interface checks cover the required failure paths.

Hashes above are the rebased ones (the earlier `4840786`, `dfa1502`, `83ac0ea`, `3ff82f9`, `73f2522`, `748aaed`, `3ebc929`,
`4dfe931`, `830130b`, `211fe78` became `a79bd8c`, `3df9b6e`, `8a4bb5c`, `aaff1a3`, `1dc30a1`, `3697fd2`, `6053d79`,
`448a28f`, `836264c`, `94d8446`). The repair cycle replaced the Alpha/Re/Ncrit/Family refusal tests and the confidence-floor
test; their old rows are removed. The envelope Family refusal no longer exists: family is derived from geometry and only flags.
