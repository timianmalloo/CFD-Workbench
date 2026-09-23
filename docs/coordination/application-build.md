---
id: coordination-application-build
title: "Coordination plan - first CFD-Workbench application increment"
type: plan
status: proposed
owner: "@cfd-coordinator-20260923"
tags: [coordination, worktrees, parallelism, application]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: plan-application-build, rel: relates-to}
  - {to: mockup-workbench-v7, rel: relates-to}
review-by: "2026-10-23"
summary: >-
  Assign one architecture author first, then at most two disjoint implementation tracks after the Owner rules stable first-slice contracts.
---

# First application increment: coordination plan

**Authority and status, 2026-09-23.** User request: `al-01M376G6MEVMMTPKPYP4GGBGVD` (compiled `al-01M376HFY6J6F4SNW84C5J5Q4H`, dispatchable). The root Codex session leads; `cfd-owner-20260923` is the Astra technical Owner and ruling seat; `cfd-coordinator-20260923` is the Sol Coordinator and designated leader at launch. The Owner does not author track code or clear its own veto. Root performs independent review. The [execution graph](../plans/application-build.md) and [normative specification](../specs/cfd-workbench-v1.md) define scope. Review by root is conditional pass for the first architecture author only; downstream tracks remain behind Owner gates.

## Layer state

| check | result | meaning |
|---|---|---|
| Pack doctor, 2026-09-23 | revision 92; 20 PASS, 2 WARN, 0 FAIL | Filesystem inventory ready; warnings are user Copilot long context and existing graph freshness flags. |
| `coord doctor`, 2026-09-23 | 3 classified patterns; `coord-regen` and `coord-register` effective; no leader at read time | Layer inherited from primary. Coordinator pins leadership before launch and reads it back. |
| Worktrees / sessions | primary plus foundation and other historic trees; Coordinator tree active; `cfd-application-20260923` and Coordinator session observed | Do not treat historical trees as free worker assignments. Runner creates fresh branches. |
| Doorbell / heartbeat | not recorded | Poll runner state and native events; do not claim hooks are observed from file presence. |
| Installed harnesses | Codex 0.155.1; Claude Code 2.1.280; Grok 1.0.41; Agy 1.2.7 | Versions observed via local CLI on macOS ARM64. Actual worker profile still requires binding/negative probe. |
| Edit-boundary capability | native Codex, Claude, Grok, Agy unqualified in this task | Historical `coord doctor` spike is not version-bound qualification; commit floor remains a later guard. |

The coordination layer is installed **once in the primary checkout**. Every linked worktree inherits its Git driver/hooks; no worker installs it again. Root updated primary to pack/hook-fix commit `62950e6` and installed there. Before prepare, Coordinator incorporates that commit, runs `coord doctor`, verifies the inherited registration in each created tree, and checks `git status` in the owned checkout. A relative `apply_patch` targeted the primary during plan authoring; its exact files were moved/restored and both checkout statuses inspected. **Control:** all future file edits use absolute paths, followed by `git status --short` in both the owned worktree and primary before commit/dispatch. Root reviews the recurrence class for the defect register.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl`, `docs/notes/rulings.md`, `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | Append-only union merge through `coord-register`; use official scripts, never direct edits | No file lease; each logical entry has one author. |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `audit-log.py ... render`, executed by layer initialization | No file owner; regenerate at join. |
| `docs/docs-index.js` | derived | `docs-graph.py derive`, executed in this branch before registry addition | No file owner; regenerate at join. |
| `docs/specs/*`, `docs/adr/*`, `docs/design/*`, production source/tests, `docs/coordination/*.md` | authored | Git three-way merge; one writer per exact file | Yes; assign by track below. |
| `docs/mockups/workbench-v7.*` | authored reference | Input and proof only for M1; no worker edits | No writer in this plan. |

The existing registry had the first three audit patterns. Coordinator adds project-specific derived/register patterns in `.agents/artifacts.yml` after running the exact docs-index generator. Every other path stays `authored`. The architecture author must reserve exact file names before dispatch; no two tracks may author one path or one aggregate invariant.

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| A · architecture author, `cfd-arch-author-20260923` | Fresh architecture/ADR/design/spike docs and bounded spike code under paths declared in compiled brief; no product implementation | Plan root review, Agy qualification, Owner routing ruling | T3 design / T2 mechanical spike | 0 | 70 calls, ≤100k context, 50 min first run; runner deadline ≤3600 s | Candidate evidence; domain aggregate and durable model; native UI/accessibility/viewport/file-dialog and packaging spike; exact RFC 8785/BLAKE3/decimal vectors; conservative geometry validity oracle; atomic save faults/conflicts; compiling public contracts/fixtures; explicit Windows evidence gaps; Owner request and descendant commit | Agy 1.2.7 `claude-opus-4-6-thinking`, `accept-edits`, native stream, **pending actual negative probe**; fallback authenticated Claude/Codex through separately qualified profile or retained manual brief. |
| B · accepted-source core | Core source/test files under exact paths frozen by A; no UI files | A artifacts and Owner G3 ruling | T2 | 0 | 90 calls, ≤100k context, ≤55 min per run | Red→green parser/validation, immutable revisions, semantic/source identity, certified admitted geometry, persistence fault/reopen proof, CLI-consumable API, descendant commit | Qualified Grok 1.0.41 `grok-4.7` preferred after actual model/edit boundary probe; qualified Agy Gemini 3.1 Pro High fallback. |
| C · native adapters | GUI/CLI/viewport and accessibility test files under exact paths frozen by A; no core implementation files | A artifacts and Owner G3 ruling with compiling stubs/fixtures | T2 | 0 | 90 calls, ≤100k context, ≤55 min per run | Example/source open, one numeric independent rail draft and keyboard flow, native AX/viewport proof, same core CLI identity/diagnostics, honest Unavailable states, descendant commit | Qualified Grok 1.0.41 `grok-4.7` preferred; qualified Agy fallback; do not launch if G3 leaves a G4→G5 decision edge. |
| D · integration and independent proof | Coordinator owns merge operation and plan ledger only; root/Owner own review comments/rulings, no production co-authoring | B and C returned evidence | T2 | 0 | One whole-suite recount per join; ≤55 min join window | `conductor-join.py`, `coord regen`, integrated gates, live macOS workflow/AX screenshot and CLI/save/reopen inspection, Windows runner evidence separated, independent Data/Test/UX disposition | Deterministic scripts, root Codex/Astra and Owner review. |

**Per-track common contract:** brief begins with `audit-log.py start --session ... --skill ...`; exact goal/done-when, owned absolute paths, excluded neighbor paths, deadline, context ceiling, budget, fallback and return evidence are compiled and hash-pinned. Workers never use `EnterWorktree`, install the layer, spawn teams, or treat a permission denial as approval. A budget cap requires a report and re-plan, not an automatic higher cap. Every worker decision request goes `coord decide request --to cfd-owner-20260923`; the Owner rules into `docs/notes/rulings.md` through `coord decide rule`, never by accepting a worker's claim. Coordinator resolves seams or asks Owner for a ruling. The first `coord leader pin cfd-coordinator-20260923` epoch is recorded in the launch manifest and checked at join.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Architecture/data model and stack candidate → SDK/native spikes → public contract | Each result changes downstream shape (GO5 decision edge); no technology is selected from the mockup | A authors; Owner rules; root independently reviews |
| Semantic identity and source authority before production persistence/UI | One authored source and one semantic definition are aggregate invariants; split writers would create competing authorities | A contract, then B implementation |
| Core and adapter join → rendered application/CLI proof | Track-local green cannot prove the integrated workflow; Windows proof has separate platform coverage | Coordinator joins; root/Owner inspect |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| A -> B/C | Compiling core API, accepted-source schema, diagnostic/status vocabulary, fixtures and ownership map; source SHA-256 exact bytes distinct from semantic RFC 8785/BLAKE3 | Owner G3 numbered ruling; A authors contract |
| B -> C | If an API change is unavoidable, typed seam request with input/output example; C does not edit B's file | Coordinator boundary ruling; Owner if contract changes |
| B/C -> D | Proof pack, failing fixtures/oracles, commit and exact path inventory | Coordinator verifies and root/Owner independently veto |

**Shared surfaces and satisfiable guards:** A alone writes normative contracts. B may import only the frozen core contracts and fixture files, and may write only its implementation/test paths. C may import the same contracts and write only GUI/CLI paths. Neither branch scans the whole repository to forbid tokens in the other's authored path. Any scan-shaped guard must declare its root, recursion, token set and named allowlist alongside the plan clause it discharges. D reads all authored paths and writes only integration result/ledger, never silently repairs a track file. This permits both branches to satisfy their clauses simultaneously. If A cannot publish compiling stubs, B→C is serial; the plan's parallel width falls to 1.

## Routing table and harness qualification

| task difficulty | requested preference | installed observation | launch choice / fallback |
|---|---|---|---|
| Architecture, hard ambiguity | Opus 5.5 or Codex Astra | No Opus 5.5 ID observed. Agy lists `claude-opus-4-6-thinking`; Codex Astra exists as Owner/root seat, not a worker author. Claude Code logged in, but ACP adapter executable not found in PATH. | Agy Opus 4.6 architecture author **after negative permission/ownership probe**; Owner Astra and root independently review. If Agy fails, retain brief and qualify authenticated Claude native/manual or Codex adapter; no weakened policy. |
| Routine implementation | strongest available Grok or Agy | User re-authenticated and updated Grok. Recheck observed Grok 1.0.41 logged in with grok.com; `grok-4.7` default and `grok-4.7-build-fast` listed. Agy 1.2.7 lists `gemini-3.1-pro-high`, among others. | Prefer Grok 4.7 for routine code after its version-bound model/edit/denial qualification; Agy Gemini 3.1 Pro High is the qualified-profile fallback. |
| Deterministic checks/join | scripts | Python 3.14.4, Node 22.22.2, `coord` drivers effective | Execute scripts, no model. |

Capability values are **per actual worker fingerprint**: worktree isolation = observed-only until runner verifies checkout identity; instructions = observed-only until actual loaded bytes inspected; ownership hook = unsupported until an unleased edit succeeds and a leased edit is denied with unchanged bytes; permissions = unsupported until effective native policy and denial are observed; lifecycle/cancellation = observed-only until run receipts exist. A model list and `--help` flag are inventory, not qualification. Native Codex built-in collaborators remain observed-only; their hook behavior is not inferred from CLI hooks. Grok authentication is now observed, but edits/permission behavior are not. Claude dispatch waits on a qualified adapter/native profile; root repaired the duplicate hook definitions in pack commit `62950e6`. Agy `ask` is refused by the unattended runner; the intended profile is `accept-edits` with no trust bypass flag. No permission request will be left unattended; unknown action causes a blocked receipt and fallback. An observed-only isolated serial worker is admissible under Owner ruling if its actual model, cwd, allowed edit, denied edit, cancellation, owned-path diff and precommit identity receipts are checked; no enforced sandbox claim follows from that.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Independent parser, evaluator, identity and persistence authors | They share one accepted-source invariant, revision rule and canonical identity. Separating would multiply interfaces and review handoffs. |
| Solver/export/AI authors in M1 | No analysis backend selected and no M1 compute reader; prototype values cannot be production evidence. |
| Grok architecture worker now | Newly authenticated Grok 1.0.41 is reserved for routine implementation after its own native qualification; architecture needs Opus-class author plus Astra/root review. |
| Third simultaneous external worker | G1 and G3 are serial and G4/G5 width ceiling is 2; no measured benefit justifies worker 3. |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Commit plan/registry/audit in Coordinator tree; integrate pack/hook fix `62950e6`; run `coord doctor`, docs graph and docs check; root independently reviews A contract | Initial 70-call / 30-minute setup budget; a cap triggers re-plan | The invoking checkout HEAD supplies fresh runner worktrees. |
| 2 | Pin Coordinator leader, record epoch; compile A brief, run `coord-runner prepare`, fingerprint, probe Agy native model/edit/lease denial/cancellation in an isolated worker tree; record qualification file and Owner routing request/ruling (`Ruling 1`) | One probe, 10-minute ceiling; failures retained, no replay | Unknown profile cannot be dispatched as enforced. |
| 3 | Launch A through `coord-runner run` with exact fingerprint and bounded unattended contract; monitor structured events/status and Owner inbox; finish mailbox | One worker; 50-minute deadline, 70 calls and ≤100k context in brief | Architecture is the first real decision edge. |
| 4 | Verify A's artifacts, root/Owner vetoes; join A; issue G3 implementation contracts with exact paths and compiling fixtures | One integrated gate pass, no duplicate full recount | Stable interface required before B/C. |
| 5 | Launch B and C at width ≤2 only if G3 removes the decision edge and negative probes pass; else serialize | Two worker budgets above | The sole candidate parallel wave. |
| 6 | Verify returns, resolve seams, join in dependency order, inspect live native and CLI M1, record macOS/Windows evidence separately | One recount per join; measured later | A runnable result is the checkpoint. |
| 7 | Record planned versus actual calls/time/tokens where exposed, review pending slices and re-plan only when dependency-ready | No unbounded loop | User asked continued increments in approved scope. |

**Fan-out contract:** width ≤2 external; clean pre-prompt 429/529/timeout/EOF may retry once with backoff under same budget; each branch returns owned committed files and observed evidence; all M1-critical branches must succeed at join, partials stay unmerged with a report; one failure contains that branch and requires Owner ruling for reassignment. Every request has a deadline and fallback. Monitoring loop variant is count of branches missing verified exit evidence; floor zero; two passes without decrease trigger `coord kick` and then Owner request under CO17. A cap firing signals estimate failure, never completion.

## Planned versus actual

| track | budget / reason for separate track | actual calls, time, tokens | seams, boundary correction, result |
|---|---|---|---|
| A | 70 calls / design needs independent Owner veto and isolated spike artifacts | Not recorded | Pending |
| B | 90 calls / one coherent core invariant, isolated from UI | Not recorded | Pending |
| C | 90 calls / native UI machine time and disjoint files after G3 | Not recorded | Pending |
| D | deterministic join / independent integrated proof | Not recorded | Pending |

| status | item |
|---|---|
| Completed | Pack and repo inventory, normative scope and first graph draft, harness/model inventory, initial class registry extension. |
| Remaining | Owner A routing ruling, Agy negative qualification, architecture author launch, downstream G3 contracts and implementation. |
| Best next action | Root reviews this plan; Coordinator commits/integrates hook fix, then prepares and probes the first Agy architecture worker. |
