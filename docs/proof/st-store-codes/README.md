---
id: proof-st-store-codes
title: ST dispatch 1 store-code and plumbing proof
type: proof-pack
status: active
owner: "@st"
tags: [store, windows, testing, telemetry]
links:
  - {to: coordination-wave3-next-steps, rel: implements}
  - {to: plan-st-dispatch1, rel: tested-by}
review-by: 2026-11-26
summary: Observed red and green harness lines for ST items 1 to 5, with the full-suite result and remaining environment limitations.
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
