---
id: coordination-wave3-next-steps
title: "Coordination plan - wave 3: the next steps of 2026-10-23"
type: plan
status: complete
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, windows]
links:
  - { to: coordination-next-best-six-wave2, rel: refines }
  - { to: investigation-section-catalog-apply, rel: implements }
  - { to: investigation-join-dirty-primary, rel: implements }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-26"
summary: >-
  Wave 3 closes the next steps the last two sessions left: the section-catalog repair (phases 1-4, rulings DR-INV-1 a
  and DR-INV-2 a), join fence R2, and the wave-2 follow-ups grouped by file family. Five tracks at width 3; Fable
  Owner, Opus leader, Opus and Sonnet sub-agent coders, Codex and Grok CLI coders.
---

# Coordination plan: wave 3

**Input.** The operator's prompt of 2026-10-26 (audit al entry committed at 8e433b4f) and their answers (8b6d35a3):
DR-INV-1 = a ("Modified from NACA 0012" on every surface); DR-INV-2 = a (listed, muted, the reason in the detail line,
a state tag); B7 = the encode re-measure only, the compatible fit stays a design; B9 = `git branch -d` only, unmerged
branches listed. Sources: `docs/investigations/section-catalog-apply.md` (phased repair plan),
`docs/investigations/join-dirty-primary.md` sections 9-10, `docs/coordination/next-best-six-wave2.md` section 4.

**Not in scope.** The macOS verify-gate run (dropped under the Windows pivot, wave-2 input row 39); the compatible fit
build (DR-DF-8); the SF1 advisories and WF N7 (board items, not in the operator's list).

**Seats.** Owner = a Fable sub-agent, session `fable-owner`; it rules with
`AGENT_SESSION=fable-owner python docs/ai-forward-pack/scripts/coord-decide.py rule next --request <id>` into
`docs/notes/rulings.md` (next number 267) and never commits. Leader = an Opus sub-agent `opus-leader`: it writes every
brief, reviews every Return and decides every join. The main session (`wave3-coordinator`) runs the mechanics:
worktrees, dispatch, CLI runs, joins, commits, pushes. Reviewers are Opus sub-agents in Adversary Mode with the lenses
the leader names. Every track's decision request goes `--to fable-owner`.

**Leader ref.** Released at epoch 5 and **not pinned**: the Ruling 247 session recorded that pinning broke joins and
that joins read the released epoch and work (memory f1-attempt2-status, re-observed by wave 2). Deviation from the
skill's S3 row 1, recorded here.

## Layer state
| check | result | meaning |
|---|---|---|
| registry | ok - 7 pattern(s) | `.agents/artifacts.yml` present; managed block plus four repo entries |
| merge driver | effective - coord-regen, coord-register, defect-register registered | derived and register files merge without a human |
| leader | released (epoch 5) | not pinned (see Leader ref) |
| heartbeat | 1 session, newest beat 1206481 s ago; 0 live | nothing in flight |
| requests | 34, 0 open | no unanswered decision |
| lease overlap | none (0 live leases) | clean start |
| claude edit boundary | enforcing (spike S5) | sub-agent coders are fenced by the PreToolUse hook |
| commit floor | enforces regardless of the hook | the pre-commit lease check covers the CLI coders |
| doorbells | not recorded | no harness doorbell probed; dispatch is by the main session |
| python | `python3` fails here; use `python` | every command below is written `python` |

Measured 2026-10-26 15:33Z by `coord-core.py doctor` and `pack-doctor.py --json`. The layer is installed **once, in
the primary checkout**; every worktree this plan creates inherits it and runs `coord doctor` to read it back, never
`coord install`.

## Artifact classes
| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | coord-register union merge | no |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | coord-regen: `audit-log.py render` | no |
| `docs/docs-index.js` | derived | coord-regen: `docs-graph.py derive` | no |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | coord-register | no |
| `docs/lessons/defect-classes.md` | register | defect-register driver | no |
| `docs/notes/rulings.md` | authored, single writer | only `fable-owner` writes; the main session commits it on main before any join | serial (spine) |
| `DESIGN.md` copy rows | authored, shared | rows appended in a per-track COPY-id range (below); both sides kept at a merge | range rule |
| `src/CfdWorkbench.Analysis/Labels.cs` | authored, shared | constants appended in a per-track region comment; both sides kept | range rule |
| `src/CfdWorkbench.Desktop/WorkbenchController.cs` (4,232 lines) | authored, shared by region | SC1: the catalog-preview region (`PreviewFault` ~`:2089`, `FinishPreview`, the reset ~`:2407`) **except** `ChipSourceName` (`:2359`) and its two callers (`:2102`, `:2114`), which are SC2's; ST: the open region (~`:3370-3580`) and the save region (~`:3800-3845`); HX: `ApplyMatchAsync` only | region rule |
| `docs/design/m12d-catalog.md` | authored, shared by section | SC1 writes §11.4 only; SC2 writes §14.1 only | section rule |
| `tests/CfdWorkbench.Desktop.Tests/CatalogDialogTests.cs` | authored, shared by hunk | SC1 owns it; ST may change only the Save-section sentence assertion (~`:264`) | hunk rule; ST rebases at its join |
| `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs` | authored, shared by hunk | HX owns it; ST may change only the store-code assertion (~`:202`) | hunk rule; the later join rebases |
| `tools/run-tests.sh` | authored | one variable per `unset` line (wave-2 control); only HX may add one | single owner |
| everything else | authored | one owner per file (Tracks) | owner |

**COPY-id ranges** (high water today COPY-646): SC1 647-666 · SC2 667-676 · ST 677-706 · HX 707-716. A new row lands
"proposed" for the operator; moving shipped code from an approved row to a proposed one is a supersession and needs an
Owner request first (wave-2 control).

**Joint satisfiability of the shared surfaces.** The guards over `DESIGN.md` and `Labels.cs` are the existing
one-definition checks (`Copy_*_MatchesDesignRows` and the Labels-to-DESIGN row scans in the Desktop and Analysis
harnesses; root: the repo's `DESIGN.md` copy table and `Labels.cs`; no recursion; token set `COPY-\d+`; allowlist: none).
Each track adds rows and constants only inside its range, so every other track's scan sees a superset with its own rows
unchanged: satisfiable. ST merges COPY-644 into COPY-125 (a removal), which no other track cites. The four regions of
`WorkbenchController.cs` are disjoint hunks: SC1 (the catalog-preview path, ~`:2089`-`:2407` minus SC2's lines), SC2
(`ChipSourceName` `:2359` and its callers `:2102`, `:2114`), ST (open ~`:3370-3580`, save ~`:3800-3845`), HX
(`ApplyMatchAsync`).

## Tracks
| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **SC1** catalog sheet (A1, A2, A4) | `src/CfdWorkbench.Desktop/CatalogDialog.axaml(.cs)`, the catalog-preview region of `WorkbenchController.cs`, catalog telemetry, Desktop catalog tests, `docs/design` m12d §11.4 note | A0 joined | T1 | 1 (phases serial: 1, 2, then 4) | 3 dispatches · 80 calls · 300k · 3 h | the investigation's named tests seen **red first** then green: `CatalogDialog_NonChoosableRow_RenderedDistinct`, `CatalogDialog_PendingGenRow_TagNamesState`, `Controller_PreviewInternalFault_ReportsReason`, `Controller_PreviewDraftChanged_ReportsReason`, `CatalogDialog_PreviewFault_LeavesFitting`, `CatalogDialog_EnterDuringFit_AppliesOnLanding`, `Catalog_Select_RecordsOutcome`; `CatalogDialog_NonChoosableRow_RenderedDistinct` includes a damaged My-sections row (`CatalogChoice.Damaged`) case, observed red before the fix; `CatalogDialog_ArrowsReachDisabledRows_Quiet` stays green; `AutomationState_HasRenderedState` red with Phase 1's class removed, green with it; `ui-craft-gate.py` clean on the sheet; `catalog.preview` outcome unchanged (Phase 2 validation); the damaged My-sections rows (investigation class 1) rendered distinct by the same Phase 1 test; the manual pointer pass after Phase 1 recorded by the operator or marked Inferred with the residual named; `run-tests.sh` green | Claude **Opus** sub-agent (enforcing) |
| **SC2** source text (A3) | `src/CfdWorkbench.Core/Provenance.cs` (one composer), `Panes/BrowserPane.axaml.cs`, `PropertiesView.cs` (section title, `:1325`), `ChipSourceName` and its two callers in `WorkbenchController.cs` (the second designation lookup), their tests, the m12d §14.1 trace rows | A0 joined | T1 | 1 | 1 dispatch · 50 calls · 150k · 1 h 30 min | `BrowserRow_AfterEdit_NotTheOriginal`, `Properties_SectionTitle_AfterEdit_NotTheOriginal`, `SourceText_AllSurfacesAgree` red first then green with the DR-INV-1 a string "Modified from NACA 0012"; `BrowserRow_AfterReplace_NameThenSource` unchanged; `run-tests.sh` green | Claude **Sonnet** sub-agent (enforcing) |
| **ST** store and copy (B2, B3, B8) | `src/CfdWorkbench.Persistence/*` (`WindowsProjectStore.cs`, `ProjectStore.cs`, `PreferenceStore.cs`, `SectionLibrary.cs`), `OpenOutcome.cs`, `SaveSectionDialog.axaml.cs`, `src/CfdWorkbench.Cli/Program.cs` (code to exit-status map, `:42-46`), `MainWindow.axaml.cs` (status kind, `:283`), `Panes/StartView.axaml.cs`, the open and save regions of `WorkbenchController.cs`, the store region of `Labels.cs`, Persistence, Cli and copy tests, plus the two named hunks in `CatalogDialogTests.cs` and `ControllerShellTests.cs` | — | T1 | 1 | 2 dispatches · 90 calls · 300k · 3 h | every item red first, then green: (1) DOC-UNSUPPORTED-PLATFORM and DOC-NOT-A-FILE each with its own message, and one end-to-end test that runs each new code through the CLI (exit 3), the status kind (Warning), StartView (a message) and preferences (session-only); (2) the non-NTFS remedy names the drive (test asserts the drive letter in the text); (3) Win32 4395 mapped (a mapping test) and the 4390 comment corrected; (4) `File.Exists` off the UI thread (a test that fails if the probe runs on the dispatcher); (5) one store `Operation` per action (a telemetry test per action: Open, Save, Clear, read-origin); (6) COPY-644 merged into COPY-125 (the one-definition check red on two rows); (7) the Save-section sentence chosen by store admission (a test with an unadmitted store on the host OS); (8) COPY-615: an old-build stored run fixture opened, **fail = any run shown without its COPY-615 reason or with a wrong one**, else the gap is closed by that test; (9) preference load checks folder ownership (a test with a foreign-owned folder → session-only); (10) the owner check and the ACL change use one handle (a test or a code-level assertion that one handle is opened); `run-tests.sh` green | **Codex CLI** `gpt-6.1-sol` reasoning high (observed-only; commit floor enforces) |
| **JF** join fence R2 (B1) | `docs/ai-forward-pack/scripts/conductor-join.py`, its self-test, the investigation's R2 status row | — | T1 | 1 | 1 run · 40 calls · 120k · 1 h | the self-test dirties a path after the fence and asserts the join commit does not carry it (red on today's `git add -A`); git's "would be overwritten" refusal reported apart from a real conflict (red first); the `--continue` path fenced and a post-commit `git status --porcelain` empty outside `.agents/log/`; `check-docs.py` green | **Grok CLI** `grok-4.6` (unsupported until login and one probe pass; fallback Codex `gpt-6.1-sol`) |
| **HX** harness and small fixes (B4, B5, B6, B7) | `ApplyMatchAsync` region of `WorkbenchController.cs`, the SF2 region of `Labels.cs`, `tests/CfdWorkbench.Desktop.Tests/VerticesLifetimeProbe.cs`, `ControllerShellTests.cs`, `tools/run-tests.sh` (one line per variable), the encode-timing check in `tests/CfdWorkbench.Analysis.Tests/` | — | T0/T1 | 1 | 1 dispatch · 60 calls · 180k · 2 h | Match receipt a `Labels` constant (one-definition check red on the literal); the cheap "already matches" test with a mutant; probe: quiet control spawn, 124 reads as a timeout, the Debug SKIP and unknown-arm paths run and logged; the flaky check's race reproduced red by forced timing (an injected delay or barrier in `StatusStrip_BackgroundCompletion_DoesNotReplaceNewerReport`, `ControllerShellTests.cs:69`), then fixed and green, then 20 repeats green **alone** (no concurrent suites: wave-2 rule, and width 3 would collide with SC1 and ST); encode cost measured at the 32,000,000-byte cap (`AuthoringSession.cs:2419`, `MaxBytes` `:2567`) and recorded against the 100 ms rule; **if over, HX files a decision request to `fable-owner` and does not change `AuthoringSession.cs`** (B7 = re-measure only); `run-tests.sh` green | Claude **Sonnet** sub-agent (enforcing) |

Every track: its own worktree from `coord worktree new`, `coord doctor` read back in the tree, a Return written as the
final message, one Opus Adversary review before the join (lenses: SC1 UX & Accessibility + Test Architect; SC2
Test Architect + marine-CAD UX; ST Security + Test Architect; JF Python developer + Test Architect; HX Test Architect +
SRE). Co-Authored-By trailers are checked before a join (wave-2 lesson). **Termination:** a track ends at exit evidence
met and reviewed, or at its budget cap, which is a defect signal reported to the leader, never a reason to continue.
Fallback for any CLI track that fails to start or breaks the commit floor: re-dispatch to a Sonnet sub-agent.

## Serial spine
| item | why it cannot be parallel | who owns it |
|---|---|---|
| A0: join `inv-section-catalog-apply` (docs and probes) | SC1 and SC2 build on its report, probes and register entries (data edge) | main session |
| Commit this plan, then every ruling and logged prompt, before any join | a dirty primary refuses the join (COORD-JOIN-DIRTY-PRIMARY) | main session |
| JF's join runs alone, and the next join is watched step by step | it changes the joiner; a broken joiner must not land under another track's merge, and the next join is R2's first real use | leader decides; main session runs |
| Joins one at a time, loops paused | the Desktop harness crash/124 rule from wave 2: a crashed child is rerun alone | main session |
| SC2 joins before SC1 | SC2 rewrites `ChipSourceName` inside SC1's file region (data edge, review finding 2) | leader |
| SC1 phases 1, 2, 4 | phase 4's sweep is validated by removing phase 1's class (data edge) | SC1 |
| B9 housekeeping last | branch pruning after all wave branches are joined and removed | main session |

## Seams
| from -> to | the request | resolved by |
|---|---|---|
| SC1 -> ST | none expected: SC1's tag and fault copy uses its own range | — |
| SC2 -> SC1 | **hard edge: SC2 joins before SC1.** SC2 replaces the second designation lookup (`ChipSourceName`) inside the file region SC1 edits; SC1 rebases on SC2 and does not touch `ChipSourceName` or source text | leader orders the joins |
| ST -> SC1, ST -> HX | ST changes one assertion each in `CatalogDialogTests.cs` (~`:264`) and `ControllerShellTests.cs` (~`:202`); whichever joins second rebases and keeps both | main session at the join |
| HX -> SC1, ST -> SC1 | disjoint regions of `WorkbenchController.cs`; the later join rebases | main session at the join |
| any -> Owner | a copy supersession, a new store code's wording, or an exit-evidence change | `fable-owner` ruling |

## Struck tracks
| track | why it was not worth its multiplier |
|---|---|
| one track per wave-2 follow-up (eight) | grouped by file family: store, harness, catalog, source text, joiner (wave-2 Simplifier rule) |
| SC1 split by phase | phases share one sheet and one test file; split would share an aggregate and generate seams |
| B3 copy as its own track | its Save-section sentence depends on ST's admission codes (decision and data edge); merged into ST |
| B9 as a track | three commands; main-session mechanics |
| macOS verify gate, compatible fit build | out of scope by the operator's answers |

**Cost.** Five tracks at width 3, plus a leader, an Owner and five reviews: about 10-15 times one session's tokens
(GO6). Each track is paid for by **independence** (disjoint file families, no decision edge after the operator's
answers) and **context hygiene** (SC1's three phases and ST's Win32 detail would crowd one context past the 400k ceiling
that broke A3 in wave 1). The CLI tracks add harness diversity at no Claude token cost.

## Order of operations
| # | action | cost | why now |
|---|---|---|---|
| 1 | Commit this plan (md, html, index) on main | 1 commit | the plan is cited by every brief |
| 2 | A0: `conductor-join` the investigation branch; remove its worktree | 1 join, ~5 min | SC1 and SC2 need it |
| 3 | Spawn `opus-leader` and `fable-owner`; leader writes the five briefs | ~60k | briefs before dispatch |
| 4 | Ask the operator to run `grok login`; probe Grok with one read-only prompt | 2 min | JF's harness is unsupported until then |
| 5 | Wave A at width 3: SC1 (Opus), ST (Codex), SC2 (Sonnet) | ~3 h | the longest tracks start first; SC2 early so it joins before SC1 without pushing a late rebase onto SC1 (review cycle 2) |
| 6 | As a slot frees: JF (Grok, or Codex as fallback), then HX (Sonnet) | ~2 h | width cap 3; JF depends on nothing |
| 7 | Review each Return (Opus Adversary); repair cycles as the leader rules | per track | — |
| 8 | Joins as ready, one at a time, with SC2 always before SC1 (hard edge); JF's join alone and the next one watched | ~5 min each + ring | spine |
| 9 | B9: rmdir the empty cr2 folder; `git branch -d` merged old branches; list the rest; confirm the wave-2 OneDrive throwaway files are gone (removed at the wave-2 close) | 5 min | after all joins |
| 10 | Planned vs actual appended here; push; CI green read back | — | close |

## Gate record

`GATE prepare-for-coordination · 2026-10-26 · Orchestrator (author, Peer) · Test Architect with the Simplifier and Tech
Lead lenses (Adversary, Opus sub-agent, read-only) · cycle 1 verdict BLOCK (one blocker: ST's owned files missed the
surfaces its new store codes reach: CLI exit map, status kind, StartView, preferences, the open and save regions) ·
resolution: ST owns those files and adds one end-to-end red-first test; SC2 owns ChipSourceName and joins before SC1
(hard edge); m12d and two test files declared shared by section or hunk; every ST item given a named red-first test
and COPY-615 a fail condition; HX's flaky fix reproduced red by forced timing and repeated alone; HX files a request
instead of changing AuthoringSession.cs; SC1's evidence gains the damaged My-sections rows, the manual pointer pass
and catalog.preview unchanged; finding 8 already met (the OneDrive throwaway files were removed at the wave-2 close),
B9 confirms. Tech Lead casting vote: five tracks, no strike. Cycle 2 verdict PASS-WITH-CONDITIONS, CLEARS-THE-VETO yes:
three conditions folded (a damaged-row case in the Phase 1 test; SC2 dispatched in wave A in place of JF; the
joint-satisfiability paragraph names all four controller regions). Each Return attaches a Proof Pack before its join.`

## Planned vs actual (record by `wave3-coordinator`, 2026-10-26; final at the ST join, ae8c624a)

**Outcome.** All five tracks joined to `main` and pushed; every join's ring green and all ten verify gates green. A0
joined first (bf793289). Ruling 267 (fable-owner) is the wave's one ruling. B9 done: the cr2 folder was already gone,
45 merged local branches deleted with `git branch -d` (0 kept; the list is in the session scratchpad), no wave-2
OneDrive throwaway file remains.

| track | planned budget | actual (calls) | review cycles | result | join |
|---|---|---|---|---|---|
| SC2 source text (Sonnet) | 50 · 150k · 1 h 30 | ~33 + repair ~14 | 1 (ACCEPT-WITH-CONDITIONS: the "one lookup" claim narrowed to gen:/vend:; a vend: Core test; lazy catalog read) | Browser row, section title and every surface say "Modified from NACA 0012" (DR-INV-1 a); `ChipSourceName` removed; follow-up: designer-readable names for dat and My-sections origins | 9c6d25a9 (ring 278 s) |
| JF join fence R2 (Codex gpt-6.1-sol; Grok fallback) | 40 · 120k · 1 h (55 after the PATH item) | 33 + repair 11 + gate fix ~6 | 1 (ACCEPT-WITH-CONDITIONS: a foreign audit append mid-join was still staged) + the join's step-8 gate | R2 cases A/B/C, audit ownership, vanished outputs, MERGE_HEAD guard; exits 13/12/7; DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=false in five tools scripts with a fail-closed control | e91f0e85 + gate fix ba65057f |
| SC1 catalog sheet (Opus) | 80 · 300k · 3 h | 37 + 33 + 25 + repair 27 = 122 (**cap fired**) | 1 (ACCEPT-WITH-CONDITIONS: queued choice + step still dropped a fit silently; sweep claim narrowed; register controls named) | muted rows with state tags (DR-INV-2 a), every fit exit has a reason, Enter during the fit applies on landing, catalog.select/apply telemetry, the Modal-sheet opening-state sweep | 42c573aa (ring 248 s) |
| HX harness and small (Sonnet) | 60 · 180k · 2 h | 68 + repair 14 (**cap fired**) | 1 (ACCEPT-WITH-CONDITIONS: the 20 repeats alone; wait on the completion signal) | Match receipt a Labels constant; probe follow-ups; status-strip race fixed in the test (review mutants red); 20 of 20 alone (main session, 0 foreign processes); encode median ~60 ms, cold first read 99.3 ms (at the limit) | 3ec7c693 (ring 258 s) |
| ST store and copy (Codex gpt-6.1-sol) | 90 · 300k · 3 h | 11 + 11 (boundary stops) + 45 + 50 + repair 39 = 156 (**cap fired**) | 1 (ACCEPT-WITH-CONDITIONS: a Save-section regression on an unadmitted host; a text-scan control; two copy sentences false for some causes; the COPY-644 marker) | separate store codes to every surface; the drive named only for a non-NTFS cause; 4395; store.* per action; preference ownership on one handle (runtime test, three mutants red); item 8 NOT DONE under Ruling 267 | ae8c624a (ring 259 s) |

**Boundary corrections (coordinator).** SC2 and ST owned new test files plus one wiring line each in
`WorkbenchTests.cs` (shared by hunk); SC1 owned `ControllerSectionTests.cs`; HX had a conditional grant on
`StatusSlot.cs`/`ShellHost.cs` (unused); ST gained the Core store-test assertions, the `RecordPersistence` region of
`AuthoringSession.cs`, and "any file no other track owns, listed in the Return" after two up-front stops. **Root
cause:** the plan listed product files per track but not the existing tests that pin today's behaviour; a literal coder
(Codex) stops at each. The next plan lists the test files that assert a code a track changes.

**Which justifications paid.** Independence paid: the five tracks never needed a seam request; the only collisions were
the defect register (two hand-resolved merges, both "keep both sides": the register driver unions appends but not an
in-place edit next to an append) and the expected rebase of SC1 over SC2 (clean). Context hygiene paid: SC1's three
phases ran as three fresh dispatches with no ceiling breach. Harness diversity paid in findings, not speed: Codex was
literal about ownership (two stops), hit one model-capacity error (resumed by session), and its JF code passed its own
checks but failed the join's text-I/O gate. Grok was never logged in, so JF ran on its Codex fallback.

**Budget signals (defects in the estimate, not reasons to raise caps).** SC1 122/80: the plan sized three unequal
phases equally. ST 156/90: the missing test-file ownership above plus two repair-heavy dispatches. HX 82/60: the forced
timing work and two machine-quiet checks.

**Findings for the next plan.**
- **User PATH pollution (Verified, fixed):** 72 dead `%TEMP%\cfd-*-recount-*\dotnet-home\.dotnet\tools` entries
  (7,883 characters) made `cmd.exe` unable to resolve `ping`, failing a Cli check in every tree. The operator approved
  the cleanup (backup in the session scratchpad); JF's change stops the leak (class TOOL-FRESH-HOME-PERSISTS-USER-ENV).
  Shells started before the cleanup keep the old PATH: strip `recount-` entries inline until they restart.
- **ui-craft-gate cannot scan Avalonia XAML (Verified):** no windows-arm64 detector build exists; the x64 build under
  emulation exits 0 with no findings on a planted bad `.axaml` (magenta, size 13.7, Comic Sans). SC1's craft-gate item
  is Not assessed. A clean run on `.axaml` is vacuous until the gate refuses file types its detector does not read.
- **Worktree cleanup ORPHANED (Verified cause):** every track tree left `.tmp-tests/tmp/winstore-*` paths over 260
  characters that `git worktree remove` cannot delete; `rm -rf` then `git worktree prune` works.
- **Brief paths leak into audit prompts:** a coordinator brief quoting a scratchpad path under the user profile was
  copied by a CLI coder into two audit entries; check-docs' PROOF-PII check stopped the join; redacted. Briefs cite
  scratchpad files relative to `%USERPROFILE%`.
- **R2's first real use:** the second JF join exited 7 because Ruling 267 was written into the primary mid-join; the
  new joiner kept it out of the join commit, as designed. Recovery: commit the ruling, run the gates, push.
- **Open follow-ups:** designer-readable source names for dat and My-sections origins (SC2); `SaveSectionDialog.ShowError`
  sets ItemStatus invalid with no rendered class (SC1); the COPY-615 headline for runs from builds at or after 0b277815
  (Ruling 267); preference ownership on macOS not checked; proposed copy rows COPY-647..651 and 677..680 await the
  operator; the manual pointer pass on the catalog sheet (operator steps in SC1's Return); upstream the conductor-join
  and recount changes to the pack.
