---
id: proof-phl-red-first
title: Track PHL red-first record (per-host limits, Windows manifest, WingRun)
type: doc
status: done
owner: "@trk-phl"
phase: implementation
tags: [proof, test-ring, windows, ruling-173]
links:
  - { to: review-pr-18, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Red and green runs for the four Ruling 173 items. The PC runs 1-3 of PR #18 are replayed through the changed tools.
---

# Track PHL red-first record

Evidence source: `origin/win/r166-ring-baseline`, `docs/proof/ring-windows/calibration-ruling-170/run-{1,2,3}/`.

## 1. Per-host ms limits (`tools/check-test-costs.py`)

| Run | File | Result |
|---|---|---|
| Red, old tool | `red-costs.txt` | `--self-test` exit 1: 7 FAIL, e.g. "pc-win run 1 passes with the baseline limits" (old tool ignores `limit.` lines and fails C-2 at 5,000 ms) and four "malformed ... is refused" cases (no refusal) |
| Red, C-5 cases | `red-costs-c5.txt` | `limit.check` is not a known name: BaselineError traceback |
| Green | `green-costs.txt` | `SELFTEST 67/67 cases` |

Cases added: limits pass run 1; the same run fails C-2, C-3, C-4 and C-5 without them; one limit line re-bases only its own
rule; `limit.check` leaves the named exemptions on their own limit; a sysctl (Mac) host ignores the baseline; no host keeps
the Mac C-4 limit 49,369; one ms over the host limit fails and names it; four malformed lines (`fast`, `core_part`, `0`,
a repeated name) are refused with the line.

## 2. The baseline and the replay

`sim-with-baseline.txt`: runs 1, 2 and 3 with `--load-source proc-gitbash --host pc-win` and the end loads 12.18, 10.08,
11.80: `0 failures, 0 COST-MISS`, exit 0 each. Measured limits (max + 2,000 ms over the three runs): analysis part 26,391,
desktop 306,344, net wall 313,389 (run 1: 336,392 - 25,003 = 311,389). All three match Ruling 173. Without the C-5 limits the
same run 1 fails 24 times on C-5 (`limit.check` 3,708 = 1,707.929 + 2,000; `limit.check_exempt` 3,890 = 1,889.286 + 2,000).

## 3. Windows manifest and child exits (`tools/check-expected-failures.py`)

| Run | File | Result |
|---|---|---|
| Red | `red-classifier.txt` | 2 of 16 cases FAIL: a child exit whose block holds only expected failures is expected; a block belongs to its own child |
| Green | `green-classifier.txt` | `16 cases, manifest 52 entries` |
| Replay | `replay-desktop.txt` | runs 1, 2, 3 identical: 26 EXPECTED (22 store names + 4 child exits), 15 UNEXPECTED |

UNEXPECTED after the replay: the 8 class-(a) names, `Telemetry_MarkerInjection_AbsentEverywhere`, and the 6 child exits
(`--shell-window` x2, `--views` x2, `--properties-cells` x2) whose blocks hold those names.

## 4. WingRun timing

| Run | File | Result |
|---|---|---|
| Red | `red-wingrun.txt` | allowlist entry removed, old code: `WALLCLOCK-ASSERT-UNGATED ... SectionSeamTests.cs:298`, exit 1 |
| Green | `green-wingrun.txt` | lint exit 0 ("2 ungated, all allowlisted") with the throw moved to a readiness row |

The functional assertions stay in `Section_WingRun_PanelValuesAtEveryStation` (PASS in the fast ring). The 1 s target is now
`Section_WingRun_WarmTime` in the Analysis `--readiness` tier: it prints `READINESS WingRun value_ms=` and, over 1,000 ms,
`READINESS-MISS`, and never throws.
