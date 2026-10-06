---
id: coordination-round-oct06
title: "Coordination plan - Mac round of 2026-10-06 (copy fixes, adaptive panels, DX, group move, 1280x800 layout, hook, spec 1.7.5)"
type: plan
status: completed
owner: "@cfd-leader-14e5e8d5"
tags: [coordination, worktrees, parallelism, analysis, a3b, a3c, dx, group-move, layout, spec]
links:
  - { to: rulings, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: coordination-two-machine, rel: depends-on }
  - { to: coordination-round-oct05, rel: relates-to }
  - { to: coordination-pc-kickoff, rel: relates-to }
review-by: "2026-11-06"
summary: >-
  Schedules the Mac half of Rulings 101-108: a docs-first copy and spec commit (DOC) that removes DESIGN.md contention,
  the A3a copy and display fixes (CPY), adaptive panels (PNL), the DX section and polar build, group move (Core then
  Desktop), the 1280x800 four-view layout behind an operator pick, and the heredoc hook inline. At most 3 coding tracks
  and 2 heavy test runs at once; the PC's W-0..W-5 run on their own machine and reach main only as reviewed PRs.
---

# Coordination plan: the Mac round of 2026-10-06

- **Scope:** the seven items the operator approved on 2026-10-06 ("go"), from Rulings 101, 103, 104, 107 and 108, and the
  spec amendments they carry, plus reviewing the PC session's pull requests (Ruling 106). Not in scope: the PC's W-0..W-5
  ([pc-kickoff.md](pc-kickoff.md)); the open section-editor flake (SECTION-EDITOR-LOAD-FLAKE); export, backend, signing.
- **Seats:** Leader and Coordinator = this Claude Code session `14e5e8d5` (Opus 5.5). Owner = a Fable sub-agent
  (`claude-fable-5-1`), one at a time, ruling into `docs/notes/rulings.md` and noting "ruled by the Fable owner"; it also
  reviews PC pull requests. Held for the operator: copy and spec wording, a mockup or layout pick, anything irreversible
  or public. Coders: Sonnet 5.5 sub-agents (they carried every build last round after Codex and Agy each failed once);
  Codex `gpt-6-sol` is the fallback with `< /dev/null`.
- **Base:** `main` = `origin/main` = `dbdfa561` (readiness green, 128.7 s of 240 s). Planning tree
  `CFD-Workbench-chore-coordination-round-oct06`.

Labels: **Verified** = observed in this run; **Inferred** = reasoned, not observed; **Flagged** = observed but suspect.

## Layer state

`coord-core.py doctor`, run in the planning tree on 2026-10-06.

| check | result | meaning |
|---|---|---|
| registry | ok - 9 patterns | `.agents/artifacts.yml` now also classifies `docs/coordination/xmsg.jsonl` (Verified) |
| merge driver | effective - coord-regen, coord-register declared and registered | derived files regenerate and registers union-merge at every merge (Verified) |
| leader | EXPIRED 157 s ago (epoch 77, 14e5e8d5) | TTL is 300 s; this session reclaims immediately before each move of `main`, as last round (Verified) |
| heartbeat | not recorded | no heartbeat is wired; dispatch is observed by `git log` / `git status` per track |
| requests | ok - 128, 0 open | Rulings 101-108 left nothing open (Verified) |
| lease overlap | none | no live leases (Verified) |
| harness capability | claude edit boundary: enforcing (spike S5, 2026-08-24, version not pinned); copilot: historical | **not re-measured here**; the commit floor enforces regardless |
| install | once, in the primary checkout | every worktree **inherits** the drivers and hooks through the shared `.git/config`; `coord doctor` in a tree reads it back; never `coord install` in a worktree. The PC clone installs its own (two-machine.md) |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js`, `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | coord-regen | **none** |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl`, `docs/notes/rulings.md`, `.agents/log/*.jsonl`, `docs/coordination/xmsg.jsonl` | register | coord-register union merge; ruling numbers only through `coord-decide.py rule next` on the Mac | **none** |
| `docs/proof/<track>/**` | authored, new per track | each track writes only its own folder | none (disjoint) |
| `DESIGN.md` §7 | authored | **DOC adds every new row this round approves, once, first, and edits no existing row**. `LabelsTests.CopyRows` pins the markers of COPY-206..239 (`LabelsTests.cs:102-104`) and runs `design.Contains(...)` for 11 `Labels` constants (`:106-108`), so an edited row turns it red while a new row does not (Verified). A change to an existing row's text is made by the code track, in the same commit as the constant it mirrors | serial: DOC first (new rows); CPY/DX for rows mirroring a constant they change |
| `docs/specs/cfd-workbench-v1.md`, `docs/specs/amendments/spec-1.7.5.md` (new), the spec HTML | authored | DOC only | none after DOC |
| `tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs` (`X.Run();` lines) | authored | append one line per new suite; the second to merge rebases | append conflicts only (PNL, CPY, DX) |
| `tests/CfdWorkbench.Analysis.Tests/LabelsTests.cs` | authored | CPY then DX (serial); GRP puts its copy assertions in its own new suite | serial |
| `src/CfdWorkbench.Analysis/Labels.cs`, `AnalysisProjection.cs`, `Loads.cs` | authored | CPY, then DX (serial) | serial |
| `src/CfdWorkbench.Desktop/Analysis/ConditionsBand.axaml(.cs)` | authored | CPY (units, cut cells), then DX (Find α beside Evaluate, `ConditionsBand.axaml:27`) | serial |
| `src/CfdWorkbench.Desktop/PropertiesView.cs`, `WorkbenchController.cs` | authored | CPY owns the Analysis regions (ShellMode Analysis block; `AnalysisView`'s error card and tampered note); GRP owns the CAD regions (`SeveralRows`, `BeginGesture`, `SelectPoint`) **after CPY joins** | serial by time: GRP does Core first and rebases before its Desktop edits |
| `.claude/settings.json`, `tools/hooks/no-heredoc.py` (new) | authored | Coordinator inline (HOOK) | none |
| every other authored file | authored | one owner, from the Tracks table | the owner only |

## Tracks

Boxes follow last round's measured ratios: Sonnet build tracks ran at 0.1-0.5 of their box, median about 0.3, so boxes are
about 0.4× of what last round would have set. Every coding brief: foreground only; a Return section; two repair cycles;
`AGENT_SESSION` exported; a separate session id per task (B2's pooling hid per-task cost); `audit-log.py start` again on a
resume; `CFD_TEST_ONLY=<name>` in the inner loop and the full `tools/run-tests.sh` **once** before the Return; a red-first
receipt in `docs/proof/<trk>/red-first.md`; `tools/trace-brief.py` run by the Coordinator before dispatch, every listed
file either owned or named as read-only. A track that needs a file outside its ownership **stops and sends a seam
request**. Decision requests go `--to` the Fable owner seat; copy, spec and layout picks go to the operator, batched.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **DOC** copy rows + spec 1.7.5 | `DESIGN.md` §7, **new rows only** (Ruling 101's four mockup strings and the 18 built strings as approved, with the two fixes' final text; the Total-drag row of DXM-5 written **only after HYD's verdict**, which the Coordinator relays, and held "proposed — awaiting operator" if HYD changes the label; COPY-G1..G12; the DX NEW rows of `docs/design/dx-screen-states.md` verbatim; **drafted** display text for every SEAM reason code marked "proposed — awaiting operator"); `docs/specs/cfd-workbench-v1.md` (revision 1.7.5: the CAD-04 clause and the AM-1.7-15 retirement of Ruling 107; A5.6 Total drag of Ruling 108 DXM-5; Appendix H §H.5); `docs/specs/amendments/spec-1.7.5.md`; the rendered spec HTML; `docs/proof/doc-oct06/` | — | T1 | 1 | 40 min | every row's text equals its ruling's or design's text (a script compares, listed in the proof); `git diff` shows no existing `| COPY-` row changed; `Copy_AnalysisStrings_MatchDesignMd` green on DOC's head (`CFD_TEST_ONLY=Copy_AnalysisStrings_MatchDesignMd tools/run-suite.sh dotnet <Analysis tests dll>`; `run-tests.sh` unsets the filter) (the join may skip the ring, so DOC proves `LabelsTests` itself); `python3 tools/check-docs.py` green; spec rendered and parity-checked (`tools/render-spec.mjs`, `tools/check-spec-html.mjs`, `SPEC_NAME=cfd-workbench-v1`); the canonical `Product specification · revision 1.7.5 ·` line intact; a docs-only join (ring skipped, Ruling 89) | Sonnet sub-agent |
| **HYD** Wing-only label check | `docs/reviews/dxm5-wing-only.md` (new) | — | T1 | 1 | 15 min | verdict on Ruling 108 DXM-5 (the Total drag row showing wing drag marked "Wing only" with the omitted components listed): CLEAR, or CLEAR WITH CONDITIONS naming the label change; a label change goes to the operator before CPY's Total-drag commit | hydrofoil-hydrodynamicist persona (Opus) |
| **PNL** adaptive panels | `src/CfdWorkbench.Analysis/SectionTier.cs`, `PanelMethod.cs`, `MethodRecord.cs` (the method version string only), `Cavitation.cs` (governing Cp_min source only); `tests/CfdWorkbench.Analysis.Tests/PanelCpTests.cs`, `SectionSeamTests.cs` (incl. `Section_CamberedWing129_WarmTime`, `:19`), the cavitation suite, its `AnalysisChecks.cs` line; `docs/proof/pnl/` | — ; step 0's decision for the other-station clause | T1 | 1 | 60 min | **Step 0 (Flagged):** today the 200-vs-400 under-read is measured only at the governing station (`SectionTier.cs:115-117`), so Ruling 103's "other stations keep the DR-DXM-7 provisional row when their under-read is above 10 %" has no measurement behind it. PNL proposes how that under-read is known (a bound by thickness from the Kármán–Trefftz study, or a 400 solve at stations within a stated margin of the governing one), with its cost; the decision request goes to the Fable owner after a cfd-numerical-verification-expert opinion. **Then**, each red then green, the first two joining first and the other-station test as a follow-on commit once the ruling exists (it is outside the box): the governing station's screen uses its 400-panel Cp_min and every other station 200; a non-governing station whose under-read exceeds 10 % by the ruled measure carries the provisional flag (the planted case uses a thin section); the run still records the governing under-read; the method version moves so older runs read Historical. Timing re-measured warm and first-run with the extra solve, inside 1 s. Four figures **reconciled in writing** (which workload each measured): 419 ms (readiness MEASURE line, 2026-10-06, session output in `.tmp-tests`; PNL commits the excerpt to `docs/proof/pnl/` or keeps it labelled Inferred), 723 ms (Ruling 103's citation of the previous session's readiness), 430 ms (`docs/proof/a3bc-seam/proof-pack.md:48`), 1.6 s (SEAM CFD review); each labelled Inferred until re-measured by PNL. cfd-numerical-verification-expert review clears | Sonnet sub-agent |
| **MCAD** group-move Adversary review | `docs/reviews/group-move-adversary.md` (new) | — | T1 | 1 | 20 min | Ruling 107 DR-GM-9: a real Adversary pass on `docs/design/group-move-node-m.md` and its mockup; findings ranked; Blockers become design edits by GRP or an operator item | marine-cad-ux-expert persona (Opus) |
| **CPY** A3a copy and display fixes | `Labels.cs`, `AnalysisProjection.cs`, `Loads.cs`; `LabelsTests.cs`, `ProjectionTests.cs`; `PropertiesView.cs` (ShellMode Analysis block), `Panes/PropertiesPane.axaml(.cs)`, `Analysis/ConditionsBand.axaml(.cs)`, `Analysis/LoadingChart.cs`, `WorkbenchController.cs` (`AnalysisView` error card and tampered note only); `Analysis/AnalysisPanel.axaml(.cs)` (the Loads tab's cut cells only, before DX starts); the Total-drag assertions in `LoadsViewTests.cs`, `StripFixtureTests.cs`, `PolarNumericsTests.cs`; the Desktop suites of these files; `ShellWindowTests.cs` (Analysis checks only; GRP edits its CAD checks after CPY joins); `docs/proof/cpy/` | DOC (rows), HYD (Total drag label) | T1 | 1 | 60 min | Ruling 101 and DXM-5, each red then green: shallow station (h/c < 5 anywhere) shows COPY-224/226 not COPY-223; no bare "Unavailable" in any projection state (a scan over every projection fixture); one Total-drag reason string; Total row shows wing drag marked "Wing only" with omitted components; the four new strings rendered where the mockup shows them; the band in kn under Imperial and m/s under Metric; any `DESIGN.md` row mirroring a constant CPY changes is updated in the same commit and `Copy_AnalysisStrings_MatchDesignMd` stays green; Envelope, Total drag and band cells not cut and the chart x ticks visible while Running at 1500x870 (captures); one green ring | Sonnet sub-agent |
| **GRP** group move | Core: `src/CfdWorkbench.Core/AuthoringSession.cs` (group gesture and typed group command), its Core tests (new suite `GroupGestureTests`); then, **after CPY joins**: `WorkbenchController.cs` (`BeginGesture`, `SelectPoint`, nudge), `Selection.cs`, `PlanCanvas.cs`, `ElevationView.cs`, `CurvePointLayer.cs`, `PropertiesView.cs` (`SeveralRows`), `Shell/StatusStrip.axaml(.cs)`, Desktop suite `GroupDragTests` (new) with its copy-row assertions, `ShellWindowTests.cs` (`Properties_MultiplePoints_MixedReadOnly`, `:2630`, which the editable several-point rows replace); read-only with **no signature change** (new overloads only): `SectionCanvas.cs` (`BeginGesture` at `:739`, `:962`), `PointsView.cs`; `docs/proof/grp/` | MCAD (design edits), DOC (COPY-G rows); CPY for the Desktop half | T1 | 1 | Core 60 min; Desktop 60 min | `docs/design/group-move-node-m.md` §9's named tests each print `PASS <name>` (Coordinator greps every line; a missing line is red); the §6 `assume:` confirmed by the one-member byte-equality sweep; planted mutants red (clamp per member instead of rigid; collapse on press); `gesture.end` carries `members`; captures of drag, hold at limit and typed Set to / Move by vs the approved mockup; marine-cad-ux-expert re-check of the build | Sonnet sub-agent, one session id per half |
| **DX** section and polar displays | `Analysis/AnalysisPanel.axaml(.cs)` (Section tab), new `Analysis/Section*.cs`, `Analysis/PolarChart.cs`, `Analysis/FindAlphaDialog.axaml(.cs)`; after CPY: `AnalysisProjection.cs` Section group (`:145` and `SectionRows`), `Labels.cs` DX strings, `LabelsTests.cs` DX rows, `ConditionsBand.axaml(.cs)` Find α button; `Styles.axaml` (tokens); Desktop suites; `docs/proof/dx/` | CPY (shared files); DOC (rows); the operator's answer on the reason-code drafts **for the reason-rendering step only** | T1 | 1 | 120 min | test names listed from `dx-screen-states.md` and cleared by the test-architect **before** the build, each printing `PASS <name>`; every state in the approved mockup `docs/mockups/dx-section-polar-states.html` captured from the app and compared (a state not in the mockup is shown to the operator before it is built); DXM-2..9 each pinned by a test; ux-accessibility review as advice (operator priority); `run-readiness.py --check` green for the DX head | Sonnet sub-agent |
| **LAY** 1280x800 four views | step a: `docs/proof/lay-1280/` (captures and options only). Step b, after the operator picks: `ModelArea.axaml(.cs)` (the fallback at `:242-243`; the floor `:21` stays), `Shell/ShellLayout.cs`, `Shell/WorkspacePresets.cs`, `ViewCamera.cs` if the layout enum changes; Desktop layout tests | step a: —. Step b: the operator's pick | T1 | 1 | a 30 min; b 60 min | a: two or three layouts captured at 1280x800 in Analysis with today's docks (what shrinks: dock widths, rails, bottom panel), each with measured view sizes vs the four-view minimum; b: Analysis keeps four views at 1280x800 (test red then green) **with `MinimumFourViewSize` pinned at 320x240 in the test** (`ModelArea.axaml.cs:21`) and every measured view size ≥ it, so lowering the floor cannot pass; no layout regresses at 1500x870 (captures); one green ring | Sonnet sub-agent |

**HOOK** (Ruling 104) is not a track. The Coordinator does it inline: `tools/hooks/no-heredoc.py` from the proposal, wired in
`.claude/settings.json` with the existing portable `python3`/`python` command form. A planted heredoc command is
refused, a plain command passes, and one Sonnet sub-agent is asked to run a heredoc. Whether the hook fires for a
sub-agent is recorded in AGENT-HEREDOC. About 10 minutes.

**PC pull requests** are not a track. On each `pr-ready` message the Coordinator fetches, and the Fable owner reviews the
diff and its receipt (two-machine.md). On approval the Leader joins it through `conductor-join.py`, with Mac readiness
green. A PC branch never waits on this round, and this round never edits the PC's paths.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| DOC → every build on `DESIGN.md` | one writer of the approved rows; GO5(a) on `DESIGN.md` §7, the file that cost a serial C → AUX → DX chain last round | DOC |
| HYD → CPY's Total-drag commit | a decision edge (GO5 b): a label condition changes the string CPY writes | HYD, then CPY |
| CPY → DX on `Labels.cs`, `AnalysisProjection.cs`, `Loads.cs`, `LabelsTests.cs`, `ConditionsBand` | two authors of one file fail GO5(a) | CPY, then DX |
| CPY → GRP's Desktop half on `PropertiesView.cs`, `WorkbenchController.cs` | same files, different regions; serial by time is cheaper than region leases | CPY, then GRP Desktop |
| MCAD → GRP | a decision edge: a Blocker changes the design GRP builds | MCAD, then GRP |
| operator's pick → LAY step b | a decision edge, and the rule that the operator sees a layout before it is built | operator |
| operator's answer on reason-code drafts → DX's reason rendering | copy is the operator's (Ruling 108 approved text that did not exist yet) | operator, then DX |
| every move of `main` | one leader (Ruling 106); readiness green for that HEAD; reclaim the designation first | Leader |
| heavy test runs | at most 2 at once (`tools/ring-lock.sh`, 2 slots); the 3D frame checks fail under load | Coordinator staggers |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| DOC -> CPY, DX, GRP | the approved rows' exact text | DOC joins first; builds cite row ids |
| HYD -> CPY | the Wing-only label condition, if any | HYD's file; a wording change goes to the operator first |
| CPY -> DX | the projection, labels and band after the Ruling 101 fixes | CPY joins; DX rebases |
| CPY -> GRP | `PropertiesView.cs`, `WorkbenchController.cs` | CPY joins before GRP's Desktop half starts |
| PNL -> DX | the section tier's data shape (unchanged by design: adaptive panels change values, not fields) | PNL keeps the projection's fields; a field change is a seam request to DX |
| PNL, CPY, DX -> `AnalysisChecks.cs` | one registration line each | append; the second to merge rebases |
| MCAD -> GRP | Blocker findings | design edits by GRP before code, or an operator item |
| LAY -> CPY | the Properties dock width, if a layout option narrows it | LAY step b starts after CPY joins; a width change is LAY's, the cell content CPY's |
| PC -> Mac | a `win/*` pull request | Fable review, then the Leader's join |
| any -> Owner | a decision request | `coord-decide.py request --to <Fable owner seat>`; copy, spec and layout to the operator, batched |

**Joint satisfiability of shared surfaces (GO14a).** `DESIGN.md`: after DOC, no track edits it, so the only guard is
`LabelsTests`. It is a set of `Contains` checks on row text plus marker (`LabelsTests.cs:99-120`; no scan root or
recursion; token = the literal row; allowlist none). CPY and DX each add their own `Contains` lines for rows DOC wrote,
so all are jointly satisfiable. `AnalysisChecks.cs`: `tools/check-named-tests.py` scans `tests/` recursively for the
token `PASS <name>`, allowlist none. Appending never removes a name. `PropertiesView.cs` / `WorkbenchController.cs`:
CPY's guards are Analysis-mode Desktop tests and GRP's are CAD-mode tests. Neither asserts on the other's region, so
the files are serial by time, with no joint guard to satisfy.

## Review record (Stage 8 DISCONFIRM)

Reviewer: test-architect + the-simplifier persona (Sonnet sub-agent, Adversary Mode), 2026-10-06. Verdict **BLOCK**
(findings 1 and 2), cleared by plan edits only; every finding was checked against the repo before it was applied:
(1) DOC could break `LabelsTests` with the ring skipped → DOC adds rows only, proves `Copy_AnalysisStrings_MatchDesignMd`
itself, and the code track edits any row mirroring a constant · (2) Ruling 103's other-station clause and the timing
figures had no test → PNL step 0 (the under-read is measured only at the governing station, `SectionTier.cs:115-117`),
a planted thin-section test, four figures reconciled · (3) DXM-5 row raced HYD → the row waits for HYD's verdict ·
(4) LAY-b could pass by lowering the floor → `MinimumFourViewSize` pinned · (5) unowned test files → named per track,
`ShellWindowTests.cs` serial CPY → GRP · (6) `BeginGesture` callers → read-only, no signature change · (7) Loads-tab cut
cells → CPY before DX · (8) line references corrected (`:145`, `:21`; `LabelsTests` scope) · (10) HYD and MCAD run as
single review calls, not tracks. Finding 9 (GRP's tests do not exist yet) is expected for red-first and stays as the
grep of every `PASS <name>` line.

Re-check, same reviewer: **CLEARS-THE-VETO yes**, PASS-WITH-CONDITIONS; its three Minor conditions (PNL's ruled test as a
follow-on commit, the 419 ms source, CPY's row-mirroring duty in its exit) are applied above. A Proof Pack is still
required at each track's join.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| HOOK as a track | about 10 min of settings work; done inline (commit `1616d891`): self-test, refuse/allow payloads, `check-pack-hooks.py` extended to `tools/hooks/`; sub-agent firing measured in a new session after the join |
| HYD and MCAD as tracks | single persona review calls; a track's 15× multiplier buys nothing for one file each |
| Spec 1.7.5 as its own track | same reviewer, same render tooling and same docs-only join as the copy rows; merged into DOC |
| CPY and DX merged | considered (they share five files). Kept apart: CPY is about 60 min of small fixes that unblock GRP's Desktop half and LAY, and DX is the longest track. Merging would put GRP and LAY behind DX |
| GRP Core and Desktop as two tracks | one track with two halves and one session id each; the Core half is independent of CPY, the Desktop half is not |
| A separate "reason-code copy" track | the drafts are rows like any other; DOC drafts them and the operator answers in the next batch |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Commit and join this plan (docs-only); HOOK inline | ~15 min | the briefs cite it; the hook guards every later brief |
| 2 | Reviews first, as single Opus calls (not tracks): **HYD**, **MCAD**. With them: **DOC** (Sonnet; its Total-drag row waits for HYD), **PNL** (Sonnet), **LAY-a** (Sonnet, captures) | 15 / 20; boxes 40 / 60 / 30 | DOC is on the critical path; PNL and LAY-a are independent; the reviews feed DOC, CPY and GRP |
| 3 | Join DOC (docs-only). Operator batch 1: the reason-code drafts, the LAY options, any HYD or MCAD item for the operator | ~5 min + operator | unblocks CPY now and DX's reason step later |
| 4 | **CPY** (Sonnet) and **GRP Core** (Sonnet) | 60 / 60 | CPY gates DX and GRP Desktop; GRP Core needs only MCAD |
| 5 | Join PNL and CPY as each clears review (readiness green; reclaim leader; push) | ~5 min + readiness ~2 min each | — |
| 6 | **DX** (Sonnet); **GRP Desktop** (Sonnet); **LAY-b** once the operator has picked | 120 / 60 / 60 | after CPY; at most 3 coding at once |
| 7 | Join GRP, LAY, DX; PC pull requests joined whenever the Fable owner approves one | per join | — |
| 8 | Close: Completed / Remaining / Next, planned vs actual per track, one list of operator items | — | — |

**Critical path (Inferred from boxes):** DOC 40 → CPY 60 → DX 120 ≈ 220 min of boxes. At last round's measured median
of about 0.3 of the box, that is about 1.1 h plus join and operator time. The operator's answer on the reason codes can
put the end of DX on the path; DX builds its other states first.

**Cost.** An orchestrator-worker round costs roughly 15× a single session in tokens (GO6). It is paid here for:
- **genuine independence:** PNL's numerics and GRP's Core half share no file with the CPY → DX chain;
- **context hygiene:** each build carries its own design trace;
- **operator latency:** LAY's options and the reason-code drafts wait on you while the builds run.

The chain CPY → DX is serial, and its width is 3 only in wave 2.

## Planned vs actual (Stage 7, 2026-10-06)

`main` at `b36f8d7b`, readiness green (182.3 s of 240 s), pushed. Rulings 101-123 this round; 14 joins. **Measured
minutes** are the harness's agent wall time per dispatch (task notifications, Verified), summed over each track's passes;
the audit log's `duration_seconds` under-records resumed passes (AC-09 shape again) and `trk-lay` reused last round's
session id, so it is the cross-check, not the source. Box = the brief's budget, summed over passes.

| track | passes | measured min | box min | ratio | note |
|---|---|---|---|---|---|
| DOC | 3 (rows + spec; R109 flip; R116 rows) | 12.0 | 40 + 10 + 20 | 0.17 | 107 + 37 rows, spec 1.7.5, 0 mismatches |
| PNL | 3 (build; R110 + CFD conditions; telemetry + R114) | 42.1 | 60 + 2 cycles | 0.70 of build box | two Fable rulings (110, 114) reshaped it; the second closed a real gap (a thin station could govern unseen) |
| LAY-a | 1 | 10.9 | 30 | 0.36 | its build was folded into CPY (Ruling 112) |
| CPY | 2 (build; C-3 join repair) | 32.6 | 90 | 0.36 | grew past brief: Units plumbing (no Units setting existed) |
| GRP Core | 1 | 20.1 | 75 | 0.27 | ring failed only under load 47-71 |
| GRP Desktop | 1 + 2 repair | 48.8 + 40.6 | 90 + 75 + 40 | 0.43 | marine-CAD BLOCK then two conditions; real bugs: Δ 0.00, 800x unit error, stale readout |
| DX | step 1 + step 2 | 42.6 | 120 | 0.36 | ran out of context; partial red-first |
| DX finish | 2 (finish + stations) | 27.2 | 90 + 30 | 0.23 | Ruling 117 compute-and-flag folded in |
| DX third pass | 1 (operator cap exception) | 13.3 | 45 | 0.30 | CFD conditions + Ruling 119 |
| CI lessons | 1 | 10.8 | 60 | 0.18 | 6 classes, 3 new controls |
| UNI | 3 (build; persistence; lint) | 16.6 | 60 + 25 + 15 | 0.17 | lint caught a raw margin at the join |
| SPL | 2 (profile; move checks) | 28.8 | 90 + 40 | 0.22 | split does not pay (CPU-bound); 11 checks to readiness |
| Reviews (11 single calls) | — | ~29 | — | — | test-architect ×3, hydrodynamicist, marine-CAD ×3, CFD-V&V ×2, Fable owner ×2 |

**What paid.** Docs-first (DOC) removed every `DESIGN.md` conflict except one id collision, and made copy approval a data
change for the builds. The adversarial reviews found real defects every time they ran: the hydrodynamicist's "Total
drag" block, the CFD lens's governing-station flip, the Fable owner's 2x-width gap, the marine-CAD lens's Δ 0.00 and the
800x unit error. Folding LAY-b into CPY saved a track. Boxes ran at 0.17-0.43 of plan except PNL, whose two rulings came
mid-track; the next plan should box Sonnet build tracks at about 0.35x of this round's.

**What did not.** Three tracks building at once drove load to 47-77 and produced load-only failures and one wasted join
(PARALLEL-BUILD-LOAD; `tools/dispatch-gate.py` now exists, and the Coordinator still started one join without running it).
DX needed three passes and an operator-authorised cap exception: its brief carried a false premise (the stations count)
and its first agent exhausted its context. GRP's ring stayed green while the fast ring crossed its budget because the load
gate turned the overrun into a COST-MISS; the next join on a quiet machine exposed it (51.9 s net). The Desktop split the
operator chose measured slower (CPU-bound), and the headroom came from moving 11 checks to readiness instead.

**Next plan inputs.** Run `dispatch-gate.py` before every dispatch and join; at most 2 build tracks. Give parallel tracks
COPY id ranges at dispatch. Give large UI tracks a context checkpoint (commit + Return at ~60 % of the box). Fix the three
ungated wall-clock checks and the five `DSL-`/`DOC-` raw-code fallbacks (register). The fast ring is at 46-47.6 s net: about
one round of growth before the next decision (Core rebalance was partly achieved by the moves: Core 3/3 45 -> 29 s).

## Operator items carried to the next round

1. Review the packaged app with the new screens (DX, group move, Units); ask for a fresh package.
2. Post the NeuralFoil weights question on GitHub (carried from round-oct05).
3. The PC session: no `win/*` branch or message yet. A handoff about the new `units` key in `cfdw-display` is posted
   in `docs/coordination/xmsg.jsonl` (Ruling 121).
4. Unapproved wording UNI left out: a Units status announcement, a tooltip, a message for a units save that cannot be kept.
