---
id: coordination-application-build
title: "Coordination plan - first CFD-Workbench application increment"
type: plan
status: in-progress
owner: "@cfd-coordinator-20260923"
tags: [coordination, worktrees, parallelism, application]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: spec-foildsl, rel: implements}
  - {to: plan-application-build, rel: relates-to}
  - {to: mockup-workbench-v7, rel: relates-to}
review-by: "2026-10-23"
summary: >-
  Track accepted architecture and core, one active native adapter author, and the independent M1 join gates.
review-suggested:
  - { by: spec-foildsl, on: 2026-09-23, reason: "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax." }
  - { by: plan-application-build, on: 2026-09-23, reason: "Execution readback records B0/G3 gates and serial G4 checkpoints; review coordination timing and claims." }
  - { by: mockup-workbench-v7, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
---

# First application increment: coordination plan

**Original authority and status, 2026-09-23.** User request: `al-01M376G6MEVMMTPKPYP4GGBGVD` (compiled `al-01M376HFY6J6F4SNW84C5J5Q4H`, dispatchable). The root Codex session leads; `cfd-owner-20260923` is the Astra technical Owner and ruling seat; `cfd-coordinator-20260923` is the Sol Coordinator and designated leader at launch. The Owner does not author track code or clear its own veto. Root performs independent review. The [execution graph](../plans/application-build.md) and [normative specification](../specs/cfd-workbench-v1.md) define scope. Owner Rulings 8–13 and root accept the bounded native M1 direction and joined B0 design/schema contract. Ruling 13 freezes one serial first-core implementation track with 18 exact paths; this authorizes bounded implementation, not product acceptance. Ruling 11 selects the conditional Codex route; the [cancellation drill](application-cancel-drill.md) met the observed lifecycle condition through explicit owned-child cleanup. At this original plan stage, worker identity/path/cache preflight and the serial B→C contract gate still remained.

**Historical R17–19 gate, 2026-09-23:** Rulings 17–19 chose evaluator `/2` degree-CV identity, whole-query deterministic feasibility, and a fixed directory-relative cooperative overwrite claim with measured native I/O. They authorized repair, not B acceptance. Root had the [30-path exact companion assignment](contract-r17-companions.md) in a disjoint tree while the serial core author finished its `/1` store/Ruling 16 checkpoint. The reviewed companion commit preceded the dedicated `/2` core continuation; C was then held.

**Historical R17/R18 gate readback:** Owner Ruling 20 accepted the bounded companion handoff and
the conductor joined it at `1582d69`; the clean core branch received it at
`65ac0b9`. Its first integrated verify run exposed the expected `/1` source
against `/2` fixtures plus a portable text-I/O control failure. R17 source
repair is compiling green; R18 all-query proof and the full B gate are still
open. Owner Ruling 21 conditionally routes **one future C adapter** to a fresh
requested `gpt-6-sol` built-in author under the same observed-only controls;
it does not launch C before independent B acceptance and a compiling API
freeze. That pre-repair hold has since cleared; see the current readback below.

**Historical B-to-C gate, 2026-09-23 18:32 UTC:** Owner Ruling 22 accepted complete B
`cce9ee52` with independent root scientific/store review; the conductor joined
B and root review at `18278c4`, and all 11 integrated gates passed. The final
Core/Persistence [API freeze](contract-c-api-freeze.md) compiled against that
HEAD. Root independently accepted the one-certificate 15-point sampler across
root/interior/tip and exact save-ack seam, while reserving actual UI and
performance gates. Ruling 21 then authorized one fresh requested-`gpt-6-sol`
native adapter author. The actual [C launch](application-c-launch.md) at base
`21f2f5b` read back clean cwd/branch/HEAD, live leader, zero open decisions and
no lease overlap. Effective model is Not recorded. Ruling 23 stopped the first
build when default `bin/obj` appeared in its isolated tree; one corrected
`--artifacts-path` build/test now has retained raw receipts, distinct task-local
project outputs and quiescent children. Coordinator accepted containment only.
C was then active; native product, AX, macOS live, Windows runtime, packaging
and full M1 acceptance remained open.

**Final C disposition, 2026-09-23:** clean C candidate `de105f0` passed its
11-step source/build/test/package gate, including a real apphost XAML startup
smoke and isolated macOS ARM64 and Windows x64 development packages. The
[independent native review](../reviews/ui-application-native.md) records bounded
CLI/controller/recovery evidence and root's one supported CUA exact-path bind,
which returned `cgWindowNotFound` without a screenshot, AX tree, keyboard or
rendered workflow proof. [Owner Ruling 25](../notes/rulings.md) retains C as an
isolated source-bound candidate: production C and M1 native acceptance are
**BLOCKED**, with no partial join or new launch loop. B remains joined under
Ruling 22. Windows runtime, release trust and native interaction budgets are
separate open gates.

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
| B0 · serial contract completion, `cfd-contracts-author-20260923` | Exactly `docs/design/application-contracts.md`, `docs/proof/application-contracts.md`, `docs/adr/0004-application-project-contract.md`, both existing security rollups, `tools/spikes/ApplicationContracts/{ApplicationContracts.csproj,Program.cs}`, and `tools/spikes/application-session-contract-vectors.py`; official generated index/audit metadata only | Joined A and independent review at `73cabb89`; Owner Rulings 8–10 | T3 design / T2 executable contract fixtures | 0 | 70 calls, ≤100k context, 50 min first window; cap triggers checkpoint/replan | Joined author/review at `c13db27`; 89 C# / 42 Python / 2505 vectors, 10/10 verification gates; root independent PASS and Owner Ruling 12 bounded technical PASS | Fresh requested `gpt-6-astra` built-in collaborator in isolated tree; effective model `Not recorded` under Ruling 9's B0-only exception. Observed-only containment; no production code or G3 self-clearance. |
| B · accepted-source core | [Ruling 13 frozen exact 18-path packet](contract-b-core.md), including owned proof/gate; no UI files | B0 Ruling 12 PASS and Owner G3 Ruling 13 | T2 | 0 | 90 calls, ≤100k context, ≤55 min first run, followed by bounded continuations | Clean complete `cce9ee52`, 172 author tests; independent root science/store proof and Owner Ruling 22; joined at `18278c4` with 11/11 gates | Ruling 11 requested Astra/effective Not recorded built-in author. Observed-only containment and explicit owned-child cleanup, no native enforcement claim. |
| C · native adapters, `cfd-adapters-20260923` | [Exact 24-path contract](contract-c-native.md), no core implementation files | Joined B `18278c4`, independently reviewed [compiled API freeze](contract-c-api-freeze.md), Owner Ruling 21 | T2 | 0 | 90 calls, ≤100k context, ≤55 min initial checkpoint; bounded continuations with aggregate calls/tokens Not recorded | Clean candidate `de105f0`; 11-step final gate passed with real XAML startup and packages; independent [native review](../reviews/ui-application-native.md) BLOCKS rendered/AX/keyboard acceptance after `cgWindowNotFound` | Requested Sol, effective model Not recorded. Ruling 25 keeps production C isolated and M1 native gate blocked; no partial join or Windows runtime claim. |
| D · integration and independent proof | Coordinator owns merge and plan/status ledger; root/Owner own independent review/rulings, no production co-authoring | B joined; C returned clean source-bound evidence but native veto remains | T2 | 0 | One whole-suite recount per eligible product join; docs-only joins do not clear product gates | B and its review joined at `18278c4`; final root C review and Ruling 25 joined as documents only. C production branch remains unjoined; macOS rendered/AX workflow and Windows runtime are unassessed | Deterministic scripts, root independent veto and Owner Ruling 25. |

**Per-track common contract:** brief begins with `audit-log.py start --session ... --skill ...`; exact goal/done-when, owned absolute paths, excluded neighbor paths, deadline, context ceiling, budget, fallback and return evidence are compiled and hash-pinned. Workers never use `EnterWorktree`, install the layer, spawn teams, or treat a permission denial as approval. A budget cap requires a report and re-plan, not an automatic higher cap. Every worker decision request goes `coord decide request --to cfd-owner-20260923`; the Owner rules into `docs/notes/rulings.md` through `coord decide rule`, never by accepting a worker's claim. Coordinator resolves seams or asks Owner for a ruling. The first `coord leader pin cfd-coordinator-20260923` epoch is recorded in the launch manifest and checked at join.

**Active-seat control (CAP-SEAT):** immediately before activating any worker or optional reviewer,
`list_agents` must show fewer than three active execution turns, counting root, Coordinator,
Owner and workers. An optional Owner review replaces a paused seat or waits for a worker
checkpoint; its queued message is not treated as a delivered finding. Root's attempted
fourth-seat review was stopped without a new writer or child process.

**Ruling-register propagation boundary (GRAPH-REG):** `docs/notes/rulings.md` is
written by `coord decide rule`, not by a coordination V16 review suggestion.
If `docs-graph.py flag` includes that inbound register, restore only its
generated frontmatter delta, run `docs-graph.py derive`, and require an empty
`git diff -- docs/notes/rulings.md` before a Coordinator commit. Owner rulings
and their canonical prose remain untouched.

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
| Routine implementation | strongest qualified Grok or Agy preferred by the user | Grok 1.0.41 ACP wrote a fixture and built a .NET probe, but changed the protected outside-root sentinel without denial; loaded cwd, inference alias and cancellation unverified. Agy Gemini 3.1 Pro High has no task-bound write/build qualification. Relative coding costs are Not recorded. | Ruling 6 rejects those observed profiles and bars further probe churn. Ruling 21 conditionally chooses one requested `gpt-6-sol` built-in C author after complete B and compiling API freeze, with effective model Not recorded if unexposed; observed-only identity/cwd/HEAD, lifecycle, exact path/cache, serial isolation, diff and independent root review remain floors. |
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
| B0 · contract completion | Serial prerequisite under Ruling 8 | Clean author commit `cb73079e` from integrated `73cabb89`; exact calls/tokens not recorded, 50-minute first window was replanned to a bounded ≤20-call/20-minute same-scope continuation near its estimated cap | Eight substantive plus four official metadata paths, joined with independent root review at `c13db27`. Coordinator recounted 89 C# / 42 Python checks and 2505 vectors; 10/10 gates. Root technical PASS; Owner Ruling 12 bounded PASS for G3 review. Requested Astra, effective model Not recorded under Ruling 9. Commit hook was advisory because identity was unset; twelve post-commit `allow` checks are detection, not retroactive enforcement. |
| B | 90 calls / one coherent core invariant, isolated from UI | Complete clean `cce9ee52`; exact aggregate calls/tokens Not recorded; 172 author tests | Owner Ruling 22 and root independent R17/R18 science and R16/R19 store/projection review accepted bounded B. Joined through conductor with root review at `18278c4`, 11/11 integrated gates. Not full language/Windows/UI/M1 acceptance. |
| C | 90 calls / native UI machine time and disjoint files after accepted B | Clean requested-Sol/effective-Not-recorded candidate `de105f0`; aggregate calls/tokens Not recorded; bounded author slices measured separately in its proof/audit | Earlier thin CLI/controller REDs, output drift, collector and typed-XAML startup failure were corrected with retained receipts. Final isolated [proof](../proof/application-adapters.md) records 11 zero steps, real apphost startup, 52 unchanged inputs and self-contained macOS/Windows development packages. Root independently passed named CLI/controller/recovery checks and then observed `cgWindowNotFound` on the one final CUA bind. Ruling 25 retains C unjoined and BLOCKS native acceptance. |
| D | deterministic join / independent integrated proof | B product/review integrated at `18278c4`; C source candidate unjoined; root final C review joined docs-only at `44710f7`; exact aggregate calls/tokens Not recorded | Coordinator reconciled status, proof, audit and Ruling 25 in documentation. No integrated C product or full M1 claim; native rendered/AX/keyboard and Windows runtime evidence remain absent. |

| status | item |
|---|---|
| Completed | Pack/repo graph, bounded architecture/B0 and independently accepted B; B joined at `18278c4` with 11/11 integrated gates. Clean C source candidate `de105f0` passed its final 11-step isolated gate and root's bounded CLI/controller/recovery review. Root's final native review was joined as documentation only; Ruling 25 keeps production C unjoined and native M1 blocked. |
| Remaining | Await a concrete external change in supported native window visibility/access before one fresh evidence-bound native inspection. Then root must exercise rendered Example/edit/save/recovery, keyboard/dialog/focus, AX, minimum-window/theme/hard states and interaction budgets; Owner reviews a new gate request before any C product join. Windows runtime and release trust remain separate. No further launch/profile loop is authorized by Ruling 25. |
| Best next action | Await the pending window-visibility answer or another concrete supported-tool change. Preserve the clean C candidate and exact-owned review instance; after that change, freeze its source/binary manifest and run one supported native inspection against the root checklist before a new Owner gate request. |
