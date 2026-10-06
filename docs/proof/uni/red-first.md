# trk-uni red-first (Ruling 115)

Command: `CFD_TEST_ONLY=Units_ tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --readiness`
(controller check: `--controller-shell`). Logs: `red-*.txt` (before), `green-readiness.txt` (after).

| Check | Before | After |
|---|---|---|
| Units_MenuItems_ExistAreCheckedByState_AndToggle (a) | FAIL View has no Units submenu | PASS |
| Units_StatusItem_TogglesAndShowsState (b) | FAIL missing UnitsButton | PASS |
| Units_MenuAndItemRoutes_ConvertTheAnalysisRows (d, via the menu command and the item) | FAIL Lift L: N -> N | PASS |
| Units_KeyboardAccess_PaletteRowsAndFocusedItemTakeSpaceAndEnter (e) | FAIL palette rows: (none) | PASS |
| Units_Switch_ChangesForceAndSpeed_KeysAndStoredValuesUnchanged (d, controller invariant) | PASS (plumbing from CPY already converts; guard, not red) | PASS |
| (c) persists across restart | not built: needs a Persistence key (see Return) | |
