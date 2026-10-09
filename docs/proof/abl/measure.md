---
id: proof-abl-measure
title: "ABL Analysis part balance: measurements"
type: proof-pack
status: active
owner: "@trk-abl"
phase: implementation
tags: [abl, analysis, partition, c-2, timing]
links:
  - { to: proof-abl-red-first, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Per-group cost of the Analysis harness (three runs, median), the hint change, and three ring runs per part with load.
---

# ABL Analysis part balance: measurements

Track `trk-abl`, branch `fix/abl-analysis-balance`, base `796e9015`, 2026-10-08, Release, `DOTNET_TieredPGO=0`.

**Load caveat.** The machine was not quiet: `dispatch-gate.py --running 0` printed GO at load 9.4 to 11.4 for the measurement
(its limit is 24), and WAIT at 29.96 before ring 3. The ring itself raises the 1-minute load to 17-47 while it runs.

## 1. Group cost, whole harness in one process, three runs (ms)

Load at the three starts: 11.36, 10.77, 9.42.

| Group | Old hint | Run 1 | Run 2 | Run 3 | Median |
|---|---|---|---|---|---|
| Architecture | 10 | 7 | 6 | 6 | 6 (hint 7) |
| RunStore | 773 | 820 | 713 | 719 | 719 |
| Lattice | 580 | 559 | 559 | 560 | 559 |
| Strip | 302 | 273 | 278 | 274 | 274 |
| Service | 1230 | 1034 | 1044 | 1038 | 1038 |
| Freshness | 561 | 602 | 604 | 604 | 604 |
| Projection | 1180 | 29 | 28 | 29 | 29 |
| Labels | 20 | 38 | 37 | 38 | 38 |
| LoadsView | 10 | 8 | 8 | 8 | 8 |
| PanelCp | 40 | 37 | 36 | 36 | 37 |
| SectionEstimator | 60 | 76 | 77 | 77 | 77 |
| Cavitation | 10 | 10 | 10 | 10 | 10 |
| NeuralFoil | 100 | 102 | 100 | 98 | 100 |
| PolarSeam | 353 | 13 | 13 | 13 | 13 |
| SectionSeam | 1040 | 1528 | 1604 | 1513 | 1528 |
| ProvenanceSeam | 70 | 32 | 40 | 33 | 33 |
| PolarNumerics | 304 | 19 | 23 | 19 | 19 |
| OperatingSearch | 45 | 20 | 26 | 20 | 20 |
| TipPolar | 50 | 1 | 1 | 1 | 1 |
| DxSection | 800 | 998 | 1054 | 1024 | 1024 |
| SectionForce | 780 | 294 | 316 | 297 | 297 |
| NotResolved | 950 | 842 | 878 | 802 | 842 |
| Sum | 6,546 | | | | 6,876 |

Old hints were off by large amounts: Projection 1180 (measured 29; the old hint carried a cold start that now falls on
whichever group runs first), PolarSeam 353 (13), PolarNumerics 304 (19), SectionForce 780 (297), SectionSeam 1040 (1528).

**Order effect (measured).** A group's cost depends on which groups ran before it in the same process (shared JIT and
fixtures). SectionSeam costs 1528 ms cold and 1206 ms after Strip and Freshness. Hints taken from one partition layout
and fed back into the partition changed the layout and moved the cost. So the hint is the cold, whole-harness median
(section 1), a number anyone can reproduce, and it is not re-derived from a partition's own GROUP lines.

## 2. Hint sets tried

| Set | Hints | Part 1 / Part 2 in the ring (ms) | Result |
|---|---|---|---|
| Old (leader, two join rings) | 2026-10-06 values | 4900 / 4065, 4936 / 3947 | part 1 near limit, skew about 1 s |
| A | cold medians, section 1 | 4580 / 5036, 4447 / 4874, 4500 / 4901 (82 / 163 checks) | part 2 heavier by 400-450 ms; ring 1 red (C-2 5036 > 5000) |
| B | GROUP costs read from set A's ring | 5481 / 4687, 5267 / 4550 | red twice: the layout changed, SectionSeam went cold in part 1 |
| C (kept) | cold medians; Service 1205 and DxSection 1177 (ring-measured, the two load-sensitive groups) | see section 3 | green three times |

## 3. Proof: `tools/run-tests.sh`, set C

| Ring | Part 1 (ms) | Part 2 (ms) | Larger part, % of 5000 | Skew | Load at ring start -> end | Exit |
|---|---|---|---|---|---|---|
| 1 | 4716 | 4971 | 99.4 % | 255 ms | 10.91 -> 23.77 | 0 |
| 2 | 4550 | 4706 | 94.1 % | 156 ms | 23.77 -> 34.78 | 0 |
| 3 | 4619 | 4712 | 94.2 % | 93 ms | 29.96 -> 47.46 | 0 |

Parts hold 126 and 119 checks. Skew is |part 1 - part 2|, 1.9 % to 5.1 % of the limit (old: 17 % to 20 %).

**Target not met.** The brief's target was each part at or under 4500 ms at load under 8. No ring ran at load under 8: the
ring raises the load itself (all of Core, Desktop, Analysis and Cli start together), so the Analysis parts always run at
load 10 to 47 in a ring. A part alone, with nothing else running, takes 3.5 to 4.0 s (set A, load 5 to 9). At ring
concurrency the two parts together cost about 9.3 s of wall; balanced, that is 4.6 s each, which leaves 8 % headroom to 5000.
Going lower needs a third part (n=3, one number in `jobs=`), which the brief forbids without a ruling. Recommend asking for it.
