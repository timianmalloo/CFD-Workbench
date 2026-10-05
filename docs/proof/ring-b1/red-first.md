---
id: proof-ring-b1-red-first
title: "Ring B1 red-first receipt"
type: proof-pack
status: active
owner: "@trk-b1"
phase: implementation
tags: [ring, test-cost, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: plan-test-cost, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  Each self-test case of tools/check-test-costs.py was red against a stub checker (7429f70) and green once the
  checker landed; OD-2 measured and not met.
---

# Track B1 red-first record

| Test (`python3 tools/check-test-costs.py --self-test`) | Catches | Red commit | Green commit |
|---|---|---|---|
| baseline is green | a false positive on a healthy run | 7429f70 (stub checker, 2/10 cases) | the `feat: check-test-costs.py` commit |
| C-2 Analysis.ms 5900 | whole-second clocks reading 5 s | 7429f70 | same |
| C-3 wall.ms 50400 | run wall over 50 s | 7429f70 | same |
| C-4 Desktop.ms 43100 | Desktop over 43 s; message names DR-ANA-10 | 7429f70 | same |
| C-5 COST Units_Lbf_KeyUnchanged 512.3 | a check over 500 ms | 7429f70 | same |
| C-5 COST F6_ObservedOrder 1612.0 | an exempt check over 1,500 ms | 7429f70 | same |
| C-5 F6_ObservedOrder 1499.0 | the exemption itself (stays green; green on the stub too) | n/a | same |
| C-6 Analysis.ms deleted | a missing reading passing | 7429f70 | same |
| C-6 wall.ms deleted | a missing wall reading passing | 7429f70 | same |
| C-6 Analysis PASS without COST | an unrecorded check cost passing | 7429f70 | same |

The red commit holds the full self-test table and a checker that returns no errors. C-1 (the clocks in `run-tests.sh`) is
proved by C-6: a run without `<name>.ms` or `wall.ms` is red, and the three runs in `run-after-*.log` print them.
Timing thresholds touched: none (the constants are new in `check-test-costs.py` with the §13.4 values).
