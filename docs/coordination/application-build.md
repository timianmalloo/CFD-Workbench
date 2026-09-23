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

**Authority and status, 2026-09-23.** User request: `al-01M376G6MEVMMTPKPYP4GGBGVD` (compiled `al-01M376HFY6J6F4SNW84C5J5Q4H`, dispatchable). The root Codex session leads; `cfd-owner-20260923` is the Astra technical Owner and ruling seat; `cfd-coordinator-20260923` is the Sol Coordinator and designated leader at launch. The Owner does not author track code or clear its own veto. Root performs independent review. The [execution graph](../plans/application-build.md) and [normative specification](../specs/cfd-workbench-v1.md) define scope. Owner Rulings 8–10 and root conditionally accept the bounded native M1 direction and B0 design/schema policies; serial session/schema/identity/persistence contract fixtures remain in progress, and production B/C remain behind a later G3 Owner gate.

## Layer state

| check | result | meaning |
|---|---|---|
| Pack doctor, 2026-09-23 | revision 92; 20 PASS, 2 WARN, 0 FAIL | Filesystem inventory ready; warnings are user Copilot long context and existing graph freshness flags. |
| `coord doctor`, 2026-09-23 | 3 classified patterns; `coord-regen` and `coord-register` effective; no leader at read time | Layer inherited from primary. Coordinator pins leadership before launch and reads it back. |
| Worktrees / sessions | primary plus foundation and other historic trees; Coordinator tree active; `cfd-application-20260923` and Coordinator session observed | Do not treat historical trees as free worker assignments. Runner creates fresh branches. |
| Doorbell / heartbeat | not recorded | Poll runner state and native events; do not claim hooks are observed from file presence. |
| Installed harnesses | Codex 0.155.1; Claude Code 2.1.280; Grok 1.0.41; Agy 1.2.7 | Versions observed via local CLI on macOS ARM64. Actual worker profile still requires binding/negative probe. |
| Edit-boundary capability | Grok ACP allowed write/build observed but protected outside-root write also succeeded; Agy authorized shell write denied; native Codex/Claude enforcement unqualified | Historical `coord doctor` spike is not version-bound qualification. No external profile is approved for a production track. |

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
| A · architecture author, `cfd-arch-codex-20260923` | Exact four docs and three spike source files in compiled brief, plus `docs/security/threat-model.md` and `docs/security/privacy-review.md` by Owner Ruling 5; no product implementation | Plan root review and Owner Rulings 1–3, 5, 7 | T3 design / T2 mechanical spike | 0 | 70 calls, ≤100k context, 50 min first run; no Ruling 5 budget increase | Candidate evidence; domain aggregate and durable model; native UI/accessibility/viewport/file-dialog and packaging spike; exact RFC 8785/BLAKE3/decimal vectors; conservative geometry validity oracle; atomic save faults/conflicts; compiling public contracts/fixtures; security risk dispositions; explicit Windows evidence gaps; Owner request and descendant commit | Existing Codex author thread in fresh isolated tree; underlying model ID `Not recorded` under architecture-only Ruling 7 exception. Final cwd/HEAD/paths/lifecycle and independent Astra Owner/root technical review still required. External harness qualifications are in [receipt](qualification-architecture.md). |
| B0 · serial contract completion, `cfd-contracts-author-20260923` | Exactly `docs/design/application-contracts.md`, `docs/proof/application-contracts.md`, optional `docs/adr/0004-application-project-contract.md`, both existing security rollups, `tools/spikes/ApplicationContracts/{ApplicationContracts.csproj,Program.cs}`, and `tools/spikes/application-session-contract-vectors.py`; official generated index/audit metadata only | Joined A and independent review at `73cabb89`; Owner Rulings 8–10 | T3 design / T2 executable contract fixtures | 0 | 70 calls, ≤100k context, 50 min first window; cap triggers checkpoint/replan | Complete session port vocabulary, native-v1 schema/reference/operation ID/cursor replay with durable rail edit receipt, missing-ID `.foil` acceptance, parser patch behavior, exact decimal/JCS identity with compensated exponent/zero limits, growth/retention, OS persistence no-replace/handle policy, named negative fixtures, clean descendant commit and independent Owner/root gate | Fresh requested `gpt-6-astra` built-in collaborator in isolated tree; effective model `Not recorded` under Ruling 9's B0-only exception. Actual cwd/HEAD and lifecycle evidence must be read back. Observed-only containment; no production code or G3 self-clearance. |
| B · accepted-source core | Core source/test files under exact paths frozen by A; no UI files | A artifacts and Owner G3 ruling | T2 | 0 | 90 calls, ≤100k context, ≤55 min per run | Red→green parser/validation, immutable revisions, semantic/source identity, certified admitted geometry, persistence fault/reopen proof, CLI-consumable API, descendant commit | Current Grok ACP profile rejected by Ruling 6 after protected-path failure. Isolated Codex candidate needs the same Ruling 6 model/cwd/HEAD/lifecycle/path/diff evidence and independent review; no production route yet admitted. |
| C · native adapters | GUI/CLI/viewport and accessibility test files under exact paths frozen by A; no core implementation files | A artifacts and Owner G3 ruling with compiling stubs/fixtures | T2 | 0 | 90 calls, ≤100k context, ≤55 min per run | Example/source open, one numeric independent rail draft and keyboard flow, native AX/viewport proof, same core CLI identity/diagnostics, honest Unavailable states, descendant commit | Same G3/Ruling 6 route gate; do not launch if G3 leaves a G4→G5 decision edge. |
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
| Architecture, hard ambiguity | Opus 5.5 or Codex Astra | No Opus 5.5 ID observed. Agy lists `claude-opus-4-6-thinking`; two actual profiles failed qualification. A fresh Astra spawn hit the host thread limit. Existing Codex thread model ID is not exposed. | Existing Codex author in fresh assigned tree under Ruling 7 architecture-only `Not recorded` exception; Astra Owner and root independently review, with no unsupported model claim. |
| Routine implementation | strongest available Grok or Agy | Grok 1.0.41 ACP wrote a fixture and built a .NET probe, but changed the protected outside-root sentinel without denial; loaded cwd, inference alias and cancellation unverified. Agy Gemini 3.1 Pro High has no task-bound write/build qualification. | Ruling 6 rejects this Grok profile for production and allows no further probe now. A later observed-only route, including Codex, needs G3 risk acceptance plus exact identity/cwd/HEAD, cancellation, paths/cache, serial isolation, diff and independent root review. |
| Deterministic checks/join | scripts | Python 3.14.4, Node 22.22.2, `coord` drivers effective | Execute scripts, no model. |

Capability values are **per actual worker fingerprint**: worktree isolation = observed-only until runner verifies checkout identity; instructions = observed-only until actual loaded bytes inspected; ownership hook = unsupported until an unleased edit succeeds and a leased edit is denied with unchanged bytes; permissions = unsupported until effective native policy and denial are observed; lifecycle/cancellation = observed-only until run receipts exist. A model list and `--help` flag are inventory, not qualification. Native Codex built-in collaborators remain observed-only; their hook behavior is not inferred from CLI hooks. Grok ACP edit/build succeeded, but outside-root protection failed in its selected profile; the runner's `ready_for_review` is structural, not semantic acceptance. Claude dispatch waits on a qualified adapter/native profile; root repaired the duplicate hook definitions in pack commit `62950e6`. Agy `ask` is refused by the unattended runner; `accept-edits` blocked ordinary headless shell commands, and the single Owner-approved full-auto sandbox probe unexpectedly denied an authorized shell write. No permission request will be left unattended; an unknown action causes a blocked receipt and fallback. Observed-only isolated serial Codex work remains under exact owned-path diff, precommit identity and independent root review; no enforced sandbox claim follows from that.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Independent parser, evaluator, identity and persistence authors | They share one accepted-source invariant, revision rule and canonical identity. Separating would multiply interfaces and review handoffs. |
| Solver/export/AI authors in M1 | No analysis backend selected and no M1 compute reader; prototype values cannot be production evidence. |
| Grok architecture worker now | Its ACP write/build path exists, but the protected-path failure makes it unsuitable for architecture routing under the active contract; the existing Codex author and independent Astra/root review are already active. |
| Third simultaneous external worker | G1 and G3 are serial and G4/G5 width ceiling is 2; no measured benefit justifies worker 3. |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Commit plan/registry/audit in Coordinator tree; integrate pack/hook fix `62950e6`; run `coord doctor`, docs graph and docs check; root independently reviews A contract | Initial 70-call / 30-minute setup budget; a cap triggers re-plan | The invoking checkout HEAD supplies fresh runner worktrees. |
| 2 | Pin Coordinator leader, record epoch; compile A brief, run `coord-runner prepare`, fingerprint, probe Agy native model/edit/lease denial/cancellation in an isolated worker tree; record qualification file and Owner routing request/ruling (`Ruling 1`) | One probe, 10-minute ceiling; failures retained, no replay | Unknown profile cannot be dispatched as enforced. |
| 3 | After failed external qualifications, assign A contract to existing Codex author in a fresh `coord worktree new` tree under Ruling 2; Ruling 5 later adds exactly two security rollup files; monitor branch, audit/coord events and Owner inbox | One worker; original 50-minute initial budget, 70 calls and ≤100k context remain | Architecture is the first real decision edge; runner a1/a2 remain prepared and unlaunched as retained evidence. |
| 4 | Verify A's artifacts, root/Owner vetoes; join A and independent review; dispatch B0 serial contract completion before G3 | One integrated gate pass per join, no duplicate full recount | Ruling 8 leaves a real session/schema/identity/persistence decision edge. |
| 5 | Launch B and C at width ≤2 only if G3 removes the decision edge and negative probes pass; else serialize | Two worker budgets above | The sole candidate parallel wave. |
| 6 | Verify returns, resolve seams, join in dependency order, inspect live native and CLI M1, record macOS/Windows evidence separately | One recount per join; measured later | A runnable result is the checkpoint. |
| 7 | Record planned versus actual calls/time/tokens where exposed, review pending slices and re-plan only when dependency-ready | No unbounded loop | User asked continued increments in approved scope. |

**Fan-out contract:** width ≤2 external; clean pre-prompt 429/529/timeout/EOF may retry once with backoff under same budget; each branch returns owned committed files and observed evidence; all M1-critical branches must succeed at join, partials stay unmerged with a report; one failure contains that branch and requires Owner ruling for reassignment. Every request has a deadline and fallback. Monitoring loop variant is count of branches missing verified exit evidence; floor zero; two passes without decrease trigger `coord kick` and then Owner request under CO17. A cap firing signals estimate failure, never completion.

**Leader supervision:** The first epoch expired during qualification. Coordinator reclaimed epoch 2 through the canonical leader CAS and runs a bounded 60-cycle, 60-second renewal supervisor from `/tmp/cfd-leader-renew.py`; each renewal reads back its epoch and stops on refusal. Before dispatch or join, Coordinator checks `leader who --json` and records the current live epoch. An expired lease is reacquired explicitly, never assumed live from an earlier receipt.

## Planned versus actual

| track | budget / reason for separate track | actual calls, time, tokens | seams, boundary correction, result |
|---|---|---|---|
| A | 70 calls / design needs independent Owner veto and isolated spike artifacts | Commit `a92c4e7`, clean; exact calls/tokens not recorded | Nine authored + four generated/audit paths, 30/30 primitive checks, nine native mismatch refusals, macOS/Windows publish; Ruling 8 conditionally accepts architecture only. Model ID `Not recorded` by Ruling 7; existing Codex author natural completion, cancellation not exercised. |
| B0 · contract completion | Serial prerequisite under Ruling 8 | Started from integrated `73cabb89`; call/time/token totals not yet recorded | Fresh requested Astra author in `feature/application-contracts`; exact eight substantive path allowances above, no product code. Owner/root gate pending. |
| B | 90 calls / one coherent core invariant, isolated from UI | Not recorded | Pending |
| C | 90 calls / native UI machine time and disjoint files after G3 | Not recorded | Pending |
| D | deterministic join / independent integrated proof | Not recorded | Pending |

| status | item |
|---|---|
| Completed | Pack and repo inventory, normative scope and graph, class registry, Owner Rulings 1–10, failed native boundary evidence, architecture handback `a92c4e7`, conditional independent approval, architecture/review joins through clean `73cabb89` with final 10/10 verification gates and 30/30 primitive recount plus nine native mismatch refusals. |
| Remaining | B0 serial contract completion and G3 freeze, production implementation and integrated application proof. |
| Best next action | Supervise B0 fixtures and Owner/root vetoes; freeze compiling public contracts before any production B/C dispatch. |
