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

The first nine tests were committed in `4840786` before the production namespace existed. The selected Analysis
harness exited 1 with CS0234/CS0246, then the same tests printed PASS after `dfa1502`. The failing build establishes
that the implementation was absent; it does not by itself establish mutation sensitivity for each assertion.

| Test | Incorrect behavior caught | Red commit and observation | Green commit |
|---|---|---|---|
| `NeuralFoil_Fidelity_Python032` | Wrong forward pass or output scaling against five Python cases | `4840786`, missing namespace, exit 1 | `dfa1502`, max absolute difference 2.44e-15 |
| `NeuralFoil_CorruptWeights_Refused` | Tampered resource accepted | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS with `ANA-POLAR-WEIGHTS-HASH` |
| `NeuralFoil_Envelope_Alpha` | Out-of-range alpha computed | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_Envelope_Re` | Out-of-range Re computed | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_Envelope_Ncrit` | Out-of-range Ncrit computed | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_Envelope_Family` | An unprobed family computed | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_Envelope_CstResidual` | A high-fit-error section computed | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_Envelope_OutsideValidatedBracket` | A cambered/intermediate-Ncrit result loses its flag or is dropped | `4840786`, missing namespace, exit 1 | `dfa1502`, PASS |
| `NeuralFoil_InferenceCost` | Inference cost not emitted by the harness | `4840786`, missing namespace, exit 1 | `dfa1502`, `COST NeuralFoil_InferencePerCall 2.169` ms in one selected run |
| `NeuralFoil_CstFit_RecoversSection` | CST fit does not recover exact synthetic geometry | **not red observed**; added during `dfa1502` | `dfa1502`, PASS |
| `NeuralFoil_Source_ProducesPolarSample` | Interface loses a coefficient, confidence, method version, or water hash | **not red observed**; added during `dfa1502` | `dfa1502`, PASS |
| `NeuralFoil_Confidence_BelowFloorNonComputable` | Low confidence point is admitted | `3ff82f9`, missing `ConfidenceReason`, exit 1 | `73f2522`, PASS |
| `NeuralFoil_Telemetry_EmittedOnNormalPath` | Call count and duration absent | `3ebc929`, observed `durations=0, calls=0`, exit 1 | `4dfe931`, PASS |
| `NeuralFoil_Source_NoncomputableNamesReason` | Interface silently drops a refused point | `830130b`, observed failure “silently dropped”, exit 1 | `211fe78`, PASS |

The two additional fit/source checks were added after implementation and are recorded as unverified greens. They are
not used alone to claim red-first proof; the earlier residual and interface checks cover the required failure paths.
