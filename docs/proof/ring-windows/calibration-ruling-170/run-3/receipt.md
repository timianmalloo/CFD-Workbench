---
id: proof-ring-windows-r170-run-3
title: "Ruling 170 Windows calibration run 3 (complete)"
type: proof-pack
status: complete
owner: "@win-r166-ring-baseline"
tags: [windows, ruling-170, ruling-168, ruling-166, test-ring, evidence]
links:
  - {to: proof-ring-windows-r170-run-2, rel: relates-to}
  - {to: proof-ring-windows-calibration-run-1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: "2026-11-09"
summary: >-
  The third Ruling 170 ring completed within the 900 s total capture envelope on tested HEAD c8e254f7. It used
  CFD_RING_HOST=pc-win, six-CPU affinity and the reported six-rank L3 workload. It exited 1 after classified Windows
  test failures and qualifies as calibration row 3, completing the three-run measurement series.
---

# Ruling 170 Windows calibration run 3

**Goal:** complete the third Windows calibration ring with the Ruling 170 host key and capture ceiling.
**Done when:** preserve the full ring output, suite logs, classifications, timing, drift, environment measurements and
  committed-blob hashes; confirm the required held-reader PASS by name.
**Not in scope:** repairing test failures, changing product/tests/tools, or setting a host baseline gate.
**Tier:** T1. **Fan-out:** 0.

## Result

The ring completed in **332,976 ms** (`run-tests.sh` reported 326 s; wall value 325,651 ms, net 309,376 ms), with a
**332,958.98 ms** capture UTC envelope. It exited **1** after all harness jobs completed and stayed under Ruling 170's
900,000 ms total ceiling. This is a complete ring measurement and qualifies as calibration row 3.

The capture started at `2026-10-09T01:13:12.7758453Z` and ended at `2026-10-09T01:18:45.7348257Z`. The terminal
`run-tests.sh` load was **6.29 → 11.80**; **11.80** is the baseline end-load value. The separate capture-wrapper samples
were **10.95 → 12.49**. Windows total-CPU samples were 33.1%, 38.0%, 37.8% before
and 46.1%, 34.2%, 36.0% after. Capture process and Git Bash launcher affinity were `0x3F`; `process-affinity.txt` retains
the process rows. .NET SDK **10.0.203** came from `%USERPROFILE%\.dotnet`, and `CFD_RING_HOST=pc-win` was set. The
coordinator reported L3 active at six ranks; continuous residency was not independently sampled.

Tested HEAD was `c8e254f789d071f9321b558ae677f445b18f4bec`, which includes the first two complete ring captures and has no
product, source, test, project, build or tool code changes.

## Harness classification

The exact `check-expected-failures.py --host windows` outputs are preserved in `classifications/`:

| Suite | Expected failures | Unexpected failures |
|---|---:|---:|
| Analysis.part1of2 | 0 | 1 |
| Analysis.part2of2 | 0 | 0 |
| Cli | 2 | 0 |
| Core.part1of3 | 8 | 0 |
| Core.part2of3 | 11 | 0 |
| Core.part3of3 | 9 | 0 |
| Desktop | 2 | 39 |

The required `PASS Library_UserFileHeldReader_SurvivesReplaceByRename` appears in `Core.part3of3.log`. The unexpected
Analysis failure was `Section_WingRun_PanelValuesAtEveryStation` in part 1; its recorded duration was **1,338.166 ms**.
Desktop's 39 unexpected failures and their full details remain in `classifications/Desktop.txt` and `tmp-tests/Desktop.log`.
The harness failures are recorded as evidence; no test or product repair was made.

Desktop measured **302,315 ms** (`Desktop.ms`). All 18 `SUITE-TIME` lines are retained in `suite-time-lines.txt`. The
`catalog-pass-lines.txt` contains 17 passing Desktop CatalogDialog/copy checks. Separately,
`core-catalog-pass-lines.txt` records the nine Core `CatalogTests.cs` PASS lines requested by Ruling 167/170. The final
test-cost report was **0 failures, 25 COST-MISS** at end load 11.80; these advisory host costs are retained in
`console.stdout.txt`.

All five observed drift lines are in `drift-lines.txt` and their Core logs:

```text
DRIFT lambda-fit max_abs=0 limit=1e-06
DRIFT tangent-angle max_abs=0 limit=1e-06
DRIFT dat-rotation max_abs=0 limit=1e-06
DRIFT handle-polar-section max_abs=0 limit=1e-06
DRIFT handle-polar-target max_abs=0 limit=1e-06
```

The complete raw logs, captured `.tmp-tests`, classifications, measurements and derived summaries are hash-bound by the
parent `docs/proof/ring-windows/capture-manifest.json`. The manifest and PII gate are checked after commit.
