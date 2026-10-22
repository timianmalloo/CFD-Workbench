---
id: coordination-next-best-six-wave2
title: "Coordination plan - next-best-six wave 2: the operator's decisions of 2026-10-22"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, windows]
links:
  - { to: coordination-next-best-six, rel: refines }
  - { to: design-windows-native-store, rel: implements }
  - { to: design-a3d-compare, rel: implements }
  - { to: design-add-station, rel: implements }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-22"
summary: >-
  Wave 2 of next-best-six, from the operator's decisions after the wave-1 close: Windows is the platform of record; the
  proposed copy rows are approved; Clear unfinished save, read-only Open from OneDrive and the preference-folder ACL are
  built; the document size cap is raised so pinned VLM runs are not the practical limit; and every follow-up the A3d
  Compare, Add station and W2-Q2 tracks left is closed. Two Owner rulings first, then nine tracks at width 3.
---

# Coordination plan: next-best-six, wave 2

**Leader's choice:** a new plan file, not an addendum. Wave 1's plan is `status: complete` with its planned-vs-actual
appended; wave 2 has its own goal, budgets and record. This file `refines` wave 1.

## Input: the operator's decision (verbatim, 2026-10-22)

"the intermittent desktop native crash [investigate]; skip the macos run ... we are pivoting to focus on windows; accept
4; do 3; increase the VLM run budget so it works; keep going and lets get a3d compare, add station and w2-q2 follow up
implemented and complete"

**Readings** (the main session's, checked by the leader; each marked):

| phrase | reading | status |
|---|---|---|
| investigate the crash | the main session runs `/investigate` (event log, WER dumps); the leader writes a repair brief once the cause is verified | not a track here until a cause exists. Status 2026-10-22 (main session): the five crashes left no Application Error or .NET Runtime event in the Application log; Crashpad's maintainers record that Windows does not run WerFault for STATUS_HEAP_CORRUPTION unless a vectored exception handler forwards it, so the likely capture control is a VEH in the harness child writing a MiniDumpWriteDump on 0xC0000374 (plus page heap where an elevated shell exists; this shell cannot write HKLM); a full-runner loop with WER LocalDumps and the runtime's createdump armed is measuring whether either catches it |
| skip the macos run; pivoting to windows | Ruling 249's follow-up (b) is dropped; **Windows is the platform of record**. macOS code stays compiled and CI keeps its macOS job; new macOS-only behaviour is compiled, not run here | Verified against Ruling 249 |
| accept 4 | the operator approves the proposed copy rows COPY-522 to COPY-590 (69 rows; 589 and 590 already carried Ruling 253 and 257 approvals) | confirmed by the main session against its numbered close list (item 4 = the copy-row review) |
| do 3 | build Clear unfinished save for both stores (Windows run; the macOS path compiled, its checks skipped by platform), read-only Open from OneDrive (Ruling 254 option b, now the operator's choice: design and ruling first), the preference folder's inherited permissions | confirmed by the main session (item 3) |
| increase the VLM run budget so it works | raise `NativeProject.MaxBytes` (`src/CfdWorkbench.Core/AuthoringSession.cs` line 2489, today 8,000,000) so pinned runs are not the practical cap; the value is an Owner (Data & Persistence) ruling | Verified: the constant and its two guards |
| keep going ... implemented and complete | close every follow-up the A3, S1 and W2-Q2 tracks recorded | see the track table |

**Not in scope:** the crash fix (until `/investigate` verifies a cause), macOS runs, Areas 1/4/5/6, CAD export, the 2D
section workbench, the remaining CAD rows not named below.

**Tier T2; fan-out cap 3; context ceiling 400,000 tokens per dispatch; one fresh context per node.**

## Budgets: the rules wave 1 taught

- A brief's fixed steps (start line, doctor, check-docs, index, audit, commit, Return) cost **about 10 calls and 35k
  tokens**; every budget is that plus the work.
- **About 4 calls per mutant** on top of the work.
- A fresh context re-reads its inputs: every brief **names the sections and functions to read**.
- Loop work: runs × 3 calls.
- Two repair cycles per node; a third needs an Owner ruling; a budget firing is a defect signal, never a raised cap.

## Serial spine

| item | why first | who |
|---|---|---|
| **R-MAX**: the `MaxBytes` value | MX builds it; the value trades document capacity against the DoS mitigation (area3-analysis.md FM-11, STRIDE row "huge arrays") | Owner, on the leader's request |
| **R-OD**: record the operator's choice of Ruling 254 (b) and the constraints the store design must meet | DS designs it; Ruling 254 currently says (a) | Owner, on the leader's request |
| **C1 before any track that appends copy rows** | C1 edits the status of COPY-522 to 588 in place; a later append after COPY-590 sits within git's context of those lines | leader |
| **DS (design) before BS (build); DF (design) before AF and SF2 (builds)** | GO5(b): the shape before the coders | leader |
| **BS before WF** | read-only Open from OneDrive changes what the open-refusal message must say; WF's two-row message depends on it | leader |
| **MX early** | `AuthoringSession.cs` is also touched by SF2 (Match the blend) and possibly AF | leader |

## Tracks

| track | goal | owns (authored) | depends on | tier | model | budget | exit evidence |
|---|---|---|---|---|---|---|---|
| **C1 copy approval + preference ACL** | approve the proposed copy rows; fix the preference-folder inherited ACL on Windows | `DESIGN.md` §7 status cells of COPY-522 to COPY-590; `src/CfdWorkbench.Persistence/PreferenceStore.cs` (`EnsureDir`), `WindowsNative.cs` (one private-directory create, if needed), `tests/CfdWorkbench.Core.Tests/PreferenceStoreTests.cs`; the W2 proof (one section) | the main session's confirmation of "accept 4" | T1 | Sonnet | 40 calls · 110k · 1 h | every row 522-590 carries the operator approval (69 rows; 589 and 590 keep their ruling citation beside it); `AssertOwnerOnly` on a directory asserts the private policy on Windows, red on today's code, green after; an existing inherited directory handled as the brief says, with a check; TESTS-GREEN; check-docs 0 |
| **DS store design** (Opus) | one store design: read-only Open from OneDrive, and Clear unfinished save on both stores | `docs/design/windows-store-recovery.md` (new), `docs/mockups/w2-recovery.html` and `.md`, at most one spec amendment note | R-OD | T2 | Opus | 70 calls · 220k · 2 h 30 min | the design (data and invariants first, E7 list, states, copy with the reserved ids, red-first test plan, owned paths), the mockup with every state, check-docs 0; then an Adversary pass: Security (the placeholder exception; reading a not-yet-downloaded file hydrates it over the network), Data & Persistence (Clear's no-live-writer rule and provenance), UX & Accessibility |
| **BS store build** | build DS | per DS §owned paths: `ProjectStore.cs` dispatch, `WindowsProjectStore.cs`, `UnfinishedSave.cs`, `CloudFolder.cs`, `MainWindow.axaml.cs`, `OpenOutcome.cs`, `Labels.cs`, the tests | DS joined, R-OD | T2 | Sonnet | 160 calls · 400k · 4 h, split by node as DS says | DS's named tests red then green; the macOS half compiles and its checks print `SKIP <name> macos-not-run-here` (never PASS); TESTS-GREEN; CI green on both runners |
| **MX document size cap** | raise `MaxBytes` to the ruled value | `AuthoringSession.cs` (the constant and its guards only), `docs/notes/discrepancy-records.md` or a new ADR-0011 decision note, `docs/design/area3-analysis.md` FM-11 and STRIDE rows (one line each), the size tests | R-MAX | T1 | Sonnet | 45 calls · 120k · 1 h 15 min | a size test at the new cap (refused just above, accepted just below, red on today's value); open and save time and peak memory **measured** at the cap on this laptop against the ruling's budget; the pinned-run capacity re-measured with the A3 size test; TESTS-GREEN |
| **SF1 S1 surface follow-ups** | the Side outline exactness, the Promote peer's Checking state, station-chip automation peers | `ElevationView.cs` (`SliceSection`, station chips), `PlanCanvas.cs` (station chip peers), the Desktop tests | none | T1 | Sonnet | 60 calls · 160k · 1 h 30 min | Side-outline pixel error measured on F-CAMBER and F-PEAK, switched to `Placement.Sections` if above 1 device px; the Promote peer's Checking state checked red first; Plan and Side station chips exposed to assistive technology (red first, a mutant each); TESTS-GREEN |
| **DF follow-up designs** (Opus) | three design notes: dimensional L and D_i deltas in Compare (Ruling 250 Q3 c, now the operator's choice; a spec amendment, since ANA-06 blocks any delta on a reference difference); "Match the blend" before Remove; the Ruling 73 compatible fit (mixed spacing, cfdw-cv/3): design and a size estimate only, with a fit-the-wave recommendation | `docs/design/a3d-compare.md` (one new section), `docs/design/add-station.md` (one new section), `docs/design/compatible-fit.md` (new), spec amendment notes | none | T2 | Opus | 70 calls · 220k · 2 h 30 min | each design with its data rule, E7 list, red-first tests and owned paths; the amendment notes; an Adversary pass (hydrodynamicist and Test Architect for the deltas; computational geometry for Match the blend and the compatible fit) |
| **AF A3 follow-ups** | dimensional deltas per DF; the compare-event encode cost timed (and taken from the last save image if it shows); design §6.1 event name; the Cli harness gaps (SKIP lines off Windows, a child timeout, stderr captured) | `Compare.cs`, `AnalysisProjection.cs`, `Labels.cs`, `ComparePane.axaml(.cs)`, `AuthoringSession.cs` (`CompareFields` only), `tests/CfdWorkbench.Cli.Tests/*` harness lines, `DESIGN.md` rows | DF joined, its amendment ruled; MX joined | T2 | Sonnet | 110 calls · 300k · 3 h | DF's tests red then green; the encode cost measured at the new cap; the Cli SKIP, timeout and stderr checks; TESTS-GREEN |
| **SF2 S1 geometry follow-ups** | the `Placement.Sections` speed-up with a bit-for-bit test (Ruling 256 Q2, now ordered by the operator); "Match the blend" per DF; the compatible fit only if DF recommends it fits | `Placement.cs` (the speed-up: skip work the station measure does not use), `AuthoringSession.cs` (`StationEdits`), `tests/CfdWorkbench.Core.Tests/StationTests.cs`, a new bit-for-bit test | DF joined; MX joined | T2 | Sonnet | 120 calls · 320k · 3 h, two nodes (speed-up, then Match the blend) | the speed-up's output **bit-identical** to today's on every Station fixture and the Placement test set (red first by a mutant that changes one bit), its time measured before and after; Match the blend per DF; TESTS-GREEN; the computational-geometry lens at Return |
| **WF W2-Q2 follow-ups** | the open-refusal message for every cause (two rows, after BS changes what OneDrive does), the off-Windows message, the access-denied "in Finder" text on Windows, the "Save section" dialog's "This system can't save to My sections safely" | `OpenOutcome.cs`, `Panes/StartView.axaml.cs`, `Labels.cs`, the dialog's file, `DESIGN.md` rows, tests | BS joined; C1 joined | T1 | Sonnet | 50 calls · 140k · 1 h 15 min | each message checked red on today's text, green after; new rows "proposed" for the operator; the UX & Accessibility lens at Return |

**Struck or held:** the crash repair (held for `/investigate`); macOS runs (dropped by the operator); the compatible
fit's build (held unless DF recommends it fits this wave).

## Seams

| from -> to | what | resolved by |
|---|---|---|
| C1 -> every copy-appending track (BS, AF, WF, SF1 if it adds accessible names) | `DESIGN.md` §7 | C1 joins first |
| MX -> AF, SF2 | `AuthoringSession.cs` (the constant vs `CompareFields`, `StationEdits`) | MX joins first |
| BS -> WF | `OpenOutcome.cs`, `Labels.cs`, `MainWindow.axaml.cs` | BS joins first |
| SF1 -> SF2 | none in code (SF1 is Desktop, SF2 Core); both read `Placement.Sections` | none |
| AF -> SF2 | `AuthoringSession.cs` (`CompareFields` vs `StationEdits`), different functions | AF joins first if both are ready |
| any track -> `ShellHost.cs`, `PropertiesPane.axaml.cs`, `ModelArea` | wave 1's lesson: shell wiring files no row names | each brief pre-grants the lines it needs, or the coder raises a seam request |

## Order of operations (width 3)

| # | action | slots |
|---|---|---|
| 1 | file R-MAX and R-OD (Owner); the main session confirms the "accept 4" numbering | — |
| 2 | dispatch **C1** (Sonnet), **DS** (Opus, after R-OD), **SF1** (Sonnet) | 3 |
| 3 | join C1; dispatch **MX** (after R-MAX) into its slot | 3 |
| 4 | DS Adversary pass and revision cycles; join SF1; dispatch **DF** (Opus) | 3 |
| 5 | join MX; join DS (docs); dispatch **BS** (Sonnet) | 3 |
| 6 | DF Adversary pass; join DF (docs); any DF amendment ruled; dispatch **AF** when a slot frees | 3 |
| 7 | join BS (CI read on both runners); dispatch **SF2** | 3 |
| 8 | join AF; dispatch **WF** | 3 |
| 9 | join SF2 and WF; planned-vs-actual appended here; audit entry; memory | — |

Each dispatch gets a fresh context; each join runs the full ring; a native crash in the ring is recorded with its line
and its Desktop log kept (copy `.tmp-tests/Desktop.log` to the scratchpad before any rerun), never rerun silently.

## Gate record

| gate | result | by |
|---|---|---|
| Simplifier | each track earns its multiplier: the follow-ups are grouped by file family (store, Desktop surface, Core geometry, Compare, messages), not one track per item | author (leader), Peer Mode; the main session re-reads |
| Test Architect | every row names observed exit evidence; every product change is red first; SKIP never reads PASS | to be cleared by the main session, not the author |
| Data & Persistence | R-MAX and DS; the DS Adversary pass holds the veto | at R-MAX and DS |

## Status

| | |
|---|---|
| **Completed** | the operator's decision read; tracks, spine, seams, order and budgets written |
| **Remaining** | R-MAX and R-OD filed (awaiting rulings); C1 dispatched; SF1 ready; DS ready on R-OD. This plan file lands on `main` in one docs-only node with the crash investigation report once the main session's crash loop in the primary checkout finishes (a join there now would race the loop) |
| **Best next action** | dispatch SF1; dispatch DS when R-OD lands; MX when R-MAX lands and C1 has joined |
