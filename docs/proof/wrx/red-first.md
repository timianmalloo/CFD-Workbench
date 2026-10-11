---
id: proof-wrx-red-first
title: "WRX red-first receipt"
type: proof-pack
status: active
owner: "@trk-wrx"
phase: implementation
tags: [windows, ring, expected-failures, proof]
links:
  - { to: proof-wrx-classification, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  Red and green receipts for WRX: the manifest-name control (red on the pre-WRX manifest), the replay of the PC's raw log through the
  checker, and what the Mac cannot show red.
---

# WRX red-first

Host: macOS arm64, Release build, base main 7955f3f2. No Windows host. Windows claims are **Inferred** unless a raw-log line is quoted.

## 1. The control: every manifest name is a registered check (MANIFEST-NAME-DRIFT)

Red, new check against the unchanged manifest (`dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests`, exit 70):

```
STAGE SelfLaunchTests elapsed_ms=137
APP-UNHANDLED APP-CRASH System.Exception
System.Exception: SelfLaunchTests failed:
  every name in the Windows expected-failure manifest is a check the tests register: tests/expected-failures.windows.json names no
  registered check: Rollback_TextSizeV2_BytesUnchanged, PrefStore_TextSize_OutOfSetOrGarbled_100_BytesUnchanged,
  Cli_AnalyseRunKey_EqualsServiceOnCustomOp, Cli_InspectRuns_RevisionIsSessionLabel
```

The first two are the real dead entries. The two Cli names are registered as `PASS <name>` lines in the Cli harness, so the scan was widened to accept that shape (a false positive, found by the red run).

Green, after the manifest edits and the widened scan (same command, exit 0):

```
STAGE SelfLaunchTests elapsed_ms=243
SelfLaunchTests: all 11 cases passed.
```

Cost: about 10 ms (file scan only). Ring: fast, every join. Raw logs: the scratch files `red-selflaunch.log` and `green-selflaunch.log`.

## 2. The manifest edits, replayed on the PC's raw log

`python3 tools/check-expected-failures.py --log <windows-ring-wlk-rerun.stdout.txt> --host windows` with the edited manifest (the replay is Inferred
for a rerun: the raw log was produced by the old tests):

- `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged`: was `UNEXPECTED ... not in the manifest`, now `EXPECTED (Ruling 154, fail-closed-store ...)`.
- `Telemetry_MarkerInjection_AbsentEverywhere`: replay says `listed, but the failure text lacks the fragment 'Marker probe did not record the file in Recent'`. That is right: the old log holds the old message. It becomes `EXPECTED` only on a rerun of the new test.
- Still `UNEXPECTED` in the replay, with the reason: `Export_WriteFailure_...` (fixed in the test, old log), the three `TIMEOUT` lines and their child exits (no change by the brief), the two PropertiesPane checks (no manifest class; see classification section 5), `TeFloor_B10_SettingsHash_GoldenUnchanged` (untouched), the `PARTITION` lines (a consequence of the timeouts).
- Totals (measured, same log): old manifest 32 expected and 22 unexpected; edited manifest 34 expected and 20 unexpected.

## 3. What the Mac cannot show red

- `Export_WriteFailure_...`: the old expectation is the POSIX outcome and passes on the Mac before and after. The red is the PC's raw line (quoted in the classification, row 3). The new expectation is behind `OperatingSystem.IsWindows()`; the Mac runs the unchanged branch (full ring green, below).
- `Telemetry_MarkerInjection_AbsentEverywhere`: the reorder (marker judged first) changes nothing where the store persists, so the Mac stays green. On Windows the red is `Marker probe did not open and record the file` (raw line); the green is `EXPECTED` with the new fragment, **Inferred** until the PC reruns it.
- The manifest rename: the Mac does not read the manifest in `run-tests.sh` (Windows only); the control in section 1 is its Mac-side proof.
