---
id: coordination-round-oct05
title: "Coordination plan - round of 2026-10-05 (tip study, fast ring, A3a UI, A3b, A3c polars, CAD)"
type: plan
status: proposed
owner: "@cfd-leader-4e90c621"
tags: [coordination, worktrees, parallelism, analysis, a3a, a3b, a3c, test-ring, cad]
links:
  - { to: design-area3-analysis, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: depends-on }
  - { to: plan-test-cost, rel: relates-to }
  - { to: adr-0012-openfoam-backend-macos, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: coordination-m12b-build, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  Schedules the operator-approved round of 2026-10-05: the Fable tip study (A, running), the fast ring with RNG and
  Ruling 81 (B), copy markers and small findings (C), the never-built A3a desktop UI (TGL, LAY, PNA, AUX as design
  section 18 cuts it), A3b section numerics (D1), SPIKE-ANA-1 then A3c polars (D2), the section and polar displays
  (DX) and CAD fixes plus the next CAD increment (E) - at most 3 coding tracks and 2 heavy test runs at once.
---

# Coordination plan: the round of 2026-10-05

- **Scope:** the operator's kickoff prompt rev 2 (`~/projects/CFD-Workbench-kickoff-2026-10-05.md`, approved and run
  2026-10-05), plus two operator answers given during planning (2026-10-05, AskUserQuestion): **build the A3a desktop
  UI this round** ("Yes, add it") and **fold RNG into Track B** ("Fold into B").
- **Finding that reshaped the round (Verified):** A3a's desktop tracks TGL, LAY, PNA, AUX and the RNG ring track
  (`docs/design/area3-analysis.md` §18.2) never ran. `git log main` has joins for PRE, COR, STO, SVC/SVC-2, VLM,
  VLM-lattice, STP, PRJ and their follow-ups, and none for RNG/TGL/LAY/PNA/AUX. The app still shows
  `Analysis Unavailable — no method implemented.` (`src/CfdWorkbench.Desktop/ModelArea.axaml:234`), and no Desktop
  file references `AnalysisViewModel`. The section (A3b) and polar (A3c) displays plug into PNA's panes.
- **Seats:** Leader **and** Coordinator = this Claude Code session `4e90c621` (Opus 5.5). Owner = a Fable sub-agent
  (`claude-fable-5-1`), one at a time, ruling into `docs/notes/rulings.md` (class `register`) and noting "ruled by the
  Fable owner". Held for the operator: spec and copy wording, mockup approval, irreversible or outward-facing steps,
  anything the operator ruled before. Coders: Sonnet 5.5 (Claude sub-agents), Codex `gpt-6-sol`, Grok `grok-4.7`, Agy
  `gemini-3.8-flash-high`.
- **Base:** `main` = `origin/main` = `bf07a8f` (readiness green: 116.7 s of 240 s). Planning tree
  `CFD-Workbench-chore-coordination-round-oct05`.

Labels: **Verified** = observed in this run; **Inferred** = reasoned, not observed; **Flagged** = observed but suspect.

## Layer state

Measured in the primary checkout on 2026-10-05 with `coord-core.py doctor` and `pack-doctor.py --json`.

| check | result | meaning |
|---|---|---|
| registry | ok - 8 patterns | `.agents/artifacts.yml` classifies the audit views, docs index, logs and rulings (Verified) |
| merge driver | effective - coord-regen, coord-register | derived files regenerate and registers union-merge at every merge (Verified) |
| leader | was EXPIRED (epoch 53, fbfa35dc, 375,479 s) | the designation TTL is capped at 900 s and renewed every 100 s. This session reclaims it **immediately before each move of `main`** rather than holding it (Verified: `COORD-LEADER-TTL-CAP`) |
| requests | was FAIL (2 silently expired) → ok, 97 terminal, 0 open | `coord request expire` recorded both fallbacks (Verified) |
| lease overlap | none | no live leases (Verified) |
| heartbeat | 16 sessions, 0 live | old sessions only (Verified) |
| harness capability | claude: enforcing (spike S5, 2026-08-24, version not pinned); copilot: historical | **not measured here**; see the harness column |
| doorbells | not recorded | no harness was probed with `coord-mail.py dispatch`; dispatch is by direct launch |
| install | once, in the primary checkout | every worktree this plan creates **inherits** the drivers and hooks through the shared `.git/config`; run `coord doctor` in a tree to read it back; never `coord install` in a worktree |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js` | derived | `docs-graph.py derive` (coord-regen) | **none** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `audit-log.py render` (coord-regen) | **none** |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | union merge (coord-register) | **none** |
| `docs/notes/rulings.md` | register | union merge; numbers taken only through `coord-decide.py rule next` by the one Owner | **none** (the Owner is the only writer) |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | union merge | **none** |
| `docs/proof/<track>/**` | authored, new per track | each track writes only its own folder | none (disjoint) |
| `tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs` (`X.Run();` lines) | authored | append one registration line per new suite; second to merge rebases | append conflicts only (D1, D2, C may touch) |
| `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs` | authored | design S-A8: B owns the scheduling region; TGL adds the `--readiness` row; LAY and PNA fill only their own suite files | owner per region (below) |
| `DESIGN.md` | authored | C flips §7 markers first; AUX owns component rows and tokens after; DX appends new COPY rows only | serial (C → AUX/DX) |
| `src/CfdWorkbench.Desktop/WorkbenchController.cs` | authored | E2 (FLK-1) joins **before** TGL is dispatched; TGL owns it after | serial spine |
| every other authored file | authored | one owner, from the Tracks table | the owner only |

## Tracks

Boxes are 3 × a measured prior of the same class (Ruling 54 P1), from `area3-analysis.md` §18.1 where the design gives
one. Measured actual-to-box ratios on the last build ran 0.18–1.23, median 0.36. Every coding brief: foreground only,
a Return section, two repair cycles, `AGENT_SESSION` exported, Codex with `< /dev/null`, `CFD_TEST_ONLY=<name>` inner
loop, the full `tools/run-tests.sh` **once** before the Return, a red-first receipt in `docs/proof/<trk>/red-first.md`.
A track that finds a data source outside its ownership **stops and sends a seam request**.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **A** tip study (running: `wf_2c98fb91-cac`) | `docs/plans/tip-handling.md` (new) on branch `design/tip-handling` | — | T1 | 3 agents at once (Sonnet ×3 → Opus ×3 → Fable → Fable → Opus) | 9 agents; ~1–2 h wall (Inferred) | the proposal committed with V2 frontmatter; its decision requests filed to the operator; workflow journal shows which agents fell back | Workflow (Claude; enforcing per S5, not re-measured) |
| **B1** RNG (checkpoint 1 of B) | `tools/run-tests.sh` (design C-1 millisecond clocks); `tools/check-test-costs.py` (new, C-2…C-6 + `--self-test`); `WorkbenchTests.cs` scheduling region (:380–560) only if its measurement needs it | — (dispatched after the baseline profile) | T1 | 1 | **60 min** (design §18.2 RNG) | design §18.2 RNG exit: `check-test-costs.py --self-test` red on each failing input of the §13.4 table, then green; C-1…C-6 run inside every join; a **diff of every threshold constant** in the join shows none loosened | Sonnet sub-agent; owner seat `trk-b-<id>` recorded in the delegation contract at dispatch |
| **B2** fast ring under concurrent load + Ruling 81 (checkpoint 2 of B) | `tools/run-tests.sh` (after B1 joins); `View3dTests.cs` `Readiness_OrbitFrameP95Under33Ms` block (:721–750); `PlanCanvasTests.cs` `Readiness_WindowRenderPlan3d_Under33Ms` block (:1316–); `docs/coordination/join.json` ring entries; `docs/plans/test-cost.md`; `docs/proof/ring-oct05/` | B1 | T1 | 1 | **120 min** | **Baseline (Verified, this run, 3 runs at idle start):** wall 57 / 51 / 50 s inside the 60 s budget; build 5 / 1 / 0 s; Desktop 51 / 50 / 50 s (the long pole — not Core: Core parts 38–40 s); Analysis 3–4 s; Cli 1–2 s; the ring alone drives load 1.6 → 19 on 16 CPUs. The handoff's ~80 s is the ring **beside another heavy job**. Exit: the ring ≤ 60 s on 3 runs with one concurrent heavy job (a second `dotnet build` of the solution in another tree), or a stop with the numbers; profile table committed; **no check dropped** (before/after `PASS` name sets identical) and **no threshold loosened** (constants diff); every ring move states its ring and cost and test-architect clears it. Ruling 81: a planted slow frame **fails at low load** and **prints READINESS-MISS with the load (never PASS) at high load**; the threshold comes from measured data | Sonnet sub-agent (or Codex `gpt-6-sol`); owner seat recorded at dispatch |
| **C** copy markers + small findings | `DESIGN.md` §7 marker text of COPY-206…217, 221…240 only; `docs/design/m12d-catalog.md` §11.2 COPY-200…203; `tests/CfdWorkbench.Analysis.Tests/LabelsTests.cs`; `cases/tools/security-probe.sh` + its sha256 pin; `tools/check-docs.py` (STORE-SUBSET static check); `src/CfdWorkbench.Analysis/AnalysisService.cs` (linked-CTS leak, :201–215 only); `ServiceTests.cs` (IsFaulted tautology, CLI-vs-GUI key check); `src/CfdWorkbench.Cli/Program.cs` (`inspect --runs` RevisionOf); `LatticeFixtureTests.cs` F-4 helper only | — ; **joins before D1 or D2 edit `Labels.cs`, `AnalysisProjection.cs`, `LabelsTests.cs` or `AnalysisService.cs`** | T0 | 1 | **60 min** | each item: a test red on the defect then green (marker flip: LabelsTests reads "approved" for the approved ids and still "proposed — awaiting operator" for 218–220); `check-docs` green with a planted STORE-SUBSET violation red; security probe: a planted change to a probed path is reported red, then green on the clean tree (behaviour, not the re-pinned hash) | Agy `gemini-3.8-flash-high` for the pure-code items (bounded; observed-only; exit 0 is not a result) else Sonnet; owner seat recorded at dispatch |
| **E2** CAD defects | `WorkbenchController.cs` (FLK-1 lines only: :185, :726–778) and `SectionEditorView.axaml.cs` (FLK-1 state-reason lines); `src/CfdWorkbench.Core/FoilSource.cs` (`MakeIndependent` with a tangent row); their tests | — ; **the FLK-1 commit joins before TGL is dispatched**; the `MakeIndependent` fix may join after (it is off TGL's path) | T0 | 1 | **90 min** (two fixes; DSP prior 31 × 3) | `MakeIndependent_TangentRow_NoDslPatch` red then green; FLK-1: a refused step never shows Finish "Checking…" (test red then green) | Grok `grok-4.7` (observed-only); owner seat recorded at dispatch |
| **TGL** toggle, conditions band, status | as `area3-analysis.md` §18.2 row TGL (WorkbenchController, PropertiesView `ShellMode` block, ModelArea, ConditionsBand, CurvePointLayer, CommandTable, NativeMenuBuilder, StatusStrip, AnalysisToggleTests, theme rows, event allow-list lines) | **B1** (not B2), E2's FLK-1 join, PRJ ✓, SVC ✓, PVU ✓ (`1bf1fe3`) | T1 | 1 | **141 min** | §18.2 TGL exit: TGL names PASS (`check-named-tests.py TGL --design docs/design/area3-analysis.md --track-section "### 18.2" --named-sections "### 18.8"`); BC-1 for ANA-22; UX-29 S1 row; planted mutant (toggle refits the Plan camera) red | Codex `gpt-6-sol` (observed-only); owner seat recorded at dispatch |
| **LAY** layers on the views | as §18.2 row LAY | TGL | T1 | 1 | **141 min** | §18.2 LAY exit; `Readiness_CameraStep_NoPaneRefresh_Under8Ms` green with layers on | Codex `gpt-6-sol` |
| **PNA** panes and bottom panel | as §18.2 row PNA | TGL | T1 | 1 | **147 min** | §18.2 PNA exit; `verify-application-core.py`, `verify-application-adapters.py` exit 0 | Sonnet sub-agent (design named Opus; Sonnet per the operator's model split; Opus reviews) |
| **AUX** A3a review, polish, Proof Pack | as §18.2 row AUX | LAY, PNA, C (DESIGN.md) | T1 | 1 | **90 min** | §18.2 AUX exit; nine screens captured from the packaged app vs the approved mockup; hydrodynamicist re-review; readiness green for the A3a head; **operator demo** (§16 gate) | Claude persona reviewers (Opus) |
| **D1** A3b numerics | `src/CfdWorkbench.Analysis/PanelMethod.cs`, `Cavitation.cs` (new); `tests/CfdWorkbench.Analysis.Tests/PanelCpTests.cs`, `CavitationTests.cs` (new); their `AnalysisChecks.cs` lines; `docs/proof/a3b/`; **after C joins:** the cavitation screen string in `Labels.cs`, the section-tier projection in `AnalysisProjection.cs`, their rows in `LabelsTests.cs` / `ProjectionTests.cs` | — for the numerics; C for the label/projection files | T1 | 1 | **216 min** (SPT 72 × 3) | the **six** design §13.3 A3b names (:755–760) each print `PASS <name>` in `.tmp-tests/Analysis.log` — `PanelCp_KarmanTrefftz_100_200_400`, `Cavitation_ScreenString_NamesStationCount`, `Cavitation_Margin15Percent_Applied`, `Cavitation_NegCpMinNonPositive_Undefined`, `Cavitation_PvOrDepthMissing_Unavailable`, `Cavitation_GoverningStation_AtAlphaEffAndLocalDepth` (the coordinator greps each line; a missing line is red, so a never-registered test cannot pass vacuously); planted mutants from the design (TE panel dropped; N omitted; margin ignored; α_geo and h_ref used) red; CFD-V&V and hydrodynamicist reviews clear | Codex `gpt-6-sol`; owner seat recorded at dispatch |
| **D2** SPIKE-ANA-1 → A3c numerics | spike: `docs/proof/spike-ana-1/**` (probe, CSVs, verdict). On GO and clearance: `src/CfdWorkbench.Analysis/NeuralFoil*.cs` + weights resource (new), the `IPolarSource` implementation, profile drag at both Ncrit in `StripCoupler.cs`, Total drag in `Loads.cs`, Find α (ANA-05, new file), the A5.2 correction layer, the polar wiring in `AnalysisService.cs` (after C joins), polar labels in `Labels.cs` / `AnalysisProjection.cs` (after D1's label commit joins); F-13b and the A3c test files | spike: — ; A3c build: spike GO **and** the licence ruling below | T2 | 1 | spike **120 min** (GSPK box); A3c **216 min** | spike verdict with: port fidelity C# vs Python ≤ ~10⁻⁶; accuracy vs XFOIL on NACA 0012, Re 10⁶ and 2 × 10⁵, Ncrit 2/4/9 (Cl ± 0.02, ln Cd ± 0.03); licence, CST residual and model size reported. **Licence:** security-identity-architect reviews the weights' licence and the ship route (A8.5); shipping third-party weights is outward-facing, so it goes to the **operator** as a decision request; the A3c build does not start until it is ruled. A3c names each print `PASS <name>`: `FindAlpha_TargetCL_RootWithinTolerance`, `FindAlpha_NoRoot_UnavailableWithReason` (:766), F-13b (§13.2, :653), `Station_StripReadout_ReAgainstPolarRange` (:799, now with a real polar); planted mutants from the design red | Codex `gpt-6-sol` for the port; Python oracle in the `~/dev/sim` venv; owner seat recorded at dispatch |
| **DX** section and polar displays | the A3b Section tab and A3c polar/drag views in PNA's `Analysis/` files (handed over at PNA's join); their Desktop tests; **after AUX joins:** new COPY rows appended to `DESIGN.md` §7 marked "proposed — awaiting operator" and any `Styles.axaml` tokens | PNA, D1, **AUX** (for `DESIGN.md` and `Styles.axaml`); D2's A3c for the polar part | T1 | 1 | **147 min** (PNL 49 × 3) | step 1: a check that every A3b/A3c screen state is in the approved mockup (`docs/mockups/area3-analysis.html` rev 3); any state not in it is rendered and shown to the operator **before** its UI build; step 2: **DX's test names listed and cleared by the test-architect before the build** (including `Charts_TransitionAndBucket_Overlays`, :771); exit: each listed name prints `PASS <name>`; ux-accessibility review; `python3 tools/run-readiness.py --check` green for the DX head | Sonnet sub-agent or Grok; owner seat recorded at dispatch |
| **E3** next CAD increment proposal | `docs/design/<next-cad-increment>.md` (new, V2 frontmatter) and its mockup under `docs/mockups/` | E1 (the operator's native review) informs it; not blocked by it | T1 | 1 | **90 min** | a design note naming the unbuilt CAD-*/GEO-* rows it closes, the smallest increment, and a rendered mockup; `python3 tools/check-docs.py` exit 0 with the new node in `docs/docs-index.js`; marine-cad-ux-expert review; operator sees the mockup before any build | Opus persona agents (marine-cad-ux-expert lead); owner seat recorded at dispatch |

**E1** (the operator's native review of M1.2d with the packaged app) is not a track: it is operator time. Its findings
become E2-class fixes, scheduled by ownership at the time they arrive.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| B's baseline profile | it measures timing; any other build on the machine changes the answer (GO5 c: the CPU is a shared exclusive resource) | Coordinator inline, before any coding dispatch — **done 2026-10-05** (B2 row) |
| B1 → TGL | design §18.2: TGL depends on RNG; RNG owns `run-tests.sh` and the WorkbenchTests scheduling region that TGL's readiness row lands in. Only B1 (RNG) gates TGL; B2 runs beside TGL | B1, then TGL |
| E2 (FLK-1) → TGL | FLK-1 is in `WorkbenchController.cs` (`SectionChecking`, :185, :726–778), which TGL owns; two authors of one file fail GO5 (a) | E2's FLK-1 commit joins first |
| TGL → LAY ∥ PNA → AUX | design §18.4 order; TGL fixes the area state and `ShellMode` both read | as design |
| PNA → DX | DX fills PNA's panes; the pane shape is PNA's interface (GO5 a) | PNA, then DX |
| C → AUX → DX on `DESIGN.md` (and AUX → DX on `Styles.axaml`) | C flips markers; AUX then owns component rows and tokens; DX appends COPY rows and any tokens **after AUX joins**. One writer at a time, in this order | C, AUX, DX |
| C → D1/D2 on `Labels.cs`, `AnalysisProjection.cs`, `LabelsTests.cs`, `AnalysisService.cs` | C edits `LabelsTests.cs` and `AnalysisService.cs`; D1 and D2 need those files plus the projection for their labels and wiring. D1's numerics files do not wait | C, then D1's label commit, then D2's |
| SPIKE-ANA-1 → A3c build | a decision edge (GO5 b): NO-GO changes the whole A3c shape | D2 |
| every move of `main` | one leader; readiness green for that HEAD; reclaim the designation first | Leader |
| heavy test runs | at most 2 at once on this laptop (fast ring and 3D frame checks fail under load; readiness went 1.79 → 7.04 load by itself) | Coordinator staggers the full-ring runs and joins |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| C -> AUX -> DX | `DESIGN.md` §7 markers flipped first; AUX's component rows and tokens next | C joins before AUX edits `DESIGN.md`; DX edits `DESIGN.md` and `Styles.axaml` only after AUX joins; a token DX needs earlier is a seam request to AUX |
| C -> D1, D2 | `LabelsTests.cs`, `AnalysisService.cs` | C joins before D1's label commit and D2's service wiring |
| E2 -> TGL | `WorkbenchController.cs` FLK-1 lines | E2 joins before TGL dispatch; TGL rebases |
| B1, B2 -> every track | the fast ring's new shape (`run-tests.sh`, parts, cost checks) | each joins; later tracks run the new ring; a track already in flight rebases at its join |
| D1, D2, C -> `AnalysisChecks.cs` | one registration line each | append; the second to merge rebases |
| D2 (A3c) -> STP files | profile drag in `StripCoupler.cs`, Total drag in `Loads.cs` | D2 owns those regions during A3c; no other track touches them this round |
| D2 -> A (tip) | how the tip verdict uses a real section envelope | Track A's proposal states it; any tip code waits for the operator's ruling |
| PNA -> DX | Section tab and polar view hosts | PNA hands over the `Analysis/` pane files at its join |
| LAY -> PNA | a theme row LAY needs (design S-A9) | seam request to PNA |
| any -> Owner | a decision request | `coord-decide.py request --to <Fable owner seat>`; spec/copy/mockup items go to the operator, batched |

**Joint satisfiability of shared surfaces (GO14a).** `AnalysisChecks.cs`: the scan is `tools/check-named-tests.py`
over the design's §18.8 / §13.3 names (root `tests/`, recursive, token `PASS <name>`, allowlist none); appending a line
never removes a name, so all three appenders satisfy it jointly. `DESIGN.md`: C edits only marker suffixes on existing
rows; `LabelsTests` (C's own guard) reads row text + marker; AUX's and DX's edits add rows or component sections and
leave existing COPY rows' text intact, so C's guard stays green after them. `WorkbenchTests.cs`: B's scheduling region
and TGL's registration row are disjoint line ranges (S-A8).

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| RNG as its own track | operator folded it into B (2026-10-05); both own `run-tests.sh`, so separate tracks would be two authors of one file |
| Ruling 81 as its own track | same files as B's ring work (the two readiness checks); merged into B |
| A3b UI and A3c UI as two tracks | both fill PNA's panes in sequence; one track (DX) holds the pane files once |
| C split into "copy" and "findings" | each part is under 30 min; two sessions would double context load for no isolation gain |
| LAY merged into PNA | not struck: the design runs them in parallel on disjoint files (§18.4), and both are ≥ 140 min boxes |

**Cost.** An orchestrator-worker round costs roughly 15× a single session in tokens (GO6). This round pays it for
**genuine independence** (D1, D2 numerics vs the UI chain share no files), **machine time** (D2's spike and Track A run
unattended), **context hygiene** (each UI track carries its own §18 trace), and **isolation** (B's profiling). The
UI chain itself is serial; its width is 2 only at LAY ∥ PNA.

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Track A runs (launched before this plan) | 9 agents, no builds | docs-only; no contention |
| 2 | B baseline profile, Coordinator inline, quiet machine | ~3 min — **done** (B2 row) | must precede any coding dispatch |
| 3 | Commit and join this plan (readiness green) | ~5 min | the briefs cite it |
| 4 | Dispatch wave 1 (≤ 3 coding): **B1** (Sonnet), **E2** (Grok, FLK-1 first), **C** (Agy) | boxes 60 / 90 / 60 | B1 and E2 gate TGL; C gates D1's labels, D2's wiring, AUX and DX on `DESIGN.md` |
| 5 | As slots free: **D1** numerics (Codex), **D2 spike** (Codex) | 216 / 120 | independent of the UI chain; the spike is a decision edge for A3c |
| 6 | Join E2 (FLK-1), B1, C as each clears review (readiness green; reclaim leader; ff + push) | per join ~5 min + readiness ~2 min | TGL needs E2 + B1 |
| 7 | Dispatch **TGL** (Codex) and **B2** (Sonnet) | 141 / 120 | TGL is the critical path; B2 is off it |
| 8 | **E3** proposal (Opus personas, docs only) whenever a Claude slot is free; operator's **E1** review any time | 90 | not on any build path |
| 9 | On TGL join: **LAY** (Codex) ∥ **PNA** (Sonnet) | 141 / 147 | design §18.4 |
| 10 | Spike verdict → GO: licence review → operator ruling → **A3c** build (Codex); NO-GO: options to the operator | 216 | decision edge |
| 11 | On LAY + PNA + C: **AUX** and the operator demo | 90 | design §16 gate |
| 12 | On PNA + D1 + AUX (+ A3c for the polar part): **DX** — mockup check, test names cleared, operator approval for missing states, then build | 147 | needs the panes and AUX's `DESIGN.md`/`Styles.axaml` |
| 13 | Close: Completed / Remaining / Next, planned vs actual per track, one list of open operator decisions | — | — |

**Critical path (Inferred from boxes):** B1 60 → TGL 141 → PNA 147 → AUX 90 → DX 147 ≈ 585 min of boxes; at the
measured median ratio 0.36, ≈ 3.5 h. Splitting B moved 120 box-minutes off the path; serialising DX after AUX put 90
back on it, which is the price of one writer on `DESIGN.md`.

## Review record (Stage 8 DISCONFIRM)

Reviewer: test-architect + the-simplifier persona (Sonnet sub-agent, Adversary Mode), 2026-10-05, read the plan and
`area3-analysis.md`. Verdict **CLEAR WITH CONDITIONS**; all nine findings applied in this revision:
(1) D1/D2/DX name checks could pass vacuously → explicit name lists, each line grepped as `PASS <name>`; DX names cleared
before build · (2) "six `Cavitation_*`" was wrong → five plus `PanelCp` · (3) unowned `Labels.cs` /
`AnalysisProjection.cs` / service wiring → assigned to D1/D2 after C · (4) `DESIGN.md` order contradicted itself → strict
C → AUX → DX, `Styles.axaml` seam · (5) TGL over-serialised on all of B → B split into B1/B2 · (6) contention spot-checks
confirmed · (7) weak exits → security-probe behaviour, E3 `check-docs`, Ruling 81 high-load MISS, thresholds diff ·
(8) owner seat per row recorded at dispatch · (9) licence clearance owner → security review, then the operator.
Simplifier: no track struck; E2's `MakeIndependent` fix decoupled from the TGL gate; E3 kept because the operator asked
for CAD next steps.

**Termination.** Each track ends at its exit evidence, at its box (a box firing is a defect signal and stops the
track with a report), or at its second failed repair cycle. A decision request ends at its deadline through its
fallback. The round ends when the prompt's done-when list is met or every remaining item is waiting on the operator.

## Planned vs actual (Stage 7, 2026-10-06)

Measured minutes are the sum of `duration_seconds` on each track session's audit entries
(`docs/audit/audit-log.jsonl`); the box is the brief's wall budget. 47 joins recorded; `main` at `1914a58a`, readiness
green, pushed. Requests this round: 32 raised, 30 resolved, 2 expired (`.agents/requests.jsonl`); Rulings 78–100.

| track | measured min | box min | ratio | note |
|---|---|---|---|---|
| A tip (workflow) | 30 | 120 | 0.25 | judge panel; proposal `docs/plans/tip-handling.md`, Rulings 88, 91–96 |
| B1 | 16 | 60 | 0.27 | |
| B1b | 16 | 60 | 0.26 | |
| B2–B5 | 198 | 120 | 1.65 | one session id for four tasks and five ring runs, incl. the failed Desktop split; not one overrun |
| C | 17 | 60 | 0.28 | Agy stalled; Sonnet finished in the same tree |
| D1 (A3b) | 56 | 216 | 0.26 | Codex at capacity; Sonnet finished |
| D2 (SPIKE-ANA-1) | 3 | 120 | 0.02 | **under-recorded**: its Return says about 33 min; the start marker was consumed out of order (AC-09 shape) |
| A3c-1 | 25 | 180 | 0.14 | Codex at capacity; Sonnet finished |
| E2 | 74 | 90 | 0.82 | repair cycle 2 reverted a grammar widening (BRIEF-FIXTURE-AGAINST-SPEC) |
| E3 | 14 | 90 | 0.15 | |
| E4 | 9 | 100 | 0.09 | mockup awaits the operator |
| DXM | 19 | 100 | 0.19 | mockup awaits the operator |
| HK | 4 | 45 | 0.09 | hook cwd fix |
| S1 / S2 / S4 / S6 | 16 / 4 / 17 / 89 | 90 / 75 / 90 / 120 | 0.18 / 0.05 / 0.19 / 0.74 | S6 = W2c tip mesh, NO-GO |
| TGL | 93 | 141 | 0.66 | two ownership stops |
| CTX | 26 | 75 | 0.35 | |
| HIST | 9 | 90 | 0.10 | |
| LAY | 39 | 141 | 0.28 | two ownership stops |
| PNA | 49 | 147 | 0.33 | |
| AUX | 17 | 90 | 0.19 | |
| LIM | 27 | 150 | 0.18 | |
| SEAM | 86 | 180 | 0.48 | one schema stop, answered by Ruling 100 |
| POL | 32 | 120 | 0.27 | 6 of 7 items; Properties scroll partial |
| CI | 15 | 90 | 0.17 | 4 classes, 3 controls, 1 hook proposal |
| docs | 5 | 60 | 0.08 | |

**What paid.** Most tracks ran at 0.1–0.5 of the box, which matches the 0.36 median of the previous build, so boxes
are still about 2–3× too large for Sonnet build tracks. The parallelism that paid was machine time: with the ring lock (2
slots), POL and CI built and tested at the same time, and both joins' rings recorded 0 COST-MISS (CI's own ring had
one C-5 miss at load 21.66 that did not recur at the join). Track splits that did
not pay: TGL and LAY each stopped twice on files outside their ownership (OWNERSHIP-MISSES-DATA-SOURCE, aid
`tools/trace-brief.py`). Harness substitutions: Agy (C) and Codex (D1, A3c-1) each failed once, and Sonnet finished in
the same tree; Sonnet carried every later build track.

**Join friction.** `docs/docs-index.js` conflicted on every join until `coord install` bound the derived and register
patterns (DERIVED-UNBOUND, `90cf9f94`); after that, two joins (POL, CI) stopped at step 4 because the checks ran before
the owed regeneration (JOIN-CHECK-BEFORE-REGEN). `join.json` now regenerates first, guarded by TEST-RING.

**Next plan inputs.** Box Sonnet build tracks at about 0.4× today's estimate; run `tools/trace-brief.py` before every
dispatch; mark `audit-log.py start` again on any resumed node; give each task its own session id so B2-style pooling
does not hide per-task cost.
