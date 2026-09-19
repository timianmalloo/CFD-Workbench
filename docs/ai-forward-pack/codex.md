# Codex and the AI-Forward Pack

Codex reads the repository's `AGENTS.md` as project instructions and discovers skills
from `.agents/skills/<name>/SKILL.md`. This directory is shared with Antigravity;
Codex does not need Antigravity's `skills.json`, rules directory, or hooks configuration.

## Invoke a skill

Use `$collectknowledge <topic>` or `$specify <feature>` in Codex. In the CLI and IDE,
use `/skills` or type `$` to select a skill. The pack's generic `/collectknowledge`
and `/specify` notation means “invoke this workflow”; those names are not registered
as Codex slash commands. Natural-language requests can also select matching skills.
When slash notation reaches the model as text, resolve the named pack skill and read
its `SKILL.md`; repository instructions cannot change the application's command menu.

Read stage files from `reference/` beside the selected skill. Follow its script paths
under `docs/ai-forward-pack/scripts/`, using `python3` on macOS/Linux or `python` /
`py -3` on Windows. Scripts are executable workflow tools, not picker entries.

## Ground in the constitution

Read the foundation at the start of a substantive task, unless already in context:

- `.claude/knowledge/agent-body-of-knowledge.md`
- `.claude/knowledge/agent-rules-of-the-road.md`
- `.claude/knowledge/agent-persona-catalog.md`
- `.claude/knowledge/layered-optimized-architecture.md`
- `.claude/knowledge/engineering-governance.md`

Then read the relevant standards named by `AGENTS.md` and the selected skill.
Resolve a skill's `knowledge/<name>.md` to `.claude/knowledge/<name>.md` from the
repository root. These are shared knowledge files, not Claude-only instructions.
Copilot's `applyTo` wrappers are not automatically loaded by Codex; a path reference
is an instruction to read the file, not evidence that its contents are in context.
Persona definitions live in `.claude/agents/`; read the relevant persona and use the
host's available review/delegation tools. Do not assume Claude subagent registration.

## Install, update, and diagnose

`pack-apply.py` deploys these files on both fresh installs and updates. In the pack
source repo, `pwsh tools/sync-pack.ps1` generates the same surface. Run:

```sh
python3 docs/ai-forward-pack/scripts/pack-doctor.py
```

The **Codex repository readiness** check validates the installed pack skill inventory,
metadata, companion files, constitution, guide, and scripts. It checks files, not a
running application's catalog or whether an instruction was followed.

Open Codex in the target repository. If a skill is still absent after updating,
restart Codex and check the skills selector. Check for disabled entries in
`~/.codex/config.toml` (`[[skills.config]]`, `enabled = false`). Check whether an
`AGENTS.override.md` shadows `AGENTS.md`, whether nested instructions supersede it,
and whether the instruction byte budget truncates it (default 32 KiB across project
instructions). Resolve these settings with the repository owner; the pack does not
overwrite personal settings or override files. Ask Codex to name the resolved skill
path and summarize its instructions before using it as runtime confirmation.

The Antigravity/Claude/Copilot/Grok hook files are not Codex hooks. Where no Codex
hook is installed, follow the skill's explicit audit start/append commands; do not
claim automated session-start or re-read enforcement for Codex.

Sources: [OpenAI skills documentation](https://developers.openai.com/codex/skills)
and [AGENTS.md discovery](https://developers.openai.com/codex/guides/agents-md).
