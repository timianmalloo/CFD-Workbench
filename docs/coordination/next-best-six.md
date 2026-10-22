---
id: coordination-next-best-six
title: "Coordination plan - the six next-best items of the 2026-10-21 stock-take"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: design-area3-analysis, rel: implements }
  - { to: design-windows-native-store, rel: implements }
  - { to: design-next-cad-increment, rel: relates-to }
  - { to: plan-application-build, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-21"
summary: >-
  Six items from the 2026-10-21 stock-take in one bounded wave on the ARM64 laptop: one ruling (DR-6 moot), two
  measurements (the application verify gates, the Browser scroll and Properties-view diagnostics), one qualification
  track (W-2 B2, the Windows project store) and two product slices (A3d Compare, Add station), each designed before it
  is built. Opus holds the Owner and leader seats; Sonnet sub-agents code where the task allows; Fable runs the pack
  mechanics. Width cap 3, serial spine first.
---

# Coordination plan: the six next-best items

**Input:** the compiled prompt `al-01M5GHJEFB7S4ZVRY0P6V32MDF` (raw `al-01M5GHFCPXPCNYRAPDMYQWVK10`, dispatchable, no
decision request). Its goal state is this plan's goal state; its *Not in scope* is the interdiction (every other
stock-take item). **Tier T2; fan-out cap 3; context ceiling 400,000 tokens per sub-agent; main-line budget 400 tool
calls.** Git before: `876f3a88` on `main`, clean apart from the audit log entries this skill wrote.

**Seats.** Owner = a Claude Code **Opus** sub-agent, session name `opus-owner`; it rules decision requests into
`docs/notes/rulings.md` (class `register`) with `coord-decide.py rule next --request <id>` under `AGENT_SESSION=opus-owner`,
and never authors track code. Leader and Coordinator = a Claude Code **Opus** sub-agent, session name `opus-leader`;
it writes every brief, reads every Return, and decides every join. The pack mechanics (worktrees, dispatch, joins,
commits, pushes) run in the main Fable session `pc-laptop-leader`, which acts on the leader's written decisions and
never substitutes its own. Coders are **Sonnet** sub-agents unless a row says otherwise. The operator is
`operator-timianmalloo` and is not in the loop during this wave (the prompt says "work autonomously").

## Layer state

Measured with `coord doctor` and `pack-doctor.py --json` on 2026-10-22 in the primary checkout, before and after `coord install`.

| check | result | meaning |
|---|---|---|
| registry | ok, 7 patterns | `.agents/artifacts.yml` exists; the managed block's two `derived` commands were run by `classify init` when it was written (its header says so); the four hand entries below it are `derived` (docs index) and `register` (change log, coordination logs, requests) |
| merge driver, before | NOT EFFECTIVE (`COORD-DRIVER-NOT-EFFECTIVE`): `.gitattributes` declares `coord-regen`, `coord-register`, `defect-register`; none registered in this clone | this laptop clone had never run `coord install`; derived and register files would have conflicted normally |
| merge driver, after `coord install` | `coord-regen` and `coord-register` registered (`git config --get-regexp 'merge\..*\.driver'` shows both); `defect-register` still unregistered, so the doctor still prints NOT EFFECTIVE for that one pattern | `docs/lessons/defect-classes.md` is the only `defect-register` path; `tools/merge-defect-register.py` exists (check-docs runs its self-test) but nothing registers it as a git driver. Serial-spine item S4 below registers it before two tracks can both append a lesson |
| pre-commit floor | installed (`.git/hooks/pre-commit` runs `coord-core.py precommit`) | the commit boundary is enforced in every worktree of this clone (a linked worktree shares `.git/hooks`) |
| edit-boundary hook | **not installed** in `.claude/settings.json` (no `coord-core.py hook` entry) | the Claude edit boundary is **observed-only** here, not enforcing; the doctor's "enforcing" line is a 2026-08-24 spike, not this machine |
| leader | released, epoch 5 | nobody holds the leader ref; order-of-operations row 1 pins it |
| heartbeat | 1 session beating 808,120 s ago; 0 live, 1 done | the previous `pc-laptop-leader` session ended; no live session holds a tree |
| requests | FAIL `COORD-REQUEST-SILENT-EXPIRY`: `req-01M5DXHY5QN4YJCAF1MD5K60GT` (DR-6) past its deadline with no outcome | track R1 rules it; a request never ends in silence |
| lease overlap | none (0 live leases) | nothing to untangle |
| worktrees | only the primary (`D:/Projects/CFD-Workbench` at `876f3a88`) | every track creates its own |
| pack-doctor | 18 PASS, 3 WARN (python3 alias, knowledge-graph stale nodes, coordination `.gitattributes` vs registration), 1 FAIL (the request above) | the three WARNs are known and not this wave's work |

The layer is installed **once, in the primary checkout**. Every worktree this plan creates **inherits** the registration and the pre-commit floor through the shared `.git`; a track runs `coord doctor` inside its tree to read that back and never runs `coord install` there.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `audit-log.py render` (merge=coord-regen) | **none** |
| `docs/docs-index.js` | derived | `docs-graph.py derive` (merge=coord-regen) | **none** |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | append-only, union merge (coord-register) | **none** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | union merge (coord-register) | **none** |
| `docs/notes/rulings.md` | register | append-only numbered headings; written only by the Owner seat through `coord-decide.py rule` | **none** beyond "one writer, the Owner" |
| `docs/lessons/defect-classes.md` | register (`merge=defect-register`) | `tools/merge-defect-register.py`, **not registered as a driver in this clone** | serial until S4 registers it; after that none |
| `src/CfdWorkbench.Persistence/ProjectStore.cs`, `WindowsNative.cs`, new `WindowsProjectStore.cs`, `CfdWorkbench.Persistence.csproj` | authored | one owner | W2 only |
| `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs`, `WindowsProjectStoreTests.cs`, the `MacOnlyStoreChecks` set in `IdentityTests.cs` | authored | one owner | W2 only |
| the `MacOnlyStoreChecks` set in `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs`, the two `IsMacOS` guards in `tests/CfdWorkbench.Cli.Tests/CliTests.cs` | authored | one owner | W2 only (lines, not files: see Seams) |
| `src/CfdWorkbench.Analysis/*.cs` (Compare, Discrepancy), `src/CfdWorkbench.Core/RunRecord.cs` (Discrepancy record), `src/CfdWorkbench.Cli/Program.cs` (`inspect --runs`), `src/CfdWorkbench.Desktop/Analysis/*` | authored | one owner | A3 only |
| `src/CfdWorkbench.Core/FoilSource.cs`, `AuthoringSession.cs`, `Geometry.cs`, `StationNames.cs`; `src/CfdWorkbench.Desktop/PlanCanvas.cs`, `ElevationView.cs`, `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `Panes/BrowserPane.axaml(.cs)` | authored | one owner | S1 only |
| `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `PropertiesView.cs` | authored, **contested** (A3 needs the Compare entry and freshness hook; S1 needs the Add station verbs) | one owner at a time | serial: A3 lands first, S1 rebases (Seams) |
| `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`, `BrowserPane.axaml(.cs)` scroll behaviour | authored | one owner | D1 only (S1 touches BrowserPane for the station row; D1 lands first) |
| `docs/design/*.md`, `docs/mockups/*.html`, `docs/proof/<track>/**`, `docs/coordination/next-best-six.*` | authored, per track | one owner per file | per track |
| `DESIGN.md` §7 COPY rows | authored, **contested** (A3, S1, W2 all add copy) | one owner at a time; each track appends its own id range stated in its brief | serial by id range: W2 takes the next free ids first, then A3, then S1 (Seams) |
| `AGENTS.md`, `tools/run-tests.sh`, `.github/workflows/tests.yml` | authored | one owner | W2 only (the skip count line, if any) |

## Tracks

Width cap **3**. The multiplier: the pack's reference measurement is roughly 15× tokens for an orchestrator-worker shape (GO6); a Sonnet coder costs less per token but the shape is the same. Every track below names why it pays.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **R1 rule DR-6 moot** | `docs/notes/rulings.md` (one appended heading, the next free number), `.agents/requests.jsonl` | none | T0 | 0 | 10 calls · 20k tokens · 10 min | `coord-decide.py list` shows 0 open requests; `coord doctor` requests line PASS; `verify-ruling-citations.py` green. Reason recorded: Ruling 247 retired the F1 packet whose attempt 4 raised DR-6 | Opus Owner sub-agent (`opus-owner`); the main session commits the register |
| **V1 verify gates** | `docs/proof/verify-gates-2026-10-22/README.md` (the two receipts and exit codes); no `src/`, `tests/` or `tools/` change | none | T0 | 0 | 30 calls · 60k tokens · 40 min (each gate publishes once, about 1.6 GB scratch) | both gates run from the primary checkout with their full stdout and stderr tails in the receipt. Expected and acceptable: `verify-application-core.py` exits **4, "Windows process ownership adapter: Not assessed"** (its own line 191; the adapter is Mac-only by design), recorded as Not assessed, not green. Any other exit is a finding with the log. `verify-application-adapters.py` likewise | Sonnet sub-agent, Bash only |
| **D1 desktop diagnostics** | `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs` (a new check `BrowserRow_BelowFold_FocusBringsIntoView_AtScales` at RenderScaling 1.0, 1.5 forced, 2.0), `src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml(.cs)` (the product fix, only if the new check is red at the real scale), `docs/proof/tmx/windows-2.0.md` (the Ruling 221 print at this laptop's 200 %), `docs/proof/pvh/README.md` (the Properties-view loop result) | none | T1 | 0 | 90 calls · 250k tokens · 2 h | (a) `CFD_DIAG_SCROLL=1` run of `--shell-window --part=1/2` and `2/2` at 200 % with the `DIAG-SCROLL` lines committed; the ruling's three-scale check added, **red first on the product** if the scroll falls short (the 2026-10-18 fixture note recorded BringIntoView short by one row at scale 2), then green; `bash tools/run-tests.sh` TESTS-GREEN. (b) `--properties-view --part=2/2` run 5 times with `CFD_TEST_CHILD_TIMEOUT_SECONDS=120`; either a `FAIL TIMEOUT … in-flight check: <name>` line with that check named in the proof, or "not reproduced in 5 runs" recorded. Ruling 221's "150 percent on the PC" clause is satisfied at 200 % by Ruling 238 (the laptop is the Windows evidence); the proof says so | Sonnet sub-agent for (b) and the test; Opus leader reviews the product fix before join |
| **W2 Windows store (W-2 B2)** | `src/CfdWorkbench.Persistence/ProjectStore.cs` (Windows dispatch in `Supported()` and the save/open/recover entry points only; every Darwin body byte-unchanged), new `WindowsProjectStore.cs`, `WindowsNative.cs`, `CfdWorkbench.Persistence.csproj`; `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs`, `WindowsProjectStoreTests.cs`, the `MacOnlyStoreChecks` sets and the two Cli guards (deleted or emptied); `src/CfdWorkbench.Desktop/MainWindow.axaml.cs` Save/Save As folder default and OneDrive refusal dialog (Rulings 135, 146, 160); `DESIGN.md` §7 COPY-447 release and the Ruling 146 rows if absent; `docs/design/windows-native-store.md` §§3, 4, 6 erratum (Ruling 136); `docs/proof/win-store-implementation/2026-10-22/**` | R1 (a clean request board), S4 | T2 | 0 | 2 repair cycles (Ruling 145 cap) · 250 calls · 600k tokens · 6 h | `ProjectStore.Supported()` admits Windows (NTFS, x64 or ARM64) through `WindowsNative`; the 51 `macos-only-store` SKIP lines are gone and every former skip is PASS on this laptop (`bash tools/run-tests.sh` prints 0 skipped for Core and Cli; Desktop's former skips PASS); the Ruling 145 two cases and the historical probe `WindowsNative_Replace_HeldReaderKeepsOldImage` disposition are stated (PASS, or listed with the ruling that excuses it); CI green on both runners (macOS unchanged at 811/0); a crash-injection case measures the leftover inventory (Ruling 190); `check-docs` exit 0 | Sonnet sub-agent for the adapter and tests; **Opus** for the `Supported()` admission review and the Data & Persistence veto (Adversary Mode) |
| **A3 A3d Compare** | design: `docs/design/a3d-compare.md`, `docs/mockups/a3d-compare.html`; code: `src/CfdWorkbench.Core/RunRecord.cs` (the `Discrepancy` record: two run ids, per-point δ normalised to the base, reference-quantity compatibility, never averaged), `src/CfdWorkbench.Core/AuthoringSession.cs` (store and read Discrepancy records beside runs; `cfdw-project-2` stays the format since the record is additive and omitted when absent), `src/CfdWorkbench.Analysis/Compare.cs` (new), `AnalysisProjection.cs` (the Compare group), `Labels.cs`; `src/CfdWorkbench.Cli/Program.cs` (`compare <a> <b>` or `inspect --runs` extension, one of them, named in the design); `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml(.cs)` (new) and the Compare entry in `AnalysisPanel.axaml(.cs)`; the Compare command rows in `WorkbenchController.cs` and `Shell/CommandTable.cs`; tests `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs`, Desktop `--analysis` additions; `DESIGN.md` §7 COPY rows | none for the design; the build waits for D1's `ShellWindowTests.cs` join if A3 adds a theme row there | T2 | 1 (one Adversary review: hydrodynamicist on the delta semantics, Test Architect on the mutants) | design 60 calls · 200k · 2 h; build 200 calls · 500k · 5 h | the design's three named tests from area3-analysis.md §13.3 row ANA-06 PASS: `Compare_TwoTiers_DeltaAndDiscrepancyRecord`, `Compare_SameKeyTwice_ReadNotNewRecord`, `Compare_EstimatorE_ExcludedFromEDeltas` (mutant: estimator e = 1 enters the delta → red); the spec row ANA-06's four clauses each have a test; the mockup renders the hard states (no second run, incompatible reference quantities, Historical base); `run-tests.sh` TESTS-GREEN; `check-docs` and `xaml-token-lint` clean; the Desktop `--analysis` suite shows the Compare pane rendered | Sonnet sub-agent for code; **Opus** writes the design and runs the Adversary pass; the operator reviews the mockup afterwards (assumption #2 of the compiled prompt) |
| **S1 Add station** | design: `docs/design/add-station.md`, `docs/mockups/add-station.html`; code: `src/CfdWorkbench.Core/FoilSource.cs` (an `AddAssignment(source, eta, profileName)` source patch beside `MakeIndependent`; `RemoveAssignment`), `AuthoringSession.cs` (`AddStation(eta)` promoting the inspection slice under rule A: the blend at η fitted as a compatible-spacing profile, deviation reported, ≤ 1e-12 relative when the neighbours share spacing; `RemoveStation` refit with the measured deviation; root and tip refused), `Geometry.cs` (no new authority: the blend evaluator is read, never duplicated), `StationNames.cs`; `src/CfdWorkbench.Desktop/PlanCanvas.cs` and `ElevationView.cs` (the inspection-slice chip with Promote), `Panes/BrowserPane.axaml(.cs)` (the new station row), `Shell/CommandTable.cs` and `NativeMenuBuilder.cs` (`station.add`, `station.remove`), the verbs in `WorkbenchController.cs` and `PropertiesView.cs` (after A3's join); tests `tests/CfdWorkbench.Core.Tests/StationTests.cs` (new), Desktop `--plan-canvas` and `--controller-shell` additions; `DESIGN.md` §7 COPY rows | design: none; build: **A3 joined** (WorkbenchController and PropertiesView), **D1 joined** (BrowserPane) | T2 | 1 (one Adversary review: computational-geometry lens on the promotion exactness and the blend-certificate budget, Ruling 73) | design 80 calls · 250k · 3 h; build 250 calls · 600k · 6 h | CAD-02, CAD-09 and GEO-06 each have a named test, including: promote at η 0.35 gives an authored station whose section equals the blend within 1e-12 relative (red first by asserting the exact value before the promotion code exists); a coincident η is refused and the existing station selected; root and tip removal refused; remove reports the refit deviation and Apply is disabled above the acceptance; undo of add and remove is one step; save, reopen and CLI `inspect --json` show the new station; the Plan chip, Browser row and Properties all show it (the E7 surface list); `run-tests.sh` TESTS-GREEN; `check-docs` clean | Sonnet sub-agent for code; **Opus** writes the design and runs the Adversary pass; operator reviews the mockup afterwards |

### Track detail

Every track ends at its exit evidence, or at the **2-cycle repair cap** (a cycle is a dispatch that returns without the
evidence; a missing Return block or a branch without its commit counts as a failed cycle, HARNESS-SILENT-EXIT). At the
cap the leader records the track blocked with its exact pending edge and the main line continues with the others; no
third cycle without an Owner ruling. Seam requests go to the leader with `coord request add` (deadline and fallback
required). Decision requests go to the Owner with `coord decide request --to opus-owner --deadline default --fallback <text>`.
Every row carries the leader epoch that order-of-operations row 1 reads back (`assume:` 6, which is 5 + 1 after
`coord leader pin opus-leader`; `coord leader who` confirms). Doorbell for every row: **not recorded** (pack-doctor's
`doorbells` line).

| track | owner seat | rules (Owner) | seam requests expected | deadline (wall from dispatch) | fallback | lane |
|---|---|---|---|---|---|---|
| R1 | `opus-owner` | itself (it is the ruling) | none | 10 min | the main session records `coord request expire` with the request's own fallback, and the board is clean either way | Claude |
| V1 | Sonnet sub-agent | `opus-owner` | none | 40 min | a gate that cannot publish (disk, SDK) is recorded "not run: <reason>" with the log; never retried blind | Claude |
| D1 | Sonnet sub-agent (b, test); Opus leader reviews the product fix | `opus-owner` | D1 → S1: BrowserPane row shape, if the product fix changes the row control (S1 rebases) | 2 h | the diagnostic prints are committed even if the product fix does not land; the three-scale check stays red-listed with its ruling citation, never muted | Claude |
| W2 | Sonnet sub-agent; Opus Adversary review | `opus-owner` | W2 → A3 or S1: none expected (disjoint files). W2 → leader: if `Supported()` needs a guarantee the design calls unmeasured (handle identity across rename, sharing), a decision request names the measurement and its number | 6 h | at the cap: the Windows path stays closed, the measured gap is recorded in `docs/proof/win-store-implementation/2026-10-22/`, the skips stay, and a decision request names what would open it | Claude |
| A3 | Opus (design) then Sonnet (code) | `opus-owner` | A3 → D1: a theme row in `ShellWindowTests.cs` (after D1 joins). A3 → S1: the `WorkbenchController` command block boundary | design 2 h, build 5 h | design that fails the Adversary pass twice: recorded, no build. Build at the cap: the Core and Analysis half (Discrepancy record, Compare, CLI) joins alone if green; the pane waits | Claude |
| S1 | Opus (design) then Sonnet (code) | `opus-owner` | S1 → A3 (joined): `WorkbenchController` verbs; S1 → D1 (joined): BrowserPane row | design 3 h, build 6 h | as A3: the Core half (`AddAssignment`, `AddStation`, `RemoveStation`, tests, CLI visibility) joins alone if green; the Plan chip and Browser row wait for a continuation | Claude |

### Harness qualification (from what was verified here)

| harness · version | needed from the delegation mechanism | edit boundary | dispatch | basis |
|---|---|---|---|---|
| Claude Code Agent tool, `opus` and `sonnet` model overrides (this session, Fable 5.1 main line) | a brief, exclusive paths, a Return block with the commit and the oracle run, an audit entry | **observed-only**: `.claude/settings.json` carries no `coord-core.py hook` entry, so nothing refuses an off-path edit at tool time; the pre-commit floor (`coord-core.py precommit`) enforces at the commit boundary when `AGENT_SESSION` is set in the sub-agent's shell | observed-only: the leader reads `git log`, `git status` and the Return block in the worktree | `coord doctor` and `.git/hooks/pre-commit` read on 2026-10-22 |
| `coord-runner.py --launch` (ACP workers) | not needed | not exercised | **unsupported** in this wave: not qualified on this machine | the execute skill's `reference/launch.md` says "profile-qualified"; no profile exists for the laptop |

Every brief is a prompt compiled for Claude Code with `/compile` (CO-S0). A brief states Owner rulings as settled facts
with the ruling number, never as `DR-n` lines (the pack sets `dispatchable` false on any decision request).

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0 pin the leader and start the sessions (`coord leader pin opus-leader`; `coord session start` for `opus-owner`, `opus-leader`, `pc-laptop-leader`) | every track row carries the epoch; a request addressed to an unstarted Owner session has no ruler (D5) | main session |
| R1 before W2 | the doctor's FAIL on the request board is the only red on the layer; W2 is the first track that may raise a decision request, and the board must show the Owner answering, not an expired request beside it | Owner |
| S4 register the `defect-register` merge driver in this clone (`git config merge.defect-register.driver "<python> <repo>/tools/merge-defect-register.py %O %A %B %P"`, the argument order its usage line states) and prove it on a two-branch append in a scratch repo | D1, W2, A3 and S1 may each append a lesson to `docs/lessons/defect-classes.md`; with the driver unregistered, two appends conflict at the second join. The first commit after registering it is verified (the driver is effective) before two tracks rely on it | main session, before W2 or A3 dispatch |
| A3 design before A3 build; S1 design before S1 build | GO5(b): until the Discrepancy record's shape and the station-promotion contract are fixed, every coder's result changes every other file's shape. A design that fails its Adversary pass does not dispatch a build | Opus |
| A3 build before S1 build in `WorkbenchController.cs` and `PropertiesView.cs` | one owner per file; both tracks add verbs there. A3 is smaller and lands first; S1 rebases on the joined controller | leader |
| D1 before the S1 build in `BrowserPane` | the scroll fix, if any, changes the row control S1 adds a row to | leader |
| W2's `Supported()` change before any W2 test un-skip | the un-skipped checks exercise the Windows path; un-skipping first makes 51 reds that say nothing | W2 |
| `.agents/log` chore commit and push after every join; `verify-push.sh` PUSH-OK | the pack's node discipline; a join without its ledger twin fails `pack-doctor` mail twins | main session |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| W2 -> Desktop `WorkbenchTests.cs` and Cli `CliTests.cs` | delete the `MacOnlyStoreChecks` set and the two `IsMacOS` guards (lines, in files no other track owns in this wave) | W2 owns those lines; D1 owns `ShellWindowTests.cs` only, so no overlap |
| W2 -> `DESIGN.md` §7 | COPY-447 release and any Ruling 146 row still missing; W2 takes the next free ids first | W2 states its id range in its Return; A3 and S1 start after that range |
| A3 -> `WorkbenchController.cs`, `CommandTable.cs` | Compare command rows | A3 owns them until its join; S1 rebases |
| S1 -> `WorkbenchController.cs`, `PropertiesView.cs`, `BrowserPane` | station verbs and the station row | S1 after A3 and D1 join |
| A3, S1 -> spec `docs/specs/cfd-workbench-v1.md` | an amendment row (ANA-06 wording of "normalised to the base value" if the design chooses a different base; CAD-02 "explicit thickness command policy" default) | written as a `docs/specs/amendments/` note by the design, ruled by the Owner before the build |
| any track -> `docs/lessons/defect-classes.md` | a lesson that changed a tool or test | append after S4; one entry per class, the control named |
| any track -> Owner | a decision the brief does not settle | `coord decide request --to opus-owner`; the track stops until `Ruling NN` |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| a separate "spec amendments" track | three lines in two spec rows; each design writes its own amendment note and the Owner rules it. A fourth session for that is pure multiplier |
| a separate "CI" track for W2 | the W2 exit evidence already includes CI green on both runners; a second session watching CI adds nothing |
| D1 split into two tracks (scroll, Properties-view) | both run the same Desktop harness on the same machine and share one proof folder; two sessions would contend for the display and the build servers |
| a Compare CLI verb as its own track | it reads the same Discrepancy record; splitting it from A3 fails the coupling test |
| the Windows x64 Intel rerun, the fluids spikes, Areas 1/4/5/6, CAD export, the 2D workbench | out of scope by the compiled prompt |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | `coord leader pin opus-leader`; `coord session start` for the three seats; `coord leader who` read back (epoch 6 expected) | 5 calls | every later row cites the epoch; no track before the seat exists (CO15) |
| 2 | R1: the Owner rules DR-6 moot (the next free ruling number); commit the register; `coord doctor` requests PASS | 10 calls | the only red on the layer; cheapest item; frees W2 |
| 3 | S4: register `defect-register`; prove it on a scratch two-branch append; `coord doctor` merge-driver line clean | 10 calls | before any two tracks can both record a lesson |
| 4 | dispatch **V1** and **D1** in parallel (width 2): both are measurements on the primary machine; V1 needs no worktree write beyond its proof folder, D1 takes a worktree | V1 40 min, D1 2 h | independent (no data or decision edge, no shared file); both are the stock-take's cheapest open items |
| 5 | Opus writes the **A3 design** and mockup in a worktree (width 3 with V1 and D1) | 2 h | the design has no dependency; writing it while the measurements run fills the third slot |
| 6 | join V1; join D1; chore commits; push; PUSH-OK | 20 calls | frees two slots |
| 7 | dispatch **W2** (Sonnet) and the **A3 build** (Sonnet) in parallel; Opus writes the **S1 design** in the third slot | W2 6 h, A3 5 h, S1 design 3 h | W2 and A3 touch disjoint files; the S1 design needs no tree of its own beyond its docs |
| 8 | A3 Adversary pass (Opus) at its Return; join A3; W2 Adversary pass (Opus, Data & Persistence veto) at its Return; join W2; each with chore commit, push, PUSH-OK, and CI read with `gh run watch` | 40 calls | the joins are serial by the pack's design |
| 9 | dispatch the **S1 build** (Sonnet) on the joined main; S1 Adversary pass (Opus, computational-geometry lens); join S1; push; CI | 6 h | the last track depends on A3 and D1 joined |
| 10 | planned-vs-actual table appended to this plan; `docs-graph.py derive`; the execute skill's audit entry with one `--agent-run` per track; memory note updated | 10 calls | the record the next plan is better for (GO19) |

## Gate record

| gate | result | by |
|---|---|---|
| Simplifier (soft veto: every track earns its multiplier) | cleared: six items became six tracks only because R1 and V1 cost less than a session each (R1 is one command; V1 is two commands with a receipt); the four struck tracks are listed | author, Peer Mode; the Opus leader re-reads at step 1 |
| Test Architect (hard veto: a track with no exit evidence is not a track) | every row names its evidence, including the red-first clauses for D1 (product fix), A3 (the e = 1 mutant) and S1 (the exact promotion value) | to be cleared by the Opus leader, not the author, at step 1 |
| Data & Persistence Architect (veto on the store) | W2's admission review is an explicit Opus Adversary pass before its join; `Supported()` keeps the Darwin path byte-identical | at W2's Return |
| Tech Lead (casting vote on count) | three concurrent at most; the serial spine holds the contested files | author |

## Status

| | |
|---|---|
| **Completed** | layer measured and installed in this clone (merge drivers for coord-regen and coord-register registered); the compiled prompt consumed; artifact classes recorded; six tracks with owners, paths, budgets and exit evidence; serial spine; seams; struck tracks; order of operations; this plan in md and html |
| **Remaining** | `/execute-with-coordination` on this plan: pin the leader, start the seats, rule DR-6, register `defect-register`, then the dispatch order above |
| **Best next action** | `/execute-with-coordination next-best-six` |
