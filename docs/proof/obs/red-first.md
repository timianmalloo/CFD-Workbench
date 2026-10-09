---
id: proof-obs-red-first
title: "OBS ring observability: red-first record"
type: proof-pack
status: active
owner: "@trk-obs"
phase: implementation
tags: [obs, stage, spawn, partition-skew, ring]
links:
  - { to: proof-abl-red-first, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red and green runs for STAGE elapsed_ms, SPAWN-START, and the PARTITION-SKEW advisory.
---

# Track OBS red-first record (2026-10-09)

Checks: `tests/CfdWorkbench.Desktop.Tests/StageTimingTests.cs` (Desktop, in-process, fast ring, under 5 ms) and the
`self_test_skew` cases in `tools/check-test-costs.py --self-test` (fast ring, under 1 ms).

Red runs used the old behaviour behind the new seams (`DesktopChecks.Stage` printing `STAGE <name>` only; `SpawnWith` with no
start line; `partition_skew` returning an empty list).

| Behaviour | Red (old code) | Green (new code) |
|---|---|---|
| STAGE line carries `elapsed_ms` | `CFD_TEST_ONLY=StageLine_ ...`: `FAIL StageLine_CarriesElapsedMs_KeepsPrefixAndName Exception: STAGE line has no elapsed_ms after the stage name: STAGE native-review-options` | `PASS StageLine_CarriesElapsedMs_KeepsPrefixAndName`; real lines `STAGE SelfLaunchTests elapsed_ms=29` |
| One SPAWN-START per mode, in start order | `CFD_TEST_ONLY=Spawn_Prints ...`: `FAIL Spawn_PrintsOneSpawnStartPerMode_InStartOrder Exception: expected one SPAWN-START per mode, got: ` | `PASS Spawn_PrintsOneSpawnStartPerMode_InStartOrder` |
| PARTITION-SKEW printed when skewed, silent when balanced | `check-test-costs.py --self-test`: exit 1, `SELFTEST 53/56 cases`; three skew cases FAIL (Analysis 2000 ms, Analysis 751 ms, Core 6000 ms), balanced and missing-reading cases pass | exit 0, `SELFTEST 56/56 cases` |

Note: the first green Spawn run failed on the test's own modes (`--part=k/n` modes with no PARTITION lines trip Spawn's partition
check); the test now uses plain modes. That was a test defect, not a product defect.
