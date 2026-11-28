---
id: design-ai-guided-design-notebook
title: "Design: AI-Guided Hydrofoil Design Notebook (FoilReq 1.0, Grill-Me Engine, Local Physics Loop & Native Avalonia Workspace)"
type: design
status: proposed
owner: "@timianmalloo"
phase: "design — Wave 13B (build basis for Core FoilReq/Grill/Eval v2.0, Analysis NotebookOrchestrator, Persistence Envelope.notebook, CLI, and Avalonia NotebookWorkspaceView)"
tags: [ai, notebook, foilreq, grill-me, byok, hydrofoil, cad-handoff, avalonia, security, design]
links:
  - { to: spec-ai-guided-design-notebook, rel: implements }
  - { to: ui-review-ai-guided-design-notebook, rel: implements }
  - { to: mockup-ai-guided-design-notebook, rel: implements }
  - { to: design-byok-assistants, rel: depends-on }
  - { to: design-setup-seeding, rel: depends-on }
  - { to: design-section-catalog-expansion, rel: depends-on }
  - { to: design-rounded-wing-tips, rel: relates-to }
  - { to: design-language, rel: depends-on }
  - { to: threat-model, rel: relates-to }
  - { to: privacy-review, rel: relates-to }
review-by: "2027-02-27"
summary: >-
  Build-ready /design-slice for CFD-Workbench's AI-Guided Hydrofoil Design Notebook, implementing
  spec-ai-guided-design-notebook and the operator-approved interactive HTML mockup baseline. Freezes
  the Envelope.notebook? expand-migrate schema (respecting NativeProject's 32 MB and <=4,096 UTF-8 B
  per-line limits via <=1,024 B line-chunked arrays, plus in-memory pre-handoff lifecycle from Start),
  pure STORE-SUBSET CfdWorkbench.Core FoilReq 1.0 parser/EBNF/validator, GrillTreeEngine, two-profile
  RecipeSeeder.EmitFoilDsl, CandidateIntentValidator, RequirementScorecard with self-contained candidate
  operating-point summaries & Tier-0 solver_context formatting aligned to NumeralTokenRegex,
  AssistantEvalSuite v2.0 (11 active items, AI-06 lockout), single-endpoint OptionalFastModelId propagated
  end-to-end with value-level secret screening, UntrustedExcerpt-fenced multi-turn PromptRedactor,
  ArchitectureTests-safe NotebookOrchestrator over direct ProductWingMethod/PanelMethod/NeuralFoilPolarSource,
  non-destructive CAD handoff, and native Avalonia NotebookWorkspaceView with approved COPY-924..934.
---

# Design: AI-Guided Hydrofoil Design Notebook

**Status:** Proposed build blueprint for Wave 13B, implementing [spec-ai-guided-design-notebook](../specs/ai-guided-design-notebook.md) and the operator-approved interactive mockup baseline [ai-guided-design-notebook.html](../mockups/ai-guided-design-notebook.html). Zero new NuGet packages; zero new project references; respects the verified `Core ← Persistence / Analysis ← Cli ← Desktop` DAG and `STORE-SUBSET` / `ArchitectureTests` binary gates.

---

## 0. Evidence Ledger (Stage 3 Grounding — Verified from Source)

| Fact | Verified Observation | Source | Confidence |
|---|---|---|---|
| **`.cfdw.json` `Envelope`, non-empty project invariant, & per-line byte cap** | `Envelope` (`AuthoringSession.cs:23-38`) uses `WhenWritingNull` optional properties (`analysis`, `experiments`, `setup`, `cfd`), `RunRecord.Format` (`RunRecord.cs:225-229`) switches `"cfdw-project-1" → "cfdw-project-2"`, `NativeProject.Exact` (`:3371`) enforces closed top-level optional allowlist (`DOC-UNSUPPORTED-FIELD`), `NativeProject.Check` enforces `Sources/Designs/Accepted/Cursors.Length > 0` (`:3496`) and `acceptedExists(run.Inputs.AcceptedId)` (`RunRecord.cs:310-311`), `NativeProject.MaxBytes = 32_000_000`, AND **every serialized JSON line must be `<= 4,096` UTF-8 bytes** (`:3334, 3366`, else `DOC-SIZE`/`DOC-SCHEMA`). | `src/CfdWorkbench.Core/{AuthoringSession.cs:23-38, 3302-3543, RunRecord.cs:225-229, 310-311}` | **Verified** |
| **Existing `Assistant` Core, Persistence & Transport stack** | `AssistantContracts.cs` (`AssistantArea`, `AssistantProposalKind`, `AssistantSettings` `cfdw-assistant v1`), `AssistantValidators.cs` (`Lab01CopyLint`, `ForbiddenGeometryOrPhysicsFields` `:250`, `NumeralTokenRegex` `:258-260`, `FindUnsupportedNumeral` `:1133`, `CheckExplanationTextSafety` `:1174`, `TryResolveCitations` `:1213`, `TryParseStrictObject` `:1682`), `PromptRedactor.RedactPayload` (`4,096 B` excerpt / `32,768 B` prompt — **fences & delimiter-neutralizes `UntrustedExcerpt` items ONLY, NOT raw `userPrompt`**, `:106-152`), `AssistantEvalSuite` (`EvalThresholdsVersion = "1.0"`, 9 active + 1 reserved), `AssistantService` (`PreparePreviewAsync`, `ExecuteConsentedAsync` wire-hash verification, `ASSISTANT-UNEVALUATED` `:206, 350`), `ByokLlmTransport` (`public sealed class` `:60`, `AssistantTransportErrorKind` `:12-24`: `AuthenticationFailed`, `RateLimitedOrQuota`, `ResponseTooLarge`, `NetworkUnavailable`, `RedirectRefused`, `Timeout`). | `src/CfdWorkbench.Core/Assistant/*.cs`, `src/CfdWorkbench.Analysis/Assistant/*.cs`, `src/CfdWorkbench.Persistence/Assistant/*.cs` | **Verified** |
| **Direct local physics solvers vs `AnalysisService`** | `AnalysisService` (`AnalysisService.cs:113-188`) evaluates accepted revisions only (`session.AcceptedSourceOf`) and records into `Envelope.Analysis` (`RunRecord.CheckRow` requires `acceptedExists`). Direct in-process evaluation of unaccepted candidate `FoilDSL` bytes uses `Placement.Sections` + `ProductWingMethod` (`VortexLattice.Solve` + `StripCoupler.Couple` + `NeuralFoilPolarSource`) + `PanelMethod` + `SectionTier.Evaluate` without mutating `AuthoringSession` or `Envelope.Analysis`. | `src/CfdWorkbench.Analysis/{AnalysisService,ProductWingMethod,PanelMethod,SectionTier,VortexLattice,StripCoupler}.cs` | **Verified** |
| **`RecipeSeeder.EmitFoilDsl` single-profile signature** | `RecipeSeeder.EmitFoilDsl(RecipeRecord, CatalogEntry, string)` (`RecipeSeeder.cs:364, 423`) currently accepts one `CatalogEntry` and writes the same profile at root and tip. Supporting distinct root/tip sections (and `My sections` deterministic rebuild) requires a two-profile in-memory overload. | `src/CfdWorkbench.Core/RecipeSeeder.cs:364-427` | **Verified** |
| **`ArchitectureTests` IL metadata gate** | `CfdWorkbench.Analysis.dll` may NEVER call any `CfdWorkbench.Core` method named `ProfileAt`, `Cancel`, `Undo`, `Redo`, or prefixed `Begin*`, `Update*`, `Apply*`. | `tests/CfdWorkbench.Analysis.Tests/ArchitectureTests.cs:11-46` | **Verified** |
| **Desktop shell, `Styles.axaml`, & test invariants** | `ActivityRail` fixed `84 DIP` width, `0` `Dock.*` outside `Shell/`; `ModelArea.axaml:83, 240-243` & `ModelArea.axaml.cs:608-622` gate `StartCardView`, `PlanContent`, `Mode`, and `ViewArrangementGrid` explicitly on `!controller.IsSetup && !controller.IsRun && !controller.IsResults`; `ShellHost.IsPaneCommand` (`:1976-1984`) & `SyncDockAutoCollapse` (`:1853-1887`); `CommandTable` non-modal `view.notebook` must be added to `ShellModelTests.cs:414` gesture-less allowlist; `Styles.axaml:15-24, 41-138` already defines `VikNeg2Color`/`VikNeg2Brush`..`VikPos2Color`/`VikPos2Brush` and 30 closed theme brushes; `TokenDisciplineTests` forbids `Color.Parse`, C# `FontSize` literals, and new theme brushes; `StartView.axaml:42` uses a 3-column grid. | `src/CfdWorkbench.Desktop/{ActivityRail.axaml, ModelArea.axaml(.cs), Panes/StartView.axaml, Shell/ShellHost.cs, Shell/CommandTable.cs, Styles.axaml}`, `tests/CfdWorkbench.Desktop.Tests/{ShellModelTests, ControllerSectionTests, TokenDisciplineTests, Wave11AiUiTests}.cs` | **Verified** |
| **Reusable Avalonia controls** | `SurfaceRenderer`, `PlanformPreviewControl`, `SectionProfileView`, `SectionChartView`, `LoadingChart`, `PerformanceCurveChart`, `AssistantRuntimeHub`, `AssistantConsentPreviewDialog`, `AssistantSettingsDialog` are pure controller-independent controls. `View3d`, `SectionEditorView`, and `AnalysisPanel` are tightly bound to `WorkbenchController` CAD/Analysis slots and MUST NOT be embedded directly. | `src/CfdWorkbench.Desktop/{SurfaceRenderer, Setup/PlanformPreviewControl, Analysis/SectionProfileView, Analysis/SectionChartView, Analysis/LoadingChart, Sweeps/PerformanceCurveChart}.cs` | **Verified** |

---

## 1. Data Model First (DM1–DM16) & E7 Change-Surface List

### 1.1 Conceptual Model, Aggregates, Grain, Additivity & History Rules

**Bounded context:** `GuidedDesignNotebook` (extends `Assistance`; references `Design`, `Analysis`, and `ProfileCatalog` strictly by immutable `BLAKE3(JCS)` hash — never creates a second writable CAD AST and never writes unaccepted candidate runs into `Envelope.Analysis`!).

**Pre-handoff persistence lifecycle (resolves `AuthoringSession.cs:3496`):**
- When launched from Start (`!foilOpen`, zero accepted revisions in project), `NotebookSessionRecord` lives **in session memory** until the user confirms **`Open in CAD Mode →`** (`HandoffNotebookCandidate` seeds `r1`, creating a valid `.cfdw.json` project that persists both `r1` and `Envelope.Notebook`).
- When launched inside an **already-open `.cfdw.json` project** (`Sources/Designs/Accepted/Cursors.Length >= 1`), `NotebookSessionRecord` persists immediately inside `Envelope.Notebook` on Save (never mutating `Accepted[]`/`Designs[]` until `HandoffNotebookCandidate`).

| Entity / Value | Aggregate / Kind | Grain ("one row is exactly one …") | Additivity | History & Persistence Rule (`Envelope.notebook?`) | Writer → Compute Reader |
|---|---|---|---|---|---|
| **`NotebookSessionRecord`** | Aggregate root (0..1 per project in v1) | One guided design notebook attached to one `.cfdw.json` project (`sessionId`: UUID v4) | Turn count, candidate count: additive | Optional expand-only `Envelope.Notebook` (`[JsonIgnore(WhenWritingNull)]`, JSON `"notebook"`, non-empty required when present in `NativeProject.Read`, switches `RunRecord.Format(..., hasNotebook: true)` → `"cfdw-project-2"`). **4,096 B/line guard (`AuthoringSession.cs:3334`):** all multi-line prose/DSL/profile-snapshot strings are stored as `IReadOnlyList<string>` line arrays with each raw chunk **`<= 1,024` UTF-8 bytes** (guarantees encoded JSON line `<= 3,800` B even under worst-case JSON escaping + indentation!). | Writer: `AuthoringSession.RecordNotebook*`. Readers: `NativeProject.Check`, `NotebookOrchestrator`, `NotebookWorkspaceView`, `NotebookVerb`. |
| **`RequirementSpecVersion`** (`FoilReq 1.0`) | Type-2 immutable entity (append-only `Requirements[]`) | One validated `FoilReq 1.0` snapshot (`reqVersionId`, ordinal `v1.0, v1.1…`, `contentHash = BLAKE3(JCS)`, line-chunked `dslLines[]`) | Non-additive bounds & targets | Append-only. Every grill answer or edit appends a new version — never mutates prior versions. | Writer: `FoilReqSerializer` → `AuthoringSession`. Readers: `FoilReqValidator`, `GrillTreeEngine`, `RequirementScorecard`, `NotebookWorkspaceView`. |
| **`NotebookTurnRecord`** | Append-only fact (`Turns[]`) | One user prompt, grill question/answer/re-open, local solver receipt, AI translation, or error/withhold event (`turnId`, `utcTimestamp`, `kind`, `reqVersionId`, `candidateId?`, line-chunked `bodyLines[]`) | Count additive; `wallTimeMs` additive | Append-only. Re-opening a prior grill pill appends a `GrillReopen` turn; downstream candidates with older `reqVersionId` derive `IsStale = true` (`TQ9`, DM7). | Writer: `NotebookOrchestrator` → `AuthoringSession`. Readers: `NotebookWorkspaceView`, `PromptRedactor` (bounded window as `UntrustedExcerpt`s). |
| **`CandidateIntentDelta`** | Validated proposal value inside `CandidateIterationRecord` | One typed set of bounded recipe/section/channel parameters (`deltaHash = BLAKE3(JCS)`, `RootProfileId`, `TipProfileId`, optional `CustomRootProfileSnapshot?` / `CustomTipProfileSnapshot?` line-chunked `DatImport` coordinates for non-`GEN` `My sections` profiles, zero raw wing coordinates/forces) | Non-additive geometric parameters | Immutable once validated by `CandidateIntentValidator`. Embedding the immutable coordinate snapshot whenever a `My sections` profile is referenced makes `SurfaceHash_Rebuilt == SurfaceHash_Recorded` 100% deterministic even across machines or library edits! Includes optional Wave 13A seam fields (`TipLeRadiusMm?`, `TipTeRadiusMm?`, `TipStyle?`, `TipCapRatio?`). | Writer: `CandidateIntentValidator`. Readers: `NotebookCandidateRunner`, `RequirementScorecard`. |
| **`CandidateOperatingPointSummary`** | Immutable value array inside `CandidateIterationRecord` (`OperatingPoints[]`) | One local solver operating-point result (`opId`, `speedKn`, `alphaDeg`, `cl`, `cd`, `wingOnlyLd`, `ncrit2Ld`, `ncrit4Ld`, `reMac`, `cpMin`, `sigma`, `cavMargin`, `vCritKn`, `governingEta`, `hOverCRoot`, `frH`, `depthUnset`, `methodVersion`, `unavailableReason?`, `solverWallMs`) | `solverWallMs` additive; physics ratios non-additive | Self-contained inside `CandidateIterationRecord` — **never writes dangling unaccepted runs into `Envelope.Analysis`** (preserves `RunRecord.CheckRow` `acceptedExists` invariant at `RunRecord.cs:310-311`!). | Writer: `NotebookCandidateRunner`. Readers: `RequirementScorecard`, `NotebookWorkspaceView`. |
| **`CandidateIterationRecord`** (`v1..vn`) | Type-2 immutable entity (`Candidates[]`) | One deterministic 3D foil candidate materialized from `(reqVersionId, delta)` via two-profile `RecipeSeeder.EmitFoilDsl`, holding `surfaceHash = BLAKE3(JCS)`, `geometryStatus`, `refusalCode?`, `OperatingPoints[]`, `repairAttemptIndex` (`0..2`) | Non-additive physics; `solverWallMs` additive | Append-only. **Derive-don't-store (DM7):** unaccepted candidate `FoilDSL` bytes are NOT duplicated in `.cfdw.json` — regenerated deterministically from `(RecipeSeeder.SolverVersion, delta)` (using bundled `GEN` or embedded `My sections` snapshot) and verified by rebuild equality test (`SurfaceHash_Rebuilt == SurfaceHash_Recorded`). | Writer: `NotebookOrchestrator` → `AuthoringSession`. Readers: `RequirementScorecard`, `CadHandoffService`, `NotebookWorkspaceView`. |
| **`RequirementScorecard`** | **Derived** projection (DM7 — never stored) | Clause-by-clause pass/tradeoff/violated/flagged/unavailable verdict + Tier-0 precomputed `v_a → v_b` deltas (`solver_context` formatted canonically for `NumeralTokenRegex`) | Non-additive | Pure C# projection from `(RequirementSpecVersion, CandidateIterationRecord, baseline?)`. Never stored, never drifts. | Writer: none (derived). Readers: `NotebookOrchestrator` (`solver_context`), `NotebookWorkspaceView`. |
| **`CadHandoffRecord`** | Append-only fact (`Handoffs[]`) | One user-confirmed transfer of a `GeometryStatus.Certified` candidate into Area 2 CAD (`handoffId`, `candidateId`, `surfaceHash`, `seededBriefId`, `seededGoalStateId`, `seededAcceptedId`) | Count additive | Append-only; invokes `AuthoringSession.HandoffNotebookCandidate` (`current == null` → `Edit = null`, cursor `"open"`; `current != null` → `Edit = new EditReceipt(operationId, 0, "seed", seededBriefId)`, cursor `"apply"` — matching `AuthoringSession.Seed:2870-2906` and `EditReference:3478-3480`!). | Writer: `AuthoringSession.HandoffNotebookCandidate`. Readers: `NativeProject.Check`, `NotebookWorkspaceView`. |
| **`AssistantSettings.OptionalFastModelId`** | Optional expand-only string in `cfdw-assistant v1` | Optional same-endpoint fast model pin (`<= 256` chars, no controls, value-level secret rejected, omitted when null) | Non-additive | Expand-only in `AssistantSettingsStore` `AllowedRootProperties` (default files stay byte-identical; single endpoint & single OS key unchanged; requires its own `AI-06` `EvalThresholdsVersion = "2.0"` receipt). | Writer: `AssistantSettingsStore`. Readers: `AssistantService`, `AssistantSettingsDialog`, `AssistantConsentPreviewDialog`, `NotebookOrchestrator`. |

**Migration & Rollback (DM16):**
- `.cfdw.json`: Expand-only optional `"notebook"` block (`WhenWritingNull`), non-empty guard in `NativeProject.Read`, `RunRecord.Format(..., bool hasNotebook = false)`. Projects without a notebook serialize identically to pre-Wave-13B.
- `cfdw-assistant v1`: Expand-only optional `"optionalFastModelId"`; old v1 files open untouched.
- `AI-06` `EvalThresholdsVersion`: Bumps `"1.0" → "2.0"` (`11` active + `1` reserved). Old `"1.0"` receipts cleanly lock live proposals with `COPY-887` (`ASSISTANT-UNEVALUATED`) until re-evaluated.

### 1.2 E7 End-to-End Change-Surface List (Respecting Project Reference DAG)

| Layer | Exact Files & Symbols Touched |
|---|---|
| **1. Pure Core (`Core/Notebook/` & `Core/Assistant/` — 0 project refs, `STORE-SUBSET`)** | `Core/Notebook/{FoilReqAst,FoilReqParser,FoilReqValidator,GrillTreeEngine,CandidateIntentValidator,RequirementScorecard,NotebookRecords}.cs`; `Core/RecipeSeeder.cs` (two-profile `EmitFoilDsl` overload accepting distinct root/tip in-memory `DatImport` profiles); `Core/AuthoringSession.cs` (`Envelope.Notebook`, `Dispose`, `EnvelopeCore`, `ReopenCore`, `NativeProject.Read` allowlist & non-empty guard, `NativeProject.Check`, `RecordNotebook*`, `HandoffNotebookCandidate`); `Core/RunRecord.cs` (`Format(..., bool hasNotebook = false)`); `Core/Assistant/AssistantContracts.cs` (`+AssistantArea.Notebook`, `+AssistantProposalKind.RequirementDecomp`, `+CandidateIntentDelta`, `+AssistantSettings.OptionalFastModelId`); `Core/Assistant/AssistantValidators.cs` (`internal static TryParseStrictObject` & `ForbiddenGeometryOrPhysicsFields`, `ValidateResponse` switch arms for both new kinds + canonical `solver_context` formatting); `Core/Assistant/AssistantEvalSuite.cs` (`EvalThresholdsVersion = "2.0"`, 11 active items + `CheckModelEligibility(settings, modelIdOverride?)`); `Core/Assistant/KnowledgeRetriever.cs` (`AreaToCorpusKey` `+Notebook`). |
| **2. Persistence (`Persistence/Assistant/`)** | `Persistence/Assistant/AssistantSettingsStore.cs` (`+optionalFastModelId` in `AllowedRootProperties`, omit when null, value-level secret screening via `PromptRedactor.ScrubSecrets` on both `ModelId` and `OptionalFastModelId`). |
| **3. Analysis (`Analysis/Assistant/` & `Analysis/Notebook/` — refs `Core` ONLY, `ArchitectureTests` safe)** | `Analysis/Assistant/AssistantService.cs` (`EffectiveModelId` on `AssistantPreviewRequest` & `PreparedAssistantCall`, propagated through `PreparePreviewAsync`, `ExecuteConsentedAsync`, `LlmTransportRequest.ModelId`, `AssistantAuditEntry.ModelId`, `RunEvaluationAsync(targetModelId?)`); `Analysis/Notebook/{NotebookOrchestrator,NotebookCandidateRunner,INotebookSolverRunner}.cs` (direct `Placement.Sections` + `ProductWingMethod` + `PanelMethod` + `NeuralFoilPolarSource`, zero `AnalysisService` coupling, zero banned `ArchitectureTests` Core calls). |
| **4. CLI (`Cli/`)** | `Cli/NotebookVerb.cs` (`cfd-workbench notebook inspect|validate <path.cfdw.json> [--json]`); `Cli/AssistantVerb.cs` (dynamic active eval count instead of hardcoded `/9`, `+--model <id>` option for fast-model eval, `+notebook` area); `Cli/Program.cs` (`notebook` dispatch). |
| **5. Desktop (`Desktop/Notebook/` & Shell — `0 Dock.*` outside `Shell/`)** | `Desktop/Notebook/{NotebookWorkspaceView.axaml(.cs),NotebookCopy.cs,NotebookWing3dControl.cs,NotebookScorecardView.axaml(.cs),CadHandoffDialog.cs}`; `Desktop/Assistant/{AssistantConsentPreviewDialog.axaml.cs,AssistantSettingsDialog.axaml.cs,AssistantCopy.cs}` (`EffectiveModelId` display/audit, `ExtractProvenance` for `RequirementDecomp` & `CandidateIntentDelta`, `OptionalFastModelId` field + eval button, dynamic active eval count); `Desktop/ActivityRail.axaml(.cs)` (`+WorkbenchArea.Notebook`, `RailNotebookButton` shown when session active/available, `84 DIP` preserved); `Desktop/ModelArea.axaml(.cs)` (`+NotebookWorkspace`, update all `!controller.IsNotebook` visibility gates at `:608-622`); `Desktop/Panes/StartView.axaml(.cs)` (`2×2` responsive card grid at 200% scaling adding `COPY-924` while keeping all existing `x:Name`s); `Desktop/Shell/{CommandTable.cs,ShellHost.cs}` (`view.notebook` `Modal: false`, `IsPaneCommand`, `SyncDockAutoCollapse`); `tests/CfdWorkbench.Desktop.Tests/{ShellModelTests.cs:414,Wave11AiUiTests.cs}` & `tests/CfdWorkbench.Core.Tests/AssistantEvalTests.cs` & `tests/CfdWorkbench.Analysis.Tests/AssistantServiceTests.cs` (update v1.0/9-item assertions cleanly to v2.0/11-item). |

---

## 2. Delivery Phasing & Solution-Selection Ladder

- **Vertical slices (Wave 13B):**
  1. **13B-Core & Persistence & CLI:** `Core/Notebook/*`, two-profile `RecipeSeeder.EmitFoilDsl`, `Envelope.Notebook` persistence & `HandoffNotebookCandidate`, `AssistantValidators` (+2 kinds), `AssistantEvalSuite` v2.0 (11 active items), `OptionalFastModelId` + value-level secret screening, `NotebookVerb`.
  2. **13B-Runtime (`Analysis`):** `AssistantService` `EffectiveModelId` + `NotebookOrchestrator` + `NotebookCandidateRunner` — proven end-to-end against a real local loopback `HttpListener` using real `ByokLlmTransport` (zero fake cloud receipts, zero external network in CI).
  3. **13B-Desktop:** `NotebookWorkspaceView` implementing all 7 locked mockup states across Light/Dark/HighContrast with `COPY-924..934`, `CadHandoffDialog`, `ActivityRail`/`ModelArea`/`StartView`/`CommandTable` integration, and Desktop harness tests.
- **Mock-substitutable seams:** `ByokLlmTransport` (`internal ByokLlmTransport(HttpMessageHandler, ...)` + real loopback `HttpListener` server on `127.0.0.1`), `ICredentialStore`, `IAssistantSettingsStore`, and `INotebookSolverRunner` (defaults to real local `ProductWingMethod` + `PanelMethod` + `NeuralFoilPolarSource`; substitutable for fast deterministic UI harness state tests).
- **Solution-Selection Ladder (L1):**
  - *YAGNI:* Tier-0 C# computes session titles, parameter diffs, and `%` deltas (`0` LLM tokens); single configured endpoint (`AI-04`); `0..1` notebook session per project in v1; zero new NuGet packages; zero new theme brushes.
  - *Reuse:* Reuse `AssistantService`, `ByokLlmTransport`, `PromptRedactor.RedactPayload`, `Lab01CopyLint`, `AssistantConsentPreviewDialog`, `AssistantSettingsDialog`, `RecipeSeeder`, `Catalog`, `ProductWingMethod`, `PanelMethod`, `NeuralFoilPolarSource`, `SurfaceRenderer`, `PlanformPreviewControl`, `SectionProfileView`, `SectionChartView`, `LoadingChart`, `PerformanceCurveChart`, `Styles.axaml` `Vik*Brush`, `DialogChrome`.
  - *Bounded `simplify:` markers:*
    - `// simplify: 0..1 NotebookSessionRecord per .cfdw.json project in v1; ceiling: 1 session, <= 200 turns, <= 50 candidates; upgrade trigger: when users request parallel multi-brief notebooks inside one project.`
    - `// simplify: Multi-turn UntrustedExcerpt window keeps pinned FoilReq + latest active candidate summary + last 6 turns within 32,768 UTF-8 B cap; upgrade trigger: when sessions routinely exceed 25 iterations.`

---

## 3. Exact C# Contracts, LOA Allocation & Patterns

### 3.1 Named Patterns (LOA Part IV + GoF)
- **Tool-Mediated Constructor + Generate-Verify-Select (LOA Archetypes C & E):** LLM emits only schema-checked `requirement-decomp`, `candidate-intent-delta`, or `AnalysisExplanation` JSON; Tier-0 C# owns all geometry, local physics, `AI-03` numeral grounding, and `LAB-01` copy safety.
- **Immutable Versioned Pipeline (`FoilReq` & `CandidateIteration`):** Append-only snapshots preserve prior valid state across API errors, refusals, or branch re-opens.
- **Mandatory Untrusted-Excerpt Fencing + Consented Bounded Repair:** Every untrusted user string enters `PromptRedactor.RedactPayload` exclusively via `UntrustedExcerpt` (NEVER raw `userPrompt`), every LLM call requires explicit `SHA-256` wire consent, and repair loops are strictly user-triggered and capped at `max_repair_attempts = 2`.

### 3.2 Pure Core Contracts (`CfdWorkbench.Core/Notebook/` — zero I/O, `STORE-SUBSET` & `ArchitectureTests` clean)
- **`FoilReqParser.Parse(string dslText)` → `FoilReqParseResult`:** Strict recursive-descent parser implementing the closed §A5 EBNF from `docs/specs/ai-guided-design-notebook.md:143-192` verbatim.
- **`FoilReqValidator.Validate(FoilReqDocument doc)` → `FoilReqValidationResult`:**
  1. **Dimensional sanity (`TQ7`):** Lengths convert to metres; wing span outside `[0.35 m, 2.20 m]` (e.g., `950 cm = 9.50 m`) emits deterministic `UNIT-SANITY` finding (`COPY-926`) proposing `950 mm`.
  2. **Water properties & kit mass (`DisciplinePresets`, `ANA-19`):** Uses `DisciplinePresets.DefaultSaltWater15C` (`1026.0210 kg/m³`) / `DefaultFreshWater15C` (`999.1026 kg/m³`) and temperature-adjusted water density matching `water.kind` & `water.temp`. When only `rider.mass` is stated, defaults kit mass to `15.0 kg @defaulted` (`board 5.5 + foil 4.5 + wing_kit 5.0 kg`, tagged explicitly `@defaulted`). Computes `S_implied = b² / AR`, `W = m_total * 9.80665`, `CL_req = W / (0.5 * ρ * V_to² * S_implied)`; if `CL_req > 0.95`, emits `PHYSICS-TENSION`.
  3. **Free-text coordinate & IP firewall (`COMMIT-02`, `COMMIT-04`):** Enforces `reference.policy == "conceptual-only-no-proprietary-geometry"` and `confidence in {Inferred, Flagged}`, AND scans `maneuvers`, `open_questions`, and custom grill text rejecting numeric coordinate-pair tables (`>= 4` pairs), `.dat` headers, or proprietary geometry arrays (`NB-COORD-FORBIDDEN`).
- **`GrillTreeEngine.NextQuestion(FoilReqDocument doc, IReadOnlyList<GrillResolution> history)`:** Enforces at most 1 active blocking question (`Q1` unit sanity → `Q2` physics tension → `Q3` speed/cavitation → `Q4` maneuver feel); supports `Answer`, `Skip` (`Flagged`), `Reopen` (appends `GrillReopen`, deriving `IsStale = true` on older candidates), and `GenerateNow`.
- **`CandidateIntentValidator.Validate(string rawJson, CandidateValidationContext ctx)`:**
  - Reuses `internal static AssistantValidators.TryParseStrictObject` and `ForbiddenGeometryOrPhysicsFields`.
  - Validates bounded geometric recipe fields (`SpanMeters in [0.35, 2.20]`, `AreaSquareMeters`, `Taper in [0.15, 0.95]`, `RootThicknessRatio in [0.06, 0.20]`, `AnhedralDeg in [-15, 10]`, `WashoutDeg in [-6, 4]`, `SweepQuarterChordDeg in [-10, 30]`, `RootProfileId` & `TipProfileId` in admitted `GEN` or supplied `My sections` snapshot — zero disabled `VEND`/`LINK` IDs!).
  - Optional Wave 13A rounded-tip seam fields (`TipLeRadiusMm?`, `TipTeRadiusMm?`, `TipStyle?`, `TipCapRatio?`) validated when present.
- **Two-Profile `RecipeSeeder.EmitFoilDsl` (`Core/RecipeSeeder.cs`):**
  - Add pure zero-I/O overload `EmitFoilDsl(RecipeRecord recipe, string rootId, string rootDatText, string tipId, string tipDatText, string foilName = "Seeded Wing")` emitting both profiles when `rootId != tipId` and `at root profile "<rootId>" at tip profile "<tipId>"`. Existing 3-argument overload delegates to it with `root == tip`.
- **`RequirementScorecard.Evaluate(...)` & Canonical `solver_context` Formatting (`AI-03`):**
  - Computes exact `AR = b²/S`, `CL`, `Re_MAC`, `Wing-only L/D` (explicitly labelled wing-only — fuselage/mast/stabilizer unmodelled), and `v_a → v_b` deltas (`Δspan`, `Δarea`, `ΔL/D %`, `Δroll_inertia %`, `Δtakeoff`).
  - **Enforces `ANA-19`:** When `water.depth_ref` is omitted or `'?'`, cavitation (`σ`, `σ/(−Cp_min)`, `V_crit`) and free-surface (`h/c_root`, `Fr_h`) report **`Unavailable — depth not set`** (never silently assuming `0.45 m`). When `depth_ref` is set and `h/c_root < 5`, attaches mandatory `A5.1` notice (`h/c`, `Fr_h`).
  - **Canonical `NumeralTokenRegex` formatting:** `RequirementScorecard.FormatSolverContext(...)` formats every numeric value in `solver_context` in plain decimal tokens matching `AssistantValidators.NumeralTokenRegex` (`AssistantValidators.cs:258-260`, e.g., `1050 N`, `4.31e5`, `+7.3%`, `-9%`, `0.91`, `24.6`, `26.4`, `1.332`, `28.0 kn`, `29.5 kn`, `3.86`, `4.00`, `5.88`) — zero thousands commas or Unicode superscripts that would cause false-positive `AI-03` withholds!
- **`ArchitectureTests` compliance:** All `Core` methods called from `Analysis` use safe names (`RecordNotebookTurn`, `RecordNotebookCandidate`, `ReadNotebookSession`, `EvaluateScorecard`, `MaterializeCandidateDsl`) — zero banned names/prefixes (`ProfileAt`, `Cancel`, `Undo`, `Redo`, `Begin*`, `Update*`, `Apply*`).

### 3.3 `AssistantEvalSuite` v2.0 & `OptionalFastModelId` (`AI-04`, `AI-06`)
- **`AssistantEvalSuite.cs`:**
  - Bump `EvalThresholdsVersion = "2.0"`; add `eval-10-notebook-requirement-decomp` (`1.00`, 3 cases: `c10-f4-orca-unit-typo`, `c10-coord-table-refusal`, `c10-lab01-superlative-refusal`) and `eval-11-notebook-candidate-intent` (`1.00`, 2 cases: `c11-valid-delta-admitted-gen`, `c11-raw-coords-and-unadmitted-vend-refused`), renumbering reserved physical fidelity to `eval-12-physical-fidelity` (`11` active + `1` reserved).
  - Add `CheckModelEligibility(AssistantSettings settings, string? modelIdOverride = null)` checking receipts for `effectiveModel = modelIdOverride ?? settings.ModelId` on `(settings.ProviderKind, endpointOrigin, effectiveModel, "2.0")`.
  - Update dynamic active count across `AssistantVerb.cs:182`, `AssistantCopy.cs:98-119` (note: display uses dynamic `11` count in `AssistantSettingsDialog` while keeping approved `DESIGN.md` `COPY-886/888/891` phrasing synced cleanly), `AssistantEvalTests.cs:23-32`, `AssistantServiceTests.cs:373`, `Wave11AiUiTests.cs:145, 615-654`.
- **`OptionalFastModelId` End-to-End Propagation (single endpoint, single OS key):**
  - `AssistantSettingsStore`: add `"optionalFastModelId"` to `AllowedRootProperties`, omit when null, AND screen both `ModelId` and `OptionalFastModelId` values against `PromptRedactor.ScrubSecrets` (rejecting pasted API keys like `sk-ant-...` with `ASSISTANT-SECRET-FORBIDDEN`!).
  - `AssistantPreviewRequest` + `PreparedAssistantCall`: add `string? EffectiveModelId` (defaults to `settings.ModelId`).
  - `AssistantService`: `PreparePreviewAsync`, `ExecuteConsentedAsync` (AI-06 eligibility check, `LlmTransportRequest.ModelId`, `AssistantAuditEntry.ModelId`), and `RunEvaluationAsync(bool liveWire = false, string? targetModelId = null, CancellationToken ct = default)` all use `EffectiveModelId`.
  - `AssistantConsentPreviewDialog`: displays and audits `call.EffectiveModelId` (never `SettingsSnapshot.ModelId` when fast model is selected!) and extends `ExtractProvenance` (`:457-464`) to render `RequirementDecomp` and `CandidateIntentDelta` provenance rows.
  - `AssistantSettingsDialog` + `AssistantVerb` CLI (`assistant eval --model <id>`): expose configuring and running the 11-item evaluation for `OptionalFastModelId`.

### 3.4 Analysis Orchestration (`CfdWorkbench.Analysis/Notebook/`)
- **`NotebookOrchestrator`** composes with `AssistantService`:
  - **Untrusted-excerpt fencing (`P0-2` control):** Every call passes a **fixed trusted directive string** as `AssistantPreviewRequest.UserPrompt` (e.g., `"Decompose the quoted brief into FoilReq 1.0 JSON."`), and passes ALL user prose, commercial foil references, custom grill answers, and prior turns exclusively via `IReadOnlyList<UntrustedExcerpt>` (`kind = "user-brief" | "grill-answer" | "turn-history"`), so `PromptRedactor.RedactPayload` wraps every untrusted turn in `<<<CFDW_QUOTED_DATA ...>>>` and runs `NeutralizeDelimiters` under the `4,096 B` / `32,768 B` UTF-8 caps (with visible `COPY-899`).
  - **Three strict proposal kinds (`COMMIT-04` bounded):**
    1. Brief decomposition → `AssistantProposalKind.RequirementDecomp` (`"requirement-decomp"`).
    2. Candidate synthesis / refinement → `AssistantProposalKind.CandidateIntentDelta` (`"candidate-intent-delta"`).
    3. Physics-to-riding-feel translation → reuses existing **`AssistantProposalKind.AnalysisExplanation`** (`"explanation"`, `{ kind, text, citations: [candidateId] }`) validated by `AssistantService.ExecuteConsentedAsync` through `AssistantValidators.ValidateResponse` with `AllowedRunIds = [candidateId]` and `SharedContextText = RequirementScorecard.FormatSolverContext(...)` (enforcing `CheckExplanationTextSafety` `AI-03` + `Lab01CopyLint` cleanly via public `ValidateResponse` without calling private Core helpers!).
  - **`NotebookCandidateRunner` (`INotebookSolverRunner`):** Calls two-profile `RecipeSeeder.EmitFoilDsl` → `FoilSource.Parse` + `MaterializeIds` → `Geometry.Assess` → direct in-process `Placement.Sections` + `ProductWingMethod` (`VortexLattice.Solve` + `StripCoupler.Couple` + `NeuralFoilPolarSource`) + `PanelMethod` + `SectionTier.Evaluate` across the 3 operating points `[takeoff, glide, steep_drop]`, returning self-contained `CandidateOperatingPointSummary[]` (zero mutation to `Envelope.Analysis`!).
  - **AI-03 withhold & bounded repair:** If translation fails `AI-03` or `LAB-01`, the turn records `Withheld` (`COPY-934`) while all local solver artifacts remain visible and usable. Repair (`repairReq` / `repairGeometry`) is strictly user-initiated through full `PreparePreviewAsync` consent preview and capped at `max_repair_attempts = 2`.

---

## 4. Failure-Mode Analysis (SRE + Test Architect)

| Category | Failure Mode | Disposition | Control / Telemetry / Test |
|---|---|---|---|
| **Input** | Empty prompt or prompt > `32,768` UTF-8 B / excerpt > `4,096` B | **Prevent / Mitigate** | Empty blocked locally (`NB-EMPTY-PROMPT`, 0 network); `PromptRedactor.RedactPayload` truncates byte-exact rune-safe with visible `COPY-899`. |
| **Input** | Dimensional typo (`"span < 950 cm"`) or impossible physics (`AR 12`, `90 kg`, `10 kn`) | **Detect + Prevent** | Deterministic `FoilReqValidator` emits `UNIT-SANITY` (`COPY-926`) / `PHYSICS-TENSION` seeding mandatory `GrillQuestion`. |
| **Dependency (LLM)** | No API key configured (`AI-01`) or key deleted mid-session | **Degrade cleanly** | Composer & payload preview disabled (`COPY-933`), `Configure key` + `Use manual Setup / CAD` active, 0 network calls, prior candidates intact. |
| **Dependency (LLM)** | Primary or fast model unevaluated on `EvalThresholdsVersion "2.0"` (`AI-06`) | **Prevent** | Live proposals locked with `ASSISTANT-UNEVALUATED` (`COPY-887`) until 11-item suite passes on `(provider, endpointOrigin, EffectiveModelId, "2.0")`. |
| **Dependency (LLM)** | Timeout, HTTP 401/429/5xx, redirect, >1 MiB response, or UTC daily cap hit | **Recover** | `ByokLlmTransport` fails closed (`AllowAutoRedirect = false`, `MaxResponseBytes = 1 MiB`), preserves all prior `v1..v(n-1)` & `Open in CAD Mode →`, surfaces `COPY-914` / `COPY-885`. |
| **Dependency (LLM)** | LLM hallucinates ungrounded numeral or banned `LAB-01` word in translation | **Prevent / Degrade** | `AssistantValidators.ValidateResponse` (`AnalysisExplanation`) withholds prose (`COPY-934`), keeping all Tier-0 local physics artifacts visible. |
| **Dependency (LLM)** | LLM emits raw coordinates, forces, or disabled `VEND`/`LINK` profile ID | **Prevent** | `FoilReqValidator` + `CandidateIntentValidator` reject (`NB-COORD-FORBIDDEN` / `CAT-UNAVAILABLE`), zero invalid state enters session. |
| **Local Solver** | Candidate fails `Geometry.Assess` (`DSL-TIP-CHORD-MIN`) or polar out-of-Re-envelope | **Detect + Mitigate** | Candidate marked `Refused` / `Unavailable — <reason>`, `Open in CAD Mode →` disabled (`COPY-931`), last checked baseline (`v1`) stays inspectable; user-consented repair capped at `max_repair_attempts = 2`. |
| **State / Persistence** | Pre-handoff notebook launched from Start (`!foilOpen`) attempts `.cfdw.json` save | **Prevent** | Lives in session memory until `HandoffNotebookCandidate` seeds `r1` (`Edit = null`, cursor `"open"`); saving before `r1` guides user to confirm CAD handoff first (preserving `AuthoringSession.cs:3496`). |
| **State / Persistence** | Long transcript/DSL/profile line exceeds `NativeProject`'s `4,096` UTF-8 B per-line limit | **Prevent** | `NotebookRecords` splits all text fields into `<= 1,024` UTF-8 B raw chunk elements (encoded JSON line `<= 3,800` B); tested on 20 KB transcripts with heavy escaping. |
| **State / Persistence** | Project approaches `32,000,000` B `NativeProject.MaxBytes` cap | **Prevent** | Unaccepted `FoilDSL` bytes are NOT stored (rebuilt from `delta`), and `PreflightBytes` refuses before partial write (`DOC-SIZE`). |
| **State / Consistency** | User re-opens a prior grill pill after `v2` was generated | **Detect** | Appends `GrillReopen` turn; derived `IsStale = (cand.ReqVersionId != latest.ReqVersionId)` flags artifacts **`Stale`** (`TQ9`). |
| **Concurrency** | User cancels mid-stream or mid-local-solver | **Recover** | Cooperative `CancellationToken` aborts HTTP/solver within `<= 500 ms` and returns to last stable turn with zero partial writes. |
| **Resource** | Unbounded LLM repair loop burns API quota | **Prevent** | Zero automatic retries; every repair requires explicit user click + consent preview + hard cap `max_repair_attempts = 2`. |

---

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| User prompt / custom grill text → `PromptRedactor` → BYOK LLM | **I:** OS paths, usernames, hostnames, API keys, or DRC attributions exfiltrated to remote LLM | **mitigate** | Mandatory `PromptRedactor.RedactPayload` + byte-exact `SHA-256` wire consent gate (`ASSISTANT-CONSENT-HASH-MISMATCH`) + `ByokLlmTransport.ScrubSecrets` | `Notebook_OutboundPayload_RedactsPathsUsersKeysAndVerifiesSha256` |
| Multi-turn user/reference/grill text → LLM context | **T / E:** Prompt injection inside brief, reference foil name, custom grill answer, or prior turn breaks delimiter or bypasses `COMMIT-02`/`COMMIT-04` | **mitigate** | Fixed trusted directive in `UserPrompt`; **every** user brief, reference, custom grill answer, and prior turn passed exclusively as `UntrustedExcerpt` → wrapped in `<<<CFDW_QUOTED_DATA` + `NeutralizeDelimiters` + strict JSON schema + `ForbiddenGeometryOrPhysicsFields` + admitted-catalog allowlist | `Notebook_PromptInjectionInBriefOrGrill_FencedNeutralizedAndRefused` |
| LLM response → `FoilReq` / `CandidateIntentValidator` / `AnalysisExplanation` | **T:** LLM sneaks proprietary coordinates, fabricated lift/drag forces, or `LAB-01` safety claims (`"cavitation-free"`) | **mitigate** | Coordinate-pair regex + `ForbiddenGeometryOrPhysicsFields` + `AI-03` numeral grounding against canonical `solver_context` + `Lab01CopyLint` | `Notebook_LlmCoordsForcesOrLab01Claim_RejectedAndWithheld` |
| LLM repair action → `AssistantService` | **D:** Malicious or looping LLM triggers runaway API spend | **mitigate** | Explicit user consent required per attempt + UTC daily cap + `max_repair_attempts = 2` hard bound | `Notebook_RepairLoop_BlocksThirdAttemptAndRequiresConsent` |
| `.cfdw.json` (`Envelope.notebook`) → `NativeProject.Read` | **T / D:** Crafted `.cfdw.json` with unknown keys, `> 4096 B` lines, empty `notebook`, forged `CadHandoffRecord`, or tampered `SurfaceHash` | **mitigate** | `UnmappedMemberHandling = Disallow`, `Exact` allowlist (`DOC-UNSUPPORTED-FIELD`), non-empty guard, per-line preflight, `NativeProject.Check` reference & rebuild-hash verification (`DOC-REFERENCE`) | `Reopen_TamperedNotebookEnvelope_RefusedDocReferenceOrSchema` |
| `cfdw-assistant v1` (`optionalFastModelId`) → `AssistantSettingsStore` | **E / I:** API key pasted into `optionalFastModelId`/`modelId` value, or unverified fast model bypasses `AI-06` or spoofs consent dialog | **mitigate** | Value-level `PromptRedactor.ScrubSecrets` screening (`ASSISTANT-SECRET-FORBIDDEN`), `<= 256` char no-control check, single `EndpointUrl`, mandatory `AI-06` `"2.0"` receipt for `EffectiveModelId`, and `AssistantConsentPreviewDialog` renders & audits `EffectiveModelId` | `Settings_FastModelSecretOrUnevaluated_Blocked` |
| OS credential store → `ByokLlmTransport` | **S / I:** Endpoint redirect leaks `Authorization` / `x-api-key` header | **mitigate** | `AllowAutoRedirect = false`, strict HTTPS or exact loopback `ValidateEndpointUri`, live key read only inside `ExecuteConsentedAsync` | `Transport_RedirectAttempt_RefusedWithoutKeyLeak` |
| Local audit & telemetry | **R:** Missing record of outbound AI request or CAD handoff | **mitigate** | Every outbound LLM call writes `AssistantAuditEntry` (`payloadSha256`, `EffectiveModelId`, `disposition`); every CAD handoff writes append-only `CadHandoffRecord` | `Notebook_LlmCallAndCadHandoff_AuditedEndToEnd` |

---

## Privacy analysis (LINDDUN-lite)

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| Rider mass, skill level, and riding brief text sent to user-configured BYOK LLM | **D / I / U:** User's personal riding profile & prose cross device boundary to user-chosen LLM provider | **mitigate** | Explicit **byte-exact redacted payload preview** (`AssistantConsentPreviewDialog`, `COPY-896..900`, showing `EffectiveModelId`) before every send; zero silent sends; `PromptRedactor` strips OS paths, usernames, hostnames, and secrets; zero CFD-Workbench server or telemetry | User-chosen BYOK provider retention applies once user clicks **Send to model**; local copy lives only in user's `.cfdw.json` (user can delete file at any time) |
| Local `.cfdw.json` `Envelope.notebook` transcript | **L / U:** Retained prompts and grill answers remain in saved project file | **mitigate** | Explicit disclosure in `COPY-932` (*"Your notebook transcript and FoilReq stay saved in project history"*) + inspectable in UI | Local file under OS-user ACL; deleted when user deletes `.cfdw.json` |
| Local telemetry ring & `assistant-audit.jsonl` | **D:** Prompts or rider mass leaking into telemetry/audit logs | **mitigate** | Telemetry & audit entries store only `payloadSha256`, byte count, token counts, duration, `EffectiveModelId`, disposition, and error code — **zero prompt text, zero rider mass, zero paths/names** | Audit JSONL under user's private `0700`/DACL app data dir; telemetry ring in-memory only (cleared on exit) |

---

## 7. Telemetry & Observability (O1–O13)

- **Structured local events (`AuthoringSession` ring + `AssistantService.TelemetryEmitted`, zero prompt/PII):**
  - `notebook.session.open`: `{ hasExisting: bool, hasKey: bool, evalPassed: bool, openMs: double }`
  - `notebook.decomp`: `{ modelId: string, payloadBytes: int, valid: bool, unitFlagged: bool, physicsTension: bool, durationMs: double, code: string }`
  - `notebook.grill.step`: `{ action: "answer"|"skip"|"reopen"|"generate_now", openCount: int, markedStale: bool }`
  - `notebook.candidate.run`: `{ candidateOrdinal: int, geometryStatus: string, solverWallMs: double, opCount: int, repairAttempt: int, code: string }`
  - `notebook.handoff`: `{ candidateOrdinal: int, revisionOrdinal: int, durationMs: double, outcome: "ok"|"refused", code: string }`
- **HTTP / RFC 9457 note:** CFD-Workbench exposes no HTTP server; outbound BYOK HTTP failures map deterministically to existing `AssistantTransportErrorKind` (`ByokLlmTransport.cs:12-24`: `Timeout`, `AuthenticationFailed`, `RateLimitedOrQuota`, `RedirectRefused`, `ResponseTooLarge`, `NetworkUnavailable`) + `COPY-914`.
- **Stable error codes:** `NB-EMPTY-PROMPT`, `NB-COORD-FORBIDDEN`, `NB-FOILREQ-SYNTAX`, `NB-REPAIR-CAP-REACHED`, `NB-CANDIDATE-NOT-CERTIFIED`, `ASSISTANT-NO-KEY`, `ASSISTANT-UNEVALUATED`, `ASSISTANT-DAILY-CAP`, `ASSISTANT-CONSENT-HASH-MISMATCH`, `ASSISTANT-SECRET-FORBIDDEN`, `DOC-SIZE`, `DOC-REFERENCE`, `DOC-UNSUPPORTED-FIELD`.

---

## 8. Native Avalonia UI Design (`NotebookWorkspaceView`, U1–U20, TQ1–TQ12)

- **Archetype Signature (from `spec-ai-guided-design-notebook` §C1 & approved mockup):** `GuidedDesignNotebook` (G3 Notebook + G1 Synchronized Artifact Workbench companion).
- **Shell Integration (strictly respecting `Wave10UiTests` / `ShellModelTests` / `ControllerSectionTests` / `TokenDisciplineTests`):**
  - `ActivityRail`: add `WorkbenchArea.Notebook`, `RailNotebookButton` (`BadgeNotebookText`, visible when a notebook session is active or attached to the open project per `spec-ai-guided-design-notebook` §B2), keep `ActivityRailWidth = 84 DIP` and `0` `Dock.*` references.
  - `ModelArea.axaml(.cs)`: add `<notebook:NotebookWorkspaceView x:Name="NotebookWorkspace" IsVisible="False" />` sibling to `SetupWorkspace`, `SweepsWorkspace`, `RunWorkspace`, `ResultsWorkspace`; update ALL `!controller.IsSetup && !controller.IsRun && !controller.IsResults` checks (`StartCardView.IsVisible`, `PlanContent.IsVisible`, `Mode`, `ViewArrangementGrid.IsVisible` at `:608-622`) to include `!controller.IsNotebook`.
  - `StartView.axaml`: upgrade the 3-column card grid (`:42`) to a responsive **2×2 grid** (`ColumnDefinitions="*,*"`, `RowDefinitions="Auto,Auto"`) adding **`New AI-guided design`** (`COPY-924`) without clipping at `1024 px` / `200%` text scaling, while preserving every existing `x:Name`.
  - `CommandTable.cs` & `ShellHost.cs`: add `view.notebook` (`Title: "New AI-guided design"`, `Menu: "File"`, `Modal: false` — must cleanly switch even when invoked while in Section mode per `ControllerSectionTests.cs:747-769`), add `"view.notebook"` to `ShellHost.IsPaneCommand` (`:1976-1984`) and `ShellModelTests.cs:414` gesture-less allowlist; `ShellHost.SyncDockAutoCollapse()` (`:1853-1887`) collapses CAD docks on `WorkbenchArea.Notebook` and restores them on `Cad`/`Analysis`.
- **Closed Token Set (`Styles.axaml:15-24, 41-138` — ZERO new theme brushes!):**
  - Left pane chrome: `CanvasBrush`, `SurfaceBrush`, `SurfaceSoftBrush`, `InkBrush`, `MutedBrush`, `LineBrush`, `ControlLineBrush`, `PrimaryBrush`, `OnPrimaryBrush`, `SelectionBrush`, `WarningBrush`, `DangerBrush`.
  - Right graphite scientific canvas: `ViewportBrush`, `ViewportGridBrush`, `ViewportInkBrush`, `PlanSoftBrush`, `PlanMuteBrush`, `FoilBrush`, `FoilEdgeBrush`, `StationBrush`, `PlanFocusBrush`, `PlanDangerBrush`, `FoilShadeLitBrush`.
  - `vik` diverging Cp legend directly reuses existing `Styles.axaml:15-24` resources (`VikNeg2Color`/`VikNeg2Brush`..`VikPos2Color`/`VikPos2Brush` — zero hardcoded hexes, zero `Color.Parse`).
- **Right Artifacts Workbench — Reuse Map (5 arrow-key-navigable tabs + `Compare` & `Table twin`):**
  1. **`2D Section & Cp`**: reuses `SectionProfileView` + `SectionChartView` (`−Cp` upward, `σ` line, `Vik*Brush` legend strip, governing station `η` & depth).
  2. **`Polars & Curves`**: reuses `PerformanceCurveChart` + `LoadingChart` + live dynamic `Table twin` (`A5.3` / `ANA-05` notices, explicit **`Wing-only L/D`** label).
  3. **`Plan & Elevations`**: reuses `PlanformPreviewControl` + lightweight orthographic Front/Starboard elevation canvas (`A5.1` `h/c < 5` + `Fr_h` notice).
  4. **`3D Wing`**: reuses `SurfaceRenderer` + `ViewCamera` inside `NotebookWing3dControl` (pointer + keyboard orbit, Shaded/Wireframe toggle).
  5. **`Scorecard & FoilReq`**: `NotebookScorecardView` (clause table + `FoilReq 1.0` monospace view with Copy button).
- **Modals (`DialogChrome` 200%-scaling compliant):**
  - Outbound preview reuses **`AssistantConsentPreviewDialog`** (`COPY-896..900`, showing `EffectiveModelId`, dynamic UTF-8 byte count, `ExtractProvenance` for both new kinds, disabled in `no-key`).
  - CAD handoff uses **`CadHandoffDialog`** (`DialogChrome`, `COPY-932`, `SurfaceHash`, station/section summary, derived `GoalStateVersion`, `Stay in Notebook` / `Confirm & Open in CAD`, Escape/focus trap/focus restoration).
- **Approved Copy (`DESIGN.md:1178-1188`):** `COPY-924..934` (all operator-approved 2026-11-27, `LAB-01` clean).
- **HAX & Shape-of-AI (`U13–U15`):** Wayfinders (`empty` starter chips, `no-key` `Configure key` + `Use manual Setup / CAD`), Tuners (quick refinement chips + pinned `FoilReq` chips), Governors (serial 1-at-a-time `GrillCard`, byte-exact `AssistantConsentPreviewDialog`, explicit `CadHandoffDialog`, `max_repair_attempts = 2`), Trust builders (`A5.1`/`A5.3`/`A5.4`/`ANA-05`/`ANA-19` provenance strip, `Flagged` reference foil disclosure `COPY-927`, `AI-03` withheld banner `COPY-934`).
- **UI Craft Gate (`CD8`):** `xaml-token-lint.py` + `TokenDisciplineTests` + native Avalonia Desktop harness verifying all 7 states across Light/Dark/HighContrast (live OS screen-reader validation remains excluded per standing operator directive).

---

## 9. Testing Strategy Trigger Union & Red-First Test Plan (Test Architect Gate)

**Triggered directives:** **D0** (hygiene, red-first) + **D1** (pure parser/validator/physics math) + **D2** (append-only `Envelope.notebook` persistence, rebuild equality, CAD handoff undo/redo) + **D3** (async cancellation, real local loopback `HttpListener` wire transport) + **D6** (Avalonia Desktop 7-state harness, `CommandTable`/`ShellModelTests`/`ControllerSectionTests`/`TokenDisciplineTests`) + **D7** (security: `UntrustedExcerpt` fencing, redaction, prompt injection, `AI-03`/`AI-04`/`AI-06`/`LAB-01`, tampered envelope, zero-PII telemetry).

| Test ID | Project | Behavior & Killable Red-First Mutant Verified |
|---|---|---|
| `T-NB-01` | `Core.Tests` | `FoilReqParser` golden round-trip on worked F4 Orca DSL (`grill0` & `iterated`), depth-unset `water`, invalid syntax rejection (`NB-FOILREQ-SYNTAX`). *Mutant:* drop optional rationale on `@flagged("...")` → RED. |
| `T-NB-02` | `Core.Tests` | `FoilReqValidator`: catches `"950 cm"` (`UNIT-SANITY`), detects `90 kg` + `15 kg @defaulted` kit + `span <= 950 mm` + `AR 12` (`CL_req = 1.028 > 0.95`, `PHYSICS-TENSION`) using `DisciplinePresets` salt/fresh water density, rejects coordinate tables in `reference`/`maneuvers`/`open_questions` (`NB-COORD-FORBIDDEN`). *Mutant:* bypass `UNIT-SANITY` or `PHYSICS-TENSION` check → RED. |
| `T-NB-03` | `Core.Tests` | `GrillTreeEngine`: strictly 1 active blocking question at a time; answering appends `FoilReq` version; re-opening prior pill marks downstream candidates `Stale`; `Skip` & `GenerateNow` mark open clauses `Flagged`. *Mutant:* leave `IsStale == false` after `GrillReopen` → RED. |
| `T-NB-04` | `Core.Tests` | `CandidateIntentValidator` + two-profile `RecipeSeeder.EmitFoilDsl` + `RequirementScorecard`: rejects raw coordinates/forces/disabled `VEND` IDs; exact ITTC 15 °C arithmetic & `ANA-19` depth-unset → `Unavailable`; `FormatSolverContext` passes `FindUnsupportedNumeral` with 0 false positives while fabricated numeral withholds (`COPY-934`). *Mutant:* skip `ForbiddenGeometryOrPhysicsFields` or admit `VEND` profile → RED. |
| `T-NB-05` | `Core.Tests` | `Envelope.Notebook` expand-migrate save/reopen, **`<= 1,024 B` chunking test on 20 KB heavily-escaped transcript proving every JSON line `<= 4,096` UTF-8 B**, rebuild equality (`SurfaceHash_Rebuilt == SurfaceHash_Recorded`) for both `GEN` and snapshot-embedded `My sections` profiles, tampered `CadHandoffRecord` → `DOC-REFERENCE`, `HandoffNotebookCandidate` covers both `current == null` (`Edit = null`, `"open"`) and `current != null` (`Edit.Rail = "seed"`, `"apply"`, 1-step Undo). *Mutant:* remove `<= 1,024 B` line chunking or drop `DOC-REFERENCE` check → RED. |
| `T-NB-06` | `Core.Tests` + `Cli.Tests` | `AssistantEvalSuite` v2.0 (11 active items): old `"1.0"` receipt locks proposals (`ASSISTANT-UNEVALUATED`); passing `"2.0"` unlocks; `OptionalFastModelId` expand-only round-trip in `AssistantSettingsStore` (omitted when null, blocks pasted `sk-ant-...` with `ASSISTANT-SECRET-FORBIDDEN`, requires own `"2.0"` receipt); `cfd-workbench notebook inspect|validate` & `assistant eval --model` dynamic active count. *Mutant:* skip `OptionalFastModelId` `AI-06` receipt check or value-level secret scan → RED. |
| `T-NB-07` | `Analysis.Tests` | Real local loopback `HttpListener` + `ByokLlmTransport` end-to-end `NotebookOrchestrator` + `NotebookCandidateRunner` (direct `ProductWingMethod`/`PanelMethod`/`NeuralFoilPolarSource`, zero `Envelope.Analysis` pollution): verifies EVERY user brief/grill/history turn is fenced inside `<<<CFDW_QUOTED_DATA` (`UntrustedExcerpt`), byte-exact `SHA-256` consent gate (`ASSISTANT-CONSENT-HASH-MISMATCH`), `max_repair_attempts = 2` blocks 3rd repair & requires consent, `AI-03` withheld translation (`AnalysisExplanation`) keeps local artifacts intact, mid-run `CancellationToken` cancels `<= 500 ms`, `ArchitectureTests` PE IL gate passes. *Mutant:* pass user brief in raw `UserPrompt` (unfenced) or skip `ValidateResponse` on translation → RED. |
| `T-NB-08` | `Desktop.Tests` | `NotebookWorkspaceView` renders all 7 states across Light/Dark/HighContrast with `COPY-924..934`; `Open in CAD Mode →` disabled until `GeometryStatus.Certified` (disabled on `refused`, enabled on `v1`); `AssistantConsentPreviewDialog` displays `EffectiveModelId` & `ExtractProvenance` for both new kinds; `CadHandoffDialog` appends `rN` and switches to Area 2 CAD; `StartView` 2×2 grid fits at `1024 px` / 200%; `ShellModelTests`, `ControllerSectionTests` (incl. `view.notebook` from Section mode), `Wave10UiTests` (`ActivityRail` `84 DIP`, `0 Dock.*`), `TokenDisciplineTests`, and `xaml-token-lint.py` all pass. *Mutant:* enable `Open in CAD Mode →` on `Refused` candidate → RED. |

---

## 10. Confidence Ledger & Residual Risk

| Item | Confidence | Residual Risk & Mitigation |
|---|---|---|
| `.cfdw.json` `<= 4,096 B` per-line constraint & `<= 1,024 B` line-array chunking | **Verified** (`AuthoringSession.cs:3334, 3371`) | Tested on 20 KB heavily-escaped transcripts so encoded JSON lines stay `<= 3,800` B. |
| Direct `ProductWingMethod`/`PanelMethod`/`NeuralFoilPolarSource` in `NotebookCandidateRunner` (zero `Envelope.Analysis` pollution) | **Verified** (`RunRecord.cs:310-311`, `AnalysisService.cs:113-188`) | Keeps `RunRecord.CheckRow` 100% intact; self-contained `CandidateOperatingPointSummary[]` persists cleanly. |
| `UntrustedExcerpt` fencing & `EffectiveModelId` end-to-end propagation | **Verified** (`PromptRedactor.cs:106-152`, `AssistantService.cs:374`) | Eliminates raw-prompt injection gap and consent-preview model-ID drift. |
| Local 3-OP solver latency `<= 5 s` p95 inside notebook loop | **Inferred** from existing `AnalysisService` timings | Must benchmark on Windows ARM64/x64 & macOS ARM64 during Wave 13B `/implement`; narrated progress + Cancel mitigate slower machines. |
| Wave 13A → 13B rounded-tip seam | **Verified** (`design-rounded-wing-tips.md`) | Optional `TipLeRadiusMm`/`TipTeRadiusMm`/`TipStyle` fields on `CandidateIntentDelta` degrade cleanly if Wave 13B runs before Wave 13A, and light up automatically once `foildsl "4.2"` lands. |
