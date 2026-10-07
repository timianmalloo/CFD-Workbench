---
id: proof-rgt-red-first
title: "RGT red-first receipt"
type: proof-pack
status: active
owner: "@trk-rgt"
phase: implementation
tags: [rgt, ruling-139, test-ring, cost-gate, red-first]
links:
  - { to: rulings, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  Ruling 139: the HEAD checker fails C-5 at load 30 and rejects the host flags; the new checker prints COST-MISS and exits 0,
  while C-5 at a quiet load stays red. The self-test grew from 26 to 41 cases and exits 0.
---

# RGT red-first receipt

Session `trk-rgt`, branch `fix/ring-gate-c5`, 2026-10-07. Ruling 139 (DR-RING-4). The old checker is `git show HEAD:tools/check-test-costs.py`
(before this track). Same input directory for both: every job 2,000 ms, Desktop 49,400 ms (over C-4), `Slow_Check` COST 612 ms (over C-5).

| Input | Old checker (red) | New checker (green) |
|---|---|---|
| C-5 612 ms at end load 30 | exit 1, `1 failures, 1 COST-MISS` (C-5 fails) | exit 0, `0 failures, 2 COST-MISS` (C-4 and C-5 are misses) |
| C-5 612 ms at end load 5 (must stay red) | exit 1, `2 failures` | exit 1, `2 failures` (unchanged) |
| quiet load 5, `--load-source proc-gitbash --host winx`, no baseline | exit 2, usage (unknown flag) | exit 0, `0 failures, 2 COST-MISS` (each carries `host winx`) |

Self-test (`python3 tools/check-test-costs.py --self-test`): 41/41, exit 0. New cases: C-5 at load 30 and load not recorded are COST-MISS;
C-5 at 24.0 is gated; C-6 stays ungated at load 40; an uncalibrated host turns TEST-BUDGET and C-4/C-5 over limit into `COST-MISS ... host winx`
(exit 0); a host with two baseline runs stays uncalibrated; a host with three runs and `gate=30` fails the same readings and applies its own gate;
a `sysctl` host is never uncalibrated. The old checker has none of these inputs: it lacks the host flags and gates only C-2..C-4.

Baseline file form read by the checker, `docs/proof/ring-<host>/baseline.csv`: a first line `gate=<n>`, then rows `<run>,<end load>,<wall ms>`; three rows or more.
