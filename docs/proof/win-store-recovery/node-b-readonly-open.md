---
id: proof-win-store-recovery-node-b
title: "Windows store recovery, node B: read-only Open of a fully local OneDrive file, red, green and mutants"
type: proof-pack
summary: "Node B of the Windows store recovery build: the store, Desktop and CLI checks were red, then green; thirteen mutants were seen red; one survivor is defence in depth. The two real-file records were not made (no path supplied): SKIP."
status: active
owner: "@pc-laptop-leader"
phase: windows-w-2-build
tags: [windows, persistence, read-only-open, onedrive, ruling-259, ruling-260, ruling-261, proof]
links:
  - { to: design-windows-store-recovery, rel: implements }
  - { to: rulings, rel: depends-on }
---

# Read-only Open from OneDrive (node B)

Reproduce: `dotnet build CFDWorkbench.slnx -c Release`, then
`CFD_TEST_ONLY=WindowsStore_Open_,WindowsStore_SaveWalks,WindowsNative_Layouts <Core exe> --harness`,
`CFD_TEST_ONLY=ControllerShell_OneDriveOpen,ControllerShell_CloudOnly,OpenOutcome_ClassifyCode_CloudOnly <Desktop exe> --harness --section-editor`
and `<Cli exe> --harness`. Real files: set `CFD_ONLINE_ONLY_FILE` (an online-only file) and `CFD_LOCAL_ONEDRIVE_FILE`
(a fully local OneDrive file); the checks read attributes only and never hydrate or modify them.

## Red (Core, before the Read path changed)

9 failures, for example `WindowsStore_Open_OneDriveFullyLocal_ReadOnlyOrigin_NothingWritten ... Expected OK; actual DOC-UNSUPPORTED-PERSISTENCE`.
Two checks guard behaviour that was already right (`JunctionUnderOneDrive_Refused`, `UnqualifiedOneDriveRoot_NoException`) and were
red only through their mutants. CLI: `FAIL Cli_ExitForCode_CloudOnly_Is3 Exception: DOC-CLOUD-ONLY exits 2`. The Desktop checks and
the DR-DS-6 classification check were written after the trivial mapping existed, so their red lines are the mutants N and O below.

## Green

Core full harness, Analysis and Cli full harnesses: `RESULT failures=0`. Desktop mode `--section-editor` with the six node B checks: all PASS.
Guard prefix `--harness --theme-evidence`: exit 0. A finding fixed on the way: the change-time comparison needs the file's change
time to have left the clock tick of the seed (the check waits 100 ms).

## Mutants (each seen red, then reverted)

| mutant | red line |
|---|---|
| A bit test dropped | `Ruling260LeafPredicate_Rows: row recall-on-data-access-alone: expected DOC-CLOUD-ONLY, got DOC-CONFLICT` |
| B "no tag only" leaf rule | `Ruling260LeafPredicate_Rows: row tag-9000601A-local: expected OK, got DOC-UNSUPPORTED-PERSISTENCE` |
| D ReOpenFile (error 87) | `DataHandleFromHeldHandle_NoRecall_LocalAndOneDrive NativeFailure: ... Win32=87: The parameter is incorrect.` |
| E post-handle recall as DOC-CLOUD-ONLY | `RecallBitAfterDataHandle_RefusedConflict: Expected DOC-CONFLICT; actual DOC-CLOUD-ONLY` |
| F post-read change check dropped | `RecalledOrChangedDuringRead_RefusedNoPartial: change time case: OK` |
| G save walk passes OpenRead | `SaveWalks_PlaceholderExceptionNotApplied: Expected False; actual True` |
| H root bound by string only | `UnqualifiedOneDriveRoot_NoException: Expected False; actual True` |
| I exception without the root tag | `UnqualifiedOneDriveRoot_NoException: Expected DOC-UNSUPPORTED-PERSISTENCE; actual OK` |
| J mask loosened | `ReparseTagsUnderOneDrive_OnlyCloudFamilyAdmitted: Expected DOC-UNSUPPORTED-PERSISTENCE; actual OK` |
| K leaf exception without the OneDrive origin | `Ruling260LeafPredicate_Rows: Expected refusal DOC-UNSUPPORTED-PERSISTENCE` |
| N origin not set at Open | `ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing: the project did not open read-only: origin Local` |
| O DOC-CLOUD-ONLY unmapped | `OpenOutcome_ClassifyCode_CloudOnly_And_StartViewSentence: DOC-CLOUD-ONLY is not CloudOnly: NotRecognised` |
| R CLI arm omitted | `Cli_ExitForCode_CloudOnly_Is3 Exception: DOC-CLOUD-ONLY exits 2` |

Survivor Q (the controller's own in-place guard removed): green, because the store refuses the same save with the same code
(`RequireLocalFolder`, unchanged). The window refuses first with the OneDrive dialog (checked), so the guard is defence in depth.

## Real files

`SKIP WindowsStore_Open_RealOnlineOnlyFile_RefusedLeftCloudOnly no-online-only-file`
`SKIP WindowsStore_Open_RealFullyLocalOneDriveFile_AdmittedTagRecorded no-local-onedrive-file`
`SKIP Cli_Inspect_OneDriveFullyLocal_Succeeds no-local-onedrive-file`
When supplied, the records print `REAL-ONLINE-ONLY ...` and `REAL-LOCAL-ONEDRIVE attributes=0x.. tag=0x..` with no path
(write `%OneDrive%\...` if a path is ever quoted). The measured tags of the design (root 0x9000701A, fully local 0x9000601A,
cloud-only 0x9000401A with 0x401400) are the reviewer's, not re-measured here.

## Not done, by seam

`store.read-origin` is not emitted (it would count as a document.save in `RecordPersistence`); the notice row is placed from
`MainWindow` by inserting a shell grid row (request req-01M5JDFP0C75JZJWQ3XXP028JN, fallbacks taken). The save picker's start
folder was not driven (the native picker); it is unchanged code.
