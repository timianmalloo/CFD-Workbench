---
id: proof-cbl-measure
title: "CBL Core part balance: measurements"
type: proof-pack
status: active
owner: "@trk-cbl"
phase: implementation
tags: [cbl, core, partition, timing]
links:
  - { to: proof-cbl-red-first, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Per-check cost of the Core harness (two runs), the top 20, and three ring runs with the Core part ms and load.
---
# CBL measure: Core part balance (2026-10-08)

Source: `CFD_CORE_COST=1` makes the Core harness print `COST <name> <ms>` after each passing check (IdentityTests.cs). Two unpartitioned
Release runs (`DOTNET_TieredPGO=0`, one process), load 5.71 and 4.54 at start (`tools/dispatch-gate.py --running 0`: GO). Wall 50.3 s and 50.1 s.
729 checks, 49.8 s of check time (mean of the two runs) in `tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.tsv`.

## Top 20 (mean ms)

| ms | check |
|---:|---|
| 2429.6 | Guard_TwentySevenSpansDiffering_Certifies |
| 1887.2 | GroupGesture_OneMember_EqualsSinglePointGesture |
| 1698.2 | Provenance_SurvivesEveryRewriter |
| 1618.8 | Reopen_LegacyInsertDeleteFairRebuildReceipts_StillCheck |
| 1592.0 | Reopen_Rebuild_AcceptedRoundtripsUndoRedo |
| 1589.1 | Guard_ThirtyThirdPoint_RefusedDslCurveBeforeCopy194 |
| 1564.7 | Rebuild_TenVertices_CertifiedOneUndoItem |
| 1549.1 | Reopen_RecoveryMidRebuild_ResumesSameDraftBytes |
| 1537.6 | PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged |
| 1501.4 | Rebuild_TenVertices_SharesKnotsAbscissaAndPins |
| 1355.3 | Sections_PlaceEqualsSurfaceMidline_Bitwise |
| 907.6 | Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged |
| 892.7 | TipChord_InteriorVerticesAndHandles_NeverChangeEitherChord |
| 749.6 | Placement_RandomFixtures_WithinCertifiedEnclosure |
| 458.6 | Replace_CurrentSpacing_KeepsAngleSmoothSymmetricRows |
| 438.4 | Blend_IdenticalProfileBytes_MatchSingleProfileExample |
| 390.3 | Placement_ProbeChord_EqualsWingEstimatesOnPl0Fixtures |
| 384.1 | SectionDraft_FinishSixSteps_OneAcceptedRow |
| 366.4 | Thickness_UseSource_IndependentMiddle_OnlyMiddleTargetInsideSupport |
| 358.8 | Replace_FourDifferingSections_Certified_SevenRefusedCopy194b |

The top 11 are 18.1 s of 49.8 s. Round-robin by registration index (the old rule) over the table's own costs gives parts of
19106 / 18149 / 12491 ms (red run, `red.log`): the same shape as the OBS line `31550,31613,21928`.

## Core parts, old rule vs cost-aware (ring, parts run concurrently)

| run | load at start -> end | part 1 | part 2 | part 3 | PARTITION-SKEW |
|---|---|---:|---:|---:|---|
| old rule (OBS join) | n/a | 31550 | 31613 | 21928 | printed, skew_ms=9685 |
| new 1 | 1.86 -> 16.45 | 29252 | 29694 | 30251 | none (Desktop failed, unrelated) |
| new 2 | 14.22 -> 19.47 | 31238 | 31285 | 31830 | none |
| new 3 | 8.46 -> 16.34 | 29373 | 29296 | 30423 | none |

Exactly once: run 2's three part logs hold 731 `PASS` lines and no duplicate (729 table rows + 2 new CorePartition checks).
Part ms includes JIT and start-up, so the parts' CPU is not additive to the 50 s single-process run: the ring is CPU-bound.
Largest part: 30.4 s (run 3) against 31.6 s before; under load 14 (run 2) it was 31.8 s. Desktop (41-42 s) stays the ring's critical path.
