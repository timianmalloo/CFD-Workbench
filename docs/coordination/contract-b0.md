---
id: coordination-contract-b0
title: Serial application contract-completion author assignment
type: plan
status: active
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, contracts, design]
links:
  - {to: coordination-application-build, rel: depends-on}
  - {to: plan-application-build, rel: depends-on}
  - {to: design-application-foundation, rel: refines}
  - {to: architecture-application, rel: depends-on}
review-by: 2026-10-23
summary: Exact isolated author assignment and handback gate for the serial B0 session, schema, identity and persistence contracts before production implementation.
---

# B0 serial contract-completion packet

**Dispatch record, 2026-09-23.** The Coordinator launched a fresh built-in
collaborator with requested model `gpt-6-astra`. The worker reports that its
effective model field is **Not recorded**; Owner Ruling 9 permits that evidence
gap only for this serial design assignment. This packet records the dispatched
scope and subsequent Ruling 9 amendment; it was written after dispatch, so it
does not claim to be a prelaunch compiled fingerprint. The actual launch message
remains the collaborator assignment. No native filesystem confinement is claimed.

| Assignment field | Exact value |
|---|---|
| Session | `cfd-contracts-author-20260923` |
| Branch | `feature/application-contracts` |
| Worktree | `/Users/mallalieut/projects/CFD-Workbench-feature-application-contracts` |
| Base | `73cabb89ed7fc77b484ca7786d1b835333299795`, architecture and root review joined |
| First command | `python3 docs/ai-forward-pack/scripts/audit-log.py start --session cfd-contracts-author-20260923 --skill design-slice` in assigned cwd |
| Tier and first window | T3 design / T2 executable contract evidence; 70 tool calls, 50 minutes, ≤100k context. A cap requires a checkpoint and revised estimate, never a false PASS. |
| Author fan-out | Zero; no subagents or extra worktree |
| Independent seats | `cfd-owner-20260923` technical decisions/veto; root `cfd-application-20260923` independent review in `docs/reviews/application-contracts.md` |

## Grounding and owned paths

The author reads `AGENTS.md`, `docs/ai-forward-pack/codex.md`, the full
`design-slice` skill references, normative product/FoilDSL specs, application
architecture, ADR 0003, foundation design, architecture spike proof, Owner
Rulings 8–9 and root architecture review. Ruling 8 authorizes only this serial
contract completion, not production B/C or M1 acceptance.

The author owns exactly these substantive files:

1. `docs/design/application-contracts.md`
2. `docs/proof/application-contracts.md`
3. `docs/adr/0004-application-project-contract.md` if a load-bearing decision needs an ADR
4. `docs/security/threat-model.md`
5. `docs/security/privacy-review.md`
6. `tools/spikes/ApplicationContracts/ApplicationContracts.csproj`
7. `tools/spikes/ApplicationContracts/Program.cs`
8. `tools/spikes/application-session-contract-vectors.py`

The official generated `docs/docs-index.js` and audit/change JSONL/render
artifacts are metadata exceptions, not extra authored design paths. No worker
edits specs, `AGENTS.md`, architecture A files, ADR 0003, the plan, production
source, or unrelated paths. Scratch/cache paths stay task-local under ignored
`.contract-scratch`. Root alone authors `docs/reviews/application-contracts.md`.

## Required executable contract

Freeze the complete session operations: Open, BeginRailEdit, UpdateDraft,
Validate, Apply, Cancel, Undo, Redo, Snapshot and recovery. The session owns a
base-bound draft and target; inspection changes cannot retarget it. Validate
and Apply bind exact source bytes, accepted base, draft ID, generation,
evaluator and definition hash; retain the conservative Not assessed blocker.
Exercise target-retarget refusal, stale validation, forged identity and
deterministic replay of operation IDs/cursor facts.

Freeze native-v1 schema/reference/duplicate/extension rules and immutable
source/Design/Surface identity. Comment-only source changes keep semantic
identity. Cover source patch behavior, original-byte preservation, and
`.foil` files with missing curve-local IDs: show a deterministic candidate
source and final diff, then require explicit acceptance. Built-in Example
must launch without a hidden rewrite; reopen must not rematerialize IDs.

Execute cross-language decimal-to-SI/binary64 and RFC 8785/BLAKE3 vectors.
Numeric token ≤4096 UTF-8 bytes; bound digit/exponent scanning before
BigInteger or powers, including compensated large written exponents and
all-zero huge exponents. Ruling 9 defines supported effective magnitude,
unsupported-resource `DSL-LIMIT`, ties-to-even, subnormal/underflow and
separate structural positivity. Source SHA-256 is over exact UTF-8 bytes;
semantic identity is separate. A sample or a default .NET JSON serializer is
not proof of canonical identity.

Freeze the 8000000-byte encoded envelope, 1048576-byte decoded source,
immutable full-history growth and recovery retention policy. A refused
oversized transaction preserves dirty draft, accepted state and last complete
file. Specify concurrent save snapshots, atomic new-file **no-replace**
creation, cooperative overwrite with verified handle-relative/no-follow and
directory identity, conflict/fault cases and unsupported-platform outcome.
Native implementation proof remains a later gate; model fixtures may prove
the transaction rules now. macOS and Windows build evidence must remain
separate from live target-platform behavior.

The handback includes a committed exact-path diff, source fingerprints,
specific executed assertion names and results, task-local dependency versions,
cwd/branch/base/final HEAD, tracked and untracked status, model evidence as
requested versus effective, lifecycle outcome and remaining limits. The author
requests any new load-bearing decision with `coord decide request` to Owner;
it cannot self-clear Data/Test/Security/UX vetoes. Root and Owner inspect the
actual files and executed evidence before the Coordinator joins. The next
production route requires a separate G3 Owner ruling under Ruling 6.
