---
id: proof-hyg-core-cost
title: "HYG Core partition hints: measurements"
type: proof-pack
status: active
owner: "@trk-hyg"
phase: implementation
tags: [hyg, core, partition, timing]
links:
  - { to: proof-etc-c2-cost, rel: relates-to }
  - { to: proof-hyg-red-first, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Core part hints were stale (53 of 796 checks unlisted); re-measured from three whole-harness runs. Predicted skew by measured mean 2950 ms before, 34 ms after. No cap changed.
---

# HYG Core partition hints

Method as `docs/proof/etc/c2-cost.md`: three unpartitioned runs, `CFD_CORE_COST=1 dotnet run -c Release --no-build --project tests/CfdWorkbench.Core.Tests`,
then `python3 tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.py core1.log core2.log core3.log` (mean ms per check).

Same checks: 796 PASS and 0 FAIL in each of the three runs; the file lists 796 rows after, 743 before (53 checks were unlisted and fell back to
`registration index % 3`).

Ring reading before (task brief): `PARTITION-SKEW Core parts=29865,36369,36806` ms (spread 6941 ms; threshold 4500 ms).

Predicted split, replaying the longest-first assignment on the measured means (`scratch/hyg/skew.py`):

| Table | Part 1 | Part 2 | Part 3 | Spread |
|---|---|---|---|---|
| before (743 rows) | 17114 | 19533 | 20064 | 2950 |
| after (796 rows) | 19381 | 19347 | 19361 | 34 |

These are in-process check sums, not part walls (a wall adds process start and fixture setup); the ring's own PARTITION-SKEW reading after is in
the Return. No cap, limit or `run-tests.sh` job list changed.

Ring reading after (`tools/run-tests.sh`, exit 0): Core parts 36246 / 35279 / 35212 ms wall, 362 + 217 + 217 = 796 PASS, no PARTITION-SKEW line (spread 1034 ms).
