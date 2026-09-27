---
id: proof-p1-red-runs
title: P1 preferences red-first runs
type: proof-pack
status: in-review
owner: "@track-p1"
phase: implementation
tags: [app-shell, preferences, layout, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Red run of the P1 checks at 92079ab, while the codec and store still threw.
  With the version-first return removed, the three v2 rollback fixtures were overwritten.
  The same checks passed at 4efe980. Reader depth stays 8; the writer uses 9.
review-suggested: []
---

# P1 preferences: red-first runs

Track P1 (design `docs/design/app-shell.md` §3.4–§3.5, §4, §9, §14). Branch `p1-preferences`, macOS arm64, 2026-09-27. Session start `2026-09-27T17:17:21Z`. The green suite finished at `2026-09-27T17:59:17Z`, 42 minutes, inside the 90 minute box.

## Red run, checks committed, codec still throwing

`92079ab` commits the 23 named checks, the depth check, and the fixtures. `LayoutCodec`, `RecentList`, and `PreferenceStore` throw `NotImplementedException`.

`tools/run-tests.sh` at that commit. Core exit 1. Wall 31 s. The P1 lines:

```text
FAIL LayoutParse_Corrupt_ReturnsPresetsWithCode NotImplementedException: The method or operation is not implemented.
FAIL LayoutCodec_RandomBytes_NeverThrows NotImplementedException: The method or operation is not implemented.
FAIL LayoutParse_BadWorkspace_OthersRestored NotImplementedException: The method or operation is not implemented.
FAIL LayoutParse_UnknownPane_DroppedOthersKept NotImplementedException: The method or operation is not implemented.
FAIL LayoutParse_MissingPane_PlacedByPreset NotImplementedException: The method or operation is not implemented.
FAIL LayoutParse_OutOfRange_Clamped NotImplementedException: The method or operation is not implemented.
FAIL RecentParse_RelativePath_Dropped NotImplementedException: The method or operation is not implemented.
FAIL LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9 NotImplementedException: The method or operation is not implemented.
FAIL LayoutLoad_Absent_ReturnsPresets NotImplementedException: The method or operation is not implemented.
FAIL Rollback_V2UnknownTopLevel_BytesUnchanged NotImplementedException: The method or operation is not implemented.
FAIL Rollback_V2UnknownWorkspaceMember_BytesUnchanged NotImplementedException: The method or operation is not implemented.
FAIL Rollback_V2Oversized_BytesUnchanged NotImplementedException: The method or operation is not implemented.
FAIL LayoutLoad_ReadError_NeverWrites NotImplementedException: The method or operation is not implemented.
FAIL PrefStore_Unsupported_SessionOnly NotImplementedException: The method or operation is not implemented.
FAIL PrefStore_SymlinkedDirectory_SessionOnly NotImplementedException: The method or operation is not implemented.
FAIL PrefsSave_LayoutAndRecentConcurrent_BothKept NotImplementedException: The method or operation is not implemented.
FAIL LayoutSave_ClaimBusyHashSame_RetriesOnce NotImplementedException: The method or operation is not implemented.
FAIL LayoutSave_Conflict_MergesChangedWorkspaceOnly NotImplementedException: The method or operation is not implemented.
FAIL LayoutSave_TwoQueuedSaves_UnionOfChangedWorkspaces NotImplementedException: The method or operation is not implemented.
FAIL LayoutSave_StaleClaim_SessionOnlyNamesClaim NotImplementedException: The method or operation is not implemented.
FAIL Recent_Conflict_ReappliesAdd NotImplementedException: The method or operation is not implemented.
FAIL Recent_ClearFails_ReportedNotCleared NotImplementedException: The method or operation is not implemented.
FAIL Recent_Clear_NoFileContainsMarkerPath NotImplementedException: The method or operation is not implemented.
FAIL StoreContract_CancelBeforePublish_DocCancelled NotImplementedException: The method or operation is not implemented.
RESULT failures=24
```

Suite summary: Core exit 1 (250 PASS outside these failures), Cli exit 0, Desktop exit 0, wall 31 s, `tools/run-tests.sh` exit 1.

## Rollback fixtures, red then green

The three fixtures are `tests/CfdWorkbench.Core.Tests/Fixtures/layout/v2-unknown-top.json`, `v2-unknown-workspace.json`, and `v2-oversized.json` (70,081 bytes). Each is `cfdw-layout` version 2.

**Red.** The version-first return in `LayoutCodec.Parse` was removed, and the committed checks were run with `CFD_TEST_ONLY=Rollback`. Each load left `NeverWrite` false:

```text
FAIL Rollback_V2UnknownTopLevel_BytesUnchanged InvalidOperationException: Expected True; actual False
FAIL Rollback_V2UnknownWorkspaceMember_BytesUnchanged InvalidOperationException: Expected True; actual False
FAIL Rollback_V2Oversized_BytesUnchanged InvalidOperationException: Expected True; actual False
RESULT failures=3
```

Exit 1. With those two assertions set aside so the three save points ran, each fixture was overwritten:

```text
FAIL Rollback_V2UnknownTopLevel_BytesUnchanged InvalidOperationException: v2-unknown-top.json bytes changed
FAIL Rollback_V2UnknownWorkspaceMember_BytesUnchanged InvalidOperationException: v2-unknown-workspace.json bytes changed
FAIL Rollback_V2Oversized_BytesUnchanged InvalidOperationException: v2-oversized.json bytes changed
RESULT failures=3
```

Exit 1. The version return was put back before the green run. That edit is not in `4efe980`.

**Green.** `4efe980` keeps the return. A version greater than 1 is presets for the session and is not written. `tools/run-tests.sh` exited 0: Core 274 PASS, `RESULT failures=0`, wall 30 s. The three rollback names printed `PASS`. `python3 tools/check-named-tests.py P1` exited 0, 23/23.

## Writer depth

§3.4 sets `MaxDepth = 8`. Measured on the §3.4 example, before this codec:

- Reflection serialization of the `IReadOnlyList` records throws `JsonException` at `MaxDepth` 8, path `$.Workspaces.Regions.Groups.Panes`. `MaxDepth` 9 writes the bytes.
- `JsonDocument` reads those bytes at `MaxDepth` 8. `example-v1.json` parses at 8 and fails at 7. `depth-9.json` fails at 8 and parses at 9.
- A source-generated context writes the same graph at `MaxDepth` 8.

`LayoutCodec.ReaderMaxDepth` is 8, on both `JsonDocument` and the deserializer. `LayoutCodec.WriterMaxDepth` is 9, so the deepest valid layout still serializes if the reflection counter is the one in use. `LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9` serializes the example, reads it back at the reader bound, checks the byte round trip, and requires `depth-9.json` to return `LAYOUT-SCHEMA`.

## Gate

`tools/run-tests.sh` exit 0, wall 30 s, Core 274 PASS. `python3 tools/check-named-tests.py P1` exit 0, 23/23. `python3 tools/check-docs.py` exit 0 after `docs-graph.py derive` indexed this file.
