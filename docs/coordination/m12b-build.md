---
id: coordination-m12b-build
title: "Coordination plan - M1.2b build (CAD point editing on the Plan view)"
type: plan
status: proposed
owner: "@cfd-leader-fbfa35dc"
tags: [coordination, worktrees, parallelism, desktop, core, cad, plan-view, m1.2b]
links:
  - { to: design-m12b-points, rel: implements }
  - { to: architecture-application, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: coordination-app-shell-build, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-30"
summary: >-
  Schedules the M1.2b design's seven tracks (B0, B1a, B1b, U1a, U1b, U2, U3) behind one precondition track (PRE: the
  checker's --design flag and the test-class stubs), on Grok, Codex and Claude with at most two concurrent coding
  tracks, boxes at 3x measured priors, join gates, the 2-cycle cap and a closing operator review.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# Coordination plan: the M1.2b build

- **Scope:** slice M1.2b, CAD point editing on the Plan view ([design](../design/m12b-points.md) §14,
  [architecture](../architecture/application.md) §10.6 row M1.2b). The design's track table (B0 · B1a · B1b · U1a · U1b
  · U2 · U3) is the decomposition. This plan schedules and assigns it; it does not re-cut it. It adds one precondition
  track (PRE) that the design itself requires before dispatch (§12.2). PRE is the Coordinator's inline step, not a
  delegated session. It is called a track only because it has owned paths, a box and exit evidence like the others. M1.2b2 (a parallel design track), M1.2c–e and
  pushing are out of scope.
- **Inputs:** design `design-m12b-points` as merged at `5f31233` (status "In review … Owner acceptance pending");
  Rulings 53–56; the app-shell plan's "Planned vs actual" section for the priors. Base read: integration tip
  `3a37f5f` (branch `feature/ui-cad-direction`, the LEFTPANE join).
- **Seats:** Leader and Coordinator = Claude Code Opus 5.5, session `fbfa35dc`. Owner = seat `cfd-owner-fbfa35dc`
  (Verified: it ruled Rulings 52–56). The Owner rules into `docs/notes/rulings.md` (class `register`). The Owner never
  authors track code or clears its own veto.
- **Author:** `/prepare-for-coordination` sub-agent `track-m12b-plan`, 2026-09-30, in worktree
  `CFD-Workbench-plan-m12b-build` (branch `plan/m12b-build`). Git before: `3a37f5f`.

Labels: **Verified** = observed in this run; **Inferred** = reasoned, not observed; **Flagged** = observed but suspect.

## Preconditions (before any track is dispatched)

| # | precondition | state (this run) | evidence | cleared by |
|---|---|---|---|---|
| P1 | SHELLFIX joined (design §1: "Every Desktop track branches from `main` **after** SHELLFIX joins") | **met** (Verified) | merge `63e35a5` ("SHELLFIX — model redraws on tab return, one model tab strip …") is an ancestor of `3a37f5f`; `git merge-base --is-ancestor fix/m12a-shell-visuals HEAD` exit 0 | — |
| P2 | LEFTPANE joined (the Properties pane must be visible, design §1 (c)) | **met** (Verified) | merge `11cc130` and its join `3a37f5f`; `fix/m12a-left-pane` is an ancestor of HEAD | — |
| P3 | `tools/check-named-tests.py --design <path>`, so the checker reads `m12b-points.md` (design §12.2: "The Coordinator lands it before dispatch and gates it on `--self-test` exit 0, with the planted-name case observed red before the flag and green after, and the extraction run on this design before any track is dispatched") | **not met** (Verified) | `check-named-tests.py`:30 hard-codes `DESIGN = … "app-shell.md"`; `main()` (:191-198) refuses any argv other than one argument, so `<track> --design …` exits 2 today | **PRE** track |
| P4 | the design's extraction is clean | **met for the design text** (Verified) | the as-built `extract()` run on `m12b-points.md` (read-only script): **187 names, 0 errors**; B0 29 · B1a 24 · B1b 43 · U1a 23 · U1b 43 · U2 25 · **U3 0** | PRE re-runs it through the new flag |
| P5 | Owner acceptance of `design-m12b-points` and ADR-0001 Amendment 1 (the design's status line: "Owner acceptance pending"), plus this plan and its seams S1–S6 | **not met** | design status line; no ruling after 56 | Owner ruling **O-M12B** (Order of operations step 2) |
| P6 | DR-12, DR-13, OI-1 ruled | **met** (Verified) | Ruling 56 | — |
| P7 | a live leader designation and no silently expired requests | **not met** (Verified) | `coord doctor`: leader epoch 42 expiring in 536 s; 4 requests `COORD-REQUEST-SILENT-EXPIRY` | step 1 |
| P8 | tracks branch from the integration branch, not `main` | **note** (Verified) | local `main` is `405e54b`, 156 commits behind `3a37f5f`. The design's "branches from `main`" means the integration branch `feature/ui-cad-direction` at or after `3a37f5f` | the Coordinator names the base in every brief |

The native attach (review F-ATTACH, class CO-UI-READY) gates **U3's exit**, not dispatch (design §12.3 item 5).

## Layer state

Measured in this tree on 2026-09-30 with `coord-core.py doctor` (exit 1), `pack-doctor.py --json` (20 PASS · 2 WARN ·
0 FAIL) and `coord session list` (3 active sessions, 97 files scanned).

| check | result | meaning |
|---|---|---|
| registry | ok - 8 pattern(s) | `.agents/artifacts.yml` is present and parses. Anything not listed stays `authored` |
| merge driver | effective - coord-regen, coord-register declared and registered | derived files regenerate and registers union-merge at every join. It was installed **once, in the primary checkout**; each track worktree **inherits** it through the shared `.git/config` and `.git/hooks`. Each tree runs `coord doctor` to read it back and never runs `coord install` |
| regeneration | nothing owed | no derived artifact is stale in this tree |
| leader | `fbfa35dc` epoch 42, expires in 536 s | live now, but the 900 s lease will lapse before the first join (Known coordination defects K2). Step 1 re-pins |
| heartbeat | 11 sessions; newest beat 195985 s ago; 3 stalled, 0 live, 11 done | nothing is running. Nothing is held |
| requests | **FAIL** `COORD-REQUEST-SILENT-EXPIRY`: `req-01M3JGPAP1NS54N6GDMFZ6W9E0`, `req-01M3M5PGZJ4Z84EVAR1B4H5BQ0`, `req-01M3M5PQ57GS2R24NKWBYD9KX9`, `req-01M3SG5TE62HKD3T7CPYMDXVZ3` | step 1 runs `coord request expire` (the Coordinator's action, not this run's) |
| lease overlap | none (0 live leases) | no contention now |
| sessions | `track-m12b2-design` (M1.2b2 design, parallel), `fbfa35dc`, `track-m12b-plan` (this run); 0 claims each | the M1.2b2 design track writes only docs; it holds no M1.2b path |
| harness capability | claude edit boundary "enforcing" (spike S5, 2026-08-24, **not measured here**); copilot "historical" | see Harness qualification |
| pack-doctor `doorbells` | not recorded (no `coord-mail.py dispatch` probe run) | every track row carries doorbell = not recorded |
| pack-doctor WARN | copilot `contextTier=long_context`; knowledge graph has stale or flagged nodes | not on this plan's path |
| pre-commit floor | `.git/hooks/pre-commit` runs `coord-core.py precommit`, **advisory unless `AGENT_SESSION` is set** | K1 below. The join's ownership read (gate 5) is the real control |

**Deliberately not run: `coord classify init` and `coord install`.** The app-shell plan recorded (Flagged) that
`coord classify init --force` run from a linked worktree wrote the **primary checkout's** registry and derived files. The
registry already exists and `coord doctor` reads it back clean, so this run changed nothing in it. Rule for this plan:
**no track runs `coord classify` or `coord install`**; the Coordinator runs them only in the primary checkout.

## Artifact classes

Classes are read from `.agents/artifacts.yml` in this tree. `docs-graph.py derive` and the audit render run at emit.

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js` | derived | `python docs/ai-forward-pack/scripts/docs-graph.py derive` via coord-regen | **none**: a conflict here is regenerated, never hand-merged (K3) |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `audit-log.py --root docs --project CFD-Workbench render` | **none** |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | union merge (coord-register) | **none**: every track appends its own audit entry |
| `docs/notes/rulings.md` | register | union merge | **none**: only the Owner appends |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | union merge | **none** |
| `.tmp-tests/` | ignored | per-tree test logs read by `check-named-tests.py` | **none**: never committed |
| `docs/design/m12b-points.md` | authored, **frozen for the wave** | the single authority `check-named-tests.py --design` reads | Coordinator only, after an Owner ruling, and only serially |
| `docs/design/app-shell.md` | authored, frozen | still the authority for the M1.2a names that U1a and U2 must keep green (GO14a guards) | Coordinator only, after an Owner ruling |
| `docs/coordination/join.json` | authored | its `readiness` array is the ring `run-readiness.py` runs | one owner: U3 |
| `docs/proof/m12b/<track>.md` | authored | each track's Proof Pack | one owner each: its track |
| `docs/lessons/defect-classes.md`, this plan | authored | conventional conflict markers | Coordinator only |
| `tests/CfdWorkbench.Core.Tests/Fixtures/m12b/**`, `docs/proof/m12b-old-build/`, the golden M1.2a recovery fixture | authored | golden bytes are the assertion; nothing regenerates them | one owner each: B0, B0, B1b |
| `src/CfdWorkbench.Desktop/packages.lock.json`, all `*.csproj` | authored | no track adds a package; a change is a seam request to the Coordinator | nobody during the wave |
| `src/**`, `tests/**`, `tools/check-named-tests.py`, `tools/run-readiness.py`, `DESIGN.md`, `docs/specs/foildsl.md`, `docs/reviews/m12b-native.md` | authored | conventional conflict markers | **one owner per file at a time**. See Tracks and Seams |

The only real contention is authored C#, AXAML and three shared test-runner files. Every file has exactly one owner at
a time. Six files change hands at a merge, never concurrently (Seams S1–S6).

## Tracks

Width cap: **two concurrent delegated coding tracks.** Every coding track's test command is the design's §14 wording:
"`tools/run-tests.sh` then `tools/check-named-tests.py <track> --design docs/design/m12b-points.md`". Named-check counts
are the as-built extractor's output on the design (P4). Boxes follow Ruling 54 P1, quoted: "A track's time box is 3x a
measured prior from the same class of work. Without such a prior the box stays as written and the measured time goes in
the audit entry." **Red before green (every coding track):** the track commits a Proof Pack at
`docs/proof/m12b/<track>.md` (owned by that track) that records, for each of its named tests, a red run before the
code that makes it green, or, for a test that pins as-built behaviour, the named mutant run that turns it red. A PASS
line alone is not exit evidence: `check-named-tests.py` checks only that each name printed PASS (`:127-133`). The
priors are the measured rows of `app-shell-build.md` "Planned vs actual"; the prior class per
track is the design's §14 choice. The boxes are the design's §14 numbers; two are rounded (47 × 3 = 141 → 140, 43 × 3 = 129 → 130), and the plan does not restate them as a second figure.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **PRE** checker flag and stubs | `tools/check-named-tests.py` (`--design <path>`, default `docs/design/app-shell.md`; `--self-test` gains a planted unattributed name in a second design); empty `PointModelTests`, `LengthExpressionTests`, `PointGestureTests`, `PointCommandTests`, `ReopenPointEditTests` classes and their `Run()` lines in `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` (seam S1); empty `tests/CfdWorkbench.Desktop.Tests/PlanCanvasTests.cs`, the `--plan-canvas` branch and its entry in the `DesktopChecks.Spawn(…)` line of `WorkbenchTests.cs` (seam S2); a `--readiness` switch in `IdentityTests.cs` and `WorkbenchTests.cs`, never spawned by `run-tests.sh`, that calls empty `RunReadiness()` members on `PointModelTests`, `ControllerShellTests` and `PlanCanvasTests` (B0, U1a and U1b fill them) | step 2 (O-M12B) | T1 | 0 | inline · **32 min** (G0 glue 642 s × 3; G0 was Opus, same class) | `--self-test` exit 0, where the new case calls `main()` **with `--design <second design>`** (the existing cases call `check()` on design text, so they would catch a planted name without the flag), observed **red before the flag and green after** (both runs recorded in the audit entry); `check-named-tests.py B0 --design docs/design/m12b-points.md` runs the extraction (187 names, 0 errors) and fails only on missing PASS lines; `check-named-tests.py D3a` (default design) still exits as before; `tools/run-tests.sh` exit 0 with the empty suites compiled and `--plan-canvas` spawned; `check-docs.py` exit 0 | **Coordinator inline** (Claude Code Opus 5.5, design §12.2 "Coordinator, before dispatch"); not delegated |
| **B0** Core grammar and model | `src/CfdWorkbench.Core/FoilSource.cs`, `Geometry.cs`, `PointModel.cs` (new); `docs/specs/foildsl.md` (§4/§5/§8 and conformance, same change); `src/CfdWorkbench.Cli/Program.cs`; `tests/CfdWorkbench.Cli.Tests/` additions; `FoilSourceTests.cs`, `GeometryTests.cs`, `PointModelTests.cs` (body); `tests/CfdWorkbench.Core.Tests/Fixtures/m12b/`; `docs/proof/m12b-old-build/` | PRE | T2 | 0 | 150 calls · tokens not recorded · **135 min** (Grok P1 45 × 3) | the test command with B0's **29** names PASS. The old-build characterization receipt is committed **before** the parser change (commit order in `git log`). The 4.1 parse ∘ print round trip runs with a fixed seed. `Readiness_SixteenPointThreeAnchors_AssessUnderProofBudget` is written, excluded from `run-tests.sh`, run once, and its measured value recorded in the audit entry. `PointModel.cs` stays Avalonia-free (`Architecture_DockConfinedToShell` green) | Grok `grok-4.7` |
| **B1a** Core chords | `src/CfdWorkbench.Core/ChordDimension.cs` (new), `LengthExpression.cs` (new); `ConstrainedFit.cs` if the chord fit needs a change (seam S6); in `AuthoringSession.cs` only `ApplyDimensionCore`/`ApplyChord`, the memo `Fingerprint`, and the chord `rule` receipt path (seam S3); `DimensionTests.cs`, `LengthExpressionTests.cs` (body) | PRE (parallel with B0) | T2 | 0 | 150 calls · tokens not recorded · **90 min** (Grok C1 30 × 3) | the test command with B1a's **24** names PASS. `Reopen_RetrySameDimensionOperationId_ReturnsPriorId` (the F-5 REPLAY-MEMO fix) is red first against the as-built memo, and the red log is recorded before the fix commit. The hand mutant "limit 20 µm" turns `ApplyDimension_FitJustAboveLimit_AcceptedWithWarning` (the 10.1 µm fixture) red; the 9.9 µm fixture pins the other side and stays green under it (both runs in the Proof Pack). `LengthExpression_RandomText_NeverThrowsUnexpected` runs with a fixed seed | Grok `grok-4.7` |
| **B1b** Core gestures and commands | the rest of `AuthoringSession.cs` (session members, `NativeProject` receipt code, `EditReference` point arms, `RecoveryReference`); `PointGestureTests.cs`, `PointCommandTests.cs`, `ReopenPointEditTests.cs` (bodies); the golden M1.2a recovery fixture | B0 and B1a merged | T2 | 0 | 150 calls · tokens not recorded · **140 min** (Codex D3a 47 × 3) | the test command with B1b's **43** names PASS. The golden recovery fixture is bytes written by the M1.2a build, not synthesized (`Recovery_M12aGoldenRailDraft_ApplyOneRowNoCurve`, `…_DiscardClears`). The hand mutant "ceiling 15" turns `MakeAnchor_ThirteenPoints_SixteenAccepted` red; `MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling` goes red only if it asserts the ceiling number (16) in the refusal text, so B1b writes that assertion (both runs in the Proof Pack). Random-drag property tests (ordering, Smooth collinearity, Symmetric midpoint) run with a fixed seed | Codex `gpt-6-sol` |
| **U1a** controller | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `Selection.cs`, `Shell/ShellEvents.cs`; `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs` | B1b merged; SHELLFIX joined (P1) | T2 | 0 | 150 calls · tokens not recorded · **130 min** (Codex D3b 43 × 3) | the test command with U1a's **23** names PASS. **Controller tier:** the §6.2 gesture table is driven with synthetic input, one test per cell. The hand mutant "threshold 0" turns `Controller_MoveTwoPixels_NoDraft` red (Proof Pack). Span moves to the async path and the Desktop copies of the Core refusal rules are gone (F-6). Every app-shell name in `ControllerShellTests.cs` still PASSes (`check-named-tests.py D2`). `Readiness_NewFoilDrag_FrameP95Under100Ms` and `Readiness_NewFoilCommit_P95Under250Ms` are written, excluded from `run-tests.sh`, run once, and their measured values recorded in the audit entry | Codex `gpt-6-sol` |
| **U1b** Plan canvas | `src/CfdWorkbench.Desktop/PlanCanvas.cs` (new, with `PlanPointPeer`); `ModelArea.axaml`(.cs) (Plan and 3D samples tabs); `Styles.axaml` (new tokens); `PlanCanvasTests.cs` (body) | U1a merged | T2 | 0 | 150 calls · tokens not recorded · **140 min** (Codex D3a 47 × 3) | the test command with U1b's **43** names PASS; `run-tests.sh` three times with identical PASS sets. **Rendered tier (UI-RENDERED-STATE):** the whole realized window is rendered; each point's position comes from `TranslatePoint` canvas → window; pixels are read there (glyph interior, ring radius, handle ends, rail polyline against the draft's samples, orientation); the realized tree has a visible `PlanCanvas` ≥ 320 × 240 not covered at the sampled points. State-only assertions do not count, and the check is a mutant: with `PlanCanvas.Render` blanked, **every** rendered-tier name goes red (Proof Pack lists each). `PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` and `PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` run **first**; if either assumption is false, U1b stops and reports (no silent downgrade). `PlanCanvas_Brushes_AllFromThemeResources` PASS; `xaml-token-lint` clean. `Readiness_PlanRender_Under8Ms` written, run once, measured value recorded. `run-tests.sh` stays under its 60 s budget | Codex `gpt-6-sol` |
| **U2** panes and menus | `PropertiesView.cs`, `Panes/PropertiesPane.axaml`(.cs), `Panes/BrowserPane.axaml`(.cs), `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `Shell/ShellHost.cs` (pane registration only); delete `Panes/RailEditorPane.*`; `ShellModelTests.cs`, `ShellWindowTests.cs` additions; the retirement hand-overs of seam S4 | U1a merged (parallel with U1b) | T2 | 0 | 150 calls · tokens not recorded · **135 min** (Grok P1 45 × 3) | the test command with U2's **25** names PASS; `run-tests.sh` three times with identical PASS sets. **Dead-control tier (UI-DEAD-CONTROL):** `UI_DEAD_CONTROL_ShellButtonsHaveActions` extended to the new Properties, Wing and Browser states and green; every new visible enabled control has an action test **and** an effect test, and with one new control's command unbound the sweep and that control's action test go red (Proof Pack). The Plan and "3D samples" tabs are added to the `Shell_AllModelTabs_ReentryRealizesContent` array (`ShellWindowTests.cs`:73; the UI-RENDERED-STATE re-entry control). The retirement is complete: `grep -rn 'RailEditorPane\|PatchRail\|UpdateDraft(\|BeginEdit(' src tests` returns nothing, and every ported test keeps its name (the app-shell checks `D1`, `D3a` still exit 0). `xaml-token-lint` clean | Grok `grok-4.7` |
| **U3** UX review and polish | `DESIGN.md` (if tokens move); `Styles.axaml` (after U1b merges, seam S5); `docs/reviews/m12b-native.md` (new); the `readiness` array of `docs/coordination/join.json` (the four M1.2b readiness checks, launched through the `--readiness` switch PRE adds; `run-readiness.py` reads that array) | U1b and U2 merged | T1 | 2 (UX & Accessibility, Marine-CAD UX; Adversary Mode, separate agents) | 80 calls · 300k tokens · **120 min** (no same-class measured prior: the app-shell U1a row has no measurement; box as written, measured time recorded) | U3 has **0 named tests**, so `check-named-tests.py U3` would fail on an empty list; U3 is gated by this evidence and gate 3. **Native tier (CO-UI-READY):** rows N-B1 … N-B12 (the §0.1 steps) in `docs/reviews/m12b-native.md`, **each with an agent attach receipt accepted by `tools/check-review-attach.mjs`** before any operator session (N-B1 with a screenshot of the Plan showing both rails and points; N-B10 with a VoiceOver trace of Tab across points and an AX dump of the point peers). `check-review-attach.mjs` proves launch identity, non-empty screenshot bytes and the review window title, not the row's content. So each N-B row also carries a **content assertion** (N-B1: sampled pixels at the expected rail and point positions in the screenshot; N-B10: the parsed AX dump lists every point peer with bounds and focus). A row without one is labelled "attach-ready", never passed. If attach is blocked, U3 stops and reports: "operator-run, not done" is **not** an exit. The UX & A11y and Marine-CAD lenses re-review and clear. `python3 tools/run-readiness.py` produces a green receipt with all four checks, and `run-readiness.py --check` exits 0 on that HEAD. `design-lint.py --strict DESIGN.md` clean if `DESIGN.md` changed | Claude Code **Opus 5.5** (UI judgement) |

Agy is not assigned. The design (§14) says "Agy is not used: every track here is either Core work better suited to
Codex/Grok or long UI work (two HARNESS-SILENT-EXITs)", and every box here exceeds the ~60 min bound under which Agy
finished (D1 21 min, D2 35 min). PRE is the Coordinator's own inline step (§12.2), so no harness is dispatched for it.

### Track detail

Every track ends at its exit evidence, or when the **2-cycle repair cap** fires (AGENTS.md: "Repair loops are capped at
2 cycles; at the cap, stop the track and report to the operator."). A missing Return section, or a branch without the
claimed commit, counts as a failed cycle (HARNESS-SILENT-EXIT). **At the cap the Coordinator stops the track, reports to
the operator, and switches approach or harness; it never runs a third cycle on the same harness.** Precedent: D3a's Agy
silent exit moved to Codex; D3b's contrast work split at its cap into THEME on Claude. A continuation dispatch that
returns with a commit is not a repair cycle, but it counts against the box: the box is the cap on continuations.
**Fallback rule:** a harness fault in cycle 1 moves cycle 2 to the other code harness (Grok ↔ Codex). At the cap the
restart goes to a harness that has **not** run this track (for a Grok → Codex track that is Claude Code Opus 5.5), or
to a narrower approach ruled by the Owner. A track is never re-dispatched to a harness that already failed it.

Seam requests go to the Coordinator with `coord request add`, each with a deadline and a fallback. Decision requests go
to the Owner with `coord decide request --to cfd-owner-fbfa35dc`. Each row carries the leader epoch that
`coord leader who` reads **at that dispatch** (not an assumed number; K2). Doorbell for every row: **not recorded**.

| track | owner seat | rules (Owner) | seam requests expected | deadline (wall from dispatch) | fallback (never a third cycle; never back to a harness that failed the track) | lane |
|---|---|---|---|---|---|---|
| PRE | Coordinator (inline) | `cfd-owner-fbfa35dc` | none | 32 min | over the box: the Coordinator records the measured time and reports; PRE is serial, so a sub-agent saves nothing | Coordinator |
| B0 | Grok session | `cfd-owner-fbfa35dc` | B0 ↔ B1a: the `Curve` record shape (seam S6) | 135 min | fallback rule (Track detail); Grok rate-limit stall while B1a also runs → re-dispatch this track on Codex, never wait on both | A |
| B1a | Grok session | `cfd-owner-fbfa35dc` | B1a → B0: any `FoilSource` helper (refused in-wave: write it in `ChordDimension.cs`) | 90 min | fallback rule; a Grok rate-limit stall while B0 also runs → re-dispatch B1a on Codex | B |
| B1b | Codex session | `cfd-owner-fbfa35dc` | B1b → B1a (merged): `Fingerprint` members | 140 min | fallback rule (Track detail) | A |
| U1a | Codex session | `cfd-owner-fbfa35dc` | U1a → B1b (merged): Core member gaps | 130 min | as B1b | A |
| U1b | Codex session | `cfd-owner-fbfa35dc` | U1b → U1a (merged): controller members (S5); U1b → U3: token names | 140 min | as B1b. An assumption refuted (whole-window render, AX peers) is **not** a repair cycle: stop and report | A |
| U2 | Grok session | `cfd-owner-fbfa35dc` | U2 → U1a (merged): controller members (S5); U2 → app-shell names (S4) | 135 min | fallback rule (Track detail) | B |
| U3 | Claude Opus sub-agent + operator | `cfd-owner-fbfa35dc` | U3 → U1b/U2 (merged): string or token fixes, as a Sonnet edit on its own branch after merge | 120 min | attach blocked → stop and report; the slice stays open | Claude |

### Harness qualification (from what was verified here)

| harness · model | needed from the delegation mechanism | edit boundary | dispatch | basis |
|---|---|---|---|---|
| Claude Code (Opus 5.5, Sonnet) | exclusive paths, a Return block, an audit entry | **observed-only** here (spike S5 records "enforcing", 2026-08-24; not re-measured) | observed-only | `coord doctor`; the commit floor is advisory without `AGENT_SESSION` |
| Grok `grok-4.7` | foreground run, commits on its branch, Return block | **unsupported** | observed-only: C1, P1, NEWFOIL returned with commits (app-shell Planned vs actual). **Two concurrent Grok sessions (B0 ∥ B1a) were never exercised**; every earlier lane paired Grok with Agy | the commit floor (if `AGENT_SESSION` reaches it) and the Leader's branch read |
| Codex `gpt-6-sol` | the same | **unsupported** | observed-only: D3a dispatches 2–3, D3b 1–2, U1FIX, OPENFIX returned with commits | the same |
| Agy `gemini-3.8-flash-high` | — | unsupported | not assigned (two HARNESS-SILENT-EXITs on long UI work) | — |

Dispatch commands (from the operator's recorded usage):
`grok --cwd <wt> -m grok-4.7 --always-approve --no-subagents --output-format plain -p "$(cat brief.md)"` and
`codex exec -m gpt-6-sol -C <wt> -s danger-full-access "$(cat brief.md)"`.

**Every Codex and Grok brief is hand-compiled.** The prompt compiler (`/compile`, `prompt-compile.py`) has no templates
for the Codex, Grok or Agy harnesses (app-shell Planned vs actual: "The prompt compiler has no Grok/Agy templates, so
those briefs were hand-compiled"). The Coordinator writes each brief by hand with: the goal state, the base commit, the
owned paths (and the explicit list of files it must not touch), the test command with `--design`, the exit evidence
row, "foreground only; no background processes", `export AGENT_SESSION=<track> AGENT_WI=<track>` as the first shell
line, and the Return section. The one Claude brief (U3) goes through `/compile`. A brief never carries an unanswered `DR-n`
line; Owner rulings are stated as settled facts with the ruling number (known pack issue: `prompt-compile.py` sets
`dispatchable` false whenever `decision_requests` is non-empty).

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| O-M12B Owner ruling: accept `design-m12b-points` and ADR-0001 Amendment 1; accept this plan (track count: the Tech Lead was not convened) and seams S1–S6 | decision edge into every track; the design is "Owner acceptance pending". Fails GO5(b) | Owner `cfd-owner-fbfa35dc` |
| PRE | the checker is the join gate for every coding track, and it owns the three shared runner lines (`IdentityTests.cs` `Run()` list, the `WorkbenchTests.cs` spawn line). Until it lands, B0 and B1a would both author `IdentityTests.cs`. Fails GO5(c) | PRE |
| {B0, B1a} → B1b | data edge: B1b consumes B0's `PointModel`/parser and B1a's `Fingerprint` and chord path in the same `AuthoringSession.cs` | lane A |
| B1b → U1a | data edge: U1a calls `BeginPointGesture`, `UpdatePointGesture`, `ApplyPointCommand`, `ApplyChord` | lane A |
| U1a → {U1b, U2} | data edge: both read the controller's `Planform`, `Gesture`, commands and camera (§5.2). Until U1a fixes them, every Desktop track's shape changes. Fails GO5(b) | lane A |
| {U1b, U2} → U3 | U3 reviews the rendered and wired whole; it also takes over `Styles.axaml` from U1b (S5) | U3 |
| the first join after PRE | read `coord doctor` in the integration tree before two lanes rely on the merge driver | Coordinator |
| wave join → operator review → any merge to main | the operator review is the standing rule after M1.2a; readiness runs before main | Coordinator, operator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| S1 · PRE -> B0, B1a, B1b | B0 and B1a run in parallel and both need a `Run()` line in `IdentityTests.cs` (Verified: `:52-68` is one list of `XTests.Run();` calls); B1b adds three more | PRE creates the five empty classes and their `Run()` lines, the G0 pattern that Ruling 52 accepted for the app-shell ("G0 creates the empty Desktop test classes … so the spawn lines G0 owns compile; ownership passes at G0's merge"). Each class body passes to its track at PRE's merge. **O-M12B confirms.** Considered and rejected: calling `PointModelTests.Run()` from `GeometryTests` and `LengthExpressionTests.Run()` from `DimensionTests`. It removes S1 but hides two suites behind others, so a deleted call drops a suite with no visible runner line |
| S2 · PRE -> U1b | §14 lists "the `--plan-canvas` entry point" under U1b, but §12.2 puts it in the Coordinator's harness change before dispatch, and `WorkbenchTests.cs` is also edited by U2's retirement (Verified: it references the rail editor contracts) while U1b runs | PRE adds the `--plan-canvas` branch and spawn entry (`WorkbenchTests.cs`:49-64, :1861 shape) and the empty `PlanCanvasTests`, per §12.2. U1b owns only `PlanCanvasTests.cs`; `WorkbenchTests.cs` passes to U2. **O-M12B confirms** |
| S3 · B1a -> B1b | B1a's named tests `ApplyDimension_Receipt_CarriesRuleId`, `Reopen_ChordRowWithoutRule_DocReference`, `Reopen_ForgedRuleValue_DocReference` (design §9, §12.4) need the receipt's `rule` member, `"rule"` in `NativeProject.Read`'s optional list and the `"dimension"` arm of `EditReference` (§4), which §14 gives to B1b ("`NativeProject` receipt code, `EditReference`") | B1a owns those three chord-receipt members; B1b owns the `curve` member and the `point-type`/`tangent-kind` arms. B1b starts only after B1a merges, so the file never has two concurrent owners. `assume:` the design intends the chord receipt with the chord track; confirm: O-M12B; if refused, the three B1a names move to B1b by a design amendment (Coordinator, after the ruling), because a track cannot pass a name whose code it may not write |
| S4 · U2 retirement hand-overs | §8 gives U2 the contract removal ("`RailEditorPane`; `BeginEdit(rail,id)`, `UpdateDraft(si)` and `PatchRail` once no caller remains, their tests ported"), but the callers are in files §14 does not give U2 (Verified by grep: `MainWindow.axaml.cs`, Desktop `Program.cs`, `FoilSource.cs`, `AuthoringSession.cs`, `FoilSourceTests.cs`, `DimensionTests.cs`, `ProjectStoreTests.cs`, `ReopenConstructionTests.cs`, `SectionEditTests.cs`, `AuthoringSessionTests.cs`, `WingEstimatesTests.cs`, `WorkbenchTests.cs`, `ShellWindowTests.cs`) | each of these files passes to U2 **for the retirement edit only**, after its M1.2b owner has merged (B0, B1a, B1b and U1a all merge before U2 starts). No other open track holds them while U2 runs (U1b owns none of them). A ported test keeps its name; a name that cannot survive needs an Owner ruling amending `app-shell.md` §12.4 **before** U2 merges |
| S5 · U1b, U2 -> U1a (merged); U1b -> U3 | a controller member missing from §5.2; `Styles.axaml` changes hands from U1b to U3 | `WorkbenchController.cs` is frozen after U1a merges. A gap is a `coord request add`; the Coordinator hands the file to the **first** requester for that edit only, never to both lanes at once. `Styles.axaml` passes to U3 at U1b's merge |
| S6 · B0 <-> B1a | B0 adds `Curve.Tangents` while B1a builds the chord fit on the `Curve` record. Recorded join lesson: "B1 changed the internal `Curve` record's positional constructor under B2 (build gate caught it)" | B0 adds `Tangents` as a non-positional member with an empty default; B1a takes tangent rows as generic linear equality rows (design §14) and never constructs `Curve` positionally. `ConstrainedFit.cs` constructs `Curve` today (Verified by the Simplifier) and §14 gives it to nobody; B1a owns it for the wave, and B0 must not edit it. The join build gate catches a breach |

### Guards over shared surfaces (GO14a)

| guard (owner) | root · recursion · token set · allowlist | other tracks it reads | jointly satisfiable with what the owner writes |
|---|---|---|---|
| `check-named-tests.py <track> --design docs/design/m12b-points.md` (PRE) | `docs/design/m12b-points.md` §9 and §12.4, one file, no recursion. Tokens: backticked `Method_State_Outcome` names, attributed to the first `(<track>` closed by `)`, `,` or `;` on the same line; `*`, `<>`, `…` fail. Also `.tmp-tests/*.log` (non-recursive): `PASS <name>`, any `FAIL`. Allowlist: none. Readiness names sit in §12.3 and are not extracted (Verified) | every coding track | yes. No track edits the design; each writes tests with exactly its names. U3 has no names and is gated otherwise |
| `check-named-tests.py <track>` default design `app-shell.md` (G0, M1.2a) | `docs/design/app-shell.md` §9 and §12.4, plus the inventory's ported-name column for D3a | U1a (`ControllerShellTests.cs`, D2's names), U2 (`ShellModelTests.cs` D1, `ShellWindowTests.cs` and `WorkbenchTests.cs` D3a) | yes, if every ported or edited test keeps its name. Renaming or deleting one reddens at the join, which is intended (S4) |
| `Architecture_DockConfinedToShell` (D3a) | root `src/`, recursive `**/*.cs` text. Tokens: `Dock.` namespaces, and `Avalonia` or `Dock` inside `CfdWorkbench.Core`. Allowlist: `src/CfdWorkbench.Desktop/Shell/**` | B0 (`PointModel.cs`), B1a, B1b (Core), U1b (`PlanCanvas.cs` at the Desktop root, outside the allowlist) | yes. Core stays Avalonia-free by design (§12.1 T3); `PlanCanvas.cs` uses Avalonia but not Dock. AXAML is not scanned |
| `xaml-token-lint` (fast ring) | `src/CfdWorkbench.Desktop`, recursive, `.xaml` and `.axaml`. Tokens: raw colours, inline `SolidColorBrush` colours, raw dimension literals. Allowlist: `is_allowed_dimension` | U1b (`ModelArea.axaml`, `Styles.axaml`), U2 (panes), U3 (`Styles.axaml`) | yes, if AXAML uses `Styles.axaml` tokens. C# brushes in `PlanCanvas.cs` are not scanned; `PlanCanvas_Brushes_AllFromThemeResources` covers them |
| `UI_DEAD_CONTROL_ShellButtonsHaveActions` (U1FIX; U2 owns `ShellWindowTests.cs` in this wave) | the `ShellHost` visual tree, every `Button` that is effectively visible and enabled; token: a bound `Command` or a `Click` handler. Allowlist: names starting `PART_` | U2's new Properties, Wing and Browser controls; U1b's canvas draws points in `Render`, not as `Button`s | yes. The sweep only covers the states it visits, so U2's exit also requires an action and an effect test per new control |
| `run-tests.sh` TEST-BUDGET (60 s wall, exit 3) | the whole suite | every track | yes (Inferred): the last measured wall is 29 s (app-shell Planned vs actual); the design estimates +6–10 s. A track over budget reports the seconds, and the Coordinator moves rendered cases to readiness, never raising the budget |
| `tools/check-spiral.py` (via `check-docs.py`) | `main..HEAD` commits; fails at ≥ 12 commits, none touching `src/` or `tests/`, and ≥ 60 % bookkeeping | every branch | yes. Every track branch starts from the integration history, which already touches `src/` and `tests/` |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| a separate retirement track for `RailEditorPane` and the old rail contracts | §8 gives it to U2, and U2 already holds the pane registrations and menus the retirement touches. A separate track would take the same files right after U2. Kept in U2 by seam S4 |
| B0 and B1a merged into one Core session | rejected: they have no data edge (§14: "tangent rows enter the chord fit as generic linear equality rows") and no shared file once S1 moves the `Run()` lines to PRE; the parallel run saves one 90 min box on the path |
| U1b and U2 serialised | rejected: disjoint files after S2, S4 and S5; both only read the controller |
| Agy for any track | two HARNESS-SILENT-EXITs on long UI work; every box here is over the ~60 min Agy bound |
| a docs-only track for the `review-suggested` flags the design raised on ADR-0001 and `design-language` | not required before dispatch; the Coordinator clears them at the wave join through `docs-graph.py` |
| a render or index track | derived work; no coordination is needed (Artifact classes) |

## Known coordination defects and mitigations

| # | defect (observed) | mitigation in this wave |
|---|---|---|
| K1 | Sub-agents committed without `AGENT_SESSION`, so the pre-commit hook went advisory ("advisory: AGENT_SESSION is unset, so nothing was checked"). This happened **four times** (class COORD-ENV, `defect-classes.md`) | every brief's first shell line is `export AGENT_SESSION=<track> AGENT_WI=<track>`, and the brief says to export it in **every** shell call, including `git commit`. Because an external harness may drop the environment (not assessed), join gate 5 reads `git diff --name-only <base>..<branch>` against the track's owned paths; a path outside them fails the join whatever the hook said |
| K2 | The 900 s leader lease expires between joins (app-shell Planned vs actual: "The leader lease (900 s TTL) expired between joins and was reclaimed at every join"). This run observed epoch 42 with 536 s left | `coord leader pin fbfa35dc` at the start of every join and before every dispatch; each track row records the epoch `coord leader who` reads **then**. An expired lease between joins is expected, not a failure. Not yet a registered class: the Coordinator registers it with a control (a renew at each join step) |
| K3 | `docs/docs-index.js` conflicts at every join | the file is `derived`; the coord-regen driver regenerates it. If a conflict marker still appears, the Coordinator runs `python3 docs/ai-forward-pack/scripts/docs-graph.py derive` on the integration tree and commits the result. Never hand-merge it. Tracks do not commit it except through `derive` |
| K4 | `coord classify init --force` from a linked worktree wrote the primary checkout (app-shell plan, Flagged) | no track runs `coord classify` or `coord install` |
| K5 | HARNESS-SILENT-EXIT: exit 0 without work | gate 4: the claimed commit on the branch and the Return section, or the cycle failed |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Coordinator (primary checkout): `coord leader pin fbfa35dc`, then `coord leader who` (read the epoch back); `coord request expire` (clears the 4 silent expiries) | seconds | P7; every row carries the epoch (CO-L) |
| 2 | Owner: ruling **O-M12B** into `docs/notes/rulings.md` (accept the design, ADR-0001 Amendment 1, this plan, seams S1–S6, the track count) | one Owner session | P5; decision edge for every track |
| 3 | Coordinator lands **PRE** inline in its own tree from `feature/ui-cad-direction` (≥ `3a37f5f`). The tree runs `coord doctor` and never `coord install` | 32 min box | P3; the join gate for every coding track |
| 4 | join PRE (gates 1, 3, plus its own evidence) → `coord doctor` in the integration tree | 10 min | the first join re-reads the merge driver (Serial spine) |
| 5 | hand-compile briefs → lane A **B0** (Grok) ∥ lane B **B1a** (Grok) | 135 min box | disjoint files after S1; no data or decision edge (GO5); watch for the untested two-Grok concurrency |
| 6 | join B1a, then B0 → **B1b** (Codex) | 140 min box | B1b needs both merged |
| 7 | join B1b → **U1a** (Codex) | 130 min box | fixes the Desktop contract |
| 8 | join U1a → lane A **U1b** (Codex) ∥ lane B **U2** (Grok) | 140 min box | disjoint files after S2, S4, S5 |
| 9 | join U1b and U2 → **U3** (Claude Opus, `/compile` brief) | 120 min box | reviews the whole; owns the native rows |
| 10 | wave join on the integration tree: every gate, the named check for **every** M1.2b track and the app-shell tracks, readiness | 30 min | proves the slice as merged, not per track |
| 11 | **Operator review** (below) | one operator session | the standing rule from the M1.2a review |
| 12 | merge to main only after step 11 passes and the readiness ring is green (pushing is out of this plan's scope) | — | readiness before any merge to main |

**Critical path:** O-M12B → PRE (32) → B0 (135) → B1b (140) → U1a (130) → U1b (140) → U3 (120) → operator review.
Sum of boxes ≈ **697 min (11.6 h)**. The same chain at the measured priors (G0 11 + P1 45 + D3a 47 + D3b 43 + D3a 47,
plus U3 unmeasured) is ≈ 3.2 h plus U3 (Inferred). B1a (90) is off the path by 45 min; U2 (135) is off it by 5 min, so
either lane of step 8 can set it.

**Join gates (the Coordinator runs them on the track branch, then again on the integration tree after the merge):**
1. `tools/run-tests.sh` **three times with identical `PASS` sets**, and every named suite's PASS set non-empty (Ruling 54
   P2, quoted: "three full run-tests.sh runs with identical PASS sets, and the compared PASS set must be non-empty for
   every named suite").
2. Coding tracks: `python3 tools/check-named-tests.py <track> --design docs/design/m12b-points.md` exits 0. After each
   merge, the integration tree **re-runs it for every M1.2b track already merged**, and re-runs the app-shell checks
   (`check-named-tests.py D1`, `D2`, `D3a` with the default design) after U1a and U2, and `C1` and `P1` after every Core
   join (B0, B1a, B1b): B1a rewrites the memo that C1's `ApplyDimension_SameOperationId_Memoized` pins, in
   `DimensionTests.cs`. PRE uses `--self-test`.
3. `python3 tools/check-docs.py` exit 0 (includes `tools/check-spiral.py`).
4. `git log` and `git status` show the claimed commit, and the final message holds the brief's Return section (K5).
5a. Proof Pack: `docs/proof/m12b/<track>.md` exists on the branch and lists, for every named test of the track, a
   recorded red run or the mutant run that turns it red. A name with neither fails the join.
5. Ownership read: `git diff --name-only <base>..<branch>` lists only the track's owned paths plus `derived` and
   `register` files (K1).
6. U1b, U2, U3: the `xaml-token-lint` check clean (fast ring).
7. A suspect merge is probed in a throwaway worktree at the pre-merge commit before a repair is delegated.
8. **Readiness before any merge to main:** `tools/run-readiness.py`, then `run-readiness.py --check` exit 0 on the
   integration HEAD, with the four M1.2b readiness checks in `join.json`, plus the app
   gates (`verify-application-core.py`, `verify-application-adapters.py`) and the recounts (Ruling 54 P5: "app gates and
   recounts run at readiness before the merge to main").
9. At the 2-cycle cap: stop the track, report to the operator, then switch approach or harness. Never a third cycle on
   the same harness.

**Cost (honest):** the orchestrator-worker shape costs about **15×** a single session (GO6). What it buys here is
**context hygiene** (Core grammar, chord numerics, session gestures, controller, rendering and panes each fit one
context; the slice does not), **harness isolation** (a stall in one tree cannot touch another), and real **independence**
in steps 5 and 8 (disjoint files, no data or decision edge). It does not buy speed on most of the path, which is serial
through PRE, B1b, U1a and U3. **The track count is not settled.** The Tech Lead was not convened (this run's fan-out cap
was 2, used by the Test Architect and the Simplifier). The casting vote goes to the Owner in O-M12B.

## Operator review (closes the wave)

Run on the packaged `.app` (macOS) after step 10, with U3's attach receipts in `docs/reviews/m12b-native.md`. The two
lists below are restated from the design on purpose: the operator's M1.2a review made "open the review with what you
will and will not see" a standing rule, and the operator reads this page, not the design. **The design §0.1 and §0.2
are the authority**; if they change, this section is re-copied at the wave join, never edited on its own.

**You will see** (design §0.1):
1. **New foil** opens the Planform workspace on the **Plan** tab: the near-elliptic planform from the top, both halves,
   leading- and trailing-edge rails as curves over a light fill, dashed station chord lines with name chips, a centre
   line, a scale bar. On the starboard half each rail shows ten points (a diamond at each end, a filled circle per
   control point) and a dashed control polygon. Properties is on the left and ends with the complete Wing block. The
   M1.2a isometric sample plot is in its own "3D samples" tab.
2. **Hover:** the Tracing probe reads η, span, % half-span, LE, TE and chord. Hovering a trailing-edge point shows a ring
   and a tooltip ("Trailing edge, point 5 of 10, control point, span 180.00 mm, aft 118.52 mm").
3. **Click:** the glyph fills; Properties shows "Trailing edge · point 5 of 10", Type: Control point, Span and Aft in mm.
4. **Drag aft:** the curve, fill and mirrored half follow; the probe shows the live Δ; MAC, Area and Aspect ratio change
   **during** the drag ("≈ preview"). Shift locks to the aft axis. Release is one undo step with a status report; ⌘Z
   returns the rail exactly.
5. **Nudge:** ↓ 0.1 mm, ⌘↓ 0.01 mm, ⇧↓ 1 mm; a held key is one undo step on release.
6. **Drag across the leading edge:** a red dashed marker shows the crossing; release puts the point back with "That would
   make the leading and trailing edges cross. The point is back where it was." Undo depth is unchanged.
7. **Type → Anchor point:** 10 → 13 points; a square on the curve with two handles, tangent Smooth; the other handle
   stays in line. Symmetric and Corner (comb break) work; each change is one undo step.
8. **Save, close, reopen:** types and tangent kinds survive; the Foil source tab shows `foildsl "4.1"` and a `tangents`
   block.
9. **Wing block → Root chord `152.09`:** one undo step and "Root chord 152.09 mm. Fit 9.01 µm (limit 10 µm). 4.21 mm
   from a straight taper. Planform moved 6.34 mm so the leading edge stays at the root." `190` is accepted with a
   **warning** (fit 22.53 µm above the 10 µm limit). Tip chord works the same; `15 cm` and `#root_chord * 0.1` are
   echoed in mm.
10. **Keyboard only:** Tab enters the Plan and moves point to point; arrows nudge; Return jumps to Span; Escape returns;
    Tab past the last target leaves the Plan. ⌥+arrows pan, ⌘= / ⌘− zoom, ⌘0 fits.
11. **Pointer navigation:** the wheel zooms about the pointer; two-finger scroll pans and pinch zooms; middle-drag or
    Shift-drag on empty canvas pans; right-click (Control-click) on a point opens Make Anchor Point / Make Control Point
    / Tangent / Fit.
12. **View ▸ Curvature comb** (C with the Plan focused) shows the comb on the selected rail, live during a drag.

**You will not see yet** (design §0.2):

| Not in M1.2b | Brought by |
|---|---|
| Section editor mode, section point types (B6 restart), Points and Messages panes, Precision workspace | M1.2c |
| Replace from catalog, Save to My sections | M1.2d |
| Floating panes, Maximize pane, saved layouts, the Review workspace | M1.2e |
| A 3D view beside the Plan (wireframe or shaded, with orbit) | M1.2b2 (Ruling 56) |
| Front, Side and Starboard elevations; editing dihedral, twist and thickness channels | M1.2b2 (Ruling 56) |
| Insert / Delete / Fair / Rebuild on rails; shape-preserving "insert anchor" | no slice yet (OI-2) |
| Moving or typing several points at once ("Mixed", read-only) | no slice yet (OI-3) |
| The Rhino navigation preset and a trackpad-mode setting | no slice yet (OI-4) |
| Comb scale and density controls, monotone-piece count, curvature readout on hover | no slice yet (OI-5) |
| A history list of edit reports | M1.2c (Messages pane) |
| A CLI `dimension` command | no slice yet (deviation D-3) |
| Windows | deferred (architecture §8) |

M1.2b is done only when every N-B row passes in this session with its attach receipt, and the UX & A11y re-review after
it clears. It is never done because the app launched.

## Gate record

`GATE plan · 2026-09-30 · test-architect (hard), the-simplifier (soft) · verdict: PASS-WITH-CONDITIONS · vetoes →
resolution: Test Architect BLOCK → cleared on re-review; Simplifier SOFT VETO → cleared on re-review (S1 and the
operator-review copy kept by written rationale)`. Each lens ran as a separate agent in Adversary Mode. The author
cleared no veto. Repair cycles used: **1 of 2**. The residual Minors and Nits from the re-reviews were applied without
another cycle. The Tech Lead was not convened, so the track count goes to the Owner in O-M12B.

| Lens | Round 1 | Main findings | Repair | Final |
|---|---|---|---|---|
| Test Architect (hard veto) | **BLOCK**: 1 Blocker, 4 Major, 2 Minor | U1a, U1b and U2 could exit on green never shown red; C1 not re-run after the Core joins; nobody owned the readiness wiring; native rows proved launch only; the re-entry control had no owner; overstated mutant claims; PRE's self-test bypassed the flag | Proof Pack rule and per-track mutants; gate 2 re-runs C1 and P1; U3 owns `join.json` readiness and gate 8 uses `--check`; per-row content assertions or "attach-ready"; U2 extends the re-entry array; mutants restated; PRE's case calls `main()` with `--design` | **Cleared** (PASS-WITH-CONDITIONS). Residual Minors applied: gate 5a checks the Proof Pack; PRE adds the `--readiness` switch |
| Simplifier (soft veto) | **SOFT VETO**: 2 Major, 3 Minor, 1 Nit | PRE delegated although §12.2 makes it the Coordinator's step; the §0.1/§0.2 copy; fallbacks that returned to a failed harness; two numbers per box; `ConstrainedFit.cs` unowned; S1 avoidable | PRE inline; the fallback rule; the design's box numbers; B1a owns `ConstrainedFit.cs`; a two-Grok stall rule. S1 and the review copy kept, with the rationale written | **Cleared**. Residual Nits applied (PRE wording, best next action) |

Residual risk: the §0.1/§0.2 copy can drift from the design, and only the re-copy rule at the wave join guards it.
U3 writes the per-row native content checks itself. Rendered and native behaviour stays **Inferred** until U1b's
first tests and U3's attach receipts exist.

## Status

| | |
|---|---|
| **Completed** | Layer state measured (`coord doctor`, `pack-doctor`, `coord session list`); preconditions checked (SHELLFIX and LEFTPANE met; the checker flag not met); the design's extraction run (187 names, 0 errors, U3 none); artifact classes; the seven tracks plus PRE scheduled with owners, boxes at 3× priors, harness and model, exit evidence per tier, seams S1–S6, GO14a guards, struck tracks, known defects, join gates and the operator review |
| **Remaining** | step 1 (leader pin, request expiry); O-M12B; every track; the operator review; registering K2 as a class with a control; clearing the design's `review-suggested` flags at the wave join |
| **Best next action** | Step 1, then the Owner's O-M12B ruling, then the Coordinator lands PRE inline |

## Planned vs actual

Filled by the Coordinator at each join: dispatch start → the branch's last commit (`git log -1 --format=%cI`).

| Track | Harness · model | Box | Measured | Outcome |
|---|---|---|---|---|
