---
id: proof-a3a-cor-red-first
title: "A3a COR red-first receipt"
type: proof-pack
status: active
owner: "@track-cor"
phase: implementation
tags: [a3a, cor, placement, sections, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The red runs of the COR track: both named tests failed on the skeleton, the bitwise test failed when Sections
  placed camber with its own formula, and the counter test failed when the Interlocked increments were restored to ++.
---

# A3a COR red-first receipt

Design: `docs/design/area3-analysis.md` §18.2 (COR row), §18.8. Session `track-cor`, branch `feature/a3a-cor`, 2026-10-04.
Model: Grok 4.7. Green body: `a40d78b`. Parent `b7aa706` still throws `NotImplementedException("COR: Placement.Sections")`.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| `Sections_PlaceEqualsSurfaceMidline_Bitwise` | skeleton body, before `a40d78b` | `FAIL Sections_PlaceEqualsSurfaceMidline_Bitwise NotImplementedException: COR: Placement.Sections` | body landed, `PASS` |
| `Placement_Counters_ExactUnderConcurrentSections` | same skeleton | `FAIL Placement_Counters_ExactUnderConcurrentSections NotImplementedException: COR: Placement.Sections` | `PASS` |
| `Sections_PlaceEqualsSurfaceMidline_Bitwise` | `SampleSection` places the camber with a local formula (`leading + chord * (x cos + z sin)`), not `PlacementRule.Place` | `FAIL Sections_PlaceEqualsSurfaceMidline_Bitwise InvalidOperationException: example.foil s1 i1 Z bits 3e9fbe61aa436800 -> 3e9fbe61aa43698c` | mutant reverted, `PASS` |
| `Placement_Counters_ExactUnderConcurrentSections` | `Interlocked.Increment` restored to `++` on both counters | `FAIL Placement_Counters_ExactUnderConcurrentSections InvalidOperationException: Expected 116160; actual 84407` | mutant reverted, `PASS` |

The two mutants were applied on `a40d78b` and reverted. They are not commits. The skeleton red lines were observed in the working tree before `a40d78b`; that commit is the first green tree.

**Why the placed point is the midline of the two `Place` calls.** `PlacementRule.Place` of the averaged camber is not the drawn midline. On `example.foil`, station 1, sample 1, the Z bits differ by 396 ulps: `3e9fbe61aa436800` is 4.730176590535479e-07 m and `3e9fbe61aa43698c` is 4.7301765905356887e-07 m (difference 2.096e-20 m). The midline of `Place(zu)` and `Place(zl)` matches `Surface` bit for bit, and that is what `Sections` returns. The local formula above is `Place` of the averaged camber written out by hand, and the bitwise test rejects it.

**Counters.** One `Sections` call on `example.foil` at five η and 17 cosine stations increments the channel counter by 25 and the profile counter by 7260. Eight threads, two repeats each: channel 400, profile 116160, both exact with `Interlocked`. With `++` restored, the same run kept channel 400 and lost profile updates (84407). `Placement_Surface_EvaluatorCallsBounded` still reads 205 channel evaluations (5 × 41 stations) and 7798 profile evaluations. Those assertions were not changed.

**Pins.** `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` and `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged` passed on this tree. The surface-bit pin is the file at PRE commit `1b42ee6`, already in the base.
