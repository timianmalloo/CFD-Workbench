---
id: proof-uni-red-first
title: "UNI red-first receipt"
type: proof-pack
status: active
owner: "@track-uni"
phase: implementation
tags: [uni, units, ruling-115]
links:
  - { to: design-area3-analysis, rel: depends-on }
review-by: "2026-11-04"
summary: >-
  Units switch (Ruling 115) red-first of the UNI track.
---

# trk-uni red-first (Ruling 115)

Command: `CFD_TEST_ONLY=Units_ tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --readiness`
(every check is registered at readiness). Logs: `red-*.txt` (before), `green-readiness.txt` (after).

| Check | Before | After |
|---|---|---|
| Units_MenuItems_ExistAreCheckedByState_AndToggle (a) | FAIL View has no Units submenu | PASS |
| Units_StatusItem_TogglesAndShowsState (b) | FAIL missing UnitsButton | PASS |
| Units_MenuAndItemRoutes_ConvertTheAnalysisRows (d, via the menu command and the item) | FAIL Lift L: N -> N | PASS |
| Units_KeyboardAccess_PaletteRowsAndFocusedItemTakeSpaceAndEnter (e) | FAIL palette rows: (none) | PASS |
| Units_Switch_ChangesForceAndSpeed_KeysAndStoredValuesUnchanged (d, controller invariant) | PASS (plumbing from CPY already converts; guard, not red) | PASS |
| (c) persists across restart | built in repair cycle 1, see below | |

## Repair cycle 1 (Ruling 121): persistence

Units now persist as an optional `units` key in `cfdw-display` v1 (written only when imperial; absent reads Metric).
- (c) `Units_Persist_ChoiceSurvivesShellRestart_TextSizeKept`: with the save line disabled it FAILED ("after a restart: Metric", `red-persist.txt`); PASS with the wiring. It also proves a units save keeps the Text size (150 %) and that Metric is kept after Imperial.
- `Units_OldPreferenceFileWithoutKey_LoadsMetric` PASS: a v1 file without the key loads (Metric, Text size 125 %). A guard: the old reader already accepted that file, so it is not red.
- `Units_UnknownValue_FallsBackToMetric_FileNeverRewritten` PASS: an unknown value makes the file unreadable (the existing rule). The status item reads Metric, the Text size falls back to 100 %, the file is never rewritten, a session choice of Imperial still works, and no message appears (no new wording; a units save that cannot be kept is silent). Also a guard.
