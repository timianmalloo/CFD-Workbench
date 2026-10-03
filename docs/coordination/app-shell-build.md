---
id: coordination-app-shell-build
title: "Coordination plan - app-shell build (M1.2a shell and M1.2e floats and layouts)"
type: plan
status: accepted
owner: "@cfd-leader-fbfa35dc"
tags: [coordination, worktrees, parallelism, desktop, shell, m1.2a, m1.2e]
links:
  - { to: design-app-shell, rel: implements }
  - { to: architecture-application, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: design-section-editor, rel: relates-to }
  - { to: coordination-application-build, rel: relates-to }
review-by: "2026-10-27"
summary: >-
  Schedules the app-shell design's nine tracks plus one review-flag track across Claude Code, Grok and Agy with two
  concurrent coding lanes, a serial spine of Owner rulings, S8, G0 and D3a, and M1.2b-d and the M1.1b follow-ups as
  gated later waves.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# Coordination plan: the app-shell build

- **Scope:** the app-shell build, meaning the shell parts of M1.2a and all of M1.2e
  ([design](../design/app-shell.md) §14, [architecture](../architecture/application.md) §10.6). The design's track table
  (S8 · G0 · C1 · P1 · D1 · D2 · D3a · D4 · U1) is the decomposition. This plan schedules and assigns it; it does not
  re-cut it. M1.2b–d and the M1.1b follow-ups appear only as gated later waves.
- **Inputs (final):** spec 1.6 `f56d259`; architecture §10 and ADR-0005–0009 `6301eb9`; app-shell design `494aef2`.
- **Seats:** Leader and Coordinator = Claude Code Opus 5.5, session `fbfa35dc` (the main session). Owner = Claude Code
  Fable, seat `cfd-owner-fbfa35dc`. `assume:` that is the session id the Coordinator gives the Owner when it spawns
  it. Confirm with `coord session list` at step 2. If it differs, change the one "rules" string in Track detail. The
  Owner rules into `docs/notes/rulings.md` (class `register`). The Owner never authors track code or clears its own veto.
- **Author:** `/prepare-for-coordination` sub-agent `fbfa35dc-prepare`, 2026-09-27. Git before: `494aef2`.

Labels: **Verified** = observed in this run; **Inferred** = reasoned, not observed; **Flagged** = observed but suspect.

## Layer state

Measured with `coord doctor` in this tree on 2026-09-27 (exit 1), `pack-doctor.py --json` (20 PASS · 2 WARN · 0 FAIL)
and `coord session list` (0 active sessions, 71 files scanned).

| check | result | meaning |
|---|---|---|
| registry | ok - 8 pattern(s) | `.agents/artifacts.yml` is present and parses. Anything not listed stays `authored` |
| merge driver | effective - coord-regen, coord-register declared and registered | derived files regenerate and registers union-merge at every join. It was installed **once, in the primary checkout**. Each worktree **inherits** it through the shared `.git/config` and `.git/hooks`. Never run `coord install` in a track tree |
| regeneration | 1 artifact OWED, then `coord regen` rendered `docs/audit/audit-data.js` (338 audit + 25 change entries) | cleared in this tree. The regenerated file is committed with this plan |
| leader | EXPIRED 126137 s ago (epoch 29, was `fbfa35dc`) | no live designation. Order of operations step 1 pins it |
| heartbeat | 5 sessions, newest beat 162731 s ago; 0 live | no track is running. Nothing is held |
| requests | **FAIL** `COORD-REQUEST-SILENT-EXPIRY`: `req-01M3BWBXBGBHHTKCC9ZBRCT2S2` is past its deadline with no outcome | step 1 runs `coord request expire`, which records the fallback as the outcome. This is the Coordinator's action, not this run's |
| lease overlap | none (0 live leases) | no contention now |
| harness capability | claude edit boundary "enforcing" (spike S5, 2026-08-24, **not measured here**); copilot "historical" | see Harness qualification and the pre-commit floor row |
| pack-doctor `doorbells` | not recorded (no `coord-mail.py dispatch` probe run) | every track row carries doorbell = not recorded |
| pack-doctor WARN | copilot `contextTier=long_context`; knowledge graph has stale or flagged nodes | not on this plan's path, except the three `design-app-shell` flags, which R0 clears because the brief requires them cleared before the first merge |
| pre-commit floor | `.git/hooks/pre-commit` runs `coord-core.py precommit` from the primary checkout | Verified present, but **advisory unless `AGENT_SESSION` is set**. This run's own commit printed "advisory: AGENT_SESSION is unset, so nothing was checked." So every dispatch exports `AGENT_SESSION=<track session>` (and `AGENT_WI=<track>`) in the worktree shell before it runs the harness command. Whether an external harness passes that environment to its `git commit` is **not assessed**, so the Leader's branch read stays the real control |

**Finding (Flagged, for the Coordinator's defect register):** `coord classify init --force` run from this linked worktree
wrote the **primary checkout's** `.agents/artifacts.yml`. It also regenerated `docs/audit/audit-data.js`,
`docs/docs-index.js` and `docs/audit/index.html` there. All four files had the same mtime (19:55:29 local), which was
the second of this run's command, so they were this run's writes. They were restored with `git checkout --` in the primary
checkout, which is now back to `?? .agents/log/` only. Class: a coordination command resolves the primary checkout from a
linked worktree and writes there. Rule for this plan: **no track runs `coord classify` or `coord install`.** The
Coordinator runs them only in the primary checkout. Registering this class and building its control belongs to the
Coordinator. This run did neither.

## Artifact classes

Classes are read from `.agents/artifacts.yml` in this tree. Each derived command was run in this tree during this run:
`coord regen` ran the audit render, and `docs-graph.py derive` ran at emit.

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js` | derived | `python docs/ai-forward-pack/scripts/docs-graph.py derive` via the coord-regen driver | **none**: any track that edits frontmatter regenerates it, and the join regenerates it again |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `python docs/ai-forward-pack/scripts/audit-log.py --root docs --project CFD-Workbench render` | **none** |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | union merge (coord-register) | **none**: every track appends its own audit entry |
| `docs/notes/rulings.md` | register | union merge | **none**: only the Owner appends |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | union merge | **none** |
| `.tmp-tests/` | ignored (`.gitignore`:30) | per-tree test logs read by `check-named-tests.py` | **none**: never committed |
| `src/CfdWorkbench.Desktop/packages.lock.json` (new, D3a) | authored | Kept authored on purpose. Regenerating a lock file resolves versions, so a merge driver would hide a pin change (STRIDE row "a changed transitive package") | one owner: D3a |
| `tests/CfdWorkbench.Core.Tests/Fixtures/layout/**` | authored | golden fixtures are the assertion; nothing regenerates them | one owner: P1 |
| `docs/design/app-shell.md` | authored, **frozen for the wave** | the single authority `check-named-tests.py` reads | Coordinator only, after an Owner ruling, and only serially (Serial spine) |
| `src/**`, `tests/**`, `tools/check-named-tests.py`, `DESIGN.md`, `docs/proof/**`, `docs/reviews/app-shell-native.md`, spec, ADR, architecture | authored | conventional conflict markers | **one owner per file**. See Tracks and Track detail |

The only real contention is authored C# and AXAML. Every file has exactly one owner at a time. Four files change hands
at a merge, never concurrently (Seams).

## Tracks

Width cap: **two concurrent delegated coding tracks** (one Grok and one Agy), plus the Claude lane. Every track's test
command follows the design's §14 wording: "`tools/run-tests.sh` (exit 0), then `python3 tools/check-named-tests.py
<track>` (exit 0)". Named-check counts are simulated from §12.2's rule over §9 and §12.4. They are **Inferred** until
G0's script lands: C1 14 · D1 12 · D2 21 · D3a 32 · D4 26 · P1 23.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **R0 review flags** | `docs/specs/cfd-workbench-v1.md`, `docs/adr/0009-cad-first-shell-docking-and-menus.md`, `docs/architecture/application.md` (the `design-app-shell` `review-suggested` entries, and the rows those flags name) | O1 rulings | T1 | 0 | 40 calls · 150k tokens · 45 min | the `design-app-shell` flags on the spec, ADR-0009 and the architecture are cleared through `docs-graph.py`. Each resolution cites an Owner ruling or an in-place edit. `python3 tools/check-docs.py` exit 0. R0 does not send OI-S1, which O1 sends | Claude Code **Sonnet** |
| **S8 spike** | `docs/proof/cad-first-spikes/dock-split/` (probe + raw output) | O1 | T1 | 0 | **time box 1 h** — box = 3× a measured same-class prior; no prior → 1 h, measured time recorded in the audit entry (Ruling 54). S8 measured 254 s; no same-class prior existed, so the box held. **Done** · 60 calls · 200k tokens | probe source and raw output committed. Verdict: "split drops blocked by `CanDrop`/capability overrides: yes or no". The G0 schema variant is named from it | Claude Code **Opus 5.5** (this Mac) |
| **G0 glue** | `src/CfdWorkbench.Persistence/LayoutDocument.cs`; `Run()` lines in `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` + empty `WingEstimatesTests.cs`, `DimensionTests.cs`, `LayoutFileTests.cs`, `PreferenceStoreTests.cs`; the Desktop `Check` helper + spawn lines in `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs` + empty `ShellModelTests.cs`, `ControllerShellTests.cs`, `ShellWindowTests.cs` (seam 1); `tools/check-named-tests.py` | S8 (and its design amendment, if any) | T2 | 0 | 120 calls · 400k tokens · 2.5 h — box = 3× a measured same-class prior; no prior → 2.5 h, measured time recorded in the audit entry (Ruling 54); no same-class prior found in `docs/audit/audit-log.jsonl`, so the box held | `tools/run-tests.sh` exit 0. `check-named-tests.py --self-test` exit 0, and each of its two planted failures (no-track name, glob) was red first. A planted failing check in a spawned suite made `run-tests.sh` exit nonzero, and that red run is recorded. `LayoutDocument.cs` equals §3.4 (or the one-group shape if S8 says yes). `check-docs.py` exit 0 | Claude Code **Opus 5.5** |
| **C1 core estimates + Span** | `src/CfdWorkbench.Core/WingEstimates.cs`; `ApplyDimension` + `EditReference "dimension"` in `src/CfdWorkbench.Core/AuthoringSession.cs`; span patch in `src/CfdWorkbench.Core/FoilSource.cs`; `tests/CfdWorkbench.Core.Tests/WingEstimatesTests.cs`, `DimensionTests.cs` | G0 | T2 | 0 | 150 calls · tokens not recorded by the harness · 90 min | the test command, with C1's 14 names PASS. The red-first `Receipt_Dimension_OldReaderRefusesDocReference` commit and its run log are in the proof pack before the `EditReference` change | Grok `grok-4.7` |
| **D1 shell model** | `src/CfdWorkbench.Desktop/Shell/WorkspacePresets.cs`, `FloatFrame.cs`, `FloatPlacement.cs`, `ScreenSource.cs`, `FocusRing.cs`, `CommandTable.cs`, `ShellEvents.cs`, `FocusTargetEvents.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellModelTests.cs` | G0 | T2 | 0 | 70 min (Agy `--print-timeout`) · tokens not recorded | the test command, with D1's 12 names PASS. `CommandTable` references only BCL `System.Windows.Input.ICommand` (seam 2) | Agy `gemini-3.8-flash-high` |
| **D2 controller** | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `Selection.cs`, `PropertiesView.cs`, `OpenOutcome.cs`; `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs` | C1 | T2 | 0 | 70 min · tokens not recorded | the test command, with D2's 21 names PASS, including `Open_CancelDuringPrepare_CurrentFoilUnchanged` (the as-built open-cancel hazard) | Agy `gemini-3.8-flash-high` |
| **P1 preferences** | `src/CfdWorkbench.Persistence/LayoutCodec.cs`, `RecentList.cs`, `PreferenceStore.cs`; `tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs`, `PreferenceStoreTests.cs`, `Fixtures/layout/**`, `CfdWorkbench.Core.Tests.csproj` | G0 | T2 | 0 | 150 calls · tokens not recorded · 90 min | the test command, with P1's 23 names PASS. The three v2 rollback fixtures were red, then green, with both runs recorded (the Data & Persistence condition) | Grok `grok-4.7` |
| **D3a shell host (M1.2a)** | `src/CfdWorkbench.Desktop/Shell/ShellHost.cs`, `ShellLayout.cs`, `NativeMenuBuilder.cs`, `EditVerbRouter.cs`; `App.axaml(.cs)`, `MainWindow.axaml(.cs)`, `Styles.axaml`, `CfdWorkbench.Desktop.csproj` + `packages.lock.json`, `Program.cs`; `Panes/StartView.axaml(.cs)`, `Panes/PropertiesPane.axaml(.cs)`, `Panes/BrowserPane.axaml(.cs)`, `ModelArea.axaml(.cs)`, `SectionEditorView.axaml(.cs)`; `FocusedTargetChanged` raising in `Viewport.cs`, `SectionCanvas.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`; `docs/proof/app-shell-test-inventory.md` + the ported and deleted rows of `WorkbenchTests.cs` | D1, D2, P1 (seam 7) | T2 | 0 | up to 3 dispatches × 70 min, each ending in a commit and a Return · tokens not recorded | **checkpoint D3a-0:** the inventory is committed before any source change and the Coordinator reviews it. Mutations may be batched; every ported row goes red in at least one recorded mutation run (Ruling 54) (design §12.5). Then the test command with D3a's 32 names PASS, and `check-named-tests.py` fails if the ported-name column is empty. `Architecture_DockConfinedToShell` was red first with a planted `Dock.` reference, and that log is recorded. `dotnet restore --locked-mode` exits 0 (design §12.1 T3). `run-tests.sh` is run **three times** with identical `PASS` sets; every named suite's PASS set non-empty (Ruling 54). The join's `xaml-token-lint` check clean (Ruling 54) | Agy `gemini-3.8-flash-high` |
| **U1a UI judgement (M1.2a)** | `DESIGN.md` §7 COPY rows for §11's strings; `docs/reviews/app-shell-native.md` (M1.2a rows) | D3a | T1 | 1 (UX & A11y lens, Adversary Mode) | 80 calls · 300k tokens · 2 h, plus one operator session | `python3 tools/check-docs.py` exit 0. `python3 docs/ai-forward-pack/scripts/design-lint.py --strict DESIGN.md` clean. The native review holds attach proof (CO-UI-READY), not launch success. It includes the M1.2 entry-gate rendered/AX proof (architecture §10.6). If a native row cannot run in the session, it is recorded as operator-run and not done. **U1a, and so M1.2a, stays open until the operator runs that row** | Claude Code **Opus 5.5** |
| **D4 floats, workspaces, layouts (M1.2e)** | `Shell/ShellHost.cs`, `ShellLayout.cs` (handed over after D3a merges), `Shell/Maximize.cs`, `Shell/FocusSafeFloats.cs`, `Shell/PaneViewState.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellFloatTests.cs`; the `--shell-float` spawn line in `WorkbenchTests.cs` (seam 3) | P1, D3a, **M1.2c merged** | T2 | 0 | up to 3 dispatches × 70 min · tokens not recorded | the test command, with D4's 26 names PASS. `run-tests.sh` is run three times with identical `PASS` sets; every named suite's PASS set non-empty (Ruling 54). The join's `xaml-token-lint` check clean (Ruling 54). OP-S's S6 and S7 single-monitor rows PASS from the packaged `.app` **before merge** | Agy `gemini-3.8-flash-high` |
| **U1e UI judgement (M1.2e)** | `DESIGN.md` §7 rows for the M1.2e strings; `docs/reviews/app-shell-native.md` (M1.2e rows: per-window menus with a float key, S6/S7 rows) | D4 | T1 | 1 | 80 calls · 300k tokens · 2 h, plus one operator session | as U1a, for M1.2e | Claude Code **Opus 5.5** |
| **OP-S operator spikes** | `docs/proof/cad-first-spikes/` rows for S6, S7 (single-monitor), S2, S3 | a D4 build on its branch | T1 | 0 | ADR-0009 time boxes: S6 1 h · S7 2 h · S2 3 h · S3 1 h | **operator-run, never marked done by an agent.** S6 and S7 single-monitor rows gate D4's merge. S2 and S3 stay Inferred in the proof pack until run | operator (hardware: a Retina display plus a 1× external display) |

Deferred, not scheduled: **S1** (Windows 11), **S4** (Snap Layouts), **S5 NVDA**. The machine has no Windows host. S1's
gate is quoted from ADR-0009 §Spikes: "**Run S1 before the layout file format is frozen** (M1.2e)". The design (§3.5)
bounds that gate: "**S1 gates freezing** it for Windows (a new member → v2 with a v1 → v2 migration before any Windows
build writes)". So v1 ships macOS-only in M1.2e, and S1 runs before any Windows build writes a layout file.

### Track detail

Every track uses the same termination condition. It ends at its exit evidence. Or it ends when the **2-cycle repair cap**
fires (AGENTS.md project section). At the cap the track stops and the Coordinator reports to the operator. It never
issues a third cycle. A missing Return section, or a branch without the claimed commit, counts as a failed cycle
(HARNESS-SILENT-EXIT). Seam requests go to the Coordinator with `coord request add`, and each needs a deadline and a
fallback. Decision requests go to the Owner with `coord decide request --to cfd-owner-fbfa35dc`. Every row carries the
leader epoch that step 1 reads back (`assume:` 30, which is 29 + 1 after `coord leader pin`; `coord leader who` confirms
it). Doorbell for every row: **not recorded**.

| track | owner seat | rules (Owner) | seam requests expected | deadline (wall from dispatch) | fallback | lane |
|---|---|---|---|---|---|---|
| R0 | Claude Sonnet sub-agent | `cfd-owner-fbfa35dc` | none. If a flag needs a spec decision beyond O1, it becomes a decision request | 45 min | Opus 5.5 takes the leftover flags | Claude |
| S8 | Claude Opus sub-agent | `cfd-owner-fbfa35dc` | none | 1 h (hard) — box = 3× a measured same-class prior; no prior → 1 h, measured time recorded in the audit entry (Ruling 54); measured 254 s | undecided at 1 h → S8 is recorded **not done**, and the Owner rules the schema before G0. The default is the design's `groups` shape. What is Verified is that validation cannot be overridden (design §3.4). Whether `CanDrop` or capability overrides block splits is **Not assessed** (design §5.2) | Claude |
| G0 | Claude Opus sub-agent | `cfd-owner-fbfa35dc` | none (it owns all the glue) | 2.5 h — box = 3× a measured same-class prior; no prior → 2.5 h, measured time recorded in the audit entry (Ruling 54) | Coordinator finishes it inline. G0 is serial, so a second sub-agent saves nothing | Claude |
| C1 | Grok session | `cfd-owner-fbfa35dc` | C1 → G0: `IdentityTests.cs` `Run()` line shape | 90 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle | A |
| D1 | Agy session | `cfd-owner-fbfa35dc` | none | 70 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle | B |
| D2 | Agy session | `cfd-owner-fbfa35dc` | D2 → C1 (merged): `WingEstimates` members | 70 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle | A |
| P1 | Grok session | `cfd-owner-fbfa35dc` | P1 → G0: any `LayoutDocument` change is a schema change (serial; Owner) | 90 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle | B |
| D3a | Agy session | `cfd-owner-fbfa35dc` | D3a → D1 or D2 (both merged): member gaps. D3a → U1: string mismatches | 3 × 70 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle. A continuation dispatch that returns with a commit is not a repair cycle | A |
| U1a / U1e | Claude Opus sub-agent + operator | `cfd-owner-fbfa35dc` | U1 → D3a or D4: AXAML string fixes (a Sonnet edit after merge) | 2 h | native rows recorded "operator-run, not done". U1 and its slice stay open until the operator runs them | Claude |
| D4 | Agy session | `cfd-owner-fbfa35dc` | seam 3; D4 → controller owner at that time (M1.2c merged) | 3 × 70 min | Codex `gpt-6-sol` as cycle 2 only when cycle 1 failed from a harness fault (stall, silent exit, no Return); a code fault repeats on the same harness. No third cycle. A continuation dispatch that returns with a commit is not a repair cycle | A |
| OP-S | operator | `cfd-owner-fbfa35dc` | none | operator-scheduled | S6 or S7 not run → D4 does not merge | operator |

### Harness qualification (from what was verified here)

| harness · version (Verified present) | needed from the delegation mechanism | edit boundary | dispatch | basis |
|---|---|---|---|---|
| Claude Code (Opus 5.5, Sonnet, Fable) | exclusive paths, a Return block, audit entry | **observed-only** here. Spike S5 records "enforcing" (2026-08-24), but it was not re-measured in this run | observed-only | `coord doctor` capability block. The commit floor is advisory without `AGENT_SESSION` |
| Grok 1.0.41, `grok-4.7` / `grok-4.7-build-fast` | foreground run, commits on its branch, Return block | **unsupported** (no hook qualification; the floor is enforcing only if `AGENT_SESSION` reaches its commit, which is not assessed) | observed-only: the M1.1 B-track runs of 2026-09-25 returned with commits (session run logs) | the commit floor (if `AGENT_SESSION` reaches it) and the Leader's branch read |
| Agy 1.2.11, `gemini-3.8-flash-high` | the same; **"foreground only; no background processes"** in every brief | **unsupported** | observed-only, with one recorded silent exit 0 (HARNESS-SILENT-EXIT) | the commit floor (if `AGENT_SESSION` reaches it). The Leader reads `git log` and `git status` and requires the Return section |
| Codex, `gpt-6-sol` | fallback only | **unsupported** | not exercised in this run | fallback only |

Dispatch commands, exactly as the operator gave them:
`grok --cwd <wt> -m grok-4.7 --always-approve --no-subagents --output-format plain -p "$(cat brief.md)"` and
`cd <wt> && agy --model gemini-3.8-flash-high --dangerously-skip-permissions --print-timeout 70m -p "$(cat brief.md)"`.
Every delegation carries a prompt compiled for its target harness with `/compile` (CO-S0). A compile whose
`dispatchable` is false, or whose text has a `DR-n` line, is not dispatched: stop with
`decision request unanswered: DR-n`. **Known pack issue (Flagged):** `prompt-compile.py` sets `dispatchable` false
whenever `decision_requests` is non-empty, even when every one is answered. So briefs state Owner rulings as settled
facts with the ruling number, never as DR lines.

### Decision requests and what each one gates

| DR | Default | Gates (this plan) | Later wave it gates |
|---|---|---|---|
| DR-S1 `Avalonia.Headless` | No | **G0** harness shape. D3a and D4 window suites use the §12.3 rules | — |
| DR-S2 modified chords after committing a text field | Yes (§6.4) | **D3a** (`EditVerbRouter` chord list); **R0** (B7 wording, the design's "Finding for the spec owner") | — |
| OI-S1 S1-Switch-to-Analysis N/A | written N/A requested | **O1** (the request); **D4 merge** (the Test Architect's condition: "OI-S1 and the 15 M1.2b–d edges before the M1.2e merge") | — |
| DR-2 held line for typed chords | leading edge | none. C1 types Span only | M1.2b |
| DR-6 gesture commit | commit at gesture end | none | M1.2b |
| DR-7 activity rail | rail outside the left side bar, not built in M1.2 | none. If ruled "build in M1.2", D3a gains a shell region outside the Dock host | — |
| DR-8 My sections location | installation library | none | M1.2d |
| DR-9 chord rule | strict A4.6 with the operator's rule | none | **M1.2b** (and its `/design-slice`) |
| DR-10 channel ceiling | 6–10 | none | M1.2b |
| DR-11 section point types | paired per chord position | none | **M1.2c** (and B6, below) |

The shell does not depend on DR-2 or DR-6 to DR-11 (design §13). The plan relies on that statement and does not re-derive it.

### Later waves (not planned here)

| wave | gated on | notes |
|---|---|---|
| M1.2b rail points | its own `/design-slice`; DR-9 (and DR-2, DR-6, DR-10) | will own `WorkbenchController.cs`, `Viewport.cs` and Properties point rows after D3a |
| M1.2c section editor mode | its own `/design-slice`; DR-11 | **D4 depends on M1.2c merged**. The `section-editor.md` spec-1.6 and architecture flags, and its as-built "full" label (B1–B5 merged), are cleared by that `/design-slice`. The M1.1b follow-up **B6** (independent abscissae) belongs here. It was deferred when its 2-of-2 cap fired (section-editor §9). A restart needs an Owner ruling that uses the recorded restart point, and only matters if DR-11 is overturned or the multi-profile refusal must lift |
| M1.2d catalog and My sections | its own `/design-slice`; DR-8 | M1.1b follow-up: DAT import accepts at `maxRes <= 1e-5` normalised (`DatImport.cs`:255), not A4.6's 10 µm at local chord. Architecture §10.7 routes it "For design-slice" |
| M1.2e join | D4, U1e, OP-S S6/S7, OI-S1 | the S1 freeze stays open for Windows |

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| O1 Owner rulings: accept `design-app-shell` (status "Owner acceptance pending"), ADR-0006, ADR-0007 and ADR-0009 with its two recorded deviations, architecture §10 for M1.2a and M1.2e; DR-S1 and DR-S2 defaults; seams 1 and 7; the track count (Tech Lead casting vote); send OI-S1 to the spec owner | decision edge into every track. §10 and ADR-0005–0009 are "Proposed until the Owner rules them" (architecture §10). Fails GO5(b) | Owner `cfd-owner-fbfa35dc` |
| S8 → (design amendment if splits are blockable) → G0 | the design orders it: "**S8 runs before G0** (§14): if `CanDrop`/capability overrides do block splits, G0 lands the one-group shape". Data edge | S8 track; the amendment is the Coordinator's after an Owner ruling |
| G0 | freezes `LayoutDocument`, the Desktop harness and `check-named-tests.py`. Until they are fixed, every track's result changes shape. Fails GO5(b) | G0 |
| C1 → D2 | data edge: D2 calls `WingEstimates.From` and `ApplyDimension` | lane A |
| D3a | joins D1, D2 and P1, and owns `App`, `MainWindow`, the csproj and the lock file that every Desktop surface shares. Two sessions in the same layer fail GO5(c) | D3a |
| handover of `ShellHost.cs`, `ShellLayout.cs` and the `WorkbenchTests.cs` spawn line from D3a to D4 | the same files change hands; only one owner at a time | D3a, then D4 |
| M1.2c merged → D4 | named in the design's dependency column | D4 |
| OP-S S6/S7 → D4 merge | the design: "**S6** gates M1.2e's merge" | operator |
| first product join after G0 | read `coord doctor` in the integration tree before two lanes rely on the driver. It was measured effective here, but it is re-read at the first join | Coordinator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| 1 · G0 -> D1, D2, D3a | the spawn lines G0 owns call `ShellModelTests.Run`, `ControllerShellTests.Run`, `ShellWindowTests.Run`, which must exist to compile | G0 creates them as empty classes. This is the design's "empty classes for C1/P1" pattern applied to Desktop. Ownership passes at G0's merge. `assume:` the design names stubs only for C1 and P1. **O1 confirms**; if refused, each Desktop track owns its own spawn line, as seam 3 already does for D4 |
| 2 · D1 -> D3a | `CommandTable` rows hold an `ICommand`. `RelayCommand` comes from `CommunityToolkit.Mvvm`, a transitive of `Dock.Model.Mvvm`, which D3a adds to the csproj (Verified: the Desktop csproj references no Dock package today) | D1 uses only BCL `System.Windows.Input.ICommand` and test stubs. D3a creates the `RelayCommand` instances. No edge points back, so there is no cycle |
| 3 · D4 -> D3a (merged) | add the `--shell-float` spawn line in `WorkbenchTests.cs` (the design: "and later `--shell-float`") | that line's ownership passes to D4 when D3a merges |
| 4 · D4 -> controller owner (M1.2c merged) | any controller member for `Workspace_Switch_KeepsSelectionCameraDraftAndFocus` | `coord request add` to the Coordinator, who hands `WorkbenchController.cs` to D4 while no M1.2c track holds it |
| 5 · P1 -> G0 | any `LayoutDocument` change | refused inside the wave. It is a schema change and goes to the Owner (version-first rule, §3.5) |
| 6 · U1 -> D3a / D4 | an AXAML string that disagrees with a COPY row | during the track: counted against its repair cap. After merge: a Sonnet edit on its own branch |
| 7 · D3a -> P1 (merged); D2 -> recent add | the Start Recent rows and File ▸ Open Recent read `RecentList`. The design's E7 row puts "`OpenAsync` on success" as the recent-add point, but §14 lists neither P1 → D3a nor P1 → D2 | D3a consumes `PreferenceStore.LoadRecentAsync` and `UpdateRecentAsync`. The recent add runs in D3a's `ShellHost` on the `Opened` outcome of D2's `OpenAsync`, so D2 stays free of preference storage and runs in parallel with P1. `assume:` this placement matches the design's intent. **O1 confirms**. If refused, moving Recent to D4 would ship Start without the design's "3 card buttons + Recent" (§11). That is a **design deviation**, and it needs its own Owner ruling after UX & A11y review. It is not a silent fallback |

### Guards over shared surfaces (GO14a)

| guard (owner) | root · recursion · token set · allowlist | other tracks it reads | jointly satisfiable with what the owner writes |
|---|---|---|---|
| `tools/check-named-tests.py <track>` (G0) | `docs/design/app-shell.md` §9 and §12.4, one file, no recursion. Tokens: backticked names, attributed to the first `(<track>` closed by `)`, `,` or `;` on the same line. `*` or `<…>` in a name fails. Also `.tmp-tests/*.log` (non-recursive): `PASS <name>`, any `FAIL`. For D3a, the inventory's ported-name column. Allowlist: none | every coding track | yes. No track edits the design. Each track writes tests with exactly its names. Renaming a test reddens at the join, which is intended |
| `Architecture_DockConfinedToShell` (D3a) | root `src/`, recursive `**/*.cs` text. Tokens: `Dock.` namespaces, plus `Avalonia` or `Dock` inside `CfdWorkbench.Core`. Allowlist: namespace `CfdWorkbench.Desktop.Shell` (`src/CfdWorkbench.Desktop/Shell/**`) | D1 (inside the allowlist), D2, C1, P1, G0 (must stay Dock-free) | yes. D2, C1, P1 and G0 are Dock-free by design. D3a's `MainWindow.axaml.cs` hosts Dock only through `ShellHost`. AXAML is not scanned. This is recorded, not widened |
| xaml-token-lint (D3a, D4) | `xaml-token-lint.py --root <repo> src/CfdWorkbench.Desktop`, recursive (`rglob`, `:154`), extensions `.xaml` and `.axaml` (`:57`). Tokens: raw colours, inline `SolidColorBrush` colours, raw dimension literals. Allowlist: `is_allowed_dimension` (`:64`) | D3a's new `Panes/*.axaml` are linted. `verify-application-adapters.py`'s corpus receipt globs top-level `*.axaml` only (`:628`), but the lint itself recurses | yes, if pane AXAML uses `Styles.axaml` tokens |
| `tools/check-spiral.py` (all, via `check-docs.py`) | `main..HEAD` commits. Tokens: the bookkeeping vocabulary. Fails when there are ≥ 12 commits, none touching `src/` or `tests/`, and ≥ 60 % bookkeeping | every branch | yes. This branch is at 11 docs commits (3 bookkeeping), 12 with this plan, and G0 is its first product commit. The pre-G0 docs commits (S8, R0) add at most 2, which gives 3 of 14 bookkeeping, so it stays green |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| brief's early "mixed-DPI dual-monitor OS-window float" spike track | superseded by the design's gates (inputs are final). S8 is the early time-boxed track that gates docking (G0). S2 and S6 need hardware this session lacks, so they go to OP-S. S1, S4 and S5 need Windows and are deferred |
| P1 as a third concurrent lane beside C1 and D1 | P1 feeds D3a: the Start card's Recent rows and File ▸ Open Recent come from `RecentList` and `PreferenceStore` (design §8 E7 "Recent files" row, §11 "Start card | 3 card buttons + Recent"). So P1 is on the M1.2a path, but only as an input to D3a. Running P1 in lane B after D1 keeps one Grok and one Agy at a time and bounds the join load. By budget, lane B (D1 70 min + P1 90 min) equals lane A (C1 90 min + D2 70 min). So whether P1 or D2 sets the path into D3a is **Inferred**; the measured durations decide it |
| a separate test-inventory track (§12.5) | tightly coupled to the port it governs; one coherent session is cheaper. It stays in D3a as checkpoint D3a-0 with a Coordinator review |
| splitting D3a into host and panes | both halves write `MainWindow`, `App`, the csproj and `WorkbenchTests.cs`. Splitting would create shared authored files, which is a wrong boundary |
| B6 restart now | the 2-cycle cap fired (section-editor §9). It is off the M1.2a and M1.2e path and needs an Owner ruling (M1.2c wave) |
| DAT-import A4.6 acceptance fix now | architecture §10.7 routes it to design-slice. The local-chord rule is a design decision (M1.2d wave) |
| a Sonnet track to render the HTML and derive the index | derived work; no coordination is needed (Artifact classes) |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Coordinator: `coord leader pin fbfa35dc`, then `coord leader who` (read the epoch back); `coord request expire` (clears the silent expiry) | seconds | the leader is expired. Every row carries the epoch (CO-L) |
| 2 | Owner (Fable): rulings O1 into `docs/notes/rulings.md`; OI-S1 request to the spec owner | one Owner session | decision edge for every track |
| 3 | `coord worktree new` for R0 and S8 → run both (Claude lane, width 2). Each tree runs `coord doctor` to read the inherited registration and never runs `coord install` or `coord classify` | 1 h wall — S8's box = 3× a measured same-class prior; no prior → 1 h, measured time recorded in the audit entry (Ruling 54); S8 measured 254 s. **Done** | S8 is the first node on the critical path. R0 must join before the first product merge |
| 4 | If S8 says "yes": the Owner rules and the Coordinator amends §3.4, V8 and `Snapshot_SplitGroup_RoundTrips` in the design. Join R0 and S8 | 15 min | G0 freezes the schema from this |
| 5 | G0 (Claude Opus) → join: the gates, then `coord doctor` in the integration tree | 2.5 h — box = 3× a measured same-class prior; no prior → 2.5 h, measured time recorded in the audit entry (Ruling 54) | freezes the interfaces (serial spine) |
| 6 | compile briefs → lane A **C1** (Grok) ∥ lane B **D1** (Agy) | 90 min | first parallel wave; disjoint files; no data or decision edge between them (GO5) |
| 7 | join C1 and D1 → lane A **D2** (Agy) ∥ lane B **P1** (Grok) | 90 min | D2 needs C1. P1 fills lane B's slack |
| 8 | join D2 and P1 → **D3a**: D3a-0 inventory → Coordinator review → D3a-1, D3a-2 | ≤ 3.5 h | joins D1, D2 and P1 (serial spine) |
| 9 | join D3a (three identical runs) → **U1a** + operator native session (M1.2 entry-gate AX proof) | 2 h + operator | M1.2a is done only when U1a passes with every native row run and attached, including the operator rows. It is never done because the app launched |
| 10 | later waves: M1.2b, M1.2c and M1.2d `/design-slice` runs with DR-9, DR-11 and DR-8 | per design | not planned here |
| 11 | after M1.2c merges: **D4** → OP-S S6 and S7 → **U1e** → M1.2e join (OI-S1 and the 15 M1.2b–d edges settled) | ≤ 3.5 h + operator | the design's dependency column |

**Critical path (M1.2a):** O1 → S8 (1 h) → G0 → (C1 → D2 ∥ D1 → P1) → D3a → U1a. Both lanes have a 160 min budget, so either can be the longer one. **M1.2e** adds M1.2c merged → D4 → OP-S → U1e.

**Join gates (run by the Coordinator on the track branch, then again on the integration tree after the merge):**
1. `tools/run-tests.sh` exit 0. D3a and D4 also need `dotnet restore --locked-mode` to exit 0.
2. Coding tracks (G0, C1, D1, D2, P1, D3a, D4) only: `python3 tools/check-named-tests.py <track>` exits 0 (G0 uses `--self-test`). After each merge, the integration tree re-runs this for **every track already merged**. The docs tracks (R0, S8, U1a, U1e) are gated by their own exit evidence and gate 3. OP-S is gated by the operator's attached rows.
3. `python3 tools/check-docs.py` exit 0, which includes `tools/check-spiral.py`.
4. `git log` and `git status` show the claimed commit, and the final message holds the brief's Return section.
5. D3a and D4 only: `run-tests.sh` three times with identical `PASS` sets; every named suite's PASS set non-empty (Ruling 54).
6. C1: reviewers first check that a `dimension` receipt reopens and that `Recovery_Dimension_Refused` holds (EDIT-KIND-REOPEN).
7. A suspect merge is probed in a throwaway worktree at the pre-merge commit before a repair is delegated.
8. At the 2-cycle cap: stop the track and report to the operator.
9. App gates (`verify-application-core.py`, `verify-application-adapters.py`) and the recounts run in the readiness ring (`tools/run-readiness.py`) before a merge to main, not at every join; `xaml-token-lint` stays in the fast ring (Ruling 54).

**Cost (honest):** the orchestrator-worker shape costs about **15×** a single session (GO6). What that buys here is
**context hygiene**: the Core, Persistence and Desktop tracks each fit one context, while the whole build does not. It
also buys **harness isolation**: an Agy stall cannot touch a Grok tree. And it buys real **independence** in steps 6
and 7 (disjoint files, no data or decision edge). It does not buy speed on M1.2a's critical path. That path is serial
through G0, the two lanes and D3a. The lanes run D1 and P1 beside C1 and D2, instead of after them. The alternative is one serial session for the
whole shell. The author rejects it because that session would carry Core numerics, persistence and Avalonia in one
context, which is class CTX-A. **The track count is not settled.** The Tech Lead was not convened (the run's fan-out cap
was 2, and those two slots went to the Test Architect and the Simplifier). The casting vote goes to the Owner in O1.

## Gate record

`GATE plan · 2026-09-27 · test-architect (hard), the-simplifier (soft) · verdict: PASS-WITH-CONDITIONS · vetoes →
resolution: Test Architect cleared on re-review; Simplifier cleared on re-review (R0 kept by written rationale: the brief
requires it)`. Each lens ran as a separate agent in Adversary Mode. The author cleared no veto. Repair cycles used: **1 of 2**,
and the residual Minors from the re-reviews were applied without another cycle. The Tech Lead was not convened, so the
track count goes to the Owner in O1.

| Lens | Round 1 | Main findings | Repair | Final |
|---|---|---|---|---|
| Test Architect (hard veto) | PASS-WITH-CONDITIONS, 5 Major, 1 Minor | S8 fallback claimed a result nobody observed; D3a lacked the red-first Dock check, `--locked-mode` and per-row red runs; U1a could close M1.2a with operator rows open; no integration-tree re-run; gate 2 applied to docs tracks | all applied (S8 row, D3a evidence, U1a and step 9, join gates 1–2) | **Cleared**. Residual Minor: P1 is on the path into D3a (critical path rewritten) |
| Simplifier (soft veto) | **SOFT VETO**, 3 Major, 1 Minor, 1 Nit | R0 unneeded; fallback chains exceeded the cap; P1 consumer misread (Recent rows in D3a); dead join gate 7; impossible seam 1 fallback | R0 trimmed and kept by the brief's rationale; one fallback per track inside the cap; seam 7 and the P1 → D3a edge; gate 7 deleted; seam 1 fallback rewritten | **Cleared**. Residuals applied: OI-S1 gated by O1, not R0; the seam 7 refusal is a design deviation that needs a ruling |

## Status

| | |
|---|---|
| **Completed** | Layer state measured (`coord doctor`, `pack-doctor`); `coord regen` cleared the owed artifact; artifact classes recorded; the design's tracks scheduled into a serial spine, two lanes and gated later waves with owners, budgets, exit evidence, harness and model, DR gating, seams, GO14a guards and struck tracks; a cross-tree write by `coord classify init` found and reverted |
| **R0** | Done. Merge commit `06a5bf0` |
| **S8** | Done. Measured 254 s (Ruling 54). Merge commit `3e47767` |
| **Remaining** | O1 rulings; leader pin and request expiry; every track; OP-S; S1, S4 and S5 (Windows); M1.2b–d designs; registering the classify cross-tree class |
| **Best next action** | Step 1, then the Owner's O1 rulings, then dispatch S8 and R0 |

## Planned vs actual (M1.2a wave, 2026-09-27 → 2026-09-30)

Measured from each dispatch's recorded start to its branch's last commit (Coordinator, `git log -1 --format=%cI`).
Every result below was re-verified by the Coordinator on the track branch (three `run-tests.sh` runs with identical
PASS-set hashes for Desktop tracks, named checks, docs gate) before its join; no track was accepted on its own report.

| Track | Harness · model | Box | Measured | Outcome |
|---|---|---|---|---|
| R0 | Claude · Sonnet 5 | 45 min | — | Done; merged `06a5bf0` |
| S8 | Claude · Opus 5.5 | 1 h | 254 s | Done; verdict "no" — design `groups` shape stands |
| G0 | Claude · Opus 5.5 | 2.5 h | 642 s | Done + 1 repair (portable text I/O); spawned-suite red proof moved to D1 (classifier refused the plant) |
| C1 | Grok · grok-4.7 | 90 min | 30 min | Done, first pass |
| D1 | Agy · gemini-3.8-flash-high | 70 min | 21 min | Done, first pass; its red run closed G0's proof |
| D2 | Agy · gemini-3.8-flash-high | 63 min | 35 min | Done, first pass |
| P1 | Grok · grok-4.7 | 90 min | 45 min | Done, first pass |
| NEWFOIL (added: operator decision) | Grok · grok-4.7 | 90 min | 27 min | Done; restarted once when the operator changed the shape to near-elliptic AR 10 |
| D3a-0 inventory | Claude · Opus 5.5 (deviation from Agy) | 45 min | — | Done; 2 questions to the operator (Ruling 55) |
| D3a dispatch 1 | Agy | 70 min | 70 min (timeout) | **Failed — HARNESS-SILENT-EXIT** (background tasks despite the brief; no commit, no Return); partial work salvaged to `d3a-salvage-agy1` |
| D3a dispatches 2–3 | Codex · gpt-6-sol (fallback) | 70 min each | 20 / 47 min | Green subset, 29/40 named |
| D3b dispatches 1–2 (operator-authorized follow-on) | Codex · gpt-6-sol | 70 min each | 43 / 40 min | 40/40 named, 60/60 inventory ports |
| THEME (split from D3b at its repair cap) | Claude · Opus 5.5 | 90 min | — | Dock dark-tab contrast 1.19 → 5.29; 1 repair (flaky probe caught by Coordinator re-run) |
| U1a | Claude · Opus 5.5 | 2 h | — | Copy rows; native rows operator-run; a11y veto held (dead buttons, focus) |
| U1FIX / COPYFIX / OPENFIX | Codex / Claude / Codex | 70 / 60 / 45 min | — | All findings fixed with red-first tests; UI-DEAD-CONTROL control added |
| COREGATE, TESTCI, crash investigation | Claude · Opus 5.5 | — | — | Joins unblocked; core gate 302 s → 88 s; run-tests 67 s → 29 s |

**What paid:** the two parallel lanes (C1 ∥ D1, then D2 ∥ P1) each finished in under half their box. The
Coordinator's independent re-runs caught one flaky test and one "green" report that was not green, and the U1a
review caught dead controls that 40 green named tests missed.

**What did not:** boxes were 2–3× the measured time (Ruling 54's "3× a measured prior" now has priors). Agy is not
fit for a long UI track (second HARNESS-SILENT-EXIT). The prompt compiler has no Grok/Agy templates, so those briefs
were hand-compiled. The leader lease (900 s TTL) expired between joins and was reclaimed at every join.

**Open at the end of the wave:** the operator's native session (`docs/reviews/app-shell-native.md` §4) and the
ux-accessibility re-review after it; M1.2a is not done until both pass. M1.2b–d need their own `/design-slice`
(M1.2c includes the B6 restart for DR-11); D4/M1.2e waits on M1.2c.
