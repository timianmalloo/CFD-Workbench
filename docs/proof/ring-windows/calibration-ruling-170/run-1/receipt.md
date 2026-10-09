---
id: proof-ring-windows-r170-run-1
title: "Ruling 170 Windows calibration run 1 (complete)"
type: proof-pack
status: complete
owner: "@win-r166-ring-baseline"
tags: [windows, ruling-170, ruling-168, ruling-166, test-ring, evidence]
links:
  - {to: proof-ring-windows-calibration-run-1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: "2026-11-09"
summary: >-
  The first Ruling 170 ring completed within the 900 s total capture envelope on the current merged main head. It used
  CFD_RING_HOST=pc-win, six-CPU affinity and the reported six-rank L3 workload. The ring exited 1 on classified Windows
  test failures; the completed measurement qualifies as calibration row 1, pending the three-run series.
---

# Ruling 170 Windows calibration run 1

**Goal:** complete the first Windows calibration ring with the Ruling 170 host key and capture ceiling.
**Done when:** preserve the full ring output, suite logs, classifications, timing, drift, environment measurements and
  committed-blob hashes; confirm the required held-reader PASS by name.
**Not in scope:** repairing test failures, changing product/tests/tools, or writing a baseline row before all three runs.
**Tier:** T1. **Fan-out:** 0.

## Result

The ring completed in **344,129 ms** (`run-tests.sh` reported 337 s; wall value 336,392 ms), with a **344,114.214 ms**
capture UTC envelope. It exited **1** after all harness jobs completed and stayed under Ruling 170's 900,000 ms total
ceiling. This is a complete ring measurement and qualifies as calibration row 1. The baseline CSV remains unwritten until
the three serial rings are complete.

The capture started at `2026-10-09T00:55:09.6472501Z` and ended at `2026-10-09T01:00:53.7614636Z`. Ring load was
**11.15 → 9.13**; the final cost checker sampled **12.18**. Windows total-CPU samples were 34.9%, 34.5%, 34.9% before
and 36.7%, 34.2%, 34.3% after. Capture process and Git Bash launcher affinity were `0x3F`; `process-affinity.txt` retains
the process rows and marks two descendants unreadable. .NET SDK **10.0.203** came from `%USERPROFILE%\.dotnet`, and
`CFD_RING_HOST=pc-win` was set. The coordinator reported L3 active at six ranks; continuous residency was not independently
sampled.

Tested HEAD was `2046a73ca10fe18f9b4ece3de0811dd7a0b40c18`, a merge containing the current main head `94242628` and the
reachable Windows-tested source head `a248b264`. The only worktree changes before the ring were proof helper/receipt
documentation; no source, test, project, build, or tool code changed.

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
Analysis failure was `Section_WingRun_PanelValuesAtEveryStation` in part 1; its recorded duration was **1,301.409 ms**.
Desktop's 39 unexpected failures and their full details remain in `classifications/Desktop.txt` and `tmp-tests/Desktop.log`.
The harness failures are recorded as evidence; no test or product repair was made.

Desktop measured **304,344 ms** (`Desktop.ms`). All 18 `SUITE-TIME` lines are retained in `suite-time-lines.txt`. The
separate `catalog-pass-lines.txt` contains 17 passing catalog checks. The final test-cost report was **0 failures, 28
COST-MISS** at end load 12.18; these advisory host costs are retained in `console.stdout.txt`.

All five observed drift lines are in `drift-lines.txt` and their Core logs:

```text
DRIFT lambda-fit max_abs=0 limit=1e-06
DRIFT tangent-angle max_abs=0 limit=1e-06
DRIFT dat-rotation max_abs=0 limit=1e-06
DRIFT handle-polar-section max_abs=0 limit=1e-06
DRIFT handle-polar-target max_abs=6.1232339957367663E-18 limit=1e-06
```

The complete raw logs, captured `.tmp-tests`, classifications, measurements and derived summaries are hash-bound by the
parent `docs/proof/ring-windows/capture-manifest.json`. The manifest and PII gate are checked after commit.
