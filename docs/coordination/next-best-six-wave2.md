---
id: coordination-next-best-six-wave2
title: "Coordination plan - next-best-six wave 2: the operator's decisions of 2026-10-22"
type: plan
status: complete
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

## Planned vs actual: wave 2 (record by `opus-leader`, 2026-10-22; final at the WF join, 5de36f84)

Same conventions as wave 1: numbers are as reported in Returns and relayed by the main session unless marked
**Verified**; "not recorded" is never estimated.

### 1. Per track

| track | planned budget | actual spend | repair cycles | exit evidence | joined |
|---|---|---|---|---|---|
| C1 copy approval + preference ACL | 40 · 110k · 1 h | at the call cap (audit close and doctor not run) | 1 (review conditions T1, T2, S1 and the skipped audit close; 25 calls) | Part A: 67 rows approved (522-588), 589 and 590 already approved under Rulings 253 and 257 and left as they were; Part B: the directory assertion red then green, two mutants red; full runs red only on the native crash (and one timeout under triple load) | joined 68919b3f (ring green, 190 s, no crash) |
| DS store design | 70 · 220k · 2 h 30 min | ~43 calls, ~140k, 40 min; revision 1 ~57 calls against its 35 cap (**cap fired**: the designer passed the cap before committing; every condition folded; the mockup re-render skipped) | 1 (S1-S5, D1-D4, U1-U5, Ruling 260); S4 then went to the Owner as an amendment of Ruling 260 | design, mockup (11 states), red-first test plan (16 Windows Core, 9 Windows Desktop, 1 CLI, 4 macOS compiled SKIP) | joined docs-only (9775dd4b) |
| SF1 | 60 · 160k · 1 h 30 min; conditions cycle 45 · 120k · 1 h | ~55 calls, ~150k, 1 h 10 min; conditions ~38 of 45 | 1 (UX 1, 2; Test Architect 3-6) | Side outline: the blend kept on three measured fixtures (constant-chord untwisted 0.0000 px; F-PEAK analogue 0.0002 px; new F-CURVED with curved rails, twist to 8°, dihedral to 100 mm: 0.0338 px at mid-pitch, 0.0000 px at η 0.35), marker and proof name all three; Promote Checking peer guard with mutant; Plan and Side station chip peers red then green with mutants; the Edit-section peer reports DSL-DRAFT-OWNED instead of throwing (red then green, mutant); chip help text carries the station identity (red then green, mutant); Section.Origin and the Analysis-state refusal asserted (green on current code, no mutant run); "appears or goes" backed by `PlanCanvas_StationChips_PeerFollowsRemoval`, a rename stated unobserved. The coder's `--plan-canvas` and `--views` runs both reported 111 PASS; the main session checked on main's build (Verified): the modes select disjoint sets (104 and 109 PASS, no check name in common), so the equal counts were a coincidence of SF1's additions landing in both modes; closed | joined 67ae5143 (ring green, 193 s, no crash) |
| MX | 60 · 160k · 1 h 30 min (re-planned from 45) | build (9e9def64) stopped on Ruling 258's budget; idle re-measure 25 calls; continuation, review, two repair cycles (the last docs-only) | 2 (the cap; cycle 2 docs-only: ten current-tense 8 MB statements, the ADR-0011 note's links, the encode check gated behind `CFD_COMPARE_ENCODE_TIMING`) | idle at 64 MB: open 4,097 ms (budget 3,000), peak 1.81 GB (budget 1.5 GB), save within; at 32 MB: open 2,208 ms, save 732 ms, peak 1.01 GB, all within; **Ruling 263: MaxBytes 32,000,000**; Ruling 264 accepted spec amendment AM-1.7.10; at-cap test red first | joined after repair 2 (9da8d66a) |
| DF | 70 · 220k · 2 h 30 min | ~63 calls, ~185k, 1 h 10 min | 1 (revision 1, fee91412; joined docs-only without a second Adversary pass, leader decision) | three notes and two amendments (spec-1.7.8 deltas, spec-1.7.9 Match); compatible fit sized at about 440 calls and 1.23M tokens (about 690 and 2.2M at S1's overrun ratio): **leader decision NEXT WAVE** (no CF-1 now) | joined docs-only (revision fee91412; Ruling 262 DF-Q1 to DF-Q3) |
| BS store build | node A 70 · 170k · 1 h 45 min; node B 90 · 230k · 2 h 15 min | node A ~85 calls (**cap fired**; three provenance mutants unfinished); node B ~95 calls against a cap of 90 (**cap fired**), ~330k tokens (coder estimate, not metered), ~1 h 40 min; node B repair 1: 38 calls, ~100k, 25 min | node A 1 (the unfinished mutants); node B 1 (36628c66) | node A: 12 Core `WindowsStore_Clear_*` and 8 Desktop checks green, 10 of 14 mutants killed, 4 macOS SKIPs; COPY-444 to 455 used, COPY-447 built, COPY-591 (DS-COPY-5); seam (FolderRefusalDialogTests, three assertions) confirmed. Node B: read-only Open from OneDrive under Rulings 259 to 261; the post-data-handle DOC-CONFLICT limited to `OneDriveReadOnly` (Verified by reading the condition); the DS-COPY-2 notice row held through the strip's row change handler (red then green, mutant red); the main session's real-file proof (44df937d, Verified): an online-only file refused DOC-CLOUD-ONLY with nothing downloaded, a fully local one opened read-only | node A joined; node B joined after repair 1 (diff 5eb0b188..44df937d) |
| AF A3 follow-ups | 110 · 300k · 3 h | ~75 calls, ~250k, 1 h 15 min; repair 1 ~33 calls, ~95k, 25 min | 1 (the `--self-launch` repo-root guard red at the join, a real red; review N1, N2, N4, N5 folded) | dimensional δL and δD_i display-only (Ruling 262 DF-Q1), axes keeps the full block; L = CL·q·S_ref held on two real VLM runs; `CompareEvent.Dimensional` after verifying events never reach the image; encode cost 14.8 / 16.0 ms near 8 MB (64 MB re-measure is a condition on the second of AF and MX to join); Cli SKIP lines, a real sleeping-child timeout check; text scan dropped, one-definition check the guard of record; COPY-611 to 617 | f955bbca (ring green, 200 s) |
| SF2 S1 geometry follow-ups | 120 · 320k · 3 h (SF2-1 45 · 120k · 1 h; SF2-2 75 · 200k · 2 h) | SF2-1 ~62 calls, ~110k, 55 min (**cap fired**); SF2-2 ~80 calls, ~320k, 1 h 40 min (**cap fired**) | 2 (the cap): combined repair 1 in two dispatches plus a re-read (R-2 about 62 of 60 calls, **cap fired**); repair 2 (b5e2239c) | SF2-1: measure-only path, bit-identical over 87 pairs and 412,873 doubles (coverage gap B1 closed in repair), 8121 → 3813 ms per 258 × 2001 call, 9741 → 4682 ms per promotion plan (did not reproduce under load); SF2-2: 16 `MatchBlend_` checks red then green, eight mutants red, residuals bit-equal to Promote's, COPY-621 to 632; repair 2: the `fly:match`-only mutant red, the Remove result shown first with Match offered after a background preview, F1 (a) (b) and F3 red then green with mutants | joined b31668b6 (ring green, 261 s, no crash; local merge 2d563f9c, chore aaf450b8) |
| WF copy follow-ups | 80 · 220k · 2 h | build ~70 calls, ~200k, 2 h including the usage-limit interruption; review 43 calls, ~130k; repair 1: 24 calls, 75k, 20 min; repair 2: 14 calls, 25k, 8 min | 2 (cycle 1 a4c38b9b: B1 the stale `Copy_OpenFailures_MatchesDesignRows` in `--shell-window`, a mode the coder did not run; N6, N8, N9 folded; the Ruling 265 and 266 rows. Cycle 2 3d71e646: the leader's N8 fold made COPY-643 contradict itself, so its clause is restored, red first. Review verdict: ACCEPT WITH CONDITIONS, one blocker) | d924c3db code, 8a98a46c rows and proof: the DOC-UNSUPPORTED-PERSISTENCE cause map by store site (one code, so one sentence, COPY-641); COPY-642 off Windows without "damaged"; COPY-643 for DOC-CONFLICT on Open with Try again (a new `OpenOutcome` Changed kind); COPY-644 missing file via `File.Exists` in `ClassifyCode`; COPY-645 File Explorer on Windows (macOS keeps COPY-126); item 6 measured: the section library saves on Windows, so COPY-646 there; export default for a read-only OneDrive document moved to the local default; COPY-615 left with its reason. Every item red then green, M1-M7 red; `--section-editor` 187 PASS; Core 909, Analysis 307, Cli 32. **Ruling 265:** COPY-590 retired, COPY-641 approved | joined 5de36f84 (ring green, 258 s, no crash, no 124; ten gates green) |
| CR2 native crash fix | 50 · 140k · 1 h 30 min (fix); repair 35 calls | fix: ~30 calls, ~90k, ~40 min (two wasted commands: a find over the root, a heredoc attempt); repair 1: 17 calls, ~60k, 15 min | 1 (C1 the probe through the shipped `MeshFrame.Draw`; C2 the array arm as positive control; C3 to C5) | root cause **Verified** by a two-arm spike: SkiaSharp 2.88.9's array helper `DrawVertices(mode, points, null, colors, paint)` leaves its `SKVertices` unreachable, and a GC during the native draw finalizes it (raster 0xC0000005; GPU 0xC0000374 found later by the heap timer). Fix in `MeshFrame.Draw`: `using` plus `GC.KeepAlive` (e22616bc). Evidence: the paired loop on e22616bc, 8 GPU pairs and 8 software pairs, **0 fatal exits, 0 dumps**, against a first-pair crash on each path before the fix (loops D, G). Probe: held through the shipped code, 547 draws under 535,571 collections in 60 s; the restored-helper mutant at the shipped site died 0xC0000005; `SurfaceRenderer.cs` unchanged from e22616bc to 09f64991 (**Verified**, `git diff --quiet` exit 0) | joined 28e23de0 (ring green, 264 s, no crash, no 124; ten gates green) |
| R1 harness software rendering | 70 calls | Return 6035438a; review held | n/a | the software path had its own fault under load (two 0xC0000005 on pair 1), which led to the root cause | **dropped** (leader decision): its purpose was a crash mitigation the cause no longer needs |
| CR1 crash capture (main session's investigation instrumentation, not a plan track) | 40 calls | done (1f94165d): the child-wrapper hook red first with a mutant; ProcDump's UTF-16 output fixed in SpawnWith (red first); a forced heap-corruption probe captured a dump whose clrstack names the faulting frames; all 18 Desktop modes green with the wrapper on | | | R0 joined after repair 1 (B1-B3); the capture wrapper stays opt-in |

### 2. Rulings

| ruling | question |
|---|---|
| 258 | R-MAX: MaxBytes 64,000,000 with a measured open, save and memory budget |
| 259 | R-OD: read-only Open of a fully local OneDrive file; Clear unfinished save on Windows, macOS compiled |
| 260 | DR-DS-1 (a): on Open only, a OneDrive file with no reparse tag, or the cloud-family tag under the 0xFFFF0FFF mask, is admitted when RECALL_ON_DATA_ACCESS, RECALL_ON_OPEN and OFFLINE are all clear, checked on the data handle before and after the read; any set bit refuses DOC-CLOUD-ONLY before any data handle; Security's veto holds until its DS review clears it |
| 261 | S4: amends Ruling 260 (option b): DOC-CLOUD-ONLY only before any data handle exists; a recall or offline bit, or any change, seen after it refuses DOC-CONFLICT |
| 262 | DF-Q1 (a) dimensional δL and δD_i display-only; DF-Q2 (a); DF-Q3 (a) the SF2 speed-up only through a path the station measure uses, bit for bit elsewhere. DF-Q1 is the Owner's reading of the operator's instruction: it goes to the operator for a yes or a no |
| 263 | MaxBytes 32,000,000 (amends 258's 64 MB, which failed its own open and memory budget when measured idle) |
| 264 | spec amendment AM-1.7.10: the spec's two document-limit lines read 32 MB |
| 265 | COPY-590 retired (false for OneDrive since Rulings 259, 260); COPY-641 approved as the first line of the Windows Open failure for DOC-UNSUPPORTED-PERSISTENCE, Ruling 257's rules kept; COPY-642 to 646 stay proposed for the operator's copy review; a wording repair from the WF review may come as a new request (the Owner flags a non-NTFS fixed disk, a case-sensitive folder and an unsupported Windows system as thin) |
| 266 | amends 265 (option a): COPY-641 keeps its id and takes the WF reviewer's sentence byte for byte (true for the case-sensitive, non-NTFS and .cfd- causes); a check keeps "damaged" and "Windows can't use" out of it; the system-not-admitted and directory causes go to a store follow-up with separate codes (DOC-UNSUPPORTED-PLATFORM, DOC-NOT-A-FILE suggested), outside this wave |

### 3. The native crash (0xC0000374): count and join policy

| measure | value | source |
|---|---|---|
| crashes today (both waves) | **12 in about 50 full runs** (SF1's runs added three, C1's rerun one, CR1's own unwrapped run one, in `--analysis`) | the main session's tally from Returns and its loop |
| modes hit | `--properties-view`, `--shell-window`, `--plan-canvas` (part 2/2); `--views` (part 1/2, twice) | Returns; kept logs |
| under added load | 1 in 8 full runs in the main session's loop; 2 of 2 in C1's runs during that loop; SF1's first full run under three concurrent suites crashed twice (`--shell-window` part 1/2, `--properties-view` part 2/2), its second once (`--views` part 1/2, green twice alone): **with three suites running at once, a full run crashes more often than not** | main session; SF1 Return |
| where it stops (the two kept logs, Verified) | before the crashed part's first check (start-up) | wave-1 record |
| dump capture | none from WER LocalDumps (HKCU per-exe, full dumps), none from the runtime's createdump; no Application Error or .NET Runtime event: **Verified** that the fault leaves no dump through WER or the runtime | main session |
| capture control | CR1: ProcDump as a child wrapper (`CFD_TEST_CHILD_WRAPPER`, unset in CI) with `-e 1 -f C0000374`, and a forced-corruption probe proving the capture end to end | main session |

**Join policy (the main session's call, recorded by the leader, 2026-10-22):** no join attempts until CR1's capture
lands and the crash is at least characterised. A join's ring has become a coin flip, and a lucky green proves nothing
about the change. Each held track joins after its review plus **one green ring run with the capture wrapper on**, so a
crash during that ring yields the dump the investigation needs instead of a rerun.

**Policy replaced (leader decision on the main session's question, 2026-10-22), option (b) with a rule:** crash loop C
ran 12 of 12 wrapped full runs green with 0 dumps, against 1 crash in 8 unwrapped runs, so the attached debugger very
likely hides the fault (Inferred, consistent with a start-up timing race). A green ring under the wrapper therefore proves
no more than the join's own ring, and the hold would wait on a capture that may never come. Held tracks now join on their
reviews plus the join's own full ring, under this rule:
1. A Desktop child that exits with a negative code (the harness names it a native crash) is not a verdict. Keep its
   Desktop log, then rerun **that mode, all its parts, alone**. Green: the join proceeds, and the instance is recorded
   (mode, part, the last line printed, load at the time).
2. If the rerun crashes too, rerun once more. **Two consecutive crashes in the same mode, or any crash after the crashed
   part had printed a check, stop the join**: that no longer matches the known start-up pattern and may be the change.
3. Any non-crash red is a real red.
3a. **Timeouts (exit 124), added 2026-10-22 after loop E:** under two or more concurrent harness parents, children hit
   the 120 s timeout in several modes. At a join, a 124 is handled like a crash: rerun that mode, all parts, alone with
   nothing else running; green, the join proceeds and the instance is recorded. **A 124 on that quiet rerun is a real red**
   (a hang the change may have introduced). Joins run with the investigation's loops paused, so the ring is not loaded by
   them.
4. CR1's wrapper stays an opt-in capture; loop D (two wrapped runners at once) and, if it stays clean, CR2 (an in-process
   vectored exception handler writing a minidump on 0xC0000374) continue in parallel as the investigation.

**The dump (main session, Verified from a minidump read with its own readers and ntdll symbols):** exception 0xC0000374
on a native thread with no managed frames, raised by the segment heap's garbage-collection timer
(`RtlpHpGCTimerCallback` → … → `RtlReportFatalFailure`), heap failure type 17 (VS subsegment corruption). The bytes before
the corrupted block header are 12-byte (x, y, ARGB) vertex records in pixel range, running over the block's tail guard:
**a heap buffer overrun by a writer of a per-vertex-colour vertex stream**, detected later by a timer on a thread-pool
thread, which explains why it is intermittent, hits any mode, and names no culprit. Inferred: the writer is in the
rendering stack (Skia Ganesh or ANGLE), not our managed code; no P/Invoke of ours writes such a stream.

**Correction recorded:** the earlier inference that the attached debugger hid the fault is withdrawn by the main session:
loop C's 12 clean wrapped runs fit the one-in-eight base rate (p about 0.2), and the dumped process ran with the segment
heap's tail guards under the debugger. Load is the lever. (The leader had carried the same inference; it is withdrawn here
too. The join rule above does not depend on it: it rests on reruns, not on the wrapper.)

**Next (main session):** loop E, two concurrent parents with ANGLE's natives removed so Avalonia falls back to software
rendering (the fallback verified from the loaded-module list before a clean run counts). Clean over the count that crashed
loop D on run 1: the GPU path is necessary. A crash: the dump points at Skia's raster path or our code. If loop E does not
settle it, page heap with allocation stacks (an HKLM IFEO entry, one elevated step) goes to the operator.

**Register findings from loop E (main session):** (1) Git for Windows puts its own `av_libGLESv2.dll` (an ANGLE build) on
the PATH of every Git bash session through its clangarm64 bin folder; with the app-local ANGLE renamed, the loader found
Git's copy, so two dumps from an overlapping attempt are GPU-path crashes with the same signature, not software-rendering
evidence. Any experiment that removes the app-local ANGLE must also strip that PATH entry, and must verify the fallback
from the loaded-module list. (2) Under two or more harness parents, children time out (124) in several modes: rule 3a.

Leader's reading of the earlier hold: the policy traded speed (the third objective) for evidence (the first) and loses nothing else: every
track's own checks are already observed red then green in its Return, and the hold only stops a ring result that could
not distinguish a crash from a regression. While joins are held, tracks keep their worktrees and branches; any track
that depends on a held join (MX on C1) waits with it.

**Outcome (2026-10-22):** root cause **Verified** and fixed in CR2 (joined 28e23de0). The chain:
1. The GPU dump showed a vertex stream beside the corrupted block.
2. The software path's 0xC0000005 exits named `sk_canvas_draw_vertices` ← `MeshFrame.Draw`.
3. The coordinate spike refuted bad coordinates.
4. Skipping `DrawVertices` gave 0 fatal exits in 8 software pairs.
5. The two-arm GC-pressure spike crashed the array helper within 60 s and kept the held form alive.
6. The paired loop on the fix: 16 pairs, 0 fatal exits, 0 dumps on both paths.

**Count, before and after the fix (main session):**

| path | before the fix | after the fix (e22616bc) |
|---|---|---|
| GPU | 4 dumps in about 9 paired runs (loop D, the overlapped attempt, the R1 GPU half) | 8 pairs: 0 fatal exits, 0 dumps |
| software | 2 fatal exits on the first paired run with the R1 harness, plus first-chance access violations (which the old wrapper hid) in all 7 loop E pairs | 8 pairs: 0 fatal exits, 0 dumps |

Withdrawn on the way: "the debugger hides the crash", "0 dumps in 7 software pairs means the GPU path is necessary" (a
dump filter for one code cannot show the absence of others), and the window-close lifetime race (strokes into the same
canvases never faulted). Lesson class NATIVE-TEMPORARY-FINALIZED-MID-CALL. R1 dropped. The crash-and-124 join rule stays
for the rings: 124s under load remain, and the rule is cheap.

### 4. Follow-ups recorded from reviews

- C1 (advisory, Security): the owner check and the ACL change are two opens of the folder, a small fail-safe race; an
  earlier elevated run may have left Administrators as the folder's owner, keeping that area session-only (Inferred);
  loading preferences does not check folder ownership (pre-existing). Operator follow-ups.

- **Flaky check (board):** `StatusStrip_BackgroundCompletion_DoesNotReplaceNewerReport` failed once under three
  concurrent suites in SF1's first full run; 3 of 3 green alone; the SF1 reviewer reads it as a load artefact unrelated to
  SF1 (bare controller), a likely race between its setup and `SupersedeStatus` (Inferred). The `--controller-shell` exit 1
  in that run was only this failure. Owner of the fix: none yet (operator board).
- **SF1 advisories (board):** Side station chip peers could pass `EntryOrigin.Side`; `ChipScreenRect` duplicates
  `ScreenRect`; `RefreshChipPeer` recomputes `Chips()` on every update with its cost unmeasured; no NVDA (screen reader)
  trace of the new peers yet.
- **Owner agreements by mail:** S5 (a second handle opened from the held handle by an empty relative name meets "same
  held handle"): agreed, mail-01M5J5FMC4YNVXBWB4NG9N25R9.

- **Operator prompt logged late (a PACK-O gap):** the operator's wave-2 instruction was logged to the audit log
  (al-01M5J79V9SGNNMS8GY2555V04Q) only when the DF reviewer asked for the ruling to cite it, hours after it was given.
  Control: a substantive operator message is logged with `prompt-log.py add` when it arrives, before any brief cites it.

- **Encode cost at 64 MB (condition on the second of AF and MX to join):** AF measured `CompareFields`' whole-document
  encode at 14.8 and 16.0 ms (medians) near today's 8 MB cap; at 64 MB the coder infers 120-130 ms, over the 100 ms rule.
  Re-measure once both are on `main`; move the size to the last save image if over.

- **Real-file evidence for Rulings 259 and 260 (node B's C1, the main session's run, Verified):** a throwaway online-only
  document under the OneDrive root (attributes 0x501620, tag 0x9000401A, allocation 0, length 8,316) was refused
  DOC-CLOUD-ONLY with its attributes identical afterwards (nothing downloaded); a throwaway fully local document
  (attributes 0x80420, tag 0x9000601A, allocation 12,288, length 8,316) opened read-only with origin OneDriveReadOnly, the
  hydrated-user-file tag the DS reviewer measured and Ruling 260's predicate admits. The three real-file checks passed;
  no user file was touched; the throwaway files stay until the wave closes.

- **Shared one-line list (register finding):** `tools/run-tests.sh` keeps every harness variable it clears on one
  `unset` line, which CR1, MX, SF2 and R1 each edited; every merge conflicts there and only the union is correct (MX's
  join kept it: `CFD_TEST_ONLY CFD_NATIVE_CAPABILITY_PROBE CFD_DESKTOP_SLOTS_BASELINE CFD_TEST_CHILD_WRAPPER
  CFD_FORCE_HEAP_CORRUPTION CFD_COMPARE_ENCODE_TIMING`). Control for later briefs: one variable per `unset` line.
- **A live coder lease refused a join's merge commit (register finding):** the SF2 cycle-2 coder held a short lease on
  `tools/run-tests.sh` while MX's join committed its merge; the pre-commit hook refused it, and the conductor's
  `--continue` committed after the lease expired. The rule: a join waits for the lease, never overrides it. Note for the
  SF2 diff check: SF2's cycle-2 brief did not list `run-tests.sh`; the lease shows the coder touched it (to be read in its
  Return).

- **CR2 (non-blocking, from the diff-check):**
  - N1: the probe's pure verdict checks run only when the probe mode is called (before its SKIP gate). They are not on
    CI. Accepted: `Verdict` has no other caller.
  - N2: the control child prints a `DesktopChecks` FAIL line for its expected crash inside a passing probe run. This is
    opt-in only, and the full parent never reaches it. Follow-up: a quiet spawn for the control.
  - N3: a held-arm 124 reads "crashed (0x0000007C)". Wording follow-up.
  - N4: the Debug SKIP path and the unknown-arm path are written but not run (Inferred).
- **WF (review, non-blocking; not folded into the repair):**
  - Operator list (Ruling 266 residuals): the remedy stays ambiguous for a fixed non-NTFS drive ("a local folder" does not say which drive); the store follow-up below.
  - N1 residue (store): the system-not-admitted and directory causes stay false under any single sentence. They need
    separate store codes (for example DOC-UNSUPPORTED-PLATFORM and DOC-NOT-A-FILE).
  - N2 (store): the `CodeOf` comment names Win32 4390 as "reparse point encountered". `FormatMessage` says 4390 is "not a
    reparse point". 4395 is the encountered code, and it is unmapped.
  - N3: `File.Exists` in `ClassifyCode` runs after the `await`, likely on the UI thread (Inferred). A slow volume could
    stall it. Move the test into the `Task.Run`, or carry it on the refusal.
  - N4 (copy review): COPY-644 and approved COPY-125 both describe a missing file. One code-dependent message has two
    definitions.
  - N5: the Save-section sentence is chosen by the OS name, not by the store's admission. The kept sentence is likely
    false on macOS for a location refusal (not assessed under the Windows pivot).
  - N7: the "named constants for pure mappings" pattern hides a future real foreign-OS call from the host-guard scan.
    Fix: an explicit marker the scan recognises.
  - N10: COPY-615's argument holds for runs this build computes. There is an Inferred gap for stored runs from an older
    build.
- **SF2:** the Match receipt is a literal in `ApplyMatchAsync`, not a `Labels` constant (a second definition waiting to
  drift). The cheap "already matches" test was not built.
- **BS node B:** a real sharing violation (Win32 32) maps to DOC-CONFLICT for any origin. That is Ruling 145's existing
  mapping, not a regression. One store `Operation` value per action, for the Clear and read-origin telemetry.
- **Wave report to the operator:**
  - the dimensional deltas (Ruling 262 DF-Q1): yes or no;
  - the copy review of COPY-642 to 646 (proposed);
  - the follow-ups above.
- **A ruling left uncommitted in the primary blocked a join (register finding):** the first CR2 merge did not start,
  because the Owner's Ruling 265 and the logged operator prompt sat uncommitted on files the merge updates. Control: a
  ruling or prompt written into the primary is committed before any join starts.
- **The leader's WF copy rule let proposed copy replace approved copy (leader error):** the WF brief said "add a new
  proposed row and map to it". WF did that, so the shipped code moved from approved COPY-590 to proposed COPY-641 with
  no retirement ruling. Ruling 265 then settled it. Control for later briefs: moving shipped code from an approved row to
  a proposed one is a supersession, so file an Owner request, or keep the approved text until it is ruled.
- **Three Return task files were empty:** the record for those tracks is the main session's relay. Control: a coder
  writes its Return as its final message.

## 5. Findings

- C1 ended at its call cap without its audit close; the main session carries C1's summary into the join's audit entry.
- The DS designer measured nothing it could not (the hydrated-file case); the main session measured it (no hydrated
  user file exists to read without downloading the operator's data), and DR-DS-1 went to the Owner with the numbers.

- **New failure shapes under load (not crashes; not yet read):** SF1's first full run also showed a `--plan-canvas`
  120 s timeout, a `StatusStrip_BackgroundCompletion_DoesNotReplaceNew` failure and a `--controller-shell` exit 1;
  the SF1 reviewer reads the kept log. Until read, they are load-dependent reds with no cause, like the crash.
- **Concurrency is a measured driver.** Three full suites at once on this laptop (C1, SF1, CR1 or the crash loop)
  made most full runs red. Leader's control for the remaining briefs: a coder runs only its own harness modes and
  the Core/Analysis/Cli projects; the full ring runs once per track, at the join, by the main session, with the
  capture wrapper on and no other full suite running.
