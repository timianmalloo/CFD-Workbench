---
id: proof-abl-red-first
title: "ABL red-first receipt"
type: proof-pack
status: active
owner: "@trk-abl"
phase: implementation
tags: [abl, analysis, partition, red-first]
links:
  - { to: proof-abl-measure, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  The ring failed C-2 on the stale hints and on two intermediate hint sets; the kept hint set passed three rings.
---

# ABL red-first receipt

This track changes data (hints), not behaviour. No assertion or test body changed. The check is C-2 per part in `tools/run-tests.sh`.

| Hints | Ring result | Line |
|---|---|---|
| old (base `796e9015`), leader's two join rings | red under load in three tracks | part 1 4900 ms and 4936 ms against 5000; WRB, RG2, NCR failed C-2 on part 1 |
| set A, cold medians | red once | `FAILED: C-2 Analysis.part2of2 took 5036 ms, over 5000 ms` (load 13.36) |
| set B, ring-read costs | red twice | `FAILED: C-2 Analysis.part1of2 took 5481 ms, over 5000 ms`; then 5267 ms |
| set C (kept) | green three times | 4716 / 4971, 4550 / 4706, 4619 / 4712 ms, `test costs: 0 failures` |

Detail and loads: `docs/proof/abl/measure.md`.
