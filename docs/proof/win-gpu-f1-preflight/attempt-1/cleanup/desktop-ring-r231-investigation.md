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
  Serial reruns and a full bounded-concurrency Windows ring show the six Desktop
  timeout modes complete below the child deadline; all 33 Desktop failures match
  the Windows expected-failures manifest, with no unexpected failures.
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

## Full bounded-concurrency ring

To resolve the remaining question, one full `tools/run-tests.sh` ring ran from
Git Bash with `%USERPROFILE%\.dotnet\dotnet.exe` SDK `10.0.203` on `PATH`,
`DOTNET_PROCESSOR_COUNT=8` (five Desktop child slots),
`CFD_TEST_CHILD_TIMEOUT_SECONDS=120`, and the default 60-second budget. The
budget environment variable was unset. The captured `.tmp-tests` output is in
[`r231-full-ring-capture`](r231-full-ring-capture/); the immediately preceding
serial investigation logs are in [`r231-pre-ring-capture`](r231-pre-ring-capture/).
The full capture manifest records SHA-256 and byte length for every preserved
file. The copied output was checked for user-profile paths before commit.

| Suite | Wall | Result | Functional classification |
|---|---:|---|---|
| Build | 15 s | exit 0 | Release build passed; three existing AVLN3001 warnings |
| Core 1/3 | 71,132 ms | exit 0 | 346 PASS; 9 EXPECTED-FAIL |
| Core 2/3 | 71,661 ms | exit 0 | 210 PASS; 7 EXPECTED-FAIL |
| Core 3/3 | 76,030 ms | exit 0 | 205 PASS; 11 EXPECTED-FAIL |
| Desktop | 180,337 ms | exit 0 | 776 PASS; 33 EXPECTED-FAIL; 0 unexpected |
| Analysis 1/3 | 10,688 ms | exit 0 | 44 PASS; no failures |
| Analysis 2/3 | 10,838 ms | exit 0 | 69 PASS; no failures |
| Analysis 3/3 | 11,159 ms | exit 0 | 146 PASS; no failures |
| CLI | 8,577 ms | exit 0 | 18 PASS; 2 EXPECTED-FAIL |
| Python unittest | — | exit 0 | 94 PASS |

The Windows expected-failure classifier independently processed the retained
Desktop log: `EXPECTED-FAIL 33 (manifest)`, `UNEXPECTED 0`, exit 0. All 18
Desktop child modes completed. Every partitioned mode emitted its completion
record, including the four partitions that were incomplete in the original
ring. In particular, the six previously timed-out modes completed within the
full run; the Desktop suite took 180,337 ms total, but no child hit its
120-second limit. Core, CLI, and the two known Desktop manifest categories
remain expected failures, not new regressions.

The functional ring verdict is exit 0: all suites passed or contained only
manifest-expected failures, with no timeout or unexpected classification. The
budget/cost verdict is separate: total wall was 207,380 ms (192,959 ms net of
build) against the 60,000 ms budget, ending at load 11.70. The harness emitted
`COST-MISS TEST-BUDGET 207 s` and 16 advisory `COST-MISS` entries, but reported
`test costs: 0 failures` and final exit 0. The ring emitted a host-category
label that matches this machine name, so the label is redacted here. The cost
report is advisory and does not change the functional classification.

## Disposition

**Verified:** each of the six reported children completes serially in 24.6–56.2
seconds and also completed in the bounded full ring. All four previously
incomplete partitions now emit completion records. The full Desktop classifier
reported 33 manifest-expected failures and `UNEXPECTED 0`; every suite exited 0.

**Inferred:** the original six timeouts and four missing partition records were
caused by the original broad-ring load or contention. The bounded full ring
disconfirms deterministic child timeout and partition incompletion at five
Desktop slots, but it does not reproduce the original concurrency level.

**Verified, separate cost verdict:** the ring's functional exit is 0. The
60-second budget was exceeded (207,380 ms wall), with 16 advisory cost misses;
the harness reports zero cost failures and leaves exit 0. This evidence supports
disposing of the broad-ring functional RED as a transient load timeout while
retaining the budget/cost advisory for its owner. It does not justify waiving
any unexpected test failure, timeout, incomplete partition, or cost gate that
has a hard-fail policy.

**Recommendation to Mac:** dispose the reported Desktop timeout/partition RED
as non-deterministic under the bounded five-slot rerun; classify the functional
ring as passing with manifest-expected failures only. Track the 60-second
budget overrun and advisory cost misses separately. Do not claim the original
load caused the timeout as a verified fact, and keep any new timeout,
incomplete partition, or unexpected classifier result blocking.


