---
id: proof-cbl-red-first
title: "CBL red first: the cost-table partition check"
type: proof-pack
status: active
owner: "@trk-cbl"
phase: implementation
tags: [cbl, core, partition, red-first]
links:
  - { to: proof-cbl-measure, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  The partition check red on the old round-robin rule (19106/18149/12491 ms), then green on longest-first.
---
# CBL red first

Check: `CorePartition_CostTable_PlacesEveryCheckOnceWithinFifteenPercent` (tests/CfdWorkbench.Core.Tests/CorePartition.cs). It runs the
assignment over `core-costs.tsv` for n = 3 and asserts every name lands on exactly one part and the part totals differ by less than 15 % of the largest.

- Red: `CorePartition.Assign` stubbed to the old rule (`i % n` over the table in registration order). Command:
  `CFD_TEST_ONLY=CorePartition_ dotnet run -c Release --no-build --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`.
  Result (`red.log`): `FAIL ... Parts 19106/18149/12491 ms differ by 15 % or more of the largest`, `RESULT failures=1`.
- Green: longest-processing-time-first assignment. Result (`green.log`): both CorePartition checks `PASS`, `RESULT failures=0`.
- An earlier red attempt with a table that dropped rows under 1 ms passed on the old rule (the dropped rows shifted the indices), so the table keeps every check.
