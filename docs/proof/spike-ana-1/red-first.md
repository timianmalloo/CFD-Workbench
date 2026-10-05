---
id: proof-spike-ana-1-red-first
title: "SPIKE-ANA-1 red-first probe test"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: design
tags: [analysis, neuralfoil, spike, red-first]
links:
  - { to: proof-spike-ana-1, rel: relates-to }
  - { to: design-area3-analysis, rel: depends-on }
review-by: 2026-11-30
summary: >-
  Records the observed failing Python-oracle test on the placeholder C# probe and its passing commit after the port.
review-suggested: []
---

# SPIKE-ANA-1 red-first record

| Test | Incorrect behavior it catches | Red commit | Green commit |
|---|---|---|---|
| `PythonOracle_Naca0012_Alpha4_Re1e6_Ncrit4` | A placeholder or wrong network cannot reproduce Python NeuralFoil's CL and CD for a fixed CST case. | `90c9e7a`: exit 134; CL=0, CD=0 | `1eb4b3a`: exit 0; `PASS PythonOracle_Naca0012_Alpha4_Re1e6_Ncrit4` |

The cross-language grid comparison and XFOIL accuracy are measured in the rerun script; they are independent oracles, not substitutes for this red-first test.
