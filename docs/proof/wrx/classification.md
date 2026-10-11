---
id: proof-wrx-classification
title: "WRX classification of the Windows ring after WLK"
type: proof-pack
status: active
owner: "@trk-wrx"
phase: implementation
tags: [windows, ring, expected-failures, proof]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  Every failing name and every unseen manifest entry in the PC's WLK rerun (main eea2c7f1 merged at 67056b75), with the first failure
  line, the cause, the introducing commit and the action. Windows claims are labelled Verified (seen in the raw log) or Inferred.
---

# WRX: what the Windows ring still reports after WLK

Source (PC branch `win/gpu-g2-b1-r199`, `docs/proof/win-gpu-b2/`): `windows-ring-wlk-rerun.stdout.txt` (raw), `windows-ring-wlk-rerun-summary.txt`,
`windows-ring-wlk-timeout-{shell,plan,analysis}-isolated.txt`, and the pre-WLK `windows-ring-summary.txt` (77 failures). Host paths in the raw
log are not copied here. The Mac cannot run Windows; "Verified" means a line in that raw output.

## 0. Three corrections to the PC summary (Verified from the raw log)

1. **Core is 27 failures, not 11.** The ring prints names only for the job that exits non-zero with unexpected lines. Core 1/3 printed
   `RESULT failures=9` and `EXPECTED-FAIL 9`, Core 2/3 `failures=7` and `EXPECTED-FAIL 7`; their names are not in the log. Core 3/3 printed 11 names.
   The summary's 11 are the Core 3/3 names only.
2. **Cli is 2 expected failures, not 0.** The Cli block printed `17 PASS` and `EXPECTED-FAIL 2 (manifest)`. The two are
   `Cli_AnalyseRunKey_EqualsServiceOnCustomOp` and `Cli_InspectRuns_RevisionIsSessionLabel`, the only Cli names in the manifest.
3. **`TeFloor_B10_SettingsHash_GoldenUnchanged` is not in the manifest.** The log prints `UNEXPECTED TeFloor_B10_SettingsHash_GoldenUnchanged: not in
   the manifest` for Analysis 2/2. It failed in the pre-WLK run too (first failure line of that run's Analysis block), so it is not new today.

## 1. Failing names not in the manifest (5 named, plus TeFloor)

| # | Harness | Name | First failure line (quoted from the raw log) | Cause | Introduced | Action |
|---|---|---|---|---|---|---|
| 1 | Core 3/3 | `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged` | `FAIL ... InvalidOperationException: missing LAYOUT-VERSION [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` | Verified: PRF (`1344bdd2`, 2026-10-10) renamed `Rollback_TextSizeV2_BytesUnchanged`; the manifest kept the old name, so the renamed check is "not in the manifest". Same fail-closed store as its siblings (the line carries `DOC-UNSUPPORTED-PERSISTENCE`). Also named in the pre-WLK run. | `1344bdd2` | **Fixed**: manifest entry renamed, ruling 154 and class kept. |
| 2 | Desktop shell-window 2/2 | `Telemetry_MarkerInjection_AbsentEverywhere` | `FAIL ... InvalidOperationException: Marker probe did not open and record the file` | Test fails at its precondition, before any telemetry is read. Inferred: the Recent store cannot persist on Windows (fail-closed, the same cause as `Recent_OpenedOutcome_AppendsPath` in this run), so `recent.Entries.Count == 0`. The message fit either half (open or record). **No path reached telemetry: no emitted text was compared.** Not a privacy defect on this evidence. | `75cba053` (2026-09-30) | **Fixed + listed**: the test now judges the marker first, then throws `Marker probe did not record the file in Recent`; listed as ruling 173 `fail-closed-store`, evidence Inferred until the next PC ring. A real leak would now surface as `Marker appeared in telemetry or stderr`, which is not listed. |
| 3 | Desktop section-editor 2/2 | `Export_WriteFailure_CauseCopy_EarlierFileUntouched_NoTemp_H6` | `FAIL ... expected Can't write the file. Nothing was changed.; actual Can't write the file. You don't have permission to write to that folder. Nothing was changed.` | Verified from the line, Inferred for the mechanism: the step makes the target a directory (`taken.dat`). POSIX rename over a directory is an `IOException`; Windows refuses with access denied, an `UnauthorizedAccessException`, which the writer words as the permission cause. The test assumed the POSIX file model. | `bfa709a5` (2026-10-09) | **Fixed in the test**: expected text takes the platform branch on `OperatingSystem.IsWindows()`. Product note below (section 5). |
| 4 | Desktop properties-cells 2/2 | `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` | `FAIL ... InvalidOperationException: input ends 218.33, fact 217.33` | Verified: the host scale is 150 percent (`SCALE_CONTEXT ... RenderScaling=1.5 PrimaryScaling=1.5` in the isolated runs). The 1.0 DIP skew is the number `docs/proof/wri/investigation.md` row 6 recorded; the test carries `assume: DPI-A item 6`. Held by Ruling 178 and Ruling 186 (fractional-scale proof paused). Class DPI-A. Not a new failure: in the pre-WLK run too. | `ea2cab37` (2026-10-02) | **Not fixed, not listed (stopped)**: the manifest has no class for it and `CLASSES` is in `tools/check-expected-failures.py`, outside WRX's paths. See section 5. |
| 5 | Desktop properties-cells 1/2 | `PropertiesPane_Density_UnitOnValueBaseline` | `FAIL ... InvalidOperationException: input 143.87 unit 144.54 label 144.54; fact 120.54/120.54` | Inferred: unit and label sit 0.67 DIP below the input baseline, which is one device pixel at 150 percent (0.667 DIP). Same layout-rounding family as row 4 (DPI-A). Not in the WRI eight; in the pre-WLK run. The fact row agrees (120.54 both). | `ea2cab37` (2026-10-02) | Same as row 4: stopped. |
| 6 | Analysis 2/2 | `TeFloor_B10_SettingsHash_GoldenUnchanged` | `FAIL ... expected 9713863597250cd245c4d70560abee8840c002f6ff33501127ba331ac8efad62; actual 752ebf3bb52860261175c7f04c38c2df04488e09b437f84be4ef31a8479bd865` | Verified: the first assertion (`Settings.Default`, golden captured on main `affddb89`) gives a different SHA-256 on Windows. `RunRecord.SettingsHash` is BLAKE3 over JCS of the settings, so the **same settings produce a different run key on Windows than on macOS**. Inferred lead: number or string canonicalisation. That is a possible product defect (cross-platform run identity), not a test fixture. | `66cb1503` (2026-10-09) | **Untouched, per the brief. Not listed anywhere. Reported to the leader.** |

## 2. Manifest entries not seen in the failure list (21)

Method: the ring prints `EXPECTED-FAIL n` per job. For Core, `n` is exactly the number of existing Core manifest entries not named in Core 3/3 (see the count below), so each failed.

| Entry | Harness | Verdict | Basis |
|---|---|---|---|
| `Library_SecondInstanceSameRoot_EntryListed`, `Library_Save_PublishesHashNamedFile`, `Library_WriteFails_NothingPublished`, `Library_EntryBytes_ParseAsStandaloneSection`, `Library_ScanNameCollision_BothReported` | Core 1/3 or 2/3 | **Failed, expected (kept)** | Verified by count: Core 1/3 + 2/3 printed 9 + 7 = 16 expected failures; the existing Core manifest entries not named in 3/3 are exactly 16 (these 5, `Rollback_V2Oversized`, `Rollback_V2UnknownWorkspaceMember`, `Recent_ClearFails`, `LayoutLoad_Absent`, `LayoutSave_Conflict`, `PrefsSave_LayoutAndRecentConcurrent`, `LayoutSave_TwoQueuedSaves`, `Recent_RemoveWriteFails`, `PrefStore_TextSize_RoundTrip`, `Recent_Clear_NoFileContainsMarkerPath`, `StoreContract_CancelBeforePublish`). A name missing from the 16 would have left a failure the checker calls unexpected; none was printed for 1/3 or 2/3. The names themselves are Inferred from the count, because the log omits them. |
| the other 11 in that list | Core 1/3 or 2/3 | **Failed, expected (kept)** | Same count. |
| `Rollback_TextSizeV2_BytesUnchanged` | Core | **Renamed** to `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged` (`1344bdd2`) | Row 1. Entry renamed. |
| `PrefStore_TextSize_OutOfSetOrGarbled_100_BytesUnchanged` | Core | **Gone, split** by `1344bdd2` into `..._ValueError_ThatMemberDefaults_BytesUnchanged` and `..._StructureError_WholeFileDefaults_BytesUnchanged`, both wrapped in `OrSessionOnly` (NOT ASSESSED where the store is unsupported). Neither appears among the failures, so they pass on Windows (Verified by elimination: the 16-count leaves no room for them). | Entry **removed**. A pass would otherwise be UNEXPECTED-PASS. |
| `Cli_AnalyseRunKey_EqualsServiceOnCustomOp`, `Cli_InspectRuns_RevisionIsSessionLabel` | Cli | **Failed, expected (kept)** | Verified: `EXPECTED-FAIL 2 (manifest)` after `17 PASS`; the Cli harness prints no per-name FAIL lines in the ring output because its block was fully expected. |
| `Tampered_RunView_UnavailableNoLayersRowKept` | Desktop `--analysis` | **Not reached (kept)** | Verified: `--analysis` timed out at 120 s, and the PC's own localization names it the first uncompleted check. Run alone it fails in 1.8 s with `expected OK, got DOC-UNSUPPORTED-PERSISTENCE`, which carries the entry's fragment: the entry is correct. Not removed, because a timeout is not a pass. |

Count check: 16 + 2 renamed or removed + 2 Cli + 1 not reached = 21, the leader's number. Verified by script (scratch `list.py`): 21 entries not named in any `FAIL` line.

## 3. Timeouts (item 4; Recommend, no change made)

Verified from the raw log: `--shell-window --part=1/2`, `--plan-canvas --part=2/2` and `--analysis` each ended with `TIMEOUT ... after 120 s (CFD_TEST_CHILD_TIMEOUT_SECONDS); its process tree was killed`.
Ring wall 147 s (net 137.5 s), load 14.10 to 11.35, Desktop job 132 s, `C-4 Desktop took 132061 ms, over 49369 ms`. Core parts 87, 88 and 92 s; Analysis 13 and 15 s.

Run alone, on the same host (isolated files, Verified): shell-window check ran PASS in 5.8 s, plan-canvas PASS in 4.6 s, analysis ended in 1.8 s (failing with the expected `DOC-UNSUPPORTED-PERSISTENCE` at the first uncompleted check). So the 120 s is contention, not an intrinsic hang (the PC's own interpretation; agreed). The pre-WLK run printed no TIMEOUT line (grep of `windows-ring-summary.txt`), so these three timeouts are new in the WLK rerun. Inferred reason: before WLK, `WRT-HARNESS-ABORT` shapes aborted harnesses early; with the aborts gone, more checks run to the end.

Did today's tests add material time on the Mac? Today's (2026-10-10) changes under `tests/CfdWorkbench.Desktop.Tests`: only `PlanCanvasTests.cs` (5 lines) plus the new `PlanCombViewPrefsTests.cs` (10 checks, `004ab1e4`) reach these modes. Measured on the Mac (unloaded, Release, one process each, including start-up): shell-window 21 s and 19 s, plan-canvas 25 s and 27 s, analysis 18 s. The 10 new plan-comb checks cost 6.7 s of the 54 s of `COST` in `--plan-canvas` (12 percent), two of which fall in the part that timed out. PRF, WLK and HYG changed no shell-window or analysis check (`git log --since=2026-10-10` on `ShellWindowTests*`, `AnalysisPanelTests*`: no commits). A before/after Mac timing was not run: it needs a rebuilt old tree, and the 12 percent bound above answers the question.

Recommendation (not done): (a) the Windows Desktop job is one 132 s critical path against a 49 s calibrated budget; spreading the Desktop job (DR-ANA-10, already printed by C-4) is the lever, not the 120 s ceiling; (b) a per-check START line would turn "first uncompleted check" from an inference into a fact (the PC's note says the runner has none); (c) do not list the timeouts in the manifest: a timeout has no named test.

## 4. TeFloor (item 5)

`TeFloor_B10_SettingsHash_GoldenUnchanged` is **not listed** in `tests/expected-failures.windows.json` (the brief assumed it was; `git grep` finds it only in `TeFloorLabelTests.cs` and `docs/proof/dat/red-first.md`). It fails in both PC runs with the same two hashes. Untouched. It is the single remaining named Windows failure the Mac cannot classify, and the one most likely to be a product matter (row 6).

## 5. What WRX stopped

1. **Classes for rows 4 and 5.** `tools/check-expected-failures.py` accepts only `historical-probe` and `fail-closed-store`. The two PropertiesPane checks need a third class, for example `fractional-scale` (ruling 178 and 186, `until` "the fractional-scale proof reopens"), and a self-test case. `tools/` is not a WRX path. Proposed manifest entries, ready to paste once the class exists:

   - `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs`: fragment `input ends 218.33, fact 217.33`
   - `PropertiesPane_Density_UnitOnValueBaseline`: fragment `input 143.87 unit 144.54 label 144.54; fact 120.54/120.54`

   A fragment this tight also fails the ring if the scale or the layout changes, which is the intent. Before listing, the leader may prefer to fix the layout (a product change) after Ruling 186 lifts.
2. **Export copy (observation, not a defect claim).** On Windows an export aimed at a path that is a directory says "You don't have permission to write to that folder." The target is a folder, not a permission problem. The test now records that wording per platform; whether the writer should word it differently is a copy decision for the spec owner.
3. **`TeFloor` hash.** See row 6. A possible product determinism defect; stop and report.

## 6. Checks the PC must rerun (Windows)

1. `tools/run-tests.sh` (Git Bash), then read: `Export_WriteFailure_...` should now pass (the rest of that test after the directory step was never reached on Windows: Inferred risk of a second failure at the permission, missing-folder, disk-full or good-write steps).
2. `Telemetry_MarkerInjection_AbsentEverywhere`: expect `EXPECTED ... Marker probe did not record the file in Recent`. Any other message (`did not open the file`, `Marker appeared in telemetry or stderr`) is a finding: the first says the open itself failed, the second is a privacy defect. Please paste the line.
3. `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged` should now be `EXPECTED`.
4. `--shell-window --part=2/2` and `--section-editor --part=2/2` exit lines should now be `EXPECTED` (their only unexpected members were rows 2 and 3).
5. `CFD_TEST_ONLY=Tampered_RunView_UnavailableNoLayersRowKept` alone: confirm it still fails fast with `DOC-UNSUPPORTED-PERSISTENCE`.
6. The still-red items for the leader: the three timeouts, rows 4 and 5 (until the class lands), and TeFloor (row 6).
