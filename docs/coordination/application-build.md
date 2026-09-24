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
  Track accepted architecture and core, isolated permission and adapter repairs, and the independent M1 join gates.
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
| B · accepted-source core | [Ruling 13 frozen exact 18-path packet](contract-b-core.md), including owned proof/gate; no UI files | B0 Ruling 12 PASS and Owner G3 Ruling 13 | T2 | 0 | 90 calls, ≤100k context, ≤55 min first run, followed by bounded continuations | Original B joined at `18278c4`; Ruling 28 accepted six-path macOS permission correction `ee6d73ad` after independent [native review](../reviews/ui-application-native.md), joined B-only at `a550466` with affected core recount and 11 gates | Ruling 11 requested Astra/effective Not recorded built-in author. The permission repair used a separately authorized requested-Astra author; effective model Not recorded. Windows/Intel/ACL/release coverage remains open. |
| C · native adapters, `cfd-adapters-20260923` | [Exact 24-path contract](contract-c-native.md), no core implementation files; Ruling 29 narrowed its continuation to seven changed C paths | Joined B `a550466`, independently reviewed [compiled API freeze](contract-c-api-freeze.md), Owner Rulings 21 and 29 | T2 | 0 | Initial 90 calls / 55 minutes; Ruling 29 first and second bounded checkpoints, aggregate calls/tokens Not recorded | Clean isolated `3a1d67b` repaired the five portable-tooling findings and passed one final 11-step adapter gate. Its 93 diagnostic records measure fresh target compositor batch cycles, not display presentation. A source-bound light/Example app is running for root CUA; the first exact-path bind returned `cgWindowNotFound` and a user front-window reply is pending. | Requested Sol, effective model Not recorded. Root UX/performance veto and C/M1 product join remain open; no Windows runtime claim. |
| D · integration and independent proof | Coordinator owns merge and plan/status ledger; root/Owner own independent review/rulings, no production co-authoring | B accepted; C source-bound gate and timing evidence exist but final native state/presentation coverage remains open | T2 | 0 | One affected recount per eligible product join; docs-only joins do not clear product gates | B permission repair joined at `a550466`; root native and R29 timing reviews joined docs-only at `7eeccaa`. C product source remains isolated. | Deterministic scripts, root independent veto and Owner Rulings 28–29. |

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
| B | 90 calls / one coherent core invariant, isolated from UI | Original B `cce9ee52`, 172 author tests; permission repair `ee6d73ad`; exact aggregate calls/tokens Not recorded | Ruling 22 accepted original B at `18278c4`. Ruling 28 accepted measured macOS arm64 creation-permission correction after root's actual native `0600` Save/overwrite/Reopen; conductor B-only join `a550466` passed architecture 30, contract 93/42/2505, changed core suite and 11 integrated gates. Not Windows/Intel/ACL/release or full M1 acceptance. |
| C | Native UI machine time and disjoint files after accepted B; Ruling 29 first checkpoint ≤60 calls / 35 minutes, then bounded same-scope continuation | Clean requested-Sol/effective-Not-recorded `3a1d67b`; review-only combined `e6e5628`; downstream handoff `13c883a`; aggregate calls/tokens Not recorded | The author retained the 3+2 portable-tooling RED/GREEN and an honest compositor endpoint spike, then removed temporary drivers. The final source-bound 11-step gate passed. Root independently checked all 93 batch-cycle trial records and the final gate. OS-input timing, display presentation and the declared final native matrix remain open. No C product join. |
| D | deterministic join / independent integrated proof | B product/review integrated at `a550466`; root R29 timing reviews joined docs-only at `7eeccaa`; exact aggregate calls/tokens Not recorded | Rulings 28–29 separate completed B permission scope from open C UX/performance. Clean C `3a1d67b` remains isolated. The first exact-path CUA attach to its light/Example app returned `cgWindowNotFound`; a user front-window reply is pending. Windows runtime and distribution trust remain separate. |

| status | item |
|---|---|
| Completed | B original core and Ruling 28 macOS permission repair are joined at `a550466`; changed core recount and 11 integrated gates passed. The review-only B+C package `e6e5628` passed 11 adapter steps; root's supported CUA and disk oracle verified native Save, overwrite, Reopen with mode `0600`, plus named keyboard, edit, recovery and import flows. The original anomalous `0454` file is preserved. |
| Remaining | C remains isolated. The final gate and diagnostic batch-cycle trials do not measure the visible 5-second cold, 100 ms edit, or 250 ms preview/cancel targets. Root's final light/dark/high-contrast native state matrix is incomplete; the first light-window CUA bind failed while the user's front-window response is pending. Windows runtime/store and release trust are unassessed. |
| Best next action | Resume supported CUA on the exact source-bound light/Example window when it is visible; then inspect the dark/empty and high-contrast/dense groups serially. Root independently records the outcomes and retains the visible-performance veto unless a valid endpoint is measured. Seek Owner C disposition only after that review; do not join C product source from a green package gate alone. |

### Native visibility continuation · one optimized graph

The user replied, verbatim, “Yes, I see the workbench” and “The new CFD Workbench review window”. Root's supported CUA attachment then returned a rendered window and AX tree bound to PID 48841, start `Wed Sep 23 13:24:28 2026`, Desktop DLL `6aaff522eec3da85898685992fd3f2e3325035e2c482b9edda76b6dc42f54aa9`. This was the earlier n1 review copy; it is not a bitwise claim for the later combined package. Root's [native review](../reviews/ui-application-native.md) records the actual action/result rows. The CUA surface exposed a screenshot and AX return to the reviewer, without a documented file export; no PNG or AX file is claimed.

| Node | Dependency | Exit evidence and boundary |
|---|---|---|
| N0 · frozen identity | User visibility confirmation | Exact app path, PID/start, DLL and prior launch receipt read back; complete. |
| N1 · native inspection | N0 | Root alone exercises the frozen window and records each keyboard, state, AX and dialog row as Verified, Failed or Not assessed. Three interaction failures are observed; unexercised rows remain open. |
| N2 · bounded repair | Failed N1 rows | Existing C author owns only `MainWindow.axaml.cs`, `WorkbenchTests.cs` and bounded proof; adds executable regression controls, runs changed-source gates and commits clean. No interaction with root's frozen instance. |
| N3 · independent fixed-package review | N2 clean handback | Root binds the exact new package/hash through supported CUA, reruns failed rows and remaining native checklist, records any hard veto. Managed tests or startup alone cannot clear native behavior. |
| N4 · Owner gate and conditional join | N3 independent disposition | Owner rules on C acceptance. Conductor joins product only after a positive ruling; an unresolved native veto leaves the candidate isolated. Windows runtime and distribution trust retain their separate status. |

The loop variant is the number of unresolved native checklist rows. A confirmed hard veto stops acceptance and drives a single scoped repair; loss of CUA access stops native claims until a concrete external change. No launch-profile retry loop or partial product join is part of this continuation. Planned same-author repair checkpoint: at most 35 minutes and 60 calls before a measured replan; actual call/token use is Not recorded until handback.

### Ruling 26 · native save permission repair graph

Root's actual native Save created a file with mode `0454` while the store requested `0600`. The original 7,836-byte file and its SHA-256 `b5fa3b7cdc6360ce559881caa443b4f44e3db3bb4c9d38ecff8d6f902c5ce150` remain unchanged. Root's [independent investigation](../investigations/native-save-permissions.md) and controlled arm64 ABI comparison (receipt SHA-256 `474341b5ea211509650b24c8419998839654a8ca1383a4bf15617a7a277a587f`) are joined as documentation. Owner [Ruling 26](../notes/rulings.md#ruling-26--reopen-macos-creation-mode-boundary-with-a-serial-abi-repair) reopens only the macOS creation-permission boundary of accepted B. It authorizes a fixed-signature C bridge subject to a real .NET/package spike; no mode-after-create masking or guessed ABI layout is allowed.

| Node | Dependency | Exit evidence |
|---|---|---|
| P0 · route and isolate | C author clean `ae777e9` and Ruling 26 | Separate `feature/application-permissions` worktree/session `cfd-permissions-20260923` at clean integrated-B descendant `7417713`; requested Astra, effective model Not recorded if unavailable. One B author only. |
| P1 · native ABI/loading spike | P0 | Actual installed .NET 10 arm64 C bridge, native errno, architecture and package-relative loading observed. Failed spike stops for a typed alternative; no implementation by inference. |
| P2 · production RED and repair | P1 | Independent OS stat catches bad mode before bytes, on temp/claim/final create and overwrite under child umasks `0000`, `0022`, `0077`; real store is corrected without weakening held-directory, no-follow, atomicity, collision or cleanup checks. |
| P3 · independent store gate | P2 clean commit | Changed-source core gate and packaged native Save/Reopen, exact modes and source/history preservation. Root Security/Data/Test review, then Owner bounded gate; anomalous original untouched. |
| P4 · serial C handoff | P3 positive ruling plus C native review | Bring corrected B into clean C, rerun affected package/native checks and remaining keyboard/AX rows. Only then request C/M1 gate and an eligible conductor join. |

The author lease is initially **six exact paths**: `src/CfdWorkbench.Persistence/ProjectStore.cs`, `src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj`, `src/CfdWorkbench.Persistence/native/cfd_store.c`, `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs`, `tools/verify-application-core.py`, and `docs/proof/application-core.md`. The seventh Ruling 26 candidate, `tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`, is held until an actual test-build dependency justifies it and Coordinator freezes that seam. Official index/audit outputs remain derived/register work. Root owns the independent investigation/review; C author has no B lease. The first B checkpoint is 45 calls or 25 minutes, whichever first; a cap triggers a measured replan, not automatic scope or time expansion. Native C/M1 integration remains held throughout.

### Ruling 27 · isolated combined evidence, not a product join

Owner [Ruling 27](../notes/rulings.md#ruling-27--review-only-b-and-c-composition-for-packaged-native-proof) permits one disposable review tree because Ruling 26 requires app-bundle native Save evidence before a B Owner gate, while the C package was built before the B repair. Coordinator composed complete clean C `6f168c44` and clean B `ee6d73ad` on joined-base descendant `91c529d` in review-only tree `feature/application-combined-review-20260923`, HEAD `e6e562843b000e27542efa1ee7dc2bb251eb2b4f`, tree `097878e383da4bdbd65e53316a129f9a32641feb`. Source conflicts were absent; only generated index metadata required official regeneration. Neither product branch moved.

The retained [input manifest](/tmp/cfd-combined-review-20260923.muHX8h/combined-input-manifest.json), SHA-256 `4fa156928a6bd0ae8b0d44fc97099488c7337b27c90e1827a18cfe306e9b3799`, binds 55 inputs to their exact candidate/base blobs (30 base, 20 C, five B); its pre/post SHA is unchanged. The [combined gate receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-nko701v2/receipts/verification.json), SHA-256 `6b8a4f748f0211fb34e1677e7ed8bf9221a7bbbd1397e9806151548d6d31cf44`, reports 11 zero-exit steps, task-local outputs/caches, no remaining owned groups or collector, and 53 unchanged gate-source hashes; the manifest adds the core verifier and XAML lint script. The macOS app has Persistence DLL `35b38f817c739efd5ccc669316acee6234040ece5e66755fb405b3d18365b3be` and arm64 native helper `90181814d8acebf1b322579d0ff41964f63311e946b34c3df1c04698e9b24c88` at the package-relative loader location. Windows publication is no Windows runtime proof.

The [review-copy manifest](/tmp/cfd-combined-review-20260923.muHX8h/review-copy-manifest.json), SHA-256 `4bd4cea5765d4e79d7cbe0aea1a09cbb1463a3f0c0c4627366434728b631840b`, compares 231 files and proves only `Info.plist` `CFBundleIdentifier` changed to `com.cfdworkbench.desktop.reviewcombined27`. The [launch receipt](/private/tmp/cfd-combined-review-20260923.muHX8h/launch-receipt.json), SHA-256 `b3679315d3116bd1423b6899dddd5748e719dea09573ee81b4f4f146e64bd3c8`, binds PID 71600/start `Wed Sep 23 15:00:25 2026`, exact executable and `Window.Opened` marker to that copy. The first exact-path CUA bind returned `cgWindowNotFound`; after the user confirmed “The newest review window is visible and in front”, root attached the same instance and independently exercised native Save/overwrite/Reopen, mode `0600`, keyboard, recovery and import. The [native review](../reviews/ui-application-native.md) retains C performance and remaining UX evidence as open. The review tree/package/file receipts stay available; canonical C remains unjoined.

### Rulings 28–29 · B join and bounded C proof continuation

Owner [Ruling 28](../notes/rulings.md#ruling-28--bounded-macos-creation-permission-repair-accepted-for-b-join) accepted only the measured macOS arm64 B correction. The conductor joined root's independent review documents and then the six B source/proof paths at `a550466`; architecture 30, contracts 93 C# / 42 Python / 2505 vectors, the changed core suite and all 11 integrated gates passed. The B repair source bytes at this head match clean author `ee6d73ad`. Original B/C and review-only branches and the anomalous `0454` file remain available.

A fresh isolated downstream C handoff from `6f168c44` merged canonical B into clean `13c883a`. Its `src/tests/tools` bytes match the reviewed combined `e6e5628`, but the conductor stopped at step 8: `verify-portable-text-io.py` found three C-tooling issues and `verify-subprocess-utf8.py` found two. This is a RED handoff, not an integrated C pass. Owner [Ruling 29](../notes/rulings.md#ruling-29--bounded-c-portability-repair-and-truthful-native-timing-proof) authorized the existing requested-Sol author to repair the named paths, then establish an honest installed-Avalonia render endpoint with known-delay and stale/cancel controls before collecting timed trials. The first checkpoint is at most 60 calls or 35 minutes. Root alone judges the endpoint and subsequent native matrix; no C product join, changed threshold, Windows runtime or release claim follows from this authorization.

The author committed clean isolated C `3a1d67b`. Its proof in that isolated commit retains every one of the 93 diagnostic trial outcomes and the final [11-step receipt](/private/var/folders/8b/b13cycfj2psdxdnk19xw8jch0000gn/T/cfd-adapters-verify-c0toimuk/receipts/verification.json), SHA-256 `028de95d10c93ec85f2c5e8f7e18fdf9d1b9101d664cf785f77a41dc778f96fc`. Root independently read the receipt's 53 source and 16 binary hashes, all 11 zero-exit/quiescent steps, and the final package/helper parity. The 30 edit, 30 Preview and 30 Cancel trials plus three managed Example starts measure fresh target compositor batch completion only; they use synthetic review controls, not OS input or display-presentation observation. Root's independent [timing review](../reviews/ui-application-native.md) joined as documentation at `7eeccaa`. The exact light/Example review copy is `/private/tmp/cfd-r29-light-ui-t66srblp/CFD Workbench.app`, PID 91248, launch receipt SHA-256 `fc93b494d7ec941fecc76cac39ebdffc559dd5c3b0e4d3b835b31c34cb6cb447`. Its first supported CUA bind returned `cgWindowNotFound`; no dark or high-contrast substitute was launched. C product integration and M1 acceptance remain held.
