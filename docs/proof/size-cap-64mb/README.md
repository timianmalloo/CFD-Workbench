---
id: proof-size-cap-64mb
title: "Document size cap at 64,000,000 bytes: budget measured, exceeded, MaxBytes stays at 8,000,000"
type: proof-pack
status: active
owner: "@track-w2b-mx"
phase: implementation
tags: [wave-2, mx, ruling-258, document-size, doc-size, measurement]
links:
  - { to: adr-0011-analysis-run-storage, rel: refines }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-05"
summary: >-
  Ruling 258 raises NativeProject.MaxBytes to 64,000,000 if a document at the cap opens within 3 s, saves within 3 s and
  peaks within 1.5 GB. Measured on a Windows ARM64 laptop, the open (4,470 ms) and the peak (1,811,509,248 bytes) exceed
  their budgets, so the constant stays at 8,000,000 and the track stopped. The save (1,377 ms) was within budget.
---

# Size cap 64 MB: the measurement stops the raise

Ruling 258 (`docs/notes/rulings.md`) makes the raise conditional: the track stops and reports if any of the three numbers
exceeds its budget, and `MaxBytes` then stays at 8,000,000. Two of three exceeded. Nothing was tuned; tuning is out of scope.

## Result

| Figure | Budget | Run 1 | Run 2 | Run 3 | Median of three | Verdict |
|---|---|---|---|---|---|---|
| open (to views settled) | 3,000 ms | 4,360 | 4,527 | 4,470 | **4,470 ms** | over |
| save (controller path, store acknowledged) | 3,000 ms | 1,377 | 1,322 | 2,307 | **1,377 ms** | within |
| peak working set | 1,500,000,000 bytes | 1,811,984,384 | 1,810,939,904 | 1,811,509,248 | **1,811,509,248 bytes** | over (also over 1.5 GiB = 1,610,612,736) |

Machine class: Windows ARM64 laptop, Release build, NTFS. Each run is a fresh process. Other coders ran suites on the same
machine at the same time, so the times are noisy (the save spread shows it); the peak is stable to 0.1 % across the runs and
does not depend on load.

The document: exactly 64,000,000 bytes (34 runs of 2,048 strips plus one run of 827 strips and a 12,432-byte free-text pad).

How it was measured (`Measure_DocumentAtCap_OpenSavePeak`, Desktop harness mode `--controller-shell`, runs only with
`CFD_MEASURE_CAP=1` and `CFD_MEASURE_CAP_DOC=<file>`, otherwise prints `SKIP ... measurement-only`): copy the document to a
fresh temp folder; start a stopwatch; `WorkbenchController.OpenAsync` until it returns `Opened` and `WhenSurfaceSettledAsync`
completes; print `open_ms`; restart; `WorkbenchController.SaveAsync` until it returns `OK`; print `save_ms`; print
`Process.PeakWorkingSet64`. The save writes the opened document unchanged: a document at the cap cannot take an edit (the
edit would be refused as `DOC-SIZE`), so the brief's "one edit then save" is not possible at exactly the cap. The document is
written once by the Core check under `CFD_MEASURE_CAP_WRITE=<file>` and shared by the three runs, rather than rebuilt inside each
child (a build takes about 100 s).

Command (three times): `CFD_MEASURE_CAP=1 CFD_MEASURE_CAP_DOC=<file> CFD_TEST_ONLY=Measure_DocumentAtCap CfdWorkbench.Desktop.Tests.exe --harness --controller-shell`

## The size test

`Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact` (Core, Windows store; on macOS it prints
`SKIP ... macos-not-run-here`). Method: n full 2,048-strip runs at distinct operating points, one last run with the largest
strip count that fits, and a free-text pad (the unavailable reason of the first strips, 2,000 characters at most per line) that
lands the canonical image on exactly `NativeProject.MaxBytes`; the length is asserted before use. It then checks: the image
saves through the store and reads back byte for byte and through `AuthoringSession.Reopen`; an image of `MaxBytes + 1` is refused
with `DOC-SIZE` by `SaveRequest`, by `NativeProject.Read` and by the store reader; the file on disk keeps its SHA-256 and size
and no temporary file is left; one pad byte more is refused by the session at record time.

- Red on today's value, with the ruled number as a literal: `FAIL Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact ContractError: DOC-SIZE` (the 64,000,000-byte document refused at `MaxBytes = 8,000,000`).
- Green with `MaxBytes = 64,000,000`: `STORE-CAP-DOC bytes=64000000 fullRuns=34 lastRunStrips=827 pad=12432`, `PASS`.
- Shipped state (constant 8,000,000, test reads the constant): `STORE-CAP-DOC bytes=8000000 fullRuns=4 lastRunStrips=592 pad=5591`, `PASS`.

Mutants (run against the 64,000,000 constant, reverted after, `git diff` clean of each):

- (a) `WindowsProjectStore.ReadAll` reads a hard-coded 8,000,000: `FAIL ... Expected OK; actual DOC-SAVE-UNCERTAIN` (the save's read-back is refused).
- (b) the upper bound removed from `WindowsProjectStore.ReadAll`: `FAIL ... Expected refusal DOC-SIZE`.

## The guards (all read `NativeProject.MaxBytes`; none has its own number)

- `src/CfdWorkbench.Core/AuthoringSession.cs`: the constructor envelope cap (about line 517), the save preflight (about 2126), the constant (about 2489), `NativeProject.Read` (about 2546).
- `src/CfdWorkbench.Persistence/ProjectStore.cs`: `SaveRequest` (about 15), the macOS `ReadAll` (about 305 and 312).
- `src/CfdWorkbench.Persistence/WindowsProjectStore.cs`: `ReadAll` (about 250).
- Left alone by the ruling: `LayoutCodec`, `PreferenceStore`, `RecentList`, `SectionLibrary` and the 4 kB line cap.

## Capacity (the A3 size test)

`Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` runs at its own session cap of 700,000 bytes and does not read
`MaxBytes`, so it is unchanged and passes: `bytesPerRun128Strips=109991 bytesPerRecord=859`. A fill to 64 MB through that
loop was tried and not completed: its edits stop producing new run keys after about 58 steps (`DOC-RUN-KEY`), and its cost is
quadratic (58 steps took 36 s at 7 MB, which projects to roughly 45 minutes at 64 MB). The capacity at 64 MB is therefore
computed, not measured: 64,000,000 / 109,991 is about 581 pinned 128-strip runs before geometry and records (Inferred;
Ruling 258 states about 580).

## Not done because the raise did not happen

The ADR-0011 amendment note, the two lines in `docs/design/area3-analysis.md` (FM-11 and the denial-of-service row) and the
re-based comments are not written or changed: they restate a number that was not adopted. They are ready to write from this
page if the budget is raised or the open path and memory are improved. One user-facing text also names the old number:
`src/CfdWorkbench.Desktop/Panes/StartView.axaml.cs` (about line 94, "8 MB"); it stays right while the constant is 8,000,000.

## Harness results at the shipped state

Core 862 PASS, Analysis 296 PASS, Cli 26 PASS, each `RESULT failures=0`; Desktop `--controller-shell` 147 PASS on the rerun.
The first Desktop run had one failure, `GestureLimit_KeyboardNudges_HoldWithoutBankingAndAnnounceOnce` (a timing count under
concurrent load, unrelated to size); the rerun of the same mode alone passed.

## Idle re-measure

The first measurement ran under load. This one ran with the investigation loops paused and no other test suite running.
Idle check, before and after the runs (`tasklist` filtered on `CfdWorkbench.*`): "No tasks are running which match the
specified criteria." The only `dotnet.exe` processes were 8 idle build servers (no test host).

Method: the constant `NativeProject.MaxBytes` was set to 64,000,000 in the working tree only (never committed; reverted
afterwards, `git diff` clean on `AuthoringSession.cs`) and built once. The Core check wrote the document once per size
(`CFD_MEASURE_CAP_WRITE`; at 32,000,000 with `CFD_MEASURE_CAP_BYTES=32000000`, which builds the document at that size and skips
the at-cap steps). The Desktop check ran in a fresh process per run:
`CFD_MEASURE_CAP=1 CFD_MEASURE_CAP_DOC=<file> [CFD_MEASURE_CAP_BYTES=32000000] CFD_TEST_ONLY=Measure_DocumentAtCap CfdWorkbench.Desktop.Tests.exe --harness --controller-shell`.
`CFD_MEASURE_CAP_BYTES` is the only code change committed (test-only, default `NativeProject.MaxBytes`).

| Size | Figure | Run 1 | Run 2 | Run 3 | Median | Budget (Ruling 258) | Verdict |
|---|---|---|---|---|---|---|---|
| 64,000,000 | open (ms) | 4,277 | 4,097 | 4,020 | **4,097** | 3,000 | over |
| 64,000,000 | save (ms) | 1,338 | 1,278 | 1,298 | **1,298** | 3,000 | within |
| 64,000,000 | peak working set (bytes) | 1,812,115,456 | 1,812,336,640 | 1,811,357,696 | **1,812,115,456** | 1,500,000,000 | over (also over 1.5 GiB = 1,610,612,736) |
| 32,000,000 | open (ms) | 2,119 | 2,230 | 2,208 | **2,208** | 3,000 | within |
| 32,000,000 | save (ms) | 732 | 778 | 674 | **732** | 3,000 | within |
| 32,000,000 | peak working set (bytes) | 1,012,297,728 | 1,109,200,896 | 1,011,113,984 | **1,012,297,728** | 1,500,000,000 | within |

Reading: idle lowers the 64 MB open by about 8 % (4,470 to 4,097 ms) and leaves the peak unchanged, so the open and the peak
still exceed their budgets at 64 MB; load did not cause either. At 32 MB all three figures are within budget. Open scales
about 1.9x and peak about 1.8x from 32 to 64 MB. The committed constant stays 8,000,000; the choice is the Owner's.

After the revert, the at-cap test is green at 8,000,000 again: `STORE-CAP-DOC bytes=8000000 fullRuns=4 lastRunStrips=592 pad=5591`, `PASS Store_DocumentAtCap_OpensAndSaves_OneByteOverRefusedIntact`.
