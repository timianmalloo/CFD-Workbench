---
id: coordination-wave11-byok-assistants-and-prior-carryforwards
title: "Coordination plan - wave 11: Cross-Area Live BYOK LLM Assistants and Prior-Wave Non-Accessibility Carry-Forwards"
type: plan
status: active
owner: "@timianmalloo"
tags: [coordination, worktrees, ai, byok, assistant, security, evals, agy-swarm, wave11]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: kb-hw-integration-and-ai-workflows, rel: depends-on }
  - { to: design-guided-solver-setup, rel: depends-on }
  - { to: design-cfd-run-pipeline, rel: depends-on }
  - { to: design-results-and-candidates, rel: depends-on }
  - { to: coordination-wave10-cfd-run-results-and-rail, rel: refines }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-24"
summary: >-
  Wave 11 delivers the complete cross-area BYOK LLM assistant stack (AI-01..AI-11, A5.12, A8.5, A8.6, SET-02/03)
  plus verified non-accessibility prior-wave carry-forwards across four serial AGY swarm tracks: W11-DESIGN freezes
  the architecture and contracts; W11-CORE-AI ships pure STORE-SUBSET Core contracts, PromptRedactor (4 kB/32 kB),
  deterministic T0 validators across all 6 active areas, hashed BM25 KnowledgeRetriever, 9-item A8.6 eval suite, and
  CF-W8-1 verify-open-measure.py math.isfinite hardening; W11-RUNTIME-AI ships OS credential store (Windows Credential
  Manager DPAPI / macOS Keychain), closed-DTO AssistantSettingsStore, BCL HttpClient ByokLlmTransport (Anthropic +
  OpenAI-compatible, loopback-tested), AssistantService, and CLI; W11-DESKTOP-AI wires Settings, consent preview, and
  assistant entry bars across all 7 areas plus Wave 10+11 Light/Dark/HC theme verification (CF-W7-1).
---

# Coordination plan: wave 11 — Cross-Area Live BYOK LLM Assistants and Prior-Wave Non-Accessibility Carry-Forwards

**Input.** Operator directive of 2026-11-24 following Wave 10 push (`d89d727f`) and **Ruling 276** (`8f147c46`, 0
unapproved `COPY-*` rows remaining in `DESIGN.md`): plan and execute **Wave 11** (cross-area live BYOK LLM assistants)
plus all executable non-accessibility outstanding tasks from prior waves autonomously via `/prepare-for-coordination`
and `/execute-with-coordination` (AGY swarm).

**Goal state.**
- **Goal:** Ship the complete end-to-end **Wave 11 Cross-Area BYOK LLM Assistant** stack (`AI-01`–`AI-11`, `A5.12`,
  `A8.1`, `A8.5`, `A8.6`, `SET-02`, `SET-03`) and close all executable non-accessibility prior-wave carry-forwards on
  `main`:
  1. **`W11-DESIGN`:** Adversary-cleared design slice `docs/design/byok-assistants.{md,html}` freezing the E7 surface
     list, project-graph-respecting boundaries (`Core/Assistant` pure zero-I/O → `Persistence/Assistant` OS credentials
     & closed-DTO settings/audit → `Analysis/Assistant` BCL `HttpClient` transport & orchestrator → `Cli` → `Desktop`),
     all 7 area entry contracts, `PromptRedactor` 4 kB/32 kB rules, 9-item `A8.6` eval suite & versioned thresholds,
     and `COPY-866..915` inventory.
  2. **`W11-CORE-AI`:** Pure `STORE-SUBSET` `src/CfdWorkbench.Core/Assistant/**` (`AssistantContracts`,
     `PromptRedactor` taking a pure `RedactionContext` with UTF-8 byte-exact 4,096 B / 32,768 B rune-safe caps and
     delimiter-breakout neutralization, `AssistantValidators` for Setup/CAD/Analysis/Experiment/Run/Results composing
     with `SetupAssistantValidator`, hashed embedded `KnowledgeRetriever` BM25 index failing closed on tamper, and
     9-item `AssistantEvalSuite` enforcing `AI-06` `"Unevaluated on this model"` on model-id, provider/protocol, or
     `EvalThresholdsVersion` change), plus prior-wave carry-forward **`CF-W8-1`**
     (`tools/spikes/cad-export-measure/verify-open-measure.py:40` `math.isfinite` + `json.loads(..., parse_constant=...)`
     hardening + `tests/test_cad_export_measure.py`).
  3. **`W11-RUNTIME-AI`:** OS credential store (`src/CfdWorkbench.Persistence/Assistant/OsCredentialStore.cs`: Windows
     `advapi32.dll` `[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]` `CredWriteW`/`CredReadW`/`CredDeleteW`
     /`CredFree` DPAPI-protected `CRED_TYPE_GENERIC` `CRED_PERSIST_LOCAL_MACHINE` `<= 2560` B with `CredFree` and
     `CryptographicOperations.ZeroMemory` in `finally`; macOS `/usr/bin/security -i` via **stdin** — never passing the
     secret on process `argv` — or `Security.framework` P/Invoke; fail-closed elsewhere; zero key in files/docs/logs/
     exports/env), closed-DTO `AssistantSettingsStore.cs` (`cfdw-assistant` v1 `assistant.json` + delete-sharing-safe
     append-only `assistant-audit.jsonl`, private DACL/`0700` directory, symlink/reparse refusal, strictly rejecting any
     secret/key field, UTC daily cap tracking), zero-new-NuGet `ByokLlmTransport.cs` (`.NET 10` `HttpClient` with
     `AllowAutoRedirect = false`, default TLS validation, `<= 1 MiB` response bound, Anthropic `/v1/messages` &
     OpenAI-compatible `/v1/chat/completions`, strictly one configured endpoint rejecting `userinfo@`, `0.0.0.0`, and
     host lookalikes like `localhost.evil.com`, HTTPS-only except exact loopback `127.0.0.1`/`::1`/`localhost`, key
     scrubbing on errors), `AssistantService.cs` (binding execution to the consented UTF-8 `payloadSha256`), CLI
     `assistant status|eval|preview|audit --json`, real loopback `HttpListener` wire & negative security tests in
     `Analysis.Tests`, and real Windows Credential Manager + Persistence/CLI tests in `Cli.Tests`.
  4. **`W11-DESKTOP-AI`:** `AssistantSettingsDialog` (`DialogChrome`-conformant modal opened from ActivityRail footer
     **Settings** button via `ShellHost.ShowSettingsDialog` + `CommandTable` `view.settings` `Modal: true` + every
     area's `"Configure key"` action; password-masked OS key Save/Remove taking effect without restart; endpoint/model/
     caps; live `AI-06` 9-item eval runner; local audit viewer), `AssistantConsentPreviewDialog` (`DialogChrome`-
     conformant byte-exact redacted payload preview, per-field Stated/Inferred/Defaulted diff, Accept/Discard/Refresh-
     on-base-drift, withheld-numeral reason), reusable `AssistantPromptBar` for new assistant surfaces (Area 2 CAD
     `AI-07`, Area 3 Analysis `AI-08`, Area 4 Experiment `AI-09` preserving 0 Run buttons per `XS-03`/`UX-17`) while
     keeping existing test-pinned `x:Name` trees in place on Area 1 Setup (`AI-02`/`SET-02` + `SetupController.cs`),
     Area 5 Run (`AI-05`) & Solver Setup (`AI-11`), and Area 6 Results (`AI-10`), plus Area 7 Export explicit
     `"No assistant action here."` note (`A5.12`), `DESIGN.md` `COPY-866..915` proposed, and `--wave11-ai` Desktop
     harness covering all 7 areas + Light/Dark/HighContrast theme brush verification across Wave 10+11 surfaces
     (`CF-W7-1`).
- **Done when:** All four serial tracks (`W11-DESIGN` → `W11-CORE-AI` → `W11-RUNTIME-AI` → `W11-DESKTOP-AI`) pass
  red-first tests and live mutants, clear independent Adversary verification (`w11-verifier`: Security & Identity HARD
  VETO + Hydrodynamicist + Data & Persistence + Marine-CAD UX + Test Architect HARD VETO + Simplifier), and merge
  serially onto `main` via `conductor-join.py --no-push` with `TESTS-GREEN`, `xaml-token-lint.py` clean,
  `check-event-subscribers.py` 0 findings, `UI-CS-TOKEN` 0 findings, recursive `STORE-SUBSET` ok, `check-proof-pii.py`
  0 hits, `design-lint.py DESIGN.md --strict` 0 warnings, `check-docs.py` exit 0, and `10/10` `run-verify-gates.py`
  PASS; swarm retired and Planned vs Actual committed on `main`.
- **Not in scope (honestly Struck / Externally blocked / Operator-excluded):**
  - Accessibility / screen-reader tracks (NVDA / VoiceOver / Narrator) — explicitly excluded per operator directive of
    2026-11-23 & 2026-11-24.
  - Live third-party cloud LLM calls during automated tests — no real third-party API keys are stored or spent in CI;
    real HTTP/JSON/header/error/redaction wire paths are proven end-to-end against a real local loopback `HttpListener`
    server (`http://127.0.0.1`), and zero fabricated cloud receipts are claimed.
  - `A5-RESUME` (live WSL `openfoam2512` download & `petsc4Foam`) — externally blocked by Cisco Umbrella TLS inspection
    (`net.proxy-tls` on `sourceforge.net`).
  - `SPIKE-CAD-02` commercial/maker CAD open-and-measure receipts (`Fusion`, `FreeCAD CAM`, `Vectric VCarve/Aspire`,
    `Rhino 8`) — requires interactive third-party GUI CAD software outside this repo to populate
    `docs/proof/cad-export-measure/cad-open-measure.json`, lift `COPY-481`, and accept `ADR-0013`.
  - Live ParaView 5.12+ binary execution (`PARAVIEW_FOUND=False` on this host) and Authenticode / macOS codesign
    notarization (no signing certs in repo).
  - Pushing new Wave 11 commits on `main` to `origin` without operator confirmation.
- **Tier:** T2 · **Fan-out cap:** 1 concurrent author (width 1 across 4 serial `share` tracks — zero merge conflicts on
  shared `Labels.cs`, `Program.cs`, `Styles.axaml`, `DESIGN.md`, `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`,
  `WorkbenchTests.cs`, and zero real-display Desktop harness contention).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w11-coord`). Owns plan, seams, join order, `conductor-join.py --no-push`
  merges, and closeout. Never authors track code.
- **Owner:** Independent swarm worker `w11-owner` (`workspace: "inherit"`, read-only on `src/`), spawned only if a
  mid-wave decision request arises.
- **Adversary Verifier:** Independent swarm worker `w11-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track in Adversary Mode (Security & Identity HARD VETO + Hydrodynamicist + Data & Persistence + Marine-CAD UX + Test
  Architect HARD VETO + Simplifier) before join.
- **Stall Monitor:** Swarm worker `w11-monitor` (`workspace: "inherit"`, loads `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3, 5, 6, 7, 8, and 10).

## Layer state

Measured 2026-11-24T18:42Z in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `8f147c46` after Wave 10 push
`d89d727f` and Ruling 276 `8f147c46`, clean working tree).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| sessions | `0 active session(s); 249 file(s) scanned` | clean start |
| requests | `44 terminal requests, 0 open` | zero open decision requests after Ruling 276 |
| worktrees | `1 (D:/Projects/CFD-Workbench 8f147c46 [main])` | primary only |
| `DESIGN.md` high-water mark | `COPY-865` (0 unapproved rows after Ruling 276) | Wave 11 reserves `COPY-866..915` (proposed) |
| NuGet / lockfile policy | `CfdWorkbench.Desktop.csproj` has `RestorePackagesWithLockFile=true`; `.NET 10` BCL `HttpClient` + Win32 `advapi32.dll` / macOS `security` need **0 new NuGet packages** | zero supply-chain/lockfile churn; climbs Solution-Selection Ladder (stdlib/BCL first) |
| Project reference DAG | `Core` (0 refs) ← `Persistence` (`Core`, `AllowUnsafeBlocks=true`) · `Analysis` (`Core` only) ← `Cli` (`Core`, `Persistence`, `Analysis`) ← `Desktop` (`Cli`, `Analysis`); `Cli.Tests` refs `Cli` + `Analysis` (transitively `Persistence`) | `Core/Assistant` stays pure zero-I/O; `Persistence/Assistant` owns OS credentials & disk settings/audit tested in `Cli.Tests`; `Analysis/Assistant` owns `HttpClient` transport & service tested in `Analysis.Tests` |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifier/monitor) | each track gets a clean linked working copy; `git config core.longpaths true` active |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token wakeup; `w11-monitor` attached |
| agy ownership hooks (`.agents/hooks.json`) | `observed-only` | `pre-commit` floor + coordinator diff inspection enforce path boundaries |
| pack CLI mail doorbells (`coord-mail.py`) | `unsupported` | board `@mention` + `swarm_wait` is the sole enforced wakeup mechanism |

The coordination layer is installed **once, in the primary checkout** (`D:/Projects/CFD-Workbench`). Every share
workspace created by `swarm{action:"add", workspace:"share"}` **inherits** `.git/config` merge drivers and `.git/hooks`
from the primary checkout — workers verify inherited drivers via `git config --get-regexp 'merge\..*\.driver'` and
**never** run `coord install` inside a share workspace.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | `coord-register` union merge | **no** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen`: `audit-log.py --root docs --project CFD-Workbench render` | **no** |
| `docs/docs-index.js` | derived | `coord-regen`: `docs-graph.py derive` | **no** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | `coord-register` union merge | **no** |
| `docs/lessons/defect-classes.md` | register | `defect-register` (`tools/merge-defect-register.py`) | **no** |
| `docs/notes/rulings.md` | authored, single writer | Coordinator / Owner only | serial |
| `docs/design/byok-assistants.{md,html} (new)` | authored, single owner | `W11-DESIGN` owns in Wave 11A | serial |
| `src/CfdWorkbench.Core/Assistant/** (new — pure zero-I/O)`, `src/CfdWorkbench.Core/CatalogData/KnowledgeCorpus.json (new)`, `tools/spikes/cad-export-measure/verify-open-measure.py` (`CF-W8-1`), `tests/test_cad_export_measure.py (new)` | authored, single owner | `W11-CORE-AI` owns in Wave 11B (after `W11-DESIGN` joins) | serial |
| `src/CfdWorkbench.Persistence/Assistant/** (new — OS credential store + closed-DTO settings/audit)`, `src/CfdWorkbench.Analysis/Assistant/** (new — BCL HttpClient transport + orchestrator)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{AssistantVerb.cs (new),Program.cs}` | authored, single owner | `W11-RUNTIME-AI` owns in Wave 11C (after `W11-CORE-AI` joins) | serial |
| `src/CfdWorkbench.Desktop/Assistant/** (new)`, `src/CfdWorkbench.Desktop/{ActivityRail.axaml(.cs),ModelArea.axaml(.cs),WorkbenchController.cs,MainWindow.axaml.cs,ExportDialog.axaml,Styles.axaml (non-theme tokens only)}`, `src/CfdWorkbench.Desktop/Shell/{CommandTable.cs,ShellHost.cs}`, `src/CfdWorkbench.Desktop/{Setup/SetupController.cs,Setup/SetupWorkspaceView.axaml(.cs),Analysis/AnalysisPanel.axaml(.cs),Sweeps/ExperimentSetupView.axaml(.cs),Run/RunWorkspaceView.axaml(.cs),SolverSetup/SolverSetupView.axaml(.cs),Results/ResultsWorkspaceView.axaml(.cs)}`, `DESIGN.md` (`COPY-866..915` proposed only) | authored, single owner | `W11-DESKTOP-AI` owns in Wave 11D (after `W11-RUNTIME-AI` joins) | serial |
| `tests/**` (`IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, `ShellModelTests.cs`, `ControllerSectionTests.cs`, `WorkbenchTests.cs`, plus new test files) | authored, serial | strictly one owner per wave (width 1) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- Strictly **one** author active per wave (`W11-DESIGN` → `W11-CORE-AI` → `W11-RUNTIME-AI` → `W11-DESKTOP-AI`): zero
  concurrent writers on `Labels.cs`, `Program.cs`, `Styles.axaml`, `DESIGN.md`, `IdentityTests.cs`,
  `AnalysisChecks.cs`, `CliTests.cs`, `ShellModelTests.cs`, `WorkbenchTests.cs`, or the real-display Desktop harness.
- **Recursive `tools/store_subset.py` (`STORE-SUBSET`, run via `tools/check-docs.py`):**
  - Root: `src/CfdWorkbench.Core/` and `tests/CfdWorkbench.Core.Tests/`, recursive `.rglob("*.cs")` skipping `bin/`/`obj/`.
  - Token set: `File.` (except `ReadAllBytes`/`ReadAllText`), `Directory.` (except `EnumerateFiles`/`GetFiles`),
    `FileStream`, `FileInfo`, `GetTempPath`, `GetEnvironmentVariable`, `DllImport`, `LibraryImport`, `ProjectStore`,
    `CfdWorkbench.Persistence` (except `InternalsVisibleTo`).
  - Allowlist: 6 `PARTITION_EXEMPT` files only (`ProjectStoreTests.cs`, `LayoutFileTests.cs`, `PreferenceStoreTests.cs`,
    `SectionLibraryTests.cs`, `IdentityTests.cs`, `WindowsProjectStoreTests.cs`).
  - **Satisfaction:** `src/CfdWorkbench.Core/Assistant/**` and `tests/CfdWorkbench.Core.Tests/Assistant*.cs` are 100%
    pure zero-I/O (read embedded `KnowledgeCorpus.json` via `Assembly.GetManifestResourceStream`, zero `File.*`/PInvoke/
    `Persistence` references). ALL OS credential P/Invoke (`advapi32.dll` / macOS `security`) and file persistence live
    in `src/CfdWorkbench.Persistence/Assistant/**` and are tested in `tests/CfdWorkbench.Cli.Tests/AssistantPersistenceAndCliTests.cs`
    (outside `Core.Tests`, keeping `PARTITION_EXEMPT` untouched). Jointly satisfiable.
- **`ArchitectureTests.cs` (`Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt`):**
  - Root: compiled IL of `CfdWorkbench.Analysis.dll`; fails on ANY call to a `CfdWorkbench.Core` method named
    `ProfileAt`, `Cancel`, `Undo`, `Redo`, or prefixed `Begin*`, `Update*`, `Apply*`.
  - **Satisfaction:** ALL `Core/Assistant/` method names invoked by `CfdWorkbench.Analysis` avoid those names/prefixes
    (use `EvaluateSuite`, `RedactPayload`, `ValidateProposal`, `ValidateExplanation`, `RecordAuditEntry`,
    `BuildGeometryEditDraft`, `SaveSettings`, etc.). Jointly satisfiable.
- **`xaml-token-lint.py`, `tools/check-event-subscribers.py`, `Styles.axaml` closed theme-brush set
  (`tools/verify-application-adapters.py:123-130`), `TokenDisciplineTests.cs` (`UI-CS-TOKEN`), and `ActivityRail` /
  `CommandTable` / `ModelArea.Navbar` guards:**
  - `xaml-token-lint.py`: root `src/CfdWorkbench.Desktop/` (`.xaml`/`.axaml`), zero raw colors/sizes, allowlist none.
  - `tools/check-event-subscribers.py`: root `src/` (`*.cs`), every `event` declaration under `src/` must have ≥ 1
    `<name> +=` subscriber under `src/`; 4-entry `ALLOWED` set.
  - `Styles.axaml` theme dictionaries (`Light`/`Dark`/`HighContrast`): **ZERO** new `<SolidColorBrush x:Key="...Brush">`
    keys added inside theme dictionaries; reuse existing theme brush keys only.
  - `TokenDisciplineTests.cs`: root `src/CfdWorkbench.Desktop/` (`**/*.cs`), all resource keys resolve, zero
    `FontSize < 11` / `DrawText`/`FormattedText` `< 11`, zero `Color.Parse(...)`, allowlist empty.
  - `ActivityRail.axaml(.cs)`: ZERO `Dock.*` references (`Architecture_DockConfinedToShell`); adding footer **Settings**
    button stays inside `Grid.Row="1"` footer without changing `ShellHost`'s `Auto,*` 3-row / `ColumnSpan=2` layout.
  - `CommandTable.cs`: `view.settings` added under `"View"` with **`Modal: true`**, routed through `ShellHost.ShowSettingsDialog` (`public Func<Task>? ShowSettingsDialog { get; set; }`) and hooked in BOTH `ControllerSectionTests.Commands_ModalFlag_MatchesRowsThatShowADialog` (`ControllerSectionTests.cs:755-757`) AND `ControllerSectionTests.AutomationState_HasRenderedState` (`ControllerSectionTests.cs:815-821`), plus `"view.settings"` added to the gesture-less allowlist in `ShellModelTests.cs:414` (`C4`).
  - Existing Desktop test invariants (`C5`):
    - Keep exact `x:Name` controls directly in `SetupWorkspaceView.axaml` (`LanguagePromptBox`, `LanguageSubmitButton`, `LanguageNoKeyText`, `LanguageCardTitle` — `SetupViewTests.cs:57-73`) and `SolverSetupView.axaml` (`AssistantOfflineCard`, `AssistantPromptBox`, `AssistantResultCard`, `AssistantVerdictText` — `SolverSetupViewTests.cs:341-368`), and isolate default `WorkbenchController`/`ShellHost` tests from host OS credentials so `HasApiKey` defaults `false`.
    - `AnalysisPanel`: keep exact 5 `TabItem` headers and `LoadingPlot.Bounds.Height >= 82` (`AnalysisPanelTests.cs:142-143,395,576-597`); place compact `"Ask about this calculation"` trigger beside `SolverSetupButton`.
    - `ModelArea`: keep 4-button `Navbar` (`Setup | CAD | Analysis | Sweeps`) and `ViewArrangementGrid`↔`NavSetupButton` Tab adjacency & `PlanContent` pixel frame (`ControllerViewTests.cs:1027-1053,1174-1195`); place CAD `"Describe a change to the shape"` in `Grid.Row="0"` top `StackPanel` (collapsed by default / toggled).
    - Every visible enabled `Button` in `ShellHost` (including `ActivityRail.RailSettingsButton`) attaches a `Click` handler or `Command` in its constructor (`UI_DEAD_CONTROL_ShellButtonsHaveActions`, `ShellWindowTests.cs:1097-1157`); keep `ActivityRail` width unchanged (`84 DIP`) and `RailSolverSetupButton` preserved beside `RailSettingsButton`.
    - `ExperimentSetupView`: adding `"Describe the experiment"` MUST introduce **ZERO** Run/Start/Execute buttons (`XS-03`/`UX-17`, `SweepViewTests.cs:75,97,137,248-261`) and zero `LAB-01` words.
    - `ExportDialog`: place read-only `"No assistant action here."` inside `BodyScrollViewer` below `SafetyLine` preserving 3/5 `ListBoxItem`s, sticky OS-ordered footer, and `MaxHeight <= 700` at 200%; new `AssistantSettingsDialog` and `AssistantConsentPreviewDialog` follow `DialogChrome` (`MainWindow.axaml.cs:481-537`). Jointly satisfiable.
- **`tools/check-reader-sharing.py`, `tools/coordination/check-test-paths.py`, `tools/check-proof-pii.py`,
  `docs/ai-forward-pack/scripts/verify-no-machine-paths.py`,
  `docs/ai-forward-pack/scripts/verify-no-new-console-launches.py`,
  `docs/ai-forward-pack/scripts/verify-portable-text-io.py`,
  `docs/ai-forward-pack/scripts/verify-subprocess-utf8.py`, `design-lint.py --strict`:**
  - All `src/**/*.cs` readers use `UserFile.OpenRead` or explicit `FileShare.Read | FileShare.Delete` /
    `FileShare.ReadWrite | FileShare.Delete`.
  - Zero relative `"docs/..."` reads in `tests/CfdWorkbench.Core.Tests/*.cs` (allowlist `docs/examples/foildsl/` &
    `Fixtures/` only).
  - Zero `CREATE_NEW_CONSOLE`, zero Windows user-profile paths or machine SIDs, zero raw `#rgb`/`#rrggbb` hexes in
    `DESIGN.md`, zero committed binary captures, zero API keys in any file/log/env/test artifact. Jointly satisfiable.

**Quoted Spec / ADR / Ruling trigger lines (DC-189 — exact citations, never paraphrased).**
- **Spec A6 `AI-01`** (`docs/specs/cfd-workbench-v1.md:1233`):
  > "**Given** no key, no network or a 401/timeout/quota error, **then** every non-AI verb continues; the wayfinder
  > renders disabled with a Configure-key action; removing the key stops new calls without restart; the key never enters
  > a document, log, export or crash report and lives only in the OS credential store (macOS Keychain; Windows Credential
  > Manager, DPAPI-protected) — never a file, environment variable or the document."
- **Spec A6 `AI-04`** (`docs/specs/cfd-workbench-v1.md:1236`):
  > "**Given** a prompt, **then** the payload is previewable byte-for-byte, paths, user names, hostnames and DRC
  > attributions are redacted, exactly one configured endpoint is used, per-request and daily caps are settings, and a
  > local audit record stores payload hash, model id, usage or "Not recorded" and the consent action. **Given**
  > instructions inside a `.dat` name line, comment, solver log or knowledge snippet, **then** they remain quoted data
  > capped at 4 kB per excerpt and 32 kB per prompt (A8.1) and cannot change a proposal field or trigger any action; an
  > excerpt above its cap is truncated with a visible marker, never dropped silently."
- **Spec A6 `AI-06`** (`docs/specs/cfd-workbench-v1.md:1237`):
  > "**Given** a changed model identifier, **then** every AI capability shows "Unevaluated on this model" and is
  > disabled for proposals until the eval suite (A8.6; nine of the ten items in v1, physical-fidelity reserved) is
  > re-run and **every item meets its recorded threshold** — thresholds are fixed at design time and a threshold change
  > is a versioned event — with model id, date and per-item pass rate recorded; a recorded run below any threshold
  > leaves the capability disabled; saved geometry identities are unchanged."
- **Spec A6 `LAB-01`** (`docs/specs/cfd-workbench-v1.md:1229`):
  > "**Given** product copy, exports and assistant output, **then** a lint fails on "cavitation-free",
  > "ventilation-safe", "validated" (outside an ITTC statement), "optimized", "best", "recommended" or "certified"
  > without an A7 grant; every A7 label maps to exactly one string."
- **Spec A8.5 Bundled decision data** (`docs/specs/cfd-workbench-v1.md:1371`):
  > "fail closed: a mismatched file makes its rules Unavailable with the reason | one-bit flip in a class-rule file →
  > the rule reads Unavailable, never a changed verdict"

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **W11-DESIGN** Design Slice: Cross-Area BYOK LLM Assistants & Prior-Wave Carry-Forwards | `docs/design/byok-assistants.{md,html} (new)` | S0 plan committed on `main` (Wave 11A, docs-only) | T2 | 1 | 70 calls · 200k · 1 h · ceiling 300k | One Adversary-cleared `/design-slice` document (`docs/design/byok-assistants.md` + rendered `.html` via `render-markdown.py`, V2 frontmatter linked to `spec-cfd-workbench-v1`, `kb-hw-integration-and-ai-workflows`, `design-guided-solver-setup`, `design-cfd-run-pipeline`, `design-results-and-candidates`, `rulings`): E7 surface list; exact project-DAG layer split (`Core/Assistant` pure zero-I/O, `Persistence/Assistant` OS credential store & closed-DTO `cfdw-assistant` v1 settings/audit, `Analysis/Assistant` zero-new-NuGet BCL `HttpClient` transport + orchestrator, `Cli`, `Desktop`); `PromptRedactor` 4 kB/32 kB redaction & injection-isolation rules; deterministic T0 validator contracts for all 6 active areas (`setup-seed`, `geometry-edit`, `analysis-explain`, `experiment-config`, `run-explain-failure`/`case-diff`/`environment-step`, `results-explain`) plus Export `"No assistant action here"`; hashed embedded BM25 `KnowledgeCorpus.json` tamper-refusal contract; 9-item `A8.6` eval suite & versioned design-time thresholds (`EvalThresholdsVersion = "1.0"`); `COPY-866..915` inventory; test & live-mutant matrix; `tools/check-proof-pii.py`, `verify-ruling-citations.py`, `tools/check-docs.py` exit 0; Adversary pass (`w11-verifier`: Security & Identity HARD VETO + Hydrodynamicist + Data & Persistence + Marine-CAD UX + Test Architect HARD VETO) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W11-CORE-AI** Pure Core AI Domain, Validators, Redactor, Hashed BM25 Knowledge, 9-Item Eval Suite & `CF-W8-1` | `src/CfdWorkbench.Core/Assistant/{AssistantContracts.cs,PromptRedactor.cs,AssistantValidators.cs,KnowledgeRetriever.cs,AssistantEvalSuite.cs} (new — strictly pure zero-I/O)`, `src/CfdWorkbench.Core/CatalogData/KnowledgeCorpus.json (new — auto-embedded by CfdWorkbench.Core.csproj:12-16)`, `tools/spikes/cad-export-measure/verify-open-measure.py` (`CF-W8-1` `math.isfinite` + `parse_constant`), `tests/test_cad_export_measure.py (new)`, `tests/CfdWorkbench.Core.Tests/{AssistantDomainTests.cs (new),AssistantEvalTests.cs (new),IdentityTests.cs}` | `W11-DESIGN` joined on `main` (Wave 11B) | T2 | 1 | 200 calls · 360k · 2 h · ceiling 400k | Red-first tests + ≥ 5 live mutants killed: (1) **Pure `STORE-SUBSET` Core (`src/CfdWorkbench.Core/Assistant/`, avoiding `ArchitectureTests` banned names):** `ICredentialStore` & `IAssistantSettingsStore` interfaces, `AssistantContracts` (all 7 area kinds, `FieldProvenance` Stated/Inferred/Defaulted, `AssistantSettings` with ZERO key field, `AssistantModelEvalReceipt`, `AssistantAuditEntry`), `PromptRedactor` (pure `RedactionContext` input — zero env calls in Core; byte-exact preview; Windows/POSIX paths, user names, default hostnames, DRC attributions, active key/secret patterns redacted; `<= 4,096` **UTF-8 bytes** per excerpt & `<= 32,768` **UTF-8 bytes** per prompt inclusive of visible truncation marker sliced on rune boundaries; delimiter-breakout neutralization inside quoted `.dat`/log/snippet blocks — `C3`), `AssistantValidators` (`setup-seed` field-by-field range check never clamping + rejects coordinates/meshes/forces/scores + `SET-03` hash parity with manual `SET-01`; `geometry-edit` channel/station/lock deltas -> `GeometryEditDraft` + `assistant-proposal <id>` lock source + base `AcceptedId` drift block; `experiment-config` sweep/multipoint optimize + single-point refusal; `case-diff` closed `TypedCaseModel` subset + queued-immutability refusal + log-injection `"apply this repair and run"` disposition stays `Pending`; grounded explanations with numeral-in-context check, `LAB-01` forbidden-claim lint, and `AI-10` $\tau_w$-gated separation check; composes with `SetupAssistantValidator`), `KnowledgeRetriever` (embedded `CatalogData/KnowledgeCorpus.json` verified against pinned SHA-256 build hash — 1-bit flip fails closed to Unavailable — deterministic BM25 ranking, zero network), `AssistantEvalSuite` (9-item deterministic suite with fixed versioned thresholds `1.0`, physical-fidelity reserved; any single failing item or changed model id, provider/protocol, or `EvalThresholdsVersion` disables proposals with `"Unevaluated on this model"`); (2) **`CF-W8-1` (`C6`):** `tools/spikes/cad-export-measure/verify-open-measure.py:40` adds `math.isfinite(val)` AND `json.loads(..., parse_constant=...)` rejection + `tests/test_cad_export_measure.py` proving JSON & direct `NaN`/`Infinity`/`-Infinity` rejection and `--self-test` green; (3) New Core test classes registered in `IdentityTests.cs`; recursive `tools/store_subset.py` ok, `bash tools/run-tests.sh` `TESTS-GREEN`, all static/doc gates exit 0; Adversary pass (`w11-verifier`: Security & Identity HARD VETO + Hydrodynamicist + Data & Persistence + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W11-RUNTIME-AI** OS Credential Store, Closed-DTO Settings/Audit Persistence, BCL `HttpClient` BYOK Transport, `AssistantService` & CLI | `src/CfdWorkbench.Persistence/Assistant/{OsCredentialStore.cs,AssistantSettingsStore.cs} (new)`, `src/CfdWorkbench.Analysis/Assistant/{ByokLlmTransport.cs,AssistantService.cs} (new)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{AssistantVerb.cs (new),Program.cs}`, `tests/CfdWorkbench.Analysis.Tests/{AssistantServiceTests.cs (new),AnalysisChecks.cs}`, `tests/CfdWorkbench.Cli.Tests/{AssistantPersistenceAndCliTests.cs (new),CliTests.cs}` | `W11-CORE-AI` joined on `main` (Wave 11C) | T2 | 1 | 200 calls · 380k · 2 h 15 min · ceiling 400k | Red-first tests + ≥ 5 live mutants killed: (1) **`Persistence/Assistant/` (`C1`, `C3`):** `OsCredentialStore` (`ICredentialStore`: Windows `advapi32.dll` with `[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]`, `CredWriteW`/`CredReadW`/`CredDeleteW`/`CredFree`, `CRED_TYPE_GENERIC` (1), `CRED_PERSIST_LOCAL_MACHINE` (2), `<= 2560` B blob check, `CredFree` + `CryptographicOperations.ZeroMemory` in `finally`; macOS `/usr/bin/security -i` via **stdin** with `UseShellExecute=false`/`CreateNoWindow=true` — NEVER passing secret on `argv` — or `Security.framework` P/Invoke; fail-closed on unsupported OS; zero env-var key reads anywhere; tested with real Windows Credential Manager round-trip under isolated test target name cleaned up in `finally`!), `AssistantSettingsStore` (`IAssistantSettingsStore`: closed-DTO `cfdw-assistant` v1 `assistant/assistant.json` max 64 KiB, private DACL/`0700` dir via `WindowsNative.EnsurePrivateDirectory`+`OwnedByCurrentUser`, symlink/reparse refusal via `File.ResolveLinkTarget`, fail-closed `never-write` on newer version, **strictly rejects any `apiKey`/`secret`/`token` field**, delete-sharing-safe `UserFile.OpenRead`/`FileShare.ReadWrite|FileShare.Delete` append-only `assistant/assistant-audit.jsonl` with UTC daily cap accounting even when usage is `"Not recorded"`); (2) **`Analysis/Assistant/` (`C2`, `C3` — zero `Persistence` ref, zero banned Core edit verbs, zero new NuGet packages):** `ByokLlmTransport` (`.NET 10` `HttpClient` with `AllowAutoRedirect = false`, default TLS validation, `<= 1 MiB` response bound, Anthropic `/v1/messages` & OpenAI-compatible `/v1/chat/completions`, strictly one configured endpoint rejecting `userinfo@`, `0.0.0.0`, and lookalikes like `localhost.evil.com`, HTTPS required except exact loopback `127.0.0.1`/`::1`/`localhost`, typed `401`/`429`/timeout/network errors scrubbing active key), `AssistantService` (key check → `AI-06` model/provider/threshold eval gate → per-request & UTC daily cap check → byte-exact `PromptRedactor` consent preview bound by UTF-8 `payloadSha256` → `KnowledgeRetriever` BM25 grounding → `ByokLlmTransport` → `AssistantValidators` T0 authority → audit append + `assistant.request` event); (3) **Real loopback `HttpListener` wire & security tests (`AssistantServiceTests.cs`):** proves Anthropic & OpenAI-compatible wire calls, exact consented redacted payload on wire, 302 redirect refusal (key never forwarded to second listener), `http://example.com` & `http://localhost.evil.com` refusal, 401 echo-back key scrubbing, 429/timeout handling with manual path unaffected, prompt-injection containment, key removal stopping next call immediately without restart, and negative grep proving API key never appears in settings, audit JSONL, logs, `.cfdw.json`, or process env; (4) CLI `assistant status|eval|preview|audit --json`; (5) Registered in `AnalysisChecks.cs` & `CliTests.cs`; `bash tools/run-tests.sh` `TESTS-GREEN` (incl. `Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt`), `check-reader-sharing`, `check-event-subscribers`, `verify-no-new-console-launches`, `check-docs` all exit 0; Adversary pass (`w11-verifier`: Security & Identity HARD VETO + Data & Persistence + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W11-DESKTOP-AI** Desktop Settings Dialog, Consent Preview, 7-Area Assistant UI Integration & `CF-W7-1` Theme Check | `src/CfdWorkbench.Desktop/Assistant/{AssistantCopy.cs,AssistantSettingsDialog.axaml,AssistantSettingsDialog.axaml.cs,AssistantConsentPreviewDialog.axaml,AssistantConsentPreviewDialog.axaml.cs,AssistantPromptBar.axaml,AssistantPromptBar.axaml.cs} (new)`, `src/CfdWorkbench.Desktop/{ActivityRail.axaml,ActivityRail.axaml.cs,ModelArea.axaml,ModelArea.axaml.cs,WorkbenchController.cs,MainWindow.axaml.cs,ExportDialog.axaml,Styles.axaml (non-theme layout tokens ONLY — ZERO new theme Brush keys)}`, `src/CfdWorkbench.Desktop/Shell/{CommandTable.cs,ShellHost.cs}`, `src/CfdWorkbench.Desktop/{Setup/SetupController.cs,Setup/SetupWorkspaceView.axaml,Setup/SetupWorkspaceView.axaml.cs,Analysis/AnalysisPanel.axaml,Analysis/AnalysisPanel.axaml.cs,Sweeps/ExperimentSetupView.axaml,Sweeps/ExperimentSetupView.axaml.cs,Run/RunWorkspaceView.axaml,Run/RunWorkspaceView.axaml.cs,SolverSetup/SolverSetupView.axaml,SolverSetup/SolverSetupView.axaml.cs,Results/ResultsWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml.cs}`, `DESIGN.md` (`COPY-866..915` proposed ONLY; zero edits to approved `COPY-001..865`; zero raw hexes), `tests/CfdWorkbench.Desktop.Tests/{Wave11AiUiTests.cs (new),ShellModelTests.cs,ControllerSectionTests.cs,WorkbenchTests.cs}` | `W11-RUNTIME-AI` joined on `main` (Wave 11D) | T2 | 1 | 220 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + ≥ 4 live mutants killed: (1) **`AssistantSettingsDialog`** (`DialogChrome`-conformant, `MaxHeight <= 700` at 200%, sticky OS-ordered footer) opened from ActivityRail footer **Settings** button (keeping `84 DIP` width, `RailSolverSetupButton`, 0 `Dock.*` refs, `UI_DEAD_CONTROL` action wired, and `ShellHost` `Auto,*` 3-row/`ColumnSpan=2` intact), `ShellHost.ShowSettingsDialog` + `CommandTable` `view.settings` (`Modal: true`, `ControllerSectionTests.cs` & `ShellModelTests.cs:414` updated per `C4`), and every area's `"Configure key"` action: password-masked OS key Save/Remove taking effect immediately without restart, single endpoint URL, provider preset, model id, per-request & daily caps, live `AI-06` 9-item eval runner with `"Unevaluated on this model"` banner & per-item pass rates, local audit ledger viewer; (2) **`AssistantConsentPreviewDialog` (`DialogChrome`-conformant) & `AssistantPromptBar`:** byte-exact redacted payload preview, per-field Stated/Inferred/Defaulted diff, Accept/Discard/Refresh-on-base-drift, withheld-numeral reason, disabled no-key wayfinder with `"Configure key"` (`AI-01`); (3) **All 7 Areas wired honoring `C5` & `C7`:** Area 1 Setup (`SetupController.cs` + `SetupWorkspaceView` keeping `LanguagePromptBox`/`LanguageSubmitButton`/`LanguageNoKeyText`/`LanguageCardTitle` `x:Name`s in place; default tests isolated from host OS key; `"Describe a starting design"`, `SET-02`/`SET-03`), Area 2 CAD (`ModelArea` `Grid.Row="0"` top bar collapsed by default preserving `ViewArrangementGrid`↔`NavSetupButton` Tab order & `PlanContent` pixel frame; `"Describe a change to the shape"`, `AI-07` → `GeometryEditDraft` preview/diff → Accept = Apply = 1 undo step), Area 3 Analysis (`AnalysisPanel` compact trigger beside `SolverSetupButton` preserving 5 exact tabs & `LoadingPlot.Bounds.Height >= 82`; `"Ask about this calculation"`, `AI-08`), Area 4 Experiment (`ExperimentSetupView` `"Describe the experiment"`, `AI-09`, **ZERO Run/Start/Execute buttons** & zero `LAB-01` words), Area 5 Run (`RunWorkspaceView` keeping offline `ExplainFailureButton`/`PreviewCaseDiffButton` functional + live BYOK `AI-05`) & Solver Setup (`SolverSetupView` keeping `AssistantOfflineCard`/`AssistantPromptBox`/`AssistantResultCard`/`AssistantVerdictText` `x:Name`s in place + live BYOK `AI-11`), Area 6 Results (`ResultsWorkspaceView` keeping `AskResultInput`/`AskResultButton`/`AskResultAnswerText` + live BYOK `AI-10` $\tau_w$-gated separation), Area 7 Export (`ExportDialog` read-only `"No assistant action here."` inside `BodyScrollViewer` below `SafetyLine`); (4) Zero new theme `...Brush` keys in `Styles.axaml`, `UI-CS-TOKEN` 0 findings, `xaml-token-lint` clean, `check-event-subscribers` 0 findings, `DESIGN.md` `COPY-866..915` proposed (`design-lint --strict` 0 warnings; test accepts both `— approved —` and `— proposed —`); (5) `--wave11-ai` wired into `WorkbenchTests.cs` (`Spawn`, `ScaleModes`, `PrintScaleContext("wave11-ai")`), proving all 7 areas, loopback BYOK round-trip, key removal without restart, `AI-06` model-change lockout, and **`CF-W7-1`** Light/Dark/HighContrast theme brush resolution across Wave 10+11 surfaces (without altering frozen `THEME-ROW` keys); `bash tools/run-tests.sh` `TESTS-GREEN` + all static/doc gates exit 0; Adversary pass (`w11-verifier`: Marine-CAD UX + Security & Identity HARD VETO + Hydrodynamicist + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `w11-monitor` | `inherit` | `main` | inherit |
| Adversary verifier | `w11-verifier` | `inherit` | `main` | inherit |
| `W11-DESIGN` (Wave 11A) | `w11-arch-design` | `share` | `main` | inherit |
| `W11-CORE-AI` (Wave 11B) | `w11-core-ai` | `share` | `main` | inherit |
| `W11-RUNTIME-AI` (Wave 11C) | `w11-runtime-ai` | `share` | `main` | inherit |
| `W11-DESKTOP-AI` (Wave 11D) | `w11-desktop-ai` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** Zero open DRs at Wave 11 start (spec `A5.12`, `A6` `AI-01`–`AI-11`, `SET-02`/`SET-03`, `A8.1`, `A8.5`,
  `A8.6`, `B6`–`B6c`, and Rulings 69/70/271/272/276 fix all contracts). If any track surfaces a real spec ambiguity,
  Coordinator spawns `w11-owner` (`workspace: "inherit"`) to rule into `docs/notes/rulings.md` via
  `coord-decide.py rule next --request <id>`.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board &
  `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only` (backed by `pre-commit` floor +
  coordinator diff inspection), pack CLI `coord-mail.py` doorbells `unsupported`.
- **Deadline / Fallback:** Each track has a 1–2.5 h wall budget and 300–400k context ceiling (warm `fork` if needed).
  No track ever writes real production secrets, calls external cloud endpoints during tests, or mutates host WSL
  distros.
- **Termination condition:** Track ends when all exit evidence is independently verified by `w11-verifier` (`PASS` or
  `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py --no-push`, or when the 2-repair-cycle / budget
  cap fires.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` before dispatching | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-w11-coord`) |
| S1: `W11-DESIGN` joins before `W11-CORE-AI` starts | Freezes Core/Persistence/Analysis/Desktop contracts, `cfdw-assistant` v1 schema, 9-item eval thresholds, and `COPY-866..915` before C# code is written (`GO5` decision edge) | Coordinator (`agy-w11-coord`) |
| S2: Serial code waves `W11-CORE-AI` → `W11-RUNTIME-AI` → `W11-DESKTOP-AI` | Each downstream wave compiles against upstream contracts (`Core/Assistant` → `Persistence`+`Analysis/Assistant` → `Desktop/Assistant`) and shares the real-display Desktop test harness (`GO5`) | Coordinator (`agy-w11-coord`) |
| S3: Joins run one at a time via `conductor-join.py --no-push` | Serial on `main`; `W11-DESIGN` (`--docs-only`) self-skips `tools/run-tests.sh` (`RING-SKIPPED docs-only`); every code join runs full `TESTS-GREEN` + `xaml-token-lint` + `check-event-subscribers` + `10/10` verify gates | Coordinator (`agy-w11-coord`) |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W11-DESIGN` → `W11-CORE-AI` & `W11-RUNTIME-AI` & `W11-DESKTOP-AI` | Frozen `ICredentialStore`/`IAssistantSettingsStore` interfaces, `cfdw-assistant` v1 schema, 4 kB/32 kB `PromptRedactor`, 7-area validator DTOs, 9-item `A8.6` eval thresholds, and `COPY-866..915` inventory | Hard wave boundary: `W11-DESIGN` clears `w11-verifier` and joins onto `main` before `W11-CORE-AI` branches |
| `W11-CORE-AI` → `W11-RUNTIME-AI` | Pure `Core/Assistant` interfaces, validators, redactor, BM25 retriever, and 9-item eval suite | Hard wave boundary: `W11-CORE-AI` joins before `W11-RUNTIME-AI` branches |
| `W11-RUNTIME-AI` → `W11-DESKTOP-AI` | `OsCredentialStore`, `AssistantSettingsStore`, `ByokLlmTransport`, `AssistantService` | Hard wave boundary: `W11-RUNTIME-AI` joins before `W11-DESKTOP-AI` branches |

## Struck tracks

| track | why it was not worth its multiplier / why struck |
|---|---|
| Live third-party cloud LLM API tests | Requires external paid credentials and internet egress; real HTTP/JSON/header/401/429/timeout/redaction wire paths are tested deterministically via local loopback `HttpListener` (`http://127.0.0.1`). |
| Accessibility / screen-reader tracks (NVDA / VoiceOver / Narrator) | Explicitly excluded by operator directive of 2026-11-23 & 2026-11-24. |
| Live WSL `openfoam2512` build (`A5-RESUME`) | Network Cisco Umbrella (`net.proxy-tls`) still blocks `sourceforge.net`. |
| Commercial/maker CAD open-and-measure receipts (`SPIKE-CAD-02` → `ADR-0013` acceptance) | Requires interactive GUI runs in Autodesk Fusion, FreeCAD CAM, Vectric VCarve/Aspire, and Rhino 8 outside this repo (`CF-W8-1` hardens the receipt validator `verify-open-measure.py` now). |
| Live ParaView 5.12+ launch & Authenticode / macOS codesigning | Externally absent on this host (`PARAVIEW_FOUND=False`; no signing certs in repo). |
| Concurrent `W11-CORE-AI` / `W11-RUNTIME-AI` / `W11-DESKTOP-AI` | Strict compile-time & contract dependency chain plus shared `tools/run-tests.sh` real-display Desktop harness; width-1 serial execution avoids merge conflicts and Desktop exit-124 timeouts. |

## Cost justification (GO6 — 15× multiplier)

Honest Simplifier assessment: **width 1 across 4 bounded-context `share` tracks is the smallest reliable shape.**
Each track matches an architectural layer boundary (`Design` → pure `Core` + `CF-W8-1` → `Persistence`+`Analysis`+`CLI`
→ `Desktop` 7-area UI + `CF-W7-1`), keeps every worker comfortably below the 400k context ceiling, eliminates merge
conflicts and real-display Desktop harness contention, and gives each high-risk boundary (especially OS credential
storage, redaction/prompt-injection isolation, and `AI-06` eval gating) an independent Security & Identity HARD VETO
audit before join.

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect hard veto + Security & Identity hard veto + Simplifier soft veto + Tech Lead casting vote); fold all verified conditions | 1 subagent pass | Clears Adversary vetoes before S0 commit |
| 2 | Render `docs/coordination/wave11-byok-assistants-and-prior-carryforwards.html`, run `docs-graph.py derive`, `tools/check-docs.py`, `verify-ruling-citations.py`, `tools/check-proof-pii.py`, append `prepare-for-coordination` audit entry, commit S0 on `main` | 1 commit | Clean primary required before any join |
| 3 | Update swarm title (`wave11-byok-assistants: DESIGN -> CORE-AI -> RUNTIME-AI -> DESKTOP-AI`), write `plan.json`, add `w11-monitor` + `w11-verifier` (`inherit`), and dispatch **Wave 11A** `w11-arch-design` (`W11-DESIGN`, `share`) | width 1 + verifier | Freezes BYOK assistant contracts before code |
| 4 | Route `W11-DESIGN` to `w11-verifier` → join via `conductor-join.py --no-push --docs-only` → `swarm kill w11-arch-design` | 1 docs join | Unblocks Wave 11B `W11-CORE-AI` |
| 5 | Dispatch **Wave 11B** `w11-core-ai` (`W11-CORE-AI`, `share`) → `w11-verifier` → join via `conductor-join.py --no-push` → `swarm kill w11-core-ai` | width 1 + 1 join | Unblocks Wave 11C `W11-RUNTIME-AI` |
| 6 | Dispatch **Wave 11C** `w11-runtime-ai` (`W11-RUNTIME-AI`, `share`) → `w11-verifier` → join via `conductor-join.py --no-push` → `swarm kill w11-runtime-ai` | width 1 + 1 join | Unblocks Wave 11D `W11-DESKTOP-AI` |
| 7 | Dispatch **Wave 11D** `w11-desktop-ai` (`W11-DESKTOP-AI`, `share`) → `w11-verifier` → join via `conductor-join.py --no-push` → `swarm kill w11-desktop-ai w11-verifier w11-monitor` | width 1 + 1 join | Full Wave 11 convergence on `main` |
| 8 | Post `[done]` on swarm board, append Planned vs Actual to this plan, render `.html`, run `docs-graph.py derive` + `tools/check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main` (no push), and report verified results to operator | 1 close commit | Clean Wave 11 closure |

## Gate record

| gate | reviewer | verdict | conditions folded |
|---|---|---|---|
| Independent Adversary Plan Review | `Wave 11 Adversary Plan Reviewer` (`0a930c37`; `Test Architect` hard veto · `Security & Identity` hard veto · `Simplifier` soft veto · `Tech Lead` casting vote) | **PASS-WITH-CONDITIONS → CLEARED** | Folded all 7 verified conditions (`C1–C7`): (C1) macOS Keychain via `/usr/bin/security -i` stdin (never secret on `argv`) or `Security.framework`, Windows `advapi32.dll` `System32` search path / `CRED_PERSIST_LOCAL_MACHINE` / `<= 2560` B / `CredFree` + `ZeroMemory` in `finally`, zero env-var key reads; (C2) `ByokLlmTransport` `AllowAutoRedirect = false`, default TLS, strict loopback-only HTTP host validation (rejecting `userinfo@`, `0.0.0.0`, `localhost.evil.com`), `<= 1 MiB` response bound, error key scrubbing, and loopback 302/401 negative tests; (C3) `PromptRedactor` pure `RedactionContext` + UTF-8 byte-exact rune-safe 4,096 B / 32,768 B caps + delimiter neutralization, preview-to-wire `payloadSha256` binding, private DACL/`0700` + symlink-refusal + UTC daily cap accounting on `AssistantSettingsStore`, and `AI-06` trigger on model/provider/threshold change; (C4) added `ControllerSectionTests.cs` & `SetupController.cs` to `W11-DESKTOP-AI`, wired `ShellHost.ShowSettingsDialog` into both `ControllerSectionTests` loops and `"view.settings"` into `ShellModelTests.cs:414`; (C5) forewarned all live Desktop `x:Name`, 5-tab/82 px `AnalysisPanel`, `ModelArea` Tab-order/pixel, `UI_DEAD_CONTROL`, `SweepViewTests` zero-Run, `ActivityRail` 84 DIP width, `ExportDialog`/`DialogChrome` 200% `MaxHeight <= 700`, and default no-key test isolation invariants; (C6) `CF-W8-1` adds both `math.isfinite` and `json.loads(..., parse_constant=...)`; (C7) dropped unnecessary `CfdWorkbench.Core.csproj` edit (`CatalogData\**\*` already embedded), kept existing `x:Name` trees in place, fixed script path. Tech Lead casting vote: APPROVE 4 serial width-1 `share` tracks. |

## Status

| | |
|---|---|
| **Completed** | Pushed Wave 10 (`d89d727f`) to `origin/main`, recorded Operator Ruling 276 (`8f147c46`, 0 unapproved `COPY-*` rows left in `DESIGN.md`), grounded Wave 11 layer state & project dependency DAG, and drafted & Adversary-cleared `docs/coordination/wave11-byok-assistants-and-prior-carryforwards.md` (`C1–C7` folded) |
| **Remaining** | S0 commit → serial swarm execution (`W11-DESIGN` → `W11-CORE-AI` → `W11-RUNTIME-AI` → `W11-DESKTOP-AI`) → independent Adversary verification & `conductor-join.py --no-push` merges → Planned vs Actual closeout |
| **Best next action** | Render `.html`, run docs/PII/ruling/verify gates, commit S0 on `main`, and launch `w11-monitor` + `w11-verifier` + Wave 11A `w11-arch-design` (`W11-DESIGN`) |
