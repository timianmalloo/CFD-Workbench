# CFD-Workbench: AI-Forward installation record

Installed on 2026-09-19 from `timianmalloo/ai-forward`, source commit `6d8c247`,
revision **73**, bundle **2026.09.19.1**.

The target was a new Git repository with no existing application, language choice,
AI configuration, or documentation to reconcile. The scope is repository setup for
a Windows/macOS hydrofoil design and simulation client. Application architecture,
solver selection, implementation, and license selection remain future work.

The source pack's `pack-apply.py plan` and `apply --install` reported no failed actions.
The full host deployment map was installed, including 27 skills and Codex readiness
metadata. The C# reference is retained as shipped and scoped to C# file globs; its
presence does not select C# as the application language.

## Repository integration

- The README records the project purpose, supported target platforms, setup state,
  pack entry points, and repository check commands.
- Project-specific instructions are outside the managed AGENTS block.
- The user selected the optional documentation CI workflow. It runs on relevant
  pull requests, pushes to main, and manual dispatch, with read-only repository access.
- The reference foundation checker expects a source-tree layout. `tools/check-docs.py`
  temporarily stages that layout using the installed `.claude/knowledge` files and
  the unmodified checker. Hash validation remains the pack's own implementation.
- The pack requires the first install to leave `docs/docs-index.js` absent. Its audit
  tool independently creates `docs/audit/audit-log.md`. Before the first content
  workflow, the repository check therefore accepts an empty inventory or the single
  valid, orphaned `audit-log` bootstrap node. Other content or malformed metadata
  requires an index and fails. Once the index exists, normal validation and freshness
  checks run. No index was created in the target during installation.
- Coordination classification and local Git drivers/hooks were initialized. The
  generated registry's absolute Python paths were replaced with `python` on PATH,
  and the resulting audit regeneration command was executed successfully. Fresh
  clones run `coord-core.py install` to register their own local interpreter paths.
- Basic OS metadata/Python caches are ignored and text files use LF line endings.
- No source-pack files or personal assistant settings were changed.

## Verification

The unadapted reference commands were observed failing on the missing initial index
and source-only foundation path. The consuming-repository check passes after the
integration above. Fault cases below ran in a temporary copy, never in the target.

| Scenario | Result |
|---|---|
| Fresh installation with audit metadata passes without creating an index | PASS |
| Foundation content drift is rejected | PASS |
| Malformed documentation is rejected before index initialization | PASS |
| Authored documentation with a missing index is rejected | PASS |
| An initialized connected graph passes | PASS |
| An invalid committed index is rejected | PASS |
| An unreadable audit record is rejected | PASS |

The installed pack doctor reports no failures after coordination setup. Expected
local warnings are the pre-content audit node and a user-level Copilot context setting;
the latter is not changed by repository setup. GitHub CI is checked after publication.
These checks validate repository tooling; no application runtime exists to test yet.

## Bootstrap execution record

Tier T0, fan-out cap 0. Dependency chain: inspect and initialize → apply and configure
→ verify → commit and publish. Independent read-only checks were batched; no agent
delegation was needed. Completion requires a public repository with synchronized
main, the installed pack, and passing repository CI. Application design is outside
this task. Runtime duration is recorded by the pack audit marker; token cost is not
recorded here.

Recorded worktree exception: this is the initial commit of an empty repository, so
bootstrap runs in the explicitly requested primary directory. Future writing sessions
follow the installed worktree discipline. The user's instruction to create the public
repository and apply the pack authorizes the initial commit and push.

## Deployment inventory

The following rows preserve the installer's per-artifact result. Context baselines
were refreshed again after adding the project-specific AGENTS preamble.

| Area | Destination | Action | Purpose | Status | Installer note |
|---|---|---|---|---|---|
| knowledge | `.claude/knowledge/FOUNDATION.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/FOUNDATION.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/agent-body-of-knowledge.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/agent-body-of-knowledge.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/agent-persona-catalog.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/agent-persona-catalog.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/agent-rules-of-the-road.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/agent-rules-of-the-road.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/ai-commercial-models.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ai-commercial-models.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/audit-and-change-log.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/audit-and-change-log.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/ci-and-test-efficiency.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/ci-and-test-efficiency.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/code-knowledge-graph.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/code-knowledge-graph.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/collaborative-personas.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/collaborative-personas.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/communication-and-task-discipline.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/communication-and-task-discipline.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/continuous-improvement.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/continuous-improvement.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/csharp-style-guide.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/csharp-style-guide.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/domain-and-data-modelling.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/domain-and-data-modelling.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/end-to-end-integrity.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/end-to-end-integrity.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/engineering-governance.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/engineering-governance.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/execution-graph-optimization.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/execution-graph-optimization.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/instrumentation-over-inference.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/instrumentation-over-inference.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/knowledge-visualization.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/knowledge-visualization.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/layered-optimized-architecture.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/layered-optimized-architecture.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/no-guessing-protocol.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/no-guessing-protocol.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/observability-and-instrumentation.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/observability-and-instrumentation.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/obsidian-lens.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/obsidian-lens.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/persona-audit.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/persona-audit.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/persona-cards.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/persona-cards.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/project-memory-and-obsidian.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/project-memory-and-obsidian.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/responsible-ai-policy.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/responsible-ai-policy.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/rigor-protocol.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/rigor-protocol.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/session-worktree-discipline.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/session-worktree-discipline.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/solution-selection-ladder.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/solution-selection-ladder.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/specification-standards.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/specification-standards.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/spike-protocol.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/spike-protocol.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/technical-ui-design.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/technical-ui-design.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/testing-strategy.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/testing-strategy.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/ui-archetype-catalog.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ui-archetype-catalog.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/ui-archetype-grammar.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ui-archetype-grammar.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/ui-craft-detection.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ui-craft-detection.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/ui-design-craft.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ui-design-craft.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.claude/knowledge/ui-interaction-design.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/instructions/ui-interaction-design.instructions.md` | ADD | Engineering standards and reference knowledge | ok | wrapped applyTo |
| knowledge | `.claude/knowledge/ui-visual-assets.md` | ADD | Engineering standards and reference knowledge | ok |  |
| knowledge | `.github/knowledge/ui-visual-assets.md` | ADD | Engineering standards and reference knowledge | ok |  |
| skills | `.claude/skills/adddomainexperts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/adddomainexperts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/adddomainexperts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/adddomainexperts.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/addpacktorepo/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/addpacktorepo/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/addpacktorepo/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/addpacktorepo.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/adopt/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/adopt/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/adopt/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/adopt.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/also/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/also/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/also/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/also.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/apply-learnings/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/apply-learnings/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/apply-learnings/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/apply-learnings.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/auditlog/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/auditlog/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/auditlog/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/auditlog.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/code-hygiene/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/code-hygiene/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/code-hygiene/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/code-hygiene.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/collectknowledge/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/collectknowledge/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/collectknowledge/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/collectknowledge.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/define-architecture/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/define-architecture/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/define-architecture/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/define-architecture.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/design-slice/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/design-slice/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/design-slice/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/design-slice/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/design-slice/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/design-slice/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/design-slice/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/design-slice/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/design-slice/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/design-slice.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/document/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/document/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/document/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/document.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/dream/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/dream/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/dream/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/dream.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/execute-with-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/execute-with-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/execute-with-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/execute-with-coordination.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/extendaibundle/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/extendaibundle/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/extendaibundle/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/extendaibundle.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/forensicreview/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/forensicreview/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/forensicreview/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/forensicreview.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/implement/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/implement/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/implement/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/implement/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/implement/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/implement/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/implement/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/implement/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/implement/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/implement.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/investigate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/investigate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/investigate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/investigate.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/migrate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/migrate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/migrate/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/migrate.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/optimize-graph/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/optimize-graph/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/optimize-graph/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/optimize-graph.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/prepare-for-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/prepare-for-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/prepare-for-coordination/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/prepare-for-coordination.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/prompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/prompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/prompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/prompts.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/searchprompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/searchprompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/searchprompts/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/searchprompts.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/session-profiler/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/session-profiler/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/session-profiler/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/session-profiler.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/specify/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/specify/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/specify/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/specify.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/ui-design/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/ui-design/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/ui-design/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/ui-design/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/ui-design/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/ui-design/reference/definition-of-done.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/ui-design/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/ui-design/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/ui-design/reference/flow.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/ui-design/reference/triggered-standards.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/ui-design/reference/triggered-standards.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/ui-design/reference/triggered-standards.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/ui-design.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/updatepack/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/updatepack/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/updatepack/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/updatepack.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.claude/skills/visualize/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.grok/skills/visualize/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.agents/skills/visualize/SKILL.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| skills | `.github/prompts/visualize.prompt.md` | ADD | Reasoning workflows and their companion resources | ok |  |
| bundle | `docs/ai-forward-pack/codex-skills.json` | ADD | Shared reference material, tooling, templates, or configuration | ok | derived pack skill inventory regenerated from commands/ |
| bundle | `docs/ai-forward-pack/codex.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| agents | `.claude/agents/ai-systems-engineer.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/ai-systems-engineer.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/ai-systems-engineer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/data-persistence-architect.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/data-persistence-architect.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/data-persistence-architect.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/documentation-steward.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/documentation-steward.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/documentation-steward.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/domain-researcher.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/domain-researcher.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/domain-researcher.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/mobile-app-developer.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/mobile-app-developer.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/mobile-app-developer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/native-desktop-developer.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/native-desktop-developer.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/native-desktop-developer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/orchestrator.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/orchestrator.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/orchestrator.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/privacy-data-governance.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/privacy-data-governance.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/privacy-data-governance.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/product-strategist.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/product-strategist.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/product-strategist.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/release-engineer.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/release-engineer.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/release-engineer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/ux-accessibility.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/ux-accessibility.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/ux-accessibility.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/ux-researcher-ia.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/ux-researcher-ia.agent.md` | ADD | Peer and review personas | ok | tools: stripped |
| agents | `.grok/agents/ux-researcher-ia.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/csharp-developer_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/csharp-developer.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/csharp-developer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/distributed-systems-architect_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/distributed-systems-architect.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/distributed-systems-architect.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/enterprise-architect_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/enterprise-architect.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/enterprise-architect.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/patterns-expert_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/patterns-expert.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/patterns-expert.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/python-developer_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/python-developer.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/python-developer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/rust-developer_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/rust-developer.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/rust-developer.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/security-identity-architect_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/security-identity-architect.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/security-identity-architect.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/sre-diagnostician_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/sre-diagnostician.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/sre-diagnostician.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/tech-lead_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/tech-lead.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/tech-lead.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/test-architect_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/test-architect.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/test-architect.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| agents | `.claude/agents/the-simplifier_agent.md` | ADD | Peer and review personas | ok |  |
| agents | `.github/agents/the-simplifier.agent.md` | ADD | Peer and review personas | ok | renamed .agent.md |
| agents | `.grok/agents/the-simplifier.md` | ADD | Peer and review personas | ok | tools: stripped; Grok type name |
| bundle | `docs/ai-forward-pack/templates/adr.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/architecture.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/audit-explorer.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/decision-note.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/defect-classes.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/design-language-preview.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/design-language.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/design.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/doc-viewer.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/docs-explorer.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/documentation-bundle.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/domain-expert.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/dream-manifest.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/dream-review.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/glossary.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/investigation.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/knowledge-base.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/mockup-harness.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/native-ui-proof-pack.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/privacy-review.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/project-memory.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/proof-pack.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/session-contract.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/spec.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/threat-model.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/ui-capability-guide.template.html` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/ui-guide-hub.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/templates/ui-review.template.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/apply-learnings.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/audit-log.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/bounded_process.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/conductor-join.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/context-budget.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/coord-core.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/coord_ids.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/design-lint.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/docs-explorer-core.js` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/docs-graph.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/dream.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/foundation-check.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/graphify-setup.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/marker-lint.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/obsidian-setup.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/pack-apply.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/pack-doctor.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/prompt-log.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/repo_identity.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/run-verify-gates.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/scrub.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/session-profile.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/ui-craft-gate.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/verify-no-conflict-markers.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/verify-no-new-console-launches.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/visual-assets-setup.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/scripts/xaml-token-lint.py` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/README.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/OVERVIEW.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/research-synthesis.md` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| bundle | `docs/ai-forward-pack/context-budget.json` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| hooks | `docs/ai-forward-pack/hooks/reread-guard.py` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `docs/ai-forward-pack/hooks/session-start.py` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `docs/ai-forward-pack/hooks/README.md` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `.github/hooks/ai-forward.json` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `.grok/hooks/ai-forward.json` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `.agents/hooks.json` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `.grok/rules/grok-surface.md` | ADD | Host-specific session and read hooks | ok |  |
| hooks | `.agents/rules/agy-surface.md` | ADD | Host-specific session and read hooks | ok |  |
| bundle | `.agents/skills.json` | ADD | Shared reference material, tooling, templates, or configuration | ok |  |
| hooks | `.claude/settings.json` | ADD | Host-specific session and read hooks | ok | hooks + showThinkingSummaries merged; other keys untouched |
| bundle | `.gitignore` | UPDATE | Shared reference material, tooling, templates, or configuration | ok | added *.jsonl.lock, spikes/, docs/audit/.run-starts.json, docs/audit/.run-starts.json.tmp, .agents/*, !.agents/artifacts.yml, !.agents/skills*, !.agents/hooks.json, !.agents/rules* |
| bundle | `docs/index.html` | ADD | Shared reference material, tooling, templates, or configuration | ok | Docs Explorer instantiated (one-time) |
| bundle | `docs/docs-index.js` | SKIP | Shared reference material, tooling, templates, or configuration | ok | never created or overwritten (V10) |
| front-doors | `AGENTS.md` | ADD | Project instruction entry points | ok | created with the managed block |
| front-doors | `CLAUDE.md` | ADD | Project instruction entry points | ok | @AGENTS.md import + addendum |
| meta | `docs/ai-forward-pack/INSTALL.md` | UPDATE | Installed revision and measured context baselines | ok | revision None -> 73 |
| meta | `context-budget gate` | BASELINE | Installed revision and measured context baselines | ok | Commit it with the change that caused the growth — that diff IS the control. |
| meta | `context-budget prefix` | BASELINE | Installed revision and measured context baselines | ok | prefix baseline updated to ~89,223 in /Users/mallalieut/projects/CFD-Workbench/docs/ai-forward-pack/context-budget.json |
| meta | `context-budget skills` | BASELINE | Installed revision and measured context baselines | ok | skills baseline updated in /Users/mallalieut/projects/CFD-Workbench/docs/ai-forward-pack/context-budget.json |

Repository additions beyond the mechanical deployment: README and project preamble,
`.github/workflows/docs-health.yml`, `tools/check-docs.py`, `.gitattributes`, the
coordination artifact registry, this report, and the audit history/viewer.

## Next workflow

Use `$collectknowledge` to establish hydrofoil design and simulation knowledge, then
`$specify` for the first application workflow. `$adopt` brings existing implementation
and documentation into the knowledge graph when such material is introduced.

[Pack explainer](https://timianmalloo.github.io/ai-forward/) ·
[Overview](OVERVIEW.md) · [Deployment map](INSTALL.md) · [Docs Explorer](../index.html)
