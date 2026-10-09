---
id: proof-ring-windows-r170-series
title: "Ruling 170 Windows three-ring calibration evidence"
type: proof-pack
status: complete
owner: "@win-r166-ring-baseline"
tags: [windows, ruling-170, ruling-168, ruling-166, calibration, evidence]
links:
  - {to: proof-ring-windows-r170-run-1, rel: relates-to}
  - {to: proof-ring-windows-r170-run-2, rel: relates-to}
  - {to: proof-ring-windows-r170-run-3, rel: relates-to}
  - {to: proof-ring-windows-calibration-attempt-1, rel: relates-to}
review-by: "2026-11-09"
summary: >-
  Three serial Windows rings completed under Ruling 170 with host key pc-win, six-CPU affinity and the reported six-rank
  L3 workload. Their measurements are preserved in calibration-rows.csv and individual receipt/capture directories.
  This is evidence for the Mac-owned baseline file and gate decision; it does not create either.
---

# Ruling 170 Windows three-ring calibration evidence

**Goal:** complete and preserve three serial Windows test-ring measurements under Ruling 170.
**Done when:** all three complete capture envelopes, raw test outputs, expected-failure classifications, timing, drift,
  host measurements, PII checks, and committed-blob identities are available for Mac review.
**Not in scope:** creating `baseline.csv`, setting a Windows gate, or repairing any test failures.
**Tier:** T1. **Fan-out:** 0.

## Measurement and source identity

The three complete rings ran serially with `CFD_RING_HOST=pc-win`, SDK 10.0.203 from `%USERPROFILE%\.dotnet`, six-CPU
affinity `0x3F`, and the coordinator-reported L3 workload at six ranks. All three finished below the 900,000 ms total
capture ceiling. The exact UTC start/end, end load, wall time, exit, CPU samples and affinity observations are in each
run's `measurement.txt`.

The tested heads were `2046a73ca10fe18f9b4ece3de0811dd7a0b40c18`,
`b12d76da40013a1baaa1ff516cc8030d561af74c`, and
`c8e254f789d071f9321b558ae677f445b18f4bec`. Each is a docs/evidence-only descendant of the merged current-main head
`94242628bab7f3189dcfe2a428f903a93394bcef` and retains tested source head
`a248b264e8f9a911cbfd22322b74f737ba203f84` in ancestry. The source/test/tool/project tree was unchanged across all
three rings. The capture hashes record the tested head for each run.

`calibration-rows.csv` contains the requested `(run, end load, wall ms)` values, plus Desktop duration, its `SUITE-TIME`
capture path, and separately named capture-wrapper load samples. `end_load` comes from the terminal `run-tests.sh` line;
`capture_load_start` and `capture_load_end` come from the outer capture process and do not substitute for the harness load.
`Desktop.ms`, all 18 `SUITE-TIME` lines, 17 passing Desktop CatalogDialog/copy checks, nine passing Core `CatalogTests.cs`
checks from Ruling 167/170, and each test's measured WingRun duration are preserved in the corresponding run directory.
The required
`PASS Library_UserFileHeldReader_SurvivesReplaceByRename` appears in Core part 3 for all three rings.

Every run recorded the same five drift names: `lambda-fit`, `tangent-angle`, `dat-rotation`, `handle-polar-section`, and
`handle-polar-target`. Each exact line is retained in the run's `drift-lines.txt`. Per-suite expected/unexpected
classification outputs are retained unchanged; every ring exited 1 on the same unexpected Analysis WingRun failure and
39 unexpected Desktop failures. These test failures were recorded, not repaired. The expected-failure classifier outputs
are the authority for their names and counts.

## Excluded evidence and baseline ownership

The earlier 300 s Ruling 168 attempt remains excluded. It lacked the host override and exceeded that then-current
ceiling; its separate receipt is [the excluded Ruling 168 attempt](../receipt.md). The incomplete 300 s calibration
capture is separately retained under `calibration/` and is not part of the three rows.

Ruling 168 (2) assigns the Mac the creation of `docs/proof/ring-pc-win/baseline.csv` and the host `gate=` decision based
on the observed end loads. This branch supplies the three-run evidence only; neither file nor gate value is authored here.
