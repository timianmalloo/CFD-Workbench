---
id: proof-size-cap-64mb
title: "Document size cap at 32,000,000 bytes: Ruling 258's budget measured, 64 MB refused, 32 MB adopted (Ruling 263)"
type: proof-pack
status: active
owner: "@track-w2b-mx"
phase: implementation
tags: [wave-2, mx, ruling-258, ruling-263, document-size, doc-size, measurement]
links:
  - { to: adr-0011-analysis-run-storage, rel: refines }
  - { to: rulings, rel: depends-on }
  - { to: note-adr-0011-size-cap, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  Ruling 258 raises NativeProject.MaxBytes if a document at the cap opens within 3 s, saves within 3 s and peaks within 1.5 GB.
  Measured idle on a Windows ARM64 laptop (medians of three), 64,000,000 bytes opens in 4,097 ms and peaks at 1,812,115,456 bytes,
  over budget, so it is refused. 32,000,000 bytes opens in 2,208 ms, saves in 732 ms and peaks at 1,012,297,728 bytes, within budget,
  so Ruling 263 sets MaxBytes to 32,000,000. The folder keeps its first name.
---

# Size cap: 64 MB refused, 32 MB adopted

Ruling 258 (`docs/notes/rulings.md`) made the raise conditional on three budgets. Ruling 263 applied the measurements below: 64,000,000
bytes is over two budgets and is refused; **32,000,000 bytes is within all three and is the cap** (`NativeProject.MaxBytes`).
Nothing was tuned; the optimisation route (Ruling 263 (c)) is the operator's next step. The amendment note is
`docs/notes/adr-0011-size-cap.md`.

## The measurements

Machine class: Windows ARM64 laptop, Release build, NTFS. Each run is a fresh process. Idle: the investigation loops were paused and no
other test suite ran (`tasklist` filtered on `CfdWorkbench.*`: none; the only `dotnet.exe` processes were 8 idle build servers).

| Size | Figure | Run 1 | Run 2 | Run 3 | Median | Budget (Ruling 258) | Verdict |
|---|---|---|---|---|---|---|---|
| **32,000,000** | open (to views settled, ms) | 2,119 | 2,230 | 2,208 | **2,208** | 3,000 | within |
| **32,000,000** | save (controller path, store acknowledged, ms) | 732 | 778 | 674 | **732** | 3,000 | within |
| **32,000,000** | peak working set (bytes) | 1,012,297,728 | 1,109,200,896 | 1,011,113,984 | **1,012,297,728** | 1,500,000,000 | within |
| 64,000,000 (refused) | open (ms) | 4,277 | 4,097 | 4,020 | 4,097 | 3,000 | over |
| 64,000,000 (refused) | save (ms) | 1,338 | 1,278 | 1,298 | 1,298 | 3,000 | within |
| 64,000,000 (refused) | peak working set (bytes) | 1,812,115,456 | 1,812,336,640 | 1,811,357,696 | 1,812,115,456 | 1,500,000,000 | over (also over 1.5 GiB = 1,610,612,736) |

Reading: idle lowered the 64 MB open by about 8 % (a first run under load gave 4,470 ms) and left the peak unchanged, so load explains
neither overrun. Open scales about 1.9x and peak about 1.8x from 32 to 64 MB.

How it was measured (`Measure_DocumentAtCap_OpenSavePeak`, Desktop harness mode `--controller-shell`, runs only with
`CFD_MEASURE_CAP=1` and `CFD_MEASURE_CAP_DOC=<file>`, otherwise prints `SKIP ... measurement-only`): copy the document to a fresh temp
folder; start a stopwatch; `WorkbenchController.OpenAsync` until it returns `Opened` and `WhenSurfaceSettledAsync` completes; print
`open_ms`; restart; `WorkbenchController.SaveAsync` until it returns `OK`; print `save_ms`; print `Process.PeakWorkingSet64`. The save writes
the opened document unchanged: a document at the cap cannot take an edit (it would be refused as `DOC-SIZE`). The document is written once
by the Core check under `CFD_MEASURE_CAP_WRITE=<file>` (`CFD_MEASURE_CAP_BYTES` builds it below the cap, for the 32 MB figures taken while
the constant was 8,000,000 or 64,000,000) and shared by the three runs. Command (three times):
`CFD_MEASURE_CAP=1 CFD_MEASURE_CAP_DOC=<file> CFD_TEST_ONLY=Measure_DocumentAtCap CfdWorkbench.Desktop.Tests.exe --harness --controller-shell`.

## The size test at 32,000,000

`Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact` (Core, Windows store; on macOS it prints
`SKIP Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact macos-not-run-here`). Method: n full 2,048-strip runs at distinct
operating points, one last run with the largest strip count that fits, and a free-text pad that lands the canonical image on exactly
`NativeProject.MaxBytes`; the length is asserted before use. It checks: the image saves through the store and reads back byte for byte and
through `AuthoringSession.Reopen`; an image of `MaxBytes + 1` is refused with `DOC-SIZE` by `SaveRequest`, by `NativeProject.Read` and by the
store reader; the file on disk keeps its SHA-256 and size and no temporary file is left; one pad byte more is refused by the session at
record time. The check now also asserts the literal `32_000_000 == NativeProject.MaxBytes` once, so it is red on any other constant.

- Red with the constant at 8,000,000: `FAIL Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact InvalidOperationException: Expected 32000000; actual 8000000`.
- Green with 32,000,000: `STORE-CAP-DOC bytes=32000000 fullRuns=17 lastRunStrips=406 pad=5639`, `PASS Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact`.

## One constant everywhere

Every size guard reads `NativeProject.MaxBytes` (the `src/` grep for `MaxBytes`, `DOC-SIZE`, `8_000_000`, `8000000`, `64_000_000` finds no
second number):

- `src/CfdWorkbench.Core/AuthoringSession.cs`: the constructor envelope cap (line 522), the save preflight (2131), the constant (2494), `NativeProject.Read` (2551).
- `src/CfdWorkbench.Persistence/ProjectStore.cs`: `SaveRequest` (16), the macOS provenance check (122) and `ReadAll` (399, 406).
- `src/CfdWorkbench.Persistence/WindowsProjectStore.cs`: `ReadAll` (348, the read-back) and the provenance check (137).
- `src/CfdWorkbench.Desktop/Panes/StartView.axaml.cs` line 94: the sentence derives "32 MB" from the constant.
- Left alone by the ruling: `LayoutCodec` (64 kB), `PreferenceStore` (4 kB), `RecentList`, `SectionLibrary` and the 4 kB line cap.

Finding on the Windows store's read-back (Ruling 263 names it as hard-coded): on this branch (after the BS node A join) `ReadAll` at line 348 already
reads `NativeProject.MaxBytes`, so the new test below was not red for that reason first; it was red only because the constant was 8,000,000.
The mutant proves it now bites.

`Store_ReadBack_DocumentBetweenOldAndNewCap_Opens` (Core): a 9,000,000-byte file (between the old and new cap) opens through the store's read-back.

- Red with the constant at 8,000,000: `FAIL Store_ReadBack_DocumentBetweenOldAndNewCap_Opens ContractError: DOC-SIZE`.
- Green with 32,000,000: `PASS Store_ReadBack_DocumentBetweenOldAndNewCap_Opens`.
- Mutant (the hard-coded value back, `WindowsProjectStore.ReadAll` line 348 `<= 8_000_000`): `FAIL Store_ReadBack_DocumentBetweenOldAndNewCap_Opens ContractError: DOC-SIZE` and `FAIL Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact InvalidOperationException: Expected OK; actual DOC-SAVE-UNCERTAIN`. Reverted, `git diff` clean of it.

## The user-facing text

`StartView.Sentence`, `OpenFailure.TooLarge` for a project: "It is larger than CFD Workbench can open (32 MB). The file hasn't been changed."
The number is `NativeProject.MaxBytes / 1_000_000`; the `.foil` branch keeps "1 MiB". Desktop check
`Copy_OpenFailure_TooLargeProject_NamesTheDocumentCap_Ruling263` (`--shell-window`).

- Red with the old literal: `FAIL Copy_OpenFailure_TooLargeProject_NamesTheDocumentCap_Ruling263 InvalidOperationException: The project sentence does not name 32 MB: 'It is larger than CFD Workbench can open (8 MB). The file hasn't been changed.`
- Green: `PASS Copy_OpenFailure_TooLargeProject_NamesTheDocumentCap_Ruling263`.
- Mutant (a literal "8 MB" back in the expression): the same `FAIL` line as the red line. Reverted.

`DESIGN.md` COPY-129 and `docs/design/app-shell.md` carry the `<limit>` placeholder, not a number, so they needed no change.

## Capacity (the A3 size test)

`Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` runs at its own session cap of 700,000 bytes and does not read `MaxBytes`:
`bytesPerRun128Strips=109991 bytesPerRecord=859`. The capacity at 32 MB is computed, not measured: 32,000,000 / 109,991 is about 290
pinned 128-strip runs before geometry and records (Inferred; about 72 at 8 MB). A fill through that loop at the cap was not tried: its
cost is quadratic (Ruling 258's proof noted about 45 minutes at 64 MB).

## AF's encode cost at this cap

`CompareFields` encodes the whole document per compare. At a 31,826,151-byte document the median of three is 65.8 ms (runs 64.5, 69.1, 65.8),
under the 100 ms rule, so it is left as it is. See `docs/proof/a3d-compare/README.md`, "Re-measured at the 32,000,000-byte cap".

## Harness results (no full run; the ring runs at the join)

Core 877 PASS, Analysis 308 PASS, Cli 30 PASS, each `RESULT failures=0`; Desktop `--shell-window` 121 PASS, exit 0; the guard prefix
`--harness --theme-evidence` exit 0 (`SUITE --theme-matrix exit 0`).
