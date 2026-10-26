---
id: proof-st-store-codes
title: ST store-code, copy and preference proof
type: proof-pack
status: active
owner: "@st"
tags: [store, windows, testing, telemetry]
links:
  - {to: coordination-wave3-next-steps, rel: implements}
review-by: 2026-11-26
summary: Observed ST store-code, copy and preference checks. Dispatch 2 records the genuine old-build fixture's axes refusal while leaving COPY-615 dimensional-headline evidence open.
---

# ST dispatch 1

## Dispatch 2 plan and evidence

Goal: ST items 6–10. Done when their stated evidence is observed, or an honest NOT DONE reason is recorded, and the full suite plus every failed Desktop mode's alone rerun is read. Tier T1; width 1. No schema or copy wording changes beyond the approved COPY-644 retirement.

Execution graph: ground → (6 red → fix → green → commit) → (7 red → fix → green → commit) → (9 red → fix → green → commit) → (8 historical-fixture provenance → open/compare test → commit or report) → (10 red → fix → green → commit) → full suite → failed Desktop modes alone → proof/audit commit. Each item's test result is the decision edge to its fix. Serial order follows the dispatch contract; no agent fan-out. The remaining failing assertions decrease through each repair; a repeated unrelated failure becomes a finding. Budget: 50 calls / 105 minutes, then report incomplete work without dropping proof requirements.

Surface list: COPY registry → Labels → StartView; platform admission → Save-section refusal label → dialog; preference folder owner → layout/recent/display load result → session-only save gate; existing-folder open → owner read → DACL update; old document → codec → stored run admission → compare projection. Existing telemetry remains the emitting source for store actions; no new data fields are introduced.

### 6 — one active missing-file definition

The scan reads DESIGN.md's COPY rows, excludes only rows marked `superseded by`, and counts the missing-file cause phrase. The two active rows make it fail. StartView now uses COPY-125 for every Missing code, including DOC-CONFLICT; its file headline and actions supply the rest of that row.

```text
FAIL Labels_MissingFile_OneActiveDefinition_Copy125 InvalidOperationException: active missing-file definitions: COPY-125, COPY-644 expected 1; actual 2
PASS Labels_MissingFile_OneActiveDefinition_Copy125
```

### 7 — Save-section sentence follows store admission

ProjectStore exposes its existing admission predicate, and Supported uses that same definition. SaveSectionDialog accepts an injected predicate for the host-OS negative test. Both unsupported store codes use the system sentence when unadmitted; admitted stores retain the folder sentence. No wording changed.

```text
FAIL StoreCodes_SaveSection_UnadmittedHost_UsesSystemSentence Exception: DOC-UNSUPPORTED-PERSISTENCE unadmitted host: CFD Workbench can't save to the My sections folder safely (it may be in a network, removable, linked or cloud-synced folder). Nothing was saved.
PASS StoreCodes_SaveSection_UnadmittedHost_UsesSystemSentence
```

The first build attempt was blocked by a leftover Desktop.Tests process (PID 9064). Its executable path was verified as this tree's test apphost before termination. The stale apphost's SELECTOR failure is not the red proof above.

### 9 — preference load ownership

The real Windows system directory supplies a foreign-owned folder without elevation. Layout, recent, text size, units and comb loads must all return session-only. Subsequent layout/display saves must remain session-only, the store factory must never run, and the folder's owner/DACL must remain equal. The load check applies to both the installation root and each preference subfolder. Missing folders keep first-run behaviour; owner-probe I/O/access failures fail closed. macOS keeps its existing admission (this item concerns Windows ownership).

```text
FAIL PrefStore_ForeignOwnedFolder_LoadIsSessionOnly_NoWrite InvalidOperationException: Expected True; actual False
PASS PrefStore_ForeignOwnedFolder_LoadIsSessionOnly_NoWrite
```

### 8 — genuine old-build fixture, dimensional branch NOT DONE

Fixture: `docs/proof/a3a-old-build/one-run.cfdw.json`, introduced by 91501f10 (2026-10-04 11:22 -0700), a verified ancestor of 0b277815 (2026-10-04 18:08 -0700), which introduced `ReferenceQuantities.cs`. The original receipt describes the STO writer's `RecordRun` + `SaveImage` production. SHA-256 is pinned to that receipt: `9edbe94b74396a7c97fa72096b15b9f1317a27f79b9e612139c2234e9f0fd929`. The test opens those unchanged committed bytes, computes a modern estimator at the stored run's revision/point/water, and compares both orientations through production coefficient derivation and CompareProjection.

The first expectation was too broad: `FAIL Compare_OldBuildStoredRun_Copy615Reason InvalidOperationException: old versus modern references expected ReferenceDiffers; actual Blocked`. Inspection of Compare.DimensionalApplies established that an axes mismatch correctly requires the full block (design 12.1). This fixture's axes are `body`; modern axes are `body; wind for lift and drag`. The corrected characterization requires the exact reference/axes refusal and no outputs in either orientation. This is not a COPY-615 defect and does not close its dimensional-headline gap. No old run was rewritten or resealed to force that branch. Item 8 remains NOT DONE: an honest old-build fixture reaching the dimensional branch was not established.

### 10 — owner and ACL use one held handle

The existing-folder path opens once with READ_CONTROL | WRITE_DAC, reads the owner from that handle and passes that same handle to SetSecurityInfo. Access denied on that open fails closed to session-only. Foreign owners remain untouched. The separate read-only path owner probe is for preference loads and is not called by this transaction.

Dispatch 2 originally used the structural `check-one-handle.py` assertion. Repair 1 below supersedes that evidence with the harness runtime test and three observed mutant failures. The structural script and its run.sh invocation are deleted.

```text
FAIL Preferences_OneHeldHandle_OwnerAndDacl owner check reopens the path before the DACL handle
PASS Preferences_OneHeldHandle_OwnerAndDacl
FAIL Preferences_OneHeldHandle_InjectedSecondOpen existing folder must be opened exactly once (expected mutant failure)
PASS Preferences_OneHeldHandle_MutationRejected
```

### Dispatch 2 review and limits

The brief is the compiled contract. All five requested increments were assessed serially; items 6, 7, 9 and 10 have observed red/green evidence, while item 8 has a passing historical characterization and an explicit unresolved dimensional-branch requirement. The solution-selection ladder reused existing Labels, ProjectStore admission, preference session flags and native SafeFileHandle APIs; no dependency or durable field was added.

Testing triggers: D0 applies throughout; deterministic rule checks cover copy cardinality/admission; UI failure-state checks render the Save dialog; native boundary checks cover real ownership and ACL publication; historical-state characterization uses committed bytes and production readers. The single-handle structural guard has an explicit second-open mutation. Failure dispositions remain existing messages or session-only; no new public error code or copy row was introduced. Existing store telemetry and test COST lines remain the measurement sources.

| Claim | Oracle / evidence | Confidence / remaining limit |
|---|---|---|
| One active missing-file definition | Two rows fail cardinality; COPY-125 body matches Labels; StartView shares it for every Missing code | Verified on this tree |
| Save-section follows admission | Inject false on Windows; rendered SaveError must equal the system sentence for either unsupported code | Verified on Windows; existing host-predicate catalog test also passes |
| Foreign-owner load is session-only | Real foreign-owned folder; all five loads; store factory never called; owner/DACL unchanged | Verified on Windows; macOS owner policy unchanged |
| ACL update uses owner handle | Explicit scoped assertion; injected second open rejected; real owned/foreign folder checks pass | Verified source assertion and Windows behaviour; helper-spelled reopen outside the guard is a residual limit |
| Old-run COPY-615 dimensional reason | Genuine pre-reference fixture opens and compares; axes mismatch correctly blocks output | NOT DONE for dimensional headline; no eligible fixture established |

Independent pre-merge reviewer clearance is not claimed. The author has not cleared its own hard veto. No new UI layout, design token, generated asset, HTTP endpoint or persisted schema was introduced.

Verified on Windows. Reproduce focused green checks with `bash docs/proof/st-store-codes/run.sh`.
The CLI harness has the coordinator-reported environment defect: its sleeping child cannot resolve ping through the over-long User PATH. It is outside ST's scope.

Surface list: native causes → store code → CLI exit map; controller open → OpenFailure → StartView sentence; save → Labels and Warning status; display/layout/recent preference load → session-only. Telemetry: actual Open/Save/Clear calls → RecordPersistence → session ring, with read-origin on successful reads and the same trace as store.read. No persisted project schema changed.

The approved platform-save text COPY-31W and location sentences COPY-641/COPY-589 remain intact. New COPY-677/678/679 are proposed; COPY-680 is a proposed appended drive remedy. Owner wording review remains with the coordinator before join.

Win32 numbers were checked with Python ctypes.FormatError, which uses Windows FormatMessage:
4390: The file or directory is not a reparse point.
4395: The object manager encountered a reparse point while retrieving an object.
4390 retains its existing unsupported-path mapping; both codes fail closed under DOC-UNSUPPORTED-PERSISTENCE.

The dispatcher test initializes Avalonia before measuring CheckAccess; an uninitialized dispatcher is not a valid thread oracle. Both existing and missing paths are tested. The save-remedy assertion runs only on Windows, while the pure Windows Open-copy mapping runs on either host.

## Exact red and green lines

### 1 Desktop

```text
FAIL StoreCodes_NewCodes_ReachEverySurface Exception: DOC-UNSUPPORTED-PLATFORM status=Error; DOC-UNSUPPORTED-PLATFORM StartView=It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed.|Code: DOC-UNSUPPORTED-PLATFORM; DOC-UNSUPPORTED-PLATFORM preferences=never-write; DOC-NOT-A-FILE status=Error; DOC-NOT-A-FILE StartView=It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed.|Code: DOC-NOT-A-FILE; DOC-NOT-A-FILE preferences=never-write
PASS StoreCodes_NewCodes_ReachEverySurface
```

### 1 Native mapping

```text
FAIL WindowsStore_NewCodes_Mapping InvalidOperationException: Expected DOC-UNSUPPORTED-PLATFORM; actual DOC-UNSUPPORTED-PERSISTENCE
PASS WindowsStore_NewCodes_Mapping
```

### 1 CLI

```text
FAIL Cli_NewStoreCodes_ExitThree DOC-UNSUPPORTED-PLATFORM exit=2
PASS Cli_NewStoreCodes_ExitThree
```

### 2 Drive

```text
FAIL StoreCodes_NonNtfsRemedy_NamesDrive Exception: drive/NTFS remedy absent: Open=CFD Workbench can't open this file from here. It may be in a folder or on a drive that CFD Workbench can't use safely, such as a network, removable, linked or cloud-synced folder, or its name or path may be one CFD Workbench can't use. The file hasn't been changed. Copy the project to a local folder, give it a short plain name, and open it from there.|Code: DOC-UNSUPPORTED-PERSISTENCE; Save=CFD Workbench can't save to this location or over this file. Your changes are kept in this session. Use Save As to choose a local folder.
PASS StoreCodes_NonNtfsRemedy_NamesDrive
```

### 3 Win32

```text
FAIL WindowsStore_ReparseErrorCodes_4390And4395 InvalidOperationException: Expected DOC-UNSUPPORTED-PERSISTENCE; actual DOC-IO
PASS WindowsStore_ReparseErrorCodes_4390And4395
```

### 4 Dispatcher

```text
FAIL StoreCodes_ConflictExistenceProbe_OffDispatcher Exception: existence probe dispatcher=True
PASS StoreCodes_ConflictExistenceProbe_OffDispatcher
```

### 5 Operations

```text
FAIL Store_Operation_Open InvalidOperationException: Expected store.read; actual document.reopen
FAIL Store_Operation_Save InvalidOperationException: Expected store.save; actual document.save
FAIL Store_Operation_Clear InvalidOperationException: Expected store.clear; actual document.save
FAIL Store_Operation_ReadOrigin InvalidOperationException: Expected 1; actual 0
PASS Store_Operation_Open
PASS Store_Operation_Save
PASS Store_Operation_Clear
PASS Store_Operation_ReadOrigin
```

## Single full-suite run

```text
== CfdWorkbench.Core.Tests: 915 passed, 7 skipped, 0 failed, exit 0
== CfdWorkbench.Analysis.Tests: 307 passed, 1 skipped, 2 failed, exit 2
failed CfdWorkbench.Tests.HarnessTests.Check(name: "Labels_NoRawDocDslCodeAloneOnAProductSurface") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Harness_ExitsZero_AfterPrintingChecks (0ms)
FAILED: CfdWorkbench.Analysis.Tests (exit 2)
== CfdWorkbench.Desktop.Tests: 890 passed, 1 skipped, 6 failed, exit 2
failed CfdWorkbench.Tests.HarnessTests.Harness_ExitsZero_AfterPrintingChecks (150ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "--controller-shell") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "TIMEOUT") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "StoreCodes_ConflictExistenceProbe_OffDispatcher") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "--section-editor") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "OpenRefusal_Sentences_AreTheLabelsConstants") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "--section-editor") (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "OpenRefusal_Sentences_AreTheLabelsConstants") (0ms)
FAILED: CfdWorkbench.Desktop.Tests (exit 2)
== CfdWorkbench.Cli.Tests: 32 passed, 1 skipped, 2 failed, exit 2
failed CfdWorkbench.Tests.HarnessTests.Harness_ExitsZero_AfterPrintingChecks (256ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "InspectRuns_ChildHarness_SleepingChild_KilledAtThe"Â·Â·Â·) (0ms)
failed CfdWorkbench.Tests.HarnessTests.Check(name: "InspectRuns_ChildHarness_SleepingChild_KilledAtThe"Â·Â·Â·) (0ms)
FAILED: CfdWorkbench.Cli.Tests (exit 2)
TESTS-RED 277 s
```

The full suite was invoked once. No green suite is claimed unless its summary says TESTS-GREEN. macOS native execution and independent reviewer clearance are not established by this Windows run.

## Follow-up of suite failures

The single run was TESTS-RED in 277 s: Core 915 passed / 7 skipped / 0 failed; Analysis 307 / 1 / 2; Desktop 890 / 1 / 6; CLI 32 / 1 / 2. Counts include each harness exit assertion.

The ST failures were fixed and verified separately:
- Analysis's raw-code guard required `(DOC-NOT-A-FILE)` in the proposed Save sentence. COPY-679 now includes it. Analysis rerun: 309 passed, 1 skipped, 0 failed.
- `OpenRefusal_Sentences_AreTheLabelsConstants` expected exactly COPY-641 without the new drive suffix. Its assertion now includes COPY-680; the focused check passed.
- `FAIL --controller-shell exited 124` and `FAIL TIMEOUT --controller-shell after 120 s; in-flight check: StoreCodes_NonNtfsRemedy_NamesDrive`: earlier controller checks can bind an uninitialized dispatcher or leave a synchronization context installed. The affinity check now runs in a fresh child; the ST checks preserve and clear the surrounding synchronization context. Required alone rerun with CFD_TEST_ONLY unset: 156 PASS, 0 FAIL, exit 0. All three StoreCodes checks passed. There was no heap-corruption crash.
- CLI's remaining `InspectRuns_ChildHarness_SleepingChild_KilledAtTheLimit` failure is the coordinator-reported PATH/ping environment defect. It was not changed. `PASS Cli_NewStoreCodes_ExitThree` was observed.

No second full-suite invocation was made. The join must run its authoritative suite; a completely green full suite is not claimed. The scripted Windows reproduction uses apphosts; macOS native persistence remains unexecuted here.

Additional edited repository files beyond the original authored-file list: src/CfdWorkbench.Core/AuthoringSession.cs (explicit Amendment 2 region), tests/CfdWorkbench.Desktop.Tests/ControllerShellOneDriveTests.cs (Amendment 2 permits unowned files), this proof and run.sh, docs/plans/st-dispatch1.md, docs/lessons/defect-classes.md, docs/docs-index.js, and audit log / derived audit-data. No other track's owned region was edited.

The requested graph hub kb-graph-and-loop-engineering is absent from this tree's index; the plan links to the actual upstream coordination report instead. This is a finding, not a repaired pack dependency. No independent review clearance is claimed.

### Dispatch 2 complete suite and required alone reruns

The required single `bash tools/run-tests.sh` invocation finished TESTS-RED in 268 s. Core: 916 passed / 7 skipped / 0 failed; Analysis: 311 / 1 / 0; Desktop: 891 / 1 / 7 (exit 2); CLI: 34 / 1 / 0. No heap-corruption crash occurred. The controller child exited 124 at 120 s, in StoreCodes_NonNtfsRemedy_NamesDrive. Shell-window part 2/2 and section-editor part 1/2 exited 1 on the stale COPY-644 assertions.

All three modes were rerun alone with CFD_TEST_ONLY unset. The repeated stale copy assertions were corrected to COPY-125. The new Save-dialog test had left Avalonia's synchronization context installed; restoring its entry context fixes the later synchronous store checks. The before controller rerun was bounded at 150 s and its own process tree was killed on timeout.

```text
before --shell-window: 120 PASS, 1 FAIL
FAIL Copy_OpenFailures_MatchesDesignRows InvalidOperationException: COPY-644: expected 'The file isn't there any more. It may have been moved, renamed or deleted. The file hasn't been changed. - superseded by COPY-125 (wave 3 ST, merged) ú Locate. ú Open another file.', built 'It isn't where it was - it may have been moved, renamed or deleted. The file hasn't been changed. ú Locate. ú Open another file.'
before --section-editor: 186 PASS, 1 FAIL
FAIL OpenRefusal_ConflictOnAbsentFile_IsMissing InvalidOperationException: It isn't where it was - it may have been moved, renamed or deleted. The file hasn't been changed.
before --controller-shell: 155 PASS, 1 FAIL
FAIL ST-RERUN-TIMEOUT standalone mode exceeded 150 s; own process tree killed
after --shell-window: 121 PASS, 0 FAIL
after --section-editor: 187 PASS, 0 FAIL
after --controller-shell: 157 PASS, 0 FAIL
```

A complete Desktop project rerun after these repairs (all modes, no selector) returned:

```text
Test run summary: Passed!
total: 897
failed: 0
succeeded: 896
skipped: 1
duration: 1m 17s 859ms
```

This establishes the repaired Desktop project result, not a green exit from the earlier run-tests.sh invocation. No second full run-tests.sh invocation was made. The strict green-script requirement remains unobserved. Item 8's dimensional-headline evidence also remains open.

Planned five serial item assessments and one full script run; actual five assessments, one full script run, three before/after mode reruns, and one complete Desktop project rerun. Rework: one stale-apphost build retry, two corrected test compile signatures, historical axes/field expectations corrected from observed compatibility, and the two copy assertions plus context cleanup found by the full suite. Independent review remains pending. No dispatch-1 production change was reworked; item 6 required updating its prior missing-file assertions.

## ST repair 1 — Opus ACCEPT-WITH-CONDITIONS

Goal: address review items 1–8 in the assigned ST tree. Done when the targeted regressions and three native mutants are observed red, repaired checks pass, the full script and each affected Desktop mode are reported, and the repair plus audit are committed. Tier T1; fan-out cap 1; 45 calls / 90 minutes. Wave-plan item 8 stays NOT DONE under Owner Ruling 267. No new approved copy or change to COPY-615 is authorized.

The optimize-graph pass uses the existing proof as its record rather than creating another plan. The serial dependency graph is grounding → red assertions → minimal fixes → native/fixture mutations → full script → isolated Desktop reruns → docs checks → audit/commit. Verification nodes are immovable. Each mutation restores source in a finally block; its exit is an observed assertion failure, followed by the real-code pass. No agents were delegated. Test Architect and Security lenses are the supplied Opus conditions; their independent acceptance remains with the main session.

Surface list: store admission → SectionLibrary → Save-section error; native open/owner/DACL transaction → Core harness; volume admission → ContractError.Reason → OpenFailure → StartView and SaveResult.UnsupportedReason → controller → Labels; preference owner read → session-only load/save; historical run integrity → compare projection; copy register and operation-name documentation. The refusal cause is transient, one cause per failed operation, and adds no durable document schema or stored history. The owner probe and native counters are internal test seams; default behavior still uses the real OS calls.

### 1. SectionLibrary refusal propagation — Verified

The factory constructs a real ProjectStore with platform admission refused on this host. Before the fix:

```text
FAIL Library_UnadmittedHost_PassesPlatformCode InvalidOperationException: Expected DOC-UNSUPPORTED-PLATFORM; actual LIB-IO
FAIL Library_FolderRefusal_PassesNotAFileCode InvalidOperationException: Expected DOC-NOT-A-FILE; actual LIB-IO
```

After passing both codes through (alongside DOC-UNSUPPORTED-PERSISTENCE):

```text
PASS Library_UnadmittedHost_PassesPlatformCode
PASS Library_FolderRefusal_PassesNotAFileCode
PASS StoreCodes_SaveSection_UnadmittedHost_UsesSystemSentence
```

DOC-NOT-A-FILE stays a store refusal instead of becoming a write failure; the dialog's existing generic code-bearing sentence handles it. DOC-UNSUPPORTED-PLATFORM now reaches the existing system-sentence arm. The tests assert that no section file was published.

### 2. Runtime one-handle control — Verified

`PrefStore_OneHeldHandle_OwnerBeforeDacl` runs the existing-folder path against Windows. It observes successful opens at the CreateFileW and NtCreateFile boundaries, owner acceptance after the real identity comparison, and the actual handle-based SetSecurityInfo call. It requires exactly `open,owner-accepted,dacl`, one handle value throughout, then reads the resulting protected owner-only DACL. `run-tests.sh` executes this Core harness test normally. The old text scan and its run.sh call are removed.

`runtime-mutants.py` temporarily applies, builds and runs each mutant, restores the exact original source bytes, then rebuilds and runs the real code:

```text
MUTANT second-relative-open
FAIL PrefStore_OneHeldHandle_OwnerBeforeDacl InvalidOperationException: Expected open,owner-accepted,dacl; actual open,owner-accepted,open,dacl
MUTANT path-dacl-comment-decoy
FAIL PrefStore_OneHeldHandle_OwnerBeforeDacl InvalidOperationException: Expected open,owner-accepted,dacl; actual open,owner-accepted
MUTANT owner-after-dacl
FAIL PrefStore_OneHeldHandle_OwnerBeforeDacl InvalidOperationException: Expected open,owner-accepted,dacl; actual open,dacl,owner-accepted
REAL CODE
PASS PrefStore_OneHeldHandle_OwnerBeforeDacl
```

The path-DACL mutant calls SetNamedSecurityInfoW and leaves `SetSecurityInfo(handle,` in a comment. The relative-open mutant calls OpenRelative, not CreateFileW. None of the mutants is committed. Reproduce with the PATH strip from run.sh, then `python docs/proof/st-store-codes/runtime-mutants.py`.

### 3. Cause-true copy — Verified

Win32 ERROR_DIRECTORY (267) can identify a parent component that is a file. It retains DOC-UNSUPPORTED-PERSISTENCE; the distinct STATUS_FILE_IS_A_DIRECTORY mapping remains DOC-NOT-A-FILE. The real filesystem test saves to `<file>.txt/x.cfdw.json` and checks the parent bytes remain unchanged:

```text
FAIL WindowsStore_ParentIsFile_UnsupportedPersistence InvalidOperationException: Expected DOC-UNSUPPORTED-PERSISTENCE; actual DOC-NOT-A-FILE
PASS WindowsStore_ParentIsFile_UnsupportedPersistence
```

Only the actual non-NTFS volume-check branch supplies the machine-readable `non-ntfs` cause. Reads carry it in ContractError.Reason and OpenFailure.UnsupportedReason; saves carry it in SaveResult.UnsupportedReason. StartView and Labels append the drive remedy only for that cause. The tests inject filesystem identity or ancestor reparse attributes into the real WindowsProjectStore; they do not infer a filesystem from a drive letter or an error code.

```text
FAIL StoreCodes_LocalLinkedCause_NoDriveRemedy Exception: local reparse refusal received non-NTFS remedy
PASS StoreCodes_LocalLinkedCause_NoDriveRemedy
PASS StoreCodes_NonNtfsRemedy_NamesDrive
PASS StoreCodes_NewCodes_ReachEverySurface
```

The full run exposed an older test that required the remedy for every persistence refusal. It was updated to assert both unknown cause (no drive remedy) and explicit non-NTFS cause (drive remedy):

```text
FAIL OpenRefusal_Sentences_AreTheLabelsConstants InvalidOperationException: COPY-641 and drive remedy are not the Windows sentence
```

### 4–8. Copy record, fail-closed preferences, fixture and documentation

4. COPY-644 restores the complete approved text and approval status from commit `99b81769`, then appends `— superseded by COPY-125 — wave 3 plan, ST item 6 (WF review N4)`. Documentation-only correction; no runtime red applies. `PASS Labels_MissingFile_OneActiveDefinition_Copy125` verifies the retired row is not a second active definition.

5. Any Exception from the preference ownership/descriptor probe now fails closed to session-only. The test injects SecurityException, ArgumentException and InvalidOperationException, checks every load family and a save, and asserts zero store creations and zero files:

```text
FAIL PrefStore_OwnerProbeThrows_SessionOnlyNoWrite AggregateException: One or more errors occurred. (Security error.)
PASS PrefStore_OwnerProbeThrows_SessionOnlyNoWrite
```

6. Compare_OldBuildStoredRun asserts the historical run is Intact before attempting comparison. A temporary test mutant marks the loaded run PayloadFailedCheck and proves the new assertion prevents vacuous continuation. The fixture bytes and SHA-256 remain unchanged; the mutant is restored:

```text
FAIL Compare_OldBuildStoredRun_ReferenceAndAxesRefusal InvalidOperationException: the genuine old-build run must be intact before comparison expected Intact; actual PayloadFailedCheck
PASS Compare_OldBuildStoredRun_ReferenceAndAxesRefusal
```

This strengthens the characterization only. Wave-plan item 8 / COPY-615 remains NOT DONE under Ruling 267 because this historical fixture has incompatible axes and cannot reach the dimensional-headline branch honestly.

7. application-contracts.md and architecture/application.md now list `store.save`, `store.read`, `store.clear`, and `store.read-origin` instead of document.save/document.reopen. Documentation-only correction; no runtime red applies.

8. The duplicate docs/plans/st-dispatch1.md and its proof graph link are removed. AuthoringSession.cs has no repair-cycle diff; its existing RecordPersistence region records the emitted action as Operation. Documentation-only correction; no runtime red applies.

Amendment (c) paths: WindowsNative.cs provides the required native runtime seams; CompareTests.cs strengthens the expressly requested historical assertion; ControllerShellOneDriveTests.cs reconciles the stale copy assertion found by the full run; DESIGN.md and the two named contract/architecture docs implement the requested record corrections. No other track's owned region was changed. No claim was refused.

### Full run and residual acceptance

The single required `bash tools/run-tests.sh` invocation returned:

```text
== CfdWorkbench.Core.Tests: 921 passed, 7 skipped, 0 failed, exit 0
== CfdWorkbench.Analysis.Tests: 311 passed, 1 skipped, 0 failed, exit 0
== CfdWorkbench.Desktop.Tests: 895 passed, 1 skipped, 3 failed, exit 2
== CfdWorkbench.Cli.Tests: 34 passed, 1 skipped, 0 failed, exit 0
TESTS-RED 283 s
```

Desktop reported the aggregate harness failure, `--section-editor --part=1/2 exited 1`, and the stale OpenRefusal assertion above. The first full unpartitioned section-editor rerun alone repeated only that assertion. Controller-shell also ran alone with exit 0. An unrecognized `--onedrive` argument inadvertently triggered an extra Desktop harness run; it repeated the same failures and is not counted as isolated evidence. Subsequent final isolated rerun results are recorded below. No crash or exit 124 was reported by the required script. The main session's join ring remains the authoritative green; this repair does not relabel the red script as green.

Final standalone checks, with CFD_TEST_ONLY unset, on the final Debug build:

```text
--section-editor: 187 PASS, 0 FAIL, exit 0
PASS OpenRefusal_Sentences_AreTheLabelsConstants
--section-editor --part=1/2: 94 PASS, 0 FAIL, exit 0
PASS OpenRefusal_Sentences_AreTheLabelsConstants
```

The complete editor log finished at 17:30:21 UTC; the partition log was created at 17:30:33 UTC, establishing that the failed partition's final rerun did execute alone. No final-run crash, timeout or exit 124 occurred. The final focused Core run reported five PASS and `RESULT failures=0`; Analysis reported both the old-build characterization and one-active-copy checks PASS, `RESULT failures=0`. The focused Desktop store-code suite reported five PASS. `python tools/check-docs.py` printed `Documentation checks passed.` and git diff --check returned 0.

Proof oracles: exact code equality and no publication for item 1; actual native call order, handle equality and real DACL for item 2; real parent-file bytes plus read/save projection under injected volume/reparse attributes for item 3; original approved register row plus one-active-definition check for item 4; no exception escape, no store creation and no writes for item 5; explicit intact precondition under a corrupt-run mutant for item 6; source contract names and docs gate for items 7–8. Confidence is Verified for those observations. macOS native behavior was not executed on this Windows ARM64 host. COPY-615 dimensional-headline coverage remains open; independent acceptance and the authoritative full green remain with the main session.

The existing-store/native-call seams reuse installed code and OS primitives; no dependency or general persistence abstraction was added. The obsolete structural scan and duplicate plan were deleted. No commented-out implementation or committed mutant remains. Planned one full script, targeted red/green and three native mutants; actual one full script, all three rejected native mutants, one rejected fixture-integrity mutant, targeted greens, and isolated failed-mode reruns. Rework: one explicit UTF-8 correction, test-prefix alignment with the existing store-subset control, and reconciliation of a stale drive-remedy assertion. The accidental extra Desktop dispatch is reported above rather than counted as isolated proof. No approval of the author's own hard veto is claimed.
