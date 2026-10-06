---
id: proof-pnl-timing
title: "PNL timing: adaptive panels and the four whole-wing figures"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, timing, budget]
links:
  - { to: proof-a3bc-seam, rel: relates-to }
  - { to: proof-pnl-step0-other-stations, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Re-measured Section_CamberedWing129_WarmTime with the governing 400-panel estimate, and reconciled the
  419 / 723 / 430 ms and 1.6 s whole-wing figures by workload.
review-suggested: []
---

# PNL timing (Ruling 103)

Track `trk-pnl`, 2026-10-06, macOS arm64 (16 logical CPUs, load average 3–4 at the start), .NET 10.
Check: `Section_CamberedWing129_WarmTime` (`SectionSeamTests.cs`): NACA 2412 (12 %, 2 % camber), 129 stations
(`Settings.Default`), α 3°, `Fixture.Salt`, no strips, `Evaluate` called twice in one process: the first call is the
"first run", the second the "warm" one. Command:
`CFD_TEST_ONLY=Section_CamberedWing129_WarmTime tools/run-suite.sh dotnet <Analysis dll> --readiness`.
The first-run figure was added to the MEASURE line by this track.

## Measured now

| Code | Build | Load | Runs | Warm (ms) | First run (ms) | Panels |
|---|---|---|---|---|---|---|
| Before (200 everywhere; the old code already ran one 400 solve at the governing station for the delta) | Release | quiet | 3 | 435.8, 433.4, 494.7 | not recorded by the old check | 200 + 1 × 400 solve |
| After (200 everywhere, governing station re-solved at 400 as the full estimate) | Release | quiet | 3 | 432.0, 440.9, 487.6 | 523.5, 534.3, 528.6 | 200 + 1 × 400 estimate |
| After | Release | 8 parallel copies on 16 CPUs | 8 | 436.5–495.7 | 512–539 | same |
| After | Release | 32 parallel copies on 16 CPUs (2× oversubscribed) | 32 | 989–1,108 (min, median, max of the sort) | 1,442–1,530 | same |
| **Ruling 114** (width max(2u, 25 %), thinnest-station slot; same cap of 4) | Release | quiet | 2 | 551.2, 501.8 | 596.8, 590.4 | up to 4 x 400 estimate, as in cycle 1; no new budget |
| **Cycle 1** (Ruling 110: governing plus up to 3 near-tie stations at 400) | Release | quiet | 3 | 509.8, 493.3, 537.1 | 593.8, 583.7, 578.6 | 200 + up to 4 × 400 estimate; on this uniform fixture all 4 tie, the worst case |
| After | Debug | quiet | 3 | 1,973–2,001 | 1,992–1,996 | same |

Cycle 1 (Ruling 110) adds up to three more 400-panel estimates: warm 493-537 ms, about +60 ms, as the ruling predicted
(+64 ms), still under 1 s on a quiet Release machine. The cycle-0 reading below is kept for the reconciliation.

Result (cycle 0): the governing 400-panel estimate costs nothing measurable. The old code already did one 400-panel solve at the
governing station (to measure the delta); the new code runs the full estimate there (for a cambered foil, a few more
right-hand sides from one factorization), and the difference is inside run-to-run noise (about ±30 ms). Quiet Release
warm stays under the 1 s budget at 432–488 ms. The readiness check prints the figure but does not assert 1 s.
Under a 2× oversubscribed machine the same code reads about 1.0–1.1 s warm and about 1.5 s on the first call.

## Reconciling the four figures

| Figure | Workload | Reproduced? | Reading |
|---|---|---|---|
| 419 ms | readiness MEASURE line, 2026-10-06: cambered 2 %, warm, 129 stations, Release, 200 panels + 1 governing 400 solve | Yes: 433–495 ms now (before) / 432–488 ms (after), same check | **Verified** by reproduction (the spread across runs is 420–495 ms) |
| 430 ms | `docs/proof/a3bc-seam/proof-pack.md:48`: repair cycle 1, 129-station NACA 2412, warm, including the 400-panel governing check, macOS arm64 | Yes: same check and code shape | **Verified**; same workload as 419 ms |
| 723 ms | Ruling 103: previous session's readiness reading | **Not reproduced** on a quiet machine. Not found in a committed log (searched `docs/proof`, `docs/notes`, `docs/audit`). | **Inferred**: a readiness run is a concurrent ring (`tools/ring-lock.sh`, up to two suites plus the host). My load runs read 989–1,108 ms warm at 2× oversubscription and 436–496 ms at 0.5×, so 723 ms sits between, consistent with a moderately loaded ring. The load level of that run is unknown, so this stays Inferred. |
| 1.6 s | the SEAM CFD review's whole-wing figure | **Source not found** in `docs/reviews/` or `docs/proof/a3bc-seam/` (searched for 1.6 s, 1.5–1.7 s, 16xx ms). | **Inferred**: it matches Debug warm (about 2.0 s here) or a loaded first run (about 1.5 s here, 32 copies), or the 129-station fixture before the matrix-reuse repair (SEAM repair cycle 1 measured 505 ms for the first ring and 449.8 ms after). I cannot tell which; no workload is recorded with it. |

So the four numbers are one workload read under different conditions: **quiet Release, 419–495 ms; loaded ring,
about 0.7–1.1 s; Debug or oversubscribed first run, 1.5–2.0 s**. None of them contradicts the other once the
conditions are named.

## What this means for the 1 s budget

- The 1 s budget holds for warm quiet Release. It does not hold on first run under 2× oversubscription (1.5 s) or in
  Debug (2.0 s). The budget is a Release, warm statement. The readiness check measures but does not enforce it.
- Cost of knowing a non-governing station's under-read: about 16 ms per extra 400 solve (2.0 s for all 129);
  see `step0-other-stations.md`.
