---
id: proof-prf-red-first
title: "PRF red-first proof: the comb view preferences"
type: proof-pack
status: active
owner: "@timianmalloo"
tags: [proof, trk-prf, view-preferences, rail-comb]
links:
  - { to: design-view-preferences, rel: relates-to }
review-by: 2027-04-01
summary: Each behaviour of the comb view preferences ran red on the old code, then green. Core checks failed to compile before the API existed; Desktop checks failed against a build with the restore and save lines removed.
---

# PRF red-first proof

Ring: fast. Design: `docs/design/view-preferences.md` section 8. Rulings 205, 206, 207.

## Core / Persistence (`PreferenceStoreTests.cs`, 12 new checks; plus the two split and one renamed pinned checks)

- **Red.** Before the store change, `dotnet build -c Release tests/CfdWorkbench.Core.Tests` failed with CS1061: `PreferenceStore` has no
  `LoadCombViewAsync` or `SaveCombViewAsync`, and `DisplayParse` has no `CombScale`, `CombDensity` or `CombVisible`. A compile failure is
  the red for an API that did not exist; it is not a behavioural proof by itself.
- **Green.** After the change, `CFD_TEST_ONLY=PrefStore_Comb`, `PrefStore_DisplayCodec_`, `PrefStore_TextSize` and `Rollback_TextSizeV2` each
  exit 0 with every check PASS (10, 2, 9 and 1 checks). The renamed pinned checks pass: `PrefStore_TextSize_ValueError_ThatMemberDefaults_BytesUnchanged`,
  `PrefStore_TextSize_StructureError_WholeFileDefaults_BytesUnchanged`, `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged`.
- Not shown red on old code: the value-fault cases that keep sound members (for example a bad `units` keeping Text size 150). The old
  `Parse` returned the whole file unreadable for those inputs, so they could not have passed; this was reasoned, not run.

## Desktop (`PlanCombViewPrefsTests.cs`, 8 checks and one screenshot check)

- **Red.** With `Controller.ApplyCombView(...)` and the `SaveCombViewAsync` call in `ShellHost` replaced by comments (a mutant, then reverted),
  `CFD_TEST_ONLY=Comb_Settings ... --plan-canvas` exited 1:

  ```
  FAIL Comb_Settings_Persist_ChoiceSurvivesShellRestart_PlateAndTeethRendered  The comb was off after a restart that saved it on
  FAIL Comb_Settings_Auto_RestoredAsAuto_Visible  Auto, 32 and on were not restored
  FAIL Comb_Settings_SaveKeepsTextSizeAndUnits  The comb did not come back
  FAIL Comb_Settings_ChoiceBeforeLoadWins  The early choice was not kept
  FAIL Comb_Settings_FoilAndProjectBytesUnchanged_Telemetry_NoPath  display.save rows lack trigger=comb
  FAIL Comb_Settings_ValueFault_KeepsOtherMembers_NeverRewrites  A bad scale did not default alone
  PASS Comb_Settings_LimitPress_NoSave   (vacuous under the mutant: nothing saves)
  PASS Comb_Settings_TextSizeAndUnitsSaves_CarryTheirTrigger   (the mutant left those two lines in place)
  ```
- **Green.** The same filter on the real code: 9 PASS, exit 0. The full `--plan-canvas` ring: 91 PASS, 0 FAIL.
- The restart check compares what is rendered, not the controller alone: the plate's scale line, density line and Auto state, and the pixels
  just below every fifth outline sample of both rails, before and after a restart; it also requires the default comb to draw different pixels, so the
  comparison is not vacuous.
- The two checks that stay green under the mutant name what they protect: `LimitPress_NoSave` guards the triple compare (a press at a limit
  fires `CombChanged` but must not write), and `...CarryTheirTrigger` guards the new `text-size` and `units` triggers.

## Whole-ring results

- `tools/run-tests.sh`: exit 0, 50 s wall, all harnesses passed (Core 365 + 222 + 222, Desktop 798, Analysis 102 + 146, Cli 19).
- `dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests -- --readiness`: exit 0, 95 PASS, no FAIL.
- `xaml-token-lint.py`: clean. `check-event-subscribers.py`: 37 events, 0 findings.

## Screenshot

`docs/proof/prf/plan-comb-restored-after-restart.png`: a second shell over the same preference folder. The plan shows the comb, and the plate reads
"30 px = 0.5 per m" and "64 per rail" (the first session stepped Larger teeth twice and Denser once).
