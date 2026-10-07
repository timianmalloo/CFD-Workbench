---
id: proof-win-smoke-reverify
title: "W-1b Windows WFX re-verification receipt"
type: proof-pack
status: active
owner: "@win-smoke-reverify"
tags: [windows, smoke-test, wfx, evidence]
links:
  - { to: proof-wfx-pc-reverify, rel: implements }
  - { to: proof-win-smoke, rel: relates-to }
  - { to: review-pr-3, rel: depends-on }
review-by: "2026-11-07"
summary: "Native WFX re-verification after the Mac fixes. Live point names, undo/redo and save refusal are observed; keyboard menu and host shortcut limitations remain."
---

# W-1b Windows WFX re-verification

Goal: run the exact WFX Windows walk and one full application ring, preserving observed failures without product repairs.
Done when every requested row has an observed disposition and the owned evidence is committed.
Not in scope: product fixes, host configuration changes, persistence implementation, packaging, shared audit/index writes, push or PR.
Tier: bounded evidence verification. Fan-out cap: one assigned worker.

Confidence: **Verified** means directly observed in this run. **Not assessed** means the requested property was not established.
This receipt does not grant Windows platform acceptance.

## Execution and binding

The worktree is `C:\Projects\CFD-Workbench-win-smoke-reverify`, branch `win/smoke-reverify`.
`git fetch origin` followed by `git merge origin/main` reported already up to date.
The single tested source HEAD is **`defbe0a931e703068a4c06278a413c0cf6d7b6bc`**.

Plan: grounding and lightweight SDK/resolver preparation → coordinator concurrency release → one Debug build → one native launch → finite serial WFX inputs/captures → one ring and immediate raw-log copy → receipt/docs gate → owned-path commit.
No fan-out is used: desktop input and the machine CPU budget are exclusive resources. The coordinator held heavy work while W-3 ran, then explicitly released this build, native walk and ring. No heavy W-3 work overlapped this worker's released track.
The verification surfaces are native chrome/menu, rendered geometry and the same point's automation name/event, history/status, Analysis/results, save picker/status, and gesture-controlled docks/layouts. Each WFX expected value or state change is the oracle; a failed oracle is recorded rather than repaired.
The finite input list decreases to zero. Capture mechanics used two repair cycles; no product repair, launch retry or ring retry occurred.
Shared audit/index reconciliation belongs to the coordinator. The graph-engineering evidence directory referenced by the planning skill was absent on this source HEAD; no cost claim is based on it.

`launch.json` binds the source HEAD, apphost and DLL hashes, PID and start time. One process was launched directly from the Debug output directory with `Start-Process -WindowStyle Hidden`, with stderr/stdout retained.

- PID: **21424**; start: **2026-10-07T22:06:06.2763382Z**.
- Apphost SHA-256: `866144C3A3D37284B496FC867518897647D2D1401E48C4462FF38B3C64D33676`.
- Desktop DLL SHA-256: `6D5E8AACF85A9275CABF94F25FC966F9F9C1D95832CC81C9E712804CC5A06EC0`.
- Build/launch/ring environment explicitly loads persisted User PATH then Machine PATH and persisted User `DOTNET_ROOT=C:\Users\malla\.dotnet`.
- Every numbered PNG has a JSON sidecar with PID/start, source HEAD, both hashes, capture time, inputs, native title, DPI and UI Automation descendants. The start-time guard checks UTC ticks before each capture.
- DPI: **144 (150% of 96)**. Screenshot and automation sampling are consecutive rather than atomic. Captures `22`–`26` are occluded by ZoomIt and establish no rendered app outcome.

The first capture attempted before `MainWindowHandle` became available. The same PID was captured once startup completed; it was not relaunched. The initial pwsh click did not open the foil; invoking the scratch helper through Windows PowerShell did. This is the first capture-mechanics repair cycle; the precise input-host difference is **Not assessed**. The second cycle corrected picker foreground targeting; explicit clicks in the file-name field made the path inputs effective. Earlier path attempts did not modify the file name and their screenshots are retained. No further helper repair was attempted.
Scratch helpers remained outside the repository in the user's local Temp directory. They use Win32 window/cursor/mouse APIs, Windows Forms SendKeys, CopyFromScreen and Windows UI Automation. A C# `AutomationPropertyChangedEventHandler` records the same node's name events.

## Environment and checks

The fresh `powershell.exe -NoProfile -Command 'dotnet --version'` used the inherited PATH with **no PATH refresh**. It failed to resolve pinned SDK `10.0.203` and reported installed SDKs `9.0.315` and `10.0.301` under `C:\Program Files\dotnet\sdk` (`dotnet-fresh-shell.log`). Its exit value was not separately recorded. The inherited executable was `C:\Program Files\dotnet\dotnet.exe`; the persisted user SDK directory `C:\Users\malla\.dotnet\sdk\10.0.203` exists. The explicit build environment resolved `C:\Users\malla\.dotnet\dotnet.exe`.

| Check | Observed disposition | Evidence |
| --- | --- | --- |
| Debug solution build | Verified: exit 0; 17.012 measured seconds; 0 errors, 2 AVLN3001 warnings for CatalogDialog and SaveSectionDialog | `build.log`, `build.json` |
| `tools/py-resolve.sh --self-test` | Verified: 4/4 PASS, exit 0 | `python-resolver.log` |
| Resolver selection | Verified: `py -3`, exit 0 | `python-resolver.log` |

## Native walk

| Step | Direct observation | Evidence |
| --- | --- | --- |
| Menu row/chrome | Verified: File, Edit, View, Section, Window render above the dock and the status strip remains at the bottom. Captured app bounds do not include enough Windows title-bar chrome to prove its overlap/control behavior; that part is Not assessed | `01-launch` |
| Default foil | Verified: Untitled, 1000.00 mm span, 127.04 mm root chord, 12.70 mm tip chord, two NACA 0012 stations, approximately 1000 cm² area | `02-new-settled` |
| Spanwise drag | Verified: leading-edge root handle at (1319,550) dragged to (1369,550); length 166.67 → 226.40 mm, area approximately 1000 → 1005 cm² | `03-drag` |
| Same-node name/event | Verified: runtime id `[42,1182498,4,574506]` changed from 166.67 to 226.40 mm. Four NameProperty events carry intermediate 190.56, 208.48, 220.43 and final 226.40 mm names | `point-name-event.json` |
| Undo, Ctrl+Z once | Verified: one drag is undone, root handle 166.67 mm and area approximately 1000 cm²; the document remains open. One-step restoration excludes a second undo removing the new document. The settled strip reports the accepted slice, rather than an undo message; the requested undo-status text is not observed | `04-undo` |
| Redo, Ctrl+Shift+Z once | Verified: root handle 226.40 mm and area approximately 1005 cm² restored | `05-redo` |
| Edit menu | Verified: Undo enabled, Redo disabled after redo; displayed gestures Ctrl+Z and Ctrl+Shift+Z | `06-edit-menu` |
| Analysis/Evaluate | Verified: Ctrl+Shift+A after ZoomIt dismissal shows conditions/results. Evaluate completes in displayed 5.389 s, CL 0.177, CDi 0.00099, lift 240.38 N and induced drag 1.354 N | `28-ctrl-shift-a-clean`, `29-evaluate`, `30-evaluate-settled` |
| Ctrl+S | Verified: native picker titled Save native CFD Workbench project opens | `31-save-picker` |
| Valid save path | Verified: selecting `C:\Users\malla\AppData\Local\Temp\w1b-valid.cfdw.json` produces `DOC-UNSUPPORTED-PERSISTENCE: Save was not acknowledged. Resolve the refusal before retry.`. A pending picker remains over the first result; the status is visible below it and sampled in UIA | `36-save-valid-click-focus` |
| Invalid extension | Verified: selecting `C:\Users\malla\AppData\Local\Temp\w1b-invalid.txt` produces `DOC-TYPE: Save was not acknowledged. Resolve the refusal before retry.` with red strip/icon | `37-save-invalid-click-focus`, `38-save-status-settled` |
| File > Save | Verified: same picker and unsupported-persistence status after `.cfdw.json` choice | `39-file-menu`, `40-file-save-picker`, `41-file-save-result`, `42-file-save-settled` |
| Click File > New foil | Verified: one click from the visibly open menu yields one observed default-foil state and New foil status, clears analysis, restores approximately 1000 cm² area. Internal execution count is Not assessed | `57-file-new-menu-open`, `58-click-new-foil`, `59-click-new-result` |

Analysis unavailable/missing contributions remain visible scope limits, not numerical validation findings. Save remains intentionally refused pending W-2.

## Keyboard menu and single-fire dispositions

| Input | Direct observation and limit | Evidence |
| --- | --- | --- |
| Alt | Verified: clean Alt press/release focuses File | `44-alt-clean` |
| Right, Down | Verified: moves to Edit and opens its menu with Ctrl gestures | `45-arrow-clean` |
| Escape | Verified: closes the menu. Focus remains Edit rather than returning to the preceding Evaluate focus; return-focus expectation fails in this observation | `43-f10-clean`, `46-escape-clean` |
| F10 | Verified finding: no menu focus/highlight; focus remains Evaluate. F10 expectation fails | `43-f10-clean` |
| Alt+F | Verified finding: no File menu opens; access-key expectation not established | `12-alt-f` |
| Tab / F6 fallback | Not assessed: a Tab press records an empty focused name; the subsequent F6 press records the external Address and search bar. These inputs did not establish menu focus or the app F6 ring; no further capture repair was attempted at the two-cycle cap | `60-menu-tab`, `61-menu-f6` |
| Ctrl+Z / Ctrl+Shift+Z | Verified single visible undo/redo restoration as above | `04-undo`, `05-redo` |
| Ctrl+B | Verified: left sidebar disappears once, and a separate restore press shows it again | `13-ctrl-b`, `14-ctrl-b-restore` |
| Ctrl+J | In CAD it reports that the panel belongs to Analysis. In Analysis, one press hides the result panel and a separate press restores it | `15-ctrl-j`, `49-analysis-bottom-fold`, `50-analysis-bottom-restore` |
| Ctrl+K | Verified: one palette input opens. Internal execution count is Not assessed; opening an already-open palette is idempotent, so screenshots alone cannot exclude duplicate invocation | `17-ctrl-k` |
| Ctrl+= / Ctrl+- | Verified: plan handle-to-root horizontal UIA distance 199 → 249 → 199 px, consistent with one 1.25 zoom step each within rounding; status Zoomed in/Zoomed out | `19-plan-focus`, `20-ctrl-equals`, `21-ctrl-minus` |
| Ctrl+1 / Ctrl+2 / Ctrl+3 | Not assessed: machine-global ZoomIt takes focus (`Zoomit Zoom Window`) after Ctrl+1; the later inputs/captures are occluded. No host configuration was changed. Escape dismissed the overlay before further app input | `22-ctrl-1`–`27-overlay-dismiss` |
| Ctrl+Shift+A | Verified: one clean press changes CAD to Analysis. The earlier press while ZoomIt had focus establishes no app outcome | `25-ctrl-shift-a`, `28-ctrl-shift-a-clean` |

No Mac command/modifier symbols were seen in the expanded File, Edit, View (including Pan), Section and Window menus. Shown Ctrl gestures are screenshot evidence; unopened nested menus are **Not assessed**. The sidecar encoding replaces some non-ASCII text characters, so screenshots are the oracle for glyph claims.
There is no emitted command-invocation counter in this evidence method. Visible reversible transitions prove the listed single-step effects; idempotent commands and workspace shortcuts do not receive an exact internal count claim.

## Full application ring

Verified: `bash --noprofile --norc tools/run-tests.sh` ran exactly once with the persisted user SDK environment. It progressed past the resolver, built Release with 0 errors and the two known AVLN3001 warnings, then reached every harness. Exit **1**. `ring.json` records tested HEAD `defbe0a931e703068a4c06278a413c0cf6d7b6bc`, `DOTNET_ROOT=C:\Users\malla\.dotnet`, executable and measured outer duration **61.491 s**. The ring reports wall **60184 ms**, build **11155 ms**, net **49029 ms**, load **14.49 → 9.10**. Its printed `cpu 2 s` is retained as emitted and is not interpreted as total Windows harness CPU work.

Raw `.tmp-tests` log/timing files were copied immediately on completion to `raw-ring/`, before any other ring could delete them. `test-ring.log` preserves the complete ring output. No retry occurred.

| Harness | PASS count | Named failures | Exit / result |
| --- | ---: | ---: | --- |
| Core 1/3 | 215 | 13 | exit 1 |
| Core 2/3 | 215 | 13 | exit 1 |
| Core 3/3 | 216 | 11 | exit 1 |
| Desktop | 0 named PASS | no failing test name emitted | exit 70 |
| Analysis 1/2 | 111 | 1 | exit 1 |
| Analysis 2/2 | 120 | 0 | RESULT failures=0 |
| Cli | 3 | 1 | exit 127 |

Core partitions each report `of 683 checks`; Analysis partitions each report `of 21 groups`. No partition completeness failure was emitted. The budget verdict after cost checks was not reached because suite/cost failures already set the ring failure.

Desktop raw output is exactly `SelfLaunchTests: all 6 cases passed.` then `APP-UNHANDLED APP-CRASH System.Exception`. **Crash frame: Not recorded**; no stack frame or failing check name exists in the raw Desktop log. Source `src/CfdWorkbench.Desktop/Program.cs` `StartupFailure.Install` prints the exception type then calls `Environment.Exit(70)`; it emits no stack. A frame cannot be recovered from that output. No rerun, debugger attachment or product instrumentation change was authorized for this evidence-only track. Remaining Desktop checks are Not assessed.

Cli's first exception is `System.InvalidOperationException: inspect --runs returned 3`, with payload `{"code":"DOC-UNSUPPORTED-PERSISTENCE"}`. Its first frame is `Program.<Main>$(String[] args)` in `tests/CfdWorkbench.Cli.Tests/CliTests.cs:line 179`.

Every named failing test and its first emitted error line follows. Long multi-line hash comparisons are preserved in full in the raw log.

| Raw log | Failing test | First error |
| --- | --- | --- |

| `Analysis.part1of2.log` | `NeuralFoil_Family_CatalogNaca0012_DerivedNotLabelled` | `ContractError: a catalog file failed its check` |
| `Cli.log` | `Cli_AnalyseRunKey_EqualsServiceOnCustomOp` | `Unhandled exception. System.InvalidOperationException: inspect --runs returned 3: {` |
| `Core.part1of3.log` | `Catalog_VendAndLink_NoCoordinates` | `ContractError: a catalog file failed its check` |
| `Core.part1of3.log` | `CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX` | `ContractError: a catalog file failed its check` |
| `Core.part1of3.log` | `Library_Save_PublishesHashNamedFile` | `ContractError: DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part1of3.log` | `Library_WriteFails_NothingPublished` | `InvalidOperationException: Expected LIB-IO; actual DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part1of3.log` | `LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9` | `InvalidOperationException: Expected -1; actual 1` |
| `Core.part1of3.log` | `Rollback_V2UnknownWorkspaceMember_BytesUnchanged` | `InvalidOperationException: missing LAYOUT-VERSION [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` |
| `Core.part1of3.log` | `LayoutSave_ClaimBusyHashSame_RetriesOnce` | `InvalidOperationException: Expected True; actual False` |
| `Core.part1of3.log` | `LayoutSave_StaleClaim_SessionOnlyNamesClaim` | `InvalidOperationException: Expected claim-held; actual failed` |
| `Core.part1of3.log` | `Recent_Clear_NoFileContainsMarkerPath` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part1of3.log` | `StoreContract_CancelBeforePublish_DocCancelled` | `InvalidOperationException: Expected False; actual True` |
| `Core.part1of3.log` | `PrefStore_TextSize_OutOfSetOrGarbled_100_BytesUnchanged` | `InvalidOperationException: {"format":"cfdw-display","version":1,"textSize":175}: missing DISPLAY-SCHEMA [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` |
| `Core.part1of3.log` | `PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites` | `IOException: A required privilege is not held by the client. : 'C:\Projects\CFD-Workbench-win-smoke-reverify\.tmp-tests\p1-01282e7110e64d94bfe7dc73c9a9fac6\prefs'.` |
| `Core.part1of3.log` | `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged` | `InvalidOperationException: Expected blended-dihedral.foil 943866A4A779DE0CD263065574A79453E7C64D8E64664A2B8C355B2C265DAEDE` |
| `Core.part2of3.log` | `Catalog_GenEntries_RegenerateToRecordedHash` | `ContractError: a catalog file failed its check` |
| `Core.part2of3.log` | `Catalog_Fairings_NeverListed` | `ContractError: a catalog file failed its check` |
| `Core.part2of3.log` | `Catalog_GenNeverThroughDatParse` | `ContractError: a catalog file failed its check` |
| `Core.part2of3.log` | `Library_EntryBytes_ParseAsStandaloneSection` | `ContractError: DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part2of3.log` | `Library_ScanNameCollision_BothReported` | `ContractError: DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part2of3.log` | `Replace_Preview_EmitsCatalogPreviewOutcome` | `InvalidOperationException: Expected ReplaceEvent { Scope = draft, Stations = 2, ResidualChord = 8.122861410878169E-05, Spacing = current, Family = Naca, Class = Gen }; actual ReplaceEvent { Scope = draft, Stations = 2, ResidualChord = 8.122861410878169E-05, Spacing = current, Family = , Class = Gen }` |
| `Core.part2of3.log` | `LayoutLoad_Absent_ReturnsPresets` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part2of3.log` | `Rollback_V2Oversized_BytesUnchanged` | `InvalidOperationException: missing LAYOUT-VERSION [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` |
| `Core.part2of3.log` | `PrefStore_SymlinkedDirectory_SessionOnly` | `IOException: A required privilege is not held by the client. : 'C:\Projects\CFD-Workbench-win-smoke-reverify\.tmp-tests\p1-f0a5df6e267047f89a5d754004a1d806\prefs'.` |
| `Core.part2of3.log` | `LayoutSave_Conflict_MergesChangedWorkspaceOnly` | `InvalidOperationException: Expected True; actual False` |
| `Core.part2of3.log` | `Recent_Conflict_ReappliesAdd` | `InvalidOperationException: Expected True; actual False` |
| `Core.part2of3.log` | `Recent_Remove_KeepsEveryOtherEntry` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part2of3.log` | `Rollback_TextSizeV2_BytesUnchanged` | `InvalidOperationException: missing LAYOUT-VERSION [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` |
| `Core.part3of3.log` | `Library_DuplicateIgnoringCaseNfc_Refused` | `ContractError: DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part3of3.log` | `Library_StaleClaim_ReportedNotDeleted` | `InvalidOperationException: Expected LIB-CLAIM-HELD; actual DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part3of3.log` | `Library_SecondInstanceSameRoot_EntryListed` | `ContractError: DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part3of3.log` | `Rollback_V2UnknownTopLevel_BytesUnchanged` | `InvalidOperationException: missing LAYOUT-VERSION [LAYOUT-SESSION-ONLY,DOC-UNSUPPORTED-PERSISTENCE]` |
| `Core.part3of3.log` | `PrefsSave_LayoutAndRecentConcurrent_BothKept` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part3of3.log` | `LayoutSave_TwoQueuedSaves_UnionOfChangedWorkspaces` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part3of3.log` | `Recent_ClearFails_ReportedNotCleared` | `InvalidOperationException: Expected LAYOUT-VERSION; actual DOC-UNSUPPORTED-PERSISTENCE` |
| `Core.part3of3.log` | `Recent_RemoveWriteFails_ListUnchanged` | `FileNotFoundException: Could not find file 'C:\Projects\CFD-Workbench-win-smoke-reverify\.tmp-tests\p1-690ffdd5a5b946a4ad56a184d143917a\recent\recent.json'.` |
| `Core.part3of3.log` | `PrefStore_TextSize_RoundTrip` | `InvalidOperationException: Expected saved; actual session-only` |
| `Core.part3of3.log` | `PrefStore_TextSize_PriorRoot_LayoutAndRecentUntouched` | `InvalidOperationException: Expected saved; actual failed` |
| `Core.part3of3.log` | `PrefStore_TextSize_LoadAndSaveSerialized_NoStaleHash` | `InvalidOperationException: Expected saved; actual session-only` |

Every cost failure (10 total: 2 suite C-2 failures plus all 8 individual C-5 failures):

```text
FAILED: C-2 Analysis.part1of2 took 13202 ms, over 5000 ms
FAILED: C-2 Analysis.part2of2 took 11380 ms, over 5000 ms
FAILED: C-5 Section_EditedProfileNoPolar_Unavailable took 633.192 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 Polar_ProductRun_ReachesStripsAndSectionProjection took 555.677 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 SectionProjection_EstimatorLabel_DepthAware took 589.108 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 SectionView_Speeds_FollowUnitsSwitch took 643.55 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 DeriveVerdicts_TaperedPlanform_SweepIsTheLatticeSweepAndAlphaL0IsTheStripSection took 1018.548 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 DeriveVerdicts_AlphaBound_9p9InsideAnd10p1Outside took 837.25 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 DeriveVerdicts_OnlyTheTipReasonGetsTheTipRule took 697.088 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
FAILED: C-5 Section_WingRun_PanelValuesAtEveryStation took 797.686 ms, over 500 ms (move it to readiness with its cost, or make it cheaper)
```

Cost output reports `10 failures, 0 COST-MISS (load 9.10)`. No cost failure was fixed or rerun.

## Final validation and residuals

Verified: `py -3 tools/check-docs.py` exited **1**. Its final graph diagnostic is `validate: 1 defect(s) - 0 problem(s), 0 orphan(s), 1 index-drift item(s).` The sole defect is `file not in index: proof-win-smoke-reverify` (`docs-check.log`). The worker cannot write the excluded shared index; the coordinator owns derivation and the final green gate. No green docs result is claimed here. The SPIRAL red line earlier in that log belongs to its exercised self-test vector; the later actual spiral check reports `ok (73 commits, 13 product)`.

Actual plan: preparation and coordinator hold → release → one Debug build → one native launch → finite UI inputs and two capture-mechanics repairs → one Release ring and immediate raw preservation → receipt and docs gate. Native tested source and ring tested source are the same HEAD. No overall duration or token count is available; build/ring elapsed measurements are retained in their JSON files. No product repair, ring repeat, relaunch, host configuration change, push or PR occurred.

The original native app remains at a fresh default foil for operator review. Residuals: absent settled undo-status message; F10/menu focus return; Alt+F access keys; Tab/F6 input targeting; ZoomIt-intercepted workspace gestures; internal counts of idempotent commands; incomplete chrome bounds; unopened nested-menu glyphs; Desktop crash name/frame; failed Windows harnesses/cost gates; shared index reconciliation; and persistence intentionally refused pending W-2. The receipt completes the bounded evidence disposition while leaving these failures and unmeasured properties explicit.
