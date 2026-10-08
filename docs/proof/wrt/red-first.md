---
id: proof-wrt-red-first
title: "WRT red-first receipt: Windows known-expected-failure manifest"
type: proof-pack
status: active
owner: "@track-wrt"
phase: implementation
tags: [windows, proof, ruling-152, ruling-154]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for the Windows ring's known-expected-failure manifest (Rulings 152 (2), 154 (3)): the checker's self-test cases and the Desktop harness crash, red then green.
---
# WRT red-first: Windows known-expected-failure manifest (Rulings 152 (2), 154 (3))

Ring and cost: `tools/check-expected-failures.py --self-test` runs in the fast ring (`tools/check-docs.py`), 0.22 s wall measured on the Mac. On a Windows host `tools/run-tests.sh` runs the checker once per harness log (one pass over a log, under 0.1 s). On macOS nothing runs it in the ring.

## 1. The checker (`tools/check-expected-failures.py`)

Red: before the file existed there was no classification. The first self-test run had one wrong table row in the test itself (case 2 expected 0 unexpected), and printed `SELF-TEST FAIL: one unexpected ... (want (1, 0, 0))`, exit 1. The table was corrected (one repair cycle); the checker was not changed.

Green, `python3 tools/check-expected-failures.py --self-test`, exit 0 (full output in `self-test.txt`):

| Case (synthetic log lines) | Exit | Result |
|---|---|---|
| all FAILs listed, name and fragment match | 0 | `EXPECTED-FAIL 2 (manifest)`, `UNEXPECTED 0` |
| one FAIL not in the manifest | 1 | `UNEXPECTED Other_D: not in the manifest` |
| name listed, wrong fragment | 1 | `UNEXPECTED Store_B: listed, but the failure text lacks the fragment` |
| a listed test passes | 1 | `UNEXPECTED-PASS Store_B passed but Ruling 154 lists it` |
| fragment on a continuation line, entry says `aborts_harness` | 0 | expected, "later checks unassessed" |
| abort under an entry that does not say it aborts | 1 | unexpected |
| standalone `APP-UNHANDLED` crash | 1 | `UNEXPECTED <abort>` |
| harness exit 1 with no FAIL line | 1 | `UNEXPECTED harness exit 1 with no FAIL line the manifest explains` |
| host is not Windows, listed failure | 1 | `manifest has no effect on this host` |
| host is not Windows, listed pass | 0 | nothing |

Against the PC's real lines (`git show origin/win/wfx2-b2-ring:docs/proof/wfx2/pc-ring-20261008/evidence-extracts.txt`, line-number prefixes stripped, `--host windows --status 1`): Core parts 1-3 gave 9 + 11 + 8 expected and 2 + 3 + 3 unexpected, CLI gave 1 expected (the crash trace carries the code), and the pre-fix Desktop log gave `UNEXPECTED <abort>`. Expected in total: 29 (28 class (b) plus the probe). Unexpected in total: 8, exactly the Core rows of classes (c), (d), (f) of `docs/reviews/pr-12.md` (1 + 5 + 2). The class (d) NeuralFoil row and class (e) sit in the Analysis logs, which this check did not replay; they are not in the manifest, so they stay unexpected. The manifest lists 30 entries: 1 probe, 28 class (b), 1 Desktop check.

## 2. The Desktop harness crash (`WorkbenchTests.cs`, `Save_ExistingFile_DefiniteCreateOnlyConflict`)

Plant (a temporary edit, removed afterwards; `desktop-change-with-plant.diff` is the diff with it): `new WorkbenchController(_ => new PlantedUnsupportedStore())`, a store whose `SaveAsync` answers `DOC-UNSUPPORTED-PERSISTENCE`, through the existing `storeFactory` seam.

Red, old code with the plant, `tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj` exit 70 (`desktop-red.txt`):
`APP-UNHANDLED APP-CRASH System.Exception` / `Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE`, 8 log lines in all, no `PASS` line. Same text as the Windows ring's `Desktop.log`.

Green, new code with the same plant, exit 1 (`desktop-planted-green.txt`): one line
`FAIL Save_ExistingFile_DefiniteCreateOnlyConflict Exception: Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE`,
and the harness went on: 697 `PASS` lines followed, including the theme matrix, the spawned suites and `--shell-model`.
`check-expected-failures.py --log desktop-planted-green.txt --status 1 --host windows` gives `EXPECTED-FAIL 1 (manifest)`, exit 0. With `--host other` the same log gives `UNEXPECTED ... manifest has no effect on this host`, exit 1.

macOS without the plant: the assertion is unchanged and the check prints `PASS Save_ExistingFile_DefiniteCreateOnlyConflict`.

## 3. Wiring in `tools/run-tests.sh`

With a `uname` shim that answers `MINGW64_NT-10.0`, a ring on the Mac (where every listed test passes) failed each harness with `UNEXPECTED-PASS ...` lines and `FAILED: ... (a listed test passed: stale manifest entry)`: 8 + 10 + 9 + 1 listed tests passing across Core parts and Desktop. That proves the Windows branch runs and the drift rule fails the ring. The "all failures expected, status forced to 0" branch was proved on the checker and with the `sed` extraction of the count (`1`), not through a shell run on a Windows host. The PC's next ring is the first run of that branch.

Not covered by this track (see the Return): after the fixed check, the next throw in the Desktop default run on Windows is `Durable recovery save did not settle the unchanged visible draft` (`WorkbenchTests.cs`, the `savingDraft.SaveAsync` assertion), and the CLI harness aborts at `Cli_AnalyseRunKey_EqualsServiceOnCustomOp`.
