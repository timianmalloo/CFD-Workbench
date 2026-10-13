---
id: proof-win-gpu-f1-r231-desktop-ring-investigation
title: "R231 Windows Desktop ring timeout investigation"
type: proof-pack
status: draft
owner: "@pc-f1-r231-ring-investigator"
phase: preflight
tags: [windows, desktop, ring, timeout, r231]
links:
  - { to: proof-win-gpu-f1-cleanup-helper-repair, rel: relates-to }
  - { to: proof-win-gpu-f1-r229-cleanup-candidate, rel: relates-to }
review-by: "2026-11-12"
summary: >-
  Serial Windows reruns of the six Desktop children named in the R231 broad-ring
  failure completed below the child deadline; all observed test failures match
  the Windows expected-failures manifest.
---

# R231 Desktop ring investigation

## Scope and method

Goal: determine whether the six reported Desktop child timeouts and four
incomplete partitions reproduce as test failures or as bounded-load timeouts.
No `src/`, `tests/`, `tools/`, or helper code changed. The investigation ran in
worktree `CFD-Workbench-win-gpu-f1-r231-ring-investigation` at base
`37b96e4749a13d2fee3a7d73444d66a6a39a6403`.

The broad-ring summary in
[`helper-repair.md`](helper-repair.md#validation-ledger) records six Desktop
children over 120 seconds, four incomplete partitions, and end load 11.02.
The retained run output is temporary: [`run-tests.sh`](../../../../../tools/run-tests.sh)
clears `.tmp-tests/*.log`, `.seconds`, and `.ms` at each start, then stores
per-suite output there. No original broad-ring child logs were available in
this worktree.

The harness starts eight top-level jobs; the Desktop harness then starts 18
child modes concurrently. Each child has a 120-second wait limit, and each
partitioned child reports a `PARTITION k/n of N checks` line. These contracts
are visible in [`run-tests.sh`](../../../../../tools/run-tests.sh#L55) and
[`WorkbenchTests.cs`](../../../../../tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs#L497).

The Release solution built successfully with
`%USERPROFILE%\.dotnet\dotnet.exe` SDK `10.0.203`: exit 0, 20,604 ms wall,
three existing Avalonia AVLN3001 warnings. The child commands below ran serially
with `DOTNET_TieredPGO=0` and `TMPDIR`, `TMP`, and `TEMP` set to this worktree's
`.tmp-tests`, matching the ring's process environment. Each used:

```text
%USERPROFILE%\.dotnet\dotnet.exe run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- <mode> [--part=k/n]
```

## Measured outcomes

| Child | Exit | Wall | PASS | FAIL | Partition record |
|---|---:|---:|---:|---:|---|
| `--shell-window --part=1/2` | 1 | 36,125 ms | 56 | 3 | `1/2 of 118` |
| `--plan-canvas --part=2/2` | 0 | 56,148 ms | 46 | 0 | `2/2 of 92` |
| `--section-editor --part=2/2` | 1 | 42,936 ms | 74 | 6 | `2/2 of 160` |
| `--controller-shell` | 1 | 26,520 ms | 136 | 1 | not partitioned |
| `--views --part=2/2` | 0 | 24,643 ms | 53 | 0 | `2/2 of 107` |
| `--analysis` | 1 | 31,242 ms | 49 | 1 | not partitioned |

All six completed below 120 seconds. All four partitioned children emitted a
completion record. The 11 observed failures were:

- `Recent_StoredRows_StartAndFileMenu`
- `Copy_RecentNotCleared_StatusAndTryAgain`
- `Recovery_RailDraftResumed_PlanShowsDraftApplyCommits`
- `Controller_ResumeSectionRecovery_EntersSectionMode`
- `Controller_SaveMine_SurvivesSectionCancelAndDocumentUndo`
- `Controller_CatalogTelemetry_NamedSessionRing`
- `Controller_SaveOffUiThread_HonorsCancellation`
- `SaveDialog_EmptyOrDuplicate_ErrorFocusStays`
- `SaveDialog_Save_LiveRegionFocusToSectionMenu`
- `Controller_SaveDuringDrag_CommitsThenSaves`
- `Tampered_RunView_UnavailableNoLayersRowKept`

The Windows classifier [`check-expected-failures.py`](../../../../../tools/check-expected-failures.py)
reported all 11 as expected Ruling 173 fail-closed-store failures and reported
`UNEXPECTED 0` for each failed child. The two successful children also reported
`UNEXPECTED 0`. The manifest and its exact names/fragments are in
[`expected-failures.windows.json`](../../../../../tests/expected-failures.windows.json).

After the serial runs, a two-sample Windows performance-counter read showed
processor queue length 0 and total CPU 7.13% / 6.87%. This was after the tests,
not a measurement during them. This machine does not expose the ring's
`/proc/loadavg` measure natively, so these readings cannot be compared to the
reported 11.02.

## Disposition

**Verified:** each reported child mode completes serially in 24.6–56.2 seconds;
all partition records are present; every observed test failure matches the
Windows manifest; no unexpected failure appeared in the classifier.

**Inferred:** the six broad-ring timeouts and resulting missing partition
records are consistent with contention from the broad concurrent fan-out. The
serial runs disconfirm a deterministic 120-second timeout for these six child
commands, but do not prove contention caused the original timeouts.

**Flagged:** the original broad-ring child logs and in-run load samples were not
retained here, and the six children were not rerun concurrently. Therefore the
original red remains unresolved.

**Recommendation to Mac:** rerun the broad Desktop ring with bounded concurrency
and preserve its child logs and in-run load samples. Do not waive the red on
serial evidence alone. Keep the join blocked if any rerun child times out, omits
its partition record, or has an unexpected manifest classification.
